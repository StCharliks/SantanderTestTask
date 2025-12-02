using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
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
                try
                {
                    var client = _httpClientFactory.CreateClient("HackerNews");
                    var response = await client.GetAsync($"item/{id}.json");

                    if (!response.IsSuccessStatusCode)
                    {
                        // Treat non-success (including 429) as missing item; return null to allow partial results
                        return (Story?)null;
                    }

                    var storyEntity = await response.Content.ReadFromJsonAsync<HackerNewsStory>();
                    if (storyEntity == null) return (Story?)null;

                    return Story.FromEntity(storyEntity);
                }
                catch
                {
                    // Swallow per-item exceptions to avoid failing the whole batch
                    return (Story?)null;
                }
            });

            var results = await Task.WhenAll(tasks);

            return results.Where(s => s != null).Select(s => s!).ToList() ?? new List<Story>(0);
        }
    }
}
