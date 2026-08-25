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
string? rawConnStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(rawConnStr) || rawConnStr.Contains("localhost"))
{
    string? envDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(envDbUrl))
    {
        rawConnStr = envDbUrl;
    }
}

bool forceInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase") || 
                     string.Equals(Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB"), "true", StringComparison.OrdinalIgnoreCase);

if (forceInMemory || string.IsNullOrWhiteSpace(rawConnStr) || rawConnStr.Contains("localhost"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("TestAppInMemoryDb"));
}
else
{
    string npgsqlConnectionString = ConvertPostgresUriToConnectionString(rawConnStr);
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(npgsqlConnectionString));
}

var app = builder.Build();

// Enable Swagger in Development and Staging
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Ensure DB migrations are applied on startup (if using relational DB like PostgreSQL)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not apply database migrations on startup. Please ensure connection string is valid and database is running.");
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
