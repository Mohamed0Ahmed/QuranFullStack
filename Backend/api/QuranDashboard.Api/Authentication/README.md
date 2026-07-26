# Authentication

JWT bearer authentication wiring for `QuranDashboard.Api`. This folder owns registration of the
bearer scheme, its options validation, role-claim enrichment, the current-user accessor, the named
authorization policies, and the 401 failure envelope. Everything here is registered by
`AddApiAuthentication`, called from `AddApiServices` in `../Extensions/ServiceCollectionExtensions.cs`.

## JWT bearer configuration

- Options bind from the `Auth` configuration section (`JwtAuthenticationOptions.SectionName`) and expose
  `Authority` and `Audience`.
- `Authority` sets `options.Authority`; `Audience` sets `TokenValidationParameters.ValidAudience`. No
  explicit issuer is configured, so issuer validation follows the JwtBearer default derived from the
  authority's OIDC metadata.
- `MapInboundClaims = false` keeps raw claim types, so the identity key stays the literal `sub`. Logto
  issues RFC 9068 `at+jwt` access tokens, whose default inbound claim map would otherwise rename `sub`.
- Startup validation: `JwtAuthenticationOptionsValidator` with `ValidateOnStart` fails fast unless
  `Authority` is an absolute `https` URI and `Audience` is non-blank.

## Role enrichment

`RoleClaimsTransformation` (`IClaimsTransformation`, scoped) runs on authenticated principals only. It
loads the active role name via `IUserRoleResolver` keyed on the `sub` claim and, when one exists, adds a
`ClaimTypes.Role` claim on a separate `ClaimsIdentity` marked with `RoleClaimsAuthenticationType`. It is
idempotent off that marker identity (not off any token-borne role claim); there is no result caching in
the transformation itself, so an un-marked authenticated principal re-queries the resolver.

## Current user

`HttpContextCurrentUser` (`ICurrentUser`, scoped) exposes `Sub` from the request principal's `sub` claim
and throws `InvalidOperationException` when it is absent — so it must be resolved only inside an
authenticated request.

## Authorization policies

- `AuthorizationPolicyNames` exposes `Owner`/`Admin`/`Editor` constants whose values equal `RoleNames`
  (`Domain.Access`); policy name == seeded `roles.name`.
- Three named role policies are registered, each `RequireAuthenticatedUser().RequireRole(<role>)`, plus
  the `SystemOwner` policy and one policy per permission code (registered by
  `../Security/Authorization/PermissionAuthorizationRegistration.cs`).
- No global fallback policy is configured.

## Failed-auth response

`UnauthorizedRejectionWriter` (singleton) writes a `401` carrying the shared
`ApiResponse<object>.Fail(ApiMessages.Unauthorized)` JSON envelope in place of the framework's empty 401.
It is wired through `JwtBearerEvents.OnChallenge`, which suppresses the default response
(`HandleResponse`) and delegates to the writer; the writer no-ops if the response has already started.

## Boundary / current phase

- There is no fallback policy: endpoints are anonymous unless a controller opts in. Applied today:
  plain `[Authorize]` on `api/access/me`; `SystemOwner` (+ the `PermissionAdmin` rate-limit policy) on
  `api/security/permissions/*`; per-action permission-code policies on all Abwab write surfaces
  (sections, categories, relationships, templates, protection). The three role policies
  (`Owner`/`Admin`/`Editor`) remain registered but are not applied to any endpoint by name.
- This folder owns auth wiring only. User provisioning and role resolution live in Application /
  Infrastructure behind `ICurrentUser` and `IUserRoleResolver`.

## Testing the pipeline

The smoke harness (`Backend/tests/QuranDashboard.Tests/Smoke/`) exercises this wiring end to end with
the REAL JwtBearer handler: fixtures `PostConfigure<JwtBearerOptions>` with a test issuer + RSA key and
mint `sub`-only tokens (`TestJwtTokens`), so `MapInboundClaims = false`, the role transformation, and
the `OnChallenge` 401 envelope all run unmodified. Personas are DB rows (never claims), matching how
roles/permissions are resolved in production. A guard test asserts the scheme inventory stays exactly
`["Bearer"]` with the JwtBearer handler.

## Related

- Registration entry: `../Extensions/ServiceCollectionExtensions.cs` (`AddApiServices` → `AddApiAuthentication`)
- Role constants: `../../../domain/QuranDashboard.Domain/Access/RoleNames.cs`
- Response envelope: `../Contracts/ApiResponse.cs`
- Controllers: `../Controllers/README.md`
- API root: `../README.md`
