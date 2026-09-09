using MedMatch.Application.Contracts;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MedMatch.Infrastructure.Services;

public sealed class AuthService(MedMatchDbContext db, PasswordHasher passwords, ITokenService tokens, IEmailSender emailSender, IConfiguration configuration) : IAuthService
{
    public async Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Password.Length < 8)
            throw new ArgumentException("Email and a password of at least 8 characters are required.");
        if (request.Role is UserRole.Admin) throw new InvalidOperationException("Admin registration is not available.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken)) throw new InvalidOperationException("An account with that email already exists.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = new User { Email = email, Role = request.Role, PasswordHash = passwords.Hash(request.Password), PatientProfile = request.Role == UserRole.Patient ? new PatientProfile() : null, ConsentSettings = new ConsentSettings(), EmailConfirmed = false };
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            await SendVerificationEmailAsync(user, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new RegistrationResult(true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !passwords.Verify(request.Password, user.PasswordHash)) return null;
        if (!user.EmailConfirmed) throw new InvalidOperationException("Please verify your email address before signing in.");
        user.LastLogin = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= DateTimeOffset.UtcNow) return null;
        stored.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await IssueTokensAsync(stored.User, cancellationToken);
    }

    public async Task<AuthResponse?> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var clientId = configuration["GOOGLE_CLIENT_ID"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(request.Credential)) return null;
        GoogleJsonWebSignature.Payload payload;
        try { payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] }); }
        catch (InvalidJwtException) { return null; }
        if (string.IsNullOrWhiteSpace(payload.Email) || payload.EmailVerified != true) return null;

        var email = payload.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null)
        {
            user = new User { Email = email, Role = UserRole.Patient, PasswordHash = passwords.Hash(Guid.NewGuid().ToString("N")), PatientProfile = new PatientProfile(), ConsentSettings = new ConsentSettings(), EmailConfirmed = true };
            db.Users.Add(user);
        }
        else if (!user.EmailConfirmed) user.EmailConfirmed = true;
        user.LastLogin = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<bool> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) return false;
        var hash = tokens.HashRefreshToken(request.Token);
        var verification = await db.EmailVerificationTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (verification is null || verification.UsedAt is not null || verification.ExpiresAt <= DateTimeOffset.UtcNow) return false;
        verification.UsedAt = DateTimeOffset.UtcNow;
        verification.User.EmailConfirmed = true;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var (accessToken, expiresAt) = tokens.CreateAccessToken(user);
        var refreshToken = tokens.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = tokens.HashRefreshToken(refreshToken), ExpiresAt = DateTimeOffset.UtcNow.AddDays(14) });
        await db.SaveChangesAsync(cancellationToken);
        return new AuthResponse(accessToken, refreshToken, expiresAt, user.Role);
    }

    private async Task SendVerificationEmailAsync(User user, CancellationToken cancellationToken)
    {
        var rawToken = tokens.CreateRefreshToken();
        db.EmailVerificationTokens.Add(new EmailVerificationToken { UserId = user.Id, TokenHash = tokens.HashRefreshToken(rawToken), ExpiresAt = DateTimeOffset.UtcNow.AddHours(24) });
        await db.SaveChangesAsync(cancellationToken);
        var frontendUrl = (configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL is required.")).TrimEnd('/');
        await emailSender.SendEmailVerificationAsync(user.Email, $"{frontendUrl}/verify-email?token={Uri.EscapeDataString(rawToken)}", cancellationToken);
    }
}