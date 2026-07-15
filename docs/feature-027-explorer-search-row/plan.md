# Feature 027 — Explorer Search Row (unified)

**Type:** Frontend UI redesign (no backend, no contract change)
**Status:** Plan (not implemented)
**Scope:** Four "words" explorers — stems (الأصول الصرفية), unique-words (الكلمات), lemmas (الصيغ المعجمية), roots (الجذور)
**Branch/commit:** not created by this plan; implementation lands on a feature branch off `main` per repo workflow.

This plan continues directly from the read-only inspection of the four explorers. It assumes the
LOCKED DECISIONS in the task brief are authoritative and does not re-litigate them.

---

## 0. Reality confirmation (paths cited)

Confirmed current state (unchanged since the inspection):

- Pages: [stems](../../Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.html) ·
  [unique-words](../../Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.html) ·
  [lemmas](../../Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html) ·
  [roots](../../Frontend/quran-dashboard-ui/src/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.html).
  Roots/lemmas/stems inline an identical `<input .qd-explorer__search>` + `<select .qd-explorer__sort>` block inside
  `.qd-explorer-controls`; unique-words uses `qd-unique-words-search-bar` (search **and** sort inline) + `qd-unique-words-tabs`.
- Shared children: [explorer-association-filter](../../Frontend/quran-dashboard-ui/src/app/features/words/components/explorer-association-filter/explorer-association-filter.component.html)
  (a `<details>` disclosure holding one `<input type="search" data-testid="association-filter-search">` + toggle-`<button>` options,
  `aria-pressed`, **deliberately not a listbox**), [explorer-count-range-filter](../../Frontend/quran-dashboard-ui/src/app/features/words/components/explorer-count-range-filter/explorer-count-range-filter.component.html),
  [explorer-result-count](../../Frontend/quran-dashboard-ui/src/app/features/words/components/explorer-result-count/explorer-result-count.component.html),
  [unique-words-search-bar](../../Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-search-bar/unique-words-search-bar.component.html),
  [unique-words-tabs](../../Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-tabs/unique-words-tabs.component.html) (router tablist: بالتشكيل/بدون تشكيل).
- Layout SCSS: [_words-explorer-layout.scss](../../Frontend/quran-dashboard-ui/src/styles/_words-explorer-layout.scss)
  (`.uw-toolbar-recess`, `.uw-toolbar-recess__stat`, `.qd-explorer-controls`, `.qd-explorer__search`, `.qd-explorer__sort`).
- Globals: [_forms.scss](../../Frontend/quran-dashboard-ui/src/styles/_forms.scss) (`.qd-input{width:100%}`, `.qd-select{width:100%}` — the wrap bug),
  [_components.scss](../../Frontend/quran-dashboard-ui/src/styles/_components.scss).
- Search wiring: each page has `searchDraft` signal (immediate echo) + a `searchInput` Subject `.pipe(debounceTime(300))` → `updateQueryParams({search, page:null})`;
  association search debounced at page level (`rootSearchInput`/`lemmaSearchInput` `.pipe(debounceTime(300), switchMap(...))`); type filter is `clientFilter` (local, no emit).
- **Popover precedent already in-repo:** [surah-jump-picker](../../Frontend/quran-dashboard-ui/src/app/features/mushaf/components/surah-jump-picker/surah-jump-picker.component.ts)
  and [source-selector](../../Frontend/quran-dashboard-ui/src/app/features/mushaf/components/source-selector/source-selector.component.ts) use
  `@HostListener('document:keydown')` (Escape → close), `@HostListener('document:click')` (outside-click → close), `window:scroll/resize` reposition,
  a `getBoundingClientRect()`-derived viewport-aware panel `max-height`, and focus-restore. `@angular/cdk` ^20.2.14 is available but the house pattern is the hand-rolled HostListener; we follow the house pattern (no CDK Overlay).
- **What we reuse vs. what we do NOT (precise, corrects any "mirror surah-jump-picker" shorthand):** surah-jump-picker is a full **listbox** (`role="listbox"`, `aria-activedescendant`, Arrow/Home/End/Enter — see `surah-jump-picker.component.ts:94–141, 84–92`) opened by a **`<button>` trigger**, with the search `<input>` living **inside** the panel. We reuse **only** its panel *mechanics* — open/close, Escape, outside-click, `window:scroll/resize` reposition, and the `getBoundingClientRect()` max-height computation (`surah-jump-picker.component.ts:219–236`). We deliberately do **NOT** copy its listbox role, its arrow-key/active-descendant model, or its separate button-trigger structure. Here the always-visible association **search field itself is the anchor** (no separate button), and options stay plain `aria-pressed` toggle-buttons. This divergence is stated explicitly so implementation cannot accidentally reintroduce a listbox role or an arrow-key model.

**No stop-condition conflict found.** Query keys live in `*-url-sync.ts` and page `.ts`, never in the search markup, so a shared-layout extraction and a `<details>`→popover swap can preserve every query key and testid. The toggle-button option model moves into the popover unchanged.

---

## 1. Objective & exact final behavior

One clean, RTL, consistent **search row** per explorer: the text search inputs sit side-by-side, each dedicated to its function. The root/lemma/type pickers become **inline fields** whose option lists open in a **dropdown/popover anchored under the field** (no more full-width `<details>` disclosure). Sort + count-range-filter form a **secondary controls row** below; the result-count stat stays visible.

Per explorer, the search row contains (RTL order, leading = right):

- **stems:** main stem search (`اكتب أصلًا صرفيًا…`) + root field (الجذر الأساسي) + lemma field (الصيغة المعجمية الأساسية) — **3 fields**.
- **unique-words:** بالتشكيل/بدون تشكيل tabs **leading/above the row**, then main word search (`ابحث في الكلمات…`) + type field (النوع الأساسي, `clientFilter`) + root field (الجذر الأساسي, server) — **3 fields + tabs**.
- **lemmas:** main lemma search (`اكتب صيغة معجمية…`) + root field (الجذر) — **2 fields**.
- **roots:** main root search only — **1 field**, restyled to match (no association field).

Secondary row (all four): sort `<select>` + `qd-explorer-count-range-filter`. Result-count stat visible near the rows. All existing behavior (debounce, URL-state, page-reset, selection preservation, range buckets) is byte-for-byte preserved; only layout/markup/skin and the disclosure→popover interaction change.

---

## 2. Scope & non-goals

**In scope:** the four page templates + their SCSS, a new shared search-row component, refactor of `explorer-association-filter` (`<details>`→inline field + popover), repositioning sort + range-filter into a secondary row, scoped sort-width fix, tests, and doc updates.

**Non-goals (hard):** no backend/API/DTO/SQL; no url-sync/query-key contract change (all `*-url-sync` specs pass **unchanged**); no change to search semantics, 300ms debounce, imlaei-simple identity, or data; no new npm packages; no global token/cache change; no route/label churn (`unique-words` stays `unique-words`); `count-range-filter` and `result-count` are repositioned/restyled only, not re-contracted; no changes to global `_forms.scss` defaults for non-explorer pages.

---

## 3. Affected files / layers

**New:**
- `Frontend/quran-dashboard-ui/src/app/features/words/components/explorer-search-row/` — `explorer-search-row.component.{ts,html,scss,spec.ts}` (presentational shell).

**Modified — components:**
- `explorer-association-filter.component.{ts,html,scss,spec.ts}` — disclosure → inline field + popover.
- `unique-words-search-bar.component.*` — retired/absorbed (see §4); its two testids relocate.

**Modified — page templates + SCSS (4):**
- `stems-explorer-page.component.html`, `lemmas-explorer-page.component.html`, `roots-explorer-page.component.html`, `unique-words-page.component.html` (+ each `.scss` if page-local tweaks needed; currently `:host`-only).

**Modified — shared styles:**
- `src/styles/_words-explorer-layout.scss` — new `.qd-explorer-search-row` + `.qd-explorer-controls-secondary` classes; scoped sort/input width override.

**Modified — labels (if any new sr-only/aria strings):**
- `models/words-shared.labels.ts` + per-explorer `*.labels.ts` (reuse existing label/placeholder consts wherever possible; add only sr-only/aria strings for the popover, via the TDZ-getter pattern).

**Modified — tests:** four page specs, `unique-words-page` spec, `unique-words-search-bar` spec, `explorer-association-filter` spec. The four `*-url-sync.spec.ts` remain **untouched and must pass as-is**.

**Modified — docs:** `Frontend/quran-dashboard-ui/src/app/features/words/README.md` (controls-layout / popover behavior note).

**Explicitly not touched:** any `*-url-sync.ts`, `*.api.ts`, facades, caches, `_forms.scss` global rules, backend.

---

## 4. Shared-component decision & blast radius

**Decision: extract a shared presentational `qd-explorer-search-row` using content projection; retire `qd-unique-words-search-bar`.** Justification: the four explorers differ in field count and in which association fields they carry, but share the row *shell* (RTL flex layout, `role="search"`, main input, scoped width fix). A **content-projection** shell keeps each page's association-filter wiring, testids, query keys, and debounce **exactly where they are today** (just relocated into the projected slot), giving DRY layout with the smallest blast radius. A fully data-driven (config-array) row was rejected: it would re-route association bindings and risk testid/debounce drift.

Shape:

```
<qd-explorer-search-row [searchValue] [searchLabel] [searchPlaceholder]
                        [searchTestid] [disabled] (searchChange)>
  <ng-content>                      <!-- association fields, per page -->
</qd-explorer-search-row>
```

- The row renders the sr-only-labelled main `<input type="search">` (host `role="search"`) and lays out the projected association fields beside it.
- Sort **leaves** the search row (moves to the secondary controls row), so `unique-words-search-bar` (which bundled search+sort) is superseded: its **search input** becomes the row's main input (keep `data-testid="unique-words-search-input"`), its **sort select** moves to the secondary row (keep `data-testid="unique-words-sort-select"`).

**Blast radius (explicit):**
- `unique-words-page.component.html` — replace `qd-unique-words-search-bar` with `qd-explorer-search-row` + move sort into secondary row; keep both testids.
- `unique-words-search-bar.component.*` + its spec — **removed**; the spec's two presence assertions migrate to the unique-words page spec (already asserts them at [:116–119](../../Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.spec.ts)).
- roots/lemmas/stems templates — replace inline `.qd-explorer-controls` search block with `qd-explorer-search-row`; keep `*-search-input` / `*-sort-select` testids (sort select relocates to secondary row with the same testid).
- `explorer-association-filter` consumers — unchanged bindings/testids; only the component's internal markup/interaction changes.

If, during implementation, extraction is found to force any query-key or testid change that cannot be avoided → **stop and report** (per brief); do not proceed by mutating the contract.

---

## 5. Association field → popover model

Refactor `explorer-association-filter` from `<details>/<summary>` to an inline **field + popover**, preserving the option model.

**Markup/interaction:**
- Trigger = the inline field: a labelled `<input type="search" data-testid="association-filter-search">` (keep testid) acting as combobox-style filter, with the selection shown as a `qd-badge` + clear `×` button (keep `association-filter-value` / `association-filter-clear` testids and the composed clear `aria-label`). **The field itself is the popover anchor** — there is no separate `<button>` trigger (the deliberate divergence from surah-jump-picker; see §0).
- Popover = the option list panel anchored under the field, containing the **same toggle `<button>`s** (`aria-pressed`, `qd-is-selected`, `association-filter-option-*` testids) — **not a listbox** (no `role=listbox`, no `aria-activedescendant`, no arrow-key model; keep the deliberate plain-button model and its comment).
- **Open trigger = FOCUS (locked by product owner):** the panel opens when the field receives focus **and** when the user types. Opening on focus must **not** trap focus.
- **Close triggers:** `Escape` (close + restore focus to the field), outside-click (`document:click` outside the component root), **`focusout` of the whole component** (focus leaves the field *and* all options — i.e. `event.relatedTarget` is not contained in the component root), and **selecting an option**. `onClear` must also not leave a stale-open panel.
- **Focus & keyboard management (precise, non-disruptive):**
  - **Tab order:** field → its option `<button>`s in DOM order (Tab-reachable per the locked plain-button model) → tabbing past the last option leaves the component; that `focusout` closes the panel and focus continues naturally to the next control. No focus trap, no arrow-key navigation, no `aria-activedescendant`.
  - **Escape** closes and restores focus to the field, setting a transient **suppress-reopen guard** so the focus-restore does not immediately re-fire the focus-open (guard clears on the next `blur`/pointer-down on the field).
  - **Selecting an option** closes the panel, clears the query, and restores focus to the field **with the same suppress-reopen guard** (the restored focus must not reopen the panel).
  - **Outside-click** and **`focusout`-to-outside** close *without* moving focus (focus is already leaving).
  - **Single-open invariant:** because opening is focus-driven and only one element can hold focus at a time, focusing a *sibling* association field fires `focusout` on the currently-open one and closes it. Stems and unique-words each carry two association fields; this guarantees at most one association popover is open at a time — with no cross-component coordination bus.
- **ARIA:** field exposes `aria-expanded` (bound to open state), `aria-controls` (popover id), and `aria-haspopup` set to a **non-listbox** value (`"true"` — deliberately not `"listbox"`, to match the plain-button model). Popover keeps `aria-label={{label}}`. The `role=status` loading hint stays (not `aria-hidden`).
- **Mechanics (reuse only, per §0):** reuse the **house HostListener pattern** from surah-jump-picker/source-selector — `document:keydown` (Esc), `document:click` (outside-click), `window:scroll/resize` (reposition), and the `getBoundingClientRect()` viewport-aware max-height — **without** its listbox role or arrow-key model. Not CDK Overlay.
- **Popover must not be clipped by the recess (correction):** the field lives inside `.uw-toolbar-recess` (a bordered, padded `--qd-surface-recessed` surface — `_words-explorer-layout.scss:14`) within a flex row. The popover panel MUST escape any ancestor `overflow`/stacking-context clipping — render **above** the recess border (correct `z-index` / floating stacking context), anchor directly **under the field**, mirror correctly in **RTL** (leading edge = right), and compute a **viewport-aware `max-height`** near the bottom of the screen. Reuse **only** surah-jump-picker's `getBoundingClientRect()` max-height computation (`window.innerHeight − rect.bottom − padding`, clamped — `surah-jump-picker.component.ts:219–236`) and its `window:scroll/resize` reposition mechanics (`:156–162`), anchored to the **field** rather than a button trigger.

**Preserved server/client split:** `onQueryInput` still emits `searchChange` only when `!clientFilter` (server), and filters `visibleOptions` locally when `clientFilter` (type). Page-level 300ms debounce + `switchMap` → options is untouched.

**State swap:** the `expanded` signal (currently driven by `<details>` `toggle`) becomes a `panelOpen` signal driven by focus/typing/Esc/outside-click/`focusout`/select. `query` signal unchanged.

**Verified current close-on-select behavior (read from `explorer-association-filter.component.ts:88–92`, not assumed):** today `onSelect(option)` emits `selectionChange`, sets `expanded.set(false)`, and clears `query.set('')` — it already closes and clears; `onClear` (`:94–97`) emits `null` and clears `query` but does **not** touch open state (harmless under `<details>`, must be handled under the popover). **Intended behavior after the swap (explicit):** `onSelect` emits `selectionChange`, sets `panelOpen.set(false)`, clears `query`, and restores focus to the field with the suppress-reopen guard set (so the restored focus does not reopen the panel); `onClear` emits `null`, clears `query`, and closes the panel if open.

---

## 6. Ordered phases + dependencies

Dependency order: shared shell/SCSS → association popover → per-explorer wiring → sort-width fix → tests/docs. Each phase is buildable and testable on its own.

**Phase 1 — Shared search-row shell + SCSS scaffolding.**
- Add `qd-explorer-search-row` (presentational, `role="search"`, main input + `<ng-content>`, OnPush).
- Add `.qd-explorer-search-row` + `.qd-explorer-controls-secondary` to `_words-explorer-layout.scss`; RTL flex, wrap at narrow widths, soft-elevation surface, spacing tokens.
- Tests: new `explorer-search-row.component.spec.ts` — renders main input with passed testid/label/placeholder, emits `searchChange`, projects content, host has `role="search"`.
- Depends on: nothing.

**Phase 2 — Association-filter popover refactor.**
- Convert `<details>`→field+popover; add `panelOpen`, `aria-expanded`/`aria-controls`, HostListener Esc/outside-click/focus-restore; keep option toggle-buttons + all testids + server/client split.
- Tests: update `explorer-association-filter.component.spec.ts` — keep `searchChange`(server)/no-emit(`clientFilter`)/clear-`aria-label`/loading-`role=status` assertions; add: focus opens the panel, typing opens the panel, `focusout` to outside closes, `Escape` closes + restores focus to the field (no immediate reopen), outside-click closes, selecting an option closes + clears + restores focus, focusing a sibling field closes the first (single-open invariant), `aria-expanded`/`aria-controls`/`aria-haspopup` wired, options remain Tab-reachable `aria-pressed` toggle-buttons (no listbox/arrow-key model).
- Depends on: nothing (parallelizable with Phase 1).

**Phase 3 — Per-explorer wiring into the row.**
- roots/lemmas/stems: replace the inline `.qd-explorer-controls` block with `qd-explorer-search-row`; drop association `<details>` blocks into the projection slot; move sort `<select>` into `.qd-explorer-controls-secondary` alongside `qd-explorer-count-range-filter`; keep `qd-explorer-result-count` visible.
- unique-words: replace `qd-unique-words-search-bar` with `qd-explorer-search-row`; keep `qd-unique-words-tabs` leading/above; move sort to secondary row; keep both unique-words testids; remove `unique-words-search-bar` component + spec.
- Tests: update the four page specs for new DOM location of the same testids (presence + behavior via method calls / testids; no flex/pixel assertions). Migrate the two `unique-words-search-bar` presence assertions into the unique-words page spec.
- Depends on: Phases 1 & 2.

**Phase 4 — Sort-width fix (scoped).**
- In `_words-explorer-layout.scss`, scope an override so sort/inputs size within the row: e.g. `.qd-explorer-search-row .qd-input`, `.qd-explorer-controls-secondary .qd-select { inline-size:auto; }` plus sensible `flex`/`min-inline-size`. Do **not** edit `_forms.scss` globals.
- Tests: covered by visual/manual check + the presence specs; no brittle width assertion.
- Depends on: Phase 3 (needs final row markup).

**Phase 5 — Docs + full-suite verification.**
- Update `words/README.md` controls-layout / popover note.
- Run the four page specs, association-filter spec, unique-words page spec, and the four `*-url-sync` specs (must pass unchanged) under the repo vitest worker cap.
- Depends on: 1–4.

---

## 7. State / URL / cache / UI / RTL / a11y behavior

- **State:** `searchDraft` (immediate echo) and page `searchInput`/`*SearchInput` Subjects unchanged; association `query`/`panelOpen` are component-local UI state, not URL state.
- **URL:** every query key (`search`, `sort`, `root`, `lemma`, `type`, `page`, range buckets) and its parse/build shape unchanged; `*-url-sync.ts` untouched.
- **Cache:** untouched (no facade/cache edits).
- **UI/RTL:** row is RTL-first (logical props, leading = right); tabs lead unique-words; secondary row holds sort + range-filter; result-count visible. Navy+gold+parchment, gold ≤10% (One Voice: gold only for selected option `aria-pressed`, active badge, focus ring), soft elevation on the popover panel (`--qd-shadow-lg` for the floating layer), calm motion (≤140ms fade/none; respect reduced-motion).
- **Responsive / narrow-width (explicit, using repo tokens in `_breakpoints.scss`):**
  - **≥ `$qd-bp-desktop-min` (≥1024px):** the search row is one horizontal, side-by-side flex row (main input + association fields); sort + range-filter form the secondary row beneath.
  - **`768px…1023px` (tablet, ≤ `$qd-bp-tablet-max`):** the search row wraps **2-up** (`flex-wrap`) — main input leading full-width, association fields wrapping beneath in the locked leading→trailing order; the secondary controls row also wraps.
  - **≤ `$qd-bp-phone-max` (≤767px, phone):** search row and secondary controls become **full-width stacked** (single column, one control per line) in defined order: main search → association fields (locked order) → secondary controls (sort → range-filter) → result-count.
  - **unique-words tabs (بالتشكيل/بدون تشكيل):** stay **leading/above** the search row at **all** widths; they never fold into the field row.
- **A11y / keyboard / focus:** main input keeps sr-only label + `type=search`; host `role="search"`. Association field: `aria-expanded`/`aria-controls`/`aria-haspopup="true"` (non-listbox); **focus opens** the panel (typing also opens), Tab-reachable options in DOM order, tabbing past the last option (a `focusout` to outside) closes and lets focus continue; `Escape` closes + restores focus to the field with a suppress-reopen guard; outside-click and `focusout`-to-outside close; selecting an option closes + clears + restores focus (guarded); focusing a sibling association field closes the first (single-open invariant). No focus trap. Loading hint `role=status` (not `aria-hidden`); clear button composed `aria-label`. **No listbox/arrow-key/`aria-activedescendant` model introduced** (preserve the plain-button model).

---

## 8. Loading / empty / error / retry / partial-failure for the dropdowns

- **Loading:** popover shows the existing `role=status` hint (`جارٍ التحميل…`, `aria-hidden` false) while `loading()` is true; field stays interactive; skeleton/non-interactive rows stay non-interactive.
- **Empty:** popover shows a neutral "no matches" empty state (reuse existing option-list empty semantics; add a short sr-friendly line if none exists) — no crash, field stays open.
- **Error / partial-failure:** association option loads are page-level `switchMap` streams; a failed root/lemma search must not blank the whole row. Behavior: keep the last good options or show the empty/hint state; the main list and other fields are unaffected (each association field is independent). No new retry contract is added; if the current code swallows option-load errors, preserve that behavior (out of scope to add new error UI), but document the gap in the README note if present.
- **Disabled:** while the main list is `loading`, association fields honor `disabled()` as today.

---

## 9. Tests to write / update

- **New:** `explorer-search-row.component.spec.ts` — input rendering (testid/label/placeholder), `searchChange` emit, content projection, `role=search`.
- **Update `explorer-association-filter.component.spec.ts`:** keep `searchChange`(server) / no-emit(`clientFilter`) / clear `aria-label` / loading `role=status` (not `aria-hidden`); add: opens on **focus**, opens on typing, closes on `focusout` to outside, closes on `Escape` + restores focus to field (no immediate reopen), closes on outside-click, closes + clears + restores focus on selection, focusing a sibling field closes the first (single-open invariant), `aria-expanded`/`aria-controls`/`aria-haspopup` wired, options remain Tab-reachable toggle-buttons with `aria-pressed` (no listbox/arrow-key model).
- **Update four page specs:** same testids at new DOM locations; behavior via method calls (`onSortChange`, `onSearchInput`, range buckets) and query-key assertions unchanged; **no** flex/pixel/`side-by-side` assertions (assert presence + relative structure only).
- **Update unique-words page spec:** absorb the two `unique-words-search-bar` presence assertions (`unique-words-search-input`, `unique-words-sort-select`); confirm tabs still lead.
- **Remove:** `unique-words-search-bar.component.spec.ts` with the component.
- **Untouched (must pass):** `roots/lemmas/stems/unique-words-url-sync.spec.ts`.
- Run under the repo `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap (words README + frontend README testing rule).

---

## 10. README / doc updates

- `Frontend/quran-dashboard-ui/src/app/features/words/README.md`: note that the four explorers share `qd-explorer-search-row`; association pickers are inline field + popover (**focus/typing opens**; Esc/outside-click/`focusout`-to-outside/selection closes; focus-restore on Esc/select; `aria-expanded`/`aria-controls`/`aria-haspopup="true"`, plain-button `aria-pressed` options, **not a listbox**; popover not clipped by the toolbar recess); the search row is side-by-side at ≥ tablet and stacks below; sort + range-filter are a secondary row; URL-state contract unchanged. Update in the **same change** (README invariant rule).
- No `docs/contracts/` change (no contract shift). This plan doc (`docs/feature-027-explorer-search-row/plan.md`) is the planning artifact.

---

## 11. Data validation & performance

- **Change detection:** all components OnPush; popover open is a signal flip (no new subscriptions per keystroke beyond existing Subjects).
- **No extra network calls:** association option loads keep the page-level 300ms debounce + `switchMap` (cancels in-flight); opening the popover triggers **no** fetch by itself (it renders already-loaded `options()`), so dropdown-open cost is DOM-only.
- **Dropdown open cost:** option lists are already capped/scrolled (`max-height:14rem; overflow-y:auto`); keep that. Avoid layout thrash by positioning via CSS anchored to the field (or the existing HostListener reposition), not per-frame JS.
- **RTL correctness:** verify logical properties render mirrored; test with real Arabic + tashkeel.
- **Popover not clipped:** verify the association popover renders above the `.uw-toolbar-recess` border and is not clipped by any ancestor `overflow`/stacking context; anchors under the field; mirrors in RTL; and is height-limited via the reused `getBoundingClientRect()` max-height when the field sits near the viewport bottom (reposition on `window:scroll/resize`).
- **Manual RTL narrow-width check:** at phone (≤767px) and tablet (768–1023px) widths, in RTL, confirm the search row stacks/wraps in the defined order, the popover still anchors under its field and mirrors correctly, and the unique-words tabs stay leading/above.

---

## 12. Risks, rollback, stop conditions

- **Risk — testid drift** when extracting the shared row / relocating sort. Mitigation: keep every existing testid; migrate assertions, don't rename. **Stop** if a query key would have to change.
- **Risk — a11y regression** swapping `<details>` for a custom popover. Mitigation: reuse **only** the surah-jump-picker/source-selector panel *mechanics* (Esc/outside-click/`window:scroll/resize` reposition/max-height/focus-restore) and add explicit tests; **do not** adopt its listbox role or arrow-key/`aria-activedescendant` model — keep plain-button `aria-pressed` options and the focus-open model.
- **Risk — partial option-load failure** blanking the row. Mitigation: independent fields; preserve current error-swallowing behavior; no new contract.
- **Risk — sort-width fix leaking globally.** Mitigation: scope strictly under `.qd-explorer-search-row` / `.qd-explorer-controls-secondary`; never touch `_forms.scss` globals.
- **Risk — popover clipped by the toolbar recess.** The field sits inside `.uw-toolbar-recess` (bordered/padded surface) in a flex row; an ancestor `overflow`/stacking context or the recess border can clip a naively-positioned panel. Mitigation: float the panel above the recess (correct `z-index`/stacking), anchor under the field, verify RTL mirroring, and height-limit via the reused `getBoundingClientRect()` max-height + `window:scroll/resize` reposition.
- **Rollback:** the change is presentational + component-internal; revert the feature commit to restore the `<details>` disclosures and stacked controls. No data/URL migration, so rollback is clean.
- **Stop conditions (report, don't plan around):** (a) shared extraction forces a url-sync/query-key change; (b) the toggle-button option model can't live in a popover without a contract/role change; (c) preserving a testid is impossible without a query-key change.

---

## 13. Acceptance criteria

1. Each explorer shows **one horizontal, side-by-side RTL search row at ≥ tablet (≥1024px)** with its locked field set (stems 3 / unique-words 3 (+ leading tabs) / lemmas 2 / roots 1), degrading to the **defined graceful stack below** (2-up wrap on tablet 768–1023px, full-width stacked column on phone ≤767px); unique-words tabs stay leading/above at all widths.
2. Association pickers are inline fields; options open in a popover under the field; **open on focus and on typing** (focus does not trap); **close on Esc (focus returns to field), `focusout` to outside, outside-click, and selection**; focusing a sibling field closes the first (single-open); `aria-expanded`/`aria-controls`/`aria-haspopup="true"` present; options are Tab-reachable `aria-pressed` toggle-buttons (no listbox, no arrow-key/`aria-activedescendant` model).
3. **Association popover is not clipped by the toolbar recess** (renders above the `.uw-toolbar-recess` border, escapes ancestor `overflow`/stacking clipping); correct RTL anchoring under the field; height-limited near the viewport bottom via the reused `getBoundingClientRect()` max-height (reposition on `window:scroll/resize`).
4. Sort + count-range-filter form a secondary row; result-count visible; all behavior identical.
5. Server/client search split intact (300ms debounce + switchMap for server; local filter, no emit for `clientFilter` type).
6. Sort/inputs size within the row (no full-width wrap); `_forms.scss` globals unchanged.
7. All query keys and `*-url-sync` specs unchanged and passing; all existing testids present.
8. RTL, sr-only labels, `role=search`, loading `role=status` (not `aria-hidden`), non-interactive skeletons preserved.
9. Design language honored (navy+gold+parchment, gold ≤10%, soft elevation, calm motion, primitives composed).
10. Full affected test suite green under the vitest worker cap; `words/README.md` updated in the same change.

---

## 14. Expected commit boundary

**One coherent feature commit** (`feat(words): unified explorer search row + inline association popovers`) covering the shared component, association-filter refactor, four page rewires, scoped sort-width fix, tests, and the README update — they form one atomic behavior and share a rollback point. Split only if the association-filter popover refactor lands meaningfully before the page rewires (Phase 2 as a standalone commit) to keep review reviewable; otherwise keep it single.
