namespace MedMatch.Application.Contracts;

public interface IEmailSender
{
    Task SendEmailVerificationAsync(string recipientEmail, string verificationUrl, CancellationToken cancellationToken);
}
