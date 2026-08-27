using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class ChangeUserRoleCommandTests
{
    [Fact]
    public async Task Active_admin_promotes_active_user_with_audit()
    {
        await using var context = CreateContext();
        var actor = new User { Email = "admin@example.test", Role = UserRole.Admin };
        var target = new User { Email = "user@example.test", Role = UserRole.User };
        context.Users.AddRange(actor, target);
        await context.SaveChangesAsync();

        var result = await new ChangeUserRoleCommand(context)
            .ExecuteAsync(actor.Id, target.Id, UserRole.Admin);

        Assert.Equal(ChangeUserRoleResult.Changed, result);
        Assert.Equal(UserRole.Admin, target.Role);
        var audit = await context.RoleChangeAudits.SingleAsync();
        Assert.Equal(actor.Id, audit.ActorUserId);
        Assert.Equal(target.Id, audit.TargetUserId);
        Assert.Equal(UserRole.User, audit.PreviousRole);
        Assert.Equal(UserRole.Admin, audit.NewRole);
    }

    [Fact]
    public async Task Admin_demotes_admin_when_another_active_admin_remains()
    {
        await using var context = CreateContext();
        var actor = new User { Email = "actor@example.test", Role = UserRole.Admin };
        var target = new User { Email = "target@example.test", Role = UserRole.Admin };
        context.Users.AddRange(actor, target);
        await context.SaveChangesAsync();

        var result = await new ChangeUserRoleCommand(context)
            .ExecuteAsync(actor.Id, target.Id, UserRole.User);

        Assert.Equal(ChangeUserRoleResult.Changed, result);
        Assert.Equal(UserRole.User, target.Role);
        Assert.Single(context.RoleChangeAudits);
    }

    [Fact]
    public async Task Last_active_admin_cannot_be_demoted()
    {
        await using var context = CreateContext();
        var admin = new User { Email = "admin@example.test", Role = UserRole.Admin };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var result = await new ChangeUserRoleCommand(context)
            .ExecuteAsync(admin.Id, admin.Id, UserRole.User);

        Assert.Equal(ChangeUserRoleResult.LastActiveAdmin, result);
        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.Empty(context.RoleChangeAudits);
    }

    [Fact]
    public async Task Inactive_target_cannot_be_promoted()
    {
        await using var context = CreateContext();
        var admin = new User { Email = "admin@example.test", Role = UserRole.Admin };
        var target = new User { Email = "target@example.test", Status = "Disabled" };
        context.Users.AddRange(admin, target);
        await context.SaveChangesAsync();

        var result = await new ChangeUserRoleCommand(context)
            .ExecuteAsync(admin.Id, target.Id, UserRole.Admin);

        Assert.Equal(ChangeUserRoleResult.InactiveTarget, result);
        Assert.Empty(context.RoleChangeAudits);
    }

    [Fact]
    public async Task Existing_audit_cannot_be_deleted()
    {
        await using var context = CreateContext();
        var audit = new RoleChangeAudit
        {
            ActorUserId = Guid.NewGuid(),
            TargetUserId = Guid.NewGuid(),
            PreviousRole = UserRole.User,
            NewRole = UserRole.Admin
        };
        context.RoleChangeAudits.Add(audit);
        await context.SaveChangesAsync();

        context.RoleChangeAudits.Remove(audit);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
