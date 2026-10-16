using ClientSupportTicketingSystem.Data.Enums;
using ClientSupportTicketingSystem.DTOs.Common;
using ClientSupportTicketingSystem.DTOs.Tickets;

namespace ClientSupportTicketingSystem.Services
{
    public record AttachmentDownload(Stream Stream, string FileName);

    public interface ITicketService
    {
        Task<(int StatusCode, PagedResponse<TicketSummaryResponse>? Data, string? Error)>
            ListAsync(int userId, UserRole role, TicketQuery query);

        Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)>
            DetailsAsync(int userId, UserRole role, int id);

        Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)>
            CreateAsync(int userId, UserRole role, CreateTicketRequest request);

        Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)>
            UpdateAsync(int userId, UserRole role, int id, UpdateTicketRequest request);

        Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)>
            AssignAsync(int userId, UserRole role, int id, int employeeId);

        Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)>
            SetStatusAsync(int userId, UserRole role, int id, TicketStatus status);

        Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)>
            ResolveAsync(int userId, UserRole role, int id);

        Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)>
            ConfirmAsync(int userId, UserRole role, int id);

        Task<(int StatusCode, TicketDetailsResponse? Data, string? Error)>
            CloseAsync(int userId, UserRole role, int id);

        Task<(int StatusCode, PagedResponse<CommentResponse>? Data, string? Error)>
            CommentsAsync(int userId, UserRole role, int id, PageQuery query);

        Task<(int StatusCode, CommentResponse? Data, string? Error)>
            AddCommentAsync(int userId, UserRole role, int id, AddCommentRequest request);

        Task<(int StatusCode, AttachmentDownload? Data, string? Error)>
            DownloadAsync(int userId, UserRole role, int id, int attachmentId);
    }
}