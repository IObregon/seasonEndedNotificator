namespace SeasonEnded.Api.Tests;

public sealed class TestEmailSender : IEmailSender
{
    public EmailMessage? SentMessage { get; private set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        SentMessage = message;
        return Task.CompletedTask;
    }
}
