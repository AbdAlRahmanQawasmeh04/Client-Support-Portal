using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ClientSupportTicketingSystem.DTOs.Tickets
{
    public class CreateTicketRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Required, StringLength(5000)]
        public string ProblemDescription { get; set; } = string.Empty;

        public List<IFormFile> Attachments { get; set; } = new();
    }
}