using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Tests;

public sealed class EmailPreferenceTests
{
    [Fact]
    public async Task Unset_preference_defaults_enabled_and_can_be_toggled()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var preferences = new EmailPreferenceService(context);

        Assert.True(await preferences.IsEnabledAsync(user.Id));
        Assert.True(await preferences.SetAsync(user.Id, enabled: false));
        Assert.False(await preferences.IsEnabledAsync(user.Id));
        Assert.Null(user.PreferredLanguage);
    }

    [Fact]
    public async Task Recipient_selection_excludes_disabled_preference_and_inactive_users()
    {
        await using var context = CreateContext();
        var enabled = new User { Email = "enabled@example.test", EmailNotificationsEnabled = true };
        var defaulted = new User { Email = "default@example.test" };
        var optedOut = new User { Email = "disabled@example.test", EmailNotificationsEnabled = false };
        var inactive = new User { Email = "inactive@example.test", Status = "Disabled" };
        context.Users.AddRange(enabled, defaulted, optedOut, inactive);
        await context.SaveChangesAsync();

        var recipients = await new EmailRecipientQuery(context).GetAsync();

        Assert.Equal(["default@example.test", "enabled@example.test"],
            recipients.Select(user => user.Email).OrderBy(email => email));
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
