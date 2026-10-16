using System;

namespace ClientSupportTicketingSystem.DTOs.Clients
{
    public class ClientDetailsResponse
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string MobileNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string Address { get; set; } = string.Empty;

        public string? ImagePath { get; set; }

        public bool IsActive { get; set; }
    }
}