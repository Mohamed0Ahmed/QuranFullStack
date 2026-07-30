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

**Measured 2026-07-30** via a temporary Playwright harness against the real app
(Chromium, 1440×900, root font-size 16px), against the relations modal — the
tallest abwab dialog — with comprehensiveness type selected (so the direction
row renders) and the picker populated with 12 candidate doors:

- Viewport 900px → `92dvh` = 828px.
- Relations modal computed `block-size: 662.16px` (= **41.4rem**), with
  `max-block-size: none` and `overflow-y: visible` — confirming plan §5.1's
  claim that the bare `.qd-modal` base has neither a block-size nor a
  scroller.
- Modal `padding: 24px` (`--qd-space-5`), matching §5.3.
- Fixed-chrome parts, measured: `h3` 24px · `.abwab-relations-modal__desc`
  19px · `__divider` 1px · `__add-title` 20px · `__types` row 40px ·
  `__direction` row 73px · search input 38px · `__selected` 20px · foot 41px.
  Sum 276px + 48px padding = 324px ≈ **20.25rem** of non-scrolling chrome
  before inter-element gaps.
- `.abwab-relations-modal__pick-list` rendered 176px against a natural
  `scrollHeight` of 798px — i.e. today's 11rem inner cap
  (`abwab-relations-modal.component.scss:221`) is doing the scrolling that
  plan §5.2 assigns to Slice C.
- The measured door had zero relations, so the four relation-chip groups did
  not render; four groups add roughly 220px, putting the fully populated
  dialog near 880px ≈ 55rem.

**Conclusion: `<N>` = `44rem`.** 41.4rem (the un-populated natural height)
already fits inside 44rem, so the common case does not scroll at all; the
populated case exceeds it and scrolls in the body, which is the point of a
fixed single-scroller geometry; and 44rem is `qd-detail-modal-shell`'s
existing value, so the app converges on **one** modal height instead of
gaining a fourth geometry. At a 900px viewport `min(92dvh, 44rem)` resolves to
44rem (704px), so the rem term governs on desktop.

**Observation outside Slice A's scope, not a defect this slice fixes:**
relations POSTed directly to `POST /api/abwab/doors/{id}/relations` did not
appear in the relations modal's read on a subsequent page load (count badge
stayed `0`, empty state rendered). May be a genuine read/cache issue or a
harness error; it is unrelated to any Slice A change and belongs to whoever
owns abwab relations next.

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

## Phase 4 verification

**Measured:** 2026-07-30, branch `ux-audit-slice-a`, immediately after the phase 4 edits
(T401–T403), before commit.

### Selectors added (`src/styles/_components.scss`, immediately after the `.qd-modal`
base rule, before `.qd-skeleton`)

```scss
.qd-modal--fixed {
  display: flex;
  flex-direction: column;
  block-size: min(92dvh, 44rem);
  padding: 0;
  overflow: hidden;
}

.qd-modal__head,
.qd-modal__foot {
  flex-shrink: 0;
  padding: var(--qd-space-5);
}

.qd-modal__body {
  flex: 1;
  min-block-size: 0;
  overflow-y: auto;
  scrollbar-gutter: stable;
  padding-inline: var(--qd-space-5);
}

@media (max-width: bp.$qd-bp-phone-max) {
  .qd-modal--fixed {
    block-size: min(94dvh, 44rem);
  }

  .qd-modal__head,
  .qd-modal__foot {
    padding: var(--qd-space-3);
  }

  .qd-modal__body {
    padding-inline: var(--qd-space-3);
  }
}
```

`44rem` is the T401-measured value (see above). Head/foot padding uses `--qd-space-5` —
the same step the base `.qd-modal` padding uses — per plan §5.3/T401; body carries only
`padding-inline` (block spacing comes from the head/foot's own padding, avoiding a doubled
gap at the head/body and body/foot seams). Phone override mirrors
`detail-modal-shell.component.scss:134-151`'s own phone rule (94dvh bump, tightened
padding step).

### `.qd-modal` base rule — unchanged, proven

Before (still current, `_components.scss:554-564`):

```scss
.qd-modal {
  background: var(--qd-surface);
  border: 1px solid var(--qd-border);
  border-radius: var(--qd-radius-lg);
  box-shadow: var(--qd-shadow-lg);
  padding: var(--qd-space-5);
  width: min(100%, 36rem);
}
```

`git diff` on `_components.scss` confirms the new rules are a pure insertion **after**
line 564's closing brace — no line inside the base rule (554–564) appears in the diff.
The base still has no `block-size`/`height`, matching plan §5.1/§8's concern: a
block-size on the base would apply to `.qd-modal.explorer-detail-modal` too (it sets
`max-height` but never `height`/`block-size`) and clamp the five shipped words modals.
`--fixed` stays an opt-in modifier, never merged into the base.

### Zero-consumers grep

```bash
$ grep -rn "qd-modal--fixed\|qd-modal__head\|qd-modal__body\|qd-modal__foot" src/ \
    --include="*.html" --include="*.ts"
(no output)

$ grep -rln "qd-modal--fixed\|qd-modal__head\|qd-modal__body\|qd-modal__foot" src/ --include="*.scss"
src/styles/_components.scss
```

No `.html`/`.ts` file references any of the four new selectors, and the only `.scss` hit
is the definition file itself. This phase is provably zero visual change — it adds a
primitive nobody composes yet (Slice C's job, plan §2).

### §17 entry location

`.architecture/UI_STYLE_SYSTEM.md` §17, new `### .qd-modal` / `.qd-modal--fixed`
entry appended after the existing `### .qd-checkbox / .qd-check-row` entry (the
section's last entry before this phase — that entry already forward-referenced "the
same specificity trap §17's `.qd-modal` entry names for the modal geometry work",
confirming this is where the next agent expected it). States: the base stays
width-only and scroller-less; `--fixed` carries the fixed-block-size rule (`dvh`, never
`vh`; `44rem`, matching `qd-detail-modal-shell`); the head/body/foot slot contract; the
opt-in-not-base rationale with the `explorer-detail-modal` collision risk; the
composing specificity trap; **and the required convergence trigger** — the next change
touching any of the five words detail modals' geometry converges all five onto
`--fixed` and deletes the `vh` hold-out; and that this phase ships zero consumers.

### `styles/README.md`

`_components.scss` bullet now names `.qd-modal--fixed` and its `__head`/`__body`/`__foot`
slots, and states the base stays width-only/scroller-less so the modifier is composed
rather than a call-site adding its own block-size.

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
- Duration (Vitest-reported): **168.21 s**

Identical file/test counts to T101's baseline and phases 2/3's (191 / 2161), **+0** as
the plan predicted — this phase adds no tests, and no existing spec asserts `.qd-modal`'s
computed geometry (plan §5.6), so the unchanged count also confirms the base rule was not
touched.

### Build result

- Result: **success** — `Application bundle generation complete.` (14.844 s)
- Same three pre-existing SCSS-budget warnings as baseline/phase 2/phase 3
  (`abwab-relations-modal`, `selected-word-section`, `selected-ayah-section`), plus the
  same initial-bundle-over-budget warning, now **568.18 kB** (over budget by 68.18 kB, vs
  phase 3's 567.71 kB / 67.71 kB) — a further ~0.47 kB drift from the new
  `.qd-modal--fixed` rule block, not a regression. No build errors.

## Phase 5 verification

### `reserve` input — signature and default

`src/app/shared/ui/state/state.component.ts`:

```ts
readonly reserve = input(false);
```

Additive, `boolean`, default `false`. No existing `qd-state` call-site sets it, so all
seven keep today's rendered output.

### Reserved-box token

`min-block-size: var(--qd-control-block-size)` — the existing shared control-geometry
token (`_tokens.scss`, the same family `--qd-pagination-slot-block-size`,
`.qd-checkbox`'s `--qd-checkbox-size`, and `.qd-modal--fixed` already draw from). This
token already fit the "one reserved control row" shape `qd-state`'s box needs, so **no
new token was added** to `_tokens.scss` — `styles/README.md`'s "size a new reserved slot
from these tokens; never re-measure the control by hand" rule was satisfied by reuse,
not extension.

**Placement correction made during review (advisor catch, before commit):** the first
draft put `min-block-size` on the container div (`.qd-state--reserve`). That is a no-op —
`.qd-empty-state`/`.qd-loading-state`/`.qd-error-state` (`_components.scss:522-533`)
already carry `padding: var(--qd-space-6)` (2rem) on both block edges, 4rem total, which
alone exceeds `--qd-control-block-size` (≈2.5rem), so the container is never at risk of
being shorter than the reservation regardless of message content — the rule would apply
but never bind. It is sized correctly on `.qd-state__message` (the span) instead, per
the fix below.

### Markup/CSS change (`state.component.html` / `.scss`)

- Each of the three `@switch` branches gains `[class.qd-state--reserve]="reserve()"` on
  its container div (present only when `reserve()` is true — no class when false, so the
  seven existing call-sites render byte-identical markup).
- The message, previously interpolated directly (`<span>{{ message() }}</span>`), is now
  wrapped `<span class="qd-state__message" [class.qd-state__message--visible]="message()
  !== ''">…</span>` in all three branches, unconditionally. This is inert without the
  `.qd-state--reserve` ancestor class — no CSS rule in `state.component.scss` matches
  `.qd-state__message` alone, so opacity stays at the browser default (1) exactly as
  before for all seven untouched call-sites; `textContent`, `role`, and interactive
  elements are unaffected (verified by the seven pre-existing tests staying green
  unmodified).
- `.qd-state--reserve .qd-state__message` carries `display: block` (set explicitly
  rather than relied on via the parent's flex blockification, so the reservation still
  holds if the container's layout ever changes) + `min-block-size: var(--qd-control-block-size)`,
  starting at `opacity: 0` and transitioning to `1` via `.qd-state__message--visible`
  (`transition: opacity var(--qd-t-fast)`) — opacity only, no translate/height animation.
  `@media (prefers-reduced-motion: reduce)` zeroes the transition, mirroring
  `detail-modal-shell.component.scss`'s reduced-motion block for its count span.
- Precedents copied verbatim: `explorer-result-count.component.html` (all three states
  render the same one-line box shape) and `detail-modal-shell.component.html:37-44` +
  `.scss:58-71` (box always rendered, reserved `min-inline-size`, text-only opacity fade,
  `--visible` modifier-class naming) — including the detail here that the count span's
  `min-inline-size` also lands on the child element (a flex item of its header), not the
  container, which is what this correction converges onto.

### `state.component.spec.ts` — new assertions (T502)

Three new tests appended (file not created — extends the existing spec, `+0` files):

1. `reserve` off leaves no `.qd-state--reserve` in the DOM (regression guard for the
   seven existing call-sites).
2. `reserve` on keeps the box (`data-testid="qd-state-empty"`) mounted with the reserved
   class even when `message` is `''`.
3. `reserve` on toggles `.qd-state__message--visible` off for an empty message and on
   once the message is non-empty.

Test count: **13** in this file (10 baseline + 3), consistent with the plan's "+2 to +3"
budget at the top of that range.

### Docs (T503)

- `.architecture/UI_STYLE_SYSTEM.md` §17's `qd-state` entry gains a `reserve` bullet
  stating the input, default, the `--qd-control-block-size` token reuse, the opacity-only
  fade, and explicitly cross-referencing "the Loading/skeleton system entry above" for
  the §N3 no-layout-shift doctrine **instead of restating it** — the two cannot drift
  because N3's text lives in exactly one place.
- `src/app/shared/README.md`'s `ui/state/` bullet now names the `reserve` input, its
  default-off state, and that no current call-site turns it on.

### Call-sites-untouched grep

```bash
grep -rn "qd-state" src/app --include="*.html" | grep -v "shared/ui/state/state.component.html"
```

→ seven matches, all bare `<qd-state …>` usages with no `reserve`/`[reserve]` attribute
(`word-drilldown-modal`, the four `*-detail-overlay-adapter` components, and
`auth-callback` ×2). A follow-up `grep -A5 "qd-state" … | grep -i reserve` (scoped to the
call-sites, excluding the component itself) returned **no matches** — confirming none of
the seven passes `reserve`.

### Commands run

```bash
cd Frontend/quran-dashboard-ui
npm test
npm run build
```

`npm test` ran unmodified — the `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap baked into
`package.json`'s `test` script was not overridden or bypassed.

### Vitest suite result

- Test files: **191 passed (191)** — **+0** vs T101/phase 2–4 baseline (191)
- Tests: **2164 passed (2164)** — **+3** vs the 2161 baseline (T502's three new
  assertions; no other spec's count moved, confirming the `reserve` input did not touch
  any of the seven existing call-sites' observable behavior)
- Failed: **0**, Skipped: **0**
- Duration (Vitest-reported): **170.46 s** (final run, after the reservation-placement
  correction below)

### Build result

- Result: **success** — `Application bundle generation complete.` (14.954 s, final run)
- Same three pre-existing SCSS-budget warnings (`abwab-relations-modal`,
  `selected-word-section`, `selected-ayah-section`) and the same initial-bundle-over-budget
  warning, **568.18 kB** (over budget by 68.18 kB — unchanged from phase 4, since this
  phase's CSS lives in the component's own scoped stylesheet, not a shared global
  partial). No build errors.

## Phase 6 verification

**Measured:** 2026-07-30, branch `ux-audit-slice-a`, immediately after the phase 6 edits
(T601–T605), before commit. Parent commit `ab436c570788dc3e9c09933135cb7ce5ef113f32`.

### New primitive

`src/app/shared/ui/context-menu/` — `QdContextMenuComponent` (`qd-context-menu`),
standalone, `OnPush`:

- **Inputs:** `position: input.required<{ x: number; y: number }>()`,
  `menuTestId: input.required<string>()`, `backdropTestId: input.required<string>()`.
- **Output:** `dismissed = output<void>()`, emitted on backdrop click and on
  `@HostListener('document:keydown.escape')`.
- Renders a `position: fixed; inset: 0` transparent backdrop at
  `var(--qd-z-menu-backdrop)` and a positioned `role="menu"` box at
  `var(--qd-z-menu)` (`[style.left.px]`/`[style.top.px]` off `position()`), with
  `<ng-content />` for projected items.
- Item styling (hover, `:focus-visible` ring, `--danger` variant) lives as **global**
  classes in `src/styles/_components.scss` (`.qd-context-menu__item` /
  `.qd-context-menu__item--danger`), not in the component's own stylesheet — content
  projected via `<ng-content>` is compiled under the *consumer's* Angular emulated
  encapsulation, so a scoped rule in `context-menu.component.scss` would never reach it
  (the `.qd-tabs__tab` precedent, verified against `src/app/shared/ui/tabs/tabs.component.scss`,
  which is empty of item styling for the same reason).
- **No new spec file** — plan §3 permits new tests only in phase 5 (T502); a
  `context-menu.component.spec.ts` would have moved the file count off the target
  191/2164.

### Danger-variant divergence found and preserved (not unified)

Re-reading both pre-extraction SCSS blocks byte-for-byte found they were **not**
identical, despite the plan's premise: `abwab-page.component.scss:128-131` colored the
archive item red **only on `:hover`** (tint background + danger text, the same idiom as
`abwab-side-panel.component.scss:70-73`'s `__op--danger:hover`); the templates page's
`abwab-templates-page.component.scss:186-188` colored its delete item red **at rest,
unconditionally**, with no override on hover. Per plan §3's "no visual change to any
shipped surface" (one flagged exception, already spent on T203), both behaviors were
preserved rather than unified:

- The shared `.qd-context-menu__item--danger` (`_components.scss`) carries the
  hover-only idiom — `abwab-page` renders byte-identically to before.
- `abwab-templates-page.component.scss` keeps a 9-line page-scoped override (comment +
  rule) reproducing its own always-red-at-rest look, so its rendering is also
  unchanged. Named as a third gap in the new §17 `qd-context-menu` entry and flagged for
  reconciliation as a later slice's call, not this extraction's.

### T602 — `abwab-page` composition

- `abwab-page.component.html:243-260` (old) → composes `<qd-context-menu>` with
  `menuTestId="abwab-page-context-menu"`, `backdropTestId="abwab-page-ctx-backdrop"`,
  `[position]="overlays.contextMenuPosition()"`, `(dismissed)="overlays.closeContextMenu()"`;
  the 5 projected buttons keep their exact `data-testid`s and gain
  `class="qd-context-menu__item"` (`--danger` added on the archive item only).
- **`abwab-page.component.scss`: 48 lines deleted, 0 added** (backdrop, menu, item,
  hover, focus, danger, including the two bare `z-index: 49`/`z-index: 50` literals at
  old `:88,94`) — file went from 142 lines to 94 lines, confirmed by `git diff --stat`
  (the media query and everything before the deleted block are untouched).
- `abwab-page.component.ts` — added the `QdContextMenuComponent` import and its entry in
  the `imports` array. No other line changed.

### T603 — `abwab-templates-page` composition

- `abwab-templates-page.component.html:208-251` (old) → composes `<qd-context-menu>`
  with `menuTestId="abwab-templates-page-context-menu"`,
  `backdropTestId="abwab-templates-page-ctx-backdrop"`,
  `[position]="contextMenuPosition()"`, `(dismissed)="closeContextMenu()"`; the 4
  projected buttons (edit, add-child, delete-template, delete-node) keep their exact
  `data-testid`s and gain `class="qd-context-menu__item"` (`--danger` on both delete
  items). **The root-vs-node `@if (contextMenuIsRoot())`/`@else` swap and its
  explanatory comment stay in the page**, unmoved — page logic, not menu shell.
- **`abwab-templates-page.component.scss`: 44 lines deleted, 10 lines added**
  (backdrop, menu, item, hover, focus, danger, including the two bare `z-index: 49`/
  `z-index: 50` literals at old `:146,152`, replaced by the danger-override documented
  above), confirmed by `git diff --stat` — file went from 202 lines to 168 lines.
- `abwab-templates-page.component.ts` — added the `QdContextMenuComponent` import and
  its entry in the `imports` array. No other line changed.

### Zero bare `z-index` literals outside `_tokens.scss` — proven repo-wide

```bash
$ grep -rn "z-index" src/ | grep -v "var(--qd-z-"
src/styles/README.md:19:  stacking `z-index` in the app is one of these rungs; never write a bare `z-index`. Also
src/styles/_tokens.scss:161:  /* Layer scale (UI_STYLE_SYSTEM.md §4). Every stacking `z-index` in the app is one of
src/styles/_tokens.scss:162:     these rungs — never write a bare z-index. The four abwab context-menu literals that
```

Every remaining hit is prose (a README sentence and a `_tokens.scss` comment), not a
declaration. **Zero** bare numeric `z-index` declarations remain anywhere in `src/`
outside `_tokens.scss` itself — satisfying plan §9's checklist item, completed by this
phase as T202 (phase 2) deliberately deferred it here. The `_tokens.scss` comment
written in phase 2 (which forward-referenced "the Slice A phase that moves them onto
`--qd-z-menu-backdrop`/`--qd-z-menu`") was updated in this phase to record that the move
happened, rather than left describing a still-future step.

### Old class names — zero dangling references

```bash
$ grep -rn "abwab-page__ctx-backdrop\|abwab-page__ctx-menu\|abwab-page__ctx-item\|abwab-templates-page__ctx-backdrop\|abwab-templates-page__ctx-menu\|abwab-templates-page__ctx-item" \
    --include="*.ts" --include="*.html" --include="*.scss" --include="*.md" .
(no output)
```

Checked across `src/`, `e2e/`, `docs/`, `.architecture/` (the whole repo) — no stray
reference to any of the six deleted class names anywhere.

### No spec file edited

`git status --porcelain` before commit lists no file under `*.spec.ts` or `e2e/*.e2e.ts`
— confirmed no test assertion was touched to make the suite pass; the 4 Vitest
assertions (`abwab-page.component.spec.ts:449,453,578,593`, unmoved by this phase's
edits since the test file itself was not touched) and the e2e assertions passed
unmodified.

### Docs

- `.architecture/UI_STYLE_SYSTEM.md` §17 — new `### qd-context-menu` entry (after the
  `.qd-modal` / `.qd-modal--fixed` entry): purpose, inputs/outputs, the projected-items
  boundary, why item styling is global not scoped, the document-level Escape and its
  reason (the one additive a11y gain, called out as such), and **three** named gaps —
  no viewport clamping, no focus management into the menu, and the danger-rest-state
  divergence (with both recipes named and which page keeps the override).
- `src/app/shared/README.md` — new `ui/context-menu/` bullet naming the same contract
  points at a glance.
- `src/app/features/abwab/README.md` — the `abwab-tree` bullet's "the page shell renders
  the menu there" (previously describing the menu as page-rendered markup) corrected to
  "the page shell composes the shared `qd-context-menu` … there, projecting its own
  operation buttons in"; the `abwab-templates-page` bullet's stale "**its SCSS two lines
  over 200**" claim corrected to record the file dropping back under 200 lines once this
  phase moved the row menu off it (a factual claim this phase's own edit falsified, fixed
  in the same change per the root `CLAUDE.md`'s README-freshness rule).

### Commands run

```bash
cd Frontend/quran-dashboard-ui
npm test
npm run build
```

`npm test` ran unmodified — the `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap baked into
`package.json`'s `test` script was not overridden or bypassed.

### Vitest suite result

- Test files: **191 passed (191)** — **+0** vs phase 5's 191
- Tests: **2164 passed (2164)** — **+0** vs phase 5's 2164 (no new tests this phase, per
  plan §3/§7 — phase 5's T502 remains the only new assertions in the slice)
- Failed: **0**, Skipped: **0**
- Duration (Vitest-reported): **168.57 s**

### Build result

- Result: **success** — `Application bundle generation complete.` (15.034 s)
- Same three pre-existing SCSS-budget warnings (`selected-ayah-section`,
  `abwab-relations-modal`, `selected-word-section`) and the same initial-bundle-over-budget
  warning, now **568.69 kB** (over budget by 68.69 kB, vs phase 5's 568.18 kB / 68.18 kB)
  — a ~0.5 kB drift from the new shared component plus the global
  `.qd-context-menu__item` classes, not a regression. No build errors.

## T605 e2e evidence

**This is evidence for the context-menu extraction only — it is explicitly NOT a tier
and never a substitute for the Vitest suite or the build**
(`Frontend/quran-dashboard-ui/CLAUDE.md`; `TESTING_STRATEGY.md` §6). Restated here and
in the phase report per the plan's own instruction to state this twice.

**Measured:** 2026-07-30. Dev server (`https://localhost:4200`) and backend
(`https://localhost:5015`) were not already running; Playwright's `webServer` config
booted both (`npm run start:https` / `dotnet run …` against the already-built
`Backend/QuranDashboard.sln`) inside its own 180s/120s timeouts. Postgres was already up
(`pg_isready` → accepting connections).

### Command run

```bash
npx playwright test e2e/abwab-operations.e2e.ts e2e/abwab-url-and-a11y.e2e.ts --project=abwab --workers=1
```

### Result

**11 passed (54.5s)**, 0 failed — including the two locked assertions named in plan
§5.6/the verification instructions: `e2e/abwab-operations.e2e.ts:110-146` ("row context
menu offers exactly edit / add-child / move / relations / archive") and
`e2e/abwab-url-and-a11y.e2e.ts:149` (Shift+F10 opens `abwab-page-context-menu`). Neither
spec file was edited.

These specs write to the local dev DB through `e2e/fixtures/abwab.ts`'s sandbox and
leave archived residue behind **by design** (`features/abwab/README.md`,
`TESTING_STRATEGY.md` §6) — not a failure of this run.

Temp artifacts (`test-results/`, `playwright-report/`) were removed after the run so
nothing gets committed.

## Phase 7 verification

**Measured:** 2026-07-30, branch `ux-audit-slice-a`, immediately after the phase 7 edits
(T701–T702), before commit. Parent commit `028788d6` (phase 6).

### `.qd-truncate` — selector added (`src/styles/_utilities.scss`)

```scss
.qd-truncate {
  min-inline-size: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
```

Shaped exactly on `.qd-scroll-stable` (`_utilities.scss:54-56`, immediately above) — a
single-concern utility, no selector nesting, no `flex` declaration. It deliberately does
**not** set `flex`: the call-site owns its flex context, matching the app's existing
flexible-with-ellipsis shape (`detail-modal-shell.component.scss:28-35`'s `__title`), not
a hard column.

### `--qd-name-min-inline-size` — token added (`src/styles/_tokens.scss`), value and derivation

**Chosen: `12rem`.** No existing name-render site in the app sets a reserved minimum
today — `abwab-tree.component.scss:70-75`'s `__name` is `flex: 1` with no
`min-inline-size` (shrinks to nothing under sibling pressure), and this is exactly the
gap the audit's item 15 names as a "NEW pattern." The value is therefore derived, not
invented, from two independent real anchors:

1. **Primary anchor — the one shipped site with this exact shape, on the same abwab
   surface:** `abwab-toolbar.component.scss:13-14` — `.abwab-toolbar__search { flex: 1;
   min-inline-size: 12rem; }`. This is the only place in the codebase that already
   composes `flex: 1` with a reserved `min-inline-size` floor, which is precisely the
   shape the audit recommends for names (§15: *"implement it as a reserved minimum
   (`flex: 1; min-inline-size: <token>`) rather than a hard `inline-size`"*).
2. **Arithmetic check against `abwab-tree`'s row budget** (audit item 14's "~36rem-ish
   column"): fixed siblings in a bulk-mode row — checkbox `--qd-checkbox-size`
   (0.9375rem) + chevron (1.25rem) + order pill (~1.25rem, `min-inline-size`) + count
   (~2rem estimated) + flags (~3.2rem estimated, two chips + gap) + two row-hover
   actions (~3rem estimated) + six `--qd-space-2` (0.5rem) gaps between these elements
   (~3rem) ≈ **14.6rem of fixed siblings**, leaving roughly **21rem** for the name at
   depth 0 in a 36rem column. `12rem` fits comfortably with margin for several indent
   levels (`padding-inline-start: calc(var(--abwab-tree-depth, 0) * var(--qd-space-5) +
   var(--qd-space-2))`) before it would need to shrink below its floor.
3. **Independent corroboration, different surface:** `abwab-cards.component.scss:39`'s
   `.abwab-cards__grid { grid-template-columns: repeat(auto-fill, minmax(13rem, 1fr)); }`
   reserves a whole door **card** at a 13rem floor; minus two `--qd-space-3` (0.75rem)
   card paddings, that is a ~11.5rem content box — the same order of magnitude as
   `12rem`, arrived at independently from an unrelated layout (a card grid, not a flex
   row).

The full derivation is recorded as a block comment beside the token in `_tokens.scss`,
matching the form of the existing `--qd-checkbox-size` comment.

```scss
--qd-name-min-inline-size: 12rem;
```

### §17 entry location and how it forecloses hard-coded widths

`.architecture/UI_STYLE_SYSTEM.md` §17, new `### Truncatable entity names` entry
appended after the `### qd-context-menu` entry (the section's last entry before this
phase). It states, in this order:

- **The flexible-with-ellipsis rule as *the* rule** — cites
  `detail-modal-shell.component.scss:28-35`'s `__title` and `abwab-tree`'s own `__name`
  as the two existing precedents, and names `.qd-truncate` + optionally
  `var(--qd-name-min-inline-size)` as how a call-site composes it.
- **A hard fixed `inline-size` name column is stated as a per-surface exception, not an
  equal alternative** — a surface reaching for one must write down, at that call-site,
  why its layout cannot tolerate a shrinking name column the way every other one does.
  This is written so a later agent reading the entry cannot treat the audit's own
  escape-hatch language ("if the user wants a truly hard column, do it as a grid
  column") as a sanctioned equal option — the entry frames the grid-column route as
  something requiring its own written justification, not a co-equal choice with
  flexible-with-ellipsis.
- **The mandatory `[title]` obligation**, citing `word-type-filter.component.html:57`
  (`<span class="word-type-filter__child-label" [title]="child.label.ar">…</span>`) as
  the app's existing ellipsis+tooltip precedent, stated as a contract violation (not a
  style nit) when missing.
- **The known debt named honestly:** none of the eleven abwab name-render sites compose
  `.qd-truncate`, the token, or `[title]` yet; three of the eleven are missing the
  ellipsis half entirely and all eleven are missing `[title]`; wiring them is Slice
  C/D's job.
- **Zero consumers at ship time**, matching this phase's actual diff.

### `styles/README.md`

- `_utilities.scss` bullet now names `.qd-truncate` and points to
  `--qd-name-min-inline-size` for the reserved-minimum pairing.
- `_tokens.scss` bullet gains a clause naming `--qd-name-min-inline-size` and its §17
  cross-reference.

### Zero-consumers grep (proves no component file was touched, no visual change)

```
$ grep -rn "qd-truncate\b" src/ --include="*.html" --include="*.ts" --include="*.scss" \
    | grep -v "^src/styles/_utilities.scss"
src/styles/_tokens.scss:161:  /* `.qd-truncate`'s reserved minimum (`_utilities.scss`), for a truncatable entity-name

$ grep -rln "qd-truncate\b" src/ --include="*.html" --include="*.ts" --include="*.scss"
src/styles/_utilities.scss
src/styles/_tokens.scss

$ grep -rln "qd-name-min-inline-size\b" src/ --include="*.html" --include="*.ts" --include="*.scss"
src/styles/_tokens.scss
```

The only hit for `qd-truncate` outside its own definition file is the doc comment
beside the token in `_tokens.scss` — not a class reference. `qd-name-min-inline-size`
has exactly one hit: its own declaration. **No `.html`/`.ts` file references either
name, and no component file (`.html`/`.ts`/component `.scss`) was touched this phase** —
confirmed by `git status --porcelain` listing exactly the five files in scope
(`docs/feature-ux-slice-a/evidence.md`, `.architecture/UI_STYLE_SYSTEM.md`,
`src/styles/README.md`, `src/styles/_tokens.scss`, `src/styles/_utilities.scss`).

### Clean-code self-check

Ran against `.claude/skills/engineering-review/references/clean-code-guard/` per root
`CLAUDE.md`: two new selectors and one token, each single-concern, named for what they
are (`qd-truncate`, `qd-name-min-inline-size`), no dead code, no premature abstraction
(one utility, one token — not a component), the `12rem` value carries its derivation as
a comment rather than standing as a bare magic number, and no comment narrates WHAT the
four declarations already say.

### Commands run

```bash
cd Frontend/quran-dashboard-ui
npm test
npm run build
```

`npm test` ran unmodified — the `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap baked into
`package.json`'s `test` script was not overridden or bypassed.

### Vitest suite result

- Test files: **191 passed (191)** — **+0** vs phase 6's 191
- Tests: **2164 passed (2164)** — **+0** vs phase 6's 2164 (no new tests this phase, per
  plan §3/§7 — phase 5's T502 remains the only new assertions in the slice)
- Failed: **0**, Skipped: **0**
- Duration (Vitest-reported): **167.67 s**

### Build result

- Result: **success** — `Application bundle generation complete.` (14.708 s)
- Same three pre-existing SCSS-budget warnings (`selected-ayah-section`,
  `abwab-relations-modal`, `selected-word-section`) and the same initial-bundle-over-budget
  warning, now **568.81 kB** (over budget by 68.81 kB, vs phase 6's 568.69 kB / 68.69 kB)
  — a ~0.12 kB drift from the new `.qd-truncate` rule and the `--qd-name-min-inline-size`
  token plus its derivation comment, not a regression. No build errors.

## T801 post-change verification

**Measured:** 2026-07-30, branch `ux-audit-slice-a`, working tree clean (all seven prior
phases already committed), immediately before phase 8's doc edits.

### Commands run

```bash
cd Frontend/quran-dashboard-ui
time npm test
time npm run build
```

`npm test` ran unmodified — the `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap baked into
`package.json`'s `test` script was not overridden or bypassed (verified by reading
`package.json`'s `test` entry immediately before the run).

### Vitest suite result

- Test files: **191 passed (191)**
- Tests: **2164 passed (2164)**
- Failed: **0**, Skipped: **0**
- Duration (Vitest-reported): **167.47 s** (transform 5.44s, setup 67.80s, collect 13.07s,
  tests 50.88s, environment 151.84s, prepare 15.49s)
- Wall-clock (`time`): **3 m 7.97 s** (187.97 s)

### Delta vs T101 baseline (191 files / 2161 tests)

| | T101 baseline | T801 measured | Delta |
|---|---|---|---|
| Test files | 191 | 191 | **+0** |
| Tests | 2161 | 2164 | **+3** |

**Matches the plan's prediction exactly** (§6 phase 8: "+0 files and +2–3 tests"). The
delta is fully accounted for by phase 5's T502 (three new assertions appended to the
already-specced `state.component.spec.ts`: `reserve` off leaves no `.qd-state--reserve`
in the DOM; `reserve` on keeps the box mounted with an empty message; `reserve` on toggles
`.qd-state__message--visible` on/off with message content). No other phase added, removed,
or renamed a test file, and no other spec's test count moved between phase baselines
(phases 2, 3, 4, 6, 7 all measured 191/2161 or 191/2164 unchanged from the phase before
them — see their individual "Phase N verification" sections above). The count has been
stable at 2164 since phase 5 landed; phases 6 and 7 (the context-menu extraction and the
truncate utility) added zero test changes, consistent with plan §3's "no new tests except
phase 5" claim holding through to the end of the slice.

### Build result

- Result: **success** — `Application bundle generation complete.` (14.666 s)
- Wall-clock (`time`): **15.539 s**
- Same three pre-existing SCSS-budget warnings as every prior phase measurement
  (`selected-ayah-section`, `selected-word-section`, `abwab-relations-modal`) and the same
  initial-bundle-over-budget warning, **568.81 kB** (over budget by 68.81 kB) — byte-identical
  to phase 7's final measurement, confirming no doc-only work between phase 7 and T801 touched
  the compiled bundle. No build errors.

**Tier B satisfied**: full Frontend Vitest suite + `npm run build`, both green, fork cap
preserved, as required by plan §7 for a slice touching `shared/` + `styles/` + the app shell's
token layer.

## T802 grep sweep

**Measured:** 2026-07-30, branch `ux-audit-slice-a`, repo root
`/projects/Dashboard/App` (whole repo, not just `Frontend/`).

### 1. Deleted page-level context-menu SCSS class names

```bash
$ grep -rn "abwab-page__ctx-backdrop\|abwab-page__ctx-menu\|abwab-page__ctx-item\|abwab-templates-page__ctx-backdrop\|abwab-templates-page__ctx-menu\|abwab-templates-page__ctx-item" \
    --include="*.ts" --include="*.html" --include="*.scss" --include="*.md" .
```

**Result: zero live references.** The only hit anywhere in the repo is inside
`docs/feature-ux-slice-a/evidence.md`'s own phase 6 section, which quotes this exact grep
command as evidence the class names are gone — not a reference to the classes themselves.
Also checked `e2e/**` and `.specify/`, `.claude/` directly — zero hits in both.

### 2. Bare `z-index` literals

```bash
$ grep -rn "z-index:" Frontend/quran-dashboard-ui/src --include="*.scss" | grep -v "var(--qd-z-"
(no output)
```

**Zero bare numeric `z-index` declarations anywhere in `src/` outside `_tokens.scss`.**
(`_tokens.scss` itself was excluded from the grep since it is where the rungs are declared;
a repeat of phase 6's own repo-wide proof, re-run fresh at phase 8 rather than trusted from
the earlier phase.) The only prose hits for the bare string `z-index` (unfiltered) are
`styles/README.md:19`'s sentence stating the rule and `_tokens.scss`'s own comment — neither
is a declaration.

### 3. `.qd-modal` mentions in docs — do they need to say the base is width-only or reference `--fixed`?

```bash
$ grep -rln "qd-modal\b" docs specs .specify Frontend/quran-dashboard-ui/.architecture
docs/feature-ux-slice-a/evidence.md
docs/feature-abwab-doors/plan-slice-b.md
docs/abwab-ux-audit.md
docs/feature-ux-slice-a/plan.md
docs/feature-abwab-templates/plan.md
Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md
docs/feature-abwab-relations/plan.md
```

Checked each:

- `UI_STYLE_SYSTEM.md` — already carries the correct, current contract (phase 4's `.qd-modal`
  / `.qd-modal--fixed` §17 entry: base stays width-only/scroller-less, `--fixed` is the opt-in).
  No fix needed.
- `docs/feature-ux-slice-a/plan.md` / `evidence.md` — this slice's own planning/evidence
  record; describes the base correctly as of measurement time. No fix needed.
- `docs/abwab-ux-audit.md` — the pre-slice audit; its item 7 ("Every modal needs fixed width
  AND height with internal scroll") is the **problem statement** `.qd-modal--fixed` now
  answers. Left as historical diagnosis (matches the doc's own "read-only, working doc"
  framing) — it does not claim anything about `.qd-modal` that is now false (the base really
  is still `width: min(100%, 36rem)` with no block-size, exactly as item 7 describes).
- `docs/feature-abwab-doors/plan-slice-b.md:470`, `docs/feature-abwab-templates/plan.md:768`,
  `docs/feature-abwab-relations/plan.md:509` — each states that an abwab modal composes
  `.qd-modal`/`.qd-modal-backdrop` + `qdModalScrollLock`. Still true: none of the six abwab
  modals were converged onto `--fixed` in this slice (plan §2 explicitly defers that to
  Slice C), so these statements remain accurate. Not dangling.

### 4. Line-number citations into files this slice edited

Checked every citation in `docs/abwab-ux-audit.md` (the highest-risk document, since it
predates this slice and cites `abwab-page.component.scss`, `abwab-templates-page.component.scss`,
`detail-modal-shell.component.scss`, `_components.scss`, `_tokens.scss`, `_utilities.scss`,
`_forms.scss`, `top-navbar.component.scss` by line number) against the actual current file
content and against `git diff cbbfdbac..HEAD` hunk locations for each file, so drift is proven
rather than assumed:

- **`abwab-page.component.scss:85-103`** (audit line 201, describing "abwab's context menu
  uses 49/50") — **dangling, fixed.** That line range is the exact block T602 deleted (the
  file went from 142 to 94 lines); nothing at those lines today is the context menu. **Fixed**
  in `docs/abwab-ux-audit.md`: rewrote the sentence to say the z-index budget item is now
  done (Slice A's `--qd-z-*` scale + the menu's move into
  `shared/ui/context-menu/context-menu.component.ts`) and dropped the now-wrong line citation,
  naming instead where the old bare literals used to be (`abwab-page.component.scss:88,94`,
  `abwab-templates-page.component.scss:146,152`) as no-longer-existing.
- **`abwab-templates-page.component.html:208-251`** (audit line 840, item 21's "What exists")
  — **stale end-line, fixed.** T603 composed `qd-context-menu` in the same start position
  (`@if (contextMenuNodeId() !== null) {` is still line 208 — confirmed unaffected, the diff
  hunk starts at 206) but the block is now shorter (208–245, not 208–251) since composing the
  shared primitive takes fewer lines than the old duplicated markup. **Fixed**: citation
  corrected to `:208-245`.
- **Item 21's closure state** — the audit's "Fix / Size" bullet for item 21 described (a) the
  template-tree keyboard/right-click parity work and (b) the menu extraction as both still
  open. (b) is now done. **Fixed**: added a note under the (a)/(b) split recording "(b) done —
  Slice A" with the primitive's path and §17 entry, and "(a) remains open" for whichever slice
  takes the template-tree keyboard work (plan §2 names Slice G) — so the next agent reading
  this audit item does not re-scope already-shipped work.
- **`abwab-page.component.scss:21-23`** (tree-card min-height, audit line 137) — checked
  against current file: unchanged, still lines 21-23 exactly (the deleted block was later in
  the file, after this rule). Not dangling.
- **`abwab-page.component.scss:31-33`** (sticky side panel, audit line 197) — checked: unchanged,
  still lines 31-33 exactly, same reason. Not dangling.
- **`abwab-page.component.html`** citations (lines 2, 47, 49, 67, 95, 104, 142-195, 3-42, and
  `feature-abwab-templates/plan.md:787`'s `:12-38`) — all sit before line 241, and `git diff`
  confirms T602's edit is a same-line-count swap starting at line 243 (a `div` swapped for
  `<qd-context-menu>` with an equal net line count in that hunk region, well after any of these
  citations). Not dangling.
- **`abwab-templates-page.component.html`** citations at lines 14, 16, 20-32, 29, 59, 65,
  65-68, 75, 82 — all before line 206, where T603's diff hunk starts. Not dangling.
- **`detail-modal-shell.component.scss:28-35`** (cited from both the audit and
  `UI_STYLE_SYSTEM.md:1048`), **`:12-15, 91-93, 140`**, **`:62-63`** — T204's edit is an
  in-place same-line substitution at line ~101 (`3.5rem` → `var(--qd-navbar-block-size)`,
  `z-index: 40` → `var(--qd-z-floating)` at the restore control), confirmed via
  `git diff --stat`/hunk headers: no lines added or removed, so nothing before or after line
  101 shifted. Not dangling.
- **`_components.scss:543-546`** (`.qd-modal-backdrop`), **`:554-564`** (`.qd-modal` base),
  **`:208-222`** — the first diff hunk (`@@ -543,7 +543,7 @@`) is a same-line-count token
  substitution; the new content (`.qd-modal--fixed` etc.) is inserted starting at old line 563,
  after all three cited ranges. Not dangling.
- **`_tokens.scss:77`, `:94`, `:107`** — the phase 2/3/7 insertions all start at old line 147
  (`@@ -147,6 +147,58 @@`); every citation ≤146 is unaffected. Not dangling.
- **`_utilities.scss:54-56`** (`.qd-scroll-stable`) — the phase 7 insertion hunk
  (`@@ -54,3 +54,10 @@`) preserves the first 3 old lines verbatim before appending
  `.qd-truncate` after them; `.qd-scroll-stable` is still at 54-56. Not dangling.
- **`_forms.scss:39`** (an unrelated pre-existing engineering-review citation) — phase 3's
  insertion hunk (`@@ -84,3 +84,22 @@`) appends after line 84, well past line 39. Not dangling.
- **`top-navbar.component.scss`** — no doc cites a specific line in this file (confirmed by
  grep); T203's edit does shift lines after 60 by +2, but nothing points at them. Not dangling.
- **`mushaf-header-navigation.component.scss:7`**, **`source-selector.component.scss:89`**,
  **`surah-jump-picker.component.scss:57`**, **`explorer-association-filter.component.scss:71`**
  — all cited only from this slice's own plan/evidence tables, verified against the live files
  during phase 2 and re-confirmed unchanged since (single-line token substitutions, no line
  count change). Not dangling.
- **`styles/_layout.scss:7-11`, `:26-37`** (audit lines 133, 192) — `_layout.scss` does not
  appear in this slice's `git diff --stat` at all; confirmed via `git diff cbbfdbac..HEAD --
  src/styles/_layout.scss` (empty) and `git log -1 -- src/styles/_layout.scss` (last touched by
  an unrelated pre-slice commit, `26dcab9e`). Not dangling — file untouched by this slice.
- **`docs/engineering-review-full-project-2026-07-18.md`** — this file is itself a dated,
  point-in-time snapshot (header: "Date: 2026-07-18, Branch reviewed:
  `033-auth-roles-permissions`", frontend test counts of 1829/1832 already stale against
  today's 2164), explicitly excluded from `docs/` freshness expectations by its own
  "Excluded: `docs/` content review (stale)" line — but it was still grepped (it matched the
  per-file citation loop) and its hits into files this slice edited were adjudicated rather
  than skipped, since T802's brief is to check the whole repo:
  - **N30** (`detail-modal-shell.component.scss:101`) — **describes the exact defect T204
    fixed** (hardcoded `3.5rem` → `var(--qd-navbar-block-size)`). **Fixed**: annotated N30
    in place with a "Fixed — T204, 2026-07-30" note recording the actual resulting line, kept
    the original finding text intact (review findings are an audit trail, not live prose to
    rewrite). Also noted in the same annotation that the finding's own `_tokens.scss:78`
    citation for the token was already off by two lines **before this slice ever ran** —
    confirmed via `git show cbbfdbac:.../_tokens.scss` showing `--qd-navbar-block-size` already
    at line 76 at this slice's own baseline commit, so that particular drift predates and is
    unrelated to Slice A.
  - **M19** (`state.component.html:11`, `state.component.scss:6`) — describes a
    `qd-button`/`qd-btn` phantom-class bug. Checked against the pre-slice baseline
    (`git show cbbfdbac:.../state.component.html`): the button already read
    `class="qd-btn qd-btn-secondary qd-state__action"` at the baseline commit, **before** any
    Slice A edit — the fix this finding suggested had already landed in an earlier, unrelated
    commit. This citation was already stale when Slice A started and Slice A's own edit to
    `state.component.html` (T501, wrapping the message span) did not touch the button line.
    **Determination: out of this slice's remit** — T802 sweeps for references to things
    *this slice* moved; M19's drift predates Slice A and belongs to whoever last touched that
    button markup, not to this phase. Left unannotated (no Slice A action caused or resolves
    it).

### 5. README describing the abwab context menu as page-rendered markup

`features/abwab/README.md` already reads "the page shell composes the shared
`qd-context-menu` (`../../shared/ui/context-menu/`) there, projecting its own operation
buttons in" (line 56) and names the SCSS-block deletion at line 121 — this was fixed in phase
6 itself (T604), re-verified here rather than re-trusted: grepped for "renders the menu" and
"page-rendered markup" repo-wide, zero hits remain. Not dangling.

### Sweep verdict

**Four stale-citation candidates found in `docs/`; three fixed, one adjudicated out-of-remit:**

- `docs/abwab-ux-audit.md` audit item 6 (stale z-index citation) — **fixed**.
- `docs/abwab-ux-audit.md` audit item 21 (stale html line-range + unrecorded closure) —
  **fixed**.
- `docs/engineering-review-full-project-2026-07-18.md` N30 (describes a defect T204 already
  fixed) — **fixed** (annotated in place).
- `docs/engineering-review-full-project-2026-07-18.md` M19 (phantom-class citation) —
  **checked, determined out of this slice's remit**: the drift predates Slice A's baseline
  and no Slice A edit touched the cited line; left as-is rather than annotated, since
  annotating it would misattribute an unrelated, earlier fix to this slice.

Every other citation checked into every file this slice touched — across `docs/`,
`.architecture/`, every README, `e2e/`, `.specify/`, `.claude/` — was verified against the
live tree and against `git diff` hunk boundaries, and found accurate. No fix was made
anywhere else because none was needed.

## Phase 8 verification

**Measured:** 2026-07-30, branch `ux-audit-slice-a`. T801 (Tier B gate) and T802 (grep sweep)
both executed; three stale references found and fixed (two in `docs/abwab-ux-audit.md`, one
in `docs/engineering-review-full-project-2026-07-18.md`), one more checked and determined
out of this slice's remit (see the T802 sweep verdict above).

### §9 obligations checklist — verified against the tree, not assumed

| # | Obligation | Verdict | Evidence checked |
|---|---|---|---|
| 1 | Six primitives/rules shipped; six §17 entries written, including phase 4's `explorer-detail-modal` convergence trigger | **VERIFIED** | Counted against the task brief's own enumeration (checkbox, modal, state's `reserve`, context-menu, truncatable names, plus §4's layer-scale category) — six touch-points, all present: `grep -n "^### " UI_STYLE_SYSTEM.md` shows **four new §17 headings** (`.qd-checkbox` / `.qd-check-row`, `.qd-modal` / `.qd-modal--fixed`, `qd-context-menu`, `Truncatable entity names`); **one existing §17 entry amended** (`qd-state` gains the `reserve` bullet, read directly at line 734); **one §4 category added** (the layer-scale bullet at line 131 listing all eight `--qd-z-*` rungs and the "never write a bare `z-index`" rule). 4 + 1 + 1 = 6. Read the `.qd-modal--fixed` entry directly: it states the convergence trigger for `explorer-detail-modal` in the required wording ("the next change that touches any of the five words detail modals' geometry converges all five onto `--fixed`"). |
| 2 | `styles/README.md` amended for `_tokens.scss`, `_forms.scss`, `_components.scss`, `_utilities.scss` | **VERIFIED** | Read `styles/README.md` lines 8-46 directly: all four bullets name their new addition (layer scale + checkbox token + truncate token in `_tokens.scss`; `.qd-checkbox`/`.qd-check-row` in `_forms.scss`; `.qd-modal--fixed` + slots + `.qd-context-menu__item` in `_components.scss`; `.qd-truncate` in `_utilities.scss`). |
| 3 | `src/app/shared/README.md` amended for `ui/state/` and `ui/context-menu/` | **VERIFIED** | `grep -n "context-menu\|ui/state" shared/README.md` shows both bullets present (line 16 `ui/context-menu/`, line 38 `ui/state/`). |
| 4 | `UI_STYLE_SYSTEM.md` §4 carries the layer-scale token category | **VERIFIED** | §4 (line 131) contains the "layer scale (stacking order for every fixed/absolute layer in the app)" bullet with the full ascending rung list and the "never write a bare `z-index`" rule. |
| 5 | `features/abwab/README.md` records that both pages compose the shared menu | **VERIFIED** | Line 56: "the page shell composes the shared `qd-context-menu` (`../../shared/ui/context-menu/`) there, projecting its own operation buttons in"; line 121 records the SCSS deletion. Covers both pages (the `abwab-tree` bullet and the templates-workshop bullet). |
| 6 | Zero bare `z-index` literals remain outside `_tokens.scss` | **VERIFIED** | T802 sweep item 2, re-run fresh at phase 8: `grep -rn "z-index:" src --include="*.scss" \| grep -v "var(--qd-z-"` → no output. |
| 7 | Both duplicated context-menu SCSS blocks deleted (not just one) | **VERIFIED** | `abwab-page.component.scss` (142→94 lines, 48 deleted / 0 added, confirmed phase 6) and `abwab-templates-page.component.scss` (202→168 lines, 44 deleted / 10 added — the 10 added are the page-scoped danger-override, not a re-added menu shell, confirmed phase 6). T802's class-name grep confirms zero references to any of the six deleted BEM class names anywhere in the repo. |
| 8 | T101 and T801 evidence recorded, with the test-count delta explained | **VERIFIED** | T101 above (191/2161); T801 above (191/2164, +0/+3, explained as T502's three assertions, cross-checked against every intermediate phase measurement staying flat since phase 5). |
| 9 | T605 evidence recorded, labelled as extraction evidence and not as a tier | **VERIFIED** | "## T605 e2e evidence" section states verbatim: "This is evidence for the context-menu extraction only — it is explicitly NOT a tier and never a substitute for the Vitest suite or the build," citing both `Frontend/quran-dashboard-ui/CLAUDE.md` and `TESTING_STRATEGY.md` §6. |
| 10 | T802 grep clean — no dangling reference to anything moved | **VERIFIED, with three findings fixed and one adjudicated as out-of-remit in this phase** | See "T802 grep sweep" above: three stale references found and fixed (`docs/abwab-ux-audit.md` items 6 and 21; `docs/engineering-review-full-project-2026-07-18.md` N30), and one (`M19`) checked and determined to predate Slice A with no Slice A cause, left unannotated on that basis. Everything else checked (class names, z-index literals, `.qd-modal` mentions, README wording, every other line citation into every file this slice touched, including `_layout.scss` confirmed untouched) was already accurate. |
| 11 | T203 either done or explicitly deferred to Slice B in writing, not silently dropped | **VERIFIED — done, shape (a)** | "## T203 decision" section above: "T203 is IN SCOPE for Slice A. Shape (a) chosen by the user on 2026-07-30 — lower `.dropdown-menu` / `.mobile-menu` beneath `--qd-z-modal-backdrop`. Shape (b) ... stays deferred to Slice B." Phase 2 verification confirms the actual CSS change (`z-index: 100`→`var(--qd-z-mobile-nav)`, `z-index: 200`→ same). Not silently dropped — recorded in writing before phase 2 executed. |
| 12 | Root `CLAUDE.md` "Active Spec Kit Feature" updated at start (T102) and cleared at close, and `docs/feature-ux-slice-a/` swept per the lifecycle rule | **N/A-and-why (partial: first half VERIFIED, second half correctly NOT YET DONE)** | Root `CLAUDE.md`'s "Active Spec Kit Feature" section (read directly) lists both `abwab-templates` **and** `ux-slice-a`, satisfying T102's "updated at start" half. The "cleared at close" / "folder swept" half is explicitly **not** this phase's job: the task brief for this very phase states "this slice is not closed yet — the orchestrator and user decide that," and plan §9's own item is phrased as a close-time obligation, not a phase-8 one. Marking this NOT MET would be wrong (nothing failed); it is correctly still open, pending the orchestrator's close decision. |

**Summary: 11 of 12 items VERIFIED; item 12 is half-done by design (T102 done; the close-time half is out of this phase's authority and correctly not yet executed).** No item required new code or a call-site application to close — the three T802 findings were doc-only fixes, made in this phase per the task's instruction to fix small, in-scope gaps rather than only report them; the fourth (M19) was checked and correctly left alone since it predates and is unrelated to this slice.
