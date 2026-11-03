using MedMatch.Api.Observability;
using MedMatch.Application.Contracts;

namespace MedMatch.Api.Endpoints;

public static class AuthEndpoints
{
    /// <summary>Authentication endpoints for user registration, login, token refresh, and email verification.</summary>
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        // POST /api/auth/register: creates a patient account and sends the email verification link.
        api.MapPost("/auth/register", async (RegisterRequest request, IAuthService auth, ILogger<Program> logger, CancellationToken ct) =>
        {
            try
            {
                var response = await auth.RegisterAsync(request, ct);
                MedMatchMetrics.RecordRegistration();
                logger.LogInformation("User registered successfully: {Email}", request.Email);
                return Results.Accepted("/api/auth/login", response);
            }
            catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
            catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
        }).AllowAnonymous();

        // POST /api/auth/login: validates credentials and returns access and refresh tokens.
        api.MapPost("/auth/login", async (LoginRequest request, IAuthService auth, ILogger<Program> logger, CancellationToken ct) =>
        {
            try
            {
                var response = await auth.LoginAsync(request, ct);
                var success = response is not null;
                MedMatchMetrics.RecordLogin(success);
                if (success)
                {
                    logger.LogInformation("User logged in successfully: {Email}", request.Email);
                    return Results.Ok(response);
                }
                logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                return Results.Unauthorized();
            }
            catch (InvalidOperationException)
            {
                MedMatchMetrics.RecordLogin(false);
                logger.LogWarning("Forbidden login attempt for deactivated user: {Email}", request.Email);
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }
        }).AllowAnonymous();

        // POST /api/auth/refresh: exchanges a refresh token for a new token pair.
        api.MapPost("/auth/refresh", async (RefreshRequest request, IAuthService auth, CancellationToken ct) =>
            (await auth.RefreshAsync(request, ct)) is { } response ? Results.Ok(response) : Results.Unauthorized()).AllowAnonymous();

        // POST /api/auth/google: signs in or registers with a validated Google ID token.
        api.MapPost("/auth/google", async (GoogleLoginRequest request, IAuthService auth, CancellationToken ct) =>
            (await auth.LoginWithGoogleAsync(request, ct)) is { } response ? Results.Ok(response) : Results.Unauthorized()).AllowAnonymous();

        // POST /api/auth/verify-email: confirms an email address using a one-time token.
        api.MapPost("/auth/verify-email", async (VerifyEmailRequest request, IAuthService auth, CancellationToken ct) =>
            await auth.VerifyEmailAsync(request, ct) ? Results.Ok(new { verified = true }) : Results.BadRequest(new { error = "This verification link is invalid or expired." })).AllowAnonymous();

        return api;
    }
}
