using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MedMatch.Application.Contracts;

namespace MedMatch.Api.Services;

/// <summary>
/// LLM-based moderation of patient recommendations via an OpenAI-compatible
/// chat-completions API. Configured entirely through environment variables so
/// it can be turned on later without code changes:
///   LLM_MODERATION_ENABLED  (default: false)
///   LLM_MODERATION_API_URL  e.g. https://api.openai.com/v1/chat/completions
///   LLM_MODERATION_API_KEY
///   LLM_MODERATION_MODEL    (default: gpt-4o-mini)
/// When disabled, the caller decides what to do (see Program.cs).
/// </summary>
public sealed class ContentModerationService(IConfiguration configuration, HttpClient httpClient) : IContentModerationService
{
    private const string SystemPrompt =
        "You are a content moderator for MedMatch, a platform where patients share positive experiences about care providers that helped them.\n" +
        "Decide whether the following recommendation may be published.\n" +
        "Rules:\n" +
        "- It must be positive, constructive and respectful.\n" +
        "- It must describe how one or more care providers helped with a diagnosis or symptom.\n" +
        "- It must NOT contain insults, harassment, hate, spam, promotion, medical advice, or negative claims.\n" +
        "Reply with exactly one line: \"APPROVE\" or \"REJECT: <short reason>\", in any language.";

    public bool IsEnabled => string.Equals(configuration["LLM_MODERATION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase);

    public async Task<ModerationResult> ModerateAsync(string details, IReadOnlyList<string> diagnoses, IReadOnlyList<string> providerNames, CancellationToken cancellationToken)
    {
        var apiUrl = configuration["LLM_MODERATION_API_URL"] ?? string.Empty;
        var apiKey = configuration["LLM_MODERATION_API_KEY"] ?? string.Empty;
        var model = configuration["LLM_MODERATION_MODEL"] ?? "gpt-4o-mini";

        if (!IsEnabled || string.IsNullOrWhiteSpace(apiUrl))
            return new ModerationResult(false, "LLM moderation is not configured.");

        var userPrompt =
            $"Diagnoses: {string.Join(", ", diagnoses)}\n" +
            $"Care providers: {string.Join(", ", providerNames)}\n" +
            $"Recommendation details: {details}";

        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = JsonContent.Create(new
            {
                model,
                temperature = 0,
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = userPrompt }
                }
            })
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken);
        var verdict = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;

        return verdict.StartsWith("APPROVE", StringComparison.OrdinalIgnoreCase)
            ? new ModerationResult(true, null)
            : new ModerationResult(false, string.IsNullOrWhiteSpace(verdict) ? "The moderator returned no verdict." : verdict);
    }

    private sealed record ChatCompletionResponse([property: JsonPropertyName("choices")] ChatCompletionChoice[]? Choices);
    private sealed record ChatCompletionChoice([property: JsonPropertyName("message")] ChatCompletionMessage? Message);
    private sealed record ChatCompletionMessage([property: JsonPropertyName("content")] string? Content);
}
