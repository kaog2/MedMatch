using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using MedMatch.Application.Contracts;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using MedMatch.Infrastructure.Services;
using MedMatch.Api.Configuration;
using MedMatch.Api.Services;
using static MedMatch.Api.Mapping.DtoMapper;
using static MedMatch.Api.Queries.ReviewQueries;
using static MedMatch.Api.Security.UserIdentity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var connectionString = $"Host={builder.Configuration.Required("DATABASE_HOST")};Port={builder.Configuration["DATABASE_PORT"] ?? "5432"};Database={builder.Configuration.Required("DATABASE_NAME")};Username={builder.Configuration.Required("DATABASE_USER")};Password={builder.Configuration.Required("DATABASE_PASSWORD")}";
builder.Services.AddDbContext<MedMatchDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHttpClient<IContentModerationService, ContentModerationService>();
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
    await AdminSeeder.SeedAsync(db, passwords, builder.Configuration, CancellationToken.None);
    if (SampleDataSeeder.IsEnabled(builder.Configuration))
        await SampleDataSeeder.SeedAsync(db, passwords, CancellationToken.None);
    if (CaseStudySeeder.IsEnabled(builder.Configuration))
        await CaseStudySeeder.SeedAsync(db, passwords, CancellationToken.None);
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
var api = app.MapGroup("/api");

api.MapPost("/auth/register", async (RegisterRequest request, IAuthService auth, CancellationToken ct) =>
{
    try { return Results.Accepted("/api/auth/login", await auth.RegisterAsync(request, ct)); }
    catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
    catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
}).AllowAnonymous();

api.MapPost("/auth/login", async (LoginRequest request, IAuthService auth, CancellationToken ct) =>
{
    try { return (await auth.LoginAsync(request, ct)) is { } response ? Results.Ok(response) : Results.Unauthorized(); }
    catch (InvalidOperationException) { return Results.StatusCode(StatusCodes.Status403Forbidden); }
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
api.MapPut("/profile", async (PatientProfileDto dto, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal);
    var profile = await db.PatientProfiles.Include(x => x.DiagnosisTags).SingleOrDefaultAsync(x => x.UserId == userId, ct);
    if (profile is null) return Results.NotFound();
    ApplyProfile(profile, dto);
    await DiagnosisMatching.SyncDiagnosisTagsAsync(profile, dto.Diagnoses ?? [], db, ct);
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

api.MapGet("/clinics", async (string? specialty, string? city, string? tag, MedMatchDbContext db, CancellationToken ct) =>
{
    var clinics = await db.Clinics.AsNoTracking().Where(x => x.PublicationConsentGranted).ToListAsync(ct); var filtered = clinics.Where(x => string.IsNullOrWhiteSpace(specialty) || x.Specialty.Contains(specialty, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(city) || x.City.Contains(city, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(tag) || x.TreatmentsOffered.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase))).Select(ToClinicDto); return Results.Ok(filtered);
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
api.MapPost("/reviews", async (UpsertReviewRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) => { if (request.Rating is < 1 or > 5 || (request.ClinicId is null && request.DoctorId is null)) return Results.BadRequest(new { error = "Rating must be 1–5 and a clinic or doctor is required." }); var review = new Review { AuthorUserId = UserId(principal) }; ApplyReview(review, request); db.Reviews.Add(review); await db.SaveChangesAsync(ct); var result = await ReviewQuery(db, null, null).SingleAsync(x => x.Id == review.Id, ct); return Results.Created($"/api/reviews/{review.Id}", ToReviewDto(result)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });
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
    return Results.Ok(profiles.Where(x => string.IsNullOrWhiteSpace(diagnosis) || x.Diagnoses.Any(v => v.Contains(diagnosis, StringComparison.OrdinalIgnoreCase))).Where(x => string.IsNullOrWhiteSpace(symptom) || x.Symptoms.Any(v => v.Contains(symptom, StringComparison.OrdinalIgnoreCase))).Select(x => new { x.City, x.Country, x.Diagnoses, x.Interventions, x.Symptoms }));
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
        .Where(x => string.IsNullOrWhiteSpace(symptom) || x.Symptoms.Any(v => v.Contains(symptom, StringComparison.OrdinalIgnoreCase)))
        .Where(x => string.IsNullOrWhiteSpace(city) || (x.City ?? "").Contains(city, StringComparison.OrdinalIgnoreCase))
        .Select(x => new PatientDirectoryDto(x.UserId, DisplayName(x), x.City, x.Country, x.Diagnoses, x.Interventions, x.Symptoms, x.Bio, x.Languages));
    return Results.Ok(matches);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

api.MapPost("/people/{id:guid}/connection-requests", async (Guid id, ConnectionRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var senderId = UserId(principal);
    if (senderId == id) return Results.BadRequest(new { error = "You cannot connect with yourself." });
    var recipient = await db.Users.Include(x => x.ConsentSettings).SingleOrDefaultAsync(x => x.Id == id && x.Role == UserRole.Patient, ct);
    if (recipient?.ConsentSettings is null || !recipient.ConsentSettings.PatientsContactMe) return Results.NotFound();
    var message = string.IsNullOrWhiteSpace(request.Message) ? "I would like to connect and exchange experiences through MedMatch." : request.Message.Trim();
    db.Messages.Add(new Message { FromUserId = senderId, ToUserId = id, ThreadId = Guid.NewGuid(), Content = message, ConsentSnapshot = "PatientsContactMe=true" });
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

api.MapGet("/diagnosis-tags/suggest", async (string? q, MedMatchDbContext db, CancellationToken ct) =>
{
    var query = string.IsNullOrWhiteSpace(q) ? string.Empty : NormalizeTag(q);
    var tags = await db.DiagnosisTags.AsNoTracking()
        .Where(x => query.Length == 0 || x.Name.ToLower().Contains(query) || x.Slug.Contains(ToSlug(query)))
        .OrderByDescending(x => x.UsageCount).ThenBy(x => x.Name)
        .Take(20).ToListAsync(ct);
    return Results.Ok(tags.Select(ToDiagnosisTagDto));
}).RequireAuthorization();

api.MapGet("/matches", async (string? country, string? city, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
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
        ));

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
    var query = db.Users.Include(x => x.PatientProfile).Include(x => x.ConsentSettings).AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim().ToLowerInvariant();
        query = query.Where(x => x.Email.ToLower().Contains(term) || (x.PatientProfile != null && (x.PatientProfile.Pseudonym != null && x.PatientProfile.Pseudonym.ToLower().Contains(term) || x.PatientProfile.RealName != null && x.PatientProfile.RealName.ToLower().Contains(term))));
    }
    if (!string.IsNullOrWhiteSpace(role))
    {
        var parsed = Enum.TryParse<UserRole>(role, true, out var r);
        if (parsed) query = query.Where(x => x.Role == r);
    }

    var size = Math.Clamp(pageSize ?? 25, 1, 100);
    var currentPage = Math.Max(page ?? 1, 1);
    var total = await query.CountAsync(ct);
    var users = await query.OrderByDescending(x => x.CreatedAt).Skip((currentPage - 1) * size).Take(size).ToListAsync(ct);

    var result = users.Select(u => new AdminUserDto(
        u.Id,
        u.Email,
        u.Role.ToString(),
        u.PatientProfile is null ? null : DisplayName(u.PatientProfile),
        u.PatientProfile?.City,
        u.PatientProfile?.Country,
        u.PatientProfile?.Diagnoses ?? [],
        u.PatientProfile?.Symptoms ?? [],
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
    var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
    if (user is null) return Results.NotFound();
    user.IsActive = request.IsActive;
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { id = user.Id, isActive = user.IsActive });
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

app.Run();

static async Task<Recommendation?> LoadRecommendation(MedMatchDbContext db, Guid id, CancellationToken ct) =>
    await db.Recommendations
        .Include(x => x.AuthorUser).ThenInclude(x => x.PatientProfile)
        .Include(x => x.Clinics).ThenInclude(x => x.Clinic)
        .Include(x => x.DiagnosisTags).ThenInclude(x => x.DiagnosisTag)
        .AsNoTracking()
        .SingleOrDefaultAsync(x => x.Id == id, ct);





