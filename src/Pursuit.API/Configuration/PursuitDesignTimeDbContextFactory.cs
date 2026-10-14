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

        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (!PostgreSqlConnectionSecurityPolicy.PermitsExplicitLocalPlaintext(connectionString, environment))
            throw new InvalidOperationException(
                "Design-time EF commands support only local Development or Testing databases. Use Pursuit.Migrations for deployment.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options, new SystemDbContextScope());
    }
}
