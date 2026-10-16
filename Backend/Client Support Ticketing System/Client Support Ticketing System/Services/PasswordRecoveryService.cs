using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClientSupportTicketingSystem.Data.Models;
using ClientSupportTicketingSystem.Data.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace ClientSupportTicketingSystem.Services;

public sealed class PasswordRecoveryService
{
    private readonly IPasswordRecoveryRepository users;
    private readonly IPasswordHasher<User> hasher;
    private readonly ITimeLimitedDataProtector protector;
    public PasswordRecoveryService(IPasswordRecoveryRepository users, IPasswordHasher<User> hasher,
        IDataProtectionProvider protection)
    {
        this.users = users;
        this.hasher = hasher;
        protector = protection.CreateProtector("SupportPortal.PasswordRecovery.v1").ToTimeLimitedDataProtector();
    }

    public static string PasswordVersion(string hash) => Convert.ToBase64String(
        SHA256.HashData(Encoding.UTF8.GetBytes(hash)));

    public string CreateToken(User user) => protector.Protect(
        JsonSerializer.Serialize(new ResetPayload(user.Id, user.Email, PasswordVersion(user.PasswordHash))),
        TimeSpan.FromMinutes(15));

    public async Task<bool> ResetAsync(string token, string password)
    {
        ResetPayload? payload;
        try { payload = JsonSerializer.Deserialize<ResetPayload>(protector.Unprotect(token)); }
        catch (Exception error) when (error is CryptographicException or JsonException or FormatException)
        { return false; }
        if (payload == null) return false;
        var user = await users.FindByIdAsync(payload.Id);
        if (user == null || user.Email != payload.Email || PasswordVersion(user.PasswordHash) != payload.Version)
            return false;
        return await users.ReplacePasswordAsync(user, user.PasswordHash, hasher.HashPassword(user, password));
    }
    public record ResetPayload(int Id, string Email, string Version);
}
