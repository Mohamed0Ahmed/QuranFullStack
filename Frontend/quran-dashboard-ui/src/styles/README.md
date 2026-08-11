# Global styles

Global SCSS partials for app-wide tokens, shared UI primitives, and explorer layouts.
Compiled through `../styles.scss`; component-specific styling stays beside each component.

## Partial groups

- `_tokens.scss` — root CSS custom properties for color, spacing, radii, shadows, fonts,
  Mushaf, and explorer variables. **This is the one place a colour, radius, shadow, spacing step,
  control height, gutter, rail, measure, or modal width may be written as a literal**; every other
  partial references a token, and `npm run check:golden-ui` fails on a colour literal found
  elsewhere in the Golden layer. It now carries the Golden light values on the existing *themed*
  token names, with the Golden semantic names (`--qd-bg-page`, `--qd-surface-quiet`, `--qd-ink*`,
  `--qd-green-*`, `--qd-lifecycle-*`, `--qd-mutation-*`, …) declared as aliases onto them — that
  direction is load-bearing, because `_themes.scss` overrides the themed name and an alias written
  the other way round would strand every migrated component in light values under the dark toggle
  (`.architecture/UI_STYLE_SYSTEM.md` §18.1). The `--qd-s-2 … --qd-s-64` scale is the Golden
  4px/8px rhythm and `--qd-space-1…6` are aliases onto it, so there is a single spacing truth.
  `--qd-page-gutter` is the one responsive route gutter (16/24/32/40 at Compact/Medium/Wide/
  Wide-plus) and is declared here, not in `_layout.scss`. Includes the color-doctrine role tokens
  (`--qd-accent-fg`, `--qd-border-accent`, `--qd-surface-hover`, `--qd-selected-bg`,
  `--qd-danger-tint`, `--qd-success-tint`, `--qd-warning-tint`) — see
  `.architecture/UI_STYLE_SYSTEM.md` §16 for the role→color contract these back. Also holds the
  shared control geometry (`--qd-btn-*` → `--qd-control-block-size`, and
  `--qd-pagination-margin-block-start` → `--qd-pagination-slot-block-size`): `.qd-btn` and
  `qd-pagination` are built from these values and the reserved slots that stand in for a
  not-yet-mounted control row are sized from the same ones, so a reservation can never drift from
  its control. Size a new reserved slot from these tokens; never re-measure the control by hand.
  Also holds the `--qd-z-*` layer scale (`.architecture/UI_STYLE_SYSTEM.md` §4) — every
  stacking `z-index` in the app is one of these rungs; never write a bare `z-index`. Also
  holds `--qd-checkbox-size`, the fixed box size `.qd-checkbox` (`_forms.scss`) is built
  from. Also holds `--qd-name-min-inline-size` — the reserved-minimum floor a truncatable
  entity-name column pairs with `.qd-truncate` (`_utilities.scss`), per
  `.architecture/UI_STYLE_SYSTEM.md` §17's "Truncatable entity names" entry. Also holds
  `--qd-mushaf-sticky-top` and `--qd-mushaf-panel-height` (Slice B2, T902) — both re-based
  onto `--qd-navbar-block-size` now that the navbar is sticky; `--qd-mushaf-panel-height`
  derives from `--qd-mushaf-sticky-top`, not the bare navbar token, or the panel's stuck
  bottom edge lands past the viewport (`.architecture/UI_STYLE_SYSTEM.md` §17 "Sticky app
  chrome"). `--qd-mushaf-word-font-size` is the protected reader's responsive type step:
  `1.25rem` on Compact and `1.35rem` from Medium upward.
- `_themes.scss` — dark-theme overrides for the same token surface (`--qd-accent-fg` and
  `--qd-selected-bg` are intentionally theme-invariant and not overridden here).
- `_typography.scss` — font-face declarations and shared Arabic-first type classes. The classes are
  built from the `--qd-type-*` scale in `_tokens.scss` rather than local rem literals; the
  `$font-naskh` / `$font-sans` / `$font-quran` Sass variables were removed because they duplicated
  `--qd-font-naskh` / `--qd-font-ui` / `--qd-font-quran` and nothing consumed them.
- `_breakpoints.scss` — the **Sass adapter** over
  `../app/shared/layout/breakpoints.contract.json`, which is the single neutral source TypeScript
  and Tailwind read directly. Sass cannot import JSON, so the literals are restated here and
  `npm run check:golden-ui` compares every one of them against the contract. Compact `≤767`,
  Medium `768–1079`, Wide `≥1080`, Wide-plus `≥1440`. `$qd-bp-phone-max` / `$qd-bp-tablet-max` /
  `$qd-bp-desktop-min` / `$qd-bp-wide-desktop-min` remain as aliases onto those five values, and
  they still have call-sites in `_components.scss`, `_explorer-tables.scss`,
  `_words-explorer-layout.scss`, `_words-explainer.scss` and nine feature stylesheets, so Plan 7
  §7's zero-consumer condition for retiring them is **not** met and they are deliberately kept.
  They resolve to the Golden bands — `tablet-max` is `1079`, not `1023`, and `desktop-min`
  is `1080`, not `1024`. Moving those two is D10 itself: it is what stops the legacy desktop
  composition from engaging at the 1024 edge.
- `_layout.scss` — shell, navbar, footer, and page-level layout primitives, plus the
  Golden page-shell contract (`.qd-page-shell` + the four named intents, the three rail sizes, the
  two named splits, and the four bounded grids). `.qd-page` is **block rhythm only**, and
  `.qd-page-shell` is the **sole route-gutter owner**: it holds the only
  `padding-inline: var(--qd-page-gutter)` declaration in the whole stylesheet tree, a nested shell
  drops it, and `npm run check:golden-ui` fails if a second one appears anywhere under `src/`
  (D01, `.architecture/UI_STYLE_SYSTEM.md` §18.4). The `.qd-container` / `.qd-page-frame` /
  `.qd-explorer-frame` aliases and the `.qd-page > .qd-page-header` compatibility gutter were
  deleted in Phase 11 after `rg` proved zero template consumers; they remain absent.
  `.qd-page-header` is likewise down to the two rules every page actually
  composes — its block rhythm and `__description`; the `--split`, `__eyebrow`, `__meta` and
  `__actions` members were nominated F03 owners that no page header ever asked for, so they are gone
  rather than left as a header vocabulary the next author would assume is load-bearing.
  `.qd-navbar` is `position: sticky` on `--qd-z-mobile-nav`, **not** `--qd-z-sticky` (Slice B2,
  T901/T903) — see `.architecture/UI_STYLE_SYSTEM.md` §17 "Sticky app chrome" for the
  containing-block gotcha (`top-navbar.component.scss`'s `:host { display: contents }` is
  load-bearing, not decorative) and why the navbar's own rung has to match the rung the app
  navigation's own menu surfaces declare, since sticky positioning makes the navbar's rung a
  ceiling for everything inside it.
- `_forms.scss` — shared input/select styling and focus behavior. Also holds the
  `.qd-checkbox` / `.qd-check-row` family (`.architecture/UI_STYLE_SYSTEM.md` §17) — a
  fixed `--qd-checkbox-size` box plus a fixed-gap label row; call-sites compose them and
  never re-declare box size or accent locally. At Compact, `.qd-control` / `.qd-input` /
  `.qd-select` and `.qd-check-row` ratchet their `min-block-size` from `--qd-control-md` up to
  `--qd-hit-target-min` (44px), which is the §1.4 Compact hit-target floor the `.qd-action`
  family already honours.
- `_components.scss` — global cards, buttons, badges, modal, detail-panel, and skeleton patterns.
  Also holds the Golden surface ladder, which is `.qd-surface` + `--quiet` **only**: the `--sunken`
  and `--chrome` rungs, plus `.qd-card--feature`, were classes nothing composed and were deleted —
  the two surfaces themselves are still reachable as `--qd-surface-sunken` / `--qd-bg-chrome`, which
  is how `.qd-tabs--segmented` and `.qd-data-table__header` take them. There is likewise no
  `.qd-selected-thread` utility: the logical 2px `border-inline-start` selection mark (D26) is
  declared by the owners that actually draw it — the `.qd-result-item` row variants here, the explorer table row in
  `_explorer-tables.scss`, and `abwab-tree` / `abwab-cards` / `abwab-templates-page` in their own
  component stylesheets — all through `--qd-green-thread` / `--qd-green-thread-size`, because each
  needs the transparent-when-unselected reservation that a bare utility cannot express.
  `.qd-card` carries **no resting `box-shadow`** (resting elevation is zero;
  shadow exists only on floating layers) and hover is the one neutral surface
  `--qd-surface-quiet` — the former `.qd-card--mini` accent-border hover was green decoration and
  is gone. The legacy modal aliases `.qd-modal--wide` / `.qd-modal--fixed` and the
  `.qd-modal__head` / `__body` / `__foot` slots were deleted in Phase 11 at zero consumers; every
  dialog now resolves to `qd-modal-shell`'s four named widths. The bare `.qd-modal` /
  `.qd-modal-backdrop` pair went the same way once the four Words Compact detail drawers
  (`root`/`lemma`/`stem`/`word-type-details-panel`) moved onto the shell's `overlay` variant;
  `.explorer-detail-modal` remains, but only as a content class — flex column, `min-block-size: 0`
  and the detail background, with **no** inline size, block size or scroller of its own. See
  `../app/features/words/README.md`. Also holds
  `.qd-context-menu__item` / `--danger` (`.architecture/UI_STYLE_SYSTEM.md` §17) — the item
  styling `shared/ui/context-menu/`'s `qd-context-menu` projects its content into; global
  because a rule scoped to the primitive's own stylesheet cannot reach content the *consumer*
  projects via `<ng-content>` (the `.qd-tabs__tab` precedent). Since Plan 7 Phase 3 it also holds
  the shared interaction vocabulary (`.architecture/UI_STYLE_SYSTEM.md` §20): the F08 `.qd-toolbar`
  zones, the F10 `.qd-result-list` / `.qd-result-item` frame, the F11 `.qd-details__*` anatomy, the
  F15 `.qd-floating-layer*` surface and item states, and the F17 static badges
  `.qd-badge--lifecycle-*` and `.qd-badge--membership-owner` — static because they carry no
  interaction and therefore need no Angular owner. Within that F10 frame the row geometry
  (`display`, `align-items`, `gap`, `padding`, borders, background) is scoped to the row variants
  `--linked` / `--display-only` / `--master` / `--event`, and the `--qd-hit-target-min` floor is
  scoped further to the interactive rows (`--linked` rows and `--selectable` items), so the
  `quran-result` card list leaves geometry entirely to `qdAyahCard`. The variant scope is written
  with `:where()` so the frame keeps single-class specificity and cannot out-cascade the detail-list
  row layouts in `_explorer-detail-lists.scss`. Two nominated owners here had no call-site and
  are gone: the F15 danger item is `.qd-context-menu__item--danger` alone (the only danger item the
  app renders is an Abwab row-menu item, so the parallel `.qd-floating-layer__item--danger` selector
  was dead), and F17's `.qd-count-chip` had no consumer at all — a count today rides on `qd-chip`'s
  trailing count or on a feature-local chip. The F08 toolbar likewise keeps only `.qd-toolbar` and
  `--taxonomy`; `--workspace` was a modifier no toolbar asked for. The `.qd-tabs*` family gained the count-driven `--segmented`
  layout and the Golden selected pill (its `--scrollable` sibling, the one `overflow-x: auto` rule
  in the family, was deleted in Phase 11 once `--tracks` left it without a consumer); `qd-modal-shell`'s own geometry lives in its component
  stylesheet, not here, because nothing projects into it from outside.
- `_utilities.scss` — small utility classes such as screen-reader-only, flex, spacing, and stable
  scrollbars. Also holds `.qd-ltr-isolate` (the only sanctioned Latin island — applied to the value
  element, never a container), `.qd-hit-target` (expands a small control to the 44px
  `--qd-hit-target-min` through a negative-inset `::after`, leaving the visible icon size alone),
  and `.qd-flex-shrink-guard` / `.qd-flex-fixed` (the Golden shrink guard: a bare `flex: 1` on a
  text input keeps `min-width: auto` ≈ 20 characters and pushes a Compact row past the viewport).
  Also holds `.qd-truncate` (`.architecture/UI_STYLE_SYSTEM.md` §17 "Truncatable
  entity names") — the one flexible-with-ellipsis rule for a truncatable entity-name column;
  pair it with `--qd-name-min-inline-size` (`_tokens.scss`) for a reserved minimum.
- `_words-explorer-layout.scss` — shared layout pieces for words explorer intro/toolbar surfaces.
  (The page-frame rule that used to live here moved to `_layout.scss` — see above.) The
  `uw-toolbar-rise` entrance animation and its `--kinetic` modifier are gone (D19: motion is state
  feedback only), and no template carries the class name any more.
  Its `--qd-explorer-chrome-block-size` (`14rem` at `:77`, `12rem` in the wide-desktop override
  at `:143`) is a hand-measured viewport budget consumed by
  `calc(100dvh - var(--qd-explorer-chrome-block-size))` (`:116,145`): it includes the navbar's
  height but deliberately does not reference `--qd-navbar-block-size`, unlike every other
  viewport-relative figure the sticky-navbar work re-based. A navbar height change therefore
  does not track into it automatically — re-measure it by hand, per the measured-not-derived
  doctrine of the contrast table below.
- `_words-explainer.scss` — shared visual primitives for the Words explainer hero example regions
  (global, not component-scoped, because pages project their own example markup via `<ng-content>`).
- `_explorer-tables.scss` — responsive shared table/list rules for explorer pages. The selected row
  is marked by an absolutely positioned `::before` thread on `inset-inline-start`, not the former
  `box-shadow: inset -2px 0` (D26): a physical inset is wrong in RTL, and a real
  `border-inline-start` would have shifted the body grid 2px out of alignment with the header row,
  which is a separate element. The pseudo-element is out of flow, so it creates no grid item.
- `_explorer-detail-lists.scss` — shared detail-list layouts for roots/lemmas/stems/word-types panels.
  It does **not** own `.ayah-matches-list__viewport` geometry: that lives in
  `features/words/components/ayah-matches-list/ayah-matches-list.component.scss`, and
  the inline `.qd-details-panel-shell--contained-scroll` mode disables the outer body scroller so
  the flex-filled ayah viewport owns the only scrollbar and leaves pagination visible. Modal
  contexts select the outer-scroller variant through component-local `:host-context` rules instead
  of re-declaring that geometry globally. The overlay dialog owns the scroll, so nothing inside it
  may nest a second one.

## Import order

`../styles.scss` loads partials in this order:

1. `tokens`
2. `themes`
3. `typography`
4. `layout`
5. `components`
6. `words-explorer-layout`
7. `words-explainer`
8. `explorer-tables`
9. `explorer-detail-lists`
10. `forms`
11. `utilities`
12. Tailwind base/components/utilities
13. `html`/`body` base reset and body font/background colors

`_breakpoints.scss` supports other partials but is not imported directly by `../styles.scss`.

## Boundary

- Put reusable tokens, global utility classes, shared explorer scaffolding, and app-shell styles here.
- Keep feature- or component-specific selectors in local component `.scss` files.
- If a selector is only meaningful inside one component tree, keep it scoped there even if it looks reusable.

## Invariants

- Arabic-first typography and RTL-friendly spacing start here; do not swap shared font roles casually.
- Breakpoints have one source: `../app/shared/layout/breakpoints.contract.json`. TypeScript and
  Tailwind read it; `_breakpoints.scss` restates it because Sass cannot import JSON, and
  `npm run check:golden-ui` fails when the restatement drifts. Never hand-sync three files again,
  and never write a raw pixel threshold in a migrated partial.
- Global explorer partials should stay generic across Roots, Lemmas, Stems, Word Types, and related detail panels.
- **Measured contrast ratios pinned to light-theme tokens (`_tokens.scss`).** These were measured,
  not derived: nothing in the file recomputes them, so re-tuning any of these tokens by eye
  silently drops the pairing below its target. Re-measure rather than adjust. Each ratio is stated
  against the surface it was measured on:

  The table below was **re-measured against the Golden light values** when the foundation retuned
  the themed tokens; the previous numbers were measured against the superseded oklch values and no
  longer apply.

  | token | measured against | ratio |
  |---|---|---|
  | `--qd-ayah-card-bg` | Quran text on the ayah card | 14.24:1 |
  | `--qd-ayah-card-bg` | muted meta text on the ayah card | 4.96:1 (AA) |
  | `--qd-accent-text` | text emphasis on `--qd-surface` | 7.68:1 |
  | `--qd-accent-text` | text emphasis on `--qd-bg` | 6.86:1 |
  | `--qd-warning` | on `--qd-warning-tint` | 5.13:1 |
  | `--qd-warning` | as a dot on the navy footer | **2.66:1** (was 3.02:1) |
  | `--qd-danger` | on `--qd-danger-tint` | 6.97:1 |
  | `--qd-accent-text` | on `--qd-success-tint` (mutation success) | 6.60:1 |
  | `--qd-text-muted` | on `--qd-bg` | 5.01:1 (AA) |
  | `--qd-text-muted` | on `--qd-surface` / `--qd-section-bg` / `--qd-surface-recessed` | 5.60 / 5.37 / 4.70:1 (AA) |
  | `--qd-text` | on `--qd-bg` | 14.37:1 |
  | `--qd-text-body` | on `--qd-surface` | 10.44:1 |
  | `--qd-footer-text` | on `--qd-footer-bg` | 11.39:1 |
  | `--qd-primary-fg` | on `--qd-primary` (primary action) | 7.17:1 |

  One pairing moved the wrong way and is recorded rather than silently accepted: the footer health
  **dot** on the navy band fell from 3.02:1 to 2.66:1, because the Golden warning value (`#8A5A12`)
  is darker than the value it replaced. It is a non-text indicator that always sits beside its own
  text label, and the footer is app chrome owned by a later phase — do not "fix" it by inventing a
  warning value outside the Golden status table, which is exhaustive.

  `--qd-ayah-card-bg` is a warm tone deliberately recessed below `--qd-surface` so an ayah card
  reads as a distinct card on the near-white surfaces it sits on; the dark theme overrides it to
  `--qd-surface` (`_themes.scss`).
- `.qd-badge`'s line box (`_components.scss:132,135,137,140` — `padding-block var(--qd-space-1)`,
  `0.75rem` text at `1.4` line-height, `1px` border each side) is mirrored by the dashboard
  app-meta skeleton, which composes the same metrics into one height
  (`../app/features/dashboard/pages/dashboard-home/dashboard-home.component.scss:40-48`). There is
  no shared badge line-box token, so any `.qd-badge` restyle must be repeated there or the skeleton
  silently stops matching the loaded badge it stands in for.
