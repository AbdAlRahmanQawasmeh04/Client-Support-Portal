using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Context;
using ClientSupportTicketingSystem.Data.Enums;
using ClientSupportTicketingSystem.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientSupportTicketingSystem.Data.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<bool> ExistsAsync(
            string username,
            string email)
        {
            return await _dbSet.AnyAsync(user =>
                user.Username == username ||
                user.Email == email);
        }

        public async Task<User?> GetByUsernameOrEmailAsync(
            string usernameOrEmail)
        {
            return await _dbSet.FirstOrDefaultAsync(user =>
                user.Username == usernameOrEmail ||
                user.Email == usernameOrEmail);
        }

        public async Task<List<User>> GetEmployeesAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(user => user.Role == UserRole.SupportEmployee)
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.Id)
                .ToListAsync();
        }

        public async Task<bool> ExistsForOtherUserAsync(
            int userId,
            string username,
            string email)
        {
            return await _context.Users.AnyAsync(user =>
                user.Id != userId &&
                (user.Username == username ||
                 user.Email == email));
        }

        public async Task<List<(User Client, int TicketCount)>>
            GetClientsWithTicketCountsAsync()
        {
            var clients = await _dbSet
                .AsNoTracking()
                .Where(user => user.Role == UserRole.Client)
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.Id)
                .Select(user => new
                {
                    Client = user,
                    TicketCount = _context.Tickets.Count(
                        ticket => ticket.ClientId == user.Id)
                })
                .ToListAsync();

            return clients
                .Select(item => (
                    Client: item.Client,
                    TicketCount: item.TicketCount))
                .ToList();
        }
    }
}