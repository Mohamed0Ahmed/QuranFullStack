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

## T605 e2e evidence

pending — phase 6

## T801 post-change verification

pending — phase 8

## T802 grep sweep

pending — phase 8
