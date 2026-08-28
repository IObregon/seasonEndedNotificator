using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Tests;

public sealed class DigestMessagesTests
{
    [Fact]
    public void English_digest_contains_show_title_season_number_and_link()
    {
        var items = new List<DigestCandidate>
        {
            new(Guid.NewGuid(), "Breaking Bad", 5, new DateOnly(2013, 9, 29), Guid.NewGuid(), DateTimeOffset.UtcNow, 169)
        };

        var message = DigestMessages.Create("en", "user@example.test", items);

        Assert.Equal("user@example.test", message.To);
        Assert.Equal("Seasons Ended", message.Subject);
        Assert.Contains("Breaking Bad", message.TextBody);
        Assert.Contains("Season 5", message.TextBody);
        Assert.Contains("2013-09-29", message.TextBody);
        Assert.Contains("/shows/169/seasons/5", message.TextBody);
        Assert.Contains("<ul>", message.HtmlBody);
        Assert.Contains("Breaking Bad", message.HtmlBody);
    }

    [Fact]
    public void Spanish_digest_uses_temporada()
    {
        var items = new List<DigestCandidate>
        {
            new(Guid.NewGuid(), "The Wire", 5, new DateOnly(2008, 3, 9), Guid.NewGuid(), DateTimeOffset.UtcNow, 179)
        };

        var message = DigestMessages.Create("es", "user@example.test", items);

        Assert.Equal("Temporadas finalizadas", message.Subject);
        Assert.Contains("The Wire", message.TextBody);
        Assert.Contains("Temporada 5", message.TextBody);
        Assert.Contains("finalizo", message.TextBody);
    }

    [Fact]
    public void Null_language_defaults_to_english()
    {
        var items = new List<DigestCandidate>
        {
            new(Guid.NewGuid(), "Test Show", 1, new DateOnly(2024, 1, 1), Guid.NewGuid(), DateTimeOffset.UtcNow, 1)
        };

        var message = DigestMessages.Create(null, "user@example.test", items);
        Assert.Equal("Seasons Ended", message.Subject);
    }

    [Fact]
    public void Multiple_items_are_listed()
    {
        var items = new List<DigestCandidate>
        {
            new(Guid.NewGuid(), "Show A", 1, new DateOnly(2024, 1, 1), Guid.NewGuid(), DateTimeOffset.UtcNow, 1),
            new(Guid.NewGuid(), "Show B", 2, new DateOnly(2024, 2, 1), Guid.NewGuid(), DateTimeOffset.UtcNow, 2)
        };

        var message = DigestMessages.Create("en", "user@example.test", items);

        Assert.Contains("Show A", message.TextBody);
        Assert.Contains("Show B", message.TextBody);
        Assert.Contains("Season 1", message.TextBody);
        Assert.Contains("Season 2", message.TextBody);
    }

    [Fact]
    public void Missing_end_date_shows_unknown()
    {
        var items = new List<DigestCandidate>
        {
            new(Guid.NewGuid(), "Test Show", 1, null, Guid.NewGuid(), DateTimeOffset.UtcNow, 1)
        };

        var message = DigestMessages.Create("en", "user@example.test", items);
        Assert.Contains("unknown", message.TextBody);
    }
}
