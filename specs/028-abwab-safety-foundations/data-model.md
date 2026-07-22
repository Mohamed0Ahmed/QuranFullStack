# Data Model: Abwab Safety Foundations — Fail-Closed Substrate

**Feature**: `028-abwab-safety-foundations` | **Date**: 2026-07-22 | **Source**: Master Plan §18.2

This model covers only the **substrate** entities §18.2 mandates. No Abwab domain entity
(section, category, tree node, relationship, attribution link) and no Quran foreign key are
defined here — those are owned by `029`–`033`, and the first Abwab Quran FK is prohibited
until this feature's exit.

## Kernel entities (Story 3)

### ChangeSet (tracked unit of work)

- **Fields**: `Id`, immutable `TimelineGeneration` stamp (captured at creation),
  `ChangeSetSequence` (the commit-order coordinate, **assigned from** the row-locked
  `AbwabRevisionState.AuditHeadSequence` at commit — never inverse-restored), actor/subject,
  created-at (server clock), correlation to the emitted audit events.
- **Rules**: Every Abwab mutation MUST run inside exactly one ChangeSet; a write with no
  ChangeSet is rejected at `SavingChanges`. The `TimelineGeneration` stamp is **immutable**
  once set (§7.9). One completed product-audited operation has exactly one final,
  commit-correct `ChangeSetSequence` (§6.1).
- **Relationships**: 1 ChangeSet → N append-only AuditEvents.

### AuditEvent (append-only)

- **Fields**: `Id`, `EventOrdinal` (deterministic ordering **within the operation** — not the
  cross-operation commit coordinate), `ChangeSetId`, event payload, server timestamp.
- **Rules**: Append-only — no update, no physical delete, no `TRUNCATE` (enforced at the DB
  via the application-role privilege + append-only/TRUNCATE defense). `ChangeSetSequence`
  (per operation, from `AuditHeadSequence`) and `EventOrdinal` (within an operation) are
  **distinct** coordinates and MUST NOT be conflated (§6.1, §7.9).
- **State transitions**: created → (never mutated / never deleted).

### AbwabRevisionState (singleton technical state)

- **Fields**: `AuditHeadSequence`, `TimelineGeneration`, `TreeRevision`, and `Version` (`xmin`).
- **Rules**: **Exactly one row is seeded at migration** with `AuditHeadSequence = 0`,
  generation-0, and `TreeRevision = 0`. It is **row-locked by the commit protocol** (§6.2 step
  4, immediately after the `AbwabWriteBarrier` gate), and `ChangeSetSequence` is assigned
  **from** `AuditHeadSequence` (never the inverse) as the barrier-held head advances by one per
  successful audited commit. These counters are **current concurrency/reconciliation state, not
  product snapshot values, and are never inverse-restored**; a rollback leaves all three
  unchanged (§7.1). A successful Restore ChangeSet advances the head like any audited operation
  and a new generation never resets it.
- **Relationships**: singleton; its `AuditHeadSequence` feeds each ChangeSet's
  `ChangeSetSequence`.
- **State transitions**: seeded (0 / gen-0 / 0) → monotonically advanced under row-lock.

### TimelineGenerationBoundary (singleton timeline state)

- **Fields**: `Generation` (monotonic `uint`, xmin convention), `IsRoot`, boundary metadata.
- **Rules**: Migration seeds **exactly one immutable generation-zero root**. Root
  edit/delete/duplicate MUST fail. Non-root boundaries may be inserted **only** by `033`'s
  sealed restore transaction. Uniqueness enforced.
- **State transitions**: gen-zero root seeded → (advance to gen N only via sanctioned
  restore path in `033`).

### AbwabWriteBarrier (global gate)

- **Fields**: singleton state ∈ {Writable, …}, initialized **Writable**.
- **Rules**: The **singleton barrier row is seeded at migration in the `Writable` state**,
  alongside the `AbwabRevisionState` singleton and the generation-zero boundary. Every Abwab
  writer MUST lock and evaluate the barrier first (§6.2 step 3), then the `AbwabRevisionState`
  head (§6.2 step 4). A stabilization registry test MUST fail if any writer lacks the gate.
  Cache publication happens **post-commit**; provider retries are locked off for Abwab manual
  transactions.

### ExpectedTimelineGeneration (concurrency contract, not a table)

- **Carried by**: every mutation port/command and every actionable read.
- **Rules**: A stale expected-generation MUST cause the **exact 409 before any row
  mutation**. A foundation contract/source test fails if any port/command/actionable read
  omits it. See `contracts/timeline-generation-contract.md`.

### Soft-delete + personal-delete exception

- **Rules**: Physical/hard delete is rejected everywhere; soft-delete is enforced. A
  **sealed, default-deny** personal-delete exception is proven with foundation-only fixture
  descriptors, without depending on future workspace types.

## Ownership & permission entities (Story 5)

### SystemOwnerMembership

> **Naming**: `SystemOwnerMembership` is a permitted **local** implementation name for the
> Master Plan's `SystemOwner`/`SystemOwners` entity. Per §20.1, class/file names are ordinary
> local mechanics that do not alter the locked contract; the composite `(Issuer, Subject)`
> identity and security-audit semantics are the Master Plan's, unchanged.

- **Fields**: immutable `Issuer` + `Subject` identity, enabled/disabled account state,
  audit linkage.
- **Rules**: Immutable membership; **no email/role/runtime fallback**. Add/remove is
  serialized; a **final-owner invariant** guarantees ≥1 active owner survives concurrent
  removals. Removal/disable observed on the next sensitive request. Administered **outside**
  the dashboard.
- **Bootstrap**: atomic, idempotent, permanently-audited zero-to-one bootstrap; fails on
  wrong issuer, unverified bootstrap email, disabled account, or duplicate mismatched
  identity.

### PermissionCatalogue / PermissionCode

- **Fields**: exact permission `code`, metadata (e.g. `SystemOwnerOnly`,
  `DashboardAdminBaseline`), assignability.
- **Rules**: Codes are **identical across seed / policy / `/me` / frontend / test**
  catalogues (0 drift). Baseline codes (e.g. `attribution.view`) cannot be removed.

### PermissionAssignment (role / direct)

- **Fields**: unique key over (role|subject, permission code), version, audit linkage.
- **Rules**: Uniquely keyed; serialized race semantics (first-grant and grant-vs-revoke
  serialization); idempotent no-op grants/revokes produce **no audit**; stale-version and
  unauthorized attempts fail; grants are permanently audited; cache invalidation on commit.
- **Projection**: `/me`, backend policy, cache, and UI converge on the **committed winner**;
  frontend hiding is **non-authoritative**.

## Notification storage entities (Story 6)

### NotificationRecord

- **Fields**: recipient, **source identity** (idempotency key), payload, created-at.
- **Rules**: Written **inside a caller's domain transaction**; **unique source identity**
  prevents duplicates. No public port/endpoint/mock/HTTP/UI in this feature.

### NotificationReadState

- **Fields**: recipient, notification reference, read/unread state.
- **Rules**: Kept **outside** product audit/restore. Low-level repository only.

## Invariant summary (verification anchors)

| Invariant | Entity | Enforced by | Story |
|-----------|--------|-------------|-------|
| Write requires ChangeSet | ChangeSet | `SavingChanges` guard | 3 |
| Append-only, no physical delete/TRUNCATE | AuditEvent | DB role + defense | 3 |
| Singleton head seeded once (0/gen-0/0), monotonic under row-lock | AbwabRevisionState | migration seed + row-lock | 3 |
| `ChangeSetSequence` from `AuditHeadSequence`; `EventOrdinal` per operation (distinct) | ChangeSet / AuditEvent | commit protocol | 3 |
| Audit atomicity: rollback leaves head/generation/tree unchanged | AbwabRevisionState | transactional rollback test | 3 |
| Barrier singleton seeded `Writable` | AbwabWriteBarrier | migration seed | 3 |
| Exactly one immutable gen-zero root | TimelineGenerationBoundary | migration + forbidden-edit tests | 3 |
| Stale generation → exact 409 pre-mutation | ExpectedTimelineGeneration | contract test | 3 |
| Every writer passes the gate | AbwabWriteBarrier | stabilization registry test | 3 |
| ≥1 active owner always | SystemOwnerMembership | serialized removal + final-owner invariant | 5 |
| Atomic/idempotent audited bootstrap | SystemOwnerMembership | zero-to-one bootstrap | 5 |
| Permission codes identical across 5 catalogues | PermissionCatalogue | parity tests | 5 |
| Baseline `attribution.view` unremovable | PermissionCode | policy + removal-rejection test | 5 |
| Dedup by unique source identity, tx-joined | NotificationRecord | unique index + tx writer | 6 |
| Read state outside audit/restore | NotificationReadState | schema boundary | 6 |
