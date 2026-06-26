# Words Page Response Inventory And Column Plan

## 1. Verdict

- Unique Words table is fed by `GET /api/words/unique/{kind}` through `UniqueWordsController.Get` -> `GetUniqueWordsPageHandler` -> `IUniqueWordsReader.GetUniqueWordsPageAsync` -> `EfUniqueWordsReader.GetUniqueWordsPageAsync`.
- Current list response does **not** contain enough data for `نوع الكلمة` or `الجذر`.
- Add fields to `UniqueWordListItemDto` and frontend `UniqueWordListItemDto`; render only on Unique Words table.
- Do not add these columns to `الصيغ المعجمية` or `الأصول الصرفية` tables; their explorer pages already own that context.
- No migration, importer, or Quran data mutation expected. Existing read models have morphology source data: `quran_word_morphology.head_pos`, `pos_tags.arabic_label`, `quran_word_morphology.root_id`, `quran_roots.root_text`.

## 2. Current data flow

- Route: `/dashboard/words/unique/:mode` from `words.routes.ts` into `UniqueWordsPageComponent`.
- URL/list state: `UniqueWordsPageComponent` updates query params; `UniqueWordsFacade.bindToRoute()` reads `mode`, `search`, `sort`, `page`, `word`, `view`, `ap`.
- List load: `UniqueWordsFacade.runListRequest()` calls `UniqueWordsApi.getList()` and maps rows via `mapUniqueWordListItems()`.
- Table render: `UniqueWordsTableComponent` receives `rows`, `loading`, `selectedWordId`, `currentPage`, `pageSize`.
- Row open: table emits `rowSelected` or count-chip drilldown; page opens `UniqueWordsDrilldownFacade` and writes `word/view/ap` query params.
- URL restore/back-forward: `restoreFromUrl()` loads summary with `UniqueWordsApi.getSummary()` when `word` query param exists, then loads selected view endpoint.
- Details panel: `WordDrilldownModalComponent` renders mentioned surahs, missing surahs, or ayah matches based on `WordDrilldownState.view`.
- Caching: `UniqueWordsCacheKeys.list/summary/surahs/missing/ayahs` caches `ApiResponse<T>` by mode/id/page.

## 3. Add columns plan

| Need | Plan |
| --- | --- |
| Backend endpoint | Extend `GET /api/words/unique/{kind}` list item only. |
| Backend DTO | Add nullable display fields to `UniqueWordListItemDto`, e.g. `PrimaryWordTypeArabicLabel` and `RootText`. |
| Backend projection | In `EfUniqueWordsReader`, derive per unique word from `quran_words` joined to `quran_word_morphology`; join `pos_tags` for Arabic type label and `quran_roots` for root text. |
| Type selection | Primary type only: group by POS for unique word, order by count desc, then earliest Quran word id/Mushaf order, then code. No `+1`/`+4` counters. |
| Root selection | Group by root for unique word, order by count desc, then earliest Quran word id/Mushaf order, then root id. Display root text only. |
| Frontend model | Add same optional fields to `Frontend/.../models/unique-words.models.ts` `UniqueWordListItemDto`; view model inherits through `UniqueWordListItemViewModel`. |
| Frontend render | Update `UniqueWordsTableComponent` template and SCSS only. Add headers `نوع الكلمة` and `الجذر`; add body cells in virtual and non-virtual rows; add compact mobile badges/metadata if needed. |
| Placeholder | Use `—`; current UI uses quiet empty/error states and badges, not prose placeholders like `لم يذكر` inside dense table cells. |
| Responsive width | Current desktop grid is `row number + word + 4 count columns`; adding 2 text columns requires narrower count columns or horizontal/table overflow. Mobile already hides chip cells and uses stat badges; keep root/type as compact secondary metadata near word or in mobile stats. |

Recommended labels:

- `نوع الكلمة`
- `الجذر`

No-go for this slice:

- No `الصيغ المعجمية` column.
- No `الأصول الصرفية` column.
- No morphology counters in Unique Words table.

## 4. Response inventory

All endpoints return `ApiResponse<T>` envelope: `isSuccess`, `message`, `data`, `errors`.

### List data

| Item | Value |
| --- | --- |
| Endpoint | `GET /api/words/unique/{kind}?search=&sort=&page=&pageSize=` |
| Controller/handler/DTO | `UniqueWordsController.Get` / `GetUniqueWordsPageHandler` / `PagedResult<UniqueWordListItemDto>` |
| Frontend call | `UniqueWordsApi.getList(kind, search, sort, page, pageSize)` |
| Consumers | `UniqueWordsFacade`, `UniqueWordsPageComponent`, `UniqueWordsTableComponent`, `PaginationComponent`, `toUniqueWordSummary()` for row-open summary seed |

| Field | Current usage |
| --- | --- |
| `PagedResult.page` | Internal state: loaded page; ayah-style pagination pattern; list page comes from URL. Keep. |
| `PagedResult.pageSize` | State/pagination/table row numbering. Keep. |
| `PagedResult.totalCount` | Visible via pagination and empty/success status. Keep. |
| `PagedResult.items` | Table rows. Keep. |
| `id` | Row identity, selection, cache keys, URL `word`, drilldown calls. Do not remove. |
| `kind` | API mode identity, row-open drilldown, summary seed. Do not remove. |
| `displayTextUthmani` | Visible fallback/display mapping, panel title, tests. Keep. |
| `textUthmani` | Visible in `tashkeel` display mapping when present; backend always returns it. Keep. |
| `textUthmaniSimple` | Visible in `simple` display mapping fallback. Keep. |
| `textImlaeiSimple` | Display fallback for simple mode; search source on backend. Maybe remove from frontend response only after confirming no fallback need. |
| `wordKeyImlaeiSimple` | Not visible in current UI; backend tests assert simple/tashkeel contract. Unknown. |
| `qpcGlyph` | Not visible in Unique Words UI; backend tests assert simple/tashkeel contract. Unknown. |
| `occurrencesCount` | Visible count column/mobile stat; sort option uses backend count. Keep. |
| `ayahsCount` | Visible count chip, disabled state, drilldown affordance. Keep. |
| `surahsCount` | Visible count chip, disabled state, drilldown affordance. Keep. |
| `missingSurahsCount` | Visible count chip/mobile stat; summary seed. Keep. |
| `firstVerseKey` | Summary seed and backend tests; not visible in current list. Unknown. |
| `firstLocation` | Summary seed and backend tests; not visible in current list. Unknown. |

### Summary / route restore

| Item | Value |
| --- | --- |
| Endpoint | `GET /api/words/unique/{kind}/{id}` |
| Controller/handler/DTO | `UniqueWordsController.GetSummary` / `GetUniqueWordSummaryHandler` / `UniqueWordSummaryDto` |
| Frontend call | `UniqueWordsApi.getSummary(kind, id)` |
| Consumers | `UniqueWordsDrilldownFacade.loadSummaryAndRestore()`, `WordDrilldownModalComponent` title, URL restore/back-forward not-found/error states |

| Field | Current usage |
| --- | --- |
| `id` | URL restore identity and detail calls. Do not remove. |
| `kind` | Detail endpoint mode after restore. Do not remove. |
| `displayTextUthmani` | Panel title fallback/display. Keep. |
| `textUthmani` | Panel title in `tashkeel` mode. Keep. |
| `textUthmaniSimple` | Panel title in `simple` mode. Keep. |
| `textImlaeiSimple` | Display fallback. Unknown. |
| `wordKeyImlaeiSimple` | Not visible; backend tests assert. Unknown. |
| `qpcGlyph` | Not visible; backend tests assert. Unknown. |
| `occurrencesCount` | Not visible in modal header today; logging/tests. Maybe remove after confirmation. |
| `ayahsCount` | Not visible after restore except state seed compatibility; maybe future disabled state. Unknown. |
| `surahsCount` | Not visible after restore except state seed compatibility; maybe future disabled state. Unknown. |
| `missingSurahsCount` | Not visible after restore except state seed compatibility; logging/tests. Unknown. |
| `firstVerseKey` | Not visible in current details. Unknown. |
| `firstLocation` | Not visible in current details. Unknown. |

### Mentioned surahs

| Item | Value |
| --- | --- |
| Endpoint | `GET /api/words/unique/{kind}/{id}/surahs` |
| Controller/handler/DTO | `UniqueWordsController.GetSurahs` / `GetUniqueWordSurahsHandler` / `UniqueWordSurahsResponse` |
| Frontend call | `UniqueWordsApi.getMentionedSurahs(kind, id)` |
| Consumers | `UniqueWordsDrilldownFacade`, `WordDrilldownModalComponent`, `SurahOccurrencesListComponent` |

| Field | Current usage |
| --- | --- |
| `id` | Stored in state/tests; not visible. Do not remove unless response identity convention changes globally. |
| `kind` | Stored in state/tests; not visible. Unknown. |
| `displayTextUthmani` | Stored in state/tests; modal title uses summary, not this response. Maybe remove after confirmation. |
| `surahsCount` | Stored/tests; UI uses `surahs.length` and list count chips use list response. Maybe remove after confirmation. |
| `surahs[].surahNumber` | Track key/order identity. Keep. |
| `surahs[].nameArabic` | Visible. Keep. |
| `surahs[].occurrencesInSurah` | Visible. Keep. |

### Missing surahs

| Item | Value |
| --- | --- |
| Endpoint | `GET /api/words/unique/{kind}/{id}/missing-surahs` |
| Controller/handler/DTO | `UniqueWordsController.GetMissingSurahs` / `GetUniqueWordMissingSurahsHandler` / `UniqueWordMissingSurahsResponse` |
| Frontend call | `UniqueWordsApi.getMissingSurahs(kind, id)` |
| Consumers | `UniqueWordsDrilldownFacade`, `WordDrilldownModalComponent`, `MissingSurahsListComponent` |

| Field | Current usage |
| --- | --- |
| `id` | Stored in state/tests; not visible. Do not remove unless response identity convention changes globally. |
| `kind` | Stored in state/tests; not visible. Unknown. |
| `displayTextUthmani` | Stored in state/tests; modal title uses summary, not this response. Maybe remove after confirmation. |
| `missingSurahsCount` | Stored/tests; UI uses `surahs.length` and list count chips use list response. Maybe remove after confirmation. |
| `surahs[].surahNumber` | Track key/order identity. Keep. |
| `surahs[].nameArabic` | Visible. Keep. |

### Ayah matches

| Item | Value |
| --- | --- |
| Endpoint | `GET /api/words/unique/{kind}/{id}/ayahs?page=&pageSize=` |
| Controller/handler/DTO | `UniqueWordsController.GetAyahs` / `GetUniqueWordAyahsHandler` / `PagedResult<UniqueWordAyahMatchDto>` |
| Frontend call | `UniqueWordsApi.getAyahMatches(kind, id, page, pageSize)` |
| Consumers | `UniqueWordsDrilldownFacade`, `WordDrilldownModalComponent`, `AyahMatchesListComponent`, `HighlightedAyahComponent`, `PaginationComponent`, Mushaf deep links |

| Field | Current usage |
| --- | --- |
| `PagedResult.page` | State `ayahPage`, URL `ap`, pagination. Do not remove. |
| `PagedResult.pageSize` | Row numbering and pagination. Keep. |
| `PagedResult.totalCount` | Empty/success state and pagination. Keep. |
| `PagedResult.items` | Visible ayah cards. Keep. |
| `ayahId` | Track key. Keep. |
| `verseKey` | Mushaf deep link `ayah/focusAyah`; tests. Do not remove. |
| `surahNumber` | Not visible in current ayah card; may support ordering/identity/tests. Unknown. |
| `surahNameArabic` | Visible. Keep. |
| `ayahNumber` | Visible. Keep. |
| `pageNumber` | Visible and Mushaf deep link target. Do not remove. |
| `matchedQuranWordIds` | Highlight matching words. Do not remove. |
| `words[].quranWordId` | Highlight identity/track key. Do not remove. |
| `words[].wordNumber` | Backend ordering/tests; not rendered directly. Unknown. |
| `words[].textUthmani` | Visible ayah text and aria labels. Keep. |
| `words[].isAyahMarker` | Filters marker out from visible text; tests. Keep. |

## 5. Candidate fields for cleanup

| Response | Field | Current frontend usage | Safe to remove? | Notes |
| --- | --- | --- | --- | --- |
| `UniqueWordListItemDto` | `wordKeyImlaeiSimple` | Not visible; typed/tested. | Unknown | Backend tests assert simple contract; confirm downstream consumers first. |
| `UniqueWordListItemDto` | `qpcGlyph` | Not visible; typed/tested. | Unknown | Could be reserved for glyph display. |
| `UniqueWordListItemDto` | `firstVerseKey` | Not visible; copied into summary seed. | Unknown | Do not remove until summary seed shape is reduced safely. |
| `UniqueWordListItemDto` | `firstLocation` | Not visible; copied into summary seed. | Unknown | Backend tests assert; may be useful for future linking. |
| `UniqueWordSummaryDto` | `wordKeyImlaeiSimple` | Not visible. | Unknown | Contract/tested; potential future display/linking. |
| `UniqueWordSummaryDto` | `qpcGlyph` | Not visible. | Unknown | Potential future glyph display. |
| `UniqueWordSummaryDto` | `occurrencesCount` | Not visible after URL restore; logged/tested. | Maybe remove after confirmation | Verify no planned modal header stats. |
| `UniqueWordSummaryDto` | `ayahsCount` | Not visible after URL restore. | Unknown | Could drive disabled/empty affordance later. |
| `UniqueWordSummaryDto` | `surahsCount` | Not visible after URL restore. | Unknown | Could drive disabled/empty affordance later. |
| `UniqueWordSummaryDto` | `missingSurahsCount` | Not visible after URL restore; logged/tested. | Unknown | Could drive disabled/empty affordance later. |
| `UniqueWordSummaryDto` | `firstVerseKey` | Not visible. | Unknown | Potential first-occurrence link. |
| `UniqueWordSummaryDto` | `firstLocation` | Not visible. | Unknown | Potential first-occurrence link. |
| `UniqueWordSurahsResponse` | `displayTextUthmani` | Not visible; summary owns title. | Maybe remove after confirmation | Keep if response identity convention requires header echo. |
| `UniqueWordSurahsResponse` | `surahsCount` | UI uses `surahs.length`; tests assert. | Maybe remove after confirmation | Count duplicates list length today. |
| `UniqueWordSurahsResponse` | `kind` | Not visible. | Unknown | Header echo/contract consistency. |
| `UniqueWordMissingSurahsResponse` | `displayTextUthmani` | Not visible; summary owns title. | Maybe remove after confirmation | Keep if response identity convention requires header echo. |
| `UniqueWordMissingSurahsResponse` | `missingSurahsCount` | UI uses `surahs.length`; tests assert. | Maybe remove after confirmation | Count duplicates list length today. |
| `UniqueWordMissingSurahsResponse` | `kind` | Not visible. | Unknown | Header echo/contract consistency. |
| `UniqueWordAyahMatchDto` | `surahNumber` | Not visible. | Unknown | Backend ordering/identity value; keep unless not needed by links/tests. |
| `AyahWordForHighlightDto` | `wordNumber` | Not visible. | Unknown | Backend tests assert ordering; may aid accessibility/debug later. |

Fields marked `Do not remove` by rule: all IDs used for URL/selection/loading/highlighting, counts visible or paging-related, `verseKey`, `pageNumber`, highlighted word text/marker data, pagination fields.

## 6. Recommended next implementation slice

Backend:

- Extend `UniqueWordListItemDto` with nullable `PrimaryWordTypeArabicLabel` and `RootText`.
- Extend `EfUniqueWordsReader` list projection row with those fields, derived from existing morphology tables.
- Keep endpoint shape/read-only behavior; no new endpoints.
- Add/adjust `UniqueWordsListReadTests` coverage for a known fixture word with root/type, plus null-root/type fallback if fixture has one.

Frontend:

- Extend `UniqueWordListItemDto` in `unique-words.models.ts`.
- Add table labels `نوع الكلمة` and `الجذر` in `unique-words.labels.ts` or local protected readonlys.
- Render cells in `UniqueWordsTableComponent` virtual and non-virtual paths; render `—` when missing.
- Add compact mobile metadata without duplicating count chips; avoid `+N` morphology counters.
- Update `unique-words-table.component.spec.ts`, `unique-words.facade.list.spec.ts`, and API model fixtures as needed.

Commands:

- Backend targeted: `dotnet test Backend/QuranDashboard.sln --filter "FullyQualifiedName~UniqueWordsListReadTests"`
- Backend build: `dotnet build Backend/QuranDashboard.sln`
- Frontend targeted: `npm test -- --watch=false --include src/app/features/words/components/unique-words-table/unique-words-table.component.spec.ts --include src/app/features/words/state/unique-words.facade.list.spec.ts --include src/app/features/words/data-access/unique-words.api.spec.ts`
- Frontend build: `npm run build --prefix Frontend/quran-dashboard-ui`

Expected no-go areas:

- No migrations.
- No importers.
- No Quran data mutation.
- No route changes.
- No broad redesign.
- No columns for `الصيغ المعجمية` or `الأصول الصرفية` in Unique Words table.

## 7. Risks / questions

- Primary root/type rule needs product acceptance: count-desc then first occurrence is consistent with morphology summary ordering, but still collapses multi-root/multi-POS unique words into one visible value.
- Adding two desktop columns may compress current count chips; SCSS grid must be adjusted carefully and checked on tablet width.
- Summary/surah/missing response cleanup should be separate follow-up after confirming whether header echo fields are part of API consistency policy.
- Current frontend tests use Arabic-looking synthetic strings; keep UI-only additions synthetic and avoid invented Quranic morphology in tests.
