using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeasonEnded.Api.Identity;
using System.Net;
using System.Net.Http.Json;

namespace SeasonEnded.Api.Tests;

public sealed class DevelopmentEmailEndpointTests
{
    [Fact]
    public async Task Development_endpoint_sends_a_multipart_test_email()
    {
        var sender = new TestEmailSender();
        await using var application = CreateApplication("Development", sender);
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync("/api/dev/email-test", new { recipient = "viewer@example.test" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(sender.SentMessage);
        Assert.Equal("viewer@example.test", sender.SentMessage.To);
        Assert.Equal("Season Ended email check", sender.SentMessage.Subject);
        Assert.Contains("Local email is working.", sender.SentMessage.TextBody);
        Assert.Contains("<strong>Local email is working.</strong>", sender.SentMessage.HtmlBody);
    }

    [Fact]
    public async Task Development_endpoint_is_absent_in_production()
    {
        await using var application = CreateApplication("Production", new TestEmailSender());
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync("/api/dev/email-test", new { recipient = "viewer@example.test" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Development_endpoint_rejects_an_invalid_recipient()
    {
        var sender = new TestEmailSender();
        await using var application = CreateApplication("Development", sender);
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync("/api/dev/email-test", new { recipient = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(sender.SentMessage);
    }

    private static WebApplicationFactory<Program> CreateApplication(string environment, IEmailSender sender) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.UseSetting("ConnectionStrings:Postgres", "Host=127.0.0.1;Port=1;Database=seasonended;Username=app;Timeout=1");
                builder.UseSetting("BootstrapAdmin:Email", "");
                builder.ConfigureTestServices(services =>
                {
                    RemoveDbContextRegistrations(services);
                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase($"email-test-{Guid.NewGuid()}"));
                    services.AddSingleton(sender);
                });
            });

    private static void RemoveDbContextRegistrations(IServiceCollection services)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                services[i].ServiceType == typeof(DbContextOptions))
                services.RemoveAt(i);
        }
    }
}
