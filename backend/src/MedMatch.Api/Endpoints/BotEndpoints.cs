using System.Security.Claims;
using MedMatch.Api.Security;
using MedMatch.Application.Contracts;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static MedMatch.Api.Mapping.DtoMapper;
using static MedMatch.Api.Security.UserIdentity;

namespace MedMatch.Api.Endpoints;

/// <summary>Bot ingestion endpoints (n8n / chatbot) authenticated by a patient-scoped API key.</summary>
public static class BotEndpoints
{
    public static RouteGroupBuilder MapBotEndpoints(this RouteGroupBuilder api)
    {
        var botApi = api.MapGroup("/bot").AllowAnonymous();

        botApi.MapPost("/symptom-entries", async (BotLogSymptomRequest request, HttpContext context, ClaimsPrincipal principal, MedMatchDbContext db, ILogger<Program> logger, CancellationToken ct) =>
        {
            var user = await ResolveBotOrUserAsync(context, principal, db, logger, ct);
            if (user is null)
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.SymptomName))
                return Results.BadRequest(new { error = "Symptom name is required." });

            var date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var sheet = await db.SymptomDiarySheets.SingleOrDefaultAsync(x => x.UserId == user.Id && x.Date == date, ct);
            if (sheet is null)
            {
                sheet = new SymptomDiarySheet { UserId = user.Id, Date = date };
                db.SymptomDiarySheets.Add(sheet);
                await db.SaveChangesAsync(ct);
            }

            var entry = new SymptomDiaryEntry
            {
                SheetId = sheet.Id,
                UserId = user.Id,
                Date = date,
                RecordedAt = (request.RecordedAt ?? DateTimeOffset.UtcNow).ToUniversalTime(),
                Category = ParseSymptomCategory(request.Category),
                SymptomName = request.SymptomName.Trim(),
                PainType = request.PainType?.Trim(),
                BodyLocation = request.BodyLocation?.Trim(),
                Severity = Math.Clamp(request.Severity, 0, 10),
                DurationMinutes = request.DurationMinutes,
                Triggers = request.Triggers?.Trim(),
                Relievers = request.Relievers?.Trim(),
                MedicationsTaken = request.MedicationsTaken?.Trim(),
                Notes = request.Notes?.Trim(),
                Source = string.IsNullOrWhiteSpace(request.Source) ? "n8n" : request.Source.Trim()
            };

            db.SymptomDiaryEntries.Add(entry);
            sheet.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/symptom-diary/entries/{entry.Id}", ToSymptomDiaryEntryDto(entry));
        });

        botApi.MapGet("/symptom-entries/today", async (HttpContext context, ClaimsPrincipal principal, MedMatchDbContext db, ILogger<Program> logger, CancellationToken ct) =>
        {
            var user = await ResolveBotOrUserAsync(context, principal, db, logger, ct);
            if (user is null)
                return Results.Unauthorized();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var sheet = await db.SymptomDiarySheets
                .Include(x => x.Entries)
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.UserId == user.Id && x.Date == today, ct);

            if (sheet is null)
            {
                return Results.Ok(new SymptomDiarySheetDto(
                    Guid.Empty,
                    today,
                    null,
                    null,
                    null,
                    null,
                    0,
                    null,
                    null,
                    [],
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow
                ));
            }

            return Results.Ok(ToSymptomDiarySheetDto(sheet));
        });

        botApi.MapGet("/symptom-entries", async (DateOnly? date, HttpContext context, ClaimsPrincipal principal, MedMatchDbContext db, ILogger<Program> logger, CancellationToken ct) =>
        {
            var user = await ResolveBotOrUserAsync(context, principal, db, logger, ct);
            if (user is null)
                return Results.Unauthorized();

            if (date is null)
                return Results.BadRequest(new { error = "A date in YYYY-MM-DD format is required." });

            var entries = await db.SymptomDiaryEntries
                .Where(x => x.UserId == user.Id && x.Date == date.Value)
                .OrderBy(x => x.RecordedAt)
                .AsNoTracking()
                .ToListAsync(ct);

            return Results.Ok(entries.Select(ToSymptomDiaryEntryDto));
        });

        botApi.MapPatch("/symptom-entries/{id:guid}", async (Guid id, BotUpdateSymptomEntryRequest request, HttpContext context, ClaimsPrincipal principal, MedMatchDbContext db, ILogger<Program> logger, CancellationToken ct) =>
        {
            var user = await ResolveBotOrUserAsync(context, principal, db, logger, ct);
            if (user is null)
                return Results.Unauthorized();

            if (request.SymptomName is not null && string.IsNullOrWhiteSpace(request.SymptomName))
                return Results.BadRequest(new { error = "Symptom name cannot be blank." });
            if (request.Severity is < 0 or > 10)
                return Results.BadRequest(new { error = "Severity must be between 0 and 10." });
            if (request.DurationMinutes < 0)
                return Results.BadRequest(new { error = "Duration must be a non-negative integer." });
            if ((request.SymptomName?.Trim().Length ?? 0) > 200 ||
                (request.PainType?.Trim().Length ?? 0) > 200 ||
                (request.BodyLocation?.Trim().Length ?? 0) > 200 ||
                (request.Triggers?.Trim().Length ?? 0) > 500 ||
                (request.Relievers?.Trim().Length ?? 0) > 500 ||
                (request.MedicationsTaken?.Trim().Length ?? 0) > 500 ||
                (request.Notes?.Trim().Length ?? 0) > 3000)
                return Results.BadRequest(new { error = "One or more entry fields exceed the supported length." });
            if (request.Date is null && request.RecordedAt is null && request.Category is null && request.SymptomName is null &&
                request.Severity is null && request.DurationMinutes is null && request.PainType is null && request.BodyLocation is null &&
                request.Triggers is null && request.Relievers is null && request.MedicationsTaken is null && request.Notes is null)
                return Results.BadRequest(new { error = "At least one entry field must be supplied." });

            var entry = await db.SymptomDiaryEntries
                .Include(x => x.Sheet)
                .SingleOrDefaultAsync(x => x.Id == id && x.UserId == user.Id, ct);
            if (entry is null)
                return Results.NotFound();

            var now = DateTimeOffset.UtcNow;
            var oldSheet = entry.Sheet;
            var destinationDate = request.Date ?? entry.Date;
            if (destinationDate != entry.Date)
            {
                var destinationSheet = await db.SymptomDiarySheets
                    .SingleOrDefaultAsync(x => x.UserId == user.Id && x.Date == destinationDate, ct);
                if (destinationSheet is null)
                {
                    destinationSheet = new SymptomDiarySheet { UserId = user.Id, Date = destinationDate };
                    db.SymptomDiarySheets.Add(destinationSheet);
                }

                entry.Date = destinationDate;
                entry.Sheet = destinationSheet;
                entry.SheetId = destinationSheet.Id;
                if (oldSheet is not null) oldSheet.UpdatedAt = now;
                destinationSheet.UpdatedAt = now;
            }

            if (request.RecordedAt is not null) entry.RecordedAt = request.RecordedAt.Value.ToUniversalTime();
            if (request.Category is not null) entry.Category = ParseSymptomCategory(request.Category);
            if (request.SymptomName is not null) entry.SymptomName = request.SymptomName.Trim();
            if (request.Severity is not null) entry.Severity = request.Severity.Value;
            if (request.DurationMinutes is not null) entry.DurationMinutes = request.DurationMinutes;
            if (request.PainType is not null) entry.PainType = string.IsNullOrWhiteSpace(request.PainType) ? null : request.PainType.Trim();
            if (request.BodyLocation is not null) entry.BodyLocation = string.IsNullOrWhiteSpace(request.BodyLocation) ? null : request.BodyLocation.Trim();
            if (request.Triggers is not null) entry.Triggers = string.IsNullOrWhiteSpace(request.Triggers) ? null : request.Triggers.Trim();
            if (request.Relievers is not null) entry.Relievers = string.IsNullOrWhiteSpace(request.Relievers) ? null : request.Relievers.Trim();
            if (request.MedicationsTaken is not null) entry.MedicationsTaken = string.IsNullOrWhiteSpace(request.MedicationsTaken) ? null : request.MedicationsTaken.Trim();
            if (request.Notes is not null) entry.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            entry.UpdatedAt = now;
            if (oldSheet is not null) oldSheet.UpdatedAt = now;
            if (entry.Sheet is not null) entry.Sheet.UpdatedAt = now;

            await db.SaveChangesAsync(ct);
            return Results.Ok(ToSymptomDiaryEntryDto(entry));
        });

        botApi.MapPost("/symptom-entries/quick-text", async (BotQuickLogTextRequest request, HttpContext context, ClaimsPrincipal principal, MedMatchDbContext db, ILogger<Program> logger, CancellationToken ct) =>
        {
            var user = await ResolveBotOrUserAsync(context, principal, db, logger, ct);
            if (user is null)
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Text))
                return Results.BadRequest(new { error = "Text is required." });

            var date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var sheet = await db.SymptomDiarySheets.SingleOrDefaultAsync(x => x.UserId == user.Id && x.Date == date, ct);
            if (sheet is null)
            {
                sheet = new SymptomDiarySheet { UserId = user.Id, Date = date };
                db.SymptomDiarySheets.Add(sheet);
                await db.SaveChangesAsync(ct);
            }

            var cleanText = request.Text.Trim();
            var title = cleanText.Length > 80 ? cleanText[..80] + "..." : cleanText;
            var entry = new SymptomDiaryEntry
            {
                SheetId = sheet.Id,
                UserId = user.Id,
                Date = date,
                RecordedAt = DateTimeOffset.UtcNow,
                Category = SymptomCategory.Pain,
                SymptomName = title,
                Severity = 5,
                Notes = cleanText,
                Source = "n8n-quick"
            };

            db.SymptomDiaryEntries.Add(entry);
            sheet.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Ok(ToSymptomDiaryEntryDto(entry));
        });

        return api;
    }

    private static async Task<User?> ResolveBotOrUserAsync(HttpContext httpContext, ClaimsPrincipal principal, MedMatchDbContext db, ILogger<Program> logger, CancellationToken ct)
    {
        var botUser = await BotApiKeyAuth.AuthenticateBotKeyAsync(httpContext, db, logger, ct);
        if (botUser is not null) return botUser;

        if (principal.Identity?.IsAuthenticated == true)
        {
            var id = UserId(principal);
            return await db.Users.FindAsync([id], ct);
        }

        logger.LogWarning(
            "Unauthorized bot API request. No valid bot key or authenticated user was found. Path: {RequestPath}; X-API-Key header present: {HasApiKeyHeader}; Authorization header present: {HasAuthorizationHeader}",
            httpContext.Request.Path,
            httpContext.Request.Headers.ContainsKey("X-API-Key"),
            httpContext.Request.Headers.ContainsKey("Authorization"));
        return null;
    }
}
