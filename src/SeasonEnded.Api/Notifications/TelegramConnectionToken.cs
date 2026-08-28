using System.Security.Cryptography;
using System.Text;

namespace SeasonEnded.Api.Notifications;

public sealed class TelegramConnectionToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public string TokenHash { get; init; } = "";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}

public sealed class TelegramDestination
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public long ChatId { get; init; }
    public DateTimeOffset ConnectedAt { get; init; } = DateTimeOffset.UtcNow;
}
