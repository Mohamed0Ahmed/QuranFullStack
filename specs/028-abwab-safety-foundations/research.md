# Research: Abwab Safety Foundations — Fail-Closed Substrate

**Feature**: `028-abwab-safety-foundations` | **Date**: 2026-07-22 | **Source**: Master Plan §18.2

The spec is clarification-free (0 `[NEEDS CLARIFICATION]` markers). This document records the
technical decisions that realize §18.2 against the existing stack. Each decision is
constrained by §18.2 and the repository; none introduces new product scope.

## Stage 1 — CI and migration safety

- **Decision**: Build the Section 15 pipeline on **migration-based** Testcontainers
  PostgreSQL (4.4.0) — apply EF Core migrations to a fresh container, then assert
  schema compatibility — rather than `EnsureCreated`. Add a contract-drift gate and a
  reusable `@playwright/test` harness. Preserve the Vitest fork cap by keeping
  `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` in the `package.json` `test` script.
- **Rationale**: Migration-based provisioning proves the real production schema path and
  catches drift the app model would hide. The fork cap must live in `package.json` because
  `vitest.config.ts` is ignored by the `@angular/build:unit-test` builder (§18.2 step 1);
  removing it regresses CI stability.
- **Alternatives considered**: `EnsureCreated`/in-memory (rejected — bypasses migrations and
  Postgres semantics the safety tests depend on); config-file-based fork cap (rejected —
  silently ignored by the Angular unit-test builder).

## Stage 2 — Quran import safety

- **Decision**: Enumerate every destructive/force/importer path (primarily in
  `QuranDashboard.DataImporter` plus any force/reseed scripts), remove or neutralize
  `TRUNCATE ... CASCADE` effects on Abwab, add a **race-safe dependent lock/preflight**
  (advisory/row lock taken before any destructive step), restrict by environment and a
  reduced DB privilege set, and pin canonical source identity + stable-ID verification.
  Prove refusal with **real-PostgreSQL** tests using actual forbidden-source fixtures.
- **Rationale**: The first Abwab Quran FK is prohibited until these fail closed (§18.2 step
  2 / exit). A preflight that is not race-safe can still lose to a concurrent dependent
  insert; only a real-PG test proves the CASCADE and privilege behavior.
- **Alternatives considered**: Application-only guards (rejected — bypassable by direct SQL
  and races); trusting source packages without identity pinning (rejected — allows wrong
  or forbidden sources); mocked DB refusal tests (rejected — do not prove CASCADE/privilege
  behavior). Quranic fixtures stay source-safe per repo test-data rules.

## Stage 3 — Audit / timeline / write / concurrency / time kernel

- **Decision**: Implement a **tracked ChangeSet unit of work** with append-only events and
  commit-correct sequencing; a `SavingChanges` guard rejecting no-ChangeSet writes and
  physical deletes (soft-delete enforced) with a DB-level append-only/TRUNCATE defense and
  a restricted application role; singleton monotonic audit-head/revision/generation state
  using the `uint`/xmin convention with **immutable ChangeSet generation stamping**; a
  migration seed of exactly one immutable **generation-zero `TimelineGenerationBoundary`**;
  a mandatory `ExpectedTimelineGeneration` contract on every mutation port/command and
  actionable read that returns the **exact 409 before any row mutation**; a
  server-authoritative clock; and a global singleton **`AbwabWriteBarrier`** (initial
  Writable) enforced by a stabilization registry test, with post-commit cache publication
  and provider retries disabled for manual transactions.
- **Rationale**: These are the exact primitives every `029`–`034` writer builds on (§18.2
  step 3 / exit). Generation-checked-before-mutation guarantees optimistic-concurrency
  rejection with no partial writes; a registry test is the only way to prove *no* future
  writer bypasses the barrier; DB-level defenses hold even against direct SQL.
- **Alternatives considered**: Interceptor-only audit without DB append-only defense
  (rejected — bypassable); per-entity `RowVersion` only without a global generation
  (rejected — cannot express timeline-wide restore boundaries `033` needs); client clock
  (rejected — non-authoritative); EF execution-strategy retries on manual transactions
  (rejected — re-runs non-idempotent manual work, §18.2).

## Stage 4 — Shared frontend foundation

- **Decision**: Implement only the §14.1 ownership — stable DI/form **conventions** (no
  Forms package yet), generic cache/store/action/conflict primitives, IndexedDB-backed
  cache, the Playwright harness, and a **bounded synthetic-tree spike** — and nothing
  domain-specific.
- **Rationale**: The substrate is shared by all later UI Kits but must not leak `029`–`033`
  ownership. Installing `@angular/forms` or domain mocks/adapters here would freeze forward
  decisions (§18.2 step 4 / exit).
- **Alternatives considered**: Installing `@angular/forms` early "to be ready" (rejected —
  §18.2 forbids Forms-as-preparation; it arrives at Story 5); building a domain HTTP
  adapter/mock (rejected — owned by later Kits); freezing a domain DTO in the spike
  (rejected — spike measures perf/behavior only).

## Stage 5 — System Owner and permission foundation

- **Decision**: Model **immutable issuer/subject** Owner membership with enabled-account
  checks and **no email/role/runtime fallback**; serialize add/remove with a final-owner
  invariant; provide an atomic/idempotent, permanently-audited **zero-to-one bootstrap**;
  implement the **exact permission catalogue** with codes identical across
  seed/policy/`/me`/frontend/test and uniquely-keyed role/subject assignments with
  serialized race semantics; expose `/me`, cache invalidation, and policy handlers; and
  ship the Owner-only **permission-administration** port/mock + list/grant/revoke
  backend/API/HTTP/UI as one vertical slice. Add `@angular/forms` here for the real
  Reactive-Forms grant/revoke form. **Never** expose Owner *membership* administration in
  the dashboard. Keep frontend hiding demonstrably non-authoritative.
- **Rationale**: This slice proves the whole substrate end-to-end (audit, concurrency,
  `/me`, cache, real Forms) and freezes the permission primitives every later Kit's
  authorization depends on (§18.2 step 5 / exit). Backend policy is the authority; frontend
  hiding is UX only.
- **Alternatives considered**: Role/email fallback for owners (rejected — §18.2 forbids any
  fallback); non-serialized owner removal (rejected — can drop the last owner); frontend-
  enforced permissions (rejected — must be non-authoritative); mutable membership (rejected
  — must be immutable/append with serialized state).

## Stage 6 — Durable notification storage

- **Decision**: Implement a notification **recipient/source/idempotency** schema with read
  state, a **transaction-capable** persistence writer that joins a caller's domain
  transaction, and a low-level recipient/read-state repository. Enforce duplicate prevention
  by **unique source identity**. Keep read state **outside** product audit/restore. Expose
  **no** public port, endpoint, mock, HTTP adapter, or UI.
- **Rationale**: `032` needs to persist notifications inside a caller's transaction and
  `033` needs to emit restore events via this writer; both require the storage to exist
  first, deduplicated and transaction-joining, without prematurely owning surfaces (§18.2
  step 6 / exit).
- **Alternatives considered**: Separate-transaction notification write (rejected — can
  commit a notification for a rolled-back action); dedup by content hash (rejected — source
  identity is the canonical idempotency key); exposing a port/endpoint now (rejected —
  `032` owns surfaces and the event matrix).

## Cross-cutting

- **Real infrastructure everywhere it matters**: correctness of import safety, DB defenses,
  timeline-generation, and permission serialization is proven against real PostgreSQL
  (Testcontainers) and, for UI, a real browser (Playwright). In-memory substitutes are used
  only where they cannot mask a safety property.
- **Order dependency**: Stage N+1 begins only after Stage N's §18.2 exit gate passes; the
  first Abwab Quran FK stays prohibited until all exits pass.
