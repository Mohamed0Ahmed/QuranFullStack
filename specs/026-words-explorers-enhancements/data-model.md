# Data Model: Words Explorers Enhancements (Feature 026)

**Date**: 2026-07-14 · Read-only feature: **no new tables, no schema changes, no
migrations**. This file describes the request/response shapes and derived-state model
the feature adds over existing data.

## Existing entities (consumed, unchanged)

| Entity | Table (rows) | Fields used by this feature |
|---|---|---|
| Unique word (tashkeel / simple) | `quran_words_unique_tashkeel` (21,294) / `quran_words_unique_simple` (14,783) | `id`, `text_uthmani` (display), `text_imlaei_simple` (search identity), `occurrences_count`, `ayahs_count`, `surahs_count` |
| Root | `quran_roots` (1,642) | `id`, `root_text`, global usage aggregates (existing summary rows) |
| Lemma | `quran_lemmas` (4,790) | `id`, `lemma_text`, `root_id` (real FK — basis of the Lemmas-by-root filter) |
| Stem | `quran_stems` (12,108) | `id`, `stem_text`; *primary* root/lemma associations are derived onto its list rows (not FK columns) |
| Word-context morphology | `quran_word_morphology` (77,432) | `root_id`, `stem_id`, `lemma_id` (nullable — basis of has-flags), scope columns already used by `BaseRowsSql` |

**Identity rules (unchanged, hard)**: word identity = clean imlaei-simple text;
Uthmani is display-only. Word Types row grain = `(unique_tashkeel_word_id,
context_code)` word-context identity. Grouped identities are numeric
`root_id`/`stem_id`/`lemma_id`, never display text.

**Count families (unchanged, hard)**: scoped word-context counts (Word Types) vs
global `words_count`-backed aggregates (Roots/Lemmas/Stems explorers). Never mixed in
one surface; the new scope counts belong exclusively to the scoped family.

## New/extended request parameters

### Word Types list scope (extends `GET api/words/word-types/words` and `/table`)

| Param | Type | Validation | Meaning |
|---|---|---|---|
| `search` | `string?` | trim; empty→null; max length 64 else `InvalidFilter`; never logged (only `hasSearch`) | normalized-contains match on the folded word-identity search column (`search_text_normalized`); part of the shared scope — words view, grouped views, and scope counts all inherit it |
| `hasRoot` / `hasStem` / `hasLemma` | `bool?` (tri-state) | absent = any; `true` = has; `false` = missing | presence predicates over `m.root_id`/`m.stem_id`/`m.lemma_id` (allowlisted `IS [NOT] NULL`); part of the shared scope |
| `pageSize` | `int?` | list reads: 1..**1000** (`MaxListPageSize`), default **1000**; detail reads: 1..**100** (`MaxDetailPageSize`), default **100** | split caps — the documented grouped-detail 1..100 contract is preserved |

### Count-range filters (Unique Words, Roots, Lemmas, Stems list reads)

Per metric `k`: `kMin` / `kMax`, both `int?`.

| Rule | Behavior |
|---|---|
| `kMin >= 0`; `kMax >= kMin` when both present | else 400 `InvalidFilter` (Arabic message) |
| Either bound omissible | open-ended range |
| Metrics per page | Unique Words: `occ`, `ayahs`, `surahs` · Roots: + `simpleWords`, `tashkeelWords`, `lemmas`, `stems` · Lemmas: + `simpleWords`, `tashkeelWords`, `stems` · Stems: + `simpleWords`, `tashkeelWords` |
| Execution | Unique Words: SQL predicates; Roots/Lemmas/Stems: in-memory derivation predicates over cached whole-summary rows |

### Association filters

| Page | Param | Type | Validation | Semantics |
|---|---|---|---|---|
| Unique Words | `primaryType` | `string?` | POS code validated against the catalogue, else 400 | rows whose *primary* word type (same rule as the displayed chip) equals the code |
| Unique Words | `rootId` | `int?` | positive, else 400 | rows whose *primary* root (same rule as the displayed chip) is this root |
| Lemmas | `rootId` | `int?` | positive, else 400 | lemmas with `root_id = @rootId` (real FK belonging) |
| Stems | `rootId` / `lemmaId` | `int?` | positive, else 400 | stems whose derived **primary** root/lemma association matches; label "الجذر الأساسي" / "الصيغة المعجمية الأساسية" (primary-not-sole, documented) |

## New response DTO

```
WordTypeScopeCountsDto
├── WordsCount  : int   # scoped word-context identities — byte-consistent with RowsCountSql
├── RootsCount  : int   # COUNT(DISTINCT root_id)  excl. NULL — byte-consistent with roots GroupedRowsCountSql
├── StemsCount  : int   # COUNT(DISTINCT stem_id)  excl. NULL — byte-consistent with stems GroupedRowsCountSql
└── LemmasCount : int   # COUNT(DISTINCT lemma_id) excl. NULL — byte-consistent with lemmas GroupedRowsCountSql
```

Served by `GET api/words/word-types/scope-counts` wrapped in `ApiResponse<T>`.
Invariant: each field equals the corresponding tableView's `PagedResult.TotalCount`
for the identical scope (FR-016 equality contract). Existence semantics: an invalid
scope → 400 `InvalidFilter`; a valid scope with no rows → all-zero DTO (200).

## Frontend state model (derived; no persistence)

### URL state additions (contract detail in `contracts/frontend-url-state.md`)

| Page | New keys |
|---|---|
| Unique Words | `occ`, `ayahs`, `surahs` (range grammar `min..max`), `primaryType`, `rootId` |
| Roots | `occ`, `ayahs`, `surahs`, `simple`, `tashkeel`, `lemmas`, `stems` (ranges) |
| Lemmas | ranges + `rootId` |
| Stems | ranges + `rootId`, `lemmaId` |
| Word Types | `search`, `hasRoot`, `hasStem`, `hasLemma` |

All parse fail-closed (malformed ⇒ absent); all reset the list page on change; all are
optional — existing URLs unchanged.

### List/scope state

- Normal explorers: list state gains the active range/association filter values;
  headline stat = existing `totalCount` (no new state).
- Word Types: the "scope" grows from `(type, childCode, case, tense, voice)` to
  `(type, childCode, case, tense, voice, search, hasRoot, hasStem, hasLemma)`.
  Scope counts are keyed by the full scope; they reload on scope change only —
  not on `tableView` or `page` changes.
- Cache identity (frontend and backend): every cache key that covers a filtered read
  includes every filter input that shaped it; empty/absent values normalize to the
  pre-feature key so warm entries stay valid.

### Bucket presets (presentation constants, not contract)

| Metric family | Buckets (disjoint) |
|---|---|
| occurrences | 1 · 2–10 · 11–100 · 101–1000 · 1001+ |
| ayahs / surahs | 1 · 2–10 · 11–50 · 51+ (surahs ≤ 114) |
| word/lemma/stem sub-counts | 1 · 2–5 · 6–20 · 21+ |

Each bucket maps to a canonical range serialized in the URL; مخصّص reveals min/max
inputs producing the same grammar.

## State transitions

```
filter/search change (any page)
  → URL updated (merge), list page reset to 1
  → list reload (cache-first) → totalCount → headline stat (normal pages)
  → [Word Types] scope changed → scope-counts reload (cache-first)

tableView / page change (Word Types)
  → list reload only; scope counts NOT reloaded

counts read fails, table succeeds → strip shows compact error + إعادة المحاولة; table unaffected
table read fails               → page error state owns messaging; stat/strip show no stale numbers
```
