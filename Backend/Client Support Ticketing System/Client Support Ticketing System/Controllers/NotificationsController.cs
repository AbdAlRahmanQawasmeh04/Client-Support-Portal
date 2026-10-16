using System.Security.Claims;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.DTOs.Common;
using ClientSupportTicketingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientSupportTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationsController(INotificationService service)
        {
            _service = service;
        }

        private bool TryReadUser(out int userId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;
        }

        [HttpGet]
        public async Task<IActionResult> All([FromQuery] PageQuery query, [FromQuery] bool? isRead)
        {
            if (!TryReadUser(out int userId)) return Unauthorized();
            return Ok(await _service.ListAsync(userId, query, isRead));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> Count()
        {
            if (!TryReadUser(out int userId)) return Unauthorized();
            return Ok(new { unreadCount = await _service.UnreadCountAsync(userId) });
        }

        [HttpPatch("{id:int}/read")]
        public async Task<IActionResult> Read(int id)
        {
            if (!TryReadUser(out int userId)) return Unauthorized();
            bool found = await _service.MarkReadAsync(userId, id);
            if (!found) return NotFound(new { message = "Notification not found." });
            return NoContent();
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> ReadAll()
        {
            if (!TryReadUser(out int userId)) return Unauthorized();
            await _service.MarkAllReadAsync(userId);
            return NoContent();
        }
    }
}