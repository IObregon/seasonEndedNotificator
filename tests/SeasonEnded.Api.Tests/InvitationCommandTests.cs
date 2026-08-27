using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class InvitationCommandTests
{
    [Fact]
    public async Task Creates_invitation_with_default_user_role_and_hashed_token()
    {
        using var context = CreateInMemoryContext();
        var sender = new TestEmailSender();
        var command = new InviteUserCommand(context, sender);

        var result = await command.ExecuteAsync("admin@localhost", "newuser@example.test");

        Assert.True(result.Created);
        var invitation = await context.Invitations.SingleAsync();
        Assert.Equal("newuser@example.test", invitation.Email);
        Assert.Equal(UserRole.User, invitation.Role);
        Assert.Equal("Pending", invitation.Status);
        Assert.NotEmpty(invitation.TokenHash);
        Assert.NotEqual(result.RawToken, invitation.TokenHash);
        Assert.True(invitation.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Duplicate_active_invitation_returns_existing_without_creating_new_one()
    {
        using var context = CreateInMemoryContext();
        var sender = new TestEmailSender();
        var command = new InviteUserCommand(context, sender);

        await command.ExecuteAsync("admin@localhost", "newuser@example.test");
        var second = await command.ExecuteAsync("admin@localhost", "newuser@example.test");

        Assert.False(second.Created);
        Assert.Single(context.Invitations);
    }

    [Fact]
    public async Task Sends_invitation_email_with_raw_token()
    {
        using var context = CreateInMemoryContext();
        var sender = new TestEmailSender();
        var command = new InviteUserCommand(context, sender);

        var result = await command.ExecuteAsync("admin@localhost", "newuser@example.test");

        Assert.NotNull(sender.SentMessage);
        Assert.Equal("newuser@example.test", sender.SentMessage!.To);
        Assert.Contains(result.RawToken, sender.SentMessage.TextBody);
        Assert.Contains(result.RawToken, sender.SentMessage.HtmlBody);
    }

    [Fact]
    public async Task Rejects_blank_email()
    {
        using var context = CreateInMemoryContext();
        var sender = new TestEmailSender();
        var command = new InviteUserCommand(context, sender);

        var act = async () => await command.ExecuteAsync("admin@localhost", "");

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
