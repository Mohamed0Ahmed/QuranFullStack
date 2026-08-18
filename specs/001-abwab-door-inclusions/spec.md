# Feature Specification: Abwab Door Inclusions

**Feature Branch**: `feat/abwab-chapter-inclusion`

**Created**: 2026-08-17

**Status**: Draft

**Input**: User description: "Create the specification from `docs/feature-037-abwab-door-inclusions/abwab-door-inclusions-plan.md`."

## Clarifications

### Session 2026-08-17

- Q: Should V1 enforce product caps on active direct sources per target or inclusion-graph depth?
  → A: No hard V1 caps on direct sources or graph depth.
- Q: How must an edit workflow handle a physical one-to-many or many-to-one replacement of source
  records?
  → A: Preserve every logical source-record occurrence and deterministically transfer its active,
  overridden, or suppressed mapping state. If that state-preserving reconciliation is impossible,
  reject the complete edit atomically before any source or target change commits.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Build an Aggregate Door (Priority: P1)

An authorized Abwab curator can make one door the aggregate target of one or more source doors so
the target receives their existing Quran link records while every participating door keeps its
current section, parent, and tree position.

**Why this priority**: Creating a valid inclusion and receiving the source's current records is the
smallest useful version of the feature.

**Independent Test**: Add two live, unrelated source doors to one live target and verify that the
target shows both direct inclusion relationships and ordinary link records matching the sources,
while the tree placement and semantic relations of all doors remain unchanged.

**Acceptance Scenarios**:

1. **Given** a live target and a live source with independent and grouped records, **When** an
   authorized curator includes the source, **Then** the target receives records with the same ayahs,
   selected words, descriptions, and grouping before the action reports success.
2. **Given** an authorized curator starts from a live aggregate target door, **When** they
   right-click that target, select `تضمين الأبواب`, and open the source picker, **Then** the picker
   presents the same live door tree/list used on the main Abwab page, permits selecting multiple
   source doors from any section or tree position, keeps the target, already directly included
   sources, and archived doors unselectable, and submits the selected sources as one atomic action.
3. **Given** live source doors in different sections or at unrelated tree depths, **When** they are
   included in one batch, **Then** every inclusion is created and none of the doors moves in the
   hierarchy.
4. **Given** a proposed batch containing a self-inclusion, repeated source, existing direct
   inclusion, archived source, or any edge that would create a cycle, **When** the curator submits
   it, **Then** the complete batch is rejected and no inclusion or synchronized record from that
   batch remains.
5. **Given** a target that already has directly authored records, **When** a source is included,
   **Then** the direct and synchronized records coexist without merging their record lifetimes.

---

### User Story 2 - Keep Included Content Synchronized (Priority: P2)

An Abwab curator can continue working on a source door normally, confident that every reachable
aggregate door receives the same committed record additions, edits, and deletions without a later
catch-up period.

**Why this priority**: Durable one-way synchronization is the principal value beyond a one-time
copy and is required to keep aggregate doors trustworthy.

**Independent Test**: Include B in A and C in B, then add, edit, and delete a C record and verify
that B and A each reflect the complete change before the source action succeeds, with no change
flowing from either target back to C.

**Acceptance Scenarios**:

1. **Given** a source record that is actively synchronized to one or more targets, **When** its
   ayahs, selected words, descriptions, or grouping are edited, **Then** every still-synchronized
   target record is updated within the same successful action.
2. **Given** a source record synchronized through a chain of inclusions, **When** the record is
   added or deleted, **Then** the corresponding change reaches every downstream target in the
   acyclic graph.
3. **Given** several active paths that supply the same ayah or selected word to a target, **When**
   one supplying record is removed, **Then** the ayah or word remains visible while any other
   surviving record still supplies it.
4. **Given** any synchronization step cannot complete safely, **When** the initiating source or
   inclusion action is attempted, **Then** the initiating action and all of its propagated changes
   are rejected together.

---

### User Story 3 - Curate an Aggregate Locally (Priority: P3)

An Owner can edit or delete a synchronized record through the target door's existing link tools
without changing the source. The local choice remains stable for that source-record occurrence.

**Why this priority**: Aggregate doors need local editorial control while preserving the feature's
strictly one-way relationship.

**Independent Test**: Edit one synchronized target record and delete another, then edit both source
records and verify that the override is preserved, the suppressed record stays absent, and neither
target action changed the source.

**Acceptance Scenarios**:

1. **Given** an active synchronized record, **When** an Owner replaces selected words in the target,
   **Then** only the target record changes and later edits to the same source occurrence do not
   overwrite the target result.
2. **Given** an active synchronized record, **When** an Owner deletes it from the target, **Then**
   only that target occurrence is removed and later edits to the same source occurrence do not
   recreate it.
3. **Given** an overridden or locally suppressed target occurrence, **When** its source occurrence
   is deleted, **Then** the target occurrence or suppression state ends as applicable.
4. **Given** a suppressed source occurrence has ended, **When** a user later creates a new source
   record for the same visible Quran content, **Then** the new occurrence synchronizes normally.
5. **Given** direct and synchronized target records are selected together for deletion, **When**
   the Owner confirms the action, **Then** each direct record is deleted normally and each
   synchronized record is suppressed independently in one complete action.

---

### User Story 4 - Manage Inclusion Lifecycles Safely (Priority: P4)

An authorized curator can inspect both directions of a door's direct inclusion topology, understand
archived participants, detach a source, and later reattach it without losing unrelated content or
reviving old local choices.

**Why this priority**: Operators need predictable archive and detach behavior to maintain aggregate
doors over time.

**Independent Test**: Archive and restore source and target doors, detach an inclusion, and reattach
the same pair; verify preservation during archive, edge-owned cleanup on detach, and a fresh sync on
reattach.

**Acceptance Scenarios**:

1. **Given** a source with existing synchronized records, **When** the source is archived and later
   restored, **Then** target records and counts remain present throughout and no duplicate is
   created on restore.
2. **Given** an archived target, **When** its included sources change, **Then** synchronization keeps
   the target current for restore while user link and inclusion mutations against that target stay
   blocked.
3. **Given** an inclusion with active, overridden, and suppressed occurrences, **When** an authorized
   curator detaches it, **Then** all state owned by that edge is removed while source records,
   target-direct records, and records owned by other inclusions remain unchanged.
4. **Given** a previously detached source-target pair, **When** the source is included again, **Then**
   the source's current records synchronize as a fresh relationship without reusing prior
   suppressions or overrides.
5. **Given** any existing door, including an archived one, **When** a reader opens its inclusion
   topology, **Then** the reader sees its direct sources and direct consumer doors with archive
   status.

---

### User Story 5 - Use Included Records Through Existing Link Experiences (Priority: P5)

A reader or Owner sees synchronized content through the existing Abwab counts, record list, ayah
rendering, selected-word highlights, edit, delete, and copy experiences without being shown origin
or synchronization metadata.

**Why this priority**: Inclusion should extend the current content experience rather than create a
second interpretation or presentation of a door's Quran content.

**Independent Test**: Open an aggregate door using the current link experience, inspect and copy a
synchronized record, and verify that all content renders normally, no origin is exposed, and the
copy is an ordinary direct record with no inclusion relationship.

**Acceptance Scenarios**:

1. **Given** a target containing direct and synchronized records, **When** a reader opens the
   existing link count and record list, **Then** every live record appears in the current grouped or
   independent presentation with no source door, origin badge, or synchronization state.
2. **Given** a synchronized record, **When** an Owner copies it to another door, **Then** the
   destination receives an ordinary direct record and no inclusion relationship or synchronization
   ownership is copied.
3. **Given** source propagation changes an already-open target, **When** the view detects that its
   door version is stale, **Then** it recovers through the existing refresh behavior without
   silently editing an obsolete record set.

### Edge Cases

- A target and source can already have a semantic relation; the independent inclusion may still be
  created when all inclusion rules pass.
- Multiple sources or inclusion paths may supply identical ayahs and selected words; record rows
  remain separate while visible door-level membership is distinct.
- Removing one supplier must not remove an ayah or selected word still supplied by a direct record
  or another synchronized record.
- An edit process that replaces source records internally must preserve every logical occurrence and
  deterministically transfer its active, overridden, or suppressed synchronization state without
  reintroducing content; if a one-to-many or many-to-one reshape cannot do so, the complete edit is
  rejected before any source or target change commits.
- Renaming, refreshing, archiving, restoring, or reconfirming an unchanged source occurrence must
  not end its suppression or override.
- A source can be archived after inclusion but cannot be selected for a new inclusion while
  archived.
- An archived target continues to receive internal synchronization but rejects user mutations.
- Concurrent link and topology actions must not create a cycle, accept stale target state, or leave
  source and target records out of sync.
- Detaching one of several paths must remove only records and state owned by that direct edge;
  another path may keep the same Quran content visible.
- A missing door or inclusion, invalid identifier, empty source selection, stale target version, or
  safe-completion failure must produce a controlled outcome without a partial mutation.
- Physical door deletion is outside the feature; no inclusion state may be silently orphaned if a
  future deletion capability is introduced.

## Requirements *(mandatory)*

### Terminology

| Concept | English term | Arabic UI term |
| --- | --- | --- |
| Capability | Door inclusion | تضمين الأبواب |
| Door receiving synchronized links | Target door / aggregate door | الباب الجامع |
| Door supplying links | Source door / included door | الباب المُضمَّن / مصدر المحتوى |
| Stored directed relationship | Door inclusion | تضمين |
| One source-record lifetime | Source record occurrence | نسخة الربط الحالية |
| Materialized target record | Synchronized record | رابط في الباب الجامع |
| Target deletion of the current occurrence | Local suppression | حذف من الباب الجامع |
| Target edit of the current occurrence | Local override | تعديل داخل الباب الجامع |

An inclusion MUST NOT be described as a parent, child, hierarchy edge, comprehensiveness relation,
bidirectional relation, read-time union, or manual copy.

### Functional Requirements

- **FR-001**: The system MUST treat door inclusion as an independent directed relationship in which
  one target door includes one source door; it MUST NOT alter hierarchy placement or semantic door
  relations.
- **FR-002**: An authorized curator MUST be able to add multiple source doors to one live target in
  a single action, and the complete submitted batch MUST either succeed or fail together.
- **FR-003**: The system MUST permit target and source doors from different sections and unrelated
  tree depths.
- **FR-004**: The active inclusion graph MUST reject self-inclusion, duplicate direct inclusions,
  repeated source IDs within a submitted batch, and every direct or transitive cycle.
- **FR-005**: The system MUST permit several active paths between doors while maintaining separate
  synchronization ownership for each direct inclusion.
- **FR-006**: A target MUST be able to retain ordinary directly authored records alongside records
  synchronized through inclusions.
- **FR-007**: Version 1 MUST NOT introduce authored ordering of source doors or one action that adds
  sources to several target doors.
- **FR-008**: Creating an inclusion MUST synchronize every current live source record to the target,
  preserving its grouped or independent shape, ayahs, selected canonical Quran words, and ordered
  descriptions.
- **FR-009**: Creating, editing, or deleting a live source record through any supported workflow
  MUST create, update, or remove every corresponding still-synchronized target record.
- **FR-010**: Every target-visible synchronized-record change, including source changes, local
  target overrides or suppressions, and detach cleanup, MUST propagate transitively through every
  reachable consumer door in the active acyclic graph.
- **FR-011**: An initiating source or inclusion mutation MUST report success only after all required
  reachable target changes are complete.
- **FR-012**: Failure to complete any required synchronization change MUST reject the initiating
  mutation and every associated target change together; accepted source-to-target drift is not
  permitted.
- **FR-013**: Synchronization state MUST be associated with the lifetime of the source record
  occurrence, not merely its visible ayah or Quran content.
- **FR-014**: Editing an existing source record MUST keep the same occurrence identity; if an
  internal replacement is unavoidable, every active, overridden, or suppressed mapping MUST move
  to the replacement within the same complete action. A one-to-many or many-to-one physical reshape
  MUST preserve every logical occurrence and transfer every mapping state deterministically; when
  that is impossible, the complete edit MUST be rejected before any source or target change commits.
- **FR-015**: A mutation performed on a synchronized target record MUST NOT change the source door,
  source record, source content, source operation, or source version.
- **FR-016**: Deleting a synchronized target record MUST remove the target record and locally
  suppress only that source record occurrence.
- **FR-017**: Editing a locally suppressed source occurrence MUST NOT recreate its target record.
- **FR-018**: Deleting a suppressed source occurrence MUST end its suppression, and a later explicit
  source link creation MUST be treated as a new occurrence eligible for synchronization.
- **FR-019**: Editing selected words on a synchronized target record MUST create a local override
  whose result is not overwritten by later edits to the same source occurrence.
- **FR-020**: Deleting the source occurrence MUST remove its overridden target record, and ending the
  inclusion MUST remove the override owned by that inclusion.
- **FR-021**: Direct target records MUST retain their existing edit and delete behavior and MUST
  survive source deletion, source archive, and inclusion detach.
- **FR-022**: Copying a synchronized record MUST create an ordinary direct record in the destination
  without copying the inclusion relationship, source occurrence ownership, suppression, or
  override state.
- **FR-023**: The target MUST retain separate grouped and independent record occurrences even when
  several records refer to the same ayah.
- **FR-024**: At door level, each ayah and each selected canonical word within that ayah MUST appear
  once while at least one surviving direct or synchronized record supplies it.
- **FR-025**: Removing one supplying record MUST remove an ayah or selected word from the target only
  when no other surviving direct or synchronized record supplies it.
- **FR-026**: Synchronized records MUST appear through the existing link counts, selected-word
  counts, record lists, ayah rendering, highlights, edit, delete, bulk delete, and copy experiences.
- **FR-027**: Existing link counts and selected-word counts MUST retain their current meanings; the
  feature MUST NOT introduce separate direct/effective metrics or a separate content mode.
- **FR-028**: Content experiences MUST NOT expose a synchronized record's source door, inclusion,
  origin, internal ownership, suppression state, override state, or other source attribution.
- **FR-029**: Archiving a source door MUST preserve its existing inclusions, synchronized target
  records, counts, and local synchronization state, and restoring it MUST NOT duplicate records.
- **FR-030**: An archived source MUST remain visible in existing topology but MUST NOT be selectable
  for a new inclusion.
- **FR-031**: Archiving a target MUST preserve its direct records, synchronized records, inclusions,
  and synchronization state; source changes MUST continue to keep it current for restore.
- **FR-032**: User link and inclusion mutations against an archived target MUST be rejected, and
  restoring the target MUST NOT duplicate records.
- **FR-033**: Detaching an inclusion MUST remove its active and overridden target records, its
  suppressed occurrences, and all other state owned by that edge while leaving source records,
  target-direct records, and other inclusions unchanged.
- **FR-034**: Record removals caused by detaching an inclusion MUST propagate to doors that include
  the affected target.
- **FR-035**: Reattaching a previously detached target/source pair MUST create a fresh relationship
  from the source's current live records and MUST NOT restore retired suppressions or overrides.
- **FR-036**: Any reader MUST be able to inspect a door's direct source inclusions and direct
  consumer inclusions, including door identity, name, and archive status, for either a live or
  archived requested door.
- **FR-037**: Door discovery surfaces MUST show separate direct source-inclusion and
  consumer-inclusion counts without changing hierarchy, semantic relation, link, selected-word, or
  child-count meanings.
- **FR-038**: Creating and deleting inclusions MUST use an independent inclusion permission group;
  read access to inclusion topology MUST remain public.
- **FR-039**: Existing Owner-only authorization for link edits and deletions MUST remain unchanged
  when the affected record is synchronized.
- **FR-040**: A stale target version, invalid input, missing entity, archived mutation target,
  unauthorized mutation, graph conflict, or safe-completion failure MUST produce a controlled
  outcome and MUST NOT leave a partial mutation.
- **FR-041**: Inclusion management MUST be visually and conceptually separate from semantic door
  relations and MUST use the agreed terms "Door inclusion," "Target/aggregate door," and
  "Source/included door."
- **FR-042**: The Arabic management experience MUST use `تضمين الأبواب` for the capability,
  `الباب الجامع` for the target, and `الباب المُضمَّن` or `مصدر المحتوى` for a source.
- **FR-043**: The inclusion management experience MUST show direct `مصادر الباب` and direct
  `يُستخدم في أبواب جامعة` topology and distinguish archived doors with text or an icon rather
  than color alone.
- **FR-044**: The source picker MUST exclude the target door, active direct sources, and archived
  doors; system validation MUST remain authoritative for all graph rules.
- **FR-045**: Detach confirmation MUST state that the source remains unchanged while synchronized
  target records owned by the inclusion are removed.
- **FR-046**: Inclusion management MUST provide distinct loading, refreshing, empty, error, and
  success-notice states with calm, actionable Arabic messaging.
- **FR-047**: The management experience MUST support right-to-left layout, keyboard source
  selection, focus restoration, confirmation focus, and live announcements without changing or
  animating Quran text.
- **FR-048**: Every target door changed by propagation MUST receive a new observable version before
  success so an open stale view cannot silently mutate an obsolete record set.
- **FR-049**: Internal synchronization-only records and identities MUST NOT be accepted as
  user-authored Quran link sources or exposed in public content responses.
- **FR-050**: The feature MUST NOT modify canonical Quran data, door hierarchy placement, existing
  semantic relation meanings, or deployment boundaries.
- **FR-051**: Version 1 MUST NOT impose a hard product cap on active direct source inclusions per
  target or on inclusion-graph depth; all graph validity and atomic synchronization rules MUST
  apply regardless of topology size.
- **FR-052**: The inclusion-management experience MUST compose the complete target-first flow: an
  authorized curator starts from one aggregate target door, right-clicks it and selects
  `تضمين الأبواب`, or invokes the same target context menu by keyboard; then chooses one or multiple
  source doors, from any section or tree position, through the same live door tree/list used on the
  main Abwab page. The picker MUST apply the target, existing-direct-source, and archived-door
  exclusions and authoritative graph validation defined by **FR-044**. Submission MUST use the
  atomic one-target batch defined by **FR-002**, and the experience MUST provide neither the
  source-first nor multi-target flow prohibited by **FR-007**.

### Scope Boundaries

**In scope**:

- Directed inclusion management for one target and one or more live source doors.
- Immediate initial synchronization and durable transitive one-way synchronization of normal link
  records.
- Target-local edit overrides and occurrence-scoped deletion suppression.
- Source and target archive behavior, detach cleanup, and fresh reattachment.
- Public direct-topology reads, permission-classified topology writes, topology counts, and Arabic
  inclusion management.
- Compatibility with the existing link content, count, edit, delete, bulk-delete, and copy
  experiences.

**Out of scope**:

- Moving doors, changing their hierarchy or tree order, or representing inclusion as a semantic
  relation.
- Bidirectional synchronization or any target-to-source mutation.
- A read-time content union, eventual background propagation, or a separate effective-content
  delivery path, tab, cache, or screen.
- Source attribution on individual records, ayahs, words, or descriptions.
- Flattening grouped and independent records into a synthetic ayah-only list.
- Permanent ayah blacklists or allowing an edit of the same suppressed occurrence to recreate
  target content.
- Removing synchronized content merely because a source is archived, or creating a new inclusion
  to an archived source.
- Authored source ordering or a multi-target inclusion command.
- Hard deletion of doors, changes to Quran data, a Quran renderer redesign, or a deployment change.

### Key Entities

- **Abwab Door**: An existing door with a stable identity, name, lifecycle state, hierarchy
  placement, version, normal link records, and direct inclusion topology.
- **Door Inclusion**: One durable directed relationship from a target door to a source door. It owns
  the synchronized records and local states created through that direct edge and has an active or
  detached lifetime.
- **Source Record Occurrence**: One lifetime of a source door's normal link record. It includes the
  grouped/independent shape, ayahs, selected canonical Quran words, and ordered descriptions. An
  edit keeps the occurrence; deletion ends it; a later explicit link creation starts a new one.
- **Synchronized Target Record**: A normal target-door link record materialized from one source
  occurrence through one inclusion. It participates in existing content and count behavior but
  reveals no source attribution.
- **Synchronization Mapping**: Internal ownership connecting one inclusion and source occurrence to
  its target record and current state. It is active while source edits flow through, overridden
  after a target-local edit, or suppressed after a target-local deletion.
- **Direct Target Record**: A normal record authored directly in the target. It has no inclusion
  owner and is unaffected by inclusion detach or source lifecycle changes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In acceptance testing, 100% of a newly included source's current live records appear
  in the target with matching grouping, ayahs, selected words, and descriptions before the include
  action reports success.
- **SC-002**: Across every supported source-record workflow, 100% of committed additions, edits,
  and deletions reach every applicable target in a three-level inclusion chain before the source
  action reports success; failed actions leave zero partial source or target changes.
- **SC-003**: In all local-curation scenarios, 100% of target edits and deletions leave the source
  unchanged; same-occurrence source edits overwrite zero overrides and recreate zero suppressions;
  every one-to-many or many-to-one reshape that cannot preserve and deterministically transfer all
  occurrence states is rejected with zero source or target changes.
- **SC-004**: Self-inclusion, duplicate direct inclusion, repeated batch sources, archived-source
  creation, stale-target actions, and direct or transitive cycles are rejected in 100% of
  acceptance cases with zero partially created inclusions or records.
- **SC-005**: For every duplicate-content case in the acceptance matrix, visible target ayahs and
  selected words equal the distinct union supplied by surviving records, with zero premature
  removals.
- **SC-006**: Archive, restore, detach, and reattach acceptance cases produce zero duplicate
  records, zero loss of unrelated direct or synchronized records, and zero reuse of retired
  suppressions or overrides.
- **SC-007**: In 100% of inclusion-management acceptance cases, an authorized curator can open
  `تضمين الأبواب` from one aggregate target door's context menu, select one or multiple source
  doors from the same live door tree/list used on the main Abwab page, and submit all selected
  sources as one atomic action.
- **SC-008**: All inclusion-management actions are completable by keyboard alone, preserve expected
  focus, announce state changes, and communicate archive state without relying on color in 100% of
  the supported responsive layouts.
- **SC-009**: Public readers can inspect 100% of direct inclusion topology, while unauthorized
  inclusion mutations are rejected and authorized inclusion mutations succeed in 100% of the
  permission acceptance cases.
- **SC-010**: Existing content screens expose zero source-door, origin, synchronization-state, or
  internal-ownership labels and require zero new content modes to view or manage synchronized
  records.
- **SC-011**: The complete acceptance matrix shows zero changes to Quran data, hierarchy placement,
  semantic relation meanings, direct-record behavior, or existing count definitions.

## Assumptions

- Existing Abwab authentication, Owner bypass, permission assignment, archive/restore, tree,
  version-conflict handling, and normal link workflows remain available and keep their current
  behavior unless this specification explicitly changes an outcome.
- Arabic and right-to-left presentation are the product baseline, and the existing inclusion-adjacent
  modal, picker, confirmation, action, responsive, and state patterns are reused.
- Existing link records already support independent and grouped shapes, ayahs, selected canonical
  Quran words, ordered descriptions, bulk deletion, and copying.
- Existing door-level ayah and selected-word membership represents the distinct union across all
  surviving records and remains the authoritative visible behavior.
- The current product archives doors rather than physically deleting them; any future hard-delete
  capability must reconcile inclusion dependents separately and is not authorized here.
- Existing records require no inclusion backfill because no inclusion relationships exist before
  this feature.
- Non-functional timing targets and performance service levels are deferred to `speckit-plan`; this
  specification requires only that no partial state is exposed and no action reports success before
  all required changes complete.
- Specification approval authorizes planning only. Generating or applying the required data-schema
  migration, changing stored data, implementing the feature, or deploying it requires the
  corresponding later authorization.
- The current test freeze remains in effect: this feature does not authorize new permanent
  automated tests. Existing retained exact-contract protections may be minimally updated when
  their owned subject changes; any new permanent test requires separate owner authorization.
- Verification planning must cover every supported path that creates, edits, or removes a live
  source record, including retained prepared or maintenance workflows, plus the complete manual
  behavior matrix defined by the source plan.
