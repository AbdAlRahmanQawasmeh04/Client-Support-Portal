namespace ClientSupportTicketingSystem.DTOs.Tickets
{
    public class TicketListResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProblemDescription { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? AssignedEmployeeId { get; set; }
        public bool IsConfirmedByClient { get; set; }
    }
}