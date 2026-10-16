using System;
using System.ComponentModel.DataAnnotations;

namespace ClientSupportTicketingSystem.DTOs.Clients
{
    public class UpdateClientRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(450)]
        public string Username { get; set; } = string.Empty;

        [Required, Phone]
        public string MobileNumber { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(450)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;
    }
}