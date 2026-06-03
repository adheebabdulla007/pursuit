using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pursuit.Domain.Entities;

namespace Pursuit.Infrastructure.Persistence.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ResumeUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Status)
            .IsRequired();

        builder.HasIndex(a => new { a.JobId, a.ApplicantId })
            .IsUnique();

        builder.HasOne(a => a.Job)
            .WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Applicant)
            .WithMany(u => u.Applications)
            .HasForeignKey(a => a.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}