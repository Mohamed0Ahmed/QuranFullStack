# Implementation Plan — Phase 1 (Navbar + Footer chrome) + Phase 2 (Global tokens / light-dark surface hierarchy)

**Status:** Ready to implement. This is the executable spec for the navy + gold +
parchment adoption, Phases 1 + 2 only.
**Audience:** the implementer (e.g. Cursor). Apply the edits exactly as written.
**Source of truth:** `../../.architecture/UI_STYLE_SYSTEM.md` §15, `../../../../DESIGN.md`
§2/§4, and the extraction report `real-pages-visual-system-extraction-report.md`.
All OKLCH values below are pre-computed from the prototype hex; **use them as-is**.

> Scope is **Phase 1 + Phase 2 only**. Card hover/elevation (Phase 3), full button
> restyle (Phase 4), and Mushaf/study polish (Phase 5) are **out of scope** here
> except the two small regression-prevention fixes explicitly marked below.

---

## 0. Rules for the implementer (do not skip)

1. **Do not paste prototype CSS or hex.** Use the OKLCH `--qd-*` tokens defined here.
2. **Define every new token in BOTH themes** — `_tokens.scss` (`:root` = light) and
   `_themes.scss` (`[data-theme='dark']`). A token missing from dark breaks dark mode.
3. **Keep existing token names working.** `--qd-bg`, `--qd-surface`, `--qd-border`,
   `--qd-accent`, `--qd-surface-elevated`, `--qd-floating-shadow`, `--qd-shadow` are
   retuned in place (not removed), so current consumers keep resolving.
4. **Do not touch:** Quran/Mushaf fonts (`--qd-font-quran*`), the segment color
   palette (`segment-color-palette.ts` / `segment-rendered-word.component.scss`),
   routing, component TypeScript, or layout structure. No new components.
5. **RTL + reduced-motion:** keep logical properties; any transition must respect
   `prefers-reduced-motion` (already handled in `_components.scss` — extend, don't
   regress).
6. **No commit.** Leave changes in the working tree.

---

## 1. Files to touch

| # | File | Phase | What changes |
|---|------|-------|--------------|
| 1 | `src/styles/_tokens.scss` | P2 | Retune core neutrals/brand to parchment+navy+gold; add new semantic tokens (surfaces, border-strong, primary, accent layers, footer, chrome, shadow ladder, motion). |
| 2 | `src/styles/_themes.scss` | P2 | Dark-theme values for every token added/retuned in #1. |
| 3 | `src/styles/_components.scss` | P2 (hygiene) | Tokenize `.qd-btn-primary` (remove 2 hardcoded literals) so it does not break when `--qd-accent` becomes gold. |
| 4 | `src/styles/_layout.scss` | P1 | `.qd-navbar` → distinct light chrome + border + soft shadow (+ optional blur). `.qd-footer` → dark navy anchor + gradient top hairline. |
| 5 | `src/app/core/layout/top-navbar/top-navbar.component.scss` | P1 | Active item = gold on accent-tint pill; hover = quiet surface + accent; brand color. |
| 6 | `src/app/core/layout/footer/footer.component.scss` | P1 | Footer text → footer tokens; **fix `var(--qd-text-meta)`** (undefined) → `--qd-footer-muted`; error text readable on dark. |

(Real paths are `src/app/core/layout/...`, not `src/app/layout/...`.)

---

## 2. PHASE 2 — Global tokens

### Step P2-1 — `src/styles/_tokens.scss` (`:root`, light)

Retune the existing color tokens to these values and **add** the new ones. Keep all
non-color tokens (fonts, radius, spacing, mushaf layout) as they are, except the
optional radius/motion additions noted.

```scss
:root {
  /* ---- Surfaces (parchment canvas, near-white cards, warm recessed) ---- */
  --qd-bg: oklch(0.985 0.008 91.5);            /* parchment page  #FCFAF4 */
  --qd-surface: oklch(0.997 0.003 90);          /* near-white card (tinted, not pure #FFF) */
  --qd-section-bg: oklch(0.955 0.015 77.1);     /* quiet/recessed section  #F6EFE5 */
  --qd-surface-recessed: oklch(0.921 0.025 75.3); /* deep recessed  #EFE3D3 */
  --qd-surface-elevated: var(--qd-section-bg);  /* back-compat: existing hover bg consumers */

  /* ---- Text ---- */
  --qd-text: oklch(0.278 0.030 256.8);          /* #1F2937 */
  --qd-text-muted: oklch(0.544 0.035 265.1);    /* #667085 */

  /* ---- Borders (navy-tinted hairlines) ---- */
  --qd-border: oklch(0.263 0.046 250 / 0.12);        /* navy @ 12% */
  --qd-border-strong: oklch(0.263 0.046 250 / 0.22); /* navy @ 22% */

  /* ---- Primary (navy structural) ---- */
  --qd-primary: oklch(0.263 0.046 250.0);       /* navy  #12263A */
  --qd-primary-fg: oklch(0.985 0.008 91.5);     /* parchment on navy */
  --qd-primary-hover: oklch(0.236 0.044 254.8); /* deeper navy #0F1F33 (AA-safe primary-btn hover) */

  /* ---- Accent (gold) + layers ---- */
  --qd-accent: oklch(0.718 0.118 83.7);         /* gold  #C79D43 — background/large-element use */
  --qd-accent-hover: oklch(0.660 0.117 81.6);   /* #B68A30 */
  --qd-accent-soft: oklch(0.845 0.087 86.6);    /* #E5C98A */
  --qd-accent-tint: oklch(0.960 0.028 86.6);    /* #FAF1DD */
  --qd-accent-text: var(--qd-primary);          /* AA-safe accent-emphasis TEXT on light (navy);
                                                   gold fails as small text on light surfaces */

  /* ---- Status (retuned to prototype; warning kept) ---- */
  --qd-danger: oklch(0.541 0.138 22.8);         /* #B14848 */
  --qd-warning: oklch(0.70 0.14 75);            /* unchanged */
  --qd-success: oklch(0.546 0.062 162.7);       /* #4E7C66 */

  /* ---- Focus ---- */
  --qd-focus-ring: oklch(0.60 0.10 84);         /* gold focus outline color */
  --qd-ring: 0 0 0 4px oklch(0.718 0.118 84 / 0.22); /* soft halo (used in Phase 4) */

  /* ---- Footer (dark anchor; defined here, used by .qd-footer) ---- */
  --qd-footer-bg: oklch(0.236 0.044 254.8);     /* #0F1F33 */
  --qd-footer-bg-2: oklch(0.304 0.055 247.5);   /* #163149 */
  --qd-footer-text: oklch(0.919 0.018 89.4);    /* #E9E4D7 */
  --qd-footer-muted: oklch(0.680 0.037 261.9);  /* #8C99B0 */
  --qd-footer-accent: oklch(0.786 0.099 85.6);  /* #D6B56D */
  --qd-footer-accent-hover: oklch(0.841 0.090 85.8); /* #E5C786 */
  --qd-footer-border: oklch(1 0 0 / 0.08);      /* white @ 8% */

  /* ---- Navbar chrome (translucent near-white) ---- */
  --qd-chrome-bg: oklch(0.997 0.003 90 / 0.85);

  /* ---- Elevation ladder (navy-tinted, soft) ---- */
  --qd-shadow-sm: 0 1px 2px oklch(0.263 0.046 250 / 0.04), 0 1px 1px oklch(0.263 0.046 250 / 0.03);
  --qd-shadow: 0 6px 20px oklch(0.263 0.046 250 / 0.07), 0 2px 6px oklch(0.263 0.046 250 / 0.04);
  --qd-floating-shadow: 0 30px 70px oklch(0.263 0.046 250 / 0.14), 0 8px 22px oklch(0.263 0.046 250 / 0.07);
  /* NOTE: --qd-shadow was `none` (unused) before; it now carries the hover-elevation value. */

  --qd-overlay: oklch(0.236 0.044 255 / 0.30);  /* navy scrim (was warm ink) */

  /* ---- Motion tokens (consumed by chrome now, components later) ---- */
  --qd-t-fast: 140ms ease;
  --qd-t-base: 220ms cubic-bezier(0.2, 0.7, 0.3, 1);

  /* ---- Radius (RECOMMENDED bump toward prototype; opt out if undesired) ---- */
  --qd-radius-sm: 0.5rem;     /* 8px  (was 4px) */
  --qd-radius-md: 0.875rem;   /* 14px (was 8px) */
  --qd-radius-lg: 1.375rem;   /* 22px (was 12px) */
  --qd-radius-pill: 999px;

  /* keep existing: spacing scale, font tokens, mushaf layout tokens, navbar-block-size */
}
```

> The mushaf word-selection-indicator and ayah-marker tokens stay as-is (they
> reference `--qd-accent` / `--qd-text-muted`, so they follow automatically).

### Step P2-2 — `src/styles/_themes.scss` (`[data-theme='dark']`)

```scss
[data-theme='dark'] {
  /* Surfaces */
  --qd-bg: oklch(0.189 0.032 266.8);            /* #0D1322 */
  --qd-surface: oklch(0.228 0.037 265.3);       /* #141C2E */
  --qd-section-bg: oklch(0.265 0.039 262.7);    /* #1B2538 */
  --qd-surface-recessed: oklch(0.302 0.045 264.3); /* #232E45 */
  --qd-surface-elevated: var(--qd-section-bg);

  /* Text */
  --qd-text: oklch(0.935 0.007 277.2);          /* #E8E9EE */
  --qd-text-muted: oklch(0.706 0.032 269.0);    /* #98A0B5 */

  /* Borders (solid in dark) */
  --qd-border: oklch(0.319 0.045 266.5);        /* #28324A */
  --qd-border-strong: oklch(0.403 0.062 268.0); /* #3A476A */

  /* Primary == gold in dark (matches prototype midnight) */
  --qd-primary: oklch(0.772 0.098 82.0);        /* #D4AF6A */
  --qd-primary-fg: oklch(0.189 0.032 266.8);    /* dark text on gold */
  --qd-primary-hover: var(--qd-accent-hover);   /* gold lightens on hover in dark */

  /* Accent + layers */
  --qd-accent: oklch(0.772 0.098 82.0);         /* #D4AF6A */
  --qd-accent-hover: oklch(0.817 0.093 82.6);   /* #E1BE7C */
  --qd-accent-soft: oklch(0.323 0.030 88.2);    /* #3A3322 */
  --qd-accent-tint: oklch(0.250 0.030 281.2);   /* #1F2030 */
  --qd-accent-text: var(--qd-accent);           /* gold reads fine as text on dark surfaces */

  /* Status */
  --qd-danger: oklch(0.682 0.116 20.5);         /* #D77A7A */
  --qd-warning: oklch(0.75 0.14 75);            /* unchanged */
  --qd-success: oklch(0.761 0.072 152.1);       /* #8FBF9B */

  /* Focus */
  --qd-focus-ring: oklch(0.78 0.10 82);
  --qd-ring: 0 0 0 4px oklch(0.772 0.098 82 / 0.22);

  /* Footer (deeper navy in dark) */
  --qd-footer-bg: oklch(0.161 0.029 266.8);     /* #080D1A */
  --qd-footer-bg-2: oklch(0.202 0.034 265.5);   /* #0F1626 */
  --qd-footer-text: oklch(0.935 0.007 277.2);
  --qd-footer-muted: oklch(0.706 0.032 269.0);
  --qd-footer-accent: oklch(0.772 0.098 82.0);
  --qd-footer-accent-hover: oklch(0.817 0.093 82.6);
  --qd-footer-border: oklch(1 0 0 / 0.06);

  /* Chrome (translucent dark surface) */
  --qd-chrome-bg: oklch(0.228 0.037 265.3 / 0.82);

  /* Shadows (heavier, tinted near-black) */
  --qd-shadow-sm: 0 1px 2px oklch(0.10 0.02 265 / 0.40), 0 1px 1px oklch(0.10 0.02 265 / 0.30);
  --qd-shadow: 0 4px 16px oklch(0.10 0.02 265 / 0.35), 0 2px 6px oklch(0.10 0.02 265 / 0.25);
  --qd-floating-shadow: 0 28px 70px oklch(0.10 0.02 265 / 0.55), 0 6px 18px oklch(0.10 0.02 265 / 0.35);

  --qd-overlay: oklch(0.08 0.01 265 / 0.62);

  /* mushaf ayah-marker override stays as currently defined */
}
```

### Step P2-3 (hygiene, required) — `src/styles/_components.scss` `.qd-btn-primary`

Removes the two hardcoded literals so the primary button does not turn into a
gold-with-brown-hover mess once `--qd-accent` is gold. Navy primary, gold hover
(mirrors the prototype; works in both themes because `--qd-primary` / `--qd-accent-hover`
flip per theme).

```scss
.qd-btn-primary {
  background: var(--qd-primary);
  color: var(--qd-primary-fg);
  border-color: var(--qd-primary);

  &:hover {
    background: var(--qd-primary-hover);
    border-color: var(--qd-primary-hover);
  }
}
```

> Hover uses `--qd-primary-hover` (deeper navy in light, lighter gold in dark), not
> `--qd-accent-hover`. Parchment-on-gold-hover was only 3.02:1; deeper-navy keeps it at
> ~16:1 in light and the dark gold-on-dark-text at ~10:1.

---

## 3. PHASE 1 — Navbar + Footer chrome

### Step P1-1 — `src/styles/_layout.scss` → `.qd-navbar`

Replace the current navbar background/border block. Make it a distinct, lifted light
chrome (translucent + blur + hairline + soft shadow). Keep height/padding/flex as-is.

```scss
.qd-navbar {
  display: flex;
  align-items: center;
  gap: var(--qd-space-4);
  box-sizing: border-box;
  height: var(--qd-navbar-block-size);
  min-height: var(--qd-navbar-block-size);
  max-height: var(--qd-navbar-block-size);
  padding: var(--qd-space-3) var(--qd-space-5);
  background: var(--qd-chrome-bg);
  backdrop-filter: saturate(160%) blur(14px);
  -webkit-backdrop-filter: saturate(160%) blur(14px);
  border-block-end: 1px solid var(--qd-border);
  box-shadow: var(--qd-shadow-sm);
}

/* Opaque fallback where backdrop-filter is unsupported */
@supports not ((backdrop-filter: blur(1px)) or (-webkit-backdrop-filter: blur(1px))) {
  .qd-navbar { background: var(--qd-surface); }
}
```

> Sticky positioning is **optional and out of scope** (it is a layout change). Leave
> the navbar non-sticky for this phase unless explicitly requested.

### Step P1-2 — `src/styles/_layout.scss` → `.qd-footer`

Replace the current footer background/border block. Dark navy anchor + gradient top
hairline. Set `color` so all footer text is light.

```scss
.qd-footer {
  position: relative;
  padding: var(--qd-space-4) var(--qd-space-5);
  background:
    radial-gradient(circle at 8% 0%, var(--qd-footer-bg-2), transparent 35%),
    var(--qd-footer-bg);
  color: var(--qd-footer-text);
  border-block-start: 1px solid var(--qd-footer-border);
}

/* Gradient top hairline (purposeful accent end-cap) */
.qd-footer::before {
  content: '';
  position: absolute;
  inset-inline: 0;
  inset-block-start: 0;
  block-size: 1px;
  background: linear-gradient(90deg, transparent, var(--qd-footer-accent), transparent);
  opacity: 0.55;
}
```

### Step P1-3 — `src/app/core/layout/top-navbar/top-navbar.component.scss`

Update the active + hover states (currently active uses the near-invisible
`--qd-surface-elevated`; hover via ghost is barely visible). Active = gold text on an
accent-tint pill; hover = quiet surface + accent text. Optionally color the brand navy.

```scss
/* active nav item: AA-safe emphasis text on accent-tint pill
   (--qd-accent-text = navy in light / gold in dark; gold itself fails as small
   text on the pale light tint, ~2.2:1, so the pill carries the gold, the label
   carries the contrast) */
.nav-link.active {
  background-color: var(--qd-accent-tint);
  color: var(--qd-accent-text);
}

/* hover: quiet warm surface + emphasis text */
.nav-link:hover {
  background-color: var(--qd-section-bg);
  color: var(--qd-accent-text);
}

/* dropdown / mobile active use the same AA-safe emphasis text */
.dropdown-link.active,
.mobile-link.active { color: var(--qd-accent-text); }
.dropdown-link:hover,
.mobile-link:hover { background-color: var(--qd-section-bg); }

/* brand wordmark in structural navy (optional but recommended) */
.brand .qd-page-title { color: var(--qd-primary); }
```

> **Do not use `--qd-accent` (gold) as small text on a light surface** — it fails AA
> (~2.2:1 on tint, ~2.5:1 on near-white). Use `--qd-accent-text`. Gold is for the pill,
> backgrounds, large elements, and dark-surface text.

> Leave `.theme-toggle` hover as-is; it already resolves to `--qd-surface-elevated`
> (now the quiet warm tone), which is correct.

### Step P1-4 — `src/app/core/layout/footer/footer.component.scss`

Fix the **undefined** `--qd-text-meta` reference and make footer text use footer
tokens (now that the footer is dark in both themes).

```scss
/* was: color: var(--qd-text-meta)  -- that token does not exist */
.health-indicator { color: var(--qd-footer-muted); }

/* error text must read on the dark footer */
.health-error { color: var(--qd-footer-text); }

/* health dots keep status tokens (success/danger/warning) — readable on navy */
```

> The retry button (`.health-retry-btn` → `.qd-btn`) will render as a light button on
> the dark footer. That is acceptable for this phase; a dark-footer button variant is
> Phase 4 polish, not required here.

---

## 4. Acceptance criteria

**Phase 2 (tokens):**
- [ ] Every new token (`--qd-section-bg`, `--qd-surface-recessed`, `--qd-border-strong`,
  `--qd-primary`, `--qd-primary-fg`, `--qd-accent-hover/soft/tint`, all `--qd-footer-*`,
  `--qd-chrome-bg`, `--qd-shadow-sm`, `--qd-ring`, `--qd-t-fast`, `--qd-t-base`,
  `--qd-radius-pill`) is defined in **both** `_tokens.scss` and `_themes.scss`.
- [ ] No remaining hardcoded color literal in `_components.scss` `.qd-btn-primary`.
- [ ] App surfaces shift to parchment+navy+gold; light text on light, dark on dark; no
  invisible text in either theme.
- [ ] Primary button is navy (light) / gold (dark) with a gold hover, not a clashing pair.

**Phase 1 (chrome):**
- [ ] Navbar is visibly distinct from the page and from cards (border + soft shadow,
  translucent light fill), not the flat same-surface bar.
- [ ] Active nav item reads clearly (navy label on the gold-tint pill in light; gold
  label on the dark pill in dark); hover is visible but quiet. **Active/hover nav text
  meets WCAG AA (≥4.5:1)** — do not ship gold-on-tint (~2.2:1).
- [ ] Footer is a dark navy anchor with warm off-white text, muted secondary text, and
  a subtle gold gradient top hairline.
- [ ] No `var(--qd-text-meta)` remains anywhere; footer health/error text is readable.
- [ ] Dark theme renders correctly (no light-only literals leaking in).

**Both:**
- [ ] `git diff` is confined to the 6 files in §1 (token + chrome only); no component
  TS, routing, Quran fonts, or segment palette touched.

---

## 5. Verification commands

```bash
cd Frontend/quran-dashboard-ui

# 1. Build must pass
npm run build

# 2. Run unit tests (chrome/token change should not break specs)
npm test -- --watch=false   # or the project's configured test script

# 3. Confirm focused diff (only the 6 expected files)
git status --short
git diff --stat

# 4. Sanity: no undefined footer token and no new hardcoded hex in chrome
grep -rn "qd-text-meta" src/            # expect: no matches
grep -rnE "#[0-9a-fA-F]{3,6}" src/styles/_layout.scss \
  src/app/core/layout/top-navbar src/app/core/layout/footer   # expect: none (tokens only)
```

Then visually verify both themes (toggle via the navbar) on the dashboard home and a
Mushaf page: navbar lifted, footer dark anchor, cards readable, active nav gold.

---

## 6. Out of scope (do NOT do here)

- Card hover motion / resting elevation on `.qd-card` (**Phase 3**).
- Full button system: ghost/soft variants, focus-ring halo application, badge/chip
  contrast (**Phase 4**) — only the primary-button tokenization hygiene fix is included.
- Mushaf/study surfaces, reader chrome, side panels (**Phase 5**).
- Segment word-coloring palette (`#3d6b8e` …) — unrelated Mushaf feature, leave alone.
- Quran/Mushaf glyph fonts and rendering — do not change.
- Sticky navbar, container width, nav height, or any layout-structure change.
- Refactoring every component's hardcoded `0.15s` transition to `--qd-t-*` (later
  cleanup; the tokens are added now but broad retrofit is not required this phase).

---

## 7. Risks / gotchas

- **Dark mode parity:** the single biggest risk. Verify every retuned/added token has a
  dark value. Especially `--qd-primary-fg` (must be dark text on the gold dark-primary)
  and shadows (must be the heavier near-black set, not the light navy-tinted set).
- **`--qd-surface` near-white vs pure white:** keep the tiny warm tint
  (`oklch(0.997 0.003 90)`); do not set it to `#fff`. Separation from the page comes
  from the parchment `--qd-bg` + border + (Phase 3) shadow, not from tone alone.
- **Translucent border over varying bg:** `--qd-border` is navy@12% (translucent). It
  reads correctly over parchment and cards; if any element places it over a dark
  surface in light theme, check contrast (none expected in chrome scope).
- **`backdrop-filter` performance/support:** the `@supports` fallback handles
  unsupported browsers; the blur is chrome-only and cheap, but verify no jank on the
  Mushaf reader scroll.
- **Existing hover consumers of `--qd-surface-elevated`:** they now resolve to the warm
  quiet tone — this is intended and on-brand; just confirm nothing looks worse (it
  should look better than the prior ~1% delta).
```
