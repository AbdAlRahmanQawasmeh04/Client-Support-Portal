using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using ClientSupportTicketingSystem.Data.Enums;
using ClientSupportTicketingSystem.Data.Models;
using ClientSupportTicketingSystem.Data.Repositories;
using ClientSupportTicketingSystem.Data.UnitOfWork;
using ClientSupportTicketingSystem.DTOs.Auth;
using ClientSupportTicketingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace ClientSupportTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IUserImageService _userImageService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IWebHostEnvironment _environment;

        public AuthController(IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher, IUserImageService userImageService, IJwtTokenService jwtTokenService, IWebHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _userRepository = unitOfWork.Users;
            _passwordHasher = passwordHasher;
            _userImageService = userImageService;
            _jwtTokenService = jwtTokenService;
            _environment = environment;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterClientRequest request)
        {
            string username = request.Username.Trim();
            string email = request.Email.Trim();
            bool userExists = await _userRepository.ExistsAsync(username, email);
            if (userExists) return Conflict("Username or email is already registered.");
            if (request.DateOfBirth!.Value.Date > DateTime.Today) return BadRequest("Date of birth cannot be in the future.");

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Username = username,
                MobileNumber = request.MobileNumber.Trim(),
                Email = email,
                DateOfBirth = request.DateOfBirth.Value.Date,
                Address = request.Address.Trim(),
                Role = UserRole.Client,
                IsActive = true
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            if (request.Image != null)
            {
                try
                {
                    user.ImagePath = await _userImageService.SaveAsync(request.Image);
                }
                catch (InvalidDataException exception)
                {
                    return BadRequest(exception.Message);
                }
            }

            try
            {
                await _userRepository.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                if (user.ImagePath != null) _userImageService.Delete(user.ImagePath);
                throw;
            }

            return StatusCode(201, new { user.Id, Message = "Client registered successfully." });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            string usernameOrEmail = request.UsernameOrEmail.Trim();
            var user = await _userRepository.GetByUsernameOrEmailAsync(usernameOrEmail);
            if (user == null) return Unauthorized("Invalid login credentials.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed) return Unauthorized("Invalid login credentials.");

            if (!user.IsActive) return StatusCode(403, new { Message = "Your account is deactivated." });

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
                await _unitOfWork.SaveChangesAsync();
            }

            string token = _jwtTokenService.CreateToken(user);

            return Ok(new
            {
                AccessToken = token,
                TokenType = "Bearer",
                UserId = user.Id,
                FullName = user.FullName,
                Role = user.Role.ToString()
            });
        }

        [Authorize]
        [HttpGet("~/api/users/{id:int}/image")]
        public async Task<IActionResult> GetImage(int id)
        {
            if (id <= 0) return BadRequest("Invalid user id.");

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId) || userId <= 0)
            {
                return Unauthorized();
            }

            if (userId != id && !User.IsInRole(nameof(UserRole.SupportManager))) return NotFound();

            var person = await _userRepository.GetByIdAsync(id);
            string? name = person?.ImagePath;

            if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name) return NotFound();

            string extension = Path.GetExtension(name).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png") return NotFound();

            Stream stream;
            try
            {
                stream = new FileStream(Path.Combine(_environment.ContentRootPath, "App_Data", "UserImages", name), FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
            catch (DirectoryNotFoundException)
            {
                return NotFound();
            }

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Cache-Control"] = "no-store";

            return File(stream, extension == ".png" ? "image/png" : "image/jpeg");
        }
    }
}