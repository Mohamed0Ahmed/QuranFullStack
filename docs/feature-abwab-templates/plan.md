# Feature: abwab-templates — door templates (قوالب الأبواب)

Planning document. **Plan only — no code was changed and no Git action was taken while
writing it.**

> Path note: workspace convention is `docs/feature-XXX-feature-name/`. This folder omits the
> numeric prefix, following `docs/feature-abwab-doors/`, `docs/feature-abwab-global-order/`,
> and `docs/feature-abwab-relations/` (the same user instruction).

> Spec Kit note: this feature does **not** populate `specs/`, for the third time in this area.
> `specs/` holds only its `README.md`. A later implementation review should look for
> `docs/feature-abwab-templates/plan.md`, not `specs/abwab-templates/contracts/`.

---

## 0. Guard result — 35 tasks, **split into two slices**

| Phase | Slice | Commit | Tasks | Ends with |
|---|---|---|---|---|
| 1 — housekeeping, schema, migration | A | 1 | 5 | `abwab_templates` + `abwab_template_nodes` exist; smoke dump regenerated |
| 2 — read path | A | 2 | 3 | `GET templates` + `GET templates/{id}` |
| 3 — template & node CRUD writes | A | 3 | 5 | six write routes, 400/404/409 mapped, never a 500 |
| 4 — apply (the deep copy) | A | 4 | 4 | `POST templates/{id}/apply`, all-or-nothing, N targets |
| 5 — contract regeneration | A | 5 | 1 | `openapi/swagger.json` → `core/api/generated/` + `docs/api-reference/` |
| 6 — frontend data & state | B | 6 | 4 | api, models, facade + controller, labels |
| 7 — the workshop page and the tree editor | B | 7 | 5 | `/abwab/templates`, the ◆ tree, the shared authoring form |
| 8 — the copy modal and the entry point | B | 8 | 3 | «نسخ إلى أبواب…», «القوالب» in the doors header |
| 9 — docs, debt, evidence | B | 9 | 5 | READMEs, `docs/TESTING_DEBT.md`, re-measured counts |
| **Total** | | **9** | **35** | Over the ~30 guard — **split** |

**The guard trips and the split is real, not cosmetic.** 35 tasks is not padded: the route
surface alone is **nine endpoints** (§4's route table), roughly triple what `abwab-relations`
added, and the frontend is a whole second page rather than one modal grafted onto an existing
one. The split point is the same one `abwab-global-order` and `abwab-relations` both named:
**phase 5, contract regeneration.** Slice A = phases 1–5 (backend + contract, 18 tasks),
Slice B = phases 6–9 (frontend + docs, 17 tasks). Phase 6 cannot start before phase 5 lands
either way, so the boundary is a real handoff and not an arbitrary cut.

**Execution: two short-lived branches off `dev`, two PRs into `dev`** —
`abwab-templates-a` (Slice A) then `abwab-templates-b` (Slice B), the `abwab-doors-a` /
`abwab-doors-b` precedent (#48/#49) rather than `abwab-relations`' single branch. Slice A
carries a migration; Slice B cannot compile before Slice A's generated models exist. Never
`main` (root `CLAUDE.md`).

---

## 1. Objective

`features/abwab/README.md:238-241` currently reads: **"Zero dead controls. Nothing for
protection, templates, or the «الأبواب الرئيسية» tab, anywhere in this feature."** This
feature falsifies the *templates* half of that line, exactly as `abwab-relations` falsified
the relations half — an admin can author a reusable door subtree once («أركان الإيمان» with
its six children) and copy it, whole, into any number of existing doors.

The approved design contract is `docs/design-preview/abwab-templates-concept.html`: a right-
hand template list with «+ قالب جديد», a center editor that is **the same tree language as the
doors tree** (chevrons, order chips, hover `＋`/`⋯`, the template root marked `◆`), and
«نسخ إلى أبواب…» opening a multi-target tree picker whose preview states the contract in one
sentence («كل باب مستهدف سيكسب ابنًا جديدًا… لا يمكن النسخ كباب رئيسي»).

The mockup shows structure only. The user locked two content additions it does not show, and
they change the data model, not just the UI:

1. **Every template node — root, child, grandchild — is authored through the same door-details
   modal** the doors page uses: name (required), description, representative-ayah free text,
   and search-alias chips.
2. **Any node can gain a child at any depth**, with no depth limit, exactly like doors.

Everything below derives from the contract plus §4's locked decisions; where the mockup
contradicts a decision, §9 names the line and the decision wins.

## 2. Scope

- Two new admin-only tables, `abwab_templates` and `abwab_template_nodes`, with the audit-seed
  columns, `xmin`, and the soft-delete columns the three abwab tables already carry.
- **Nine routes** (§4's table): template list / detail / create / delete, node create / edit /
  reorder / delete, and the apply (deep copy).
- A new route `/abwab/templates` and its workshop page: the template list, the tree editor, the
  node authoring modal, and the copy modal.
- One «القوالب» button in the doors page header, and a «↩ العودة للأبواب» return in the
  workshop header (mockup `:117`).
- **No new tests** (§8). Parity-catalog entries are **not** tests and are mandatory — nine of
  them.

## 3. Non-goals

- **No template application at root level.** A copy is always a new **child** of an existing
  door. The API refuses a rootless apply (`400`), so the rule is enforced at the wire, not just
  hidden in the picker (§6.1).
- **No link between a template and its copies.** Copies are detached at birth (§5.6). No
  "update all copies", no "which doors came from this template", no provenance column.
- **No "save this door subtree as a template"** — the obvious next ask, deliberately out. This
  slice authors templates from scratch only.
- **No template archive UI.** Template deletion is a soft delete and the template is simply gone
  from the list; there is no archived-templates view and no restore route (§4).
- **No relations and no protection on template nodes.** They are not doors. A copy is a plain
  door from birth, so relations can be added to the copies afterwards like to any door.
- **No template reordering** and no template folders/tags. The list is one flat list.
- **No visitor-facing anything.** Separate tables, admin routes only; the doors tree snapshot's
  contract is **unchanged** by this feature (§5.7).
- **No auth change.** The routes stay `Open`, like every other `/api/abwab` route; the release
  block in `features/abwab/README.md` («do not include this feature in a `dev → main` release
  until write protection lands») still stands and now covers **seven** more write-capable
  routes.

---

## 4. Locked decisions

| Area | Decision |
|---|---|
| What a template is | **A door subtree**: exactly one root node plus children and grandchildren to any depth, each carrying the same four authoring fields as a door |
| Storage | **Separate admin tables** — `abwab_templates` (identity + lifecycle) and `abwab_template_nodes` (the subtree). **Not** hidden doors: the visitor-facing door invariants stay clean |
| The root | **The root is a node row** (`parent_node_id IS NULL`), and there is **exactly one per template**, enforced by a partial unique index (§5.2). `abwab_templates` carries **no `name` column** — the template's name *is* its root node's name |
| Node aliases | Stored **on the node row** as `text[]`, not in a third table — the two-table decision forecloses `abwab_template_node_aliases` (§5.3) |
| Apply | **Deep copy.** The template root becomes a **new child** of each target door, full depth, all four fields, sibling order preserved |
| Multi-target | **One** endpoint call, N targets, **all-or-nothing**, per the bulk precedent (`features/abwab/README.md`, "Bulk is all-or-nothing") |
| Never a root | A copy is never a new root door. Empty `targetDoorIds` → `400`; there is no wire shape that could express "as a root" (§6.1) |
| Detachment | Copies are **ordinary doors from birth** — no back-link, no provenance. Editing the template later never touches earlier copies (§5.6) |
| Placement | Each copy **appends at the end of the target's children** (`CreateAsync`'s `count + 1` precedent) and **inherits the target's `section_id`** at every depth (the cascade invariant, `Writes/Abwab/README.md`) |
| Global order | Untouched. `global_order_value IS NOT NULL ⟺ parent_id IS NULL AND deleted_at IS NULL`, and a copy is never a root, so no copied door ever gets one (§5.7) |
| Uniqueness inside a template | `UNIQUE (template_id, parent_node_id, name) WHERE deleted_at IS NULL`, `NULLS NOT DISTINCT` — the doors' index, one table over. It is what makes a deep copy collide **only at the root** (§5.5) |
| Apply collision | The per-sibling door index is the authority. If any target already has a live child named like the template root, the **whole apply fails** with one `409` naming every colliding target (all-or-nothing) |
| Template delete | **Soft**, on the template row only. Node rows are untouched — the reader filters by the template's own `deleted_at`, so cascading would be ceremony. **Restore is out of scope this slice** |
| Node delete | **Soft, and it takes the node's subtree with it** — a template child has no meaning without its parent. Siblings resequence to `1..N`. Deleting the **root** is refused `400`; deleting the template is the way |
| Version tokens | **No version token on any templates route** — the `abwab-relations` decision, for the same reason (§5.4). Both tables still map `xmin`; nothing reads it |
| Node authoring | The **same** four fields as a door, through the **same form component**, extracted for reuse rather than duplicated (§7 T703) |
| Entry | «القوالب» in the doors page header; the workshop is its own route `/abwab/templates` |
| Migration | Authorized. EF tooling only; **local apply only** — plus the standing dump-regen rule (T105) |
| Branches | `abwab-templates-a` then `abwab-templates-b`, both off `dev`, both PR'd into `dev`. Never `main` |

### The route surface (nine)

| # | Route | Success | Refusals |
|---|---|---|---|
| 1 | `GET api/abwab/templates` | `200` list | — |
| 2 | `GET api/abwab/templates/{templateId:int}` | `200` template + flat node list | `404` |
| 3 | `POST api/abwab/templates` | `201` (creates template **and** its root node) | `400` empty name |
| 4 | `DELETE api/abwab/templates/{templateId:int}` | `204` | `404` |
| 5 | `POST api/abwab/templates/{templateId:int}/apply` | `201` created root doors | `400` empty targets / archived target, `404` unknown template or target, `409` name collision |
| 6 | `POST api/abwab/templates/{templateId:int}/nodes` | `201` node | `400`, `404`, `409` duplicate sibling name |
| 7 | `PUT api/abwab/template-nodes/{nodeId:int}` | `200` node | `400`, `404`, `409` |
| 8 | `POST api/abwab/template-nodes/{nodeId:int}/order` | `200` node | `400` (root has no siblings / out of range), `404` |
| 9 | `DELETE api/abwab/template-nodes/{nodeId:int}` | `204` | `400` (the root), `404` |

`POST … /order` and not `PUT`, matching `api/abwab/doors/{id:int}/order`
(`AbwabDoorsController.cs:100`); `PUT` for the edit, matching `AbwabDoorsController.cs:49`.
**Two controllers, decided here and not mid-phase:** at the ~26 lines/action the 79-line
`AbwabDoorRelationsController` measures, nine actions land near the 200-line controller soft
threshold (`BACKEND_STRUCTURE.md` §1). `AbwabTemplatesController` takes routes 1–5,
`AbwabTemplateNodesController` takes 6–9 with `[Route("api/abwab")]` and per-action templates,
the shape `AbwabDoorRelationsController` already uses.

---

## 5. The data model, derived line by line

### 5.1 The one sentence everything derives from

> **Applying a template inserts a copy of its root node as a NEW CHILD of each target door,
> and recursively copies that node's subtree beneath it.**

Every matrix cell in §6 is a consequence of that sentence plus the doors' own write invariants.

### 5.2 One root per template, and why the template row has no name

The mockup renders exactly one `◆` (`renderNode(t,0)`, `:208`) and the apply copy says the
template is copied **بجذره** as one child (`:139`, `:147`). But the editor also offers an input
placeheld «إضافة عنصر جذري للقالب…» (`:132-135`), which read literally would allow a second
root — and a template with two roots makes "the template root becomes a new child" undefined.

**Resolved, not inherited: exactly one root node per template.** That input adds a child *of*
the root. The invariant is structural, not handler logic:

```
UNIQUE (template_id) WHERE parent_node_id IS NULL AND deleted_at IS NULL
```

Consequence, and the payoff of the decision: **the template's name is the root node's name.**
`abwab_templates` therefore carries no `name` column — a denormalized copy would drift the
moment the root is edited through the authoring modal, and two edit paths for one string is
exactly the kind of duplication this area's READMEs already refuse. The list query joins the
root node for its display name and counts its live descendants for the «N عناصر» chip
(`:193`).

Second consequence: **the mockup's «إعادة تسمية» button (`:127`) is not a route.** Editing the
root through route 7 *is* the rename, and per the locked content addition it opens the **full**
authoring modal, not a name-only prompt. The button is relabelled «تعديل القالب» (§9).

### 5.3 Aliases live on the node row

The user locked **two** tables. That forecloses an `abwab_template_node_aliases` child table,
so the aliases must be a column: `aliases text[] NOT NULL DEFAULT '{}'`, Npgsql's native array
mapping for `IReadOnlyList<string>`.

This is a deliberate divergence from `abwab_door_aliases`, and the reason it is safe is that
the three mechanisms that table exists for have **no consumer here**: template aliases are never
searched (the toolbar's name+alias search is over doors), never soft-deleted, and never
individually identified. What is bought back: no third table, no `ReplaceAliasesAsync` twin, no
second `SaveChanges` in the node create path, and therefore no explicit transaction there.

Two things this obliges:

- **A named verification in T104**, not an assumption: confirm the generated migration emits
  `text[]` and not a JSON or owned-entity mapping. The repo has no `text[]` precedent (its
  `jsonb` uses — `MutashabihatGroupConfiguration.cs:51` and friends — are opaque source
  payloads, not authoring collections), so the column type is checked, not trusted.
- **Aliases are replaced wholesale, never mutated element-wise** — EF change-tracks an array
  property by reference comparison, so an in-place `Add` on the tracked instance may not be
  detected. Assign a new array.

The cost is stated plainly: if template-alias **search** is ever wanted, it is a migration and a
table, not a query change. Recorded so nobody discovers it as a surprise.

### 5.4 No version token on any templates route

`abwab-relations` established the rule and it applies unchanged: a token nothing checks is a lie
in the contract. Templates are solo-authored admin scaffolding — there is no second editor to
conflict with, no resequencing that bumps a sibling's `xmin` behind the user's back (node
reorder does resequence, but nothing anywhere holds a template node's token to be invalidated),
and no client that could produce an `expectedVersion` honestly.

So: **both tables map `Version` as `IsRowVersion()`** — it is Postgres's `xmin` system column,
free, no migration column, and it keeps the three abwab tables shaped alike — and **no request
body on any of the nine routes carries a `version`.** The only `409` these routes can produce is
the duplicate-sibling-name one. Do not add a token "for consistency" with the door writes.

The one place concurrency could bite is **apply × a concurrent template edit** — resolved in
§6.4 by reading the nodes inside the apply's own transaction and stating both outcomes as
legitimate, rather than by offering a token the user has no way to hold.

### 5.5 Sibling-name uniqueness inside a template is what keeps the copy honest

`abwab_doors` has `UNIQUE (section_id, parent_id, name) WHERE deleted_at IS NULL` with
`NULLS NOT DISTINCT` (`AbwabDoorConfiguration.cs:93-99`). A deep copy inserts doors under one
parent chain, so **every** internal sibling group of the copy is subject to it.

If a template were allowed two children named «الأدلة» under one parent, the apply would fail
mid-copy on a constraint the user never saw and cannot locate — a `409` naming a door that does
not exist yet. Mirroring the doors' index onto the nodes table removes that failure mode
entirely:

```
UNIQUE (template_id, parent_node_id, name) WHERE deleted_at IS NULL   -- NULLS NOT DISTINCT
```

With it, **the only collision an apply can hit is at the root**: the template root's name
against the target door's existing live children. That is one comprehensible message
(§6.1), and it is the *only* `409` the apply route can produce.

`NULLS NOT DISTINCT` is load-bearing for the same reason it is on doors: `parent_node_id` is
`NULL` for the root, and Postgres NULLs do not collide by default — without it the one-root
index and this one would both be silently unenforced for roots. Requires PostgreSQL 15+; the
target is postgres:16, as `AbwabDoorConfiguration.cs:93-95` already records.

### 5.6 Copies are detached, and the copy path is a door write

A copied door is an ordinary door with no memory of where it came from: no `template_id`
column, no `source_node_id`, nothing. Editing «أركان الإيمان» tomorrow changes the template and
nothing else; the three doors copied from it yesterday are untouched. Deleting the template
does not touch them either. **This must be stated in the modal's preview copy** so the user is
never surprised (§7 T801) — it is the single most likely wrong expectation this feature invites.

Where the copy is implemented follows from a measurement, not a preference. A persistence write
seam is a repository implementation, `BACKEND_STRUCTURE.md` §4 — soft 400, **hard 600** — and
`EfAbwabDoorsWriter` is **816 lines**, already 216 past hard. That section's own rule is "split
large repositories by aggregate, feature, read model, **or use case**", so the apply gets **its
own writer**, `EfAbwabTemplateApplyWriter` (`IAbwabTemplateApplyWriter`), whose single use case is
"copy a template subtree into N target doors". **That is the §4-prescribed split, applied
prospectively — not a workaround for a file that is too big.** Its seam crosses two aggregates by
design — reads `abwab_template_nodes`, writes `abwab_doors` — and that is documented in
`Writes/Abwab/README.md` (T901) rather than left for a reviewer to discover.

The doors writer's own excess is **out of scope**: this feature adds nothing to it, and splitting
what is already there would be a refactor of untested-by-this-feature write paths under a
no-new-tests posture. Noted, not carried.

What the apply does **not** need, and why — this is what makes a second writer cheap rather
than a duplication:

- **No `MaintainGlobalOrderAsync`.** Copies are never roots, so no `global_order_value` is ever
  assigned or resequenced.
- **No `Resequence`.** Every insert appends (`count + 1` within its own new parent scope), so
  every scope the copy touches is `1..N` by construction. The target's existing children are not
  renumbered — nothing left their scope.
- **`ResolveCreateSectionAsync`'s rule, not its code.** Section is read once off each target door
  and applied to the whole copied subtree, which is the cascade invariant stated directly rather
  than derived per node.
- **`23505` translation it owns itself.** Unlike `EfAbwabRelationsWriter` — whose duplicate is a
  *pair*, so the door-name-keyed helper was wrong for it — this writer's duplicate **is** a door
  name, so the message shape matches `AbwabDuplicateNameException` exactly. The trap is the
  inverse of the relations one and is worth naming: the collision is still pre-checked up front
  (the relations `GuardAgainstExistingAsync` pattern) so the `409` can name **which** target
  collided; the catch in the save helper stays as the race backstop, with no names.

What it **does** need, discovered in implementation and not anticipated above: **`AbwabDoor` has no
parent navigation property** (`AbwabDoorConfiguration` maps the self-FK with `HasOne<AbwabDoor>()
.WithMany()` and no CLR navigation), so a copied child's `ParentId` cannot be filled in by EF fixup —
it can only be set once its parent's generated id exists. The copy therefore **descends one level per
`SaveChanges`**, all inside the single enclosing transaction, which is what keeps the batch
all-or-nothing; each level's alias rows flush alongside the next level's doors. Do not "optimize"
this into one save: it would require a navigation property the entity deliberately does not have.

### 5.7 The doors tree snapshot contract is unchanged

`abwab-relations` added `RelationCount` to `AbwabTreeDoorDto` and therefore owed the
`api/abwab/tree` catalog entry a contract-change comment. **This feature adds nothing to that
DTO.** Templates are invisible to the doors snapshot; an applied copy shows up as ordinary
doors on the next `GET api/abwab/tree` like any other create. Snapshot `Version` needs no
widening either — the copy writes `abwab_doors` rows, which `Version` already covers.

Stated because the absence is a decision: nothing about a door says "I came from a template",
by §5.6, so there is no flag, badge, or count to add anywhere in the doors UI.

---

## 6. The interaction matrix

Mandatory section. "Live" = `deleted_at IS NULL`. Every cell has a matching manual-check step in
§10.

### 6.1 Apply × target door states

Anchor case: template «أركان الإيمان» (root + 6 nodes, one of them with 2 grandchildren),
applied to N selected doors.

| Target state | Outcome | Why |
|---|---|---|
| **Live door, no name clash** | `201`. Target gains one new child «أركان الإيمان» with the full subtree beneath it, appended last | §5.1 |
| **Live door that already has a live child «أركان الإيمان»** | `409`, **nothing is created anywhere** — the whole apply fails, message names the colliding target(s) | the doors unique index + all-or-nothing |
| **…whose colliding child is archived** | `201`. The index filters on `deleted_at IS NULL`, so an archived child does not occupy the name | same rule the doors create path already has |
| **Archived target** | `400` — the copy would be born invisible, and the target is read-only (`features/abwab/README.md`, "Archived doors are read-only"). Unreachable from the UI: the picker lists live doors only | the relations archived-target precedent |
| **Section-less target** (`section_id IS NULL`) | `201`. The whole copied subtree inherits `section_id = NULL` — a first-class state (§R8 of the doors plan) | cascade invariant |
| **Nested target, any depth** | `201`. Doors nest without limit; the copy just deepens the branch | no depth limit exists |
| **Two targets, one an ancestor of the other** | `201`. **Both get their own copy** — no dedup, no union | See below |
| **Unknown target id** | `404`, whole apply refused | |
| **Empty `targetDoorIds`** | `400`. **This is the "no root-level application" rule** — there is no wire shape that expresses "as a root", so the refusal is the empty-list refusal, not a rejected mode | §3 |
| **Unknown template id** | `404` | |
| **Deleted (soft) template** | `404` — a deleted template is gone, not hidden | §4 |

**The ancestor/descendant cell is the templates twin of bulk-archive's union-count gotcha, and
the answer is the opposite.** Bulk archive counts a *union* because archiving an ancestor
already claims its descendants (`features/abwab/README.md`, "Bulk-archive's confirm count is a
union, not a sum"). Applying a template claims nothing: each target independently gains its own
copy, so **the confirm count is the number of targets, always**. Do not "fix" this into a union.

### 6.2 Template editing × the copies that already exist

| Event | Existing copies | The template | Notes |
|---|---|---|---|
| Edit any node's name/description/ayah/aliases | **untouched** | updated | §5.6 — detachment is the whole rule |
| Add a node | untouched | grows | a later apply copies more |
| Reorder nodes | untouched | resequenced `1..N` | |
| Delete a node (with its subtree) | untouched | shrinks | |
| Delete the **template** | untouched | gone from the list | copies outlive their template |
| Edit a **copied door** | — | untouched | it is an ordinary door |
| Archive / move / reorder a copied door | — | untouched | no back-link exists to break |

### 6.3 Deep-copy edge cells

| Case | Outcome |
|---|---|
| **Empty template** (root only, no children) | Legal. Target gains one childless door. This is the default state of every newly created template |
| **Single-child template** | Legal, trivially |
| **3+ levels deep** | Legal; recursion has no depth limit, matching doors |
| **A node with empty description / no ayah / no aliases** | Copied as `NULL` / `NULL` / `{}` — the same nullability doors already allow |
| **A node with aliases** | Copied into `abwab_door_aliases` rows for the created door, live, in order |
| **Sibling order** | Preserved exactly: the copy's `order_value` is the node's `order_value`, which is `1..N` by the template's own resequencing |
| **Duplicate name *inside* the template** | **Unrepresentable** (§5.5) — refused at authoring time with a `409`, long before any apply |
| **Same template applied twice to the same target** | Second apply → `409` (the first copy now occupies the name). Correct and intended: two identical sibling subtrees would be indistinguishable |

### 6.4 Concurrency cells

| Case | Outcome |
|---|---|
| **Apply × a concurrent template edit** | The apply reads its nodes **once, inside its own transaction**. Postgres default isolation is READ COMMITTED, so a concurrent edit either commits before that read (copied) or after it (not copied). **Both are legitimate**; no token is offered because the user holds no template version (§5.4) |
| **Apply × apply on the same target** | Both compute `nextOrder = count + 1` and can produce a duplicate `order_value`. **Apply inherits this from `CreateAsync` and introduces nothing new** — reads tolerate gaps and ties, ordering by `OrderValue` then `Id` (`Reads/Abwab/README.md`). Do not invent a guard here that door create does not have |
| **Apply × apply of the *same* template on the same target** | One wins, the other `409`s on the root name. The unique index is the arbiter |
| **Node edit × node delete** | The delete wins or the edit wins; a missing row is `404`, never a 500 |
| **Template delete × apply** | The apply reads the template row first; a deleted one is `404` |

### 6.5 Derived, not decided

- **No template restore route, and no archived-templates view.** Deletion is soft purely so the
  rows survive for audit; there is no UI surface for them, so building a restore path would be
  building a control nothing reaches. Recorded as a consequence so nobody adds it "for symmetry"
  with doors, which *do* have a restore route because they *do* have an archive view.
- **The copy modal's picker lists live doors only** and has no root-level option — which is what
  makes the `400`s in §6.1 unreachable through the UI. They exist so the route is not a hole.
- **Quran safety:** the representative-ayah field is admin-authored free text, copied verbatim
  from node to door. Nothing in this feature generates, edits, or derives Quranic content.

---

## 7. Phases

Every phase is one commit. The tree builds at each commit boundary, and the phase's tier is
green before the next one starts.

## Slice A — backend and contract (phases 1–5, 18 tasks, branch `abwab-templates-a`)

### Phase 1 — housekeeping, schema, migration (5 tasks)

**Files** — `CLAUDE.md`; `Backend/domain/QuranDashboard.Domain/Abwab/AbwabTemplate.cs`,
`AbwabTemplateNode.cs` (new);
`Infrastructure/Persistence/Configurations/Abwab/AbwabTemplateConfiguration.cs`,
`AbwabTemplateNodeConfiguration.cs` (new); `Infrastructure/Migrations/` (generated);
`resources/db-dumps/quran-canonical/` (regenerated).

- **T101 — Housekeeping, with the buffer arithmetic verified rather than assumed.** Set the root
  `CLAUDE.md` Active-Feature line to `abwab-templates` + this plan.
  **Resolved at execution (2026-07-29):** `abwab-relations` merged into `dev` as **#52**
  (`d7511b38`) with **no close chore**, so the buffer is `abwab-relations` + `abwab-global-order`
  and the eviction due is **`docs/feature-abwab-doors/`** — the brief's guess (global-order) was
  wrong, exactly as `docs/feature-abwab-relations/plan.md` §11 predicted.
  **The eviction is deferred to its own chore PR (user decision), not done in this feature.**
  The reason is measured, not stylistic: `abwab-relations` §11 listed four inbound reference
  sites; the actual re-grep found **~55 references across ~30 files** — `features/abwab/README.md`
  (8), the e2e fixtures and four e2e specs, ten component/state files, three spec files,
  `shared/ui/chip/`, `src/styles/_components.scss`, `TESTING_STRATEGY.md`, `AGENTS.md`,
  `UI_STYLE_SYSTEM.md`, plus `docs/feature-abwab-global-order/plan.md` and
  `Backend/report/feature-abwab-global-order/005-*`. Repointing each to code + the nearest
  `README.md` is a hygiene change touching thirty files with no templates code in it; folding it
  into a feature commit would make the phase unreviewable. Flagged-not-fixed, the posture
  `docs/feature-abwab-global-order/plan.md` §9 already set for the 032/033 folders — see §11.
  The eviction is **docs-only** — there is no `Backend/report/feature-abwab-doors/`
  (`Backend/report/` holds `architecture`, `database`, `database-inventory`, `feature-008-*`,
  `feature-009-*`, `feature-abwab-global-order`). The pre-existing `docs/feature-032-rate-limiting/`
  + `docs/feature-033-auth-roles-permissions/` drift stays **out of scope** too.
- **T102 — The two entities**, in `Domain/Abwab/` beside `AbwabDoor`:
  - `AbwabTemplate`: `Id`, the audit-seed columns (`CreatedAtUtc`/`CreatedBy`/`UpdatedAtUtc`/
    `UpdatedBy`/`ApprovedAtUtc`/`ApprovedBy`), the soft-delete pair (`DeletedAtUtc`/`DeletedBy`),
    `Version` (`uint`, `xmin`). **No `Name`** — §5.2, and one comment carrying that sentence.
  - `AbwabTemplateNode`: `Id`, `TemplateId`, `ParentNodeId` (`int?`), `Name`, `Description`,
    `RepresentativeAyahText`, `Aliases` (`IReadOnlyList<string>`), `OrderValue`, the same audit
    seed, soft-delete pair, and `Version`.
- **T103 — The two configurations**, following the three existing abwab configurations exactly:
  `ToTable("abwab_templates")` / `ToTable("abwab_template_nodes")`, **explicit `HasColumnName` on
  every property**, `Version` as `IsRowVersion()` with **no** `HasColumnName` (the trap
  `AbwabDoorConfiguration.cs:65-68` documents). FKs with `OnDelete(DeleteBehavior.Restrict)` —
  node → template, node → parent node — for the same reason the doors carry it: deletion here is
  soft. Indexes:
  - `UNIQUE (template_id) WHERE parent_node_id IS NULL AND deleted_at IS NULL` — one root, §5.2;
  - `UNIQUE (template_id, parent_node_id, name) WHERE deleted_at IS NULL`, `AreNullsDistinct(false)`
    — §5.5, with the comment tying it to the doors' index it mirrors and to why `NULLS NOT
    DISTINCT` is load-bearing;
  - plain indexes on `(template_id, parent_node_id, order_value)` (the tree read), `parent_node_id`,
    and `deleted_at` — the doors' precedent;
  - `aliases` as `text[]` (§5.3), and its no-in-place-mutation comment.
- **T104 — Migration. STOP CONDITION.** Generate with EF tooling only, on explicit user go-ahead
  (`Backend/CLAUDE.md`). Two new empty tables, no backfill. Report migration name, generated
  files, build status, and that `database update` ran **locally only**. **Named verification:**
  confirm the `aliases` column emitted as `text[]` and not as an owned entity or a JSON column
  (§5.3) before moving on.
- **T105 — Regenerate the canonical smoke dump.** Two new tables move the migration head, and
  `TESTING_STRATEGY.md` §3/§5 is explicit that a **stale dump fails loud rather than skipping**.
  Under §8's no-new-tests posture a loud data-tier failure is a merge blocker, so this is a named
  task: run `Backend/scripts/create-smoke-dump --yes` after T104's local apply and state the
  outcome in the phase evidence.

**Verification**

```bash
dotnet build Backend/QuranDashboard.sln    # REBUILD after `migrations add`, before `dotnet test`
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab."
```

`Tests.Abwab` (46 today) must stay green — `AbwabSchemaTests`/`AbwabSchemaFixture` build a
Testcontainers schema from the migrations, so a broken migration fails here with nothing new
written.

**The rebuild between the two commands is load-bearing.** A migration generated after the last
build leaves the test assembly holding the entities and configurations but **not** the migration, so
every one of the 46 fails at fixture init with `PendingModelChangesWarning: The model for context
'QuranDashboardDbContext' has pending changes` — which reads as a broken migration and is not one.
Hit during execution; one rebuild clears it.

---

### Phase 2 — read path (3 tasks)

**Files** — `Application.Abstractions/Abwab/Responses/AbwabTemplateSummaryDto.cs`,
`AbwabTemplateDto.cs`, `AbwabTemplateNodeDto.cs` (new);
`Application.Abstractions/Abwab/IAbwabTemplatesReader.cs` (new);
`Application/Abwab/Queries/GetTemplates/`, `GetTemplate/` (new);
`Infrastructure/Persistence/Reads/Abwab/EfAbwabTemplatesReader.cs` (new);
`Api/Controllers/Abwab/AbwabTemplatesController.cs` (new);
`tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs`.

- **T201 — The three DTOs.**
  - `AbwabTemplateSummaryDto(int Id, string Name, int NodeCount)` — `Name` from the root node,
    `NodeCount` = live descendants of the root, which is the mockup's «N عناصر» chip
    (`countNodes`, `:186`, counts descendants and not the root itself — match it).
  - `AbwabTemplateNodeDto(int Id, int? ParentNodeId, string Name, string? Description,
    string? RepresentativeAyahText, IReadOnlyList<string> Aliases, int OrderValue)`.
  - `AbwabTemplateDto(int Id, string Name, IReadOnlyList<AbwabTemplateNodeDto> Nodes)` — **flat,
    not nested**, the `AbwabTreeDto` convention (`Reads/Abwab/README.md`, "Flat, not nested"): the
    client assembles the tree from `ParentNodeId` at any depth, and the frontend already has that
    builder shape.
- **T202 — `EfAbwabTemplatesReader`.** `AsNoTracking` throughout (the read area's rule).
  `GetAllAsync` — live templates, each with its root name and live descendant count, in **one**
  grouped query, never one per template (the `GetLiveRelationCountsAsync` rule). `GetAsync(id)`
  returns `null` for an unknown or deleted template so the handler can answer `404`.
  **A rootless template is treated as not-found, both here and in the list.** T302 is the only
  creation path and it always writes the root, so this state is unreachable today — but node rows
  are soft-deleted and the reader is what filters them, so a future bug, a manual DB fix, or a
  node-restore path someone adds would produce a template whose `Name` (derived from the root,
  §5.2) has no value to return. Naming the behavior costs one `INNER JOIN` on the root and removes
  an unstated failure mode from the one field the whole list UI renders. Stated in the README
  (T901). Node ordering: `ParentNodeId`, `OrderValue`, `Id` — the tie-break hardening the tree
  reader already uses.
- **T203 — Two query handlers + the two GET actions + two catalog entries.** New
  `AbwabTemplatesController` (`[Route("api/abwab")]`, per-action templates). Outcomes:
  `Success` → `200`; `NotFound` → `404`. **Both entries `ParityOnly = true`**, with the rationale
  spelled out: the templates workshop is the first feature whose own future smoke tests would
  create templates in the shared schema, and a dispatched `/api/abwab/templates` (derived `200`
  with `[]`) or `/api/abwab/templates/1` (derived `404`) would flip the moment such a test lands —
  the same order-dependence argument `abwab-relations` made for its GET
  (`SmokeRouteCatalog.cs:280-287`). Each entry still carries a `DerivedStatus`: documentation of
  what a well-formed call answers against the empty schema, never an assertion the sweep runs.
  Mandatory gate — `SmokeCoverageParityTests` fails by name without them.
  **`api/abwab/tree` needs no touch at all** this time (§5.7) — stated so nobody copies the
  relations plan's contract-change step out of habit.

**Verification** — a new route means the smoke filter alongside the API families
(`Backend/CLAUDE.md`):

```bash
dotnet build Backend/QuranDashboard.sln
dotnet test … --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab."
dotnet test … --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api"
dotnet test … --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

Evidence **must state whether `Tests.Smoke.Data` ran or skipped**.

---

### Phase 3 — template and node CRUD writes (5 tasks)

**Files** — `Application.Abstractions/Abwab/IAbwabTemplatesWriter.cs` + exception types (new);
`Application/Abwab/Commands/Templates/` (new, six command folders);
`Infrastructure/Persistence/Writes/Abwab/EfAbwabTemplatesWriter.cs` (new);
`Api/Controllers/Abwab/AbwabTemplatesController.cs`, `AbwabTemplateNodesController.cs` (new);
`Api/Common/ApiMessages.cs`; `tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs`.

- **T301 — Abstractions and Arabic messages.** `IAbwabTemplatesWriter` (create/delete template;
  add/edit/reorder/delete node) plus the plain exception types the seam may throw:
  `AbwabTemplateNodeDuplicateNameException`, **`AbwabTemplateRootNodeException`** (named for the
  root, not for deletion: the root refuses *reordering* as well, and one type with two
  handler-specific messages beats two near-identical types),
  `AbwabTemplateNodeNotFoundException`, `AbwabTemplateNotFoundException` — in
  `Application.Abstractions/Abwab/`, beside the twelve the area already defines. New `ApiMessages`
  entries for every outcome, Arabic, singular/plural through the counted-forms rule where a count
  appears.
- **T302 — `EfAbwabTemplatesWriter`: template create and delete.** Create writes the template row
  **and its root node** in one transaction (two `SaveChanges` — the template, then the node keyed
  by its generated id — the exact reason `CreateAsync` needs an explicit transaction,
  `Writes/Abwab/README.md`). Delete is a soft delete on the **template row only**; node rows are
  untouched (§4) and the reader filters by the template's `deleted_at`. A missing template is
  `false`, not an exception (the `IAbwabSectionsWriter` convention).
- **T303 — `EfAbwabTemplatesWriter`: node add / edit / reorder / delete.**
  - **Add**: parent must belong to the same template and be live; `order_value = live sibling
    count + 1` (the `CreateAsync` precedent — append, never insert).
  - **Edit**: the four authoring fields, aliases assigned as a **new array** (§5.3).
  - **Reorder**: within `(template_id, parent_node_id)`, resequence to `1..N`. The **root is
    refused `400`** — it has no siblings. Position out of range → `400`.
  - **Delete**: soft-delete the node **and its whole live subtree** (one parent-map BFS, the
    `CollectDescendantIds` shape — build the map once per operation), then resequence the
    remaining siblings to `1..N`. Deleting the root → `400`.
  - **Its own translating save helper**, keyed to the node **name** — the duplicate here is
    `(template_id, parent_node_id, name)`, so the message names the node. No stale-token branch:
    §5.4 makes it unreachable code, and unreachable branches are how the relations writer's
    helper decision was justified.
- **T304 — Six handlers** with exhaustive outcome enums, matching the doors commands' shape 1:1:
  `CreateTemplate`, `DeleteTemplate`, `AddTemplateNode`, `EditTemplateNode`, `ReorderTemplateNode`,
  `DeleteTemplateNode`. Trim-and-require the name at the edge → `400` on empty.
- **T305 — Routes, status mapping, six catalog entries.** Routes 3, 4, 6, 7, 8, 9 of §4's table,
  split across the two controllers as decided there. **All six `ParityOnly = true`** with the
  sibling write routes' rationale comment ("these write, so the generic sweep must not dispatch
  them"), each carrying a `DerivedStatus` derived against the empty schema. Mandatory gate.

**Verification** — the same four commands as phase 2; the smoke tier is **required** (six new
routes + new request contracts). State the `Tests.Smoke.Data` ran/skipped line.

---

### Phase 4 — apply, the deep copy (4 tasks)

**Files** — `Application.Abstractions/Abwab/IAbwabTemplateApplyWriter.cs` + exception types (new);
`Application/Abwab/Commands/Templates/ApplyTemplate/` (new);
`Infrastructure/Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs` (new);
`Api/Controllers/Abwab/AbwabTemplatesController.cs`; `ApiMessages`; `SmokeRouteCatalog.cs`.

- **T401 — Abstractions and messages.** `IAbwabTemplateApplyWriter.ApplyAsync(templateId,
  targetDoorIds, ct)`, plus `AbwabTemplateApplyCollisionException` carrying the colliding target
  door names, and `AbwabTemplateTargetArchivedException`. `ApiMessages`: applied
  (counted — «تم النسخ إلى بابين», not «إلى 2 أبواب»), collision-naming-the-targets,
  archived-target, empty-targets, not-found.
- **T402 — `EfAbwabTemplateApplyWriter`.** One transaction, all-or-nothing (§5.6 explains why it
  is its own writer and what it does **not** need). Steps, in order:
  1. Load the template's live nodes **once**, inside the transaction (§6.4);
  2. load all target doors in **one** query; refuse unknown ids (`404`), archived targets (`400`),
     an empty list (`400`);
  3. **pre-check collisions**: for each target, does a live child already carry the root node's
     name? Any hit → `AbwabTemplateApplyCollisionException` naming every colliding target, before
     any insert;
  4. per target: `order_value = live child count + 1`, `section_id = target.section_id`,
     `parent_id = target.id`, then recurse the node subtree assigning `parent_id` = the newly
     generated door id and `order_value` = the node's own;
  5. insert the alias rows for every created door;
  6. save through **its own** duplicate-name-translating helper — the race backstop, names-free
     (§5.6).
  Returns the created **root** doors as `AbwabDoorDto` (one per target).
- **T403 — `ApplyTemplateHandler`** with an exhaustive outcome enum:
  `Success(IReadOnlyList<AbwabDoorDto>)`, `InvalidRequest` (empty targets), `TargetArchived`,
  `NotFound`, `Collision(IReadOnlyList<string> DoorNames)`.
- **T404 — Route, mapping, catalog entry.** `POST api/abwab/templates/{templateId:int}/apply` →
  `201 Created` with the created root doors; a multi-create has no single resource URI, so the
  location is the doors collection — the exact call `AbwabDoorRelationsController.cs:42-44`
  already makes and comments. Mapping: `400` empty targets / archived target, `404` unknown
  template or target, `409` collision. One `ParityOnly = true` catalog entry.
  **Why a payload the UI ignores is still right here:** the frontend refreshes the doors snapshot
  by navigating back (§7 T802), so it does not read the body — but `201` with nothing created
  named in it would be the only write route in this area that reports a creation without saying
  what it created, and the ids are the natural evidence for a non-UI consumer. Recorded as a
  decision, not an oversight.

**Verification** — same four commands; smoke required. State the ran/skipped line.

---

### Phase 5 — contract regeneration (1 task)

- **T501** — **`Backend/scripts/export-swagger` FIRST**, then `npm run generate:api`
  (`ng-openapi-gen` + `scripts/prune-generated-api.mjs`) and `npm run docs:api`. The export is not
  optional and not implied: `generate:api` reads `Frontend/quran-dashboard-ui/openapi/swagger.json`
  off disk, so running it alone silently regenerates the **previous** contract and reports success.
  `docs/feature-abwab-relations/plan.md` §7 had the same hole and it cost a cycle here. Confirm the generated models carry `AbwabTemplateSummaryDto`,
  `AbwabTemplateDto`, `AbwabTemplateNodeDto`, and every request body. **`abwab-tree-door-dto.ts`
  must be unchanged** — if it moved, something violated §5.7. This is the Slice A → Slice B
  handoff artifact; Slice B cannot start before it lands.

**Verification** — `npm run build`: the generated models must typecheck before anything consumes
them. Nothing is expected to break (no existing DTO changed, by design).

---

## Slice B — frontend and docs (phases 6–9, 17 tasks, branch `abwab-templates-b`)

### Phase 6 — frontend data and state (4 tasks)

**Files** — `features/abwab/data-access/abwab-templates.api.ts` (new);
`models/abwab-templates.models.ts` (new); `models/abwab.labels.ts`;
`state/abwab-templates.facade.ts`, `state/abwab-templates.controller.ts` (new).

- **T601 — `AbwabTemplatesApi`**, its **own** data-access file rather than a tenth to fifteenth
  method pile on `abwab.api.ts`: nine endpoints on a separate route family, and the existing file
  is already at fifteen. The two `204` routes (template delete, node delete) return
  `Observable<ApiResponse<unknown> | null>` — the **null-envelope** shape (`features/abwab/README.md`,
  "A `204 No Content` arrives as a `null` envelope"). Do not dereference `response.isSuccess`
  first; this exact bug class already shipped once in this feature area.
- **T602 — `models/abwab-templates.models.ts`.** `AbwabTemplateSummaryVm`,
  `AbwabTemplateNodeVm` (with `children`), and `buildAbwabTemplateTree(dto)` — the pure flat→tree
  build, the `abwab-tree.builder.ts` shape and its sibling ordering. Plus
  `AbwabAuthoringFields { name, description, representativeAyahText, aliases }`, the value type the
  shared form component (T703) reads and emits.
- **T603 — `state/abwab-templates.facade.ts` + `state/abwab-templates.controller.ts`.** The facade
  owns the list and the selected template's tree with loading/error/empty state and a
  `refresh()` that always issues a new request — the `AbwabSnapshotFacade` contract, including
  "on failure the previous snapshot is left in place". The controller owns the writes, the
  outcome→message mapping, and the announcement, forwarding to the facade's refresh after every
  success. **It does not reuse `AbwabWriteController`**: that controller's core invariant is
  refresh-the-doors-snapshot-and-rebind-every-version-token, and templates have no version tokens
  and are not in that snapshot. Two different refresh targets, so two controllers — and the
  409-policy sharing that justified reusing it for sections and relations does not apply, since
  the only 409 here is a duplicate name with a backend message.
  **The apply write does not refresh the doors snapshot** — `AbwabPageComponent.ngOnInit` calls
  `facade.load()` on every entry (`abwab-page.component.ts:143-145`), so returning to `/abwab`
  always refetches. Coupling the workshop to the doors facade would buy a fetch nobody sees.
- **T604 — `abwab.labels.ts`.** Every Arabic string for the workshop: page title/subtitle,
  «قالب جديد», «تعديل القالب», «نسخ إلى أبواب…», «العودة للأبواب», the node action labels, the
  copy modal's title/description/**preview** (including the detachment sentence, §5.6), the
  «N عناصر» chip, the counted apply button («انسخ إلى 3 أبواب» / «انسخ القالب»), the empty
  states, and the confirm copy for node deletion (which takes a subtree). Counted labels go
  through the existing Arabic number-forms helper (`abwab.labels.ts:11-20`) — «انسخ إلى بابين»,
  never «إلى 2 أبواب». **Check before authoring conflict copy**: the write path prefers the
  backend's own Arabic message when present, so a plan-authored duplicate string is dead code —
  the mistake `features/abwab/README.md` records for the section-delete copy.

**Verification** — `npm test -- --include="src/app/features/abwab/**/*.spec.ts"` (Tier A).

---

### Phase 7 — the workshop page and the tree editor (5 tasks)

**Files** — `abwab.routes.ts`; `pages/abwab-templates-page/` (new);
`components/abwab-template-tree/` (new); `components/abwab-door-fields-form/` (new);
`components/abwab-door-modal/`; `components/abwab-template-node-modal/` (new);
`pages/abwab-page/`.

- **T701 — Route and page shell.** A second entry in `ABWAB_ROUTES` (`path: 'templates'`,
  `loadComponent`, its own `title`) — the feature already lazy-loads its own routes file, so
  `/abwab/templates` costs one entry and no app-level change. The page renders the header
  (title, subtitle, «العودة للأبواب»), the left template list with «+ قالب جديد» and its
  selected state, and the editor panel — the mockup's `:112-137` layout, in the existing
  `.qd-page`/`.qd-container` shell so it inherits the flat parchment+green surface rules rather
  than restating them.
- **T702 — `abwab-template-tree`**, the editor's tree. **The same tree language as the doors
  tree, not the same component**: chevron expand/collapse at any depth, the order chip, the root
  marked `◆` (`:208`) with a bold name (`:67`), hover-revealed `＋` (add child) and `⋯` (more)
  actions, and the inline «إضافة عنصر…» row. Presentational — every action is an output; no
  service is injected. Reusing `AbwabTreeComponent` itself is rejected in the plan, not
  discovered mid-phase: it is typed on `AbwabNode`, carries selection/bulk/roving-tabindex/URL
  concerns this page has none of, and has an existing spec suite pinned to that behavior.
- **T703 — Extract `abwab-door-fields-form`, then `abwab-template-node-modal`.** The locked
  content addition is that a node is authored through the **same** door-details form. The
  extraction, not a generalization of `AbwabDoorModalComponent`, is the choice — the discriminator
  is spec churn under a no-new-tests posture (§8):
  - `abwab-door-fields-form` (new, presentational) owns the four fields, the alias chips
    (composing `qd-chip` with `removable`), the dirty tracking, and the inline error surface. It
    takes an `AbwabAuthoringFields` value and emits changes; it injects nothing.
  - `abwab-door-modal` keeps its **entire public contract** — `open`/`door`/`parentId`/
    `parentName`/`activeSectionId`, `closed`/`saved`, `inject(AbwabWriteController)` — and simply
    renders the shared form inside its shell. **Its tracking-data box stays in the shell**, which
    is why no "hide tracking" flag is needed: template nodes have no archive status to show.
  - `abwab-template-node-modal` (new) is the second shell: same form, title/context of its own,
    and its submit arrives as a **function input** bound by the workshop page to
    `AbwabTemplatesController` — the `abwab-sections-modal` / `abwab-relations-modal` precedent.
  - **Trap, load-bearing:** `AbwabDoorModalComponent.submit()` carries
    `sectionId: parentId != null ? null : activeSectionId()` as documented defense-in-depth
    (M10/M33, `features/abwab/README.md`). It stays in the door modal's shell. Do not let the
    extraction drift it into the shared form, which has no concept of a section.
  - **Markup and `data-testid`s are preserved** through the move, so
    `abwab-door-modal.component.spec.ts` and `abwab-operations.e2e.ts` stay green. That is a
    **verified claim, not an assumption** — the task is not done until that spec file runs green
    unchanged.
- **T704 — Node and template actions wired.** Create template (name-only prompt → creates the
  template and its root, then the root is editable through the full modal), «تعديل القالب» on the
  root, add child at any depth, edit any node, inline reorder (the doors tree's click-the-number
  editor is the precedent), delete node — with a confirm that says the subtree goes too — and
  delete template. Errors and successes go through the page's announcer region, the
  `abwab-announcer` pattern.
- **T705 — Existing-spec repair pass (not new tests).** T703 moves markup between components and
  T701 adds a route; `abwab.routes.spec.ts` asserts the route table, and the door-modal spec
  renders the extracted form. Repairing what this feature breaks is keeping the suite green —
  §8 forbids **new** coverage, not fixture repair. Do not quietly add workshop assertions here;
  that coverage is debt, recorded in T903.

**Verification**

```bash
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/features/abwab/**/*.spec.ts"   # Tier A, focused
npm test                                                       # Tier B/C, full suite
npm run build
```

---

### Phase 8 — the copy modal and the entry point (3 tasks)

**Files** — `components/abwab-template-copy-modal/` (new);
`pages/abwab-templates-page/`; `pages/abwab-page/abwab-page.component.html`.

- **T801 — `abwab-template-copy-modal`**, the contract's `:142-160`. Composes
  `.qd-modal`/`.qd-modal-backdrop` + `qdModalScrollLock` like every modal in this feature. Takes
  the live door tree and its apply function as inputs. Details that are easy to lose:
  - the **preview block** states the rule in the user's words: every target gains a new child
    «X» with its N elements, **and it cannot be copied as a root door** (`:147-148`) — plus the
    detachment sentence from §5.6, which the mockup does not have and the user must not learn
    by surprise;
  - the picker is a **live-doors-only** expandable tree with checkbox multi-select at any depth,
    search that **auto-expands matching paths** (`subtreeMatches` at `:231`, `hasKids && q`
    forcing open at `:235`), and a selection summary bar naming the picks (`:263`);
  - **no root-level option exists** — by design, §6.5;
  - the confirm button is disabled at zero and counts the picks through the Arabic number forms
    (`:265`, corrected for «بابين»).
  **The picker logic is deliberately duplicated from `abwab-relations-modal.component.ts:167-197`,
  not extracted.** That component has **no spec at all** (`docs/TESTING_DEBT.md` row 4), so
  refactoring it under a no-new-tests posture would be changing untested code to save ~30 lines.
  The unification trigger is **the same trigger row 4 already carries** — when the relations modal
  next changes shape and gets its specs, both pickers become one. Recorded so this does not
  silently become two divergent pickers.
- **T802 — The «القوالب» entry.** One button in the doors page header
  (`abwab-page.component.html:12-38`, beside «إدارة الأقسام»), routing to `/abwab/templates`;
  hidden while the archive view is active, like the add-root button, since templates apply only
  to live doors. The workshop's «↩ العودة للأبواب» routes back. No state is carried across —
  returning re-runs `facade.load()` (T603), which is what makes the copies visible.
- **T803 — Announcements and error surfaces.** The apply's `409` is the one refusal the user will
  actually hit: it names the colliding target(s), the whole apply failed, nothing was created —
  and the modal stays open with the selection preserved, the bulk-conflict precedent
  (`features/abwab/README.md`, "that selection is preserved rather than cleared on conflict").
  Success announces the counted message and closes.

**Verification** — same three commands as phase 7.

---

### Phase 9 — docs, debt, evidence (5 tasks)

- **T901 — Backend READMEs. MOVED TO SLICE A** (executed 2026-07-29, its own commit on
  `abwab-templates-a`). Slice A is its own PR into `dev`, and the root `CLAUDE.md` requires a README
  to be updated in the **same change** as the boundaries it describes — leaving it here would have
  merged two READMEs stating a falsified endpoint count and a "one seam per aggregate" rule the apply
  writer breaks by design. T902 (`features/abwab/README.md`) correctly stays in Slice B, where the
  frontend it describes lands. What it covered: `Persistence/Writes/Abwab/README.md`: the two
  new writers, **why the apply is its own writer** (the 816-vs-600 measurement, §5.6) and that it
  crosses two aggregates by design, the append-only/no-resequence/no-global-order derivation, the
  inverse-of-relations `23505` note, node delete taking its subtree, template delete touching one
  row. Its "**one seam per aggregate**" rule now needs the use-case-seam exception stated
  explicitly. `Persistence/Reads/Abwab/README.md`: the third and fourth read endpoints, flat-not-
  nested for nodes, the one-grouped-query count rule, `null` vs empty and the rootless-template
  rule (T202), and that the doors snapshot is untouched by templates.
- **T902 — `features/abwab/README.md`, including the line this feature falsifies.** The **"Zero
  dead controls. Nothing for protection, templates, or the «الأبواب الرئيسية» tab"** gotcha is now
  wrong for *templates* — rewrite it the way `abwab-relations` rewrote the relations half: say the
  templates entry and workshop are live and *why*, keeping the rule intact for protection and the
  tab. Add: the new route and page, the two new state files and why they are **not**
  `AbwabWriteController`, the shared authoring form and the M10/M33 trap that stayed in the door
  modal's shell, the duplicated picker and its unification trigger, detachment (§5.6), and the
  render-chain entries. Update the counts in "What this feature does" — endpoints and write
  endpoints both move; **measure them off the controllers**, do not do arithmetic on the old
  numbers.
- **T903 — `docs/TESTING_DEBT.md`** gains its **second** feature section (the file exists; append,
  do not recreate). One row per skipped area, each naming a concrete future trigger:
  - backend template/node write behavior (one-root enforcement, sibling uniqueness, subtree
    delete, resequence) — pays when the templates writer is next touched;
  - **the deep copy** (depth, order preservation, section inheritance, aliases, all-or-nothing,
    the root-name collision) — **the highest-value row in the file**: it is the only place in the
    repo where door rows are created by something other than `CreateAsync`, and it pays when the
    apply path or the doors' unique index is next changed;
  - templates smoke (`201`/`204`/`400`/`404`/`409` bodies for nine routes) — pays when write
    protection lands and `/api/abwab` stops being `Open`;
  - frontend workshop specs (tree editor, node modal, copy modal picker) — pays when the workshop
    next changes shape;
  - one e2e flow (author a two-level template → copy into two doors → both trees show the subtree
    → edit the template → the copies do not change) — the only check that would catch a
    detachment regression end to end.
  The file's existing "**catalog entries are not debt-able**" preamble already covers the nine
  parity entries; do not restate it per section.
- **T904 — `TESTING_STRATEGY.md` §5/§6 counts re-measured, not arithmetic.** The smoke tier grows
  (nine new catalog entries → the parity theory grows; no new dispatched sweep case, since all
  nine are `ParityOnly`). `Tests.Abwab` and the pipeline rows should not move. Re-verify the
  three-way partition identity (`1,086 + 617 + 140 = 1,843` as of `abwab-relations`) rather than
  adjusting one number. Frontend counts move only if T705's repair changes case counts — state
  the measured value either way. Record the T105 dump regeneration beside the existing note.
- **T905 — Evidence + the user's manual pass.** The verification runs from phases 4 and 8 re-run
  at each PR boundary (there is no CI — every tier is a local gate), plus §10's checklist walked
  by the user. Acceptance is the existing suites green + that walk; do not present the opt-in e2e
  run as a tier.

---

## 8. Testing posture (user decision, in effect)

Unchanged from `abwab-relations`, and it is now the second consecutive feature under it — worth
saying plainly rather than by reference:

- **No new tests are written in this feature** — not backend, not Vitest, not e2e.
- **Parity catalog one-liners are mandatory for every new route** and are *not* tests. **Nine**
  entries (T203 ×2, T305 ×6, T404 ×1). `SmokeCoverageParityTests` fails by name when a registered
  route has no entry, so they are a build-level gate. Debt-ing them would fail the suite.
- **Existing suites must run green before each merge** — backend (`Tests.Abwab`, `Tests.Api`,
  `Tests.Smoke.` with the ran/skipped statement), the full Frontend suite, and `npm run build`.
  Fixing fixtures this feature breaks is maintenance, not new coverage (T705).
- **One row per skipped area in `docs/TESTING_DEBT.md`** (T903), each with its paying trigger.
- Behavioral verification is therefore **existing suites + the §10 manual pass**. No phase
  contains a test task, and no evidence in this feature may claim behavioral coverage it does not
  have. **The debt this posture accrues is now larger than the last feature's** — the deep copy is
  the first non-`CreateAsync` path that writes door rows — which is exactly why T903's second row
  is called out as the highest-value one.

---

## 9. Traps and contract conflicts — do not "fix" these in review

- **The mockup's «إضافة عنصر جذري للقالب…» (`:132-135`) contradicts its own apply contract.**
  One root per template, enforced by a partial unique index (§5.2); that input adds a child of the
  root. Named by line, decision wins.
- **«إعادة تسمية» (`:127`) is not a rename route.** The root is edited through the full authoring
  modal like every other node, so the button becomes «تعديل القالب». Consequence of the user's
  locked content addition, not a design deviation.
- **`abwab_templates` has no `name` column** (§5.2). A denormalized copy of the root's name drifts
  on the first root edit. Do not add one "to simplify the list query" — the join is one query.
- **Aliases are a `text[]` column, not a table** (§5.3), and are **assigned wholesale** — an
  in-place mutation of the tracked array can go undetected by EF.
- **The apply lives in its own writer because `EfAbwabDoorsWriter` is 816 lines against a 600-line
  hard threshold** (§5.6), and because `BACKEND_STRUCTURE.md` §4 prescribes exactly this
  use-case split. Do not "consolidate" it back there.
- **Do not collapse the copy's level-order inserts into one `SaveChanges`** (§5.6). `AbwabDoor` has
  no parent navigation property, so a child's `ParentId` needs its parent's generated id. The
  enclosing transaction is what makes the batch all-or-nothing, not the number of saves.
- **`AbwabTemplateRootNodeException` covers reordering *and* deletion**, not deletion alone — the
  root has no siblings either. Two near-identical types would say the same thing twice; the two
  handlers carry the two messages.
- **Run `Backend/scripts/export-swagger` before `npm run generate:api`** (T501) — the generator reads
  the spec off disk and will happily regenerate the previous contract while reporting success.
- **Rebuild between `dotnet ef migrations add` and `dotnet test --no-build`** (phase 1) — otherwise
  all 46 `Tests.Abwab` fail with `PendingModelChangesWarning`, which looks like a broken migration
  and is not one.
- **The apply's `23505` helper is the *inverse* of the relations case.** There, the door-name-keyed
  helper was wrong; here the collision genuinely **is** a door name. Still pre-check up front so
  the `409` can name the target — `23505` names no row.
- **Do not add a `version` to any templates route** (§5.4) — nothing checks it.
- **Do not build a template↔copy link** (§5.6). No provenance column, no "update all copies", no
  badge on copied doors. The preview copy promises the opposite.
- **Do not union the target count** (§6.1). Selecting an ancestor and its descendant produces two
  copies; that is correct, and it is the opposite of bulk-archive's union rule.
- **Do not guard the concurrent-apply `order_value` race** (§6.4). Door create has the same race,
  reads tolerate gaps, and a guard here would be a rule doors do not have.
- **Do not extract the relations modal's picker** (T801). It has no spec; the unification trigger
  is `TESTING_DEBT.md` row 4's own trigger.
- **The `204` null-envelope trap applies to both delete routes** — `HttpClient` yields `null`, not
  an envelope. This already cost this feature area one production-path bug.
- **`AbwabTreeDoorDto` must not change** (§5.7). If T501 shows it moved, something is wrong.
- **The M10/M33 `sectionId` defense-in-depth stays in the door modal's shell** (T703), not in the
  extracted form.

---

## 10. The user's manual-test checklist

Given §8, this is the behavioral acceptance. Each item maps to a §6 cell. Run against the local
dev DB with at least two sections and one section-less door so those cases are reachable.

**Authoring**

1. `/abwab` → «القوالب» opens the workshop; «العودة للأبواب» returns.
2. «+ قالب جديد» → a template appears in the list with «0 عناصر», its root marked `◆`.
3. «تعديل القالب» on the root opens the **full** authoring modal — name, description,
   representative ayah, alias chips. Save; the list entry's name follows.
4. `＋` on the root adds a child through the same modal; `＋` on that child adds a grandchild; and
   once more at depth 3. *(no depth limit)*
5. The «N عناصر» chip counts descendants and excludes the root.
6. Add a second child with the **same name** under one parent → `409`, Arabic message names it,
   nothing is created. *(§5.5)*
7. Reorder two siblings inline → the order chips renumber `1..N` and survive a reload.
8. Delete a node that has children → the confirm says the subtree goes too; after it, the
   siblings are renumbered `1..N`.
9. Try to delete the root → refused, with the "delete the template instead" message.
10. Delete a template → gone from the list; no archived-templates view exists anywhere.

**Copying — the happy paths**

11. «نسخ إلى أبواب…» → the modal opens; the preview names the template, its element count, states
    that copies cannot be root doors, **and that copies are independent of the template**.
12. The picker lists **live doors only**, expands/collapses at any depth, and search auto-expands
    matching ancestors.
13. Pick **one** door → the button reads «انسخ القالب» → copy → open `/abwab`: that door has a new
    **last** child «X» with the full subtree, all four fields carried on every node.
14. Pick **two** doors → the button reads «انسخ إلى بابين» (not «إلى 2 أبواب») → both get their own
    copy.
15. Copy into a **nested** door (depth 2+) → works; the copy deepens the branch.
16. Copy into a **section-less** door → the whole copied subtree has no section, and it appears
    under «كل الأبواب» only.
17. Copy into a door **inside a section** → every copied node at every depth shows that section's
    tab. *(cascade invariant)*
18. Copy an **empty template** (root only) → the target gains one childless door.
19. Select a door **and its own descendant** → both get a copy, independently. *(§6.1)*

**Copying — the refusals**

20. Copy the same template into the same door twice → `409`, message names the door, **nothing** is
    created.
21. Select three targets where **one** already has a child with the root's name → the whole copy
    fails with one `409` naming that target; the other two get nothing. *(all-or-nothing)*
22. Archive that colliding child, then retry → succeeds. *(the index is live-scoped)*
23. The confirm button is disabled until at least one target is picked, and there is **no**
    root-level option anywhere in the picker.

**Detachment — the cell most likely to be misunderstood**

24. After a copy, edit a node in the **template** (rename it, add an alias) → the copied doors are
    **unchanged**.
25. Add a node to the template → the existing copies do **not** grow.
26. Delete the **template** entirely → the copied doors are still there, fully intact.
27. Edit a **copied door** (rename, move, archive, add a relation to it) → the template is
    unchanged, and the copy behaves as an ordinary door in every operation.

**Doors page unaffected**

28. The copied doors reorder, move, archive, restore, and accept relations exactly like
    hand-created ones; nothing marks them as template-derived.
29. The «القوالب» button is hidden in the archive view.
30. Return to `/abwab` after a copy without a manual reload → the new doors are there.
    *(`ngOnInit → facade.load()`)*

---

## 11. Close checklist — planning-artifact sweep

- **`docs/feature-abwab-doors/` is due for eviction and is DEFERRED to its own chore PR**
  (T101, user decision, 2026-07-29). `abwab-relations` merged as #52 with no close chore, so the
  buffer is `abwab-relations` + `abwab-global-order` and doors is past it. The sweep was measured
  before deferring: **~55 references across ~30 files**, far past the four sites
  `docs/feature-abwab-relations/plan.md` §11 anticipated, so the repoint is its own change with
  its own review. Re-grep at that point; do not trust either list. **This is the first item that
  chore PR owns**, and it is a debt this feature explicitly names rather than one it hides.
- **Closing this feature makes the buffer `abwab-templates` + `abwab-relations`, which evicts
  `docs/feature-abwab-global-order/` and `Backend/report/feature-abwab-global-order/`.** Re-grep
  before deleting — `docs/feature-abwab-relations/plan.md` and both abwab READMEs cite
  global-order's plan by section (§4, §6, §1) — and repoint each reference to code + the nearest
  `README.md`, or fold the fact into that README, **before** the deletion. Dangling links are a
  defect (root `CLAUDE.md`).
- **Pre-existing drift, flagged not fixed:** `docs/feature-032-rate-limiting/` and
  `docs/feature-033-auth-roles-permissions/` are still past the buffer, as
  `docs/feature-abwab-global-order/plan.md` §9 recorded and `abwab-relations` §11 re-flagged.
  Third time; raise separately rather than folding it into a feature commit.
- **Not deletable by this rule:** `Backend/report/feature-008-*` and `feature-009-*` are import
  **evidence** (canonical counts/provenance), protected per file.
- **`EfAbwabDoorsWriter` at 816 lines vs `BACKEND_STRUCTURE.md` §4's 600-line hard threshold**
  (§5.6). Not a sweep item and not this feature's to fix: the apply writer *is* §4's prescribed
  use-case split applied to the new path, and this feature adds nothing to the existing file.

## 12. Obligations checklist

- [x] Migration by EF tooling only, on explicit go-ahead; **local apply only**; name/files/build
      reported, **plus the `text[]` column-type verification** (T104) —
      `20260729162330_AddAbwabTemplates`, `aliases` emitted as a real `text[]` column, both partial
      unique indexes present with `NullsDistinct: false` on the sibling-name one
- [x] Canonical smoke dump regenerated after the migration — a stale dump **fails loud** (T105) —
      sha256 `b14e5bd7…`, head `20260729162330_AddAbwabTemplates`, 23 migrations applied
- [x] **Nine** `SmokeRouteCatalog` entries, all `ParityOnly`, each with a `DerivedStatus`
      (T203 ×2, T305 ×6, T404 ×1). **Mandatory gate, not debt**
- [x] `Tests.Abwab` + `Tests.Api` + `Tests.Smoke.` run at phases 2, 3, 4 and at the Slice A PR
      boundary, each with the `Tests.Smoke.Data` **ran/skipped** statement — PR boundary
      (2026-07-29): full suite **1,843 passed / 0 skipped**, no-pipeline **1,086**, smoke **140
      passed, 0 skipped (data tier RAN)**, `Tests.Abwab` 46, `Tests.Api` 60
- [x] Slice A: `Backend/scripts/export-swagger` → `npm run generate:api` → `npm run docs:api`, and
      `npm run build` clean (T501). Eleven new models; `abwab-tree-door-dto.ts` **unchanged**, as §5.7
      requires
- [x] Slice A live exercise of the apply path, since it has no automated coverage: depth-3 copy into
      two targets preserved order/section/aliases/description/ayah, every refusal answered its
      designed status, the failed apply created nothing, and the copies survived deleting the
      template (2026-07-29; sandbox torn down)
- [ ] Full Frontend suite + `npm run build` before the Slice B PR (T705, phases 7–8)
- [ ] `abwab-door-modal.component.spec.ts` green **unchanged** after the form extraction — the
      verified claim T703 rests on
- [ ] `docs/TESTING_DEBT.md` gains its second section, each row naming its paying trigger (T903)
- [x] `Writes/Abwab/README.md` + `Reads/Abwab/README.md` updated **in Slice A**, counts measured off
      the controllers: **five writers / twenty write endpoints**, three readers / four read
      endpoints, plus the use-case-seam exception to "one seam per aggregate" and the level-order
      copy (T901, moved forward — see phase 9)
- [ ] `features/abwab/README.md`, including the **"Zero dead controls"** correction (T902, Slice B)
- [x] `TESTING_STRATEGY.md` counts re-measured and the partition identity re-verified (T904) —
      `1,086 + 617 + 140 = 1,843`, every term unchanged, which is itself the finding. All nine new
      catalog entries are `ParityOnly`, so the parity gate's two set-comparison `[Fact]`s absorb them
      without adding a case and the dispatched sweep is untouched
- [x] Root `CLAUDE.md` Active-Feature line → `abwab-templates`; N-2 arithmetic verified against
      `git log`, not this document (T101) — `abwab-relations` merged as #52, so
      `docs/feature-abwab-doors/` is the eviction now due; **deferred to its own chore PR** (§11)
- [ ] The §10 manual pass walked by the user before the Slice B merge (T905)
- [ ] Clean-code self-check (`.claude/skills/engineering-review/references/clean-code-guard/`)
      before delivery, per the root `CLAUDE.md`. The test-code self-check applies **narrowly to
      T705's repair pass** — test files are edited, so it is in scope, but there is no new
      coverage for it to judge
- [ ] Branches `abwab-templates-a` and `abwab-templates-b` off `dev`; PRs into `dev`. **Never
      `main`**
