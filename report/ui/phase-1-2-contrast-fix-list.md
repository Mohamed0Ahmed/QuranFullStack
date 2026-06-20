# Fix List — Phase 1+2 contrast follow-up (for Cursor)

**Why:** review found the light-theme active/hover nav text (gold `--qd-accent` on a
pale tint) is **2.24:1 / 2.21:1**, failing WCAG AA (the app requires AA, and the active
nav is the primary "where am I" cue). The dropdown/mobile active items have the same
problem (gold on near-white = ~2.5:1). The primary-button hover dips to 3.02:1. Root
cause: **gold is a background/large-element color, not a small-text color on light
surfaces.**

**Fix:** add two theme-aware tokens and point the affected text at them. Gold stays the
accent for the pill/background and for dark mode; light-mode emphasis text becomes navy.
All fixes verified ≥13:1 (light) and ≥7.7:1 (dark).

**Scope:** 4 files. Tokens only, no new hardcoded colors, both themes. No commit.

---

## Fix 1 (MAJOR) — Accessible active/hover nav text

### 1a. `src/styles/_tokens.scss` — add to `:root` (light), in the accent block
```scss
  --qd-accent-text: var(--qd-primary);   /* AA-safe accent-emphasis text on light surfaces (navy) */
```

### 1b. `src/styles/_themes.scss` — add to `[data-theme='dark']`, in the accent block
```scss
  --qd-accent-text: var(--qd-accent);    /* in dark, gold reads fine on dark surfaces */
```

### 1c. `src/app/core/layout/top-navbar/top-navbar.component.scss`
Replace the four `color: var(--qd-accent);` usages on active/hover states with
`var(--qd-accent-text)`. Leave the background-color lines unchanged.

```scss
.nav-link.active {
  background-color: var(--qd-accent-tint);
  color: var(--qd-accent-text);   /* was --qd-accent */
}

.nav-link:hover {
  background-color: var(--qd-section-bg);
  color: var(--qd-accent-text);   /* was --qd-accent */
}

.dropdown-link.active {
  color: var(--qd-accent-text);   /* was --qd-accent */
}

.mobile-link.active {
  color: var(--qd-accent-text);   /* was --qd-accent */
}
```

> Do **not** change `.dropdown-link:hover` / `.mobile-link:hover` (they only set
> background). Do **not** change the brand color (`.brand .qd-page-title` stays
> `--qd-primary`).

> Optional (not required): to keep a visible gold cue on the active item in light mode,
> add `box-shadow: inset 0 -2px 0 var(--qd-accent);` to `.nav-link.active`. Skip if you
> prefer the cleaner tint-pill look.

---

## Fix 2 (MINOR) — Accessible primary-button hover

### 2a. `src/styles/_tokens.scss` — add to `:root` (light), in the primary block
```scss
  --qd-primary-hover: oklch(0.236 0.044 254.8);   /* deeper navy #0F1F33 */
```

### 2b. `src/styles/_themes.scss` — add to `[data-theme='dark']`, in the primary block
```scss
  --qd-primary-hover: var(--qd-accent-hover);     /* gold lightens on hover in dark */
```

### 2c. `src/styles/_components.scss` — `.qd-btn-primary:hover`
```scss
  &:hover {
    background: var(--qd-primary-hover);     /* was --qd-accent-hover */
    border-color: var(--qd-primary-hover);   /* was --qd-accent-hover */
  }
```

> This makes the light primary button deepen its navy on hover (parchment text →
> 15.9:1) instead of flipping to gold (which gave parchment-on-gold = 3.02:1). In dark
> the gold button lightens (dark text → 10.5:1). If you specifically want the navy→gold
> hover flip, the alternative is to switch the hover **text** to a dark color; the
> navy-deepen above is the simpler AA-safe choice and is what the updated spec uses.

---

## Verify

```bash
cd Frontend/quran-dashboard-ui
npm run build                 # must pass
git diff --stat               # expect only: _tokens.scss, _themes.scss,
                              # _components.scss, top-navbar.component.scss
grep -rn "color: var(--qd-accent)\b" src/app/core/layout/top-navbar  # expect: none on active/hover
```

Then eyeball both themes: the active nav item should read clearly (navy label on the
gold-tint pill in light; gold label on the dark pill in dark), and the primary button
hover should stay readable.

### Resulting contrast (for reference)
| Pair | Before | After |
|------|--------|-------|
| Light active nav text | 2.24:1 ❌ | 13.7:1 ✅ |
| Light hover nav text | 2.21:1 ❌ | 13.5:1 ✅ |
| Light dropdown/mobile active | ~2.5:1 ❌ | 15.4:1 ✅ |
| Light primary btn hover | 3.02:1 ⚠ | 15.9:1 ✅ |
| Dark active nav text | 7.75:1 ✅ | 7.75:1 ✅ (unchanged) |
| Dark primary btn hover | (gold) | 10.5:1 ✅ |

---

## Note for later phases
The same trap applies in **Phase 4**: `.qd-btn-soft` (gold text on accent-tint) and any
`.qd-is-selected` text would fail the same way in light. Those should use
`var(--qd-accent-text)` too — already reflected in the updated Phase 3-4-5 plan.
