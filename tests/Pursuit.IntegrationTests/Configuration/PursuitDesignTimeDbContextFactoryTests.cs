using FluentAssertions;
using Pursuit.API.Configuration;

namespace Pursuit.IntegrationTests.Configuration;

[CollectionDefinition(nameof(DesignTimeEnvironmentCollection), DisableParallelization = true)]
public sealed class DesignTimeEnvironmentCollection;

[Collection(nameof(DesignTimeEnvironmentCollection))]
public sealed class PursuitDesignTimeDbContextFactoryTests
{
    private const string ConnectionKey = "ConnectionStrings__DefaultConnection";

    [Fact]
    public void CreateDbContext_WithConnectionEnvironmentVariable_UsesSqlServerProvider()
    {
        var original = Environment.GetEnvironmentVariable(ConnectionKey);

        try
        {
            Environment.SetEnvironmentVariable(
                ConnectionKey,
                "Server=localhost,1433;Database=PursuitDesignTime;User Id=sa;Password=Marker-Password-1!;TrustServerCertificate=True;");

            using var context = new PursuitDesignTimeDbContextFactory().CreateDbContext([]);

            context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionKey, original);
        }
    }

    [Fact]
    public void CreateDbContext_WithoutConnectionEnvironmentVariable_ThrowsSafeConfigurationError()
    {
        var original = Environment.GetEnvironmentVariable(ConnectionKey);

        try
        {
            Environment.SetEnvironmentVariable(ConnectionKey, null);

            var action = () => new PursuitDesignTimeDbContextFactory().CreateDbContext([]);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("ConnectionStrings:DefaultConnection is required to create migration artifacts.")
                .Which.Message.Should().NotContain("Marker-Password-1!");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionKey, original);
        }
    }
}
