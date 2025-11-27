using Microsoft.EntityFrameworkCore;
using SessionTrackerService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Add SQLite database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=sessions.db"));

var app = builder.Build();

// Create database if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Log a session
app.MapPost("/api/sessions", async (AppDbContext db, SessionRequest request) =>
{
    var session = new Session
    {
        Id = Guid.NewGuid(),
        Type = request.Type,
        Minutes = request.Minutes,
        StartedAt = request.StartedAt,
        CompletedAt = request.CompletedAt,
        WasCompleted = request.WasCompleted,
        Notes = request.Notes
    };

    db.Sessions.Add(session);
    await db.SaveChangesAsync();

    return Results.Ok(session);
});

// Get today's sessions
app.MapGet("/api/sessions/today", async (AppDbContext db) =>
{
    var today = DateTime.UtcNow.Date;
    var sessions = await db.Sessions
        .Where(s => s.StartedAt >= today)
        .OrderByDescending(s => s.StartedAt)
        .ToListAsync();

    return Results.Ok(sessions);
});

// Get all sessions
app.MapGet("/api/sessions", async (AppDbContext db, int? limit) =>
{
    var query = db.Sessions.OrderByDescending(s => s.StartedAt);
    var sessions = limit.HasValue 
        ? await query.Take(limit.Value).ToListAsync()
        : await query.ToListAsync();

    return Results.Ok(sessions);
});

// Get stats
app.MapGet("/api/stats", async (AppDbContext db) =>
{
    var today = DateTime.UtcNow.Date;
    var weekAgo = today.AddDays(-7);

    var todaySessions = await db.Sessions
        .Where(s => s.StartedAt >= today && s.WasCompleted)
        .ToListAsync();

    var weekSessions = await db.Sessions
        .Where(s => s.StartedAt >= weekAgo && s.WasCompleted)
        .ToListAsync();

    var totalMinutesToday = todaySessions.Sum(s => s.Minutes);
    var totalMinutesWeek = weekSessions.Sum(s => s.Minutes);

    return Results.Ok(new
    {
        today = new
        {
            sessions = todaySessions.Count,
            minutes = totalMinutesToday,
            pomodoros = todaySessions.Count(s => s.Type == "pomodoro")
        },
        week = new
        {
            sessions = weekSessions.Count,
            minutes = totalMinutesWeek,
            pomodoros = weekSessions.Count(s => s.Type == "pomodoro")
        }
    });
});

app.Run();

// Request model
public record SessionRequest(
    string Type,
    int Minutes,
    DateTime StartedAt,
    DateTime? CompletedAt,
    bool WasCompleted,
    string? Notes
);