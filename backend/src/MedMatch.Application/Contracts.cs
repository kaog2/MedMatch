using MedMatch.Domain;

namespace MedMatch.Application;

public record RegisterRequest(string Email, string Password, UserRole Role = UserRole.Patient);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, UserRole Role);
public record PatientProfileDto(DisplayMode DisplayMode, string? Pseudonym, string? RealName, string? City, string? Country, string[] Diagnoses, string[] Interventions, string[] Symptoms, string? AgeRange, string? Bio, string[] Languages);
public record ConsentSettingsDto(bool ShowProfilePublicly, bool ClinicsContactMe, bool PatientsContactMe, bool DataForSearch, int Version, DateTimeOffset UpdatedAt);
public record PatientDirectoryDto(Guid UserId, string DisplayName, string? City, string? Country, string[] Diagnoses, string[] Interventions, string[] Symptoms, string? Bio, string[] Languages);
public record ConnectionRequest(string Message);
public record ClinicDto(Guid Id, string Name, string Type, string Specialty, string[] TreatmentsOffered, string? Address, string City, string Country, string? ContactInfo, bool IsVerified);
public record DoctorDto(Guid Id, Guid? ClinicId, string Name, string Specialty, string[] TreatmentsOffered, string? City, string? Country, string? ContactInfo, bool IsVerified);
public record ReviewDto(Guid Id, Guid? ClinicId, Guid? DoctorId, int Rating, string Title, string Body, string[] Tags, bool IsAnonymous, string AuthorDisplayName, Guid? AuthorUserId, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public record UpsertReviewRequest(Guid? ClinicId, Guid? DoctorId, int Rating, string Title, string Body, string[] Tags, bool IsAnonymous, bool AllowContactByPatients, bool AllowContactByClinics);

public interface IAuthService { Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct); Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct); Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken ct); }
public interface IPatientProfileService { Task<PatientProfileDto?> GetAsync(Guid userId, CancellationToken ct); Task<PatientProfileDto> UpsertAsync(Guid userId, PatientProfileDto profile, CancellationToken ct); }
public interface IClinicSearchService { Task<IReadOnlyList<ClinicDto>> SearchAsync(string? specialty, string? city, string? tag, CancellationToken ct); }
public interface IReviewService { Task<IReadOnlyList<ReviewDto>> GetAsync(Guid? clinicId, Guid? doctorId, CancellationToken ct); Task<ReviewDto> CreateAsync(Guid authorUserId, UpsertReviewRequest request, CancellationToken ct); }
public interface IConsentService { Task<ConsentSettingsDto> UpdateAsync(Guid userId, ConsentSettingsDto settings, string? ip, string? userAgent, CancellationToken ct); }
