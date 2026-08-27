namespace SeasonEnded.Api.Catalog;

public interface ITvShowDetails
{
    Task<ImportedShow> GetAsync(int providerId, CancellationToken cancellationToken);
}

public sealed record ImportedShow(
    int ProviderId,
    string Title,
    int? PremiereYear,
    string Status,
    string? ImageUrl,
    IReadOnlyList<ImportedSeason> Seasons);

public sealed record ImportedSeason(
    int ProviderSeasonId,
    int Number,
    DateOnly? PremiereDate,
    DateOnly? EndDate);
