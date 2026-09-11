namespace MedMatch.Application.Contracts;

public sealed record TranslateRequest(string Text, string? TargetLanguage);
public sealed record TranslateResponse(string TranslatedText);
