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

## T605 e2e evidence

pending — phase 6

## T801 post-change verification

pending — phase 8

## T802 grep sweep

pending — phase 8
