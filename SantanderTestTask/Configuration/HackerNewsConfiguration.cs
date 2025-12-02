namespace SantanderTestTask.Configuration
{
    public class HackerNewsConfiguration
    {
        public required string BaseAddress { get; init; }

        // rate limiter    
        public required int PermitLimit { get; init; }
        public required int WindowSeconds { get; init; }
        public required int QueueLimit { get; init; }

        // handler timeout (ms) used to fail fast when rate limiter is saturated
        public required int AcquireTimeoutMs { get; init; }

        // Polly settings
        public required int PollyBulkheadMaxParallelization { get; init; }
        public required int PollyBulkheadMaxQueuingActions { get; init; }
        public required int PollyRetryCount { get; init; }
        public required int PollyRetryJitterMsMax { get; init; }
    }
}
