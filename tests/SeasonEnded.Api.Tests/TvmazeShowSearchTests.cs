using System.Net;
using System.Text;
using SeasonEnded.Api.Catalog;

namespace SeasonEnded.Api.Tests;

public sealed class TvmazeShowSearchTests
{
    [Fact]
    public async Task Maps_provider_payload_to_show_search_result()
    {
        const string payload = """
            [{"score":1,"show":{"id":82,"name":"Game of Thrones","premiered":"2011-04-17","status":"Ended","image":{"medium":"https://img.test/got.jpg"},"providerOnly":"ignored"}}]
            """;
        var client = new HttpClient(new StubHandler(HttpStatusCode.OK, payload))
        {
            BaseAddress = new Uri("https://api.tvmaze.com")
        };
        var search = new TvmazeShowSearch(client);

        var results = await search.SearchAsync("Game of Thrones", CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal(82, result.ProviderId);
        Assert.Equal("Game of Thrones", result.Title);
        Assert.Equal(2011, result.PremiereYear);
        Assert.Equal("Ended", result.Status);
        Assert.Equal("https://img.test/got.jpg", result.ImageUrl);
    }

    [Fact]
    public async Task Empty_provider_result_maps_to_empty_result()
    {
        var search = CreateSearch(HttpStatusCode.OK, "[]");

        var results = await search.SearchAsync("missing", CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Rate_limit_has_distinct_failure()
    {
        var search = CreateSearch(HttpStatusCode.TooManyRequests, "{}");

        var act = () => search.SearchAsync("show", CancellationToken.None);

        await Assert.ThrowsAsync<TvSearchRateLimitedException>(act);
    }

    [Fact]
    public async Task Provider_failure_has_distinct_failure()
    {
        var search = CreateSearch(HttpStatusCode.BadGateway, "{}");

        var act = () => search.SearchAsync("show", CancellationToken.None);

        await Assert.ThrowsAsync<TvSearchUnavailableException>(act);
    }

    private static TvmazeShowSearch CreateSearch(HttpStatusCode statusCode, string content) =>
        new(new HttpClient(new StubHandler(statusCode, content))
        {
            BaseAddress = new Uri("https://api.tvmaze.com")
        });

    private sealed class StubHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        });
    }
}
