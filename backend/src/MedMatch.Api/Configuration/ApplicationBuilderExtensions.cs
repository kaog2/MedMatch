namespace MedMatch.Api.Configuration;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseMedMatchPipeline(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Unhandled exception processing HTTP {Method} {Path}", context.Request.Method, context.Request.Path);
                throw;
            }
        });

        if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
