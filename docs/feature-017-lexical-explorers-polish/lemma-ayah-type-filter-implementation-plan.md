# Lemma Ayah Type Filter Implementation Plan

**Feature:** 017 Lexical Explorers Polish  
**Scope:** Lemmas Explorer only, route `/dashboard/words/lemmas`  
**Source:** `docs/feature-017-lexical-explorers-polish/lemma-ayah-type-filter-focused-report.md`

## 11. Verdict

**READY_FOR_IMPLEMENTATION**

Backend support is required and current data already provides the stable POS identity needed for filtering. No blocking clarification remains.

## 12. Scope and non-goals

- Apply now only to Lemmas Explorer.
- Do not implement direct Stems changes in this pass.
- Do not touch Word Types Explorer.
- No migrations.
- No importers.
- No Quran data mutation.
- No global `ApiResponse<T>` contract changes.
- No shared `qd-type-distribution-list` redesign.

## 13. Current-state references read

- `Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs` exposes `GET /api/words/lemmas/{id}/ayahs?page&pageSize` only.
- `Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/GetLemmaAyahs/GetLemmaAyahsQuery.cs` carries only `Id`, `Page`, and `PageSize`.
- `Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/GetLemmaAyahs/GetLemmaAyahsHandler.cs` validates id/paging and calls `ILemmasReader.GetLemmaAyahMatchesAsync`.
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/ILemmasReader.cs` has no type-filter parameter today.
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs` filters ayahs by lemma only, pages distinct ayahs, excludes ayah markers, and computes `IsMatched` from lemma-only matched ids.
- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Lemmas/LemmasCacheKeys.cs` builds `Ayahs(id, page, pageSize)` keys with no filter dimension.
- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Lemmas/CachedLemmasReader.cs` caches ayah pages by lemma + page only.
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/Responses/LemmaSummaryDto.cs` already exposes `TypeDistribution`.
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Morphology/Responses/TypeSummaryDto.cs` already exposes stable `Code`, labels, and `OccurrencesCount`.
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html` renders shared `qd-type-distribution-list` in always-visible panel chrome.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail.facade.ts` tracks selection, view, and detail page, but no type filter state.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-url-sync.ts` serializes search/sort/list/page and detail selection keys only.
- `Frontend/quran-dashboard-ui/src/app/features/words/data-access/lemmas.api.ts` calls the ayah endpoint without `typeCode`.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/type-distribution-list/type-distribution-list.component.ts` is shared by Lemmas and Stems, so changing it directly would create side effects.
- `Frontend/quran-dashboard-ui/src/app/features/words/utils/lemma-ayah-match.mapper.ts` already maps Lemmas ayah DTOs into the shared ayah list shape.

## 14. Target UX behavior

- Remove always-visible type distribution from general Lemmas details panel.
- Show compact type filters only inside the Ayahs tab, above ayah matches.
- Default selected filter: `عرض الكل`.
- Each filter chip/card shows Arabic label + occurrence count, e.g. `حرف نفي — 1364 مرة`.
- Layout should wrap compactly: 4 per row on wide desktop, 3 on smaller desktop/tablet, 2 or 1 on mobile.
- `عرض الكل` means no type filter.
- Selecting a type filter sets `typeCode` and resets detail page to 1.
- Server filters ayahs by lemma + optional `typeCode` before paging.
- Highlighting marks only words matching both selected lemma and selected type; with `عرض الكل`, highlight remains lemma-wide.

## 15. Backend implementation plan, file by file

### `Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs`

- Extend `GetAyahs` with `[FromQuery] string? typeCode`.
- Pass `typeCode` into `GetLemmaAyahsQuery`.
- Keep validation and response envelope unchanged.

### `Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/GetLemmaAyahs/GetLemmaAyahsQuery.cs`

- Add `string? TypeCode` to the query record.
- Preserve existing id/page/pageSize semantics.

### `Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/GetLemmaAyahs/GetLemmaAyahsHandler.cs`

- Keep id and paging validation unchanged.
- Normalize empty/whitespace `typeCode` to `null` before calling the reader, or do that in controller/query construction.
- Pass `typeCode` through to reader without extra domain validation.

### `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/ILemmasReader.cs`

- Extend `GetLemmaAyahMatchesAsync` with `string? typeCode`.

### `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Lemmas/LemmasCacheKeys.cs`

- Add `typeCode` to `Ayahs(...)` cache key.
- Normalize `null`/empty to a stable marker such as `all`.
- Keep page and pageSize in the key so pagination remains collision-free.

### `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Lemmas/CachedLemmasReader.cs`

- Thread `typeCode` through the ayah cache lookup and cache write path.
- Use the updated cache key so filtered and unfiltered requests never collide.
- Keep all other caches unchanged.

### `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs`

- Filter matched ayahs by `LemmaId == id` and optional `HeadPos == typeCode`.
- Use the filtered match set for total count, page selection, and `IsMatched` computation.
- Keep `Words` collection on the page unfiltered except for existing ayah-marker exclusion.
- Preserve `ResolveAyahPageNumber` behavior.
- If `typeCode` matches nothing, return a safe empty page with `TotalCount = 0`.

### Backend tests under `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/`

- Extend `LemmasAyahsReadTests.cs` with filtered and unfiltered coverage.
- Add cache-key coverage through the repeated-read path.
- Keep marker exclusion assertions intact.
- Keep `IsMatched` assertions focused on selected-type occurrences only.

## 16. Frontend implementation plan, file by file

### `Frontend/quran-dashboard-ui/src/app/features/words/models/lemmas.models.ts`

- Add `ayahTypeCode: string | null` to `LemmasPanelState`.
- Add `typeCode` to `LEMMAS_QUERY_KEYS`.
- Add `typeCode` to `LEMMAS_SELECTION_QUERY_KEYS` so selection/view changes clear it.
- Add `typeCode` to `ParsedLemmasQuery` and `LemmasQueryChange`.

### `Frontend/quran-dashboard-ui/src/app/features/words/models/lemmas.labels.ts`

- Add labels needed by the new Lemmas-only filter UI, at minimum `عرض الكل` and any section/ARIA label the new component needs.
- Keep existing labels unchanged unless the new component needs a dedicated string.

### `Frontend/quran-dashboard-ui/src/app/features/words/data-access/lemmas.api.ts`

- Add optional `typeCode?: string | null` to `getLemmaAyahMatches`.
- Send `typeCode` as query param only when present.

### `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-cache.ts`

- Add `typeCode` to the ayah cache key so filtered and unfiltered ayah pages stay isolated.

### `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail.facade.ts`

- Store selected ayah `typeCode` in panel state.
- Add a setter for type selection that resets `detailPage` to 1 and reloads Ayahs.
- Keep `typeCode` only for `view === 'ayahs'`.
- Clear `typeCode` when selection is cleared or the view changes away from Ayahs.
- When restoring from URL, keep the selected `typeCode` until summary validation says it is stale.

### `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail-view.loader.ts`

- Pass `typeCode` into `getLemmaAyahMatches`.
- Include `typeCode` in the load context so cached ayah pages are keyed correctly.

### `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail-panel.updates.ts`

- No structural change expected unless a helper is needed for filter reset; keep the filter state management in the facade if possible.

### `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-url-sync.ts`

- Parse and build `typeCode`.
- Omit `typeCode` for `عرض الكل`.
- Include `typeCode` in the selection query key set.
- Keep search/sort/catalogue query behavior unchanged.

### `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.ts`

- Add page handlers for type filter changes.
- Update URL with `typeCode` and `detailPage: 1` when a type changes.
- Clear `typeCode` when leaving Ayahs, changing selection, or clearing selection.
- After summary loads, normalize stale `typeCode` by comparing it against `summary.typeDistribution`; if absent, remove the query param and return to `عرض الكل`.

### `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html`

- Remove `qd-type-distribution-list` from always-visible panel chrome.
- Render the new Lemmas-only compact filter component only inside the Ayahs branch, above `qd-ayah-matches-list`.
- Keep other tabs untouched.

### New Lemmas-only compact type filter component

- Add a dedicated component under `features/words/components/` for Lemmas ayah type filters.
- Input: summary `typeDistribution`, current `ayahTypeCode`, loading state.
- Output: selected `typeCode | null`.
- Render a grid of buttons/cards, not the shared vertical `qd-type-distribution-list`.
- Use local SCSS to achieve the responsive 4/3/2/1 layout.
- Do not make this a shared component; that avoids Stems side effects.

### `Frontend/quran-dashboard-ui/src/app/features/words/utils/lemma-ayah-match.mapper.ts`

- No change expected; keep mapping to the shared ayah list shape.

## 17. URL/state behavior

- All filter: no `typeCode` param.
- Selected type: `typeCode=NEG`.
- `typeCode` is only meaningful for `view=ayahs`.
- Clear `typeCode` when:
  - leaving Ayahs,
  - clearing selected lemma,
  - resetting selection,
  - summary/typeDistribution does not contain the current code.
- Preserve existing search, sort, page, and detail selection behavior.
- Reset detail page to `1` whenever `typeCode` changes.

## 18. Invalid/stale typeCode behavior

- Backend should not crash on unknown or stale `typeCode`.
- Preferred backend behavior: treat unknown code as a valid-but-empty filter and return an empty page safely.
- Empty or whitespace `typeCode` should behave like `عرض الكل`.
- Frontend should normalize stale codes after summary loads by checking `summary.typeDistribution` and removing `typeCode` from URL when absent.
- If the URL contains a code that later proves stale, the UI should recover to `عرض الكل` without surfacing an error.

## 19. Test plan

### Backend tests

- `LemmasAyahsReadTests.cs`: no `typeCode` returns all lemma ayah matches.
- `LemmasAyahsReadTests.cs`: `typeCode` filters ayahs server-side.
- `LemmasAyahsReadTests.cs`: pagination still works after filtering.
- `LemmasAyahsReadTests.cs`: ayah markers remain excluded.
- `LemmasAyahsReadTests.cs`: `IsMatched` is true only for selected-type occurrences.
- `LemmasAyahsReadTests.cs`: cache path includes `typeCode` and does not collide with all-filter reads.
- `LemmasAyahsReadTests.cs`: unknown `typeCode` follows the chosen safe behavior.

### Frontend tests

- New Lemmas-only compact filter spec: filters render only in Ayahs.
- New Lemmas-only compact filter spec: `عرض الكل` is selected by default.
- New Lemmas-only compact filter spec: selecting a type emits `typeCode`.
- New Lemmas-only compact filter spec: selecting `عرض الكل` clears `typeCode`.
- `lemmas-explorer-page.component.spec.ts`: clicking a type updates URL with `typeCode` and resets `detailPage` to `1`.
- `lemmas-explorer-page.component.spec.ts`: stale `typeCode` is cleared after summary loads.
- `lemmas-explorer-page.component.spec.ts`: ayah list still receives mapped shared ayah shape.
- `lemmas-explorer-page.component.spec.ts`: the global always-visible type distribution is no longer rendered in panel chrome.
- Keep Stems page/spec untouched unless a compile-time shared dependency forces a minimal adjustment.

## 20. Verification commands

### Backend

```bash
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter "FullyQualifiedName~WordsMorphologyExplorers.LemmasAyahsReadTests"
dotnet build Backend/QuranDashboard.sln
```

### Frontend

```bash
cd Frontend/quran-dashboard-ui
npm test -- lemmas-explorer-page.component
npm test -- lemma-ayah-type-filters.component
npm run build
```

- Keep the existing Vitest worker cap if the project guidance requires it.

## 21. Risks and edge cases

- Lemma with no type distribution: show only `عرض الكل`.
- Count meaning: keep occurrence counts, not ayah counts.
- Multiple matching occurrences in one ayah: all matching words should highlight.
- Same ayah containing the lemma with different types: selected type should isolate matching highlights.
- Pagination and server-side filtering: page must reset to `1` on type change.
- Stale or invalid `typeCode`: backend safe-empty, frontend normalizes to all.
- Shared component side effects: avoid by using a Lemmas-only filter component.
- Future Stems reuse: defer to a separate follow-up once Lemmas is stable.

## 22. Proposed phases

### Phase 1: Backend typeCode query/filter/cache/tests

- Thread `typeCode` through controller, query, handler, reader, and cache.
- Add backend test coverage for filtering, paging, marker exclusion, and cache isolation.

### Phase 2: Frontend state/API/URL wiring

- Add `typeCode` to models, URL sync, facade state, loader, and API method.
- Add stale-code normalization after summary load.

### Phase 3: Lemmas compact type filter UI inside Ayahs tab

- Add Lemmas-only compact filter component.
- Render it only in Ayahs.
- Remove always-visible type distribution from panel chrome.

### Phase 4: Frontend tests and verification

- Add focused component/page/facade tests.
- Run focused frontend tests and build.

### Phase 5: Review cleanup

- Confirm no shared Stems surface changed.
- Confirm filter state, URL, and highlight behavior stay aligned.
