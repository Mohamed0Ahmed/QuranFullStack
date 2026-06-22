# Words Explorer Table Refactor Feasibility Report

Date: 2026-06-22

Scope: report only. No source code, branches, commits, migrations, or Quranic source data changes were made.

Report location decision: this report lives under `Frontend/quran-dashboard-ui/report/ui/` because the primary change is the Unique Words Explorer UI at `/dashboard/words`. Backend impact is included here because it is limited to read DTO/search behavior and does not currently justify a second backend report.

## 1. Verdict

This is a focused refactor, not a new product feature, if it stays within the existing Words Hub and Unique Words Explorer behavior:

- Replace the card grid with a table-like explorer.
- Fix the displayed word form per mode.
- Expand search matching across existing word forms.
- Move selected-word context from a modal-first interaction toward a persistent action/context panel.
- Compute missing surahs client-side from static surah data plus mentioned-surah numbers.

Spec Kit is not needed for the recommended scope. The existing Feature 014 route, user stories, URL-state rules, and read-only data model still apply. Spec Kit should only be reconsidered if the work adds a new route, new user story, writes/curation, data model changes, migrations, or a new backend resource beyond read-model adjustments.

This is likely Frontend + Backend. The table/layout work is frontend-only, but the display and search requirements need backend read-contract changes because current list and summary DTOs expose only `displayTextUthmani`. The database already has the required fields, so this should be an additive API/read-model adjustment with no migrations.

Proceed, with no Spec Kit, on a dedicated branch after this report is accepted.

## 2. Current State

### Components, Routes, Services, State

Current frontend files involved:

- `src/app/features/words/words.routes.ts` routes `/dashboard/words`, `/dashboard/words/unique`, `/dashboard/words/unique/tashkeel`, and `/dashboard/words/unique/simple`.
- `pages/unique-words-page/unique-words-page.component.*` is the route shell.
- `components/unique-word-card/` renders each unique word as a card.
- `components/word-drilldown-modal/` renders selected-word details in a modal.
- `components/surah-occurrences-list/`, `missing-surahs-list/`, `ayah-matches-list/`, and `highlighted-ayah/` render drill-down content.
- `data-access/unique-words.api.ts` calls the backend.
- `state/unique-words.facade.ts` owns API orchestration, list state, modal state, and URL restore.
- `state/unique-words-url-sync.ts` parses and builds query params.
- `models/unique-words.models.ts` mirrors backend DTOs.

Current route and URL behavior:

- Mode is a route segment: `/dashboard/words/unique/:mode`.
- List state is query params: `search`, `sort`, `page`.
- Modal state is query params: `word`, `view`, `ap`.
- Closing the modal clears only modal params and preserves list state.
- Browser back/forward restoration is already guarded in the facade.

Current backend endpoints:

- `GET /api/words/unique/{kind}?search=&sort=&page=&pageSize=`
- `GET /api/words/unique/{kind}/{id}`
- `GET /api/words/unique/{kind}/{id}/surahs`
- `GET /api/words/unique/{kind}/{id}/missing-surahs`
- `GET /api/words/unique/{kind}/{id}/ayahs?page=&pageSize=`

Current DTO/model fields:

- List and summary return: `id`, `kind`, `displayTextUthmani`, counts, `firstVerseKey`, `firstLocation`.
- Mentioned/missing surah payloads also use `displayTextUthmani`.
- Ayah match words return `textUthmani` per occurrence for canonical highlighted ayah rendering.
- Frontend does not receive `textUthmaniSimple`, `textImlaeiSimple`, `wordKeyImlaeiSimple`, or `qpcGlyph` in list or summary payloads.

Current pagination:

- Frontend default list page size is 50.
- Backend default list page size is 50.
- Backend list page size maximum is 200.

Current modal/details behavior:

- Cards expose count chips.
- `المواضع` is disabled.
- `الآيات`, `السور`, and `لم يذكر في` open the modal.
- Modal tab state is represented by `view=surahs|missing|ayahs`.
- Ayah highlighting is ID-based via `matchedQuranWordIds`, not text replacement. This should be preserved.

## 3. Data Display Findings

Current display behavior:

- Frontend `UniqueWordListItemDto` has only `displayTextUthmani`.
- `unique-word-card.component.html` renders `word().displayTextUthmani`.
- `word-drilldown-modal.component.ts` uses `summary.displayTextUthmani` as the title.
- Backend `UniqueWordListItemDto` and `UniqueWordSummaryDto` expose only `DisplayTextUthmani`.
- Backend `EfUniqueWordsReader` projects `text_uthmani` for both tashkeel and simple modes.

Available source fields already exist:

- `quran_words.qpc_glyph`
- `quran_words.text_uthmani`
- `quran_words.text_uthmani_simple`
- `quran_words.text_imlaei_simple`
- `quran_words.word_key_imlaei_simple`
- `quran_words_unique_tashkeel.text_uthmani`
- `quran_words_unique_tashkeel.text_uthmani_simple`
- `quran_words_unique_tashkeel.text_imlaei_simple`
- `quran_words_unique_simple.word_key_imlaei_simple`
- `quran_words_unique_simple.text_uthmani`
- `quran_words_unique_simple.text_uthmani_simple`
- `quran_words_unique_simple.text_imlaei_simple`
- `quran_words_unique_simple.qpc_glyph`

Correct display mapping proposal:

| Mode | User-facing display field | Notes |
|---|---|---|
| `tashkeel` | `text_uthmani` | Canonical Uthmani with tashkeel. |
| `simple` | `text_uthmani_simple` | Preferred readable no-tashkeel Uthmani-facing display. |
| Search identity | `word_key_imlaei_simple` and/or `text_imlaei_simple` | Useful for simple grouping/search identity, not primary label. |
| Optional glyph display | `qpc_glyph` | Keep available for future Mushaf-style views, not required for table primary label. |

Backend response shape must change. The frontend does not currently have enough data to render different display text per mode.

Recommended additive DTO shape:

- Preserve existing `displayTextUthmani` temporarily for compatibility.
- Add `textUthmani`, `textUthmaniSimple`, `textImlaeiSimple`.
- Add `wordKeyImlaeiSimple` and `qpcGlyph` where the source table has them. For tashkeel rows, either omit/null `wordKeyImlaeiSimple` and `qpcGlyph`, or provide nullable fields consistently.
- Add a computed `displayText` if the API should own the mode-specific display decision. If added, map `displayText = textUthmani` for tashkeel and `displayText = textUthmaniSimple` for simple.

The safest frontend plan is to render a single mode-aware view-model field named `displayText`, produced by the facade or a small mapper, while preserving raw form fields for tooltips/debug/search labels only when needed.

## 4. Search Findings

Current search behavior is backend-only:

- `UniqueWordsApi.getList()` sends `search` to `/api/words/unique/{kind}`.
- The facade reloads the backend list when route/query state changes.
- No frontend-assisted filtering is applied to loaded items.

Current searched columns:

- `kind=tashkeel`: backend searches `quran_words_unique_tashkeel.text_imlaei_simple`.
- `kind=simple`: backend searches `quran_words_unique_simple.word_key_imlaei_simple`.
- The backend normalizes the user query by stripping tashkeel/tatweel and folding common Arabic variants.
- The SQL folds the searched column with `translate(lower(...), @foldFrom, @foldTo)`.

Current limitation:

- Search does not target all available display/search forms.
- `text_uthmani` itself is not searched.
- `text_uthmani_simple` is not searched.
- `text_imlaei_simple` is not searched in simple mode unless it equals the key.

Recommended search target fields:

For `tashkeel` rows:

- `text_uthmani_simple`
- `text_imlaei_simple`
- Optional direct literal match against `text_uthmani` only if column-side diacritic handling is implemented correctly.

For `simple` rows:

- `text_uthmani_simple`
- `text_imlaei_simple`
- `word_key_imlaei_simple`
- Optional direct literal match against `text_uthmani` with the same caution.

Do not rely on `text_uthmani ILIKE` alone for diacritic-insensitive search. The existing normalization strips diacritics from the query, but the SQL column side currently does not strip diacritics. The safer first implementation is to search the already-normalized/simple columns with OR conditions and keep `text_uthmani` as display.

Backend impact:

- Update the raw SQL projections in `EfUniqueWordsReader`.
- Expand the `WHERE` clause to OR across normalized target columns.
- Keep all user input parameterized.
- Add backend tests for searches that only match `text_uthmani_simple`, only match `text_imlaei_simple`, and only match `word_key_imlaei_simple`.

Index/performance notes:

- Current data scale is modest for list search: 21,294 tashkeel rows and 14,783 simple rows.
- Current unique-table indexes support identity/order, not multi-form contains search.
- Existing `ILIKE '%term%'` with `translate()` will not use ordinary btree indexes well.
- For v1, a scan over 15k to 21k unique rows is likely acceptable, but measure after the OR search change.
- Do not add migrations or indexes in the first refactor unless profiling shows unacceptable latency.
- If needed later, consider PostgreSQL trigram indexes or generated normalized search columns through a separate measured backend task.

Quranic text safety:

- Search must not modify stored Quran text.
- Search normalization is matching-only.
- Display text must come from canonical database columns.
- Do not synthesize fallback Quran words in frontend tests. Use existing source-safe synthetic placeholders where tests are not asserting Quran text.

## 5. Table + CDK Virtual Scroll Plan

`@angular/cdk` is not currently installed in the frontend package. CDK virtual scroll is feasible, but implementation requires adding `@angular/cdk` at the Angular 20 compatible version.

Recommended component structure:

- Keep `UniqueWordsPageComponent` as the route shell and URL coordinator.
- Replace `UniqueWordCardComponent` with a new table component, for example `unique-words-table/`.
- Add `unique-words-selection-panel/` for the left action/context column.
- Keep `word-count-chip/` if useful inside rows, or replace with compact table-cell buttons.
- Keep `ayah-matches-list` and `highlighted-ayah` for selected-word context.
- Split facade responsibilities before adding more behavior. `unique-words.facade.ts` is already above the frontend hard threshold at 583 lines. A table refactor should introduce smaller state helpers or stores rather than growing this file further.

Recommended virtual scroll strategy:

- Use `cdk-virtual-scroll-viewport` with fixed row height.
- Render only visible rows plus a small buffer.
- Keep row height stable. Avoid variable-height rows inside the virtual viewport.
- Use row selection by stable `id`, not by row index.
- Track rows by `id`.

Recommended scrolling and paging:

- Combine virtual scroll with paged/infinite backend loading.
- Do not load all 21k rows into DOM.
- Do not rely on classic page buttons as the primary browse pattern once virtual scrolling is added.
- Preserve server-side search and sort.
- On mode/search/sort change, clear accumulated rows and load from page 1.
- As the viewport approaches the loaded-row boundary, request the next backend page and append it.

Batch size:

- Current backend maximum `pageSize` is 200, so a 1000-row batch is not compatible today.
- Safest first phase: use pageSize 200 and accumulate pages into the virtual data source.
- If the product strongly wants 1000-row network batches, raise backend `MaxPageSize` only after measuring payload size and query latency, then add tests for the new bound.
- A practical compromise is 200 or 250 rows per API page, with the viewport prefetching until roughly 800 to 1000 rows are cached client-side.

Recommended row density:

- Desktop row height: 56 to 64 px.
- Include: display word, first location, occurrences, ayahs, surahs, missing count, and a compact action affordance.
- Use `var(--qd-font-quran)` for the word display cell, especially for tashkeel mode.
- Use UI sans for labels, counts, and controls.

Accessibility:

- Prefer a semantic table if CDK integration remains clean. If native table semantics conflict with virtual scroll wrappers, use `role="grid"` / `role="row"` / `role="columnheader"` / `role="gridcell"` carefully.
- Selection must expose `aria-selected`.
- Keyboard should support arrow movement, Enter/Space select, and visible focus.
- Do not rely on color alone for the selected row.
- Keep loading, empty, and error states outside the scroll viewport where possible so assistive tech can announce them predictably.

## 6. Layout Plan

Desktop RTL layout:

- Right column: table explorer, about 70% width.
- Left column: selected-word action/context panel, about 30% width.
- Use CSS grid with `grid-template-columns: minmax(0, 7fr) minmax(18rem, 3fr)` in RTL source order or explicit grid placement so the table remains visually on the right.
- Keep the filter/search toolbar above the two-column body, spanning both columns.

Tablet behavior:

- Keep two columns only while the action panel remains readable.
- Consider 60/40 at medium widths.
- If width is constrained, move the action panel below the table but keep selected-row context visible.

Mobile behavior:

- Use a single column.
- Table rows become compact list rows with the same data ordering.
- Selected-word context can become an inline panel below the selected row or a bottom sheet/dialog if screen height is constrained.

Modal replacement recommendation:

- Move selected-word summary, mentioned surahs, missing surahs, and ayah matches into the left action/context panel.
- Keep the existing modal only as a transitional fallback for mobile if the panel becomes too cramped.
- Long-term, the modal should not be the primary desktop interaction because the new layout explicitly reserves a context column.

URL state preservation:

- Preserve `word=<id>` as the selected row.
- Preserve `view=surahs|missing|ayahs` as the active panel tab.
- Preserve `ap=<page>` for ayah-match pagination.
- Closing/clearing selection should remove `word`, `view`, and `ap` only.
- Browser back/forward should continue to restore mode, search, sort, page or loaded-scroll equivalent, selected word, active panel tab, and ayah page.

Avoiding test breakage:

- Keep test IDs stable where possible or add compatible replacements before deleting card-specific tests.
- Replace "renders one card per item" tests with "renders table rows for loaded items."
- Keep URL sync specs largely unchanged.
- Keep facade restore tests unchanged where possible by preserving the same query params.

## 7. Missing Surahs Plan

Existing static catalog:

- `src/app/features/mushaf/data/mushaf-surah-juz-catalog.json` contains Arabic surah names and start pages.
- `src/app/features/mushaf/data/mushaf-surah-juz-catalog.ts` exports grouped juz data and `MUSHAF_SURAH_START_PAGES`.
- Existing tests assert 30 juz groups and 114 unique surahs.

Important shape detail:

- The catalog is grouped by juz and repeats long surahs across adjacent juz groups.
- Missing-surah computation should first derive a deduped ordered catalog:
  `[{ surahNumber, nameArabic, startPageNumber }]` for 1 through 114.

Computation strategy:

- Load mentioned surah numbers for the selected word from the existing mentioned-surahs data.
- Compute `missingSurahs = allStaticSurahs.filter(s => !mentionedSet.has(s.surahNumber))`.
- Validate `mentioned + missing = 114` in tests.
- Display the existing backend `missingSurahsCount` or a client-derived count, but ensure it matches `114 - mentionedSurahs.length`.

Backend endpoint impact:

- Do not remove `GET /missing-surahs` in this refactor.
- Stop using it from the frontend once mentioned-surah data and static catalog computation are in place.
- Keep the endpoint as a compatibility/read-model path until a later cleanup explicitly removes it.

Surah name font:

- The project defines Quran/Mushaf font tokens: `--qd-font-quran`, `--qd-font-mushaf-surah-name`, `--qd-font-mushaf-surah-name-v2`, and `--qd-font-mushaf-common`.
- For plain Arabic surah names such as `الفاتحة`, use `var(--qd-font-quran)` or the existing Amiri-based Quran text style for scholarly readability.
- Use the dedicated Mushaf surah-name ligature fonts only when rendering encoded ligature strings, not plain catalog names.

## 8. Branch and Commit Strategy

Current git status observed:

- FullStack/App: `main...origin/main`, clean.
- Backend: `main...origin/main`, clean.
- Frontend: `main...origin/main`, clean.

Recommended branches:

- If implementing frontend table/layout only: create a branch only in `Frontend/quran-dashboard-ui`.
- If implementing the recommended display/search fixes: create branches in both `Backend` and `Frontend/quran-dashboard-ui`.
- Create a FullStack/App branch only if child repo commits need to be recorded as submodule pointer updates or workspace docs are changed.

Suggested branch names:

- Frontend: `014-words-explorer-table-refactor`
- Backend, if touched: `014-words-explorer-display-search-refactor`
- FullStack/App, if submodule pointers are committed: `014-words-explorer-refactor`

Suggested commit sequence:

1. Backend: Add unique-word display/search fields and multi-form search tests.
2. Frontend: Update DTO models/mappers and display-mode tests.
3. Frontend: Add CDK dependency and table virtual-scroll skeleton.
4. Frontend: Add selection panel and URL-state preservation.
5. Frontend: Compute missing surahs from static catalog and stop calling missing-surahs endpoint.
6. Frontend: Update component/facade tests and visual smoke notes.
7. FullStack/App: commit submodule pointer updates only after child commits exist.

This should be a new PR unless there is an already-open Feature 014 PR intended to absorb post-review refactors. Because all repos are currently on `main`, the safest assumption is a new focused refactor PR.

## 9. Risks

URL state regression:

- The current modal query params are well-tested.
- Reinterpreting them as selected-panel state is safe only if `word`, `view`, and `ap` semantics stay stable.

Search behavior regression:

- Expanding search from one column to multiple columns can change result counts and alpha-sort expectations.
- Existing tests that assume exact totals for fixture searches will need updates.

Performance risk:

- CDK virtual scroll requires fixed row height and careful container sizing.
- Infinite loading can duplicate or skip pages if route/search/sort changes race with in-flight requests.
- 1000-row backend batches are not currently allowed and should not be introduced without measuring payload/query cost.

API contract drift:

- Add fields instead of renaming/removing `displayTextUthmani` in the first backend change.
- Frontend should move to a view-model `displayText` while raw DTO fields remain explicit.

Angular dependency risk:

- `@angular/cdk` is absent. Adding it changes `package.json` and lockfile.
- Use the Angular 20 compatible CDK version.

Existing tests likely needing updates:

- `unique-words-page.component.spec.ts`: card assertions become table-row assertions.
- `unique-word-card` tests: replaced or removed with new table row tests.
- `word-drilldown-modal` tests: reduced if desktop panel replaces modal.
- `unique-words.facade.*.spec.ts`: preserve URL tests, add selection/infinite-load cases.
- `unique-words.api.spec.ts`: DTO shape and stopped missing endpoint use.
- Backend `UniqueWordsSearchSortPagingTests`: multi-form search and new page-size bound if changed.
- Backend list/summary read tests: new DTO fields.

## 10. Recommended Implementation Phases

### Phase A: Data, Display, Search Audit Fixes

- Add backend DTO fields for the available word forms.
- Map `displayText` by mode.
- Update frontend models and view-model mapper.
- Expand backend search to `text_uthmani_simple`, `text_imlaei_simple`, and `word_key_imlaei_simple` where available.
- Keep existing UI shape during this phase.

### Phase B: Table Skeleton + Virtual Scroll

- Add Angular CDK.
- Create `unique-words-table`.
- Render current paged data in a fixed-height virtual viewport.
- Start with backend page size 200 unless a measured backend change raises the limit.
- Keep selected word behavior simple: click row selects and updates query params.

### Phase C: Left Action Panel

- Create `unique-words-selection-panel`.
- Move summary, counts, surah list, missing list, and ayah matches into the panel.
- Preserve `word`, `view`, and `ap` query params.
- Keep modal only for mobile or remove it after parity tests pass.

### Phase D: Missing Surahs Frontend Computation

- Add a small deduped static surah catalog helper near the Mushaf catalog or Words feature.
- Load mentioned surahs, compute missing surahs client-side.
- Stop calling `getMissingSurahs` from the frontend.
- Do not remove the backend endpoint in this refactor.

### Phase E: Tests + Review

- Update unit tests after each phase.
- Run focused frontend tests first, then full frontend test suite.
- Run backend tests only if backend is touched.
- Run a design/visual smoke after the table and panel are in place.

## 11. Testing Strategy

Focused frontend tests first:

- DTO/view-model mapper maps tashkeel display to `textUthmani`.
- DTO/view-model mapper maps simple display to `textUthmaniSimple`.
- Table renders only loaded rows and marks selected row by id.
- Query params still restore mode/search/sort/page/word/view/ap.
- Infinite loading requests next pages only near the viewport boundary.
- Missing surahs helper derives 114-partition from static catalog plus mentioned set.

Focused backend tests if backend is touched:

- List and summary include new display/search fields.
- Tashkeel search matches `text_uthmani_simple` and `text_imlaei_simple`.
- Simple search matches `text_uthmani_simple`, `text_imlaei_simple`, and `word_key_imlaei_simple`.
- Page-size validation remains bounded. If max changes, test the new max and rejection above it.

Full test sequence near the end:

- `npm test` in `Frontend/quran-dashboard-ui`.
- `dotnet test` in `Backend` only if backend code changed.
- If CDK table work is implemented, run a browser visual smoke for:
  - `/dashboard/words/unique/tashkeel`
  - `/dashboard/words/unique/simple`
  - search with and without tashkeel
  - row selection and back/forward
  - selected panel tabs
  - mobile width

Recommended visual smoke:

- Start frontend and backend normally.
- Open `/dashboard/words/unique/tashkeel`.
- Confirm the table is on the right and the context panel is on the left in RTL desktop.
- Switch to simple mode and confirm the primary display has no tashkeel.
- Scroll enough to trigger the next page.
- Select a row, switch panel views, then use browser back/forward.

## Final Recommendation

Proceed with the refactor.

No Spec Kit is needed for the recommended scope.

Use a new focused branch. Expect Backend + Frontend branches if the display/search fixes are included, with child repo commits before any FullStack/App submodule pointer commit.

First implementation prompt outline:

1. Implement additive backend Unique Words DTO fields and multi-form search without schema changes.
2. Update frontend DTOs and create a mode-aware `displayText` mapper.
3. Add Angular CDK and replace the card list with a virtualized table using paged/infinite loading.
4. Move selected-word details into a left-side context panel while preserving `word`, `view`, and `ap` query params.
5. Derive missing surahs from the static Mushaf surah catalog plus mentioned surah numbers.
6. Update focused tests phase by phase, then run the full relevant suites.
