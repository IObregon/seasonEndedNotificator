using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Tests;

public sealed class DisconnectTelegramCommandTests
{
    [Fact]
    public async Task Disconnect_removes_destination_and_disables_preference()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test", TelegramNotificationsEnabled = true };
        context.Users.Add(user);
        context.TelegramDestinations.Add(new TelegramDestination { UserId = user.Id, ChatId = 12345 });
        await context.SaveChangesAsync();

        var result = await new DisconnectTelegramCommand(context).ExecuteAsync(user.Id);

        Assert.True(result);
        Assert.Null(await context.TelegramDestinations.FirstOrDefaultAsync(d => d.UserId == user.Id));
        Assert.False(user.TelegramNotificationsEnabled);
    }

    [Fact]
    public async Task Disconnect_is_idempotent()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await new DisconnectTelegramCommand(context).ExecuteAsync(user.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task Disconnect_preserves_delivery_history()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test", TelegramNotificationsEnabled = true };
        context.Users.Add(user);
        context.TelegramDestinations.Add(new TelegramDestination { UserId = user.Id, ChatId = 12345 });
        var delivery = new DigestDelivery { UserId = user.Id, Channel = "Telegram", Status = "Sent", DigestDate = DateOnly.FromDateTime(DateTime.UtcNow) };
        context.DigestDeliveries.Add(delivery);
        await context.SaveChangesAsync();

        await new DisconnectTelegramCommand(context).ExecuteAsync(user.Id);

        var deliveries = await context.DigestDeliveries.ToListAsync();
        Assert.Single(deliveries);
        Assert.Equal("Sent", deliveries[0].Status);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
