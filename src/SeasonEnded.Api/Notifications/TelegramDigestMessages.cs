namespace SeasonEnded.Api.Notifications;

public static class TelegramDigestMessages
{
    public static string Create(string? language, List<DigestCandidate> items)
    {
        return language == "es" ? BuildSpanish(items) : BuildEnglish(items);
    }

    private static string BuildEnglish(List<DigestCandidate> items)
    {
        var lines = items.Select(i =>
            $"- {EscapeMarkdown(i.ShowTitle)}, Season {i.SeasonNumber} (ended {FormatDate(i.EndDate)})\n  /shows/{i.ShowProviderId}/seasons/{i.SeasonNumber}");
        return $"*Seasons Ended*\n\n{string.Join("\n\n", lines)}";
    }

    private static string BuildSpanish(List<DigestCandidate> items)
    {
        var lines = items.Select(i =>
            $"- {EscapeMarkdown(i.ShowTitle)}, Temporada {i.SeasonNumber} (finalizó {FormatDate(i.EndDate)})\n  /shows/{i.ShowProviderId}/seasons/{i.SeasonNumber}");
        return $"*Temporadas finalizadas*\n\n{string.Join("\n\n", lines)}";
    }

    private static string EscapeMarkdown(string text) =>
        text.Replace("*", "\\*").Replace("_", "\\_").Replace("[", "\\[").Replace("]", "\\]");

    private static string FormatDate(DateOnly? date) =>
        date?.ToString("yyyy-MM-dd") ?? "unknown";
}
