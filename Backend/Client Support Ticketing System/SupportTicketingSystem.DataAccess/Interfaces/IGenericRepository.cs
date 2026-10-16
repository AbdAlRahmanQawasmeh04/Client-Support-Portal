using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClientSupportTicketingSystem.Data.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        IQueryable<T> Query();

        Task<T?> GetByIdAsync(int id);

        Task<List<T>> GetAllAsync();

        Task AddAsync(T entity);
    }
}