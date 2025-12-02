namespace SantanderTestTask.Models
{
    public record Story
    {
        public required string Title { get; init; }

        public required string Uri { get; init; }

        public required string PostedBy { get; init; }

        public required DateTimeOffset Time { get; init; }

        public required int Score { get; init; }

        public required int CommentsCount { get; init; }

        public static Story FromEntity(Infrastructure.Entities.HackerNewsStory entity)
        {
            return new Story
            {
                Title = entity.Title,
                Uri = entity.Url ?? string.Empty,
                PostedBy = entity.By,
                Time = DateTimeOffset.FromUnixTimeSeconds(entity.Time),
                Score = entity.Score,
                CommentsCount = entity.Descendants
            };
        }
    }
}
