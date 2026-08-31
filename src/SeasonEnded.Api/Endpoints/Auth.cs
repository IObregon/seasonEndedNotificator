using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net.Mail;
using System.Security.Claims;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/auth/me", (HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            var email = httpContext.User.FindFirstValue(ClaimTypes.Email);
            var role = httpContext.User.FindFirstValue(ClaimTypes.Role);
            return Results.Ok(new { email, role });
        }).RequireAuthorization();

        app.MapPost("/api/auth/magic-link", async (
            MagicLinkRequest? request,
            AppDbContext db,
            IEmailSender sender,
            HttpContext httpContext) =>
        {
            const string responseMessage = "If an account exists, a sign-in link has been sent.";
            if (!MailAddress.TryCreate(request?.Email, out var email))
                return Results.Ok(new { message = responseMessage });

            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            await new RequestMagicLinkCommand(db, sender, baseUrl).ExecuteAsync(email.Address);
            return Results.Ok(new { message = responseMessage });
        });

        app.MapPost("/api/auth/magic-link/consume", async (
            ConsumeMagicLinkRequest? request,
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (string.IsNullOrWhiteSpace(request?.Token))
                return Results.Problem("Token is invalid.", statusCode: StatusCodes.Status410Gone);

            var result = await new ConsumeMagicLinkCommand(db).ExecuteAsync(request.Token);
            if (!result.Succeeded)
                return Results.Problem("Token is invalid, expired, or already used.",
                    statusCode: StatusCodes.Status410Gone);

            var user = await db.Users.FindAsync(result.UserId);
            await SessionSignIn.SignInUserAsync(httpContext, user!.Id, user.Email, user.Role);
            return Results.NoContent();
        });

        app.MapPut("/api/me/language", async (
            SetLanguageRequest? request,
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();

            try
            {
                var changed = await new SetUserLanguageCommand(db)
                    .ExecuteAsync(userId, request?.Language ?? "");
                return changed ? Results.NoContent() : Results.Unauthorized();
            }
            catch (ArgumentException)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(SetLanguageRequest.Language)] = ["Language must be 'en' or 'es'."]
                });
            }
        }).RequireAuthorization();

        app.MapDelete("/api/me", async (
            [FromBody] DeleteAccountRequest? request,
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (request?.Confirmation != "DELETE MY ACCOUNT")
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(DeleteAccountRequest.Confirmation)] = ["Confirmation must be 'DELETE MY ACCOUNT'."]
                });
            if (httpContext.GetUserId() is not { } userId)
                return Results.Unauthorized();
            if (!long.TryParse(httpContext.User.FindFirstValue("authenticated_at"), out var authenticatedAt) ||
                DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(authenticatedAt) > TimeSpan.FromMinutes(10))
                return Results.Problem("Recent authentication is required.", statusCode: StatusCodes.Status403Forbidden);

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var result = await new RequestAccountDeletionCommand(db).ExecuteAsync(userId);
            if (result == RequestAccountDeletionResult.Pending)
            {
                await transaction.CommitAsync();
                await httpContext.SignOutAsync();
            }

            return result switch
            {
                RequestAccountDeletionResult.Pending => Results.Accepted(),
                RequestAccountDeletionResult.AlreadyPending => Results.Accepted(),
                RequestAccountDeletionResult.LastActiveAdmin => Results.Conflict(new { message = "Transfer admin responsibility before deleting this account." }),
                _ => Results.NotFound()
            };
        }).RequireAuthorization();

        return app;
    }

    public static WebApplication MapAdminUserEndpoints(this WebApplication app)
    {
        app.MapPost("/api/admin/users/{targetId:guid}/disable", async (
            Guid targetId,
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } callerId)
                return Results.Unauthorized();

            var result = await new DisableUserCommand(db).ExecuteAsync(callerId, targetId);
            return result switch
            {
                DisableUserResult.Disabled => Results.NoContent(),
                DisableUserResult.SelfDisableRejected => Results.Conflict(new { message = "Administrators cannot disable themselves." }),
                DisableUserResult.AlreadyDisabled => Results.Conflict(new { message = "User is already disabled." }),
                DisableUserResult.NotFound => Results.NotFound(),
                _ => Results.Forbid()
            };
        }).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        app.MapPut("/api/admin/users/{targetId:guid}/role", async (
            Guid targetId,
            ChangeRoleRequest? request,
            AppDbContext db,
            HttpContext httpContext) =>
        {
            if (httpContext.GetUserId() is not { } actorId)
                return Results.Unauthorized();
            if (!Enum.TryParse<UserRole>(request?.Role, ignoreCase: true, out var role))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(ChangeRoleRequest.Role)] = ["Role must be 'User' or 'Admin'."]
                });

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var result = await new ChangeUserRoleCommand(db).ExecuteAsync(actorId, targetId, role);
            if (result == ChangeUserRoleResult.Changed)
                await transaction.CommitAsync();

            return result switch
            {
                ChangeUserRoleResult.Changed => Results.NoContent(),
                ChangeUserRoleResult.NotFound => Results.NotFound(),
                ChangeUserRoleResult.InactiveTarget => Results.Conflict(new { message = "Inactive users cannot change role." }),
                ChangeUserRoleResult.LastActiveAdmin => Results.Conflict(new { message = "Last active admin cannot be demoted." }),
                ChangeUserRoleResult.Unchanged => Results.Conflict(new { message = "User already has that role." }),
                _ => Results.Forbid()
            };
        }).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        return app;
    }
}
