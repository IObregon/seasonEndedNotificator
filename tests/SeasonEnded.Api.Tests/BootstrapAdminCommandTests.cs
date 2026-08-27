using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class BootstrapAdminCommandTests
{
    [Fact]
    public async Task Creates_one_active_admin_from_configured_email()
    {
        using var context = CreateInMemoryContext();
        var command = new BootstrapAdminCommand(context);

        var result = await command.ExecuteAsync("admin@example.test");

        Assert.True(result.Created);
        var admin = await context.Users.SingleAsync();
        Assert.Equal("admin@example.test", admin.Email);
        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.Equal("Active", admin.Status);
    }

    [Fact]
    public async Task Repeated_execution_is_idempotent()
    {
        using var context = CreateInMemoryContext();
        var command = new BootstrapAdminCommand(context);

        await command.ExecuteAsync("admin@example.test");
        var result = await command.ExecuteAsync("admin@example.test");

        Assert.False(result.Created);
        Assert.Single(context.Users);
    }

    [Fact]
    public async Task Rejects_existing_non_admin_account_with_same_email()
    {
        using var context = CreateInMemoryContext();
        context.Users.Add(new User { Email = "admin@example.test", Role = UserRole.User });
        await context.SaveChangesAsync();
        var command = new BootstrapAdminCommand(context);

        var act = async () => await command.ExecuteAsync("admin@example.test");

        await Assert.ThrowsAsync<BootstrapConflictException>(act);
    }

    [Fact]
    public async Task Rejects_blank_email()
    {
        using var context = CreateInMemoryContext();
        var command = new BootstrapAdminCommand(context);

        var act = async () => await command.ExecuteAsync("");

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    private static AppDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
