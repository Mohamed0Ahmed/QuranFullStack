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

## T801 post-change verification

pending — phase 8

## T802 grep sweep

pending — phase 8
