using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using SantanderTestTask.Configuration;
using SantanderTestTask.Infrastructure;
using SantanderTestTask.Repository;
using SantanderTestTask.Services;
using System.Net.Http;
using System.Threading.RateLimiting;

namespace SantanderTestTask
{
    public static class ServiceCollectionExtensions
    {
        //TODO: get name from config
        public static IServiceCollection AddHackerNewsHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<HackerNewsConfiguration>(configuration.GetSection("HackerNewsConfiguration"));

            services.AddSingleton<RateLimiter>(x =>
            {
                var options = x.GetRequiredService<IOptions<HackerNewsConfiguration>>().Value;
                return new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = TimeSpan.FromSeconds(options.WindowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = options.QueueLimit
                });
            });

            services.AddTransient<HackerNewRateLimitter>();

            services.AddHttpClient("HackerNews", (sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<HackerNewsConfiguration>>().Value;
                client.BaseAddress = new Uri(opts.BaseAddress);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler((sp, request) => CreateRetryAndBulkheadPolicy(sp))
            .AddHttpMessageHandler<HackerNewRateLimitter>();

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CacheConfiguration>(configuration.GetSection(nameof(CacheConfiguration)));
            services.AddMemoryCache();
            services.AddScoped<IStoriesRepository, StoriesRepository>();
            services.AddScoped<IStoriesService, StoriesService>();
            services.Decorate<IStoriesRepository, CachedStoriesRepository>();

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> CreateRetryAndBulkheadPolicy(IServiceProvider sp)
        {
            var opts = sp.GetRequiredService<IOptions<HackerNewsConfiguration>>().Value;

            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(opts.PollyRetryCount, retryAttempt =>
                {
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, opts.PollyRetryJitterMsMax));
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + jitter;
                });

            var bulkheadPolicy = Policy.BulkheadAsync<HttpResponseMessage>(opts.PollyBulkheadMaxParallelization, opts.PollyBulkheadMaxQueuingActions);

            return Policy.WrapAsync(bulkheadPolicy, retryPolicy);
        }
    }
}
