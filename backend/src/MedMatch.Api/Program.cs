using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using MedMatch.Application.Contracts;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using MedMatch.Infrastructure.Services;
using MedMatch.Api.Configuration;
using MedMatch.Api.Observability;
using MedMatch.Api.Services;
using MedMatch.Api.Security;
using static MedMatch.Api.Mapping.DtoMapper;
using static MedMatch.Api.Queries.ReviewQueries;
using static MedMatch.Api.Security.UserIdentity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.AddMedMatchObservability();
var connectionString = $"Host={builder.Configuration.Required("DATABASE_HOST")};Port={builder.Configuration["DATABASE_PORT"] ?? "5432"};Database={builder.Configuration.Required("DATABASE_NAME")};Username={builder.Configuration.Required("DATABASE_USER")};Password={builder.Configuration.Required("DATABASE_PASSWORD")}";
builder.Services.AddDbContext<MedMatchDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHttpClient<IContentModerationService, ContentModerationService>();
builder.Services.AddHttpClient<ITranslationService, TranslationService>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(builder.Configuration["FRONTEND_URL"] ?? "http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));

var jwtSecret = builder.Configuration.Required("JWT_SECRET");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = builder.Configuration.Required("JWT_ISSUER"), ValidateAudience = true, ValidAudience = builder.Configuration.Required("JWT_AUDIENCE"),
        ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MedMatchDbContext>();
    var passwords = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
    await db.Database.MigrateAsync();
    await DiagnosisMatching.SeedDiagnosisTagsAsync(db, CancellationToken.None);
    await DiagnosisTagLocalization.SeedAsync(db, CancellationToken.None);
    await AdminSeeder.SeedAsync(db, passwords, builder.Configuration, CancellationToken.None);
    if (SampleDataSeeder.IsEnabled(builder.Configuration))
        await SampleDataSeeder.SeedAsync(db, passwords, CancellationToken.None);
    if (CaseStudySeeder.IsEnabled(builder.Configuration))
        await CaseStudySeeder.SeedAsync(db, passwords, CancellationToken.None);
    await RestorePatientRolesAsync(db, CancellationToken.None);
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
var api = app.MapGroup("/api");

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

api.MapPost("/auth/refresh", async (RefreshRequest request, IAuthService auth, CancellationToken ct) =>
    (await auth.RefreshAsync(request, ct)) is { } response ? Results.Ok(response) : Results.Unauthorized()).AllowAnonymous();

api.MapPost("/auth/google", async (GoogleLoginRequest request, IAuthService auth, CancellationToken ct) =>
    (await auth.LoginWithGoogleAsync(request, ct)) is { } response ? Results.Ok(response) : Results.Unauthorized()).AllowAnonymous();

api.MapPost("/auth/verify-email", async (VerifyEmailRequest request, IAuthService auth, CancellationToken ct) =>
    await auth.VerifyEmailAsync(request, ct) ? Results.Ok(new { verified = true }) : Results.BadRequest(new { error = "This verification link is invalid or expired." })).AllowAnonymous();

api.MapGet("/profile", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var profile = await db.PatientProfiles.FindAsync([UserId(principal)], ct); return profile is null ? Results.NotFound() : Results.Ok(ToProfileDto(profile));
}).RequireAuthorization();
api.MapPut("/profile", async (PatientProfileDto dto, ClaimsPrincipal principal, MedMatchDbContext db, IConfiguration configuration, CancellationToken ct) =>
{
    var userId = UserId(principal);
    var profile = await db.PatientProfiles.Include(x => x.DiagnosisTags).SingleOrDefaultAsync(x => x.UserId == userId, ct);
    if (profile is null) return Results.NotFound();
    ApplyProfile(profile, dto);
    await DiagnosisMatching.SyncDiagnosisTagsAsync(profile, dto.Diagnoses ?? [], db, ct);
    await SymptomTagExtraction.EnrichFromSymptomsAsync(profile, db, configuration, ct);
    await db.SaveChangesAsync(ct);
    await DiagnosisMatching.RefreshTagUsageCountsAsync(db, ct);
    await DiagnosisMatching.ComputeMatchesAsync(userId, db, ct);
    return Results.Ok(ToProfileDto(profile));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

api.MapGet("/consent", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var settings = await db.ConsentSettings.FindAsync([UserId(principal)], ct); return settings is null ? Results.NotFound() : Results.Ok(ToConsentDto(settings));
}).RequireAuthorization();
api.MapPut("/consent", async (ConsentSettingsDto dto, HttpContext context, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal); var settings = await db.ConsentSettings.FindAsync([userId], ct); if (settings is null) return Results.NotFound();
    settings.ShowProfilePublicly = dto.ShowProfilePublicly; settings.ClinicsContactMe = dto.ClinicsContactMe; settings.PatientsContactMe = dto.PatientsContactMe; settings.DataForSearch = dto.DataForSearch; settings.Version++; settings.UpdatedAt = DateTimeOffset.UtcNow; settings.Ip = context.Connection.RemoteIpAddress?.ToString(); settings.UserAgent = context.Request.Headers.UserAgent.ToString();
    await db.SaveChangesAsync(ct); await DiagnosisMatching.ComputeMatchesAsync(userId, db, ct); return Results.Ok(ToConsentDto(settings));
}).RequireAuthorization();

api.MapGet("/clinics", async (string? specialty, string? city, string? country, string? tag, MedMatchDbContext db, CancellationToken ct) =>
{
    var clinics = await db.Clinics.AsNoTracking().Where(x => x.PublicationConsentGranted).ToListAsync(ct); var filtered = clinics.Where(x => string.IsNullOrWhiteSpace(specialty) || x.Specialty.Contains(specialty, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(city) || x.City.Contains(city, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(country) || x.Country.Contains(country, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(tag) || x.TreatmentsOffered.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase))).Select(ToClinicDto); return Results.Ok(filtered);
}).AllowAnonymous();
api.MapGet("/clinics/{id:guid}", async (Guid id, MedMatchDbContext db, CancellationToken ct) => { var clinic = await db.Clinics.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.PublicationConsentGranted, ct); return clinic is null ? Results.NotFound() : Results.Ok(ToClinicDto(clinic)); }).AllowAnonymous();
api.MapPost("/clinics", async (ClinicDto dto, MedMatchDbContext db, CancellationToken ct) => { var clinic = new Clinic(); ApplyClinic(clinic, dto); db.Clinics.Add(clinic); await db.SaveChangesAsync(ct); return Results.Created($"/api/clinics/{clinic.Id}", ToClinicDto(clinic)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic,Admin" });
api.MapPut("/clinics/{id:guid}", async (Guid id, ClinicDto dto, MedMatchDbContext db, CancellationToken ct) => { var clinic = await db.Clinics.FindAsync([id], ct); if (clinic is null) return Results.NotFound(); ApplyClinic(clinic, dto); await db.SaveChangesAsync(ct); return Results.Ok(ToClinicDto(clinic)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic,Admin" });
api.MapDelete("/clinics/{id:guid}", async (Guid id, MedMatchDbContext db, CancellationToken ct) => { var clinic = await db.Clinics.FindAsync([id], ct); if (clinic is null) return Results.NotFound(); db.Clinics.Remove(clinic); await db.SaveChangesAsync(ct); return Results.NoContent(); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic,Admin" });

api.MapGet("/doctors", async (string? specialty, string? city, string? tag, MedMatchDbContext db, CancellationToken ct) => { var doctors = await db.Doctors.AsNoTracking().ToListAsync(ct); return Results.Ok(doctors.Where(x => string.IsNullOrWhiteSpace(specialty) || x.Specialty.Contains(specialty, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(city) || (x.City ?? "").Contains(city, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(tag) || x.TreatmentsOffered.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase))).Select(ToDoctorDto)); }).AllowAnonymous();
api.MapPost("/doctors", async (DoctorDto dto, MedMatchDbContext db, CancellationToken ct) => { var doctor = new Doctor(); ApplyDoctor(doctor, dto); db.Doctors.Add(doctor); await db.SaveChangesAsync(ct); return Results.Created($"/api/doctors/{doctor.Id}", ToDoctorDto(doctor)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic,Doctor,Admin" });
api.MapPut("/doctors/{id:guid}", async (Guid id, DoctorDto dto, MedMatchDbContext db, CancellationToken ct) => { var doctor = await db.Doctors.FindAsync([id], ct); if (doctor is null) return Results.NotFound(); ApplyDoctor(doctor, dto); await db.SaveChangesAsync(ct); return Results.Ok(ToDoctorDto(doctor)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic,Doctor,Admin" });
api.MapDelete("/doctors/{id:guid}", async (Guid id, MedMatchDbContext db, CancellationToken ct) => { var doctor = await db.Doctors.FindAsync([id], ct); if (doctor is null) return Results.NotFound(); db.Doctors.Remove(doctor); await db.SaveChangesAsync(ct); return Results.NoContent(); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic,Doctor,Admin" });

api.MapGet("/reviews", async (Guid? clinicId, Guid? doctorId, MedMatchDbContext db, CancellationToken ct) => { var reviews = await ReviewQuery(db, clinicId, doctorId).ToListAsync(ct); return Results.Ok(reviews.Select(ToReviewDto)); }).AllowAnonymous();
api.MapPost("/reviews", async (UpsertReviewRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) => { if (request.Rating is < 1 or > 5 || (request.ClinicId is null && request.DoctorId is null)) return Results.BadRequest(new { error = "Rating must be 1–5 and a clinic or doctor is required." }); var review = new Review { AuthorUserId = UserId(principal) }; ApplyReview(review, request); db.Reviews.Add(review); await db.SaveChangesAsync(ct); MedMatchMetrics.RecordReviewCreated(request.ClinicId is not null); var result = await ReviewQuery(db, null, null).SingleAsync(x => x.Id == review.Id, ct); return Results.Created($"/api/reviews/{review.Id}", ToReviewDto(result)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });
api.MapPut("/reviews/{id:guid}", async (Guid id, UpsertReviewRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) => { var currentUserId = UserId(principal); var review = await db.Reviews.SingleOrDefaultAsync(x => x.Id == id && x.AuthorUserId == currentUserId, ct); if (review is null) return Results.NotFound(); if (request.Rating is < 1 or > 5) return Results.BadRequest(new { error = "Rating must be 1–5." }); ApplyReview(review, request); await db.SaveChangesAsync(ct); var result = await ReviewQuery(db, null, null).SingleAsync(x => x.Id == id, ct); return Results.Ok(ToReviewDto(result)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });
api.MapDelete("/reviews/{id:guid}", async (Guid id, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) => { var currentUserId = UserId(principal); var review = await db.Reviews.SingleOrDefaultAsync(x => x.Id == id && x.AuthorUserId == currentUserId, ct); if (review is null) return Results.NotFound(); db.Reviews.Remove(review); await db.SaveChangesAsync(ct); return Results.NoContent(); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient,Admin" });

api.MapPost("/recommendations", async (CreateRecommendationRequest request, ClaimsPrincipal principal, MedMatchDbContext db, IContentModerationService moderation, IConfiguration configuration, CancellationToken ct) =>
{
    var userId = UserId(principal);
    var clinicIds = (request.ClinicIds ?? []).Distinct().ToArray();
    var diagnoses = (request.Diagnoses ?? []).Select(CleanTagDisplay).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    var details = (request.Details ?? string.Empty).Trim();

    if (clinicIds.Length == 0) return Results.BadRequest(new { error = "Select at least one care provider." });
    if (diagnoses.Length == 0) return Results.BadRequest(new { error = "Select at least one diagnosis or symptom." });
    if (details.Length is < 10 or > 2000) return Results.BadRequest(new { error = "Please describe your experience (10–2000 characters)." });

    var clinics = await db.Clinics.Where(x => clinicIds.Contains(x.Id)).ToListAsync(ct);
    if (clinics.Count != clinicIds.Length) return Results.BadRequest(new { error = "One or more care providers were not found." });

    var tags = await DiagnosisMatching.ResolveDiagnosisTagsAsync(db, diagnoses, ct);

    var recommendation = new Recommendation { AuthorUserId = userId, Details = details };
    foreach (var clinic in clinics) recommendation.Clinics.Add(new RecommendationClinic { Clinic = clinic });
    foreach (var tag in tags) recommendation.DiagnosisTags.Add(new RecommendationDiagnosisTag { DiagnosisTag = tag });

    if (moderation.IsEnabled)
    {
        try
        {
            var verdict = await moderation.ModerateAsync(details, tags.Select(t => t.Name).ToArray(), clinics.Select(c => c.Name).ToArray(), ct);
            recommendation.Status = verdict.Approved ? RecommendationStatus.Approved : RecommendationStatus.Rejected;
            recommendation.ModerationNote = verdict.Reason;
            recommendation.ReviewedAt = DateTimeOffset.UtcNow;
        }
        catch
        {
            // Fail-safe: never publish content that could not be verified.
            recommendation.Status = RecommendationStatus.Pending;
            recommendation.ModerationNote = "Moderation service unavailable; awaiting manual review.";
        }
    }
    else
    {
        var autoApprove = !string.Equals(configuration["LLM_MODERATION_AUTO_APPROVE"], "false", StringComparison.OrdinalIgnoreCase);
        recommendation.Status = autoApprove ? RecommendationStatus.Approved : RecommendationStatus.Pending;
        recommendation.ModerationNote = autoApprove ? null : "Awaiting review.";
        if (autoApprove) recommendation.ReviewedAt = DateTimeOffset.UtcNow;
    }

    db.Recommendations.Add(recommendation);
    await db.SaveChangesAsync(ct);
    MedMatchMetrics.RecordRecommendation(recommendation.Status.ToString());

    var created = await LoadRecommendation(db, recommendation.Id, ct);
    return Results.Created($"/api/recommendations/{created!.Id}", ToRecommendationDto(created));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

api.MapGet("/recommendations", async (Guid? clinicId, string? diagnosis, MedMatchDbContext db, CancellationToken ct) =>
{
    var query = db.Recommendations
        .Include(x => x.AuthorUser).ThenInclude(x => x.PatientProfile)
        .Include(x => x.Clinics).ThenInclude(x => x.Clinic)
        .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
        .Where(x => x.Status == RecommendationStatus.Approved)
        .AsNoTracking().AsQueryable();

    if (clinicId.HasValue && clinicId.Value != Guid.Empty)
        query = query.Where(x => x.Clinics.Any(c => c.ClinicId == clinicId.Value));

    if (!string.IsNullOrWhiteSpace(diagnosis))
    {
        var norm = NormalizeTag(diagnosis);
        query = query.Where(x => x.DiagnosisTags.Any(dt => dt.DiagnosisTag.Slug.Contains(ToSlug(norm)) || NormalizeTag(dt.DiagnosisTag.Name).Contains(norm)));
    }

    var recommendations = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    return Results.Ok(recommendations.Select(ToRecommendationDto));
}).AllowAnonymous();

api.MapGet("/recommendations/mine", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal);
    var recommendations = await db.Recommendations
        .Include(x => x.AuthorUser).ThenInclude(x => x.PatientProfile)
        .Include(x => x.Clinics).ThenInclude(x => x.Clinic)
        .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
        .Where(x => x.AuthorUserId == userId)
        .AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    return Results.Ok(recommendations.Select(ToRecommendationDto));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

api.MapGet("/admin/recommendations", async (string? status, MedMatchDbContext db, CancellationToken ct) =>
{
    var query = db.Recommendations
        .Include(x => x.AuthorUser).ThenInclude(x => x.PatientProfile)
        .Include(x => x.Clinics).ThenInclude(x => x.Clinic)
        .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
        .AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RecommendationStatus>(status, true, out var parsed))
        query = query.Where(x => x.Status == parsed);

    var recommendations = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    return Results.Ok(recommendations.Select(ToRecommendationDto));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

api.MapPost("/admin/recommendations/{id:guid}/moderate", async (Guid id, ModerateRecommendationRequest request, MedMatchDbContext db, CancellationToken ct) =>
{
    var recommendation = await db.Recommendations.SingleOrDefaultAsync(x => x.Id == id, ct);
    if (recommendation is null) return Results.NotFound();
    recommendation.Status = request.Approve ? RecommendationStatus.Approved : RecommendationStatus.Rejected;
    recommendation.ModerationNote = string.IsNullOrWhiteSpace(request.Note) ? (request.Approve ? "Approved by administrator." : "Rejected by administrator.") : request.Note.Trim();
    recommendation.ReviewedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { id = recommendation.Id, status = recommendation.Status.ToString(), moderationNote = recommendation.ModerationNote });
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

api.MapDelete("/recommendations/{id:guid}", async (Guid id, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal);
    var isAdmin = principal.IsInRole(UserRole.Admin.ToString());
    var recommendation = await db.Recommendations.SingleOrDefaultAsync(x => x.Id == id && (isAdmin || x.AuthorUserId == userId), ct);
    if (recommendation is null) return Results.NotFound();
    db.Recommendations.Remove(recommendation);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient,Admin" });

api.MapGet("/clinic/patients", async (string? diagnosis, string? symptom, MedMatchDbContext db, CancellationToken ct) =>
{
    var profiles = await db.PatientProfiles.Include(x => x.User).ThenInclude(x => x.ConsentSettings).Where(x => x.User.ConsentSettings!.ClinicsContactMe && x.User.ConsentSettings.DataForSearch).AsNoTracking().ToListAsync(ct);
    return Results.Ok(profiles.Where(x => string.IsNullOrWhiteSpace(diagnosis) || x.Diagnoses.Any(v => v.Contains(diagnosis, StringComparison.OrdinalIgnoreCase))).Where(x => string.IsNullOrWhiteSpace(symptom) || (x.Symptoms ?? "").Contains(symptom, StringComparison.OrdinalIgnoreCase)).Select(x => new { x.City, x.Country, x.Diagnoses, x.Interventions, x.Symptoms }));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic" });

api.MapGet("/people", async (string? diagnosis, string? symptom, string? city, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var currentUserId = UserId(principal);
    var profiles = await db.PatientProfiles
        .Include(x => x.User).ThenInclude(x => x.ConsentSettings)
        .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
        .Where(x => x.UserId != currentUserId && x.User.ConsentSettings != null && x.User.ConsentSettings.PatientsContactMe && x.User.ConsentSettings.DataForSearch)
        .AsNoTracking().ToListAsync(ct);

    var diagNormalized = string.IsNullOrWhiteSpace(diagnosis) ? null : NormalizeTag(diagnosis);

    var matches = profiles
        .Where(x => string.IsNullOrWhiteSpace(diagNormalized) ||
                    x.Diagnoses.Any(v => NormalizeTag(v).Contains(diagNormalized)) ||
                    x.DiagnosisTags.Any(dt => dt.DiagnosisTag.Slug.Contains(diagNormalized) || NormalizeTag(dt.DiagnosisTag.Name).Contains(diagNormalized)))
        .Where(x => string.IsNullOrWhiteSpace(symptom) || (x.Symptoms ?? "").Contains(symptom, StringComparison.OrdinalIgnoreCase))
        .Where(x => string.IsNullOrWhiteSpace(city) || (x.City ?? "").Contains(city, StringComparison.OrdinalIgnoreCase))
        .Select(x => new PatientDirectoryDto(x.UserId, DisplayName(x), x.City, x.Country, x.Diagnoses, x.Interventions, x.Symptoms, x.Bio, x.Languages));
    return Results.Ok(matches);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

api.MapPost("/people/{id:guid}/connection-requests", async (Guid id, ConnectionRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var senderId = UserId(principal);
    if (senderId == id) return Results.BadRequest(new { error = "You cannot connect with yourself." });
    var recipient = await db.Users.Include(x => x.ConsentSettings).SingleOrDefaultAsync(x => x.Id == id && x.Roles.Any(r => r.Role == UserRole.Patient), ct);
    if (recipient?.ConsentSettings is null || !recipient.ConsentSettings.PatientsContactMe) return Results.NotFound();
    var message = string.IsNullOrWhiteSpace(request.Message) ? "I would like to connect and exchange experiences through MedMatch." : request.Message.Trim();
    db.Messages.Add(new Message { FromUserId = senderId, ToUserId = id, ThreadId = Guid.NewGuid(), Content = message, ConsentSnapshot = "PatientsContactMe=true" });
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

api.MapGet("/diagnosis-tags/suggest", async (string? q, HttpContext context, MedMatchDbContext db, CancellationToken ct) =>
{
    var culture = PreferredCulture(context);
    var query = string.IsNullOrWhiteSpace(q) ? string.Empty : NormalizeTag(q);
    var tags = await db.DiagnosisTags.AsNoTracking()
        .Include(x => x.Translations)
        .Where(x => query.Length == 0 || x.Name.ToLower().Contains(query) || x.Slug.Contains(ToSlug(query)))
        .OrderByDescending(x => x.UsageCount).ThenBy(x => x.Name)
        .Take(20).ToListAsync(ct);
    return Results.Ok(tags.Select(x => ToDiagnosisTagDto(x, culture)));
}).RequireAuthorization();

api.MapPost("/translate", async (TranslateRequest request, ITranslationService translator, CancellationToken ct) =>
{
    var text = (request.Text ?? string.Empty).Trim();
    if (text.Length == 0) return Results.BadRequest(new { error = "Text is required." });
    if (text.Length > 2000) return Results.BadRequest(new { error = "Text is too long to translate." });
    var target = (request.TargetLanguage ?? "en").Trim().ToLowerInvariant();
    if (target is not ("en" or "es" or "de" or "it")) return Results.BadRequest(new { error = "Unsupported target language." });
    var translated = await translator.TranslateAsync(text, target, ct);
    if (translated is not null)
    {
        MedMatchMetrics.RecordTranslation(target);
    }
    return translated is null ? Results.StatusCode(StatusCodes.Status503ServiceUnavailable) : Results.Ok(new TranslateResponse(translated));
}).AllowAnonymous();

api.MapPost("/translate/batch", async (TranslateBatchRequest request, ITranslationService translator, CancellationToken ct) =>
{
    var texts = request.Texts?
        .Select(text => text?.Trim())
        .Where(text => !string.IsNullOrWhiteSpace(text))
        .Select(text => text!)
        .Distinct(StringComparer.Ordinal)
        .ToArray() ?? [];

    if (texts.Length == 0) return Results.BadRequest(new { error = "At least one text value is required." });
    if (texts.Length > 50) return Results.BadRequest(new { error = "A maximum of 50 text values can be translated at once." });
    if (texts.Any(text => text.Length > 2000)) return Results.BadRequest(new { error = "Each text value must be 2,000 characters or fewer." });

    var target = (request.TargetLanguage ?? "en").Trim().ToLowerInvariant();
    if (target is not ("en" or "es" or "de" or "it")) return Results.BadRequest(new { error = "Unsupported target language." });

    var translations = await translator.TranslateManyAsync(texts, target, ct);
    MedMatchMetrics.RecordTranslation(target);
    var response = new List<TranslateBatchItem>(texts.Length);
    foreach (var text in texts)
    {
        response.Add(new TranslateBatchItem(text, translations.TryGetValue(text, out var translated) ? translated : null));
    }
    return Results.Ok(new TranslateBatchResponse(response));
}).AllowAnonymous();

api.MapGet("/matches", async (string? country, string? city, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var currentUserId = UserId(principal);
    var current = await db.PatientProfiles
        .Include(x => x.User).ThenInclude(x => x.ConsentSettings)
        .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
        .SingleOrDefaultAsync(x => x.UserId == currentUserId, ct);

    if (current is null || current.User.ConsentSettings is null ||
        !current.User.ConsentSettings.PatientsContactMe || !current.User.ConsentSettings.DataForSearch)
    {
        return Results.Ok(Array.Empty<MatchDto>());
    }

    var candidates = await db.PatientProfiles
        .Include(x => x.User).ThenInclude(x => x.ConsentSettings)
        .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
        .Where(x => x.UserId != currentUserId && x.User.ConsentSettings != null && x.User.ConsentSettings.PatientsContactMe && x.User.ConsentSettings.DataForSearch)
        .AsNoTracking().ToListAsync(ct);

    var matches = candidates
        .Select(candidate =>
        {
            var (score, sharedDiagnoses, sharedSymptoms, sameLoc) = DiagnosisMatching.EvaluateMatch(current, candidate);
            return new
            {
                Score = score,
                SharedDiagnoses = sharedDiagnoses,
                SharedSymptoms = sharedSymptoms,
                SameLocation = sameLoc,
                Profile = candidate
            };
        })
        .Where(x => x.Score > 0 && x.SharedDiagnoses.Length > 0)
        .Where(x => string.IsNullOrWhiteSpace(country) || string.Equals(x.Profile.Country?.Trim(), country.Trim(), StringComparison.OrdinalIgnoreCase))
        .Where(x => string.IsNullOrWhiteSpace(city) || (x.Profile.City ?? "").Contains(city.Trim(), StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(x => x.Score)
        .ThenByDescending(x => x.SharedDiagnoses.Length)
        .Select(x => new MatchDto(
            x.Profile.UserId,
            DisplayName(x.Profile),
            x.Profile.City,
            x.Profile.Country,
            x.SharedDiagnoses,
            x.SharedSymptoms,
            x.SameLocation,
            x.Score,
            x.Profile.Bio,
            x.Profile.Languages
        ))
        .ToList();

    sw.Stop();
    MedMatchMetrics.RecordMatchesComputed(matches.Count, sw.Elapsed.TotalMilliseconds);

    return Results.Ok(matches);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

api.MapGet("/matches/summary", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var currentUserId = UserId(principal);
    var unread = await db.MatchNotifications.AsNoTracking().CountAsync(x => x.UserId == currentUserId && !x.IsRead, ct);
    return Results.Ok(new MatchSummaryDto(unread));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

api.MapGet("/matches/notifications", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var currentUserId = UserId(principal);
    var notifications = await db.MatchNotifications.AsNoTracking().Where(x => x.UserId == currentUserId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    var matchedIds = notifications.Select(x => x.MatchedUserId).Distinct().ToList();
    var profiles = await db.PatientProfiles.AsNoTracking().Where(x => matchedIds.Contains(x.UserId)).ToDictionaryAsync(x => x.UserId, ct);
    return Results.Ok(notifications.Select(x => new MatchNotificationDto(x.Id, x.MatchedUserId, profiles.TryGetValue(x.MatchedUserId, out var p) ? DisplayName(p) : "MedMatch member", x.SharedDiagnoses, x.Score, x.IsRead, x.CreatedAt)));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

api.MapPost("/matches/notifications/read", async (Guid? id, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var currentUserId = UserId(principal);
    var query = db.MatchNotifications.Where(x => x.UserId == currentUserId && !x.IsRead);
    if (id.HasValue && id.Value != Guid.Empty)
    {
        query = query.Where(x => x.Id == id.Value);
    }
    var unread = await query.ToListAsync(ct);
    foreach (var n in unread) n.IsRead = true;
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

api.MapGet("/admin/users", async (string? search, string? role, int? page, int? pageSize, MedMatchDbContext db, CancellationToken ct) =>
{
    var query = db.Users.Include(x => x.PatientProfile).Include(x => x.ConsentSettings).Include(x => x.Roles).AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
    {
        var terms = search.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).ToArray();
        foreach (var term in terms)
        {
            query = query.Where(x =>
                x.Email.ToLower().Contains(term) ||
                (x.PatientProfile != null && (
                    (x.PatientProfile.Pseudonym != null && x.PatientProfile.Pseudonym.ToLower().Contains(term)) ||
                    (x.PatientProfile.RealName != null && x.PatientProfile.RealName.ToLower().Contains(term))
                )));
        }
    }
    if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var r))
    {
        query = query.Where(x => x.Roles.Any(a => a.Role == r));
    }

    var size = Math.Clamp(pageSize ?? 25, 1, 100);
    var currentPage = Math.Max(page ?? 1, 1);
    var total = await query.CountAsync(ct);
    var users = await query.OrderByDescending(x => x.CreatedAt).Skip((currentPage - 1) * size).Take(size).ToListAsync(ct);

    var result = users.Select(u => new AdminUserDto(
        u.Id,
        u.Email,
        u.Roles.Select(a => a.Role.ToString()).OrderBy(x => x).ToArray(),
        u.PatientProfile is null ? null : DisplayName(u.PatientProfile),
        u.PatientProfile?.City,
        u.PatientProfile?.Country,
        u.PatientProfile?.Diagnoses ?? [],
        u.PatientProfile?.Symptoms ?? string.Empty,
        u.EmailConfirmed,
        u.IsActive,
        u.ConsentSettings?.PatientsContactMe ?? false,
        u.ConsentSettings?.DataForSearch ?? false,
        u.CreatedAt
    ));

    return Results.Ok(new { total, page = currentPage, pageSize = size, items = result });
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

api.MapGet("/admin/users/{id:guid}/matches", async (Guid id, MedMatchDbContext db, CancellationToken ct) =>
{
    var target = await db.PatientProfiles
        .Include(x => x.User).ThenInclude(x => x.ConsentSettings)
        .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
        .SingleOrDefaultAsync(x => x.UserId == id, ct);

    if (target is null) return Results.NotFound();

    var candidates = await db.PatientProfiles
        .Include(x => x.User).ThenInclude(x => x.ConsentSettings)
        .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
        .Where(x => x.UserId != id && x.User.ConsentSettings != null && x.User.ConsentSettings.PatientsContactMe && x.User.ConsentSettings.DataForSearch)
        .AsNoTracking().ToListAsync(ct);

    var matches = candidates
        .Select(candidate =>
        {
            var (score, sharedDiagnoses, sharedSymptoms, sameLoc) = DiagnosisMatching.EvaluateMatch(target, candidate);
            return new { Score = score, SharedDiagnoses = sharedDiagnoses, SharedSymptoms = sharedSymptoms, SameLocation = sameLoc, Profile = candidate };
        })
        .Where(x => x.Score > 0 && x.SharedDiagnoses.Length > 0)
        .OrderByDescending(x => x.Score)
        .ThenByDescending(x => x.SharedDiagnoses.Length)
        .Select(x => new MatchDto(
            x.Profile.UserId,
            DisplayName(x.Profile),
            x.Profile.City,
            x.Profile.Country,
            x.SharedDiagnoses,
            x.SharedSymptoms,
            x.SameLocation,
            x.Score,
            x.Profile.Bio,
            x.Profile.Languages
        ));

    return Results.Ok(matches);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

api.MapPost("/admin/users/{id:guid}/active", async (Guid id, UpdateActiveRequest request, MedMatchDbContext db, CancellationToken ct) =>
{
    var user = await db.Users.Include(x => x.Roles).SingleOrDefaultAsync(x => x.Id == id, ct);
    if (user is null) return Results.NotFound();

    if (!request.IsActive && user.IsActive && user.Roles.Any(x => x.Role == UserRole.Admin))
    {
        var activeAdminCount = await db.Users
            .CountAsync(x => x.IsActive && x.Roles.Any(role => role.Role == UserRole.Admin), ct);
        if (activeAdminCount <= 1)
            return Results.BadRequest(new { error = "At least one active administrator must remain." });
    }

    user.IsActive = request.IsActive;
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { id = user.Id, isActive = user.IsActive });
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

api.MapPost("/admin/users/{id:guid}/role", async (Guid id, UpdateRoleRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        return Results.BadRequest(new { error = "Invalid role. Use Patient, Clinic, Doctor or Admin." });

    var user = await db.Users.Include(x => x.Roles).SingleOrDefaultAsync(x => x.Id == id, ct);
    if (user is null) return Results.NotFound();

    var currentAdminId = UserId(principal);
    var existing = user.Roles.FirstOrDefault(a => a.Role == role);

    if (request.Enabled && existing is null)
    {
        user.Roles.Add(new UserRoleAssignment { Role = role });
        if (role == UserRole.Patient && user.PatientProfile is null)
            user.PatientProfile = new PatientProfile();
    }
    else if (!request.Enabled && existing is not null)
    {
        if (role == UserRole.Admin)
        {
            if (id == currentAdminId)
                return Results.BadRequest(new { error = "You cannot remove your own administrator role." });
            var adminCount = await db.Users.CountAsync(x => x.Roles.Any(a => a.Role == UserRole.Admin), ct);
            if (adminCount <= 1)
                return Results.BadRequest(new { error = "At least one administrator must remain." });
        }
        if (user.Roles.Count <= 1)
            return Results.BadRequest(new { error = "A user must keep at least one role." });
        user.Roles.Remove(existing);
    }

    await db.SaveChangesAsync(ct);
    return Results.Ok(new { id = user.Id, roles = user.Roles.Select(a => a.Role.ToString()).OrderBy(x => x).ToArray() });
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

// --- Symptom Diary Endpoints (Patient web UI) ---
var diaryApi = api.MapGroup("/symptom-diary").RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

diaryApi.MapGet("/sheets", async (DateOnly? startDate, DateOnly? endDate, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal);
    var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
    var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

    var sheets = await db.SymptomDiarySheets
        .Include(x => x.Entries)
        .Where(x => x.UserId == userId && x.Date >= start && x.Date <= end)
        .OrderByDescending(x => x.Date)
        .AsNoTracking()
        .ToListAsync(ct);

    return Results.Ok(sheets.Select(ToSymptomDiarySheetSummaryDto));
});

diaryApi.MapGet("/sheets/{date}", async (DateOnly date, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal);
    var sheet = await db.SymptomDiarySheets
        .Include(x => x.Entries)
        .AsNoTracking()
        .SingleOrDefaultAsync(x => x.UserId == userId && x.Date == date, ct);

    if (sheet is null)
    {
        return Results.Ok(new SymptomDiarySheetDto(
            Guid.Empty,
            date,
            null,
            null,
            null,
            null,
            0,
            null,
            null,
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        ));
    }

    return Results.Ok(ToSymptomDiarySheetDto(sheet));
});

diaryApi.MapPut("/sheets/{date}", async (DateOnly date, UpdateDailySheetRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal);
    var sheet = await db.SymptomDiarySheets
        .Include(x => x.Entries)
        .SingleOrDefaultAsync(x => x.UserId == userId && x.Date == date, ct);

    if (sheet is null)
    {
        sheet = new SymptomDiarySheet
        {
            UserId = userId,
            Date = date,
            OverallWellbeing = request.OverallWellbeing,
            SleepQuality = request.SleepQuality,
            SleepHours = request.SleepHours,
            DailyNotes = request.DailyNotes?.Trim()
        };
        db.SymptomDiarySheets.Add(sheet);
    }
    else
    {
        sheet.OverallWellbeing = request.OverallWellbeing;
        sheet.SleepQuality = request.SleepQuality;
        sheet.SleepHours = request.SleepHours;
        sheet.DailyNotes = request.DailyNotes?.Trim();
        sheet.UpdatedAt = DateTimeOffset.UtcNow;
    }

    await db.SaveChangesAsync(ct);
    return Results.Ok(ToSymptomDiarySheetDto(sheet));
});

diaryApi.MapPost("/sheets/{date}/entries", async (DateOnly date, UpsertSymptomEntryRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.SymptomName))
        return Results.BadRequest(new { error = "Symptom name is required." });

    var userId = UserId(principal);
    var sheet = await db.SymptomDiarySheets.SingleOrDefaultAsync(x => x.UserId == userId && x.Date == date, ct);
    if (sheet is null)
    {
        sheet = new SymptomDiarySheet { UserId = userId, Date = date };
        db.SymptomDiarySheets.Add(sheet);
        await db.SaveChangesAsync(ct);
    }

    var entry = new SymptomDiaryEntry
    {
        SheetId = sheet.Id,
        UserId = userId,
        Date = date,
        RecordedAt = (request.RecordedAt ?? DateTimeOffset.UtcNow).ToUniversalTime(),
        Category = ParseSymptomCategory(request.Category),
        SymptomName = request.SymptomName.Trim(),
        PainType = request.PainType?.Trim(),
        BodyLocation = request.BodyLocation?.Trim(),
        Severity = Math.Clamp(request.Severity, 0, 10),
        DurationMinutes = request.DurationMinutes,
        Triggers = request.Triggers?.Trim(),
        Relievers = request.Relievers?.Trim(),
        MedicationsTaken = request.MedicationsTaken?.Trim(),
        Notes = request.Notes?.Trim(),
        Source = string.IsNullOrWhiteSpace(request.Source) ? "Web" : request.Source.Trim()
    };

    db.SymptomDiaryEntries.Add(entry);
    sheet.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(ct);

    return Results.Created($"/api/symptom-diary/entries/{entry.Id}", ToSymptomDiaryEntryDto(entry));
});

diaryApi.MapPut("/entries/{id:guid}", async (Guid id, UpsertSymptomEntryRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.SymptomName))
        return Results.BadRequest(new { error = "Symptom name is required." });

    var userId = UserId(principal);
    var entry = await db.SymptomDiaryEntries.Include(x => x.Sheet).SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    if (entry is null) return Results.NotFound();

    entry.RecordedAt = request.RecordedAt?.ToUniversalTime() ?? entry.RecordedAt.ToUniversalTime();
    entry.Category = ParseSymptomCategory(request.Category);
    entry.SymptomName = request.SymptomName.Trim();
    entry.PainType = request.PainType?.Trim();
    entry.BodyLocation = request.BodyLocation?.Trim();
    entry.Severity = Math.Clamp(request.Severity, 0, 10);
    entry.DurationMinutes = request.DurationMinutes;
    entry.Triggers = request.Triggers?.Trim();
    entry.Relievers = request.Relievers?.Trim();
    entry.MedicationsTaken = request.MedicationsTaken?.Trim();
    entry.Notes = request.Notes?.Trim();
    if (!string.IsNullOrWhiteSpace(request.Source)) entry.Source = request.Source.Trim();
    entry.UpdatedAt = DateTimeOffset.UtcNow;
    if (entry.Sheet != null) entry.Sheet.UpdatedAt = DateTimeOffset.UtcNow;

    await db.SaveChangesAsync(ct);
    return Results.Ok(ToSymptomDiaryEntryDto(entry));
});

diaryApi.MapDelete("/entries/{id:guid}", async (Guid id, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal);
    var entry = await db.SymptomDiaryEntries.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    if (entry is null) return Results.NotFound();

    db.SymptomDiaryEntries.Remove(entry);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

diaryApi.MapGet("/summary", async (int? days, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal);
    var lookback = Math.Clamp(days ?? 30, 1, 365);
    var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-lookback));
    var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

    var entries = await db.SymptomDiaryEntries
        .Where(x => x.UserId == userId && x.Date >= startDate && x.Date <= endDate)
        .OrderBy(x => x.RecordedAt)
        .AsNoTracking()
        .ToListAsync(ct);

    var sheets = await db.SymptomDiarySheets
        .Where(x => x.UserId == userId && x.Date >= startDate && x.Date <= endDate)
        .AsNoTracking()
        .ToListAsync(ct);

    var totalEntries = entries.Count;
    var trackedDates = entries.Select(x => x.Date).Concat(sheets.Select(x => x.Date)).Distinct().Count();
    var overallAvg = totalEntries > 0 ? Math.Round(entries.Average(x => x.Severity), 1) : 0;

    var trend = entries
        .GroupBy(x => x.Date)
        .OrderBy(g => g.Key)
        .Select(g => new DailySeverityPointDto(
            g.Key,
            Math.Round(g.Average(x => x.Severity), 1),
            g.Max(x => x.Severity),
            g.Count()
        ))
        .ToList();

    var topSymptoms = entries
        .GroupBy(x => new { Name = x.SymptomName.Trim(), Category = x.Category.ToString() })
        .OrderByDescending(g => g.Count())
        .Take(10)
        .Select(g => new SymptomFrequencyDto(
            g.Key.Name,
            g.Key.Category,
            g.Count(),
            Math.Round(g.Average(x => x.Severity), 1)
        ))
        .ToList();

    var topLocations = entries
        .Where(x => !string.IsNullOrWhiteSpace(x.BodyLocation))
        .SelectMany(x => x.BodyLocation!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Where(loc => !string.IsNullOrWhiteSpace(loc))
        .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(g => g.Count())
        .Take(8)
        .Select(g => new LocationFrequencyDto(g.Key, g.Count()))
        .ToList();

    var topPainTypes = entries
        .Where(x => !string.IsNullOrWhiteSpace(x.PainType))
        .SelectMany(x => x.PainType!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Where(pt => !string.IsNullOrWhiteSpace(pt))
        .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(g => g.Count())
        .Take(8)
        .Select(g => new PainTypeFrequencyDto(g.Key, g.Count()))
        .ToList();

    return Results.Ok(new SymptomDiaryAnalyticsDto(
        totalEntries,
        trackedDates,
        overallAvg,
        trend,
        topSymptoms,
        topLocations,
        topPainTypes
    ));
});

diaryApi.MapGet("/bot-key", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal);
    var key = await db.UserBotApiKeys
        .Where(x => x.UserId == userId && x.IsActive)
        .OrderByDescending(x => x.CreatedAt)
        .FirstOrDefaultAsync(ct);

    return Results.Ok(key is null ? null : ToBotApiKeyDto(key));
});

diaryApi.MapPost("/bot-key", async (CreateBotKeyRequest? request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal);

    var existingKeys = await db.UserBotApiKeys.Where(x => x.UserId == userId && x.IsActive).ToListAsync(ct);
    foreach (var k in existingKeys) k.IsActive = false;

    var (rawKey, hash, prefix) = BotApiKeyAuth.GenerateKey();
    var key = new UserBotApiKey
    {
        UserId = userId,
        KeyHash = hash,
        KeyPrefix = prefix,
        Label = string.IsNullOrWhiteSpace(request?.Label) ? "n8n Chatbot" : request.Label.Trim(),
        IsActive = true
    };
    db.UserBotApiKeys.Add(key);
    await db.SaveChangesAsync(ct);

    return Results.Ok(new CreateBotKeyResponse(key.Id, rawKey, prefix, key.Label, key.CreatedAt));
});

diaryApi.MapDelete("/bot-key/{id:guid}", async (Guid id, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal);
    var key = await db.UserBotApiKeys.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    if (key is null) return Results.NotFound();

    key.IsActive = false;
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

// --- Bot Ingestion Endpoints (n8n / Chatbot API) ---
var botApi = api.MapGroup("/bot").AllowAnonymous();

botApi.MapPost("/symptom-entries", async (BotLogSymptomRequest request, HttpContext context, ClaimsPrincipal principal, MedMatchDbContext db, ILogger<Program> logger, CancellationToken ct) =>
{
    var user = await ResolveBotOrUserAsync(context, principal, db, logger, ct);
    if (user is null)
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(request.SymptomName))
        return Results.BadRequest(new { error = "Symptom name is required." });

    var date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
    var sheet = await db.SymptomDiarySheets.SingleOrDefaultAsync(x => x.UserId == user.Id && x.Date == date, ct);
    if (sheet is null)
    {
        sheet = new SymptomDiarySheet { UserId = user.Id, Date = date };
        db.SymptomDiarySheets.Add(sheet);
        await db.SaveChangesAsync(ct);
    }

    var entry = new SymptomDiaryEntry
    {
        SheetId = sheet.Id,
        UserId = user.Id,
        Date = date,
        RecordedAt = (request.RecordedAt ?? DateTimeOffset.UtcNow).ToUniversalTime(),
        Category = ParseSymptomCategory(request.Category),
        SymptomName = request.SymptomName.Trim(),
        PainType = request.PainType?.Trim(),
        BodyLocation = request.BodyLocation?.Trim(),
        Severity = Math.Clamp(request.Severity, 0, 10),
        DurationMinutes = request.DurationMinutes,
        Triggers = request.Triggers?.Trim(),
        Relievers = request.Relievers?.Trim(),
        MedicationsTaken = request.MedicationsTaken?.Trim(),
        Notes = request.Notes?.Trim(),
        Source = string.IsNullOrWhiteSpace(request.Source) ? "n8n" : request.Source.Trim()
    };

    db.SymptomDiaryEntries.Add(entry);
    sheet.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(ct);

    return Results.Created($"/api/symptom-diary/entries/{entry.Id}", ToSymptomDiaryEntryDto(entry));
});

botApi.MapGet("/symptom-entries/today", async (HttpContext context, ClaimsPrincipal principal, MedMatchDbContext db, ILogger<Program> logger, CancellationToken ct) =>
{
    var user = await ResolveBotOrUserAsync(context, principal, db, logger, ct);
    if (user is null)
        return Results.Unauthorized();

    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var sheet = await db.SymptomDiarySheets
        .Include(x => x.Entries)
        .AsNoTracking()
        .SingleOrDefaultAsync(x => x.UserId == user.Id && x.Date == today, ct);

    if (sheet is null)
    {
        return Results.Ok(new SymptomDiarySheetDto(
            Guid.Empty,
            today,
            null,
            null,
            null,
            null,
            0,
            null,
            null,
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        ));
    }

    return Results.Ok(ToSymptomDiarySheetDto(sheet));
});

botApi.MapPost("/symptom-entries/quick-text", async (BotQuickLogTextRequest request, HttpContext context, ClaimsPrincipal principal, MedMatchDbContext db, ILogger<Program> logger, CancellationToken ct) =>
{
    var user = await ResolveBotOrUserAsync(context, principal, db, logger, ct);
    if (user is null)
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(request.Text))
        return Results.BadRequest(new { error = "Text is required." });

    var date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
    var sheet = await db.SymptomDiarySheets.SingleOrDefaultAsync(x => x.UserId == user.Id && x.Date == date, ct);
    if (sheet is null)
    {
        sheet = new SymptomDiarySheet { UserId = user.Id, Date = date };
        db.SymptomDiarySheets.Add(sheet);
        await db.SaveChangesAsync(ct);
    }

    var cleanText = request.Text.Trim();
    var title = cleanText.Length > 80 ? cleanText[..80] + "..." : cleanText;
    var entry = new SymptomDiaryEntry
    {
        SheetId = sheet.Id,
        UserId = user.Id,
        Date = date,
        RecordedAt = DateTimeOffset.UtcNow,
        Category = SymptomCategory.Pain,
        SymptomName = title,
        Severity = 5,
        Notes = cleanText,
        Source = "n8n-quick"
    };

    db.SymptomDiaryEntries.Add(entry);
    sheet.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(ct);

    return Results.Ok(ToSymptomDiaryEntryDto(entry));
});

app.Run();

static async Task<User?> ResolveBotOrUserAsync(HttpContext httpContext, ClaimsPrincipal principal, MedMatchDbContext db, ILogger<Program> logger, CancellationToken ct)
{
    var botUser = await BotApiKeyAuth.AuthenticateBotKeyAsync(httpContext, db, logger, ct);
    if (botUser is not null) return botUser;

    if (principal.Identity?.IsAuthenticated == true)
    {
        var id = UserId(principal);
        return await db.Users.FindAsync([id], ct);
    }

    logger.LogWarning(
        "Unauthorized bot API request. No valid bot key or authenticated user was found. Path: {RequestPath}; X-API-Key header present: {HasApiKeyHeader}; Authorization header present: {HasAuthorizationHeader}",
        httpContext.Request.Path,
        httpContext.Request.Headers.ContainsKey("X-API-Key"),
        httpContext.Request.Headers.ContainsKey("Authorization"));
    return null;
}


static async Task<Recommendation?> LoadRecommendation(MedMatchDbContext db, Guid id, CancellationToken ct) =>
    await db.Recommendations
        .Include(x => x.AuthorUser).ThenInclude(x => x.PatientProfile)
        .Include(x => x.Clinics).ThenInclude(x => x.Clinic)
        .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
        .AsNoTracking()
        .SingleOrDefaultAsync(x => x.Id == id, ct);

/// <summary>
/// Ensures users who still have a patient profile keep the Patient role.
/// Covers accounts promoted to Admin before multi-role support existed.
/// </summary>
static async Task RestorePatientRolesAsync(MedMatchDbContext db, CancellationToken ct)
{
    var missing = await db.Users
        .Include(x => x.Roles)
        .Where(x => x.PatientProfile != null && !x.Roles.Any(r => r.Role == UserRole.Patient))
        .ToListAsync(ct);

    foreach (var user in missing)
        user.Roles.Add(new UserRoleAssignment { Role = UserRole.Patient });

    if (missing.Count > 0)
        await db.SaveChangesAsync(ct);
}





