namespace SeasonEnded.Api.Jobs;

public sealed class JobLease
{
    public string Name { get; init; } = "";
    public string Owner { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class JobExecution
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string JobName { get; init; } = "";
    public string Status { get; set; } = "Started";
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int Refreshed { get; set; }
    public int Failed { get; set; }
}
