# Feature 013 — Words & Roots Explorer: Unique Words Capability Report

> Report only. No source code was changed, no migrations were created, and no
> database writes were run. Schema facts below are read from the backend domain
> entities, EF Core configurations, the display-rebuild SQL, the migrations list,
> and the read-only database baseline report
> (`Backend/report/database/current-database-tables-and-relationships-report.md`).

## 0. Verdict

**READY WITH NOTES.**

Everything the feature needs for the **Unique Words** card already exists in the
backend data model and is populated:

- `quran_words` carries its own occurrence id **and** both identity links
  (`unique_tashkeel_word_id`, `unique_simple_word_id`), nullable, and explicitly
  **NULL for ayah markers** (enforced by import validation).
- The two unique-word tables (`quran_words_unique_tashkeel`,
  `quran_words_unique_simple`) already **store** `occurrences_count`,
  `ayahs_count`, and `surahs_count`, plus a representative Uthmani display text
  and first-occurrence fields.
- Filtered indexes already exist on both link columns for drill-down.

The "NOTES" are decisions/small additions, none of which block the feature:

1. `missing_surahs_count` is **not** stored — but it is trivially `114 − surahs_count`. No schema change needed.
2. The unique-**simple** representative Uthmani text is the **first occurrence's** form; a simple key can legitimately map to several Uthmani spellings. This is a display/UX decision, not a data gap.
3. Drill-down grouping (per-surah / per-ayah counts for one word) is correct on the current single-column filtered indexes, but for the highest-frequency words an **optional** composite index would make grouping index-only. Not required for v1.
4. The `words` nav item currently routes to `/words` (placeholder), not `/dashboard/words`. Moving it to `/dashboard/words` (matching `/dashboard/mushaf`) is a small routing change.
5. Pagination + search contracts for the list need to be chosen (offered below).

No migration is required to ship the Unique Words feature. The optional composite
indexes in note 3 are the only schema items worth considering, and only if
profiling shows the highest-frequency drill-downs are slow.

---

## 1. Current schema inventory

### 1.1 Entity / table names

| Concept | Domain entity | Table | Rows (baseline) | Identity key |
| --- | --- | --- | --- | --- |
| Quran word occurrences | `QuranWord` (`Domain/Quran/Words/QuranWord.cs`) | `quran_words` | 83,668 (77,432 readable + 6,236 ayah markers) | PK `id` (natural source id, `ValueGeneratedNever`) |
| Unique words **with tashkeel** (Uthmani identity) | `UniqueTashkeelWord` (`Domain/Quran/Words/Display/UniqueTashkeelWord.cs`) | `quran_words_unique_tashkeel` | 21,294 | PK `id`; natural key `text_uthmani` (unique) |
| Unique words **simple / imlaei** | `UniqueSimpleWord` (`Domain/Quran/Words/Display/UniqueSimpleWord.cs`) | `quran_words_unique_simple` | 14,783 | PK `id`; natural key `word_key_imlaei_simple` (unique) |

Supporting tables (already exist, not the focus of this feature but useful for the
hub and future cards): `quran_words_ordered_tashkeel` / `quran_words_ordered_simple`
(77,432 each), `quran_roots` (1,642), `quran_lemmas`, `quran_stems`, `quran_pos_tags`
(49), `quran_word_morphology` (77,432), `quran_word_morphology_segments` (128,219),
`quran_surahs` (114), `quran_ayahs`, `quran_mushaf_pages` (604).

### 1.2 `quran_words` columns (occurrence table)

Source: `QuranWordConfiguration.cs`. The two identity links the feature relies on **are present**.

| Column | CLR type | Notes |
| --- | --- | --- |
| `id` | `int` (PK, value-generated-never) | Stable natural occurrence id — **use this for highlighting** |
| `location` | `string` (unique) | `surah:ayah:word` |
| `ayah_id` | `int` (FK → `quran_ayahs.id`) | distinct-ayah grouping key |
| `surah_number` | `smallint` | distinct-surah grouping key |
| `ayah_number` | `smallint` | |
| `word_number` | `smallint` | word order in ayah |
| `page_number` | `smallint` (FK → `quran_mushaf_pages`) | |
| `line_number` | `smallint` | |
| `line_word_order` | `smallint` | |
| `qpc_glyph` | `string` | |
| `text_uthmani` | `string` | display text (Uthmani) |
| `text_uthmani_simple` | `string` | |
| `text_imlaei_simple` | `string` | |
| `word_key_imlaei_simple` | `string` | clean imlaei identity key (no tashkeel/marks) |
| `is_ayah_marker` | `bool` | true for end-of-ayah glyph rows |
| **`unique_tashkeel_word_id`** | **`int?`** (nullable) | **→ `quran_words_unique_tashkeel.id`** |
| **`unique_simple_word_id`** | **`int?`** (nullable) | **→ `quran_words_unique_simple.id`** |

> The believed structure in the request is confirmed: `quran_words` has its own
> occurrence id, a unique-tashkeel link id, and a unique-simple/imlaei link id.

**Ayah markers excluded / null for links — confirmed.** The display-rebuild
pipeline (`DisplayWordsSql.cs`) sets both link columns only for readable rows:

- `UpdateUniqueTashkeelLinks` / `UpdateUniqueSimpleLinks` run with `WHERE w.is_ayah_marker = false`.
- After a full reset, `NullQuranWordLinks` nulls every row first.
- Two invariants validate the result every rebuild:
  - `CheckLinkMarkersNullViolations` — ayah markers **must** have both links NULL.
  - `CheckLinkReadableCompleteViolations` — every readable word **must** have both links non-NULL.

So for this feature: filtering on either link id with `is_ayah_marker = false`
(or simply relying on the link being NULL on markers) yields exactly the readable
occurrences of a unique word, never an end marker.

### 1.3 `quran_words_unique_tashkeel` columns

Source: `UniqueTashkeelWordConfiguration.cs`. Identity = `text_uthmani` (Uthmani-with-tashkeel form).

| Column | CLR type | Stored counter? |
| --- | --- | --- |
| `id` | `int` (PK) | — |
| `text_uthmani` | `string` (unique) | display + identity |
| `text_uthmani_simple` | `string` | — |
| `text_imlaei_simple` | `string` | — |
| `occurrences_count` | `int` | **yes** |
| `ayahs_count` | `smallint` | **yes** |
| `surahs_count` | `smallint` | **yes** |
| `first_quran_word_id` | `int` (FK → `quran_words.id`) | first-occurrence |
| `first_location` | `string` | first-occurrence |
| `first_surah_number` | `smallint` | first-occurrence |
| `first_ayah_number` | `smallint` | first-occurrence |
| `first_word_order_in_mushaf` | `int` (unique) | first-occurrence / default sort |
| `first_page_number` | `smallint` | first-occurrence |
| `first_line_number` | `smallint` | first-occurrence |

### 1.4 `quran_words_unique_simple` columns

Source: `UniqueSimpleWordConfiguration.cs`. Identity = `word_key_imlaei_simple` (clean imlaei key). Display = `text_uthmani` / `qpc_glyph` (representative — see §6.2).

| Column | CLR type | Stored counter? |
| --- | --- | --- |
| `id` | `int` (PK) | — |
| `word_key_imlaei_simple` | `string` (unique) | identity / technical key (do **not** show raw) |
| `text_uthmani` | `string` | **representative Uthmani display** |
| `text_uthmani_simple` | `string` | — |
| `text_imlaei_simple` | `string` | — |
| `qpc_glyph` | `string` | representative glyph |
| `occurrences_count` | `int` | **yes** |
| `ayahs_count` | `smallint` | **yes** |
| `surahs_count` | `smallint` | **yes** |
| `first_quran_word_id` | `int` (FK → `quran_words.id`) | first-occurrence |
| `first_location` | `string` | first-occurrence |
| `first_surah_number` | `smallint` | first-occurrence |
| `first_ayah_number` | `smallint` | first-occurrence |
| `first_word_order_in_mushaf` | `int` (unique) | first-occurrence / default sort |
| `first_page_number` | `smallint` | first-occurrence |
| `first_line_number` | `smallint` | first-occurrence |

---

## 2. Are the required counters already stored?

| Counter / field | `quran_words_unique_tashkeel` | `quran_words_unique_simple` | Notes |
| --- | --- | --- | --- |
| occurrences count | ✅ `occurrences_count` | ✅ `occurrences_count` | `COUNT(*)` of readable matches |
| ayahs count (distinct) | ✅ `ayahs_count` | ✅ `ayahs_count` | `COUNT(DISTINCT ayah_id)` |
| surahs count (distinct) | ✅ `surahs_count` | ✅ `surahs_count` | `COUNT(DISTINCT surah_number)` |
| missing surahs count | ❌ not stored | ❌ not stored | trivial: `114 − surahs_count`; compute in handler/DTO |
| representative Uthmani display | ✅ `text_uthmani` (= the identity itself) | ✅ `text_uthmani` + `qpc_glyph` (first occurrence) | simple table also keeps `text_uthmani_simple`, `text_imlaei_simple` |
| first / last occurrence fields | ✅ first-occurrence block (`first_*`) | ✅ first-occurrence block (`first_*`) | **last** occurrence is **not** stored (not needed by this feature) |

How the stored counters are produced (`DisplayWordsSql.InsertUniqueTashkeel` /
`InsertUniqueSimple`): a CTE over readable words (`is_ayah_marker = false`) grouped
by the identity column computes `COUNT(*)`, `COUNT(DISTINCT ayah_id)`,
`COUNT(DISTINCT surah_number)`; a `DISTINCT ON (identity) ... ORDER BY identity,
word_order_in_mushaf` CTE picks the first occurrence and its display text. Counter
correctness is re-validated on every rebuild by `CheckStatMatchViolations` (stored
counts must equal live `GROUP BY` recomputation) and `CheckFirstOccViolations`.

**Conclusion:** the three primary counters and a representative Uthmani display are
already persisted and validated. Only `missing_surahs_count` is derived, and it is
a one-line subtraction.

---

## 3. Recommended implementation approach (counters)

Because the counters are already stored and validated, the safest and simplest
plan is a **hybrid**:

1. **List page → read precomputed counters directly** from
   `quran_words_unique_tashkeel` / `quran_words_unique_simple`. No grouping over
   `quran_words`, no per-card subquery, no N+1. `missing_surahs_count` is computed
   as `114 − surahs_count` in the read mapper.
2. **Single-word drill-downs (surahs / missing surahs / ayahs + highlights) →
   live grouping** from `quran_words` filtered by the selected link id. These are
   one-word, index-supported queries triggered only on click.

Why this over the alternatives:

| Option | Verdict | Reasoning |
| --- | --- | --- |
| Compute all counts live from `quran_words` for the list | ❌ | Re-derives 21k/15k group rows on every page load; wasteful when validated precomputed counts already exist. |
| Add counters to the unique tables | ❌ (already there) | They already store the three counts; adding `missing_surahs_count` is redundant (derivable) and would need a migration + rebuild step. |
| Create new read models / materialized views | ❌ for v1 | The existing rebuildable tables already *are* the read models. A new view adds maintenance with no benefit. |
| **Hybrid: precomputed list + live drill-down** | ✅ **recommended** | Fast list (no N+1), correct drill-downs, zero schema change, reuses validated data and existing indexes. |

No migration required for v1. (Optional drill-down index in §7.)

---

## 4. Proposed backend read API shape (contracts only — not implemented)

Follows the existing read pattern: thin controller → `Get…Handler` (validates +
discriminated outcome) → `I…Reader` interface (Application.Abstractions) → EF reader
(Infrastructure, `AsNoTracking`) → response record, wrapped in
`ApiResponse<T>` (`{ isSuccess, message, data, errors }`, English property names,
Arabic default messages). Routes are resource-oriented and must not leak table names.

Suggested route family: `GET /api/words/unique/...`. A `kind` segment (`tashkeel` |
`simple`) selects identity so the four drill-down endpoints are shared.

### 4.1 List — unique words (tashkeel or simple)

```text
GET /api/words/unique/{kind}
    ?search=<text>&page=<n>&pageSize=<n>&sort=<mushaf|occurrences|alpha>
    kind ∈ { tashkeel, simple }
```

```jsonc
// data
{
  "kind": "tashkeel",
  "page": 1,
  "pageSize": 50,
  "totalCount": 21294,
  "items": [
    {
      "id": 1,
      "displayTextUthmani": "بِسْمِ",   // text_uthmani (representative for simple)
      "occurrencesCount": 3,
      "ayahsCount": 3,
      "surahsCount": 3,
      "missingSurahsCount": 111,        // 114 - surahsCount, computed
      "firstLocation": "1:1:1",
      "firstVerseKey": "1:1"
    }
    // ...
  ]
}
```

Notes: for `kind=simple`, `displayTextUthmani` = representative `text_uthmani`; the
raw `word_key_imlaei_simple` is **not** surfaced to users (kept internal). `search`
behaviour is a decision (§6.4).

### 4.2 Surahs where the selected word occurs (+ per-surah count)

```text
GET /api/words/unique/{kind}/{id}/surahs
```

```jsonc
// data
{
  "kind": "tashkeel",
  "id": 1,
  "surahsCount": 3,
  "surahs": [
    { "surahNumber": 1, "nameArabic": "الفاتحة", "occurrencesInSurah": 1 },
    { "surahNumber": 2, "nameArabic": "البقرة",  "occurrencesInSurah": 1 }
  ]
}
```

Backed by:
`SELECT surah_number, COUNT(*) FROM quran_words WHERE unique_{kind}_word_id = @id AND is_ayah_marker = false GROUP BY surah_number ORDER BY surah_number`,
joined to `quran_surahs.name_arabic`.

### 4.3 Surahs where the selected word does NOT occur

```text
GET /api/words/unique/{kind}/{id}/missing-surahs
```

```jsonc
// data
{
  "kind": "tashkeel",
  "id": 1,
  "missingSurahsCount": 111,
  "surahs": [ { "surahNumber": 3, "nameArabic": "آل عمران" } /* ... */ ]
}
```

Backed by the 114-surah catalog **minus** the occurs-set from §4.2 (anti-join /
`NOT IN`). Cheap (114 rows).

### 4.4 Ayahs containing the selected word (with matched occurrence ids for highlight)

```text
GET /api/words/unique/{kind}/{id}/ayahs?page=<n>&pageSize=<n>
```

```jsonc
// data
{
  "kind": "tashkeel",
  "id": 1,
  "ayahsCount": 3,
  "page": 1,
  "pageSize": 20,
  "ayahs": [
    {
      "ayahId": 1,
      "verseKey": "1:1",
      "surahNumber": 1,
      "ayahNumber": 1,
      "nameArabic": "الفاتحة",
      "matchedQuranWordIds": [1],            // quran_words.id of matches in THIS ayah
      "words": [                              // full ordered ayah for rendering
        { "quranWordId": 1, "wordNumber": 1, "textUthmani": "بِسْمِ", "isAyahMarker": false },
        { "quranWordId": 2, "wordNumber": 2, "textUthmani": "ٱللَّهِ", "isAyahMarker": false }
        // ...
      ]
    }
  ]
}
```

**Highlighting rule (matches the request):** the frontend highlights a word iff
its `quranWordId` is in `matchedQuranWordIds` — never by string matching. Build
plan (no N+1):

1. matched rows:
   `SELECT id, ayah_id, word_number FROM quran_words WHERE unique_{kind}_word_id = @id AND is_ayah_marker = false ORDER BY ayah_id, word_number` → gives matched ids + the distinct, ordered `ayah_id` set.
2. take the requested page of distinct `ayah_id`s.
3. one batched query for those ayahs' words:
   `SELECT id, ayah_id, word_number, text_uthmani, is_ayah_marker FROM quran_words WHERE ayah_id = ANY(@pageAyahIds) ORDER BY ayah_id, word_number`.
4. group in memory; `matchedQuranWordIds` = matched ids from step 1 intersected per ayah.

`quran_ayahs.text_uthmani` (full ayah text) is available too if a non-word-segmented
fallback render is ever wanted, but per-word rows are what enable id-based highlight.

### 4.5 Suggested contract/handler placement

- Contracts (response records): a new Application.Abstractions area, e.g.
  `Quran/Words/Responses/` (sibling to `MushafReader/Responses/`).
- Queries/handlers/outcomes: `Application/Quran/Words/Queries/…` mirroring
  `MushafReader/Queries/GetWordAnalysis`.
- Reader interface: `IUniqueWordsReader` (Abstractions) + `EfUniqueWordsReader`
  (Infrastructure `Persistence/Reads/Quran/Words/`).
- Controllers: `Api/Controllers/Words/…`, thin, returning `ApiResponse<T>` with
  localized `ApiMessages` constants. Invalid `kind`/`id` → controlled `400`/`404`
  via a discriminated outcome (same shape as `GetWordAnalysisOutcome`).

---

## 5. Proposed frontend page structure

New feature folder `src/app/features/words/` (feature-first, per
`FRONTEND_STRUCTURE.md`). Lazy-loaded like `mushaf`.

### 5.1 Hub + routing

- `/dashboard/words` → **Words & Roots hub** page with cards.
  - **Active card:** Unique Words → navigates into the unique-words explorer.
  - **Future cards (disabled / "قريبًا"):** Roots, Lemmas/Dictionary forms, Stems,
    Word Types / POS. Rendered as calm "coming section" cards, not implemented.
- Routing change required: `nav-items.ts` currently maps `words` → `/words`
  (served by the generic placeholder route). Update it to `/dashboard/words`
  (consistent with `/dashboard/mushaf`), add `'words'` to the placeholder-exclusion
  filter in `app.routes.ts`, and register a lazy `words.routes.ts`.

```text
features/words/
  pages/
    words-hub-page/            // /dashboard/words  (cards)
    unique-words-page/         // /dashboard/words/unique  (tabs + list + drill-downs)
  components/
    word-section-card/         // hub card (active or "coming soon")
    unique-words-tabs/         // tashkeel | simple (URL-backed)
    unique-words-search-bar/
    unique-word-card/          // row/card with the 4 clickable counters
    word-surahs-modal/         // surahs where it occurs (+per-surah count)
    word-missing-surahs-modal/ // surahs where it does NOT occur
    word-ayahs-modal/          // ayahs list
    highlighted-ayah/          // renders ayah words, highlights matched ids
  data-access/
    unique-words.api.ts        // returns Observable<ApiResponse<T>>
  state/
    unique-words.facade.ts     // loading/empty/error, tab, search, pagination, selection
  models/
    unique-words.models.ts     // DTOs + view models
  words.routes.ts
```

### 5.2 Unique words page — tabs

Two tabs, each a major section → **child route or query param with stable keys**
(`FRONTEND_STRUCTURE.md` "Tabs and URL State"):

- Tashkeel → `kind=tashkeel` (Uthmani/tashkeel identity).
- Simple/imlaei → `kind=simple` (displayed using representative Uthmani text, never
  the raw `word_key_imlaei_simple`).

Recommended: child routes `/dashboard/words/unique/tashkeel` and
`/dashboard/words/unique/simple` (refresh/back/share friendly). Pagination/search
live in the facade and reflect to query params.

### 5.3 Counters are clickable

Each `unique-word-card` shows occurrences / ayahs / surahs / missing-surahs.
- surahs → `word-surahs-modal` (calls §4.2).
- missing-surahs → `word-missing-surahs-modal` (calls §4.3).
- ayahs → `word-ayahs-modal` (calls §4.4).
- occurrences is informational (not necessarily clickable).

### 5.4 Ayah highlight

`highlighted-ayah` receives the ayah's `words[]` + `matchedQuranWordIds[]` and adds
the highlight class to words whose `quranWordId` is in the matched set — **id-based,
no string replacement**. Missing/empty data shows a controlled empty state; never
fabricate Quranic text (`Quranic data safety`).

### 5.5 Data flow

Page → facade → `unique-words.api.ts` → backend; components consume page-ready
state (`data / isLoading / errorMessage / errors / isEmpty`), per
`API_INTEGRATION_GUIDELINES.md`. Use shared `qd-loading-state` / `qd-empty-state` /
`qd-error-state`. Mushaf text should use **Amiri** (per project memory), not
UthmanicHafs_V22.

---

## 6. Risks / open questions

### 6.1 Stable ids vs natural keys
- `quran_words.id` is a natural source id (`ValueGeneratedNever`) — stable across
  rebuilds → safe to use in `matchedQuranWordIds` for highlighting.
- `quran_words_unique_*.id` is **identity-generated** and the unique tables are
  **rebuildable** (`TRUNCATE … RESTART IDENTITY` in `DisplayWordsSql`). So a unique
  word's `id` is **not guaranteed stable across a display rebuild**. Stable natural
  keys are `text_uthmani` (tashkeel) and `word_key_imlaei_simple` (simple).
  **Decision:** for deep-linkable URLs prefer the stable natural key (or
  `first_word_order_in_mushaf`, also unique) over the surrogate `id`; or accept
  that rebuilds invalidate any saved `id` links. For in-session navigation, `id` is
  fine.

### 6.2 Multiple Uthmani forms per simple word
- A `word_key_imlaei_simple` can map to several Uthmani spellings (different
  tashkeel/orthography). The simple table stores only the **first occurrence's**
  `text_uthmani`/`qpc_glyph` as representative.
- **Open question:** is showing the first-occurrence Uthmani acceptable, or should
  the simple card show "N spellings" / a small list of distinct Uthmani forms? The
  distinct forms are derivable live
  (`SELECT DISTINCT text_uthmani FROM quran_words WHERE unique_simple_word_id=@id`)
  if a richer display is wanted. v1 can ship with the stored representative.

### 6.3 Choosing representative display text
- Current rule = first occurrence in mushaf order (`DISTINCT ON … ORDER BY key,
  word_order_in_mushaf`). Reasonable and already validated. If product wants
  "most frequent spelling" instead, that is a rebuild-SQL change (out of scope
  here) — flag, don't change.

### 6.4 Pagination & search constraints
- List sizes: tashkeel 21,294 / simple 14,783 → pagination is required.
- **Decisions needed:** default `pageSize`; offset vs keyset pagination (keyset on
  `first_word_order_in_mushaf` is cheap and stable); default sort (mushaf order via
  `first_word_order_in_mushaf` is natural). Search target: against `text_uthmani`,
  `text_uthmani_simple`, `text_imlaei_simple`, and/or the simple key — and whether
  prefix vs contains vs tashkeel-insensitive. Tashkeel-insensitive search over
  Uthmani text may need a normalized column/index later (note, not a v1 blocker if
  search is prefix on a simple form).

### 6.5 Required migration or index change
- **No migration is required** for the Unique Words feature.
- Optional only (§7): composite indexes to make the highest-frequency drill-down
  grouping index-only. Add only if profiling justifies it; per `Backend/CLAUDE.md`
  migrations are generated via EF tooling and only on explicit request.

### 6.6 Ayah-marker safety
- Already handled: links are NULL on markers and the drill-down queries filter
  `is_ayah_marker = false`, so markers never appear as occurrences or highlights.

---

## 7. Performance / index notes

Baseline sizes: `quran_words` 83,668 (77,432 readable); unique tashkeel 21,294;
unique simple 14,783; 114 surahs.

**List page (no N+1).** Read straight from the unique tables; counts are columns,
not aggregates. One paginated query per tab, ordered by `first_word_order_in_mushaf`
(unique index) or `occurrences_count`. `missing_surahs_count = 114 − surahs_count`
in the mapper. The list never touches `quran_words`.

**Existing indexes relevant to drill-down** (`QuranWordConfiguration.cs`):

| Index | Definition | Helps |
| --- | --- | --- |
| `IX_quran_words_unique_tashkeel_word_id` | `(unique_tashkeel_word_id)` filtered `is_ayah_marker = false AND unique_tashkeel_word_id IS NOT NULL` | filter by tashkeel link id |
| `IX_quran_words_unique_simple_word_id` | `(unique_simple_word_id)` filtered `is_ayah_marker = false AND unique_simple_word_id IS NOT NULL` | filter by simple link id |
| `IX_…_word_key_imlaei_simple` | `(word_key_imlaei_simple)` filtered readable | identity lookups |
| FK index on `ayah_id` | `(ayah_id)` | batched ayah-words fetch (§4.4 step 3) |
| `IX_quran_words_surah_ayah_word` (+ readable variant) | `(surah_number, ayah_number, word_number)` | ordering / readable filter |

Index sufficiency for the required operations:

- **filter by `unique_tashkeel_word_id` / `unique_simple_word_id`** — ✅ covered by the two filtered indexes.
- **group by surah for one word** (§4.2) — ✅ correct; the filtered index finds the rows, `surah_number` comes from the heap. For low/medium-frequency words this is a handful of rows. For the very highest-frequency words (a few thousand occurrences) it is a few thousand heap fetches per click — acceptable for an on-click drill-down.
- **group by ayah / fetch ayah words for highlight** (§4.4) — ✅ matched-row scan via the filtered index; ayah-words batch via the `ayah_id` index. No N+1 (single `ANY(@ids)` query per page of ayahs).
- **retrieve `quran_words.id` for highlighting** — ✅ the id is returned by the same matched-row scan.

**Optional composite indexes (only if profiling shows hot, high-frequency drill-downs):**

```text
(unique_tashkeel_word_id, surah_number)   -- index-only per-surah grouping
(unique_simple_word_id,  surah_number)
(unique_tashkeel_word_id, ayah_id)        -- index-only distinct-ayah enumeration
(unique_simple_word_id,  ayah_id)
```

These let the grouping/enumeration be index-only (no heap), helping only the
heaviest words. They are **not** needed for correctness or for typical words and
should be added via EF tooling only on explicit request after measurement.

---

## 8. Summary tables

### 8.1 Believed-vs-actual (request verification)

| Claim in request | Actual | Status |
| --- | --- | --- |
| `quran_words` has its own occurrence id | `id` PK, natural, value-generated-never | ✅ |
| `quran_words` has a unique tashkeel link id | `unique_tashkeel_word_id` (`int?`) | ✅ |
| `quran_words` has a unique simple/imlaei link id | `unique_simple_word_id` (`int?`) | ✅ |
| Ayah markers excluded/null for links | NULL on markers; enforced by 2 invariants | ✅ |
| Tashkeel identity for counts/lists/highlight | `unique_tashkeel_word_id` | ✅ usable |
| Simple identity for counts/lists/highlight | `unique_simple_word_id` | ✅ usable |
| Highlight by `quran_words.id`, not string replace | id returned per matched occurrence | ✅ supported |

### 8.2 Stored counter availability

| Counter | Tashkeel table | Simple table | If absent |
| --- | --- | --- | --- |
| occurrences | ✅ stored | ✅ stored | — |
| ayahs (distinct) | ✅ stored | ✅ stored | — |
| surahs (distinct) | ✅ stored | ✅ stored | — |
| missing surahs | ❌ | ❌ | derive `114 − surahs_count` |
| representative Uthmani display | ✅ `text_uthmani` | ✅ `text_uthmani` (+`qpc_glyph`) | — |
| first occurrence block | ✅ | ✅ | — |
| last occurrence | ❌ | ❌ | not needed by this feature |

### 8.3 Decisions needed before build

1. URL style for tabs: child routes (recommended) vs query param `kind`.
2. Move `words` nav from `/words` → `/dashboard/words` (recommended).
3. Pagination: page size + offset vs keyset; default sort.
4. Search: which columns, prefix vs contains, tashkeel-insensitive or not.
5. Simple-word display: stored first-occurrence Uthmani (v1) vs show distinct spellings.
6. Stable identifier in URLs: surrogate `id` (rebuild-volatile) vs natural key / `first_word_order_in_mushaf`.
7. Optional composite drill-down indexes: defer until measured.

---

## 9. Files inspected (provenance)

- `Backend/domain/QuranDashboard.Domain/Quran/Words/QuranWord.cs`
- `Backend/domain/QuranDashboard.Domain/Quran/Words/Display/UniqueTashkeelWord.cs`, `UniqueSimpleWord.cs`, `OrderedTashkeelWord.cs`, `OrderedSimpleWord.cs`
- `Backend/domain/QuranDashboard.Domain/Quran/Ayahs/Ayah.cs`
- `Backend/infrastructure/.../Persistence/Configurations/Quran/QuranWordConfiguration.cs`
- `Backend/infrastructure/.../Persistence/Configurations/Quran/Words/Display/UniqueTashkeelWordConfiguration.cs`, `UniqueSimpleWordConfiguration.cs`
- `Backend/infrastructure/.../Persistence/DataPipelines/Quran/Words/DisplayRebuilding/DisplayWordsSql.cs`
- `Backend/infrastructure/.../Persistence/QuranDashboardDbContext.cs`
- `Backend/infrastructure/.../Persistence/Reads/Quran/MushafReader/EfWordAnalysisReader.cs`
- `Backend/.../MushafReader/Responses/WordAnalysisResponse.cs`, `MushafPageResponse.cs`, `MushafSurahCatalogResponse.cs`
- `Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs`; `Controllers/MushafReader/Catalogs/MushafSurahCatalogController.cs`
- `Backend/.architecture/API_GUIDELINES.md`
- `Backend/report/database/current-database-tables-and-relationships-report.md` (row counts)
- Migrations list (`…/Migrations/`): `AddUniqueSimpleImlaeiIdentity`, `AddQuranWordIdentityLinks`, `WordsDisplayTables`, etc.
- Frontend: `src/app/app.routes.ts`, `core/navigation/nav-items.ts`, `features/mushaf/mushaf.routes.ts`, `core/data-access/api-response.model.ts`, `.architecture/FRONTEND_STRUCTURE.md`, `.architecture/API_INTEGRATION_GUIDELINES.md`
