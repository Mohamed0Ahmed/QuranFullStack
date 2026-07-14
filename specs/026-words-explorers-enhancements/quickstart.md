# Quickstart: Words Explorers Enhancements (Feature 026)

## Prerequisites

- Local PostgreSQL with the seeded `quran_dashboard` database (do NOT reseed/reset —
  read-only feature).
- Backend: .NET 10 SDK. Frontend: Node + npm per repo root README.
- Read first: `features/words/README.md`, reads README
  (`Backend/.../Persistence/Reads/Quran/Words/README.md`),
  `docs/feature-026-words-explorers-enhancements/plan.md` (locked decisions).

## Build & test commands

```bash
# Backend
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/QuranDashboard.sln          # Testcontainers needs Docker

# Frontend (obey repo vitest worker cap — see root README test-command rule)
cd Frontend/quran-dashboard-ui
npm run build
npm test
```

Focused test areas: backend
`Backend/tests/QuranDashboard.Tests/Quran/{WordsWordTypes,Words,WordsRoots,WordsMorphologyExplorers}`;
frontend `src/app/features/words/**/*.spec.ts`.

## Manual smoke per phase (run backend API + `npm start`)

### P1 — Word Types parity

1. `/dashboard/words/types?type=noun` → first page holds up to 1000 rows; smooth
   scroll; tabs/details behave as before.
2. Type a fragment in the new search box → table narrows (word identity match);
   switch to جذور/أصول صرفية/صيغ معجمية tabs → grouped rows reflect the searched
   words; URL carries `search=`; refresh restores.
3. Open a word's آيات view → 100 ayahs per page; grouped member words/ayahs → 100.
4. API checks: `GET /api/words/word-types/table?type=noun&pageSize=1000` → 200;
   `pageSize=1001` → 400; grouped detail `pageSize=101` → 400 (detail cap kept).

### P2 — Cheap filters + result count

1. Each of the four normal explorers shows "عدد الـ…: N" equal to pagination total;
   search → number updates.
2. Pick bucket chips (e.g. occurrences 11–100) → rows + stat narrow; مخصّص min/max →
   same; URL shows `occ=11..100`; malformed URL value (e.g. `occ=9..2`) → filter
   ignored, page loads.
3. Word Types: set hasRoot=missing → words view + grouped views reshape together.
4. API checks: `occMin=11&occMax=100` → 200 filtered; `occMin=5&occMax=2` → 400.

### P3 — Association filters

1. Unique Words: filter by a word type → every visible row's type chip equals the
   filter; filter by root → same agreement for the root chip.
2. Lemmas: `rootId` filter → only that root's lemmas. Stems: root/lemma filter labeled
   "الجذر الأساسي" / "الصيغة المعجمية الأساسية".
3. Valid-but-unmatched id → empty page + stat 0 (not an error).

### P4 — Scope counts

1. Word Types: strip between filters and tabs shows four counts
   (كلمات | جذور | أصول صرفية | صيغ معجمية).
2. Equality: for the same scope, each count equals the matching tab's pagination
   total — check all four tabs, then repeat with search + a has-flag active.
3. Change type/sub-filter/search → counts reload; switch tabs or pages → counts do
   NOT reload (network tab quiet).
4. Kill backend mid-session, retry via إعادة المحاولة on the strip → only counts
   refetch; table unaffected.

## Perf gates (mandatory, record results in the phase report if one is requested)

- P1: time `/table` at `pageSize=1000` for `type=verb` (unscoped) and
  `tableView=stems` (≈12k groups); verify UI scroll on that page; note cache-entry
  growth. Hard failure at default 1000 → STOP (decision record stop condition 4).
- P4: time `/scope-counts` on widest scopes; assert 1 SQL command (test pin).

## Verification before review

- `dotnet build` + `dotnet test` green; `npm test` green under worker cap.
- READMEs updated in the same commit (`features/words/README.md` + reads README).
- Run repo deploy-smoke flow after P1 and P4.
- Count-family audit: no `words_count`-backed number on any Word Types surface; no
  scoped count on the trio's stat line.
