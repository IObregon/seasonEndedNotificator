using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Options;

public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ISmtpTransport transport) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var mail = new MailMessage
        {
            From = new MailAddress(options.Value.FromAddress, options.Value.FromName),
            Subject = message.Subject,
            SubjectEncoding = Encoding.UTF8
        };

        mail.To.Add(message.To);
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            message.TextBody,
            Encoding.UTF8,
            MediaTypeNames.Text.Plain));
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            message.HtmlBody,
            Encoding.UTF8,
            MediaTypeNames.Text.Html));

        await transport.SendAsync(mail, cancellationToken);
    }
}
