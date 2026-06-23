# Feature 015 — Quran Roots Explorer — Combined Backend + Frontend Implementation Plan

> **Planning report only.** No code, no Spec Kit files, no migrations, no source-file changes,
> nothing committed. This document is the input to a later Spec Kit `specify` → `plan` → `tasks`
> run. Where any earlier report conflicts with the **read-only verification report**, this plan
> follows the verification report — in particular, **lemmas use morphology co-occurrence semantics**.

**Inputs read:** capability analysis report, read-only verification report, frontend UX contract
report; the Feature 014 backend (controller, handlers, `IUniqueWordsReader`/`EfUniqueWordsReader`,
`CachedUniqueWordsReader`, cache keys, DI, DTOs, tests/fixtures) and frontend (`words.routes.ts`,
models, `unique-words.api.ts`, url-sync, drilldown facade, `ApiResponseCache`, table/highlighted-ayah/
count-chip, shared `qd-pagination`, shared deep-link helper); backend `API_GUIDELINES.md`,
`LOGGING_GUIDELINES.md`, `BACKEND_STRUCTURE.md`, `CLEAN_ARCHITECTURE.md`; frontend
`FRONTEND_STRUCTURE.md`, `API_INTEGRATION_GUIDELINES.md`, `UI_STYLE_SYSTEM.md` references.

---

## 1. Executive summary and final verdict

Feature 015 adds a **read-only Quran Roots Explorer** at `/dashboard/words/roots`: a split-screen
screen with a CDK roots table (8 summary columns) on the main area and a **persistent,
independent-scroll details side panel** (no modal) with tabs الكلمات / الآيات / السور / الصيغ المعجمية
/ الأصول الصرفية. It is a structural sibling of Feature 014 (Unique Words Explorer) and reuses its
proven backend read-model/cache/logging pattern and its frontend highlighting, surah/ayah lists,
count-chip, shared pagination, API-response cache, and URL-sync patterns.

Three independent analyses already cleared the path: the data is fully present, **no migration or
index is required** (verified by query plans/timings), all aggregate counts are sane and cheap
(~30–115 ms for the whole 1,642-root summary), every root-bearing word has both unique-word links
(0 missing), and the lemma-count metric is locked to **co-occurrence** (`DISTINCT lemma_id` from
`quran_word_morphology WHERE root_id = X`), which equals the precomputed `distinct_lemmas_count` for
all 1,642 roots.

**Final verdict: READY_FOR_SPEC_KIT.** All decisions are locked; remaining items (§10) are minor
copy/UX confirmations that do not block specification.

---

## 2. Scope and non-goals

### In scope

- Read-only backend Roots API under the existing `words` area (list + 7 detail reads).
- Compute-once + cache-whole-list roots summary; per-root detail reads with namespaced caching.
- Angular Roots Explorer page (table + persistent side panel) inside `features/words/`.
- URL state for search, sort, page, selected root, view, sub-views, and detail pagination.
- Word-ID-based ayah highlighting (reuse F014 component and DTO shape).
- Deep links from simple/tashkeel word rows into the existing Unique Words detail flow.
- Structured logging + milestone-based backend and frontend tests.

### Non-goals (explicit)

- **No writes, no importer, no new data pipeline, no Quran text mutation.**
- **No migrations and no new indexes** (verification found none required; do not add without new
  measured evidence).
- **No lemma/stem detail pages** — lemmas/stems are static display lists now, link-ready by ID only.
- **No `نظرة عامة` tab**; the table is the summary surface.
- **No modal** as the primary desktop detail experience (drawer allowed only as a narrow-screen
  adaptation).
- No changes to Feature 014 behavior; it is reused as the navigation target for word rows.
- No global cache reconfiguration (no new size limits / eviction / default expirations).
- No POS/segment-level views, no verb-feature exploration, no lemma/meaning search.

---

## 3. Locked product/UX decisions

- **Route:** `/dashboard/words/roots` (one child route under `words`).
- **Desktop:** split-screen — CDK roots table (main) + persistent details panel (inline-end) with its **own scroll container**.
- **Narrow screens:** details panel becomes a drawer/sheet (focus-trapped, `Esc` to dismiss, focus returns to the originating control). Drawer is a responsive adaptation, not the desktop default.
- **No overview tab; no modal** for the main detail experience.
- **No backend IDs in visible UI**; use UI (page-relative) row numbers. IDs appear only in the URL (`root=…`) and in DTOs for deep links.
- **Lemmas/stems are static display lists now** (no fake buttons/links); DTOs carry `lemmaId`/`stemId` so they become link-ready when their detail routes exist.
- **Word rows (simple/tashkeel) deep-link into Feature 014** using the unique word ID; the destination shows that word’s **global** counts (a per-root word may also occur under other roots).
- **Shared pagination:** use the existing `qd-pagination` (`src/app/shared/ui/pagination`). Do **not** reintroduce the old `unique-words-list-pagination`.
- **Sort keys (F014 style):** `mushaf-order` (default), `occurrences`, `alpha`.
- **Defaults:** row-select default `view=ayahs`; words sub-view default `simple`; surahs sub-view default `mentioned`.

### Locked Arabic labels

| Surface | Label |
|---|---|
| Columns | الجذر · المواضع · الآيات · السور · كلمات بدون تشكيل · كلمات بالتشكيل · الصيغ المعجمية · الأصول الصرفية |
| Panel tabs | الكلمات · الآيات · السور · الصيغ المعجمية · الأصول الصرفية |
| Words sub-views | بدون تشكيل · بالتشكيل |
| Surahs sub-views | ورد فيها · لم يذكر فيها |
| Empty panel (no selection) | اختر جذرًا لعرض تفاصيله |

> Terminology guard: the table column is **`كلمات بالتشكيل`**, never `الصيغ بالتشكيل` (which would
> collide with `الصيغ المعجمية`/lemmas). `الكلمات` is used only as the panel parent tab.

---

## 4. Backend plan

Clean Architecture placement mirrors F014 exactly (per `BACKEND_STRUCTURE.md` / `CLEAN_ARCHITECTURE.md`):
contracts/DTOs + reader interface in `Application.Abstractions/Quran/Words/Roots`; query handlers in
`Application/Quran/Words/Roots/Queries/…`; EF reader + cache decorator + DI in
`Infrastructure/Persistence/Reads/…` and `Infrastructure/Caching/…`; thin controller in
`Api/Controllers/Words`.

### 4.1 APIs (all `GET`, all return `ApiResponse<T>`, route base `api/words/roots`)

| Endpoint | Params | Response `data` | Cached | Paged |
|---|---|---|---|---|
| `GET /api/words/roots` | `search?`, `sort?`, `page?`, `pageSize?` | `PagedResult<RootListItemDto>` | whole-list (see 4.3) | yes |
| `GET /api/words/roots/{id}` | — | `RootSummaryDto` | yes | no |
| `GET /api/words/roots/{id}/words/{wordKind}` | `wordKind`∈`simple\|tashkeel`, `page?`, `pageSize?` | `PagedResult<RootWordItemDto>` | yes | yes |
| `GET /api/words/roots/{id}/ayahs` | `page?`, `pageSize?` | `PagedResult<RootAyahMatchDto>` | yes | yes |
| `GET /api/words/roots/{id}/surahs` | — | `RootSurahsResponse` | yes | no (whole ≤114) |
| `GET /api/words/roots/{id}/missing-surahs` | — | `RootMissingSurahsResponse` | yes | no (whole) |
| `GET /api/words/roots/{id}/lemmas` | — | `RootLemmasResponse` | yes | no (whole, bounded) |
| `GET /api/words/roots/{id}/stems` | — | `RootStemsResponse` | yes | no (whole, bounded) |

Boundary rules (`API_GUIDELINES.md`): thin controller maps outcome unions → status codes
(`200`/`400`/`404`); Arabic messages centralized in an `ApiMessages` area near the feature; invalid
`kind`/`sort`/paging → `400`; missing root id → `404`; no EF/LINQ in the controller.

### 4.2 DTOs (Application.Abstractions)

```
RootListItemDto(
  int Id, string RootText,
  int OccurrencesCount,      // = quran_roots.words_count
  int AyahsCount, int SurahsCount,
  int SimpleWordsCount, int TashkeelWordsCount,
  int LemmasCount,           // = distinct_lemmas_count (co-occurrence)
  int StemsCount,
  string FirstVerseKey)      // for ordering/context only; Id never shown in UI

RootSummaryDto( int Id, string RootText, …same counts…, string FirstVerseKey )

RootWordItemDto(
  int UniqueWordId,          // simple→unique_simple_word_id, tashkeel→unique_tashkeel_word_id
  string Kind,               // "simple" | "tashkeel" (for the F014 deep link)
  string DisplayTextUthmani,
  int OccurrencesCount,      // occurrences WITHIN this root (in-context)
  string FirstVerseKey )

RootAyahMatchDto(            // shape-identical to F014 UniqueWordAyahMatchDto
  int AyahId, string VerseKey, int SurahNumber, string SurahNameArabic,
  int AyahNumber, short PageNumber,
  IReadOnlyList<int> MatchedQuranWordIds,
  IReadOnlyList<AyahWordForHighlightDto> Words )   // REUSE F014 AyahWordForHighlightDto

RootSurahsResponse( int Id, string RootText, int SurahsCount, IReadOnlyList<RootSurahItemDto> Surahs )
RootSurahItemDto( int SurahNumber, string NameArabic, int OccurrencesInSurah )
RootMissingSurahsResponse( int Id, string RootText, int MissingSurahsCount, IReadOnlyList<MissingSurahItemDto> Surahs )

RootLemmasResponse( int Id, string RootText, int LemmasCount, IReadOnlyList<RootLemmaItemDto> Lemmas )
RootLemmaItemDto( int LemmaId, string LemmaText, int OccurrencesCount )   // occurrences within this root

RootStemsResponse( int Id, string RootText, int StemsCount, IReadOnlyList<RootStemItemDto> Stems )
RootStemItemDto( int StemId, string StemText, int OccurrencesCount )      // occurrences within this root
```

`AyahWordForHighlightDto` is reused from F014 unchanged so the frontend `highlighted-ayah` component
needs no change. `Id`/`LemmaId`/`StemId`/`UniqueWordId` exist for selection/URL/deep-links, never for
display.

### 4.3 Query / read-model strategy

`IRootsReader` (abstraction) → `EfRootsReader` (EF/Npgsql, `AsNoTracking`) → `CachedRootsReader`
(decorator). Driving relationship for every per-root read: `quran_word_morphology m (m.root_id = X)`
joined to `quran_words w (w.id = m.quran_word_id)`. Morphology rows are one-per-readable-word, so
ayah markers never enter the set.

- **List / summary (compute-once + cache-whole-list — recommended):** one grouped aggregation over
  the ~50,298 root-bearing morphology rows produces all 1,642 rows with all 8 counts
  (`occurrences = words_count`; `ayahs = DISTINCT ayah_id`; `surahs = DISTINCT surah_number`;
  `simpleWords = DISTINCT unique_simple_word_id`; `tashkeelWords = DISTINCT unique_tashkeel_word_id`;
  `lemmas = DISTINCT lemma_id`; `stems = DISTINCT stem_id`). The decorator caches this **whole list**
  once; the handler then applies **search → sort → page in memory** over the cached list.
  - *Search handling:* because the whole bounded list is in memory, root-text `search` filters the
    cached list in-process (Arabic-normalized contains, reusing F014’s fold approach) — **no per-search
    cache key and no DB round-trip**. This honors F014’s intent (never create unbounded cache keys)
    by a stronger mechanism (no per-query keys at all). *Fallback if in-memory search is undesired:*
    mirror F014 exactly — DB query with `ILIKE` + sort + page, cache only no-search pages, bypass on
    search; but this re-aggregates per request and is not recommended given the verified small size.
  - `occurrences` may be read directly from `quran_roots.words_count` (verified exact) or recomputed
    in the same pass; either is fine.
- **Words (simple/tashkeel), paged:** distinct unique-word IDs for the root with in-context
  occurrence count and display text; `DISTINCT`/`GROUP BY` on `unique_simple_word_id` /
  `unique_tashkeel_word_id`, ordered by first occurrence, server-paged.
- **Ayahs, paged:** reuse F014’s `GetAyahMatchesAsync` shape — page the distinct matched ayah IDs,
  batch-load the page’s ayah words, and build `MatchedQuranWordIds` from the root’s `quran_words.id`
  set (no per-ayah N+1). Bounded by `SqlCommandCountInterceptor` in tests.
- **Surahs / missing:** distinct surah numbers (+ per-surah occurrence counts) joined to
  `quran_surahs` for Arabic names; missing = 114 − mentioned; both returned whole.
- **Lemmas (co-occurrence) / stems:** `DISTINCT lemma_id` / `DISTINCT stem_id` for the root, joined to
  `quran_lemmas` / `quran_stems` for text, with in-context occurrence counts; both bounded
  (verified worst case ≈ 22 lemmas, ≈ 84 stems) and returned whole. **Lemmas must use
  morphology co-occurrence; never `COUNT(quran_lemmas WHERE root_id)`** — the list column and the
  lemmas tab must agree.

### 4.4 Caching

Reuse the decorator pattern and the **already-registered shared `IMemoryCache`** (registered once in
`MushafReaderDependencyInjection`; do not re-register or reconfigure globally — no size limit /
eviction / default expiration changes). Add a `RootsCacheKeys` helper, namespace prefix `roots:`:

| Read | Key | Notes |
|---|---|---|
| Whole summary list | `roots:summary:all` | computed once; immutable; search/sort/page derived in memory |
| Root summary | `roots:{id}:summary` | for deep-link restore |
| Words | `roots:{id}:words:{kind}:p{page}:s{size}` | |
| Ayahs | `roots:{id}:ayahs:p{page}:s{size}` | |
| Surahs / missing | `roots:{id}:surahs` / `roots:{id}:missing` | |
| Lemmas / stems | `roots:{id}:lemmas` / `roots:{id}:stems` | |

- **Expiration:** none (cache for process lifetime) — Quran/morphology data is immutable at runtime;
  reseed implies restart, which clears the cache.
- **Why safe:** values are pure functions of immutable tables; the key space is bounded (1,642 roots
  × a few views/pages); the whole-list approach removes any unbounded free-text key risk.

### 4.5 Logging

Per `LOGGING_GUIDELINES.md`, log at the **Application handler** boundary with structured templates and
stable lower-camelCase fields; the reader/decorator stays quiet (optional `Debug` for cache hit/miss).

- **Completed (Information):** `{feature}="Roots"`, `{operation}`, `{rootId}`, `{view}`, `{subView}`,
  `{pageNumber}`, `{pageSize}`, `{sort}`, `{hasSearch}`, `{totalCount}`, `{itemCount}`,
  `{cacheResult}` (hit/miss), `{elapsedMs}` (only if actually measured).
- **Rejected (Warning):** invalid kind/sort/paging/id → `{reason}` + safe fields.
- **Not found (Warning):** missing `rootId`.
- **Never log:** root text, lemma/stem text, ayah/word text, raw `search` text (log `hasSearch` only),
  or any large payload (log counts).

### 4.6 Backend tests (focused, milestone-based)

Model on the F014 harness: **Testcontainers PostgreSQL + a committed embedded SQL slice**
(`roots-explorer-seed.sql`) covering a handful of representative roots with their morphology rows
(root_id/lemma_id/stem_id), words with unique-word links, and supporting ayah/surah rows — **not** the
full or local DB; canonical Uthmani text used verbatim. Real-run env escape hatch like F014.
`RecordingLoggerProvider` for log assertions; `SqlCommandCountInterceptor` for bounded-query / cache
assertions.

Coverage:

- list counts correct (all 8 columns) for seeded roots;
- `occurrences == words_count` reconciliation on the slice;
- **lemmas co-occurrence**: list `LemmasCount` == lemmas-tab item count == `DISTINCT lemma_id`; and a root where co-occurrence ≠ `COUNT(quran_lemmas WHERE root_id)` to lock the chosen semantics;
- stems aggregation via morphology;
- simple/tashkeel word items carry correct unique-word IDs and in-context occurrence counts;
- ayah-match highlighting returns exact `MatchedQuranWordIds`; bounded query count (no N+1) via the interceptor;
- mentioned + missing surahs (counts sum to 114);
- search/sort/pagination (mushaf-order/occurrences/alpha; in-memory filtering correctness);
- cache behavior: second identical read does not re-issue DB commands (interceptor); whole-list cached once;
- not-found (`404`) and invalid input (`400`); logging emits the required fields and **no** Quran/root/search text.

---

## 5. Frontend plan

Inside the existing `features/words/` feature (per `FRONTEND_STRUCTURE.md`; the nav item is already
`الكلمات والجذور`).

### 5.1 Route / page

- Add `WORDS_ROOTS_ROUTE` (`path: 'roots'`, lazy `loadComponent`) to `words.routes.ts`; add
  `WORDS_ROOTS_SEGMENT` + `rootsRoutePath()` to `core/navigation/route-paths.ts`.
- `pages/roots-explorer-page` is a thin **shell/orchestrator**: reads route query params, connects to
  the two facades, composes the table + panel; no API orchestration or large logic inline.

### 5.2 Components

| Component | Role | Reuse |
|---|---|---|
| `roots-explorer-page` | split-screen shell | new |
| `roots-table` | 8-column CDK list (div-grid + ARIA roles + real CDK virtual scroll w/ `ResizeObserver` fallback + UI row numbers + count-chip cells) | model on `unique-words-table` |
| `root-details-panel` | side-panel shell, `role="tablist"` strip, own scroll container, drawer on narrow | new |
| `root-words-list` | بدون تشكيل / بالتشكيل rows; each row deep-links to F014 | new |
| `root-lemmas-list` | static الصيغ المعجمية list (non-interactive now) | new |
| `root-stems-list` | static الأصول الصرفية list (non-interactive now) | new |
| `highlighted-ayah` | matched-word highlight | **reuse as-is** |
| `ayah-matches-list` | paged ayah matches | reuse / lightly generalize |
| `surah-occurrences-list`, `missing-surahs-list` | surah lists | reuse |
| `word-count-chip` | clickable count cells (real buttons) | reuse |
| `qd-pagination` | shared pagination | **reuse shared** (not the old words pagination) |

### 5.3 Services / facades / cache

- `data-access/roots.api.ts` — 8 endpoints, `HttpParams`, encoded segments, returns `Observable<ApiResponse<T>>` (model on `unique-words.api.ts`).
- `state/roots-explorer.facade.ts` — list + selection + list URL state (search/sort/page); page-ready loading/empty/error/no-results state.
- `state/roots-detail.facade.ts` — panel state for the 5 views + 2 sub-view axes; **lazy per active view**; restore-from-URL; not-found/error handling. Models the F014 drilldown facade but is **persistent panel** (no `isOpen`/modal-close semantics; selection drives visibility).
- `state/roots-cache.ts` — `extends ApiResponseCache` (in-flight dedup via `shareReplay`, reuse loaded views) with `roots:` keys mirroring the backend key params.
- `state/roots-url-sync.ts` — `parseRootsQueryParams` / `buildRootsQueryParams` (F014 url-sync discipline).
- `models/roots.models.ts` — DTOs, view models, state, query-key constants, type guards, defaults.
- Word→F014 navigation: reuse the existing `buildUniqueWordsDeepLink(kind, { wordId, view })`.

### 5.4 URL state

Query params on `/dashboard/words/roots`:

| Param | Values | Default | Notes |
|---|---|---|---|
| `search` | Arabic root text | empty | in-memory filtered (backend whole-list) |
| `sort` | `mushaf-order`/`occurrences`/`alpha` | `mushaf-order` | |
| `page` | int ≥1 | 1 | list page |
| `root` | root ID | — | selection; drives panel; URL-only (never displayed) |
| `view` | `words`/`ayahs`/`surahs`/`lemmas`/`stems` | — | only valid with `root` |
| `wordView` | `simple`/`tashkeel` | `simple` | only when `view=words` |
| `surahView` | `mentioned`/`missing` | `mentioned` | only when `view=surahs` |
| `detailPage` | int ≥1 | 1 | only for `ayahs`/`words` |

Parse rules: unknown `sort`→default; bad `page`/`detailPage`→default; `view` ignored unless `root` is
a valid positive int; sub-views ignored unless their parent view active; clearing selection clears
`root`/`view`/`wordView`/`surahView`/`detailPage`, preserves `search`/`sort`/`page`. `pageSize` /
`detailPageSize` stay fixed defaults (not URL params) for clean links + stable cache keys.

Count-click mapping (table cell → URL/view): المواضع→`view=ayahs`; الآيات→`view=ayahs`;
السور→`view=surahs&surahView=mentioned`; كلمات بدون تشكيل→`view=words&wordView=simple`;
كلمات بالتشكيل→`view=words&wordView=tashkeel`; الصيغ المعجمية→`view=lemmas`; الأصول الصرفية→`view=stems`.
Selecting the row (root-text cell) → `view=ayahs` (default).

### 5.5 Split-screen / drawer UX

- Desktop: table (primary) + panel (inline-end) using CSS logical properties; each has its own scroll
  container; recommended ~60/40–65/35 split with a panel min-width that keeps Quran text readable.
- Narrow: panel becomes an inline-end drawer (focus-trap, `Esc`, focus return); not a modal-driven
  desktop experience.
- Empty-selection: panel shows `اختر جذرًا لعرض تفاصيله` (desktop) / drawer stays closed (narrow).
- States everywhere: loading, empty, error (safe backend message), **not-found** (invalid `root`→
  controlled Arabic message, list stays usable), no-results (empty search).

### 5.6 Accessibility / RTL

- Count cells and root-text cell are real `<button>`s (`word-count-chip` already emits + has `aria-label`); **no fake buttons** for lemmas/stems until routes exist (render as static list items).
- Tab strip: `role="tablist"`/`tab`/`tabpanel`, `aria-selected`, roving `tabindex`, RTL-aware arrow keys; sub-views are a nested tablist.
- Selected row uses `aria-current`; panel load status via `role="status" aria-live="polite"`.
- Highlight conveyed beyond color (class/marker + `aria-label`), reusing `highlighted-ayah`.
- RTL-first; Quran text rendering/fonts stable and unanimated.

### 5.7 Frontend tests (focused)

- URL parse/build/restore round-trips (incl. sub-views + back/forward + refresh).
- Table count-click → correct view/sub-view URL mapping (all 7 mappings + row-select default).
- Panel tab/sub-view routing reflects/derives from URL.
- Lazy-loading: a view loads only when first active; **table render issues no detail calls** (assert API service not called for detail on list render).
- Reuse of `highlighted-ayah` with correct `matchedQuranWordIds`.
- Deep links to Unique Words simple/tashkeel flows (correct `kind` + `wordId`).
- Responsive/drawer behavior where feasible (guard `matchMedia`/`ResizeObserver` absence — jsdom lacks them; default desktop).
- Accessibility basics: count cells are buttons, tab roles/`aria-selected`, selected-row `aria-current`, lemmas/stems are non-interactive.
- Keep the `VITEST_MAX_FORKS` worker cap for `npm test` (machine OOMs without it).

---

## 6. Backend / frontend contract table

| Capability | Backend endpoint | Frontend trigger | Cached (key params) | Paged |
|---|---|---|---|---|
| Roots list + 8 counts | `GET /api/words/roots?search&sort&page&pageSize` | table load / search / sort / page | whole-list `roots:summary:all` (search/sort/page in memory) | yes |
| Root summary (restore) | `GET /api/words/roots/{id}` | deep-link restore w/ `root` set | `roots:{id}:summary` | no |
| Simple words | `GET /api/words/roots/{id}/words/simple?page&pageSize` | `view=words&wordView=simple` | `roots:{id}:words:simple:p:s` | yes |
| Tashkeel words | `GET /api/words/roots/{id}/words/tashkeel?page&pageSize` | `view=words&wordView=tashkeel` | `roots:{id}:words:tashkeel:p:s` | yes |
| Ayah matches + highlight | `GET /api/words/roots/{id}/ayahs?page&pageSize` | `view=ayahs` (+ `detailPage`) | `roots:{id}:ayahs:p:s` | yes |
| Mentioned surahs | `GET /api/words/roots/{id}/surahs` | `view=surahs&surahView=mentioned` | `roots:{id}:surahs` | no (≤114) |
| Missing surahs | `GET /api/words/roots/{id}/missing-surahs` | `view=surahs&surahView=missing` | `roots:{id}:missing` | no |
| Lemmas (co-occurrence) | `GET /api/words/roots/{id}/lemmas` | `view=lemmas` | `roots:{id}:lemmas` | no (bounded) |
| Stems | `GET /api/words/roots/{id}/stems` | `view=stems` | `roots:{id}:stems` | no (bounded) |
| Word → Unique Words detail | (F014 endpoints) | click word row → `buildUniqueWordsDeepLink(kind,{wordId})` | (F014 cache) | — |

Invariants the frontend relies on: `OccurrencesCount = words_count`; `LemmasCount` (list) ==
lemmas-tab count (both co-occurrence); every word row resolves a valid unique-word ID;
`RootAyahMatchDto` is shape-identical to F014’s ayah DTO.

---

## 7. Phased implementation plan (Spec-Kit-ready)

Each phase is a full-stack, independently demoable slice. Phasing follows F014’s layering so the read
model, cache, and logging land with their first consumer.

- **Phase 0 — Foundations & contracts (no behavior).** Add route shell + page placeholder; define
  backend DTOs + `IRootsReader`; define frontend models + url-sync + empty facades; register cache
  keys/namespace. Exit: builds; route resolves to an empty shell; contracts compile.

- **Phase 1 — US1: Roots table (MVP).** Backend list endpoint with compute-once + cache-whole-list
  (all 8 counts), search/sort/page; `CachedRootsReader`; logging. Frontend `roots-table` (CDK +
  shared `qd-pagination`), search/sort, list URL state, row numbers, count cells rendered as buttons
  (clicks wired in later phases or set selection). Exit: list renders with real counts; search/sort/
  page work and survive refresh/back-forward; no detail calls on table render.

- **Phase 2 — US2: Selection + side panel + الآيات.** Selection sets `root` + default `view=ayahs`;
  `root-details-panel` shell (tab strip, own scroll, drawer on narrow); ayah-matches endpoint + reuse
  `highlighted-ayah`; restore-from-URL; not-found handling. Exit: clicking المواضع/الآيات opens the
  panel on highlighted, paginated ayah matches; deep link restores.

- **Phase 3 — US3: السور (ورد فيها / لم يذكر فيها).** Mentioned + missing endpoints (whole); reuse
  surah/missing list components; sub-view URL state. Exit: السور cell opens mentioned; sub-view
  toggle + URL restore; counts sum to 114.

- **Phase 4 — US4: الكلمات (بدون تشكيل / بالتشكيل) + F014 deep links.** Words endpoints (paged);
  `root-words-list`; deep link into Unique Words simple/tashkeel via unique-word IDs. Exit: word cells
  open correct sub-view; clicking a word opens the existing Unique Words detail in the right mode.

- **Phase 5 — US5: الصيغ المعجمية + الأصول الصرفية (static, link-ready).** Lemmas (co-occurrence) +
  stems endpoints (whole); static, non-interactive lists with IDs in DTOs. Exit: tabs render; list
  count == table column count for lemmas; no fake interactive elements.

- **Phase 6 — Cross-cutting hardening.** Responsive drawer polish; full accessibility pass
  (tablist/buttons/aria-current/live regions); logging field completeness + redaction; cache-hit
  verification; empty/error/not-found/no-results states; performance sanity. Exit: a11y + state
  matrix complete; logging-safe; cache verified.

Dependencies: Phase 1 depends on Phase 0; Phases 2–5 depend on Phase 1 (table + selection) and are
otherwise parallelizable; Phase 6 depends on 2–5.

---

## 8. Milestone-based test checkpoints

Run focused tests at each milestone, not the full suite after every change.

- **CP-0 (Foundations):** projects build; backend slice fixture (`roots-explorer-seed.sql`) loads;
  frontend models/url-sync unit tests (parse/build round-trip).
- **CP-1 (List):** backend list-count tests (8 columns), `occurrences==words_count`, search/sort/page,
  cache-once (interceptor: no re-query on 2nd read), logging fields + redaction; frontend table
  count-click mapping, list URL restore, **no detail calls on table render**.
- **CP-2 (Ayahs):** backend ayah-match highlight IDs + bounded query count (no N+1) + paging +
  not-found; frontend `highlighted-ayah` reuse, panel open/restore, lazy-load on activate.
- **CP-3 (Surahs):** backend mentioned/missing (sum 114); frontend sub-view routing + whole load.
- **CP-4 (Words):** backend word items (unique-word IDs, in-context counts) + paging; frontend deep
  links into F014 (correct kind/wordId).
- **CP-5 (Lemmas/Stems):** backend co-occurrence lemmas (list==tab), stems aggregation, bounded whole
  loads; frontend static non-interactive lists.
- **CP-6 (Hardening):** a11y checks (buttons/tablist/aria-current/live region), responsive/drawer
  where feasible, cache-hit + logging-safe assertions; targeted regression only.
- **Pre-merge:** one full backend + frontend suite run (frontend with `VITEST_MAX_FORKS` cap).

---

## 9. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Lemma count diverges between table column and tab | Lock both to morphology **co-occurrence** (`DISTINCT lemma_id`); add a backend test on a known divergent root; never use `quran_lemmas.root_id`. |
| In-memory search/sort over cached whole-list seen as a deviation from F014 | Justified by verified bounded size (1,642 rows, ~30–115 ms); honors the “no unbounded cache keys” intent more strongly; documented fallback (DB ILIKE + bypass) if rejected in plan. |
| Eager detail loading creeping into the list | Backend returns only counts; frontend asserts no detail API calls on table render; `SqlCommandCountInterceptor` bounds queries. |
| N+1 on ayah matches | Reuse F014’s batched page-load; interceptor test asserts bounded command count. |
| Fake interactivity on lemmas/stems | Render as static list items (no buttons/links) until detail routes exist; a11y test asserts non-interactive. |
| Side-panel scroll bleeding into page scroll | Panel is its own scroll container (`overflow-y:auto`, constrained height); explicit test/visual check. |
| jsdom lacks `matchMedia`/`ResizeObserver` (responsive + virtual scroll) | Guard and default desktop (existing project pattern); keep CDK virtual-scroll observer fallback like F014. |
| Frontend test runner OOM | Keep `VITEST_MAX_FORKS` cap for `npm test`. |
| Logging leaking Quran/root/search text | Log IDs/counts/`hasSearch` only; `RecordingLoggerProvider` test asserts absence of text. |
| Accidental global cache reconfiguration | Reuse the already-registered shared `IMemoryCache`; only add `roots:` keys; no size/eviction/expiration changes. |
| Cross-repo coordination (backend + frontend submodules) | Land backend endpoints before/with their frontend consumers per phase; commit children-first then workspace pointer (existing commit workflow). |

---

## 10. Open questions (minor; do not block Spec Kit)

1. **Sortable columns:** confirm sort applies to the three F014 keys only (`mushaf-order`/
   `occurrences`/`alpha`), or whether additional numeric columns should be sortable. (Recommend: the
   three keys only for parity.)
2. **Word-row per-root count vs global destination copy:** confirm whether the word row should label
   its count as in-root and whether any micro-copy is needed to set expectation that the destination
   shows global counts. (Recommend: show in-root count; no extra copy unless testing shows confusion.)
3. **Zero-count cells:** confirm clickable-to-empty-state (recommended) vs disabled when a count is 0.
4. **Panel side + drawer direction in RTL:** confirm with design which inline side the panel occupies.
5. **detailPageSize values:** confirm fixed defaults (ayahs ≈100 as F014; words ≈50–100) are acceptable
   without a UI selector.

(The capability report’s wording that `distinct_lemmas_count` is “equivalent to
`COUNT(quran_lemmas WHERE root_id)`” is **incorrect** and is superseded here by co-occurrence; the
Spec Kit spec should state co-occurrence explicitly.)

---

## 11. Final plan verdict

### READY_FOR_SPEC_KIT

The feature is fully specified end-to-end: read-only data is confirmed sufficient (no migration/
index), the backend API/DTO/read-model/cache/logging design mirrors a proven feature, the frontend
IA/route/URL-state/components/accessibility are locked, the backend↔frontend contract is explicit,
and the work is phased into independently testable full-stack slices with milestone test
checkpoints. The lemma-count semantics are locked to morphology co-occurrence in both the table
column and the tab. Open questions in §10 are minor confirmations that can be resolved inside the
Spec Kit `specify`/`clarify` step and do not block proceeding.
