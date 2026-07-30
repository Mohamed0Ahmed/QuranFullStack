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
