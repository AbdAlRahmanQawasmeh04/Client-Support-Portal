using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Models;

namespace ClientSupportTicketingSystem.Data.Repositories
{
    public interface ITicketRepository : IGenericRepository<Ticket>
    {
        Task<List<Ticket>> GetByClientIdAsync(int clientId);

        Task<Ticket?> GetDetailsAsync(int id);

        IQueryable<Comment> Comments(int ticketId);
    }
}