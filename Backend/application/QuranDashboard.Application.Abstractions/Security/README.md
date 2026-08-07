# Security (Access Abstractions)

This folder defines the **contracts** for first-login user provisioning and request-scoped
authorization state. It contains only abstractions and their DTOs;
the concrete behavior lives in the Application and Infrastructure layers (see **Implementations**
below).

The sibling `Access/` contracts define the shared email identity boundary used by access
provisioning and the Phase 2 operator preflight. `IEmailIdentityNormalizer` produces the required
canonical identity while preserving the provider value for display; `IEmailIdentityPreflight`
reports invalid and colliding legacy identities before the additive migration, then missing,
mismatched, and colliding persisted identities while it performs the explicit normalizer-backed
backfill between the staged migrations.

## What this folder defines

- **`ICurrentUser`** — exposes the authenticated caller's Logto `sub`, `email`, and
  `email_verified` claims after the API has validated signature, issuer, audience, and expiry.
  `sub` is the stable identity key that joins a Logto account to its local `Users` row.
  Accessing it outside an authenticated (`[Authorize]`) request fails closed.
- **`IInteractiveIdentityEvidenceValidator`** — validates a separately supplied interactive OIDC bearer
  token for the confirmed Logto-subject relink flow and returns only the validated `sub`, `email`, and
  `email_verified` identity facts. The contract never retains or returns the raw token.
- **`IAccessRequestContext`** — supplies a request correlation identifier to the access-audit appender;
  it is deliberately not an authorization or identity source.
- **`IExternalUserProfileSource`** — `GetProfileAsync(logtoSub, ct)` returns an
  `ExternalUserProfile` (`Email`, `UserName`, `DisplayName`). `primaryEmail` is provider
  identity/matching data only; it is never proof that the email is verified.
- **`IUserProvisioningService`** — `GetOrCreateAsync(identity, ct)` returns a
  `ProvisionedUser` (`Sub`, `Email`, `DisplayName`, `Status`, `IsOwner`, ordered direct
  `Permissions`).
- **`IAuthorizationStateResolver`** — `ResolveAsync(logtoSub, ct)` returns an
  `AuthorizationState` or `null` for an unknown local subject. Its state contains the local user id,
  status, Owner flag, and direct permission-code set.
- **`AuthorizationState`** — the authorization snapshot consumed by API requirement handlers; it has no
  claim-derived role or permission data.
- **`ProvisionedUser`** — the provisioning result record.
- **`UserProvisioningEmailConflictException`** — an expected business conflict (see below),
  carrying only the conflicting email.

## First-login provisioning contract

- **Get-or-create keyed by Logto `sub`.** An existing subject is returned unchanged; a
  first-time subject is created from the provider profile email used as local identity data.
- **Fail-closed by default.** A newly created user starts `Pending` with **no role**
  (`RoleId` is a nullable FK); a role is granted only by later assignment. A user cannot be
  provisioned without provider email data, and a client-supplied value is never substituted.
- **Owner bootstrap (sole exception).** A login whose normalized identity belongs to the validated
  `OwnerBootstrap:Emails` desired set may become `Owner`/`Active` only in that authenticated
  interactive request: the validated claims must have matching `sub`, a present normalized `email`,
  and `email_verified=true`; the provider primary email is used only to match the local identity.
  The M2M CLI cannot add Owners. Other configured identities may remain
  `AwaitingVerifiedSignIn` without blocking a verified configured Owner. A `Disabled` user is never
  auto-revived or promoted by login. Empty, invalid, and duplicate normalized owner lists fail
  configuration validation.
- **Email conflict.** `UserProvisioningEmailConflictException` is raised when provisioning a new
  subject collides with an existing local email identity, including the canonical normalized-email
  comparison. Provisioning does not merge or relink users.

## Request-scoped authorization-state contract

`IAuthorizationStateResolver` receives only the authenticated `sub` supplied by `ICurrentUser`. It
projects one local database user snapshot without provisioning or accepting token role/permission
claims. A scoped implementation memoizes the task for that subject, returns unknown local users as
`null`, and treats a second distinct subject in the same scope as an invariant failure. Direct
permission codes are present only for an active non-Owner; an active Owner is represented by `IsOwner`
and has no need for direct grants.

There is no cached role resolver or token-claim transformation in this boundary. Every authorization
decision resolves its state from the local database through the scoped resolver.

## Domain facts (from `Domain/Access`)

- `UserStatus`: `Pending = 1`, `Active = 2`, `Disabled = 3` (values pinned explicitly).
- `RoleNames`: `Owner`, the sole seeded `roles.name` value.
- `AccessAuditMetadata` is the audit envelope and the only construction path for an
  `AccessAuditEvent`'s metadata: `SchemaVersion` is an `int` rejected below `1`, `CorrelationId` is
  null or non-blank, and `Provenance` is a copied ordinal string map. The version invariant is a
  typed Domain rule, so no caller can express an unversioned event.
- The audit **payloads** (`ActorSnapshotJson`, `TargetSnapshotJson`, `BeforeStateJson`,
  `AfterStateJson`) stay opaque versioned documents rather than fixed types, because
  `SchemaVersion` exists so historical rows stay readable after their shape evolves. Domain
  therefore rejects only blank documents and **never parses JSON** — that would be parsing
  infrastructure in Domain (`Backend/.architecture/CLEAN_ARCHITECTURE.md`). Well-formedness comes
  from the `jsonb` columns, and object-ness from
  `ck_access_audit_events_documents_are_objects`; `ck_access_audit_events_metadata_schema_version`
  re-asserts the version at the storage boundary as a **positive `Int32`-range integer**, matching
  what `AccessAuditMetadata.SchemaVersion` can hold — a decimal-form, fractional, or out-of-range
  raw write would otherwise persist audit history the typed reader cannot deserialize. Both are
  enforced for every writer, including SQL that bypasses the Domain, and
  `AuthorizationSchemaPreflight` requires them by exact definition.
- `AccessAuditMetadata` is stored as `jsonb` through the value conversion in
  `Infrastructure/Persistence/Configurations/Access/AccessAuditEventConfiguration.cs`; the camelCase
  `schemaVersion` property name the check constraint reads is that converter's contract.

## Implementations (outside this folder)

- `Application/Access/Commands/ProvisionCurrentUser/ProvisionCurrentUserHandler.cs` — wires
  `ICurrentUser.Sub` into `IUserProvisioningService.GetOrCreateAsync`.
- `Infrastructure/Access/UserProvisioningService.cs` — the provisioning contract above,
  including the concurrent unique-index race handling.
- `Infrastructure/Persistence/Reads/Access/AuthorizationStateResolver.cs` — the scoped local
  authorization-state projection and memoization contract.
- `Infrastructure/Access/LogtoManagementApiUserProfileSource.cs` — resolves primary-email identity
  data from the Logto Management API (M2M client-credentials token), without asserting verification.
- `Infrastructure/Access/OwnerBootstrapOptions.cs` — validates the normalized
  `OwnerBootstrap:Emails` desired set.
- `Application/Access/OwnerReconciliation/OwnerReconciliationService.cs` — owns the locked Owner
  policy and orchestration. `Infrastructure/Access/OwnerReconciliationStore.cs` owns its EF
  transaction, lock, and audit persistence adapter.
