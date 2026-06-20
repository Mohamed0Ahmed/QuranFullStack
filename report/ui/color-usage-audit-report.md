# Color Usage Audit — Quran Dashboard / المنهج القرآني (Frontend)

**Type:** Read-only audit. No source files changed, no formatting run, no colors implemented.
**Scope:** Frontend only (`Frontend/quran-dashboard-ui`).
**Date:** 2026-06-20
**Reviewer:** automated UI color audit

> Method: read the central token/theme system (`src/styles/`), the theme service
> (`src/app/core/theme/theme.service.ts`), all component SCSS, all templates, and
> Tailwind/PostCSS config. Token values are stated as authored (OKLCH). Lightness
> deltas (ΔL) are computed on the OKLCH L axis (0–1), which is roughly perceptual,
> to quantify how far apart two surfaces actually sit.

---

## 1. Executive Summary

**Verdict: the flatness is real and it is mostly a token-value problem, not a token-usage or hardcoded-color problem.** The theme system itself is healthy: tokens are semantically named, almost everything consumes them, theme switching is implemented cleanly, and there are essentially no rogue hardcoded colors on the main surfaces.

The page reads as "one sheet of paper" because of four compounding causes, in order of impact:

1. **Surfaces are nearly the same lightness.** In the default (light) theme the page background `--qd-bg` (L 0.96), the card/navbar/footer surface `--qd-surface` (L 0.985), and the "elevated" surface `--qd-surface-elevated` (L 0.995) span only **0.035 of lightness end-to-end**. Card vs page is ΔL ≈ 0.025; elevated vs base is ΔL ≈ 0.010 (effectively invisible). Everything floats at the same tonal level.

2. **There is no resting elevation.** `--qd-shadow` is defined as `none` and is **never applied anywhere**. The only shadow token in use, `--qd-floating-shadow`, is reserved for transient pop-ups (dropdown, surah picker, source selector). So cards, the navbar, and the footer get zero ambient depth. All separation is carried by a single 1px hairline border.

3. **One surface token does too many jobs.** `--qd-surface` is the fill for the navbar, the footer, every dashboard card, badges, inputs, and dropdown panels. With no `page` / `section` / `card` / `chrome` distinction, structurally different regions are literally the same color, so the eye gets no hierarchy.

4. **The accent never appears where you look.** `--qd-accent` is low-chroma (chroma 0.08) and on the landing page it is used only as the active-nav-link text color. The five dashboard cards, the page header, and all primary affordances render with no accent at all, so the first screen is parchment-on-parchment with muted ink and nothing to anchor the eye.

**Where the issue lives:** ~80% token **values** (surfaces too close, no elevation, accent too weak/underused), ~20% a missing **semantic layer** (a single `--qd-surface` instead of distinct page / section / card / elevated tokens, and a single `--qd-border` instead of subtle/strong). It is **not** a hardcoded-color problem and **not** a broken-theme-system problem.

**Note on theme names:** the brief expected `ivory / sage / midnight`. Those do not exist. The app ships exactly **two** themes: `light` (the `:root` default) and `dark` (`[data-theme='dark']`). The dark theme is actually better separated than light (see §6), so the flatness complaint is strongest in the default light theme.

---

## 2. Theme Inventory

There are two themes. `light` is the implicit default declared on `:root` in `src/styles/_tokens.scss`. `dark` overrides a subset of tokens in `src/styles/_themes.scss`. Tokens not listed under `dark` inherit their `:root` value.

### Light (default) — `:root`

| Theme | Token | Value | Role / Meaning | Defined In |
|-------|-------|-------|----------------|------------|
| light | `--qd-bg` | `oklch(0.96 0.008 75)` | App / page background (parchment) | `_tokens.scss` |
| light | `--qd-surface` | `oklch(0.985 0.006 75)` | Card / navbar / footer / input / badge fill | `_tokens.scss` |
| light | `--qd-surface-elevated` | `oklch(0.995 0.005 75)` | Hover / "elevated" / coverage strip fill | `_tokens.scss` |
| light | `--qd-text` | `oklch(0.18 0.015 60)` | Primary text / high-emphasis ink | `_tokens.scss` |
| light | `--qd-text-muted` | `oklch(0.44 0.012 60)` | Secondary text, labels, placeholders | `_tokens.scss` |
| light | `--qd-border` | `oklch(0.82 0.01 75)` | Hairline borders / dividers (the only one) | `_tokens.scss` |
| light | `--qd-accent` | `oklch(0.45 0.08 55)` | Primary action, selection, active state | `_tokens.scss` |
| light | `--qd-danger` | `oklch(0.55 0.15 25)` | Error text / unhealthy status | `_tokens.scss` |
| light | `--qd-warning` | `oklch(0.70 0.14 75)` | Degraded status | `_tokens.scss` |
| light | `--qd-success` | `oklch(0.55 0.12 150)` | Healthy status | `_tokens.scss` |
| light | `--qd-focus-ring` | `oklch(0.50 0.10 55)` | Focus outline | `_tokens.scss` |
| light | `--qd-shadow` | `none` | Resting elevation (DEFINED, NEVER USED) | `_tokens.scss` |
| light | `--qd-floating-shadow` | `0 0.25rem 0.75rem oklch(0.18 0.015 60 / 0.08)` | Pop-up/menu drop shadow | `_tokens.scss` |
| light | `--qd-overlay` | `oklch(0.18 0.015 60 / 0.30)` | Modal/mobile-menu scrim | `_tokens.scss` |
| light | `--qd-mushaf-word-selection-indicator` | `var(--qd-accent)` | Selected-word ring (alias of accent) | `_tokens.scss` |
| light | `--qd-mushaf-ayah-marker-color` | `var(--qd-text-muted)` | Ayah-end marker color | `_tokens.scss` |

(Plus non-color tokens in the same `:root` block: font-family tokens `--qd-font-*`, radius `--qd-radius-sm/md/lg`, spacing `--qd-space-1..6`, and mushaf layout sizing tokens. Listed here for completeness; not color.)

### Dark — `[data-theme='dark']`

| Theme | Token | Value | Role / Meaning | Defined In |
|-------|-------|-------|----------------|------------|
| dark | `--qd-bg` | `oklch(0.16 0.012 60)` | App / page background (dark ink) | `_themes.scss` |
| dark | `--qd-surface` | `oklch(0.20 0.012 60)` | Card / navbar / footer / input / badge fill | `_themes.scss` |
| dark | `--qd-surface-elevated` | `oklch(0.25 0.012 60)` | Hover / elevated / coverage strip fill | `_themes.scss` |
| dark | `--qd-text` | `oklch(0.90 0.008 75)` | Primary text | `_themes.scss` |
| dark | `--qd-text-muted` | `oklch(0.62 0.010 60)` | Secondary text | `_themes.scss` |
| dark | `--qd-border` | `oklch(0.30 0.010 60)` | Hairline border | `_themes.scss` |
| dark | `--qd-accent` | `oklch(0.60 0.08 55)` | Primary action / selection / active | `_themes.scss` |
| dark | `--qd-danger` | `oklch(0.65 0.15 25)` | Error / unhealthy | `_themes.scss` |
| dark | `--qd-warning` | `oklch(0.75 0.14 75)` | Degraded | `_themes.scss` |
| dark | `--qd-success` | `oklch(0.65 0.12 150)` | Healthy | `_themes.scss` |
| dark | `--qd-focus-ring` | `oklch(0.65 0.10 55)` | Focus outline | `_themes.scss` |
| dark | `--qd-floating-shadow` | `0 0.25rem 0.75rem oklch(0.08 0.008 60 / 0.35)` | Pop-up drop shadow | `_themes.scss` |
| dark | `--qd-overlay` | `oklch(0.08 0.008 60 / 0.62)` | Modal/mobile-menu scrim | `_themes.scss` |
| dark | `--qd-mushaf-word-selection-indicator` | `var(--qd-accent)` | Selected-word ring | `_themes.scss` |
| dark | `--qd-mushaf-ayah-marker-color` | `oklch(0.98 0.006 75)` | Ayah-end marker (brighter than light) | `_themes.scss` |

**Not overridden in dark (inherits `:root`):** `--qd-shadow` (`none`, harmless) **and the hardcoded button literals in `_components.scss`** (see §4 and §8 — these do not theme).

---

## 3. Color Usage Map

Grouped by UI role. "Token / Color Used" names the resolved token. Files cite where the rule lives.

| UI Role | Token / Color Used | Files | Notes |
|---------|--------------------|-------|-------|
| App background | `--qd-bg` | `styles.scss` (body), `_layout.scss` (`.qd-shell`) | L 0.96 light / 0.16 dark. |
| Page background | `--qd-bg` (inherited) | `mushaf-reader-page.component.scss` uses `background: transparent` | Pages don't introduce their own bg; they show through to `--qd-bg`. |
| Section background | (none / `--qd-bg`) | mushaf reader page is `transparent` | **No section-level surface token exists.** Sections sit directly on page bg. |
| Cards | `--qd-surface` + 1px `--qd-border` | `_components.scss` (`.qd-card`), `dashboard-home.component.scss` | ΔL vs page ≈ 0.025 (light). Hairline does all the lifting. |
| Elevated cards / hover | `--qd-surface-elevated` | `_components.scss`, `dashboard-home`, `top-navbar`, `_study-card.shared.scss` (`__coverage`) | ΔL vs `--qd-surface` ≈ 0.010 (light). Near-invisible step. |
| Navbar | `--qd-surface` + `border-block-end: 1px --qd-border` | `_layout.scss` (`.qd-navbar`), `top-navbar.component.scss` | **Same fill as cards and footer.** Only the bottom hairline separates it from the page. |
| Footer | `--qd-surface` + `border-block-start: 1px --qd-border` | `_layout.scss` (`.qd-footer`), `footer.component.scss` | Same fill as navbar and cards. |
| Borders | `--qd-border` (single token) | everywhere (30 uses) | One value for dividers, card edges, inputs, badges. No subtle/strong split. |
| Shadows (resting) | `--qd-shadow: none` | defined in `_tokens.scss`, **0 usages** | No ambient elevation anywhere. |
| Shadows (floating) | `--qd-floating-shadow` | `top-navbar` dropdown, `source-selector`, `surah-jump-picker` | Only on transient pop-ups. |
| Primary buttons | `--qd-accent` bg, text `oklch(0.98 0.005 75)`, hover `oklch(0.40 0.09 55)` | `_components.scss` (`.qd-btn-primary`) | Accent fill is the one strong color, but it is **absent from the landing page**. Text + hover are hardcoded (see §4). |
| Secondary buttons | transparent bg, `--qd-text`, `--qd-border` | `_components.scss` (`.qd-btn-secondary`) | Reads as an outlined chip; low presence. |
| Ghost buttons | transparent, `--qd-text-muted`, hover `--qd-surface` | `_components.scss` (`.qd-btn-ghost`) | All navbar links + theme toggle + retry use this. Very low contrast at rest. |
| Links / nav | `.qd-btn-ghost`; active → `--qd-accent` text + `--qd-surface-elevated` bg | `top-navbar.component.scss` (`.nav-link`, `.dropdown-link`, `.mobile-link`) | Active state is the only place accent shows on the shell, and only as text color. |
| Chips / badges | `--qd-surface` bg, `--qd-text-muted` text, `--qd-border` | `_components.scss` (`.qd-badge`) | **Badge fill == card fill**, so badges on a card have no container contrast; only the hairline reads. |
| Form controls (input) | `--qd-surface` bg, `--qd-border`, focus `--qd-accent` border + `--qd-focus-ring` | `_forms.scss` (`.qd-input`) | Same fill as the card they sit in. |
| Form controls (select) | `--qd-surface` bg, border `mix(--qd-border 88%, --qd-accent)`, hover `--qd-surface-elevated`, arrow `--qd-text-muted` | `_forms.scss` (`.qd-select`) | The one control that tints its border toward accent; the most "designed" surface in the system. |
| Modals / drawers | `--qd-overlay` scrim + `--qd-surface` panel | `top-navbar.component.scss` (`.mobile-menu`, `.mobile-menu-panel`) | Panel fill again == card fill. |
| Status indicators | `--qd-success` / `--qd-warning` / `--qd-danger` | `footer.component.scss` (health dots), `_components.scss` (`.qd-error-state`) | The only saturated UI colors on chrome, but tiny (8px dots) and footer-only. |
| Selection / highlight (mushaf) | ring via `--qd-accent` / `--qd-border` mixes | `mushaf-word`, `selected-ayah-section`, `source-selector`, `surah-jump-picker` | Selection shown as `inset 0 0 0 1px color-mix(...)` rings, not fills. Subtle by design. |
| Segment word coloring (mushaf) | 6 hardcoded hex (see §4) | `segment-rendered-word.component.scss`, `segment-color-palette.ts` | The only multi-hue palette in the app; confined to word-analysis highlights, absent from the dashboard. |

---

## 4. Hardcoded Colors

Colors authored as raw literals outside the `--qd-*` token contract. Token **definitions** in `_tokens.scss` / `_themes.scss` are excluded (raw OKLCH is expected at the definition site). What remains is genuine bypass.

| Color | File | Selector / Component | Usage | Should become token? |
|-------|------|----------------------|-------|----------------------|
| `oklch(0.98 0.005 75)` | `styles/_components.scss:42` | `.qd-btn-primary` `color` | Primary-button label color (near-white on accent) | **Yes** — should be `--qd-on-accent` (a token), so it themes and is reusable. |
| `oklch(0.40 0.09 55)` | `styles/_components.scss:46` | `.qd-btn-primary:hover` `background` | Darker-accent hover fill | **Yes** — should be `--qd-accent-strong` / derived from `--qd-accent`. It does **not** change between light and dark today. |
| `#3d6b8e` (slot 1, slate-blue) | `segment-rendered-word.component.scss:3` + `segment-color-palette.ts` | `::highlight(qd-segment-slot-1)` / `SEGMENT_COLOR_PALETTE` | Word-segment linking color | Partially — these are a deliberate categorical palette, but they are **duplicated in two files** (SCSS map + TS array) and **do not adapt to dark theme**. Should be single-sourced as tokens. |
| `#9a6b3c` (slot 2, brown) | same | slot 2 | same | same |
| `#3a7d56` (slot 3, green) | same | slot 3 | same | same |
| `#7d4a6b` (slot 4, mauve) | same | slot 4 | same | same |
| `#5a5a9e` (slot 5, indigo) | same | slot 5 | same | same |
| `#8b6914` (slot 6, gold) | same | slot 6 | same | same |

**Findings:**
- **No hardcoded colors on the main surfaces.** Navbar, footer, cards, page shells, inputs, badges, and the dashboard are 100% token-driven. This is genuinely good and is why this is a value problem, not a hygiene problem.
- The two `.qd-btn-primary` literals are the only token-bypass on shared chrome. The hover literal is a latent dark-theme bug: in dark mode the primary button hover stays a light-theme value.
- The 6 segment hexes are duplicated across `segment-rendered-word.component.scss` and `state/segment-color-palette.ts` (a DRY violation the code comment acknowledges: "Colors mirror SEGMENT_COLOR_PALETTE"). They are also static across themes.
- No `rgb()/rgba()/hsl()`, no named CSS colors, **no Tailwind color utilities** (`bg-*`, `text-*`, arbitrary `[#...]`) anywhere in templates, and **no inline `style=` colors**. Tailwind's `theme.extend` is empty; Tailwind contributes nothing to the palette.

---

## 5. Repeated / Overlapping Colors

Tokens/values sitting too close to be read as distinct. ΔL is OKLCH lightness distance (light theme unless noted). Directions only, no replacement values.

| Pair | Values | Problem | Suggested Direction |
|------|--------|---------|---------------------|
| Page bg ↔ card surface | `--qd-bg` L 0.96 ↔ `--qd-surface` L 0.985 (ΔL ≈ 0.025) | Cards barely separate from the page; the whole grid reads as one sheet. | Needs **stronger tonal separation** between page and card (push card lighter or page warmer/darker, or add real elevation). This is the single biggest cause. |
| Card surface ↔ elevated surface | `--qd-surface` L 0.985 ↔ `--qd-surface-elevated` L 0.995 (ΔL ≈ 0.010) | Hover and "elevated"/coverage strips are perceptually identical to the base card. The elevation step does nothing. | Needs a **clearly perceptible** elevated step; current 1% is below the noticeable threshold. |
| Navbar/footer ↔ cards | all `--qd-surface` (identical) | Chrome (navbar, footer) and content (cards) are the same fill; only hairlines distinguish structurally different regions. | Chrome should read as a **distinct layer** from content (separate token, tone, or elevation). |
| Badge fill ↔ card fill | both `--qd-surface` | A badge placed on a card has no container contrast; the "chip" only exists because of its 1px border. | Badge surface should be **distinct from the card it sits on** (a muted/tinted fill). |
| Input/select/panel fill ↔ card fill | all `--qd-surface` | Form fields and dropdown panels are the same color as their container card; field boundaries rely entirely on the hairline. | Controls need a **subtly different field tone** from their card. |
| Border ↔ surfaces (single token) | `--qd-border` L 0.82 used for both faint dividers and structural card edges | One border value cannot be both an invisible internal divider and a confident card outline; today it is asked to do all separation alone. | Split into **subtle vs strong** borders; structural edges should be **more visible**. |
| Accent ↔ neutral ink | `--qd-accent` L 0.45 / chroma 0.08 vs `--qd-text` L 0.18 / `--qd-text-muted` L 0.44 | Accent is so low-chroma and mid-lightness it reads as just another muted brown-ink, not as "action/selection." Active nav text barely differs from muted text. | Accent needs **more chroma / more distinctness** so action and selection states stand apart from plain ink. |

---

## 6. Contrast and Hierarchy Findings

No full WCAG computation; obvious risks flagged. (OKLCH L is roughly perceptual lightness, used here as a proxy.)

**Text legibility — generally good:**
- `--qd-text` (L 0.18) on `--qd-surface` (L 0.985) / `--qd-bg` (L 0.96): very high contrast, comfortable. Dark theme `--qd-text` (L 0.90) on `--qd-bg` (L 0.16): also strong. **No reading risk.**
- `--qd-text-muted` light (L 0.44) on surface (L 0.985): comfortable; likely clears AA for body. Dark muted (L 0.62) on dark surface (L 0.20): also fine.

**Low-contrast / weak-hierarchy risks:**
- **Ghost buttons at rest** (`--qd-text-muted` on transparent over `--qd-surface`): all navbar links, the theme toggle, and the retry button sit in muted ink with no border and no fill. They are legible but read as low-priority text, not as controls. On the landing page this means **no element announces itself as interactive** until hover.
- **Active nav link vs inactive:** active = `--qd-accent` text (L 0.45) + `--qd-surface-elevated` bg; inactive = `--qd-text-muted` (L 0.44). The two text colors are almost the same lightness and the elevated bg is ~1% off the navbar fill, so the **selected nav item is hard to spot** at a glance.
- **Borders against cards:** `--qd-border` (L 0.82) vs `--qd-surface` (L 0.985) is ΔL ≈ 0.165, visible but doing 100% of the structural work with a single 1px line. Remove or lighten it and cards disappear entirely (because the fill ΔL is only 0.025).
- **Navbar / footer separation from page:** carried by one hairline border each, with identical fill to the cards in the page body. The shell does not feel "framed."
- **Badge container contrast:** badge fill == card fill, so on `.qd-card` the badges only register via their hairline; the container affordance is weak.

**Adequate contrast (not flagged):**
- Primary button: `oklch(0.98)` text on `--qd-accent` (L 0.45) — strong, fine (but the button is absent from the landing page).
- Focus ring `--qd-focus-ring` and status colors (`success/warning/danger`) are saturated enough to read.

**Theme comparison (important):** the dark theme separates surfaces *better* than light. Light steps are 0.96 → 0.985 → 0.995 (ΔL 0.025 / 0.010). Dark steps are 0.16 → 0.20 → 0.25 (ΔL 0.040 / 0.050). So the flatness is **worst in the default light theme**, and any fix should prioritize the light ramp.

**Dark-theme segment colors:** the 6 segment hexes are tuned for light parchment. On dark bg (L 0.16) the darker members (`#3d6b8e`, `#5a5a9e`, `#8b6914`) get closer to the background and lose contrast. Flagged as a NOTE (word-analysis feature, not the dashboard).

---

## 7. Component-Level Findings

| Component / Area | Current color behavior | Problem | Severity |
|------------------|------------------------|---------|----------|
| **App shell** (`app-shell`, `_layout.scss`) | `.qd-shell` = `--qd-bg`; main content shows through to bg; pages are `transparent`. | No section/region layering token; the shell is a single flat tone with content floating on it. | MAJOR |
| **Navbar** (`top-navbar`, `.qd-navbar`) | `--qd-surface` fill + 1px bottom `--qd-border`. Links are ghost (muted). | Same fill as the cards below it; only a hairline separates chrome from content; links read as plain muted text. | MAJOR |
| **Footer** (`footer`) | `--qd-surface` fill + 1px top `--qd-border`; health dots use status colors. | Same fill as navbar and cards; the only color is three 8px status dots. Footer barely registers as a distinct band. | MINOR |
| **Dashboard / home** (`dashboard-home`) | Header (title + muted subtitle + neutral badges) over a grid of `.qd-card` (`--qd-surface`) on `--qd-bg`. No accent anywhere. | This is the first screen and it is entirely parchment-on-parchment with muted ink: cards ΔL 0.025 from page, no shadow, no accent. **This is where users feel the flatness most.** | MAJOR |
| **Cards** (`.qd-card`, `.dashboard-card`) | `--qd-surface` + 1px border; hover → `--qd-surface-elevated` (ΔL 0.010). | Weak lift at rest, near-zero hover feedback. The "elevated" hover is imperceptible. | MAJOR |
| **Study cards** (`_study-card.shared.scss`) | `__coverage` strip uses `--qd-surface-elevated` on a card that is `--qd-surface`. | Elevated-vs-base ΔL 0.010 means the coverage strip and the card body look like one block. | MINOR |
| **Buttons** (`_components.scss`) | Primary = accent fill (distinct); secondary = outline; ghost = muted text. | The one strong affordance (primary) never appears on the shell/landing; everything visible is ghost/outline, so no action color is present. | MAJOR |
| **Badges** (`.qd-badge`) | `--qd-surface` fill + border + muted text. | Fill equals the card fill, so badges have no container contrast on cards. | MINOR |
| **Inputs / selects** (`_forms.scss`) | Input = `--qd-surface`; select tints its border toward accent and is the most distinct control. | Field fill equals card fill; boundaries depend on the hairline. Select is the positive exception. | MINOR |
| **Dropdown / pop-ups** (`top-navbar` menu, `source-selector`, `surah-jump-picker`) | `--qd-surface` panel + `--qd-floating-shadow` + border. | These are the **only** elevated surfaces with a real shadow, which is correct, but it makes the lack of any resting elevation elsewhere more obvious by contrast. | NOTE |
| **Mobile menu / overlay** | `--qd-overlay` scrim + `--qd-surface` panel. | Panel fill equals card fill; works, but no layer distinction. | NOTE |
| **Mushaf segment words** (`segment-rendered-word`) | 6 hardcoded hex via `::highlight()`. | Only multi-hue color in the app; duplicated across SCSS + TS; static across themes. Isolated from the dashboard. | NOTE |
| **Status / health** (`footer`, `qd-error-state`) | `success/warning/danger` tokens. | Fine and readable; correctly not relied on by color alone elsewhere. | NOTE |

No BLOCKER: nothing is unreadable. The cluster of MAJORs around the shell, dashboard, cards, and buttons is the flatness.

---

## 8. Theme System Health

- **Are theme names consistent?** Yes, but they are **not** `ivory/sage/midnight`. There are two: `light` (default `:root`) and `dark`. The naming is consistent across the service type (`Theme = 'light' | 'dark'`), the `data-theme` attribute, and the storage key. (Recommend correcting any external assumption about ivory/sage/midnight.)
- **Are all components using tokens?** Almost entirely. Exceptions: 2 `.qd-btn-primary` literals (`_components.scss`) and the 6 segment hexes (duplicated in SCSS + TS). Everything else consumes `--qd-*`.
- **Are tokens semantically named?** Yes. `--qd-bg`, `--qd-surface`, `--qd-surface-elevated`, `--qd-text`, `--qd-text-muted`, `--qd-border`, `--qd-accent`, plus status/focus/overlay/shadow. Good role naming. The gap is **granularity**, not naming: one `--qd-surface` covers page-chrome + cards + badges + inputs + panels; one `--qd-border` covers subtle and structural.
- **Are there unused tokens?** Yes: **`--qd-shadow` (`none`) is defined and never referenced** — dead and misleading, since it implies resting elevation exists. `--qd-radius-lg` and `--qd-space-6` are each used once (minor, not color).
- **Are there duplicated tokens?** `--qd-mushaf-word-selection-indicator` aliases `--qd-accent` (acceptable indirection). Real duplication is outside the token system: the segment palette (SCSS map vs TS array) and the implicit "darker accent" baked into the button hover literal instead of a token.
- **Are dark/light mappings complete?** Mostly. Every semantic color token is remapped in dark. **Gaps:** the `.qd-btn-primary` text/hover literals do not theme (dark-mode hover regression), and the segment hexes do not adapt to dark.
- **Is localStorage / theme switching likely to work?** Yes. `theme.service.ts` is clean: SSR-guarded via `isPlatformBrowser`, reads `localStorage['qd-theme']`, falls back to `prefers-color-scheme`, writes `data-theme` on `documentElement`, and wraps storage in try/catch. Switching is sound; the problem is the values it switches between, not the mechanism.

---

## 9. Recommendations — No Implementation

Direction only; no values, no code changes here.

**Token groups to change first (highest impact):**
1. **The surface ramp (light theme first).** Widen the lightness gaps between page, card, and elevated so cards lift off the page and the elevated step is actually perceptible. The light ramp (ΔL 0.025 / 0.010) is the primary offender; the dark ramp (0.040 / 0.050) is the reference for "how far apart is enough."
2. **Resting elevation.** Either give `--qd-shadow` a real, soft value and apply it to cards/navbar/footer, or commit fully to tonal-only depth (per DESIGN.md's "flat by default") and compensate with a wider surface ramp + stronger borders. Right now it does neither: `--qd-shadow` is `none` and unused, and the ramp is too tight to carry depth alone.
3. **Accent strength and presence.** Raise the accent's distinctness and actually use it on the landing page (primary affordance, selected card, active nav) so the first screen has a focal point. Today the accent is low-chroma and absent from the home view.

**Components needing token remapping:**
- Navbar and footer should consume a **chrome** surface distinct from the **card** surface, so the shell frames the content.
- Badges, inputs, and dropdown panels should not share the exact card fill; give them a distinct field/chip tone.
- Active nav link and card hover should use a perceptible step, not `--qd-surface-elevated`'s current ~1% delta.

**Hardcoded colors to eliminate:**
- Replace the two `.qd-btn-primary` literals with tokens (an on-accent text token and an accent-strong/hover token) so the primary button themes correctly in dark mode.
- Single-source the 6 segment colors as tokens (remove the SCSS-map / TS-array duplication) and give them dark-theme-aware values.
- Remove or implement `--qd-shadow` (do not leave a `none` elevation token that is never used).

**More semantic tokens worth introducing (names illustrative, the brief's suggestions are sound):**
- `--qd-page-bg` (page/app base) vs `--qd-section-bg` (grouping regions) vs `--qd-card-bg` (cards) vs `--qd-card-elevated-bg` (hover/lifted) — break the single `--qd-surface` into a real ladder.
- `--qd-border-subtle` vs `--qd-border-strong` — separate faint internal dividers from confident structural edges.
- `--qd-accent-soft` (tinted backgrounds / selected rows) and `--qd-accent` (the action color), plus `--qd-on-accent` for text on accent.
- `--qd-shadow-soft` actually applied for resting elevation (or an explicit decision to stay shadowless with a compensating tonal ramp).
- A distinct **chrome** surface token for navbar/footer if they should not equal card fill.

Sequencing: (1) widen the light surface ramp + decide the elevation strategy, (2) introduce the page/section/card/elevated and subtle/strong-border tokens and remap chrome/cards/badges/inputs onto them, (3) strengthen and place the accent, (4) tokenize the button literals and segment palette. Steps 1–3 remove the flatness; step 4 is hygiene.

---

## 10. Final Verdict

**CHANGES RECOMMENDED.**

The flatness is real and reproducible from the token values: in the default light theme the page, cards, navbar, and footer span only ~0.035 of OKLCH lightness, there is no resting elevation (`--qd-shadow` is `none` and unused), a single `--qd-surface` serves chrome + cards + badges + inputs, and the accent is both low-chroma and absent from the landing page. The underlying theme system is **healthy** — semantic tokens, near-total token adoption, clean and correct theme switching, and only two minor hardcoded bypasses — so this is a token-value and token-granularity problem, not contrast/readability breakage and not a broken theme system. Text remains readable throughout, which is why this is *recommended* rather than *required*. Fix the light-theme surface ramp, commit to an elevation strategy, split the one surface/border token into a proper ladder, and give the accent strength and presence; the palette tuning then follows.
