using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedMatch.Api.Services;

/// <summary>
/// Derives structured diagnosis tags from a patient's free-text symptoms so
/// they can participate in peer matching. Mode is controlled by
/// SYMPTOM_TAG_EXTRACTION:
///   dictionary (default) - keyword matching against the known diagnosis tags
///   llm        (planned)  - extract via an LLM API (LLM_MODERATION_* config)
///   off        - disabled
/// </summary>
public static class SymptomTagExtraction
{
    private static readonly char[] Separators = [' ', ',', '.', ';', ':', '!', '?', '\n', '\r', '\t', '-', '(', ')', '/'];

    public static async Task EnrichFromSymptomsAsync(PatientProfile profile, MedMatchDbContext db, IConfiguration configuration, CancellationToken ct)
    {
        var mode = (configuration["SYMPTOM_TAG_EXTRACTION"] ?? "dictionary").Trim().ToLowerInvariant();
        if (mode == "off" || string.IsNullOrWhiteSpace(profile.Symptoms)) return;

        // Planned: when mode == "llm", call an OpenAI-compatible chat-completions
        // API (LLM_MODERATION_API_URL / LLM_MODERATION_API_KEY) to extract tags.
        // Until then, every enabled mode uses dictionary keyword matching.
        var extracted = await ExtractWithDictionaryAsync(profile.Symptoms, db, ct);
        if (extracted.Length == 0) return;

        await DiagnosisMatching.SyncDiagnosisTagsAsync(profile, [.. profile.Diagnoses, .. extracted], db, ct);
    }

    private static async Task<string[]> ExtractWithDictionaryAsync(string symptomText, MedMatchDbContext db, CancellationToken ct)
    {
        var tags = await db.DiagnosisTags.AsNoTracking().ToListAsync(ct);
        if (tags.Count == 0) return [];

        var tokens = DiagnosisMatching.TokenizeSymptoms(symptomText).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (tokens.Count == 0) return [];

        var extracted = new List<string>();
        foreach (var tag in tags)
        {
            var words = tag.Name
                .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim().ToLowerInvariant())
                .Where(w => w.Length >= 3 && !DiagnosisMatching.StopWords.Contains(w))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // A tag is extracted only when every meaningful word of its name
            // appears in the symptom text (conservative, avoids over-tagging).
            if (words.Length > 0 && words.All(w => tokens.Contains(w)))
                extracted.Add(tag.Name);
        }

        return extracted.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
