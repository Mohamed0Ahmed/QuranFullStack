# Slice B — evidence log

Evidence for the Tier B / build gates run at each phase boundary of
`docs/feature-ux-slice-b/plan.md`. Per `TESTING_STRATEGY.md` §8 there is no CI — this file is
the only record that a gate ran and what it found.

## T101 — B1 pre-change baseline

Commit SHA (branch tip, before any Slice B code edit): `8c2551e5d1e67937d706a7d25790e6b17ecdce97`
on branch `ux-slice-b1-states`, working tree clean.

Commands run from `Frontend/quran-dashboard-ui/`:

```bash
npm test
```

Result: **191 spec files passed (191) / 2164 tests passed (2164) / 0 failed.**

```
Test Files  191 passed (191)
     Tests  2164 passed (2164)
  Start at  13:58:30
  Duration  181.04s (transform 5.87s, setup 73.23s, collect 13.91s, tests 53.90s, environment 167.77s, prepare 16.35s)
```

The `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap baked into the `npm test` script was preserved —
the script was invoked as-is, no direct `ng test` call was made.

This matches the expected starting point recorded in the plan (Slice A closed at 191 files /
2164 tests) — measured here, not assumed.

```bash
npm run build
```

Result: **green.** `ng build` completed in 16.482s with output at
`dist/quran-dashboard-ui`. No errors. Four pre-existing warnings, unrelated to this
baseline run (no code was changed before this build):

- initial bundle exceeded the 500.00 kB budget by 68.81 kB (568.81 kB total)
- `abwab-relations-modal.component.scss` exceeded its 4.00 kB budget by 1.51 kB
- `selected-word-section.component.scss` exceeded its 4.00 kB budget by 649 bytes
- `selected-ayah-section.component.scss` exceeded its 4.00 kB budget by 1.85 kB

Both commands are green. This run is the only pre-change comparison point for every later
"no regression" claim in B1 and B2.

**Doc-integrity note, not fixed here (outside this task's write allowlist):**
`TESTING_STRATEGY.md` §1's table and §6's command catalog both cite the Frontend baseline as
**191 spec files / 2,161 tests / 205.45 s**, taken at the `abwab-templates` Slice B review-fix
round (2026-07-30). This T101 run measures **191 files / 2164 tests**, i.e. 3 more than that
doc records. The plan's own expectation (`docs/feature-ux-slice-b/plan.md` §6 Phase 1, citing
Slice A's close at "191 files / 2164 tests") matches this measurement exactly, so **2164 is the
correct current count** — `TESTING_STRATEGY.md`'s Frontend row was not refreshed when UX Slice A
merged (`3644b772`, which per its own commit history added tests) and is stale by 3 tests as of
this measurement. `TESTING_STRATEGY.md` is not in this task's write scope; whoever next has
write access to it should reconcile the count.

## T502 — B1 layout-stability acceptance (§7.2)

Extraction-style evidence for the layout-stability claim; not a tier and no substitute for the
Vitest suite or the build.

**Harness:** a temporary Playwright spec, `e2e/abwab-tmp-layout-stability.e2e.ts`, run as
`npx playwright test --project=abwab --workers=1 e2e/abwab-tmp-layout-stability.e2e.ts` against
a local dev server + local backend (`https://localhost:4200` / `https://localhost:5015`) and the
local `quran_dashboard` dev DB, using the `abwabSandbox` fixture (`e2e/fixtures/abwab.ts`) plus
one extra API-created "empty" section for the empty-view cells. Fixed viewport `1440x900`.
**Deleted after this run** — confirmed absent from the tree (`ls e2e/*tmp*` → no match) and no
stray dev server left on `:4200`/`:5015` afterward.

The invariant element is `.abwab-page` (the frame, `data-testid="abwab-page"`) plus
`.abwab-toolbar`. Measured via `getBoundingClientRect()` (Playwright `boundingBox()`), after
awaiting `document.fonts.ready` on every capture (the custom Arabic font is `font-display: swap`,
so the first few navigations in a fresh browser context can wrap the toolbar's section tabs onto
a second line before the font is cached — awaiting fonts on **every** capture, not just the
first, removes that ordering confound).

| Cell | frameHeight (px) | toolbarTop (px) | toolbarBottom (px) |
|---|---|---|---|
| loaded / tree / search off | 801.296875 | 192.796875 | 305.296875 |
| loaded / tree / search match | 801.296875 | 192.796875 | 305.296875 |
| loaded / tree / search no-match | 801.296875 | 192.796875 | 305.296875 |
| loaded / cards / search off | 801.296875 | 192.796875 | 305.296875 |
| loaded / cards / search match | 801.296875 | 192.796875 | 305.296875 |
| loaded / cards / search no-match | 801.296875 | 192.796875 | 305.296875 |
| loaded / archive / search off | 9460.296875 (excluded, see below) | 192.796875 | 230.296875 |
| loaded / archive / search match | 726.296875 | 192.796875 | 230.296875 |
| loaded / archive / search no-match | 726.296875 | 192.796875 | 230.296875 |
| empty / tree | 801.296875 | 192.796875 | 305.296875 |
| empty / cards | 801.296875 | 192.796875 | 305.296875 |
| loading / tree | 533.296875 | 192.796875 | 233.296875 |
| loading / cards | 533.296875 | 192.796875 | 233.296875 |
| loading / archive | 530.296875 | 192.796875 | 230.296875 |
| error / tree | 533.296875 | 192.796875 | 233.296875 |
| error / cards | 533.296875 | 192.796875 | 233.296875 |
| error / archive | 530.296875 | 192.796875 | 230.296875 |

Row height: loaded tree row (`abwab-tree__row`) = 32px; `qd-skeleton-rows__row` = 12px **as
measured**, i.e. before the `--qd-skeleton-h` fix recorded under (b) below.

**What this proves (asserted and green):**

1. **`toolbarTop` is 192.796875 in all 17 cells, with no exception.** This is T402's concrete,
   provable win: the old code (`abwab-page.component.html:47-49` pre-phase-4) swapped the
   *entire* toolbar+tree branch for a one-line paragraph on load/error, so the toolbar's position
   moved (or vanished) on every state change. It no longer does, in any of loading, loaded,
   error, or empty, for any of tree/cards/archive, with or without a search query.
2. **Within a settled view, search does not move the frame or the toolbar's own height.** Tree
   (801.3/305.3) and cards (801.3/305.3) are identical off/match/no-match; archive
   match/no-match (726.3/230.3) are identical to each other. This evidences T301/T302's
   reservations (the empty/error `qd-state` sites) and the T403 archive-confirm slot holding
   their space.
3. **`loading` and `error` occupy the same space, per view** — tree loading (533.3) = tree error
   (533.3); cards loading (533.3) = cards error (533.3); archive loading (530.3) = archive error
   (530.3). This evidences T401's reservation: the skeleton and the reserved error message take
   up the identical box.

**Excluded, not a defect:** `loaded / archive / search off` renders **every archived door in the
whole shared local dev DB** — hundreds of doors permanently archived by every prior Abwab e2e run
(`e2e/README.md`: "archived doors ... permanent, not self-cleaning"), an unbounded, ever-growing
number unrelated to this phase's reservations (it grew from 9244→9352→9460px across three runs
of this same harness, each run adding its own one archived door to the pile). Excluded from the
equality assertions for that reason; the archive view's own `qd-state` reservation is still
proven by the match/no-match pair above.

**Two genuine, out-of-T501-504-scope gaps found and recorded, not silently fixed:**

- **(a) `loaded` (801.3 / 726.3) is taller than that same view's own `loading`/`error`
  (533.3 / 530.3).** Root cause, confirmed by reading `abwab-page.component.html`: the
  `<aside>` (side panel + archive-confirm slot) only renders in the `loaded`/`empty` branches —
  the `isLoading`/`errorMessage` branches render only `.abwab-page__main`, no aside — and the
  toolbar's own section tabs are empty until `facade.snapshot()` exists (`sections()` reads
  `facade.snapshot()?.sections ?? []`). Closing this gap is item 4's viewport reservation
  (`.qd-page-frame` + `flex: 1` stretch), explicitly **B2 phase 8 (T801/T802)**, deliberately
  sequenced after B1 (plan §0). B1 does not claim this invariant; §7.2 itself schedules the
  full-matrix run a second time at **T1101**, after B2 lands, for exactly this reason.
- **(b) §4.7's "measured match" claim is falsified, not just unmet — this is a defect finding,
  not a soft gap.** Measured: skeleton row = **12px**, loaded row (`.abwab-tree__row`) = **32px**.
  `rowTemplate="1.25rem 1fr auto"` (T201) correctly matches the loaded row's **column widths**,
  but not its block-size. Two independent, verified causes, with two different owners:
  - `.qd-skeleton` (`_components.scss:645-651`) defaults to `height: var(--qd-skeleton-h,
    0.75rem)` = 12px, and abwab supplies no `--qd-skeleton-h` override anywhere. **This half is
    abwab-local and fixable inside this phase's own blast radius** — a CSS custom property
    inherits down through the DOM, so an override on `abwab-page.component.scss` (or wherever
    `<qd-skeleton-rows>` is hosted) would reach the spans inside the shared component without
    editing `shared/ui/skeleton/*` at all. `qd-skeleton-rows.component.ts` takes no `count`/
    `rowTemplate`-adjacent height input, so this is the only lever a caller has.
  - `.qd-skeleton-rows__row` (`skeleton-rows.component.scss:12-16`) sets no padding at all
    (`display: grid; gap; width: 100%`), while `.abwab-tree__row` sets
    `padding: var(--qd-space-1) var(--qd-space-2)` (`abwab-tree.component.scss:10`, 4px
    top+bottom = the remaining ~8px of the gap once the span height is fixed). **This half has
    no caller-facing lever today** — closing it needs either a shared-component change (touching
    `shared/ui/`, which mushaf and words explorers also consume) or a fragile descendant-selector
    override reaching into a child component's internal class name from abwab's own SCSS; neither
    is a call this verification-only phase should make unilaterally.
  **Resolution taken by the main thread, after the measurement above.** The first half is fixed:
  `.abwab-page__tree-skeleton { --qd-skeleton-h: 1.5rem }` in `abwab-page.component.scss`, the
  same abwab-local composition phase 2 used for the templates list. That is a **pitch** fix, and
  pitch is the right target — §17's N3 wording is *"a skeleton must occupy the box its loaded
  content will occupy"*, i.e. total occupied height, not the row element's own box in isolation.
  `.abwab-tree` is a gapless column flex (`abwab-tree.component.scss:1-4`), so its pitch is the
  row's measured 32px; `qd-skeleton-rows` puts `--qd-space-2` (0.5rem, `_tokens.scss:122`)
  between rows, so a 1.5rem bar gives 24 + 8 = **32px pitch, matching the measured loaded pitch
  exactly**. The model behind that arithmetic is not a guess: it predicted the pre-fix row at
  12px and the harness measured 12px.

  **Residual, stated rather than hidden.** `qd-skeleton-rows`' `gap` is not parameterized, so
  *n* skeleton rows come out exactly one gap (8px) short of *n* loaded rows. Exact total parity
  is unreachable through `--qd-skeleton-h` alone — that is a real limitation of the primitive,
  and it is the honest answer to §4.7's open question: **the `rowTemplate` matches the loaded
  row's column widths, never its block-size; block-size needs the `--qd-skeleton-h` override,
  and the trailing gap cannot be removed by a caller at all.** Closing the last 8px needs a
  shared-component change (parameterizing the gap), which is outside B1's `features/abwab/**`
  blast radius and is left to whoever next edits `shared/ui/skeleton/`. `.abwab-page__tree-card`
  absorbs the 8px today.

  Not re-measured in the browser after the fix — the dev server and sandbox were torn down with
  the temporary harness — so the 32px pitch is a derivation from measured inputs, not a fresh
  measurement. §7.2's second full-matrix run at **T1101** is where it gets re-measured.

  The copy modal's pick-list skeleton (T203) was **not** given the same treatment: its loaded row
  height was never browser-measured, and its scroller carries a fixed `max-block-size: 13rem`
  plus `qd-scroll-stable`, so a skeleton/loaded height difference there is bounded by the
  scroller and cannot move the modal's frame. Named so it is a known gap, not an oversight.

## T503 — B1 Tier B gate (post-phase-5)

Commands run from `Frontend/quran-dashboard-ui/`, script invoked as-is (the
`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap baked into `npm test` was preserved, no direct
`ng test` call was made):

```bash
npm test
```

Result: **191 spec files passed (191) / 2164 tests passed (2164) / 0 failed.** Run twice across
this phase (once before, once after the T504 fixes below); both green with identical counts, the
second at `Duration 202.04s`.

```
Test Files  191 passed (191)
     Tests  2164 passed (2164)
```

**Delta vs T101 (191 files / 2164 tests): +0 files, +0 tests — exactly as expected (§3: "B1 adds
no tests").** No spec broke; nothing needed fixing.

```bash
npm run build
```

Result: **green**, re-run after every phase-5 edit (T501's markup, T504's SCSS/doc fixes) per
`TESTING_STRATEGY.md` §7 — the final run is the one reported. `ng build` completed in 12.836s,
output at `dist/quran-dashboard-ui`. Same four pre-existing budget warnings as T101 (initial
bundle, `abwab-relations-modal.component.scss`, `selected-word-section.component.scss`,
`selected-ayah-section.component.scss`), no new ones — the `abwab-relations-modal.component.scss`
overage kept shrinking (1.51 kB over at T101 → 1.08 kB over now) as phase 3 deleted its
`__error`/`__empty` blocks and phase 5's T504 removed one more orphaned selector (below).

Branch tip at the start of phase 5 (before this phase's own edits): `9e255bbbd9911970fc3d7fd49b3ce40bc56c1b5c`
on `ux-slice-b1-states`. Phase 5's edits (T501, and T504's fixes) are uncommitted at the time of
this run — the main thread commits per the hard bounds in this phase's instructions.

## T504 — dangling-reference sweep

`grep -rn` across code, tests, e2e, skills, docs, READMEs, `.specify/`, `.architecture/` for
every selector/path B1 moved or deleted, plus prose (Slice A's own lesson: a grep for class
names and literals still missed a stale prose claim last time).

| # | Hit | Disposition |
|---|---|---|
| 1 | `abwab-relations-modal__error`, `abwab-template-copy-modal__error`, `abwab-sections-modal__error` class names anywhere in code/docs | **Clean.** Zero hits outside the audit/evidence historical documents (below). |
| 2 | `abwab-relations-modal__empty` still present at `abwab-relations-modal.component.scss:10`, grouped with `__desc`/`__preview`/`__selected` in a shared text-muted rule | **Fixed.** The template no longer has any element with this class (confirmed by grep on the `.html`) — an orphaned selector left behind when phase 3 (T303) deleted the standalone `__empty` block elsewhere in the same file. Removed `.abwab-relations-modal__empty` from that selector list; `__desc`/`__preview`/`__selected` (all still genuinely used) are untouched. |
| 3 | `qd-loading-state`/`qd-error-state`/`qd-empty-state` mentions in abwab docs | **Clean.** The only hits are generic architecture docs (`UI_STYLE_SYSTEM.md`, `API_INTEGRATION_GUIDELINES.md`, `shared/README.md`) describing the shared primitive itself, not abwab specifically; none describe abwab's states as hand-rolled. |
| 4 | Prose in `features/abwab/README.md` describing abwab's states | **Clean, already correct.** `README.md:289` reads *"Loading/empty/error surfaces are composed, not hand-rolled … now composes `qd-skeleton-rows`/`qd-panel-skeleton` (loading) or `qd-state` (empty/error)"* — accurate, updated by phase 3's T304. |
| 5 | `UI_STYLE_SYSTEM.md`'s `.qd-modal--fixed` entry cites `abwab-sections-modal.component.scss:14`, `abwab-template-copy-modal.component.scss:52`, `abwab-relations-modal.component.scss:221` | **Fixed (dangling).** Phase 3 deleted 9/9/17 lines respectively from these three files (confirmed via `git diff 3644b772 HEAD --stat`), shifting every later line up. Current, verified locations: `abwab-sections-modal.component.scss:5`, `abwab-template-copy-modal.component.scss:43`, `abwab-relations-modal.component.scss:203` — all three re-checked by reading the file at the new line number. |
| 6 | `docs/feature-abwab-templates/plan.md:680` cites `abwab-page.component.ts:143-145` for `ngOnInit`/`facade.load()` | **Fixed.** `abwab-page.component.ts` gained 5 lines during B1 (phase 4, T402); the real location is now `:155-156`, confirmed by reading the file. `abwab-templates` is a currently-open feature (root `CLAUDE.md` Active Spec Kit Feature), so its plan is a live document, not frozen evidence. |
| 7 | `docs/feature-abwab-templates/plan.md:780,~782,~784` cite `abwab-relations-modal.component.ts:167-197`, `:231`, `:235`, `:263`, `:265` for the picker-search/auto-expand/confirm-count logic | **Found, not fixed — flagged.** These have drifted (current equivalents are roughly `:187-235`, confirmed by locating `pickerRows`, `subtreeMatches`, the `isExpanded`/search auto-expand line, `selectedSummary`, `addCount`/`addButtonLabel`), but `abwab-relations-modal.component.ts` only changed +3/-2 lines in the whole B1 branch — this drift predates B1 (from `abwab-templates`'s own subsequent work), so it is not B1's defect to repoint, and guessing at exact corrected ranges risked introducing new wrong numbers. Left for `abwab-templates`'s own maintainers. |
| 8 | `abwab-page.component.html:12-38` cited in the same `abwab-templates` plan (T802, the «القوالب» entry) | **Clean.** Still accurate — the header block (lines 3-40 currently) is untouched by any B1 phase. |
| 9 | Every other `file:line` citation into the six B1-edited files, in `docs/abwab-ux-audit.md`, `docs/feature-ux-slice-a/{plan,evidence}.md`, `docs/feature-abwab-global-order/*` | **Left as-is, by design.** `docs/abwab-ux-audit.md` is the cross-cutting source audit multiple slices (A, B, …) execute against — root `CLAUDE.md`'s lifecycle rule names "cross-cutting audits" as evidence that is never swept, and its citations are "as observed at audit time" snapshots, same convention `docs/feature-ux-slice-a/{plan,evidence}.md` (N-2 buffer, frozen) already follow. `docs/feature-abwab-global-order/*` belongs to an already-closed feature outside this phase's scope. |
| 10 | `abwab-page.component.spec.ts:477` (`abwab-page-archive-empty` assertion) | **Verified, not dangling.** Still exactly at line 477: `expect(root.querySelector('[data-testid="abwab-page-archive-empty"]')).toBeTruthy();` — confirmed by reading the file, and it passed in T503's full run. |

## T601 — B2 re-baseline (post-B1-merge, pre-B2-code)

Branch tip (working tree clean, no B2 code edits made before this run):
`e5c7060d26b5c939875be9fd32234199370b1b67` on `ux-slice-b2-frame` (`Merge branch
'ux-slice-b1-states' into dev`). Per §7 phase 6: B1 changed abwab markup, so this run's
comparison point is B1's own closing T503 numbers (191 files / 2164 tests), not Slice A's.

Commands run from `Frontend/quran-dashboard-ui/`, script invoked as-is (the
`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap baked into `npm test` was preserved, no direct
`ng test` call was made):

```bash
npm test
```

Result: **191 spec files passed (191) / 2164 tests passed (2164) / 0 failed.**

```
Test Files  191 passed (191)
     Tests  2164 passed (2164)
   Start at  16:05:37
   Duration  181.49s (transform 5.98s, setup 73.17s, collect 14.31s, tests 56.12s, environment 165.19s, prepare 16.73s)
```

**Delta vs T503 (B1's close, 191 files / 2164 tests): +0 files, +0 tests — exact match.** No
spec drifted between the B1 merge into `dev` and this branch tip.

```bash
npm run build
```

Result: **green.** `ng build` completed in 14.875s, output at `dist/quran-dashboard-ui`. No
errors. The same four pre-existing budget warnings as T503, byte-for-byte unchanged (compared
against T503's numbers, not T101's, because T503 is the run that already reflects B1's edits to
`abwab-relations-modal.component.scss`):

- initial bundle exceeded the 500.00 kB budget by 68.81 kB (568.81 kB total) — unchanged from T101/T503
- `selected-word-section.component.scss` exceeded its 4.00 kB budget by 649 bytes — unchanged from T101/T503
- `selected-ayah-section.component.scss` exceeded its 4.00 kB budget by 1.85 kB — unchanged from T101/T503
- `abwab-relations-modal.component.scss` exceeded its 4.00 kB budget by 1.08 kB — **unchanged from
  T503** (this is the number after B1's phase 3/5 SCSS deletions shrank it from T101's 1.51 kB
  overage; no further drift since B1 closed)

No new warnings. This run is the only pre-B2-change comparison point for every later "no
regression" claim in B2; T1102 measures against these numbers, not T101's or T503's directly.

## Phase 7 (T701–T704) — the page frame, item 1

Branch `ux-slice-b2-frame`, working tree was clean at T601 (`cbebce8c`) before phase 7's edits.

### T701/T702 — rename and move

`.qd-explorer-frame` → `.qd-page-frame` with `.qd-explorer-frame` kept as an alias on the same
rule block (dual selector: `.qd-page-frame, .qd-explorer-frame { … }`), moved from
`src/styles/_words-explorer-layout.scss:53-63` to `src/styles/_layout.scss`, immediately after
`.qd-container`. `box-sizing: border-box` and every other declaration carried over unchanged.

Import order verified by reading `src/styles.scss:1-11` rather than assumed: `layout` (line 4)
loads before `words-explorer-layout` (line 6), so the rule now compiles *earlier* in the
cascade than before. Confirmed safe: `.qd-explorer-frame`/`.qd-page-frame` has exactly one
definition site in the whole codebase (`grep -rn "\.qd-explorer-frame\|\.qd-page-frame"
src/styles/ src/app/` returns only the new `_layout.scss` block plus doc prose and the five HTML
call-sites), so there is no second rule at equal specificity for the earlier position to lose
a tie-break against.

### T703 — call-sites + browser verification

Added `qd-page-frame` to `abwab-page.component.html:2` and
`abwab-templates-page.component.html:2` (`qd-container qd-page-frame`).

**Browser verification performed** (not skipped): built the backend
(`dotnet build Backend/QuranDashboard.sln`), started it (`dotnet run --no-build --urls
https://localhost:5015`, healthy — `GET /api/health` returned `status: healthy`), started the
frontend (`npm start`, `https://localhost:4200`), and drove it with the `claude-in-chrome` MCP
tool (the user's real Chrome, which trusts the already-installed mkcert root CA — the Playwright
MCP browser and the headless chrome-devtools-mcp browser both failed with
`ERR_CERT_AUTHORITY_INVALID` and were abandoned in favor of this tool). Abwab's routes are
public-read (no auth guard), so no Logto sign-in was needed to view them.

Observed via `getComputedStyle` + `getBoundingClientRect` at `/abwab` (1873px content width):

| Property | `.qd-page-frame` on `abwab-page` | `.qd-explorer-frame` on `/dashboard/words/roots` |
|---|---|---|
| `display` | `flex` | `flex` |
| `flexDirection` | `column` | `column` |
| `gap` | `0px` | `0px` |
| `boxSizing` | `border-box` | `border-box` |
| `paddingInline` | `16px` | `16px` |
| `paddingBlockEnd` | `48.8px` | `48.8px` |
| `width` | `100%` (`1873px` computed) | `100%` (`1873px` computed) |
| `maxWidth` | `none` | `none` |
| `marginInline` | `0px` | `0px` |

Identical values on both sides of the alias — the five explorer call-sites are unaffected, and
abwab's new call-site gets the same rule. `roots-explorer-page` was screenshotted before and
after the change area was touched (full-bleed table, no visual difference from the pre-Phase-7
shape); `abwab-page` and `abwab-templates-page` were screenshotted too — both render full-bleed
with no console errors (`read_console_messages`, `onlyErrors: true`, empty on all three pages).

**§5.1's two flex caveats, checked in the browser, not assumed:**

1. **Column-flex frame vs. `.abwab-page__layout`'s row.** `getComputedStyle` on
   `.abwab-page__layout` returned `display: flex`, `flexDirection: row`, `gap: 16px`,
   `marginBlockStart: 12px`. Nested inside the frame's `flexDirection: column; gap: 0`, this
   renders exactly as expected — no clipping, no gap collapse, no overlap. The frame's `gap: 0`
   does not remove spacing between the header and the layout row; `.abwab-page__layout`'s own
   `margin-block-start` supplies it, confirmed both in the computed style and visually (toolbar
   and side panel sit at the same vertical position, correctly gapped from the page header,
   across both screenshots taken at 1568×783).
2. **The frame's mobile-stat-bar `padding-block-end` (48.8px) against abwab's own bottom
   spacing.** Scrolled to the bottom of `/abwab`: there is a consistent ~49px gap between the
   last tree row / side panel content and the app footer. This is the frame's fixed
   `padding-block-end`, applied unconditionally (not media-gated), and it is **not new** —
   `getComputedStyle` on `.qd-explorer-frame` at `/dashboard/words/roots` returned the identical
   `48.8px`, i.e. the five explorer pages already carry this same bottom gap today. Abwab
   inherits the existing trait of the shared class rather than acquiring a new one. No visual
   double-gap or squeeze was observed against `.abwab-page__tree-card`'s own padding or
   `.abwab-page__side`'s `gap`.

### T704 — docs

Updated: `src/styles/README.md` (`_layout.scss` bullet documents `.qd-page-frame`, the alias,
and that new call-sites use the neutral name; `_words-explorer-layout.scss` bullet notes the
rule moved out), `.architecture/UI_STYLE_SYSTEM.md` §2 (new paragraph after the "Current state"
note), `src/app/features/words/README.md` (new paragraph in "Shared pattern" naming the rename/
move/alias and the five call-sites), `src/app/features/abwab/README.md` (new "Gotchas" bullet
recording the frame, `box-sizing: border-box` being load-bearing for the later viewport
reservation, and both browser-verified caveats above).

**Sweep for dangling references** (`grep -rn "_words-explorer-layout.scss:5[0-9]\|
_words-explorer-layout.scss:6[0-3]"` and `grep -rn "qd-explorer-frame" src/`):

| Hit | Disposition |
|---|---|
| `docs/abwab-ux-audit.md:57` cites `_words-explorer-layout.scss:53-63` | **Left as-is, by design.** Cross-cutting audit, never swept (root `CLAUDE.md` lifecycle rule; same disposition as B1's T504 sweep item 9). |
| `docs/feature-ux-slice-b/plan.md:182,273,300,507` cite the same old lines/class | **Left as-is, by design.** This plan's §5 is "measured on `dev` at plan time" — a frozen snapshot, not a live description of current code; editing it mid-execution would misrepresent when it was captured. Same convention `docs/feature-ux-slice-a/{plan,evidence}.md` already follow. |
| Every other `qd-explorer-frame` hit in `src/` | **Current, not dangling.** Only the new `_layout.scss` definition, the four just-updated READMEs, and the five untouched explorer HTML call-sites. |

### Tier B gate

Commands run from `Frontend/quran-dashboard-ui/`, `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`
preserved (baked into `npm test`, no direct `ng test` call):

```bash
npm test
```

Result: **191 spec files passed (191) / 2164 tests passed (2164) / 0 failed.** Duration 189.48s.

**Delta vs T601 (191 files / 2164 tests): +0 files, +0 tests — exact match**, as expected for a
zero-test-change phase (§3, §7.1). No spec was edited or added.

```bash
npm run build
```

Result: **green.** `ng build` completed in 16.883s, output at `dist/quran-dashboard-ui`. Same
four pre-existing budget categories as T601, with one real, explained, sub-kilobyte delta:

- initial bundle exceeded the 500.00 kB budget by **68.83 kB (568.83 kB total)** — **+20 bytes
  vs T601's 568.81 kB.** Expected: the global stylesheet now carries one extra selector string,
  `.qd-page-frame,` (15 chars plus minifier overhead), added to the dual-selector rule. Not a
  regression — it is the literal, minimal cost of the alias mechanism itself.
- `selected-word-section.component.scss` exceeded its 4.00 kB budget by 649 bytes — unchanged from T601
- `selected-ayah-section.component.scss` exceeded its 4.00 kB budget by 1.85 kB — unchanged from T601
- `abwab-relations-modal.component.scss` exceeded its 4.00 kB budget by 1.08 kB — unchanged from T601

No new warning categories. No errors.

**Obligations checked at close of phase 7:** `.qd-page-frame` shipped with `.qd-explorer-frame`
as a working alias; five explorer call-sites untouched and green (Tier B + browser); abwab
full-bleed on both pages; both §5.1 flex caveats checked in the browser and recorded above. The
viewport-reservation and `box-sizing: border-box` proof-in-diff obligations belong to phase 8,
not this phase.

## Phase 8 (T801–T803) — the viewport reservation, item 4

Branch `ux-slice-b2-frame`, working tree clean at phase 7's close (`d037b25b`) before this
phase's edits.

**Scope decision, made before writing code:** the doors page (`abwab-page.component`) only, not
the templates page. §7.2's invariant names `.abwab-page`, T802 enumerates exactly one card
(`.abwab-page__tree-card`) and one declaration to remove (`min-height: 20rem`), and the
acceptance axes (tree/cards/archive) exist only on the doors page.
`abwab-templates-page.component`'s editor panel keeps its own `min-block-size: 22rem`
(`abwab-templates-page.component.scss:94`) untouched — it is named nowhere in phase 8 and that
page's editor already has a different, unspecified stretch story from phase 2's `--loading`
flex modifier. Verified in the browser (below) that the templates page's frame carries no
reservation and is byte-for-byte unaffected.

### T801 — the reservation, and why it is not on `.qd-page-frame`

Added `.abwab-page__frame` to `abwab-page.component.html:2` (alongside the existing
`qd-container qd-page-frame`) and, in `abwab-page.component.scss`, a new rule:

```scss
.abwab-page__frame {
  min-block-size: calc(100dvh - var(--qd-navbar-block-size));
}
```

**Abwab-local by construction, not by convention:** the reservation is a new selector on the
abwab component's own stylesheet, not an addition to `.qd-page-frame` in `_layout.scss`.
Promoting it onto the shared rule would have silently reserved a viewport on all five explorer
pages with zero measurement — exactly the premature generalization §1/T803 forbids ("abwab-local
for now"). `UI_STYLE_SYSTEM.md`'s new "Viewport reservation" entry (§17, T803) names the
concrete trigger for promoting it later.

**`box-sizing: border-box` assertion — read from the source, then confirmed live in the same
browser session that measured the frame (not two separate claims):**

```
$ grep -n "box-sizing: border-box" Frontend/quran-dashboard-ui/src/styles/_layout.scss
58:  box-sizing: border-box;
```

That line is inside the `.qd-page-frame, .qd-explorer-frame` rule block committed in phase 7
(`d037b25b`) — it does not appear in this phase's diff because `.qd-page-frame` is already on
the abwab frame element from T703; `.abwab-page__frame` is a second class on the *same* element,
so it inherits `border-box` from the first. Confirmed live via `getComputedStyle` on the actual
DOM element carrying both classes: `boxSizing: "border-box"`. Without it, the reservation would
overshoot the viewport by `.qd-page-frame`'s own `padding-inline` and `padding-block-end`
(48.8 px, measured in phase 7) under the default `content-box` — that padding is exactly what
`border-box` absorbs into the declared `min-block-size` instead of adding on top of it.

**Note on unit:** the shell's own viewport reservation (`_layout.scss:8`,
`.qd-shell-viewport { min-height: 100vh }`) uses `100vh`; T801's text specifies `100dvh`
(matching `--qd-mushaf-panel-height`'s existing arithmetic at `_tokens.scss:77`) and that is
what shipped. The two units can diverge on mobile browsers with a collapsing address bar; not
exercised in this desktop verification, flagged so a future reader does not read the mismatch as
a copy-paste error.

### T802 — making the reservation stretch the content, not just bound the frame

Four-link chain (`abwab-page.component.scss`), replacing the old fixed
`min-height: 20rem` on `.abwab-page__tree-card`:

```scss
.abwab-page__layout {
  display: flex;
  flex: 1;                 // new
  min-block-size: 0;       // new
  gap: var(--qd-space-4);
  align-items: flex-start; // UNCHANGED — see below
  margin-block-start: var(--qd-space-3);
}

.abwab-page__main {
  flex: 1;
  min-width: 0;
  align-self: stretch;     // new
  display: flex;            // new
  flex-direction: column;   // new
}

.abwab-page__tree-card {
  flex: 1;             // was: min-height: 20rem
  min-block-size: 0;   // was: min-height: 20rem
}
```

**`.abwab-page__layout` keeps `align-items: flex-start`, not `stretch`.** `.abwab-page__side` is
`position: sticky`; stretching the row to the frame's full height would give the sticky aside
zero scroll travel, silently breaking it (and would have made phase 9's T902 re-base a dead
behavior). Only `.abwab-page__main` opts into filling the frame, via `align-self: stretch` on
itself rather than `align-items: stretch` on its parent row.

Verified before editing: `grep -rn "20rem\|tree-card\|abwab-page__layout\|abwab-page__main"
src/app/features/abwab/pages/abwab-page/` returned no spec hits — zero-test-change, confirmed
by the T503-baseline-matching Vitest run below, not assumed.

### Browser verification

Backend built (`dotnet build Backend/QuranDashboard.sln`, 0 warnings/errors) and run
(`dotnet run --no-build --urls https://localhost:5015`, `/api/health` → healthy). Frontend run
via `npm start` (`https://localhost:4200`). Driven with the `claude-in-chrome` MCP tool per
phase 7's finding (Playwright MCP and headless chrome-devtools MCP both fail on the mkcert cert
with `ERR_CERT_AUTHORITY_INVALID`). Both servers were stopped (`pkill`) after verification,
before running the Tier B gate.

**Geometry at the one viewport this environment's browser window would actually hold**
(`window.innerHeight` 903 px throughout — `resize_window` calls to 1600×900 and 1400×560 did not
visibly change `window.innerHeight`/`outerHeight` in this environment, confirmed by re-reading
`window.outerWidth/outerHeight` after the call, which still reported the full 1920×1080 screen;
noted as an environment limitation, not a finding about the code — the `calc(100dvh - …)`
formula is viewport-size-independent by construction, so a single confirmed viewport proves the
arithmetic):

| Quantity | Measured | Expected | Match |
|---|---|---|---|
| `--qd-navbar-block-size` (`.qd-navbar` rect height) | 56 px | 3.5 rem = 56 px | yes |
| `window.innerHeight` | 903 px | — | — |
| `.abwab-page__frame` computed `min-block-size` | 847 px | `903 − 56 = 847` | **exact** |
| `.abwab-page__frame` `getBoundingClientRect().height` (tree view, modest data) | 847 px | = min-block-size (content smaller than floor) | **exact** |
| `.abwab-page__frame` `boxSizing` | `border-box` | `border-box` | yes |
| Frame `top` | 80 px | navbar 56 + `.qd-page` top padding 24 (`--qd-space-5`) | **exact** |
| `qd-footer` `top` (page bottom, no archived residue) | 1007 px | navbar 56 + page padding-top 24 + frame 903-min-analog… see overshoot note below | see below |

**The frame does not overshoot the viewport itself** (847 px computed exactly equals
`903 − 56`); the outer `.qd-page.abwab-page` wrapper's own block padding
(`padding: var(--qd-space-5) var(--qd-space-4)`, 24 px top **and** bottom, `_layout.scss:74`)
sits *outside* the frame and is not part of this reservation — §4.2's constraint is only that
the reservation's own arithmetic never cite a footer number, which it does not. Measured on the
first page load (959 px viewport before the browser window's chrome/tab-strip stabilized):
`document.documentElement.scrollHeight − window.innerHeight` gap before the footer was
`1007 − 959 = 48 px`, exactly `2 × --qd-space-5` (2 × 24 px) — the page wrapper's own top+bottom
padding, explained and not a defect. The footer itself sits wholly below that, its own height
varying with its health-indicator branch (120 px observed with the DB check rendered) — never
folded into the reservation's calc, per §4.2.

**The content actually stretches (T802 proven, not aspirational) — measured across four states
at the same viewport, in one script so no window resize could occur between them:**

| State | Frame height | Frame top | `.abwab-page__tree-card` height | Toolbar top/bottom |
|---|---|---|---|---|
| `tree`, loaded (small sandbox section) | 847 px | 80 px | 632.9 px | 192.8 / 233.3 |
| `cards`, loaded | 847 px | 80 px | 632.9 px | 192.8 / 233.3 |
| `tree`, search matches nothing (`qd-state variant="empty"`, `abwab-page-empty`) | 847 px | 80 px | 632.9 px | 192.8 / 233.3 |
| `tree`, search cleared | 847 px | 80 px | 632.9 px | 192.8 / 233.3 |
| `tree`, transport error, no snapshot (backend killed, `qd-state variant="error"` + retry, backend restarted after) | 847 px | 80 px | — (card replaced by `qd-state`) | 192.8 / 233.3 |

Frame height, frame top and toolbar position are **pixel-identical** across all five
loaded/empty/error cells. The toolbar stays mounted through the error state (T402, phase 4;
confirmed still holding here) — visible text read from the DOM: *"تعذر تحميل شجرة الأبواب. حاول
مرة أخرى. إعادة المحاولة"*.

**Loading state — verified by construction, not pixel-captured.** Killing the local backend
makes the connection refuse instantly (no observable pending window on localhost), so the
skeleton branch (`abwab-page.component.html:61-73`) could not be caught mid-flight by polling at
100 ms intervals; a controlled delay/route-interception harness is explicitly phase 11's job
(T1101), not this phase's. Construction argument instead: all three `@if` branches
(loading/error-without-snapshot/loaded) wrap their content in the *same*
`.abwab-page__layout > .abwab-page__main > .qd-card.abwab-page__tree-card` markup
(confirmed by reading `abwab-page.component.html:61-155`), and the geometry chain above (frame
`min-block-size` → `.abwab-page__layout` `flex:1` → `.abwab-page__main`
`align-self:stretch` → `.abwab-page__tree-card` `flex:1; min-block-size:0`) is indifferent to
what the card's children are — `qd-skeleton-rows` included. The loaded/empty/error cells already
measured pixel-identical is the empirical half of that argument; the shared markup is the
structural half.

**One real dataset stretched the frame past the floor, and that is correct, not a regression.**
The archive view (`?archive=1`) on this local dev DB carries **1,033 archived items**
(residue from repeated e2e sandbox runs, per `Frontend/quran-dashboard-ui/CLAUDE.md`'s own note
that the abwab e2e specs "leave archived residue by design") — its frame grew to 9533 px, far
past the 847 px floor. `min-block-size` is a floor, not a cap: when real content genuinely
exceeds the reserved viewport, the frame (and the page) grows and the page scrolls further, the
same as it always could. This is not the layout-shift item 4 guards against — that guard is
about *state changes at the same amount of content* (which the table above proves holds), not
about a page becoming taller because there is more to show.

**Nothing leaked through the shared `.qd-page-frame` class:**

| Page | Selector checked | `min-block-size` | Carries `abwab-page__frame`? |
|---|---|---|---|
| `abwab-templates-page` (`/abwab/templates`) | `.qd-page-frame` | `0px` | no |
| `roots-explorer-page` (`/dashboard/words/roots`) | `.qd-explorer-frame` | `0px` | no (different literal class; same shared rule, `boxSizing: border-box` still present, unaffected) |

Both pages render full-bleed with no console errors (`read_console_messages`, `onlyErrors:
true`, empty) and no visual regression versus their phase-7 screenshots.

### Tier B gate

Commands run from `Frontend/quran-dashboard-ui/`, `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`
preserved (baked into `npm test`, no direct `ng test` call):

```bash
npm test
```

Result: **191 spec files passed (191) / 2164 tests passed (2164) / 0 failed.** Duration 178.92s.

**Delta vs T601 (191 files / 2164 tests): +0 files, +0 tests — exact match**, as expected for a
zero-test-change phase (§3, §7.1, confirmed by the pre-edit spec grep above). No spec was edited
or added.

```bash
npm run build
```

Result: **green.** `ng build` completed in 15.176s, output at `dist/quran-dashboard-ui`. Same
four pre-existing budget categories as phase 7's close, **byte-for-byte unchanged** (this
phase's only edits are component-scoped SCSS/HTML plus doc files — no global stylesheet bytes
added):

- initial bundle exceeded the 500.00 kB budget by 68.83 kB (568.83 kB total) — unchanged from phase 7
- `selected-word-section.component.scss` exceeded its 4.00 kB budget by 649 bytes — unchanged
- `selected-ayah-section.component.scss` exceeded its 4.00 kB budget by 1.85 kB — unchanged
- `abwab-relations-modal.component.scss` exceeded its 4.00 kB budget by 1.08 kB — unchanged

No new warning categories. No errors.

**Obligations checked at close of phase 8:** the viewport reservation ships with
`box-sizing: border-box` proven present (source read + live `getComputedStyle`, same session);
the reservation stays abwab-local (`UI_STYLE_SYSTEM.md` §17 entry states the arithmetic, the
`border-box` prerequisite, and the concrete generalization trigger); the content stretches into
the reservation and no state resizes the frame (measured across five states, plus the
construction argument for `loading`); `features/abwab/README.md` records the scope decision
(doors page only) and the four-link stretch chain. T101→T601→phase-7→phase-8 Vitest/build
deltas are unbroken at +0/+0 and byte-identical budgets.

## Phase 9 (T901–T905) — sticky navbar, item 6

Branch `ux-slice-b2-frame`, working tree clean at phase 8's close before this phase's edits.
Backend built and run (`dotnet build Backend/QuranDashboard.sln`, 0 warnings/errors;
`dotnet run --no-build --urls https://localhost:5015`, `/api/health` → healthy); frontend via
`npm start` (`https://localhost:4200`). Driven with the `claude-in-chrome` MCP tool per phases
7/8's finding. Both servers stopped after verification, before the Tier B gate.

### T901 — `.qd-navbar` becomes sticky, and a real defect surfaced while proving it

`_layout.scss`: `.qd-navbar` gained `position: sticky; inset-block-start: 0; z-index:
var(--qd-z-sticky)`.

**First browser measurement showed it did not stick at all** — `getBoundingClientRect().top`
tracked `-scrollY` exactly at every scroll offset tested (`0, 20, 40, 56, 80, 120, 168`), and a
screenshot at `scrollY: 168` confirmed the navbar had scrolled fully off-screen. Root-caused by
building a minimal in-page repro (a `position: sticky` probe inside a wrapper sized to exactly
its own height) that reproduced the failure identically, then fixing it by setting the wrapper to
`display: contents` — which resolved it. **Root cause: `.qd-navbar`'s Angular component host
(`<qd-top-navbar>`) is a flex item of `.qd-shell-viewport` (flex items are blockified) and, with
no height of its own, wraps the 56px navbar in a block exactly 56px tall.** A sticky box's travel
is clamped to its containing block's content box; a containing block exactly the element's own
height gives zero travel, so the box can never leave its static position — spec behavior in every
engine, not a Chrome-specific bug (confirmed by an isolated repro on `example.com`, where an
ordinary tall-container sticky probe worked correctly on the first try, in the same browser
session). **Fix:** `:host { display: contents; }` added to `top-navbar.component.scss`, which
drops the host out of the box tree so `.qd-navbar` becomes the direct flex item of the
903px+-tall `.qd-shell-viewport` instead. Re-measured after the fix: `rect.top === 0` at every
scroll offset `0`–`168`, confirmed both via `getBoundingClientRect()` and a screenshot showing the
navbar pinned at the top of the viewport while the page content and footer scrolled underneath.

| Property | Value |
|---|---|
| `.qd-navbar` computed `position` | `sticky` |
| `.qd-navbar` computed `z-index` | `5` (`--qd-z-sticky`) |
| `.qd-navbar` `getBoundingClientRect().height` | `56px` (= `--qd-navbar-block-size`, 3.5rem) |
| `<qd-top-navbar>` computed `display` (after fix) | `contents` |

### T902 — both sticky offsets re-based, plus a third arithmetic gap found and fixed

`--qd-mushaf-sticky-top` (`_tokens.scss`): `var(--qd-space-3)` → `calc(var(--qd-navbar-block-size)
+ var(--qd-space-3))`. `.abwab-page__side`'s `top` (`abwab-page.component.scss`): `var(--qd-space-4)`
→ `calc(var(--qd-navbar-block-size) + var(--qd-space-4))`.

| Offset | Formula | Computed value |
|---|---|---|
| `--qd-mushaf-sticky-top` | `calc(3.5rem + 0.75rem)` | `68px` |
| `.abwab-page__side` `top` | `calc(var(--qd-navbar-block-size) + var(--qd-space-4))` | `72px` (measured `cssTop`) |

Both confirmed live: the mushaf reader panel's computed `top` read `68px` and stuck flush under
the 56px navbar at every scroll offset tested; the abwab side panel's computed `top` read `72px`.

**A third gap found while checking §4.5, adjacent to but distinct from double-subtraction:**
`--qd-mushaf-panel-height` (`_tokens.scss`) was still `calc(100dvh -
var(--qd-navbar-block-size))` — sized from the bare navbar token, not from the panel's own
re-based `top`. Measured live: at `scrollY: 120` (enough to fully stick the panel), `rect.top =
68`, `rect.height = 847`, `rect.bottom = 915`, `window.innerHeight = 903` — the panel's stuck
bottom edge sat **12px below the viewport** (`--qd-space-3`, exactly the extra gap
`--qd-mushaf-sticky-top` adds beyond the bare navbar height). Not double-subtraction (the
arithmetic never subtracts the navbar twice), but a single omission: the height formula ignored
part of the offset it is supposed to fit beneath. Confirmed this wasn't an artifact of thin
content (al-Fatihah, 7 short ayahs, under-filling the 847px box) by forcing genuine container
slack (selecting a word to populate the study column to `966.9px`, well past the panel's own
height) and re-measuring — the 12px overshoot persisted at `scrollY: 100` regardless. **Fixed:**
`--qd-mushaf-panel-height` re-derived as `calc(100dvh - var(--qd-mushaf-sticky-top))` instead of
the bare navbar token — CSS custom properties resolve at used-value time, so referencing
`--qd-mushaf-sticky-top` (declared later in the same `:root` block) is valid. Re-measured after
the fix: `top: 68, height: 835, bottom: 903` — flush with the viewport, `overshoot: 0`, exactly.
This has exactly one consumer (`mushaf-reader-page.component.scss`), confirmed by `grep -rn
"qd-mushaf-panel-height"` before editing.

### T903 — item 6's three constraints, plus §4.5's, verified live

**1. `--qd-mushaf-panel-height` double-subtraction check: passed, no double-subtraction found**
(see T902 above for the adjacent single-omission defect that *was* found and fixed).

**2. Both re-based sticky offsets sit flush under the chrome: confirmed** — `68px` (mushaf) and
`72px` (abwab side panel), both measured live, both landing exactly at
`navbar-height + original-offset` with no drift.

**3. Navbar dropdowns must still escape the navbar's new stacking context — initial finding,
escalation, and the resolved rung.**

*First finding, at `--qd-z-sticky` (5):* `position: sticky` unconditionally establishes a new
stacking context in every current browser engine, regardless of `z-index` value — confirmed by
forcing `.qd-navbar`'s `z-index` to `auto !important` live and re-testing: the dropdown (still a
DOM descendant of the now-`z-index:auto`-but-still-sticky navbar) remained trapped, proving the
trigger is `position: sticky` itself, not the `z-index` value. Confirmed a second, independent way
with a clean isolated repro (a `position: sticky; z-index: auto` wrapper containing a `z-index:
45` child, versus a `z-index: 40` sibling appended to `document.body`): the sibling won every
time. A decisive test against the app's real DOM (a transparent `--qd-z-floating` probe placed at
the open dropdown's own rect, outside the navbar) confirmed the probe won — the dropdown lost to a
40-rung element it should have beaten.

**This was escalated rather than accepted as a limitation, and the call came back: fix it, still
inside phase 9.** Re-analysis against every `--qd-z-*` consumer (`grep -rn "var(--qd-z-"`) showed
`--qd-z-sticky` (5) is not merely suboptimal for the dropdown — it is the wrong rung for a sticky
navbar generally, because the navbar hosts two of its own descendants that already declare
`--qd-z-mobile-nav` (45) for themselves (`top-navbar.component.scss`): `.dropdown-menu` and
`.mobile-menu` (`position: fixed; inset: 0`, the full-screen mobile overlay). At rung 5 both are
clamped below `--qd-z-popover` (30) and `--qd-z-floating` (40) — not just the dropdown, but the
mobile overlay too, and in the other direction, a page popover at rung 30 would now paint *over*
the sticky navbar's own box on a scrolled page, a failure mode that could not exist before the
navbar was sticky (content never scrolled under it).

**Resolution: `.qd-navbar` moved from `--qd-z-sticky` to `--qd-z-mobile-nav` — the same rung its
dropdown and mobile menu already declare, no new token.** This satisfies §4's stated purpose for
the scale ("deliberately below `--qd-z-menu-backdrop`, so row menus and modals paint above the
chrome") while fixing the mechanism: 45 beats popover (30) and floating (40); 49/50/51 (menu
backdrop / menu / modal backdrop) still beat it. `--qd-z-sticky` (5) is unchanged and keeps its
one in-page consumer (`mushaf-header-navigation.component.scss`) — it was never wrong for a
sticky element with no competing descendants of its own, only for one that also hosts higher-rung
menus. `styles/_layout.scss`'s `.qd-navbar` rule and `_tokens.scss`'s layer-scale comment block
(both the per-token trailing comments and the ordering prose) were updated accordingly.

**Re-verified live, all four cases the escalation asked for, all passing:**

| Check | Method | Result |
|---|---|---|
| (a) Dropdown vs. a `--qd-z-floating` (40) element outside the navbar | Same probe technique as the first finding: transparent `div` at the open `#words-menu`'s rect, `z-index: var(--qd-z-floating)`, appended to `document.body` | `elementFromPoint()` at the dropdown's center now returns the dropdown link (`.dropdown-link`), not the probe |
| (a, cont.) Dropdown-rung vs. the **real** `detail-modal-shell__restore` control | Probe at the restore control's actual live rect, `z-index: var(--qd-z-mobile-nav)` | `elementFromPoint()` at the restore control's center returns the probe — a 45-rung element now beats the real 40-rung restore control |
| (b) `.mobile-menu` covers page content and beats a popover | Opened the mobile menu (`.menu-toggle` click, computed `position: fixed`, `z-index: 45`, rect `0,0`–full viewport); probe at `--qd-z-popover` (30) placed under it | `elementFromPoint()` inside the menu returns `.mobile-link`, not the probe |
| (c) `qd-context-menu` and a modal backdrop still paint above the (now 45-rung) navbar | Opened a real abwab door-modal backdrop (`z-index: 50`) and a real row `qd-context-menu` (`__backdrop` 49 / menu 50) via `contextmenu` dispatch; `elementFromPoint()` at the navbar's own center in both cases | Both return the modal backdrop / context-menu backdrop, not the navbar |
| (d) The sticky navbar itself is not overpainted by a popover on a scrolled page | Scrolled the mushaf reader to `scrollY: 50`; probe at the navbar's own live rect, `z-index: var(--qd-z-popover)` (30, matching the real `surah-jump-picker` popover's own rung) | `elementFromPoint()` at the navbar's center returns `.qd-navbar`, not the probe |

**Cleanup:** every probe/override element (`zindex-probe`, `zindex-probe2`, `zindex-probe3`,
`zindex-test-override`, `repro-wrap`, `repro-probe`, `zindex-probe-recheck`, `mobile-nav-probe`,
`popover-probe`, `popover-vs-navbar-probe`) was removed from the live DOM after each test; none
touched source files.

**E2E regression guard**, `npx playwright test --project=default --workers=2 e2e/shell-nav.e2e.ts
e2e/mushaf-reader.e2e.ts`:

```
8 passed (9.4s)
```

All three `shell-nav.e2e.ts` cases (navbar links reach the reader, words dropdown reaches the
words hub, more dropdown reaches a placeholder section) and all five `mushaf-reader.e2e.ts` cases
passed. Evidence, not a tier (§7.1). **Re-run again after the `--qd-z-mobile-nav` rung fix below
(same command): 8 passed (9.5s)**, identical result.

### T904 — `ScrollLockService.isLocked`, navbar `[inert]`, and inert-inside-inert observed live

`scroll-lock.service.ts`: `lockCount` became a `signal(0)`; added `readonly isLocked =
computed(() => this.lockCount() > 0)`. `top-navbar.component.ts` injects `ScrollLockService` and
exposes `protected readonly locked = this.scrollLock.isLocked`; the template gained
`[attr.inert]="locked() ? '' : null"` and `[attr.aria-hidden]="locked() ? true : null"` on
`<nav class="qd-navbar">`, copying `app.ts:14`'s pairing exactly.

**`scroll-lock.service.spec.ts` extended** (the one sanctioned additive spec for B2, per plan §3/
§7.1) with one new test: `isLocked` tracks true/false correctly across two simultaneous
acquire/release consumers. No new spec file.

**Live verification, four scenarios, all measured directly on the real DOM (not just the unit
spec):**

| Scenario | `body.style.overflow` | `.qd-navbar[inert]` | `.qd-navbar[aria-hidden]` | `qd-app-shell[inert]` |
|---|---|---|---|---|
| Plain abwab door modal open | (n/a, not re-measured here) | `""` | `"true"` | — |
| Closed again | — | `null` | — | — |
| `abwab-sections-modal` open (T905) | `"hidden"` | `""` | `"true"` | — |
| `abwab-move-picker` open (T905) | `"hidden"` | `""` | `"true"` | — |
| Words drawer alone (`root-details-modal`, forced mobile via `isDesktop.set(false)` on the live component instance) | `"hidden"` | `""` | `"true"` | `null` |
| Words drawer **+** global overlay (`detail-modal-shell`) both open — **inert-inside-inert** | `"hidden"` | `""` | `"true"` | `""` (+ `aria-hidden="true"`) |

The drawer-alone row is the concrete proof of T904's new, independent behavior: the navbar goes
inert **without** the shell being inert, something impossible before this phase (shell inert was
previously the only inert mechanism, gated solely on the global overlay).

**Inert-inside-inert, observed and recorded as asked:** with the words drawer open under the
global overlay, `qd-app-shell.getAttribute('inert')` is `""` (from `app.ts`'s `overlayOpen()`)
**and** `.qd-navbar.getAttribute('inert')` is independently `""` (from
`ScrollLockService.isLocked()`, since the drawer itself holds the lock) — both apply
simultaneously. `shell.contains(drawer) === true`, `shell.contains(dialog) === false`, matching
`app.nested-layers.spec.ts`'s existing assertions. **"Exactly one focus trap enabled" still
holds, confirmed live, not just via the spec:** `document.querySelectorAll(
'.cdk-focus-trap-anchor[tabindex="0"]')` returned exactly 2 elements, both children of
`[data-testid="detail-modal-backdrop"]` (the dialog's trap) — none under the drawer.

`app.nested-layers.spec.ts` run (targeted, before the full suite): **4/4 passed**
(`spec-app-app.nested-layers.spec.js`).

### T905 — `qdModalScrollLock` added to the two remaining abwab modals

`abwab-sections-modal.component.ts`/`.html` and `abwab-move-picker.component.ts`/`.html`: imported
`ModalScrollLockDirective`, added it to each component's `imports` array, and added
`qdModalScrollLock` to each modal's root `<section class="qd-modal …">` element. Verified live for
both (table above): opening either sets `body.style.overflow = "hidden"` and inerts the navbar;
closing either (`abwab-sections-modal-close`, `abwab-move-picker-cancel`) clears both.

**Two intentional behavior changes, named per the task's instruction, not slipped in:**

1. **The navbar becomes keyboard-unreachable while any of nine surfaces is open** — six abwab
   modals (four pre-existing plus these two) and five words detail surfaces/dialogs that already
   held the lock before this phase. Nobody asked about the five words surfaces; accepted
   deliberately because the doctrine ("app chrome is not reachable while a modal dialog is open")
   is not abwab-specific and `app.ts:14` already applies a *stronger* version of it (whole-shell
   inert) for the global overlay.
2. **`abwab-sections-modal` and `abwab-move-picker` stop the page scrolling behind them** — a
   latent defect fixed as a side effect of giving them the lock, per plan §2's naming.

### Re-checked: phase 8's invariants, after the navbar's box-tree change

T901's `display: contents` fix removes `<qd-top-navbar>` from the box tree, so the abwab page
frame's ancestor chain changed shape. Re-measured at the same viewport phase 8 used:

| Quantity | Phase 8 (evidence.md) | Phase 9 (re-measured) | Match |
|---|---|---|---|
| `.abwab-page__frame` `top` | `80px` | `80px` | exact |
| `.abwab-page__frame` height / `min-block-size` | `847px` | `847px` | exact |
| `boxSizing` | `border-box` | `border-box` | exact |
| `.qd-navbar` height | `56px` (3.5rem) | `56px` | exact |

Unchanged — the box-tree change is contained to the navbar's own subtree and does not perturb the
abwab frame's geometry.

### Tier B gate

Commands run from `Frontend/quran-dashboard-ui/`, `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`
preserved (baked into `npm test`, no direct `ng test` call):

```bash
npm test
```

Result: **191 spec files passed (191) / 2165 tests passed (2165) / 0 failed.** Duration 178.14s.

**Delta vs T601 (191 files / 2164 tests): +0 files, +1 test — exactly T904's `isLocked` case.**
The plan's own T1102 expectation (+0 files, +2–4 tests total for B2) is split across phases: this
phase contributes 1 of that range; phase 10 (T1002/T1004, the builder/labels specs) owes the
remaining 1–3.

```bash
npm run build
```

Result: **green.** `ng build` completed in 18.151s, output at `dist/quran-dashboard-ui`. Same four
pre-existing budget categories as phase 8's close, one small explained delta:

- initial bundle exceeded the 500.00 kB budget by **69.15 kB (569.15 kB total)** — **+0.32 kB vs
  phase 8's 568.83 kB.** Expected: the sticky-navbar CSS (`_layout.scss`), the re-based token/offset
  arithmetic (`_tokens.scss`, `abwab-page.component.scss`), `top-navbar.component.ts`'s new
  `ScrollLockService` import/signal, and two more components importing
  `ModalScrollLockDirective`. Not a regression — the minimal, explained cost of five components'
  worth of new imports/bindings.
- `selected-word-section.component.scss` exceeded its 4.00 kB budget by 649 bytes — unchanged
- `selected-ayah-section.component.scss` exceeded its 4.00 kB budget by 1.85 kB — unchanged
- `abwab-relations-modal.component.scss` exceeded its 4.00 kB budget by 1.08 kB — unchanged

No new warning categories. No errors.

**Gate re-run after the `--qd-z-mobile-nav` rung fix (below), same commands:** `npm test` →
**191 files / 2165 tests, 0 failed** (191.07s), byte-for-byte the same pass/fail shape as above.
`npm run build` → **green**, initial bundle **569.15 kB**, identical to the number above —
swapping which token `.qd-navbar` references costs nothing measurable (`var(--qd-z-mobile-nav)`
is the same length as `var(--qd-z-sticky)`). Same four pre-existing budget categories, no new
warnings, no errors. Both gate runs are recorded because the rung changed after the first one;
the numbers did not move.

**Obligations checked at close of phase 9:** `.qd-navbar` ships `position: sticky`, confirmed
sticking in the browser only after the `display: contents` fix; both re-based sticky offsets
(mushaf `68px`, abwab side panel `72px`) verified flush, plus the adjacent
`--qd-mushaf-panel-height` gap found and fixed; `--qd-mushaf-panel-height` re-checked for
double-subtraction (none found); navbar dropdowns' escape constraint checked, found to fail at
`--qd-z-sticky`, escalated, and **resolved** by moving `.qd-navbar` to `--qd-z-mobile-nav` — the
rung its own dropdown and mobile menu already declare — with all four re-verification cases
passing live (probe-vs-dropdown, probe-vs-real-restore-control, mobile-menu-vs-popover,
context-menu/modal-backdrop-vs-navbar, popover-vs-scrolled-navbar); no "known limitation" claim
left standing in any doc; `ScrollLockService` exposes `isLocked`; navbar inert + `aria-hidden`
paired as `app.ts:14` does; `app.nested-layers.spec.ts` run (4/4) and the inert-inside-inert state
observed live on the real DOM, not only asserted by the spec; `qdModalScrollLock` added to
`abwab-sections-modal` and `abwab-move-picker`, both intentional behavior changes named above;
phase 8's frame geometry re-verified unchanged after the box-tree change; Vitest/build deltas
explained (+0/+1, +0.32 kB then unchanged after the rung fix) and re-confirmed after the fix.

## Phase 10 (T1001–T1005) — the stats bar, item 17

Branch `ux-slice-b2-frame`, working tree clean at phase 9's close before this phase's edits.

### T1001 — promoting `explorer-result-count` to `shared/ui/result-count/`

Moved via `git mv` (rename, not delete-and-recreate — confirmed in `git status` as `R`, not `D`+`A`):
`explorer-result-count.component.ts/.html/.scss/.spec.ts` from
`features/words/components/explorer-result-count/` to `shared/ui/result-count/`, filenames
unchanged. **Kept the class name `ExplorerResultCountComponent`** (renaming it would have forced
edits to all four words pages' `imports: […]` arrays for zero behavioral gain) and kept every
internal class/testid (`.explorer-result-count*`, `explorer-result-count-*`) byte-identical — the
moved spec asserts them and passed unedited.

Selector became `'qd-result-count, qd-explorer-result-count'` (dual selector, same mechanism as
`qd-panel-skeleton, qd-explorer-panel-skeleton` at `explorer-panel-skeleton.component.ts:16`).
`WORDS_RESULT_COUNT_LABELS` moved to a new file, `shared/ui/result-count/result-count.labels.ts`,
renamed `RESULT_COUNT_LABELS` (grepped first: its only reader anywhere in the repo was the
component itself, so no re-export was needed); the component's own TDZ-safe getter
(`protected get labels() { return RESULT_COUNT_LABELS; }`) is unchanged in shape, only its import
target moved — the idiom was preserved, not dropped, on the move.

**Precedent for abwab importing a component whose class still says "Explorer"**, confirmed before
using it: `abwab-templates-page.component.ts` already imports `ExplorerPanelSkeletonComponent`
from `shared/ui/explorer-panel-skeleton/` and consumes it via the neutral `qd-panel-skeleton`
selector. `abwab-page.component.ts` now does the identical thing with
`ExplorerResultCountComponent` / `qd-result-count`.

**Four words call-sites, not five** (the plan's own text says "five"; measured here instead of
assumed): `grep -rln "qd-explorer-result-count\|ExplorerResultCountComponent"` returns exactly
Roots, Lemmas, Stems, Unique Words — `features/words/README.md` itself states "Word Types uses
the separate four-count scope summary, not this stat," so there never was a fifth page consumer.
Only the four pages' **TS import path** changed (`'../../components/explorer-result-count/…'` →
`'../../../../shared/ui/result-count/explorer-result-count.component'`, depth verified against an
existing `shared/ui/pagination` import already in the same four files); their `.html` templates
are untouched, since the alias selector keeps `<qd-explorer-result-count>` resolving to the same
component.

### T1002 — deriving the two numbers, live-only

Two pure functions added to `abwab-tree.builder.ts` (the specced module the plan names):

- `countLiveAbwabDoors(byId)` — iterates `AbwabTreeSnapshotVm.byId` and counts every
  `!isArchived` node. This is «كل الأبواب»'s number.
- `countAbwabDoorsInOpenScope(sections, activeSectionId, totalLiveDoors)` — returns the section's
  own `doorsInScopeCount` (already on the wire) for a specific section, or the live-only total
  itself when `activeSectionId === null` («كل الأبواب» has no per-section count to read, and
  "everything" and "the open scope" are the same set on that tab).

**The live-only choice is stated in a comment at each function**, and **no test in this phase
asserts the two numbers sum** — the one new test
(`abwab-tree.builder.spec.ts`, "item 17 stats bar") builds a snapshot with one section-less live
door, one in-section live door, and one archived door in that section, and checks
`countLiveAbwabDoors` (2, archived excluded, section-less counted) and
`countAbwabDoorsInOpenScope` (1 for the section — deliberately less than the total 2, with a
comment explaining why; 0 for an unknown section id; the total itself for `null`) as three
independent facts, never their sum.

`abwab-page.component.ts` wires both as computed signals (`totalLiveDoorsCount`,
`openScopeDoorsCount`) reading the page's existing `byId`/`sections`/`activeSectionId` signals —
no new subscription, no new API call.

### T1003 — composing the stat bar

Two `<qd-result-count>` instances in a new `.abwab-page__stats` block
(`abwab-page.component.html`), mounted directly above `<qd-abwab-toolbar>` and **always
mounted** (no `@if`) through every loading/error/loaded state and every tab switch — matching the
toolbar's own T402 reasoning: an unmounting stat would move the toolbar under it, exactly the
regression this slice exists to remove. `[loading]`/`[hasError]` wired off
`facade.isLoading() && !facade.snapshot()` / `!!facade.errorMessage() && !facade.snapshot()`,
the same conditions the page's own loading/error branches already use two lines below.

**Styling: abwab-local tokens, not the `uw-` prefixed words class.** `.abwab-page__stats` reuses
`.uw-toolbar-recess__stat`'s *shape* (a flex row, `gap: var(--qd-space-4)`,
`margin-block-end: var(--qd-space-3)`) rather than the words-owned class itself — recorded as a
deliberate choice, not an oversight: phase 7 (T701–T704) just finished de-words-ifying the shared
page-frame class for the identical reason, and this page owns no `.uw-toolbar-recess` wrapper to
extend in the first place. Two lines, not a card — `PRODUCT.md:90-91`'s anti-reference list is
named directly in the code comment.

**Bounded risk named, not hidden:** `flex-wrap: wrap` means the row's height is width-dependent —
at the 1440×900 viewport §7.2's harness uses, both stats sit on one line trivially, so T1101 will
not exercise the wrap. At a narrow enough width the two labels plus a digit that shrinks from four
digits to one between tab switches could cross the wrap threshold and change the block's height,
moving the toolbar under it — the same class of regression this slice targets, just at a width
this phase did not measure.

### T1004 — Arabic copy: the `countPhrase` clause does not apply here, stated rather than forced

**Consulted the advisor before writing this task**, specifically on how "run counts through
`countPhrase`" squares with `qd-result-count`'s fixed template. The answer, verified against the
component's own `.html` (`{{ labelPrefix() }}: {{ count() }}`, plus `ariaLabel =
\`${labelPrefix()}: ${count()}\``): **the component always renders the label and the raw digit as
two separate pieces of one line — "label: N" — a data-display idiom already shipped, unremarked,
on all four words pages ("عدد الجذور: 1642").** Feeding a `countPhrase` agreement string (e.g.
"12 بابًا") into `labelPrefix` would render **"12 بابًا: 12"** — worse Arabic than the plain
form, with the digit duplicated — and the only way to avoid that duplication is a new
`valueText`/`ariaLabel`-override input on the shared component, which is a change five words
pages' worth of consumers do not otherwise need and which the phase's own test-budget line
(0 new spec files) does not have room to earn tests for.

**Conclusion: `T1004`'s "never a bare interpolated count" clause is satisfied (the two new labels
embed no count at all), but its "through `countPhrase`'s forms tables" and "extend the existing
data-driven agreement cases" clauses do not apply to this shape of copy** — they presuppose a
count-taking label function, and this pattern is a stat display, not a counted-noun sentence. This
is stated here rather than forced: no sentence-shaped label was invented just to have something to
feed `countPhrase`.

What shipped instead: two **static** `ABWAB_LABELS` entries. `allDoorsTab` («كل الأبواب») is
reused verbatim as the total stat's label — one string, one concept, no duplicate constant. A new
entry, `statOpenScopeDoors` = «أبواب هذا التبويب» ("doors in this tab"), is deliberately worded to
cover both cases the second stat renders: a specific section's count, or (on «كل الأبواب») the
same number as the first stat — "the tab" reads correctly either way, whereas "the active section"
would be false when no section is active. A small pin test was added to `abwab.labels.spec.ts`'s
existing **"the locked strings"** describe block (not the count-agreement `it.each` block, which
this copy does not use) asserting both label values and that they differ.

### Browser verification

Backend built (`dotnet build QuranDashboard.sln`, 0 warnings/errors after one transient CLR crash
on the first attempt — retried clean) and run (`dotnet run --no-build --urls
https://localhost:5015`, `/api/health` → healthy). Frontend via `npm start`
(`https://localhost:4200`). Driven with the `claude-in-chrome` MCP tool per prior phases' finding.
Both servers stopped after verification, before the Tier B gate.

**The stats bar renders quietly and correctly at `/abwab`:** `data-testid="abwab-page-stat-total"`
read "كل الأبواب:13", `data-testid="abwab-page-stat-open-scope"` read "أبواب هذا التبويب:13" on
«كل الأبواب» (the two legitimately coincide there, as designed). Both lines are plain inline text
above the toolbar — no card, no gradient, no KPI-row visual weight — matching the screenshot.

**The section stat recomputes, and the toolbar does not move:** clicking the «الجهاد» section tab
navigated to `?section=217` and the open-scope stat became "أبواب هذا التبويب:1" (that section
holds one live door) while the total stat stayed "كل الأبواب:13". Measured
`.abwab-toolbar.getBoundingClientRect()` before and after the tab switch (and again after
switching back to «كل الأبواب»): `top: 228.796875` in **all three** captures, pixel-identical —
the stats bar's own height never changes across a tab switch, so it cannot move the toolbar
beneath it. No console errors on any of the three states (`read_console_messages`, `onlyErrors:
true`, empty).

**The archive view was also checked, not assumed:** at `?archive=1` both stats still read
"13" — correct by construction (both are defined as live-door counts, never an archive count;
item 17 was never scoped to the archive), though a user glancing at "13" while scrolling past
hundreds of e2e-residue archived doors could misread it as an archive count. Not a defect against
this phase's own definition, named here as a UX observation for whoever next revisits the stats
bar's scope. No console errors.

**Known wart, reasoned from the code rather than re-driven in the browser:** `?archive=1` alone
was tested with no `section` param, but toggling archive does not clear `section` (only `door`/
`card`), and `hideSectionControls` hides the tabs while `activeSectionId` stays whatever it was.
So `?archive=1&section=217` would show «أبواب هذا التبويب» still computing that section's
`doorsInScopeCount` while no tab is visible to name — a label naming a UI element the user cannot
see. Not exercised live; flagged for whoever next touches this scope rather than silently left for
phase 11 to discover.

**All four words explorer pages are visually and functionally unchanged by the promotion,**
checked individually, not just built: Roots ("عدد الجذور:1642"), Unique Words
(`/dashboard/words/unique/tashkeel`, "عدد الكلمات:21294"), Lemmas ("عدد الصيغ المعجمية:4817"),
Stems ("عدد الأصول الصرفية:11843") — all read via `document.querySelector('qd-explorer-result-count,
qd-result-count')`, all resolving through the alias selector to the moved component, all with
empty `read_console_messages({onlyErrors:true})`.

### Tier B gate

Commands run from `Frontend/quran-dashboard-ui/`, `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`
preserved (baked into `npm test`, no direct `ng test` call):

```bash
npm test
```

Result: **191 spec files passed (191) / 2167 tests passed (2167) / 0 failed.** Duration 167.16s.

**Delta vs T601 (191 files / 2164 tests): +0 files, +3 tests** — 1 from phase 9's T904
(`isLocked`), 2 from this phase: `abwab-tree.builder.spec.ts`'s one new `it` (T1002) and
`abwab.labels.spec.ts`'s one new `it` (T1004, pinning the two static labels — see above for why
the count-agreement `it.each` block itself gained no new rows). **Within the plan's own +2–4
expectation for all of B2** (T1102, phase 11, will re-confirm against T601 directly). No spec
file was added; no existing spec was edited beyond the two additive `it` blocks above and the
import-path changes in the four words pages (which touched no assertion).

```bash
npm run build
```

Result: **green.** `ng build` completed in 12.967s, output at `dist/quran-dashboard-ui`. Same
four pre-existing budget categories as phase 9's close, **byte-for-byte unchanged**
(initial bundle 569.15 kB — identical to phase 9's post-rung-fix number; the four label-file/
component-move edits are non-global TS/HTML, so no global stylesheet or shared-chunk byte moved):

- initial bundle exceeded the 500.00 kB budget by 69.16 kB (569.15 kB total) — unchanged from phase 9
- `selected-word-section.component.scss` exceeded its 4.00 kB budget by 649 bytes — unchanged
- `selected-ayah-section.component.scss` exceeded its 4.00 kB budget by 1.85 kB — unchanged
- `abwab-relations-modal.component.scss` exceeded its 4.00 kB budget by 1.08 kB — unchanged

No new warning categories. No errors.

### Files touched

Moved (via `git mv`): `explorer-result-count.component.{ts,html,scss}` and its `.spec.ts`,
`features/words/components/explorer-result-count/` → `shared/ui/result-count/`.

New: `shared/ui/result-count/result-count.labels.ts`.

Edited: `features/words/models/words-shared.labels.ts` (removed `WORDS_RESULT_COUNT_LABELS`),
`shared/ui/result-count/explorer-result-count.component.ts` (selector, import, comment),
`roots-explorer-page.component.ts` / `lemmas-explorer-page.component.ts` /
`stems-explorer-page.component.ts` / `unique-words-page.component.ts` (import path only),
`features/abwab/state/abwab-tree.builder.ts` (+2 exported functions),
`features/abwab/state/abwab-tree.builder.spec.ts` (+1 test),
`features/abwab/pages/abwab-page/abwab-page.component.ts` (+2 computed signals, +2 label getters,
+1 import, +1 `imports` array entry),
`features/abwab/pages/abwab-page/abwab-page.component.html` (+`.abwab-page__stats` block),
`features/abwab/pages/abwab-page/abwab-page.component.scss` (+1 rule),
`features/abwab/models/abwab.labels.ts` (+1 label), `features/abwab/models/abwab.labels.spec.ts`
(+1 test). Docs: `features/words/README.md`, `shared/README.md`, `features/abwab/README.md`,
`.architecture/UI_STYLE_SYSTEM.md` (§17, new `qd-result-count` entry).

**Obligations checked at close of phase 10:** `qd-result-count` promoted with the alias selector
in place, TDZ getter idiom preserved (traced through both the getter's shape and its new import
target), spec moved with it (file-count-neutral — a rename, not a new file); both stats derived
from the existing snapshot with no backend call added (verified: no new import of `AbwabApi` or
any HTTP client in any file this phase touched); the live-only definition and the
nullable-section caveat both written down (`abwab-tree.builder.ts` comments,
`features/abwab/README.md`); the two stats' labels are distinct and neither is a bare interpolated
count (T1004's inapplicable clause named explicitly above, not silently skipped); four
`UI_STYLE_SYSTEM.md`/README amendments made; Tier B delta explained and inside the plan's own
range.

## Phase 11 (T1101–T1103) — B2 verification and doc integrity

Branch `ux-slice-b2-frame`, working tree clean at phase 10's close before this phase's work.
This phase makes **no source edits** — it is verification-only plus one dangling-doc repoint —
so it introduces nothing for Tier B to regress.

### T1101 — layout-stability acceptance, full matrix (§7.2)

Extraction-style evidence for the layout-stability claim; not a tier and no substitute for the
Vitest suite or the build.

**Harness:** a temporary Playwright spec, `e2e/abwab-tmp-layout-stability.e2e.ts`, run as
`npx playwright test --project=abwab --workers=1 e2e/abwab-tmp-layout-stability.e2e.ts` against a
local dev server + local backend (`https://localhost:4200` / `https://localhost:5015`) and the
local `quran_dashboard` dev DB, using the `abwabSandbox` fixture plus one extra API-created empty
section for the `empty` macro-state (same mechanism T502 used). Fixed viewport `1440×900`. Two
further one-off temporary specs were added afterward, each to settle a specific open question
raised by review of this run's numbers and described inline where they apply below:
`e2e/abwab-tmp-tabwrap-check.e2e.ts` (whether the toolbar-wrap finding reproduces on
near-realistic, non-sandbox data) and `e2e/abwab-tmp-toolbar-height-check.e2e.ts` (whether the
266.3-vs-269.3px split is state-driven or driven by which toolbar children render). **All three
deleted after their runs** — confirmed absent from the tree (`ls e2e/*tmp*` → no match) each time,
and no stray dev server left on `:4200`/`:5015` afterward (`pkill`/`fuser -k`, re-verified with a
`curl` timeout against both health endpoints each time).

**A methodology bug was found and fixed in the harness itself before trusting its numbers.** The
first cut waited only for `abwab-toolbar-search` to be visible before capturing — but the toolbar
mounts unconditionally in the loading branch too (T402), so that wait is satisfied *before* the
tree snapshot (and therefore `sections()`, which drives the toolbar's own section tabs) has
arrived. The `loaded/tree/off` cell was captured mid-flight, during the still-loading render, and
produced a false "1 tab, single-line toolbar" reading — a race in the harness, not the app.
Fixed by waiting for the real "data has arrived" signal instead: `qd-skeleton-rows` reaching zero
count (`gotoAndWaitLoaded`) plus, for the `loaded`/`empty` loops, waiting for the sandbox's own
section tab (`abwab-toolbar-tab-<sandbox section id>`) to be visible — a signal tied to data this
harness controls, not to ambient DB content. Confirmed the fix mattered by re-running before and
after: the "loaded" cells' `toolbarBottom` changed from 269.3 (1 tab, wrong) to 318.8 (6 tabs,
real) once the race was closed.

**Degenerate cells folded, not silently skipped — corrected accounting.** The full cross-product
is view × state × search = 3 × 4 × 3 = **36 cells**. A prior draft of this section claimed
"16 captured + 20 folded" (= 37, an arithmetic error caught in review); the corrected, cell-by-cell
count is **16 captured + 18 folded + 2 excluded = 36**:

| State | Captured | Folded (proven identical to a captured cell) | Excluded |
|---|---|---|---|
| `loading` (3 views × 3 searches = 9) | 3 (`tree`/`cards`/`archive`, search irrelevant) | 6 | 0 |
| `error` (9) | 3 (same) | 6 | 0 |
| `loaded` (9) | 8 (`tree`×3, `cards`×3, `archive`×2 — match/no-match) | 0 | 1 (`archive`/off) |
| `empty` (9) | 2 (`tree`, `cards` — search irrelevant on an empty result set) | 6 | 1 (`archive`/off-equivalent) |
| **Total** | **16** | **18** | **2** |

The folds are proven from the source, not assumed:
- **`loading`/`error` (6+6=12 folded):** the branches at `abwab-page.component.html:82-115` check
  only `facade.isLoading()`/`facade.errorMessage()`, never `searchQueryParam()` — a search query
  cannot filter a snapshot that does not exist yet, so all 3 search variants of `loading`/`error`
  render identically to the captured (search-agnostic) cell, for all three views.
- **`empty`/`tree`+`cards` (4 folded):** `displayRoots`/`displayArchivedRoots` are already
  length-0 before any search filter runs (`abwab-page.component.ts`'s `visibleRoots` computed);
  filtering an empty set with any query still yields 0 results, so `qd-state variant="empty"`
  renders identically regardless of the search term.
- **`empty`/`archive` (2 folded + 1 excluded-equivalent):** `archivedRoots` is
  `computed(() => this.facade.snapshot()?.archivedRoots ?? [])` — **not** filtered by
  `activeSectionId` at all (unlike `visibleRoots`, which explicitly calls
  `filterAbwabRootsBySection`). The archive view therefore renders the identical global archived
  list regardless of which section — including this phase's deliberately-empty one — happens to
  be active in the URL, so `empty/archive/match` and `empty/archive/no-match` are the same render
  as the already-captured `loaded/archive/match`/`no-match`, and `empty/archive/off` is the same
  unbounded-residue phenomenon as `loaded/archive/off` (below), not independently measured either.

**Two cells excluded, not folded, same reason for both:** `loaded/archive/search-off` and its
`empty/archive/search-off` equivalent — the local dev DB carries ~1,033 archived e2e-residue
doors, an unbounded, ever-growing number unrelated to this phase's reservations, so no equality
claim is made for either.

**The invariant element** is `.abwab-page` (the frame, `data-testid="abwab-page"`) plus
`.abwab-toolbar`, measured via `getBoundingClientRect()` (Playwright `boundingBox()`) after
awaiting `document.fonts.ready` on every capture (same font-swap precaution T502 used).

| Cell | frameHeight (px) | toolbarTop (px) | toolbarBottom (px) |
|---|---|---|---|
| loaded / tree / search off | 892 | 228.796875 | 318.796875 |
| loaded / tree / search match | 892 | 228.796875 | 318.796875 |
| loaded / tree / search no-match | 892 | 228.796875 | 318.796875 |
| loaded / cards / search off | 892 | 228.796875 | 318.796875 |
| loaded / cards / search match | 892 | 228.796875 | 318.796875 |
| loaded / cards / search no-match | 892 | 228.796875 | 318.796875 |
| loaded / archive / search match | 892 | 228.796875 | 266.296875 |
| loaded / archive / search no-match | 892 | 228.796875 | 266.296875 |
| empty / tree | 892 | 228.796875 | 318.796875 |
| empty / cards | 892 | 228.796875 | 318.796875 |
| loading / tree | 892 | 228.796875 | 269.296875 |
| loading / cards | 892 | 228.796875 | 269.296875 |
| loading / archive | 892 | 228.796875 | 266.296875 |
| error / tree | 892 | 228.796875 | 269.296875 |
| error / cards | 892 | 228.796875 | 269.296875 |
| error / archive | 892 | 228.796875 | 266.296875 |

`loaded / archive / search off` is excluded by design, same as T502: the local dev DB carries
~1,033 archived e2e-residue doors, an unbounded, ever-growing number unrelated to this phase's
reservations.

**What this proves (asserted and green — the headline claim, closed):**

1. **`frameHeight` is 892px in all 16 cells, with no exception.** This is the gap B1's own T502
   evidence explicitly left open (item (a): "`loaded` is taller than that same view's own
   `loading`/`error`", scheduled for "**B2 phase 8 (T801/T802)**... §7.2 itself schedules the
   full-matrix run a second time at T1101, after B2 lands, for exactly this reason"). It is now
   closed: no state (loading/loaded/empty/error), view (tree/cards/archive), or search condition
   moves the frame by a single pixel. The viewport reservation (T801/T802) holds under every
   condition this matrix drives, including through the ambient toolbar-height variability found
   below — proof the reservation architecture (`.abwab-page__frame`'s `min-block-size` plus the
   four-link stretch chain) is robust to more than what B1/B2 anticipated. **892px is
   self-explanatory, not a new number to take on faith:** `.abwab-page` is the outer `.qd-page`
   wrapper (`data-testid="abwab-page"`), one level above `.abwab-page__frame`, which carries the
   actual `min-block-size: calc(100dvh - var(--qd-navbar-block-size))` reservation phase 8
   measured at 847px against a 903px viewport. This run's viewport is 900px, so the reservation
   itself is `900 − 56 = 844px`; `.qd-page`'s own `padding: var(--qd-space-5) var(--qd-space-4)`
   (`_layout.scss:74`) adds `2 × 24px = 48px` of block padding **outside** the frame (§4.2's own
   point: the reservation's arithmetic never includes this). `844 + 48 = 892px`, exact.
2. **`toolbarTop` is 228.796875px in all 16 cells, with no exception.** T402's B1-era fix (the
   toolbar stays mounted through every state) still holds after B2's frame/sticky-navbar/stats-bar
   changes landed on top of it. (The absolute value moved from T502's 192.8px to 228.8px — an
   expected, structural +36px from T1003's always-mounted stats bar sitting above the toolbar,
   not a regression.)

**What does *not* hold, measured and reported honestly, not adjusted to pass:**

3. **`toolbarBottom` — i.e. the toolbar's own height — is not invariant across the 16 cells.** It
   takes exactly three values: **266.296875px**, **269.296875px**, **318.796875px**. A coordinator
   review of the first draft of this section correctly flagged that only the 318.8px value had
   been root-caused (the tab-wrap finding below) — the 266.3-vs-269.3 split, a 3px gap living
   entirely inside the toolbar's own box (`toolbarTop` is 228.796875px in **all** 16 cells, so
   none of this 3px comes from anything above the toolbar), had been asserted but not explained.
   It is now.

   **The 266.3-vs-269.3 split, explained and directly measured — not a state defect, not a
   search defect, something else: which children the toolbar's own flex row has.**
   `abwab-toolbar.component.html` gates **both** the tab strip *and* the tree/cards view toggle
   behind the same `@if (!hideSectionControls())` — archive's toolbar therefore renders **one**
   child (the search input alone), never two. `.abwab-toolbar { display: flex; align-items:
   center; flex-wrap: wrap }` sizes each flex line to its tallest child, and the three
   candidate children have different intrinsic heights: `.abwab-toolbar__tabs` (hosting
   `qd-tabs`/`.qd-tabs__tab`, whose own padding is taller) measures **40.5px**; the search input
   and the view toggle both measure **37.5px**. Directly measured (`abwab-tmp-toolbar-height-
   check.e2e.ts`) on a fresh, ambient (no sandbox) load of `/abwab` and `/abwab?archive=1`,
   confirming the arithmetic rather than only deriving it:

   | View | Children present | Measured `.abwab-toolbar` height | `toolbarTop`+height |
   |---|---|---|---|
   | non-archive (`archiveParam()` false) | tabs (40.5px) + search (37.5px) + toggle (37.5px), one line | **40.5px** | 228.796875 + 40.5 = **269.296875** ✓ |
   | archive (`archiveParam()` true) | search only (37.5px) | **37.5px** | 228.796875 + 37.5 = **266.296875** ✓ |

   Both figures match the table's measured `toolbarBottom` values exactly. **This settles the
   coordinator's question directly: the 266.3-vs-269.3 split is not on state and not on search —
   it is on `archiveParam()`.** Within archive, `toolbarBottom` is 266.3px for `loading`, `error`,
   and both captured `loaded` cells alike — genuinely constant across every state, because
   `hideSectionControls` depends only on `archiveParam()`, never on the lifecycle. Within
   non-archive, the same 3px comparison holds only between `loading` and `error`: both measure
   269.3px identically (the "all doors" tab always renders, snapshot or not, and one tab alone
   never wraps). **This is not a claim that non-archive `toolbarBottom` is state-independent
   overall** — it visibly is not: `loaded`/`empty` measure 318.8px in the same view, a real
   escalation the wrap paragraph below explains. The distinction is between two different
   mechanisms on the same flex row: the **3px gap** (`.qd-tabs` vs `.qd-input`'s own intrinsic
   height) is driven by `archiveParam()` and is state-independent by construction; the further
   **49.5px escalation** to 318.8px is driven by tab *count*, which happens to correlate with the
   lifecycle only because `sections()` is empty until the snapshot arrives — the same phenomenon
   as the wrap finding below, not a third, independent cause.

   This 40.5px-vs-37.5px gap is itself a real, small, **pre-existing** styling detail — `qd-tabs`'
   own vertical padding renders 3px taller than `.qd-input`/the toggle buttons — not introduced or
   touched by any of B1/B2's seven deliverables (`abwab-toolbar.component.html/scss` is untouched
   by this slice; `qd-tabs`, `.qd-input`, and the toggle button styles are shared/pre-existing
   primitives). It is the same underlying mechanism (which children exist, and their own
   intrinsic heights, on a `flex-wrap: wrap` row) that also explains 318.8px below — not two
   unrelated causes, one continuum.

   **Verdict on the 3px, per the coordinator's own branch condition:** it does not split on
   state, so it is **not** an N3/§17 violation requiring "fix the code, not the assertion" — it is
   a **scoped, named exception**, not a code defect. A user only crosses the archive/non-archive
   boundary by deliberately clicking the archive toggle, which is a full content change (tabs and
   the tree/cards toggle are meant to disappear — "the archive view has no live section grouping",
   `abwab-toolbar.component.ts`'s own doc comment), the same class of allowed content difference
   as switching `tree`↔`cards`. `toolbarTop` (228.796875px) and `frameHeight` (892px) are
   pixel-identical across that transition — nothing above or around the toolbar moves; only the
   toolbar's own box legitimately reflects which controls are actually present.

   **The 318.8px wrap case — corrected after the same review caught a wrong first draft.** It is
   *not* the tab strip itself wrapping onto two lines. A direct DOM measurement of
   `.abwab-toolbar__tabs` (`getBoundingClientRect().height`) is **40.5px in both the 1-tab and the
   6-tab case** — the tab row never grows past one line. What wraps is the **outer**
   `.abwab-toolbar` (`display: flex; flex-wrap: wrap`, `abwab-toolbar.component.scss:1-6`), which
   holds the tab strip, the search input (`min-inline-size: 12rem` = 192px), and the view toggle
   as three flex children on one row. The tab strip's own width grew from 84px (1 tab) to 1160px
   (6 tabs,
   4 of them long e2e-generated names) against a 1361px toolbar width — leaving only 201px for a
   search box that refuses to shrink below 192px plus a 117px toggle plus two 12px gaps (201px <
   192 + 117 + 24 = 333px needed), so the search box and toggle wrap onto their own line below the
   tabs — measured, not estimated: `318.796875 − 269.296875 = 49.5px` added, which decomposes
   exactly into the search box's own 37.5px height plus one 12px `--qd-space-3` row gap. Measured
   threshold: wrap triggers once the tab strip alone exceeds `1361 − 192 − 117 − 24 = 1028px`.

   **Verified against the current, near-realistic ambient database, not just derived:** a direct
   measurement (temporary, deleted immediately after) of `/abwab` with **no sandbox created** —
   i.e. only the app's real data plus the one pre-existing ambient residue section this harness
   did not create — found 4 tabs totaling **621px** of tab-strip width (`كل الأبواب` 84px +
   `اللغات` 62px + `الانبياء` 61px + the 51-character residue name `e2e-sandbox-w0-…-layout-
   empty-section` 414px), well under the 1028px threshold: **the toolbar does not wrap**, single
   line, same 40.5px height regardless of loading/loaded. With the two real sections alone
   (84 + 62 + 61 = 207px) the margin is even larger. **This means the 318.8px cells above are not
   a defect that reproduces on the real product data today** — they are an artifact of this
   harness's own two long, timestamp-named sandbox tabs (T502's own precedent for creating a
   sandbox section, unavoidable per §7.2's own instruction to reach the `empty` macro-state) pushing
   the tab strip past a threshold that 3 real, short-named tabs sit nowhere near. The honest
   characterization is a **latent capacity limit**, not an active regression: `.abwab-toolbar`'s
   `flex-wrap: wrap` has no cap on section count or name length, so a real deployment that
   accumulates enough sections (or one long enough section name) could still hit it — but nothing
   B1/B2 shipped changed this threshold or this mechanism; `qd-tabs` and `.abwab-toolbar`'s own
   `flex-wrap: wrap` were not touched by any of items 1/2/3/4/5/6/17.

   **What B1/B2 did change is when the toolbar-growth window exists at all.** Before T402, the
   `loading`/`error` branches replaced the entire toolbar+tree subtree with one line of text — the
   toolbar was not present to grow or shrink at all, so this intra-toolbar shape change was
   invisible (masked by the far larger vanish-and-reappear shift). T402 (phase 4, B1) mounted the
   toolbar unconditionally through every state, which is what makes the `sections()`-arrives
   transition (and therefore this capacity limit, on a database that happens to be over it) newly
   *observable* — visible now because the far larger shift is gone, not introduced by B1/B2. And
   whenever it does occur, T801/T802's frame reservation (point 1, above) absorbs all of it: the
   *frame* still never moves; only the toolbar/content split inside it does, since
   `.abwab-page__layout` is `flex: 1; min-block-size: 0`. Flagged for whoever next revisits
   abwab's toolbar or the `qd-tabs` primitive (a capacity cap, a tab-name length limit, or a
   narrower search `min-inline-size` at this viewport would all close it); not fixed here, both
   because it sits outside this phase's seven-deliverable scope and because T1103's hard bounds
   forbid unscoped source edits.

   Distinct from the archived-doors residue note above: the extra *section* driving this
   (`e2e-sandbox-w0-1785413500729-layout-empty-section`, id 362) is confirmed pre-existing, not
   created by this phase's four harness runs — verified via `GET /api/abwab/tree` before and
   after each run, with the one section this phase's own first, failed run accidentally left
   behind deleted by hand before the final measurement run.

**Skeleton row height vs. loaded row height — B1's T502 residual, re-measured as promised:**

| Quantity | Value |
|---|---|
| Loaded tree row (`.abwab-tree__row`) height | 32px |
| Skeleton row (`qd-skeleton-row-0`) height | 24px |

T502 predicted, but could not re-measure in the browser (the dev server had already been torn
down), that the `--qd-skeleton-h: 1.5rem` fix landed in `abwab-page.component.scss` during B1
phase 5 would produce a 24px skeleton row against the 32px loaded row, closing to **pitch**
parity (24px bar + 8px `--qd-space-2` gap = 32px, matching the loaded row's own pitch) while
leaving the row's own **box** 8px short — an unparameterized-gap residual, not a caller-fixable
defect. Measured here: **exactly 24px**, confirming the derivation held. The 8px gap between
24px and 32px is the same residual T502 already named and left to whoever next edits
`shared/ui/skeleton/` to parameterize the gap; `.abwab-page__tree-card` still absorbs it. Also
confirmed live (measured pixel-identical across `loaded`/`loading`/`error`/`empty` in the table
above): **the reserved error slot does not change the frame's height when it fills** — `frameHeight`
is 892px whether the error `qd-state` is present or not.

### T1102 — Tier B gate (post-phase-10, no source changes)

Commands run from `Frontend/quran-dashboard-ui/`, script invoked as-is (the
`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap baked into `npm test` was preserved, no direct
`ng test` call was made):

```bash
npm test
```

Result: **191 spec files passed (191) / 2167 tests passed (2167) / 0 failed.**

```
Test Files  191 passed (191)
     Tests  2167 passed (2167)
   Start at  19:25:39
   Duration  168.04s
```

**Delta vs T601 (191 files / 2164 tests): +0 files, +3 tests — exactly matching phase 10's own
close, independently re-confirmed here.** The three additive tests are, as phase 9/10 already
recorded: `scroll-lock.service.spec.ts`'s `isLocked` case (T904, phase 9, +1),
`abwab-tree.builder.spec.ts`'s "item 17 stats bar" case (T1002, phase 10, +1), and
`abwab.labels.spec.ts`'s locked-strings pin for the two new stat labels (T1004, phase 10, +1).
Within the plan's own **+2–4** expectation for all of B2 combined (§7 phase 11 bullet). No spec
file was added or removed between phase 10's close and this run — the working tree was clean
before this phase's own single doc-repoint edit (below), and Vitest does not touch docs.

```bash
npm run build
```

Result: **green.** `ng build` completed in 15.012s, output at
`Frontend/quran-dashboard-ui/dist/quran-dashboard-ui`. Same four pre-existing budget categories
as phase 10's close, **byte-for-byte unchanged** — expected, since this phase made zero edits to
any file `ng build` compiles (the harness lived only under `e2e/`, outside the build, and was
deleted before this gate ran):

- initial bundle exceeded the 500.00 kB budget by 69.16 kB (569.15 kB total) — unchanged from phase 10
- `selected-word-section.component.scss` exceeded its 4.00 kB budget by 649 bytes — unchanged
- `selected-ayah-section.component.scss` exceeded its 4.00 kB budget by 1.85 kB — unchanged
- `abwab-relations-modal.component.scss` exceeded its 4.00 kB budget by 1.08 kB — unchanged

No new warning categories. No errors. `git status --short` was empty in both `Frontend/quran-dashboard-ui/`
and repo root immediately before this gate ran, confirming no stray edits (including the temp
harness, already deleted) were present.

### T1103 — dangling-reference sweep, including prose

`grep -rn` across code, tests, e2e, skills, docs, READMEs, `.specify/`, `.architecture/`,
`docs/contracts/` for everything B2 moved or renamed, per the plan's own list, plus every
`file:line` citation into the six named files (`_layout.scss`, `_words-explorer-layout.scss`,
`_tokens.scss`, `app.ts`, `top-navbar.component.*`, `scroll-lock.service.ts`, and the two abwab
pages), plus prose (Slice A's own lesson: a class-name/literal grep alone missed a stale prose
claim in `UI_STYLE_SYSTEM.md` §4 last time).

| # | Item swept | Disposition |
|---|---|---|
| 1 | `.qd-explorer-frame` (renamed to `.qd-page-frame`, alias kept) | **Clean.** Definition site (`_layout.scss`), all five explorer HTML call-sites, and four READMEs all describe the current alias correctly. No stray references outside `docs/abwab-ux-audit.md` / `docs/feature-ux-slice-a/plan.md` / this slice's own `plan.md` (all frozen, below). |
| 2 | `explorer-result-count`'s old path (`features/words/components/explorer-result-count/`) and `WORDS_RESULT_COUNT_LABELS` (renamed `RESULT_COUNT_LABELS`, moved to `shared/ui/result-count/result-count.labels.ts`) | **Clean.** No live code or README references the old path or the old constant name; both only appear inside this slice's own `plan.md`/`evidence.md` (frozen/historical). Verified the new location exists (`shared/ui/result-count/{explorer-result-count.component.*, result-count.labels.ts}`) and the selector is `qd-result-count, qd-explorer-result-count` as documented. |
| 3 | `--qd-mushaf-sticky-top`, `--qd-mushaf-panel-height`, `--qd-z-sticky`, `--qd-z-mobile-nav` | **Clean.** All live citations (`_tokens.scss`'s own comments, `UI_STYLE_SYSTEM.md` §4/§17, `styles/README.md`, `features/abwab/README.md`, the two live source files that consume them) describe the current, post-T902/T903 arithmetic and rung correctly. Citations with specific line numbers into these tokens exist only in frozen docs (below). |
| 4 | Every `file:line` citation into `_layout.scss`, `_words-explorer-layout.scss`, `_tokens.scss` | **Clean.** No live doc (`styles/README.md`, `UI_STYLE_SYSTEM.md`, `shared/README.md`, feature READMEs) cites these three files with a specific line number at all — they're referenced by name/section only, which cannot go dangling. All line-numbered citations exist only in frozen docs (below). |
| 5 | Every `file:line` citation into `app.ts` | **Clean.** `app.ts:14` is the only line cited anywhere, and `app.ts` itself is unchanged by B2 (confirmed: line 14 is still the `[attr.inert]`/`[attr.aria-hidden]` pairing). Cited correctly in `UI_STYLE_SYSTEM.md` §17, `top-navbar.component.ts`'s own new comment, and `features/abwab/README.md`. |
| 6 | Every `file:line` citation into `top-navbar.component.*` | **`top-navbar.component.html` gained 6 lines and `.ts` gained ~8** (T901's `[attr.inert]`/`[attr.aria-hidden]` bindings, T904's `ScrollLockService` injection/comment), shifting every line number below the insertion point. **No live doc cites a specific line number into either file** — the only line-numbered citations found (`top-navbar.component.html:44-60`, `:62-74`, `:8-61`, `:286-300`, `:8`; `top-navbar.component.ts:29-31,82-94`, `:45-56`, `:58-71`) are in `docs/abwab-ux-audit.md` (frozen, never swept) and `docs/feature-ux-slice-a/plan.md` (closed feature, frozen) and `docs/feature-033-auth-roles-permissions/{plan.md,decision-record.md}` (a **different, closed feature**, frozen by the same general convention — flagged explicitly since it is outside this slice's own history but was caught by this sweep's file-scoped grep). None needed repointing because none are live. |
| 7 | Every `file:line` citation into `scroll-lock.service.ts` | **Clean.** All citations (`:14`, `:9-31`, `:13-31`, `:13-22`) are in `docs/abwab-ux-audit.md` (frozen) and the two slices' own `plan.md` files (this slice's own is frozen per phase 7's convention; Slice A's is a closed, frozen feature). No live doc cites a line number into this file. |
| 8 | Every `file:line` citation into `abwab-page.component.*` and `abwab-templates-page.component.*` | **One dangling reference found and fixed** (below); everything else clean. |
| 9 | `docs/feature-abwab-templates/plan.md:680` — `AbwabPageComponent.ngOnInit`/`facade.load()` cited as `abwab-page.component.ts:155-156` | **Fixed (dangling).** `abwab-page.component.ts` gained lines across phases 8–10 (computed signals, label getters, imports); the real location is now `:171-172`, confirmed by reading the file. `abwab-templates` is a currently-open feature (root `CLAUDE.md` Active Spec Kit Feature), so its plan is a live document, not frozen evidence — same disposition B1's own T504 gave this exact file for a different line. |
| 10 | `docs/feature-abwab-templates/plan.md:787-788` — «القوالب» entry cited as `abwab-page.component.html:12-38` | **Verified, not dangling.** The cited element (`data-testid="abwab-page-templates"`) is at lines 29–31 of the current file, inside the cited 12–38 range. Header block itself unshifted by B2 (T801 only appended a class name to line 2; T1003's stats-bar insertion landed *after* line 42, outside this range). |
| 11 | `features/abwab/README.md:211` — `abwab-page.component.html:2` / `abwab-templates-page.component.html:2` | **Verified, not dangling.** Both files' line 2 still reads `qd-container qd-page-frame` (plus `abwab-page__frame` on the doors page), confirmed by reading both files. |
| 12 | `docs/feature-abwab-global-order/plan.md:353` and `Backend/report/feature-abwab-global-order/003-phase3-contract-regeneration.md:39` — both cite `abwab-page.component.ts:224` | **Found, not repointed — frozen by design.** `abwab-global-order` is not in root `CLAUDE.md`'s Active Spec Kit Feature list, i.e. a closed feature; its `docs/feature-abwab-global-order/` and `Backend/report/feature-abwab-global-order/` folders are frozen snapshots, same disposition this phase's own instructions name for `docs/abwab-ux-audit.md` and other closed features. Explicitly not silently skipped: found, named, left alone. |
| 13 | Prose check (Slice A's own lesson — a literal/class-name grep alone missed a stale prose claim in `UI_STYLE_SYSTEM.md` §4 last time) | **Clean.** Re-read `UI_STYLE_SYSTEM.md` §4's layer-scale prose in full: it correctly states `.qd-navbar` sits at `--qd-z-mobile-nav` alongside its dropdown/mobile-menu and that `--qd-z-sticky` is for "in-page sticky headers **with no descendant menus of their own**" — this is the exact rung-choice reasoning T903 arrived at, written accurately, not left describing the plan's original (superseded) intent of putting the navbar on `--qd-z-sticky`. Also re-read `styles/README.md`'s `_layout.scss`/`_tokens.scss` bullets and `features/abwab/README.md`'s "Gotchas" section in full prose (not just grepped) — both correctly describe the `--qd-z-mobile-nav` resolution, the `display: contents` fix, and the viewport reservation's scope (doors page only). |
| 14 | `docs/contracts/`, `.specify/`, `.claude/` skills | **Clean.** No hits for any moved item in any of these locations. |

**Left as-is, by design (the established convention, followed explicitly rather than silently
skipped):** `docs/abwab-ux-audit.md` (cross-cutting audit spanning multiple slices, never swept —
root `CLAUDE.md`'s lifecycle rule), `docs/feature-ux-slice-a/{plan.md,evidence.md}` (closed
feature, frozen), `docs/feature-ux-slice-b/plan.md` (this slice's own plan — §5 is explicitly "as
measured at plan time", a frozen snapshot per the convention phase 7's own T704 sweep already
established and is not re-litigated here), `docs/engineering-review-full-project-2026-07-18.md`
(a dated, point-in-time engineering-review report — evidence whose facts are not restated by a
live document, per root `CLAUDE.md`'s evidence-preservation rule), `docs/feature-abwab-global-order/`
and its `Backend/report/` counterpart (closed feature), `docs/feature-033-auth-roles-permissions/`
(a different closed feature, caught incidentally by the `top-navbar.component.*` sweep).

## §9 obligations checklist — the B2 half, walked item by item

Per the plan's own instruction, two items were resolved differently from the plan's literal text
during execution, already escalated and documented at the phase that made the call. They are
**verified below, not re-decided.**

| # | Item | TRUE? | Evidence |
|---|---|---|---|
| 1 | `.qd-page-frame` shipped with `.qd-explorer-frame` as a working alias; five explorer call-sites untouched and green | **TRUE** | Phase 7 T701/T702/T704; re-verified this phase (T1103 #1): dual-selector rule in `_layout.scss`, five explorer HTML call-sites unchanged, Tier B green throughout. |
| 2 | Abwab full-bleed on both pages; the two §5.1 flex caveats checked in the browser and recorded | **TRUE** | Phase 7 T703: `getComputedStyle`/`getBoundingClientRect` browser verification, both caveats (column-flex-frame-vs-row-layout, mobile-stat-bar `padding-block-end`) checked and recorded with live measurements. |
| 3 | The viewport reservation ships with `box-sizing: border-box` proven present | **TRUE** | Phase 8 T801: source grep + live `getComputedStyle` in the same browser session, `boxSizing: "border-box"` confirmed on the element carrying both `.qd-page-frame` and `.abwab-page__frame`. |
| 4 | Sticky navbar on `--qd-z-sticky`; both re-based sticky offsets (mushaf + abwab side panel) verified flush | **TRUE, with the documented deviation.** | The navbar is sticky and both offsets (mushaf `68px`, abwab side panel `72px`) are verified flush — phase 9 T901/T902. **Deviation #1** (already escalated and resolved at phase 9, not re-litigated here): the navbar itself ships on `--qd-z-mobile-nav` (45), not `--qd-z-sticky` (5) — `--qd-z-sticky` was found live to clamp the navbar's own dropdown/mobile-menu beneath page popovers and the `detail-modal-shell` restore control, an app-breaking regression the plan's literal text did not anticipate. `UI_STYLE_SYSTEM.md` §4/§17, `styles/README.md`, and `features/abwab/README.md` all describe the resolved rung accurately (re-read in full at T1103 #13, not just grepped). |
| 5 | `--qd-mushaf-panel-height` re-checked for double subtraction | **TRUE** | Phase 9 T902/T903: checked, no double-subtraction found. A *different*, adjacent single-omission gap (the formula ignored `--qd-mushaf-sticky-top`'s extra `--qd-space-3`) was found and fixed in the same pass — recorded distinctly, not conflated with the thing that was checked and found clean. |
| 6 | Navbar dropdowns still escape the new stacking context | **TRUE, this is deviation #1 itself.** | Initial finding at `--qd-z-sticky`: failed (dropdown trapped by the navbar's own stacking context, confirmed via `elementFromPoint()` probes and an isolated repro). Escalated, resolved by moving `.qd-navbar` to `--qd-z-mobile-nav`. Four live re-verification cases all passed post-fix (T903): dropdown vs. a `--qd-z-floating` probe, dropdown-rung vs. the real `detail-modal-shell__restore` control, `.mobile-menu` vs. a popover, sticky navbar vs. a popover on a scrolled page. `docs/feature-ux-slice-b/plan.md`'s own text (§7 phase 9 bullet, §9 line 700) still says `--qd-z-sticky` — that is the frozen plan-time snapshot (T1103 #6 disposition), not a live claim; the live docs (`UI_STYLE_SYSTEM.md`, both READMEs) all state the resolved rung correctly. |
| 7 | `ScrollLockService` exposes lock state; navbar inert + `aria-hidden` paired as `app.ts:14` does; `app.nested-layers.spec.ts` run and the inert-inside-inert observation recorded | **TRUE** | Phase 9 T904: `isLocked` computed signal added (specced, `scroll-lock.service.spec.ts` +1 test); `[attr.inert]`/`[attr.aria-hidden]` pairing on `.qd-navbar` copies `app.ts:14` exactly; `app.nested-layers.spec.ts` run 4/4; inert-inside-inert state observed live on the real DOM (both `qd-app-shell[inert]` and `.qd-navbar[inert]` simultaneously non-null with a words drawer open under the global overlay), not only asserted by the spec. |
| 8 | `qdModalScrollLock` added to `abwab-sections-modal` and `abwab-move-picker` — named as an intentional behavior change, not slipped in | **TRUE** | Phase 9 T905: added to both, verified live (both now lock body scroll and inert the navbar); named explicitly as one of "two intentional behavior changes" in both the evidence log and `features/abwab/README.md`'s Gotchas section. |
| 9 | `qd-result-count` promoted to `shared/ui/`, alias selector in place, TDZ getter idiom preserved, spec moved with it | **TRUE** | Phase 10 T1001; re-verified this phase (T1103 #2): `shared/ui/result-count/` holds the component, its spec, and `result-count.labels.ts`; selector is `qd-result-count, qd-explorer-result-count`; the TDZ-safe getter is unchanged in shape, only its import target moved. |
| 10 | Both stats derived from the snapshot with no backend call added; the live-only definition and the nullable-section caveat both written down | **TRUE** | Phase 10 T1002; re-verified this phase: `abwab-tree.builder.ts`'s two new functions read only `byId`/`sections`, no new `AbwabApi`/`HttpClient` import anywhere this phase touched; `features/abwab/README.md`'s stats-bar paragraph (re-read in full at T1103 #13) states the live-only choice and the `Σ doorsInScopeCount ≤ total` caveat explicitly. |
| 11 | Arabic counted-noun forms used for both stats; «كل الأبواب» has its own copy | **TRUE for the half that applies; the other half is deviation #2, already documented.** | «كل الأبواب» (`allDoorsTab`) and the second stat's label (`statOpenScopeDoors` = «أبواب هذا التبويب») are two distinct static strings — «كل الأبواب» does have its own copy, confirmed via the live screenshot text in phase 10's evidence ("كل الأبواب:13" / "أبواب هذا التبويب:13"). **Deviation #2:** neither label routes through `countPhrase`'s forms tables, because `qd-result-count` always renders `"{{ labelPrefix() }}: {{ count() }}"` — feeding a counted-noun phrase into `labelPrefix` would print the digit twice ("12 بابًا: 12"). Phase 10's own T1004 section states this reasoning in full (including that the advisor was consulted before writing the task), and `features/abwab/README.md`'s stats-bar paragraph states it too ("Neither label goes through `countPhrase`... not a counted-noun sentence, so the bare-count rule below does not reach it") — a deliberate, reasoned exception, not an oversight, and it is written down where the next reader will find it. |
| 12 | Three `UI_STYLE_SYSTEM.md` entries written (viewport reservation, sticky chrome, chrome-inert) plus the `qd-result-count` §17 entry and the §2 frame amendment | **TRUE** | All five confirmed present by heading this phase (`### Viewport reservation`, `### Sticky app chrome`, `### Chrome-inert rule`, `### qd-result-count`, plus §2's "Current state" paragraph naming the rename). |
| 13 | `styles/README.md`, `shared/README.md`, `features/words/README.md`, `features/abwab/README.md` all amended | **TRUE** | All four re-read in full this phase (T1103 #13) and confirmed to describe B2's changes accurately — the frame rename/alias, the sticky navbar and its rung, the viewport reservation's scope, the chrome-inert rule, the `qd-result-count` promotion, and the stats bar's two definitions. |
| 14 | §7.2 acceptance run at T1101 across the full matrix | **TRUE, run — with a mixed, honestly-reported result, not an unqualified pass.** | This phase's own T1101, above: the 3×4×3=36-cell matrix was run as 16 captured cells (18 folded as structurally identical — proven from source, not assumed — and 2 excluded per the T502 precedent). `frameHeight` and `toolbarTop` are invariant across all 16 with no exception — the headline claim. `toolbarBottom` is **not** invariant (three values); both root causes were measured directly (not derived) and both are scoped, named exceptions, not N3/§17 defects: the 266.3-vs-269.3px 3px gap splits on `archiveParam()` (which toolbar children render — a deliberate content difference on user-initiated archive toggle, not a state-transition shift), and the 269.3-vs-318.8px 49.5px escalation is a pre-existing `qd-tabs`/toolbar capacity limit tied to ambient tab count that does not reproduce on the app's real, near-realistic ambient data (verified by direct measurement). Neither splits on loading-vs-loaded state or on the search query. See T1101 in full for the honest accounting. |
| 15 | T1102 delta explained (expected +0 files, +2–4 tests); T1103 grep clean including prose | **TRUE** | T1102 above: +0 files, +3 tests, inside range. T1103 above: swept including prose, one dangling reference found and fixed (`docs/feature-abwab-templates/plan.md`), everything else clean or frozen-by-design. |
| 16 | Root `CLAUDE.md` "Active Spec Kit Feature" updated at B1 start and cleared at B2 close; `docs/feature-ux-slice-b/` retained while the UX series is open | **NOT YET — deferred to merge, by this phase's own explicit instruction, not an oversight.** | The entry was updated at B1 start (T102) and is still present, unmodified, in root `CLAUDE.md` as of this phase. Per this phase's task instructions: "§9 says it is cleared at B2 close. B2 closes when this branch merges, not now; the branch is not merged yet... Leave the entry in place, but flag in your report what the main thread should do at merge time." Left in place, not cleared. **Flag for the main thread:** when `ux-slice-b2-frame` merges into `dev`, clear the `ux-slice-b` line from root `CLAUDE.md`'s "Active Spec Kit Feature" section (leaving `abwab-templates`, which stays open). `docs/feature-ux-slice-b/` itself is correctly retained per the Slice A precedent and is not touched by this instruction. |

**Summary: 15 of 16 items TRUE as of this phase's close; item 16 is intentionally deferred to
merge time, not failed.** No item is FALSE.
