using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pursuit.Infrastructure.Persistence;
using Pursuit.Infrastructure.Services;

namespace Pursuit.API.Configuration;

public sealed class PursuitDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ConnectionEnvironmentKey = "ConnectionStrings__DefaultConnection";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionEnvironmentKey);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required to create migration artifacts.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options, new SystemDbContextScope());
    }
}
