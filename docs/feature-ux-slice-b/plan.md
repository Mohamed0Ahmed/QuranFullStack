# Slice B — page frame and layout stability (UX audit)

Source: `docs/abwab-ux-audit.md` → "Slice B — Page frame and layout stability". Items **1**
(full-bleed frame), **2** (skeletons + `qd-state`), **3-applied** (two pick-list scrollers),
**4** (content reserves a viewport), **6** (sticky navbar), **5-applied** (six reserved error
slots), **17** (stats bar).

**Mode when this plan was written:** plan-only. No code, no Git, nothing amended.

**Precondition — VERIFIED on `dev` at plan time.** Slice A is merged (`3644b772`) and all four
primitives this slice consumes exist in `dev`:

| Primitive | Verified at |
|---|---|
| `--qd-z-*` layer scale (12 lines, 8 rungs) | `src/styles/_tokens.scss` |
| `reserve` input on `qd-state` | `state.component.ts:26` |
| `.qd-truncate` | `_utilities.scss:58` |
| `.qd-modal--fixed` | `_components.scss:572` |

`.qd-modal--fixed` is listed because it is *not* consumed here — Slice C applies it. It is named
so no phase in this slice restructures a modal's geometry on the way past.

---

## 0. Guard result — 37 tasks, so this slice SPLITS in two

Counted below: **B1 = 16 tasks**, **B2 = 21 tasks**, total **37**. Over the 30 threshold, so the
guard trips and the slice splits. It is drawn here, before execution, rather than mid-flight.

**The split line is the review surface, and it also happens to be the dependency order:**

| Sub-slice | Items | Scope of the diff | Reviews as |
|---|---|---|---|
| **B1 — states** (phases 1–5, `T101`–`T504`) | 2, 5-applied, 3-applied | `features/abwab/**` only, composing existing shared primitives | Component composition. Blast radius = abwab. |
| **B2 — frame** (phases 6–11, `T601`–`T1103`) | 1, 4, 6, 17 | `styles/**`, `core/layout/**`, `shared/ui/**`, plus one cross-feature component promotion | App-shell change. Blast radius = **every page**, the mushaf reader, and five words surfaces. |

**B1 runs first, and that ordering is load-bearing, not stylistic.** Abwab's loading branch
(`abwab-page.component.html:47`) today replaces the *entire* toolbar + tree subtree with a
one-line `<p>`. If B2's viewport reservation (item 4) landed first, the interim shipped state
between the two branches would be a viewport-tall empty box containing one line of text — worse
than today, and visible on `dev`. Converting the states first (B1) means the reservation in B2
lands on top of boxes that already hold their shape. Reversing the order requires B2 to declare
the interim regression acceptable; this plan declines to.

Recorded so nobody re-draws the line on task count: the seam is *not* "CSS vs components" (that
was Slice A's seam). Here both halves touch components. The seam is **who can be hurt by a
mistake** — B1 can only break abwab, B2 can break the mushaf reader and the words drawers.

---

## 1. Objective

Make the abwab surfaces obey the app's own layout-stability doctrine, and give the app shell the
frame that doctrine assumes.

The governing philosophy, quoted from the audit's Part 1 preamble: *nothing ever shifts.* The app
already wrote this down — **`UI_STYLE_SYSTEM.md` §17 "Loading/skeleton system" → "No layout shift
(§N3 doctrine): a skeleton must occupy the box its loaded content will occupy — same padding,
gaps, line boxes and item count."** Abwab is the feature that ignores it, in two places (items 2
and 5) and with two frame-level consequences (items 1 and 4).

| # | Deliverable | Home | Audit item | Sub-slice |
|---|---|---|---|---|
| 1 | 14 text loader / empty / error sites composed onto `qd-state` + the two skeleton primitives | `features/abwab/**` | 2 | B1 |
| 2 | Six error surfaces move into reserved slots via `reserve` | `features/abwab/**` | 5-applied | B1 |
| 3 | `.qd-scroll-stable` on the two pick-list scrollers | two modal SCSS files | 3-applied | B1 |
| 4 | Abwab goes full-bleed on a **renamed, feature-neutral** page frame | `styles/_words-explorer-layout.scss` → its new home | 1 | B2 |
| 5 | The content region reserves a viewport | the page frame | 4 | B2 |
| 6 | Sticky navbar on `--qd-z-sticky`, **plus T203's deferred keyboard half** | `styles/_layout.scss`, `core/layout/**`, `ScrollLockService` | 6 | B2 |
| 7 | A quiet stats bar: total live doors + doors in the open section | promoted `qd-result-count` + abwab toolbar recess | 17 | B2 |

**Yardstick discipline (the rule this plan is written under):** every fix below cites either an
existing precedent (`file:line`) or a Slice A primitive. Where no precedent exists the item is
marked NEW PATTERN and owes a `UI_STYLE_SYSTEM.md` entry in the same phase. There are exactly
three NEW PATTERN artifacts in this slice: the viewport reservation (item 4), sticky app chrome
(item 6), and the chrome-inert rule (item 6's keyboard half).

---

## 2. Scope

In:

- The seven deliverables above.
- **Renaming `.qd-explorer-frame` to a neutral name with the old name kept as an alias.** The
  audit put this as a preference; this plan takes it, because item 17 puts a *words-named* stat
  slot on an *abwab* page and the honest fix is to stop calling shared frame furniture
  "explorer". Precedent for the alias mechanism is already in the tree — `qd-panel-skeleton,
  qd-explorer-panel-skeleton` is a dual selector on one component
  (`explorer-panel-skeleton.component.ts:16`), added for exactly this reason.
- **Promoting `explorer-result-count` from `features/words/` into `shared/ui/`.** It currently
  lives at `features/words/components/explorer-result-count/`, and abwab cannot import across
  features (nothing in the app does; the only cross-feature import today is a test-bed helper at
  `abwab.api.spec.ts:6`). `FRONTEND_STRUCTURE.md:387-388` — *"Truly reusable components may live
  in `shared/`, but only when they are genuinely reused across features"* — is satisfied the
  moment abwab becomes the sixth consumer.
- **Adding `qdModalScrollLock` to `abwab-sections-modal` and `abwab-move-picker`**, the two
  abwab modals that do not hold it (§5.6). Required for item 6's keyboard half to be uniform,
  and a latent defect on its own (the page scrolls behind both dialogs today).
- Doc amendments: `UI_STYLE_SYSTEM.md` (§2 the frame, §4 no new tokens expected, §17 three new
  entries), `styles/README.md`, `src/app/shared/README.md`, `features/abwab/README.md`,
  `features/words/README.md` (the promotion moves a component it documents).

Out (named so nobody "finishes the thought"):

- **`.qd-modal--fixed` applied to any abwab modal** → Slice C. B1 edits modal *contents*
  (an error paragraph, a scroller class); it does not touch any modal's geometry, width, or
  head/body/foot structure.
- **The relations modal's redesign** (item 9: ellipsis, checkbox, `qd-tabs`, spacing) → Slice C.
  B1 deletes its `__error`/`__empty` SCSS because `qd-state` owns that; everything else in that
  file is Slice C's.
- **The two duplicated pickers unified** → Slice C, which is the trigger recorded in
  `features/abwab/README.md`. B1 adds one utility class to each; it does not merge them.
- **`abwab-door-fields-form`'s inline error** → named in the audit's item 5 list but it is a
  *form field* error, not a surface error; it belongs with Slice C's form work. B1 covers the six
  surface sites §5.5 enumerates.
- **The footer** — neither sized, tokenized, nor made inert. See §4.2 for why a
  `--qd-footer-block-size` token is refused.
- **Section tab count badges** (item 19) → Slice F. Item 17's section stat and item 19's tab badge
  are *different numbers* off the same DTO (§5.7); building one is not building the other.
- **The navbar dropdown becoming a hover menu** (item 22) → Slice H. B2 makes the navbar sticky
  and inert-when-locked; it does not restructure its menus.
- **Making the whole shell inert for abwab modals.** Impossible by construction, not deferred:
  abwab's modals render *inside* the shell (unlike the global overlay, which is `app.ts`'s
  sibling), so shell-level inert would inert the dialog too. See §4.4.

---

## 3. Non-goals

- **No new tests except two additive ones**, both on already-specced units, mirroring Slice A's
  T502 discipline:
  - `scroll-lock.service.spec.ts` exists → T904's observable lock state extends it.
  - `abwab-tree.builder.spec.ts` / `abwab-snapshot.facade.spec.ts` exist → T1002's stat
    derivation extends one of them.
  Every other task is designed to be provably zero-test-change; where that is a claim rather
  than a fact, the task names its evidence.
- **No new specs for the unspecced surfaces this slice edits** (`abwab-templates-page`,
  `abwab-template-copy-modal`, `abwab-relations-modal` — `docs/TESTING_DEBT.md` rows 4 and 9).
  Writing them is Slice C's recorded trigger and would break this slice's testing posture. The
  consequence is in the risk register, not hidden.
- **No visual change to any non-abwab surface in B1.** B2 changes non-abwab surfaces by design
  (every page's frame, the mushaf reader's sticky offset, five words surfaces' chrome
  reachability); each is enumerated in §4.4 and §4.5 rather than discovered.
- No `qd-` class renames beyond the one the slice explicitly takes (`.qd-explorer-frame`).
- No backend change anywhere → no `dotnet test`, no route-smoke tier, no `SmokeRouteCatalog`
  entry. Item 17 was confirmed backend-free by the audit and re-confirmed here (§5.7).

---

## 4. Locked decisions

### 4.1 Carried in from the audit and from the user

| Decision | Consequence |
|---|---|
| **The 4-vs-6 conflict is resolved: sticky wins.** Item 4 is read as *"the content region always reserves a full viewport, so the footer sits below the fold consistently on every page"*; the navbar does not scroll away. | Both items land in B2, in that order: reserve first, stick second. |
| **Slice A's T203 shape (b) — the keyboard half — is scoped here.** Slice A shipped shape (a) (paint order) and measured that the modal backdrop now also intercepts the *pointer* over the navbar; the navbar nonetheless stays **focusable**, so a keyboard user can still open a dropdown beneath a dim backdrop. | T904 is that fix. |
| **Item 17 is derived entirely from the existing snapshot; no backend.** | Re-verified at §5.7, including the one semantic trap. |

### 4.2 A `--qd-footer-block-size` token is REFUSED

The audit's item 4 offers *"add `--qd-footer-block-size` rather than a magic number"*. Measured,
the footer has **no stable height**: `qd-footer.component.html` renders a health indicator with
three branches (loading text / success with an optional second database chip / error text **plus
a retry button**), all inside `.qd-container` and free to wrap at narrow widths. A token would be
a magic number wearing a token's clothes — the exact failure `styles/README.md` warns about.

**Instead:** reserve `calc(100dvh - var(--qd-navbar-block-size))` on the page frame and let the
footer sit wholly below the fold. That satisfies item 4's resolved reading — *"the footer sits
below the fold consistently"* — using only the arithmetic the app already established at
`_tokens.scss:77` (`--qd-mushaf-panel-height`), and it cannot drift when the footer's health chip
changes shape.

### 4.3 `.qd-explorer-frame` → `.qd-page-frame`, with the old class kept as an alias

Both class names carry the same rule block, so the five explorer call-sites keep working
untouched and abwab opts in under the neutral name. This is the CSS form of the dual-selector
alias already shipped at `explorer-panel-skeleton.component.ts:16`. `styles/README.md` records
the alias and that new call-sites use `.qd-page-frame`.

The frame also brings `box-sizing: border-box` (`_words-explorer-layout.scss:58`), which item 4
**needs** — see §5.3. That makes item 1 a hard prerequisite of item 4 inside B2, not a
cosmetic warm-up.

### 4.4 The chrome-inert mechanism: observable lock state + inert on the **navbar**, not the shell

Three candidate mechanisms were considered against the code:

| Candidate | Verdict |
|---|---|
| `inert` on the shell, keyed off an abwab overlay signal | **Impossible.** `app.ts:14` can do this for the global detail overlay because the overlay host is the shell's *sibling*. Abwab's six modals render inside the page, inside `<main>`, inside the shell — shell-level inert would inert the dialog. |
| Observable state on `ScrollLockService` + `inert` on `.qd-navbar` | **Taken.** The navbar is a sibling of `<main>`, so inerting it leaves every dialog interactive. `ScrollLockService` is the one piece of state every modal dialog in the app already holds. |
| A new "any modal open" service | Refused: YAGNI. It would duplicate `lockCount`'s job and give two sources of truth for the same fact. |

`ScrollLockService.lockCount` is currently `private` with no signal, observable, or getter
(`scroll-lock.service.ts:14`) — exactly what Slice A's T203 recorded as the blocker. T904 makes
it a signal and exposes `isLocked`, which is additive and lands in a **specced** service
(`scroll-lock.service.spec.ts`).

**Blast radius, enumerated because it reaches beyond abwab.** Nine surfaces hold the lock today
(`grep -rln qdModalScrollLock`): four abwab modals (`abwab-door-modal`,
`abwab-relations-modal`, `abwab-template-copy-modal`, `abwab-template-node-modal`) and **five
words surfaces** (`root-details-panel`, `lemma-details-panel`, `stem-details-panel`,
`word-type-details-panel`, `word-drilldown-modal`). After T904 the navbar becomes unreachable by
keyboard while any of those nine is open. That is a **behavior change on five shipped words
surfaces that nobody asked about**, and it is accepted deliberately: each of the nine is a modal
dialog, the doctrine "app chrome is not reachable while a modal dialog is open" is not an abwab
quirk, and the existing precedent is *stronger* — `app.ts:14` inerts the entire shell for the
global overlay. Enumerated here so §3's "no unlisted behavior change" stays honest.

Two modals do **not** hold the lock (`abwab-sections-modal`, `abwab-move-picker`), so T905 adds
it. That is a second intentional behavior change — the page stops scrolling behind those two
dialogs — and it gets its own obligations line (§9), the way Slice A gave T203 one.

### 4.5 Sticky offsets must be re-based, or two shipped sticky surfaces regress

Not in the audit's item 6 constraint list; found while measuring. Two elements are `position:
sticky` with a **viewport-relative** offset:

| Element | Offset today | After a sticky navbar |
|---|---|---|
| `mushaf-reader-page.component.scss:53-54` | `top: var(--qd-mushaf-sticky-top)` = `var(--qd-space-3)` (`_tokens.scss:107`) | Sticks **under** the navbar — the app's most-used surface |
| `abwab-page.component.scss:25-33` (`.abwab-page__side`) | `top: var(--qd-space-4)` | Same |

Both must become `calc(var(--qd-navbar-block-size) + <existing offset>)`. T902 owns it.

### 4.6 Composing `qd-state` is zero-test-change, and here is why

`qd-state` keeps the ad-hoc classes as its backing layer — its template renders
`.qd-loading-state` / `.qd-error-state` / `.qd-empty-state` verbatim
(`state.component.html:4,15,26`). So any spec asserting those selectors keeps passing; that is
how the mushaf specs at `selected-word-section.component.spec.ts:120,417,447,462` and
`selected-ayah-section.component.spec.ts:184,322,352` survive the app-wide migration that
already happened.

`qd-state` hard-codes `data-testid="qd-state-loading|error|empty"` and has **no `testId`
input**. Abwab's per-site ids are preserved by putting the existing attribute on the
**`<qd-state>` host element** — `querySelector('[data-testid="…"]')` and Playwright's
`getByTestId` both match a host attribute. Exactly one abwab spec assertion is at stake:
`abwab-page.component.spec.ts:477` (`abwab-page-archive-empty`). **No `testId` input is added to
`qd-state`** — that would be a component change earning new tests for no benefit.

### 4.7 The skeleton matches the **depth-0, non-bulk** tree row, and says so

`abwab-tree__row` is flex, not grid (`abwab-tree.component.scss:6-14`), with a gap of
`--qd-space-2`, an indent of `calc(var(--abwab-tree-depth,0) * var(--qd-space-5) + var(--qd-space-2))`,
and a **variable column count** — the checkbox only exists in bulk mode
(`abwab-tree.component.html:19-22`). No single `grid-template-columns` string reproduces all of
that, which is precisely the hand-guessing §17 forbids.

The resolution is that it does not have to. During a **cold load** there is no selection, no bulk
mode, and nothing expanded, so the only row shape that can exist is depth-0, non-bulk:
`1.25rem` (the chevron's fixed box, `abwab-tree.component.scss:35-37`) · `1fr` (name) · `auto`
(trailing metadata). `rowTemplate="1.25rem 1fr auto"` is therefore a *measured* match, not a
guess — and T502's acceptance harness measures the skeleton row's height against the loaded
row's height to prove it.

---

## 5. The ground truth this plan is derived from

Read before executing. Every row is measured on `dev` at plan time, not assumed.

### 5.1 Abwab is the only data-dense feature still inside the reading measure

`abwab-page.component.html:2` and `abwab-templates-page.component.html:2` are both bare
`<div class="qd-container">`. `.qd-container` caps at `max-width: 72rem`
(`_layout.scss:39-44`). The five explorer pages use the *same* container class plus
`.qd-explorer-frame`, which overrides the cap with `width: 100%; max-width: none;
margin-inline: 0` and adds `box-sizing: border-box; display: flex; flex-direction: column;
gap: 0` and a `padding-block-end` sized for mobile stat bars
(`_words-explorer-layout.scss:53-63`). Precedent call-site:
`roots-explorer-page.component.html:2`.

**The flex caveat is real:** the frame is `display: flex; flex-direction: column`, and
`.abwab-page__layout` is its own flex **row** (`abwab-page.component.scss:8-13`). A column flex
parent with a row child is fine, but the frame's `gap: 0` plus the layout's
`margin-block-start: var(--qd-space-3)` must be checked visually, not assumed.

### 5.2 The shell already reserves a viewport — abwab just doesn't fill it

`.qd-shell-viewport { min-height: 100vh }` (`_layout.scss:7-11`), with `<qd-footer />` rendered
**outside** it (`app-shell.component.html:9`), so every page is `100vh + footer` and a short page
yields exactly one footer-height of scroll. `.qd-shell-viewport main { flex: 1; min-height: 0 }`
(`:13-16`) stretches `<main>` — but `.qd-page` inside it is a plain block at `height: auto`, so
abwab's content collapses to `.abwab-page__tree-card { min-height: 20rem }`
(`abwab-page.component.scss:21-23`) and leaves the rest of the viewport empty.

So item 4 is **not** "make the page scroll"; the shell already does that. It is *"make abwab's
own content region fill the reserved viewport so its state changes cannot resize the frame."*
That reading is what ties item 4 to the acceptance bar instead of to the footer.

`main.qd-shell-main--page-scroll` (`:18-24`) opts a route out of that flex behavior and is used
by **exactly one route** — the mushaf reader (`mushaf.routes.ts:14`). Abwab uses the default.

### 5.3 There is no global `box-sizing: border-box` in this app

Only four selectors set it: `_layout.scss:30` (`.qd-navbar`), `_explorer-tables.scss:178`,
`_words-explorer-layout.scss:58` (`.qd-explorer-frame`), `_components.scss:391`. Consequence:
`min-block-size: calc(100dvh - var(--qd-navbar-block-size))` on a **padded** `.qd-page`
(`_layout.scss:54-56`, `padding: var(--qd-space-5) var(--qd-space-4)`) overshoots the viewport by
its padding under the default `content-box`. Item 1's frame class supplies the `border-box` item
4 needs — hence §4.3's ordering.

### 5.4 The token arithmetic a sticky navbar must not break

- `--qd-navbar-block-size: 3.5rem` (`_tokens.scss:76`); `.qd-navbar` pins `height`, `min-height`
  and `max-height` to it and already sets `box-sizing: border-box` (`_layout.scss:26-37`).
- `--qd-mushaf-panel-height: calc(100dvh - var(--qd-navbar-block-size))` (`_tokens.scss:77`).
  Sticky keeps the navbar **in flow**, so this arithmetic survives — but it is exactly where a
  double-subtraction bug hides, so T903 re-checks the mushaf reader in the browser.
- Slice A already replaced the hand-written `3.5rem` in
  `detail-modal-shell.component.scss` with `var(--qd-navbar-block-size)`, so that call-site is
  already sticky-ready.
- `--qd-z-sticky` is the rung Slice A reserved for this, deliberately **below**
  `--qd-z-menu-backdrop`, so row menus and modals paint above the chrome.

### 5.5 The 14 item-2 sites, enumerated

The audit says "3 loaders + 9 empty/error paragraphs"; a `grep` for the `qd-*` backing classes
returns exactly those 12. **Two more** use the relations modal's own class names and so do not
appear in that grep — they are an SCSS *re-style* to delete, not a class swap. 14 total.

| # | Site | Kind | Fix |
|---|---|---|---|
| 1 | `abwab-page.component.html:47` | loader | `qd-skeleton-rows` (tree/cards/archive load) |
| 2 | `abwab-templates-page.component.html:14` | loader | `qd-skeleton-rows` |
| 3 | `abwab-template-copy-modal.component.html:75` | loader | `qd-skeleton-rows` (pick-list) |
| 4 | `abwab-page.component.html:49` | error | `qd-state` + `reserve` + retry (transport) |
| 5 | `abwab-page.component.html:67` | empty | `qd-state` (id `abwab-page-archive-empty`) |
| 6 | `abwab-page.component.html:95` | empty | `qd-state` (id `abwab-page-empty`) |
| 7 | `abwab-templates-page.component.html:16` | error | `qd-state` + `reserve` |
| 8 | `abwab-templates-page.component.html:59` | empty | `qd-state` |
| 9 | `abwab-templates-page.component.html:65` | error | `qd-state` + `reserve` |
| 10 | `abwab-templates-page.component.html:75` | empty | `qd-state` |
| 11 | `abwab-template-copy-modal.component.html:79` | error | `qd-state` + `reserve` + retry (transport) |
| 12 | `abwab-template-copy-modal.component.html:83` | empty | `qd-state` |
| 13 | `abwab-relations-modal.component.html:23` | error (own class) | `qd-state` + `reserve`; **delete** `__error` SCSS at `:39-46` |
| 14 | `abwab-relations-modal.component.html:36` | empty (own class) | `qd-state`; **delete** `__empty` SCSS at `:99-105` |

The templates **editor panel** load is the one site that wants `qd-panel-skeleton shape="panel"`
rather than rows (`explorer-panel-skeleton.component.ts:24`, `shape: 'lines' | 'rows' | 'panel'`).

### 5.6 The scroll-lock landscape

`qdModalScrollLock` is held by nine surfaces — four abwab modals plus five words surfaces (listed
in §4.4). **`abwab-sections-modal` and `abwab-move-picker` are real modals** — both render
`.qd-modal-backdrop` + `.qd-modal` (`abwab-sections-modal.component.html:2-3`,
`abwab-move-picker.component.html:2-3`) — and hold **no** lock. `ScrollLockService` is
reference-counted with `private lockCount` and no accessor (`scroll-lock.service.ts:9-31`).

`app.nested-layers.spec.ts` is the spec that pins the layered behavior: `:212` asserts
`shell.getAttribute('inert')` is non-null with the global overlay open, `:219` that it clears on
close, `:222` "exactly one focus trap enabled", `:242`/`:258` that the body lock survives until
both layers close. T904 must run it and reason about **inert inside inert** (drawer under global
overlay ⇒ shell inert *and* navbar inert simultaneously).

### 5.7 Item 17's two numbers, and the trap in each

- **Doors in the open section: already on the wire, rendered nowhere.**
  `AbwabTreeSectionDto.doorsInScopeCount`
  (`core/api/generated/models/abwab-tree-section-dto.ts:5`), computed backend-side as every live
  door with that `sectionId` **at any depth**. Zero frontend render sites; it appears only in
  seven spec fixtures. This is the right number for item 17 and the **wrong** number for item 19,
  which wants roots only — Slice F's problem, named here so it is not conflated.
- **Total live doors: derivable, with a semantic trap.** `AbwabTreeSnapshotVm.byId`
  (`abwab.models.ts:132-141`) holds every door, live and archived; `AbwabNode.isArchived` exists
  (`:121`). **But `AbwabNode.sectionId` is `number | null` (`:114`)**, so a live door may belong
  to no section — which means **Σ `doorsInScopeCount` ≤ total live**, and the two stats are not
  reconcilable by arithmetic. T1002 must state which definition ships (recommended: **live-only**,
  consistent with every other count in the feature) and must not assume the sum identity.
- **Precedent for the artifact:** `explorer-result-count.component.html` — all three states
  render the same one-line box, with the header comment recording why (*"the stat used to unmount
  on a list error and collapse the toolbar, and its loading bar sat ~1.4px taller than the loaded
  line"*). Inputs: `count`, `labelPrefix`, `loading`, `hasError`. Its labels come from
  `WORDS_RESULT_COUNT_LABELS` via a **TDZ-safe getter** (`:22`, and the words README explains
  why) — the promotion must preserve that idiom or the bundled test build reads `undefined`.
  The slot is `.uw-toolbar-recess__stat` (`_words-explorer-layout.scss:28-30`), used at
  `roots-explorer-page.component.html:29-41`.
- **Arabic copy is not optional.** `abwab.labels.ts:22` has the private `countPhrase(count,
  forms)` helper with singular / dual / 3–10 / 11+ forms, `ABWAB_LABELS` at `:58`, and
  `abwab.labels.spec.ts` pins the agreement (`[2, 'سيتم أرشفة بابين'…]`, `[11, '… 11 بابًا']`).
  The abwab README mandates it: *"Do not interpolate a bare count into new copy — «سيتم أرشفة 1
  بابًا» is wrong Arabic."*
- **Styling is constrained by PRODUCT.md.** Its anti-reference list names *"identical gradient
  stat cards"* (`PRODUCT.md:90-91`). Item 17 is a quiet inline stat line — the
  `explorer-result-count` one-liner — never a card, never a KPI row.

### 5.8 Test surface at risk

- **Specced, and therefore a real gate:** `abwab-page.component.spec.ts` (one assertion on a site
  this slice edits, `:477`), `abwab-sections-modal.component.spec.ts`,
  `abwab-tree.component.spec.ts`, `abwab-toolbar.component.spec.ts`,
  `abwab-snapshot.facade.spec.ts`, `abwab-tree.builder.spec.ts`, `scroll-lock.service.spec.ts`,
  `app.nested-layers.spec.ts`.
- **Unspecced surfaces this slice edits anyway** — `abwab-templates-page`,
  `abwab-template-copy-modal`, `abwab-relations-modal` (`docs/TESTING_DEBT.md` rows 4 and 9).
  Row 4's trigger is *"the next time the modal changes shape — a component with no spec cannot be
  refactored safely twice."* B1 consumes that trigger without paying it. See §8.
- **E2E that exercises these surfaces:** `e2e/abwab-structure.e2e.ts`,
  `abwab-operations.e2e.ts`, `abwab-archive.e2e.ts`, `abwab-url-and-a11y.e2e.ts`,
  `abwab-global-order.e2e.ts`, plus `e2e/shell-nav.e2e.ts` (the navbar) and
  `e2e/mushaf-reader.e2e.ts` (the sticky panel). Evidence, never a tier.

---

## 6. Phases — B1 (states): items 2, 5-applied, 3-applied

Task IDs are phase-prefixed (`T1nn` = phase 1, `T2nn` = phase 2, …). Ordering inside a phase is dependency order.

### Phase 1 — Baseline and record (2 tasks)

- **T101** — Capture the pre-change baseline: full Frontend Vitest suite via `npm test`
  (**preserving** the `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` cap baked into the script) plus
  `npm run build`, both green, with file/test counts, timings and the commit SHA recorded in
  `docs/feature-ux-slice-b/evidence.md`. Slice A closed at **191 files / 2164 tests**; that is
  the expected starting point, not an assumption to skip measuring. There is no CI
  (`TESTING_STRATEGY.md` §8), so every later "no regression" claim measures against this run.
- **T102** — Record `ux-slice-b` in the root `CLAUDE.md` "Active Spec Kit Feature" section per
  its own instruction, **appending** (`abwab-templates` is still open and must stay). Name this
  plan and the branch.

### Phase 2 — Loading skeletons (3 tasks)

- **T201** — `abwab-page.component.html:47`: replace the text loader with `qd-skeleton-rows`
  `[count]` ≈ 6, `rowTemplate="1.25rem 1fr auto"` (§4.7's measured depth-0 non-bulk row), inside
  the same `.qd-card.abwab-page__tree-card` the loaded tree occupies — **not** in place of the
  toolbar. Today's loader replaces toolbar *and* tree; §5.2's whole point is that the frame must
  not change shape, so the toolbar stays mounted while the tree loads. That is a deliberate
  behavior change, listed here.
- **T202** — `abwab-templates-page.component.html:14`: same treatment for the templates list, and
  the **editor panel** gets `qd-panel-skeleton shape="panel"`
  (`explorer-panel-skeleton.component.ts:24`), which fills its host per §17/N3.
- **T203** — `abwab-template-copy-modal.component.html:75`: the pick-list loader becomes
  `qd-skeleton-rows` sized to that list's row, measured — not the tree's template copied over.

### Phase 3 — `qd-state` composition (4 tasks)

- **T301** — The six **empty** sites (§5.5 rows 5, 6, 8, 10, 12, 14) become `<qd-state
  variant="empty" [message]="…">`, each carrying its existing `data-testid` **on the `qd-state`
  host** (§4.6). Delete no test id and rename none.
- **T302** — The five **error** sites (§5.5 rows 4, 7, 9, 11, 13) become `<qd-state
  variant="error">`. The two **transport** errors (row 4, the page's snapshot load; row 11, the
  copy modal's doors load) additionally take the single `actionLabel` retry §17 permits, wired to
  the facade's existing `refresh()` / re-load path — abwab today offers **no** recovery from a
  failed snapshot load at all.
- **T303** — Delete the re-styled SCSS `qd-state` now owns:
  `abwab-relations-modal.component.scss:39-46` (`__error`) and `:99-105` (`__empty`). Deletion,
  not porting — §17: *"Compose, do not re-style."*
- **T304** — Docs: `features/abwab/README.md` records that abwab's states are now composed
  primitives (it currently describes hand-rolled paragraphs). No `UI_STYLE_SYSTEM.md` change is
  owed — this phase creates no pattern, it consumes two.

### Phase 4 — Reserved error slots, item 5-applied (3 tasks)

- **T401** — Turn `reserve` on for the **six** error surfaces §5.5/§5.6 identify:
  `abwab-page.component.html:49`, `abwab-templates-page.component.html:16` and `:65`,
  `abwab-relations-modal.component.html:23`, `abwab-template-copy-modal.component.html:17-21`,
  `abwab-sections-modal.component.html:6-10`. `reserve` is Slice A's additive input, default
  off, so this is one attribute per site and no component change.
- **T402** — The page-level error stops replacing the view. `abwab-page.component.html:47-49`
  currently swaps the entire toolbar + tree branch for a paragraph; the audit's recommendation —
  taken here — is that a load failure **keeps the toolbar visible**, which is safe because
  `AbwabSnapshotFacade` retains the previous snapshot on refresh failure
  (`abwab-snapshot.facade.ts`). Restructure the `@if` so the error is a reserved slot *inside*
  the frame rather than a replacement *of* it.
- **T403** — The two archive-confirm cards mount inside the sticky `<aside>` and push it
  (`abwab-page.component.html:141-168`, `abwab-page.component.scss:35-44`). Reserve their space
  the same way, so confirming an archive does not shove the side panel. This is the one item-5
  site that is a *card*, not a text state, so it reserves via `min-block-size` on the aside slot
  rather than via `qd-state`.

### Phase 5 — Pick-list scroll stability + B1 verification (4 tasks)

- **T501** — Add `qd-scroll-stable` (`_utilities.scss:54-56`) to both pick-list scrollers:
  `abwab-relations-modal.component.html:120` (`__pick-list`, `scss:220-224`,
  `max-block-size: 11rem; overflow: auto`) and
  `abwab-template-copy-modal.component.html:38` (`scss:52-53`, `13rem`). Precedent for composing
  the utility rather than re-declaring the property: `mushaf-page-view.component.html:1`,
  `study-context-section.component.html:2`.
- **T502** — Layout-stability acceptance for B1. See §7.2 for the matrix, the invariant, and the
  exact assertion. Numbers recorded in `evidence.md`, labelled evidence and **not** a tier.
- **T503** — Tier B: full Vitest + `npm run build`, compared against T101. Expected **+0 files,
  +0 tests** — B1 adds no tests (§3). If any spec breaks, per Slice A's rule: **fix the code, not
  the assertion.**
- **T504** — `grep -rn` sweep for every selector and path B1 moved (the deleted relations-modal
  `__error`/`__empty` class names, the `qd-loading-state` mentions in abwab docs, every
  `file:line` citation into the six files B1 edited) and repoint anything dangling. Dangling
  references are a defect per the root `CLAUDE.md`.

---

## 7. Phases — B2 (frame): items 1, 4, 6, 17

Task IDs continue the same phase-prefixed scheme (`T6nn` = phase 6 … `T11nn` = phase 11). B2 starts from a `dev` that already contains B1.

### Phase 6 — Baseline (1 task)

- **T601** — Re-baseline: full Vitest + `npm run build`, counts and SHA appended to
  `evidence.md`. B1 changed abwab markup, so B1's closing numbers — not Slice A's — are B2's
  comparison point.

### Phase 7 — The page frame, item 1 (4 tasks)

- **T701** — Rename `.qd-explorer-frame` to `.qd-page-frame` in
  `styles/_words-explorer-layout.scss:53-63`, keeping `.qd-explorer-frame` on the same rule block
  as an alias (§4.3). Zero call-sites change in this task.
- **T702** — Move the rule out of `_words-explorer-layout.scss` into `styles/_layout.scss`,
  beside `.qd-container`, since it is no longer words furniture. Keep the import order valid
  (`styles.scss:1-11`; `layout` loads before `words-explorer-layout`, so the move is safe — verify,
  do not assume).
- **T703** — Add `qd-page-frame` to `abwab-page.component.html:2` and
  `abwab-templates-page.component.html:2`. Verify the two caveats §5.1 names: the column-flex
  frame against `.abwab-page__layout`'s flex row, and the frame's mobile-stat-bar
  `padding-block-end` against abwab's own bottom spacing. Record what you observed, in the
  browser, not in prose.
- **T704** — Docs: `styles/README.md` (the rename, the alias, and that new call-sites use the
  neutral name), `UI_STYLE_SYSTEM.md` §2, `features/words/README.md` (it documents the old class),
  `features/abwab/README.md`.

### Phase 8 — The viewport reservation, item 4 (3 tasks)

- **T801** — On the abwab page frame: `min-block-size: calc(100dvh - var(--qd-navbar-block-size))`.
  `box-sizing: border-box` arrives with T703's frame class (§5.3) — **assert that in the diff**,
  because without it the reservation overshoots by `.qd-page`'s padding.
- **T802** — Make the reservation *do* something: the tree/cards/archive card stretches to fill
  it (`flex: 1` in the frame's column context) and `.abwab-page__tree-card`'s
  `min-height: 20rem` (`abwab-page.component.scss:21-23`) is replaced by that stretch, so no
  state — loading, loaded, empty, error — can resize the frame. This is the task that makes
  §7.2's acceptance provable rather than aspirational.
- **T803** — Docs: this is a **NEW PATTERN** (§1) — no page in the app declares its own content
  minimum today. It owes a `UI_STYLE_SYSTEM.md` entry stating the arithmetic
  (`100dvh` minus the navbar token, never a footer number — §4.2), that it is **abwab-local for
  now**, and the trigger for generalizing it to every page. Also `features/abwab/README.md`.

### Phase 9 — Sticky navbar, item 6 (5 tasks)

- **T901** — `.qd-navbar` (`_layout.scss:26-37`) becomes `position: sticky; inset-block-start: 0;
  z-index: var(--qd-z-sticky)`. Slice A reserved that rung below `--qd-z-menu-backdrop`
  specifically so row menus and modals paint above the chrome.
- **T902** — Re-base both viewport-relative sticky offsets onto the navbar token (§4.5):
  `--qd-mushaf-sticky-top` (`_tokens.scss:107`) and `.abwab-page__side`'s `top`
  (`abwab-page.component.scss:25-33`). Without this, the mushaf reader's sticky panel and abwab's
  own side panel slide under the chrome.
- **T903** — Browser verification of item 6's own three constraints plus §4.5's: the navbar
  dropdowns still escape the navbar's new stacking context (`top-navbar.component.html:45,107`);
  `--qd-mushaf-panel-height`'s `100dvh - navbar` arithmetic did not double-subtract
  (`_tokens.scss:77`); both re-based sticky offsets sit flush under the chrome. Run
  `e2e/shell-nav.e2e.ts` and `e2e/mushaf-reader.e2e.ts` as the regression guard — the same role
  `shell-nav` played for Slice A's own T203.
- **T904** — T203's keyboard half. `ScrollLockService` gains signal-backed state and a public
  `isLocked` (§4.4); `.qd-navbar` takes `[attr.inert]` + `[attr.aria-hidden]` off it, copying
  `app.ts:14`'s pairing. Extend `scroll-lock.service.spec.ts` (the one additive spec in B2 —
  specced service, new public API). **Run `app.nested-layers.spec.ts` and reason about inert
  inside inert** (§5.6): with a words drawer under the global overlay, the shell and the navbar
  are both inert at once; confirm `:222`'s "exactly one focus trap" still holds and record what
  you observed.
- **T905** — Add `qdModalScrollLock` to `abwab-sections-modal` and `abwab-move-picker` (§5.6),
  so the chrome-inert rule covers all six abwab modals rather than four. Second intentional
  behavior change; it also stops the page scrolling behind those two dialogs.

### Phase 10 — The stats bar, item 17 (5 tasks)

- **T1001** — Promote `explorer-result-count` from
  `features/words/components/explorer-result-count/` to `shared/ui/result-count/`, selector
  `qd-result-count, qd-explorer-result-count` (the dual-selector alias precedent,
  `explorer-panel-skeleton.component.ts:16`), so the five words call-sites and their spec keep
  working untouched. Move `WORDS_RESULT_COUNT_LABELS` to a shared label home **preserving the
  TDZ-safe getter idiom** (§5.7) — dropping it makes the labels `undefined` in the bundled test
  build. Move `explorer-result-count.component.spec.ts` with the component.
- **T1002** — Derive the two numbers in the **specced** builder/facade layer
  (`abwab-tree.builder.ts` + `abwab-tree.builder.spec.ts`, or the facade and its spec): total
  **live** doors, and doors in the active section from
  `AbwabTreeSectionDto.doorsInScopeCount`. State the live-only choice in a comment tied to the
  reason (§5.7), and **do not** assert the sum identity — `sectionId` is nullable, so
  Σ section counts ≤ total. This is B2's second and last additive spec.
- **T1003** — Compose two `qd-result-count` instances in a toolbar-recess slot above the abwab
  toolbar, using `.uw-toolbar-recess__stat`'s shape (`_words-explorer-layout.scss:28-30`;
  call-site `roots-explorer-page.component.html:29-41`). The section stat recomputes from
  `activeSectionId()` (`abwab-page.component.ts:71`). Quiet numbers only — `PRODUCT.md:90-91`
  forbids stat cards (§5.7).
- **T1004** — Arabic copy through `countPhrase`'s forms tables (`abwab.labels.ts:22,58`), never
  a bare interpolated count, and «كل الأبواب» gets its **own** label — it is a total, not a
  section count. Extend `abwab.labels.spec.ts`'s existing data-driven agreement cases rather
  than writing a new describe block.
- **T1005** — Docs: `UI_STYLE_SYSTEM.md` §17 entry for `qd-result-count` under its new shared
  name, noting the alias and *why* it exists; `shared/README.md` (`ui/result-count/`);
  `features/words/README.md` (the component left the feature); `features/abwab/README.md` (the
  stats bar and its two definitions, including the nullable-section caveat).

### Phase 11 — B2 verification and doc integrity (3 tasks)

- **T1101** — Layout-stability acceptance, full matrix, §7.2. This is the run that answers the
  slice's headline claim, so it happens **after** every item has landed.
- **T1102** — Tier B: full Vitest + `npm run build` against T601. Expected **+0 files** and
  **+2–4 tests** (T904 and T1002/T1004 only).
- **T1103** — `grep -rn` sweep for everything B2 moved: `.qd-explorer-frame`,
  `explorer-result-count`'s old path, `WORDS_RESULT_COUNT_LABELS`, `--qd-mushaf-sticky-top`, and
  every `file:line` citation into `_layout.scss`, `_words-explorer-layout.scss`,
  `_tokens.scss`, `app.ts` and the two abwab pages. Repoint anything dangling; a doc that cites a
  moved line is a defect, not an acceptable cost. **Note from Slice A:** its own sweep grepped
  class names and literals and still missed a stale *prose* claim in `UI_STYLE_SYSTEM.md` §4 —
  grep the prose too.

---

## 7.1 Testing posture

- **Tier B (full Frontend suite) + `npm run build` is the gate for both sub-slices.** B1 touches
  `features/abwab/**` only, which Tier A would cover — but it edits three surfaces with **no
  specs at all** (§5.8), so the wider net is the compensating control, not ceremony. B2 touches
  `styles/`, `core/layout/`, `shared/ui/` and a cross-feature move, where Tier B is mandatory
  (`Frontend/quran-dashboard-ui/CLAUDE.md`; `TESTING_STRATEGY.md` §4, §6).
- **Preserve `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`**, baked into `npm test`. Nothing enforces
  it and there is no CI (§8), so it is a review obligation.
- **Exactly two additive spec extensions in the whole slice**, both on already-specced units:
  `scroll-lock.service.spec.ts` (T904) and the builder/labels specs (T1002/T1004). Everything
  else is zero-test-change by design. If any other spec breaks, that is a **signal the
  composition changed behavior — fix the code, do not rewrite the assertion.**
- **E2E is evidence, never a tier** (`Frontend/quran-dashboard-ui/CLAUDE.md`;
  `TESTING_STRATEGY.md` §6). Stated once here and once per task that runs one. The five abwab
  specs write to the local dev DB through their sandbox and leave archived residue by design.
- **No backend change → no `dotnet test`, no route-smoke tier, no `SmokeRouteCatalog` entry.**

## 7.2 Layout-stability acceptance — how the "nothing shifts" claim is proved

The audit's philosophy is the acceptance bar, so it is measured, not asserted. Slice A
established the mechanism twice (the T401 geometry measurement and the T203 hit-test), and the
same harness pattern is used here: a **temporary** Playwright spec named `abwab-tmp-*.e2e.ts`
(the `abwab` project's `testMatch` requires the prefix; single-worker per `e2e/README.md`),
deleted after the numbers are recorded.

**The invariant element** is the *frame* — the element carrying the reservation
(`.abwab-page`) — plus the **toolbar**, because item 5's page-level case is precisely a toolbar
that collapses on error. Not the card: the card is allowed to change its contents.

**The assertion** is equality of `getBoundingClientRect()` (`height`, and `top`/`bottom` for the
toolbar) across every cell of this matrix, at a fixed viewport:

| Axis | Cells |
|---|---|
| view | `tree` · `cards` · `archive` |
| state | `loading` · `loaded` · `empty` · `error` |
| search | off · on (a query that matches, and one that matches nothing) |

`loading` is reached by delaying the tree response, `error` by failing it, `empty` by an empty
sandbox section — all three drivable from the existing `abwabSandbox` fixture plus route
interception. Additionally: the **skeleton row height must equal the loaded row height** (§4.7's
open question, answered by measurement), and the **reserved error slot must not change the
frame's height** when it fills.

Recorded in `evidence.md` as a table of measured pixel values, with the wording *"extraction-style
evidence for the layout-stability claim; not a tier and no substitute for the Vitest suite or the
build."* Run once at T502 (B1's states) and once at T1101 (B2's frame), because a shift can be
introduced by either half.

---

## 8. Risk register

| Risk | Why it is real | Mitigation |
|---|---|---|
| B1 edits three surfaces with **no specs** (`abwab-templates-page`, copy modal, relations modal) | `docs/TESTING_DEBT.md` rows 4 and 9; row 4 says plainly *"a component with no spec cannot be refactored safely twice"* — Slice C is the trigger to write them, and B1 spends that trigger's first refactor without paying it | Accepted, not hidden. Compensating controls: Tier B instead of Tier A, the five abwab e2e specs, and §7.2's acceptance harness. **Slice C still owes those specs and must not treat B1 as having discharged the debt.** |
| The chrome-inert rule changes keyboard behavior on **five words surfaces** | `ScrollLockService` is `providedIn: 'root'` and shared; the fix cannot be scoped to abwab without leaking feature identity into `core/` | Enumerated in §4.4, listed as an intentional change, and weaker than the existing `app.ts:14` precedent that inerts the whole shell |
| Sticky navbar breaks two shipped sticky surfaces | Both offsets are viewport-relative (§4.5); the mushaf reader is the app's most-used surface | T902 re-bases both; T903 runs `mushaf-reader.e2e.ts` and checks in the browser |
| The viewport reservation overshoots by `.qd-page`'s padding | No global `box-sizing: border-box` (§5.3) | T703 (frame, which carries `border-box`) is a hard prerequisite of T801, and T801 asserts it in the diff |
| A `--qd-footer-block-size` token gets added anyway by a later agent | The audit suggests it | §4.2 refuses it in writing, with the measured reason (variable footer content) |
| `qd-state` composition silently drops a `data-testid` | 12 sites, one Vitest assertion (`abwab-page.component.spec.ts:477`) and several e2e ids | §4.6 fixes the mechanism (host attribute) before any site is touched; T503/T1102 are the gate |
| The skeleton's `rowTemplate` is a hand-guess and re-introduces the drift §17 forbids | The tree row is flex with a variable column count (§4.7) | §4.7 restricts the claim to the depth-0 non-bulk row that is the *only* shape a cold load can produce, and §7.2 measures skeleton height against loaded-row height |
| Item 17's two stats look inconsistent to a user | Σ `doorsInScopeCount` ≤ total live, because `sectionId` is nullable (§5.7) | T1002 states the definition, refuses the sum identity, and the caveat lands in the abwab README |
| The promotion of `explorer-result-count` breaks the words pages | Five call-sites plus a spec | Dual-selector alias (T1001) — the mechanism already shipped once in this repo — and the TDZ getter idiom preserved |
| Interim state between B1 and B2 is worse than today | A viewport-tall frame around a one-line text loader | §0 orders B1 first precisely so this state never exists on `dev` |

---

## 9. Obligations checklist (all must be true at close)

**B1:**

- [ ] All **14** item-2 sites composed (§5.5), including the two relations-modal sites that use
      their own class names.
- [ ] Both re-styled SCSS blocks **deleted**, not ported (`abwab-relations-modal.component.scss:39-46,99-105`).
- [ ] All **six** error surfaces carry `reserve` (§5.6), and the page-level error no longer
      replaces the toolbar (T402).
- [ ] Both pick-list scrollers carry `qd-scroll-stable`.
- [ ] Every migrated site keeps its original `data-testid`; `abwab-page.component.spec.ts:477`
      passes untouched.
- [ ] T101 and T503 evidence recorded with the delta explained (expected +0/+0).
- [ ] §7.2 acceptance run at T502, numbers recorded, labelled evidence and not a tier.
- [ ] `features/abwab/README.md` amended; T504 grep clean.

**B2:**

- [ ] `.qd-page-frame` shipped with `.qd-explorer-frame` as a working alias; five explorer
      call-sites untouched and green.
- [ ] Abwab full-bleed on both pages; the two §5.1 flex caveats checked **in the browser** and
      recorded.
- [ ] The viewport reservation ships with `box-sizing: border-box` proven present.
- [ ] Sticky navbar on `--qd-z-sticky`; **both** re-based sticky offsets (mushaf + abwab side
      panel) verified flush.
- [ ] `--qd-mushaf-panel-height` re-checked for double subtraction.
- [ ] Navbar dropdowns still escape the new stacking context.
- [ ] `ScrollLockService` exposes lock state; navbar inert + `aria-hidden` paired as `app.ts:14`
      does; `app.nested-layers.spec.ts` run and the inert-inside-inert observation recorded.
- [ ] `qdModalScrollLock` added to `abwab-sections-modal` and `abwab-move-picker` — **named as an
      intentional behavior change**, not slipped in.
- [ ] `qd-result-count` promoted to `shared/ui/`, alias selector in place, TDZ getter idiom
      preserved, spec moved with it.
- [ ] Both stats derived from the snapshot with **no backend call added**; the live-only
      definition and the nullable-section caveat both written down.
- [ ] Arabic counted-noun forms used for both stats; «كل الأبواب» has its own copy.
- [ ] Three `UI_STYLE_SYSTEM.md` entries written (viewport reservation, sticky chrome,
      chrome-inert) plus the `qd-result-count` §17 entry and the §2 frame amendment.
- [ ] `styles/README.md`, `shared/README.md`, `features/words/README.md`,
      `features/abwab/README.md` all amended.
- [ ] §7.2 acceptance run at T1101 across the full matrix.
- [ ] T1102 delta explained (expected +0 files, +2–4 tests); T1103 grep clean **including
      prose**.
- [ ] Root `CLAUDE.md` "Active Spec Kit Feature" updated at B1 start and cleared at B2 close.
      `docs/feature-ux-slice-b/` is **retained** while the UX series is open, per the precedent
      Slice A set at its own close.

---

## 10. Execution note

**Two light branches off `dev`, sequential, B1 then B2** — `ux-slice-b1-states` and
`ux-slice-b2-frame`. One branch for the whole slice is refused for the reason §0 gives: B2 can
break the mushaf reader and the words drawers, B1 cannot break anything outside abwab, and
bisecting a regression in a single 37-task branch would cost more than the second branch does.
Each is one revert point over a coherent, independently shippable change.

Per the root `CLAUDE.md` branching model, both branch off `dev` and PR into `dev`, never `main`.
Neither is a `dev → main` candidate; abwab's routes are still unprotected
(`features/abwab/README.md`).

**Phase/task summary**

| Sub-slice | Phase | Tasks | Count |
|---|---|---|---|
| B1 | 1 Baseline and record | T101–T102 | 2 |
| B1 | 2 Loading skeletons | T201–T203 | 3 |
| B1 | 3 `qd-state` composition | T301–T304 | 4 |
| B1 | 4 Reserved error slots | T401–T403 | 3 |
| B1 | 5 Pick-lists + verification | T501–T504 | 4 |
| | | **B1 total** | **16** |
| B2 | 6 Baseline | T601 | 1 |
| B2 | 7 Page frame (item 1) | T701–T704 | 4 |
| B2 | 8 Viewport reservation (item 4) | T801–T803 | 3 |
| B2 | 9 Sticky navbar (item 6) | T901–T905 | 5 |
| B2 | 10 Stats bar (item 17) | T1001–T1005 | 5 |
| B2 | 11 Verification and doc integrity | T1101–T1103 | 3 |
| | | **B2 total** | **21** |
| | | **Slice B total** | **37** |
