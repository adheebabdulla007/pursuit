using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pursuit.Domain.Enums;
using Pursuit.Infrastructure.Persistence;

namespace Pursuit.IntegrationTests.Configuration;

[Collection("Integration Tests")]
public sealed class AdminBootstrapTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Bootstrap_is_idempotent_when_replicas_start_concurrently()
    {
        await using (var cleanupScope = factory.Services.CreateAsyncScope())
        {
            var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await cleanupDb.Users.IgnoreQueryFilters()
                .Where(user => user.Role == UserRole.Admin)
                .ExecuteDeleteAsync();
        }

        await Task.WhenAll(
            AdminSeeder.SeedAsync(factory.Services),
            AdminSeeder.SeedAsync(factory.Services));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var administrators = await db.Users.IgnoreQueryFilters()
            .CountAsync(user => user.Role == UserRole.Admin);

        administrators.Should().Be(1);
    }
}
