using FluentAssertions;
using Pursuit.Infrastructure.Persistence;

namespace Pursuit.IntegrationTests.Configuration;

public sealed class PostgreSqlConnectionSecurityPolicyTests
{
    [Theory]
    [InlineData("Host=database;Database=Pursuit;Username=pursuit;Password=secret;GSS Encryption Mode=Disable")]
    [InlineData("Host=database;Database=Pursuit;Username=pursuit;Password=secret;SSL Mode=Disable;GSS Encryption Mode=Disable")]
    [InlineData("Host=database;Database=Pursuit;Username=pursuit;Password=secret;SSL Mode=Prefer;GSS Encryption Mode=Disable")]
    [InlineData("Host=database;Database=Pursuit;Username=pursuit;Password=secret;SSL Mode=Require;GSS Encryption Mode=Disable")]
    [InlineData("Host=database;Database=Pursuit;Username=pursuit;Password=secret;SSL Mode=VerifyCA;GSS Encryption Mode=Disable")]
    [InlineData("Server=database;Database=Pursuit;UID=pursuit;Pwd=secret;SSLMode=Require;GSS Encryption Mode=Disable")]
    public void ValidateProduction_rejects_connections_without_hostname_verified_tls(string connectionString)
    {
        var action = () => PostgreSqlConnectionSecurityPolicy.ValidateProduction(connectionString);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Production PostgreSQL configuration must use SSL Mode=VerifyFull.");
    }

    [Fact]
    public void ValidateProduction_rejects_certificate_trust_bypass()
    {
        const string connectionString =
            "Host=database;Database=Pursuit;Username=pursuit;Password=secret;SSL Mode=VerifyFull;Trust Server Certificate=true;GSS Encryption Mode=Disable";

        var action = () => PostgreSqlConnectionSecurityPolicy.ValidateProduction(connectionString);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Production PostgreSQL configuration must validate the server certificate.");
    }

    [Fact]
    public void ValidateProduction_rejects_compact_certificate_trust_bypass_alias()
    {
        const string connectionString =
            "Host=database;Database=Pursuit;Username=pursuit;Password=secret;SSLMode=VerifyFull;TrustServerCertificate=true;GSS Encryption Mode=Disable";

        var action = () => PostgreSqlConnectionSecurityPolicy.ValidateProduction(connectionString);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Production PostgreSQL configuration must validate the server certificate.");
    }

    [Fact]
    public void ValidateProduction_accepts_hostname_verified_tls()
    {
        const string connectionString =
            "Host=database;Database=Pursuit;Username=pursuit;Password=secret;SSL Mode=VerifyFull;Trust Server Certificate=false;GSS Encryption Mode=Disable";

        var action = () => PostgreSqlConnectionSecurityPolicy.ValidateProduction(connectionString);

        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData(";GSS Encryption Mode=Prefer")]
    [InlineData(";GSS Encryption Mode=Require")]
    public void ValidateProduction_rejects_connections_that_can_prefer_gss_over_tls(string gssSetting)
    {
        var connectionString =
            "Host=database;Database=Pursuit;Username=pursuit;Password=secret;SSL Mode=VerifyFull" + gssSetting;

        var action = () => PostgreSqlConnectionSecurityPolicy.ValidateProduction(connectionString);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Production PostgreSQL configuration must use GSS Encryption Mode=Disable.");
    }

    [Fact]
    public void ValidateProduction_rejects_invalid_connection_without_echoing_secret()
    {
        const string connectionString = "not-a-connection-string;Password=Marker-Secret-1!";

        var action = () => PostgreSqlConnectionSecurityPolicy.ValidateProduction(connectionString);

        var exception = action.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Be("ConnectionStrings:DefaultConnection is not a valid PostgreSQL connection string.")
            .And.NotContain("Marker-Secret-1!");
    }

    [Theory]
    [InlineData("Development", "localhost")]
    [InlineData("Testing", "127.0.0.1")]
    [InlineData("Testing", "::1")]
    [InlineData("Development", "postgres")]
    [InlineData("Testing", "postgres-e2e")]
    public void PermitsExplicitLocalPlaintext_accepts_only_repo_local_hosts(
        string environment,
        string host)
    {
        var connectionString =
            $"Host={host};Database=Pursuit;Username=pursuit;Password=secret;SSL Mode=Disable;GSS Encryption Mode=Disable";

        PostgreSqlConnectionSecurityPolicy.PermitsExplicitLocalPlaintext(connectionString, environment)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("Production", "localhost", "Disable")]
    [InlineData(null, "localhost", "Disable")]
    [InlineData("Testing", "provider.example.com", "Disable")]
    [InlineData("Development", "localhost,provider.example.com", "Disable")]
    [InlineData("Testing", "localhost", "Prefer")]
    public void PermitsExplicitLocalPlaintext_rejects_nonlocal_or_nonexplicit_cases(
        string? environment,
        string host,
        string sslMode)
    {
        var connectionString =
            $"Host={host};Database=Pursuit;Username=pursuit;Password=secret;SSL Mode={sslMode}";

        PostgreSqlConnectionSecurityPolicy.PermitsExplicitLocalPlaintext(connectionString, environment)
            .Should().BeFalse();
    }

    [Fact]
    public void PermitsExplicitLocalPlaintext_rejects_implicit_gss_preference()
    {
        const string connectionString =
            "Host=localhost;Database=Pursuit;Username=pursuit;Password=secret;SSL Mode=Disable";

        PostgreSqlConnectionSecurityPolicy.PermitsExplicitLocalPlaintext(connectionString, "Development")
            .Should().BeFalse();
    }
}
