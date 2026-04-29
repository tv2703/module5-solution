using UserManagementAPI;
using UserManagementAPI.Middleware;
using UserManagementAPI.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddControllers();

// Singleton repository — shared in-memory store across all requests
builder.Services.AddSingleton<UserRepository>();

// CORS — allow all origins for development/testing with Postman
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// ── Middleware pipeline ────────────────────────────────────────────────────────

// Corporate policy requirement: Configure middleware in the correct order

// 1. Error-handling middleware FIRST
app.UseErrorHandling();

// 2. Authentication middleware NEXT
app.UseSimpleAuth();

// 3. Logging middleware LAST
app.UseRequestLogging();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// (Using custom auth middleware instead of built-in)
// app.UseAuthorization(); 

app.MapControllers();

// Health-check / route listing at root
app.MapGet("/", () => Results.Ok(new
{
    api      = "TechHive Solutions – User Management API",
    version  = "v1",
    status   = "Running ✔",
    endpoints = new[]
    {
        "GET    /api/users",
        "GET    /api/users/{id}",
        "POST   /api/users",
        "PUT    /api/users/{id}",
        "DELETE /api/users/{id}"
    }
}));

app.Run();
