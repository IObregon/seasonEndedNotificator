using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class MagicLinkCommandTests
{
    [Fact]
    public async Task Request_sends_email_to_active_user_only()
    {
        await using var context = CreateInMemoryContext();
        context.Users.Add(new User { Email = "active@example.test", Status = "Active" });
        context.Users.Add(new User { Email = "disabled@example.test", Status = "Disabled" });
        await context.SaveChangesAsync();
        var sender = new TestEmailSender();

        var command = new RequestMagicLinkCommand(context, sender, "https://season-ended.localhost");
        await command.ExecuteAsync("active@example.test");

        Assert.NotNull(sender.SentMessage);
        Assert.Equal("active@example.test", sender.SentMessage!.To);
    }

    [Fact]
    public async Task Request_for_unknown_user_sends_nothing()
    {
        await using var context = CreateInMemoryContext();
        var sender = new TestEmailSender();
        var command = new RequestMagicLinkCommand(context, sender, "https://season-ended.localhost");

        await command.ExecuteAsync("nobody@example.test");

        Assert.Null(sender.SentMessage);
        Assert.Empty(context.MagicLinkTokens);
    }

    [Fact]
    public async Task Request_for_disabled_user_sends_nothing()
    {
        await using var context = CreateInMemoryContext();
        context.Users.Add(new User { Email = "disabled@example.test", Status = "Disabled" });
        await context.SaveChangesAsync();
        var sender = new TestEmailSender();
        var command = new RequestMagicLinkCommand(context, sender, "https://season-ended.localhost");

        await command.ExecuteAsync("disabled@example.test");

        Assert.Null(sender.SentMessage);
        Assert.Empty(context.MagicLinkTokens);
    }

    [Fact]
    public async Task Raw_token_is_not_persisted_only_hash()
    {
        await using var context = CreateInMemoryContext();
        context.Users.Add(new User { Email = "active@example.test", Status = "Active" });
        await context.SaveChangesAsync();
        var sender = new TestEmailSender();
        var command = new RequestMagicLinkCommand(context, sender, "https://season-ended.localhost");

        var result = await command.ExecuteAsync("active@example.test");

        Assert.NotNull(result.RawToken);
        var token = await context.MagicLinkTokens.SingleAsync();
        Assert.NotEqual(result.RawToken, token.TokenHash);
    }

    [Fact]
    public async Task Consume_valid_token_creates_session()
    {
        await using var context = CreateInMemoryContext();
        var user = new User { Email = "active@example.test", Status = "Active" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var sender = new TestEmailSender();
        var requestCmd = new RequestMagicLinkCommand(context, sender, "https://season-ended.localhost");
        var result = await requestCmd.ExecuteAsync("active@example.test");

        var consumeCmd = new ConsumeMagicLinkCommand(context);
        var consumeResult = await consumeCmd.ExecuteAsync(result.RawToken!);

        Assert.True(consumeResult.Succeeded);
        Assert.Equal(user.Id, consumeResult.UserId);
    }

    [Fact]
    public async Task Consume_replayed_token_fails()
    {
        await using var context = CreateInMemoryContext();
        var user = new User { Email = "active@example.test", Status = "Active" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var sender = new TestEmailSender();
        var requestCmd = new RequestMagicLinkCommand(context, sender, "https://season-ended.localhost");
        var result = await requestCmd.ExecuteAsync("active@example.test");

        var consumeCmd = new ConsumeMagicLinkCommand(context);
        await consumeCmd.ExecuteAsync(result.RawToken!);
        var second = await consumeCmd.ExecuteAsync(result.RawToken!);

        Assert.False(second.Succeeded);
    }

    [Fact]
    public async Task Consume_expired_token_fails()
    {
        await using var context = CreateInMemoryContext();
        var user = new User { Email = "active@example.test", Status = "Active" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var rawToken = "EXPIREDTOKEN";
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        context.MagicLinkTokens.Add(new MagicLinkToken
        {
            TokenHash = tokenHash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            Status = "Pending"
        });
        await context.SaveChangesAsync();

        var consumeCmd = new ConsumeMagicLinkCommand(context);
        var result = await consumeCmd.ExecuteAsync(rawToken);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Consume_token_for_disabled_user_fails()
    {
        await using var context = CreateInMemoryContext();
        var user = new User { Email = "active@example.test", Status = "Active" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var sender = new TestEmailSender();
        var requestCmd = new RequestMagicLinkCommand(context, sender, "https://season-ended.localhost");
        var result = await requestCmd.ExecuteAsync("active@example.test");

        user.Status = "Disabled";
        await context.SaveChangesAsync();

        var consumeCmd = new ConsumeMagicLinkCommand(context);
        var consumeResult = await consumeCmd.ExecuteAsync(result.RawToken!);

        Assert.False(consumeResult.Succeeded);
    }

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
