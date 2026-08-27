using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class RefreshFollowedShowsCommandTests
{
    [Fact]
    public async Task Refreshes_distinct_followed_running_shows_only()
    {
        await using var context = CreateContext();
        var firstUser = new User { Email = "one@example.test" };
        var secondUser = new User { Email = "two@example.test" };
        var running = new Show { ProviderId = 1, Title = "Running", Status = "Running" };
        var ended = new Show { ProviderId = 2, Title = "Ended", Status = "Ended" };
        var unfollowed = new Show { ProviderId = 3, Title = "Unfollowed", Status = "Running" };
        context.Users.AddRange(firstUser, secondUser);
        context.Shows.AddRange(running, ended, unfollowed);
        context.ShowFollows.AddRange(
            new ShowFollow { UserId = firstUser.Id, ShowId = running.Id },
            new ShowFollow { UserId = secondUser.Id, ShowId = running.Id },
            new ShowFollow { UserId = firstUser.Id, ShowId = ended.Id });
        await context.SaveChangesAsync();
        var provider = new RecordingDetails();

        var result = await new RefreshFollowedShowsCommand(context, provider)
            .ExecuteAsync(CancellationToken.None);

        Assert.Equal([1], provider.RequestedProviderIds);
        Assert.Equal(1, result.Refreshed);
        Assert.Equal(0, result.Failed);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class RecordingDetails : ITvShowDetails
    {
        public List<int> RequestedProviderIds { get; } = [];

        public Task<ImportedShow> GetAsync(int providerId, CancellationToken cancellationToken)
        {
            RequestedProviderIds.Add(providerId);
            return Task.FromResult(new ImportedShow(
                providerId, $"Show {providerId}", 2026, "Running", null, []));
        }
    }
}
