# Contract: Lemmas & Stems Explorer Read API

All endpoints are read-only `GET` operations and return the existing `ApiResponse<T>` envelope.
Properties remain English; user-facing messages are centralized and Arabic by default. Controllers
bind input, call Application handlers, and map controlled outcomes only.

## Common Rules

- Defaults: `page=1`; catalogue `pageSize` follows the existing Roots/Words list convention; detail
  word/ayah page size follows the existing Roots detail convention; maximum page size remains bounded
  by the shared paging rules.
- `sort`: `mushaf-order`, `occurrences`, or `alpha`; default `mushaf-order`.
- `wordKind`: `simple` or `tashkeel`.
- `400`: invalid ID, sort, kind, non-positive page, or non-positive/out-of-bounds page size.
- `404`: valid positive resource ID does not exist.
- `200`: successful response, including a valid positive catalogue or detail page beyond the
  available results; that case returns an empty `Items` collection with the normal paging metadata.
- Unexpected `500` is handled globally.

## Lemmas Endpoints

Route base: `/api/words/lemmas`.

### List

```text
GET /api/words/lemmas?search={text}&sort={key}&page={n}&pageSize={n}
→ ApiResponse<PagedResult<LemmaListItemDto>>
```

Arabic-normalized contains search over lemma display text. Summary only; no detail lists.

### Summary

```text
GET /api/words/lemmas/{id}
→ ApiResponse<LemmaSummaryDto>
```

Used for URL restoration and panel heading/type distribution.

### Words

```text
GET /api/words/lemmas/{id}/words/{wordKind}?page={n}&pageSize={n}
→ ApiResponse<PagedResult<LemmaWordItemDto>>
```

### Ayahs

```text
GET /api/words/lemmas/{id}/ayahs?page={n}&pageSize={n}
→ ApiResponse<PagedResult<LemmaAyahMatchDto>>
```

### Mentioned Surahs

```text
GET /api/words/lemmas/{id}/surahs
→ ApiResponse<LemmaSurahsResponse>
```

### Missing Surahs

```text
GET /api/words/lemmas/{id}/missing-surahs
→ ApiResponse<LemmaMissingSurahsResponse>
```

### Related Stems

```text
GET /api/words/lemmas/{id}/stems
→ ApiResponse<LemmaStemsResponse>
```

## Stems Endpoints

Route base: `/api/words/stems`.

### List

```text
GET /api/words/stems?search={text}&sort={key}&page={n}&pageSize={n}
→ ApiResponse<PagedResult<StemListItemDto>>
```

### Summary

```text
GET /api/words/stems/{id}
→ ApiResponse<StemSummaryDto>
```

### Words

```text
GET /api/words/stems/{id}/words/{wordKind}?page={n}&pageSize={n}
→ ApiResponse<PagedResult<StemWordItemDto>>
```

### Ayahs

```text
GET /api/words/stems/{id}/ayahs?page={n}&pageSize={n}
→ ApiResponse<PagedResult<StemAyahMatchDto>>
```

### Mentioned Surahs

```text
GET /api/words/stems/{id}/surahs
→ ApiResponse<StemSurahsResponse>
```

### Missing Surahs

```text
GET /api/words/stems/{id}/missing-surahs
→ ApiResponse<StemMissingSurahsResponse>
```

### Related Lemmas

```text
GET /api/words/stems/{id}/lemmas
→ ApiResponse<StemLemmasResponse>
```

## Response Shapes

Conceptual C# record shapes; implementation may split supporting records into focused files.

```csharp
TypeSummaryDto(
    string Code,
    string ArabicLabel,
    string EnglishLabel,
    int OccurrencesCount,
    int FirstSurahNumber,
    int FirstAyahNumber,
    int FirstWordNumber)

LemmaListItemDto(
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
    string FirstVerseKey)

LemmaSummaryDto(
    // same identity/display/count fields as list,
    IReadOnlyList<TypeSummaryDto> TypeDistribution)

StemListItemDto(
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
    string FirstVerseKey)

StemSummaryDto(
    // same identity/display/count fields as list,
    IReadOnlyList<TypeSummaryDto> TypeDistribution)
```

Word item:

```csharp
LemmaWordItemDto / StemWordItemDto(
    int UniqueWordId,
    string Kind,
    string DisplayTextUthmani,
    int OccurrencesCount,
    string FirstVerseKey)
```

Ayah match (resource-specific names, existing shared word shape):

```csharp
LemmaAyahMatchDto / StemAyahMatchDto(
    int AyahId,
    string VerseKey,
    int SurahNumber,
    string SurahNameArabic,
    int AyahNumber,
    short PageNumber,
    IReadOnlyList<int> MatchedQuranWordIds,
    IReadOnlyList<AyahWordForHighlightDto> Words)
```

Bounded responses:

```csharp
LemmaSurahsResponse(int Id, string LemmaText, int SurahsCount, IReadOnlyList<SurahOccurrenceDto> Surahs)
StemSurahsResponse(int Id, string StemText, int SurahsCount, IReadOnlyList<SurahOccurrenceDto> Surahs)

LemmaMissingSurahsResponse(int Id, string LemmaText, int MissingSurahsCount, IReadOnlyList<MissingSurahDto> Surahs)
StemMissingSurahsResponse(int Id, string StemText, int MissingSurahsCount, IReadOnlyList<MissingSurahDto> Surahs)

LemmaStemsResponse(int Id, string LemmaText, int StemsCount, IReadOnlyList<LemmaStemItemDto> Stems)
LemmaStemItemDto(int StemId, string StemText, int OccurrencesCount)

StemLemmasResponse(int Id, string StemText, int LemmasCount, IReadOnlyList<StemLemmaItemDto> Lemmas)
StemLemmaItemDto(int LemmaId, string LemmaText, string? LemmaBuckwalter, int OccurrencesCount)
```

IDs are navigation/restoration fields and are not rendered as visible content.

## Mushaf Word Analysis Additive Contract

Existing `WordMorphologyDto` keeps the same structure, with additive identities:

```csharp
WordMorphologyLemma(int Id, string? Text, string? Buckwalter)
WordMorphologyStem(int Id, string? Text)
```

When the morphology relationship is absent, the containing `Lemma` or `Stem` remains null. No lookup
by text is added.

## Count and Ordering Contract

- Every aggregate is derived from the selected resource's matching morphology occurrences.
- Type distribution: count descending, then earliest Mushaf occurrence ascending.
- Stem dominant lemma/root: co-occurrence count descending, then earliest Mushaf occurrence.
- Lemma root: owned `quran_lemmas.root_id`, not inferred dominant co-occurrence.
- Catalogue `mushaf-order`: existing first occurrence order.
- Catalogue `occurrences`: descending count with deterministic Mushaf-order tie-break.
- Catalogue `alpha`: Arabic display text with deterministic identity tie-break.

## Caching

| Read | Key pattern |
|---|---|
| Lemma whole summary | `lemmas:summary:all` |
| Stem whole summary | `stems:summary:all` |
| Summary | `lemmas:{id}:summary`, `stems:{id}:summary` |
| Words | `{resource}:{id}:words:{kind}:p{page}:s{size}` |
| Ayahs | `{resource}:{id}:ayahs:p{page}:s{size}` |
| Surahs/missing | `{resource}:{id}:surahs`, `{resource}:{id}:missing` |
| Related items | `lemmas:{id}:stems`, `stems:{id}:lemmas` |

Use the existing shared memory cache and resource-specific decorators. Do not reconfigure the global
cache and do not key retained entries by raw free-text search.

## Logging

Application handler boundary fields:

- `feature`: `Lemmas` or `Stems`
- `operation`
- `lemmaId` or `stemId`
- `view`, `subView`
- `pageNumber`, `pageSize`, `sort`
- `hasSearch`
- `totalCount`, `itemCount`
- `cacheResult` when available
- `elapsedMs` only when measured
- `reason` for controlled rejection/not-found

Forbidden: Quran/ayah/word/lemma/stem/root text, Buckwalter text, raw search, SQL, response payloads,
connection details, or large ID lists.
