using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Models;
using Microsoft.AspNetCore.Http;

namespace ClientSupportTicketingSystem.Services
{
    public interface ITicketAttachmentService
    {
        Task<List<TicketAttachment>> SaveAsync(IReadOnlyList<IFormFile> files);

        Stream? OpenRead(string storedName);

        void DeleteFiles(IEnumerable<TicketAttachment> attachments);
    }
}