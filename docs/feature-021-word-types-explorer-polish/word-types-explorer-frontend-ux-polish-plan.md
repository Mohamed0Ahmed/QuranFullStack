# Word Types Explorer — Frontend UX Polish Plan

Branch: `021-word-types-explorer-polish`
Scope: **frontend only.** No backend, no migrations, no importer/DataPipeline, no Quran data, no
POS taxonomy or count-semantics change, no redesign of other explorers, no global design-token
changes unless unavoidable (and justified inline).

Status: plan only — no code written, no source modified.

This plan is handed to an implementation agent phase by phase. Every step names the exact file,
symbol, and change. Do not renegotiate the constraints below.

---

## 0. Already done (do not redo / do not regress)

Previous plan (`word-types-explorer-polish-implementation-plan.md`) is implemented + reviewed:
backend subtype-count semantics, particle children, zero-count hiding, parent-does-not-load-rows,
and the Phase-2 table/details scroll shell. **Keep all of it intact.** In particular the leaf-gating
in `WordTypesExplorerFacade.loadList()` and the Roots-style table scroll shell stay; this plan only
adjusts *what happens to previously-loaded rows* and the *visual/detail* layers.

## Hard constraints

- Frontend only. If any change appears to require a backend/contract change, **stop and raise it as
  a blocker** (§7) instead of planning it.
- No changes to count semantics, tree shape, or URL contract meaning (params may be dropped from
  *use*, but the parser must stay tolerant of old deep-links).
- Do **not** edit shared components consumed by other explorers (e.g. `ayah-matches-list`,
  `surah-occurrences-list`, `missing-surahs-list`) — change only how the Word Types page *uses* them.
- Reuse existing `qd-*` / `explorer-*` shared classes and `--qd-*` tokens. Add only local
  `word-type(s)-*` classes.

---

## 1. Executive summary

Five frontend UX problems remain:

1. **Filter cards + subtype chips look poor** — the 4 parent buttons read as plain inputs; subtype
   choices read as scattered text; counts are not badge-like; selected/hover/focus/expanded states
   are weak.
2. **Switching parent hides existing table results** — after a subtype is chosen and rows exist,
   clicking a *different* parent clears the table. Desired: keep the previous rows visible, open the
   new parent's subtype panel, and refresh rows only when a real subtype/leaf is chosen.
3. **Loading states are abrupt** — cards/table blink to empty/loading. Desired: skeletons, and no
   destructive replace of existing results when only a parent is clicked.
4. **Details panel carries a useless "analysis" tab** — remove the analysis view/tab and the
   in-ayah "عرض التحليل" action; keep ayahs, surahs, and the concise summary.
5. **Table rows are not visually distinct** — rows should feel clickable, selected obvious,
   hover/focus clear, and root/lemma/stem/count cells organized (not plain text beside plain text).

Phases: **A** state/behavior (keep rows on parent switch) → **B** filter cards → **C** table rows →
**D** remove analysis → **E** tests + manual QA. A and D are the only phases that touch state/models;
B and C are visual; E is verification.

---

## 2. File inventory

### Phase A — behavior/state
| File | Change |
| --- | --- |
| `state/word-types-explorer.facade.ts` | `loadList()` + `handleTreeOnlyResponse()`: stop nulling `rows`; keep previous rows visible when a parent (non-leaf) is selected; only `selectPrompt` when no rows ever loaded. |
| `components/word-types-table/word-types-table.component.{html,ts,scss}` | Add a loading **skeleton** body; keep rows rendered with `aria-busy` while a new leaf loads (no blink to placeholder). |
| `pages/word-types-explorer-page/word-types-explorer-page.component.html` | Table gate now keys off "rows exist" instead of `status !== 'selectPrompt'`. |

### Phase B — filter cards + subtype chips (visual)
| File | Change |
| --- | --- |
| `components/word-type-filter/word-type-filter.component.html` | Card structure for parent triggers; grouped, labelled subtype chip group; counts as badges. |
| `components/word-type-filter/word-type-filter.component.scss` | Card/selected/hover/focus/expanded styling via shared tokens; chip styling; badge count. |

### Phase C — table row polish (visual)
| File | Change |
| --- | --- |
| `components/word-types-table/word-types-table.component.scss` | Stronger clickable/hover/selected/focus affordances; organized meta + count cells; skeleton styles. |
| `components/word-types-table/word-types-table.component.html` | Minor class/markup hooks for cell grouping + skeleton (shared with Phase A). |

### Phase D — remove analysis view
| File | Change |
| --- | --- |
| `models/word-types.models.ts` | Drop `'analysis'` from `WordTypeDetailView` + `WORD_TYPE_DETAIL_VIEW_KEYS`; remove `analysis` from `WordTypesDetailState`. |
| `models/word-types.labels.ts` | Remove `analysis` entries from `WORD_TYPE_DETAIL_TAB_LABELS`/`WORD_TYPE_DETAIL_TAB_ARIA`; remove `WORD_TYPES_ANALYSIS_ACTION_LABEL`. |
| `state/word-types-detail.facade.ts` | Remove `analysis` from `INITIAL_PANEL`, `setAnalysisLocation()`, analysis branch in `loadActiveView()`, and `location`-for-analysis handling. |
| `state/word-types-detail-view.loader.ts` | Remove `onAnalysis`, the `'analysis'` case, and the `MushafWordAnalysisApi`/`WordAnalysisDto` imports. |
| `state/word-types-detail-panel.updates.ts` | Remove `analysis: null` from `restoredRowNotFoundUpdate`. |
| `pages/word-types-explorer-page/word-types-explorer-page.component.ts` | Remove `onAnalysisLocationRequested`, `analysisLoadState`, `analysisActionLabel`, `SelectedWordSectionComponent`/`ResourceLoadState` imports + `location/column: 'analysis'` writes. |
| `pages/word-types-explorer-page/word-types-explorer-page.component.html` | Remove the `view === 'analysis'` block + `qd-selected-word-section`; drop analysis inputs from `qd-ayah-matches-list`. |
| `state/word-types-url-sync.ts` | No signature change; `normalizeView` already degrades unknown `view` → default (`ayahs`), so old `?view=analysis` deep-links stay valid. Leave `location`/`column` params parsed-but-tolerated. |
| `state/word-types-cache.ts` | Remove the now-unused `WordTypesCacheKeys.analysis` key (dead code). |

### Phase E — tests
| File | Change |
| --- | --- |
| `pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts` | Update the parent-switch test to expect the table **stays**; keep initial-no-subtype selectPrompt test; add analysis-tab-absent + `view=analysis`→`ayahs` fallback tests. |
| `components/word-type-filter/word-type-filter.component.spec.ts` | Selector/class updates if Phase B renames DOM hooks; assert selected/expanded state classes. |
| `components/word-types-table/word-types-table.component.spec.ts` | Assert skeleton renders while loading with no rows; row selection/visual hooks stable. |
| `components/word-type-details-panel/word-type-details-panel.component.spec.ts` (if present) | Assert only 2 tabs (ayahs, surahs); no analysis tab. |
| `state/word-types-detail.facade.spec.ts` (if present) | Remove analysis-path assertions; add `view=analysis`→`ayahs` normalization. |

### Reference only (read, never edit)
- Roots skeleton/cards: `components/roots-table/roots-table.component.{html,scss}`,
  `pages/roots-explorer-page/*`.
- Shared classes/tokens: `src/styles/_components.scss`, `_tables.scss` (if present), `_explorer-*.scss`.

---

## 3. Phase A — keep previous rows when switching parent; polished loading

### Problem trace
`selectType()` (facade) navigates with `childCode: null` → `requestKey` changes → `loadList()` runs
with `leafSelected = false` → today it sets `rows: null` then `handleTreeOnlyResponse()` sets
`status: 'selectPrompt', rows: null` → **table disappears.** The desired rule: `selectPrompt` only
when **no rows have ever loaded**; otherwise keep the last rows on screen and just open the new
parent's subtype panel (the filter already opens it via `openPanelType.set(...)`).

### A1 — `state/word-types-explorer.facade.ts`

1. In `loadList()`, change the pre-load status update to **not** clear rows:
   ```ts
   // was: { ...current, status: 'loading', rows: null, errorMessage: '' }
   this.state.update((current) => ({ ...current, status: 'loading', errorMessage: '' }));
   ```
   Rationale: rows are replaced by `handleListResponse` on success, or preserved on a parent-only
   (tree-only) selection. The table shows the old rows with `aria-busy` while a *new leaf* loads.

2. In `handleTreeOnlyResponse(tree)`, keep existing rows and choose the status by whether rows exist:
   ```ts
   this.state.update((current) => ({
     ...current,
     status: current.rows ? 'success' : 'selectPrompt',
     tree: treeData,
     rows: current.rows,       // keep whatever was there (null on first load)
     errorMessage: '',
   }));
   ```
   The failure branch is unchanged (still sets `status: 'error'`, `rows: null`). Result: first load
   (no rows) → `selectPrompt`; parent switch after results → `success` with the previous rows still
   visible.

3. `handleListResponse` — unchanged (already sets `rows` to the new page and `status` empty/success).
   `selectChild()` / `selectCase/Tense/Voice` / `changeSort` still `clearWordTypesSelection()` and
   reset the page → they set `childCode` (leaf) → `forkJoin` loads and replaces rows. No change.

4. Keep `selectType()` semantics (reset child + secondary filters, clear selection). The only visible
   change is that the previous rows persist until a real subtype is chosen.

> **Stale-row note (intended):** after a parent switch the visible rows belong to the *previous*
> subtype until the user picks a new one. A row's identity (`tashkeelWordId + contextCode + case +
> tense + voice`) is self-contained, so clicking a lingering row still resolves its own summary
> correctly. This matches the requested behavior ("do not clear previous rows just because a parent
> was clicked"). Do **not** add logic to disable the stale rows unless QA finds it confusing (§7 Q2).

### A2 — table loading polish (`components/word-types-table/*`)

Today the table shows a plain `word-types-table__placeholder` text when `rows()` is null. Replace the
abrupt states with:

- **No rows yet + loading** → render a **skeleton body** (mirror `roots-table__body--loading`:
  a few skeleton rows using shared `qd-skeleton qd-skeleton--text` / `--rounded-md`, `aria-busy`,
  `role="rowgroup"`, an `aria-live` status with the loading label). Add to
  `word-types-table.component.html` an `@if (loading() && !rows())` branch and matching skeleton
  styles in `.scss`.
- **Rows present + loading** (new leaf/sort/page loading over old data) → keep the body rendered, add
  `[attr.aria-busy]="loading()"` on the body and a subtle `.word-types-table__body--busy` dim
  (opacity ~0.6, `pointer-events` allowed) so results don't blink away.
- Preserve `data-word-types-row`, `role`s, and the `__header`/`__body`/`__header-gutter` structure
  from Phase 2.

### A3 — page template (`word-types-explorer-page.component.html`)

Current gate: `@if (listState().status !== 'selectPrompt') { <table/> <pagination/> }`. Change the
gate so the table shows whenever rows exist **or** a leaf load is in flight, and the
`selectPrompt` empty-state shows only when there are no rows:

```html
@if (listState().status === 'selectPrompt' && !listState().rows) {
  <p class="qd-empty-state" data-testid="word-types-select-subtype">{{ selectSubtypeLabel }}</p>
}

@if (listState().rows || listState().status === 'loading') {
  <qd-word-types-table … />
  @if (listState().rows; as page) { <qd-pagination … /> }
}
```

Keep the existing `error` / `empty` branches. Net effect: initial load → prompt only; parent switch
with prior results → table stays; new subtype → table stays and shows the busy/skeleton state.

### Acceptance criteria — Phase A
- Fresh page, default `noun`, no subtype → shows "اختر نوعًا فرعيًا لعرض الكلمات"; no table; **no** rows request.
- Select a subtype → rows load (skeleton if first) → table renders.
- With results visible, click a **different** parent → table **stays visible** (previous rows), the
  new parent's subtype panel opens, **no** rows request fires until a subtype is picked.
- Pick a subtype under the new parent → rows refresh in place (busy state, no blink to empty).
- Error path still shows the error state; empty result still shows the empty label.

---

## 4. Phase B — filter cards + subtype chips (visual only)

Reuse `qd-card`, the elevation/hover ladder, `--qd-accent*`, `--qd-border(-strong)`, and `qd-badge`.
No token additions unless a needed role is missing (justify inline if so). Keep all existing DOM
hooks used by tests/focus management: `.word-type-filter__button`, `.word-type-filter__expand`,
`.word-type-filter__child-button`, `data-word-type-code`, `aria-current`, `aria-expanded`,
`aria-controls`, `#word-type-filter-panel-<code>`.

### B1 — parent trigger cards (`.html` + `.scss`)
- Make `.word-type-filter__trigger` read as an intentional **filter card**: padded surface, hairline
  border, resting `shadow-sm`, hover `border-strong` + `shadow` + ≤2px lift (per `UI_STYLE_SYSTEM`
  §15E/F). The label (`__label`) is the card title; the count (`__count`) is a `qd-badge`-style pill.
- **Selected** (`qd-is-selected`): accent-tint fill + accent border + `--qd-accent-text` label
  (already partly present) — make it unmistakable (e.g. inset accent ring + a small check/indicator,
  not color-only, per §12 no-color-alone).
- **Expanded** (`.word-type-filter__expand.qd-is-selected` / `aria-expanded`): visually tie the open
  `+/−` toggle to its card (shared active treatment).
- Focus: keep the existing `:focus-visible` ring; ensure it shows on the card and the toggle.

### B2 — subtype chip group (`.html` + `.scss`)
- Wrap `.word-type-filter__children` as a **labelled group** (small eyebrow/heading inside the panel,
  e.g. the parent label + "الأنواع الفرعية") so chips read as a grouped, selectable set — not text
  scattered in a box. The panel already is `qd-surface-elevated` with shadow; keep it.
- Style `.word-type-filter__child-button` as **chips/segmented options**: pill radius, clear resting
  border, hover surface, and a strong **selected** state (accent-tint + accent border + inset ring),
  count as a small badge. Keep the responsive `auto-fit minmax(10rem,1fr)` grid.
- Secondary controls (`__secondary` case/tense/voice selects) stay; align their spacing to the chip
  group.

### Acceptance criteria — Phase B
- The 4 parents look like deliberate filter cards, distinct from plain inputs, with obvious selected
  vs unselected, hover, focus, and expanded states.
- Counts render as badges (parent + subtype).
- Subtypes look like a grouped, selectable chip set with a clear selected chip.
- Light **and** dark themes both read correctly; RTL layout intact; reduced-motion respected
  (existing `@media (prefers-reduced-motion)` block extended to any new transitions).

---

## 5. Phase C — table row visual polish (visual only)

Builds on the Phase-2 table shell. Keep grid/columns, `min-inline-size:0`, header/body split, and the
scrollbar-gutter sync. Change only affordances/organization.

### C1 — `.scss`
- **Clickable feel:** rows get a clear resting separator + hover surface (`--qd-section-bg`, already
  present) plus cursor + subtle hover lift/inset; ensure the whole row button reads as interactive.
- **Selected:** keep the accent-tint + inset accent ring, and add a start-edge accent marker
  (logical `border-inline-start`) so selection is obvious beyond color (per §12).
- **Focus:** keep `:focus-visible` ring; ensure it is visible over the selected state.
- **Cell organization:** the word cell (`--cell--word`) stays the emphasized Quran-font headline;
  meta cells (root/stem/lemma) use muted, ellipsized text with the existing mobile `data-label`
  pattern; count cells keep the centered `qd-word-count-chip` (already badge-like). Add subtle column
  separation/alignment so it stops reading as "plain text beside plain text."
- **Skeleton:** add `.word-types-table__body--loading` + skeleton row/cell styles (shared with A2).

### C2 — `.html`
- Only add class hooks needed for the above and the skeleton branch; no semantic/ARIA changes; keep
  `data-word-types-row`.

### Acceptance criteria — Phase C
- Rows visibly read as clickable; hover and focus are clear; the selected row is unmistakable
  (marker + tint, not color-only).
- Root/stem/lemma/counts are visually organized and aligned; counts read as badges.
- Skeleton shows on first load; light/dark + RTL correct; no regression to Phase-2 scroll/alignment.

---

## 6. Phase D — remove the analysis view/tab cleanly

Goal: details panel keeps **ayahs**, **surahs**, and the concise summary; the analysis tab and the
in-ayah "عرض التحليل" action are removed with no dangling state or broken deep-links.

Drive the removal from the type so the compiler lists every site:

### D1 — `models/word-types.models.ts`
- `WordTypeDetailView`: `'ayahs' | 'surahs'` (drop `'analysis'`).
- `WORD_TYPE_DETAIL_VIEW_KEYS`: `['ayahs', 'surahs']`. (`WORD_TYPE_DETAIL_VIEWS` follows; the panel's
  `tabs` derive from this, so the analysis tab disappears automatically.)
- `WordTypesDetailState`: remove the `analysis: WordAnalysisViewModel | null` field (and the
  `WordAnalysisViewModel` import if now unused).

### D2 — `models/word-types.labels.ts`
- Remove the `analysis` keys from `WORD_TYPE_DETAIL_TAB_LABELS` and `WORD_TYPE_DETAIL_TAB_ARIA`
  (the `Record<WordTypeDetailView, …>` type now forbids them).
- Remove `WORD_TYPES_ANALYSIS_ACTION_LABEL`.

### D3 — `state/word-types-detail.facade.ts`
- `INITIAL_PANEL`: remove `analysis: null`.
- Remove `setAnalysisLocation()` entirely.
- `setView()`: drop the `location: view === 'analysis' ? … : null` special-case (always `null`).
- `loadActiveView()`: remove the `view === 'analysis' && !location` empty branch and the `onAnalysis`
  handler; keep ayahs/surahs. Remove the `toWordAnalysisViewModel` import and the `analysis:` field
  write.
- `PanelUrlState.location` and the `location` plumbing were analysis-only for behavior — keep the
  field parsed for URL tolerance but it no longer drives a load (safe to leave as an always-`null`
  passthrough, or remove if trivially clean; do not break `parseWordTypesQueryParams`).

### D4 — `state/word-types-detail-view.loader.ts`
- Remove the `onAnalysis` handler from `WordTypesDetailViewHandlers`, the `'analysis'` `case`, and the
  `MushafWordAnalysisApi` + `WordAnalysisDto` imports.

### D5 — `state/word-types-detail-panel.updates.ts`
- Remove `analysis: null` from `restoredRowNotFoundUpdate`. `isPaginatedWordTypeView` (`=== 'ayahs'`)
  is unaffected.

### D6 — `state/word-types-cache.ts`
- Remove the unused `WordTypesCacheKeys.analysis(...)` key.

### D7 — `pages/word-types-explorer-page/word-types-explorer-page.component.ts`
- Remove `onAnalysisLocationRequested()`, `analysisLoadState` computed, `analysisActionLabel` getter,
  and the `SelectedWordSectionComponent` + `ResourceLoadState` + `WORD_TYPES_ANALYSIS_ACTION_LABEL`
  imports (and `SelectedWordSectionComponent` from the `imports:` array).
- In `selectRow()`, `onCountOpened()`, `onPanelViewChange()` remove the `location: null` /
  `column: 'analysis'` writes tied to analysis (keep `word`/`contextCode`/`view`/`detailPage`).
  `ayahsPageForView` stays (it feeds the ayahs list).

### D8 — `pages/word-types-explorer-page/word-types-explorer-page.component.html`
- Remove the `@if (panelState().view === 'analysis') { <qd-selected-word-section …/> }` block.
- On `<qd-ayah-matches-list>` drop `[showAnalysisAction]="true"`, `[analysisActionLabel]`, and
  `(analysisRequested)`. **Do not edit `AyahMatchesListComponent` itself** (shared) — only stop
  passing the analysis inputs from this page.

### D9 — `state/word-types-url-sync.ts`
- No change required for correctness: `isWordTypeDetailView('analysis')` becomes `false`, so
  `normalizeView` returns the default (`ayahs`). Old `?view=analysis&location=…&column=analysis`
  deep-links therefore resolve to the ayahs view with the selection preserved — **no broken URL.**
  Leave `location`/`column` parsing in place (tolerated, ignored).

### Acceptance criteria — Phase D
- Details panel shows exactly two tabs (الآيات / السور) plus the summary; no analysis tab, no
  "عرض التحليل" action inside ayah rows.
- Selecting a row, switching ayahs↔surahs, and paging ayahs all still work and deep-link.
- Visiting a stale `?view=analysis` URL loads the ayahs view for that row (no error, no blank panel).
- `dotnet`/TS build is clean (no unused import or dangling `analysis` reference).
- No other explorer (roots/lemmas/stems) is touched; `AyahMatchesListComponent` is unchanged.

---

## 7. Phase E — tests + manual QA

### E1 — automated tests (run with the fork cap: `npm test` already sets `VITEST_MAX_FORKS=2`)

Focused command:
```bash
npm test -- --watch=false \
  --include='src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts' \
  --include='src/app/features/words/components/word-type-filter/word-type-filter.component.spec.ts' \
  --include='src/app/features/words/components/word-types-table/word-types-table.component.spec.ts' \
  --include='src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.spec.ts' \
  --include='src/app/features/words/state/word-types-detail.facade.spec.ts' \
  --include='src/app/features/words/state/word-types-url-sync.spec.ts'
```

Required cases:
- **Initial no-subtype** (default `noun`, no `childCode`) → `selectPrompt`, no `<qd-word-types-table>`,
  no `getRows` call. (Keep existing test.)
- **Parent switch keeps table** — start with a loaded subtype (rows present); click a different
  parent → `getRows` **not** called again, `<qd-word-types-table>` **still present**, no
  `word-types-select-subtype` prompt. *(This replaces the Phase-1 test that asserted the table
  disappears — update it.)*
- **Subtype click loads results** — pick a subtype → `getRows` called with the new `childCode`,
  table renders.
- **Loading skeleton** — table shows the skeleton body when `loading()` and no rows; keeps rows with
  `aria-busy` when loading over existing rows.
- **Selected row visual state** — selecting a row applies the selected class/marker and it survives a
  detail refresh.
- **Analysis removed** — details panel renders only `ayahs`/`surahs` tabs; no analysis tab / action
  in the DOM.
- **Deep-link fallback** — `parseWordTypesQueryParams('view=analysis')` yields `view === 'ayahs'`;
  a row with `?view=analysis` restores the ayahs view (facade spec).
- **Filter DOM hooks** — if Phase B renames/wraps anything, update selectors; assert selected +
  expanded classes.

Also run the **full build**: `npm run build` (must be clean — proves no dangling analysis imports).

### E2 — manual QA checklist
- **Each parent:** Nouns, Verbs, Particles & Tools, Disjoint Letters — cards look intentional;
  selected/hover/focus/expanded states clear; counts are badges.
- **Subtypes:** grouped chip set; selected chip obvious; particle subtypes present; no zero-count
  chips; `inl` loads directly (no panel).
- **Parent switch with existing results:** table stays; new subtype panel opens; rows refresh only
  after picking a subtype; no blink to empty/loading.
- **Loading:** first load shows skeleton; subtype/sort/page changes keep prior rows dimmed/busy.
- **Details panel:** select rows → ayahs + surahs + summary only; no analysis tab/action; paging and
  tab switch work; stale `?view=analysis` link opens ayahs.
- **Table rows:** clearly clickable; selected row unmistakable; root/stem/lemma/counts organized.
- **Widths:** desktop (two-column, independent scroll) and mobile/tablet (single column, card reflow,
  modal details) — no header/row desync (Phase-2 fix intact).
- **Themes:** light and dark both calm/readable (RTL correct, no color-only meaning).

### Acceptance criteria — Phase E
- Focused specs + `npm run build` green.
- Manual checklist passes on desktop + mobile, light + dark.
- No regression to Phase-1 semantics or Phase-2 scroll/alignment.

---

## 8. Risks / notes / open questions

- **Stale rows after parent switch (by design).** Visible rows may belong to the previous subtype
  until a new one is picked — this is the requested behavior. Row identity is self-contained so a
  lingering-row click still resolves. *(Q2: if QA finds it confusing, we can add a subtle "results
  for <previous subtype>" caption or a one-line hint in the open panel — decide during QA, not now.)*
- **`selectPrompt` semantics widen.** It now means "no rows have ever loaded," not "a parent is
  selected." The page keys the prompt off `!listState().rows`. Keep that condition in sync if the
  status enum is refactored.
- **Analysis deep-link tolerance.** Do not throw on `view=analysis`; rely on `normalizeView`'s
  default fallback. Keep `location`/`column` params parsed-but-ignored so old links don't 404 the
  route.
- **Shared component boundary.** `AyahMatchesListComponent` (and surah/missing-surah lists) are
  shared; only the Word Types page's *usage* changes. *(Q1: if `ayah-matches-list` turns out to be
  used **only** by Word Types, its `showAnalysisAction`/`analysisActionLabel`/`analysisRequested`
  API could also be pruned — verify usages first; if shared, leave it untouched.)*
- **No global token changes assumed.** If Phase B/C genuinely need a missing role token (e.g. a chip
  surface), justify it inline and keep it additive; do not restyle other explorers.

### Blockers
- **None.** All five problems are solvable in the frontend. No backend, migration, importer, or
  contract change is required. If implementation uncovers a forced backend/contract change, stop and
  escalate rather than expanding scope.
