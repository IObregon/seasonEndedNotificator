namespace SeasonEnded.Api.Notifications;

public static class PushDigestMessages
{
    public static string Create(string? language, List<DigestCandidate> items)
    {
        return language == "es" ? BuildSpanish(items) : BuildEnglish(items);
    }

    private static string BuildEnglish(List<DigestCandidate> items)
    {
        var first = items[0];
        var title = System.Text.Json.JsonEncodedText.Encode(first.ShowTitle);
        var body = items.Count == 1
            ? $"{title} Season {first.SeasonNumber} has ended!"
            : $"{items.Count} seasons have ended, including {title} Season {first.SeasonNumber}!";
        return $"{{\"title\":\"Seasons Ended\",\"body\":\"{body}\",\"url\":\"/shows/{first.ShowProviderId}/seasons/{first.SeasonNumber}\"}}";
    }

    private static string BuildSpanish(List<DigestCandidate> items)
    {
        var first = items[0];
        var title = System.Text.Json.JsonEncodedText.Encode(first.ShowTitle);
        var body = items.Count == 1
            ? $"Temporada {first.SeasonNumber} de {title} ha finalizado!"
            : $"{items.Count} temporadas han finalizado, incluyendo {title} Temporada {first.SeasonNumber}!";
        return $"{{\"title\":\"Temporadas finalizadas\",\"body\":\"{body}\",\"url\":\"/shows/{first.ShowProviderId}/seasons/{first.SeasonNumber}\"}}";
    }
}
