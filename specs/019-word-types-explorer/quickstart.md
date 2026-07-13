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
the plural `word-types-*` explorer/table specs — 13 files):

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm test -- --include='src/app/features/words/**/*word-type*.spec.ts'   # 237 passed (13 files)
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
- URL state restores exact identity plus the independent five-key detail scope
  (`detailType`, `detailChildCode`, `detailCase`, `detailTense`, `detailVoice`).
- Secondary filters appear only for their valid main type.
- Browsing a parent is URL/list/detail inert. Selecting a child changes list scope/page while preserving
  identity/view/detail page/detail scope; a new statistic replaces the detail snapshot from the current
  list. Existing secondary-filter reset behavior and sort/list-page behavior remain covered.
- Secondary filter changes do not expect or render scoped tree counts.
- The table displays Uthmani-with-tashkeel words only.
- Null or deferred lemma/stem values render `—` and do not remove rows.
- Details ayah highlights are context-scoped and do not use text replacement.
- Loading, empty, error, and not-found states are explicit and calm.

Table-view tabs & grouped detail coverage (Feature 022 evolution):

- The table-view strip (كلمات | جذور | أصول | صيغ), table shell, and details host stay mounted
  through every parent/child/filter/sort/view/loading/empty/error transition.
- `tableView` survives type/child/case/tense/voice/sort/page changes; only the Words tab returns a
  grouped view to `words`. Switching a tab changes only the displayed table/list page and preserves the
  complete open detail identity, scope, view, page, title, and content without another detail request.
- Activating a grouped occurrence statistic writes its explicit `root`/`stem`/`lemma` key, all five
  detail-scope keys, `view=words`, and no page-1 `detailPage`; ayah/surah statistics map directly to
  their views. The frontend cache key includes `tableView` so tabs never cross-serve.
- Detail panels are kind-aware (word → آيات/سور; grouped → كلمات مرتبطة/آيات/سور), begin directly with
  tabs/content with no repeated summary card, and member-word rows remain strictly display-only.
- Refresh/direct URLs/Back/Forward restore mismatched table/detail kinds independently. No row is active
  while kinds differ; returning to the exact matching table kind and grammatical scope restores the
  shared active color.
- Grouped words/ayahs are server-paged with internal page 1: page 1 omits `detailPage`, pages `> 1`
  serialize it, and surahs always remove it.
- All four views render quiet, non-focusable row containers with a leading page-relative row number.
  Row click/Enter/Space is inert; only native statistic buttons act. The exact identity+scope row gets
  `aria-selected`/`aria-current` and the shared active color, cross-scope details never falsely select,
  focus returns to the originating statistic, and skeletons remain non-interactive.

## 4. Manual Smoke Flow

1. Start backend and frontend using the existing project dev commands.
2. Open `/dashboard/words/types`.
3. Confirm اسم is selected by default and rows load.
4. Switch to فعل, then choose ماض / مضارع / أمر and voice filters.
5. Switch to حرف وأداة and confirm no secondary filter appears.
6. Switch to حروف مقطعة and confirm disconnected letters are isolated.
7. Use only the row statistic buttons to open الآيات and السور; confirm row-container click/Enter/Space does nothing.
8. Confirm main-type selection loads the first page within the 2-second target in the local dev environment after initial app bootstrap.
9. Confirm the path from page open to a selected row's الآيات or التحليل view takes at most 4 interactions.
10. Change the child list scope while details remain open, then copy/reload and use Back/Forward; confirm
    list scope and the original detail scope/identity/view restore independently.

### 4a. Table-View Tabs & Grouped Detail (Feature 022 evolution)

1. From a populated Verb child with open details, browse Noun. Confirm only Noun children change; the
   strip, table, selected detail title/tab/content, URL, and request counts remain unchanged.
2. Select a Noun child. Confirm `roots` stays active, the table changes, and the original Verb detail
   remains open under its stored scope with no false row highlight.
3. Activate each root/stem/lemma statistic; confirm only the statistic acts, the URL contains exact
   numeric identity plus all five detail keys and the mapped view, and page 1 omits `detailPage`.
4. Page grouped words and ayahs. Confirm page 1 omits `detailPage`, page 2 writes `detailPage=2`,
   returning to page 1 removes it, and switching to surahs removes it regardless of the prior page.
5. Click/tap member-word rows and confirm nothing happens; use pagination and confirm only
   pagination acts.
6. Verify row 26 on page 2, no visible database IDs, quiet hover, native statistic focus, active color
   transfer/clear, no cross-scope false highlight, and no skeleton hover.
7. Check desktop split scrolling and mobile modal/RTL layout in both light and dark themes.

### 4b. Acceptance Record (last updated 2026-07-13)

The stable-shell and error-state invariants exercised by the manual flow are now covered by facade and
DOM-level automated tests (host identity across transitions, the in-table subtype prompt on a parent
scope, rows-only and later parent-tree failure strip survival, grouped-summary loading and
error-with-retry, and cross-kind row skipping):

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm test -- --include='src/app/features/words/**/*word-type*.spec.ts'   # 237 passed (13 files)
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
