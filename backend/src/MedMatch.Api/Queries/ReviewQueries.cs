using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedMatch.Api.Queries;

public static class ReviewQueries
{
    public static IQueryable<Review> ReviewQuery(MedMatchDbContext db, Guid? clinicId, Guid? doctorId)
    {
        var query = db.Reviews.AsNoTracking().Include(x => x.AuthorUser).ThenInclude(x => x.PatientProfile).AsQueryable();
        if (clinicId is not null) query = query.Where(x => x.ClinicId == clinicId);
        if (doctorId is not null) query = query.Where(x => x.DoctorId == doctorId);
        return query.OrderByDescending(x => x.CreatedAt);
    }
}
