namespace MedMatch.Application.Contracts;

public interface IPatientProfileService
{
    Task<PatientProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<PatientProfileDto> UpsertAsync(Guid userId, PatientProfileDto profile, CancellationToken cancellationToken);
}
