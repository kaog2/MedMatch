namespace MedMatch.Application.Contracts;

public sealed record DiagnosisTagDto(Guid Id, string Name, string? LocalizedName, int UsageCount);

public sealed record MatchDto(
    Guid UserId,
    string DisplayName,
    string? City,
    string? Country,
    string[] SharedDiagnoses,
    string[] SharedSymptoms,
    bool SameLocation,
    int MatchPercentage,
    string? Bio,
    string[] Languages
);

public sealed record MatchNotificationDto(
    Guid Id,
    Guid MatchedUserId,
    string DisplayName,
    string[] SharedDiagnoses,
    int Score,
    bool IsRead,
    DateTimeOffset CreatedAt
);

public sealed record MatchSummaryDto(int UnreadCount);