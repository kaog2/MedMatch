using System.Security.Claims;
using MedMatch.Api.Services;
using MedMatch.Application.Contracts;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using static MedMatch.Api.Mapping.DtoMapper;
using static MedMatch.Api.Security.UserIdentity;

namespace MedMatch.Api.Endpoints;

/// <summary>Administrator user management. Recommendation moderation lives in <see cref="RecommendationEndpoints"/>.</summary>
public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/admin/users", async (string? search, string? role, int? page, int? pageSize, MedMatchDbContext db, CancellationToken ct) =>
        {
            var query = db.Users.Include(x => x.PatientProfile).Include(x => x.ConsentSettings).Include(x => x.Roles).AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var terms = search.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).ToArray();
                foreach (var term in terms)
                {
                    query = query.Where(x =>
                        x.Email.ToLower().Contains(term) ||
                        (x.PatientProfile != null && (
                            (x.PatientProfile.Pseudonym != null && x.PatientProfile.Pseudonym.ToLower().Contains(term)) ||
                            (x.PatientProfile.RealName != null && x.PatientProfile.RealName.ToLower().Contains(term))
                        )));
                }
            }
            if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var r))
            {
                query = query.Where(x => x.Roles.Any(a => a.Role == r));
            }

            var size = Math.Clamp(pageSize ?? 25, 1, 100);
            var currentPage = Math.Max(page ?? 1, 1);
            var total = await query.CountAsync(ct);
            var users = await query.OrderByDescending(x => x.CreatedAt).Skip((currentPage - 1) * size).Take(size).ToListAsync(ct);

            var result = users.Select(u => new AdminUserDto(
                u.Id,
                u.Email,
                u.Roles.Select(a => a.Role.ToString()).OrderBy(x => x).ToArray(),
                u.PatientProfile is null ? null : DisplayName(u.PatientProfile),
                u.PatientProfile?.City,
                u.PatientProfile?.Country,
                u.PatientProfile?.Diagnoses ?? [],
                u.PatientProfile?.Symptoms ?? string.Empty,
                u.EmailConfirmed,
                u.IsActive,
                u.ConsentSettings?.PatientsContactMe ?? false,
                u.ConsentSettings?.DataForSearch ?? false,
                u.CreatedAt
            ));

            return Results.Ok(new { total, page = currentPage, pageSize = size, items = result });
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        api.MapGet("/admin/users/{id:guid}/matches", async (Guid id, MedMatchDbContext db, CancellationToken ct) =>
        {
            var target = await db.PatientProfiles
                .Include(x => x.User).ThenInclude(x => x.ConsentSettings)
                .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
                .SingleOrDefaultAsync(x => x.UserId == id, ct);

            if (target is null) return Results.NotFound();

            var candidates = await db.PatientProfiles
                .Include(x => x.User).ThenInclude(x => x.ConsentSettings)
                .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
                .Where(x => x.UserId != id && x.User.ConsentSettings != null && x.User.ConsentSettings.PatientsContactMe && x.User.ConsentSettings.DataForSearch)
                .AsNoTracking().ToListAsync(ct);

            var matches = candidates
                .Select(candidate =>
                {
                    var (score, sharedDiagnoses, sharedSymptoms, sameLoc) = DiagnosisMatching.EvaluateMatch(target, candidate);
                    return new { Score = score, SharedDiagnoses = sharedDiagnoses, SharedSymptoms = sharedSymptoms, SameLocation = sameLoc, Profile = candidate };
                })
                .Where(x => x.Score > 0 && x.SharedDiagnoses.Length > 0)
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
                ));

            return Results.Ok(matches);
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        api.MapPost("/admin/users/{id:guid}/active", async (Guid id, UpdateActiveRequest request, MedMatchDbContext db, CancellationToken ct) =>
        {
            var user = await db.Users.Include(x => x.Roles).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (user is null) return Results.NotFound();

            if (!request.IsActive && user.IsActive && user.Roles.Any(x => x.Role == UserRole.Admin))
            {
                var activeAdminCount = await db.Users
                    .CountAsync(x => x.IsActive && x.Roles.Any(role => role.Role == UserRole.Admin), ct);
                if (activeAdminCount <= 1)
                    return Results.BadRequest(new { error = "At least one active administrator must remain." });
            }

            user.IsActive = request.IsActive;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { id = user.Id, isActive = user.IsActive });
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        api.MapPost("/admin/users/{id:guid}/role", async (Guid id, UpdateRoleRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
        {
            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
                return Results.BadRequest(new { error = "Invalid role. Use Patient, Clinic, Doctor or Admin." });

            var user = await db.Users.Include(x => x.Roles).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (user is null) return Results.NotFound();

            var currentAdminId = UserId(principal);
            var existing = user.Roles.FirstOrDefault(a => a.Role == role);

            if (request.Enabled && existing is null)
            {
                user.Roles.Add(new UserRoleAssignment { Role = role });
                if (role == UserRole.Patient && user.PatientProfile is null)
                    user.PatientProfile = new PatientProfile();
            }
            else if (!request.Enabled && existing is not null)
            {
                if (role == UserRole.Admin)
                {
                    if (id == currentAdminId)
                        return Results.BadRequest(new { error = "You cannot remove your own administrator role." });
                    var adminCount = await db.Users.CountAsync(x => x.Roles.Any(a => a.Role == UserRole.Admin), ct);
                    if (adminCount <= 1)
                        return Results.BadRequest(new { error = "At least one administrator must remain." });
                }
                if (user.Roles.Count <= 1)
                    return Results.BadRequest(new { error = "A user must keep at least one role." });
                user.Roles.Remove(existing);
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { id = user.Id, roles = user.Roles.Select(a => a.Role.ToString()).OrderBy(x => x).ToArray() });
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        return api;
    }
}
