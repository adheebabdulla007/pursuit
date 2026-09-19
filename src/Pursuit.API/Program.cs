using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Pursuit.API.Filters;
using Pursuit.API.Middleware;
using Pursuit.Application;
using Pursuit.Infrastructure;
using Serilog;
using System.Text;
using Pursuit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pursuit.Application.Interfaces;
using Pursuit.Application.Security;
using Pursuit.Domain.Enums;
using System.Security.Claims;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Pursuit API");

    var builder = WebApplication.CreateBuilder(args);
    AccessTokenPolicy.ReadLifetimeMinutes(builder.Configuration);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
                     .ReadFrom.Services(services)
                     .Enrich.FromLogContext());

    // Add services to the container
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddOpenApi();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddHealthChecks();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["JwtSettings:Secret"]!))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("pursuit_token", out var token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var principal = context.Principal!;
                var tenantClaim = principal.FindFirst("tenantId")?.Value;
                Guid? tenantId = null;
                if (tenantClaim is not null)
                {
                    if (!Guid.TryParse(tenantClaim, out var parsedTenant))
                    {
                        context.Fail("Invalid account state.");
                        return;
                    }
                    tenantId = parsedTenant;
                }
                if (!Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
                    || !Guid.TryParse(principal.FindFirst(AccessTokenPolicy.SecurityVersionClaim)?.Value, out var version)
                    || !Enum.TryParse<UserRole>(principal.FindFirst(ClaimTypes.Role)?.Value, out var role)
                    || !Enum.IsDefined(role)
                    || !await context.HttpContext.RequestServices.GetRequiredService<IUserRepository>()
                        .IsAuthenticationAllowedAsync(userId, version, role, tenantId, context.HttpContext.RequestAborted))
                    context.Fail("Invalid account state.");
            }
        };
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowReactDev", policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    await AdminSeeder.SeedAsync(app.Services);

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseCors("AllowReactDev");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Pursuit API failed to start");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program
{
    protected Program()
    { }
}
