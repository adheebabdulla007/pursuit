using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Domain.Enums;
using Pursuit.Infrastructure.Persistence;
using Xunit;

namespace Pursuit.IntegrationTests.TenantIsolation;

[Collection("Integration Tests")]
public class ApplicationJobseekerIsolationTests
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    public ApplicationJobseekerIsolationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyApplications_CalledByAnotherJobseeker_ReturnsEmptyList()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Tenant A", Slug = "tenant-jobseeker-test" };

        var employer = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "employer-js@test.com",
            PasswordHash = "x",
            FirstName = "Employer",
            LastName = "JS",
            Role = UserRole.Employer
        };

        var jobseekerA = new User
        {
            Id = Guid.NewGuid(),
            TenantId = null,
            Email = "jobseekerA@test.com",
            PasswordHash = "x",
            FirstName = "Job",
            LastName = "SeekerA",
            Role = UserRole.JobSeeker
        };

        var jobseekerB = new User
        {
            Id = Guid.NewGuid(),
            TenantId = null,
            Email = "jobseekerB@test.com",
            PasswordHash = "x",
            FirstName = "Job",
            LastName = "SeekerB",
            Role = UserRole.JobSeeker
        };

        var job = new Job
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Title = "Frontend Developer",
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
            JobId = job.Id,
            TenantId = tenant.Id,
            ApplicantId = jobseekerA.Id,
            ResumeUrl = "resumes/test.pdf",
            Status = ApplicationStatus.Applied
        };

        db.Tenants.Add(tenant);
        db.Users.AddRange(employer, jobseekerA, jobseekerB);
        db.Jobs.Add(job);
        db.Applications.Add(application);
        await db.SaveChangesAsync();

        var jobseekerAToken = tokenService.GenerateToken(jobseekerA);
        var jobseekerBToken = tokenService.GenerateToken(jobseekerB);

        using var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jobseekerAToken);

        using var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jobseekerBToken);

        // Act
        var responseA = await clientA.GetAsync("/api/applications/my");
        var responseB = await clientB.GetAsync("/api/applications/my");

        // Assert
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var applicationsForOwner = await responseA.Content.ReadFromJsonAsync<List<ApplicationDto>>(JsonOptions);
        applicationsForOwner.Should().ContainSingle(a => a.Id == application.Id);

        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var applicationsForOtherJobseeker = await responseB.Content.ReadFromJsonAsync<List<ApplicationDto>>(JsonOptions);
        applicationsForOtherJobseeker.Should().BeEmpty();
    }
}