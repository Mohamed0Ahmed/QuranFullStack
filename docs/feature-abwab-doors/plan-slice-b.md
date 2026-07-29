# Feature: Abwab (الأبواب) — Slice B, frontend

Planning document. **Plan only — no code was changed and no Git action was taken while
writing it.** Companion to `plan.md` (Slice A, merged as PR #48).

---

## 0. Guard result — Slice B is split again into B1 and B2

The outline in `plan.md` §8 estimated Slice B at ~24 tasks. Re-costed against the actual
frontend tree and the inputs locked since that outline, the honest count is **33**:

| Outline phase | Outline estimate | Re-costed | Why it grew |
|---|---|---|---|
| 4 — state / data-access / tree / modal | ~9 | **15** | the announcement surface does not exist (§4.1); `qd-chip` has no remove affordance (§4.2); the `gates → abwab` rename is not free (§3.1); the tree splits into a presentational component + a testable keyboard controller; the modal and the page shell are two tasks, not one |
| 5 — cards / bulk / move / archive / sections | ~8 | **12** | reorder, search, context/side-action wiring and the styling pass were folded into "etc." in the outline; they are real tasks |
| 6 — e2e flows + docs | ~7 | **7** | unchanged (a shared sandbox-teardown helper replaced one flow task) |
| **Total** | **~24** | **34** | |

34 > 30, so the hard guard fires. Per the guard the work splits again, and **only Slice B1
is planned in full below**:

| Slice | Phases | Tasks | Ends with |
|---|---|---|---|
| **B1** (planned here) | 4 — state / data-access / tree / modal · 5 — cards / bulk / move / archive / sections | **27** | The complete page, matching the approved contract, with the full Vitest suite and `npm run build` green. No e2e write flows yet, so no test-doc invariant is violated. |
| **B2** (outlined in §9) | 6 — e2e flows + the two doc amendments + `TESTING_STRATEGY.md` §6 | **7** | The doors e2e flows, self-cleaning, with the deliberate read-only-invariant deviation amended in **both** documents. |

**Why this seam and not the one inside phase 5.** The alternative split (foundation +
single-door ops, then bulk/archive/sections) leaves a merged page on which nothing can be
archived and no section can be created — the only way to undo a mis-created door would be
nothing at all. "Zero dead controls" is satisfiable at either boundary, but only the
UI-complete seam produces a coherent product increment at both. B1 merges a page whose
every visible control works; B2 adds the browser-level proof and the doc amendments that
proof requires.

**What this costs:** the doors e2e flows and the `e2e/README.md` + `TESTING_STRATEGY.md` §6
amendments land in a second PR into `dev`, not the first. Both are still inside the feature
and both precede any `dev → main` release. The DoD in `plan.md` §11 is met across B1 + B2,
not by B1 alone. The interaction matrix (§6) is written **once, here**, and covers both
sub-slices; B2's flows are pinned against the same cells.

---

## 1. Objective and scope (Slice B1)

Build the Abwab page: a doors-and-sections management surface at `/abwab` that renders the
`GET api/abwab/tree` snapshot as a tree and as drill-down cards, and drives the eleven write
endpoints — create, edit, move, reorder, bulk move, bulk archive, archive, restore, and the
three section commands — with optimistic-concurrency conflicts surfaced, never swallowed.

**In scope (B1):** nav/route rename, feature folder, API client, snapshot facade + tree
builder, URL state, selection/bulk store, write controllers, the tree with its full ARIA and
keyboard model, add/edit modal, cards + breadcrumbs, move picker, archive view, sections
modal, the announcement region, the two allowed-green fixes, the `qd-chip` remove extension,
the feature README, and the root `CLAUDE.md` Active-Feature line.

**Out of scope (B1):** e2e flows and the two test-doc amendments (Slice B2). Protection,
relations, and templates — **no dead controls for them anywhere** (`plan.md` §5.1). Section
restore (there is no route). Bulk restore (there is no route). Paging (the read is one
snapshot). Auth (`plan.md` §10 still governs the release posture).

---

## 2. Inputs locked since the outline

Carried into the plan verbatim; each has a home in a task and a row in the matrix.

1. **Interaction matrix** — §6. Every UI operation × every door state, resolved.
2. **Archive view** — archived doors shown in their hierarchy; each restorable
   independently; a door whose parent is archived shows a **disabled** restore with
   «استرجع الأب أولًا».
3. **Restore reports the detach** — `AbwabRestoredDoorDto.detachedFromArchivedSection === true`
   ⇒ announce «استُرجع الباب خارج قسمه المحذوف». No silent data change.
4. **A11y** — `role="tree"` / `treeitem`, `aria-expanded`, `aria-level`, `aria-selected`,
   roving tabindex, keyboard expand/collapse/navigate. `dblclick` is an affordance, never the
   only path.
5. **Create under a parent** — the API derives the section from the parent and `400`s on a
   stated mismatch (`plan.md` §13.5), so the UI **never sends `sectionId` when `parentId` is
   set**.
6. **Bulk is all-or-nothing** — one stale token fails the whole operation with `409`; the
   message says so: «فشلت العملية كاملة — حدث تعارض على: X», then refresh and preserve context.
7. **409 everywhere** — unsaved input preserved, still-valid context kept, invalidated
   selection cleared, explicit conflict message before any retry. Never an auto-retry.

Plus, already locked: tokens-only styling with the two allowed-green fixes; English testid
slugs under the `abwab-` prefix; TDZ-safe label getters; URL state for section / view /
archive / selection; search over names **and** aliases; cards drill-down in scope; dirty
guard + inline error surface in the modal; zero dead controls.

---

## 3. Corrections to the Slice A plan

`plan.md` §5's conflict rule requires a conflict to be reported, never silently resolved.
Two of its statements do not survive contact with the tree.

### 3.1 The `gates → abwab` rename is **not** free — `plan.md` §8 is wrong

§8 asserts: *"This is free and safe … no test asserts the gates key."* The key is not the
only reference. `features/dashboard/pages/dashboard-home/dashboard-home.component.html:51`
carries a hardcoded `[routerLink]="['/gates']"` under the heading «الأبواب». Rename the nav
route and that card falls through the `**` wildcard to `/dashboard` — a dead card on the home
page. `e2e/dashboard-home.e2e.ts` asserts only «المصحف والآيات» and «الكلمات والجذور», so
nothing catches it.

The complete site list (grep over `src` + `e2e` for `gates`, both `.ts` and `.html`):

| Site | Change |
|---|---|
| `core/navigation/nav-items.ts:14` | `key`, `route`, `labelEn` → `abwab` / `/abwab` / `Abwab`. `labelAr` is already «الأبواب» |
| `features/dashboard/pages/dashboard-home/dashboard-home.component.html:51` | `[routerLink]="['/gates']"` → `['/abwab']` |
| `app.routes.ts:11-13` | add `abwab` to the placeholder-exclusion filter, else a placeholder route and the real route both register for `/abwab` |
| `app.routes.spec.ts:22-25` | register `abwab: ABWAB_ROUTES` in `STATIC_LAZY_ROUTE_ARRAYS` — the spec **throws** on a `loadChildren` route with no entry |

`route-paths.ts` exports no `GATES_ROUTE_PATH`, so nothing there changes except the new
`ABWAB_ROUTE_PATH` export. `shell-nav.e2e.ts` uses `nav-link--mushaf` and
`placeholder-routes.e2e.ts` probes `/mutashabihat`; both are unaffected. The rest of §8's
claim holds.

### 3.2 `TESTING_STRATEGY.md` §5 needs no edit — and the plan says so rather than inventing one

§5 is the **backend** command catalog. Slice B adds no backend test, touches no backend
namespace, and changes no backend filter; §5 already records `Tests.Abwab` (36 tests) and the
re-measured three-way identity `1,076 + 617 + 134 = 1,827` from Slice A. The frontend work is
entirely §6: re-measure the frontend counts, and (in B2) land the write-flow amendment. **§5
is deliberately untouched.**

---

## 4. Decisions taken in this plan

Five gaps the inputs imply but do not resolve. Each is decided here, with its reason, so no
task begins on an unbacked verb.

### 4.1 There is no toast surface — the announcement is feature-scoped

Verified: no toast component, no `qd-toast` class, nothing under `shared/ui/` or
`src/styles/`. Requirements 3 and 6 both say "toast".

**Decision:** a feature-owned `abwab-announcer` component — an `aria-live="polite"` region
that renders the current operation message. Not a global toast primitive.

**Why:** `UI_STYLE_SYSTEM.md` §9 admits a pattern to the style system when it is *repeated*;
§17's contracts are for app-wide patterns. One feature is not repeated. A global primitive
would amend `_components.scss`, §9, §17 and `shared/README.md` — four doc surfaces and real
task weight against the guard — to serve one caller. The feature-scoped region also doubles
as the a11y announcement channel required by input 4, which a visual-only toast would not.
If a second feature needs it, promote it then, with the §17 contract it will deserve.

### 4.2 `qd-chip` gains a remove affordance rather than being forked

The contract (§17 `qd-chip`) has `selected`, `disabled`, `as`, `count` — no remove. Alias
chips need the mock's `✕`. §17: *"a consumer needing a different surface/border is a signal
to extend this contract, not fork it."*

**Decision:** extend `QdChipComponent` with `removable: boolean` + a `remove` output rendering
a nested `<button>` with an Arabic `aria-label` («إزالة {alias}»), and amend §17's `qd-chip`
entry in the same change. The mock's solid-green `.chip` fill (`:158-159`) does not survive:
the chip renders per §16.1 — `--qd-selected-bg` + `--qd-accent-text` + `--qd-border-accent`.

### 4.3 Route form: `loadChildren` + `ABWAB_ROUTES`

One page today, but the feature will grow child routes (per-door detail is the obvious next
one), and `FRONTEND_STRUCTURE.md` prefers feature-owned route files. `loadChildren` costs one
extra line in `app.routes.spec.ts` (§3.1) and keeps the growth path open.

### 4.4 URL contract

Path `/abwab`. Six query keys, all parsed **fail-closed** to the default (the words
`*-url-sync.ts` precedent):

| Key | Values | Absent means |
|---|---|---|
| `section` | positive int (a section id) | «كل الأبواب» — every door including section-less ones |
| `view` | `tree` \| `cards` | `tree` |
| `archive` | `1` | the live view |
| `door` | positive int | no selection |
| `card` | positive int (the drilled-into parent) | the top card level |
| `q` | free text | no search |

`q` is in the URL, not local state: a filtered view is shareable and survives refresh, and
the words explorers already put search there. `card` exists because breadcrumb depth must
survive refresh and Back/Forward — the same rule that puts `view` there.

**Invalidation:** switching `section`, or turning `archive` on, clears `door` and `card`
(a selection is not meaningful across scopes). Turning `archive` off restores neither.

### 4.5 Archived doors are read-only

The archive view offers **restore only** — no edit, no move, no reorder, no add-child, no
bulk. Slice A exposes no write for an archived door except restore, so any other control
would be dead by definition (`plan.md` §5.1's rule, applied forward).

### 4.6 Refresh-after-write is an invariant, not an optimization

`plan.md` §4 locks *"every write resequences siblings to `1..N`"*. Resequencing UPDATEs
sibling rows, so **their `xmin` bumps too**. `AbwabBulkDoorRef` carries `{ doorId, version }`
and one stale token fails the whole bulk operation. So after *any* write in a scope, every
cached `version` in that scope is stale — including rows the user never touched.

**Invariant:** every successful write refetches `GET api/abwab/tree` and rebinds every cached
version token from the fresh snapshot; selection rebinds **by id**, dropping ids that vanished.
Pinned by a test named for the failure it prevents (M16).

---

## 5. File structure

`FRONTEND_STRUCTURE.md` requires the split to be stated before the tasks. Thresholds: facade
600 hard / 400 soft; component TS and HTML 400 hard / 300 soft; SCSS 300 hard / 200 soft; API
service 350 hard / 250 soft; helper 300 hard / 200 soft.

```text
src/app/features/abwab/
  abwab.routes.ts                      route + ABWAB_ROUTES export
  README.md                            the area's WHAT (routes, URL contract, invariants)
  models/
    abwab.models.ts                    view models, query keys, defaults, type guards
    abwab.labels.ts                    every Arabic string; read via TDZ-safe getters
  data-access/
    abwab.api.ts                       12 endpoints, ~190 lines
    abwab.api.spec.ts
  state/
    abwab-snapshot.facade.ts           load / refresh / cache / loading / error   (~220)
    abwab-tree.builder.ts              pure: snapshot → live tree + archive tree  (~180)
    abwab-selection.store.ts           single selection + bulk set + rebinding    (~150)
    abwab-write.controller.ts          door writes, outcome→message, 409 policy   (~260)
    abwab-sections.controller.ts       section writes                             (~120)
    abwab-url-sync.ts                  parse / build query params                 (~140)
    (+ one .spec.ts per file)
  components/
    abwab-toolbar/                     section tabs + search + view toggle
    abwab-tree/                        presentational tree (role="tree")
      abwab-tree-keyboard.controller.ts   roving tabindex + key model, pure       (~150)
    abwab-cards/                       cards grid + breadcrumbs
    abwab-side-panel/                  active door + operations + bulk bar
    abwab-door-modal/                  add / edit, aliases, dirty guard, errors
    abwab-move-picker/                 two-stage destination picker
    abwab-sections-modal/              list / add / rename / delete-empty
    abwab-archive-view/                archived hierarchy + per-entry restore
    abwab-announcer/                   aria-live="polite" operation messages
    (+ one .spec.ts per component)
  pages/
    abwab-page/                        shell: URL ⇄ state, composes the above
```

**Why this split.** One facade owning tree + cards + bulk + move + archive + sections + modal
would pass 600 lines and mix six workflows — the shape `FRONTEND_STRUCTURE.md` §"Large Page
Split" bans. The snapshot facade owns data; the selection store owns what is picked; two
controllers own writes by aggregate (doors, sections); the builder is pure and testable
without a DOM. The tree splits into a presentational component and a **pure keyboard
controller** so the roving-tabindex and RTL arrow model can be unit-tested without mounting a
tree — the same reason `explorer-table-sort.controller.ts` exists in Words.

**Expected to approach a soft threshold:** `abwab-write.controller.ts` (~260, soft 400 — it is
one workflow family, doors, and splitting per-command would scatter one 409 policy across
eight files) and `abwab-page.component.html` (~200, soft 300 — it composes nine children and
holds no panel bodies of its own). Neither is expected near a hard threshold; if either
crosses its soft line during implementation, report it in that phase's completion note.

**Shared/global files touched:** `core/navigation/nav-items.ts`, `core/navigation/route-paths.ts`,
`app.routes.ts`, `app.routes.spec.ts`, `features/dashboard/.../dashboard-home.component.html`,
`shared/ui/chip/*`, `src/styles/_components.scss` (the `.qd-chip` remove element only),
`.architecture/UI_STYLE_SYSTEM.md` §17. This is what puts the milestone at Tier B (§10).

---

## 6. Interaction matrix (mandatory)

Every UI operation against every door state. **An unresolved cell is a plan defect** — there
are none; the two cells that cannot be derived from the shipped contract are resolved
explicitly and say why.

### 6.1 The states

| Tag | State |
|---|---|
| **L-root** | live root door inside a section (`parentId = null`, `sectionId ≠ null`, `isArchived = false`) |
| **L-child** | live nested door (`parentId ≠ null`; its section is its parent's, `plan.md` §13.5) |
| **L-free** | live section-less door (`sectionId = null`) — the state that makes «كل الأبواب» a real superset (`plan.md` R8) |
| **A-live** | archived door whose parent is live or absent |
| **A-arch** | archived door whose parent is **also** archived |
| **A-lost** | archived door whose section was deleted meanwhile — restore will detach it (`plan.md` §13.2/§13.4) |

A **live door under an archived section is impossible**: section delete `409`s while the
section holds live doors, so the state cannot be authored. That is exactly why restore has to
detach rather than refuse.

### 6.2 The matrix

Cells: behavior · the test tag that pins it (legend in §6.3). "—" means the control is **not
rendered** for that state (never rendered-and-disabled, except where a disabled control is
itself the required affordance).

| Operation | L-root | L-child | L-free | A-live | A-arch | A-lost |
|---|---|---|---|---|---|---|
| **Select** (click / Enter) | selects; writes `door=<id>`; side panel enables single ops · M9, M26 | same · M9 | same · M9 | selects inside the archive view only; side panel stays disabled · M22 | same · M22 | same · M22 |
| **Expand / collapse** (chevron, dblclick, Arrow) | toggles; `aria-expanded` flips · M5, M7 | same · M5 | same · M5 | toggles inside the archive tree · M20 | same · M20 | same · M20 |
| **Keyboard focus** (roving) | one tabbable row app-wide; ArrowUp/Down walk **visible** rows · M6, M8 | same · M6 | same · M6 | same, within the archive tree · M20 | same · M20 | same · M20 |
| **Add child** (＋ / side / ctx) | opens the modal with `parentId=<id>`, **never** `sectionId` · M10, M33 | same · M10 | same · M10 (child inherits `null`) | — · M22 | — · M22 | — · M22 |
| **Add root** (header / ghost) | sends `sectionId` = the active section, or `null` under «كل الأبواب» · M11 | same | same | n/a — the control is hidden in the archive view · M22 | n/a | n/a |
| **Edit details** | modal prefilled; `PUT` under the door's own token; aliases replaced wholesale · M12, M13 | same | same | — · M22 | — · M22 | — · M22 |
| **Inline reorder** (the number) | commits on Enter, reverts on Escape; the scope resequences `1..N` and the whole snapshot refetches · M29, M15 | same · M29 | same · M29 | — · M22 | — · M22 | — · M22 |
| **Move single** | picker: section, then a door in it, or «كباب رئيسي» scoped to that section · M30 | same · M30 | same · M30 | — · M22 | — · M22 | — · M22 |
| **Archive single** | confirms with the **live**-subtree count computed client-side from the snapshot, then `DELETE` · M18 | same · M18 | same · M18 | — · M22 | — · M22 | — · M22 |
| **Restore** | n/a | n/a | n/a | enabled; on success refetch — **the UI promises nothing about which descendants return** (§6.4) · M15 | **disabled**, hint «استرجع الأب أولًا» · M21 | enabled; on success announce «استُرجع الباب خارج قسمه المحذوف» · M19 |
| **Bulk-select toggle** | checkbox in bulk mode; adds `{doorId, version}` · M23, M24 | same · M23 | same · M23 | bulk mode is **unavailable** in the archive view · M23 | same · M23 | same · M23 |
| **Bulk move** | all-or-nothing; on 409 «فشلت العملية كاملة — حدث تعارض على: X» · M17 | same · M17 | same · M17 | — · M23 | — · M23 | — · M23 |
| **Bulk archive** | same all-or-nothing contract · M17 | same · M17 | same · M17 | — · M23 | — · M23 | — · M23 |
| **Search** (name + alias) | matches; ancestors of a match stay visible and auto-expand · M4 | same · M4 | same · M4 | matched **only** while the archive view is on · M4, M31 | same · M4 | same · M4 |
| **Cards drill-down** | drills into live children; `card=<id>` · M25 | same · M25 | same · M25 | never rendered as a card · M31 | same · M31 | same · M31 |
| **Section tab placement** | under its section tab **and** «كل الأبواب» · M3 | under its parent's section · M3 | «كل الأبواب» **only** · M3 | in no live tab · M2, M31 | same · M2 | same · M2 |
| **Delete its section** | `409` «القسم يحتوي أبوابًا نشطة»; modal stays open · M27 | `409` (its section counts it) · M27 | n/a — no section · M27 | succeeds if the section holds no **live** door; the door becomes **A-lost** · M28 | same · M28 | already A-lost · M28 |
| **Any op → 409** | unsaved input preserved, valid context kept, invalidated selection cleared, explicit message, no auto-retry · M14 | same · M14 | same · M14 | same · M14 | same · M14 | same · M14 |

### 6.3 Test legend

Each tag is one planned test. Every cell above points at one of these; each of these is
written in the task that owns it.

| Tag | Spec file | Test |
|---|---|---|
| M1 | `state/abwab-tree.builder.spec.ts` | orders siblings by `orderValue` and tolerates gaps |
| M2 | `state/abwab-tree.builder.spec.ts` | partitions archived doors out of the live tree into the archive tree |
| M3 | `state/abwab-tree.builder.spec.ts` | keeps section-less doors in «كل الأبواب» and out of every section tab |
| M4 | `state/abwab-tree.builder.spec.ts` | matches search over names **and** aliases, keeping and expanding ancestors |
| M5 | `components/abwab-tree/abwab-tree.component.spec.ts` | renders `role="tree"`/`treeitem` with `aria-level`, `aria-expanded`, `aria-selected` |
| M6 | `components/abwab-tree/abwab-tree.component.spec.ts` | roving tabindex leaves exactly one tabbable row |
| M7 | `components/abwab-tree/abwab-tree-keyboard.controller.spec.ts` | **RTL**: ArrowLeft expands / enters the first child, ArrowRight collapses / moves to the parent |
| M8 | `components/abwab-tree/abwab-tree-keyboard.controller.spec.ts` | ArrowUp/Down/Home/End walk visible rows only (collapsed subtrees are skipped) |
| M9 | `components/abwab-tree/abwab-tree.component.spec.ts` | Enter selects the focused row; dblclick expands as an extra affordance, not the only path |
| M10 | `components/abwab-door-modal/abwab-door-modal.component.spec.ts` | create under a parent sends `parentId` and **never** `sectionId` |
| M11 | `components/abwab-door-modal/abwab-door-modal.component.spec.ts` | create at root sends the active section id, or `null` under «كل الأبواب» |
| M12 | `components/abwab-door-modal/abwab-door-modal.component.spec.ts` | the dirty guard blocks close with unsaved input; the inline error surface renders the backend message |
| M13 | `components/abwab-door-modal/abwab-door-modal.component.spec.ts` | alias chips add on Enter and remove through `qd-chip`'s remove output |
| M14 | `state/abwab-write.controller.spec.ts` | 409 keeps input, keeps valid context, clears invalidated selection, shows the message, never auto-retries |
| M15 | `state/abwab-write.controller.spec.ts` | every successful write refetches the snapshot and rebinds cached version tokens |
| M16 | `state/abwab-write.controller.spec.ts` | bulk move after a create in the same scope sends **fresh** tokens (the resequencing trap) |
| M17 | `state/abwab-write.controller.spec.ts` | a bulk 409 reports «فشلت العملية كاملة — حدث تعارض على: X» and preserves still-valid selection |
| M18 | `state/abwab-write.controller.spec.ts` | archive confirms with the **live**-subtree count derived from the snapshot |
| M19 | `state/abwab-write.controller.spec.ts` | `detachedFromArchivedSection: true` announces «استُرجع الباب خارج قسمه المحذوف» |
| M20 | `components/abwab-archive-view/abwab-archive-view.component.spec.ts` | renders archived doors in their hierarchy |
| M21 | `components/abwab-archive-view/abwab-archive-view.component.spec.ts` | a door whose parent is archived shows a **disabled** restore with «استرجع الأب أولًا» |
| M22 | `components/abwab-archive-view/abwab-archive-view.component.spec.ts` | restore is the only action offered on an archived door |
| M23 | `state/abwab-selection.store.spec.ts` | bulk mode is unavailable in the archive view |
| M24 | `state/abwab-selection.store.spec.ts` | selection survives a refetch by id and drops ids that vanished |
| M25 | `components/abwab-cards/abwab-cards.component.spec.ts` | drills into live children only and restores the level from `card` |
| M26 | `state/abwab-url-sync.spec.ts` | parses `section/view/archive/door/card/q` fail-closed and round-trips them |
| M27 | `components/abwab-sections-modal/abwab-sections-modal.component.spec.ts` | delete answers 409 «القسم يحتوي أبوابًا نشطة» and keeps the modal open |
| M28 | `components/abwab-sections-modal/abwab-sections-modal.component.spec.ts` | deleting a section holding only archived doors succeeds and refetches |
| M29 | `components/abwab-tree/abwab-tree.component.spec.ts` | inline order editing commits on Enter and reverts on Escape |
| M30 | `components/abwab-move-picker/abwab-move-picker.component.spec.ts` | picks a section, then a door in it, or «كباب رئيسي» scoped to that section |
| M31 | `pages/abwab-page/abwab-page.component.spec.ts` | archived doors are unreachable from the live tree, cards, tabs and search |
| M32 | `components/abwab-announcer/abwab-announcer.component.spec.ts` | messages land in one `aria-live="polite"` region |
| M33 | `data-access/abwab.api.spec.ts` | `createDoor` omits `sectionId` from the body whenever `parentId` is set |

### 6.4 The two cells that need an explicit resolution

**Restore of A-live — "which descendants come back?" is not derivable, by design.**
`AbwabTreeDoorDto` carries `isArchived: boolean` and **no `deletedAt`**, while restore matches
descendants on the archive operation's own timestamp (`plan.md` §13.1). A descendant archived
in the same operation and one archived separately earlier are, in the snapshot, indistinguishable:
both `isArchived: true`, both with the same `parentId`. **Resolution:** the UI promises
nothing — no preview, no count, no "N doors will return" copy. It calls restore, refetches the
snapshot, and lets the result speak. The alternative (adding `deletedAt` or an archive-batch id
to the tree DTO) is a backend contract change that reopens Slice A and regenerates OpenAPI; it
is out of Slice B's scope. **Do not "fix" this cell later by guessing a count.**

**Archive of a live door — the count *is* derivable, and is shown.** The snapshot carries every
door with `isArchived` flagged, so the live subtree under a door is computable client-side and
the confirm says «سيتم أرشفة N بابًا». The contrast with the cell above is the point: the
confirm counts *live* rows the UI can see; restore would have to count *archived* rows the UI
cannot distinguish.

---

## 7. Phase 4 — state, data-access, tree, modal (15 tasks)

One commit. TDD per task: the spec is written and run red before the implementation.

**Files**

- `src/app/core/navigation/nav-items.ts`, `route-paths.ts`, `app.routes.ts`, `app.routes.spec.ts`
- `src/app/features/dashboard/pages/dashboard-home/dashboard-home.component.html`
- `src/app/features/abwab/**` — the structure in §5 (page shell, toolbar, tree, modal,
  announcer, api, models, labels, facade, builder, selection store, write controller,
  url-sync, routes, README)
- `src/app/shared/ui/chip/chip.component.{ts,html,scss,spec.ts}`, `src/styles/_components.scss`
- `.architecture/UI_STYLE_SYSTEM.md` (§17 `qd-chip`)
- `CLAUDE.md` (root, Active Spec Kit Feature line)

**Tasks**

- **T401 — nav rename and its four sites.** Apply §3.1 exactly: `nav-items.ts:14`,
  `dashboard-home.component.html:51`, the `app.routes.ts` placeholder-exclusion filter, and a
  new `ABWAB_ROUTE_PATH` export in `route-paths.ts`. Add a dashboard-home spec assertion that
  the «الأبواب» card's `routerLink` is `/abwab` — the missing assertion that made this rename
  look free.
- **T402 — the route and the feature skeleton.** `features/abwab/abwab.routes.ts` exporting
  `ABWAB_ROUTES` (one `loadComponent` route for `AbwabPageComponent`, `title: navLabel('abwab')`),
  the `loadChildren` entry in `app.routes.ts`, and the `abwab: ABWAB_ROUTES` registration in
  `app.routes.spec.ts` `STATIC_LAZY_ROUTE_ARRAYS`. Verify the public-browse guard assertion
  still passes (no guard is added — `plan.md` §10 keeps the posture procedural).
- **T403 — `models/abwab.models.ts`.** View models (`AbwabNode` with `children`, `depth`,
  `liveChildCount`, `isArchived`; `AbwabTreeSnapshotVm` with `sections`, `liveRoots`,
  `archivedRoots`, `byId`), `ABWAB_QUERY_KEYS`, defaults, and the `isAbwabView` /
  `isPositiveId` guards used by url-sync. No labels here.
- **T404 — `models/abwab.labels.ts`.** Every Arabic string in one const, including the four
  locked ones (§2 items 2, 3, 6 and the section-delete conflict). Consumers read it through
  **TDZ-safe getters, never `readonly` field initialisers** (words README rule — they resolve
  to `undefined` in the bundled test build).
- **T405 — `data-access/abwab.api.ts` + spec.** Plain `@Injectable({providedIn:'root'})` +
  `HttpClient`, `environment.apiBaseUrl`, the `stems.api.ts` shape. Twelve methods: `getTree`,
  `createSection`, `renameSection`, `deleteSection`, `createDoor`, `updateDoor`, `moveDoor`,
  `reorderDoor`, `bulkMoveDoors`, `bulkArchiveDoors`, `archiveDoor`, `restoreDoor`. Returns
  `Observable<ApiResponse<T>>` using the generated `Abwab*Dto` types from
  `core/api/generated/`. **`createDoor` omits `sectionId` from the body whenever `parentId` is
  set** (input 5) — pinned by M33. Spec uses `setupApiTestBed` / `teardownApiTestBed` from
  `features/words/data-access/testing/api-test-bed.ts`; assert URL, method, and body per
  endpoint.
- **T406 — `state/abwab-tree.builder.ts` + spec.** Pure. Snapshot DTO → `AbwabTreeSnapshotVm`:
  build parent/child links, sort siblings by `orderValue` (gap-tolerant), partition archived
  from live into two trees, compute `liveChildCount` and per-section scope counts, and apply
  the search predicate over `name` **and** `aliases`, keeping ancestors of a match and marking
  them auto-expanded. Pins M1–M4.
- **T407 — `state/abwab-snapshot.facade.ts` + spec.** Owns the snapshot signal, loading, error
  and empty state, `load()` and `refresh()`, and the `ApiResponse` unwrap (`isSuccess` checked,
  `data` never assumed, backend `message` preserved) per `API_INTEGRATION_GUIDELINES.md`.
  Transport failure → a controlled error state, never a blank page. **`AbwabTreeDto.version` is
  deliberately not used for conflict detection** — per-row `xmin` tokens are the only concurrency
  currency, and §4.6's refetch makes a stale snapshot unreachable. Carry it on the view model for
  diagnostics only; do not build snapshot-level optimistic concurrency on it.
- **T408 — `state/abwab-url-sync.ts` + spec.** `parseAbwabQueryParams(ParamMap)` and
  `buildAbwabQueryParams(changes)` over the six keys in §4.4, fail-closed, plus the
  invalidation rule (a `section`/`archive` change nulls `door` and `card`). Pins M26.
- **T409 — `state/abwab-selection.store.ts` + spec.** Single selection (id + its live version),
  bulk mode flag, bulk set of `{doorId, version}`, `rebindTo(snapshot)` that re-reads every
  token by id and drops vanished ids, and the rule that bulk mode is unavailable while the
  archive view is on. Pins M23, M24.
- **T410 — `state/abwab-write.controller.ts` + spec.** Every door write. Per call: optimistic
  token from the store, dispatch, then on success → announce + `facade.refresh()` +
  `selection.rebindTo(...)` (§4.6); on `409` → the §2.7 policy; on `400`/`404` → the backend
  message inline (modal) or announced (non-modal). Bulk failures render
  «فشلت العملية كاملة — حدث تعارض على: X» with `X` = the names the response names. Restore maps
  `detachedFromArchivedSection` to its announcement. Archive computes the live-subtree count
  for the confirm. Pins M14–M19.
- **T411 — `components/abwab-announcer/` + spec.** One `aria-live="polite"` `role="status"`
  region (§4.1) plus a visible calm message strip using the existing tokens; `qd-state` is not
  the right primitive here (it replaces content; this annotates it). Pins M32.
- **T412 — `qd-chip` remove extension.** `removable` input + `remove` output + the nested
  `<button>` with `aria-label` «إزالة {label}», the `.qd-chip__remove` rule in
  `_components.scss` (hairline/tint only — no solid green, §16.3), the chip spec, and the §17
  contract amendment in `.architecture/UI_STYLE_SYSTEM.md` in the **same** change (root
  `CLAUDE.md` README/doc rule).
- **T413 — `components/abwab-tree/` + `abwab-tree-keyboard.controller.ts` + specs.** The
  presentational tree: `role="tree"` container with an Arabic `aria-label`; each row a
  `role="treeitem"` with `aria-level`, `aria-expanded` (branches only), `aria-selected`;
  roving tabindex with exactly one tabbable row. The pure controller owns the key model —
  **RTL-mirrored per the `qd-tabs` precedent (`tabs.component.ts:82-93`): ArrowLeft expands or
  enters the first child, ArrowRight collapses or moves to the parent**; ArrowUp/Down over
  visible rows only; Home/End; Enter selects; Space toggles the bulk checkbox in bulk mode;
  `ContextMenu`/`Shift+F10` opens the row menu (the keyboard path for right-click). `dblclick`
  stays as an extra expand affordance. Inline order editing (number → input, Enter commits,
  Escape reverts). Rows carry `data-testid="abwab-tree-row-<id>"`. Pins M5–M9, M29.
- **T414 — `components/abwab-door-modal/` + spec.** Name (required), description,
  representative-ayah free text with the hint that it is not a verified Quranic reference, alias
  input adding chips on Enter (composing the extended `qd-chip`), the **dirty guard**
  (close/Escape/backdrop with unsaved input asks first), and the **inline error surface**
  carrying the backend message. Tracking-data box on edit only, from the DTO. Composes
  `.qd-modal` / `.qd-modal-backdrop` and `modal-scroll-lock`; it does not hand-roll a dialog.
  Pins M10–M13.
- **T415 — page shell, toolbar, side panel, and the docs that ship with them.**
  `pages/abwab-page/` (URL ⇄ state wiring, composing toolbar + tree + side panel + announcer +
  modal); `components/abwab-toolbar/` (section tabs composing `qd-tabs` — «كل الأبواب» + one tab
  per section, **no** «الأبواب الرئيسية» tab per `plan.md` §5.1 — search input, view toggle);
  `components/abwab-side-panel/` (active door + the single-door operations; **no**
  relations/protection entries). Feature `README.md` and the root `CLAUDE.md` Active-Feature line
  («Slice B active», pointing at this document) land in this task. Pins M31.

**Verification**

```bash
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/features/abwab/**/*.spec.ts"
npm test -- --include="src/app/shared/ui/chip/*.spec.ts"
npm test -- --include="src/app/*.spec.ts"
npm test                     # Tier B: core/navigation, app.routes and shared/ui were touched
npm run build
cd ../.. && git diff --stat dev -- Backend/     # MUST be empty: the backend is untouched
```

**Budget (estimates — measure and record the real numbers in the completion note).** Focused
abwab glob ~18 spec files, est. 40–60 s. Chip + app-shell globs < 10 s each. Full suite today
169 files / 1,938 tests / ~2.9 min → est. ~187 files / ~2,100 tests / ~3.1–3.3 min. `npm run
build` est. 40–70 s. Backend: **zero runs** — no backend file changes, so no tier fires
(`TESTING_STRATEGY.md` §4, "Frontend feature only").

---

## 8. Phase 5 — cards, bulk, move, archive, sections (12 tasks)

One commit. Same TDD discipline.

**Files** — `src/app/features/abwab/components/{abwab-cards,abwab-move-picker,
abwab-sections-modal,abwab-archive-view}/**`, `state/abwab-sections.controller.ts`,
plus edits to `abwab-page`, `abwab-toolbar`, `abwab-side-panel`, `abwab-tree`,
`abwab-selection.store.ts`, `abwab-url-sync.ts`, and the feature `README.md`.

**Tasks**

- **T501 — cards view + breadcrumbs.** `components/abwab-cards/`: a grid of the current level's
  **live** doors, each card showing its order number, name and live-child count; clicking a
  branch drills in and writes `card=<id>`; breadcrumbs walk back; «كل الأبواب» is the root
  crumb. Leaf cards are non-drillable. Pins M25.
- **T502 — the view toggle and `card` restore.** Wire `view=tree|cards` through the toolbar and
  page shell; restore the drill level from `card` on entry and on Back/Forward; hide the
  "add root" ghost in cards mode (contract `:616`).
- **T503 — bulk mode.** The bulk toggle in the side panel (its `.on` state is the **first
  allowed-green fix**: tint + `--qd-accent-text` + hairline, never the mock's solid fill at
  `:76`), row/card checkboxes, the bulk bar with count and the selected names, and the
  clear action. Bulk mode clears the single selection and hides per-row actions, per the mock.
  Unavailable in the archive view.
- **T504 — bulk move and bulk archive.** Wire both to `abwab-write.controller`; the confirm for
  bulk archive states the total live-subtree count across the selection; the all-or-nothing
  409 message is the locked string. After either, refetch and rebind, keeping the still-valid
  selection. Pins M17 (with T410).
- **T505 — the move picker.** `components/abwab-move-picker/`: stage one picks a section
  (including «بلا قسم» for the section-less scope), stage two picks a destination door inside
  that section or «كباب رئيسي» — scoped to the picked section (`plan.md` §4). The moved
  door(s) and their descendants are excluded from the destination list (the client half of the
  cycle guard; the server's `409 WouldCycle` remains the authority and is surfaced if it
  fires). Single and bulk share the picker. Pins M30.
- **T506 — reorder wiring.** Connect the tree's inline number editor (T413) to
  `reorderDoor`, including the refetch-and-rebind path and the `409` policy.
- **T507 — search wiring.** Toolbar search → `q` in the URL → the builder's predicate (T406),
  debounced in the page shell, matching names **and** aliases, auto-expanding matched
  ancestors. In the archive view it filters the archive tree.
- **T508 — the archive view.** `components/abwab-archive-view/`: `archive=1` swaps the main
  column for the archived hierarchy, rendered with the same tree semantics. Each entry has a
  restore control; a door whose **parent is archived** renders it **disabled** with the hint
  «استرجع الأب أولًا» (the one place a disabled control is the required affordance). No other
  action is offered (§4.5). Pins M20–M22.
- **T509 — restore wiring and the detach announcement.** `restoreDoor` → on success refetch,
  and when `detachedFromArchivedSection` is true announce «استُرجع الباب خارج قسمه المحذوف».
  The restored door then renders under «كل الأبواب» with no section tab — the announcement is
  what makes that visible rather than mysterious. Pins M19 (with T410).
- **T510 — `state/abwab-sections.controller.ts` + `components/abwab-sections-modal/` + specs.**
  List / add / rename / delete-empty. Delete surfaces the backend `409` («القسم يحتوي أبوابًا
  نشطة») inline and keeps the modal open; a section holding only archived doors deletes
  cleanly and its doors become **A-lost** (§6.2). Section writes carry no client token on
  delete (`plan.md` §13.3). New/renamed sections appear as toolbar tabs after the refetch.
  Pins M27, M28.
- **T511 — the row context menu and side-panel operations.** Right-click (and the keyboard path
  from T413) opens a menu with edit / add child / move / archive — **and nothing else**: the
  relations and protection entries at contract `:277-278` and the sidebar pair at `:250-251` are
  **not implemented** (`plan.md` §5.1). Same for the per-node flags (`:436-437`), the card flag
  (`:598`) and the «الأبواب الرئيسية» tab (`:207-211`).
- **T512 — styling pass and contract reconciliation.** Every color through `--qd-*` tokens; no
  raw hex anywhere in the feature's SCSS (the dark theme is gold-accented — hardcoded greens
  would break it). The **second allowed-green fix** is already carried by T412 (alias chips
  compose `qd-chip`); this task verifies both fixes and sweeps the rest: hover =
  `--qd-surface-hover`, selected row = `--qd-selected-bg` + `--qd-accent-text` + a hairline,
  flat surfaces (no shadows outside the floating layers), `:focus-visible` rings on every
  interactive element. Report any remaining contract-vs-decision conflict in the completion
  note rather than resolving it silently (`plan.md` §5).

**Verification**

```bash
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/features/abwab/**/*.spec.ts"
npm test
npm run build
cd ../.. && git diff --stat dev -- Backend/     # still empty
```

**Budget (estimates).** Focused abwab glob ~28 spec files, est. 60–90 s. Full suite est. ~197
files / ~2,200 tests / ~3.2–3.5 min. Build est. 40–70 s.

---

## 9. Phase 6 — e2e flows and docs (Slice B2, outline only, 7 tasks)

Planned in full in its own document when B1 merges. Recorded here so the bar is set now.

- **T601 — the sandbox fixture.** A shared `e2e/fixtures/abwab.ts` that creates a uniquely
  named sandbox section, yields its id, and tears down by archiving every door it created and
  then deleting the now-empty section (the only lawful order — section delete `409`s while
  live doors remain). One helper, not the same dance repeated per flow.
- **T602–T605 — the flows.** Add section · add root and child door via the modal including
  alias chips · rename · number reorder · move single and bulk · archive and restore including
  the detach announcement · search by alias · cards drill-down · bulk-select · URL restore
  (refresh and Back/Forward over `section`/`view`/`archive`/`door`/`card`/`q`) · the keyboard
  a11y pass over the tree.
- **T606 — the two doc amendments, both explicit.** `e2e/README.md:39-41` ("read-only flows and
  loose count assertions only") **and** `TESTING_STRATEGY.md` §6, whose precondition is
  stronger — *"do not add write flows to it without first moving it onto an isolated
  database"* — and which the locked decision violates outright. §6 must be amended by name, not
  by implication (`plan.md` R1). Both amendments must state the **residue**: teardown archives
  the sandbox doors and deletes the section, so those archived doors detach on any future
  restore and there is no hard delete and no section restore — every run leaves archived orphan
  doors in the local dev DB permanently. That is tolerable on a local DB with loose counts, and
  it is not "self-cleaning"; say so in both files.
- **T607 — counts and READMEs.** Re-measure `TESTING_STRATEGY.md` §6 frontend counts (today 169
  files / 1,938 tests / ~2.9 min) — measured, not computed — and update `e2e/README.md`'s scope
  paragraph and the feature README. **§5 stays untouched** (§3.2).

---

## 10. Tier placement (Slice B1)

`TESTING_STRATEGY.md` §4, two rows fire; the stricter governs:

- *"Frontend feature only"* → phase tier A, pre-PR C.
- *"Frontend routing, app shell, or a public browse surface"* → phase tier A, pre-PR C.

But `Frontend/quran-dashboard-ui/CLAUDE.md` requires the **full** frontend suite (Tier B) at
milestones that touch `core/`, `shared/`, routing, the app shell, or theming — and phase 4
touches `core/navigation/`, `app.routes.ts`, `shared/ui/chip/` and `_components.scss`. So the
**full suite runs at both phase boundaries**, not just pre-PR, and `npm run build` runs at each
too (§7 of the strategy: builds are run after the latest fix, never before it).

**No backend tier fires.** No backend file changes; `git diff --stat dev -- Backend/` staying
empty is the evidence, and it is part of each phase's verification block. The Slice A smoke
tier and `Tests.Abwab` are not re-run and must not be cited as Slice B evidence.

**There is no CI** (§8). Every tier is a local gate that nothing verifies ran; "CI is green" is
never available. The Vitest fork cap (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`) is baked into
`npm test` and must be preserved — a direct `ng test` call must prefix it itself.

**Branch and PR.** `abwab-doors-b` branched off `dev`; one commit per phase; PR into `dev` at
the end of phase 5. Never `main` (root `CLAUDE.md`). The PR description repeats `plan.md` §10:
the eleven write routes are still `Open`, so this feature must not be included in a `dev → main`
release until write protection lands.

---

## 11. Risks and stop conditions

**R10 — The resequencing/stale-token trap.** Every write resequences its scope to `1..N`, so
every sibling's `xmin` bumps, so every cached token in that scope goes stale — including rows
the user never touched. Bulk is all-or-nothing, so one stale token fails everything. Mitigation
is the §4.6 invariant plus M16, whose name states the failure it prevents. **Without this the
feature will 409 exactly where inputs 6 and 7 promise it will not.**

**R11 — RTL arrow direction.** In an RTL tree the "into children" key is **ArrowLeft**. Getting
it backwards is the standard bug and it is invisible to an LTR reviewer. Pinned by M7, with the
`qd-tabs` mirroring precedent (`tabs.component.ts:82-93`) as the in-tree authority.

**R12 — Restore's descendant set is not previewable.** §6.4. The temptation is to show
"سيُسترجع N بابًا" by counting archived descendants; that count is wrong whenever any descendant
was archived in an earlier operation. The plan forbids the copy rather than the arithmetic.

**R13 — The `/gates` rename touches a page nothing tests.** §3.1. T401 adds the missing
dashboard-home assertion in the same task, so the next rename is caught.

**R14 — `app.routes.spec.ts` throws, it does not fail softly.** A `loadChildren` route with no
`STATIC_LAZY_ROUTE_ARRAYS` entry throws inside `flattenRoutes`. T402 registers it; if the app
shell spec suddenly errors rather than fails, this is why.

**R15 — Dead controls creep in from the mock.** The contract HTML still contains relations,
protection, the flags, and the «الأبواب الرئيسية» tab. Implementing it faithfully means
implementing them. T511 and T512 name the exact line numbers to leave unbuilt; the completion
note must state that none shipped.

**R16 — Global-file blast radius.** Phase 4 edits `nav-items.ts`, `app.routes.ts`,
`shared/ui/chip/` and `_components.scss`. That is why Tier B runs at the phase boundary, not
only pre-PR (§10). A green focused glob is **not** sufficient evidence for these phases.

**R17 — STOP: no backend change.** If any task appears to need a backend edit — a new field on
the tree DTO, a new route, a changed response — **stop**. That reopens Slice A, regenerates
`openapi/swagger.json` and `core/api/generated/`, and is a different plan. Report it instead.

---

## 12. Acceptance criteria (Slice B1)

- The approved contract (`docs/design-preview/abwab-tree-concept.html`) is visually matched with
  the §5.1 deletions applied and the §5.2 additions present — **the user's own headed run is the
  final gate**, not any automated assertion.
- **Zero dead controls**: no relations, no protection, no templates, no flags, no
  «الأبواب الرئيسية» tab, anywhere.
- **URL restore works**: `section`, `view`, `archive`, `door`, `card` and `q` survive refresh and
  Back/Forward, and invalid values fail closed to the defaults.
- **A11y keyboard pass**: the tree is reachable and fully operable from the keyboard —
  `role="tree"`/`treeitem`, `aria-level`, `aria-expanded`, `aria-selected`, one tabbable row,
  RTL-correct arrows, and a keyboard path to the row menu. `dblclick` is never the only path.
- Every matrix cell in §6.2 is backed by a passing test from the §6.3 legend.
- The full Vitest suite and `npm run build` are green at both phase boundaries, with the run
  after the last fix, not before it.
- `git diff --stat dev -- Backend/` is empty — the backend is untouched and no backend tier is
  claimed as evidence.
- `TESTING_STRATEGY.md` §6 counts are **re-measured** where they changed (or the change deferred
  to B2 with the current numbers stated); §5 is untouched and the plan says why.
- The feature `README.md` documents the routes, the URL contract, the refresh-after-write
  invariant, and the archived-doors-are-read-only rule; `UI_STYLE_SYSTEM.md` §17's `qd-chip`
  entry carries the remove affordance; the root `CLAUDE.md` Active-Feature line reads
  "Slice B active" and points at this document.
- Any contract-vs-decision conflict discovered during implementation is **reported** in the
  phase completion note, never silently resolved (`plan.md` §5).

---

## 13. Task-count summary

| Phase | Tasks |
|---|---|
| 4 — nav/route, api, models, labels, facade, builder, url-sync, selection, writes, announcer, chip, tree+keyboard, modal, shell | 15 |
| 5 — cards, view toggle, bulk mode, bulk ops, move picker, reorder, search, archive view, restore, sections, context menu, styling | 12 |
| **Slice B1 total (planned here)** | **27** |
| 6 — e2e fixture, four flow tasks, the two doc amendments, counts + READMEs | 7 |
| **Slice B2 estimate (own document)** | **7** |
| **Slice B total** | **34** |
| **Full feature (A + B)** | **61** |
