using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MedMatch.Api.Security;

public static class UserIdentity
{
    public static Guid UserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? throw new UnauthorizedAccessException());

    /// <summary>Resolves the requested UI culture from the Accept-Language header (fallback: en).</summary>
    public static string PreferredCulture(HttpContext context)
    {
        var header = context.Request.Headers.AcceptLanguage.ToString();
        var first = header.Split(',')[0].Trim();
        var code = first.Split('-', ';')[0].ToLowerInvariant();
        return code is "es" or "de" or "it" ? code : "en";
    }
}
