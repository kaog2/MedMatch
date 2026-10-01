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

    public static async Task<User?> AuthenticateBotKeyAsync(HttpContext context, MedMatchDbContext db, ILogger logger, CancellationToken ct = default)
    {
        string? rawKey = null;
        var credentialSource = "none";

        if (context.Request.Headers.TryGetValue("X-API-Key", out var headerKey) && !string.IsNullOrWhiteSpace(headerKey))
        {
            rawKey = headerKey.ToString().Trim();
            credentialSource = "X-API-Key";
        }
        else if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authStr = authHeader.ToString().Trim();
            if (authStr.StartsWith("Bearer mm_bot_", StringComparison.OrdinalIgnoreCase))
            {
                rawKey = authStr["Bearer ".Length..].Trim();
                credentialSource = "Authorization: Bearer";
            }
            else if (authStr.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
            {
                rawKey = authStr["ApiKey ".Length..].Trim();
                credentialSource = "Authorization: ApiKey";
            }
        }

        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return null;
        }

        if (!rawKey.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Bot API authentication rejected a credential with an invalid prefix. Source: {CredentialSource}; Path: {RequestPath}", credentialSource, context.Request.Path);
            return null;
        }

        var hash = HashKey(rawKey);
        var keyEntity = await db.UserBotApiKeys
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.KeyHash == hash, ct);

        if (keyEntity is null)
        {
            logger.LogWarning("Bot API authentication rejected a key that does not match a stored key. Source: {CredentialSource}; Path: {RequestPath}", credentialSource, context.Request.Path);
            return null;
        }

        if (!keyEntity.IsActive)
        {
            logger.LogWarning("Bot API authentication rejected a revoked key. Source: {CredentialSource}; Path: {RequestPath}", credentialSource, context.Request.Path);
            return null;
        }

        if (!keyEntity.User.IsActive)
        {
            logger.LogWarning("Bot API authentication rejected a key because its owner account is inactive. Source: {CredentialSource}; Path: {RequestPath}", credentialSource, context.Request.Path);
            return null;
        }

        keyEntity.LastUsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return keyEntity.User;
    }
}

