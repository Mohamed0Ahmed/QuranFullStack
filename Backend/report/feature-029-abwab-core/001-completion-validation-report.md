# 029 Abwab Core — Completion & Validation Report

**Feature:** `029-abwab-core` (Sections, Categories, Tree, Protection) · **Branch:**
`029-abwab-core` · **Date:** 2026-07-23 · **Phase:** 7 (Polish & Cross-Cutting Concerns) ·
**Exit criterion:** every Master Plan §18.3 exit/acceptance gate passes, in the mandatory internal
order (schema/read → protection → writers → frontend slice), with the three restore adapters
accepted for `033`.

This is the acceptance-evidence record for the four delivered stages (US1–US4) plus Polish. It
records per-gate exit results, phase commit hashes, the measured performance baselines §18.3
requires (query budget, large-tree interaction), the three accepted restore adapters, known
deviations, and the handoff to `030`/`031`.

## Phase commit hashes

| Stage | Commit | Subject |
|---|---|---|
| Setup + Foundational | `119dbaf` | Arabic normalizer, shared corpus, no-Quran-FK guard |
| US1 (schema + read) | `a6be989` | entities, filtered uniqueness, seed, tree/search reads, Section+Category restore adapters |
| US2 (protection) | `8f8487e` | ManualProtection, resolver, 3rd adapter accepted (order gate) |
| US3 (writers) | `ed20c10` | section/category mutations, move/subtree, manual protection, exact 409s, 3 adapters accepted for `033` |
| US4 (frontend slice) | `fa0c469` | core port/mock↔HTTP parity, tree/editor/protection UI, §6.3 audit renders |

Migrations delivered (EF-tooling generated): `20260722235439_AddAbwabCoreSchema` (US1),
`20260723004210_AddAbwabManualProtection` (US2),
`20260723011959_AddAbwabCategoryDeletionOperationAndPermissions` (US3).

## Per-US acceptance summary

- **US1 — Schema and read-only tree.** Exactly one `IsPermanentDefault` section
  (`أبواب غير مصنفة`); active root names globally unique across sections, active sibling names
  unique per §5.1, active section names unique, alias uniqueness scoped per-category (separate
  owned rows); root/descendant shape invariants hold; the `كل الأبواب` projection, independent
  root orders, and ancestry/depth read correctly; category search matches the shared normalization
  corpus over name + aliases (never `Description`); Section and Category restore adapters
  round-trip (order as a facet); no mutation endpoint or editable UI exists at this checkpoint.
- **US2 — Protection storage and resolver.** Exactly one active `ManualProtection` per
  `(CategoryId, ProtectionType)` (filtered unique index); the resolver returns type/scope, the
  resolving source ancestor, and server-clock-derived expiry, evaluated from current `AncestorIds`
  (no descendant snapshot); the deep-tree query budget holds (see below); authorized
  view/lift by immutable `CategoryId` works on a soft-deleted target; the ManualProtection adapter
  is versioned, round-trip tested, and accepted **before** any protected writer exists (order gate
  satisfied).
- **US3 — Activate tracked writers.** All §9-matrix section/category mutations run on one audited
  `ChangeSet` carrying `ExpectedTimelineGeneration` + expected `xmin`/`TreeRevision`; exact
  `abwab.*` 409s for every conflict class (name, cycle, overlap, unavailable, manual/ordinary
  protection, scope conflict, stabilization, revision/generation/row staleness); atomic subtree
  delete/operation-restore with one `DeletionOperationId`; `CategoryContentRevision` bumps exactly
  once per direct-content operation and never on a pure move/reorder; the reservation seam is
  present but inert; composite-read redaction leaks nothing across any grant combination; all
  **three** restore adapters (Section, Category, ManualProtection) accepted for `033`, proven by
  the static §8 registry test. No drag-and-drop.
- **US4 — Domain frontend vertical slice.** The core mock and the HTTP adapter implement the ONE
  `AbwabCorePort` contract in proven parity; neither fabricates `TimelineGeneration`/`TreeRevision`
  on a mutation result; composite-read UI visibility mirrors the backend redaction table across
  every grant combination with 0 leaks (cosmetic only — the backend DTO projection is the sole
  authority); the category editor reuses the `028` `@angular/forms` Reactive Forms package with no
  edit-session lock; the §6.3 audit-render payloads (category create/edit, bulk-move, subtree
  delete/restore, manual-protection) publish with no standalone "ordering" component; the Playwright
  source suite (mock/HTTP parity, stale-cache/rollback, RTL keyboard/focus, large-tree, explicit
  action, no-edit-session-lock, no-drag, post-mutation context preservation) passes in a real
  browser.

## Measured performance baselines (§18.3 fixes no numeric limit — these ARE the recorded budgets)

- **Deep-tree protection query budget** (`Backend/tests/QuranDashboard.Tests/Abwab/Protection/DeepTreeBudgetTests.cs`,
  documented in `Backend/application/QuranDashboard.Application/Abwab/Protection/README.md`):
  `EfManualProtectionReadPort.GetProtectionContextAsync` issues a **constant 3 SQL queries**,
  confirmed identical at chain depth 5 and depth 200 on real PostgreSQL — resolution reads the
  denormalized `AncestorIds` column directly instead of walking parent links, so there is no N+1.
  The test asserts a budget of **measured baseline (3) + 2 margin = 5**, not an invented threshold.
- **Large-tree interaction baseline** (`Frontend/quran-dashboard-ui/e2e/abwab/core-slice.spec.ts`,
  reusing the `028` synthetic-tree spike scale of 2,000–3,000 nodes): measured on this run at
  `nodeCount=2500` — the reused `028` spike baseline was `baselineRenderMs=60`,
  `baselineScrollMs=162`; the Abwab tree view at the same scale measured `abwabRenderMs=134`,
  `abwabScrollMs=124`, both well inside the asserted budget (`max(baseline*5, 2000ms)`). The
  Abwab tree virtualizes via `cdk-virtual-scroll-viewport`, rendering only the visible window
  regardless of total tree size.

## Restore adapters accepted for `033`

Three adapters registered — **Section**, **Category** (aggregate: Category + CategorySearchAlias +
content + hierarchy/ancestry + all three orders + subtree delete/operation-restore +
`DeletionOperationId` correlation + ordinary-protection actor/time), and **ManualProtection** — all
versioned, round-trip tested, and accepted. `SortOrder`/`SiblingOrder`/`SectionOrder`/`GlobalOrder`
round-trip as **facets** of their owning adapter, never as a standalone "Order" registration. The
static §8 registry test
(`Backend/tests/QuranDashboard.Tests/Abwab/RestoreAdapters/RestoreRegistryTests.cs`) asserts the
DI-registered set is exactly `{Section, Category, ManualProtection}` and fails CI on a missing
registration or a duplicate/standalone `Order` adapter. Full detail and acceptance history:
`Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/Restore/README.md`.

## Exit-gate results (quickstart.md, run 2026-07-23 on this checkout)

Environment: .NET 10.0.110, Node 20.20.2 / npm 10.8.2, Docker 29.6.2 (Testcontainers
`postgres:16-alpine`), Playwright chromium 1.61.1.

| # | Gate | Command | Result |
|---|---|---|---|
| 1 | **Authoritative whole-project backend test** (real PG, no filter) | `dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj -c Release` | **PASS** — `Failed: 0, Passed: 2007, Skipped: 0, Total: 2007`, 4m49s |
| 2 | Abwab-scoped backend tests (real PG) | `dotnet test … --filter FullyQualifiedName~QuranDashboard.Tests.Abwab` | **PASS** — `Failed: 0, Passed: 341, Total: 341` |
| 3 | Backend Release build | `dotnet build Backend/QuranDashboard.sln -c Release` | **PASS** — 0 Warning(s), 0 Error(s) |
| 4 | No-Quran-FK guard (028, extended for 029) | `Abwab/_Guards/NoPrematureQuranFkTests` (in gate 1) | **PASS** — 4/4: no Abwab↔Quran FK; both non-vacuity assertions; `RepresentativeQuranExcerpt` confirmed plain `string` with no ayah FK |
| 5 | §8 restore registry gate | `Abwab/RestoreAdapters/RestoreRegistryTests` (in gate 1) | **PASS** — 7/7: exactly `{Section, Category, ManualProtection}` registered, no duplicates, Order never a registered type, CI-fail simulations for a standalone Order and a missing registration both correctly fail |
| 6 | Conflict-code parity gate | `Abwab/_Support/ConflictCodeParityTests` (in gate 1) | **PASS** — 5/5: every `abwab.*` code maps identically across core/HTTP/contract fixtures, no invented code |
| 7 | Schema-compatibility gate | `Abwab/Ci/SchemaCompatibilityTests` (in gate 1) | **PASS** — migrated schema has no pending model changes |
| 8 | Permission-parity gate | `Abwab/Permissions/PermissionParityTests` (in gate 1) | **PASS** — 5/5: seed/policy/`/me`-projection/frontend catalogues all match |
| 9 | API contract-drift gate | `Abwab/Ci/ContractDriftTests` (in gate 1) | **PASS** — 2/2: committed swagger baseline well-formed and matches the live endpoint set |
| 10 | Frontend unit tests (Vitest fork cap) | `npm test` (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2 ng test`) | **PASS** — 186 files, 2031 tests passed |
| 11 | Frontend production build | `npm run build` | **PASS** (exit 0). Pre-existing bundle-/SCSS-budget **warnings** on mushaf components (unrelated to 029; non-blocking) |
| 12 | Frontend `tsc --noEmit` | `npx tsc --noEmit -p tsconfig.json` | **PASS** — no errors |
| 13 | Vitest fork-cap gate | `npm run check:fork-cap` | **PASS** |
| 14 | Frontend no-drag source gate | `npm run check:no-drag` | **PASS** — no drag/drop tokens in `src/` |
| 15 | Foundation-boundary gate | `npm run check:foundation-boundary` | **PASS** — primitives stay Forms/HTTP/domain-free |
| 16 | Playwright browser/source suite | `npx playwright test` | **PASS** — 15/15 (9 `e2e/abwab/core-slice.spec.ts` + 3 harness smoke + 1 permissions non-authoritative + 1 synthetic-tree spike + 1 harness RTL/ARIA — see stage breakdown) |

**Exit verdict:** all quickstart.md gates green on this checkout, in the mandatory internal stage
order (US1 → US2 → US3 → US4). Acceptance is the repo owner's to grant.

## Quickstart end-to-end result (T075), by stage

- **Stage 1 (schema/read):** confirmed via gate 2 (US1 test classes: `PermanentDefaultSeedTests`,
  `NameUniquenessTests`, `AliasUniquenessTests`, `TreeSnapshotReadTests`, `CategorySearchTests`,
  `SectionCategoryRoundTripTests`, `NoMutationSurfaceTests`) — all green.
- **Stage 2 (protection):** confirmed via gate 2 (`OneActiveRecordTests`, `ProtectionResolverTests`,
  `DeepTreeBudgetTests`, `SoftDeletedTargetAccessTests`, `ManualProtectionRoundTripTests`) — all
  green; budget baseline recorded above.
- **Stage 3 (writers):** confirmed via gate 2 (the full `Sections`/`Categories`/`Protection` US3
  suite incl. `SubtreeDeleteRestoreTests`, `FullPresetTests`, `CompositeReadPolicyTests`,
  `CategoryContentRevisionTests`, `RestoreRegistryTests`) — all green.
- **Stage 4 (frontend slice):** confirmed via gates 10 and 16 (`abwab-core-parity.spec.ts`,
  `abwab-permissions.spec.ts`, `audit-render.spec.ts`, and `e2e/abwab/core-slice.spec.ts`) — all
  green; large-tree baseline recorded above.
- **Final gate:** all §18.3 criteria pass in CI in the mandatory order; three adapters accepted for
  `033`; no relationship/template/attribution/workspace/audit-restore/realtime surface was built
  (confirmed below, T079).

## Clean-code & test-guard self-check (T077)

The delivered 029 diff (backend + frontend, excluding `Migrations/`/`*ModelSnapshot*`/
`*.Designer.cs`) was scanned against the clean-code-guard reference pack and the test-guard rules.

- **Finding:** several 029 files carried **T0xx-prefixed class/file-level header comments**
  describing WHAT the file/class does (e.g. `// T061: explicit category actions only…`,
  `// T069: core cache rules for the Abwab tree snapshot…`) — per `CODING_PRINCIPLES.md` this
  belongs in a README, not a comment tied to a task ID that loses meaning after merge.
- **Fix applied:** relocated the substance of every such header into the newly-written area READMEs
  (see below) and removed the banner comments from **15 backend files** (4 API controllers, 8
  Application handlers, 3 test files) and **14 frontend files** (data-access, editor, protection,
  state, tree, audit). Genuinely load-bearing WHY comments tied to an exact line were **kept**,
  trimmed of the task-ID prefix where one was present:
  - `abwab-core.mock.ts` — why the mock is not `@Injectable()`.
  - `category-editor.component.ts` — why alias edit/remove carries the alias's own `Version`, never
    the category's.
  - `protection-panel.component.ts` — why rendering nothing when `canView` is false is safe by
    construction.
  - `abwab-tree.facade.ts` — why a conflict's invalidate+reload IS the facade's "rollback" (no
    separate optimistic-undo path exists to get wrong).
  - `abwab-tree-view.component.ts` (RTL arrow-key reversal), `abwab-tree-node.ts` (visible-only
    flattening), `abwab-cache.ts` (IndexedDB-fallback rationale, invalidation call sites),
    `abwab-mock-normalize.ts` (mock mirror is not the source of truth) — all pre-existing,
    non-task-ID-prefixed, unchanged.
  - Backend: the `xmin`-vs-row-lock distinction, the reservation-seam rationale, and the
    soft-deleted-target read rationale (`EfManualProtectionReadPort`) were already documented
    without task-ID banners and were left as-is.
- **No other findings.** No dead code, no debug/TODO/FIXME leftovers, no unused imports (backend
  build: 0 warnings; frontend `tsc --noEmit`: clean). Test-guard: real PostgreSQL for
  persistence/concurrency/query-budget tests, a real browser (Playwright) for interaction/RTL/
  virtualization/no-drag, real DTOs/entities constructed throughout, mocks target real boundaries
  (`AbwabCoreMock` mirrors the real `AbwabCorePort`, exercised by the parity suite against the real
  HTTP adapter) — no violations found.
- **Re-verification after the fix:** backend build (0/0 warnings/errors), Abwab-scoped backend
  tests (341/341), frontend `tsc --noEmit` (clean), frontend abwab-scoped unit tests (9 files /
  57 tests), and `npx playwright test e2e/abwab/` (9/9) all re-ran green after the comment cleanup
  — see "Final verification" below for the full-suite re-run.

## READMEs created / updated (T076)

**Created** (none existed before this pass):

- `Backend/domain/QuranDashboard.Domain/Abwab/README.md` — Section/Category/CategorySearchAlias/
  ManualProtection/ArabicNameNormalizer, root/descendant shape, the two-revision-counter invariant.
- `Backend/application/QuranDashboard.Application.Abstractions/Abwab/README.md` — the
  `Core`/`Restore` port contracts.
- `Backend/application/QuranDashboard.Application/Abwab/README.md` — Tree/Sections/Categories
  handlers (Protection already had its own README, referenced, not duplicated).
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/README.md` —
  `EfAbwabCoreReadPort`/`EfManualProtectionReadPort`.
- `Backend/api/QuranDashboard.Api/Abwab/README.md` — the four controllers, policies, conflict
  mapping.
- `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md` — the full slice: port/mock/HTTP
  parity, `028` §14.1 cache reuse, no-drag/RTL, composite-read visibility (non-authoritative),
  §6.3 renders.

**Updated:**

- `Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/README.md` — added the `029`
  `InertDeletionReservationChecker` seam note and cross-links to the new READMEs above.
- `Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/Restore/README.md` — already carried
  the T062 three-adapter acceptance note from US3; confirmed accurate, no change needed.
- `Backend/application/QuranDashboard.Application/Abwab/Protection/README.md` — already carried the
  measured deep-tree query-budget baseline from US2; confirmed accurate, no change needed.

## Known deviations / carry-forwards

None. No spec-vs-repo contradiction was found; no test or guard was weakened; no new migration or
feature-behavior change was made in Polish — only documentation and comment relocation.

## Exit-gate confirmations (T079)

1. **No Quran FK exists.** `NoPrematureQuranFkTests` is green in the authoritative run: no
   Abwab↔Quran foreign key, both non-vacuity assertions hold, and `RepresentativeQuranExcerpt` is
   confirmed a plain `string` column with no ayah foreign key on every Abwab entity that carries it.
2. **No out-of-scope surface was built.** `029` built no relationship/template schema (`030`), no
   attribution/link schema (`031`), no workspace/review/notification public surface (`032`), no
   audit-restore read model/planner/execution surface (`033`), and no realtime surface (`034`).
   Its only cross-feature touch is the accepted `028` substrate (ChangeSet UoW, write barrier,
   timeline generation, server clock, §14.1 frontend primitives) it was built to consume, not
   modify.
3. **Three restore adapters accepted for `033`, §8 registry test green.** Section, Category
   (aggregate, all three orders + subtree delete/operation-restore as facets), and ManualProtection
   are the only three registered adapters; `RestoreRegistryTests` (7/7) proves the registry is
   exactly this set, that no duplicate/standalone `Order` adapter exists, and that both a
   standalone-Order simulation and a missing-registration simulation correctly fail the CI check.
4. **Acceptance handoff to `030`/`031` is recorded.** The category-tree product model (sections,
   categories, aliases, ancestry/orders, manual + ordinary protection, the composite-read
   redaction, and the three accepted restore adapters) is ready for `030` (relationship/template)
   and `031` (attribution/link) to build on. Both must continue to route every mutation through the
   same audited `ChangeSet`/barrier/`ExpectedTimelineGeneration` discipline and must not reopen the
   Abwab→Quran FK question, which stays owned by `028`'s exit acceptance, not `029`'s.

## Final verification (after T077 fixes)

| Suite | Result |
|---|---|
| Backend, full (real PG) | `Failed: 0, Passed: 2007, Skipped: 0, Total: 2007` |
| Backend, Abwab-scoped (real PG) | `Failed: 0, Passed: 341, Total: 341` |
| Backend Release build | 0 Warning(s), 0 Error(s) |
| Frontend unit (Vitest, fork cap enforced) | 186 files, **2031** tests passed |
| Frontend unit, Abwab-scoped re-run after comment cleanup | 9 files, **57** tests passed |
| Frontend `tsc --noEmit` | clean |
| Frontend production build | exit 0 (pre-existing, unrelated bundle/SCSS budget warnings only) |
| Playwright, full suite | **15/15** passed |
| Playwright, Abwab-scoped re-run after comment cleanup | **9/9** passed |
