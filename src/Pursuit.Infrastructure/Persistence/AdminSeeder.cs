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

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
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
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = N'Pursuit.AdminBootstrap',
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = 15000;
            SELECT @result;
            """;

        var result = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (result < 0)
            throw new InvalidOperationException("Could not acquire the administrator bootstrap lock.");
    }
}
