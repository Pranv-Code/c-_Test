using Microsoft.EntityFrameworkCore;
using TestApp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for multi-client desktop / browser access
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

bool forceInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase") || 
                     string.Equals(Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB"), "true", StringComparison.OrdinalIgnoreCase);

if (forceInMemory || (string.IsNullOrWhiteSpace(envDbUrl) && (string.IsNullOrWhiteSpace(configConnStr) || configConnStr.Contains("localhost"))))
{
    // Fallback to In-Memory DB if DATABASE_URL is not set
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("TestAppInMemoryDb"));
}
else
{
    string targetConnectionString = !string.IsNullOrWhiteSpace(envDbUrl) 
        ? ConvertPostgresUriToConnectionString(envDbUrl) 
        : configConnStr!;

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(targetConnectionString));
}

var app = builder.Build();

app.UseDeveloperExceptionPage();

// Enable Swagger in Development and Staging
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Ensure DB migrations are applied on startup (if using relational DB like PostgreSQL) or EnsureCreated (if in-memory)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
        }
        else
        {
            dbContext.Database.EnsureCreated();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not initialize database on startup: {Message}", ex.Message);
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
