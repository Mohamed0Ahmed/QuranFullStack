# Contract: UI Design Tokens & `qd-*` Classes

The visual contract future features build on. Tokens are CSS custom properties; reusable patterns
are `qd-*` classes. Components MUST consume these and MUST NOT hardcode colors or redefine
primitives (`UI_STYLE_SYSTEM.md` §1–§4, §10).

## Theme switching

- Root attribute on `<html>`: `data-theme="light"` or `data-theme="dark"`.
- `_tokens.scss` defines base + light values; `_themes.scss` overrides per theme.
- Concrete OKLCH values are chosen in implementation: warm-tinted neutrals, **no pure `#fff`/`#000`**;
  one muted accent used on ≤10% of a screen (One Voice Rule).

## Token set (names are the contract; values set in implementation)

```scss
:root {
  /* color */
  --qd-bg: ;            /* app background */
  --qd-surface: ;       /* cards/panels */
  --qd-surface-elevated: ; /* menus/popovers */
  --qd-text: ;          /* primary text */
  --qd-text-muted: ;    /* secondary/meta */
  --qd-border: ;        /* hairlines */
  --qd-accent: ;        /* single muted accent */
  --qd-danger: ; --qd-warning: ; --qd-success: ;
  --qd-focus-ring: ;    /* visible focus */
  /* radius */
  --qd-radius-sm: ; --qd-radius-md: ; --qd-radius-lg: ;
  /* spacing scale */
  --qd-space-1: ; --qd-space-2: ; --qd-space-3: ;
  --qd-space-4: ; --qd-space-5: ; --qd-space-6: ;
}
```

## `qd-*` classes built this phase

| Class | Purpose |
|-------|---------|
| `qd-shell` | App shell grid (navbar / content / footer) |
| `qd-navbar` | Top navigation bar container |
| `qd-container` | Centered max-width content container |
| `qd-footer` | Footer container |
| `qd-page` | Page wrapper (padding/spacing) |
| `qd-page-header` | Page title + description block |
| `qd-card` | Surface card (tonal layering + hairline, not shadow) |
| `qd-btn`, `qd-btn-primary`, `qd-btn-secondary`, `qd-btn-ghost` | Buttons |
| `qd-input` | Basic form control (foundation set) |
| `qd-badge` | Small status/label chip |
| `qd-empty-state`, `qd-loading-state`, `qd-error-state` | Common async states (calm) |
| `qd-page-title`, `qd-section-title`, `qd-card-title` | Heading text styles (naskh) |
| `qd-text`, `qd-text-muted`, `qd-text-meta` | Body/meta text styles (UI sans) |

## Typography contract

- Content/headings: **Amiri** (naskh), self-hosted.
- UI chrome: **IBM Plex Sans Arabic**, self-hosted.
- `@font-face` in `_typography.scss`, `font-display: swap`.
- Tashkeel must render correctly with generous line-height for Arabic.

## RTL contract

- Use logical properties (`margin-inline-*`, `padding-inline`, `inset-inline-*`,
  `border-inline-*`); avoid hardcoded `left/right`.
- The app root sets `dir="rtl"` / `lang="ar"`; components must not break in RTL.

## Rules

- New components compose these classes/tokens; they do NOT reinvent buttons/cards/inputs.
- Component SCSS stays small (local layout only); never redefines global primitives or palettes.
- Both themes meet WCAG 2.1 AA; focus states always visible; reduced-motion respected.
