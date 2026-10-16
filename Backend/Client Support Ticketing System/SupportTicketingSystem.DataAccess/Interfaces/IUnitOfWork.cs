using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Repositories;

namespace ClientSupportTicketingSystem.Data.UnitOfWork
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }

        ITicketRepository Tickets { get; }

        IProductRepository Products { get; }

        INotificationRepository Notifications { get; }

        Task<int> SaveChangesAsync();
    }
}