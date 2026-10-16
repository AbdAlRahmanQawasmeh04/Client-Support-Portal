using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;
using ClientSupportTicketingSystem.Data.Context;
using ClientSupportTicketingSystem.Data.Models;
using ClientSupportTicketingSystem.Data.Repositories;
using ClientSupportTicketingSystem.Data.UnitOfWork;
using ClientSupportTicketingSystem.Middlewares;
using ClientSupportTicketingSystem.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Client_Support_Ticketing_System
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDataProtection();
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<AccountService>();
            builder.Services.AddScoped<IAccountEmailSender, AccountEmailSender>();
            builder.Services.AddScoped<IPasswordRecoveryRepository, PasswordRecoveryRepository>();
            builder.Services.AddScoped<PasswordRecoveryService>();
            builder.Services.AddSingleton(builder.Configuration.GetSection("RecoveryMail").Get<RecoveryMailSettings>() ?? new RecoveryMailSettings());
            builder.Services.AddSingleton<RecoveryLimits>();
            builder.Services.AddSingleton<RecoveryQueue>();
            builder.Services.AddHostedService<RecoveryMailWorker>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ITicketRepository, TicketRepository>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IEmployeeService, EmployeeService>();
            builder.Services.AddScoped<IClientService, ClientService>();
            builder.Services.AddScoped<IUserImageService, UserImageService>();
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
            builder.Services.AddScoped<ITicketService, TicketService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<ITicketAttachmentService, TicketAttachmentService>();
            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
                    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
                });
            });

            string jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is missing from configuration.");
            string jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is missing from configuration.");
            string jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience is missing from configuration.");
            byte[] keyBytes = Convert.FromBase64String(jwtKey);

            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException("JWT key must contain at least 32 random bytes.");
            }

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        string? idValue = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (!int.TryParse(idValue, out int id))
                        {
                            context.Fail("Invalid user.");
                            return;
                        }

                        var database = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var user = await database.Users.AsNoTracking().Where(u => u.Id == id).Select(u => new { u.IsActive, u.Role, u.PasswordHash }).SingleOrDefaultAsync(context.HttpContext.RequestAborted);
                        string? tokenRole = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;

                        if (user == null || !user.IsActive || tokenRole != user.Role.ToString() || context.Principal?.FindFirst("pwdv")?.Value != PasswordRecoveryService.PasswordVersion(user.PasswordHash))
                        {
                            context.Fail("Account is unavailable or its role changed.");
                        }
                    }
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter the JWT access token only."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<ExceptionMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("Frontend");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.MapGet("/api/health", async (AppDbContext database, HttpContext context, ILogger<Program> logger) =>
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));

                try
                {
                    await database.Notifications.Select(n => n.Id).Take(1).ToListAsync(timeout.Token);
                    return Results.Ok(new { status = "Healthy", database = "Available", checkedAt = DateTime.UtcNow });
                }
                catch (Exception error)
                {
                    logger.LogWarning(error, "Database health check failed.");
                    return Results.Json(new { status = "Unhealthy", database = "Unavailable" }, statusCode: 503);
                }
            }).AllowAnonymous();

            app.Run();
        }
    }
}