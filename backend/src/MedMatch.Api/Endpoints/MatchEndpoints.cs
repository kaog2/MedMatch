using System.Security.Claims;
using MedMatch.Api.Observability;
using MedMatch.Api.Services;
using MedMatch.Application.Contracts;
using MedMatch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using static MedMatch.Api.Mapping.DtoMapper;
using static MedMatch.Api.Security.UserIdentity;

namespace MedMatch.Api.Endpoints;

/// <summary>Peer matches and match notifications.</summary>
public static class MatchEndpoints
{
    public static RouteGroupBuilder MapMatchEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/matches", async (string? country, string? city, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var currentUserId = UserId(principal);
            var current = await db.PatientProfiles
                .Include(x => x.User).ThenInclude(x => x.ConsentSettings)
                .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
                .SingleOrDefaultAsync(x => x.UserId == currentUserId, ct);

            if (current is null || current.User.ConsentSettings is null ||
                !current.User.ConsentSettings.PatientsContactMe || !current.User.ConsentSettings.DataForSearch)
            {
                return Results.Ok(Array.Empty<MatchDto>());
            }

            var candidates = await db.PatientProfiles
                .Include(x => x.User).ThenInclude(x => x.ConsentSettings)
                .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
                .Where(x => x.UserId != currentUserId && x.User.ConsentSettings != null && x.User.ConsentSettings.PatientsContactMe && x.User.ConsentSettings.DataForSearch)
                .AsNoTracking().ToListAsync(ct);

            var matches = candidates
                .Select(candidate =>
                {
                    var (score, sharedDiagnoses, sharedSymptoms, sameLoc) = DiagnosisMatching.EvaluateMatch(current, candidate);
                    return new
                    {
                        Score = score,
                        SharedDiagnoses = sharedDiagnoses,
                        SharedSymptoms = sharedSymptoms,
                        SameLocation = sameLoc,
                        Profile = candidate
                    };
                })
                .Where(x => x.Score > 0 && x.SharedDiagnoses.Length > 0)
                .Where(x => string.IsNullOrWhiteSpace(country) || string.Equals(x.Profile.Country?.Trim(), country.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(city) || (x.Profile.City ?? "").Contains(city.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.SharedDiagnoses.Length)
                .Select(x => new MatchDto(
                    x.Profile.UserId,
                    DisplayName(x.Profile),
                    x.Profile.City,
                    x.Profile.Country,
                    x.SharedDiagnoses,
                    x.SharedSymptoms,
                    x.SameLocation,
                    x.Score,
                    x.Profile.Bio,
                    x.Profile.Languages
                ))
                .ToList();

            sw.Stop();
            MedMatchMetrics.RecordMatchesComputed(matches.Count, sw.Elapsed.TotalMilliseconds);

            return Results.Ok(matches);
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

        api.MapGet("/matches/summary", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var currentUserId = UserId(principal);
            var unread = await db.MatchNotifications.AsNoTracking().CountAsync(x => x.UserId == currentUserId && !x.IsRead, ct);
            return Results.Ok(new MatchSummaryDto(unread));
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

        api.MapGet("/matches/notifications", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var currentUserId = UserId(principal);
            var notifications = await db.MatchNotifications.AsNoTracking().Where(x => x.UserId == currentUserId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
            var matchedIds = notifications.Select(x => x.MatchedUserId).Distinct().ToList();
            var profiles = await db.PatientProfiles.AsNoTracking().Where(x => matchedIds.Contains(x.UserId)).ToDictionaryAsync(x => x.UserId, ct);
            return Results.Ok(notifications.Select(x => new MatchNotificationDto(x.Id, x.MatchedUserId, profiles.TryGetValue(x.MatchedUserId, out var p) ? DisplayName(p) : "MedMatch member", x.SharedDiagnoses, x.Score, x.IsRead, x.CreatedAt)));
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

        api.MapPost("/matches/notifications/read", async (Guid? id, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var currentUserId = UserId(principal);
            var query = db.MatchNotifications.Where(x => x.UserId == currentUserId && !x.IsRead);
            if (id.HasValue && id.Value != Guid.Empty)
            {
                query = query.Where(x => x.Id == id.Value);
            }
            var unread = await query.ToListAsync(ct);
            foreach (var n in unread) n.IsRead = true;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

        return api;
    }
}
