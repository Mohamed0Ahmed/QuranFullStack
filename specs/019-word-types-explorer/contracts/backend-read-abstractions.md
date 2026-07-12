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

    // Feature 022 — unified table-view-tabs endpoint. tableView=Words wraps the same rows
    // GetRowsAsync would return (kind:"word"); Roots/Stems/Lemmas return grouped rows keyed by the
    // numeric dimension ID, grouped and counted before pagination.
    Task<PagedResult<WordTypeTableRowDto>> GetTableRowsAsync(
        WordTypeFilter filter,
        WordTypeTableView tableView,
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

    // Feature 023 — grouped (root/stem/lemma) scoped summary. Returns null when the positive dimension
    // ID does not exist in the supplied scope. Membership/counts derive from head-level
    // quran_word_morphology only (never the segments table).
    Task<WordTypeGroupedSummaryDto?> GetGroupedSummaryAsync(
        WordTypeGroupedSelection selection,
        CancellationToken cancellationToken);
}
```

`null` from selected-row reads means the positive row identity does not resolve to a row-context under
the supplied filter/context. Empty pages/lists for an existing row-context are successful non-null
responses. For `GetGroupedSummaryAsync`, `null` means the positive `DimensionId` has no rows in the
supplied scope (a scoped-group 404); the paged grouped reads added by Tasks 2–3 return a non-null empty
page with the correct `TotalCount` for an out-of-range page of an existing group.

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

### `WordTypeTableView` (Feature 022 — table-view tabs)

Selects the aggregation level for `GetTableRowsAsync` / `GET .../word-types/table`:

```text
Words | Roots | Stems | Lemmas
```

- Missing/blank value defaults to `Words`. An unrecognized non-blank value is a controlled failure
  (`InvalidTableView`), not a silent fallback — same contract shape as `WordTypeSortParser`.
- `Roots`/`Stems`/`Lemmas` group the same scoped occurrence base as `Words` by the numeric
  `root_id`/`stem_id`/`lemma_id`, excluding null dimension IDs.

### `WordTypeGroupedDimensionKind` + `WordTypeGroupedSelection` (Feature 023 — grouped details)

Addresses one grouped root/stem/lemma dimension for a scoped detail read:

```text
WordTypeGroupedDimensionKind: Root | Stem | Lemma
  - Parser accepts only the PLURAL route keys roots|stems|lemmas (unknown/blank → controlled failure).
  - ToRouteKey() → plural (roots|stems|lemmas); ToDtoKind() → singular (root|stem|lemma).

WordTypeGroupedSelection(Kind, DimensionId, Filter)
  - DimensionId: numeric root_id/stem_id/lemma_id; IsValid ⇔ DimensionId > 0.
  - Filter: the identical five-field WordTypeFilter scope the selected table row carried.
```

- The selection reuses the **identical** scoped occurrence `base` as `GetTableRowsAsync`; the summary
  restricts that base to the single allowlisted numeric column (`root_id`/`stem_id`/`lemma_id = @dimensionId`)
  and computes `COUNT(*)`, `COUNT(DISTINCT ayah_id)`, `COUNT(DISTINCT surah_number)`, plus `MIN(text)` for
  display. The three counts and `displayText` are identical to the selected E2b grouped row.
- `WordTypeGroupedSummaryDto` carries the **singular** `kind` discriminator; the text field is
  projection-only display and is never the membership predicate. Null dimensions and markers are excluded.

## Handler and Outcome Pattern

Application handlers own input validation and map failures to controlled outcomes. Controllers only
bind HTTP values, call handlers, and map outcomes to `ApiResponse<T>`.

Required handler groups:

```text
GetWordTypeTree
GetWordTypeRows
GetWordTypeTable
GetWordTypeSummary
GetWordTypeAyahs
GetWordTypeSurahs
GetWordTypeGroupedSummary   (Feature 023; grouped words/ayahs/surahs handlers added by Tasks 2–4)
```

Required outcome categories:

- `Success`
- `InvalidFilter`
- `InvalidPaging`
- `InvalidSort`
- `InvalidTableView` (`GetWordTypeTable` only — unrecognized non-blank `tableView`)
- `InvalidIdentity`
- `NotFound` for valid row identity that does not resolve
- Grouped-detail handlers (Feature 023) validate in a fixed order and map to
  `InvalidKind` → `InvalidId` → `InvalidFilter` → reader result (`Success`/`NotFound`). `InvalidKind`
  covers an unrecognized non-blank route `kind`; `InvalidId` covers `DimensionId ≤ 0`.

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
- **Grouped table reads (Feature 022)**: `GetTableRowsAsync` for `Roots`/`Stems`/`Lemmas` reuses the
  identical scoped occurrence base as `GetRowsAsync` (same type/child/case/tense/voice predicates,
  `!IsAyahMarker`, non-null tashkeel identity), grouped by the numeric dimension ID with nulls
  excluded. **Grouping and total counting happen before pagination.**
  `COUNT(DISTINCT dimension_id)` over the scoped base is the grouped `totalCount`.
- Grouped counts (`occurrencesCount`/`ayahsCount`/`surahsCount` per dimension) are a **third**
  occurrence-count family, separate from both the tree/node row-count family (§4.1 of
  `../data-model.md`) and the Roots/Lemmas/Stems explorers' own global, unscoped,
  segment/`words_count`-backed aggregates. Grouped counts must never be derived from those explorer
  aggregates.
- Grouped `alpha` sort reuses the Roots explorer's Arabic fold (`RootsListDerivation.ArabicFoldFrom`/
  `ArabicFoldTo`) with `COLLATE "C"` ordinal collation, so grouped alphabetical order stays consistent
  with the standalone Roots/Lemmas/Stems explorers. All grouped sorts tie-break on the numeric
  dimension ID for deterministic pages.
- **Grouped detail reads (Feature 023)**: `GetGroupedSummaryAsync` selects from the same scoped `base`
  CTE as the grouped table reads and applies the allowlisted numeric predicate
  `root_id|stem_id|lemma_id = @dimensionId` before grouping — **head-level `quran_word_morphology`
  only**. `quran_word_morphology_segments` is never joined, so a segment-only dimension can never surface
  and can never displace a word's head IDs. The membership predicate is always the numeric ID; the text
  columns are projection-only and never filter membership. Kept in a size-split partial
  (`EfWordTypesReader.GroupedDetails.cs` + `.GroupedDetails.Sql.cs`) so the primary reader stays under its
  threshold.

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
wordtypes:table:{filter-hash}:view:{tableView}:sort:{sort}:p{page}:s{pageSize}
wordtypes:summary:{identity-hash}
wordtypes:ayahs:{identity-hash}:p{page}:s{pageSize}
wordtypes:surahs:{identity-hash}
wordtypes:grouped:{kind}:summary:{scope-hash}      (Feature 023; grouped words/ayahs/surahs keys added by Tasks 2–4)
```

The `wordtypes:table:` key **must include `tableView`** — switching tabs must never return another
view's cached rows. The `wordtypes:rows:` key (E2 `/words`) is untouched and stays independent. The
`wordtypes:grouped:` keys fold the numeric dimension ID plus the five scope fields into `{scope-hash}`
and expose only kind/view labels in the readable prefix, so different kinds/scopes never cross-serve.

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
- **Table-view tabs (Feature 022)** — `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesTableReadTests.cs`:
  - `tableView=roots|stems|lemmas` returns one row per distinct non-null dimension ID; `displayText`
    matches the dimension text; `kind` matches the view.
  - Grouped counts under an active grammatical filter (e.g. verb `tense=past`) equal the occurrences of
    that scope only, from the same scoped base as E2.
  - Null-dimension occurrences produce no grouped row: grouped `totalCount` equals the distinct
    non-null dimension-ID count for that scope (never compared to the `/words` `totalCount`), and the
    occurrence-sum identity holds (`Σ occurrencesCount` over grouped pages + null-dimension occurrences
    = `Σ occurrencesCount` over `/words` pages for the same scope).
  - Sorting/tie-breakers are deterministic for every `sort` value (metric DESC → first-Mushaf →
    dimension ID; `mushaf-order` → dimension ID; `alpha` → fold + ordinal collation → dimension ID).
  - Grouping and total counting happen before pagination; page 2 continues the deterministic order;
    an out-of-range page returns empty items with the correct `totalCount`.
  - `roots`/`stems`/`lemmas` for the same filter/sort/page produce different cache keys and never
    cross-serve.
  - Missing `tableView` defaults to `words`; an unknown value returns the controlled `InvalidTableView`
    400.
  - `/words` (E2) stays unchanged; `/table?tableView=words` returns the same rows plus `kind:"word"`.
