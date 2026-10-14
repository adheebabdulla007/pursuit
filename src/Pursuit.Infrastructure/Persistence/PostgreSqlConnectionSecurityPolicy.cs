using Npgsql;

namespace Pursuit.Infrastructure.Persistence;

public static class PostgreSqlConnectionSecurityPolicy
{
    private static readonly HashSet<string> LocalMigrationHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "::1",
        "postgres",
        "postgres-e2e"
    };

    public static IReadOnlyList<string> GetProductionValidationErrors(string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var errors = new List<string>();
            if (builder.SslMode != SslMode.VerifyFull)
                errors.Add("Production PostgreSQL configuration must use SSL Mode=VerifyFull.");

            if (builder.GssEncryptionMode != GssEncryptionMode.Disable)
                errors.Add("Production PostgreSQL configuration must use GSS Encryption Mode=Disable.");

            if (builder.TryGetValue("Trust Server Certificate", out var trustServerCertificate)
                && trustServerCertificate is true)
                errors.Add("Production PostgreSQL configuration must validate the server certificate.");

            return errors;
        }
        catch (ArgumentException)
        {
            return ["ConnectionStrings:DefaultConnection is not a valid PostgreSQL connection string."];
        }
    }

    public static void ValidateProduction(string connectionString)
    {
        var errors = GetProductionValidationErrors(connectionString);
        if (errors.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
    }

    public static bool PermitsExplicitLocalPlaintext(string connectionString, string? environment)
    {
        if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(environment, "Testing", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            return builder.SslMode == SslMode.Disable
                && builder.GssEncryptionMode == GssEncryptionMode.Disable
                && !string.IsNullOrWhiteSpace(builder.Host)
                && LocalMigrationHosts.Contains(builder.Host);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
