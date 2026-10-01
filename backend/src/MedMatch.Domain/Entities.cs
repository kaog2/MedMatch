namespace MedMatch.Domain;

public enum UserRole { Patient, Clinic, Doctor, Admin }
public enum DisplayMode { Anonymous, Pseudonym, RealName }
public enum CareProviderType { Clinic, MedicalPractice, Doctor, Therapist, Hospital, Other }
public enum RecommendationStatus { Pending, Approved, Rejected }
public enum SymptomCategory { Pain, Fatigue, Neurological, Musculoskeletal, Digestive, MentalMood, Respiratory, Sleep, Other }

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public ICollection<UserRoleAssignment> Roles { get; set; } = new List<UserRoleAssignment>();
    public IReadOnlyList<UserRole> RoleList => Roles.Select(r => r.Role).Distinct().ToList();
    public bool HasRole(UserRole role) => Roles.Any(r => r.Role == role);
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLogin { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; } = true;
    public PatientProfile? PatientProfile { get; set; }
    public ConsentSettings? ConsentSettings { get; set; }
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
    public ICollection<SymptomDiarySheet> SymptomDiarySheets { get; set; } = new List<SymptomDiarySheet>();
    public ICollection<SymptomDiaryEntry> SymptomDiaryEntries { get; set; } = new List<SymptomDiaryEntry>();
    public ICollection<UserBotApiKey> BotApiKeys { get; set; } = new List<UserBotApiKey>();
}

public sealed class EmailVerificationToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PatientProfile
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DisplayMode DisplayMode { get; set; } = DisplayMode.Anonymous;
    public string? Pseudonym { get; set; }
    public string? RealName { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string[] Diagnoses { get; set; } = [];
    public string[] Interventions { get; set; } = [];
    public string Symptoms { get; set; } = string.Empty;
    public string? AgeRange { get; set; }
    public string? Bio { get; set; }
    public string[] Languages { get; set; } = [];
    public ICollection<PatientDiagnosisTag> DiagnosisTags { get; set; } = new List<PatientDiagnosisTag>();
}

public sealed class DiagnosisTag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<PatientDiagnosisTag> Patients { get; set; } = new List<PatientDiagnosisTag>();
    public ICollection<RecommendationDiagnosisTag> Recommendations { get; set; } = new List<RecommendationDiagnosisTag>();
    public ICollection<DiagnosisTagTranslation> Translations { get; set; } = new List<DiagnosisTagTranslation>();
}

public sealed class DiagnosisTagTranslation
{
    public Guid DiagnosisTagId { get; set; }
    public DiagnosisTag DiagnosisTag { get; set; } = null!;
    public string Culture { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class TranslationCache
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SourceHash { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public string SourceText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PatientDiagnosisTag
{
    public Guid UserId { get; set; }
    public PatientProfile Patient { get; set; } = null!;
    public Guid DiagnosisTagId { get; set; }
    public DiagnosisTag DiagnosisTag { get; set; } = null!;
}

public sealed class MatchNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid MatchedUserId { get; set; }
    public string[] SharedDiagnoses { get; set; } = [];
    public int Score { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Clinic
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public CareProviderType Type { get; set; } = CareProviderType.Clinic;
    public string Specialty { get; set; } = string.Empty;
    public string[] TreatmentsOffered { get; set; } = [];
    public string? Address { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ContactInfo { get; set; }
    public string? PublicWebsiteUrl { get; set; }
    public bool PublicationConsentGranted { get; set; }
    public DateTimeOffset? PublicationConsentAt { get; set; }
    public bool IsVerified { get; set; }
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<RecommendationClinic> Recommendations { get; set; } = new List<RecommendationClinic>();
}

public sealed class Doctor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ClinicId { get; set; }
    public Clinic? Clinic { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string[] TreatmentsOffered { get; set; } = [];
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ContactInfo { get; set; }
    public bool IsVerified { get; set; }
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public sealed class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuthorUserId { get; set; }
    public User AuthorUser { get; set; } = null!;
    public Guid? ClinicId { get; set; }
    public Clinic? Clinic { get; set; }
    public Guid? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
    public bool IsAnonymous { get; set; } = true;
    public bool AllowContactByPatients { get; set; }
    public bool AllowContactByClinics { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ConsentSettings
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public bool ShowProfilePublicly { get; set; }
    public bool ClinicsContactMe { get; set; }
    public bool PatientsContactMe { get; set; }
    public bool DataForSearch { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
}

public sealed class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public Guid ThreadId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ConsentSnapshot { get; set; } = string.Empty;
}

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

/// <summary>
/// A patient's positive endorsement of one or more care providers that helped
/// with a specific diagnosis or symptom. Only Approved recommendations are
/// publicly visible; new ones start Pending until moderation (human or LLM).
/// </summary>
public sealed class Recommendation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuthorUserId { get; set; }
    public User AuthorUser { get; set; } = null!;
    public string Details { get; set; } = string.Empty;
    public RecommendationStatus Status { get; set; } = RecommendationStatus.Pending;
    public string? ModerationNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public ICollection<RecommendationClinic> Clinics { get; set; } = new List<RecommendationClinic>();
    public ICollection<RecommendationDiagnosisTag> DiagnosisTags { get; set; } = new List<RecommendationDiagnosisTag>();
}

public sealed class RecommendationClinic
{
    public Guid RecommendationId { get; set; }
    public Recommendation Recommendation { get; set; } = null!;
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;
}

public sealed class RecommendationDiagnosisTag
{
    public Guid RecommendationId { get; set; }
    public Recommendation Recommendation { get; set; } = null!;
    public Guid DiagnosisTagId { get; set; }
    public DiagnosisTag DiagnosisTag { get; set; } = null!;
}

public sealed class UserRoleAssignment
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public UserRole Role { get; set; }
}

/// <summary>
/// A patient's daily symptom diary sheet representing one full day of tracking.
/// </summary>
public sealed class SymptomDiarySheet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public int? OverallWellbeing { get; set; } // 1 (Poor) to 5 (Excellent)
    public int? SleepQuality { get; set; } // 1 (Poor) to 5 (Restful)
    public decimal? SleepHours { get; set; } // e.g. 7.5
    public string? DailyNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<SymptomDiaryEntry> Entries { get; set; } = new List<SymptomDiaryEntry>();
}

/// <summary>
/// A specific pain or symptom log entry recorded on a daily sheet.
/// Open for many pain qualities and all non-pain symptoms.
/// </summary>
public sealed class SymptomDiaryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SheetId { get; set; }
    public SymptomDiarySheet Sheet { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public SymptomCategory Category { get; set; } = SymptomCategory.Pain;
    public string SymptomName { get; set; } = string.Empty;
    public string? PainType { get; set; } // e.g. "Sharp, Throbbing"
    public string? BodyLocation { get; set; } // e.g. "Lower Back (LWS)"
    public int Severity { get; set; } // 0 (None) to 10 (Worst possible)
    public int? DurationMinutes { get; set; }
    public string? Triggers { get; set; }
    public string? Relievers { get; set; }
    public string? MedicationsTaken { get; set; }
    public string? Notes { get; set; }
    public string Source { get; set; } = "Web"; // "Web", "ChatBot", "n8n"
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// An API key for automated chatbot / n8n logging on behalf of a patient.
/// </summary>
public sealed class UserBotApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string Label { get; set; } = "n8n Bot";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAt { get; set; }
}
