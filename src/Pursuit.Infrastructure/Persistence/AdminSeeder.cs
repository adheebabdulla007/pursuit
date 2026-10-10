using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Domain.Enums;

namespace Pursuit.Infrastructure.Persistence;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("AdminSeeder");

        if (!configuration.GetValue<bool>("AdminBootstrapSettings:Enabled"))
        {
            logger.LogInformation("Administrator bootstrap is disabled.");
            return;
        }

        // The transaction-scoped advisory lock serializes bootstrap attempts.
        // Read committed lets a waiter observe the winner's committed admin row;
        // a serializable snapshot taken before the wait would be stale instead.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await AcquireBootstrapLockAsync(db);

        var adminExists = await userRepository.ExistsByRoleAsync(UserRole.Admin);

        if (adminExists)
        {
            await transaction.CommitAsync();
            logger.LogInformation("Admin user already exists, skipping seed.");
            return;
        }

        var email = configuration["AdminBootstrapSettings:Email"]!.ToLowerInvariant();
        var password = configuration["AdminBootstrapSettings:Password"]!;

        if (await userRepository.ExistsByEmailAsync(email))
            throw new InvalidOperationException("The administrator bootstrap email is already assigned to another account.");

        var admin = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "System",
            LastName = "Admin",
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            Role = UserRole.Admin,
            TenantId = null
        };

        await userRepository.AddAsync(admin);
        await transaction.CommitAsync();

        logger.LogInformation("Admin user seeded with email {Email}", email);
    }

    private static async Task AcquireBootstrapLockAsync(AppDbContext db)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandTimeout = 15;
        command.CommandText = "SELECT pg_advisory_xact_lock(5784682419472734785);";
        await command.ExecuteNonQueryAsync();
    }
}
