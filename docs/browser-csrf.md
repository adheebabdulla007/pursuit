# SEC-1: Browser CSRF boundary

## Problem and accepted behavior

Pursuit uses HttpOnly JWT and refresh cookies. SameSite=Strict helps, but cookie
authentication needs an independent proof that a state-changing browser request
originated from our client. Every unsafe `/api` request must carry a valid ASP.NET
Core antiforgery request token and cookie. The bootstrap is a no-cache
`GET /api/auth/csrf` returning the request token in JSON; the cookie remains
HttpOnly. The SPA sends the token in `X-CSRF-TOKEN` and keeps it only within the
immediate request. It never stores either authentication token in browser storage.

The client bootstraps before each mutation, including anonymous register/login,
refresh, logout, JSON writes, and multipart resume upload. This handles principal
changes on login, logout, expiry, and in another tab. A 401 still follows the
existing single-flight refresh flow; that refresh gets its own antiforgery token,
then the retried request gets a fresh token for the new principal. Bootstrap is
a plain fetch, so it cannot recurse into refresh or redirect on an anonymous 401.
The extra GET per mutation is deliberate: it bounds token lifetime/identity
coupling without cache invalidation across tabs. The service validates before
MVC body/form binding, preventing invalid requests from uploading a blob.

Bearer-only API calls without auth cookies can bypass the antiforgery check only
when a bearer identity has already authenticated and the path is not under
`/api/auth`. Auth endpoints require the token even with a bearer header. Requests
with any auth cookie always require it, regardless of an Authorization header.
Unsafe requests with an Origin must match the server origin or an explicitly
trusted configured client origin. An opaque `null` Origin and cross-site Fetch
Metadata without an Origin are rejected. An absent Origin is still subject to
antiforgery; non-browser bearer clients remain supported. CORS permits credentials
only for the same trusted client origins. Development/E2E trust the two existing
localhost Vite origins; production has no implicit localhost allowance. A
separately hosted production client must configure exact HTTPS
`Cors:AllowedOrigins` entries and remain same-site with the API because the
authentication cookies use SameSite=Strict. A same-origin deployment needs no
separate CORS entry.

## Key management and rollout

ASP.NET Core antiforgery uses Data Protection. Production requires an absolute,
pre-existing shared `DataProtection:KeyRingPath` with appropriately restricted
filesystem permissions and encryption at rest. All API instances use the same
application name and key directory. Deployment must persist/back up the ring and
verify cross-instance token issuance/validation before scaling. Losing keys
invalidates existing CSRF tokens; the next bootstrap can issue a fresh pair.
Secure antiforgery cookies are mandatory in Production; local HTTP remains usable
for Development/E2E. TLS termination and trusted forwarded headers need validation
in the actual deployment topology, not a guessed proxy trust rule here.

The protected cookie flow changes atomically across API and client. An old SPA
served against the new API will receive HTTP 400 on mutations. Deploy the new
client and API together, and test the cutover. The existing access/refresh cookie
names and paths do not change.

## Acceptance and review cases

- Anonymous bootstrap plus register/login; authenticated bootstrap after identity
  change; refresh after access expiry; logout via direct and legacy routes.
- Missing/invalid token, untrusted Origin, and cross-site metadata are denied
  before a state-changing endpoint executes; a valid token succeeds.
- Multipart apply succeeds with token and rejects without it. A retry after 401
  builds a fresh header and preserves a reusable FormData body.
- A valid bearer-only client without auth cookies still works; a bearer header
  cannot disable protection when auth cookies are present.
- Production startup rejects an absent shared key-ring path or unsafe client
  origin configuration. Cross-instance and proxy behavior require deployment
  verification before public operation.

References: [ASP.NET Core antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0),
[antiforgery API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.antiforgery.iantiforgery?view=aspnetcore-10.0),
[CORS](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0),
[Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0).
