using Microsoft.EntityFrameworkCore;

namespace ClientSupportTicketingSystem.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                context.Abort();
            }
            catch (DbUpdateConcurrencyException exception)
            {
                if (context.Response.HasStarted)
                    throw;
                _logger.LogWarning(exception, "Concurrent update for {Method} {Path}. TraceId: {TraceId}", context.Request.Method, context.Request.Path, context.TraceIdentifier);
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = StatusCodes.Status409Conflict,
                    message = "The record changed. Reload it and retry.",
                    traceId = context.TraceIdentifier
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}. TraceId: {TraceId}", context.Request.Method, context.Request.Path, context.TraceIdentifier);
                if (context.Response.HasStarted)
                    throw;
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = "An unexpected error occurred. Please try again later.",
                    traceId = context.TraceIdentifier
                });
            }
        }
    }
}