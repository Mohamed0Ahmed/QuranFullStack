# Global styles

Global SCSS partials for app-wide tokens, shared UI primitives, and explorer layouts.
Compiled through `../styles.scss`; component-specific styling stays beside each component.

## Partial groups

- `_tokens.scss` — root CSS custom properties for color, spacing, radii, shadows, fonts,
  Mushaf, and explorer variables. Includes the color-doctrine role tokens
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
  chrome").
- `_themes.scss` — dark-theme overrides for the same token surface (`--qd-accent-fg` and
  `--qd-selected-bg` are intentionally theme-invariant and not overridden here).
- `_typography.scss` — font-face declarations and shared Arabic-first type classes.
- `_breakpoints.scss` — canonical Sass breakpoints; mirrored in `../app/shared/layout/breakpoints.ts`.
- `_layout.scss` — shell, navbar, footer, container, and page-level layout primitives. Also
  holds `.qd-page-frame` (`.architecture/UI_STYLE_SYSTEM.md` §2) — the full-bleed page-frame
  rule (`box-sizing: border-box`, no `.qd-container` width cap, column flex, the mobile-stat-bar
  `padding-block-end`), beside `.qd-container` since it is shared page furniture, not
  words-specific. `.qd-explorer-frame` is kept as a working alias on the same rule (Slice B2,
  T701/T702) so the five existing explorer call-sites keep working untouched — dual-selector
  precedent: `explorer-panel-skeleton.component.ts:16`. New call-sites use `.qd-page-frame`.
  `.qd-navbar` is `position: sticky` on `--qd-z-mobile-nav`, **not** `--qd-z-sticky` (Slice B2,
  T901/T903) — see `.architecture/UI_STYLE_SYSTEM.md` §17 "Sticky app chrome" for the
  containing-block gotcha (`top-navbar.component.scss`'s `:host { display: contents }` is
  load-bearing, not decorative) and why the navbar's own rung has to match the rung its
  dropdown/mobile-menu already declare, since sticky positioning makes the navbar's rung a
  ceiling for everything inside it.
- `_forms.scss` — shared input/select styling and focus behavior. Also holds the
  `.qd-checkbox` / `.qd-check-row` family (`.architecture/UI_STYLE_SYSTEM.md` §17) — a
  fixed `--qd-checkbox-size` box plus a fixed-gap label row; call-sites compose them and
  never re-declare box size or accent locally.
- `_components.scss` — global cards, buttons, badges, modal, detail-panel, and skeleton patterns.
  Also holds `.qd-modal--fixed` (`.architecture/UI_STYLE_SYSTEM.md` §17) — the opt-in fixed
  block-size modifier for `.qd-modal`, plus its `.qd-modal__head` / `.qd-modal__body` /
  `.qd-modal__foot` slots; the bare `.qd-modal` base stays width-only and scroller-less, so
  compose the modifier rather than adding a block-size to a call-site. Also holds
  `.qd-context-menu__item` / `--danger` (`.architecture/UI_STYLE_SYSTEM.md` §17) — the item
  styling `shared/ui/context-menu/`'s `qd-context-menu` projects its content into; global
  because a rule scoped to the primitive's own stylesheet cannot reach content the *consumer*
  projects via `<ng-content>` (the `.qd-tabs__tab` precedent).
- `_utilities.scss` — small utility classes such as screen-reader-only, flex, spacing, and stable
  scrollbars. Also holds `.qd-truncate` (`.architecture/UI_STYLE_SYSTEM.md` §17 "Truncatable
  entity names") — the one flexible-with-ellipsis rule for a truncatable entity-name column;
  pair it with `--qd-name-min-inline-size` (`_tokens.scss`) for a reserved minimum.
- `_words-explorer-layout.scss` — shared layout pieces for words explorer intro/toolbar surfaces.
  (The page-frame rule that used to live here moved to `_layout.scss` — see above.)
- `_words-explainer.scss` — shared visual primitives for the Words explainer hero example regions
  (global, not component-scoped, because pages project their own example markup via `<ng-content>`).
- `_explorer-tables.scss` — responsive shared table/list rules for explorer pages.
- `_explorer-detail-lists.scss` — shared detail-list layouts for roots/lemmas/stems/word-types panels.

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
- Keep breakpoint values synchronized between `_breakpoints.scss` and `../app/shared/layout/breakpoints.ts`.
- Global explorer partials should stay generic across Roots, Lemmas, Stems, Word Types, and related detail panels.
