# Global styles

Global SCSS partials for app-wide tokens, shared UI primitives, and explorer layouts.
Compiled through `../styles.scss`; component-specific styling stays beside each component.

## Partial groups

- `_tokens.scss` — root CSS custom properties for color, spacing, radii, shadows, fonts,
  Mushaf, and explorer variables. Includes the color-doctrine role tokens
  (`--qd-accent-fg`, `--qd-border-accent`, `--qd-surface-hover`, `--qd-selected-bg`,
  `--qd-danger-tint`, `--qd-success-tint`, `--qd-warning-tint`) — see
  `.architecture/UI_STYLE_SYSTEM.md` §16 for the role→color contract these back.
- `_themes.scss` — dark-theme overrides for the same token surface (`--qd-accent-fg` and
  `--qd-selected-bg` are intentionally theme-invariant and not overridden here).
- `_typography.scss` — font-face declarations and shared Arabic-first type classes.
- `_breakpoints.scss` — canonical Sass breakpoints; mirrored in `../app/shared/layout/breakpoints.ts`.
- `_layout.scss` — shell, navbar, footer, container, and page-level layout primitives.
- `_forms.scss` — shared input/select styling and focus behavior.
- `_components.scss` — global cards, buttons, badges, modal, detail-panel, and skeleton patterns.
- `_utilities.scss` — small utility classes such as screen-reader-only, flex, spacing, and stable scrollbars.
- `_words-explorer-layout.scss` — shared layout pieces for words explorer intro/toolbar surfaces.
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
7. `explorer-tables`
8. `explorer-detail-lists`
9. `forms`
10. `utilities`
11. Tailwind base/components/utilities
12. `html`/`body` base reset and body font/background colors

`_breakpoints.scss` supports other partials but is not imported directly by `../styles.scss`.

## Boundary

- Put reusable tokens, global utility classes, shared explorer scaffolding, and app-shell styles here.
- Keep feature- or component-specific selectors in local component `.scss` files.
- If a selector is only meaningful inside one component tree, keep it scoped there even if it looks reusable.

## Invariants

- Arabic-first typography and RTL-friendly spacing start here; do not swap shared font roles casually.
- Keep breakpoint values synchronized between `_breakpoints.scss` and `../app/shared/layout/breakpoints.ts`.
- Global explorer partials should stay generic across Roots, Lemmas, Stems, Word Types, and related detail panels.
