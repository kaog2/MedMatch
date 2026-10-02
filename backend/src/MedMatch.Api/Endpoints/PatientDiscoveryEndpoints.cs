using System.Security.Claims;
using MedMatch.Application.Contracts;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using static MedMatch.Api.Mapping.DtoMapper;
using static MedMatch.Api.Security.UserIdentity;

namespace MedMatch.Api.Endpoints;

/// <summary>Consent-gated discovery of patients by clinics and by other patients.</summary>
public static class PatientDiscoveryEndpoints
{
    public static RouteGroupBuilder MapPatientDiscoveryEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/clinic/patients", async (string? diagnosis, string? symptom, MedMatchDbContext db, CancellationToken ct) =>
        {
            var profiles = await db.PatientProfiles.Include(x => x.User).ThenInclude(x => x.ConsentSettings).Where(x => x.User.ConsentSettings!.ClinicsContactMe && x.User.ConsentSettings.DataForSearch).AsNoTracking().ToListAsync(ct);
            return Results.Ok(profiles.Where(x => string.IsNullOrWhiteSpace(diagnosis) || x.Diagnoses.Any(v => v.Contains(diagnosis, StringComparison.OrdinalIgnoreCase))).Where(x => string.IsNullOrWhiteSpace(symptom) || (x.Symptoms ?? "").Contains(symptom, StringComparison.OrdinalIgnoreCase)).Select(x => new { x.City, x.Country, x.Diagnoses, x.Interventions, x.Symptoms }));
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic" });

        api.MapGet("/people", async (string? diagnosis, string? symptom, string? city, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var currentUserId = UserId(principal);
            var profiles = await db.PatientProfiles
                .Include(x => x.User).ThenInclude(x => x.ConsentSettings)
                .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
                .Where(x => x.UserId != currentUserId && x.User.ConsentSettings != null && x.User.ConsentSettings.PatientsContactMe && x.User.ConsentSettings.DataForSearch)
                .AsNoTracking().ToListAsync(ct);

            var diagNormalized = string.IsNullOrWhiteSpace(diagnosis) ? null : NormalizeTag(diagnosis);

            var matches = profiles
                .Where(x => string.IsNullOrWhiteSpace(diagNormalized) ||
                            x.Diagnoses.Any(v => NormalizeTag(v).Contains(diagNormalized)) ||
                            x.DiagnosisTags.Any(dt => dt.DiagnosisTag.Slug.Contains(diagNormalized) || NormalizeTag(dt.DiagnosisTag.Name).Contains(diagNormalized)))
                .Where(x => string.IsNullOrWhiteSpace(symptom) || (x.Symptoms ?? "").Contains(symptom, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(city) || (x.City ?? "").Contains(city, StringComparison.OrdinalIgnoreCase))
                .Select(x => new PatientDirectoryDto(x.UserId, DisplayName(x), x.City, x.Country, x.Diagnoses, x.Interventions, x.Symptoms, x.Bio, x.Languages));
            return Results.Ok(matches);
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

        api.MapPost("/people/{id:guid}/connection-requests", async (Guid id, ConnectionRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            var senderId = UserId(principal);
            if (senderId == id) return Results.BadRequest(new { error = "You cannot connect with yourself." });
            var recipient = await db.Users.Include(x => x.ConsentSettings).SingleOrDefaultAsync(x => x.Id == id && x.Roles.Any(r => r.Role == UserRole.Patient), ct);
            if (recipient?.ConsentSettings is null || !recipient.ConsentSettings.PatientsContactMe) return Results.NotFound();
            var message = string.IsNullOrWhiteSpace(request.Message) ? "I would like to connect and exchange experiences through MedMatch." : request.Message.Trim();
            db.Messages.Add(new Message { FromUserId = senderId, ToUserId = id, ThreadId = Guid.NewGuid(), Content = message, ConsentSnapshot = "PatientsContactMe=true" });
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

        return api;
    }
}
