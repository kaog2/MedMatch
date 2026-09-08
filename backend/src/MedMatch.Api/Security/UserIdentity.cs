using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MedMatch.Api.Security;

public static class UserIdentity
{
    public static Guid UserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? throw new UnauthorizedAccessException());
}
