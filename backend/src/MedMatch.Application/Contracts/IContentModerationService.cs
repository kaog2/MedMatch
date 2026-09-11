namespace MedMatch.Application.Contracts;

/// <summary>
/// Verifies that patient-generated recommendation text is positive and
/// appropriate before it can be published. Implementations may call an LLM
/// via API or apply simple heuristics; callers treat a failure as "not yet
/// approved" so unverified content is never made public.
/// </summary>
public interface IContentModerationService
{
    /// <summary>True when an external LLM moderation endpoint is configured.</summary>
    bool IsEnabled { get; }

    Task<ModerationResult> ModerateAsync(string details, IReadOnlyList<string> diagnoses, IReadOnlyList<string> providerNames, CancellationToken cancellationToken);
}
