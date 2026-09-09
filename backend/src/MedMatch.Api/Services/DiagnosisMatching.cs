using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static MedMatch.Api.Mapping.DtoMapper;

namespace MedMatch.Api.Services;

public static class DiagnosisMatching
{
    public static readonly string[] SeedDiagnoses =
    [
        "Type 1 Diabetes", "Type 2 Diabetes", "Gestational Diabetes", "Asthma", "COPD", "Hypertension",
        "Coronary Artery Disease", "Heart Failure", "Atrial Fibrillation", "Stroke", "Migraine", "Epilepsy",
        "Multiple Sclerosis", "Parkinson's Disease", "Alzheimer's Disease", "Depression", "Anxiety Disorder",
        "Bipolar Disorder", "Schizophrenia", "PTSD", "ADHD", "Autism Spectrum Disorder", "Rheumatoid Arthritis",
        "Osteoarthritis", "Lupus", "Fibromyalgia", "Chronic Fatigue Syndrome", "Crohn's Disease", "Ulcerative Colitis",
        "Celiac Disease", "Irritable Bowel Syndrome", "Hypothyroidism", "Hyperthyroidism", "Hashimoto's Disease",
        "Breast Cancer", "Lung Cancer", "Prostate Cancer", "Colorectal Cancer", "Leukemia", "Lymphoma",
        "Endometriosis", "PCOS", "Chronic Kidney Disease", "Kidney Stones", "Glaucoma", "Cataracts",
        "Psoriasis", "Eczema", "Sleep Apnea", "Chronic Pain", "Long COVID", "HIV", "Hepatitis C"
    ];

    public static async Task SeedDiagnosisTagsAsync(MedMatchDbContext db, CancellationToken ct)
    {
        if (await db.DiagnosisTags.AnyAsync(ct)) return;
        foreach (var name in SeedDiagnoses)
        {
            db.DiagnosisTags.Add(new DiagnosisTag { Name = name, Slug = ToSlug(name), UsageCount = 0 });
        }
        await db.SaveChangesAsync(ct);
    }

    public static async Task SyncDiagnosisTagsAsync(PatientProfile profile, string[] diagnoses, MedMatchDbContext db, CancellationToken ct)
    {
        var cleanTags = diagnoses.Select(CleanTagDisplay).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var existing = await db.DiagnosisTags.ToListAsync(ct);
        var byNormalized = existing.ToDictionary(x => NormalizeTag(x.Name), StringComparer.OrdinalIgnoreCase);

        var canonicalDiagnoses = new List<string>();
        var newTagIds = new List<Guid>();

        foreach (var inputTag in cleanTags)
        {
            var norm = NormalizeTag(inputTag);
            if (byNormalized.TryGetValue(norm, out var tag))
            {
                canonicalDiagnoses.Add(tag.Name);
                newTagIds.Add(tag.Id);
            }
            else
            {
                var slug = ToSlug(inputTag);
                if (string.IsNullOrEmpty(slug)) slug = "tag-" + Guid.NewGuid().ToString("n")[..8];
                var created = new DiagnosisTag { Name = inputTag, Slug = slug, UsageCount = 0 };
                db.DiagnosisTags.Add(created);
                newTagIds.Add(created.Id);
                byNormalized[norm] = created;
                canonicalDiagnoses.Add(created.Name);
            }
        }

        profile.Diagnoses = canonicalDiagnoses.ToArray();

        var currentIds = profile.DiagnosisTags.Select(x => x.DiagnosisTagId).ToHashSet();
        var desiredIds = newTagIds.ToHashSet();

        foreach (var link in profile.DiagnosisTags.Where(x => !desiredIds.Contains(x.DiagnosisTagId)).ToList())
            profile.DiagnosisTags.Remove(link);

        foreach (var id in desiredIds.Where(x => !currentIds.Contains(x)))
            profile.DiagnosisTags.Add(new PatientDiagnosisTag { UserId = profile.UserId, DiagnosisTagId = id });
    }

    public static async Task RefreshTagUsageCountsAsync(MedMatchDbContext db, CancellationToken ct)
    {
        var counts = await db.PatientDiagnosisTags
            .GroupBy(x => x.DiagnosisTagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TagId, x => x.Count, ct);

        var allTags = await db.DiagnosisTags.ToListAsync(ct);
        foreach (var tag in allTags)
        {
            tag.UsageCount = counts.TryGetValue(tag.Id, out var c) ? c : 0;
        }
        await db.SaveChangesAsync(ct);
    }

    public static (int Score, string[] SharedDiagnoses, string[] SharedSymptoms, bool SameLocation) EvaluateMatch(
        PatientProfile current,
        PatientProfile candidate)
    {
        var myTagIds = current.DiagnosisTags.Select(x => x.DiagnosisTagId).ToHashSet();
        var sharedTagNames = candidate.DiagnosisTags
            .Where(t => myTagIds.Contains(t.DiagnosisTagId))
            .Select(t => t.DiagnosisTag.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();

        if (sharedTagNames.Length == 0)
        {
            return (0, [], [], false);
        }

        int totalDiagnoses = current.DiagnosisTags.Count + candidate.DiagnosisTags.Count;
        double dice = totalDiagnoses > 0 ? (2.0 * sharedTagNames.Length) / totalDiagnoses : 0.0;
        double baseScore = 50.0 + (dice * 35.0);

        var mySymptoms = (current.Symptoms ?? []).Select(s => s.Trim().ToLowerInvariant()).Where(s => s.Length > 0).ToHashSet();
        var sharedSymptoms = (candidate.Symptoms ?? [])
            .Where(s => mySymptoms.Contains(s.Trim().ToLowerInvariant()))
            .Select(CleanTagDisplay)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        double symptomBonus = Math.Min(10.0, sharedSymptoms.Length * 4.0);

        double locationBonus = 0.0;
        bool sameLocation = false;
        if (!string.IsNullOrWhiteSpace(current.City) && !string.IsNullOrWhiteSpace(candidate.City) &&
            string.Equals(current.City.Trim(), candidate.City.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            locationBonus = 10.0;
            sameLocation = true;
        }
        else if (!string.IsNullOrWhiteSpace(current.Country) && !string.IsNullOrWhiteSpace(candidate.Country) &&
                 string.Equals(current.Country.Trim(), candidate.Country.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            locationBonus = 5.0;
        }

        int finalScore = (int)Math.Clamp(Math.Round(baseScore + symptomBonus + locationBonus), 10, 100);
        return (finalScore, sharedTagNames, sharedSymptoms, sameLocation);
    }

    public static async Task ComputeMatchesAsync(Guid userId, MedMatchDbContext db, CancellationToken ct)
    {
        var current = await db.PatientProfiles
            .Include(x => x.User).ThenInclude(x => x.ConsentSettings)
            .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
            .SingleOrDefaultAsync(x => x.UserId == userId, ct);

        if (current is null) return;

        bool hasConsent = current.User.ConsentSettings is not null &&
                          current.User.ConsentSettings.PatientsContactMe &&
                          current.User.ConsentSettings.DataForSearch;

        if (!hasConsent || current.DiagnosisTags.Count == 0)
        {
            var stale = await db.MatchNotifications.Where(x => x.UserId == userId || x.MatchedUserId == userId).ToListAsync(ct);
            if (stale.Count > 0)
            {
                db.MatchNotifications.RemoveRange(stale);
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        var candidates = await db.PatientProfiles
            .Include(x => x.User).ThenInclude(x => x.ConsentSettings)
            .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
            .Where(x => x.UserId != userId && x.User.ConsentSettings != null && x.User.ConsentSettings.PatientsContactMe && x.User.ConsentSettings.DataForSearch)
            .ToListAsync(ct);

        var existingNotifications = await db.MatchNotifications
            .Where(x => x.UserId == userId || x.MatchedUserId == userId)
            .ToListAsync(ct);

        foreach (var candidate in candidates)
        {
            var (score, sharedDiagnoses, _, _) = EvaluateMatch(current, candidate);
            var notifToMe = existingNotifications.FirstOrDefault(x => x.UserId == userId && x.MatchedUserId == candidate.UserId);
            var notifToCandidate = existingNotifications.FirstOrDefault(x => x.UserId == candidate.UserId && x.MatchedUserId == userId);

            if (score == 0)
            {
                if (notifToMe != null) db.MatchNotifications.Remove(notifToMe);
                if (notifToCandidate != null) db.MatchNotifications.Remove(notifToCandidate);
            }
            else
            {
                if (notifToMe == null)
                {
                    db.MatchNotifications.Add(new MatchNotification
                    {
                        UserId = userId,
                        MatchedUserId = candidate.UserId,
                        SharedDiagnoses = sharedDiagnoses,
                        Score = score,
                        IsRead = false
                    });
                }
                else
                {
                    notifToMe.SharedDiagnoses = sharedDiagnoses;
                    notifToMe.Score = score;
                }

                if (notifToCandidate == null)
                {
                    db.MatchNotifications.Add(new MatchNotification
                    {
                        UserId = candidate.UserId,
                        MatchedUserId = userId,
                        SharedDiagnoses = sharedDiagnoses,
                        Score = score,
                        IsRead = false
                    });
                }
                else
                {
                    notifToCandidate.SharedDiagnoses = sharedDiagnoses;
                    notifToCandidate.Score = score;
                }
            }
        }
        await db.SaveChangesAsync(ct);
    }
}