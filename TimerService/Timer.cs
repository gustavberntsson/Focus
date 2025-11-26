namespace TimerService
{
    public class Timer
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = "pomodoro"; // pomodoro, break, deepwork
        public int Minutes { get; set; }
        public string Status { get; set; } = "idle"; // idle, running, paused, done
        public DateTime StartedAt { get; set; }
        public int SecondsLeft { get; set; }
    }
}
