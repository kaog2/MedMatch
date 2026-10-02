using System.Text;
using System.Text.Json.Serialization;
using MedMatch.Application.Contracts;
using MedMatch.Api.Services;
using MedMatch.Infrastructure.Persistence;
using MedMatch.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MedMatch.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddMedMatchServices(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;

        var connectionString = $"Host={configuration.Required("DATABASE_HOST")};Port={configuration["DATABASE_PORT"] ?? "5432"};Database={configuration.Required("DATABASE_NAME")};Username={configuration.Required("DATABASE_USER")};Password={configuration.Required("DATABASE_PASSWORD")}";
        services.AddDbContext<MedMatchDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddHttpClient<IContentModerationService, ContentModerationService>();
        services.AddHttpClient<ITranslationService, TranslationService>();

        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(configuration["FRONTEND_URL"] ?? "http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));

        builder.AddMedMatchAuthentication();
        return builder;
    }

    private static WebApplicationBuilder AddMedMatchAuthentication(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var jwtSecret = configuration.Required("JWT_SECRET");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = configuration.Required("JWT_ISSUER"), ValidateAudience = true, ValidAudience = configuration.Required("JWT_AUDIENCE"),
                ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), ClockSkew = TimeSpan.FromSeconds(30)
            };
        });
        builder.Services.AddAuthorization();
        return builder;
    }
}
