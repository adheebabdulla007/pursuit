using Microsoft.EntityFrameworkCore;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;

namespace Pursuit.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly IDbContextScope _scope;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IDbContextScope scope) : base(options)
    {
        _scope = scope;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Domain.Entities.Application> Applications => Set<Domain.Entities.Application>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Job>()
            .HasQueryFilter(j =>
                _scope.TenantId == null
                || j.TenantId == _scope.TenantId);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u =>
                u.TenantId == null
                || u.TenantId == _scope.TenantId);

        modelBuilder.Entity<Domain.Entities.Application>()
            .HasQueryFilter(a =>
                _scope.TenantId != null
                    ? a.TenantId == _scope.TenantId
                    : _scope.CurrentUserId == null
                        || a.ApplicantId == _scope.CurrentUserId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}