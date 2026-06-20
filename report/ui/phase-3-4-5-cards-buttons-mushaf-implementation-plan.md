# Implementation Plan — Phase 3 (Cards/elevation) + Phase 4 (Buttons/active/selected) + Phase 5 (Mushaf/study polish)

**Status:** Ready, but gated. Implement **only after Phase 1 + 2 are merged/reviewed**
(see the companion plan `phase-1-2-navbar-footer-tokens-implementation-plan.md`). These
phases depend on the tokens introduced there (`--qd-shadow-sm`, `--qd-shadow`,
`--qd-border-strong`, `--qd-section-bg`, `--qd-surface-recessed`, `--qd-accent-*`,
`--qd-primary`, `--qd-ring`, `--qd-t-fast`, `--qd-t-base`).
**Audience:** the implementer (Cursor). **Source of truth:** `../../.architecture/UI_STYLE_SYSTEM.md`
§15 (E, F, G) + `../../../../DESIGN.md` §4.

> **Three phases remain.** Do them in order, **one phase = one reviewable diff**. Stop
> at each checkpoint (§Review checkpoints) for review before starting the next.

---

## 0. Rules (same as Phases 1–2)

1. Tokens only — no hardcoded colors/shadows/timings; use the `--qd-*` set.
2. Anything new is defined for both themes already (these phases mostly **consume**
   tokens, not define them; if you must add one, add it to both `_tokens.scss` and
   `_themes.scss`).
3. **Do not touch** Quran/Mushaf glyph fonts, Quran rendering, or the segment color
   palette (`segment-color-palette.ts`, `segment-rendered-word.component.scss`).
4. **Never animate** Quran text, ayah glyphs, or word segments.
5. Respect `prefers-reduced-motion` (no transforms when reduced).
6. No routing or component-TypeScript behavior changes. Adding a CSS class in a
   template is allowed; changing logic is not.
7. No commit.

---

## PHASE 3 — Card hover / elevation system

**Goal:** cards rest on a soft shadow and lift gently on hover (shadow + border + small
translate), replacing today's invisible `--qd-surface-elevated` background swap.

### Files
| File | Change |
|------|--------|
| `src/styles/_components.scss` | `.qd-card` resting shadow + hover/quiet/bordered/feature/mini variants. |
| `src/app/features/dashboard/pages/dashboard-home/dashboard-home.component.scss` | `.dashboard-card` lift on hover (remove the bg-swap hover). |
| `src/app/features/mushaf/components/_study-card.shared.scss` | confirm `__coverage` uses `--qd-section-bg` (no shadow); no other change. |

### Step P3-1 — `_components.scss` `.qd-card`

```scss
.qd-card {
  background: var(--qd-surface);
  border: 1px solid var(--qd-border);
  border-radius: var(--qd-radius-md);
  padding: var(--qd-space-4);
  box-shadow: var(--qd-shadow-sm);   /* resting elevation (NEW) */
}

/* Interactive lift — add this class to clickable cards */
.qd-card--hover {
  transition: border-color var(--qd-t-fast), box-shadow var(--qd-t-fast),
              transform var(--qd-t-fast);
}
.qd-card--hover:hover {
  border-color: var(--qd-border-strong);
  box-shadow: var(--qd-shadow);
  transform: translateY(-2px);
}

/* Quiet/recessed card: no shadow, warm tone */
.qd-card--quiet {
  background: var(--qd-section-bg);
  border-color: transparent;
  box-shadow: none;
}

/* Border-only card: no shadow */
.qd-card--bordered { box-shadow: none; }

/* Feature card: larger radius/padding; deepen shadow on hover, NO translate */
.qd-card--feature {
  border-radius: var(--qd-radius-lg);
  padding: var(--qd-space-5);
}
.qd-card--feature.qd-card--hover:hover { transform: none; box-shadow: var(--qd-shadow); }

/* Mini card: smaller lift, accent-soft hover border */
.qd-card--mini { padding: var(--qd-space-3); }
.qd-card--mini.qd-card--hover:hover {
  border-color: var(--qd-accent-soft);
  transform: translateY(-1px);
}

@media (prefers-reduced-motion: reduce) {
  .qd-card--hover { transition: border-color var(--qd-t-fast), box-shadow var(--qd-t-fast); }
  .qd-card--hover:hover,
  .qd-card--mini.qd-card--hover:hover { transform: none; }
}
```

### Step P3-2 — `dashboard-home.component.scss` `.dashboard-card`

Replace the current `background: var(--qd-surface-elevated)` hover with the lift.

```scss
.dashboard-card {
  display: block;
  text-decoration: none;
  transition: border-color var(--qd-t-fast), box-shadow var(--qd-t-fast),
              transform var(--qd-t-fast);

  &:hover {
    border-color: var(--qd-border-strong);
    box-shadow: var(--qd-shadow);
    transform: translateY(-2px);
  }

  &:focus-visible {
    outline: 2px solid var(--qd-focus-ring);
    outline-offset: 2px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .dashboard-card { transition: border-color var(--qd-t-fast), box-shadow var(--qd-t-fast); }
  .dashboard-card:hover { transform: none; }
}
```

### Phase 3 acceptance
- [ ] Cards show a soft resting shadow; they no longer look flat against the page.
- [ ] Hovering a dashboard card lifts it ~2px with a stronger shadow + border (no bg
  flash).
- [ ] Reduced-motion users get the shadow/border change but no movement.
- [ ] Both themes correct (dark uses the heavier shadow set automatically).

---

## PHASE 4 — Buttons / active / selected states

**Goal:** finish the button system (primary navy already done in Phase 2), give ghost/
secondary/soft clear behavior, a soft focus ring, accent-tint selected states, and make
badges distinct from cards.

### Files
| File | Change |
|------|--------|
| `src/styles/_components.scss` | `.qd-btn` active + focus ring; `.qd-btn-secondary` / `.qd-btn-ghost` hover; add `.qd-btn-soft`; `.qd-badge` distinct fill + accent variant; `.qd-is-selected` utility. |
| `src/styles/_forms.scss` | add the soft focus ring to `.qd-input` / `.qd-select` (optional polish). |

> **Do not add a border to `.qd-btn-ghost`** — the navbar nav-links reuse it and must
> stay borderless. The bordered/outlined role is `.qd-btn-secondary`.

### Step P4-1 — `_components.scss` buttons

```scss
.qd-btn {
  /* keep existing layout/typography; update transition + add active/focus */
  transition: background-color var(--qd-t-fast), border-color var(--qd-t-fast),
              color var(--qd-t-fast), transform var(--qd-t-fast);

  &:active { transform: translateY(1px); }

  &:focus-visible {
    outline: 2px solid var(--qd-focus-ring);
    outline-offset: 2px;
    box-shadow: var(--qd-ring);   /* soft gold halo */
  }
}

/* secondary = outlined; hover strengthens border + quiet fill */
.qd-btn-secondary:hover {
  background: var(--qd-section-bg);
  border-color: var(--qd-border-strong);
}

/* ghost stays borderless (used by nav-links); hover = quiet fill */
.qd-btn-ghost:hover {
  color: var(--qd-text);
  background: var(--qd-section-bg);
}

/* NEW soft/tonal accent button.
   Label uses --qd-accent-text (navy in light / gold in dark): gold-on-tint fails AA. */
.qd-btn-soft {
  background: var(--qd-accent-tint);
  color: var(--qd-accent-text);
  border-color: transparent;

  &:hover { background: var(--qd-accent-soft); }
}

@media (prefers-reduced-motion: reduce) {
  .qd-btn { transition: background-color var(--qd-t-fast), border-color var(--qd-t-fast),
                        color var(--qd-t-fast); }
  .qd-btn:active { transform: none; }
}
```

### Step P4-2 — `_components.scss` badges + selected state

```scss
/* badge fill distinct from the card it sits on */
.qd-badge {
  background: var(--qd-section-bg);
  color: var(--qd-text-muted);
  border: 1px solid var(--qd-border);
}
.qd-badge--accent {
  background: var(--qd-accent-tint);
  color: var(--qd-accent-text);   /* AA-safe: gold-on-tint fails */
  border-color: transparent;
}

/* shared selected/active treatment (calm: tint fill + accent-text, no heavy fill).
   Selection is conveyed by the tint fill AND the text (not the border alone), so it
   does not rely on color alone. The gold border is decorative; if you want the
   boundary itself to read clearly, use --qd-border-strong instead of --qd-accent. */
.qd-is-selected {
  background: var(--qd-accent-tint);
  border-color: var(--qd-accent);
  color: var(--qd-accent-text);
}
```

### Step P4-3 (optional) — `_forms.scss` focus ring

Add `box-shadow: var(--qd-ring);` to the existing `:focus`/`:focus-visible` of
`.qd-input` and `.qd-select` (keep the current accent border + outline).

### Phase 4 acceptance
- [ ] Primary = navy (light) / gold (dark); secondary = outlined with strong-border
  hover; ghost stays borderless; `.qd-btn-soft` is gold-on-tint.
- [ ] Buttons press down 1px on `:active` and show a soft gold focus halo on keyboard
  focus (reduced-motion: no press).
- [ ] Badges are visibly distinct from the card surface; `.qd-badge--accent` reads gold.
- [ ] `.qd-is-selected` gives a calm tint+accent state (no saturated fill).
- [ ] Nav-links (which reuse `.qd-btn-ghost`) are still borderless and correct.

---

## PHASE 5 — Mushaf / study page-specific polish

**Goal:** bring the reader and study surfaces onto the new ladder/elevation, *without*
touching Quran rendering. Most of this happens automatically because these components
already consume `--qd-surface` / `--qd-border` / `--qd-floating-shadow` / accent mixes;
Phase 5 is **targeted review + small adjustments**, not a fixed recipe.

### Surface mapping for the reader (apply per region)
| Reader region | Token to use |
|---------------|--------------|
| Page / reader background | `--qd-bg` (parchment) — keep `transparent` pass-through |
| Content/study cards (tafsir, translation, i3rab, morphology) | `.qd-card` system (resting `--qd-shadow-sm`, hover lift if interactive) |
| Quiet/labelled panels, coverage strips, meta rows | `--qd-section-bg` (no shadow) |
| Deep insets / nested panels | `--qd-surface-recessed` |
| Side panel / drawer container | `--qd-surface` + `--qd-border`; floating → `--qd-floating-shadow` |
| Dropdowns/pickers (source-selector, surah-jump-picker) | already `--qd-floating-shadow` — verify only |
| Selected ayah / selected word | `.qd-is-selected` (accent-tint + accent border), or keep existing accent ring mixes |

### Files to review (Mushaf feature)
`src/app/features/mushaf/components/`:
`selected-ayah-section`, `selected-word-section`, `study-context-section`,
`tafsir-card`, `translation-card`, `full-i3rab-card`, `word-morphology-summary`,
`segment-data-rows`, `source-selector`, `surah-jump-picker`,
`mushaf-header-navigation`, `mushaf-page-area`, `mushaf-page-view`,
`_study-card.shared.scss`, plus `pages/mushaf-reader-page`.

### Do NOT touch (Quran rendering)
`mushaf-line`, `mushaf-word`, `mushaf-marker`, `segment-rendered-word`,
`segment-color-palette.ts`, and any `--qd-font-quran*` / ayah-marker styling. Word
selection indicators may keep their existing `color-mix(--qd-accent …)` rings.

### Per-component checklist
- [ ] Study/result cards use the `.qd-card` elevation system (resting shadow; lift only
  if the card is clickable).
- [ ] Recessed/meta panels use `--qd-section-bg` or `--qd-surface-recessed`, not the
  plain card surface, so hierarchy reads.
- [ ] Selected ayah/word uses the calm accent-tint selected treatment; no heavy fills.
- [ ] Floating panels (pickers, source selector) use `--qd-floating-shadow` and read as
  lifted over the parchment.
- [ ] Quran text, glyph fonts, segment word colors: **unchanged**; no transitions/motion
  on them.
- [ ] Both themes verified on a real Mushaf page; tashkeel/diacritics render correctly
  (no regression from chrome changes).

### Phase 5 acceptance
- [ ] Reader chrome and study cards match the navy+gold+parchment system and have clear
  surface hierarchy.
- [ ] No change to Quran rendering, fonts, or segment coloring; diff in
  `mushaf-line/word/marker` and `segment-*` is empty.
- [ ] Dark mode reader is correct and readable.

---

## Review checkpoints (the gate between phases)

1. **After Phase 1 + 2:** reviewer checks token parity (both themes), navbar distinct,
   footer dark anchor, `--qd-text-meta` fixed, primary button tokenized, focused diff.
   → approve → proceed.
2. **After Phase 3:** cards have resting + hover elevation; reduced-motion respected.
   → approve → proceed.
3. **After Phase 4:** button/badge/selected system complete; nav-links still borderless;
   focus ring accessible. → approve → proceed.
4. **After Phase 5:** reader/study polished; Quran rendering untouched. → approve →
   done.

Keep each phase a **separate diff/commit** so review stays small.

---

## Verification (run after each phase)

```bash
cd Frontend/quran-dashboard-ui
npm run build                      # must pass
npm test -- --watch=false          # or the configured test script
git diff --stat                    # confirm only the phase's files changed

# Phase 5 guard: Quran rendering must be untouched
git status --short -- \
  src/app/features/mushaf/components/mushaf-line \
  src/app/features/mushaf/components/mushaf-word \
  src/app/features/mushaf/components/mushaf-marker \
  src/app/features/mushaf/components/segment-rendered-word \
  src/app/features/mushaf/state/segment-color-palette.ts
# expect: empty
```

---

## Out of scope (all three phases)

- New theme names (stays light + dark).
- Re-deriving token values — they are fixed in the Phase 1+2 plan; reuse them.
- Quran/Mushaf glyph fonts, Quran rendering, segment word coloring.
- Layout structure, routing, container widths, sticky nav.
- Any new color literals — tokens only.

---

## Risks / gotchas

- **`.qd-btn-ghost` border:** must remain borderless or navbar links get boxed. Use
  `.qd-btn-secondary` for the outlined role.
- **Reduced-motion:** every new `transform` (card lift, button press) needs a
  reduced-motion fallback that drops the transform but keeps the color/shadow change.
- **Phase 5 scope creep:** it is easy to drift into restyling Quran text. Treat the
  "Do NOT touch" list as hard; if a Mushaf card needs the elevation system, change only
  its container chrome, never the verse rendering.
- **Dark shadows:** confirm cards/popovers use the dark (heavier) shadow set — this is
  inherited from Phase 2 tokens, but verify visually in dark mode.
- **Order matters:** Phases 3–5 assume Phase 2 tokens exist. If a `--qd-shadow-sm` /
  `--qd-border-strong` / `--qd-section-bg` is missing, stop and finish Phase 2 first.
```
