using System;
using System.Linq;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace ClientSupportTicketingSystem.Services
{
    public static class Paging
    {
        public static async Task<PagedResponse<T>> ToPageAsync<T>(this IQueryable<T> query, PageQuery page)
        {
            if (page.PageNumber < 1 || page.PageNumber > 1000000 || page.PageSize < 1 || page.PageSize > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "Invalid page number or size.");
            }

            return new PagedResponse<T>
            {
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = await query.CountAsync(),
                Items = await query.Skip((page.PageNumber - 1) * page.PageSize).Take(page.PageSize).ToListAsync()
            };
        }
    }
}