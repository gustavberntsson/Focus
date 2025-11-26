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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// In-memory timer storage
Timer? currentTimer = null;

// Start timer
app.MapPost("/api/start", (StartRequest request) =>
{
    var minutes = request.Type switch
    {
        "pomodoro" => 25,
        "break" => 5,
        "deepwork" => 90,
        _ => request.Minutes ?? 25
    };

    currentTimer = new Timer
    {
        Id = Guid.NewGuid(),
        Type = request.Type,
        Minutes = minutes,
        Status = "running",
        StartedAt = DateTime.UtcNow,
        SecondsLeft = minutes * 60
    };

    return Results.Ok(currentTimer);
});

// Get current status
app.MapGet("/api/status", () =>
{
    if (currentTimer == null)
        return Results.Ok(new { status = "idle" });

    var elapsed = (DateTime.UtcNow - currentTimer.StartedAt).TotalSeconds;
    var secondsLeft = Math.Max(0, currentTimer.SecondsLeft - (int)elapsed);

    return Results.Ok(new
    {
        timer = currentTimer,
        secondsLeft = secondsLeft,
        minutesLeft = Math.Ceiling(secondsLeft / 60.0)
    });
});

// Stop timer
app.MapPost("/api/stop", () =>
{
    if (currentTimer == null)
        return Results.BadRequest("No timer running");

    currentTimer.Status = "stopped";
    var timer = currentTimer;
    currentTimer = null;

    return Results.Ok(timer);
});

// Pause timer
app.MapPost("/api/pause", () =>
{
    if (currentTimer == null || currentTimer.Status != "running")
        return Results.BadRequest("No timer to pause");

    var elapsed = (DateTime.UtcNow - currentTimer.StartedAt).TotalSeconds;
    currentTimer.SecondsLeft = Math.Max(0, currentTimer.SecondsLeft - (int)elapsed);
    currentTimer.Status = "paused";
    currentTimer.StartedAt = DateTime.UtcNow;

    return Results.Ok(currentTimer);
});

// Resume timer
app.MapPost("/api/resume", () =>
{
    if (currentTimer == null || currentTimer.Status != "paused")
        return Results.BadRequest("No timer to resume");

    currentTimer.Status = "running";
    currentTimer.StartedAt = DateTime.UtcNow;

    return Results.Ok(currentTimer);
});

app.Run();

// Models
public class Timer
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "pomodoro";
    public int Minutes { get; set; }
    public string Status { get; set; } = "idle";
    public DateTime StartedAt { get; set; }
    public int SecondsLeft { get; set; }
}

public class StartRequest
{
    public string Type { get; set; } = "pomodoro";
    public int? Minutes { get; set; }
}