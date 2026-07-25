# Implementation Plan: Abwab Relationships and Templates — Category Adjuncts

**Branch**: `030-abwab-relationships-templates` | **Date**: 2026-07-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/030-abwab-relationships-templates/spec.md`

> **Canonical source**: This plan realizes **Master Plan §18.4 only** (`030` — category
> relationships and door templates) plus its entry/exit gates and the §17 portfolio exit
> condition for `030`. Section pointers (§5.2, §6.3, §6.4, §6.6, §7.3, §7.4, §8, §9, §11,
> §14.1, §15.1) are owned by those Master Plan sections and are referenced here, not
> re-decided. Where a perceived conflict arises, the Master Plan governs. Technical-stack
> details below come from the actual repository and the accepted `028`/`029` substrate (they
> are the *how* of realizing §18.4, not new product decisions).

## Summary

`030-abwab-relationships-templates` builds the two **category adjuncts** on the accepted
`029` core: **category relationships** and **door templates**. §18.4 permits both workstreams
to run **in parallel** and requires **each to finish its own adapter and vertical slice before
the Spec Kit exits** — that is the whole of its mandatory internal order, and no further
sequencing is added here.

The **Relationship** workstream adds one typed `CategoryRelationship` table carrying both the
mutual (`Similar`/`Opposite`, canonical lower/higher) and directional (`BroaderNarrower`)
shapes with CHECK/filtered-unique enforcement, cycle-safe directional validation under the
transaction (with an explicit direct A→C still allowed), tracked soft-delete/restore,
either-endpoint `Relationship` manual protection over the current ∪ proposed / stored /
proposed target sets, and the complete vertical slice (port, mock, HTTP mapping, UI/actions,
cache keys, parity suite, specialized audit payload, versioned inverse adapter). It also
replaces `029`'s **generic** dependent-visibility fixture with **real** relationship dormancy
across subtree delete / operation-restore.

The **Template** workstream adds the `DoorTemplate` / `TemplateNode` / `TemplateNodeSearchAlias`
aggregate with manual-editor-only CRUD, `TemplateRevision`-guarded node reparent/reorder, tracked
alias soft delete, and **one-target application** that writes real categories **only through the
accepted `029` category writer** — one ChangeSet, one `TreeRevision` bump, roots created as direct
children, uniqueness/protection revalidated under the transaction, and a strict copy allowlist. It
owns **one** DoorTemplate aggregate inverse adapter plus a **versioned application-event
interpreter** that delegates real-category inversion to the **single `029` `CategoryRestoreAdapter`**
and adds **no** registry entry.

The technical approach is *reuse-the-accepted-substrate, protect-under-transaction,
backend-authoritative, exactly-one-adapter-per-persisted-type*: every writer runs on the `028`
audited-commit executor behind the `AbwabWriteBarrier`, every conflict maps to an existing §11
`abwab.*` code, and the §8 registry test is extended so a duplicate or missing registration fails
CI.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`) backend; TypeScript 5.9 / Angular 20.3 standalone
+ Signals frontend.

**Primary Dependencies**: Backend — EF Core 10 / Npgsql, Testcontainers.PostgreSql 4.4.0, xUnit
2.9.3, FluentAssertions 8.2.0. Frontend — Angular 20.3 (`@angular/build` 20.3.27), RxJS 7.8,
Vitest 3.2.6, `@playwright/test` 1.61. Reused from the accepted `028`/`029` substrate (not
re-installed or re-implemented here): `AbwabAuditedCommitExecutor` + `AbwabWriteGuardInterceptor`,
`AbwabWriteBarrier`, `AbwabRevisionState`/`ExpectedTimelineGeneration`, `IServerClock`,
`IAbwabRestoreAdapter<TProduct, TSnapshot>` / `IAbwabRestoreAdapterDescriptor`, `ArabicNameNormalizer` (§5.1),
`ManualProtectionResolution` + `ProtectionResolver`, the `029` category writer
(`AbwabCoreWriteHandler` → `CategoryContentHandler`/`CategoryOrderingHandler`), `CategoryRestoreAdapter`,
the §14.1 frontend primitives (`PersistentCache`/IndexedDB, action/conflict), `@angular/forms`, and
the Playwright harness.

**Storage**: PostgreSQL. New Abwab tables: `CategoryRelationship` (one typed row, mutual **or**
directional columns, soft-delete metadata, `Version`), `DoorTemplate` (identity, name/normalized
name, optional description, `TemplateRevision`, soft-delete, `Version`), `TemplateNode` (template
ownership, parent node, name/normalized name, optional plain-string representative excerpt, optional
description, explicit `SiblingOrder`, soft-delete, `Version`), `TemplateNodeSearchAlias` (mirrors the
category-alias value/normalization/soft-delete contract). Constraints: one-shape CHECK + canonical
lower/higher ordering + no self-link, filtered unique indexes for active mutual pairs per type and
active directional edges (§7.3). **No Quran foreign key is introduced** — `TemplateNode`'s
representative excerpt is a plain string, exactly like `Category`'s; the first Abwab Quran FK stays
owned by `031`.

**Testing**: Backend — xUnit against **real PostgreSQL** (migration-based Testcontainers): shape
CHECKs/indexes, cycle validation including the **race-created** cycle, duplicate/reverse-duplicate
rejection, protection-target unions, restore collision, subtree-delete dormancy + operation-restore
visibility with no cascade/history loss, template self/descendant reparent, stale/concurrent
reparent/reorder, cyclic restore, one-`TemplateRevision`/one-`TreeRevision` bump counts, alias
physical-delete rejection, permission-ownership matrices, and versioned adapter round-trips plus the
extended §8 registry test. Frontend — Vitest with the preserved fork cap
(`VITEST_MIN_FORKS`/`VITEST_MAX_FORKS` in `package.json`) plus the reusable Playwright harness
(relationship/template mock↔HTTP parity, stale-cache, rollback, RTL keyboard/focus, explicit action,
**no-drag**, post-mutation context preservation).

**Target Platform**: Linux server (Railway auto-deploy from `main`) for the backend; browser SPA for
the frontend.

**Project Type**: Web application — .NET Clean Architecture backend
(`Domain`/`Application(.Abstractions)`/`Infrastructure`/`Api`/`Shared`) + Angular SPA
(`core`/`shared`/`features`).

**Performance Goals**: §18.4 names no numeric budget, and none is invented as a product decision.
§15.3 nevertheless requires the **owning domain Spec Kit to freeze numeric query/response/browser
budgets before its writer or UI is accepted**, from *recorded hardware/data assumptions and p95
measurements* — a measurement gate, not a product choice. `030` therefore **measures and freezes**
budgets for directional cycle validation, the subtree-dormancy query, template application, and the
template-editor/application UI interaction, reusing the `029` large-tree browser budget as the UI
baseline. Freezing measured numbers is a gate, never permission to weaken correctness.

**Constraints**: Backend-authoritative. Both workstreams may proceed in parallel; **neither may be
left without its own adapter and vertical slice at exit**. Template application writes real
categories **only** through the `029` category writer — no second category writer, no second
Category adapter. The `029` writer may be **extended behavior-preservingly** (its in-transaction
creation core — normalization, tree/name guards, protection gate, order allocation — extracted into
a grouped seam used by both the existing single-add path and template application inside one audited
operation); it is never forked, and a regression assertion proves `029` single-operation behavior
unchanged. The application-event interpreter is **not** an adapter and must add **no**
registry entry. Every writer runs on one audited ChangeSet carrying `ExpectedTimelineGeneration` /
expected `xmin` / expected `TreeRevision` or `TemplateRevision`, behind the `AbwabWriteBarrier`.
Only existing §11 `abwab.*` codes are used — none is added, renamed, or remapped. Relationship
mutations neither start nor are blocked by the ordinary 24-hour window. **No drag-and-drop.** All
soft deletes are tracked; physical delete stays rejected by the `028` `SavingChanges` guard. Template
copying is a strict allowlist (name, representative excerpt, description, aliases, order, structure)
— everything else is forbidden. Quranic test data stays source-safe. API boundary follows the
`ApiResponse` contract / `Backend/.architecture/API_GUIDELINES.md`.

**Scale/Scope**: Category relationships + door templates only. Excludes the `028` kernel/CI/shared
frontend foundation, the `029` sections/categories/tree/protection core, Quran links and sources
(`031`), workspace/review/notifications (`032`), the audit-restore read model/preview/planner/execution
(`033`), and realtime hardening/release (`034`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is an unfilled template with no ratified principles, so it imposes
no project-specific gates. Governing gates come from repository governance (`CLAUDE.md`,
`CODING_PRINCIPLES.md`, `Backend/.architecture/*`) and Master Plan §18.4:

- **Clean Architecture layering** — Domain holds `CategoryRelationship`, `DoorTemplate`,
  `TemplateNode`, `TemplateNodeSearchAlias` and their invariants with no outward dependency;
  Application owns the relationship/template ports and handlers; Infrastructure owns EF config,
  the migration, the two inverse adapters, and the application-event interpreter; Api owns HTTP +
  `ApiResponse`/`abwab.*` mapping. ✅ PASS (design in Phase 1; layering/source tests in tasks).
- **API contract** — relationship and template endpoints return the `ApiResponse` shape and only
  the exact §11 `abwab.*` codes; every mutation DTO carries `ExpectedTimelineGeneration` plus the
  applicable expected `xmin`/`TemplateRevision`/`TreeRevision`. ✅ PASS (contracts in Phase 1).
- **Exactly one adapter per persisted type (§8)** — `030` registers exactly **two** new adapters
  (Relationship, DoorTemplate aggregate); the application-event interpreter reuses
  `CategoryRestoreAdapter` and registers nothing. The existing registry test
  (`Abwab/RestoreAdapters/RestoreRegistryTests.cs`) is extended to assert
  `{Section, Category, ManualProtection, Relationship, DoorTemplate}` exactly, failing CI on a
  duplicate (e.g. a "template-created category" adapter) or a missing registration. ✅ PASS.
- **No writer bypasses the foundation** — every relationship/template writer runs through
  `IAbwabWriteExecutor` behind the barrier; the `028` `SavingChanges` guard keeps physical deletes
  rejected and every mutation inside a tracked ChangeSet; stabilization blocks all of it. ✅ PASS.
- **No new error codes** — relationship/template conflicts map to the already-frozen
  `abwab.relationship_duplicate`, `abwab.relationship_cycle`, `abwab.template_cycle`,
  `abwab.template_revision_stale`, `abwab.row_stale`, `abwab.timeline_generation_stale`,
  `abwab.tree_revision_stale`, `abwab.manual_protection`, `abwab.category_name_conflict`,
  `abwab.category_unavailable`, `abwab.stabilization_active`. ✅ PASS (§11 governs; the four §11
  strings not yet present in `AbwabConflictCodes` are declared by the tasks — §11 members, not
  additions).
- **No premature Quran FK** — `TemplateNode`'s representative excerpt is a plain string; the
  `NoPrematureQuranFkTests` guard must stay green. ✅ PASS (data-model).
- **Permission catalogue frozen (§5.2)** — only `relationship.*` and `template.*` catalogue codes
  are used, with the exact `template.add`/`edit`/`delete`/`restore`/`apply` ownership split; no
  synonym or child-CRUD permission is invented. The §5.2-frozen strings are new to the repository
  `PermissionCatalogue` and are added there (constants + EF `HasData` seed carried by each
  workstream's migration + `/me` projection + frontend visibility map) by explicit tasks — an
  implementation step, not a new decision. ✅ PASS (contracts + handler tests).
- **No drag-and-drop** — template node ordering/reparent are explicit actions; the `check:no-drag`
  source gate and browser proof are preserved. ✅ PASS.
- **Quranic data safety** — template/relationship fixtures use synthetic Arabic only. ✅ PASS.
- **Comment sparingly / clean-code + test-guard self-checks** — applied before delivery. ✅ PASS
  (process gate).

No unjustified violations. Complexity Tracking left empty.

**Post-design re-check (after Phase 1)**: re-evaluated against
[`data-model.md`](./data-model.md) and [`contracts/`](./contracts/) — all gates still ✅ PASS. The
design adds exactly two registered adapters plus a non-registering event interpreter (§8 gate), routes
every real-category write through the `029` writer (no second writer/adapter), introduces no `abwab.*`
code and no Quran FK, and keeps template ordering/reparent on explicit actions. No new violation
surfaced, so Complexity Tracking stays empty.

## Project Structure

### Documentation (this feature)

```text
specs/030-abwab-relationships-templates/
├── plan.md                                  # This file (/speckit-plan output)
├── spec.md                                  # Feature spec (derived from §18.4)
├── research.md                              # Phase 0 output — decisions per workstream
├── data-model.md                            # Phase 1 output — entities & invariants
├── quickstart.md                            # Phase 1 output — exit-gate validation guide
├── contracts/
│   ├── relationships-api.md                 # add/edit/delete/restore, canonical shapes, protection targets, exact 409s
│   ├── relationship-dormancy-contract.md    # subtree delete/operation-restore vs real Relationship rows (fills the 029 seam)
│   ├── templates-api.md                     # aggregate + node/alias editor CRUD, TemplateRevision, permission ownership
│   ├── template-application-contract.md     # one-target application through the 029 category writer; copy allow/deny list
│   ├── restore-adapters-contract.md         # Relationship + one DoorTemplate adapter + application-event interpreter (no duplicate entry)
│   └── audit-render-contract.md             # specialized relationship payload; frozen template snapshot; separate template-history view
└── checklists/
    └── requirements.md                      # Spec quality checklist (from /speckit-specify)
```

### Source Code (repository root)

```text
Backend/
├── domain/QuranDashboard.Domain/Abwab/
│   ├── Relationships/                            # CategoryRelationship + RelationshipType; one-shape,
│   │                                             #   canonical ordering, no-self invariants
│   └── Templates/                                # DoorTemplate, TemplateNode, TemplateNodeSearchAlias,
│                                                 #   TemplateRevision semantics
├── application/QuranDashboard.Application.Abstractions/Abwab/
│   ├── Relationships/                            # relationship read/write ports, commands, DTOs
│   └── Templates/                                # template read/write ports, node/alias commands, apply command
├── application/QuranDashboard.Application/Abwab/
│   ├── Relationships/                            # writer handlers: shape validation, cycle check under tx,
│   │                                             #   endpoint-protection gate (reuses 029 ProtectionResolver),
│   │                                             #   tracked soft-delete/restore + collision check
│   └── Templates/                                # editor handlers (aggregate/node/alias/order) and the
│                                                 #   application handler that calls the 029 category writer
├── infrastructure/QuranDashboard.Infrastructure/
│   ├── Persistence/Configurations/Abwab/         # EF config: one-shape CHECKs, canonical-order CHECK,
│   │                                             #   filtered unique indexes, template constraints
│   ├── Migrations/                               # additive migrations — one per workstream, EF-tooling generated
│   ├── Persistence/Reads/Abwab/                  # relationship + template read ports
│   └── Abwab/Restore/                            # RelationshipRestoreAdapter, DoorTemplateRestoreAdapter,
│                                                 #   TemplateApplicationEventInterpreter (delegates to
│                                                 #   CategoryRestoreAdapter; registers no new entry)
├── api/QuranDashboard.Api/Abwab/
│   ├── Relationships/                            # endpoints + ApiResponse/abwab.* mapping
│   └── Templates/                                # editor + apply endpoints; exact permission policies
└── tests/QuranDashboard.Tests/Abwab/
    ├── Relationships/                            # real-PG shape/duplicate/cycle/race/protection/dormancy tests
    ├── Templates/                                # real-PG editor/apply/negative-copy/permission tests
    └── RestoreAdapters/                          # extended §8 registry test + adapter round-trips

Frontend/quran-dashboard-ui/src/app/features/abwab/
├── data-access/                                  # relationship + template ports, mocks, HTTP adapters,
│                                                 #   parity suites, relationship/template cache keys
├── state/                                        # relationship + template facades (own cache rules)
├── relationships/                                # relationship actions/panel UI (explicit actions only)
├── templates/                                    # template editor + application UI (Reactive Forms, no drag)
└── audit/                                        # relationship render, dormant-count contribution,
                                                  #   frozen-application render, template-history render

Frontend/quran-dashboard-ui/
└── e2e/abwab/                                    # relationship + template browser specs on the 028 harness
```

**Structure Decision**: Web application. Backend extends the existing Clean Architecture projects
with two new Abwab sub-areas (`Relationships/`, `Templates/`) alongside the accepted `029`
`Sections`/`Categories`/`Protection`/`Tree` areas — feature-grouped, not type-grouped, per
`BACKEND_STRUCTURE.md`. Frontend extends the single `features/abwab` slice with relationship and
template sub-areas reusing the `029` data-access/state conventions and the `028` §14.1 primitives.
No new top-level project, no new forms substrate, and no second category writer or Category adapter
is introduced.

## Complexity Tracking

No Constitution Check violations. Section intentionally empty.
