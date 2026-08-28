using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Tests;

public sealed class TelegramDigestMessagesTests
{
    [Fact]
    public void English_message_contains_shows_with_links()
    {
        var items = new List<DigestCandidate>
        {
            new(Guid.NewGuid(), "Breaking Bad", 5, new DateOnly(2013, 9, 29), Guid.NewGuid(), DateTimeOffset.UtcNow, 169)
        };

        var text = TelegramDigestMessages.Create("en", items);

        Assert.Contains("*Seasons Ended*", text);
        Assert.Contains("Breaking Bad", text);
        Assert.Contains("Season 5", text);
        Assert.Contains("2013-09-29", text);
        Assert.Contains("/shows/169/seasons/5", text);
    }

    [Fact]
    public void Spanish_message_uses_temporada()
    {
        var items = new List<DigestCandidate>
        {
            new(Guid.NewGuid(), "The Wire", 5, new DateOnly(2008, 3, 9), Guid.NewGuid(), DateTimeOffset.UtcNow, 179)
        };

        var text = TelegramDigestMessages.Create("es", items);

        Assert.Contains("*Temporadas finalizadas*", text);
        Assert.Contains("Temporada 5", text);
        Assert.Contains("finalizo", text);
    }

    [Fact]
    public void Null_language_defaults_to_english()
    {
        var items = new List<DigestCandidate>
        {
            new(Guid.NewGuid(), "Test Show", 1, null, Guid.NewGuid(), DateTimeOffset.UtcNow, 1)
        };

        var text = TelegramDigestMessages.Create(null, items);
        Assert.Contains("*Seasons Ended*", text);
    }
}
