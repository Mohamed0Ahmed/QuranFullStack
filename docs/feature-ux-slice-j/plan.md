# Plan — Abwab UX slice J: polish (modal `--wide`, confirm retrofit, badge header, tracking panel)

- **Status:** planned, not started. Normal implementation plan, NOT Spec Kit. Frontend-only:
  no API contract, no migration, no route-smoke gate.
- **Base branch:** `dev`; feature branch off `dev`, PR into `dev`.
- **Path rationale:** siblings `docs/feature-ux-slice-a/` … `docs/feature-ux-slice-i/` each
  hold a `plan.md`; this is slice J of the same series, hence `docs/feature-ux-slice-j/plan.md`.
- **Planning basis:** the two-part inspection of 2026-08-02 (post-PR-#59 tree). Every
  file:line below re-verified against the working tree on that date.
- **Locked decisions:** J1 (modal `--wide` 52rem), J6 (one confirm retrofit pass + ride-alongs
  A3-a/A2-a), J8 (badge header row), J9 (tracking-panel deletion) — as given; interpretation
  points marked.

## 0. Non-goals (locked) and recorded follow-ups

Out of scope: relations-with-the-tree, the relations loading state, the picker's
flat-children defect (slice K); search highlight-instead-of-filter (slice L); any backend
change — including **F4** (`ToListAsync().ToHashSet()` → `ToHashSetAsync`,
`EfAbwabTreeReader.cs:44-48`, info, recorded not done); widening `--wide` to the five words
explorer-detail modals (the documented 42rem holdout, `UI_STYLE_SYSTEM.md` §17 — its
convergence trigger is "the next change that touches any of the five words detail modals'
geometry", which this slice does not); closed-feature planning-doc cleanup (separate pass).

**Named follow-up candidate — backend tree-read perf (record only, evidence from the
inspection):**
- **F1 (medium, pre-existing):** `GetSnapshotVersionAsync` issues 3 sequential `MaxAsync`
  round trips for one scalar (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/EfAbwabTreeReader.cs:99-107`)
  — 3 of the 8 cache-miss queries. Candidate fix: one `UNION ALL` via `Concat` (in-repo
  precedent `EfMushafPageReader.cs:140-155`); never `Task.WhenAll` (single DbContext throws).
- **F2 (medium, pre-existing):** no single-flight on the tree cache miss, and the
  generation-derived ETag invalidates every client simultaneously on any write
  (`CachedAbwabTreeReader.cs:29-35`; refusal comment `:9-10` covers staleness, not
  concurrency) — N admins ⇒ N×8-query burst.
- **B7-a (low, pre-existing, belongs to slice L):** the search walk allocates
  `[...ancestors, node]` per tree edge (`abwab-tree.builder.ts:172`); slice L rewrites that
  walk anyway.

All frontend paths below are relative to `Frontend/quran-dashboard-ui/`; abwab paths to
`src/app/features/abwab/`.

## 1. Verified anchors

| Fact | Where (re-verified 2026-08-02) |
|---|---|
| `.qd-modal` width literal | `src/styles/_components.scss:598` `width: min(100%, 36rem)`; `--fixed` block-size-only `:607-610` |
| No width modifier exists; no test pins any modal width | grep-verified (inspection §A, §F) |
| Confirm dialog width + hardcoded testids | `shared/ui/confirm-dialog/confirm-dialog.component.scss:2` (`min(28rem, 100%)`); testids at `confirm-dialog.component.html:2, 12, 32, 42`; inputs `component.ts:29-37` |
| Rotted comment pointer | `src/styles/_tokens.scss:160-163` (cites `abwab-template-copy-modal.component.scss:75-76`; that file is 36 lines now) |
| Archive confirms (inline cards, sticky aside) | `pages/abwab-page/abwab-page.component.html:215-270`; T403 slot SCSS `abwab-page.component.scss:86-105` |
| Archive confirm state | `state/abwab-page-overlays.controller.ts:102` (`archiveConfirming`), `:134` (`bulkArchiveConfirming`); close-before-request at `:117` and `:147` |
| Sections delete (no confirm) | `components/abwab-sections-modal/abwab-sections-modal.component.ts:207-211` (`remove()`); button `component.html:79`; error line `component.html:22`; label `deleteSectionButton: 'حذف'` (`abwab.labels.ts:198`) |
| Templates-page confirms | `pages/abwab-templates-page/abwab-templates-page.component.html:148-153` (`abwab-templates-page-delete-template-confirm`), `:177-182` (`...-delete-node-confirm`); state `component.ts:74` (`confirmingTemplateDelete`) + the `deletingName`-keyed node confirm; dispatch `component.ts:276`, `:295` |
| Existing confirm bodies | `abwab.labels.ts:351` (`templateDeleteConfirm`), `:349-350` (`templateNodeDeleteConfirm(nodeName)`), `:223` (`archiveConfirm(count)` counted-noun), `:148` (`archiveOp`), `:170` (`cancelButton`), `:348` (`templateNodeDeleteOp: 'حذف العنصر'`) |
| Never-stack / unconditional-trap rule | `features/abwab/README.md:450-453` — "abwab modals never stack — under the entity-detail overlay or each other. **A future change that makes them nest must revisit this.**" |
| Restore-modal busy precedent | `components/abwab-door-restore-modal/abwab-door-restore-modal.component.ts:43` (`busy = signal(false)`), `:117` (set before `restoreDoor`) |
| Ride-along sites | `abwab-page-overlays.controller.ts:241-254` (`restoreAncestors`, fresh `[]` per rebuild); `abwab-door-restore-modal.component.ts:89-99` (effect tracks node object identity); `NO_IDS`/`NO_ROOTS` precedent `abwab-page.component.ts:61-67`; move-picker guard comment `abwab-move-picker.component.ts:109-112` |
| Badges | `components/abwab-tree/abwab-tree.component.html:64-83` (three spans, gated `row.hasChildren`); sizing `_components.scss:279-291` (`.qd-chip__count`, 3 other consumers) + local `abwab-tree.component.scss:118-123`; tablet drop `:125-135` (`--wide` pair only) |
| «ع» prefix | `models/abwab.labels.ts:138` `rowDepthBadge: (depth) => \`ع${depth}\``; sole template consumer `abwab-tree.component.ts:170-172` |
| Badge specs | `abwab-tree.component.spec.ts:458` (asserts via `rowDepthBadge` — «ع» not literal-pinned), `:469`, `:480`, `:499-505` (the 2-of-3 tablet-drop pin) |
| Tracking panel | `components/abwab-door-modal/abwab-door-modal.component.html:54-61` (`@if (isEdit)` only — **already absent in add mode**); getters `component.ts:113-119`; constants `abwab.labels.ts:179-185`; SCSS `component.scss:6-30`; doc comments `component.ts:18`, `:29-34`; specs `component.spec.ts:272-280`, `:282-287` |
| E2E pins | `e2e/abwab-archive.e2e.ts:32, 34`; `e2e/abwab-operations.e2e.ts:108, 109`; `e2e/abwab-structure.e2e.ts:29-34, 54+` (sections delete flows); templates confirms have **no** unit spec (no `abwab-templates-page` spec file exists) and no e2e pin |
| §17 lines | `qd-confirm-dialog` entry `UI_STYLE_SYSTEM.md:751-778` (retrofit list `:776-778` omits the archive confirms); `.qd-modal` entry `:1008-1073` (base "must keep composing unmodified" `:1010-1013`); `.qd-detail-list` entry `:865-875` (header-row mixin undocumented — 10 consumers of `.qd-detail-list__header`, `_explorer-detail-lists.scss:13-28`) |
| TESTING_DEBT rows to pay | `docs/TESTING_DEBT.md:150` (J2), `:151` (J3) |

## 2. Design

### 2.1 J1 — `.qd-modal--wide` at 52rem

- One rule, placed **immediately after `.qd-modal`** in `src/styles/_components.scss`:
  `.qd-modal--wide { width: min(100%, 52rem); }`. One property. **No token**: the file's own
  convention keeps every modal geometry a literal rem on the class (28/36/42/44/46rem are all
  literals); a lone width token would be the odd one out.
- **Why 52rem and not the existing 46rem step:** §17 documents resistance to new geometry
  (the `--fixed` height deliberately reused 44rem, `:1013-1018`, `:1052-1055`). 46rem is only
  +160px over the base — measured against the relations modal's two-pane content it
  under-delivers the point of the variant. 52rem = 832px fits inside the 992px available at
  the 1024px minimum desktop with ~80px gutters per side (vs the rejected literal-double
  72rem, full-bleed below 1184px). The §17 entry therefore introduces 52rem **as the single
  sanctioned wide step** and extends the ladder to 28/36/42(holdout)/44–46/52, explicitly
  forbidding further ad-hoc widths — the doctrine is preserved by making the new step part of
  the documented system rather than a per-modal cap.
- Adopters (exactly three, class-list edits at line 4 of each template):
  `abwab-relations-modal`, `abwab-move-picker`, `abwab-template-copy-modal`. Nothing else.
- **Confirm dialog and detail shell stay untouched automatically — verified, not asserted:**
  `.qd-confirm-dialog` (`confirm-dialog.component.scss:2`) and `.detail-modal-shell`
  (`detail-modal-shell.component.scss:14`) are `(0,2,0)` under Angular emulated
  encapsulation and outrank the `(0,1,0)` modifier; neither template gains the class either.
  The five `.qd-modal.explorer-detail-modal` consumers are likewise `(0,2,0)` and inert.
  Re-verified in this session against the three SCSS files; acceptance criterion 3 re-checks
  it in-browser.
- Doc obligations (same commit): §17 entry for `--wide` that **explicitly addresses**
  `:1010-1013` ("base … must keep composing unmodified" — the base is untouched; `--wide` is
  the width sibling of `--fixed`, and the sentence gains "width variants opt in via
  `--wide`, never by editing the base"); `src/styles/README.md:50-53` gains the sibling
  sentence naming `--wide`; the rotted pointer at `_tokens.scss:160-163` is repointed to the
  current `abwab-template-copy-modal.component.scss` line or reworded to drop the dead
  file:line.

### 2.2 J6 — the confirm retrofit pass

**Five confirms migrate** onto `qd-confirm-dialog` with `tone: 'danger'`: single archive,
bulk archive (both in `abwab-page.component.html`), sections-modal delete (TESTING_DEBT J2),
templates-page template-delete and node-delete (TESTING_DEBT J3). No wrapper components —
each site inlines `<qd-confirm-dialog>` with a projected body (the restore modal proved the
pattern; wrappers added nothing here).

**Primitive change — test-id prefix input:** `readonly testIdPrefix = input('qd-confirm-dialog')`;
the four hardcoded testids (`confirm-dialog.component.html:2, 12, 32, 42`) become
`{prefix}-backdrop`, `{prefix}`, `{prefix}-confirm`, `{prefix}-cancel`. Default preserves
the restore modal and the primitive's own spec unchanged. §17 entry updated (input list +
one sentence on when to pass it: whenever a page can host more than one confirm). Prefixes
used: `abwab-page-archive-confirm`, `abwab-page-bulk-archive-confirm`,
`abwab-sections-modal-delete-confirm`, `abwab-templates-page-delete-template-confirm`,
`abwab-templates-page-delete-node-confirm` — the two archive **container** ids are preserved
verbatim, so container assertions survive; button ids shift `-yes/-no` → `-confirm/-cancel`
(enumerated spec/e2e edits in phase 3).

**`busy` is wired — the UX change, stated plainly:** today every one of these confirms
closes **before** the request fires (`abwab-page-overlays.controller.ts:117`, `:147`;
sections `remove()` fires immediately; templates dispatch at `:276`, `:295`). After this
slice each dialog **stays open with both buttons disabled until the write resolves**;
`Escape` and backdrop dismissal are suppressed while busy (primitive contract). Success
closes the dialog. Failure keeps it open and shows the outcome's Arabic message inside the
projected body via `qd-state variant="error"` (restore-modal precedent); retry = press
confirm again; cancel remains available once busy clears. Concretely per site:
- Archive single/bulk: busy lives in the overlays controller; on failure the dialog shows
  the outcome message with testid `abwab-page-archive-confirm-error` /
  `...-bulk-archive-confirm-error`.

**Single error owner while a confirm dialog is open — decided: the dialog.** For a write
dispatched from an open confirm dialog, the failure message renders ONLY inside that
dialog's projected `qd-state` line; the subscriber consumes the outcome and writes no
page-level error state for that write (the global surfaces — announcer, page/modal error
lines — stay untouched by it). The reverse (global owns, dialog silent) would leave the
still-open dialog mute about why nothing happened. Assertion (task 3.10): a failed
single archive renders **exactly one** visible error element — the in-dialog one — and the
page-level error surface stays empty.

**Focus return, archive confirms — specified per entry point.** The primitive's
`cdkTrapFocusAutoCapture` restores focus to the previously-focused element on close; that
is sufficient only when the trigger still exists and is focusable. Per entry point:
- **Side-panel op** (`abwab-side-panel-op-archive`): cancel → the op button (selection
  survives cancel; auto-restore suffices). Success → the trigger survives but the
  selection is cleared and the button becomes disabled, so auto-restore would drop focus —
  the page explicitly moves focus to the tree's roving-focus item
  (`rovingId`, `abwab-tree.component.ts:117`; focus helper `:283`).
- **Row context menu** (`abwab-page-ctx-archive`): the menu closes when the dialog opens,
  so the trigger element is gone in BOTH outcomes. Cancel → focus the targeted row (it
  still exists and is the roving item — the ctx action selected it). Success → the row is
  gone → the tree's roving-focus item (the next surviving row; the tree container when no
  rows remain).
- **Bulk bar** (`abwab-side-panel-bulk-archive`): cancel → the bulk-archive button
  (bulk bar survives; auto-restore suffices). Success → the bulk selection clears and the
  bar unmounts → the tree's roving-focus item.
The rule, stated once: **on dialog close, if the auto-restored target no longer exists or
is not focusable, the page focuses the tree's roving-focus item, falling back to the tree
container.** Assertions (task 3.10): one spec case per entry point (three cancel cases,
plus one success case asserting roving-item focus after the row disappears).

**URL contract — explicit, not implicit.** Slice E's `modal` key carries six kinds;
**confirmation dialogs are deliberately outside that contract and never URL-addressable**
— a destructive confirm must be re-initiated, never restored from a URL (the existing
doctrine at `features/abwab/README.md:317-321`, pinned by
`abwab-page.component.spec.ts:904-916`). This plan extends the statement to every migrated
confirm: the archive confirms, the sections-delete confirm (the sections modal itself is
`modal=sections`; its nested confirm adds nothing to the key), and the templates-page
confirms (that page has no `modal` key at all) all leave the URL untouched on open, close,
and success. The §17 `qd-confirm-dialog` entry (task 3.1) gains the sentence: *"Confirm
dialogs are transient and never URL-addressable — no consumer may write a URL key for
one."* Assertions: the existing `:904-916` case continues; task 3.10 adds one case —
opening and cancelling the sections-delete confirm leaves the URL unchanged.
- Sections delete: the 409 «لا يمكن حذف القسم لاحتوائه على أبواب حالية» now renders inside
  the dialog (`abwab-sections-modal-delete-error`), not the modal-level error line; the
  modal-level `qd-state` (`abwab-sections-modal.component.html:22`) remains for
  create/rename failures. E2E `abwab-structure.e2e.ts` follows (phase 3).
- Templates: failure message inside each dialog (`...-delete-template-confirm-error`,
  `...-delete-node-confirm-error`).
- No loading/empty states exist for these dialogs beyond busy (their content is static
  copy); no retry affordance beyond re-confirm.

**Mutual exclusion — decided: yes, state-enforced.** The two archive booleans become one
signal `archiveConfirm = signal<'single' | 'bulk' | null>(null)` in the overlays
controller; `requestArchive`/`requestBulkArchive` write it, both cancel paths null it, and
two backdropped dialogs can no longer coexist by construction. (As inline cards the overlap
was cosmetic; as `aria-modal` dialogs with focus traps it would be a defect.)

**Stacking rule revisit (README:450-453) — required, pre-authorized:** the sections-delete
confirm nests a dialog over the open sections modal, which the README's unconditional-trap
rule anticipates ("a future change that makes them nest must revisit this"). Resolution:
the sections modal's `cdkTrapFocus` becomes conditional — active only while its delete
confirm is closed (the words dialogs' `drawerTrapEnabled` precedent named in the same
paragraph); the confirm dialog's own trap takes over while open, and `cdkTrapFocusAutoCapture`
restores focus to the delete button on close. The README paragraph is rewritten in the same
commit to scope the rule: authoring modals never stack **with each other**; a confirmation
dialog may nest above exactly one authoring modal, and the host trap yields while it does.
The templates-page confirms nest over nothing (page-level) and need no gating.

**Also in this phase:** delete `.abwab-page__archive-confirm-slot` markup + its T403 SCSS
block (`abwab-page.component.scss:86-105`) — nothing pins it; correct §17's retrofit list
(`:776-778`) which omits the two archive confirms it is retrofitting; **remove TESTING_DEBT
rows J2 and J3 outright** — both are paid in full by this pass (J2's subject is the missing
confirmation, J3's the hand-rolled markup; templates-page *workshop* test coverage is row
9's business, not J3's, so nothing is left to narrow).

**Ride-alongs (explicit tasks, PR #59-owned, same files):**
- **A3-a:** module-scope `const NO_ANCESTORS: readonly AbwabNode[] = []` in
  `abwab-page-overlays.controller.ts`; `restoreAncestors` early-returns it while
  `restoreTarget()` is null (mirrors `NO_IDS`/`NO_ROOTS`, `abwab-page.component.ts:61-67`).
  Assertion: `abwab-page.component.spec.ts` new case — with no restore target, two
  snapshot rebuilds yield the **same** `overlays.restoreAncestors()` instance (`toBe`).
- **A2-a:** `abwab-door-restore-modal.component.ts` gains
  `private readonly doorSubjectId = computed(() => this.door()?.id ?? null)`; the reset
  effect (`:89-99`) tracks it instead of the node object, reading `door()` inside
  `untracked` (the `abwab-move-picker.component.ts:109-112` guard, applied). Assertion:
  `abwab-door-restore-modal.component.spec.ts` new case — replacing the `door` input with a
  **new object of the same id** does not wipe a chosen section; replacing with a different
  id does reset.

### 2.3 J8 — badge header row

- **Structure (the restructure, named):** the three badges wrap in a fixed-grid group
  `.abwab-tree__counts` — `display: grid; grid-template-columns: repeat(3, var width)` with
  one local width (a literal rem in the component SCSS, sized to the widest of the three
  header words at the row font; expected ≈3.5rem — measured at implementation, single
  source via one SCSS variable used by rows AND header). The group renders on **every** row
  (leaf rows render it empty), so columns exist row-over-row. `.abwab-tree__count` gains a
  fixed `inline-size: 100%` within its cell + `justify-self: center`. **The global
  `.qd-chip__count` is not touched** — it has 3 other consumers (`chip.component.html:21`,
  `word-type-filter.component.html:30, 58`).
- **Header row:** a sibling `<div class="abwab-tree__header" aria-hidden="true">` rendered
  by `abwab-tree.component.html` **before** the `role="tree"` element (component template
  gets two root nodes; the header never enters the tree's ARIA subtree). It reuses the same
  grid template, right-aligned against the row's badge-group position (RTL: labels read
  «مباشرين، الجميع، العمق» right-to-left, matching the DOM order children→descendants→depth).
  Gated on the tree having at least one branch row? No — rendered whenever the tree renders
  rows; with zero branch rows every group is empty and the header still labels the columns.
  **Alignment is structural (shared grid), not approximate. Stop condition:** if
  implementation cannot make header cells and row cells share one grid template (e.g. the
  actions/flag cluster forces divergent row layouts), STOP and report — do not ship
  eyeballed alignment.
- **Labels (net-new):** `rowHeaderDirect: 'مباشرين'`, `rowHeaderTotal: 'الجميع'`,
  `rowHeaderDepth: 'العمق'` in `abwab.labels.ts`, read via TDZ getters.
- **«ع» removal:** `rowDepthBadge` body becomes `` `${depth}` `` (function and name stay —
  spec `:458` asserts via the function and stays green); its comment (`:137` "a bare
  numeral would read as a fourth count") is rewritten: the header now disambiguates.
- **Accessible layer unchanged:** the header is presentational (`aria-hidden="true"`); the
  per-badge Arabic `aria-label`s (`abwab.labels.ts:129-136`) remain the meaning carrier —
  the `qd-tabs` count-meta precedent (visible digits `aria-hidden`, meaning in the label).
  The README comment "the accessible name is the only place its meaning exists"
  (README badge paragraph, ~L104-115) is corrected to "the accessible layer; the visible
  header is presentational."
- **Responsive:** the existing `≤ $qd-bp-tablet-max` rule hides the two `--wide` badges
  (`abwab-tree.component.scss:131-135`). The header's «الجميع» and «العمق» cells take the
  same `--wide` class and drop in the **same** media query; «مباشرين» survives at every
  width — matching the pin at `abwab-tree.component.spec.ts:499-505`, which is untouched.
  The grid template collapses to one column in the same query.
- **Tree-only by structure:** cards show one plain number (`abwab-cards.component.html:52-54`),
  the archive view none (deliberate, README) — neither gets a header.
- **Loading and empty states — decided: the header renders only once real rows exist.**
  The page owns the skeleton and empty branches (`abwab-page.component.html` tree card:
  `qd-skeleton-rows` in the loading branch, `qd-state variant="empty"` in the empty
  branch); `<qd-abwab-tree>` — and therefore the header inside it — mounts only in the
  populated branch. So: no header over skeleton rows, no header over the empty state; the
  header appears in the same single repaint that swaps the skeleton for the tree. The
  slice-B1 skeleton matches row *pitch*, not total block height, so the one-header-row
  delta at swap introduces no new doctrine violation — recorded here as accepted, not
  discovered later. Assertions (task 4.5): header absent while the page shows the tree
  skeleton; header absent in the empty state; header present once rows render.
- **§17:** new entry for the header-over-badge-columns pattern (placement outside
  `role="tree"`, aria-hidden doctrine, the 2-of-3 drop). The `.qd-detail-list__header`
  documentation gap (10 consumers, `_explorer-detail-lists.scss:13-28`, entry `:865-875`
  silent) is a **different entry in a different section of the file** — not genuinely one
  edit; it is logged in TESTING_DEBT-adjacent terms instead: one line added to the §17
  `.qd-detail-list` entry's own TODO is out of scope, so the plan records it here as a
  known doc gap and leaves it.

### 2.4 J9 — tracking-panel deletion

Premise honored: the panel is already absent in add mode; the work is deleting the
`@if (isEdit)` branch. Delete outright (git history is the archive): markup
(`abwab-door-modal.component.html:54-61`), 7 getters (`component.ts:113-119`), 7 constants
(`abwab.labels.ts:179-185`), 4 SCSS blocks (`component.scss:6-30`), the two doc comments
(`component.ts:18` clause + `:29-34` block). No `false` gate. `isEdit` itself survives
(title + section logic). The modal does **not** shrink — `--fixed`'s zero-resize trade
stands; no height work. Spec surgery: `:272-280` splits — the prefill assertions (`:276-278`)
stay as a renamed case, the `:279` box assertion is deleted; the `:282-287` create-mode case
is deleted (vacuous once the box is gone). README same-change edits: L184-187 (shell
inventory sentence drops the box), L199-203 (the template-node-modal justification loses
its box comparison — rewritten to stand alone), L587-591 (the no-audit-columns gotcha keeps
its first half, loses the box sentence).

## 3. Phases

### Phase 1 — J9 tracking-panel deletion (tiny, independent)

| # | Task | Files |
|---|---|---|
| 1.1 | Delete the `@if (isEdit)` panel branch | `components/abwab-door-modal/abwab-door-modal.component.html:54-61` |
| 1.2 | Delete 7 getters + the two doc comments | `abwab-door-modal.component.ts:18, 29-34, 113-119` |
| 1.3 | Delete 7 label constants; delete 4 SCSS blocks | `models/abwab.labels.ts:179-185`; `abwab-door-modal.component.scss:6-30` |
| 1.4 | Spec surgery per §2.4; README edits L184-187, L199-203, L587-591 | `abwab-door-modal.component.spec.ts:272-287`; `features/abwab/README.md` |

Behavior change: the edit modal no longer shows «بيانات التتبع»; nothing else.
**Verification (Tier A):** `npm test -- --include="src/app/features/abwab/components/abwab-door-modal/*.spec.ts"`.
**Commit boundary:** one commit.

### Phase 2 — J1 `--wide` + adopters + docs (no behavior change)

| # | Task | Files |
|---|---|---|
| 2.1 | Add `.qd-modal--wide { width: min(100%, 52rem); }` after `.qd-modal` | `src/styles/_components.scss` (after `:599`) |
| 2.2 | Add the class to the three adopters' line-4 class lists | `abwab-relations-modal.component.html:4`, `abwab-move-picker.component.html:4`, `abwab-template-copy-modal.component.html:4` |
| 2.3 | §17 `--wide` entry per §2.1 (addresses `:1010-1013` explicitly; documents the 52rem step + ladder) | `.architecture/UI_STYLE_SYSTEM.md` §17 `.qd-modal` entry |
| 2.4 | `styles/README.md:50-53` sibling sentence; repoint/reword `_tokens.scss:160-163` | those two files |
| 2.5 | Visual check at 1024 / 1184 / 1440px, both themes, RTL: three adopters at 52rem; confirm dialog still 28rem; a words detail modal still 42rem; detail shell still 46rem | manual, recorded in the §14 DoD report |

Behavior change: none (geometry only). No spec exists to update — nothing pins widths.
**Verification (Tier B — styles/ touched):** full `npm test` + task 2.5.
**Commit boundary:** one commit.

### Phase 3 — J6 confirm retrofit pass + ride-alongs (the behavior seam)

| # | Task | Files |
|---|---|---|
| 3.1 | Primitive: `testIdPrefix` input + the four testid bindings; §17 entry update (input list + the "transient, never URL-addressable" sentence per §2.2) | `shared/ui/confirm-dialog/confirm-dialog.component.{ts,html}`; `UI_STYLE_SYSTEM.md:751-778` |
| 3.2 | Primitive spec: default-prefix regression case + custom-prefix case | `shared/ui/confirm-dialog/confirm-dialog.component.spec.ts` |
| 3.3 | Overlays controller: merge the two booleans into `archiveConfirm: 'single'\|'bulk'\|null`; add `archiveBusy` signal; invert close order (dialog closes in the subscriber, not before dispatch — `:115-127`, `:146-149`) | `state/abwab-page-overlays.controller.ts:101-153` |
| 3.4 | Page template: replace both inline cards (`:215-270`) with two `<qd-confirm-dialog tone="danger">` (prefixes per §2.2, titles per §5, bodies = existing messages + in-dialog error line — the single error owner per §2.2); delete the T403 slot + SCSS `:86-105`; the focus-return rule per §2.2 (roving-item fallback via `abwab-tree.component.ts:117/:283`) | `pages/abwab-page/abwab-page.component.{html,ts,scss}` |
| 3.5 | Sections modal: delete-confirm dialog (name-bearing body per §5), local confirm+busy signals, `remove()` becomes confirm-gated, 409 renders in-dialog, conditional `cdkTrapFocus` per §2.2; README `:450-453` rewrite | `components/abwab-sections-modal/abwab-sections-modal.component.{ts,html}`; `features/abwab/README.md:450-453` |
| 3.6 | Templates page: both inline alertdialogs (`:148-153`, `:177-182`) become `<qd-confirm-dialog tone="danger">`; busy signals; dispatch moves into subscribers (`:276`, `:295`) | `pages/abwab-templates-page/abwab-templates-page.component.{ts,html}` + its SCSS confirm block |
| 3.7 | Labels: 4 titles + sections body (§5), read via TDZ getters | `models/abwab.labels.ts` |
| 3.8 | Ride-along A3-a (`NO_ANCESTORS`) + its page-spec identity assertion | `state/abwab-page-overlays.controller.ts:241-254`; `pages/abwab-page/abwab-page.component.spec.ts` |
| 3.9 | Ride-along A2-a (`doorSubjectId` computed) + its same-id/different-id spec pair | `components/abwab-door-restore-modal/abwab-door-restore-modal.component.{ts,spec.ts}` |
| 3.10 | Spec updates (rewritten): `abwab-page.component.spec.ts:180-197` (`-yes` → `-confirm`; add busy-window case: dialog open + buttons disabled until the mocked write resolves), `:351-390` (same for bulk), `:904-916` (container id unchanged — re-run only); **new cases per §2.2:** three focus-return cancel cases (side-panel op / ctx-menu row / bulk bar) + one success case (roving-item focus after the row disappears); one single-error-owner case (failed archive → exactly one visible error, in-dialog); one URL case (open + cancel the sections-delete confirm → URL unchanged); `abwab-sections-modal.component.spec.ts:200, 217` (insert confirm step; add 409-in-dialog case); labels spec additions for the new constants | those spec files |
| 3.11 | E2E rewrites + run: `abwab-archive.e2e.ts:34` (`-yes` → `-confirm`; `:32` container survives), `abwab-operations.e2e.ts:109` (same; `:108` survives), `abwab-structure.e2e.ts:29-34` and `:54+` (insert confirm click; 409 asserted via `abwab-sections-modal-delete-error`, same Arabic message) — then an actual `npm run e2e` run, reported as supplementary evidence, never as Tier-C | `e2e/abwab-archive.e2e.ts`, `e2e/abwab-operations.e2e.ts`, `e2e/abwab-structure.e2e.ts` |
| 3.12 | §17 retrofit-list correction (`:776-778`); **delete TESTING_DEBT rows J2 (`:150`) and J3 (`:151`)** | `UI_STYLE_SYSTEM.md`; `docs/TESTING_DEBT.md` |
| 3.13 | Danger-tone visual verification (first production render): both themes, RTL, contrast vs the AA-verified pair (5.01:1), focus ring visible on the danger button | manual, recorded in the §14 DoD report |

Behavior changes (stated): confirms stay open during the write with dismissal suppressed;
failures render inside the dialog; sections delete gains a confirmation step (new click in
every flow that deletes a section); archive confirms move from the aside into a modal
overlay with focus trap, Escape, backdrop, initial-focus-on-CANCEL.
**Verification (Tier B — shared/ touched):** focused globs for the six touched spec files,
then full `npm test`; the e2e run per 3.11.
**Commit boundary:** two commits — (a) primitive `testIdPrefix` + spec + §17 (shared layer
alone); (b) the five migrations + ride-alongs + specs + e2e + docs + debt rows.

### Phase 4 — J8 badge header (the restructure, last)

| # | Task | Files |
|---|---|---|
| 4.1 | Wrap the three badges in the always-rendered `.abwab-tree__counts` grid group (leaf rows render it empty — the `@if (row.hasChildren)` moves inside the group) | `components/abwab-tree/abwab-tree.component.html:64-83` |
| 4.2 | SCSS: the shared grid template (one local width variable), `.abwab-tree__count` cell sizing, header styles; the `--wide` columns join the existing `≤1023px` rule `:131-135` | `abwab-tree.component.scss` |
| 4.3 | Header row before the `role="tree"` element, `aria-hidden="true"`, labels via new getters | `abwab-tree.component.html:1` area, `abwab-tree.component.ts` |
| 4.4 | Labels: `rowHeaderDirect/rowHeaderTotal/rowHeaderDepth` (§5); `rowDepthBadge` body → `` `${depth}` `` + comment rewrite | `models/abwab.labels.ts:129-138` |
| 4.5 | Specs: new cases — header renders with three labels, header is `aria-hidden`, header «الجميع»/«العمق» cells carry the `--wide` drop class, leaf row renders the empty group; **loading/empty cases per §2.3:** header absent alongside the tree skeleton, absent in the empty state, present with rows (skeleton/empty cases live in `abwab-page.component.spec.ts` — the page owns those branches); existing `:458/:469/:480/:499-505` re-run (only `:469` may need its selector adjusted to the group, not the spans — keep its assertion "no badge *values* on a leaf row") | `abwab-tree.component.spec.ts`; `pages/abwab-page/abwab-page.component.spec.ts` |
| 4.6 | README badge paragraph (~L104-115) rewritten: header exists, aria-labels are the accessible layer (not "the only place"), «ع» gone, 2-of-3 drop restated | `features/abwab/README.md` |
| 4.7 | §17 entry for the header-over-badges pattern per §2.3 | `UI_STYLE_SYSTEM.md` §17 |

Behavior change: visual only — header row, bare depth numeral, identical accessible output.
**Verification (Tier A):** `npm test -- --include="src/app/features/abwab/components/abwab-tree/*.spec.ts"`
plus a visual pass at desktop and ≤1023px (labels drop with their badges), RTL, both themes.
**Commit boundary:** one commit.

### Pre-PR (Tier C)

`npm test` (full, fork cap preserved) + `npm run build`. E2E already run in phase 3;
cite it as supplementary only. No backend gates — frontend-only slice.

## 4. Arabic strings (verbatim)

New (`models/abwab.labels.ts`, TDZ-getter consumption):

| Key | Value | Surface |
|---|---|---|
| `archiveConfirmTitle` | `تأكيد الأرشفة` | single + bulk archive dialog title |
| `sectionDeleteConfirmTitle` | `حذف القسم` | sections-delete dialog title |
| `sectionDeleteConfirmBody` | `(name) => \`سيتم حذف القسم «${name}»\`` | sections-delete dialog body |
| `templateDeleteConfirmTitle` | `حذف القالب` | template-delete dialog title |
| `templateNodeDeleteConfirmTitle` | `حذف العنصر` | node-delete dialog title (same string as `templateNodeDeleteOp:348` but a separate constant — titles must not be coupled to button labels) |
| `rowHeaderDirect` | `مباشرين` | badge header, column 1 |
| `rowHeaderTotal` | `الجميع` | badge header, column 2 |
| `rowHeaderDepth` | `العمق` | badge header, column 3 |

Reused unchanged: bodies `archiveConfirm(count)` (`:223`), `templateDeleteConfirm` (`:351`),
`templateNodeDeleteConfirm(name)` (`:349`); confirm buttons `archiveOp` «أرشفة» (`:148`),
`deleteSectionButton` «حذف» (`:198`), existing template/node delete button labels; cancel
`cancelButton` «إلغاء» (`:170`).

Removed: `trackingDataHeading` «بيانات التتبع», `trackingAddedByLabel` «أُضيف بواسطة»,
`trackingAddedByPlaceholder` «— (يُملأ مع تفعيل الحسابات)», `trackingApprovedLabel` «اعتمده»,
`trackingApprovedPlaceholder` «— (لم يُعتمد بعد)», `trackingArchiveLabel` «الأرشفة»,
`trackingArchiveActiveValue` «نشط» (`:179-185`); the «ع» prefix inside `rowDepthBadge`
(`:138` — function stays, body becomes the bare numeral).

## 5. Accessibility, RTL, and §14 DoD

- **Danger confirms:** focus trapped, **initial focus on CANCEL** (primitive contract — the
  safe answer for a destructive interrupt), `Escape`/backdrop cancel when not busy, both
  suppressed while busy, `aria-busy` on confirm while busy, `role="alertdialog"` +
  `aria-modal` + `aria-labelledby` — all inherited from the primitive; the migration is a
  strict a11y upgrade over the inline cards (which had none of it). Danger contrast is the
  AA-verified `--qd-danger` pair (5.01:1); task 3.13 eyeballs the first production render in
  both themes.
- **Focus return on close** follows §2.2's per-entry-point table: auto-restore when the
  trigger survives; the tree's roving-focus item (fallback: tree container) whenever the
  trigger is gone or disabled — never a dropped focus to `<body>`.
- **One error voice:** while a confirm dialog is open its projected error line is the sole
  surface for that write's failure (§2.2); no duplicate announcement.
- **Never URL-addressable:** no confirm dialog reads or writes any URL key (§2.2).
- **Header row:** `aria-hidden="true"`, presentational; per-badge `aria-label`s remain the
  accessible layer (qd-tabs precedent); it must never enter the `role="tree"` element;
  tablet drop mirrors the badge drop exactly (2 of 3).
- **RTL:** logical properties only throughout (grid + `inline-size`; no `left/right`);
  header label order matches badge DOM order; verified visually in phases 2–4.
- **§14 DoD report fields** (collected across phases, reported in the PR): global style
  files changed (`_components.scss`, `_tokens.scss` comment); new `qd-` classes
  (`.qd-modal--wide`; feature-local `.abwab-tree__counts`/`__header` are not `qd-`); theme
  tokens added/changed (none); components affected (three adopters, five confirm sites,
  primitive, tree, door modal); light/dark impact (danger tone first render — checked);
  RTL impact (header order, dialog footers — checked); build status.

## 6. Risks, rollback, stop conditions

**Risks:** the sections-delete confirm adds a click to a flow two e2e tests and two unit
specs script — all four are rewritten in-phase, but any OTHER incidental section-delete in
future tests must now confirm (localized risk); dual focus traps if the conditional-trap
gating regresses (spec-guarded in 3.5's cases); the phase-4 grid restructure touches the
row layout that the truncation contract's measured budget cites (`UI_STYLE_SYSTEM.md`
truncation entry: the name held ~184px beside all three badges) — the fixed group must not
shrink the name below usable width at 1024px (visual check in 4.5's pass); no CI — every
gate is a local run recorded in the PR.

**Rollback:** phases are independent commits in dependency-free areas; revert
individually. Phase 3's two commits revert together (primitive input is consumed by (b)).

**Stop conditions:** (1) phase-4 alignment cannot be made structural (shared grid) — STOP,
report, do not ship approximate alignment; (2) the conditional-trap gating in 3.5 produces
focus escape or double-trap fighting in the spec — STOP and report before shipping a
stacked confirm; (3) any adopter of `--wide` turns out to carry its own width rule that
defeats the modifier — report, do not add specificity hacks; (4) danger-tone contrast
visibly fails in either theme — STOP, token change is out of scope.

## 7. Acceptance criteria (each independently checkable)

1. Edit-door modal shows no «بيانات التتبع» panel; the 7 constants, 7 getters, 4 SCSS
   blocks are gone; `abwab-door-modal` specs green with the split prefill case.
2. `.qd-modal--wide` exists once in `_components.scss` at 52rem; exactly three templates
   carry it; at 1024px the wide modals render 832px wide with symmetric gutters.
3. Confirm dialog measures 28rem, detail shell 46rem, a words detail modal 42rem —
   unchanged with `--wide` present in the stylesheet (browser-verified).
4. All five migrated confirms render as `qd-confirm-dialog` with `tone: 'danger'`,
   distinct test-id prefixes, an Arabic title each (per §4), initial focus on cancel.
5. During an in-flight archive/delete the dialog stays open, both buttons disabled,
   Escape and backdrop inert; a failed write shows the Arabic outcome message inside the
   dialog — and **only** there (exactly one visible error element; page-level surface
   empty) — and the dialog remains open.
5a. Focus lands per §2.2's table: cancel returns focus to a surviving trigger
   (side-panel op, bulk bar) or the targeted row (ctx menu); success lands on the tree's
   roving-focus item; focus never drops to `<body>` (four spec cases green).
5b. No confirm dialog touches the URL: `:904-916` green, plus the sections-delete
   open/cancel URL-unchanged case; the §17 entry carries the "transient, never
   URL-addressable" sentence.
6. `grep -rn 'role="alertdialog"' src/app --include='*.html'` returns only the primitive
   and the three dirty-discard strips (door, sections, template-node modals).
7. The two archive confirms cannot be open simultaneously (single `archiveConfirm` signal).
8. `.abwab-page__archive-confirm-slot` and its SCSS are gone; §17's retrofit list names no
   surviving hand-rolled confirm; TESTING_DEBT rows J2 and J3 are deleted.
9. A3-a: `restoreAncestors()` identity is stable across rebuilds while closed (spec green).
   A2-a: same-id node replacement preserves the chosen section (spec green).
10. Tree shows one aria-hidden header row outside `role="tree"` with «مباشرين» / «الجميع» /
    «العمق», numbers grid-aligned under their words on every branch row; depth badge shows
    a bare numeral; ≤1023px drops «الجميع»/«العمق» (labels AND badges) and keeps «مباشرين»;
    spec `:499-505` untouched and green.
10a. The header renders only with real rows: absent alongside the tree skeleton, absent
    in the empty state (three spec cases green).
11. Per-badge Arabic `aria-label`s unchanged; screen-reader output identical to before J8.
12. Rewritten e2e specs pass in an actual `npm run e2e` run (reported as supplementary).
13. Full `npm test` + `npm run build` green (Tier B at phases 2–3, Tier C pre-PR).
14. All doc edits landed in the same commits as their behavior: §17 (three entries + the
    retrofit-list fix), `styles/README.md`, `_tokens.scss` pointer, abwab README (≥5
    paragraphs: L104-115, L184-187, L199-203, L450-453, L587-591), TESTING_DEBT.
