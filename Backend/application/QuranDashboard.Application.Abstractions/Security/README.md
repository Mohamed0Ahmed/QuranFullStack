# Security (Access Abstractions)

This folder defines the **contracts** for first-login user provisioning and role
resolution. It contains only abstractions and their DTOs; the concrete behavior lives
in the Application and Infrastructure layers (see **Implementations** below).

The sibling `Access/` contracts define the shared email identity boundary used by access
provisioning and the Phase 2 operator preflight. `IEmailIdentityNormalizer` produces the required
canonical identity while preserving the provider value for display; `IEmailIdentityPreflight`
reports invalid, missing, mismatched, and colliding persisted identities and performs the explicit
normalizer-backed backfill between the staged migrations.

## What this folder defines

- **`ICurrentUser`** — exposes `Sub`, the authenticated caller's Logto `sub` claim: the
  stable identity key that joins a Logto account to its local `Users` row. Fail-closed:
  accessing it outside an authenticated (`[Authorize]`) request throws.
- **`IExternalUserProfileSource`** — `GetProfileAsync(logtoSub, ct)` returns an
  `ExternalUserProfile` (`Email`, `UserName`, `DisplayName`, `EmailVerified`). Its values
  are **server-verified by the identity provider (Logto)** and must be treated as trusted —
  never client-supplied.
- **`IUserProvisioningService`** — `GetOrCreateAsync(logtoSub, ct)` returns a
  `ProvisionedUser` (`Sub`, `Email`, `DisplayName`, `Status`, `RoleId`, `RoleName`).
- **`IUserRoleResolver`** — `GetActiveRoleNameAsync(logtoSub, ct)` returns the active role
  name or `null`; `Evict(logtoSub)` drops a subject's cached result immediately.
- **`ProvisionedUser`** — the provisioning result record.
- **`UserProvisioningEmailConflictException`** — an expected business conflict (see below),
  carrying only the conflicting email.

## First-login provisioning contract

- **Get-or-create keyed by Logto `sub`.** An existing subject is returned unchanged; a
  first-time subject is created from the server-verified profile email.
- **Fail-closed by default.** A newly created user starts `Pending` with **no role**
  (`RoleId` is a nullable FK); a role is granted only by later assignment. A user cannot be
  provisioned without a server-verified email, and a client-supplied value is never
  substituted.
- **Owner bootstrap (sole exception).** A login whose email matches the configured
  `Auth:BootstrapOwnerEmail` is provisioned (or promoted) to `Owner`/`Active` **only when the
  IdP also reports the email as verified**; otherwise it is provisioned like any normal user
  (`Pending`, no role) and can be promoted later once verified. A `Disabled` user is never
  auto-revived or promoted by login. An empty `BootstrapOwnerEmail` disables bootstrap (a
  valid config); a non-empty value is format-validated fail-fast at startup.
- **Email conflict.** `UserProvisioningEmailConflictException` is raised when provisioning
  collides on the **email** unique index (not `logto_sub`): a subject deleted and recreated
  in the IdP presents a brand-new `sub` carrying a server-verified email that already belongs
  to an existing local user.

## Role-resolution / caching contract

Implementations must cache results — **including negative ones** — behind a short TTL so a
role-less caller does not hit the database on every authenticated request, and must expose
immediate invalidation via `Evict`, which the role/status write path calls so a change is
seen without waiting for the TTL. A `null` result means no active role: no user, user not
`Active`, or no role assigned. (`CachedUserRoleResolver` uses a 30-second TTL and cancellation
change tokens so an `Evict` on any request invalidates the shared cache immediately and
race-safely.)

## Domain facts (from `Domain/Access`)

- `UserStatus`: `Pending = 1`, `Active = 2`, `Disabled = 3` (values pinned explicitly).
- `RoleNames`: `Owner`, `Admin`, `Editor` (seeded `roles.name` values that auth policies match).

## Implementations (outside this folder)

- `Application/Access/Commands/ProvisionCurrentUser/ProvisionCurrentUserHandler.cs` — wires
  `ICurrentUser.Sub` into `IUserProvisioningService.GetOrCreateAsync`.
- `Infrastructure/Access/UserProvisioningService.cs` — the provisioning contract above,
  including the concurrent unique-index race handling.
- `Infrastructure/Access/CachedUserRoleResolver.cs` — the caching/eviction contract.
- `Infrastructure/Access/LogtoManagementApiUserProfileSource.cs` — resolves the
  server-verified profile from the Logto Management API (M2M client-credentials token).
- `Infrastructure/Access/OwnerBootstrapOptions.cs` — the `Auth:BootstrapOwnerEmail` option
  (empty disables bootstrap; a non-empty value is format-validated fail-fast).
