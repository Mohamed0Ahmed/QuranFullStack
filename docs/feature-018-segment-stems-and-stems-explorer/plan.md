# Stems Explorer Response Cleanup Plan

## Executive Summary
- Fix Stems Words tab bug: `بدون تشكيل` currently shows tashkeel because simple branch still projects `TextUthmani`.
- Rename `displayTextUthmani` to `displayText` in Stems word DTOs and frontend models.
- Trim Stems response contracts to target shapes; keep `ApiResponse<T>` and `PagedResult<T>` unchanged.
- Treat list/summary/type distribution cleanup and ayah payload cleanup as coordinated backend + frontend work.

## Final Target Contracts

### Common
- `ApiResponse<T>` stays unchanged.
- `PagedResult<T>` stays unchanged.
- `TypeSummaryDto` target shape:
```json
{ "code": string, "arabicLabel": string, "occurrencesCount": number }
```
- Remove from `TypeSummaryDto`: `englishLabel`, `firstSurahNumber`, `firstAyahNumber`, `firstWordNumber`.

### 1. `GET /api/words/stems`
Target item:
```json
{
  "id": number,
  "stemText": string,
  "lemmaId": number | null,
  "lemmaText": string | null,
  "rootId": number | null,
  "rootText": string | null,
  "occurrencesCount": number,
  "ayahsCount": number,
  "surahsCount": number,
  "simpleWordsCount": number,
  "tashkeelWordsCount": number
}
```
- Remove: `lemmaBuckwalter`, `rootBuckwalter`, `dominantType`, `otherTypesCount`, `firstVerseKey`.

### 2. `GET /api/words/stems/{id}`
Target:
```json
{
  "id": number,
  "stemText": string,
  "lemmaId": number | null,
  "lemmaText": string | null,
  "rootId": number | null,
  "rootText": string | null,
  "occurrencesCount": number,
  "ayahsCount": number,
  "surahsCount": number,
  "simpleWordsCount": number,
  "tashkeelWordsCount": number,
  "typeDistribution": [
    { "code": string, "arabicLabel": string, "occurrencesCount": number }
  ]
}
```
- Remove: `lemmaBuckwalter`, `rootBuckwalter`, `dominantType`, `otherTypesCount`, `firstVerseKey`.
- Remove from `typeDistribution`: `englishLabel`, `firstSurahNumber`, `firstAyahNumber`, `firstWordNumber`.

### 3. `GET /api/words/stems/{id}/words/{wordKind}`
Target item:
```json
{ "uniqueWordId": number, "displayText": string, "occurrencesCount": number }
```
- Remove: `kind`, `firstVerseKey`, old `displayTextUthmani` name.
- Behavior:
  - `wordKind=simple` => `displayText = TextUthmaniSimple`
  - `wordKind=tashkeel` => `displayText = TextUthmani`

### 4. `GET /api/words/stems/{id}/ayahs`
Target item:
```json
{
  "ayahId": number,
  "verseKey": string,
  "surahNameArabic": string,
  "pageNumber": number,
  "words": [
    { "textUthmani": string, "isMatched": boolean }
  ]
}
```
- Remove: `surahNumber`, `ayahNumber`, `matchedQuranWordIds`, `words[].quranWordId`, `words[].isAyahMarker`.
- Behavior:
  - Backend computes `isMatched` per word.
  - Backend excludes ayah marker rows from `words[]`.
  - Frontend derives `surahNumber` and `ayahNumber` from `verseKey` using shared helper.
  - Keep `pageNumber` for Mushaf navigation.

### 5. `GET /api/words/stems/{id}/surahs`
Target:
```json
{ "surahs": [ { "surahNumber": number, "nameArabic": string, "occurrencesInSurah": number } ] }
```
- Remove wrapper metadata: `id`, `stemText`, `surahsCount`.

### 6. `GET /api/words/stems/{id}/missing-surahs`
Target:
```json
{ "surahs": [ { "surahNumber": number, "nameArabic": string } ] }
```
- Remove wrapper metadata: `id`, `stemText`, `missingSurahsCount`.

### 7. `GET /api/words/stems/{id}/lemmas`
Target:
```json
{ "lemmas": [ { "lemmaId": number, "lemmaText": string, "occurrencesCount": number } ] }
```
- Remove: `id`, `stemText`, `lemmasCount`, `lemmaBuckwalter`.

## Phase A - Backend Words Bug + DTO Rename

### Goal
- Fix `بدون تشكيل` display bug.
- Rename `displayTextUthmani` to `displayText`.
- Remove `kind` and `firstVerseKey` from `StemWordItemDto`.

### Files likely involved
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Stems/Responses/StemWordItemDto.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/StemsWordsReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/StemsListReadTests.cs` if the DTO rename touches shared fixtures indirectly

### Exact DTO/model changes
- `StemWordItemDto`: `{ uniqueWordId, displayText, occurrencesCount }`
- Simple branch projection: `w.TextUthmaniSimple`
- Tashkeel branch projection: `w.TextUthmani`

### Test changes
- Add/update test proving simple mode returns simple display text and tashkeel mode returns tashkeel text.
- Keep count / paging assertions intact.

### Commands
- `dotnet test Backend/QuranDashboard.sln --filter "FullyQualifiedName~StemsWordsReadTests"`
- `dotnet build Backend/QuranDashboard.sln`

### Risk
- Low.

### Rollback risk
- Low.

### Coordination
- Backend can land first, but frontend rename must follow before final merge.

## Phase B - Backend Response DTO Cleanup

### Goal
- Apply target shapes for list, summary, ayahs, surahs, missing-surahs, lemmas, and `TypeSummaryDto`.

### Files likely involved
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Stems/Responses/StemListItemDto.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Stems/Responses/StemSummaryDto.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Stems/Responses/StemAyahMatchDto.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Stems/Responses/StemSurahsResponse.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Stems/Responses/StemMissingSurahsResponse.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Stems/Responses/StemLemmasResponse.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Morphology/Responses/TypeSummaryDto.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.Summary.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/StemsListDerivation.cs`

### Exact DTO/model changes
- Remove list/summary fields: `lemmaBuckwalter`, `rootBuckwalter`, `dominantType`, `otherTypesCount`, `firstVerseKey`.
- Shrink `TypeSummaryDto` to three fields.
- Ayahs: remove `surahNumber`, `ayahNumber`, `matchedQuranWordIds`; words remove `quranWordId`, `isAyahMarker`; backend computes `isMatched` and excludes markers.
- Surahs / missing-surahs / lemmas wrappers become array-only envelopes.

### Test changes
- `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/StemsListReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/StemsAyahsReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/StemsSurahsReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyRelationshipsReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersLoggingTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersCacheReadTests.cs`

### Commands
- `dotnet test Backend/QuranDashboard.sln --filter "FullyQualifiedName~StemsListReadTests|FullyQualifiedName~StemsAyahsReadTests|FullyQualifiedName~StemsSurahsReadTests|FullyQualifiedName~MorphologyRelationshipsReadTests"`
- `dotnet build Backend/QuranDashboard.sln`

### Risk
- Medium-high.

### Rollback risk
- Medium.

### Coordination
- Backend and frontend must be coordinated in same branch/PR because this is a breaking API contract change.

## Phase C - Frontend Model, API, State, Component Updates

### Goal
- Match frontend models and consumers to new backend shapes.
- Keep visible behavior unchanged except the bug fix.

### Files likely involved
- `Frontend/quran-dashboard-ui/src/app/features/words/models/stems.models.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/data-access/stems.api.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-explorer.facade.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail.facade.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail-view.loader.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail-panel.updates.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-words-list/stem-words-list.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-words-list/stem-words-list.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-ayah-type-filters/stem-ayah-type-filters.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-ayah-type-filters/stem-ayah-type-filters.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/stems-table/stems-table.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/stems-table/stems-table.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/utils/verse-key.ts`
- Any Stems-only ayah presenter/mapper file if needed for `verseKey`-based derivation

### Exact DTO/model changes
- `StemListItemDto` and `StemSummaryDto` match backend shrink.
- `StemWordItemDto` becomes `{ uniqueWordId, displayText, occurrencesCount }`.
- Remove frontend use of `kind` and `displayTextUthmani`.
- `StemAyahMatchDto` becomes `{ ayahId, verseKey, surahNameArabic, pageNumber, words: [{ textUthmani, isMatched }] }`.
- `TypeSummaryDto` becomes `{ code, arabicLabel, occurrencesCount }`.

### UI/state behavior changes
- `stem-words-list` must use selected `wordView` from page state for deep link creation.
- Stems ayah rendering must derive surah/ayah numbers from `verseKey` using shared helper.
- Stems ayah list must not require marker rows or matched id arrays.
- `stem-ayah-type-filters` stops depending on `englishLabel`.
- Stems table must stop depending on removed type-summary fields; if type column remains visible, it needs a new source, otherwise hide/rework it.

### Test changes
- `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-words-list/stem-words-list.component.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-ayah-type-filters/stem-ayah-type-filters.component.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/stems-table/stems-table.component.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-url-sync.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-lemmas-list/stem-lemmas-list.component.spec.ts` only if contract wiring changes reach shared helpers
- Add a Stems ayah presenter spec if ayah rendering is split out

### Commands
- `cd Frontend/quran-dashboard-ui && npm test -- --run src/app/features/words`
- `cd Frontend/quran-dashboard-ui && npm run build`

### Risk
- Medium.

### Rollback risk
- Medium.

### Coordination
- Must follow Phase B.
- Shared model changes may touch Lemmas compile targets only if shared helpers are reused; avoid behavioral change there.

## Phase D - Verification and Review

### Goal
- Prove backend and frontend compile, tests pass, and bug is fixed.

### Commands
- `dotnet test Backend/QuranDashboard.sln --filter "FullyQualifiedName~WordsMorphologyExplorers|FullyQualifiedName~MorphologyRelationshipsReadTests|FullyQualifiedName~MorphologyExplorersLoggingTests|FullyQualifiedName~MorphologyExplorersCacheReadTests"`
- `dotnet build Backend/QuranDashboard.sln`
- `cd Frontend/quran-dashboard-ui && npm test -- --run src/app/features/words`
- `cd Frontend/quran-dashboard-ui && npm run build`

### Review scope
- Backend response contracts for Stems only.
- Frontend Stems pages, state, and tests only.
- Confirm `بدون تشكيل` now shows simple text.

### Risk
- Low.

### Rollback risk
- Low.

## Test Plan
- Backend contract tests first, because cleanup changes contract shape.
- Frontend tests second, because mocks and models must follow backend shape.
- Keep one focused regression test for simple vs tashkeel display source.
- Keep ayah tests focused on marker exclusion, `isMatched`, and Mushaf navigation page number.

## Risks and Open Questions
- Stems table currently uses type-distribution data for a visible column; if `dominantType` is removed, that UI needs a deliberate follow-up choice.
- Stems ayah response no longer fits the current shared highlight DTO shape; use a Stems-only adapter or presenter if needed.
- `TypeSummaryDto` shrink is breaking for any frontend test or component that still expects `englishLabel` or first-occurrence fields.
- `surahNumber` / `ayahNumber` removal requires `verseKey` parsing helper reuse.
- No migration, importer, seed, or Quran text change should be included.

## Recommended Commit Split
- Commit 1: Phase A backend bug fix and word DTO rename.
- Commit 2: Phase B backend contract cleanup.
- Commit 3: Phase C frontend model/state/component updates.
- Commit 4: Phase D verification only if report output or docs are tracked separately.
- Do not merge Phase B without Phase C; the API change is breaking until frontend is updated.
