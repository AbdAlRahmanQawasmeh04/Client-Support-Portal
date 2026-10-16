using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Enums;
using ClientSupportTicketingSystem.Data.UnitOfWork;
using ClientSupportTicketingSystem.DTOs.Clients;

namespace ClientSupportTicketingSystem.Services
{
    public class ClientService : IClientService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ClientService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ClientListResponse>> GetClientsAsync()
        {
            var clients = await _unitOfWork.Users.GetClientsWithTicketCountsAsync();

            return clients.Select(item => new ClientListResponse
            {
                Id = item.Client.Id,
                FullName = item.Client.FullName,
                Username = item.Client.Username,
                MobileNumber = item.Client.MobileNumber,
                Email = item.Client.Email,
                IsActive = item.Client.IsActive,
                TicketCount = item.TicketCount
            }).ToList();
        }

        public async Task<ClientDetailsResponse?> GetClientByIdAsync(int id)
        {
            var client = await _unitOfWork.Users.GetByIdAsync(id);

            if (client == null || client.Role != UserRole.Client)
                return null;

            return new ClientDetailsResponse
            {
                Id = client.Id,
                FullName = client.FullName,
                Username = client.Username,
                MobileNumber = client.MobileNumber,
                Email = client.Email,
                DateOfBirth = client.DateOfBirth,
                Address = client.Address,
                ImagePath = client.ImagePath,
                IsActive = client.IsActive
            };
        }

        public async Task<(bool Found, bool HasConflict)> UpdateClientAsync(
            int id, UpdateClientRequest request)
        {
            var client = await _unitOfWork.Users.GetByIdAsync(id);

            if (client == null || client.Role != UserRole.Client)
                return (false, false);

            string username = request.Username.Trim();
            string email = request.Email.Trim();

            if (await _unitOfWork.Users.ExistsForOtherUserAsync(
                id, username, email))
            {
                return (true, true);
            }

            client.FullName = request.FullName.Trim();
            client.Username = username;
            client.MobileNumber = request.MobileNumber.Trim();
            client.Email = email;
            client.DateOfBirth = request.DateOfBirth!.Value.Date;
            client.Address = request.Address.Trim();

            await _unitOfWork.SaveChangesAsync();

            return (true, false);
        }

        public async Task<bool> SetClientActiveStatusAsync(
            int id, bool isActive)
        {
            var client = await _unitOfWork.Users.GetByIdAsync(id);

            if (client == null || client.Role != UserRole.Client)
                return false;

            client.IsActive = isActive;

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<(bool Found, string? PreviousImagePath)>
            UpdateClientImageAsync(int id, string imagePath)
        {
            var client = await _unitOfWork.Users.GetByIdAsync(id);

            if (client == null || client.Role != UserRole.Client)
                return (false, null);

            string? previousImagePath = client.ImagePath;
            client.ImagePath = imagePath;

            await _unitOfWork.SaveChangesAsync();

            return (true, previousImagePath);
        }
    }
}