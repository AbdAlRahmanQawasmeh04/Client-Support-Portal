using System.Security.Cryptography;
using System.Text.Json;
using System.Net;
using System.Net.Mail;
using ClientSupportTicketingSystem.Data.Models;
using ClientSupportTicketingSystem.Data.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace ClientSupportTicketingSystem.Services;

public interface IAccountEmailSender
{
    Task SendConfirmationAsync(string email, string token, CancellationToken cancellation);
}

public sealed class AccountEmailSender : IAccountEmailSender
{
    private readonly RecoveryMailSettings settings;
    public AccountEmailSender(RecoveryMailSettings settings) => this.settings = settings;
    public async Task SendConfirmationAsync(string email, string token, CancellationToken cancellation)
    {
        if (!settings.IsReady) throw new InvalidOperationException("Email is not configured.");
        var link = settings.FrontendUrl.TrimEnd('/') + "/confirm-email#token=" + Uri.EscapeDataString(token);
        using var message = new MailMessage(settings.From, email)
        {
            Subject = "Confirm your Support Portal email change",
            Body = "You requested to change your Support Portal email address. Open this link, then select Confirm email change:\n\n" +
                link + "\n\nThis link expires in 15 minutes. Your email will not change until you confirm. " +
                "If you did not request this, ignore this message.", IsBodyHtml = false
        };
        using var smtp = new SmtpClient(settings.Host, settings.Port)
        { EnableSsl = true, UseDefaultCredentials = false, Credentials = new NetworkCredential(settings.Username, settings.Password) };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var registration = timeout.Token.Register(smtp.SendAsyncCancel);
        await smtp.SendMailAsync(message);
    }
}

public sealed class AccountService
{
    private readonly IAccountRepository users;
    private readonly IPasswordRecoveryRepository passwords;
    private readonly IPasswordHasher<User> hasher;
    private readonly IUserImageService images;
    private readonly IAccountEmailSender mail;
    private readonly ITimeLimitedDataProtector protector;
    private readonly ILogger<AccountService> logger;
    public AccountService(IAccountRepository users, IPasswordRecoveryRepository passwords, IPasswordHasher<User> hasher,
        IUserImageService images, IAccountEmailSender mail, IDataProtectionProvider protection, ILogger<AccountService> logger)
    {
        this.users=users; this.passwords=passwords; this.hasher=hasher; this.images=images; this.mail=mail; this.logger=logger;
        protector=protection.CreateProtector("SupportPortal.EmailChange.v1").ToTimeLimitedDataProtector();
    }
    public Task<User?> GetAsync(int id) => users.GetAsync(id);
    public Task<bool> SaveAsync(int id, string name, string phone, string address) => users.SaveProfileAsync(id,name.Trim(),phone.Trim(),address.Trim());
    public async Task<bool> PhotoAsync(int id, IFormFile? file)
    {
        var user=await users.GetAsync(id);
        if(user==null) return false;
        string? newPath=file==null ? null : await images.SaveAsync(file);
        bool saved=false;
        try { saved=await users.ReplaceImageAsync(user,newPath); }
        finally { if(!saved && newPath!=null) DeleteImage(newPath); }
        if(saved && user.ImagePath!=null) DeleteImage(user.ImagePath);
        return saved;
    }
    private void DeleteImage(string path)
    {
        try { images.Delete(path); }
        catch(Exception) { logger.LogWarning("An unused account image could not be removed."); }
    }
    public async Task<bool> PasswordAsync(int id,string current,string replacement)
    {
        var user=await users.GetAsync(id);
        if(user==null || hasher.VerifyHashedPassword(user,user.PasswordHash,current)==PasswordVerificationResult.Failed) return false;
        return await passwords.ReplacePasswordAsync(user,user.PasswordHash,hasher.HashPassword(user,replacement));
    }
    public async Task<bool> RequestEmailAsync(int id,string current,string email,CancellationToken cancellation)
    {
        var user=await users.GetAsync(id);
        if(user==null || hasher.VerifyHashedPassword(user,user.PasswordHash,current)==PasswordVerificationResult.Failed) return false;
        email=email.Trim();
        if(string.Equals(email,user.Email,StringComparison.OrdinalIgnoreCase) || await users.EmailExistsAsync(email,id)) return false;
        // Rehash the verified current password only when confirmation succeeds, revoking old sessions and links.
        var payload=new EmailChange(id,user.Email,email,PasswordRecoveryService.PasswordVersion(user.PasswordHash),hasher.HashPassword(user,current));
        string token=protector.Protect(JsonSerializer.Serialize(payload),TimeSpan.FromMinutes(15));
        await mail.SendConfirmationAsync(email,token,cancellation);
        return true;
    }
    public async Task<bool> ConfirmEmailAsync(string token)
    {
        EmailChange? payload;
        try { payload=JsonSerializer.Deserialize<EmailChange>(protector.Unprotect(token)); }
        catch(Exception error) when(error is CryptographicException or JsonException or FormatException) { return false; }
        if(payload==null) return false;
        var user=await users.GetAsync(payload.Id);
        if(user==null || user.Email!=payload.OldEmail || PasswordRecoveryService.PasswordVersion(user.PasswordHash)!=payload.Version ||
            await users.EmailExistsAsync(payload.NewEmail,user.Id)) return false;
        return await users.ConfirmEmailAsync(user,payload.NewEmail,payload.NewPasswordHash);
    }
    public record EmailChange(int Id,string OldEmail,string NewEmail,string Version,string NewPasswordHash);
}
