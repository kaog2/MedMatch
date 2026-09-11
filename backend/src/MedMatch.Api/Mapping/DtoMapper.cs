using MedMatch.Application.Contracts;
using MedMatch.Domain;

namespace MedMatch.Api.Mapping;

public static class DtoMapper
{
    public static PatientProfileDto ToProfileDto(PatientProfile profile) => new(profile.DisplayMode, profile.Pseudonym, profile.RealName, profile.City, profile.Country, profile.Diagnoses, profile.Interventions, profile.Symptoms, profile.AgeRange, profile.Bio, profile.Languages);

    public static void ApplyProfile(PatientProfile profile, PatientProfileDto dto)
    {
        profile.DisplayMode = dto.DisplayMode; profile.Pseudonym = dto.Pseudonym; profile.RealName = dto.RealName; profile.City = dto.City; profile.Country = dto.Country;
        profile.Diagnoses = (dto.Diagnoses ?? []).Select(CleanTagDisplay).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(); profile.Interventions = dto.Interventions ?? []; profile.Symptoms = dto.Symptoms ?? []; profile.AgeRange = dto.AgeRange; profile.Bio = dto.Bio; profile.Languages = dto.Languages ?? [];
    }

    public static string CleanTagDisplay(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return string.Empty;
        if (trimmed.Any(char.IsUpper)) return trimmed;
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmed);
    }

    public static string NormalizeTag(string value) => value.Trim().ToLowerInvariant();

    public static string ToSlug(string value)
    {
        var normalized = NormalizeTag(value);
        return string.Concat(normalized.Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
    }

    public static DiagnosisTagDto ToDiagnosisTagDto(DiagnosisTag tag) => new(tag.Id, tag.Name, tag.UsageCount);

    public static string DisplayName(PatientProfile profile) =>
        profile.DisplayMode == DisplayMode.Pseudonym && !string.IsNullOrWhiteSpace(profile.Pseudonym) ? profile.Pseudonym! :
        profile.DisplayMode == DisplayMode.RealName && !string.IsNullOrWhiteSpace(profile.RealName) ? profile.RealName! :
        "MedMatch member";

    public static ConsentSettingsDto ToConsentDto(ConsentSettings settings) => new(settings.ShowProfilePublicly, settings.ClinicsContactMe, settings.PatientsContactMe, settings.DataForSearch, settings.Version, settings.UpdatedAt);
    public static ClinicDto ToClinicDto(Clinic clinic) => new(clinic.Id, clinic.Name, clinic.Type, clinic.Specialty, clinic.TreatmentsOffered, clinic.Address, clinic.City, clinic.Country, clinic.ContactInfo, clinic.PublicWebsiteUrl, clinic.PublicationConsentGranted, clinic.IsVerified);
    public static void ApplyClinic(Clinic clinic, ClinicDto dto) { clinic.Name = dto.Name; clinic.Type = dto.Type; clinic.Specialty = dto.Specialty; clinic.TreatmentsOffered = dto.TreatmentsOffered ?? []; clinic.Address = dto.Address; clinic.City = dto.City; clinic.Country = dto.Country; clinic.ContactInfo = dto.ContactInfo; clinic.PublicWebsiteUrl = dto.PublicWebsiteUrl; clinic.PublicationConsentGranted = dto.PublicationConsentGranted; clinic.PublicationConsentAt = dto.PublicationConsentGranted ? clinic.PublicationConsentAt ?? DateTimeOffset.UtcNow : null; clinic.IsVerified = dto.IsVerified; }
    public static DoctorDto ToDoctorDto(Doctor doctor) => new(doctor.Id, doctor.ClinicId, doctor.Name, doctor.Specialty, doctor.TreatmentsOffered, doctor.City, doctor.Country, doctor.ContactInfo, doctor.IsVerified);
    public static void ApplyDoctor(Doctor doctor, DoctorDto dto) { doctor.ClinicId = dto.ClinicId; doctor.Name = dto.Name; doctor.Specialty = dto.Specialty; doctor.TreatmentsOffered = dto.TreatmentsOffered ?? []; doctor.City = dto.City; doctor.Country = dto.Country; doctor.ContactInfo = dto.ContactInfo; doctor.IsVerified = dto.IsVerified; }
    public static void ApplyReview(Review review, UpsertReviewRequest dto) { review.ClinicId = dto.ClinicId; review.DoctorId = dto.DoctorId; review.Rating = dto.Rating; review.Title = dto.Title; review.Body = dto.Body; review.Tags = dto.Tags ?? []; review.IsAnonymous = dto.IsAnonymous; review.AllowContactByPatients = dto.AllowContactByPatients; review.AllowContactByClinics = dto.AllowContactByClinics; review.UpdatedAt = DateTimeOffset.UtcNow; }

    public static ReviewDto ToReviewDto(Review review)
    {
        var profile = review.AuthorUser.PatientProfile;
        var masked = review.IsAnonymous || profile is null || profile.DisplayMode == DisplayMode.Anonymous;
        var name = masked ? "Anonymous" : profile!.DisplayMode == DisplayMode.Pseudonym && !string.IsNullOrWhiteSpace(profile.Pseudonym) ? profile.Pseudonym : !string.IsNullOrWhiteSpace(profile!.RealName) ? profile.RealName : "Patient";
        return new(review.Id, review.ClinicId, review.DoctorId, review.Rating, review.Title, review.Body, review.Tags, masked, name, masked ? null : review.AuthorUserId, review.CreatedAt, review.UpdatedAt);
    }

    public static RecommendationDto ToRecommendationDto(Recommendation recommendation)
    {
        var profile = recommendation.AuthorUser.PatientProfile;
        var authorName = profile is null ? "MedMatch member" : DisplayName(profile);
        var clinics = recommendation.Clinics
            .Select(x => new RecommendationClinicDto(x.Clinic.Id, x.Clinic.Name, x.Clinic.Type, x.Clinic.City, x.Clinic.Country))
            .OrderBy(x => x.Name)
            .ToArray();
        var diagnoses = recommendation.DiagnosisTags
            .Select(x => x.DiagnosisTag.Name)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new(recommendation.Id, recommendation.AuthorUserId, authorName, clinics, diagnoses, recommendation.Details, recommendation.Status, recommendation.ModerationNote, recommendation.CreatedAt);
    }
}
