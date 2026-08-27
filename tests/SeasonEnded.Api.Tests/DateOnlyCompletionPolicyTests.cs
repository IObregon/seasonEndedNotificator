using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Tests;

public sealed class DateOnlyCompletionPolicyTests
{
    [Theory]
    [InlineData("2024-03-09", "2024-03-10T04:59:59Z", false)]
    [InlineData("2024-03-09", "2024-03-10T05:00:00Z", true)]
    [InlineData("2024-11-02", "2024-11-03T03:59:59Z", false)]
    [InlineData("2024-11-02", "2024-11-03T04:00:00Z", true)]
    public void Uses_next_midnight_in_original_timezone(
        string airDate,
        string now,
        bool expected)
    {
        var evidence = new DateOnlyFinaleEvidence(
            1,
            "regular",
            ExplicitFinale: true,
            DateOnly.Parse(airDate),
            "America/New_York");

        Assert.Equal(expected,
            DateOnlyCompletionPolicy.IsEligible(evidence, DateTimeOffset.Parse(now)));
    }

    [Theory]
    [InlineData(0, "regular", true)]
    [InlineData(1, "significant_special", true)]
    [InlineData(1, "regular", false)]
    public void Invalid_finale_evidence_never_completes(
        int season,
        string type,
        bool explicitFinale)
    {
        var evidence = new DateOnlyFinaleEvidence(
            season, type, explicitFinale, new DateOnly(2026, 8, 27), "UTC");

        Assert.False(DateOnlyCompletionPolicy.IsEligible(
            evidence, new DateTimeOffset(2026, 8, 29, 0, 0, 0, TimeSpan.Zero)));
    }
}
