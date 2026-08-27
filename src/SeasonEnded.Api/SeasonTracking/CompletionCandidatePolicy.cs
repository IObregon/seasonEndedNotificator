namespace SeasonEnded.Api.SeasonTracking;

public static class CompletionCandidatePolicy
{
    public static bool IsEligible(DateTimeOffset followedAt, DateTimeOffset completedAt) =>
        completedAt > followedAt;
}
