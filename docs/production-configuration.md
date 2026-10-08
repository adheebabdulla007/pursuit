# Production configuration and administrator bootstrap

Pursuit validates required configuration immediately after the application builder
is created. Invalid settings stop startup before administrator creation or hosted
consumers can run. Error messages name configuration keys and never include their
values.

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

## Database migrations

The API does not create or alter the database schema. CI builds
`artifacts/migrations/efbundle` for Linux and runs it against an empty database and
an up-to-date database. The API container image carries the same bundle at
`/app/migrations/efbundle`.

Use two database identities in production:

- The migration job identity can create and alter schema objects. Supply its
  connection as `ConnectionStrings__DefaultConnection` only to the one-time job.
- The API identity has the data permissions needed by normal requests. Supply its
  connection under the same key only to API instances.

The deployment order is fixed: back up the database, run the migration job, stop on
any non-zero exit, deploy the API image, then run post-deployment checks. API replicas
must not start before the migration job succeeds.

To build the same Linux artifact outside CI:

```powershell
dotnet tool restore
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=PursuitBuild;User Id=build;Password=BuildOnly1!;TrustServerCertificate=True;"
dotnet dotnet-ef migrations bundle --project src/Pursuit.Infrastructure --startup-project src/Pursuit.API --configuration Release --target-runtime linux-x64 --output artifacts/migrations/efbundle --force
Remove-Item Env:ConnectionStrings__DefaultConnection
```

Run it with the migration identity injected by the deployment secret store:

```bash
ConnectionStrings__DefaultConnection='<migration connection>' ./artifacts/migrations/efbundle
```

Do not run an automatic down-migration during rollback. If a migration fails, keep
the new API revision stopped and inspect the database. Restore the provider snapshot
or ship a reviewed forward fix when schema changes have already committed. Deploy the
previous API image only when its code is compatible with the current schema.

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

Run the migration job before enabling administrator bootstrap. After it succeeds,
start the API with bootstrap enabled, confirm the account exists, remove the bootstrap
email and password, disable bootstrap, and complete the rollout.
