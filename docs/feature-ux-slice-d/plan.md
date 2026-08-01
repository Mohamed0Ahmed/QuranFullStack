# Slice D — Tree and row behaviors (UX audit)

Source: `docs/abwab-ux-audit.md` "Slice D — Tree and row behaviors" (`:1094-1100`) —
items 12, 13, 14, 15-applied, 10 — plus the appendix reversal-checklist rows 12 and 13
(`:1145-1146`), the two §17 debt lines Slice C narrowed toward D (checkbox
`UI_STYLE_SYSTEM.md:941-949`, truncation `:1100-1105`), and two commissioned additions
named by the commissioning prompt: a read-only frontend performance pass over Slice C's
merged modal work, and the live bulk-archive 404 bug.

**Mode when this plan was written:** plan-only. No code, no Git, nothing amended.

## Precondition — VERIFIED on `dev` (`a6601a1f`, clean) at plan time

| Consumed primitive / mechanism | Where it lives | Verified |
|---|---|---|
| Slice C merged to `dev` | PR #55, merge `a6601a1f` | ✅ |
| `.qd-truncate` | `src/styles/_utilities.scss:58-63` | ✅ shipped; abwab consumers today: door-picker rows + relations `__target` chips only (`abwab-relations-modal.component.html:36`) |
| `.qd-checkbox` / `.qd-check-row` + `--qd-checkbox-size` | `src/styles/_forms.scss`, `_tokens.scss:166` | ✅ shipped; sole consumer `abwab-door-picker` — tree/cards still on local rules |
| `--qd-name-min-inline-size` (reserved-floor token) | `src/styles/_tokens.scss:170-182` (12rem) | ✅ shipped, zero consumers — built for exactly this slice's name sweep |
| `qd-chip` component: `count` input, `removable`, static classes | `shared/ui/chip/chip.component.html:1-53`; `.qd-chip__count` | ✅ shipped; removable branch wraps in a **static `<span>`**, so a second nested control is valid HTML |
| `AbwabTreeComponent.forceExpandedIds` (unioned with manual toggles) | `abwab-tree.component.ts:51-52,67-69`; page binds search auto-expand only (`abwab-page.component.ts:113`, `.html:159`) | ✅ |
| URL patch that survives the scope-invalidation clear | `abwab-url-sync.ts:44-47,65-75` — *"an explicit `door`/`card` in the same change overrides the invalidation clear"* | ✅ — cross-section reveal is one `router.navigate` |
| Select-then-act invariant + its worked example | README `:218-223`; `AbwabPageOverlaysController#runContextAction` (`abwab-page-overlays.controller.ts:327-339`) | ✅ pattern for the flag-click and reveal wiring |
| `$qd-bp-tablet-max` | `src/styles/_breakpoints.scss:3` (1023px) | ✅ |
| Tree specs + keyboard-controller spec + e2e reorder flows | `abwab-tree.component.spec.ts` (M29 at `:222`), `abwab-tree-keyboard.controller.spec.ts`, `e2e/abwab-operations.e2e.ts:8-33` | ✅ live gates for the two reversals |

## 0. Guard result

Task arithmetic: Phase 1 = 2, Phase 2 = 2, Phase 3 = 3, Phase 4 = 2, Phase 5 = 2,
Phase 6 = 2, Phase 7 = 3, Phase 8 = 4, Phase 9 = 4. **24 tasks — under the 30-task
threshold. One slice, no split.**

Recorded so a mid-execution split does not get drawn on task count: if this slice had
split, the seam is **after Phase 7** — "row-surface reversals and sweeps" (items 12, 13,
14, 15-applied, 16-applied, plus the bulk-archive fix: every one lands on surfaces with
existing specs/e2e, amended in the same change) versus "the reveal behavior" (item 10: a
`shared/ui` primitive extension, a new page mechanism, and the app's first timed
highlight — the only NEW-PATTERN work in the slice). The seam is who can be hurt: Phases
3–7 are guarded by suites that already exist; Phase 8 creates behavior nothing pins yet.

## 1. Objective

| # | Deliverable | Home | Audit item |
|---|---|---|---|
| 1 | Read-only performance findings over Slice C's modal work (focus traps + `qd-tabs` on the open path, change-detection cost of the six shells), each with severity — fixes NOT applied | `docs/feature-ux-slice-d/evidence.md` | commissioned input 1 |
| 2 | Bulk-archive stale-id 404 fixed frontend-side: the bulk set drops archived ids per §4.6's own words, submit never sends a door the snapshot knows is gone, and the all-or-nothing failure message names the offending door(s) | `abwab-selection.store.ts`, `abwab-write.controller.ts`, `abwab.labels.ts` | commissioned input 2 |
| 3 | ⟲ Reorder input: Enter commits, **blur cancels**, Escape cancels — README/spec/e2e amended in the same change | `abwab-tree` + README + e2e | 12 (appendix row 12) |
| 4 | ⟲ علاقات flag: always visible, dimmed at zero, clickable (opens the relations modal) — README flag line, "Zero dead controls" gotcha, and the SCSS comment amended in the same change | `abwab-tree` + page wiring + README | 13 (appendix row 13) |
| 5 | Row metadata (order / counts / flag) on one chip vocabulary | `abwab-tree` SCSS + `qd-chip` classes | 13 (formatting half) |
| 6 | Three per-door badges — direct children, total live descendants, max relative depth — inside a written row-width budget | `abwab-tree.builder.ts` + `abwab-tree` | 14 |
| 7 | `.qd-truncate` + mandatory `[title]` on every remaining abwab name-render site; local ellipsis rules deleted | eight components/pages (call-sites in §5) | 15-applied |
| 8 | Tree + cards checkboxes compose `.qd-checkbox`/`.qd-check-row` with accessible names; §17 debt lines paid | `abwab-tree`, `abwab-cards` | 16-applied (§17 debt) |
| 9 | Reveal-in-tree from the relations modal, with explicit per-state rules (other section, cards view, active search, archived target) and the app's first reveal highlight (§16-safe, reduced-motion-safe) | `qd-chip` + relations modal + page | 10 |
| 10 | Docs true again: README render-chain/flag/gotcha lines, §17 checkbox/truncation debt lines, new §17 reveal-highlight + `qd-chip` label-control entries | docs | doc integrity |

## 2. Scope

**In:**

- `abwab-tree` (component, SCSS, spec) and `abwab-tree-keyboard.controller.spec.ts` where
  the flag button or badges brush the roving-tabindex assertions.
- `abwab-cards` (checkbox composition, name/crumb truncation).
- `abwab-selection.store.ts` + spec, `abwab-write.controller.ts` + spec, `abwab.labels.ts`
  (bulk-archive fix + new Arabic strings on the counted-noun helper).
- `abwab-relations-modal` (relation-name control + `revealRequested`), `shared/ui/chip`
  (the label-control extension, decision 4.2-7).
- `abwab-page.component.*` + `abwab-page-overlays.controller.ts` (flag-click wiring,
  reveal mechanism), `abwab-url-sync.ts` **consumers only** — the six keys and their parse
  are untouched.
- `abwab-archive-view`, `abwab-side-panel`, `abwab-move-picker`, `abwab-sections-modal`,
  `abwab-templates-page`, `abwab-template-tree` — name-render sites only (`.qd-truncate` +
  `[title]`).
- `e2e/abwab-operations.e2e.ts` — the two reversal amendments (appendix rows 12/13).
- Docs the above force: `features/abwab/README.md`, `UI_STYLE_SYSTEM.md` §17 (+§16.2 note),
  the `abwab-tree.component.scss:89-90` comment.

**Out (named so nobody "finishes the thought"):**

- **Item 11 — the restorable overlay / URL contract. Slice E owns it whole**; nothing here
  adds a seventh query key or a restore control.
- Sections reorder + tab count badges (Slice F), templates children-only apply + workshop
  menu parity (Slice G), navbar dropdown (Slice H), cache/ETag (Slice I).
- **Backend changes of any kind** — the Phase 3 investigation's verdict is frontend-only
  (§4.2-1); if execution finds that verdict wrong, the phase gate stops the slice.
- Performance **fixes**. Phase 2 records findings; a fix happens only if the user accepts a
  finding, as its own scoped change.
- The archive view and cards do **not** gain the relations flag — the README derivation
  (visible relation count is always 0 there) stands and is not part of reversal 13
  (audit `:469-472`).
- The move picker's flat destination list, the template tree's list-not-`role="tree"`
  stance, and every other recorded README invariant not named in §1.
- Any planning-artifact deletion (§3) and any `dev → main` merge.

## 3. Non-goals

- **No planning-artifact sweep in this slice — standing user decision.** ALL
  planning-folder sweeps (including `docs/feature-abwab-templates/` and any other
  closed-feature folder) are deferred to one cleanup pass after Slice I. Nothing here
  deletes or repoints a planning folder. **Not deferred:** same-change README/§17
  amendments for behavior this slice changes — those stay mandatory (§1 rows 3, 4, 10).
- No new E2E posture: the two e2e amendments are the appendix's mandated same-change
  updates to existing flows, not a new tier; no e2e run is cited as a gate.
- No hard fixed-width name column. §17 (`:1075-1092`) settled this: flexible-with-ellipsis,
  reserved floor via `--qd-name-min-inline-size` where sibling pressure demands one.
- No new hue anywhere — the reveal highlight derives from §16.1/§16.3 tokens.
- No reorder affordance changes beyond the blur semantics (the editor's click-to-edit,
  Enter, Escape grammar is untouched).

## 4. Locked decisions

### 4.1 Carried in from the audit / prior slices / standing rules

1. **Reversals are recorded, not litigated** (audit `:1150-1152`). Items 12 and 13 amend
   every recorded text in the same change that changes the behavior — appendix rows 12
   and 13 are the checklist.
2. **Enter-only commit is the feature's own idiom** (audit item 12): the workshop's two
   inline authoring rows already commit on Enter with no submit button (README `:440-444`).
   The reversal aligns the reorder editor to it. New rule: **Enter commits; blur and
   Escape both cancel.**
3. **The flag reversal does not extend to archive view or cards** (audit `:469-472`) —
   the README's always-zero derivation there still holds.
4. **Badge semantics are live-only** (audit item 14; `EfAbwabTreeReader.cs:30-32` states
   the rule for the two existing counts). An archived subtree never inflates a badge.
5. **The two new badges are a contract extension** beyond
   `abwab-tree-concept.html:107,439` (which specifies direct children only). The user
   commissioned them; recorded as an extension, not a missed concept line (audit `:496-502`).
6. **"Max internal depth" is relative to the door** — child + grandchild +
   great-grandchild = 3; never `node.depth`. The Arabic label copy must pin that meaning
   (audit `:520-523`).
7. **§17's truncation contract governs the sweep**: `.qd-truncate` on a `flex: 1` item
   (floor via `--qd-name-min-inline-size` only where needed), `[title]` **mandatory** on
   every composing element (`UI_STYLE_SYSTEM.md:1093-1099`), local rules deleted, not
   shadowed (the specificity trap, `:950-957`).
8. **The reveal highlight must sit above the hover rung and freeze under
   `prefers-reduced-motion`** — `_tokens.scss:94`'s recorded lesson (a mark ΔL-too-close
   to hover reads as a flash) is the contrast warning; §16.3's allowed-green list bounds
   the color; §15-F/§17's blanket reduced-motion rule applies (audit item 10 `:369-379`).
9. **The doors plan §4.6 rule is the bulk-set contract** — quoted verbatim from
   `docs/feature-abwab-doors/plan-slice-b.md:201-203`:
   > **Invariant:** every successful write refetches `GET api/abwab/tree` and rebinds
   > every cached version token from the fresh snapshot; selection rebinds **by id**,
   > dropping ids that vanished. Pinned by a test named for the failure it prevents (M16).
10. **Planning-artifact sweeps are deferred to one pass after Slice I** (standing user
    decision, §3). Same-change doc amendments are not.

### 4.2 Decided by this plan

1. **The bulk-archive fix is frontend-only — no backend contract change.** Verdict from
   §5's measured rows: the backend's bulk 404 is all-or-nothing by design
   (`EfAbwabDoorsWriter.cs:330-336` loads live rows only and throws on any mismatch) and
   its payload is the generic «الباب غير موجود» (`AbwabDoorsController.cs:168-169`,
   `ApiMessages.cs:127`) naming no door — but the frontend does not need it to: the
   snapshot already knows every door's name and archived state, and
   `bulkConflictMessage` (`abwab-write.controller.ts:253-259`) is the in-file precedent
   for naming doors from the snapshot on an all-or-nothing failure. Three layers:
   - **(a) `rebindTo` drops archived ids** — the root cause is that `byId` holds archived
     doors too (`abwab-tree.builder.ts:69` sets every node, archive tree included), so
     "dropping ids that vanished" (§4.6) currently drops nothing: doors are soft-deleted
     and never leave the snapshot. The drop test becomes `node && !node.isArchived` for
     the bulk set. The **single** selection keeps its current missing-only rule — an
     archived single selection is already cleared by the URL/store invariant and the
     archive-confirm flow, and bulk is where stale ids actually reach a write.
   - **(b) `currentBulkRefs()` filters at submit** — defense in depth: refs exclude ids
     the current snapshot shows archived/missing, so the confirm count
     (`bulkLiveSubtreeCount` already walks live-only, `:96-116`) and the submitted refs
     stop disagreeing.
   - **(c) On a bulk-archive/bulk-move 404**: refresh the snapshot, `rebindTo` (which now
     drops the archived ids), diff the *attempted* refs against the fresh snapshot, and
     announce a message that **names the dropped door(s)** on the counted-noun helper;
     generic `writeInvalidFallback` only if the diff finds nothing. Selection handling
     mirrors the 409 policy's spirit: still-valid picks survive (they were just rebound),
     only the vanished ids are gone.
2. **Flag-click wiring follows the select-then-act invariant.** New
   `relationsRequested = output<number>()` on the tree; the page handler writes
   `door=<id>` (`updateQueryParams(buildAbwabQueryParams({ door: id }))`), calls
   `selection.select(id, version)` synchronously, then `overlays.openRelations()` — the
   `runContextAction` shape (`abwab-page-overlays.controller.ts:327-339`), because
   `openRelations()` reads `selectedDoor()` and the URL subscription is async.
3. **The flag button keeps the roving-tabindex invariant**: real `<button>`,
   `[attr.tabindex]="-1"`, Arabic `aria-label` naming the door and the count on the
   counted-noun helper — the `.abwab-tree__act` pattern two elements away
   (`abwab-tree.component.html:73-92`). `--empty` modifier at zero: muted text +
   `--qd-border` hairline, **no accent tint** (accent means "has relations").
4. **Row metadata vocabulary = raw `qd-chip` class composition on static spans** for the
   count and the flag (informational variant: hairline + muted; flag's non-empty state
   keeps the accent tint as the "has relations" mark). Precedent: the relations count
   pill (`abwab-relations-modal.component.html:20-22`) and
   `word-type-filter.component.html`. The **order pill stays a control** with its
   click-to-edit affordance — it aligns to the chip metrics (size/radius/hairline) but is
   not converted to `qd-chip`, because the chip contract has no "editable number" variant
   and inventing one for a single consumer is a fork.
5. **Badges collapse by dropping, not combining**: below `$qd-bp-tablet-max` the two new
   badges (descendants, depth) are hidden; the direct-children count — the concept's own
   line — survives at every width. Written priority (README, same change):
   **name (only shrinkable, ellipsis) > order pill > actions > children count >
   descendants/depth badges > flag**. §17's detail-shell "Header priority" section is the
   cited reasoning precedent (audit `:511-519`). A combined `3 / 12 / د3` chip is the
   recorded alternative if execution finds dropping too lossy — choosing it is not a
   scope change.
6. **The two derivations live in the builder, memoized on the node**:
   `liveDescendantCount` and `maxRelativeDepth` computed during `build()`
   (`abwab-tree.builder.ts:47-71`) exactly like `liveChildCount` (`:65`) — pure, specced
   in `abwab-tree.builder.spec.ts`, never computed in the component (audit `:505-510`).
7. **The relation-name control is a `qd-chip` extension, not consumer markup.** The
   removable branch's wrapper is a static `<span>` (`chip.component.html:8-24`), so a
   nested label-button is valid HTML — the exact structure §17 recorded as the reason a
   removable chip could not be clickable *as a whole*. `qd-chip` gains an opt-in
   (`labelClickable` input + `labelClick` output, or equivalent) rendering the label as a
   nested `<button>` **only in the removable branch**; the five existing consumers are
   untouched (input defaults off). §17's `qd-chip` entry is amended in the same change
   ("extend the base, not fork it"). Consumer-side hand-rolled buttons inside
   `ng-content` were rejected: every future consumer would re-invent focus/hover styling.
8. **Reveal per-state rules — one `buildAbwabQueryParams` patch, stated per state:**
   - **Same scope, live door:** patch `{ door: id }`; ancestor chain (a `parentId` walk up
     `byId` — the cards-breadcrumb walk, README `:63-66`) feeds a new page-level
     `revealExpandedIds` signal unioned into the tree's `forceExpandedIds` binding;
     `scrollIntoView({ block: 'nearest' })` (`surah-jump-picker.component.ts:277`
     precedent); `.abwab-tree__row--revealed` for ~3s.
   - **Other section (or section-less door while a section tab is active):** same single
     patch adds `section: <target's own sectionId, or null>` — the explicit `door` in the
     same change overrides the invalidation clear (`abwab-url-sync.ts:44-47,65-75`), so
     this is one navigation, no race. The target's *own* tab, not the superset, because
     the reveal should land where the door lives.
   - **Cards view active:** patch adds `view: 'tree'`. The item is reveal-in-**tree**;
     revealing inside the cards drill would be a second, unasked-for behavior.
   - **Active search (`q`):** patch clears `q`. A reveal that leaves the target pruned by
     `pruneAbwabNodesToVisible` breaks the promise the click makes; clearing the filter is
     the only end-state with the row on screen. Recorded as this plan's call — cheap to
     revisit if the user prefers preserving the filter.
   - **Archived target: defensively unreachable, and guarded anyway.** The relations read
     hides any relation whose endpoint is archived (dormancy, `Reads/Abwab/README.md`),
     and the archive view offers no relations entry point (README `:422-427`) — so a
     rendered relation chip always names a live door. The handler still no-ops (with the
     announcer's generic message) if `byId` shows the id archived or missing: the guard
     costs two lines and turns an impossible state into a visible non-action instead of a
     silent broken reveal into a tree that cannot contain it.
   - The modal closes before navigating (it holds no URL state — Slice E's item, not
     touched here).
9. **The highlight is a class + tokens, not JS animation**: background/border derived
   from `--qd-selected-bg`/`--qd-border-accent`, a CSS transition that decays, a
   `setTimeout`-cleared signal on the page (~3s), and a `prefers-reduced-motion` block
   that renders it static for the same duration. Owes the §16.2/§17 note as the app's
   reveal-highlight rule — audit says items 13/18 will want it later.
10. **Expected test-count delta, stated in advance: net increase, roughly +15 to +30**
    across `abwab-tree.component.spec.ts` (blur-cancel, flag states/click, badges),
    `abwab-tree.builder.spec.ts` (two derivations), `abwab-selection.store.spec.ts`
    (archived-drop — the missing M24 case), `abwab-write.controller.spec.ts` (submit
    filter + 404 naming), `abwab-relations-modal.component.spec.ts` (label control →
    `revealRequested`), the page spec (reveal wiring), and the chip spec (label-control
    opt-in). Zero removals; the two e2e files gain/adjust cases, never lose flows.
11. **One light branch off `dev`: `ux-slice-d-tree`.** Per-phase commits, PR targets
    `dev`, never `main`. Not a `dev → main` candidate (the abwab routes are still `Open`;
    README `:10-13`).

## 5. The ground truth this plan is derived from

Read before executing; each row is a measured fact from `dev` at `a6601a1f`, not an
assumption.

| Fact | Where |
|---|---|
| Reorder commits on blur today: `(blur)="commitOrderEdit(node.id, $event.target)"`; Enter commits / Escape reverts in `onOrderKeydown`; `commitOrderEdit` guards `editingId() !== id`, so the blur that follows an Enter commit is already a no-op — the cancel-on-blur change is one handler swap plus that guard's mirror | `abwab-tree.component.html:41-59` (blur at `:50`), `.ts:187-207` |
| The reversal's recorded texts: README render-chain line says "Enter commits, Escape reverts" and is **silent on blur** (how the code drifted); M29 spec block; e2e Enter-commit/Escape-revert flows (no blur case exists yet) | `features/abwab/README.md:48-50`; `abwab-tree.component.spec.ts:222+`; `e2e/abwab-operations.e2e.ts:8-33` |
| Flag today: `@if (node.relationCount > 0)` around a static span; "A chip, not a control… (plan §7 T603)" comment; tinted-pill styling; README flag line and the "Zero dead controls" gotcha calling it "the one deliberate non-control" | `abwab-tree.component.html:65-69`; `.scss:89-90` (comment), `:91-98` (rule); README `:60-62`, `:416-421` |
| The row's three metadata spans are three vocabularies: order = bordered pill (`flex:none`, click target), count = bare muted text, flag = accent-tinted pill | `abwab-tree.component.scss:48-59`, `:77-81`, `:91-98` |
| Only direct children ship: `node.liveChildCount` rendered when `hasChildren`; computed in the builder at `:65`; `parentId`/`children`/`depth` all materialized — both new badges are pure functions of the built tree | `abwab-tree.component.html:62-64`; `abwab-tree.builder.ts:47-71` |
| `byId` holds **every** door, live and archived — `build()` runs for `archivedRoots` too and `byId.set` is unconditional. Consequence: `rebindTo`'s "vanished" test (`snapshot.byId.get(doorId)`) never fires in production — soft-deleted doors never leave the snapshot, so an archived door stays in the bulk set **with a freshly rebound version** | `abwab-tree.builder.ts:42-91` (set at `:69`); `abwab-selection.store.ts:85-105` |
| M24's spec constructs "vanished" by omitting the door from the snapshot DTO — a state the wire cannot produce. The archived-in-snapshot case is unpinned | `abwab-selection.store.spec.ts:99-127` |
| Live-bug mechanics, end to end: bulk-archive an ancestor+descendant pair (or any selection) → success → `rebindTo` keeps the now-archived ids → bulk bar still lists them → next bulk submit sends them → writer loads `DeletedAtUtc == null` rows only, count mismatch throws `AbwabNotFoundException` → 404 «الباب غير موجود», generic, all-or-nothing. Bulk-move shares the shape | `abwab-write.controller.ts:170-176,189-191`; `EfAbwabDoorsWriter.cs:322-336`; `AbwabDoorsController.cs:128-174`; `ApiMessages.cs:123-132` |
| The confirm message already disagrees with the submitted refs in that state: `bulkLiveSubtreeCount` skips archived nodes while `currentBulkRefs` sends them | `abwab-write.controller.ts:93-116` vs `:189-191` |
| Naming-doors-from-the-snapshot precedent on an all-or-nothing failure: `bulkConflictMessage` (the 409 path) | `abwab-write.controller.ts:241-259` |
| Relations-modal names are **not** controls today: group chips are `qd-chip [removable]`; the removable branch renders a static `<span>` wrapper + label span + remove `<button>`. The commissioning prompt's "9c made the names real controls" holds structurally (static wrapper ⇒ a nested name-button is valid HTML; the chip is the shared component), not literally — the reveal control itself is this slice's work, exactly as audit item 10's fix text says ("the door name needs its own nested control beside the remove button") | `abwab-relations-modal.component.html:50-58`; `chip.component.html:8-24`; audit `:349-356` |
| Reveal building blocks all exist: `forceExpandedIds` input unioned with manual toggles; page binds search auto-expand only; explicit `door` in a patch overrides the section/archive invalidation clear; select-then-act invariant and its `runContextAction` worked example; `scrollIntoView({block:'nearest'})` precedent | `abwab-tree.component.ts:51-52,67-69`; `abwab-page.component.ts:113,223,228`; `abwab-url-sync.ts:44-75`; README `:218-223`; `abwab-page-overlays.controller.ts:259-267,327-339`; `surah-jump-picker.component.ts:277` |
| No flash/reveal artifact exists anywhere; the tokens file's measured selected-vs-hover ladder and its "read as a brief flash" warning are the contrast budget | `styles/_tokens.scss:89-101` (warning at `:94`) |
| Truncation state per remaining site — local ellipsis to replace: tree `__name` (`scss:70-75`), archive-view `__name` (`scss:47-51`), template-tree `__name` (`scss:78-82`). No ellipsis at all: sections-modal `__name` (`scss:20-23`, `flex:1` only), templates-page `__item-name` (`scss:49-52`), side-panel `__active-name` (no rule), move-picker section/door rows, cards title/crumbs. `[title]` present at **zero** of these sites | name renders: `abwab-tree.component.html:61`; `abwab-archive-view.component.html:27`; `abwab-side-panel.component.html:6`; `abwab-move-picker.component.html:39,63`; `abwab-sections-modal.component.html:45`; `abwab-templates-page.component.html:43,105-106`; `abwab-template-tree.component.html:49`; `abwab-cards.component.html:12-23,45-48` |
| Checkbox state: tree checkbox = local `flex:none; accent-color` rule, no accessible name; cards checkbox = local absolute-position + accent rule, no size, no accessible name. §17 names both as Slice D's debt and warns adding the class without deleting the local rule is a silent no-op | `abwab-tree.component.html:19-27`, `.scss:100-103`; `abwab-cards.component.html:36-44`, `.scss:85-90`; `UI_STYLE_SYSTEM.md:941-957` |
| §17's truncation debt line assigns the page-surface name sites to Slice D; the flexible-with-ellipsis rule and the mandatory-`[title]` contract are already written — this slice composes, it does not legislate | `UI_STYLE_SYSTEM.md:1070-1108` (debt at `:1100-1105`) |
| Slice C perf-pass surface: six shells hosted as static siblings on the doors page (door modal `:264`, move picker `:273`, relations `:283`, sections `:296`, plus the templates page's two); every shell `@if (open())` + `cdkTrapFocus cdkTrapFocusAutoCapture` + queued explicit focus (`focusFirstField`/`focusSearch`); `qd-tabs` runs in the relations modal body; `qd-state [reserve]` boxes render per open | `abwab-page.component.html:264-303`; README `:261-281` |
| Relations modal only reads in door mode; bulk/anchor-pick add closes without re-reading — reveal from a chip therefore always follows a completed read | `abwab-relations-modal.component.ts` (Slice C, spec-pinned) |

## 6. Phases

### Phase 1 — Baseline and record (2 tasks)

- **T101** — Baseline on `dev`: full Vitest (`npm test`, fork cap preserved via the npm
  script) + `npm run build`; record file/test counts, timings, and the `dev` SHA into
  `docs/feature-ux-slice-d/evidence.md`. No CI exists (`TESTING_STRATEGY.md` §8); every
  later delta measures against this run only.
- **T102** — Record the slice in the root `CLAUDE.md` "Active Spec Kit Feature" section
  (read current content first, replace "None" — slug `ux-slice-d`, this plan, no
  `docs/feature-XXX` decision record beyond this folder). Create branch `ux-slice-d-tree`
  off `dev`. **Do not sweep any planning folder** (§3 standing decision).

### Phase 2 — Read-only performance pass over Slice C's modal work (2 tasks)

Commissioned input 1; ordered first among the work phases because its findings are
evidence the user may act on, and because it must observe the modals **before** this
slice touches the relations modal.

- **T201** — Profile/review, read-only, findings with severity into `evidence.md`:
  1. **Open-path cost**: `cdkTrapFocusAutoCapture` + the queued explicit focus
     (`focusFirstField()`/`focusSearch()`) — double-focus work, timing, forced layout on
     open; the `qd-tabs` roving-tabindex initialization inside the relations modal body.
  2. **Change-detection cost of the six shells**: all are static siblings evaluated on
     every page CD cycle (`abwab-page.component.html:264-303` + the templates page's
     two); confirm each shell component is `OnPush` and each `@if (open())` keeps the
     closed cost to guard-check-only; look for signal reads in the page template that
     re-evaluate per keystroke (e.g. search `q` typing) and drag modal bindings with them.
  3. **Per-open allocations**: picker `pickerRows` walks, `nodesById` rebuilds, and the
     relations modal's group derivation — are they `computed` (memoized) or
     recomputed-per-CD?
  Method: Vitest-free — browser DevTools performance recordings on `/abwab` with the dev
  backend, plus code reading. No code changes.
- **T202** — Verdict in `evidence.md`: each finding gets severity
  (blocker/major/minor/info) and a one-line proposed fix **not implemented**. Regardless
  of findings, the slice proceeds — fixes are the user's call (§2 Out). Only exception:
  a finding that Phase 8's reveal would make materially worse is flagged against the
  Phase 8 tasks so execution accounts for it.

### Phase 3 — Bulk-archive stale-id 404, frontend-only (3 tasks)

The live bug (commissioned input 2). Verdict locked in §4.2-1: frontend-only.
**Gate:** if execution finds the frontend genuinely cannot name the offending door(s)
from the snapshot (i.e. the diff in (c) is provably empty in the reproduced failure), the
backend payload would need per-door identification — that is a contract change: STOP this
phase, write up the reproduction, surface the decision. Do not change the backend
unilaterally.

- **T301** — `abwab-selection.store.ts`: `rebindTo` drops bulk ids whose fresh node is
  archived (`!node.isArchived`), per §4.6's "dropping ids that vanished" (quoted,
  §4.1-9). Spec first: extend M24 with the case its current construction cannot reach —
  a snapshot that **contains** the door as archived (build it through
  `buildAbwabTreeSnapshot` with `isArchived: true`, the production shape) — assert the id
  drops; keep the existing missing-id cases green. Amend the README's §4.6 gotcha
  (`:297-305`) in the same change: state that "vanished" includes archived-in-snapshot,
  and that `byId` retaining archived nodes is why the naive missing-only test was a
  silent no-op.
- **T302** — `abwab-write.controller.ts`: (b) `currentBulkRefs()` filters against the
  current snapshot (live nodes only); (c) `handleBulkFailure` on `invalid` (404) from
  bulk archive/move: refresh + rebind, diff attempted refs vs fresh snapshot, announce a
  new `ABWAB_LABELS` message naming the vanished door(s) — counted-noun forms per the
  README rule (`:409-412`), generic fallback if the diff is empty. Spec: submit-filter
  case; 404-names-doors case; 404-empty-diff-falls-back case; existing 409 cases
  untouched. Bulk-relations targets are healed by (a)+(b) upstream — note it, add no
  third path.
- **T303** — Reproduce and close in `evidence.md`: drive the original failure (bulk
  select ancestor+descendant → bulk archive → observe the set survive → second submit
  404s) against the dev backend **on the pre-fix SHA**, then the same steps post-fix
  (set empties, message names doors when forced via a second tab). Record ids, responses,
  announcer text. Extend `e2e/abwab-operations.e2e.ts`'s bulk-archive flow with the
  set-empties-after-success assertion if the flow doesn't already pin it.

### Phase 4 — ⟲ Item 12: Enter commits, blur cancels (2 tasks)

- **T401** — `abwab-tree.component.*`: `(blur)` → `cancelOrderEdit(node.id)` which only
  clears `editingId` (guarded on `editingId() === id` so the post-Enter blur stays a
  no-op — same guard shape as today's commit). Spec (M29 block): add blur-after-typing
  cancels (no `orderCommitted` emission, pill text restored); keep Enter-commit and
  Escape-revert green.
- **T402** — Amendments in the same change (appendix row 12): README render-chain line
  (`:48-50`) now names all three — "Enter commits; blur and Escape cancel" (silence on
  blur is what let the code drift); `e2e/abwab-operations.e2e.ts` gains a blur-cancel
  step in the reorder flow (fill 99 → click elsewhere → pill unchanged, no resequence).
  Run the amended e2e flow once as evidence (not a gate).

### Phase 5 — ⟲ Item 13: the علاقات flag, and the row vocabulary (2 tasks)

- **T501** — Flag reversal (appendix row 13): drop the `@if`; the flag renders on every
  row as a `<button>` with `[attr.tabindex]="-1"`, Arabic `aria-label` naming door +
  count (counted-noun helper), `--empty` modifier at zero (muted + `--qd-border`
  hairline, no tint), click emits new `relationsRequested`; page wires it per §4.2-2
  (write `door=`, select, `overlays.openRelations()`). Bulk mode: the flag stays visible
  but inert like the row actions are hidden — clicking must not fight `bulkToggled`;
  stopPropagation like the other row controls. Amend in the same change: README flag
  line (`:60-62`), the "Zero dead controls" gotcha (`:416-421` — the flag stops being
  "the one deliberate non-control"; restate the rule by what remains), the SCSS comment
  (`scss:89-90`). Spec: renders at zero (dimmed), renders with count, click emits with
  the right id, tabindex stays -1 (keyboard-controller spec untouched — verify its
  roving assertions still pass).
- **T502** — Row metadata vocabulary (item 13's formatting half, §4.2-4): count and flag
  compose `qd-chip` classes on static spans (informational = hairline + muted; flag
  non-empty keeps the accent tint); order pill aligns to the chip metrics but stays the
  click-to-edit control. Delete the superseded local rules
  (`abwab-tree.component.scss:77-81`, `:91-98` — replaced, not shadowed). Testids
  unchanged. Visual check in browser at both themes.

### Phase 6 — Item 14: the three badges within a written width budget (2 tasks)

- **T601** — Builder: `liveDescendantCount` + `maxRelativeDepth` computed in `build()`
  beside `liveChildCount` (§4.2-6), live-only (§4.1-4), typed on `AbwabNode`. Spec first
  in `abwab-tree.builder.spec.ts`: leaf (0/0), the user's worked example
  (child+grandchild+great-grandchild ⇒ depth 3), archived-subtree exclusion, and the
  deep-chain vs wide-fan disambiguation (depth ≠ count).
- **T602** — Tree row: render the three badges on the chip vocabulary from T502; the two
  new ones hidden below `$qd-bp-tablet-max` (§4.2-5); Arabic `aria-label`s pin the depth
  semantics (§4.1-6). Write the priority order into the README in the same change:
  name > order pill > actions > children count > descendants/depth badges > flag. Spec:
  badges render with builder values, collapse rule via the media class presence. Browser
  check at 1024px/1023px and at a long-Arabic-name row — the name ellipsizes before any
  badge clips (this is the budget's acceptance).

### Phase 7 — Items 15-applied + 16-applied: the name and checkbox sweeps (3 tasks)

§17 already legislates both (`:1070-1108`, `:923-959`); this phase only composes.
Every task deletes the local rule it supersedes — adding the class beside a surviving
local rule is the named silent no-op.

- **T701** — Truncation, tree-family sites: tree `__name` (compose `.qd-truncate` +
  `[title]`, delete `scss:70-75`; keep `flex: 1`, add the
  `--qd-name-min-inline-size` floor only if the badge row squeezes names to nothing —
  record which), archive-view `__name` (delete `scss:47-51`), template-tree `__name`
  (delete `scss:78-82`), side-panel `__active-name`, cards title + crumbs (the title
  span gains a truncating name span so the order chip and name stop sharing one text
  node; crumbs get `[title]`).
- **T702** — Truncation, list/modal sites the C sweep left: sections-modal `__name`
  (`scss:20-23` gains the full contract), templates-page `__item-name` (`scss:49-52`) +
  editor title (`html:105-106`), move-picker section rows (`html:39`) and door rows
  (`html:63`). `[title]` mandatory at every site (§4.1-7). Amend §17's truncation debt
  line (`:1100-1105`) to paid in the same change.
- **T703** — Checkboxes: tree (`html:19-27`) and cards (`html:36-44`) compose
  `.qd-checkbox`/`.qd-check-row`; both gain Arabic `aria-label`s naming the door
  (door-picker precedent); delete the local rules (`abwab-tree.component.scss:100-103`,
  `abwab-cards.component.scss:85-90` — cards keeps only its positional placement if the
  card layout needs it, never size/accent). Spec: accessible-name assertions in both
  component specs. Amend §17's checkbox debt line (`:941-949`) to paid.

### Phase 8 — Item 10: reveal-in-tree from the relations modal (4 tasks)

- **T801** — `qd-chip` label-control opt-in (§4.2-7): input + output, nested `<button>`
  label in the removable branch only, focus-visible ring, default off — five existing
  consumers render byte-identically (their specs prove it). Chip spec: opt-in renders a
  button, emits `labelClick`, default stays a span. §17 `qd-chip` entry amended in the
  same change.
- **T802** — Relations modal: group chips set the opt-in; `labelClick` →
  `revealRequested = output<number>()` carrying `relation.otherDoorId`; Arabic
  `aria-label` on the name control («إظهار في الشجرة …»). Modal spec: click emits the
  right id; remove still emits `remove` independently (two controls, one chip).
- **T803** — Page mechanism (§4.2-8, all five states): overlay handler closes the modal,
  guards archived/missing via `byId` (defensive no-op + announcer), builds the single
  query patch (`door` always; `section` when the target's differs from active; `view:
  'tree'` when cards; `q: null` when filtering), sets `revealExpandedIds` (ancestor
  `parentId` walk) unioned into the tree's `forceExpandedIds` binding, then scroll +
  `.abwab-tree__row--revealed` (~3s signal; class from §4.2-9's tokens; static under
  `prefers-reduced-motion`). Page/overlays spec: per-state patch contents (same-scope,
  cross-section, cards, search, archived-guard), expand-chain contents, highlight signal
  clears.
- **T804** — The pattern's record, same change: §16.2/§17 note — the app's
  reveal-highlight rule (derivation tokens, the above-hover contrast requirement citing
  `_tokens.scss:94`, the reduced-motion rule, ~3s decay); README render-chain +
  URL-contract notes (reveal writes existing keys only — the contract gains **no** new
  key); browser acceptance: reveal across all reachable states, screenshot into
  `evidence.md`, contrast of the highlight vs hover checked against the `:89-101` ladder
  in both themes.

### Phase 9 — Verification and doc integrity (4 tasks)

- **T901** — Tier B against T101: full Vitest + `npm run build`. Tier B, not A, because
  `shared/ui/chip` changed (Tier B trigger: `shared/`). Expected delta per §4.2-10:
  net +15–30 tests, zero removed, +0 spec files (every touched surface already has one)
  — any other delta explained or fixed before proceeding. No backend change ⇒ no
  `dotnet test`, no route-smoke tier.
- **T902** — Browser acceptance matrix into `evidence.md`: blur-cancel vs Enter-commit vs
  Escape; flag at zero/nonzero, click opens the modal on the right door, roving tabindex
  unaffected (Tab count per row unchanged); badges at desktop/tablet widths; checkbox
  names announced (AT spot-check or DOM assertion); reveal all-states pass from T804.
  Run the five abwab e2e specs once (their own single-worker project, `e2e/README.md`) as
  extraction-style evidence — not a tier, not a gate.
- **T903** — Docs true again, cross-checked as a set (each edit already landed with its
  phase; this task verifies none was missed): README render-chain (blur, flag control,
  badges, reveal), flag line, "Zero dead controls" gotcha, §4.6 gotcha, row-priority
  paragraph; `UI_STYLE_SYSTEM.md` §17 checkbox + truncation debt lines paid, `qd-chip`
  entry extended, reveal-highlight note present; the `scss:89-90` comment gone with its
  rule; TESTING_DEBT untouched (nothing here pays or creates a row — state that in
  evidence rather than editing the file).
- **T904** — Close-out sweep: `grep -rn` the repo (prose included) for every deleted
  selector/rule/comment — the old `__flag` non-control comment, the local ellipsis rules,
  the local checkbox rules, `commitOrderEdit`-on-blur references — fix any dangling
  reference. Final `evidence.md` entry: baseline vs closing numbers, the perf-pass
  verdict, the bulk-archive before/after reproduction, acceptance artifacts. The
  Active-Feature record clears at merge, not before (Slice C's `chore` precedent).

## 7. Testing posture

- Per-phase: Tier A focused globs
  (`npm test -- --include="src/app/features/abwab/**/*.spec.ts"`, narrower where the
  phase touches one component; the chip work adds
  `--include="src/app/shared/ui/chip/**"`), fork cap preserved
  (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` via the npm script). Suites + build stay green
  at every commit.
- Spec/e2e amendments for the two reversals are **mandated same-change updates**
  (appendix rows 12/13), not new-test-posture violations; the bulk-archive and reveal
  specs extend existing suites. Behavior-first per test-guard: real DTOs through
  `buildAbwabTreeSnapshot`, function-input harnesses where the components already use
  them, TDZ label getters, Arabic counted-noun forms asserted where new copy lands.
- Pre-PR: Tier C = T901's Tier B + `npm run build` (frontend-only slice; no backend
  commands, no `SmokeRouteCatalog`).
- Browser passes (T303, T602, T804, T902) and the e2e single-worker run are
  extraction-style evidence — never cited in place of the Vitest suite or the build.
- Expected count delta declared in §4.2-10 and checked in T901.

## 8. Risk register

| Risk | Why it is real | Mitigation |
|---|---|---|
| Blur-cancel breaks the Enter path via handler ordering | Enter → commit → input unmounts → blur fires on the dead input | The existing `editingId() !== id` guard already makes post-commit blur a no-op; T401 keeps the same guard shape for cancel; spec pins Enter-then-blur emits exactly once |
| Flag button breaks the roving-tabindex invariant | A new focusable per row would double the tree's tab stops | `tabindex="-1"` per the `.abwab-tree__act` pattern; keyboard-controller spec + T902's Tab-count check |
| Flag click fights bulk row-toggle | Row click in bulk mode toggles selection; the flag sits inside the row | `stopPropagation` like every other row control; bulk-mode behavior pinned in the tree spec |
| Composing chip/checkbox/truncate classes without deleting local rules → silent no-op | §17 names this trap twice (`:950-957`, modal entry) | Every sweep task pairs compose-with-delete; T904 greps for the dead selectors |
| Badges overflow the row at real Arabic name lengths | Three badges where one was; 36rem-ish column; RTL | §4.2-5's written priority + tablet drop; T602's long-name acceptance; name keeps `flex:1` + ellipsis as the only shrinkable |
| `rebindTo` dropping archived ids surprises a flow that relied on set survival | Bulk 409 policy deliberately preserves the attempted selection | 409 path is untouched — the drop happens on **successful** rebind and on the new 404 recovery only; write-controller spec pins 409 preservation unchanged |
| Bulk 404 diff finds nothing (message can't name a door) | Snapshot could theoretically refresh past the offender | Gate in Phase 3 preamble: that outcome stops the phase and surfaces the backend-contract decision; generic fallback keeps the UI honest meanwhile |
| Reveal navigation races the snapshot/URL subscriptions | `door=` restore is deliberately deferred until both URL and snapshot settle (`abwab-page.component.ts:155-157`) | Single query patch (no sequenced navigations); expand/scroll/highlight keyed off the same param emission the page already uses; per-state page spec |
| Reveal highlight invisible (below hover) or garish (new hue) | `_tokens.scss:94`'s recorded failure, both directions | §4.2-9 derives from §16.1 tokens; T804 checks against the measured ladder in both themes |
| Chip extension ripples into five existing consumers | `qd-chip` is shared | Opt-in input defaulting off; consumers' existing specs are the regression net; Tier B run in T901 |
| Perf pass mutates what it measures | Same slice later edits the relations modal | Phase 2 is strictly read-only and runs before any Phase 5–8 change; findings recorded, fixes deferred to user acceptance |

## 9. Obligations checklist (all must be true at close)

- [ ] Baseline recorded (T101) before any change; closing Tier B compared against it (T901)
- [ ] Perf findings in `evidence.md` with severities; zero perf fixes implemented
- [ ] Bulk-archive: §4.6 quote in plan honored — archived ids drop at rebind, submit filters, 404 names the door(s) or falls back honestly; README §4.6 gotcha amended
- [ ] Phase 3 gate respected: any backend-contract need stopped the phase and was surfaced
- [ ] ⟲ 12: blur cancels; README line names blur; M29 + e2e amended in the same change
- [ ] ⟲ 13: flag always visible, dimmed at zero, clickable; README flag line + "Zero dead controls" gotcha + `scss:89-90` comment all amended in the same change; archive view/cards untouched
- [ ] Badges live-only; depth semantics pinned in Arabic copy; row priority written in README; tablet collapse in force
- [ ] Every remaining abwab name-render site composes `.qd-truncate` with `[title]`; every superseded local rule deleted; §17 truncation debt line paid
- [ ] Tree + cards checkboxes compose `.qd-checkbox`/`.qd-check-row` with accessible names; §17 checkbox debt line paid
- [ ] Reveal: five per-state rules implemented as specified; no new URL key; highlight §16-derived, above-hover, reduced-motion-safe; §16.2/§17 note written
- [ ] `qd-chip` extension opt-in; five existing consumers byte-identical by their specs
- [ ] Test delta within §4.2-10's declared direction; zero tests removed
- [ ] Fork cap preserved on every run; no e2e cited as a gate
- [ ] No planning folder deleted or repointed (standing decision)
- [ ] PR targets `dev`; no `dev → main`

## 10. Execution note

One light branch off `dev`: `ux-slice-d-tree` (§4.2-11). Commits per task or tight
task-pair, phases in order — the ordering is the discipline (perf observation before
mutation; the live bug before the surface work it shares files with; primitives before
their consumers in Phase 8).

| Phase | Title | Items | Tasks |
|---|---|---|---|
| 1 | Baseline and record | — | T101–T102 (2) |
| 2 | Read-only perf pass over Slice C modals | input 1 | T201–T202 (2) |
| 3 | Bulk-archive stale-id 404 | input 2 | T301–T303 (3) |
| 4 | ⟲ Reorder: blur cancels | 12 | T401–T402 (2) |
| 5 | ⟲ Relations flag + row vocabulary | 13 | T501–T502 (2) |
| 6 | Three badges + width budget | 14 | T601–T602 (2) |
| 7 | Name + checkbox sweeps | 15-applied, 16-applied | T701–T703 (3) |
| 8 | Reveal-in-tree | 10 | T801–T804 (4) |
| 9 | Verification and doc integrity | — | T901–T904 (4) |

**24 tasks. Guard: under 30 — one slice, no split** (seam recorded in §0 in case
execution learns otherwise).
