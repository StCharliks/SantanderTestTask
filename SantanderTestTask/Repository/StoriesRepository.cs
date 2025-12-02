using SantanderTestTask.Infrastructure.Entities;
using SantanderTestTask.Models;

namespace SantanderTestTask.Repository
{
    public class StoriesRepository : IStoriesRepository
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public StoriesRepository(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IReadOnlyList<int>> GetBestStoriesIdsAsync()
        {
            var client = _httpClientFactory.CreateClient("HackerNews");

            var result = await client.GetFromJsonAsync<IReadOnlyList<int>>("beststories.json");

            return result ?? Array.Empty<int>();
        }

        public async Task<IReadOnlyList<Story>> GetStoriesAsync()
        {
            var storiesIds = await GetBestStoriesIdsAsync();

            var tasks = storiesIds.Select(async id =>
            {
                var client = _httpClientFactory.CreateClient("HackerNews");
                var story = await client.GetFromJsonAsync<HackerNewsStory>($"item/{id}.json");
                return Story.FromEntity(story!);
            });

            return await Task.WhenAll(tasks)
                .ContinueWith(t => t.Result.Where(s => s != null).ToList()!)
                ?? new List<Story>(0);
        }
    }
}
