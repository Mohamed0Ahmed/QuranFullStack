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
