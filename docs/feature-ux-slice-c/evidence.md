# Slice C — evidence

Companion to `plan.md`. Every "no regression" claim in this slice measures against the
T101 baseline recorded here, not against any number quoted in another document
(`plan.md` §5 last row: three docs disagree today).

## T101 — Baseline on `dev`

| Fact | Value |
|---|---|
| Branch point | `dev` @ `b84385f044c52fe976b14d4276d8a1682725bc43` ("chore: close ux-slice-b in the active-feature list") |
| Slice branch | `ux-slice-c-modals` |
| Date | 2026-08-01 |
| `npm test` (fork cap via the npm script) | **191 test files, 2167 tests, all passed** |
| Vitest duration | 205.25 s (wall 4:06) |
| `npm run build` | success, 21.68 s bundle generation (wall 28.7 s) |
| Initial bundle total | 569.15 kB raw / 142.16 kB transfer |

Pre-existing build warnings at baseline (not introduced by this slice, recorded so the
closing run compares like for like):

- `bundle initial exceeded maximum budget` — 569.15 kB against a 500 kB budget.
- `abwab-relations-modal.component.scss` exceeded the 4 kB per-component budget — 5.08 kB.
  Phase 6 deletes rules from this file, so the closing number should fall.
- `selected-ayah-section.component.scss` — 5.85 kB; `selected-word-section.component.scss` — 4.65 kB.

There is no CI (`TESTING_STRATEGY.md` §8); both runs above are local gates.

## T102 — Slice recorded

- Root `CLAUDE.md` "Active Spec Kit Feature" replaced: the stale `abwab-templates` line
  (that feature closed and merged to `dev` via PR #54) is gone; the section now names
  exactly one open feature, `ux-slice-c`.
- `docs/feature-abwab-templates/` deliberately **not** swept — it is inside the N-2
  buffer of most-recently-closed features.
- Branch `ux-slice-c-modals` created off `dev`.
