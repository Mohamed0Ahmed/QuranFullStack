# Feature 014 — Words Hub + Unique Words Explorer: Planning Report

> Planning report only. No code, migrations, or DB writes. Read-only inspection
> only. Builds on the locked Feature 013 capability work and the now-implemented
> deterministic unique-word IDs. Companions:
> `docs/feature-013-words-roots-explorer/feature-013-unique-words-capability-report.md`,
> `docs/feature-013-words-roots-explorer/feature-013-deterministic-unique-word-ids-plan.md`,
> `Backend/report/feature-013-deterministic-unique-word-ids/002-reset-reseed-acceptance-report.md`.

## 0. Verdict

**READY WITH NOTES.**

All data and patterns the feature needs already exist and are verified:

- **Deterministic, URL-safe IDs (prerequisite met).** Feature 013 is implemented
  and acceptance-verified: `quran_words_unique_tashkeel.id = first_quran_word_id`
  and `quran_words_unique_simple.id = first_quran_word_id`, stable byte-for-byte
  across rebuilds. This **resolves the earlier "id is rebuild-volatile" risk** —
  the deterministic id is now safe to use directly in URLs and as the selection key.
- **Counters are precomputed and validated** (`occurrences_count`, `ayahs_count`,
  `surahs_count`) on both unique tables; `لم يذكر في = 114 − surahs_count` is a
  trivial derivation. The list needs no live grouping.
- **Drill-downs** filter `quran_words` by the deterministic link id
  (`unique_tashkeel_word_id` / `unique_simple_word_id`), which are NULL on ayah
  markers and covered by filtered indexes. Highlighting uses `quran_words.id`.
- **Read/UX patterns to mirror** exist: the thin controller → handler (discriminated
  outcome) → `IReader` → EF reader → `ApiResponse<T>` chain; and the Mushaf
  `mushaf-word` highlight-by-computed-class pattern.

The NOTES are decisions to confirm (none blocking), collected in §9:

1. No paged read API exists yet → introduce one small `PagedResult<T>` contract.
2. Route shape for tabs (child routes) and drill-down (modal + query-param state).
3. Default sort, and search normalization location (server-side) for both kinds.
4. Use deterministic `id` in URLs (recommended) vs natural keys.
5. No new tables and **no new indexes required for v1**; optional composite indexes
   only if profiling shows high-frequency drill-down heap cost.

---

## 1. Current-state inventory (verified read-only)

### 1.1 Backend data model

| Concept | Table | Key columns | Rows |
| --- | --- | --- | --- |
| Occurrences | `quran_words` | `id` (deterministic natural PK, `ValueGeneratedNever`), `ayah_id`, `surah_number`, `ayah_number`, `word_number`, `text_uthmani`, `word_key_imlaei_simple`, `is_ayah_marker`, `unique_tashkeel_word_id` (`int?`), `unique_simple_word_id` (`int?`) | 83,668 (77,432 readable + 6,236 markers) |
| Unique — tashkeel | `quran_words_unique_tashkeel` | `id` (= `first_quran_word_id`), `text_uthmani` (unique identity + display), `occurrences_count`, `ayahs_count`, `surahs_count`, `first_*` | 21,294 |
| Unique — simple | `quran_words_unique_simple` | `id` (= `first_quran_word_id`), `word_key_imlaei_simple` (unique identity, **not** for display), `text_uthmani` + `qpc_glyph` (representative display), `text_uthmani_simple`, `text_imlaei_simple`, `occurrences_count`, `ayahs_count`, `surahs_count`, `first_*` | 14,783 |
| Ordered display | `quran_words_ordered_tashkeel` / `_simple` | keyed by `quran_word_id`; not needed by this feature | 77,432 each |

Invariants enforced every rebuild (Feature 013 acceptance, all green): `UNQ-ID-DETERMINISTIC`,
`UNQ-ID-UNIQUE`, `LINK-READABLE-COMPLETE` (every readable word has both links),
`LINK-MARKERS-NULL` (markers have NULL links), `LINK-RESOLVES`, `LINK-CONSISTENT`
(tashkeel link ⇒ `u.text_uthmani = w.text_uthmani`; simple link ⇒ key match),
`STAT-MATCH`, `SRC-UNTOUCHED`.

Relevant indexes on `quran_words`:
- `IX_quran_words_unique_tashkeel_word_id` — `(unique_tashkeel_word_id)` filtered `is_ayah_marker = false AND … IS NOT NULL`
- `IX_quran_words_unique_simple_word_id` — same for simple
- FK index on `ayah_id`; `(surah_number, ayah_number, word_number)`
- **No DB FK** from `quran_words.unique_*_word_id` to the unique tables (only the filtered indexes).

### 1.2 Backend read/API patterns

- Flow: thin controller → `Get…Handler` (validates input, returns a discriminated
  `…Outcome` for 200/400/404 without exceptions) → `I…Reader` (Application.Abstractions)
  → `Ef…Reader` (Infrastructure, `AsNoTracking`) → response `record` → wrapped in
  `ApiResponse<T>` (`{ isSuccess, message, data, errors }`; English property names,
  Arabic localized `message`).
- Messages: `ApiMessages` static class, Arabic constants, feature-prefixed
  (`MushafPageLoaded`, `MushafInvalidWordLocation`, …).
- Routes: resource-oriented, kebab/segment style (`/api/mushaf/pages/{pageNumber}`,
  `/api/mushaf/surahs`). No table names leaked.
- **No pagination contract exists** — all current reads are page-number based
  (Mushaf) or full catalogs. Feature 014 is the first paged list API → introduce a
  small generic `PagedResult<T>`.

### 1.3 Frontend structure

- Feature-first under `src/app/features/<feature>/`. Mushaf is the reference feature
  (`pages/`, `components/`, `data-access/`, `state/`, `models/`, `mushaf.routes.ts`).
- Routing: `app.routes.ts` lazy-loads `/dashboard` and `/dashboard/mushaf`; other
  nav items render a generic `placeholder-page`. **`nav-items.ts` currently maps
  `words` → `/words`** (placeholder), labelled `الكلمات والجذور`.
- `ApiResponse<T>` model in `core/data-access/`. Facade/store + Angular Signals
  pattern; `OnPush` standalone components with `.html`/`.scss` files.
- Shared UI primitives (UI_STYLE_SYSTEM): `qd-page`, `qd-shell`, `qd-card` /
  `qd-card-title`, `qd-btn` / `qd-btn-primary` / `qd-btn-ghost`, `qd-input`,
  `qd-select`, `qd-badge`, `qd-table`, `qd-modal`, `qd-toolbar`, `qd-section-title`,
  and the three states `qd-loading-state` / `qd-empty-state` / `qd-error-state`.
  (No `qd-tabs` primitive — tabs compose buttons/links + the style tokens.)
- Reusable helpers to mirror: `mushaf-word` highlight pattern (input → `computed`
  boolean → CSS class, `OnPush`); `toMushafWordDisplayText` (Amiri-safe rendering);
  `arabic-search-normalize.ts` (`normalizeArabicForSearch` / `arabicSearchIncludes`
  — lowercases, strips diacritics `ً-ٰٟ`, folds hamza/alef/waw/ya).
  Mushaf text font is **Amiri** (project memory).

---

## 2. Product scope

In this feature: the `/dashboard/words` **hub** + the **Unique Words explorer**
(two kinds: with-tashkeel and simple/imlaei), with the four counts and three
drill-downs (surahs, missing surahs, ayahs with occurrence highlighting). Hub shows
one active card (`الكلمات الفريدة`) and four "coming soon" cards (`الجذور`,
`الصيغة المعجمية`, `الأصل الصرفي`, `أنواع الكلمة`). Arabic-first / RTL; app context
"المنهج القرآني". Raw technical keys (`word_key_imlaei_simple`) are never the main
user-facing label.

---

## 3. Proposed backend API (contracts only — not implemented)

A new thin area `Api/Controllers/Words/…`, queries under
`Application/Quran/Words/Queries/…`, contracts under
`Application.Abstractions/Quran/Words/Responses/…`, reader `IUniqueWordsReader` +
`EfUniqueWordsReader`. Localized `ApiMessages` (`Words*`). A shared
`PagedResult<T>` contract (first use; keep it minimal and reusable).

`kind` is a path segment `{kind}` ∈ `tashkeel | simple`; `{id}` is the deterministic
unique-word id. Invalid `kind` → 400; unknown `id` → 404 (discriminated outcome,
mirroring `GetWordAnalysisOutcome`).

```text
GET /api/words/unique/{kind}?search=&sort=&page=&pageSize=
GET /api/words/unique/{kind}/{id}                       (summary; for deep-link/refresh)
GET /api/words/unique/{kind}/{id}/surahs
GET /api/words/unique/{kind}/{id}/missing-surahs
GET /api/words/unique/{kind}/{id}/ayahs?page=&pageSize=
```

### 3.1 List

```jsonc
// PagedResult<UniqueWordListItem>
{
  "page": 1, "pageSize": 50, "totalCount": 21294,
  "items": [
    {
      "id": 1,                       // deterministic = first quran_words.id
      "displayTextUthmani": "بِسْمِ", // tashkeel: text_uthmani; simple: representative text_uthmani
      "occurrencesCount": 3,
      "ayahsCount": 3,
      "surahsCount": 3,
      "missingSurahsCount": 111,     // 114 - surahsCount (computed in mapper)
      "firstVerseKey": "1:1",
      "firstLocation": "1:1:1"
    }
  ]
}
```
`search` (optional), `sort` (default mushaf order = `first_word_order_in_mushaf`;
also `occurrences` desc, `alpha`). For `kind=simple`, `word_key_imlaei_simple` is
**not** returned as the display label.

### 3.2 Surahs where mentioned

```jsonc
{ "id": 1, "kind": "tashkeel", "surahsCount": 3,
  "surahs": [ { "surahNumber": 1, "nameArabic": "الفاتحة", "occurrencesInSurah": 1 } ] }
```
SQL: `SELECT surah_number, COUNT(*) FROM quran_words WHERE unique_{kind}_word_id=@id
AND is_ayah_marker=false GROUP BY surah_number ORDER BY surah_number`, joined to
`quran_surahs.name_arabic`.

### 3.3 Surahs where NOT mentioned

```jsonc
{ "id": 1, "kind": "tashkeel", "missingSurahsCount": 111,
  "surahs": [ { "surahNumber": 2, "nameArabic": "البقرة" } ] }
```
The 114-surah catalog **minus** the occurs-set from §3.2 (anti-join). Cheap.

### 3.4 Ayahs containing the word (with matched occurrence ids)

```jsonc
// PagedResult<AyahWithMatches>
{
  "page": 1, "pageSize": 20, "totalCount": 3,   // totalCount = ayahsCount
  "items": [
    {
      "ayahId": 1, "verseKey": "1:1", "surahNumber": 1, "ayahNumber": 1,
      "surahNameArabic": "الفاتحة",
      "matchedQuranWordIds": [1],               // quran_words.id of matches in THIS ayah
      "words": [
        { "quranWordId": 1, "wordNumber": 1, "textUthmani": "بِسْمِ", "isAyahMarker": false },
        { "quranWordId": 2, "wordNumber": 2, "textUthmani": "ٱللَّهِ", "isAyahMarker": false }
      ]
    }
  ]
}
```
**Highlight rule:** frontend highlights a word iff its `quranWordId ∈
matchedQuranWordIds` — never string replacement. Build (no N+1): (1) matched rows
`SELECT id, ayah_id, word_number FROM quran_words WHERE unique_{kind}_word_id=@id
AND is_ayah_marker=false ORDER BY ayah_id, word_number` → matched ids + ordered
distinct `ayah_id`s; (2) take the page of `ayah_id`s; (3) one batched
`SELECT … FROM quran_words WHERE ayah_id = ANY(@pageAyahIds) ORDER BY ayah_id,
word_number`; (4) group in memory, set `matchedQuranWordIds` per ayah.

---

## 4. Proposed frontend routes / components / state

### 4.1 Routes (lazy `features/words/words.routes.ts`)

- `nav-items.ts`: change `words` route `/words` → `/dashboard/words`; add `'words'`
  to the placeholder-exclusion filter in `app.routes.ts`; register the lazy feature.
- `/dashboard/words` → **WordsHubPage**.
- `/dashboard/words/unique/tashkeel` and `/dashboard/words/unique/simple` →
  **UniqueWordsPage** (kind from the route; tabs = major sections → **child routes**,
  stable keys `tashkeel`/`simple`). `/dashboard/words/unique` → redirect to
  `tashkeel`.
- List state in **query params**: `?search=&sort=&page=`.
- Drill-down in **query params** on the same route (modal):
  `?word=<id>&view=surahs|missing|ayahs&ap=<ayahPage>` — refresh/share/back friendly,
  keeps list context behind the modal.

### 4.2 Components (`features/words/`)

```text
pages/
  words-hub-page/            // /dashboard/words
  unique-words-page/         // /dashboard/words/unique/:kind (shell/orchestrator)
components/
  word-section-card/         // hub card; active or "coming soon"
  unique-words-tabs/         // tashkeel | simple (links, stable keys)
  unique-words-search-bar/
  unique-word-card/          // display word + 4 count chips
  word-count-chip/           // label + value, clickable
  word-drilldown-modal/      // hosts the three views; reads query params
  surah-occurrences-list/    // السور (+ per-surah counts)
  missing-surahs-list/       // لم يذكر في
  ayah-matches-list/         // الآيات (paginated)
  highlighted-ayah/          // renders words; highlights matched ids (mushaf-word pattern)
data-access/ unique-words.api.ts      // returns Observable<ApiResponse<T>>
state/       unique-words.facade.ts   // signals: list, states, kind, search, sort, page, selectedWordId, drilldown
models/      unique-words.models.ts
words.routes.ts
```

### 4.3 State (facade, Signals)

Owns: kind, search, sort, page, list + `PagedResult` meta, loading/empty/error,
selected word id, drill-down view + its data/paging, and URL sync. Unwraps
`ApiResponse<T>` (components consume page-ready state). Child components receive
inputs and emit outputs; they do not call the API directly. `highlighted-ayah`
reuses the Mushaf approach: a `Set<number>` of matched ids + a `computed` per word.

---

## 5. UX plan (Arabic-first / RTL)

- **Hub (`/dashboard/words`):** calm, reverent cards under a `qd-section-title`.
  Active card **`الكلمات الفريدة`** → `/dashboard/words/unique/tashkeel`. Four
  future cards rendered with a `قريبًا` badge, visibly disabled (not links):
  `الجذور`, `الصيغة المعجمية`, `الأصل الصرفي`, `أنواع الكلمة`. Compose `qd-card`;
  no kitsch, no gamification.
- **Unique words page:** two tabs `بالتشكيل` / `إملائي (بدون تشكيل)`. Below: a
  search field (`بحث`), a sort control (`ترتيب`), then a responsive list of
  `unique-word-card`s. Each card shows the **Uthmani display word** large (Amiri),
  and four clickable count chips:
  - `المواضع` = occurrencesCount (informational; not a drill-down)
  - `الآيات` = ayahsCount → opens ayahs drill-down
  - `السور` = surahsCount → opens surah-occurrences drill-down
  - `لم يذكر في` = missingSurahsCount → opens missing-surahs drill-down
- **Drill-down modal (`qd-modal`):** title = the display word; segmented views for
  السور / لم يذكر في / الآيات. السور lists surah name + `عدد المواضع` per surah;
  لم يذكر في lists the absent surahs; الآيات paginates ayahs, each rendered with
  matched occurrences highlighted (id-based). Close returns to the list unchanged.
- **States:** `qd-loading-state`, `qd-empty-state` (e.g. "لا توجد نتائج"),
  `qd-error-state` (preserve a safe backend message). Never fabricate Quranic text;
  missing data → controlled empty/missing state.
- **RTL/a11y:** logical properties, visible focus, chips are real buttons with
  `aria-label` (e.g. "السور: 3")، sufficient contrast, no color-only meaning,
  accessible disabled state on "coming soon" cards.

---

## 6. Data / query / performance plan

- **List:** read the unique table directly; counts are columns. One paged query per
  tab ordered by `first_word_order_in_mushaf` (unique index) or `occurrences_count`.
  `missingSurahsCount = 114 − surahs_count` in the mapper. **Never touches
  `quran_words` → no N+1, no per-card grouping.**
- **Drill-downs:** live grouping filtered by the deterministic link id; the filtered
  indexes `IX_quran_words_unique_{kind}_word_id` cover the filter. Ayahs use the
  `ayah_id` index for the batched fetch. No N+1.
- **Ayah-marker safety:** queries filter `is_ayah_marker = false` and links are NULL
  on markers, so markers never appear as occurrences or in `matchedQuranWordIds`.
- **Pagination:** offset/limit is fine for v1 (21,294 / 14,783 rows, index-ordered).
  Keyset on `first_word_order_in_mushaf` is a future option; note in §9.
- **Search:** tashkeel-insensitive. Recommended server-side normalization mirroring
  `arabic-search-normalize` rules; v1 can match against the already-simplified
  columns (`text_uthmani_simple` / `text_imlaei_simple`) with `ILIKE`. A normalized
  generated column + index is a future option only if profiling requires it.
- **No new tables.** **No new indexes required for v1.** Optional composite indexes
  `(unique_{kind}_word_id, surah_number)` / `(…, ayah_id)` only if profiling shows
  the highest-frequency words' drill-down heap-fetch cost is material.

---

## 7. Implementation phases

1. **Backend contracts/API/reads/tests** — `PagedResult<T>`; `IUniqueWordsReader` +
   `EfUniqueWordsReader`; queries/handlers/outcomes; controllers; `Words*` messages;
   reader + handler tests.
2. **Frontend routes/facade/service/models** — nav + lazy `words.routes.ts`;
   `unique-words.api.ts`; `unique-words.facade.ts`; models; URL/query sync.
3. **Hub UI** — `words-hub-page`, `word-section-card` (active + 4 coming-soon).
4. **Unique words list/search/pagination** — `unique-words-page`, tabs, search bar,
   `unique-word-card` + count chips; list paging.
5. **Drill-downs** — surahs, missing surahs, ayahs; `highlighted-ayah` id-based
   highlighting; modal + query-param state.
6. **Polish** — accessibility, RTL, loading/empty/error, Amiri rendering, copy.
7. **Build/tests/review/report** — `qd-build`, frontend + backend tests, engineering
   review, completion report.

---

## 8. Test strategy

**Backend (xUnit + Testcontainers PostgreSQL, real data via the canonical-source gate):**
- List: correct counts surfaced from precomputed columns; `missingSurahsCount = 114 − surahsCount`; `totalCount` correct.
- Pagination: page/pageSize bounds, ordering stability, last-page behavior.
- Search/sort: normalized matching for both kinds; sort variants.
- Surahs / missing surahs: per-surah occurrence counts; occurs ∪ missing = 114, disjoint.
- Ayahs + highlighting: `matchedQuranWordIds` are exactly the readable occurrences of the selected id in each ayah; **ayah markers excluded** (never in matched ids, never highlighted); no N+1 (assert via batched access shape).
- Determinism reliance: a known word resolves the same id across rebuilds (leans on Feature 013 guarantees).
- Validation: invalid `kind` → 400; unknown `id` → 404 (discriminated outcome).
- Quranic-data safety: no invented text; markers/missing handled as controlled.

**Frontend (Vitest / Angular builder — honor the `VITEST_MAX_FORKS` cap, project memory):**
- Facade unwraps `ApiResponse<T>`; loading/empty/error transitions; both transport and `isSuccess===false` failures.
- `highlighted-ayah`: highlights exactly the words whose `quranWordId ∈ matchedQuranWordIds`; no string matching.
- Count chips emit the correct drill-down intent; modal opens/closes; URL/query sync for tab/search/page/selected word.
- Hub: active card routes; coming-soon cards are disabled/non-navigable.
- RTL/a11y basics: focus visibility, chip `aria-label`, no color-only meaning.

---

## 9. Risks and decisions needed

| # | Decision | Recommendation |
| --- | --- | --- |
| D1 | Tab route shape | **Child routes** `/dashboard/words/unique/{tashkeel|simple}` (major sections, stable keys). |
| D2 | Drill-down: modal vs page | **Modal** with **query-param** state (`word`,`view`,`ap`) — shareable/refresh-safe, keeps list context. |
| D3 | Default sort | **Mushaf order** (`first_word_order_in_mushaf`); offer `occurrences` desc and `alpha`. |
| D4 | Search behavior | Server-side, tashkeel/hamza-insensitive (mirror `arabic-search-normalize`); v1 match simplified columns via `ILIKE`; prefix vs contains is a sub-decision (recommend contains for discoverability). |
| D5 | URL identifier | **Deterministic `id`** (stable since Feature 013, URL-clean) over natural keys (`text_uthmani` / `word_key_imlaei_simple` are long/encoding-heavy). |
| D6 | Pagination style | **Offset/limit** for v1; keyset on `first_word_order_in_mushaf` later if needed. |
| D7 | Extra indexes | **None for v1**; optional `(unique_{kind}_word_id, surah_number)` / `(…, ayah_id)` only if profiling shows heap-fetch cost on the busiest words. |
| D8 | `PagedResult<T>` location | Shared API/Application contract (first use); keep minimal (`page`,`pageSize`,`totalCount`,`items`). |
| D9 | Simple representative display | Use stored first-occurrence `text_uthmani`/`qpc_glyph`; showing "N spellings" is out of scope for v1. |

---

## 10. Out of scope

Roots explorer; lemmas explorer; stems explorer; POS-category explorer (future hub
cards only); changing word-identity generation; changing Quran source data; changing
morphology / i3rab / tafsir / translation data; audio; global search.

---

## 11. Files / docs inspected

**Backend:** `domain/.../Words/QuranWord.cs`, `.../Display/{UniqueTashkeelWord,UniqueSimpleWord,OrderedTashkeelWord}.cs`;
`.../Persistence/Configurations/Quran/QuranWordConfiguration.cs`,
`.../Words/Display/{UniqueTashkeelWord,UniqueSimpleWord}Configuration.cs`;
`.../DataPipelines/Quran/Words/DisplayRebuilding/{DisplayWordsSql,SqlDisplayWordsRebuilder}.cs`;
`application/.../Words/DisplayRebuilding/DisplayWordsInvariants.cs`;
read pattern: `.../MushafReader/Queries/GetWordAnalysis/*`, `.../Responses/{WordAnalysisResponse,MushafSurahCatalogResponse,MushafPageResponse}.cs`,
`infrastructure/.../Reads/Quran/MushafReader/EfWordAnalysisReader.cs`;
`api/.../Contracts/ApiResponse.cs`, `Common/ApiMessages.cs`, `Controllers/MushafReader/Catalogs/MushafSurahCatalogController.cs`;
`.architecture/API_GUIDELINES.md`; `report/database/current-database-tables-and-relationships-report.md`;
`report/feature-013-deterministic-unique-word-ids/002-reset-reseed-acceptance-report.md`
(deterministic-id verification); grep confirmed **no existing pagination DTO**.

**Frontend:** `src/app/app.routes.ts`, `core/navigation/nav-items.ts`,
`core/data-access/api-response.model.ts`, `features/mushaf/mushaf.routes.ts`,
`features/mushaf/components/mushaf-word/mushaf-word.component.ts` (+ `.html`),
`features/mushaf/components/mushaf-line/*`, `features/mushaf/utils/arabic-search-normalize.ts`,
`features/mushaf/components/surah-jump-picker/*`, `shared/ui/*`,
`.architecture/{FRONTEND_STRUCTURE,API_INTEGRATION_GUIDELINES,UI_STYLE_SYSTEM}.md`.

**Docs:** `docs/feature-013-words-roots-explorer/feature-013-unique-words-capability-report.md`,
`…/feature-013-deterministic-unique-word-ids-plan.md`; `PRODUCT.md`, `DESIGN.md` (context).
