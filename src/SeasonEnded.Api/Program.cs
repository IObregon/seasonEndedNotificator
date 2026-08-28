using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Jobs;
using SeasonEnded.Api.Notifications;
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
builder.Services.AddSingleton<IRetryDelay, RetryDelay>();
builder.Services.AddTransient<TvmazeRetryHandler>();
builder.Services.AddHttpClient<ITvShowSearch, TvmazeShowSearch>(client =>
{
    client.BaseAddress = new Uri("https://api.tvmaze.com");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SeasonEnded/1.0");
}).AddHttpMessageHandler<TvmazeRetryHandler>();
builder.Services.AddHttpClient<ITvShowDetails, TvmazeShowDetails>(client =>
{
    client.BaseAddress = new Uri("https://api.tvmaze.com");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SeasonEnded/1.0");
}).AddHttpMessageHandler<TvmazeRetryHandler>();
builder.Services.AddScoped<IFollowedShowRefresh, RefreshFollowedShowsCommand>();
builder.Services.AddScoped<DailyMetadataRefreshJob>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services
    .AddOptions<MetadataRefreshOptions>()
    .BindConfiguration(MetadataRefreshOptions.SectionName)
    .Validate(options => options.HourUtc is >= 0 and <= 23, "MetadataRefresh:HourUtc must be 0-23")
    .ValidateOnStart();
builder.Services.AddHostedService<MetadataRefreshHostedService>();
builder.Services.AddScoped<DailyDigestJob>();
builder.Services.AddScoped<PrepareDigestCommand>();
builder.Services.AddScoped<SendDigestCommand>();
builder.Services
    .AddOptions<DigestScheduleOptions>()
    .BindConfiguration(DigestScheduleOptions.SectionName)
    .Validate(options => options.HourUtc is >= 0 and <= 23, "DigestSchedule:HourUtc must be 0-23")
    .ValidateOnStart();
builder.Services.AddHostedService<DigestHostedService>();
builder.Services.AddScoped<CreateTelegramLinkCommand>();
builder.Services.AddScoped<ConsumeTelegramTokenCommand>();
builder.Services.AddScoped<DisconnectTelegramCommand>();

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

builder.Services.AddSingleton<ITelegramSender, UnconfiguredTelegramSender>();
builder.Services.AddSingleton<IPushSender, UnconfiguredPushSender>();

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
                    season.EndDate,
                    season.CompletedAt))));
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

app.MapPost("/api/shows/{providerId:int}/follow", async (
    int providerId,
    AppDbContext db,
    HttpContext httpContext) =>
{
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var show = await db.Shows.FirstOrDefaultAsync(item => item.ProviderId == providerId);
    if (show is null)
        return Results.NotFound();

    var result = await new FollowShowCommand(db).ExecuteAsync(userId, show.Id);
    return Results.Ok(new { followedAt = result.Follow.FollowedAt, created = result.Created });
}).RequireAuthorization();

app.MapDelete("/api/shows/{providerId:int}/follow", async (
    int providerId,
    AppDbContext db,
    HttpContext httpContext) =>
{
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var show = await db.Shows.FirstOrDefaultAsync(item => item.ProviderId == providerId);
    if (show is not null)
        await new UnfollowShowCommand(db).ExecuteAsync(userId, show.Id);

    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/api/follows", async (
    AppDbContext db,
    HttpContext httpContext) =>
{
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var followedShows = await db.ShowFollows
        .Where(follow => follow.UserId == userId)
        .Join(db.Shows,
            follow => follow.ShowId,
            show => show.Id,
            (follow, show) => new FollowedShowResponse(
                show.ProviderId,
                show.Title,
                show.PremiereYear,
                show.Status,
                show.ImageUrl,
                follow.FollowedAt))
        .OrderBy(show => show.Title)
        .ToListAsync();

    return Results.Ok(followedShows);
}).RequireAuthorization();

app.MapGet("/api/admin/metadata/issues", async (AppDbContext db) =>
{
    var issues = await db.Seasons
        .Where(season => season.UncertaintyReason != null)
        .Join(db.Shows,
            season => season.ShowId,
            show => show.Id,
            (season, show) => new MetadataIssueResponse(
                show.ProviderId,
                show.Title,
                season.Number,
                season.UncertaintyReason!.Value.ToString()))
        .OrderBy(issue => issue.Title)
        .ThenBy(issue => issue.SeasonNumber)
        .ToListAsync();

    return Results.Ok(issues);
}).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

app.MapGet("/api/admin/delivery-failures", async (
    AppDbContext db,
    string? channel,
    string? status,
    DateOnly? fromDate,
    DateOnly? toDate,
    int page = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default) =>
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;

    var query = db.DigestDeliveries
        .Include(d => d.Attempts)
        .Where(d => d.Status == "Failed" || d.Status == "PermanentlyFailed");

    if (!string.IsNullOrEmpty(channel))
        query = query.Where(d => d.Channel == channel);
    if (!string.IsNullOrEmpty(status))
        query = query.Where(d => d.Status == status);
    if (fromDate.HasValue)
        query = query.Where(d => d.DigestDate >= fromDate.Value);
    if (toDate.HasValue)
        query = query.Where(d => d.DigestDate <= toDate.Value);

    var total = await query.CountAsync(cancellationToken);
    var deliveries = await query
        .OrderByDescending(d => d.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(d => new
        {
            d.Id,
            d.UserId,
            d.Channel,
            d.DigestDate,
            d.Status,
            d.NextAttemptAt,
            d.CreatedAt,
            Attempts = d.Attempts.Select(a => new
            {
                a.AttemptNumber,
                a.Outcome,
                a.SanitizedError,
                a.AttemptedAt
            }).ToList()
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(new { total, page, pageSize, deliveries });
}).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

app.MapPost("/api/admin/digests/send", async (
    AppDbContext db,
    IEmailSender emailSender,
    ITelegramSender telegramSender,
    IPushSender pushSender,
    CancellationToken cancellationToken) =>
{
    var digestDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
    var prepared = await new PrepareDigestCommand(db).ExecuteAsync(digestDate, cancellationToken);
    var results = new List<object>();

    foreach (var delivery in prepared)
    {
        var result = await new SendDigestCommand(db, emailSender, telegramSender, pushSender)
            .ExecuteAsync(delivery.Id, cancellationToken);
        results.Add(new { deliveryId = delivery.Id, sent = result.Sent, reason = result.Reason });
    }

    return Results.Ok(results);
}).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

app.MapPost("/api/admin/digests/{deliveryId:guid}/retry", async (
    Guid deliveryId,
    AppDbContext db,
    IEmailSender emailSender,
    ITelegramSender telegramSender,
    IPushSender pushSender,
    CancellationToken cancellationToken) =>
{
    var result = await new SendDigestCommand(db, emailSender, telegramSender, pushSender)
        .ExecuteAsync(deliveryId, cancellationToken);
    return Results.Ok(new { sent = result.Sent, reason = result.Reason });
}).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

app.MapGet("/api/notification-preferences", async (
    AppDbContext db,
    HttpContext httpContext) =>
{
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var enabled = await new EmailPreferenceService(db).IsEnabledAsync(userId);
    return Results.Ok(new EmailPreferenceResponse(enabled));
}).RequireAuthorization();

app.MapPut("/api/notification-preferences", async (
    EmailPreferenceRequest? request,
    AppDbContext db,
    HttpContext httpContext) =>
{
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
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
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
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
    IConfiguration configuration) =>
{
    if (request is null)
        return Results.BadRequest();

    var secret = configuration["Telegram:WebhookSecret"] ?? "";
    if (string.IsNullOrEmpty(secret) || request.Secret != secret)
        return Results.Unauthorized();

    if (request.Message?.Text is not string text || !text.StartsWith("/start "))
        return Results.Ok();

    var rawToken = text["/start ".Length..].Trim();
    var chatId = request.Message.Chat?.Id ?? 0;
    if (chatId == 0)
        return Results.Ok();

    await new ConsumeTelegramTokenCommand(db).ExecuteAsync(rawToken, chatId, DateTimeOffset.UtcNow);
    return Results.Ok();
});

app.MapGet("/api/telegram/status", async (
    AppDbContext db,
    HttpContext httpContext) =>
{
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var connected = await db.TelegramDestinations.AnyAsync(d => d.UserId == userId);
    return Results.Ok(new { connected });
}).RequireAuthorization();

app.MapDelete("/api/telegram/connection", async (
    AppDbContext db,
    HttpContext httpContext) =>
{
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var result = await new DisconnectTelegramCommand(db).ExecuteAsync(userId);
    return result ? Results.NoContent() : Results.Unauthorized();
}).RequireAuthorization();

app.MapGet("/api/manifest.json", (IConfiguration configuration) =>
{
    var appName = configuration["PWA:Name"] ?? "Season Ended";
    var startUrl = configuration["PWA:StartUrl"] ?? "/";
    return Results.Ok(new
    {
        name = appName,
        short_name = appName,
        start_url = startUrl,
        display = "standalone",
        background_color = "#ffffff",
        theme_color = "#1a73e8",
        icons = new[]
        {
            new { src = "/icons/icon-192.png", sizes = "192x192", type = "image/png" },
            new { src = "/icons/icon-512.png", sizes = "512x512", type = "image/png" }
        }
    });
});

app.MapPost("/api/push/subscriptions", async (
    PushSubscriptionRequest? request,
    AppDbContext db,
    HttpContext httpContext) =>
{
    if (request is null || string.IsNullOrEmpty(request.Endpoint))
        return Results.BadRequest();

    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
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
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
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
    if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var sub = await db.PushSubscriptions.FindAsync(id);
    if (sub is null || sub.UserId != userId)
        return Results.NotFound();

    sub.Active = false;
    await db.SaveChangesAsync();
    return Results.NoContent();
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
