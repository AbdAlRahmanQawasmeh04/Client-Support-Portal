using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using ClientSupportTicketingSystem.Data.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace ClientSupportTicketingSystem.Services;

public sealed class RecoveryMailSettings
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
    public string FrontendUrl { get; set; } = "http://localhost:4200";
    public bool IsReady => Enabled && !string.IsNullOrWhiteSpace(Host) && Port > 0 && Port <= 65535 &&
        !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password) &&
        MailAddress.TryCreate(From, out _) && Uri.TryCreate(FrontendUrl, UriKind.Absolute, out var uri) &&
        (uri.Scheme == "https" || (uri.Scheme == "http" && uri.IsLoopback)) &&
        string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
}

public sealed class RecoveryQueue
{
    private readonly Channel<string> channel = Channel.CreateBounded<string>(100);
    public bool Enqueue(string email) => channel.Writer.TryWrite(email);
    public IAsyncEnumerable<string> ReadAsync(CancellationToken token) => channel.Reader.ReadAllAsync(token);
}

public sealed class RecoveryLimits : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 10000 });
    private readonly object gate = new();
    public bool Allow(string key, int limit)
    {
        key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        lock (gate)
        {
            if (!cache.TryGetValue(key, out Counter? counter))
            {
                counter = new Counter();
                cache.Set(key, counter, new MemoryCacheEntryOptions().SetSize(1)
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(15)));
            }
            return ++counter!.Count <= limit;
        }
    }
    public void Dispose() => cache.Dispose();
    private sealed class Counter { public int Count; }
}

public sealed class RecoveryMailWorker : BackgroundService
{
    private readonly RecoveryQueue queue;
    private readonly IServiceScopeFactory scopes;
    private readonly RecoveryMailSettings settings;
    private readonly ILogger<RecoveryMailWorker> logger;
    public RecoveryMailWorker(RecoveryQueue queue, IServiceScopeFactory scopes, RecoveryMailSettings settings,
        ILogger<RecoveryMailWorker> logger)
    { this.queue = queue; this.scopes = scopes; this.settings = settings; this.logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in queue.ReadAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var user = await scope.ServiceProvider.GetRequiredService<IPasswordRecoveryRepository>().FindByEmailAsync(email);
                if (user == null) continue;
                string token = scope.ServiceProvider.GetRequiredService<PasswordRecoveryService>().CreateToken(user);
                // The fragment avoids putting the token into HTTP request URLs and referrers.
                string link = settings.FrontendUrl.TrimEnd('/') + "/reset-password#token=" + Uri.EscapeDataString(token);
                using var message = new MailMessage(settings.From, user.Email)
                {
                    Subject = "Reset your Support Portal password",
                    Body = "A password reset was requested for your Support Portal account.\n\n" + link +
                        "\n\nThis link expires in 15 minutes and can be used once. " +
                        "If you did not request this, ignore this email. Your password has not changed.",
                    IsBodyHtml = false
                };
                using var smtp = new SmtpClient(settings.Host, settings.Port)
                {
                    EnableSsl = true, UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(settings.Username, settings.Password), Timeout = 15000
                };
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                using var cancellation = timeout.Token.Register(smtp.SendAsyncCancel);
                await smtp.SendMailAsync(message);
            }
            catch (Exception)
            {
                // Do not log email addresses, credentials or reset links.
                logger.LogWarning("Password recovery email could not be delivered. Check SMTP settings and connectivity.");
            }
        }
    }
}
