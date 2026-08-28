using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeasonEnded.Api.Identity;
using System.Net;
using System.Net.Http.Json;

namespace SeasonEnded.Api.Tests;

public sealed class DigestPreviewEndpointTests
{
    [Fact]
    public async Task Admin_can_send_english_digest_preview()
    {
        var sender = new TestEmailSender();
        await using var application = CreateApplication("Development", sender);
        using var client = await CreateAdminClientAsync(application);

        var response = await client.PostAsJsonAsync("/api/dev/email-digest-preview",
            new { recipient = "admin@example.test", language = "en" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(sender.SentMessage);
        Assert.Equal("admin@example.test", sender.SentMessage.To);
        Assert.Equal("[PREVIEW] Seasons Ended", sender.SentMessage.Subject);
        Assert.Contains("Breaking Bad", sender.SentMessage.TextBody);
        Assert.Contains("Breaking Bad", sender.SentMessage.HtmlBody);
    }

    [Fact]
    public async Task Admin_can_send_spanish_digest_preview()
    {
        var sender = new TestEmailSender();
        await using var application = CreateApplication("Development", sender);
        using var client = await CreateAdminClientAsync(application);

        var response = await client.PostAsJsonAsync("/api/dev/email-digest-preview",
            new { recipient = "admin@example.test", language = "es" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(sender.SentMessage);
        Assert.Equal("[VISTA PREVIA] Temporadas finalizadas", sender.SentMessage.Subject);
        Assert.Contains("Temporada 5", sender.SentMessage.TextBody);
    }

    [Fact]
    public async Task Unauthenticated_request_is_rejected()
    {
        var sender = new TestEmailSender();
        await using var application = CreateApplication("Development", sender);
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync("/api/dev/email-digest-preview",
            new { recipient = "admin@example.test" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(sender.SentMessage);
    }

    [Fact]
    public async Task Digest_preview_endpoint_is_absent_in_production()
    {
        var sender = new TestEmailSender();
        await using var application = CreateApplication("Production", sender);
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync("/api/dev/email-digest-preview",
            new { recipient = "admin@example.test" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Digest_preview_rejects_invalid_recipient()
    {
        var sender = new TestEmailSender();
        await using var application = CreateApplication("Development", sender);
        using var client = await CreateAdminClientAsync(application);

        var response = await client.PostAsJsonAsync("/api/dev/email-digest-preview",
            new { recipient = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(sender.SentMessage);
    }

    [Fact]
    public async Task Digest_preview_rejects_invalid_language()
    {
        var sender = new TestEmailSender();
        await using var application = CreateApplication("Development", sender);
        using var client = await CreateAdminClientAsync(application);

        var response = await client.PostAsJsonAsync("/api/dev/email-digest-preview",
            new { recipient = "admin@example.test", language = "fr" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(sender.SentMessage);
    }

    [Fact]
    public async Task Digest_preview_defaults_to_english_without_language()
    {
        var sender = new TestEmailSender();
        await using var application = CreateApplication("Development", sender);
        using var client = await CreateAdminClientAsync(application);

        var response = await client.PostAsJsonAsync("/api/dev/email-digest-preview",
            new { recipient = "admin@example.test" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(sender.SentMessage);
        Assert.Equal("[PREVIEW] Seasons Ended", sender.SentMessage.Subject);
    }

    private static WebApplicationFactory<Program> CreateApplication(
        string environment, IEmailSender sender)
    {
        var dbName = $"digest-preview-{Guid.NewGuid()}";
        var internalProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.UseSetting("ConnectionStrings:Postgres",
                    "Host=127.0.0.1;Port=1;Database=seasonended;Username=app;Timeout=1");
                builder.UseSetting("BootstrapAdmin:Email", "admin@localhost");
                builder.ConfigureTestServices(services =>
                {
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(dbName)
                            .UseInternalServiceProvider(internalProvider));
                    services.AddSingleton(sender);
                });
            });
    }

    private static async Task<HttpClient> CreateAdminClientAsync(
        WebApplicationFactory<Program> application)
    {
        var user = new User { Email = "admin@localhost", Role = UserRole.Admin };
        var rawToken = Guid.NewGuid().ToString();
        var tokenHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawToken)));

        await using var scope = application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        db.MagicLinkTokens.Add(new MagicLinkToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
        await db.SaveChangesAsync();

        using var consumeResponse = await application.CreateClient()
            .PostAsJsonAsync("/api/auth/magic-link/consume", new { token = rawToken });

        var setCookieHeaders = string.Join("; ",
            consumeResponse.Headers.Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                .SelectMany(h => h.Value));

        var client = application.CreateClient();
        if (!string.IsNullOrEmpty(setCookieHeaders))
            client.DefaultRequestHeaders.Add("Cookie", setCookieHeaders);

        return client;
    }
}
