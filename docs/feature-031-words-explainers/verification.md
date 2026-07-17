# Feature 031 — Words Explainers — Verification

- **Branch:** `restyle/flat-green-light` (implemented here per the locked decision; not a new branch).
- **Date:** 2026-07-17.
- **Scope verified:** the collapsible per-page explainer hero on all five explorers, the hub
  redesign (intro + orientation chain + content-fed cards), the shared content model, the collapse
  memory service, the D5 allowed-green amendment, and the shared `qdAyahCard` recessed-tone tweak (C1).

## Automated

| Gate | Command | Result |
|---|---|---|
| Build | `npm run build` | **Pass** (exit 0). Only a pre-existing, unrelated SCSS-budget warning on `selected-ayah-section.component.scss`. |
| Full frontend suite | `npm test` (`VITEST_MAX_FORKS=2`) | **Pass** — 157 files, **1826 tests**, exit 0. |
| Whitespace | `git diff --check` | **Clean.** |

New/updated specs: `words-explainer.content.spec.ts` (titles equal each page's `pageTitle`; canonical
terms only; verbatim taglines; chain shape), `words-explainer.component.spec.ts` (frame from content,
projected example, `aria-expanded`/`aria-controls`, collapsed hides only the body), `words-explainer-preference.spec.ts`
(synchronous first-read restore, per-key isolation, data-driven storage-failure fallback),
`words-hub-page.component.spec.ts` (5 content-fed cards, stable-slug testids, chain, routes, no
coming-soon), `word-section-card.component.spec.ts` (single active link, no disabled branch), plus
page-level hero tests on roots (mount point + synchronous collapsed-first-render) and unique-words
(hero after the mode line).

## WCAG 2.1 AA contrast (computed, OKLCH → sRGB → relative luminance)

| Pair | Contrast | Verdict |
|---|---|---|
| `qdAyahCard`: Quran text `--qd-text` on new `--qd-ayah-card-bg` (light) | 12.70:1 | AA/AAA |
| `qdAyahCard`: muted meta `--qd-text-muted` on `--qd-ayah-card-bg` (light) | 4.69:1 | AA |
| Benefit callout: `--qd-accent-text` on `--qd-accent-tint` (light) | 6.74:1 | AA/AAA |
| Benefit callout: `--qd-accent-text`(gold) on `--qd-accent-tint`(navy) (dark) | 7.76:1 | AA/AAA |

## Manual browser pass (dev server, RTL, desktop + phone, light + dark)

Verified live at `https://localhost:4200` (backend intentionally down — the hero and hub are static
content and render fully; the explorer tables show their normal load-error, confirming the hero sits
**above and outside** the mounted shells and never depends on `listState()`).

- **Hub — light & dark (1280px):** intro title + subtitle; the orientation chain renders
  `الجذر ← الصيغة المعجمية ← الأصل الصرفي ← الكلمة + أنواع الكلمات (نحوي)` with the **`←` arrows
  pointing in the RTL reading direction** (not mirrored — D4 rider cleared) and the `+` before the
  grammatical axis; all chain nodes neutral; five content-fed cards in a calm 2+2+1 grid, each with
  green eyebrow, muted ordinal, title, and tagline; routes correct
  (`unique/tashkeel`, `roots`, `lemmas`, `stems`, `types`); no coming-soon cards. Dark reads gold-on-navy, coherent with the interim dark theme.
- **Roots hero — light & dark:** kicker (muted ordinal + green eyebrow) with the toggle pinned to the
  inline-end; tagline; body; green `h3`; the recessed `مثال` example block with six Amiri word chips +
  role notes; the green `الفائدة` callout. Exactly one visible page title (`<h1>`); the hero adds no
  duplicate heading (it is a region named by `aria-label`).
- **Collapse + memory:** clicking `طيّ الشرح` sets `aria-expanded=false`, removes the body via `@if`,
  keeps the tagline + kicker visible, flips the label to `عرض الشرح`, and persists
  `localStorage['qd-words-explainer']='roots'`.
- **Synchronous restore / no shift:** after reload with `roots` stored collapsed, the hero renders
  **collapsed on the first paint** (`aria-expanded=false`, the body element was never present) — no
  expand-then-collapse jolt.
- **Phone (390px):** the hero and its example region reflow to one column; the Amiri chips wrap 2-up;
  the callout wraps cleanly; RTL throughout.

## Constraints

- Presentation-only; no backend/DTO/DB change; no URL-state or cache-key change; Quran rendering
  untouched. Example words are illustrative `مثال` morphology in the Amiri face — never presented as
  Quranic quotations or queryable counts (D10).
- Flat-green, Arabic-first/RTL, logical properties only; no shadow/gradient/lift on the hero or hub.
- Green budget: hero green limited to eyebrow + `h3` + `مثال` label (via `--qd-accent-text`) + the one
  sanctioned `الفائدة` callout; ordinal muted; chain nodes neutral. The callout is added as item 8 of
  the allowed-green list in `DESIGN.md` §2 and `UI_STYLE_SYSTEM.md` §16.3 (kept word-identical).
- Mounted-shell / toolbar / table / detail invariants preserved; page-title testids unchanged; hub
  card testids migrated to stable slugs `words-hub-card--<key>`.
