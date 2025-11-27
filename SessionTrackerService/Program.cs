using Microsoft.EntityFrameworkCore;
using SessionTrackerService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
    );
});

// SQLite
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=sessions.db")
);

var app = builder.Build();

// create DB if missing
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

// log a session
app.MapPost("/api/sessions", async (AppDbContext db, SessionRequest req) =>
{
    var session = new Session
    {
        Id = Guid.NewGuid(),
        Type = req.Type,
        Minutes = req.Minutes,
        StartedAt = req.StartedAt,
        CompletedAt = req.CompletedAt,
        WasCompleted = req.WasCompleted,
        Notes = req.Notes
    };

    db.Sessions.Add(session);
    await db.SaveChangesAsync();
    return Results.Ok(session);
});

// today's sessions
app.MapGet("/api/sessions/today", async (AppDbContext db) =>
{
    var today = DateTime.UtcNow.Date;
    var sessions = await db.Sessions
        .Where(s => s.StartedAt >= today)
        .OrderByDescending(s => s.StartedAt)
        .ToListAsync();

    return Results.Ok(sessions);
});

// all sessions (optional limit)
app.MapGet("/api/sessions", async (AppDbContext db, int? limit) =>
{
    var q = db.Sessions.OrderByDescending(s => s.StartedAt);
    var sessions = limit.HasValue
        ? await q.Take(limit.Value).ToListAsync()
        : await q.ToListAsync();

    return Results.Ok(sessions);
});

// stats
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

    var minutesToday = todaySessions.Sum(s => s.Minutes);
    var minutesWeek = weekSessions.Sum(s => s.Minutes);

    return Results.Ok(new
    {
        today = new
        {
            sessions = todaySessions.Count,
            minutes = minutesToday,
            pomodoros = todaySessions.Count(s => s.Type == "pomodoro")
        },
        week = new
        {
            sessions = weekSessions.Count,
            minutes = minutesWeek,
            pomodoros = weekSessions.Count(s => s.Type == "pomodoro")
        }
    });
});

app.Run();

// request model
public record SessionRequest(
    string Type,
    int Minutes,
    DateTime StartedAt,
    DateTime? CompletedAt,
    bool WasCompleted,
    string? Notes
);