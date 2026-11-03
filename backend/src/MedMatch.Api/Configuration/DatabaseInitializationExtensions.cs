using MedMatch.Api.Services;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using MedMatch.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace MedMatch.Api.Configuration;

public static class DatabaseInitializationExtensions
{
    /// <summary>Applies migrations and runs the standard and opt-in seeders.</summary>
    public static async Task InitializeMedMatchDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MedMatchDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
        var configuration = app.Configuration;

        await db.Database.MigrateAsync();
        await DiagnosisMatching.SeedDiagnosisTagsAsync(db, CancellationToken.None);
        await DiagnosisTagLocalization.SeedAsync(db, CancellationToken.None);
        await AdminSeeder.SeedAsync(db, passwords, configuration, CancellationToken.None);
        if (CareProviderSampleDataSeeder.IsEnabled(configuration))
            await CareProviderSampleDataSeeder.SeedAsync(db, CancellationToken.None);
        if (SampleDataSeeder.IsEnabled(configuration))
            await SampleDataSeeder.SeedAsync(db, passwords, CancellationToken.None);
        if (CaseStudySeeder.IsEnabled(configuration))
            await CaseStudySeeder.SeedAsync(db, passwords, CancellationToken.None);
        await RestorePatientRolesAsync(db, CancellationToken.None);
    }

    /// <summary>
    /// Ensures users who still have a patient profile keep the Patient role.
    /// Covers accounts promoted to Admin before multi-role support existed.
    /// </summary>
    private static async Task RestorePatientRolesAsync(MedMatchDbContext db, CancellationToken ct)
    {
        var missing = await db.Users
            .Include(x => x.Roles)
            .Where(x => x.PatientProfile != null && !x.Roles.Any(r => r.Role == UserRole.Patient))
            .ToListAsync(ct);

        foreach (var user in missing)
            user.Roles.Add(new UserRoleAssignment { Role = UserRole.Patient });

        if (missing.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
