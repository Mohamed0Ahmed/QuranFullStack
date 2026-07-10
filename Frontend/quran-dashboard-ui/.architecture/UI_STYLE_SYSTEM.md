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

- `../../PRODUCT.md` — register, users, principles, anti-references, Visual Identity
- `../../DESIGN.md` — visual system (navy + gold + parchment, typography, elevation,
  motion, rules)

The official visual identity is the **Real Pages prototype**, adopted **with
adaptation**: **navy + gold + parchment**, a soft surface + shadow ladder, light
navbar, dark navy footer, subtle card hover motion. The app stays **light + dark**
(prototype *ivory* → light, *midnight* → dark; *sage* not adopted). Section 15 below
defines the prototype-derived implementation contract; the extraction reference is
`../report/ui/real-pages-visual-system-extraction-report.md`.

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

Required token categories (the navy + gold + parchment role set; see Section 15B for
the authoritative role list and reference values):

- page / app background
- section / quiet background
- card background
- nested / recessed background
- text
- muted text
- border
- border-strong
- primary (navy structural) / primary foreground
- accent (gold) / accent-hover / accent-soft / accent-tint
- footer: footer-bg / footer-bg-2 / footer-text / footer-muted / footer-accent /
  footer-border
- danger
- warning
- success
- focus ring
- shadow / elevation ladder (resting `sm`, hover, floating `lg`)
- motion durations (fast ~140ms, base ~220ms)
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
- Avoid hardcoded colors in component SCSS; re-author adopted prototype values as
  OKLCH `--qd-*` tokens (never paste prototype hex/inline styles).
- Use the shared **elevation ladder** and **motion duration** tokens; avoid one-off
  shadows, borders, radii, and transition timings unless justified.
- The page **canvas** stays warm parchment (tinted, never pure white); pure `#000`
  is not used. Per the revised Warm Neutral Rule in `DESIGN.md`, **near-white/white
  elevated cards are allowed** when paired with the parchment background, a border,
  and a soft shadow. Depth comes from the **surface ladder + hairline borders +
  controlled soft shadows together** (the Soft-Elevation Rule) — controlled soft
  shadows are required for elevation, not banned.
- The **gold accent** token is used sparingly — active state, links, icon
  highlights, section eyebrows, and review/publish state (the One Voice Rule), never
  as decoration. **Navy** is the structural/primary color (primary buttons, brand,
  footer) and may appear more, but still calmly.

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

## 15. Prototype-Derived Implementation Contract (Navy + Gold + Parchment)

This section is the **future implementation contract** for adopting the Real Pages
prototype as the visual source of truth. It is documentation only; nothing here is
implemented yet. When a phase is actually built, re-author every value below as an
OKLCH `--qd-*` token in the app's SCSS system — **do not paste prototype CSS, inline
styles, or hex values into Angular.** Reference values are the prototype's; the
extraction reference is `../report/ui/real-pages-visual-system-extraction-report.md`.

App themes remain **light + dark** (prototype *ivory* → light, *midnight* → dark;
*sage* not adopted). Every adopted token **must** be defined for both themes.

### A. Typography

- **UI font:** IBM Plex Sans Arabic for Arabic UI chrome; IBM Plex Sans for Latin UI.
- **Weights:** use **400 / 500 / 600 / 700** where available. Mid-weights (500/600)
  carry nav links, card titles, labels, and footer headings — ship them, do not rely
  on only 400/700.
- **Quran/Mushaf fonts stay as currently implemented** (Amiri for verse text plus the
  existing ayah-marker face). **Do not replace or restyle Quran/Mushaf glyph fonts or
  Quran rendering.** Keep `--qd-font-quran` and related tokens unchanged.
- Headings use slightly tight tracking; large/section titles may scale fluidly.

### B. Color roles

Document the **roles**, not just raw colors. Reference hex is the prototype's visual
anchor; implementation converts/adapts each into the app's OKLCH `--qd-*` convention,
defined for both themes (see `DESIGN.md` §2 for the full light/dark/footer tables).

| Role | Light reference | Suggested token direction |
|------|-----------------|---------------------------|
| App / page background | `#FCFAF4` parchment | `--qd-bg` (warm canvas) |
| Section / quiet background | `#F6EFE5` | `--qd-section-bg` (new) |
| Card background | near-white (`#FFFFFF`) | `--qd-surface` (elevated card) |
| Nested / recessed background | `#EFE3D3` | `--qd-surface-recessed` (new) |
| Border | navy @ ~12% | `--qd-border` |
| Border-strong | navy @ ~22% | `--qd-border-strong` (new) |
| Primary (structural) | navy `#12263A` | `--qd-primary` (new) |
| Primary foreground | `#FCFAF4` | `--qd-primary-fg` (new) |
| Primary hover | deeper navy `#0F1F33` | `--qd-primary-hover` (new) — AA-safe primary-btn hover |
| Accent | gold `#C79D43` | `--qd-accent` — background/large-element use |
| Accent-hover | `#B68A30` | `--qd-accent-hover` (new) |
| Accent-soft | `#E5C98A` | `--qd-accent-soft` (new) |
| Accent-tint | `#FAF1DD` | `--qd-accent-tint` (new) |
| Accent text (AA-safe) | navy (light) / gold (dark) | `--qd-accent-text` (new) — accent-emphasis **text** |
| Footer bg | `#0F1F33` | `--qd-footer-bg` (new) |
| Footer bg-2 | `#163149` | `--qd-footer-bg-2` (new) |
| Footer text | `#E9E4D7` | `--qd-footer-text` (new) |
| Footer muted | `#8C99B0` | `--qd-footer-muted` (new) |
| Footer accent | `#D6B56D` | `--qd-footer-accent` (new) |
| Footer border | `rgba(255,255,255,.08)` | `--qd-footer-border` (new) |
| Focus ring | gold @ ~22% | `--qd-focus-ring` |

Dark theme reference (adapted midnight): bg `#0D1322`, surface `#141C2E`, surface-2
`#1B2538`, surface-3 `#232E45`, border `#28324A`, border-strong `#3A476A`, text
`#E8E9EE`, text-muted `#98A0B5`, accent & primary gold `#D4AF6A`, primary-fg
`#0D1322`, footer-bg `#080D1A`. Shadows are re-tuned heavier/darker for dark.

### C. Navbar

- Light / near-white background, **clearly distinct from cards/content** (do not
  reuse the plain card surface as the navbar fill).
- **Optional** translucency + backdrop blur (e.g. translucent surface +
  `backdrop-filter`), only if performance is acceptable, with an **opaque fallback**.
- Subtle **bottom border and/or soft shadow** so it lifts off the page.
- **Active nav item** = `--qd-accent-text` label on an **accent-tint pill** background.
  The pill carries the gold; the **label uses `--qd-accent-text`** (navy in light, gold
  in dark) so it meets WCAG AA. Do **not** use raw `--qd-accent` (gold) for the label —
  gold on the pale light tint is ~2.2:1 and fails AA.
- **Hover** = quiet surface (e.g. section/quiet bg) + `--qd-accent-text` label.
- **No** heavy colored navbar in light mode.

### D. Footer

- The footer is a **dark navy anchor** (`--qd-footer-bg`), an end-cap to the page.
- **Warm off-white** body text (`--qd-footer-text`); **muted blue-grey** secondary
  text (`--qd-footer-muted`).
- **Gold** section headings and link hover (`--qd-footer-accent`).
- Subtle **gradient top hairline** (purposeful, low opacity — an allowed exception).
- Use **dedicated footer tokens** for both themes.
- During implementation, **fix/avoid undefined footer tokens** such as the current
  `var(--qd-text-meta)` reference (it is not a defined token today); replace with a
  real footer-muted token.

### E. Cards and elevation

- **Resting** cards: border + soft shadow (`shadow-sm`).
- **Hover** cards: stronger border (`border-strong`) + stronger shadow (`shadow`) +
  small **`translateY(-2px)`** lift.
- **Mini cards:** smaller lift (~`-1px`), accent-soft hover border.
- **Feature cards:** may deepen the shadow on hover **without** a large move.
- **No scale-up** for content cards.
- Variants to provide: default, hover, **quiet** (recessed surface, no shadow),
  bordered (no shadow).

### F. Motion

- **Two-token motion contract:**
  - **fast** hover transition ≈ **140ms ease** (color/border/background, small
    transforms).
  - **base** transition ≈ **220ms `cubic-bezier(.2,.7,.3,1)`** (popovers, modals,
    theme/background transitions).
- **Subtle only:** card lift ≤ ~2px, floating layers ≤ ~12px translate.
- **No bounce**, no heavy/showy animation.
- **Respect `prefers-reduced-motion`** (extend the app's existing handling).
- Animate `transform` / `opacity` / `box-shadow` / `color` only — never layout
  properties.
- **Never animate Quran/Mushaf text, ayah glyphs, or word-segments.**

### G. Buttons / active states

- **Primary button:** structural **navy** (`--qd-primary`) background with
  `--qd-primary-fg` text; hover → `--qd-primary-hover` (deeper navy / lighter gold in
  dark). (Replaces today's hardcoded `.qd-btn-primary` literals. Do not hover to raw
  `--qd-accent-hover` — parchment-on-gold is only ~3:1.)
- **Accent (gold) `--qd-accent`:** for **backgrounds, pills, large elements, icons,
  section eyebrows**, and dark-surface text. **Not** for small text on light surfaces.
- **Soft button:** accent-tint background + **`--qd-accent-text`** label (tonal
  secondary action). Gold-on-tint fails AA in light; use `--qd-accent-text`.
- **Ghost button:** transparent with a (strong) border; hover → quiet surface +
  `--qd-accent-text`. (Note: nav-links reuse `.qd-btn-ghost` and must stay borderless —
  see Phase 4 plan.)
- **Selected / active states:** accent-tint background + `--qd-accent-text` border/text;
  never a heavy saturated fill (keep it calm).
- **Badges / chips:** a tint distinct from the card surface (not the same fill).

> **Accessibility rule (color):** `--qd-accent` (gold) is a background/large-element
> color. As **small text it fails WCAG AA on light surfaces** (~2.2–2.5:1). Whenever you
> need accent-level emphasis as **text** (active nav, links, soft/selected labels), use
> **`--qd-accent-text`** (navy in light, gold in dark). The app requires AA (§12).

### H. Implementation phasing

Future implementation order (build only when explicitly requested; each phase is
additive and must keep `--qd-bg` / `--qd-surface` / `--qd-border` / `--qd-accent`
working during migration):

- **Phase 1 — Navbar + Footer chrome.** Introduce chrome + footer tokens (both
  themes); light distinct navbar, dark navy footer, active-nav state, fix the
  undefined footer token.
- **Phase 2 — Global tokens / light-dark surface hierarchy.** Add the surface ladder,
  `border-strong`, accent layers, shadow ladder, and motion duration tokens.
- **Phase 3 — Card hover / elevation system.** Resting `shadow-sm` + hover
  `translateY(-2px)` + `shadow` + `border-strong`; add quiet/feature/mini variants.
- **Phase 4 — Buttons / active / selected states.** Tokenize the primary button,
  add ghost/soft behavior, soft focus ring, accent-tint active/selected, chip
  contrast.
- **Phase 5 — Mushaf / study page-specific polish.** Apply the new surfaces and
  elevation to the reader, ayah/study cards, side panels; keep Quran text and word
  rendering untouched.

**Implementation note:** do not paste prototype CSS directly into Angular. Re-author
everything with the app's SCSS partials and OKLCH `--qd-*` tokens.
