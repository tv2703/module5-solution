using System.Net;
using System.Text.Json;

namespace UserManagementAPI.Middleware
{
    /// <summary>
    /// Error Handling Middleware — catches ALL unhandled exceptions that bubble
    /// up from the pipeline and returns a consistent JSON error response,
    /// preventing raw stack traces from reaching the client.
    ///
    /// Copilot prompt used:
    ///   "Create ASP.NET Core middleware that catches unhandled exceptions and
    ///    returns consistent JSON error responses."
    ///
    /// Must be registered FIRST in the middleware pipeline so it wraps
    /// every subsequent middleware and controller action.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next,
                                       ILogger<ErrorHandlingMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception on {Method} {Path}: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);

                await WriteErrorResponseAsync(context, ex);
            }
        }

        private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
        {
            // Map known exception types to specific status codes
            var statusCode = ex switch
            {
                ArgumentNullException      => (int)HttpStatusCode.BadRequest,
                ArgumentException          => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException        => (int)HttpStatusCode.NotFound,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                _                          => (int)HttpStatusCode.InternalServerError
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = statusCode;

            // Safe, consistent error envelope — never exposes internal details
            var errorResponse = new
            {
                error     = GetFriendlyMessage(statusCode),
                path      = context.Request.Path.ToString(),
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(errorResponse,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            await context.Response.WriteAsync(json);
        }

        private static string GetFriendlyMessage(int statusCode) => statusCode switch
        {
            400 => "Bad request. Please check the data you submitted.",
            401 => "Unauthorized. Valid authentication is required.",
            404 => "The requested resource was not found.",
            _   => "Internal server error. Please try again later."
        };
    }

    /// <summary>Extension method for clean registration in Program.cs.</summary>
    public static class ErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandling(
            this IApplicationBuilder app) =>
            app.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
