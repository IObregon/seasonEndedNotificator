using Microsoft.Extensions.Options;

namespace SeasonEnded.Api.Jobs;

public sealed class MetadataRefreshHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<MetadataRefreshOptions> options,
    TimeProvider timeProvider)
    : DailyJobHostedService<MetadataRefreshOptions>(scopeFactory, options, timeProvider)
{
    protected override string JobName => DailyMetadataRefreshJob.JobName;

    protected override Task RunJobAsync(
        IServiceScope scope, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var job = scope.ServiceProvider.GetRequiredService<DailyMetadataRefreshJob>();
        return job.RunAsync(Environment.MachineName, now, cancellationToken);
    }
}
