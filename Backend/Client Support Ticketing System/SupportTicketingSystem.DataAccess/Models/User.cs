using System;
using ClientSupportTicketingSystem.Data.Enums;

namespace ClientSupportTicketingSystem.Data.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public UserRole Role { get; set; }
    }
}