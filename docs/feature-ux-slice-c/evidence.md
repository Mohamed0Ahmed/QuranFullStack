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

## Deviations from the plan, and why

Three, each recorded where the plan named something the code could not carry:

1. **No `single` input on `abwab-door-picker`** (plan T401's contract lists one). Selection is
   consumer-owned, so the single-anchor rule lives in the relations modal's own `togglePicked`
   and the picker never reads such a flag — shipping it would have been a dead input.
2. **A fourth height cap existed.** The plan's inventory names three (sections 14rem, copy
   13rem, relations 11rem); `abwab-move-picker.component.scss` carried a 15rem cap on its
   destination list too. It is deleted with the others, along with the nested scrollers those
   caps implied — inside `--fixed` the only scroller is `.qd-modal__body`.
3. **Focus-on-open is asserted differently than the plan assumed.** Where the plan calls for
   explicit focus (the door/template-node name field, the two picker searches) the component
   places it and the spec asserts `document.activeElement` — those pass. Where it calls for
   plain auto-capture (sections, move-picker), no unit assertion is possible: jsdom gives every
   element a zero-size box, so the CDK's own focusable check rejects the target and auto-capture
   never fires. Those specs assert the contract that produces the behavior (trap attached,
   capture on, the intended control first in tab order) and the real focus is a browser fact,
   recorded in the T802 matrix.

## T604 — Relations modal, visual acceptance against the concept

Seeded door `slice-c-vis-anchor` (id 674) with one relation in each of the four display groups,
opened from its real trigger in the browser. Screenshot: `relations-modal-acceptance.png`.

| Concept element | Observed |
|---|---|
| One global count pill | «4», classes `qd-chip qd-chip--pill qd-chip--static` |
| Four groups, dots + chips, empty ones omitted | all four render, in contract order, one chip each |
| Type segment | `role="tablist"` labelled «نوع العلاقة»; three tabs; `aria-selected` on the active one; roving tabindex `0/-1/-1` |
| Type segment keyboard model | ArrowLeft from the first tab moves focus to «تضاد» — RTL-correct, and something the hand-rolled `aria-pressed` strip never had |
| Conditional direction row | absent under تشابه, present after switching to شمولية |
| Picker + selected bar + foot | present; selected bar reads «لم تختر شيئًا بعد» |
| Geometry | dialog 576 × 704 px (`min(92dvh, 44rem)` at this viewport), `.qd-modal__body` genuinely scrolling, foot pinned |
| First focus | `abwab-relations-modal-search` |
| Escape | closes the dialog |

**Sanctioned deviation from the mock:** the active tab is §16.1 (`--qd-selected-bg` +
`--qd-accent-text` + `--qd-border-accent`), not the concept's surface+bold. §16.1 outranks an
ad-hoc active state, per the audit and plan decision 4.1-2.

## T801 — Closing run against the T101 baseline

| | Baseline (T101) | Closing (T801) | Delta |
|---|---|---|---|
| Test files | 191 | **193** | +2 |
| Tests | 2167 | **2206** | +39 |
| Failures | 0 | **0** | — |
| Vitest duration | 205.25 s | 203.08 s | — |
| `npm run build` | success | **success** | — |
| Initial bundle | 569.15 kB | 569.06 kB | −0.09 kB |

The delta is exactly the one stated in advance: +2 spec files (relations modal, template-copy
modal) and +39 tests, inside the predicted +30–50 — the two new suites plus the extensions to
the door, sections, move-picker, and templates-facade specs. Nothing was removed.

Build warnings: the three pre-existing budget warnings remain unchanged, and
`abwab-relations-modal.component.scss` **dropped off the list** — it was 5.08 kB against a 4 kB
budget at baseline, and the rules that moved into `abwab-door-picker` or died with the redesign
took it back under.

Tier B rather than Tier A because the slice reshaped surfaces that had no specs at all. No
backend change, so no `dotnet test` and no route-smoke tier. There is no CI: both runs are local.

## T802 — Keyboard-only acceptance, all six modals

Driven in the browser from each modal's real trigger. Every modal: focus lands on the intended
control, twenty Tab presses never leave the dialog, the shell slots are present, and Escape does
the right thing.

| Modal | Focus on open | Tab stays inside (×20) | `--fixed` + head/body/foot | role / aria-modal / labelledby | Height | Escape |
|---|---|---|---|---|---|---|
| `abwab-door-modal` (dirty) | `-name` | ✅ | ✅ | ✅ | 704 px | raises the discard guard, does not close ✅ |
| `abwab-relations-modal` | `-search` | ✅ | ✅ | ✅ | 704 px | closes ✅ |
| `abwab-move-picker` | `-section-none` (first tabbable) | ✅ | ✅ | ✅ | 704 px | closes ✅ |
| `abwab-sections-modal` | first rename button (first tabbable) | ✅ | ✅ | ✅ | 704 px | closes ✅ |
| `abwab-template-node-modal` | `-name` | ✅ | ✅ | ✅ | 704 px | closes ✅ |
| `abwab-template-copy-modal` | `-search` | ✅ | ✅ | ✅ | 704 px | closes ✅ |

All six render at the same 704 px — `min(92dvh, 44rem)` at a 900 px-tall viewport — which is the
"zero resize" contract holding across modals of very different content depth.

**Geometry byte-share.** Each modal's shell block (backdrop line through the `__head` open tag)
was extracted and normalised on the three sanctioned variables — component name, testid, close
handler. All six normalise to **identical** text.

**Cap grep.** `max-block-size` across the six modal folders plus `abwab-door-picker`: **zero
hits**.

## T804 — Close-out

Swept the repo (`src`, `e2e`, `.architecture`, `docs/contracts`, `docs/TESTING_DEBT.md`) for every
selector, class, label, and path this slice deleted or renamed — `__type--active`, the
hand-rolled `__count` and `__already` rules, all four `__pick-*` picker classes,
`abwab-door-fields-form__error`, the two dead copy-modal aria-label factories, and the deleted
caps. Two live references needed repointing, both fixed here rather than left dangling:

- `docs/TESTING_DEBT.md` row 5 said "same trigger as row 4"; row 4 is deleted (paid), so row 5
  now states its own trigger and points at this file for what the manual reproduction did and
  did not cover.
- `UI_STYLE_SYSTEM.md`'s `.qd-checkbox` specificity-trap example named
  `.abwab-relations-modal__pick-row`, a selector that no longer exists; it now uses a generic
  shape and notes where that one went.

Matches inside `docs/abwab-ux-audit.md`, `docs/feature-ux-slice-a/evidence.md`, and
`docs/feature-ux-slice-b/plan.md` were left alone deliberately: those are dated records of what
was true when they were written, not live pointers.

Docs updated in the same change as the facts they describe: the abwab README (one door picker,
the shared shell contract and its four consequences, the sections dirty guard, the paid
`selectedLoading` gotcha), `UI_STYLE_SYSTEM.md` §17 (the `--fixed` entry's consumer list and
cap rule, the checkbox and truncation debt lines narrowed to the page surfaces Slice D owns),
`docs/TESTING_DEBT.md` (row 4 deleted, row 9 narrowed, rows 2/5 untouched), and the navbar's
inert-surface count (nine → eleven; it had been stale since abwab reached six modals).

Temporary artifacts removed: both Playwright driver scripts used for the browser evidence, and
the seeded reproduction/acceptance doors (672–678) are archived, the accepted dev-DB convention.

**The slice's Active-Feature record is deliberately still open** in the root `CLAUDE.md` — the
lifecycle rule clears it at merge, as a separate `chore` commit (the `b84385f0` precedent).
