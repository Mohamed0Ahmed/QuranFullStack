# Contract: Backend Read Abstractions

**Feature**: 019 — Word Types Explorer  
**Layer**: Application.Abstractions + Application handlers + Infrastructure readers.  
**Nature**: Read-only. No entity mutation, importer call, migration, or data repair happens through this feature.

## Boundary

Add a resource-specific read contract under:

```text
Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/
```

Conceptual shape:

```csharp
public interface IWordTypesReader
{
    Task<WordTypeTreeDto> GetTreeAsync(CancellationToken cancellationToken);

    Task<PagedResult<WordTypeRowDto>> GetRowsAsync(
        WordTypeFilter filter,
        WordTypeSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<WordTypeSummaryDto?> GetSummaryAsync(
        WordTypeRowIdentity identity,
        CancellationToken cancellationToken);

    Task<PagedResult<WordTypeAyahMatchDto>?> GetAyahMatchesAsync(
        WordTypeRowIdentity identity,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<WordTypeSurahsResponse?> GetSurahsAsync(
        WordTypeRowIdentity identity,
        CancellationToken cancellationToken);
}
```

`null` from selected-row reads means the positive row identity does not resolve to a row-context under
the supplied filter/context. Empty pages/lists for an existing row-context are successful non-null
responses.

## Value Objects

### `WordTypeFilter`

Represents the active tree node plus secondary filter for row/detail reads. The tree read itself is unscoped in v1 and does not take this filter:

```text
Type: noun | verb | particle | inl
ChildCode: noun head POS code or verb tense child; optional
Case: nominative | accusative | genitive | null-filter | all; nominal only
Tense: past | present | imperative | all; verb only
Voice: active | passive | all; verb only
```

Rules:

- Missing `Type` defaults to `noun`.
- `ChildCode` must belong to the selected parent. For `noun`, valid values are the current noun-category POS codes returned by the tree endpoint.
- `Case` is valid only for `noun`.
- `Tense` and `Voice` are valid only for `verb`.
- `particle` and `inl` reject case/tense/voice filters.
- `inl` rejects child nodes because it is a leaf.

### `WordTypeRowIdentity`

Addresses one row context:

```text
TashkeelWordId: positive int
ContextCode: required string
Case/Tense/Voice: the active secondary values that participated in the row identity
```

The identity must reproduce the same row context returned from `GetRowsAsync`; it must never widen to
all usages of the displayed word. Missing `ContextCode` for a selected row is invalid unless the
implementation can prove the active node uniquely pins the context.

### `WordTypeSort`

Allowed catalogue sorts:

```text
occurrences
ayahs
surahs
mushaf-order
alpha
```

Default: `occurrences` descending, with Mushaf-order and identity tie-breaks for deterministic pages.

## Handler and Outcome Pattern

Application handlers own input validation and map failures to controlled outcomes. Controllers only
bind HTTP values, call handlers, and map outcomes to `ApiResponse<T>`.

Required handler groups:

```text
GetWordTypeTree
GetWordTypeRows
GetWordTypeSummary
GetWordTypeAyahs
GetWordTypeSurahs
```

Required outcome categories:

- `Success`
- `InvalidFilter`
- `InvalidPaging`
- `InvalidSort`
- `InvalidIdentity`
- `NotFound` for valid row identity that does not resolve

## Query Rules

- Every query joins `quran_words` and filters `!IsAyahMarker`.
- Tree and row predicates read `quran_word_morphology` word-level fields only.
- Segment/prefix/suffix morphology tables are not read for type buckets, secondary filters, or counts.
- Particle parent predicate must include `HeadPos <> "INL"`.
- Tree counts are distinct word-context row counts using the grouping key from `data-model.md`, unscoped by case/tense/voice in v1.
- Table/detail counts are occurrence-level aggregates scoped to the exact row context.
- POS codes outside noun, verb, particle-without-INL, and INL are excluded from v1 buckets and must not be silently reclassified.
- Do not use `quran_words_unique_tashkeel.occurrences_count`, `ayahs_count`, or `surahs_count` for
  filter-scoped counts.
- POS labels come from `quran_pos_tags.ArabicLabel` for child/subtype labels.
- Main labels and secondary-option labels may be static feature-owned Arabic labels.

## Enrichment Rules

- Root enrichment ships in v1 by reusing the existing primary-root winner pattern and returning a value where source data provides a root.
- Lemma/stem enrichment may ship if mirrored winner queries remain low-risk; otherwise fields return
  null and the frontend displays the neutral placeholder.
- Winner tie-break: occurrence count descending, then earliest Mushaf occurrence, then stable identity.
- Null root/lemma/stem never removes a row.

## Caching

Use a resource-specific cache decorator and keys under a `wordtypes:` namespace. Do not reconfigure
global cache services.

Suggested keys:

```text
wordtypes:tree
wordtypes:rows:{filter-hash}:sort:{sort}:p{page}:s{pageSize}
wordtypes:summary:{identity-hash}
wordtypes:ayahs:{identity-hash}:p{page}:s{pageSize}
wordtypes:surahs:{identity-hash}
```

Do not include raw Quran text, word text, or raw search text in cache keys or logs.

## Logging

Log at Application handler boundaries using structured fields only:

```text
feature = WordTypes
operation
type
childCode
hasCaseFilter
hasTenseFilter
hasVoiceFilter
pageNumber
pageSize
sort
tashkeelWordId
contextCode
totalCount
itemCount
cacheResult
elapsedMs
reason
```

Forbidden in logs: Quran text, displayed word text, raw search text, root/lemma/stem text, response
payloads, SQL, connection details, and large ID/location lists.

## Required Backend Tests

- `PRO` pre-implementation gate query documented or automated: `PRO` must be category `particle` and
  label `حرف نهي` before accepting data-correctness results.
- Tree contains exactly four main types; particle count excludes `INL`; out-of-bucket POS rows are excluded from all four buckets.
- Child node count equals table `TotalCount` for that same node only when no secondary filter is applied.
- A known multi-context displayed word returns separate rows with separate counts.
- Nominal case filter applies only to noun selections; verb tense/voice apply only to verb selections.
- Marker words contribute zero rows/counts.
- `contextCode`-scoped summary/ayahs/surahs do not widen to all usages of the word.
- Invalid filter, paging, sort, and row identity map to controlled outcomes.
- Cache hit does not re-query expensive grouped reads when using the same filter/page.
