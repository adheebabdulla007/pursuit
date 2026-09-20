# SEC-2: OpenAPI dependency remediation

Status: locally verified on 20 September 2026; hosted CI pending a push.

## Decision

Update the existing direct `Microsoft.AspNetCore.OpenApi` reference from `10.0.8` to
`10.0.12`. Its NuGet dependency range requires `Microsoft.OpenApi >= 2.12.0` and
`< 3.0.0`. Restore resolves `Microsoft.OpenApi` `2.12.0` in both the API and
integration-test projects. The reported circular-reference parsing advisory
affects the 2.x line below `2.7.5`.

First review: the original restore graph resolved vulnerable
`Microsoft.OpenApi` `2.0.0` through `Microsoft.AspNetCore.OpenApi` `10.0.8`.
The newer ASP.NET Core package stays on the project's .NET 10 major version and
requires a patched OpenAPI.NET 2.x version.

Second review: updating the direct framework integration package preserves the
existing package ownership and avoids pinning a transitive library separately.
It changes one production package reference, with no application API or schema
change. The runtime-generated document and full integration suite were checked
because compilation alone would not establish compatibility.

## Verification

- `dotnet restore Pursuit.slnx --force-evaluate`: passed without NU1903.
- `dotnet list Pursuit.slnx package --vulnerable --include-transitive`: no
  vulnerable packages reported from the configured NuGet sources.
- `dotnet build Pursuit.slnx --configuration Release --no-restore`: passed with
  zero warnings and errors.
- `dotnet test Pursuit.slnx --configuration Release --no-build`: 26/26 passed
  against Docker-backed services, including an HTTP 200 OpenAPI document with
  the Jobs path.
- Clean frontend install: passed with zero npm vulnerabilities. Lint, 47/47
  component tests, and the production build passed.
- The existing employer-to-applicant Playwright journey passed in Chromium,
  Firefox, and WebKit (3/3) against the isolated E2E stack. The first launch
  used mismatched SQL credentials between Compose and the API; after the
  synthetic credentials were aligned, all three browsers passed.

This removes the reported vulnerable resolved version. It does not establish
that Pursuit accepts untrusted OpenAPI documents or certify a public deployment.
Hosted CI for this change remains pending until the commit is pushed.

References: [OpenAPI.NET advisory](https://github.com/microsoft/OpenAPI.NET/security/advisories/GHSA-v5pm-xwqc-g5wc),
[ASP.NET Core OpenAPI 10.0.12 package dependencies](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/10.0.12).
