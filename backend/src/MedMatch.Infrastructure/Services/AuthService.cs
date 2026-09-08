using MedMatch.Application.Contracts;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedMatch.Infrastructure.Services;

public sealed class AuthService(MedMatchDbContext db, PasswordHasher passwords, ITokenService tokens) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Password.Length < 8)
            throw new ArgumentException("Email and a password of at least 8 characters are required.");
        if (request.Role is UserRole.Admin) throw new InvalidOperationException("Admin registration is not available.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken)) throw new InvalidOperationException("An account with that email already exists.");
        var user = new User { Email = email, Role = request.Role, PasswordHash = passwords.Hash(request.Password), PatientProfile = request.Role == UserRole.Patient ? new PatientProfile() : null, ConsentSettings = new ConsentSettings() };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !passwords.Verify(request.Password, user.PasswordHash)) return null;
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

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var (accessToken, expiresAt) = tokens.CreateAccessToken(user);
        var refreshToken = tokens.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = tokens.HashRefreshToken(refreshToken), ExpiresAt = DateTimeOffset.UtcNow.AddDays(14) });
        await db.SaveChangesAsync(cancellationToken);
        return new AuthResponse(accessToken, refreshToken, expiresAt, user.Role);
    }
}