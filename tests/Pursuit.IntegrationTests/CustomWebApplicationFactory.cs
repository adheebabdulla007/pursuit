using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace Pursuit.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer =
    new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private readonly RedisContainer _redisContainer =
        new RedisBuilder("redis:7-alpine")
            .Build();

    private readonly RabbitMqContainer _rabbitMqContainer =
    new RabbitMqBuilder("rabbitmq:3-management-alpine")
        .WithUsername("pursuit_test")
        .WithPassword("pursuit_test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Intentionally empty: configuration overrides are applied via
        // environment variables in InitializeAsync, not here. AddInfrastructure
        // reads config eagerly before builder.Build() runs, which is before
        // ConfigureAppConfiguration overrides would ever take effect.
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await Task.WhenAll(
            _sqlContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _rabbitMqContainer.StartAsync());

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _sqlContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("RedisSettings__ConnectionString", _redisContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("RabbitMqSettings__Host", _rabbitMqContainer.Hostname);
        Environment.SetEnvironmentVariable("RabbitMqSettings__Port", _rabbitMqContainer.GetMappedPublicPort(5672).ToString());
        Environment.SetEnvironmentVariable("RabbitMqSettings__Username", "pursuit_test");
        Environment.SetEnvironmentVariable("RabbitMqSettings__Password", "pursuit_test");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(
            _sqlContainer.DisposeAsync().AsTask(),
            _redisContainer.DisposeAsync().AsTask(),
            _rabbitMqContainer.DisposeAsync().AsTask());
    }
}