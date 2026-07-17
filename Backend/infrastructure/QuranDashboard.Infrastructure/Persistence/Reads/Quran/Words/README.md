# Words explorer read models

**Layer:** Infrastructure · read-only queries · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md`

## What this area does

Read-only EF readers behind the five Words explorers — Roots, Lemmas, Stems, WordTypes,
and Unique Words. They back the `application/.../Quran/Words/**` query handlers and the
`api/.../Controllers/Words/*` endpoints. No writes happen here.

## Key pieces

- `EfUniqueWordsReader.cs`, `Roots/EfRootsReader.cs`, `Lemmas/EfLemmasReader.cs`,
  `Stems/EfStemsReader.cs` (+ `.Summary.cs`), `WordTypes/EfWordTypesReader.cs`
  (+ `.Sql.cs`, + `.GroupedDetails.cs` / `.GroupedDetails.Sql.cs`) — the readers. Word-types and
  stems readers are **partial-split by size** (summary vs list/SQL vs grouped-details); keep the split
  when adding to them.
- `*ListDerivation` / `*SummaryRow` — how list rows and summary aggregates are derived.
- `WordTypes/WordTypeIdentityMatcher.cs`, `WordTypeGrouping.cs` — POS/type identity + grouping.
- `MorphologyRelatedItemsOrdering.cs` — shared ordering for related lemmas/stems/roots.
- `ReadPaging.cs` — the paging contract shared by all list endpoints.

## Invariants / caveats (read before changing)

- **Word identity keys on clean imlaei-simple**; the Uthmani form is display only. Two
  words that differ only by tashkeel/Uthmani orthography are the same identity.
- **Ordering is part of the contract** — related-item and list ordering is deterministic
  via `MorphologyRelatedItemsOrdering` / `*ListDerivation`; do not reorder casually.
- **List `sort` token grammar** (Feature 030, N8) — the five explorers share ONE opaque `sort` query
  param (there is deliberately **no `dir` param**) whose grammar is
  `token := column | column "-asc" | column "-desc"` (`WordSortToken`, Application.Abstractions).
  A **bare token means the column's NATURAL direction** — counts descend, text ascends — so every
  pre-feature token keeps its exact meaning as an **alias**: `occurrences` ≡ `occurrences-desc`,
  `alpha` ≡ `alpha-asc`, word-types `ayahs`/`surahs` ≡ their `-desc` forms. **`mushaf-order` is
  ascending-only and matches the BARE token only** — it is the release/default order, not a column, so
  `mushaf-order-asc`/`-desc` are **rejected (400)**. **Canonical serialization:** the bare form is
  canonical for the natural direction, the suffixed form only for the opposite one (canonical Roots
  المواضع set = `occurrences`, `occurrences-asc`; `occurrences-desc` parses but canonicalizes OUT to
  `occurrences`). `*SortSpec.CanonicalToken()` is the single source of that mapping and is what the
  cache `SortKey()` and the handler logs emit, so aliases collapse onto ONE cache entry and every
  pre-feature key/URL stays byte-identical. Allowlisted columns per explorer (defaults in bold):
  **Roots** `alpha` · `occurrences` · `ayahs` · `surahs` · `simple` · `tashkeel` · `lemmas` · `stems`
  · **`mushaf-order`**; **Lemmas** the same minus `lemmas`; **Stems** minus `lemmas`/`stems`;
  **Unique Words** `alpha` · `occurrences` · `ayahs` · `surahs` · **`mushaf-order`**; **Word Types**
  (words AND all three grouped views) `alpha` · `ayahs` · `surahs` · `mushaf-order` ·
  **`occurrences`** — word-types is the one explorer that defaults to `occurrences` (desc), not Mushaf
  order. Related-entity TEXT columns (lemmas' root, stems' dominant root/lemma, unique's type/root,
  word-types' type/root/stem/lemma) and unique-words' missing-surahs (post-page computed; the monotone
  inverse of السور) are **deliberately not sortable**; grouped member-word detail reads take no sort at
  all. An unknown/unlisted token is a controlled **400 InvalidSort**, never a silent fallback.
- **Sort tie-break contract** (Feature 030, N8) — sorting changes ORDER BY only (never the filter, so
  `TotalCount` and the row multiset are invariant across every token), and **each column's tie chain is
  identical in BOTH directions**, so reversing a column never reshuffles its ties:
  - **count columns** (both directions): count → `FirstWordOrderInMushaf` → `Id`.
  - **`alpha` on Roots/Lemmas/Stems** (both directions): text → `Id`, with **NO Mushaf tie-break** —
    this is a deliberate EXCEPTION preserving the exact row order existing `sort=alpha` links already
    return (pinned by `StemsListReadTests`' alpha sequence). Do not "harmonize" it.
  - **`alpha` on Unique Words** keeps its own pre-existing chain: `SearchText` → `FirstWordOrderInMushaf`
    → `Id`.
  - **`mushaf-order`**: `FirstWordOrderInMushaf` → `Id`.
  - **Unique Words** now appends `.ThenBy(Id)` to EVERY branch — pure hardening, no order change
    (`FirstWordOrderInMushaf` is already effectively unique), closing a DB-side non-determinism gap that
    the in-memory explorers never had (LINQ-to-Objects sorts are stable; a SQL `ORDER BY` is not).
  - **Word Types** keeps its existing per-view tie chains in both directions
    (`g.tashkeel_word_id, g.context_code` for the words view; `dimension_id` for grouped).
  Directions are **allowlisted, never interpolated**: controllers hand the raw string only to the
  Application parser, SQL/LINQ only ever see a parsed `(column, direction)` pair, and the word-types
  ORDER BY strings stay compiler-known CONSTANTS selected by an enum switch with the direction baked in.
- **Word Types tree parent counts use row-count semantics** — each parent count equals the
  unscoped grouped word-context row total returned for that main type, not the number of visible children.
- **Read-only + `AsNoTracking`** semantics; these readers must not mutate state.
- Response shape/paging must stay aligned with `ReadPaging` and the API contract; changing
  a column or page shape is an API-contract change (update the controller + `API_GUIDELINES`
  expectations and any frontend model).
- **Word Types word-identity search** (Feature 026) is one optional predicate on the shared
  `BaseRowsSql` occurrence base (`SearchPredicate` in `EfWordTypesReader.Sql.cs`): a parameterized
  `unique_word.search_text_normalized ILIKE @searchPattern` on the tashkeel-word join the base already
  carries. It reuses the **same computed identity-search column** (`search_text_normalized`, a folded
  `text_uthmani_simple || ' ' || text_imlaei_simple`) and the **same query normalizer**
  (`ArabicSearchQueryNormalizer`, extracted from `EfUniqueWordsReader`) that Unique Words search uses, so
  the two boxes fold diacritics/orthography identically. It matches **word identity text only** — never
  `root_text`/`stem_text`/`lemma_text` — and, because it lives on the shared base, the words view, all
  three grouped views, and their `TotalCount`s narrow together (list scope). The search term reaches SQL
  only as a parameter value and is never logged (`hasSearch` boolean only). **Grouped-detail reads
  (`.GroupedDetails.*`) take NO search term** — their identity is a numeric dimension id already scoped;
  `ToGroupedReadContext` builds a search-free context, so the asymmetry is by construction.
- **Count-range filters** (Feature 026, US5) narrow the four normal explorers by exactly the count
  columns each list already displays — no recomputation, no count-family change. The shared
  `CountRange`/`<page>CountFilter` value objects (Application.Abstractions) validate `Min >= 0` and
  `Max >= Min` in the handlers (else a controlled 400 `InvalidFilter`); an open bound is allowed. Every
  active range **ANDs** with search/sort. Execution follows each page's existing read mechanism: Unique
  Words applies parameterized SQL predicates on `occurrences_count`/`ayahs_count`/`surahs_count` inside
  `BuildListQuery` (identifiers allowlisted, bounds are parameter values only) and its list cache keys
  gain the range fragment (absent ⇒ pre-feature key); Roots/Lemmas/Stems apply **in-memory** predicates
  in their `*ListDerivation.FilterAndSort` over the cached whole-summary rows, so their backend cache
  keys are unchanged. Ranges filter dimension entries (Roots/Lemmas/Stems) or unique-word identities
  (Unique Words). The filtered `PagedResult.TotalCount` equals the filtered row count (the stat
  contract), and ordering is untouched — the predicates are pure `Where`s.
- **Association filters** (Feature 026, US7) narrow three normal explorers by a related dimension, always
  by the SAME association the list row displays (so the filter and the displayed value can never disagree —
  the chip⇔filter invariant, pinned by tests). **Unique Words** `primaryType` (POS code) and `rootId`
  (positive int) are predicates in the base SQL of `BuildTashkeelQuery`/`BuildSimpleQuery`
  (`EfUniqueWordsReader.BuildListQuery`): each is an `id IN (…)` over a `DISTINCT ON (unique_id)` winner
  subquery that reproduces EXACTLY the primary-selection ordering `LoadPrimaryWordTypesAsync` /
  `LoadPrimaryRootsAsync` use for the displayed chip (group the word's occurrences by POS code / root,
  order by occurrence count DESC, earliest `quran_word` id ASC, then code/root id). Values reach SQL only
  as parameters; the unique-id column is an allowlisted constant. `primaryType` is validated against the
  POS catalogue (`quran_pos_tags` via `IPosTagCatalogueReader`) in the handler — an unknown code is a
  controlled **400 InvalidFilter**, not a silent empty result; a nonpositive id is likewise 400. A
  valid-but-unmatched id returns a **200 empty page** (`TotalCount = 0`), never a 404. Unique Words list
  cache keys gain a `pt…:root…` segment (absent ⇒ pre-feature key). **Lemmas** `rootId` is an in-memory
  predicate on the real FK `quran_lemmas.root_id` (`LemmasListDerivation.FilterAndSort`) — a true
  belonging relation. **Stems** `rootId`/`lemmaId` are in-memory predicates on the derived **primary**
  (dominant) association surfaced on the stem's list row (`StemsListDerivation.FilterAndSort`,
  `DominantRootId`/`DominantLemmaId`). **This is a primary-not-sole filter: a stem whose primary root or
  lemma differs is excluded even if it co-occurs with the filtered id** — the filter matches only the one
  primary association, not all co-occurring associations. Roots/Lemmas/Stems derive over the cached whole
  summary, so their backend cache keys are unchanged; ordering is untouched (the predicates are pure
  `Where`s). All association params may be logged as booleans/ids (no user text).
- **Word Types presence flags** (Feature 026, US6) are tri-state `hasRoot`/`hasStem`/`hasLemma`
  (`bool?`: null = any, true = has, false = missing) threaded through `WordTypeFilter` →
  `WordTypeReadContext` → `PresenceFilterPredicate` on the shared `BaseRowsSql`. The predicate is
  allowlisted `m.root_id|stem_id|lemma_id IS [NOT] NULL` — no user text, no parameters. Because it lives
  on the shared base, the words view, all three grouped views, and their `TotalCount`s narrow together
  (list scope); grouped-detail reads (`.GroupedDetails.*`) never set the flags (same asymmetry as
  search). `WordTypesCacheKeys.HashFilter` appends a flag component only when a flag is set (absent ⇒
  pre-feature 5-part hash), so flagged and unflagged reads never cross-serve. Malformed direct-API flag
  values are rejected by the `[ApiController]` bool binding (400); the frontend fails closed to absent.
- **Word Types page-size caps** are split in `WordTypesHandlerValidation` (Feature 026): **list reads**
  (`/words`, `/table`) accept `pageSize 1..1000` (`MaxListPageSize`, default 1000); **detail reads** (word
  ayahs, grouped member words, grouped ayahs) keep `pageSize 1..100` (`MaxDetailPageSize`, default 100).
  The former single 100 cap gated both; the split preserves the documented grouped-detail 1..100 contract
  while unlocking 1000-row list parity. Grouped surahs stay single-shot.
- **Word Types grouped table reads** (`EfWordTypesReader.GetTableRowsAsync` for
  `tableView=roots|stems|lemmas`) reuse the same scoped `BaseRowsSql` occurrence base as the
  word rows, verbatim — grouping by the numeric `root_id`/`stem_id`/`lemma_id`, excluding
  nulls, with grouping and total counting happening **before** pagination
  (`GroupedRowsSql`/`GroupedRowsCountSql` in `EfWordTypesReader.Sql.cs`). **Single-command
  window count:** the words view (`GetRowsAsync`) and the grouped table views return the page
  **and** the `PagedResult.TotalCount` from ONE scoped command — `GroupedRowsSql`/`RowsSql`
  project `COUNT(*) OVER()` over the grouped set (the winner joins are 1:1, so it equals the
  matching `RowsCountSql`/`GroupedRowsCountSql` for the identical scope). The separate
  count-only command (`CountRowsAsync`/`CountGroupedRowsAsync`) runs **only** for an empty page
  (out-of-range or empty scope), where the window count has no row to carry it; the scope-count
  read (`.ScopeCounts.cs`) stays its own one-command contract, untouched. These grouped
  counts are a **separate family** from the Roots/Lemmas/Stems explorers' global,
  unscoped, segment/`words_count`-backed aggregates (`EfRootsReader.LoadWholeSummaryAsync`
  and friends) — never conflate the two. Grouped `alpha` sort reuses the Roots explorer's
  Arabic fold (`RootsListDerivation.ArabicFoldFrom`/`ArabicFoldTo`) with `COLLATE "C"`
  ordinal collation, tie-broken by the numeric dimension ID — in **both directions**
  (`alpha` = `norm_text COLLATE "C"`, `alpha-desc` = `norm_text COLLATE "C" DESC`). The folded
  `norm_text` column is projected into the grouped CTE **only** for alpha, and the `@foldFrom`/`@foldTo`
  pair (always SQL **parameters**, never interpolated) is bound under the SAME condition: both the SQL
  shape (`GroupedRowsSql`) and the parameter list (`BuildGroupedRowsParameters`) gate on the single
  `NeedsFold(sort)` predicate. **Keep them on that one predicate** — if the two ever disagree the query
  either orders by a column that was never projected or Npgsql rejects an unbound parameter, and both
  fail only at RUNTIME.
- **Word Types scoped four-count summary** (Feature 026, US8, `EfWordTypesReader.GetScopeCountsAsync` in the
  `.ScopeCounts.cs` partial) returns `WordTypeScopeCountsDto(WordsCount, RootsCount, StemsCount, LemmasCount)`
  for the FULL active list scope (type, childCode, case, tense, voice, search, presence flags — the same
  `WordTypeReadContext` the words/table reads build). It is **one SQL command**: a single CTE over the shared
  scoped `BaseRowsSql` base (search predicate + presence flags included), then four aggregates that **reuse
  the existing count formulas verbatim** rather than re-deriving them — words = the `RowsCountSql` formula
  (`COUNT(DISTINCT (tashkeel_word_id, context_code))`, the row-constructor form of its `GROUP BY … COUNT(*)`),
  and roots/stems/lemmas = the `GroupedRowsCountSql` formula (`COUNT(DISTINCT <dim>_id)`, which already
  excludes NULLs). Because the base and the formulas are identical, each count **equals the corresponding
  tableView's `PagedResult.TotalCount` for the identical scope** — the FR-016 equality contract, pinned by the
  equality matrix in `WordTypesScopeCountsReadTests`. These are the **scoped word-context count family only**,
  never the global `words_count`-backed aggregates. The search term reaches SQL only as a parameter value and
  is never logged (`hasSearch` boolean only). `CachedWordTypesReader` caches it under
  `WordTypesCacheKeys.ScopeCounts` — keyed by every scope input and nothing view/page (no `tableView`/`sort`/
  `page`) — with the table read's entry options; a zero-row valid scope returns an all-zero DTO, an invalid
  scope is a controlled 400 `InvalidFilter`.
- **Word Types grouped detail reads** (`EfWordTypesReader.GetGroupedSummaryAsync`, Feature 023, in the
  `.GroupedDetails.*` partials) select from the **same scoped `BaseRowsSql` occurrence base** as the
  grouped table, then restrict it to a single allowlisted numeric column
  (`root_id`/`stem_id`/`lemma_id = @dimensionId`) at **head grain** — `quran_word_morphology` only, never
  `quran_word_morphology_segments`. The summary's counts and display text are byte-for-byte identical to
  the selected grouped table row in the same scope; a positive dimension ID absent from the scope returns
  `null` (a scoped-group 404). The dimension text columns are projection-only display and never
  participate in the membership predicate.
- **Word Types grouped member words** (`EfWordTypesReader.GetGroupedMemberWordsAsync`, Feature 023) reuse
  the **existing** `RowsSql`/`RowsCountSql` (the Words-view grouping/winner/order SQL) with an optional
  `WordTypeGroupedDimensionKind` that adds only the allowlisted numeric predicate
  `m.root_id|m.stem_id|m.lemma_id = @dimensionId` to `BaseRowsSql`. Members are grouped by the identical
  `(unique_tashkeel_word_id, context_code)` formula the Words view uses, so they are a **row-for-row**
  subset of the Words table for that numeric dimension — never a distinct-word collapse and never filtered
  by `root_text`/`stem_text`/`lemma_text` (labels are projection-only). Rows are **display-only** for the
  consumer: the reader orders them by the fixed occurrence order (`WordTypeSort.Occurrences`), counts the
  grouped word-context rows **before** paging (`page`/`pageSize 1..100`), returns a non-null empty page for
  an out-of-range page, and `null` when the dimension is absent from the scope. Existing list/table callers
  pass `null` for the dimension kind and stay semantically unchanged.
- **Word Types grouped ayahs** (`EfWordTypesReader.GetGroupedAyahMatchesAsync`, Feature 023) page the
  **distinct `ayah_id`** of the same dimension-filtered `BaseRowsSql` base in Mushaf order and reuse the
  `WordTypeAyahMatchDto` shape. Hydration is **bounded to three commands** per page — a distinct-ayah count
  (doubling as the existence check: zero → `null`/404), one grouped-page query that joins the page ayahs
  back to the scoped base for their matched `(word id, position)`, and one `AsNoTracking`
  `quran_words` hydration query for the whole page — never one query per ayah. Highlight text is the
  **canonical `quran_words.text_uthmani`** loaded through the shared `ResolveAyahPageNumber` helper (no
  ayah-text fallback, no string replacement); `MatchedWordIds`/`MatchedWordPositions` carry only the scoped
  head matches, and ayah markers are excluded from the hydrated word list. Out-of-range page → non-null
  empty page with the correct `TotalCount`.
- **Word Types grouped surahs** (`EfWordTypesReader.GetGroupedSurahsAsync`, Feature 023) are **single-shot**
  (no paging contract). Occurrence counts are aggregated **inside PostgreSQL** by `surah_number` over the same
  dimension-filtered `BaseRowsSql` base at head grain; the mentioned/missing split is then derived in memory
  against the surah catalogue in numeric order. Hydration is **bounded to two commands** — one scoped surah
  aggregate (doubling as the existence check: zero rows → `null`/404, short-circuiting before the catalogue
  read) and one catalogue read — never one query per occurrence. Both the mentioned (`Surahs`) and missing
  (`MissingSurahs`) arrays ship in the same `WordTypeSurahsResponse`; `detailPage`/paging is not part of this
  read's contract.

## Related

- Handlers: `application/QuranDashboard.Application/Quran/Words/**`.
- Frontend consumers: `Frontend/quran-dashboard-ui/src/app/features/words/README.md`.
- Specs: `specs/015-roots-explorer/`, `016-lemmas-stems-explorer/`, `019-word-types-explorer/`,
  `014-words-hub-unique-words/`. (Prior feature-015/016/017 evidence reports were purged.)
