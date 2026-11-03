namespace MedMatch.Api.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    public static WebApplication MapMedMatchEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var api = app.MapGroup("/api");
        api.MapAuthEndpoints();
        api.MapProfileEndpoints();
        api.MapCareProviderEndpoints();
        api.MapRecommendationEndpoints();
        api.MapPatientDiscoveryEndpoints();
        api.MapTaxonomyEndpoints();
        api.MapMatchEndpoints();
        api.MapAdminEndpoints();
        api.MapSymptomDiaryEndpoints();
        api.MapBotEndpoints();

        return app;
    }
}
