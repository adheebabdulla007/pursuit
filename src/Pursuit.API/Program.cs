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
using Microsoft.AspNetCore.DataProtection;

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
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
        options.Cookie.Name = "pursuit_csrf";
        options.Cookie.HttpOnly = true;
        options.Cookie.Path = "/";
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction()
            ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    });

    var dataProtection = builder.Services.AddDataProtection().SetApplicationName("Pursuit");
    if (builder.Environment.IsProduction())
    {
        var keyRingPath = builder.Configuration["DataProtection:KeyRingPath"];
        if (string.IsNullOrWhiteSpace(keyRingPath) || !Path.IsPathFullyQualified(keyRingPath)
            || !Directory.Exists(keyRingPath))
            throw new InvalidOperationException("Production requires an existing absolute DataProtection:KeyRingPath.");
        dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
    }

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (!builder.Environment.IsProduction())
        allowedOrigins = [.. allowedOrigins, "http://localhost:5173", "http://localhost:5174"];
    allowedOrigins = allowedOrigins.Select(origin =>
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && (builder.Environment.IsProduction() || uri.Scheme != Uri.UriSchemeHttp))
            || uri.UserInfo.Length != 0 || uri.AbsolutePath != "/" || uri.Query.Length != 0 || uri.Fragment.Length != 0
            || !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Invalid Cors:AllowedOrigins entry: {origin}");
        return uri.GetLeftPart(UriPartial.Authority);
    }).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

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
            policy.WithOrigins(allowedOrigins)
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
    app.UseMiddleware<BrowserCsrfMiddleware>((object)allowedOrigins);
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
