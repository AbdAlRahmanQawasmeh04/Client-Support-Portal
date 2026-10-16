using System.Collections.Generic;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Models;

namespace ClientSupportTicketingSystem.Data.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<bool> ExistsAsync(
            string username,
            string email);

        Task<User?> GetByUsernameOrEmailAsync(
            string usernameOrEmail);

        Task<List<User>> GetEmployeesAsync();

        Task<bool> ExistsForOtherUserAsync(
            int userId,
            string username,
            string email);

        Task<List<(User Client, int TicketCount)>>
            GetClientsWithTicketCountsAsync();
    }
}