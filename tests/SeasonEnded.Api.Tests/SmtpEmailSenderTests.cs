using Microsoft.Extensions.Options;

namespace SeasonEnded.Api.Tests;

public sealed class SmtpEmailSenderTests
{
    [Fact]
    public async Task Sends_configured_multipart_message()
    {
        var transport = new TestSmtpTransport();
        var sender = new SmtpEmailSender(Options.Create(new SmtpOptions
        {
            Host = "mailpit",
            Port = 1025,
            FromAddress = "notifications@season-ended.local",
            FromName = "Season Ended"
        }), transport);

        await sender.SendAsync(new EmailMessage(
            "viewer@example.test",
            "Subject",
            "Plain text",
            "<p>HTML</p>"), CancellationToken.None);

        Assert.Equal("notifications@season-ended.local", transport.FromAddress);
        Assert.Equal("Season Ended", transport.FromName);
        Assert.Equal("viewer@example.test", transport.To);
        Assert.Equal("Subject", transport.Subject);
        Assert.Equal(["text/plain", "text/html"], transport.MediaTypes);
    }

    private sealed class TestSmtpTransport : ISmtpTransport
    {
        public string? FromAddress { get; private set; }
        public string? FromName { get; private set; }
        public string? To { get; private set; }
        public string? Subject { get; private set; }
        public string[] MediaTypes { get; private set; } = [];

        public Task SendAsync(System.Net.Mail.MailMessage message, CancellationToken cancellationToken)
        {
            FromAddress = message.From!.Address;
            FromName = message.From.DisplayName;
            To = message.To.Single().Address;
            Subject = message.Subject;
            MediaTypes = message.AlternateViews.Select(view => view.ContentType.MediaType).ToArray();
            return Task.CompletedTask;
        }
    }
}
