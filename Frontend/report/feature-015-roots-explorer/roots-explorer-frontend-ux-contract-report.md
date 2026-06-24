# Feature 015 — Quran Roots Explorer — Frontend UX + API Contract Alignment

> **Contract report only.** No code was written, no source files changed, no implementation plan
> produced, nothing committed. This aligns the requested Roots Explorer UX with the existing
> Feature 014 (Unique Words Explorer) frontend, the frontend architecture docs, and the verified
> backend data decisions, so the later plan can cover backend + frontend coherently.

| Item | Value |
| --- | --- |
| Feature | 015 — Quran Roots Explorer (frontend) |
| Route | `/dashboard/words/roots` (child of the existing `words` feature) |
| Reference | Feature 014 — Unique Words Explorer (`src/app/features/words/`) |
| Detail UX | **Persistent split-screen side panel** (not a modal) |
| Lemma semantics | **Co-occurrence** (`DISTINCT lemma_id` via morphology) — column **and** tab |
| **Verdict** | **FRONTEND_READY_WITH_NOTES** |

Sources read: `roots-explorer-capability-analysis-report.md`, `roots-explorer-readonly-verification-report.md`;
F014 frontend (`words.routes.ts`, `unique-words.models.ts`, `unique-words.api.ts`,
`unique-words-url-sync.ts`, `unique-words-drilldown.facade.ts`, `unique-words-cache.ts`,
`core/caching/api-response-cache.ts`, `unique-words-table.component.*`,
`highlighted-ayah.component.ts`, `word-count-chip.component.ts`, `route-paths.ts`, `nav-items.ts`);
docs (`FRONTEND_STRUCTURE.md`, `API_INTEGRATION_GUIDELINES.md`, `UI_STYLE_SYSTEM.md` references).

---

## 1. Recommended frontend information architecture

The Roots Explorer is a **new routeable page inside the existing `words` feature** — it is a sibling
of the Unique Words page, not a new feature folder. The nav item is already
`الكلمات والجذور` (`words` → `/dashboard/words`), so roots are anticipated.

```
src/app/features/words/
  pages/
    roots-explorer-page/            ← NEW routeable shell (split-screen orchestrator)
  components/
    roots-table/                    ← NEW main-area CDK list (8 summary columns)
    root-details-panel/             ← NEW side panel shell + tab strip (independent scroll)
    root-words-list/                ← NEW الكلمات sub-views (بدون تشكيل / بالتشكيل)
    root-lemmas-list/               ← NEW الصيغ المعجمية (display + link-ready, non-interactive now)
    root-stems-list/                ← NEW الأصول الصرفية (display + link-ready, non-interactive now)
    highlighted-ayah/               ← REUSE as-is
    ayah-matches-list/              ← REUSE / lightly generalize
    surah-occurrences-list/         ← REUSE (ورد فيها)
    missing-surahs-list/            ← REUSE (لم يذكر فيها)
    word-count-chip/                ← REUSE (clickable count cells)
    unique-words-list-pagination/   ← REUSE (rename to a shared pagination if convenient)
  data-access/
    roots.api.ts                    ← NEW
  state/
    roots-explorer.facade.ts        ← NEW (list + selection + URL list-state)
    roots-detail.facade.ts          ← NEW (panel; lazy per tab/sub-view) — models F014 drilldown
    roots-cache.ts                  ← NEW (extends ApiResponseCache)
    roots-url-sync.ts               ← NEW (parse/build query params)
  models/
    roots.models.ts                 ← NEW
  words.routes.ts                   ← ADD roots route
```

Shell composition (the page is an orchestrator, per `FRONTEND_STRUCTURE.md`): the
`roots-explorer-page` owns the split layout, reads route/query params, connects to the two facades,
and composes `roots-table` (main) + `root-details-panel` (side). It must not hold API orchestration
or large logic inline.

**No `نظرة عامة` tab** — the table is the summary surface; the panel opens straight to a real view.

---

## 2. Final route and URL state proposal

### Route

```
/dashboard/words/roots
```

A single child route under `words` (not a per-mode child route like `unique/:mode`, because root
selection and detail views are panel state, not separate top-level destinations). Add
`WORDS_ROOTS_SEGMENT = 'roots'` + a `rootsRoutePath()` helper to `core/navigation/route-paths.ts`
and a `WORDS_ROOTS_ROUTE` to `words.routes.ts`, lazy-loaded like the existing pages.

### Query-param state (deep-link, refresh, back/forward)

All explorer state lives in query params on `/dashboard/words/roots`, mirroring F014's approach
(F014 keeps list state + drill-down state in query params and parses/builds them in
`unique-words-url-sync.ts`).

| Param | Values | Default | Meaning |
|---|---|---|---|
| `search` | Arabic root text | empty | Root-text search (cache-bypassing on backend). |
| `sort` | `mushaf-order`, `occurrences`, `alpha` | `mushaf-order` | List ordering. **Align with F014 keys** (see Notes on `occurrences_desc`). |
| `page` | positive int | `1` | List page. |
| `root` | stable root ID | — | Selected root (drives the panel). ID is in the **URL only**, never shown in visible UI. |
| `view` | `words`, `ayahs`, `surahs`, `lemmas`, `stems` | — | Active panel tab; only meaningful when `root` is set. |
| `wordView` | `simple`, `tashkeel` | `simple` | Sub-view; only when `view=words`. |
| `surahView` | `mentioned`, `missing` | `mentioned` | Sub-view; only when `view=surahs`. |
| `detailPage` | positive int | `1` | Detail pagination; only for paginated views (`ayahs`, `words`). |

Parsing rules (copy F014's discipline): unknown `sort` → default; non-positive/NaN `page`/`detailPage`
→ default; `view` ignored unless `root` is a valid positive int; sub-views ignored unless their
parent `view` is active; `detailPage` ignored outside paginated views. Clearing the selection clears
`root`, `view`, `wordView`, `surahView`, `detailPage` while preserving `search`, `sort`, `page`.

> **Page size:** keep `pageSize`/`detailPageSize` as fixed defaults (not URL params) to keep links
> clean and backend cache keys stable. Promote to a URL param only if a page-size selector is added.

### Example URLs (resolved)

```
/dashboard/words/roots?search=رحم&sort=occurrences&page=1&root=55&view=ayahs&detailPage=1
/dashboard/words/roots?root=55&view=words&wordView=simple
/dashboard/words/roots?root=55&view=words&wordView=tashkeel
/dashboard/words/roots?root=55&view=surahs&surahView=missing
```

(`occurrences_desc` from the brief is normalized to the F014-style `occurrences` key — see §10 note 1.)

---

## 3. Table columns and count-click mapping

### Columns (summary numbers only — no eager detail)

| # | Header (Arabic) | Meaning | Source | Interactive? |
|---|---|---|---|---|
| — | (UI row #) | page-relative row number | computed (`pageRelativeRowNumber`) | no |
| 1 | الجذر | root text | `rootText` | selects row (opens panel default view) |
| 2 | المواضع | occurrences | precomputed `words_count` | **button** → `view=ayahs` |
| 3 | الآيات | distinct ayahs | aggregate | **button** → `view=ayahs` |
| 4 | السور | distinct surahs | aggregate | **button** → `view=surahs&surahView=mentioned` |
| 5 | كلمات بدون تشكيل | distinct simple words | aggregate (`unique_simple_word_id`) | **button** → `view=words&wordView=simple` |
| 6 | كلمات بالتشكيل | distinct tashkeel words | aggregate (`unique_tashkeel_word_id`) | **button** → `view=words&wordView=tashkeel` |
| 7 | الصيغ المعجمية | lemmas (**co-occurrence**) | `distinct_lemmas_count` | **button** → `view=lemmas` |
| 8 | الأصول الصرفية | stems | aggregate (`DISTINCT stem_id`) | **button** → `view=stems` |

Terminology guardrails (from the brief, enforced): column 6 is labeled **`كلمات بالتشكيل`**, never
`الصيغ بالتشكيل` (which would collide with `الصيغ المعجمية`/lemmas). The `الكلمات` parent label is
used **only** inside the panel tab, with sub-views `بدون تشكيل` / `بالتشكيل`.

### Click mapping (count cell → panel target)

```
المواضع           → view=ayahs                          (ayah matches + highlight)
الآيات            → view=ayahs
السور             → view=surahs & surahView=mentioned   (ورد فيها)
كلمات بدون تشكيل   → view=words  & wordView=simple        (بدون تشكيل)
كلمات بالتشكيل     → view=words  & wordView=tashkeel      (بالتشكيل)
الصيغ المعجمية     → view=lemmas
الأصول الصرفية     → view=stems
```

Each numeric cell is a **real `<button>`** (reuse `WordCountChipComponent`, which already emits a
click output and exposes an `aria-label`/`disabled`). Selecting the **row** (root-text cell button)
opens the panel on a sensible default (`view=ayahs`). Backend IDs are never rendered; UI row numbers
cover the "which row" need.

### Table behavior

- **Search / sort / pagination** owned by `roots-explorer.facade.ts` and reflected in the URL.
- **Selection** sets `root` (and a default `view`); the selected row gets `aria-current`/selected styling (F014 `isSelected` pattern).
- **Rendering:** reuse F014's table approach — a `role="table"` div-grid with `role="row"/"columnheader"/"cell"` and **correct CDK virtual scroll** (`CdkVirtualScrollViewport`, fixed row height) with the existing **plain-scroll fallback when `ResizeObserver` is unavailable** (jsdom/tests). With 1,642 roots and server pagination, this is real virtual scroll, not faux. See §4 anti-pattern note.

---

## 4. Details panel structure and lazy-loading behavior

### Structure — persistent side panel (NOT a modal)

```
root-details-panel  (own scroll container: overflow-y:auto; height:100%)
├── header: root text + summary counts (no IDs)
├── tab strip (role="tablist"):  الكلمات | الآيات | السور | الصيغ المعجمية | الأصول الصرفية
└── active view:
    ├── الكلمات      → sub-tabs: بدون تشكيل / بالتشكيل   → root-words-list (paginated)
    ├── الآيات       → ayah-matches-list (paginated) using highlighted-ayah
    ├── السور        → sub-tabs: ورد فيها / لم يذكر فيها  → surah-occurrences-list / missing-surahs-list
    ├── الصيغ المعجمية → root-lemmas-list   (display + link-ready; non-interactive now)
    └── الأصول الصرفية → root-stems-list     (display + link-ready; non-interactive now)
```

The panel’s **own scroll container** is the core requirement: the table and the panel scroll
independently, so browsing many ayah/word rows never scrolls the whole page. Use CSS logical
properties (`inline-start`/`inline-end`, `padding-inline`) so RTL placement is correct.

### Lazy-loading rules (mirror F014 drilldown facade)

| View / sub-view | Load trigger | Pagination | Whole vs paged |
|---|---|---|---|
| الكلمات / بدون تشكيل | tab+subview active | `detailPage` | **paged** (large roots have many words) |
| الكلمات / بالتشكيل | tab+subview active | `detailPage` | **paged** |
| الآيات | tab active | `detailPage` | **paged — mandatory** (worst root ≈ 1,879 ayahs, verified) |
| السور / ورد فيها | tab active | none | **whole** (≤114; one request) |
| السور / لم يذكر فيها | tab active | none | **whole** (≤114) |
| الصيغ المعجمية | tab active | none (recommended) | **whole** — bounded; worst root verified ≈ 22 lemmas |
| الأصول الصرفية | tab active | none (recommended) | **whole** — bounded; worst root verified ≈ 84 stems |

- **Lazy:** a view loads only when first activated; nothing is fetched for inactive tabs.
- **Reuse loaded state:** `roots-detail.facade` keeps loaded views in signal state and uses the
  frontend `ApiResponseCache` (`getOrLoad`) so re-opening a tab or paging back is a memory hit, not a
  refetch. F014 already demonstrates both (e.g. reusing already-loaded `missingSurahs` from state).
- **No eager detail in the list response:** the table request returns only the 8 numbers; no ayah
  words, surah lists, lemmas, or stems ride along.

### Virtual-scroll honesty (explicit)

- Paginated lists (`ayahs`, `words`) use **normal scrolling inside the panel’s scroll container plus
  server pagination** — simplest and honest. CDK virtual scroll inside the panel is optional and only
  if rows are uniform-height; if used, use a real `CdkVirtualScrollViewport` with a measured row
  height (as the roots table does), never a fake windowing illusion.
- The **roots table** reuses F014’s proven real CDK virtual scroll (+ observer fallback). Do not
  invent a custom windowing scheme.

---

## 5. API contract expectations (frontend perspective)

All endpoints return `ApiResponse<T>` (`isSuccess`/`message`/`data`/`errors`); the facade unwraps,
components consume page-ready state. Endpoints align with capability report §3.

| Call | Endpoint (GET) | Response `data` | Notes |
|---|---|---|---|
| List | `/api/words/roots?search&sort&page&pageSize` | `PagedResultDto<RootListItemDto>` | 8 summary numbers per row; **lemmas = co-occurrence**. |
| Summary (restore) | `/api/words/roots/{id}` | `RootSummaryDto` | Header + counts for deep-link restore; `404` → not-found state. |
| Words | `/api/words/roots/{id}/words/{wordKind}?page&pageSize` | `PagedResultDto<RootWordItemDto>` | `wordKind` ∈ `simple|tashkeel`; each row carries the **unique word ID** for the F014 deep link. |
| Ayahs | `/api/words/roots/{id}/ayahs?page&pageSize` | `PagedResultDto<RootAyahMatchDto>` | Reuse `AyahWordForHighlightDto` + `matchedQuranWordIds`. |
| Surahs | `/api/words/roots/{id}/surahs` | `RootSurahsDto` | mentioned + per-surah occurrences; whole (≤114). |
| Missing surahs | `/api/words/roots/{id}/missing-surahs` | `RootMissingSurahsDto` | whole. |
| Lemmas | `/api/words/roots/{id}/lemmas` | `RootLemmasDto` | **co-occurrence** list; items `{ lemmaId, lemmaText, wordsCount }`. |
| Stems | `/api/words/roots/{id}/stems` | `RootStemsDto` | items `{ stemId, stemText, occurrencesCount }`. |

### Frontend-facing DTO expectations

- `RootListItemDto`: `{ id, rootText, occurrencesCount, ayahsCount, surahsCount, simpleWordsCount, tashkeelWordsCount, lemmasCount, stemsCount }`. `id` is for selection/URL/deep-links only — **never displayed**.
- `RootWordItemDto`: `{ uniqueWordId, displayTextUthmani, occurrencesCount, firstVerseKey, ... }` — the `uniqueWordId` is the bridge to F014 (`simple`→`unique_simple_word_id`, `tashkeel`→`unique_tashkeel_word_id`).
- `RootAyahMatchDto`: **shape-identical to F014’s `UniqueWordAyahMatchDto`** (`ayahId`, `verseKey`, `surahNumber`, `surahNameArabic`, `ayahNumber`, `pageNumber`, `matchedQuranWordIds[]`, `words: AyahWordForHighlightDto[]`). This lets `highlighted-ayah` and `ayah-matches-list` be reused unchanged.
- `RootLemmasDto.lemmasCount` **must equal** `RootListItemDto.lemmasCount` for the same root (both co-occurrence). The table column and the lemmas tab must agree — this is a hard contract requirement (verification found `quran_lemmas.root_id` ownership counts differ for 49 roots, so the backend must **not** use that source).

### Contract guarantees the frontend relies on (from verification report)

- Every root-bearing word has both `unique_simple_word_id` and `unique_tashkeel_word_id` (0 missing) → the simple/tashkeel columns and word→F014 navigation are always resolvable.
- `occurrences` = `words_count` (exact); `lemmasCount` = `distinct_lemmas_count` (co-occurrence).

---

## 6. Reuse opportunities from Feature 014

**Reuse directly (root-agnostic):**

- `HighlightedAyahComponent` — inputs are `words` + `matchedQuranWordIds`; ID-based highlight, ayah-marker filtering, and accessible `aria-label` ("كلمة مطابقة: …") already built. No change needed.
- `ayah-matches-list`, `surah-occurrences-list`, `missing-surahs-list` — render paged ayah matches / surah lists; data is root-agnostic. Reuse, lightly generalizing prop names if they’re typed to unique-word DTOs.
- `WordCountChipComponent` — clickable count cell with `disabled` + `aria-label` + click output.
- `unique-words-list-pagination` — pagination control (consider promoting to a shared name).
- `core/caching/api-response-cache.ts` (`ApiResponseCache`) — in-memory dedup (Map, max 48 entries, in-flight `shareReplay`); subclass as `RootsCache`.
- `buildUniqueWordsDeepLink(kind, { wordId, view })` (`unique-words-url-sync.ts`) — already produces the exact F014 deep link; the roots word list reuses it to open the existing word flow.
- Shared state primitives `qd-loading-state` / `qd-empty-state` / `qd-error-state`, and `LoadStatus` (`idle|loading|success|empty|error|notFound`).

**Reuse the pattern, not the file:**

- `unique-words-drilldown.facade.ts` — the lazy-per-view + cache `getOrLoad` + restore-from-URL +
  not-found/error handling pattern is the blueprint for `roots-detail.facade.ts`. But it is built
  around a **modal** (`isOpen`, `closeDrilldown`, modal URL tuple) with 3 views; the roots panel is
  persistent with 5 views and 2 sub-view axes, so it is a new facade modeled on it, not a copy.
- `unique-words-table.component.*` — model `roots-table` on it (div-grid + ARIA + CDK virtual scroll
  + observer fallback + UI row numbers + count-chip cells), expanded to 8 columns.

**Do NOT reuse:** `word-drilldown-modal` as the main detail surface — the brief forbids a modal for
the primary detail experience. (A modal/drawer is acceptable only as the small-screen responsive
adaptation; see §9.)

---

## 7. New frontend components / services / facades likely needed

- **Page:** `roots-explorer-page` (split-screen shell/orchestrator).
- **Components:** `roots-table`, `root-details-panel` (shell + `role="tablist"` strip), `root-words-list`, `root-lemmas-list`, `root-stems-list`.
- **Data-access:** `roots.api.ts` (8 endpoints, returns `ApiResponse<T>`, `HttpParams`, encoded segments — modeled on `unique-words.api.ts`).
- **State:** `roots-explorer.facade.ts` (list + selection + list URL state), `roots-detail.facade.ts` (panel; lazy per tab/sub-view; restore-from-URL), `roots-cache.ts` (`extends ApiResponseCache`), `roots-url-sync.ts` (parse/build params), `roots.models.ts` (DTOs, view models, state, query keys, type guards).
- **Routing:** add `WORDS_ROOTS_ROUTE` to `words.routes.ts`; add `rootsRoutePath()` + `WORDS_ROOTS_SEGMENT` to `route-paths.ts`.

File-size watch (`FRONTEND_STRUCTURE.md`): the page shell and `roots-detail.facade` are the likely
growth points (5 views × 2 sub-view axes). Keep the page a thin orchestrator; split the facade by
slice if it nears the ~400-line soft threshold (e.g. a small per-view loader helper, as F014 splits
`unique-words-drilldown.state.ts`).

---

## 8. Accessibility and RTL requirements

- **Real interactive elements:** count cells and the root-text cell are `<button>`s (via
  `WordCountChipComponent` / cell buttons), never clickable `div`s. Lemmas/stems items are **static**
  for now (see §10 note 3) — **no fake buttons with no action**.
- **Tab strip:** `role="tablist"` / `role="tab"` with `aria-selected`, roving `tabindex`, and
  Left/Right (RTL-aware) arrow-key navigation; the active panel region uses `role="tabpanel"` with
  `aria-labelledby` the active tab. Sub-views (`بدون تشكيل`/`بالتشكيل`, `ورد فيها`/`لم يذكر فيها`)
  are a nested tablist.
- **Live status:** panel load/empty/error announced via `role="status" aria-live="polite"` (F014
  already uses this for the loading row).
- **Table semantics:** keep F014’s `role="table"/rowgroup/row/columnheader/cell"`; selected row gets
  `aria-current`. Keyboard: rows and count buttons are tab-focusable and Enter/Space-activatable.
- **Highlight not color-only:** reuse `highlighted-ayah`’s class/marker + `aria-label` so matched
  words are conveyed beyond color.
- **RTL-first:** Arabic default; use logical CSS (`inline-start/inline-end`, `margin-inline`) for the
  split layout and panel side so direction flips correctly; Quran text rendering/fonts stay stable
  and unanimated (per F014 highlighting rules).
- **No invented data:** missing data renders a controlled Arabic empty/not-found state, never
  fabricated Quranic content.

---

## 9. Responsive layout recommendation

- **Desktop (wide):** two-pane split — roots table as the primary pane, `root-details-panel` on the
  inline-end side, each with its **own independent scroll**. Recommended ~60/40 to 65/35 split; the
  panel has a sensible min-width so Quran text and counts stay readable.
- **Tablet / narrow:** below a breakpoint, switch the panel to a **drawer/sheet sliding from the
  inline-end** over the table (with focus trap + `Esc` to dismiss + return focus to the originating
  count button), OR stack the panel beneath the table. **Recommended: drawer**, because it preserves
  the "browse details without losing the list" intent on small screens. This drawer is a *responsive
  adaptation only* — it is not the desktop default, so it does not violate the "no modal as the main
  detail experience" rule.
- **Empty selection state:** when no `root` is selected, the panel shows a calm "اختر جذرًا لعرض
  تفاصيله" prompt (desktop) rather than a blank pane; on narrow screens the drawer simply stays
  closed.
- **State clarity everywhere:** loading, empty (no counts / empty sub-view), error (with safe backend
  message), **not-found** (invalid `root` in URL → controlled message, list stays usable, matching
  F014’s restore-not-found handling), and no-results (empty search) are all explicit.

---

## 10. Risks / open questions for the implementation plan

1. **Sort key naming.** The brief’s example uses `occurrences_desc`; F014’s convention is
   direction-implicit keys (`mushaf-order`, `occurrences`, `alpha`). **Recommend aligning with F014
   keys** (`occurrences` = descending by occurrence) unless multi-column / bi-directional sorting is
   actually wanted. Confirm which columns are sortable (likely root text + occurrences; possibly
   others). Decide before the URL contract is frozen.
2. **Lemma-count contract consistency (hard requirement).** Backend must return the lemmas list and
   count using **co-occurrence** semantics (`DISTINCT lemma_id` via `quran_word_morphology WHERE
   root_id`), equal to `distinct_lemmas_count`. If the backend ever sourced lemmas from
   `quran_lemmas.root_id`, the table column and the lemmas tab would disagree for 49 roots. The
   capability report’s "equivalent to `COUNT(quran_lemmas WHERE root_id)`" wording is wrong and
   should be corrected in the plan.
3. **Lemmas/stems are display-only now → render as non-interactive.** Their detail pages are future
   work, so per "no fake buttons with no action," render lemma/stem items as **static list items**
   that carry the ID in the data model (link-ready) but are **not** buttons/links yet. Flip them to
   links only when the detail routes exist. Confirm this is the intended interim UX.
4. **Word-list counts: per-root vs global.** The الكلمات sub-views show words *in this root*, but
   clicking opens the F014 word detail which shows that word’s **global** counts (a unique simple
   spelling can occur under more than one root). Decide whether the row shows a per-root occurrence
   count (recommended, computed in context by the backend) and accept that the destination screen is
   global — and whether any copy is needed to avoid confusion.
5. **Page-size in URL.** Recommend fixed defaults (not URL params) for list and detail; revisit only
   if a page-size selector is introduced.
6. **Panel placement + drawer in RTL.** Confirm with design which side the panel occupies and the
   small-screen drawer behavior; ensure logical-property layout so RTL is correct.
7. **Zero-count cells.** Decide whether a `0` count (e.g. a root present in all 114 surahs →
   `لم يذكر في` would be 0 via the missing view) is a disabled chip or a clickable cell that opens an
   empty-state view. **Recommend clickable → empty state** for consistency; use `disabled` only where
   a view is genuinely N/A.
8. **Selection default view.** Decide the default `view` when a row (not a specific count) is
   selected. **Recommend `ayahs`** (most informative), or `words/simple`. Minor, but fix it for
   deterministic deep links.

None of these block frontend work; all are contract/UX decisions to lock during planning.

---

## 11. Caching / logging awareness (frontend perspective)

**Which UI calls benefit from backend caching, and the cache-key parameters** (mirror capability
report §6; the frontend `RootsCache` mirrors these keys for in-session dedup):

| UI call | Backend-cacheable? | Cache-key parameters |
|---|---|---|
| List, no search | yes | `sort`, `page`, `pageSize` |
| List, with search | **no (bypass)** | (free-text → unbounded keys) |
| Root summary | yes | `rootId` |
| Root words (simple/tashkeel) | yes | `rootId`, `wordKind`, `detailPage`, `pageSize` |
| Root ayahs | yes | `rootId`, `detailPage`, `pageSize` |
| Root surahs / missing | yes | `rootId` |
| Root lemmas / stems | yes | `rootId` |

- **Frontend does not implement backend cache.** It uses `ApiResponseCache.getOrLoad` to (a)
  **dedupe in-flight** identical requests (`shareReplay`) and (b) **reuse loaded detail state** so
  re-opening a tab or paging back doesn’t refetch. Keep frontend cache keys aligned with the backend
  key parameters above.
- **Avoid duplicate requests:** child components never call the API directly (they consume facade
  state); only the facade loads, and only for the active view; switching tabs reuses cached/served
  state.
- **Logging parameters the frontend must send so backend logs are useful:** `rootId`, `view`,
  `subView` (`wordView`/`surahView`), `page`/`pageSize` (`detailPage`), `sort`, and search presence
  (`hasSearch` — the backend derives this; the frontend sends the `search` query normally and must
  **not** push raw search text into any frontend logging). No Quran/ayah/word text is logged on
  either side.

---

## Final verdict: FRONTEND_READY_WITH_NOTES

The Roots Explorer frontend is fully buildable as a sibling of Feature 014: a new
`/dashboard/words/roots` page with a split-screen layout, a reused/extended CDK roots table, and a
**persistent independent-scroll side panel** replacing the F014 modal. Highlighting, surah/ayah
lists, count chips, pagination, the frontend cache, the deep-link builder, and the lazy-load +
restore-from-URL facade pattern are all reusable. The notes in §10 are contract/UX decisions
(sort-key naming, lemma-count consistency, lemmas/stems non-interactive-for-now, per-root vs global
word counts, drawer/RTL placement) — none are blockers. Resolve the §10 notes and the feature is
ready to enter the combined backend + frontend implementation plan.
