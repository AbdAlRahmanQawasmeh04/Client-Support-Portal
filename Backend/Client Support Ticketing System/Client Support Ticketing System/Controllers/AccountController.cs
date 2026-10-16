using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ClientSupportTicketingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientSupportTicketingSystem.Controllers;

[ApiController, Route("api/account"), Authorize(Roles="Client")]
public sealed class AccountController : ControllerBase
{
    private readonly AccountService service;
    private readonly RecoveryLimits limits;
    private readonly RecoveryMailSettings mail;
    private readonly ILogger<AccountController> logger;
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id) ? id : 0;
    public AccountController(AccountService service,RecoveryLimits limits,RecoveryMailSettings mail,ILogger<AccountController> logger)
    { this.service=service; this.limits=limits; this.mail=mail; this.logger=logger; }
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        Response.Headers["Cache-Control"]="no-store";
        var user=await service.GetAsync(UserId);
        return user==null ? NotFound() : Ok(new { user.Id,user.FullName,user.Username,user.Email,user.MobileNumber,user.Address,user.DateOfBirth,hasImage=user.ImagePath!=null });
    }
    [HttpPut]
    public async Task<IActionResult> Save(ProfileRequest request) => await service.SaveAsync(UserId,request.FullName,request.MobileNumber,request.Address)
        ? Ok(new {message="Account details saved."}) : NotFound();
    [HttpPut("photo"), RequestSizeLimit(3*1024*1024)]
    public async Task<IActionResult> Photo([FromForm] PhotoRequest request)
    {
        try { return await service.PhotoAsync(UserId,request.Image) ? Ok(new {message="Photo updated."}) : Conflict(new {message="Account changed. Reload and try again."}); }
        catch(InvalidDataException error) { return BadRequest(new {message=error.Message}); }
    }
    [HttpDelete("photo")]
    public async Task<IActionResult> RemovePhoto() => await service.PhotoAsync(UserId,null) ? Ok(new {message="Photo removed."}) : NotFound();
    [HttpPost("password")]
    public async Task<IActionResult> Password(ChangePasswordRequest request)
    {
        if(!limits.Allow("account-password:"+UserId,10)) return StatusCode(429,new {message="Too many attempts. Try again in 15 minutes."});
        if(request.CurrentPassword==request.NewPassword) return BadRequest(new {message="Choose a different new password."});
        return await service.PasswordAsync(UserId,request.CurrentPassword,request.NewPassword)
            ? Ok(new {message="Password changed. Please sign in again."}) : BadRequest(new {message="Current password is incorrect or the account changed. Try again."});
    }
    [HttpPost("email")]
    public async Task<IActionResult> Email(ChangeEmailRequest request)
    {
        if(!limits.Allow("account-email:"+UserId,3) || !limits.Allow("account-email-global",100)) return StatusCode(429,new {message="Too many requests. Try again in 15 minutes."});
        if(!mail.IsReady) return StatusCode(503,new {message="Email delivery is currently unavailable."});
        try {
            return await service.RequestEmailAsync(UserId,request.CurrentPassword,request.NewEmail,HttpContext.RequestAborted)
                ? Ok(new {message="Check your new email for the confirmation link. Your current email has not changed."})
                : BadRequest(new {message="Check your current password and use a different available email address."});
        } catch(Exception) {
            logger.LogWarning("Account email confirmation could not be sent.");
            return StatusCode(503,new {message="Could not send the confirmation email. Try again later."});
        }
    }
    [AllowAnonymous, HttpPost("confirm-email")]
    public async Task<IActionResult> Confirm(ConfirmEmailRequest request)
    {
        Response.Headers["Cache-Control"]="no-store";
        if(!limits.Allow("email-confirm-global",200) || !limits.Allow("email-confirm-ip:"+HttpContext.Connection.RemoteIpAddress,20))
            return StatusCode(429,new {message="Too many attempts. Try again in 15 minutes."});
        return await service.ConfirmEmailAsync(request.Token) ? Ok(new {message="Email changed. Sign in again."})
            : BadRequest(new {message="This link is invalid, expired, already used, or the email is unavailable. Request a new link from My account."});
    }
}
public sealed class ProfileRequest
{
    [Required,StringLength(150)] public string FullName {get;set;}="";
    [Required,RegularExpression(@"^\+?[0-9() .-]{6,25}$")] public string MobileNumber {get;set;}="";
    [Required,StringLength(500)] public string Address {get;set;}="";
}
public sealed class PhotoRequest { [Required] public IFormFile Image {get;set;}=null!; }
public sealed class ChangePasswordRequest
{
    [Required,StringLength(1024)] public string CurrentPassword {get;set;}="";
    [Required,StringLength(128,MinimumLength=8)] public string NewPassword {get;set;}="";
    [Required,Compare(nameof(NewPassword))] public string ConfirmPassword {get;set;}="";
}
public sealed class ChangeEmailRequest
{
    [Required,StringLength(1024)] public string CurrentPassword {get;set;}="";
    [Required,EmailAddress,StringLength(254)] public string NewEmail {get;set;}="";
}
public sealed class ConfirmEmailRequest { [Required,StringLength(8192)] public string Token {get;set;}=""; }
