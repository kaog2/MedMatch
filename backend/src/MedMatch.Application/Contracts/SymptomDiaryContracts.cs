namespace MedMatch.Application.Contracts;

public record SymptomDiaryEntryDto(
    Guid Id,
    Guid SheetId,
    DateOnly Date,
    DateTimeOffset RecordedAt,
    string Category,
    string SymptomName,
    string? PainType,
    string? BodyLocation,
    int Severity,
    int? DurationMinutes,
    string? Triggers,
    string? Relievers,
    string? MedicationsTaken,
    string? Notes,
    string Source,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record SymptomDiarySheetDto(
    Guid Id,
    DateOnly Date,
    int? OverallWellbeing,
    int? SleepQuality,
    decimal? SleepHours,
    string? DailyNotes,
    int EntryCount,
    double? AverageSeverity,
    int? MaxSeverity,
    IReadOnlyList<SymptomDiaryEntryDto> Entries,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record SymptomDiarySheetSummaryDto(
    Guid Id,
    DateOnly Date,
    int? OverallWellbeing,
    int? SleepQuality,
    decimal? SleepHours,
    int EntryCount,
    double? AverageSeverity,
    int? MaxSeverity,
    string[] MainSymptoms
);

public record UpsertSymptomEntryRequest(
    DateOnly? Date,
    DateTimeOffset? RecordedAt,
    string? Category,
    string SymptomName,
    string? PainType,
    string? BodyLocation,
    int Severity,
    int? DurationMinutes,
    string? Triggers,
    string? Relievers,
    string? MedicationsTaken,
    string? Notes,
    string? Source
);

public record UpdateDailySheetRequest(
    int? OverallWellbeing,
    int? SleepQuality,
    decimal? SleepHours,
    string? DailyNotes
);

public record BotLogSymptomRequest(
    DateOnly? Date,
    DateTimeOffset? RecordedAt,
    string? Category,
    string SymptomName,
    string? PainType,
    string? BodyLocation,
    int Severity,
    int? DurationMinutes,
    string? Triggers,
    string? Relievers,
    string? MedicationsTaken,
    string? Notes,
    string? Source
);

public record BotUpdateSymptomEntryRequest(
    DateOnly? Date,
    DateTimeOffset? RecordedAt,
    string? Category,
    string? SymptomName,
    string? PainType,
    string? BodyLocation,
    int? Severity,
    int? DurationMinutes,
    string? Triggers,
    string? Relievers,
    string? MedicationsTaken,
    string? Notes
);

public record BotQuickLogTextRequest(
    string Text,
    DateOnly? Date
);

public record BotApiKeyDto(
    Guid Id,
    string KeyPrefix,
    string Label,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt
);

public record CreateBotKeyRequest(
    string? Label
);

public record CreateBotKeyResponse(
    Guid Id,
    string ApiKey,
    string KeyPrefix,
    string Label,
    DateTimeOffset CreatedAt
);

public record SymptomDiaryAnalyticsDto(
    int TotalEntries,
    int DaysTracked,
    double OverallAverageSeverity,
    IReadOnlyList<DailySeverityPointDto> SeverityTrend,
    IReadOnlyList<SymptomFrequencyDto> TopSymptoms,
    IReadOnlyList<LocationFrequencyDto> TopLocations,
    IReadOnlyList<PainTypeFrequencyDto> TopPainTypes
);

public record DailySeverityPointDto(DateOnly Date, double AverageSeverity, int MaxSeverity, int EntryCount);
public record SymptomFrequencyDto(string Name, string Category, int Count, double AverageSeverity);
public record LocationFrequencyDto(string Location, int Count);
public record PainTypeFrequencyDto(string PainType, int Count);

