# Implementation Plan: Abwab Core — Sections, Categories, Tree, and Protection

**Branch**: `029-abwab-core` | **Date**: 2026-07-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/029-abwab-core/spec.md`

> **Canonical source**: This plan realizes **Master Plan §18.3 only** (`029` — sections,
> categories, tree, and protection) plus its entry/exit gates and the §17 portfolio exit
> condition for `029`. Section pointers (§5.1, §6.3, §7.1, §7.2, §9, §11, §14.1) are owned by
> those Master Plan sections and are referenced here, not re-decided. Where a perceived
> conflict arises, the Master Plan governs. Technical-stack details below come from the actual
> repository and the accepted `028` substrate (they are the *how* of realizing §18.3, not new
> product decisions).

## Summary

`029-abwab-core` builds the **first Abwab domain vertical slice** on the accepted `028`
substrate, in §18.3's strict order: **schema/read model → protection → writers → frontend
slice**. It is delivered as four stages: (1) the Section/Category/Alias/revision schema with a
seeded permanent default section and read/search/snapshot only; (2) ManualProtection storage
plus ordinary-protection fields and the direct/inherited resolver, with the ManualProtection
adapter accepted *before* any protected writer; (3) the tracked section/category writers of the
§9 matrix on one audited unit of work — names/ordering/ancestry, move cycle guards, atomic
subtree delete/operation-restore, protection gating, and exact `abwab.*` 409s; (4) the domain
frontend vertical slice — core port/mock, HTTP mapping, parity suite, tree/search/editor/
protection UI, cache rules, and the §6.3 audit render payloads. The technical approach is
*read-before-protect, protect-before-write, backend-authoritative*: each stage lands its
guarantees and CI tests against real infrastructure (real PostgreSQL via Testcontainers, real
browser via Playwright) before the next begins, there is **no drag-and-drop**, and the **three**
registered restore adapters (Section, Category — incl. order + subtree delete/operation-restore —
and ManualProtection; Order is a §8 facet, not a fourth registration) are versioned and accepted
for `033`.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`) backend; TypeScript 5.9 / Angular 20.3
standalone + Signals frontend.

**Primary Dependencies**: Backend — EF Core 10.0.8, Npgsql.EntityFrameworkCore.PostgreSQL
10.0.0, Testcontainers.PostgreSql 4.4.0, xUnit 2.9.3, FluentAssertions 8.2.0. Frontend —
Angular 20.3 (`@angular/build` 20.3.27), RxJS 7.8, Vitest 3.2.6, `@playwright/test`. Reused from
the accepted `028` substrate (not re-installed here): the tracked-ChangeSet UoW,
`AbwabWriteBarrier`, `ExpectedTimelineGeneration` contract, server clock, the §14.1 generic
frontend primitives (cache/store/action/conflict, IndexedDB), the Playwright harness, and
`@angular/forms` (Reactive Forms — the category editors reuse the same package the `028`
permission-administration form installed and tested; `029` installs no new forms substrate).

**Storage**: PostgreSQL. New Abwab domain tables: Section, Category, CategorySearchAlias,
ManualProtection, plus the versioned restore-snapshot adapters. Soft-delete + append-only audit
and the singleton `AbwabRevisionState` (`TreeRevision`/`AuditHeadSequence`/`TimelineGeneration`)
are the `028` substrate; `029` bumps `TreeRevision` once per atomic structural operation.
`RepresentativeQuranExcerpt` is a **plain string column — no Quran foreign key** (the first
Abwab Quran FK remains owned by later Kits).

**Testing**: Backend — xUnit against **real PostgreSQL** (migration-based Testcontainers):
uniqueness/normalization, tree shape/order/ancestry, deep-tree protection query budget, writer
concurrency (move vs reorder, five-type preset stale rollback), subtree delete/operation-restore
atomicity, versioned adapter round-trips, composite-read redaction. The published §5.1
normalization fixture corpus is shared across backend/db/API/frontend parity tests. Frontend —
Vitest unit tests with the preserved fork cap (`VITEST_MIN_FORKS`/`VITEST_MAX_FORKS` in
`package.json`) plus the reusable Playwright harness (mock/HTTP parity, stale-cache, rollback,
RTL keyboard/focus, large-tree, explicit action, no-edit-session-lock, **no-drag**, post-mutation
context preservation).

**Target Platform**: Linux server (Railway auto-deploy from `main`) for the backend; browser SPA
for the frontend.

**Project Type**: Web application — .NET Clean Architecture backend
(`Domain`/`Application(.Abstractions)`/`Infrastructure`/`Api`/`Shared`) + Angular SPA
(`core`/`shared`/`features`).

**Performance Goals**: A measured **deep-tree protection-resolution query budget** against real
PostgreSQL (Stage 2) and a **large-tree** browser interaction budget exercised by the Playwright
suite (Stage 4). No new numeric budgets are invented beyond what §18.3 requires; the `028`
synthetic-tree spike informs the large-tree UI target.

**Constraints**: Backend-authoritative. Mandatory internal order (schema/read → protection →
writers → frontend); no mutation endpoint/editable UI at the Stage 1 checkpoint; the
ManualProtection adapter is accepted before any protected writer. Every writer runs on one
audited ChangeSet carrying `ExpectedTimelineGeneration`/expected `xmin`/expected `TreeRevision`
and passes the `AbwabWriteBarrier`. **No drag-and-drop.** Composite-read redaction is enforced by
**backend DTO projection, not frontend hiding**. Exact `abwab.*` 409 codes only (§11). Server
clock for expiry. Quranic test data stays source-safe. API boundary follows the `ApiResponse`
contract / `Backend/.architecture/API_GUIDELINES.md`. **No forward schema dependency** on
relationships/links: the dependent-visibility seam uses a core fixture.

**Scale/Scope**: Sections + categories + tree + protection only. Excludes relationships/templates
(`030`), attribution/Quran links + sources (`031`), workspace/review/notification surfaces
(`032`), the audit-restore read model/planner/execution (`033`), and realtime hardening (`034`).
Category deletion exposes an inert reservation-checker seam only; `032` installs the Pending-aware
checker before Submit.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is an unfilled template with no ratified principles, so it
imposes no project-specific gates. Governing gates come from repository governance (`CLAUDE.md`,
`CODING_PRINCIPLES.md`, `Backend/.architecture/*`) and Master Plan §18.3:

- **Clean Architecture layering** — Domain holds Section/Category/Alias/ManualProtection
  entities and invariants with no outward dependency; Application owns handlers/ports;
  Infrastructure owns EF config/migrations/adapters; Api owns HTTP + `ApiResponse`/`abwab.*`
  mapping. ✅ PASS (enforced by design in Phase 1; layering test in Stage 3).
- **API contract** — section/category/protection endpoints return the `ApiResponse` shape and
  the exact `abwab.*`/409 codes per §11 / `API_GUIDELINES.md`; every mutation DTO carries
  `ExpectedTimelineGeneration`, every actionable read carries `TimelineGeneration`. ✅ PASS
  (contracts in Phase 1).
- **Backend-authoritative reads / no leak** — composite-read redaction of
  type/scope/actor/source-ancestor is a backend DTO projection; frontend hiding is
  non-authoritative. ✅ PASS (composite-read contract).
- **Mandatory order** — the four stages build in §18.3 order; no mutation surface before the read
  model, no writer before the accepted ManualProtection adapter, no frontend slice before the
  writers. ✅ PASS (sequencing in the tasks phase).
- **No drag-and-drop** — all ordering/moves are explicit actions; a no-drag source/browser gate
  is preserved from `028`. ✅ PASS (frontend gate + browser test).
- **Quranic data safety** — `RepresentativeQuranExcerpt` is a plain string with no Quran FK and
  no full-ayah validation; the §5.1 fixture corpus and any Quran-touching test data stay
  source-safe. ✅ PASS (research + data-model).
- **No forward dependency** — subtree delete/restore uses a generic RESTRICT/no-cascade +
  dependent-visibility seam via a core fixture; no relationship/link schema is referenced. ✅
  PASS (Stage 3 design).
- **Comment sparingly / clean-code + test-guard self-checks** — applied before delivery. ✅ PASS
  (process gate).

No unjustified violations. Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/029-abwab-core/
├── plan.md                              # This file (/speckit-plan output)
├── spec.md                              # Feature spec (derived from §18.3)
├── research.md                          # Phase 0 output — decisions for each stage
├── data-model.md                        # Phase 1 output — domain entities & invariants
├── quickstart.md                        # Phase 1 output — exit-gate validation guide
├── contracts/
│   ├── tree-read-contract.md            # AbwabTreeSnapshot read/search/snapshot + composite-read redaction
│   ├── sections-api.md                  # Section read/add/edit/reorder/delete-empty + permanent-default
│   ├── categories-api.md                # Category add/edit/single+bulk move/reorder/subtree-delete/restore/search
│   ├── manual-protection-contract.md    # ManualProtection apply/lift/full-preset + resolver + soft-deleted access
│   ├── restore-adapters-contract.md     # Three versioned adapters: Section, Category (order facet), ManualProtection (accepted for 033)
│   └── audit-render-contract.md         # §6.3 category/bulk-move/subtree-delete/ordering/manual-protection payloads
└── checklists/
    └── requirements.md                  # Spec quality checklist (from /speckit-specify)
```

### Source Code (repository root)

```text
Backend/
├── domain/QuranDashboard.Domain/                 # Section, Category, CategorySearchAlias,
│                                                 #   ManualProtection entities; root/descendant
│                                                 #   shape, normalized-name uniqueness, order/
│                                                 #   ancestry rules, ordinary-protection fields
├── application/QuranDashboard.Application.Abstractions/  # core port (tree/search/section/category/
│                                                 #   protection), reservation-checker seam,
│                                                 #   versioned restore-adapter interfaces
├── application/QuranDashboard.Application/        # handlers on one audited UoW: section+category
│                                                 #   actions (§9), move cycle guards, subtree
│                                                 #   delete/operation-restore, protection resolver,
│                                                 #   composite-read projection, full-preset command
├── infrastructure/QuranDashboard.Infrastructure/  # EF config + migration (Section/Category/Alias/
│                                                 #   ManualProtection, seeded permanent default
│                                                 #   section, filtered unique indexes); Section/
│                                                 #   Category/Order/ManualProtection restore adapters
├── api/QuranDashboard.Api/                        # sections/categories/tree/manual-protection
│                                                 #   endpoints; ApiResponse + exact abwab.* 409 map
└── tests/QuranDashboard.Tests/                    # xUnit + Testcontainers real-PG domain tests

Frontend/quran-dashboard-ui/src/app/
├── core/                                          # reuse §14.1 primitives (cache/store/action/
│                                                 #   conflict, IndexedDB) from 028; core cache keys
├── shared/                                        # reused generic conflict/action primitives
└── features/abwab/                                # core domain slice: core port + core mock,
                                                   #   HTTP adapter, tree/search/editor/protection
                                                   #   UI (Reactive Forms editors reuse 028's
                                                   #   @angular/forms), parity suite, no-drag

Frontend/quran-dashboard-ui/
└── e2e/ (Playwright)                              # core browser suite: mock/HTTP parity, stale-
                                                   #   cache, rollback, RTL keyboard/focus, large-
                                                   #   tree, explicit action, no-drag, context-preserve
```

**Structure Decision**: Web application. Backend extends the existing Clean Architecture projects
(`Domain` → `Application(.Abstractions)` → `Infrastructure`/`Api`) with the Abwab category domain;
Frontend adds a single `features/abwab` domain vertical slice on the reused `028` `core`/`shared`
primitives. No new top-level project is introduced; `@angular/forms` is reused, not re-installed.

## Complexity Tracking

No Constitution Check violations. Section intentionally empty.
