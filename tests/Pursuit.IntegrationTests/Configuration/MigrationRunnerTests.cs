extern alias migrations;

using FluentAssertions;
using MigrationRunner = migrations::Pursuit.Migrations.MigrationRunner;

namespace Pursuit.IntegrationTests.Configuration;

public sealed class MigrationRunnerTests
{
    [Fact]
    public async Task RunAsync_ProductionArtifactRejectsTestingPlaintextWithoutRunningMigration()
    {
        var migrationCalled = false;
        var error = new StringWriter();

        var exitCode = await MigrationRunner.RunAsync(
            "Host=localhost;Database=Pursuit;Username=pursuit;Password=Marker-Secret-1!;SSL Mode=Disable;GSS Encryption Mode=Disable;",
            "Testing",
            allowInsecureLocalMigrations: false,
            _ =>
            {
                migrationCalled = true;
                return Task.CompletedTask;
            },
            error);

        exitCode.Should().Be(1);
        migrationCalled.Should().BeFalse();
        error.ToString().Should().Contain("SSL Mode=VerifyFull")
            .And.NotContain("Marker-Secret-1!");
    }

    [Fact]
    public async Task RunAsync_LocalArtifactPermitsTestingLoopbackPlaintext()
    {
        var migrationCalled = false;
        var error = new StringWriter();

        var exitCode = await MigrationRunner.RunAsync(
            "Host=127.0.0.1;Database=Pursuit;Username=pursuit;Password=Marker-Secret-1!;SSL Mode=Disable;GSS Encryption Mode=Disable;",
            "Testing",
            allowInsecureLocalMigrations: true,
            _ =>
            {
                migrationCalled = true;
                return Task.CompletedTask;
            },
            error);

        exitCode.Should().Be(0);
        migrationCalled.Should().BeTrue();
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_LocalArtifactRejectsTestingExternalPlaintext()
    {
        var migrationCalled = false;
        var error = new StringWriter();

        var exitCode = await MigrationRunner.RunAsync(
            "Host=provider.example.com;Database=Pursuit;Username=pursuit;Password=Marker-Secret-1!;SSL Mode=Disable;GSS Encryption Mode=Disable;",
            "Testing",
            allowInsecureLocalMigrations: true,
            _ =>
            {
                migrationCalled = true;
                return Task.CompletedTask;
            },
            error);

        exitCode.Should().Be(1);
        migrationCalled.Should().BeFalse();
        error.ToString().Should().Contain("SSL Mode=VerifyFull")
            .And.NotContain("Marker-Secret-1!");
    }

    [Fact]
    public async Task RunAsync_MigrationFailureReturnsSanitizedErrorInsteadOfThrowing()
    {
        var error = new StringWriter();

        var exitCode = await MigrationRunner.RunAsync(
            "Host=database;Database=Pursuit;Username=pursuit;Password=Marker-Secret-1!;SSL Mode=VerifyFull;GSS Encryption Mode=Disable;",
            "Production",
            allowInsecureLocalMigrations: false,
            _ => throw new InvalidOperationException("Provider error contained Marker-Secret-1!"),
            error);

        exitCode.Should().Be(1);
        error.ToString().Should().Be("Database migration failed." + Environment.NewLine)
            .And.NotContain("Marker-Secret-1!");
    }
}
