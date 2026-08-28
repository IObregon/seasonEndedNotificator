using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Tests;

public sealed class DigestPreviewMessagesTests
{
    [Theory]
    [InlineData("en", "[PREVIEW] Seasons Ended", "The following seasons have ended", "PREVIEW")]
    [InlineData("es", "[VISTA PREVIA] Temporadas finalizadas", "Las siguientes temporadas han finalizado", "VISTA PREVIA")]
    [InlineData(null, "[PREVIEW] Seasons Ended", "The following seasons have ended", "PREVIEW")]
    [InlineData("unsupported", "[PREVIEW] Seasons Ended", "The following seasons have ended", "PREVIEW")]
    public void Create_produces_localized_preview_with_english_fallback(
        string? language,
        string expectedSubject,
        string expectedText,
        string expectedPreviewMark)
    {
        var message = DigestPreviewMessages.Create(language, "admin@example.test");

        Assert.Equal("admin@example.test", message.To);
        Assert.Equal(expectedSubject, message.Subject);
        Assert.Contains(expectedText, message.TextBody);
        Assert.Contains(expectedPreviewMark, message.TextBody);
        Assert.Contains(expectedPreviewMark, message.HtmlBody);
    }

    [Fact]
    public void English_preview_contains_sample_items_with_internal_links()
    {
        var message = DigestPreviewMessages.Create("en", "admin@example.test");

        Assert.Contains("Breaking Bad", message.TextBody);
        Assert.Contains("Season 5", message.TextBody);
        Assert.Contains("/shows/169/seasons/5", message.TextBody);
        Assert.Contains("The Wire", message.TextBody);
        Assert.Contains("2013-09-29", message.TextBody);
    }

    [Fact]
    public void Spanish_preview_contains_sample_items_with_season_translated()
    {
        var message = DigestPreviewMessages.Create("es", "admin@example.test");

        Assert.Contains("Breaking Bad", message.TextBody);
        Assert.Contains("Temporada 5", message.TextBody);
        Assert.Contains("/shows/169/seasons/5", message.TextBody);
    }

    [Fact]
    public void Html_body_contains_structured_markup()
    {
        var message = DigestPreviewMessages.Create("en", "admin@example.test");

        Assert.Contains("<ul>", message.HtmlBody);
        Assert.Contains("</ul>", message.HtmlBody);
        Assert.Contains("<h2>", message.HtmlBody);
    }

    [Fact]
    public void Html_body_contains_localized_heading()
    {
        var enMessage = DigestPreviewMessages.Create("en", "admin@example.test");
        var esMessage = DigestPreviewMessages.Create("es", "admin@example.test");

        Assert.Contains("<h2>Seasons Ended</h2>", enMessage.HtmlBody);
        Assert.Contains("<h2>Temporadas finalizadas</h2>", esMessage.HtmlBody);
    }
}
