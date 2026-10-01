namespace MedMatch.Application.Contracts;

public sealed record TranslateRequest(string Text, string? TargetLanguage);
public sealed record TranslateResponse(string TranslatedText);
public sealed record TranslateBatchRequest(IReadOnlyList<string>? Texts, string? TargetLanguage);
public sealed record TranslateBatchItem(string SourceText, string? TranslatedText);
public sealed record TranslateBatchResponse(IReadOnlyList<TranslateBatchItem> Translations);
