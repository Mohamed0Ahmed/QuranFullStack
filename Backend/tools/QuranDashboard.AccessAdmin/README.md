# AccessAdmin CLI

`QuranDashboard.AccessAdmin` is the Phase 2 operator boundary for normalized identity and the
canonical permission catalogue. It uses the Application and Infrastructure services and never
silently migrates the database, reconciles Owners, assigns grants, or exposes an HTTP mutation.

## Commands

| Command | Behavior |
|---|---|
| `identity scan` | Read-only invalid, missing, mismatched, and normalized-collision scan. |
| `identity backfill --apply` | Writes `Users.NormalizedEmail` through the shared normalizer after a clean collision/validity check. |
| `catalogue sync` | Inserts missing canonical permissions, updates display metadata, and reports unknown database codes without deleting them. |
| `authorization preflight` | Requires no pending migrations, clean normalized identity data, and exact canonical database-code parity. |

The staged Phase 2 migration sequence is: apply the nullable additive migration, run the identity
scan and explicit backfill, apply the required-column migration, then synchronize the catalogue and
run the preflight. A failed scan or preflight returns non-zero and does not merge identities.

Owner reconciliation, grant administration, endpoint enforcement, and legacy-role conversion are
later phases and are deliberately absent from this executable.
