using Application.Interfaces;
using Application.Services;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register application services and repositories
builder.Services.AddScoped<IItineraryRepository, ItineraryRepository>();
builder.Services.AddScoped<IItineraryService, ItineraryService>();

// Controllers and Swagger/OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Tourist Itinerary Management System API",
        Version = "v1",
        Description = "REST API for thematic tourist itinerary management."
    });
});

var app = builder.Build();

// Automatic database migration at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    // Fix: Swagger UI embedded validator requires openapi 3.0.1 - 3.0.3, but recent Microsoft.OpenApi emits 3.0.4
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.Value != null &&
            context.Request.Path.Value.EndsWith("swagger.json", StringComparison.OrdinalIgnoreCase))
        {
            var originalBody = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            await next();

            memStream.Position = 0;
            using var reader = new StreamReader(memStream);
            var responseBody = await reader.ReadToEndAsync();

            responseBody = responseBody.Replace("\"openapi\": \"3.0.4\"", "\"openapi\": \"3.0.1\"")
                                       .Replace("\"openapi\":\"3.0.4\"", "\"openapi\":\"3.0.1\"");

            context.Response.Body = originalBody;
            context.Response.Headers.ContentLength = Encoding.UTF8.GetByteCount(responseBody);
            await context.Response.WriteAsync(responseBody);
            return;
        }

        await next();
    });

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tourist Itinerary API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
