using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
public class JobSearchPaginationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public JobSearchPaginationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public async Task Search_WithInvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/jobs?page={page}&pageSize={pageSize}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_WithEqualCreationTimes_UsesDescendingIdTieBreakerAcrossPages()
    {
        var marker = $"stable-{Guid.NewGuid():N}";
        var createdAt = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
        var lowerId = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff1");
        var higherId = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff2");
        var oldestId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tenant = NewTenant(marker);
            var lower = NewJob(lowerId, tenant.Id, $"{marker}-lower");
            var higher = NewJob(higherId, tenant.Id, $"{marker}-higher");
            var oldest = NewJob(oldestId, tenant.Id, $"{marker}-oldest");

            db.Tenants.Add(tenant);
            db.Jobs.AddRange(lower, higher, oldest);
            await db.SaveChangesAsync();

            lower.CreatedAt = createdAt;
            higher.CreatedAt = createdAt;
            oldest.CreatedAt = createdAt.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        var firstPage = await GetPageAsync(client, marker, page: 1, pageSize: 2);
        var secondPage = await GetPageAsync(client, marker, page: 2, pageSize: 2);

        firstPage.TotalCount.Should().Be(3);
        firstPage.Items.Select(job => job.Id).Should().Equal(higherId, lowerId);
        secondPage.Items.Select(job => job.Id).Should().Equal(oldestId);
        secondPage.Items.Should().NotContain(job => firstPage.Items.Any(first => first.Id == job.Id));
    }

    [Fact]
    public async Task Search_ExcludesClosedJobFromItemsAndCount_ButDirectLookupStillReturnsIt()
    {
        var marker = $"closed-{Guid.NewGuid():N}";
        var closedId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tenant = NewTenant(marker);
            db.Tenants.Add(tenant);
            db.Jobs.Add(NewJob(closedId, tenant.Id, marker, isActive: false));
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        var search = await GetPageAsync(client, marker, page: 1, pageSize: 10);
        var directResponse = await client.GetAsync($"/api/jobs/{closedId}");

        search.TotalCount.Should().Be(0);
        search.Items.Should().BeEmpty();
        directResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var direct = await directResponse.Content.ReadFromJsonAsync<JobDto>(JsonOptions);
        direct.Should().NotBeNull();
        direct!.Id.Should().Be(closedId);
        direct.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task AdminStats_TotalJobsStillIncludesClosedJobs()
    {
        var marker = $"admin-count-{Guid.NewGuid():N}";
        string token;
        int expectedTotalJobs;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var tenant = NewTenant(marker);
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = $"{marker}@test.com",
                PasswordHash = "x",
                FirstName = "Admin",
                LastName = "Counter",
                Role = UserRole.Admin
            };

            db.Tenants.Add(tenant);
            db.Users.Add(admin);
            db.Jobs.Add(NewJob(Guid.NewGuid(), tenant.Id, marker, isActive: false));
            await db.SaveChangesAsync();

            expectedTotalJobs = await db.Jobs.CountAsync();
            token = tokenService.GenerateToken(admin);
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var stats = await client.GetFromJsonAsync<AdminStatsDto>("/api/admin/stats", JsonOptions);

        stats.Should().NotBeNull();
        stats!.TotalJobs.Should().Be(expectedTotalJobs);
    }

    private static async Task<PagedResult<JobDto>> GetPageAsync(
        HttpClient client,
        string keyword,
        int page,
        int pageSize)
    {
        var response = await client.GetAsync(
            $"/api/jobs?keyword={Uri.EscapeDataString(keyword)}&page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PagedResult<JobDto>>(JsonOptions))!;
    }

    private static Tenant NewTenant(string marker) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"Pagination {marker}",
        Slug = marker
    };

    private static Job NewJob(Guid id, Guid tenantId, string title, bool isActive = true) => new()
    {
        Id = id,
        TenantId = tenantId,
        Title = title,
        Description = "Pagination contract test job.",
        Location = "Remote",
        SalaryMin = 50_000,
        SalaryMax = 80_000,
        JobType = JobType.FullTime,
        IsActive = isActive
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };
}
