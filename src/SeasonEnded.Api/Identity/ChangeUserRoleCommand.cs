using Microsoft.EntityFrameworkCore;

namespace SeasonEnded.Api.Identity;

public sealed class ChangeUserRoleCommand(AppDbContext context)
{
    public async Task<ChangeUserRoleResult> ExecuteAsync(
        Guid actorId,
        Guid targetId,
        UserRole newRole)
    {
        var actor = await context.Users.FindAsync(actorId);
        if (actor is null || actor.Role != UserRole.Admin || actor.Status != "Active")
            return ChangeUserRoleResult.Forbidden;

        var target = await context.Users.FindAsync(targetId);
        if (target is null)
            return ChangeUserRoleResult.NotFound;
        if (target.Status != "Active")
            return ChangeUserRoleResult.InactiveTarget;
        if (target.Role == newRole)
            return ChangeUserRoleResult.Unchanged;
        if (target.Role == UserRole.Admin && newRole != UserRole.Admin)
        {
            var anotherAdminExists = await context.Users.AnyAsync(user =>
                user.Id != targetId &&
                user.Role == UserRole.Admin &&
                user.Status == "Active");
            if (!anotherAdminExists)
                return ChangeUserRoleResult.LastActiveAdmin;
        }

        var previousRole = target.Role;
        target.Role = newRole;
        context.RoleChangeAudits.Add(new RoleChangeAudit
        {
            ActorUserId = actorId,
            TargetUserId = targetId,
            PreviousRole = previousRole,
            NewRole = newRole
        });
        await context.SaveChangesAsync();
        return ChangeUserRoleResult.Changed;
    }
}

public enum ChangeUserRoleResult
{
    Changed,
    Forbidden,
    NotFound,
    InactiveTarget,
    Unchanged,
    LastActiveAdmin
}
