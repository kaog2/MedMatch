using MedMatch.Domain;
using Microsoft.EntityFrameworkCore;

namespace MedMatch.Infrastructure.Persistence;

public sealed class MedMatchDbContext(DbContextOptions<MedMatchDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<ConsentSettings> ConsentSettings => Set<ConsentSettings>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MedMatchDbContext).Assembly);
    }
}
