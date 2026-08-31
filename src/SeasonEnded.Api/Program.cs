using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Threading.RateLimiting;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;
using SeasonEnded.Api.Jobs;
using SeasonEnded.Api.Notifications;
using SeasonEnded.Api.SeasonTracking;
using SeasonEnded.Api;

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
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
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

var telegramToken = builder.Configuration["Telegram:BotToken"];
if (!string.IsNullOrWhiteSpace(telegramToken))
{
    builder.Services.AddHttpClient("TelegramBot", client =>
    {
        client.BaseAddress = new Uri("https://api.telegram.org");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SeasonEnded/1.0");
    });
    builder.Services.AddOptions<TelegramOptions>()
        .BindConfiguration(TelegramOptions.SectionName);
    builder.Services.AddSingleton<ITelegramSender, TelegramBotSender>();
}
else
{
    builder.Services.AddSingleton<ITelegramSender, UnconfiguredTelegramSender>();
}

var pushPrivateKey = builder.Configuration["Push:PrivateKey"];
if (!string.IsNullOrWhiteSpace(pushPrivateKey))
{
    builder.Services.AddHttpClient("WebPush");
    builder.Services.AddOptions<PushOptions>()
        .BindConfiguration(PushOptions.SectionName);
    builder.Services.AddSingleton<IPushSender, WebPushSender>();
}
else
{
    builder.Services.AddSingleton<IPushSender, UnconfiguredPushSender>();
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            ip,
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

var app = builder.Build();
app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (InvalidOperationException)
    {
        // Multiple database providers registered (test environment with InMemory override).
        // Migration is not applicable; skip silently.
    }

    if (app.Environment.IsDevelopment())
    {
        var bootstrapEmail = builder.Configuration["BootstrapAdmin:Email"];
        if (!string.IsNullOrWhiteSpace(bootstrapEmail))
        {
            var command = new BootstrapAdminCommand(db);
            var result = await command.ExecuteAsync(bootstrapEmail);
            if (result.Created)
                app.Logger.LogInformation("Bootstrapped admin {Email}", bootstrapEmail);
        }
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

app.MapGet("/api/version", (IConfiguration configuration) =>
{
    var version = configuration["App:Version"] ?? "dev";
    return Results.Ok(new { version });
});

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

app.MapAuthEndpoints();
app.MapAdminUserEndpoints();
app.MapShowEndpoints();
app.MapAdminEndpoints();
app.MapNotificationEndpoints();
app.MapInvitationEndpoints();

if (app.Environment.IsDevelopment())
    app.MapDevEndpoints();

app.Run();

public partial class Program;
