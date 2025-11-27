using NAudio.Wave;
using SoundService;

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

// available sounds
var sounds = new List<Sound>
{
    new Sound { Id = "rain", Name = "Rain", FilePath = "Sounds/Rain.mp3", Type = "nature" },
    new Sound { Id = "waves", Name = "Ocean Waves", FilePath = "Sounds/Waves.mp3", Type = "nature" },
    new Sound { Id = "whitenoise", Name = "White Noise", FilePath = "Sounds/WhiteNoise.mp3", Type = "noise" }
};

// currently playing
IWavePlayer? currentPlayer = null;
AudioFileReader? currentAudio = null;
string? currentSoundId = null;

// list sounds
app.MapGet("/api/sounds", () =>
{
    return Results.Ok(sounds);
});

// play sound
app.MapPost("/api/sounds/play/{id}", (string id) =>
{
    var sound = sounds.FirstOrDefault(s => s.Id == id);
    if (sound == null)
        return Results.NotFound($"Sound '{id}' not found");

    if (!File.Exists(sound.FilePath))
        return Results.NotFound($"Sound file not found: {sound.FilePath}");

    try
    {
        // stop current sound if playing
        if (currentPlayer != null)
        {
            try
            {
                currentPlayer.Stop();
                currentPlayer.Dispose();
            }
            catch { }
            currentPlayer = null;
        }
        
        if (currentAudio != null)
        {
            try
            {
                currentAudio.Dispose();
            }
            catch { }
            currentAudio = null;
        }

        // small delay
        System.Threading.Thread.Sleep(100);

        // start new sound
        currentAudio = new AudioFileReader(sound.FilePath);
        currentPlayer = new WaveOutEvent();
        currentPlayer.Init(currentAudio);
        
        // loop the sound
        currentPlayer.PlaybackStopped += (sender, args) =>
        {
            if (currentAudio != null && currentPlayer != null)
            {
                try
                {
                    currentAudio.Position = 0;
                    currentPlayer.Play();
                }
                catch { }
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

// stop sound
app.MapPost("/api/sounds/stop", () =>
{
    if (currentPlayer == null)
        return Results.BadRequest("No sound is playing");

    currentPlayer.Stop();
    currentPlayer.Dispose();
    currentAudio?.Dispose();
    currentPlayer = null;
    currentAudio = null;

    var stopped = currentSoundId;
    currentSoundId = null;

    return Results.Ok(new { message = "Stopped playing", soundId = stopped });
});

// status
app.MapGet("/api/sounds/status", () =>
{
    if (currentPlayer == null || currentSoundId == null)
        return Results.Ok(new { isPlaying = false });

    var sound = sounds.FirstOrDefault(s => s.Id == currentSoundId);
    return Results.Ok(new
    {
        isPlaying = true,
        sound,
        volume = currentPlayer.Volume
    });
});

// volume 0.0 - 1.0
app.MapPost("/api/sounds/volume", (VolumeRequest req) =>
{
    if (currentPlayer == null)
        return Results.BadRequest("No sound is playing");

    var volume = Math.Clamp(req.Volume, 0f, 1f);
    currentPlayer.Volume = volume;

    return Results.Ok(new { volume });
});

app.Run();

// request model
public record VolumeRequest(float Volume);
