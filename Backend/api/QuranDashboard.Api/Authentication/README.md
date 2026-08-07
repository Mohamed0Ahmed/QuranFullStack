# Authentication

JWT bearer authentication wiring for `QuranDashboard.Api`. This folder owns registration of the
bearer scheme, its options validation, the current-user accessor, and authorization registration.
The sibling `Authorization/` folder owns the requirement handlers, endpoint metadata,
controlled authorization responses, and unsafe-endpoint validator; their registrations are wired by
`AddApiAuthentication`, called from `AddApiServices` in
`../Extensions/ServiceCollectionExtensions.cs`.

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

## Current user

`HttpContextCurrentUser` (`ICurrentUser`, scoped) exposes `Sub` from the request principal's `sub` claim
and throws `InvalidOperationException` when it is absent — so it must be resolved only inside an
authenticated request.

## Authorization core

- `AuthorizationStateAccessEvaluator` receives the authenticated `sub` through `ICurrentUser`; neither
  token role claims nor permission-looking token claims participate in authorization.
- `IAuthorizationStateResolver` projects the one local user snapshot for that subject: status, local
  Owner relation, and direct permission codes only for an active non-Owner. Its scoped implementation
  memoizes that task and rejects a second subject in the same request scope.
- `[RequirePermission(code)]` requires an exact known catalogue code unless the active local user is an
  Owner. `[RequireOwner]` requires an active local Owner and never treats a direct permission grant as
  equivalent.
- `JwtInteractiveIdentityEvidenceValidator` validates the separate bearer token supplied to a Logto-subject
  relink operation with the same configured `JwtBearer` scheme. It returns only validated `sub`, `email`,
  and `email_verified` evidence to the Application layer; raw evidence tokens are neither returned nor
  persisted. `HttpContextAccessRequestContext` supplies the request trace identifier for audit metadata.
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
