namespace ClientSupportTicketingSystem.Data.Models
{
    public class TicketAttachment
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;
    }
}