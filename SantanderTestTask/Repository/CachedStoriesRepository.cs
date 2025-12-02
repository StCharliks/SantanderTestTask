using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SantanderTestTask.Configuration;
using SantanderTestTask.Models;

namespace SantanderTestTask.Repository
{
    public class CachedStoriesRepository : IStoriesRepository
    {
        private readonly IStoriesRepository _innerRepository;
        private IMemoryCache _memCache;

        // per-key locks to serialize initial population
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly TimeSpan _defaultTtl;

        public CachedStoriesRepository(IStoriesRepository innerRepository, IMemoryCache memCache, IOptions<CacheConfiguration> options)
        {
            _innerRepository = innerRepository;
            _memCache = memCache;
            _defaultTtl = TimeSpan.FromMilliseconds(options.Value.TtlInMs);
        }

        public async Task<IReadOnlyList<int>> GetBestStoriesIdsAsync()
        {
            const string key = "BestStoriesIds";

            var now = DateTimeOffset.UtcNow;

            if (_memCache.TryGetValue<CachedItem<IReadOnlyList<int>>>(key, out var cached))
            {
                if (cached.Expiry > now)
                {
                    return cached.Value;
                }

                // expired -> return stale immediately and refresh in background
                _ = RefreshBestStoriesIdsInBackground(key);
                return cached.Value;
            }

            // not present -> populate while serializing with a lock to avoid stampede
            var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1,1));
            await sem.WaitAsync();
            try
            {
                if (_memCache.TryGetValue(key, out cached))
                {
                    return cached!.Value;
                }

                var ids = await _innerRepository.GetBestStoriesIdsAsync();
                var newCached = new CachedItem<IReadOnlyList<int>>(ids, now.Add(_defaultTtl));
                _memCache.Set(key, newCached, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _defaultTtl
                });

                return ids ?? new List<int>(0);
            }
            finally
            {
                sem.Release();
            }
        }

        public async Task<IReadOnlyList<Story>> GetStoriesAsync()
        {
            const string key = "stories";

            var now = DateTimeOffset.UtcNow;

            if (_memCache.TryGetValue<CachedItem<IReadOnlyList<Story>>>(key, out var cached))
            {
                if (cached.Expiry > now)
                {
                    return cached.Value;
                }

                // expired -> return stale immediately and refresh in background
                _ = RefreshStoriesInBackground(key);
                return cached.Value;
            }

            var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1,1));
            await sem.WaitAsync();
            try
            {
                if (_memCache.TryGetValue<CachedItem<IReadOnlyList<Story>>>(key, out cached))
                {
                    return cached!.Value;
                }

                var stories = await _innerRepository.GetStoriesAsync();
                var newCached = new CachedItem<IReadOnlyList<Story>>(stories, now.Add(_defaultTtl));
                _memCache.Set(key, newCached, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _defaultTtl
                });

                return stories ?? new List<Story>(0);
            }
            finally
            {
                sem.Release();
            }
        }

        private async Task RefreshStoriesInBackground(string key)
        {
            try
            {
                var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1,1));
                // try to enter lock but don't block callers
                if (!await sem.WaitAsync(0)) return;
                try
                {
                    var stories = await _innerRepository.GetStoriesAsync();
                    var newCached = new CachedItem<IReadOnlyList<Story>>(stories, DateTimeOffset.UtcNow.Add(_defaultTtl));
                    _memCache.Set(key, newCached, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _defaultTtl
                    });
                }
                finally
                {
                    sem.Release();
                }
            }
            catch
            {
                // swallow exceptions from background refresh
            }
        }

        private async Task RefreshBestStoriesIdsInBackground(string key)
        {
            try
            {
                var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1,1));
                if (!await sem.WaitAsync(0)) return;
                try
                {
                    var ids = await _innerRepository.GetBestStoriesIdsAsync();
                    var newCached = new CachedItem<IReadOnlyList<int>>(ids, DateTimeOffset.UtcNow.Add(_defaultTtl));
                    _memCache.Set(key, newCached, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _defaultTtl
                    });
                }
                finally
                {
                    sem.Release();
                }
            }
            catch
            {
                // swallow
            }
        }

        private sealed class CachedItem<T>
        {
            public T Value { get; }
            public DateTimeOffset Expiry { get; }

            public CachedItem(T value, DateTimeOffset expiry)
            {
                Value = value;
                Expiry = expiry;
            }
        }
    }
}
