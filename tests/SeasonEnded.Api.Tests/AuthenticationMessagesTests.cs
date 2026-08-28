using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class AuthenticationMessagesTests
{
    [Theory]
    [InlineData("en", "Sign in to Season Ended", "Click the following link to sign in")]
    [InlineData("es", "Inicia sesion en Season Ended", "Haz clic en el siguiente enlace para iniciar sesion")]
    [InlineData(null, "Sign in to Season Ended", "Click the following link to sign in")]
    [InlineData("unsupported", "Sign in to Season Ended", "Click the following link to sign in")]
    public void Magic_link_message_uses_language_with_english_fallback(
        string? language,
        string expectedSubject,
        string expectedText)
    {
        var message = AuthenticationMessages.MagicLink(language, "TOKEN", "https://season-ended.localhost");

        Assert.Equal(expectedSubject, message.Subject);
        Assert.Contains(expectedText, message.TextBody);
        Assert.Contains("TOKEN", message.HtmlBody);
    }
}
