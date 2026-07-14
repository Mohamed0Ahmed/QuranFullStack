# Contract: Word Types API changes (Feature 026)

Route base: `api/words/word-types` (existing). All responses `ApiResponse<T>`; lists
`PagedResult<T>`; GET-only; Arabic messages centralized. Steady-state truth after
merge = code + reads README; this contract guides implementation and review.

## 1. Extended list reads

### `GET api/words/word-types/words` and `GET api/words/word-types/table`

New optional query params (added to the existing
`type, childCode, case, tense, voice, sort, page, pageSize` — and `tableView` on
`/table`):

| Param | Type | Default | Validation | Semantics |
|---|---|---|---|---|
| `search` | string | absent | trim; whitespace→absent; length > 64 → 400 `WordTypesInvalidFilter`; value never logged (structured logs carry `hasSearch` only) | Arabic-normalized contains-match on **word identity text** (`quran_words_unique_tashkeel.text_imlaei_simple`) applied to the shared scoped occurrence base — the words view rows, all three grouped views, and their `TotalCount`s inherit it. Never matches root/stem/lemma display text. |
| `hasRoot` | bool | absent | `true`/`false` only, else 400 | tri-state presence predicate `m.root_id IS [NOT] NULL` on the shared base |
| `hasStem` | bool | absent | same | `m.stem_id IS [NOT] NULL` |
| `hasLemma` | bool | absent | same | `m.lemma_id IS [NOT] NULL` |

Page-size contract change:

| Read | Old default / cap | New default / cap |
|---|---|---|
| `/words`, `/table` (list reads) | 25 / 100 | **1000 / 1000** (`MaxListPageSize`) |
| `/words/{id}/ayahs` (word detail) | 25 / 100 | **100** / 100 (`MaxDetailPageSize`) |

Out-of-range `pageSize` → 400 `WordTypesInvalidPaging` (unchanged shape).

Behavioral invariants preserved: grouping and total counting happen **before**
pagination; grouped `alpha` sort collation unchanged; ordering contracts unchanged;
row shapes (`WordTypeRowDto`, `WordTypeTableRowDto` polymorphic variants) unchanged.

## 2. Grouped detail reads (`api/words/word-types/table/{kind}/{dimensionId}`)

- `search` / has-flags are **NOT** accepted here — detail identity is numeric and
  already scoped (asymmetry documented in the reads README).
- `pageSize` default 25 → **100** for `/words` and `/ayahs` member reads; cap stays
  100. `/surahs` stays single-shot (no paging params).
- Command budgets unchanged: member ayahs ≤ 3 commands/page; surahs ≤ 2.

## 3. NEW read: scope counts

### `GET api/words/word-types/scope-counts`

| Aspect | Contract |
|---|---|
| Query params | `type` (required main-type code), `childCode?`, `case?`, `tense?`, `voice?`, `search?`, `hasRoot?`, `hasStem?`, `hasLemma?` — **exactly the list scope, nothing else** (no `tableView`, no paging) |
| Response | `ApiResponse<WordTypeScopeCountsDto>` — `{ wordsCount, rootsCount, stemsCount, lemmasCount }` (ints) |
| 200 | valid scope; zero-row scope returns all zeros |
| 400 | invalid type/child/feature/flag/search → `WordTypesInvalidFilter` family (Arabic message) |
| Equality invariant | each count equals the `TotalCount` the corresponding `tableView` (`words`/`roots`/`stems`/`lemmas`) list read returns for the **identical** scope — enforced by reusing `RowsCountSql` / `GroupedRowsCountSql` fragments over the same `BaseRowsSql` base |
| Count family | scoped word-context family ONLY (words = distinct `(unique_tashkeel_word_id, context_code)`; dimensions = `COUNT(DISTINCT <dim>_id)` excl. NULL). Never the global `words_count`-backed aggregates. |
| Execution budget | **1 SQL command** per uncached call (pinned by test) |
| Caching | server-side memory cache; key includes **every** scope input (type, childCode, case, tense, voice, normalized search, three flags); entry options mirror the table read's |

## 4. Handler outcomes (Application layer)

- `GetWordTypeRows/Table`: existing outcomes + `InvalidFilter` covers bad
  search/flags.
- NEW `GetWordTypeScopeCounts`: `Success | InvalidFilter`.
- Validation constants: `MinPageSize = 1`, `MaxListPageSize = 1000`,
  `MaxDetailPageSize = 100` (split of the former single `MaxPageSize = 100`).

## 5. Non-negotiables

- SQL identifiers stay allowlisted; user input reaches SQL only as parameter values.
- Search predicate joins/EXISTS against `quran_words_unique_tashkeel` on the base's
  word id; no ayah-text access, no dimension-text predicates.
- Recorded implementation deviation (accepted): the search predicate matches the computed
  `quran_words_unique_tashkeel.search_text_normalized` column (the folded identity-search text
  Unique Words search already uses) instead of the literal `text_imlaei_simple` named in §1 —
  the substance of Locked Decision A1 (normalized imlaei-simple word-identity text only) is
  preserved, and the T009(d) normalization-equivalence requirement makes the folded column the
  correct target.
- No writes; `AsNoTracking`/raw-read semantics preserved.
- reads README + `features/words/README.md` updated in the same commit as each
  contract change here.
