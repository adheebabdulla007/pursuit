using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pursuit.Application.Interfaces;
using Pursuit.Infrastructure.Caching;
using Pursuit.Infrastructure.Identity;
using Pursuit.Infrastructure.Messaging;
using Pursuit.Infrastructure.Persistence;
using Pursuit.Infrastructure.Persistence.Repositories;
using Pursuit.Infrastructure.Services;
using Pursuit.Infrastructure.Storage;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Pursuit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddHttpContextAccessor();
        services.AddScoped<IDbContextScope, HttpDbContextScope>();

        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();

        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();

        var redisConnectionString = configuration["RedisSettings:ConnectionString"]
            ?? throw new InvalidOperationException("RedisSettings:ConnectionString is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddSingleton<IConnection>(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMqSettings:Host"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMqSettings:Port"] ?? "5672"),
                UserName = configuration["RabbitMqSettings:Username"] ?? "guest",
                Password = configuration["RabbitMqSettings:Password"] ?? "guest"
            };

            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
        services.AddHostedService<ApplicationSubmittedConsumer>();
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        return services;
    }
}