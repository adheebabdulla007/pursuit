# SEC-2: registration role allowlist

Status: locally verified on 30 September 2026. Hosted CI is checked separately
after the commit is pushed.

## Security boundary

Public registration accepts only the role names `Employer` and `JobSeeker`,
compared case-insensitively. Numeric enum values, undefined values, `Admin`,
comma-separated names, repeated names, and values with surrounding whitespace
are rejected before email lookup, tenant creation, user persistence, or session
issuance.

The previous use of `Enum.TryParse` accepted numeric and composite strings. An
undefined numeric value could be stored in `Users.Role` and receive access and
refresh tokens, even though later access-token validation rejected the unusable
role. Defined numeric and composite forms could also be interpreted as a valid
registration role.

## Decision

`RegistrationRolePolicy` is the single Application-layer parser used by both
`RegisterDtoValidator` and `AuthService`. Validation therefore retains the API's
structured HTTP 400 response, while the service remains authoritative for direct
callers that do not pass through MVC validation.

Case-insensitive canonical names remain compatible with the prior contract.
Whitespace is not normalized because the client and documented API send role
identifiers, not user-authored labels. Rejecting noncanonical representations
keeps authorization inputs unambiguous.

No database constraint is added in this tranche. The database legitimately
stores `Admin`, while public registration must reject it; that semantic boundary
belongs before persistence. A future database-integrity review can separately
consider a constraint limiting stored roles to all defined system roles.

## Verification scope

Integration coverage exercises the public CSRF-protected endpoint and direct
`IAuthService` calls. Rejected attempts are followed by a valid registration with
the same email to prove that the invalid request had no persisted user side
effect. Lowercase employer and job-seeker registrations verify retained behavior.

- Focused role security and compatibility tests: 18/18 passed.
- Release build: passed with zero warnings and errors.
- Full Docker-backed integration suite: 44/44 passed.
- Frontend lint, 47/47 component tests, and production build: passed.
- Employer-to-applicant Playwright journey: 3/3 passed in Chromium, Firefox,
  and WebKit against the isolated E2E stack.
