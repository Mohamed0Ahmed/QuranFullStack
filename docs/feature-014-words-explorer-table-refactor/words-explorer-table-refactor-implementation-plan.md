# Words Explorer Table Refactor - Mini Implementation Plan

Date: 2026-06-22

Source report: `Frontend/quran-dashboard-ui/report/ui/words-explorer-table-refactor-feasibility-report.md`

Scope: focused refactor plan only. This is not Spec Kit and does not introduce a new feature specification.

## 1. Verdict

This is a focused refactor of the existing Feature 014 Words Hub and Unique Words Explorer.

Spec Kit is not needed if the work stays inside the existing `/dashboard/words` behavior and preserves the current route and query-param contract.

Expected repo impact:

- Backend: likely touched for additive read-contract fields and normalized no-tashkeel search behavior.
- Frontend: definitely touched for DTO/model mapping, table UI, Angular CDK virtual scroll, selected-word context panel, and frontend missing-surah computation.
- FullStack/App: touched only if implementation commits in child repos need submodule pointer tracking, or if implementation docs are updated in workspace docs.

Recommended branch setup:

- Backend branch, only if backend is touched: `014-words-explorer-display-search-refactor`
- Frontend branch: `014-words-explorer-table-refactor`
- FullStack/App branch, only if recording docs or submodule pointers: `014-words-explorer-refactor`

Hard constraints:

- No new user story.
- No new route.
- No migrations.
- No Quranic source data changes.
- No write or curation behavior.
- No backend endpoint removal in this refactor.
- Backend changes are additive read-contract/search adjustments only.
- Preserve URL state semantics: `search`, `sort`, `page`, `word`, `view`, `ap`.
- Preserve browser back/forward behavior.
- Preserve ID-based ayah highlighting. Do not use string replacement.
- Compute missing surahs frontend-side when safe, but do not delete the backend missing-surahs endpoint.

## 2. Branch Strategy

Current baseline before implementation should be:

- FullStack/App: `main`
- Backend: `main`
- Frontend: `main`

Phase 0 must verify all three repos before creating branches. If any unrelated change exists, stop and classify it before starting implementation.

Recommended branch sequence:

1. Create Backend branch only if Phase 1 is needed:
   `git -C Backend checkout -b 014-words-explorer-display-search-refactor`
2. Create Frontend branch:
   `git -C Frontend/quran-dashboard-ui checkout -b 014-words-explorer-table-refactor`
3. Create FullStack/App branch only if the workspace must record docs or child submodule pointers:
   `git checkout -b 014-words-explorer-refactor`

Commit order:

1. Backend child repo commits first, if backend changed.
2. Frontend child repo commits second.
3. FullStack/App commit last, and only after child commits exist, for submodule pointers or workspace docs.

Do not commit a FullStack/App submodule pointer that references uncommitted child repo work.

Suggested commit breakdown:

1. Backend: `Add unique word display forms and normalized search`
2. Frontend: `Map unique word display text by mode`
3. Frontend: `Add virtualized unique words table`
4. Frontend: `Add words explorer selection panel`
5. Frontend: `Compute missing surahs from static catalog`
6. Frontend: `Harden words explorer tests and responsive behavior`
7. FullStack/App, if needed: `Track words explorer refactor updates`

## 3. Execution Phases

### Phase 0 - Branch and Baseline

Goal:

- Confirm clean starting state.
- Create only the required branches.
- Capture baseline test/build status before source changes.
- Avoid implementation until the branch and baseline are known.

Files likely touched:

- None for implementation.
- Optional local notes only if the implementer records command output separately.

Explicit non-goals:

- Do not edit Backend source.
- Do not edit Frontend source.
- Do not edit package files.
- Do not run Spec Kit.
- Do not create migrations.

Acceptance checks:

- `git -C . status --short --branch`
- `git -C Backend status --short --branch`
- `git -C Frontend/quran-dashboard-ui status --short --branch`
- Required branches created only in repos that will be touched.
- Baseline command results recorded in the implementation summary.

Suggested focused tests:

- Frontend baseline: `npm test` from `Frontend/quran-dashboard-ui` if practical.
- Backend baseline: `dotnet test` from `Backend` if Phase 1 will touch backend.
- If full test runs are too slow, run the existing Words-focused backend/frontend tests and record that the full suite was deferred.

Commit recommendation:

- No commit for Phase 0 unless workspace documentation is intentionally updated.

Review gate:

- Confirm no unexpected dirty files and no branch was created in an untouched repo.

### Phase 1 - Backend Display/Search Contract

Goal:

- Add the read-contract data needed for correct display and normalized no-tashkeel search.
- Keep changes additive and read-only.
- Preserve existing endpoints and existing `displayTextUthmani` during transition.

Files likely touched:

- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordListItemDto.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordSummaryDto.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordSurahsResponse.cs`, only if selected-word titles need the same fields.
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordMissingSurahsResponse.cs`, only if kept aligned.
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsListReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordSummaryTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsSearchSortPagingTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/unique-words-seed.sql`, only if the current fixture cannot prove the new search cases.

Implementation details:

- Add additive fields for the available word forms:
  - `textUthmani`
  - `textUthmaniSimple`
  - `textImlaeiSimple`
  - `wordKeyImlaeiSimple`, nullable where not applicable
  - `qpcGlyph`, nullable where not applicable
- Preserve `displayTextUthmani` temporarily.
- Decide one strategy and keep it consistent:
  - Backend-owned `displayText`, with `tashkeel -> textUthmani` and `simple -> textUthmaniSimple`.
  - Or raw fields only, with frontend mapper owning `displayText`.
- Prefer backend-owned `displayText` only if API consumers benefit from one canonical display decision. Otherwise keep the display decision in frontend view-model mapping.
- Search is normalized/no-tashkeel only. `text_uthmani` / Uthmani-with-tashkeel is display-only and must not be searched directly in this refactor.
- User input may contain tashkeel, tatweel, Quranic annotation marks, small signs, dagger alif, or display-specific Arabic forms, but the query must be normalized/folded before matching safe no-tashkeel/search columns.
- Expand search across safe normalized/search fields:
  - `text_uthmani_simple`
  - `text_imlaei_simple`
  - `word_key_imlaei_simple` for simple mode
- Do not add `text_uthmani ILIKE`, `translate(text_uthmani, ...)`, or any other direct `text_uthmani` search condition.
- Keep SQL parameterized.
- Keep server-side search/sort.
- Do not add indexes unless measured latency proves a need.

Explicit non-goals:

- No migrations.
- No new endpoint.
- No endpoint removal.
- No source data changes.
- No write path.
- No change to deterministic unique-word IDs.
- No change to ayah highlighting data shape unless strictly additive.

Acceptance checks:

- Existing endpoints still return successful responses.
- List and summary responses expose the added fields.
- `displayTextUthmani` still exists.
- Search matches across the intended simple/search forms after query-side normalization.
- Pasted visible Uthmani input with tashkeel/Quranic marks matches through safe no-tashkeel/search columns.
- SQL does not search `text_uthmani` directly.
- Invalid kind, invalid sort, invalid page, and not-found behavior remain controlled.
- Backend list `pageSize` max remains unchanged unless a deliberate decision and tests change it.

Suggested focused tests:

- Backend list read includes `textUthmani`, `textUthmaniSimple`, and `textImlaeiSimple`.
- Simple rows include `wordKeyImlaeiSimple` and `qpcGlyph` if the selected contract exposes them.
- Tashkeel search matches `text_uthmani_simple`.
- Tashkeel search matches `text_imlaei_simple`.
- Simple search matches `text_uthmani_simple`.
- Simple search matches `text_imlaei_simple`.
- Simple search matches `word_key_imlaei_simple`.
- Pasted visible-form query such as an existing fixture value with alef wasla/tashkeel normalizes and matches through `text_uthmani_simple` or `text_imlaei_simple`.
- Pasted visible-form query such as an existing fixture value with a final Quranic annotation mark normalizes and matches through `text_uthmani_simple`, `text_imlaei_simple`, or `word_key_imlaei_simple`.
- Tests should prove normalization behavior; they should not assert or require direct `text_uthmani` search.
- Existing no-match search still returns success with `totalCount = 0`.

Commit recommendation:

- Commit as a single backend commit if the contract and search changes stay cohesive:
  `Add unique word display forms and normalized search`

Review gate:

- Backend review checks API compatibility, SQL safety, Quranic text safety, and test coverage before frontend consumes the new fields.

### Phase 2 - Frontend DTO/Model/Display Mapper

Goal:

- Consume the additive backend fields.
- Fix the display bug before any table refactor.
- Keep the current card UI temporarily so the data/display change can be verified independently.

Files likely touched:

- `Frontend/quran-dashboard-ui/src/app/features/words/models/unique-words.models.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/data-access/unique-words.api.ts`, only if request/response typing needs adjustment.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.ts`
- A new small mapper/helper under `src/app/features/words/`, preferably near `models/` or `state/`.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-word-card/unique-word-card.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-drilldown-modal/word-drilldown-modal.component.ts`, if modal title uses mapped display text during transition.
- Relevant `*.spec.ts` files under `src/app/features/words/`.

Implementation details:

- Add frontend DTO fields matching the backend additive contract.
- Add a mode-aware mapper that creates a view model with `displayText`.
- Display mapping:
  - `tashkeel` mode: Uthmani with tashkeel.
  - `simple` mode: Uthmani simple / without tashkeel.
- Keep ID, kind, counts, first location, and drill-down behavior unchanged.
- Keep card UI during this phase.
- Keep query params unchanged.

Explicit non-goals:

- Do not add Angular CDK yet.
- Do not replace cards yet.
- Do not change layout.
- Do not change routes.
- Do not remove modal behavior.
- Do not stop using the missing-surahs endpoint yet.

Acceptance checks:

- In `tashkeel`, primary display uses the tashkeel form.
- In `simple`, primary display uses the no-tashkeel Uthmani simple form.
- Existing card and modal interactions still work.
- `search`, `sort`, `page`, `word`, `view`, and `ap` still restore from URL.
- Back/forward behavior remains stable.

Suggested focused tests:

- Mapper test: tashkeel DTO maps `displayText` to `textUthmani`.
- Mapper test: simple DTO maps `displayText` to `textUthmaniSimple`.
- Page/component test renders mapped display text.
- Facade restore tests still pass for selected word ID and view.
- API spec accepts the new response shape without breaking old fields.

Commit recommendation:

- Commit as:
  `Map unique word display text by mode`

Review gate:

- Confirm display bug is fixed in isolation before starting the table refactor.

### Phase 3 - Table Skeleton + Angular CDK Virtual Scroll

Goal:

- Replace card browsing with a table-like explorer using Angular CDK virtual scroll.
- Avoid rendering thousands of DOM rows.
- Preserve server-side search/sort and stable selected-word identity.

Files likely touched:

- `Frontend/quran-dashboard-ui/package.json`
- `Frontend/quran-dashboard-ui/package-lock.json`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.scss`
- New `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-table/`
- New or updated frontend state helpers for accumulated pages/infinite loading.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.ts`, ideally reduced or split rather than expanded.
- Relevant Words component/facade specs.

Implementation details:

- Add `@angular/cdk` only in this phase, using the Angular 20 compatible version.
- Use `cdk-virtual-scroll-viewport`.
- Use fixed row height, approximately 56 to 64 px on desktop.
- Track rows by stable `id`.
- Select rows by stable `id`, not array index.
- Keep server-side search and sort.
- Combine virtual scroll with paged/infinite loading.
- Start with current backend max page size of 200 unless Phase 1 deliberately changes the max after measurement.
- Clear accumulated rows when mode/search/sort changes.
- Avoid duplicate page requests during in-flight loading.
- Do not use classic pagination as the primary browse model after virtual scroll is active, but keep URL `page` semantics stable.

Explicit non-goals:

- Do not load every unique word into the DOM.
- Do not fetch all rows in one backend request.
- Do not change backend max page size unless already decided in Phase 1.
- Do not introduce a new route.
- Do not move selected-word context yet beyond the minimal row-selection behavior needed for the table.

Acceptance checks:

- Table renders visible rows only.
- Scrolling near the loaded boundary fetches the next page once.
- Search and sort reset accumulated rows and reload from the start.
- Selected row remains tied to `word=<id>`.
- Back/forward still restores selection and list state.
- Loading, empty, and error states remain visible and accessible.

Suggested focused tests:

- Table component renders rows from input data.
- Table emits row selection by `id`.
- Virtual scroll/infinite-load helper requests next page only once per threshold.
- Facade clears accumulated rows on mode/search/sort change.
- Component test confirms card-specific assertions are replaced by table-row assertions.

Commit recommendation:

- If dependency and table skeleton are small enough:
  `Add virtualized unique words table`
- If large, split into:
  - `Add Angular CDK for words explorer table`
  - `Render unique words in virtualized table`

Review gate:

- Frontend structure review: ensure the facade does not keep growing past its current hard-threshold problem.
- UI review: fixed row height, RTL behavior, focus state, and no DOM over-rendering.

### Phase 4 - Left Action/Context Panel

Goal:

- Convert desktop interaction from modal-first to a persistent selected-word context panel.
- Preserve the existing selected-word URL state.
- Keep all current drill-down views available.

Files likely touched:

- `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.*`
- New `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-selection-panel/`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-drilldown-modal/`, either retained for mobile fallback or reduced.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/surah-occurrences-list/`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/missing-surahs-list/`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/ayah-matches-list/`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/highlighted-ayah/`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.ts` or split selection/drill-down state helpers.
- Relevant component/facade specs.

Implementation details:

- Desktop RTL layout:
  - Right column: table, about 70%.
  - Left column: action/context panel, about 30%.
- Use a stable two-column CSS grid at desktop widths.
- Keep search/tabs toolbar above the table/panel region.
- Preserve:
  - `word=<id>` as selected word.
  - `view=surahs|missing|ayahs` as active panel tab.
  - `ap=<page>` as ayah page.
- Closing/clearing selection removes `word`, `view`, and `ap` only.
- Decide whether the modal remains:
  - Recommended: keep it only as mobile/transitional fallback until panel parity is proven.
  - Desktop should use the persistent panel.
- Keep ID-based highlighted ayahs exactly as-is.

Explicit non-goals:

- No new drill-down type.
- No occurrence-count drill-down for `المواضع`.
- No new route.
- No string-replacement highlighting.
- No write/curation actions, even though the panel is reserved for future actions.

Acceptance checks:

- Desktop shows table on the right and panel on the left.
- Tablet behavior remains readable.
- Mobile has a safe one-column or fallback modal behavior.
- Selecting a row opens/updates the panel.
- Switching panel tabs updates `view`.
- Ayah page changes update `ap`.
- Browser back/forward restores selected word and active panel tab.

Suggested focused tests:

- Page component renders selection panel when `word` state exists.
- Selection panel tabs emit `view` changes.
- Closing selection clears modal/selection query params only.
- `highlighted-ayah` tests remain ID-based.
- Responsive behavior is covered by visual smoke rather than brittle unit tests.

Commit recommendation:

- Commit as:
  `Add words explorer selection panel`

Review gate:

- UI/accessibility review for RTL two-column layout, keyboard focus, selected row state, and mobile fallback.

### Phase 5 - Missing Surahs Frontend Computation

Goal:

- Compute missing surahs in the frontend from static surah catalog plus mentioned-surah numbers.
- Stop depending on the frontend missing-surahs API call if parity is proven.
- Keep the backend endpoint for compatibility.

Files likely touched:

- `Frontend/quran-dashboard-ui/src/app/features/mushaf/data/mushaf-surah-juz-catalog.ts`
- New helper near the existing catalog or under `src/app/features/words/`, depending on ownership.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.ts` or selection-panel state helper.
- `Frontend/quran-dashboard-ui/src/app/features/words/data-access/unique-words.api.ts`, only to stop calling `getMissingSurahs` from active UI flow. Do not remove the method unless the team wants it retained unused for compatibility tests.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/missing-surahs-list/`
- Relevant frontend specs.

Implementation details:

- Existing static catalog is grouped by juz and repeats long surahs.
- Derive a deduped 114-surah list ordered by `surahNumber`.
- Each deduped item should include at least:
  - `surahNumber`
  - `nameArabic`
  - `startPageNumber`, if useful for future navigation.
- When mentioned surahs load, compute:
  `missingSurahs = allStaticSurahs.filter(s => !mentionedSet.has(s.surahNumber))`
- Validate `mentionedSurahs.length + missingSurahs.length === 114` for normal data.
- Use project Quran/surah typography tokens:
  - Plain Arabic names should use `var(--qd-font-quran)` or the existing Amiri-based Quran text style.
  - Do not use Mushaf surah-name ligature fonts for plain catalog strings unless encoded ligature strings are provided.

Explicit non-goals:

- Do not delete backend `GET /missing-surahs`.
- Do not remove backend tests for that endpoint.
- Do not change static catalog source data.
- Do not invent surah names.
- Do not add navigation actions unless already present.

Acceptance checks:

- Missing list matches 114 minus mentioned surah numbers.
- Missing list is ordered by surah number.
- Empty missing list is handled when a word appears in all surahs.
- Backend missing-surahs endpoint remains available.
- UI no longer needs a network request for missing surahs when mentioned-surah data is available.

Suggested focused tests:

- Static catalog helper returns 114 deduped surahs.
- Helper preserves Arabic names.
- Missing-surah computation returns complement of mentioned set.
- Empty complement is handled.
- Facade/panel uses computed missing surahs when `view=missing`.

Commit recommendation:

- Commit as:
  `Compute missing surahs from static catalog`

Review gate:

- Quranic data safety review: catalog is existing static data, no fabricated surah names, no backend endpoint removal.

### Phase 6 - Hardening and Review

Goal:

- Stabilize tests, build, visual behavior, performance, and review readiness.

Files likely touched:

- Focused test files only.
- Minor frontend styles if visual smoke exposes layout issues.
- Documentation or implementation summary if requested.

Explicit non-goals:

- No scope expansion.
- No new features.
- No endpoint removal.
- No migrations.
- No unrelated cleanup.

Acceptance checks:

- Focused backend tests pass if backend changed.
- Focused frontend tests pass.
- Full frontend tests pass near the end.
- Build passes.
- Visual smoke confirms desktop/tablet/mobile layout.
- Back/forward behavior works with `search`, `sort`, `page`, `word`, `view`, `ap`.
- Virtual scroll does not render thousands of rows.
- Selected ayah highlighting remains ID-based.

Suggested focused tests:

- Run Words-focused frontend specs first.
- Run full `npm test` in `Frontend/quran-dashboard-ui`.
- Run backend Words tests if backend changed.
- Run full `dotnet test` in `Backend` if backend changes are broad or shared.

Commit recommendation:

- Commit final hardening as:
  `Harden words explorer table refactor`
- Avoid a vague "fix tests" commit if changes can be folded into their phase commits.

Review gate:

- Run `engineering-review` before merge.
- Run `performance-angular-review` if virtual scroll/infinite loading is implemented and there are concerns about rendering, repeated requests, or bundle/dependency impact.

## 4. Risk Controls

URL state regression:

- Preserve the same query keys: `search`, `sort`, `page`, `word`, `view`, `ap`.
- Keep query parsing centralized in `unique-words-url-sync.ts`.
- Keep route mode unchanged.
- Add tests for open panel, switch view, close selection, and back/forward restore.

Search result count changes:

- Expect result counts to change when search expands across safe no-tashkeel/search fields and when pasted visible-form input normalizes more completely.
- Update tests to assert behavior and representative matches, not brittle totals unless the fixture is intentionally shaped for exact totals.
- Keep empty-search and no-match behavior unchanged.
- Treat Uthmani-with-tashkeel as display-only; do not resolve count changes by querying `text_uthmani` directly.

Virtual scroll plus pagination race conditions:

- Track in-flight page requests.
- Ignore stale responses when mode/search/sort changes.
- Deduplicate rows by stable `id`.
- Request the next page only once per threshold.
- Clear accumulated rows on filter/sort/mode changes.

API contract drift:

- Add fields; do not remove or rename existing fields in the first backend change.
- Preserve `displayTextUthmani` during transition.
- Keep frontend mapper tolerant of additive fields.
- Do not expose raw technical keys as the primary user-facing label.

Accessibility:

- Provide visible focus for rows and panel tabs.
- Mark selected row without color-only meaning.
- Use table/grid semantics consistently.
- Keep loading/empty/error states announceable.
- Preserve keyboard access for selection and tab switching.

Responsive layout:

- Desktop: 70/30 table and context panel.
- Tablet: allow 60/40 or stacked if needed.
- Mobile: single column or modal/bottom-sheet fallback.
- Avoid clipped Arabic text and unstable viewport heights.

Angular CDK dependency risk:

- Add `@angular/cdk` only in Phase 3.
- Match Angular 20 dependency versions.
- Check bundle/test impact.
- Keep CDK usage isolated to the table/scroll component.

Existing tests requiring updates:

- Card rendering tests become table-row rendering tests.
- Modal tests may become selection-panel tests.
- URL sync tests should remain mostly stable.
- Facade tests need new accumulated-page and selection behavior coverage.
- Backend search tests need representative normalized no-tashkeel search-column cases.
- Backend search tests need pasted visible-form normalization cases, but must keep search targets limited to no-tashkeel/search fields.

## 5. Testing Matrix

### Backend Focused Tests

Run when Phase 1 touches backend:

- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsListReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordSummaryTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsSearchSortPagingTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordSurahDrilldownTests.cs`, if response title/display fields are expanded there.
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordAyahMatchesTests.cs`, only if ayah match contracts are touched.

Backend assertions:

- Additive fields exist and map to source columns.
- Tashkeel display data remains canonical.
- Simple display data uses Uthmani simple/no-tashkeel.
- Search covers Uthmani simple, imlaei simple, and simple word key through normalized no-tashkeel matching.
- Pasted visible Uthmani input is accepted by query normalization, not by direct `text_uthmani` search.
- No endpoint removal.
- No migrations generated.

### Frontend Focused Tests

Run phase by phase:

- `unique-words.models` or mapper tests for mode-aware display.
- `unique-words.api.spec.ts` for response typing.
- `unique-words.facade.list.spec.ts` for list loading and accumulated pages.
- `unique-words.facade.restore.spec.ts` for restored selected word.
- `unique-words-url-sync.spec.ts` for query-param preservation.
- New `unique-words-table` component tests.
- New `unique-words-selection-panel` component tests.
- Static surah catalog/missing-surah helper tests.
- Existing `highlighted-ayah` tests must continue to prove ID-based highlighting.

### Full Test Timing

- After Phase 1: run backend focused tests; run full backend tests if shared contracts were touched broadly.
- After Phase 2: run frontend focused mapper/facade/page tests.
- After Phase 3: run frontend focused table and facade tests.
- After Phase 4: run frontend focused panel, URL, and highlighted ayah tests.
- After Phase 5: run missing-surah helper/facade tests.
- Before merge: run full frontend test suite.
- Before merge: run backend full suite if backend was touched.

### Visual Smoke / Playwright MCP Flow

Suggested manual or Playwright-assisted smoke:

1. Open `/dashboard/words/unique/tashkeel`.
2. Confirm table appears on the right in desktop RTL layout.
3. Confirm action/context panel appears on the left in desktop RTL layout.
4. Confirm tashkeel mode displays Uthmani with tashkeel.
5. Switch to `/dashboard/words/unique/simple`.
6. Confirm simple mode displays Uthmani simple/no-tashkeel.
7. Search with a pasted visible-form query containing tashkeel/Quranic marks.
8. Search with the same query without tashkeel.
9. Scroll until the next page loads.
10. Confirm DOM row count stays bounded.
11. Select a word row.
12. Switch panel views: `السور`, `لم يذكر في`, `الآيات`.
13. Confirm ayah highlighting marks only matched `quranWordId` occurrences.
14. Change ayah page and confirm `ap` updates.
15. Use browser back/forward and confirm route/query state restores.
16. Check tablet width.
17. Check mobile width and confirm the panel fallback is usable.

## 6. Final Recommendation

Proceed.

No Spec Kit is needed for this focused refactor.

Begin with Phase 1 if the backend fields are not already available through the API. That is the recommended first implementation phase because it fixes the display/search contract before the UI is reshaped.

If the team decides to defer backend search expansion, begin with Phase 2 only after confirming the frontend has the necessary display fields. Based on the feasibility report, the current frontend does not have enough data, so backend-first is the safer path.

Backend and Frontend should be implemented on child repo branches first. FullStack/App should only receive the final docs/submodule pointer update after child commits exist.
