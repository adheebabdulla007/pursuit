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

Production requires PostgreSQL `SSL Mode=VerifyFull`, rejects certificate bypass,
and requires `GSS Encryption Mode=Disable` so password-authenticated connections
always use the certificate-validated TLS path.
the RabbitMQ guest account, and Azurite's development storage connection. Configuration validation checks shape
and policy only; dependency connectivity belongs in deployment readiness checks.

A hosted PostgreSQL connection must keep hostname verification enabled:

```text
Host=<provider-host>;Port=5432;Database=<database>;Username=<api-user>;Password=<secret>;SSL Mode=VerifyFull;GSS Encryption Mode=Disable;
```

Do not add `Trust Server Certificate=true`. Store the complete value in the
hosting provider's secret manager, never in a tracked file.

## Database migrations

The API does not create or alter the database schema. CI builds
`artifacts/migrations/Pursuit.Migrations` for Linux and runs it against an empty database and
an up-to-date database. The API container image carries the same artifact at
`/app/migrations/Pursuit.Migrations`.

The production runner accepts the database connection only from
`ConnectionStrings__DefaultConnection`; it has no command-line connection override
and has no plaintext exception compiled into it. It fails before opening a
connection unless PostgreSQL uses `SSL Mode=VerifyFull` with certificate trust
bypass disabled. Local Compose and CI build a separate non-production artifact
whose plaintext path is restricted to explicit Development or Testing environments
and the repository's loopback or Compose PostgreSQL hostnames. Never promote that
local-test artifact or image.

Do not use `dotnet ef database update` for deployment. EF Core applies its
`--connection` override after the design-time factory creates the context, so the
factory cannot enforce the final target. The reviewed `Pursuit.Migrations`
artifact is the production migration boundary.

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
dotnet restore src/Pursuit.Migrations/Pursuit.Migrations.csproj --runtime linux-x64
dotnet publish src/Pursuit.Migrations/Pursuit.Migrations.csproj --configuration Release --runtime linux-x64 --self-contained false -p:PublishSingleFile=true --output artifacts/migrations --no-restore
```

Run it with the migration identity injected by the deployment secret store:

```bash
ConnectionStrings__DefaultConnection='<migration connection with SSL Mode=VerifyFull>' ./artifacts/migrations/Pursuit.Migrations
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
numeric, and non-alphanumeric characters. Bootstrap execution takes a PostgreSQL
transaction-scoped advisory lock, so concurrent replicas serialize the check-and-create operation.
If an administrator already exists, startup leaves it unchanged.

After the first administrator exists, remove the email and password settings and
set `AdminBootstrapSettings__Enabled=false`. Development keeps an explicit local
bootstrap account in `appsettings.Development.json`; those settings are never
loaded by a Production environment.

Run the migration job before enabling administrator bootstrap. After it succeeds,
start the API with bootstrap enabled, confirm the account exists, remove the bootstrap
email and password, disable bootstrap, and complete the rollout.
