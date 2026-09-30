using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Pursuit.API.Configuration;

namespace Pursuit.IntegrationTests.Configuration;

public sealed class StartupConfigurationValidatorTests
{
    [Fact]
    public void Validate_accepts_complete_development_configuration_with_bootstrap_disabled()
    {
        var act = () => StartupConfigurationValidator.Validate(BuildConfiguration(), isProduction: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_reports_all_missing_required_settings_before_startup_side_effects()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminBootstrapSettings:Enabled"] = "false"
            })
            .Build();

        var act = () => StartupConfigurationValidator.Validate(configuration, isProduction: false);

        var exception = act.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("ConnectionStrings:DefaultConnection")
            .And.Contain("JwtSettings:Secret")
            .And.Contain("RedisSettings:ConnectionString")
            .And.Contain("RabbitMqSettings:Host")
            .And.Contain("AzureBlobSettings:ConnectionString");
    }

    [Fact]
    public void Validate_rejects_unsafe_production_service_settings()
    {
        var values = ValidValues();
        values["ConnectionStrings:DefaultConnection"] =
            "Server=database;Database=Pursuit;User Id=pursuit;Password=StrongDbPassword!;TrustServerCertificate=True;";
        values["RabbitMqSettings:Username"] = "guest";
        values["RabbitMqSettings:Password"] = "guest";
        values["AzureBlobSettings:ConnectionString"] = "UseDevelopmentStorage=true;";

        var act = () => StartupConfigurationValidator.Validate(BuildConfiguration(values), isProduction: true);

        var exception = act.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("validate the server certificate")
            .And.Contain("must not use the guest account")
            .And.Contain("must not use the development emulator");
    }

    [Fact]
    public void Validate_rejects_unencrypted_production_database_connection()
    {
        var values = ValidValues();
        values["ConnectionStrings:DefaultConnection"] =
            "Server=database;Database=Pursuit;User Id=pursuit;Password=StrongDbPassword!;Encrypt=False;TrustServerCertificate=False;";

        var act = () => StartupConfigurationValidator.Validate(BuildConfiguration(values), isProduction: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must encrypt the connection*");
    }

    [Fact]
    public void Validate_requires_strong_credentials_only_when_administrator_bootstrap_is_enabled()
    {
        var values = ValidValues();
        values["AdminBootstrapSettings:Enabled"] = "true";
        values["AdminBootstrapSettings:Email"] = "invalid";
        values["AdminBootstrapSettings:Password"] = "weak";

        var act = () => StartupConfigurationValidator.Validate(BuildConfiguration(values), isProduction: false);

        var exception = act.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("AdminBootstrapSettings:Email")
            .And.Contain("AdminBootstrapSettings:Password")
            .And.NotContain("weak");
    }

    [Fact]
    public void Validate_accepts_explicit_strong_administrator_bootstrap_credentials()
    {
        var values = ValidValues();
        values["AdminBootstrapSettings:Enabled"] = "true";
        values["AdminBootstrapSettings:Email"] = "admin@example.com";
        values["AdminBootstrapSettings:Password"] = "StrongAdmin1!";

        var act = () => StartupConfigurationValidator.Validate(BuildConfiguration(values), isProduction: true);

        act.Should().NotThrow();
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?>? values = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(values ?? ValidValues()).Build();

    private static Dictionary<string, string?> ValidValues() => new()
    {
        ["ConnectionStrings:DefaultConnection"] =
            "Server=database;Database=Pursuit;User Id=pursuit;Password=StrongDbPassword!;Encrypt=True;TrustServerCertificate=False;",
        ["JwtSettings:Secret"] = "unit-test-signing-secret-with-more-than-32-bytes",
        ["JwtSettings:Issuer"] = "Pursuit",
        ["JwtSettings:Audience"] = "PursuitUsers",
        ["JwtSettings:ExpiryInMinutes"] = "15",
        ["JwtSettings:RefreshTokenExpiryInDays"] = "7",
        ["RedisSettings:ConnectionString"] = "redis:6379",
        ["RabbitMqSettings:Host"] = "rabbitmq",
        ["RabbitMqSettings:Port"] = "5671",
        ["RabbitMqSettings:Username"] = "pursuit",
        ["RabbitMqSettings:Password"] = "strong-rabbit-password",
        ["AzureBlobSettings:ConnectionString"] =
            "DefaultEndpointsProtocol=https;AccountName=pursuit;AccountKey=ZmFrZS10ZXN0LWtleQ==;EndpointSuffix=core.windows.net",
        ["AzureBlobSettings:ContainerName"] = "resumes",
        ["AdminBootstrapSettings:Enabled"] = "false"
    };
}
