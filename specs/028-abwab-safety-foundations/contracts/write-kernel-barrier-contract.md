# Contract: Write Kernel, Audit, Barrier, Soft-Delete (Story 3)

**Source**: Master Plan §18.2 step 3 / exit.

## ChangeSet & audit

- Every Abwab mutation runs inside exactly **one tracked ChangeSet**; a write with **no
  ChangeSet is rejected** at `SavingChanges`.
- Each ChangeSet carries an **immutable `TimelineGeneration` stamp** and one final
  `ChangeSetSequence` **assigned from** the row-locked `AbwabRevisionState.AuditHeadSequence`
  (never the inverse). Each AuditEvent carries a deterministic **`EventOrdinal` within the
  operation**. `ChangeSetSequence` (per-operation commit coordinate) and `EventOrdinal` (order
  inside one operation) are **distinct** and MUST NOT be conflated (§6.1, §7.9).
- The migration seeds the **`AbwabRevisionState` singleton** (`AuditHeadSequence=0`,
  generation-0, `TreeRevision=0`) and the **`AbwabWriteBarrier` singleton row** (initial
  `Writable`) alongside the generation-zero boundary (§6.2, §7.1, §7.9).
- Audit events are **append-only** with **commit-correct sequencing**; no update, no
  physical delete, no `TRUNCATE` (defended at the DB via a **restricted application role**
  and an append-only/TRUNCATE defense). A **CI bypass check** guards the interceptor.

## Audit atomicity & head monotonicity

- An injected audit/event failure **rolls back all domain rows** in the same transaction —
  **no half-written ChangeSet** (§6.1, §15.3 "Audit atomicity").
- Concurrent audited commits each receive **one strictly increasing** `AuditHeadSequence`
  (both product-write locks held through commit, so a lower sequence cannot commit after a
  higher one). A **rollback leaves** `AuditHeadSequence`/`TimelineGeneration`/`TreeRevision`
  **unchanged** (§6.2, §7.1, §15.3 "Audit head").

## Forbidden-write-API bypass gate

- A CI architecture/source test **fails the build** when an Abwab writer namespace references
  `ExecuteUpdate`, `ExecuteDelete`, `ExecuteSqlRaw`, `ExecuteSqlInterpolated`, raw `DbCommand`,
  `NpgsqlConnection`, `NpgsqlCommand`, or binary COPY (§6.1 layer 2, §15.2 gate 3, §15.3
  "Bypass prevention").
- A narrow reviewed **allowlist** is permitted only for non-product, non-revertible
  infrastructure and MUST carry an **owner and reason**.
- This gate is **distinct** from the `SavingChanges` interceptor-skip check above; both exist.

## Delete policy

- Physical/hard delete is **rejected**; **soft-delete enforced**.
- A **sealed, default-deny personal-delete exception** is proven with **foundation-only
  fixture descriptors**, without depending on future workspace types. `032` later binds and
  real-PG-tests the two exact shapes.

## Write barrier

- A global singleton **`AbwabWriteBarrier`** (initial state **Writable**) gates every Abwab
  writer.
- A **stabilization registry test MUST fail** when any Abwab writer lacks the gate.
- Cache publication happens **only post-commit**.
- Provider retries are **locked off** for Abwab manual transactions.

## Time

- Server-authoritative clock (no client time) stamps ChangeSets/events.

## Test anchors

- No-ChangeSet write → rejected.
- Physical delete → rejected; soft-delete path succeeds; personal-delete exception proven
  (default-deny) with foundation fixtures.
- DB append-only/TRUNCATE defense holds under direct SQL attempt.
- Registry test fails when a writer bypasses the barrier.
- Cache publishes only after commit (not on a rolled-back tx).
- `AbwabRevisionState` singleton seeded exactly once (0 / gen-0 / 0); increments monotonically
  under row-lock; `AbwabWriteBarrier` singleton seeded `Writable`.
- Audit atomicity: injected audit/event failure → all domain rows rolled back, **0 half-written
  ChangeSets** (real PostgreSQL).
- Audit-head monotonicity: concurrent audited commits → one strictly increasing
  `AuditHeadSequence`; rollback leaves head/generation/tree unchanged.
- Bypass gate: a fixture referencing any forbidden write API fails CI; the allowlist carries an
  owner and reason — separate from the interceptor-skip check.
- A stale command whose `ExpectedTimelineGeneration` no longer matches → `abwab.timeline_generation_stale`
  (409) **before any row mutation** (see `timeline-generation-contract.md`).
