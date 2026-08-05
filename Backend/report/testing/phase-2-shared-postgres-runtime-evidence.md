# Phase 2 — Shared PostgreSQL 16 lifecycle foundation: run evidence

**Date:** 2026-08-05 · **Branch:** `feature/security-authorization-permissions` ·
**Plan:** `Backend/report/testing/test-runtime-optimization-implementation-plan.md` §4, §9 Phase 2

Evidence only. Counts and durations below are dated observations, not repository invariants.

## What landed

| Path | Purpose |
| --- | --- |
| `TestSupport/PostgreSql/PostgreSqlResourceLabels.cs` | The five ownership labels and run-ID resolution |
| `TestSupport/PostgreSql/PostgreSqlDatabaseName.cs` | Bounded, validated, quoted identifier generation |
| `TestSupport/PostgreSql/CrossProcessPostgreSqlLock.cs` | `FileShare.None` OS lock, 5 s visible retry, 15 min timeout |
| `TestSupport/PostgreSql/PostgreSqlTestServer.cs` | Container owner, migrated template, lease slots, DDL gate |
| `TestSupport/PostgreSql/PostgreSqlTestProcess.cs` | Process-static `Lazy<Task<…>>` surface plus `ProcessExit` cleanup |
| `TestSupport/PostgreSql/PostgreSqlDatabaseLease.cs` | Owned and external leases, idempotent disposal |
| `TestSupport/PostgreSql/PostgreSqlSchemaLease.cs` | Per-case schema on a leased database (consumed in Phase 4) |
| `TestSupport/PostgreSql/ExclusivePostgreSqlLease.cs` | Dedicated-image server for the Phase 5 decision |
| `TestSupport/DependencyInjection/OwnedServiceProviderRegistry.cs` | Reverse-order root disposal that survives failures |
| `Backend/scripts/cleanup-test-runtime` | Label + run-ID scoped Docker cleanup |
| `Backend/scripts/test-backend` | `EXIT`/`INT`/`TERM` traps calling the cleanup script |

`AbwabSchemaFixture` is the pilot consumer; `AbwabTreeReadTests` no longer constructs a nested
fixture.

## Commands run and results

| Command | Result |
| --- | ---: |
| `test-backend feature --class …PostgreSqlTestProcessContractTests --build` | 23 passed, 0 failed |
| `test-backend feature --class …OwnedServiceProviderRegistryTests --no-build` | 4 passed, 0 failed |
| `test-backend feature --class …TestGateCatalogTests --no-build` | 11 passed, 0 failed |
| `test-backend feature Abwab --no-build` | 63 passed, 0 failed |
| `test-backend fast --no-build` | 534 passed, 0 failed, about 4 s |
| `test-backend pre-pr --list-tests --no-build` | discovery matches the catalog exactly |

The contract class takes about 20 s wall, of which about 6 s is the deliberate child-process
lock wait. The Abwab lane takes about 14 s test wall on one shared PostgreSQL 16 start, replacing
the two starts (collection fixture plus nested fixture) it needed before.

**Explicitly not run**, per the phase fence: Access, Smoke, Tier B, Pipeline, canonical data, full
Backend, and all Frontend tests.

## Acceptance observations

- **One PG16 runtime.** During `feature Abwab`, `docker ps` showed exactly one container carrying
  all five labels (`owner=backend-tests`, `repository=quran-dashboard`, `run-id=79f7…`,
  `kind=postgresql`, `host-pid=190595`), plus Ryuk reported separately and left untouched.
- **Template and clone.** `pg_database` on that container held
  `qdb_test_template_…` with `datistemplate=t, datallowconn=f` and one clone
  `qdb_test_abwabschemafixture_…` with `datistemplate=f, datallowconn=t`.
- **Concurrent leases.** Two leases taken concurrently report one `ServerInstanceId`, two distinct
  database names, both at migration head, and a table created in one is absent from the other.
- **Slot cap.** With the cap drained, an extra lease does not complete within 2 s and completes
  after one lease is disposed. `QURAN_DASHBOARD_TEST_DB_PARALLELISM` rejects `0`, `5`, `-1`, and
  `four`; it accepts 1–4 and defaults to 4.
- **Cross-process lock.** A child `/usr/bin/flock --exclusive` holder makes the lock wait about 6 s
  with visible progress, after which it acquires. This confirms .NET's `FileShare.None` maps to
  `flock(2)` on this platform — the interop the whole cross-process guarantee rests on. A second
  in-process holder times out naming the first holder rather than waiting forever.
- **Pool cleanup is lease-scoped.** A surviving lease keeps the same `pg_backend_pid()` across
  another lease's disposal. Mutation-checked: replacing the lease-scoped `ClearPool` with
  `ClearAllPools()` fails this test (`expected 87, found 92`), so it is a real gate.
- **Idempotent disposal.** Disposing a lease twice drops the database once and returns exactly one
  slot.
- **Cancellation.** Two separate cases. An already-canceled caller is refused with
  `OperationCanceledException` and the runtime stays usable — that caller never reaches the
  semaphore, so the test is named for the refusal it proves and claims nothing about slot release.
  A caller canceled *while queued behind a drained cap* is the one that exercises the slot
  accounting: it throws, and the available-slot count returns to its starting value once the held
  leases are disposed.
- **Fast lane stays container-free.** `test-backend fast` ran 534 tests in about 4 s while
  `docker ps -a --filter label=com.qurandashboard.test.kind=postgresql` was polled continuously;
  the peak was 0. The §2.3 promise survives the new process-static runtime because no `Kind=Fast`
  class touches `PostgreSqlTestProcess` and its static constructor only registers `ProcessExit`.
- **No second container.** `LeaseExclusiveServerAsync` refuses while the shared runtime is active,
  and the shared runtime refuses while an exclusive lease is active. The live PostgreSQL 18
  exercise belongs to Phase 5.
- **Zero leak after normal exit.** After every run above,
  `docker ps -a --filter label=com.qurandashboard.test.owner=backend-tests` returns zero
  containers, and `cleanup-test-runtime` reports `none` — the managed `ProcessExit` path had
  already disposed the container, with the script trap as the backstop. Ryuk stays as the
  crash-only fallback; `.WithReuse(true)` is not used.
- **Cleanup safety.** `cleanup-test-runtime` refuses a blank or non-32-hex run ID with exit 2,
  matches all five labels plus the exact run ID, reports Ryuk separately, and never prunes. The
  `test-backend` EXIT trap preserves the underlying test exit code.

## Defect found and fixed during review

`LeaseExclusiveServerAsync` incremented an in-process exclusive-lease counter but decremented it
only on the failure path, so a *successfully* disposed exclusive lease would have left the counter
above zero and made every later shared-runtime request throw for the rest of the process. It was
unreachable today — the contract test trips the `SharedServerRequested` guard before the increment
— but it would have fired in Phase 5, where a process takes an exclusive PostgreSQL 18 server and
may then want the shared runtime. `ExclusivePostgreSqlLease` now carries a release callback that
its disposal invokes, mirroring `PostgreSqlDatabaseLease.Owned`.

## Two plan/code discrepancies resolved

1. **The Abwab caches are real and the reset must invalidate them.** An initial grep of
   `Persistence/Reads/Abwab/` and `Persistence/Writes/Abwab/` found no cache and suggested the
   plan's "invalidating tree/template caches" had no referent. That was wrong: the cache lives at
   `Infrastructure/Caching/Abwab/` (`CachedAbwabTreeReader`, `CachedAbwabTemplatesReader`) behind
   a singleton `AbwabCacheGeneration`. Truncating alone left `GetTreeAsync` returning a stale
   non-null `Version` over six empty tables, which is exactly how it was caught.
   `ResetAbwabAsync` now truncates and then calls `InvalidateTree()`/`InvalidateTemplates()`.
2. **The §4.6 external opt-in sentinel is not enforced yet.** `UseExternalReadOnlyDatabase`
   enforces the guard clause the plan states — local/loopback host only, explicit database name,
   never registered as an owned resource, disposal is a no-op — but
   `QURAN_DASHBOARD_TEST_EXTERNAL_DB_MODE=READ_ONLY_ACKNOWLEDGED` is checked where the five legacy
   feature variables are resolved, which is Phase 3 work. Normal gates already unset all five
   variables in `test-backend`.

## Carried into Phase 3

`AbwabSchemaTestCollection` is declared `UniqueKeyIsolation` in `test-resources.tsv`, and a case
that truncates the whole database is not that. It is safe today — xUnit never runs two classes of
one collection concurrently, the fixture seeds nothing, and every Abwab test creates the rows it
asserts on — and §4.4 sanctions the Abwab reset explicitly. Phase 3 verifies declared state
policies, so this is a known exception to reconcile there, not a fresh finding.

## READMEs checked, none falsified

`Backend/tests/QuranDashboard.Tests/README.md` and `Backend/scripts/README.md` were both read
against this change. Nothing they assert became untrue: the tests README's PostgreSQL 16/18 split
names `SmokeApiFixture`, `AccessTestFixture`, and the pipeline fixtures — all untouched here — and
its "keep fixture reuse local to the owning domain" invariant still holds, because only
server/schema provisioning moved out of `AbwabSchemaFixture` while its seed and reset stayed put.
The scripts README's command table does not yet list `test-backend`, `check-pending-model`, or
`cleanup-test-runtime`; that table and both READMEs are Phase 8's explicit file list, so the entries
land there rather than being written twice.

## Not in this phase

`TestSupport/PostgreSql/README.md` (Phase 8), fixture conversions beyond the Abwab pilot
(Phase 3), the staged migration and process-global work (Phase 4), the PostgreSQL 18 decision
(Phase 5), and any commit — the working tree is left staged-free for the user.
