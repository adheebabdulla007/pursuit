using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Pursuit.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Pursuit.IntegrationTests.Auth;

[Collection("Integration Tests")]
public class AuthFlowTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ThenLogin_JobSeeker_MeReturnsCorrectClaimsWithNoTenantId()
    {
        var email = $"jobseeker-{Guid.NewGuid()}@test.com";
        const string password = "Test123!";

        using var registerClient = _factory.CreateClient();
        var registerResponse = await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FirstName = "Job",
            LastName = "Seeker",
            Email = email,
            Password = password,
            Role = "JobSeeker"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var loginClient = _factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = password
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var meResponse = await loginClient.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        me.GetProperty("email").GetString().Should().Be(email);
        me.GetProperty("role").GetString().Should().Be("JobSeeker");
        me.TryGetProperty("tenantId", out var tenantIdProp).Should().BeTrue();
        tenantIdProp.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Register_ThenLogin_Employer_MeReturnsTenantIdClaimMatchingRegisteredTenant()
    {
        var email = $"employer-{Guid.NewGuid()}@test.com";
        const string password = "Test123!";
        var companyName = $"Test Co {Guid.NewGuid()}";

        using var registerClient = _factory.CreateClient();
        var registerResponse = await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FirstName = "Emp",
            LastName = "Loyer",
            Email = email,
            Password = password,
            Role = "Employer",
            TenantName = companyName
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var loginClient = _factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = password
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var meResponse = await loginClient.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        me.GetProperty("email").GetString().Should().Be(email);
        me.GetProperty("role").GetString().Should().Be("Employer");
        me.GetProperty("tenantId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var email = $"wrongpass-{Guid.NewGuid()}@test.com";

        using var registerClient = _factory.CreateClient();
        var registerResponse = await registerClient.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FirstName = "Wrong",
            LastName = "Pass",
            Email = email,
            Password = "CorrectPassword1!",
            Role = "JobSeeker"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var loginClient = _factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = "WrongPassword1!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_LegacyRoute_RedirectsAndRevokesRefreshToken()
    {
        var email = $"logout-{Guid.NewGuid()}@test.com";

        using var browserClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true
        });

        var registerResponse = await browserClient.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FirstName = "Logout",
            LastName = "Test",
            Email = email,
            Password = "TestPassword123!",
            Role = "JobSeeker"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshCookieHeader = registerResponse.Headers.GetValues("Set-Cookie")
            .Single(header => header.StartsWith("pursuit_refresh_token=", StringComparison.Ordinal));
        var refreshCookie = refreshCookieHeader.Split(';', 2)[0];

        var logoutResponse = await browserClient.PostAsync("/api/auth/logout", content: null);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        logoutResponse.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/api/auth/refresh/logout");

        using var replayClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        replayRequest.Headers.Add("Cookie", refreshCookie);

        var replayResponse = await replayClient.SendAsync(replayRequest);

        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
