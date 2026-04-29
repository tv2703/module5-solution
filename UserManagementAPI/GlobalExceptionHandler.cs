using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace UserManagementAPI
{
    /// <summary>
    /// Global exception handler — catches any unhandled exception that escapes
    /// individual controller try-catch blocks and returns a safe, structured
    /// JSON 500 response WITHOUT leaking internal stack trace details.
    ///
    /// BUG FIX 6 (Copilot analysis): Previously, an unhandled exception would
    /// propagate as a raw 500 with a full stack trace in the response body,
    /// which is both a security concern and a poor user experience.
    /// </summary>
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception   exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception,
                "Unhandled exception: {Message}",
                exception.Message);

            var problem = new ProblemDetails
            {
                Status   = StatusCodes.Status500InternalServerError,
                Title    = "An unexpected error occurred.",
                Detail   = "The server encountered an error. Please try again later.",
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

            // Return true — exception is handled; do not re-throw
            return true;
        }
    }
}
