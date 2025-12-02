namespace SantanderTestTask.Configuration
{
    public class CacheConfiguration
    {
        public int TtlInMs { get; init; }

        public bool StaleWhileRevalidate { get; init; }
    }
}
