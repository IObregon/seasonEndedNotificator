using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api;

public static class ShowEndpoints
{
    public static WebApplication MapShowEndpoints(this WebApplication app)
    {
        app.MapGet("/api/shows/search", async (
            string? query,
            ITvShowSearch search,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(query))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(query)] = ["Search query is required."]
                });

            try
            {
                return Results.Ok(await search.SearchAsync(query.Trim(), cancellationToken));
            }
            catch (TvSearchRateLimitedException)
            {
                return Results.Problem("TV search rate limit reached. Try again shortly.",
                    statusCode: StatusCodes.Status429TooManyRequests);
            }
            catch (TvSearchUnavailableException)
            {
                return Results.Problem("TV search is temporarily unavailable.",
                    statusCode: StatusCodes.Status502BadGateway);
            }
        }).RequireAuthorization();

        app.MapGet("/api/shows/{providerId:int}", async (
            int providerId,
            AppDbContext db,
            ITvShowDetails provider,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var show = await new ImportShowDetailsCommand(db, provider)
                    .ExecuteAsync(providerId, cancellationToken);
                return Results.Ok(new ShowDetailsResponse(
                    show.ProviderId,
                    show.Title,
                    show.PremiereYear,
                    show.Status,
                    show.ImageUrl,
                    show.Seasons
                        .OrderBy(season => season.Number)
                        .Select(season => new SeasonResponse(
                            season.Number,
                            season.PremiereDate,
                            season.EndDate,
                            season.CompletedAt))));
            }
            catch (TvShowNotFoundException)
            {
                return Results.NotFound();
            }
            catch (HttpRequestException)
            {
                return Results.Problem("Show details are temporarily unavailable.",
                    statusCode: StatusCodes.Status502BadGateway);
            }
        }).RequireAuthorization();

        app.MapPost("/api/shows/{providerId:int}/follow", async (
            int providerId,
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            var show = await db.Shows.FirstOrDefaultAsync(item => item.ProviderId == providerId);
            if (show is null)
                return Results.NotFound();

            var result = await new FollowShowCommand(db).ExecuteAsync(userId, show.Id);
            return Results.Ok(new { followedAt = result.Follow.FollowedAt, created = result.Created });
        }).RequireAuthorization();

        app.MapDelete("/api/shows/{providerId:int}/follow", async (
            int providerId,
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            var show = await db.Shows.FirstOrDefaultAsync(item => item.ProviderId == providerId);
            if (show is not null)
                await new UnfollowShowCommand(db).ExecuteAsync(userId, show.Id);

            return Results.NoContent();
        }).RequireAuthorization();

        app.MapGet("/api/follows", async (
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            var followedShows = await db.ShowFollows
                .Where(follow => follow.UserId == userId)
                .Join(db.Shows,
                    follow => follow.ShowId,
                    show => show.Id,
                    (follow, show) => new { follow, show })
                .OrderBy(x => x.show.Title)
                .Select(x => new FollowedShowResponse(
                    x.show.ProviderId,
                    x.show.Title,
                    x.show.PremiereYear,
                    x.show.Status,
                    x.show.ImageUrl,
                    x.follow.FollowedAt))
                .ToListAsync();

            return Results.Ok(followedShows);
        }).RequireAuthorization();

        return app;
    }
}
