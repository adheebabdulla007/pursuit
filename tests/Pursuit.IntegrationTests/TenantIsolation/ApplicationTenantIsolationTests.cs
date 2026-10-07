using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Domain.Enums;
using Pursuit.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Pursuit.IntegrationTests.TenantIsolation;

[Collection("Integration Tests")]
public class ApplicationTenantIsolationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ApplicationTenantIsolationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetByJob_UsesPagedStableTenantScopedContract_AndDirectDetailDoesNotDisclose()
    {
        var marker = $"tenant-app-{Guid.NewGuid():N}";
        var createdAt = new DateTime(2026, 10, 6, 9, 0, 0, DateTimeKind.Utc);
        var higherId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2");
        var lowerId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
        var oldestId = Guid.NewGuid();
        const string privateResumeUrl = "resumes/private-owner-only.pdf";
        Guid jobId;
        string ownerToken;
        string otherToken;
        string companyName;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var ownerTenant = NewTenant($"{marker}-owner");
            var otherTenant = NewTenant($"{marker}-other");
            var owner = NewUser(UserRole.Employer, ownerTenant.Id, $"{marker}-owner");
            var other = NewUser(UserRole.Employer, otherTenant.Id, $"{marker}-other");
            var applicants = Enumerable.Range(1, 3)
                .Select(index => NewUser(UserRole.JobSeeker, null, $"{marker}-seeker-{index}"))
                .ToArray();
            var job = NewJob(ownerTenant.Id, $"{marker}-job");
            var higher = NewApplication(higherId, job.Id, ownerTenant.Id, applicants[0].Id, privateResumeUrl);
            var lower = NewApplication(lowerId, job.Id, ownerTenant.Id, applicants[1].Id, "resumes/lower.pdf");
            var oldest = NewApplication(oldestId, job.Id, ownerTenant.Id, applicants[2].Id, "resumes/oldest.pdf");

            db.Tenants.AddRange(ownerTenant, otherTenant);
            db.Users.AddRange([owner, other, .. applicants]);
            db.Jobs.Add(job);
            db.Applications.AddRange(higher, lower, oldest);
            await db.SaveChangesAsync();

            higher.CreatedAt = createdAt;
            lower.CreatedAt = createdAt;
            oldest.CreatedAt = createdAt.AddMinutes(-1);
            await db.SaveChangesAsync();

            jobId = job.Id;
            ownerToken = tokenService.GenerateToken(owner);
            otherToken = tokenService.GenerateToken(other);
            companyName = ownerTenant.Name;
        }

        using var ownerClient = AuthenticatedClient(ownerToken);
        using var otherClient = AuthenticatedClient(otherToken);
        var ownerFirstResponse = await ownerClient.GetAsync($"/api/applications/job/{jobId}?page=1&pageSize=2");
        var ownerSecond = await ownerClient.GetFromJsonAsync<PagedResult<ApplicationDto>>(
            $"/api/applications/job/{jobId}?page=2&pageSize=2", JsonOptions);
        var otherPage = await otherClient.GetFromJsonAsync<PagedResult<ApplicationDto>>(
            $"/api/applications/job/{jobId}?page=1&pageSize=10", JsonOptions);
        var ownerDetailResponse = await ownerClient.GetAsync($"/api/applications/{higherId}");
        var otherDetailResponse = await otherClient.GetAsync($"/api/applications/{higherId}");

        ownerFirstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ownerFirstJson = await ownerFirstResponse.Content.ReadAsStringAsync();
        var ownerFirst = JsonSerializer.Deserialize<PagedResult<ApplicationDto>>(ownerFirstJson, JsonOptions)!;
        ownerFirst.TotalCount.Should().Be(3);
        ownerFirst.Page.Should().Be(1);
        ownerFirst.PageSize.Should().Be(2);
        ownerFirst.Items.Select(application => application.Id).Should().Equal(higherId, lowerId);
        ownerFirst.Items.Should().OnlyContain(application => application.CompanyName == companyName);
        ownerSecond!.Items.Select(application => application.Id).Should().Equal(oldestId);
        ownerSecond.Items.Should().NotContain(application => ownerFirst.Items.Any(first => first.Id == application.Id));
        otherPage!.TotalCount.Should().Be(0);
        otherPage.Items.Should().BeEmpty();
        JsonDocument.Parse(ownerFirstJson).RootElement.GetProperty("items")[0]
            .TryGetProperty("resumeUrl", out _).Should().BeFalse();

        ownerDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        otherDetailResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var serviceScope = _factory.Services.CreateScope();
        var service = serviceScope.ServiceProvider.GetRequiredService<IApplicationService>();
        (await service.GetResumeStorageUrlAsync(higherId)).Should().Be(privateResumeUrl);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    [InlineData(int.MaxValue, 50)]
    public async Task GetByJob_WithInvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        var (token, jobId) = await SeedEmployerAndJobAsync();
        using var client = AuthenticatedClient(token);

        var response = await client.GetAsync(
            $"/api/applications/job/{jobId}?page={page}&pageSize={pageSize}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(string Token, Guid JobId)> SeedEmployerAndJobAsync()
    {
        var marker = $"tenant-bounds-{Guid.NewGuid():N}";
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var tenant = NewTenant(marker);
        var employer = NewUser(UserRole.Employer, tenant.Id, marker);
        var job = NewJob(tenant.Id, marker);
        db.Tenants.Add(tenant);
        db.Users.Add(employer);
        db.Jobs.Add(job);
        await db.SaveChangesAsync();
        return (tokenService.GenerateToken(employer), job.Id);
    }

    private HttpClient AuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static Tenant NewTenant(string marker) => new()
    {
        Id = Guid.NewGuid(), Name = $"Company {marker}", Slug = marker
    };

    private static User NewUser(UserRole role, Guid? tenantId, string marker) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, Email = $"{marker}@test.com", PasswordHash = "x",
        FirstName = role.ToString(), LastName = "User", Role = role
    };

    private static Job NewJob(Guid tenantId, string title) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, Title = title, Description = "desc", Location = "Remote",
        SalaryMin = 1_000, SalaryMax = 2_000, JobType = JobType.FullTime, IsActive = true
    };

    private static Domain.Entities.Application NewApplication(
        Guid id, Guid jobId, Guid tenantId, Guid applicantId, string resumeUrl) => new()
    {
        Id = id, JobId = jobId, TenantId = tenantId, ApplicantId = applicantId,
        ResumeUrl = resumeUrl, Status = ApplicationStatus.Applied
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }, PropertyNameCaseInsensitive = true
    };
}
