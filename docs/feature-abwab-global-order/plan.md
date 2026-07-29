# Feature: abwab-global-order — an independent root ordering for «كل الأبواب»

Planning document. **Plan only — no code was changed and no Git action was taken while
writing it.**

> Path note: workspace convention is `docs/feature-XXX-feature-name/`. This folder omits the
> numeric prefix, following `docs/feature-abwab-doors/` (the same user instruction).

> Spec Kit note: this feature does **not** populate `specs/`. `specs/` currently holds only its
> `README.md`, and `abwab-doors` was planned entirely under `docs/` — this feature follows that
> precedent. A later implementation review should look for `docs/feature-abwab-global-order/plan.md`,
> not `specs/abwab-global-order/contracts/`.

---

## 0. Guard result — one slice, 27 tasks

| Phase | Tasks | Ends with |
|---|---|---|
| 1 — schema, migration, backfill | 5 | `global_order_value` on `abwab_doors`, backfilled so the superset renders identically |
| 2 — write path, scoped reorder | 8 | `POST {id}/order` takes an explicit scope; every write maintains the global sequence |
| 3 — contract regeneration | 1 | `openapi/swagger.json` → `core/api/generated/` + `docs/api-reference/` |
| 4 — frontend | 7 | Superset sorts and numbers by global order; section views unchanged |
| 5 — e2e, docs, evidence | 6 | One independence flow, all READMEs, re-measured counts |
| **Total** | **27** | Under the ~30 guard — **not split** |

The count is honest, not padded down: the four consumer-ripple items the audit surfaced
(move picker, overlays DTO, cards top-level scope, archive-view confirmation) are folded into
named tasks below rather than left implicit.

---

## 1. Objective

Ordering today is per-scope: `order_value` is unique within `(section_id, parent_id)` and every
write resequences that scope to `1..N` (`plan.md` §4 "Ordering"). The «كل الأبواب» superset
renders roots drawn from **every** section plus the section-less ones, so their numbers collide —
three sections each holding a root numbered `1` render three rows numbered `1`. This is known and
accepted (`abwab-tree.builder.ts:61-64` sorts the superset by that same per-scope value); this
feature is the fix.

Give root doors a **second, independent** order that only the superset uses.

## 2. Scope

- One nullable column, `abwab_doors.global_order_value`, meaningful for **live root doors only**.
- `POST api/abwab/doors/{id}/order` gains an explicit `scope` in its body: `Section` writes the
  existing per-scope `order_value`, `Global` writes `global_order_value`. Route path unchanged.
- Every existing write maintains the global sequence when it changes root membership.
- The tree snapshot exposes `globalOrderValue`; the superset sorts and numbers by it.
- A one-time backfill so the page looks identical on first load.

## 3. Non-goals

- **No coupling between the two orders.** A `Section` reorder never touches `global_order_value`;
  a `Global` reorder never touches `order_value`. This is the feature's whole point, and §5's
  matrix is where it is proven rather than asserted.
- **No global ordering for nested doors.** `global_order_value` is `NULL` at every depth > 0, and
  nested rows render and reorder exactly as they do today, in both views.
- **No global ordering for sections.** `abwab_sections.order_value` is untouched.
- **No drag-and-drop.** Number-click inline editing stays the single reorder affordance
  (`features/abwab/README.md` — "No reorder button … a second control doing the same thing would
  be redundant").
- **No auth change.** The routes stay `Open`; the release block in `features/abwab/README.md`
  still stands.

## 4. Locked decisions

| Area | Decision |
|---|---|
| Column | `abwab_doors.global_order_value`, `int NULL`. Root doors only |
| Invariant | `global_order_value IS NOT NULL ⟺ (parent_id IS NULL AND deleted_at IS NULL)` |
| Reorder scope | `POST {id}/order` body gains `scope`; **explicit and required**. `Section` \| `Global` |
| Coupling | Zero. Each order resequences `1..N` **within its own space** only |
| Restore | **Appends**, in both spaces. Derived from existing semantics — see §5.2 |
| Archive | Removes from the global sequence (`global_order_value → NULL`) and resequences the remaining live roots `1..N-1` |
| Nested rows | Unchanged everywhere — per-scope number, per-scope reorder, both views |
| Superset sort | `globalOrderValue`, `id` as tie-break hardening (mirrors today's `byOrderThenId`) |
| Section view sort | Unchanged: `orderValue`, then `id` |
| Backfill | `ROW_NUMBER() OVER (ORDER BY order_value, id)` over live root doors — exactly today's render order (§6.1 T103) |
| Migration | Authorized. EF tooling only; **local apply only** |
| Branch | `abwab-global-order` off `dev` → PR into `dev`. Never `main` |

---

## 5. The interaction matrix

The invariant in one line:

```
global_order_value IS NOT NULL  ⟺  (parent_id IS NULL AND deleted_at IS NULL)
```

Everything below is a consequence of it. "Global sequence" means the ordered set of live root
doors across **every** section and the section-less ones.

### 5.1 Global order × every write

| Event | Subject's `global_order_value` | Global sequence | Per-scope `order_value` |
|---|---|---|---|
| Create **root** | assigned = live-root-count + 1 (appended last) | grows by 1, stays `1..N` | its `(section, NULL)` scope appends as today |
| Create **nested** | `NULL` | untouched | parent scope appends as today |
| **Edit** (name / description / ayah text / aliases) | unchanged | untouched | untouched — edit never changes membership in either space |
| Reorder `scope=Section` (root) | **unchanged** | **untouched** | its scope renumbered `1..N` |
| Reorder `scope=Section` (nested) | stays `NULL` | untouched | its scope renumbered `1..N` |
| Reorder `scope=Global` (root) | set to the requested position | all live roots renumbered `1..N` | **untouched** |
| Reorder `scope=Global` (nested) | **rejected, `400`** | untouched | untouched |
| **Archive** root (single or bulk) | → `NULL` | remaining live roots renumbered `1..N-1` | its scope renumbered as today |
| **Archive** nested | stays `NULL` | untouched — it was never in it | its scope renumbered as today |
| Archive root **with a subtree** | root → `NULL`; swept-in descendants stay `NULL` | −1 | root's scope renumbered; descendant scopes untouched (membership unchanged) |
| **Restore** root | **appended** = live-root-count + 1 | grows by 1 | scope renumbered with the door appended last (existing) |
| **Restore** nested | stays `NULL` | untouched | scope renumbered with the door appended last (existing) |
| **Restore + detach** (section was archived) | still a root ⇒ **appended**. The detach does not change this | grows by 1 | its **new** `(NULL, parentId)` scope renumbered |
| Move root → nested | → `NULL` | remaining live roots renumbered `1..N-1` | old + destination scopes renumbered |
| Move nested → root | assigned, appended last | grows by 1 | old + destination scopes renumbered |
| Move root → root, **different section** | **unchanged** | **untouched** — root membership did not change | old + destination scopes renumbered |
| **Bulk-move** | per door, by the three move rows above, in the batch's own order | one `ResequenceGlobal` at the end of the command | per existing bulk rules (`Writes/Abwab/README.md`) |
| **Bulk-archive** | every archived root → `NULL` | one `ResequenceGlobal` at the end | per existing bulk rules |
| **Section-less root** (`section_id IS NULL`) | ordinary root — participates fully | same as any root | its scope is `(NULL, NULL)` |
| Section create / rename | unchanged | untouched | untouched |
| Section delete | `409` while live doors remain; an empty section touches no door | untouched | untouched |

The two rows that carry the whole feature are **"Reorder `scope=Section` (root) → global
untouched"** and **"Move root → root, different section → global unchanged"**. Together they say:
a door's position in the superset is changed by exactly one thing — a `Global` reorder — and by
nothing else short of leaving the root set entirely.

### 5.2 Restore appends — derived, not guessed

`EfAbwabDoorsWriter.RestoreAsync` ends with:

```csharp
Resequence(scopeSiblings.Append(door));   // EfAbwabDoorsWriter.cs:395
```

`scopeSiblings` is read `OrderBy(d => d.OrderValue)` and the restored door is appended **last** —
so the existing, shipped semantic is *restore puts the door at the end of its scope*, not back
where it was (it cannot be: the scope was renumbered `1..N-1` when the door left, and no
pre-archive position is stored). The global order follows the same rule for the same reason.
Recorded as a derivation from `EfAbwabDoorsWriter.cs:390-395`, not a new decision.

### 5.3 Section-less doors

`AbwabDoor.SectionId` is nullable on purpose — "a door may sit outside every section, which is
what makes «كل الأبواب» a real superset" (`AbwabDoor.cs:7-9`). Nothing about the global order is
special for them: a section-less **root** is a root, and it holds a `global_order_value` like any
other. Their per-scope order lives in the `(NULL, NULL)` scope, which is the exact source of the
number collision this feature fixes — a section-less root numbered `1` and a section root
numbered `1` render side by side in the superset today.

---

## 6. Traps — do not "fix" these in review

- **No `UNIQUE` index on `global_order_value`.** Renumbering issues one `UPDATE` per row, and a
  Postgres unique index is checked **per statement**, so `1..N` renumbering transiently violates
  it and the write dies mid-transaction. `order_value` has no unique index either, for the same
  reason — a plain index `(section_id, parent_id, order_value)` is all it gets. Consistency here
  is deliberate.
- **`ResequenceGlobal` reads every live root on any root-affecting write.** This is an accepted
  cost, not a regression against `Writes/Abwab/README.md`'s "one parent map per operation" rule:
  the sequence is global by definition, so its scope query cannot be narrowed the way
  `ResequenceSiblingsExcludingAsync` narrows by `(section_id, parent_id)`.
- **Departures and arrivals are handled differently, and both halves must be stated.** Like every
  other scope query in this writer, `ResequenceGlobal` reads the **database**, which still shows
  pre-`SaveChanges` values:
  - *Departures* — archive (`deleted_at` set in memory) and move-to-nested (`parent_id` set in
    memory) still come back from the read, so they are dropped via `excludeIds`, exactly as
    `ResequenceSiblingsExcludingAsync` does today.
  - *Arrivals* — a restored root still shows `deleted_at` set, and a nested→root move still shows
    `parent_id` set, so the read **does not return them at all**. They are appended in code, never
    inferred from the read. One helper that resequences purely from a DB read silently drops the
    door it was supposed to add. This is the same class of bug `Writes/Abwab/README.md` already
    documents twice for the per-scope path.
- **The scope enum's unmapped zero.** `System.Text.Json` leaves a missing property at the enum's
  default. `AbwabReorderScope` therefore starts at `Section = 1`, and the controller guards with
  `Enum.IsDefined` → `400`. An omitted `scope` is a caller bug and is refused; it does not
  silently mean `Section`.
- **Search does not renumber.** The number on a row is its position in its own order space, not
  its position among the filtered rows. Already true per-scope today; far more visible in the
  superset, where a search can leave rows numbered `4, 17, 92`. Correct — do not "fix" it.
- **The archive view shows no order number at all** (`abwab-archive-view.component.html` renders
  none), which is exactly right under the invariant: an archived door's `global_order_value` is
  `NULL`. Confirm and leave it.

---

## 7. Phases

Every phase is one commit. The tree builds and the phase's tier is green at each commit boundary.

### Phase 1 — schema, migration, backfill (5 tasks)

**Files** — `Backend/domain/QuranDashboard.Domain/Abwab/AbwabDoor.cs`;
`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Abwab/AbwabDoorConfiguration.cs`;
`Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/` (generated);
`Backend/tests/QuranDashboard.Tests/Abwab/AbwabSchemaTests.cs`.

- **T101** — `AbwabDoor.GlobalOrderValue` (`int?`). One comment carrying the invariant from §5, not
  a restatement of the type.
- **T102** — `AbwabDoorConfiguration`: `HasColumnName("global_order_value")` and a **non-unique**
  partial index `HasIndex(d => d.GlobalOrderValue).HasFilter("parent_id IS NULL AND deleted_at IS NULL")`
  — it backs the superset's `ORDER BY` and every `ResequenceGlobal` read. **No unique index** (§6).
- **T103** — **STOP CONDITION.** Generate the migration with EF tooling only, on explicit user
  go-ahead (`Backend/CLAUDE.md`). Then append the backfill to the generated migration's `Up()`:

  ```sql
  WITH ordered AS (
      SELECT id, ROW_NUMBER() OVER (ORDER BY order_value, id) AS rn
      FROM abwab_doors
      WHERE parent_id IS NULL AND deleted_at IS NULL
  )
  UPDATE abwab_doors d SET global_order_value = ordered.rn
  FROM ordered WHERE d.id = ordered.id;
  ```

  `ORDER BY order_value, id` is not a choice — it reproduces today's render order exactly:
  `abwab-tree.builder.ts:5-7` sorts roots by `orderValue` then `id`, and `Array.prototype.sort` is
  spec-stable. `Down()` drops the column.

  **Documented deviation:** this appends one `migrationBuilder.Sql(...)` call to the generated
  migration's `Up()` body. `Backend/CLAUDE.md` forbids hand-*writing* migrations and hand-editing
  `.Designer.cs`/snapshot files; neither is touched here, and a data backfill has no other
  reproducible home. Report migration name, generated files, build status, and that
  `database update` ran **locally only**, per the same file's reporting rule.
- **T104** — Schema tests: the column exists with the expected name; the partial index exists; **no
  unique index** on it (a positive assertion, so a later "hardening" PR trips this test).
- **T105** — Backfill evidence. Against the local dev DB, capture the superset root order
  **before** applying and **after**, and assert they match. `AbwabSchemaFixture` runs against an
  empty Testcontainers schema where the backfill is a no-op, so a test cannot cover this — the
  evidence is a real-run capture, recorded in the phase note.

**Verification**

```bash
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab."
```

**Budget** — build ~40 s; `Tests.Abwab` is 36 tests today, Testcontainers-backed (~30 s first run).

---

### Phase 2 — write path and the scoped reorder (8 tasks)

**Files** — `Application.Abstractions/Abwab/` (new `AbwabReorderScope`, `IAbwabDoorsWriter`,
`Responses/AbwabDoorDto.cs`, `Responses/AbwabTreeDto.cs`);
`Application/Abwab/Commands/Doors/ReorderDoor/`;
`Api/Controllers/Abwab/AbwabDoorsController.cs` + `ApiMessages`;
`Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs`;
`Infrastructure/Persistence/Reads/Abwab/EfAbwabTreeReader.cs`;
`tests/QuranDashboard.Tests/Abwab/AbwabDoorWriteBehaviorTests.cs`;
`tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteTests.cs` + `SmokeRouteCatalog.cs`.

- **T201** — `AbwabReorderScope { Section = 1, Global = 2 }` in `Application.Abstractions/Abwab/`.
  Two Arabic `ApiMessages` entries: unknown/absent scope, and `Global` on a nested door.
- **T202** — `ReorderDoorBody`/`ReorderDoorCommand` gain `Scope`. Two new
  `ReorderDoorOutcome` variants — `InvalidScope` and `ScopeNotApplicable` — both mapping to `400`
  in the controller's exhaustive switch, with the `Enum.IsDefined` guard at the controller edge.
- **T203** — `IAbwabDoorsWriter.ReorderAsync` gains the scope. `EfAbwabDoorsWriter.ReorderAsync`
  branches: `Section` keeps today's body verbatim; `Global` loads every live root
  (`ORDER BY global_order_value, id`), refuses a nested subject, bounds `position` to
  `1..live-root-count`, then `ResequenceGlobal`. New private `ResequenceGlobal` beside `Resequence`.
  Both branches keep `SaveTranslatingConcurrencyAsync` — reorder still only moves rows *out of*
  the unique name index's live scope, so a duplicate-name violation stays structurally impossible.
- **T204** — Global maintenance in `CreateAsync`, `MoveAsync`, `BulkMoveAsync`, per §5.1. The three
  move rows are the discriminating cases: root→nested, nested→root, and root→root-across-sections
  (which must leave the global sequence **untouched**).
- **T205** — Global maintenance in `DeleteAsync`, `BulkArchiveAsync`, `RestoreAsync`, per §5.1.
  Restore appends (§5.2). The restore-detach path must not disturb the global value it just
  assigned — the detach changes `section_id`, never root membership.
- **T206** — `AbwabDoorDto` and `AbwabTreeDoorDto` gain `int? GlobalOrderValue`;
  `EfAbwabTreeReader` projects it. **Added to both**: every write returning `AbwabDoorDto` can
  change the global order, and a `Global` reorder whose 200 response omitted the field it just
  changed would be silently incomplete. The reader's own `ORDER BY` is unchanged — it stays
  scope-ordered and the client sorts, matching `Reads/Abwab/README.md`'s "flat, not nested".
- **T207** — `AbwabDoorWriteBehaviorTests`: one test per §5.1 row that can actually differ, data-driven
  where the rows are variants of one shape. Non-negotiable cases: `Section` reorder leaves
  `global_order_value` untouched; `Global` reorder leaves every `order_value` untouched; root→root
  across sections leaves the global sequence untouched; archive nulls and resequences `1..N-1`;
  restore appends; a section-less root sequences with the rest.
- **T208** — `SmokeAbwabWriteTests`: `Global` reorder 200; `400` on an out-of-range global position;
  `400` on `Global` for a nested door; `400` on an absent/unknown scope; **`409` on a stale token
  for each scope**. `SmokeRouteCatalog` needs **no new entry** — `POST api/abwab/doors/{id}/order`
  already exists as a `ParityOnly` marker (`SmokeRouteCatalog.cs:254-257`) and the path is
  unchanged — but its comment must record the **body change**, and its `DerivedStatus` re-checked.
  `DerivedStatus` documents what a **well-formed** call answers, and after this change well-formed
  means "carries a valid `scope`" — so the derived status is expected to stay `404` (door 1 does
  not exist against the empty schema). What changed is the definition of well-formed, not the
  status. Confirm rather than assume, and do not edit the status to `400`.

**Verification** — the body change touches request contracts and model binding, so per
`Backend/CLAUDE.md` the smoke filter is **required alongside** the `Tests.Api.*` families:

```bash
dotnet build Backend/QuranDashboard.sln
dotnet test … --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab."
dotnet test … --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api"
dotnet test … --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

The evidence **must state whether `Tests.Smoke.Data` ran or skipped** (it self-skips when
`resources/db-dumps/quran-canonical/` is absent; a stale dump fails loud).

**Budget** — `Tests.Api` 60 tests ~10 s; smoke 134 tests ~51-52 s. Both counts move this phase.

---

### Phase 3 — contract regeneration (1 task)

- **T301** — `npm run generate:api` (`ng-openapi-gen` + `scripts/prune-generated-api.mjs`) and
  `npm run docs:api`. Confirm `abwab-tree-door-dto.ts` and `abwab-door-dto.ts` carry
  `globalOrderValue`, and that the scope enum and the widened `reorder-door-body.ts` are present.
  This is the phase 2/4 handoff artifact — phase 4 cannot start before it.

**Verification** — `npm run build` (the generated models must typecheck before anything consumes
them). Expect `abwab-page-overlays.controller.ts` to break here; T401 fixes it.

---

### Phase 4 — frontend (7 tasks)

**Files** — `features/abwab/models/abwab.models.ts`; `state/abwab-tree.builder.ts`;
`state/abwab-write.controller.ts`; `state/abwab-page-overlays.controller.ts`;
`components/abwab-tree/`; `components/abwab-cards/`; `components/abwab-move-picker/`;
`pages/abwab-page/`; the matching `*.spec.ts`.

- **T401** — `AbwabNode.globalOrderValue: number | null`; the builder carries it through `build()`.
  **Includes the `AbwabDoorDto` ripple:** `abwab-page-overlays.controller.ts:45-53` hand-builds a
  DTO-shaped object from a node and stops typechecking the moment the generated DTO grows a field.
  One line, but it is a real break, so it is named here rather than discovered.
- **T402** — Ordering. `buildAbwabTreeSnapshot` sorts `liveRoots` by `globalOrderValue` then `id`;
  `filterAbwabRootsBySection` re-sorts by `orderValue` then `id` whenever `sectionId !== null`.
  `archivedRoots` keeps today's `byOrderThenId` — archived doors have no global value.
  Both signatures return `readonly AbwabNode[]`, which has no `.sort()`; the section branch already
  returns a fresh array from `.filter(...)`, so sort that copy. **Do not widen the type to
  `AbwabNode[]` to make `.sort()` compile** — that removes the one guard against an in-place sort
  of the shared snapshot array.
  **Audit obligation:** `liveRoots` has a second consumer —
  `abwab-move-picker.component.ts:38,74` builds its flat destination list from it, so this sort
  change silently reorders the picker. Decide and record: the picker is a *destination* list, not
  an ordered outline, and following the superset's global order there is coherent — but it must be
  a stated decision with a spec pinning it, not a side effect.
- **T403** — `abwab-tree`: an `orderScope: 'global' | 'section'` input. Depth-0 rows render
  `globalOrderValue` under `'global'`, `orderValue` otherwise; **rows at depth > 0 always render
  `orderValue`**. The inline editor commits against the same space and the `reorder` output carries
  the scope.
- **T404** — `abwab-cards`: the same rule, **top level only**
  (`abwab-cards.component.html:46`). Global applies when the superset is active **and** `cardId` is
  null; every drilled-in level is a per-parent scope and stays on `orderValue`.
- **T405** — `abwab-page`: derives the scope from `activeSectionId()` (null ⇒ `'global'`), passes it
  to tree and cards, and forwards it on dispatch (`abwab-page.component.ts:224`).
- **T406** — `abwab-write.controller.ts`: `reorderDoor` passes the scope through; `abwab.labels.ts`
  gets copy for the two new `400`s **only if** the outcome→message mapping does not already prefer
  the backend's own Arabic message. Check first — `features/abwab/README.md` records that a
  plan-authored string which the backend always overrides is dead code, and that mistake is not
  worth repeating.
- **T407** — Specs: builder (both sorts, the tie-break, archived roots untouched), tree (both
  scopes, nested rows unchanged, the emitted scope), cards (top-level only), page (scope
  derivation), write controller (scope on the wire), move picker (the T402 decision).

**Verification**

```bash
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/features/abwab/**/*.spec.ts"   # Tier A, focused
npm test                                                       # Tier B/C, full suite
npm run build                                                  # required before the PR
```

**Budget** — full Frontend suite is 190 files / 2,142 tests / ~207 s today; both numbers move.

---

### Phase 5 — e2e, docs, evidence (6 tasks)

- **T501** — One e2e flow proving independence: in the superset, read the three sandbox doors'
  global numbers from the DOM and reorder one past another; assert their **relative** order
  changed and the section view's `1, 2, 3` did **not**. Then reorder in the section view and assert
  the superset's relative order did not change. **Assert relative order and the sandbox's own ids
  only — never an absolute global number and never a global count** (`TESTING_STRATEGY.md` §6 pins
  that rule for these specs).
- **T502** — The parallelism hazard, **measured, then decided**. See §8 R1. Verify first that
  Playwright's per-project options do **not** include `workers` (`playwright.config.ts` sets it at
  top level; `projects:` carries only `name`/`use`). If confirmed, the two implementable options are:
  (a) split `npm run e2e` into two sequential invocations — non-abwab at 2 workers, the four abwab
  specs at `--workers=1`; or (b) accept and document the window, leaning on the teardown's existing
  re-read-version-before-archive hardening. **Retry-on-409 is not an option** — this feature's own
  policy is that 409s are always surfaced, never swallowed or auto-retried
  (`features/abwab/README.md`). Record the choice with the measured e2e time behind it.
- **T503** — `Persistence/Writes/Abwab/README.md`: the invariant, `ResequenceGlobal`'s
  whole-root-set read as an accepted cost, the no-unique-index trap, restore-appends-in-both-spaces.
  `Persistence/Reads/Abwab/README.md`: `GlobalOrderValue` is `NULL` for nested and archived doors,
  and the reader stays scope-ordered while the client sorts.
- **T504** — `features/abwab/README.md`: the two order spaces, which view uses which, the scope on
  the reorder wire, nested-rows-unchanged, and the move-picker decision from T402. **Plus a
  correction that is easy to miss:** the "Refresh-after-write is an invariant" gotcha currently
  reasons that a write bumps every cached token *"in that scope"*. After this feature a
  root-affecting write bumps `xmin` on **every live root everywhere**. The conclusion is unchanged
  (the controller already refetches the whole snapshot and rebinds every version, so no frontend
  code moves), but the stated scope is now wrong — and it is the exact sentence a future
  implementer reads to decide whether a narrower refresh would be safe. It would no longer be.
  `e2e/README.md`: the T502 outcome and the global-resequence residue.
- **T505** — `TESTING_STRATEGY.md` §5/§6 counts **re-measured, not adjusted by arithmetic**: the
  no-pipeline run (1,076 today), the smoke tier (134), the full Backend suite (1,827), **and the
  three-way partition identity** `1,076 + 617 + 134 = 1,827` re-verified; the full Frontend suite
  (190 files / 2,142 tests / ~207 s); the e2e run (47 / ~1.6 m — and this one changes twice, once
  for the new flow and once for whatever T502 chooses).
- **T506** — Root `CLAUDE.md`: move the Active-Feature line to `abwab-global-order` and this plan.
  Record the close-checklist arithmetic from §9.

**Verification** — `npm run e2e`, plus the Tier C backend + frontend runs from phases 2 and 4
re-run at the PR boundary. There is no CI (`TESTING_STRATEGY.md` §8): every tier is a local gate
and "CI is green" is never available as evidence.

---

## 8. Risks

- **R1 — Cross-worker `409`s in the e2e suite. New, and caused by this feature.** Before it, a
  resequence was confined to one `(section_id, parent_id)` scope, so two workers in two disjoint
  sandbox sections could never touch each other's rows. Global resequencing bumps `xmin` on **every
  live root** — and every abwab spec's teardown archives sandbox roots, which now triggers one. So
  worker B can hold a version that worker A's teardown invalidated, and B's next root write `409`s.
  This hits all four abwab specs, not just the new flow. T502 owns the decision; it is listed as a
  risk and not as a solved problem because the mitigation is a measured trade, not a config line.
- **R2 — The global sequence is dev-DB-wide.** A `Global` reorder in the e2e suite renumbers every
  live root in the local dev database, sandbox or not. Resequencing is order-preserving for
  untouched rows, and teardown removes the sandbox roots again, so the residue is a permutation of
  nothing — but it **is** a write outside the sandbox's blast radius and belongs in `e2e/README.md`
  beside the residue already documented there.
- **R3 — The backfill is one-shot and unverifiable by test.** T105's real-run capture is the only
  evidence. A dev DB that took writes between the capture and the apply invalidates it; capture
  immediately before applying.
- **R4 — Two order spaces, one number widget.** The tree's inline editor is the same control in
  both views writing to different columns. T403's `orderScope` input is the seam; a component that
  guesses from anything other than that input (route params, a service) is how the two spaces get
  silently coupled again.

---

## 9. Close checklist — planning-artifact sweep

The N-2 arithmetic, worked out rather than restated. Merge dates from `git log --merges`:

| Feature | Merged into `dev` |
|---|---|
| `abwab-doors` (#48 + #49) | 2026-07-29 |
| `smoke-harness` (#47) | 2026-07-28 |
| `playwright-bootstrap` (#46, `e2e-bootstrap`) | 2026-07-28, before #47 |
| `033-auth-roles-permissions` (#38) | 2026-07-19 |
| `032-rate-limiting` (#36) | 2026-07-18 |

- **Opening this feature closes `abwab-doors`** (its Active-Feature line is replaced in T506).
  The buffer becomes `abwab-doors` + `smoke-harness`, which **evicts
  `docs/feature-playwright-bootstrap/`** — the candidate named in this feature's brief. ✔
- **Closing this feature** makes the buffer `abwab-global-order` + `abwab-doors`, which evicts
  `docs/feature-smoke-harness/` next.
- **`docs/feature-abwab-doors/` must survive until this feature closes** regardless of the buffer:
  this plan cites `plan.md` §4 (Ordering) as the rule it changes.
- **Pre-existing drift, out of scope, flagged not fixed:** `docs/feature-032-rate-limiting/` and
  `docs/feature-033-auth-roles-permissions/` are already past the N-2 buffer and were not swept
  when they should have been. Raise separately rather than folding an unrelated deletion into this
  PR.
- **Repoint before deleting.** Per the root `CLAUDE.md`, `grep -rn` the whole repo for every path
  removed — code, tests, skills, data files, READMEs, `.specify/` — and repoint or fold the fact
  into the nearest `README.md` first. Dangling links are a defect.

## 10. Obligations checklist

- [ ] `SmokeRouteCatalog` — no new entry (path unchanged); **body-change note** + `DerivedStatus`
      re-checked (T208)
- [ ] Write smokes for the new scope **including `409`s** (T208)
- [ ] `Tests.Api.*` **and** `Tests.Smoke.*` run, with `Tests.Smoke.Data` ran/skipped stated (phase 2)
- [ ] One e2e flow: superset reorder → section reorder → independence (T501)
- [ ] `TESTING_STRATEGY.md` counts re-measured, partition identity re-verified (T505)
- [ ] `features/abwab/README.md` + `Writes/Abwab/README.md` + `Reads/Abwab/README.md` +
      `e2e/README.md` updated in the same change (T503, T504)
- [ ] Root `CLAUDE.md` Active-Feature line (T506)
- [ ] Close-checklist sweep note (§9, T506)
- [ ] Clean-code self-check (`.claude/skills/engineering-review/references/clean-code-guard/`) and
      test-code self-check before delivery, per the root `CLAUDE.md`
- [ ] Branch `abwab-global-order` off `dev`; PR into `dev`. **Never `main`**
