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
public class ApplicationJobseekerIsolationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ApplicationJobseekerIsolationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMyApplications_UsesPagedStableApplicantScopedContract()
    {
        var marker = $"seeker-app-{Guid.NewGuid():N}";
        var createdAt = new DateTime(2026, 10, 6, 10, 0, 0, DateTimeKind.Utc);
        var higherId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
        var lowerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var oldestId = Guid.NewGuid();
        string seekerAToken;
        string seekerBToken;
        string companyName;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var tenant = NewTenant(marker);
            var seekerA = NewSeeker($"{marker}-a");
            var seekerB = NewSeeker($"{marker}-b");
            var jobs = Enumerable.Range(1, 4).Select(index => NewJob(tenant.Id, $"{marker}-{index}")).ToArray();
            var higher = NewApplication(higherId, jobs[0], tenant.Id, seekerA.Id);
            var lower = NewApplication(lowerId, jobs[1], tenant.Id, seekerA.Id);
            var oldest = NewApplication(oldestId, jobs[2], tenant.Id, seekerA.Id);
            var seekerBOwn = NewApplication(Guid.NewGuid(), jobs[3], tenant.Id, seekerB.Id);

            db.Tenants.Add(tenant);
            db.Users.AddRange(seekerA, seekerB);
            db.Jobs.AddRange(jobs);
            db.Applications.AddRange(higher, lower, oldest, seekerBOwn);
            await db.SaveChangesAsync();

            higher.CreatedAt = createdAt;
            lower.CreatedAt = createdAt;
            oldest.CreatedAt = createdAt.AddMinutes(-1);
            seekerBOwn.CreatedAt = createdAt.AddMinutes(1);
            await db.SaveChangesAsync();

            seekerAToken = tokenService.GenerateToken(seekerA);
            seekerBToken = tokenService.GenerateToken(seekerB);
            companyName = tenant.Name;
        }

        using var clientA = AuthenticatedClient(seekerAToken);
        using var clientB = AuthenticatedClient(seekerBToken);
        var firstResponse = await clientA.GetAsync("/api/applications/my?page=1&pageSize=2");
        var second = await clientA.GetFromJsonAsync<PagedResult<ApplicationDto>>(
            "/api/applications/my?page=2&pageSize=2", JsonOptions);
        var seekerBPage = await clientB.GetFromJsonAsync<PagedResult<ApplicationDto>>(
            "/api/applications/my?page=1&pageSize=10", JsonOptions);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstJson = await firstResponse.Content.ReadAsStringAsync();
        var first = JsonSerializer.Deserialize<PagedResult<ApplicationDto>>(firstJson, JsonOptions)!;
        first.TotalCount.Should().Be(3);
        first.Items.Select(application => application.Id).Should().Equal(higherId, lowerId);
        first.Items.Should().OnlyContain(application => application.CompanyName == companyName);
        second!.Items.Select(application => application.Id).Should().Equal(oldestId);
        seekerBPage!.TotalCount.Should().Be(1);
        seekerBPage.Items.Should().NotContain(application =>
            first.Items.Any(ownerApplication => ownerApplication.Id == application.Id)
            || second.Items.Any(ownerApplication => ownerApplication.Id == application.Id));
        JsonDocument.Parse(firstJson).RootElement.GetProperty("items")[0]
            .TryGetProperty("resumeUrl", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    [InlineData(int.MaxValue, 50)]
    public async Task GetMyApplications_WithInvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        var token = await SeedSeekerAsync();
        using var client = AuthenticatedClient(token);

        var response = await client.GetAsync($"/api/applications/my?page={page}&pageSize={pageSize}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<string> SeedSeekerAsync()
    {
        var seeker = NewSeeker($"seeker-bounds-{Guid.NewGuid():N}");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        db.Users.Add(seeker);
        await db.SaveChangesAsync();
        return tokenService.GenerateToken(seeker);
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

    private static User NewSeeker(string marker) => new()
    {
        Id = Guid.NewGuid(), Email = $"{marker}@test.com", PasswordHash = "x",
        FirstName = "Job", LastName = "Seeker", Role = UserRole.JobSeeker
    };

    private static Job NewJob(Guid tenantId, string title) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, Title = title, Description = "desc", Location = "Remote",
        SalaryMin = 1_000, SalaryMax = 2_000, JobType = JobType.FullTime, IsActive = true
    };

    private static Domain.Entities.Application NewApplication(
        Guid id, Job job, Guid tenantId, Guid applicantId) => new()
    {
        Id = id, JobId = job.Id, TenantId = tenantId, ApplicantId = applicantId,
        ResumeUrl = $"resumes/{id}.pdf", Status = ApplicationStatus.Applied
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }, PropertyNameCaseInsensitive = true
    };
}
