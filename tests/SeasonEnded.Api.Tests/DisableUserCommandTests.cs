using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class DisableUserCommandTests
{
    [Fact]
    public async Task Admin_disables_active_non_self_user()
    {
        await using var context = CreateContext();
        var admin = new User { Email = "admin@example.test", Role = UserRole.Admin };
        var target = new User { Email = "user@example.test", Role = UserRole.User };
        context.Users.AddRange(admin, target);
        await context.SaveChangesAsync();

        var result = await new DisableUserCommand(context).ExecuteAsync(admin.Id, target.Id);

        Assert.Equal(DisableUserResult.Disabled, result);
        Assert.Equal("Disabled", target.Status);
    }

    [Fact]
    public async Task Admin_cannot_disable_self()
    {
        await using var context = CreateContext();
        var admin = new User { Email = "admin@example.test", Role = UserRole.Admin };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var result = await new DisableUserCommand(context).ExecuteAsync(admin.Id, admin.Id);

        Assert.Equal(DisableUserResult.SelfDisableRejected, result);
        Assert.Equal("Active", admin.Status);
    }

    [Fact]
    public async Task Non_admin_cannot_disable_user()
    {
        await using var context = CreateContext();
        var caller = new User { Email = "caller@example.test", Role = UserRole.User };
        var target = new User { Email = "target@example.test", Role = UserRole.User };
        context.Users.AddRange(caller, target);
        await context.SaveChangesAsync();

        var result = await new DisableUserCommand(context).ExecuteAsync(caller.Id, target.Id);

        Assert.Equal(DisableUserResult.Forbidden, result);
        Assert.Equal("Active", target.Status);
    }

    [Fact]
    public async Task Repeated_disable_returns_already_disabled()
    {
        await using var context = CreateContext();
        var admin = new User { Email = "admin@example.test", Role = UserRole.Admin };
        var target = new User { Email = "target@example.test", Status = "Disabled" };
        context.Users.AddRange(admin, target);
        await context.SaveChangesAsync();

        var result = await new DisableUserCommand(context).ExecuteAsync(admin.Id, target.Id);

        Assert.Equal(DisableUserResult.AlreadyDisabled, result);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
