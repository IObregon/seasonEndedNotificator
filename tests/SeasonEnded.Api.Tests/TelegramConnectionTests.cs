using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Tests;

public sealed class TelegramConnectionTests
{
    [Fact]
    public async Task Create_link_generates_deep_link()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await new CreateTelegramLinkCommand(context)
            .ExecuteAsync(user.Id, "TestBot");

        Assert.Contains("https://t.me/TestBot?start=", result.DeepLink);

        var token = await context.TelegramConnectionTokens.SingleAsync();
        Assert.Equal("Pending", token.Status);
        Assert.True(token.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Create_link_revokes_previous_pending_token()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await new CreateTelegramLinkCommand(context).ExecuteAsync(user.Id, "TestBot");
        await new CreateTelegramLinkCommand(context).ExecuteAsync(user.Id, "TestBot");

        var tokens = await context.TelegramConnectionTokens.ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.Equal("Revoked", tokens[0].Status);
        Assert.Equal("Pending", tokens[1].Status);
    }

    [Fact]
    public async Task Consume_token_binds_destination()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var linkResult = await new CreateTelegramLinkCommand(context)
            .ExecuteAsync(user.Id, "TestBot");

        var rawToken = linkResult.DeepLink.Split('=')[1];
        var result = await new ConsumeTelegramTokenCommand(context)
            .ExecuteAsync(rawToken, 12345, DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded);
        var dest = await context.TelegramDestinations.SingleAsync();
        Assert.Equal(user.Id, dest.UserId);
        Assert.Equal(12345, dest.ChatId);

        var token = await context.TelegramConnectionTokens.SingleAsync(t => t.Status == "Consumed");
        Assert.Equal("Consumed", token.Status);
    }

    [Fact]
    public async Task Expired_token_is_not_consumed()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var linkResult = await new CreateTelegramLinkCommand(context)
            .ExecuteAsync(user.Id, "TestBot");

        var rawToken = linkResult.DeepLink.Split('=')[1];
        var pastTime = DateTimeOffset.UtcNow.AddMinutes(20);
        var result = await new ConsumeTelegramTokenCommand(context)
            .ExecuteAsync(rawToken, 12345, pastTime);

        Assert.False(result.Succeeded);
        Assert.Equal(0, await context.TelegramDestinations.CountAsync());
    }

    [Fact]
    public async Task Already_consumed_token_is_rejected()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var linkResult = await new CreateTelegramLinkCommand(context)
            .ExecuteAsync(user.Id, "TestBot");

        var rawToken = linkResult.DeepLink.Split('=')[1];
        await new ConsumeTelegramTokenCommand(context)
            .ExecuteAsync(rawToken, 12345, DateTimeOffset.UtcNow);

        var result = await new ConsumeTelegramTokenCommand(context)
            .ExecuteAsync(rawToken, 12345, DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        var destCount = await context.TelegramDestinations.CountAsync();
        Assert.Equal(1, destCount);
    }

    [Fact]
    public async Task Already_connected_user_does_not_get_duplicate()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        context.TelegramDestinations.Add(new TelegramDestination
        {
            UserId = user.Id,
            ChatId = 99999
        });
        await context.SaveChangesAsync();

        var linkResult = await new CreateTelegramLinkCommand(context)
            .ExecuteAsync(user.Id, "TestBot");

        var rawToken = linkResult.DeepLink.Split('=')[1];
        var result = await new ConsumeTelegramTokenCommand(context)
            .ExecuteAsync(rawToken, 12345, DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded);
        var dest = await context.TelegramDestinations.SingleAsync();
        Assert.Equal(99999, dest.ChatId);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
