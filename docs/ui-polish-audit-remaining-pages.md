# UI Polish Audit — Remaining Pages

**Status: OPEN — report only.** No production code, tests, styles, or configuration were changed
by this audit. No implementation plan is written here. Nothing was committed.

The Mushaf Reader audit lives in its own report, `docs/ui-polish-audit-mushaf-reader.md`, and is
not repeated here. Where a finding below is the same defect class as one already recorded there,
it is cross-referenced, not restated.

## Method

**Code-first.** Every finding was traced to a file and line by reading the source. The browser was
used only where a claim could not be settled from code alone — cascade winners between a global
stylesheet and a component stylesheet, intrinsic flex sizing, and one reproduction of a blank
panel. Those measurements are labelled **(measured)**; everything else is **(code)**. Where a
consequence follows arithmetically from a measured value plus a CSS rule, it is labelled
**(derived)**.

Measurement environment for the browser checks: Chrome, `document.documentElement.clientWidth =
1905`, `ng serve` development configuration, backend on `https://localhost:5015`, signed out
(anonymous). Anonymous session means the Access Management page and the navbar **الإعدادات**
trigger could not be rendered; findings for those are code + arithmetic and are marked as such.

---

## Executive Summary

**Total findings: 17** — 7 cross-cutting, 10 page-scoped.

| Severity | Count | IDs |
| --- | --- | --- |
| HIGH | 5 | X-1, X-2, X-3, L-1, A-1 |
| MEDIUM | 8 | X-4, X-5, X-6, U-1, R-1, R-2, B-1, N-1 |
| LOW | 4 | X-7, R-3, B-2, N-2 |

**Shared / global findings (7):** X-1 (details content projection destroys itself on a backward
tab move), X-2 (`qd-tabs` has no layout mode that matches the locked target behavior), X-3 (ayah
cards clip Quran text under a height-constrained result list), X-4 (`flex-wrap` applied to the
`qd-tabs` host is inert), X-5 (four byte-identical details-panel templates), X-6 (dead and
out-cascaded rules in `styles/_explorer-detail-lists.scss`), X-7 (`@switch` blocks with no
exhaustive fallback).

**Page-specific findings (10):** U-1 (Unique Words), R-1/R-2/R-3 (Roots), L-1 (Lemmas),
A-1 (Access Management removal surface), B-1/B-2 (Abwab), N-1/N-2 (Navbar).

**Functional / loading findings (4):** X-1 (blank panel — functional, not cosmetic), X-7,
R-2 (details geometry collapse on tab switch), A-1's eager audit/reconciliation loads.

**Confirmed deletion candidates:** the Access Management **سجل الوصول** and **الأمان المتقدم**
tabs, and — following from them — the Access tab strip itself. Enumerated in
*Confirmed Frontend Removal Candidates*.

**One conclusion worth stating up front, because it reframes most of section 1 of the brief:**
the Words details panels are **not** competing implementations of a tabs concept. All five
(Roots, Lemmas, Stems, Word Types, Unique Words drilldown) already compose the *same* shared
`qd-details-workspace` + `qd-tabs` + `qdTab` primitives, from *byte-identical* templates. The
divergence the brief describes is not architectural drift between pages — it is that the **shared
`qd-tabs` primitive has no layout mode that produces the locked target behavior**, so each
consumer lands in one of two wrong modes depending only on how many tabs it happens to have. The
work is therefore mostly *inside* the existing primitive, not a new one. The one genuine
page-specific divergence is a single stray CSS declaration in Lemmas (L-1).

---

## Cross-Cutting Findings

### X-1 — Switching to an earlier tab permanently empties the details content

- **ID:** X-1
- **Severity:** HIGH
- **Area:** Shared details panels (Roots, Lemmas, Stems, Word Types) — side panel, modal, and
  global detail overlay.

#### Current behavior

Clicking a details tab that sits **earlier** in the tab order than the current one leaves the
details content area completely empty — no content, no skeleton, no empty state, no error. The
tab strip and the panel header stay. The content does not come back on its own; it returns only
when a tab **later** in the order is selected.

This is the defect described in the brief as "content becomes completely blank during tab
switching". The measurement below shows it is **not** a loading-window artefact — it is
permanent for as long as the user stays on that tab.

#### Evidence

**(measured)** Roots explorer, root `1` selected, desktop inline panel. After each click the
active (non-`hidden`) `.root-details-panel__surface` was inspected 1.4 s later. Tab order is
`ROOT_VIEW_KEYS = ['words', 'ayahs', 'surahs', 'lemmas', 'stems']`
(`features/words/models/roots.models.ts:190`):

| Transition | Direction in tab order | Active surface child count | Active surface height |
| --- | --- | --- | --- |
| → stems | forward | 1 | 1422 px |
| stems → lemmas | **backward** | **0** | **0 px** |
| lemmas → stems | forward | 1 | 1422 px |
| stems → words | **backward** | **0** | **0 px** |
| words → ayahs | forward | 1 | 570.5 px |
| ayahs → words | **backward** | **0** | **0 px** |

Six for six, deterministic. A second run also confirmed `.qd-explorer-subview-panel` is **absent
from the DOM** in the failing state (not merely collapsed), and that the sub-tab strip
(`.qd-explorer-subtabs`) disappears with it. No console errors are logged.

#### Exact affected files/components

- `features/words/components/root-details-panel/root-details-panel.component.html:1-3` —
  `<ng-template #projectedContent><ng-content /></ng-template>`
- same file `:65-84` — five sibling `<section [hidden]="!isActive(tab.key)">` blocks, each with
  `@if (isActive(tab.key)) { … <ng-container *ngTemplateOutlet="projectedContent" /> }`
- **byte-identical at the same line numbers** in:
  - `features/words/components/lemma-details-panel/lemma-details-panel.component.html:1-3, 68, 80`
  - `features/words/components/stem-details-panel/stem-details-panel.component.html:1-3, 68, 80`
  - `features/words/components/word-type-details-panel/word-type-details-panel.component.html:1-3, 68, 80`
- Content producers hit by it: `features/words/pages/roots-explorer-page/…component.html`
  (`#rootsPanelContent`), the equivalent templates in the Lemmas / Stems / Word Types pages, and
  all four `features/words/entity-detail-overlay/adapters/*-detail-overlay-adapter.component.html`
- **Not affected:** `features/words/components/word-drilldown-modal/word-drilldown-modal.component.html`
  — it renders each view inline per `@for` branch and never projects, so Unique Words is immune by
  construction.

#### Root cause

`<ng-content>` is a **single, one-time projection slot**. Wrapping it in an `ng-template` and
outletting that template from five sibling conditional blocks means the projected DOM has exactly
one home at a time and is *moved* between homes as the active tab changes.

When the newly active section appears **earlier** in the `@for` than the outgoing one, Angular
creates the new embedded view **before** destroying the old one. The projected nodes are inserted
into the new location, and then the destruction of the old view detaches them again — because
they are still registered as that view's projected content. The result is a live, visible, empty
section. Moving **forward** destroys the old (lower-index) view first and then creates the new
one, so the ordering happens to work.

The `[hidden]` attribute on the inactive sections is a red herring: the sections are all present,
but only the active one instantiates the outlet, and only one outlet can hold the projection.

Classification requested by the brief: **component remount / rendering** — specifically a content
projection lifecycle defect. It is **not** API latency, **not** state reset (the controller keeps
the previous data in `_panel`, see R-2), and **not** a cache problem.

#### User-visible impact

A core navigation action in four explorers silently destroys the thing the user just asked for.
Because the content never recovers, the page reads as broken rather than slow. Every consumer
surface is affected: the desktop side panel, the sub-`1080` modal, and the global detail overlay
(which is reachable from Mushaf word links, so the blast radius crosses features).

#### Recommended direction

Stop projecting one `<ng-content>` into a rotating set of outlets. Two viable shapes, both
already precedented in this repo:

1. **Project once into a single stable container** and let the *page* decide which view renders
   inside it. The tabpanel identity (`role`, `id`, `aria-labelledby`, `tabindex`) can move to that
   one container and track the active tab, exactly as `selected-ayah-section` in Mushaf already
   does with one mounted panel. This is the smaller change and preserves the current
   `aria-controls`-for-the-selected-tab-only convention.
2. **Mount all panels and toggle `[hidden]`,** which is the pattern
   `word-drilldown-modal.component.html:61` already uses successfully — but that requires the
   content to be per-view components rather than a single projected slot, i.e. it changes the
   panel's contract with its pages.

Option 1 is the recommendation: it is contained to the four panel templates and leaves the pages'
`@switch` content untouched.

#### Shared/global or page-specific

**Shared.** One template pattern duplicated verbatim across four components; the fix is the same
edit four times, or one extraction (see X-5).

#### Risk

**MEDIUM.** The tabpanel/tab ARIA wiring (`workspace.tabId()` / `workspace.panelId()` from
`shared/ui/details-workspace`) is intertwined with the five-section loop, and
`features/words/README.md:173` documents the `.qd-explorer-subview-panel` id contract that the
pages bind `[panelId]` against. Collapsing to one panel changes which ids exist. Keyboard
roving-focus behavior in `QdTabsComponent` is unaffected.

#### Dependencies / related

Blocks nothing, but should land **before** X-2, because X-2 changes the tab strip that sits above
this content and both touch the same four templates. Related: X-5 (the duplication), X-7 (the
non-exhaustive `@switch` that would otherwise mask a similar blank).

---

### X-2 — `qd-tabs` has no layout mode matching the locked target behavior

- **ID:** X-2
- **Severity:** HIGH
- **Area:** Shared tabs primitive; every details navigation and several toolbars.

#### Current behavior

`QdTabsComponent` picks its layout **solely from how many tabs it happens to contain**:

```ts
// shared/ui/tabs/tabs.component.ts
export const QD_TABS_SEGMENTED_MAX = 3;
segmented  = layout() === 'inline' && tabs().length <= 3
scrollable = layout() === 'inline' && tabs().length >  3
```

Neither branch produces the locked target (equal-width, use the container, max 5 per row, wrap
instead of scroll):

- **`--segmented` (≤ 3 tabs)** — `.qd-tabs` stays `display: inline-flex`
  (`styles/_components.scss:299`); `.qd-tabs--segmented` (`:307-311`) adds only padding, radius
  and background. The row therefore **shrink-wraps** and the `flex: 1 1 0` equal-width
  distribution (`:380-383`) divides a shrink-to-fit box instead of the available width.
- **`--scrollable` (> 3 tabs)** — `.qd-tabs--scrollable` (`:313-318`) sets `display: flex` and
  `overflow-x: auto`. Tabs stay `flex: 0 0 auto` (`:335`). Under the container width they pack to
  the RTL start and leave the rest empty; over it they produce **exactly the horizontal scrollbar
  the brief forbids**.
- `.qd-tabs { flex-wrap: nowrap }` (`:302`) is never overridden by either modifier, so wrapping
  is impossible in both modes.

`--grid` exists (`:325-329`, `repeat(var(--qd-tabs-grid-columns, 5), minmax(0, 1fr))`) and is the
only mode close to the target — but it is opt-in per consumer, has a fixed column count rather
than a wrapping one, and only two consumers use it.

#### Evidence

**(code)** Consumer inventory — 19 templates use `qd-tabs`/`qdTab`. Mode is determined by tab
count alone:

| Consumer | Tabs | Resolved mode | Symptom |
| --- | --- | --- | --- |
| `word-drilldown-modal` (Unique Words details) | 3 | `--segmented` | shrink-wrapped, see U-1 |
| `word-type-details-panel` | 2 | `--segmented` | shrink-wrapped |
| `access-admin-page` | 3 | `--segmented` | shrink-wrapped |
| `unique-words-tabs` (mode switch) | 2 | `--segmented` | shrink-wrapped |
| `abwab-toolbar` (sections) | 1 + N sections | `--segmented` ≤ 3, `--scrollable` > 3 | see B-1 |
| `roots/lemmas/stems` sub-tabs (`.qd-explorer-subtabs`) | 2 each | `--segmented` | shrink-wrapped |
| `stem-details-panel` | 4 | `--scrollable` | packed to start, unused width |
| `root-details-panel` | 5 | `--scrollable` | packed to start, unused width, see R-1 |
| `lemma-details-panel` | 4 | `--scrollable` | **scrollbar**, see L-1 |
| Mushaf ayah study | 5 | `--scrollable` | see the Mushaf report, M-1 |
| `word-type-table-view-tabs` | 4 | `layout="grid"` | **correct** |
| `abwab-move-picker` | — | `layout="grid"` | correct |

**(measured)** Two representative rows, both in a details panel:

```
Unique Words drilldown (3 tabs, --segmented)
  .qd-details__tabs slot width : 666.0 px
  .qd-tabs computed display    : inline-flex
  tablist width                : 212.5 px   →  32% of the slot used, 454 px empty
  per-tab width                : 65.5 px each (flex: 1 1 0)

Roots details (5 tabs, --scrollable)
  .qd-details__tabs slot width : 634.0 px
  .qd-tabs computed display    : flex, overflow-x: auto
  sum of tab widths            : 297.6 px   →  47% of the slot used, 336 px empty
  per-tab width                : 54.5 – 66.5 px (unequal, content-sized)
  scrollWidth == clientWidth   : no scrollbar in this case
```

**(measured)** Wrapping is genuinely impossible today: on the Abwab toolbar the inner tablist
reports `flex-wrap: nowrap` and `display: inline-flex` at runtime.

#### Exact affected files/components

- `src/app/shared/ui/tabs/tabs.component.ts` — `QD_TABS_SEGMENTED_MAX`, `segmented`, `scrollable`
- `src/app/shared/ui/tabs/tabs.component.html` — the `.qd-tabs` tablist element
- `src/styles/_components.scss:298-329` — `.qd-tabs`, `--segmented`, `--scrollable`, `--vertical`,
  `--grid`
- `src/styles/_components.scss:331-350` — `.qd-tabs__tab { flex: 0 0 auto }`
- `src/styles/_components.scss:380-383` — `.qd-tabs--segmented .qd-tabs__tab { flex: 1 1 0 }`

#### Root cause

The primitive encodes a **count heuristic** where the product rule is a **layout contract**.
Three-or-fewer was presumably meant to read as "a segmented control", four-or-more as "a scrolling
strip"; the locked behavior wants neither. Because the mode is derived rather than declared, no
consumer can ask for the correct behavior without either opting into `--grid` (fixed columns) or
adding its own CSS on top — which is exactly how L-1 and B-1 arose.

#### User-visible impact

Every details navigation in the product is either compressed into a third of its container with
cramped labels, or spread as unequal content-sized chips against a large empty gap, or scrolling
horizontally. None of them looks deliberate, and the same concept looks different on every page
purely as a function of tab count.

#### Recommended direction

Give `qd-tabs` a **declared** layout that implements the locked contract, and retire the count
heuristic:

- A wrapping equal-width track layout: `display: grid`,
  `grid-template-columns: repeat(auto-fit, minmax(<floor>, 1fr))` with a **cap of 5 columns**, so
  fewer than 5 items distribute across the full width and more than 5 wrap onto a second row.
  This satisfies "equal width", "use the container", "max 5 per row", "wrap not scroll", and
  "responsive column count" in one rule, and it removes `overflow-x` entirely.
- Keep `--vertical` as-is. Fold `--grid` into the new mode (its two current consumers already ask
  for exactly this, just with a hardcoded column count).
- Keep `--segmented`'s *visual* treatment (sunken background, radius) available as a skin, but
  decouple it from tab count — a 3-tab details header should still fill its container.
- The tab label must stop wrapping mid-label: with `white-space: normal` today the flex intrinsic
  sizing collapses to the widest **word**, not the widest **label** (this is the mechanism behind
  U-1's 60 px "لم يذكر فيها"). A single-line label with a min column floor sized from the longest
  label is the honest fix.

What must remain page-specific: which labels, how many tabs, disabled state, and the active-state
semantics (all already per-consumer inputs). Nothing about *where* a tab sits should stay
page-specific.

#### Shared/global or page-specific

**Shared/global.** 19 consumer templates across `words`, `mushaf`, `abwab`, `access-admin`.

#### Risk

**MEDIUM–HIGH.** This is the highest-blast-radius change in the report: it moves every tab row in
the product simultaneously, including the Mushaf ayah-study strip covered by M-1 in the other
report. It must pass `npm run check:golden-ui` (band vocabulary, no raw responsive thresholds,
single gutter owner). Two specific interactions to watch: `QdTabsComponent`'s
`effect(() => selected?.scrollIntoView())` becomes pointless once nothing scrolls and should be
removed with the overflow, and Compact-band density needs re-measurement — 5 equal columns below
~500 px will not hold readable Arabic labels, so the wrap floor is load-bearing.

#### Dependencies / related

Supersedes the local workarounds in L-1 and B-1 — both should be deleted, not adjusted, once X-2
lands. Resolves M-1's secondary contributor in the Mushaf report (the row can no longer overflow
or scroll). Should land **after** X-1 to avoid two people editing the same four templates.

---

### X-3 — Ayah cards clip Quran text under a height-constrained result list

- **ID:** X-3
- **Severity:** HIGH
- **Area:** Shared `qdAyahCard` / `qdResultItem` primitives; manifests in every Words explorer's
  الآيات view.

#### Current behavior

In the الآيات view of the Words details panels, every ayah card is forced to a fixed 44 px box
while its content needs 63–90 px. The Quran text is cut horizontally through the glyph line.

#### Evidence

**(measured)** Roots explorer, root `1`, الآيات view, first six cards:

```
.ayah-matches-list__viewport
  display: flex   flex-direction: column   block-size: 480px   overflow: auto
  scrollHeight: 5215 px   (100 cards)

card[0]  height 44.0  clientHeight 42  scrollHeight 63   clipped
card[1]  height 44.0  clientHeight 42  scrollHeight 90   clipped
card[2]  height 44.0  clientHeight 42  scrollHeight 90   clipped
card[3]  height 44.0  clientHeight 42  scrollHeight 90   clipped
… every sampled card: flex-shrink 1, min-block-size 44px, align-items center
```

A screenshot of that state shows the ayah lines visibly sliced through their glyphs.

#### Exact affected files/components

- `src/styles/_components.scss:902-909` — `.qd-result-list { display: flex; flex-direction: column }`
- `src/styles/_components.scss:911-921` — `.qd-result-item { display: flex; align-items: center;
  min-block-size: var(--qd-hit-target-min) }`
- `src/app/shared/ui/ayah-card/ayah-card.component.scss` — `:host { display: flex;
  flex-direction: column; … }`; declares **no** `flex-shrink` and **no** `min-block-size`
- `src/app/features/words/components/ayah-matches-list/ayah-matches-list.component.scss:11-15` —
  `.ayah-matches-list__viewport { block-size: min(58vh, 30rem); overflow: auto }`
- `src/app/features/words/components/ayah-matches-list/ayah-matches-list.component.html` — the
  card carries **both** `qdAyahCard` and `qdResultItem`
- Consumers of `qd-ayah-matches-list` (9 templates): the Roots / Lemmas / Stems / Word Types
  explorer pages, `word-drilldown-modal`, and all four `entity-detail-overlay/adapters/*`

#### Root cause

Four facts compose, and no single one of them is wrong on its own:

1. `qdResultList` makes the list a **column flex container**.
2. `.ayah-matches-list__viewport` gives that container a **definite height** (`min(58vh, 30rem)` =
   480 px measured).
3. The cards are flex items and default to **`flex-shrink: 1`** — neither `.qd-ayah-card` nor
   `.qd-result-item` sets `flex-shrink: 0`.
4. The only floor the cards have is `.qd-result-item`'s **`min-block-size: var(--qd-hit-target-min)`
   = 44 px** — a *touch-target* floor, borrowed by a card that is not a touch target.

So every card is compressed to the hit-target minimum and its content overflows. Because the card
is `overflow: visible`, the text is not hidden by the card itself — it is overlapped by the next
card, which is what reads as "clipped".

A secondary defect from the same collision: `.qd-result-item { align-items: center }` fights
`.qd-ayah-card`'s `flex-direction: column`, so the ayah link is cross-axis-centred at fit-content
rather than stretched. Measured: a short ayah's link is 130.1 px wide inside a 604.5 px card
(cards 1–5 with longer text reach 578.5 px, so it only bites on short ayat).

Per the brief's classification: this is a **shared Quran/Ayah rendering issue**, not a Roots bug.
The Mushaf `similar-ayahs-card` / `mutashabihat-groups-card` compose the same `qdAyahCard` but sit
in a content-sized list with no definite height, so they do not shrink today — the shared
primitives are latently wrong there too, and only the absence of a fixed height is protecting them.

#### User-visible impact

Quran text is rendered unreadable in the most-used view of four explorers. This violates the
project's own locked rule that Quran text must never be clipped to satisfy row geometry.

#### Recommended direction

- Give the ayah card `flex-shrink: 0` so a Quran card is never compressed by its container. This
  is the minimal, targeted fix and it belongs on the shared `qdAyahCard`, because "a Quran card
  sizes to its text" is a property of the card, not of any one list.
- Decide explicitly whether `qdAyahCard` and `qdResultItem` should ever be on the same element.
  Today they overwrite each other's `display`, `align-items` and `padding` at equal specificity,
  and which one wins depends on stylesheet injection order. If both are needed (one for geometry,
  one for list semantics/ARIA), the overlapping geometry declarations should live in exactly one
  of them.
- Reconsider `.qd-result-item { min-block-size: var(--qd-hit-target-min) }` as a *blanket* rule.
  A hit-target floor is right for interactive rows and wrong for a content card; scoping it to the
  interactive variants would remove the false floor without losing the accessibility property.
- Do **not** fix this by raising the fixed viewport height — the content is variable-height by
  nature.

#### Shared/global or page-specific

**Shared.** The defect lives in `shared/ui/ayah-card` and `styles/_components.scss`; the trigger
lives in one Words component but is consumed by 9 templates.

#### Risk

**MEDIUM.** `flex-shrink: 0` on the card changes list scroll geometry everywhere ayah cards appear,
including the Mushaf similar-ayahs / mutashabihat cards whose *loading placeholders* are
deliberately count-and-geometry-matched to the loaded cards (documented in
`features/mushaf/README.md`). Those placeholders compose the same frame, so they should follow the
loaded geometry automatically — but that is the one thing to re-verify visually after the change.

#### Dependencies / related

Independent of X-1 and X-2. Related to X-6 (a global override that was *meant* to unconstrain
these viewports but never takes effect).

---

### X-4 — `flex-wrap` applied to the `qd-tabs` host element is inert

- **ID:** X-4
- **Severity:** MEDIUM
- **Area:** Shared tabs primitive + its two wrapping consumers.

#### Current behavior

Two global classes try to make tab rows wrap by styling the `<qd-tabs>` **host**:

```scss
// styles/_words-explorer-layout.scss:34
.qd-explorer-subtabs { display: flex; flex-wrap: wrap; gap: var(--qd-space-2); … }

// features/abwab/components/abwab-toolbar/abwab-toolbar.component.scss
.abwab-toolbar__tabs { flex-wrap: wrap; }
```

Neither has any effect. The element that actually lays the tabs out is the `div.qd-tabs`
`role="tablist"` **inside** the component's template, and it is `flex-wrap: nowrap`.

#### Evidence

**(code)** `shared/ui/tabs/tabs.component.html` renders `<div class="qd-tabs" role="tablist">` as
the template root; the tabs are that div's children. `:host { display: block }` in
`tabs.component.scss`. A `flex-wrap` on the host cannot reach the children of a descendant div.

**(measured)** Abwab toolbar, live: the `<qd-tabs>` host computes to `display: block` (the
component's `:host` rule wins over the global class) and the inner tablist computes to
`flex-wrap: nowrap`, `display: inline-flex`.

#### Exact affected files/components

- `src/styles/_words-explorer-layout.scss:34-40` (`.qd-explorer-subtabs`) — 6 consumer templates
  (Roots / Lemmas / Stems pages, and the Roots / Lemmas / Stems overlay adapters, 2 rows each)
- `src/app/features/abwab/components/abwab-toolbar/abwab-toolbar.component.scss`
  (`.abwab-toolbar__tabs`)
- `src/app/shared/ui/tabs/tabs.component.html`, `tabs.component.scss`

#### Root cause

The tabs component projects its content into an inner tablist div but exposes no way to style
that div from outside. Consumers reached for the only element they could address — the host — and
the styles have been sitting there doing nothing.

#### User-visible impact

None directly today (the rows do not wrap, but they also would not have needed to at their
current tab counts). The real cost is that it hides X-2: two places in the codebase *believe*
wrapping is configured, which makes the primitive look more capable than it is.

#### Recommended direction

Delete both declarations as part of X-2, and let the primitive own wrapping. Do not "fix" them by
adding `::ng-deep` or a deeper selector — that would entrench styling a shared component's
internals from the outside.

#### Shared/global or page-specific

**Shared** (the primitive), with two page-level dead declarations to remove.

#### Risk

**LOW.** Removing inert declarations changes no rendered pixel; the measurement above confirms
they are not currently doing anything.

#### Dependencies / related

Sub-finding of X-2; should be removed in the same change.

---

### X-5 — Four byte-identical details-panel templates

- **ID:** X-5
- **Severity:** MEDIUM
- **Area:** Words details panels.

#### Current behavior

`root-details-panel`, `lemma-details-panel`, `stem-details-panel` and `word-type-details-panel`
have the same template, the same structure, and the same defects at the **same line numbers**.
Their SCSS files are also near-identical. The five overlay adapters and the five page templates
repeat the same `@switch (status)` content block a second and third time.

#### Evidence

**(code)** `grep -n` across the four panel templates returns identical hits:

```
ng-template #projectedContent  → line 1 in all four
<ng-content />                 → line 2 in all four
[hidden]="!isActive(tab.key)"  → line 68 in all four
*ngTemplateOutlet=…            → line 80 in all four
qd-tabs qdDetailsTabs          → line 37 in root/stem/word-type
```

The SCSS files differ only in the BEM prefix and three genuinely local declarations:
`.lemma-details-panel__tab { inline-size: 100% }` (the L-1 defect),
`.stem-details-panel__tab { overflow: hidden; text-overflow: ellipsis; white-space: nowrap }`, and
`.lemma-details-panel__tab:disabled { opacity: .5 }`. Root and Word Types set only `font-family`
and `font-size`.

`features/words/entity-detail-overlay/adapters/root-detail-overlay-adapter.component.html` is a
near-verbatim copy of `roots-explorer-page.component.html`'s `#rootsPanelContent` template — same
`@switch`, same six branches, differing only in test ids and in reading `frame()` instead of
`activeView()` / `panelState()`.

#### Exact affected files/components

- `features/words/components/{root,lemma,stem,word-type}-details-panel/*` (12 files)
- `features/words/entity-detail-overlay/adapters/*-detail-overlay-adapter.component.html` (5 files)
- the `#…PanelContent` templates in the four explorer pages

#### Root cause

The shared layer stops at `qd-details-workspace` + `qd-tabs`. The *composition* of those
primitives — header slots, tab loop, tabpanel loop, projection, ARIA id wiring — was copied per
entity instead of being expressed once.

#### User-visible impact

Indirect: every defect found in one panel exists in four, and any fix must be applied four times
or it introduces drift. X-1 and (had it been applied more widely) L-1 are both instances of this.

#### Recommended direction

Extract **one** details-panel shell that owns the header slots, the tab loop, the panel loop, the
ARIA ids, and the (post-X-1) single content container. Keep per-entity: the tab key list and
labels, the disabled predicate, the empty/not-found copy, the close/escape wiring, and the content
itself. This is a genuine three-plus-consumer duplication with identical behavior — it is not
abstraction for its own sake.

Do **not** extend the extraction to the page-level `@switch` content blocks. Those look similar
but carry entity-specific view keys, entity-specific list components and entity-specific
sub-views; unifying them would encode domain names into a shared component, which
`FRONTEND_UI_RULES.md` §1 explicitly prohibits.

#### Shared/global or page-specific

**Shared.**

#### Risk

**MEDIUM.** Four working surfaces are replaced by one; the ARIA id generation (`instanceId` in
`shared/ui/details-workspace/details-workspace.component.ts`) and the per-instance ids documented
in `features/words/README.md` must be preserved exactly, since the overlay and the side panel can
be on screen at the same time.

#### Dependencies / related

Naturally sequenced **after** X-1 and X-2 — extract the shell once the shape is correct, not
before, or the extraction bakes in the current defects.

---

### X-6 — Dead and out-cascaded rules in `styles/_explorer-detail-lists.scss`

- **ID:** X-6
- **Severity:** MEDIUM
- **Area:** Words explorer detail-list styling.

#### Current behavior

Roughly 100 lines of that stylesheet never apply. One block targets a class that exists in no
template. Another block matches correctly but **loses the cascade** to the component stylesheet it
was written to override.

#### Evidence

**(code)** `.explorer-detail-panel__body` — the selector prefix for `styles/_explorer-detail-lists.scss:345-410`
— appears in **zero** templates. `grep -rn "explorer-detail-panel__body" src/app` returns nothing.
Only `.explorer-detail-panel` (no `__body`) is used, on the five panel section elements.

**(measured)** The block at `styles/_explorer-detail-lists.scss:61-88` (`.root-details-panel,
.lemma-details-panel { … .ayah-matches-list__viewport { block-size: auto; max-block-size: none;
overflow: visible; flex: 0 0 auto } … }`) does match the live DOM — `.root-details-panel` is
confirmed as an ancestor of the viewport — yet the element computes to `block-size: 480px;
overflow: auto`, i.e. the **component** rule wins.

Both selectors carry specificity `(0,1,0)`+`(0,1,0)` after Angular's emulated-encapsulation
attribute is added, so the tie resolves on document order, and Angular appends component styles
after the global stylesheet.

#### Exact affected files/components

- `src/styles/_explorer-detail-lists.scss:61-88` — matches but loses the cascade
- `src/styles/_explorer-detail-lists.scss:345-410` — dead selector, never matches
- the component stylesheets that win:
  `features/words/components/ayah-matches-list/ayah-matches-list.component.scss` and siblings

#### Root cause

Global overrides were written at the same specificity as the component rules they intend to
override, on the assumption that a global sheet loaded earlier would win. It does not. Separately,
a wrapper class was renamed or removed without its stylesheet following.

#### User-visible impact

X-3's clipping is a direct consequence: the override at `:61-88` was *specifically* written to
release `.ayah-matches-list__viewport` from its fixed height inside the Roots and Lemmas panels,
and it does not run. The asymmetry also means Stems and Word Types were never even listed in that
override, so the three panels differ for no designed reason.

#### Recommended direction

- Delete the `.explorer-detail-panel__body` block outright — it is unreachable.
- For `:61-88`, decide the owner rather than raising specificity. The height policy of a detail
  list belongs to the component that renders the list; the panel should not be reaching in. Moving
  the height decision into `ayah-matches-list` (as an input or a variant class the panel sets)
  removes the cascade race entirely.
- While the cascade race exists, no conclusion drawn from reading that stylesheet is safe —
  verify against computed style, not source.

#### Shared/global or page-specific

**Shared** (a global stylesheet), affecting four explorers asymmetrically.

#### Risk

**LOW** for the dead block. **MEDIUM** for `:61-88`, because making it effective (rather than
deleting it) would change list heights and scroll containers in the Roots and Lemmas panels — and
that interacts directly with X-3.

#### Dependencies / related

Must be resolved together with X-3; they are the same geometry.

---

### X-7 — `@switch` content blocks with no exhaustive fallback

- **ID:** X-7
- **Severity:** LOW
- **Area:** Words explorer pages and overlay adapters.

#### Current behavior

The details content blocks switch on `panelState().status` with cases for `loading`, `error`,
`empty`, `success` — and **no `@default`**, no `@case('idle')`, no `@case('notFound')`. Inside
`@case('success')` the view is selected by an `@if / @else if` chain with **no terminal `@else`**.
Any combination outside the enumerated set renders literally nothing.

#### Evidence

**(code)** `features/words/pages/roots-explorer-page/roots-explorer-page.component.html` —
`@switch (panelState().status)` covers four of six `DetailPanelStateBase['status']` values; the
`@case('success')` chain ends at `stems` with no fallback. The same shape appears in the Lemmas,
Stems and Word Types pages and in all five overlay adapters.

**(code)** The container has nothing to fall back on either:
`styles/_words-explorer-layout.scss:42-47` — `.qd-explorer-subview-panel { display: flex;
flex-direction: column; flex: 1 1 auto; min-block-size: 0 }`. With no children it collapses to
zero height.

**(code)** The view predicate and the data come from **two different sources**: the template gates
on `activeView()` (which is `tableFocus.activeView() ?? panelState().view`,
`roots-explorer-page.component.ts:100`) while the data lives in `panelState()`. They are allowed
to diverge — `ExplorerTableFocusController.handleEvent(event, 'keyboard')` sets the focus view
**immediately** but defers the actual load through `ExplorerKeyboardNavScheduler` for
`EXPLORER_KEYBOARD_NAV_DEBOUNCE_MS = 500` (`features/words/utils/explorer-keyboard-nav.scheduler.ts:1`).
For those 500 ms `activeView()` is the new view, `status` is still `'success'` from the old one,
and the new view's data is `null` — no branch matches. **(derived, not reproduced in browser.)**

#### Exact affected files/components

- `features/words/pages/{roots,lemmas,stems,word-types}-explorer-page/*.component.html`
- `features/words/entity-detail-overlay/adapters/*-detail-overlay-adapter.component.html`
- `features/words/utils/explorer-keyboard-nav.scheduler.ts:1`
- `features/words/utils/explorer-table-focus-controller.ts` (`activeView` / `activeWordView` /
  `activeSurahView` computeds)
- `src/styles/_words-explorer-layout.scss:42-47`

#### Root cause

Two sources of truth for "which view is on screen", plus a `@switch` that assumes they can never
disagree.

#### User-visible impact

A second, narrower blank-panel path in addition to X-1. Lower severity because it is time-bounded
(500 ms) and only reachable via keyboard navigation across the table's count columns.

#### Recommended direction

- Add a terminal branch to both the `@switch` and the `@case('success')` chain that renders the
  view's skeleton (the `loading` branch already enumerates every view), so an unmatched
  combination degrades to "loading" rather than to nothing.
- Longer term, gate the content on **one** signal. `activeView()` exists to let the table's
  keyboard focus preview a view before committing; the *content* should follow the committed
  `panelState().view`, and only the table highlight should follow the focus preview.

#### Shared/global or page-specific

**Page-specific in code** (nine templates), but a single shared cause — the focus-controller
contract.

#### Risk

**LOW.** Adding a fallback branch cannot make any currently-working case worse. Re-pointing the
content at `panelState().view` is **MEDIUM** — it changes the deliberate keyboard-preview
behavior and needs a product decision.

---

## Unique Words Explorer

### U-1 — Details navigation uses 32% of its container and wraps a label onto two lines

- **ID:** U-1
- **Severity:** MEDIUM
- **Area:** Unique Words selected-word details header (`السور` / `لم يذكر فيها` / `الآيات`).
- **Classification: manifestation of shared finding X-2.** There is no Unique-Words-specific
  layout code involved.

#### Current behavior

The three-tab navigation shrink-wraps into a small strip at the start of the details header,
leaving most of the header empty. All three tabs are equal width, but that width is set by the
longest *word* rather than the longest *label*, so `لم يذكر فيها` wraps onto two lines and its tab
is half again as tall as its neighbours.

#### Evidence

**(measured)** Unique Words → row selected → drilldown panel:

```
.qd-details__tabs slot width : 666.0 px
tablist (.qd-tabs--segmented): 212.5 px  → 32% used, 453.5 px empty
tablist computed display     : inline-flex
per-tab box width            : 65.5 px (all three, flex: 1 1 0)

tab "السور"        single-line text width 28.5 px   box height 40 px
tab "لم يذكر فيها"  single-line text width 60.5 px   box height 60 px   ← wraps
tab "الآيات"       single-line text width 29.5 px   box height 40 px
```

#### Exact affected files/components

- `features/words/components/word-drilldown-modal/word-drilldown-modal.component.html:36-56` —
  the `<qd-tabs qdDetailsTabs>` block (3 tabs, `WORD_DRILLDOWN_VIEW_KEYS`,
  `features/words/models/unique-words.models.ts:114`)
- `features/words/components/word-drilldown-modal/word-drilldown-modal.component.scss` —
  `.word-drilldown-modal__tab` sets only `font-family` and `font-size`; **no local layout code**
- `src/styles/_components.scss:299` (`display: inline-flex`), `:307-311` (`--segmented`),
  `:380-383` (`flex: 1 1 0`)
- `src/styles/_components.scss:999-1003` — `.qd-details__tabs`, the 666 px slot

#### Root cause

X-2's `--segmented` branch: equal-width distribution applied to a shrink-to-fit `inline-flex` box.
The two-line label is the same rule seen from the intrinsic-sizing side — with `flex-basis: 0` and
`white-space: normal`, the container's intrinsic width resolves from each item's **min-content**
(the widest word), which for `لم يذكر فيها` is `يذكر`, hence 65.5 px.

#### User-visible impact

Exactly as reported: the navigation feels crammed into a corner of a wide panel, the items look
too narrow, and the longest label is visibly cramped and misaligned against its neighbours.

#### Recommended direction

No page-specific work. Fixing X-2 — equal-width tracks across the container, max 5 per row, with a
column floor sized from the longest label and single-line labels — resolves all three symptoms at
once. The active-state visual language (`.qd-tabs__tab.qd-is-selected`, green tint + inset thread,
`styles/_components.scss:368-373`) is untouched by that change and is preserved.

#### Shared/global or page-specific

**Shared** (X-2). Nothing here should be fixed inside `word-drilldown-modal`.

#### Risk

**LOW** as a consumer — this row gains from X-2 with no local change. The risk is X-2's.

#### Dependencies / related

Blocked by X-2. Not affected by X-1 (this modal does not project).

---

## Roots Explorer

### R-1 — Details navigation leaves 53% of the strip empty with unequal tabs

- **ID:** R-1
- **Severity:** MEDIUM
- **Area:** Roots details tabs (`الكلمات` / `الآيات` / `السور` / `الصيغ` / `الأصول`).
- **Classification: manifestation of shared finding X-2**, on the `--scrollable` side rather than
  the `--segmented` side.

#### Current behavior

Five content-sized tabs pack to the start of a 634 px strip, occupying 297.6 px. They are not
equal width. No scrollbar appears at this width, but the row is one long label away from
producing one.

#### Evidence

**(measured)** Roots explorer, desktop inline panel:

```
.qd-tabs class list : "qd-tabs qd-tabs--scrollable"   display: flex   overflow-x: auto
container           : 634.0 px   scrollWidth 634 == clientWidth 634 (no scrollbar today)
tabs                : الكلمات 66.5 | الآيات 55.5 | السور 54.5 | الصيغ 57.3 | الأصول 63.8
sum                 : 297.6 px  → 47% used, 336.4 px empty
```

#### Exact affected files/components

- `features/words/components/root-details-panel/root-details-panel.component.html:37-57`
- `features/words/components/root-details-panel/root-details-panel.component.scss` —
  `.root-details-panel__tab` sets only `font-family` / `font-size`; **no local layout code**
- `features/words/models/roots.models.ts:190` — 5 view keys, which is what selects `--scrollable`
- `src/styles/_components.scss:313-318, 331-350`

#### Root cause

X-2's count heuristic: 5 > `QD_TABS_SEGMENTED_MAX`, so the row becomes a `flex: 0 0 auto` scroll
strip instead of an equal-width distribution.

#### User-visible impact

The Roots header looks unfinished next to the (differently wrong) Unique Words header, and the
same five destinations are harder to hit than they need to be. At narrower widths or with longer
labels the same rule produces a horizontal scrollbar, which the brief forbids outright.

#### Recommended direction

None locally. X-2 gives this row 5 equal tracks across the full 634 px, which is exactly the
locked target for a 5-item navigation. Do **not** retain a Roots-specific tabs implementation —
there isn't one to retain; the divergence is entirely inside the shared primitive.

#### Shared/global or page-specific

**Shared** (X-2).

#### Risk

**LOW** as a consumer.

#### Dependencies / related

Blocked by X-2.

---

### R-2 — Details geometry collapses on every tab switch (and see X-1 for the blank)

- **ID:** R-2
- **Severity:** MEDIUM
- **Area:** Roots details content area.

#### Current behavior

Two distinct problems were reported together as "content disappears while the new data loads".
They have different causes and different fixes:

1. **The blank panel is X-1** — a content-projection lifecycle defect, permanent, triggered only
   by backward tab moves. See X-1.
2. **On a forward move the loading state is correct but the geometry is not.** The skeleton
   appears immediately with no blank frame, but the content box changes height dramatically
   between views.

This finding covers (2).

#### Evidence

**(measured)** Roots explorer, root `1`, forward tab moves, `.qd-explorer-subview-panel` height:

```
الآيات   (loaded)           570.5 px
→ الأصول (skeleton, t+0ms)  334.0 px   ← immediate skeleton, testid root-stems-list-loading
→ الأصول (loaded)          1422.0 px
```

The skeleton is present from the first sampled frame — **there is no blank window on the forward
path** — but the panel travels 570 → 334 → 1422 px in one interaction.

**(code)** The state layer is well-behaved and is **not** the cause:

- `AbstractDetailController.applyIdentity` (`features/words/state/abstract-detail.controller.ts`)
  keeps the previous panel data when the entity is unchanged (`sameIdentity` true) and only sets
  `status: 'loading'`. Previously loaded data is **not** discarded.
- `RootsDetailController.setView` (`roots-detail.controller.ts`) does the same via
  `_panel.update((s) => ({ ...s, view, status: 'loading' }))`.
- `RootsCache` (`state/roots-cache.ts`, extending `core/caching/api-response-cache.ts`) replays a
  cached response **synchronously** on subscribe, so a re-visited tab resolves inside the same
  change-detection tick — no skeleton flash, no second request. Caching is being used correctly.
- `DetailRequestLifecycle` (`state/detail-request-lifecycle.ts`) cancels both the summary and the
  detail request on every identity transition and re-checks `isCurrent(token)` in each callback,
  so late responses cannot write into the wrong panel.

**Classification requested by the brief:** for (1) **component remount / rendering**; for (2)
**rendering / loading UX**. Not API latency, not state reset, not a cache issue.

#### Exact affected files/components

- `features/words/pages/roots-explorer-page/roots-explorer-page.component.html` — the
  `@case('loading')` branch renders each view's own skeleton at that view's natural size
- `src/styles/_words-explorer-layout.scss:42-47` — `.qd-explorer-subview-panel { min-block-size: 0 }`,
  no height reservation of any kind
- `src/styles/_words-explorer-layout.scss:64-105` — `.qd-explorer-layout__panel` is given a fixed
  `block-size` at desktop, so the *panel* does not move; the collapse is inside it

#### User-visible impact

Every tab switch snaps the content box to a different height, the scrollbar appears and
disappears, and anything below the fold jumps. It reads as instability even when the data arrives
quickly.

#### Recommended direction

Reserve the content geometry across the loading transition instead of letting the skeleton size
itself. This is the same class of problem the Mushaf reader already solved twice (Feature 029 U1
and Feature 030 N3, documented in `features/mushaf/README.md`): hold the last known natural block
size while `status === 'loading'`, release it on settle. **That is now a third consumer**, which
is exactly the threshold the Mushaf README's decision N3-a named for extracting a shared utility
rather than porting the pattern a third time.

Do **not** add caching for this — the data layer already caches correctly, and the jump happens
on cache hits too.

#### Shared/global or page-specific

The symptom is page-specific (four explorer pages), the fix should be **shared** — a reservation
utility, per the N3-a threshold.

#### Risk

**LOW–MEDIUM.** A height reservation that outlives its load strands the panel at a stale height;
the Mushaf implementations guard this with a `ResizeObserver` that clears the reservation when the
inline size changes, and any shared extraction must carry that guard.

#### Dependencies / related

Distinct from X-1 — fixing X-1 does **not** fix this, and fixing this does not fix X-1. Both must
land for "switching tabs feels stable".

---

### R-3 — Roots modal / overlay uses the same tabs but a duplicated template

- **ID:** R-3
- **Severity:** LOW
- **Area:** Roots details modal (sub-`1080`) and the global entity detail overlay.

#### Current behavior

The modal and the overlay show the same navigation concept as the inline panel. Contrary to the
brief's expectation, they are **not** a second tabs implementation — all three paths render the
*same* `qd-root-details-panel` component and therefore the same `qd-tabs` instance. What is
duplicated is the **content** template around it.

#### Evidence

**(code)** `root-details-panel.component.html:88-113` — one component, three render paths
(`frameless()` → overlay, `inline()` → side panel, else → `qd-modal-shell`), all outletting the
same `#panelBody`. The tab strip is declared once at `:37-57`.

**(code)** `entity-detail-overlay/adapters/root-detail-overlay-adapter.component.html` reproduces
`roots-explorer-page.component.html`'s `#rootsPanelContent` almost line for line — same
`@switch (panelState().status)`, same six view branches, same sub-tab rows — differing only in
test ids and in reading `frame().view` instead of `activeView()`.

**(code)** Container sizing does legitimately differ and is already separated:
`.qd-modal-shell--overlay` (`shared/ui/modal-shell/modal-shell.component.scss`) vs
`.qd-explorer-layout__panel` (`styles/_words-explorer-layout.scss`).

#### Exact affected files/components

- `features/words/components/root-details-panel/root-details-panel.component.html:88-113`
- `features/words/entity-detail-overlay/adapters/root-detail-overlay-adapter.component.html`
  (and the four sibling adapters)

#### Root cause

Covered by X-5. The tab behavior is already shared; the content composition is not.

#### User-visible impact

None visually today beyond X-1/X-2, which affect all three paths equally. The cost is maintenance:
the overlay adapter carries its own copy of the same `@switch` gap described in X-7 (gated on
`frame().view` rather than `activeView()`, but with the same missing fallback).

#### Recommended direction

Confirm the tab behavior stays identical across the three paths — it already does, and X-2 keeps
it that way. Container sizing stays per-path. Address the content duplication under X-5, not here.

#### Shared/global or page-specific

**Shared** (X-5).

#### Risk

**LOW.**

#### Dependencies / related

X-5, X-7.

---

### R-4 — The empty header strip above the tabs (brief item 3E)

- **ID:** R-4
- **Severity:** LOW
- **Area:** `qd-details-workspace` header, in the frameless (overlay) render path.
- Counted under X-5's file set; listed separately because the brief asks for it explicitly.

#### Current behavior

Above the tabs there is an empty region bounded by a hairline. In the overlay/frameless path it
contains nothing at all.

#### Evidence

**(code)** `shared/ui/details-workspace/details-workspace.component.html:12-24` renders
`<header class="qd-details__header">` **unconditionally**, with an identity `<h2>` guarded by
`@if (identity())` and two projection slots.

**(code)** `styles/_components.scss:967-974` — `.qd-details__header { padding: var(--qd-card-padding);
border-block-end: var(--qd-hairline) }`. With `--qd-card-padding: var(--qd-s-16)` that is a 32 px
tall empty box plus a rule.

**(code)** In the overlay adapters `[frameless]="true"`, and
`root-details-panel.component.html:16-35` skips both the `qdDetailsMetadata` block and the close
button. `[identity]` is bound to `frameless() ? '' : …`, so the `<h2>` is skipped too. The header
renders with **no children that have size**.

**(code)** Someone already noticed: `styles/_explorer-detail-lists.scss:61-62` hides it —
`.root-details-panel--frameless .qd-details__header, .lemma-details-panel--frameless
.qd-details__header { display: none }` — but only for **two** of the four frameless panels. Stems,
Word Types and the word-drilldown frameless path keep the empty strip.

**(code)** The second rule the brief describes is the nested surface border: `.qd-modal-shell`
(border + `--qd-radius-lg`) directly wraps `.qd-details__shell`
(`styles/_components.scss:957-965`, border + `--qd-radius-md`) because the modal is opened with
`[flushBody]="true"`. `.qd-modal-shell__header--bare` correctly collapses to `padding: 0;
border-block-end: 0` when `showTitle` and `showClose` are both false, so the modal's own header is
**not** the culprit — the two visible rules are the modal border and the details-shell border.

#### Root cause

`qd-details-workspace` has no "no header" mode. Consumers that legitimately have no identity, no
metadata and no actions still get a padded, bordered header box, and the workaround was applied as
a per-consumer `display: none` in a global stylesheet — for two consumers out of four.

#### User-visible impact

Dead vertical space and a meaningless divider at the top of the Stems, Word Types and Unique Words
overlays; inconsistent with the Roots and Lemmas overlays, which hide it.

#### Recommended direction

Give the header a real absence condition in the primitive — render `<header>` only when it has
something to show — and delete the two `display: none` workarounds. Separately, decide whether a
`qd-details-workspace` nested directly inside a flush `qd-modal-shell` should keep its own border
and radius; a `variant` that drops them would remove the double outline. **Do not remove the
header where it carries the identity, the panel label, or the close button** — that is the inline
and modal path, where it is doing real work.

#### Shared/global or page-specific

**Shared** (`shared/ui/details-workspace` + `styles/_components.scss`), with two dead workarounds
to delete in `styles/_explorer-detail-lists.scss`.

#### Risk

**LOW.** The header is presentational; the `aria-labelledby` on `.qd-details__shell` is already
guarded by `@if (identity())` and resolves to `null` in exactly the cases where the header would
be dropped.

---

## Lemmas Explorer

### L-1 — Every Lemmas details tab is 100% of the container, forcing a 4× horizontal scrollbar

- **ID:** L-1
- **Severity:** HIGH
- **Area:** Lemmas details tab header.
- **Classification: genuine page-specific defect** layered on top of X-2. This is the one place
  where a page really did diverge.

#### Current behavior

The Lemmas details tab header has a permanent internal horizontal scrollbar. Only the first tab
(`الكلمات`) is visible; it fills the entire strip. The other three are scrolled entirely out of
view.

#### Evidence

**(measured)** Lemmas explorer, details header, no row selected (the tabs render disabled but
present, so this is not selection-dependent):

```
.qd-tabs class list : "qd-tabs qd-tabs--scrollable"   display: flex   overflow-x: auto
clientWidth  : 634 px
scrollWidth  : 2550 px          →  4.02× overflow, permanent scrollbar

tab "الكلمات"  x =   209.5   width 634.5   inline-size 634.453px
tab "الآيات"   x =  -429.0   width 634.5   inline-size 634.453px
tab "السور"    x = -1067.4   width 634.5   inline-size 634.453px
tab "الأصول"   x = -1705.9   width 634.5   inline-size 634.453px

computed flex on every tab : 0 0 auto
```

#### Exact affected files/components

- `features/words/components/lemma-details-panel/lemma-details-panel.component.scss:33-41` —
  ```scss
  .lemma-details-panel__tab {
    inline-size: 100%;        // ← the defect
    justify-content: center;
    overflow: hidden;
    …
  }
  ```
  The three sibling panels (`root`, `stem`, `word-type`) do **not** have this declaration.
- `src/styles/_components.scss:313-318` — `.qd-tabs--scrollable { display: flex; overflow-x: auto }`
- `src/styles/_components.scss:335` — `.qd-tabs__tab { flex: 0 0 auto }`
- `features/words/models/lemmas.models.ts:181` — 4 view keys, which selects `--scrollable`

#### Root cause

`inline-size: 100%` on a flex item whose flex basis is `auto` and whose `flex-grow`/`flex-shrink`
are `0` resolves each tab to **100% of the flex container's width**. Four tabs therefore need
4 × 634 px = 2536 px (2550 with gaps) inside a 634 px scroller.

The declaration was almost certainly an attempt to solve exactly the problem X-2 describes — "make
the tabs fill the container" — written against `--segmented`'s `flex: 1 1 0` intuition. Under
`--scrollable`'s `flex: 0 0 auto` it does the opposite. The `overflow-x: auto` on the tablist is
what turns the mistake into a scrollbar instead of a visible overflow.

#### User-visible impact

Three of the four Lemmas details destinations are unreachable without discovering and using a
horizontal scrollbar inside a tab header. This is the most severe individual UI defect in the
report.

#### Recommended direction

The correct fix is **not** local. Delete `inline-size: 100%` (and, once X-2 lands, the
`justify-content: center` / `overflow: hidden` / `text-overflow` / `white-space` block too, since
the shared mode will own alignment and truncation). Then let X-2 give the row 4 equal tracks
across the full 634 px — which is what the declaration was reaching for in the first place.

Deleting the declaration **without** X-2 leaves Lemmas looking like Roots (R-1): packed to the
start with ~50% empty. That is still strictly better than a scrollbar and is a safe interim state
if the two changes cannot land together.

#### Shared/global or page-specific

**The defect is page-specific; the correct fix belongs in the shared primitive (X-2).** The local
declaration should be deleted, not adjusted.

#### Risk

**LOW** to delete the declaration (it can only improve the current state). The risk sits with X-2.

#### Dependencies / related

X-2. Same panel is also subject to X-1 (blank on backward tab move) and X-3 (ayah clipping).

---

## Shared Words Details Loading (brief item 5)

Audited code-first across `features/words/state/*`. The result is largely a **negative** finding,
which is worth recording explicitly so it is not re-investigated.

### What was checked, and what the code shows

| Concern from the brief | Finding | Classification |
| --- | --- | --- |
| API call on each tab switch | Yes, one per view — but only on a cache miss. `RootsDetailViewLoader.loadActiveView` routes to exactly one endpoint per view. | **intentional lazy loading** |
| Duplicate / repeated requests | None found. `ApiResponseCache.getOrLoad` (`core/caching/api-response-cache.ts`) de-duplicates in-flight requests via `shareReplay({ refCount: true })` and holds a 48-entry LRU. | — |
| Cache lookup behavior | Correct. A cached entry is replayed **synchronously** on subscribe, so a revisited tab settles inside the same change-detection tick — no skeleton, no request. | — |
| Cache keys | Correct and complete: `roots:{id}:{view}[:{subview}][:p{page}]` (`state/roots-cache.ts`), mirrored in `lemmas-cache.ts`, `stems-cache.ts`, `word-types-cache.ts`, `unique-words-cache.ts`. Summary reads share one key across the side panel and the overlay, so they dedupe with each other. | — |
| State resets | Previously loaded data is **not** discarded on a view change. `AbstractDetailController.applyIdentity` keeps `...s` and only flips `status`. A full reset happens only when the *entity* changes, which is correct. | — |
| Component recreation | The panel components are not recreated on a tab switch — but the **projected content is destroyed and not restored** on a backward move. That is X-1, and it is the single real defect in this area. | **component remount / rendering** |
| Signal / effect chains | No effect loops found. The controllers write signals imperatively from RxJS callbacks guarded by `DetailRequestLifecycle.isCurrent(token)`. | — |
| URL-state reactions | `AbstractRouteDetailFacade.bindToRoute` applies `distinctUntilChanged(urlStatesEqual)` over the complete identity, and `applyUrlState` short-circuits on an unchanged identity, so the router round-trip after an in-component `setView` does not re-issue the load. | — |
| Loading gates | Present and per-view: each page's `@case('loading')` renders that view's own skeleton. Gaps are X-7 (missing fallback), not missing skeletons. | — |
| Unnecessary sequential operations | One real serialization: on a **new entity**, `loadSummaryAndRestore` awaits the summary response before `loadActiveView` runs, so the view read is chained behind the summary read. It is a genuine dependency only in that `applySummary` populates the header — the view endpoints take `rootId` from the URL state, not from the summary. | **frontend orchestration** — see note below |
| Old content cleared before new is available | No, for the data. Yes, for the DOM — X-1. | — |
| Virtual scrolling delaying render | Not implicated. Virtual scroll exists only in `shared/ui/data-table` (the list tables), not in any details list. The detail lists render all rows. | — |

### The one orchestration observation worth carrying forward

`AbstractDetailController.loadSummaryAndRestore` chains the active-view request behind the summary
request. On a cold selection that is two sequential round trips where the second does not
technically depend on the first. It is **not** currently a user-visible problem worth acting on —
`RootsCache` makes the summary a cache hit for any entity the user has already touched, and the
per-request cost in this environment is single-digit milliseconds — but if perceived latency is
revisited after the locked decision to remove `devApiLatencyMs` (see the Mushaf report), this is
where the remaining frontend serialization lives.

**Insufficient evidence** for the brief's premise that details data "feels slow after the recent
UI refactor" as a *data* problem: measured request behavior is correct, cached, deduplicated and
parallel-where-independent. The felt slowness is much more likely to be X-1 (content that never
arrives), R-2 (geometry that jumps), and the dev-only 450 ms interceptor documented in the Mushaf
report. **No preloading should be introduced.** Lazy stays lazy.

---

## Access Management

### A-1 — Removal surface for سجل الوصول and الأمان المتقدم

- **ID:** A-1
- **Severity:** HIGH (product decision already locked; the risk is in getting the surface right)
- **Area:** `features/access-admin`.

#### Current behavior

The Access Management page has three tabs — `workspace` (مساحة العمل), `audit` (سجل الوصول),
`security` (الأمان المتقدم) — selected by a `?tab=` query parameter. Both the audit log and the
owner-reconciliation status are fetched **on page load regardless of which tab is active**.

#### Evidence

**(code)** `features/access-admin/models/access-admin-tabs.ts:1` —
`ACCESS_ADMIN_TAB_KEYS = ['workspace', 'audit', 'security']`.

**(code)** `features/access-admin/state/access-admin.facade.ts:120-127`:

```ts
await Promise.all([
  this.loadUsers(),
  this.loadPermissionCatalogue(),
  this.loadAuditEvents(),          // ← audit tab data, fetched eagerly
  this.loadReconciliationStatus(), // ← security tab data, fetched eagerly
]);
```

**(code)** `features/access-admin/pages/access-admin-page/access-admin-page.component.html` —
`@case('audit')` and `@case('security')` blocks; `@default` is the workspace.

This finding was **not** verified in the browser: the page requires an owner session and the audit
run was anonymous.

#### Root cause

Not a defect — a scope reduction. Recorded here because the deletion surface crosses eight files
and two of the removals also delete work from the page's critical path.

#### User-visible impact

After removal: two navigation destinations disappear, and the Access page stops issuing two
requests it currently makes on every load.

#### Recommended direction

Enumerated in *Confirmed Frontend Removal Candidates* below.

#### Shared/global or page-specific

**Page-specific**, entirely inside `features/access-admin/`. No shared component is deleted.

#### Risk

**MEDIUM.** Two ordering hazards: (a) `busyAction` is a union type that includes
`'relink-preview'` and `'relink-confirm'`, consumed by workspace components that must keep
compiling; (b) removing two of the four `Promise.all` legs changes the page's load timing and the
`accessStateKnown()` gate — worth a targeted check that the workspace still gates correctly.

#### Dependencies / related

Independent of every other finding in this report.

---

## Abwab

### B-1 — Sections, search and view controls share one wrapping flex row

- **ID:** B-1
- **Severity:** MEDIUM
- **Area:** Abwab management page toolbar.

#### Current behavior

One `.qd-toolbar` flex row contains, in order: the section/category tab strip, the search field,
and the Tree/Cards toggle. All three wrap against each other as the viewport narrows. The sections
are rendered as `qd-tabs` chips with count badges, not as a grid of consistently sized items.

#### Evidence

**(code)** `features/abwab/components/abwab-toolbar/abwab-toolbar.component.html` — a single
`<div class="abwab-toolbar qd-toolbar">` holding `<qd-tabs class="… qd-toolbar__identity">`
(sections), `<div class="abwab-toolbar__search qd-toolbar__filters">`, and
`<div class="abwab-toolbar__view-toggle qd-toolbar__actions">`.

**(code)** `styles/_components.scss:855-865` — `.qd-toolbar { display: flex; flex-wrap: wrap; … }`.

**(measured)** Abwab page, anonymous (2 sections visible):

```
.abwab-toolbar      width 1520 px   height 126.8 px   flex-wrap: wrap
qd-tabs host        display: block          (the .abwab-toolbar__tabs flex-wrap is inert — X-4)
inner tablist       "qd-tabs qd-tabs--segmented"   display: inline-flex   flex-wrap: nowrap
tablist width       246.7 px        2 section tabs × 117.3 px
```

**(derived)** With more than three sections the tablist flips to `--scrollable`
(`QD_TABS_SEGMENTED_MAX = 3`), which sets `overflow-x: auto` on the section strip — i.e. the
sections area gains its own horizontal scroller, which the locked target forbids.

#### Exact affected files/components

- `features/abwab/components/abwab-toolbar/abwab-toolbar.component.html` (whole file)
- `features/abwab/components/abwab-toolbar/abwab-toolbar.component.scss` —
  `.abwab-toolbar { align-items: flex-end }`, `.abwab-toolbar__tabs { flex-wrap: wrap }` (inert),
  `.abwab-toolbar__search { flex: 1 1 0 }`, `.abwab-toolbar__view-toggle { flex: none }`
- `features/abwab/pages/abwab-page/abwab-page.component.html:74-86` — the single
  `<qd-abwab-toolbar>` placement
- `src/styles/_components.scss:855-895` — `.qd-toolbar` and its slot classes
- `src/styles/_components.scss:298-329` — the `qd-tabs` modes

#### Root cause

The sections were modelled as **tabs inside a toolbar** rather than as a **navigation grid below
a toolbar**. `.qd-toolbar`'s slot vocabulary (`__identity` / `__filters` / `__actions`) is a
single-row contract, so putting a variable-length collection into `__identity` makes that
collection compete for width with the controls beside it.

#### User-visible impact

Section chips, search and view controls jostle for the same horizontal space; section item widths
vary with label length; as sections are added the strip either pushes the search field onto its
own row or (past three sections) starts scrolling horizontally.

#### Recommended direction

Split the area into the two rows the brief specifies:

- **Row 1** — a `.qd-toolbar` containing only search (`__filters`) and the Tree/Cards toggle
  (`__actions`). This is exactly what `.qd-toolbar` is for, and the existing slot classes already
  fit.
- **Row 2+** — the sections as a **responsive grid** in their own block below the toolbar:
  `display: grid; grid-template-columns: repeat(auto-fill, minmax(<floor>, 1fr))`, with items
  stretched to a consistent block size. Wrapping is then automatic, the column count follows the
  width, no horizontal scroller exists, no nested scroller is introduced, and the page simply
  grows taller as rows are added.

Reusing an existing primitive is possible but should be checked rather than assumed: `.qd-grid`
(`styles/_layout.scss:128-138`) is an `auto-fit`/`minmax` grid driven by `--qd-grid-min` /
`--qd-grid-max` / `--qd-grid-columns` tokens and is the closest fit; it would need a sections
variant with its own token trio, following the existing `--destinations` / `--doors` pattern. If
the sections keep their `role="tab"` semantics, the grid must be applied to the tablist element,
which reinforces X-2's recommendation that the tabs primitive own a wrapping grid mode — in which
case Abwab can consume it directly instead of getting its own grid.

Business behavior (section selection, counts, search scoping, archive mode) is untouched by this;
it is a placement and layout change only.

#### Shared/global or page-specific

**Page-specific placement**, but the sections-grid mechanism should come from X-2's wrapping mode
rather than a new Abwab-only grid.

#### Risk

**MEDIUM.** `hideSectionControls()` currently hides *both* the section strip and the view toggle
in archive mode from one condition; splitting the rows means that condition has to be applied in
two places, and getting it wrong would leave an empty row in the archive view. The
`qd-tabs__count` badges also carry the M-1 geometry problem from the Mushaf report — the count
values change as doors are added, so an equal-track grid is what stops the section row from
reflowing.

#### Dependencies / related

X-2 (wrapping grid mode), X-4 (delete the inert `flex-wrap`), and M-1 in the Mushaf report (the
count badge is the other `qd-tabs__count` consumer).

---

### B-2 — Section count badges reflow the section strip

- **ID:** B-2
- **Severity:** LOW
- **Area:** Abwab toolbar section tabs.

#### Current behavior

Each section tab carries a `.qd-tabs__count` badge whose width follows its digit count. Because
the tabs are content-sized, changing a count changes the strip's geometry.

#### Evidence

**(code)** `features/abwab/components/abwab-toolbar/abwab-toolbar.component.html:14-15, 32-33` —
`<span class="qd-tabs__count" [class.qd-tabs__count--empty]="rootCountFor(section.id) === 0">`.
Note this consumer keeps the element mounted and only changes its appearance when the count is
zero — which is the **correct** pattern, and the one recommended in the Mushaf report's M-1.

**(code)** `styles/_components.scss:391-403` — `.qd-tabs__count { min-inline-size: 1.25rem;
padding-inline: 0.35rem; font-variant-numeric: tabular-nums }`. The 1.25rem floor fits one digit;
two- and three-digit counts are wider.

#### Root cause

Same as M-1's residual: the count slot has no width that accommodates its largest realistic value.

#### User-visible impact

Minor: section chips shift as door counts change. Invisible at rest, visible after a create or
delete.

#### Recommended direction

Handled by X-2 — equal grid tracks make the badge width irrelevant to the strip's geometry. If
X-2 is deferred, widening `.qd-tabs__count`'s `min-inline-size` to fit the largest expected count
(`tabular-nums` is already set, so a `ch`-based floor is exact) fixes it for both this consumer and
the Mushaf ayah-study strip.

#### Shared/global or page-specific

**Shared** (`.qd-tabs__count`, two consumers: `abwab-toolbar` and the Mushaf ayah-study tabs).

#### Risk

**LOW.**

#### Dependencies / related

M-1 in `docs/ui-polish-audit-mushaf-reader.md` — the same slot, the same fix.

---

## Navbar

### N-1 — Dropdowns anchored in the actions cluster open off the viewport edge

- **ID:** N-1
- **Severity:** MEDIUM
- **Area:** Top navbar desktop dropdown menus.

#### Current behavior

Navbar dropdown menus are positioned by hand-written CSS that always aligns the menu's
inline-start edge to the trigger and lets it grow toward the inline-end. For a trigger in the
actions cluster — which in RTL sits at the **left** edge of the viewport — the menu grows further
left, past the edge of the usable page.

#### Evidence

**(code)** `styles/_components.scss:1243-1246, 1276-1295`:

```scss
.qd-nav__item { position: relative; }
.qd-nav__menu {
  position: absolute;
  inset-block-start: 100%;
  inset-inline-start: 0;      /* ← fixed alignment, no edge awareness */
  min-inline-size: 12rem;     /* 192px */
  max-inline-size: 20rem;     /* 320px */
  …
}
```

**(code)** `core/navigation/nav-items.ts:22` — `settings` is `group: 'actions'`;
`core/navigation/nav-menu.ts:26-35` gives it one child (`إدارة الوصول`), so `hasMenu()` is true and
it renders a `.qd-nav__menu`. `core/layout/top-navbar/top-navbar.component.html:26-37` places the
actions `qd-app-navigation` inside `.qd-navbar__actions`, after `.qd-navbar__spacer`.

**(measured)** Live navbar, RTL, `clientWidth = 1905`:

```
document direction        : rtl
.qd-navbar__actions       : x = 40.0   width = 162.6   (right edge at 202.6)
[data-testid=nav-more-trigger] : x = 1195.3  width = 69.0
```

**(derived)** The settings trigger sits inside the actions cluster, so its box lies within
`x ∈ [40, 202.6]`. With `inset-inline-start: 0` in RTL the menu's **right** edge pins to the
trigger's right edge and the box extends leftward by 192–320 px. Taking the cluster's own right
edge (202.6) as the most favourable case, the menu's left edge lands between `202.6 − 192 = 10.6`
and `202.6 − 320 = −117.4`; anchored to the actual trigger (further left inside the cluster) it
overflows by roughly **92–220 px**. The `المزيد` trigger at `x = 1195.3` is safe by the same
arithmetic (`1264.3 − 320 = 944`), which matches the report that only the edge-adjacent menu
misbehaves.

The settings menu itself could not be rendered — it is gated on `isActiveOwner()`
(`top-navbar.component.ts:206`) and the audit session was anonymous. The geometry above is
measured; the overflow is arithmetic on that geometry.

#### Exact affected files/components

- `src/styles/_components.scss:1276-1295` — `.qd-nav__menu` positioning
- `src/styles/_components.scss:1243-1246` — `.qd-nav__item { position: relative }`, the anchor
- `src/app/core/layout/app-navigation/app-navigation.component.html:41-51` — the `<ul class="qd-nav__menu">`
- `src/app/core/layout/top-navbar/top-navbar.component.html:26-37` — the actions cluster
- `src/app/core/navigation/nav-menu.ts:26-35` — settings gains a child, so it becomes a menu

#### Root cause

**The navbar is the one dropdown in the product that does not use the shared floating-layer
placement.** `shared/ui/floating-layer/floating-layer-placement.ts` already implements exactly
what is needed:

```ts
const preferredLeft = direction === 'rtl' ? anchor.right - size.width : anchor.left;
const maxLeft = viewport.width - size.width - FLOATING_VIEWPORT_MARGIN;
const left = clamp(preferredLeft, FLOATING_VIEWPORT_MARGIN, Math.max(FLOATING_VIEWPORT_MARGIN, maxLeft));
```

— RTL-aware preferred alignment, an 8 px viewport margin, block-axis flipping, a measured
`maxBlockSize`, and an `inlineClamped` flag it reports back. It is consumed by
`mushaf/source-selector`, `mushaf/surah-jump-picker`, `words/explorer-association-filter` and
`shared/ui/context-menu` — but not by `qd-nav__menu`, which predates it and stayed on static CSS.

So this is not a missing capability; it is one consumer that opted out.

#### User-visible impact

The Settings menu — the only route to Access Management — renders partly outside the usable
viewport in RTL, which is the product's default direction. Depending on width, its content is cut
off or unreachable.

#### Recommended direction

Move `.qd-nav__menu` onto the existing `qdFloatingLayer` placement rather than adding a second
positioning system. That gives edge collision handling, RTL correctness, block-axis flipping and
the max-height clamp in one step, and removes the hand-written `position: absolute` /
`inset-inline-start: 0` / `max-block-size` trio from `_components.scss`.

If a full migration is judged too large for a polish pass, the minimum correct interim is to make
the alignment edge-aware — menus anchored in the actions cluster must align to `inset-inline-end`
so they grow **inward** — but that is a second implementation of logic that already exists, and it
would not handle the block axis or the max-height. Prefer the migration.

#### Shared/global or page-specific

**Shared/global** — the navbar renders on every route, and the fix consolidates onto an existing
shared primitive.

#### Risk

**MEDIUM.** `qdFloatingLayer` brings its own focus management, keyboard handling and dismissal
semantics (`floating-layer-focus.ts`, `floating-layer-keyboard.ts`), which differ from the
navbar's current hover-open / pointer-leave behavior
(`onMenuPointerEnter` / `onMenuPointerLeave` in `top-navbar.component.ts`). The hover-to-open
interaction must be preserved or deliberately changed — it should not change by accident. The
directive also switches the element to `position: fixed`, which interacts with the sticky navbar's
`z-index` (`--qd-z-mobile-nav`).

#### Dependencies / related

Independent of everything else in this report.

---

### N-2 — Other navbar dropdowns share the same positioning, and one is already close to the edge

- **ID:** N-2
- **Severity:** LOW
- **Area:** Top navbar.

#### Current behavior

`.qd-nav__menu` is a single class used by every desktop navbar dropdown: `الكلمات والجذور`,
`الأبواب`, `المزيد`, and `الإعدادات`. All four inherit N-1's fixed alignment; only the actions
cluster currently overflows.

#### Evidence

**(measured)** `[data-testid=nav-more-trigger]` at `x = 1195.3`, `width = 69.0` in a 1905 px
viewport — its menu extends left to at worst `1264.3 − 320 = 944.3`, comfortably inside.

**(derived)** The margin shrinks as the viewport narrows or as more primary items are added: the
`المزيد` trigger moves toward the inline-end of the primary cluster, and the Wide band starts at
1080 px (`styles/_breakpoints.scss`), where the same 320 px menu has proportionally less room.

#### Exact affected files/components

Same as N-1.

#### Root cause

Same as N-1.

#### User-visible impact

None today. Recorded so that fixing N-1 is scoped to the shared class rather than to the settings
item, which would leave the same latent defect in three other menus.

#### Recommended direction

Fix N-1 at `.qd-nav__menu`, not at the settings item.

#### Shared/global or page-specific

**Shared.**

#### Risk

**LOW.**

#### Dependencies / related

N-1.

---

## Shared Component / Primitive Candidates

Only evidence-backed candidates are listed. Two of the three are consolidations of existing
duplication; the third is a *correction* to an existing primitive rather than a new one.

### C-1 — Details Navigation / Tabs: correct `qd-tabs`, do not build a second primitive

- **Current duplicate implementations:** **none in the sense the brief anticipated.** All details
  navigations already use the same `shared/ui/tabs` primitive from byte-identical templates. What
  is duplicated is (a) the surrounding *composition* — see C-2 — and (b) two local CSS
  workarounds that exist only because the primitive has no correct mode:
  `.lemma-details-panel__tab { inline-size: 100% }` (L-1) and the inert `flex-wrap` declarations
  on `.qd-explorer-subtabs` / `.abwab-toolbar__tabs` (X-4).
- **Proposed responsibility:** `qd-tabs` owns tab-strip layout as a **declared contract**, not a
  count heuristic — equal-width tracks, container-filling, max 5 per row, wrapping instead of
  scrolling, responsive column count, single-line labels with a min column floor, and a stable
  active state. It keeps its existing roving-focus keyboard model and ARIA wiring unchanged.
- **Consumers (19 templates today):** Unique Words drilldown, Roots details, Lemmas details, Stems
  details, Word Types details, Word Types table-view tabs, the six `.qd-explorer-subtabs` rows
  (Roots/Lemmas/Stems pages + their overlay adapters), `unique-words-tabs`, `abwab-toolbar`
  sections, `abwab-move-picker`, `abwab-relations-modal`, `access-admin-page`, and the Mushaf
  ayah-study strip.
- **What remains page-specific:** the tab key list and labels, disabled predicates, count badge
  content, empty/error copy, and which panel a tab controls. Nothing about placement or width.
- **Consolidation benefit:** resolves U-1, R-1, L-1, B-1's section strip, B-2 and M-1's secondary
  contributor in one change; deletes two local workarounds; and removes `overflow-x` from tab
  strips product-wide, which is the brief's strict rule.
- **Consolidation risk:** highest blast radius in the report — every tab row in the product moves
  at once, including Compact-band density and the Mushaf ayah-study strip. Must pass
  `npm run check:golden-ui`. See X-2 for the specific interactions.

### C-2 — Details panel shell (header + tab strip + tabpanel + content container)

- **Current duplicate implementations:** four byte-identical templates —
  `root-details-panel`, `lemma-details-panel`, `stem-details-panel`, `word-type-details-panel`
  (identical at lines 1-3, 37, 68, 80), plus near-identical SCSS. `word-drilldown-modal` is a
  fifth, structurally similar but not identical, and correctly diverges by not projecting.
- **Proposed responsibility:** one shell owning the three render paths (frameless / inline /
  modal), the `qd-details-workspace` composition, the tab loop, the single content container that
  replaces the five-section projection (X-1), the per-instance ARIA ids, and the close/escape
  wiring.
- **Consumers:** the four Words details panels; possibly `word-drilldown-modal` as a fifth if its
  per-view rendering can be expressed through the same content container.
- **What remains page-specific:** the view key list, labels and aria strings, the disabled
  predicate, the empty/not-found copy, and the entity's content template.
- **Consolidation benefit:** X-1 and R-4 stop being four fixes each; future defects stop
  multiplying by four.
- **Consolidation risk:** MEDIUM — ARIA id generation and the documented
  `.qd-explorer-subview-panel` id contract (`features/words/README.md:173`) must survive, and the
  overlay and side panel can be on screen simultaneously, so per-instance ids are load-bearing.
- **Explicitly out of scope:** the page-level `@switch` content blocks. They look similar but
  carry entity-specific view keys and list components; unifying them would push domain names into
  a shared component, which `FRONTEND_UI_RULES.md` §1 prohibits.

### C-3 — Loading geometry reservation utility

- **Current duplicate implementations:** two, both in Mushaf and both documented as deliberate
  local ports — `selected-word-section` (Feature 029, U1) and `selected-ayah-section`
  (Feature 030, N3 row 10, decision **N3-a: extract a shared utility only on a third consumer**).
- **Proposed responsibility:** hold the last known natural block size of a content region while it
  is loading, release on settle, and invalidate on an inline-size change — the guarded
  `ResizeObserver` + numeric-geometry-only contract both existing implementations already follow.
- **Consumers:** the two Mushaf sections, plus the Words details content area (R-2) as the third
  — which is precisely the threshold N3-a named.
- **What remains page-specific:** the per-band baseline floor, and the decision of which element
  is the reservation host.
- **Consolidation benefit:** R-2 is fixed with a reviewed implementation rather than a third
  hand-rolled port; the two Mushaf consumers converge on one behavior.
- **Consolidation risk:** LOW–MEDIUM. The invalidation guard is the subtle part (a reservation
  that outlives its load strands the panel at a stale height), and the Mushaf README records an
  accepted trade-off — reserving the previous entity's height while a *different* entity loads —
  that a shared utility inherits and must state.

### Candidates deliberately **not** proposed

- **A dropdown/menu positioning primitive.** One already exists and is correct
  (`shared/ui/floating-layer`). N-1 is a migration, not an extraction.
- **A shared "explorer content `@switch`" component.** The blocks are similar in shape but
  entity-specific in substance; see C-2's out-of-scope note.
- **A shared ayah-list component.** `ayah-matches-list` is already the single shared
  implementation across nine consumers. X-3 is a defect in it and in the primitives it composes,
  not a duplication.

---

## Confirmed Frontend Removal Candidates

Per the locked product decision: **remove سجل الوصول and الأمان المتقدم from the frontend; the
Workspace remains.**

### A. Confirmed frontend deletion

**Audit tab (سجل الوصول)**

| Surface | Path |
| --- | --- |
| Component | `features/access-admin/components/access-audit-log/access-audit-log.component.{ts,html,scss}` |
| State | `features/access-admin/state/access-audit.store.ts` |
| API method | `AccessAdminApi.listAuditEvents` — `data-access/access-admin.api.ts:69-73` (+ its `auditParams` helper) |
| Facade fields | `access-admin.facade.ts` — `audit` (`:53`), `auditEvents`/`auditNextCursor`/`auditQuery`/`auditLoading`/`auditError`/`auditAppending`/`auditAppendError`/`auditAppendedCount` (`:98-105`) |
| Facade methods | `updateAuditQuery` (`:383`), `loadNextAuditPage` (`:388`), `loadAuditEvents` (`:396`), the `findUsers` audit delegation (`:188`) |
| Facade wiring | the `this.loadAuditEvents()` leg of `Promise.all` in `load()` (`:124`) |
| Page template | the whole `@case ('audit')` block in `pages/access-admin-page/access-admin-page.component.html` |
| Page component | `auditTargetSearch()`, `auditActorSearch()`, `applyAuditFilters`, `loadNextAuditPage`, `searchAuditTarget`, `searchAuditActor` |
| Labels | `auditAppendedAnnouncement` (`models/access-admin.labels.ts:19`), `auditActionType` (`:47`), the `AUDIT_ACTION_TYPE_LABELS` map, and the `if (tab === 'audit')` branch of `labels.tab` (`:25`) |
| Models | `AccessAuditQuery`, `AccessAuditEventPage` and audit-only types in `models/access-admin.models.ts` |

**Advanced security tab (الأمان المتقدم)**

| Surface | Path |
| --- | --- |
| Components | `components/access-advanced-security/*`, `components/access-owner-reconciliation/*` |
| API methods | `previewRelink` (`data-access/access-admin.api.ts:75`), `confirmRelink` (`:85`), `getOwnerReconciliationStatus` (`:95`) |
| Facade fields | `reconciliationState`/`reconciliationLoadingState`/`reconciliationErrorState` (`:62-64`), `relinkPreviewState`/`relinkEvidenceTokenState` (`:65-66`), `relinkPreviewRequestVersion` (`:71`), and the public `reconciliationStatus`/`reconciliationLoading`/`reconciliationError`/`relinkPreview` readonlys (`:106-109`) |
| Facade methods | `previewSelectedUserRelink` (`:309`), `confirmSelectedUserRelink` (`:355`), `cancelSelectedUserRelink` (`:376`), `loadReconciliationStatus` (`:404`), `invalidateRelinkPreviewRequest`, `isCurrentRelinkPreviewRequest` |
| Facade wiring | the `this.loadReconciliationStatus()` leg of `Promise.all` in `load()` (`:125`) |
| Page template | the whole `@case ('security')` block |
| Page component | `workflowResetToken()`, `previewRelink`, `confirmRelink`, `cancelRelink` |
| Labels | `reconciliationCandidateState` (`:49`), the `RECONCILIATION_CANDIDATE_STATE_LABELS` map, and the `if (tab === 'security')` branch of `labels.tab` (`:28`) |
| Models | `AccessRelinkPreviewRequest`, `AccessRelinkConfirmRequest`, and the generated re-exports `LogtoSubjectRelinkPreview`, `OwnerReconciliationStatus`, `PreviewLogtoSubjectRelinkBody`, `ConfirmLogtoSubjectRelinkBody` (imports only — do **not** delete generated model files) |

**Nothing in the shared layer is deleted by any of the above.**

### B. Verify before delete

| Item | Why it needs verification |
| --- | --- |
| `models/access-admin-tabs.ts` — the whole file | With only `workspace` left, `ACCESS_ADMIN_TAB_KEYS`, `AccessAdminTab`, `parseAccessAdminTab` and `DEFAULT_ACCESS_ADMIN_TAB` become a one-element enum. Removing them removes the type used across the labels and the page. |
| The `<qd-tabs>` block in `access-admin-page.component.html` | A one-tab tablist is meaningless UI. Confirm no design intent to add a fourth tab before deleting the strip. |
| `activeTab` signal, `selectTab` (`access-admin-page.component.ts:202`), `showTab` (`:213`), and the `route.queryParamMap` subscription at `:134-136` | Dead once the strip goes, but they own the `?tab=` URL contract. |
| The `?tab=` query parameter | A shareable URL contract. Confirm no bookmark/deep-link or e2e journey depends on `?tab=audit` / `?tab=security` before dropping the parser — a stale link should degrade to the workspace, which `parseAccessAdminTab`'s `?? DEFAULT` already does, so keeping the parser is the safer option even after the strip is removed. |
| `busyAction` union members `'relink-preview'`, `'relink-confirm'` | Consumed by workspace components (`access-permission-editor`, `access-lifecycle-actions`, `access-change-review`) through `[busyAction]`. Narrowing the union must not break their template checks. |
| `AccessAdminApi.findUsers` / audit-backed user search (`facade:188`) | Delegates to `AccessAuditStore.findUsers`. Confirm whether the **workspace** context search uses this path before deleting the store. |
| `features/access-admin/README.md` | Must be updated in the same change per the repository's nearest-README rule. |
| `access-admin-unsaved-changes.guard.ts`, `access-permission-draft.store.ts` | Workspace-owned — **keep**. Listed here only to record that they were checked and are not in scope. |

### C. Backend surface requiring independent review

The following endpoints lose their only known frontend caller. They are recorded as
**possible unused API surface — separate review required**, and are explicitly **not** deletion
recommendations:

- `GET /api/access/audit-events`
- `POST /api/access/users/{userId}/logto-sub/relink/preview`
- `POST /api/access/users/{userId}/logto-sub/relink/confirm`
- `GET /api/access/owner-reconciliation/status`

**No backend authorization, security, access-audit, security-logging, permission-enforcement or
owner/admin security infrastructure should be removed because these screens are removed.** Audit
events and owner reconciliation are backend safety mechanisms whose value does not depend on a UI
existing to read them. Whether the read endpoints stay exposed is a separate decision with its own
review.

---

## Planning Inputs

Planning inputs only — this is not an implementation plan, and no sequencing below has been
committed to.

### 1. Shared/global fixes that should happen first

1. **X-1** — details content projection. Highest severity, smallest blast radius, and it must
   precede any other edit to the four details-panel templates.
2. **X-3 + X-6** — ayah card shrinking and the stylesheet that was supposed to prevent it. These
   are one geometry problem and should be reasoned about together.
3. **X-2 (+ X-4)** — the `qd-tabs` layout contract. Largest blast radius; it resolves U-1, R-1,
   L-1, B-1's section strip, B-2 and the Mushaf M-1 secondary contributor at once.
4. **N-1** — migrate `.qd-nav__menu` onto the existing floating-layer placement.

### 2. Page-specific fixes

- **L-1** — delete `.lemma-details-panel__tab { inline-size: 100% }`. Safe on its own and
  strictly improves the current state, so it does not have to wait for X-2.
- **R-2** — content geometry reservation (third consumer of the C-3 pattern).
- **X-7** — add exhaustive fallbacks to the nine `@switch` content blocks.
- **B-1** — restructure the Abwab toolbar into two rows.
- **R-4** — give `qd-details-workspace` a real "no header" condition and delete the two
  `display: none` workarounds.
- **A-1** — the Access Management removal.

### 3. Dependency order

```
X-1  ──────────────► X-5 (extract the shell only once the shape is correct)
X-2  ──┬──► U-1, R-1, B-2      (consumers, no local work)
       ├──► L-1 (delete the local override in the same change, or earlier standalone)
       ├──► X-4 (delete the inert flex-wrap declarations)
       └──► B-1 (the sections grid should consume X-2's wrapping mode)
X-6  ──┬──► X-3  (same geometry; resolve the cascade before changing the card)
R-2  ──────► C-3 (third consumer triggers the shared extraction, decision N3-a)
X-7        independent
N-1        independent
A-1        independent
```

X-1 and X-2 both edit the four details-panel templates and should not be attempted in parallel.

### 4. Safe deletion work

Can proceed independently of every layout change:

- `.explorer-detail-panel__body` block — `styles/_explorer-detail-lists.scss:345-410` (dead
  selector, verified zero template matches).
- `.qd-explorer-subtabs { flex-wrap: wrap }` and `.abwab-toolbar__tabs { flex-wrap: wrap }`
  (verified inert).
- `.lemma-details-panel__tab { inline-size: 100% }` (L-1).
- The Access Management audit and security surfaces (A-1, section A above).
- Once X-2 lands: `QdTabsComponent`'s `effect(() => selected?.scrollIntoView())` and the
  `--scrollable` mode itself.

### 5. Areas requiring targeted browser verification

Not blocking, but each is a claim that should be confirmed against a rendered page before or
during implementation:

- **Access Management** — the entire A-1 surface was audited from code only; the page requires an
  owner session that the audit did not have. Confirm the three-tab strip and the two eager loads
  against a signed-in owner.
- **Navbar الإعدادات menu** — the trigger is gated on `isActiveOwner()`. The overflow in N-1 is
  arithmetic on measured cluster geometry, not a rendered observation; confirm with an owner
  session.
- **Compact and Medium bands** — every measurement in this report was taken at 1905 px. The
  automated window could not be resized, so the `≤767` and `768–1079` behavior of the tab rows
  (X-2), the Abwab toolbar (B-1) and the details panels is unverified.
- **X-2 after implementation** — Compact-band label readability at 5 equal columns is the specific
  risk, and the Mushaf ayah-study strip must be re-checked because it is a consumer.
- **X-3 after implementation** — the Mushaf similar-ayahs / mutashabihat loading placeholders are
  deliberately geometry-matched to their loaded cards; confirm `flex-shrink: 0` does not desync
  them.
- **Word Types explorer** — inferred to behave like Stems (4-key/2-key tab sets, same panel
  template) but not opened during the audit.

### 6. Areas that should be isolated into separate implementation phases

- **X-2** deserves its own phase with its own visual verification pass. It moves every tab row in
  the product simultaneously and touches the permanent visual authority's territory.
- **A-1 (Access removal)** should be its own phase — it is a scope reduction with a `?tab=` URL
  contract and a backend follow-up question, and it shares no files with any layout finding.
- **C-2/C-3 extractions** should follow their triggering fixes rather than accompany them;
  extracting while the shape is still wrong bakes in the defects.
- **The backend endpoint review** (A-1 section C) is out of frontend scope entirely and belongs to
  a separate review with its own authorization-safety analysis.

---

## Final Verdict

**READY_WITH_TARGETED_BROWSER_VERIFICATION**

Every HIGH and MEDIUM finding except A-1 and N-1 is confirmed against a rendered page with
measured geometry, and every finding is traced to specific files and lines. The two exceptions
(Access Management, the navbar Settings menu) are blocked on an owner session, not on analysis,
and the Compact/Medium bands were not measurable in this environment. Those items are enumerated
in *Planning Inputs* §5 and are the only work needed before an implementation plan can be written
with full confidence.

---

Status: AUDITED — REPORT ONLY, NO IMPLEMENTATION
