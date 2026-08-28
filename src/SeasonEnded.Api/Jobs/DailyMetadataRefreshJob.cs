using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Jobs;

public sealed class DailyMetadataRefreshJob(AppDbContext context, IFollowedShowRefresh refresh)
    : DailyJob(context)
{
    public const string JobName = "daily-metadata-refresh";

    protected override string LeaseKey => JobName;

    public async Task<DailyJobResult> RunAsync(
        string owner, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var execution = await AcquireLeaseAndStartAsync(owner, now, cancellationToken);
        if (IsLeaseUnavailable(execution))
            return DailyJobResult.LeaseUnavailable;

        try
        {
            var result = await refresh.ExecuteAsync(cancellationToken);
            await FinishAsync(execution!.Id, now,
                result.Failed == 0 ? "Completed" : "CompletedWithFailures",
                result.Refreshed, result.Failed, cancellationToken);
            return DailyJobResult.Completed;
        }
        catch
        {
            await FinishAsync(execution!.Id, now, "Failed", 0, 0, CancellationToken.None);
            throw;
        }
    }
}

public enum DailyJobResult
{
    Completed,
    LeaseUnavailable
}
