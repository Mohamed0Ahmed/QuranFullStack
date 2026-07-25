# Research: Abwab Relationships and Templates — Category Adjuncts

**Feature**: `030-abwab-relationships-templates` | **Date**: 2026-07-25 | **Source**: Master Plan §18.4

The spec is clarification-free (0 `[NEEDS CLARIFICATION]` markers). This document records the
technical decisions that realize §18.4 against the existing stack and the accepted `028`/`029`
substrate. Each decision is constrained by §18.4 and the repository; none introduces new product
scope. §18.4 defines **no numbered internal order** — the two workstreams may run **in parallel**,
and each must finish **its own adapter and vertical slice** before the Spec Kit exits.

## Workstream A — Category relationships

### A1. One typed table carrying both shapes

- **Decision**: One `CategoryRelationship` table with `CategoryRelationshipId`, `RelationshipType`,
  the mutual pair (`LowerCategoryId`, `HigherCategoryId`), the directional pair
  (`SourceCategoryId`, `TargetCategoryId`), soft-delete metadata, and `Version` (xmin, mapped
  explicitly as the `029` entities do). CHECK constraints enforce **one shape per row** (mutual
  columns non-null ⇔ directional columns null, and vice-versa), **canonical ordering**
  (`LowerCategoryId < HigherCategoryId`), and **no self-link**. Filtered unique indexes over active
  rows prevent duplicate mutual pairs **per type** and duplicate directional edges. The
  Broader/Narrower inverse label is **derived for display**, never a second stored row.
- **Rationale**: §7.3 specifies exactly this shape; the canonical lower/higher ordering is what makes
  a *reverse* duplicate collapse into the same index key, so the reverse-duplicate case is a DB
  guarantee rather than an application check. Storing the inverse as a row would double the
  protection/audit surface and create a second thing to keep consistent.
- **Alternatives considered**: Two tables (mutual + directional) — rejected, §7.3 says one table with
  a typed shape. Storing both directions of a mutual pair — rejected, defeats the canonical-order
  unique index. Application-only duplicate checks — rejected, races; the filtered unique index is the
  final guard, mapped to `abwab.relationship_duplicate` exactly as `029` maps its named constraints.

### A2. Cycle-safe Broader/Narrower under the transaction

- **Decision**: Validate directional cycles **inside** the writer transaction, after the `028`
  barrier + `AbwabRevisionState` locks are held, by walking the existing active directional edges
  from the proposed narrower endpoint upward and rejecting if the proposed broader endpoint is
  reachable. An explicit direct **A→C is allowed** even when A→B→C exists — only a genuine cycle is
  refused. Conflicts map to `abwab.relationship_cycle`.
- **Rationale**: §7.3 requires rejection **under the transaction**; §18.4 requires the
  **race-created cycle** to be proven. Because every Abwab writer already serializes on the locked
  revision singleton (`AbwabAuditedCommitExecutor`), two concurrent edge inserts cannot interleave
  their validation — the second one revalidates against the first one's committed state. No
  additional advisory lock or transitive-closure table is needed.
- **Alternatives considered**: A materialized transitive-closure/reachability table — rejected as
  unnecessary state to keep correct across soft delete/restore and a source of its own races.
  Post-commit background cycle detection — rejected, fail-closed validation must precede the write.
  Rejecting transitive A→C — rejected, §7.3 explicitly allows it.

### A3. Endpoint protection targets and the ordinary-window exclusion

- **Decision**: Resolve `Relationship`-type manual protection through the accepted `029`
  `ManualProtectionResolution` / `ProtectionResolver` (direct + inherited via current `AncestorIds`),
  over the exact §7.3 target sets: **proposed** endpoints on add, **current ∪ proposed** on edit, and
  **stored** endpoints on delete/restore. Any applicable direct or inherited protection on **any**
  target blocks the **entire** mutation (`abwab.manual_protection`). Relationship writers neither
  read nor write the ordinary 24-hour actor/time fields.
- **Rationale**: §7.3's union rule is precisely what prevents an edit from escaping protection by
  replacing a protected endpoint; making it a single resolved target-set check (rather than
  per-endpoint short-circuits) keeps that guarantee visible in one place. §9 and the §2.1
  supersession put relationship mutations outside the ordinary window in both directions.
- **Alternatives considered**: Checking only the changed endpoint on edit — rejected, that is the
  exact escape §7.3 closes. Re-implementing inheritance locally — rejected, `029` owns the one shared
  resolution rule and duplicating it is how the single-read and batch-read paths drift.

### A4. Tracked soft delete, restore, and restore collision

- **Decision**: Delete/restore are tracked soft delete/restore through the `028` audited executor;
  physical delete stays rejected by the `AbwabWriteGuardInterceptor`. **Restore revalidates** the
  filtered unique index under the transaction, so restoring a row whose canonical pair/edge became
  active again fails with `abwab.relationship_duplicate` rather than creating a second active row.
  Stale expectations map to `abwab.row_stale`.
- **Rationale**: §18.4 names **restore collision** as a required proof, and the only race-safe place
  to detect it is the same transaction that reactivates the row. This mirrors how `029` revalidates
  names on category operation-restore.
- **Alternatives considered**: Unconditional restore with later cleanup — rejected, it would create
  two active rows for one canonical relationship. Partial unique index over all rows rather than
  active rows — rejected, it would forbid keeping deleted history.

### A5. Real dormancy across category subtree deletion

- **Decision**: Relationship rows reference categories with **RESTRICT / no-cascade** FKs and carry
  no deletion state of their own: when a category subtree is soft-deleted, its relationship rows stay
  **present and unchanged** and are filtered out of read projections as **dormant**; category
  **operation-restore** makes the same rows (same IDs, same history) visible again. Stored-endpoint
  protection is enforced on both paths. This replaces `029`'s generic dependent-visibility **core
  fixture** with the real thing.
- **Rationale**: §18.4 requires exactly this and requires real-PostgreSQL proof of **no cascade and
  no history loss**; `029` deliberately shipped only a generic seam so it would carry no forward
  schema dependency. Keeping dormancy a *read projection* rather than a written flag means a category
  restore needs no relationship-side write to reverse.
- **Alternatives considered**: Cascade-deleting relationship rows with the subtree — rejected, §7.1
  makes dependents dormant, not deleted. Writing an `IsDormant` column — rejected, it duplicates
  derivable state and would need its own restore inversion.

### A6. Relationship vertical slice

- **Decision**: Own the relationship port + mock + HTTP adapter + parity suite, the relationship
  actions/panel UI (explicit actions only), relationship cache keys with post-commit-only publication,
  the **specialized relationship audit payload**, and the versioned `RelationshipRestoreAdapter`.
  Reuse the `029` data-access conventions verbatim: reads carry server `TimelineGeneration`, mutations
  never synthesize one, and a conflict invalidates + reloads rather than reconciling locally.
- **Rationale**: §14.1 assigns `030` the relationship/template ports, mocks, HTTP mappings, UI, parity
  tests, and cache behavior; the parity suite is what keeps the mock a safe stand-in, and `029`'s
  invalidate-and-reload rule already removes the optimistic-undo class of bugs.
- **Alternatives considered**: Extending the `029` core port with relationship methods — rejected,
  §14.1 gives each domain its own port; a mega-port is explicitly excluded. Client-side merge of a
  stale relationship list — rejected, §14.3 forbids silent retry/automatic merge.

## Workstream B — Door templates

### B1. Manual-editor-only aggregate

- **Decision**: `DoorTemplate` + `TemplateNode` + `TemplateNodeSearchAlias` are created and edited
  **only** through template-editor commands. There is **no** endpoint, command, service, or UI action
  that reads real categories into a template, and no cross-door copy path. A source/architecture test
  plus negative API tests assert the absence.
- **Rationale**: §7.4 and §18.4 state the prohibition as a hard invariant; proving absence needs a
  static gate (no such handler/route exists) in addition to behavioural negatives, because an absent
  path cannot be exercised.
- **Alternatives considered**: A "seed template from an existing door" convenience — rejected,
  explicitly forbidden. Marking such a path as internal-only — rejected, §7.4 admits no exception.

### B2. `TemplateRevision`-guarded node structure

- **Decision**: Node create/reparent/reorder carry **expected `TemplateRevision`** plus the row's
  expected `xmin`, run on tracked rows inside one audited operation, reject **self-parenting** and a
  destination **inside the moved node's descendant tree**, validate the parent chain under the
  transaction, update affected sibling orders **atomically**, and bump `TemplateRevision` **exactly
  once** per grouped operation. Stale expectations map to `abwab.template_revision_stale` /
  `abwab.row_stale`; a cyclic structure — including one a restore would produce — maps to
  `abwab.template_cycle`. No cyclic template can be saved, applied, rendered, or restored.
- **Rationale**: §7.4 specifies this verbatim and §6.4 fixes the "one bump per grouped operation"
  rule for aggregate logical counters. Mirroring the `029` ordering handler's tracked-atomic-rewrite
  approach keeps the two tree implementations behaviourally consistent.
- **Alternatives considered**: Fractional/sparse ordering keys to avoid sibling rewrites — rejected,
  §7.4 requires explicit `SiblingOrder` with atomic sibling updates, matching `029`. Bumping
  `TemplateRevision` per touched row — rejected, §6.4 says once per grouped operation.

### B3. One-target application through the `029` category writer

- **Decision**: Application runs as **one audited operation** that calls the accepted `029` category
  writer to create every template root as a **direct child** of the target category and recursively
  copy **only** name, representative excerpt, description, aliases, order, and structure. Destination
  uniqueness (§5.1 normalization), manual `InternalStructure` protection on the target, current
  category state, concurrency, and order allocation are **revalidated inside that one transaction**;
  the result is **one ChangeSet** and **one `TreeRevision`** bump. Conflicts reuse the `029` codes
  (`abwab.category_name_conflict`, `abwab.manual_protection`, `abwab.category_unavailable`,
  `abwab.tree_revision_stale`, `abwab.timeline_generation_stale`). The `029` writer is reused
  through a **behavior-preserving grouped creation seam**: the in-transaction creation core of
  `CategoryContentHandler` (normalization, tree/name guards, protection gate, order allocation) is
  extracted so both the existing single-add path and the application handler run it — the
  application inside **one** audited operation. The writer is never forked, and a regression
  assertion proves `029` single-add behavior unchanged.
- **Rationale**: §7.4/§18.4 require application to produce ordinary, independent real categories;
  routing through the one `029` writer is what makes that true by construction — the created rows are
  indistinguishable from hand-created ones, so §8's "keyed by persisted type" rule holds and no
  second Category adapter is needed.
- **Alternatives considered**: Calling the unmodified single-add writer N times — rejected:
  `CategoryContentHandler.AddAsync` opens one audited operation and bumps `TreeRevision` per call,
  so N calls produce N ChangeSets and N bumps, violating §7.4. A dedicated template-application
  category writer — rejected, it would
  fork the category invariants and imply a second Category adapter. Multiple ChangeSets (one per
  created node) — rejected, §7.4 makes application one ChangeSet and one `TreeRevision` bump.
  Pre-validating uniqueness outside the transaction — rejected, races; revalidation must be under the
  transaction.

### B4. Strict copy allowlist

- **Decision**: The copy set is an **allowlist** in code (name, representative excerpt, description,
  aliases, order, structure). Links, ayah members, highlights, notes, requests, sources, decisions,
  notifications, audit/workflow history, and technical revisions are **never** copied; negative tests
  assert each family is absent on the produced tree, and the created categories start with their own
  fresh revision/technical state.
- **Rationale**: §7.4 enumerates both the copied set and the forbidden set. An allowlist fails closed
  when a later Kit adds a new child family (e.g. `031` links): the new family is simply not copied,
  with no edit required here.
- **Alternatives considered**: A denylist of forbidden families — rejected, it fails **open** for
  anything added later. Copying "harmless" descriptive metadata beyond the list — rejected, the list
  is exact.

### B5. Alias soft delete and history

- **Decision**: `TemplateNodeSearchAlias` mirrors the category-alias contract: value + normalized
  value (§5.1 `ArabicNameNormalizer`), tracked **soft delete** on remove and tracked restore, physical
  delete rejected by the `028` guard, and full round-trip through the one Template adapter so alias
  history is never lost.
- **Rationale**: §7.4 says the alias mirrors the category alias contract, and §18.4 demands
  physical-delete and adapter tests that prohibit losing alias history.
- **Alternatives considered**: Hard-deleting template aliases because templates are "just drafts" —
  rejected, they are reversible product state in §8 with their own adapter obligations.

### B6. Frozen permission ownership

- **Decision**: Enforce at the handler: `template.add` creates **only** the aggregate; **every**
  node/alias add/edit/reparent/reorder/internal remove requires `template.edit`; aggregate lifecycle
  delete/restore require `template.delete` / `template.restore`; real-category application requires
  `template.apply`. Handler, source/architecture, mock/HTTP parity, and negative-permission tests
  cover a **partial-grant matrix** proving no verb is borrowed and frontend hiding authorizes nothing.
- **Rationale**: §5.2 freezes this mapping explicitly (aggregate subresources invent no child-CRUD
  permissions), and §18.4 requires the partial-grant proof. This is the same discipline `029` applied
  to `category.edit` owning alias mutations.
- **Alternatives considered**: Node-level `templateNode.*` codes — rejected, forbidden by §5.2.
  Letting `template.edit` imply `template.apply` — rejected, application is a real-category write with
  its own code.

### B7. Template audit surfaces

- **Decision**: Template application stores and renders the **frozen template snapshot at application
  time** plus target/path, the complete created tree, all copied basic fields, and counts by level;
  later template edits cannot change that rendering. Template **CRUD** renders in the **separate
  template-history view** over the same append-only audit engine and produces **no** main
  product-audit row. `030` defines and publishes these payload shapes only; the audit page/read model
  stays `033`'s.
- **Rationale**: §6.3 assigns exactly these presentations, and `029` already established the
  "publish the payload shape, not the audit page" boundary for domain Kits.
- **Alternatives considered**: Storing a template reference instead of a frozen snapshot — rejected,
  later edits would rewrite history. Putting template CRUD in the main log — rejected, §6.3 keeps it
  in the separate history view.

### B8. One aggregate adapter + a non-adapter application interpreter

- **Decision**: Register **one** `DoorTemplateRestoreAdapter` for the whole aggregate (DoorTemplate +
  TemplateNode + TemplateNodeSearchAlias). Register the **versioned application-event interpreter**
  as an event-kind interpreter, **not** an `IAbwabRestoreAdapterDescriptor` — it maps a
  template-application event to real-category inversion performed by the **single `029`
  `CategoryRestoreAdapter`**. Extend the existing static §8 registry test to assert the registered
  adapter set is exactly `{Section, Category, ManualProtection, Relationship, DoorTemplate}` and to
  fail CI on any duplicate (notably a "template-created category" adapter) or missing registration.
- **Rationale**: §8 is keyed by **persisted type** and states that template application creates
  ordinary Category rows and therefore uses the one Category adapter; §18.4 demands the interpreter's
  reuse be *verified* with no duplicate registry entry. Extending the existing `029` registry test is
  the cheapest place to make a duplicate fail CI.
- **Alternatives considered**: A second Category-shaped adapter for template-created rows — rejected,
  a §8 duplicate. Three template adapters (one per persisted child type) — rejected, §8 registers the
  DoorTemplate **aggregate** as one adapter. Making the interpreter an adapter descriptor — rejected,
  it would add a registry entry §18.4 forbids.

## Cross-cutting

- **Parallel workstreams, one exit gate**: A and B share no schema and no handler; they may be built
  concurrently. The Spec Kit exits only when **both** have their own accepted adapter and their own
  finished vertical slice (backend + port/mock/HTTP + UI + parity + browser proof).
- **Additive migrations, one per workstream**: relationships and the template aggregate ship as
  separate additive migrations generated by the repository's normal EF tooling, so neither workstream
  waits on the other's schema; both are validated against real PostgreSQL and against the model
  snapshot. No destructive or backfilling step is required (§15.1).
- **Real infrastructure where it matters**: shape CHECKs/indexes, duplicate/reverse-duplicate, cycle
  and race-created cycle, protection unions, restore collision, subtree dormancy + restore visibility,
  template reparent/reorder/cyclic-restore, revision bump counts, alias history, permission matrices,
  and adapter round-trips are proven against **real PostgreSQL** (Testcontainers); parity, RTL,
  explicit-action/no-drag, and context preservation are proven in a **real browser** (Playwright).
- **No new contracts invented**: only existing §11 codes and §5.2 permissions are used; no code is
  added, renamed, or remapped, and no new frontend substrate is installed.
- **Boundaries held**: `030` builds no `028` kernel/CI work, no `029` core category/section/protection
  behavior, no Quran FK or link/source structure (`031`), no workspace/request/review/notification
  surface (`032`), no restore preview/planner/execution or audit read model (`033`), and no realtime
  transport (`034`).
