namespace SeasonEnded.Api.Identity;

public static class AuthenticationMessages
{
    public static EmailMessage MagicLink(string? language, string token) =>
        language == "es"
            ? new EmailMessage(
                "",
                "Inicia sesion en Season Ended",
                $"Haz clic aqui para iniciar sesion: {token}",
                $"<p>Haz clic aqui para iniciar sesion: <code>{token}</code></p>")
            : new EmailMessage(
                "",
                "Sign in to Season Ended",
                $"Click here to sign in: {token}",
                $"<p>Click here to sign in: <code>{token}</code></p>");
}
