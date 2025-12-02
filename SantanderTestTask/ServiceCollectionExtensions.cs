using SantanderTestTask.Infrastructure;
using SantanderTestTask.Repository;
using SantanderTestTask.Services;
using System.Threading.RateLimiting;

namespace SantanderTestTask
{
    public static class ServiceCollectionExtensions
    {
        //TODO: get name from config
        public static IServiceCollection AddHackerNewsHttpClient(this IServiceCollection services)
        {
            services.AddSingleton<RateLimiter>(x =>
                new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromSeconds(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 100
                })
            );

            services.AddTransient<HackerNewRateLimitter>();

            services.AddHttpClient("HackerNews", client =>
            {
                client.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddHttpMessageHandler<HackerNewRateLimitter>();

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddScoped<IStoriesRepository, StoriesRepository>();
            services.AddScoped<IStoriesService, StoriesService>();
            services.Decorate<IStoriesRepository, CachedStoriesRepository>();

            return services;
        }
    }
}
