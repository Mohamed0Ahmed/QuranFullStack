# Phase 5 — e2e, docs, evidence

Plan: `docs/feature-abwab-global-order/plan.md` §7 Phase 5 (T501–T506).

## T501 — the independence e2e flow

New file: `Frontend/quran-dashboard-ui/e2e/abwab-global-order.e2e.ts`. One flow: creates three
sandbox doors, reads their `globalOrderValue` badges in the superset (`/abwab`, no `section`
param), reorders one past another there (a `Global`-scope write), asserts the section view's own
`orderValue` (`1, 2, 3`) is untouched, reorders within the section (a `Section`-scope write), and
asserts the superset's relative order from the earlier `Global` write is untouched. Only relative
order and the sandbox's own ids are asserted — never an absolute global number, never a global
count (`TESTING_STRATEGY.md` §6). Verified standalone (`--workers=1`): 1 passed.

## T502 — the R1 parallelism hazard, measured then decided

Verified first: `playwright.config.ts`'s `projects` carried only `name`/`use` — no per-project
`workers`, confirming the plan's premise before choosing a fix.

**Measured, not assumed.** Ran all five Abwab specs at the existing default (`workers: 2`):
`abwab-global-order.e2e.ts` failed — not a `409`, a **silently wrong result**:
`expect(locator).toHaveText('4')` received `'6'`. Another worker's teardown resequenced the global
order between this test's read of its target position and its commit, so the write landed on the
wrong door. Same run at `--workers=1`: 20/20 passed. Retry-on-409 was never on the table — this
feature's own policy is that `409`s are always surfaced, never swallowed or auto-retried
(`features/abwab/README.md`) — and this failure was not even a `409`, which makes serialization
the only real fix.

**Decision: option (a).** `playwright.config.ts` now splits `projects` into `default`
(`testIgnore` on `abwab-*.e2e.ts`, runs at the top-level `workers: 2`) and `abwab` (`testMatch` on
`abwab-*.e2e.ts`, forced to `--workers=1`). `package.json`'s `e2e` script runs both in sequence:

```json
"e2e": "playwright test --project=default --workers=2 && playwright test --project=abwab --workers=1"
```

Both invocations share `webServer` + `reuseExistingServer`, so the second does not re-pay startup
cost when the first left the servers running. Full gate re-run after the split: **48/48 passed**,
~2 m 40 s (28 default + 20 Abwab, up from 47/~1.6 m before this feature — the 48th test is T501's
new flow, and the rest of the increase is the serialized Abwab phase no longer overlapping with
`default`). `npm run e2e:headed`/`e2e:ui` do **not** apply the split (documented in
`e2e/README.md` as a debug-only gap, with the `--project=abwab --workers=1 --headed` workaround).

## T503 — backend READMEs

`Persistence/Writes/Abwab/README.md`: added the `global_order_value IS NOT NULL ⟺ (parent_id IS
NULL AND deleted_at IS NULL)` invariant, `ResequenceGlobal`'s whole-live-root read as an accepted
cost (not a violation of "one parent map per operation"), the no-`UNIQUE`-index trap (same
reasoning as `order_value`), `MaintainGlobalOrderAsync`'s departures-vs-arrivals asymmetry, and
restore-appends-in-both-spaces.

`Persistence/Reads/Abwab/README.md`: added that `GlobalOrderValue` is `NULL` for nested and
archived doors, and that the reader stays scope-ordered (`OrderValue`, `Id`) while the client
sorts the superset — consistent with the existing "flat, not nested" invariant.

## T504 — frontend READMEs

`features/abwab/README.md`:

- Corrected the "Refresh-after-write is an invariant" gotcha: it previously reasoned a write
  bumps every cached token *"in that scope"*; a root-affecting write now bumps `xmin` on **every
  live root everywhere** via the global resequence. The conclusion is unchanged (the controller
  already refetches the whole snapshot and rebinds every version), but the old wording was the
  exact sentence a future implementer would read to justify a narrower refresh — and that would
  no longer be safe.
- Added the two-order-spaces summary (which view uses which, the `scope` on the reorder wire,
  `ABWAB_ORDER_SCOPE_TO_WIRE` as the one mapping point) and the T402 move-picker decision
  (destination list follows the superset's global order, pinned by a spec case, not a side
  effect).
- Browser e2e section: added `abwab-global-order.e2e.ts` to the file list and the order
  independence assertion, and pointed at `e2e/README.md` for the single-worker split.

`e2e/README.md`: five Abwab specs (was four) in the file list and Commands section; the `npm run
e2e` two-project sequence and how to target one group; the measured R1 hazard and the chosen
fix; the R2 residue note (a `Global` reorder renumbers every live root in the dev DB, sandbox or
not — order-preserving for untouched rows, permutation of nothing observable, same acceptance
terms as the existing archived-doors residue); the `e2e:headed`/`e2e:ui` gap.

`Frontend/quran-dashboard-ui/README.md`: the one-line `npm run e2e` Testing section now states
the two-project split, since it documents that exact command.

## T505 — re-measured counts

Backend rebuilt (`dotnet build Backend/QuranDashboard.sln`, clean) before every `--no-build` run
below.

**The `Tests.Smoke.Data` dump was stale — a real environment failure, not a product failure**
(`TESTING_STRATEGY.md` §9 requires reporting these separately). First smoke run:
`Failed: 13, Passed: 127, Total: 140` — every failure the same
`Canonical smoke dump is stale … manifest.json was taken at migration
'20260728144026_AddAbwabDoorsAndSections', but this tree's head migration is
'20260729105806_AddAbwabGlobalOrderValue'`. This is the item flagged unresolved at the end of
phase 2. Fixed by regenerating it: `Backend/scripts/create-smoke-dump --yes` → new dump, sha256
`1fa83773f07643f3721d2be2de19f16bba3e0ec8da9b223efa53d6551c405f8b`, migration head
`20260729105806_AddAbwabGlobalOrderValue`. Re-run: **140/140 passed, 0 skipped — the data tier
ran**, not skipped.

| Run | Before this feature | After (measured 2026-07-29) |
| --- | --- | --- |
| Backend full suite | 1,827 / 5 m 35 s | **1,843 / 5 m 34 s** |
| Backend no-pipeline (Tier B/C) | 1,076 / 18-20 s | **1,086 / 21 s** |
| Backend pipeline families (derived, unchanged — no pipeline namespace touched) | 617 / 3 m 54 s | **617 / 3 m 54 s** |
| Backend route smoke | 134 / 51-52 s | **140 / 52 s** |
| `Tests.Abwab` (focused) | 36 | **46** |
| `Tests.Smoke.Data` (unchanged, confirmed) | 13 | **13** |
| Frontend full suite | 190 files / 2,142 tests / 207.54 s | **190 files / 2,158 tests / 205.65 s** |
| Frontend build | clean | **clean** (same pre-existing bundle-budget warnings, unrelated) |
| e2e gate | 47 / ~1.6 m | **48 / ~2 m 40 s** |

Partition identity re-verified: **1,086 + 617 + 140 = 1,843**, exact, zero failures, zero skips.

**On the 2,142 → 2,158 frontend delta** (16, not the 15 phase 4's own report claimed): phase 4
added the `abwab-tree.builder.spec.ts` tie-break test *after* its full-suite run, and only
re-verified it via a standalone run of that one spec file — so phase 4's "215/2,157" number was
stale by exactly the one test added after the last full-suite run, not a miscount. This full run
is fresh against final code (`TESTING_STRATEGY.md` §2) and is the number carried into
`TESTING_STRATEGY.md`.

`TESTING_STRATEGY.md` updated throughout §1, §3, §5, §6 with the numbers above, plus a new note
in §6 recording the measured R1 hazard and the single-worker Abwab project.

## T506 — root `CLAUDE.md` and the close-checklist sweep

**Active-Feature line** replaced: `abwab-doors-b` → `abwab-global-order`, pointing at
`docs/feature-abwab-global-order/plan.md` and naming its dependency on
`docs/feature-abwab-doors/plan.md` §4.

**Close-checklist arithmetic (plan.md §9), executed as far as it is due now:**

- `abwab-doors` (#48 + #49) and `smoke-harness` (#47) merged into `dev` 2026-07-29 and
  2026-07-28 — confirmed against `git log --merges`. Per the N-2 buffer (two most-recently-closed
  features by merge date), `docs/feature-playwright-bootstrap/` (the third-most-recent closed
  feature, `#46`) is due for eviction **now**, independent of whether `abwab-global-order` itself
  has merged — the buffer counts closed features, and `abwab-global-order` is still open, not one
  of the two buffered slots. Found still present in the tree (the plan's own "✔" mark for this
  eviction was aspirational, not yet executed) — `grep -rn` for `feature-playwright-bootstrap`
  across the whole repo found only the historical PR-number citation inside this feature's own
  plan.md, not a live link, so no repoint was needed. Deleted (`git rm -r
  docs/feature-playwright-bootstrap/`).
- `docs/feature-smoke-harness/` stays — it is one of the two currently-buffered closed features.
  It is due for eviction only when `abwab-global-order` itself merges into `dev` (plan.md §9's
  "closing this feature" event), which has not happened yet — that eviction is a **later** step,
  not part of this phase.
- `docs/feature-abwab-doors/` stays — both because it is still inside the N-2 buffer and because
  plan.md §9 explicitly pins it as required background until this feature closes.
- `docs/feature-032-rate-limiting/` and `docs/feature-033-auth-roles-permissions/` are
  pre-existing drift already past the N-2 buffer, **flagged, not fixed** — plan.md §9 says to
  raise this separately rather than fold an unrelated deletion into this PR.

## Clean-code / test-code self-check (root `CLAUDE.md`)

- New e2e spec matches the sandbox fixture's own `import type { Page } from '@playwright/test'`
  style (fixed after an initial inline `import(...)` type that diverged from
  `e2e/fixtures/abwab.ts`'s convention).
- No mocks in the new e2e flow — it drives the real backend end to end, consistent with every
  other Abwab e2e spec.
- Every README/strategy edit states facts directly (invariants, measured numbers, decisions) —
  none of it depends on `docs/feature-abwab-global-order/plan.md` surviving this feature's own
  eventual planning-artifact sweep.

## Verification run in this phase

```bash
cd Frontend/quran-dashboard-ui
npm run e2e:typecheck                                              # clean
npx playwright test e2e/abwab-global-order.e2e.ts --workers=1      # 1 passed
npx playwright test <5 abwab specs> --workers=2                    # 19 passed, 1 failed (R1, measured)
npx playwright test <5 abwab specs> --workers=1                    # 20 passed
npm run e2e                                                        # 48 passed (default 28 + abwab 20)
npm test                                                            # 190 files / 2,158 tests passed
npm run build                                                       # clean

dotnet build Backend/QuranDashboard.sln                             # clean
dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."          # 140/140, data tier ran
dotnet test … --filter "<no-pipeline filter>"                                    # 1,086/1,086
dotnet test … (full suite, no filter)                                            # 1,843/1,843
```

## Not yet done

Phase 5 was the last phase in `docs/feature-abwab-global-order/plan.md` §7. Remaining before the
feature closes: the `finishing-a-development-branch` skill flow and a PR into `dev` (never
`main`, per branch off `dev`, `CLAUDE.md`). `docs/feature-smoke-harness/` eviction and this
feature's own planning-artifact sweep are deferred to that merge, per plan.md §9.
