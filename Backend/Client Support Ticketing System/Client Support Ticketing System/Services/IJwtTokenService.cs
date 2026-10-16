using ClientSupportTicketingSystem.Data.Models;

namespace ClientSupportTicketingSystem.Services
{
    public interface IJwtTokenService
    {
        string CreateToken(User user);
    }
}