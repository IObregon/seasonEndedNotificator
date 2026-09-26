using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SeasonEnded.Api.Catalog;

public sealed class TvmazeShowDetails(HttpClient client, TimeProvider timeProvider) : ITvShowDetails
{
    public async Task<ImportedShow> GetAsync(
        int providerId,
        CancellationToken cancellationToken)
    {
        var show = await client.GetFromJsonAsync<TvmazeShow>(
            $"/shows/{providerId}?embed=episodes", cancellationToken)
            ?? throw new TvShowNotFoundException();
        var seasons = await client.GetFromJsonAsync<List<TvmazeSeason>>(
            $"/shows/{providerId}/seasons", cancellationToken) ?? [];
        var importedSeasons = seasons
            .Where(season => season.Number > 0)
            .Select(season => new ImportedSeason(
                season.Id,
                season.Number,
                ParseDate(season.PremiereDate),
                ParseDate(season.EndDate)))
            .ToList();
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var latestFutureEpisodeDate = FindLatestFutureEpisodeDate(
            show.Embedded?.Episodes,
            today);

        return new ImportedShow(
            show.Id,
            show.Name,
            ParseYear(show.Premiered),
            show.Status,
            show.Image?.Medium,
            importedSeasons,
            latestFutureEpisodeDate);
    }

    private static int? ParseYear(string? value) =>
        ParseDate(value)?.Year;

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, out var date) ? date : null;

    private static DateOnly? FindLatestFutureEpisodeDate(
        IReadOnlyList<TvmazeEpisode>? episodes,
        DateOnly today)
    {
        if (episodes is null)
            return null;

        var seasonNumbers = episodes
            .Where(episode => episode.Type == "regular" && episode.Season > 0)
            .Select(episode => episode.Season)
            .Distinct()
            .OrderByDescending(seasonNumber => seasonNumber);

        foreach (var seasonNumber in seasonNumbers)
        {
            var dates = episodes
                .Where(episode =>
                    episode.Season == seasonNumber &&
                    episode.Type == "regular")
                .Select(episode => ParseDate(episode.AirDate))
                .Where(date => date.HasValue)
                .Select(date => date!.Value)
                .ToList();

            if (dates.Count == 0)
                continue;

            var futureDates = dates.Where(date => date > today).ToList();
            return futureDates.Count == 0 ? null : futureDates.Max();
        }

        return null;
    }

    private sealed record TvmazeShow(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("premiered")] string? Premiered,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("image")] TvmazeImage? Image,
        [property: JsonPropertyName("_embedded")] TvmazeEmbedded? Embedded);

    private sealed record TvmazeEmbedded(
        [property: JsonPropertyName("episodes")] IReadOnlyList<TvmazeEpisode>? Episodes);

    private sealed record TvmazeEpisode(
        [property: JsonPropertyName("season")] int Season,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("airdate")] string? AirDate);

    private sealed record TvmazeImage(
        [property: JsonPropertyName("medium")] string? Medium);

    private sealed record TvmazeSeason(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("number")] int Number,
        [property: JsonPropertyName("premiereDate")] string? PremiereDate,
        [property: JsonPropertyName("endDate")] string? EndDate);
}

public sealed class TvShowNotFoundException : Exception;
