<!-- Visual source of truth: the Real Pages prototype (/projects/Real Pages), adopted
     WITH ADAPTATION as the official navy + gold + parchment identity. See
     Frontend/quran-dashboard-ui/report/ui/real-pages-visual-system-extraction-report.md
     and the future token contract in
     Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md. -->
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
The visual identity is adopted from the **Real Pages prototype**: a **navy + gold
+ parchment** system. Color is still **restrained**: a warm parchment canvas and
near-white elevated cards carry the surfaces, deep **navy** is the structural color
(primary actions, brand, footer), and a single muted **gold** accent appears rarely
for state and emphasis. Content leads; the chrome stays quiet so the ayah and
research text are what the eye lands on. Depth is real but soft: cards rest on a
gentle shadow and lift slightly on hover. Motion is minimal, present only to confirm
state, and never touches Quran text.

This system explicitly rejects, per PRODUCT.md: the **generic SaaS template**
(gradient stat cards, cookie-cutter admin layouts), **kitschy religious decor**
(gold filigree, crescents, mosque clipart), **consumer / gamified** UI (emoji,
badges, reward mechanics), and **dense enterprise greige** (joyless gray-on-gray).

**Key Characteristics:**
- Arabic-first, RTL by default; direction-aware throughout.
- Navy + gold + parchment: warm parchment canvas, near-white elevated cards, deep
  navy structural color, one muted gold accent used sparingly.
- Quran/Mushaf content keeps its current naskh faces (Amiri etc.), unchanged; a
  clean Arabic sans (IBM Plex Sans Arabic) carries UI chrome.
- Quiet but layered; depth comes from a soft surface ladder **and** controlled soft
  shadows (a real, restrained elevation ladder), plus hairline borders.
- Light navbar, dark navy footer/anchor; calm for long focus; reverence through
  restraint, never ornament.
- Light + dark only (prototype *ivory* → light, *midnight* → dark; *sage* not
  adopted).

## 2. Colors: Navy + Gold + Parchment

Adopted from the Real Pages prototype. A warm parchment canvas, near-white elevated
cards, deep **navy** as the structural color, and a restrained **gold** accent. The
hex values below are the prototype's **visual reference**; implementation must
re-author them as the app's **OKLCH `--qd-*` tokens** (not paste the hex), defined
for **both** themes. Light = adapted prototype *ivory*; dark = adapted prototype
*midnight*. *Sage* is not adopted.

### Light theme (adapted ivory) — reference values

| Role | Reference (prototype hex) | Notes |
|------|---------------------------|-------|
| Page / app background | `#FCFAF4` | Warm parchment canvas; cards lift above it. |
| Section / quiet background | `#F6EFE5` | Recessed/quiet groupings. |
| Card background | near-white (prototype `#FFFFFF`) | Elevated content card (see white-card rule below). |
| Nested / recessed background | `#EFE3D3` | Deeper insets. |
| Border | navy @ ~12% (`rgba(18,38,58,.12)`) | Hairline dividers/card edges. |
| Border-strong | navy @ ~22% (`rgba(18,38,58,.22)`) | Hover edges, outlined controls. |
| Text | `#1F2937` | Primary text/ink. |
| Text-muted | `#667085` | Secondary text, labels. |
| Primary (structural) | navy `#12263A` | Primary buttons, brand mark. |
| Primary foreground | `#FCFAF4` | Text/icon on primary. |
| Accent | gold `#C79D43` | Active state, links, icon highlight, eyebrow. |
| Accent-hover | `#B68A30` | Accent hover. |
| Accent-soft | `#E5C98A` | Selection backgrounds, soft hover borders. |
| Accent-tint | `#FAF1DD` | Active-nav pill bg, soft-button bg, icon chip bg. |
| Focus ring | gold @ ~22% (`rgba(199,157,67,.22)`) | Soft `:focus-visible` halo. |
| Danger / success | `#B14848` / `#4E7C66` | Status (warning already exists in-app). |

### Footer palette (dark anchor; shared light/dark, deeper in dark)

| Role | Reference (prototype hex) | Notes |
|------|---------------------------|-------|
| footer-bg | `#0F1F33` (dark `#080D1A`) | Deep navy anchor. |
| footer-bg-2 | `#163149` (dark `#0F1626`) | Gradient glow layer. |
| footer-text | `#E9E4D7` | Warm off-white. |
| footer-muted | `#8C99B0` | Muted blue-grey secondary text. |
| footer-accent | `#D6B56D` | Gold section headings / link hover. |
| footer-border | `rgba(255,255,255,.08)` | Translucent hairlines inside footer. |

### Dark theme (adapted midnight) — reference values

Deep blue-black canvas with a stepping surface ladder and gold accent. Reference:
bg `#0D1322`, surface `#141C2E`, surface-2 `#1B2538`, surface-3 `#232E45`, border
`#28324A`, border-strong `#3A476A`, text `#E8E9EE`, text-muted `#98A0B5`, accent &
primary gold `#D4AF6A`, primary-fg `#0D1322`. Shadows are re-tuned heavier/darker for
dark mode (do not reuse light-mode shadow alphas).

### Named Rules
**The One Voice Rule.** The gold accent appears on no more than ~10% of any given
screen — active state, links, icon highlights, section eyebrows, and review/publish
state. Its rarity is what gives it meaning. Navy is structural, not an accent, and
may appear more (primary buttons, brand, footer) but still calmly.

**The allowed-gold list (locked).** Gold (`--qd-accent` / `--qd-accent-soft`) may
appear **only** as:
1. the `:focus-visible` ring/halo (`--qd-focus-ring` / `--qd-ring`);
2. the 2px selection **indicator** bar or the selected **dot** (fill), with
   `--qd-accent-fg` ink if it carries a glyph;
3. a **1px selected/active border** (`--qd-accent` or `--qd-border-accent`);
4. **text** emphasis via `--qd-accent-text` (active nav, links, soft/selected
   labels, section eyebrows) — never raw `--qd-accent` as small text on light;
5. footer gold (`--qd-footer-accent`) headings and link-hover;
6. icon highlights and the mushaf word-selection indicator
   (`--qd-mushaf-word-selection-indicator`).

Everything else — chip fills, badge fills, count fills, range badges, selected-row
fills, `qd-select` resting border — is **banned gold**: no solid gold at rest and no
gold fill behind text, anywhere; use a tint, `--qd-accent-text`, or a hairline
border instead. This list is the authoritative source mirrored into
`UI_STYLE_SYSTEM.md` §16.3 — keep the two in sync if either changes.

**The Warm Neutral Rule (revised).** The parchment canvas and neutrals stay tinted
warm; pure `#000` is still forbidden. **Near-white or white *elevated cards* are
allowed** when they sit on the warm parchment background and are defined by a border
and a soft shadow — the warm canvas + border + shadow keep them from reading cold.
Do not use a flat pure-white *page canvas*; the canvas is parchment.

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

Depth is **soft but real**, adopted from the prototype. Separation comes from three
cooperating layers: the **surface ladder** (parchment page → near-white card →
recessed tones), **hairline borders** (`border` and a stronger `border-strong`), and
a **controlled soft-shadow ladder**. Controlled soft shadows are **part of the
identity, required for elevation — not a rare exception.**

**Elevation ladder (reference):**
- `shadow-sm` — resting elevation on content cards (low alpha, navy-tinted in light).
- `shadow` — hover elevation on cards.
- `shadow-lg` — floating layers (dropdowns, popovers, modals, drawers).
- Dark mode uses heavier, darker shadows re-tuned for the deep canvas.

**Card behavior:** cards rest with a border + `shadow-sm`. On hover they gain a
**stronger border + stronger shadow + a small `translateY(-2px)` lift** (mini cards
≈ `-1px`; feature cards may deepen the shadow without a large move). **No scale-up**
on content cards.

**Motion contract:** two duration tokens — a **fast** hover transition (~140ms ease)
and a **base** transition (~220ms `cubic-bezier(.2,.7,.3,1)`). Subtle only: small
transforms, no bounce, no showy animation. Respect `prefers-reduced-motion`. **Never
animate Quran/Mushaf text.**

### Named Rules
**The Soft-Elevation Rule.** Surfaces use a tonal ladder **and** controlled soft
shadows together. Cards rest on `shadow-sm` and lift gently on hover; floating
layers use `shadow-lg`. Shadows are calm and low-contrast, never heavy or dramatic.

**The Calm-Motion Rule.** Motion confirms state and stays subtle (≤ ~2px card lift,
≤ ~12px for floating layers), quick, and bounce-free. Quran text is never animated.

## 5. Do's and Don'ts

### Do:
- **Do** design **RTL-first**: mirror layout, spacing, and icons, and test every
  screen with real Arabic text and tashkeel.
- **Do** keep the accent on **≤10%** of any screen (the One Voice Rule); use it for
  primary action, current selection, and review/publish state only.
- **Do** let content lead: naskh/Amiri and generous space for ayah/research text,
  quiet Plex sans for the chrome (the Content-Leads Rule).
- **Do** convey depth with the **surface ladder, hairline borders, and controlled
  soft shadows together** (the Soft-Elevation Rule). Cards rest on a soft shadow and
  lift slightly on hover.
- **Do** keep the **navbar light/near-white and distinct from content**; make the
  active nav item gold-accent text on an accent-tint pill.
- **Do** make the **footer a deep navy anchor** with warm off-white text and gold
  accents.
- **Do** make review and publish states distinguishable **without relying on color
  alone** (icon, label, or shape as well).
- **Do** keep motion to **state changes only** (hover, focus, selected, loading),
  subtle and bounce-free; respect reduced-motion; never animate Quran text.

### Don't:
- **Don't** build a **generic SaaS template**: no identical gradient stat cards, no
  hero-metric template, no cookie-cutter admin dashboard layout, no endless
  identical card grids.
- **Don't** add **kitschy religious decor**: no gold filigree, crescent moons, or
  mosque clipart. Reverence comes from restraint, not ornament.
- **Don't** go **consumer / gamified**: no bright playful palettes, emoji, badges,
  or reward-style UI. This is professional curation work.
- **Don't** fall into **dense enterprise greige**: cramped gray-on-gray tables with
  no breathing room. Density without care is not seriousness.
- **Don't** use a pure-white *page canvas* or pure `#000`; the canvas is parchment
  and neutrals stay tinted warm. Near-white/white **elevated cards** are allowed when
  paired with the parchment background, a border, and a soft shadow (revised Warm
  Neutral Rule).
- **Don't** use **decorative** gradients, gradient text, or **glassmorphism as a
  default**. Purposeful exceptions adopted from the prototype are allowed and scoped:
  the footer's subtle gradient top hairline, and an **optional** translucent navbar
  backdrop blur (performance-gated, with an opaque fallback). Colored side-stripe
  borders remain banned — if a stripe is tempting, rewrite the element.
- **Don't** paste prototype CSS, inline styles, or hex values directly into Angular;
  re-author every adopted value as an OKLCH `--qd-*` token in the app's SCSS system.
