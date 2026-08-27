using System.Net;

namespace SeasonEnded.Api.Catalog;

public sealed class TvmazeRetryHandler(IRetryDelay delay) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.TooManyRequests &&
            (int)response.StatusCode < 500)
            return response;

        var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
        response.Dispose();
        await delay.WaitAsync(retryAfter, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
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
