using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Enums;
using ClientSupportTicketingSystem.Data.UnitOfWork;
using ClientSupportTicketingSystem.DTOs.Clients;
using ClientSupportTicketingSystem.DTOs.Common;
using ClientSupportTicketingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClientSupportTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/clients")]
    [Authorize(Roles = nameof(UserRole.SupportManager))]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly IUserImageService _userImageService;
        private readonly ILogger<ClientsController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public ClientsController(IClientService clientService, IUserImageService userImageService, ILogger<ClientsController> logger, IUnitOfWork unitOfWork)
        {
            _clientService = clientService;
            _userImageService = userImageService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PageQuery query)
        {
            var users = _unitOfWork.Users.Query().Where(u => u.Role == UserRole.Client);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.Trim();
                users = users.Where(u => u.FullName.Contains(search) || u.Username.Contains(search) || u.Email.Contains(search));
            }
            var tickets = _unitOfWork.Tickets.Query();
            var result = await users.OrderBy(u => u.FullName).ThenBy(u => u.Id).Select(u => new ClientListResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Username = u.Username,
                MobileNumber = u.MobileNumber,
                Email = u.Email,
                IsActive = u.IsActive,
                TicketCount = tickets.Count(t => t.ClientId == u.Id)
            }).ToPageAsync(query);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest("Client id must be greater than zero.");
            try
            {
                var client = await _clientService.GetClientByIdAsync(id);
                if (client == null) return NotFound("External client not found.");
                return Ok(client);
            }
            catch (Exception exception)
            {
                return UnexpectedError(exception, "details", id);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateClientRequest request)
        {
            if (id <= 0) return BadRequest("Client id must be greater than zero.");
            if (request.DateOfBirth!.Value.Date > DateTime.Today) return BadRequest("Date of birth cannot be in the future.");
            try
            {
                var result = await _clientService.UpdateClientAsync(id, request);
                if (!result.Found) return NotFound("External client not found.");
                if (result.HasConflict) return Conflict("Username or email is already registered.");
                return Ok(new { Message = "Client updated successfully." });
            }
            catch (DbUpdateException exception) when (exception.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            {
                return Conflict("Username or email is already registered.");
            }
            catch (Exception exception)
            {
                return UnexpectedError(exception, "update", id);
            }
        }

        [HttpPatch("{id:int}/active-status")]
        public async Task<IActionResult> UpdateActiveStatus(int id, [FromForm] bool? isActive)
        {
            if (id <= 0) return BadRequest("Client id must be greater than zero.");
            if (!isActive.HasValue) return BadRequest("Active status is required.");
            try
            {
                bool found = await _clientService.SetClientActiveStatusAsync(id, isActive.Value);
                if (!found) return NotFound("External client not found.");
                return Ok(new
                {
                    Id = id,
                    IsActive = isActive.Value,
                    Message = isActive.Value ? "Client activated successfully." : "Client deactivated successfully."
                });
            }
            catch (Exception exception)
            {
                return UnexpectedError(exception, "active status", id);
            }
        }

        [HttpPut("{id:int}/image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateImage(int id, IFormFile? image)
        {
            if (id <= 0) return BadRequest("Client id must be greater than zero.");
            if (image == null) return BadRequest("Image is required.");
            string? newImagePath = null;
            bool saved = false;
            try
            {
                if (await _clientService.GetClientByIdAsync(id) == null) return NotFound("External client not found.");
                try
                {
                    newImagePath = await _userImageService.SaveAsync(image);
                }
                catch (InvalidDataException exception)
                {
                    return BadRequest(exception.Message);
                }
                var result = await _clientService.UpdateClientImageAsync(id, newImagePath);
                if (!result.Found) return NotFound("External client not found.");
                saved = true;
                TryDeleteImage(result.PreviousImagePath);
                return Ok(new { Id = id, ImagePath = newImagePath, Message = "Client image updated successfully." });
            }
            catch (Exception exception)
            {
                return UnexpectedError(exception, "image", id);
            }
            finally
            {
                if (!saved) TryDeleteImage(newImagePath);
            }
        }

        private void TryDeleteImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;
            try
            {
                _userImageService.Delete(imagePath);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to delete client image {ImagePath}.", imagePath);
            }
        }

        private ObjectResult UnexpectedError(Exception exception, string operation, int? clientId)
        {
            _logger.LogError(exception, "Client operation {Operation} failed. ClientId: {ClientId}. TraceId: {TraceId}", operation, clientId, HttpContext.TraceIdentifier);
            return StatusCode(500, new { StatusCode = 500, Message = "An unexpected error occurred. Please try again later.", TraceId = HttpContext.TraceIdentifier });
        }
    }
}