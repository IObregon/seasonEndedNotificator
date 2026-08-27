using System.Net.Mail;

public interface ISmtpTransport
{
    Task SendAsync(MailMessage message, CancellationToken cancellationToken);
}
