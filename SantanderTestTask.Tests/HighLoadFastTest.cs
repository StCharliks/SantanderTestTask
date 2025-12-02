using SantanderTestTask.Infrastructure.Entities;
using SantanderTestTask.Repository;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace SantanderTestTask.Tests
{
    public class HighLoadFastTest
    {
        [Fact]
        public async Task ManyConcurrentCallers_ShouldMeasureOutboundConcurrency()
        {
            // Arrange
            var ids = Enumerable.Range(1, 100).ToArray();

            var active = 0;
            var maxActive = 0;

            var handler = new TestHandler(async (request, ct) =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("beststories.json"))
                {
                    var json = JsonSerializer.Serialize(ids);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json)
                    };
                }

                if (request.RequestUri!.AbsolutePath.Contains("/item/"))
                {
                    Interlocked.Increment(ref active);
                    try
                    {
                        var current = Interlocked.CompareExchange(ref active, 0, 0);
                        int prev;
                        do
                        {
                            prev = maxActive;
                            if (current <= prev) break;
                        } while (Interlocked.CompareExchange(ref maxActive, current, prev) != prev);

                        // simulate remote latency
                        await Task.Delay(20, ct);

                        var story = new HackerNewsStory
                        {
                            By = "tester",
                            Descendants = 0,
                            Kids = Array.Empty<int>(),
                            Score = 1,
                            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            Title = "title",
                            Type = "story",
                            Url = "http://example.com"
                        };

                        var json = JsonSerializer.Serialize(story);
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(json)
                        };
                    }
                    finally
                    {
                        Interlocked.Decrement(ref active);
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/")
            };

            var factory = new SimpleHttpClientFactory(client);
            var repo = new StoriesRepository(factory);

            // Act
            var callers = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => repo.GetStoriesAsync()))
            .ToArray();

            await Task.WhenAll(callers);

            // Assert
            // Expect global outbound concurrency to exceed per-call semaphore(10) to demonstrate global overload risk
            Assert.True(maxActive > 10, $"Expected global concurrent outbound calls to exceed10, but was {maxActive}");
        }

        private class SimpleHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;
            public SimpleHttpClientFactory(HttpClient client) => _client = client;
            public HttpClient CreateClient(string name) => _client;
        }

        private class TestHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;
            public TestHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _responder(request, cancellationToken);
        }
    }
}
