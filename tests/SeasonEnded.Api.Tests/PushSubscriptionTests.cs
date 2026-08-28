using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Tests;

public sealed class PushSubscriptionTests
{
    [Fact]
    public async Task New_subscription_is_stored()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var sub = new PushSubscription
        {
            UserId = user.Id,
            Endpoint = "https://push.example.com/sub/1",
            P256DH = "key1",
            Auth = "auth1"
        };
        context.PushSubscriptions.Add(sub);
        await context.SaveChangesAsync();

        var stored = await context.PushSubscriptions.SingleAsync();
        Assert.Equal(user.Id, stored.UserId);
        Assert.Equal("https://push.example.com/sub/1", stored.Endpoint);
        Assert.True(stored.Active);
    }

    [Fact]
    public void Endpoint_unique_index_is_configured()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(PushSubscription))!;
        var endpointIndex = entityType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == "Endpoint"));

        Assert.NotNull(endpointIndex);
        Assert.True(endpointIndex.IsUnique);
    }

    [Fact]
    public async Task Revoked_device_is_marked_inactive()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        var sub = new PushSubscription
        {
            UserId = user.Id,
            Endpoint = "https://push.example.com/sub/1",
            P256DH = "key1",
            Auth = "auth1"
        };
        context.PushSubscriptions.Add(sub);
        await context.SaveChangesAsync();

        sub.Active = false;
        await context.SaveChangesAsync();

        var stored = await context.PushSubscriptions.SingleAsync();
        Assert.False(stored.Active);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
