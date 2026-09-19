# AUTH-3: account and session revocation

## Enforcement policy

- Every newly issued access JWT includes the user's `security_version` GUID. The
  authentication handler queries current user and tenant state for each protected
  request. It requires an active user, matching security version, role and tenant,
  and an active tenant when present. A missing account, missing claim, inactive
  account, changed role/tenant, or database failure denies authentication.
- Deactivation changes the security version and revokes all refresh tokens in one
  SQL transaction, serialized on the user's row with login, refresh and logout.
  The next authentication check rejects all previously issued access tokens.
  Reactivation does not restore them. A request already past authentication when
  deactivation commits may finish; this is not mid-request cancellation.
- Tenant suspension denies existing access tokens, login and refresh at their
  next checks. Resuming a tenant permits still-valid access and refresh tokens;
  tenant suspension does not rotate each user's security version. A future tenant
  management workflow should decide whether suspension should permanently revoke.
- Current-device logout revokes the presented refresh token and all its rotated
  descendants within the same transaction. Other login chains remain renewable.
  Logout does not invalidate an already-issued access JWT. That JWT expires in at
  most 15 minutes. This is a deliberate bounded logout window, while account
  deactivation is checked on every authenticated request.

## Storage, scale, and rollout

The migration adds `Users.SecurityVersion`, fills existing rows with distinct
values, and removes the temporary empty-GUID default. Existing access JWTs lack
the claim, so users must obtain a new token after rollout; existing refresh tokens
can renew if their accounts remain active. Apply the migration before serving the
new API version. Rolling back the API may temporarily restore old JWT behavior,
so rollback needs a deliberate access policy. The current API startup applies
migrations automatically; before a multi-instance production rollout, rehearse
the backfill on representative data and apply it in a controlled deployment step.

An indexed user lookup plus tenant join runs for every authenticated request. This
avoids stale per-instance caches and makes SQL availability part of protected
request availability. The query projects only a boolean. Login, refresh and
status change take the same user-row lock. Logout's recursive set update follows
the session's indexed replacement chain, avoiding scans of unrelated sessions.
The SQL recursion limit is 32,767; an unexpectedly deeper or malformed chain
causes rollback rather than partial revocation.

The configured JWT lifetime must be 1–15 minutes; validation uses zero clock
skew. API instances and SQL hosts therefore need synchronized clocks. Existing
Development Compose now uses 15 minutes. Invalid lifetime configuration logs a
fatal startup error and exits unsuccessfully. The next security milestone addresses
CSRF for cookie-authenticated browser mutations; this change does not certify
the application for public production deployment.
