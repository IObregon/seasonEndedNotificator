using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Notifications;
using SeasonEnded.Api.SeasonTracking;

namespace SeasonEnded.Api;

public static class DevEndpoints
{
    public static WebApplication MapInvitationEndpoints(this WebApplication app)
    {
        app.MapPost("/api/invitations/accept", async (
            AcceptInvitationRequest? request,
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (string.IsNullOrWhiteSpace(request?.Token))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(AcceptInvitationRequest.Token)] = ["Token is required"]
                });

            var result = await new AcceptInvitationCommand(db).ExecuteAsync(request.Token);
            if (!result.Succeeded)
                return Results.Problem("Invitation token is invalid, expired, or already used.",
                    statusCode: StatusCodes.Status410Gone);

            await SessionSignIn.SignInUserAsync(httpContext, result.UserId!.Value, result.Email!, UserRole.User);
            return Results.NoContent();
        }).RequireRateLimiting("auth");

        return app;
    }

    public static WebApplication MapDevEndpoints(this WebApplication app)
    {
        app.MapPost("/api/dev/auto-login", async (
            AppDbContext db,
            HttpContext httpContext) =>
        {
            var bootstrapEmail = app.Configuration["BootstrapAdmin:Email"] ?? "admin@localhost";
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == bootstrapEmail);
            if (user is null)
                return Results.NotFound("No bootstrap admin found. Set BootstrapAdmin:Email in configuration.");

            await SessionSignIn.SignInUserAsync(httpContext, user.Id, user.Email, user.Role);
            return Results.Ok(new { email = user.Email, role = user.Role.ToString() });
        });

        app.MapPost("/api/dev/email-test", async (
            EmailTestRequest? request,
            IEmailSender sender,
            CancellationToken cancellationToken) =>
        {
            if (!MailAddress.TryCreate(request?.Recipient, out var recipient))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(EmailTestRequest.Recipient)] = ["Recipient must be a valid email address"]
                });
            }

            await sender.SendAsync(new EmailMessage(
                recipient.Address,
                "Season Ended email check",
                "Local email is working.",
                "<p><strong>Local email is working.</strong></p>"), cancellationToken);

            return Results.NoContent();
        });

        app.MapPost("/api/dev/email-digest-preview", async (
            DigestPreviewRequest? request,
            IEmailSender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!MailAddress.TryCreate(request?.Recipient, out var recipient))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(DigestPreviewRequest.Recipient)] = ["Recipient must be a valid email address"]
                });
            }

            var language = request?.Language;
            if (language is not null && language is not ("en" or "es"))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(DigestPreviewRequest.Language)] = ["Language must be 'en' or 'es'."]
                });
            }

            var message = DigestPreviewMessages.Create(language, recipient.Address);
            await sender.SendAsync(message, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        app.MapPost("/api/dev/simulate-finale", async (
            SimulateFinaleRequest? request,
            AppDbContext db,
            PrepareDigestCommand prepare,
            SendDigestCommand send,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            if (request is null || request.ProviderId <= 0)
                return Results.BadRequest("ProviderId is required.");

            var show = await db.Shows
                .Include(s => s.Seasons)
                .FirstOrDefaultAsync(s => s.ProviderId == request.ProviderId, cancellationToken);
            if (show is null)
                return Results.NotFound($"Show with ProviderId {request.ProviderId} not found. Search for it first via GET /api/shows/{request.ProviderId}.");

            var follow = await db.ShowFollows.FirstOrDefaultAsync(f => f.UserId == userId && f.ShowId == show.Id, cancellationToken);
            if (follow is null)
                return Results.BadRequest("You must follow this show first. Use POST /api/shows/{providerId}/follow.");

            var season = request.SeasonNumber.HasValue
                ? show.Seasons.FirstOrDefault(s => s.Number == request.SeasonNumber.Value)
                : show.Seasons.OrderByDescending(s => s.Number).FirstOrDefault();

            if (season is null)
                return Results.NotFound("No seasons found for this show.");

            if (season.CompletedAt is not null)
            {
                var existingEvent = await db.SeasonCompletionEvents
                    .FirstOrDefaultAsync(e => e.SeasonId == season.Id, cancellationToken);
                if (existingEvent is not null)
                {
                    var existingDelivery = await db.DigestDeliveries
                        .FirstOrDefaultAsync(d => d.UserId == userId && d.DigestDate == DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date), cancellationToken);
                    if (existingDelivery is not null)
                        return Results.Ok(new
                        {
                            message = "Season already completed and digest already prepared today.",
                            showTitle = show.Title,
                            seasonNumber = season.Number,
                            completedAt = season.CompletedAt,
                            deliveryId = existingDelivery.Id,
                            deliveryStatus = existingDelivery.Status
                        });
                }
            }

            var completedAt = DateTimeOffset.UtcNow;
            season.CompletedAt = completedAt;
            season.UncertaintyReason = null;

            db.SeasonCompletionEvents.Add(new SeasonCompletionEvent
            {
                SeasonId = season.Id,
                CompletedAt = completedAt
            });
            await db.SaveChangesAsync(cancellationToken);

            var digestDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
            var prepared = await prepare.ExecuteAsync(digestDate, cancellationToken);
            var results = new List<object>();

            foreach (var delivery in prepared)
            {
                var result = await send.ExecuteAsync(delivery.Id, cancellationToken);
                results.Add(new { deliveryId = delivery.Id, delivery.Channel, sent = result.Sent, reason = result.Reason });
            }

            return Results.Ok(new
            {
                message = "Finale simulated. Check Mailpit at http://localhost:8025 for the digest email.",
                showTitle = show.Title,
                seasonNumber = season.Number,
                completedAt = completedAt,
                deliveries = results
            });
        }).RequireAuthorization();

        app.MapPost("/api/invitations", async (
            InviteUserRequest? request,
            AppDbContext db,
            IEmailSender sender,
            CancellationToken cancellationToken) =>
        {
            if (!MailAddress.TryCreate(request?.Email, out var email))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(InviteUserRequest.Email)] = ["Email must be a valid email address"]
                });
            }

            var bootstrapEmail = app.Configuration["BootstrapAdmin:Email"] ?? "admin@localhost";
            var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == bootstrapEmail && u.Role == UserRole.Admin);
            if (admin is null)
                return Results.Problem("No admin account available to issue invitations.", statusCode: StatusCodes.Status503ServiceUnavailable);

            var command = new InviteUserCommand(db, sender);
            var result = await command.ExecuteAsync(admin.Id.ToString(), email.Address);

            if (!result.Created)
                return Results.Conflict(new { message = "An active invitation already exists." });

            return Results.Created();
        });

        return app;
    }
}
