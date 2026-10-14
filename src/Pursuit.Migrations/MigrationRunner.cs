using Pursuit.Infrastructure.Persistence;

namespace Pursuit.Migrations;

public static class MigrationRunner
{
    public static async Task<int> RunAsync(
        string? connectionString,
        string? environment,
        bool allowInsecureLocalMigrations,
        Func<string, Task> migrate,
        TextWriter error)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            await error.WriteLineAsync(
                "ConnectionStrings:DefaultConnection is required to run database migrations.");
            return 1;
        }

        var permitsLocalPlaintext = allowInsecureLocalMigrations
            && PostgreSqlConnectionSecurityPolicy.PermitsExplicitLocalPlaintext(
                connectionString,
                environment);

        if (!permitsLocalPlaintext)
        {
            var validationErrors = PostgreSqlConnectionSecurityPolicy.GetProductionValidationErrors(
                connectionString);
            if (validationErrors.Count != 0)
            {
                foreach (var validationError in validationErrors)
                    await error.WriteLineAsync(validationError);

                return 1;
            }
        }

        try
        {
            await migrate(connectionString);
            return 0;
        }
        catch (Exception)
        {
            await error.WriteLineAsync("Database migration failed.");
            return 1;
        }
    }
}
