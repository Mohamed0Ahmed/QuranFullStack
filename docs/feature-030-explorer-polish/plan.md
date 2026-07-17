# Feature 030 — Explorer & Overlay Polish Batch (9 items) — Implementation Plan

- **Branch:** `restyle/flat-green-light` (contains the flat-green restyle + Feature 029).
- **Status:** PLAN ONLY — read-only analysis performed 2026-07-17; no code changed.
- **Scope:** frontend-only. No backend, DTO, DB, import, or cache-key-format changes.
  Anything requiring one is FLAGGED and stops there (see N8).
- **Doctrine inputs:** `DESIGN.md` (flat parchment + green, hairline structure, one
  floating-layer shadow, allowed-green list), `PRODUCT.md`,
  `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` §16/§17,
  `docs/feature-029-floating-detail-navigation-ui/plan.md` (U1 reservation precedent,
  overlay architecture). All file:line references below verified on this branch.

Frontend root shorthand: `FE = Frontend/quran-dashboard-ui/src`.

---

## 0. Summary verdicts

| Item | One-line verdict | Size |
|---|---|---|
| C1 ayah-card look | **Already satisfied** by 029 Change A — verification pass only | verify |
| N1 single-type no-reload | Real defect confirmed (redundant HTTP + skeleton + URL write); guard in the 2 shared chip components fixes all 4 render sites | S |
| N2 fixed modal dims | Width already stable; height is content-driven — fix `block-size` in shell SCSS + phone band | S |
| N3 no loading shift | Audit complete: 13 SHIFTS cases app-wide (worst: `selected-ayah-section`); ranked per-case fixes, U1 ResizeObserver pattern needed only twice | L |
| N4 count-range chips + Enter | Per-keystroke fetch confirmed; 3-chip reduction is a preset-only change (URL grammar untouched); per-metric threshold = open decision | M |
| N5 search dropdown gating | Exactly ONE focus-opened dropdown exists (shared association filter, 5 instances); 1-component fix + ArrowDown opener | S |
| N6 modal header context | Kind label + ayah count; `ayahsCount` already in every summary DTO — pure wiring, zero new requests | S |
| N7 ayah hover | **No ayah hover exists today** — introduce word→line→page-view hover chain; recommended variant is a flat accent tint (NO shadow needed) | M |
| N8 header sorting | Backend supports ONE baked direction per sort key (asc/desc toggle = out-of-scope backend gap); ship 2-state cycle (natural ⇄ default) + mobile fallback; dropdown removed on desktop | XL |

---

## C1 — Ayah-card color/look (verification only)

### Current state (evidence)

- `FE/app/shared/ui/ayah-card/ayah-card.component.scss:3-12` — the frame **already**
  declares the approved look verbatim:
  `background: var(--qd-surface); border: 1px solid var(--qd-border); border-radius: var(--qd-radius-sm); box-shadow: none;`
  plus compact logical padding/gap. Not `--qd-section-bg`, not `--qd-surface-recessed`.
- Consumers carry no frame overrides except the sanctioned selected-occurrence accent
  hairline: `FE/app/features/mushaf/components/mutashabihat-groups-card/mutashabihat-groups-card.component.scss:64-68`
  (`__occurrence--selected { border-color: var(--qd-border-accent); }` — permitted by
  the §17 contract, UI_STYLE_SYSTEM.md:724).
- Former per-context recolors already removed: `FE/styles/_explorer-detail-lists.scss:379-380, 423-424`
  (comments record the Feature-029 Change A removal).
- Dark theme comes free via token remaps (`FE/styles/_themes.scss:4,20,22`).

### Target

No frame change. Deliverable = a verification pass in both themes across the three
consumers (Words ayah-matches loaded + skeleton, Mushaf Similar Ayahs, Mushaf
Mutashabihat with a selected occurrence).

### Known contrast nuance (open decision C1-a, cosmetic)

The identical frame *reads* differently by context: Mushaf study cards sit on
`--qd-surface` (hairline-only look, `study-context-section.component.scss:16-20`),
Words panels sit on `--qd-explorer-detail-bg` (= `--qd-section-bg`,
`_components.scss:340-351`) so the card reads faintly lighter there. If the user wants
identical *reading*, that is a panel-background decision — never a `qdAyahCard` fork.

Adjacent observation (separate item if wanted): `.study-context-section` still carries
`box-shadow: var(--qd-shadow-sm)` (`study-context-section.component.scss:20`) — a
no-op in light (`--qd-shadow-sm: none`) but nominally against flat doctrine hygiene.

**Risks:** the context tone difference above being "fixed" inside `qdAyahCard` would
fork the §17 contract; removing the mutashabihat selected override during cleanup
would lose the sanctioned accent hairline; no dark-specific frame rules may be added
(tokens handle dark).
**Affected files:** none. **Tests:** none new. **Phase:** P7 (final verification).

---

## N1 — Single-type (and any active-chip) re-click = no-op

### Current state (evidence)

Only lemmas and stems have ayah type chips (roots have none; word-types detail is
per-type by construction; `type-distribution-list` is display-only). Defect chain,
single-type case:

1. Chip renders active while state is null:
   `FE/app/features/words/components/lemma-ayah-type-filters/lemma-ayah-type-filters.component.ts:39-45`
   — `isSelected` special case `items().length === 1 && selectedTypeCode() === null`.
2. Click emits unconditionally (`selectTypeCode`, lines 31-33).
3. Page guard passes (null ≠ 'N'):
   `lemmas-explorer-page.component.ts:213-220` → `setAyahTypeCode` **and** a URL write
   (`typeCode=N` appears). Stems mirror: `stems-explorer-page.component.ts:236-243`.
4. Overlay adapter guard passes the same way:
   `lemma-detail-overlay-adapter.component.ts:196-212` → `replaceTopFrame` →
   frame effect (131-143) → `applyUrlState`.
5. Controller flips panel to `loading` and reloads:
   `lemmas-detail.controller.ts:201-235` / `stems-detail.controller.ts:323-357`.
6. Cache key differs (`null → 'all'` vs `'N'`): `lemmas-cache.ts:27-30` — guaranteed
   cache miss; real HTTP request with a `typeCode` param (`lemmas.api.ts:81-95`).
   `ApiResponseCache` dedupes exact keys only (`core/caching/api-response-cache.ts:12-39`)
   — it cannot absorb this.

So today: one redundant HTTP request + full list skeleton + a URL identity change per
single-type entity click.

### Target

Clicking any chip already rendered active (`aria-pressed="true"`) is a complete no-op
in all four render sites: no emit, no state call, no URL write, no HTTP. The general
"re-click of active chip ⇒ return" rule subsumes the single-type case.

### Approach

Guard at the **single shared point both render sites pass through before any
state/URL mutation** — the two chip components' `selectTypeCode`:

```ts
const alreadyActive = typeCode === null ? this.isAllSelected() : this.isSelected(typeCode);
if (alreadyActive) { return; }
this.typeCodeChange.emit(typeCode);
```

applied identically in `lemma-ayah-type-filters.component.ts` and
`stem-ayah-type-filters.component.ts` (lockstep). Downstream guards stay untouched as
defense in depth. A controller-only guard is NOT sufficient: pages call
`updateQueryParams` directly and adapters call `replaceTopFrame` directly.

Do not touch `isSelected` and do not start writing the only-type code into state/URL
(shared-URL identity + `aria-pressed` contract tests depend on the visual-only
convention).

### Behavior change to accept (open decision N1-a)

Downstream guards include `&& detailPage === 1`, so re-clicking the active type while
on detail page > 1 today resets to page 1 and reloads. The component-level no-op
removes that hidden affordance. Recommended: accept (pagination owns page
navigation). If the affordance must survive, the guard moves to the four handlers
instead (effective-selection comparison) at the cost of 4 edit sites.

**Affected files:** the 2 chip components + their specs, 2 page specs, 2 adapter
specs (8 files).
**Risks:** existing adapter/page specs emit `typeCodeChange` programmatically and
bypass the guard — new no-op tests MUST click the rendered chip button.
**Tests:** chip specs — 4 cases (single-type chip click emits nothing; active chip in
multi-type set emits nothing; active عرض الكل emits nothing; non-active chip still
emits). Page/adapter specs — click-through: router/`replaceTopFrame` not called, API
call count unchanged.
**Phase:** P1.

---

## N2 — Fixed modal dimensions (+ shared geometry base for N3 §7)

### Current state (evidence)

- `FE/app/shared/ui/detail-modal-shell/detail-modal-shell.component.scss:4-9` —
  `inline-size: min(100%, 46rem)` (width already stable) but only
  `max-block-size: min(92dvh, 44rem)` — height is content-driven; tab switches,
  pagination, and loading passes resize the dialog around its flex-centered backdrop.
- Internal scroll region already exists: `__body { flex: 1; min-block-size: 0; overflow-y: auto; }` (lines 40-45).
- No phone band exists (only a reduced-motion block, lines 75-81).
- Body scroll is locked while open (`ScrollLockService`, component ts:59-68) — a fixed
  dvh height is safe.
- Precedent for banded fixed height: `.qd-modal.explorer-detail-modal`
  (`_components.scss:438-446`).

### Target

- Desktop/tablet: `block-size: min(92dvh, 44rem)` (replace `max-block-size`);
  `__header { flex-shrink: 0; }`; `__body` remains the only scroller.
- Phone (≤ `$qd-bp-phone-max` 767px, `_breakpoints.scss:2-6`): backdrop padding
  `var(--qd-space-2)`, dialog `block-size: min(94dvh, 44rem)` — near-fullscreen.
- Switching tabs/pagination/loading/empty/not-found never changes rendered width or
  height. This one change also closes N3's overlay-dialog-resize finding (N3 row 13).

### Risks

- Shallow states (skeleton, not-found) render a tall dialog with empty space —
  accepted trade for zero resize (option to shrink desktop cap to ~40rem is open
  decision N2-a, visual call).
- Verify at 320×568 landscape that `min(..., 44rem)` + reduced phone padding keeps the
  dialog inside the viewport.
- Global `.qd-modal` / `.qd-modal-backdrop` primitives untouched — change lives in the
  shell's own SCSS.

**Affected files:** `detail-modal-shell.component.scss` (+ spec).
**Tests:** shell spec — body carries the scroll-region contract; geometry classes
present; open/close/restore focus behavior unchanged. Height itself = browser
verification (devtools, tab-switch at 1440/390).
**Phase:** P2 (with N6 — same component).

---

## N6 — Modal header: entity type label + ayah count

### Current state (evidence)

- Header today: Back (depth>1) · `h2 {{ titleText() }}` (ellipsis) · Close;
  polite live region re-announces `titleText`
  (`detail-modal-shell.component.html:14-48`). Shell inputs: `titleText`, `depth`,
  labels — no type/count inputs (`.ts:39-53`).
- Title publish chain: adapters compute entity title → `EntityDetailOverlayTitleStore`
  (`entity-detail-overlay-title.store.ts:11-24`) → host `title` computed falls back to
  `ENTITY_DETAIL_KIND_TITLES[kind]` (`entity-detail-overlay-host.component.ts:90-97`).
- **Ayah count already arrives with the title** — `ayahsCount` exists on all five
  summary DTOs the adapters load: `RootSummaryDto.ayahsCount`
  (`core/api/generated/models/root-summary-dto.ts:5`), lemma/stem/unique summary DTOs,
  and `WordTypeSummaryDto` (`word-types.models.ts:74,78`). No new fetch.

### Target

Header (RTL, inline-start→end): Back · **kind chip** (new
`ENTITY_DETAIL_KIND_LABELS`: root `جذر`, lemma `صيغة معجمية`, stem `أصل صرفي`,
unique `كلمة`, wordType `نوع الكلمة`) · h2 title (unchanged) · **ayah-count meta**
(`الآيات: N`, N = `summary.ayahsCount`) — count box ALWAYS rendered with reserved
`min-inline-size` (~6rem, `tabular-nums`), text fades in on load (opacity only,
disabled under reduced motion) ⇒ zero layout shift on arrival. Count lives outside
the h2 and outside the live region (no double announcement); dialog gains
`aria-describedby` → count element.

### Approach (condensed)

1. `entity-detail-overlay.labels.ts` — add `ENTITY_DETAIL_KIND_LABELS` +
   `entityDetailAyahCountText(count)`.
2. Title store — add `ayahCount` signal + `setAyahCount`; cleared in existing `clear()`.
3. Five adapters — `entityAyahCount` computed from `panelState().summary?.ayahsCount`
   (unique: drilldown state) + one publish effect beside the existing `setTitle` effect.
   Use the summary count, NOT `ayahs.totalCount` (tab/filter-dependent).
4. Host — `kindLabel` computed from `topFrame().kind` (synchronous), `countText`
   computed from store; bind both.
5. Shell — optional inputs `kindLabel = input('')`, `countText = input('')` (defaults
   keep shell presentation-only and other callers valid); kind chip = hairline border +
   muted text (no fill, no shadow — flat doctrine; NOT `qd-chip`, which carries
   interactive semantics).
6. Update UI_STYLE_SYSTEM.md §17 shell contract in the same change.

### Risks

- Inlining the count into the h2 or the live region would double-announce on load —
  the count deliberately lives outside both; reviewers must not "simplify" it back in.
- A count wider than the reserved ~6rem box steals width from the flex:1 title
  (ellipsis absorbs it; max real `ayahsCount` ≤ 6236 — widen the reservation once if
  needed), never moves Back/Close.
- Shell inputs must stay optional with `''` defaults (shared presentation component);
  `entity-detail-overlay-invariant.spec.ts` / `…-ayah-continuity.spec.ts` may assert
  header text — keep green after the restructure.

### Open decisions

- **N6-a digits:** Latin digits (matches explorer tables) vs Eastern-Arabic —
  recommend Latin.
- **N6-b count semantics:** header count is the ENTITY-level ayah count; on
  lemma/stem ayah tabs narrowed by `typeCode` the visible list total is smaller.
  Encode: header count is entity-stable, does not track filters (recommended).

**Affected files:** shell (html/ts/scss/spec), labels, title store, host (+spec),
5 adapters (+5 specs), UI_STYLE_SYSTEM.md — ~20 files.
**Tests:** shell spec (chip renders/omits; count box always present; `--visible`
toggle; count outside live regions; `aria-describedby`); host spec (kind label per
kind; countText lifecycle); 5 adapter specs (ayahCount published + cleared on destroy).
**Phase:** P2.

---

## N3 — No loading layout shift, app-wide (audit + ranked fixes)

### Audit table (every loading/skeleton state, both sweeps)

Verdicts: **SHIFTS** = layout moves; STABLE = verified holds footprint.

| # | Where | Evidence | Verdict → fix class |
|---|---|---|---|
| 1 | Word Types scope-counts strip (**confirmed defect**) | `word-type-scope-counts.component.html:9-23,41-54`, `.scss:14-52`; trigger `word-types-explorer.facade.ts:302-313` | **SHIFTS** −16.5px per filter change (loaded item ≈68.5px vs skeleton bar 52px); also error branch ≈54px and `!tableFailed()` unmount → 0px. Fix: skeleton mirrors the loaded two-line item box; reserve strip height across states → static mirror |
| 2 | Word Types view tabs insertion | `word-types-explorer-page.component.html:71-78` | **SHIFTS** (first load only): tabs gated on `listState().tree`. Fix: reserved slot (or render disabled — open decision N3-c) |
| 3 | Word Types pagination unmount on view switch | `word-types-explorer-page.component.html:98-107`; facade nulls rows on view change | **SHIFTS**: ~2.75rem bar unmount/remount. Fix: reserved slot |
| 4 | Detail-list pagination unmounts (5 lists) | `ayah-matches-list.component.html:65-73`, `root-words-list.component.html:42-50`, lemma-words-list, stem-words-list, `word-type-grouped-words-list.component.html:52-60` | **SHIFTS** inside panel bodies + overlay. Fix: always-rendered `__pagination-slot` wrapper `min-block-size: 2.75rem` (shared rule in `_explorer-detail-lists.scss`); do NOT keep `qd-pagination` mounted (it self-hides when `totalCount<=pageSize`, `pagination.component.ts:64`) |
| 5 | Explorer page-level error/empty/notFound banners | `roots-explorer-page.component.html:51-70` (+ lemmas 64-83, stems ~76-93, unique-words 76-103) | **SHIFTS**: banner inserts ABOVE the fixed table+panel grid (~4.5rem push). Fix: open decision N3-b — reserved status slot (minimal diff) vs move states into table shells (mounted-shell doctrine, larger) |
| 6 | `explorer-result-count` | `.component.html:1-24`, `.scss:26-30` | **SHIFTS** on error (renders nothing); ~1.4px loading residual. Fix: same-height muted line on error + align line boxes |
| 7 | word-types-table resolution states ≤1023px | `word-types-table.component.html:106-121`, `.scss:156-158`, `_words-explorer-layout.scss:120-123` | **SHIFTS** ≤tablet only (card ~40rem → ~5rem on empty/error/selectPrompt). Fix: `min-block-size: min(70vh, 40rem)` on `__state` in the tablet band |
| 8 | word-type-filter first load | `word-type-filter.component.html:2,105-107` | **SHIFTS** (initial): one-line placeholder → multi-card toolbar. Fix: skeleton toolbar mirroring trigger cards + static per-breakpoint baseline; ONLY escalation candidate for full U1 ResizeObserver if wrap variance proves it |
| 9 | Mushaf page area | `mushaf-page-area.component.html:2-31`; sticky panel `mushaf-reader-page.component.scss:52-62,88-97` | **SHIFTS** ≤1023px (whole area collapses to a 3rem `qd-panel-skeleton` bar; study section shoves up); desktop panel fixed but skeleton is a 3rem bar in a full-height panel. Fix: static measured min-height ≤1023px (15-line page at fixed 28rem column — deterministic) + stretch the panel skeleton on desktop; keep header nav slot reserved. Cached pages never flash (sync cache emit) |
| 10 | Mushaf `selected-ayah-section` (**largest offender**) | `.component.html:141-198`, `.scss:156-169,187-194`; runner delay `mushaf-ayah-study-load.runner.ts:39-52` (700ms); partial mobile floors `_components.scss:607-641` | **SHIFTS**: loaded tafsir (arbitrary height, up to 1000s px) collapses to ~10rem skeleton; desktop has NO reservation; mobile loading floor (48vh/24rem) > loaded floor (42vh/22rem) causes settle-shift. Fix: **full U1 ResizeObserver port** (record natural block size on success → `min-block-size: max(baseline, reserved)` while loading, copy the `--loading` class scoping from `selected-word-section.component.scss:46-54`) + raise one-line ayah skeleton floor + equalize mobile floors |
| 11 | Similar-ayahs card loading | `similar-ayahs-card.component.html:5-13` | **SHIFTS** (grow-on-load: 3 text lines → N qdAyahCards). Fix: qdAyahCard-shaped placeholders, count = `min(similaritySummary.similarAyahCount, cap)` (already loaded pre-tab); fallback static min-height |
| 12 | Mutashabihat groups card loading | `mutashabihat-groups-card.component.html:5-13` | **SHIFTS** (same family). Same fix via `mutashabihatGroupCount` |
| 13 | Floating detail overlay dialog resize | `detail-modal-shell.component.scss:4-9` | **SHIFTS** — closed by **N2's fixed block-size** (no separate work) |
| 14 | dashboard-home meta badges | `dashboard-home.component.html:7-31` | **SHIFTS** ~1.3rem (skeleton row lacks badge height + `margin-top`). Fix: static `--qd-skeleton-h` + margin |
| 15 | Explorer tables loading (roots/lemmas/stems/unique/word-types) | e.g. `roots-table.component.html:25-54`; fixed body `_explorer-tables.scss:71-76` | STABLE (fixed body height; skeleton rows reuse row grid). Cosmetic: word-types shows 5 skeleton rows vs siblings' 12 (open N3-d) |
| 16 | `selected-word-section` | 029 U1 | STABLE — precedent; untouched |
| 17 | Ayah-study source selector loading | `selected-ayah-section.component.html:91-139` | STABLE (2.75rem parity); minor: single-source loaded state renders shorter bare span — optional same-min-height fix |
| 18 | Association filter dropdown loading | popover `explorer-association-filter.component.scss:64-80` | STABLE (anchored popover, never moves page) |
| 19 | Lemma/stem ayah-type chips loading | `lemma-ayah-type-filters.component.scss:18-20` | STABLE (~1px; noted >4-chip wrap caveat) |
| 20 | word-drilldown-modal loading | `.component.html:23-26` | STABLE outer (fixed hosts); cosmetic default 6-line skeleton |
| 21 | Footer health, app-shell, top-navbar, surah-jump-picker, pagination, qd-state, skeleton primitives | (part-2 sweep) | STABLE / no loading states |
| 22 | Route-level lazy-load | `app.routes.ts:19-47` | GAP, not a shift — no loading UI exists at all; explicitly deferred (open N3-e) |
| 23 | Detail-list ROW skeletons: root-words / lemma-words / stem-words (8 rows), word-type-grouped-words (6) | `root-words-list.component.html:13-25` + mirrors | STABLE-in-panel — skeleton rows mirror the loaded `qd-detail-list__row` grid; ~4px/row shorter (skeleton text 1rem vs quran-font ~1.5rem) absorbed by the flex-1 fixed panel viewport. Only their pagination shifts (row 4) |
| 24 | surah-occurrences-list + missing-surahs-list | `surah-occurrences-list.component.html:13-25` (+ missing-surahs `:12-23`) | STABLE — 8 flat skeleton rows share the `qd-detail-list__row--flat { min-block-size: 44px }` a11y floor with loaded rows (`_explorer-detail-lists.scss:44-51`); fixed/flex-1 containers; no pagination |
| 25 | root-lemmas / root-stems / lemma-stems / stem-lemmas / type-distribution lists | `root-lemmas-list.component.html:13-25`, `stem-lemmas-list.component.html:15-30`, `type-distribution-list.component.html:14-23` | STABLE-in-panel — 4-8 mirrored skeleton rows inside fixed-height panel body / fixed `explorer-detail-modal`; skeleton-vs-full-list row-count mismatch only matters in the floating overlay, whose height N2 fixes |
| 26 | Detail panels' own summary loading (`aria-busy` branches) incl. word-types page panel skeletons and the lemmas-page sr-only-only fallback | `word-types-explorer-page.component.html:123-141`, `lemmas-explorer-page.component.html:240-244` | STABLE-in-panel — desktop panel column is fixed-height (`_words-explorer-layout.scss:109-134`: `block-size: var(--qd-explorer-table-card-height)`), ≤1023px panels become the fixed `qd-modal explorer-detail-modal` (`_components.scss:438-446`); internal swaps never move page geometry |
| 27 | Ayah-matches list viewport (standalone) | `ayah-matches-list.component.html:7-21`, `.scss:11-15` | STABLE standalone — fixed `block-size: min(58vh, 30rem)`; 4 skeleton qdAyahCards mirror the card chrome (multi-line ayat clipped inside the fixed viewport); pagination shift = row 4 |

### Target

Every load/refetch anywhere in the app repaints in place with zero outer layout
movement: skeletons structurally mirror loaded geometry (same box, padding, line
boxes); pagination/status/tab slots stay reserved through loading; fixed-height
shells never collapse on empty/error/prompt states at any breakpoint; variable-height
content (row 10) reserves its last natural size via the U1 pattern; reservations
apply ONLY while loading — loaded content always sizes itself.

### Approach (ranked, simplest-stable-first)

Static structural mirrors and reserved slots close 11 of 13 SHIFTS rows; the full U1
ResizeObserver pattern is needed only for row 10 (`selected-ayah-section`) and as
escalation for row 8. All changes are template/SCSS/component-local — no facade, URL,
DTO, or HTTP-cache identity changes; every `role="status"`/`aria-busy`/sr-only node
stays; reduced-motion shimmer fallback already global (`_components.scss:592-605`).

Order inside the phase: row 1 (confirmed defect) → rows 4, 6, 14 (one-liners) → rows
3, 2, 7 → rows 9, 11, 12 → row 10 (U1 port) → row 8 → row 5 (needs decision N3-b).

### Open decisions

- **N3-a (shared observer):** port U1 ResizeObserver logic into
  `selected-ayah-section` as a local copy, or extract a shared directive/utility used
  by both mushaf sections? Recommend local copy first (keeps the U1 precedent file
  untouched), extract on third consumer.
- **N3-b (page banners):** reserved status slot (minimal diff, permanent ~4.5rem
  empty strip) vs moving error/empty into the 4 table shells (mounted-shell doctrine
  §17, bigger diff). Recommend the table-shell variant; ship slot-reservation if the
  batch wants minimal risk.
- **N3-c:** Word Types view tabs — reserved slot vs render-disabled pre-tree.
- **N3-d:** align word-types-table to 12 skeleton rows for parity while touching it.
- **N3-e:** route-level loading placeholder = new scope; deferred out of this batch.

### Risks

- `selected-ayah-section` reservation interacts with three layered min-heights
  (component scss:187-194, the `_components.scss:607-641` mobile overrides, the new
  reserved var) — copy the U1 `--loading` class scoping exactly
  (`selected-word-section.component.scss:46-54`) or a too-tall floor can survive an
  error/empty settle.
- Reserving the previous ayah's height while a DIFFERENT ayah loads holds stale
  geometry (U1-accepted trade-off).
- Count-driven placeholders (rows 11/12) need `similaritySummary` counts; on deep
  links the study may still be loading — fall back to a fixed placeholder count.
- Measured static baselines (row 9 mushaf page) invalidate silently if font metrics /
  column-width tokens change (same accepted risk as the shipped U1 baselines);
  pages 1-2 over-reserve slightly.
- Pagination slot height must match the real `qd-pagination` row; the rare jump-error
  line still grows the slot (accepted).
- The scope-counts loaded height derives from the Tailwind preflight line-height —
  mirror with real skeleton line elements, not a hardcoded calc.
- Do NOT touch mushaf-page-view/line/word internals — all mushaf fixes stay at the
  container/skeleton level (Quran-rendering invariant).

**Affected files:** ~24 words-feature files + ~13 mushaf/dashboard/shared files
(specs included). The audit table carries one primary evidence ref per row; the
per-row fix sites are the cited components' html/scss (+ `_explorer-detail-lists.scss`
for the shared pagination-slot rule).
**Tests:** per component — skeleton mirrors loaded structure (class/element presence);
pagination/status slots present in BOTH loading and loaded renders;
`selected-ayah-section` reservation lifecycle (mirrors the 4 U1 tests of
`selected-word-section.component.spec.ts`); `selected-word-section` spec untouched as
regression guard. Pixel outcomes = browser pass with devtools layout-shift overlay at
1440/768/390.
**Phase:** P4.

---

## N4 — Count-range filter: 3 chips per metric + Enter-only custom commit

### Current state (evidence)

- Chips today are family buckets, 4-5 + مخصّص per metric:
  `words-filter-presets.ts:21-41` (`occurrences: 1 / 2–10 / 11–100 / 101–1000 / 1001+`;
  `ayahsSurahs: 1 / 2–10 / 11–50 / 51+`; `subCount: 1 / 2–5 / 6–20 / 21+`).
- Metrics per page: roots 7, lemmas 6, stems 5, unique-words 3
  (`roots.models.ts:108-117`, `lemmas.models.ts:102-110`, `stems.models.ts:104-111`,
  `unique-words.models.ts:126-131`). Word Types does NOT use this filter.
- Chips are presentation-only; URL stores the actual range (`min..max` grammar,
  fail-closed parse) — `words-range-filters.ts:46-91`; request params
  `<apiKey>Min/Max`; cache fragment `serializeRangeFiltersKey`.
- **Per-keystroke fetch confirmed:**
  `explorer-count-range-filter.component.html:55,70` bind `(input)` →
  `onMinInput/onMaxInput` (`.ts:98-106`) → immediate `emit` → page
  `onRangesChange` → `router.navigate` (no debounce, no replaceUrl —
  `roots-explorer-page.component.ts:146-149` + 3 mirrors) → facade `switchMap` →
  HTTP. Typing "150" = up to 3 navigations + 3 fetches + 3 history entries.
  (Text search IS debounced 300ms; ranges are not.)

### Target

**(a)** Each metric row shows exactly 3 chips: `[أكثر من N] [أقل من N] [مخصّص]`.
Chips remain presentation-only; `أكثر من N` emits `{min: N+1, max: null}` → URL
`N+1..`; `أقل من N` emits `{min: null, max: N-1}` → URL `..N-1`. URL grammar, urlKeys,
API params, and cache fragment are byte-identical mechanisms — old shared links with
bucket ranges still parse and render as an active مخصّص state (existing
`isCustomActive` fallback, component.ts:58-62).

Per-metric threshold table (**open decision N4-a — primary**):

| Metric | Proposed N | Grounding |
|---|---|---|
| المواضع (occurrences) | 100 | existing 11–100/101–1000 boundary; Zipf tail into thousands (mean ≈47/root, DB inventory) |
| الآيات (ayahs) | 100 (alt 50) | high-count metric, max 6,236 |
| السور (surahs) | **50** | hard cap 114 — ">100" matches almost nothing; existing top bucket is 51+ |
| كلمات بدون تشكيل / بالتشكيل | **10** | subCount family top bucket is 21+ — counts mostly ≤20 |
| الصيغ المعجمية / الأصول الصرفية | **10** | same subCount family |

Alternative: fixed 100 everywhere — rejected-by-default (produces dead ">100" chips on
surahs + all sub-count metrics; ~96% of unique words occur ≤10 times per the
feature-026 validation data). Implementation: per-family/per-metric `threshold`
(family map + optional `RangeMetric.threshold` override — needed because the shared
`ayahsSurahs` family would otherwise force ayahs and surahs to one N).

**(b)** Custom min/max become draft-local: typing writes only component-local draft
signals (NO emit). `keydown.enter` (with preventDefault) commits the normalized draft
through the existing emit path — URL/history/cache/HTTP change ONLY on commit.
`keydown.escape` reverts the draft to the last committed value without emitting.
Blur = no-op (draft persists). Drafts re-sync when `ranges()` changes externally
(URL restore, Back/Forward, clear-all). Existing `parseBound`/`normalize` guards
(component.ts:126-141) run at commit time.

### Open decisions

- **N4-a:** threshold table above vs fixed 100 (product call before implementation).
- **N4-b:** exact-N gap — strict `أكثر من ١٠٠`/`أقل من ١٠٠` leaves exactly 100 only
  reachable via مخصّص; alternative: one side inclusive (`١٠٠ فأكثر` = min 100).
- **N4-c:** digits in chip labels (Latin, matching current buckets, vs Arabic-Indic;
  affects testids derived from labelAr — consider stable slug testids instead).
- **N4-d:** optional small `تطبيق` apply button beside the inputs for touch users
  (Enter stays primary) — recommend include.

**Affected files:** presets (+spec), shared labels, `words-range-filters.ts`,
filter component (html/ts/spec), 4 page specs, words README (§count-range bullet) —
~12 files.
**Risks:** page specs click bucket testids (`range-filter-bucket-occurrences-11–100`,
`roots-explorer-page.component.spec.ts:183` + mirrors) — must be rewritten; regression
test must assert NO emit on bare input; shared family split must not desync surahs
chips across the 4 explorers.
**Tests:** component spec rewrite (3 chips per metric with per-metric N; gt/lt emit
shapes; re-click clears; input events don't emit; Enter commits incl. min>max
fail-open; Escape reverts; clear-all resets drafts); presets spec → threshold
derivation; url-sync/range-filter/api specs stay green untouched (identity proof).
**Phase:** P3.

---

## N5 — Search dropdown behavior (all search boxes)

### Current state (evidence — app-wide sweep)

Exhaustive `<input>` sweep: exactly **one** focus-opened suggestion dropdown exists —
the shared `ExplorerAssociationFilterComponent`, 5 instances (lemmas root;
unique-words primary-type [clientFilter] + primary-root; stems primary-root +
primary-lemma).

- Opens unconditionally on focus today:
  `explorer-association-filter.component.ts:158-163` (`onFieldFocus` → `openPanel()`
  unless `reopenSuppressed`).
- Typing already opens (`onQueryInput`, lines 169-178) — target "open on first char"
  path exists.
- "Value selected" is representable: `hasSelection = computed(() => selectedId() !== null)`
  (line 83); note the input text is cleared on select, so selection ≠ input value.
- **Already compliant, zero change:** mushaf `surah-jump-picker` (panel opens only via
  trigger button; full listbox keyboard model, `.ts:94-141,164-180`) and
  `source-selector` (same trigger pattern, `.ts:103-114`). Plain no-dropdown inputs
  (explorer main search ×4, word-types search, pagination/page-number/range inputs)
  are out of scope. No third-party autocomplete anywhere.

### Target

One component changes; all 5 instances inherit:

1. `onFieldFocus()` opens only when `hasSelection() || query().trim().length > 0`
   (still gated by `reopenSuppressed`). Empty + unselected focus = closed.
2. With a selection, focus re-opens the options (client-filtered type picker shows its
   static list; server-searched pickers show last-loaded options).
3. New `ArrowDown` (and Alt+ArrowDown) keydown on the field: preventDefault +
   `openPanel()` even when empty — the ARIA-combobox escape hatch so keyboard users can
   browse without typing; recommend moving focus to the first option button on open.
4. Everything else untouched: Escape/outside-click/focusout/select-close,
   `reopenSuppressed` lifecycle, `onClear`, `aria-haspopup/expanded/controls`, and the
   deliberately plain non-listbox option buttons (template comment html:49-50).
5. Update `words/README.md:162-164` (documents the old focus-open contract) in the
   same change.

### Open decisions

- **N5-a:** on selected-reopen with empty options (URL-restored server pickers), the
  panel opens but shows NO options until the user types — technically meets "focus
  re-opens the options" but may miss its intent. Optional fix: emit `searchChange('')`
  on that reopen to populate the default first-page list (reuses an existing request
  shape, so URL/HTTP-cache identity holds — but focus then triggers a fetch).
  Default: NO fetch (minimal scope); flip to the fetch variant if the empty panel is
  judged unacceptable during review.
- **N5-b:** keep roles as-is (no `role="combobox"` claim without a listbox model) —
  recommended; full combobox model would be a separate item.
- **N5-c:** does the unique-words type picker (small static list) get an open-on-focus
  exemption for discoverability? Item text implies uniform — recommend uniform +
  ArrowDown opener.

**Affected files:** association filter (ts/html/spec) + words README (4 files).
**Risks:** spec helper `openPanel(fixture)` (spec:52-57) drives ~8 tests via
FocusEvent — must switch to typing/ArrowDown; discoverability regression on the static
type picker (mitigated by ArrowDown + placeholder).
**Tests:** flip spec:173 to "does NOT open on empty unselected focus"; keep spec:186
(opens on typing); add: opens on focus with selection; ArrowDown opens from empty;
Escape suppression still holds with selection; after clear, focus stays closed.
**Phase:** P1.

---

## N7 — Mushaf ayah hover prominence

### Premise correction (evidence)

**No ayah hover state exists today.** `mushaf-word.component.scss:1-43` has no
`:hover` rule at all (only `cursor: pointer`, focus outline, the selected-word wash,
and the `--highlighted-ayah` **text-color** state, which is the click/URL-synced
`focusAyah` highlight — `mushaf-reader.facade.ts:76,201,286`). Repo-wide grep for
`hoveredVerseKey|pointerenter|mouseenter` under `src/app`: zero hits. An ayah spans
multiple flex lines of separate `<qd-mushaf-word>` buttons (`mushaf-line.component.html:19-27`),
so CSS-only `:hover` cannot paint the whole ayah — a verseKey-driven state through the
existing input chain (page-view → line → word) is required. N7 therefore *introduces*
the hover affordance.

### Target (recommended = Option A, flat-preserving, NO shadow)

Hovering any word of an ayah (hover-capable devices) paints a calm, clearly visible
background wash behind ALL words of that ayah; keyboard focus paints the same wash for
parity. Background + radius only — glyphs, fonts, padding, line metrics untouched;
never animated beyond a `--qd-t-fast` background-color transition (color transitions
are kept under reduced motion per the `_components.scss:592-597` precedent).

- New mushaf-scoped token:
  `--qd-mushaf-ayah-hover-bg: color-mix(in oklch, var(--qd-mushaf-word-selection-indicator) 10%, var(--qd-bg))`
  — visibly stronger than `--qd-surface-hover` (ΔL≈0.022 vs the parchment canvas =
  near-invisible, `_tokens.scss:7,28-34`), same hue family as the 16% selected-word
  wash so it reads as "pre-selection" and sits below selection in the intensity ladder
  (10% < 16% — approximate: selection mixes into `--qd-surface` while hover mixes into
  `--qd-bg`, so the ladder is calibrated live per N7-b, not proven by the percentages
  alone). Resolves per theme automatically (10% gold-into-navy in dark; override
  in `_themes.scss` only if dark QA wants ~8%).
- CSS: `@media (hover: hover) { .mushaf-word--hovered-ayah:not(.mushaf-word--selected-word) { background-color: var(--qd-mushaf-ayah-hover-bg); border-radius: var(--qd-radius-sm); } }`
  + an unguarded focus-driven variant; rule ordered above `--selected-word` so
  selection always wins.
- State chain: `hoveredVerseKey` input + `ayahHover` output on `mushaf-word`
  (pointerenter/leave + focus/blur; marker exclusion mirrors
  `isHighlightedAyahWord`, `.ts:23-35`); pass-through on `mushaf-line`; owner signal in
  `MushafPageViewComponent` — **never the facade, never the URL** (`focusAyah`
  semantics + URL/cache identity untouched). Reset on page change (verse keys are
  global — stale-key phantom wash otherwise).

Option B (evaluated honestly, NOT recommended): a shadow affordance — inset hairline
ring `inset 0 0 0 1px color-mix(... 18%, transparent)` (paints as an edge; precedent
`mushaf-word.component.scss:36`) or a true outer shadow. Because each word is a
separate flex item, per-word fragmentary rings/shadows read noisy (worse in dark's
heavy shadow values). The user-allowed shadow exception is not worth spending here —
Option A achieves the prominence flat.

### Doctrine recording (required either way)

- Option A: annotate UI_STYLE_SYSTEM.md §16.1 "Hover fill" row (the one documented
  exception: mushaf ayah-hover uses `--qd-mushaf-ayah-hover-bg` because
  `--qd-surface-hover` is imperceptible on the reading canvas) and extend §16.3 item 6
  wording; mirror **word-identically** in `DESIGN.md` §2 (the lists are locked mirrors,
  UI_STYLE_SYSTEM.md:~645). No §16.2 shadow-doctrine edit needed.
- Option B only: add the explicit exception sentence to §16.2 "Shadow doctrine (flat)".
- Update `features/mushaf/README.md` gotchas: hover is component-local,
  pointer/focus-only, background-only, never URL-synced.

### Open decisions

- **N7-a:** Option A (flat tint, recommended) vs Option B (inset ring) — user allowed
  a shadow; recommendation stands unless overridden.
- **N7-b:** mix percentage calibration 8–12% against the real canvas (selection sits
  at 16%; hover must stay visibly below).
- **N7-c:** include the ayah-marker glyph in the wash? Recommend NO (mirrors the
  highlight exclusion).

**Affected files:** mushaf-word (ts/html/scss/spec), mushaf-line (ts/html/spec),
mushaf-page-view (ts/html/spec), `_tokens.scss`, UI_STYLE_SYSTEM.md, DESIGN.md,
mushaf README — 14 files.
**Risks:** OnPush fan-out per pointer move (~15 lines dirty; same cost class as the
existing click highlight — coalesce emits if jank appears); per-word wash
fragmentation matches the selected-word treatment (continuous band impossible without
restructuring Quran DOM — out of bounds); doctrine files must be edited identically in
one commit; touch devices gated via `@media (hover: hover)`.
**Tests:** word spec (class on verseKey match; marker excluded; hover+selected class
combination; emits on pointerenter/focus, null on leave/blur — source-safe
`buildWord()` factory); line spec (pass-through); page-view spec (signal set/cleared;
reset on page change).
**Phase:** P5.

---

## N8 — Column-header sorting; remove top sort dropdown (all explorers)

### Backend contract verdict (decision input — inspected read-only, both sides)

**The read contract supports exactly ONE baked-in direction per sort key. No
direction parameter exists anywhere.** Unknown sort values → HTTP 400 InvalidSort
(`RootsController.cs:44,83-84`; strict `TryParse` switches in `RootSort.cs:27-40` and
siblings).

| Explorer(s) | Key | Direction shipped | Opposite | Evidence |
|---|---|---|---|---|
| roots/lemmas/stems/unique | `mushaf-order` (default) | ASC | **NO** | `RootsListDerivation.cs:81-83` + mirrors, `EfUniqueWordsReader.List.cs:178` |
| roots/lemmas/stems/unique | `occurrences` | DESC | **NO** | `RootsListDerivation.cs:74-77` + mirrors |
| roots/lemmas/stems/unique | `alpha` | ASC (ordinal) | **NO** | `RootsListDerivation.cs:78-80` + mirrors |
| word-types | `occurrences`/`ayahs`/`surahs` | DESC | **NO** | `EfWordTypesReader.Sql.cs:501-508, 307-314` (DESC baked into SQL strings) |
| word-types | `mushaf-order`/`alpha` | ASC | **NO** | same |
| roots/lemmas/stems/unique | count columns آيات/سور/بدون تشكيل/بالتشكيل/الصيغ/الأصول | — | **NO KEY AT ALL** | parsers accept only the 3 keys; columns visible `roots-table.component.html:14-20` |

Server defaults: roots/lemmas/stems/unique = `mushaf-order` ASC when param absent
(`GetRootsPageHandler.cs:17,25` + siblings) — this IS ترتيب المصحف and is also an
ordinary parseable key (explicit return-to-default is safe). **Word Types default =
`occurrences` DESC** (`WordTypesHandlerValidation.cs:18`), not mushaf order.

**FLAGGED OUT OF SCOPE (backend gaps, no backend change planned):** opposite
directions for every key (occurrences-asc, alpha-desc, reverse-mushaf, ayahs-asc,
surahs-asc) and sort keys for the six unkeyed count columns. Client-side re-sorting of
the fetched page is NOT a substitute (server pagination at 1000 rows/page — reversing
one page corrupts global order) and is rejected. Backend cache keys also embed the
sort value (`WordTypesCacheKeys.cs:12-16`), confirming direction support is a
coordinated backend feature (enum + parser + derivations + SQL + cache keys + tests).

### Current frontend state (evidence)

- Dropdown is per-page hand-rendered native `<select>` (not shared):
  `roots-explorer-page.component.html:27-39` (+ lemmas 41-52, stems 53-64,
  unique-words 52-64, word-types 39-46 visible-label variant). Handlers are pure URL
  navigations: `onSortChange → updateQueryParams({ sort, page: null })`
  (`roots-explorer-page.component.ts:150` + mirrors); word-types via
  `explorerFacade.changeSort` (facade:232-233).
- Option sets: roots/lemmas/stems `mushaf-order | occurrences | alpha`
  (labels e.g. `ROOTS_SORT_LABELS`, `roots.labels.ts:89-94`); unique-words same keys;
  word-types adds `ayahs`, `surahs` (`word-types.labels.ts:81-87`).
- Header cells today are plain non-interactive `role="columnheader"` divs
  (`roots-table.component.html:8-23` + 4 mirrors); zero `aria-sort` anywhere in the app.
- **Mobile hazard:** the entire header row is `display: none` ≤tablet in all five
  table SCSS files (`roots-table.component.scss:93-96` et al.) — the dropdown is
  currently the ONLY sort control there.
- URL/cache identity: param name `sort`, validate-or-default parse
  (`roots-url-sync.ts:28-31,86-88` + mirrors; `word-types-url-sync.ts:75,312-314`);
  cache keys embed sort as an opaque string slot (`roots-cache.ts:8-10`:
  `roots:list:${sort}:${search}:p${page}`; word-types
  `wordtypes:table:…:sort:${sort}:p${page}`; scope-counts key deliberately omits sort
  — guarded by `word-types-cache.spec.ts:95-99`).

### Target (v1, frontend-only — works entirely on existing keys)

- Sortable header cells render a real full-cell `<button type="button">`; click (or
  Enter/Space, native) **cycles 2 states: column's natural direction ⇄ default
  (ترتيب المصحف / param absent)**. The third (opposite-direction) step of the ideal
  asc→desc→default cycle is BLOCKED on the backend gap above; the cycle function is
  written to accept it later without rework (open decision N8-c fixes the future wire
  format as suffixed values of the existing `sort` param, e.g. `occurrences-asc` —
  never a new `dir` param, which would change URL/cache-key composition).
- Column ↔ key map (existing wire values only): roots — الجذر→`alpha`,
  المواضع→`occurrences`; lemmas — الصيغة المعجمية→`alpha`, المواضع→`occurrences`;
  stems — الأصل الصرفي→`alpha`, المواضع→`occurrences`; unique-words —
  الكلمة→`alpha`, المواضع→`occurrences`; word-types — الكلمة/dimension column→`alpha`,
  المواضع→`occurrences`, الآيات→`ayahs`, السور→`surahs`. `mushaf-order` maps to the
  no-active-column release state ⇒ **no dropdown option loses functionality** (verified
  against all 5 option sets). All other columns stay plain text (no backend key — see
  flagged gap).
- Active sort header per §16 doctrine: label `--qd-accent-text` + trailing direction
  glyph ▲/▼ (separate `aria-hidden` span; fixed direction per key — ▼ on count
  columns, ▲ on text/mushaf); hover `--qd-surface-hover`; `:focus-visible` ring;
  optional 2px `--qd-accent` bottom indicator (green thread, §16.1 row 3 — open
  decision N8-d); NO solid green fill, no shadow.
- A11y: `aria-sort="ascending"|"descending"` on the `role="columnheader"` element
  (absent when inactive); Arabic `aria-label` on the button naming column + next
  action; RTL-safe (logical `text-align: start`, vertical arrows).
- Styling added ONCE on the `.qd-explorer-table` base (`_explorer-tables.scss`:
  new `__sort-button` + `.qd-is-sorted`) — extend the base per §17, never fork per
  table. UI_STYLE_SYSTEM.md §17 updated in the same change.
- Top dropdown block (label + select) deleted from all 5 desktop layouts along with
  `sortOptions`/`sortLabels`/`onSortChange` plumbing, `.qd-explorer__sort`
  (`_words-explorer-layout.scss:77-79`) and the `.qd-explorer-controls-secondary
  .qd-select` width fix (194-215); `.qd-explorer-controls-secondary` itself STAYS
  (hosts the count-range filter).
- **Mobile (≤tablet):** keep a compact sort affordance — recommended: the existing
  `<select>` markup retained inside a tablet-max-only wrapper, same URL contract
  (open decision N8-e for a nicer chip/menu later). Sorting must not silently
  disappear on phone. **Explicit deviation from the verbatim "REMOVE the dropdown
  entirely":** the header row — the replacement control — is `display:none` ≤tablet,
  so full removal would delete the only mobile sort control; the dropdown is removed
  from all desktop layouts and survives only ≤tablet. Needs user sign-off (N8-e).
- URL + cache identity preserved by construction: param stays `sort`, existing values
  verbatim, default stays param-absent, cache keys absorb values as opaque strings.
  Wire: pages keep `updateQueryParams({ sort: nextOrNull, page: null })`; word-types
  keeps `facade.changeSort`.

### Open decisions

- **N8-a (product):** Word Types default stays `occurrences` (its release state
  returns to occurrences-desc and المواضع shows active-by-default — one page whose
  default is not mushaf order) vs changing the default to `mushaf-order` for
  cross-page consistency (changes what the parameterless URL returns — scope change).
  Recommend: keep `occurrences` in this batch; flag the consistency question.
- **N8-b:** ship v1 2-state cycle now (recommended) vs block the whole item on a
  future backend direction feature.
- **N8-c:** freeze the future direction wire format with the backend owner before
  extending key lists (`occurrences-asc` suffix style recommended).
- **N8-d:** active header = accent-text + arrow only, or + 2px green-thread bar (both
  doctrine-legal; pick one for all 5 tables).
- **N8-e:** mobile fallback shape (keep select ≤tablet — cheapest — vs compact chip/menu).
- **N8-f:** word-types grouped views — confirm the dimension header is the
  alpha-mapped column there (`GroupedOrderBy` sorts dimension `norm_text`,
  `EfWordTypesReader.Sql.cs:312`).

**Affected files:** ~59 (5 tables html/ts/spec, 5 pages html/ts/spec, 5 models,
5 labels, 5 url-sync + specs, word-types facade + spec + cache spec, 2 style partials,
shared labels, UI_STYLE_SYSTEM.md, words README).
**Risks:** page specs drive sorting through select testids (must be rewritten);
`word-types-cache.spec.ts:95-99` scope-counts no-sort guard must stay green; table
container `tabindex=0` keyboard-nav (`roots-table.component.html:5-6`) must not
conflict with header buttons entering the tab order; `column` URL param namespace
already used for detail focus (`roots.models.ts:102`) — sort state stays inside the
`sort` value.
**Tests:** table specs (button only on mapped columns; emits `sortToggled`;
`aria-sort` lifecycle; plain headers stay plain; RTL glyph span aria-hidden); page
specs (first toggle navigates `{sort: key, page: null}`, release navigates
`{sort: null, page: null}`; dropdown gone from desktop / moved under mobile wrapper);
url-sync specs (existing default-fallback + verbatim tests stay green); word-types
facade spec (changeSort resets page); a11y label assertions.
**Phase:** P6.

---

## Cross-cutting constraints (bind every phase)

1. **Quran rendering faithful and unchanged** — no file under mushaf-word/line/
   page-view glyph internals, ligatures, fonts, `--qd-font-quran` is touched except
   N7's button-box background/class (no glyph, padding, or metric change); skeletons
   never approximate Quran text.
2. **Arabic-first / RTL** — logical properties, RTL-verified header buttons, chips,
   modal header order.
3. **Keyboard + WCAG 2.1 AA** — native buttons everywhere; `aria-sort`; `aria-pressed`
   chips unchanged; ArrowDown combobox escape hatch; focus-visible rings; count/kind
   header outside live regions; hover never the only indicator.
4. **Light + dark** — all new colors via existing theme-mapped tokens; the one new
   token (N7) derives from theme vars; dark stays interim navy+gold.
5. **Flat doctrine** — no new shadows anywhere under the recommended choices (N7
   Option A is shadow-free); the only shadow remains the floating-layer
   `--qd-shadow-lg`. Sole sanctioned conditional: if the user overrides N7-a to
   Option B, that ONE inset hairline ring ships with the explicit §16.2 exception
   sentence — nothing else may cite it as precedent.
6. **URL + HTTP-cache identity** — N1 prevents a spurious param; N4 commits only on
   Enter with unchanged grammar; N8 reuses the `sort` param and values verbatim; N5/N7
   never touch the URL; no cache-key format changes anywhere.
7. **Docs-in-same-change rule** — words README (N1 behavior, N4 chips, N5 contract,
   N8 URL contract), mushaf README (N7, N3 mushaf reservations), UI_STYLE_SYSTEM.md
   §16/§17 (N6 shell contract, N7 doctrine note + DESIGN.md mirror, N8 table
   contract).
8. **Specs:** follow the repo test-command rule (vitest via `ng test`, glob ends
   `*.spec.ts`); jsdom lacks `matchMedia`/`ResizeObserver` — guard/stub (U1 FakeResizeObserver
   precedent in `selected-word-section.component.spec.ts`).

---

## Integrated implementation order

| Phase | Items | Rationale | Commit |
|---|---|---|---|
| P0 | Decisions | Resolve open decisions (min. N4-a, N7-a, N8-a/b/e; rest have recommended defaults) | — |
| P1 | N1 + N5 | Two small behavior guards, independent, immediately user-visible; no visual redesign | `fix(words): active-chip no-op + association dropdown focus gating (N1, N5)` |
| P2 | N2 + N6 | One shared component (shell) owns both; fixed geometry also closes N3 row 13 before the N3 sweep starts | `feat(overlay): fixed modal geometry + kind/count header (N2, N6)` |
| P3 | N4 | Filter chips + Enter commit; depends only on the N4-a decision | `feat(words): 3-chip count ranges + Enter-commit custom bounds (N4)` |
| P4 | N3 | App-wide loading reservations, ordered inside the phase: confirmed defect → one-liners → mushaf U1 port → banner decision | `fix(ui): loading states hold layout footprint app-wide (N3)` |
| P5 | N7 | Hover chain + token + doctrine mirror edits | `feat(mushaf): ayah hover wash (N7)` |
| P6 | N8 | Largest structural change last; lands on a stable base; includes dropdown removal + mobile fallback | `feat(words): column-header sorting, sort dropdown removed (N8)` |
| P7 | Final guard | C1 verification pass (both themes, 3 consumers); full suite; production build; browser matrix (1440/768/390, RTL, keyboard, light+dark, reduced motion, layout-shift overlay for N3/U1 gates); README/doc diff check | `docs(feature-030): verification record` |

Phases are commit-sized and independently revertible; P1–P3 and P5 have no
inter-dependencies and can reorder freely if a decision stalls. P4 should follow P2
(shell geometry settled). P6 last (touches the most files).

---

## Open decisions (consolidated)

**Required before the affected phase starts:**

1. **N4-a — per-metric thresholds** (blocks P3): proposed table
   {مواضع 100, آيات 100, سور 50, كلمات ١٠, صيغ ١٠, أصول ١٠} vs fixed 100. Evidence
   says fixed-100 creates dead ">100" chips on 5 of 7 metrics. → recommend the table.
2. **N7-a — hover form** (blocks P5): flat accent tint 10% (recommended, no doctrine
   exception beyond a hover-fill note) vs inset-ring shadow variant (requires the
   §16.2 exception sentence). N7-b calibration happens live either way.
3. **N8-a/b — Word Types default + cycle depth** (blocks P6): keep occurrences
   default (recommended) + ship 2-state cycle now (recommended, backend gap flagged) —
   the full asc→desc→default cycle needs a future backend feature (N8-c wire format to
   be frozen with the backend owner; NOT in this batch).
4. **N8-e — mobile sort fallback** (blocks P6): keep the select ≤tablet (recommended)
   vs new compact control.
5. **N3-b — page-level banners** (blocks the last step of P4): reserved status slot
   vs mounted-shell table states (recommended).

**Defaults proposed (override only if disagreed):** N1-a drop the page>1 re-click
reset affordance; N2-a keep 44rem cap; N6-a Latin digits; N6-b entity-stable count;
N3-a local U1 copy; N3-c reserved tabs slot; N3-d 12 skeleton rows; N3-e route-level
placeholder deferred; N5-a no fetch on reopen; N5-b keep roles; N5-c uniform
behavior; N4-b strict boundaries (100 via مخصّص); N4-c Latin digits + slug testids;
N4-d include تطبيق button; N7-c marker excluded; N8-d accent-text + arrow (no bar);
N8-f dimension header is the alpha column in grouped views; C1-a leave panel tone as
is.

---

## Explicitly flagged out of scope (do not implement in this batch)

- **Backend sort directions + new sort keys** (N8): opposite-direction variants and
  keys for the six unkeyed count columns — a coordinated backend feature (enums,
  parsers, derivations, SQL, backend cache keys, tests). Frontend v1 ships the
  2-state cycle on existing keys only; client-side page re-sorting rejected as
  incorrect under server pagination.
- **Route-level lazy-load placeholder** (N3-e): a gap, not a shift; new scope.
- **`--qd-explorer-detail-bg` panel-tone unification** (C1-a) and the
  `.study-context-section` `--qd-shadow-sm` hygiene nit: separate items if wanted.
- Any change to `docs/design-preview/decisions.html` (user-owned, untracked) or the
  merged feature-026/027/029 spec artifacts (historical; only live READMEs update).
