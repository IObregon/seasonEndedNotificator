using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api;

public static class NotificationEndpoints
{
    public static WebApplication MapNotificationEndpoints(this WebApplication app)
    {
        app.MapGet("/api/notification-preferences", async (
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            var enabled = await new EmailPreferenceService(db).IsEnabledAsync(userId);
            return Results.Ok(new EmailPreferenceResponse(enabled));
        }).RequireAuthorization();

        app.MapPut("/api/notification-preferences", async (
            EmailPreferenceRequest? request,
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();
            if (request is null)
                return Results.BadRequest();

            var changed = await new EmailPreferenceService(db).SetAsync(userId, request.EmailEnabled);
            return changed ? Results.NoContent() : Results.Unauthorized();
        }).RequireAuthorization();

        app.MapPost("/api/telegram/link", async (
            AppDbContext db,
            HttpContext httpContext,
            IConfiguration configuration) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            var botUsername = configuration["Telegram:BotUsername"] ?? "";
            if (string.IsNullOrEmpty(botUsername))
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

            var result = await new CreateTelegramLinkCommand(db).ExecuteAsync(userId, botUsername);
            return Results.Ok(new { deepLink = result.DeepLink });
        }).RequireAuthorization();

        app.MapPost("/api/telegram/webhook", async (
            TelegramWebhookRequest? request,
            AppDbContext db,
            ITelegramSender telegramSender,
            IConfiguration configuration,
            HttpContext httpContext) =>
        {
            if (request is null)
                return Results.BadRequest();

            var secret = configuration["Telegram:WebhookSecret"] ?? "";
            var headerSecret = httpContext.Request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString();
            if (string.IsNullOrEmpty(secret) || headerSecret != secret)
                return Results.Unauthorized();

            if (request.Message?.Text is not string text)
                return Results.Ok();

            var chatId = request.Message.Chat?.Id ?? 0;
            if (chatId == 0)
                return Results.Ok();

            if (text.StartsWith("/start "))
            {
                var rawToken = text["/start ".Length..].Trim();
                await new ConsumeTelegramTokenCommand(db).ExecuteAsync(rawToken, chatId, DateTimeOffset.UtcNow);
                return Results.Ok();
            }

            if (text.StartsWith("/login "))
            {
                var email = text["/login ".Length..].Trim();
                var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                var sent = await new RequestTelegramLoginCommand(db, telegramSender, baseUrl)
                    .ExecuteAsync(email);
                if (!sent)
                    await telegramSender.SendAsync(chatId, "No account found with that email, or Telegram is not connected to that account.", CancellationToken.None);
                return Results.Ok();
            }

            if (text == "/login")
            {
                await telegramSender.SendAsync(chatId, "To sign in, send: /login your@email.com", CancellationToken.None);
            }

            return Results.Ok();
        });

        app.MapGet("/api/telegram/status", async (
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            var connected = await db.TelegramDestinations.AnyAsync(d => d.UserId == userId);
            return Results.Ok(new { connected });
        }).RequireAuthorization();

        app.MapDelete("/api/telegram/connection", async (
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            var result = await new DisconnectTelegramCommand(db).ExecuteAsync(userId);
            return result ? Results.NoContent() : Results.Unauthorized();
        }).RequireAuthorization();

        app.MapPost("/api/push/subscriptions", async (
            PushSubscriptionRequest? request,
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (request is null || string.IsNullOrEmpty(request.Endpoint))
                return Results.BadRequest();

            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            var existing = await db.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint);

            if (existing is not null)
            {
                if (existing.UserId != userId)
                    return Results.Forbid();

                existing.P256DH = request.P256DH;
                existing.Auth = request.Auth;
                existing.Active = true;
                await db.SaveChangesAsync();
                return Results.Ok(new { id = existing.Id });
            }

            var subscription = new PushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256DH = request.P256DH,
                Auth = request.Auth,
                Label = request.Label
            };
            db.PushSubscriptions.Add(subscription);
            await db.SaveChangesAsync();
            return Results.Ok(new { id = subscription.Id });
        }).RequireAuthorization();

        app.MapGet("/api/push/subscriptions", async (
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            var devices = await db.PushSubscriptions
                .Where(s => s.UserId == userId && s.Active)
                .Select(s => new { s.Id, s.Label, s.RegisteredAt, s.LastSuccessAt })
                .ToListAsync();
            return Results.Ok(devices);
        }).RequireAuthorization();

        app.MapDelete("/api/push/subscriptions/{id:guid}", async (
            Guid id,
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            var sub = await db.PushSubscriptions.FindAsync(id);
            if (sub is null || sub.UserId != userId)
                return Results.NotFound();

            sub.Active = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }
}
