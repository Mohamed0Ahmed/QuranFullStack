# AccessAdmin CLI

`QuranDashboard.AccessAdmin` is the operator boundary for normalized identity, the canonical
permission catalogue, and explicit Phase 3 Owner reconciliation. It uses the shared application
and infrastructure services, never silently migrates the database, and exposes no HTTP mutation.

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
| `authorization preflight` | Requires the live Phase 2 tables, columns, indexes, and constraints; no pending migrations; clean normalized identity data; exact canonical database-code parity with no canonical code retired; and reconciled ready Owner state. |

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
payloads. Direct-grant administration, endpoint enforcement, and legacy-role conversion remain later
phases and are deliberately absent from this executable.
