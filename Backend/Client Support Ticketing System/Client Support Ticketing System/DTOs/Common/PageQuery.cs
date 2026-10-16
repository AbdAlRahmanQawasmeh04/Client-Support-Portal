using System.ComponentModel.DataAnnotations;

namespace ClientSupportTicketingSystem.DTOs.Common
{
    public class PageQuery
    {
        [Range(1, 1000000)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        [StringLength(200)]
        public string? Search { get; set; }
    }

    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();

        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}