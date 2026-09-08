using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using MedMatch.Application;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using MedMatch.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var connectionString = $"Host={Required("DATABASE_HOST")};Port={builder.Configuration["DATABASE_PORT"] ?? "5432"};Database={Required("DATABASE_NAME")};Username={Required("DATABASE_USER")};Password={Required("DATABASE_PASSWORD")}";
builder.Services.AddDbContext<MedMatchDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<TokenService>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(builder.Configuration["FRONTEND_URL"] ?? "http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));

var jwtSecret = Required("JWT_SECRET");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = Required("JWT_ISSUER"), ValidateAudience = true, ValidAudience = Required("JWT_AUDIENCE"),
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
    await db.Database.MigrateAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
var api = app.MapGroup("/api");

api.MapPost("/auth/register", async (RegisterRequest request, MedMatchDbContext db, TokenService tokens, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || request.Password.Length < 8) return Results.BadRequest(new { error = "Email and a password of at least 8 characters are required." });
    if (request.Role is UserRole.Admin) return Results.BadRequest(new { error = "Admin registration is not available." });
    var email = request.Email.Trim().ToLowerInvariant();
    if (await db.Users.AnyAsync(x => x.Email == email, ct)) return Results.Conflict(new { error = "An account with that email already exists." });
    var user = new User { Email = email, Role = request.Role, PasswordHash = Passwords.Hash(request.Password), PatientProfile = request.Role == UserRole.Patient ? new PatientProfile() : null, ConsentSettings = new ConsentSettings() };
    db.Users.Add(user); await db.SaveChangesAsync(ct); return Results.Ok(await IssueTokens(user, db, tokens, ct));
}).AllowAnonymous();

api.MapPost("/auth/login", async (LoginRequest request, MedMatchDbContext db, TokenService tokens, CancellationToken ct) =>
{
    var user = await db.Users.SingleOrDefaultAsync(x => x.Email == request.Email.Trim().ToLowerInvariant(), ct);
    if (user is null || !Passwords.Verify(request.Password, user.PasswordHash)) return Results.Unauthorized();
    user.LastLogin = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(await IssueTokens(user, db, tokens, ct));
}).AllowAnonymous();

api.MapPost("/auth/refresh", async (RefreshRequest request, MedMatchDbContext db, TokenService tokens, CancellationToken ct) =>
{
    var hash = TokenService.Hash(request.RefreshToken); var stored = await db.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
    if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= DateTimeOffset.UtcNow) return Results.Unauthorized();
    stored.RevokedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(await IssueTokens(stored.User, db, tokens, ct));
}).AllowAnonymous();

api.MapGet("/profile", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var profile = await db.PatientProfiles.FindAsync([UserId(principal)], ct); return profile is null ? Results.NotFound() : Results.Ok(ToProfileDto(profile));
}).RequireAuthorization();
api.MapPut("/profile", async (PatientProfileDto dto, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var userId = UserId(principal); var profile = await db.PatientProfiles.FindAsync([userId], ct); if (profile is null) return Results.NotFound();
    ApplyProfile(profile, dto); await db.SaveChangesAsync(ct); return Results.Ok(ToProfileDto(profile));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });

api.MapGet("/consent", async (ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var settings = await db.ConsentSettings.FindAsync([UserId(principal)], ct); return settings is null ? Results.NotFound() : Results.Ok(ToConsentDto(settings));
}).RequireAuthorization();
api.MapPut("/consent", async (ConsentSettingsDto dto, HttpContext context, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var settings = await db.ConsentSettings.FindAsync([UserId(principal)], ct); if (settings is null) return Results.NotFound();
    settings.ShowProfilePublicly = dto.ShowProfilePublicly; settings.ClinicsContactMe = dto.ClinicsContactMe; settings.PatientsContactMe = dto.PatientsContactMe; settings.DataForSearch = dto.DataForSearch; settings.Version++; settings.UpdatedAt = DateTimeOffset.UtcNow; settings.Ip = context.Connection.RemoteIpAddress?.ToString(); settings.UserAgent = context.Request.Headers.UserAgent.ToString();
    await db.SaveChangesAsync(ct); return Results.Ok(ToConsentDto(settings));
}).RequireAuthorization();

api.MapGet("/clinics", async (string? specialty, string? city, string? tag, MedMatchDbContext db, CancellationToken ct) =>
{
    var clinics = await db.Clinics.AsNoTracking().ToListAsync(ct); var filtered = clinics.Where(x => string.IsNullOrWhiteSpace(specialty) || x.Specialty.Contains(specialty, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(city) || x.City.Contains(city, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(tag) || x.TreatmentsOffered.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase))).Select(ToClinicDto); return Results.Ok(filtered);
}).AllowAnonymous();
api.MapGet("/clinics/{id:guid}", async (Guid id, MedMatchDbContext db, CancellationToken ct) => { var clinic = await db.Clinics.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); return clinic is null ? Results.NotFound() : Results.Ok(ToClinicDto(clinic)); }).AllowAnonymous();
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

api.MapGet("/clinic/patients", async (string? diagnosis, string? symptom, MedMatchDbContext db, CancellationToken ct) =>
{
    var profiles = await db.PatientProfiles.Include(x => x.User).ThenInclude(x => x.ConsentSettings).Where(x => x.User.ConsentSettings!.ClinicsContactMe && x.User.ConsentSettings.DataForSearch).AsNoTracking().ToListAsync(ct);
    return Results.Ok(profiles.Where(x => string.IsNullOrWhiteSpace(diagnosis) || x.Diagnoses.Any(v => v.Contains(diagnosis, StringComparison.OrdinalIgnoreCase))).Where(x => string.IsNullOrWhiteSpace(symptom) || x.Symptoms.Any(v => v.Contains(symptom, StringComparison.OrdinalIgnoreCase))).Select(x => new { x.City, x.Country, x.Diagnoses, x.Interventions, x.Symptoms }));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic" });

api.MapGet("/people", async (string? diagnosis, string? symptom, string? city, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) =>
{
    var currentUserId = UserId(principal);
    var profiles = await db.PatientProfiles.Include(x => x.User).ThenInclude(x => x.ConsentSettings)
        .Where(x => x.UserId != currentUserId && x.User.ConsentSettings!.PatientsContactMe && x.User.ConsentSettings.DataForSearch)
        .AsNoTracking().ToListAsync(ct);
    var matches = profiles
        .Where(x => string.IsNullOrWhiteSpace(diagnosis) || x.Diagnoses.Any(v => v.Contains(diagnosis, StringComparison.OrdinalIgnoreCase)))
        .Where(x => string.IsNullOrWhiteSpace(symptom) || x.Symptoms.Any(v => v.Contains(symptom, StringComparison.OrdinalIgnoreCase)))
        .Where(x => string.IsNullOrWhiteSpace(city) || (x.City ?? "").Contains(city, StringComparison.OrdinalIgnoreCase))
        .Select(x => new PatientDirectoryDto(x.UserId, x.DisplayMode == DisplayMode.Pseudonym && !string.IsNullOrWhiteSpace(x.Pseudonym) ? x.Pseudonym! : x.DisplayMode == DisplayMode.RealName && !string.IsNullOrWhiteSpace(x.RealName) ? x.RealName! : "MedMatch member", x.City, x.Country, x.Diagnoses, x.Interventions, x.Symptoms, x.Bio, x.Languages));
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

app.Run();

string Required(string key) => builder.Configuration[key] ?? throw new InvalidOperationException($"{key} is required.");
static Guid UserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? throw new UnauthorizedAccessException());
static async Task<AuthResponse> IssueTokens(User user, MedMatchDbContext db, TokenService tokens, CancellationToken ct) { var (access, expires) = tokens.CreateAccessToken(user); var refresh = TokenService.CreateRefreshToken(); db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = TokenService.Hash(refresh), ExpiresAt = DateTimeOffset.UtcNow.AddDays(14) }); await db.SaveChangesAsync(ct); return new AuthResponse(access, refresh, expires, user.Role); }
static PatientProfileDto ToProfileDto(PatientProfile p) => new(p.DisplayMode, p.Pseudonym, p.RealName, p.City, p.Country, p.Diagnoses, p.Interventions, p.Symptoms, p.AgeRange, p.Bio, p.Languages);
static void ApplyProfile(PatientProfile p, PatientProfileDto d) { p.DisplayMode = d.DisplayMode; p.Pseudonym = d.Pseudonym; p.RealName = d.RealName; p.City = d.City; p.Country = d.Country; p.Diagnoses = d.Diagnoses ?? []; p.Interventions = d.Interventions ?? []; p.Symptoms = d.Symptoms ?? []; p.AgeRange = d.AgeRange; p.Bio = d.Bio; p.Languages = d.Languages ?? []; }
static ConsentSettingsDto ToConsentDto(ConsentSettings c) => new(c.ShowProfilePublicly, c.ClinicsContactMe, c.PatientsContactMe, c.DataForSearch, c.Version, c.UpdatedAt);
static ClinicDto ToClinicDto(Clinic c) => new(c.Id, c.Name, c.Type, c.Specialty, c.TreatmentsOffered, c.Address, c.City, c.Country, c.ContactInfo, c.IsVerified);
static void ApplyClinic(Clinic c, ClinicDto d) { c.Name = d.Name; c.Type = d.Type; c.Specialty = d.Specialty; c.TreatmentsOffered = d.TreatmentsOffered ?? []; c.Address = d.Address; c.City = d.City; c.Country = d.Country; c.ContactInfo = d.ContactInfo; c.IsVerified = d.IsVerified; }
static DoctorDto ToDoctorDto(Doctor d) => new(d.Id, d.ClinicId, d.Name, d.Specialty, d.TreatmentsOffered, d.City, d.Country, d.ContactInfo, d.IsVerified);
static void ApplyDoctor(Doctor d, DoctorDto dto) { d.ClinicId = dto.ClinicId; d.Name = dto.Name; d.Specialty = dto.Specialty; d.TreatmentsOffered = dto.TreatmentsOffered ?? []; d.City = dto.City; d.Country = dto.Country; d.ContactInfo = dto.ContactInfo; d.IsVerified = dto.IsVerified; }
static void ApplyReview(Review r, UpsertReviewRequest d) { r.ClinicId = d.ClinicId; r.DoctorId = d.DoctorId; r.Rating = d.Rating; r.Title = d.Title; r.Body = d.Body; r.Tags = d.Tags ?? []; r.IsAnonymous = d.IsAnonymous; r.AllowContactByPatients = d.AllowContactByPatients; r.AllowContactByClinics = d.AllowContactByClinics; r.UpdatedAt = DateTimeOffset.UtcNow; }
static IQueryable<Review> ReviewQuery(MedMatchDbContext db, Guid? clinicId, Guid? doctorId) { var query = db.Reviews.AsNoTracking().Include(x => x.AuthorUser).ThenInclude(x => x.PatientProfile).AsQueryable(); if (clinicId is not null) query = query.Where(x => x.ClinicId == clinicId); if (doctorId is not null) query = query.Where(x => x.DoctorId == doctorId); return query.OrderByDescending(x => x.CreatedAt); }
static ReviewDto ToReviewDto(Review review) { var p = review.AuthorUser.PatientProfile; var masked = review.IsAnonymous || p is null || p.DisplayMode == DisplayMode.Anonymous; var name = masked ? "Anonymous" : p!.DisplayMode == DisplayMode.Pseudonym && !string.IsNullOrWhiteSpace(p.Pseudonym) ? p.Pseudonym : !string.IsNullOrWhiteSpace(p!.RealName) ? p.RealName : "Patient"; return new(review.Id, review.ClinicId, review.DoctorId, review.Rating, review.Title, review.Body, review.Tags, masked, name, masked ? null : review.AuthorUserId, review.CreatedAt, review.UpdatedAt); }

static class Passwords
{
    public static string Hash(string password) { var salt = RandomNumberGenerator.GetBytes(16); var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210_000, HashAlgorithmName.SHA512, 32); return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}"; }
    public static bool Verify(string password, string value) { var parts = value.Split(':'); if (parts.Length != 2) return false; var salt = Convert.FromBase64String(parts[0]); var expected = Convert.FromBase64String(parts[1]); var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210_000, HashAlgorithmName.SHA512, 32); return CryptographicOperations.FixedTimeEquals(actual, expected); }
}
