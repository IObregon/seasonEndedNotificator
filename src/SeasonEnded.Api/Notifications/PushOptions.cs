namespace SeasonEnded.Api.Notifications;

public sealed class PushOptions
{
    public const string SectionName = "Push";
    public string PublicKey { get; init; } = "";
    public string PrivateKey { get; init; } = "";
    public string Subject { get; init; } = "";
}
