namespace SantanderTestTask.Infrastructure.Entities
{
    public record HackerNewsStory
    {
        public required string By { get; set; }

        public required int Descendants { get; set; }

        public int[]? Kids { get; set; }

        public required int Score { get; set; }

        public required long Time { get; set; }

        public required string Title { get; set; }

        public required string Type { get; set; }

        public string? Url { get; set; }
    }
}
