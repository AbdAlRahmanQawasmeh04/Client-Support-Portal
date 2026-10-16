using System.Collections.Generic;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.DTOs.Clients;

namespace ClientSupportTicketingSystem.Services
{
    public interface IClientService
    {
        Task<List<ClientListResponse>> GetClientsAsync();

        Task<ClientDetailsResponse?> GetClientByIdAsync(int id);

        Task<(bool Found, bool HasConflict)> UpdateClientAsync(
            int id, UpdateClientRequest request);

        Task<bool> SetClientActiveStatusAsync(int id, bool isActive);

        Task<(bool Found, string? PreviousImagePath)> UpdateClientImageAsync(
            int id, string imagePath);
    }
}