# Focus

Productivity timer with ambient sounds. Side project for practicing microservices and desktop development.

## What It Does

Pomodoro timer + ambient sounds + session tracking.

- Start 25min/5min/90min focus sessions
- Play rain, waves, or white noise while working
- Track sessions and see daily/weekly stats
- Everything runs locally

## Tech

**Backend**: C# .NET microservices (Timer, Session Tracker, Sound Service)
**Frontend**: Electron desktop app
**Database**: SQLite
**Audio**: NAudio for MP3 playback

## Run It
```bash
cd FocusApp
npm install
npm start
```

App auto-starts all backend services.

**Need**: .NET 9 SDK installed

## How It Works

Electron app talks to three local APIs:
- Timer Service (port 5004)
- Session Tracker (port 5179) 
- Sound Service (port 5014)

Services are independent. Could be deployed separately.

## Files
```
TimerService/          → Timer logic
SessionTrackerService/ → SQLite session tracking  
SoundService/          → Plays MP3s
  └── Sounds/          → Rain.mp3, Waves.mp3, WhiteNoise.mp3
FocusApp/              → Desktop UI
```

## Notes

- Data resets on app restart (SQLite recreated each time)
- Need MP3 files in Sounds folder
- Backend terminal output visible when running

Built to practice microservices patterns and C#/JavaScript integration. Fully functional but not production-ready.

---

**Stack**: C# • .NET 9 • Electron • SQLite • NAudio
