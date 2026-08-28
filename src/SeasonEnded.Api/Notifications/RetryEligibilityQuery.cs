using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Notifications;

public sealed class RetryEligibilityQuery(AppDbContext context)
{
    public async Task<List<DigestDelivery>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        return await context.DigestDeliveries
            .Where(d => d.Status == "Failed" && d.NextAttemptAt <= now)
            .OrderBy(d => d.NextAttemptAt)
            .ToListAsync(cancellationToken);
    }
}
