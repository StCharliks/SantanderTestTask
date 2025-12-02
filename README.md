# SantanderTestTask

Lightweight .NET8 service that fetches Hacker News stories with built-in protections to avoid overloading the upstream API.

## Key features
- Named `HttpClient` for Hacker News with a singleton `FixedWindowRateLimiter`.
- `HackerNewRateLimitter` handler: fail-fast lease acquisition (returns 429) to avoid long internal queues.
- Polly policies (bulkhead + retry) applied to the client for concurrency control and resilient retries.
- `CachedStoriesRepository` implements stale-while-revalidate + per-key serialization to prevent cache stampedes.
- `StoriesRepository` fetches items in parallel but is resilient to per-item failures and returns partial results.

## Requirements
- .NET8 SDK

## Configuration
The app reads settings from configuration (e.g., `appsettings.json` or `application.json`). Important section: `HackerNews`.

Example `application.json` (minimal)

```json
{
 "HackerNews": {
 "BaseAddress": "https://hacker-news.firebaseio.com/v0/",
 "AcquireTimeoutMs":200,
 "PermitLimit":20,
 "WindowSeconds":1,
 "QueueLimit":100,
 "HttpClientTimeoutSeconds":30,
 "PollyBulkheadMaxParallelization":20,
 "PollyBulkheadMaxQueuingActions":50,
 "PollyRetryCount":3,
 "PollyRetryJitterMsMax":100
 },

 "Cache": {
 "StoriesTtlSeconds":300,
 "IdsTtlSeconds":300,
 "StaleWhileRevalidate": true
 }
}
```

## How it protects the upstream API
- Rate limiter: enforces a permit rate (permits/second) and can queue a small burst. If a permit cannot be acquired quickly, the handler returns 429.
- Bulkhead: bounds concurrent outbound requests to protect local resources.
- Retry: conservative retry with exponential backoff + jitter; honor `Retry-After` when possible.
- Cache with stale-while-revalidate: returns stale entries immediately and refreshes in background to avoid thundering herd.

## Run locally
1. Restore & build:
 `dotnet restore && dotnet build`
2. Run:
 `dotnet run --project SantanderTestTask`
3. API endpoint:
 `GET /api/stories/best/{count}`

## Docker
- Build image:
 `docker build -t santandertesttask ./SantanderTestTask`
- Run container (example):
 `docker run -p8080:8080 santandertesttask`

## Tests
- Unit tests live in `SantanderTestTask.Tests`.
- Fast concurrency test: `HighLoadFastTest` simulates concurrent callers and measures outbound concurrency.
- Run tests:
 `dotnet test SantanderTestTask.Tests`

## Load testing recommendations
- Use an external tool (k6, Locust, JMeter) against a staging instance to observe end-to-end behavior and tune:
 - PermitLimit and WindowSeconds (rate limiter)
 - Bulkhead sizes and queue limits
 - Retry count and backoff
 - Cache TTL and stale behavior

## Tuning tips
- Keep `QueueLimit` small to avoid many waiting tasks.
- Bulkhead should be sized according to instance capacity (HTTP connections, CPU).
- Retry count should be small (2–3) with jitter.
- Make these values configurable via the `HackerNews` section and tune in staging.

## Where to look in code
- `ServiceCollectionExtensions.AddHackerNewsHttpClient` — client setup, rate limiter and Polly wiring
- `Infrastructure/HackerNewRateLimitter.cs` — fail-fast lease acquisition
- `Repository/StoriesRepository.cs` — per-item fetch and partial result handling
- `Repository/CachedStoriesRepository.cs` — cache coalescing and stale-while-revalidate
- `Controllers/StoriesController.cs` — API surface

## Notes
- These protections are per application instance. If you have multiple instances, tune limits accordingly to avoid exceeding the upstream quota.
- Add metrics/logging (429 counts, retry attempts, bulkhead rejections, cache hits/misses) to observe and tune.
