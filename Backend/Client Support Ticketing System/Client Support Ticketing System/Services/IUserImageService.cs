using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ClientSupportTicketingSystem.Services
{
    public interface IUserImageService
    {
        Task<string> SaveAsync(IFormFile image);

        void Delete(string fileName);
    }
}