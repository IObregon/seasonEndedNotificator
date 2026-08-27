using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class ActiveUserPolicyTests
{
    [Fact]
    public async Task Disabled_user_is_not_session_or_notification_eligible()
    {
        await using var context = CreateContext();
        var active = new User { Email = "active@example.test" };
        var disabled = new User { Email = "disabled@example.test", Status = "Disabled" };
        context.Users.AddRange(active, disabled);
        await context.SaveChangesAsync();

        var policy = new ActiveUserPolicy(context);

        Assert.True(await policy.CanUseSessionAsync(active.Id));
        Assert.False(await policy.CanUseSessionAsync(disabled.Id));
        Assert.Equal([active.Id], await policy.NotificationEligibleUserIdsAsync([active.Id, disabled.Id]));
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
