using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Api.IntegrationTests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestConnectionString =
        "Host=localhost;Port=5432;Database=itineraries_test_db;Username=postgres;Password=postgrespassword";

    private static readonly object _dbLock = new();
    private static bool _initialized;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        lock (_dbLock)
        {
            if (!_initialized)
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseNpgsql(TestConnectionString)
                    .Options;
                using var db = new ApplicationDbContext(options);
                db.Database.EnsureDeleted();
                db.Database.Migrate();
                _initialized = true;
            }
        }

        return base.CreateHost(builder);
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(TestConnectionString)
                .Options;
            using var db = new ApplicationDbContext(options);
            db.Database.EnsureDeleted();
        }
        catch
        {
        }
        base.Dispose(disposing);
    }
}
