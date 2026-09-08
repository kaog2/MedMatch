using MedMatch.Domain;

namespace MedMatch.Application.Contracts;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user);
    string CreateRefreshToken();
    string HashRefreshToken(string token);
}
