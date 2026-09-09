using MailKit.Net.Smtp;
using MailKit.Security;
using MedMatch.Application.Contracts;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace MedMatch.Infrastructure.Services;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendEmailVerificationAsync(string recipientEmail, string verificationUrl, CancellationToken cancellationToken)
    {
        var host = Required("SMTP_HOST");
        var port = int.Parse(configuration["SMTP_PORT"] ?? "587");
        var username = Required("SMTP_USERNAME");
        var password = Required("SMTP_PASSWORD");
        var allowInvalidCertificate = bool.TryParse(configuration["SMTP_ALLOW_INVALID_CERTIFICATE"], out var parsed) && parsed;
        var fromEmail = configuration["SMTP_FROM_EMAIL"] ?? username;
        var fromName = configuration["SMTP_FROM_NAME"] ?? "MedMatch";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = "Verify your MedMatch email address";
        message.Body = new BodyBuilder
        {
            TextBody = $"Welcome to MedMatch. Verify your email address by opening this link:\n\n{verificationUrl}\n\nThis link expires in 24 hours. If you did not create a MedMatch account, you can ignore this email.",
            HtmlBody = $"<p>Welcome to MedMatch.</p><p><a href=\"{verificationUrl}\">Verify your email address</a></p><p>This link expires in 24 hours. If you did not create a MedMatch account, you can ignore this email.</p>"
        }.ToMessageBody();

        using var client = new SmtpClient();
        if (allowInvalidCertificate) client.ServerCertificateValidationCallback = (_, _, _, _) => true;
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private string Required(string key) => configuration[key] ?? throw new InvalidOperationException($"{key} is required for email delivery.");
}
