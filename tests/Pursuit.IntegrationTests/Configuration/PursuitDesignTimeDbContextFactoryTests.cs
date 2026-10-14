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
    public void CreateDbContext_WithConnectionEnvironmentVariable_UsesNpgsqlProvider()
    {
        var originalConnection = Environment.GetEnvironmentVariable(ConnectionKey);
        var originalEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable(
                ConnectionKey,
                "Host=localhost;Port=5432;Database=PursuitDesignTime;Username=postgres;Password=Marker-Password-1!;SSL Mode=Disable;GSS Encryption Mode=Disable;");

            using var context = new PursuitDesignTimeDbContextFactory().CreateDbContext([]);

            context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionKey, originalConnection);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalEnvironment);
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

    [Fact]
    public void CreateDbContext_RejectsExternalDatabaseEvenWhenDevelopmentIsSelected()
    {
        var originalConnection = Environment.GetEnvironmentVariable(ConnectionKey);
        var originalEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable(
                ConnectionKey,
                "Host=provider.example.com;Database=Pursuit;Username=pursuit;Password=Marker-Password-1!;SSL Mode=Disable;GSS Encryption Mode=Disable;");

            var action = () => new PursuitDesignTimeDbContextFactory().CreateDbContext([]);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*Use Pursuit.Migrations for deployment.*")
                .Which.Message.Should().NotContain("Marker-Password-1!");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionKey, originalConnection);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalEnvironment);
        }
    }

}
