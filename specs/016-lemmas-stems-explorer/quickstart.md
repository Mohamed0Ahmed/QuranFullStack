# Quickstart: Quran Lemmas & Stems Explorer

Feature 016 is a full-stack read-only feature. It adds no migration, importer, source package, or
Quran-data mutation.

## Prerequisites

- .NET 10 SDK.
- Node/npm matching the existing Angular 20 workspace.
- Docker for backend Testcontainers.
- Local PostgreSQL `quran_dashboard` with the existing morphology/word foundation for manual smoke
  testing.
- Workspace branch: `016-lemmas-stems-explorer`. Child repository branches should be created/aligned
  before implementation commits according to the workspace commit workflow.

## Read Before Implementing

- `specs/016-lemmas-stems-explorer/spec.md`
- `specs/016-lemmas-stems-explorer/plan.md`
- `specs/016-lemmas-stems-explorer/research.md`
- `specs/016-lemmas-stems-explorer/data-model.md`
- `specs/016-lemmas-stems-explorer/contracts/`
- fuller phased plan:
  `docs/feature-016-lemmas-stems-explorer/feature-016-lemmas-stems-explorer-combined-implementation-plan.md`

Also follow Backend/Frontend `AGENTS.md` and architecture guides.

## Build and Run

Backend:

```sh
cd Backend
dotnet build QuranDashboard.sln
dotnet run --project api/QuranDashboard.Api
```

Representative read-only smoke requests:

```text
GET /api/words/lemmas?sort=mushaf-order&page=1&pageSize=100
GET /api/words/lemmas/{id}
GET /api/words/lemmas/{id}/ayahs?page=1&pageSize=100
GET /api/words/lemmas/{id}/stems

GET /api/words/stems?sort=mushaf-order&page=1&pageSize=100
GET /api/words/stems/{id}
GET /api/words/stems/{id}/ayahs?page=1&pageSize=100
GET /api/words/stems/{id}/lemmas
```

Frontend:

```sh
cd Frontend/quran-dashboard-ui
npm install
npm start
```

Open:

```text
/dashboard/words/lemmas
/dashboard/words/stems
```

## Tests

Backend targeted suites:

```sh
cd Backend
dotnet test --filter "FullyQualifiedName~WordsMorphologyExplorers|FullyQualifiedName~WordAnalysisMorphologyIdentity"
```

Frontend targeted tests (repository script already sets the safe worker cap):

```sh
cd Frontend/quran-dashboard-ui
npm test -- --run src/app/features/words src/app/features/mushaf/components/selected-word-section src/app/features/mushaf/components/word-morphology-summary
```

If invoking the builder outside the package script, preserve:

```sh
VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2
```

## Acceptance Checkpoints

### CP-0 — Contracts

- Lemma/stem read interfaces and DTOs compile.
- Mushaf morphology lemma/stem identities are additive and mapped.
- The Feature 016 Testcontainers fixture starts PostgreSQL and completes its smoke test.
- No migration or dependency addition.

### CP-1 — Catalogues

- Both tables render locked columns.
- Search/sort reset list page to 1 while preserving selection/detail state; URL restoration works.
- Malformed/non-positive pages normalize to 1, while valid positive out-of-range pages render a
  controlled empty result.
- Lemma missing root and stem missing lemma/root are controlled values.
- Dominant type and dominant stem relationships use deterministic tie-breaks.
- List render causes no detail request.

### CP-2 — Selection and Details

- Row selection opens words/simple.
- Count controls open the exact mapped view.
- Invalid identity shows panel not-found while list remains usable.
- Active view only is loaded.

### CP-3 — Words and Links

- Simple/tashkeel lists are paginated and counts are selection-scoped.
- Unique-word links use stable IDs and safe new-tab anchors.

### CP-4 — Ayahs and Surahs

- Exact matched word IDs are highlighted; no string replacement.
- Ayahs are paginated and link to Mushaf focus.
- Mentioned and missing surahs are disjoint and total 114.

### CP-5 — Related Morphology and Type Distribution

- Lemma stems count equals related-stems item count.
- Stem related lemmas are correct.
- Full type distribution totals occurrences.
- Root/lemma/stem links open correct new-tab explorer states.

### CP-6 — Mushaf Integration

- Selected-word root/lemma/stem values link only when IDs exist.
- Missing identities remain non-clickable.
- Existing unique-word links and morphology display remain unchanged.

### CP-7 — Hardening

- Cache keys are bounded; no raw-search keys.
- Query-count tests rule out N+1 reads.
- Logs contain safe fields and exclude Quran/lexical/raw-search content.
- Keyboard, RTL, focus, loading/empty/error/not-found, and responsive behavior pass.
- Backend build/tests and frontend build/tests pass.

### SC-002 — Catalogue First-Render Timing

> **T122 — Manual measurement required.** This check cannot be satisfied from a headless agent
> shell; it requires a real browser against the running stack. The automated Phase 11 work
> (logging/cache/SQL audits, accessibility/state matrix, builds, full targeted tests) is complete,
> but the 40 timings below must be captured by a human run before this check is marked passed.

Manual run procedure:

1. Build production artifacts: `dotnet build Backend/QuranDashboard.sln` and
   `npm run build --prefix Frontend/quran-dashboard-ui`.
2. Start the local API (`dotnet run --project Backend/api/QuranDashboard.Api`) and serve the
   production frontend bundle (`npm start --prefix Frontend/quran-dashboard-ui`).
3. Open Chrome/Edge DevTools → Performance (or Lighthouse → Navigate), enable "Disable cache"
   **unchecked** (we want warm cache), and disable any CPU/network throttling.
4. For each route (`/dashboard/words/lemmas` and `/dashboard/words/stems`):
   - Pre-warm the app once so JS bundles, API auth, and the catalogue summary cache are hot.
   - Then record 20 fresh in-app navigations from `performance.timing.navigationStart` (or the
     `navigation` entry's `startTime`) until the first paint of the catalogue table rows
     (e.g. first `[data-testid^="lemmas-table"]` / `[data-testid^="stems-table"]` row visible).
   - Log each of the 20 timings in milliseconds.
5. Requirement: at least 19 of 20 timings per route must be ≤ 1,000 ms.

Use production frontend/backend builds, the local API, warm application/cache state, and no browser
throttling. Measure 20 fresh route openings for each explorer from navigation start until the first
successful catalogue-table render. At least 19 of 20 timings for each route must be at or below
1,000 ms.

Record the implementation-time environment and all 40 timings below. Do not mark this check passed
without measured evidence.

```text
Environment: pending (OS / browser / CPU / app commit / cache state)
Lemmas timings (ms): pending
Lemmas passing openings: pending/20
Stems timings (ms): pending
Stems passing openings: pending/20
SC-002 result: pending
```

### Phase 11 Completion Evidence (T115–T121, T123)

- **T115** — `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersLoggingTests.cs`
  audits all fourteen handlers for required structured fields and forbidden text. Redundant
  `LemmasLoggingTests.cs` / `StemsLoggingTests.cs` removed in favour of the consolidated file.
- **T116** — `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersCacheReadTests.cs`
  audits every bounded cache entry (lemma + stem, all detail methods), confirms catalogue
  search/sort/page changes reuse the cached whole summary, reflects that no cache-key method
  accepts raw search, and asserts `AddLemmas`/`AddStems` register no global `IMemoryCache` /
  `MemoryCacheOptions`.
- **T117** — Frontend panels already ship responsive drawer (`inline` input + `cdkTrapFocus` +
  `cdkTrapFocusAutoCapture`), Escape/backdrop close, RTL logical properties, and guarded
  `matchMedia`. Updated stale doc comments to record the completed behaviour and added
  `:focus-visible` outlines to the lemma/stem panel surfaces.
- **T118** — Added modal-mode coverage (dialog role, `aria-modal`, backdrop click, inner-click
  suppression, Escape, close button, empty-selection renders no chrome) to the lemma and stem
  panel specs and tightened the Words hub spec to assert the lemma card route.
- **T119** — `dotnet test Backend/QuranDashboard.sln --filter "FullyQualifiedName~WordsMorphologyExplorers|FullyQualifiedName~WordAnalysisMorphologyIdentity"`
  → **Passed: 159, Failed: 0, Skipped: 0**.
- **T120** — Affected frontend specs
  (`lemma-details-panel`, `stem-details-panel`, `words-hub-page`) → **30 passed / 30**.
- **T121** — `dotnet build Backend/QuranDashboard.sln` → **0 errors / 0 warnings**;
  `npm run build --prefix Frontend/quran-dashboard-ui` → succeeded (only pre-existing SCSS budget
  warnings, unrelated to Feature 016). `git ls-files --others --exclude-standard` shows no new
  migration, package, lockfile, design-token, or build-config files.
- **T123** — Clean-code and test-code self-checks run against the changed files; no findings
  requiring changes. New backend test files reuse the existing `RecordingLoggerProvider`,
  `SqlCommandCountInterceptor`, real `QuranDashboardDbContext`, and the Feature 016 Testcontainers
  fixture (no new mocks of real boundaries). Frontend additions reuse the existing Angular
  `TestBed` pattern and assert observable DOM behaviour, not implementation details.

## Definition of Done

- Fourteen read-only endpoints return controlled `ApiResponse<T>` outcomes.
- Both explorer pages and Words hub entries are available and restorable.
- No technical IDs are visibly rendered.
- All cross-page study links are inspectable safe new-tab anchors.
- No source text, morphology data, schema, or index is changed.
- Implementation delivery includes the required clean-code and test-code self-checks.
