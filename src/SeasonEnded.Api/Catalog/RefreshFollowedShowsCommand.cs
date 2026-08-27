using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Catalog;

public sealed class RefreshFollowedShowsCommand(
    AppDbContext context,
    ITvShowDetails provider) : IFollowedShowRefresh
{
    public async Task<RefreshFollowedShowsResult> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var providerIds = await context.ShowFollows
            .Join(context.Shows.Where(show => show.Status == "Running"),
                follow => follow.ShowId,
                show => show.Id,
                (_, show) => show.ProviderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var refreshed = 0;
        var failed = 0;
        foreach (var providerId in providerIds)
        {
            try
            {
                await new ImportShowDetailsCommand(context, provider)
                    .ExecuteAsync(providerId, cancellationToken);
                refreshed++;
            }
            catch (HttpRequestException)
            {
                failed++;
            }
        }

        return new RefreshFollowedShowsResult(refreshed, failed);
    }
}

public sealed record RefreshFollowedShowsResult(int Refreshed, int Failed);

public interface IFollowedShowRefresh
{
    Task<RefreshFollowedShowsResult> ExecuteAsync(CancellationToken cancellationToken);
}
