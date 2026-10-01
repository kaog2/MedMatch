using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MedMatch.Api.Services;

/// <summary>
/// Inserts 100 clearly labeled fictional care-provider directory entries for local testing.
/// Enabled only when SEED_SAMPLE_PROVIDERS is "true". Missing entries are added on rerun.
/// </summary>
public static class CareProviderSampleDataSeeder
{
    private const string NamePrefix = "DEMO ONLY - MedMatch Sample Provider ";

    private static readonly (string City, string Country)[] Locations =
    [
        ("Berlin", "Germany"), ("Munich", "Germany"), ("Hamburg", "Germany"), ("Cologne", "Germany"),
        ("Madrid", "Spain"), ("Barcelona", "Spain"), ("Valencia", "Spain"), ("Seville", "Spain"),
        ("Paris", "France"), ("Lyon", "France"), ("Rome", "Italy"), ("Milan", "Italy"),
        ("Vienna", "Austria"), ("Graz", "Austria"), ("Zurich", "Switzerland"), ("Basel", "Switzerland"),
        ("London", "United Kingdom"), ("Manchester", "United Kingdom"), ("Amsterdam", "Netherlands"),
        ("Rotterdam", "Netherlands"), ("Brussels", "Belgium"), ("Antwerp", "Belgium"),
        ("Lisbon", "Portugal"), ("Porto", "Portugal"), ("Dublin", "Ireland"), ("Cork", "Ireland"),
        ("Stockholm", "Sweden"), ("Gothenburg", "Sweden"), ("Copenhagen", "Denmark"),
        ("Oslo", "Norway"), ("Helsinki", "Finland"), ("Prague", "Czechia")
    ];

    private static readonly (string Specialty, string[] Treatments)[] Services =
    [
        ("Orthopedics", ["Joint assessment", "Mobility care", "Rehabilitation planning"]),
        ("Physiotherapy", ["Movement therapy", "Back rehabilitation", "Strength exercises"]),
        ("Rheumatology", ["Joint evaluation", "Inflammatory disease care", "Medication review"]),
        ("Neurology", ["Neurological assessment", "Headache care", "Movement evaluation"]),
        ("Cardiology", ["Heart health assessment", "Cardiac rehabilitation", "Blood pressure review"]),
        ("Dermatology", ["Skin assessment", "Eczema care", "Psoriasis care"]),
        ("Endocrinology", ["Diabetes care", "Thyroid assessment", "Hormone health review"]),
        ("Gastroenterology", ["Digestive health assessment", "IBS care", "Inflammatory bowel support"]),
        ("Pulmonology", ["Breathing assessment", "Asthma care", "Respiratory rehabilitation"]),
        ("Psychiatry", ["Mental health assessment", "Medication consultation", "Ongoing care planning"]),
        ("Psychology", ["Emotional wellbeing support", "Coping strategies", "Wellbeing consultation"]),
        ("Pain Medicine", ["Pain assessment", "Pain management planning", "Multidisciplinary referral"]),
        ("Rehabilitation Medicine", ["Functional assessment", "Recovery planning", "Mobility support"]),
        ("Gynecology", ["Women's health consultation", "Pelvic health assessment", "Preventive care"]),
        ("Oncology", ["Cancer care consultation", "Treatment support", "Survivorship planning"]),
        ("Nephrology", ["Kidney health assessment", "Chronic kidney care", "Medication review"]),
        ("Ophthalmology", ["Vision assessment", "Eye health consultation", "Glaucoma monitoring"]),
        ("Sleep Medicine", ["Sleep assessment", "Sleep apnea consultation", "Sleep health planning"]),
        ("Occupational Therapy", ["Daily living assessment", "Adaptive strategies", "Functional rehabilitation"]),
        ("General Practice", ["Primary care consultation", "Preventive health review", "Care coordination"])
    ];

    private static readonly CareProviderType[] ProviderTypes =
    [
        CareProviderType.Clinic,
        CareProviderType.MedicalPractice,
        CareProviderType.Doctor,
        CareProviderType.Therapist,
        CareProviderType.Hospital,
        CareProviderType.Other
    ];

    public static bool IsEnabled(IConfiguration configuration) =>
        string.Equals(configuration["SEED_SAMPLE_PROVIDERS"], "true", StringComparison.OrdinalIgnoreCase);

    public static async Task SeedAsync(MedMatchDbContext db, CancellationToken ct)
    {
        var existingNames = await db.Clinics
            .Where(x => x.Name.StartsWith(NamePrefix))
            .Select(x => x.Name)
            .ToHashSetAsync(StringComparer.Ordinal, ct);

        var samples = new List<Clinic>(100);
        for (var index = 0; index < 100; index++)
        {
            var (specialty, treatments) = Services[index % Services.Length];
            var name = $"{NamePrefix}{index + 1:000} - {specialty}";
            if (!existingNames.Add(name)) continue;

            var (city, country) = Locations[(index * 7) % Locations.Length];
            samples.Add(new Clinic
            {
                Name = name,
                Type = ProviderTypes[index % ProviderTypes.Length],
                Specialty = specialty,
                TreatmentsOffered = treatments,
                City = city,
                Country = country,
                PublicationConsentGranted = true,
                PublicationConsentAt = DateTimeOffset.UtcNow,
                IsVerified = false
            });
        }

        if (samples.Count == 0) return;

        db.Clinics.AddRange(samples);
        await db.SaveChangesAsync(ct);
    }
}