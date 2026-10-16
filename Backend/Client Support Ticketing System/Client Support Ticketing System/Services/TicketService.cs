using ClientSupportTicketingSystem.Data.Enums;
using ClientSupportTicketingSystem.Data.Models;
using ClientSupportTicketingSystem.Data.UnitOfWork;
using ClientSupportTicketingSystem.DTOs.Common;
using ClientSupportTicketingSystem.DTOs.Tickets;
using Microsoft.EntityFrameworkCore;

namespace ClientSupportTicketingSystem.Services
{
    public class TicketService : ITicketService
    {
        private readonly IUnitOfWork _work;
        private readonly ITicketAttachmentService _files;
        private readonly INotificationService _notifications;

        public TicketService(IUnitOfWork work, ITicketAttachmentService files, INotificationService notifications)
        {
            _work = work;
            _files = files;
            _notifications = notifications;
        }

        private async Task<(int StatusCode, Ticket? Data, string? Error)> AccessibleAsync(int id, int userId, UserRole role)
        {
            if (id <= 0) return (400, null, "Ticket id must be greater than zero.");

            var ticket = await _work.Tickets.GetDetailsAsync(id);

            bool allowed = ticket != null && (role == UserRole.SupportManager || (role == UserRole.Client && ticket.ClientId == userId) || (role == UserRole.SupportEmployee && ticket.AssignedEmployeeId == userId));

            if (!allowed) return (404, null, "Ticket not found.");

            return (200, ticket, null);
        }

        private static void Touch(Ticket ticket)
        {
            ticket.UpdatedAt = DateTime.UtcNow;
        }

        private static void ResetResolution(Ticket ticket)
        {
            ticket.IsConfirmedByClient = false;
            ticket.IsResolvedByEmployee = false;
            ticket.ResolvedAt = null;
        }

        private async Task NotifyParticipantsAsync(Ticket ticket, string message, int actorId, int? extra = null)
        {
            var recipients = await _notifications.ManagerIdsAsync();
            recipients.Add(ticket.ClientId);

            if (ticket.AssignedEmployeeId.HasValue) recipients.Add(ticket.AssignedEmployeeId.Value);

            if (extra.HasValue) recipients.Add(extra.Value);

            await _notifications.QueueAsync(ticket, message, recipients, actorId);
        }

        public async Task<(int StatusCode, PagedResponse<TicketSummaryResponse>? Data, string? Error)> ListAsync(int userId, UserRole role, TicketQuery filter)
        {
            var query = _work.Tickets.Query();

            if (role == UserRole.Client)
            {
                query = query.Where(t => t.ClientId == userId);
            }
            else if (role == UserRole.SupportEmployee)
            {
                query = query.Where(t => t.AssignedEmployeeId == userId);
            }
            else if (role != UserRole.SupportManager)
            {
                return (403, null, "This operation is not allowed for your role.");
            }

            if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status.Value);

            if (filter.ClientId.HasValue) query = query.Where(t => t.ClientId == filter.ClientId.Value);

            if (filter.EmployeeId.HasValue) query = query.Where(t => t.AssignedEmployeeId == filter.EmployeeId.Value);

            if (filter.ProductId.HasValue) query = query.Where(t => t.ProductId == filter.ProductId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string term = filter.Search.Trim();
                query = query.Where(t => t.ProblemDescription.Contains(term) || t.Product.Name.Contains(term));
            }

            var result = await query
                .OrderByDescending(t => t.Id)
                .Select(t => new TicketSummaryResponse
                {
                    Id = t.Id,
                    ProductId = t.ProductId,
                    ProductName = t.Product.Name,
                    ClientId = t.ClientId,
                    ClientName = t.Client.FullName,
                    ProblemDescription = t.ProblemDescription,
                    AssignedEmployeeId = t.AssignedEmployeeId,
                    AssignedEmployeeName = t.AssignedEmployee == null ? null : t.AssignedEmployee.FullName,
                    Status = t.Status.ToString(),
                    IsConfirmedByClient = t.IsConfirmedByClient,
                    IsResolvedByEmployee = t.IsResolvedByEmployee,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    ClosedAt = t.ClosedAt
                })
                .ToPageAsync(filter);

            return (200, result, null);
        }

        public async Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)> DetailsAsync(int userId, UserRole role, int id)
        {
            var access = await AccessibleAsync(id, userId, role);

            if (access.Data == null) return (access.StatusCode, null, access.Error);

            return (200, Map(access.Data), null);
        }

        public async Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)> CreateAsync(int userId, UserRole role, CreateTicketRequest request)
        {
            if (role != UserRole.Client) return (403, null, "This operation is not allowed for your role.");

            var client = await _work.Users.GetByIdAsync(userId);

            if (client == null || !client.IsActive || client.Role != UserRole.Client)
            {
                return (403, null, "Client account is inactive.");
            }

            var product = await _work.Products.GetByIdAsync(request.ProductId);

            if (product == null) return (400, null, "Selected product does not exist.");

            if (string.IsNullOrWhiteSpace(request.ProblemDescription)) return (400, null, "Problem description is required.");

            List<TicketAttachment> attachments;

            try
            {
                attachments = await _files.SaveAsync(request.Attachments);
            }
            catch (InvalidDataException error)
            {
                return (400, null, error.Message);
            }

            bool saved = false;

            try
            {
                var ticket = new Ticket
                {
                    ClientId = client.Id,
                    Client = client,
                    ProductId = product.Id,
                    Product = product,
                    ProblemDescription = request.ProblemDescription.Trim(),
                    Status = TicketStatus.New,
                    Attachments = attachments
                };

                await _work.Tickets.AddAsync(ticket);

                await _notifications.QueueAsync(ticket, "A new support ticket was submitted.", await _notifications.ManagerIdsAsync(), userId);

                await _work.SaveChangesAsync();
                saved = true;

                return (201, Map(ticket), null);
            }
            finally
            {
                if (!saved) _files.DeleteFiles(attachments);
            }
        }

        public async Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)> UpdateAsync(int userId, UserRole role, int id, UpdateTicketRequest request)
        {
            if (role != UserRole.Client) return (403, null, "This operation is not allowed for your role.");

            var access = await AccessibleAsync(id, userId, role);

            if (access.Data == null) return (access.StatusCode, null, access.Error);

            var ticket = access.Data;

            if (ticket.Status == TicketStatus.Closed) return (409, null, "The ticket is already closed.");

            var product = await _work.Products.GetByIdAsync(request.ProductId);

            if (product == null) return (400, null, "Selected product does not exist.");

            if (string.IsNullOrWhiteSpace(request.ProblemDescription)) return (400, null, "Problem description is required.");

            string description = request.ProblemDescription.Trim();

            if (ticket.ProductId == product.Id && ticket.ProblemDescription == description)
            {
                return (200, Map(ticket), null);
            }

            ticket.ProductId = product.Id;
            ticket.Product = product;
            ticket.ProblemDescription = description;

            ResetResolution(ticket);
            Touch(ticket);

            await NotifyParticipantsAsync(ticket, $"Ticket #{id} was updated by its owner.", userId);

            await _work.SaveChangesAsync();

            return (200, Map(ticket), null);
        }

        public async Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)> AssignAsync(int userId, UserRole role, int id, int employeeId)
        {
            if (role != UserRole.SupportManager) return (403, null, "This operation is not allowed for your role.");

            var access = await AccessibleAsync(id, userId, role);

            if (access.Data == null) return (access.StatusCode, null, access.Error);

            var ticket = access.Data;

            if (ticket.Status == TicketStatus.Closed) return (409, null, "The ticket is already closed.");

            var employee = await _work.Users.GetByIdAsync(employeeId);

            if (employee == null || employee.Role != UserRole.SupportEmployee || !employee.IsActive)
            {
                return (400, null, "Select an active support employee.");
            }

            if (ticket.AssignedEmployeeId == employeeId) return (200, Map(ticket), null);

            int? previous = ticket.AssignedEmployeeId;

            ticket.AssignedEmployeeId = employeeId;
            ticket.AssignedEmployee = employee;
            ticket.AssignedAt = DateTime.UtcNow;
            ticket.Status = TicketStatus.Assigned;

            ResetResolution(ticket);
            Touch(ticket);

            await NotifyParticipantsAsync(ticket, $"Assignment of ticket #{id} was updated.", userId, previous);

            await _work.SaveChangesAsync();

            return (200, Map(ticket), null);
        }

        public async Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)> SetStatusAsync(int userId, UserRole role, int id, TicketStatus status)
        {
            if (role != UserRole.SupportEmployee) return (403, null, "This operation is not allowed for your role.");

            var access = await AccessibleAsync(id, userId, role);

            if (access.Data == null) return (access.StatusCode, null, access.Error);

            var ticket = access.Data;

            if (ticket.Status == TicketStatus.Closed) return (409, null, "The ticket is already closed.");

            if (status != TicketStatus.Assigned && status != TicketStatus.Paused)
            {
                return (400, null, "Use Assigned or Paused here; closing requires client confirmation.");
            }

            if (ticket.Status == status) return (200, Map(ticket), null);

            ticket.Status = status;
            Touch(ticket);

            await NotifyParticipantsAsync(ticket, $"Ticket #{id} status changed to {status}.", userId);

            await _work.SaveChangesAsync();

            return (200, Map(ticket), null);
        }

        public async Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)> ResolveAsync(int userId, UserRole role, int id)
        {
            if (role != UserRole.SupportEmployee) return (403, null, "This operation is not allowed for your role.");

            var access = await AccessibleAsync(id, userId, role);

            if (access.Data == null) return (access.StatusCode, null, access.Error);

            var ticket = access.Data;

            if (ticket.Status == TicketStatus.Closed) return (409, null, "The ticket is already closed.");

            if (ticket.IsResolvedByEmployee) return (200, Map(ticket), null);

            ticket.IsResolvedByEmployee = true;
            ticket.ResolvedAt = DateTime.UtcNow;
            ticket.Status = TicketStatus.Assigned;

            Touch(ticket);

            await NotifyParticipantsAsync(ticket, $"A solution is ready for ticket #{id}. The client can confirm it.", userId);

            await _work.SaveChangesAsync();

            return (200, Map(ticket), null);
        }

        public async Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)> ConfirmAsync(int userId, UserRole role, int id)
        {
            if (role != UserRole.Client) return (403, null, "This operation is not allowed for your role.");

            var access = await AccessibleAsync(id, userId, role);

            if (access.Data == null) return (access.StatusCode, null, access.Error);

            var ticket = access.Data;

            if (ticket.Status == TicketStatus.Closed) return (409, null, "The ticket is already closed.");

            if (!ticket.AssignedEmployeeId.HasValue)
            {
                return (409, null, "The ticket must be assigned before confirming a solution.");
            }

            if (ticket.IsConfirmedByClient) return (200, Map(ticket), null);

            ticket.IsConfirmedByClient = true;
            ticket.ResolvedAt ??= DateTime.UtcNow;

            Touch(ticket);

            await NotifyParticipantsAsync(ticket, $"The client confirmed the solution for ticket #{id}.", userId);

            await _work.SaveChangesAsync();

            return (200, Map(ticket), null);
        }

        public async Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)> CloseAsync(int userId, UserRole role, int id)
        {
            if (role != UserRole.SupportEmployee) return (403, null, "This operation is not allowed for your role.");

            var access = await AccessibleAsync(id, userId, role);

            if (access.Data == null) return (access.StatusCode, null, access.Error);

            var ticket = access.Data;

            if (ticket.Status == TicketStatus.Closed) return (200, Map(ticket), null);

            if (!ticket.IsConfirmedByClient)
            {
                return (409, null, "Client confirmation is required before closing.");
            }

            ticket.Status = TicketStatus.Closed;
            ticket.ClosedAt = DateTime.UtcNow;

            Touch(ticket);

            await NotifyParticipantsAsync(ticket, $"Ticket #{id} was closed.", userId);

            await _work.SaveChangesAsync();

            return (200, Map(ticket), null);
        }

        public async Task<(int StatusCode, PagedResponse<CommentResponse>? Data, string? Error)> CommentsAsync(int userId, UserRole role, int id, PageQuery query)
        {
            var access = await AccessibleAsync(id, userId, role);

            if (access.Data == null) return (access.StatusCode, null, access.Error);

            var result = await _work.Tickets.Comments(id)
                .OrderBy(c => c.Id)
                .Select(c => new CommentResponse
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    AuthorName = c.User.FullName,
                    AuthorRole = c.User.Role.ToString(),
                    Content = c.Content,
                    ParentCommentId = c.ParentCommentId,
                    CreatedAt = c.CreatedAt
                })
                .ToPageAsync(query);

            return (200, result, null);
        }
        public async Task<(int StatusCode, CommentResponse? Data, string? Error)> AddCommentAsync(int userId, UserRole role, int id, AddCommentRequest request)
        {
            if (role != UserRole.Client && role != UserRole.SupportEmployee)
            {
                return (403, null, "Only the client and assigned employee can add comments.");
            }

            var access = await AccessibleAsync(id, userId, role);

            if (access.Data == null) return (access.StatusCode, null, access.Error);

            var ticket = access.Data;

            if (ticket.Status == TicketStatus.Closed) return (409, null, "The ticket is already closed.");

            if (string.IsNullOrWhiteSpace(request.Content)) return (400, null, "Comment content is required.");

            if (request.ParentCommentId.HasValue && !await _work.Tickets.Comments(id).AnyAsync(c => c.Id == request.ParentCommentId.Value))
            {
                return (400, null, "The parent comment does not belong to this ticket.");
            }

            var author = await _work.Users.GetByIdAsync(userId);

            var comment = new Comment
            {
                Content = request.Content.Trim(),
                UserId = userId,
                ParentCommentId = request.ParentCommentId,
                User = author!,
                TicketId = id
            };

            ticket.Comments.Add(comment);
            Touch(ticket);

            await NotifyParticipantsAsync(ticket, $"A new comment was added to ticket #{id}.", userId);

            await _work.SaveChangesAsync();

            return (201, new CommentResponse
            {
                Id = comment.Id,
                UserId = comment.UserId,
                AuthorName = author!.FullName,
                AuthorRole = author.Role.ToString(),
                Content = comment.Content,
                ParentCommentId = comment.ParentCommentId,
                CreatedAt = comment.CreatedAt
            }, null);
        }
        public async Task<(int StatusCode, AttachmentDownload? Data, string? Error)> DownloadAsync(int userId, UserRole role, int id, int attachmentId)
        {
            var access = await AccessibleAsync(id, userId, role);

            if (access.Data == null) return (access.StatusCode, null, access.Error);

            var ticket = access.Data;

            var attachment = ticket.Attachments.SingleOrDefault(a => a.Id == attachmentId);

            if (attachment == null) return (404, null, "Attachment not found.");

            var stream = _files.OpenRead(attachment.FilePath);

            if (stream == null) return (404, null, "Attachment file not found.");

            return (200, new AttachmentDownload(stream, attachment.FileName), null);
        }
        private static TicketDetailsResponse Map(Ticket ticket)
        {
            return new TicketDetailsResponse
            {
                Id = ticket.Id,
                ProductId = ticket.ProductId,
                ProductName = ticket.Product.Name,
                ClientId = ticket.ClientId,
                ClientName = ticket.Client.FullName,
                ProblemDescription = ticket.ProblemDescription,
                AssignedEmployeeId = ticket.AssignedEmployeeId,
                AssignedEmployeeName = ticket.AssignedEmployee?.FullName,
                Status = ticket.Status.ToString(),
                IsConfirmedByClient = ticket.IsConfirmedByClient,
                IsResolvedByEmployee = ticket.IsResolvedByEmployee,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                AssignedAt = ticket.AssignedAt,
                ResolvedAt = ticket.ResolvedAt,
                ClosedAt = ticket.ClosedAt,
                Attachments = ticket.Attachments
                    .OrderBy(a => a.Id)
                    .Select(a => new AttachmentResponse
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        DownloadUrl = $"/api/tickets/{ticket.Id}/attachments/{a.Id}"
                    })
                    .ToList()
            };
        }
    }
}