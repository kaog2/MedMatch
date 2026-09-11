using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using MedMatch.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using static MedMatch.Api.Mapping.DtoMapper;

namespace MedMatch.Api.Services;

/// <summary>
/// Inserts 100 fictional patients for local testing of diagnosis matching and peer discovery.
/// Enabled only when SEED_SAMPLE_DATA is "true". Idempotent: skips when sample users already exist.
/// </summary>
public static class SampleDataSeeder
{
    private static readonly string[] Cities =
    [
        "Berlin", "Munich", "Hamburg", "Cologne", "Frankfurt", "Stuttgart", "Leipzig", "Dresden",
        "Madrid", "Barcelona", "Valencia", "Seville",
        "Paris", "Lyon", "Marseille",
        "Rome", "Milan", "Naples", "Turin",
        "Vienna", "Graz",
        "Zurich", "Geneva", "Basel",
        "London", "Manchester", "Birmingham",
        "Amsterdam", "Rotterdam",
        "Brussels", "Antwerp"
    ];

    private static readonly (string City, string Country)[] Locations =
    [
        ("Berlin", "Germany"), ("Munich", "Germany"), ("Hamburg", "Germany"), ("Cologne", "Germany"),
        ("Frankfurt", "Germany"), ("Stuttgart", "Germany"), ("Leipzig", "Germany"), ("Dresden", "Germany"),
        ("Madrid", "Spain"), ("Barcelona", "Spain"), ("Valencia", "Spain"), ("Seville", "Spain"),
        ("Paris", "France"), ("Lyon", "France"), ("Marseille", "France"),
        ("Rome", "Italy"), ("Milan", "Italy"), ("Naples", "Italy"), ("Turin", "Italy"),
        ("Vienna", "Austria"), ("Graz", "Austria"),
        ("Zurich", "Switzerland"), ("Geneva", "Switzerland"), ("Basel", "Switzerland"),
        ("London", "United Kingdom"), ("Manchester", "United Kingdom"), ("Birmingham", "United Kingdom"),
        ("Amsterdam", "Netherlands"), ("Rotterdam", "Netherlands"),
        ("Brussels", "Belgium"), ("Antwerp", "Belgium")
    ];

    private static readonly string[] Symptoms =
    [
        "lower back pain", "chronic fatigue", "joint stiffness", "shortness of breath", "headache",
        "muscle weakness", "sleep disturbances", "anxiety", "nausea", "skin irritation",
        "vision changes", "frequent urination", "memory problems", "dizziness", "chest tightness"
    ];

    private static readonly string[] Pseudonyms =
    [
        "River", "Sage", "Ember", "Ash", "Oak", "Wren", "Nova", "Juno", "Iris", "Cedar",
        "Luna", "Orion", "Atlas", "Lyra", "Echo", "Sol", "Vale", "Hale", "Finn", "Rune"
    ];

    private static readonly string[] Bios =
    [
        "Sharing my journey to help others feel less alone.",
        "Open to exchanging experiences and practical tips.",
        "Looking for peers who understand the day-to-day reality.",
        "Happy to talk about treatments that worked for me.",
        "Here to learn from others and share what I know.",
        "Focused on recovery and mutual support."
    ];

    public static bool IsEnabled(IConfiguration configuration) =>
        string.Equals(configuration["SEED_SAMPLE_DATA"], "true", StringComparison.OrdinalIgnoreCase);

    public static async Task SeedAsync(MedMatchDbContext db, PasswordHasher passwords, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(x => x.Email.StartsWith("sample-") && x.Email.EndsWith("@medmatch.test"), ct))
            return;

        var tags = await db.DiagnosisTags.ToListAsync(ct);
        var tagByName = tags.ToDictionary(x => NormalizeTag(x.Name), StringComparer.OrdinalIgnoreCase);
        var diagnoses = tags.Select(x => x.Name).ToArray();

        var random = new Random(20260911);
        var passwordHash = passwords.Hash("SamplePass123!");

        var users = new List<User>(100);
        for (var i = 1; i <= 100; i++)
        {
            var (city, country) = Locations[random.Next(Locations.Length)];
            var displayMode = random.Next(100) < 60 ? DisplayMode.Pseudonym : DisplayMode.RealName;
            var name = displayMode == DisplayMode.Pseudonym
                ? $"{Pseudonyms[random.Next(Pseudonyms.Length)]}{random.Next(10, 99)}"
                : $"Sample Person {i}";

            var diagnosisCount = random.Next(1, 5);
            var selectedDiagnoses = diagnoses.OrderBy(_ => random.Next()).Take(diagnosisCount).ToArray();
            var selectedSymptoms = Symptoms.OrderBy(_ => random.Next()).Take(random.Next(1, 4)).ToArray();

            var profile = new PatientProfile
            {
                DisplayMode = displayMode,
                Pseudonym = displayMode == DisplayMode.Pseudonym ? name : null,
                RealName = displayMode == DisplayMode.RealName ? name : null,
                City = city,
                Country = country,
                Diagnoses = selectedDiagnoses,
                Symptoms = string.Join(", ", selectedSymptoms),
                Languages = random.Next(100) < 70 ? ["en"] : ["de", "en"],
                Bio = Bios[random.Next(Bios.Length)]
            };

            foreach (var diagnosisName in selectedDiagnoses)
            {
                var key = NormalizeTag(diagnosisName);
                if (!tagByName.TryGetValue(key, out var tag)) continue;
                profile.DiagnosisTags.Add(new PatientDiagnosisTag { DiagnosisTagId = tag.Id });
            }

            var user = new User
            {
                Email = $"sample-{i:000}@medmatch.test",
                PasswordHash = passwordHash,
                Roles = [new UserRoleAssignment { Role = UserRole.Patient }],
                EmailConfirmed = true,
                PatientProfile = profile,
                ConsentSettings = new ConsentSettings
                {
                    ShowProfilePublicly = true,
                    PatientsContactMe = true,
                    ClinicsContactMe = false,
                    DataForSearch = true,
                    Version = 1
                }
            };

            users.Add(user);
        }

        db.Users.AddRange(users);
        await db.SaveChangesAsync(ct);
        await DiagnosisMatching.RefreshTagUsageCountsAsync(db, ct);
    }
}
