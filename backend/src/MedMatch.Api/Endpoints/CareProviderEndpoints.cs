using System.Security.Claims;
using MedMatch.Api.Observability;
using MedMatch.Application.Contracts;
using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using static MedMatch.Api.Mapping.DtoMapper;
using static MedMatch.Api.Queries.ReviewQueries;
using static MedMatch.Api.Security.UserIdentity;

namespace MedMatch.Api.Endpoints;

/// <summary>Care provider directory (clinics, doctors) and patient reviews.</summary>
public static class CareProviderEndpoints
{
    public static RouteGroupBuilder MapCareProviderEndpoints(this RouteGroupBuilder api)
    {
        // GET /api/clinics: lists published care providers, filtered by specialty, city, country or treatment tag.
        api.MapGet("/clinics", async (string? specialty, string? city, string? country, string? tag, MedMatchDbContext db, CancellationToken ct) =>
        {
            var clinics = await db.Clinics.AsNoTracking().Where(x => x.PublicationConsentGranted).ToListAsync(ct); var filtered = clinics.Where(x => string.IsNullOrWhiteSpace(specialty) || x.Specialty.Contains(specialty, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(city) || x.City.Contains(city, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(country) || x.Country.Contains(country, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(tag) || x.TreatmentsOffered.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase))).Select(ToClinicDto); return Results.Ok(filtered);
        }).AllowAnonymous();
        // GET /api/clinics/{id}: returns one published care provider.
        api.MapGet("/clinics/{id:guid}", async (Guid id, MedMatchDbContext db, CancellationToken ct) => { var clinic = await db.Clinics.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.PublicationConsentGranted, ct); return clinic is null ? Results.NotFound() : Results.Ok(ToClinicDto(clinic)); }).AllowAnonymous();
        // POST /api/clinics: creates a care provider (Clinic, Admin).
        api.MapPost("/clinics", async (ClinicDto dto, MedMatchDbContext db, CancellationToken ct) => { var clinic = new Clinic(); ApplyClinic(clinic, dto); db.Clinics.Add(clinic); await db.SaveChangesAsync(ct); return Results.Created($"/api/clinics/{clinic.Id}", ToClinicDto(clinic)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic,Admin" });
        // PUT /api/clinics/{id}: updates a care provider (Clinic, Admin).
        api.MapPut("/clinics/{id:guid}", async (Guid id, ClinicDto dto, MedMatchDbContext db, CancellationToken ct) => { var clinic = await db.Clinics.FindAsync([id], ct); if (clinic is null) return Results.NotFound(); ApplyClinic(clinic, dto); await db.SaveChangesAsync(ct); return Results.Ok(ToClinicDto(clinic)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic,Admin" });
        // DELETE /api/clinics/{id}: deletes a care provider (Clinic, Admin).
        api.MapDelete("/clinics/{id:guid}", async (Guid id, MedMatchDbContext db, CancellationToken ct) => { var clinic = await db.Clinics.FindAsync([id], ct); if (clinic is null) return Results.NotFound(); db.Clinics.Remove(clinic); await db.SaveChangesAsync(ct); return Results.NoContent(); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic,Admin" });

        // GET /api/doctors: lists doctors, filtered by specialty, city or treatment tag.
        api.MapGet("/doctors", async (string? specialty, string? city, string? tag, MedMatchDbContext db, CancellationToken ct) => { var doctors = await db.Doctors.AsNoTracking().ToListAsync(ct); return Results.Ok(doctors.Where(x => string.IsNullOrWhiteSpace(specialty) || x.Specialty.Contains(specialty, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(city) || (x.City ?? "").Contains(city, StringComparison.OrdinalIgnoreCase)).Where(x => string.IsNullOrWhiteSpace(tag) || x.TreatmentsOffered.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase))).Select(ToDoctorDto)); }).AllowAnonymous();
        // POST /api/doctors: creates a doctor (Clinic, Doctor, Admin).
        api.MapPost("/doctors", async (DoctorDto dto, MedMatchDbContext db, CancellationToken ct) => { var doctor = new Doctor(); ApplyDoctor(doctor, dto); db.Doctors.Add(doctor); await db.SaveChangesAsync(ct); return Results.Created($"/api/doctors/{doctor.Id}", ToDoctorDto(doctor)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic,Doctor,Admin" });
        // PUT /api/doctors/{id}: updates a doctor (Clinic, Doctor, Admin).
        api.MapPut("/doctors/{id:guid}", async (Guid id, DoctorDto dto, MedMatchDbContext db, CancellationToken ct) => { var doctor = await db.Doctors.FindAsync([id], ct); if (doctor is null) return Results.NotFound(); ApplyDoctor(doctor, dto); await db.SaveChangesAsync(ct); return Results.Ok(ToDoctorDto(doctor)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic,Doctor,Admin" });
        // DELETE /api/doctors/{id}: deletes a doctor (Clinic, Doctor, Admin).
        api.MapDelete("/doctors/{id:guid}", async (Guid id, MedMatchDbContext db, CancellationToken ct) => { var doctor = await db.Doctors.FindAsync([id], ct); if (doctor is null) return Results.NotFound(); db.Doctors.Remove(doctor); await db.SaveChangesAsync(ct); return Results.NoContent(); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Clinic,Doctor,Admin" });

        // GET /api/reviews: lists reviews, optionally for one clinic or doctor.
        api.MapGet("/reviews", async (Guid? clinicId, Guid? doctorId, MedMatchDbContext db, CancellationToken ct) => { var reviews = await ReviewQuery(db, clinicId, doctorId).ToListAsync(ct); return Results.Ok(reviews.Select(ToReviewDto)); }).AllowAnonymous();
        // POST /api/reviews: creates a 1-5 star review of a clinic or doctor (Patient).
        api.MapPost("/reviews", async (UpsertReviewRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) => { if (request.Rating is < 1 or > 5 || (request.ClinicId is null && request.DoctorId is null)) return Results.BadRequest(new { error = "Rating must be 1–5 and a clinic or doctor is required." }); var review = new Review { AuthorUserId = UserId(principal) }; ApplyReview(review, request); db.Reviews.Add(review); await db.SaveChangesAsync(ct); MedMatchMetrics.RecordReviewCreated(request.ClinicId is not null); var result = await ReviewQuery(db, null, null).SingleAsync(x => x.Id == review.Id, ct); return Results.Created($"/api/reviews/{review.Id}", ToReviewDto(result)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });
        // PUT /api/reviews/{id}: updates the caller's own review (Patient).
        api.MapPut("/reviews/{id:guid}", async (Guid id, UpsertReviewRequest request, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) => { var currentUserId = UserId(principal); var review = await db.Reviews.SingleOrDefaultAsync(x => x.Id == id && x.AuthorUserId == currentUserId, ct); if (review is null) return Results.NotFound(); if (request.Rating is < 1 or > 5) return Results.BadRequest(new { error = "Rating must be 1–5." }); ApplyReview(review, request); await db.SaveChangesAsync(ct); var result = await ReviewQuery(db, null, null).SingleAsync(x => x.Id == id, ct); return Results.Ok(ToReviewDto(result)); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient" });
        // DELETE /api/reviews/{id}: deletes the caller's own review (Patient, Admin).
        api.MapDelete("/reviews/{id:guid}", async (Guid id, ClaimsPrincipal principal, MedMatchDbContext db, CancellationToken ct) => { var currentUserId = UserId(principal); var review = await db.Reviews.SingleOrDefaultAsync(x => x.Id == id && x.AuthorUserId == currentUserId, ct); if (review is null) return Results.NotFound(); db.Reviews.Remove(review); await db.SaveChangesAsync(ct); return Results.NoContent(); }).RequireAuthorization(new AuthorizeAttribute { Roles = "Patient,Admin" });

        return api;
    }
}
