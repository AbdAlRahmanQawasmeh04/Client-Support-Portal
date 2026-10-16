using ClientSupportTicketingSystem.Data.Context;
using ClientSupportTicketingSystem.Data.Models;

namespace ClientSupportTicketingSystem.Data.Repositories
{
    public class NotificationRepository
        : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(AppDbContext context)
            : base(context)
        {
        }
    }
}