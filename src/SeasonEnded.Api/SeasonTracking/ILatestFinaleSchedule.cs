namespace SeasonEnded.Api.SeasonTracking;

public interface ILatestFinaleSchedule
{
    Task<RefreshedFinaleSchedule> GetAsync(
        int providerSeasonId,
        CancellationToken cancellationToken);
}

public sealed record RefreshedFinaleSchedule(
    int ProviderSeasonId,
    int SeasonNumber,
    string EpisodeType,
    bool ExplicitFinale,
    DateTimeOffset AirStart,
    int? RuntimeMinutes,
    FinaleEvidenceAssessment Assessment);
