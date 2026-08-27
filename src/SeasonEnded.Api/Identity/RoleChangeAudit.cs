namespace SeasonEnded.Api.Identity;

public sealed class RoleChangeAudit
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ActorUserId { get; init; }
    public Guid TargetUserId { get; init; }
    public UserRole PreviousRole { get; init; }
    public UserRole NewRole { get; init; }
    public DateTime ChangedAt { get; init; } = DateTime.UtcNow;
}
