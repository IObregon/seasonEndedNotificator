using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api.Tests;

public sealed class CompletionCandidatePolicyTests
{
    private static readonly DateTimeOffset FollowedAt = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Only_completion_after_follow_is_eligible(int secondsFromFollow, bool expected)
    {
        var completedAt = FollowedAt.AddSeconds(secondsFromFollow);

        Assert.Equal(expected, CompletionCandidatePolicy.IsEligible(FollowedAt, completedAt));
    }
}
