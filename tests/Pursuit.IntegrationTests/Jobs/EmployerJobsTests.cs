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

namespace Pursuit.IntegrationTests.Jobs;

[Collection("Integration Tests")]
public class EmployerJobsTests
{
    private readonly CustomWebApplicationFactory _factory;

    public EmployerJobsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Mine_AnonymousUser_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/jobs/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Mine_JobSeeker_ReturnsForbidden()
    {
        var token = await SeedUserAsync(UserRole.JobSeeker);
        using var client = AuthenticatedClient(token);

        var response = await client.GetAsync("/api/jobs/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public async Task Mine_WithInvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        var token = await SeedUserAsync(UserRole.Employer);
        using var client = AuthenticatedClient(token);

        var response = await client.GetAsync($"/api/jobs/mine?page={page}&pageSize={pageSize}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Mine_ReturnsOnlyCurrentTenant_WithStatusFiltersAndStablePaging()
    {
        var marker = $"mine-{Guid.NewGuid():N}";
        var createdAt = new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc);
        var higherOpenId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee2");
        var lowerOpenId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1");
        var oldestOpenId = Guid.NewGuid();
        var closedId = Guid.NewGuid();
        string employerToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var tenant = NewTenant($"{marker}-owner");
            var otherTenant = NewTenant($"{marker}-other");
            var employer = NewUser(UserRole.Employer, tenant.Id, marker);
            var higherOpen = NewJob(higherOpenId, tenant.Id, $"{marker}-higher", isActive: true);
            var lowerOpen = NewJob(lowerOpenId, tenant.Id, $"{marker}-lower", isActive: true);
            var oldestOpen = NewJob(oldestOpenId, tenant.Id, $"{marker}-oldest", isActive: true);
            var closed = NewJob(closedId, tenant.Id, $"{marker}-closed", isActive: false);
            var otherTenantJob = NewJob(Guid.NewGuid(), otherTenant.Id, $"{marker}-foreign", isActive: true);

            db.Tenants.AddRange(tenant, otherTenant);
            db.Users.Add(employer);
            db.Jobs.AddRange(higherOpen, lowerOpen, oldestOpen, closed, otherTenantJob);
            await db.SaveChangesAsync();

            higherOpen.CreatedAt = createdAt;
            lowerOpen.CreatedAt = createdAt;
            oldestOpen.CreatedAt = createdAt.AddMinutes(-1);
            closed.CreatedAt = createdAt.AddMinutes(1);
            otherTenantJob.CreatedAt = createdAt.AddMinutes(2);
            await db.SaveChangesAsync();

            employerToken = tokenService.GenerateToken(employer);
        }

        using var client = AuthenticatedClient(employerToken);
        var firstPage = await GetMineAsync(client, isActive: null, page: 1, pageSize: 2);
        var secondPage = await GetMineAsync(client, isActive: null, page: 2, pageSize: 2);
        var openFirstPage = await GetMineAsync(client, isActive: true, page: 1, pageSize: 2);
        var openSecondPage = await GetMineAsync(client, isActive: true, page: 2, pageSize: 2);
        var closedPage = await GetMineAsync(client, isActive: false, page: 1, pageSize: 10);

        firstPage.TotalCount.Should().Be(4);
        firstPage.Page.Should().Be(1);
        firstPage.PageSize.Should().Be(2);
        firstPage.Items.Select(job => job.Id).Should().Equal(closedId, higherOpenId);
        secondPage.Items.Select(job => job.Id).Should().Equal(lowerOpenId, oldestOpenId);
        secondPage.Items.Should().NotContain(job => firstPage.Items.Any(first => first.Id == job.Id));

        openFirstPage.TotalCount.Should().Be(3);
        openFirstPage.Items.Select(job => job.Id).Should().Equal(higherOpenId, lowerOpenId);
        openSecondPage.Items.Select(job => job.Id).Should().Equal(oldestOpenId);

        closedPage.TotalCount.Should().Be(1);
        closedPage.Items.Select(job => job.Id).Should().Equal(closedId);
        firstPage.Items.Concat(secondPage.Items).Should().OnlyContain(job => job.Title.StartsWith(marker));
    }

    private async Task<string> SeedUserAsync(UserRole role)
    {
        var marker = $"mine-user-{Guid.NewGuid():N}";
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        Tenant? tenant = null;

        if (role == UserRole.Employer)
        {
            tenant = NewTenant(marker);
            db.Tenants.Add(tenant);
        }

        var user = NewUser(role, tenant?.Id, marker);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return tokenService.GenerateToken(user);
    }

    private HttpClient AuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<PagedResult<JobDto>> GetMineAsync(
        HttpClient client,
        bool? isActive,
        int page,
        int pageSize)
    {
        var filter = isActive.HasValue ? $"&isActive={isActive.Value.ToString().ToLowerInvariant()}" : string.Empty;
        var response = await client.GetAsync($"/api/jobs/mine?page={page}&pageSize={pageSize}{filter}");
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PagedResult<JobDto>>(JsonOptions))!;
    }

    private static Tenant NewTenant(string marker) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"Employer Jobs {marker}",
        Slug = marker
    };

    private static User NewUser(UserRole role, Guid? tenantId, string marker) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Email = $"{marker}-{role}@test.com",
        PasswordHash = "x",
        FirstName = "Employer",
        LastName = "Jobs",
        Role = role
    };

    private static Job NewJob(Guid id, Guid tenantId, string title, bool isActive) => new()
    {
        Id = id,
        TenantId = tenantId,
        Title = title,
        Description = "Employer jobs contract test.",
        Location = "Remote",
        SalaryMin = 60_000,
        SalaryMax = 90_000,
        JobType = JobType.FullTime,
        IsActive = isActive
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };
}
