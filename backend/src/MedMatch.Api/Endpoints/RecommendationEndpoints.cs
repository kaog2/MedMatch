using System.Security.Claims;
using MedMatch.Api.Observability;
using MedMatch.Api.Services;
using MedMatch.Application.Contracts;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using static MedMatch.Api.Mapping.DtoMapper;
using static MedMatch.Api.Security.UserIdentity;

namespace MedMatch.Api.Endpoints;

/// <summary>Patient recommendations of care providers, including administrator moderation.</summary>
public static class RecommendationEndpoints
{
    public static RouteGroupBuilder MapRecommendationEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/recommendations", async (CreateRecommendationRequest request, ClaimsPrincipal principal, MedMatchDbContext db, IContentModerationService moderation, IConfiguration configuration, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var clinicIds = (request.ClinicIds ?? []).Distinct().ToArray();
            var diagnoses = (request.Diagnoses ?? []).Select(CleanTagDisplay).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var details = (request.Details ?? string.Empty).Trim();

            if (clinicIds.Length == 0) return Results.BadRequest(new { error = "Select at least one care provider." });
            if (diagnoses.Length == 0) return Results.BadRequest(new { error = "Select at least one diagnosis or symptom." });
            if (details.Length is < 10 or > 2000) return Results.BadRequest(new { error = "Please describe your experience (10–2000 characters)." });

            var clinics = await db.Clinics.Where(x => clinicIds.Contains(x.Id)).ToListAsync(ct);
            if (clinics.Count != clinicIds.Length) return Results.BadRequest(new { error = "One or more care providers were not found." });

            var tags = await DiagnosisMatching.ResolveDiagnosisTagsAsync(db, diagnoses, ct);

            var recommendation = new Recommendation { AuthorUserId = userId, Details = details };
            foreach (var clinic in clinics) recommendation.Clinics.Add(new RecommendationClinic { Clinic = clinic });
            foreach (var tag in tags) recommendation.DiagnosisTags.Add(new RecommendationDiagnosisTag { DiagnosisTag = tag });

            if (moderation.IsEnabled)
            {
                try
                {
                    var verdict = await moderation.ModerateAsync(details, tags.Select(t => t.Name).ToArray(), clinics.Select(c => c.Name).ToArray(), ct);
                    recommendation.Status = verdict.Approved ? RecommendationStatus.Approved : RecommendationStatus.Rejected;
                    recommendation.ModerationNote = verdict.Reason;
                    recommendation.ReviewedAt = DateTimeOffset.UtcNow;
                }
                catch
                {
                    // Fail-safe: never publish content that could not be verified.
                    recommendation.Status = RecommendationStatus.Pending;
                    recommendation.ModerationNote = "Moderation service unavailable; awaiting manual review.";
                }
            }
            else
            {
                var autoApprove = !string.Equals(configuration["LLM_MODERATION_AUTO_APPROVE"], "false", StringComparison.OrdinalIgnoreCase);
                recommendation.Status = autoApprove ? RecommendationStatus.Approved : RecommendationStatus.Pending;
                recommendation.ModerationNote = autoApprove ? null : "Awaiting review.";
                if (autoApprove) recommendation.ReviewedAt = DateTimeOffset.UtcNow;
            }

            db.Recommendations.Add(recommendation);
            await db.SaveChangesAsync(ct);
            MedMatchMetrics.RecordRecommendation(recommendation.Status.ToString());

            var created = await LoadRecommendation(db, recommendation.Id, ct);
            return Results.Created($"/api/recommendations/{created!.Id}", ToRecommendationDto(created));
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

        api.MapGet("/recommendations", async (Guid? clinicId, string? diagnosis, MedMatchDbContext db, CancellationToken ct) =>
        {
            var query = db.Recommendations
                .Include(x => x.AuthorUser).ThenInclude(x => x.PatientProfile)
                .Include(x => x.Clinics).ThenInclude(x => x.Clinic)
                .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
                .Where(x => x.Status == RecommendationStatus.Approved)
                .AsNoTracking().AsQueryable();

            if (clinicId.HasValue && clinicId.Value != Guid.Empty)
                query = query.Where(x => x.Clinics.Any(c => c.ClinicId == clinicId.Value));

            if (!string.IsNullOrWhiteSpace(diagnosis))
            {
                var norm = NormalizeTag(diagnosis);
                query = query.Where(x => x.DiagnosisTags.Any(dt => dt.DiagnosisTag.Slug.Contains(ToSlug(norm)) || NormalizeTag(dt.DiagnosisTag.Name).Contains(norm)));
            }

            var recommendations = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
            return Results.Ok(recommendations.Select(ToRecommendationDto));
        }).AllowAnonymous();

        api.MapGet("/recommendations/mine", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var recommendations = await db.Recommendations
                .Include(x => x.AuthorUser).ThenInclude(x => x.PatientProfile)
                .Include(x => x.Clinics).ThenInclude(x => x.Clinic)
                .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
                .Where(x => x.AuthorUserId == userId)
                .AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
            return Results.Ok(recommendations.Select(ToRecommendationDto));
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

        api.MapGet("/admin/recommendations", async (string? status, MedMatchDbContext db, CancellationToken ct) =>
        {
            var query = db.Recommendations
                .Include(x => x.AuthorUser).ThenInclude(x => x.PatientProfile)
                .Include(x => x.Clinics).ThenInclude(x => x.Clinic)
                .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
                .AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RecommendationStatus>(status, true, out var parsed))
                query = query.Where(x => x.Status == parsed);

            var recommendations = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
            return Results.Ok(recommendations.Select(ToRecommendationDto));
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        api.MapPost("/admin/recommendations/{id:guid}/moderate", async (Guid id, ModerateRecommendationRequest request, MedMatchDbContext db, CancellationToken ct) =>
        {
            var recommendation = await db.Recommendations.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (recommendation is null) return Results.NotFound();
            recommendation.Status = request.Approve ? RecommendationStatus.Approved : RecommendationStatus.Rejected;
            recommendation.ModerationNote = string.IsNullOrWhiteSpace(request.Note) ? (request.Approve ? "Approved by administrator." : "Rejected by administrator.") : request.Note.Trim();
            recommendation.ReviewedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { id = recommendation.Id, status = recommendation.Status.ToString(), moderationNote = recommendation.ModerationNote });
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        api.MapDelete("/recommendations/{id:guid}", async (Guid id, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var isAdmin = principal.IsInRole(UserRole.Admin.ToString());
            var recommendation = await db.Recommendations.SingleOrDefaultAsync(x => x.Id == id && (isAdmin || x.AuthorUserId == userId), ct);
            if (recommendation is null) return Results.NotFound();
            db.Recommendations.Remove(recommendation);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient,Admin" });

        return api;
    }

    private static async Task<Recommendation?> LoadRecommendation(MedMatchDbContext db, Guid id, CancellationToken ct) =>
        await db.Recommendations
            .Include(x => x.AuthorUser).ThenInclude(x => x.PatientProfile)
            .Include(x => x.Clinics).ThenInclude(x => x.Clinic)
            .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);
}
