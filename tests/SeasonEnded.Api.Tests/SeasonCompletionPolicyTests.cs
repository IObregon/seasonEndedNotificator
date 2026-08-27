using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Tests;

public sealed class SeasonCompletionPolicyTests
{
    private static readonly DateTimeOffset Start = new(2026, 8, 27, 20, 0, 0, TimeSpan.FromHours(-4));

    [Fact]
    public void Explicit_regular_finale_is_eligible_at_episode_end()
    {
        var evidence = new FinaleEvidence(1, "regular", ExplicitFinale: true, Start, RuntimeMinutes: 60);

        Assert.False(SeasonCompletionPolicy.IsEligible(evidence, Start.AddMinutes(59).AddSeconds(59)));
        Assert.True(SeasonCompletionPolicy.IsEligible(evidence, Start.AddMinutes(60)));
    }

    [Theory]
    [InlineData(0, "regular", true)]
    [InlineData(1, "significant_special", true)]
    [InlineData(1, "regular", false)]
    public void Ineligible_evidence_never_completes(int season, string type, bool explicitFinale)
    {
        var evidence = new FinaleEvidence(season, type, explicitFinale, Start, RuntimeMinutes: 60);

        Assert.False(SeasonCompletionPolicy.IsEligible(evidence, Start.AddDays(1)));
    }

    [Fact]
    public void Missing_runtime_uses_two_hour_buffer()
    {
        var evidence = new FinaleEvidence(1, "regular", ExplicitFinale: true, Start, RuntimeMinutes: null);

        Assert.False(SeasonCompletionPolicy.IsEligible(evidence, Start.AddMinutes(119)));
        Assert.True(SeasonCompletionPolicy.IsEligible(evidence, Start.AddHours(2)));
    }
}
