public sealed class SmtpOptions
{
    public const string SectionName = "Email:Smtp";

    public string Host { get; init; } = "";
    public int Port { get; init; }
    public bool UseTls { get; init; }
    public string FromAddress { get; init; } = "";
    public string FromName { get; init; } = "";
}
