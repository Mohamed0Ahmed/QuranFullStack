# Abwab link records — direct implementation plan

- Status: ready for phased implementation
- Delivery: Backend contracts and persistence, generated API models, Angular UI, then verification
- Access: public record reads; active Owner only for selection and mutations
- Database: no schema change and no database reset

## 1. Required outcome

The Abwab tree must show the number of confirmed link records for every door. Selecting that number
opens a wide inline panel under the same tree row. Every user can inspect complete ayahs. The Owner
can select records, replace selected words, delete records from the door, and copy all or selected
records to another door.

Copying must use the existing direct Quran-link request and its current prepare, review, and confirm
lifecycle. It must not add a copy route or change that route's request contract.

## 2. Locked domain rules

1. The displayed record is one live `LinkingUnit` referenced by at least one live
   `LinkingSourceContribution` in the same door.
2. One grouped unit counts as one record regardless of how many ayahs it contains.
3. Every independent unit counts as one record.
4. The count is `COUNT(DISTINCT linking_units.id)` after the live-contribution filter. It is not an
   ayah count and does not count contribution-to-unit mappings twice.
5. A record can be shared by several internal source contributions, but source metadata is not
   exposed, displayed, or counted as part of the link.
6. Deleting a record means removing that unit from the door, including every contribution mapping
   to it in that door. It does not delete or archive the door.
7. Editing replaces the complete selected-word set for the record's ayahs. It does not change the
   grouped/independent shape, ayah membership, descriptions, or Quran data.
8. Copying preserves ayah membership, group boundaries, selected canonical Quran word IDs, and
   descriptions.
9. Target classification remains owned by the existing linking preflight: new, already present,
   updated, invalid, or removed behavior must not be reimplemented in the Abwab feature.
10. Archived doors can retain a count in the tree response, but record management and copy targets
    are limited to live doors.

## 3. HTTP contracts

### 3.1 Existing tree read

Extend each `AbwabTreeDoorDto` with:

```text
linkCount: integer >= 0
```

The existing tree route, response envelope, ETag, and conditional-read behavior remain unchanged.

### 3.2 Door record page

```text
GET /api/abwab/doors/{doorId}/links?page={n}&pageSize={n}&expectedDoorVersion={optional}
```

Response data:

```text
doorId
doorVersion
page
pageSize
totalCount
items[]:
  unitId
  isGrouped
  ayahCount
  selectedWordCount
  descriptionCount
  firstVerseKey
  lastVerseKey
```

Ordering is Quranic by surah number and ayah number, with `LinkingUnit.Id` used only as a stable
tiebreaker. Page size is positive and no greater than the existing linking page-size maximum of
100. Page 1 captures `doorVersion`; later pages send it back and receive 409 if the door's link
state changed.

### 3.3 Record ayah page

```text
GET /api/abwab/doors/{doorId}/links/{unitId}/ayahs
    ?page={n}&pageSize={n}&expectedDoorVersion={version}&expectedLinkingDataRevision={optional}
```

Response data:

```text
doorId
doorVersion
unitId
isGrouped
linkingDataRevision
page
pageSize
totalCount
items[]:
  hydrated Quran ayah fields
  selectedWordIds[]
  descriptions[]
```

The first page captures `linkingDataRevision`; later pages send it back. Hydration reuses the
existing linking Quran reader so text, order, markers, and canonical word IDs are never rebuilt in
the Abwab layer.

### 3.4 Replace selected words

```text
PATCH /api/abwab/doors/{doorId}/links/{unitId}/words
```

Body:

```text
expectedDoorVersion
selectedWords[]: { ayahId, quranWordId }
```

The list is a full replacement, not an add/remove delta. The Backend validates that every ayah
belongs to the unit and every word belongs to that ayah.

### 3.5 Complete door-link snapshot

```text
GET /api/abwab/doors/{doorId}/links/snapshot
```

The response returns every live record for the door, one deduplicated hydrated Quran ayah catalog,
and each record's Quran-ordered ayah references with selected word IDs and descriptions. It does
not return source metadata. It captures one `doorVersion` and one `linkingDataRevision` for the
complete response. The endpoint uses set-based reads inside the existing revision scope and does
not issue one query per record.

### 3.6 Delete selected records

```text
POST /api/abwab/doors/{doorId}/links/bulk-delete
```

Body:

```text
expectedDoorVersion
selectionMode: only | all_except
unitIds[]
```

`only` deletes the submitted IDs. `all_except` deletes every live record except the submitted IDs,
which allows select-all across pages without first sending thousands of IDs. One-record deletion
uses the same command with `only` and one ID.

Mutation responses return the affected count and the new door version. Invalid shapes return 400,
missing doors/records return 404, stale versions or archived doors return 409, and successful reads
or writes use the existing `ApiResponse<T>` envelope and localized messages. All three read actions
are public; the replace and delete actions are Owner-only.

## 4. Phase 1 — Backend tree count and cache correctness

### Files to change

- `Backend/application/QuranDashboard.Application.Abstractions/Abwab/Responses/AbwabTreeDto.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/EfAbwabTreeReader.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/LinkingDependencyInjection.cs`

### Files to add

- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/InvalidatingLinkingConfirmationWriter.cs`

### Work

1. Add `LinkCount` to the door DTO.
2. Load all door counts with one set-based query joining units, mappings, and live contributions;
   never issue one query per door.
3. Map zero for doors with no live units.
4. Decorate `ILinkingConfirmationWriter`. After a non-replay, non-no-op successful commit, call
   `IAbwabCacheInvalidator.InvalidateTree()` so the cached payload and ETag advance.
5. Do not invalidate the tree for failed, replayed, or no-op confirmations.

### Phase gate

- A grouped unit with five ayahs adds one to `LinkCount`.
- Five independent units add five.
- A unit mapped by two live contributions is counted once.
- A completed link confirmation is visible on the next tree refresh without restarting the API.

## 5. Phase 2 — Backend record reads

### Files to add

- `Backend/application/QuranDashboard.Application.Abstractions/Linking/DoorLinks/IDoorLinkRecordsReader.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Linking/DoorLinks/DoorLinkRecordDtos.cs`
- `Backend/application/QuranDashboard.Application/Linking/DoorLinks/Queries/GetDoorLinkRecords/*`
- `Backend/application/QuranDashboard.Application/Linking/DoorLinks/Queries/GetDoorLinkAyahs/*`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/EfDoorLinkRecordsReader.cs`
- `Backend/api/QuranDashboard.Api/Contracts/Linking/AbwabDoorLinkBodies.cs`
- `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorLinksController.cs`

### Files to change

- `Backend/application/QuranDashboard.Application/DependencyInjection.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/LinkingDependencyInjection.cs`
- `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`

### Work

1. Keep list and ayah-detail queries behind one focused Application abstraction.
2. Verify the door exists, is live, and matches `expectedDoorVersion` before returning later pages.
3. Read only distinct units that still have a live contribution mapping in the requested door.
4. Aggregate counts in SQL without exposing source metadata or materializing the complete link graph.
5. Page unit ayahs before Quran hydration and hydrate only that page.
6. Load selected word IDs and descriptions only for the returned unit-ayah IDs.
7. Execute Quran hydration inside the current linking-data revision read scope and return stable
   lifecycle conflict codes for stale door or Quran state.
8. Keep the controller limited to binding, authorization, outcome-to-HTTP mapping, and envelopes.

### Phase gate

- Opening a door with thousands of records returns only the requested record page.
- Expanding a grouped record with thousands of ayahs returns only the requested ayah page.
- Numbering can be calculated as `(page - 1) * pageSize + itemIndex + 1`.
- Missing, archived, stale, and unauthorized cases return controlled responses.

## 6. Phase 3 — Backend edit and deletion

### Files to add

- `Backend/application/QuranDashboard.Application.Abstractions/Linking/DoorLinks/IDoorLinkRecordsWriter.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Linking/DoorLinks/DoorLinkSelection.cs`
- `Backend/application/QuranDashboard.Application/Linking/DoorLinks/Commands/ReplaceDoorLinkWords/*`
- `Backend/application/QuranDashboard.Application/Linking/DoorLinks/Commands/DeleteDoorLinks/*`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfDoorLinkRecordsWriter.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfDoorLinkRecordsWriter.Deletion.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfDoorLinkRecordsWriter.DoorState.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/InvalidatingDoorLinkRecordsWriter.cs`

### Files to change

- `Backend/application/QuranDashboard.Application/DependencyInjection.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/LinkingDependencyInjection.cs`
- `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorLinksController.cs`
- `Backend/api/QuranDashboard.Api/Contracts/Linking/AbwabDoorLinkBodies.cs`
- `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`

### Edit transaction

1. Lock and validate the live door using `expectedDoorVersion`.
2. Lock the requested unit and its ayahs; reject cross-door or orphan IDs.
3. Validate the submitted canonical word IDs against the unit's ayahs.
4. Replace unit word rows and recompute `LinkingUnitIdentity` plus its hash.
5. If the new identity already exists in the door, merge contribution mappings into that unit,
   remove duplicate mappings, normalize contribution order values, and delete the orphaned unit.
6. Update affected contribution timestamps, actor fields, and versions.
7. Rebuild the affected `linking_door_ayah_words` union from the surviving live units.
8. Update the door row so its concurrency version changes, commit, then invalidate the tree.

### Delete transaction

1. Resolve `only` or `all_except` against live units in the door under the same transaction.
2. Detach every selected unit from all contribution mappings in that door.
3. Normalize the remaining unit order inside each affected contribution.
4. Soft-delete contributions left with no units and update surviving contribution audit/version
   fields.
5. Delete selected units only after their mappings are gone, including words, descriptions, and
   unit-ayah children.
6. Rebuild affected `linking_door_ayahs` and `linking_door_ayah_words` from surviving live units.
7. Update the door row, commit atomically, then invalidate the tree.

### Phase gate

- Removing a selected word removes only that word from the record.
- An identity collision merges records without duplicate mappings or lost source references.
- Deleting one grouped record deletes the group as one record.
- A failed or stale mutation leaves units, contributions, door state, and tree cache unchanged.

## 7. Phase 4 — Generated contracts and Frontend state

### Contract generation

1. Run `Backend/scripts/check-api-contract` after the Backend contracts compile.
2. Regenerate committed Angular models through the existing generator; never hand-edit generated
   model files.

### Files to add

- `Frontend/quran-dashboard-ui/src/app/features/abwab/models/abwab-door-links.models.ts`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/data-access/abwab-door-links.api.ts`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-door-links.store.ts`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-door-links.facade.ts`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-door-link-snapshot.mapper.ts`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-door-link-copy.mapper.ts`

### Files to change

- `Frontend/quran-dashboard-ui/src/app/features/abwab/models/abwab.models.ts`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/models/abwab.labels.ts`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-tree.builder.ts`

### State contract

The dedicated store owns:

- open door ID and captured door version;
- complete normalized record/ayah snapshot, total count, linking-data revision, loading, refresh,
  empty, and error states;
- record selection as `only` or `all-except` plus exception IDs;
- edit draft and write state;
- delete confirmation and write state; and
- copy scope, target door, batch queue, current batch number, and errors.

Changing the open door, receiving a stale response, completing a mutation, or closing the panel
clears incompatible snapshots and selections. Presentational components never call HTTP directly.

## 8. Phase 5 — Inline tree panel and link operations

### Files to add

- `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-links-panel/*`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-links-list/*`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-link-record/*`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-link-operations/*`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-link-editor/*`

### Files to change

- `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-tree/abwab-tree.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-tree/abwab-tree.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-tree/abwab-tree.component.scss`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.scss`

### Work

1. Add the `Links` header and count immediately to the right of `Direct` in RTL order. Zero remains
   visible with muted treatment.
2. A single click opens or closes the active door panel and stops row-selection propagation. Only
   one panel can be open.
3. Render the panel as one anchored floating layer so opening it never pushes later tree rows.
4. Show a compact header with door name, total record count, selection count, select-all, and clear
   actions.
5. Render numbered record rows with checkbox, `grouped`/`independent` Arabic label, ayah count,
   selected-word count, descriptions count, and source labels.
6. Show every record's ayahs immediately. Reuse the established linking ayah-card renderer so
   complete Quran text, numbering, and selected-word presentation match the linking workspace.
7. Fetch the complete door snapshot once and virtualize the record rows so all records are available
   without load-more controls while the DOM remains bounded.
8. Place `Link operations` inside the same floating layer. Enable edit for exactly one selected
   record, delete for one or more, and copy for one or more or all.
9. Use controlled skeleton, refreshing, empty, error, and notice owners. All layout uses logical
   properties, existing tokens, RTL order, and flat bordered surfaces.

### File-size boundary

The current Abwab page, tree component, and interaction controllers are already near their review
thresholds. Link loading, selection, editing, deletion, and copy orchestration stay in the new
focused files above; they must not be appended to the existing page interaction or overlay
controllers.

### Phase gate

- The count opens and closes on the first click without changing the selected door accidentally.
- The panel is anchored to the correct row without changing tree layout at every supported band.
- Full ayah text and selected words match their linking-workspace presentation.
- Record and ayah numbering remain globally correct across the complete snapshot.

## 9. Phase 6 — Edit and delete UI

1. Edit opens the selected record inside the floating panel from the already loaded snapshot and
   enables word selection using canonical word IDs.
2. Saving sends a complete selected-word replacement with the captured door version.
3. Delete shows a confirmation naming the selected count and whether all records are selected.
4. Successful edit/delete refreshes the tree and the open panel, clears stale selection, preserves
   the active door, and announces the result.
5. A 409 discards no server data: refresh the panel, show a stale notice, and require the Owner to
   review the refreshed state before retrying.

## 10. Phase 7 — Copy through the existing linking workflow

### Frontend linking files to change or split

- `Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-operation-draft.models.ts`
- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-operation-draft.store.ts`
- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts`
- add a focused inline-source workflow controller before extending the facade

The workflow facade is already at its hard size boundary. First extract its direct-source draft
orchestration into a focused controller without changing behavior; then add a small entry point for
multiple prepared inline sources. The existing workspace and direct-source entry points must remain
unchanged.

### Copy preparation

1. The operations card shows target-door selection first, followed by `all door links` or
   `selected links (N)`. Archived and source doors are not valid targets.
2. Enumerate only the chosen record summaries. Fetch ayah details lazily for the next batch being
   prepared, then release them after that batch completes.
3. Build normal manual-Mushaf inline drafts:
   - pack independent records with different ayah IDs into one independent source;
   - keep duplicate independent records for the same ayah in separate sources;
   - keep every grouped record in its own grouped source;
   - copy selected word IDs and descriptions exactly.
4. Keep every request at or below the current 100-source limit. If more sources remain, create a
   visible batch queue; do not change the Backend request contract.
5. Start the existing linking workflow with the chosen target already set and proceed through its
   normal preparation and preflight review.
6. Show `batch X of Y`. The Owner reviews and confirms every batch; success may advance to the next
   batch, but no batch is confirmed automatically.
7. Stop the queue on failure, cancellation, stale source data, or stale target state. Preserve the
   remaining queue for an explicit retry after refresh.
8. After the final successful batch, refresh the target and source tree counts and close the copy
   state with one completion notice.

### Phase gate

- Independent and grouped shapes remain distinct after copy.
- Selected words and descriptions are identical to the source records.
- An unchanged target is reported by the existing preflight as already present.
- A target difference follows the existing update classification.
- More than 100 grouped records are copied through reviewed batches using the same link route.

## 11. Testing decision and verification

### Testing decision

No new automated test class, test method, Frontend unit file, or browser journey is included. The
change uses the retained route-smoke catalogue and existing verification gates. Any new permanent
test requires separate Owner approval.

### Required verification

1. Update the retained smoke route catalogue for the new public GET and Owner-only PATCH and POST
   actions.
2. Run the Backend build.
3. Run the retained Backend smoke lane to validate route discovery, binding, envelopes, and Owner
   authorization.
4. Run `Backend/scripts/check-api-contract` and confirm no generated contract drift.
5. From the Frontend directory, run independently and in order:
   - `npm run check:no-unit-specs`
   - `npm run typecheck:app`
   - `npm run build:verify`
6. Run `npm run check:golden-ui` for the inline-panel visual contract.
7. With the local Backend and Frontend running, the Owner manually verifies:
   - zero, independent, grouped, shared-source, and very large record counts;
   - first-click panel toggle and stable numbering;
   - full Quran text and selected-word display;
   - add/remove selected words and stale-edit recovery;
   - single, multi, and select-all deletion;
   - copy selected/all to an empty and a partially linked target;
   - a source containing more than 100 grouped records; and
   - tree-count refresh without an API restart.

## 12. Completion boundary

The feature is complete only when all phases pass their gates, no database migration or reset was
introduced, the direct Quran-link HTTP contract is unchanged, record counts are distinct and live,
all Quran rendering comes from existing authoritative hydration, and the Owner can display, edit,
delete, and copy link records from the Abwab tree without a modal.
