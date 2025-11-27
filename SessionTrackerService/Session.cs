namespace SessionTrackerService
{
    public class Session
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = "pomodoro";
        public int Minutes { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool WasCompleted { get; set; }
        public string? Notes { get; set; }
    }
}