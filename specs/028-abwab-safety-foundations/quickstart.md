# Quickstart: Validating the Abwab Safety Foundations

**Feature**: `028-abwab-safety-foundations` | **Source**: Master Plan §18.2 exit/acceptance

This guide lists the runnable checks that prove each §18.2 exit gate. It is a **validation
guide**, not implementation. Detailed obligations live in [`contracts/`](./contracts/) and
[`data-model.md`](./data-model.md). The feature is complete only when **every** check below
passes and the first Abwab Quran foreign key is still prohibited until that point.

## Prerequisites

- .NET 10 SDK; Docker running (Testcontainers PostgreSQL 4.4.0).
- Node + the frontend workspace `Frontend/quran-dashboard-ui`.
- Backend test project: `Backend/tests/QuranDashboard.Tests` (xUnit + FluentAssertions).

## Stage 1 — CI & migration safety

```bash
# Backend: migration-based real-Postgres foundation tests + schema compatibility
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj

# Frontend: unit tests MUST run with the preserved fork cap (from package.json "test")
cd Frontend/quran-dashboard-ui && npm test            # VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2 ng test
```

**Expect**: migration-based Testcontainers stand up; schema-compatibility assertion passes;
contract-drift gate active; Playwright harness runs; the Vitest fork cap is present in
`package.json` and enforced.

## Stage 2 — Quran import safety

**Expect** (real PostgreSQL): every destructive/force/importer path enumerated; no
`TRUNCATE ... CASCADE` reaches Abwab; a concurrent dependent creation makes a destructive
import fail closed; forbidden-source and wrong-identity fixtures are refused. See
[`contracts/import-safety-contract.md`](./contracts/import-safety-contract.md). The first
Abwab Quran FK remains prohibited.

## Stage 3 — Audit / timeline / write / concurrency / time kernel

**Expect**:
- A write with **no ChangeSet** is rejected; a **physical delete** is rejected (soft-delete
  enforced); the sealed **default-deny personal-delete** exception is proven with
  foundation-only fixtures.
- Exactly **one immutable gen-zero `TimelineGenerationBoundary`** after migration; root
  edit/delete/duplicate fail.
- A stale `ExpectedTimelineGeneration` returns the **exact 409 before any row mutation**
  (including an untouched-row fixture); the contract test fails if any port/command/actionable
  read omits it.
- The **stabilization registry test fails** if any Abwab writer lacks the global
  `AbwabWriteBarrier`; cache publishes only post-commit; provider retries are off for manual
  transactions.

See [`contracts/write-kernel-barrier-contract.md`](./contracts/write-kernel-barrier-contract.md)
and [`contracts/timeline-generation-contract.md`](./contracts/timeline-generation-contract.md).

## Stage 4 — Shared frontend foundation

```bash
cd Frontend/quran-dashboard-ui && npm test        # §14.1 primitive unit tests
# Playwright: bounded synthetic-tree spike records perf/browser behavior
```

**Expect**: only §14.1 primitives (DI/form conventions, generic cache/store/action/conflict,
IndexedDB, Playwright harness); the synthetic-tree spike records bounded performance/behavior
and **freezes no domain DTO**; **no** domain mock/HTTP adapter and **no** all-domain adapter;
`@angular/forms` **not yet installed**.

## Stage 5 — System Owner & permission foundation

**Expect**:
- Concurrent owner removals always leave **≥1 active owner**; removal/disable observed on the
  next sensitive request; no email/role/runtime fallback.
- Zero-to-one bootstrap is atomic/idempotent/permanently-audited; wrong issuer, unverified
  email, disabled account, duplicate mismatched identity each fail.
- Permission codes **identical** across seed/policy/`/me`/frontend/test (0 drift);
  list/grant/revoke parity, serialization, stale-version, unauthorized, permanent-audit,
  cache-invalidation, stabilization tests pass; `/me`, policy, cache, UI converge on the
  committed winner; frontend hiding is **non-authoritative**.
- `attribution.view` baseline identical across layers; baseline removal rejected.
- `@angular/forms` is added **here** (real Reactive Forms grant/revoke); Owner **membership**
  admin is **never** in the dashboard.

See [`contracts/permission-admin-api.md`](./contracts/permission-admin-api.md).

## Stage 6 — Durable notification storage

**Expect**: the storage writer joins a caller's transaction (rolled-back caller → no row);
unique source identity prevents duplicates; read state is outside product audit/restore; **no**
public notification port/endpoint/mock/HTTP/UI is introduced. See
[`contracts/notification-storage-contract.md`](./contracts/notification-storage-contract.md).

## Final gate

- All §18.2 exit/acceptance criteria pass in CI.
- The first Abwab Quran foreign key remains **prohibited** until this feature's exit is
  accepted; `029` may add it only after acceptance.
