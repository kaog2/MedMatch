using System.Security.Claims;
using MedMatch.Api.Security;
using MedMatch.Application.Contracts;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using static MedMatch.Api.Mapping.DtoMapper;
using static MedMatch.Api.Security.UserIdentity;

namespace MedMatch.Api.Endpoints;

/// <summary>Symptom diary used by the patient web UI, including bot API key management.</summary>
public static class SymptomDiaryEndpoints
{
    public static RouteGroupBuilder MapSymptomDiaryEndpoints(this RouteGroupBuilder api)
    {
        var diaryApi = api.MapGroup("/symptom-diary").RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

        diaryApi.MapGet("/sheets", async (DateOnly? startDate, DateOnly? endDate, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
            var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var sheets = await db.SymptomDiarySheets
                .Include(x => x.Entries)
                .Where(x => x.UserId == userId && x.Date >= start && x.Date <= end)
                .OrderByDescending(x => x.Date)
                .AsNoTracking()
                .ToListAsync(ct);

            return Results.Ok(sheets.Select(ToSymptomDiarySheetSummaryDto));
        });

        diaryApi.MapGet("/sheets/{date}", async (DateOnly date, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var sheet = await db.SymptomDiarySheets
                .Include(x => x.Entries)
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.UserId == userId && x.Date == date, ct);

            if (sheet is null)
            {
                return Results.Ok(new SymptomDiarySheetDto(
                    Guid.Empty,
                    date,
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

        diaryApi.MapPut("/sheets/{date}", async (DateOnly date, UpdateDailySheetRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var sheet = await db.SymptomDiarySheets
                .Include(x => x.Entries)
                .SingleOrDefaultAsync(x => x.UserId == userId && x.Date == date, ct);

            if (sheet is null)
            {
                sheet = new SymptomDiarySheet
                {
                    UserId = userId,
                    Date = date,
                    OverallWellbeing = request.OverallWellbeing,
                    SleepQuality = request.SleepQuality,
                    SleepHours = request.SleepHours,
                    DailyNotes = request.DailyNotes?.Trim()
                };
                db.SymptomDiarySheets.Add(sheet);
            }
            else
            {
                sheet.OverallWellbeing = request.OverallWellbeing;
                sheet.SleepQuality = request.SleepQuality;
                sheet.SleepHours = request.SleepHours;
                sheet.DailyNotes = request.DailyNotes?.Trim();
                sheet.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(ToSymptomDiarySheetDto(sheet));
        });

        diaryApi.MapPost("/sheets/{date}/entries", async (DateOnly date, UpsertSymptomEntryRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.SymptomName))
                return Results.BadRequest(new { error = "Symptom name is required." });

            var userId = UserId(principal);
            var sheet = await db.SymptomDiarySheets.SingleOrDefaultAsync(x => x.UserId == userId && x.Date == date, ct);
            if (sheet is null)
            {
                sheet = new SymptomDiarySheet { UserId = userId, Date = date };
                db.SymptomDiarySheets.Add(sheet);
                await db.SaveChangesAsync(ct);
            }

            var entry = new SymptomDiaryEntry
            {
                SheetId = sheet.Id,
                UserId = userId,
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
                Source = string.IsNullOrWhiteSpace(request.Source) ? "Web" : request.Source.Trim()
            };

            db.SymptomDiaryEntries.Add(entry);
            sheet.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/symptom-diary/entries/{entry.Id}", ToSymptomDiaryEntryDto(entry));
        });

        diaryApi.MapPut("/entries/{id:guid}", async (Guid id, UpsertSymptomEntryRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.SymptomName))
                return Results.BadRequest(new { error = "Symptom name is required." });

            var userId = UserId(principal);
            var entry = await db.SymptomDiaryEntries.Include(x => x.Sheet).SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
            if (entry is null) return Results.NotFound();

            entry.RecordedAt = request.RecordedAt?.ToUniversalTime() ?? entry.RecordedAt.ToUniversalTime();
            entry.Category = ParseSymptomCategory(request.Category);
            entry.SymptomName = request.SymptomName.Trim();
            entry.PainType = request.PainType?.Trim();
            entry.BodyLocation = request.BodyLocation?.Trim();
            entry.Severity = Math.Clamp(request.Severity, 0, 10);
            entry.DurationMinutes = request.DurationMinutes;
            entry.Triggers = request.Triggers?.Trim();
            entry.Relievers = request.Relievers?.Trim();
            entry.MedicationsTaken = request.MedicationsTaken?.Trim();
            entry.Notes = request.Notes?.Trim();
            if (!string.IsNullOrWhiteSpace(request.Source)) entry.Source = request.Source.Trim();
            entry.UpdatedAt = DateTimeOffset.UtcNow;
            if (entry.Sheet != null) entry.Sheet.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);
            return Results.Ok(ToSymptomDiaryEntryDto(entry));
        });

        diaryApi.MapDelete("/entries/{id:guid}", async (Guid id, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var entry = await db.SymptomDiaryEntries.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
            if (entry is null) return Results.NotFound();

            db.SymptomDiaryEntries.Remove(entry);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        diaryApi.MapGet("/summary", async (int? days, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var lookback = Math.Clamp(days ?? 30, 1, 365);
            var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-lookback));
            var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var entries = await db.SymptomDiaryEntries
                .Where(x => x.UserId == userId && x.Date >= startDate && x.Date <= endDate)
                .OrderBy(x => x.RecordedAt)
                .AsNoTracking()
                .ToListAsync(ct);

            var sheets = await db.SymptomDiarySheets
                .Where(x => x.UserId == userId && x.Date >= startDate && x.Date <= endDate)
                .AsNoTracking()
                .ToListAsync(ct);

            var totalEntries = entries.Count;
            var trackedDates = entries.Select(x => x.Date).Concat(sheets.Select(x => x.Date)).Distinct().Count();
            var overallAvg = totalEntries > 0 ? Math.Round(entries.Average(x => x.Severity), 1) : 0;

            var trend = entries
                .GroupBy(x => x.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailySeverityPointDto(
                    g.Key,
                    Math.Round(g.Average(x => x.Severity), 1),
                    g.Max(x => x.Severity),
                    g.Count()
                ))
                .ToList();

            var topSymptoms = entries
                .GroupBy(x => new { Name = x.SymptomName.Trim(), Category = x.Category.ToString() })
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new SymptomFrequencyDto(
                    g.Key.Name,
                    g.Key.Category,
                    g.Count(),
                    Math.Round(g.Average(x => x.Severity), 1)
                ))
                .ToList();

            var topLocations = entries
                .Where(x => !string.IsNullOrWhiteSpace(x.BodyLocation))
                .SelectMany(x => x.BodyLocation!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(loc => !string.IsNullOrWhiteSpace(loc))
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Take(8)
                .Select(g => new LocationFrequencyDto(g.Key, g.Count()))
                .ToList();

            var topPainTypes = entries
                .Where(x => !string.IsNullOrWhiteSpace(x.PainType))
                .SelectMany(x => x.PainType!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(pt => !string.IsNullOrWhiteSpace(pt))
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Take(8)
                .Select(g => new PainTypeFrequencyDto(g.Key, g.Count()))
                .ToList();

            return Results.Ok(new SymptomDiaryAnalyticsDto(
                totalEntries,
                trackedDates,
                overallAvg,
                trend,
                topSymptoms,
                topLocations,
                topPainTypes
            ));
        });

        diaryApi.MapGet("/bot-key", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var key = await db.UserBotApiKeys
                .Where(x => x.UserId == userId && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            return Results.Ok(key is null ? null : ToBotApiKeyDto(key));
        });

        diaryApi.MapPost("/bot-key", async (CreateBotKeyRequest? request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);

            var existingKeys = await db.UserBotApiKeys.Where(x => x.UserId == userId && x.IsActive).ToListAsync(ct);
            foreach (var k in existingKeys) k.IsActive = false;

            var (rawKey, hash, prefix) = BotApiKeyAuth.GenerateKey();
            var key = new UserBotApiKey
            {
                UserId = userId,
                KeyHash = hash,
                KeyPrefix = prefix,
                Label = string.IsNullOrWhiteSpace(request?.Label) ? "n8n Chatbot" : request.Label.Trim(),
                IsActive = true
            };
            db.UserBotApiKeys.Add(key);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new CreateBotKeyResponse(key.Id, rawKey, prefix, key.Label, key.CreatedAt));
        });

        diaryApi.MapDelete("/bot-key/{id:guid}", async (Guid id, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var key = await db.UserBotApiKeys.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
            if (key is null) return Results.NotFound();

            key.IsActive = false;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return api;
    }
}
