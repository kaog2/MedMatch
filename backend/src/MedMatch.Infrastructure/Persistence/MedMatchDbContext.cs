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
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<DiagnosisTag> DiagnosisTags => Set<DiagnosisTag>();
    public DbSet<DiagnosisTagTranslation> DiagnosisTagTranslations => Set<DiagnosisTagTranslation>();
    public DbSet<PatientDiagnosisTag> PatientDiagnosisTags => Set<PatientDiagnosisTag>();
    public DbSet<MatchNotification> MatchNotifications => Set<MatchNotification>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<RecommendationClinic> RecommendationClinics => Set<RecommendationClinic>();
    public DbSet<RecommendationDiagnosisTag> RecommendationDiagnosisTags => Set<RecommendationDiagnosisTag>();
    public DbSet<UserRoleAssignment> UserRoles => Set<UserRoleAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MedMatchDbContext).Assembly);
    }
}
