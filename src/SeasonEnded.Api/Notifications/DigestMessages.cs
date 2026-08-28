namespace SeasonEnded.Api.Notifications;

public static class DigestMessages
{
    public static EmailMessage Create(string? language, string recipient, List<DigestCandidate> items)
    {
        return language == "es"
            ? BuildSpanish(recipient, items)
            : BuildEnglish(recipient, items);
    }

    private static EmailMessage BuildEnglish(string recipient, List<DigestCandidate> items)
    {
        var textItems = string.Join("\n", items.Select(i =>
            $"- {i.ShowTitle}, Season {i.SeasonNumber} (ended {FormatDate(i.EndDate)}) - /shows/{i.ShowProviderId}/seasons/{i.SeasonNumber}"));
        var htmlItems = string.Join("", items.Select(i =>
            $"<li>{i.ShowTitle}, Season {i.SeasonNumber} (ended {FormatDate(i.EndDate)}) &mdash; <a href=\"/shows/{i.ShowProviderId}/seasons/{i.SeasonNumber}\">view</a></li>"));

        return new EmailMessage(
            recipient,
            "Seasons Ended",
            $"The following seasons have ended:{Environment.NewLine}{Environment.NewLine}{textItems}{Environment.NewLine}{Environment.NewLine}Season Ended",
            $"<h2>Seasons Ended</h2><ul>{htmlItems}</ul><p>Season Ended</p>");
    }

    private static EmailMessage BuildSpanish(string recipient, List<DigestCandidate> items)
    {
        var textItems = string.Join("\n", items.Select(i =>
            $"- {i.ShowTitle}, Temporada {i.SeasonNumber} (finalizo {FormatDate(i.EndDate)}) - /shows/{i.ShowProviderId}/seasons/{i.SeasonNumber}"));
        var htmlItems = string.Join("", items.Select(i =>
            $"<li>{i.ShowTitle}, Temporada {i.SeasonNumber} (finalizo {FormatDate(i.EndDate)}) &mdash; <a href=\"/shows/{i.ShowProviderId}/seasons/{i.SeasonNumber}\">ver</a></li>"));

        return new EmailMessage(
            recipient,
            "Temporadas finalizadas",
            $"Las siguientes temporadas han finalizado:{Environment.NewLine}{Environment.NewLine}{textItems}{Environment.NewLine}{Environment.NewLine}Season Ended",
            $"<h2>Temporadas finalizadas</h2><ul>{htmlItems}</ul><p>Season Ended</p>");
    }

    private static string FormatDate(DateOnly? date) =>
        date?.ToString("yyyy-MM-dd") ?? "unknown";
}
