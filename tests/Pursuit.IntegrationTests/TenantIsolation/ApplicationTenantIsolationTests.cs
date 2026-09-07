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

    public ApplicationTenantIsolationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetByJob_CalledByAnotherTenantsEmployer_ReturnsEmptyList()
    {
        // Arrange: seed two tenants, two employers, one job under Tenant A, one application to it.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var tenantA = new Tenant { Id = Guid.NewGuid(), Name = "Tenant A", Slug = "tenant-a" };
        var tenantB = new Tenant { Id = Guid.NewGuid(), Name = "Tenant B", Slug = "tenant-b" };

        var employerA = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA.Id,
            Email = "employerA@test.com",
            PasswordHash = "x",
            FirstName = "Employer",
            LastName = "A",
            Role = UserRole.Employer
        };

        var employerB = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantB.Id,
            Email = "employerB@test.com",
            PasswordHash = "x",
            FirstName = "Employer",
            LastName = "B",
            Role = UserRole.Employer
        };

        var jobseeker = new User
        {
            Id = Guid.NewGuid(),
            TenantId = null,
            Email = "jobseeker@test.com",
            PasswordHash = "x",
            FirstName = "Job",
            LastName = "Seeker",
            Role = UserRole.JobSeeker
        };

        var jobA = new Job
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA.Id,
            Title = "Backend Developer",
            Description = "desc",
            Location = "Remote",
            SalaryMin = 1000,
            SalaryMax = 2000,
            JobType = JobType.FullTime,
            IsActive = true
        };

        var application = new Domain.Entities.Application
        {
            Id = Guid.NewGuid(),
            JobId = jobA.Id,
            TenantId = tenantA.Id,
            ApplicantId = jobseeker.Id,
            ResumeUrl = "resumes/test.pdf",
            Status = ApplicationStatus.Applied
        };

        db.Tenants.AddRange(tenantA, tenantB);
        db.Users.AddRange(employerA, employerB, jobseeker);
        db.Jobs.Add(jobA);
        db.Applications.Add(application);
        await db.SaveChangesAsync();

        var employerAToken = tokenService.GenerateToken(employerA);
        var employerBToken = tokenService.GenerateToken(employerB);

        using var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employerAToken);

        using var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employerBToken);

        // Act
        var responseA = await clientA.GetAsync($"/api/applications/job/{jobA.Id}");
        var responseB = await clientB.GetAsync($"/api/applications/job/{jobA.Id}");

        // Assert: the owning tenant sees the real application; the other tenant sees nothing.
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var applicationsForOwner = await responseA.Content.ReadFromJsonAsync<List<ApplicationDto>>(JsonOptions);
        applicationsForOwner.Should().ContainSingle(a => a.Id == application.Id);

        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var applicationsForOtherTenant = await responseB.Content.ReadFromJsonAsync<List<ApplicationDto>>(JsonOptions);
        applicationsForOtherTenant.Should().BeEmpty();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };
}