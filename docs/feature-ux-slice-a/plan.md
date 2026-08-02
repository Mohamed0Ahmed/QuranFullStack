# Slice A — shared primitives and rules (UX audit)

Source: `docs/abwab-ux-audit.md` → "Slice A — Shared primitives and rules (pattern
decisions)". This slice builds **only** the primitives and writes **only** the rules; it
applies almost none of them to abwab. Slices B–I consume it.

**Mode when this plan was written:** plan-only. No code, no Git, nothing amended yet.

---

## 0. Guard result — 24 tasks, one slice, no split

Counted below: 2 + 4 + 3 + 3 + 3 + 5 + 2 + 2 = **24 tasks** across 8 phases. Under the 30
threshold, so this stays one slice.

Had it split, the line would have been **CSS/token-only** (phases 2, 3, 4, 7 — checkbox,
`--qd-z-*`, `.qd-modal--fixed`, truncation rule) versus **Angular-component** (phases 5, 6 —
the `qd-state` `reserve` input and `qd-context-menu`), because those two are the only phases
carrying spec/`data-testid` risk and they review differently. Recorded so a mid-execution split
does not get drawn on task count.

---

## 1. Objective

Land the six shared decisions the audit put ahead of every abwab fix, so no later slice
hand-rolls what a primitive is about to provide:

| # | Deliverable | Home | Audit item |
|---|---|---|---|
| 1 | `--qd-z-*` layer scale | `styles/_tokens.scss` | 6 (prerequisite) |
| 2 | `.qd-checkbox` / `.qd-check-row` | `styles/_forms.scss` | 16 |
| 3 | `.qd-modal--fixed` geometry + slots | `styles/_components.scss` | 7 |
| 4 | `reserve` input on `qd-state` | `shared/ui/state/` | 5 |
| 5 | `qd-context-menu` | `shared/ui/context-menu/` | 21 (half) |
| 6 | Truncatable-name rule + `.qd-truncate` | `styles/_utilities.scss` | 15 (half) |

Every one of the six owes its `UI_STYLE_SYSTEM.md` §17 contract entry and its
`styles/README.md` / `shared/README.md` line **in this slice**. A primitive that ships without
its contract entry is the failure this whole audit was written to stop.

---

## 2. Scope

In:

- The six deliverables above, plus repointing the app's existing `z-index` literals onto the
  new scale (phase 2) — a scale nobody uses is two sources of truth.
- Composing `qd-context-menu` in **both** existing call-sites (`abwab-page`,
  `abwab-templates-page`) and deleting their duplicated SCSS. An extraction with one consumer
  is not an extraction.
- Doc amendments: `UI_STYLE_SYSTEM.md` §17 (six entries), §4 (tokens), `styles/README.md`,
  `src/app/shared/README.md`, and one note in `features/abwab/README.md` for the menu move.

Out (named so nobody "finishes the thought"):

- **Applying** `.qd-checkbox` to the four checkbox call-sites → Slice C/D.
- **Applying** `.qd-modal--fixed` to the six abwab modals and restructuring their markup into
  head/body/foot → Slice C.
- **Applying** `reserve` to abwab's six error surfaces → Slice B/C.
- **Applying** `.qd-truncate` / `[title]` to the eleven name-render sites → Slice C/D.
- Adding right-click and the keyboard menu path to `abwab-template-tree` → Slice G (this slice
  only moves the menu's *shell* so that work lands in code that has stopped moving).
- Converging `explorer-detail-modal` onto the fixed-geometry rule → its own slice, five shipped
  words modals, own review. Phase 4 records the trigger.
- The sticky navbar itself → Slice B. This slice only reserves its rung.

---

## 3. Non-goals

- No new tests except phase 5's additive assertions on an already-specced component
  (`state.component.spec.ts` exists). Every other phase is designed to be provably
  zero-test-change; where that is a claim rather than a fact, the plan names the evidence.
- No visual change to any currently shipped surface, with **one flagged exception** (T203,
  deferrable — see phase 2).
- No `qd-` class renames. `.qd-explorer-frame`'s awkward name is a Slice B question.
- No behavior change to `qd-detail-modal-shell`. It is the precedent, not the subject.

---

## 4. Locked decisions

### 4.1 Carried in from the audit (user-resolved, recorded here for the slices that consume them)

| Decision | Consumed by |
|---|---|
| **Sticky navbar wins** the item 4 vs item 6 conflict. Item 4 becomes "the content region reserves a viewport so the footer sits below the fold consistently"; the navbar does not scroll away. | Slice B. This slice reserves the navbar's z-index rung and nothing else. |
| **Compose `qd-tabs`** for the relations modal's type segment (item 9c), accepting §16.1's selected treatment over the concept's `surface`+bold. §16.1 is the doctrine; a mockup's ad-hoc active state does not outrank it. Record the concept deviation at `abwab-relations-concept.html:57-62`. | Slice C. No task here. |
| **An empty-root template refuses with `400`** rather than becoming a silent no-op apply. | Slice G. Recorded so its plan does not re-open it. |

### 4.2 Decided by this plan

- **`.qd-modal--fixed` is an opt-in modifier, not a change to the `.qd-modal` base.** See
  phase 4 for the specificity evidence. This is what keeps "zero change to shipped surfaces"
  true.
- **The reserved error slot is a `reserve` input on `qd-state`, not a new `qd-error-slot`
  component.** `state.component.spec.ts` already exists, so an additive input extends a specced
  component instead of creating an unspecced one, and it reuses the sizing rule
  `styles/README.md` already states: *"Size a new reserved slot from these tokens; never
  re-measure the control by hand."*
- **`qd-context-menu` takes `data-testid` values as inputs.** Non-negotiable: 4 Vitest
  assertions and ~8 Playwright assertions select `abwab-page-context-menu` and
  `abwab-page-ctx-backdrop` by test id. Inputs are what make the extraction zero-test-change.
- **Modifier naming is `--`**, matching `.qd-chip--pill` / `.qd-skeleton--block`, not the older
  `.qd-btn-primary` form.
- **`dvh`, not `vh`**, for every modal block-size (§17 already says so; `explorer-detail-modal`
  is the only `vh` hold-out and phase 4 does not touch it).

---

## 5. The ground truth this plan is derived from

Read before executing; each row is a measured fact, not an assumption.

### 5.1 The app has three modal geometries, and §17's rule is applied in exactly one

| Selector | Geometry | Scroller |
|---|---|---|
| `.qd-modal` (`_components.scss:554-564`) | `width: min(100%, 36rem)`; **no block-size** | **none** |
| `.qd-modal.explorer-detail-modal` (`:441-451`, `:481-489`) | `42rem`; **`max-height: min(90vh, 36rem)`** desktop, fixed `min(88dvh,42rem)` ≤tablet | `__body` |
| `.detail-modal-shell` (`detail-modal-shell.component.scss:12-15`) | `46rem` × **fixed `min(92dvh, 44rem)`** | `__body` (`:91-93`) |

§17 mandates the third form (*"a fixed block-size, never `max-block-size`"*). The second
violates it on desktop. The first has neither. **Twelve consumers** total: the shell, five
words modals on the `explorer-detail-modal` variant, and the six abwab modals on the bare base.

**Specificity, verified:** `.qd-modal.explorer-detail-modal` is `(0,2,0)` and
`.detail-modal-shell` is `(0,2,0)` under Angular's emulated encapsulation; a bare `.qd-modal`
rule is `(0,1,0)`. So the shell is immune either way — but `explorer-detail-modal` sets
`max-height` and never `height`/`block-size`, so a `block-size` added to the **base** *would*
apply to it and clamp five shipped words modals. That is the whole reason phase 4 is opt-in.

### 5.2 No abwab modal sets its own geometry — but three set inner scroller heights

None of the six sets `width`, `inline-size`, or any block-size. Three set an inner
`max-block-size` that becomes redundant (and arguably wrong) once the modal body is the single
scroller: `abwab-sections-modal.component.scss:14` (14rem list),
`abwab-template-copy-modal.component.scss:52` (13rem pick-list),
`abwab-relations-modal.component.scss:221` (11rem pick-list). **Slice C's problem, named here
so it is not discovered there.**

### 5.3 `.qd-modal { padding: var(--qd-space-5) }` fights a single-scroller body

Both existing fixed variants deal with it: `explorer-detail-modal` sets `padding: 0` and pads
its slots; the shell pads header and body separately. `.qd-modal--fixed` must make the same
call, and that call is what decides whether Slice C's markup restructure is mechanical or
invasive.

### 5.4 The z-index landscape — and a latent ordering defect

Every `z-index` in the app, in order:

| Value | Owner |
|---|---|
| 5 | `mushaf-header-navigation.component.scss:7` |
| 20 | `source-selector.component.scss:89` (popover) |
| 30 | `surah-jump-picker.component.scss:57`, `explorer-association-filter.component.scss:71` (popovers) |
| 40 | `detail-modal-shell.component.scss:103` (the fixed restore control) |
| 49 / 50 | abwab context-menu backdrop / menu — **twice**, `abwab-page.component.scss:88,94` and `abwab-templates-page.component.scss:146,152` |
| 50 | `.qd-modal-backdrop` (`_components.scss:546`) |
| 100 | `top-navbar.component.scss:63` — `.dropdown-menu` |
| 200 | `top-navbar.component.scss:136` — `.mobile-menu` |

**The defect:** the navbar's dropdown (100) and mobile menu (200) paint **above** every modal
backdrop (50). It is reachable today — the six abwab modals do not make the shell `inert` (only
the global detail overlay does, `app.ts:13-14`), so hovering «الكلمات» while an abwab modal is
open opens a dropdown over the dialog. Sticky navbar (Slice B) makes it more visible, not less.

**Also latent:** `detail-modal-shell.component.scss:98` hard-codes `3.5rem` in
`inset-block-start: calc(var(--qd-space-4) + 3.5rem)` — that is `--qd-navbar-block-size`'s
value (`_tokens.scss:76`) written by hand. Sticky navbar will invalidate the assumption behind
it. Fixed in T204 while the area is open.

### 5.5 What the concepts already specify (so three of these are contract gaps, not requests)

- `abwab-relations-concept.html:84` — `.pick-row input{width:15px; height:15px;
  accent-color:var(--qd-primary);}`. The checkbox sizing is **approved contract**; the
  implementation dropped it. `abwab-template-copy-modal.component.scss:75-76` independently
  re-added `1.1rem` — a local override to delete in Slice C once `.qd-checkbox` exists.
- `abwab-tree-concept.html:41,207` — the section tab count badge (Slice F).
- `abwab-relations-concept.html:118` — the count pill (Slice C).

### 5.6 Test surface at risk

- `state.component.spec.ts` **exists** → phase 5 extends it (the one place new assertions are
  warranted).
- Context-menu test ids are asserted in `abwab-page.component.spec.ts:449,453,578,593` and in
  `e2e/abwab-operations.e2e.ts:110-146` + `e2e/abwab-url-and-a11y.e2e.ts:149`. Phase 6's
  `testId` inputs are what keep all of those passing untouched.
- No spec asserts `.qd-modal`'s computed geometry, any checkbox styling, or any `z-index`.

---

## 6. Phases

Task IDs are `TAnn`. Ordering inside a phase is dependency order.

### Phase 1 — Baseline and record (2 tasks)

- **T101** — Capture the **pre-change** baseline as evidence: full Frontend Vitest suite +
  `npm run build`, both green, with counts and timings recorded. Every later "no regression"
  claim in this slice is measured against this run, and there is no CI to fall back on
  (`TESTING_STRATEGY.md` §8).
- **T102** — At execution time (not now), record the slice in the root `CLAUDE.md` "Active Spec
  Kit Feature" section per its own instruction, naming this plan. Reverted/cleared when the
  slice closes and its `docs/feature-ux-slice-a/` folder is swept.

### Phase 2 — The `--qd-z-*` layer scale (4 tasks)

- **T201** — Add the scale to `styles/_tokens.scss`, one rung per existing role, derived from
  §5.4's table rather than invented:
  `--qd-z-sticky` (page chrome and sticky headers) · `--qd-z-popover` (selector/filter panels) ·
  `--qd-z-floating` (the restore control) · `--qd-z-menu-backdrop` · `--qd-z-menu` ·
  `--qd-z-modal-backdrop` · `--qd-z-modal` · `--qd-z-mobile-nav`.
  **Reserve `--qd-z-sticky` below `--qd-z-menu-backdrop`** — that is the rung Slice B's sticky
  navbar consumes, and it must sit under the abwab row menu or the menu paints behind the
  chrome. Keep the numeric values **identical to today's** so this task alone changes nothing
  visually.
- **T202** — Repoint the **eight non-menu** literals onto the tokens, 1:1, no reordering.
  Purely mechanical; no DOM touched.
  **Deliberately excluded: the four menu literals** (`abwab-page.component.scss:88,94` and
  `abwab-templates-page.component.scss:146,152`) — they sit inside the exact SCSS blocks
  T602/T603 delete, so tokenizing them here would be work undone two phases later.
  `qd-context-menu` uses `var(--qd-z-menu-backdrop)` / `var(--qd-z-menu)` from birth (T601), so
  §9's "zero bare `z-index` literals" obligation is still provably met at close — just by
  phase 6 rather than phase 2.
- **T203** — **Flagged, deferrable, the slice's only intentional visual/behavioral change.**
  Fix §5.4's ordering defect so navbar layers sit **below** the modal layers. Two candidate
  shapes: (a) lower `.dropdown-menu` / `.mobile-menu` beneath `--qd-z-modal-backdrop`, or
  (b) close/suppress navbar menus while a modal is open, since a *visible* dropdown under a dim
  backdrop is still wrong even when it paints below.
  **(a) is a two-value CSS change. (b) is materially more expensive than it looks and must not
  be scoped from here:** abwab's six modals never touch `DetailOverlayHistoryService` (that is
  the global detail overlay's own state, which is why the words drawers can suspend their focus
  trap off `isOpen`). What abwab's modals hold is `ScrollLockService`, whose `lockCount` is
  **private with no signal, observable, or getter** (`scroll-lock.service.ts:13-31`) — so
  "while any modal is open" means adding observable state to a reference-counted service
  shared with the global overlay. That is a shared-service change with its own review surface,
  not a navbar tweak.
  **Recommend (a) now; leave (b) to Slice B, shape TBD.** **If the user prefers a strictly
  zero-change Slice A, defer this whole task to Slice B** — nothing else here depends on it.
- **T204** — Docs: `UI_STYLE_SYSTEM.md` §4 gains the layer-scale token category with the rung
  order and the rule *"never write a bare `z-index`"*; `styles/README.md`'s `_tokens.scss`
  bullet names it. Also replace the hard-coded `3.5rem` at
  `detail-modal-shell.component.scss:98` with `var(--qd-navbar-block-size)` (§5.4).

### Phase 3 — `.qd-checkbox` / `.qd-check-row` (3 tasks)

- **T301** — Add to `styles/_forms.scss` (the partial the README already assigns to shared
  input styling, loaded after `_components.scss` per its documented import order):
  - `.qd-checkbox` — fixed square box (~`1rem`; reconcile the concept's `15px` with
    `abwab-template-copy-modal`'s `1.1rem` and pick **one** value, tokenized as
    `--qd-checkbox-size` beside the existing `--qd-control-block-size` family so it cannot
    drift), `flex: none`, `margin: 0`, `accent-color: var(--qd-accent)`, and the standard
    `:focus-visible` ring.
  - `.qd-check-row` — the label/row wrapper: `display: flex; align-items: center` with a
    single `--qd-space-2` gap between box and label, so the "far from its label" gap cannot be
    reintroduced per call-site.
  - **No** new hue; `accent-color` resolves off `--qd-accent` and is therefore theme-correct in
    both themes with no `_themes.scss` change.
- **T302** — §17 gains a **form-controls** entry (a genuine gap in that section: it covers
  tabs, chips, state, tables, detail lists, ayah cards, the dialog shell, and skeletons, but no
  input control). State the accessible-name obligation as part of the contract — *every*
  checkbox carries a real `<label for>` or an `aria-label`; two of the four existing call-sites
  have neither.
- **T303** — `styles/README.md`'s `_forms.scss` bullet names the new family and its
  compose-don't-re-style rule.

### Phase 4 — `.qd-modal--fixed` (3 tasks)

- **T401** — Add to `styles/_components.scss`, modelled directly on
  `detail-modal-shell.component.scss:12-15, 91-93, 140`:
  - `.qd-modal--fixed` — `display: flex; flex-direction: column;`
    `block-size: min(92dvh, <N>rem)` (**fixed**, never `max-block-size`), `padding: 0`
    (§5.3 — padding moves to the slots), and `overflow: hidden`.
  - `.qd-modal__head` / `.qd-modal__foot` — `flex-shrink: 0`, own padding.
  - `.qd-modal__body` — `flex: 1; min-block-size: 0; overflow-y: auto`, **the only scroller**,
    plus `scrollbar-gutter: stable` so it does not reintroduce audit item 3 one level down.
  - Phone (`≤ $qd-bp-phone-max`) near-fullscreen override, mirroring the shell's.
  - `<N>` needs one in-browser check against the tallest abwab dialog (relations, four groups +
    segment + direction row + picker + foot) before it is fixed; the shell's `44rem` is the
    starting guess, not the answer.
- **T402** — §17 gains a `.qd-modal` entry that states: the base is width-only and
  scroller-less; `--fixed` is the opt-in that carries the §17 geometry rule; the head/body/foot
  slot contract; and — required, or the third geometry becomes permanent by default — **the
  convergence trigger for `.qd-modal.explorer-detail-modal`**, which today uses
  `max-height: min(90vh, 36rem)` in violation of the same rule. Trigger: the next change that
  touches any of the five words detail modals' geometry converges all five onto `--fixed` and
  deletes the `vh` hold-out. §17 tolerates `qd-detail-modal-shell` as its own component; it must
  not silently tolerate a fourth geometry.
- **T403** — `styles/README.md`'s `_components.scss` bullet names the modifier and the slots.

### Phase 5 — `reserve` on `qd-state` (3 tasks)

- **T501** — Add an additive `reserve` input (default off, so all seven existing call-sites are
  untouched). When on, the component always renders its box with a `min-block-size` **sized
  from the existing control/slot tokens** per `styles/README.md`'s standing rule, and only the
  message text fades in — opacity only, static under `prefers-reduced-motion`. The precedents
  to copy verbatim: `explorer-result-count.component.html` (all three states render the same
  one-line box) and `detail-modal-shell.component.html:37-44` + `scss:62-63` (*"always rendered
  so its reserved box never appears/disappears"*).
- **T502** — Extend `state.component.spec.ts`: `reserve` off keeps today's mount/unmount
  behavior (regression guard for the seven call-sites), `reserve` on keeps the box present with
  an empty message. The only new test assertions in the slice.
- **T503** — §17's `qd-state` entry gains the `reserve` input and the no-layout-shift rationale
  (cross-referencing §17's existing N3 doctrine so the two do not drift);
  `src/app/shared/README.md`'s `ui/state/` bullet names it.

### Phase 6 — `qd-context-menu` (5 tasks)

- **T601** — New `src/app/shared/ui/context-menu/`, presentation-only, `OnPush`, owning exactly
  what both existing copies own and nothing more:
  - a `position: fixed; inset: 0` transparent backdrop at `--qd-z-menu-backdrop` emitting
    `dismissed`, and a positioned `role="menu"` at `--qd-z-menu` (`left`/`top` from an
    `{x, y}` input);
  - the item styling both copies duplicate today — hover, `:focus-visible` ring, and the
    `--danger` item variant;
  - **`menuTestId` and `backdropTestId` inputs** (§4.2), so `abwab-page-context-menu` /
    `abwab-page-ctx-backdrop` / `abwab-templates-page-*` survive byte-identically;
  - items stay **projected content** (`<ng-content>`) carrying their own test ids, labels, and
    handlers — the primitive learns nothing about doors or template nodes;
  - `Escape` dismisses via a **document-level** `@HostListener('document:keydown.escape')`,
    copying `top-navbar.component.ts:45-56`, which does exactly this for its two dropdowns.
    **It must be document-level, not `(keydown.escape)` on the menu element:** none of the four
    open paths puts focus inside the menu — the keyboard path
    (`abwab-tree.component.ts:246-253`) leaves focus on the tree row and the three mouse paths
    move focus nowhere — so an element-bound handler would never receive the key. A control that
    cannot fire plus a §17 entry claiming it works is worse than no control. This is the one
    place this phase is not literally behavior-preserving (neither copy dismisses on Escape
    today); call it out in review as an additive a11y gain.
  - **Out of scope:** focus management *into* the menu. Both copies lack it, adding it changes
    keyboard behavior on a shipped surface, and it belongs with Slice G's keyboard-path work.
    Document-level Escape is deliberately chosen because it is the one keyboard affordance that
    works **without** that focus work.
- **T602** — Compose it in `abwab-page.component.html:243-260`; delete
  `abwab-page.component.scss:85-131` (backdrop, menu, item, hover, focus, danger).
- **T603** — Compose it in `abwab-templates-page.component.html:208-251`; delete the
  byte-identical block in `abwab-templates-page.component.scss:143-...`. Keep the root-vs-node
  item swap and its explanatory comment in the page — that is page logic, not menu shell.
- **T604** — Docs: §17 entry (purpose, inputs, the projected-items boundary, the test-id
  inputs and *why* they exist, and the document-level Escape with its reason). **The entry must
  also state the two gaps the primitive deliberately did NOT fix**, or the next agent reads
  "shared, §17-contracted menu" and assumes they are solved: (1) **no viewport clamping** —
  both copies position from raw pointer coords via `[style.left.px]`/`[style.top.px]`, so a menu
  opened near the inline-start edge under RTL overflows, and the faithful extraction preserves
  that; (2) **no focus management into the menu**. Naming them is the difference between an
  honest contract and the drift this audit exists to stop.
  Also: `shared/README.md` `ui/context-menu/` bullet; and a line in
  `features/abwab/README.md` recording that both pages now compose the shared menu, since that
  README currently describes the menu as page-rendered markup.
- **T605** — Evidence for the zero-test-change claim: run `e2e/abwab-operations.e2e.ts` and
  `e2e/abwab-url-and-a11y.e2e.ts`, the two specs that assert the menu's test ids.
  **This is evidence for this extraction only — it is explicitly not a tier and never a
  substitute for the Vitest suite or the build** (`Frontend/quran-dashboard-ui/CLAUDE.md`;
  `TESTING_STRATEGY.md` §6). Note the Abwab e2e specs write to the local dev DB through their
  sandbox section and leave archived residue behind by design.

### Phase 7 — The truncatable-name rule (2 tasks)

- **T701** — Add `.qd-truncate` to `styles/_utilities.scss` — `min-inline-size: 0;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap` — on the exact shape of
  `.qd-scroll-stable` (`_utilities.scss:54-56`), the app's established one-concern utility.
  It deliberately does **not** set `flex`: the call-site owns its flex context, and the app's
  rule is flexible-with-ellipsis (`detail-modal-shell.component.scss:28-35`), not a hard column.
  Add `--qd-name-min-inline-size` for call-sites that want the audit's "reserved minimum" so the
  reservation is one token, not eleven hand-picked numbers.
- **T702** — §17 gains a **truncatable entity names** rule: reserved minimum + `.qd-truncate` +
  a **mandatory `[title]`** with the full name (precedent `word-type-filter.component.html:57`).
  State plainly that the app's rule is flexible-with-ellipsis and that a *hard* fixed column is
  a per-surface exception needing its own justification — the audit found the user asking for
  fixed width where every existing precedent is flexible, and the §17 entry is where that gets
  settled once instead of eleven times.

### Phase 8 — Verification and doc integrity (2 tasks)

- **T801** — Full Frontend Vitest suite (fork cap preserved) + `npm run build`, compared
  against T101's baseline. Tier B is required, not optional: this slice touches `styles/` and
  `shared/`. Evidence records file/test counts and the delta, which should be **+0 files** and
  **+2–3 tests** (T502 only).
- **T802** — `grep -rn` the repo for every path and selector this slice moved — the deleted
  page-level menu SCSS class names, `.qd-modal` mentions in docs, `z-index` literals — and
  confirm no README, `.architecture/*` doc, skill, or spec still points at something that
  moved. Dangling references are a defect per the root `CLAUDE.md`, not an acceptable cost.

---

## 7. Testing posture

- **Tier B (full Frontend suite) + `npm run build`** is the gate, because the changed scope is
  `shared/` + `styles/` + the app shell's token layer
  (`Frontend/quran-dashboard-ui/CLAUDE.md`; `TESTING_STRATEGY.md` §4, §6). Baseline at T101,
  compare at T801. Preserve the `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap baked into
  `npm test` — nothing enforces it and there is no CI (§8), so it is a review obligation.
- **No new tests except T502.** Every other phase is either CSS-only or, in phase 6's case,
  designed for selector stability via test-id inputs. If any existing spec does break, that is
  a **signal the extraction changed behavior** — fix the code, do not rewrite the assertion.
- **T605's two e2e specs are evidence for the menu extraction, not a tier.** Stated twice, in
  the phase and here, because the frontend CLAUDE.md is emphatic and a reviewer will check the
  wording.
- No backend change anywhere in this slice → no `dotnet test`, no route-smoke tier, no
  `SmokeRouteCatalog` entry.

---

## 8. Risk register

| Risk | Why it is real | Mitigation in the plan |
|---|---|---|
| `.qd-modal` base change silently resizes five words modals | `explorer-detail-modal` sets `max-height` but no `height`, so a base `block-size` applies and clamps | Opt-in `--fixed` (§4.2, §5.1); base untouched |
| A z-index scale that omits a consumer becomes authoritative and wrong | Next agent trusts it | T201 derives the rungs from a complete inventory (§5.4); T202 repoints all twelve |
| The menu extraction breaks 12 test assertions | 4 Vitest + ~8 Playwright select by test id | `menuTestId`/`backdropTestId` inputs (T601); T605 proves it |
| A primitive ships without its §17 entry | Exactly the drift this audit documents | Every phase carries its doc task; T802 sweeps |
| `.qd-modal--fixed`'s `<N>rem` guessed wrong, so Slice C inherits a too-short dialog | Relations is the tallest and has no spec | T401 requires one in-browser check against relations before fixing the value |
| Three abwab modals' inner `max-block-size` fights the new single scroller | `sections:14`, `copy:52`, `relations:221` | Named in §5.2 as Slice C's task, not silently inherited |
| T203 makes this slice non-zero-change | It is a real behavior fix, not a refactor | Flagged and independently deferrable to Slice B |

---

## 9. Obligations checklist (all must be true at close)

- [ ] Six primitives/rules shipped; **six §17 entries** written, including phase 4's
      `explorer-detail-modal` convergence trigger.
- [ ] `styles/README.md` amended for `_tokens.scss`, `_forms.scss`, `_components.scss`,
      `_utilities.scss`.
- [ ] `src/app/shared/README.md` amended for `ui/state/` and `ui/context-menu/`.
- [ ] `UI_STYLE_SYSTEM.md` §4 carries the layer-scale token category.
- [ ] `features/abwab/README.md` records that both pages compose the shared menu.
- [ ] Zero bare `z-index` literals remain outside `_tokens.scss`.
- [ ] Both duplicated context-menu SCSS blocks deleted (not just one).
- [ ] T101 and T801 evidence recorded, with the test-count delta explained.
- [ ] T605 evidence recorded, labelled as extraction evidence and not as a tier.
- [ ] T802 grep clean — no dangling reference to anything moved.
- [ ] T203 either done or explicitly deferred to Slice B **in writing**, not silently dropped.
- [ ] Root `CLAUDE.md` "Active Spec Kit Feature" updated at start (T102) and cleared at close,
      and this `docs/feature-ux-slice-a/` folder swept per the planning-artifact lifecycle rule.

---

## 10. Execution note

Slice A is CSS/token work plus two small shared components, all zero-DOM-change by design
except the two menu call-sites and the flagged T203. **A light branch off `dev` is the right
call** — `ux-slice-a` — because phase 6 touches two shipped pages and phase 2 touches eight
files across four features, so a single revert point is worth having. It is not a
`dev → main` candidate; abwab's routes are still unprotected (`features/abwab/README.md`).
Per the root `CLAUDE.md` branching model, ALL work branches off `dev` and PRs target `dev`,
never `main`.
