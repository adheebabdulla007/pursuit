using System.Diagnostics;
using System.Text.Json;

namespace Pursuit.IntegrationTests.Deployment;

public sealed class ComposeMigrationBoundaryTests
{
    [Theory]
    [InlineData("docker-compose.yml", "migrate", "sqlserver")]
    [InlineData("docker-compose.e2e.yml", "migrate-e2e", "sqlserver-e2e")]
    public async Task ResolvedComposeConfig_DefinesOneShotMigrationService(
        string composeFile,
        string migrationService,
        string sqlService)
    {
        using var config = await ResolveComposeConfigAsync(composeFile);
        var services = config.RootElement.GetProperty("services");
        var migration = services.GetProperty(migrationService);

        Assert.Contains("/app/migrations/efbundle", migration.GetProperty("entrypoint").GetRawText());
        Assert.Equal(
            "service_healthy",
            migration.GetProperty("depends_on").GetProperty(sqlService).GetProperty("condition").GetString());
        Assert.False(migration.TryGetProperty("ports", out _));
        Assert.Contains("ConnectionStrings__DefaultConnection", migration.GetProperty("environment").GetRawText());
    }

    [Fact]
    public async Task ResolvedLocalComposeConfig_BlocksApiUntilMigrationSucceeds()
    {
        using var config = await ResolveComposeConfigAsync("docker-compose.yml");
        var api = config.RootElement.GetProperty("services").GetProperty("api");

        Assert.Equal(
            "service_completed_successfully",
            api.GetProperty("depends_on").GetProperty("migrate").GetProperty("condition").GetString());
    }

    private static async Task<JsonDocument> ResolveComposeConfigAsync(string composeFile)
    {
        var repositoryRoot = FindRepositoryRoot();
        var startInfo = new ProcessStartInfo("docker")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("compose");
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add(composeFile);
        startInfo.ArgumentList.Add("config");
        startInfo.ArgumentList.Add("--format");
        startInfo.ArgumentList.Add("json");
        startInfo.Environment["SA_PASSWORD"] = "ComposeBoundary1!";
        startInfo.Environment["JWT_SECRET"] = "compose-boundary-test-secret-at-least-32-bytes";

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Docker Compose.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.True(process.ExitCode == 0, $"docker compose config failed: {error}");
        return JsonDocument.Parse(output);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Pursuit.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the Pursuit repository root.");
    }
}
