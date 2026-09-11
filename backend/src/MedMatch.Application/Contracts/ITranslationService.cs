namespace MedMatch.Application.Contracts;

public interface ITranslationService
{
    Task<string?> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken);
}
