# Independent review — branch `abwab-review-fixes` (read-only)

**Date:** 2026-08-04. **Reviewed:** `git diff dev...abwab-review-fixes` (8 commits, 96 files,
+5,505/−461), against `docs/abwab-engineering-review.md` and the repository. The implementer's
report and commit-message narrative were not used as evidence. Every reported gate was re-run
independently, and the frontend gates were additionally **proven falsifiable** (an injected type
error was caught by `tsc -p tsconfig.app.json`; reverting fixes made their tests fail).

---

## Verdict: **BLOCKED**

The code that landed is largely excellent — every sampled fix is real, every revert experiment
produced the expected failure, the assertion-meaning audit found exactly the two authorized
changes and nothing else, roughly 30 new `file:LINE` doc citations all resolve, and all gates
pass. But the branch's **central claim is false**: the adjudication log states that "every
finding ends in one of three states … Nothing is left merely noted"
(`docs/abwab-engineering-review.md:24`), and **19 of the 104 findings — 9 of them MEDIUM — have
no state at all**: no code change, no ledger row, no recorded closure, and their defects verified
still present in branch code. One of the two refutations (F-95) fails independent re-derivation.
Two findings marked **fixed** are not fixed (F-52's policy asymmetry; F-51's template-apply row).
One finding marked **PAID** is neither paid nor recorded (F-69), and the ledger affirmatively
says "No row" for it. Merging now would trigger the artifact-deletion lifecycle and destroy the
only record of the 19 open findings — that alone blocks.

Disposition arithmetic supported by the file: **77 fixed + 4 converted + 3 recorded + 2 refuted
(one partial) = 85 adjudicated**, not 104.

---

## 1. Findings (severity-ranked; "branch" = this branch owns it, "inherited" = pre-existing)

### HIGH

**H1 (branch) — The completeness claim is false: 19 findings unadjudicated.**
`docs/abwab-engineering-review.md:24-25`. Absent from every bundle of §0 (verified by set
difference over lines 22–408), cited files untouched by the diff, defects re-verified present in
branch code, no `docs/TESTING_DEBT.md` row, no recorded closure anywhere:
- MEDIUM (9): **F-02** (DELETEs answer 204, OpenAPI says 200 — `AbwabDoorsController.cs:188`),
  **F-03** (controller fabricates `ReorderDoorOutcome.InvalidScope` — `AbwabDoorsController.cs:108`),
  **F-04** (unreachable StaleVersion 409 + README says the opposite — `AbwabSectionsController.cs:63`),
  **F-06** (`NodeCount` scope stated nowhere — `AbwabTemplateSummaryDto.cs:3`),
  **F-15** (door-create stale ⇒ 500 not 409 — `CreateDoorHandler.cs:37`),
  **F-36** (`shareReplay(1)` defeats cancellation; stale tree + ETag — `abwab-snapshot.facade.ts:69`),
  **F-37** (dead `section` id ⇒ silent empty tree; README still claims fails-closed — `abwab-url-sync.ts:64`),
  **F-60** (bulk bar renders «3 باب محدد» against the counted-noun rule — `abwab-side-panel.component.html:88`),
  **F-67** (active tab's zero count at ~1.9:1 — `src/styles/_components.scss:230`).
- LOW (10): F-12, F-16, F-22, F-28, F-33, F-71, F-74, F-89, F-90, F-93 — all re-verified open at
  their cited (or slightly moved) lines. Notably **F-71's `as T` null cast survived the branch's
  own rewrite of `handleSuccess`** (`abwab-write.controller.ts:192`), and **F-89's unlabeled digit
  was carried verbatim into the branch's rewritten cards template** (`abwab-cards.component.html:49`).
Whether the omission was triage or oversight is recorded nowhere.

**H2 (branch) — F-51's KEEP/DROP discriminator is false in code, and one DROP row was never
implemented.** The fix's safety argument — "a `qd-state` bound `[reserve]="true"` exists while
empty, so text insertion announces" (§0 log; `features/abwab/README.md:380`;
`UI_STYLE_SYSTEM.md:874-878`) — is contradicted by all four DROP surfaces: each `[reserve]`
element is itself wrapped in `@if (errorMessage())` and does **not** exist while empty
(`abwab-door-fields-form.component.html:1-2`, `abwab-sections-modal.component.html:21-23`,
`abwab-template-copy-modal.component.html:23`, `abwab-relations-modal.component.html:30-32`) —
`[reserve]` on an `@if`'d element is inert. By the branch's own reliability theory (an
`@if`-created alert inside a focused dialog "is not reliably announced"), the four DROP
operations may now fail **silently** for screen-reader users. Separately, the log's
"template apply → DROP" row was never implemented: `abwab-templates.controller.ts` is untouched
by the diff and announces every failure unconditionally (`:90`, `:96`), so **apply failures are
still double-announced — the original F-51 defect surviving on that operation.** Real-AT
behavior is not decidable from code; what is certain is that the documented contract does not
describe the shipped code.

**H3 (branch) — F-69 marked PAID; the debt is neither paid nor recorded.**
`docs/TESTING_DEBT.md:217-219` and the log (`:347`) say "paid, not deferred … No row". F-69's
obligation was the RTL placement / viewport-flip / clamp math. The new
`context-menu.component.spec.ts` asserts naming and focus only (F-70's territory) and
structurally cannot test placement — the `afterRenderEffect` early-returns on jsdom's 0×0 rects
(`context-menu.component.ts:61-63`). `place()`/`resolveDirection()` were never extracted
(`:107-131`). Placement coverage remains opt-in e2e only — exactly the state F-69 flagged — and
the ledger now **erases** the obligation instead of recording it.

### MEDIUM

**M1 (branch) — F-95's refutation fails.** See §2 below.

**M2 (branch) — F-52 marked fixed; the finding's defect is untouched.** Only the mechanism was
unified (`successAnnouncement` option, `abwab-write.controller.ts:32,194`). On the doors side
only `restoreDoor` declares a success message (`:136`) — as on dev — while
`abwab-templates.controller.ts` (untouched) still announces «أُنشئ القالب» (`:28`),
«حُذف القالب» (`:33`) and the apply count phrase (`:63`) into the same announcer. "One announcer
region, two opposite policies" is still true: a door create is silent, a template create speaks.

**M3 (branch) — The template-node modal's submit has no in-flight guard while the branch's
rewritten README claims every modal write shares one.** `abwab-template-node-modal.component.ts:97`
— F-63's fix covered the door and sections modals; its twin was left open and the new README
documentation is falsified by it.

**M4 (branch) — One-sided doc fixes: the exact twins failure class recurred, four times, all in
`src/app/shared/README.md` and the Writes README.**
- **F-11**: only the Controllers README was corrected; `Writes/Abwab/README.md:254-256` still
  says "optional direction" and omits the three reachable 400s the finding named.
- **F-38**: `shared/README.md:99-101` still prescribes the grep-only blast-radius test the
  finding proved under-reports (`detail-modal-shell.component.ts:63` still acquires imperatively).
- **F-100**: `shared/README.md:27` keeps the inverted "extends toward inline-start" label the
  §17 copy just fixed.
- **F-70**: the new focus management falsifies `shared/README.md:19-21` ("none of the four paths
  … puts focus inside it") and `:30-31` ("does not manage focus into the menu") —
  `shared/README.md` is absent from the diff entirely, against the same-change README law.

**M5 (branch) — "The last surviving reference to a deleted planning artifact" (Q-01 closure,
log `:2702-2704`) and F-01's "33 of 33" are overstated.** The literal-pattern grep is clean, but
**9 dangling references by section/item number survive**: `Writes/Abwab/README.md:196` ("plan §4,
§13.5"), `Reads/Abwab/README.md:32` ("feature plan §4"), `features/abwab/README.md:51, :56, :288,
:574` ("audit item 10/11/18/…"), `UI_STYLE_SYSTEM.md:1123, :1149, :1332` ("audit item 3", "the
audit that produced this entry"). The branch fixed this exact class in the Controllers README
while leaving these.

**M6 (branch) — The F-94 closure was made stale by the branch itself, and its recorded list was
silently edited.** The branch pushed `abwab-templates-page.component.ts` to **408** lines — over
the **hard** 400 threshold — while its new README entry claims soft-only
(`features/abwab/README.md:338`); `abwab-sections-modal.component.ts` went 293 → **305** (soft
300) unrecorded; and the adjudication silently swapped `abwab-tree.component.scss` (over soft
200, still unacknowledged) out of the three files it claims the finding named (log `:305`).
Inherited alongside: `abwab-relations-modal.component.ts` at 381 was missed by everyone.

**M7 (branch) — F-104 logged "fixed as code" but half-done:** the context-menu SCSS comment
survives (`shared/ui/context-menu/context-menu.component.scss:21`); only the chip comment was
deleted.

**M8 (branch) — F-65 marked PAID but only the dirty-close half is covered.** The new node-modal
spec's only describe block is "Escape (F-49)"; `submitNode` is mocked but never asserted — the
submit/validation path has zero coverage. Ledger row 9 (`docs/TESTING_DEBT.md:63`) still lists
the node modal as uncovered while the new section says paid — the ledger contradicts itself.

### LOW

- **(branch) F-98 fixed but unpinned.** With the copy modal reverted to dev, 11 of its 12 spec
  tests pass — nothing asserts `'ready'` reachability; a silent reversion would go undetected.
- **(branch) F-46 half-fixed:** the phantom ledger pointer was removed
  (`features/abwab/README.md:1056`) but neither prescribed remedy (row or test) landed.
- **(branch) Ledger pointer imprecisions:** F-13's derived-dormancy leg lives in *abwab-relations*
  row 2, not row 1 (`docs/TESTING_DEBT.md:220`); F-34's "generation" leg is row I1, not I2
  (`:222`); new row R1's *Where* column names the production file, not where the test must live
  (`:229`); the log records `AbwabDoorsController` as "227 lines, verified" — it is 225.
- **(branch) `Writes/Abwab/README.md` now points at TESTING_DEBT row C1 whose text the same fix
  made stale** (`docs/TESTING_DEBT.md:193` still says the README claims section-first ordering).
- **(branch) An always-green claim entered the README:** "a change to either side fails loudly"
  for the backend side of the M27 string pin (`features/abwab/README.md:845`) — nothing asserts
  the backend side.
- **(branch) F-42 changed «المزيد» behavior beyond the finding:** active-state matching went from
  exact to subset and the trigger gained hover-open (`top-navbar.component.ts:164-167`) —
  undisclosed behavior change on an app-wide surface (flagged by two independent checks).
- **(branch) Adjudication-log citation drift:** F-77's anchor (`:160`, claims html:235-242,
  bindings at :187-188), F-92's fix location, and four bundle-2 line citations point at wrong
  lines. The log is itself a working artifact, but it is the adjudication record.
- **(inherited) `onRowChange` remains unreachable** by any real user interaction in both picker
  modes (`abwab-door-picker.component.ts:151`) — the F-95 defect stands open (see §2).

### Notes (defects observed, mostly disclosed or inherited)

- F-41 residuals: outside click on a non-focusable area still drops focus to `<body>`, and the
  outside-click spec asserts a restore that only occurs in jsdom (`top-navbar.component.ts:90`).
- F-48's focus target races the async refresh (`abwab-page.component.ts:573`).
- F-97 residue (disclosed in the log): the loading skeleton is still trapped in `@empty`; retry
  over stale rows gives no in-flight feedback (`abwab-door-picker.component.html:73`).
- The template-copy modal itself remains dismissible mid-flight while `applyBusy`
  (`abwab-template-copy-modal.component.ts:126`) — the same pattern F-92's fix guarded elsewhere.
- TESTING_DEBT row J1 still carries the stale "816 lines … larger after" text (file is 757); the
  branch re-pointed at the row knowing the current number (`docs/TESTING_DEBT.md:159`).
- `e2e/shell-nav.e2e.ts:15`'s comment now describes the pre-F-40 behavior the branch removed.
- F-55 nuance: a hand-edited cross-section drill renders under a "no matches" label
  (`abwab-cards.component.ts:67`).

---

## 2. Independent rulings on the refutations

**F-92 — REFUTATION HOLDS.** Re-derived from dev code: the page bound
`[busy]="templateDeleteBusy()"` (`abwab-templates-page.component.html:149` on dev) and every
dismissal path — backdrop click, Escape, cancel button (also `[disabled]="busy()"`) — funnels
into `ConfirmDialogComponent.cancel()`, whose first statement is `if (this.busy()) { return; }`;
the shared dialog is untouched by the branch (empty diff). The only guard-free closer,
`closeOverlays()`, is unreachable behind the fixed `inset: 0` backdrop and focus trap, and
predates the branch. The error-clearing half was real on dev (asymmetric with
`cancelNodeDelete`) and is genuinely fixed at `abwab-templates-page.component.ts:336-346`.

**F-95 — REFUTATION FAILS.** The adjudication's premise — "arrow keys move the selection and
fire `change` without any click at all" — is false for shipping engines: arrow-key radio
selection is dispatched as a **simulated click** (cancelable, bubbling; Blink/WebKit
`RadioInputType::HandleKeydownEvent` → `DispatchSimulatedClick`, Gecko equivalent), and the
picker's single `<input>` carries `(click)="$event.preventDefault()"` in **both** modes — only
`[attr.type]`/`[attr.name]` vary (`abwab-door-picker.component.html:55-56, 61`). `preventDefault`
reverts the check and suppresses `change`; the bubbled click lands on the row's
`togglePicked` (`html:33`). So keyboard selection works through the same path as the mouse, and
`onRowChange` (`ts:151-156`) is never reached by any real interaction. The "live test [that]
already drives that path" (`abwab-relations-modal.component.spec.ts:426-438`) manufactures the
event — it sets `target.checked = true` and dispatches `new Event('change')` by hand — so it
proves wiring, not reachability. The original finding's substance stands (with one correction:
deleting the handler would break that pre-existing synthetic test, so the right closure is a fix
or an argued keep — not "refuted, nothing changed"). Caveat: live-browser proof was outside this
review's constraints; the ruling rests on engine source semantics and the code.

**F-94 — MIXED.** The core re-derives cleanly and honestly: the 604-line page component **was**
acknowledged on dev (`git show dev:…/README.md` line 45, split trigger named), the thresholds
match `FRONTEND_STRUCTURE.md`, all four recorded counts (604/416/356/312) are exact, and the
three files are recorded with reasons and triggers. But the closure's "only its genuine
remainder was recorded" fails on what the branch itself did next — see finding M6.

---

## 3. Reverted-fix experiments (all in a detached worktree; main tree never touched; everything restored)

| # | Fix reverted | Test that failed | Failure message |
|---|---|---|---|
| 1 | **F-35** — removed the one wiring line `this.selection.setSectionScope(parsed.section);` | `F-35 — a section switch clears the bulk set, not just the single selection` | `AssertionError: expected '2' to be '0'` |
| | | `F-35 — revealing a door in another section clears the bulk set through the same rule` | `expected '1' to be '0'` — 104 other page tests still passed |
| 2 | **F-51** — announce guard neutered at all 5 sites (always announce, dev behavior) | `reports the conflict outcome and clears only the door that was under conflict` (the rewritten M14 test) | `expected 'تم تعديل الباب من مستخدم آخر' to be null` |
| | | `drops the announcer for a failed door edit, whose form reserves its alert region` | `expected 'اسم مكرر' to be null` — the three KEEP tests passed, as they should |
| 3 | **F-61** — copy modal reverted to dev | `does not re-issue the apply while the first one is still in flight` | `expected "spy" to be called 1 times, but got 2 times` — and 11/12 passing exposed that F-98 has no pin |
| 4 | **F-63** — door modal reverted to dev | `creates one door for two clicks, and re-enables save when the create fails`; `shares the create path's in-flight guard, so two clicks send one edit` | both: `expected "spy" to be called 1 times, but got 2 times`; bonus: F-49's pin `dismisses the discard strip on Escape and keeps the modal open` also failed |
| 5 | **F-40/41/77** — navbar reverted to dev | 14 of 20 fail, incl. `opens the words dropdown when the trigger is clicked after the pointer already entered the item` | `expected 'false' to be 'true'`; all nine focus-return tests: `expected <body> … to be <button>`; mobile-inert: `expected false to be true` |
| 6 | **F-92** — templates page reverted to dev | `drops the failure message when the dialog is dismissed`; `refuses to dismiss while the delete is still in flight` | `expected 'تعذر الاتصال بالخادم. حاول مرة أخرى.' to be null`; `expected null not to be null`; F-91's retry and both F-47 focus pins also failed |

Every sampled test binds to its fix. The log's quoted F-35 failure signatures were reproduced
exactly.

---

## 4. Assertion-meaning audit (whole diff)

All **45 removed lines across 25 changed `*.spec.ts` files and 7 e2e files** were inventoried
and classified. **Exactly the two authorized meaning changes exist**: the tree spec's
inert-in-bulk-mode → toggles (`toHaveLength(0)` → `toEqual([1])`), and the write-controller
spec's single removed line `expect(controller.announcement()).toBe('تم تعديل الباب من مستخدم آخر')`
replaced by the null assertion naming the owning region. Everything else is plan-pointer comment
removal, mechanical import extension, verbatim relocation, or rename-following. No `.skip`/`xit`,
no vacuous rewrites, no matcher downgrades. Every data-testid the changed e2e lines reference
resolves in current templates. Per-file `it(`-counts are purely additive.

## 5. Verification runs (mine, not the log's)

- Backend: build 0 errors; Tier C no-pipeline **1100/1100**; route-smoke **145/145, 0 skipped —
  the `Tests.Smoke.Data` tier RAN** (13/13, canonical dump present).
- Frontend: `tsc -p tsconfig.app.json` and `-p tsconfig.spec.json` → 0 (and the app config
  **caught an injected error**, so it is not vacuous); full suite **204 files / 2624 tests / 0
  failures**; AOT `npm run build` clean with exactly the three known budget warnings.
- Test-count rise: dev **2525** → branch **2624** = **+99**, purely additive per §4.
- The log's own reported numbers (2620+4, 145/13, three warnings) match what I measured — the
  implementer's run reports were honest.

## 6. Verified clean (explicitly checked invariants)

- **Quran-data safety:** the diff touches no Quran text, morphology, roots/lemmas/stems, POS,
  segments, alignment, or counting scope; no expected count adjusted; no integrity check
  weakened. Every "root" in the diff is the Abwab domain concept.
- **F-35 is genuinely closed** — store-side rule fed from the URL subscription, both entry
  paths covered, bulk mode preserved; the disclosed cards-view `q`-filter residue is a milder,
  coherent harm class, not the original defect surviving.
- All ten bundle-2 closures and fourteen bundle-3b/4 closures verified in code with pinning
  specs; the three order editors now agree on cancel-on-blur; F-103's repoint left zero stale
  `qd-confirm-dialog-*` references repo-wide.
- **~30 added `file:LINE` doc citations all resolve** — zero citation rot in the rewrite; Q-01's
  three facts and Q-02's single-instance record verify against code (`ApiMessages.cs:117`,
  `abwab-write.controller.ts:43`, fallback present).
- `src/styles/` untouched; `qd-context-menu` has no non-abwab consumers; the chip change removed
  only comments; `e2e/shell-nav.e2e.ts`'s `.hover()` still opens the menu under the new logic —
  no delay timer was introduced.
- Exactly the five claimed spec files are new; F-14's pre-existing ledger row fully covers its
  obligation; R1 is a genuine, accurate new row for F-20; F-18/F-72 recordings exist with
  justification and split triggers.
- The claimed "unfixable contradiction" (two test comments asserting the old BulkMove ordering)
  is real and correctly documented.

## 7. Closures not independently confirmable

- Whether an `@if`-inserted `role="alert"` announces in real assistive tech — the crux of H2's
  practical severity; not decidable from code (both the KEEP rationale and the DROP risk rest on
  it).
- Live-browser proof of the F-95 radio-click semantics (engine source semantics cited instead).
- Railway actually running one instance today (Q-02's folded fact) — an environment fact.
- Whether the 19 omissions were deliberate triage — no record exists anywhere.
- F-12's "already applied to production" rationale — nothing in the repo records the apply.
- Bundle 2's "all 27 files fall inside the agents' ownership lists" — no manifest exists to
  check.

## 8. What must happen before merge

1. Adjudicate the 19 findings (fix, convert, or close with a written reason) — or correct the
   log's completeness claim and keep the file alive until they are.
2. Reopen F-95 with a correct analysis; fix or argue-keep `onRowChange`.
3. Make F-51's contract true: either implement real reserved regions (move `[reserve]` outside
   the `@if`) or reclassify with an honest discriminator; wire the template-apply path; fix the
   README/§17/log copies. Decide F-52's success policy for real.
4. Pay or ledger F-69; finish F-65's submit half (or narrow row 9 honestly).
5. Land the four `shared/README.md` / Writes-README twin corrections (M4), the template-node
   guard (M3), the F-104 SCSS comment, and the nine by-number dangling references (M5).
6. Record the size-threshold facts the branch itself changed (M6).

*Method note: sections 2, 3, 4 of the brief were completed in full; findings verification
covered all six bundles (every HIGH and every user-visible behavior change, plus the full
missing-19 sweep); nothing in the brief was skipped for budget.*

---

## Addendum — resolution (second fix pass, 2026-08-04)

On the user's instruction ("fix all"), every finding above was resolved on this branch. The
canonical record is **Bundle 7** of `docs/abwab-engineering-review.md` §0 — the 19-finding
adjudication table, the corrections to bundles 1–6, and the complete list of the five
authorized assertion-meaning changes. Summary against this report's findings:

- **H1** — all 19 adjudicated: 17 fixed, 2 closed with written reasons (F-12, F-22 — the
  latter per its own finding's prescription).
- **H2** — the copy modal is now genuinely reserved (with the new `.qd-state--reserve-empty`
  quiet shape); the other three DROP surfaces keep their `@if` under the corrected
  discriminator (alert-insertion into a plain `role="dialog"` announces; insertion inside a
  focused `role="alertdialog"` does not — the KEEP set); the template-apply wiring landed in
  `abwab-templates.controller.ts` with its own new spec. Docs now state the true contract.
- **H3** — F-69 genuinely paid: placement math extracted to pure
  `context-menu-placement.ts`, four branches unit-pinned.
- **F-95 (refutation failure)** — the dead path was removed and the synthetic test rewritten
  to pin the real simulated-click mechanism (authorized change #1).
- **M2/F-52** — one success policy: every write announces politely; the three
  silence-pinning assertions rewritten under authorization (#2–#4).
- **M3–M8 and the LOW/NOTE list** — all landed: the node-modal guard, the copy-modal
  dismissal guard, the four one-sided doc fixes, the nine by-number dangling references, the
  size-threshold records (including the templates-page hard-threshold split, 408 → 392 + a
  48-line delete controller), F-104's second comment, F-65's submit half, F-98's pin, F-46's
  null-envelope test, the ledger reconciliations (I2/J1/C1/R1, F-13/F-34 pointers), the M27
  always-green reword, the stale e2e comment, and the navbar retitles (title-only).
- **Decisions taken under the fix-all authority, flagged for the record:** the uniform
  announce-all-successes policy (vs. keeping three ops silent); keeping `«المزيد»`'s
  data-driven subset matching and hover-open as deliberate; F-04 resolved as
  document-the-race rather than delete-the-mapping (the mapping is reachable); F-36 fixed
  beyond the prescription (generation guard) because the prescribed fix alone demonstrably
  could not close the finding's own scenario.

**Final gates after the pass:** backend build 0 errors; no-pipeline 1103/1103; smoke 146/146
with `Tests.Smoke.Data` RUN (13/13); `tsc` app+spec clean; frontend suite **2694/2694**
(2624 → +70); AOT build with exactly the three known budget warnings.
