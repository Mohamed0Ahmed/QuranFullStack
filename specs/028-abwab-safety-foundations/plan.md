# Implementation Plan: Abwab Safety Foundations — Fail-Closed Substrate

**Branch**: `028-abwab-safety-foundations` | **Date**: 2026-07-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/028-abwab-safety-foundations/spec.md`

> **Canonical source**: This plan realizes **Master Plan §18.2 only** (`028` — fail-closed
> substrate) plus its entry/exit gates. Section pointers (§15, §14.1, §16.3, §5, §6.x)
> are owned by those Master Plan sections and are referenced here, not re-decided. Where a
> perceived conflict arises, the Master Plan governs. Technical-stack details below come
> from the actual repository (they are the *how* of realizing §18.2, not new product
> decisions).

## Summary

`028-abwab-safety-foundations` builds the **fail-closed safety substrate** that must exist
before any Abwab domain writer or first Quran foreign key. It is delivered as six stages in
§18.2's **mandatory internal order**: (1) CI and migration safety, (2) Quran import safety,
(3) the audit / timeline / write / concurrency / time kernel, (4) the shared frontend
foundation, (5) the System Owner and permission foundation (one security vertical slice),
and (6) durable notification storage. The technical approach is *prove-before-permit*: each
stage lands its guarantees and CI tests against real infrastructure (real PostgreSQL via
Testcontainers, real browser via Playwright) before the next stage begins, and the first
Abwab Quran FK stays prohibited until every §18.2 exit criterion passes.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`) backend; TypeScript 5.9 / Angular 20.3
standalone + Signals frontend.

**Primary Dependencies**: Backend — EF Core 10.0.8, Npgsql.EntityFrameworkCore.PostgreSQL
10.0.0, Testcontainers.PostgreSql 4.4.0, xUnit 2.9.3, FluentAssertions 8.2.0. Frontend —
Angular 20.3 (`@angular/build` 20.3.27), RxJS 7.8, Vitest 3.2.6. Added *during* this
feature: `@playwright/test` (Story 1 harness) and IndexedDB access (Story 4); `@angular/forms`
is added only at Story 5 (real Reactive Forms first use), never earlier.

**Storage**: PostgreSQL (append-only audit events, soft-delete, singleton monotonic
timeline-generation/audit-head state, restricted application-role privileges); browser
IndexedDB for the generic frontend cache primitive (Story 4).

**Testing**: Backend — xUnit against **real PostgreSQL** (migration-based Testcontainers),
schema-compatibility assertion, contract-drift gate, real-source refusal fixtures. The
Section 15 CI pipeline (schema-compat + contract-drift gates, real-PG job, Playwright job)
is wired in the repo CI workflow under `.github/workflows/`. Frontend
— Vitest unit tests with the mandatory fork-concurrency cap
(`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` in `package.json`, because `vitest.config.ts` is
ignored by the `@angular/build:unit-test` builder) plus a reusable Playwright harness.

**Target Platform**: Linux server (Railway auto-deploy from `main`) for the backend;
browser SPA for the frontend.

**Project Type**: Web application — .NET Clean Architecture backend
(`Domain`/`Application(.Abstractions)`/`Infrastructure`/`Api`/`Shared`, plus the
`QuranDashboard.DataImporter` tool) + Angular SPA (`core`/`shared`/`features`).

**Performance Goals**: The Story 4 bounded synthetic-tree spike records bounded browser
performance and behavior only; no Abwab domain numeric budget is frozen here (owned by
`029`–`033`, §15.3).

**Constraints**: Fail-closed. No Abwab domain writer and no first Quran FK may exist until
exit (§18.2). Server-authoritative clock (no client time). Provider retries locked off for
Abwab manual transactions. Append-only + soft-delete enforced at the database. Global
`AbwabWriteBarrier` gate on every writer. Every mutation port/command and actionable read
carries `ExpectedTimelineGeneration`. Quranic test data must remain source-safe. API
boundary follows the `ApiResponse` contract / `Backend/.architecture/API_GUIDELINES.md`.

**Scale/Scope**: Substrate + exactly one security vertical slice (Owner membership +
permission list/grant/revoke) + notification **storage** only. No Abwab domain surfaces, no
notification surfaces (owned by `032`), no restore surfaces (owned by `033`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is an unfilled template with no ratified principles, so it
imposes no project-specific gates. Governing gates for this feature come from the repository
governance (`CLAUDE.md`, `CODING_PRINCIPLES.md`, `Backend/.architecture/*`) and Master Plan
§18.2:

- **Clean Architecture layering** — Domain has no outward dependency; Infrastructure owns
  persistence/EF; Api owns HTTP; the write kernel, barrier, and permission port respect
  `CLEAN_ARCHITECTURE.md` / `BACKEND_STRUCTURE.md` boundaries. ✅ PASS (enforced by design in
  Phase 1; layering test in Story 3).
- **API contract** — grant/revoke/`/me` endpoints return the `ApiResponse` shape and the
  exact `abwab.*`/409 conflict codes per `API_GUIDELINES.md`. ✅ PASS (contracts in Phase 1).
- **Fail-closed / no downstream leak** — no Abwab domain writer, no first Quran FK, no
  notification/restore surfaces here; ownership of `029`–`034` is not performed. ✅ PASS
  (scope boundaries in spec + this plan).
- **Mandatory order** — the six stages are built in §18.2 order; later stages depend on
  earlier exit gates. ✅ PASS (sequencing in tasks phase).
- **Quranic data safety** — import-safety fixtures and any Quran-touching test data stay
  source-safe. ✅ PASS (research + quickstart).
- **Comment sparingly / clean-code + test-guard self-checks** — applied before delivery. ✅
  PASS (process gate).

No unjustified violations. Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/028-abwab-safety-foundations/
├── plan.md                              # This file (/speckit-plan output)
├── spec.md                              # Feature spec (derived from §18.2)
├── research.md                          # Phase 0 output — decisions for each stage
├── data-model.md                        # Phase 1 output — substrate entities & invariants
├── quickstart.md                        # Phase 1 output — exit-gate validation guide
├── contracts/
│   ├── permission-admin-api.md          # Owner-only list/grant/revoke + /me contract
│   ├── timeline-generation-contract.md  # ExpectedTimelineGeneration + 409 contract
│   ├── write-kernel-barrier-contract.md # ChangeSet/append-only/soft-delete/barrier contract
│   ├── import-safety-contract.md        # Destructive-path lockdown + source-identity refusal
│   └── notification-storage-contract.md # Durable storage writer (no public surface)
└── checklists/
    └── requirements.md                  # Spec quality checklist (from /speckit-specify)
```

### Source Code (repository root)

```text
Backend/
├── domain/QuranDashboard.Domain/                 # ChangeSet, audit event, timeline boundary,
│                                                 #   Owner membership, permission catalogue,
│                                                 #   AbwabWriteBarrier state (pure domain)
├── application/QuranDashboard.Application.Abstractions/  # ports: permission-admin, notification
│                                                 #   storage writer, clock, write barrier
├── application/QuranDashboard.Application/        # handlers: bootstrap, grant/revoke, /me,
│                                                 #   timeline-generation guard, soft-delete rules
├── infrastructure/QuranDashboard.Infrastructure/  # EF config, migrations (gen-zero seed,
│                                                 #   append-only/TRUNCATE defense), SavingChanges
│                                                 #   guard, ChangeSet UoW, notification repo,
│                                                 #   restricted DB role, server clock
├── api/QuranDashboard.Api/                        # Owner-only permission endpoints, /me,
│                                                 #   ApiResponse + abwab.* 409 mapping
├── tools/QuranDashboard.DataImporter/             # destructive-path lockdown, dependent
│                                                 #   lock/preflight, source-identity verification
└── tests/QuranDashboard.Tests/                    # xUnit + Testcontainers real-PG foundation tests

Frontend/quran-dashboard-ui/src/app/
├── core/                                          # §14.1 primitives: caching (IndexedDB),
│   ├── caching/                                   #   data-access store/action/conflict, DI
│   ├── data-access/                               #   conventions, api client
│   └── auth/                                       #   permission /me consumption (non-authoritative)
├── shared/                                        # generic conflict/action primitives, form conv.
└── features/                                      # permission-admin (Owner-only) grant/revoke UI
                                                   #   (real Reactive Forms — Story 5)

Frontend/quran-dashboard-ui/
├── package.json                                   # VITEST_MIN_FORKS/MAX_FORKS cap; @angular/forms
│                                                 #   added at Story 5; @playwright/test at Story 1
└── e2e/ (Playwright)                              # reusable harness + bounded synthetic-tree spike
```

**Structure Decision**: Web application. Backend follows the existing Clean Architecture
projects (`Domain` → `Application(.Abstractions)` → `Infrastructure`/`Api`, plus the
`QuranDashboard.DataImporter` tool for import safety). Frontend extends the existing
`core`/`shared`/`features` tree with the §14.1 generic primitives and the single Owner-only
permission-admin feature. No new top-level project is introduced.

## Complexity Tracking

No Constitution Check violations. Section intentionally empty.
