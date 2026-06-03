using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pursuit.Domain.Entities;

namespace Pursuit.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(j => j.Description)
            .IsRequired();

        builder.Property(j => j.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.SalaryMin)
            .HasColumnType("decimal(18,2)");

        builder.Property(j => j.SalaryMax)
            .HasColumnType("decimal(18,2)");

        builder.Property(j => j.JobType)
            .IsRequired();

        builder.Property(j => j.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(j => j.Tenant)
            .WithMany(t => t.Jobs)
            .HasForeignKey(j => j.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}