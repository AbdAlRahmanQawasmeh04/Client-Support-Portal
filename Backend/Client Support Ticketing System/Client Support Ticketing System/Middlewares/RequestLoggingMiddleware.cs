using System.Diagnostics;

namespace ClientSupportTicketingSystem.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var timer = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                _logger.LogInformation(
                    "HTTP {Method} {Path} returned {StatusCode} "
                    + "in {ElapsedMs}ms. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    timer.ElapsedMilliseconds,
                    context.TraceIdentifier);
            }
        }
    }
}