using Microsoft.EntityFrameworkCore;
using TestApp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Ensure Kestrel listens on non-privileged ports for .NET 8 container compatibility
var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(railwayPort) && railwayPort != "8080")
{
    builder.WebHost.UseUrls("http://*:8080", $"http://*:{railwayPort}");
}
else
{
    builder.WebHost.UseUrls("http://*:8080");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure Database Connection
string? envDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string? configConnStr = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(envDbUrl) && !envDbUrl.Contains("localhost"))
{
    try
    {
        string npgsqlConn = ConvertPostgresUriToConnectionString(envDbUrl);
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(npgsqlConn));
    }
    catch
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestAppInMemoryDb"));
    }
}
else if (!string.IsNullOrWhiteSpace(configConnStr) && !configConnStr.Contains("localhost"))
{
    try
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configConnStr));
    }
    catch
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestAppInMemoryDb"));
    }
}
else
{
    // Use In-Memory DB by default for instant online testing
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("TestAppInMemoryDb"));
}

var app = builder.Build();

app.UseDeveloperExceptionPage();

// Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { status = "online", service = "TestApp.Api", time = DateTime.UtcNow }));
app.MapControllers();

// Initialize Database schema safely
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Database initialization notice: {Message}", ex.Message);
    }
}

app.Run();

static string ConvertPostgresUriToConnectionString(string uriString)
{
    if (string.IsNullOrWhiteSpace(uriString)) return string.Empty;
    if (!uriString.StartsWith("postgres://") && !uriString.StartsWith("postgresql://"))
    {
        return uriString;
    }

    var uri = new Uri(uriString);
    var userInfo = uri.UserInfo.Split(':');
    var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');

    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true;";
}

public partial class Program { }
