# Abwab door inclusions — synchronized-link pre-Spec plan

- Status: decision-complete pre-Spec input; no implementation authorized
- Feature: 037 — Abwab door inclusions
- Working branch: `feat/abwab-chapter-inclusion`
- Delivery: directed inclusion graph, durable one-way link synchronization, existing link UI
  integration, inclusion management UI, generated contracts, then verification
- Access: public topology reads; permission-classified inclusion writes; existing Owner-only link
  mutations remain unchanged
- Database: additive schema and linking-storage change; generated EF migration requires separate
  explicit authorization

## 1. Required outcome

An Abwab door can include the linked Quran records of one or more other doors without changing the
section, parent, or tree position of any participating door. The including door is the **target** or
**aggregate door**. Each included door is a **source door**.

Inclusion is a durable, one-way synchronization relationship:

```text
source door link state  ->  target door synchronized link state
```

Creating or relinking a source record creates a synchronized target record. Editing or deleting a
source record updates or removes its still-synchronized target record. A target mutation never
changes the source. Target-local deletion suppresses only the current source record occurrence; an
edit of that same source record cannot bring it back. It may return only after that source record is
deleted and a newly created source record is linked again.

Synchronized records are real target-door link records and appear through the existing Abwab link
count, list, ayah rendering, selected-word colors, edit, delete, and copy surface. The content UI
does not expose where an ayah, word, or record came from. Source identity and synchronization state
are internal persistence details only.

The feature is not a fourth semantic door relation and does not introduce a separate effective-
content screen.

## 2. Terminology

Use these names consistently in specification, code, API, and Arabic UI copy:

| Concept | English code/API term | Arabic UI term |
|---|---|---|
| Capability | Door inclusion | تضمين الأبواب |
| Door receiving synchronized links | Target door / aggregate door | الباب الجامع |
| Door supplying links | Source door / included door | الباب المُضمَّن / مصدر المحتوى |
| Stored directed edge | Door inclusion | تضمين |
| One source record lifetime | Source record occurrence | نسخة الربط الحالية |
| Materialized target record | Synchronized record | رابط في الباب الجامع |
| Target deletion of current occurrence | Local suppression | حذف من الباب الجامع |
| Target edit of synchronized occurrence | Local override | تعديل داخل الباب الجامع |

Do not call an inclusion a parent, child, hierarchy edge, comprehensiveness relation,
bidirectional relation, read-time union, or manual copy.

## 3. Locked product and domain rules

### 3.1 Inclusion graph

1. An inclusion is directed: `TargetDoorId` includes `SourceDoorId`.
2. Door placement is irrelevant. Target and source may be in different sections and at unrelated
   tree depths.
3. Existing `AbwabDoorRelation` rows remain semantic metadata only. No inclusion is stored in
   `abwab_door_relations`, and no `AbwabRelationType` member is added.
4. Inclusion synchronization is transitive. If A includes B and B includes C, a synchronized record
   entering or leaving B is synchronized onward to A.
5. The active inclusion graph must remain acyclic. Self-inclusion and a batch that would create any
   cycle are rejected atomically.
6. Multiple active paths are permitted. Each hop has independent synchronization ownership; the
   existing per-door ayah/word union remains responsible for deduplicating final door-level Quran
   membership.
7. The same direct active target/source pair cannot exist twice.
8. A target may also contain ordinary direct records authored through the existing linking flow.
9. Inclusion topology and semantic relations may coexist between the same doors because they
   express different facts.
10. V1 has no authored order for source doors and no multi-target inclusion command.

### 3.2 One-way synchronization

1. Adding a new source `LinkingUnit` occurrence creates a synchronized target `LinkingUnit`.
2. The target clone preserves the source record's grouped/independent shape, ayah membership,
   selected canonical Quran word IDs, and descriptions.
3. Editing an existing source occurrence updates every still-active synchronized clone.
4. Deleting an existing source occurrence removes every active or target-overridden clone created
   from that occurrence.
5. A target mutation never writes to the source door, its contribution, unit, ayahs, words,
   descriptions, operation, or version.
6. Synchronization writes update the target's existing relational door ayah/word union so the same
   ayah and selected-word pairs keep the current distinct door-level semantics.
7. Synchronization completes in the source/inclusion mutation transaction. A successful response
   must not leave reachable targets pending in a background queue.
8. A propagation failure rolls back the initiating mutation; the system does not accept source and
   target drift as eventual consistency.

### 3.3 Source occurrence identity

`LinkingUnit.Id` is the source record occurrence identity for this feature.

- Replacing selected words or otherwise editing the same unit keeps the same occurrence.
- Deleting that unit ends the occurrence.
- Relinking later creates a new unit ID and therefore a new occurrence.
- A target-local suppression or override is keyed to the current source unit occurrence, not only
  to `AyahId` or visible Quran content.
- A writer must not turn an edit into a new occurrence merely because its internal algorithm
  replaces physical unit rows. It must preserve every logical occurrence ID where possible or
  atomically transfer each sync mapping and its `Active`, `Overridden`, or `Suppressed` state through
  a deterministic bijection. A true split/merge that cannot preserve all occurrence identities is
  rejected before any source or target change commits.
- Only a committed source-record deletion followed by a later explicit link creation counts as the
  delete-and-relink sequence that may reintroduce target content.

This identity rule locks the required behavior:

```text
source unit 100 is synchronized to target
target deletes its clone
source edits unit 100 -> target clone stays absent
source deletes unit 100 -> suppression lifetime ends
source relinks and creates unit 145 -> target receives a fresh clone
```

Merely refreshing, renaming a door, archiving/restoring a door, editing words on unit 100, or
reconfirming the unchanged occurrence does not create a new occurrence and cannot cancel target-
local suppression.

### 3.4 Target-local delete

When the target deletes a synchronized record through the existing link deletion UI:

1. The target clone is deleted from the target's normal link records.
2. The source occurrence and source door remain unchanged.
3. The synchronization ledger remains with state `Suppressed` and no target unit.
4. Later edits to that same source unit do not recreate the target clone.
5. Deleting the source unit retires the suppressed mapping.
6. A later newly created source unit is a new occurrence and synchronizes normally.
7. If several selected target records are synchronized, bulk deletion applies this rule to each
   occurrence independently.

The deletion is not a permanent ayah blacklist. It suppresses the current record occurrence only.

### 3.5 Target-local edit

When the target replaces selected words on a synchronized record through the existing edit UI:

1. Only the target clone changes.
2. The synchronization ledger moves to `Overridden`.
3. Later edits to the same source occurrence do not overwrite the target-local result.
4. Deleting the source occurrence still removes the overridden target clone because its lifetime is
   owned by that source occurrence.
5. Relinking later creates a new source occurrence and therefore a fresh synchronized clone without
   carrying the old target override.

The target edit is local independence for the current occurrence, not a new permanent direct source
record and not a reverse update.

### 3.6 Target direct records

- Records added directly to the target through the normal linking workflow remain target-owned.
- Their edit/delete behavior remains exactly as it is today.
- Detaching an inclusion or deleting a source record never removes a target-owned direct record.
- Copying a synchronized target record through the existing copy workflow produces an ordinary
  direct record in the copy destination. It does not copy the inclusion relationship or sync state.

## 4. Duplicate ayah and selected-word semantics

The existing target door may contain several records that reference the same ayah, just as the
current linking model permits several units/contributions. Inclusion does not redesign or flatten
the existing record list.

The maintained door-level projection continues to enforce:

- one `linking_door_ayahs` row per `(DoorId, AyahId)`;
- one `linking_door_ayah_words` row per `(DoorAyahId, QuranWordId)`; and
- the distinct selected-word union across every surviving direct and synchronized target record.

Therefore:

1. Source A may select word X and source B may select word Y in the same ayah; the target door's
   ayah union contains X and Y.
2. If both sources select X, X exists once in the target door's word union.
3. Removing A removes X only if no target direct record or other synchronized record still supplies
   X.
4. The ayah disappears from the target door union only when no surviving record supplies it.
5. Target-local deletion of one synchronized record removes only that occurrence; another record
   containing the same ayah may keep the ayah visible.

The ordinary record panel retains its existing grouped/independent records and does not add source
labels or a synthetic ayah-only mode.

## 5. Archive, restore, detach, and reattach lifecycle

### 5.1 Source door archive

Archiving a source door does **not** remove or suspend synchronized target records.

- Existing inclusion edges, internal contributions, target clones, and sync mappings remain.
- The target's links, ayahs, selected-word colors, counts, and descriptions remain as they were.
- No archive event is emitted as a source-record deletion.
- An archived source cannot be newly selected for a new inclusion.
- Restoring the source does not create duplicate target records or a new source occurrence.

This intentionally supports temporary working doors that are archived after filling an aggregate
door.

### 5.2 Target door archive

- Archiving a target preserves direct records, synchronized records, inclusion edges, and mappings.
- Existing inclusions continue synchronizing internally while the target is archived so restore
  exposes current source state without a catch-up gap.
- User link and inclusion mutations against the archived target remain blocked.
- Restoring the target does not duplicate records.

### 5.3 Detach inclusion

Removing an inclusion edge:

1. Removes every active or overridden target clone owned by that edge.
2. Removes suppressed ledger rows owned by that edge.
3. Retires the edge's internal linking contribution.
4. Rebuilds affected target ayah/word union rows.
5. Leaves all source records unchanged.
6. Leaves target-owned direct records and clones belonging to other inclusion edges unchanged.
7. Propagates the resulting target record removals to any consumer doors that include this target.

Re-adding the same target/source pair creates a new inclusion row and performs a fresh initial sync
of the source's current records. Suppressions and overrides from the retired edge do not carry to
the new relationship.

### 5.4 Future hard deletion

The current product archives doors; it does not hard-delete them. Inclusion and sync foreign keys
use restrictive behavior where a physical deletion could orphan synchronization state. A future
hard-delete capability must explicitly detach/reconcile dependents first and is outside this
feature.

## 6. Persistence model

The feature adds an inclusion edge, a per-occurrence sync ledger, and an internal linking
contribution kind. It does not add a second public link-record model.

### 6.1 `AbwabDoorInclusion`

Map to `abwab_door_inclusions`:

```text
id                  integer identity primary key
target_door_id      integer not null
source_door_id      integer not null
created_at          timestamptz not null
created_by          integer null
updated_at          timestamptz not null
updated_by          integer null
deleted_at          timestamptz null
deleted_by          integer null
xmin                EF concurrency token
```

Constraints and indexes:

1. Restrictive target and source foreign keys to `abwab_doors.id`.
2. Check constraint `target_door_id <> source_door_id`.
3. Filtered unique index `(target_door_id, source_door_id)` where `deleted_at IS NULL`.
4. Index on `source_door_id` for reverse consumer traversal.
5. Index on `deleted_at` for active-edge filtering.

### 6.2 `AbwabDoorInclusionUnitSync`

Map to `abwab_door_inclusion_unit_syncs`:

```text
id                       bigint identity primary key
door_inclusion_id        integer not null
source_unit_id           bigint not null
target_unit_id           bigint null
state                    active | overridden | suppressed
source_fingerprint       bytea not null
created_at               timestamptz not null
created_by               integer null
updated_at               timestamptz not null
updated_by               integer null
```

Constraints and indexes:

1. Restrictive foreign key to `abwab_door_inclusions.id`.
2. Restrictive foreign key to source `linking_units.id`.
3. Restrictive nullable foreign key to target `linking_units.id`.
4. Unique key `(door_inclusion_id, source_unit_id)`.
5. Filtered unique index on `target_unit_id` where it is not null so one materialized clone has one
   synchronization owner.
6. State/target coherence check:
   - `active` and `overridden` require `target_unit_id IS NOT NULL`;
   - `suppressed` requires `target_unit_id IS NULL`.
7. Index on `source_unit_id` for propagation and source deletion.

The row is physically removed when its source occurrence ends or its inclusion is detached. A
suppressed row intentionally survives while the source unit still exists.

### 6.3 Internal inclusion contribution

Extend `LinkingSourceContribution` with nullable `DoorInclusionId`, add `DoorInclusion` to the Domain
and persisted source-kind enum, and make `OperationId` nullable only for that internal kind.

Rules:

1. Exactly one live internal contribution exists per live inclusion edge in its target door.
2. Its contribution-to-unit mappings own that edge's materialized target clones.
3. `DoorInclusionId` is required only for the internal kind and forbidden for every public Quran
   source kind.
4. `OperationId` is null only for the internal kind; every public Quran source kind retains its
   required `LinkingOperation` owner. No synthetic linking operation is created for an inclusion.
5. Root, lemma, stem, unique-word, and word-type reference columns remain null for the internal
   kind.
6. The internal kind is never accepted in public linking source descriptors, workspace requests,
   or prepared-preflight input.
7. Its source identity and target unit identities are internally salted by inclusion and source
   unit IDs so clones cannot silently merge with target-owned units or another edge's clone.
8. Internal labels/scope exist only to satisfy durable storage contracts and are never exposed as
   ayah/word source attribution.

Before any inclusion can materialize, the confirmed-state path must retain internal contribution
units for door-level ayah/word impact while excluding the internal kind, identity, and label from
public tokens, descriptors, overlap labels, preflight/workspace responses, and authored requests.

### 6.4 Source fingerprint

The synchronizer hashes the source unit's canonical persisted shape:

- grouped flag;
- Quran-ordered ayah IDs;
- selected Quran word IDs per ayah; and
- ordered descriptions per ayah.

The fingerprint detects edits to the same occurrence. It is not an HTTP validator, user-visible
version, or replacement for canonical Quran identities.

### 6.5 Schema and operational surfaces

Update:

- `QuranDashboardDbContext` DbSets;
- EF configurations under the Abwab/Inclusion and Linking owning folders;
- linking contribution kind/reference check constraints;
- the generated model snapshot;
- `Backend/scripts/wipe-abwab` after auditing the complete `TRUNCATE ... CASCADE` closure created by
  the new cross-feature foreign keys; and
- `AbwabSchemaFixture` reset ownership.

No Quran table changes and no Quran data backfill are permitted.

## 7. Synchronization transaction and concurrency model

### 7.1 Lock order

All operations that mutate inclusion topology or a door's link records and can trigger propagation
take one transaction-scoped PostgreSQL advisory lock in a dedicated synchronization namespace.

The global order is:

1. existing job/idempotency lock when the current linking path already requires it;
2. inclusion synchronization advisory lock;
3. affected door rows in ascending ID order;
4. source and target units/mappings in ascending ID order.

No inclusion writer may take a door/unit lock before the synchronization lock. This prevents an
inclusion mutation and a simultaneous source link mutation from each validating a different graph
or deadlocking in reverse order.

### 7.2 Topology add

Inside one transaction:

1. Normalize submitted source IDs and reject an empty or repeated set.
2. Acquire the synchronization lock.
3. Lock target and source doors in deterministic order.
4. Validate `expectedTargetDoorVersion`, existence, self, target lifecycle, and current source
   lifecycle.
5. Reject any active duplicate direct edge.
6. Evaluate the active graph plus the complete proposed batch and reject any cycle atomically.
7. Insert every inclusion and its internal target contribution.
8. Enumerate every live source unit, create a target clone, map it to the internal contribution, and
   create an `Active` sync row with its fingerprint.
9. Rebuild affected target ayah/word union rows.
10. Recursively synchronize the new target records to its consumer doors in graph order.
11. Bump every changed target door version.
12. Commit, then invalidate the Abwab tree cache once.

### 7.3 Source add

For each newly created live source unit and each active consumer inclusion:

1. Confirm this is an actual add after no same occurrence remains, not an internal replacement that
   belongs to the source-edit path.
2. Confirm no sync row exists for `(InclusionId, SourceUnitId)`.
3. Clone the complete unit into the target with an internal salted identity.
4. Add its contribution mapping and `Active` sync row.
5. Rebuild affected target union rows.
6. Continue propagation to downstream consumers.

A new source unit created by a later explicit link operation after the old occurrence's committed
deletion is the event that can reintroduce previously suppressed content. An edit-time physical row
replacement is not.

### 7.4 Source edit

For each sync row of the edited source unit:

- `Active`: replace the clone's grouped shape, ayahs, selected words, and descriptions from the
  source, update the fingerprint, rebuild target union rows, and propagate the target edit.
- `Overridden`: update only the observed source fingerprint/audit data; do not overwrite or
  propagate a target content change.
- `Suppressed`: update only the observed source fingerprint/audit data; do not recreate a target
  unit and do not propagate an add.

An edit to the same source unit cannot cancel suppression or override.

### 7.5 Source delete

Before physically deleting a source unit:

1. Load every sync row that names it.
2. For `Active` or `Overridden`, recursively remove downstream clones, detach the target clone from
   its internal contribution, and delete the clone after all dependent mappings are gone.
3. For `Suppressed`, no target clone exists.
4. Remove every sync row for the ending occurrence.
5. Rebuild affected target union rows and bump changed target versions.
6. Continue the original source unit deletion.

This ordering lets restrictive foreign keys prevent an orphaned propagation state.

### 7.6 Target edit and delete dispatch

The existing target link endpoints retain their routes and bodies. The Backend detects whether the
requested target unit has an inclusion sync owner:

- no sync owner: run the existing direct-record mutation unchanged;
- sync owner + replace words: update only the target clone and set `Overridden`;
- sync owner + delete: remove the target clone and set `Suppressed` while keeping the source unit;
- mixed bulk delete: apply direct deletion and synchronized suppression in the same transaction.

Every target-visible change propagates to consumer doors because this target can itself be a source.

### 7.7 Inclusion detach

Detach takes the synchronization lock, validates the target version, removes every downstream clone
owned by the edge, removes its sync rows, retires its internal contribution and edge, rebuilds
target unions, propagates resulting target removals, bumps changed target versions, commits, and
invalidates the tree once.

## 8. Backend integration points

Synchronization must cover every supported path that can create, edit, or remove a live unit:

1. linking confirmation execution, including prepared/background confirmation;
2. direct selected-word replacement;
3. direct record deletion and bulk deletion;
4. inclusion creation initial sync;
5. inclusion detach cleanup;
6. target-local override/suppression of synchronized records; and
7. any retained maintenance path that legitimately mutates live link units.

Do not scatter independent propagation SQL across these writers. Introduce one focused
`IAbwabDoorInclusionSynchronizer` abstraction at the Application/Infrastructure boundary and one
transaction-bound Infrastructure implementation used by the owning writers.

The synchronizer must:

- operate inside the caller's existing transaction;
- receive precise added/edited/deleted unit IDs;
- process reachable consumer doors without an N+1 query per unit or door;
- rebuild only affected ayah IDs;
- keep internal inclusion contributions out of public source descriptors; and
- report whether each target changed so callers bump versions and invalidate once.

Expected conflicts and safe-completion failures return controlled Application outcomes. The global
exception middleware handles only unexpected faults, sanitizes the response, and emits at most one
safe diagnostic at the owning boundary.

## 9. HTTP contracts

All responses use the existing `ApiResponse<T>` envelope and localized Arabic messages.

### 9.1 Read inclusion topology

```text
GET /api/abwab/doors/{doorId}/inclusions
```

Response data:

```text
doorId
doorVersion
sources[]:
  inclusionId
  doorId
  doorName
  isArchived
consumers[]:
  inclusionId
  doorId
  doorName
  isArchived
```

This topology endpoint may name doors because its purpose is managing the inclusion relationship.
It does not expose the source of any Quran link, ayah, selected word, or description.

The read is public. Missing doors return 404. An archived requested door may return topology for
audit/restore context; writes remain blocked.

### 9.2 Add source doors

```text
POST /api/abwab/doors/{targetDoorId}/inclusions
```

Body:

```text
expectedTargetDoorVersion
sourceDoorIds[]
```

Success returns 201 with added inclusion DTOs and the new target version after initial sync.

Controlled outcomes:

- 400: invalid IDs, empty list, repeated submitted ID, or self-inclusion;
- 404: target or source not found;
- 409: archived target, archived source at creation, duplicate edge, cycle, stale target version, or
  source/target link state changed while acquiring the synchronization transaction;
- 503: controlled synchronization infrastructure failure when the transaction could not safely
  complete.

The action carries `[RequirePermission(AbwabPermissions.Inclusions.Create)]`.

### 9.3 Remove inclusion

```text
DELETE /api/abwab/doors/{targetDoorId}/inclusions/{inclusionId}
```

Body:

```text
expectedTargetDoorVersion
```

Success returns 200 with removed inclusion ID, removed synchronized record count, and new target
version. It does not return 204 because the caller needs the new concurrency version and removal
summary.

Controlled outcomes:

- 400: invalid route/body shape;
- 404: active inclusion not found under that target;
- 409: archived or stale target;
- 503: synchronization could not complete atomically.

The action carries `[RequirePermission(AbwabPermissions.Inclusions.Delete)]`.

### 9.4 Existing link contracts

Do not add an effective-links endpoint and do not add source fields to existing link DTOs.

The current routes remain the only link-record UI contract:

```text
GET    /api/abwab/doors/{doorId}/links/snapshot
GET    /api/abwab/doors/{doorId}/links
GET    /api/abwab/doors/{doorId}/links/{unitId}/ayahs
PATCH  /api/abwab/doors/{doorId}/links/{unitId}/words
POST   /api/abwab/doors/{doorId}/links/bulk-delete
```

Synchronized units are returned as ordinary records with the same DTO shape. No response identifies
a record, ayah, word, or description as synchronized or names its source door. Existing edit/delete
responses remain unchanged; the Backend dispatch in section 7.6 owns local semantics.

## 10. Tree metrics and cache behavior

Because synchronized clones are normal live target `LinkingUnit` records mapped by a live internal
contribution:

- existing `LinkCount` includes them automatically;
- existing `SelectedWordCount` includes their stored selected-word rows under its current record-
  based semantics;
- clicking the existing link count opens the same current panel; and
- no effective/direct metric split or second link-panel mode is added.

Extend each tree door DTO only with topology counts needed to discover/manage the feature:

```text
inclusionSourceCount
inclusionConsumerCount
```

Do not change the meaning of `LinkCount`, `SelectedWordCount`, `RelationCount`, child counts, or
hierarchy metrics.

Invalidate the tree once after any committed operation that changes:

- inclusion topology;
- source records and therefore synchronized targets;
- target-local synchronized override/suppression;
- archived/restored/renamed topology labels through existing door invalidation; or
- direct link records through existing invalidation.

Every changed target door version advances before commit so an already-open link panel receives the
existing stale-version protection rather than silently editing an obsolete record set.

## 11. Permissions and authorization

Add an independent permission group `تضمين الأبواب`:

```text
abwab.inclusions.create
abwab.inclusions.delete
```

Do not reuse semantic relation permissions. Owner bypass and existing permission-catalogue startup
synchronization remain authoritative.

Inclusion topology GET is public. Inclusion POST/DELETE each carry exactly one permission
classification. Existing link mutation endpoints remain Owner-only; their authorization does not
change merely because the Backend may apply local synchronized semantics.

Regenerate frontend permission constants with `npm run generate:permission-codes`; never hand-edit
the generated permission file.

## 12. Backend placement and implementation phases

### Phase 1 — Domain, EF model, and internal ownership

Files to add near the owning Abwab inclusion feature:

- `AbwabDoorInclusion`
- `AbwabDoorInclusionUnitSync`
- `AbwabDoorInclusionSyncState`
- their EF configurations

Files to change:

- `LinkingSourceKind`, `LinkingSourceContribution`, and their configuration/check constraints;
- internal linking source-kind persistence conversion;
- `QuranDashboardDbContext`;
- Abwab schema fixture/reset ownership; and
- `Backend/scripts/wipe-abwab` after a deliberate cascade-closure audit.

Work:

1. Add the exact edge, ledger, state, internal contribution reference, conditional operation
   ownership, constraints, and indexes from section 6.
2. Require null `OperationId` for the internal kind, preserve non-null operation ownership for every
   public kind, and create no synthetic `LinkingOperation`.
3. Keep the internal source kind impossible to submit or emit through public descriptors.
4. Generate `AddAbwabDoorInclusionSynchronization` only after explicit migration authorization.
5. Never hand-write or manually edit generated migration/model snapshot files.

Phase gate:

- The model distinguishes active, overridden, and suppressed occurrences.
- A suppressed mapping can survive source edits but blocks source unit deletion until the deletion
  path reconciles it.
- No public source descriptor accepts the internal kind, and no public projection emits it.
- Existing public contributions keep their required operation owner; internal contributions have
  none.
- Pending-model check is clean after the authorized migration.

### Phase 2 — Shared synchronization and public-isolation foundation

Files to add:

- `IAbwabDoorInclusionSynchronizer` and focused mutation/result contracts;
- the shared lock, snapshot/fingerprint projection, affected-ayah rebuilder, and no-fixed-depth
  consumer traversal;
- target clone creation/removal and internal contribution mapping support.

Files to change:

- Abwab/Linking dependency injection;
- confirmed-state reader, operation classifier, public source tokens, and descriptor body mapper.

Work:

1. Implement the lock order and reusable recursive consumer traversal from section 7.
2. Keep internal units available for relational door-state impact while filtering their ownership
   from every public/authored source surface.
3. Clone complete normal record shape with internal salted identities.
4. Maintain the per-occurrence sync ledger and rebuild affected relational unions.

Phase gate:

- Initial and ongoing propagation share one graph engine.
- Existing linking preflight cannot tokenize or expose the internal kind.
- No internal contribution creates a linking operation.

### Phase 3 — Inclusion graph management and initial sync

Files to add:

- inclusion reader/writer abstractions and DTOs under Application Abstractions;
- `GetDoorInclusions`, `AddDoorInclusions`, and `DeleteDoorInclusion` use cases;
- EF topology reader, transactional writer, invalidating decorator, controller, and request bodies.

Work:

1. Implement direct topology reads in both source and consumer directions.
2. Implement atomic batch add with graph cycle validation.
3. Create/retire internal contributions with edges.
4. Run initial and downstream synchronization through the Phase 2 traversal.

Phase gate:

- Same-pair, self, archived-at-creation, missing, stale, and cycle outcomes are controlled.
- No batch partially commits; initial sync reaches all consumers before success.
- Archived existing sources remain visible and no source attribution appears in link responses.

### Phase 4 — Existing linking writer integration

Files to change in their focused partials or helpers:

- linking confirmation writer execution paths;
- door-link selected-word replacement;
- door-link record/bulk deletion;
- relational door ayah/word rebuild support;
- linking and Abwab cache invalidating decorators;
- source mutation dispatch across `Active`, `Overridden`, and `Suppressed` mappings.

Work:

1. Report precise added, edited, deleted, and identity-preserving replacement unit IDs to the
   synchronizer.
2. Acquire the synchronization lock before door/unit locks.
3. Dispatch target imported edits to `Overridden` and deletes to `Suppressed`.
4. Preserve the existing behavior for units with no sync owner.
5. Update only fingerprint/audit for source edits mapped as `Overridden` or `Suppressed`.
6. Propagate every target-visible edit or suppression onward when the target is a source for another
   door.
7. Keep expected synchronization failures in controlled Application outcomes and reserve global
   exception middleware for unexpected faults.

Phase gate:

- Source add/edit/delete produces target add/edit/delete atomically.
- Target edit/delete never changes the source.
- Editing a suppressed source occurrence cannot recreate it.
- Delete then relink creates a new source unit and synchronizes it again.

### Phase 5 — Tree and cache completion

Files to change:

- `AbwabTreeDto.cs`;
- `EfAbwabTreeReader.cs`;
- cached tree mapping/generation only where needed.

Work:

1. Add source/consumer topology counts.
2. Let existing link metrics naturally count live synchronized clones.
3. Invalidate once after the complete recursive transaction commits.

Phase gate:

- Target link counts update without API restart.
- Source archive leaves target link counts unchanged.
- Detach removes only the edge-owned target records from counts.

### Phase 6 — API and permission generation

1. Build Backend contracts.
2. Run `Backend/scripts/check-api-contract`; commit only sanctioned generated models.
3. Run `npm run generate:permission-codes` after catalogue changes.
4. Update existing exact permission-catalogue and unsafe-route inventories.
5. Do not hand-edit generated API or permission files.

### Phase 7 — Frontend inclusion management only

Files to add under `features/abwab/`:

- `components/abwab-inclusions-modal/*`
- `data-access/abwab-inclusions.api.ts`
- `state/abwab-inclusions.controller.ts`
- a feature view model only if generated DTOs are insufficient

Files to change only for composition:

- Abwab page and overlay composition;
- modal URL restoration;
- tree flag/action and side-panel/context-menu entry point;
- permission controller;
- tree model mapping for topology counts; and
- feature labels.

Management contract:

1. `تضمين الأبواب` is separate from semantic relations.
2. One selected live target may add several live source doors atomically.
3. The modal shows direct `مصادر الباب` and direct `يُستخدم في أبواب جامعة` topology.
4. Existing archived sources remain visible with an archived label and explanation that their
   synchronized target links stay present.
5. Detach confirmation explains that the source remains unchanged while its synchronized target
   records are removed.
6. The source picker excludes the target, active direct sources, and archived doors; Backend cycle
   validation remains authoritative.
7. Skeleton, refreshing, empty, error, and notice states retain separate owners.

### Phase 8 — Existing link UI compatibility

No new link component, tab, effective view, source badge, origin list, or source label is added.

Verify and adjust only the state orchestration needed for existing stale-version refresh:

1. Adding links uses the exact current linking UI and request flow.
2. The target's synchronized records load through the current door-links snapshot/list.
3. Ayahs, highlighted selected words, grouping, and descriptions render through current components.
4. Edit, delete, select-all, and copy controls keep their current visual behavior.
5. The Backend, not the component, distinguishes direct, overridden, and suppressed semantics.
6. After source propagation, a currently open target panel refreshes through existing stale/refresh
   behavior without revealing source metadata.

## 13. Frontend visual and accessibility rules

1. Arabic and RTL are the baseline; use logical properties only.
2. Reuse the current wide modal, door picker, tabs, confirm dialog, actions, and state owners.
3. Keep surfaces flat with existing tokens and hairline borders. Add no gradient, resting shadow,
   hover lift, raw color, new font, decorative image, or Quran renderer change.
4. Green marks current selection or the one primary action only; archive status has text/icon
   meaning and never relies on color alone.
5. Compact, Medium, and Wide use the shared breakpoint contract with no raw thresholds.
6. Keep API calls in data access, orchestration in focused state, and the page as a composition shell.
7. Preserve modal focus restoration, keyboard source selection, confirmation focus, live
   announcements, and calm controlled errors.
8. Quran text is never animated, regenerated, corrected, or restyled by inclusion.

## 14. Migration and operational decision

The feature requires a generated EF migration because it adds two tables, adds an internal linking
source reference, makes contribution `operation_id` conditionally nullable, and changes contribution
check constraints.

Planning and later specification do not authorize migration generation or application.

When explicitly authorized during implementation:

1. Generate `AddAbwabDoorInclusionSynchronization` through `Backend/scripts/add-mig` only.
2. Never apply it to a database without a separate explicit database-update instruction.
3. Report generated files, build result, pending-model result, applicable gates, and whether database
   update was skipped.
4. Audit `wipe-abwab`'s complete cascade closure before changing its literal table allowlist; do not
   assume the new Abwab-to-Linking foreign keys are contained by a simple table-count increment.

Existing rows retain non-null `OperationId` and receive null `DoorInclusionId`; the internal kind
requires null `OperationId`, no synthetic operation row is inserted, and there is no inclusion or
synchronization data to backfill.

## 15. Testing decision

The Test Freeze default remains active.

- Add no new Backend test class or test method.
- Add no Angular unit `*.spec.ts`.
- Add no Playwright journey.
- Minimally update retained exact-contract protection whose owned subject changes: Abwab schema
  reset/catalogue, linking contribution constraints when already protected, permission catalogue,
  and unsafe-route inventory.
- Verify synchronization, suppression, override, archive retention, detach, transitive propagation,
  and concurrency through the manual/runtime matrix unless the owner separately authorizes one
  focused permanent-test exception before implementation.

## 16. Required verification

### Backend and schema

1. Run the Backend build.
2. Run `Backend/scripts/check-pending-model` after the authorized generated migration.
3. Run the migration gate.
4. Run the gate-contract lane for schema/check-constraint changes.
5. Run Tier B for retained Abwab and permission-catalogue protection.
6. Run smoke for public reads and permission-classified writes.
7. Run `Backend/scripts/check-api-contract`.

### Frontend

Run independently and in order:

1. `npm run generate:permission-codes`
2. `npm run check:no-unit-specs`
3. `npm run typecheck:app`
4. `npm run build:verify`
5. `npm run check:golden-ui`

### Manual/runtime matrix

1. Include one live source with independent and grouped records; the target's current link panel
   shows normal cloned records with identical ayahs, selected words, descriptions, and grouping.
2. Include two sources from unrelated sections; neither moves in the tree.
3. The same ayah with different source-selected words produces the correct target door word union.
4. The same selected word from two records remains in the target union after one record is removed.
5. Source add creates a target clone before the source command reports success.
6. Source selected-word edit updates an `Active` target clone.
7. Source record deletion removes an `Active` target clone without removing another supporting
   record.
8. Target edit changes only the target clone, marks it overridden, and leaves the source unchanged.
9. A later edit of that same source unit does not overwrite the target override.
10. Source deletion removes the overridden target clone.
11. Target deletion removes only the target clone, keeps the source record, and leaves a suppressed
   sync row.
12. Editing the still-existing suppressed source unit does not recreate the target clone.
13. Deleting that source unit retires suppression; relinking creates a new unit ID and a fresh target
   clone.
14. An edit implementation that internally replaces a unit row transfers suppression/override and
   does not reintroduce content.
15. Merely archiving/restoring the source does not recreate or duplicate a suppressed record.
16. Source archive keeps every already-synchronized target record and count.
17. Detaching an inclusion removes active, overridden, and suppressed state owned by that edge while
   preserving source and unrelated target records.
18. Reattaching performs a fresh sync and does not reuse retired suppression/override state.
19. A includes B and B includes C; source changes propagate through both targets.
20. A proposed cycle is rejected with no partial edge or synchronized record.
21. Concurrent opposite-edge or link/topology writes cannot commit a cycle or drift.
22. A propagation failure rolls back the initiating source mutation.
23. Target direct records retain existing edit/delete behavior and survive inclusion detach.
24. Copying a synchronized record creates a normal direct copy without inclusion metadata.
25. Existing link responses and UI show no source door, sync state, internal contribution label, or
   origin metadata.
26. A permission-lacking active user receives 403 for inclusion writes; Owner succeeds.
27. An archived target rejects user mutations while internal synchronization remains current for
   restore.
28. Source and recursively affected target door versions advance; stale open panels recover through
   existing behavior.
29. Tree `LinkCount` and `SelectedWordCount` reflect stored synchronized records without a second
   effective metric mode.

## 17. Explicit non-goals

- Changing sections, parents, hierarchy, or tree order.
- Adding inclusion to semantic relation types.
- Bidirectional synchronization or any target-to-source mutation.
- A read-time effective union or a separate effective-links endpoint.
- An effective/direct links tab or new Quran-content presentation.
- Exposing per-link, per-ayah, per-word, or per-description source attribution.
- Flattening the current grouped/independent record list into one synthetic ayah list.
- Treating target deletion as a permanent ayah blacklist across future source occurrences.
- Allowing an edit of the same suppressed source occurrence to recreate target content.
- Removing synchronized target content merely because its source door is archived.
- Allowing new inclusions to archived sources.
- Authored source ordering or bulk inclusion into several targets.
- Hard-deleting archived doors or Quran data.
- A background/eventual propagation queue.
- A new effective-content cache, HTTP ETag, deployment topology, or Quran renderer.
- New automated tests without separate owner authorization.

## 18. Completion boundary

The feature is complete only when:

1. Inclusion is an independent directed concept and the active graph is acyclic under concurrency.
2. Every supported source record add/edit/delete path synchronizes reachable targets atomically.
3. Target records appear through the exact current link contracts and UI without source attribution.
4. Target edit/delete never mutates a source.
5. Suppression survives edits to the same source occurrence and is released only when that source
   occurrence ends; a later new occurrence synchronizes again.
6. Override protects a target-local edit from same-occurrence source edits but ends with the source
   occurrence or inclusion.
7. Source archive preserves synchronized target records, while detach removes only edge-owned target
   state.
8. Direct target records and existing direct link behavior remain intact.
9. Internal contribution kinds cannot enter public linking requests or leak through API responses.
10. Generated migration, API models, and permission constants use sanctioned tooling under the
    required authorization.
11. Required builds, retained gates, frontend checks, and the manual/runtime matrix pass.
12. No Quran data, semantic relation, hierarchy placement, or deployment boundary changed.

## 19. Specify handoff

After owner approval, invoke `speckit-specify` with this document as the feature-description
authority. The specification must preserve the one-way materialized synchronization model,
source-occurrence identity, suppression/override lifecycle, archive/detach behavior, unchanged link
UI contract, internal-only source attribution, migration authorization boundary, Testing Decision,
and non-goals above.

`speckit-plan` and `speckit-tasks` follow the specification in normal order. Neither may replace this
with a read-time union, eventual background propagation, reverse synchronization, or a new link UI.
