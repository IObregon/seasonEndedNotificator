using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Tests;

public sealed class BatchCompletionPolicyTests
{
    [Fact]
    public void Complete_batch_is_eligible_after_release_date_ends()
    {
        var evidence = new BatchReleaseEvidence(
            1,
            ExplicitFullSeasonRelease: true,
            ReleasedEpisodeCount: 8,
            ExpectedEpisodeCount: 8,
            new DateOnly(2026, 8, 27),
            "UTC");

        Assert.False(BatchCompletionPolicy.IsEligible(
            evidence, new DateTimeOffset(2026, 8, 27, 23, 59, 59, TimeSpan.Zero)));
        Assert.True(BatchCompletionPolicy.IsEligible(
            evidence, new DateTimeOffset(2026, 8, 28, 0, 0, 0, TimeSpan.Zero)));
    }

    [Theory]
    [InlineData(false, 8, 8)]
    [InlineData(true, 4, 8)]
    [InlineData(true, 0, 8)]
    [InlineData(true, 8, 0)]
    public void Partial_or_ambiguous_batch_never_qualifies(
        bool explicitFullSeason,
        int released,
        int expected)
    {
        var evidence = new BatchReleaseEvidence(
            1,
            explicitFullSeason,
            released,
            expected,
            new DateOnly(2026, 8, 27),
            "UTC");

        Assert.False(BatchCompletionPolicy.IsEligible(
            evidence, new DateTimeOffset(2026, 8, 29, 0, 0, 0, TimeSpan.Zero)));
    }
}
