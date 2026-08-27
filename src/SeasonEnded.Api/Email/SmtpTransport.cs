using System.Net.Mail;
using Microsoft.Extensions.Options;

public sealed class SmtpTransport(IOptions<SmtpOptions> options) : ISmtpTransport
{
    public async Task SendAsync(MailMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(options.Value.Host, options.Value.Port)
        {
            EnableSsl = options.Value.UseTls
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
