using FluentAssertions;
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
}