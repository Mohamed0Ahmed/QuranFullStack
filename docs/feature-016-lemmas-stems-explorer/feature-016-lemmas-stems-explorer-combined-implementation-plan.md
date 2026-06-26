**Verdict: READY_FOR_SPEC_KIT**

# Feature 016 — Lemmas & Stems Explorer — Combined Backend + Frontend Implementation Plan

> **Planning report only.** No code, no Spec Kit files, no migrations, no endpoints, no route changes,
> no backend/frontend source changes, and no commits were produced. This document is the implementation
> planning input for a later Spec Kit `specify` → `plan` → `tasks` workflow.

**Source of truth:**
`docs/feature-016-lemmas-stems-explorer/feature-016-lemmas-stems-explorer-capability-linking-report.md`

---

## 1. Executive Summary

Feature 016 adds two read-only morphology explorer pages inside the existing Words area:

| Page | Arabic label | Route | Selection query param |
| --- | --- | --- | --- |
| Lemmas Explorer | الصيغ المعجمية | `/dashboard/words/lemmas` | `lemma={lemmaId}` |
| Stems Explorer | الأصول الصرفية | `/dashboard/words/stems` | `stem={stemId}` |

Both pages reuse the existing Feature 014 Unique Words Explorer and Feature 015 Roots Explorer patterns:

- backend read-only APIs using Application.Abstractions DTOs/read interfaces, Application handlers,
  Infrastructure EF readers, optional cache decorators, and thin API controllers;
- Angular pages under `src/app/features/words/` with table + persistent detail panel UX;
- shared URL state, pagination, highlighted ayah rendering, surah lists, count chips, loading/error/empty
  states, and deep-link helpers;
- Arabic-first, RTL, calm scholarly visual system with no new color palette or redesign.

The capability report verified that the data is ready:

- lemmas: 4,793;
- stems: 12,108;
- Arabic display values complete for both;
- every lemma/stem connects to readable Quran words through morphology;
- numeric IDs are the canonical URL identity;
- no migration, importer, Quran text mutation, or speculative index is justified.

**Final plan status:** ready to create Spec Kit artifacts after this plan.

---

## 2. Scope and Non-Goals

### In Scope

- One combined Feature 016 with two routeable explorer pages: Lemmas and Stems.
- Read-only backend endpoints for lemma/stem list summaries and detail tabs.
- Numeric-ID deep links for roots, lemmas, stems, unique words, and ayahs.
- Minimal Mushaf Reader word-analysis DTO enhancement for `lemma.id` and `stem.id`.
- Frontend route-path helpers and deep-link builders for lemmas/stems.
- Cross-page links rendered as real anchors with `target="_blank"` and `rel="noopener noreferrer"`.
- Internal same-page state changes for search, sort, pagination, row selection, detail tabs, sub-views,
  and detail pagination.
- Dominant POS/type summary in table rows, with full POS distribution in detail panels.
- Backend and frontend tests for URL restoration, count-click mapping, cross-page links, DTO identity,
  list counts, details, and accessibility.

### Explicit Non-Goals

- No implementation during this planning task.
- No Spec Kit artifacts yet.
- No backend/frontend source changes yet.
- No route changes yet.
- No endpoint additions yet.
- No migrations unless a later implementation phase discovers a hard blocker; current evidence says none.
- No importer, no data pipeline, no Quran text mutation.
- No new visual language, no new color palette, no redesign.
- No generic morphology endpoint that hides distinct lemma/stem contracts.
- No display-text, Buckwalter, or normalized-text URL identity.
- No speculative indexes without measured evidence.

---

## 3. Locked Product and UX Decisions

### 3.1 Routes and URL Identity

Canonical routes:

```text
/dashboard/words/lemmas
/dashboard/words/stems
```

Canonical identity query params:

| Entity | URL identity |
| --- | --- |
| Root | `root={rootId}` |
| Lemma | `lemma={lemmaId}` |
| Stem | `stem={stemId}` |
| Unique word | `word={uniqueWordId}` on existing Unique Words route mode |
| Ayah | existing Mushaf `page`, `ayah`, `focusAyah` contract |

Do not use Arabic display text, Buckwalter, or normalized text as canonical identity. Display strings are
for UI rendering only.

### 3.2 Lemmas Explorer URL State

Route:

```text
/dashboard/words/lemmas
```

Query params:

| Param | Values | Default | Meaning |
| --- | --- | --- | --- |
| `search` | string | empty | List search. |
| `sort` | `mushaf-order`, `occurrences`, `alpha` | `mushaf-order` | List sort; `occurrences` means descending. |
| `page` | positive int | `1` | List page. |
| `lemma` | positive int | none | Selected lemma ID. |
| `view` | `words`, `ayahs`, `surahs`, `stems` | `words` | Active detail tab. |
| `wordView` | `simple`, `tashkeel` | `simple` | Only when `view=words`. |
| `surahView` | `mentioned`, `missing` | `mentioned` | Only when `view=surahs`. |
| `detailPage` | positive int | `1` | Only for paged detail views (`words`, `ayahs`). |

### 3.3 Stems Explorer URL State

Route:

```text
/dashboard/words/stems
```

Query params:

| Param | Values | Default | Meaning |
| --- | --- | --- | --- |
| `search` | string | empty | List search. |
| `sort` | `mushaf-order`, `occurrences`, `alpha` | `mushaf-order` | List sort; `occurrences` means descending. |
| `page` | positive int | `1` | List page. |
| `stem` | positive int | none | Selected stem ID. |
| `view` | `words`, `ayahs`, `surahs`, `lemmas` | `words` | Active detail tab. |
| `wordView` | `simple`, `tashkeel` | `simple` | Only when `view=words`. |
| `surahView` | `mentioned`, `missing` | `mentioned` | Only when `view=surahs`. |
| `detailPage` | positive int | `1` | Only for paged detail views (`words`, `ayahs`). |

### 3.4 Tables and Detail Tabs

Lemmas table columns:

| Order | Column |
| ---: | --- |
| 1 | الصيغة المعجمية |
| 2 | الجذر |
| 3 | النوع |
| 4 | المواضع |
| 5 | الآيات |
| 6 | السور |
| 7 | كلمات بدون تشكيل |
| 8 | كلمات بالتشكيل |
| 9 | الأصول الصرفية |

Lemma details panel tabs:

| Tab | Sub-view |
| --- | --- |
| الكلمات | بدون تشكيل / بالتشكيل |
| الآيات | n/a |
| السور | وردت فيها / لم ترد فيها |
| الأصول الصرفية | n/a |

Stems table columns:

| Order | Column |
| ---: | --- |
| 1 | الأصل الصرفي |
| 2 | الصيغة المعجمية |
| 3 | الجذر |
| 4 | النوع |
| 5 | المواضع |
| 6 | الآيات |
| 7 | السور |
| 8 | كلمات بدون تشكيل |
| 9 | كلمات بالتشكيل |

Stem details panel tabs:

| Tab | Sub-view |
| --- | --- |
| الكلمات | بدون تشكيل / بالتشكيل |
| الآيات | n/a |
| السور | وردت فيها / لم ترد فيها |
| الصيغ المعجمية | n/a |

### 3.5 Definition of `النوع`

`النوع` is defined as the dominant head POS by occurrence count.

Rules:

- Count `quran_word_morphology.head_pos` occurrences within the selected lemma/stem.
- Join to `quran_pos_tags` and use existing controlled Arabic labels/display conventions.
- Do not invent POS/type labels.
- The table shows the dominant Arabic POS label.
- If more than one POS exists, show a compact indicator such as `+2` beside the dominant label.
- In a tie, choose the POS whose occurrence appears first in Mushaf order.
- Mushaf-order tie break should use the earliest linked word occurrence, ordered by `surah_number`,
  `ayah_number`, `word_number` from `quran_words`.
- Details panel should expose the full POS/type distribution with count and percentage/ratio if useful.

Recommended DTO shape for type summary:

```csharp
TypeSummaryDto(
  string Code,
  string ArabicLabel,
  string EnglishLabel,
  int OccurrencesCount,
  int FirstSurahNumber,
  int FirstAyahNumber,
  int FirstWordNumber)
```

List DTOs should include `DominantType` and `OtherTypesCount`; detail summaries should include
`TypeDistribution`.

### 3.6 Cross-Page Linking Rules

Cross-page study/deep links must render as normal anchors:

```html
target="_blank"
rel="noopener noreferrer"
```

Required destinations:

| Source | Target |
| --- | --- |
| Lemmas/Stems root click | `/dashboard/words/roots?root={rootId}&view=words&wordView=simple` |
| Stems lemma click | `/dashboard/words/lemmas?lemma={lemmaId}&view=words&wordView=simple` |
| Lemmas stem click | `/dashboard/words/stems?stem={stemId}&view=words&wordView=simple` |
| Lemmas/Stems ayah click | `/dashboard/mushaf?page={pageNumber}&ayah={verseKey}&focusAyah={verseKey}&panel=ayah` |
| Lemmas/Stems simple word click | `/dashboard/words/unique/simple?word={uniqueWordId}&view=ayahs` |
| Lemmas/Stems tashkeel word click | `/dashboard/words/unique/tashkeel?word={uniqueWordId}&view=ayahs` |
| Mushaf selected word root click | Roots explorer new tab |
| Mushaf selected word lemma click | Lemmas explorer new tab |
| Mushaf selected word stem click | Stems explorer new tab |

Internal same-page interactions remain same-tab URL state updates:

- search;
- sort;
- pagination;
- selected row;
- detail tab;
- `wordView`;
- `surahView`;
- `detailPage`.

---

## 4. Architecture Summary

### 4.1 Backend Architecture

Mirror Feature 014/015 layering:

```text
Api Controllers
  -> Application query handlers / outcomes / logging
    -> Application.Abstractions read interfaces + DTOs
      -> Infrastructure EF readers (AsNoTracking projections)
        -> Optional cache decorators using shared IMemoryCache
```

Backend rules:

- Controllers are thin HTTP adapters only.
- No raw SQL in controllers.
- EF/LINQ lives in Infrastructure readers.
- Read interfaces and DTOs live in `Application.Abstractions`.
- Query handlers validate inputs, map outcomes, and log safe structured fields.
- All endpoints return existing `ApiResponse<T>` envelopes.
- All operations are read-only.
- No migration is planned.

### 4.2 Frontend Architecture

Keep both pages under the existing Words feature area:

```text
src/app/features/words/
```

Frontend rules:

- Routeable page components are thin shells/orchestrators.
- Facades own API orchestration, URL restore, selected state, and loading/error/not-found state.
- Child components render presentation and emit UI events.
- Data-access services return `Observable<ApiResponse<T>>` and mirror backend endpoints.
- URL sync is isolated in `lemmas-url-sync.ts` and `stems-url-sync.ts`.
- Cross-page links use deep-link builders plus `deepLinkToHref` and render as anchors.
- The visual system closely mirrors Roots Explorer: table + persistent panel, shared styles, shared state
  components, shared pagination.

### 4.3 Backend/Frontend Contract Overview

Lemmas endpoints:

| Capability | Endpoint | Paged | Frontend trigger |
| --- | --- | --- | --- |
| Lemmas list | `GET /api/words/lemmas?search&sort&page&pageSize` | yes | page load/search/sort/page |
| Lemma summary | `GET /api/words/lemmas/{id}` | no | selected `lemma` URL restore |
| Lemma words | `GET /api/words/lemmas/{id}/words/{wordKind}?page&pageSize` | yes | `view=words&wordView=*` |
| Lemma ayahs | `GET /api/words/lemmas/{id}/ayahs?page&pageSize` | yes | `view=ayahs` |
| Lemma mentioned surahs | `GET /api/words/lemmas/{id}/surahs` | no | `view=surahs&surahView=mentioned` |
| Lemma missing surahs | `GET /api/words/lemmas/{id}/missing-surahs` | no | `view=surahs&surahView=missing` |
| Lemma related stems | `GET /api/words/lemmas/{id}/stems` | no | `view=stems` |

Stems endpoints:

| Capability | Endpoint | Paged | Frontend trigger |
| --- | --- | --- | --- |
| Stems list | `GET /api/words/stems?search&sort&page&pageSize` | yes | page load/search/sort/page |
| Stem summary | `GET /api/words/stems/{id}` | no | selected `stem` URL restore |
| Stem words | `GET /api/words/stems/{id}/words/{wordKind}?page&pageSize` | yes | `view=words&wordView=*` |
| Stem ayahs | `GET /api/words/stems/{id}/ayahs?page&pageSize` | yes | `view=ayahs` |
| Stem mentioned surahs | `GET /api/words/stems/{id}/surahs` | no | `view=surahs&surahView=mentioned` |
| Stem missing surahs | `GET /api/words/stems/{id}/missing-surahs` | no | `view=surahs&surahView=missing` |
| Stem related lemmas | `GET /api/words/stems/{id}/lemmas` | no | `view=lemmas` |

---

## 5. Backend Plan

### 5.1 Likely Backend Files to Add

Application.Abstractions:

```text
Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/
  ILemmasReader.cs
  LemmaSort.cs
  LemmaSortKeys.cs
  LemmaViewKeys.cs
  Responses/
    LemmaListItemDto.cs
    LemmaSummaryDto.cs
    LemmaWordItemDto.cs
    LemmaAyahMatchDto.cs
    LemmaSurahsResponse.cs
    LemmaMissingSurahsResponse.cs
    LemmaStemsResponse.cs
    LemmaTypeDistributionDto.cs

Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Stems/
  IStemsReader.cs
  StemSort.cs
  StemSortKeys.cs
  StemViewKeys.cs
  Responses/
    StemListItemDto.cs
    StemSummaryDto.cs
    StemWordItemDto.cs
    StemAyahMatchDto.cs
    StemSurahsResponse.cs
    StemMissingSurahsResponse.cs
    StemLemmasResponse.cs
    StemTypeDistributionDto.cs
```

Application:

```text
Backend/application/QuranDashboard.Application/Quran/Words/Lemmas/Queries/
  GetLemmasPage/
  GetLemmaSummary/
  GetLemmaWords/
  GetLemmaAyahs/
  GetLemmaMentionedSurahs/
  GetLemmaMissingSurahs/
  GetLemmaStems/

Backend/application/QuranDashboard.Application/Quran/Words/Stems/Queries/
  GetStemsPage/
  GetStemSummary/
  GetStemWords/
  GetStemAyahs/
  GetStemMentionedSurahs/
  GetStemMissingSurahs/
  GetStemLemmas/
```

Infrastructure:

```text
Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/
  EfLemmasReader.cs
  LemmasListDerivation.cs
  LemmasWordsDerivation.cs

Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/
  EfStemsReader.cs
  StemsListDerivation.cs
  StemsWordsDerivation.cs

Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Lemmas/
  CachedLemmasReader.cs
  LemmasCacheKeys.cs
  LemmasCacheEntryOptions.cs

Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Stems/
  CachedStemsReader.cs
  StemsCacheKeys.cs
  StemsCacheEntryOptions.cs

Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/
  LemmasDependencyInjection.cs
  StemsDependencyInjection.cs
```

Api:

```text
Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs
Backend/api/QuranDashboard.Api/Controllers/Words/StemsController.cs
Backend/api/QuranDashboard.Api/Common/ApiMessages.cs          # add messages near existing Words messages
```

Mushaf DTO/linking additions:

```text
Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/Responses/WordAnalysisResponse.cs
Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfWordAnalysisReader.cs
```

Tests:

```text
Backend/tests/QuranDashboard.Tests/Quran/WordsLemmas/
Backend/tests/QuranDashboard.Tests/Quran/WordsStems/
Backend/tests/QuranDashboard.Tests/Quran/MushafReader/   # DTO regression for lemma/stem IDs
```

### 5.2 DTO Contract Sketch

Shared response concepts can be duplicated with explicit names for clarity. Avoid a generic morphology
DTO hierarchy unless implementation reveals substantial safe reuse.

Lemma list/summary:

```csharp
public sealed record LemmaListItemDto(
    int Id,
    string LemmaText,
    string? LemmaBuckwalter,
    int? RootId,
    string? RootText,
    string? RootBuckwalter,
    TypeSummaryDto DominantType,
    int OtherTypesCount,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    int StemsCount,
    string FirstVerseKey);

public sealed record LemmaSummaryDto(
    int Id,
    string LemmaText,
    string? LemmaBuckwalter,
    int? RootId,
    string? RootText,
    string? RootBuckwalter,
    TypeSummaryDto DominantType,
    int OtherTypesCount,
    IReadOnlyList<TypeSummaryDto> TypeDistribution,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    int StemsCount,
    string FirstVerseKey);
```

Stem list/summary:

```csharp
public sealed record StemListItemDto(
    int Id,
    string StemText,
    int? LemmaId,
    string? LemmaText,
    string? LemmaBuckwalter,
    int? RootId,
    string? RootText,
    string? RootBuckwalter,
    TypeSummaryDto DominantType,
    int OtherTypesCount,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    string FirstVerseKey);

public sealed record StemSummaryDto(
    int Id,
    string StemText,
    int? LemmaId,
    string? LemmaText,
    string? LemmaBuckwalter,
    int? RootId,
    string? RootText,
    string? RootBuckwalter,
    TypeSummaryDto DominantType,
    int OtherTypesCount,
    IReadOnlyList<TypeSummaryDto> TypeDistribution,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    string FirstVerseKey);
```

Word detail rows:

```csharp
public sealed record LemmaWordItemDto(
    int UniqueWordId,
    string Kind,
    string DisplayTextUthmani,
    int OccurrencesCount,
    string FirstVerseKey);

public sealed record StemWordItemDto(
    int UniqueWordId,
    string Kind,
    string DisplayTextUthmani,
    int OccurrencesCount,
    string FirstVerseKey);
```

Ayah match rows should reuse the same shape as Feature 014/015 so `highlighted-ayah` can render safely:

```csharp
public sealed record LemmaAyahMatchDto(
    int AyahId,
    string VerseKey,
    int SurahNumber,
    string SurahNameArabic,
    int AyahNumber,
    short PageNumber,
    IReadOnlyList<int> MatchedQuranWordIds,
    IReadOnlyList<AyahWordForHighlightDto> Words);
```

Create the analogous `StemAyahMatchDto`.

Related items:

```csharp
public sealed record LemmaStemItemDto(
    int StemId,
    string StemText,
    int OccurrencesCount);

public sealed record StemLemmaItemDto(
    int LemmaId,
    string LemmaText,
    string? LemmaBuckwalter,
    int OccurrencesCount);
```

### 5.3 Read-Model Semantics

Driving table:

```text
quran_word_morphology m
JOIN quran_words w ON w.id = m.quran_word_id
```

Lemma filters:

```text
m.lemma_id = {lemmaId}
```

Stem filters:

```text
m.stem_id = {stemId}
```

Counts:

| Count | Rule |
| --- | --- |
| `OccurrencesCount` / المواضع | `COUNT(*)` over matching morphology rows. |
| `AyahsCount` | `COUNT(DISTINCT w.ayah_id)`. |
| `SurahsCount` | `COUNT(DISTINCT w.surah_number)`. |
| `SimpleWordsCount` | `COUNT(DISTINCT w.unique_simple_word_id)`. |
| `TashkeelWordsCount` | `COUNT(DISTINCT w.unique_tashkeel_word_id)`. |
| Lemma `StemsCount` | `COUNT(DISTINCT m.stem_id)`. |
| Stem related lemmas | `COUNT(DISTINCT m.lemma_id)` and related list by co-occurrence. |
| Dominant type | Group `m.head_pos`, sort by count desc, then first Mushaf occurrence asc. |

Root/lemma values in stem rows:

- A stem can co-occur with multiple lemmas/roots or none.
- For table summary columns `الصيغة المعجمية` and `الجذر`, use the dominant co-occurring lemma/root by
  occurrence count, tie-broken by first Mushaf occurrence.
- If there are additional co-occurring lemmas/roots, expose compact indicators in the UI if needed and
  full distribution in the detail panel or summary metadata.
- If no related root/lemma exists, return null fields and render a calm dash/empty value.

Root values in lemma rows:

- Prefer `quran_lemmas.root_id` for the table's root column because it is the existing ownership link.
- The detail panel may also expose full co-occurring root distribution if needed for scholarly clarity,
  but it is not one of the locked tabs for Feature 016.
- If `root_id` is null, return null fields and render a non-clickable dash.

### 5.4 Query Implementation Guidance

- Use `AsNoTracking()` on all EF queries.
- Use projections rather than entity materialization where possible.
- Keep list query inputs validated in handlers: positive `page`, positive `pageSize`, known `sort`, known
  `wordKind`.
- Use `wordKind` values `simple` and `tashkeel`, aligned with Feature 014/015.
- Build ayah matches with the existing batched pattern:
  select distinct ayah IDs, page them, load words for those ayahs, then compute `matchedQuranWordIds`
  for the selected lemma/stem in the current page.
- Keep all highlight identity by `quran_words.id`; never perform string replacement against Quran text.
- Whole-list loads are acceptable for mentioned/missing surahs and related lemma/stem lists.

### 5.5 Caching Plan

Use cache decorators only if they remain consistent with Feature 014/015 patterns.

Recommended cache strategy:

| Read | Cache key shape | Notes |
| --- | --- | --- |
| Lemma summary list | `lemmas:summary:all` | Whole-list cached once; search/sort/page in memory. |
| Stem summary list | `stems:summary:all` | Whole-list cached once; search/sort/page in memory. |
| Lemma summary | `lemmas:{id}:summary` | For URL restore. |
| Stem summary | `stems:{id}:summary` | For URL restore. |
| Lemma words | `lemmas:{id}:words:{kind}:p{page}:s{size}` | Paged. |
| Stem words | `stems:{id}:words:{kind}:p{page}:s{size}` | Paged. |
| Lemma ayahs | `lemmas:{id}:ayahs:p{page}:s{size}` | Paged. |
| Stem ayahs | `stems:{id}:ayahs:p{page}:s{size}` | Paged. |
| Surahs/missing | `lemmas:{id}:surahs`, `lemmas:{id}:missing`, `stems:{id}:surahs`, `stems:{id}:missing` | Whole. |
| Related items | `lemmas:{id}:stems`, `stems:{id}:lemmas` | Whole. |

Do not cache unbounded free-text search keys. If whole-list caching is accepted, search/sort/page can be
computed in memory over the cached list.

### 5.6 Logging Plan

Application handlers should log safe structured fields only.

Safe fields:

- `feature` = `Lemmas` or `Stems`;
- `operation`;
- `lemmaId` or `stemId`;
- `view`;
- `subView`;
- `pageNumber`;
- `pageSize`;
- `sort`;
- `hasSearch`;
- `totalCount`;
- `itemCount`;
- `cacheResult` if available;
- `elapsedMs` only if actually measured;
- `reason` for controlled rejections.

Do not log:

- Quran text;
- lemma/stem/root text;
- raw search text;
- full payloads;
- large lists.

### 5.7 Mushaf Reader Minimal Backend Change

No migration required.

Change DTO contracts:

```csharp
public sealed record WordMorphologyLemma(
    int Id,
    string? Text,
    string? Buckwalter);

public sealed record WordMorphologyStem(
    int Id,
    string? Text);
```

Update `EfWordAnalysisReader.MapMorphology(...)` to pass `lemma.Id` and `stem.Id`. The reader already
loads the lemma/stem entities by `morphology.LemmaId` and `morphology.StemId`.

---

## 6. Frontend Plan

### 6.1 Likely Frontend Files to Add

Routes and route helpers:

```text
Frontend/quran-dashboard-ui/src/app/core/navigation/route-paths.ts
Frontend/quran-dashboard-ui/src/app/features/words/words.routes.ts
```

New pages:

```text
Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/
  lemmas-explorer-page.component.ts
  lemmas-explorer-page.component.html
  lemmas-explorer-page.component.scss
  lemmas-explorer-page.component.spec.ts

Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/
  stems-explorer-page.component.ts
  stems-explorer-page.component.html
  stems-explorer-page.component.scss
  stems-explorer-page.component.spec.ts
```

New components:

```text
Frontend/quran-dashboard-ui/src/app/features/words/components/lemmas-table/
Frontend/quran-dashboard-ui/src/app/features/words/components/stems-table/
Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/
Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/
Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-words-list/
Frontend/quran-dashboard-ui/src/app/features/words/components/stem-words-list/
Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-stems-list/
Frontend/quran-dashboard-ui/src/app/features/words/components/stem-lemmas-list/
Frontend/quran-dashboard-ui/src/app/features/words/components/type-distribution-list/
```

Data access:

```text
Frontend/quran-dashboard-ui/src/app/features/words/data-access/lemmas.api.ts
Frontend/quran-dashboard-ui/src/app/features/words/data-access/stems.api.ts
```

State:

```text
Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-url-sync.ts
Frontend/quran-dashboard-ui/src/app/features/words/state/stems-url-sync.ts
Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-explorer.facade.ts
Frontend/quran-dashboard-ui/src/app/features/words/state/stems-explorer.facade.ts
Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail.facade.ts
Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail.facade.ts
Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-cache.ts
Frontend/quran-dashboard-ui/src/app/features/words/state/stems-cache.ts
```

Models/labels:

```text
Frontend/quran-dashboard-ui/src/app/features/words/models/lemmas.models.ts
Frontend/quran-dashboard-ui/src/app/features/words/models/stems.models.ts
Frontend/quran-dashboard-ui/src/app/features/words/models/lemmas.labels.ts
Frontend/quran-dashboard-ui/src/app/features/words/models/stems.labels.ts
```

Mushaf model/linking updates:

```text
Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts
Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.ts
Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.html
Frontend/quran-dashboard-ui/src/app/features/mushaf/components/word-morphology-summary/word-morphology-summary.component.ts
Frontend/quran-dashboard-ui/src/app/features/mushaf/components/word-morphology-summary/word-morphology-summary.component.html
```

### 6.2 Route Helpers and Deep-Link Builders

Add route-path helpers:

```ts
export const WORDS_LEMMAS_SEGMENT = 'lemmas' as const;
export const WORDS_STEMS_SEGMENT = 'stems' as const;

export function lemmasRoutePath(): string {
  return `${WORDS_ROUTE_PATH}/${WORDS_LEMMAS_SEGMENT}`;
}

export function stemsRoutePath(): string {
  return `${WORDS_ROUTE_PATH}/${WORDS_STEMS_SEGMENT}`;
}
```

Add deep-link builders:

```ts
buildLemmasDeepLink(options?: LemmasQueryChange): DeepLinkTarget
buildStemsDeepLink(options?: StemsQueryChange): DeepLinkTarget
```

They should mirror `buildRootsDeepLink` and `buildUniqueWordsDeepLink`, returning `{ path, queryParams }`.

All cross-page rendered links should use:

```html
<a [href]="..." target="_blank" rel="noopener noreferrer">...</a>
```

### 6.3 Frontend Model Keys

Lemma model types:

```ts
export type LemmaSort = 'mushaf-order' | 'occurrences' | 'alpha';
export type LemmaView = 'words' | 'ayahs' | 'surahs' | 'stems';
export type LemmaWordView = 'simple' | 'tashkeel';
export type LemmaSurahView = 'mentioned' | 'missing';

export const LEMMAS_QUERY_KEYS = {
  search: 'search',
  sort: 'sort',
  page: 'page',
  lemma: 'lemma',
  view: 'view',
  wordView: 'wordView',
  surahView: 'surahView',
  detailPage: 'detailPage',
} as const;
```

Stem model types:

```ts
export type StemSort = 'mushaf-order' | 'occurrences' | 'alpha';
export type StemView = 'words' | 'ayahs' | 'surahs' | 'lemmas';
export type StemWordView = 'simple' | 'tashkeel';
export type StemSurahView = 'mentioned' | 'missing';

export const STEMS_QUERY_KEYS = {
  search: 'search',
  sort: 'sort',
  page: 'page',
  stem: 'stem',
  view: 'view',
  wordView: 'wordView',
  surahView: 'surahView',
  detailPage: 'detailPage',
} as const;
```

Defaults:

```text
sort = mushaf-order
page = 1
view = words
wordView = simple
surahView = mentioned
detailPage = 1
```

### 6.4 Page and Component Behavior

Lemmas page:

- Thin shell with controls, `lemmas-table`, and `lemma-details-panel`.
- Binds list facade and detail facade to route query params.
- Search is debounced and updates same-tab query params.
- Sort/page changes update same-tab query params.
- Row selection sets `lemma`, default `view=words`, `wordView=simple`.
- Count clicks map to the correct detail tab/sub-view.

Stems page:

- Same structure with `stems-table` and `stem-details-panel`.
- Row selection sets `stem`, default `view=words`, `wordView=simple`.
- Count clicks map to the correct detail tab/sub-view.

Count-click mapping:

| Count | Lemma destination | Stem destination |
| --- | --- | --- |
| المواضع | `view=ayahs` | `view=ayahs` |
| الآيات | `view=ayahs` | `view=ayahs` |
| السور | `view=surahs&surahView=mentioned` | `view=surahs&surahView=mentioned` |
| كلمات بدون تشكيل | `view=words&wordView=simple` | `view=words&wordView=simple` |
| كلمات بالتشكيل | `view=words&wordView=tashkeel` | `view=words&wordView=tashkeel` |
| الأصول الصرفية | `view=stems` | n/a |
| الصيغ المعجمية | n/a | `view=lemmas` |

### 6.5 Reuse and Visual Consistency

Reuse or closely mirror:

- `pages/roots-explorer-page/*` layout pattern;
- `components/roots-table/*` table semantics and count-chip behavior;
- `components/root-details-panel/*` persistent panel, tabs, inline/mobile behavior;
- `components/ayah-matches-list/*`;
- `components/highlighted-ayah/*`;
- `components/surah-occurrences-list/*`;
- `components/missing-surahs-list/*`;
- `components/word-count-chip/*`;
- `shared/ui/pagination/*`;
- `shared/url/deep-link-href.ts`.

Do not create new design tokens, colors, visual language, or dashboard layout motifs.

### 6.6 Mushaf Frontend Update

Update TypeScript models:

```ts
lemma: { id: number; text: string | null; buckwalter: string | null } | null;
stem: { id: number; text: string | null } | null;
```

Update selected word section:

- Add `lemmaExplorerHref` computed via `buildLemmasDeepLink({ lemmaId, view: 'words', wordView: 'simple' })`.
- Add `stemExplorerHref` computed via `buildStemsDeepLink({ stemId, view: 'words', wordView: 'simple' })`.
- Preserve existing root and unique-word links.
- Render root/lemma/stem as anchors only when corresponding IDs exist.
- Use `target="_blank"` and `rel="noopener noreferrer"` on root/lemma/stem/unique-word links.

---

## 7. Backend Implementation Phases

### Phase B0 — Backend Contracts and Mushaf DTO IDs

Add Application.Abstractions DTOs/read interfaces for Lemmas and Stems, plus minimal Mushaf DTO ID
contract changes.

Exit criteria:

- DTOs compile.
- `WordMorphologyLemma` and `WordMorphologyStem` include IDs.
- No endpoint behavior required yet if Spec Kit later phases split implementation.

### Phase B1 — Lemmas List and Summary

Implement `ILemmasReader`, `EfLemmasReader`, optional `CachedLemmasReader`, handlers, and controller
actions for:

```text
GET /api/words/lemmas
GET /api/words/lemmas/{id}
```

Exit criteria:

- Lemmas list returns locked columns and dominant type summary.
- Search/sort/page work.
- Summary restores by numeric `lemma` ID.
- No display/Buckwalter lookup path exists.

### Phase B2 — Stems List and Summary

Implement `IStemsReader`, `EfStemsReader`, optional `CachedStemsReader`, handlers, and controller actions
for:

```text
GET /api/words/stems
GET /api/words/stems/{id}
```

Exit criteria:

- Stems list returns locked columns and dominant type summary.
- Optional root/lemma fields are null-safe.
- Search/sort/page work.
- Summary restores by numeric `stem` ID.

### Phase B3 — Words and Unique Word Links

Implement paged word endpoints:

```text
GET /api/words/lemmas/{id}/words/{wordKind}
GET /api/words/stems/{id}/words/{wordKind}
```

Exit criteria:

- Rows include unique word IDs for Feature 014 links.
- `simple` uses `unique_simple_word_id`; `tashkeel` uses `unique_tashkeel_word_id`.
- Counts are scoped to selected lemma/stem.

### Phase B4 — Ayahs and Surahs

Implement:

```text
GET /api/words/lemmas/{id}/ayahs
GET /api/words/lemmas/{id}/surahs
GET /api/words/lemmas/{id}/missing-surahs
GET /api/words/stems/{id}/ayahs
GET /api/words/stems/{id}/surahs
GET /api/words/stems/{id}/missing-surahs
```

Exit criteria:

- Ayah matches are paged.
- `matchedQuranWordIds` are exact.
- Mentioned + missing surahs total 114.
- Ayah DTOs include `verseKey` and `pageNumber` for Mushaf links.

### Phase B5 — Related Stems/Lemmas and Type Distribution

Implement:

```text
GET /api/words/lemmas/{id}/stems
GET /api/words/stems/{id}/lemmas
```

Ensure summary/details expose full type distribution.

Exit criteria:

- Lemma detail can list related stems with IDs.
- Stem detail can list related lemmas with IDs.
- Type distribution matches dominant type table summary.

### Phase B6 — Backend Hardening

Finalize cache behavior, logging, validation, and not-found/error outcomes.

Exit criteria:

- Thin controllers.
- Safe Arabic API messages.
- No raw search/lexical/Quran text in logs.
- Query counts bounded for ayah matches.
- No migration/index introduced.

---

## 8. Frontend Implementation Phases

### Phase F0 — Frontend Models, URL Sync, Route Helpers

Add route helpers, models, labels, URL parse/build, and deep-link builders.

Exit criteria:

- `lemmasRoutePath()` and `stemsRoutePath()` exist.
- `buildLemmasDeepLink(...)` and `buildStemsDeepLink(...)` exist.
- URL sync tests cover valid and invalid query params.

### Phase F1 — Lemmas Page Shell and List

Add Lemmas route/page/table/facade/data-access for list and summary.

Exit criteria:

- `/dashboard/words/lemmas` page renders table in Roots visual style.
- Search/sort/page update same-tab query params.
- Row selection opens persistent details panel on `words/simple`.
- Invalid `lemma` URL shows not-found state without breaking the list.

### Phase F2 — Stems Page Shell and List

Add Stems route/page/table/facade/data-access for list and summary.

Exit criteria:

- `/dashboard/words/stems` page renders table in Roots visual style.
- Null root/lemma values render calmly and non-clickably.
- Row selection opens persistent details panel on `words/simple`.

### Phase F3 — Words, Unique Word Links, and Count Clicks

Implement words sub-views for both pages and count-click mapping.

Exit criteria:

- `wordView=simple/tashkeel` restores from URL.
- Count clicks open correct tabs/sub-views.
- Unique word links render as new-tab anchors with correct href.

### Phase F4 — Ayahs, Surahs, and Mushaf Links

Implement ayah and surah tabs for both pages.

Exit criteria:

- Ayah links use `buildMushafDeepLink` and open in a new tab.
- Highlighted ayah rendering reuses existing component.
- Mentioned/missing surah sub-views restore from URL.

### Phase F5 — Related Stems/Lemmas, Root Links, Type Distribution

Implement Lemma related stems, Stem related lemmas, root links, and type distribution UI.

Exit criteria:

- Lemma stems link to Stems Explorer in new tabs.
- Stem lemmas link to Lemmas Explorer in new tabs.
- Root links open Roots Explorer in new tabs.
- Full type distribution is visible in details panel.

### Phase F6 — Mushaf Selected Word Links

Update Mushaf selected word models and UI links.

Exit criteria:

- Root/lemma/stem are clickable when IDs are available.
- Links open target explorers in new tabs.
- Display-only fallback remains for missing root/lemma/stem.

### Phase F7 — Frontend Hardening

Finalize responsive behavior, accessibility, and state coverage.

Exit criteria:

- Desktop uses persistent details panel.
- Narrow/mobile follows existing Roots drawer/modal adaptation only.
- Loading/error/empty/not-found states match Words/Roots system.
- Count buttons, tablists, selected rows, and anchors are accessible.

---

## 9. Testing Plan

### 9.1 Backend Tests

Likely test folders:

```text
Backend/tests/QuranDashboard.Tests/Quran/WordsLemmas/
Backend/tests/QuranDashboard.Tests/Quran/WordsStems/
Backend/tests/QuranDashboard.Tests/Quran/MushafReader/
```

Required coverage:

- list summary count tests for lemmas and stems;
- representative lemma/stem details tests;
- dominant POS/type distribution tests, including tie-break by first Mushaf occurrence;
- null optional root/lemma handling tests;
- ayah matches return exact `matchedQuranWordIds`;
- mentioned + missing surahs total 114;
- unique word IDs present in word detail rows;
- no display/Buckwalter URL lookup tests;
- numeric ID not-found tests;
- invalid sort/kind/paging tests;
- cache hit/bypass behavior if cache decorators are added;
- logging redaction tests: no Quran text, lexical text, or raw search text;
- Mushaf WordAnalysis DTO includes `lemmaId`/`stemId`.

Test data rules:

- Use source-safe Quran test data.
- Prefer Testcontainers PostgreSQL and committed seed slices as in Feature 014/015.
- Do not invent Quran text unless clearly synthetic and not presented as Quranic content.

### 9.2 Frontend Tests

Required coverage:

- URL parse/build/restore for lemmas and stems;
- invalid query params default safely;
- row selection opens details panel;
- count-click mapping opens correct tab/sub-view;
- list render does not fetch detail endpoints;
- cross-page links generate correct href and open in new tab via anchor attributes;
- root/lemma/stem/ayah/unique-word link builders produce expected URLs;
- Mushaf selected word root/lemma/stem links render when IDs exist;
- missing root/lemma/stem falls back to non-clickable display;
- loading/error/empty/not-found states;
- panel responsive behavior with `matchMedia`/`ResizeObserver` guards;
- accessibility of count buttons, tablist, selected row state, and anchors.

Frontend test runner note:

- Preserve existing Vitest worker cap conventions (`VITEST_MAX_FORKS`) to avoid OOM.

### 9.3 Milestone Checkpoints

| Checkpoint | Backend | Frontend |
| --- | --- | --- |
| CP-0 Contracts | DTOs/interfaces compile; Mushaf DTO IDs compile | models/url-sync/deep-link builders test |
| CP-1 Lists | lemma/stem list counts, dominant type summary | pages render lists, search/sort/page restore |
| CP-2 Selection | summary not-found/invalid ID outcomes | row selection and panel restore |
| CP-3 Details words | unique word IDs and scoped counts | word sub-views and unique-word links |
| CP-4 Ayahs/surahs | matched IDs; surahs total 114 | highlighted ayahs, Mushaf links, surah sub-views |
| CP-5 Related items | lemma stems and stem lemmas | related-item/root links new-tab anchors |
| CP-6 Mushaf | WordAnalysis IDs present | selected word root/lemma/stem links |
| CP-7 Hardening | logging/cache/query bounds | responsive/a11y/state matrix |

---

## 10. Risks, Decisions, and Mitigations

| Classification | Risk/Decision | Mitigation/Decision |
| --- | --- | --- |
| Decision | One combined feature, not two separate features | Implement shared patterns but explicit lemma/stem contracts. |
| Decision | Numeric URL identities only | Use `lemma`, `stem`, `root`, and `word` IDs; never display/Buckwalter lookup. |
| Decision | `النوع` definition | Dominant `head_pos` by count; tie by first Mushaf occurrence; full distribution in panel. |
| Decision | Cross-page links new tab | Render anchors with `target="_blank"` and `rel="noopener noreferrer"`. |
| Risk | Stem may have no co-occurring root/lemma | Return nulls; render dash/non-clickable value; test null handling. |
| Risk | Lemma Buckwalter duplicates | Do not use Buckwalter as URL identity; test no Buckwalter lookup. |
| Risk | List summary aggregation larger than Roots | Use measured whole-list cache strategy; re-measure if production differs; no speculative index. |
| Risk | N+1 ayah matching | Reuse batched F014/F015 shape; add query-count tests. |
| Risk | Visual drift | Reuse Roots Explorer components/styles; no new palette or redesign. |
| Risk | Mushaf DTO contract change affects consumers | Minimal additive fields only; update TypeScript models/tests. |
| Risk | Over-generic backend abstraction | Prefer explicit `Lemmas` and `Stems` readers/controllers/DTOs. |
| Risk | Logs leak lexical/Quran/search text | Log IDs/counts/booleans only; redaction tests. |

No blocking risk currently requires changes before Spec Kit.

---

## 11. Spec Kit Readiness

This plan is ready to feed `/speckit.specify`.

The future Spec Kit spec should preserve these locked requirements:

- two pages in one feature;
- read-only over existing morphology data;
- no migration/importer/Quran text mutation;
- numeric IDs as canonical URL identity;
- explicit Lemmas/Stems endpoints;
- `النوع` dominant POS definition;
- persistent Roots-style detail panel on desktop;
- new-tab anchors for all cross-page study/deep links;
- minimal Mushaf DTO additions for `lemma.id` and `stem.id`.

**Final verdict: READY_FOR_SPEC_KIT**
