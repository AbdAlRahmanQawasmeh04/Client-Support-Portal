using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Context;
using ClientSupportTicketingSystem.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientSupportTicketingSystem.Data.Repositories
{
    public class TicketRepository
        : GenericRepository<Ticket>, ITicketRepository
    {
        public TicketRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<Ticket?> GetDetailsAsync(int id)
        {
            return await _dbSet
                .Include(t => t.Product)
                .Include(t => t.Client)
                .Include(t => t.AssignedEmployee)
                .Include(t => t.Attachments)
                .SingleOrDefaultAsync(t => t.Id == id);
        }

        public IQueryable<Comment> Comments(int ticketId)
        {
            return _context.Comments
                .AsNoTracking()
                .Where(c => c.TicketId == ticketId);
        }

        public async Task<List<Ticket>> GetByClientIdAsync(int clientId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(t => t.ClientId == clientId)
                .ToListAsync();
        }
    }
}