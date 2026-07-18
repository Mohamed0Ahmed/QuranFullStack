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
existing call-site onto this doctrine was a phased migration tracked in
`docs/feature-028-color-doctrine-unification/plan.md` (P1–P7); the migration is
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
> (`qd-skeleton-rows`, `qd-panel-skeleton`) are Angular components shipped in P2 of
> `docs/feature-028-color-doctrine-unification/plan.md`; `.qd-explorer-table` and
> `.qd-detail-list` are CSS class-family collapses shipped in P3/P4. Chip/tab
> call-sites and the solid-accent-fill ban landed in P5; density/motion/radius/ladder
> cleanup in P6; the remaining ad-hoc text-loading states (dashboard-home,
> mushaf-page-area) moved onto `qd-skeleton-rows`/`qd-panel-skeleton` in P7 — the
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
  popovers, count badges).
- **Inputs / roles:** `selected`, `disabled`, `as?='button'|'a'`, optional
  trailing `count`.
- **Selected / hover / disabled:** selected = `--qd-selected-bg` +
  `--qd-accent-text` + `--qd-border-accent` (§16.1) — **no solid green fill**;
  hover = `--qd-surface-hover`; disabled is visually muted and non-interactive.
- **Backing classes:** `.qd-chip`, `.qd-chip--pill`, `.qd-chip.qd-is-selected`,
  `.qd-chip__count`. Compose, do not re-style.

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
