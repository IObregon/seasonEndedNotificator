namespace SeasonEnded.Api.Jobs;

public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    public bool Enabled { get; init; }
    public int RetentionDays { get; init; } = 30;
}
