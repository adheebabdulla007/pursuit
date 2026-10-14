using Microsoft.EntityFrameworkCore;
using Pursuit.Infrastructure.Persistence;
using Pursuit.Infrastructure.Services;
using Pursuit.Migrations;

const string connectionKey = "ConnectionStrings__DefaultConnection";

#if PURSUIT_ALLOW_INSECURE_LOCAL_MIGRATIONS
const bool allowInsecureLocalMigrations = true;
#else
const bool allowInsecureLocalMigrations = false;
#endif

return await MigrationRunner.RunAsync(
    Environment.GetEnvironmentVariable(connectionKey),
    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    allowInsecureLocalMigrations,
    async connectionString =>
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new AppDbContext(options, new SystemDbContextScope());
        await context.Database.MigrateAsync();
    },
    Console.Error);
