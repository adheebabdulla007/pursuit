using Microsoft.EntityFrameworkCore;
using Pursuit.Infrastructure.Persistence;
using Pursuit.Infrastructure.Services;

namespace Pursuit.IntegrationTests.Infrastructure;

internal static class DatabaseSchemaProvisioner
{
    public static async Task MigrateAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new AppDbContext(options, new SystemDbContextScope());
        await context.Database.MigrateAsync(cancellationToken);
    }
}
