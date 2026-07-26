# 030 Abwab Relationships & Templates — Completion & Validation Report

**Feature:** `030-abwab-relationships-templates` (Category relationships + Door templates) ·
**Branch:** `030-abwab-relationships-templates` · **Date:** 2026-07-26 ·
**Phase:** 5 (Polish & Cross-Cutting Concerns) ·
**Exit criterion:** every Master Plan §18.4 exit/acceptance gate passes, with **both** workstreams
having finished **their own adapter and their own vertical slice**, and the two new restore adapters
plus the non-registering application-event interpreter accepted for `033`.

This is the acceptance-evidence record for the two parallel P1 workstreams (US1 relationships,
US2 templates) plus Polish. It records the delivered surface, the phase commits, the acceptance
gates, the self-check findings and their disposition, the exit confirmations (T082), and the
handoff to `033`.

The frozen numeric performance budgets (T075) are a separate artifact —
[`performance-budgets.md`](./performance-budgets.md) — and are not restated here.

## Phase commits

| Phase | Commit | Subject |
|---|---|---|
| Spec Kit stage 1 | `f5e5c4e7` | spec/plan/tasks/contracts approved |
| Phase 1 — Setup | `c39e514a` | relationship/template area folders across domain, application, api, tests, frontend |
| Phase 2 — Foundational | `83759d25` | no-Quran-FK, code-drift and stabilization guards + shared real-PG builders |
| Phase 3 — US1 | `d7cdd61c` | category relationships vertical slice |
| Phase 3 — US1 follow-ups | `1013de48` | engineering-review follow-ups on the relationship slice |
| Phase 4 — US2 | `92b8f23f` | door templates vertical slice |
| Phase 5 — Polish | *(this change)* | T076–T082 |

Migrations delivered (EF-tooling generated, one per workstream, never hand-written):

- `20260725151715_AddAbwabCategoryRelationships` (US1 — table, CHECKs, filtered unique indexes,
  RESTRICT endpoint FKs, `relationship.*` permission seed rows)
- `20260725225843_AddAbwabDoorTemplates` (US2 — template/node/alias tables, `template.*` permission
  seed rows)

No migration was generated in Phase 5; Polish changed no schema.

## Delivered surface

**US1 — Category relationships.** One typed `CategoryRelationship` table carrying exactly one shape
per row (mutual pair in canonical `LowerCategoryId < HigherCategoryId` order, or the
`BroaderNarrower` directional pair), enforced by DB CHECKs including no-self-link; filtered unique
indexes over **active** rows only, so a reverse duplicate collapses onto the same key and
soft-deleted history survives; in-transaction directional cycle rejection with a direct A→C still
legal alongside A→B→C; tracked soft delete/restore with restore-collision rejection; the §7.3
endpoint-protection gate (proposed on add, current ∪ proposed on edit, stored on delete/restore)
blocking the whole mutation on direct **or** inherited `Relationship` protection; dormancy derived on
read from endpoint deleted-state with **no** written flag and **no** cascade; the versioned
`RelationshipRestoreAdapter`; and the frontend slice (port/mock/HTTP adapter in parity, cache +
facade, RTL UI with explicit actions only, specialized audit render, dormant-count contribution).

**US2 — Door templates.** The `DoorTemplate` + `TemplateNode` + `TemplateNodeSearchAlias` aggregate
under a single `TemplateRevision` counter; manual-editor-only CRUD with **no** create-from-real-door
path and **no** cross-door copy path; reparent guards rejecting self and descendant destinations
under the transaction; atomic sibling-order rewrite with exactly one `TemplateRevision` bump per
grouped structural operation; alias remove/restore as tracked soft delete; the frozen §5.2 permission
ownership matrix; one-target application through the **`029`** category writer via the
behavior-preserving `CategoryGroupedCreation` seam, producing exactly one ChangeSet and one
`TreeRevision` bump with every template root as a direct child and the strict copy allowlist; the one
versioned `DoorTemplateRestoreAdapter` for the whole aggregate; the non-registering
`TemplateApplicationEventInterpreter`; and the frontend slice (port/mock/HTTP adapter in parity,
cache + facade, Reactive-Forms editor with explicit save and no drag, application panel, frozen
application render + separate template-history render).

## Restore adapters accepted for `033`

| Adapter | Persisted type(s) | Status |
|---|---|---|
| `RelationshipRestoreAdapter` | `CategoryRelationship` | **Accepted** — versioned, round-trip tested (both shapes, soft-deleted state, duplicate-collision rejection on reconstruct) |
| `DoorTemplateRestoreAdapter` | `DoorTemplate` + `TemplateNode` + `TemplateNodeSearchAlias` | **Accepted** — versioned, round-trip tested (node tree with parent links and explicit `SiblingOrder`, alias history including soft-deleted rows, aggregate soft-delete state, cyclic-restore rejection) |
| `TemplateApplicationEventInterpreter` | *none — not an adapter* | **Accepted** — registered as `IAbwabRestoreEventInterpreter` only, adds **0** registry entries, and its inversion through the single `029` `CategoryRestoreAdapter` is **observed** in test, not merely asserted in prose |

The registered set is therefore exactly `{Section, Category, ManualProtection, Relationship,
DoorTemplate}` — the two `029` `Order` facets and the two `030` template facets remain facets, never
registrations.

## Polish tasks (Phase 5)

### T076 — API contract generation/drift check (§15.2 gate 4)

`Backend/scripts/check-api-contract` was run against this tree: it re-exported the OpenAPI spec from
a Release build of the API, regenerated the frontend API models (`ng-openapi-gen` + prune) and the
static reference (`redocly`), then diffed the committed output. Result: **`API contract up to date.`**
— zero drift, so the committed snapshots (`Frontend/quran-dashboard-ui/openapi/swagger.json`,
`src/app/core/api/generated/`, `docs/api-reference/`) already describe the delivered relationship and
template endpoint families and needed no refresh.

The one-off run was then made durable as
`Backend/tests/QuranDashboard.Tests/Abwab/Ci/AbwabConflictCodeContractParityTests.cs`, which proves
each `abwab.*` code matches across all five layers §15.3 names:

| Layer | Assertion |
|---|---|
| generated contract ↔ backend | both `030` families are present in the live `ApiExplorer` surface. Composed with `ContractDriftTests`, which already proves committed **==** live for the *whole* surface, this gives presence-in-the-generated-contract without restating the equality |
| backend ↔ frontend union | every code `030`'s endpoints can return is in `ABWAB_CONFLICT_CODES`, and that union contains nothing `AbwabConflictCodes` never declares |
| UI | `ABWAB_CONFLICT_MESSAGES` covers the union exactly, **and every message value is parsed and asserted non-blank with at least one Arabic-range character** — so no code can reach the UI rendering empty or in English |
| mock / HTTP adapter / facade / UI | **no** `.ts` or `.html` file under `features/abwab` names an `abwab.*` code outside the shared union |

The live-surface read, the committed-spec read, and the repository-root walk are shared with
`ContractDriftTests` through one internal `Abwab/Ci/ApiContractSources` support type rather than
duplicated.

### T077 — §8 registry gate finalized

`RestoreRegistryTests.cs` now asserts the DI-registered set is exactly the five expected persisted
types via one equality assertion (which fails on missing, extra, **and** duplicate), plus explicit
data-driven failing cases for every duplicate shape §8 names: a second "template-created category"
adapter, a standalone `TemplateNode` adapter, a standalone `TemplateNodeSearchAlias` adapter, a
relationship-endpoint adapter, a standalone `Order` adapter, and the interpreter registered as a
descriptor — and a per-type missing-registration case for each of the five. The interpreter's own
gate is two-sided: it must resolve as the single `IAbwabRestoreEventInterpreter`, and
`TemplateApplicationEventInterpreter` must not even be assignable to `IAbwabRestoreAdapterDescriptor`,
so a mistaken registration cannot compile into the registry.

### T078 — quickstart end-to-end

`specs/030-abwab-relationships-templates/quickstart.md` was run end-to-end on this checkout against
**real PostgreSQL** (Testcontainers, `AbwabDbCollection`) and a **real browser** (Playwright
chromium). See "Verification" below for the observed counts. Both workstreams' §18.4 exit gates —
shape/constraint, duplicate (including the reverse duplicate), cycle and race-created cycle,
lifecycle and restore collision, protection target sets, ordinary-window exclusion, dormancy,
relationship round-trip; absence gate, reparent guards, stale/concurrent structure, cyclic restore,
ordering, aliases, permission-ownership matrix, application shape and revalidation, copy allowlist,
audit payloads, DoorTemplate round-trip, interpreter reuse — passed as part of the backend run, with
the mock↔HTTP parity suites and the two browser slices green.

### T079 — READMEs updated

| README | Change |
|---|---|
| `Domain/Abwab/README.md` | `030` contracts added to the authority list |
| `Application/Abwab/README.md` | scope line corrected (it now also covers Templates and Relationships); `030` contracts added |
| `Application.Abstractions/Abwab/README.md` | header corrected to name both features |
| `Infrastructure/Abwab/Restore/README.md` | **T077 acceptance note**: the final §8 gate, the two new adapters, and the non-registering interpreter, with the `033` handoff pointer |
| `Api/Abwab/README.md` | new "Contract gates (CI)" section (`check-api-contract`, `ContractDriftTests`, the new cross-layer code-parity gate); `030` contracts added |
| `features/abwab/README.md` (frontend) | `abwab-conflict.ts` documented as the single shared union for the whole feature and its CI enforcement; `030` contracts and the two Playwright slices added |

### T080 — clean-code and test-guard self-checks

Both self-checks were run over the delivered `030` code. Findings acted on:

| # | Finding | Fix |
|---|---|---|
| 1 | `"template.history."` was declared twice — the writer's `TemplateAuditActions` (Application) and a private copy in `EfAbwabTemplateReadPort` (Infrastructure). Infrastructure cannot reference Application, so a changed prefix would silently empty every history read with no compile-time signal | `TemplateAuditActions` moved to `Application.Abstractions/Abwab/Templates/`; both sides now reference the one constant |
| 2 | `"template.applied"` duplicated the same way between `TemplateAuditActions.Applied` and `TemplateApplicationEventInterpreter.Kind` | `Kind` now aliases the shared constant |
| 3 | The §7.4 acyclicity walk was implemented twice, line-for-line, in `TemplateTreeGuards` and `DoorTemplateRestoreAdapter` | extracted to `Application.Abstractions/Abwab/Templates/TemplateParentChainRules.FindCycleNodeId`; both call it and each still raises its own message under the same `abwab.template_cycle` code |
| 4 | `relationship-render.component.ts` inlined `'الطرف'`/`'الأعم'`/`'الأخص'` while its own header comment claimed the labels were derived from the one label source | now calls `relationshipEndpointLabels()` from `relationship-type-labels.ts` |
| 5 | The templates mock accepted an `addNode` under a missing or soft-deleted parent, where the writer fails closed with `abwab.row_stale` — a real mock↔server parity gap | mock now mirrors the writer's active-destination guard |
| 6 | A comment in `TemplateNodeHandler` claimed a duplicated reorder id "fails as the framework HTTP 400", but that path throws `ArgumentException` and the 400 actually comes from the request contract | comment corrected to describe the real backstop |
| 7 | `TemplateNode.IsRoot` was dead (zero references) | deleted |
| 8 | `abwab-relationships.mock.ts` had `.map(row => row.targetCategoryId!)` immediately followed by a `!== null` type-guard filter — contradictory claims | non-null assertion dropped; the honest type guard kept |
| 9 | `AbwabConflictCodeDriftTests.NewlyDeclaredCodes_MatchTheirFrozenSection11Value` filtered by `declared.ContainsKey`, so **deleting or renaming** one of the four new constants made it pass green | a `Contain(keys)` precondition added before the comparison, matching its sibling reused-codes test |
| 10 | `NoCreateFromRealDoorTests.TemplateSourceFiles()` used `.Where(Directory.Exists)`, so a renamed source root would be silently skipped while the other roots kept the absence gate green | missing roots now fail the gate explicitly |

Findings reviewed and **deliberately not actioned**, with reasons:

- **Playwright slices drive a fixture page rather than the bootstrapped app.** True, and inherited
  from the `028` harness (`playwright.config.ts` fixes no `webServer`/`baseURL` by design). Wiring a
  real app target is an `028`-harness decision affecting all Abwab browser suites, not a `030` Polish
  change; the component specs cover the same claims against the real components.
- **`RelationshipBudgetTests` counts queries on a copy of the writer's BFS, and carries one vacuous
  `NotBeNull` assertion.** Real, but that file is T075's deliverable, which was completed and
  committed before this phase and is explicitly out of Phase 5's scope. Recorded here as a
  carry-forward.
- **Several proposed test deletions** (the interpreter's observing spy, the permission-ownership
  matrix variants, the template-authorization matrix variants, the cyclic-restore success case, the
  audit-render published-component list). Each is mandated verbatim by a Phase 3/4 task in `tasks.md`
  — T051 explicitly requires the inversion to be *observed*, T045 the full ownership matrix, T042 the
  cyclic-restore pair. Deleting them would violate the binding task contract, so they stand.
- **Wall-clock p95 assertions are floored at 250 ms over 2.8–6.7 ms measurements.** Intentional and
  documented in `performance-budgets.md`: the numbers were taken on a loaded developer laptop, so the
  floor is deliberate rather than fitted.
- **Stale `(vacuous until T0xx)` clauses** in a few guard assertion messages. Cosmetic; the one that
  masked a real vacuity (#9 above) was fixed.

## Exit confirmations (T082)

| # | §18.4 confirmation | Result |
|---|---|---|
| 1 | **Both** workstreams finished their own adapter **and** vertical slice | **Yes.** US1: `RelationshipRestoreAdapter` + schema/writer/protection/dormancy/API/frontend/browser. US2: `DoorTemplateRestoreAdapter` + aggregate/editor/application/API/frontend/browser. Neither is a partial delivery |
| 2 | Relationship adapter, the **one** DoorTemplate aggregate adapter, and the interpreter's verified reuse of the `029` Category adapter accepted with **no duplicate registry entry** | **Yes.** Registered set is exactly `{Section, Category, ManualProtection, Relationship, DoorTemplate}`; the interpreter adds 0 entries and is not even a descriptor type |
| 3 | **0** `abwab.*` strings beyond the §11 catalogue and **0** renamed/remapped | **Yes.** `AbwabConflictCodeDriftTests` parses §11 from the Master Plan and proves the four newly-declared codes are genuine §11 members and the seven reused codes keep their frozen values; the new cross-layer gate extends this to the frontend union, message map, mock, adapter, and UI |
| 4 | **0** writers bypass audit / protection / concurrency / stabilization | **Yes.** Every relationship and template mutation runs one audited ChangeSet through `IAbwabWriteExecutor` past the `028` barrier; `WriterStabilizationCoverageTests` proves every new command type is discovered by `AbwabWriterRegistry` and denied with `abwab.stabilization_active` during `Stabilizing`; the `SavingChanges` guard rejects physical deletes |
| 5 | `029` single-add regression assertion green | **Yes.** `CategoryGroupedCreationSeamTests` proves `029` single-add behavior (one operation, one bump, same guards/audit draft) is unchanged by the T064 extraction; the writer was extended, never forked |
| 6 | No Quran FK | **Yes.** `NoPrematureQuranFkTests` asserts no Abwab→Quran foreign key exists and that `TemplateNode.RepresentativeQuranExcerpt` is a plain string column with no ayah validation |
| 7 | Nothing owned by `027`–`029` or `031`–`034` was built | **Yes.** No `028` kernel/CI work, no `029` core category/section/protection behavior (the category writer was reused through a seam), no link/source structure (`031`), no workspace/review/notification surface (`032`), no audit read model/preview/planner/restore execution (`033`), no realtime transport (`034`) |
| 8 | Additional `030` hard invariants | **Yes.** Exactly two new registered adapters; template application writes real categories only through the `029` writer; no create-from-real-door and no cross-door copy path (`NoCreateFromRealDoorTests`); the copy allowlist is exact with 0 forbidden families copied; no drag-and-drop anywhere including the template editor (`npm run check:no-drag` plus the browser no-drag assertions); relationship mutations neither start nor are blocked by the ordinary 24-hour window; relationship rows are never cascade-deleted by a category subtree delete |

### Acceptance handoff to `033`

`030` hereby hands the following to `033` over the direct `030 → 033` edges. `030` builds **no**
restore preview, planner, or execution surface — these are inputs, not implementations:

1. **`RelationshipRestoreAdapter`** — versioned (`SnapshotSchemaVersion`), schema-tagged,
   round-trip tested. A snapshot carries **no dormancy state**: dormancy is a read-side projection
   over untouched rows, so restoring the endpoint category alone re-exposes the relationship.
   Reconstruct respects the writer's invariants and fails on a duplicate active pair/edge rather than
   persisting a second active row.
2. **`DoorTemplateRestoreAdapter`** — the **one** versioned adapter for the whole aggregate.
   Reconstruct fails on a cyclic snapshot with the **same** typed `abwab.template_cycle` conflict the
   writer raises, so `033`'s restore path and the write path are one §11 channel.
3. **`TemplateApplicationEventInterpreter`** — versioned event-kind interpreter for
   `template.applied`, delegating real-category inversion to the single `029` `CategoryRestoreAdapter`.
   It is **not** an adapter and owns no persisted type; `033` must not register it as one.
4. **Snapshot exclusions**, uniform across all three: `xmin`, the logical revision counters
   (`TemplateRevision`, `TreeRevision`, `CategoryContentRevision`), cache state, and realtime cursors
   are current technical state and are never inverse-restored.
5. **The dormant-relationship-counts projection** (`IAbwabRelationshipReadPort`), already feeding the
   `029` subtree render payload's generic `dormantDependentCounts` seam and available to `033`'s
   preview. Counts are labelled **dormant**, never "deleted" — a subtree delete writes no relationship
   row at all.
6. **The separate template-history projection**, capped at `MaxHistoryEntries` (100) with a `HasMore`
   truncation flag, so `033` never mistakes a capped history for the complete record. Its known cost
   (an unindexable payload substring scan over the `028`-owned audit table) is recorded in
   `performance-budgets.md` and is `033`'s to fix when it builds the audit read model.

## Verification

All commands were run against the final Phase 5 working tree. Tier per
`TESTING_STRATEGY.md` §3: this is the **Tier C** ordinary pre-PR gate (Abwab-only backend + frontend
change; no Quran `DataPipelines`, importer, canonical-source, or shared-persistence path is touched,
so Tier D is not triggered).

| Command | Result |
|---|---|
| `dotnet build Backend/QuranDashboard.sln` | **Build succeeded — 0 Warning(s), 0 Error(s)** |
| Backend Tier B no-pipeline filter | **Passed — Failed: 0, Passed: 1545, Skipped: 0, Total: 1545** (41 s) |
| Backend Abwab + Api focused slice | **Passed — Failed: 0, Passed: 565, Skipped: 0, Total: 565** (37 s) |
| `Backend/scripts/check-api-contract` | **`API contract up to date.`** — 0 drift, exit 0 |
| `npm run check:no-drag` | **passed** — no drag/drop tokens in `src/` |
| `npm run check:fork-cap` | **passed** — `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` preserved |
| `npm run check:foundation-boundary` | **passed** — foundation primitives stay generic |
| `npm run build` (frontend production) | **Application bundle generation complete** (19.0 s), exit 0 |
| `npm test` (complete frontend suite) | **194 test files passed, 2139 tests passed, 0 failed** (194 s) |
| `npm run e2e` (Playwright chromium) | **39 passed** (4.4 s) — includes both `030` browser slices |

Environment: Docker available, so the Testcontainers PostgreSQL runs are genuine real-PG runs, not
skipped; Playwright chromium browsers installed locally. **No required test was skipped** — every run
above reports `Skipped: 0` / no skip notices.

The frontend production build emits three pre-existing budget **warnings** (exit code still 0): the
initial bundle exceeds its 500 kB budget by 78.6 kB, and two `features/mushaf` SCSS files exceed
theirs. None is attributable to `030`: the mushaf files are untouched by this feature, and the whole
Abwab feature — including both `030` pages — is lazy-loaded through
`app.routes.ts → features/abwab/abwab.routes.ts`, so it contributes nothing to the initial bundle.

## Known deviations / carry-forwards

- **`RelationshipBudgetTests` measures a copy of the writer's cycle BFS** rather than driving the
  real write port (its sibling `TemplateBudgetTests` does drive the real handler). The query-count
  budget therefore cannot catch an N+1 regression introduced inside
  `RelationshipWriterHandler.GuardCycleAsync`. T075's file was completed and committed before Phase 5
  and was left untouched here; correcting it is a small follow-up.
- **Playwright slices drive hand-written fixture pages**, inherited from the `028` harness, which
  fixes no `webServer`/`baseURL`. The real components are covered by the Angular component specs.
  Pointing the browser suites at the bootstrapped app is an `028`-harness decision.
- **The template-history read is an unindexable payload substring scan.** Recorded, not hidden: the
  audit table is `028` kernel substrate and the audit read model is `033`'s, so neither is `030`'s to
  reshape. Response size is bounded regardless by `MaxHistoryEntries`.
- **HTTP 400/403 carry no `abwab.*` body code**, per the accepted `028`/`029` framework behavior
  (`[ApiController]` validation 400s, `[Authorize]` policy 403s). None was introduced.
