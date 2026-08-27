namespace SeasonEnded.Api.Identity;

public sealed class DisableUserCommand(AppDbContext context)
{
    public async Task<DisableUserResult> ExecuteAsync(Guid callerId, Guid targetId)
    {
        var caller = await context.Users.FindAsync(callerId);
        if (caller is null || caller.Role != UserRole.Admin || caller.Status != "Active")
            return DisableUserResult.Forbidden;

        if (callerId == targetId)
            return DisableUserResult.SelfDisableRejected;

        var target = await context.Users.FindAsync(targetId);
        if (target is null)
            return DisableUserResult.NotFound;

        if (target.Status == "Disabled")
            return DisableUserResult.AlreadyDisabled;

        target.Status = "Disabled";
        await context.SaveChangesAsync();
        return DisableUserResult.Disabled;
    }
}

public enum DisableUserResult
{
    Disabled,
    Forbidden,
    SelfDisableRejected,
    NotFound,
    AlreadyDisabled
}
