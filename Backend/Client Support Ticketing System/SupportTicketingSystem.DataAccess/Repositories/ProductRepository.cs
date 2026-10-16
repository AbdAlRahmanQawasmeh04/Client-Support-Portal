using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Context;
using ClientSupportTicketingSystem.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientSupportTicketingSystem.Data.Repositories
{
    public class ProductRepository
        : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<Product>> GetOrderedAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .OrderBy(product => product.Name)
                .ThenBy(product => product.Id)
                .ToListAsync();
        }
    }
}