using System.Collections.Generic;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Models;

namespace ClientSupportTicketingSystem.Data.Repositories
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<List<Product>> GetOrderedAsync();
    }
}