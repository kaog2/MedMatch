namespace MedMatch.Application.Contracts;

public sealed record RegistrationResult(bool RequiresEmailVerification);
public sealed record VerifyEmailRequest(string Token);
