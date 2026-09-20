using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Pursuit.IntegrationTests;

[Collection("Integration Tests")]
public sealed class OpenApiTests
{
    private readonly CustomWebApplicationFactory _factory;

    public OpenApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DevelopmentOpenApiDocument_GeneratesSuccessfully()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        document.GetProperty("openapi").GetString().Should().NotBeNullOrWhiteSpace();
        document.GetProperty("paths").TryGetProperty("/api/Jobs", out _).Should().BeTrue();
    }
}
