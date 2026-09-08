using MedMatch.Domain;

namespace MedMatch.Application.Contracts;

public sealed record RegisterRequest(string Email, string Password, UserRole Role = UserRole.Patient);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, UserRole Role);
