using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Infrastructure.Persistence;
using System.Security.Claims;
using System.Text;
using Pursuit.Application.Security;

namespace Pursuit.IntegrationTests.Auth;

[Collection("Integration Tests")]
public class AuthRevocationTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Deactivation_RejectsIssuedAccessAndRefreshTokens_AfterReactivationOldAccessStaysInvalid()
    {
        using var scope = factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var initial = await auth.RegisterAsync(NewUser());
        var id = await db.Users.Where(user => user.Email == initial.Email)
            .Select(user => user.Id).SingleAsync();

        (await MeAsync(initial.Token)).Should().Be(HttpStatusCode.OK);
        await SetStatusAsAdminAsync(id, false);
        (await MeAsync(initial.Token)).Should().Be(HttpStatusCode.Unauthorized);
        var refresh = () => auth.RefreshTokenAsync(initial.RefreshToken);
        await refresh.Should().ThrowAsync<UnauthorizedAccessException>();

        await SetStatusAsAdminAsync(id, true);
        (await MeAsync(initial.Token)).Should().Be(HttpStatusCode.Unauthorized);
        var replacement = await auth.LoginAsync(new LoginDto
        {
            Email = initial.Email, Password = "TestPassword123!"
        });
        (await MeAsync(replacement.Token)).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TenantSuspension_RejectsIssuedAccessAndRefreshTokens()
    {
        using var scope = factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var initial = await auth.RegisterAsync(new RegisterDto
        {
            FirstName = "Tenant", LastName = "Owner", Email = $"suspend-{Guid.NewGuid()}@test.com",
            Password = "TestPassword123!", Role = "Employer", TenantName = $"Tenant {Guid.NewGuid()}"
        });
        (await MeAsync(initial.Token)).Should().Be(HttpStatusCode.OK);
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(user => user.Email == initial.Email);
        var tenant = await db.Tenants.SingleAsync(value => value.Id == user.TenantId);
        tenant.IsActive = false;
        await db.SaveChangesAsync();

        (await MeAsync(initial.Token)).Should().Be(HttpStatusCode.Unauthorized);
        var refresh = () => auth.RefreshTokenAsync(initial.RefreshToken);
        await refresh.Should().ThrowAsync<UnauthorizedAccessException>();
        var login = () => auth.LoginAsync(new LoginDto
        {
            Email = initial.Email, Password = "TestPassword123!"
        });
        await login.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LogoutAfterRotation_RevokesThisSessionChain_ButNotOtherLogin()
    {
        using var scope = factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var first = await auth.RegisterAsync(NewUser());
        var second = await auth.LoginAsync(new LoginDto
        {
            Email = first.Email, Password = "TestPassword123!"
        });
        var successor = await auth.RefreshTokenAsync(first.RefreshToken);

        await auth.LogoutAsync(first.RefreshToken);
        (await MeAsync(first.Token)).Should().Be(HttpStatusCode.OK,
            "logout's already-issued access token remains valid only until its short expiry");
        using var verify = factory.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var hashes = new[] { first.RefreshToken, successor.RefreshToken, second.RefreshToken }
            .Select(value => verify.ServiceProvider.GetRequiredService<ITokenService>().HashToken(value)).ToArray();
        var tokens = await db.RefreshTokens.Where(token => hashes.Contains(token.TokenHash)).ToListAsync();
        tokens.Single(token => token.TokenHash == hashes[1]).IsRevoked.Should().BeTrue();
        tokens.Single(token => token.TokenHash == hashes[2]).IsRevoked.Should().BeFalse();
        var otherSession = await verify.ServiceProvider.GetRequiredService<IAuthService>()
            .RefreshTokenAsync(second.RefreshToken);
        otherSession.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ConcurrentRefreshAndDeactivation_LeaveNoRenewableSession()
    {
        using var setup = factory.Services.CreateScope();
        var initial = await setup.ServiceProvider.GetRequiredService<IAuthService>()
            .RegisterAsync(NewUser());
        var userId = await setup.ServiceProvider.GetRequiredService<AppDbContext>()
            .Users.Where(user => user.Email == initial.Email).Select(user => user.Id).SingleAsync();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshTask = Task.Run(async () =>
        {
            using var scope = factory.Services.CreateScope();
            await start.Task;
            try
            {
                return await scope.ServiceProvider.GetRequiredService<IAuthService>()
                    .RefreshTokenAsync(initial.RefreshToken);
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        });
        var deactivateTask = Task.Run(async () =>
        {
            await start.Task;
            await SetStatusAsAdminAsync(userId, false);
        });
        start.SetResult();
        var rotated = await refreshTask.WaitAsync(TimeSpan.FromSeconds(30));
        await deactivateTask.WaitAsync(TimeSpan.FromSeconds(30));

        using var verification = factory.Services.CreateScope();
        var db = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(value => value.Id == userId);
        user.IsActive.Should().BeFalse();
        (await db.RefreshTokens.Where(token => token.UserId == userId).ToListAsync())
            .Should().OnlyContain(token => token.IsRevoked);
        (await MeAsync(initial.Token)).Should().Be(HttpStatusCode.Unauthorized);
        if (rotated is not null)
            (await MeAsync(rotated.Token)).Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessToken_ExpiresWithinConfiguredFifteenMinutes()
    {
        using var scope = factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var issued = await auth.RegisterAsync(NewUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issued.Token);
        (jwt.ValidTo - DateTime.UtcNow).Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(15));

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var expired = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audiences.Single(),
            claims: jwt.Claims.Where(claim => claim.Type is not ("exp" or "nbf" or "iat")),
            expires: DateTime.UtcNow.AddSeconds(-2),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]!)),
                SecurityAlgorithms.HmacSha256));
        (await MeAsync(new JwtSecurityTokenHandler().WriteToken(expired)))
            .Should().Be(HttpStatusCode.Unauthorized, "JWT validation has zero clock skew");
    }

    [Fact]
    public void AccessTokenPolicy_RejectsLifetimeLongerThanRevocationWindow()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:ExpiryInMinutes"] = "60"
        }).Build();
        var act = () => AccessTokenPolicy.ReadLifetimeMinutes(configuration);
        act.Should().Throw<InvalidOperationException>();
    }

    private async Task<HttpStatusCode> MeAsync(string token)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.GetAsync("/api/auth/me");
        return response.StatusCode;
    }

    private async Task SetStatusAsAdminAsync(Guid userId, bool isActive)
    {
        using var scope = factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        try
        {
            accessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test-admin"))
            };
            await scope.ServiceProvider.GetRequiredService<IUserService>()
                .SetUserStatusAsync(userId, isActive);
        }
        finally
        {
            accessor.HttpContext = previous;
        }
    }

    private static RegisterDto NewUser() => new()
    {
        FirstName = "Revocation", LastName = "Test",
        Email = $"revocation-{Guid.NewGuid()}@test.com",
        Password = "TestPassword123!", Role = "JobSeeker"
    };
}
