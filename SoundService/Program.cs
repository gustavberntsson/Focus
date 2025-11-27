using NAudio.Wave;
using SoundService;

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

// Available sounds
var sounds = new List<Sound>
{
    new Sound { Id = "rain", Name = "Rain", FilePath = "Sounds/Rain.mp3", Type = "nature" },
    new Sound { Id = "waves", Name = "Ocean Waves", FilePath = "Sounds/Waves.mp3", Type = "nature" },
    new Sound { Id = "whitenoise", Name = "White Noise", FilePath = "Sounds/WhiteNoise.mp3", Type = "noise" }
};

// Currently playing
IWavePlayer? currentPlayer = null;
AudioFileReader? currentAudio = null;
string? currentSoundId = null;

// Get all available sounds
app.MapGet("/api/sounds", () =>
{
    return Results.Ok(sounds);
});

// Play a sound
app.MapPost("/api/sounds/play/{id}", (string id) =>
{
    var sound = sounds.FirstOrDefault(s => s.Id == id);
    if (sound == null)
        return Results.NotFound($"Sound '{id}' not found");

    if (!File.Exists(sound.FilePath))
        return Results.NotFound($"Sound file not found: {sound.FilePath}");

    try
    {
        // Stop current sound if playing
        currentPlayer?.Stop();
        currentPlayer?.Dispose();
        currentAudio?.Dispose();

        // Start new sound
        currentAudio = new AudioFileReader(sound.FilePath);
        currentPlayer = new WaveOutEvent();
        currentPlayer.Init(currentAudio);
        
        // Loop the sound
        currentPlayer.PlaybackStopped += (sender, args) =>
        {
            if (currentAudio != null)
            {
                currentAudio.Position = 0;
                currentPlayer?.Play();
            }
        };

        currentPlayer.Play();
        currentSoundId = id;

        return Results.Ok(new { message = $"Playing {sound.Name}", sound = sound });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error playing sound: {ex.Message}");
    }
});

// Stop current sound
app.MapPost("/api/sounds/stop", () =>
{
    if (currentPlayer == null)
        return Results.BadRequest("No sound is playing");

    currentPlayer.Stop();
    currentPlayer.Dispose();
    currentAudio?.Dispose();
    currentPlayer = null;
    currentAudio = null;
    
    var stoppedSound = currentSoundId;
    currentSoundId = null;

    return Results.Ok(new { message = $"Stopped playing", soundId = stoppedSound });
});

// Get current playing status
app.MapGet("/api/sounds/status", () =>
{
    if (currentPlayer == null || currentSoundId == null)
        return Results.Ok(new { isPlaying = false });

    var sound = sounds.FirstOrDefault(s => s.Id == currentSoundId);
    return Results.Ok(new 
    { 
        isPlaying = true,
        sound = sound,
        volume = currentPlayer.Volume
    });
});

// Set volume (0.0 to 1.0)
app.MapPost("/api/sounds/volume", (VolumeRequest request) =>
{
    if (currentPlayer == null)
        return Results.BadRequest("No sound is playing");

    var volume = Math.Clamp(request.Volume, 0f, 1f);
    currentPlayer.Volume = volume;

    return Results.Ok(new { volume = volume });
});

app.Run();

public record VolumeRequest(float Volume);