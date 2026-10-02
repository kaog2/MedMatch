using MedMatch.Api.Observability;
using MedMatch.Application.Contracts;
using MedMatch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static MedMatch.Api.Mapping.DtoMapper;
using static MedMatch.Api.Security.UserIdentity;

namespace MedMatch.Api.Endpoints;

/// <summary>Diagnosis tag suggestions and on-demand display translation.</summary>
public static class TaxonomyEndpoints
{
    public static RouteGroupBuilder MapTaxonomyEndpoints(this RouteGroupBuilder api)
    {
        // GET /api/diagnosis-tags/suggest: autocompletes diagnosis tags, localized to the Accept-Language culture.
        api.MapGet("/diagnosis-tags/suggest", async (string? q, HttpContext context, MedMatchDbContext db, CancellationToken ct) =>
        {
            var culture = PreferredCulture(context);
            var query = string.IsNullOrWhiteSpace(q) ? string.Empty : NormalizeTag(q);
            var tags = await db.DiagnosisTags.AsNoTracking()
                .Include(x => x.Translations)
                .Where(x => query.Length == 0 || x.Name.ToLower().Contains(query) || x.Slug.Contains(ToSlug(query)))
                .OrderByDescending(x => x.UsageCount).ThenBy(x => x.Name)
                .Take(20).ToListAsync(ct);
            return Results.Ok(tags.Select(x => ToDiagnosisTagDto(x, culture)));
        }).RequireAuthorization();

        // POST /api/translate: translates one text for display into en, es, de or it.
        api.MapPost("/translate", async (TranslateRequest request, ITranslationService translator, CancellationToken ct) =>
        {
            var text = (request.Text ?? string.Empty).Trim();
            if (text.Length == 0) return Results.BadRequest(new { error = "Text is required." });
            if (text.Length > 2000) return Results.BadRequest(new { error = "Text is too long to translate." });
            var target = (request.TargetLanguage ?? "en").Trim().ToLowerInvariant();
            if (target is not ("en" or "es" or "de" or "it")) return Results.BadRequest(new { error = "Unsupported target language." });
            var translated = await translator.TranslateAsync(text, target, ct);
            if (translated is not null)
            {
                MedMatchMetrics.RecordTranslation(target);
            }
            return translated is null ? Results.StatusCode(StatusCodes.Status503ServiceUnavailable) : Results.Ok(new TranslateResponse(translated));
        }).AllowAnonymous();

        // POST /api/translate/batch: translates up to 50 texts for display in one call.
        api.MapPost("/translate/batch", async (TranslateBatchRequest request, ITranslationService translator, CancellationToken ct) =>
        {
            var texts = request.Texts?
                .Select(text => text?.Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Select(text => text!)
                .Distinct(StringComparer.Ordinal)
                .ToArray() ?? [];

            if (texts.Length == 0) return Results.BadRequest(new { error = "At least one text value is required." });
            if (texts.Length > 50) return Results.BadRequest(new { error = "A maximum of 50 text values can be translated at once." });
            if (texts.Any(text => text.Length > 2000)) return Results.BadRequest(new { error = "Each text value must be 2,000 characters or fewer." });

            var target = (request.TargetLanguage ?? "en").Trim().ToLowerInvariant();
            if (target is not ("en" or "es" or "de" or "it")) return Results.BadRequest(new { error = "Unsupported target language." });

            var translations = await translator.TranslateManyAsync(texts, target, ct);
            MedMatchMetrics.RecordTranslation(target);
            var response = new List<TranslateBatchItem>(texts.Length);
            foreach (var text in texts)
            {
                response.Add(new TranslateBatchItem(text, translations.TryGetValue(text, out var translated) ? translated : null));
            }
            return Results.Ok(new TranslateBatchResponse(response));
        }).AllowAnonymous();

        return api;
    }
}
