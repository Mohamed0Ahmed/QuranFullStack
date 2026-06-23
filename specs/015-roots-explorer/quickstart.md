# Quickstart: Quran Roots Explorer

How to build, run, and test Feature 015. Read-only feature; no migrations, no data import, no Quran
text changes. All three repos are on branch `015-roots-explorer`.

## Prerequisites

- .NET 10 SDK; Node + the frontend toolchain; Docker (for backend Testcontainers).
- Local PostgreSQL `quran_dashboard` seeded through Features 002→010 (for manual run); connection via user-secrets (already configured locally).
- Repos/branches: `App`, `Backend`, `Frontend/quran-dashboard-ui` all on `015-roots-explorer`.

## Build & run

Backend (from `Backend/`):

```sh
dotnet build QuranDashboard.sln
# run the API (use the project's run script if preferred, e.g. scripts/qd-api)
dotnet run --project api/QuranDashboard.Api
# smoke-check the read endpoints (read-only):
#   GET /api/words/roots?sort=mushaf-order&page=1&pageSize=50
#   GET /api/words/roots/{id}/ayahs?page=1
```

Frontend (from `Frontend/quran-dashboard-ui/`):

```sh
npm install
npm start            # or the project's qd-ui script
# open /dashboard/words/roots
```

## Test

Backend (xUnit + Testcontainers; needs Docker):

```sh
cd Backend
dotnet test --filter FullyQualifiedName~Quran.WordsRoots
# real-DB escape hatch (optional): set the roots real-db connection env var to run against a live seeded DB
```

Frontend (Vitest via the Angular builder) — **keep the worker cap or it OOMs the machine**:

```sh
cd Frontend/quran-dashboard-ui
VITEST_MAX_FORKS=2 npm test -- --run src/app/features/words
```

## Definition of done (high level)

- Roots list shows all 8 counts; search/sort/page work and survive refresh/back-forward; no detail calls on list render.
- Selecting a root opens the persistent side panel (default `ayahs`); panel scrolls independently; drawer on narrow screens.
- Verse matches highlight exactly the root's words by id (no string replace); paginated.
- Words sub-views deep-link into the existing Unique Words simple/tashkeel detail.
- Surahs (mentioned/missing) load whole and sum to 114.
- Lemmas (co-occurrence) and stems load whole and are static (non-interactive); **lemmas tab count == table الصيغ المعجمية column** for every root.
- Backend reads are cached under `roots:` keys (no global cache change); structured logs carry ids/counts/`hasSearch` only — no Quran/root/word/raw-search text.
- No migration, no index, no Quran-data mutation.

## Key references

- `specs/015-roots-explorer/spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/`
- `docs/feature-015-roots-explorer/feature-015-roots-explorer-combined-implementation-plan.md` (fuller design + phased plan + milestone test checkpoints)
- Backend verification: `Backend/report/feature-015-roots-explorer/roots-explorer-readonly-verification-report.md`
- Reuse references (Feature 014): `Backend/.../Quran/Words/...` (reader/handler/controller/cache); `Frontend/.../features/words/` (highlighted-ayah, count-chip, url-sync, drilldown facade, ApiResponseCache) and `src/app/shared/ui/pagination` (qd-pagination).
