namespace SoundService
{
    public class Sound
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Type { get; set; } = "ambient"; // ambient, noise, nature
    }
}