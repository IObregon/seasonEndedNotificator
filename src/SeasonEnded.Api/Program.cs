using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Catalog;
using System.Net.Mail;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
var postgresConnection = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.LoginPath = "/api/auth/magic-link";
        options.Events.OnValidatePrincipal = SessionValidation.ValidateAsync;
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<ActiveUserPolicy>();
builder.Services.AddHttpClient<ITvShowSearch, TvmazeShowSearch>(client =>
{
    client.BaseAddress = new Uri("https://api.tvmaze.com");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SeasonEnded/1.0");
});
builder.Services.AddHttpClient<ITvShowDetails, TvmazeShowDetails>(client =>
{
    client.BaseAddress = new Uri("https://api.tvmaze.com");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SeasonEnded/1.0");
});

builder.Services
    .AddHealthChecks()
    .AddNpgSql(postgresConnection, tags: ["ready"]);

if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddOptions<SmtpOptions>()
        .BindConfiguration(SmtpOptions.SectionName)
        .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "Email:Smtp:Host is required")
        .Validate(options => options.Port is > 0 and <= 65535, "Email:Smtp:Port is invalid")
        .Validate(options => MailAddress.TryCreate(options.FromAddress, out _), "Email:Smtp:FromAddress is invalid")
        .ValidateOnStart();
    builder.Services.AddSingleton<ISmtpTransport, SmtpTransport>();
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, UnconfiguredEmailSender>();
}

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    var bootstrapEmail = builder.Configuration["BootstrapAdmin:Email"];
    if (!string.IsNullOrWhiteSpace(bootstrapEmail))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        var command = new BootstrapAdminCommand(db);
        var result = await command.ExecuteAsync(bootstrapEmail);
        if (result.Created)
            app.Logger.LogInformation("Bootstrapped admin {Email}", bootstrapEmail);
    }
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = static (context, _) => context.Response.WriteAsync("Healthy")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = static (context, report) => context.Response.WriteAsync(report.Status.ToString())
});

if (app.Environment.IsDevelopment())
{
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

}

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
});

app.MapPost("/api/auth/magic-link", async (
    MagicLinkRequest? request,
    AppDbContext db,
    IEmailSender sender) =>
{
    const string responseMessage = "If an account exists, a sign-in link has been sent.";
    if (!MailAddress.TryCreate(request?.Email, out var email))
        return Results.Ok(new { message = responseMessage });

    await new RequestMagicLinkCommand(db, sender).ExecuteAsync(email.Address);
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
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
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

app.MapPost("/api/admin/users/{targetId:guid}/disable", async (
    Guid targetId,
    AppDbContext db,
    HttpContext httpContext) =>
{
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId))
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
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
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
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
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

app.MapGet("/api/shows/search", async (
    string? query,
    ITvShowSearch search,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(query))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(query)] = ["Search query is required."]
        });

    try
    {
        return Results.Ok(await search.SearchAsync(query.Trim(), cancellationToken));
    }
    catch (TvSearchRateLimitedException)
    {
        return Results.Problem("TV search rate limit reached. Try again shortly.",
            statusCode: StatusCodes.Status429TooManyRequests);
    }
    catch (TvSearchUnavailableException)
    {
        return Results.Problem("TV search is temporarily unavailable.",
            statusCode: StatusCodes.Status502BadGateway);
    }
}).RequireAuthorization();

app.MapGet("/api/shows/{providerId:int}", async (
    int providerId,
    AppDbContext db,
    ITvShowDetails provider,
    CancellationToken cancellationToken) =>
{
    try
    {
        var show = await new ImportShowDetailsCommand(db, provider)
            .ExecuteAsync(providerId, cancellationToken);
        return Results.Ok(new ShowDetailsResponse(
            show.ProviderId,
            show.Title,
            show.PremiereYear,
            show.Status,
            show.ImageUrl,
            show.Seasons
                .OrderBy(season => season.Number)
                .Select(season => new SeasonResponse(
                    season.Number,
                    season.PremiereDate,
                    season.EndDate))));
    }
    catch (TvShowNotFoundException)
    {
        return Results.NotFound();
    }
    catch (HttpRequestException)
    {
        return Results.Problem("Show details are temporarily unavailable.",
            statusCode: StatusCodes.Status502BadGateway);
    }
}).RequireAuthorization();

app.Run();

public partial class Program;

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
public sealed record SeasonResponse(int Number, DateOnly? PremiereDate, DateOnly? EndDate);
