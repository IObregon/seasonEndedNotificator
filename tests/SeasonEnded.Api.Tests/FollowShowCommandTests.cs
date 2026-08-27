using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class FollowShowCommandTests
{
    [Fact]
    public async Task First_follow_records_user_show_and_timestamp()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        var show = new Show { ProviderId = 82, Title = "Game of Thrones", Status = "Ended" };
        context.Users.Add(user);
        context.Shows.Add(show);
        await context.SaveChangesAsync();
        var before = DateTime.UtcNow;

        var result = await new FollowShowCommand(context).ExecuteAsync(user.Id, show.Id);

        Assert.True(result.Created);
        Assert.Equal(user.Id, result.Follow.UserId);
        Assert.Equal(show.Id, result.Follow.ShowId);
        Assert.True(result.Follow.FollowedAt >= before);
    }

    [Fact]
    public async Task Duplicate_follow_returns_unchanged_existing_follow()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        var show = new Show { ProviderId = 82, Title = "Game of Thrones", Status = "Ended" };
        context.Users.Add(user);
        context.Shows.Add(show);
        await context.SaveChangesAsync();
        var command = new FollowShowCommand(context);
        var first = await command.ExecuteAsync(user.Id, show.Id);

        var second = await command.ExecuteAsync(user.Id, show.Id);

        Assert.False(second.Created);
        Assert.Equal(first.Follow.Id, second.Follow.Id);
        Assert.Equal(first.Follow.FollowedAt, second.Follow.FollowedAt);
        Assert.Single(context.ShowFollows);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
