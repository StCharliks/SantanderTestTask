using System.Threading.RateLimiting;

namespace SantanderTestTask.Infrastructure
{
    public class HackerNewRateLimitter : DelegatingHandler
    {
        private readonly RateLimiter _rateLimiter;

        public HackerNewRateLimitter(RateLimiter rateLimiter)
        {
           _rateLimiter = rateLimiter;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var lease = await _rateLimiter.AcquireAsync(1, cancellationToken);
            if (lease.IsAcquired)
            {
                return await base.SendAsync(request, cancellationToken);
            }
            else
            {
                throw new HttpRequestException("Rate limit exceeded");
            }
        }
    }
}
