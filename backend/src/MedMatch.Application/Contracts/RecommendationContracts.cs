using MedMatch.Domain;

namespace MedMatch.Application.Contracts;

public sealed record CreateRecommendationRequest(Guid[] ClinicIds, string[] Diagnoses, string Details);

public sealed record RecommendationClinicDto(Guid Id, string Name, CareProviderType Type, string City, string Country);

public sealed record RecommendationDto(
    Guid Id,
    Guid AuthorUserId,
    string AuthorDisplayName,
    RecommendationClinicDto[] Clinics,
    string[] Diagnoses,
    string Details,
    RecommendationStatus Status,
    string? ModerationNote,
    DateTimeOffset CreatedAt
);

public sealed record ModerateRecommendationRequest(bool Approve, string? Note);

public sealed record ModerationResult(bool Approved, string? Reason);
