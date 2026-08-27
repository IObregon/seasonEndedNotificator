using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class LanguagePreferenceTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("es")]
    public async Task Active_user_can_set_supported_language(string language)
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var command = new SetUserLanguageCommand(context);
        var changed = await command.ExecuteAsync(user.Id, language);

        Assert.True(changed);
        Assert.Equal(language, user.PreferredLanguage);
    }

    [Fact]
    public async Task Unsupported_language_is_rejected()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var command = new SetUserLanguageCommand(context);

        var act = () => command.ExecuteAsync(user.Id, "fr");

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
