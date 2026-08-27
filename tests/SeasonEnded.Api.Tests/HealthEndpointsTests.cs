using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace SeasonEnded.Api.Tests;

public sealed class HealthEndpointsTests
{
    [Fact]
    public async Task Liveness_does_not_depend_on_external_services()
    {
        await using var application = CreateApplication("Host=127.0.0.1;Port=1;Database=seasonended;Username=app;Timeout=1");
        using var client = application.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Readiness_fails_when_postgresql_is_unavailable()
    {
        await using var application = CreateApplication("Host=127.0.0.1;Port=1;Database=seasonended;Username=app;Timeout=1");
        using var client = application.CreateClient();

        var readiness = await client.GetAsync("/health/ready");
        var liveness = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateApplication(string connectionString) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Postgres", connectionString));
}
