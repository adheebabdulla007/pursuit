# Production configuration and administrator bootstrap

Pursuit validates required configuration immediately after the application builder
is created. Invalid settings stop startup before database migrations, administrator
creation, or hosted consumers can run. Error messages name configuration keys and
never include their values.

The tracked base configuration contains non-secret defaults only. Supply these
settings through the deployment platform's secret store or environment variables:

- `ConnectionStrings__DefaultConnection`
- `JwtSettings__Secret` (at least 32 bytes)
- `RedisSettings__ConnectionString`
- `RabbitMqSettings__Host`, `Port`, `Username`, and `Password`
- `AzureBlobSettings__ConnectionString`
- `DataProtection__KeyRingPath` for a shared, persistent production key ring
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, and so on when the client
  is hosted on a different origin

Production rejects unencrypted SQL Server connections, SQL Server certificate
bypass, the RabbitMQ guest account, and Azurite's development storage connection. Configuration validation checks shape
and policy only; dependency connectivity belongs in deployment readiness checks.

## First administrator

Administrator creation is disabled in the base configuration. For the first
deployment only, supply the following from the deployment secret store:

```text
AdminBootstrapSettings__Enabled=true
AdminBootstrapSettings__Email=<administrator email>
AdminBootstrapSettings__Password=<unique strong password>
```

The password must be at least 12 characters and include uppercase, lowercase,
numeric, and non-alphanumeric characters. Bootstrap execution takes a SQL Server
application lock, so concurrent replicas serialize the check-and-create operation.
If an administrator already exists, startup leaves it unchanged.

After the first administrator exists, remove the email and password settings and
set `AdminBootstrapSettings__Enabled=false`. Development keeps an explicit local
bootstrap account in `appsettings.Development.json`; those settings are never
loaded by a Production environment.

Database migrations still run at application startup. Until migrations move to a
dedicated deployment job, production rollout must start one application replica,
allow migration and bootstrap to complete, disable bootstrap, and then scale out.
