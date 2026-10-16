using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Enums;
using ClientSupportTicketingSystem.Data.Models;
using ClientSupportTicketingSystem.Data.Repositories;
using ClientSupportTicketingSystem.Data.UnitOfWork;
using ClientSupportTicketingSystem.DTOs.Common;
using ClientSupportTicketingSystem.DTOs.Employees;
using ClientSupportTicketingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ClientSupportTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/employees")]
    [Authorize(Roles = nameof(UserRole.SupportManager))]
    public class EmployeesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IUserImageService _userImageService;
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher, IUserImageService userImageService, IEmployeeService employeeService, ILogger<EmployeesController> logger)
        {
            _unitOfWork = unitOfWork;
            _userRepository = unitOfWork.Users;
            _passwordHasher = passwordHasher;
            _userImageService = userImageService;
            _employeeService = employeeService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PageQuery query)
        {
            var users = _unitOfWork.Users.Query().Where(u => u.Role == UserRole.SupportEmployee);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.Trim();
                users = users.Where(u => u.FullName.Contains(search) || u.Username.Contains(search) || u.Email.Contains(search));
            }
            var result = await users.OrderBy(u => u.FullName).ThenBy(u => u.Id).Select(u => new EmployeeListResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Username = u.Username,
                MobileNumber = u.MobileNumber,
                Email = u.Email,
                IsActive = u.IsActive
            }).ToPageAsync(query);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest("Employee ID must be greater than zero.");
            try
            {
                var employee = await _employeeService.GetEmployeeByIdAsync(id);
                if (employee == null) return NotFound("Support employee not found.");
                return Ok(employee);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to retrieve employee with id {Id}.", id);
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromForm, Required] string fullName, [FromForm, Required, StringLength(450)] string username, [FromForm, Required, Phone] string mobileNumber, [FromForm, Required, EmailAddress, StringLength(450)] string email, [FromForm] DateTime? dateOfBirth, [FromForm, Required] string address)
        {
            if (id <= 0) return BadRequest("Employee ID must be greater than zero.");
            if (!dateOfBirth.HasValue || dateOfBirth.Value.Date > DateTime.Today) return BadRequest("Invalid date of birth.");
            try
            {
                var result = await _employeeService.UpdateEmployeeAsync(id, fullName, username, mobileNumber, email, dateOfBirth.Value, address);
                if (!result.Found) return NotFound("Support employee not found.");
                if (result.HasConflict) return Conflict("Username or email is already registered.");
                return Ok(new { Message = "Support employee updated successfully." });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to update employee with id {Id}.", id);
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpPatch("{id:int}/active-status")]
        public async Task<IActionResult> SetActiveStatus(int id, [FromForm] bool? isActive)
        {
            if (id <= 0) return BadRequest("Employee ID must be greater than zero.");
            if (!isActive.HasValue) return BadRequest("Active status is required.");
            try
            {
                bool found = await _employeeService.SetEmployeeActiveStatusAsync(id, isActive.Value);
                if (!found) return NotFound("Support employee not found.");
                return Ok(new { Id = id, IsActive = isActive.Value, Message = isActive.Value ? "Employee activated." : "Employee deactivated." });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to change active status for id {Id}.", id);
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpPut("{id:int}/image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateImage(int id, IFormFile? image)
        {
            if (id <= 0) return BadRequest("Employee ID must be greater than zero.");
            if (image == null) return BadRequest("Image is required.");
            string? newImagePath = null;
            bool databaseUpdated = false;
            try
            {
                var employee = await _employeeService.GetEmployeeByIdAsync(id);
                if (employee == null) return NotFound("Support employee not found.");
                try
                {
                    newImagePath = await _userImageService.SaveAsync(image);
                }
                catch (InvalidDataException exception)
                {
                    return BadRequest(exception.Message);
                }
                var result = await _employeeService.UpdateEmployeeImageAsync(id, newImagePath);
                if (!result.Found)
                {
                    TryDeleteEmployeeImage(newImagePath);
                    return NotFound("Support employee not found.");
                }
                databaseUpdated = true;
                if (result.PreviousImagePath != null) TryDeleteEmployeeImage(result.PreviousImagePath);
                return Ok(new { Id = id, ImagePath = newImagePath, Message = "Image updated successfully." });
            }
            catch (Exception exception)
            {
                if (!databaseUpdated && newImagePath != null) TryDeleteEmployeeImage(newImagePath);
                _logger.LogError(exception, "Failed to update image for id {Id}.", id);
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateEmployeeRequest request)
        {
            try
            {
                string username = request.Username?.Trim() ?? string.Empty;
                string email = request.Email?.Trim() ?? string.Empty;
                if (await _userRepository.ExistsAsync(username, email)) return Conflict("Username or email is already registered.");
                if (!request.DateOfBirth.HasValue || request.DateOfBirth.Value.Date > DateTime.Today) return BadRequest("Invalid date of birth.");
                var employee = new User
                {
                    FullName = request.FullName?.Trim() ?? string.Empty,
                    Username = username,
                    MobileNumber = request.MobileNumber?.Trim() ?? string.Empty,
                    Email = email,
                    DateOfBirth = request.DateOfBirth.Value.Date,
                    Address = request.Address?.Trim() ?? string.Empty,
                    Role = UserRole.SupportEmployee,
                    IsActive = true
                };
                employee.PasswordHash = _passwordHasher.HashPassword(employee, request.Password);
                if (request.Image != null)
                {
                    try
                    {
                        employee.ImagePath = await _userImageService.SaveAsync(request.Image);
                    }
                    catch (InvalidDataException exception)
                    {
                        return BadRequest(exception.Message);
                    }
                }
                try
                {
                    await _userRepository.AddAsync(employee);
                    await _unitOfWork.SaveChangesAsync();
                }
                catch
                {
                    TryDeleteEmployeeImage(employee.ImagePath);
                    throw;
                }
                return StatusCode(201, new { employee.Id, employee.FullName, Role = employee.Role.ToString(), employee.IsActive, Message = "Support employee created successfully." });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to create support employee.");
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        private void TryDeleteEmployeeImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;
            try
            {
                _userImageService.Delete(imagePath);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to delete image {ImagePath}.", imagePath);
            }
        }
    }
}