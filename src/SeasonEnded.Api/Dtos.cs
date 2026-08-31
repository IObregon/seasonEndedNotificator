using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SeasonEnded.Api;

public sealed record EmailTestRequest(string Recipient);
public sealed record InviteUserRequest(string Email);
public sealed record AcceptInvitationRequest(string Token);
public sealed record MagicLinkRequest(string Email);
public sealed record ConsumeMagicLinkRequest(string Token);
public sealed record SetLanguageRequest(string Language);
public sealed record ChangeRoleRequest(string Role);
public sealed record DeleteAccountRequest(string Confirmation);
public sealed record ShowDetailsResponse(
    int ProviderId,
    string Title,
    int? PremiereYear,
    string Status,
    string? ImageUrl,
    IEnumerable<SeasonResponse> Seasons);
public sealed record SeasonResponse(
    int Number,
    DateOnly? PremiereDate,
    DateOnly? EndDate,
    DateTimeOffset? CompletedAt);
public sealed record FollowedShowResponse(
    int ProviderId,
    string Title,
    int? PremiereYear,
    string Status,
    string? ImageUrl,
    DateTime FollowedAt);
public sealed record MetadataIssueResponse(
    int ProviderId,
    string Title,
    int SeasonNumber,
    string Reason);
public sealed record EmailPreferenceResponse(bool EmailEnabled);
public sealed record EmailPreferenceRequest(bool EmailEnabled);
public sealed record DigestPreviewRequest(string Recipient, string? Language = null);
public sealed record TelegramWebhookRequest(string? Secret, TelegramMessage? Message);
public sealed record TelegramMessage(long? Id, TelegramChat? Chat, string? Text);
public sealed record TelegramChat(long? Id);
public sealed record PushSubscriptionRequest(string Endpoint, string P256DH, string Auth, string? Label = null);
public sealed record SimulateFinaleRequest(int ProviderId, int? SeasonNumber = null);

internal static class HttpContextExtensions
{
    public static Guid? GetUserId(this HttpContext httpContext)
    {
        var id = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var userId) ? userId : null;
    }
}
