using System.Security.Cryptography;
using System.Text;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedMatch.Api.Security;

public static class BotApiKeyAuth
{
    private const string Prefix = "mm_bot_";

    public static (string RawKey, string KeyHash, string KeyPrefix) GenerateKey()
    {
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        var token = Convert.ToHexString(randomBytes).ToLowerInvariant();
        var rawKey = $"{Prefix}{token}";
        var keyHash = HashKey(rawKey);
        var keyPrefix = $"{Prefix}{token[..6]}...";
        return (rawKey, keyHash, keyPrefix);
    }

    public static string HashKey(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key.Trim());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static async Task<User?> AuthenticateBotKeyAsync(HttpContext context, MedMatchDbContext db, CancellationToken ct = default)
    {
        string? rawKey = null;

        if (context.Request.Headers.TryGetValue("X-API-Key", out var headerKey) && !string.IsNullOrWhiteSpace(headerKey))
        {
            rawKey = headerKey.ToString().Trim();
        }
        else if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authStr = authHeader.ToString().Trim();
            if (authStr.StartsWith("Bearer mm_bot_", StringComparison.OrdinalIgnoreCase))
            {
                rawKey = authStr["Bearer ".Length..].Trim();
            }
            else if (authStr.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
            {
                rawKey = authStr["ApiKey ".Length..].Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(rawKey) || !rawKey.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var hash = HashKey(rawKey);
        var keyEntity = await db.UserBotApiKeys
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.KeyHash == hash && x.IsActive, ct);

        if (keyEntity is null || !keyEntity.User.IsActive)
        {
            return null;
        }

        keyEntity.LastUsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return keyEntity.User;
    }
}

