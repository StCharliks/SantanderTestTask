using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using SantanderTestTask.Configuration;

namespace SantanderTestTask.Infrastructure
{
    public class HackerNewRateLimitter : DelegatingHandler
    {
        private readonly RateLimiter _rateLimiter;
        private readonly TimeSpan _acquireTimeout;

        public HackerNewRateLimitter(RateLimiter rateLimiter, IOptions<HackerNewsConfiguration> options)
        {
            _rateLimiter = rateLimiter;
            _acquireTimeout = TimeSpan.FromMilliseconds(options.Value.AcquireTimeoutMs);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // create a linked token that cancels after a short timeout to fail fast
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_acquireTimeout);

            RateLimitLease? lease = null;
            try
            {
                lease = await _rateLimiter.AcquireAsync(1, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return CreateTooManyRequestsResponse(request);
            }

            if (lease != null && lease.IsAcquired)
            {
                return await base.SendAsync(request, cancellationToken);
            }
            else
            {
                return CreateTooManyRequestsResponse(request);
            }
        }

        private static HttpResponseMessage CreateTooManyRequestsResponse(HttpRequestMessage request)
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                RequestMessage = request,
                ReasonPhrase = "Rate limit exceeded"
            };

            return response;
        }
    }
}
