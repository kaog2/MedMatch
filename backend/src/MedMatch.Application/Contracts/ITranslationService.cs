namespace MedMatch.Application.Contracts;

public interface ITranslationService
{
    Task<string?> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string>> TranslateManyAsync(IEnumerable<string> texts, string targetLanguage, CancellationToken cancellationToken);
}
