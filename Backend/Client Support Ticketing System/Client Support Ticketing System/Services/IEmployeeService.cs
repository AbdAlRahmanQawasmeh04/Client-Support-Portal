using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClientSupportTicketingSystem.DTOs.Employees;

namespace ClientSupportTicketingSystem.Services
{
    public interface IEmployeeService
    {
        Task<List<EmployeeListResponse>> GetEmployeesAsync();

        Task<EmployeeDetailsResponse?> GetEmployeeByIdAsync(int id);

        Task<(bool Found, bool HasConflict)> UpdateEmployeeAsync(
            int id,
            string fullName,
            string username,
            string mobileNumber,
            string email,
            DateTime dateOfBirth,
            string address);
        Task<bool> SetEmployeeActiveStatusAsync(
            int id,
            bool isActive);

        Task<(bool Found, string? PreviousImagePath)>
            UpdateEmployeeImageAsync(
                int id,
                string imagePath);
    }
}