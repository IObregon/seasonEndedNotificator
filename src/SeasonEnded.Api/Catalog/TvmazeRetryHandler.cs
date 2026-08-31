using System.Net;

namespace SeasonEnded.Api.Catalog;

public sealed class TvmazeRetryHandler(IRetryDelay delay) : DelegatingHandler
{
    private static readonly TimeSpan[] BackoffDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8)
    ];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            var response = await base.SendAsync(request, cancellationToken);
            var shouldRetry = response.StatusCode == HttpStatusCode.TooManyRequests ||
                (int)response.StatusCode >= 500;
            if (!shouldRetry || attempt >= BackoffDelays.Length)
                return response;

            var retryAfter = response.Headers.RetryAfter?.Delta ?? BackoffDelays[attempt];
            response.Dispose();
            await delay.WaitAsync(retryAfter, cancellationToken);
            attempt++;
        }
    }
}

public interface IRetryDelay
{
    Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken);
}

public sealed class RetryDelay : IRetryDelay
{
    public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);
}
