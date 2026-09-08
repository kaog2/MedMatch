using MedMatch.Domain;

namespace MedMatch.Application.Contracts;

public sealed record PatientProfileDto(DisplayMode DisplayMode, string? Pseudonym, string? RealName, string? City, string? Country, string[] Diagnoses, string[] Interventions, string[] Symptoms, string? AgeRange, string? Bio, string[] Languages);
public sealed record ConsentSettingsDto(bool ShowProfilePublicly, bool ClinicsContactMe, bool PatientsContactMe, bool DataForSearch, int Version, DateTimeOffset UpdatedAt);
