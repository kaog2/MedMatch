using MedMatch.Domain;

namespace MedMatch.Application.Contracts;

public sealed record ClinicDto(Guid Id, string Name, CareProviderType Type, string Specialty, string[] TreatmentsOffered, string? Address, string City, string Country, string? ContactInfo, string? PublicWebsiteUrl, bool PublicationConsentGranted, bool IsVerified);
public sealed record DoctorDto(Guid Id, Guid? ClinicId, string Name, string Specialty, string[] TreatmentsOffered, string? City, string? Country, string? ContactInfo, bool IsVerified);
