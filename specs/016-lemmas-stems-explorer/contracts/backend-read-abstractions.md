# Contract: Backend Read Abstractions

## Purpose

Define focused Application.Abstractions boundaries for Feature 016. Application handlers own
validation, outcomes, and structured diagnostics. Infrastructure implements read-only EF projections
and cache decorators. API controllers do not access readers or EF directly.

## `ILemmasReader`

```csharp
Task<PagedResult<LemmaListItemDto>> GetLemmasPageAsync(
    string? search,
    LemmaSort sort,
    int page,
    int pageSize,
    CancellationToken cancellationToken);

Task<LemmaSummaryDto?> GetLemmaSummaryAsync(
    int id,
    CancellationToken cancellationToken);

Task<PagedResult<LemmaWordItemDto>?> GetLemmaWordsAsync(
    int id,
    LemmaWordKind wordKind,
    int page,
    int pageSize,
    CancellationToken cancellationToken);

Task<PagedResult<LemmaAyahMatchDto>?> GetLemmaAyahMatchesAsync(
    int id,
    int page,
    int pageSize,
    CancellationToken cancellationToken);

Task<LemmaSurahsResponse?> GetLemmaMentionedSurahsAsync(
    int id,
    CancellationToken cancellationToken);

Task<LemmaMissingSurahsResponse?> GetLemmaMissingSurahsAsync(
    int id,
    CancellationToken cancellationToken);

Task<LemmaStemsResponse?> GetLemmaStemsAsync(
    int id,
    CancellationToken cancellationToken);
```

## `IStemsReader`

```csharp
Task<PagedResult<StemListItemDto>> GetStemsPageAsync(
    string? search,
    StemSort sort,
    int page,
    int pageSize,
    CancellationToken cancellationToken);

Task<StemSummaryDto?> GetStemSummaryAsync(
    int id,
    CancellationToken cancellationToken);

Task<PagedResult<StemWordItemDto>?> GetStemWordsAsync(
    int id,
    StemWordKind wordKind,
    int page,
    int pageSize,
    CancellationToken cancellationToken);

Task<PagedResult<StemAyahMatchDto>?> GetStemAyahMatchesAsync(
    int id,
    int page,
    int pageSize,
    CancellationToken cancellationToken);

Task<StemSurahsResponse?> GetStemMentionedSurahsAsync(
    int id,
    CancellationToken cancellationToken);

Task<StemMissingSurahsResponse?> GetStemMissingSurahsAsync(
    int id,
    CancellationToken cancellationToken);

Task<StemLemmasResponse?> GetStemLemmasAsync(
    int id,
    CancellationToken cancellationToken);
```

`null` from a selected-resource read means the requested positive identity does not exist. Empty
detail collections for an existing identity are successful non-null responses.

## Validation Ownership

Application handlers validate:

- positive resource ID;
- supported sort key;
- supported word kind;
- positive bounded page/page size;
- expected not-found result.

The API performs binding only and maps handler outcomes:

- success → `200`;
- invalid input → `400`;
- unknown positive identity → `404`.

## Reader Semantics

### Catalogue summaries

- Build bounded complete summary lists with `AsNoTracking` projections.
- Cache complete summary lists; apply normalized search, deterministic sort, and page over the cached
  list.
- List reads return counts and summary relationships only. They never load words, ayahs, surah lists,
  related lists, or full type distributions per row.

### Type derivation

- Group matching morphology rows by `head_pos`.
- Join existing POS labels.
- Count rows and capture earliest Quran word coordinates.
- Order count descending, then earliest Mushaf occurrence.
- Return the first as dominant; summary reads return the full ordered distribution.

### Lemma derivation

- Match `quran_word_morphology.lemma_id == id`.
- Root summary comes from the lemma's owned root relationship.
- Related stems are distinct non-null `stem_id` values with scoped counts.

### Stem derivation

- Match `quran_word_morphology.stem_id == id`.
- Dominant lemma and root are independent co-occurrence rankings.
- Related lemmas are distinct non-null `lemma_id` values with scoped counts.
- Missing root/lemma produces null fields, never inferred data or not-found.

### Word reads

- Group by requested unique-word identity.
- `simple` uses the simple identity; `tashkeel` uses the tashkeel identity.
- Return stored display text and occurrence count scoped to selected lemma/stem.

### Ayah reads

1. Select distinct matched ayah IDs and page them.
2. Batch-load ordered readable words for the selected ayah page.
3. Batch-load/build exact matched Quran word IDs for the selected resource.
4. Return one ayah DTO per ayah with `verseKey` and `pageNumber`.

No per-ayah query loop and no string matching.

### Surah reads

- Mentioned: distinct matching surahs with scoped occurrence counts.
- Missing: complement against the authoritative 114-surah catalogue.

## Cache Decorators and DI

- `CachedLemmasReader` decorates `EfLemmasReader`.
- `CachedStemsReader` decorates `EfStemsReader`.
- Use existing shared `IMemoryCache`.
- Add resource-specific dependency injection modules called from Infrastructure composition.
- Cache keys are bounded by resource identity, known view, kind, page, and size.
- No raw search text in retained keys.

## Outcomes and Logging

Each use case has focused query, handler, and discriminated outcome files, mirroring Roots:

- Success(data)
- InvalidId / InvalidSort / InvalidKind / InvalidPaging as applicable
- NotFound

Handlers log once at the Application boundary. They do not log Quran/lexical values or raw search.
Infrastructure does not duplicate routine handler logs.

## Mushaf Reader Contract Change

`EfWordAnalysisReader` already loads lemma and stem entities when relationships exist. Map those entity
IDs into the additive `WordMorphologyLemma.Id` and `WordMorphologyStem.Id` response fields. Do not add
an explorer lookup to the Mushaf reader and do not change stored morphology.

## Data Safety

- Every EF query is read-only and `AsNoTracking`.
- No Domain/Application contract exposes EF types.
- No database writes, migrations, importer calls, or source correction.
- No invented Quran, lemma, stem, root, or POS data.
- Quran highlighting is by `quran_words.id`.
- DTO IDs exist for navigation only and are not user-facing labels.
