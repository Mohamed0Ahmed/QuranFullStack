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

That last sentence rules out one story and pins another, and the two must not be conflated —
an earlier revision of this section did conflate them. Because step 5d returns **400**, a
harness POSTing at an already-archived door creates **no row at all**; the empty read that
follows is a *failed write*, not dormancy, and it would only look like dormancy to a harness
that ignored the POST status. The mechanism that actually produces the recorded symptom pair
against a relation that was really created is the ordering one: both doors live at POST time
(2xx, as in step 2), one of them archived afterwards — by a later `e2e-sandbox-*` teardown,
which leaves archived doors in the dev DB by design (`e2e/fixtures/abwab.ts:86-95`) — and the
read then correctly derives the relation as dormant.

Most plausible explanation of the Slice A observation, consistent with every measurement here:
that ordering. Whether the harness additionally swallowed a 400 on a second attempt is not
determinable from this reproduction and is not claimed.

`docs/TESTING_DEBT.md` rows 2 and 5 (the backend dormancy join tests and the e2e dormancy
flow) remain **unpaid and unaffected** — a manual reproduction is evidence, not a test.

## Deviations from the plan, and why

Four, each recorded where the plan named something the code could not carry:

1. **`single` on `abwab-door-picker` is an affordance flag, not a selection flag** (plan T401's
   contract implies the latter). Selection stays consumer-owned — the single-anchor rule lives in
   the relations modal's own `togglePicked` and the picker never reads the flag to decide *what a
   pick does*. What the picker cannot infer, and what an engineering review caught as a real gap,
   is which **control** to render: a checkbox promises "pick any number", and anchor-pick mode
   accepts exactly one. `single` therefore ships, switching the row control to a named radio group
   (and the relations modal to a one-door search placeholder). Anchor-pick selection became
   select-only in the same change: a radio group has no click-the-selected-one-to-clear gesture,
   so mirroring one would have been an affordance the control does not offer.
2. **A fourth height cap existed.** The plan's inventory names three (sections 14rem, copy
   13rem, relations 11rem); `abwab-move-picker.component.scss` carried a 15rem cap on its
   destination list too. It is deleted with the others, along with the nested scrollers those
   caps implied — inside `--fixed` the only scroller is `.qd-modal__body`.
3. **Focus-on-open is asserted differently than the plan assumed.** Where the plan calls for
   explicit focus (the door/template-node name field, the two picker searches) the component
   places it and the spec asserts `document.activeElement` — those pass, in **both** relations
   modes. Where it calls for plain auto-capture (sections, move-picker), no unit assertion is
   possible: jsdom gives every element a zero-size box, so the CDK's own focusable check rejects
   the target and auto-capture never fires. Both of those specs assert the contract that produces
   the behavior (trap attached, capture on, the intended control first in tab order) and the real
   focus is a browser fact, recorded in the T802 matrix. *(The sections spec did not carry that
   assertion when this deviation was first written — the claim was true of the move-picker only.
   It has since been added, which is what makes the sentence above true rather than aspirational.)*
4. **The doc updates landed one commit behind the facts they describe.** Plan T803 says "same
   change as the facts" while scheduling itself into Phase 8, and Phase 8 is what happened: the
   abwab README, `UI_STYLE_SYSTEM.md` §17, the navbar comment, and `TESTING_DEBT.md` all arrived
   in `25b60f2a`, after the behavior commits they document. Only T702's `selectedLoading` gotcha
   was removed in its own commit (`0d7b213b`), the way the workspace rule asks. Recorded rather
   than rewritten: the merged tree is consistent and the cost was a bisect window, not a wrong
   doc. The plan's own contradiction is the thing to avoid next time — a "docs true again" phase
   cannot also be a same-change obligation.

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

**One row per modal, and the relations modal has two modes.** This matrix drove each modal from
its real trigger once; the relations row is **door mode**. Anchor-pick mode shares the same
open-effect (`abwab-relations-modal.component.ts` queues one `focusSearch()` for both branches),
and it is pinned in the spec rather than here — `abwab-relations-modal.component.spec.ts`
asserts `document.activeElement` on the search input in anchor-pick mode too. The door row
records the **dirty** Escape (guard raised); the clean close is spec-covered.

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

## Post-verification fix — the door modal's error surface

The keyboard matrix collected DOM facts for all six modals but pixels for only the relations
modal, and a screenshot of the door modal afterwards caught what the DOM checks could not:
decision 4.2-7's `qd-state variant="error" [reserve]="true"` had been rendered
**unconditionally**, so a **105 px** empty, danger-tinted, centre-aligned box sat between the
title and the name field on every open of the door *and* template-node modals — the shared
container's `padding: var(--qd-space-6)` plus `reserve`'s reserved message row, with nothing in
it. The 11-test door spec did not catch it: it asserts the message's text when a write fails and
never asserts absence on the happy path.

Fixed by matching what the other four abwab modals already do —
`@if (errorMessage(); as error) { <qd-state … [reserve]="true" /> }`. That keeps the composition
the decision asks for (the surface is `qd-state`, not a hand-rolled `<p role="alert">`) and drops
the empty box. Re-verified in the browser: no error element on the happy path, the name field
sits directly under the context line.

Two smaller things fixed in the same pass:

- The queued-focus comments in the door and template-node modals claimed the trap would otherwise
  capture "the error box". A `<div role="alert">` with no action button is not tabbable, so that
  was never the reason; the comments now state the real one — the capture runs on the render
  after the effect and would overwrite a synchronous focus.
- `abwab-door-picker` held the copy modal's «لا توجد أبواب حية لنسخ القالب إليها» as its own
  empty-state string. Unreachable today (relations always passes `status: 'ready'`), but a shared
  component holding one consumer's wording is the divergence the unification exists to prevent.
  It is now an `emptyMessage` input the host supplies.

Full abwab suite and `npm run build` green after all three.

## Post-review fixes — the engineering review's findings

An engineering review of the whole branch found two defects that the suite could not have
caught, plus a list of smaller ones. All are fixed here.

**The sections modal's discard discarded nothing (the one user-visible defect).** `confirmDiscard()`
emitted `closed` without clearing `newSectionName` / `editingId` / `editingName`, and the new
open-effect reset only `confirmingDiscard`. The modal is a **static sibling** on the page shell
(`abwab-page.component.html`), so the instance outlives every close — only its inner `@if`
template is destroyed. Typing a section name, pressing Escape, and answering «تجاهل التغييرات»
therefore *hid* the draft: the next open showed the text back and was dirty before a keystroke,
so Escape immediately raised the guard again. A stale `errorMessage` survived the same way.

The door modal never had this because its drafts live in `abwab-door-fields-form`, which sits
*inside* `@if (open())` and is thrown away on close — decision 4.2-5 copied the trio's shape but
not the destruction that makes discard mean discard. Fixed with a `resetDraft()` in the
open-effect, matching what the relations and copy modals already do.

**The `2d312de6` error-surface fix was unpinned.** The door spec's only `-error` assertion checks
the message text after a failed write, and that assertion passes identically whether the surface
is guarded or rendered unconditionally — the 105 px empty box could have come straight back.
Fixed with an absence assertion on the happy path, which also covers the template-node modal
through the shared form.

Both new specs were verified to **fail** against the reverted fixes before being kept.

The rest, in one pass:

- **Anchor-pick affordance** — the `single` input (deviation 1 above): radio group + one-door
  placeholder, select-only semantics, keyboard `change` path wired.
- **The shared picker answered an unmatched search with its host's empty message** — «لا توجد
  أبواب حية لنسخ القالب إليها» is a claim about the tree, and a query that matches nothing is not
  evidence for it. New `pickerNoMatches` state, specced on both hosts, and guarded on *both*
  terms (`query typed` **and** `nodes().length > 0`) so the mirror case — a typed query over a
  genuinely empty tree — still gets the host's sentence. Both directions are specced.
  (Pre-existing on `dev`; the unification carried it across, so it is paid here.)
- **Two consequences of that path becoming reachable, each fixed in the same pass:**
  - The picker's empty state was `status === 'empty'` only, which relations never sets — an empty
    relations tree would have rendered a silent blank list. It is now the **fallthrough** branch
    and `emptyMessage` is `input.required`, so no host can omit the answer; relations supplies
    `relationPickerEmptyDoors`.
  - **One deliberate testid change:** the picker's empty state moved from `<prefix>-empty` to
    `<prefix>-doors-empty`. With one prefix serving host and picker, `abwab-relations-modal-empty`
    was owned by the modal's own «لا توجد علاقات» — the same collision the picker's error already
    avoided by being `-doors-error`. This is the one place decision 4.2-2's "existing testids
    survive verbatim" is knowingly broken, because the alternative is two elements answering to
    one id.
- **The radio group name is minted per picker instance** (`abwab-door-picker-N`, the `titleId`
  pattern), not derived from `testIdPrefix`. Radio grouping is document-scoped by `name` and
  emulated encapsulation does not scope an attribute, so a literal name would merge two pickers
  into one group. The spec asserts the shape, not just that two rows share it.
- **Every relations chip's delete button was named «حذف العلاقة»** — N identical controls per
  group. `relationDeleteAriaLabel` now names the door.
- **The sections spec gained its trap assertion** (deviation 3's correction).
- **`__target`'s `14rem`** now carries the per-call-site justification §17's truncation entry
  requires of any name-column size.
- **The move-picker's `__body` content** is indented inside the wrapper it gained.
- **Evidence corrections**: T202's closing inference (below), the T802 matrix's mode coverage,
  and deviation 4.

Not changed, deliberately: `[reserve]="true"` inside an `@if` is a contradiction of that input's
"never appears/disappears" purpose, but it is B1's shipped pattern at all four abwab sites and
matching it beats diverging one of them — noted in `UI_STYLE_SYSTEM.md` §17 for whoever revisits
`qd-state`. `TESTING_DEBT.md` row 5's trigger clause stays rewritten (row 4 is deleted, so "same
trigger as row 4" would dangle); the row itself is untouched and unpaid. The template-node modal
stays unspecced — row 9's narrowed remainder is its trigger.

**Closing run after the review fixes** (the T801 table above is a point-in-time measurement and
stays as recorded; this is the branch tip):

| | T801 | After review fixes | Delta |
|---|---|---|---|
| Test files | 193 | **193** | — |
| Tests | 2206 | **2219** | +13 |
| Failures | 0 | **0** | — |
| `npm run build` | success | **success** | — |
| Initial bundle | 569.06 kB | 569.06 kB | — |

The +13 are the specs the findings demanded: three sections-modal reset cases, the sections trap
assertion, the door modal's error-absence case, four anchor-pick affordance/keyboard/focus cases,
and — on each picker host — a matched-nothing case plus its mirror over an empty tree. Build
warnings unchanged: the same three pre-existing budget lines, none new. Both runs local; there is
no CI.

Two of these specs were verified by reverting their fix and watching them fail, not by watching
them pass: the sections reset (3 failures) and the door error-absence assertion (1). A spec that
cannot fail is the exact defect the review found in the first place.
