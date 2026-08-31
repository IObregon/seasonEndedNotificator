namespace SeasonEnded.Api.Notifications;

public static class DigestPreviewMessages
{
    public static EmailMessage Create(string? language, string recipient)
    {
        var items = language == "es" ? SampleItemsEs : SampleItemsEn;
        return language == "es"
            ? BuildSpanish(recipient, items)
            : BuildEnglish(recipient, items);
    }

    private static EmailMessage BuildEnglish(string recipient, string items)
    {
        return new EmailMessage(
            recipient,
            "[PREVIEW] Seasons Ended",
            $"[PREVIEW - not a real notification]{Environment.NewLine}{Environment.NewLine}The following seasons have ended:{Environment.NewLine}{Environment.NewLine}{items}{Environment.NewLine}{Environment.NewLine}Season Ended",
            $"<p><em>[PREVIEW &mdash; not a real notification]</em></p><h2>Seasons Ended</h2><ul>{items}</ul><p>Season Ended</p>");
    }

    private static EmailMessage BuildSpanish(string recipient, string items)
    {
        return new EmailMessage(
            recipient,
            "[VISTA PREVIA] Temporadas finalizadas",
            $"[VISTA PREVIA - notificacion de prueba]{Environment.NewLine}{Environment.NewLine}Las siguientes temporadas han finalizado:{Environment.NewLine}{Environment.NewLine}{items}{Environment.NewLine}{Environment.NewLine}Season Ended",
            $"<p><em>[VISTA PREVIA &mdash; notificacion de prueba]</em></p><h2>Temporadas finalizadas</h2><ul>{items}</ul><p>Season Ended</p>");
    }

    private const string SampleItemsEn =
        "- Breaking Bad, Season 5 (ended 2013-09-29) - /shows/169/seasons/5\n- The Wire, Season 5 (ended 2008-03-09) - /shows/179/seasons/5";

    private const string SampleItemsEs =
        "- Breaking Bad, Temporada 5 (finalizó 2013-09-29) - /shows/169/seasons/5\n- The Wire, Temporada 5 (finalizó 2008-03-09) - /shows/179/seasons/5";
}
