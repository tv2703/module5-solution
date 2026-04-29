namespace UserManagementAPI.Middleware
{
    /// <summary>
    /// Logging Middleware — logs every incoming HTTP request and its outgoing
    /// response, including method, path, status code, and elapsed time.
    ///
    /// Copilot prompt used:
    ///   "Generate middleware to log HTTP requests and responses in ASP.NET Core."
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next,
                                        ILogger<RequestLoggingMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // ── Log the incoming request ──────────────────────────────────────
            _logger.LogInformation(
                "[REQUEST]  {Method} {Path}{Query} | ClientIP: {IP}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                context.Connection.RemoteIpAddress);

            // Pass control down the pipeline
            await _next(context);

            sw.Stop();

            // ── Log the outgoing response ─────────────────────────────────────
            _logger.LogInformation(
                "[RESPONSE] {Method} {Path} → {StatusCode} ({Elapsed}ms)",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }

    /// <summary>Extension method for clean registration in Program.cs.</summary>
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(
            this IApplicationBuilder app) =>
            app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
