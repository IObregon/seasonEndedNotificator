using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SeasonEnded.Api.Catalog;

public sealed class TvmazeShowSearch(HttpClient client) : ITvShowSearch
{
    public async Task<IReadOnlyList<ShowSearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(
            $"/search/shows?q={Uri.EscapeDataString(query)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new TvSearchRateLimitedException();
        if (!response.IsSuccessStatusCode)
            throw new TvSearchUnavailableException();

        var items = await response.Content.ReadFromJsonAsync<List<TvmazeSearchItem>>(
            cancellationToken) ?? [];

        return items.Select(item => new ShowSearchResult(
            item.Show.Id,
            item.Show.Name,
            ParsePremiereYear(item.Show.Premiered),
            item.Show.Status,
            item.Show.Image?.Medium)).ToList();
    }

    private static int? ParsePremiereYear(string? premiered) =>
        DateOnly.TryParse(premiered, out var date) ? date.Year : null;

    private sealed record TvmazeSearchItem(
        [property: JsonPropertyName("show")] TvmazeShow Show);

    private sealed record TvmazeShow(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("premiered")] string? Premiered,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("image")] TvmazeImage? Image);

    private sealed record TvmazeImage(
        [property: JsonPropertyName("medium")] string? Medium);
}

public sealed class TvSearchRateLimitedException : Exception;
public sealed class TvSearchUnavailableException : Exception;
