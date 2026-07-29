# Slice A — evidence

Baseline and later phase evidence for `docs/feature-ux-slice-a/plan.md`. Recorded, not
inferred, per `TESTING_STRATEGY.md` §2/§9: every number below is a fresh command run
observed in this session.

## T101 — pre-change baseline

**Measured:** 2026-07-30 (session-local date; `date -u` at run start read
`2026-07-29T23:00:35Z`, a UTC-offset artifact of the same moment).

**Commit SHA measured at:** `9658a58538aa518475a3ba06ef6cc05403a10b68`
(branch `ux-audit-slice-a`, off `dev`, working tree clean at measurement time).

### Commands run

```bash
cd Frontend/quran-dashboard-ui
npm test
npm run build
```

(output captured via `tee`/`tail` for this record; the commands themselves ran
unmodified.) `npm test` preserves the `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` fork cap
baked into the script (`package.json`'s `test` entry:
`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2 ng test`) — not overridden, not bypassed.

### Vitest suite result

- Test files: **191 passed (191)**
- Tests: **2161 passed (2161)**
- Failed: **0**
- Skipped: **0**
- Duration (Vitest-reported): **186.93 s** (transform 6.25s, setup 76.09s, collect 14.47s,
  tests 55.88s, environment 171.24s, prepare 17.20s)
- Wall-clock (`time`): **3 m 31.54 s** (211.54 s)

This matches `TESTING_STRATEGY.md`'s last-recorded Frontend baseline (191 spec files /
2,161 tests / 0 failures) — no drift since the `abwab-templates` Slice B review-fix
measurement.

### Build result

- Result: **success** — `Application bundle generation complete.`
- Duration (Angular-reported): **18.120 s**
- Wall-clock (`time`): **19.287 s**
- Pre-existing budget warnings only (not errors, not new): initial bundle over the
  500.00 kB budget by 67.13 kB; three component SCSS files over the 4.00 kB budget
  (`selected-word-section`, `selected-ayah-section`, `abwab-relations-modal`). No build
  errors.

Both green. This is the baseline every later "no regression" claim in this slice is
measured against (§7 of the plan; there is no CI to fall back on, `TESTING_STRATEGY.md`
§8).

## T203 decision

**T203 is IN SCOPE for Slice A. Shape (a) chosen by the user on 2026-07-30 — lower
`.dropdown-menu` / `.mobile-menu` beneath `--qd-z-modal-backdrop`. Shape (b) (suppressing
navbar menus while a modal is open) stays deferred to Slice B because
`ScrollLockService.lockCount` is private with no observable (plan §5.4 / T203).** This
satisfies plan §9's "T203 either done or explicitly deferred in writing".

## T401 block-size

**User decided 2026-07-30 that the in-browser measurement against the relations modal is
performed by the orchestrator at phase 4, not guessed.**

## Phase 2 verification

**Measured:** 2026-07-30, branch `ux-audit-slice-a`, immediately after the phase 2 edits
(T201–T204), before commit.

### Commands run

```bash
cd Frontend/quran-dashboard-ui
npm test
npm run build
```

`npm test` ran unmodified — the `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap baked into
`package.json`'s `test` script was not overridden or bypassed.

### Vitest suite result

- Test files: **191 passed (191)**
- Tests: **2161 passed (2161)**
- Failed: **0**, Skipped: **0**
- Duration (Vitest-reported): **173.06 s**

Identical file/test counts to T101's baseline (191 / 2161), **+0** as the plan predicted —
this phase adds no tests (plan §3, §7).

### Build result

- Result: **success** — `Application bundle generation complete.` (14.609 s)
- Same three pre-existing SCSS-budget warnings as baseline
  (`selected-word-section`, `selected-ayah-section`, `abwab-relations-modal`), plus the
  same initial-bundle-over-budget warning, now **567.39 kB** vs baseline's 567.something
  (over budget by 67.39 kB vs baseline's 67.13 kB) — a ~0.3 kB drift from the new
  `--qd-z-*` custom-property declarations and doc comments in `_tokens.scss`, not a
  regression. No build errors.

### Remaining bare `z-index` literals (unfiltered grep, `src/`)

```
$ grep -rn "z-index" src/
src/styles/README.md:19:  stacking `z-index` in the app is one of these rungs; never write a bare `z-index`.
src/styles/_components.scss:546:  z-index: var(--qd-z-modal-backdrop);
src/styles/_tokens.scss:151-152: (comment text mentioning "z-index", not a declaration)
src/app/core/layout/top-navbar/top-navbar.component.scss:65:  z-index: var(--qd-z-mobile-nav);
src/app/core/layout/top-navbar/top-navbar.component.scss:139:  z-index: var(--qd-z-mobile-nav);
src/app/shared/ui/detail-modal-shell/detail-modal-shell.component.scss:103:  z-index: var(--qd-z-floating);
src/app/features/mushaf/components/surah-jump-picker/surah-jump-picker.component.scss:57:    z-index: var(--qd-z-popover);
src/app/features/mushaf/components/source-selector/source-selector.component.scss:89:    z-index: var(--qd-z-popover);
src/app/features/mushaf/components/mushaf-header-navigation/mushaf-header-navigation.component.scss:7:  z-index: var(--qd-z-sticky);
src/app/features/abwab/pages/abwab-templates-page/abwab-templates-page.component.scss:146:  z-index: 49;
src/app/features/abwab/pages/abwab-templates-page/abwab-templates-page.component.scss:152:  z-index: 50;
src/app/features/abwab/pages/abwab-page/abwab-page.component.scss:88:  z-index: 49;
src/app/features/abwab/pages/abwab-page/abwab-page.component.scss:94:  z-index: 50;
src/app/features/words/components/explorer-association-filter/explorer-association-filter.component.scss:71:    z-index: var(--qd-z-popover);
```

**Exactly four bare numeric `z-index` literals remain** — the abwab context-menu pair in
both pages (`abwab-page.component.scss:88,94`, `abwab-templates-page.component.scss:146,152`),
deliberately excluded per plan §6 phase 2 (T602/T603 delete these exact blocks in phase 6).
Every other declaration in the app now resolves through a `--qd-z-*` token.

### Final rung → value table (`_tokens.scss`)

| Token | Value | Consumer(s) | Note |
|---|---|---|---|
| `--qd-z-sticky` | 5 | `mushaf-header-navigation.component.scss:7` | unchanged from today; reserved for Slice B's sticky navbar too |
| `--qd-z-popover` | 30 | `source-selector.component.scss:89` (was 20), `surah-jump-picker.component.scss:57`, `explorer-association-filter.component.scss:71` | collapses two prior literal values (20, 30) onto one rung — nothing else in the app occupies 21–29, so no reordering |
| `--qd-z-floating` | 40 | `detail-modal-shell.component.scss:103` (the restore control) | unchanged |
| `--qd-z-mobile-nav` | 45 | `top-navbar.component.scss:65` (`.dropdown-menu`, was 100), `top-navbar.component.scss:139` (`.mobile-menu`, was 200) | **T203's fix** — both navbar layers lowered onto one shared rung, now below every modal layer. Held above `--qd-z-floating` (40) and `--qd-z-popover` (30) deliberately, so the fix does not create a new defect against those coexisting layers |
| `--qd-z-menu-backdrop` | 49 | none yet (phase 6 consumes) | matches today's abwab context-menu-backdrop literal |
| `--qd-z-menu` | 50 | none yet (phase 6 consumes) | matches today's abwab context-menu literal; equals `--qd-z-modal-backdrop` — an inherited collision, not introduced here |
| `--qd-z-modal-backdrop` | 50 | `_components.scss:546` (`.qd-modal-backdrop`) | unchanged |
| `--qd-z-modal` | 51 | none | reserved; `.qd-modal` has no explicit `z-index` today (paints via DOM order inside its backdrop's stacking context) |

### T203 before/after

- `.dropdown-menu` (`top-navbar.component.scss:65`): `z-index: 100` → `z-index: var(--qd-z-mobile-nav)` = **45**.
- `.mobile-menu` (`top-navbar.component.scss:139`): `z-index: 200` → `z-index: var(--qd-z-mobile-nav)` = **45**.
- Both now sit below `--qd-z-modal-backdrop` (50), fixing §5.4's defect: an open abwab
  modal's backdrop now paints above the navbar's dropdown/mobile menu instead of under it.
  Shape (a) only, as decided; shape (b) (suppressing the menus while a modal is open)
  remains deferred to Slice B per the existing T203 decision record above, which was
  checked against what shipped and needed no correction.

### `--qd-navbar-block-size` verification (T204)

Confirmed `--qd-navbar-block-size: 3.5rem` at `_tokens.scss:76` before substituting.
The hard-coded `3.5rem` was found at `detail-modal-shell.component.scss:101` (plan
text says `:98`; line drift from comments added since the plan was written — the
selector and value matched exactly, so the substitution proceeded there instead):

```
inset-block-start: calc(var(--qd-space-4) + 3.5rem);
```
→
```
inset-block-start: calc(var(--qd-space-4) + var(--qd-navbar-block-size));
```

## Phase 3 verification

**Measured:** 2026-07-30, branch `ux-audit-slice-a`, immediately after the phase 3 edits
(T301–T303), before commit.

### Chosen `--qd-checkbox-size` value and reasoning

**`--qd-checkbox-size: 0.9375rem`** (`_tokens.scss`, beside the `--qd-control-block-size`
family). Reconciles the three competing numbers named in plan §5.5:

- The approved concept, `abwab-relations-concept.html:84` — `.pick-row input{width:15px;
  height:15px; accent-color:var(--qd-primary);}` — **approved contract**, dropped by the
  shipped implementation.
- `abwab-template-copy-modal.component.scss:75-76` — re-checked directly: this `1.1rem` is
  set on `.abwab-template-copy-modal__pick-chevron` (the row's expand-icon), **not** on the
  checkbox itself — `.abwab-template-copy-modal__pick-row input[type='checkbox']`
  (`:93-96`) sets `flex: none` and `accent-color` only, no explicit size (native default).
  So the plan's §5.5 line reference names the row's other icon, not a second checkbox-size
  source; the concept's `15px` is the only real prior sizing intent.
- The plan's own suggestion of "~1rem".

**Consequence for Slice C, flagged so it is not read as a regression:** since none of the
four shipped checkboxes has an explicit size today (native browser default), applying
`.qd-checkbox` in Slice C will make all four **slightly larger** (native default is
typically ~13px in Chromium) — this is audit item 16's intended fix, not a side effect to
undo.

**Chosen: `0.9375rem`, not `1rem` and not a raw `15px`.** The app's root font-size is
unscaled 16px (no override in `styles.scss`/`_typography.scss`), so `0.9375rem` equals
`15px` **exactly** — the concept's value is about *scale*, not a mandate to write `px` in
a `rem`-based system, so reaching it through rem satisfies the concept's intent without
introducing a unit exception. `0.9375rem` is also not a new number: it is the same step as
`--qd-btn-font-size` (`_tokens.scss:132`) and `.qd-input`/`.qd-select`'s `font-size`
(`_forms.scss:10,56`), and backs `.qd-text-muted` (`_typography.scss:114`) — 28 existing
occurrences in `src/**/*.scss`. Tokenizing it under this name means the value cannot drift
again per call-site.

### `--qd-accent` theme verification

Confirmed defined per theme, not light-only: light in `_tokens.scss:31`
(`oklch(0.490 0.068 176.3)`, scholarly green) and overridden for dark in `_themes.scss:29`
(`oklch(0.772 0.098 82.0)`, interim gold). `accent-color: var(--qd-accent)` on
`.qd-checkbox` therefore resolves correctly in both themes with **zero `_themes.scss`
change** in this phase, as the plan anticipated.

### Selectors added

`src/styles/_forms.scss` (loaded after `_components.scss`, before `_utilities.scss`, per
the documented import order — unchanged in this phase):

```scss
.qd-checkbox {
  inline-size: var(--qd-checkbox-size);
  block-size: var(--qd-checkbox-size);
  flex: none;
  margin: 0;
  accent-color: var(--qd-accent);

  &:focus-visible {
    outline: 2px solid var(--qd-focus-ring);
    outline-offset: 2px;
  }
}

.qd-check-row {
  display: flex;
  align-items: center;
  gap: var(--qd-space-2);
}
```

The `:focus-visible` ring (`outline: 2px solid var(--qd-focus-ring); outline-offset: 2px`)
is copied verbatim from the app's existing small-discrete-control recipe shared by
`.qd-interactive-surface`, `.qd-tabs__tab`, and `.qd-chip` (`_components.scss:21-24,
191-194, 255-258`) — not invented.

### §17 entry location

`.architecture/UI_STYLE_SYSTEM.md` §17, new `### .qd-checkbox / .qd-check-row` entry
appended after the existing `### Loading/skeleton system` entry (the section's last entry
before this phase). States the geometry/color contract, the zero-new-hue theme
correctness, and the accessible-name obligation as contract — including the known debt.

**Correction from the plan's §5.6/T302 text:** re-checked all four existing checkbox
call-sites directly rather than trusting the plan's "two of the four have neither" count.
**Three of the four** have neither a `<label for>` nor an `aria-label`:
`abwab-tree.component.html:20-26`, `abwab-cards.component.html:37-43`, and
`abwab-relations-modal.component.html:145-151`. Only
`abwab-template-copy-modal.component.html:62-68` supplies `[attr.aria-label]="row.node.name"`.
The §17 entry states three, not two, so the contract names the actual debt rather than the
plan's estimate.

### Zero-consumers grep (proves no visual change to shipped surfaces)

```
$ grep -rn "qd-checkbox\b" src/ --include="*.html" --include="*.ts" --include="*.scss" | grep -v "^src/styles/_forms.scss"
src/styles/_tokens.scss:151:  /* `.qd-checkbox`'s fixed square box (`_forms.scss`). ...
src/styles/_tokens.scss:159:  --qd-checkbox-size: 0.9375rem;

$ grep -rn "qd-check-row\b" src/ --include="*.html" --include="*.ts" --include="*.scss" | grep -v "^src/styles/_forms.scss"
(no output)
```

The only hits for `qd-checkbox` outside `_forms.scss` are the `_tokens.scss` comment and
the token declaration itself — not a class reference. `qd-check-row` has zero hits anywhere
outside its own definition. **No component HTML/TS/SCSS file consumes either class**, and
none was edited this phase — confirmed by `git status --porcelain` showing exactly the four
files in scope (`UI_STYLE_SYSTEM.md`, `src/styles/README.md`, `src/styles/_forms.scss`,
`src/styles/_tokens.scss`) before this evidence file was added.

### Commands run

```bash
cd Frontend/quran-dashboard-ui
npm test
npm run build
```

`npm test` ran unmodified — the `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap baked into
`package.json`'s `test` script was not overridden or bypassed.

### Vitest suite result

- Test files: **191 passed (191)**
- Tests: **2161 passed (2161)**
- Failed: **0**, Skipped: **0**
- Duration (Vitest-reported): **165.36 s**

Identical file/test counts to T101's baseline and phase 2's (191 / 2161), **+0** as the
plan predicted (plan §3, §7 — no new tests until phase 5's T502).

### Build result

- Result: **success** — `Application bundle generation complete.` (14.388 s)
- Same three pre-existing SCSS-budget warnings as baseline/phase 2
  (`selected-ayah-section`, `abwab-relations-modal`, `selected-word-section`), plus the
  same initial-bundle-over-budget warning, now **567.71 kB** (over budget by 67.71 kB, vs
  phase 2's 567.39 kB / 67.39 kB) — a further ~0.3 kB drift from the new `--qd-checkbox-size`
  token and `.qd-checkbox`/`.qd-check-row` rules, not a regression. No build errors.

## T605 e2e evidence

pending — phase 6

## T801 post-change verification

pending — phase 8

## T802 grep sweep

pending — phase 8
