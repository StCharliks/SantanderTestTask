using SantanderTestTask.Models;
using SantanderTestTask.Repository;

namespace SantanderTestTask.Services
{
    public class StoriesService : IStoriesService
    {
        private readonly IStoriesRepository _storyRepository;

        public StoriesService(IStoriesRepository storyRepository)
        {
            _storyRepository = storyRepository;
        }

        public async Task<IReadOnlyList<Story>> GetBestStoriesAsync(int count)
        {
            var stories = await _storyRepository.GetStoriesAsync();

            return stories
                .OrderByDescending(s => s.Score)
                .Take(count)
                .ToList();
        }
    }
}
