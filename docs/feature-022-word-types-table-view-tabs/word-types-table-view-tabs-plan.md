# Word Types Explorer — Table View Tabs Plan (implementation-ready)

Feature: 022 — Word Types table-view tabs
Branch: `023-quran-search-and-words-ux` (current)
Scope: **planning/documentation only.** No production or test code is written by this document.
Incorporates: latest Codex repository review — verdict **CHANGES REQUIRED** — and a full re-inspection of the shipped Backend and Frontend Word Types architecture.

This plan is written so another implementation agent can execute it **without rediscovering the
architecture**. Every referenced path below was verified against the repository; each new file is
explicitly labelled **(new)**.

---

## 1. Executive summary

The Word Types Explorer (`/dashboard/words/types`) shows a paginated table of **word-context rows**
for the selected type filter. This feature adds a tab row above the table so the same filtered scope
can be viewed at four aggregation levels:

| Tab (code) | Arabic label | Table grain |
|------------|--------------|-------------|
| `words`    | كلمات        | one row per word-context (current behavior) |
| `roots`    | جذور         | one row per **root** (`rootId`) |
| `stems`    | أصول         | one row per **stem** (`stemId`) — أصول صرفية |
| `lemmas`   | صيغ          | one row per **lemma** (`lemmaId`) — صيغ معجمية |

Arabic tab order (RTL): **كلمات | جذور | أصول | صيغ**.

Grouping is **not** a frontend concern: the table is server-paginated and server-sorted, so grouping
the currently loaded page would corrupt counts, ordering, and pagination. Grouped views are backed by
the read model, grouped **before** sorting/pagination, and served by a **new** endpoint.

URL parameter: `tableView=words|roots|stems|lemmas`. `view` is **not** reused (the details panel
already owns `view=ayahs|surahs`).

---

## 2. Locked decisions (from the review — do not re-open at implementation time)

1. Add **`GET /api/words/word-types/table`** as the unified list endpoint for all four views.
2. Preserve **`GET /api/words/word-types/words`** unchanged (compatibility; existing deep links).
3. Use **numeric stable IDs** for grouped identity: `rootId`, `stemId`, `lemmaId`.
4. Preserve the **existing composite word-row identity** for the word variant
   (`tashkeelWordId` + `contextCode` + `case` + `tense` + `voice`).
5. **Never** use Arabic display text as identity.
6. Grouped views use the **filtered Word Types occurrence base** (the `base` CTE scoped by
   type/child/case/tense/voice).
7. **Do not** reuse the segment/`words_count`-backed Roots/Lemmas/Stems Explorer counts.
8. Grouping and total counting happen **before** pagination.
9. Grouped-row **details** stay **out of MVP**.
10. Grouped rows and counts are **noninteractive** in MVP.
11. In grouped views, **hide** the word details panel and **expand** the table to full width.
12. Exclude **null** `rootId`/`stemId`/`lemmaId` from grouped views in MVP.
13. Existing URLs without `tableView` default to **`words`**.
14. When `tableView !== 'words'`, word-selection URL state is **cleared or safely ignored**.
15. **Both** Backend and Frontend cache keys **must include `tableView`**.

### 2.1 Terminology lock (critical) — align Word Types with the rest of the workspace

The **canonical workspace terminology is already correct everywhere except Word Types (019)**.
Evidence (verified):

- `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs` — the Roots/Lemmas/Stems explorers use:
  - **lemma = الصيغة المعجمية** (`LemmasListLoaded = "تم تحميل الصيغ المعجمية"`, `RootLemmasLoaded = "… الصيغ المعجمية للجذر"`).
  - **stem = الأصل الصرفي** (`StemsListLoaded = "تم تحميل الأصول الصرفية"`, `RootStemsLoaded = "… الأصول الصرفية للجذر"`).

The Word Types feature alone reversed this. So the locked mapping is:

| Dimension | Correct Arabic (full) | Short label (tab / column) |
|-----------|-----------------------|----------------------------|
| root      | الجذر                 | جذور / الجذر |
| **stem**  | **الأصل الصرفي / الأصول الصرفية** | **أصول / الأصل** |
| **lemma** | **الصيغة المعجمية / الصيغ المعجمية** | **صيغ / الصيغة** |

**Currently wrong (must be corrected as part of this feature):**

- `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.labels.ts`
  `WORD_TYPES_TABLE_HEADERS`: `stem: 'الصيغة'` and `lemma: 'الأصل'` are **swapped**.
  Correct → `stem: 'الأصل'`, `lemma: 'الصيغة'` (this is a literal swap of the two strings).
- `specs/019-word-types-explorer/contracts/word-types-api.md` (E2/E3 comments `lemmaText // الأصل`,
  `stemText // الصيغة`).
- `specs/019-word-types-explorer/contracts/frontend-routing-state.md` (columns line and the
  “`الصيغة` and `الأصل` render placeholder” paragraph).
- `specs/019-word-types-explorer/data-model.md` (§1.1 `LemmaId — الأصل`, `StemId — الصيغة`; §1.5 and
  §5 “الأصل (lemma) / الصيغة (stem)”).
- Any Word Types test that asserts the reversed header strings (see §14).

This is a **label/text correction only** — the numeric IDs, columns, and identity semantics are
unchanged. Because it aligns Word Types with the already-correct Roots/Lemmas/Stems terminology, it is
low-risk, but it **is** a visible UI change and a contract-comment change, so it must be applied in the
same change and covered by tests.

---

## 3. Verified Backend architecture (source of truth)

All paths below exist unless labelled **(new)**.

### 3.1 API boundary

- `Backend/api/QuranDashboard.Api/Controllers/Words/WordTypesController.cs`
  Route base `api/words/word-types`. Existing actions: `GET tree`, `GET words`,
  `GET words/{tashkeelWordId:int}`, `…/ayahs`, `…/surahs`. Constructor injects the five handlers.
- `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`
  Existing constants `WordTypesTreeLoaded`, `WordTypesRowsLoaded`, `WordTypesInvalidFilter`,
  `WordTypesInvalidSort`, `WordTypesInvalidPaging`, … Arabic default language; identifiers English.

### 3.2 Application.Abstractions (contracts + value objects)

- `…/Quran/Words/WordTypes/IWordTypesReader.cs` — reader interface.
- `…/WordTypes/WordTypeFilter.cs` — `(Type, ChildCode, Case, Tense, Voice)`.
- `…/WordTypes/WordTypeSort.cs` — enum `Occurrences|Ayahs|Surahs|MushafOrder|Alpha` + `WordTypeSortKeys`
  + `WordTypeSortParser`.
- `…/WordTypes/WordTypeRowIdentity.cs` — `(TashkeelWordId, ContextCode, Case?, Tense?, Voice?)`.
- `…/WordTypes/Responses/WordTypeRowDto.cs` — the existing word row DTO (13 fields).
- `…/WordTypes/Responses/WordTypeLabelDto.cs`, `WordTypeTreeDto.cs`, `WordTypeSummaryDto.cs`,
  `WordTypeAyahMatchDto.cs`, `WordTypeSurahsResponse.cs`.

### 3.3 Application (handlers)

- `…/Quran/Words/WordTypes/Queries/GetWordTypeRows/` — `GetWordTypeRowsQuery.cs`,
  `GetWordTypeRowsHandler.cs`, `GetWordTypeRowsOutcome.cs` (`Success|InvalidFilter|InvalidSort|InvalidPaging`).
- `…/Queries/WordTypesHandlerValidation.cs` — `NormalizeType`, `IsValidFilter`, `IsValidChildCode`,
  `IsValidSecondaryFilter`, `IsValidPaging`, `DefaultSort`, page bounds (`MaxPageSize = 100`).
- Handler registration: `Backend/application/QuranDashboard.Application/DependencyInjection.cs`
  lines 103–107 (`AddScoped<GetWordType*Handler>()`).

### 3.4 Infrastructure (read model + cache)

- `…/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs` — reader; `GetRowsAsync` builds a
  `WordTypeReadContext`, counts, safe-skips, then runs raw SQL.
- `…/WordTypes/EfWordTypesReader.Sql.cs` — the raw SQL: **`BaseRowsSql(context)`** (the scoped
  occurrence base), `RowsSql`, `RowsCountSql`, `OrderBy(sort)`, `WordTypeRowSqlResult`,
  `WordTypeReadContext` (with `Unscoped`, `HasChildCode`, `HasCaseFilter`, …).
- `…/WordTypes/WordTypeIdentityMatcher.cs`, `WordTypeGrouping.cs`.
- `…/Caching/Quran/Words/WordTypes/CachedWordTypesReader.cs` — cache decorator (`IMemoryCache`).
- `…/Caching/Quran/Words/WordTypes/WordTypesCacheKeys.cs` — `Rows(filter, sort, page, pageSize)`
  (SHA-256 filter hash), `Tree`, `Summary`, `Ayahs`, `Surahs`.
- `…/Caching/Quran/Words/WordTypes/WordTypesCacheEntryOptions.cs` — `Tree()`, `PagedRows()`, `Detail()`.
- DI: `…/Infrastructure/DependencyInjection/WordTypesDependencyInjection.cs` — registers
  `EfWordTypesReader` + `CachedWordTypesReader` as `IWordTypesReader`.

### 3.5 The `base` CTE — the reusable scoped occurrence set (verified `BaseRowsSql`)

`BaseRowsSql(context)` already emits exactly the columns grouped views need, scoped by
type/child/case/tense/voice, with `!IsAyahMarker` and non-null tashkeel identity:

```
quran_word_id, ayah_id, surah_number, ayah_number, word_number, tashkeel_word_id,
display_text, head_pos, is_verb, verb_tense, verb_voice, case_feature,
root_id, root_text, lemma_id, lemma_text, stem_id, stem_text, pos_label, pos_category
```

Grouped views reuse this identical base, so they inherit the scope and the marker/identity rules for
free. **`first_word_order_in_mushaf` = `MIN(quran_word_id)`** is the established “first scoped Mushaf
occurrence” expression (see `RowsSql` / `OrderBy`). Reuse it verbatim.

### 3.6 Count-basis contrast (why grouped counts must come from `base`)

- **Word Types** counts are morphology-occurrence based: `COUNT(*)`, `COUNT(DISTINCT ayah_id)`,
  `COUNT(DISTINCT surah_number)` over the scoped `base`.
- **Roots/Lemmas/Stems Explorers** (`EfRootsReader.LoadWholeSummaryAsync` etc.) use **global,
  unscoped, segment/`words_count`-backed** aggregates (e.g. `quran_roots.words_count`,
  `COUNT(DISTINCT unique_*_word_id)`). These are a **different count family** and are **globally scoped**.

**Grouped table views must derive counts from the scoped `base` CTE, never from the dimension
explorers’ aggregates.** (Locked decisions 6–7.)

### 3.7 Arabic normalization/collation rule (for `alpha` sort) — reuse the dimension explorers’ fold

Verified in `…/Reads/Quran/Words/Roots/RootsListDerivation.cs` and used by
`EfRootsReader.LoadWholeSummaryAsync`:

```
ArabicFoldFrom = "أإآٱؤئةىي"
ArabicFoldTo   = "ااااواهيي"
normalized = replace(translate(lower(text), @foldFrom, @foldTo), ' ', '')   -- computed in SQL
order      = OrderBy(normalized, Ordinal) then Id                            -- ordinal, id tie-break
```

Grouped `alpha` sort uses this **exact** fold on the dimension display text, ordered by the normalized
value with byte/ordinal collation (`COLLATE "C"` in SQL to match the explorers’ `StringComparer.Ordinal`),
tie-broken by numeric dimension ID. (Locked decision 6 of §5-review; see §7.4.)

> Note: the current word-rows `alpha` sort orders by raw `display_text`. Grouped views intentionally
> use the **normalized** fold so they stay consistent with the Roots/Lemmas/Stems explorers. This is a
> documented, deliberate difference, not an oversight.

---

## 4. Final API contract

### 4.1 New — `GET /api/words/word-types/table`

```
GET api/words/word-types/table
    ?tableView={words|roots|stems|lemmas}     (optional; default words)
    &type={noun|verb|particle|inl}            (default noun)
    &childCode={head_pos | tense}             (optional — leaf scope)
    &case={nominative|accusative|genitive|null}   (noun only)
    &tense={past|present|imperative}          (verb only)
    &voice={active|passive}                   (verb only)
    &sort={occurrences|ayahs|surahs|mushaf-order|alpha}   (default occurrences)
    &page={n}                                 (default 1)
    &pageSize={n}                             (default 25, max 100)
```

Returns `ApiResponse<PagedResult<WordTypeTableRowDto>>` (discriminated rows, §5). Grouping, total
counting, sorting, and pagination all happen in the read model.

Controlled outcomes → HTTP mapping (mirror `GetRows`):

| Outcome | HTTP | Message constant |
|---------|------|------------------|
| `Success` | 200 | `ApiMessages.WordTypesTableLoaded` **(new)** |
| `InvalidTableView` **(new)** | 400 | `ApiMessages.WordTypesInvalidTableView` **(new)** |
| `InvalidFilter` | 400 | `ApiMessages.WordTypesInvalidFilter` |
| `InvalidSort` | 400 | `ApiMessages.WordTypesInvalidSort` |
| `InvalidPaging` | 400 | `ApiMessages.WordTypesInvalidPaging` |

Rules:
- Missing/blank `tableView` → `words`. Unknown `tableView` → `InvalidTableView` (controlled 400).
- `tableView=words` returns the **word variant** and is byte-for-byte semantically equal to `E2 /words`
  plus a `kind:"word"` discriminator.
- Grouped views exclude null dimension IDs (locked 12); their `totalCount` is the grouped-row count
  **after** grouping and **before** pagination.

### 4.2 Preserved — `GET /api/words/word-types/words` (unchanged)

`E2` stays exactly as today: `ApiResponse<PagedResult<WordTypeRowDto>>`, same params, same DTO. The
new UI list calls `/table`; `/words` remains for existing shareable deep links / external consumers and
is covered by a compatibility test (§13).

### 4.3 Detail endpoints (unchanged; word-row only)

`GET words/{tashkeelWordId}`, `…/ayahs`, `…/surahs` are unchanged and remain **word-variant only**.
Grouped-row details are out of MVP (locked 9).

---

## 5. Discriminated DTO contract (replaces the flat nullable DTO)

### 5.1 Backend — polymorphic records **(new file)**

`Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/Responses/WordTypeTableRowDto.cs` **(new)**

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(WordTableRowDto), "word")]
[JsonDerivedType(typeof(RootTableRowDto), "root")]
[JsonDerivedType(typeof(StemTableRowDto), "stem")]
[JsonDerivedType(typeof(LemmaTableRowDto), "lemma")]
public abstract record WordTypeTableRowDto;

// Word variant — carries the EXISTING full composite identity (locked 4) directly in the payload:
// TashkeelWordId + ContextCode + Case + Tense + Voice. The API is the single source of the identity;
// the frontend never re-stamps case/tense/voice to complete it.
public sealed record WordTableRowDto(
    int TashkeelWordId,
    string ContextCode,
    string? Case,
    string? Tense,
    string? Voice,
    string DisplayText,
    string TypeCode,
    WordTypeLabelDto TypeLabel,
    WordTypeLabelDto BroadLabel,
    string? CaseOrFeature,
    string? RootText,
    string? LemmaText,
    string? StemText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount) : WordTypeTableRowDto;

// Grouped variants — numeric stable ID identity (locked 3, 5). No details in MVP (locked 9).
public sealed record RootTableRowDto(
    int RootId, string DisplayText,
    int OccurrencesCount, int AyahsCount, int SurahsCount) : WordTypeTableRowDto;

public sealed record StemTableRowDto(
    int StemId, string DisplayText,
    int OccurrencesCount, int AyahsCount, int SurahsCount) : WordTypeTableRowDto;

public sealed record LemmaTableRowDto(
    int LemmaId, string DisplayText,
    int OccurrencesCount, int AyahsCount, int SurahsCount) : WordTypeTableRowDto;
```

Serialization notes (verify at implementation):
- The API uses ASP.NET Core default System.Text.Json. Declaring the paged item type as the abstract
  base (`PagedResult<WordTypeTableRowDto>`) makes STJ emit the `kind` discriminator automatically; no
  custom converter is needed.
- `kind` values are **singular** (`word|root|stem|lemma`); the URL param / view is **plural**
  (`words|roots|stems|lemmas`).
- The word variant carries `Case`/`Tense`/`Voice` alongside `TashkeelWordId`/`ContextCode` so the
  **full composite identity is complete in the payload**. It is a superset of the preserved
  `WordTypeRowDto` (`/words`): the `/table?tableView=words` word row = `WordTypeRowDto` fields + `kind`
  + the three secondary-identity fields. `/words` and its `WordTypeRowDto` stay unchanged.

### 5.2 Frontend — TypeScript discriminated union **(edit models)**

`Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.models.ts`

```ts
export type WordTypeTableView = 'words' | 'roots' | 'stems' | 'lemmas';

export const WORD_TYPE_TABLE_VIEWS =
  ['words', 'roots', 'stems', 'lemmas'] as const satisfies readonly WordTypeTableView[];
export const DEFAULT_WORD_TYPE_TABLE_VIEW: WordTypeTableView = 'words';

export function isWordTypeTableView(value: unknown): value is WordTypeTableView {
  return (WORD_TYPE_TABLE_VIEWS as readonly string[]).includes(value as string);
}

// WordTypeRowIdentity = { tashkeelWordId, contextCode, case, tense, voice } — all API-sourced.
// The word variant already carries the complete composite identity; NO frontend stamping.
export interface WordTableRowDto extends WordTypeRowIdentity {
  kind: 'word';
  displayText: string;
  typeCode: string;
  typeLabel: WordTypeLabelDto;
  broadLabel: WordTypeLabelDto;
  caseOrFeature: string | null;
  rootText: string | null;
  lemmaText: string | null;
  stemText: string | null;
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
}
export interface RootTableRowDto  { kind: 'root';  rootId: number;  displayText: string; occurrencesCount: number; ayahsCount: number; surahsCount: number; }
export interface StemTableRowDto  { kind: 'stem';  stemId: number;  displayText: string; occurrencesCount: number; ayahsCount: number; surahsCount: number; }
export interface LemmaTableRowDto { kind: 'lemma'; lemmaId: number; displayText: string; occurrencesCount: number; ayahsCount: number; surahsCount: number; }

export type WordTypeTableRowDto =
  | WordTableRowDto | RootTableRowDto | StemTableRowDto | LemmaTableRowDto;
```

- `ParsedWordTypesQuery` gains `tableView: WordTypeTableView`.
- `WordTypesListState.rows` becomes `PagedResultDto<WordTypeTableRowDto> | null`.
- `WORD_TYPES_QUERY_KEYS` gains `tableView: 'tableView'`.
- Existing `WordTypeRowDto` stays (still returned by the preserved `/words` and used by detail types).

---

## 6. Read-model query pipeline (exact semantics)

The reader gains one method; the SQL builder gains a grouped branch. **Grouping and total counting
happen before pagination** (locked 8).

### 6.1 Reader interface — add one method

`IWordTypesReader.cs`:

```csharp
Task<PagedResult<WordTypeTableRowDto>> GetTableRowsAsync(
    WordTypeFilter filter,
    WordTypeTableView tableView,
    WordTypeSort sort,
    int page,
    int pageSize,
    CancellationToken cancellationToken);
```

`EfWordTypesReader.GetTableRowsAsync` dispatches:
- `tableView == Words` → call the existing word-row path (`GetRowsAsync` internals) and wrap each
  `WordTypeRowDto` as `WordTableRowDto` (`kind:"word"`), preserving `PagedResult` metadata.
- `tableView ∈ {Roots, Stems, Lemmas}` → the grouped path (§6.2).

### 6.2 Grouped path (per dimension) — pipeline

For a dimension `D ∈ {root, stem, lemma}` with columns `(D_id, D_text) ∈ {(root_id, root_text),
(stem_id, stem_text), (lemma_id, lemma_text)}`:

1. **Build the filtered occurrence base** — reuse `BaseRowsSql(context)` verbatim (type + child + case
   + tense + voice + `!IsAyahMarker` + non-null tashkeel identity). No new predicate logic.
2. **Apply the marker/type/child/case/tense/voice rules** — already inside `BaseRowsSql`; nothing to add.
3. **Group by the numeric dimension ID**, excluding nulls (locked 12):
   ```sql
   WITH base AS ( {BaseRowsSql(context)} ),
   grouped AS (
       SELECT
           {D_id}                            AS dimension_id,
           MIN({D_text})                     AS display_text,
           MIN(quran_word_id)                AS first_word_order_in_mushaf,   -- first scoped Mushaf occurrence
           COUNT(*)::int                     AS occurrences_count,
           COUNT(DISTINCT ayah_id)::int      AS ayahs_count,
           COUNT(DISTINCT surah_number)::int AS surahs_count,
           replace(translate(lower(MIN({D_text})), @foldFrom, @foldTo), ' ', '') AS norm_text
       FROM base
       WHERE {D_id} IS NOT NULL
       GROUP BY {D_id}
   )
   SELECT dimension_id, display_text, occurrences_count, ayahs_count, surahs_count,
          first_word_order_in_mushaf
   FROM grouped
   ORDER BY {GroupedOrderBy(sort)}
   OFFSET @skip LIMIT @take
   ```
   (`@foldFrom`/`@foldTo` are `RootsListDerivation.ArabicFoldFrom`/`ArabicFoldTo`; `norm_text` is only
   needed when `sort == Alpha`.)
4. **Calculate per-group stats** — occurrences, distinct ayahs, distinct surahs, first scoped Mushaf
   occurrence (step 3).
5. **Count grouped rows (before pagination)**:
   ```sql
   WITH base AS ( {BaseRowsSql(context)} )
   SELECT COUNT(DISTINCT {D_id})::int FROM base WHERE {D_id} IS NOT NULL
   ```
   Feed this into `ReadPaging.CalculateSafeSkip(page, pageSize, totalCount)` (same paging contract as
   the word rows).
6. **Sort** with deterministic tie-breakers (§7).
7. **Paginate** with `OFFSET @skip LIMIT @take`.

Map each SQL row to `RootTableRowDto`/`StemTableRowDto`/`LemmaTableRowDto` with the numeric
`dimension_id` as `RootId`/`StemId`/`LemmaId` and `display_text` as `DisplayText`.

Parameters reuse `BuildRowsParameters`/`BuildCountParameters` (childCode + secondary filters) plus
`@skip`/`@take`, and add `@foldFrom`/`@foldTo` for `Alpha`.

### 6.3 SQL builder additions **(edit `EfWordTypesReader.Sql.cs`)**

Add, mirroring the existing style:
- `GroupedRowsSql(WordTypeReadContext context, WordTypeTableView view, WordTypeSort sort)`
- `GroupedRowsCountSql(WordTypeReadContext context, WordTypeTableView view)`
- `GroupedOrderBy(WordTypeSort sort)` (§7.4)
- A `GroupedRowSqlResult(int DimensionId, string DisplayText, int OccurrencesCount, int AyahsCount,
  int SurahsCount, int FirstWordOrderInMushaf)` record + a `ToDto(WordTypeTableView)` mapper.
- A private helper resolving `(D_id column, D_text column)` from `WordTypeTableView`.

Keep the file’s partial split (summary vs SQL) intact — do not merge (README invariant).

---

## 7. Deterministic sorting (grouped rows)

### 7.1 Metric sorts (`occurrences`, `ayahs`, `surahs`)
`<metric> DESC, first_word_order_in_mushaf ASC, dimension_id ASC`.

### 7.2 Mushaf sort (`mushaf-order`)
`first_word_order_in_mushaf ASC, dimension_id ASC`.

### 7.3 Alpha sort (`alpha`)
`norm_text COLLATE "C" ASC, dimension_id ASC`, where `norm_text` is the Roots-explorer fold
(`replace(translate(lower(text), @foldFrom, @foldTo), ' ', '')`). `COLLATE "C"` gives byte/ordinal
ordering to match the explorers’ `StringComparer.Ordinal`.

### 7.4 `GroupedOrderBy(sort)` mapping

| `sort` | `ORDER BY` |
|--------|------------|
| `Occurrences` | `occurrences_count DESC, first_word_order_in_mushaf, dimension_id` |
| `Ayahs` | `ayahs_count DESC, first_word_order_in_mushaf, dimension_id` |
| `Surahs` | `surahs_count DESC, first_word_order_in_mushaf, dimension_id` |
| `MushafOrder` | `first_word_order_in_mushaf, dimension_id` |
| `Alpha` | `norm_text COLLATE "C", dimension_id` |

All tie-breakers end at the **numeric** `dimension_id`, so pages are deterministic (locked 5).

---

## 8. Count families & documented caveats

- **Two count families stay separate** (data-model §4): tree/node counts = distinct **word-context row**
  counts; table column counts = **occurrence-level** stats. Grouped table counts are a **third** view of
  the occurrence family (grouped by dimension). Do **not** promise equality between tree counts and
  grouped table counts (locked; review #8).
- **`totalCount` is measured in different units per view — never compare them.** Words `totalCount` =
  number of **word-context rows**; grouped `totalCount` = number of **distinct non-null dimension
  IDs** (roots/stems/lemmas). These are different populations; one is not a subset-count of the other,
  so a Words-vs-grouped `totalCount` comparison is meaningless and must not be used to reason about
  null coverage.
- **Null-dimension coverage is an occurrence-sum identity, not a `totalCount` identity** (locked 12;
  review #7). Compare occurrence sums, both taken over the **same scope**:
  `Σ occurrencesCount over all grouped pages` (non-null dimension only) vs
  `Σ occurrencesCount over the Words-view pages`. The difference equals exactly the occurrences whose
  selected dimension ID is null. Document this in the READMEs and assert it as an occurrence-sum test
  (§13.4). Never “balance” the numbers by inventing a bucket.

---

## 9. Cache isolation (both layers must include `tableView`)

### 9.1 Backend
`WordTypesCacheKeys.cs` — add:

```csharp
public static string Table(WordTypeFilter filter, WordTypeTableView tableView, WordTypeSort sort, int page, int pageSize) =>
    $"wordtypes:table:{HashFilter(filter)}:view:{TableViewKey(tableView)}:sort:{SortKey(sort)}:p{page}:s{pageSize}";
```
Add a `TableViewKey` (`words|roots|stems|lemmas`). `CachedWordTypesReader.GetTableRowsAsync` uses this
key with `WordTypesCacheEntryOptions.PagedRows()`. The existing `Rows(...)` key is untouched (still
backs `/words`). Because `tableView` is in the key, switching tabs never returns another view’s rows.

### 9.2 Frontend
`state/word-types-cache.ts` — add a `table` key that includes `tableView`:

```ts
table(filter: WordTypesCacheFilter, tableView: WordTypeTableView, sort: WordTypeSort, page: number): string {
  return `wordtypes:table:${filter.type}:${filter.childCode ?? 'all'}:${filter.case}:${filter.tense}:${filter.voice}:view:${tableView}:sort:${sort}:p${page}`;
}
```
The facade uses `WordTypesCacheKeys.table(query, query.tableView, query.sort, query.page)` for the list.

---

## 10. Verified Frontend architecture (source of truth)

All paths exist unless labelled **(new)**.

- Page: `…/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.{ts,html,scss,spec.ts}`
- Table: `…/components/word-types-table/word-types-table.component.{ts,html,scss,spec.ts}`
- Details panel: `…/components/word-type-details-panel/word-type-details-panel.component.{ts,html,scss,spec.ts}`
  (RTL tab keydown reference: ArrowLeft → next, ArrowRight → previous.)
- Filter: `…/components/word-type-filter/word-type-filter.component.*`
- Existing tab pattern to mirror: `…/components/unique-words-tabs/unique-words-tabs.component.{ts,html}`
  (`role="tablist"`, `role="tab"`, `[attr.aria-selected]`, `data-testid` per tab, `qd-interactive-surface`).
- API: `…/data-access/word-types.api.ts`
- State: `…/state/word-types-explorer.facade.ts`, `word-types-detail.facade.ts`,
  `word-types-cache.ts`, `word-types-url-sync.ts` (+ `word-types-url-sync.spec.ts`).
- Models/labels: `…/models/word-types.models.ts`, `word-types.labels.ts`.
- Route helper: `…/core/navigation/route-paths.ts` (`WORDS_TYPES_SEGMENT='types'`, `wordTypesRoutePath()`).
- Feature README: `…/features/words/README.md`.

---

## 11. Frontend changes (state / URL / API / cache / facade / rendering / selection)

### 11.1 URL sync — `state/word-types-url-sync.ts`

- Add `normalizeTableView(value): WordTypeTableView` (default `words`, invalid → `words`).
- In `parseWordTypesQueryParams`, set `tableView`, and **clear stale selection in grouped views**
  (locked 14; review #10): when `tableView !== 'words'`, force `word=null`, `tashkeelWordId=0`,
  `contextCode=''`, `view`/`detailPage`/`location`/`column` to their empty/default values — even if the
  URL supplied them. This makes a direct link like
  `?type=noun&childCode=PN&tableView=roots&word=123&contextCode=PN` render the roots view with no
  selection instead of trying to select a non-existent word row.
- Extend `WordTypesQueryChange` with `tableView: WordTypeTableView | null`.
- Insert `'tableView'` into `WORD_TYPES_QUERY_ORDER` right after `'childCode'` (primary scope group):
  ```
  ['type','childCode','tableView','case','tense','voice','sort','page','word','contextCode','view','detailPage','location','column']
  ```
- `clearWordTypesSelection()` is unchanged (still clears the six selection keys); tab switching layers
  it on top (see facade).

### 11.2 API — `data-access/word-types.api.ts`

Add a method for the unified list endpoint (keep `getRows` for `/words` compatibility/tests):

```ts
getTableRows(options: {
  type: string; childCode: string | null;
  case: WordTypeCase; tense: WordTypeTense; voice: WordTypeVoice;
  tableView: WordTypeTableView; sort: WordTypeSort; page: number; pageSize: number;
}): Observable<ApiResponse<PagedResultDto<WordTypeTableRowDto>>> {
  let params = this.identityParams(options)
    .set('type', options.type)
    .set('tableView', options.tableView)
    .set('sort', options.sort)
    .set('page', options.page)
    .set('pageSize', options.pageSize);
  if (options.childCode !== null) params = params.set('childCode', options.childCode);
  return this.http.get<ApiResponse<PagedResultDto<WordTypeTableRowDto>>>(
    `${this.baseUrl}/api/words/word-types/table`, { params });
}
```

### 11.3 Facade — `state/word-types-explorer.facade.ts`

- `DEFAULT_QUERY` gains `tableView: DEFAULT_WORD_TYPE_TABLE_VIEW`.
- `requestKey(query)` **must include `tableView`** so a tab change triggers reload.
- `loadList()` calls `this.api.getTableRows({ ...query, pageSize: WORD_TYPES_PAGE_SIZE })` with the
  new `WordTypesCacheKeys.table(...)` key.
- Add `selectTableView(tableView)` (resets page, clears selection — locked; review #4):
  ```ts
  selectTableView(tableView: WordTypeTableView): void {
    this.navigate({
      ...buildWordTypesQueryParams({ tableView, page: DEFAULT_WORD_TYPES_PAGE }),
      ...clearWordTypesSelection(),
    });
  }
  ```
- **Stale-row prevention** (locked; review #11): when `tableView` changes, set `rows: null` before/at
  load so the previous view’s rows can never paint under the new scope; the table then shows the
  loading skeleton until the new page arrives. (The table only renders `page.items` when not loading;
  nulling `rows` on switch closes the residual window.)
- **Reset `tableView` to `words` when the table scope disappears** (locked; review #12): `selectType`
  and `selectChild(null)` must also send `tableView: DEFAULT_WORD_TYPE_TABLE_VIEW` so a grouped
  `tableView` never lingers on a parent (no-leaf) state.
- **Remove the `handleListResponse` `case/tense/voice` stamping entirely.** The `/table` word variant
  already returns `case/tense/voice` in the payload, so the identity is complete on arrival — the
  facade maps rows straight through with no re-stamping. (The old `/words` path stamped these client
  side because `WordTypeRowDto` omitted them; the discriminated word variant does not, so no stamping
  is needed to build identity for grouped **or** word rows.)

### 11.4 Rendering — `components/word-types-table/word-types-table.component.ts` + `.html`

- Add `readonly tableView = input<WordTypeTableView>('words')`.
- Change `rows` input to `PagedResultDto<WordTypeTableRowDto> | null`.
- Branch rendering by `row.kind` (defense-in-depth: skip rows whose `kind` doesn’t match the active
  `tableView` so a race can’t render stale-shaped rows).
- **Word view** columns (corrected labels): `الكلمة · النوع · الجذر · الأصل (stem) · الصيغة (lemma) ·
  المواضع · الآيات · السور`. Selection/keyboard behavior unchanged.
- **Grouped views** columns: `<dimension> · المواضع · الآيات · السور`, where `<dimension>` header is
  `الجذر` (roots) / `الأصل` (stems) / `الصيغة` (lemmas). Rows and counts are **noninteractive**
  (locked 10): no row `<button>`, no `qd-word-count-chip` click, no `countOpened`. Render counts as
  plain text/`qd-word-count-chip` with `showLabel=false` and no click handler.
- `track` key: word view keeps `row.tashkeelWordId + ':' + row.contextCode`; grouped views use
  `row.kind + ':' + row.<dimension>Id` (numeric identity).
- Missing dimension text is impossible in grouped views (null IDs excluded); still keep the `—`
  placeholder for the word view’s root/stem/lemma columns.
- `WORD_TYPES_NULL_PLACEHOLDER` and the empty-state label are reused; add per-view ARIA table labels
  (§12.3).

### 11.5 Selection types

- `WordTypeCountOpenedEvent.row` and `selectedRow` typing narrow to `WordTableRowDto` (word variant).
- The page’s `selectedRow` computed returns a word row **only when `tableView === 'words'`**; otherwise
  `null`.

---

## 12. Tabs component, layout, a11y, RTL

### 12.1 New tabs component **(new)**
`…/components/word-type-table-view-tabs/word-type-table-view-tabs.component.{ts,html,scss,spec.ts}`

```ts
@Component({
  selector: 'qd-word-type-table-view-tabs',
  standalone: true,
  templateUrl: './word-type-table-view-tabs.component.html',
  styleUrl: './word-type-table-view-tabs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeTableViewTabsComponent {
  readonly selectedView = input.required<WordTypeTableView>();
  readonly disabled = input(false);
  readonly viewSelected = output<WordTypeTableView>();
  // RTL keyboard: ArrowLeft → next, ArrowRight → previous, Home/End; mirror word-type-details-panel.
}
```

Template mirrors `unique-words-tabs`: `role="tablist"` + per-tab `role="tab"`, `[attr.aria-selected]`,
`data-testid="word-type-table-view-tab--<key>"`, `qd-interactive-surface`, active tab
`qd-is-selected`. Unlike the routerLink-based unique-words tabs, these emit `viewSelected` (button
tabs, no navigation of their own).

### 12.2 Labels — `models/word-types.labels.ts`

```ts
export const WORD_TYPE_TABLE_VIEW_OPTIONS = [
  { value: 'words',  label: 'كلمات' },
  { value: 'roots',  label: 'جذور' },
  { value: 'stems',  label: 'أصول' },   // stems = الأصول الصرفية
  { value: 'lemmas', label: 'صيغ' },    // lemmas = الصيغ المعجمية
] as const;

export const WORD_TYPE_TABLE_VIEW_TABS_LABEL = 'عرض الجدول';
export const WORD_TYPE_TABLE_VIEW_TABLE_LABELS: Record<WordTypeTableView, string> = {
  words:  'جدول كلمات النوع',
  roots:  'جدول الجذور',
  stems:  'جدول الأصول',
  lemmas: 'جدول الصيغ',
};
export const WORD_TYPE_TABLE_VIEW_EMPTY_LABELS: Record<WordTypeTableView, string> = {
  words:  'لا توجد نتائج لهذا النوع',
  roots:  'لا توجد جذور لهذا النطاق',
  stems:  'لا توجد أصول لهذا النطاق',
  lemmas: 'لا توجد صيغ لهذا النطاق',
};
```

Also **correct the reversed headers** in the same file:
`WORD_TYPES_TABLE_HEADERS.stem: 'الصيغة' → 'الأصل'` and `.lemma: 'الأصل' → 'الصيغة'` (§2.1).

### 12.3 Page integration + layout — `word-types-explorer-page.component.{ts,html,scss}`

- Import `WordTypeTableViewTabsComponent`; add `selectTableView(view)` → `explorerFacade.selectTableView(view)`.
- Render the tabs **only when a table scope exists** (locked 12): the same condition that renders the
  table (`listState().rows || status==='loading'`, i.e. a leaf/`inl` is selected). When no leaf is
  selected (parent + `selectPrompt`), the tabs are **hidden** (there is nothing to aggregate).
- Place the tabs between the filter/sort toolbar and the table; visually lighter than the type filter
  cards (a view switcher, not a taxonomy filter).
  ```html
  @if (listState().rows || listState().status === 'loading') {
    <qd-word-type-table-view-tabs
      [selectedView]="listState().query.tableView"
      [disabled]="listState().status === 'loading'"
      (viewSelected)="selectTableView($event)"
    />
  }
  ```
- Pass `[tableView]="listState().query.tableView"` to `qd-word-types-table`.
- **Hide the details panel and expand the table in grouped views** (locked 11): wrap
  `<qd-word-type-details-panel>` in `@if (listState().query.tableView === 'words')`. Add a layout
  modifier (e.g. `qd-explorer-layout--table-only` / `word-types-page__layout--full`) applied when
  `tableView !== 'words'` so `qd-explorer-layout__table` spans full width and the panel column
  collapses. Keep pagination under the table in all views.
- Selected-state styling: grouped rows are noninteractive, so they carry **no** `qd-is-selected` /
  `aria-current` / `aria-selected`; only word rows keep the existing selected styling.

### 12.4 Accessibility / RTL summary
- Tabs: `role="tablist"`/`role="tab"`, `aria-selected`, RTL arrow keys (Left=next, Right=prev),
  Home/End, roving focus — mirror `word-type-details-panel.onTabKeydown`.
- Grouped table keeps `role="table"`/`rowgroup`/`row`/`columnheader`/`cell` and an aria-label from
  `WORD_TYPE_TABLE_VIEW_TABLE_LABELS`.
- Loading uses the existing polite live-region skeleton.
- Backend numeric IDs are never rendered as visible labels.

---

## 13. Backend tests (concrete) — `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/`

Add to the existing `WordTypesCollection` fixture (`WordTypesTestFixture`). New file(s), e.g.
`WordTypesTableReadTests.cs` **(new)** and additions to controller/handler tests:

1. **Grouping** — `tableView=roots|stems|lemmas` returns one row per distinct non-null dimension ID;
   row `DisplayText` matches the dimension text; `kind` is `root|stem|lemma`.
2. **Active grammatical filters** — a verb `tense=past` (or noun `case=…`) scope yields grouped counts
   equal to the occurrences of that scope only (grouped from the same `base`).
3. **Counts** — `occurrencesCount = COUNT(*)`, `ayahsCount = COUNT(DISTINCT ayah)`,
   `surahsCount = COUNT(DISTINCT surah)` over the scoped base per group.
4. **Null dimensions excluded (occurrence-sum identity)** — occurrences with null
   `rootId/stemId/lemmaId` produce no grouped row. Two separate assertions, both over the **same scope**:
   (a) grouped `totalCount` equals the number of **distinct non-null** dimension IDs in that scope
   (nothing else — do **not** compare it to Words `totalCount`); and
   (b) `Σ occurrencesCount` across **all** grouped pages equals `Σ occurrencesCount` across the
   Words-view pages **minus** the occurrences whose selected dimension ID is null — i.e. the difference
   between the two occurrence sums equals exactly the null-dimension occurrence count.
5. **Sorting + tie-breakers** — metric DESC then first-Mushaf then dimension ID; `mushaf-order` then
   dimension ID; `alpha` uses the fold + ordinal collation, dimension-ID tie-break (deterministic).
6. **Pagination** — grouping/total happen before paging; page 2 continues the deterministic order;
   out-of-range page returns empty items with correct `totalCount`.
7. **Cache isolation** — `roots` and `stems` for the same filter/sort/page produce **different** cache
   keys and never cross-serve (assert via `WordTypesCacheKeys.Table` and a cache-hit test mirroring
   `WordTypesCacheReadTests`).
8. **Missing/invalid `tableView`** — missing → `words`; unknown string → `InvalidTableView` (400).
9. **Old `/words` endpoint compatibility** — `/words` still returns `WordTypeRowDto` unchanged, and
   `/table?tableView=words` returns the same rows plus `kind:"word"`.

### 13.1 Terminology tests
Update any assertion of the reversed Word Types labels; if backend messages are added
(`WordTypesTableLoaded`), assert the Arabic string is present and consistent with the
Roots/Lemmas/Stems terminology already in `ApiMessages.cs`.

---

## 14. Frontend tests (concrete)

Add/extend under `…/features/words/`. Obey the repo test-command rule (§16).

1. **URL parse/build** — `state/word-types-url-sync.spec.ts`: missing `tableView` → `words`; invalid →
   `words`; valid round-trips through `buildWordTypesQueryParams`; `tableView` appears in the documented
   param order.
2. **Tab switching** — `state/word-types-explorer.facade.spec.ts` **(new)**: `selectTableView` resets
   page to 1, clears selection, and changes the request key (triggers reload).
3. **Clearing selection** — switching to a grouped view drops `word/contextCode/view/detailPage/
   location/column`.
4. **Direct grouped URLs** — a URL with `tableView=roots` + stale `word`/`contextCode` parses to the
   roots view with **no** selection.
5. **Cache isolation** — `word-types-cache` `table(...)` keys differ by `tableView`.
6. **Stale-row prevention** — after `selectTableView`, `rows` is nulled/loading before the new page,
   so previous-view rows never render; and the table skips rows whose `kind` ≠ active `tableView`.
7. **Grouped rendering** — `components/word-types-table/word-types-table.component.spec.ts`: grouped
   views render the dimension column + three counts, are noninteractive (no row button, no count
   click, no `qd-is-selected`); word view keeps interactivity.
8. **Hidden details panel** — page spec: panel is not rendered and the table spans full width when
   `tableView !== 'words'`; panel returns for `words`.
9. **Tabs hidden without scope** — page spec: tabs absent on the parent/`selectPrompt` state.
10. **Keyboard/accessibility** — new tabs spec: `role="tablist"/"tab"`, `aria-selected`, RTL arrows
    (Left=next, Right=prev), Home/End, roving focus.
11. **Terminology** — table/labels specs assert corrected headers (`stem → الأصل`, `lemma → الصيغة`)
    and tab labels (`أصول` = stems, `صيغ` = lemmas) in the order كلمات | جذور | أصول | صيغ.

---

## 15. Documentation updates (explicit)

Apply in the same change:

1. `Frontend/quran-dashboard-ui/src/app/features/words/README.md` — Word Types now has table-view tabs;
   `tableView` is part of the URL-state contract (default `words`); grouped views are backend-backed and
   paginated; details panel is word-row only and hidden in grouped views; grouped `totalCount` counts
   distinct non-null dimension IDs (not comparable to the Words row `totalCount`), and null-dimension
   coverage is an occurrence-sum identity (Σ grouped occurrences vs Σ Words occurrences).
2. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md` —
   Word Types read model adds grouped table reads from the scoped `base` CTE; grouped counts are a
   separate family from the dimension explorers’ segment/`words_count` counts; alpha uses the shared
   Arabic fold.
3. `WordTypesController` area — document the new `table` endpoint alongside `words` (and `ApiMessages`
   additions) per `Backend/.architecture/API_GUIDELINES.md`.
4. `specs/019-word-types-explorer/contracts/word-types-api.md` — add the E-table endpoint + discriminated
   DTO; correct the reversed `lemmaText // الأصل` / `stemText // الصيغة` comments.
5. `specs/019-word-types-explorer/contracts/frontend-routing-state.md` — add `tableView` to the query
   table + rules; correct the columns line and the “`الصيغة`/`الأصل` placeholder” paragraph.
6. `specs/019-word-types-explorer/contracts/backend-read-abstractions.md` — add `GetTableRowsAsync`,
   the `wordtypes:table:` cache key, and the grouped count/sort rules.
7. `specs/019-word-types-explorer/data-model.md` — add the grouped read-model concept and correct §1.1
   / §1.5 / §5 terminology (`LemmaId → الصيغة المعجمية`, `StemId → الأصل الصرفي`).

> Feature-022 is an extension of 019; keep it in the 019 spec set rather than spawning a new spec
> folder. `docs/feature-022-…` (this file) stays the planning artifact.

---

## 16. Verification commands (exact)

Frontend (Angular unit-test builder; keep the `VITEST_MAX_FORKS` cap that `package.json` sets — the
`test` script is `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2 ng test`):

```bash
cd Frontend/quran-dashboard-ui
npm test -- --include=src/app/features/words/state/word-types-url-sync.spec.ts
npm test -- --include=src/app/features/words/state/word-types-explorer.facade.spec.ts
npm test -- --include=src/app/features/words/components/word-type-table-view-tabs/word-type-table-view-tabs.component.spec.ts
npm test -- --include=src/app/features/words/components/word-types-table/word-types-table.component.spec.ts
npm test -- --include=src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts
ng build
```

Backend (solution `Backend/QuranDashboard.sln`):

```bash
dotnet build Backend/QuranDashboard.sln
dotnet test  Backend/QuranDashboard.sln --filter "FullyQualifiedName~WordsWordTypes"
```

---

## 17. Phased implementation order

1. **Backend contract + read model** — `WordTypeTableView` (new), discriminated DTO (new),
   `IWordTypesReader.GetTableRowsAsync`, `EfWordTypesReader` grouped path + `EfWordTypesReader.Sql.cs`
   additions, `GetWordTypeTable` query/handler/outcome (new), controller `table` action,
   `ApiMessages` additions, DI registration of the handler, cache key + decorator method. Tests §13.
2. **Frontend models + URL sync** — `word-types.models.ts` (tableView type + discriminated union),
   `word-types-url-sync.ts` (parse/build/order + grouped-selection clearing). Tests §14.1, §14.3–4.
3. **Frontend API + cache + facade** — `getTableRows`, `word-types-cache.table`, facade
   `selectTableView`/request-key/loadList/stale-row + scope-reset. Tests §14.2, §14.5–6.
4. **Tabs component** (new) + labels + terminology correction. Tests §14.10–11.
5. **Page integration + layout** — render tabs (scoped), hide panel + expand table in grouped views.
   Tests §14.8–9.
6. **Table rendering per `tableView`** — word vs grouped columns, noninteractive grouped rows.
   Tests §14.7.
7. **Docs/specs/READMEs** (§15).
8. **Full verification** (§16).

---

## 18. Acceptance criteria

MVP is complete when:

1. The Word Types Explorer shows table-view tabs above the table in the RTL order
   **كلمات | جذور | أصول | صيغ**, only when a table scope (leaf/`inl`) is selected.
2. The active tab is reflected in URL state as `tableView`.
3. Existing URLs without `tableView` default to `words`; a direct grouped URL with stale `word`/
   `contextCode` renders the grouped view with no selection.
4. Changing the tab resets page to 1, clears selection, reloads via `/table`, and preserves the active
   type/child/case/tense/voice filters.
5. Grouped views are backed by the read model, grouped from the scoped `base` CTE, with grouping and
   total counting **before** pagination.
6. Grouped identity is the numeric `rootId`/`stemId`/`lemmaId`; Arabic display text is never identity.
7. Grouped counts (occurrences, ayahs, surahs) are correct for the scope; null dimension IDs are
   excluded; grouped `totalCount` = distinct non-null dimension IDs (never compared to Words
   `totalCount`); null coverage is verified by the occurrence-sum identity (Σ grouped occurrences vs
   Σ Words occurrences), documented not hidden.
8. Sorting is deterministic for every view (metric/mushaf/alpha with numeric-ID tie-break).
9. In grouped views the details panel is hidden and the table spans full width; grouped rows and counts
   are noninteractive.
10. Both backend and frontend cache keys include `tableView`; no cross-view stale rows.
11. Stem/Lemma terminology is correct everywhere (labels, headers, contracts, tests): stem = أصل /
    الأصول الصرفية; lemma = صيغة / الصيغ المعجمية.
12. `/words` remains functional and tested; the discriminated `/table` is the new list source.
13. Frontend and backend test suites and builds pass (§16); READMEs/specs updated (§15).

---

## 19. Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Grouped pagination/count drift | Group + `COUNT(DISTINCT dimension_id)` in SQL over the scoped `base` **before** paging; test §13.6. |
| Reusing the wrong count basis | Grouped counts come only from `base`; never from `EfRootsReader`/segment aggregates (§3.6). |
| STJ polymorphism surprises | Declare the paged item type as the abstract base; verify the `kind` discriminator serializes with default ASP.NET Core STJ; no custom converter. |
| Stale-shaped rows across tabs | `tableView` in cache keys + request key; null `rows` on switch; render skips rows whose `kind` ≠ active view (§11.3–4). |
| Grouped `tableView` on a no-leaf scope | `selectType`/`selectChild(null)` reset `tableView=words`; tabs hidden without a scope (§11.3, §12.3). |
| Alpha ordering inconsistent with explorers | Reuse the Roots fold + `COLLATE "C"` ordinal + dimension-ID tie-break (§3.7, §7.3); verify in test §13.5. |
| Terminology change is user-visible | Apply label/contract/test corrections in the same change; it aligns Word Types with already-correct Roots/Lemmas/Stems terms (§2.1). |
| URL contract change | `tableView` documented in the 019 routing contract + README; defaults keep old deep links valid. |

---

## 20. Out of scope (explicit)

- **Grouped-row details** (root/stem/lemma detail panels, ayah/surah drilldowns for a group) — **not**
  in MVP. Grouped rows are noninteractive.
- **Grouped-explorer navigation** (linking a grouped row to the standalone Roots/Lemmas/Stems explorer)
  — an **optional future follow-up only**, not part of this implementation.
- No new selection URL params (`selectedKind`/`selectedId`/`group`/`groupKind`) — the shareable URL
  contract gains only `tableView`.
- No migrations, no entity/importer changes, no new identity tables.
- No changes to the detail endpoints (`words/{id}`, `…/ayahs`, `…/surahs`) beyond staying word-row only.
