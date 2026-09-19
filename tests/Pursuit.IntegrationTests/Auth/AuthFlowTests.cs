using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Pursuit.Application.DTOs;
using Pursuit.Domain.Enums;
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
        var registerResponse = await PostAsJsonWithCsrfAsync(registerClient, "/api/auth/register", new RegisterDto
        {
            FirstName = "Job",
            LastName = "Seeker",
            Email = email,
            Password = password,
            Role = "JobSeeker"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var loginClient = _factory.CreateClient();
        var loginResponse = await PostAsJsonWithCsrfAsync(loginClient, "/api/auth/login", new LoginDto
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
        var registerResponse = await PostAsJsonWithCsrfAsync(registerClient, "/api/auth/register", new RegisterDto
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
        var loginResponse = await PostAsJsonWithCsrfAsync(loginClient, "/api/auth/login", new LoginDto
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
        var registerResponse = await PostAsJsonWithCsrfAsync(registerClient, "/api/auth/register", new RegisterDto
        {
            FirstName = "Wrong",
            LastName = "Pass",
            Email = email,
            Password = "CorrectPassword1!",
            Role = "JobSeeker"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var loginClient = _factory.CreateClient();
        var loginResponse = await PostAsJsonWithCsrfAsync(loginClient, "/api/auth/login", new LoginDto
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

        var registerResponse = await PostAsJsonWithCsrfAsync(browserClient, "/api/auth/register", new RegisterDto
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

        using var logoutRequest = await CreateCsrfRequestAsync(browserClient, HttpMethod.Post, "/api/auth/logout");
        var logoutResponse = await browserClient.SendAsync(logoutRequest);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        logoutResponse.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/api/auth/refresh/logout");

        using var replayClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        using var csrfResponse = await replayClient.GetAsync("/api/auth/csrf");
        var csrfToken = (await csrfResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        var csrfCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Single(header => header.StartsWith("pursuit_csrf=", StringComparison.Ordinal)).Split(';', 2)[0];
        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        replayRequest.Headers.Add("Cookie", $"{csrfCookie}; {refreshCookie}");
        replayRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var replayResponse = await replayClient.SendAsync(replayRequest);

        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithoutCsrfToken_IsRejectedBeforeAccountCreation()
    {
        using var client = _factory.CreateClient();
        var email = $"csrf-missing-{Guid.NewGuid()}@test.com";
        var registration = new RegisterDto
        {
            FirstName = "Missing", LastName = "Token", Email = email,
            Password = "Password123!", Role = "JobSeeker"
        };

        var denied = await client.PostAsJsonAsync("/api/auth/register", registration);
        denied.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var accepted = await PostAsJsonWithCsrfAsync(client, "/api/auth/register", registration);
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithValidTokenButUntrustedOrigin_IsRejected()
    {
        using var client = _factory.CreateClient();
        using var request = await CreateCsrfRequestAsync(client, HttpMethod.Post, "/api/auth/register");
        request.Headers.Add("Origin", "https://attacker.example");
        request.Content = JsonContent.Create(new RegisterDto
        {
            FirstName = "Untrusted", LastName = "Origin",
            Email = $"csrf-origin-{Guid.NewGuid()}@test.com",
            Password = "Password123!", Role = "JobSeeker"
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InvalidTokenAndMultipartWithoutToken_AreRejectedBeforeBinding()
    {
        using var client = _factory.CreateClient();
        using var invalid = await CreateCsrfRequestAsync(client, HttpMethod.Post, "/api/auth/register");
        invalid.Headers.Remove("X-CSRF-TOKEN");
        invalid.Headers.Add("X-CSRF-TOKEN", "invalid");
        invalid.Content = JsonContent.Create(new RegisterDto
        {
            FirstName = "Invalid", LastName = "Token",
            Email = $"csrf-invalid-{Guid.NewGuid()}@test.com",
            Password = "Password123!", Role = "JobSeeker"
        });
        (await client.SendAsync(invalid)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent([1, 2, 3]), "resume", "resume.pdf");
        multipart.Add(new StringContent(Guid.NewGuid().ToString()), "jobId");
        var response = await client.PostAsync("/api/applications", multipart);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message")
            .GetString().Should().Be("CSRF validation failed.");
    }

    [Fact]
    public async Task Register_WithCrossSiteFetchMetadataAndNoOrigin_IsRejected()
    {
        using var client = _factory.CreateClient();
        using var request = await CreateCsrfRequestAsync(client, HttpMethod.Post, "/api/auth/register");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Content = JsonContent.Create(new RegisterDto
        {
            FirstName = "Cross", LastName = "Site",
            Email = $"csrf-metadata-{Guid.NewGuid()}@test.com",
            Password = "Password123!", Role = "JobSeeker"
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BearerOnlyMutation_WorksButBearerWithAuthCookieCannotBypassCsrf()
    {
        using var browser = _factory.CreateClient();
        var registration = await PostAsJsonWithCsrfAsync(browser, "/api/auth/register", new RegisterDto
        {
            FirstName = "Bearer", LastName = "Employer",
            Email = $"csrf-bearer-{Guid.NewGuid()}@test.com",
            Password = "Password123!", Role = "Employer", TenantName = $"Co {Guid.NewGuid()}"
        });
        registration.StatusCode.Should().Be(HttpStatusCode.OK);
        var accessCookie = registration.Headers.GetValues("Set-Cookie")
            .Single(header => header.StartsWith("pursuit_token=", StringComparison.Ordinal)).Split(';', 2)[0];
        var token = accessCookie["pursuit_token=".Length..];
        var job = new CreateJobDto
        {
            Title = "CSRF boundary job", Description = "A real posting for the bearer test.",
            Location = "Remote", SalaryMin = 50000, SalaryMax = 80000, JobType = JobType.FullTime
        };

        using var api = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var bearerRequest = new HttpRequestMessage(HttpMethod.Post, "/api/jobs")
        {
            Content = JsonContent.Create(job)
        };
        bearerRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var accepted = await api.SendAsync(bearerRequest);
        accepted.StatusCode.Should().Be(HttpStatusCode.Created);

        using var cookieRequest = new HttpRequestMessage(HttpMethod.Post, "/api/jobs")
        {
            Content = JsonContent.Create(job)
        };
        cookieRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        cookieRequest.Headers.Add("Cookie", accessCookie);
        var denied = await api.SendAsync(cookieRequest);
        denied.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<HttpRequestMessage> CreateCsrfRequestAsync(
        HttpClient client, HttpMethod method, string path)
    {
        using var bootstrap = await client.GetAsync("/api/auth/csrf");
        bootstrap.EnsureSuccessStatusCode();
        var token = (await bootstrap.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", token);
        return request;
    }

    private static async Task<HttpResponseMessage> PostAsJsonWithCsrfAsync<T>(HttpClient client, string path, T body)
    {
        using var request = await CreateCsrfRequestAsync(client, HttpMethod.Post, path);
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }
}
