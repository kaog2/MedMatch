using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using MedMatch.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using static MedMatch.Api.Mapping.DtoMapper;

namespace MedMatch.Api.Services;

/// <summary>
/// Inserts 50 fictional patients for the Morbus Perthes / LWS / hip TEP surgery case study.
/// Enabled only when SEED_CASE_DATA is "true". Idempotent: skips when case users already exist.
/// </summary>
public static class CaseStudySeeder
{
    private static readonly string[] CaseDiagnoses = ["Morbus Perthes", "Hip TEP Surgery", "LWS", "Lower Back Pain", "Pain"];

    private static readonly string[][] DiagnosisPatterns =
    [
        ["Morbus Perthes", "Hip TEP Surgery", "LWS", "Lower Back Pain"],
        ["Morbus Perthes", "LWS", "Lower Back Pain", "Pain"],
        ["Morbus Perthes", "Hip TEP Surgery"],
        ["Morbus Perthes"],
        ["LWS", "Lower Back Pain", "Pain"],
        ["Morbus Perthes", "Lower Back Pain"],
        ["Hip TEP Surgery", "LWS"],
        ["Morbus Perthes", "LWS"],
        ["Lower Back Pain", "Pain"],
        ["Morbus Perthes", "Hip TEP Surgery", "Pain"]
    ];

    private static readonly string[] CaseSymptoms =
    [
        "hip pain", "limping", "groin pain", "lower back pain", "joint stiffness",
        "reduced range of motion", "muscle weakness", "leg length difference", "thigh pain", "radiating pain"
    ];

    private static readonly (string City, string Country)[] Locations =
    [
        ("Berlin", "Germany"), ("Munich", "Germany"), ("Hamburg", "Germany"), ("Cologne", "Germany"),
        ("Frankfurt", "Germany"), ("Stuttgart", "Germany"), ("Leipzig", "Germany"), ("Dresden", "Germany"),
        ("Vienna", "Austria"), ("Graz", "Austria"), ("Zurich", "Switzerland"), ("Geneva", "Switzerland")
    ];

    private static readonly string[] Pseudonyms = ["PerthesPeer", "HipHero", "LWSBuddy", "PainPal", "RecoveryRoad", "JointJourney", "StepByStep", "WalkAgain", "TherapyFriend", "StrongerEveryDay"];

    public static bool IsEnabled(IConfiguration configuration) =>
        string.Equals(configuration["SEED_CASE_DATA"], "true", StringComparison.OrdinalIgnoreCase);

    public static async Task SeedAsync(MedMatchDbContext db, PasswordHasher passwords, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(x => x.Email.StartsWith("case-") && x.Email.EndsWith("@medmatch.test"), ct))
            return;

        var existingTags = await db.DiagnosisTags.ToListAsync(ct);
        var tagByNormalized = existingTags.ToDictionary(x => NormalizeTag(x.Name), StringComparer.OrdinalIgnoreCase);

        foreach (var diagnosisName in CaseDiagnoses)
        {
            if (tagByNormalized.ContainsKey(NormalizeTag(diagnosisName))) continue;
            var tag = new DiagnosisTag { Name = diagnosisName, Slug = ToSlug(diagnosisName), UsageCount = 0 };
            db.DiagnosisTags.Add(tag);
            tagByNormalized[NormalizeTag(diagnosisName)] = tag;
        }
        await db.SaveChangesAsync(ct);

        var random = new Random(20260911);
        var passwordHash = passwords.Hash("SamplePass123!");

        var users = new List<User>(50);
        for (var i = 1; i <= 50; i++)
        {
            var (city, country) = Locations[random.Next(Locations.Length)];
            var displayMode = random.Next(100) < 70 ? DisplayMode.Pseudonym : DisplayMode.RealName;
            var name = displayMode == DisplayMode.Pseudonym
                ? $"{Pseudonyms[random.Next(Pseudonyms.Length)]}{random.Next(10, 99)}"
                : $"Case Patient {i}";

            var selectedDiagnoses = DiagnosisPatterns[(i - 1) % DiagnosisPatterns.Length];
            var selectedSymptoms = CaseSymptoms.OrderBy(_ => random.Next()).Take(random.Next(2, 5)).ToArray();

            var profile = new PatientProfile
            {
                DisplayMode = displayMode,
                Pseudonym = displayMode == DisplayMode.Pseudonym ? name : null,
                RealName = displayMode == DisplayMode.RealName ? name : null,
                City = city,
                Country = country,
                Diagnoses = selectedDiagnoses,
                Symptoms = selectedSymptoms,
                Languages = random.Next(100) < 80 ? ["de"] : ["de", "en"],
                Bio = random.Next(100) < 50
                    ? "Living with Perthes and sharing my hip replacement journey."
                    : "Managing lumbar spine issues and chronic pain — happy to exchange experiences."
            };

            foreach (var diagnosisName in selectedDiagnoses)
            {
                if (tagByNormalized.TryGetValue(NormalizeTag(diagnosisName), out var tag))
                    profile.DiagnosisTags.Add(new PatientDiagnosisTag { DiagnosisTagId = tag.Id });
            }

            users.Add(new User
            {
                Email = $"case-{i:000}@medmatch.test",
                PasswordHash = passwordHash,
                Roles = [new UserRoleAssignment { Role = UserRole.Patient }],
                EmailConfirmed = true,
                IsActive = true,
                PatientProfile = profile,
                ConsentSettings = new ConsentSettings
                {
                    ShowProfilePublicly = true,
                    PatientsContactMe = true,
                    ClinicsContactMe = false,
                    DataForSearch = true,
                    Version = 1
                }
            });
        }

        db.Users.AddRange(users);
        await db.SaveChangesAsync(ct);
        await DiagnosisMatching.RefreshTagUsageCountsAsync(db, ct);
    }
}
