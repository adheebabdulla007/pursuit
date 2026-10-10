using System.Diagnostics;
using System.Text.Json;

namespace Pursuit.IntegrationTests.Deployment;

public sealed class ComposeMigrationBoundaryTests
{
    [Theory]
    [InlineData("docker-compose.yml", "migrate", "postgres", "PursuitDb")]
    [InlineData("docker-compose.e2e.yml", "migrate-e2e", "postgres-e2e", "PursuitDb")]
    public async Task ResolvedComposeConfig_DefinesOneShotMigrationService(
        string composeFile,
        string migrationService,
        string postgresService,
        string databaseName)
    {
        using var config = await ResolveComposeConfigAsync(composeFile);
        var services = config.RootElement.GetProperty("services");
        var migration = services.GetProperty(migrationService);
        var postgres = services.GetProperty(postgresService);

        Assert.Contains("/app/migrations/efbundle", migration.GetProperty("entrypoint").GetRawText());
        Assert.Equal(
            "service_healthy",
            migration.GetProperty("depends_on").GetProperty(postgresService).GetProperty("condition").GetString());
        Assert.False(migration.TryGetProperty("ports", out _));
        var environment = migration.GetProperty("environment").GetRawText();
        Assert.Contains("ConnectionStrings__DefaultConnection", environment);
        Assert.Contains($"Host={postgresService}", environment);
        Assert.Contains($"Database={databaseName}", environment);
        Assert.Contains("SSL Mode=Disable", environment);
        Assert.StartsWith("postgres:17-alpine", postgres.GetProperty("image").GetString());
        Assert.Contains("pg_isready", postgres.GetProperty("healthcheck").GetRawText());
        Assert.DoesNotContain("sqlserver", config.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mssql", config.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DockerfileAndCi_UsePostgreSqlMigrationConnectionsOnly()
    {
        var dockerfile = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "Dockerfile"));
        var workflow = File.ReadAllText(Path.Combine(FindRepositoryRoot(), ".github", "workflows", "ci.yml"));

        Assert.Contains("Host=localhost;Port=5432;Database=PursuitBuild", dockerfile);
        Assert.Contains("postgres:17-alpine", workflow);
        Assert.Contains("Host=localhost;Port=5432;Database=PursuitMigrationCi", workflow);
        Assert.DoesNotContain("mssql", dockerfile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mssql", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TrustServerCertificate", dockerfile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TrustServerCertificate", workflow, StringComparison.OrdinalIgnoreCase);
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
        startInfo.Environment["POSTGRES_PASSWORD"] = "compose-boundary-test-password";
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
