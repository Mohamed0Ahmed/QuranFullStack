# Stems Explorer Responses Audit

## Executive Summary
- `بدون تشكيل` bug is backend projection bug in `EfStemsReader.LoadStemWordRowsAsync`.
- Frontend `StemWordsListComponent` only renders `displayTextUthmani`; no frontend-only bug found.
- Canonical simple display field is `TextUthmaniSimple`.
- Biggest cleanup target: dead wrapper metadata on stem surah/lemma response DTOs, plus a few unused stem detail fields.

## Bug Diagnosis: `بدون تشكيل`
- Backend simple branch still projects `w.TextUthmani`.
- `Roots` simple branch uses `u.TextUthmaniSimple`.
- `Lemmas` simple branch uses `w.TextUthmaniSimple`.
- Stems simple view should use `w.TextUthmaniSimple` too.
- `word_key_imlaei_simple` is identity/search data in Unique Words, not display text.

## Endpoint / Response Inventory

| Endpoint | Response | Backend source | Frontend consumer | Note |
|---|---|---|---|---|
| `GET /api/words/stems` | `PagedResult<StemListItemDto>` | `LoadWholeSummaryAsync` + `StemsListDerivation.ToPage` | `StemsExplorerFacade`, `StemsTableComponent` | Core table payload. |
| `GET /api/words/stems/{id}` | `StemSummaryDto` | same summary cache + `ToSummary` | `StemsDetailFacade`, `StemAyahTypeFiltersComponent` | Needed for detail panel and type chips. |
| `GET /api/words/stems/{id}/words/{wordKind}` | `PagedResult<StemWordItemDto>` | `GetStemWordsPageAsync` -> `LoadStemWordRowsAsync` | `StemWordsListComponent` | Bug lives here. |
| `GET /api/words/stems/{id}/ayahs` | `PagedResult<StemAyahMatchDto>` | `GetStemAyahMatchesAsync` | `AyahMatchesListComponent`, `HighlightedAyahComponent` | Highlight payload needed. |
| `GET /api/words/stems/{id}/surahs` | `StemSurahsResponse` | `GetStemMentionedSurahsAsync` | `StemsDetailFacade` -> `SurahOccurrencesListComponent` | Wrapper metadata unused in current UI. |
| `GET /api/words/stems/{id}/missing-surahs` | `StemMissingSurahsResponse` | `GetStemMissingSurahsAsync` | `StemsDetailFacade` -> `MissingSurahsListComponent` | Wrapper metadata unused in current UI. |
| `GET /api/words/stems/{id}/lemmas` | `StemLemmasResponse` | `GetStemLemmasAsync` | `StemsDetailFacade` -> `StemLemmasListComponent` | Wrapper metadata unused in current UI. |
| Related roots | none | none | none | No Stems roots response exists. |

Shared envelope: `ApiResponse<T>`.

## Field Usage Matrix

| Field | Classification | Reason |
|---|---|---|
| `StemWordItemDto.displayTextUthmani` | `FIX SOURCE` | Simple branch should use `TextUthmaniSimple`. |
| `StemWordItemDto.firstVerseKey` | `REMOVE` | No runtime consumer. |
| `StemAyahMatchDto.surahNumber` | `REMOVE` | Not rendered or typed in frontend model. |
| `StemSurahsResponse.id`, `stemText`, `surahsCount` | `REMOVE` | UI only uses `surahs[]`. |
| `StemMissingSurahsResponse.id`, `stemText`, `missingSurahsCount` | `REMOVE` | UI only uses `surahs[]`. |
| `StemLemmasResponse.id`, `stemText`, `lemmasCount` | `REMOVE` | UI only uses `lemmas[]`. |
| `StemListItemDto.lemmaBuckwalter`, `rootBuckwalter` | `KEEP FOR NOW` | Unused in UI, but backend tests still pin them. |
| `StemListItemDto.firstVerseKey` | `KEEP FOR NOW` | Backend tests assert it. |
| `StemSummaryDto.typeDistribution.first*` | `KEEP FOR NOW` | Ordered type contract. |
| `StemListItemDto` core identity/counts | `KEEP` | Required by table. |
| `StemWordItemDto.uniqueWordId`, `kind`, `occurrencesCount` | `KEEP` | Deep link + counts. |
| `StemAyahMatchDto.ayahId`, `verseKey`, `surahNameArabic`, `ayahNumber`, `pageNumber`, `matchedQuranWordIds`, `words` | `KEEP` | Highlight + Mushaf link. |
| `AyahWordForHighlightDto.quranWordId`, `TextUthmani`, `IsAyahMarker` | `KEEP` | Highlight component filters markers. |
| `StemLemmasDto.lemmas[]` | `KEEP` | Rendered on detail tab. |
| `TypeSummaryDto.code`, `arabicLabel`, `englishLabel`, `occurrencesCount` | `KEEP` | Filter chips use these. |

## Fields Recommended For Removal
- `StemWordItemDto.firstVerseKey`
- `StemAyahMatchDto.surahNumber`
- `StemSurahsResponse.id`, `StemSurahsResponse.stemText`, `StemSurahsResponse.surahsCount`
- `StemMissingSurahsResponse.id`, `StemMissingSurahsResponse.stemText`, `StemMissingSurahsResponse.missingSurahsCount`
- `StemLemmasResponse.id`, `StemLemmasResponse.stemText`, `StemLemmasResponse.lemmasCount`

## Fields Recommended To Keep
- Stem list identity/count fields.
- Stem summary `typeDistribution`.
- Ayah highlight payload.
- Lemma item payload.
- Type summary payload.
- `lemmaBuckwalter`, `rootBuckwalter`, `firstVerseKey` for now because tests still cover them.

## Compatibility Risks
- Low: backend-only source fix for `displayTextUthmani`.
- Medium: response slimming changes API shapes and frontend models.
- Medium: removing `surahNumber` / `firstVerseKey` requires test and fixture updates.
- Medium/high: removing `lemmaBuckwalter` / `rootBuckwalter` / stem `firstVerseKey` needs backend test updates first.

## Proposed Phased Plan

### Phase A: Fix `بدون تشكيل`
- Files: `Backend/infrastructure/.../EfStemsReader.cs`, `Backend/tests/.../StemsWordsReadTests.cs`
- Tests: focused backend morphology explorer suite
- Commands: `dotnet test --filter "FullyQualifiedName~WordsMorphologyExplorers"`, `dotnet build QuranDashboard.sln`
- Risk: low

### Phase B: Remove Unused Response Fields
- Files: stem response DTOs, `EfStemsReader.cs`, related tests
- Tests: update backend read tests first, then targeted build/test
- Commands: same backend suite
- Risk: medium

### Phase C: Update Frontend Models / Tests
- Files: `stems.models.ts`, `stems.api.ts`, `stems-detail.facade.ts`, `stems-detail-view.loader.ts`, `stems-detail-panel.updates.ts`, Stems specs
- Tests: `npm test -- --run src/app/features/words`
- Risk: medium

### Phase D: Verification
- Commands: `dotnet build Backend/QuranDashboard.sln`, `npm run build --prefix Frontend/quran-dashboard-ui`
- Risk: low

## Implementation Prompts

### Backend-only prompt
Fix `EfStemsReader.LoadStemWordRowsAsync` simple branch to project `w.TextUthmaniSimple`, then add a regression test proving simple stem words render from the simple field.

### Frontend-only prompt
If response slimming lands, update `stems.models.ts`, Stems facade/loader/update helpers, and Stems specs to drop dead wrapper metadata and any removed fields.

### Combined prompt
Coordinate backend + frontend Stems cleanup in one pass, keep visible behavior unchanged, remove dead response metadata, and update tests/mocks together.
