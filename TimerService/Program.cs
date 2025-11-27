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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// enkel in-memory timer
Timer? currentTimer = null;

// start
app.MapPost("/api/start", (StartRequest req) =>
{
    var minutes = req.Type switch
    {
        "pomodoro" => 25,
        "break" => 5,
        "deepwork" => 90,
        _ => req.Minutes ?? 25
    };

    currentTimer = new Timer
    {
        Id = Guid.NewGuid(),
        Type = req.Type,
        Minutes = minutes,
        Status = "running",
        StartedAt = DateTime.UtcNow,
        SecondsLeft = minutes * 60
    };

    return Results.Ok(currentTimer);
});

// status
app.MapGet("/api/status", () =>
{
    if (currentTimer == null)
        return Results.Ok(new { status = "idle" });

    var elapsed = (DateTime.UtcNow - currentTimer.StartedAt).TotalSeconds;
    var secondsLeft = Math.Max(0, currentTimer.SecondsLeft - (int)elapsed);

    return Results.Ok(new
    {
        timer = currentTimer,
        secondsLeft,
        minutesLeft = Math.Ceiling(secondsLeft / 60.0)
    });
});

// stop
app.MapPost("/api/stop", () =>
{
    if (currentTimer == null)
        return Results.BadRequest("No timer running");

    currentTimer.Status = "stopped";
    var t = currentTimer;
    currentTimer = null;

    return Results.Ok(t);
});

// pause
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

// resume
app.MapPost("/api/resume", () =>
{
    if (currentTimer == null || currentTimer.Status != "paused")
        return Results.BadRequest("No timer to resume");

    currentTimer.Status = "running";
    currentTimer.StartedAt = DateTime.UtcNow;

    return Results.Ok(currentTimer);
});

app.Run();

// models
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
