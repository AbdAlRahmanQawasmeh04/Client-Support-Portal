namespace ClientSupportTicketingSystem.DTOs.Clients
{
    public class ClientListResponse
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string MobileNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int TicketCount { get; set; }
    }
}