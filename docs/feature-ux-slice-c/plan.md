# Slice C — The modal system (UX audit)

Source: `docs/abwab-ux-audit.md` Part 8, "Slice C — The modal system" (`:1083-1092`), plus the
work Slices A and B explicitly pushed into C (slice-a plan `:58-62`, `:180-181`; slice-b plan
`:664`) and three accumulated inputs named by the commissioning prompt (the TESTING_DEBT
rows-4+9 trigger, the Slice A relations-read observation, the templates-page silent-loading
gap).

**Mode when this plan was written:** plan-only. No code, no Git, nothing amended.

## Precondition — VERIFIED on `dev` at plan time

| Consumed primitive / mechanism | Where it lives | Verified |
|---|---|---|
| `.qd-modal--fixed` + `__head/__body/__foot` slots | `src/styles/_components.scss:566-609` | ✅ defined, **zero consumers** (built ahead-of-use by Slice A phase 4) |
| `.qd-checkbox` / `.qd-check-row` | `src/styles/_forms.scss:88-105`, token `_tokens.scss:166` | ✅ defined, zero consumers |
| `.qd-truncate` | `src/styles/_utilities.scss:58-63` | ✅ defined, zero consumers |
| `qd-state` `reserve` input | `shared/ui/state/state.component.ts:26` | ✅ shipped (B1), abwab call-sites live |
| z scale | `src/styles/_tokens.scss:213-220` | ✅ `--qd-z-modal-backdrop: 50` in force |
| Navbar-inert mechanism (B2 T904) | `ScrollLockService.isLocked` → `top-navbar.component.html:5-6` | ✅ all six abwab modals hold `qdModalScrollLock` (T905 included sections + move-picker) |
| `qd-tabs` component + directive + global classes | `shared/ui/tabs/`, `_components.scss:155-224` | ✅ shipped, used in `abwab-toolbar` |
| `qd-chip` component + `.qd-chip__count` | `shared/ui/chip/`, `_components.scss:227-332` | ✅ shipped, used in relations groups + alias chips |
| Slice merges on `dev` | A `3644b772`, B1 `e5c7060d`, B2 `61a208d1` | ✅ |

## 0. Guard result

Task arithmetic: Phase 1 = 2, Phase 2 = 2, Phase 3 = 2, Phase 4 = 2, Phase 5 = 6,
Phase 6 = 4, Phase 7 = 2, Phase 8 = 4. **24 tasks — under the 30-task threshold. One
slice, no split.**

Recorded so a mid-execution split does not get drawn on task count: if this slice had
split, the seam is **after Phase 4** — "specs + unification" (behavior-preserving: new
specs over untested code, then a refactor those specs guard) versus "restructure +
redesign" (visible geometry and layout changes across all six modals). The seam is who
can be hurt by a mistake: Phases 2–4 can only break the two previously-unspecced modals
and are self-verifying via the specs they write; Phases 5–6 change what every abwab
dialog looks like.

## 1. Objective

| # | Deliverable | Home | Audit item |
|---|---|---|---|
| 1 | Slice A's relations-read observation resolved and closed (or the real bug surfaced and the slice halted for a decision) | `docs/feature-ux-slice-c/evidence.md` | accumulated input 2 |
| 2 | Behavior specs for `abwab-relations-modal` and `abwab-template-copy-modal` (TESTING_DEBT rows 4 + 9-picker) | `features/abwab/components/*/**.spec.ts` | debt trigger |
| 3 | One shared door-picker component replacing the two duplicated pickers | `features/abwab/components/abwab-door-picker/` | debt trigger / README `:434-439` |
| 4 | All six abwab modals on `.qd-modal--fixed` + `__head/__body/__foot`; the three inner `max-block-size` caps deleted | six modal components | 7-applied |
| 5 | `cdkTrapFocus cdkTrapFocusAutoCapture` + correct first-focus on all six | six modal components | 8 |
| 6 | Full dialog semantics on `abwab-sections-modal` **and** `abwab-move-picker` (role, aria-modal, labelledby, Escape) | those two components | 7-adjacent |
| 7 | Relations modal redesigned: `.qd-truncate`/`[title]` names, `.qd-check-row` alignment, `qd-tabs` type segment, `qd-chip` count pill, breathing room | `abwab-relations-modal` | 9a–9d |
| 8 | `.qd-checkbox`/`.qd-check-row` composed where modals render checkboxes (the two pickers → the unified one) | `abwab-door-picker` | 16-applied |
| 9 | `selectedLoading` on the templates facade; «اختر قالبًا» stops doubling as a loading state | `abwab-templates.facade.ts` + templates page | accumulated input 3 |
| 10 | Docs true again: abwab README, `UI_STYLE_SYSTEM.md` §17 debt lines, TESTING_DEBT rows 4/9, stale navbar comment | docs + one comment | doc integrity |

## 2. Scope

**In:**

- The six abwab modals: `abwab-door-modal`, `abwab-template-node-modal`,
  `abwab-sections-modal`, `abwab-move-picker`, `abwab-relations-modal`,
  `abwab-template-copy-modal` — geometry, semantics, focus.
- New specs for the relations and copy modals; spec updates for door, sections,
  move-picker, and the templates facade where this slice changes their behavior.
- The shared form `abwab-door-fields-form` (a `focusFirstField()` method; error surface
  onto reserved `qd-state`).
- `abwab-templates.facade.ts` + `abwab-templates-page` (the `selectedLoading` signal).
- Reproduction and closure of the Slice A relations-read observation (investigation
  only; API calls against the local dev backend; no backend code changes).
- Doc updates the above forces: abwab README, `UI_STYLE_SYSTEM.md` §17 debt/trigger
  lines, `docs/TESTING_DEBT.md` rows 4 and 9, `top-navbar.component.ts:14-15` comment.

**Out (named so nobody "finishes the thought"):**

- The `abwab-tree` and `abwab-cards` checkboxes (item 16's other two call-sites). The
  commissioning scope is "where **modals** render checkboxes"; tree/cards are page
  surfaces → Slice D (§17 `_forms` entry already names "Slice C/D").
- The eleven abwab name-render sites outside the modals (`.qd-truncate` sweep) → Slice D.
- The tree's `.flag.rel` dead chip (README `:380-385`) → Slice D.
- Backend changes of any kind. The observation's resolution is evidence, not code; if a
  real backend bug surfaces, the slice halts Phase 2 and reports (see Phase 2 gate).
- TESTING_DEBT rows 2 and 5 (backend dormancy join tests, the e2e dormancy flow). A
  manual reproduction is not a test; those rows stay.
- The five words detail modals' geometry (§17 convergence trigger `:993-1002` fires on
  *their* next change, not this slice).
- Sections-modal input styling onto `.qd-input` (not an audit item in this slice).
- The words-feature conditional-trap pattern (`drawerTrapEnabled`) — abwab modals never
  stack under the entity-detail overlay, so their traps are unconditional (see R3).
- Any `dev → main` merge.

## 3. Non-goals

- No visual redesign of the five non-relations modals beyond the shared geometry shell.
  Their content is untouched; only head/body/foot placement, semantics, and focus change.
- No new relation features (per-group counts, per-tab counts): the approved concept
  (`docs/design-preview/abwab-relations-concept.html`) has exactly one global count pill
  (`:118`) and none per group — matching content means not inventing counters.
- No E2E additions. The keyboard/geometry acceptance uses the B2 mechanism (temporary
  spec, deleted after evidence) — extraction-style evidence, not a tier.

## 4. Locked decisions

### 4.1 Carried in from the audit / prior slices

1. **Spec → unify → redesign, in that order** (audit `:1089-1092`). Writing item 9's
   redesign before the specs is refactoring untested code. Phases are ordered so this
   cannot be violated by accident: the redesign phase depends on the unified picker,
   which depends on the specs.
2. **§16.1 outranks the concept's ad-hoc active state** (audit 9c, Part 8 `:1059`). The
   type segment composes `qd-tabs`; selected = `--qd-selected-bg` + `--qd-accent-text` +
   `--qd-border-accent`, not the concept's `surface + bold`. The deviation from the
   concept line is recorded in the same change (README note, T803).
3. **The direction pill stays a pill** (audit 9c). It is a binary toggle, not a tab
   strip, and its active state already is §16.1 (9e resolves itself once 9c lands).
4. **Do not convert abwab modals to `qd-detail-modal-shell`** (audit `:252-254`) — that
   component owns overlay history and restore semantics these dialogs don't use.
5. **Composing `--fixed` means deleting the local caps, not adding the class beside
   them** (§17 `:982-992`, the specificity trap). The three caps and their current
   lines: sections `abwab-sections-modal.component.scss:5` (14rem), copy
   `abwab-template-copy-modal.component.scss:43` (13rem), relations
   `abwab-relations-modal.component.scss:202-203` (11rem).
6. **Focus targets per the audit's item 8 fix text** (`:278-284`):
   auto-capture default everywhere; explicit focus only where auto-capture lands wrong —
   the relations and copy modals open on a list, so their search input gets explicit
   focus (surah-jump-picker precedent, `surah-jump-picker.component.ts:207`); the door
   and template-node modals want the name field, reached via a new
   `focusFirstField()` **method on `abwab-door-fields-form`** — the shells must not
   reach into the form's DOM. Sections and move-picker use plain auto-capture.
7. **Tall-dialog trade accepted** (§17 shell precedent, audit `:242`): `--fixed` is a
   fixed `block-size: min(92dvh, 44rem)`; shallow modals (door, template-node) render
   with empty space. That is the contract's "zero resize" trade, not a defect to fix.

### 4.2 Decided by this plan

1. **Shared picker component: `abwab-door-picker`** (selector `qd-abwab-door-picker`),
   at `features/abwab/components/abwab-door-picker/`. Contract in Phase 4 (T401). The
   divergence fold rules: copy's checkbox `aria-label` wins (every row gets one);
   relations' disabled-row + «مرتبط بالفعل» tag becomes an optional input; copy's
   loading/error/empty states become optional status inputs (relations passes none —
   its tree comes from the snapshot cache); chevron box unifies at `1rem` (relations'
   value; copy's local `1.1rem` override dies, per slice-a plan `:180-181`).
2. **Existing `data-testid` values survive unification** via a `testIdPrefix` input, so
   the Phase 3 specs written against the *current* DOM need zero edits when the picker
   is extracted. This is what makes the extraction provably behavior-preserving.
3. **The caps do not move into the picker.** During Phase 4 the two host modals keep
   their `max-block-size` rules locally (retargeted at the picker's list class), so each
   commit is behavior-identical; the caps then die in Phase 5 with the geometry change.
4. **Discard-guard strips render in `__foot`** (door, template-node, and the new
   sections guard). A guard that scrolls away with `__body` content is unusable; the
   foot is `flex-shrink: 0` by contract.
5. **`abwab-sections-modal` gains a dirty guard** — an intentional behavior change,
   named per the T905 precedent (slice-b plan `:706-707`). Dirty = an in-progress rename
   whose draft differs from the saved name, or a non-empty add-section input. Pattern is
   door-modal's `requestClose()`/`confirmDiscard()`/`cancelDiscard()` trio verbatim
   (`abwab-door-modal.component.ts:107-122`).
6. **No dirty guard on the pickers or move-picker.** A picker selection is not a form
   draft; the door-modal family guards *field edits*, and the concept's foot is
   add/close with no guard. Matches current behavior everywhere except sections.
7. **The door-fields-form error surface composes `qd-state variant="error"
   [reserve]="true"`**, replacing the `@if`-conditional `<p role="alert">`
   (`abwab-door-fields-form.component.html:1-5`). This closes the one modal error
   surface B1's T401 list skipped (audit item 5 `:166`; slice-b plan `:457-461` named
   six sites, none in the door modal). Affects door + template-node modals (shared
   form); both named as intentional changes.
8. **The global relations count composes `qd-chip` classes on a static span**
   (`<span class="qd-chip qd-chip--pill qd-chip--static">`), not the `qd-chip`
   component — the component only emits interactive `button`/`a` elements
   (`chip.component.html:10,27,43`) and a count pill is not clickable. Raw-class
   composition has precedent (`word-type-filter.component.html:30,50,58`).
9. **Name truncation follows §17's flexible-with-ellipsis rule, not a hard fixed
   column.** The commissioning prompt says "fixed-width"; §17 `:1054-1095` makes a hard
   fixed column a per-surface exception needing written justification, and none exists
   here. Picker rows and `__target` chips get `flex: 1` + `.qd-truncate` + mandatory
   `[title]`.
10. **Leaf rows keep the hidden chevron element.** The concept itself keeps it
    (`abwab-relations-concept.html:230-231`, `visibility:hidden`), and it is what
    column-aligns leaf and parent rows at the same depth. 9b's dead-space complaint is
    resolved by `.qd-check-row`'s single `--qd-space-2` gap replacing the accumulated
    gaps, matching the concept's `gap:8px`.
11. **Modal width stays `min(100%, 36rem)`** (base `.qd-modal`; `--fixed` adds no
    inline-size). The concept mock is 680px wide, but the geometry standard is the §17
    contract, and "matches the approved concept" binds *content*, not the mock's canvas.
12. **`selectedLoading` renders `qd-skeleton-rows`** in the templates-page detail
    region while a per-template fetch is in flight, replacing the silent
    «اختر قالبًا» window. The README gotcha (`features/abwab/README.md:345-349`) that
    named this gap is removed in the same change.
13. **One light branch off `dev`: `ux-slice-c-modals`.** 24 tasks matches Slice A's
    single-branch precedent (slice-a plan `:441-449`); the specs-first ordering means a
    green safety net exists before any reshaping commit, and per-phase commits keep
    bisection cheap. PR targets `dev`, never `main`. Not a `dev → main` candidate.

## 5. The ground truth this plan is derived from

Read before executing; each row is a measured fact, not an assumption.

| Fact | Where |
|---|---|
| All six modals share the same shell shape: `@if (open())` → `.qd-modal-backdrop` → `<section class="qd-modal …">`, no CDK overlay; hosted as static siblings in `abwab-page.component.html:264-303` and `abwab-templates-page.component.html:202-222` | those files |
| Four modals have full dialog semantics + Escape; **sections and move-picker have neither `role="dialog"` nor Escape** — two modals cannot be ESC-dismissed today | `abwab-sections-modal.component.html:1-9`, `abwab-move-picker.component.html:1-9` |
| No abwab modal has `cdkTrapFocus` or any focus-on-open; the six trap sites in the app are `detail-modal-shell` + five words dialogs, pattern `cdkTrapFocus cdkTrapFocusAutoCapture` with conditional enablement only where layers nest | `root-details-panel.component.html:93-113` et al.; `features/words/README.md:65-67` |
| Specs: door 11 tests, sections 6, move-picker 8; **relations and copy have none**; facade spec 3 tests | component folders; `abwab-templates.facade.spec.ts` |
| The copy picker is a deliberate duplicate of the relations picker; the unification trigger is written in the code itself and in the README | `abwab-template-copy-modal.component.ts:35-39`; README `:434-439` |
| `subtreeMatches` is character-identical in both pickers; `nodesById`, `pickedNames`, `toggleExpanded`, `isPicked`, the `pickerRows` walk are the same modulo the row interface; relations adds `excludedIds` + `linkedIds` per (pair, type) | relations `ts:60-62,156-197,219-222,286-308`; copy `ts:21-23,91-99,124-127,157-172` |
| Divergences to fold: checkbox aria-label (copy only), disabled+tag rows (relations only), loading/error/empty (copy only), caps 11rem vs 13rem, truncation (copy only), chevron 1rem vs 1.1rem, `accent-color` (copy only), single-select mode (relations only) | per-file lines in the Phase 4 tasks |
| Relations modal layout today: h3 + hand-rolled count pill (`scss:14,27`), four stacked groups with dot + `qd-chip`s (`html:36-54`), divider, hand-rolled `aria-pressed` type segment with **no keyboard model** (`html:62-76`, active `scss:131`), direction pill (§16.1-conformant), picker, foot | `abwab-relations-modal.component.*` |
| `__pick-name` has no truncation (`flex:1` only); `__target` chips same defect | relations `scss:246`, `scss:81` |
| Concept ground truth: one global count pill; four groups, empty groups omitted; type-seg active = surface+bold (overruled by §16.1); dir pill active = §16.1 already; pick-row = chevron→checkbox(15px, accent)→name(flex:1)→«مرتبط بالفعل» tag, gap 8px; leaf keeps hidden chevron; selected-bar reserves `min-height:20px` | `docs/design-preview/abwab-relations-concept.html:28-93,118,127-146,228-234` |
| `fetchSelected` sets no loading flag; `selectedTemplate()` is `null` for the whole in-flight window because `select()` writes `selectedIdState` before fetching; the page's empty state is gated only on `!template && !selectedErrorMessage()` | `abwab-templates.facade.ts:50-56,67-68,116-139`; `abwab-templates-page.component.html:87-93` |
| Observation mechanics: the relations read + tree counts share one dormancy predicate (`relation.DeletedAtUtc == null && doorA.DeletedAtUtc == null && doorB.DeletedAtUtc == null`); an archived **anchor** yields `200 + []` (the existence pre-check ignores `DeletedAtUtc`), i.e. empty state + count 0 — the exact recorded symptom pair; POST **rejects** archived endpoints with 400 (`ArchivedDoor`), so birth-dormancy is impossible; e2e sandbox teardown leaves archived doors in the dev DB by design | `EfAbwabRelationsReader.cs:12-38`; `EfAbwabTreeReader.cs:62-82`; `EfAbwabRelationsWriter.cs:22-36`; `e2e/fixtures/abwab.ts:86-95` |
| The relations modal only issues its read in door mode (`!anchorPickMode()`); bulk/anchor-pick add closes without re-reading | `abwab-relations-modal.component.ts:250-265,330-336` |
| Baseline numbers conflict across docs (2,161 vs 2,164 + B2's +2–4) — measure, don't cite | `TESTING_STRATEGY.md:410` vs slice-b plan `:417,597` |
| Stale comment: navbar doc says "nine surfaces: four abwab modals + five words drawers/dialogs" — there are six abwab modals | `top-navbar.component.ts:14-15` |

## 6. Phases

### Phase 1 — Baseline and record (2 tasks)

- **T101** — Baseline on `dev`: full Vitest (`npm test`, fork cap preserved via the npm
  script) + `npm run build`, record file/test counts, timings, and the `dev` SHA into
  `docs/feature-ux-slice-c/evidence.md`. There is no CI (`TESTING_STRATEGY.md` §8);
  every later "no regression" claim measures against this run. The docs disagree on the
  current count — this run settles it.
- **T102** — Record the slice in the root `CLAUDE.md` "Active Spec Kit Feature" section.
  Read the section's current content first, then **replace** — do not append. The
  section still lists `abwab-templates` as open, but that feature closed and merged to
  `dev` (PR #54); the line is stale residue from a close that never cleared it. After
  the edit the section names exactly one open feature: slug `ux-slice-c`, plan
  `docs/feature-ux-slice-c/plan.md`. Clearing the `abwab-templates` line is part of this
  same edit, not a separate chore — a stale open-feature record is what sends agents
  grepping decisions that no longer bind. **Do not sweep
  `docs/feature-abwab-templates/`**: it is one of the two most recently closed features
  and sits inside the N-2 buffer. Create the branch `ux-slice-c-modals` off `dev`.

### Phase 2 — Resolve the Slice A observation, accumulated input 2 (2 tasks)

Ordered first because the commissioning prompt requires it resolved **before any
redesign**, and because its outcome is the only thing that could re-scope this slice.

- **T201** — Reproduce against the local dev backend, recording every id and response
  in `evidence.md`:
  1. `POST /api/abwab/doors` twice → two fresh **live** doors (names prefixed
     `slice-c-repro-`), record ids.
  2. `POST /api/abwab/doors/{a}/relations` linking them → expect 2xx (a 2xx proves both
     endpoints were live; the writer 400s on archived doors).
  3. `GET /api/abwab/doors/{a}/relations` → expect the relation present; `GET` the tree
     → expect both counts = 1.
  4. Open the relations modal on door *a* in the browser → expect the chip visible.
  5. Archive door *b* (`DELETE /api/abwab/doors/{b}`), re-read → expect the relation
     gone from *a*'s list and both counts 0 — dormancy derivation working as specified
     (`Reads/Abwab/README.md:67-75`).
  6. Clean up: archive door *a* (archived residue is the accepted dev-DB convention).
- **T202** — Close the observation in `evidence.md` with a verdict:
  - **Expected outcome:** steps 3–4 show the relation on live doors → the Slice A
    harness most plausibly POSTed against leftover archived `e2e-sandbox-*` doors, and
    the read was *correct* dormancy, not a bug. Record the verdict, note that
    TESTING_DEBT rows 2 + 5 (the automated coverage for exactly this) remain unpaid and
    unaffected, and proceed.
  - **Gate:** if step 3 or 4 shows the relation missing **on live doors**, a real
    read/cache bug exists. STOP the slice, write up the reproduction, and surface it —
    a backend fix is a scope change the user must decide. Do not redesign on top of an
    unexplained read.

### Phase 3 — Specs for the two unspecced modals, debt rows 4 + 9 (2 tasks)

These specs are the debt-trigger exception to the family's no-new-tests posture: they
are the point. Behavior-first per test-guard: drive inputs/DOM, assert rendered
outcomes and emitted outputs; write functions arrive as function inputs exactly as the
page shells bind them (the sections-modal spec at `abwab-sections-modal.component.ts:12-15`
is the in-family harness precedent); no mocking of DTOs or internal methods. Mind the
TDZ label-getter gotcha (README `:377-379`) and Arabic counted-noun forms (`:375-376`).

- **T301** — `abwab-relations-modal.component.spec.ts`, covering row 4's list verbatim
  plus dialog basics, against the **current** DOM (redesign comes later; these specs
  assert behavior, not layout, so they survive Phases 4–6 unchanged):
  1. four-group derivation — relations input renders the four groups (تشابه, تضاد, more-,
     less-comprehensive split by direction), empty groups omitted, global count = length;
  2. type-switch clears picks;
  3. already-linked disabling per (pair, type) — a door linked under the active type
     renders disabled with the «مرتبط بالفعل» tag, and re-enables under another type;
  4. anchor-pick mode — inverted picker semantics and the add-count button label
     (counted-noun form);
  5. mode-dependent direction-pill copy — door mode names the targets, anchor-pick mode
     names the anchor (the row-4 inversion regression);
  6. search filters by `subtreeMatches` and auto-expands matching parents;
  7. Escape and backdrop click emit close; reopen resets error and picks.
  Run: `npm test -- --include="src/app/features/abwab/components/abwab-relations-modal/*.spec.ts"` → green.
- **T302** — `abwab-template-copy-modal.component.spec.ts`, covering row 9's picker
  clause verbatim:
  1. search auto-expand;
  2. multi-select toggling with per-row checkbox `aria-label`;
  3. the targets-not-union count on the submit button;
  4. the selection surviving a `409` (duplicate outcome keeps picks and shows the error);
  5. loading → `qd-skeleton-rows`, error → `qd-state` with retry, empty → `qd-state`;
  6. Escape and backdrop click emit close.
  Run: focused glob for the component → green. Commit each spec separately.

### Phase 4 — Unify the two pickers, debt trigger (2 tasks)

- **T401** — Extract `abwab-door-picker` (`features/abwab/components/abwab-door-picker/`),
  behavior-identical. Contract:

  ```ts
  // selector: 'qd-abwab-door-picker'
  nodes        = input.required<readonly AbwabNode[]>();
  pickedIds    = input.required<readonly number[]>();
  excludedIds  = input<readonly number[]>([]);   // never rendered (anchor / copy source)
  disabledIds  = input<readonly number[]>([]);   // rendered, disabled
  disabledTag  = input('');                      // «مرتبط بالفعل بهذا النوع» — relations passes it
  single       = input(false);                   // anchor-pick mode
  status       = input<'ready' | 'loading' | 'error' | 'empty'>('ready'); // copy drives; relations always 'ready'
  errorMessage = input('');
  searchPlaceholder = input.required<string>();
  testIdPrefix = input.required<string>();       // existing data-testids survive verbatim
  toggled      = output<number>();
  retry        = output<void>();
  // focusSearch(): void — viewChild on the search input; Phase 5 calls it
  ```

  Internals move over verbatim: `subtreeMatches`, `nodesById`, the `pickerRows` walk,
  `toggleExpanded`, search auto-expand, the depth CSS var, the hidden-leaf-chevron
  alignment comment. Both modals rewire onto it; selection state stays in the modals
  (consumer-owned, like `qd-tabs`). The two host SCSS files keep their caps locally,
  retargeted (`.abwab-relations-modal .abwab-door-picker__list { max-block-size: 11rem; }`,
  copy 13rem) so this commit is visually inert. **Acceptance: both Phase 3 specs pass
  with zero edits.** Delete the duplication comment at
  `abwab-template-copy-modal.component.ts:35-39` — the trigger it describes is being
  paid in this commit.
- **T402** — Compose the primitives inside the picker, once:
  - rows become `.qd-check-row`; checkboxes get `class="qd-checkbox"` and every row an
    `aria-label` (copy's pattern generalized — the relations checkbox finally gets an
    accessible name, audit 9b/16);
  - name span gets `flex: 1` + `.qd-truncate` + `[title]="row.node.name"` (§17
    truncation contract: `[title]` is mandatory);
  - delete the superseded local rules in both host SCSS files and the picker's own:
    copy's checkbox sizing/`accent-color` (`abwab-template-copy-modal.component.scss:84-87`)
    and `1.1rem` chevron (`:62-73` → unify at 1rem), copy's hand-rolled truncation
    (`:89-95`), relations' bare `__pick-name` rule (`:246`). The §17 specificity trap
    is the checklist here: adding the class without deleting the local rule reads as
    "done" and is not.
  - Update both specs only where a selector moved; behavior assertions unchanged. Run
    both focused globs → green.

### Phase 5 — Restructure all six onto the fixed shell, items 7-applied + 7-adjacent + 8 (6 tasks)

The canonical shell every modal converges on (only names, testids, and the close
handler vary — this is what the byte-share acceptance diffs):

```html
@if (open()) {
  <div class="qd-modal-backdrop" data-testid="…-backdrop" (click)="requestClose()">
    <section
      class="qd-modal qd-modal--fixed abwab-…-modal"
      role="dialog"
      aria-modal="true"
      dir="rtl"
      [attr.aria-labelledby]="titleId"
      qdModalScrollLock
      cdkTrapFocus
      cdkTrapFocusAutoCapture
      (click)="$event.stopPropagation()"
      (keydown.escape)="requestClose()"
    >
      <header class="qd-modal__head"><!-- h3 + context/description --></header>
      <div class="qd-modal__body"><!-- everything that may grow --></div>
      <footer class="qd-modal__foot"><!-- actions; discard strip when guarded --></footer>
    </section>
  </div>
}
```

`cdkTrapFocus`/`cdkTrapFocusAutoCapture` come from `@angular/cdk/a11y` (`A11yModule`),
already a dependency (words feature). Traps are **unconditional** here — abwab modals
never stack under another trap layer (see R3; the words conditional-trap rule at
`features/words/README.md:65-67` governs *nesting*, which abwab doesn't do). Every
modal keeps `__foot` (it is load-bearing for the body/foot seam padding — §17
`:959-975`). Each task updates that modal's spec in the same commit (Escape, focus-on-
open via `document.activeElement`, guard behavior where changed) and runs its focused
glob.

- **T501** — `abwab-door-modal`: restructure onto the shell; discard strip moves to
  `__foot`; add `focusFirstField()` to `abwab-door-fields-form` (viewChild on the name
  input) and call it from the open-effect; convert the form's error `<p>` to
  `qd-state variant="error" [reserve]="true"` (decision 4.2-7, named intentional
  change). Update the 11-test spec for the new error surface + focus.
- **T502** — `abwab-template-node-modal`: same shell, same `focusFirstField()` call,
  inherits the form's new error surface. (Still unspecced beyond row 9's remainder —
  do not write its workshop spec here; row 9's narrowed remainder keeps that trigger.)
- **T503** — `abwab-sections-modal`: shell + **full dialog semantics** (role,
  aria-modal, `aria-labelledby` onto the existing `<h3>`, Escape) + **delete the 14rem
  cap** (`scss:5`) — the list now grows and `__body` scrolls + **dirty guard**
  (decision 4.2-5: rename-draft-differs or add-input-non-empty →
  `requestClose()`/`confirmDiscard()`/`cancelDiscard()`, strip in `__foot`; intentional
  behavior change, named). Auto-capture focus (first tabbable). Update the 6-test spec:
  Escape, guard, focus.
- **T504** — `abwab-move-picker`: shell + full dialog semantics + auto-capture (first
  row button). No cap to delete, no guard. Update the 8-test spec: Escape, focus.
- **T505** — `abwab-relations-modal`: shell (geometry only — layout redesign is Phase
  6); **delete the 11rem cap** (the retargeted rule from T401); explicit
  `picker.focusSearch()` in the open-effect for both door and anchor-pick modes. Spec:
  add Escape/focus cases if T301 left any gap.
- **T506** — `abwab-template-copy-modal`: shell; **delete the 13rem cap**; explicit
  `picker.focusSearch()`. Spec: same treatment.

### Phase 6 — Relations modal redesign, item 9 (4 tasks)

- **T601** — 9c type segment → `qd-tabs`: replace the hand-rolled `role="group"` +
  `aria-pressed` buttons (`html:62-76`) with the `qd-tabs` component + `qdTab`
  directive (three tabs: تشابه / تضاد / شمولية, keeping the colored `type-dot` spans as
  tab content); selection stays the existing `type` signal; the roving-tabindex RTL
  keyboard model arrives free. Delete the local `__types`/`__type--active` rules
  (`scss:102-136`). T301's "type-switch clears picks" case must pass unchanged — that
  behavior is wiring, not widget. Direction pill untouched (decision 4.1-3).
- **T602** — 9c count + 9d breathing: global count becomes
  `<span class="qd-chip qd-chip--pill qd-chip--static">` (decision 4.2-8), deleting the
  hand-rolled `__count` rules (`scss:14-32`); group spacing opens up —
  `__group { margin-block-end: var(--qd-space-4) }`, chip gap comes from the group
  row's gap token rather than the file's tightest value (audit 9d: DESIGN.md
  calm-for-long-focus is the register; most of the old spacing CSS dies with the
  composed chips).
- **T603** — 9a remaining truncation: `__target` chips in the direction preview get
  `.qd-truncate` + `[title]` (picker rows were done in T402). Grep the modal SCSS for
  any surviving hand-rolled `overflow`/`text-overflow` and delete.
- **T604** — Visual acceptance against the concept: open the modal in the browser on a
  door with relations in all four groups; screenshot into `evidence.md`; check content
  parity with `abwab-relations-concept.html` (groups render with dots and chips, empty
  groups omitted, one count pill, type segment + conditional direction row + picker +
  selected-bar + foot) and record the one sanctioned deviation: tab active state is
  §16.1, not the mock's surface+bold.

### Phase 7 — Templates facade `selectedLoading`, accumulated input 3 (2 tasks)

- **T701** — Facade: add `selectedLoadingState` signal; `fetchSelected` sets it `true`
  before the request and `false` in both the `tap` and `catchError` arms; expose
  `readonly selectedLoading`. Spec first (the facade is specced — extend
  `abwab-templates.facade.spec.ts`'s existing harness): loading is `true` during an
  in-flight `select()` with no template and no error shown; `false` after success;
  `false` after failure with the error set. Then implement; focused run → green.
- **T702** — Page: gate the detail region — `@if (facade.selectedLoading())` →
  `qd-skeleton-rows` (testid `abwab-templates-page-selected-loading`), else the
  existing `!template` / error logic (`abwab-templates-page.component.html:87-93`).
  Remove the paid gotcha from the README (`features/abwab/README.md:345-349`). The
  «اختر قالبًا» copy itself is untouched — it now means only what it says.

### Phase 8 — Verification and doc integrity (4 tasks)

- **T801** — Tier B against the T101 baseline: full Vitest + `npm run build`. Expected
  delta, stated in advance: **+2 spec files** (relations modal, copy modal) and roughly
  **+30–50 tests** (two new suites + extended door/sections/move-picker/facade specs);
  zero removed. Any other delta is explained or fixed before proceeding. Tier B rather
  than A because the slice touched surfaces that had no specs at all — the wider net is
  the compensating control (slice-b plan `:610-612`); no backend change ⇒ no `dotnet
  test`, no route-smoke tier.
- **T802** — Keyboard-only acceptance, all six modals, in the browser: open (from its
  real trigger) → focus lands per decision 4.1-6 → Tab cycles stay inside the dialog →
  ESC closes (or raises the guard where dirty). Record the pass matrix in
  `evidence.md`. Geometry byte-share check: extract each modal's shell block (backdrop
  line through `__head` open tag), diff modulo component name / testid / close handler
  → identical; `grep -rn "max-block-size" src/app/features/abwab/components/` → zero
  hits in modal SCSS.
- **T803** — Docs true again, same change as the facts they describe:
  - abwab README: picker-unification note (`:434-439`) rewritten as paid (one
    `abwab-door-picker`, specs exist); render-chain entries for the six modals + the
    new picker; sections/move-picker semantics + the sections guard; the §16.1-over-
    concept deviation note; selectedLoading gotcha already removed in T702.
  - `UI_STYLE_SYSTEM.md` §17: `.qd-modal--fixed` entry — "applying it to the six abwab
    modals is Slice C's job" (`:1003-1005`) becomes the done statement, specificity-
    trap cap list updated (caps deleted); checkbox entry debt line narrowed to
    tree/cards (Slice D); truncation entry debt line narrowed (picker + target sites
    composed).
  - `docs/TESTING_DEBT.md`: **row 4 deleted** (its tests landed — rows are deleted when
    paid, never marked done); row 5 stays (e2e dormancy flow unpaid); **row 9
    narrowed** — the copy-picker clause comes out, the tree editor / node modal / page
    remainder stays with its trigger.
  - `top-navbar.component.ts:14-15`: recount the inert surfaces at edit time (six abwab
    modals + the words dialogs) and correct the comment.
- **T804** — Close-out sweep: `grep -rn` the whole repo for every deleted/renamed
  selector and path (`__types`, `__type--active`, `__count` rules, the old picker class
  names, `abwab-door-fields-form__error`, the cap selectors) — prose included; fix any
  dangling reference. Final `evidence.md` entry: baseline vs closing numbers, the
  observation verdict, acceptance artifacts. The slice closes per the lifecycle rule
  only at merge (clear the Active-Feature record then, as a `chore` commit — B's
  `b84385f0` precedent).

## 7. Testing posture

- The new specs (T301, T302, facade extension) are the debt-trigger exception to the
  abwab family's no-new-tests posture — they are the point of the slice. Behavior-first
  per test-guard: no implementation-detail assertions, function inputs as the harness
  boundary (in-family precedent: sections-modal spec), real DTOs, Arabic label rules
  respected.
- Per-phase: Tier A focused globs (`npm test -- --include="src/app/features/abwab/**/*.spec.ts"`
  or narrower), fork cap preserved. Existing suites + build stay green at every commit.
- Pre-PR: Tier C = Tier B run of T801 + `npm run build` (frontend-only change; no
  backend commands, no `SmokeRouteCatalog`).
- The browser passes (T604, T802) and the Phase 2 reproduction are extraction-style
  evidence — not a tier, no substitute for the Vitest suite or the build, and no E2E
  run is cited as a gate.

## 8. Risk register

| Risk | Why it is real | Mitigation |
|---|---|---|
| `--fixed` composed but a cap survives → silent no-op that "reads as done" | §17 names this exact trap (`:982-992`); local selectors outrank the primitive | Cap deletion is an explicit step in T503/T505/T506; T802 greps for `max-block-size` |
| Unification silently changes picker behavior | 375-line component with zero specs until Phase 3 | Specs land first; T401's acceptance is "both specs pass with zero edits"; testids preserved by contract (decision 4.2-2) |
| Unconditional `cdkTrapFocus` breaks a nested-layer case later | words README bans unconditional traps *where layers nest* | Abwab modals never stack under the entity-detail overlay or each other; recorded here + README note in T803 so a future nesting change re-evaluates. `app.nested-layers.spec.ts` still pins the words rule |
| The observation turns out to be a real read bug | Then the modal redesign would sit on an unexplained data path | Phase 2 runs first and is a hard gate: real bug → stop, surface, await decision |
| Focus assertions flaky under Vitest/jsdom | auto-capture timing | Assert `document.activeElement` after stabilization; words nested-layers spec is the working precedent |
| Sections dirty guard surprises users of a specced modal | Behavior change on a shipped surface | Named intentional change (T905 precedent wording), spec updated in the same commit, README documents it |
| Fixed 44rem height makes shallow modals look empty | It will — door modal is short | Accepted trade per §17 shell contract and audit `:242`; recorded so nobody "fixes" it back to content-height |
| Baseline numbers cited from stale docs | Three documents disagree today | T101 measures; T801 compares only against T101 |

## 9. Obligations checklist (all must be true at close)

- [ ] Baseline recorded (T101) before any change; closing run compared against it (T801)
- [ ] Observation resolved with recorded door ids and verdict — or slice halted at the Phase 2 gate
- [ ] Specs written **before** unification; unification **before** redesign (commit order proves it)
- [ ] Both Phase 3 specs passed T401 with zero edits
- [ ] All three `max-block-size` caps deleted; grep-clean
- [ ] All six modals byte-share the shell (T802 diff), each with role/aria-modal/labelledby/Escape/trap/scroll-lock
- [ ] Keyboard-only pass matrix recorded for all six
- [ ] Relations modal content matches the concept at the new standard; §16.1 deviation recorded
- [ ] Every checkbox a modal renders has an accessible name and composes `.qd-checkbox`/`.qd-check-row`
- [ ] Truncated names in the modal carry `[title]` (§17: contract, not a nit)
- [ ] `selectedLoading` shipped; README gotcha removed; facade spec extended
- [ ] TESTING_DEBT row 4 deleted, row 9 narrowed, rows 2/5 untouched
- [ ] README + §17 + navbar comment updated in the same changes as the facts
- [ ] Fork cap preserved on every test run; no E2E cited as a gate
- [ ] PR targets `dev`; no `dev → main`

## 10. Execution note

One light branch off `dev`: `ux-slice-c-modals` (decision 4.2-13). Commits per task or
tight task-pair, phases in order — the ordering **is** the discipline (spec → unify →
redesign; observation before everything that could sit on top of it).

| Phase | Title | Items | Tasks |
|---|---|---|---|
| 1 | Baseline and record | — | T101–T102 (2) |
| 2 | Resolve the Slice A observation | input 2 | T201–T202 (2) |
| 3 | Specs for the two unspecced modals | debt rows 4+9 | T301–T302 (2) |
| 4 | Unify the pickers | debt trigger | T401–T402 (2) |
| 5 | Restructure six modals onto the fixed shell | 7-applied, 7-adjacent, 8 | T501–T506 (6) |
| 6 | Relations modal redesign | 9a–9d, 16-applied | T601–T604 (4) |
| 7 | Templates facade `selectedLoading` | input 3 | T701–T702 (2) |
| 8 | Verification and doc integrity | — | T801–T804 (4) |

**24 tasks. Guard: under 30 — one slice, no split** (seam recorded in §0 in case
execution learns otherwise).
