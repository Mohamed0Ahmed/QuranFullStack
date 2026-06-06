<!-- SEED: re-run /impeccable document once there's code to capture the actual tokens and components. -->
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
Color is **restrained**: warm parchment surfaces and deep ink text carry almost
everything, with a single muted accent appearing rarely. Content leads; the chrome
stays quiet so the ayah and research text are what the eye lands on. Motion is
minimal, present only to confirm state.

This system explicitly rejects, per PRODUCT.md: the **generic SaaS template**
(gradient stat cards, cookie-cutter admin layouts), **kitschy religious decor**
(gold filigree, crescents, mosque clipart), **consumer / gamified** UI (emoji,
badges, reward mechanics), and **dense enterprise greige** (joyless gray-on-gray).

**Key Characteristics:**
- Arabic-first, RTL by default; direction-aware throughout.
- Warm parchment neutrals, deep ink text, one muted accent used sparingly.
- Naskh for content and headings; a clean Arabic sans for UI chrome.
- Flat and quiet; depth comes from tonal layering, not shadows.
- Calm for long focus; reverence through restraint, never ornament.

## 2. Colors: The Parchment & Ink Palette

A warm, low-chroma palette: parchment and ink, with one muted earthy accent. All
neutrals are tinted toward the warm brand hue. No pure white, no pure black.

### Primary
- **Muted Earthy Accent** (`[to be resolved during implementation]`, low-chroma
  warm hue in OKLCH): reserved for primary actions, the current selection, and
  review/publish state indicators only. Never decorative.

### Neutral
- **Parchment Surface** (`[to be resolved during implementation]`, warm off-white):
  the primary background. A second, slightly cooler-or-warmer layer distinguishes
  sidebars, toolbars, and panels from content.
- **Deep Ink** (`[to be resolved during implementation]`, near-black warm
  charcoal): primary text and high-emphasis UI.
- **Soft Border / Muted Ink** (`[to be resolved during implementation]`): hairline
  dividers, secondary text, and low-emphasis labels.

### Named Rules
**The One Voice Rule.** The accent appears on no more than ~10% of any given screen.
Its rarity is what gives it meaning; when everything is emphasized, nothing is.

**The Warm Neutral Rule.** Every neutral is tinted toward the brand hue (chroma
~0.005–0.01). Pure `#fff` and `#000` are forbidden; they read cold and cheap next
to parchment.

## 3. Typography

**Display / Content Font:** `[Arabic naskh face to be chosen at implementation]`
(naskh-rooted, for Quran/research content and headings).
**Body / UI Font:** `[Arabic sans to be chosen at implementation]` (a clean,
well-hinted Arabic sans for labels, controls, data, and dense UI).

**Character:** A scholarly contrast. The naskh face carries the weight and dignity
of the textual material; the sans keeps the working chrome quiet and legible. The
pairing should feel like a well-set Arabic book whose apparatus stays out of the
way.

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

## 4. Elevation

Flat by default. Motion and decoration are restrained, so depth is conveyed through
**warm tonal layering** (parchment surfaces at slightly different lightness) and
**hairline borders**, not drop shadows. If a shadow is ever introduced, it is a
response to state (a lifted menu, a focused dialog), never an ambient decoration.

### Named Rules
**The Flat-By-Default Rule.** Surfaces are flat at rest. Separation comes from tone
and hairlines; shadows appear only as a transient response to state.

## 5. Do's and Don'ts

### Do:
- **Do** design **RTL-first**: mirror layout, spacing, and icons, and test every
  screen with real Arabic text and tashkeel.
- **Do** keep the accent on **≤10%** of any screen (the One Voice Rule); use it for
  primary action, current selection, and review/publish state only.
- **Do** let content lead: naskh and generous space for ayah/research text, quiet
  sans for the chrome (the Content-Leads Rule).
- **Do** convey depth with **warm tonal layers and hairline borders**, not shadows.
- **Do** make review and publish states distinguishable **without relying on color
  alone** (icon, label, or shape as well).
- **Do** keep motion to **state changes only** (hover, focus, selected, loading);
  respect reduced-motion preferences.

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
- **Don't** use pure `#fff` or `#000`; tint every neutral warm.
- **Don't** use decorative gradients, gradient text, glassmorphism, or colored
  side-stripe borders. If a stripe or glass card is tempting, rewrite the element.
