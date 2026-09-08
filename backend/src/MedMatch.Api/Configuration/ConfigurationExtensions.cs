namespace MedMatch.Api.Configuration;

public static class ConfigurationExtensions
{
    public static string Required(this IConfiguration configuration, string key) => configuration[key] ?? throw new InvalidOperationException($"{key} is required.");
}
