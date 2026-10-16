namespace ClientSupportTicketingSystem.Data.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public System.DateTime CreatedAt { get; set; }
            = System.DateTime.UtcNow;

        public System.DateTime? ReadAt { get; set; }
    }
}