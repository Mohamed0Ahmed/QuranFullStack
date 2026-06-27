# Unique Words List Columns And Response Cleanup Plan

## 1. Verdict

- First slice: Unique Words only.
- Add visible columns `نوع الكلمة` and `الجذر` to `UniqueWordsTableComponent`.
- Simplify Unique Words response contracts and backend projections together; removed fields must not be selected/materialized unless needed for filtering, sorting, identity, or derivation.
- Root values can link to existing Roots Explorer. Word-type values stay non-clickable until a word-type route exists.
- No Spec Kit, migrations, importers, Quran data mutation, route changes, or roots/lemmas/stems explorer changes.

## 2. Backend DTO Changes

Keep `ApiResponse<T>` and `PagedResult<T>` envelopes unchanged.

Change response DTOs under `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/`:

| DTO | Planned shape |
| --- | --- |
| `UniqueWordListItemDto` | `Id`, `Kind`, `DisplayText`, `OccurrencesCount`, `AyahsCount`, `SurahsCount`, `MissingSurahsCount`, `PrimaryWordTypeCode`, `PrimaryWordTypeArabicLabel`, `RootId`, `RootText` |
| `UniqueWordSummaryDto` | `Id`, `Kind`, `DisplayText`, `OccurrencesCount`, `AyahsCount`, `SurahsCount`, `MissingSurahsCount` |
| `UniqueWordSurahsResponse` | `Surahs` only |
| `UniqueWordMissingSurahsResponse` | `Surahs` only |
| `UniqueWordAyahMatchDto` | `AyahId`, `VerseKey`, `SurahNameArabic`, `AyahNumber`, `PageNumber`, `MatchedQuranWordIds`, `Words` |
| `AyahWordForHighlightDto` | `QuranWordId`, `TextUthmani`, `IsAyahMarker` |

Remove from list DTO:

- `DisplayTextUthmani`
- `TextUthmani`
- `TextUthmaniSimple`
- `TextImlaeiSimple`
- `WordKeyImlaeiSimple`
- `QpcGlyph`
- `FirstVerseKey`
- `FirstLocation`

Remove from summary DTO:

- `DisplayTextUthmani`
- `TextUthmani`
- `TextUthmaniSimple`
- `TextImlaeiSimple`
- `WordKeyImlaeiSimple`
- `QpcGlyph`
- `FirstVerseKey`
- `FirstLocation`

## 3. Backend Projection Changes

File: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs`.

List projection:

- Compute `DisplayText` in SQL/query layer.
- For `tashkeel`: `DisplayText = text_uthmani`.
- For `simple`: `DisplayText = text_uthmani_simple`.
- Keep search columns only in SQL `WHERE` or internal `SearchText` if required for alpha sorting; do not return removed text fields in `UniqueWordListItemDto`.
- Keep `FirstWordOrderInMushaf` internal for ordering only; do not return `FirstVerseKey` or `FirstLocation`.
- Add morphology enrichment by unique word id:
  - Join matching `quran_words` rows by `UniqueTashkeelWordId` or `UniqueSimpleWordId`.
  - Join `quran_word_morphology` on `quran_word_id`.
  - Join `pos_tags` on `head_pos = code` for `PrimaryWordTypeCode` and `PrimaryWordTypeArabicLabel`.
  - Join `quran_roots` on `root_id` for `RootId` and `RootText`.
- Primary type rule: group by POS code, order `COUNT(*) DESC`, earliest `quran_word_id ASC`, `code ASC`; take one.
- Primary root rule: group by root id/text, order `COUNT(*) DESC`, earliest `quran_word_id ASC`, `root_id ASC`; take one.
- Null handling: if no type/root data, return `null` for related fields; frontend renders `—`.

Summary projection:

- Select only fields needed for planned summary response: id, kind, display text, counts.
- Do not select `word_key_imlaei_simple`, `qpc_glyph`, first verse/location, or alternate text fields unless needed to compute `DisplayText`.

Surahs/missing projections:

- Keep existence check/header lookup minimal, but response should materialize only `Surahs`.
- Remove response echo fields from DTO construction.
- Count fields can stay as local variables only if needed for logging or status, not response payload.

Ayah matches projection:

- Stop returning `SurahNumber` in `UniqueWordAyahMatchDto`; keep it only for query ordering if needed.
- Stop returning `WordNumber` in `AyahWordForHighlightDto`; keep it only for query ordering.
- Continue selecting `quran_word_id`, `text_uthmani`, `is_ayah_marker`, `verse_key`, `surah_name`, `ayah_number`, `page_number`, and matched ids.

Caching:

- `CachedUniqueWordsReader` type signatures remain same generic DTO names; no cache-key plan change.

## 4. Frontend Model Changes

File: `Frontend/quran-dashboard-ui/src/app/features/words/models/unique-words.models.ts`.

Update interfaces to match backend:

- `UniqueWordListItemDto.displayText: string`.
- `primaryWordTypeCode: string | null`.
- `primaryWordTypeArabicLabel: string | null`.
- `rootId: number | null`.
- `rootText: string | null`.
- Remove old text/glyph/first-location fields from list and summary models.
- Remove `id/kind/displayTextUthmani/surahsCount` from `UniqueWordSurahsDto`.
- Remove `id/kind/displayTextUthmani/missingSurahsCount` from `UniqueWordMissingSurahsDto`.
- Remove `surahNumber` from `AyahMatchDto`.
- Remove `wordNumber` from `AyahWordForHighlightDto`.

State/helpers:

- Simplify `UniqueWordListItemViewModel`; it can extend DTO without a mapper-added `displayText`.
- Update `mapUniqueWordListItem()` / `mapUniqueWordListItems()` to pass through `displayText`, or remove mapper if call sites stay simple.
- Update `toUniqueWordSummary()` to copy only planned summary fields.
- Update `mapUniqueWordSummaryDisplayText()` to use `displayText` directly.

## 5. Frontend Table/Template Changes

Files:

- `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-table/unique-words-table.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-table/unique-words-table.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-table/unique-words-table.component.scss`
- `Frontend/quran-dashboard-ui/src/app/features/words/models/unique-words.labels.ts`

Plan:

- Add labels `نوع الكلمة`, `الجذر`, and placeholder `—`.
- Add two desktop columns between `الكلمة` and count columns.
- Render `primaryWordTypeArabicLabel ?? '—'`.
- Render `rootText ?? '—'`.
- Do not render `+N` counters for word type.
- Add matching skeleton cells in loading rows.
- Mobile: put type/root as quiet metadata under/near word text; keep existing count badges.
- Adjust grid from current `row + word + 4 counts` to `row + word + type + root + 4 counts`; preserve table scroll/virtual rows.

## 6. Link Behavior

Root link:

- Use existing `buildRootsDeepLink({ rootId, view: 'words', wordView: 'simple' })` + `deepLinkToHref()`.
- Render anchor only when `rootId !== null && rootText`.
- Anchor attrs: `target="_blank"`, `rel="noopener noreferrer"`.
- Use visible label `rootText`; never use root text for lookup.

Word type link:

- No existing word-type route found in `words.routes.ts` or `route-paths.ts`.
- Keep type non-clickable for now; do not link to roots/lemmas/stems as a substitute.
- Keep `primaryWordTypeCode` in row data and expose only as non-visible link data/future identity, e.g. data attribute or tested model field.
- Do not invent `types` route in this slice.

## 7. Tests To Update

Backend:

- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsListReadTests.cs`
  - Assert simplified list shape through compile-time DTO usage.
  - Assert `DisplayText` values for `tashkeel` and `simple` modes.
  - Assert `PrimaryWordTypeCode`, `PrimaryWordTypeArabicLabel`, `RootId`, `RootText` for fixture word with morphology.
  - Assert removed list fields no longer referenced.
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordSummaryTests.cs`
  - Assert simplified summary shape and `DisplayText`.
  - Remove assertions for glyph/alternate text/first location.
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordSurahDrilldownTests.cs`
  - Assert `Surahs` payload only where practical; remove echo/count assertions.
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordAyahMatchesTests.cs`
  - Remove `SurahNumber`/`WordNumber` expectations; keep page/deep-link/highlight fields.
- `Backend/tests/QuranDashboard.Tests/Quran/Words/CachedUniqueWordsReaderTests.cs`
  - Update DTO constructors/fixtures only if compile requires.

Frontend:

- `Frontend/quran-dashboard-ui/src/app/features/words/data-access/unique-words.api.spec.ts`
  - Update sample response shapes and endpoint assertions.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.list.spec.ts`
  - Update fixtures; assert `displayText` pass-through.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.restore.spec.ts`
  - Update summary fixtures.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.drilldown.spec.ts`
  - Update surahs/missing/ayahs fixtures.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-table/unique-words-table.component.spec.ts`
  - Assert new headers/cells, root link attrs, type non-clickable, placeholder `—`, no `+N` type counter.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/ayah-matches-list/ayah-matches-list.component.spec.ts`
  - Remove `wordNumber` and `surahNumber` fixture fields.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/highlighted-ayah/highlighted-ayah.component.spec.ts`
  - Remove `wordNumber` fixture fields.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-drilldown-modal/word-drilldown-modal.component.spec.ts`
  - Update summary/surahs/missing/ayahs fixtures.

## 8. Build/Test Commands

Backend targeted:

```bash
dotnet test Backend/QuranDashboard.sln --filter "FullyQualifiedName~UniqueWordsListReadTests|FullyQualifiedName~UniqueWordSummaryTests|FullyQualifiedName~UniqueWordSurahDrilldownTests|FullyQualifiedName~UniqueWordAyahMatchesTests|FullyQualifiedName~CachedUniqueWordsReaderTests"
```

Backend build:

```bash
dotnet build Backend/QuranDashboard.sln
```

Frontend targeted:

```bash
npm test -- --watch=false --include src/app/features/words/data-access/unique-words.api.spec.ts --include src/app/features/words/state/unique-words.facade.list.spec.ts --include src/app/features/words/state/unique-words.facade.restore.spec.ts --include src/app/features/words/state/unique-words.facade.drilldown.spec.ts --include src/app/features/words/components/unique-words-table/unique-words-table.component.spec.ts --include src/app/features/words/components/ayah-matches-list/ayah-matches-list.component.spec.ts --include src/app/features/words/components/highlighted-ayah/highlighted-ayah.component.spec.ts --include src/app/features/words/components/word-drilldown-modal/word-drilldown-modal.component.spec.ts
```

Frontend build:

```bash
npm run build --prefix Frontend/quran-dashboard-ui
```

## 9. Likely Files Touched

Backend:

- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordListItemDto.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordSummaryDto.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordSurahsResponse.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordMissingSurahsResponse.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Responses/UniqueWordAyahMatchDto.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsListReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordSummaryTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordSurahDrilldownTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordAyahMatchesTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/CachedUniqueWordsReaderTests.cs`

Frontend:

- `Frontend/quran-dashboard-ui/src/app/features/words/models/unique-words.models.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/models/unique-words.labels.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/utils/unique-words-display.mapper.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/utils/unique-words-state.helpers.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-table/unique-words-table.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-table/unique-words-table.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-table/unique-words-table.component.scss`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/ayah-matches-list/ayah-matches-list.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/highlighted-ayah/highlighted-ayah.component.ts`
- Corresponding spec files listed in test section.

Report only:

- `report/feature-017-lexical-explorers-polish/unique-words-list-columns-and-response-cleanup-plan.md`

## 10. Risks / Checks

- Response cleanup is breaking for current frontend; backend and frontend must land together.
- `DisplayText` must preserve current mode behavior: `tashkeel` uses Uthmani display, `simple` uses simple display.
- Primary root/type collapse may hide multiple morphology values; product explicitly asked primary type only, so no counters.
- Root link can use existing route; word-type link cannot without route change, so keep non-clickable.
- Projection enrichment can make list query heavier; keep grouping scoped to page rows if possible, or verify acceptable query cost.
- Avoid accidental changes to roots/lemmas/stems explorer response models; shared `AyahWordForHighlightDto` under Unique Words responses may affect imports in those features if referenced.
