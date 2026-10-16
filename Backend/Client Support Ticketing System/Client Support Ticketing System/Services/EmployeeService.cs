using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.Data.Enums;
using ClientSupportTicketingSystem.Data.Repositories;
using ClientSupportTicketingSystem.Data.UnitOfWork;
using ClientSupportTicketingSystem.DTOs.Employees;

namespace ClientSupportTicketingSystem.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRepository _userRepository;

        public EmployeeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _userRepository = unitOfWork.Users;
        }

        public async Task<List<EmployeeListResponse>> GetEmployeesAsync()
        {
            var employees = await _userRepository.GetEmployeesAsync();

            return employees
                .Select(employee => new EmployeeListResponse
                {
                    Id = employee.Id,
                    FullName = employee.FullName,
                    Username = employee.Username,
                    MobileNumber = employee.MobileNumber,
                    Email = employee.Email,
                    IsActive = employee.IsActive
                })
                .ToList();
        }

        public async Task<EmployeeDetailsResponse?> GetEmployeeByIdAsync(
            int id)
        {
            var employee = await _userRepository.GetByIdAsync(id);

            if (employee == null ||
                employee.Role != UserRole.SupportEmployee)
            {
                return null;
            }

            return new EmployeeDetailsResponse
            {
                Id = employee.Id,
                FullName = employee.FullName,
                Username = employee.Username,
                MobileNumber = employee.MobileNumber,
                Email = employee.Email,
                DateOfBirth = employee.DateOfBirth,
                Address = employee.Address,
                ImagePath = employee.ImagePath,
                IsActive = employee.IsActive
            };
        }

        public async Task<(bool Found, bool HasConflict)> UpdateEmployeeAsync(
            int id,
            string fullName,
            string username,
            string mobileNumber,
            string email,
            DateTime dateOfBirth,
            string address)
        {
            var employee = await _userRepository.GetByIdAsync(id);

            if (employee == null ||
                employee.Role != UserRole.SupportEmployee)
            {
                return (false, false);
            }

            username = username.Trim();
            email = email.Trim();

            bool hasConflict =
                await _userRepository.ExistsForOtherUserAsync(
                    id,
                    username,
                    email);

            if (hasConflict)
            {
                return (true, true);
            }

            employee.FullName = fullName.Trim();
            employee.Username = username;
            employee.MobileNumber = mobileNumber.Trim();
            employee.Email = email;
            employee.DateOfBirth = dateOfBirth.Date;
            employee.Address = address.Trim();

            await _unitOfWork.SaveChangesAsync();

            return (true, false);
        }

        public async Task<bool> SetEmployeeActiveStatusAsync(
            int id,
            bool isActive)
        {
            var employee = await _userRepository.GetByIdAsync(id);

            if (employee == null ||
                employee.Role != UserRole.SupportEmployee)
            {
                return false;
            }

            employee.IsActive = isActive;

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<(bool Found, string? PreviousImagePath)>
            UpdateEmployeeImageAsync(
                int id,
                string imagePath)
        {
            var employee = await _userRepository.GetByIdAsync(id);

            if (employee == null ||
                employee.Role != UserRole.SupportEmployee)
            {
                return (false, null);
            }

            string? previousImagePath = employee.ImagePath;

            employee.ImagePath = imagePath;

            await _unitOfWork.SaveChangesAsync();

            return (true, previousImagePath);
        }
    }
}