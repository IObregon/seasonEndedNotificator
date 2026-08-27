using System.Net;
using System.Text;
using SeasonEnded.Api.Catalog;

namespace SeasonEnded.Api.Tests;

public sealed class TvmazeShowDetailsTests
{
    [Fact]
    public async Task Normalizes_show_and_excludes_season_zero()
    {
        var handler = new RouteHandler(new Dictionary<string, string>
        {
            ["/shows/82"] = """
                {"id":82,"name":"Game of Thrones","status":"Ended","premiered":"2011-04-17","image":{"medium":"show.jpg"}}
                """,
            ["/shows/82/seasons"] = """
                [{"id":1,"number":0,"premiereDate":"2010-01-01","endDate":"2010-01-02"},{"id":2,"number":1,"premiereDate":"2011-04-17","endDate":"2011-06-19"},{"id":3,"number":2,"premiereDate":null,"endDate":null}]
                """
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.tvmaze.com") };
        var details = new TvmazeShowDetails(client);

        var show = await details.GetAsync(82, CancellationToken.None);

        Assert.Equal("Game of Thrones", show.Title);
        Assert.Equal(2011, show.PremiereYear);
        Assert.Collection(show.Seasons,
            season =>
            {
                Assert.Equal(1, season.Number);
                Assert.Equal(new DateOnly(2011, 4, 17), season.PremiereDate);
                Assert.Equal(new DateOnly(2011, 6, 19), season.EndDate);
            },
            season =>
            {
                Assert.Equal(2, season.Number);
                Assert.Null(season.PremiereDate);
                Assert.Null(season.EndDate);
            });
    }

    private sealed class RouteHandler(IReadOnlyDictionary<string, string> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(
                responses.TryGetValue(request.RequestUri!.AbsolutePath, out var payload)
                    ? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    }
                    : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
