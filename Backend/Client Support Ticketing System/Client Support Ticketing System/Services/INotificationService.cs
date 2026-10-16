using ClientSupportTicketingSystem.Data.Models;
using ClientSupportTicketingSystem.DTOs.Common;

namespace ClientSupportTicketingSystem.Services
{
    public record NotificationResponse(
        int Id,
        int? TicketId,
        string Message,
        bool IsRead,
        DateTime CreatedAt,
        DateTime? ReadAt);

    public interface INotificationService
    {
        Task QueueAsync(
            Ticket ticket,
            string message,
            IEnumerable<int> recipients,
            int actorId);

        Task<List<int>> ManagerIdsAsync();

        Task<PagedResponse<NotificationResponse>> ListAsync(
            int userId,
            PageQuery page,
            bool? isRead);

        Task<int> UnreadCountAsync(int userId);

        Task<bool> MarkReadAsync(int userId, int id);

        Task MarkAllReadAsync(int userId);
    }
}