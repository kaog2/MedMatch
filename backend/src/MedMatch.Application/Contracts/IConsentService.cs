namespace MedMatch.Application.Contracts;

public interface IConsentService
{
    Task<ConsentSettingsDto> UpdateAsync(Guid userId, ConsentSettingsDto settings, string? ip, string? userAgent, CancellationToken cancellationToken);
}
