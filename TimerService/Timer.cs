namespace TimerService
{
    public class Timer
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = "pomodoro";
        public int Minutes { get; set; }
        public string Status { get; set; } = "idle";
        public DateTime StartedAt { get; set; }
        public int SecondsLeft { get; set; }
    }
}
