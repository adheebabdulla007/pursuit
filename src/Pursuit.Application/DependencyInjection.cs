using Microsoft.Extensions.DependencyInjection;
using Pursuit.Application.Interfaces;
using Pursuit.Application.Services;
using FluentValidation;

namespace Pursuit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IJobService, JobService>();

        services.AddScoped<IApplicationService, ApplicationService>();

        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IUserService, UserService>();

        services.AddValidatorsFromAssemblyContaining<IJobService>();

        return services;
    }
}