namespace UserManagementAPI.Middleware
{
    /// <summary>
    /// Authentication Middleware — validates tokens from incoming requests.
    /// Allows access only to users with valid tokens (e.g., matching a predefined secret).
    /// Returns a 401 Unauthorized response for missing or invalid tokens.
    ///
    /// Copilot prompt used:
    ///   "Write ASP.NET Core middleware that validates an authorization token from
    ///    the request headers and returns 401 Unauthorized if invalid."
    /// </summary>
    public class SimpleAuthMiddleware
    {
        private readonly RequestDelegate _next;
        // In a real application, tokens would be verified against a DB or via JWT validation.
        // For this scenario, we use a simple predefined valid token.
        private const string ValidToken = "TechHive-Secret-Token-2026";

        public SimpleAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip auth for the root health-check endpoint
            if (context.Request.Path == "/")
            {
                await _next(context);
                return;
            }

            // Check for the presence of the Authorization header
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                await WriteUnauthorizedResponseAsync(context, "Missing Authorization header.");
                return;
            }

            // Extract the token (assuming "Bearer <token>" format)
            var token = authHeader.FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(token) || token != ValidToken)
            {
                await WriteUnauthorizedResponseAsync(context, "Invalid token.");
                return;
            }

            // Token is valid, proceed to the next middleware
            await _next(context);
        }

        private static async Task WriteUnauthorizedResponseAsync(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                error = "Unauthorized",
                message = message
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }

    /// <summary>Extension method for clean registration in Program.cs.</summary>
    public static class SimpleAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseSimpleAuth(this IApplicationBuilder app) =>
            app.UseMiddleware<SimpleAuthMiddleware>();
    }
}
