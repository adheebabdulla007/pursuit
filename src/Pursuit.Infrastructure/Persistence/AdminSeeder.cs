using Microsoft.Extensions.Configuration;
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
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("AdminSeeder");

        var adminExists = await userRepository.ExistsByRoleAsync(UserRole.Admin);

        if (adminExists)
        {
            logger.LogInformation("Admin user already exists, skipping seed.");
            return;
        }

        var email = configuration["AdminSeedSettings:Email"]!;
        var password = configuration["AdminSeedSettings:Password"]!;

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

        logger.LogInformation("Admin user seeded with email {Email}", email);
    }
}