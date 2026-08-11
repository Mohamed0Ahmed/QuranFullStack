# Authentication

JWT bearer authentication wiring for `QuranDashboard.Api`. This folder owns registration of the
bearer scheme, its options validation, the current-user accessor, and authorization registration.
The sibling `Authorization/` folder owns the requirement handlers, endpoint metadata,
controlled authorization responses, and unsafe-endpoint validator; their registrations are wired by
`AddApiAuthentication`, called from `AddApiServices` in
`../Extensions/ServiceCollectionExtensions.cs`.

## JWT bearer configuration

- Options bind from the `Auth` configuration section (`JwtAuthenticationOptions.SectionName`) and expose
  `Authority`, the API-resource `Audience`, and the SPA `InteractiveClientId`.
- `Authority` sets `options.Authority`; `Audience` sets `TokenValidationParameters.ValidAudience`. No
  explicit issuer is configured, so issuer validation follows the JwtBearer default derived from the
  authority's OIDC metadata.
- The default `Bearer` scheme validates only API resource access tokens against `Audience`.
  `LogtoIdTokenEvidence` is a separate named JwtBearer scheme for signed interactive ID-token evidence
  and validates its audience against `InteractiveClientId`. It never replaces the default scheme.
- `MapInboundClaims = false` keeps raw claim types, so the identity key stays the literal `sub`. Logto
  issues RFC 9068 `at+jwt` access tokens, whose default inbound claim map would otherwise rename `sub`.
- Startup validation: `JwtAuthenticationOptionsValidator` with `ValidateOnStart` fails fast unless
  `Authority` is an absolute `https` URI and both audience values are non-blank.

## E2E test issuer

Authenticated Playwright runs configure a test RS256 issuer on both bearer schemes only when the host
environment is exactly `Testing` and `E2E:TestIssuer:Enabled=true`. Both conditions are required. The
flag defaults off, has no appsettings entry, and is ignored in Development and Production. An enabled
Testing host also requires an absolute HTTPS issuer distinct from `Auth:Authority` and a non-empty
public `E2E:TestIssuer:Jwks` document. The private key remains in the Playwright fixture.

The gated schemes intentionally use a static `OpenIdConnectConfiguration` containing the test issuer
and public keys. That suppresses authority metadata retrieval for the isolated run, so real Logto
tokens do not validate while the test gate is active even though the primary authority remains in the
valid-issuer list. Outside the two-part gate, normal authority metadata and signing keys are unchanged.

The `IExternalUserProfileSource` registration is never replaced. The Playwright-owned backend wrapper
points the real `LogtoManagementApiUserProfileSource` at a local Management API stub that serves only
the run's `e2e-*` identities. Authentication, `/api/access/me` provisioning, Owner reconciliation,
local authorization-state resolution, and permission administration all remain the production paths.

## Current user

`HttpContextCurrentUser` (`ICurrentUser`, scoped) exposes only `Sub` from the API access-token
principal's `sub` claim and throws `InvalidOperationException` when it is absent — so it must be
resolved only inside an authenticated request. Email identity facts are never accepted from that
principal.

## Authorization core

- `AuthorizationStateAccessEvaluator` receives the authenticated `sub` through `ICurrentUser`; neither
  token role claims nor permission-looking token claims participate in authorization.
- `IAuthorizationStateResolver` projects the one local user snapshot for that subject: status, local
  Owner relation, and direct permission codes only for an active non-Owner. Its scoped implementation
  memoizes that task and rejects a second subject in the same request scope.
- `[RequirePermission(code)]` requires an exact known catalogue code unless the active local user is an
  Owner. `[RequireOwner]` requires an active local Owner and never treats a direct permission grant as
  equivalent.
- `JwtInteractiveIdentityEvidenceValidator` validates the signed ID token supplied separately for
  `/api/access/me` provisioning and Logto-subject relink operations. The named scheme validates
  signature, issuer, lifetime, and SPA-client audience; the validator additionally requires a present
  `sub`, a present `email`, `email_verified=true`, and exact equality between the evidence `sub` and the
  expected subject supplied by the Application layer. For `/api/access/me`, that expected subject is the
  authenticated API access-token `sub`. For relink, it is the requested replacement `newSub`; the acting
  Owner's API access token remains the bearer credential and authorization identity. The validator
  returns only validated identity facts; raw evidence tokens are neither returned nor persisted.
  `HttpContextAccessRequestContext` supplies the request trace identifier for audit metadata.
- `UnsafeEndpointMetadataValidator` checks unsafe route classification and the requirement handlers
  repeat that check fail-closed. It is registered during API startup after controller mapping, and every
  current Abwab write action carries exactly one matching `[RequirePermission]` classification.

No role-claim transformation, role resolver, or named role policy is registered. There is no global
fallback policy.

## Authorization responses

`ApiAuthorizationMiddlewareResultHandler` is the sole challenge/forbid response owner. Its
`AuthorizationRejectionWriter` writes the shared `ApiResponse<object>.Fail(...)` JSON envelope with
`application/json`, leaves a started response untouched, and selects centralized `ApiMessages` values:

- Challenge → `401` and `ApiMessages.Unauthorized`.
- Forbidden local state → `403` for unprovisioned, inactive, missing-permission, or Owner-only access.
- Authorization-state infrastructure failure → fail-closed `503` with
  `ApiMessages.AuthorizationUnavailable`; the evaluator logs correlation data without database or
  permission details.

The JWT bearer event does not write a competing challenge body.

## Boundary / current phase

- The twenty-one existing Abwab write actions have exact permission metadata and startup validates every
  unsafe endpoint. The twelve access-administration routes use exact `[RequireOwner]` metadata; normal
  direct grants never satisfy them. There is no fallback policy: public GETs remain anonymous, while
  `api/access/me` remains authenticated-only (see `../Controllers/README.md`). Production activation is an
  explicit deployment decision, not an API fallback or temporary grant path.
- This folder owns API auth wiring. User provisioning and authorization-state resolution live behind
  Application abstractions with Infrastructure implementations.

## Related

- Registration entry: `../Extensions/ServiceCollectionExtensions.cs` (`AddApiServices` → `AddApiAuthentication`)
- Role constants: `../../../domain/QuranDashboard.Domain/Access/RoleNames.cs`
- Response envelope: `../Contracts/ApiResponse.cs`
- Controllers: `../Controllers/README.md`
- API root: `../README.md`
