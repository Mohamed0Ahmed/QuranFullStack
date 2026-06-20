# Real Pages — Visual System Extraction Report

**Type:** Read-only extraction / comparison. No application code, Product docs, Design docs, or `UI_STYLE_SYSTEM.md` were modified. No styles implemented. No commit.
**Source:** `/projects/Real Pages` (prototype/design reference, brand "الباحث القرآني").
**Target app:** `Frontend/quran-dashboard-ui` (brand "المنهج القرآني", themes `light` + `dark`).
**Companion:** `report/ui/color-usage-audit-report.md` (flatness audit of the current app).
**Date:** 2026-06-20

> Method: read the prototype's shared layout reference (`00 - layout system/...`), the
> homepage (`01 - الرئيسية/...`), and both page-5 Mushaf prototypes (`02 - ...`). All
> three share one token system, so token values below are quoted from the canonical
> layout file and verified identical in the others. OKLCH equivalents are computed for
> the Angular token convention (the app authors tokens in OKLCH).

---

## 1. Executive Summary

**`/projects/Real Pages` is a good visual reference for the real app, but it must be adapted, not copied.** It is a mature, internally consistent design system that solves exactly the problems the flatness audit raised: it has a real surface ladder, a real shadow/elevation ladder, a deliberate two-step border system, a dark "anchor" footer, an elevated translucent navbar, and a disciplined motion contract. Structurally it is everything the current Angular chrome is missing.

**What should be adopted (structure, not literal values):**
- The **surface ladder** (`bg → surface → surface-2 → surface-3`) and the idea that cards are *lighter and lifted* over a warmer page, not the same tone.
- The **elevation/shadow ladder** (`shadow-sm` resting on cards, `shadow` on hover, `shadow-lg` on floating layers) plus the focus `ring`.
- The **navbar treatment**: sticky, subtly translucent with backdrop blur, hairline bottom border, clearly distinct from cards.
- The **footer treatment**: deep navy/petrol anchor with a gradient top hairline, muted text, accent links, app-download buttons.
- The **card hover motion**: `translateY(-2px)` + border-strengthen + shadow-step, on a fast (~140ms) ease.
- The **motion contract**: two duration tokens (`--t-fast` 140ms, `--t-base` 220ms cubic-bezier), short transforms, no bounce.
- The **semantic accent layering** (`accent / accent-hover / accent-soft / accent-tint`) and **primary vs accent** separation.
- The **two-step borders** (`border` vs `border-strong`).

**What should NOT be copied:**
- The **three theme names** `ivory / sage / midnight`. The app supports `light` + `dark` only. Map ivory → light and midnight → dark; treat sage as out of scope.
- The **literal hue choices**. The prototype is a **navy + gold + parchment** system (gold accent `#C79D43`, navy footer `#0F1F33`). That conflicts with two other directions already in play (see the tension note below). Adopt the *roles and relationships*, decide the *hues* separately.
- Prototype-only mechanics: pure `#FFFFFF` surfaces, the `font-palette` tajweed word-coloring, inline `<style>` blocks, Google-Fonts CDN loading, fixed background radial-gradient washes, and any page-specific one-offs.

**Strategic tension to flag before any color adoption (important):** there are now **three** color directions floating around this product:
1. **Prototype** (this report): navy `#12263A` primary + **gold** `#C79D43` accent + parchment, with a navy footer.
2. **Current Angular `DESIGN.md` / tokens**: parchment + deep ink + one **muted earthy** accent (`--qd-accent` ≈ `oklch(0.45 0.08 55)`, a terracotta-brown).
3. **The Phase-1 chrome decision** (the navbar/footer task that preceded this): **teal / petrol / green** (`#34908B`, `#659287`, …).

These do not agree on hue. This report deliberately recommends adopting the prototype's **shape, elevation, surface hierarchy, and motion** (which all three directions can share) and explicitly defers the **hue** decision to the documentation-update step, so the team locks one accent family instead of inheriting a fourth.

**Verdict: USE WITH ADAPTATION** (see §17).

---

## 2. Source Inventory

Four HTML pages exist; feature folders `03`–`10` are empty (confirmed). The `01` and `02` folders also contain mobile/tablet PNG screenshots (reference only, not inspected for code).

| Prototype Page | File(s) | Visual Areas Present | Relevance |
|----------------|---------|----------------------|-----------|
| **Layout system** (shared shell) | `00 - layout system/00-layout-system-final-nav-expanded.html` (~1,994 lines) | Design tokens (3 themes), navbar, dropdowns, buttons/states, theme switcher, language toggle, cards, skeletons, search modal, drawer, **footer** | **Primary reference.** This is the canonical chrome + token source. Highest relevance for Phase 1–2. |
| **Homepage / الرئيسية** | `01 - الرئيسية/index-audio-select-bilingual-final-user-menu-complete.html` (~4,661 lines) | Hero, Mushaf preview, gates, tafsir, words, adhkar, hadith, articles, search, developers, user menu, footer | **High.** Real page; shows feature-card, mini-card, stat, section-block, and the full card vocabulary in context. |
| **Mushaf reader (page 5)** | `02 - .../mushaf_page_5_reader_audio_ayah_actions_as_buttons.html` (~11,340 lines) | Reader header, surah index, ayah gates, audio player, Mushaf paper, side panel, doors modal, search modal, footer; `font-palette` word coloring (p1–p6) | **High for Phase 5** (Mushaf-specific). Reuses the same shell/tokens; adds reader-only surfaces. |
| **Ayah cards study** | `02 - .../تفاصيل الصفحة/page-5-ayah-cards-study.html` (~3,008 lines) | Ayah cards, reader panel, audio player, tafsir modal, doors modal, search modal, footer | **Medium.** Companion variant of the reader; relevant to study/ayah card patterns. |
| Feature folders 03–10 | (empty) | none | None. No HTML; do not treat as reference. |
| Page assets | `page5-reader-package/` (`p5.woff2`, `page-5-static-data.json`), `تفاصيل الصفحة/*.json`, mobile/tablet PNGs | fonts, data, responsive screenshots | Low for color/chrome; the woff2 is a page-specific QPC font. |

**Focus pages requested:** homepage ✔, Mushaf reader ✔, surah/ayah ✔ (page-5 variants), shared layout/nav/footer/card system ✔ (the `00` file is the cleanest source for these).

---

## 3. Visual Identity Summary

- **Mood:** scholarly, calm, premium-editorial. A warm parchment "paper" canvas, deep navy structural color, and a restrained gold accent. Reads like a serious Quran research workspace, aligned with the app's PRODUCT.md ("quiet scriptorium"), but more *finished* and *layered* than the current app.
- **Typography:** IBM Plex Sans Arabic for UI (matches the app), Amiri for Quran/verse text (matches the app), IBM Plex Sans for Latin. Tight heading tracking (`letter-spacing: -.01em`), uppercase Latin eyebrows that switch off for Arabic.
- **Spacing:** generous and rhythmic. 24px container padding, 64–80px section rhythm, 22–32px card padding, 1240px max width, 72px nav height. Not cramped, not loose.
- **Surface hierarchy:** a genuine 4-step ladder (`bg`, `surface`, `surface-2`, `surface-3`) where cards are *lighter* than the page and recessed/quiet areas are *warmer/darker*. Depth is real.
- **Light mode feel (ivory):** warm off-white page, near-white lifted cards, soft long shadows, gold accent, navy primary buttons. Premium and airy.
- **Dark mode feel (midnight):** deep blue-black (`#0D1322`) page, blue-tinted surfaces stepping up, gold accent and gold primary, heavier shadows. Cohesive, not just "inverted."
- **Quranic / premium feel:** comes from restraint plus craft details: the logo mark (navy tile + gold glyph + inner border ring), the gradient hairline above the footer, the body's faint radial gradient wash, accent eyebrows with a short rule. Reverence through finish, not ornament.
- **Motion language:** subtle and quick. Two tokens only; hovers lift 1–2px; popovers/modals fade + translate 6–12px; no bounce, no scale-up on cards. Calm.

---

## 4. Typography Extraction

**Prototype fonts:** `--font-ar: 'IBM Plex Sans Arabic'`, `--font-en: 'IBM Plex Sans'`, `--font-quran: 'Amiri', 'IBM Plex Sans Arabic', serif`. Loaded via Google Fonts CDN (weights 300–700 Plex Arabic; 400/500/600/700 Plex; 400/700 Amiri). Body 15.5px / line-height 1.65. Headings 700, tight tracking. Eyebrows 12.5px 600, uppercase (Latin only).

| Area | Prototype Typography | Current Angular Typography | Recommendation |
|------|----------------------|----------------------------|----------------|
| UI font | IBM Plex Sans Arabic (300–700), Plex Sans for Latin | IBM Plex Sans Arabic (400/700 only), self-hosted woff2 | **Keep app's self-hosting** (better than CDN). Consider adding weights **500 + 600** — the prototype leans on 500/600 heavily for nav/cards; the app only ships 400/700. |
| Quran / verse font | Amiri (`--font-quran`) | Amiri (`--qd-font-quran`), plus Uthmanic Hafs for ayah markers | **Already aligned.** No change; the app's Amiri decision matches the prototype. |
| Headings | 700, `letter-spacing: -.01em`, `clamp(22px,2.4vw,30px)` for section titles | Naskh (Amiri) titles 700, fixed sizes, line-height 1.5–1.6 | **Adopt** slight negative tracking on Latin/large headings and a `clamp()`-based fluid section title. Keep Amiri for headings if that is the app's intent (prototype uses sans for section titles). |
| Body | 15.5px / 1.65, `--font-ar` | `.qd-text` 1rem / 1.7 | Close. **Adopt** the ~1.65 body line-height as-is (app's 1.7 is fine); no real gap. |
| Nav / footer treatment | Nav links 14.5px 500; footer headings 12.5–14px 600 with letter-spacing, accent color; footer body 13.5–14px | Nav links via `.qd-btn-ghost` (muted); footer uses `--qd-text-meta` | **Adopt** the weight-500 nav links and the accent-colored, slightly-tracked footer section headings. The prototype's footer typography is a clear upgrade. |
| Line heights | body 1.65, cards 1.6, footer 1.75 | body 1.7, cards 1.8 | No meaningful gap. Keep app values. |
| Weights | 300/400/**500**/**600**/700 in active use | 400/700 only | **Adopt 500 + 600.** This is the single most impactful typography gap: the prototype's hierarchy depends on mid-weights the app does not currently ship. |

**Net:** typography is already ~80% aligned (same families). The gains are (1) ship weights 500/600, (2) tighten large-heading tracking, (3) upgrade footer/nav type treatment. No font *replacement* needed.

---

## 5. Color System Extraction

Prototype defines three themes. Below is **ivory** (the light reference) and **midnight** (the dark reference); **sage** is a green-accented light variant (out of scope for a light/dark app, captured for completeness). OKLCH values are computed for the Angular convention.

### Ivory (light reference)

| Role | Prototype Color/Token | OKLCH (computed) | Usage | Should Adopt? | Notes |
|------|----------------------|------------------|-------|---------------|-------|
| Page bg | `--bg #FCFAF4` | `0.985 0.008 91` | App background (warm off-white) | **Adapt** | Warmer/lighter than app's `--qd-bg` (`0.96`). The *relationship* (page slightly below card) is the lesson. |
| Card surface | `--surface #FFFFFF` | `1.000 0.000 90` | Cards, navbar base, dropdowns | **Adapt — do NOT copy literally** | **Pure white**; violates the app's "no pure white" rule (DESIGN.md). Adopt a near-white tinted equivalent. |
| Recessed surface | `--surface-2 #F6EFE5` | `0.955 0.015 77` | Quiet sections, hovers, soft buttons | **Adopt (as token)** | The app has no equivalent "quiet/recessed warm" tone. Fills a real gap. |
| Deep recessed | `--surface-3 #EFE3D3` | `0.921 0.025 75` | Deeper insets, swatches | Adopt | Bottom of the ladder. |
| Border | `--border rgba(18,38,58,.12)` | (navy @ 12%) | Hairlines, card edges | **Adapt** | Navy-tinted translucent border; reads cooler/cleaner than the app's warm `--qd-border`. |
| Border strong | `--border-strong rgba(18,38,58,.22)` | (navy @ 22%) | Hover edges, ghost buttons | **Adopt the concept** | The app has only one border token; a second "strong" step is needed (audit §5). |
| Text | `--text #1F2937` | `0.31 0.03 260` | Primary text | Adapt | Cooler ink than app's warm `--qd-text`. |
| Muted text | `--text-muted #667085` | `0.53 0.03 270` | Secondary text | Keep app's | App's muted is fine; hue differs (warm vs cool). |
| Primary | `--primary #12263A` (navy) | `0.26 0.05 250` | Primary buttons, logo tile | **Adapt — hue TBD** | Strong structural color the app lacks. Hue is the open question (navy vs teal vs ink). |
| Accent | `--accent #C79D43` (gold) | `0.72 0.12 84` | Active nav, links, eyebrows, icons | **Adopt role, defer hue** | Gold. Conflicts with app's earthy accent and Phase-1 teal. Adopt *that an accent is used this way*, decide the hue. |
| Accent hover | `--accent-hover #B68A30` | `0.66 0.12 83` | Accent hover | Adopt (role) | |
| Accent soft | `--accent-soft #E5C98A` | `0.85 0.09 87` | Selection bg, mini-card hover border | Adopt (role) | |
| Accent tint | `--accent-tint #FAF1DD` | `0.96 0.03 87` | Active nav bg, soft button bg, card-icon bg | **Adopt (role)** | The app's audit explicitly asked for an `accent-soft`/tint layer. |
| Shadow sm | `--shadow-sm` (two-layer, navy-tinted, low alpha) | — | Resting card elevation | **Adopt** | The app has `--qd-shadow: none` (unused). This is the missing resting elevation. |
| Shadow | `--shadow` (6/20px, navy-tinted) | — | Hover elevation | **Adopt** | |
| Shadow lg | `--shadow-lg` (30/70px) | — | Dropdowns, modals, drawers | Adopt | App has `--qd-floating-shadow` (one step); a small ladder is better. |
| Focus ring | `--ring 0 0 0 4px rgba(accent,.22)` | — | `:focus-visible` | **Adopt** | App uses `outline` + `--qd-focus-ring`; a soft ring shadow reads more premium. |
| Danger / success | `#B14848` / `#4E7C66` | — | Status | Keep app's | App already has danger/warning/success. |

### Footer palette (shared across ivory + sage; darker in midnight)

| Role | Token | OKLCH | Notes |
|------|-------|-------|-------|
| Footer bg | `--footer-bg #0F1F33` | `0.24 0.04 255` | Deep navy anchor. |
| Footer bg-2 | `--footer-bg-2 #163149` | `0.30 0.06 248` | Used in radial-gradient glow. |
| Footer text | `--footer-text #E9E4D7` | `0.92 0.02 89` | Warm off-white. |
| Footer muted | `--footer-muted #8C99B0` | `0.68 0.04 262` | Cool muted blue-grey; readable on navy. |
| Footer accent | `--footer-accent #D6B56D` | `0.79 0.10 86` | Gold; section headings + link hover. |
| Footer border | `--footer-border rgba(255,255,255,.08)` | — | Translucent white hairlines inside footer. |

### Midnight (dark reference)

| Role | Token | OKLCH | Notes |
|------|-------|-------|-------|
| Page bg | `--bg #0D1322` | `0.19 0.03 267` | Deep blue-black (not neutral). |
| Surface | `--surface #141C2E` | `0.23 0.04 265` | Steps up from bg. |
| Surface-2/3 | `#1B2538` / `#232E45` | `0.27` / `0.32` | Continued ladder. |
| Border / strong | `#28324A` / `#3A476A` | — | Two-step, visible. |
| Accent / primary | `#D4AF6A` (gold) both | `0.77 0.10 82` | In dark, primary == accent (gold). |
| Shadows | heavier, black-based, higher alpha | — | Correctly re-tuned for dark, not reused from light. |

**Requested focus (shape / motion / light-dir / dark-dir):** all four are extractable and strong. **Do not import `ivory/sage/midnight` as theme names** — the app is light/dark. Map ivory → `:root` (light), midnight → `[data-theme='dark']`, drop/shelve sage.

---

## 6. Navbar Treatment

| Aspect | Prototype | Current Angular | Adopt / Adapt / Avoid |
|--------|-----------|-----------------|------------------------|
| Background | `color-mix(in srgb, var(--surface) 88%, transparent)` — translucent near-white | `var(--qd-surface)` — opaque, same as cards | **Adapt.** Make navbar distinct from cards (audit MAJOR). Translucency optional; opacity-distinct surface is the minimum. |
| Translucency / blur | `backdrop-filter: saturate(160%) blur(14px)` | none | **Adapt (with care).** Premium effect; gate behind support + test perf. Acceptable since it is chrome, not Quran text. |
| Border / shadow | `border-bottom: 1px solid var(--border)`; no resting shadow (blur carries it) | `border-block-end: 1px solid var(--qd-border)`; no shadow | **Adopt + add.** Keep hairline; add a subtle bottom shadow or the translucency so it lifts off the page. |
| Sticky | `position: sticky; top:0; z-index:50` | not sticky (shell scroll) | **Adapt.** Sticky chrome is desirable; verify it does not fight the Mushaf reader's own scroll regions. |
| Active link | `color: var(--accent); background: var(--accent-tint)` | `color: var(--qd-accent); background: var(--qd-surface-elevated)` (≈invisible, ΔL 0.01) | **Adopt.** Active = accent text **on an accent-tint pill**. This directly fixes the audit's "active nav hard to spot" finding. |
| Hover | `background: var(--surface-2); color: var(--accent)` | `.qd-btn-ghost` hover → `--qd-surface` (barely visible) | **Adopt.** Soft tinted hover bg + accent text. |
| Logo treatment | Tile (`--primary` bg, `--accent` glyph, radius 12, inner border ring, `shadow-sm`); title 16px/700 + 11.5px muted subtitle | Plain text brand `المنهج القرآني` (`.qd-page-title`), no mark | **Adapt.** A restrained brand mark (tile + glyph) is a clear upgrade; keep it calm, optional subtitle. |
| Controls | Theme **picker** (3-swatch popover), language toggle, icon buttons, search trigger, user menu | Single light/dark **toggle** (icon), nav links, no search/user menu in chrome | **Adapt selectively.** Keep the app's 2-theme toggle (do not import the 3-swatch picker). Search/user-menu are scope decisions, not this phase. |

---

## 7. Footer Treatment

| Aspect | Prototype | Current Angular | Adopt / Adapt / Avoid |
|--------|-----------|-----------------|------------------------|
| Dark/petrol/navy | Deep navy `--footer-bg #0F1F33`, `color: --footer-text` warm off-white | `background: var(--qd-surface)` — **same as cards**, light | **Adopt.** A dark anchor footer is the headline upgrade and matches the Phase-1 decision (petrol/dark). Re-express navy→chosen hue. |
| Gradients | Two radial glows (navy `bg-2` + 10% gold) layered over `--footer-bg` | none | **Adapt (subtle).** Optional; keep very low-contrast. Avoid if it complicates theming. |
| Border accents | `::before` top hairline = `linear-gradient(90deg, transparent, accent, transparent)` at .55 opacity | top `1px solid --qd-border` | **Adopt.** The gradient top hairline is a cheap, premium "end-cap" cue. |
| Text colors | `--footer-text #E9E4D7` (warm white) | inherits `--qd-text` (dark) on light surface | **Adopt** dedicated footer text tokens (light-on-dark). Required once footer goes dark. |
| Muted text | `--footer-muted #8C99B0` | `--qd-text-meta` (undefined var — falls back) | **Adopt + fix.** The app currently references `var(--qd-text-meta)` which is **not a defined token** (latent bug, see app `footer.component.scss`). A real footer-muted token fixes both. |
| Link hover | `color: var(--footer-accent-hover)` (gold lighten) | n/a (footer has no links) | **Adopt (role).** Soft accent/mint link hover on dark. |
| Logo treatment | `.footer .nav-logo-mark` = `--footer-bg-2` tile + gold glyph + inset highlight shadow | none | Adapt. Mirror the navbar mark in footer colors if a mark is adopted. |
| App/download buttons | `.app-btn` — translucent white bg, footer-border, hover brighten + accent border | none | **Avoid for now** (out of scope; the app has no mobile-app download). Revisit if/when relevant. |

---

## 8. Card System and Hover Motion

All cards: `1px solid var(--border)`, `box-shadow: var(--shadow-sm)` resting, transitions on `border-color / box-shadow / transform` at `--t-fast` (140ms). The hover model is **lift + strengthen border + step shadow**, never scale.

| Card Type | Resting Style | Hover Style | Motion | Should Adopt? |
|-----------|---------------|-------------|--------|---------------|
| `.card` (base) | `surface`, `border`, `radius 14px`, `shadow-sm`, pad 22px | `.card--hover`: `border-strong` + `shadow` + `translateY(-2px)` | `transform/shadow/border` @140ms | **Adopt.** This is the core elevation pattern the app lacks. |
| `.card--quiet` | `surface-2`, transparent border, **no shadow** | (static) | none | **Adopt.** A recessed "quiet" card for low-emphasis groupings; fills an app gap. |
| `.card--bordered` | `surface`, border, no shadow | (static) | none | Adopt (variant). Border-only when shadow is undesired. |
| `.feature-card` | `surface`, `border`, `radius-lg 22px`, `shadow-sm`, pad 32px | `border-strong` + `shadow` (**no translate**) | shadow/border @140ms | **Adopt.** Larger hero/feature card; note it intentionally does *not* lift (heavier element). |
| `.mini-card` | `surface`, `border`, `radius 14px`, pad 18/20 | `border: accent-soft` + `translateY(-1px)` | transform/border @140ms | **Adopt.** Compact list item with a gentler 1px lift + accent-tinted border. |
| Mushaf/study/ayah cards (`02`) | Same base card tokens + reader-specific layout; ayah cards use surface + border + shadow-sm | same hover family | same contract | **Adopt the chrome**, defer Mushaf-specific layout to Phase 5. |
| Dropdown/popover/modal "cards" | `surface`, `border`, `radius`, `shadow-lg` | enter: translateY/scale (see §11) | `--t-base` (220ms) | Adopt for floating layers. |

**Key lesson for the app:** the audit found card hover (`--qd-surface-elevated`, ΔL ≈ 0.01) is invisible. The prototype gets feedback from **shadow + transform + border**, not a background tone change. Adopt that mechanism.

---

## 9. Surface Hierarchy

| Layer | Prototype Treatment | Current Angular Treatment | Recommended Angular Token Direction |
|-------|---------------------|---------------------------|-------------------------------------|
| App / page bg | `--bg` warm off-white `0.985` (ivory) | `--qd-bg` `0.96` | Keep a page bg slightly *below* cards; the relationship matters more than the value. |
| Section bg | `--surface-2` (`is-quiet` sections) | none (sections show page bg) | **Add** a `--qd-section-bg` / quiet-surface token (audit §5, §9). |
| Card bg | `--surface` (lighter than page, lifted by shadow) | `--qd-surface` (≈ page, ΔL 0.025, flat) | **Add real separation** via lighter card + resting shadow, not just tone. |
| Nested / recessed card | `--surface-2` / `--surface-3` | `--qd-surface-elevated` (ΔL 0.01, unusable) | **Re-task** the ladder: introduce distinct recessed tones; reserve "elevated" for genuine lift. |
| Quiet section bg | `--surface-2` | none | As above — `--qd-section-bg`. |
| Footer bg | `--footer-bg` deep navy | `--qd-surface` (same as cards) | **Add** `--qd-footer-bg` dark anchor (Phase 1). |
| Dropdown / modal bg | `--surface` + `shadow-lg` | `--qd-surface` + `--qd-floating-shadow` | Keep; align with the new shadow ladder. |

**Summary:** the prototype runs a **4-tone surface ladder + shadow** where the app runs a **3-tone, near-flat ladder + no shadow**. Closing this is Phase 2 of the implementation plan.

---

## 10. Buttons, Links, Active and Selected States

| State/Control | Prototype Treatment | Recommendation |
|---------------|---------------------|----------------|
| Primary button | `.btn--primary`: `--primary` (navy) bg, `--primary-fg` text; hover → `--accent-hover`; `:active translateY(1px)` | **Adopt the structure.** App's `.qd-btn-primary` uses `--qd-accent` bg with two **hardcoded** literals (audit §4) — replace with `--primary`/`--on-accent`/`--accent-hover` tokens. Decide whether primary is navy/teal/accent. |
| Ghost button | `.btn--ghost`: transparent, `border-strong`; hover → `surface-2` bg + accent border + accent text | **Adopt.** App ghost is borderless muted text (invisible at rest). A `border-strong` outline + accent hover is stronger. |
| Soft button | `.btn--soft`: `accent-tint` bg + accent text; hover → `accent-soft` | **Adopt.** The app has no "soft/tonal" button; useful for secondary actions and the active-nav style. |
| Active nav item | `accent` text on `accent-tint` pill | **Adopt** (also §6). Fixes the audit's weak active state. |
| Selected/active cards | (reader) selection via accent border/tint, not heavy fill | Adopt: selection = accent border + soft tint, never a saturated fill (calm). |
| Hover / focus | hover = tonal bg + accent; focus = `box-shadow: var(--ring)` soft halo | **Adopt** the soft focus ring; keep an accessible outline fallback. |
| Badges / chips | accent-tint / surface-2 backgrounds with accent or muted text, pill radius | **Adopt.** App badges reuse `--qd-surface` (== card, invisible, audit §3). Give chips a tint distinct from the card. |

---

## 11. Motion System

Extracted rules (consistent across all pages):

- **Duration tokens:** `--t-fast: 140ms ease` (hovers, color/border/bg, small transforms); `--t-base: 220ms cubic-bezier(.2,.7,.3,1)` (popovers, modals, theme/bg transitions). Two tokens only.
- **Transform distance:** card hover `translateY(-2px)`; mini-card `-1px`; button `:active translateY(1px)`; dropdown/popover enter from `translateY(-6px)`; modal from `translateY(-12px) scale(.98)`; drawer `translateX(±100%)` (RTL-aware).
- **Shadow change on hover:** `shadow-sm → shadow` (cards); floating layers use `shadow-lg`.
- **Scale:** essentially unused on content (only theme-swatch buttons `scale(1.05)`, modal panel `.98 → 1`). **No card scale-up.**
- **Dropdown motion:** opacity + translateY(-6→0) + visibility, `--t-base`.
- **Modal motion:** scrim fade; panel `translateY(-12px) scale(.98) → none`, `--t-base`.
- **Card hover movement:** lift 1–2px + shadow step, `--t-fast`. Calm, quick, no bounce.

**Recommended safe motion contract for Angular:**
- Adopt **two duration tokens** (`--qd-t-fast ≈ 140ms ease`, `--qd-t-base ≈ 220ms cubic-bezier(.2,.7,.3,1)`). The app currently hardcodes `0.15s ease` per component; tokenize it.
- **Subtle only:** hover lifts ≤ 2px; floating layers translate ≤ 12px; no scale on content; **no bounce/elastic** (matches impeccable laws and PRODUCT.md "calm").
- **Respect the calm reading experience:** never animate Quran/Mushaf text, ayah glyphs, or word-segments. Motion is for chrome and cards only.
- **Honor `prefers-reduced-motion`** (the app already does for skeletons/buttons; extend to new transitions).
- Animate `transform`/`opacity`/`box-shadow`/`color` only — **never layout properties** (matches impeccable laws).

---

## 12. What To Adopt

**Typography**
- Ship IBM Plex Sans Arabic weights **500 and 600** (the prototype's hierarchy depends on them).
- Slight negative tracking on large/Latin headings; `clamp()` fluid section titles.
- Upgrade footer/nav type: weight-500 nav links; accent-colored, lightly-tracked footer section headings.

**Colors (roles/relationships, not literal hues)**
- A 4-step **surface ladder** (`page → card → recessed → deep-recessed`).
- **Two-step borders** (`border` + `border-strong`).
- **Accent layering**: `accent / accent-hover / accent-soft / accent-tint`, plus a distinct **primary** structural color and an **on-primary/on-accent** text token.
- A dedicated **footer palette** (dark bg, warm off-white text, muted text, accent links, translucent footer border).

**Navbar**
- Distinct-from-cards surface (translucent or separate tone) + backdrop blur (optional, tested) + hairline border + subtle lift.
- Active = accent text on accent-tint pill; hover = soft tonal bg + accent text.
- Optional restrained brand mark (tile + glyph + inner ring).

**Footer**
- Dark navy/petrol anchor, gradient top hairline, dedicated text/muted/link tokens, accent section headings.

**Cards**
- Resting `shadow-sm` + 1px border; hover = `translateY(-2px)` + `shadow` + `border-strong`.
- Variants: `--hover`, `--quiet` (recessed, no shadow), `--bordered`; feature-card (larger, lifts via shadow only); mini-card (1px lift + accent-soft border).

**Shadows**
- A small **elevation ladder** (`shadow-sm` resting, `shadow` hover, `shadow-lg` floating) + a soft **focus ring**. Replace the app's unused `--qd-shadow: none`.

**Motion**
- Two duration tokens; subtle transforms; no bounce; reduced-motion honored; never on Quran text.

**Buttons / active states**
- `primary` (structural), `ghost` (outlined, accent hover), `soft` (tonal accent); soft focus ring; accent-tint active/selected states; chips distinct from cards.

---

## 13. What Not To Adopt

- **Theme names `ivory / sage / midnight`.** The app is `light` + `dark`. Map ivory → light, midnight → dark, shelve sage. (This is exactly the naming the original color audit was warned about.)
- **Literal navy + gold hues — pending a hue decision.** Do not silently introduce gold `#C79D43` / navy `#12263A` as the app accent/primary while a teal/petrol direction (Phase 1) and an earthy direction (DESIGN.md) are also on the table. Lock one family first.
- **Pure `#FFFFFF` surfaces** (ivory `--surface`). Violates the app's "no pure white, tint every neutral" rule. Use a tinted near-white.
- **Google Fonts CDN loading.** The app self-hosts woff2 (better for offline/perf/privacy). Keep self-hosting.
- **Inline `<style>` blocks / single-file pages.** Prototype-only. The app uses SCSS partials + component styles; keep that architecture.
- **`font-palette` tajweed word coloring (p1–p6).** This is COLRv1 font-palette glyph coloring for tajweed, unrelated to the app's `SEGMENT_COLOR_PALETTE` (morphological segment *linking*). Do not conflate; do not import. (If tajweed coloring is ever wanted, it is a separate feature decision.)
- **Fixed-attachment body radial-gradient washes.** Decorative; risk of banding and perf cost; conflicts with "flat/calm." Skip or make near-invisible.
- **The 3-swatch theme picker popover.** Keep the app's simpler light/dark toggle.
- **Footer app-download buttons**, hero/marketing-specific blocks, and any page-specific one-offs not part of the shared shell.
- **Hardcoded colors generally.** Everything adopted should become an OKLCH token, per the app's convention and the audit's findings.

---

## 14. Documentation Update Plan

Do not edit yet. Proposed future updates:

| Document | Future Update Needed | Reason |
|----------|----------------------|--------|
| **Impeccable `PRODUCT.md`** | Minor: reconcile brand name (`المنهج القرآني` vs prototype `الباحث القرآني`); confirm register stays "product". | Avoid drift; the prototype uses a different product name. No strategy change implied. |
| **Impeccable `DESIGN.md`** | Resolve the **accent hue** (gold vs teal/petrol vs earthy) and elevation stance (currently "flat by default; depth via tonal layering, not shadows"). The prototype and Phase-1 decision both rely on **shadows** + a wider surface ladder. | DESIGN.md currently forbids ambient shadows; the adopted direction needs a deliberate, documented elevation ladder. This is a real conflict to settle before implementation. |
| **`Frontend/quran-dashboard-ui` UI style file / `UI_STYLE_SYSTEM.md`** | Add the new semantic token contract: surface ladder (`page/section/card/recessed`), `border` + `border-strong`, accent layers, primary + on-* tokens, footer tokens, shadow ladder, motion duration tokens. Map ivory→light, midnight→dark in OKLCH. Document "no pure white", "no theme name proliferation", and the motion contract. | This becomes the source of truth the Phase 1–5 implementation reads from. Tokens should be defined for **both** light and dark. |

---

## 15. Implementation Phasing Recommendation

After documentation is approved, the safest order (matches the requested phasing and the audit's risk profile):

- **Phase 1 — Navbar + Footer chrome.** Lowest blast radius. Introduce chrome + footer tokens (both themes), make navbar distinct/elevated, make footer a dark anchor, fix the active-nav state and the undefined `--qd-text-meta` footer reference. No content/card changes. (This is the phase that was already in flight before this report.)
- **Phase 2 — Global tokens / light-dark surface hierarchy.** Introduce the 4-step surface ladder, `border-strong`, accent layers, shadow ladder, and motion duration tokens in `_tokens.scss` / `_themes.scss`. Re-map existing usages carefully (keep `--qd-bg/surface/border/accent` working — additive first, migrate second).
- **Phase 3 — Card hover/elevation system.** Apply resting `shadow-sm` + hover `translateY(-2px)` + `shadow` + `border-strong` to `.qd-card` and add `--quiet` / feature / mini variants.
- **Phase 4 — Buttons / active / selected states.** Tokenize `.qd-btn-primary` (remove hardcoded literals), add `ghost`/`soft` behavior, soft focus ring, accent-tint active/selected, chip contrast.
- **Phase 5 — Mushaf / study page-specific polish.** Apply the new surfaces/elevation to reader, ayah/study cards, side panels, and reader chrome. Verify Quran text and word-segment coloring are untouched by motion.

---

## 16. Risks

- **Copying prototype CSS directly into Angular.** The prototype is single-file, inline-styled, hardcoded-hex, CDN-fonts, 3-theme. Direct paste would break the app's SCSS-partial + OKLCH-token + light/dark architecture. Adopt *rules*, re-author as tokens.
- **Adding too many theme names.** Importing ivory/sage/midnight would fragment a clean light/dark system. Map to two themes only.
- **Hardcoded colors.** The prototype is full of literal hex/rgba (including pure white and inline rgba shadows). Re-express as OKLCH tokens; do not introduce new hardcoded values (the audit already flagged the two existing ones).
- **Breaking dark mode.** The prototype dark (midnight) re-tunes shadows and surfaces deliberately. Any adopted token **must** be defined for both themes; reusing light-mode shadows/literals in dark is the most likely regression (the app's existing `.qd-btn-primary` literals already have this latent bug).
- **Changing layout while only intending visual polish.** The prototype has different nav height (72px vs app 56px), container width (1240px), and sticky behavior. Treat layout changes as deliberate, scoped decisions, not side effects of color work.
- **Overusing motion in reading areas.** The prototype animates chrome only. Any lift/transform/transition must stay off Quran text, ayah glyphs, and word-segments, and honor `prefers-reduced-motion`.
- **Unresolved hue tension.** Implementing colors before DESIGN.md locks gold vs teal vs earthy risks a fourth redo. Documentation (Phase 0) must precede Phase 1 color values.
- **Backdrop-filter performance/support.** The translucent navbar blur is premium but costs GPU and lacks universal support; gate and test, with an opaque fallback.

---

## 17. Final Verdict

**USE WITH ADAPTATION.**

`/projects/Real Pages` is a strong, internally consistent reference whose **structure** is precisely the remedy for the current app's flatness: a real surface ladder, an elevation/shadow ladder, two-step borders, an elevated translucent navbar, a dark anchor footer, layered accent semantics, and a calm, disciplined motion contract. The app should **adopt these roles, relationships, and motion** and re-author them as OKLCH tokens for its existing **light + dark** system. It should **not** copy the prototype's three theme names, its specific navy + gold hues (pending a single locked accent direction), its pure-white surfaces, its inline/CDN/hardcoded mechanics, or its tajweed `font-palette` coloring. Adopt the system; decide the palette; keep the architecture.

---

### Verification (report-only)

- **No application code modified.** `git status` in `Frontend/quran-dashboard-ui` shows only the untracked `report/` directory (this report + the prior color audit); no source files changed.
- **No Product/Design/UI_STYLE_SYSTEM docs modified.** None were touched.
- **`/projects/Real Pages` untouched.** `git status` there shows only its own pre-existing untracked status-report files; this audit read the files read-only.
- **No commit made.**

```
# Frontend/quran-dashboard-ui
?? report/            # untracked (reports only)
branch: main

# /projects/Real Pages
?? REAL_PAGES_STATUS_REPORT.md     # pre-existing, not created by this task
?? real-pages-status-report.json   # pre-existing, not created by this task
branch: main
```
