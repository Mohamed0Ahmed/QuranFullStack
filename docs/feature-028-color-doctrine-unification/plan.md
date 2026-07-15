# Color-Doctrine & DRY Unification — Implementation Plan

> **For agentic workers:** this is a tightly-locked, plan-only document. It is **not**
> Spec Kit. Steps use checkbox (`- [ ]`) syntax for tracking. Do not begin implementation
> until the **Precondition** below is satisfied.

**Goal:** Unify color *usage* (not identity values) and collapse duplicated component
styling into shared components/classes across the Quran Dashboard frontend, so every
screen reads as one system in both light and dark — with **zero** change to information
architecture, behavior, API/DTOs, DB, or Quran text/rendering.

**Architecture:** Ban solid-gold fills at the token/contract level (selected state =
`accent-tint` bg + `--qd-accent-text` label + optional accent border/2px indicator);
add a small, named token set; hybrid unification (class-family collapse for the 6 table
and 10 detail-list families; new Angular shared components for tabs/chip/state/loading);
codify the rules as `UI_STYLE_SYSTEM.md` §16/§17 doctrine so these patterns are never
hand-written again.

**Tech Stack:** Angular 20 (standalone, Signals, OnPush), SCSS partials + CSS custom
properties (OKLCH `--qd-*` tokens), Tailwind v3 (layout only), Vitest (`ng test`), CDK
virtual scroll. Identity: **navy + gold + parchment**, light + dark.

---

## 0. Precondition (sequencing — hard gate)

- **Do not start P1 until feature `026-words-explorers-enhancements` is merged to `main`.**
  The `features/words` area is mid-feature and the branch is dirty (`_words-explorer-layout.scss`,
  explorer search row, inline association popovers are in flight). This plan rewrites the
  same table/list/chip surfaces and **will collide** with 026/027 work.
- After 026 merges, rebase this work onto `main` and re-verify the file inventory in §3
  (paths/selectors may have shifted). If a selector referenced here no longer exists,
  reconcile before editing — treat a moved selector as a merge task, not a silent skip.
- Each phase below is its own branch off `main` and its own PR/commit boundary (§13). Do
  not bundle phases; each must be independently green and independently revertible.

---

## 1. Objective and final behavior

Visual-only refresh. After all phases:

- **No solid gold at rest, no gold fill behind text, anywhere.** Selected/active state is
  uniform app-wide: `--qd-selected-bg` (accent-tint) background + `--qd-accent-text` label
  + a 1px `--qd-accent` (or `--qd-border-accent`) edge and/or a 2px accent indicator. The
  dark gold-on-gold collapse is impossible because no component borrows `--qd-primary` as
  ink on an accent surface.
- **One implementation each** of table, detail-list, tab, chip, state (empty/loading/error),
  and the loading/skeleton system. Per-component SCSS keeps only its
  `grid-template-columns`/column extras.
- **Both themes pass WCAG 2.1 AA** for every new/changed color pairing (accent-text on
  tint, status tints, dark surfaces).
- **Density applied app-wide**: explorer table row `3rem → 2.5rem`, cell `8×12px → 6×10px`,
  header `2.75rem` (already the default; confirmed/locked).
- **Motion is calm**: no `scale()` press/pulse; color/border transitions only; every timing
  is `var(--qd-t-fast)`/`var(--qd-t-base)` — no literal `0.15s`/`150ms`.
- **Surface ladder direction is consistent** between themes; `--qd-surface-elevated` is
  retired; the modal sits on `--qd-surface` (the near-white card / lightest content
  surface) and relies on `--qd-shadow-lg` + the dimmed backdrop for elevation.
- Doctrine is written: `UI_STYLE_SYSTEM.md` §16 (color doctrine) + §17 (component
  contracts); stale §15/§2 "not implemented yet" notes fixed; DESIGN.md One Voice Rule
  gains the allowed-gold list.

**No user-visible behavior changes.** Selection, keyboard nav, virtual scroll, URL-state,
pagination, filters, popovers, drilldown, tabs, and Quran/Mushaf rendering behave exactly
as before.

---

## 2. Scope and explicit non-goals

**In scope:** global tokens (`_tokens.scss`, `_themes.scss`); shared component classes
(`_components.scss`, `_forms.scss`, `_explorer-tables.scss`, `_explorer-detail-lists.scss`);
new shared Angular components under `src/app/shared/ui/`; migrating the words, mushaf,
dashboard, and core/layout call-sites to the shared classes/components; the two
architecture docs and the nearest styling READMEs.

**Non-goals (do not do any of these):**

- **No new features, no IA change.** No new routes, tabs, panels, filters, or columns.
- **No API / DTO / DB / Quran-text change.** No `data-access/*`, `models/*` DTO, facade,
  URL-sync, or backend change. `*-url-sync.ts` param names stay byte-for-byte.
- **No re-hue beyond the locked tokens.** The **warning hue stays frozen** (footer-dot-only)
  — no re-hue. Segment category colors (`--qd-segment-cat-*`) are a separate legible system;
  do **not** fold them into the doctrine.
- **No full Angular table/list components.** Tables and detail lists stay CSS class-families
  (collapsed to one base each). Only tabs/chip/state/loading become Angular components.
- **No CDK virtual-scroll or a11y behavior change.** Do not touch viewport wiring, `cdkVirtualFor`,
  `track`, focus/keyboard-nav utils, roles, or `aria-*` semantics except where a phase
  explicitly improves a11y (skeleton `aria-busy`, tab roles) without changing behavior.
- **Mushaf Quran rendering untouched.** No change to `--qd-font-quran*`, ayah glyphs,
  word-segment rendering, ayah markers, or any Quran text styling/motion. Mushaf chrome
  (surfaces, chips, loading, `qd-select`) is in scope; Quran content is not. Specifically, the
  ayah matched-word highlight (`highlighted-ayah` accent underline) and the mushaf
  word-selection indicator (`--qd-mushaf-word-selection-indicator`) remain **UNCHANGED** —
  they are §16 allowed-gold and are not touched by the gold-ban / chip / tab migration.
- **No mass token rename.** Token *names* are preserved; only values/ordering and a small
  additive token set change.

---

## 3. Affected files / areas (post-026 inventory to re-confirm)

### Global styles (`Frontend/quran-dashboard-ui/src/styles/`)
- `_tokens.scss` — add new tokens; density defaults; retire `--qd-surface-elevated` alias.
- `_themes.scss` — dark values for new tokens; fix ladder ordering; retire dark
  `--qd-surface-elevated` alias.
- `_components.scss` — `.qd-tabs*`/`.qd-chip*`/`.qd-state*` class layer (backing the Angular
  components), `.qd-is-selected` (already canonical), `.qd-badge*`, modal surface, skeleton
  system generalization, `--qd-surface-elevated` fallback removal, motion cleanup.
- `_forms.scss` — `qd-select` resting border (kill gold-at-rest), hover fill, `0.15s` → tokens.
- `_explorer-tables.scss` — collapse 5 families into `.qd-explorer-table`; density; kill
  gold-fill on `--action` hover and mobile-stat selected; replace ad-hoc `color-mix()`.
- `_explorer-detail-lists.scss` — collapse 10 families into `.qd-detail-list`.
- `_words-explorer-layout.scss` — audit for gold-at-rest / literals after 026 merge.

### Shared UI (`Frontend/quran-dashboard-ui/src/app/shared/ui/`)
- NEW `tabs/` — `qd-tabs` component (roving-tabindex, `role="tablist"`/`tab`).
- NEW `chip/` — `qd-chip` component (button/anchor chip; selected/disabled/count slots).
- NEW `state/` — `qd-state` component (empty | loading | error variants).
- `explorer-panel-skeleton/` → generalize to `qd-panel-skeleton` (shape-hint input); keep
  the existing selector as a thin alias if any external ref remains.
- NEW `skeleton/` — `qd-skeleton-rows` component (renders N skeleton rows into a caller-supplied
  grid template). `.qd-skeleton` stays a class, parameterized by CSS vars.

### Feature/layout call-sites
- **Tables (5):** `components/{roots-table,lemmas-table,stems-table,unique-words-table,word-types-table}/*.{html,scss}`.
- **Detail lists (10):** `components/{root-words-list,lemma-words-list,stem-words-list,root-lemmas-list,root-stems-list,lemma-stems-list,stem-lemmas-list,missing-surahs-list,surah-occurrences-list,type-distribution-list}/*.{html,scss}`.
- **Tabs call-sites:** `components/unique-words-tabs/`, `components/word-type-table-view-tabs/`,
  `mushaf/components/selected-ayah-section/` (tabs), `components/lemma-words-list/` &
  `components/stem-words-list/` (inline tabs).
- **Chips/badges:** `components/word-count-chip/`, `components/explorer-count-range-filter/`,
  `components/word-type-filter/`, `_explorer-tables.scss` mobile-stat, `_components.scss`
  `.qd-badge`, `components/ayah-matches-list/` badges.
- **State + loading→skeleton:** `dashboard/pages/dashboard-home/`,
  `mushaf/components/mushaf-page-area/`, `mushaf/components/selected-ayah-section/`,
  `mushaf/components/selected-word-section/`.
- **word-type-filter refactor:** `components/word-type-filter/*.{html,scss,ts}` onto
  `qd-card` + `qd-chip`.
- **dashboard card:** `dashboard/pages/dashboard-home/*.{html,scss}` (`.dashboard-card` → `qd-card--hover`).
- **core/layout:** `top-navbar/*.scss` (`0.15s`→token, `--qd-surface-elevated`),
  `mushaf/components/mushaf-marker/*.scss` (`--qd-surface-elevated`).
- **color-mix cleanup:** the 16 files currently using `color-mix()` (see §11) — replace
  role-derivations with the scaled tokens; keep only genuinely one-off mixes.

### Docs
- `.architecture/UI_STYLE_SYSTEM.md` (§16 + §17; fix §15/§2 stale notes).
- `../../DESIGN.md` (append allowed-gold list to One Voice Rule).
- `src/styles/README.md`, `src/app/shared/ui/` boundary (`shared/README.md`),
  `src/app/features/words/README.md`, `src/app/features/mushaf/README.md` where a documented
  invariant (selected-state contract, skeleton contract) changes.

---

## 4. New tokens (the ONLY additive tokens)

Add exactly these; **no others**. Values are OKLCH, tuned to the existing palette; final
L/C are gated on the P1 contrast check (§9). Define light in `:root` (`_tokens.scss`),
dark overrides in `[data-theme='dark']` (`_themes.scss`). Tokens whose *role* is
theme-invariant (e.g. ink-on-gold) are defined once in `:root` and deliberately **not**
overridden in dark.

```scss
/* _tokens.scss  (:root, light) */
--qd-accent-fg:      oklch(0.263 0.046 250);          /* ink on ANY solid-accent indicator; navy in BOTH themes (not overridden in dark) → never gold-on-gold */
--qd-border-accent:  oklch(0.718 0.118 84 / 0.32);    /* one canonical ~32% accent border */
--qd-surface-hover:  oklch(0.955 0.015 77.1);         /* single hover fill (== light section-bg tone) */
--qd-selected-bg:    var(--qd-accent-tint);           /* semantic alias — selected background */
--qd-danger-tint:    oklch(0.945 0.030 23);           /* danger wash; danger text must hit AA on it */
--qd-success-tint:   oklch(0.945 0.028 163);
--qd-warning-tint:   oklch(0.950 0.038 75);
```

```scss
/* _themes.scss  ([data-theme='dark']) — override only theme-varying ones */
--qd-border-accent:  oklch(0.772 0.098 82 / 0.32);
--qd-surface-hover:  oklch(0.265 0.039 262.7);        /* == dark section-bg tone */
--qd-danger-tint:    oklch(0.305 0.045 23);
--qd-success-tint:   oklch(0.305 0.035 163);
--qd-warning-tint:   oklch(0.320 0.045 75);
/* --qd-accent-fg and --qd-selected-bg intentionally NOT overridden:
   accent-fg stays navy ink; selected-bg follows accent-tint which is already dark-defined. */
```

Notes:
- `--qd-accent-fg` exists so components stop borrowing `--qd-primary` (which is **gold** in
  dark). It is ink for the rare solid-accent *indicator* (dot/2px bar), never a fill behind
  running text (fills are banned).
- `--qd-selected-bg` is a rename-free alias: existing `.qd-is-selected` may keep using
  `--qd-accent-tint`; new/migrated call-sites use `--qd-selected-bg` for intent clarity.
- Status tints are consumed by `qd-state` (error) and any future status chip; the frozen
  warning hue is unchanged (§2) — `--qd-warning-tint` is defined for completeness/parity but
  its only current consumer is the footer dot's own styling, which is **not** re-hued.

---

## 5. Doctrine to author (§16 + §17)

### `UI_STYLE_SYSTEM.md` §16 — Color doctrine

- **Role → color table** (the single source for "what color does a thing get"):

  | Role | Light | Dark | Notes |
  |------|-------|------|-------|
  | Selected/active background | `--qd-selected-bg` (accent-tint) | same token | never a solid fill |
  | Selected/active label | `--qd-accent-text` (navy) | `--qd-accent-text` (gold) | AA on tint |
  | Selected/active edge | 1px `--qd-accent` or `--qd-border-accent` | same | hairline, not fill |
  | Solid-accent indicator (dot / 2px bar) | `--qd-accent` fill + `--qd-accent-fg` ink | same | the ONLY solid gold behind pixels |
  | Hover fill | `--qd-surface-hover` | same | one token |
  | Resting control border | `--qd-border` | `--qd-border` | **no gold at rest** |
  | Primary action | `--qd-primary` + `--qd-primary-fg` | gold-primary per DESIGN dark | structural navy in light |
  | Danger/success/warning text | `--qd-danger`/`-success`/`-warning` on `*-tint` | same tokens | AA verified |

- **Grading / ladder:** document the surface ladder (parchment page → card → quiet →
  recessed) and the shadow ladder, with the corrected dark ordering (§P6). State that
  elevation direction must be consistent across themes.
- **The exact allowed-gold list** (mirrored into DESIGN.md): gold (`--qd-accent`/
  `--qd-accent-soft`) may appear ONLY as —
  1. `:focus-visible` ring/halo (`--qd-focus-ring` / `--qd-ring`);
  2. the 2px selection **indicator** bar or the selected **dot** (fill), with `--qd-accent-fg`
     ink if it carries a glyph;
  3. a **1px selected/active border** (`--qd-accent` or `--qd-border-accent`);
  4. **text** emphasis via `--qd-accent-text` (active nav, links, soft/selected labels,
     section eyebrows) — never raw `--qd-accent` as small text on light;
  5. footer gold (`--qd-footer-accent`) headings and link-hover;
  6. icon highlights and the mushaf word-selection indicator
     (`--qd-mushaf-word-selection-indicator`).
  Everything else — chip fills, badge fills, count fills, range badge, selected row fill,
  `qd-select` resting border — is **banned gold** and uses tint/`--qd-accent-text`/hairline.

### `UI_STYLE_SYSTEM.md` §17 — Component contracts ("never hand-write these again")

Document the API + do/don't for each: `qd-tabs`, `qd-chip`, `qd-state`,
`.qd-explorer-table`, `.qd-detail-list`, and the loading/skeleton system (`.qd-skeleton`
vars, `qd-skeleton-rows`, `qd-panel-skeleton`). Each contract states: purpose, inputs/slots,
required roles/aria, the selected/hover/disabled visuals (pointing at §16), and "compose,
do not re-style."

### Stale-note fixes
- §15 and §2 currently say the token/partial system is "not implemented yet / do not
  scaffold." It **is** implemented. Update those notes to describe the current reality and
  point to §16/§17 as the live contract. Do not delete the historical prototype-contract
  content; mark it "implemented — see §16/§17."

---

## 6. Component contracts (target APIs)

These are the interfaces later phases implement and later call-sites consume.

- **`qd-tabs`** — `role="tablist"`; projects tab items; inputs: `ariaLabel`,
  `orientation?='horizontal'`; each tab is `role="tab"` with `aria-selected`, roving
  tabindex, Arrow/Home/End keyboard nav (RTL-aware); selected visual per §16. Emits the
  existing selection event shape the call-site already uses (no behavior change — it adapts
  to current `@Output`/routerLink patterns). Backing class: `.qd-tabs`, `.qd-tabs__tab`,
  `.qd-tabs__tab.qd-is-selected`, `.qd-tabs__count`.
- **`qd-chip`** — button or anchor; inputs: `selected`, `disabled`, `as?='button'|'a'`,
  optional trailing `count`. Selected = `--qd-selected-bg` + `--qd-accent-text` +
  `--qd-border-accent`; hover = `--qd-surface-hover`; **no gold fill**. Backing class:
  `.qd-chip`, `.qd-chip--pill`, `.qd-chip.qd-is-selected`, `.qd-chip__count`.
- **`qd-state`** — variant `empty | loading | error`; inputs: `message`, `variant`; error
  uses `--qd-danger` on `--qd-danger-tint`, calm (not aggressive) per §11; loading is
  non-interactive `role="status"`; supersedes ad-hoc `.qd-empty-state/.qd-loading-state/
  .qd-error-state` usage (classes retained as the backing layer).
- **`.qd-explorer-table`** — one class family for all 5 tables; per-component SCSS keeps only
  `grid-template-columns` (+ any column-specific alignment). Density defaults live on the
  base. Virtual-scroll/body wiring unchanged.
- **`.qd-detail-list`** — one class family for all 10 lists; per-component SCSS keeps only
  its `grid-template-columns` and column extras (e.g. `stem-lemmas` 4-col, `type-distribution`
  2-col). Scroll/pagination wiring unchanged.
- **Loading/skeleton system** —
  - `.qd-skeleton` parameterized by `--qd-skeleton-w` / `--qd-skeleton-h` (defaults preserve
    today's `--text`/`--block`/`--w-*` shorthands, which stay as thin aliases).
  - `qd-skeleton-rows` — input `count`, `rowTemplate` (grid columns) → renders skeleton cells
    inside the **real** row grid so loading rows match loaded rows exactly.
  - `qd-panel-skeleton` — generalized `explorer-panel-skeleton` with a `shape` input
    (`lines | rows | panel`); default reproduces today's six-line panel skeleton.
  - All skeletons: non-interactive, `aria-busy="true"` + `role="status"` sr-only label,
    reduced-motion static (existing `_components.scss` reduced-motion rule extended).

---

## 7. Ordered phases

Each phase: **branch off `main` → implement → tests green → build → docs in the same
change → PR/commit (§13)**. Phases are additive; earlier tokens/components are prerequisites
for later migrations.

### P1 — Tokens + doctrine docs (foundation)
**Depends on:** Precondition (026 merged).
**Files:** `_tokens.scss`, `_themes.scss`, `UI_STYLE_SYSTEM.md`, `DESIGN.md`, `src/styles/README.md`.
**Do:**
- [ ] Add the §4 tokens (light + dark). No consumer changes yet.
- [ ] Author §16 (color doctrine + allowed-gold list) and §17 (component contracts) in
      `UI_STYLE_SYSTEM.md`; fix stale §15/§2 notes.
- [ ] Append the allowed-gold list to DESIGN.md One Voice Rule.
- [ ] Update `src/styles/README.md` token-group description to name the new tokens.
**Verify:** `npm run build` (SCSS compiles); grep shows tokens defined in both themes;
run the **AA contrast check** (§9) for every new pairing and record results in the PR.
**Tests:** none behavioral (pure tokens/docs). Add/adjust no specs.
**Commit:** `feat(styles): color-doctrine tokens + UI_STYLE_SYSTEM §16/§17`.

### P2 — Shared components: qd-tabs / qd-chip / qd-state + loading system
**Depends on:** P1.
**Files:** NEW `shared/ui/{tabs,chip,state,skeleton}/*`; generalize
`shared/ui/explorer-panel-skeleton/*`; `_components.scss` backing classes; `shared/README.md`.
**Do:**
- [ ] Build `qd-tabs`, `qd-chip`, `qd-state`, `qd-skeleton-rows` as standalone OnPush
      components with the §6 APIs; back them with `.qd-tabs*`/`.qd-chip*`/`.qd-state*`
      classes in `_components.scss`.
- [ ] Parameterize `.qd-skeleton` via CSS vars; keep `--text`/`--block`/`--w-*` aliases.
- [ ] Generalize `explorer-panel-skeleton` → `qd-panel-skeleton` (shape input); keep the old
      selector working (alias) until call-sites migrate in P5/P7.
- [ ] Update `shared/README.md` "What lives here" with the new primitives.
**Verify:** unit specs for each new component (below); `ng test` green; components render in
isolation with correct roles/aria.
**Tests (write these, behavior-first):**
- `qd-tabs`: renders `role="tablist"`, each tab `role="tab"`, selected has `aria-selected="true"`;
  Arrow/Home/End move roving focus (RTL-correct); reduced-motion path applies no transform.
- `qd-chip`: selected → `qd-is-selected` + accent-text, **no** `--qd-accent` fill in computed
  style; disabled is non-interactive; count slot renders.
- `qd-state`: each variant renders correct role; loading is `role="status"` non-interactive;
  error uses danger-on-tint.
- `qd-skeleton-rows`/`qd-panel-skeleton`: `aria-busy`, sr-only status label, N rows into the
  supplied grid; reduced-motion static.
**Commit:** `feat(ui): shared qd-tabs/qd-chip/qd-state + unified skeleton system`.

### P3 — Collapse `.qd-explorer-table` + migrate 5 tables
**Depends on:** P1.
**Files:** `_explorer-tables.scss`; 5 table components `*.{html,scss}`.
**Do:**
- [ ] Reduce the mega-selectors so the base `.qd-explorer-table*` carries all shared rules;
      each component uses `.qd-explorer-table` (+ modifier for its columns) and its SCSS keeps
      **only** `grid-template-columns`/column alignment.
- [ ] Apply density: row `--qd-explorer-table-row-height: 2.5rem` (was 3rem), cell padding
      `6px 10px` (was `space-2 space-3` ≈ 8×12), header `2.75rem` (confirm).
- [ ] Kill gold fills: `qd-explorer-mobile-stat--action:hover` `color:` → `--qd-accent-text`
      (not `--qd-primary`); selected mobile-stat edge → `--qd-border-accent`; replace the
      ad-hoc `color-mix()` borders with `--qd-border-accent` / `--qd-surface-hover`.
- [ ] Selected row stays `--qd-selected-bg`; hover → `--qd-surface-hover`.
**Verify:** virtual scroll + selection + keyboard-nav unchanged (drive P9 checks); visual
parity except intended density; no `--qd-primary` ink on accent surfaces (grep).
**Tests:** keep all 5 `*-table.component.spec.ts` green; add a regression asserting a
selected row exposes `qd-is-selected` and its computed background is the tint token (not a
solid accent); assert row height token = 2.5rem.
**Commit:** `refactor(words): collapse explorer tables into .qd-explorer-table + density`.

### P4 — Collapse `.qd-detail-list` + migrate 10 lists
**Depends on:** P1 (independent of P3).
**Files:** `_explorer-detail-lists.scss`; 10 list components `*.{html,scss}`.
**Do:**
- [ ] Introduce `.qd-detail-list*` base carrying the shared header/row/loading rules
      (currently the giant comma lists); each list uses `.qd-detail-list` + a column modifier;
      per-component SCSS keeps only `grid-template-columns` (2rem/2.5rem number col, 1fr, auto;
      4-col for `stem-lemmas`; 2-col for `type-distribution`).
- [ ] Route the header mixin `explorer-detail-list-header` through the base.
- [ ] **Align detail-list density with table density** so "density app-wide" is uniform
      across tables **and** detail lists (not tables only): compact rows and `6×10`-equivalent
      cell padding as shared-base defaults on `.qd-detail-list*` (match the P3 table row/cell
      rhythm). Keep it a base default; per-component SCSS stays columns-only.
- [ ] Replace ad-hoc `color-mix()` (e.g. `ayah-matches-list__card--alt`) only where it maps
      to a role token; leave genuine one-offs.
**Verify:** each list's scroll container, `scrollbar-gutter`, pagination, and empty/loading
rows behave identically; RTL intact.
**Tests:** keep all 10 list specs green (note: `missing-surahs-list` and
`surah-occurrences-list` have no spec today — do **not** add net-new suites unless a
regression needs one); add a focused regression that a loading row renders `qd-skeleton`
cells inside the row grid.
**Commit:** `refactor(words): collapse detail lists into .qd-detail-list`.

### P5 — Migrate chips/badges/tabs call-sites + ban gold fills + word-type-filter refactor
**Depends on:** P2, P3, P4.
**Files:** `unique-words-tabs`, `word-type-table-view-tabs`, `selected-ayah-section` (tabs),
`lemma-words-list`/`stem-words-list` (inline tabs), `word-count-chip`,
`explorer-count-range-filter`, `word-type-filter` (`.html/.scss/.ts`), `ayah-matches-list`
badges, `_components.scss` `.qd-badge`.
**Do:**
- [ ] Replace each bespoke tab strip with `qd-tabs` (or point its classes at `.qd-tabs*`);
      selected visual now comes from the contract.
- [ ] Migrate chips to `qd-chip`: **`word-count-chip` selected** and **`range-filter__chip`
      selected** lose `background: var(--qd-accent)` / `color: var(--qd-primary)` → tint +
      accent-text + hairline. **`range-filter__badge`** base loses solid gold → `--qd-selected-bg`
      + `--qd-accent-text` (already done in the `--cards` variant; apply to base + ensure the
      `--cards` selected chip is not solid gold either).
- [ ] `word-type-filter` selected count: drop `--qd-accent-soft` fill → tint + accent-text.
- [ ] Refactor `word-type-filter` (299-line SCSS) onto `qd-card` + `qd-chip`: the trigger
      becomes a `qd-card--hover`; children become `qd-chip` (pill); delete the duplicated
      hover/selected/focus SCSS. Fix trigger radius `lg → md` (radius doctrine, §P6).
- [ ] `.qd-badge`/`ayah-matches-list` badges: confirm tint, not solid accent.
**Verify:** popover open/close, child selection, secondary selects, counts, and the
association-filter behavior unchanged; grep confirms **zero** `var(--qd-accent)` /
`var(--qd-accent-soft)` used as `background` behind text and **zero** `color: var(--qd-primary)`
on accent surfaces. **Quran-highlight invariants:** the ayah matched-word highlight
(`highlighted-ayah` accent underline) and the mushaf word-selection indicator
(`--qd-mushaf-word-selection-indicator`) remain **UNCHANGED** — both are on the §16
allowed-gold list and must not be altered by the chip/tab/gold-ban migration.
**Tests:** keep `word-type-filter`, `word-count-chip`, `explorer-count-range-filter`,
`unique-words-tabs`(if present), `word-type-table-view-tabs`, tab-hosting specs green; add
regressions: selected chip/badge computed background ≠ solid `--qd-accent`; selected label =
`--qd-accent-text`; tab strip exposes tablist/tab roles.
**Commit:** `refactor(words): adopt qd-chip/qd-tabs, ban gold fills, rebuild word-type-filter`.

### P6 — Density/motion/radius/ladder/modal + color-mix cleanup
**Depends on:** P1 (safe alongside P3–P5; land after to avoid churn).
**Files:** `_components.scss`, `_forms.scss`, `top-navbar.component.scss`,
`mushaf-marker.component.scss`, `word-type-filter.component.scss`, `_tokens.scss`,
`_themes.scss`, plus the color-mix files in §11.
**Do:**
- [ ] **Motion:** remove `word-type-filter__button:active { transform: scale(0.97) }` and the
      `count-pulse` `scale(1.12)` keyframe → replace state feedback with color/border
      transition only. (Also remove the now-dead `@keyframes`.)
- [ ] **Literals:** every `0.15s`/`150ms` → `var(--qd-t-fast)` (navbar ×3, `_forms.scss` ×3).
- [ ] **`qd-select`:** resting border `color-mix(border, accent)` → plain `--qd-border` (no
      gold at rest); hover fill → `--qd-surface-hover`; radius stays `md` per doctrine.
- [ ] **Radius doctrine:** sm = controls, md = cards/tables/tabs, lg = modals/feature, pill =
      chips. Fix `word-type-filter__trigger`/`__panel` `lg → md`.
- [ ] **Surface ladder + modal:** retire `--qd-surface-elevated`; map its consumers
      (`_components.scss` `.qd-btn:hover`, modal fallback; `top-navbar` theme-toggle hover;
      `mushaf-marker`; `word-type-filter__panel`; `--qd-explorer-table-header-bg`) to the
      correct step: `--qd-surface-hover` for hovers, and **`--qd-surface`** for the modal
      (Option B, locked in R1 — the modal's elevation comes from `--qd-shadow-lg` + the dimmed
      backdrop, not a distinct surface tone). Independently, fix the dark-mode ladder ordering
      so elevation direction matches light — that correction stays regardless of the modal
      choice. Do **not** introduce a `--qd-surface-3` token.
- [ ] **color-mix cleanup:** replace role-derivations (accent borders, hover fills, header bg)
      with `--qd-border-accent`/`--qd-surface-hover`; leave genuine one-offs (e.g. skeleton
      shimmer gradient, alt-card 88% blend) and annotate why.
**Verify:** reduced-motion unaffected; navbar/select/mushaf-marker visually stable; modal
reads as elevated (not inset) in **both** themes on `--qd-surface` (shadow-lg + backdrop
carry the lift); header-bg unchanged in appearance.
**Tests:** keep green; add a regression that the modal's computed background = `--qd-surface`
and that no element animates `transform` on press (reduced-motion + default).
**Commit:** `refactor(styles): motion/radius/ladder cleanup, retire surface-elevated`.

### P7 — Loading→skeleton, dead-code removal, docs finalization
**Depends on:** P2 (skeleton system) and P5/P6.
**Files:** `dashboard-home`, `mushaf-page-area`, `selected-ayah-section`,
`selected-word-section` `*.{html,ts,scss}`; `dashboard-home` (`.dashboard-card`);
`_components.scss`; the four architecture/README docs.
**Do:**
- [ ] Convert the **4 content text-loading states** to skeletons via `qd-panel-skeleton`/
      `qd-skeleton-rows`: dashboard-home ("جارٍ تحميل بيانات التطبيق…"), mushaf-page-area
      ("جارٍ تحميل الصفحة…"), selected-ayah-section ("جارٍ تحميل دراسة الآية…"),
      selected-word-section ("جارٍ تحميل تحليل الكلمة…"). Keep the Arabic text as the sr-only
      `role="status"` label. **Exempt the footer status line** ("جارٍ التحقق من الحالة…") — do
      not touch the footer.
- [ ] `dashboard-home` `.dashboard-card` → `qd-card--hover` (delete the local duplicate hover
      SCSS).
- [ ] Remove dead code: retired `--qd-surface-elevated`, dead `@keyframes`, orphaned
      per-component table/list selectors superseded by P3/P4, the old
      `explorer-panel-skeleton` selector once no call-site references it.
- [ ] Finalize docs: reconcile `UI_STYLE_SYSTEM.md` §16/§17 with what shipped; update
      `mushaf/README.md` (skeleton/loading contract) and `words/README.md` (selected-state +
      table/list contract) where an invariant changed.
**Verify:** the four screens show skeletons that match loaded layout; reduced-motion static;
`aria-busy`/`role=status` present; grep confirms no reintroduced literals/gold fills.
**Tests:** update the four components' specs to assert skeleton presence + sr-only status
label while loading (replacing any assertion on the old loading *text* node); keep the rest
green.
**Commit:** `refactor(app): text-loading → skeletons; remove dead styles; finalize doctrine`.

---

## 8. Per-phase tests (summary matrix)

Behavior-first regression; assert **state and role**, not selector internals. Data-drive
variants (chip selected/unselected/disabled; state empty/loading/error) rather than
copy-paste specs. Do not test framework guarantees. Construct real view models — do not
mock DTOs.

| State / concern | Where asserted |
|---|---|
| Selected (tint bg, accent-text, no gold fill) | P3 table row, P5 chip/badge/tab |
| Hover (`--qd-surface-hover`) | P3/P5 (computed style spot-check) |
| Focus-visible ring present | P2 tabs/chip |
| Empty / error (calm danger-on-tint) | P2 `qd-state`, P4 lists |
| Loading = skeleton, non-interactive, `aria-busy`+`role=status` | P2 skeletons, P4 rows, P7 four screens |
| RTL (roving nav direction, logical props) | P2 `qd-tabs` |
| Reduced-motion static (no transform) | P2, P6 |
| Tab a11y roles (`tablist`/`tab`/`aria-selected`) | P2, P5 |
| Contrast where changed (AA) | P1 gate + PR record (§9) |
| Density (row 2.5rem) | P3 |

**Keep green (do not rewrite):** all existing `*.spec.ts` for tables, lists, filters,
facades, url-sync, pagination. URL-state and facade specs must not change — this refactor
touches presentation only.

---

## 9. Validation & performance checks

- **Change detection:** all shared components OnPush; no new `@Input` churn or template
  function calls in hot paths. Confirm no CD regression on the explorer pages (drive the app
  and watch for extra ticks; the `performance-angular-review` skill is available if a
  regression is suspected — not required by default).
- **Virtual scroll intact:** CDK viewport, `cdkVirtualFor`, `track`, item size, and
  scroll-restore behavior unchanged after P3/P4; verify by scrolling a large roots/lemmas
  list and confirming recycling + selection highlight follow.
- **WCAG AA contrast (blocking gate, P1 + every color change):** verify each pairing ≥ 4.5:1
  (normal text) / 3:1 (large/UI) in **both** themes: `--qd-accent-text` on `--qd-selected-bg`;
  `--qd-danger` on `--qd-danger-tint`; `--qd-success` on `--qd-success-tint`; `--qd-accent-fg`
  on `--qd-accent`; muted text on new hover fill; dark-surface text on the reordered ladder.
  Record numbers in the PR. If any pairing fails, tune the tint L/C (tints are new, so tuning
  is free) — never ship a failing pairing.
- **No mushaf visual regression:** Quran text, ayah glyphs, markers, word segments, and the
  reader layout are pixel-stable; only mushaf *chrome* (surfaces/select/loading) changes.
  Compare the reader before/after.
- **Build + lint:** `npm run build` and `ng test` green each phase; no new console warnings.
- **Grep gates (run each phase):** `0` matches for `background: var(--qd-accent)` behind
  text, `color: var(--qd-primary)` on accent surfaces, `0.15s`/`150ms` literals, `scale(`
  in interaction states.

---

## 10. Risks, rollback, stop conditions

- **R1 — Surface ladder "`--qd-surface-3`" mismatch (RESOLVED — Option B locked).**
  Locked decision 5 said "modal moves to `--qd-surface-3`", but no `--qd-surface-3` token
  exists and it is **not** in the allowed new-token list. The ladder tokens are
  `--qd-surface` / `--qd-section-bg` / `--qd-surface-recessed`. In **light**, `--qd-surface-recessed`
  is the *darkest* step (`L≈0.921`) — a modal on it would read **inset**, not elevated; in
  **dark** it is the *brightest* (`L≈0.302`). So the semantics of "recessed" differ by theme,
  which is exactly the ordering bug.
  **Decision (locked): Option B.** The modal moves to **`--qd-surface`** (near-white card in
  light / card surface in dark) and its elevation comes from `--qd-shadow-lg` + the dimmed
  backdrop — zero new tokens, and no theme-dependent inset risk. `--qd-surface-elevated` is
  retired either way. The **dark-mode ladder ordering is still corrected** for cross-theme
  consistency; that fix is independent of the modal choice and stays regardless. **No
  `--qd-surface-3` token is introduced.**
  - **Considered alternative (A, not chosen):** interpret "`--qd-surface-3`" as the existing
    3rd ladder step `--qd-surface-recessed`, correct the dark ladder values, keep the modal on
    `--qd-surface-recessed`, and rely on shadow + backdrop for lift — re-tuning the light
    recessed value only if the modal read inset. Rejected because it makes the modal surface
    theme-fragile for no visual gain over B.
- **R2 — Collapsing a table/list family changes behavior.** If any of the 5 tables / 10 lists
  cannot share the base class without altering rendered layout or virtual-scroll behavior
  (e.g. a column-specific rule that is not purely `grid-template-columns`), **stop**: leave
  that family on its own selectors, document the exception in §17, and do not force the
  collapse. Partial collapse is acceptable; behavior change is not.
- **R3 — `word-type-filter` refactor scope.** The 299-line rebuild onto `qd-card`+`qd-chip`
  is the largest behavior-risk. Gate it behind its own commit; if the popover/child-selection/
  secondary-select behavior shifts at all, revert just that commit — the chip/tab migrations
  around it are independent.
- **R4 — Merge collision with 026/027.** If the Precondition slips and 026 is not merged,
  **do not start** — the words surfaces are moving. Re-inventory §3 after merge.
- **Rollback:** each phase is one PR/commit; revert the phase commit to undo. Tokens (P1) are
  additive and safe to keep even if later phases revert. No data/behavior migration exists to
  unwind.
- **General stop condition:** any WCAG AA failure that cannot be fixed by tint tuning, or any
  detectable behavior/IA/contract/Quran-render change, halts the phase for a decision.

---

## 11. color-mix() inventory (P6/P4 cleanup targets)

45 `color-mix()` calls across 16 files. Replace **role-derivations** with scaled tokens;
keep genuine one-offs (annotate). Files:
`_tokens.scss`, `_themes.scss`, `_components.scss`, `_forms.scss`, `_explorer-tables.scss`,
`_explorer-detail-lists.scss`, `word-count-chip`, `ayah-matches-list`, and 8 mushaf component
SCSS (`word-morphology-summary`, `surah-jump-picker`, `source-selector`, `segment-data-rows`,
`selected-ayah-section`, `mushaf-word`, `selected-word-section`, `mushaf-header-navigation`).
- **Replace:** accent-border mixes (`color-mix(accent 28–40%, border)`) → `--qd-border-accent`;
  hover fills (`color-mix(section-bg, accent-tint)`) → `--qd-surface-hover`; header bg
  (`color-mix(primary 6–8%, surface-elevated)`) → keep as a defined token but re-base off the
  retained ladder step (not `--qd-surface-elevated`).
- **Keep (one-off, annotate):** skeleton shimmer gradient; `ayah-matches-list__card--alt` 88%
  blend; any mushaf-content tint that is genuinely unique (but **not** Quran glyph styling).

---

## 12. Acceptance criteria (all must hold)

- The doctrine's **allowed-gold list holds on every screen**: no solid `--qd-accent`/
  `--qd-accent-soft` fill behind text; no gold at rest; selected state uniform (tint +
  accent-text + hairline/indicator) in words, mushaf chrome, dashboard, and layout.
- **One implementation each** of table (`.qd-explorer-table`), detail-list
  (`.qd-detail-list`), tab (`qd-tabs`), chip (`qd-chip`), state (`qd-state`), and loading/
  skeleton (`.qd-skeleton` vars + `qd-skeleton-rows` + `qd-panel-skeleton`). Per-component
  SCSS is columns-only.
- **Both themes pass AA** for every new/changed pairing (recorded in PRs).
- **Density applied uniformly across tables AND detail lists**: explorer table row `2.5rem`,
  cell `6×10px`, header `2.75rem`; detail-list rows/cells aligned to the same compact rhythm
  via the shared `.qd-detail-list` base.
- **Zero hardcoded color/timing literals reintroduced** (grep gates clean): no `0.15s`/
  `150ms`, no `scale()` interaction transforms, no `color: var(--qd-primary)` on accent
  surfaces.
- **`--qd-surface-elevated` retired**; ladder direction consistent across themes; modal sits
  on `--qd-surface` and reads elevated in both (via `--qd-shadow-lg` + backdrop).
- **The 4 content text-loading states are skeletons**; footer status line unchanged.
- **No behavior/IA/API/DTO/DB/Quran-render change**; all pre-existing specs green; virtual
  scroll + URL-state intact.
- Docs shipped: `UI_STYLE_SYSTEM.md` §16/§17 (stale notes fixed), DESIGN.md One Voice Rule
  allowed-gold list, and the touched READMEs.

---

## 13. Commit / PR boundaries (one per phase)

1. `feat(styles): color-doctrine tokens + UI_STYLE_SYSTEM §16/§17` (P1)
2. `feat(ui): shared qd-tabs/qd-chip/qd-state + unified skeleton system` (P2)
3. `refactor(words): collapse explorer tables into .qd-explorer-table + density` (P3)
4. `refactor(words): collapse detail lists into .qd-detail-list` (P4)
5. `refactor(words): adopt qd-chip/qd-tabs, ban gold fills, rebuild word-type-filter` (P5)
6. `refactor(styles): motion/radius/ladder cleanup, retire surface-elevated` (P6)
7. `refactor(app): text-loading → skeletons; remove dead styles; finalize doctrine` (P7)

Each PR: list global style files changed, new `qd-` classes/components, tokens added/changed,
components affected, light/dark impact, RTL impact, AA contrast numbers, build status (the
`UI_STYLE_SYSTEM.md` §14 Definition-of-Done report).

---

## 14. Self-review against the brief

- **Locked decisions 1–8:** ban gold fills (P1 tokens + P3/P5/P6 call-sites); keep dark gold
  primary, add `--qd-accent-fg` (P1); hybrid unification — classes for tables/lists, Angular
  components for tabs/chip/state/loading (P2–P5); warning hue frozen (§2); ladder fix + retire
  `--qd-surface-elevated` + modal step (P6/R1); sequencing gate (§0); loading→skeleton with
  footer exempt (P7); density (P3). All covered.
- **New tokens:** exactly the seven in §4; no `--qd-surface-3` invented (R1).
- **Doctrine:** §16/§17 + DESIGN.md allowed-gold list + stale-note fix (P1/P7).
- **Also-items:** color-mix cleanup (§11/P6), calm-motion (P6), literal timings (P6), radius
  doctrine incl. word-type-filter trigger `lg→md` (P6), `.dashboard-card`→`qd-card--hover`
  (P7), word-type-filter rebuild (P5).
- **R1 resolved (Option B, locked):** modal → `--qd-surface` + shadow-lg/backdrop; dark ladder
  ordering still corrected; no `--qd-surface-3` token invented.
- **Plan-only:** no code/token/SCSS changes made; this document is the sole artifact.
