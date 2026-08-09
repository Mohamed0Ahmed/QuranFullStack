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
  chrome").
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
  `$qd-bp-desktop-min` / `$qd-bp-wide-desktop-min` survive as aliases for unmigrated call-sites,
  but they now resolve to the Golden bands — `tablet-max` is `1079`, not `1023`, and `desktop-min`
  is `1080`, not `1024`. Moving those two is D10 itself: it is what stops the legacy desktop
  composition from engaging at the 1024 edge.
- `_layout.scss` — shell, navbar, footer, container, and page-level layout primitives, plus the
  Golden page-shell contract (`.qd-page-shell` + the four named intents, the three rail sizes, the
  two named splits, and the four bounded grids). `.qd-page` is **block rhythm only**: the inline
  gutter belongs to the shell alone, and any shell/container/frame nested inside another drops its
  `padding-inline` so a nested surface cannot create a second route gutter
  (`.architecture/UI_STYLE_SYSTEM.md` §18.4). The one surviving exception is
  `.qd-page > .qd-page-header`, which keeps a gutter for the placeholder page — the single legacy
  shape whose header is a direct child of `.qd-page` with no content container; it retires with D04.
  Also holds `.qd-page-frame` (`.architecture/UI_STYLE_SYSTEM.md` §2) — the full-bleed page-frame
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
  Also holds the Golden surface ladder (`.qd-surface` + `--quiet` / `--sunken` / `--chrome`) and
  `.qd-selected-thread`, the logical 2px `border-inline-start` selection mark that call-sites adopt
  in their own phase. `.qd-card` carries **no resting `box-shadow`** (resting elevation is zero;
  shadow exists only on floating layers) and hover is the one neutral surface
  `--qd-surface-quiet` — the former `.qd-card--mini` accent-border hover was green decoration and
  is gone. Also holds `.qd-modal--fixed` (`.architecture/UI_STYLE_SYSTEM.md` §17) — the opt-in fixed
  block-size modifier for `.qd-modal`, plus its `.qd-modal__head` / `.qd-modal__body` /
  `.qd-modal__foot` slots; the bare `.qd-modal` base stays width-only and scroller-less, so
  compose the modifier rather than adding a block-size to a call-site. Its width sibling
  `.qd-modal--wide` (52rem, same §17 entry) is the one sanctioned wide step — three
  consumers, and no call-site may introduce a fourth width. Also holds
  `.qd-context-menu__item` / `--danger` (`.architecture/UI_STYLE_SYSTEM.md` §17) — the item
  styling `shared/ui/context-menu/`'s `qd-context-menu` projects its content into; global
  because a rule scoped to the primitive's own stylesheet cannot reach content the *consumer*
  projects via `<ng-content>` (the `.qd-tabs__tab` precedent).
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
  (The page-frame rule that used to live here moved to `_layout.scss` — see above.)
  Its `--qd-explorer-chrome-block-size` (`14rem` at `:77`, `12rem` in the wide-desktop override
  at `:143`) is a hand-measured viewport budget consumed by
  `calc(100dvh - var(--qd-explorer-chrome-block-size))` (`:116,145`): it includes the navbar's
  height but deliberately does not reference `--qd-navbar-block-size`, unlike every other
  viewport-relative figure the sticky-navbar work re-based. A navbar height change therefore
  does not track into it automatically — re-measure it by hand, per the measured-not-derived
  doctrine of the contrast table below.
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
  `--qd-surface` (`_themes.scss`). Nothing asserts any of these numbers — see
  `docs/TESTING_DEBT.md` row P2.
- `.qd-badge`'s line box (`_components.scss:132,135,137,140` — `padding-block var(--qd-space-1)`,
  `0.75rem` text at `1.4` line-height, `1px` border each side) is mirrored by the dashboard
  app-meta skeleton, which composes the same metrics into one height
  (`../app/features/dashboard/pages/dashboard-home/dashboard-home.component.scss:40-48`). There is
  no shared badge line-box token, so any `.qd-badge` restyle must be repeated there or the skeleton
  silently stops matching the loaded badge it stands in for.
