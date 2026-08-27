namespace SeasonEnded.Api.SeasonTracking;

public sealed record FinaleEvidenceAssessment(
    bool HasFinaleAuthority,
    bool HasSchedule,
    bool HasProviderMappingConflict,
    bool HasEpisodeCountConflict,
    bool HasTimeZone);

public static class SeasonUncertaintyPolicy
{
    public static UncertaintyReason? Assess(FinaleEvidenceAssessment evidence)
    {
        if (!evidence.HasFinaleAuthority)
            return UncertaintyReason.MissingFinaleAuthority;
        if (!evidence.HasSchedule)
            return UncertaintyReason.MissingSchedule;
        if (evidence.HasProviderMappingConflict)
            return UncertaintyReason.ProviderMappingConflict;
        if (evidence.HasEpisodeCountConflict)
            return UncertaintyReason.EpisodeCountConflict;
        if (!evidence.HasTimeZone)
            return UncertaintyReason.MissingTimeZone;
        return null;
    }
}

public enum UncertaintyReason
{
    MissingFinaleAuthority,
    MissingSchedule,
    ProviderMappingConflict,
    EpisodeCountConflict,
    MissingTimeZone
}
