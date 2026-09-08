namespace MedMatch.Application.Contracts;

public sealed record PatientDirectoryDto(Guid UserId, string DisplayName, string? City, string? Country, string[] Diagnoses, string[] Interventions, string[] Symptoms, string? Bio, string[] Languages);
public sealed record ConnectionRequest(string Message);
