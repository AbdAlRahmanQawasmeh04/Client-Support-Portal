using ClientSupportTicketingSystem.Data.Context;
using ClientSupportTicketingSystem.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientSupportTicketingSystem.Data.Repositories;

public interface IPasswordRecoveryRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByIdAsync(int id);
    Task<bool> ReplacePasswordAsync(User user, string expectedHash, string newHash);
}

public class PasswordRecoveryRepository : IPasswordRecoveryRepository
{
    private readonly AppDbContext database;
    public PasswordRecoveryRepository(AppDbContext database) => this.database = database;
    public Task<User?> FindByEmailAsync(string email) => database.Users.AsNoTracking()
        .SingleOrDefaultAsync(u => u.Email == email && u.IsActive);
    public Task<User?> FindByIdAsync(int id) => database.Users.AsNoTracking()
        .SingleOrDefaultAsync(u => u.Id == id && u.IsActive);

    public async Task<bool> ReplacePasswordAsync(User user, string expectedHash, string newHash)
    {
        // A conditional, atomic update makes simultaneous uses of a link single-use.
        int changed = await database.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE [Users] SET [PasswordHash] = {newHash}
            WHERE [Id] = {user.Id} AND [IsActive] = 1 AND [Email] = {user.Email}
            AND [PasswordHash] COLLATE Latin1_General_100_BIN2 = {expectedHash}");
        return changed == 1;
    }
}
