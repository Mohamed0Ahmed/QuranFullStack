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

Use production frontend/backend builds, the local API, warm application/cache state, and no browser
throttling. Measure 20 fresh route openings for each explorer from navigation start until the first
successful catalogue-table render. At least 19 of 20 timings for each route must be at or below
1,000 ms.

Record the implementation-time environment and all 40 timings below. Do not mark this check passed
without measured evidence.

```text
Environment: pending
Lemmas timings (ms): pending
Lemmas passing openings: pending/20
Stems timings (ms): pending
Stems passing openings: pending/20
SC-002 result: pending
```

## Definition of Done

- Fourteen read-only endpoints return controlled `ApiResponse<T>` outcomes.
- Both explorer pages and Words hub entries are available and restorable.
- No technical IDs are visibly rendered.
- All cross-page study links are inspectable safe new-tab anchors.
- No source text, morphology data, schema, or index is changed.
- Implementation delivery includes the required clean-code and test-code self-checks.
