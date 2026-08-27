using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;
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
    });
builder.Services.AddAuthorization();

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

    app.MapPost("/api/invitations/accept", async (
        AcceptInvitationRequest? request,
        AppDbContext db,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(AcceptInvitationRequest.Token)] = ["Token is required"]
            });

        var command = new AcceptInvitationCommand(db);
        var result = await command.ExecuteAsync(request.Token);

        if (!result.Succeeded)
            return Results.Problem("Invitation token is invalid, expired, or already used.",
                statusCode: StatusCodes.Status410Gone);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId!.ToString()!),
            new(ClaimTypes.Email, result.Email!)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });

        return Results.NoContent();
    });

    app.MapPost("/api/auth/magic-link", async (
        MagicLinkRequest? request,
        AppDbContext db,
        IEmailSender sender) =>
    {
        if (!MailAddress.TryCreate(request?.Email, out var email))
        {
            return Results.Ok(new { message = "If an account exists, a sign-in link has been sent." });
        }

        var command = new RequestMagicLinkCommand(db, sender);
        await command.ExecuteAsync(email.Address);

        return Results.Ok(new { message = "If an account exists, a sign-in link has been sent." });
    });

    app.MapPost("/api/auth/magic-link/consume", async (
        ConsumeMagicLinkRequest? request,
        AppDbContext db,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
            return Results.Problem("Token is invalid.", statusCode: StatusCodes.Status410Gone);

        var command = new ConsumeMagicLinkCommand(db);
        var result = await command.ExecuteAsync(request.Token);

        if (!result.Succeeded)
            return Results.Problem("Token is invalid, expired, or already used.",
                statusCode: StatusCodes.Status410Gone);

        var user = await db.Users.FindAsync(result.UserId);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user!.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });

        return Results.NoContent();
    });
}

app.Run();

public partial class Program;

public sealed record EmailTestRequest(string Recipient);
public sealed record InviteUserRequest(string Email);
public sealed record AcceptInvitationRequest(string Token);
public sealed record MagicLinkRequest(string Email);
public sealed record ConsumeMagicLinkRequest(string Token);
