using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class AuthenticationMessagesTests
{
    [Theory]
    [InlineData("en", "Sign in to Season Ended", "Click here to sign in")]
    [InlineData("es", "Inicia sesion en Season Ended", "Haz clic aqui para iniciar sesion")]
    [InlineData(null, "Sign in to Season Ended", "Click here to sign in")]
    [InlineData("unsupported", "Sign in to Season Ended", "Click here to sign in")]
    public void Magic_link_message_uses_language_with_english_fallback(
        string? language,
        string expectedSubject,
        string expectedText)
    {
        var message = AuthenticationMessages.MagicLink(language, "TOKEN");

        Assert.Equal(expectedSubject, message.Subject);
        Assert.Contains(expectedText, message.TextBody);
        Assert.Contains("TOKEN", message.HtmlBody);
    }
}
