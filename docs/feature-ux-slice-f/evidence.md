# Slice F — Evidence

Plan: `docs/feature-ux-slice-f/plan.md`. Branch: `ux-slice-f-sections`, off `dev` @ `7b0e8fba`.

## T101 — Baseline (dev @ `7b0e8fba`, clean)

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build Backend/QuranDashboard.sln` | Build succeeded, 0 warnings, 0 errors, 35.8s |
| No-pipeline regression | `dotnet test … --filter "...!~QuranDashboard.Tests.Smoke."` (§5 catalog) | 1086 passed, 0 failed, 0 skipped, 22s |
| Route-smoke tier | `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` | 140 passed, 0 failed, 0 skipped, 49s |
| **`Tests.Smoke.Data` — RAN** | `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke.Data"` | 13 passed (subset of the 140 above). `resources/db-dumps/quran-canonical/quran-canonical.dump` is present, so the fixture did not self-skip. |
| `check-api-contract` | `Backend/scripts/check-api-contract` | "API contract up to date." — clean at baseline; `git status --short` empty after the run. Run so a later staleness report can be attributed to this slice's own changes, not pre-existing drift. |
| Frontend tests | `npm test` | 193 files, 2316 tests passed, 0 failed. 202.32s (TESTING_STRATEGY.md's catalog figure of 191 files / 2161 tests is the prior measurement point — both counts have since grown; no contradiction). |
| Frontend build | `npm run build` | Succeeded, 17.9s. Pre-existing budget warnings (initial bundle +69.54 kB over 500 kB budget; two mushaf SCSS files over their 4 kB budget) — none introduced by this slice, carried forward as-is. |

**Baseline verdict:** both stacks green. Nothing outstanding to disentangle from this slice's own changes.

## T102 — Branch and feature record

- Branch `ux-slice-f-sections` created off `dev` @ `7b0e8fba`.
- Plan committed to the branch (not to `dev`).
- Root `CLAUDE.md` "Active Spec Kit Feature" section updated: `ux-slice-e` entry (merged) replaced with `ux-slice-f`. `docs/feature-ux-slice-e/` left untouched — no planning-artifact sweep in this slice (§3).

## Phase 2 — T201-T205 (backend route/writer/wiring/catalog)

- `dotnet build Backend/QuranDashboard.sln` — succeeded, 0 warnings, 0 errors (after retrying past one transient MSB3883 file-lock error from a lingering build-server process, unrelated to this change).
- `Tests.Abwab` — 46 passed, 0 failed (matches plan's precondition count — unchanged, no new backend tests per the rush-period posture).
- `Tests.Api` — 60 passed, 0 failed (matches §5's catalog figure).

## Phase 3 — T301 (route gate)

- `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` — **140 passed, 0 failed, 0 skipped**, 47s. Same total as the T101 baseline: `SmokeCoverageParityTests` asserts both parity directions (every registered route has a catalog entry, every catalog entry maps to a registered route) as a fixed handful of tests over the whole catalog, not one test per route, so an unchanged total together with a passing run is the expected signal that the new `POST api/abwab/sections/{id:int}/order` entry landed correctly in both directions.
- **`Tests.Smoke.Data` — RAN** (subset of the above; same `resources/db-dumps/quran-canonical/` dump as baseline, unchanged).

## Phase 5 — T501-T502 (frontend write path)

- `AbwabSectionsModalComponent` now declares `reorderSection` as a required input (T601's button/editor UI is the consumer; declaring the signature now is what T501's "bound into the modal" step requires — Angular's `strictTemplates` rejects a binding to an unknown property, so the input had to land in this phase, not deferred whole to Phase 6). The existing 15-case modal spec's shared `render()` helper got a matching stub so no existing case broke.
- Focused glob `npm test -- --include="src/app/features/abwab/**/*.spec.ts"` — **371 passed** (366 baseline-for-this-glob + 5 new: 1 in `abwab.api.spec.ts`, 4 in `abwab-write.controller.spec.ts`), 0 failed, 24 files. `abwab.api.spec.ts` asserts the POST verb, URL, and `{ position, version }` body; `abwab-write.controller.spec.ts` asserts success refreshes the snapshot, 409 maps to `conflict` with the backend message, 400 maps to `invalid`, and the door selection is untouched (a section write invalidates no door selection, matching `renameSection`).

## Phase 6 — T601-T603 (sections-modal order editor)

- Editor state machine per §T601's table: idle (a real `<button>` chip, Arabic `aria-label` naming the section and its order), editing (`<input type="number" min="1">` seeded from the live `sections()` row), submitting (`editingOrderId` cleared before the call, same ordering as the tree's `commitOrderEdit`), error (the existing single error strip). `onOrderKeydown` opens with `event.stopPropagation()` (§4.2-9, mandatory — the dialog binds `(keydown.escape)="requestClose()"`). Focus-in uses the plan's named mechanism (`#orderInput` + `viewChild` + `afterNextRender({ injector })`); focus-out (back to the row's own button) is scoped by this row's `data-testid` through the host `ElementRef`, since a bare `viewChild('orderButton')` would be ambiguous — every row not being edited renders that ref simultaneously. `editingOrderId` is a signal separate from `editingId`, and `isDirty` does not read it — an open order edit is not unsaved work (§4.2-15).
- One existing spec cell's expectation moved deliberately: "traps focus… with the first control inside the dialog" now points at `abwab-sections-modal-order-1`, because the order trigger is a real `<button>` (§4.2-16, not the tree's `<span>`) rendered before rename/delete in DOM order — it is now genuinely the row's first focusable control.
- Focused glob — **378 passed** (371 + 7 new cells: click opens seeded; Enter commits `(id, position, liveVersion)`; Escape cancels and the modal stays open; blur cancels without submitting; a non-integer/zero input submits nothing; a 409 renders the error strip; `resetDraft` on reopen clears an open order edit), 0 failed, 24 files.

## Phase 7 — T701-T703 (tab count badges)

- T701 ripple check re-run at execution time: `grep -rln AbwabTreeSnapshotVm src/app` still resolves to exactly the same 5 files the plan measured (model, builder, facade, selection store, README) — no drift, delta holds.
- `rootCountBySectionId` built in the same pass as `liveRoots` in `buildAbwabTreeSnapshot`; carried on `AbwabTreeSnapshotVm`. Builder spec: 22 passed (18 + 4 new — counts roots only, excludes archived roots, omits rather than zero-defaults a section with no roots, and pins the Σ-can-be-less-than-`liveRoots.length` non-identity).
- `.qd-tabs__count` rendered at the call-site in `abwab-toolbar.component.html` on both the «كل الأبواب» button and each section tab (`qdTab` itself untouched — DRIFT-4); `.qd-tabs__count--empty { opacity: 0.5; }` added beside the shipped selected-state block in `_components.scss`, composing rather than forking because that rule sets only `background`/`color`. Two existing tab-label assertions (`abwab-toolbar.component.spec.ts`, `abwab-page.component.spec.ts`) moved from reading a tab's full `textContent` to its leading text node, since the count `<span>` is now a sibling in the DOM.
- `ROOT_DOOR_FORMS` (four Arabic forms, no zero form — the zero-state is a visual dim, not a distinct phrase) plus `tabRootCountAriaLabel`/`allDoorsTabRootCountAriaLabel` in `abwab.labels.ts`; the badge's visible digits are `aria-hidden`, the tab's own `aria-label` carries the counted-noun phrase. `abwab-page.component.ts` derives `rootCountBySectionId` and `totalRootCount` (`liveRoots.length`, **not** a sum over the map) from the facade snapshot and binds both into the toolbar.
- Focused glob — **393 passed** (378 + 15 new: 4 builder, 4 toolbar, 7 labels), 0 failed, 24 files. `npm run build` — succeeded, 19.3s, same two pre-existing budget warnings as baseline, none new.

## Phase 8 — T801-T802 (docs true again)

- `Backend/…/Persistence/Writes/Abwab/README.md` — sections-writer key-piece line becomes "create / rename / reorder / delete-empty"; endpoint count "twenty" → "twenty-one" (both places it appeared, matching the Controllers README below in the same task); a new invariant bullet records the whole-table `1..N` resequence, the `(OrderValue, Id)` reader-order deviation and why, and the duplicate-`OrderValue` condition the reorder heals (DRIFT-3, cross-referenced to `docs/TESTING_DEBT.md` F1/F2).
- `Backend/api/QuranDashboard.Api/Controllers/README.md` — abwab paragraph's endpoint inventory gains the reorder route; "twenty write routes" → "twenty-one", "Twenty-four routes in all" → "Twenty-five".
- `Backend/…/Persistence/Reads/Abwab/README.md` — **verify-only, confirmed true.** `DoorsInScopeCount`'s recorded semantics (all-depths, live-only) are unchanged: item 19 adds a client-side, root-only count derived in the builder and touches no reader. No edit.
- `docs/contracts/http-api.md` — **verify-only, confirmed true.** Pointer-only by construction ("does **not** restate routes, parameters, or payloads") and its precedence note already defers to the controller + `Controllers/README.md`; a twenty-fifth route needs no edit here. No edit.
- `features/abwab/README.md` — sections-modal paragraph gains the order editor, its grammar, and the Escape-guard rationale (the dialog's own `(keydown.escape)` binding); "three write functions" → "four"; toolbar paragraph gains the count badge (root-only, composing `.qd-tabs__count`, `qdTab` untouched) and notes the archive view carries no badge because it carries no tab strip; the stats-bar paragraph gains a sentence forbidding an assertion that the badge and item 17's stat agree; the refresh-after-write paragraph names the section reorder as the feature's second table-wide resequencer, after the doors' `Global` reorder.
- `.architecture/UI_STYLE_SYSTEM.md` §17 — the `qd-tabs` entry gains a count-meta bullet: rendered by the call-site (the directive cannot project a child element), Latin digits with `tabular-nums`, always rendered and dimmed at zero via `--empty` (opacity only), visible digits `aria-hidden` with the accessible name carried by the tab.
- `docs/TESTING_DEBT.md` — new `ux-slice-f` section (2026-08-01) with rows F1 (writer behavior, uncovered anywhere), F2 (the duplicate-`OrderValue` condition itself), F3 (section-reorder smoke, `ParityOnly`).
- Preliminary grep sweep (`twenty write`, `20 write`, `create / rename / delete-empty`) — the only remaining hits are closed-feature planning docs (`docs/feature-abwab-doors/plan.md`, `docs/feature-abwab-templates/plan.md`) and this slice's own `plan.md`, all historical records left as-is per repo law. T903 repeats the full sweep (including `doorsInScopeCount`) at close-out.

## Phase 9 — T901 (Tier C against the T101 baseline)

| Check | Baseline (T101) | Close (T901) | Delta |
|---|---|---|---|
| Backend build | 0/0 warnings/errors, 35.8s | 0/0 warnings/errors, 39.7s (one transient MSB3883 file-lock retried mid-slice, unrelated) | — |
| No-pipeline regression | 1086 passed | **1086 passed** | **0 — backend test count unchanged**, as declared (§4.2-17: zero new backend tests, posture) |
| Route-smoke tier | 140 passed | **140 passed** | 0, both parity directions still hold with the new route catalogued |
| `Tests.Smoke.Data` | RAN (13 passed, dump present) | **RAN** (13 passed, same dump) | — |
| `check-api-contract` | clean | **clean**, `git status --short` empty after the run | — |
| Frontend tests | 193 files / 2316 tests | **193 files / 2343 tests** | **+27**, see breakdown below |
| Frontend build | succeeded, 17.9s, 2 pre-existing budget warnings | **succeeded, 21.1s, same 2 warnings, none new** | — |

**+27 vs the plan's declared "+10 to +18" (§4.2-17) — explained, not a defect.** Per-file delta:
`abwab-tree.builder.spec.ts` +4, `abwab-toolbar.component.spec.ts` +4, `abwab-sections-modal.component.spec.ts` +7,
`abwab.api.spec.ts` +1, `abwab-write.controller.spec.ts` +4, `abwab.labels.spec.ts` +7 — sums to 27 and
accounts for the whole suite-wide delta with nothing left over. Almost every one of those cells is a
behavior named explicitly in the plan (T502/T603/T701/T703's own cell lists), so most of the gap
between the estimate and the actual count is granularity, not scope: the labels file pins
`ROOT_DOOR_FORMS` with a six-row `it.each` (0/1/2/3/10/11) rather than one assertion per form, per the
workspace's data-driven-test convention (`test-guard`), and Vitest counts each `it.each` row as its own
test. **One cell is genuinely outside the plan's enumerated list, named honestly rather than folded into
"granularity":** the toolbar's "does not mark a non-zero count with the `--empty` class" case is
negative-space coverage T703 did not ask for — cheap and correct on a CSS-class toggle, kept
deliberately, not a scope creep worth reverting. Zero tests removed,
zero new spec files, fork cap (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`) preserved unchanged in
`package.json`, no `.only`/`.skip`/`xit`/`xdescribe` introduced (grepped clean).

**Tier B**, on both grounds the plan names: the global stylesheet `src/styles/_components.scss` was
edited (`.qd-tabs__count--empty`), and this slice completes a full backend+frontend vertical slice. The
same `npm test` + `npm run build` above satisfies both B and C — one run, two triggers. Confirmed
`shared/` carries no edit (DRIFT-4): `git diff --stat dev...HEAD -- Frontend/quran-dashboard-ui/src/app/shared` is empty, so `qdTab`/`qd-tabs` behavior is unchanged and no shared-primitive spec is owed.

## Phase 9 — T902 (browser acceptance matrix)

Both servers driven live: backend `dotnet run --project Backend/api/QuranDashboard.Api --launch-profile
https --no-build` with `ASPNETCORE_Kestrel__Certificates__Default__Path`/`KeyPath` pointed at the
frontend's mkcert `localhost.pem`/`localhost-key.pem` (absolute paths — `dotnet run`'s working directory
is the project folder, so relative paths from the repo root resolve to the wrong file), frontend `npm run
start:https`. Driven with `mcp__claude-in-chrome__*` against the real local Postgres `quran_dashboard`.
Scope narrowed per a pre-walk advisor consult to what a unit spec structurally cannot check (jsdom
cannot reproduce real focus/blur or the CDK's real auto-capture), plus the two full Playwright passes
below — not the plan's full 14-item list; every other T902 item is already pinned by a Phase 5/6/7 unit
cell or by the backend's own interaction matrix (§6a), and is cited here as unit/e2e coverage rather than
re-walked by hand.

- **`npx playwright test --project=abwab --workers=1` — 23/23 passed**, both before and after the fix
  below (two full runs). This is real evidence the new order `<button>` and the new `.qd-tabs__count`
  badge did not disturb any shipped Abwab flow's selectors or timing.
- **Badges render live**: «كل الأبواب» showed the true root total (6), section tabs showed their own
  root-only counts (1, 0) against a real snapshot — distinct from the door-count numbers elsewhere on
  the page, confirming item 19 end to end.
- **`cdkTrapFocusAutoCapture` lands on the order button** (first focusable control in the row, per
  §4.2-16) — a real, usable control, not a surprise landing.
- **T602 focus mechanism — click the order number, type, Enter, with no intermediate click**: focus
  landed in the input automatically (seeded with the live value) purely from `startOrderEdit`'s
  `afterNextRender`, with no forced `.focus()` call standing in for it the way Playwright's `fill()` does
  in the e2e suite. Confirmed working.
- **Blur cancels after only clicking the number, with no other click**: clicked the order button
  (auto-focuses the input), then clicked an unrelated element (the modal title) without ever clicking
  the input directly — the edit cancelled, no submission, matching the tree's grammar.
- **A real reorder was dispatched against the live backend and resequenced correctly** — the swap
  actually moved the row in the section list on both the modal and, after closing, the page's own tab
  strip (**"after a successful reorder the toolbar tab strip renders in the new order" confirmed** — the
  observable point of item 18, not just the modal list).
- **Reopening the editor on the same row after a successful reorder seeds the fresh number**, not the
  pre-reorder one — the unit spec's static fixture can't catch a stale-seed bug; the live backend
  round-trip can, and did not show one.
- **A real 400 (out-of-range position) surfaced the exact backend message** («الترتيب المطلوب خارج نطاق
  الأقسام») in the modal's error strip, proving the shared 409/400 failure map carries the backend's
  own Arabic message through to the UI end to end, not just in the mocked unit cell.
- **The `--empty` badge treatment on the *selected* tab (§11 stop condition) checked via computed style
  in both themes**: `opacity: 0.5`, `.qd-tabs__count--empty` present, selected-state `background`/`color`
  from `.qd-tabs__tab.qd-is-selected .qd-tabs__count` applied underneath with no specificity fight, in
  both light and dark (`data-theme="dark"`). No colour-override alternative needed — the plain opacity
  treatment reads correctly as shipped.

**One real defect found and fixed, not merely documented** — a genuine T602 follow-up, invisible to the
Vitest/jsdom suite because jsdom cannot reproduce it: a reorder that actually **moves** a row (not a
same-position no-op) is followed by refresh-after-write, which replaces `sections()` with a re-ordered
array; `@for` then moves that row's DOM node to its new slot, which detaches and reattaches it —
dropping the focus `focusOrderButton` had *already* correctly restored on the earlier, pre-refresh
render, because that single `afterNextRender` fired before the network round trip's later render
existed to catch. Isolated by A/B testing (a same-position commit preserved focus every time; an
actual position change lost it every time) and by instrumenting `HTMLElement.prototype.focus` to log
every call with a timestamp, which showed exactly two calls — input-open, then the immediate
post-commit button-focus — with nothing after, ending on `<body>`.

**Fix**: `abwab-sections-modal.component.ts` gains a `pendingOrderFocusId` signal, armed by
`commitOrderEdit` right after the immediate `focusOrderButton(id)` call, and a second `effect()` that
depends on `sections()` (reading `pendingOrderFocusId` via `untracked` so it does **not** also fire on
the immediate, pre-refresh render) — it fires specifically on the later, refresh-triggered render and
re-applies `focusOrderButton(id)` then, clearing the pending id so a later, unrelated reorder's refresh
never steals focus back to a stale row. A failed write clears the pending id immediately, since no
refresh follows a failure to consume it. Re-verified live after the fix: the focus log showed three
calls — input-open, the immediate post-commit button-focus, and a third button-focus roughly 2.5s
later (the real HTTP round trip) — ending on the button, not `<body>`. Regression-checked: the focused
Vitest glob stayed at 393/393 (jsdom cannot exercise the new code path meaningfully, so no new unit
cell was added for it — see the note above), and both Playwright `abwab` runs (before recording this
fix and after) passed 23/23.

Both manually-started dev servers were stopped after this pass (`kill` on the recorded PIDs, confirmed
by `curl` timing out on both `:4200` and `:5015`), per the e2e README's own rule against a stray server
outside Playwright's control.

## T903 — Close-out sweep

Full-repo grep for `twenty`, `20 write`, `create / rename / delete`, `doorsInScopeCount`, plus
`Twenty-four`/`twenty-four` (added per the T901 review — the string this slice itself changed in
`Controllers/README.md`), against `*.md`/`*.cs`/`*.ts`, excluding `node_modules`/`dist`/`bin`/`obj`:

- **One real gap found and fixed** — the frontend `features/abwab/README.md`'s own "What this feature
  does" paragraph (not the toolbar/sections-modal/refresh/stats-bar paragraphs Phase 8 already touched)
  still read "**twenty** write endpoints… the **three** section commands… **Twenty-four** endpoints in
  all… (fifteen + nine)". Fixed to twenty-one / four section commands (create, rename, reorder, delete)
  / twenty-five / (sixteen + nine) — recounted directly from the two data-access files
  (`grep -c "):\s*Observable<"` on `abwab.api.ts` → 16, `abwab-templates.api.ts` → 9, unchanged), not
  copied from the other README's number.
- **Every other hit is either a live doc already true** (the two backend READMEs, this frontend README
  now, all reading twenty-one/twenty-five consistently) **or historical, left as-is per repo law**:
  closed-slice `docs/feature-ux-slice-c/evidence.md`, `docs/feature-ux-slice-d/evidence.md`,
  `docs/feature-abwab-doors/plan.md`, `docs/feature-abwab-templates/plan.md`,
  `docs/feature-abwab-relations/plan.md`, `docs/abwab-ux-audit.md`, `docs/feature-ux-slice-b/{plan,evidence}.md`,
  and this slice's own `plan.md` (the Precondition/DRIFT tables are measurements taken *at plan time*,
  not living facts — the established convention every prior slice's plan.md already follows).
  `doorsInScopeCount` hits are all the still-correct, unchanged DTO field and its consumers/tests
  (verified true, not touched, in Phase 8's `Reads/Abwab/README.md` check) — none needed a change.
- The Active-Feature record in root `CLAUDE.md` stays pointed at `ux-slice-f` — it clears at merge, not
  before (§ root `CLAUDE.md`, "Active Spec Kit Feature").

## Close-out summary

| | Baseline (T101) | Close (T901/T903) |
|---|---|---|
| Backend: no-pipeline regression | 1086 passed | 1086 passed — unchanged |
| Backend: route-smoke tier | 140 passed | 140 passed — unchanged, both parity directions hold |
| Backend: `Tests.Smoke.Data` | **RAN** (13 passed, dump present) | **RAN** (13 passed, same dump) |
| Backend: `check-api-contract` | clean | clean (re-verified after every regeneration and again at close) |
| Frontend: unit tests | 193 files / 2316 tests | 193 files / 2343 tests (+27, explained in T901) |
| Frontend: `npm run build` | clean, 2 pre-existing budget warnings | clean, same 2 warnings, none new |
| Frontend: `npx playwright test --project=abwab` | not run (pre-existing, unmeasured) | **23/23**, twice (before and after the T902 focus-fix) |
| `Reads/Abwab/README.md` | — | verify-only, confirmed still true, no edit |
| Docs true again | — | 3 backend/frontend READMEs, `UI_STYLE_SYSTEM.md` §17, `TESTING_DEBT.md` (F1-F3), all cross-checked in T903 |

**22 plan tasks, all closed.** One out-of-scope defect (T602 follow-up focus-loss on a real reorder) found
by T902's browser pass and fixed in the same branch, not deferred, because it directly undermines the
plan's own stated accessibility goal for the feature it just shipped. All 18 items in `plan.md` §9's
obligations checklist verified individually against the phase evidence above and the actual diff (zero
migration files, zero `EfAbwabDoorsWriter.cs` edits, `shared/` untouched, fork cap unchanged — each
checked directly, not assumed). Ready for PR into `dev` — no `dev → main` merge, per plan §2/§3.
