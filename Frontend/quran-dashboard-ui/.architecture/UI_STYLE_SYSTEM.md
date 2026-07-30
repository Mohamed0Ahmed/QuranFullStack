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
- `../../DESIGN.md` — visual system (flat parchment + scholarly green, typography,
  flat elevation doctrine, motion, rules)

The official visual identity is the **flat parchment + single scholarly-green**
direction, approved as static comps in `../../docs/design-preview/` (read its README
first — it carries the point-by-point divergence list from the previous identity):
warm parchment surfaces structured by **hairline borders**, fully flat in light (no
resting card shadows, no hover lifts, no gradients, no navbar blur — shadows exist
only on floating layers), **one green accent that is also the primary color**, and
**navy demoted to the footer only**. The app stays **light + dark**: light implements
this direction; dark interim-runs the previous navy + gold values pending a
deliberate later reconciliation. Section 15 below is the superseded navy + gold
prototype contract, retained as history; §16/§17 are the live contract.

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

> Current state: **implemented.** `src/styles.scss` is the single entry point and
> pulls in Tailwind layers plus the `src/styles/` partials, which exist today:
> `_tokens.scss`, `_themes.scss`, `_typography.scss`, `_breakpoints.scss`,
> `_layout.scss`, `_components.scss`, `_words-explorer-layout.scss`,
> `_words-explainer.scss`, `_explorer-tables.scss`, `_explorer-detail-lists.scss`,
> `_forms.scss`, `_utilities.scss` — see `src/styles/README.md` for the exact import order and
> boundary. §16 (color doctrine) and §17 (component contracts) below are the live
> contract for how these partials are consumed; this section still governs file
> organization. Only add a new global partial when it holds a genuinely reusable,
> app-wide pattern — do not scaffold speculative empty files.
>
> `.qd-page-frame` (`_layout.scss`, beside `.qd-container`) is the full-bleed page-frame rule —
> `box-sizing: border-box`, no width cap, column flex, a reserved mobile-stat-bar
> `padding-block-end`. It was `.qd-explorer-frame` in `_words-explorer-layout.scss` until Slice B2
> renamed and moved it (the frame stopped being words-only once Abwab adopted it); the old name is
> kept as a working alias on the same rule so the five existing explorer call-sites are untouched.
> New call-sites use `.qd-page-frame`.

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

Required token categories (the flat parchment + green role set; the authoritative
live values are `_tokens.scss` / `_themes.scss`, governed by §16 — §15B is the
superseded prototype reference):

- page / app background
- section / quiet background
- card background
- nested / recessed background
- text
- muted text
- border
- border-strong
- primary (scholarly green — same hue as accent in light) / primary foreground
- accent (scholarly green) / accent-hover / accent-soft / accent-tint /
  accent-text / accent-fg
- footer: footer-bg / footer-bg-2 / footer-text / footer-muted / footer-accent
  (sage) / footer-accent-hover / footer-border
- danger
- warning
- success
- focus ring
- shadow (flat in light: resting `sm` and hover are `none`; one floating shadow —
  `lg` / `floating`)
- motion durations (fast ~140ms, base ~220ms)
- radius
- spacing scale
- layer scale (stacking order for every fixed/absolute layer in the app), ascending:
  `--qd-z-sticky` (page chrome / sticky headers) → `--qd-z-popover` (selector/filter
  panels) → `--qd-z-floating` (a fixed control floating over page content, e.g. the
  detail-modal-shell restore button) → `--qd-z-mobile-nav` (navbar dropdown + mobile
  menu) → `--qd-z-menu-backdrop` / `--qd-z-menu` (`qd-context-menu`) →
  `--qd-z-modal-backdrop` / `--qd-z-modal` (`.qd-modal-backdrop` / a future direct
  modal-box consumer). **Never write a bare `z-index`** — always reference one of
  these tokens. There are no exceptions: every stacking layer in the app resolves through
  this scale.
  Two caveats the numbers carry, both inherited rather than chosen: `--qd-z-menu` and
  `--qd-z-modal-backdrop` currently resolve to the **same** value, so the rung order above
  is authoritative but the arithmetic does not enforce it — a context menu and a modal
  backdrop rendered as siblings tie, and DOM order decides. And `--qd-z-modal` has no
  consumer yet (`.qd-modal` itself stacks inside its backdrop). Whoever first needs either
  rung to win by number should respace the scale, not add a literal.

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
- Avoid hardcoded colors in component SCSS; re-author adopted design/comp values as
  OKLCH `--qd-*` tokens (never paste comp or prototype hex/inline styles).
- Use the shared **shadow** and **motion duration** tokens; avoid one-off shadows,
  borders, radii, and transition timings unless justified — in light the only real
  shadow is the floating-layer shadow.
- The page **canvas** stays warm parchment (tinted, never pure white); pure `#000`
  is not used. Near-white cards are allowed when paired with the parchment
  background and a hairline border. Structure comes from the **surface ladder +
  hairline borders alone** (the flat doctrine): no resting or hover card shadows,
  no hover lifts, no gradients, no navbar blur — shadows exist **only** on floating
  layers (dropdowns, popovers, modals, drawers).
- The **green accent** token is used sparingly per the allowed-green list (§16.3) —
  focus ring, selection indicators, `--qd-accent-text` text emphasis, icon
  highlights, primary action — never as decoration. Green is also the
  **structural/primary** color in light (`--qd-primary` and `--qd-accent` share the
  same green). **Navy is footer-only** (`--qd-footer-*`); it appears nowhere else
  in light.

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

## 15. Prototype-Derived Implementation Contract (Navy + Gold + Parchment — superseded)

This section was the **implementation contract** for adopting the Real Pages
prototype (navy + gold + parchment) as the visual source of truth. **Status:
superseded.** The identity this contract implemented has been replaced by the
**flat parchment + scholarly-green** direction (approved comps:
`../../docs/design-preview/` — its README carries the point-by-point divergence
list). The live truth is `_tokens.scss` / `_themes.scss` plus §16/§17 below and
`DESIGN.md`; wherever this section conflicts with them — the B color tables, the
translucent/blurred navbar (C), the gold footer accent and gradient hairline (D),
card shadows and hover lifts (E), gold-accent buttons and states (G) — **§16/§17
and `DESIGN.md` win**. The typography roles (A) and the two-token motion contract
(F) remain in force (minus card lifts, which are gone with the flat doctrine). This
section is retained as the historical record of the phased rollout (phases A–H) and
the prototype's reference values; do not rewrite those reference values to green.
The extraction reference is
`../report/ui/real-pages-visual-system-extraction-report.md`.

App themes remain **light + dark**, and every token **must** be defined for both
themes. Light fully implements the green direction; **dark interim-runs the
prototype-derived navy + gold values** (functional; full dark reconciliation to
green is a deliberately deferred later task). Two minimal dark changes shipped with
the restyle: `--qd-accent-fg` is now overridden in dark (navy ink — the dark accent
is still gold), and dark `--qd-chrome-bg` became opaque (blur removed globally).
Shape/motion changes (lift removal, radii, flat navbar/footer geometry) are
theme-neutral and apply to dark too; dark keeps its shadow values.

### A. Typography

- **UI font:** IBM Plex Sans Arabic for Arabic UI chrome; IBM Plex Sans for Latin UI.
- **Weights:** use **400 and 700 only**. The app bundles only 400/700 woff2 faces
  for IBM Plex Sans Arabic and Amiri — no 500/600 (medium/semibold) faces exist.
  Do not use `font-weight: 500` or `600`; the browser would fall back to the
  nearest available face (400 or 700) anyway, so a mid-weight declaration is a
  faux weight that does nothing but confuse the next reader. Nav links, card
  titles, labels, and footer headings use 400 or 700.
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

**Status: implemented — see §16/§17.** The phases below shipped; kept here as the
historical rollout record.

Implementation order used (each phase was additive and kept `--qd-bg` /
`--qd-surface` / `--qd-border` / `--qd-accent` working during migration):

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

## 16. Color doctrine

This is the **live, normative color contract**: it defines what color a UI element
gets by its *role*, not by ad-hoc per-component choice, and it supersedes
component-by-component color decisions. New and changed UI must conform to this
section. The token set it depends on (`--qd-accent-fg`, `--qd-border-accent`,
`--qd-surface-hover`, `--qd-selected-bg`, `--qd-danger-tint`, `--qd-success-tint`,
`--qd-warning-tint`) is live in `_tokens.scss` / `_themes.scss`. Rolling every
existing call-site onto this doctrine was a phased migration; the migration is
**complete** — no *new* code may reintroduce a pattern this section bans.

### 16.1 Role → color table

| Role | Light | Dark | Notes |
|------|-------|------|-------|
| Selected/active background | `--qd-selected-bg` (= `--qd-accent-tint`) | same token | never a solid fill |
| Selected/active label | `--qd-accent-text` (deep green) | `--qd-accent-text` (gold, interim) | must hit AA on the tint |
| Selected/active edge | 1px `--qd-accent` or `--qd-border-accent` | same | hairline, not a fill |
| Solid-accent indicator (dot / 2px bar) | `--qd-accent` fill + `--qd-accent-fg` ink | same | the ONLY solid accent behind pixels |
| Hover fill | `--qd-surface-hover` | same | one token, everywhere — **one documented exception**: the mushaf word-hover wash uses `--qd-mushaf-word-hover-bg`, because `--qd-surface-hover` is imperceptible on the parchment reading canvas (ΔL≈0.022 vs `--qd-bg`) |
| Resting control border | `--qd-border` | `--qd-border` | no accent at rest |
| Primary action | `--qd-primary` + `--qd-primary-fg` (green) | gold-primary, interim pending dark reconciliation | green is also the structural color in light |
| Danger / success / warning text | `--qd-danger` / `--qd-success` / `--qd-warning` on the matching `*-tint` | same tokens | AA-verified, see below |

The 2px solid-accent indicator is the signature **green thread**: one green edge
means *current* everywhere — the active tab, the selected row's inline-start edge,
the mushaf word-selection indicator.

`--qd-accent-fg` is ink for a rare *filled* indicator only — never running text. In
light it is near-white on the solid green (`oklch(0.980 0.007 164.9)`); dark
**overrides it to navy ink**, because the dark accent is still gold (interim) and
gold ink on a gold indicator is unreadable. It exists so a solid-accent indicator
never borrows `--qd-primary` for ink.

**AA verification (green palette, light, all pairs ≥4.5:1):** `--qd-accent-text` on
`--qd-selected-bg` 6.75:1; `--qd-danger` on `--qd-danger-tint` 5.01:1;
`--qd-success` on `--qd-success-tint` 4.58:1; `--qd-warning` on `--qd-warning-tint`
4.58:1; `--qd-accent-fg` on `--qd-accent` 5.72:1. Dark keeps the previously
verified navy + gold pairs (the P1 gate of the color-doctrine plan) until dark
reconciliation. Tokens ship at the values in `_tokens.scss`/`_themes.scss` exactly
as specified; re-verify whenever a token in this table changes.

### 16.2 Grading / ladder

**Surface ladder (role progression, not raw lightness):** parchment page
(`--qd-bg`) → elevated card (`--qd-surface`) → quiet/section grouping
(`--qd-section-bg`) → nested/recessed inset (`--qd-surface-recessed`).
`--qd-surface-hover` sits alongside the ladder as the **one** hover fill — it is not
a ladder step. `--qd-surface-elevated` (the legacy alias that used to equal
`--qd-section-bg`) is **retired** as of P6 of the color-doctrine plan — it no longer
exists in `_tokens.scss`/`_themes.scss`; do not reintroduce it.

**Shadow doctrine (flat):** light has no elevation shadows — `--qd-shadow-sm` and
`--qd-shadow` are `none`; cards rest and hover flat, structured by hairline borders
only, with no lifts. `--qd-shadow-lg` and `--qd-floating-shadow` are the **single
floating-layer shadow**, reserved for dropdowns, popovers, modals, and drawers.
Dark keeps its previous three-step shadow values pending dark reconciliation; new
code must still treat `sm`/hover as non-elevating and reach for the floating shadow
only on floating layers.

**Elevation direction across themes (R1, resolved in P6).** `--qd-surface-recessed`
is the *darkest*/most-inset step in light (`L≈0.945`, darker than `--qd-section-bg`
at `L≈0.979`) and the *brightest* step in dark (`L≈0.302`, brighter than
`--qd-section-bg` at `L≈0.265`) — that cross-theme inversion was **deliberately
left in place** in P6 (the dark values match `DESIGN.md` §2's dark palette; the
light L values shown are the current flat-parchment tones), and no
shipped consumer places `--qd-surface-recessed` directly against `--qd-section-bg`
as a literal "more/less recessed" visual comparison (each consumer nests it one
step below its own local parent surface, not against the ladder's other steps).
What P6 actually fixed was the **ambiguity**, not the raw numbers: the confusing
`--qd-surface-elevated` alias (`= --qd-section-bg` in both themes, an ill-defined
extra "step" consumers reached for inconsistently) is **retired** — deleted from
both themes, zero remaining references — and every former consumer now maps to the
token matching its actual role: `--qd-surface-hover` for hover fills,
`--qd-section-bg` for header-bg/marker backgrounds. The modal sits on
**`--qd-surface`** (near-white card in light / card surface in dark) and gets its
lift from `--qd-shadow-lg` + the dimmed backdrop (R1 **Option B**, locked) — **no**
`--qd-surface-3` token was introduced. A future strict raw-value reordering of
`--qd-surface-recessed`/`--qd-section-bg` remains an option if a consumer ever needs
a direct cross-theme comparison between those two steps; none does today.

### 16.3 The allowed-green list (locked)

Green (`--qd-accent` / `--qd-accent-soft`) may appear **only** as:

1. `:focus-visible` ring/halo (`--qd-focus-ring` / `--qd-ring`).
2. The 2px selection **indicator** bar or the selected **dot** (fill), with
   `--qd-accent-fg` ink if it carries a glyph.
3. A **1px selected/active border** (`--qd-accent` or `--qd-border-accent`).
4. **Text** emphasis via `--qd-accent-text` (active nav, links, soft/selected
   labels, section eyebrows) — never raw `--qd-accent` as small text on light.
5. Footer sage (`--qd-footer-accent`) headings and link-hover.
6. Icon highlights, the mushaf word-selection indicator
   (`--qd-mushaf-word-selection-indicator`), and the two washes it tints — the
   word-hover wash (`--qd-mushaf-word-hover-bg`, 8%; the one exception to the single
   hover fill, since `--qd-surface-hover` is imperceptible on the reading canvas) and
   the selected-word wash (`--qd-mushaf-word-selection-bg`, 28%, with a
   `--qd-mushaf-word-selection-ring` hairline). Both are tints of that indicator on the
   one word under the pointer — never a solid fill, never an ayah-wide fill.
7. The primary action button (`--qd-primary` + `--qd-primary-fg`) — green is now
   also the structural/primary color.
8. The Words explainer benefit callout (`الفائدة`): a soft informational panel using
   `--qd-accent-tint` background + a 1px `--qd-border-accent` edge + `--qd-accent-text`
   label/body (6.74:1 on the tint, AA). It is the one non-selection tinted-green panel,
   scoped to the Words explainer hero (`.qd-explainer-benefit`). Not a solid fill; do not
   reuse it elsewhere without amending this list.

Everything else — chip fills, badge fills, count fills, range badges, selected-row
fills, resting borders — stays **banned as solid green**: use a tint,
`--qd-accent-text`, or a hairline border instead. This list is mirrored in
`DESIGN.md` §2 — keep the two word-identical if either changes.

## 17. Component contracts ("never hand-write these again")

> **Status: implemented.** This section is the **live contract** for the shared
> primitives below. `qd-tabs`, `qd-chip`, `qd-state`, and the skeleton primitives
> (`qd-skeleton-rows`, `qd-panel-skeleton`) are shipped Angular components;
> `.qd-explorer-table` and `.qd-detail-list` are shipped CSS class-family collapses.
> The chip/tab call-site unification, the solid-accent-fill ban, the
> density/motion/radius/ladder cleanup, and the move of the remaining ad-hoc
> text-loading states (dashboard-home, mushaf-page-area) onto
> `qd-skeleton-rows`/`qd-panel-skeleton` all landed — the
> `selected-ayah-section` and `selected-word-section` loading states already used
> `.qd-skeleton` + an sr-only `role="status"` label before P7 and needed no change.
> The phased migration (P1–P7) is complete. Once a contract exists for a pattern,
> **compose it — do not re-style it or hand-roll an equivalent.**

### `qd-tabs`
- **Purpose:** the one tab-strip implementation app-wide (explorer view-mode tabs,
  mushaf ayah-section tabs, inline list tabs).
- **Inputs / roles:** `ariaLabel`, `orientation?='horizontal'`; container is
  `role="tablist"`; each item is `role="tab"` with `aria-selected`, roving
  tabindex, Arrow/Home/End keyboard nav (RTL-aware).
- **Selected / hover / disabled:** selected per §16.1 (tint background +
  accent-text label + hairline/indicator edge); hover = `--qd-surface-hover`;
  disabled is non-interactive and drops out of the roving tab order.
- **Backing classes:** `.qd-tabs`, `.qd-tabs__tab`, `.qd-tabs__tab.qd-is-selected`,
  `.qd-tabs__count`. Compose, do not re-style.

### `qd-chip`
- **Purpose:** the one selectable/informational chip (filters, association
  popovers, count badges) — and, since Abwab Slice B (plan-slice-b.md T412), the one
  removable chip (alias chips in the door-details modal).
- **Inputs / roles:** `selected`, `disabled`, `as?='button'|'a'`, optional
  trailing `count`; `removable?=false` + `removeAriaLabel` for the remove
  affordance, emitting a `remove` output.
- **Selected / hover / disabled:** selected = `--qd-selected-bg` +
  `--qd-accent-text` + `--qd-border-accent` (§16.1) — **no solid green fill**;
  hover = `--qd-surface-hover`; disabled is visually muted and non-interactive.
- **Removable variant:** when `removable` is true the chip renders a static
  `<span>` wrapper (not the `button`/`a` from `as`) with a nested `<button
  class="qd-chip__remove">` carrying the caller's Arabic `aria-label` — nesting an
  interactive remove control inside another `<button>`/`<a>` is invalid HTML, so a
  removable chip is informational, not itself clickable. The remove button is
  tint/hairline only on hover (`--qd-surface-hover` + `--qd-accent-text`), never a
  solid fill.
- **Backing classes:** `.qd-chip`, `.qd-chip--pill`, `.qd-chip--static`,
  `.qd-chip.qd-is-selected`, `.qd-chip__count`, `.qd-chip__remove`. Compose, do not
  re-style.

### `qd-state`
- **Purpose:** the one empty / loading / error presentation.
- **Inputs / roles:** `variant: 'empty' | 'loading' | 'error'`, `message`, optional
  `actionLabel` + `action` output; loading is non-interactive `role="status"`,
  error is `role="alert"`.
- **Recovery action:** an `error` may offer **exactly one** action (Feature 030,
  M3) by supplying an Arabic `actionLabel` — the retry affordance for transient
  transport failures. Without a label the error stays plain text. `empty` and
  `loading` are never interactive. The control is the global `.qd-btn` (with the
  `.qd-btn-secondary` variant); do not hand-roll a retry beside a
  `.qd-error-state`.
- **Visuals:** error uses `--qd-danger` on `--qd-danger-tint` (§16.1), calm per §11
  — not visually aggressive; empty/loading stay on the neutral surface ladder, no
  status color.
- Supersedes ad-hoc `.qd-empty-state` / `.qd-loading-state` / `.qd-error-state`
  usage; those classes remain as the backing layer. Compose, do not re-style.
- **`reserve` (optional, `boolean`, default `false`):** additive input applying the
  §N3 no-layout-shift doctrine (see the Loading/skeleton system entry above; not
  restated here) to this component. On, the **message span** (not the container —
  its padding alone already exceeds one control row, so a container-level
  reservation would be a no-op) carries
  `min-block-size: var(--qd-control-block-size)` — the shared control-geometry
  token family `.qd-checkbox` / `.qd-modal--fixed` already draw from
  (`styles/README.md`'s "size a new reserved slot from these tokens; never
  re-measure the control by hand" rule) — so its box never appears/disappears;
  only its text fades in, opacity only, static under `prefers-reduced-motion`,
  mirroring `qd-detail-modal-shell`'s count span (above). Default off, so today's
  seven call-sites are unaffected. This entry only adds the capability —
  composing `reserve` into abwab's error surfaces is a later slice's task.

### `.qd-explorer-table`
- **Purpose:** the one table implementation for all 5 explorer tables (roots,
  lemmas, stems, unique-words, word-types).
- **Inputs / roles:** per-component SCSS supplies only `grid-template-columns` (+
  column-specific alignment); virtual-scroll/body wiring is unchanged.
- **Selected / hover:** selected row = `--qd-selected-bg` (§16.1); hover =
  `--qd-surface-hover`; density defaults (row height, cell padding, header height)
  live on the base, not per component.
- Compose, do not re-style — a table needing a rule beyond
  `grid-template-columns` is a signal to extend the base, not fork it.

#### Column-header sorting (Feature 030, N8)
- **Backing classes (styled once on the base, never per table):**
  `.qd-explorer-table__sort-button` (+ `.qd-is-sorted`),
  `.qd-explorer-table__sort-label`, `.qd-explorer-table__sort-glyph`.
- **Markup:** a native `<button>` **inside** the `role="columnheader"` element —
  the button gives Enter/Space for free. Non-sortable columns stay **plain text**:
  no button, no `aria-sort`, nothing focusable.
- **Active visual:** `--qd-accent-text` label + a direction glyph (▲/▼) only —
  **no** 2px green-thread bar here (N8-d), no fill, no shadow (flat doctrine
  §16.2, allowed-green list §16.3 #4). Hover = `--qd-surface-hover`;
  `:focus-visible` = the standard ring. The glyph is a separate `aria-hidden`
  span, so it never enters the accessible name; up/down glyphs carry no
  horizontal direction and stay correct under RTL.
- **A11y:** `aria-sort="ascending"|"descending"` on the columnheader, **absent**
  when the column is inactive. The button's Arabic `aria-label` names the column
  **and the state the next click moves to** (`ترتيب حسب X تصاعديًا` /
  `تنازليًا` / `إلغاء الترتيب حسب X`) — it describes the action, while
  `aria-sort` reports the current state.
- **Cycle (3-state, per column):** natural direction → opposite → release
  (param absent = the explorer's default). Counts are naturally descending, text
  naturally ascending. Word Types is the exception: its default IS `occurrences`
  desc, so that header renders active-desc in the default state and its cycle
  collapses to desc ⇄ asc.
- **Behavior/URL live in the feature, not here:** the token grammar, cycle, and
  fail-closed guards are `features/words/models/explorer-sort.ts` +
  `utils/explorer-table-sort.controller.ts`; see the words README for the URL
  contract. Tables stay presentational and emit the next token (or `null`).
- **≤1023px:** the header row is `display: none` in all five table SCSS files, so
  sorting is unreachable there. A compact `<select>` under
  `.qd-explorer-sort-fallback` (hidden ≥1024px) carries the same URL contract.
  Do not delete it, and do not add a second sort control at ≥1024px.

### `.qd-detail-list`
- **Purpose:** the one detail-list implementation for all 10 explorer detail-list
  panels (root/lemma/stem word lists, cross-links, missing-surahs, occurrences,
  type-distribution).
- **Inputs / roles:** per-component SCSS supplies only `grid-template-columns` and
  column extras (e.g. `stem-lemmas` 4-col, `type-distribution` 2-col);
  scroll/pagination wiring is unchanged.
- **Selected / hover:** same tokens as `.qd-explorer-table` (§16.1) — the two class
  families share one visual language.
- Compose, do not re-style.

### `qdAyahCard` (shared ayah-card frame)
- **Purpose:** the one flat frame for ayah-shaped list items — Words ayah matches
  (loaded + loading), Mushaf Similar Ayahs items, Mutashabihat occurrences.
- **Shape:** attribute component (`shared/ui/ayah-card`, host class `qd-ayah-card`)
  applied to the caller's own semantic wrapper (`article`/`li`). It owns only:
  `--qd-ayah-card-bg` background (a dedicated tone recessed below `--qd-surface`, so
  the card reads as a distinct card on the near-white surfaces it sits on), 1px
  `--qd-border` hairline, `--qd-radius-sm`, compact logical padding/gap. No shadow,
  no alternating fill, no hover lift (flat doctrine §16.2). Selected occurrences
  layer a `--qd-border-accent` hairline on the frame.
- **Sacred-rendering boundary:** the frame accepts no Quran/domain model, text,
  word array, match ID, formatter, route, or output, and sets no Quran font. Quran
  text normalization, marker filtering, matched-word calculation, and display
  mapping stay with the consumer (`HighlightedAyahComponent`,
  `toStudyAyahDisplayText`) — never move them into the frame.
- Compose, do not re-style — a consumer needing a different surface/border is a
  signal to extend this contract, not fork it.

### `qd-detail-modal-shell` (global detail-overlay dialog shell)
- **Purpose:** the one dialog shell of the global entity-detail overlay — dialog
  semantics (RTL `role="dialog"`/`aria-modal`, labelled heading, focus trap,
  Escape/backdrop dismissal), the header actions, the closed-state restore
  control, and reference-counted scroll locking. It owns no entity, API, URL, or
  history state; the host decides what the actions mean.
- **Geometry (fixed, both axes):** `inline-size: min(100%, 46rem)` and
  `block-size: min(92dvh, 44rem)` — a *fixed* block-size, never `max-block-size`.
  Switching tabs, paginating, and every loading/empty/not-found pass must repaint
  inside a dialog that does not resize around its flex-centered backdrop. `__body`
  is the **only** scroller (`flex: 1; min-block-size: 0; overflow-y: auto`);
  `__header` is `flex-shrink: 0`. Phone (≤ `$qd-bp-phone-max`) goes
  near-fullscreen: backdrop padding `--qd-space-2`, dialog padding `--qd-space-3`,
  `block-size: min(94dvh, 44rem)`. Shallow states (skeleton, not-found) therefore
  render a tall dialog with empty space — the accepted trade for zero resize. Body
  scroll is locked while open, so a dvh height cannot trap content.
- **Header order (inline-start → inline-end):** Back (depth > 1) · kind chip ·
  `h2` title (`flex: 1`, ellipsis) · ayah-count meta · Close. Back and Close are
  `flex-shrink: 0` + `nowrap` and are the row's anchors: **nothing may move or
  reflow them**. The title is the only shrinkable item, so a count wider than its
  reservation steals width from the title, which its ellipsis absorbs.
- **Header priority is Back/Close > title > count > kind.** The row cannot hold
  every element at phone widths (at 390px the content box is ~326px while
  Back + kind + a 6rem count + Close + gaps need ~378px), so on
  ≤ `$qd-bp-phone-max` the kind marker is `display: none` and the count
  reservation tightens to `4.5rem`. The `h2` still names the entity, and the count
  box stays reserved, so the zero-shift contract below survives. Adding a new
  header element means re-checking this budget at 390px with `depth > 1`.
- **Kind chip (`kindLabel`, optional, `''` = omitted):** hairline `--qd-border` +
  `--qd-text-muted` text, no fill, no shadow (flat doctrine §16.2). Deliberately
  **not** `qd-chip` — that contract carries selectable/interactive semantics, and
  this marker is informational.
- **Count meta (`countText`, optional, `''` = reserved but blank):** the box is
  **always** rendered with a reserved `min-inline-size` (~6rem) and
  `tabular-nums`; only its text fades in (opacity only, static under
  `prefers-reduced-motion`). The box must never appear/disappear — that
  reservation is the whole point, so a count arriving mid-load causes zero layout
  shift. Latin digits, matching the explorer tables.
- **Count a11y (do not "simplify"):** the count lives **outside** the `h2` and
  **outside** both polite live regions — the title live region already re-announces
  on load, so inlining the count into either would double-announce it. The dialog
  is `aria-describedby` the count element instead.
- **Count semantics:** the header count is the **entity-level** ayah count from the
  entity summary, and is entity-stable — it does **not** track the ayah-tab
  `typeCode` filter. On a narrowed lemma/stem ayah tab the visible list total is
  therefore smaller than the header count; that is intended, not a bug. Never
  source it from the tab/filter-dependent `ayahs.totalCount`.
- Both header inputs stay optional with `''` defaults so the shell remains
  presentation-only and callers that supply neither stay valid. Compose, do not
  re-style.

### Loading/skeleton system
- **Purpose:** the one loading representation app-wide — no bespoke text-only
  loading states.
- **Pieces:** `.qd-skeleton` (parameterized by `--qd-skeleton-w` /
  `--qd-skeleton-h`; `--text`/`--block`/`--w-*` shorthands retained as thin
  aliases); `qd-skeleton-rows` (`count`, `rowTemplate` → renders skeleton cells
  inside the real row grid so loading rows match loaded rows exactly);
  `qd-panel-skeleton` (generalized `explorer-panel-skeleton`, `shape: 'lines' |
  'rows' | 'panel'`, default reproduces today's six-line panel skeleton).
- **Roles:** all skeletons are non-interactive, `aria-busy="true"` + `role="status"`
  with an sr-only label, and static under `prefers-reduced-motion`.
- **`shape="panel"` fills its host** (Feature 030, N3): it stands in for a whole
  panel body, so given a host with a block size (a fixed-height panel, or a flex
  slot) the block stretches into it rather than stranding a 3rem bar in a tall box.
  A host with an auto block size is unaffected — it keeps the 3rem default. The
  consumer supplies the slot (flex/height); the skeleton's internals stay the
  primitive's business.
- **No layout shift** (§N3 doctrine): a skeleton must occupy the box its loaded
  content will occupy — same padding, gaps, line boxes and item count. Build the
  mirror out of the **real** loaded classes (and, where the count is knowable
  before the load, the real count) so the two cannot drift; do not hand-derive a
  parallel set of numbers. Reservations apply **only while loading** — loaded
  content always sizes itself.
- Compose, do not re-style — a new loading state is a `shape`/`rowTemplate` input,
  not a new component.

### `.qd-checkbox` / `.qd-check-row`
- **Purpose:** the one checkbox box + label-row pairing app-wide (bulk-select in
  `abwab-tree`/`abwab-cards`, pick-lists in `abwab-relations-modal`/
  `abwab-template-copy-modal`).
- **Shape:** utility classes, not a component — `.qd-checkbox` sizes and colors a
  native `<input type="checkbox">`; `.qd-check-row` is the flex wrapper that pairs
  it with its label at a fixed gap.
- **Geometry / color:** a fixed `--qd-checkbox-size` square (`0.9375rem`, reached
  through the app's own rem scale — same step as `--qd-btn-font-size` and
  `.qd-input`/`.qd-select`'s font-size — rather than a raw px; it equals the
  approved concept's `15px` at the app's unscaled root,
  `abwab-relations-concept.html:84`), `flex: none`, `margin: 0`, and
  `accent-color: var(--qd-accent)` — no new hue, and correct in both themes since
  `--qd-accent` is defined per theme (`_tokens.scss` light / `_themes.scss` dark
  override) with zero `_themes.scss` change needed here.
- **Row gap:** `.qd-check-row` is `display: flex; align-items: center` with a
  single `--qd-space-2` gap between box and label, so the audit's "checkbox far
  from its label" gap cannot be reintroduced per call-site.
- **Accessible name (contract, not optional):** every checkbox composing
  `.qd-checkbox` MUST carry a real `<label for>` or an `aria-label` naming what it
  selects. **Known debt at time of writing:** three of the four existing checkbox
  call-sites (`abwab-tree`, `abwab-cards`, `abwab-relations-modal`) have neither;
  only `abwab-template-copy-modal` supplies an `aria-label`. Paying this down is
  Slice C/D's job when those call-sites compose this class — named here so the
  contract is honest rather than aspirational.
- **Zero consumers at ship time:** this slice adds the classes with no call-site
  changes (plan §2, out of scope here); Slice C/D wires the four existing
  checkboxes onto them.
- **Composing means deleting the local rule, not adding beside it.** Under Angular
  emulated encapsulation a call-site selector like
  `.abwab-relations-modal__pick-row input[type='checkbox']` outranks the global
  `.qd-checkbox` class on specificity — adding the class without deleting the local
  rule leaves the old size/accent in force with no visible change, which reads as
  "done" and is not (the same specificity trap §17's `.qd-modal` entry names for
  the modal geometry work).
- Compose, do not re-style — a call-site needing a different box size or accent is
  a signal to extend this contract, not fork it.

### `.qd-modal` / `.qd-modal--fixed`
- **The base is width-only and scroller-less, and stays that way.** `.qd-modal`
  (`_components.scss`) sets surface/border/radius/shadow/padding and
  `width: min(100%, 36rem)` — no block-size, no scroller, no `overflow`. It is
  what the six abwab modals compose today and must keep composing unmodified.
- **`--fixed` is the opt-in that carries this section's geometry rule** (a fixed
  block-size, never `max-block-size`): `display: flex; flex-direction: column;
  block-size: min(92dvh, 44rem); padding: 0; overflow: hidden`. `dvh`, not `vh`,
  matching every other modal block-size in the app. `44rem` is
  `qd-detail-modal-shell`'s own value — reused deliberately so the app converges
  on one fixed modal height instead of gaining a fourth geometry.
- **Slot contract:** `.qd-modal__head` / `.qd-modal__foot` are `flex-shrink: 0`
  with their own `--qd-space-5` padding (the same step the base's uniform
  padding uses, so composing surfaces keep today's rhythm); `.qd-modal__body`
  is `flex: 1; min-block-size: 0; overflow-y: auto` — the **only** scroller —
  with `padding-inline: var(--qd-space-5)` and `scrollbar-gutter: stable` so
  the reserved scrollbar track cannot reflow content width once the list
  crosses the scroll threshold (the same class of defect as audit item 3, one
  level down). **`__body` has no block padding by design** — the gap at the
  head/body and body/foot seams comes entirely from `__head`'s
  `padding-block-end` and `__foot`'s `padding-block-start`, so `__foot` is
  load-bearing for that gap, not an optional slot. A `--fixed` dialog composed
  without a foot must give `__body` its own `padding-block-end` or its last
  content line sits flush against the dialog's bottom edge. Phone
  (≤ `$qd-bp-phone-max`) tightens to `--qd-space-3` padding and
  `block-size: min(94dvh, 44rem)`, mirroring `qd-detail-modal-shell`'s own
  phone rule — but **not** its backdrop padding: `.qd-modal-backdrop` is the
  shared base for all twelve modal consumers, so `--fixed` does not touch it.
- **Why opt-in and not a base change:** `.qd-modal.explorer-detail-modal` sets
  `max-height: min(90vh, 36rem)` but never `height`/`block-size`. A block-size
  added to the base would therefore also apply to it — silently clamping the
  five shipped words detail modals that use that variant. `--fixed` is how a
  consumer reaches this section's geometry rule without that collision; the
  base must never gain a block-size.
- **Specificity trap when composing:** the same trap named in the `.qd-checkbox`
  entry above — an existing call-site rule can outrank `.qd-modal--fixed`
  under Angular emulated encapsulation. Three abwab modals already set an
  inner `max-block-size` that becomes redundant, and arguably wrong, once the
  modal body is the single scroller:
  `abwab-sections-modal.component.scss:5` (14rem list),
  `abwab-template-copy-modal.component.scss:43` (13rem pick-list), and
  `abwab-relations-modal.component.scss:203` (11rem pick-list — measured at
  T401 to be doing the scrolling `__body` is supposed to do). Composing the
  modifier means **deleting** these local caps, not adding the class beside
  them.
- **Convergence trigger for `.qd-modal.explorer-detail-modal` (required, not
  optional):** `--fixed` deliberately reuses `qd-detail-modal-shell`'s own
  `44rem` rather than introducing a new height, so no new modal height enters
  the system by this change. `.qd-modal.explorer-detail-modal` is therefore
  the **one remaining** violation of this section's rule — `max-height` on
  desktop instead of a fixed block-size. This section tolerates
  `qd-detail-modal-shell` as a genuinely separate component; it must not
  silently tolerate a second, permanent exception. **The next change that
  touches any of the five words detail modals' geometry converges all five
  onto `--fixed` and deletes the `vh` hold-out.**
- **Zero consumers at ship time:** this slice adds `--fixed` and its slots with
  no call-site changes and no markup restructuring (plan §2, out of scope
  here); applying it to the six abwab modals is Slice C's job.

### `qd-context-menu`
- **Purpose:** the one row/node context-menu shell app-wide (Abwab's doors tree row menu
  and the templates workshop's node tree row menu — the two pre-existing copies this
  primitive replaces).
- **Inputs / outputs:** `position: {x, y}` (positions the menu via
  `[style.left.px]`/`[style.top.px]`, unchanged from both prior copies); `menuTestId` /
  `backdropTestId` (both `string`, required) — **non-negotiable**, because 4 Vitest
  assertions and ~8 Playwright assertions select `abwab-page-context-menu` /
  `abwab-page-ctx-backdrop` / the templates-page equivalents by test id, and inputs are
  what let the extraction keep them byte-identical; `dismissed` output, emitted on
  backdrop click and on `Escape`.
- **Shape:** a `position: fixed; inset: 0` transparent backdrop at
  `--qd-z-menu-backdrop`, and a positioned `role="menu"` box at `--qd-z-menu`. Items are
  **projected content** (`<ng-content>`) carrying their own test ids, labels, and click
  handlers — the primitive learns nothing about doors or template nodes.
- **Item styling lives outside this component, on purpose:** hover, the `:focus-visible`
  ring, and the `--danger` item variant are the global `.qd-context-menu__item` /
  `.qd-context-menu__item--danger` classes in `_components.scss` (the `.qd-tabs__tab`
  precedent) — content projected via `<ng-content>` is compiled in the *consumer's*
  template under Angular's emulated encapsulation, so a rule scoped to this component's
  own stylesheet would never reach it. Consumers apply the classes directly to their own
  projected buttons.
- **Escape dismissal is document-level** (`@HostListener('document:keydown.escape')`,
  copying `top-navbar.component.ts`), not bound to the menu element, because none of the
  four paths that open a menu (right-click, the row's `⋯`, or either page's keyboard
  path) puts focus inside it — an element-bound handler would never receive the key.
  **This is the one place this primitive is not literally behavior-preserving:** neither
  prior copy dismissed on `Escape`. Deliberate, additive a11y gain, not a bug.
- **Three gaps this primitive deliberately did not fix** — named so a future reader does
  not assume a shared, contracted primitive already covers them:
  1. **No viewport clamping.** Both prior copies positioned from raw pointer coordinates
     with no bounds check; the faithful extraction preserves that, so a menu opened near
     the inline-start edge under RTL can still overflow the viewport.
  2. **No focus management into the menu.** Neither prior copy moved focus into it on
     open; adding that changes keyboard behavior on a shipped surface and belongs to
     Slice G's row-menu keyboard-path work, not this extraction.
  3. **The `--danger` item's rest-state color is not unified.** The two prior copies
     were not byte-identical here: the doors page's danger item was plain-colored until
     hover (`--qd-danger-tint` background + `--qd-danger` text on `:hover` only, the
     `abwab-side-panel__op--danger` app-wide idiom); the templates page's danger item
     read `--qd-danger` at rest, unconditionally. The shared `--danger` modifier carries
     the doors page's hover-only idiom; the templates page keeps a short page-scoped
     override (`abwab-templates-page.component.scss`) so its own rendering stays
     unchanged. Reconciling the two recipes into one is a later slice's call, not this
     extraction's.
- Compose, do not re-style.

### Truncatable entity names
- **Purpose:** the one rule for any entity name that can overflow its row/column
  (door names, template names, and their eleven abwab render sites at the time of
  writing — trees, cards, the archive view, move/relations/copy pick-lists, the
  side panel, the sections modal, the templates page).
- **The app's rule is flexible-with-ellipsis, not a hard column.** Every existing
  precedent truncates a name/title inside a flexible item —
  `detail-modal-shell.component.scss:28-35`'s `__title` (`flex: 1; min-inline-size:
  0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap`) is the
  canonical shape, and `abwab-tree.component.scss:70-75`'s own `__name` already
  does the same. **This section states that as the rule, in both directions:** a
  name column composes `.qd-truncate` (`_utilities.scss`) on a flex item that owns
  `flex: 1` (or a reserved floor via `flex: 1; min-inline-size:
  var(--qd-name-min-inline-size)`, `_tokens.scss`, for a column that must not
  shrink to nothing under sibling pressure — see its derivation comment there). A
  **hard, fixed `inline-size` name column is a per-surface exception, not a second
  house rule** — it trades away exactly the flexibility every other truncated name
  in the app relies on, so a surface reaching for one must write down, at that
  call-site, why its layout cannot tolerate a shrinking name column the way every
  other one does. The audit that produced this entry found a request for a fixed
  name width where every existing precedent was flexible; this paragraph is where
  that gets settled once, so a later reviewer does not re-litigate it per
  call-site.
- **Mandatory `[title]`, not optional:** any element composing `.qd-truncate` (or
  otherwise capable of visually truncating) MUST carry `[title]="fullName"` so the
  full name is available on hover/long-press once the ellipsis hides it —
  precedent `word-type-filter.component.html:57`:
  `<span class="word-type-filter__child-label" [title]="child.label.ar">{{
  child.label.ar }}</span>`. A truncated name with no `[title]` is a contract
  violation, not a style nit.
- **Known debt named honestly:** none of the eleven abwab name-render sites
  compose `.qd-truncate`, the reserved-minimum token, or `[title]` yet as of this
  entry — three of the eleven are missing the ellipsis half entirely and all
  eleven are missing `[title]`. This slice ships the primitive and the rule only;
  wiring the eleven sites onto it is Slice C/D's job, named here so it reads as
  tracked debt, not an oversight.
- **Zero consumers at ship time:** `.qd-truncate` and `--qd-name-min-inline-size`
  ship with no call-site changes (plan §2, out of scope here).
- Compose, do not re-style — a surface that seems to need a fixed name column
  should re-read the paragraph above before reaching for `inline-size` instead of
  `.qd-truncate`.

### Viewport reservation
- **Purpose:** a page's content region reserves a full viewport below the navbar, so
  no state change (loading → loaded → empty → error) resizes the page frame. Slice
  B2's item 4; the shell already made the page *scroll* one footer-height
  (`.qd-shell-viewport { min-height: 100vh }`, `_layout.scss`) — this is the
  separate, narrower claim that a page's own content fills what the shell reserves.
- **The arithmetic is always `100dvh` minus the navbar token, never a footer
  number:** `min-block-size: calc(100dvh - var(--qd-navbar-block-size))`. A
  `--qd-footer-block-size` token was considered and refused — the footer has no
  stable height (`qd-footer.component.html`'s health indicator has three branches,
  one with a retry button, all free to wrap at narrow widths), so a token for it
  would be a magic number wearing a token's clothes. The reservation instead lets
  the footer sit wholly below the fold, unconditionally.
- **Requires `box-sizing: border-box` on the element carrying the reservation.**
  The app has no global `border-box`; without it the reservation overshoots the
  viewport by that element's own padding under the default `content-box`.
  `.qd-page-frame` (`_layout.scss`) already carries `border-box`, which is why the
  page-frame rename (§2) is a prerequisite of this pattern, not a coincidence.
- **Abwab-local for now.** The reservation lives on `abwab-page.component.scss`
  (`.abwab-page__frame`), not on the shared `.qd-page-frame` rule — promoting it
  there would silently reserve a viewport on all five explorer pages, which nobody
  has measured. **Generalize it only when** a second feature's page needs the same
  state-stability guarantee; at that point promote the rule onto `.qd-page-frame`
  itself and re-verify the five explorer pages' bottom-of-page geometry (their
  existing `padding-block-end` mobile-stat-bar reservation interacts with any
  `min-block-size` added alongside it) rather than assuming the abwab measurement
  transfers.
- **Reserving space is not enough — the content must stretch into it.** The
  reservation on the frame only bounds the frame; a child card still collapses to
  its own content unless something in the chain between frame and card carries
  `flex: 1; min-block-size: 0`. Abwab's chain: `.abwab-page__frame` (the
  reservation) → `.abwab-page__layout` (`flex: 1; min-block-size: 0`) →
  `.abwab-page__main` (`align-self: stretch`, its own column flex context) →
  `.abwab-page__tree-card` (`flex: 1; min-block-size: 0`, replacing a fixed
  `min-height`). The row's `align-items: flex-start` stays put rather than becoming
  `stretch` — `.abwab-page__side` is `position: sticky`, and stretching the row
  would give the sticky aside zero scroll travel, silently breaking it. Stretch the
  main column with `align-self`, not the row with `align-items`.
