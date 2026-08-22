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
direction — approved as static comps, adopted in full, and the comps retired once the
shipped app became the reference: warm parchment surfaces structured by **hairline borders**, fully flat in light (no
resting card shadows, no hover lifts, no gradients outside the fixed multi-door Mushaf word and
ayah-marker highlight, no navbar blur — shadows exist only on floating layers), **one green accent that is also the primary color**, and
**navy demoted to the footer only**. The app stays **light + dark**: light implements
this direction; dark interim-runs the previous navy + gold values pending a
deliberate later reconciliation. Section 15 below is the superseded navy + gold
prototype contract, retained as history; §16/§17 are the live contract.

When this file and `DESIGN.md` describe the same thing, `DESIGN.md` wins on the
visual choice; this file governs how that choice is implemented and reused.

The approved **Golden UI** system now sits above both for anything it covers: the
permanent visual authority is `.architecture/golden-ui/`, the short mandatory rule set is
`../FRONTEND_UI_RULES.md`, and §18 below records the foundation mechanics (tokens, bands,
page intents, gutters, grids, hover/selection semantics, and the `check:golden-ui` gate).
Read §18 before §15/§16 when the two appear to disagree.

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
> `_forms.scss`, `_utilities.scss`; `src/styles.scss` is the executable import order. §16
> (color doctrine) and §17 (component contracts) below are the live
> contract for how these partials are consumed; this section still governs file
> organization. Only add a new global partial when it holds a genuinely reusable,
> app-wide pattern — do not scaffold speculative empty files.
>
> `.qd-container`, `.qd-page-frame` and `.qd-explorer-frame` were the pre-Golden page frames. All
> three were **deleted in Phase 11** once `rg` proved zero template consumers. Every route composes
> `.qd-page-shell` plus one named page intent
> (§18.4), and that rule now owns the only `padding-inline: var(--qd-page-gutter)` declaration in
> the stylesheet tree.

## 3. Naming Convention

Use the project prefix **`qd-`** for all reusable global UI classes.

Examples:

```text
qd-page          qd-btn           qd-table
qd-shell         qd-btn-primary   qd-modal
qd-page-shell    qd-btn-secondary qd-sidebar
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
- info
- success
- focus ring
- shadow (flat in light: resting `sm` and hover are `none`; one floating shadow —
  `lg` / `floating`)
- motion durations (fast ~140ms, base ~220ms)
- radius
- spacing scale
- layer scale (stacking order for every fixed/absolute layer in the app), ascending:
  `--qd-z-sticky` (in-page sticky headers with no descendant menus of their own, e.g.
  `mushaf-header-navigation`) → `--qd-z-popover` (selector/filter panels) →
  `--qd-z-floating` (a fixed control floating over page content, e.g. the
  detail-modal-shell restore button) → `--qd-z-mobile-nav` (`.qd-navbar` itself — sticky,
  Slice B2 T901/T903 — plus its dropdown and mobile menu, all three on the same rung so the
  sticky navbar's own stacking context never clamps its own menus below what they declare)
  → `--qd-z-menu-backdrop` / `--qd-z-menu` (`qd-context-menu`) → `--qd-z-modal-backdrop` /
  `--qd-z-modal` (`.qd-modal-backdrop` / a future direct modal-box consumer) → and on upward.
  **`src/styles/_tokens.scss` is the authoritative scale**: the `--qd-z-*` tokens are declared
  there in ascending order, each with the consumer it exists for, and a rung added there is part
  of the scale whether or not this paragraph names it — so read the tokens, do not trust this
  transcription to be complete. **Never write a bare `z-index`** — always reference one of these
  tokens. There are no exceptions: every stacking layer in the app resolves through this scale.
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
  no hover lifts, no gradients outside the fixed multi-door Mushaf word and ayah-marker
  highlight defined in §13, no navbar blur — shadows exist **only** on floating
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
- The linking ayah-selection card is an approved Compact exception: it may reduce only its displayed
  word size by `--qd-s-2`, use `1.55` line-height, and add `--qd-s-2` block padding to its
  background-only word highlights. It must not change text, glyphs, word boundaries, or source data.
- Door highlighting is an approved visual exception: a highlighted word uses its assigned
  categorical door token behind unchanged Quran text, inset by the 10px
  `--qd-mushaf-door-highlight-inset` from both block edges. A highlighted ayah marker keeps its glyph
  unchanged over an assigned-color disc with no resting border. A word or ayah marker belonging to
  multiple selected doors uses fixed gradients independent of assigned colors: light gradients for
  both the word background and marker disc. In forced-colors, solid block
  edges identify a single-door word, solid outlines identify single-door markers, and dashed
  perimeters identify multi-door words and markers. It must not change or animate text, fonts, glyph
  shape, word boundaries, or line metrics.

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
**flat parchment + scholarly-green** direction. The live truth is
`_tokens.scss` / `_themes.scss` plus §16/§17 below and
`DESIGN.md`; wherever this section conflicts with them — the B color tables, the
translucent/blurred navbar (C), the gold footer accent and gradient hairline (D),
card shadows and hover lifts (E), gold-accent buttons and states (G) — **§16/§17
and `DESIGN.md` win**. The typography roles (A) and the two-token motion contract
(F) remain in force (minus card lifts, which are gone with the flat doctrine). This
section is retained as the historical record of the phased rollout (phases A–H) and
the prototype's reference values; do not rewrite those reference values to green.
**This section is itself the surviving extraction record** — the original extraction report was
a local working artifact and was never committed to this repository, so nothing else holds the
prototype's values.

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
| Edit / copy semantic actions | `--qd-warning` on `--qd-warning-tint` / `--qd-info` on `--qd-info-tint` | same semantic roles | restrained tints; labels remain authoritative |
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
9. The router navigation progress bar (`qd-nav-progress`, §17): a 2px `--qd-accent`
   hairline fixed to the top of the viewport while a lazy route's chunk is still
   downloading (200ms show-delay, so warm navigations never flash it). A loading
   affordance in the shell chrome — it reuses the green-thread thickness but marks
   "arriving", never "current", and never competes with in-content green.

Everything else — chip fills, badge fills, count fills, range badges, selected-row
fills, resting borders — stays **banned as solid green**: use a tint,
`--qd-accent-text`, or a hairline border instead. This list is mirrored in
`DESIGN.md` §2 — keep the two word-identical if either changes.

## 17. Component contracts ("never hand-write these again")

> **Status: implemented.** This section is the **live contract** for the shared
> primitives below. `qd-tabs`, `qd-chip`, `qd-nav-progress`, and the
> skeleton primitives
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
  mushaf ayah-section tabs, inline list tabs, and — at `layout='grid'` — a modal's
  section strip).
- **Inputs / roles:** `ariaLabel`, `orientation?='horizontal'`,
  `layout?='inline'|'grid'|'tracks'`;
  container is `role="tablist"`; each item is `role="tab"` with `aria-selected`,
  roving tabindex, Arrow/Home/End keyboard nav (RTL-aware).
- **Selected / hover / disabled:** selected per §16.1 (tint background +
  accent-text label + hairline/indicator edge); hover = `--qd-surface-hover`;
  disabled is non-interactive and drops out of the roving tab order.
- **Backing classes:** `.qd-tabs`, `.qd-tabs--vertical`, `.qd-tabs--grid`,
  `.qd-tabs--tracks`, `.qd-tabs__tab`, `.qd-tabs__tab.qd-is-selected`,
  `.qd-tabs__count`. Compose, do not re-style.
- **`.qd-tabs__count` is a reserved slot, not a number that sizes itself.** Its
  `min-inline-size` is `calc(2ch + 2 * var(--qd-tabs-count-padding-inline))` — two tabular
  digits plus the badge's own inline padding — so a count going **from one digit to two** moves
  nothing in the strip that holds it. That is the exact guarantee, and no more: a third or fourth
  digit does widen the badge. It is a real reservation only where counts stay small (the Mushaf
  study strip's similarity counts). `abwab-toolbar`'s `totalRootCount()` badge is routinely three
  or four digits, and the floor was deliberately not widened for it, because a 4ch floor would pad
  every count badge in the app to fix one strip. It does not need to be: Phase 10 moved the Abwab
  section strip off the count-driven `inline` modes onto `tracks`
  (`--qd-tabs-track-floor: 8.5rem`), and track sizing ignores item intrinsic width entirely, so
  that strip is count-independent regardless of this floor. `--empty` (known zero) and `--unknown` (value not yet
  known) drop the pill background and keep the slot; `--unknown` also wins over the
  selected-tab count treatment, because a selected tab can be the one still loading. A
  count-bearing tab therefore keeps its badge element **mounted at all times** and varies only
  its appearance; unmounting it is what made the Mushaf study strip shift sideways on every
  ayah change.
- **`layout='tracks'` — the equal-width wrapping strip (`.qd-tabs--tracks`).** The declared
  alternative to the count-driven `inline` modes, and the one that satisfies the tabs contract:
  `display: grid; grid-template-columns: repeat(auto-fit, minmax(min(var(--qd-tabs-track-floor, 6.25rem), 100%), 1fr)); gap: var(--qd-space-2)`.
  Three things are load-bearing:
  - **`auto-fit` plus a floor, never a column count.** The floor decides how many equal tracks a
    row holds and therefore when the strip wraps; a container with room for more balanced tracks
    is free to use them. There is deliberately **no** maximum-columns rule — that would be a
    second, invisible geometry contract, and it is `--grid` (fixed columns, tracks kept whether
    filled or not) that a call-site wanting an exact count already asks for.
  - **The floor is sized from the longest label**, not from a grid ideal: `6.25rem` clears the
    widest details/study tab label plus the tab's own `--qd-space-3` inline padding. A call-site
    with longer labels raises `--qd-tabs-track-floor` rather than adding local width CSS.
  - **`white-space: nowrap` + `overflow: hidden` on the tab.** The nowrap is a **readability
    choice, not a geometry requirement**. The widest-word collapse that folded `لم يذكر فيها` onto
    two lines is a property of the old flex modes (`inline` / `--segmented`), where a tab's
    *intrinsic* width feeds the layout; in `tracks` the track minimum is a fixed length, so no
    item's intrinsic width participates in track sizing at all and a wrapping label would be
    geometrically harmless — it would only grow the row's block size. Choosing nowrap therefore
    transfers the fit obligation onto `--qd-tabs-track-floor`, and `overflow: hidden` is what keeps
    that obligation from becoming a correctness bug: a label wider than its track is clipped inside
    the tab instead of spilling ink into the ancestor scrollable-overflow region. The tab's own
    `:focus-visible` outline and the selected state's `inset` box-shadow thread are painted by the
    tab itself and are **not** clipped by its own `overflow`. The floor is thus a readability knob
    — raise it when labels are long enough to be cut — never the thing that makes the mode
    overflow-proof.
  - **No `overflow-x` in this mode, ever** — wrapping is the overflow answer, so the strip never
    grows an inline scroller. Since Phase 11 no `.qd-tabs*` rule carries `overflow-x` at all; the
    RTL-hostile `--scrollable` mode this contract replaced no longer exists.
  - **Consumers that raise the floor:** `word-type-details-panel` passes `tabsTrackFloor="9.5rem"`
    to `qd-details-panel-shell`, which renders it as `[style.--qd-tabs-track-floor]` on the details
    `qd-tabs[qdDetailsTabs]` host alone — no stylesheet rule sets the floor, so projected sub-tab
    rows keep the `6.25rem` default. It is raised because its `lemma`/`stem` kinds render the longest labels in the
    product (`كلمات الصيغة المعجمية`, 123.7 px at `--qd-type-body`, plus the tab's two
    `--qd-space-3` insets = 147.7 px). At the `6.25rem` default those three tabs share one 319 px
    row in the sub-1080 modal and each label overflows its tab by ~18–23 px per side.
- **`layout='grid'` — the wrapping fixed-column strip (`.qd-tabs--grid`, added by
  ux-slice-m).** For a tablist that must show **every** option at once rather than
  one scrolling row: `display: grid; grid-template-columns: repeat(var(--qd-tabs-grid-columns, 5), minmax(0, 1fr)); gap: var(--qd-space-2)`.
  A horizontally scrolling row was the rejected alternative — it is hostile in RTL
  and unreachable by keyboard without a scroll-into-view crutch, whereas a grid
  simply wraps. Three properties are load-bearing:
  - **`minmax(0, …)`, never a bare `1fr`.** A grid item's `min-width: auto` lets a
    long label push its track past the column width instead of ellipsizing inside
    it, so a name that should truncate silently widens the strip instead. The same
    class of failure as the specificity traps below: nothing looks broken until the
    data gets long.
  - **The tracks exist whether or not they are filled**, which is what makes the
    cells *equal-width* rather than *proportional* — two tabs render as two
    full-width cells beside three empty tracks, not two stretched halves. A call-site
    wanting the stretched behaviour wants `--inline`, not this.
  - **Keyboard nav still follows `orientation`, not `layout`.** A grid strip keeps
    the horizontal Arrow/Home/End model, moving linearly through DOM order across
    the wrap. A row-aware Up/Down model (±one row) is deliberately **not** built:
    it would be a second keyboard contract for one consumer, and linear traversal
    already reaches every cell.
  - **Consumer (exactly one):** `abwab-move-picker`'s section strip. Its cells
    measure **150 px** — the `wide` shell's 832 px, minus 2 px of border and the
    48 px of shell header padding, minus four `--qd-space-2` gaps, over five
    tracks — so the ~15-section product ceiling is exactly three rows. The strip
    sits in the shell's header slot (`flex-shrink: 0`), so it needs **no `max-block-size` and no
    scroller of its own**, and the shell body stays the dialog's only scroller. A call-site needing a different column count sets
    `--qd-tabs-grid-columns` and records its own arithmetic the same way.
- **Extending the tab's visual state:** a call-site adding a cue the primitive does
  not carry puts it on a **feature-local class beside** `.qd-tabs__tab`
  (`abwab-move-picker__section`), never by re-styling
  `.qd-tabs__tab` from the consumer's own stylesheet. The move picker's active cell
  adds `font-weight: 700` that way, because §17's tint-plus-accent-border selected
  state is a colour cue and an active state must not rest on colour alone.
- **Count meta (`.qd-tabs__count`):** rendered by the call-site's template, not by `qdTab` —
  the directive is host-bindings-only and cannot project a child element. Latin digits,
  `tabular-nums`; **always** rendered. At zero, `.qd-tabs__count--empty` drops the filled
  pill (`background: transparent`, `_components.scss`) so the distinction is shape, not
  transparency — the digit keeps full-strength muted ink, measuring 4.82:1 against
  `--qd-bg` in light (7.08:1 dark); on a **selected** tab the selected-state count rule
  outranks the modifier, so a selected zero renders like any other count at 7.55:1 light
  (8.21:1 dark). Ratios computed from the oklch tokens; re-measure if any of those tokens
  move. The visible digits are `aria-hidden="true"`; the tab's own `aria-label` carries
  the accessible count.

### `qd-chip`
- **Purpose:** the one selectable/informational chip (filters, association
  popovers, count badges) — and the one removable chip (alias chips in the
  door-details modal).
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
- **Clickable label (`labelClickable`, opt-in, default off — Slice D):** renders the
  chip's label as a nested `<button>` (`.qd-chip__label--clickable`, emitting
  `labelClick`, named by `labelAriaLabel`), so a removable chip can carry **two**
  independent controls — a name that acts and an `×` that removes. Honored **only**
  in the removable branch, and that guard is the contract, not caution: the other
  two branches *are* a `<button>`/`<a>`, and the removable branch's static `<span>`
  wrapper is precisely what makes the nesting legal. This extends the base rather
  than forking it — the alternative, hand-rolled buttons inside each consumer's
  `ng-content`, would have every future consumer re-inventing focus and hover
  styling. Implementation note for anyone editing the template: the label wrapper is
  chosen by an `@if`, so the projected content is declared **once** in its own
  `ng-template` and rendered through an outlet in both arms — two `<ng-content>`
  elements sharing one selector would leave the second slot permanently empty while
  every element-type assertion still passed.
- **Backing classes:** `.qd-chip`, `.qd-chip--pill`, `.qd-chip--static`,
  `.qd-chip.qd-is-selected`, `.qd-chip--disabled`, `.qd-chip__count`,
  `.qd-chip__remove`, `.qd-chip__label`, `.qd-chip__label--clickable`. Compose, do
  not re-style. `.qd-chip--disabled` is what delivers the disabled state named above
  on the two branches that are not a `<button>`: the removable `<span>` and the
  anchor cannot carry the native `disabled` attribute, so `chip.component.html`
  binds `[class.qd-chip--disabled]="disabled()"` on both, and `_components.scss`
  pairs the class with `:disabled` in one rule
  (`.qd-chip:disabled, .qd-chip.qd-chip--disabled { cursor: not-allowed; opacity:
  0.5; pointer-events: none; }`). Omit the class on those branches and a disabled
  chip still takes hover and clicks.

### `qd-confirm-dialog`
- **Purpose:** the one confirmation dialog — a decision that interrupts and needs an explicit
  yes/no. Supersedes hand-written `role="alertdialog"` blocks: those get the role right and the
  focus handling wrong, and each one drifts from the next. **Do not hand-write these again.**
- **Inputs / roles:** `open`, `titleText`, `confirmLabel`, `cancelLabel`,
  `tone?: 'default' | 'danger'`, `busy?`, `confirmDisabled?`, `testIdPrefix?`; outputs
  `confirmed`, `cancelled`. Container is `role="alertdialog"` + `aria-modal="true"`, labelled
  by its own title.
- **`testIdPrefix` renames all four testids** (`{prefix}`, `-backdrop`, `-confirm`, `-cancel`)
  and defaults to `qd-confirm-dialog`. **Pass it whenever a page can host more than one
  confirm** — otherwise two dialogs on one page answer the same selector and every assertion
  against them is ambiguous.
- **Transient, never URL-addressable — no consumer may write a URL key for a confirm dialog.**
  A destructive confirm must be re-initiated, never restored from a URL: a link that reopens
  "are you sure you want to delete this" is a link that can be sent to someone.
- **Body is projected** (`<ng-content>`), so a consumer composes whatever the decision needs — a
  path, a selector, an inline `qd-error-state`. The dialog owns the framing and the
  dismissal routes; it never owns the content.
- **Behavior:** focus trapped (`cdkTrapFocus` + auto-capture); **initial focus on CANCEL** — the
  dialog interrupts, so a reflexive Enter must produce the safe answer. `Escape` and a backdrop
  click both emit `cancelled`. `busy` disables BOTH buttons (a decision in flight is not
  cancellable into an inconsistent state either), carries the house busy affordance
  (`aria-busy="true"` on the confirm button, the same signal the skeletons use),
  blocks a second `confirmed` emission, and suppresses `Escape` and backdrop dismissal for the
  same reason it disables cancel; `confirmDisabled` disables confirm alone, for a decision that
  is not yet complete.
- **Visuals / RTL:** `tone: 'danger'` maps confirm to `--qd-danger` per §16.1 — scoped to this
  component rather than a global `.qd-btn-danger`, since a new global button variant is a
  design-system decision, not this dialog's. Logical properties only; scroll locked through the
  shared `modal-scroll-lock`.
- **Not a modal shell.** Authoring modals (a form plus its dirty guard) keep their own shell —
  that is a different contract. This is for confirmations only.
- **Retrofit complete.** Every destructive confirmation in the app now composes this primitive:
  the abwab page's single and bulk archive confirms, the sections modal's delete, the relations
  modal's relation-delete, and the templates page's template- and node-delete. The relations one
  is the primitive's first **new** consumer rather than a retrofit — no hand-written confirm
  existed there; the chip deleted on click — and it nests above an open modal, the sections
  modal's precedent, which needs no focus-trap gating on the host. The only surviving hand-written
  `role="alertdialog"` blocks are the three **dirty-discard strips** (door, sections, and
  template-node modals) — those are in-shell footers guarding unsaved work, not interrupting
  dialogs, and they deliberately stay where the unsaved work is. A new hand-rolled confirm is a
  defect.

### `qd-state` — retired
> **Deleted in Plan 7 Phase 11 (D39).** The adapter that conflated empty / loading / error behind a
> `variant` flag no longer exists. Its five replacements are the F12 owners in §19.3:
> `qd-panel-skeleton`, `qd-refreshing-indicator`, `qd-empty-state`, `qd-error-state` and
> `qd-notice`. `npm run check:golden-ui` fails on any `<qd-state` or `QdStateComponent` reference
> and on `src/app/shared/ui/state/` reappearing.
- **What moved where.** `variant="loading"` → `qd-panel-skeleton shape="text"`;
  `variant="empty"` → `qd-empty-state`; `variant="error"` → `qd-error-state`, whose `severity`
  now makes the announcing explicit: `read` is a scoped retry block with **no** alert role, `write`
  is the only `role="alert"` and never clears a draft. That split is the point of the retirement —
  the adapter announced every failure as an alert.
- **The `qd-state-*` test ids survive on the owners**, deliberately: `testId="qd-state-error"`,
  `qd-state-empty`, `qd-state-loading`, `qd-state-action`. They are stable call-site identifiers,
  not evidence of a surviving component.
- **The `.qd-state--reserve` / `.qd-state--reserve-empty` / `.qd-state__message` /
  `.qd-state__action` classes also survive**, in `_components.scss`, as the backing layer for the
  owners' `reserve` input. Renaming them would be a broad class rename with no behavioural gain and
  is out of Plan 7's scope.
- **`reserve` semantics are unchanged** and now belong to the owners. The message span (not the
  container) carries `min-block-size: var(--qd-control-block-size)`, so its box never
  appears/disappears; only its text fades in, opacity only, static under
  `prefers-reduced-motion`.
- **`reserve` under an `@if` reserves nothing, and most abwab sites do exactly that —
  knowingly.** All but one abwab `[reserve]` error surface are guarded on a **non-empty
  message**; inventory them with `grep -rn '\[reserve\]' src/app/`. Most take the direct shape
  `@if (message; as m) { <qd-error-state severity="write" [reserve]="true" [message]="m" /> }`;
  the two list-level page surfaces add a "nothing loaded yet" test to the same truthiness
  (`abwab-page.component.html`, `abwab-templates-page.component.html`); and
  `abwab-door-picker` reaches the same shape through a computed that is empty unless the
  picker is in its error status. At a guarded site the box appears and disappears with the
  message, the input's own contract ("never appears/disappears") cannot hold, and the announcing
  comes from the `role="alert"` being **inserted** into the open `role="dialog"`, not from the
  reserve. **The one unconditional site is the template-copy modal**
  (`abwab-template-copy-modal.component.html`): its region is permanently mounted so the
  live region exists before any failure, and `.qd-state--reserve-empty` (`_components.scss`)
  keeps it visually quiet while empty — `role="alert"` and block size retained, danger tint
  dropped to transparent and transitioned in over `--qd-t-fast` (static under
  `prefers-reduced-motion`) when the message lands. That class exists because Slice C once
  rendered the door/template-node surface unguarded **without** it and shipped a 105px empty
  danger box on every open of both modals — the quiet class is what makes a permanently-mounted
  reserve viable. `reserve` earns its keep where a box is **permanently mounted** and only its
  content arrives late. Do not delete a guarded site's `@if` without also giving it the
  quiet-empty shape, and flip the operation's `announceFailure` in the owning Abwab write/template
  controller if you change which shape a surface has.

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
  `utils/explorer-table-sort.controller.ts`. Tables stay presentational and emit the next token
  (or `null`).
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
- **Header layout:** the balanced overlay header has three tracks: Back/kind/`h2` identity on the
  RTL start side, optional linking actions in the exact center track, and ayah-count/Close on the
  RTL end side. The two side tracks are equal and flexible, so the action group stays centered while
  the title absorbs pressure through ellipsis. Back and Close remain `nowrap` anchors.
- **Header priority is Back/Close > title > count > kind.** The row cannot hold
  every element at phone widths (at 390px the content box is ~326px while
  Back + kind + a 6rem count + Close + gaps need ~378px), so on
  ≤ `$qd-bp-phone-max` the kind marker is `display: none` and the count
  reservation tightens to `4.5rem`. The `h2` still names the entity, and the count
  box stays reserved, so the zero-shift contract below survives. When linking actions exist they
  occupy a centered second row inside the same header on Compact, avoiding collisions without
  returning the controls to the tab/content region.
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
  'rows' | 'panel' | 'text'`, default reproduces today's six-line panel skeleton).
- **The pulse is flat** (D18): `.qd-skeleton` sits on `--qd-surface-sunken` and animates
  `opacity 1 → .62` over `1.4s`. The shimmer sweep it replaced was a `linear-gradient`
  pseudo-element, and no gradient may return to any loading or refresh treatment.
- **`shape="text"` is the single-value text loader** (D40), not a fourth skeleton: a
  `.qd-loading-state` region with a visible label, `role="status"`, `aria-live="polite"` and
  `aria-busy`. A surface with a known final shape (table, list, panel, card grid, Quran page)
  must use a content-shaped skeleton instead; the text loader is only for a count, a badge or a
  single-value region — and it is the shape the retired `qd-state` adapter's `loading` variant
  resolved to before Phase 11 deleted it.
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
  native `<input type="checkbox">` **or `<input type="radio">`**; `.qd-check-row` is
  the flex wrapper that pairs it with its label at a fixed gap. The radio is a
  sanctioned composition, not drift: `abwab-door-picker` keeps one row markup and
  switches the input type on its `single` input
  (`abwab-door-picker.component.html:54-56`,
  `[attr.type]="single() ? 'radio' : 'checkbox'"` plus one `name` shared by that
  picker's rows), so
  single-pick mode is a real radio group with a live keyboard `change` path
  (`abwab-door-picker.component.ts:151-156`). The geometry below is type-agnostic — do
  not "fix" the radio call-site off this class.
- **Geometry / color:** a fixed `--qd-checkbox-size` square (`0.9375rem`, reached
  through the app's own rem scale — the same step as `--qd-btn-font-size`
  (`_tokens.scss:119`, beside `--qd-checkbox-size` at `:135`) and as
  `.qd-input`/`.qd-select`'s font-size (`_forms.scss:10`, `:52`) — rather than a raw
  px), `flex: none`, `margin: 0`, and
  `accent-color: var(--qd-accent)` — no new hue, and correct in both themes since
  `--qd-accent` is defined per theme (`_tokens.scss` light / `_themes.scss` dark
  override) with zero `_themes.scss` change needed here.
- **Row gap:** `.qd-check-row` is `display: flex; align-items: center` with a
  single `--qd-space-2` gap between box and label, so a "checkbox far
  from its label" gap cannot be reintroduced per call-site.
- **Accessible name (contract, not optional):** every checkbox composing
  `.qd-checkbox` MUST carry a real `<label for>` or an `aria-label` naming what it
  selects. **Debt paid (Slice D):** `abwab-tree` and `abwab-cards` compose the class
  and name each box after its door, joining the modal pickers. `abwab-cards` keeps a
  local rule for *placement only* — the card positions its box absolutely — and
  states neither size nor accent, which is the boundary the trap below draws.
- **Consumers:** `abwab-door-picker` (both modal call-sites), `abwab-tree`,
  `abwab-cards`.
- **Composing means deleting the local rule, not adding beside it.** Under Angular
  emulated encapsulation a call-site selector like
  `.some-modal__pick-row input[type='checkbox']` (the shape both abwab pickers carried
  before Slice C folded them into `abwab-door-picker`) outranks the global
  `.qd-checkbox` class on specificity — adding the class without deleting the local
  rule leaves the old size/accent in force with no visible change, which reads as
  "done" and is not (the same specificity trap §17's `.qd-modal` entry names for
  the modal geometry work).
- Compose, do not re-style — a call-site needing a different box size or accent is
  a signal to extend this contract, not fork it.

### `.qd-modal` / `.qd-modal-backdrop` (retired base, one documented consumer)
- **`qd-modal-shell` is the dialog owner.** Every dialog in the app resolves to one of its four
  named widths — `confirm` 30rem / `form` 38rem / `wide` 52rem / `overlay` 46rem — plus the Compact
  full-bleed `94dvh` sheet (D48). `npm run check:golden-ui` fails if a fifth
  `.qd-modal-shell--*` variant is declared.
- **The legacy modifiers are gone.** `.qd-modal--wide`, `.qd-modal--fixed` and the
  `.qd-modal__head` / `__body` / `__foot` slots were deleted in Plan 7 Phase 11 after `rg` proved
  zero consumers; the abwab and detail dialogs that once composed them now compose
  `qd-modal-shell`, which owns padding, the single body scroller, focus and scroll lock.
- **What survives, and why.** `.qd-modal` (`width: min(100%, 36rem)`, surface/border/radius/
  padding, no block-size and no scroller) and `.qd-modal-backdrop` (fixed, centred, `--qd-overlay`)
  remain for exactly one consumer: the four Words Compact **detail drawers**
  (`root` / `lemma` / `stem` / `word-type-details-panel`), which compose
  `.qd-modal.explorer-detail-modal` below Wide. That drawer keeps a fifth geometry
  (`min(100%, 42rem)` wide, `min(88dvh, 42rem)` tall below Wide) and is therefore a **known open
  D48 residue**, recorded rather than silently accepted — Plan 7 §7's failure branch for
  "feature-local outer modal width/padding/scroll selectors" is *retain*, and migrating those four
  panels to `qd-modal-shell` needs a plan amendment. Do not add a second consumer to `.qd-modal`.
- **Standing rule:** no `max-block-size` in a dialog's own SCSS. The shell owns block geometry.
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
- **Consumers:** the six abwab modals (`abwab-door-modal`,
  `abwab-template-node-modal`, `abwab-sections-modal`, `abwab-move-picker`,
  `abwab-relations-modal`, `abwab-template-copy-modal`), all composed by Slice C
  with `__head`/`__body`/`__foot`. Focus containment belongs to `qd-modal-shell`
  (§20.9), not to these consumers.
  **A modal that hosts a nested confirm dialog yields its trap while that confirm is
  open, so two traps are never live at once** — and no consumer arranges that itself:
  `qd-modal-shell` registers open shells in a stack and enables the topmost one's trap
  only, with `[trapFocus]="false"` available when a consumer must suspend its own.
  Two live traps fight over focus, so there is no second nesting level and no confirm above a
  confirm. A modal that wants a control other than the first tabbable one marks that
  control `cdkFocusInitial` rather than moving focus itself after the trap captures —
  one focus move. Focus return belongs to the shell (§20.9), which captures the
  pre-open `activeElement`; `cdkTrapFocusAutoCapture` is deliberately absent. The
  shallow ones (door, template-node) render with empty space below the fields:
  that is this section's "zero resize" trade, not a defect to fix back to
  content height.

### Header over badge columns (feature-local pattern)

- **Where it applies:** a row list whose trailing numeric badges want naming, and only
  there. The doors tree (`features/abwab/components/abwab-tree/`) is the one
  consumer; this is a documented pattern, not a `qd-` class.
- **The header must sit OUTSIDE the `role="tree"` / `role="list"` element** and be
  `aria-hidden="true"`. A presentational row inside the ARIA container reads as an
  unlabelled item to a screen reader. Meaning stays on each badge's own
  `aria-label` — the visible header is a hint, never the semantic carrier. This is
  the `qd-tabs` count-meta precedent: visible digits `aria-hidden`, meaning in the
  label.
- **Alignment is structural, via a three-level subgrid**, never eyeballed:

  ```
  .abwab-tree-frame            display: grid   — owns the ONE column template
  ├── .abwab-tree__header      subgrid, grid-column: 1 / -1, aria-hidden
  └── .abwab-tree  [role=tree] subgrid, grid-column: 1 / -1
      └── .abwab-tree__row     subgrid, grid-column: 1 / -1
  ```

  The frame exists because the header must be outside the ARIA container, so the
  grid owner has to be an ancestor of both. `display: contents` on the ARIA element
  is **not** the shortcut it looks like: it has a history of dropping elements from
  the accessibility tree, and that element carries the role and its `aria-label`.
- **Four rules the layout depends on**, each of which fails silently if broken:
  - The flexible name track is `minmax(0, 1fr)`, never a bare `1fr` — `1fr` means
    `minmax(auto, 1fr)`, whose auto minimum refuses to shrink past min-content, so
    `.qd-truncate` never engages and the name pushes the badge tracks out.
  - **No inline padding on any subgrid element.** It is subtracted from the space
    the tracks occupy. Row insets live on the first and last cells instead
    (`> *:last-child` covers the trailing one across conditional layouts).
  - Every row renders every badge cell, empty when it has no value. Auto-placement
    is positional: one row that skips a cell puts its trailing furniture in the
    wrong track and breaks alignment for the whole list.
  - When a column drops responsively, the **cells and the template's tracks drop in
    the same media query**, or the two drift apart.
- **The column width is a name-budget decision, not a typographic one.** Every pixel
  of a fixed badge track is taken from the row's only shrinkable item. Size the
  column from the widest *badge* a real value produces, then check the labels fit —
  not the other way round. If full words do not fit, **abbreviate the visible label
  and leave the `aria-label` alone**; the accessible layer is where the meaning has
  to survive. Slice J's full words needed 2.5 rem and cost the name 55 px; the
  abbreviated set needs 1.75 rem and costs 19 px, with identical screen-reader
  output. Re-measure the truncation entry's budget rule whenever this changes.

### `qd-context-menu`
- **Purpose:** the one row/node context-menu shell app-wide (Abwab's doors tree row menu
  and the templates workshop's node tree row menu — the two pre-existing copies this
  primitive replaces).
- **Non-interactive preview variant:** `variant="tooltip"` keeps the same anchored floating-layer
  placement but renders `role="tooltip"`, never takes focus, uses a non-blocking backdrop, and lets
  its control anchor keep pointer ownership. It is reserved for short hover disclosures with no
  actions; pointer exit is owned by the feature trigger and dismisses the surface.
- **Inputs / outputs:** `position: {x, y}` (positions the menu via
  `[style.left.px]`/`[style.top.px]`, unchanged from both prior copies); `menuTestId` /
  `backdropTestId` (both `string`, required) — **non-negotiable**, because surviving consumers use
  `abwab-page-context-menu` / `abwab-page-ctx-backdrop` / the templates-page equivalents, and the
  inputs keep them byte-identical; `menuAriaLabel` (optional, defaults to
  `CONTEXT_MENU_LABELS.menuAriaLabel`) names the `role="menu"` box — a menu with no
  accessible name is announced as an unlabelled group, so a consumer whose menu is not
  "operations" passes its own Arabic string; `dismissed` output, emitted on
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
  copying `top-navbar.component.ts`), not bound to the menu element. It has to stay
  document-level even now that the menu takes focus on open (below): the open-focus is a
  best effort over *projected* items, so a menu whose consumer projects nothing focusable
  leaves focus outside it, and an element-bound handler would never receive the key there.
  **This is the one place this primitive is not literally behavior-preserving:** neither
  prior copy dismissed on `Escape`. Deliberate, additive a11y gain, not a bug.
- **Keyboard contract — the menu owns its own focus.** On open it moves focus to the first
  projected `[role="menuitem"]` (`context-menu.component.ts:52-56`; queued so the items
  have rendered, and skipped if the menu was destroyed in the meantime).
  `ArrowDown`/`ArrowUp` move between items with wrap-around, `preventDefault`d so the page
  does not scroll (`:79-92`, bound on the `role="menu"` box). On destroy focus returns to
  whatever was focused when the menu opened (`:98-105`, `:138-141`), but **only if focus is
  still inside the menu or has been lost to `<body>`** — a consumer that deliberately moves
  focus elsewhere while acting on an item keeps it. Traversal reads the live DOM
  (`querySelectorAll('[role="menuitem"]')`) rather than a registry, because the items are
  projected: **an item the consumer does not mark `role="menuitem"` is invisible to both
  the open-focus and the arrow keys.**
- **Placement contract (slice L).** State it as the mechanics, never as a bare direction
  word: a box that *begins* at the inline-start and grows away from it reads as
  "extending" toward whichever end you had in mind, and a doc that names the wrong one
  gets implemented against.
  **The menu's inline-START edge is pinned at the anchor point and the box grows in the
  reading direction:** under RTL its right edge sits at `x` and the box grows leftward;
  under LTR the mirror (left edge at `x`), which is the behaviour that originally shipped.
  Direction is resolved from `closest('[dir]')`, never hardcoded
  (`floating-layer-placement.ts:127`). The menu owns no placement math of its own: the
  template delegates to `qdFloatingLayer` (`context-menu.component.html:3`), the directive
  places at `ngAfterViewInit` (`floating-layer.directive.ts:139`), and the geometry lives in
  `computeFloatingPlacement()` (`floating-layer-placement.ts:59-88`). It **flips in the block
  axis** when opening below would cross the bottom and more space exists above; both axes are
  then clamped to an `8px` viewport margin (`FLOATING_VIEWPORT_MARGIN`), and block size is
  capped at 60% of viewport height. The clamp is a floor, not a guarantee: a menu wider or
  taller than that axis of the viewport is pinned at the `8px` margin and overflows the far
  side — and since the pin is always `left`/`top`, under RTL that means the edge nearest the
  anchor is the one lost.
  Both trees' keyboard paths anchor at the focused row's inline-start edge to match
  (`abwab-tree.component.ts:317-319`, `abwab-template-tree.component.ts:106-108`).
  Recorded browser walk (1024px and 1440px, both themes, 12 points): mid-viewport right-edge
  delta 0.0px; viewport-edge and bottom-edge opens never clipped; the bottom open flipped
  upward in every case. `menuTestId`/`backdropTestId` and the projected-item contract are
  unchanged.
- **Two of the three gaps this primitive originally left open are now closed** — kept
  named so a future reader does not re-open them as work: gap 1, no viewport clamping,
  **closed by slice L** (the placement contract above — whose math now lives in the pure
  `computeFloatingPlacement()`/`resolveFloatingDirection()` helpers of
  `shared/ui/floating-layer/floating-layer-placement.ts`, with the RTL/LTR default, block flip,
  and clamp branches); gap 2, no focus management into
  the menu, **closed** (the keyboard contract above). What is still open:
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
  canonical shape, and `abwab-tree.component.scss`'s own `.abwab-tree__name` rule already
  does the same. **This section states that as the rule, in both directions:** a
  name column composes `.qd-truncate` (`_utilities.scss`) on a flex item that owns
  `flex: 1` (or a reserved floor via `flex: 1; min-inline-size:
  var(--qd-name-min-inline-size)`, `_tokens.scss`, for a column that must not
  shrink to nothing under sibling pressure — see its derivation comment there). A
  **hard, fixed `inline-size` name column is a per-surface exception, not a second
  house rule** — it trades away exactly the flexibility every other truncated name
  in the app relies on, so a surface reaching for one must write down, at that
  call-site, why its layout cannot tolerate a shrinking name column the way every
  other one does. This entry exists because a request once arrived for a fixed
  name width where every existing precedent was flexible; this paragraph is where
  that gets settled once, so a later reviewer does not re-litigate it per
  call-site.
- **Mandatory `[title]`, not optional — and never sufficient on its own:** any
  element composing `.qd-truncate` (or otherwise capable of visually truncating)
  MUST carry `[title]="fullName"` so the full name is available on hover/long-press
  once the ellipsis hides it — precedent `word-type-filter.component.html:57`:
  `<span class="word-type-filter__child-label" [title]="child.label.ar">{{
  child.label.ar }}</span>`. A truncated name with no `[title]` is a contract
  violation, not a style nit. But `title` is a *pointer* affordance, so it never
  discharges D35 by itself: the truncating node must ALSO sit on a rung of the
  Golden §8.1 disclosure ladder — a focusable owner carrying the full value, or a
  related surface already showing it in full. `word-type-filter`'s span qualifies
  because it labels a focusable filter control; a bare `<span>` in a static row or
  chip does not. **When no rung fits, the surface does not truncate at all** — it
  wraps (`min-inline-size: 0; overflow-wrap: anywhere`) and drops both `.qd-truncate`
  and `[title]`. Adding `tabindex="0"` to the text node to manufacture a rung is
  explicitly prohibited.
- **Nine sites left the rule under D35, and the whole set was swept, not listed.**
  The nine that had `title` as their *only* disclosure, each with no focusable owner
  and no related full-value surface, now wrap instead of truncating: the relations
  modal's bulk target chips; the sections modal's row names and the template tree's
  node names (both of which a read-only visitor sees with the permission-gated
  buttons — and, for the tree row, its `tabindex` — absent); the Access audit row's
  target/actor lines and the owner-reconciliation candidate emails (both inside a
  `qdResultItem` that is deliberately not focusable); the Access Compact context-bar
  identity; the Access user picker's chosen identity (a `<p>`, whose sibling button
  clears the choice rather than revealing it); the Abwab side panel's active door
  name (a plain `div` chain under `role="group"`); and the templates page's editor
  heading (a plain `<h2>`). Each remains without `title`, `.qd-truncate`, or an added `tabindex`.
  **The remaining `[title]` sites were resolved individually against their real
  focusable owner and are correct**: a name truncating inside a `button`
  (`abwab-cards` card and crumbs, `access-user-list` row, `abwab-move-picker`'s two
  row kinds, `abwab-templates-page`'s list item, `word-type-filter`'s child chip), a
  `role="treeitem"` with a roving tabindex (`abwab-tree`, `abwab-archive-view`), a row whose focusable check control carries the name
  (`abwab-door-picker`), an `aria-hidden` count inside a focusable tab
  (`abwab-toolbar`), a `<label>` wrapping a checkbox with no truncation
  (`access-permission-editor`), and one that is not the HTML attribute at all
  (`words-hub-page` binds a component input named `title`). **Close this class by
  sweeping `rg '\[title\]|\btitle='` over `src/`, never by working a reported list.**
- **Debt paid (Slice D).** Slice C composed the rule inside the abwab modals — the
  door picker's row names and the relations modal's bulk target chips (the latter
  since reverted to wrapping, above). Slice D
  finished the page surfaces: the doors tree, the archive view, the template tree
  (since reverted), the side panel's active door (since reverted), the cards' title
  and breadcrumb trail, the sections modal (since reverted), the templates list and
  its editor title (the title since reverted), and the move picker's two row
  kinds. Every remaining one composes `.qd-truncate` with its mandatory `[title]` on a
  focusable row owner, and each
  local ellipsis rule it supersedes was **deleted**, not left beside it. Three of
  those sites needed a shape change rather than a class: a name sharing one text
  node with a sibling chip (the card title, the templates editor title) had to
  become its own span before it could truncate independently, and the move picker's
  row buttons needed an inner block-level span, since `text-overflow` needs a block
  box. **No site took the `--qd-name-min-inline-size` floor**, and a 12 rem floor on
  the tree row would overflow the row instead of truncating inside it. The token
  stays available for a surface that measures differently.
- **The doors-tree name budget is a rule, not a figure** (measured 2026-08-02, slice
  J, in-browser at three viewports in both themes — no theme token participates in
  geometry, so the two are identical). On a **branch row carrying all three badges**:
  **325 px at 1024 px, 485 px at 1184 px, 741 px at 1440 px — minus 24 px per depth
  level** (the indent step is `--qd-space-5`, and the indent is the only depth-varying
  term). A leaf row gains back what its absent badges cost. This entry previously
  carried "~184 px at the narrowest viewport the doors page reaches", which is
  reproducible only on a branch row at depth ≈ 6–7 at 1024 px — a deep-row number
  stated as a general one. **Any change to the row's leading or trailing furniture
  re-measures this rule rather than inheriting it**; slice J's badge-column header
  cost 19 px against a 20 px ceiling set before the work began, and the visible header
  labels were abbreviated (their `aria-label`s were not) precisely to stay under it.
- Compose, do not re-style — a surface that seems to need a fixed name column
  should re-read the paragraph above before reaching for `inline-size` instead of
  `.qd-truncate`.

### Reveal highlight (the app's "here it is" mark)

- **Purpose:** the one way to answer "where did that go?" — a control elsewhere
  navigates to a row/item and the destination has to identify itself on arrival.
  First consumer: abwab's reveal-in-tree, where a relation chip in the modal reveals
  the related door in the doors tree.
- **It cannot be a background tint, and this is the trap worth stating first.**
  `--qd-selected-bg` **is** `--qd-accent-tint` in both themes (`_tokens.scss`
  light, `_themes.scss` dark — measured: light `oklch(0.954 0.010 164.9)`, dark
  `oklch(0.250 0.030 281.2)`), and a reveal lands on the row it has just selected.
  A tint highlight is therefore an exact **zero-delta** against the destination's own
  selected fill: the code looks right, ships, and marks nothing. Read `_tokens.scss:94`'s
  recorded lesson the other way round — there a mark too *close* to hover read as a
  flash; here a mark *equal* to selected does not read at all.
- **The mark is an outline**, because a selected row carries `outline-style: none` at
  rest, so a ring is a genuinely new signal rather than a competing fill: `2px solid
  var(--qd-accent)` at `outline-offset: -2px`, decaying to `transparent` over ~3s
  through a keyframe animation, with the consuming component clearing the class on the
  **same** duration so nothing lingers invisibly. No new hue — `--qd-accent` is on
  §16.3's allowed list and is defined per theme.
- **Reduced motion (§15-F/§17's blanket rule):** `animation: none` plus the mark held
  statically at full strength for the same span. The reveal must still *say where*; it
  just must not animate.
- **Focus keeps precedence.** `:focus-visible`'s own outline is declared after the
  reveal rule, so a keyboard user never loses the focus ring to a decaying mark.
- **The class is a signal the host owns, not a self-clearing effect.** The consumer
  holds the marked id in a signal and clears it on a timer it also clears on destroy;
  the CSS only renders it. Keeping the timer with the host is what lets the same host
  key the mark off the navigation that makes the destination exist
  (`features/abwab/state/abwab-reveal.controller.ts`) instead of off the click.
- **The persistent variant: a search match mark (ux-slice-l).** The same "this row is the
  one" problem with a different lifetime — it holds for as long as the query does instead of
  decaying. Same reasoning rules out a tint (see above), so it is also outline-family, but it
  takes a **different CSS property**: `box-shadow: inset 0 0 0 1px var(--qd-accent)`, while
  the reveal keeps `outline`. That split is the point. The reveal ring and `:focus-visible`
  already both claim `outline`, and a third claimant would make which mark shows depend on
  SCSS declaration order — a silent-reformat hazard. Separate properties compose
  order-independently: **match + revealed** shows the 2px decaying outline around the static
  1px inset ring, and **match + focused** shows the focus ring with the match still visible.
  The 1px/2px pairing keeps the persistent mark quieter than the transient one. It never
  animates, so reduced motion needs nothing new; the row's `border-radius` is followed by an
  inset shadow, so it hugs the same shape hover does. **Do not unify the two onto one
  property** — that reintroduces the race this split removes.
  Measured in-browser (1024px and 1440px, both themes, rest / hover / selected / focused —
  16 readings): the mark is `oklch(0.49 0.068 176.3)` in light against fills of transparent,
  `oklch(0.945 0.015 94.2)` hover and `oklch(0.954 0.01 164.9)` selected — a lightness delta
  of 0.46–0.46 on the two fills; in dark it is `oklch(0.772 0.098 82)` against
  `oklch(0.265 0.039 262.7)` hover and `oklch(0.25 0.03 281.2)` selected, delta 0.51–0.52.
  The shadow was byte-identical in all four states in both themes — no fill and no focus ring
  overwrites it.
- Compose, do not re-style — a second consumer takes this class shape, not a new one.

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
  `.qd-page-shell` (`_layout.scss`) carries `border-box`, which is why the page-shell contract
  (§18.4) is a prerequisite of this pattern, not a coincidence.
- **Abwab-local for now.** The reservation lives on `abwab-page.component.scss`
  (`.abwab-page__frame`), not on the shared `.qd-page-shell` rule — promoting it
  there would silently reserve a viewport on all five explorer pages, which nobody
  has measured. **Generalize it only when** a second feature's page needs the same
  state-stability guarantee; at that point promote the rule onto `.qd-page-shell`
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

### Sticky app chrome
- **Purpose:** `.qd-navbar` (`_layout.scss`) stays visible while the page scrolls —
  Slice B2, item 6, T901. `position: sticky; inset-block-start: 0; z-index:
  var(--qd-z-mobile-nav)`, **not** `--qd-z-sticky` — see the stacking-context entry below
  for why the rung had to be `--qd-z-mobile-nav`, the one its own dropdown and mobile menu
  already declare.
- **A sticky element's containing block must be TALLER than the element itself, or
  it never sticks at all — not a Chrome quirk, spec behavior in every engine.** A
  sticky box's travel is clamped to its containing block's content box; when the
  containing block is exactly the element's own height, available travel is zero,
  so the box can never leave its static position and just scrolls away with the
  page. This is the single most common real-world cause of "sticky doesn't stick,"
  and it bit this exact rung: `.qd-navbar`'s Angular component host
  (`<qd-top-navbar>`) is a flex item of `.qd-shell-viewport` (flex items are
  blockified), and with no height of its own it wraps the 56px navbar in a 56px
  box — zero travel. The fix is `:host { display: contents; }` on
  `top-navbar.component.scss`, which drops the host out of the box tree so
  `.qd-navbar` becomes the direct flex item of the (903px+) `.qd-shell-viewport`
  instead. **Any future sticky element whose component host wraps it tightly hits
  the same wall** — check the sticky element's actual containing block in the
  browser before shipping, not just its computed `position`/`top`.
- **Both viewport-relative sticky offsets that predate this rung had to be
  re-based onto it, or two shipped surfaces regress (T902):**
  `--qd-mushaf-sticky-top` (`_tokens.scss`) and `.abwab-page__side`'s `top`
  (`abwab-page.component.scss`) both become `calc(var(--qd-navbar-block-size) +
  <existing offset>)`. **`--qd-mushaf-panel-height` had to be re-derived from the
  re-based offset, not just the bare navbar token** — sizing it as `100dvh -
  var(--qd-navbar-block-size)` leaves the panel's stuck bottom edge exactly
  `--qd-mushaf-sticky-top`'s extra gap (`--qd-space-3`) past the viewport once
  stuck, since the height formula never accounted for that gap. Re-derived as
  `100dvh - var(--qd-mushaf-sticky-top)` instead (`_tokens.scss`), which makes the
  panel's stuck bottom edge land exactly flush with the viewport by construction:
  CSS custom properties resolve at used-value time, so referencing a token declared
  later in the same block is fine. **The lesson generalizes: a sticky element's own
  height/`min-block-size`/`max-block-size` must be measured from its OWN stuck
  `top`, never from a shorter token that ignores part of that offset** — the two
  will only coincide by accident.
- **A sticky element's own rung must be the SAME rung its descendant menus already declare, or
  it clamps them — this is a real mechanism, resolved by rung choice, not a limitation.**
  `position: sticky` unconditionally establishes a new stacking context in every current engine,
  regardless of `z-index` value (verified: forcing `.qd-navbar`'s `z-index` to `auto` does not
  restore the escape — sticky itself is the trigger, confirmed with an isolated repro on this
  app's own page). The navbar's own Wide dropdown, `.qd-nav__menu` (`_components.scss`), declares
  `--qd-z-mobile-nav` (45) for itself.
  Putting `.qd-navbar` on `--qd-z-sticky` (5) instead — the first, wrong instinct, since it reads
  as "the lowest rung, so everything else wins" — clamps that descendant down to 5 against
  anything painting *outside* the navbar, regardless of its own declared z-index. **That isn't
  only a dropdown problem: confirmed against every z-scale consumer (`grep -rn "var(--qd-z-"`),
  it silently breaks three real surfaces once the navbar is sticky:**
  1. `.qd-nav__menu` (`--qd-z-mobile-nav`) loses to a `--qd-z-floating` (40) sibling outside the
     navbar — confirmed live with a synthetic probe positioned over an open dropdown.
  2. The Compact/Medium navigation sheet — since Phase 10 a `qd-modal-shell` on
     `--qd-z-modal-backdrop` (50) / `--qd-z-modal` (51), previously a fixed full-screen overlay on
     `--qd-z-mobile-nav` — would paint *below* every page popover (`--qd-z-popover` = 30) and
     below `.detail-modal-shell__restore` (`--qd-z-floating` = 40) — a visible regression, not
     latent.
  3. Page popovers (`source-selector`, `surah-jump-picker`, `explorer-association-filter`, all
     `--qd-z-popover` = 30) would paint *over* the sticky navbar's own box on a scrolled page — a
     failure mode that did not exist before the navbar was sticky, since content never used to
     scroll under it.
  **Resolution: `.qd-navbar` sits on `--qd-z-mobile-nav` (45), the same rung its dropdown
  declares** — not a new token, and not a respacing of the scale. This
  satisfies the scale's stated purpose exactly (§4: "deliberately below `--qd-z-menu-backdrop`,
  so row menus and modals paint above the chrome") while fixing the mechanism: 45 beats popover
  (30) and floating (40), so a `qd-context-menu`/modal backdrop still paints above the chrome at
  49/50/51. Re-verified live after the fix: an open dropdown now beats a `--qd-z-floating` probe
  at the same screen position; the navigation sheet covers page content; a `qd-context-menu` and a
  modal backdrop still paint above the sticky navbar; a page popover no longer overpaints the
  navbar on a scrolled page. **`--qd-z-sticky` (5) stays reserved for a genuinely in-page sticky element
  with no competing descendants of its own** (`mushaf-header-navigation.component.scss` is the one
  consumer) — the failure mode above is specific to a sticky element that *also* hosts its own
  higher-rung menus, which is why respacing the whole scale was rejected: nothing else on the
  scale has that shape.

### Chrome-inert rule
- **Purpose:** while any modal dialog holds `ScrollLockService`'s lock, `.qd-navbar`
  itself goes `[inert]` + `[aria-hidden="true"]` — Slice B2, item 6's keyboard half,
  T904, completing Slice A's T203. Copies `app.ts:14`'s existing shell-inert pairing
  at the one level that does not also inert the dialog: `inert` goes on **the
  navbar, not the shell**, because abwab's modals render *inside* `<main>`, inside
  the shell — shell-level inert would inert the dialog itself. `.qd-navbar` is a
  sibling of `<main>`, so inerting it leaves every dialog interactive.
- **`ScrollLockService.lockCount` is the one piece of state every modal dialog in
  the app already holds** (`shared/ui/modal-scroll-lock/`) — it gained a
  signal-backed `isLocked` computed for this (`scroll-lock.service.ts`), rather than
  a second "any modal open" service, which would duplicate `lockCount`'s job and
  give two sources of truth for the same fact.
- **Blast radius: the rule is the membership test, not a list.** The membership test is
  **holding `ScrollLockService`'s lock** — not applying the directive, which is only the
  usual way to hold it. So the radius is `grep -rn qdModalScrollLock src/app/` **plus
  `detail-modal-shell.component.ts:63`**, where the global entity-detail overlay shell
  acquires the lock **imperatively** in an effect (releasing at `:66` and `:104`) while
  its template applies no directive: it inerts the chrome exactly like the rest and the
  grep alone never returns it. No count belongs here either, because every new dialog
  moves it. The radius reaches well beyond abwab: the abwab modals, the words detail
  panels and drilldown, that overlay shell, and — since it composes the directive itself —
  **every `qd-confirm-dialog`, and therefore every confirm in the app**. That last one is
  the trap: adding a confirm anywhere silently enlarges this radius. The navbar is
  keyboard-unreachable while any lock holder is open. This is an intentional
  behavior change on shipped words surfaces nobody asked about, accepted
  deliberately: every holder is a modal dialog or a modal overlay, "app chrome is not
  reachable while a modal dialog is open" is not an abwab-only doctrine, and the precedent
  is *stronger* — `app.ts:14` already inerts the entire shell for the global overlay.
- **Inert-inside-inert is real and was observed live.** With a
  words drawer (e.g. `root-details-panel`, holding the lock) open *under* the global
  detail overlay (`app.ts`'s `overlayOpen()`, which inerts the whole shell): the
  shell carries `inert`/`aria-hidden` from `app.ts`, and `.qd-navbar` — itself a
  shell descendant, already inert by cascade — *also* carries its own explicit
  `inert`/`aria-hidden` from `ScrollLockService.isLocked()`. Both apply
  simultaneously and harmlessly; browsers treat nested/duplicate `inert` as
  idempotent. Exactly one focus trap remains enabled (the dialog's, not the drawer's), confirmed by
  a live count of enabled `.cdk-focus-trap-anchor` elements in the
  browser.

### `qd-result-count`
- **Purpose:** a one-line "label: N" stat that holds its line across loading/error/
  loaded instead of unmounting and resizing whatever it sits above (Feature 026,
  US4). Three states render the same line box: loaded shows the label plus the
  number; loading shows an `aria-hidden` skeleton bar with sr-only loading text
  (`role="status"`); error shows an `aria-hidden` muted placeholder (`—`) — the
  page's own error surface stays the only place that announces or explains a
  failure. Never a card, never a KPI row — `PRODUCT.md`'s anti-reference list names
  "identical gradient stat cards" explicitly.
- **Promoted to `shared/ui/result-count/` in Slice B2 (T1001)**, class
  `ExplorerResultCountComponent`, selector `qd-result-count, qd-explorer-result-count`
  — the same dual-selector alias mechanism as `qd-panel-skeleton,
  qd-explorer-panel-skeleton` (`ui/explorer-panel-skeleton/`), kept so the four
  existing words explorer call-sites (Unique Words, Roots, Lemmas, Stems) needed no template
  change, only an import-path update. New call-sites (item
  17's abwab stats bar) use the neutral `qd-result-count` selector.
- **Its own labels are read through a TDZ-safe getter**
  (`result-count.labels.ts` → `protected get labels()`), never a `readonly` field —
  a field initialiser can observe the label module inside its temporal dead zone. The promotion
  preserved the existing `*.labels.ts` idiom rather than dropping it on the move.
- **Renders `labelPrefix()`: `count()` — a data-display idiom, not a counted-noun
  sentence.** Every consumer (the four words explorers' "عدد الجذور: 1642"-shaped
  copy, and item 17's abwab «كل الأبواب: N» / «أبواب هذا التبويب: N») passes a
  static `labelPrefix` and the raw digit; none run the count through
  `abwab.labels.ts`'s `countPhrase` agreement forms, because "label: N" is a stat
  display, not a sentence embedding a counted noun — the "never a bare interpolated
  count" rule targets sentence-shaped copy like `archiveConfirm`, not this shape.
- **Item 17's second consumer, abwab's stats bar, is two instances above the
  toolbar** (`abwab-page.component.html`), both live-only and both derived from the
  existing tree snapshot with no new backend read. Their selectors and nullable-`sectionId`
  handling live in the owning page code, and the stats stay mounted through every tab switch.

### `qd-nav-progress` (router navigation progress bar)
- **Purpose:** the app-shell-level "the click was heard" affordance for lazy-route
  navigations. Every route is lazy and every skeleton lives inside the route's own
  chunk, so on a cold first visit nothing can paint while the chunk downloads — the
  bar is the one element that renders during that gap because it lives in the shell
  (`core/layout/nav-progress/`, rendered by `app-shell.component.html` above the
  navbar), outside every `<router-outlet>`.
- **Form:** a 2px `--qd-accent` hairline fixed to the viewport's top edge, growing
  from the inline start — from the right in RTL — via an `inline-size` keyframe to
  86%, where it holds: it never fakes completion; the done state owns the snap to
  100% and the `--qd-t-base` fade. `pointer-events: none`, `aria-hidden`, z-rung
  `--qd-z-nav-progress` (top of the layer scale — a non-interactive hairline may
  paint above modals, because a navigation can be triggered from inside one).
- **Show-delay (200ms):** `NavigationStart` arms a timer; nothing renders until it
  fires. Warm (chunk-cached) navigations settle in well under 100ms, so they show
  nothing at all — no flash on instant navigations.
- **The settle rule is an inversion — never a terminal-event whitelist.** The
  component whitelists the known *in-flight* lifecycle events (`RouteConfigLoad*`,
  `RoutesRecognized`, `GuardsCheck*`, `Activation*`/`ChildActivation*`,
  `Resolve*`); any other router event — End/Cancel/Error/**Skipped** today, or any
  event class a future Angular adds — settles the bar. Unknown events fail closed
  (the bar clears early) instead of sticking forever. Do not "fix" this into a
  whitelist of terminal events.
- **A11y:** a single permanent sr-only `role="status"` polite region announces
  «جارٍ تحميل الصفحة…» only once the bar actually becomes visible — warm
  navigations announce nothing, so the shell never queues chatter against
  page-owned polite regions (e.g. abwab's announcer). No focus is moved. Under
  `prefers-reduced-motion` the bar is a static full-inline-size hairline: no growth
  animation, no fade.
- **Handoff contract:** the bar covers "code is downloading"; the routed
  component's own skeleton covers "data is loading". The bar's fade may overlap the
  skeleton's appearance by at most the fade duration; neither replaces the other,
  and in-component skeletons must never be removed in its favor.

## 18. Golden UI foundation (Plan 7, Phase 1)

The permanent visual authority is `.architecture/golden-ui/` (four Markdown contracts plus four
HTML acceptance boards). The short mandatory rule set every UI change must read first is
`../FRONTEND_UI_RULES.md`. This section records the *mechanics* that foundation put in place; the
sections above stay valid for everything it did not touch.

### 18.1 One token truth, reached through the existing theme mechanism

`src/styles/_tokens.scss` now carries the Golden light values on the **existing themed token
names**, and the Golden semantic names are aliases pointing at them:

```
--qd-bg:      #F4F2EC   ←  --qd-bg-page
--qd-chrome-bg: #FAF9F5 ←  --qd-bg-chrome
--qd-surface: #FFFFFF   (already the Golden name)
--qd-section-bg: #FBFAF6 ← --qd-surface-quiet   (and --qd-surface-hover)
--qd-surface-recessed: #EEEBE1 ← --qd-surface-sunken
--qd-text/-body/-muted: #23211C / #443F37 / #6E6759 ← --qd-ink / --qd-ink-body / --qd-ink-muted
--qd-primary = --qd-accent: #1C6349 ← --qd-green-solid / --qd-green-thread
--qd-accent-text: #1B5E46 ← --qd-green-text ; --qd-accent-tint: #E7F0EA ← --qd-green-tint
--qd-accent-soft: #CFE0D6 ← --qd-green-quiet (and --qd-border-accent)
--qd-danger/-tint #8C2F22/#F7E9E5 · --qd-warning/-tint #8A5A12/#F7EEDC · --qd-success/-tint #1B5E46/#E7F0EA
--qd-footer-bg #16233A · --qd-footer-text #D5DCE6 ← --qd-ink-on-dark
```

The direction matters and is not interchangeable: **the themed name holds the value, the Golden
name is the alias.** `_themes.scss` overrides the themed name, so a component written against a
Golden alias still follows the existing dark toggle instead of becoming a light island. Writing
this the other way round (Golden name holds the value, themed name aliases it) would break dark for
every migrated component. Nothing Golden-dark was added; dark remains interim and unreviewed.

Tokens with no themed equivalent are declared directly and are light-only for now:
`--qd-neutral`, `--qd-neutral-tint`, `--qd-neutral-ink-disabled`, `--qd-danger-hairline`,
`--qd-warning-hairline`, and the `--qd-lifecycle-*` / `--qd-mutation-*` / `--qd-membership-owner`
role aliases (§2.4 of the Golden system is exhaustive — no status colour may be invented at a call
site). Lifecycle and mutation names never merge: "active account" and "successful mutation" share a
green today and must keep separate names.

The morphology taxonomy palette (`--qd-segment-cat-*`) is deliberately **unchanged** — it is content
taxonomy, not status, and no Golden alias recolours it.

### 18.2 Spacing, radius, type, geometry

- `--qd-s-2 … --qd-s-64` is the Golden 4px/8px-rhythm scale (`2,4,8,12,16,20,24,32,40,48,64`).
  The historical `--qd-space-1…6` are now aliases onto it, so there is one spacing truth and the
  three steps the old scale lacked (2, 20, 40/48/64) exist without a second vocabulary.
- Radii are `4/6/10/14/999px` (`--qd-radius-xs` is new; `--qd-radius-sm` moved 7px → 6px).
- `--qd-type-*` carries `12/1.5 · 13/1.6 · 14/1.75 · 16/1.8 · 18/1.45 · 20/1.4 · 24/1.35 · 30/1.3`
  plus identity. `_typography.scss`'s `.qd-page-title`/`.qd-section-title`/`.qd-card-title`/
  `.qd-text*` are built from those tokens rather than local rem literals, and `.qd-text-body`,
  `.qd-text-caption`, `.qd-text-identity`, `.qd-prose` were added for the roles that had no class.
- Control geometry, hit target, modal widths, rails, splits, page measures, grid bounds, and the
  floating-layer block size are all tokens (`--qd-control-*`, `--qd-hit-target-min`,
  `--qd-modal-*`, `--qd-rail-*`, `--qd-split-*`, `--qd-page-measure-*`, `--qd-grid-*`,
  `--qd-floating-max-block-size`). Phase 1 declares them; later phases consume them.
- Elevation: resting is zero. `--qd-shadow-layer` (`0 8px 24px -10px rgb(35 33 28 / .22)`) is the
  one floating-layer shadow and `--qd-floating-shadow` / `--qd-shadow-lg` resolve to it.
  `.qd-card` no longer declares a resting `box-shadow` at all.

### 18.3 Bands

`src/app/shared/layout/breakpoints.contract.json` is the single neutral source. `breakpoints.ts`
imports it, `tailwind.config.js` requires it, and `src/styles/_breakpoints.scss` is a Sass adapter
whose every literal is compared against the JSON by `npm run check:golden-ui`.

Compact `≤767` · Medium `768–1079` · Wide `≥1080` · Wide-plus `≥1440` (measure only, never a fourth
structure). The historical `$qd-bp-tablet-max` / `$qd-bp-desktop-min` and
`QD_BP_TABLET_MAX_QUERY` / `QD_BP_DESKTOP_MIN_QUERY` are kept as aliases but now resolve to
`1079/1080`, not `1023/1024` — that move is the point of D10, and it is why the legacy desktop
behaviour no longer engages at the 1024 edge. The aliases retire in Phase 11 once no consumer is
left.

### 18.4 One gutter, four page intents, three rails

`.qd-page` is **block rhythm only** (`padding-block: var(--qd-page-rhythm)`). The inline gutter
belongs to the page shell alone:

- `.qd-page-shell` + `--capped-reading` (72rem) / `--full-data` (100rem) / `--split-workspace`
  (100rem) / `--protected-mushaf` (90rem, feature-owned) — the canonical API.
- The `.qd-container` / `.qd-page-frame` / `.qd-explorer-frame` legacy aliases and the
  `.qd-page > .qd-page-header` compatibility gutter are **deleted** (Phase 11, zero consumers).
- A page shell nested inside another drops its `padding-inline` to `0`, so a nested surface cannot
  manufacture a second route gutter. `npm run check:golden-ui` fails if any stylesheet under
  `src/` other than `_layout.scss` declares `padding-inline: var(--qd-page-gutter)`, or if
  `_layout.scss` declares it more than once.

`--qd-page-gutter` is `16 / 24 / 32 / 40px` at Compact / Medium / Wide / Wide-plus, declared once in
`_tokens.scss`. `.qd-page-rail--s/--m/--l` are `16/18/20rem` and collapse to full width below Wide;
`.qd-page-split--data` (`1.25fr 1fr`) and `.qd-page-split--mushaf` (`40% 60%`) become two columns
only at Wide, so Medium can never be a squeezed Wide.

### 18.5 Bounded grids (F04)

`.qd-grid` plus `--destinations` (18–26rem, max 3), `--curriculum` (20–30rem, max 2), `--doors`
(14–20rem, max 4), `--permission-groups` (15–22rem, max 3). Each modifier sets only the three
custom properties; the base rule derives both `grid-template-columns` and the max-column cap from
them, and Compact forces a single column. `.qd-grid__span-all` carries the "final card spans" and
orphan rules. Feature phases apply these classes; they never restate the numbers.

### 18.6 Hover, selection, direction, hit area

- One neutral hover surface: `--qd-surface-quiet` (`--qd-surface-hover` now aliases it), used by
  cards, menus, tabs, chips and navigation alike. Green is never a hover tone; the former
  `.qd-card--mini` accent-border hover is gone (D15).
- `.qd-selected-thread` is the logical 2px `border-inline-start` green selection mark. It is
  declared here as the owner; call sites migrate to it in their own phase (D26, Phase 3), and the
  generic `.qd-is-selected` stays untouched until then.
- `.qd-ltr-isolate` (`direction: ltr; unicode-bidi: isolate`) is the only sanctioned Latin island,
  applied to the **value element**, never a container.
- `.qd-hit-target` expands a small control to `--qd-hit-target-min` (44px) through a negative-inset
  `::after`, leaving the visible icon size alone.
- `.qd-flex-shrink-guard` / `.qd-flex-fixed` encode the Golden shrink guard (`flex: 1 1 0` +
  `min-inline-size: 0` for the flexing child, `flex: 0 0 auto; white-space: nowrap` for its
  siblings) — a bare `flex: 1` on a text input is the most common way a Compact row pushes the
  document past the viewport.

### 18.7 The gate

`npm run check:golden-ui` (`scripts/check-golden-ui-contract.mjs`) fails on: a band value that
disagrees with the JSON contract, a restated band literal in TypeScript or Tailwind, a raw
non-band `@media` threshold in a migrated file, a gradient / active-transform / hover-lift /
resting-shadow / physical inline property / colour literal in the Golden layer, `.qd-page` regaining
an inline gutter, a missing page-intent or rail or bounded-grid selector, a domain-named selector in
the layout layer, and any reappearance of the retired `qd-state` adapter.

Its `LEGACY_ALLOWLIST` is explicit and each entry names the phase that retires it. The list may only
shrink: an entry whose recorded count no longer matches reality fails the check, so a cleanup cannot
silently leave a stale allowance behind.

## 19. Golden UI controls and async owners (Plan 7, Phase 2)

Phase 2 implements F05 (action), F06 (field/control) and F12 (the five async concepts) and closes
D14, D16, D17, D18, D20 and D21. §18 stays the foundation; this section is what consumes it.

### 19.1 One action contract (F05)

`qdAction` is a **directive on a native `button` or `a`** (`shared/ui/action/`), never a custom
element. It adds `.qd-action` plus one variant class (`primary`, `secondary`, `tertiary`, `danger`,
`icon-only`, `toolbar`, `row-action`) and one size class (`sm`/`md`/`lg` → `--qd-control-sm/-md/-lg`
= `32/40/48`). The styling is the global `.qd-action*` family in `_components.scss`.

Load-bearing decisions:

- **Native activation and native `disabled` stay with the call-site.** The directive never writes
  `disabled`, never adds `role="button"` to an anchor, and never intercepts a click. Over-normalising
  a link into a button is how keyboard and middle-click behaviour gets lost.
- **The busy icon slot is reserved as soon as `busy` is bound at all**, not when it turns true
  (`.qd-action--busy-slot` vs `.qd-action--busy`). Reserving on `true` would resize the control at
  the exact moment the operator is waiting on it. An action that never declares a busy state gets
  no slot, so ordinary buttons keep their natural width. Busy sets `aria-busy` and keeps the label.
- **No active-state translate** (D14). Active and hover change tone only, and `.qd-btn` lost its
  `translateY(1px)` in the same change — the transition list no longer mentions `transform`.
- **Compact hit area** (D45): at `≤767` actions outside a modal use
  `--qd-hit-target-min` (44px), and `primary`/`lg` take `--qd-control-lg` (48px). Dense modal
  workflows use `--qd-control-sm` (32px) for actions and filter inclusion controls; density is an
  approved modal exception. Icon-only and row actions outside that exception retain the 44px box.
- **Padding-inline uses the spacing scale** (`8/12/16`), not the board's `10/14/18`: the Plan 7
  contract restricts every spacing step to `2,4,8,12,16,20,24,32,40,48,64`, and a control padding
  is a spacing step. The heights are the locked values and were not rounded.
- `row-action` is defined here as an always-mounted transparent icon action. The "reveal on
  hover/focus/selection at Wide" rule needs a row to hang off, so it belongs to the row families
  (F09/F10) and is not invented here as an unconsumed container class.

### 19.2 One field contract (F06)

`qd-form-field` owns `[label] [required marker] [control] [helper] [error]` and generates
`qd-field-N-control/-helper/-error` ids. The projected native control carries `qdControl`, which
resolves the field through the element injector (content projection preserves the injector
hierarchy) and binds `id`, `aria-describedby` and `aria-invalid` from it. A `qdControl` outside a
field is still a styled control but borrows nothing — it exposes its own `invalid` input.

- **Required is stated in text as well as `*`**: the glyph is `aria-hidden`, and an `.qd-sr-only`
  word carries the meaning.
- **Focus is `:focus-visible` only, 2px green at 2px offset, and changes no geometry** (D21, D42).
  `.qd-control`/`.qd-input`/`.qd-select` lost their `:focus` ring, their `box-shadow: var(--qd-ring)`
  and their focus border-colour change: a pointer click must paint nothing and a keyboard focus must
  not move the control.
- **One control geometry** (D20): `.qd-control`, `.qd-input` and `.qd-select` share
  `min-block-size: var(--qd-control-md)`, `--qd-radius-sm`, the hairline border, `--qd-font-ui` and
  `--qd-type-body`. The select's radius moved `md → sm` and its height `2.375rem → 2.5rem` so it
  matches inputs and actions.
- **Hover is neutral** (D16): `--qd-border-strong` + `--qd-surface-quiet`. The select's green
  `--qd-border-accent` hover is gone; green now appears only on the focus ring and on selection.
- **The chevron is a flat inline background image** (D17), replacing two `linear-gradient`s. A
  `background-image` SVG is an isolated document — it cannot read `currentColor` or a custom
  property — so the muted-ink hex inside that data URI is the one written colour in the styles layer
  outside `_tokens.scss`. It is not a token violation to be "fixed" by re-introducing a gradient or
  by adding a second chevron token; if it must change, change the encoded SVG.
  `background-position` has no logical form either, so the chevron is positioned from `left`, which
  is the inline-end edge under the app's RTL direction and exactly where the gradient chevron sat.
- Disabled controls use `--qd-neutral-tint` + `--qd-neutral-ink-disabled`, never opacity alone, and
  an invalid control shows a danger hairline **plus** the error text — never colour alone.

### 19.3 Five async owners, five geometry contracts (F12)

| Concept | Owner | Role / live behaviour | Geometry it owns |
|---|---|---|---|
| Skeleton / loading | `qd-skeleton-rows`, `qd-panel-skeleton` (`lines`/`rows`/`panel`/`text`) | `aria-busy` + one sr-only `role="status"` (visible label for `text`) | the **final** shape of the surface it replaces |
| Refreshing | `qd-refreshing-indicator` | none — no role, no live region, `aria-hidden` | nothing; a 2px absolute track on `.qd-refreshing-region` |
| Empty | `qd-empty-state` | `role="status"` | its own content region |
| Error / notFound | `qd-error-state` | `severity="read"` → no role; `severity="write"` → `role="alert"` | its own content region; `reserve` keeps a mounted region quiet |
| Notice | `qd-notice` | `role="status"` + `aria-live="polite"`, always mounted | **zero** idle height; grows only while a message exists (D41) |

The read/write split is the point: a failed read is announced once through the workspace's polite
region and offers a scoped retry, so making it an alert interrupts twice; only a write failure is an
alert, and it never clears the draft. Refreshing announces nothing itself — the region it decorates
keeps its content and carries `aria-busy`, which is why the indicator must not be given a status,
alert or dialog role.

`reserve` + `.qd-state--reserve` / `.qd-state--reserve-empty` / `.qd-state__message` (legacy
class names kept as the backing layer) are now the
shared reserved-live-region vocabulary of the empty, error and text-loader owners (the styles moved
from `state.component.scss` into `_components.scss`, because a component stylesheet cannot reach
markup another component renders). The names keep their legacy spelling on purpose: two feature
families and one page still consume them, and renaming them would be a call-site migration, not a
rename.

`.qd-empty-state` / `.qd-loading-state` / `.qd-error-state` keep their current padding and
alignment. Their Golden retune (padding 16, `min-block-size: min(40vh, 20rem)` for read states) is
deliberately **not** done here: roughly sixty unmigrated feature templates use those classes
directly, and changing their reserve behaviour globally is exactly what Phase 2 is told not to do.
Each family retunes them as it migrates.

### 19.4 What the gate now enforces

On top of §18.7, `npm run check:golden-ui` fails when any of the six F12 owner files is missing and
when `state.component.html` declares a `role`, `aria-live`, `aria-busy` or `qd-*` class — the
adapter may only delegate. Its legacy allowlist lost the four Phase-2 entries (the skeleton shimmer
gradient, the `.qd-btn:active` translate, and the two select-chevron gradients), so the control and
state layer now carries zero gradients and zero active transforms with no allowance at all.

## 20. Golden UI interaction primitives (Plan 7, Phase 3)

Phase 3 implements F07 (tabs), F08 (toolbar zones), F10 (result list), F11 (details workspace),
F13 (pagination), the F14 modal base, the F15 floating base and F17, and closes D19, D25, D26, D27,
D31, D42, D43, D44 and D45 at the shared layer. §18 is the foundation, §19 the controls and async
owners; this section is the interaction layer that composes both. Broad modal/picker migration is
Phase 7 — what is built here is the base every later consumer resolves to.

### 20.1 One tab contract (F07)

`qd-tabs` keeps its selector, its `ariaLabel`/`orientation`/`layout` inputs and its consumer-owned
selection. Phase 3 added, without changing any of that:

- **Per-instance ids** (D31). The tablist gets `qd-tabs-N`; each `qdTab` gets
  `qd-tabs-N-tab-M`. These are **fallbacks written in `ngAfterViewInit`, only when the element has
  none.** `abwab-move-picker` binds `[id]="sectionTabId(...)"` and `access-admin-page` binds
  `[attr.id]`/`[attr.aria-controls]`; a host binding on the directive would have won the update
  pass and removed theirs. `panelId` and `disabledReasonId` behave the same way.
- **Layout by count, never by wrap** (D30). Three tabs or fewer render `qd-tabs--segmented` on the
  sunken track with equal-width tabs. `.qd-tabs` is `flex-wrap: nowrap`, so a tablist can no longer
  form an accidental second row. An explicit `layout="grid"` or `layout="tracks"` opts out of the
  count-driven choice — and since Phase 11 that opt-out is the *only* answer for four or more:
  the `qd-tabs--scrollable` half of D30 is **retired**. It was a single row owning an
  `overflow-x: auto` inline scroller, which is exactly the RTL-hostile behaviour §17 and the
  `tracks` contract exist to remove, and by the end of the tabs migration no consumer could still
  resolve to it — the three remaining `inline` tablists (`unique-words-tabs` 2 tabs,
  `abwab-relations-modal` 3, `access-admin-page` 3) are statically bounded at the segmented
  maximum, and every variable-length strip is `tracks`. The class, its rules and the
  selected-tab `scrollIntoView` effect are gone; a strip that outgrows three tabs adopts
  `layout="tracks"`, it does not get a scroller back.
- **Selected treatment** is the Golden pill: green tint, `--qd-green-text`, and a 2px thread on the
  block-end edge (`box-shadow: inset`, because a border would change the tab's height). A vertical
  tablist gets the logical `border-inline-start` thread instead.
- **Selected-tab scroll-into-view on keyboard move** (`{ block: 'nearest', inline: 'nearest' }`),
  so a tab reached with Arrow/Home/End is brought into view by whatever ancestor scrolls. The
  companion `effect` that re-scrolled on every selection change went with `--scrollable`: with no
  scroller inside the primitive it could only ever move an ancestor the user did not ask to move.

The RTL arrow mapping was already correct and is unchanged: ArrowLeft is the logical *next* tab in
RTL because that is the next tab in visual order.

### 20.2 Result list and details workspace (F10, F11)

`qdResultList`/`qdResultItem` are a native-role directive pair, not a data component: they add
`role="list"`/`listitem` (D25), the logical selected thread (D26), `aria-current` on the selected
master row, and optional set metadata. They add **no** `tabindex` — a truncated value is disclosed
through the §8.1 ladder on the owning control, and manufacturing a tab stop per truncated node is
explicitly prohibited.

Dense workspace rows use the native membership control as the state authority. The logical
inline-start selected thread is only visual reinforcement; flat result-list siblings must not add
card elevation, nested list shells, or hover-only actions.

`qd-details-workspace` is the projected details anatomy: identity, metadata, actions, an optional
tab zone, a permanently mounted polite status slot with zero idle geometry, exactly one body
scroller, and an optional footer. Every zone is `<ng-content>`; the shell holds no feature data and
takes no domain input. Its `tabId(key)`/`panelId(key)`/`statusId` namespace is per instance, which
is what lets an inline detail panel and the global overlay body coexist (D31).

### 20.3 Pagination geometry (F13)

The jump input is `--qd-pagination-jump-inline-size` (`6rem`) in every state — the `:focus` widen is
gone (D42). Go is always mounted and only toggles `disabled` (D43). `jumpSubmittable` is
"parses to a number", **not** "is in range": an out-of-range page has to stay submittable, because
submitting is what produces the range error in the reserved error line. That line is now permanently
mounted and toggles `visibility`, so a failed jump adds no height. Ids for the input, the error line
and the live region are per instance (D44), and every page change announces the new **result range**
through that instance's polite region. Compact controls take `--qd-hit-target-min` (D45). Audit
`Load more` remains a separate capability with none of this API.

### 20.4 Modal and floating bases (F14, F15)

`qd-modal-shell` owns four named widths and nothing else may exist: `confirm` 30rem, `form` 38rem,
`wide` 52rem, `overlay` 46rem. A consumer may override only the existing `wide` variant's inline and
block size through its neutral shell inputs; the Linking Workspace uses this for its 80vw by 80dvh
working area. Abwab workflow pickers share the same `60rem` by `min(94dvh, 48rem)` override for
move, template copy, inclusions, and relations. Compact remains the full-bleed `94dvh` sheet with safe-area padding
(D48). The shell owns padding; header and footer are sticky siblings of the single scrolling body
(D49). `dismissed` carries its route (`close`/`escape`/`backdrop`) rather than being a bare void, so
a dirty authoring form can refuse the casual routes and keep an explicit close. Escape is consumed
by the topmost shell whether or not it dismisses: a shell that refuses the route still calls
`stopPropagation`/`preventDefault`, so an ancestor drawer or a document-level listener cannot close
in its place. Backdrop dismissal requires the pointer sequence to **start and end** on the backdrop
(the press target is recorded on `pointerdown`/`mousedown` and compared at `click`), so a drag-select
released outside the dialog never discards a draft.

Focus containment is CDK-trap based, but the shell — not each consumer — decides **which** trap is
live: shells register in an internal open-shell stack and only the topmost one enables its trap, and
`[trapFocus]="false"` lets a consumer suspend its own trap while it hosts a nested decision. Exactly
one trap is enabled across nested shells.

**Focus return has exactly one owner: the shell.** It captures the pre-open `activeElement`, drives
initial focus through the trap itself (there is no `cdkTrapFocusAutoCapture` second restorer), and
restores **synchronously** on close and on destroy-while-open. A consumer that wants to place focus
itself sets `[returnFocus]="false"` and owns it end to end; nothing restores asynchronously behind
its back. The reference-counted
scroll-lock hold is released on close **and** on destroy, so a shell torn down while open cannot
strand the page. Making the route content behind the dialog `inert` remains an app-shell concern
(`app.ts` for the global overlay, `ScrollLockService.isLocked` for the navbar) — the shell does not
reach outside itself, and D13's nav-sheet work stays in Phase 10.

`qdFloatingLayer` is one keyboard script for five variants (D33) and `placeFloatingLayer()` is the
pure geometry (D34): block-axis flip when the preferred side cannot hold the layer and the other
side has more room, inline clamp to an 8px viewport margin, `min(60vh, 24rem)` cap, and
`position: fixed` so a layer never joins document flow. Items are located by ARIA role, never by a
shared option model, so grouped and flat hierarchies stay feature-owned (G12).

**One option model per variant** (F15 §16): `action-menu` roves real DOM focus and never carries
`aria-activedescendant`; `select-listbox` and `searchable-picker` keep DOM focus on the field (or on
the layer) and move an `aria-activedescendant` cursor, which is cleared when the layer closes or its
variant changes. Key handling is scoped by event target: inside a text input, textarea or
`contenteditable`, printable keys, Home/End and the caret arrows stay with the field, and only
ArrowUp/ArrowDown drive the option cursor. Type-ahead
accumulates inside a 600ms window and searches forward from the active item; an empty or
whitespace-only prefix never matches, and Space extends a type-ahead already in progress but
otherwise belongs to the focused item (APG). The `24rem` half of the cap resolves against the live
root font size (`resolveRootFontSize()`), so JS can never disagree with
`--qd-floating-max-block-size`. The computed placement
is written to `left` because a viewport coordinate has no logical form — the *choice* of anchored
edge is direction-aware, which is what RTL actually needs. `context-menu-placement.ts` has since
been retired into this helper; the context menu carries no placement math of its own.

### 20.5 Chip, badge and toolbar semantics (F08, F17)

The Angular `qd-chip` owns the **interactive** families only, through a `variant` input
(`filter`/`taxonomy`/`alias`; `plain` is the default and adds no class). Static badges have no
interaction and therefore no Angular owner: `.qd-badge--lifecycle-pending/-active/-disabled/-unknown`,
`.qd-badge--membership-owner` and `.qd-count-chip` are semantic classes in `_components.scss`.
Lifecycle and Owner membership are separate badges and Unknown resolves to the neutral tokens with
its own literal copy — it is never rendered as Disabled (G17). Every one of them states its meaning
in text; colour is reinforcement only.

`.qd-toolbar` supplies the F08 zones (`__identity`, `__filters`, `__result`, `__actions`,
`__applied`) plus the `explorer`/`taxonomy`/`workspace` modifiers. It is a semantic layer, not an
Angular wrapper: it owns no draft/applied value and emits nothing, and a feature composes it with
its own filter fields. It carries **no entrance animation** — `uw-toolbar-rise` is deleted (D19),
and the checker fails on any `@keyframes` whose `from` frame translates.

### 20.6 What the gate now enforces

On top of §18.7 and §19.4, `npm run check:golden-ui` fails when a Phase 3 owner file is missing,
when `modal-shell.component.scss` declares anything other than the four named width classes, when a
selection edge is written as a physical `box-shadow: inset -Npx 0` instead of the logical thread,
when a shared partial regains decorative entrance motion, or when a `.qd-truncate` node in a shared
template carries `tabindex="0"`.
