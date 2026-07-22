# Contract: Write Kernel, Audit, Barrier, Soft-Delete (Story 3)

**Source**: Master Plan §18.2 step 3 / exit.

## ChangeSet & audit

- Every Abwab mutation runs inside exactly **one tracked ChangeSet**; a write with **no
  ChangeSet is rejected** at `SavingChanges`.
- Audit events are **append-only** with **commit-correct sequencing**; no update, no
  physical delete, no `TRUNCATE` (defended at the DB via a **restricted application role**
  and an append-only/TRUNCATE defense). A **CI bypass check** guards the interceptor.

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
