namespace ClientSupportTicketingSystem.Data.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public System.DateTime CreatedAt { get; set; }
            = System.DateTime.UtcNow;

        public int? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }

        public string Content { get; set; } = string.Empty;

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}