namespace MedMatch.Application.Contracts;

public interface IClinicSearchService
{
    Task<IReadOnlyList<ClinicDto>> SearchAsync(string? specialty, string? city, string? tag, CancellationToken cancellationToken);
}
