using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Context;
using ClientSupportTicketingSystem.Data.Repositories;

namespace ClientSupportTicketingSystem.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IUserRepository Users { get; }
        public ITicketRepository Tickets { get; }
        public IProductRepository Products { get; }
        public INotificationRepository Notifications { get; }

        public UnitOfWork(
            AppDbContext context,
            IUserRepository users,
            ITicketRepository tickets,
            IProductRepository products,
            INotificationRepository notifications)
        {
            _context = context;
            Users = users;
            Tickets = tickets;
            Products = products;
            Notifications = notifications;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}