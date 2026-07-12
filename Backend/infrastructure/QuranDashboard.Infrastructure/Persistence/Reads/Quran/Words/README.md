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
- **Read-only + `AsNoTracking`** semantics; these readers must not mutate state.
- Response shape/paging must stay aligned with `ReadPaging` and the API contract; changing
  a column or page shape is an API-contract change (update the controller + `API_GUIDELINES`
  expectations and any frontend model).
- **Word Types grouped table reads** (`EfWordTypesReader.GetTableRowsAsync` for
  `tableView=roots|stems|lemmas`) reuse the same scoped `BaseRowsSql` occurrence base as the
  word rows, verbatim — grouping by the numeric `root_id`/`stem_id`/`lemma_id`, excluding
  nulls, with grouping and total counting happening **before** pagination
  (`GroupedRowsSql`/`GroupedRowsCountSql` in `EfWordTypesReader.Sql.cs`). These grouped
  counts are a **separate family** from the Roots/Lemmas/Stems explorers' global,
  unscoped, segment/`words_count`-backed aggregates (`EfRootsReader.LoadWholeSummaryAsync`
  and friends) — never conflate the two. Grouped `alpha` sort reuses the Roots explorer's
  Arabic fold (`RootsListDerivation.ArabicFoldFrom`/`ArabicFoldTo`) with `COLLATE "C"`
  ordinal collation, tie-broken by the numeric dimension ID.
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

## Related

- Handlers: `application/QuranDashboard.Application/Quran/Words/**`.
- Frontend consumers: `Frontend/quran-dashboard-ui/src/app/features/words/README.md`.
- Specs: `specs/015-roots-explorer/`, `016-lemmas-stems-explorer/`, `019-word-types-explorer/`,
  `014-words-hub-unique-words/`. (Prior feature-015/016/017 evidence reports were purged.)
