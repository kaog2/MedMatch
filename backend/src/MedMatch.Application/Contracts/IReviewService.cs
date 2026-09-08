namespace MedMatch.Application.Contracts;

public interface IReviewService
{
    Task<IReadOnlyList<ReviewDto>> GetAsync(Guid? clinicId, Guid? doctorId, CancellationToken cancellationToken);
    Task<ReviewDto> CreateAsync(Guid authorUserId, UpsertReviewRequest request, CancellationToken cancellationToken);
}
