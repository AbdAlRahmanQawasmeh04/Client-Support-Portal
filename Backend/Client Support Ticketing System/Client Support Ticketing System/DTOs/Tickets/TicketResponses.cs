namespace ClientSupportTicketingSystem.DTOs.Tickets
{
    public class TicketSummaryResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;

        public int? AssignedEmployeeId { get; set; }
        public string? AssignedEmployeeName { get; set; }

        public string ProblemDescription { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public bool IsConfirmedByClient { get; set; }
        public bool IsResolvedByEmployee { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    public class TicketDetailsResponse : TicketSummaryResponse
    {
        public DateTime? AssignedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public List<AttachmentResponse> Attachments { get; set; } = new();
    }

    public class AttachmentResponse
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
    }

    public class CommentResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public int? ParentCommentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}