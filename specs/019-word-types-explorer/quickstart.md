# Quickstart: Word Types Explorer

**Feature**: 019 — Word Types Explorer  
**Scope**: Planning artifact for implementation and verification. Commands assume the workspace root
is `/projects/Dashboard/App`.

## 1. Pre-Implementation Data Gate

Verify the live POS catalogue before implementing the reader:

```bash
sudo -u postgres psql -d quran_dashboard -X -A -F $'\t' -c "SELECT code, arabic_label, category FROM quran_pos_tags WHERE code = 'PRO';"
```

Expected:

```text
PRO    حرف نهي    particle
```

If the row is stale, stop implementation and apply the existing morphology reseed/data-correction
workflow first. Do not validate Feature 019 against stale `PRO` data.

## 2. Backend Build and Tests

```bash
cd /projects/Dashboard/App/Backend
dotnet build QuranDashboard.sln --disable-build-servers -m:1 -p:BuildInParallel=false -p:RestoreDisableParallel=true -v minimal
```

Targeted tests expected after implementation:

```bash
cd /projects/Dashboard/App/Backend
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter FullyQualifiedName~WordsWordTypes --logger "console;verbosity=minimal"
```

Required backend acceptance coverage:

- Tree has four main types and particle excludes `INL`.
- Tree node count equals paged table `TotalCount` for the same active type/child only when no secondary filter is applied.
- Secondary filters narrow table `TotalCount` and active UI count chips only; they do not require scoped tree counts.
- Out-of-bucket POS rows are excluded from noun/verb/particle/INL buckets.
- Multi-context displayed words produce separate rows with separate context-scoped counts.
- E3/E4/E5 never widen a selected row to all usages of the displayed word.
- Nominal/verb secondary filters validate by type and reject cross-type filters.
- Marker words never contribute to rows or counts.
- `ApiResponse<T>` status mapping uses `200`, `400`, and `404` consistently.

Grouped table-view coverage (Feature 022 evolution — same `WordsWordTypes` filter, all in
`QuranDashboard.Tests`):

- Grouped root/stem/lemma summary counts equal the same-scope list-row counts (same grain).
- Member words are filtered and compared by numeric `root_id`/`stem_id`/`lemma_id` only; display
  text is projection-only and never a grouping or filter key.
- Grouped member words split by word context and expose the four measures; paged views use the
  documented page policy while surahs are a single-shot read.
- Scoped ayahs are canonical and highlighting stays context-scoped; queries stay bounded.
- Null/marker rows are excluded; missing surahs are handled without widening scope.
- `WordTypesCacheKeys.table` / `.grouped*` isolate `tableView`, kind, numeric ID, scope, view, and
  page, so no request cross-serves another selection; invalid/not-found map to `400`/`404`.

Last verified command and result (workspace root `/projects/Dashboard/App`):

```bash
cd /projects/Dashboard/App/Backend
dotnet build QuranDashboard.sln --disable-build-servers -m:1 -p:BuildInParallel=false -p:RestoreDisableParallel=true -v minimal   # Build succeeded, 0 Warning(s), 0 Error(s)
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter FullyQualifiedName~WordsWordTypes --logger "console;verbosity=minimal"   # Passed! 124 total
```

## 3. Frontend Build and Tests

The `npm test` script already carries the mandatory `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`
worker cap (see `Frontend/quran-dashboard-ui/README.md`); do not drop it or the run OOMs.

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm run build            # Application bundle generation complete (SCSS budget warnings are non-fatal, see below)
```

Focused Word Types suite (the `*word-type*` glob matches both the singular grouped-detail specs and
the plural `word-types-*` explorer/table specs — 14 files):

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm test -- --include='src/app/features/words/**/*word-type*.spec.ts'   # 210 passed (14 files)
```

If the builder rejects that glob, run these supported capped subsets instead:

```bash
npm test -- --include=src/app/features/words/state/word-types-*.spec.ts
npm test -- --include=src/app/features/words/data-access/word-types.api.spec.ts
npm test -- --include='src/app/features/words/components/word-type-*/**/*.spec.ts'
npm test -- --include='src/app/features/words/pages/word-types-explorer-page/*.spec.ts'
```

`npm run build` emits two non-fatal `anyComponentStyle` budget warnings
(`word-type-filter.component.scss`, `word-types-table.component.scss`); the bundle still generates.
Only a `Build failed` line is a real failure.

Required frontend acceptance coverage:

- `/dashboard/words/types` route loads and defaults to `type=noun`.
- URL state restores exact `word + contextCode`.
- Secondary filters appear only for their valid main type.
- Changing a scope filter (type/child/case/tense/voice) clears selection and resets page; sorting and
  list pagination preserve a still-compatible selection.
- Secondary filter changes do not expect or render scoped tree counts.
- The table displays Uthmani-with-tashkeel words only.
- Null or deferred lemma/stem values render `—` and do not remove rows.
- Details ayah highlights are context-scoped and do not use text replacement.
- Loading, empty, error, and not-found states are explicit and calm.

Table-view tabs & grouped detail coverage (Feature 022 evolution):

- The table-view strip (كلمات | جذور | أصول | صيغ), table shell, and details host stay mounted
  through every parent/child/filter/sort/view/loading/empty/error transition.
- `tableView` survives type/child/case/tense/voice/sort/page changes; only the Words tab returns a
  grouped view to `words`, and switching a tab clears only the incompatible selection keys.
- Selecting a grouped row writes only its explicit `root`/`stem`/`lemma` key with `view=words` and
  no page-1 `detailPage`; the frontend cache key includes `tableView` so tabs never cross-serve.
- Grouped detail panels are kind-aware (word → آيات/سور; grouped → كلمات مرتبطة/آيات/سور) with a
  summary card, and member-word rows are strictly display-only (no button/link/tabindex/selection).
- Grouped words/ayahs are server-paged with internal page 1: page 1 omits `detailPage`, pages `> 1`
  serialize it, and surahs always remove it.
- All four views render quiet explorer rows (`word-types-table__row` + `qd-explorer-table__row`,
  no `qd-interactive-surface`) with a leading page-relative row number (never the database ID),
  `aria-selected`/`aria-current` + visible focus on the selected row, and non-interactive skeletons.

## 4. Manual Smoke Flow

1. Start backend and frontend using the existing project dev commands.
2. Open `/dashboard/words/types`.
3. Confirm اسم is selected by default and rows load.
4. Switch to فعل, then choose ماض / مضارع / أمر and voice filters.
5. Switch to حرف وأداة and confirm no secondary filter appears.
6. Switch to حروف مقطعة and confirm disconnected letters are isolated.
7. Select a row, open الآيات, السور, and التحليل.
8. Confirm main-type selection loads the first page within the 2-second target in the local dev environment after initial app bootstrap.
9. Confirm the path from page open to a selected row's الآيات or التحليل view takes at most 4 interactions.
10. Copy the URL, reload, and confirm the same filters and exact selected row restore.

### 4a. Table-View Tabs & Grouped Detail (Feature 022 evolution)

1. Open a parent scope with `tableView=roots`. Confirm the strip, table, and details region remain
   present while the subtype prompt is inside the table.
2. Change main type, child, case/tense/voice, and sort. Confirm `roots` stays active and no blank
   frame appears.
3. Select root/stem/lemma rows; confirm the URL contains the explicit identity and `view=words` but
   no page-1 `detailPage`. Refresh, share the URL, and use Back/Forward; confirm the correct kind,
   default words tab, and internal page 1 restore.
4. Page grouped words and ayahs. Confirm page 1 omits `detailPage`, page 2 writes `detailPage=2`,
   returning to page 1 removes it, and switching to surahs removes it regardless of the prior page.
5. Click/tap member-word rows and confirm nothing happens; use pagination and confirm only
   pagination acts.
6. Verify row 26 on page 2, no visible database IDs, quiet hover, visible keyboard focus, a distinct
   selected row, and no skeleton hover.
7. Check desktop split scrolling and mobile modal/RTL layout in both light and dark themes.

### 4b. Acceptance Record (last updated 2026-07-13)

The stable-shell and error-state invariants exercised by the manual flow are now covered by facade and
DOM-level automated tests (host identity across transitions, the in-table subtype prompt on a parent
scope, rows-only and later parent-tree failure strip survival, grouped-summary loading and
error-with-retry, and cross-kind row skipping):

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm test -- --include='src/app/features/words/**/*word-type*.spec.ts'   # 210 passed (14 files)
npm run build            # Application bundle generation complete (2 pre-existing non-fatal SCSS budget warnings)
```

The live desktop/mobile + light/dark + modal/RTL walkthrough (steps 1–10 and 4a.1–7) still requires a
running dev environment and remains a human confirmation step; it was **not executed** as part of this
automated change.

## 5. Non-Regression Checks

After Feature 019 implementation, existing Words explorers must keep their contracts and results:

```bash
cd /projects/Dashboard/App/Backend
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter "FullyQualifiedName~Words" --logger "console;verbosity=minimal"
```

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm test
```

At minimum, re-check Roots, Lemmas, Stems, and Unique Words routes manually if the full suites are not
practical in the current environment.
