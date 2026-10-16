using System.ComponentModel.DataAnnotations;
using ClientSupportTicketingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientSupportTicketingSystem.Controllers;

[ApiController, AllowAnonymous, Route("api/auth")]
public sealed class PasswordRecoveryController : ControllerBase
{
    private readonly RecoveryMailSettings settings;
    private readonly RecoveryLimits limits;
    private readonly RecoveryQueue queue;
    private readonly PasswordRecoveryService recovery;
    public PasswordRecoveryController(RecoveryMailSettings settings, RecoveryLimits limits,
        RecoveryQueue queue, PasswordRecoveryService recovery)
    { this.settings = settings; this.limits = limits; this.queue = queue; this.recovery = recovery; }

    [HttpPost("forgot-password")]
    public IActionResult Forgot(ForgotPasswordRequest request)
    {
        Response.Headers["Cache-Control"] = "no-store";
        if (!limits.Allow("forgot-global", 100) || !limits.Allow("forgot-ip:" + HttpContext.Connection.RemoteIpAddress, 10))
            return StatusCode(429, new { message = "Too many requests. Try again in 15 minutes." });
        if (!settings.IsReady) return StatusCode(503, new { message = "Email recovery is temporarily unavailable. Contact your support manager." });
        string email = request.Email.Trim();
        if (limits.Allow("forgot-email:" + email.ToUpperInvariant(), 3) && !queue.Enqueue(email))
            return StatusCode(503, new { message = "Email recovery is busy. Please try again later." });
        return Ok(new { message = "If an active account matches this email, a reset link will be sent. Check your inbox and spam folder." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> Reset(ResetPasswordRequest request)
    {
        Response.Headers["Cache-Control"] = "no-store";
        if (!limits.Allow("reset-global", 200) || !limits.Allow("reset-ip:" + HttpContext.Connection.RemoteIpAddress, 20))
            return StatusCode(429, new { message = "Too many attempts. Try again in 15 minutes." });
        if (!await recovery.ResetAsync(request.Token, request.Password))
            return BadRequest(new { message = "This reset link is invalid, expired or already used. Request a new link." });
        return Ok(new { message = "Your password has been changed. Sign in with your new password." });
    }
}

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
}
public sealed class ResetPasswordRequest
{
    [Required, StringLength(4096)] public string Token { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";
}
