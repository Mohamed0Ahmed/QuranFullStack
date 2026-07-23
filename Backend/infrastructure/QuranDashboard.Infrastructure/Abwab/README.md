# Abwab write kernel (infrastructure) — 028 US3

**Layer:** Infrastructure · **Feature:** 028 (fail-closed substrate) · **HOW rules:**
`Backend/.architecture/CLEAN_ARCHITECTURE.md`

The infrastructure half of the Abwab write kernel: the primitives every future Abwab
domain writer (`029`–`034`) is built on, so no mutation can escape audit, the global write
barrier, generation freshness, or the server clock. Domain types live in
`QuranDashboard.Domain/Abwab/`; ports live in `QuranDashboard.Application.Abstractions/Abwab/`;
the writer registry lives at the API composition root (see below). Notification **storage**
is a sibling sub-area with its own boundary — see `Notifications/README.md`.

## What is here

- `Persistence/AbwabWriteGuardInterceptor` — **layer 1**, the `SavingChanges` guard. Before
  every `SaveChanges` it inspects the tracked graph and rejects: a mutation of any
  `IAbwabAuditable` with **no tracked `ChangeSet`** in the same unit of work
  (`AbwabWriteWithoutChangeSetException`), and any **physical delete** of an `IAbwabAuditable`
  (`AbwabPhysicalDeleteRejectedException`) unless the sealed personal-delete policy allows that
  exact CLR type. This runs at execution time; it is **distinct** from the build-time
  forbidden-write-API bypass gate (a source/architecture test).
- `Persistence/AbwabPersonalDeletePolicy` — the **sealed, default-deny** personal-delete
  exception. 028 ships it empty (deny all); `032` binds the two exact personal-data shapes.
  The allowlist is fixed at construction and cannot be widened at runtime, so the exception can
  never be quietly broadened into a general hard-delete path.
- `Persistence/AbwabAuditedCommitExecutor` — **layer 2**, the barrier-gated audited-commit
  protocol (`IAbwabWriteExecutor`). One manual transaction with provider retries locked off;
  order is load-bearing: `FOR UPDATE` row-lock + evaluate the `AbwabWriteBarrier` (fail closed
  unless `Writable`) → `FOR UPDATE` row-lock the `AbwabRevisionState` singleton → verify
  `ExpectedTimelineGeneration` **before any mutation** (exact 409, zero rows touched) → advance
  `AuditHeadSequence` by one and assign `ChangeSetSequence` from it → append the `ChangeSet` +
  events, save, commit → publish caches **only** after commit. Both locks are held through
  commit, so concurrent commits get one strictly increasing head and a rollback leaves
  head/generation/tree unchanged.
  - **`029` addition**: a second `ExecuteAsync(AbwabAuditedOperationRequest, ct)` overload runs a
    caller-supplied operation delegate **inside** the same two locks, after the generation check
    and before any ChangeSet is written. It hands the delegate the locked `AbwabRevisionState` so
    a structural writer can validate/bump `TreeRevision` atomically with its own entity
    mutations, and supports an idempotent `NoOp` outcome (protection apply at an unchanged scope)
    that commits the transaction **without** writing a ChangeSet. The original single-shot
    `ExecuteAsync(AbwabWriteRequest, ct)` is untouched.
  - `LockSingletonAsync` reloads the barrier/revision entity after acquiring its lock. EF's
    identity resolution otherwise returns an already-tracked instance's stale in-memory values
    instead of the freshly locked row — harmless when each caller gets its own `DbContext`, but
    wrong the moment one scope (e.g. a test harness, or a future multi-command flow) issues two
    writes through the same context. The reload makes the lock's post-acquisition truth
    observable regardless of prior tracking state.
- `Time/ServerClock` — `IServerClock`, server-authoritative `UtcNow` (never client time).
- `Caching/NullAbwabCachePublisher` — the post-commit publication seam. 028 has no Abwab caches
  to invalidate yet; the hook already exists (called only after commit) so `029`+ bind real
  caches without moving it.
- `Persistence/InertDeletionReservationChecker` — **`029` addition**: the default
  `IDeletionReservationChecker` a subtree-delete handler consults before proceeding. Always reports
  "not reserved" — `032` installs the Pending-aware replacement that can map to
  `abwab.category_reserved_by_pending`; the seam exists precisely so that swap never touches the
  delete handler itself.
- `AbwabKernelDependencyInjection` — composes the clock, the null cache publisher, and the
  audited-commit executor. The **stabilization writer registry** (`AbwabWriterRegistry` /
  `AbwabWriterStabilizationGuard` in `Application/Abwab/Concurrency/`) is wired at the **API**
  composition root, because it must see the Application writer types Infrastructure does not
  reference; writer discovery is therefore **assembly-scoped** (Application + Api).

The DB shape (exactly one immutable gen-zero `TimelineGenerationBoundary`, the
`AbwabRevisionState` and `AbwabWriteBarrier` singletons, immutable ChangeSet stamping, the
append-only/TRUNCATE trigger defense, and the restricted `abwab_app` role) is seeded by the
`AddAbwabSafetyKernel` migration. EF configs live in `Persistence/Configurations/Abwab/`.

## Invariants / caveats (read before changing)

- **Two layers, both required.** Never let an Abwab mutation reach the DB without both the
  `SavingChanges` guard (ChangeSet + soft-delete) and the audited-commit executor (barrier +
  generation + append-only head). Removing either is a fail-open regression.
- **Row-lock, not xmin, for `AbwabRevisionState` specifically.** Its concurrency is enforced by
  the mandated `FOR UPDATE` row-lock, not an EF concurrency token; the singleton has no `Version`
  property. This scoped rule is about the model-wide `UseXminAsConcurrencyToken()` convenience
  convention, which Npgsql 10 removed — **not** about per-property xmin mapping. Ordinary
  per-row `029`+ entities (e.g. `Section`, `Category`, `CategorySearchAlias`) correctly carry a
  `uint Version` property mapped explicitly (`HasColumnName("xmin").HasColumnType("xid")
  .ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()`) and DOES throw
  `DbUpdateConcurrencyException` if EF's own optimistic-concurrency check ever fires — but the
  `029` US3 writers do not rely on that path for `abwab.row_stale`: because every writer already
  runs inside the same global barrier/revision lock, a genuine concurrent write mid-operation is
  impossible, so each handler instead does an **explicit** `entity.Version != ExpectedVersion`
  comparison against the freshly loaded row (under the lock) and throws
  `AbwabWriteConflictException(abwab.row_stale)` directly — a deliberate, simpler, race-safe
  check, not a fallback (exercised by the US3 concurrency tests, T040). The `xmin` concurrency
  token stays configured as defense-in-depth for any future code path that bypasses the handler
  layer. Do not add a bare `Version` property to
  `AbwabRevisionState` expecting the same — its concurrency guarantee is the row-lock above.
- **Row locks use a read API.** The `FOR UPDATE` locks go through `FromSqlRaw` (a read API), not
  a forbidden write/bypass API, so the bypass gate stays green. Keep it that way.
- **Append-only is enforced in the DB.** The `abwab_app` role is NOLOGIN defense-in-depth; the
  live guarantee is the append-only trigger. Do not rely on the role alone.
- **No Abwab→Quran foreign key.** Prohibited until 028 is accepted (FR-009); the
  `NoPrematureQuranFkTests` guard must stay green.

## Related

- Domain: `QuranDashboard.Domain/Abwab/README.md` (kernel types in `{Audit,Timeline,Concurrency,
  Persistence}/`; the `029` product model in `{Sections,Categories,Protection,Tree}/`).
- Ports/contracts: `QuranDashboard.Application.Abstractions/Abwab/README.md`.
- Stabilization registry: `QuranDashboard.Application/Abwab/Concurrency/`.
- `029` restore adapters (this project): `Restore/README.md`. `029` read ports:
  `Persistence/Reads/Abwab/README.md`.
- Security-audit separation (permission/owner writes): `Backend/api/QuranDashboard.Api/Security/README.md`.
- Notification storage sub-area: `Notifications/README.md`.
- Verified by real-PostgreSQL tests under `Backend/tests/QuranDashboard.Tests/Abwab/Kernel/`.
