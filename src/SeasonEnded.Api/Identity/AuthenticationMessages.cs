namespace SeasonEnded.Api.Identity;

public static class AuthenticationMessages
{
    public static EmailMessage MagicLink(string? language, string token)
    {
        if (language == "es")
        {
            return new EmailMessage(
                "",
                "Inicia sesion en Season Ended",
                $"Haz clic aqui para iniciar sesion: {token}",
                $"<p>Haz clic aqui para iniciar sesion: <code>{token}</code></p>");
        }

        return new EmailMessage(
            "",
            "Sign in to Season Ended",
            $"Click here to sign in: {token}",
            $"<p>Click here to sign in: <code>{token}</code></p>");
    }
}
