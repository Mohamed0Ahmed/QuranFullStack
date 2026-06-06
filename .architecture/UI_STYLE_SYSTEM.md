# Frontend UI Style System

## Purpose

This document defines the **shared frontend style system** for the Quran Dashboard
Angular app, so all future components consume a centralized design system instead
of scattered custom styles.

It covers:

- colors
- themes (light/dark)
- typography
- spacing
- layout primitives
- cards
- buttons
- inputs
- badges
- tables
- modals
- common UI states

Read this file **before creating or changing**:

- global styles
- theme tokens
- reusable UI classes
- layout shell styles
- component visual styles
- dark/light theme behavior
- shared UI patterns

This file defines the **mechanics** of the style system (tokens, classes, file
organization, theming, RTL). The **visual decisions** it serves — the actual
palette, fonts, and "Quiet Scriptorium" character — are owned by the product and
design context. Read those first and treat them as the source of truth:

- `../../PRODUCT.md` — register, users, principles, anti-references
- `../../DESIGN.md` — visual system (parchment & ink, typography, elevation, rules)

When this file and `DESIGN.md` describe the same thing, `DESIGN.md` wins on the
visual choice; this file governs how that choice is implemented and reused.

> Scope note: this is documentation/rules only. It does not create global styles,
> theme files, or components — it defines how they must be built when that work is
> explicitly requested.

## 1. Centralized Style System

- The application **must** use a centralized style system.
- Repeated visual patterns **must** be implemented as shared global classes, not
  recreated inside every component.
- Components should consume shared classes and design tokens.
- Component-specific SCSS is allowed **only** for local layout or truly unique
  details.
- Component SCSS **must not** redefine global colors, buttons, cards, inputs,
  tables, or modal styles from scratch.

## 2. Style File Organization

Recommended structure:

```text
src/styles.scss
src/styles/
  _tokens.scss
  _themes.scss
  _typography.scss
  _layout.scss
  _components.scss
  _forms.scss
  _tables.scss
  _utilities.scss
```

Rules:

- `src/styles.scss` is the **single entry point** that imports the style partials.
- Keep the number of global style files small and purposeful.
- Do not create many random style files.
- Do not put feature-specific styles in global files.
- Do not put global design-system styles inside component SCSS.

> Current state: `src/styles.scss` exists and pulls in Tailwind layers; the
> `src/styles/` partials above do not exist yet. Create them only when global
> style work is actually requested — do not scaffold empty files in advance.

## 3. Naming Convention

Use the project prefix **`qd-`** for all reusable global UI classes.

Examples:

```text
qd-page          qd-btn           qd-table
qd-shell         qd-btn-primary   qd-modal
qd-container     qd-btn-secondary qd-sidebar
qd-card          qd-btn-ghost     qd-toolbar
qd-section-title qd-input         qd-empty-state
                 qd-select        qd-loading-state
                 qd-badge         qd-error-state
```

Rules:

- All reusable global UI classes **must** use the `qd-` prefix.
- Do not create ambiguous global classes like `.card`, `.button`, `.title`,
  `.box`.
- Avoid global class names that may collide with libraries or feature styles.

## 4. Design Tokens

Use **CSS variables** as the base source of truth for themeable values.

Required token categories:

- background
- surface
- elevated surface
- text
- muted text
- border
- primary / accent
- danger
- warning
- success
- focus ring
- shadow / elevation
- radius
- spacing scale

Example shape only — **do not force exact colors yet** (the real palette is
resolved in `DESIGN.md`):

```scss
:root {
  --qd-bg: ...;
  --qd-surface: ...;
  --qd-surface-elevated: ...;
  --qd-text: ...;
  --qd-text-muted: ...;
  --qd-border: ...;
  --qd-accent: ...;
  --qd-danger: ...;
  --qd-warning: ...;
  --qd-success: ...;
  --qd-focus-ring: ...;
  --qd-radius-sm: ...;
  --qd-radius-md: ...;
  --qd-radius-lg: ...;
  --qd-space-1: ...;
  --qd-space-2: ...;
}
```

Rules:

- Components **must** use CSS variables or shared classes.
- Avoid hardcoded colors in component SCSS.
- Avoid one-off shadows, borders, and radii unless justified.
- Do **not** use pure black or pure white as the default dashboard visual language
  unless a specific accessibility need requires it. Per `DESIGN.md`, neutrals are
  tinted warm (the Warm Neutral Rule); depth comes from tonal layering and
  hairline borders, not shadows (the Flat-By-Default Rule).
- The accent token is used sparingly — for primary action, current selection, and
  review/publish state only (the One Voice Rule), never as decoration.

## 5. Light / Dark Themes

- The dashboard **must** support Light and Dark themes.
- Theme switching is controlled through **one root attribute or class**, for
  example:

  ```text
  [data-theme="light"]
  [data-theme="dark"]
  ```

- Token values are redefined per theme in `_themes.scss`; components reference the
  same tokens and never branch on theme themselves.
- Do **not** implement theme switching with scattered component booleans.
- Do **not** duplicate whole component styles for dark mode.
- Dark mode should be premium, calm, readable, and not overly saturated — it stays
  within the same restrained "Quiet Scriptorium" character.
- Theme choice should be persistable later, but **this file does not implement
  persistence** and no theme-switching runtime should be added as part of style
  documentation work.

## 6. Tailwind Usage

The project uses Tailwind CSS (currently v3).

Rules:

- Tailwind utilities are allowed for simple layout and spacing.
- Repeated design patterns should become `qd-` classes.
- Do **not** fill complex components with long, repeated Tailwind class strings
  when a shared `qd-` class is appropriate.
- Do **not** use Tailwind utilities to bypass design tokens for colors and
  repeated visual decisions.
- Tailwind supports the design system; it does not replace it.

## 7. Typography

- Arabic-first readability is required.
- Quranic or religious text display must be treated with extra care (correct
  rendering of diacritics / tashkeel, generous line-height) per `DESIGN.md`.
- UI chrome uses a clean, readable Arabic UI font; content/headings lean on the
  naskh-rooted content face (the Content-Leads Rule).
- Avoid random font sizes inside components.
- Define **shared text classes**, e.g.:

  - `qd-page-title`
  - `qd-section-title`
  - `qd-card-title`
  - `qd-text` (body text)
  - `qd-text-muted`
  - `qd-text-meta` (metadata text)

- Text must support RTL correctly.

## 8. RTL and Direction

- Arabic is the **default UI direction** (the app shell establishes `dir="rtl"`
  and `lang="ar"`).
- Use logical CSS properties where practical:

  ```text
  margin-inline-start
  margin-inline-end
  padding-inline
  border-inline-start
  ```

- Avoid `left` / `right` hardcoding unless there is a clear reason.
- Components **must not** break in RTL.
- English technical labels may remain English, but layout must respect RTL.

## 9. Reusable UI Primitives

Shared classes should exist for:

- page shell
- page header
- content container
- cards
- buttons
- form controls
- filters
- tables
- badges / status labels
- modals / dialogs
- side navigation
- toolbar
- empty / loading / error states

Rules:

- New components should **compose** these primitives.
- Do not reinvent buttons / cards / inputs per feature.
- If a new repeated visual pattern appears, add it to the style system instead of
  copying styles.

## 10. Component SCSS Rules

Component SCSS should stay small and local. It **may** handle:

- local grid / flex layout
- one-off alignment
- responsive behavior specific to that component
- small component-only interaction states

It **must not**:

- define a new color palette
- redefine `qd-card` / `qd-btn` / `qd-input` equivalents
- duplicate global design primitives
- use many hardcoded color values
- become the main source of page styling

If component SCSS grows large, split the UI into smaller components or move
repeated patterns to the global style system.

## 11. Common States

Define standard handling for:

- loading
- empty
- error
- disabled
- selected
- active
- focus
- hover

Rules:

- These states must share a consistent visual language across the app.
- Error states must be **clear but not visually aggressive** (calm, not alarmist).
- Disabled states must remain accessible.
- Focus states must be visible.

## 12. Accessibility

- Maintain readable contrast in **both** themes (WCAG 2.1 AA baseline per
  `PRODUCT.md`).
- Focus states must be visible.
- Do **not** rely on color alone to convey meaning (review/publish state needs an
  icon, label, or shape too).
- Buttons and interactive elements must have clear states.
- Text sizes must be readable for Arabic content.
- Respect reduced-motion preferences; motion conveys state only.

## 13. Quranic Data Display Safety

- Do **not** invent Quranic text or labels in the UI.
- Do **not** visually modify Quranic text in ways that may change meaning.
- Any Quranic text display style must prioritize readability and accuracy.
- Missing data must be shown as a **controlled state**, not silently fabricated.

## 14. Definition of Done for Style / UI Foundation Changes

Any future style system change should report:

- global style files changed
- new `qd-` classes added
- theme tokens added / changed
- components affected
- light / dark impact
- RTL impact
- build status
