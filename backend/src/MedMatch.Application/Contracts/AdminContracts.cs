namespace MedMatch.Application.Contracts;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string[] Roles,
    string? DisplayName,
    string? City,
    string? Country,
    string[] Diagnoses,
    string Symptoms,
    bool EmailConfirmed,
    bool IsActive,
    bool PatientsContactMe,
    bool DataForSearch,
    DateTimeOffset CreatedAt
);

public sealed record UpdateActiveRequest(bool IsActive);

public sealed record UpdateRoleRequest(string Role, bool Enabled);
