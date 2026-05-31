using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyWebApp.Data;
using MyWebApp.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// =========================
// Services
// =========================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Nested DTOs in multiple controllers share short names (e.g. CreateProjectTasksRequest).
    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".", StringComparison.Ordinal));
});
builder.Services.AddSingleton<GenerativeEngineChatService>();
builder.Services.AddScoped<DemoDataService>();
builder.Services.AddScoped<PmoAnalysisAiService>();

// EF Core + SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS for Blazor Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5058",
                    "https://localhost:5058",
                    "http://127.0.0.1:5058",
                    "https://127.0.0.1:5058")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Apply pending EF Core migrations at startup and ensure schema fixes for SQLite.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Apply migrations normally
    db.Database.Migrate();

    // SQLite sometimes needs manual ALTER TABLE to add columns when migrations were not applied.
    try
    {
        var conn = db.Database.GetDbConnection();
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info('Projects')";
            using (var reader = cmd.ExecuteReader())
            {
                var hasDeadline = false;
                while (reader.Read())
                {
                    var colName = reader.GetString(1);
                    if (string.Equals(colName, "Deadline", StringComparison.OrdinalIgnoreCase))
                    {
                        hasDeadline = true;
                        break;
                    }
                }

                if (!hasDeadline)
                {
                    // Add the column as nullable (no DEFAULT expression) then populate existing rows.
                    db.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN Deadline TEXT");
                    db.Database.ExecuteSqlRaw("UPDATE Projects SET Deadline = date('now') WHERE Deadline IS NULL");
                }
            }
        }
        conn.Close();
    }
    catch
    {
        // If this fails, let the app continue so the developer can inspect logs.
    }

    if (scope.ServiceProvider.GetRequiredService<IConfiguration>().GetValue<bool>("Sync:DemoMode"))
    {
        var demo = scope.ServiceProvider.GetRequiredService<DemoDataService>();
        await demo.SeedIfEmptyAsync();
    }
}

// =========================
// HTTP pipeline
// =========================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();