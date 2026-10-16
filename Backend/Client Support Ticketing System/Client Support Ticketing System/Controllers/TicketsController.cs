using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Enums;
using ClientSupportTicketingSystem.Data.UnitOfWork;
using ClientSupportTicketingSystem.DTOs.Common;
using ClientSupportTicketingSystem.DTOs.Tickets;
using ClientSupportTicketingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientSupportTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _service;
        private readonly IUnitOfWork _work;

        public TicketsController(ITicketService service, IUnitOfWork work)
        {
            _service = service;
            _work = work;
        }

        private bool TryReadUser(out int userId, out UserRole role)
        {
            bool validId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;
            bool validRole = Enum.TryParse(User.FindFirstValue(ClaimTypes.Role), out role) && Enum.IsDefined(typeof(UserRole), role);
            return validId && validRole;
        }

        private IActionResult Reply<T>((int StatusCode, T? Data, string? Error) result) where T : class
        {
            if (result.Error != null)
            {
                return StatusCode(result.StatusCode, new
                {
                    statusCode = result.StatusCode,
                    message = result.Error,
                    traceId = HttpContext.TraceIdentifier
                });
            }
            return StatusCode(result.StatusCode, result.Data);
        }

        [HttpGet]
        [Authorize(Roles = "SupportManager")]
        public async Task<IActionResult> All([FromQuery] TicketQuery query)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            return Reply(await _service.ListAsync(userId, role, query));
        }

        [HttpGet("my")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> My([FromQuery] TicketQuery query)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            return Reply(await _service.ListAsync(userId, role, query));
        }

        [HttpGet("assigned")]
        [Authorize(Roles = "SupportEmployee")]
        public async Task<IActionResult> Assigned([FromQuery] TicketQuery query)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            return Reply(await _service.ListAsync(userId, role, query));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            return Reply(await _service.DetailsAsync(userId, role, id));
        }

        [HttpPost]
        [Authorize(Roles = "Client")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(30 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 30 * 1024 * 1024)]
        public async Task<IActionResult> Create([FromForm] CreateTicketRequest request)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            var result = await _service.CreateAsync(userId, role, request);
            if (result.Data == null) return Reply(result);
            return CreatedAtAction(nameof(Details), new { id = result.Data.Id }, result.Data);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketRequest request)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            return Reply(await _service.UpdateAsync(userId, role, id, request));
        }

        [HttpPatch("{id:int}/assignment")]
        [Authorize(Roles = "SupportManager")]
        public async Task<IActionResult> Assign(int id, [FromBody] AssignTicketRequest request)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            return Reply(await _service.AssignAsync(userId, role, id, request.EmployeeId));
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "SupportEmployee")]
        public async Task<IActionResult> Status(int id, [FromBody] TicketStatusRequest request)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            return Reply(await _service.SetStatusAsync(userId, role, id, request.Status!.Value));
        }

        [HttpPost("{id:int}/resolve")]
        [Authorize(Roles = "SupportEmployee")]
        public async Task<IActionResult> Resolve(int id)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            return Reply(await _service.ResolveAsync(userId, role, id));
        }

        [HttpPost("{id:int}/confirm-solution")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Confirm(int id)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            return Reply(await _service.ConfirmAsync(userId, role, id));
        }

        [HttpPost("{id:int}/close")]
        [Authorize(Roles = "SupportEmployee")]
        public async Task<IActionResult> Close(int id)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            return Reply(await _service.CloseAsync(userId, role, id));
        }

        [HttpGet("{id:int}/comments")]
        public async Task<IActionResult> Comments(int id, [FromQuery] PageQuery query)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            return Reply(await _service.CommentsAsync(userId, role, id, query));
        }

        [HttpPost("{id:int}/comments")]
        [Authorize(Roles = "Client,SupportEmployee")]
        public async Task<IActionResult> Comment(int id, [FromBody] AddCommentRequest request)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            return Reply(await _service.AddCommentAsync(userId, role, id, request));
        }

        [HttpGet("{id:int}/attachments/{attachmentId:int}")]
        public async Task<IActionResult> Download(int id, int attachmentId)
        {
            if (!TryReadUser(out int userId, out UserRole role)) return Unauthorized();
            var result = await _service.DownloadAsync(userId, role, id, attachmentId);
            if (result.Data == null) return Reply(result);
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Cache-Control"] = "no-store";
            return File(result.Data.Stream, "application/octet-stream", result.Data.FileName);
        }

        [HttpGet("~/api/dashboard")]
        [Authorize(Roles = "SupportManager")]
        public async Task<IActionResult> Dashboard()
        {
            var tickets = _work.Tickets.Query();
            var users = _work.Users.Query();
            var counts = await tickets.GroupBy(t => t.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();

            var statusCounts = Enum.GetValues<TicketStatus>().Select(status => new
            {
                status = status.ToString(),
                count = counts.Where(c => c.Status == status).Select(c => c.Count).FirstOrDefault()
            }).ToList();

            var employees = await users.Where(u => u.Role == UserRole.SupportEmployee).Select(u => new
            {
                employeeId = u.Id,
                fullName = u.FullName,
                isActive = u.IsActive,
                assignedTickets = tickets.Count(t => t.AssignedEmployeeId == u.Id),
                openTickets = tickets.Count(t => t.AssignedEmployeeId == u.Id && t.Status != TicketStatus.Closed),
                closedTickets = tickets.Count(t => t.AssignedEmployeeId == u.Id && t.Status == TicketStatus.Closed)
            }).OrderByDescending(e => e.closedTickets).ThenBy(e => e.employeeId).ToListAsync();

            var now = DateTime.UtcNow;
            var firstMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
            var monthly = await tickets.Where(t => t.CreatedAt >= firstMonth).GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month }).Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();

            var months = Enumerable.Range(0, 12).Select(offset => firstMonth.AddMonths(offset)).Select(month => new
            {
                year = month.Year,
                month = month.Month,
                createdTickets = monthly.Where(m => m.Year == month.Year && m.Month == month.Month).Select(m => m.Count).FirstOrDefault()
            }).ToList();

            return Ok(new
            {
                totalTickets = counts.Sum(c => c.Count),
                totalClients = await users.CountAsync(u => u.Role == UserRole.Client),
                activeClients = await users.CountAsync(u => u.Role == UserRole.Client && u.IsActive),
                totalEmployees = employees.Count,
                activeEmployees = employees.Count(e => e.isActive),
                statusCounts,
                employeeProductivity = employees,
                ticketsByMonth = months,
                productivityDefinition = "Number of closed tickets currently assigned to each employee.",
                generatedAt = now
            });
        }
    }
}