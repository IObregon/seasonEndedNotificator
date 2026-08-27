using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);
var postgresConnection = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required");

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
}

app.Run();

public partial class Program;

public sealed record EmailTestRequest(string Recipient);
