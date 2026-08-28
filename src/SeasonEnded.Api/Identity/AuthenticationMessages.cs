namespace SeasonEnded.Api.Identity;

public static class AuthenticationMessages
{
    public static EmailMessage MagicLink(string? language, string token, string baseUrl)
    {
        var loginUrl = $"{baseUrl.TrimEnd('/')}/?token={token}";

        if (language == "es")
        {
            return new EmailMessage(
                "",
                "Inicia sesion en Season Ended",
                $"Haz clic en el siguiente enlace para iniciar sesion:{Environment.NewLine}{loginUrl}{Environment.NewLine}{Environment.NewLine}O usa este codigo: {token}",
                $"<p>Haz clic en el siguiente enlace para iniciar sesion:</p><p><a href=\"{loginUrl}\">{loginUrl}</a></p><p>O usa este codigo: <code>{token}</code></p>");
        }

        return new EmailMessage(
            "",
            "Sign in to Season Ended",
            $"Click the following link to sign in:{Environment.NewLine}{loginUrl}{Environment.NewLine}{Environment.NewLine}Or use this code: {token}",
            $"<p>Click the following link to sign in:</p><p><a href=\"{loginUrl}\">{loginUrl}</a></p><p>Or use this code: <code>{token}</code></p>");
    }
}
