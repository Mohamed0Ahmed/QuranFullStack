# Quran import destructive-path safety guard

**Layer:** Infrastructure · persistence write · **Feature:** 028 US2 (fail-closed substrate)

## What this area does

`QuranImportDestructiveGuard` is the single fail-closed guard every destructive Quran import step
routes through. It exists so a `TRUNCATE … CASCADE` (or a `DELETE`) inside a `--force` import can never
silently destroy a future Abwab dependent, even under concurrent creation of that dependent.

`ExecuteDestructiveAsync(connection, transaction, sql, ct)` runs, in order:

1. **Advisory lock** — a transaction-scoped `pg_advisory_xact_lock` (key `20280002`) that serializes
   destructive imports against any writer cooperatively taking the same key.
2. **FK-closure preflight** — parses the destructive target tables, computes their transitive
   FK-dependent closure from `pg_catalog` (what a CASCADE reaches), and throws
   `QuranImportSafetyException` if any reached persistent table is **not** `quran_*`.
3. **Execute** the destructive SQL.

The closure preflight is **privilege-agnostic**: it fails closed on a cross-domain cascade regardless
of the DB role, so Abwab protection does not depend on grants. The seeded restricted application role
(GRANT/REVOKE) is a migration concern owned by US3/T038, not this US2 guard.

## Invariants / caveats (read before changing)

- **Every** destructive statement in `../` (the domain writers) must go through this guard on its
  `--force` branch. If you add a new force/reseed path, route it here too.
- The guard's in-scope domain is defined by the `quran_*` table-name prefix. The only non-Quran tables
  in the schema (`users`, `roles`) reference no Quran table, so the preflight passes for every current
  import. A new legitimately-Quran table **must** keep the `quran_` prefix or the guard will fail-close
  on it.
- The guard is **structural**: no Abwab table/FK exists yet (prohibited until 028 exits, FR-009). The
  preflight starts protecting Abwab automatically the instant the first Abwab→Quran FK is added.
- The navigation pipeline keeps its own stricter `EnsureWriteIsolation`; this guard is layered on top,
  not a replacement.

## Related

- Destructive-path inventory + design: `Backend/report/feature-028-abwab-safety-foundations/destructive-path-inventory.md`.
- Importer-side environment/source-identity gate: `Backend/tools/QuranDashboard.DataImporter/Import/Safety/`.
- Write mechanics: `../README.md`.
