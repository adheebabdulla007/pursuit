using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Entities;
using Pursuit.Domain.Enums;
using Pursuit.Infrastructure.Persistence;

namespace Pursuit.IntegrationTests.Applications;

[Collection("Integration Tests")]
public class ApplicationUploadBoundaryTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ApplicationUploadBoundaryTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task DuplicateApplication_DoesNotUploadAnotherResume()
    {
        var marker = Guid.NewGuid().ToString("N");
        Guid jobId;
        string seekerToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var tenant = new Tenant { Id = Guid.NewGuid(), Name = $"Company {marker}", Slug = marker };
            var seeker = new User
            {
                Id = Guid.NewGuid(), Email = $"seeker-{marker}@test.com", PasswordHash = "x",
                FirstName = "Test", LastName = "Seeker", Role = UserRole.JobSeeker
            };
            var job = new Job
            {
                Id = Guid.NewGuid(), TenantId = tenant.Id, Title = "Open job",
                Description = "A role", Location = "Remote", SalaryMin = 1_000,
                SalaryMax = 2_000, JobType = JobType.FullTime, IsActive = true
            };
            db.Tenants.Add(tenant);
            db.Users.Add(seeker);
            db.Jobs.Add(job);
            db.Applications.Add(new Domain.Entities.Application
            {
                Id = Guid.NewGuid(), JobId = job.Id, TenantId = tenant.Id,
                ApplicantId = seeker.Id, ResumeUrl = "https://example.test/resume.pdf",
                Status = ApplicationStatus.Applied
            });
            await db.SaveChangesAsync();
            jobId = job.Id;
            seekerToken = tokenService.GenerateToken(seeker);
        }

        var storage = new CountingBlobStorageService();
        using var app = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService>(storage);
        }));
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seekerToken);
        using var body = new MultipartFormDataContent();
        body.Add(new StringContent(jobId.ToString()), "jobId");
        using var resume = new ByteArrayContent("%PDF-1.4"u8.ToArray());
        resume.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        body.Add(resume, "resume", "resume.pdf");

        var response = await client.PostAsync("/api/applications", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        storage.UploadCount.Should().Be(0);
    }

    private sealed class CountingBlobStorageService : IBlobStorageService
    {
        public int UploadCount { get; private set; }

        public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            UploadCount++;
            return Task.FromResult("https://example.test/extra.pdf");
        }

        public Task<string> GetDownloadUrlAsync(string blobUrl, TimeSpan expiry, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
