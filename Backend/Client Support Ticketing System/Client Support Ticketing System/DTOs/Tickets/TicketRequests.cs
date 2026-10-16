using System.ComponentModel.DataAnnotations;
using ClientSupportTicketingSystem.Data.Enums;
using ClientSupportTicketingSystem.DTOs.Common;

namespace ClientSupportTicketingSystem.DTOs.Tickets
{
    public class TicketQuery : PageQuery
    {
        [EnumDataType(typeof(TicketStatus))]
        public TicketStatus? Status { get; set; }

        [Range(1, int.MaxValue)]
        public int? ClientId { get; set; }

        [Range(1, int.MaxValue)]
        public int? EmployeeId { get; set; }

        [Range(1, int.MaxValue)]
        public int? ProductId { get; set; }
    }

    public class UpdateTicketRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Required, StringLength(5000)]
        public string ProblemDescription { get; set; } = string.Empty;
    }

    public class AssignTicketRequest
    {
        [Range(1, int.MaxValue)]
        public int EmployeeId { get; set; }
    }

    public class AddCommentRequest
    {
        [Required, StringLength(5000)]
        public string Content { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int? ParentCommentId { get; set; }
    }

    public class TicketStatusRequest
    {
        [Required, EnumDataType(typeof(TicketStatus))]
        public TicketStatus? Status { get; set; }
    }
}