using System.Net.Mail;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Pursuit.API.Configuration;

public static class StartupConfigurationValidator
{
    public static void Validate(IConfiguration configuration, bool isProduction)
    {
        var failures = new List<string>();

        var database = RequireConnectionString(configuration, "DefaultConnection", failures);
        var jwtSecret = Require(configuration, "JwtSettings:Secret", failures);
        Require(configuration, "JwtSettings:Issuer", failures);
        Require(configuration, "JwtSettings:Audience", failures);
        ValidateInteger(configuration, "JwtSettings:ExpiryInMinutes", 1, 15, failures);
        ValidateInteger(configuration, "JwtSettings:RefreshTokenExpiryInDays", 1, 30, failures);
        Require(configuration, "RedisSettings:ConnectionString", failures);
        Require(configuration, "RabbitMqSettings:Host", failures);
        ValidateInteger(configuration, "RabbitMqSettings:Port", 1, 65535, failures);
        var rabbitUsername = Require(configuration, "RabbitMqSettings:Username", failures);
        var rabbitPassword = Require(configuration, "RabbitMqSettings:Password", failures);
        var blobConnection = Require(configuration, "AzureBlobSettings:ConnectionString", failures);
        Require(configuration, "AzureBlobSettings:ContainerName", failures);

        if (jwtSecret is not null && Encoding.UTF8.GetByteCount(jwtSecret) < 32)
            failures.Add("JwtSettings:Secret must contain at least 32 bytes.");

        var bootstrapEnabled = ValidateBoolean(configuration, "AdminBootstrapSettings:Enabled", failures);
        if (bootstrapEnabled)
            ValidateAdministratorBootstrap(configuration, failures);

        ValidateDatabase(database, isProduction, failures);

        if (isProduction)
        {
            if (string.Equals(rabbitUsername, "guest", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rabbitPassword, "guest", StringComparison.Ordinal))
                failures.Add("Production RabbitMQ credentials must not use the guest account.");

            if (blobConnection?.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase) is true)
                failures.Add("Production Azure Blob storage must not use the development emulator.");
        }

        if (failures.Count != 0)
            throw new InvalidOperationException(
                "Invalid startup configuration:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    private static string? Require(
        IConfiguration configuration,
        string key,
        ICollection<string> failures)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{key} is required.");
            return null;
        }

        return value;
    }

    private static string? RequireConnectionString(
        IConfiguration configuration,
        string name,
        ICollection<string> failures)
    {
        var value = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"ConnectionStrings:{name} is required.");
            return null;
        }

        return value;
    }

    private static void ValidateInteger(
        IConfiguration configuration,
        string key,
        int minimum,
        int maximum,
        ICollection<string> failures)
    {
        if (!int.TryParse(configuration[key], out var value) || value < minimum || value > maximum)
            failures.Add($"{key} must be between {minimum} and {maximum}.");
    }

    private static bool ValidateBoolean(
        IConfiguration configuration,
        string key,
        ICollection<string> failures)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{key} is required.");
            return false;
        }

        if (bool.TryParse(value, out var parsed))
            return parsed;

        failures.Add($"{key} must be either true or false.");
        return false;
    }

    private static void ValidateAdministratorBootstrap(
        IConfiguration configuration,
        ICollection<string> failures)
    {
        var email = Require(configuration, "AdminBootstrapSettings:Email", failures);
        var password = Require(configuration, "AdminBootstrapSettings:Password", failures);

        if (email is not null && (!MailAddress.TryCreate(email, out var address)
                                  || !string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase)))
            failures.Add("AdminBootstrapSettings:Email must be a valid email address.");

        if (password is not null
            && (password.Length < 12
                || !password.Any(char.IsUpper)
                || !password.Any(char.IsLower)
                || !password.Any(char.IsDigit)
                || password.All(char.IsLetterOrDigit)))
            failures.Add("AdminBootstrapSettings:Password must be at least 12 characters and include upper-case, lower-case, numeric, and non-alphanumeric characters.");
    }

    private static void ValidateDatabase(
        string? connectionString,
        bool isProduction,
        ICollection<string> failures)
    {
        if (connectionString is null)
            return;

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (isProduction && builder.Encrypt == SqlConnectionEncryptOption.Optional)
                failures.Add("Production SQL Server configuration must encrypt the connection.");
            if (isProduction && builder.TrustServerCertificate)
                failures.Add("Production SQL Server configuration must validate the server certificate.");
        }
        catch (ArgumentException)
        {
            failures.Add("ConnectionStrings:DefaultConnection is not a valid SQL Server connection string.");
        }
    }
}
