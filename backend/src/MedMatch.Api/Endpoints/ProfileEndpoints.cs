using System.Security.Claims;
using MedMatch.Api.Services;
using MedMatch.Application.Contracts;
using MedMatch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using static MedMatch.Api.Mapping.DtoMapper;
using static MedMatch.Api.Security.UserIdentity;

namespace MedMatch.Api.Endpoints;

public static class ProfileEndpoints
{
    public static RouteGroupBuilder MapProfileEndpoints(this RouteGroupBuilder api)
    {
        // GET /api/profile: returns the signed-in user's patient profile.
        api.MapGet("/profile", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var profile = await db.PatientProfiles.FindAsync([UserId(principal)], ct); return profile is null ? Results.NotFound() : Results.Ok(ToProfileDto(profile));
        }).RequireAuthorization();

        // PUT /api/profile: updates the profile and diagnosis tags, then recomputes matches (Patient).
        api.MapPut("/profile", async (PatientProfileDto dto, ClaimsPrincipal principal, MedMatchDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            var profile = await db.PatientProfiles.Include(x => x.DiagnosisTags).SingleOrDefaultAsync(x => x.UserId == userId, ct);
            if (profile is null) return Results.NotFound();
            ApplyProfile(profile, dto);
            await DiagnosisMatching.SyncDiagnosisTagsAsync(profile, dto.Diagnoses ?? [], db, ct);
            await SymptomTagExtraction.EnrichFromSymptomsAsync(profile, db, configuration, ct);
            await db.SaveChangesAsync(ct);
            await DiagnosisMatching.RefreshTagUsageCountsAsync(db, ct);
            await DiagnosisMatching.ComputeMatchesAsync(userId, db, ct);
            return Results.Ok(ToProfileDto(profile));
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

        // GET /api/consent: returns the signed-in user's consent settings.
        api.MapGet("/consent", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var settings = await db.ConsentSettings.FindAsync([UserId(principal)], ct); return settings is null ? Results.NotFound() : Results.Ok(ToConsentDto(settings));
        }).RequireAuthorization();

        // PUT /api/consent: updates consent flags with an audit trail, then recomputes matches.
        api.MapPut("/consent", async (ConsentSettingsDto dto, HttpContext context, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var userId = UserId(principal); var settings = await db.ConsentSettings.FindAsync([userId], ct); if (settings is null) return Results.NotFound();
            settings.ShowProfilePublicly = dto.ShowProfilePublicly; settings.ClinicsContactMe = dto.ClinicsContactMe; settings.PatientsContactMe = dto.PatientsContactMe; settings.DataForSearch = dto.DataForSearch; settings.Version++; settings.UpdatedAt = DateTimeOffset.UtcNow; settings.Ip = context.Connection.RemoteIpAddress?.ToString(); settings.UserAgent = context.Request.Headers.UserAgent.ToString();
            await db.SaveChangesAsync(ct); await DiagnosisMatching.ComputeMatchesAsync(userId, db, ct); return Results.Ok(ToConsentDto(settings));
        }).RequireAuthorization();

        return api;
    }
}
