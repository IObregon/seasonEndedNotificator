using System.Net;
using System.Text;
using SeasonEnded.Api.Catalog;

namespace SeasonEnded.Api.Tests;

public sealed class TvmazeShowDetailsTests
{
    private static readonly TimeProvider Time = new FixedTimeProvider(
        new DateTimeOffset(2026, 9, 25, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Normalizes_show_and_excludes_season_zero()
    {
        var handler = new RouteHandler(new Dictionary<string, string>
        {
            ["/shows/82"] = """
                {"id":82,"name":"Game of Thrones","status":"Ended","premiered":"2011-04-17","image":{"medium":"show.jpg"},"_embedded":{"episodes":[{"id":1,"season":1,"type":"regular","airdate":"2011-06-19"},{"id":2,"season":2,"type":"regular","airdate":"2026-10-06"},{"id":3,"season":2,"type":"regular","airdate":"2026-10-13"},{"id":4,"season":2,"type":"special","airdate":"2027-01-01"},{"id":5,"season":0,"type":"regular","airdate":"2028-01-01"}]}}
                """,
            ["/shows/82/seasons"] = """
                [{"id":1,"number":0,"premiereDate":"2010-01-01","endDate":"2010-01-02"},{"id":2,"number":1,"premiereDate":"2011-04-17","endDate":"2011-06-19"},{"id":3,"number":2,"premiereDate":null,"endDate":null}]
                """
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.tvmaze.com") };
        var details = new TvmazeShowDetails(client, Time);

        var show = await details.GetAsync(82, CancellationToken.None);

        Assert.Equal("Game of Thrones", show.Title);
        Assert.Equal(2011, show.PremiereYear);
        Assert.Equal(new DateOnly(2026, 10, 13), show.CurrentSeasonLatestEpisodeDate);
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

    [Fact]
    public async Task Falls_back_to_previous_season_when_current_season_episodes_have_no_date()
    {
        var handler = new RouteHandler(new Dictionary<string, string>
        {
            ["/shows/82"] = """
                {"id":82,"name":"Pending","status":"Running","premiered":"2024-01-01","_embedded":{"episodes":[{"id":1,"season":2,"type":"regular","airdate":null},{"id":2,"season":1,"type":"regular","airdate":"2026-11-01"},{"id":3,"season":1,"type":"regular","airdate":"2026-11-08"}]}}
                """,
            ["/shows/82/seasons"] = """
                [{"id":10,"number":1,"premiereDate":"2024-01-01","endDate":null},{"id":11,"number":2,"premiereDate":null,"endDate":null}]
                """
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.tvmaze.com") };
        var details = new TvmazeShowDetails(client, Time);

        var show = await details.GetAsync(82, CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 11, 8), show.CurrentSeasonLatestEpisodeDate);
    }

    [Fact]
    public async Task Returns_null_when_season_episodes_are_all_aired()
    {
        var handler = new RouteHandler(new Dictionary<string, string>
        {
            ["/shows/82"] = """
                {"id":82,"name":"Aired","status":"Running","premiered":"2020-01-01","_embedded":{"episodes":[{"id":1,"season":2,"type":"regular","airdate":"2020-06-01"},{"id":2,"season":1,"type":"regular","airdate":"2020-01-15"}]}}
                """,
            ["/shows/82/seasons"] = """
                [{"id":10,"number":1,"premiereDate":"2020-01-15","endDate":"2020-05-01"},{"id":11,"number":2,"premiereDate":"2020-06-01","endDate":"2020-09-01"}]
                """
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.tvmaze.com") };
        var details = new TvmazeShowDetails(client, Time);

        var show = await details.GetAsync(82, CancellationToken.None);

        Assert.Null(show.CurrentSeasonLatestEpisodeDate);
    }

    [Fact]
    public async Task Returns_null_when_current_season_has_no_dated_episode()
    {
        var handler = new RouteHandler(new Dictionary<string, string>
        {
            ["/shows/82"] = """
                {"id":82,"name":"Game of Thrones","status":"Running","premiered":"2011-04-17","_embedded":{"episodes":[{"id":1,"season":2,"type":"regular","airdate":"2017-07-16"}]}}
                """,
            ["/shows/82/seasons"] = """
                [{"id":2,"number":1,"premiereDate":"2011-04-17","endDate":"2011-06-19"},{"id":3,"number":2,"premiereDate":null,"endDate":null},{"id":4,"number":3,"premiereDate":null,"endDate":null}]
                """
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.tvmaze.com") };
        var details = new TvmazeShowDetails(client, Time);

        var show = await details.GetAsync(82, CancellationToken.None);

        Assert.Null(show.CurrentSeasonLatestEpisodeDate);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
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
