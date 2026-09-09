using MedMatch.Domain;

namespace MedMatch.Application.Contracts;

public interface IAuthService
{
    Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken);
    Task<bool> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken);
}
