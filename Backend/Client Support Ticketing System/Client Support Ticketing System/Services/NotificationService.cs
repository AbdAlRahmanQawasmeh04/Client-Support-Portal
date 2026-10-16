using ClientSupportTicketingSystem.Data.Enums;
using ClientSupportTicketingSystem.Data.Models;
using ClientSupportTicketingSystem.Data.UnitOfWork;
using ClientSupportTicketingSystem.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace ClientSupportTicketingSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _work;

        public NotificationService(IUnitOfWork work)
        {
            _work = work;
        }

        public Task<List<int>> ManagerIdsAsync()
        {
            return _work.Users.Query()
                .Where(u => u.IsActive
                    && u.Role == UserRole.SupportManager)
                .Select(u => u.Id)
                .ToListAsync();
        }

        public async Task QueueAsync(
            Ticket ticket,
            string message,
            IEnumerable<int> recipients,
            int actorId)
        {
            var ids = recipients
                .Where(id => id > 0 && id != actorId)
                .Distinct()
                .ToList();

            var activeIds = await _work.Users.Query()
                .Where(u => u.IsActive && ids.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            foreach (int id in activeIds)
            {
                await _work.Notifications.AddAsync(new Notification
                {
                    UserId = id,
                    Ticket = ticket,
                    Message = message
                });
            }
        }

        public Task<PagedResponse<NotificationResponse>> ListAsync(
            int userId,
            PageQuery page,
            bool? isRead)
        {
            var query = _work.Notifications.Query()
                .Where(n => n.UserId == userId);

            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            return query
                .OrderByDescending(n => n.Id)
                .Select(n => new NotificationResponse(
                    n.Id,
                    n.TicketId,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt,
                    n.ReadAt))
                .ToPageAsync(page);
        }

        public Task<int> UnreadCountAsync(int userId)
        {
            return _work.Notifications.Query()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkReadAsync(int userId, int id)
        {
            var item = await _work.Notifications.GetByIdAsync(id);

            if (item == null || item.UserId != userId)
                return false;

            if (item.IsRead)
                return true;

            item.IsRead = true;
            item.ReadAt = DateTime.UtcNow;

            await _work.SaveChangesAsync();

            return true;
        }

        public async Task MarkAllReadAsync(int userId)
        {
            var ids = await _work.Notifications.Query()
                .Where(n => n.UserId == userId && !n.IsRead)
                .Select(n => n.Id)
                .ToListAsync();

            foreach (int id in ids)
            {
                var item = await _work.Notifications.GetByIdAsync(id);

                if (item != null && !item.IsRead)
                {
                    item.IsRead = true;
                    item.ReadAt = DateTime.UtcNow;
                }
            }

            await _work.SaveChangesAsync();
        }
    }
}