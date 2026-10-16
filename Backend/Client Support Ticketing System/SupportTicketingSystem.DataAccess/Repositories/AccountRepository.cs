using ClientSupportTicketingSystem.Data.Context;
using ClientSupportTicketingSystem.Data.Enums;
using ClientSupportTicketingSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace ClientSupportTicketingSystem.Data.Repositories;

public interface IAccountRepository
{
    Task<User?> GetAsync(int id);
    Task<bool> SaveProfileAsync(int id, string name, string phone, string address);
    Task<bool> ReplaceImageAsync(User expected, string? image);
    Task<bool> EmailExistsAsync(string email, int exceptId);
    Task<bool> ConfirmEmailAsync(User expected, string email, string newPasswordHash);
}

public sealed class AccountRepository : IAccountRepository
{
    private readonly AppDbContext db;
    public AccountRepository(AppDbContext db) => this.db = db;
    public Task<User?> GetAsync(int id) => db.Users.AsNoTracking().SingleOrDefaultAsync(
        u => u.Id == id && u.IsActive && u.Role == UserRole.Client);
    public async Task<bool> SaveProfileAsync(int id, string name, string phone, string address) =>
        await db.Database.ExecuteSqlInterpolatedAsync($@"UPDATE [Users]
            SET [FullName]={name}, [MobileNumber]={phone}, [Address]={address}
            WHERE [Id]={id} AND [IsActive]=1 AND [Role]={(int)UserRole.Client}") == 1;
    public async Task<bool> ReplaceImageAsync(User expected, string? image) =>
        await db.Database.ExecuteSqlInterpolatedAsync($@"UPDATE [Users] SET [ImagePath]={image}
            WHERE [Id]={expected.Id} AND [IsActive]=1 AND [Role]={(int)UserRole.Client}
            AND (([ImagePath] IS NULL AND {expected.ImagePath} IS NULL) OR [ImagePath]={expected.ImagePath})") == 1;
    public Task<bool> EmailExistsAsync(string email, int exceptId) => db.Users.AnyAsync(u => u.Id != exceptId && u.Email == email);
    public async Task<bool> ConfirmEmailAsync(User expected, string email, string newPasswordHash)
    {
        try
        {
            return await db.Database.ExecuteSqlInterpolatedAsync($@"UPDATE [Users]
                SET [Email]={email}, [PasswordHash]={newPasswordHash}
                WHERE [Id]={expected.Id} AND [IsActive]=1 AND [Role]={(int)UserRole.Client}
                AND [Email]={expected.Email}
                AND [PasswordHash] COLLATE Latin1_General_100_BIN2={expected.PasswordHash}") == 1;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627) { return false; }
    }
}
