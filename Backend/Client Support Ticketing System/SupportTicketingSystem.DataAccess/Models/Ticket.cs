using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ClientSupportTicketingSystem.Data.Enums;

namespace ClientSupportTicketingSystem.Data.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AssignedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public bool IsResolvedByEmployee { get; set; }
        public bool IsConfirmedByClient { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string ProblemDescription { get; set; } = string.Empty;

        public TicketStatus Status { get; set; } = TicketStatus.New;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int ClientId { get; set; }
        public User Client { get; set; } = null!;

        public int? AssignedEmployeeId { get; set; }
        public User? AssignedEmployee { get; set; }

        public ICollection<Comment> Comments { get; set; }
            = new List<Comment>();

        public ICollection<TicketAttachment> Attachments { get; set; }
            = new List<TicketAttachment>();
    }
}