using MedMatch.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedMatch.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users"); builder.HasKey(x => x.Id); builder.Property(x => x.Email).HasMaxLength(320).IsRequired(); builder.HasIndex(x => x.Email).IsUnique(); builder.Property(x => x.PasswordHash).IsRequired();
        builder.HasMany(x => x.Roles).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.PatientProfile).WithOne(x => x.User).HasForeignKey<PatientProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ConsentSettings).WithOne(x => x.User).HasForeignKey<ConsentSettings>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
{
    public void Configure(EntityTypeBuilder<PatientProfile> builder)
    {
        builder.ToTable("patient_profiles"); builder.HasKey(x => x.UserId); builder.Property(x => x.DisplayMode).HasConversion<string>().HasMaxLength(20); builder.Property(x => x.Diagnoses).HasColumnType("text[]"); builder.Property(x => x.Interventions).HasColumnType("text[]"); builder.Property(x => x.Symptoms).HasColumnType("text[]"); builder.Property(x => x.Languages).HasColumnType("text[]");
    }
}
public sealed class ConsentSettingsConfiguration : IEntityTypeConfiguration<ConsentSettings>
{
    public void Configure(EntityTypeBuilder<ConsentSettings> builder) { builder.ToTable("consent_settings"); builder.HasKey(x => x.UserId); builder.Property(x => x.Ip).HasMaxLength(64); builder.Property(x => x.UserAgent).HasMaxLength(512); }
}
public sealed class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder) { builder.ToTable("clinics"); builder.HasKey(x => x.Id); builder.Property(x => x.Name).HasMaxLength(200).IsRequired(); builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40); builder.Property(x => x.Specialty).HasMaxLength(150).IsRequired(); builder.Property(x => x.City).HasMaxLength(120).IsRequired(); builder.Property(x => x.Country).HasMaxLength(120).IsRequired(); builder.Property(x => x.PublicWebsiteUrl).HasMaxLength(500); builder.Property(x => x.TreatmentsOffered).HasColumnType("text[]"); }
}
public sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder) { builder.ToTable("doctors"); builder.HasKey(x => x.Id); builder.Property(x => x.Name).HasMaxLength(200).IsRequired(); builder.Property(x => x.Specialty).HasMaxLength(150).IsRequired(); builder.Property(x => x.TreatmentsOffered).HasColumnType("text[]"); builder.HasOne(x => x.Clinic).WithMany(x => x.Doctors).HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.SetNull); }
}
public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews"); builder.HasKey(x => x.Id); builder.Property(x => x.Rating).IsRequired(); builder.HasCheckConstraint("CK_reviews_rating", "\"Rating\" BETWEEN 1 AND 5"); builder.Property(x => x.Title).HasMaxLength(200).IsRequired(); builder.Property(x => x.Body).IsRequired(); builder.Property(x => x.Tags).HasColumnType("text[]");
        builder.HasOne(x => x.AuthorUser).WithMany(x => x.Reviews).HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.Cascade); builder.HasOne(x => x.Clinic).WithMany(x => x.Reviews).HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.SetNull); builder.HasOne(x => x.Doctor).WithMany(x => x.Reviews).HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.SetNull);
    }
}
public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder) { builder.ToTable("messages"); builder.HasKey(x => x.Id); builder.Property(x => x.Content).IsRequired(); builder.Property(x => x.ConsentSnapshot).IsRequired(); builder.HasIndex(x => new { x.ThreadId, x.CreatedAt }); }
}
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder) { builder.ToTable("refresh_tokens"); builder.HasKey(x => x.Id); builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired(); builder.HasIndex(x => x.TokenHash).IsUnique(); builder.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder) { builder.ToTable("email_verification_tokens"); builder.HasKey(x => x.Id); builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired(); builder.HasIndex(x => x.TokenHash).IsUnique(); builder.HasOne(x => x.User).WithMany(x => x.EmailVerificationTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class DiagnosisTagConfiguration : IEntityTypeConfiguration<DiagnosisTag>
{
    public void Configure(EntityTypeBuilder<DiagnosisTag> builder) { builder.ToTable("diagnosis_tags"); builder.HasKey(x => x.Id); builder.Property(x => x.Name).HasMaxLength(200).IsRequired(); builder.Property(x => x.Slug).HasMaxLength(200).IsRequired(); builder.HasIndex(x => x.Slug).IsUnique(); builder.HasIndex(x => x.Name); }
}
public sealed class PatientDiagnosisTagConfiguration : IEntityTypeConfiguration<PatientDiagnosisTag>
{
    public void Configure(EntityTypeBuilder<PatientDiagnosisTag> builder)
    {
        builder.ToTable("patient_diagnosis_tags"); builder.HasKey(x => new { x.UserId, x.DiagnosisTagId });
        builder.HasOne(x => x.Patient).WithMany(x => x.DiagnosisTags).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.DiagnosisTag).WithMany(x => x.Patients).HasForeignKey(x => x.DiagnosisTagId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class MatchNotificationConfiguration : IEntityTypeConfiguration<MatchNotification>
{
    public void Configure(EntityTypeBuilder<MatchNotification> builder)
    {
        builder.ToTable("match_notifications"); builder.HasKey(x => x.Id); builder.Property(x => x.SharedDiagnoses).HasColumnType("text[]");
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.MatchedUserId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.IsRead });
    }
}
public sealed class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("recommendations"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Details).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ModerationNote).HasMaxLength(1000);
        builder.HasOne(x => x.AuthorUser).WithMany(x => x.Recommendations).HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
public sealed class RecommendationClinicConfiguration : IEntityTypeConfiguration<RecommendationClinic>
{
    public void Configure(EntityTypeBuilder<RecommendationClinic> builder)
    {
        builder.ToTable("recommendation_clinics"); builder.HasKey(x => new { x.RecommendationId, x.ClinicId });
        builder.HasOne(x => x.Recommendation).WithMany(x => x.Clinics).HasForeignKey(x => x.RecommendationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Clinic).WithMany(x => x.Recommendations).HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class RecommendationDiagnosisTagConfiguration : IEntityTypeConfiguration<RecommendationDiagnosisTag>
{
    public void Configure(EntityTypeBuilder<RecommendationDiagnosisTag> builder)
    {
        builder.ToTable("recommendation_diagnosis_tags"); builder.HasKey(x => new { x.RecommendationId, x.DiagnosisTagId });
        builder.HasOne(x => x.Recommendation).WithMany(x => x.DiagnosisTags).HasForeignKey(x => x.RecommendationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.DiagnosisTag).WithMany(x => x.Recommendations).HasForeignKey(x => x.DiagnosisTagId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ToTable("user_roles"); builder.HasKey(x => new { x.UserId, x.Role });
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(x => x.User).WithMany(x => x.Roles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.Role);
    }
}
