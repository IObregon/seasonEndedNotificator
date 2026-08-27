using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class UnfollowShowCommandTests
{
    [Fact]
    public async Task Unfollow_is_idempotent_and_preserves_show()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        var show = new Show { ProviderId = 82, Title = "Game of Thrones", Status = "Ended" };
        context.Users.Add(user);
        context.Shows.Add(show);
        await context.SaveChangesAsync();
        await new FollowShowCommand(context).ExecuteAsync(user.Id, show.Id);
        var command = new UnfollowShowCommand(context);

        var first = await command.ExecuteAsync(user.Id, show.Id);
        var second = await command.ExecuteAsync(user.Id, show.Id);

        Assert.True(first.Removed);
        Assert.False(second.Removed);
        Assert.Empty(context.ShowFollows);
        Assert.NotNull(await context.Shows.FindAsync(show.Id));
    }

    [Fact]
    public async Task Refollow_gets_new_timestamp()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.test" };
        var show = new Show { ProviderId = 82, Title = "Game of Thrones", Status = "Ended" };
        context.Users.Add(user);
        context.Shows.Add(show);
        await context.SaveChangesAsync();
        var follow = new FollowShowCommand(context);
        var first = await follow.ExecuteAsync(user.Id, show.Id);
        await new UnfollowShowCommand(context).ExecuteAsync(user.Id, show.Id);
        await Task.Delay(10);

        var second = await follow.ExecuteAsync(user.Id, show.Id);

        Assert.True(second.Created);
        Assert.True(second.Follow.FollowedAt > first.Follow.FollowedAt);
        Assert.NotEqual(first.Follow.Id, second.Follow.Id);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
