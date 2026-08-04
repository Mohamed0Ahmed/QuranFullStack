# Abwab UX/UI audit — shipped-surface critique, located and classified

> ## ⚠ CLOSED BACKLOG — 2026-08-04. Do not read this as current behavior.
>
> **All 23 items below were implemented**, across the UX slice series A–M that followed this
> audit. This file is retained as the record of *why* those changes were made and which
> decisions the user reversed — not as a work list and **not as a description of how the app
> behaves**.
>
> **Its per-item `file:line` citations are pre-slice and many are now wrong.** Verified
> examples: item 10 describes a `forceExpandedIds` input that no longer exists (the tree takes
> `expandSeedIds`, and seeds rather than forces — the opposite of what item 10's fix recipe
> prescribes); item 15 asserts a `[title]` is "missing at all 11 sites" including a line that
> has carried one since ux-slice-d; and the scope note below calls
> `docs/feature-abwab-templates/plan.md` "the open feature named in the root `CLAUDE.md`" when
> no feature is open and that plan has since been swept to git history. Assume any citation
> here is stale until re-checked.
>
> **The current record is `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md`**,
> plus the backend area READMEs under `Persistence/Reads|Writes/Abwab/`. Read those first.
>
> Kept rather than deleted because the four ⟲ USER REVERSAL entries are the only written
> record of decisions the user changed mid-series, and the appendix pairs each with the text
> it invalidated — context git history holds but does not surface.

**Mode:** read-only. No code changed, no Git action taken. This is a working doc, not a
feature folder; it is not swept by the planning-artifact lifecycle rule and nothing under
`docs/README.md` points at it yet (a pointer line is the user's call, not this audit's).

**Scope walked:** `features/abwab/**` (page, tree, cards, archive view, side panel, six
modals, templates workshop, relations), the app shell (`core/layout/**`, `styles/**`,
`styles.scss`), the shared primitives (`shared/ui/**`), the backend abwab read/write
slices (`api/**/Abwab`, `Persistence/Reads|Writes/Abwab`), and the two governing planning /
contract sources: `docs/feature-abwab-templates/plan.md` (the **open** feature named in the
root `CLAUDE.md`, so a live input, not an archive) and the approved design contracts
`docs/design-preview/abwab-tree-concept.html` / `abwab-relations-concept.html` /
`abwab-templates-concept.html`.

**Three items turned out to be unimplemented contract lines rather than new requests** — the
approved design already specifies them and the implementation dropped them: item 19 (section
tab count badge, `abwab-tree-concept.html:41,207`), item 16's sizing half
(`abwab-relations-concept.html:84`), and item 9's count pill
(`abwab-relations-concept.html:118`). Those are the cheapest items in the report and carry the
least review risk.

## How to read each item

Every item carries five fields:

| Field | Meaning |
|---|---|
| **Where** | `file:line` of the code that produces the behavior |
| **Cause** | why it behaves that way |
| **Precedent** | how the established app already solves this class (`file:line` + governing §). The yardstick. |
| **Class** | `ALIGNMENT` — abwab deviated from an existing rule; the fix is to compose what exists. `NEW PATTERN` — no precedent anywhere; the fix invents a rule and owes a §17 contract entry + doc amendment in the same slice. `MIXED` — half of it is alignment, half is new. |
| **Fix / Size** | the shape, and one of: **token** (CSS/token only) · **component** (component change) · **behavior** (new interaction/state) · **backend** |

Four items **reverse a recorded decision**. They are marked
**⟲ USER REVERSAL — record, do not litigate** with the exact text that must be amended.
Three were named by the user (12, 13, 20); the fourth (22) is one this audit found.

---

# Part 1 — Layout stability

The governing philosophy stated by the user is *nothing ever shifts*. The app already has
this doctrine written down: **UI_STYLE_SYSTEM §17 "Loading/skeleton system" → "No layout
shift (§N3 doctrine): a skeleton must occupy the box its loaded content will occupy — same
padding, gaps, line boxes and item count… Reservations apply only while loading."** Abwab
is the feature that ignores it. Items 2 and 5 are the same doctrine violated twice.

## 1. Abwab content is width-capped while other feature pages are full-bleed

- **Where:** `abwab-page.component.html:2` and `abwab-templates-page.component.html:2` —
  both use bare `<div class="qd-container">`. `.qd-container` is `max-width: 72rem`
  (`styles/_layout.scss:39-44`).
- **Cause:** the words explorers use the *same* container class but add
  `qd-explorer-frame`, which overrides the cap:
  `width: 100%; max-width: none; margin-inline: 0`
  (`styles/_words-explorer-layout.scss:53-63`). Abwab never opted in, so it is the only
  data-dense feature still living inside the 72rem reading measure.
- **Precedent:** `roots-explorer-page.component.html:2`
  (`class="qd-container qd-explorer-frame roots-explorer-frame"`) — and the same pairing in
  the other four explorer pages.
- **Class:** ALIGNMENT.
- **Fix / Size:** **token.** Add the frame class to both abwab pages. Two caveats:
  (a) `.qd-explorer-frame` also sets `display:flex; flex-direction:column; gap:0` and a
  bottom padding sized for "mobile stat bars" — abwab's `__layout` is its own flex row, so
  verify the flex context does not fight it; (b) the class name reads as words-owned. Either
  rename it to a neutral `qd-page-frame` (keeping `qd-explorer-frame` as an alias so five
  call-sites stay valid) or accept the cross-feature reuse. Renaming touches `styles/` and
  therefore needs a §2/§3 note — prefer it, because item 17's stats bar wants the same frame.

## 2. Loading states are text, not skeletons — abwab is the last hold-out

- **Where:** three bespoke text loaders: `abwab-page.component.html:47`,
  `abwab-templates-page.component.html:14`,
  `abwab-template-copy-modal.component.html:75`. Also five bare
  `qd-empty-state`/`qd-error-state` paragraphs: `abwab-page.component.html:49,67,95`,
  `abwab-templates-page.component.html:16,59,65,75`,
  `abwab-template-copy-modal.component.html:79,83`,
  `abwab-relations-modal.component.html:23,36`.
- **Cause:** abwab hand-rolls the backing classes instead of composing the primitives. §17
  is explicit: *"Supersedes ad-hoc `.qd-empty-state` / `.qd-loading-state` /
  `.qd-error-state` usage; those classes remain as the backing layer. Compose, do not
  re-style."* And: *"no bespoke text-only loading states."* The migration of the last two
  ad-hoc text loaders (dashboard-home, mushaf-page-area) is recorded as **complete** in
  §17's status block — abwab shipped after that sweep and re-introduced the pattern.
- **Precedent:** `qd-state` (`shared/ui/state/state.component.html`, §17 `qd-state`) for
  empty/error/loading text; `qd-skeleton-rows` (`shared/ui/skeleton/skeleton-rows.component.ts`
  — `count` + `rowTemplate`, renders skeleton cells *inside the real row grid*) for the
  tree/list load; `qd-panel-skeleton` (`shape="panel"`) for the editor panel. 29 HTML files
  use `qd-skeleton`; zero are in abwab.
- **Class:** ALIGNMENT.
- **Fix / Size:** **component.**
  - Tree/archive/cards load → `qd-skeleton-rows` with the tree row's real grid. The tree row
    is flex, not grid (`abwab-tree.component.scss:6-11`), so either pass an equivalent
    `rowTemplate` (`1.25rem 1.5rem 1fr auto`) or give the row a grid — the primitive's whole
    contract is that loading rows match loaded rows exactly, so a hand-guessed template
    re-introduces the drift §17 forbids.
  - Templates editor panel → `qd-panel-skeleton shape="panel"` (it fills its host, per
    §17/N3).
  - Every empty/error paragraph → `<qd-state variant=… message=…>`, and the two
    *transport* errors (page load, copy-modal doors load) gain the single `actionLabel`
    retry §17 permits — abwab currently offers no recovery from a failed snapshot load at all.
  - The relations modal's `__error`/`__empty` SCSS (`abwab-relations-modal.component.scss:39-46,99-105`)
    is a re-style of what `qd-state` owns and should be deleted, not ported.

## 3. Scrollbar jitter — the app-wide fix already shipped; abwab's inner scrollers are the gap

**This item's premise is partly already solved.** Reporting it straight rather than
building what exists.

- **Where (already fixed):** `styles.scss:30-32` sets `html { scrollbar-gutter: stable }`,
  with a comment (`:23-29`) that names exactly the failure the user describes and why the
  gutter was preferred over padding compensation under RTL. `ScrollLockService`
  (`shared/ui/modal-scroll-lock/scroll-lock.service.ts:13-22`) sets `body{overflow:hidden}`
  and relies on that reservation. So the **document-level** class of jitter — modal open/close,
  page-height crossing the viewport — cannot occur.
- **Where (real remaining gap):** abwab's *inner* scrollers do not reserve their gutter:
  `abwab-relations-modal.component.scss:220-224` (`max-block-size: 11rem; overflow: auto`)
  and `abwab-template-copy-modal.component.scss` pick-list. Typing in either picker's search
  shrinks the list past 11rem, the inner scrollbar vanishes, and the rows shift sideways.
- **Precedent:** `.qd-scroll-stable` (`styles/_utilities.scss:54-56`) exists for precisely
  this, and mushaf composes it: `mushaf-page-view.component.html:1`,
  `study-context-section.component.html:2`. The explorer tables and detail lists set
  `scrollbar-gutter: stable` on their own scrollers (`styles/_explorer-tables.scss:142`,
  `styles/_explorer-detail-lists.scss:72,376,395,439`).
- **Class:** ALIGNMENT (add the utility class to two scrollers).
- **Fix / Size:** **token.** Add `qd-scroll-stable` to both pick-lists. Then, if jitter is
  still observed on `/abwab`, it is **not** a scrollbar problem — it is items 2 and 5
  (content box changing size between states), which is where the fix belongs.

## 4. Content min-height so the page scrolls like the others — and its conflict with item 6

- **Where:** `.qd-shell-viewport { min-height: 100vh }` (`styles/_layout.scss:7-11`), with
  `<qd-footer />` rendered **outside** that div (`app-shell.component.html:2-11`). So every
  page is `100vh + footer`, and a short page yields exactly one footer-height of scroll.
  Abwab's own content is short (`.abwab-page__tree-card { min-height: 20rem }`,
  `abwab-page.component.scss:21-23`), so the page reads as "doesn't scroll" while the words
  explorers, whose tables are tall, read as normal pages.
- **Cause:** no page in the app declares its own minimum content height; the shell's 100vh
  is the only such rule.
- **Precedent:** partial. The *unit* choice is settled — `--qd-mushaf-panel-height:
  calc(100dvh - var(--qd-navbar-block-size))` (`styles/_tokens.scss:77`) establishes `dvh`
  and subtracting the navbar token rather than hard-coding. A per-page content minimum has
  no precedent.
- **Class:** MIXED — `dvh`-minus-chrome arithmetic is established; "every page's content
  reserves a viewport" is new.
- **Fix / Size:** **token.** `min-block-size: calc(100dvh - var(--qd-navbar-block-size) - <footer>)`
  on the page's content region. The footer has no size token — add one
  (`--qd-footer-block-size`) rather than a magic number, or measure with the existing
  `.qd-footer` padding tokens. If this becomes a rule for all pages it needs a §2/§17 note;
  if it stays abwab-local, say so in the abwab README.
- **⚠ Conflict with item 6, which the user must resolve:** item 4 asks that the navbar
  *scroll* like other pages; item 6 asks that it be *sticky*. Sticky means it never scrolls
  away. **Recommendation:** take item 6 (sticky navbar) and read item 4 as "the content
  region always reserves a full viewport, so the footer sits below the fold consistently on
  every page." That satisfies the stated goal — a uniform page rhythm — without the
  contradiction. Flagged rather than silently reconciled.

## 5. Error surfaces must occupy reserved space

- **Where:** every abwab error is an `@if`-mounted node, so it inserts and removes layout:
  `abwab-page.component.html:49` (page-level, and it replaces the entire toolbar+tree
  branch), `abwab-relations-modal.component.html:22-26`,
  `abwab-template-copy-modal.component.html:17-21`,
  `abwab-sections-modal.component.html:6-10`,
  `abwab-templates-page.component.html:16,65-68`, plus the door modal's inline error via
  `abwab-door-fields-form`. Also `abwab-page.component.html:142-195`: the two archive-confirm
  cards mount inside the sticky `<aside>` and push it.
- **Cause:** the no-layout-shift doctrine was applied to *skeletons* (§17) and to the detail
  overlay's count box, but never generalized to error surfaces, and abwab is where errors
  are most frequent (every 409 is a normal outcome here).
- **Precedent — strong, and it is a *reservation* precedent, not a skeleton one:**
  - `explorer-result-count.component.html` — all three states (error / loading / loaded)
    render *the same one-line box*, with the header comment recording exactly why:
    *"the stat used to unmount on a list error and collapse the toolbar, and its loading bar
    sat ~1.4px taller than the loaded line."* That is this item, already solved once.
  - `detail-modal-shell.component.html:37-44` + §17: the count box is *"always rendered so
    its reserved box never appears/disappears: only the text fades in"* —
    `min-inline-size: 6rem` at `detail-modal-shell.component.scss:62-63`.
- **Class:** ALIGNMENT of the *principle*; NEW PATTERN for the *artifact*, since no shared
  reserved-error-slot component exists (`qd-state` mounts and unmounts like everything else).
- **Fix / Size:** **component.** Add a reserved-slot variant to the error family — the
  cheapest honest shape is a `qd-state`-adjacent `qd-error-slot` (or a `reserve` input on
  `qd-state`) that always renders a `min-block-size`-fixed box and fades text in, exactly the
  count-box rule. Owes a §17 entry under the `qd-state` contract. Apply to all six abwab
  sites. Note the page-level error currently *replaces* the whole view — reserving space there
  means deciding whether a load failure keeps the toolbar visible; recommend yes, since
  `AbwabSnapshotFacade` keeps the previous snapshot on refresh failure.

## 6. Sticky navbar

- **Where:** `.qd-navbar` (`styles/_layout.scss:26-37`) is a plain flex child of
  `.qd-shell-viewport`; nothing is sticky.
- **Precedent:** `position: sticky` is used twice in the app —
  `mushaf-reader-page.component.scss:53` (with `--qd-mushaf-sticky-top`,
  `styles/_tokens.scss:107`) and, ironically, abwab's own side panel
  (`abwab-page.component.scss:31-33`). The pattern is established; the *shell* has never used it.
- **Class:** MIXED — sticky is an established technique, sticky app chrome is new.
- **Fix / Size:** **token**, with three constraints that must be honored in the same change:
  1. **z-index budget.** `.qd-modal-backdrop` is `z-index: 50` (`styles/_components.scss:543-546`)
     and abwab's context menu uses `--qd-z-menu-backdrop`/`--qd-z-menu` (49/50). A sticky navbar
     must sit **below 49**, or modals and row menus paint under the chrome. **Done — Slice A**:
     the `--qd-z-*` layer scale now exists in `_tokens.scss` (`--qd-z-mobile-nav` holds the
     navbar's dropdown/mobile-menu rungs below every modal layer), and the menu's z-index moved
     with it into `shared/ui/context-menu/context-menu.component.ts` when the shared primitive
     was extracted (the old bare literals at `abwab-page.component.scss:88,94` and
     `abwab-templates-page.component.scss:146,152` no longer exist). Slice B still owns
     composing the sticky navbar itself against `--qd-z-sticky`.
  2. **`--qd-mushaf-panel-height`** (`styles/_tokens.scss:77`) subtracts the navbar from
     `100dvh`. Sticky keeps the navbar in flow, so the arithmetic survives — but say so, and
     re-check the mushaf reader visually, because sticky + an inner 100dvh-minus-navbar panel
     is exactly where a double-subtraction bug hides.
  3. The navbar's dropdowns (`.dropdown-menu`) become children of a stacking context; verify
     they still escape the navbar's bounds.

---

# Part 2 — Modals as a system

Abwab has six modals, none of which composes the app's dialog shell. The shell's rules are
already written: **§17 `qd-detail-modal-shell`**.

## 7. Every modal needs fixed width AND height with internal scroll

- **Where:** all six abwab modals apply `.qd-modal` and add no geometry:
  `abwab-door-modal.component.html:4`, `abwab-relations-modal.component.html:4`,
  `abwab-move-picker`, `abwab-sections-modal.component.html:3`,
  `abwab-template-copy-modal.component.html:4`, `abwab-template-node-modal`.
  `.qd-modal` is `width: min(100%, 36rem)` with **no block-size and no scroller**
  (`styles/_components.scss:554-564`), inside a flex-centered
  `.qd-modal-backdrop` (`:543-552`).
- **Cause:** with no `block-size` and no internal scroller, a modal grows with its content;
  once it exceeds the viewport a flex-centered child overflows *both* ends and the overflow is
  unreachable, because the body is scroll-locked. The relations modal is the one that hits it
  (four relation groups + type segment + direction row + an 11rem picker + footer).
- **Precedent — exact, and it is a *contract*:** §17 `qd-detail-modal-shell` → *"Geometry
  (fixed, both axes): `inline-size: min(100%, 46rem)` and `block-size: min(92dvh, 44rem)` — a
  fixed block-size, never `max-block-size`… `__body` is the only scroller (`flex: 1;
  min-block-size: 0; overflow-y: auto`); `__header` is `flex-shrink: 0`… Phone goes
  near-fullscreen… Shallow states therefore render a tall dialog with empty space — the
  accepted trade for zero resize."* Implemented at
  `detail-modal-shell.component.scss:12-15, 91-93, 140`.
- **Class:** MIXED. The *rule* exists and is authoritative; what does not exist is that rule
  applied to `.qd-modal`, the class every non-overlay dialog in the app uses. So: alignment in
  substance, and a **new §17 entry for `.qd-modal`** as the artifact.
- **Fix / Size:** **component.** Give `.qd-modal` the same three-part shape as the shell —
  fixed `block-size: min(92dvh, Nrem)`, a `flex-shrink: 0` header slot, and a single
  `flex: 1; min-block-size: 0; overflow-y: auto` body slot — plus the phone override. Then
  restructure each abwab modal's markup into head/body/foot so the body is the scroller
  (today the footers, e.g. `abwab-relations-modal.component.html:164-177`, would scroll away
  with the content). Add the `.qd-modal` geometry entry to §17 in the same change.
  **Do not** convert abwab's modals to `qd-detail-modal-shell` itself — that component owns
  overlay history, a Back stack, and restore semantics abwab's dialogs have no use for
  (except item 11's, below).

### 7-adjacent (found while sweeping): the sections modal is not a dialog at all

`abwab-sections-modal.component.html:3` has **no** `role="dialog"`, no `aria-modal`, no
`aria-labelledby`, no `(keydown.escape)`, and no `qdModalScrollLock` — while the other five
abwab modals have all five. This is a plain a11y defect, not a design preference. It belongs
in this slice. **ALIGNMENT · component.**

## 8. Autofocus the first input on open — and the larger gap, no focus trap anywhere

- **Where:** no abwab modal focuses anything on open. The door modal's open-effect
  (`abwab-door-modal.component.ts:99-104`) resets error + discard state and stops there. Same
  for the copy modal (`abwab-template-copy-modal.component.ts:136-145`).
- **Bigger gap found by the sweep:** **no abwab modal uses `cdkTrapFocus`.** The seven
  dialogs that do are all in words/shared: `detail-modal-shell.component.html:11-12`
  (`cdkTrapFocus cdkTrapFocusAutoCapture`), `word-drilldown-modal`, and the four
  `*-details-panel` components. Without the trap, Tab walks out of an abwab modal into the
  inert page behind it. Autofocus falls out of `cdkTrapFocusAutoCapture` for free.
- **Precedent:** `cdkTrapFocusAutoCapture` (`detail-modal-shell.component.html:11-12`) for
  the declarative path; `surah-jump-picker.component.ts:207`
  (`this.searchInputRef()?.nativeElement.focus()` on open) for the explicit path when a
  *specific* control must win.
- **Class:** ALIGNMENT.
- **Fix / Size:** **component.** Add `cdkTrapFocus cdkTrapFocusAutoCapture` to all six abwab
  dialogs (this is also the right place to fix 7-adjacent). Where auto-capture would land on
  the wrong control — the relations and copy modals open on a *list*, not a field — add the
  explicit `viewChild().focus()` on the search input per the surah-jump-picker precedent. The
  add/edit door and template-node modals want the name field, which
  `abwab-door-fields-form` owns, so the form needs a `focusFirstField()` method rather than
  the shells reaching into its DOM.

## 9. The relations modal is badly laid out

Four distinct causes, and the biggest one is that this modal hand-rolls three §17 contracts.

- **9a — names wrap vertically.** `abwab-relations-modal.component.scss:264-267`:
  `.abwab-relations-modal__pick-name { flex: 1; font-size: .8125rem }` — no
  `min-inline-size: 0`, no `overflow: hidden`, no `text-overflow`, no `white-space: nowrap`.
  Inside a 36rem modal whose rows also carry a depth indent
  (`padding-inline-start: calc(var(--abwab-relations-depth) * var(--qd-space-5) + …)`, `:231`),
  a long Arabic door name wraps to several lines and the row grows. Same defect on
  `.abwab-relations-modal__target` chips (`:91-97`, inside a `flex-wrap` row, `:80-85`).
  **Precedent:** `detail-modal-shell.component.scss:28-35` — `flex: 1; min-inline-size: 0;
  overflow: hidden; text-overflow: ellipsis` is the app's title rule; abwab's own tree row
  already does it correctly (`abwab-tree.component.scss:70-75`). **ALIGNMENT · token.**
- **9b — checkbox far from its label and unnamed.** `abwab-relations-modal.component.html:130-152`
  puts a `gap: var(--qd-space-2)` between chevron→checkbox→name, and the leaf chevron is
  `visibility: hidden` (`:260-262`) so it still occupies 1rem — a leaf row therefore shows
  ~2×space-2 + 1rem of dead space before the checkbox. Worse, **this checkbox has no
  accessible name** (`:145-151`: no `<label>`, no `aria-label`) while the copy modal's
  equivalent does (`abwab-template-copy-modal.component.html:65`). **See item 16** for the
  shared fix. **ALIGNMENT · component.**
- **9c — three §17 contracts re-styled by hand.** The strongest finding in this item, and the
  one place where §17 and the approved concept **pull against each other** — flagged, not
  silently resolved, because the user bounded item 9 with *"redesign within the approved
  content."*
  - `.abwab-relations-modal__count` (`scss:15-33`) is a hand-rolled count pill.
    `qd-chip` has an optional trailing `count` and ships `.qd-chip__count` (§17 `qd-chip`).
    The concept specifies the pill (`abwab-relations-concept.html:118`,
    `<span class="cnt">3</span>`), so composing `qd-chip` here is pure alignment — the visual
    survives.
  - `.abwab-relations-modal__types` (`html:64-78`, `scss:120-154`) is a hand-rolled
    segmented control with `aria-pressed` and **no keyboard model**. §17: `qd-tabs` is
    *"the one tab-strip implementation app-wide"* with `role="tablist"`, roving tabindex,
    RTL-aware Arrow/Home/End, and the §16.1 selected treatment.
    **The conflict:** the implementation is a *faithful* copy of the concept
    (`abwab-relations-concept.html:57-62` `.type-seg` — `section-bg` track, and `.active` =
    `surface` + `font-weight:600`), and that active treatment is **not** §16.1's
    (`--qd-selected-bg` + `--qd-accent-text` + `--qd-border-accent`), which is what `qd-tabs`
    would impose. So composing `qd-tabs` gains the keyboard model and costs the concept's
    exact visual. **Recommend composing it anyway** — §16.1 is the color doctrine and outranks
    a mockup's ad-hoc active state, and the modal already contradicts itself (see 9e). Record
    the deviation from the concept line in the same change.
  - The direction **pill** (`html:83-102`, concept `:69-72`) is a binary toggle, not a tab
    strip. **Leave it as a pill** — but fix its inconsistency (9e).
  - `__error` / `__empty` (`scss:39-46, 99-105`) re-style `qd-state` — see item 2.
  **ALIGNMENT · component**, and this is what makes item 9 a component change rather than a
  CSS pass.
- **9e — the modal uses two different "selected" treatments, three elements apart.** The type
  segment's active state is `surface` + bold (`scss:149-154`); the direction pill's active
  state is `--qd-selected-bg` + `--qd-accent-text` + bold (`scss:198-202`) — which *is* §16.1.
  One modal, one selection concept, two visual languages. This is the concrete core of the
  user's "consistent formatting" complaint and it resolves itself once 9c composes `qd-tabs`.
  **ALIGNMENT · token.**
- **9d — groups and chips need breathing room.** `__group { margin-block-end: space-3 }`
  with `__chips { gap: space-1 }` (`scss:48-50, 80-85`) is the tightest gap in the file while
  the group headings are `font-size: .75rem`. **DESIGN.md** calm-for-long-focus and §16.2's
  density work are the governing register. **ALIGNMENT · token.** Once 9c lands, chip spacing
  comes from `qd-chip` and most of this disappears.

## 10. Clicking a door name inside the relations modal reveals it in the tree

- **Where:** relation names render as `qd-chip` content
  (`abwab-relations-modal.component.html:46-52`) — informational, no click path. §17 records
  *why* they cannot simply become buttons: *"nesting an interactive remove control inside
  another `button`/`a` is invalid HTML, so a removable chip is informational, not itself
  clickable."* So the door name needs its own nested control beside the remove button, not a
  clickable chip.
- **What already exists for the reveal half:** `AbwabTreeComponent` has `forceExpandedIds`
  (`abwab-tree.component.ts:51-52`, unioned with manual toggles at `:67-69`) — search
  auto-expand already drives it, so "expand the path to a door" is a solved input. The page
  computes `forceExpandedIds()` (`abwab-page.component.html:104`). Selection is
  URL-driven (`door=<id>`, abwab README "URL contract"), so "select that door" is
  `router.navigate`. Scroll: `element.scrollIntoView({ block: 'nearest' })` —
  `surah-jump-picker.component.ts:277`.
- **What has no precedent:** a **timed soft highlight**. Grep finds no flash/pulse/timed
  highlight anywhere; the only "flash" in the repo is a token comment explaining why the mushaf
  word selection was deliberately *strengthened* out of flash territory —
  `styles/_tokens.scss:94`: *"16% landed only ΔL 0.0157 below hover in light — the selected
  word was invisible among the words around it, **which is what made it read as a brief flash
  rather than a persistent mark**."* Read that as a warning about the *contrast budget*: a
  reveal highlight must be clearly above the hover rung or it will not register at all. The
  measured ladder is right there (`:89-101`) — use it rather than picking a mix percentage by
  eye.
- **Class:** MIXED — expand/select/scroll are all ALIGNMENT; the 3s highlight is a
  **NEW PATTERN**.
- **Fix / Size:** **behavior.** Close the modal → `door=<id>` + add the ancestor chain to
  `forceExpandedIds` → `scrollIntoView({block:'nearest'})` → apply a
  `.abwab-tree__row--revealed` class for ~3s. For the highlight: derive it from
  `--qd-selected-bg`/`--qd-border-accent` (§16.1, allowed-green list §16.3 — **no new hue**),
  make it a transition that decays, and make it **static under
  `prefers-reduced-motion`** (§17's blanket rule for skeletons; §15 F "Motion"). Owes a §17
  or §16.2 note as the app's reveal-highlight rule, because items 13 and 18 will want it too.
  Ancestor-chain derivation is free: `byId` + `parentId` walk, the same walk
  `abwab-cards` uses for breadcrumbs.

## 11. A modal dismissed by navigation is restorable, plus an explicit discard

- **Where:** abwab overlay state is page-scoped signals in
  `state/abwab-page-overlays.controller.ts`, provided by `AbwabPageComponent`, not root — and
  the abwab README records why: *"Root scope would outlive `/abwab`, and the page renders
  every dialog outside its loading/error guard, so a left-open modal would paint again on
  re-entry before any data loads."* So today, navigating away destroys the modal with no trace.
- **Precedent — this is exactly the words feature's shipped pattern, and it is URL-based, not
  service-memory-based:**
  - `DetailOverlayHistoryService` (`core/navigation/detail-overlay/detail-overlay-history.service.ts`):
    `isOpen` / **`isRetainedClosed`** (`:46-47`); close *retains the stack in the URL in a
    closed, restorable state* (`:125-131`); `restore()` reopens it **as a history push so
    Back returns to the closed state** (`:134-140`). State is re-parsed on every
    `NavigationEnd`, so browser Back/Forward work.
  - `qd-detail-modal-shell` renders, in its `@else` (closed) branch, a persistent restore
    button (`detail-modal-shell.component.html:70-82`) and moves focus to it when the dialog
    closes (`detail-modal-shell.component.ts:91-95`) — *"so keyboard users can immediately
    reopen it."*
  - The host composition point is sanctioned and documented at `app.ts:6-17`
    (`<qd-entity-detail-overlay-host />` beside the shell, shell goes `inert` while open).
  - §17 lists *"the closed-state restore control"* as part of the shell's purpose.
- **Class:** ALIGNMENT — the pattern exists end to end. What is new is a *second* consumer of
  it, which is a real design decision (see below).
- **Fix / Size:** **behavior**, and the largest of the frontend items. Two honest routes:
  - **(a) Encode abwab's overlay state in the URL** (a `modal=` key joining the six existing
    abwab query keys), give it a fail-closed parse per the URL contract, render a restore
    control in the closed state, and add an X that clears the key entirely. This follows the
    words pattern faithfully and gets Back/Forward for free. Cost: the abwab URL contract
    grows a seventh key with its own scope-invalidation rule, and the README's "no URL state"
    statement for `/abwab/templates` must be revisited.
  - **(b) Generalize `DetailOverlayHistoryService`** to carry non-entity frames. Cheaper for
    abwab, but it is core navigation shared with words — a much larger review surface.
  **Recommend (a).** Note in the same change that this **supersedes** the README gotcha quoted
  above: the reason overlay state was page-scoped was "a left-open modal would paint again on
  re-entry before any data loads" — a restorable-but-closed state is precisely the shape that
  makes re-entry safe, so the invariant is being *refined*, not violated. Say that in the README.

---

# Part 3 — Tree and page behaviors

## 12. ⟲ USER REVERSAL — reorder input: Enter commits, blur cancels

- **Where:** `abwab-tree.component.html:50` — `(blur)="commitOrderEdit(node.id, $event.target)"`,
  plus `abwab-tree.component.ts:196-207` (`commitOrderEdit`) and `:187-194` (`onOrderKeydown`,
  where Enter commits and Escape reverts).
- **Reversal — record, do not litigate.** The commit-on-blur behavior is documented in
  **`features/abwab/README.md`** (render-chain, `components/abwab-tree/`): *"Inline reorder
  editing (click the order number → input, Enter commits, Escape reverts)"* — the README does
  not name blur, but the code commits on it, and `abwab-tree.component.spec.ts` and
  `e2e/abwab-operations.e2e.ts` exercise inline reorder. New rule: **Enter commits; blur and
  Escape both cancel.**
- **Precedent:** the workshop's two inline authoring rows already commit on Enter only, with
  no submit button, and the README calls that *"the page's own idiom"*
  («اسم القالب… (Enter)», «إضافة عنصر… (Enter)»). The `abwab-sections-modal` rename is a
  `<form>` submit. So Enter-only is already this feature's established grammar — the reversal
  **aligns** the reorder editor to it. Elsewhere in the app, `word-type-filter` /
  `surah-jump-picker` close-on-blur without committing.
- **Class:** ALIGNMENT (to abwab's own recorded idiom).
- **Fix / Size:** **component** (one line + a guard + spec updates). `(blur)` → a
  `cancelOrderEdit()` that only clears `editingId`. Ordering is already safe: Enter's handler
  sets `editingId` to `null` *before* the input unmounts, and `commitOrderEdit` guards on
  `editingId() !== id`, so the subsequent blur is a no-op. Update the README line to name
  blur explicitly (silence there is what let the code drift), and update
  `abwab-tree.component.spec.ts` + `abwab-operations.e2e.ts`.

## 13. ⟲ USER REVERSAL — the علاقات flag: always visible, dimmed at zero, and clickable

- **Where:** `abwab-tree.component.html:65-69` — `@if (node.relationCount > 0)`; styled at
  `abwab-tree.component.scss:83-98`.
- **Reversal — record, do not litigate.** Two recorded decisions are being reversed at once:
  1. Render-only-when->0. The abwab README states the flag is `relationCount > 0` **only**,
     and derives the archive-view/cards absence from it.
  2. **Non-control.** `abwab-tree.component.scss:89-90` — *"A chip, not a control: no tab
     stop and no click handler, so the row's roving tabindex is untouched (plan §7 T603)"* —
     and the README's *"Zero dead controls"* gotcha calls it *"the one deliberate
     non-control."* Both statements must be amended in the same change.
- **Precedent:** the dimmed-at-zero treatment has one — `explorer-result-count.component.html`
  renders an `--unavailable` variant rather than unmounting (item 5's precedent). For the
  clickable-flag half, the row's own `.abwab-tree__act` buttons
  (`abwab-tree.component.html:73-92`) are the pattern: real `<button>`, Arabic `aria-label`,
  and **`[attr.tabindex]="-1"`** so the roving-tabindex invariant survives.
- **Class:** ALIGNMENT (compose the row-action pattern that already exists two elements away).
- **Fix / Size:** **component.** Drop the `@if`; add an `--empty` modifier
  (muted text + `--qd-border` hairline, no accent tint — the accent means "has relations");
  turn the chip into a `<button>` with `tabindex="-1"`, an Arabic `aria-label` naming the
  door and the count, and a new `relationsRequested` output the page wires to
  `overlays.openRelations()`. Three doc edits owed: the README's flag line, the README's
  "Zero dead controls" line, and the SCSS comment. **Do not** extend the always-visible flag
  to the archive view or cards — the README's derivation there (an archived door's visible
  relation count is *always* 0, so the flag would be permanently dimmed and its click would
  open an always-empty modal) still holds and is not part of this reversal.
- **"Consistent formatting" (the other half of item 13):** the row currently mixes three
  unstyled-together metadata spans — `.abwab-tree__order` (bordered pill, `:48-59`),
  `.abwab-tree__count` (bare muted text, `:77-81`), `.abwab-tree__flag` (tinted pill,
  `:91-98`). Precedent: `qd-chip` + `.qd-chip__count` (§17) is the app's one chip/count
  vocabulary; `qd-detail-modal-shell`'s kind chip shows the informational variant
  (hairline + muted, no fill). Unify the three onto that vocabulary. **ALIGNMENT · component.**

## 14. Per-door badges: direct children, total descendants, max internal depth

- **Where:** only direct children ship today — `abwab-tree.component.html:62-64` renders
  `node.liveChildCount`, sourced from `AbwabTreeDoorDto.directChildCount`
  (`core/api/generated/models/abwab-tree-door-dto.ts`), computed live-only at
  `EfAbwabTreeReader.cs:33-36`.
- **No backend need — confirmed.** The tree DTO ships every door with its `parentId`, and
  `AbwabTreeBuilder` already materializes full `children` arrays and `depth`
  (`models/abwab.models.ts:109-130`, `state/abwab-tree.builder.ts`). Total live descendants
  and max relative depth are both pure functions of that tree. Precedent for deriving counts
  client-side rather than asking the backend:
  `AbwabWriteController#bulkArchiveConfirmMessage` already walks selected subtrees into an id
  set (abwab README, "Bulk-archive's confirm count is a union").
- **Precedent for the *presentation*:** `.qd-tabs__count` / `.qd-chip__count` (§17) and
  `qd-detail-modal-shell`'s reserved count box.
- **⚠ The approved design contract specifies only the direct-children count** —
  `abwab-tree-concept.html:107` (`.count{font-size:11px; color:var(--qd-muted); flex:none;}`)
  and `:439` (`${hasKids?d.kids.length:''}`), which is exactly what shipped. **Total
  descendants and max internal depth are beyond the concept**, so this item adds two badges
  the contract never asked for. That is the user's call to make and this audit records it as a
  contract *extension*, not a missed line — the opposite of item 19's situation. Because the
  concept gives no guidance on their visual, the row-priority decision below is the whole
  design.
- **Class:** ALIGNMENT for the derivation and the count vocabulary; **contract extension** for
  the two new badges.
- **Fix / Size:** **component.** Add two pure derivations to `abwab-tree.builder.ts`
  (`liveDescendantCount`, `maxRelativeDepth`), both memoized on the node — the builder is pure
  and specced (`abwab-tree.builder.spec.ts`), which is where they belong rather than in the
  component. Keep the semantics identical to the backend's: **live-only**
  (`EfAbwabTreeReader.cs:30-32` states that rule for both existing counts) — an archived
  subtree must not inflate a badge.
- **⚠ Row width budget — cite this, don't skip it.** The row already carries: checkbox
  (bulk) · chevron · order pill · name · count · flags · two hover actions. Three badges
  where there was one is a real risk at the current 36rem-ish column, and §17's
  `qd-detail-modal-shell` "Header priority" section is the app's precedent for *how to reason*
  about it: *"The row cannot hold every element at phone widths… Adding a new header element
  means re-checking this budget."* Do the same here and write the priority down:
  **name (only shrinkable, ellipsis) > order pill > actions > badges > flag**, with badges
  collapsing to a single combined chip (`3 / 12 / د3`) or dropping below
  `$qd-bp-tablet-max`. State the chosen order in the abwab README.
- **Definition to pin:** "max internal depth" per the user's example (child + grandchild +
  great-grandchild = 3) is the depth **relative to the door**, counting the deepest
  descendant, not `node.depth`. Write that in the label's Arabic copy so it cannot be misread.

## 15. Door names everywhere: fixed width + ellipsis + hover tooltip

Two halves with different classifications — do not conflate them.

- **The tooltip half — ALIGNMENT, and it is missing at all 11 sites.** No abwab name render
  has a `[title]`: `abwab-tree.component.html:61`, `abwab-cards.component.html:21,47`,
  `abwab-archive-view.component.html:27`, `abwab-side-panel.component.html:6`,
  `abwab-move-picker.component.html:24,48`, `abwab-relations-modal.component.html:31,152`,
  `abwab-template-copy-modal.component.html:69`, `abwab-template-tree.component.html:49`,
  `abwab-sections-modal.component.html:32`,
  `abwab-templates-page.component.html:29,82`. **Precedent:**
  `word-type-filter.component.html:57` — `<span class="…__child-label" [title]="child.label.ar">`,
  the app's ellipsis+tooltip pairing; also `words-hub-page.component.html:23`. **Fix:**
  `[title]="name"` wherever the text can be truncated. **token.**
- **The ellipsis half — ALIGNMENT, three sites missing it.** Present:
  `abwab-tree.component.scss:70-75`, `abwab-archive-view…scss:47-51`,
  `abwab-template-copy-modal…scss:98-103`, `abwab-template-tree…scss:78-82`. **Missing:**
  `.abwab-relations-modal__pick-name` (`scss:264-267` — item 9a),
  `.abwab-sections-modal__name` (`scss:31`), `.abwab-templates-page__item-name`
  (`scss:49`). Also check `.abwab-side-panel__active-name` and the cards' name/crumb.
- **The *fixed-width* half — NEW PATTERN, and the app's rule is currently the opposite.**
  Every precedent in the repo is *flexible with ellipsis*: `flex: 1; min-inline-size: 0;
  overflow: hidden; text-overflow: ellipsis` (`detail-modal-shell.component.scss:28-35`, §17
  *"The title is the only shrinkable item"*), and abwab's tree name already does exactly that.
  A **fixed** name column is a different rule: it makes the name width independent of depth
  indent and sibling badges, which is what actually stops the jitter the user is describing —
  but it also means a shallow row wastes width a deep row needs. **Recommendation:** implement
  it as a *reserved minimum* (`flex: 1; min-inline-size: <token>`) rather than a hard
  `inline-size`, which delivers the stability without the waste, and if the user wants a truly
  hard column, do it as a grid column on the row (which item 2's `qd-skeleton-rows`
  `rowTemplate` wants anyway — the two fixes reinforce each other). Either way this owes a §17
  entry: "truncatable entity names — reserved width + ellipsis + `[title]`". **token**, low risk.

## 16. Checkboxes aligned beside their labels

- **Where:** the app has exactly **four** checkboxes and all four are in abwab:
  `abwab-tree.component.html:19-27` (bulk mode), `abwab-cards.component.html:38`,
  `abwab-relations-modal.component.html:145-151`,
  `abwab-template-copy-modal.component.html:62-68`.
- **Cause:** there is **no checkbox styling anywhere** — grep finds no `checkbox` or
  `accent-color` rule in `styles/_forms.scss`, `_components.scss`, or `_utilities.scss`. Each
  call-site improvises: the tree sets `flex: none; accent-color: var(--qd-accent)`
  (`abwab-tree.component.scss:100-103`), the two pickers set nothing at all, so those
  checkboxes render at UA default size with UA default margins inside a flex row whose gap was
  tuned for text. And the accessible-name treatment diverges: the copy modal's has
  `[attr.aria-label]="row.node.name"`, the relations modal's has nothing.
- **Precedent:** **none in code for a checkbox** — but the **approved contract sizes it**:
  `abwab-relations-concept.html:84` — `.pick-row input{width:15px; height:15px;
  accent-color:var(--qd-primary);}`, alongside `.pick-row{…gap:8px…}` (`:82`). The
  implementation kept the row gap and dropped the sizing, which is precisely why the checkbox
  reads as misaligned. For the *shape* of the fix, the analogue is how §17 treats every other
  repeated control — one contract, composed, never re-styled per call-site — and
  `styles/_forms.scss` is where the app already centralizes field styling.
- **Class:** **NEW PATTERN** for the primitive; ALIGNMENT to the concept for the sizing values
  it should carry (~1rem box + `accent-color`, tokenized rather than `15px`).
- **Fix / Size:** **component.** Add a `.qd-checkbox` (input) + `.qd-check-row`
  (label/row wrapper) pair to `styles/_forms.scss`: fixed `1rem` box, `flex: none`,
  `accent-color: var(--qd-accent)`, `margin: 0`, `--qd-space-2` to the label, and a
  `:focus-visible` ring. Every checkbox must get an accessible name — either a real
  `<label for>` or `aria-label` — and the four call-sites compose it. Owes a §17 entry (a
  form-control family is a genuine gap in that section) and a `styles/README.md` note. **Build
  this before items 9 and 20 touch the pickers**, or those slices hand-roll it again.

## 17. A stats bar at the top of the page

- **Where:** nothing exists. The page header is title + subtitle + four buttons
  (`abwab-page.component.html:3-42`).
- **No backend need — confirmed, with one semantic caveat.**
  - *Total doors*: derivable from the snapshot — `AbwabTreeSnapshotVm.byId`
    (`models/abwab.models.ts:133-141`) holds every door, live and archived; live-only totals
    come from walking `liveRoots`. Decide and state whether "total" means live-only (consistent
    with every other count in this feature — `EfAbwabTreeReader.cs:30-32`) or live+archived.
    Recommend live-only, with the archive count as its own stat if wanted.
  - *Doors in the currently open section*: **already on the wire** —
    `AbwabTreeSectionDto.doorsInScopeCount`
    (`core/api/generated/models/abwab-tree-section-dto.ts`), computed at
    `EfAbwabTreeReader.cs:37-40` as **every live door with that `sectionId`, at any depth**.
    It is currently **rendered nowhere in the frontend**. Note the semantic: it is *all* doors
    in the section, which is the right number for item 17 and the *wrong* number for item 19
    (which wants roots only) — see there.
- **Precedent:** `qd-explorer-result-count` + `.uw-toolbar-recess__stat`
  (`roots-explorer-page.component.html:29-41`; slot defined at
  `styles/_words-explorer-layout.scss:27`) is the app's headline-stat pattern, and it already
  encodes item 5's reservation rule (all three states, one line box).
- **Class:** ALIGNMENT.
- **Fix / Size:** **component.** Compose `qd-explorer-result-count` (or generalize its name if
  "explorer" grates — it is already used by five pages, so renaming is a `styles/` +
  five-call-site change to weigh) in a toolbar-recess slot above the abwab toolbar. The
  section stat must recompute from `activeSectionId()`, and «كل الأبواب» needs its own copy
  (total, not a section count). Use the Arabic counted-noun helper the abwab README mandates —
  *"Do not interpolate a bare count into new copy — «سيتم أرشفة 1 بابًا» is wrong Arabic"* —
  singular/dual/3–10/11+ forms.

---

# Part 4 — Sections

## 18. Sections are reorderable

**The user's premise ("likely needs a backend order column + migration") is wrong in the
user's favor: the column already exists and no migration is needed.**

- **What exists:**
  - `AbwabSection.OrderValue` (`domain/QuranDashboard.Domain/Abwab/AbwabSection.cs`).
  - Set on create: `EfAbwabSectionsWriter.cs:17` (`OrderValue = nextOrder`).
  - Read ordered by it: `EfAbwabTreeReader.cs:12-15`
    (`.OrderBy(s => s.OrderValue).ThenBy(s => s.Id)`).
  - Shipped on the wire: `AbwabTreeSectionDto.orderValue`
    (`core/api/generated/models/abwab-tree-section-dto.ts`).
  - Read by the frontend model: `AbwabTreeSnapshotVm.sections` is
    `readonly AbwabTreeSectionDto[]` (`models/abwab.models.ts:134`), and the toolbar renders
    `sections()` in wire order (`abwab-toolbar.component.html:15-23`).
- **The actual gaps, all three of them:**
  1. **No reorder endpoint.** `AbwabSectionsController.cs` has `POST` (`:15`),
     `PUT {id}` rename (`:35`), `DELETE {id}` (`:60`) — no reorder. Compare
     `AbwabDoorsController`, which has one, and `AbwabReorderScope`
     (`Application.Abstractions/Abwab/AbwabReorderScope.cs`) which is doors-only
     (`Section`/`Global`).
  2. **No writer method.** `IAbwabSectionsWriter` / `EfAbwabSectionsWriter` expose
     create/rename/delete only.
  3. **No UI.** `abwab-sections-modal` is list/add/rename/delete-empty (abwab README).
- **Precedent — strong, and it must be followed exactly:** the doors reorder path, end to
  end. Backend: the doors writer resequences its scope to `1..N` on every write. Frontend:
  `abwab-tree.component.ts:182-207` (inline number editor) + `AbwabWriteController`'s
  409 policy. **And the invariant that governs the design:** the abwab README's
  *"Refresh-after-write is an invariant, not an optimization — every write resequences its
  scope to 1..N, which bumps every sibling's `xmin` too."* A section reorder resequences
  **every section**, so every section's version token goes stale in one write — exactly the
  doors case, and exactly why the write controller refetches the whole snapshot. The
  `abwab-sections-modal` already *"always reads the section's row from the live `sections`
  input at submit time, never a value captured when edit mode opened"* (README) — that is the
  rebinding this reorder needs, already in place.
- **Class:** ALIGNMENT (the doors reorder is the template for all three layers).
- **Fix / Size:** **backend** + component. `PUT api/abwab/sections/{id}/order` taking
  `{ position, version }`, a writer that resequences `1..N` among live sections, an
  `AbwabInvalidPositionException` reuse, the frontend api method, a controller command
  routed through `AbwabWriteController` so the 409 policy and the snapshot refetch are shared,
  and the number-click editor in the sections modal reusing the tree's editor grammar
  (**and item 12's new rule: Enter commits, blur cancels**). Required in the same change:
  a `SmokeRouteCatalog` entry — `SmokeCoverageParityTests` fails without it
  (Backend CLAUDE.md §10) — plus the route-smoke tier, since this adds a route.
  **No migration.**
- **Resequencing risk checked, and it is clear.** `AbwabSectionConfiguration` indexes
  `Name` (unique, filtered) and `DeletedAtUtc` — there is **no unique index on
  `OrderValue`**, so a naive `1..N` rewrite cannot collide mid-update and needs no two-phase
  trick. (Compare `AbwabDoorConfiguration:81`, where `(SectionId, ParentId, OrderValue)` is
  indexed but deliberately **not** unique; the unique one is
  `(SectionId, ParentId, Name)` at `:94-97`.) So the doors resequence pattern transfers
  directly.
- **Adjacent instance (the sweep the user asked for):** the *other* user-facing ordered list
  with no reorder affordance is the **template list** on `/abwab/templates`
  (`abwab-templates-page.component.html:20-32`, rendered in
  `facade.templates()` order). `AbwabTemplate` should be checked for an order column; if it
  has none, this is where a migration would actually be needed. Flagged, not scoped.

## 19. Each section tab shows a count badge of the root doors inside it

- **Where:** `abwab-toolbar.component.html:15-23` renders `section.name` alone.
- **The data situation, precisely:**
  - `AbwabTreeSectionDto.doorsInScopeCount` exists and is **all live doors in the section at
    any depth** (`EfAbwabTreeReader.cs:37-40`). The user asked for **root doors only**. So
    the shipped field answers a *different* question and must not be used as-is.
  - Root-only count **is** derivable client-side with zero backend work:
    `snapshot.liveRoots.filter(r => r.sectionId === section.id).length`. `liveRoots` is
    exactly the depth-0 live partition (`state/abwab-tree.builder.ts`).
  - **Recommendation:** derive root-only in the builder (a `rootCountBySectionId` map beside
    the existing partition, pure and specced), and leave `doorsInScopeCount` for item 17's
    section stat, where its all-depths semantics is the right number. Two different counts
    answering two different questions is correct; reusing one for both is the trap.
- **⚠ This is an unimplemented line of the approved contract, not a new request.**
  `abwab-tree-concept.html:207` renders
  `<button class="tab active">كل الأبواب <span class="badge">33</span></button>`, with
  `.tab .badge{font-size:11px; color:var(--qd-muted); margin-inline-start:4px;}` at `:41`. The
  toolbar shipped the tabs without the badges. Reclassifies this item from "feature request"
  to "contract gap" — the cheapest item in the report.
- **Precedent — the backing class already ships and has no consumer:** `.qd-tabs__count`
  exists at `styles/_components.scss:208-222` (including its selected-state rule) and is
  listed in §17 `qd-tabs` as a backing class — but `grep` finds **zero** HTML using it, and
  `qdTab` (`shared/ui/tabs/tab.directive.ts:19-21`) exposes only `selected` and `disabled`.
  §17's rule for this situation is stated for tables and generalizes: *"a table needing a
  rule beyond `grid-template-columns` is a signal to **extend the base, not fork it**."*
- **Class:** ALIGNMENT (to both the concept and §17).
- **Fix / Size:** **component.** Add a `count?: number | null` input to `qdTab` that renders
  `.qd-tabs__count` (`null` = omitted, so the five existing call-sites are unaffected), then
  bind it in the abwab toolbar. Extend §17's `qd-tabs` entry with the `count` input in the
  same change. The «كل الأبواب» tab gets the total-live-roots count. Latin digits, matching
  the explorer tables' convention (§17 count-meta).

---

# Part 5 — Templates

## 20. ⟲ USER REVERSAL — apply copies the template's CHILDREN only, not its root

This is the item with the most ripples, and several reach the backend.

- **Where today (root IS copied):** `EfAbwabTemplateApplyWriter.cs` —
  `rootNode` located at **`:42-43`**; the collision pre-check keyed on `rootNode.Name` at
  **`:66-77`**; `nextOrder` = the target's live child count + 1 at **`:93-94`**; one
  `copiedRoot` per target at **`:96-99`**; the level-by-level descent copying that root's
  subtree at **`:107-134`** (children's verbatim `OrderValue` at **`:121-124`**);
  `NewDoor` at **`:154-170`**; the writer's `<remarks>` at **`:7-14`**; the
  *"one created root per target"* comment at **`:55-56`**.
- **⚠ The plan is a live input and it is the primary document this reverses.**
  `docs/feature-abwab-templates/plan.md` is the **open** feature's plan (root `CLAUDE.md`,
  "Active Spec Kit Feature"). Its **§5.1 is titled "The one sentence everything derives
  from"** (`:158-163`):
  > *"Applying a template inserts a copy of its root node as a NEW CHILD of each target door,
  > and recursively copies that node's subtree beneath it."*
  > *"Every matrix cell in §6 is a consequence of that sentence plus the doors' own write
  > invariants."*

  So this reversal replaces the plan's **axiom**, and every §6 matrix cell is downstream of it
  by the plan's own statement. Amend §5.1 first; the rest follows.
- **Reversal — record, do not litigate.** Recorded statements that must be amended:
  - **`plan.md:158-163` (§5.1)** — the derivation sentence above.
  - **`plan.md:116`** (§4 locked decisions, "Apply"): *"Deep copy. The template root becomes a
    **new child** of each target door, full depth, all four fields, sibling order preserved."*
  - **`plan.md:123`** (§4, "Apply collision"): *"If any target already has a live child named
    like the template root, the whole apply fails with one `409` naming every colliding
    target."*
  - **`plan.md:232-249` (§5.5)** — the section titled *"Sibling-name uniqueness inside a
    template is what keeps the copy honest"*, whose conclusion is
    *"**the only collision an apply can hit is at the root**… and it is the only `409` the
    apply route can produce"* (`:247-249`). **This argument does not survive the reversal.**
    The template's own `UNIQUE (template_id, parent_node_id, name)` index still guarantees the
    root's children are internally distinct — but it says nothing about the *target's*
    existing children, so the collision surface becomes N names per target instead of one. The
    §5.5 conclusion must be rewritten, not just annotated.
  - **`plan.md:140`** (route table, route 5): *"`201` created root doors"* — now N per target.
  - **`plan.md:330`** (§6.1 matrix cell): *"Live door that already has a live child «أركان
    الإيمان» → `409`, nothing is created anywhere"* — the anchor case is keyed to the root's
    name and must be re-keyed to the root's children; §6.3's deep-copy edge cells inherit it.
  - **Unaffected, do not touch:** `plan.md:87-88` and `:337` — *"No template application at
    root level… the API refuses a rootless apply (400)"* is about **target** doors, not the
    template's root node, and stays correct. Likewise §5.2's one-root-per-template index and
    *"the template's name is the root node's name"* (`:165-183`) survive — arguably read
    *better* afterwards, since the root becomes purely a naming/container row that is never
    copied.
  - `ABWAB_LABELS.templateCopyDescription` (`models/abwab.labels.ts:245`):
    «القالب سيُنسخ كاملًا **(بجذره وكل فروعه)** داخل كل باب تختاره» — says the root is copied.
  - **Correction to the obvious assumption:** `templateNodeCount` needs **no arithmetic
    change**. `AbwabTemplateSummaryDto` (`Application.Abstractions/Abwab/Responses/AbwabTemplateSummaryDto.cs:3-5`)
    documents *"NodeCount counts the root's live descendants and **excludes the root
    itself**"*, matching `plan.md:183`. So today the preview promises `nodeCount` elements
    while the apply actually creates `nodeCount + 1` doors per target — **the reversal makes
    the existing count correct.** Only the surrounding prose is wrong, not the number.
  - `abwab-template-copy-modal.component.ts:23-31` — *"The preview states the whole contract
    before the write: what each target gains…"*
  - The writer's `<remarks>` (`:7-14`) and its two inline comments (`:55-56`, `:121-124`).
  - `AbwabTemplateRootNodeException`'s rationale
    (`Application.Abstractions/Abwab/AbwabTemplateRootNodeException.cs`):
    *"deleting it would leave a template that cannot be applied"* — still true, but for a new
    reason (no root ⇒ no children to enumerate). Note `plan.md` §9 records that this one type
    covers **both** reorder and delete refusals; do not split it.
- **The ripples, concretely:**
  1. **Collision becomes per-child-name under each target.** Today the pre-check (`:66-77`) is
     one name: `d.Name == rootNode.Name` for each target parent. It becomes *the set of the
     root's direct child names* against each target's existing live child names.
     `AbwabTemplateApplyCollisionException` currently carries `IReadOnlyList<string>` of
     **target names**; the all-or-nothing message must now name **(target, colliding child
     name)** pairs, so the exception's payload shape and its Arabic message in `ApiMessages`
     both change. Precedent for naming the offenders rather than emitting a generic 409: the
     comment already at the pre-check (`:64-65`) — *"Named up front so the 409 can say WHICH
     targets collided; 23505 names no row"* — is the rule to extend, not replace. `plan.md` §9
     independently locks that pre-check-then-`23505`-backstop shape; keep both halves.
  2. **First-level ordering can no longer be carried verbatim.** Today one child is appended at
     `nextOrder = <live child count> + 1` (`:93-94`). Now N children are appended, so level-1
     order must be `nextOrder + i` for the template's sibling order `i`; deeper levels keep the
     verbatim `OrderValue` (`:121-124`) because their scopes are freshly created. The writer's
     remark (`:7-14`) *"every insert appends into a scope it either just created or is the
     newest member of, so all touched scopes stay 1..N by construction"* survives — but only if
     the offset is applied. Get this wrong and a target's child scope is no longer `1..N`,
     which the reorder path depends on. `plan.md` §9 also forbids guarding the concurrent-apply
     `order_value` race — that stays unguarded; this is a different, deterministic offset.
  3. **The response shape's meaning changes** from one `AbwabDoorDto` per target to N per
     target. Type is unchanged (`IReadOnlyList<AbwabDoorDto>`), so nothing breaks at compile
     time — which is exactly why it must be written down. The copy modal's success path
     ignores the payload (`abwab-template-copy-modal.component.ts:177-187`), so no frontend
     consumer breaks.
  4. **"Same template twice is no longer auto-blocked" — partly.** Applying twice still
     collides, on the *children's* names rather than the root's. What genuinely changes is
     the **empty-root template**: a template whose root has no children becomes a **no-op
     apply** (nothing created, no collision ever). Today that is impossible, because the root
     itself is always the one guaranteed copy. That is a **new edge case needing a decision**:
     refuse with a `400` («القالب لا يحتوي عناصر لنسخها»), or allow a silent no-op. Recommend
     refusing — the copy modal's confirm button would otherwise promise N copies and produce
     zero. Note the workshop can reach this state today: the root is created with the template
     and children are added separately.
  5. **The matrix / preview copy.** `templateCopyDescription` and `templateCopyPreview` need
     rewriting to state "the template's elements (without its root) are copied into each
     target"; `templateCopyConfirmButton(count)` counts **targets** and is unaffected.
     `templateCopyPreviewNoRoot` («لا يمكن النسخ كباب رئيسي») stays correct and becomes *more*
     obviously so. `templateCopyPreviewDetached` is unaffected. And per the correction above,
     the element **number** already matches children-only — only the prose lies today. The
     concept's own copy («بجذره», `abwab-relations`-era concept `:139,:147` as cited in
     `plan.md:167-168`) is superseded by this reversal; record that, since the workshop concept
     is treated as a contract elsewhere in this report.
  6. **Untouched invariants — do not "fix" them while here:** the confirm count stays the
     number of targets, never a union (abwab README); the copy stays detached at birth
     (no `templateId`, no provenance); the apply still refreshes nothing.
- **Precedent:** none for children-only apply — this is a product-semantics reversal, not a
  pattern gap. The precedents that *do* govern it are the ones above (name the offenders in a
  409; keep every scope `1..N`; all-or-nothing inside one transaction).
- **Class:** ALIGNMENT is not the right frame — it is a **contract change**. Treat as its own
  slice with its own review.
- **Fix / Size:** **backend** (writer + exception payload + `ApiMessages` copy) **and**
  component (preview/description/confirm copy, `templateNodeCount - 1`). Same change owes:
  the writer's `<remarks>`, `Persistence/Writes/Abwab/README.md`, the abwab feature README's
  templates paragraphs, and the route-smoke tier (contract change on an existing route → the
  `SmokeRouteCatalog` entry exists but its expectations move).

## 21. The workshop gets the doors tree's right-click context menu

**Half of this already exists.** Reporting the real gap.

- **What exists:** `abwab-templates-page.component.html:208-245` already renders a row context
  menu (edit / add-child / delete-node, with the root swapping delete-node for
  delete-template), and `abwab-template-tree` already emits `menuRequested`.
- **The real gap — three of the doors tree's four menu paths are missing.**
  `abwab-template-tree.component.ts:104-105` emits the menu **only** from the `⋯` button's
  click. The doors tree emits it from four paths: `⋯` (`abwab-tree.component.ts:145-148`),
  **right-click** (`:130-136`, `onRowContextMenu` with `preventDefault`), and the
  **keyboard** `ContextMenu`/`Shift+F10` path (`:246-253`, anchored to the focused row's
  `getBoundingClientRect()` — with the comment *"a menu pinned at (0,0) is not a usable
  keyboard path"*). The template tree has no `(contextmenu)` binding and no keyboard model at
  all.
- **The second, larger gap the sweep found: the two menus are duplicated CSS.** A `diff` of
  the two pages' menu SCSS is byte-identical modulo the BEM prefix — backdrop
  (`position: fixed; inset: 0; z-index: 49`), menu (`z-index: 50; min-inline-size: 11rem;
  --qd-shadow-lg`), item buttons, hover, focus ring, danger variant. §17 lists **no menu
  primitive**, so the app has two hand-rolled popup menus and will grow a third.
- **Precedent:** for the *behavior*, the doors tree (cited above) — pure ALIGNMENT. For the
  *artifact*, none: no `qd-menu` exists. The nearest shape is the navbar dropdown
  (`top-navbar.component.html:44-60`), which is a hover menu with different semantics.
- **Plan check:** `docs/feature-abwab-templates/plan.md` specifies **no** context menu for the
  workshop anywhere (grep for context-menu / right-click / قائمة السياق across all 1097 lines
  returns nothing; phase 7, `:696-761`, covers the workshop page and its tree editor). So the
  `⋯`-only implementation is **not** a plan deviation — the existing menu was built beyond the
  plan, and this item extends it. Nothing to amend; noted so the reversal list stays at four.
- **Class:** MIXED — behavior parity is ALIGNMENT; extracting a menu primitive is a
  **NEW PATTERN**.
- **Fix / Size:** **component.** (a) Add `(contextmenu)` + the keyboard path to
  `abwab-template-tree`, reusing the anchor-point contract (`{ nodeId, x, y }` already
  matches). Note the README's recorded reason the template tree is a list and **not**
  `role="tree"` — *"claiming the role without the arrow-key model would promise a navigation
  contract the workshop does not implement"* — so adding `ContextMenu`/`Shift+F10` alone does
  **not** license adding the role; either add the full RTL keyboard model (and then the role
  legitimately follows) or add only the menu key and say so. (b) Extract
  `qd-context-menu` (backdrop + positioned `role="menu"` + item styling + Escape + focus
  handling) into `shared/ui/`, compose it from both pages, and add its §17 entry. Do (b)
  **before** (a) or the parity work lands in code that is about to move.
  **(b) done — Slice A** (`docs/feature-ux-slice-a/plan.md` phase 6): `shared/ui/context-menu/`
  now exists, both pages compose it, both duplicated SCSS blocks are deleted, and its §17 entry
  is written (`.architecture/UI_STYLE_SYSTEM.md`, `qd-context-menu` entry — including the two
  gaps it deliberately left open: no viewport clamping, no focus management into the menu).
  **(a) remains open**, unmoved, for whichever slice takes `abwab-template-tree`'s keyboard/
  right-click parity (plan §2 names it Slice G).
- **Adjacent instances:** `abwab-cards` (`abwab-cards.component.html`) offers no row menu at
  all — the tree does, cards do not, and no README records that as deliberate. Worth a
  decision. `abwab-archive-view` correctly has none (restore-only is a recorded invariant).

---

# Part 6 — Navbar

## 22. ⟲ USER REVERSAL — الأبواب becomes a hover dropdown: الرئيسية / القوالب / الأرشيف

- **Where:** `NAV_ITEMS` (`core/navigation/nav-items.ts`) carries a flat `abwab` entry
  routing to `/abwab`, rendered by the generic `@else` branch at
  `top-navbar.component.html:62-74`. The words dropdown is hard-coded:
  `@if (item.key === 'words')` (`:8`), with `WORDS_MENU_ITEMS`
  (`core/navigation/words-nav-items.ts`) and the `wordsOpen`/`openWords`/`closeWords`
  members (`top-navbar.component.ts:29-31, 82-94`).
- **⟲ Reversal this audit found (fourth one) — record, do not litigate.**
  `abwab.routes.ts:19-21` records the opposite decision verbatim:
  *"The workshop is reached from the doors page header, **not the sidebar**, so its title is
  its own page title rather than a `navLabel`: `navLabel` throws on a key `NAV_ITEMS` does not
  carry, and **adding one would put an item in the nav nobody asked for**."* Item 22 is
  exactly that item. Same treatment as 12/13/20: amend the comment, do not argue with the
  request. Note the mechanical consequence the comment already predicts — `navLabel('…')`
  **throws** on an unknown key, so adding nav entries changes what
  `abwab.routes.ts:16,24` may call.
- **Precedent — exact, and it is a *generalization* opportunity:** the words dropdown
  (`top-navbar.component.html:8-61`) already implements hover-open (`mouseenter`/`mouseleave`),
  click-toggle, `aria-haspopup`/`aria-controls`/`aria-expanded`, Escape
  (`top-navbar.component.ts:45-56`), outside-click dismissal (`:58-71`), a chevron, and an
  active-parent state via `router.isActive(…, { paths: 'subset' })` (`:134-141`).
- **Class:** ALIGNMENT of behavior; the generalization is small and NEW only in the sense that
  a *second* dropdown makes the hard-coded `item.key === 'words'` branch untenable.
- **Fix / Size:** **component.** Add an optional `children?: NavItem[]` (or a `menuKey`) to
  `NavItem`, replace the `item.key === 'words'` special case with a data-driven
  `@if (item.children)` branch, and collapse `wordsOpen`/`moreOpen` into one
  `openMenuKey: string | null` so the mutual-exclusion logic
  (`toggleMore` clears `wordsOpen` and vice versa) stops being pairwise. Then add the abwab
  children.
- **Two specifics to decide, both non-obvious:**
  1. **الأرشيف is not a route** — the archive view is the `archive=1` query param on `/abwab`
     (abwab README URL contract), so that menu entry must be
     `[routerLink]="/abwab" [queryParams]="{archive:'1'}"`, and its `routerLinkActive` needs
     `queryParams: 'exact'` or it will light up on the live view too. Every existing dropdown
     link is a plain path, so this is the first query-param nav entry in the app.
  2. **الرئيسية** as a label for `/abwab` collides with the dashboard's «لوحة التحكم» register;
     «الأبواب» or «شجرة الأبواب» reads better in the parent-named-الأبواب context. Flagged as
     copy, for the user to settle.
- **Mobile:** `allItems` drives a flat `mobile-menu-list`
  (`top-navbar.component.html:286-300`) — the words children are already absent there, so
  abwab's would be too. Consistent, but worth confirming that is intended rather than a
  second gap.

---

# Part 7 — Cache

## 23. Tree + templates + archive cached on both ends, invalidated only by their own mutations

This is the slice where the constraint the user named — *caches must never serve stale `xmin`
tokens* — does most of the design work.

### The governing invariant, stated precisely

The abwab README's first gotcha is the authority: *"**Refresh-after-write is an invariant, not
an optimization.** Every write resequences its scope to `1..N`, which bumps every sibling's
`xmin` too. A root-affecting write additionally maintains the global order in the same request,
which resequences **every live root everywhere** — so after any such write, the stale version
tokens are not confined to one scope at all… Skipping the refresh reproduces spurious `409`s on
the very next write."*

Two consequences that must be written into the cache design, not discovered later:

1. **The tree snapshot is a single indivisible cache entry.** There is no safe partial
   invalidation — one root-affecting write invalidates every row's token. So the entry key is
   the whole snapshot, and *any* door/section/relation/alias write evicts it entirely. The
   README already forbids the narrower option: *"it does mean a narrower, scope-only refresh
   would no longer be safe."*
2. **A conditional read must never let the client keep tokens it thinks are current.** This is
   the trap in an ETag design: after a write, the client's `If-None-Match` may still match a
   backend cache entry that was populated *before* the write if invalidation is not strictly
   ordered before the response returns. The write path must evict **inside** the write's
   transaction boundary (or immediately after commit, before responding), so the
   post-write refetch the frontend already performs
   (`state/abwab-write.controller.ts`) can never be served a pre-write body. State this as the
   rule; do not leave it to implementation.

### What exists on each end

- **Backend — a decorator pattern, but built for immutable data.**
  `Infrastructure/Caching/**` holds ~13 `Cached*Reader` classes that decorate an `Ef*Reader`
  with `IMemoryCache` (e.g. `CachedRootsReader.cs:7` —
  `CachedRootsReader(EfRootsReader efReader, IMemoryCache cache) : IRootsReader`), plus
  `Caching/CacheLoadGate.cs`. **Every one of them caches Quran reference data that is never
  mutated at runtime**, so **no invalidation path exists anywhere in the backend**. Abwab has
  no cached reader at all (`Persistence/Reads/Abwab/` is `EfAbwabTreeReader`,
  `EfAbwabRelationsReader`, `EfAbwabTemplatesReader`, uncached).
- **Backend — no HTTP caching at all.** `grep` for `ETag`, `If-None-Match`,
  `ResponseCache`, `OutputCache` across `api/`, `application/`, `infrastructure/`, `shared/`
  returns **zero** hits. `AbwabTreeController.Get` is an unconditional 200 with a full body.
- **Frontend — facades are the cache, and their contract is already written.**
  `AbwabSnapshotFacade` and `AbwabTemplatesFacade` are `providedIn: 'root'` *because they are
  caches* (abwab README: *"Root-scoped: it is a cache"*), with the contract *"`refresh` always
  refetches; a failure leaves the previous value in place."* There is no TTL, no
  conditional request, and `AbwabPageComponent.ngOnInit` calls `facade.load()` on **every**
  entry to `/abwab` (README, "The apply refreshes nothing, on purpose") — so today, navigating
  to `/abwab` always refetches the whole snapshot.
- **`AbwabTreeDto.version` already exists** — `max(updated_at, deleted_at)` across sections,
  doors, and aliases (`EfAbwabTreeReader.GetSnapshotVersionAsync`, `:89-103`). **But the abwab
  README pins it down: *"`AbwabTreeDto.version` is diagnostics only. Per-row `xmin` tokens are
  the only concurrency currency; do not build snapshot-level conflict detection on it."*** An
  ETag is *not* conflict detection — it is cache validation — so using it as the ETag source is
  defensible, but it must be argued explicitly against that sentence, and the README amended
  to distinguish the two uses. Note the field's gap: it does **not** cover
  `AbwabDoorRelations` or the templates tables, so as an ETag source it would miss relation
  writes (which change `relationCount` on two rows) — either extend the aggregate or use a
  different token.

### Classification and shape

- **Class:** **NEW PATTERN**, on both ends.
  - Backend: mutable-data caching with invalidation, and HTTP conditional GET, are both
    firsts. Precedent exists only for the *decorator shape* (`Cached*Reader` + `IMemoryCache`),
    not for its lifecycle.
  - Frontend: conditional requests / cache validators are a first;
    `.architecture/API_INTEGRATION_GUIDELINES.md` says nothing about caching at all.
- **Fix / Size:** **backend** + component, and this should be the **last** slice — it is the
  only one whose correctness depends on every other write path being final. Shape:
  1. `CachedAbwabTreeReader` / `CachedAbwabTemplatesReader` decorators on the existing
     `Cached*Reader` precedent, wired in `AbwabDependencyInjection.cs`. One entry per read;
     the tree entry is indivisible (above).
  2. An explicit invalidation surface (e.g. `IAbwabCacheInvalidator`) that **every** abwab
     write command calls after commit and before responding. The writes are already funnelled
     through a small set of writers, and the frontend already funnels through one
     `AbwabWriteController` — mirror that on the backend so no write can forget.
  3. `ETag` + `If-None-Match` on the three reads, returning `304` with no body. Source the
     validator from a token that covers **doors + sections + aliases + relations** (and, for
     templates, the templates tables) — not the current `version` field unless it is extended.
     `304` must be unreachable for a client that has just written, by the ordering rule above.
  4. Frontend: send `If-None-Match` from the facades, treat `304` as "keep current value"
     (which is already the facades' failure semantics, so the state machine barely changes),
     and **keep the unconditional `load()` on route entry** — with a validator it costs a
     `304`, not a body.
  5. Archive: it is not a separate resource — the archive view is a partition of the same
     snapshot (`archivedRoots`, `state/abwab-tree.builder.ts`). It is cached by the tree entry
     and needs nothing of its own. Say so, or someone will build a third cache.
- **Doc obligations in the same slice:** a §-level note in
  `.architecture/API_INTEGRATION_GUIDELINES.md` (conditional-request handling is an API
  integration rule), `Persistence/Reads/Abwab/README.md`, `Backend/.architecture/API_GUIDELINES.md`
  (ETag/`304` is a response-shape rule and `ApiResponse` does not describe a bodiless
  response), the abwab feature README's `version`-is-diagnostics-only gotcha, and a
  `SmokeRouteCatalog` review since response semantics on three routes change.

---

# Part 8 — Cross-cutting sweep: the classes, not the instances

Each row is a class of defect found by sweeping for adjacent instances, so fixes land as rules.

| Class | Instances found | Rule to land |
|---|---|---|
| Ad-hoc `qd-loading/empty/error-state` instead of `qd-state`/skeletons | 3 loaders + 9 empty/error paragraphs, **all in abwab**; 29 non-abwab HTML files already use `qd-skeleton` | §17 already forbids it — abwab is the only violator |
| Missing focus trap in a dialog | 6 abwab modals; 7 non-abwab dialogs have `cdkTrapFocus` | Every `role="dialog"` gets `cdkTrapFocus cdkTrapFocusAutoCapture` |
| Dialog missing dialog semantics entirely | `abwab-sections-modal.component.html:3` (no role/aria-modal/labelledby/Escape/scroll-lock) | Same rule as above |
| Unbounded modal geometry | all 6 abwab modals via `.qd-modal` (`_components.scss:554-564`) | Extend `.qd-modal` with `qd-detail-modal-shell`'s fixed-both-axes + single-scroller rule; §17 entry |
| Truncatable name with no `[title]` | **all 11** abwab name-render sites | `[title]` wherever text can truncate (precedent `word-type-filter.component.html:57`) |
| Truncatable name with no ellipsis | `__pick-name` (relations), `.abwab-sections-modal__name`, `.abwab-templates-page__item-name` | `flex:1; min-inline-size:0; overflow:hidden; text-overflow:ellipsis` |
| Unstyled / unnamed checkbox | all 4 in the app, all in abwab; 2 of 4 lack an accessible name | New `.qd-checkbox` / `.qd-check-row` in `_forms.scss` + §17 entry |
| Inner scroller without a stable gutter | 2 abwab pick-lists; mushaf's 2 scrollers already compose `.qd-scroll-stable` | Add the utility class |
| Hand-rolled popup menu | 2 (doors page, templates page) — byte-identical SCSS | Extract `qd-context-menu` to `shared/ui/` + §17 entry |
| Hand-rolled segmented control | `abwab-relations-modal` type segment (+ the direction pill, judgment call) | `qd-tabs` is "the one tab-strip implementation app-wide" |
| Hand-rolled count pill | `abwab-relations-modal__count`, `abwab-templates-page__count`, `abwab-tree__count` | `qd-chip` `count` / `.qd-tabs__count` |
| Error surface that mounts/unmounts | 6 abwab sites | Reserved-slot error (precedent: `explorer-result-count`, detail-shell count box) |
| Ordered list with no reorder affordance | sections (item 18); **template list** (`abwab-templates-page.component.html:20-32`) — check `AbwabTemplate` for an order column | The doors-reorder path is the template for all three layers |
| Hard-coded nav special-case | `item.key === 'words'` (`top-navbar.component.html:8`) | Data-driven `children` on `NavItem` before adding a second dropdown |
| No z-index scale | navbar (none), ctx menus (49/50), modal backdrop (50) | `--qd-z-*` tokens before item 6 makes the navbar sticky |
| Backing class shipped with zero consumers | `.qd-tabs__count` (`_components.scss:208`) | Either give `qdTab` a `count` input (item 19) or delete the class |
| **Approved-contract line dropped in implementation** | section tab count badge (`abwab-tree-concept.html:41,207`); checkbox sizing (`abwab-relations-concept.html:84`); relations count pill exists but hand-rolled (`:118`) | Re-check the three concept files against the shipped surfaces as its own pass — this audit found three by walking the user's list, not by auditing the contracts systematically, so more may remain |
| Selected-state not per §16.1 | `abwab-relations-modal__type--active` (`scss:149-154`, surface+bold) vs the direction pill (`scss:198-202`, §16.1-correct) in the same modal | §16.1 is the doctrine; a mockup's ad-hoc active state does not outrank it |

---

# Part 9 — Recommended slices, in order

Pattern decisions land **before** the abwab fixes that consume them, so no fix hand-rolls
what a primitive is about to provide and nothing is reviewed twice.

### Slice A — Shared primitives and rules (pattern decisions)
**Items:** 16 (checkbox), 7 (`.qd-modal` geometry), 5 (reserved error slot), 15-partial
(name width/tooltip rule), 21-partial (`qd-context-menu`), plus the `--qd-z-*` token scale
that item 6 needs. **Owes §17 entries + `styles/README.md` + `UI_STYLE_SYSTEM.md` amendments
in this slice.** Touches `styles/` and `shared/ui/` → **Tier B** frontend tests (shell/shared/
theming scope), plus a `npm run build`.
*Why first:* every later slice composes at least one of these.

### Slice B — Page frame and layout stability
**Items:** 1 (full width), 2 (skeletons + `qd-state`), 3 (two pick-list scrollers), 4
(content min-height), 6 (sticky navbar), 5-applied, 17 (stats bar).
*Why second:* it consumes Slice A's error slot and z-index tokens, and it is the slice the
user will *see* — the "nothing shifts" philosophy becomes visible here. Shell + `styles/`
again → **Tier B**. **Resolve the 4-vs-6 conflict before starting.**

### Slice C — The modal system
**Items:** 7-applied (all six modals restructured to head/body/foot), 8 (focus trap +
autofocus), 7-adjacent (sections modal's missing dialog semantics), 9 (relations modal
redesign: 9a ellipsis, 9b checkbox, 9c compose `qd-tabs`/`qd-chip`/`qd-state`, 9d spacing),
16-applied.
*Note:* `abwab-relations-modal` and `abwab-template-copy-modal` have **no specs at all**
(`docs/TESTING_DEBT.md` row 4). The abwab README states the unification trigger for their two
duplicated pickers: *"when the relations modal next changes shape and gets its specs, both
pickers become one."* **This slice is that trigger.** Write the specs, then unify the pickers,
then redesign. Doing 9 without the specs is refactoring untested code.

### Slice D — Tree and row behaviors
**Items:** 12 (⟲ blur cancels), 13 (⟲ always-visible clickable flag + metadata vocabulary),
14 (three badges + the row width budget), 15-applied, 10 (reveal-in-tree).
*Why after C:* 10's reveal needs the relations modal's names to be real controls, which
Slice C's 9c reshapes. All four reversal/README amendments for 12 and 13 land here.
Tree specs exist (`abwab-tree.component.spec.ts`, `abwab-tree-keyboard.controller.spec.ts`,
`abwab-operations.e2e.ts`) → focused **Tier A** plus those e2e flows.

### Slice E — Restorable overlays
**Item:** 11.
*Alone, because it changes the abwab URL contract* (a seventh query key with its own
fail-closed parse and scope-invalidation rule) and refines a recorded README invariant about
why overlay state is page-scoped. `abwab-url-and-a11y.e2e.ts` + `abwab-url-sync.spec.ts` are
the gates.

### Slice F — Sections
**Items:** 18 (reorder: endpoint + writer + UI), 19 (tab count badges).
*First backend slice.* Adds a route → `SmokeRouteCatalog` entry **required in the same
change** + the route-smoke tier, and the evidence must state whether `Tests.Smoke.Data` ran or
skipped. **No migration needed** — `AbwabSection.OrderValue` already exists. 19 extends
`qdTab`, which is `shared/` → Tier B.

### Slice G — Templates
**Items:** 20 (⟲ children-only apply), 21 (right-click + keyboard parity, composing Slice A's
`qd-context-menu`).
*Own slice, own review:* 20 is a **contract change** touching the writer, an exception
payload, `ApiMessages` Arabic copy, two preview strings, four recorded rationales, **and the
open feature's plan — including its §5.1 axiom and the whole of §5.5** — plus the
empty-root-template decision, which needs the user's answer before implementation. Amend
`plan.md` §5.1 *first*: the plan states every §6 matrix cell is a consequence of that one
sentence, so re-deriving the matrix from the new sentence is the cheapest way to catch cells
this audit did not enumerate. Route-smoke tier required (response semantics change on an
existing route).

### Slice H — Navbar
**Item:** 22 (⟲ dropdown, via a data-driven `NavItem.children`).
*Small and independent.* Two open questions to settle first: the الأرشيف query-param link's
`routerLinkActive` semantics, and the الرئيسية label. Core layout → **Tier B**.

### Slice I — Cache
**Item:** 23 (both ends, ETag/`If-None-Match`, invalidation).
**Last, deliberately.** Its correctness depends on every abwab write path being final — Slices
F and G both add or change writes, and each new write is another invalidation call that must
not be forgotten. Building the cache first means auditing it twice.

---

# Appendix — the four reversals, as a checklist

| # | Reversal | Recorded text that must be amended |
|---|---|---|
| 12 | reorder commit-on-blur → **blur cancels**, Enter-only commits | `features/abwab/README.md` (tree render-chain line — currently silent on blur, which is how the code drifted); `abwab-tree.component.spec.ts`; `e2e/abwab-operations.e2e.ts` |
| 13 | relations flag render-only-when->0 → **always visible, dimmed at zero**, and **clickable** | `features/abwab/README.md` (flag line + the "Zero dead controls" gotcha calling it *"the one deliberate non-control"*); `abwab-tree.component.scss:89-90` (*"A chip, not a control… plan §7 T603"*) |
| 20 | apply copies root → **children only** | **`docs/feature-abwab-templates/plan.md` (the open feature's live plan) — §5.1 `:158-163` the derivation axiom, §4 `:116` and `:123`, §5.5 `:232-249` (its conclusion collapses), route table `:140`, §6.1 `:330` + §6.3 cells**; `abwab.labels.ts:245` («بجذره وكل فروعه»); `abwab-template-copy-modal.component.ts:23-31`; `EfAbwabTemplateApplyWriter.cs:7-14, 55-56, 121-124`; `AbwabTemplateRootNodeException.cs` rationale; `Persistence/Writes/Abwab/README.md`; the abwab feature README's templates paragraphs. **Not** `plan.md:87-88,:337` (rootless-*target* refusal) or §5.2's one-root index — both survive. **No** `nodeCount` arithmetic change (`AbwabTemplateSummaryDto.cs:3-5` already excludes the root) |
| 22 | «القوالب» reached from the page header only → **a nav dropdown** *(found by this audit)* | `abwab.routes.ts:19-21` (*"reached from the doors page header, not the sidebar… adding one would put an item in the nav nobody asked for"*) |

**None of these are conflicts to litigate.** Each is a recorded decision the user has now
decided differently; the obligation is to amend the record in the same change that changes the
behavior, so the next agent reads the current rule rather than the superseded one.
