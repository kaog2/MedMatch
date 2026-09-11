using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Json;
using MedMatch.Application.Contracts;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MedMatch.Infrastructure.Services;

/// <summary>
/// Translates text on-demand via a self-hosted LibreTranslate instance and
/// caches results in the database so the same content is never translated twice.
/// </summary>
public sealed class TranslationService(MedMatchDbContext db, HttpClient http, IConfiguration configuration) : ITranslationService
{
    private sealed record LibreTranslateResponse(string? TranslatedText);

    public async Task<string?> TranslateAsync(string text, string targetLanguage, CancellationToken ct)
    {
        var source = text.Trim();
        if (source.Length == 0) return null;

        var hash = Hash(source);
        var cached = await db.TranslationCache.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SourceHash == hash && x.TargetLanguage == targetLanguage, ct);
        if (cached is not null) return cached.TranslatedText;

        var baseUrl = configuration["LIBRETRANSLATE_URL"] ?? "http://libretranslate:5000";
        var payload = new { q = source, source = "auto", target = targetLanguage, format = "text" };

        using var response = await http.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/translate", payload, ct);
        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<LibreTranslateResponse>(cancellationToken: ct);
        var translated = result?.TranslatedText?.Trim();
        if (string.IsNullOrWhiteSpace(translated)) return null;

        db.TranslationCache.Add(new TranslationCache
        {
            SourceHash = hash,
            SourceLanguage = "auto",
            TargetLanguage = targetLanguage,
            SourceText = source,
            TranslatedText = translated,
        });
        await db.SaveChangesAsync(ct);

        return translated;
    }

    private static string Hash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }
}
