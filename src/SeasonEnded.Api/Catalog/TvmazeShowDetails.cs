using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SeasonEnded.Api.Catalog;

public sealed class TvmazeShowDetails(HttpClient client) : ITvShowDetails
{
    public async Task<ImportedShow> GetAsync(
        int providerId,
        CancellationToken cancellationToken)
    {
        var show = await client.GetFromJsonAsync<TvmazeShow>(
            $"/shows/{providerId}", cancellationToken)
            ?? throw new TvShowNotFoundException();
        var seasons = await client.GetFromJsonAsync<List<TvmazeSeason>>(
            $"/shows/{providerId}/seasons", cancellationToken) ?? [];

        return new ImportedShow(
            show.Id,
            show.Name,
            ParseYear(show.Premiered),
            show.Status,
            show.Image?.Medium,
            seasons
                .Where(season => season.Number > 0)
                .Select(season => new ImportedSeason(
                    season.Id,
                    season.Number,
                    ParseDate(season.PremiereDate),
                    ParseDate(season.EndDate)))
                .ToList());
    }

    private static int? ParseYear(string? value) =>
        ParseDate(value)?.Year;

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, out var date) ? date : null;

    private sealed record TvmazeShow(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("premiered")] string? Premiered,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("image")] TvmazeImage? Image);

    private sealed record TvmazeImage(
        [property: JsonPropertyName("medium")] string? Medium);

    private sealed record TvmazeSeason(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("number")] int Number,
        [property: JsonPropertyName("premiereDate")] string? PremiereDate,
        [property: JsonPropertyName("endDate")] string? EndDate);
}

public sealed class TvShowNotFoundException : Exception;
