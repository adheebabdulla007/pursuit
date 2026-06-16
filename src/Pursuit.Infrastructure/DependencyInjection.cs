using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pursuit.Application.Interfaces;
using Pursuit.Infrastructure.Identity;
using Pursuit.Infrastructure.Persistence;
using Pursuit.Infrastructure.Persistence.Repositories;
using Pursuit.Infrastructure.Services;

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

        return services;
    }
}