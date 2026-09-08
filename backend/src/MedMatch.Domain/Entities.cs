namespace MedMatch.Domain;

public enum UserRole { Patient, Clinic, Doctor, Admin }
public enum DisplayMode { Anonymous, Pseudonym, RealName }

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Patient;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLogin { get; set; }
    public PatientProfile? PatientProfile { get; set; }
    public ConsentSettings? ConsentSettings { get; set; }
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
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
    public string[] Symptoms { get; set; } = [];
    public string? AgeRange { get; set; }
    public string? Bio { get; set; }
    public string[] Languages { get; set; } = [];
}

public sealed class Clinic
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Clinic";
    public string Specialty { get; set; } = string.Empty;
    public string[] TreatmentsOffered { get; set; } = [];
    public string? Address { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ContactInfo { get; set; }
    public bool IsVerified { get; set; }
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
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
