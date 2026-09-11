using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using MedMatch.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MedMatch.Api.Services;

/// <summary>
/// Creates the administrator account from ADMIN_EMAIL / ADMIN_PASSWORD configuration.
/// Idempotent: skips when an account with the configured email already exists.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(MedMatchDbContext db, PasswordHasher passwords, IConfiguration configuration, CancellationToken ct)
    {
        var email = (configuration["ADMIN_EMAIL"] ?? "admin@medmatch.test").Trim().ToLowerInvariant();
        var password = configuration["ADMIN_PASSWORD"] ?? "AdminPass123!";

        if (await db.Users.AnyAsync(x => x.Email == email, ct)) return;

        var admin = new User
        {
            Email = email,
            PasswordHash = passwords.Hash(password),
            EmailConfirmed = true,
            IsActive = true,
            ConsentSettings = new ConsentSettings()
        };
        admin.Roles.Add(new UserRoleAssignment { Role = UserRole.Admin });
        db.Users.Add(admin);

        await db.SaveChangesAsync(ct);
    }
}
