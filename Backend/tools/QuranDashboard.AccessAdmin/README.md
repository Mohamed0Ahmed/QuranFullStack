# AccessAdmin CLI

`QuranDashboard.AccessAdmin` is the operator boundary for normalized identity, the canonical
permission catalogue, explicit Owner reconciliation, and legacy Admin/Editor conversion. It uses the
shared application and infrastructure services, never silently migrates the database, and exposes no
HTTP mutation.

## Commands

| Command | Behavior |
|---|---|
| `identity scan` | Read-only invalid and normalized-collision scan before the additive migration; once `normalized_email` exists, it also reports missing and mismatched values. |
| `identity backfill --apply` | Writes `Users.NormalizedEmail` through the shared normalizer after a clean collision/validity check. |
| `catalogue sync` | Inserts missing canonical permissions, updates display metadata, and reports unknown database codes and retired canonical codes without deleting or reactivating them. |
| `owners validate` | Validates `OwnerBootstrap:Emails`, then reports the same read-only reconciliation state as status. Candidates without verified interactive evidence are `AwaitingVerifiedSignIn`. |
| `owners status` | Reports desired/current Owner reconciliation state without mutation. Configured users that have not supplied verified interactive evidence are `AwaitingVerifiedSignIn`. |
| `owners reconcile --dry-run` | Reports the same read-only reconciliation delta; an apply always reacquires the lock and recomputes trusted state. |
| `owners reconcile --apply --reason <text> [--confirm-production]` | Applies only safe removals and Owner direct-grant cleanup after resolved provider matching and last-active-Owner checks. It never promotes a new Owner. `--confirm-production` is required when the tool runs in Production. |
| `legacy-roles inventory` | Read-only inventory of every role-bearing Owner/Admin/Editor user: ID, role ID/name, status, normalized email, `sub`, and direct-grant count. It also reports Owner reconciliation and conversion-preflight violations. |
| `legacy-roles convert --apply [--confirm-production]` | Rechecks and locks the inventory, then clears only zero-grant Admin/Editor relations in one transaction and appends an audit event per user. It never derives permissions from a removed role and refuses on any conversion preflight violation. `--confirm-production` is required when the tool runs in Production. |
| `authorization preflight` | Requires the live Phase 2 tables, columns, indexes, and constraints; no pending migrations; clean normalized identity data; exact canonical database-code parity with no canonical code retired; reconciled ready Owner state; and no Admin/Editor reference. |

Indexes and constraints are compared by their **actual PostgreSQL definition**
(`pg_get_indexdef` / `pg_get_constraintdef`, whitespace-normalized), not by name, so a same-named
constraint carrying a different expression, a foreign key rewired to another column, a unique index
downgraded to non-unique, and a reordered or unpredicated index are all reported. Indexes must also
be valid and ready. Columns are compared on nullability, `format_type` (so `integer` → `bigint` or
`varchar(128)` → `varchar(256)` is a violation) and identity generation.
`AuthorizationSchemaRequirements` holds those expected definitions verbatim;
`AccessSchemaDriftTests` asserts a freshly migrated schema produces zero violations, so a
PostgreSQL rendering change fails a test rather than silently loosening the gate.

Catalogue parity is over **active** canonical codes: a canonical row carrying `retired_at` is
reported as `catalogue_retired=` and fails both `catalogue sync` and `authorization preflight`.
Neither command reactivates it — retiring a still-referenced permission is an operator decision to
reverse deliberately.

**A normal deploy no longer needs `catalogue sync`.** The API host synchronizes the catalogue itself
at startup through `Program.cs` and `SynchronizePermissionCatalogueAsync`, so a migrated database populates `permissions`
without an operator running anything. This command stays the remedy for the cases the startup sync
deliberately will not resolve: an unknown code left in the table, a retired canonical code, and any
database the API cannot reach at boot. It is also the only way to sync a database whose API is
running with `Access__PermissionCatalogueStartupSync__Enabled=false`. The staged sequence below keeps
the explicit `catalogue sync` step because it runs before the API is deployed at all.

After `qd-build`, run the staged Phase 2 sequence from `Backend/` in this order:

```bash
./scripts/access-admin identity scan
dotnet ef database update --migration AddAuthorizationAccessFoundation --project infrastructure/QuranDashboard.Infrastructure --startup-project api/QuranDashboard.Api --context QuranDashboardDbContext
./scripts/access-admin identity scan
./scripts/access-admin identity backfill --apply
./scripts/access-admin identity scan
dotnet ef database update --migration RequireNormalizedEmail --project infrastructure/QuranDashboard.Infrastructure --startup-project api/QuranDashboard.Api --context QuranDashboardDbContext
./scripts/access-admin catalogue sync
./scripts/access-admin authorization preflight
```

The first scan reads only legacy `users.id` and `users.email`, so it is the stop-before-DDL
collision gate. The additive migration leaves `normalized_email` nullable and unindexed. The final
migration requires the completed backfill before it adds the required-column constraint and unique
index. The additive migration also creates the audit-document constraints. A failed scan or
preflight returns non-zero and does not merge identities.

After the Phase 2 foundation is live, validate and reconcile the configured Owner set before the
preflight gate:

```bash
./scripts/access-admin owners validate
./scripts/access-admin owners status
./scripts/access-admin owners reconcile --dry-run
./scripts/access-admin owners reconcile --apply --reason "initial Owner invariant cleanup"
./scripts/access-admin authorization preflight
```

When `DOTNET_ENVIRONMENT=Production`, add `--confirm-production` to the apply command. A dry run
does not reserve its outcome: apply always takes the advisory lock and reloads configuration,
database rows, and provider evidence before it computes and commits its own delta.

Owner reconciliation has one serialized mutation boundary. It records any direct-grant revocation
before the Owner-grant audit event in the same committed mutation, and it leaves a configured
identity `AwaitingVerifiedSignIn` until that person completes verified interactive OIDC sign-in.

## Owner recovery

Recovery from an inaccessible Owner starts by adding another email to `OwnerBootstrap:Emails`; do
not relink or remove the inaccessible Owner as part of that first step. The person who controls the
newly configured email must complete their **own** verified interactive `/api/access/me` sign-in.
Only that matching `sub`, normalized email, and `email_verified=true` evidence can promote the new
identity to an Active Owner. The AccessAdmin CLI cannot promote anyone.

After the new Owner can act, any removal of the inaccessible Owner is a separate reconciliation
change. It is audited and remains subject to last-active-Owner protection, so it cannot reduce the
configured active Owner count below one.

## Legacy conversion before cleanup

The cleanup migration deletes only the seeded Admin and Editor rows. The existing restrictive
`users.role_id` foreign key is intentional: if any user still references either row, PostgreSQL rejects
the migration rather than cascading or changing a user.

Before an operator applies that migration, perform and retain this sequence under the short access-admin
write freeze:

1. Run `legacy-roles inventory` and retain its non-secret per-user output and counts.
2. Confirm the intended Owner set through the configured normalized emails and current Logto provider
   identity matching reported by the command; only previously verified interactive-OIDC promotions remain
   Owners, while configured users without that evidence remain `AwaitingVerifiedSignIn`.
3. Resolve every inventory/preflight violation. In particular, a former Admin/Editor user with a direct
   grant is refused; remove that explicit grant through the normal audited administration path before
   conversion rather than preserving or inferring anything from the removed role.
4. Run `legacy-roles convert --apply` (add `--confirm-production` in Production). It locks and rereads
   the rows, clears former Admin/Editor `RoleId` values transactionally, writes no grants, and records
   one audit event per converted user.
5. Run `legacy-roles inventory` again and retain the before/after counts and identifiers. It must show
   no Admin/Editor reference.
6. Run `authorization preflight`; before the cleanup migration it is expected to remain non-zero for the
   pending migration, but it must not report a legacy-role reference or conversion-preflight violation.
7. Apply the generated cleanup migration through the deployment process, then rerun
   `authorization preflight` for a fully clean result.

For a clean database created from the current migration head, there is no populated legacy
authorization state to convert and no conversion rehearsal is required. If a release instead upgrades
a populated database that carries legacy Admin/Editor identities, an operator must rehearse the
sequence above against a production-like copy and retain the before/after inventory artifact before
applying the cleanup migration. Never use this tool or migration workflow to fabricate rehearsal
evidence.

Before real authorization users, grants, or audit history exist, an explicitly disposable
development or pre-release database may be dropped and recreated from the current migration head.
Once real authorization data exists, never use a destructive authorization `Down` migration or table
drop as rollback. Keep unsafe routes protected and use a schema-compatible code rollback, a
data-preserving restore, or repair forward.

The executable loads its copied `appsettings.json` beside the compiled tool, then Development user
secrets for this tool, then environment variables. Set `ConnectionStrings__QuranDashboardDb` to
override the connection without changing files; the command works from any current directory. User
secrets load only under the `Development` environment, and `Backend/scripts/access-admin` defaults
`DOTNET_ENVIRONMENT` to `Development` unless the caller already exported one — running the raw
executable in another environment needs the connection environment variable.

The command is parsed before any host or database service is constructed, so a malformed command
never touches configuration. Exit codes are stable: `0` clean, `2` usage, `3` a reported
preflight/catalogue failure, and `4` a configuration or database failure, printed as
`access_admin_failure=<exception type>` with an operator hint and no stack trace or connection
details.

The Logto profile source uses Management API `primaryEmail` only for current identity matching.
There is no documented durable verified-email field for the M2M client, so the executable never
promotes a user from Management API data. A configured user becomes Owner only during that user's
authenticated interactive sign-in after Backend OIDC validation confirms matching `sub`, present
normalized `email`, and `email_verified=true`. Provider unavailability returns stable exit `4` as
`access_admin_failure=LogtoProviderUnavailable` without provider URLs, subjects, tokens, or response
payloads. Direct-grant administration and endpoint enforcement are handled elsewhere; this executable
does not expose endpoint or direct-grant mutation.
