namespace MedMatch.Application.Contracts;

public sealed record ReviewDto(Guid Id, Guid? ClinicId, Guid? DoctorId, int Rating, string Title, string Body, string[] Tags, bool IsAnonymous, string AuthorDisplayName, Guid? AuthorUserId, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record UpsertReviewRequest(Guid? ClinicId, Guid? DoctorId, int Rating, string Title, string Body, string[] Tags, bool IsAnonymous, bool AllowContactByPatients, bool AllowContactByClinics);
