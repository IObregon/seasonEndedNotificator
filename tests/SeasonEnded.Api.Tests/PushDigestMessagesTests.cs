using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Tests;

public sealed class PushDigestMessagesTests
{
    [Fact]
    public void English_single_item_contains_title_and_season()
    {
        var items = new List<DigestCandidate>
        {
            new(Guid.NewGuid(), "Breaking Bad", 5, new DateOnly(2013, 9, 29), Guid.NewGuid(), DateTimeOffset.UtcNow, 169)
        };

        var payload = PushDigestMessages.Create("en", items);

        Assert.Contains("Seasons Ended", payload);
        Assert.Contains("Breaking Bad", payload);
        Assert.Contains("Season 5 has ended", payload);
    }

    [Fact]
    public void English_multiple_items_shows_count()
    {
        var items = new List<DigestCandidate>
        {
            new(Guid.NewGuid(), "Show A", 1, null, Guid.NewGuid(), DateTimeOffset.UtcNow, 1),
            new(Guid.NewGuid(), "Show B", 2, null, Guid.NewGuid(), DateTimeOffset.UtcNow, 2)
        };

        var payload = PushDigestMessages.Create("en", items);

        Assert.Contains("2 seasons have ended", payload);
    }

    [Fact]
    public void Spanish_single_item_uses_temporada()
    {
        var items = new List<DigestCandidate>
        {
            new(Guid.NewGuid(), "The Wire", 5, null, Guid.NewGuid(), DateTimeOffset.UtcNow, 179)
        };

        var payload = PushDigestMessages.Create("es", items);

        Assert.Contains("Temporada 5", payload);
        Assert.Contains("finalizado", payload);
    }

    [Fact]
    public void Payload_escapes_quotes_in_show_title()
    {
        var items = new List<DigestCandidate>
        {
            new(Guid.NewGuid(), "Show \"Quoted\"", 1, null, Guid.NewGuid(), DateTimeOffset.UtcNow, 1)
        };

        var payload = PushDigestMessages.Create("en", items);

        Assert.DoesNotContain("\"Quoted\"", payload);
        Assert.Contains("Quoted", payload);
        Assert.DoesNotContain($"\"body\":\"Show \"", payload);
    }
}
