using Microsoft.Extensions.Options;

namespace SeasonEnded.Api.Jobs;

public sealed class DigestHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<DigestScheduleOptions> options,
    TimeProvider timeProvider)
    : DailyJobHostedService<DigestScheduleOptions>(scopeFactory, options, timeProvider)
{
    protected override string JobName => DailyDigestJob.JobName;

    protected override Task RunJobAsync(
        IServiceScope scope, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var job = scope.ServiceProvider.GetRequiredService<DailyDigestJob>();
        return job.RunAsync(Environment.MachineName, now, cancellationToken);
    }
}
