<!-- Visual source of truth: the approved flat parchment + green comps in
     docs/design-preview/ (read docs/design-preview/README.md first — it records the
     divergences that were reconciled into this document). This direction SUPERSEDES
     the earlier Real Pages navy + gold + parchment identity
     (Frontend/quran-dashboard-ui/report/ui/real-pages-visual-system-extraction-report.md)
     for the LIGHT theme; the dark theme still runs the interim navy + gold values
     pending reconciliation. Token contract:
     Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md; implemented tokens:
     src/styles/_tokens.scss (light) and src/styles/_themes.scss (dark). -->
---
name: Quran Dashboard (Manhag Qurany)
description: Arabic-first, scholarly-calm dashboard for curating Quran research data
---

# Design System: Quran Dashboard (Manhag Qurany)

## 1. Overview

**Creative North Star: "The Quiet Scriptorium"**

A scriptorium is where texts were copied and ordered with patience and care. That
is the feeling this dashboard should evoke: a calm room for serious textual work,
where the gravity of the material shows through restraint rather than decoration.
The references that anchor it are the reverent Arabic typography and reading space
of Quran.com, the long-session typographic calm of reading tools like Readwise
Reader and Instapaper, and the craft of a printed mushaf: parchment, naskh,
careful margins.

The interface is **Arabic-first and right-to-left** by default, not retrofitted.
The visual identity is **flat parchment + one scholarly green**, with **navy as a
footer-only anchor** — the approved comps in `docs/design-preview/` are the visual
reference for this direction. Color is **restrained**: a warm parchment canvas and
near-white cards carry the surfaces, and a single scholarly **green** is both the
structural color (primary actions, brand) and the accent (state, emphasis, focus).
Deep **navy** appears nowhere in the light theme except the footer. Content leads;
the chrome stays quiet so the ayah and research text are what the eye lands on.
Depth is **drawn, not cast**: hairline borders and the tonal surface ladder carry
all structure; shadows exist only under floating layers, and nothing lifts on
hover. Motion is minimal, present only to confirm state, and never touches Quran
text.

This system explicitly rejects, per PRODUCT.md: the **generic SaaS template**
(gradient stat cards, cookie-cutter admin layouts), **kitschy religious decor**
(gold filigree, crescents, mosque clipart), **consumer / gamified** UI (emoji,
badges, reward mechanics), and **dense enterprise greige** (joyless gray-on-gray).

**Key Characteristics:**
- Arabic-first, RTL by default; direction-aware throughout.
- Flat parchment + one scholarly green: warm parchment canvas, near-white cards,
  hairline borders, green as both primary and accent; navy demoted to footer-only.
- Quran/Mushaf content keeps its current naskh faces (Amiri etc.), unchanged; a
  clean Arabic sans (IBM Plex Sans Arabic) carries UI chrome.
- Quiet and **flat**; separation comes from the tonal surface ladder and hairline
  borders. A single shadow exists, reserved for floating layers only.
- Light navbar (opaque, flat, hairline bottom border), dark navy footer/anchor;
  calm for long focus; reverence through restraint, never ornament.
- Light + dark: light implements this green direction; dark still runs the interim
  navy + gold values (see §2, dark reconciliation pending).

## 2. Colors: Parchment + Ink + One Scholarly Green

A warm parchment canvas, warm near-black ink, near-white cards, and one scholarly
**green** that serves as both structural color and accent. The values below are the
**implemented** OKLCH `--qd-*` tokens (hex shown for readability); the source of
truth is `src/styles/_tokens.scss` (light) and `src/styles/_themes.scss` (dark).
All text/background pairs listed here are verified **WCAG AA ≥ 4.5:1**.

### Light theme (approved direction) — implemented values

| Role | Token(s) | Value | Notes |
|------|----------|-------|-------|
| Page / app background | `--qd-bg` | `#f6f4ee` = `oklch(0.967 0.008 91.5)` | Warm parchment canvas. |
| Card background | `--qd-surface` | `#fffdf8` = `oklch(0.994 0.007 88.6)` | Near-white card, defined by its hairline border — no shadow. |
| Section / quiet background | `--qd-section-bg` | `#fbf8f0` = `oklch(0.979 0.011 89.7)` | Quiet groupings, reading paper. |
| Nested / recessed background | `--qd-surface-recessed` | `#f0ede2` = `oklch(0.945 0.015 94.2)` | Deeper insets; also the explorer table-header recess and the hover fill. |
| Text / ink | `--qd-text` | `#2b2a26` | Warm near-black ink. |
| Text-muted | `--qd-text-muted` | `#6f6b62` | Secondary text, labels, table headers. |
| Border | `--qd-border` | `#e7e2d7` | The hairlines that carry all structure. |
| Border-strong | `--qd-border-strong` | `#d5cfbf` | Hover edges, outlined controls. |
| Primary (structural) | `--qd-primary` | green `#2f6d5f` = `oklch(0.490 0.068 176.3)` | Primary buttons, brand mark. Same hue as the accent — one voice. |
| Primary foreground | `--qd-primary-fg` / `--qd-accent-fg` | `#f4faf7` | Ink on solid green. |
| Accent | `--qd-accent` | `#2f6d5f` (== primary) | Active state, selection indicators, focus. |
| Accent-hover / deep | `--qd-accent-hover` / `--qd-primary-hover` | `#245448` = `oklch(0.409 0.056 174.7)` | Hover/pressed green. |
| Accent-text | `--qd-accent-text` | `#275c50` = `oklch(0.435 0.060 176.3)` | AA text shade for green emphasis on light surfaces. |
| Accent-soft | `--qd-accent-soft` / `--qd-border-accent` | `#bcd6cc` | The one soft-green selected/active border. |
| Accent-tint | `--qd-accent-tint` (aliased by `--qd-selected-bg`) | `#eaf2ee` | Selected backgrounds, active-nav pill, soft-button bg. |
| Focus ring | `--qd-focus-ring` / `--qd-ring` | green; halo = green @ 22% | Soft `:focus-visible` halo. |
| Warning | `--qd-warning` on `--qd-warning-tint` | amber `oklch(0.540 0.111 75.1)` on `#fbf1dc` | 4.58:1. |
| Danger | `--qd-danger` on `--qd-danger-tint` | `#a44a3f` on `#f9ece8` | 5.01:1. |
| Success | `--qd-success` on `--qd-success-tint` | `oklch(0.546 0.062 162.7)` on its tint | 4.58:1 (unchanged). |

**Data / segment palette.** The six `--qd-segment-cat-*` morphology colors are
re-tuned to desaturated, parchment-friendly values — `#8a5f2a`, `#2f6d5f`,
`#a04b40`, `#46617d`, `#6d5680`, `#33736b` — with their function unchanged. Mushaf
segment cards are flat: surface background + hairline border + a **3px
segment-colored inline-start edge** (the old gradient wash is removed).

### Footer palette (light) — the one navy anchor

Navy is **footer-only**. The footer is a flat solid navy block: the radial glow
layer and the gold gradient top hairline are both removed.

| Role | Token | Value | Notes |
|------|-------|-------|-------|
| footer-bg | `--qd-footer-bg` | `#13253a` | Flat solid navy; `--qd-footer-bg-2` == footer-bg (glow removed; token kept for architecture). |
| footer-text | `--qd-footer-text` | `oklch(0.924 0.016 95.2)` | Warm off-white. |
| footer-muted | `--qd-footer-muted` | `oklch(0.708 0.036 257.9)` | Muted blue-grey secondary text. |
| footer-accent | `--qd-footer-accent` | sage `#a8c8ba` | Section headings / link hover. |
| footer-accent-hover | `--qd-footer-accent-hover` | `#bcd6cc` | Link hover. |
| footer-border | `--qd-footer-border` | white @ 10% | Translucent hairlines inside the footer. |

### Dark theme (interim — dark reconciliation pending)

Dark still runs the **previous navy + gold** adapted-midnight values (deep
blue-black surface ladder, gold accent & primary — see
`src/styles/_themes.scss`) and stays fully functional. Full reconciliation of dark
to the green direction is a **deliberately deferred later task**. Two minimal dark
changes shipped with the light restyle:

- `--qd-accent-fg` is now overridden in dark to navy ink (dark's solid accent is
  still gold, which needs dark ink);
- dark `--qd-chrome-bg` became opaque (backdrop blur is removed globally).

Theme-neutral shape/motion changes — lift removal, the crisper radii, and the flat
navbar/footer geometry — apply to dark as well; dark keeps its own (heavier)
shadow values until reconciliation.

### Named Rules
**The One Voice Rule.** The green accent appears on no more than ~10% of any given
screen — active state, links, icon highlights, section eyebrows, the primary
action, and review/publish state. Its rarity is what gives it meaning. Green is
now also the structural color (primary buttons, brand), and structural uses obey
the same restraint: one primary action per view, calm everywhere else. Navy is
footer-only and is never an accent.

**The allowed-green list (locked).** Green (`--qd-accent` / `--qd-accent-soft`) may
appear **only** as:
1. `:focus-visible` ring/halo (`--qd-focus-ring` / `--qd-ring`).
2. The 2px selection indicator bar or the selected dot (fill), with
   `--qd-accent-fg` ink if it carries a glyph.
3. A 1px selected/active border (`--qd-accent` or `--qd-border-accent`).
4. Text emphasis via `--qd-accent-text` (active nav, links, soft/selected labels,
   section eyebrows) — never raw `--qd-accent` as small text on light.
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
fills, resting borders — stays banned as solid green: use a tint,
`--qd-accent-text`, or a hairline border instead. This list is the authoritative
source mirrored into `UI_STYLE_SYSTEM.md` §16.3 — keep the two in sync if either
changes. (It supersedes the previous allowed-gold list verbatim: same discipline,
new hue.)

**The Green Thread.** The signature of the system: a **2px green line or edge
means "current" everywhere** — the active tab, the selected table/list row's
inline-start edge, and the mushaf word-selection indicator
(`--qd-mushaf-word-selection-indicator`, green via `--qd-accent`). One hue, one
meaning.

**The Warm Neutral Rule (revised).** The parchment canvas and neutrals stay tinted
warm; pure `#000` is still forbidden. **Near-white elevated cards are allowed**
when they sit on the warm parchment background and are defined by a hairline
border — the warm canvas + border keep them from reading cold; no shadow is used
or needed. Do not use a flat pure-white *page canvas*; the canvas is parchment.

## 3. Typography

**Quran / Content Font:** the app's **current** Quran/Mushaf faces (Amiri for
Mushaf/verse text, with the existing ayah-marker face). These are **sacred and
stable: do not change or replace Quran/Mushaf glyph fonts or Quran rendering**, and
never animate Quran text. The prototype agrees (it also uses Amiri for verse text),
so no change is needed here.

**UI Font (adopted from prototype):** **IBM Plex Sans Arabic** for Arabic UI chrome
and **IBM Plex Sans** for Latin UI. Use weights **400 / 500 / 600 / 700** where
available — mid-weights (500/600) carry the nav, cards, labels, and footer hierarchy
the prototype relies on. Headings use slightly tight tracking; large/section titles
may scale fluidly.

**Character:** A scholarly contrast. The naskh/Amiri face carries the weight and
dignity of the textual material; the Plex sans keeps the working chrome quiet and
legible. The pairing should feel like a well-set Arabic book whose apparatus stays
out of the way.

### Hierarchy
- **Display** (naskh, large): ayah text and primary headings. Generous line-height
  for Arabic; correct rendering of diacritics (tashkeel) is mandatory.
- **Headline / Title** (sans or naskh per context, medium weight): section and
  panel headers.
- **Body** (sans, regular): UI prose and descriptions. Cap prose at 65–75ch; dense
  data and tables may run wider.
- **Label** (sans, smaller, medium weight): field labels, table headers, metadata.

### Named Rules
**The Content-Leads Rule.** Quran and research text gets the naskh face and the most
space on any screen. Interface chrome stays in the quiet sans and never competes
with the content.

**The Genuinely-RTL Rule.** Layout, alignment, spacing logic, and iconography are
designed right-to-left from the start. Arabic typography is the baseline, not a
mirrored afterthought.

## 4. Elevation and Motion

The light theme is **fully flat**. Separation comes from two cooperating layers:
the **surface ladder** (parchment page → near-white card → quiet section →
recessed tones) and **hairline borders** (`--qd-border` and a stronger
`--qd-border-strong`). There are **no resting card shadows, no hover shadows, no
hover lifts, no gradients, and no backdrop blur** anywhere in light.

**Shadow tokens (light):**
- `--qd-shadow-sm` and `--qd-shadow` are `none` — flat by contract, not by habit.
- `--qd-shadow-lg` / `--qd-floating-shadow` are the **single floating-layer
  shadow**, reserved for layers that genuinely float above the page: dropdowns,
  popovers, modals, drawers.
- Dark keeps its own heavier shadow values until dark reconciliation (§2).

**Card behavior:** cards rest with a hairline border and no shadow. On hover they
may shift **fill and/or border only** (`--qd-surface-hover`,
`--qd-border-strong`) — **no `translateY` lift, no shadow, no scale-up**. The
navbar is an opaque flat surface with a hairline bottom border (no blur, no
shadow); the footer is flat solid navy.

**Shape:** radii are crisp — `--qd-radius-sm` 0.4375rem (7px, controls),
`--qd-radius-md` 0.625rem (10px, cards), `--qd-radius-lg` 0.875rem (14px, feature
surfaces and modals), `--qd-radius-pill` 999px (chips/pills).

**Motion contract (unchanged):** two duration tokens — a **fast** hover transition
(~140ms ease) and a **base** transition (~220ms `cubic-bezier(.2,.7,.3,1)`).
Subtle only: color/fill/border transitions on surfaces, small transforms reserved
for floating layers, no bounce, no showy animation. Respect
`prefers-reduced-motion`. **Never animate Quran/Mushaf text.**

### Named Rules
**The Flat Rule** (supersedes the Soft-Elevation Rule). Surfaces separate by tone
and hairline border alone. Exactly one shadow exists in light, and it belongs
exclusively to floating layers (dropdowns, popovers, modals, drawers). Cards never
cast a shadow and never lift — at rest or on hover.

**The Calm-Motion Rule.** Motion confirms state and stays subtle (fill/border
transitions on cards; ≤ ~12px translate for floating layers), quick, and
bounce-free. Quran text is never animated.

## 5. Do's and Don'ts

### Do:
- **Do** design **RTL-first**: mirror layout, spacing, and icons, and test every
  screen with real Arabic text and tashkeel.
- **Do** keep green on **≤10%** of any screen (the One Voice Rule); use it for the
  primary action, current selection, focus, and review/publish state only — and
  only in the forms the allowed-green list permits.
- **Do** let content lead: naskh/Amiri and generous space for ayah/research text,
  quiet Plex sans for the chrome (the Content-Leads Rule).
- **Do** convey depth with the **surface ladder and hairline borders alone** (the
  Flat Rule); reserve the one shadow for floating layers.
- **Do** keep the **navbar an opaque, flat near-white surface with a hairline
  bottom border**, distinct from content; make the active nav item
  `--qd-accent-text` green on a green `--qd-accent-tint` pill.
- **Do** make the **footer a flat deep-navy anchor** with warm off-white text and
  sage (`--qd-footer-accent`) accents — the only navy in the light theme.
- **Do** mark "current" with the **green thread**: a 2px green indicator/edge for
  the active tab, the selected row, and the selected mushaf word.
- **Do** make review and publish states distinguishable **without relying on color
  alone** (icon, label, or shape as well).
- **Do** keep motion to **state changes only** (hover, focus, selected, loading),
  subtle and bounce-free; respect reduced-motion; never animate Quran text.

### Don't:
- **Don't** build a **generic SaaS template**: no identical gradient stat cards, no
  hero-metric template, no cookie-cutter admin dashboard layout, no endless
  identical card grids.
- **Don't** add **kitschy religious decor**: no filigree, crescent moons, or
  mosque clipart. Reverence comes from restraint, not ornament.
- **Don't** go **consumer / gamified**: no bright playful palettes, emoji, badges,
  or reward-style UI. This is professional curation work.
- **Don't** fall into **dense enterprise greige**: cramped gray-on-gray tables with
  no breathing room. Density without care is not seriousness.
- **Don't** use a pure-white *page canvas* or pure `#000`; the canvas is parchment
  and neutrals stay tinted warm. Near-white **elevated cards** are allowed when
  paired with the parchment background and a hairline border (revised Warm Neutral
  Rule) — never with a shadow.
- **Don't** use gradients, gradient text, glassmorphism, or backdrop blur —
  **anywhere, zero exceptions**. The two previously sanctioned exceptions (the
  footer's gradient top hairline and the optional translucent navbar blur) are
  removed. *Decorative* colored side-stripes remain banned; the only sanctioned
  semantic edges are the 2px green thread (current/selected) and the 3px
  segment-colored inline-start edge on mushaf segment cards.
- **Don't** paste raw CSS, inline styles, or hex values into Angular; every value
  lives as an OKLCH `--qd-*` token in `src/styles/_tokens.scss` /
  `src/styles/_themes.scss` and is consumed through the token system.
