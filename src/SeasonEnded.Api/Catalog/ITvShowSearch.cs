namespace SeasonEnded.Api.Catalog;

public interface ITvShowSearch
{
    Task<IReadOnlyList<ShowSearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken);
}

public sealed record ShowSearchResult(
    int ProviderId,
    string Title,
    int? PremiereYear,
    string Status,
    string? ImageUrl);
