# Lemma Ayah Type Filter Report

**Feature:** 017 Lexical Explorers Polish  
**Scope:** Lemmas Explorer only, route `/dashboard/words/lemmas`  
**Type:** inspection + report only

## 1. Verdict

**READY_FOR_PLAN**

Backend support is required, but current data already exposes the stable type identity needed for filtering. No blocking clarification is required.

## 2. Current implementation summary

### Backend

- `Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs` exposes `GET /api/words/lemmas/{id}/ayahs` with only `page` and `pageSize` query params.
- `Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/GetLemmaAyahs/GetLemmaAyahsQuery.cs` carries only `Id`, `Page`, and `PageSize`.
- `Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/GetLemmaAyahs/GetLemmaAyahsHandler.cs` validates id/paging, then calls `ILemmasReader.GetLemmaAyahMatchesAsync`.
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs` currently filters ayahs by lemma only, pages distinct ayahs, excludes ayah markers from `Words`, and computes `IsMatched` from lemma-only matched word ids.
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/Responses/LemmaSummaryDto.cs` already exposes `TypeDistribution`.
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Morphology/Responses/TypeSummaryDto.cs` already exposes stable `Code`, labels, and `OccurrencesCount`.
- `EfLemmasReader.LoadWholeSummaryAsync` derives the type distribution from `WordMorphology.HeadPos` joined to `PosTag.Code`, so the code is a controlled stable POS identity, not a display-only label.

### Frontend

- `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html` renders `qd-type-distribution-list` inside shared panel content, before the active detail view branch.
- That means type distribution is always visible anywhere the panel has a summary, not only on Ayahs.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/ayah-matches-list/ayah-matches-list.component.ts` already supports highlight via `matchedQuranWordIds`.
- `Frontend/quran-dashboard-ui/src/app/features/words/utils/lemma-ayah-match.mapper.ts` already maps Lemmas ayah DTOs into the shared ayah shape.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail.facade.ts` currently tracks view, subviews, and `detailPage`, but no type filter state.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-url-sync.ts` currently serializes `search`, `sort`, `page`, `lemma`, `view`, `wordView`, `surahView`, and `detailPage`, but no type filter.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/type-distribution-list/type-distribution-list.component.ts` is display-only and shared by Lemmas and Stems.
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.html` also uses the same shared `qd-type-distribution-list`, so changing that shared component would affect Stems.

### Current placement

- Type distribution is currently always-visible in the details panel chrome area, not scoped to Ayahs.
- The same shared display component is used in Stems, so any change to it has cross-page side effects.

## 3. UX target summary

- Move type distribution out of always-visible panel chrome.
- Show compact type filter chips only inside the Lemmas Ayahs tab.
- Default selected filter: `عرض الكل`.
- Each chip should show label + occurrence count, for example `حرف نفي — 1364 مرة`.
- Layout should be compact and responsive: 4 per row on wide desktop, 3 on smaller desktop/tablet if needed, 2 or 1 on mobile.
- Type filters must not appear outside Ayahs.

## 4. Feasibility analysis

### Backend support required

Yes. Frontend-only filtering is not safe because Ayah results are paginated. Filtering only the current page would hide valid matches on later pages and would not preserve correct `totalCount`.

### Stable filter key

- Use POS/type `Code`, not a numeric id.
- Reason: `TypeSummaryDto` already exposes `Code`, and the backend derives it from controlled `PosTag.Code` / `WordMorphology.HeadPos`.
- A numeric type id is not currently exposed in the summary payload.

### URL state recommendation

- Yes, store selected type in URL.
- Recommended param: `typeCode`.
- Why: explicit, unambiguous, and aligned with current query naming style (`wordView`, `surahView`, `detailPage`).
- Keep it only when `view=ayahs`; clear it when leaving Ayahs and when clearing selection.
- Use `null` omission for `عرض الكل`.

### Pagination behavior

- Clicking a type filter must reset detail pagination to page 1.
- Backend should page after filtering, not before.
- Empty result pages after filter should still be valid if the current page is out of range, but UI should normally reset to page 1 on selection change.

### Highlight behavior

- `IsMatched` can be computed for selected type only.
- Backend should filter matched rows by both lemma id and selected `typeCode` before building `matchedQuranWordIds`.
- No frontend highlight model change is required if the ayah DTO still carries `Words` plus `IsMatched`.

### Count semantics

- Show `OccurrencesCount` only.
- That is the only count already available on `TypeSummaryDto`, and it matches the user-facing `مرة` copy.
- Ayah count would need another aggregate and DTO change.

## 5. Recommended response/API shape changes

- Add optional query param to `GET /api/words/lemmas/{id}/ayahs`:
  - `typeCode=<POS_CODE>`
- Extend `GetLemmaAyahsQuery` with `TypeCode`.
- Extend `ILemmasReader.GetLemmaAyahMatchesAsync` with `typeCode`.
- Update `EfLemmasReader.GetLemmaAyahMatchesAsync` to filter both:
  - matched ayahs: lemma id + optional `HeadPos == typeCode`
  - matched words: same filter, so highlight follows selected type only
- Update `CachedLemmasReader` and `LemmasCacheKeys.Ayahs(...)` to include `typeCode` in cache key.
- No response DTO change is required for this UX.
- Keep `LemmaSummaryDto.TypeDistribution` and `TypeSummaryDto` unchanged.

## 6. Recommended frontend state/model changes

- Add `ayahTypeCode: string | null` to `LemmasPanelState` and the panel URL state.
- Add `typeCode` to `LEMMAS_QUERY_KEYS`, `LEMMAS_SELECTION_QUERY_KEYS`, and `ParsedLemmasQuery`.
- Update `lemmas-url-sync.ts` to parse/build `typeCode` and clear it when leaving Ayahs.
- Add a `setAyahTypeCode(...)` path in `LemmasDetailFacade` that:
  - stores selected type
  - resets `detailPage` to 1
  - reloads Ayahs
- Update `LemmasDetailViewLoader` to pass the filter through to the API and cache key.
- Render a new Lemmas-specific compact filter component inside the Ayahs branch of `lemmas-explorer-page.component.html`, above `qd-ayah-matches-list`.
- Do not reuse `qd-type-distribution-list` for this UI change. It is shared with Stems and its current DOM/layout is display-only, vertical, and not suitable for chips.
- Keep `mapLemmaAyahMatchToShared` unchanged.

## 7. Test impact

### Backend tests

- Add ayah filter-by-type coverage in `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/LemmasAyahsReadTests.cs`.
- Add default/all-filter coverage when `typeCode` is omitted.
- Keep paging coverage on filtered results, including positive out-of-range page behavior.
- Keep marker exclusion coverage intact.
- Add assertion that only selected-type occurrences are marked `IsMatched`.
- Consider cache coverage for `typeCode` variants so filtered and unfiltered pages do not collide.

### Frontend tests

- Add component/page coverage that compact filters render only in Ayahs.
- Assert `عرض الكل` is selected by default.
- Assert clicking a type updates URL state and resets page to 1.
- Assert Ayah highlighting still works with filtered results.
- Assert the global `qd-type-distribution-list` is no longer rendered in the always-visible panel chrome.
- Update `lemmas-explorer-page.component.spec.ts` and any new filter component spec accordingly.

## 8. Risks and edge cases

- Lemma with no type distribution: show only `عرض الكل`.
- Count meaning: occurrences count vs ayah count must stay explicit.
- Multiple matching occurrences in one ayah: all matching words should highlight.
- Same ayah containing same lemma with different types: filter must keep only selected type matches highlighted.
- Pagination and server-side filtering: page reset must happen on filter change.
- Invalid or stale `typeCode` in URL: normalize back to `عرض الكل` or another valid summary item once summary is loaded; do not leave the UI in a mismatched hidden-state condition.
- Future Stems reuse: keep this Lemmas-only first pass isolated so Stems stays untouched.

## 9. Out of scope

- Stems implementation
- Word Types Explorer
- Migrations
- Importers
- Quran data mutation
- Global component redesign

## 10. Proposed phased implementation plan

1. Backend first: add `typeCode` query support, reader filtering, cache key update, and focused tests.
2. Frontend model/state next: add URL key, panel state, facade setter, and loader wiring.
3. Frontend UI next: introduce Lemmas-specific compact filter chips and place them only in the Ayahs branch.
4. Frontend tests last: update page/spec coverage for URL state, page reset, and highlight behavior.
5. Verify with focused backend and frontend tests, then full builds if needed.
