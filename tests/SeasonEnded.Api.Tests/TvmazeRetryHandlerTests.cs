using System.Net;
using SeasonEnded.Api.Catalog;

namespace SeasonEnded.Api.Tests;

public sealed class TvmazeRetryHandlerTests
{
    [Fact]
    public async Task Retries_rate_limit_once_then_returns_success()
    {
        var inner = new SequenceHandler(
            new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            new HttpResponseMessage(HttpStatusCode.OK));
        var delays = new RecordingDelay();
        var client = new HttpClient(new TvmazeRetryHandler(delays) { InnerHandler = inner });

        var response = await client.GetAsync("https://api.tvmaze.com/shows/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.Calls);
        Assert.Single(delays.Delays);
    }

    private sealed class SequenceHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private int index;
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(responses[index++]);
        }
    }

    private sealed class RecordingDelay : IRetryDelay
    {
        public List<TimeSpan> Delays { get; } = [];

        public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            return Task.CompletedTask;
        }
    }
}
