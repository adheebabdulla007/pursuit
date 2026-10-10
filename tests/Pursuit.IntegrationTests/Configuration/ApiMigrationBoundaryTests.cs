using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Pursuit.IntegrationTests.Infrastructure;
using Testcontainers.PostgreSql;

namespace Pursuit.IntegrationTests.Configuration;

[Collection(nameof(DesignTimeEnvironmentCollection))]
public sealed class ApiMigrationBoundaryTests
{
    [Fact]
    public async Task ApiStartup_WithEmptyDatabaseAndBootstrapDisabled_DoesNotCreateSchema()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await postgres.StartAsync();
        var settings = ProductionLikeSettings(postgres.GetConnectionString());
        var originals = settings.ToDictionary(
            setting => setting.Key,
            setting => Environment.GetEnvironmentVariable(setting.Key));

        try
        {
            foreach (var setting in settings)
                Environment.SetEnvironmentVariable(setting.Key, setting.Value);

            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services => services.RemoveAll<IHostedService>()));
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await using var connection = new NpgsqlConnection(postgres.GetConnectionString());
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT to_regclass('public.\"__EFMigrationsHistory\"')::text";
            var historyTable = await command.ExecuteScalarAsync();
            Assert.True(historyTable is null or DBNull);
        }
        finally
        {
            foreach (var setting in originals)
                Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
    }

    [Fact]
    public async Task DatabaseSchemaProvisioner_MigrateTwice_IsIdempotent()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await postgres.StartAsync();

        await DatabaseSchemaProvisioner.MigrateAsync(postgres.GetConnectionString());
        await DatabaseSchemaProvisioner.MigrateAsync(postgres.GetConnectionString());

        await using var connection = new NpgsqlConnection(postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\"";
        var migrationCount = (long)(await command.ExecuteScalarAsync())!;
        migrationCount.Should().BeGreaterThan(0);
    }

    private static Dictionary<string, string> ProductionLikeSettings(string connectionString) => new()
    {
        ["ConnectionStrings__DefaultConnection"] = connectionString,
        ["JwtSettings__Secret"] = "migration-boundary-test-secret-at-least-32-bytes",
        ["JwtSettings__Issuer"] = "Pursuit",
        ["JwtSettings__Audience"] = "PursuitUsers",
        ["JwtSettings__ExpiryInMinutes"] = "15",
        ["JwtSettings__RefreshTokenExpiryInDays"] = "7",
        ["RedisSettings__ConnectionString"] = "127.0.0.1:1,connectTimeout=100,connectRetry=0",
        ["RabbitMqSettings__Host"] = "127.0.0.1",
        ["RabbitMqSettings__Port"] = "1",
        ["RabbitMqSettings__Username"] = "test",
        ["RabbitMqSettings__Password"] = "test",
        ["AzureBlobSettings__ConnectionString"] = "UseDevelopmentStorage=true",
        ["AzureBlobSettings__ContainerName"] = "resumes",
        ["AdminBootstrapSettings__Enabled"] = "false"
    };
}
