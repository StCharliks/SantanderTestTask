using Microsoft.Extensions.Caching.Memory;
using SantanderTestTask.Models;

namespace SantanderTestTask.Repository
{
    public class CachedStoriesRepository : IStoriesRepository
    {
        private readonly IStoriesRepository _innerRepository;
        private IMemoryCache _memCache;

        public CachedStoriesRepository(IStoriesRepository innerRepository, IMemoryCache memCache)
        {
            _innerRepository = innerRepository;
            _memCache = memCache;
        }

        public async Task<IReadOnlyList<int>> GetBestStoriesIdsAsync()
        {
            return (await _memCache.GetOrCreateAsync("BestStoriesIds", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _innerRepository.GetBestStoriesIdsAsync();
            })) ?? new List<int>(0); 
        }

        public async Task<IReadOnlyList<Story>> GetStoriesAsync()
        {
            return await _memCache.GetOrCreateAsync("stories", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _innerRepository.GetStoriesAsync();
            }) ?? new List<Story>(0);
        }
    }
}
