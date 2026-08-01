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

## T201 — Reproduction of the Slice A relations-read observation

Run against the local dev backend (`https://localhost:5015`, Development, local Postgres
`quran_dashboard`, `/api/health` reporting `database: healthy`) with the Angular dev
server on `https://localhost:4200`. Abwab endpoints carry no `[Authorize]` and there is no
global fallback policy, so the calls below are unauthenticated by design, exactly like the
e2e sandbox fixture.

| Step | Call | Result |
|---|---|---|
| 1 | `POST /api/abwab/doors` ×2 (`slice-c-repro-a`, `slice-c-repro-b`, section 217) | `201` — ids **672** and **673**, both live |
| 2 | `POST /api/abwab/doors/672/relations` `{type:1, direction:null, targetDoorIds:[673]}` | `201` — relation id **42**, `type:1`, `otherDoorName: slice-c-repro-b` |
| 3a | `GET /api/abwab/doors/672/relations` | `200` — `[{id:42, otherDoorId:673, otherDoorName:"slice-c-repro-b", type:1}]` |
| 3b | `GET /api/abwab/doors/673/relations` | `200` — the mirrored row (`otherDoorId:672`) |
| 3c | `GET /api/abwab/tree` | `relationCount = 1` on **both** 672 and 673 |
| 4 | Relations modal opened on door 672 in the browser | count pill **1**; one group «تشابه» containing the chip «slice-c-repro-b» |
| 5a | `DELETE /api/abwab/doors/673` (version 9252) | `204` |
| 5b | `GET /api/abwab/doors/672/relations` | `200` with **`data: []`** |
| 5c | `GET /api/abwab/tree` | `relationCount = 0` on both; 673 `isArchived: true` |
| 5d | `POST /api/abwab/doors/672/relations` targeting the archived 673 | `400` «لا يمكن إنشاء علاقة مع باب مؤرشف» |
| 6 | Cleanup: `DELETE /api/abwab/doors/672` | `204` (archived residue is the accepted dev-DB convention) |

Notes for anyone repeating this:

- The create-relations body field is `type` (numeric `AbwabRelationType`: 1 similarity,
  2 opposition, 3 comprehensiveness), not `kind`. A `kind` payload 400s with
  «نوع العلاقة غير صالح».
- The browser step could not run through the Chrome-extension driver: the ASP.NET dev
  certificate is untrusted in a fresh Chromium profile, so every `https://localhost:5015`
  call fails `ERR_CERT_AUTHORITY_INVALID` and the page renders its tree-load error. It was
  run through the project's own Playwright with `ignoreHTTPSErrors: true` — the same
  setting `playwright.config.ts` uses — from a throwaway script that was deleted after the
  evidence was taken (the B2 temporary-artifact precedent).

## T202 — Verdict on the observation: **closed, not a bug**

Steps 3 and 4 show the relation present on live doors, over the API and in the UI, on both
sides of the pair, with the tree counts agreeing. The gate condition in `plan.md` §6 Phase 2
("relation missing on live doors") **did not occur**, so the slice proceeds.

Step 5 reproduces the recorded symptom pair exactly — empty relations list **and** count 0 —
by the one input that produces it: an archived endpoint. That is the dormancy derivation
working as specified (`Reads/Abwab/README.md:67-75`), not a read or cache fault. Step 5d
closes the remaining alternative: the writer refuses archived endpoints outright, so a
relation cannot be born dormant; a dormant relation can only be one whose door was archived
after the fact.

Most plausible explanation of the Slice A observation, consistent with every measurement
here: that harness POSTed against leftover archived `e2e-sandbox-*` doors, which the e2e
teardown leaves in the dev DB by design (`e2e/fixtures/abwab.ts:86-95`), and the read it
then judged "wrong" was correct dormancy.

`docs/TESTING_DEBT.md` rows 2 and 5 (the backend dormancy join tests and the e2e dormancy
flow) remain **unpaid and unaffected** — a manual reproduction is evidence, not a test.
