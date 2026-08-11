# Abwab Linking Frontend Prototype V2 Implementation Plan

## Objective

Convert the currently implemented V1 Angular Linking prototype into the approved V2 frontend UX
without restarting the feature or implying a real Linking backend.

An authenticated active Owner can:

- keep several independently configured Quran sources in an actor-bound workspace;
- choose one or several prepared sources for the current operation;
- edit each source's ayah inclusion independently;
- enable/disable automatic word contributions or choose manual Mushaf words;
- resolve selected sources completely;
- review one deduplicated Quran display keyed by verseKey;
- preserve separate, source-owned link intents behind that display;
- choose one currently live Abwab Door;
- complete only the frontend mock result تم الربط بنجاح;
- close/reopen the browser and recover prepared rows/configuration without recovering transient
  operation membership.

The implementation remains frontend/UI prototype work. It does not create real links, durable
groups, requests, approvals, audit records, or server-owned workspaces.

## Planning Authority and Conflict Resolution

Use these authorities in order:

1. The attached V2 implementation-plan brief.
2. docs/abwab-linking-frontend-prototype-v2-report.md.
3. Current frontend code and the nearest current-truth READMEs.
4. docs/abwab-linking-frontend-prototype-implementation-plan.md as V1 historical context only.

The new brief resolves one item that the report left for later discussion:

**Merged review deduplication and link intent are separate contracts.**

- MergedLinkingSelection exists only to show each ayah/word occurrence once.
- Every automatic source produces independent per-ayah link units.
- Only a manual Mushaf source can produce one grouped multi-ayah unit.
- A merged display must never be used to reconstruct, widen, or flatten source link intent.

Example:

    Manual Mushaf source: grouped A + B -> intent units [[A, B]]
    Lemma source: independent A + C   -> intent units [[A], [C]]
    Merged review display             -> A, B, C once each

The frontend mock carries both sibling products—merged display and ordered source intents—through
review and confirmation. Real backend representation remains deferred.

## Locked Scope

- Modify only the Angular frontend and frontend current-truth READMEs during later implementation.
- Reuse existing read APIs; do not add or alter API endpoints or generated API contracts.
- Keep Linking fail-closed and Owner-only. Every state mutation and mock confirmation rechecks live
  access; visibility is not the only gate.
- Keep Direct Link as an explicit one-source shortcut. It must not add a source to the workspace.
- Keep automatic source families: Unique Word simple/tashkeel, Root, Lemma, Stem, and Word Type
  word/grouped dimensions.
- Remove only selected-Mushaf-word Linking. Preserve ordinary word study, analysis, keyboard
  behavior, URL state, glyph text, word order, marker behavior, font, spacing, and line metrics.
- Add a manual Mushaf ayah source and an Owner-only reader action named تحديد.
- Use one primary Linking shell. The shared remove-all confirmation alertdialog is the only
  intentional nested modal exception.
- Give each Linking surface exactly one vertical scrolling owner.
- Use approximately 80vw by 88dvh on Wide/Medium; preserve the shared Compact 94dvh behavior.
- Use client paging for visible ayah cards. Keep complete source loading and accept its bounded V2
  prototype request/memory cost; do not introduce virtualization without later evidence.
- Keep workspace persistence temporary, actor-bound, versioned, and localStorage-backed behind a
  replaceable repository port.
- Keep final completion mock-only with no write, cache invalidation, durable ID, or history.
- Do not add, delete, rename, or modify automated tests.

## Explicit Non-Goals

- Backend Linking commands or writes.
- Linking/workspace/group database schema, entities, migrations, repositories, or transactions.
- Real per-user server persistence or cross-device synchronization.
- Backend grouped-link storage or server-issued group IDs.
- Canonical manual-word batch-resolution API.
- Permission/authorization redesign, Admin Linking, or fake Admin behavior.
- Approval/review workflows, audit history, notifications, realtime, or link history.
- Cross-device or same-actor cross-tab conflict resolution.
- New Quran data or corrected Quran source data.
- Global Quran/Mushaf search.
- Virtual scrolling without measured implementation evidence.
- Automated test work.

## V1 Reuse / Refactor / Retire Map

| V1 element | V2 action |
| --- | --- |
| LinkingAccessService and Owner gate | Reuse unchanged in authority; repeat checks in every new mutator |
| Navbar workspace trigger/count | Reuse; prepared-row count remains distinct from checked-operation count |
| Root-mounted Linking host | Refactor to an eager primary shell with deferred inner surfaces |
| App inert/aria-hidden composition | Reuse and extend only for the remove-all alertdialog exception |
| Source descriptors and stable source keys | Preserve automatic identities; add manual ayah-set identity |
| LinkingSelection all-except/only helper | Reuse per prepared source |
| CompletePagedSourceLoader | Reuse unchanged for automatic sources |
| Automatic source resolvers/registry | Reuse one-source boundaries; keep source-set knowledge outside |
| Neutral LinkingAyah resolver output | Reuse per source; add only truthful occurrence coordinates needed by manual reads |
| WorkspaceStore | Refactor from scalar active-source semantics to prepared rows plus transient checked membership |
| LinkingWorkspaceSession | Retire; replace with codec + repository port + localStorage adapter |
| Large workspace cards | Reshape into qdResultList rows |
| Inert edit-selection action | Replace with a real source-ayah editor surface |
| Scalar Direct Link facade/command | Split and refactor around an operation snapshot, coordinator, merge, intents, and mock |
| Abwab snapshot and Door picker | Reuse; correct select-only radio and live/selectable validation |
| Linking ayah selection/card components | Reuse with semantic, paging, merged-provenance, and accessibility changes |
| Words source action/adapters | Reuse; update target size, feedback identity, and flow handoff |
| Selected Mushaf word Linking | Retire completely without touching normal study |
| V1 session payload/key | Do not migrate; it cannot express V2 semantics safely |

## Target Frontend Architecture

    Words source action
      -> one ephemeral Direct Link member

    Prepared workspace
      -> zero or more checked source keys
      -> immutable ordered operation-member snapshot

    Manual Mushaf تحديد
      -> manual ayah-set descriptor
      -> prepared workspace row

    Operation members
      -> resolve each source independently
      -> reconcile each member against its own universe
      -> apply its ayah inclusion and word behavior
      -> atomic source-set result
         -> pure merged display by verseKey
         -> pure ordered per-source link intents
      -> one live Door
      -> merged review + preserved intents
      -> mock command
      -> تم الربط بنجاح

Ownership boundaries:

- WorkspaceStore owns prepared rows/configuration, transient checked keys, surface/editor targets,
  local revisions, and one undo snapshot.
- LinkingWorkspaceRepository owns only serialized prepared workspace data for one actor.
- LinkingSourceResolver and its registry continue resolving exactly one descriptor.
- LinkingSourceSetCoordinator owns multi-source loading, progress, cancellation, reconciliation,
  and atomic publication.
- Pure merge utilities own display deduplication and source-intent derivation without side effects.
- LinkingWorkflowFacade owns step navigation, Door state, review, mock execution, and origin return;
  it delegates source-set work instead of growing beyond its current responsibility.
- Components render signals and dispatch commands; they do not merge, persist, or call source APIs
  directly.

## Persistence and Identity Invariants

Persist only:

- ordered source descriptors and stable source keys;
- validated display snapshots;
- per-source ayah-inclusion overrides;
- automatic word-contribution preference;
- manual wordLocations grouped by verseKey;
- manual grouped/independent preference;
- optional explicitly stale last-resolved unique-ayah count.

Never persist:

- checked source keys;
- loaded ayahs, Quran text, API DTOs, presentation occurrence slots, or merged display;
- modal/editor/step state, focus origins, loading/progress/errors, selected Door, or mock result.

Identity rules:

- verseKey is the source-backed ayah merge identity.
- The current coarse verse-key parser is structural only. Operation membership must be proven by a
  successful source/page/study read and ordered numerically by surah/ayah.
- quranWordId is canonical only when an existing read supplies it.
- wordLocation is a temporary manual Mushaf coordinate, never a canonical backend word ID.
- renderPosition, array index, wordNumber, lineWordOrder, and text alone are never persisted as
  canonical identity.
- A validated verse-scoped occurrence slot can deduplicate presentation only; it never becomes an
  intent or persisted word ID.

## Phase Dependency Map

| Phase | Depends on | Unlocks |
| --- | --- | --- |
| 1. Retire selected-word Linking | None | Removes the obsolete source before V2 state persists |
| 2. Define V2 contracts and identities | Phase 1 | Store, manual source, merge, and set-aware workflow |
| 3. Rebuild workspace state and persistence | Phase 2 | Durable prepared rows and transient operation membership |
| 4. Rework the global shell/surface/focus boundary | Phase 3 | Real editors and shared flow inside one shell |
| 5. Build the source-ayah configuration editor | Phases 2–4 | Working count editor and reconciled source configuration |
| 6. Add manual Mushaf data loading and word editor | Phases 2–5 | Complete manual sources without reader entry yet |
| 7. Add atomic source-set resolution, merge, and intents | Phases 2, 3, 5, 6 | Set-aware operation result |
| 8. Converge Direct Link and workspace flow | Phases 4 and 7 | Shared Door/review/mock pipeline |
| 9. Replace cards with the dense workspace | Phases 3, 5, 6, 8 | Complete multi-source composition surface |
| 10. Add Mushaf ayah-selection mode | Phases 3, 4, 6, 8, 9 | Manual Mushaf entry into the complete pipeline |
| 11. Harden integrations and current-truth docs | Phases 1–10 | Final implementation verification |

## Transitional Compatibility and Cutover Rules

- Treat phases as bounded implementation/review increments, not independently releasable product
  versions. In particular, do not release the Phase 1 selected-word removal without Phase 10's
  replacement Mushaf entry and the V2 workspace/flow beneath it.
- Preserve existing automatic descriptor identity and source keys. Existing Words call sites may
  migrate behind short-lived compile adapters, but those adapters may only translate call shape;
  they must not reinterpret source meaning or persist V1 fields.
- Do not run V1 and V2 persistence as dual truth. Once Phase 3 cuts over, do not read, migrate, or
  write qd-linking-workspace-v1. A V1 mushaf-word row, scalar highlight flag, result count, or active
  source cannot hydrate V2 configuration or checked membership.
- Do not preserve the scalar V1 command by flattening an operation into selectedVerseKeys. The V2
  mock command becomes usable only after it can carry the sibling merged display and per-source
  intents from Phase 7.
- During component migration, keep checked membership empty by default and require explicit
  selection. Never emulate missing V2 membership by treating every prepared V1 row as checked.
- Reconcile source results only through the current complete resolver/read. Persisted labels,
  counts, page hints, verse syntax, and wordLocation coordinates are not restored source truth.
- Remove compatibility selectors/fields immediately after their final caller moves in Phase 11;
  do not leave a hidden old Direct Link, selected-word action, session path, or intent-reconstruction
  fallback.
- No phase fabricates a temporary client group ID. Manual grouped intent is an explicit nested
  ayah unit until a later backend supplies its own durable representation.

## Automated Test Freeze and Later Implementation Gates

No phase may modify or create automated tests.

During later implementation—not while authoring this plan—run the existing frontend gates after
each coherent phase:

    cd Frontend/quran-dashboard-ui
    npm run check:no-unit-specs
    npm run typecheck:app
    npm run build:verify

For any phase changing templates/styles, run npm run check:golden-ui before build:verify.

Browser/manual work is reserved for the final verification matrix after the implementation is
complete enough to exercise whole flows. It is not an automated test substitute and is not part of
creating this plan.

## Phase 1 — Retire Selected-Mushaf-Word Linking

### Objective

Remove the obsolete Mushaf word source atomically before any V2 workspace payload can preserve or
reintroduce it, while leaving ordinary Mushaf word selection/study byte-for-behavior equivalent.

### Dependencies

None.

### Exact Frontend Files / Areas

Modify:

- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.ts
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.html
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.scss
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-source.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking.labels.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-key.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/README.md
- Frontend/quran-dashboard-ui/src/app/features/mushaf/README.md

Delete:

- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/resolvers/mushaf-word-linking-source.resolver.ts

Preserve:

- MushafWordComponent and its current study event emission.
- StudyContextSectionComponent and all analysis/morphology/identity content.
- Mushaf word-analysis API, runners, cache, URL/session state, and Golden UI mushaf-word selector.

### State / Model Changes

- Remove mushaf-word from LinkingSourceKind and LinkingSourceDescriptor.
- Remove its runtime guard, label, stable-key branch, resolver registration, and injection.
- Keep all automatic source descriptor identities unchanged.

### UI / Component Changes

- Remove only إضافة للربط / ربط مباشر from selected-word study.
- Remove only the Linking wrapper class/style.
- Do not change the selected-word layout reservation, loading states, tabs, text, or study controls.

### Data-Flow Changes

- Selected-word analysis no longer constructs or dispatches a Linking descriptor.
- The remaining analysis/read flow is untouched.

### Persistence Implications

- Current V1 session decoding naturally drops the retired invalid descriptor while retaining valid
  automatic rows.
- Do not migrate or translate a Mushaf word row into a manual ayah row; the meanings are different.

### Acceptance Criteria

- No selected-word Linking action is rendered.
- No mushaf-word branch or resolver remains in Linking.
- Unique/Root/Lemma/Stem/Word Type descriptors and actions still compile against their existing
  keys.
- Selecting/studying a Mushaf word behaves exactly as before outside Linking.
- Only the named production files and their truthful READMEs change; no tests or backend files do.

### Explicit Out-of-Scope Boundary

- No replacement Mushaf selection UI in this phase.
- No renderer/glyph/style redesign.
- No manual ayah descriptor or grouped-link model yet.

## Phase 2 — Define V2 Source, Configuration, Merge, and Intent Contracts

### Objective

Add the smallest explicit frontend contracts needed for prepared source rows, manual Mushaf ayahs,
multi-source operations, deduplicated display, and non-flattenable source link intents before
runtime orchestration changes.

### Dependencies

Phase 1.

### Exact Frontend Files / Areas

Modify:

- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-source.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-ayah.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workspace.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workflow.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking.labels.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-key.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/README.md

Add:

- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-manual-mushaf.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-operation.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-merge.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/manual-link-shape.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-presentation.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-verse-order.ts

Read-only terminology authority:

- Frontend/quran-dashboard-ui/src/app/features/words/models/words-shared.labels.ts

### State / Model Changes

- Add manual-mushaf-ayahs with one or more source-backed verse references and page/display hints.
- Keep manual identity equal to the deduplicated verse set in numeric Quran order. Page hints,
  labels, ayah overrides, words, and grouping preference do not alter sourceKey.
- Retain descriptor.label as a validated human-readable display snapshot; combine it with typed
  family/mode/scope rather than using it as identity.
- Define source configuration as a discriminated union:
  - automatic: ayah inclusion plus automaticWordMatchesEnabled;
  - manual: ayah inclusion, wordLocations by verseKey, and stored grouped/independent preference.
- Initialize a newly prepared automatic source with automaticWordMatchesEnabled=true. Re-preparing
  an equivalent source preserves its existing preference rather than resetting it to the default.
- Define an immutable LinkingOperationMember snapshot with sourceKey, descriptor, captured
  configuration, origin, and configuration revision.
- Define MergedLinkingSelection and MergedAyahSelection for display only.
- Define ordered LinkingSourceIntent records containing explicit intent units:
  - automatic source with A+C -> [[A], [C]];
  - manual grouped A+B -> [[A, B]];
  - manual independent A+B -> [[A], [B]].
- Each intent also retains its source descriptor/provenance and source-owned per-ayah word
  contributions with identity classification: canonical quranWordId when supplied, validated
  presentation occurrence when canonical identity is absent, or manual wordLocation coordinate.
  The latter two are never labelled backend-ready canonical IDs.
- Keep stored manual preference when inclusion leaves one ayah; derive effective single intent
  without erasing the preference.
- Distinguish canonical quranWordId, manual wordLocation, and presentation-only occurrence slot.

### UI / Component Changes

- Add typed Arabic source presentation for mode/scope/value:
  - كلمات فريدة بدون تشكيل / بالتشكيل;
  - جذر;
  - الصيغة المعجمية;
  - الأصل الصرفي;
  - complete Word Type selection/scope;
  - single/multi manual Mushaf ayah labels.
- Do not change rendered workspace structure in this phase.

### Data-Flow Changes

- Keep one-source LinkingAyah resolver output neutral.
- Cross-source provenance belongs only to operation/merge outputs.
- Treat structural verse parsing as insufficient for operation truth; later coordinators require
  source/study membership.

### Persistence Implications

- Mark only descriptor/configuration/display snapshot as persistable.
- Make checked membership, load state, merged display, source intents, Door, focus, and mock result
  explicitly transient in the contracts.
- Do not change the storage implementation until Phase 3.

### Acceptance Criteria

- Existing automatic source keys remain stable.
- Equivalent manual verse sets produce one key regardless of selection order or page hint.
- Impossible automatic/manual word-configuration combinations are unrepresentable.
- Merged display and source intent are sibling outputs; neither can be derived from the other.
- No model describes wordLocation or a presentation slot as canonical.
- Canonical Lemma/Stem terminology matches Words.

### Explicit Out-of-Scope Boundary

- No store cutover, localStorage, source resolution, merge implementation, UI, or backend entity.
- No durable group ID or backend request shape.

## Phase 3 — Rebuild Workspace State and Actor-Bound Persistence

### Objective

Replace scalar prepared-source/session state with independently configured rows, transient
operation membership, and an actor-bound V2 repository abstraction backed by localStorage.

### Dependencies

Phase 2.

### Exact Frontend Files / Areas

Modify:

- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workspace.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-selection.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/quran-source-linking-actions/quran-source-linking-actions.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/README.md

Add:

- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-workspace.repository.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/local-storage-linking-workspace.repository.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-workspace.codec.ts

Delete after the store is switched:

- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace-session.ts

Read-only actor authority:

- Frontend/quran-dashboard-ui/src/app/core/auth/current-user.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-access.service.ts

### State / Model Changes

- Keep ordered prepared items with source-specific configuration.
- Add ordered checkedSourceKeys as transient operation intent; initialize/reset it empty.
- Replace scalar activeSourceKey with explicit editorSourceKey and surface state.
- Add a local per-item configurationRevision for conditional reconciliation writeback.
- Add an actor generation and durable workspace revision so a late hydrate/save from actor A cannot
  publish after the app has moved to actor B or a newer in-memory snapshot.
- Rename resultCount semantics to optional lastResolvedCount and always mark it stale until a
  successful current resolution.
- Add one transient removed-item undo snapshot.
- Separate commands: addSource, check/uncheck source, clear checked sources, open ayah editor, open
  manual-word editor, remove, undo remove, request/confirm clear all, and capture operation members.
- Re-adding an equivalent source refreshes display metadata but preserves order/configuration.
- Repeat the Owner gate inside every public mutation.

### UI / Component Changes

- Keep current workspace components compiling through selectors/adapters until Phases 5 and 9.
- Explorer Add to Workspace retains focus and announces add/already-exists; it does not open the
  workspace or focus a hidden row.

### Data-Flow Changes

- Source-row checking changes only checkedSourceKeys.
- Ayah edits change only that row's inclusion.
- Automatic/manual word edits change only that row's word configuration.
- Begin storage work only after authentication is resolved and the active subject passes the Owner
  gate; authentication loading is not logout and must not trigger bucket reads or invalidation.
- Store hydration completes before rows render, but never marks them resolved or checked.
- A captured operation receives immutable row snapshots and revisions.

### Persistence Implications

- Introduce async-capable repository load/save/invalidate-active-actor operations; invalidation is
  scoped to one exact actor key and is used only for that actor's malformed/mismatched payload.
- Use qd-linking-workspace:v2:<encodeURIComponent(actorSub)>; repeat version and exact actorSub
  inside the payload.
- Serialize same-tab saves by durable revision so an earlier slow save cannot overwrite a later
  snapshot. Ignore stale completion/error publication after an actor-generation change.
- Strict codec validation makes an unknown version, wrong actor, non-object envelope, or non-array
  item collection envelope-fatal and invalidates only that active actor's bucket. Within a valid
  envelope, drop an invalid row when independent valid rows remain, recompute every source key,
  keep the first valid occurrence of a duplicate key, bound all collections/counts/strings,
  validate configuration/source-kind compatibility, and treat verse syntax as structural only.
- Do not semantically migrate qd-linking-workspace-v1; invalidate/ignore it so scalar and retired
  meanings cannot leak.
- Logout/access loss closes surfaces, cancels transient work, and clears memory, but preserves that
  actor's bucket.
- Actor B reads only actor B's key and never clears actor A's key.
- Storage failure keeps in-memory behavior usable and exposes a non-blocking persistence warning.
- Same-actor multi-tab behavior is explicitly last-writer-wins with no live storage-event merge.

### Acceptance Criteria

- Prepared configuration survives browser restart and same-actor logout/login.
- Restored rows are unchecked, unresolved, and contain no transient operation state.
- Actor changes never flash or expose another actor's rows.
- No storage API is called before resolved Owner identity, and a late prior-actor hydrate/save cannot
  publish into the current actor state.
- Changing any one of the three selection levels leaves the other two unchanged.
- An automatic OFF/manual empty word set keeps included ayahs.
- A new automatic row starts with word contributions ON, while re-adding it preserves a deliberate
  OFF preference.
- Removing a row reconciles checked/editor state; Undo restores the full configuration.
- No loaded Quran text/DTO, merged model, Door, step, error, or mock result is serialized.
- A mixed valid/invalid envelope retains valid independent rows, and duplicate rows retain only the
  first valid normalized source without resetting its configuration.

### Explicit Out-of-Scope Boundary

- No server repository, cross-tab reconciliation, cross-device semantics, or real security claim.
- No workspace row redesign or source API load in this phase.

## Phase 4 — Rework the Global Shell, Surface Router, Geometry, and Focus Ownership

### Objective

Prepare one stable primary Linking shell for the workspace, source editors, manual-word editor, and
shared operation flow without a first-open focus vacuum or nested vertical scroll.

### Dependencies

Phase 3.

### Exact Frontend Files / Areas

Modify:

- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.scss
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workspace.models.ts
- Frontend/quran-dashboard-ui/src/styles/_tokens.scss
- Frontend/quran-dashboard-ui/src/app/core/README.md
- Frontend/quran-dashboard-ui/src/app/shared/README.md
- Frontend/quran-dashboard-ui/src/app/features/linking/README.md

Add:

- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-focus-origin.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-focus.coordinator.ts

Reuse without framework replacement:

- Frontend/quran-dashboard-ui/src/app/app.ts global dialog/inert composition.
- Frontend/quran-dashboard-ui/src/app/shared/ui/modal-shell/
- Frontend/quran-dashboard-ui/src/app/shared/ui/modal-scroll-lock/

### State / Model Changes

- Expand the surface union to closed, workspace, source-ayah-editor, manual-word-editor, and
  linking-flow.
- Track the editor source key separately from a captured operation.
- Track a transient focus-origin token for Navbar, workspace row, inline source action, and retained
  entity-overlay source action.
- Keep remove-all confirmation state separate from the primary surface.

### UI / Component Changes

- Mount the lightweight qd-modal-shell synchronously.
- Defer heavy inner surfaces only and provide a real shell/body-ready focus target.
- Bind the shell's returnFocus behavior off and make LinkingFocusCoordinator the single owner of
  entry, inner-surface Back, outer Close, and retained-overlay origin restoration.
- Change the Linking block-size token from 80dvh to 88dvh; retain 80vw inline size and shared
  Compact 94dvh override.
- Keep flushBody=true. The shell body remains overflow:hidden.
- Require each child surface to expose exactly one body scroller and no nested list overflow.
- Preserve one title/Close owner; surface back/next actions must not duplicate outer close meaning.

### Data-Flow Changes

- Opening Linking first establishes dialog/inert state, then focuses the initial surface when its
  deferred body reports ready.
- Surface transitions capture the invoker, focus the new heading/control after render, and restore
  the connected invoker or a deterministic fallback on return.
- Entity-overlay Direct Link origin retains its history frame for Phase 8 restoration.

### Persistence Implications

- Surface, focus origin, deferred readiness, and confirm state remain memory-only.
- Opening/closing the shell never saves a workspace.

### Acceptance Criteria

- No state exists where the app is inert and no Linking dialog/focus target exists.
- First body readiness moves focus from shell Close to the intended surface entry.
- Workspace/editor/flow swaps do not strand focus in a destroyed subtree.
- The primary shell has no vertical scrollbar; each mounted surface can own only one.
- The custom 80vw by 88dvh contract is explicit; it does not claim the default 52rem/44rem caps.
- Compact remains delegated to the shared 94dvh sheet rule.

### Explicit Out-of-Scope Boundary

- No new modal framework or shared modal rewrite.
- No finished editor/workspace/flow UI.
- No remove-all alertdialog until Phase 9.

## Phase 5 — Build the Real Source-Ayah Configuration Editor

### Objective

Replace the inert V1 edit-selection behavior with a reusable in-shell editor that loads one source
completely, reconciles its ayah inclusion, supports local search/bulk actions/client paging, and
returns to the invoking workspace control.

### Dependencies

Phases 2–4.

### Exact Frontend Files / Areas

Add:

- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-source-ayah-editor/linking-source-ayah-editor.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-source-ayah-editor/linking-source-ayah-editor.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-source-ayah-editor/linking-source-ayah-editor.component.scss
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-editor.facade.ts

Modify:

- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-selection/linking-ayah-selection.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-selection/linking-ayah-selection.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-selection/linking-ayah-selection.component.scss
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/linking-workspace.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workflow.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking.labels.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/README.md

Reuse:

- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/complete-paged-source.loader.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-selection.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-card/

### State / Model Changes

- Give the editor its own transient source load, raw progress, unique universe, query, client page,
  error, and stale-generation state.
- Keep the authoritative inclusion in the prepared row.
- Store a captured configuration revision when loading begins.
- Support manual grouped/independent preference conditionally when a manual source exists; the
  branch becomes reachable after Phase 6.

### UI / Component Changes

- Make the count/control open the source editor instead of calling addOrFocus.
- Show unresolved, loading, error/retry, ready selected count, and optional stale previous count
  truthfully.
- Keep Select All, Clear All, per-ayah checkbox, and local current-source search.
- Replace the current label/article nesting with a semantic list item, sibling checkbox, and
  correctly associated activation label/control.
- Use client paging over filtered cards; search and selection still operate on the full resolved
  universe.
- Treat paging as a visible-DOM bound only. CompletePagedSourceLoader still fetches/holds the full
  source universe for this prototype; do not claim a request or memory reduction.
- Keep one editor body as the only vertical owner. Search, bulk controls, pager, and cards do not
  create another scroller.
- Move focus to the editor heading/search on entry and back to the source count control on exit.

### Data-Flow Changes

- Resolve the descriptor through the existing one-source resolver.
- Distinguish raw API-row progress from final unique verse count.
- Publish ready only after complete success.
- Reconcile the captured inclusion against the complete source universe.
- Use the reconciled snapshot in the editor.
- Conditionally write reconciled inclusion/lastResolvedCount back only if the row still exists and
  configurationRevision is unchanged.
- Ignore/cancel stale loads on source switch/close.

### Persistence Implications

- Persist only the reconciled inclusion and optional stale count hint.
- Keep loaded ayahs/text, query, client page, progress, and errors transient.

### Acceptance Criteria

- Workspace edit/count opens a visible, working editor.
- All source ayahs start selected unless persisted overrides say otherwise.
- Hidden filtered/paged ayahs retain selection.
- Select All/Clear All affect the complete universe, not only the visible page.
- A refreshed universe reconciles safely without overwriting a newer row edit.
- Partial/error loads never enable a misleading complete selection.
- No global Quran search or inner vertical scrollbar is introduced.

### Explicit Out-of-Scope Boundary

- No manual Mushaf page assembly or word editor yet.
- No source-set merge or Door flow changes.
- No virtualization or automated tests.

## Phase 6 — Add the Manual Mushaf Source Read Boundary and Word Editor

### Objective

Create a complete, source-backed manual Mushaf ayah source and a scalable editor for optional word
contributions. This phase deliberately uses existing study/page reads and the shared reader cache;
it does not call per-word analysis or invent canonical word IDs.

The manual source becomes constructible through frontend state in this phase. The reader entry
point named تحديد is added later in Phase 10, after the workspace and common operation flow are
ready to receive it.

### Dependencies

- Phase 2 final manual descriptor/configuration and identity contracts.
- Phase 3 V2 codec/repository support for manual configuration.
- Phase 4 editor surface and focus routing.
- Phase 5 source-ayah editor and complete-inclusion semantics.

### Exact Frontend Files / Areas

Add:

- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/manual-mushaf-ayah.reader.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/manual-mushaf-ayah-completeness.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/resolvers/manual-mushaf-ayahs-linking-source.resolver.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-manual-word-editor.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-manual-word-editor/linking-manual-word-editor.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-manual-word-editor/linking-manual-word-editor.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-manual-word-editor/linking-manual-word-editor.component.scss

Modify:

- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-ayah.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking.labels.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-key.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-source-ayah-editor/linking-source-ayah-editor.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-source-ayah-editor/linking-source-ayah-editor.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/README.md

Reuse without changing their public read contracts:

- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-manual-mushaf.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/manual-link-shape.ts
- Frontend/quran-dashboard-ui/src/app/features/mushaf/data-access/mushaf-ayah-study.api.ts
- Frontend/quran-dashboard-ui/src/app/features/mushaf/data-access/mushaf-pages.api.ts
- Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader-cache.ts
- Frontend/quran-dashboard-ui/src/app/core/api/generated/models/ayah-core-dto.ts
- Frontend/quran-dashboard-ui/src/app/core/api/generated/models/mushaf-page-response.ts
- Frontend/quran-dashboard-ui/src/app/core/api/generated/models/mushaf-word-dto.ts

### State / Model Changes

- Use the Phase 2 manual-mushaf-ayahs descriptor containing a deduplicated, Quran-ordered set of
  source-backed verse references and refreshable display/page hints without changing its persisted
  shape after the Phase 3 codec cutover.
- Derive the stable source key only from that normalized verseKey set. Page hints, labels, current
  inclusion, selected words, and link-shape preference do not participate in identity.
- Store manual configuration as:
  - the standard per-source ayah inclusion;
  - validated wordLocation sets keyed by verseKey;
  - a durable grouped or independent preference.
- Define effective manual link shape as single when exactly one ayah contributes; with two or more,
  use the stored preference. Never overwrite the stored preference while the effective shape is
  temporarily single.
- Default a newly prepared multi-ayah manual source to grouped. Automatic source kinds do not gain
  this property.
- Give the word-editor facade one active ayah, per-ayah load states, completed occurrence lists,
  local selected-location sets, a captured row revision, and stale-request generation.
- Treat the page hints on a persisted descriptor as hints only. Fresh complete-ayah proof remains
  authoritative.

### UI / Component Changes

- Add a manual-word action to the manual row/editor path; do not put it on automatic rows.
- Render a compact ayah chooser with a selected-word count for each included manual ayah.
- Render only the active ayah's complete word list. Do not mount all words for all selected ayahs.
- Keep exact Uthmani display text and native button semantics; every word toggle uses aria-pressed
  and an action-specific accessible name.
- Provide zero/one/many word states, clear-current-ayah, previous/next ayah, retry, and an overall
  summary.
- In the source-ayah editor, show grouped/independent native radios only for a manual source with
  two or more included ayahs. For one included ayah, state effective single without erasing the
  stored preference.
- Make the editor's qd-details body the sole vertical scroll owner. The chooser, active word area,
  and pager must not create nested overflow.
- Save one editor draft atomically; Cancel/Back discards it. This is intentionally different from
  the source-ayah editor's immediate inclusion updates and must be explicit in the UI copy.

### Data-Flow Changes

- Give the reader two explicit operations: a lightweight authoritative metadata read for descriptor
  creation, and a complete-occurrence read for word editing/resolution. Both start from AyahCoreDto
  and require its verseKey to match the request.
- The metadata operation returns validated AyahCore display/page-range hints without pretending it
  has loaded a complete word sequence.
- For the active word-editor/resolver verseKey, continue from that metadata into the complete read.
- Load every page in pageFrom through pageTo through MushafReaderCache, deduplicating in-flight page
  reads and using a small bounded concurrency rather than unbounded fan-out.
- Order candidate tokens by numeric page, line, and lineWordOrder, excluding ayah markers.
- Publish a complete manual ayah only after proving all of the following:
  - every requested page envelope matches its page;
  - all returned word verseKeys match the requested verse;
  - wordLocation is present, unique within the ayah, and belongs to the verse;
  - non-marker wordNumber values are contiguous from 1 through wordsCount;
  - the final non-marker count equals AyahCoreDto.wordsCount.
- A missing page, wrong key, duplicate location, numeric gap, count mismatch, or conflicting
  metadata is one controlled blocking read error. Never publish a partial occurrence list.
- The manual resolver returns the complete included ayahs and validates saved wordLocations against
  those occurrence lists. Invalid/stale locations block confirmation; they are not guessed,
  silently dropped, or promoted to quranWordId.
- Empty word selection contributes the ayah with no manual word highlight. Excluded ayahs retain
  their saved word configuration but contribute neither ayah nor words.
- Never use MushafWordAnalysisApi or MushafWordAnalysisLoadRunner for this path. N selected words
  must not cause N analysis requests.

### Persistence Implications

- Persist the normalized manual descriptor, inclusion, selected wordLocations, stored link-shape
  preference, and optional stale count through the Phase 3 codec.
- Do not persist study/page DTOs, Quran text, active editor ayah, occurrence lists, load state,
  authoritative page range, or derived effective shape.
- On hydration, validate structural coordinates only and mark counts/coordinates unresolved until
  source-backed refresh. A saved coordinate cannot become canonical because it passed the codec.

### Acceptance Criteria

- Equivalent manual verse sets prepared in different click order produce one source key.
- Re-preparing an existing manual source refreshes descriptor/display metadata while preserving row
  order, inclusion, saved words, stored grouping preference, and checked membership.
- A page-spanning ayah becomes one complete, numerically ordered occurrence list.
- An incomplete or conflicting ayah cannot be edited or confirmed as if complete.
- Switching active ayahs ignores stale publication and preserves successful cached reads/selections.
- Zero words means no manual highlights, not all words.
- One included ayah reports effective single; restoring a second ayah restores the saved grouped or
  independent preference.
- No fabricated quranWordId, renderPosition identity, or per-word analysis call enters the path.

### Explicit Out-of-Scope Boundary

- No Mushaf reader تحديد action or renderer-selection state yet.
- No canonical manual-word backend resolution or batch endpoint.
- No global Mushaf search, all-ayah word grid, virtualization, or test changes.

## Phase 7 — Add Atomic Source-Set Resolution, Pure Merge, and Preserved Link Intents

### Objective

Replace the V1 one-source workflow assumption with an atomic multi-source coordinator whose result
contains two sibling products:

1. MergedLinkingSelection for deduplicated review presentation.
2. Ordered, source-owned link-intent records for the eventual command boundary.

The second product must be derived from each reconciled source independently, never reconstructed
from the merged display.

### Dependencies

- Phase 2 operation, merge, provenance, and intent contracts.
- Phase 3 immutable checked-member snapshot and per-row revisions.
- Phase 5 complete automatic source configuration.
- Phase 6 complete manual source/configuration reads.

### Exact Frontend Files / Areas

Add:

- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-set.coordinator.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-merge.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-intents.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-operation-members.ts

Modify:

- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-operation.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-merge.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workflow.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-ayah.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/README.md

Reuse:

- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/complete-paged-source.loader.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-selection.ts
- All automatic one-source resolvers.
- The manual resolver from Phase 6.

### State / Model Changes

- Define an immutable operation member containing sourceKey, descriptor snapshot, selection/config
  snapshot, operation order, and captured row revision.
- Keep resolver outputs source-neutral. Attach sourceKey, display descriptor, configuration, and
  provenance only in the source-set coordinator after a resolver succeeds.
- Track per-member unresolved/loading/raw-progress/error/ready state plus one operation generation.
- Define an atomic operation result with mergedSelection and sourceIntents as required siblings.
  Neither is optional after success.
- Define a source intent record with source identity/provenance, contribution mode, and ordered
  ayah units plus source-owned per-ayah word contributions. Units are explicit nested arrays, not
  inferred later from item count or merged provenance.
- Keep manual wordLocation contribution metadata visibly typed as a prototype coordinate. Retain
  canonical quranWordId only when supplied by an existing automatic source read.

### UI / Component Changes

- Expose per-source load/progress/error states to the flow rather than collapsing them into one
  vague spinner.
- Show a source-qualified Retry action for a failed member, but restart the atomic set operation;
  never present a stale mixture of old and new member results.
- Present a controlled warning for a selected source that resolves successfully but contributes
  zero included ayahs.
- Block continuation when all selected sources contribute zero ayahs.
- Do not render final review in this phase; Phase 8 consumes the result.

### Data-Flow Changes

- Snapshot exactly the checked workspace members, in prepared-row order, or the one ephemeral
  Direct Link member. None checked never falls back to all prepared rows.
- Resolve every member through its existing one-source resolver, reconcile against that member's
  complete universe, and apply its own ayah/word configuration.
- Publish no merged or intent result unless every member finishes successfully under the same
  operation generation. Cancel or ignore all stale publications after member change, surface exit,
  actor/access change, or retry.
- Conditionally write refreshed count/reconciliation back to a prepared row only when sourceKey and
  captured revision still match. Ephemeral Direct Link members never write to the workspace.
- Merge display ayahs by verseKey and Quran numeric order. If source-backed metadata for one
  verseKey is null on one contribution and present on another, enrich from the present value; when
  two non-null ayah IDs, surah/ayah numbers, or exact display fields conflict, block the operation
  rather than choosing one silently.
- Within an ayah, union word contributions and contributor provenance:
  - first require every contributing complete-ayah sequence to agree on marker-normalized word
    count, order, and exact Uthmani display text;
  - use quranWordId as canonical identity when available and consistent;
  - align manual wordLocation only through its validated complete-ayah occurrence mapping;
  - use a validated verse-scoped occurrence slot only for presentation deduplication;
  - never merge by text, array index, renderPosition, wordNumber, or lineWordOrder alone.
- Preserve all contributing source keys/match types on the merged occurrence so overlap can be
  explained without duplicating Quran text.
- Derive intents independently per source after its own reconciliation:
  - every automatic ayah becomes a singleton unit;
  - a one-ayah manual source becomes one singleton unit;
  - an independent manual source becomes one singleton per included ayah;
  - a grouped manual source with two or more included ayahs becomes exactly one ordered unit.
- Preserve sources with overlapping ayahs as separate intent records. Never derive intent from the
  merged verse list or merged word-provenance list.
- Preserve each source's own word-contribution records even when the display unions the same
  occurrence; do not reverse-map merged contributor badges into command intent.

### Persistence Implications

- Persist no operation members, load state, resolved universes, merged selection, contributor
  provenance, or source intents.
- Only the coordinator's conditional refreshed row configuration/count may reach the repository.

### Acceptance Criteria

- Automatic A+C produces intent units [[A], [C]].
- Manual grouped A+B produces exactly [[A, B]]; manual independent A+B produces [[A], [B]].
- Manual grouped A+B plus automatic A+C renders A/B/C once but preserves the two source-intent
  records exactly as [[A, B]] and [[A], [C]].
- The same word contributed by two sources renders once with both contributors.
- Turning automatic word contribution off leaves that source's included ayahs and intents intact.
- An excluded ayah contributes neither display nor intent for that source even when another source
  contributes the same verseKey.
- One failed member yields no partial review or command-ready operation result.
- No client-fabricated group ID or canonical manual-word ID appears.

### Explicit Out-of-Scope Boundary

- No Door selection, mock command, real group persistence, or backend contract.
- No heuristic partial-result mode or best-effort merge.
- No cross-source configuration mutation and no automated tests.

## Phase 8 — Converge Direct Link and Workspace on One Door, Review, and Mock Pipeline

### Objective

Make Direct Link an ephemeral one-member entrance to the same operation pipeline used by checked
workspace sources. Preserve the existing Door picker and mock-only completion while removing the
V1 scalar/highlight assumptions.

### Dependencies

- Phase 4 eager shell, surface routing, and focus-origin ownership.
- Phase 7 atomic operation result.

### Exact Frontend Files / Areas

Modify:

- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workflow.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-command.port.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/mock-linking-command.port.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.scss
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-door-step/linking-door-step.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-door-step/linking-door-step.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-door-step/linking-door-step.component.scss
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-card/linking-ayah-card.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-card/linking-ayah-card.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-card/linking-ayah-card.component.scss
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-source-ayah-editor/linking-source-ayah-editor.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-source-ayah-editor/linking-source-ayah-editor.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-editor.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/linking-workspace.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/linking-workspace.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/quran-source-linking-actions/quran-source-linking-actions.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/quran-source-linking-actions/quran-source-linking-actions.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking.labels.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/README.md

Add only if the current Abwab store has no equivalent selector:

- Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-selectable-door-ids.ts

### State / Model Changes

- Replace scalar source/selection/highlight workflow state with an immutable operation snapshot and
  the Phase 7 result.
- Use explicit steps such as configure-source when Direct Link requires it, resolve, door, review,
  submitting, success, and error. Workspace entry begins from its checked member snapshot.
- Keep Door ID, command state, mock result, and current step transient.
- Keep reviewPage and any review-local filter transient; reset them whenever a new merged operation
  result replaces the prior generation.
- Represent the command request as the selected live Door plus both merged review data and ordered
  source intents. The adapter injects/rechecks live actor access; actor identity is not accepted
  from a component-authored payload.
- Make the command port async-ready, using the project's existing Observable convention, even
  though the V2 adapter resolves locally.
- Store exactly one terminal success state per operation generation so double submission cannot
  repeat the mock.

### UI / Component Changes

- Keep one component route for the current direct-link-workflow surface, but make its labels and
  inputs operation-aware so workspace and Direct Link do not fork the downstream UI.
- Remove the global Highlight source words step. For automatic Direct Link, put the same native
  automatic-word checkbox in its one-source configuration surface beside the ayah-editor entry,
  default it ON, and store the choice only in the ephemeral member. Workspace operations use each
  prepared row's already configured preference.
- Mark the active step with aria-current=step and move focus to the step heading after render.
- Reuse AbwabDoorPickerComponent with single=true as true radio/select-only behavior. Selecting the
  current Door again must not toggle it to null.
- Validate and render only exact currently live/selectable Door IDs from the same loaded snapshot;
  do not accept archived by-ID records because they happen to resolve.
- Review the merged ayah list once and show concise contributor/match summaries outside protected
  Quran text runs. Keep source intent visible enough to distinguish one manual group from automatic
  independent units.
- Render only a bounded client page of merged ayah cards and place the shared pager inside the sole
  workflow body scroller. Paging changes presentation only; it never slices mergedSelection,
  provenance, sourceIntents, counts, or the command payload, and it makes no network/memory claim.
- For each prepared manual intent shown in review, provide a source-qualified Edit grouping/ayahs
  action that routes to that source's ayah editor. On return, re-capture and re-resolve the
  operation before review; restore focus to that source's intent summary or the review heading
  fallback.
- Show source-qualified load/retry errors on every applicable step, including review.
- Keep the final text تم الربط بنجاح explicitly labelled as a prototype mock result.
- Use outcome-specific Back/Cancel labels; avoid several ambiguous رجوع controls in the same focus
  context.

### Data-Flow Changes

- Direct Link constructs one ephemeral operation member with that source's default all-ayah
  inclusion and automatic word contribution on. It can open the same source-ayah editor before
  resolution.
- Adapt the source editor to accept either a prepared-row target or an explicit ephemeral
  configuration draft. Prepared-row edits retain Phase 5's revision-guarded store writes; Direct
  Link returns the reconciled draft to the workflow and never assumes a workspace row exists.
- Direct Link does not call prepare/add and never persists unless the user separately invokes the
  explicit Add to workspace action.
- Workspace captures only its checked members. Changes made to rows after the snapshot require a
  new operation rather than mutating the in-flight result.
- Entering a manual source editor from review invalidates the captured result immediately. Preserve
  the chosen Door only if it remains in the live selectable set, then resolve a fresh checked-member
  snapshot; never patch old merged review or intents in place.
- Both entrances call the same source-set coordinator, receive the same result shape, select a
  Door, and call the same mock command adapter.
- Mock success performs no HTTP write, cache invalidation, durable entity/group ID creation, audit
  append, or server readback.
- On success, clear only the operation draft/checked membership after the terminal result is
  acknowledged. Preserve prepared workspace rows and their configurations.
- On Direct Link dismissal, restore the retained entity-detail overlay when applicable and then
  focus the regenerated source action; use containing opener then Navbar trigger as fallbacks.

### Persistence Implications

- Direct Link is entirely ephemeral.
- Door, step, operation snapshot, merged review, intents, errors, and result are never persisted.
- A completed workspace operation may clear transient checked keys but does not rewrite or remove
  prepared rows.

### Acceptance Criteria

- Direct Link and an equivalent one-source workspace operation produce the same resolved review,
  source intent, Door validation, and mock command request.
- Direct Link automatic word ON/OFF changes only its ephemeral word contribution and can match the
  same configured workspace row without reviving the scalar highlight step.
- Direct Link does not change workspace count or localStorage.
- A grouped manual source remains grouped through Door, review, and mock command.
- Review paging bounds mounted ayah cards while confirmation still carries the full merged display
  and every source intent.
- Returning from a manual review intent to its editor and back produces a newly resolved review and
  returns focus deterministically.
- The Door picker behaves as one required live radio choice and rejects archived/stale IDs.
- Errors remain visible and recoverable at review; success cannot be reconfirmed.
- Closing/back returns focus to the correct Navbar, inline Words, or retained-overlay origin.
- No real write or backend contract is implied.

### Explicit Out-of-Scope Boundary

- No server-issued request/link/group ID, persistence, audit, approval, or retry queue.
- No redesign of the Abwab tree or picker.
- No workspace dense-row/removal polish yet and no automated tests.

## Phase 9 — Replace V1 Cards with the Dense Multi-Source Workspace

### Objective

Make prepared sources, operation membership, ayah inclusion, and word behavior visibly orthogonal
in a dense qdResultList workspace while preserving one scroll owner and restrained Golden UI.

### Dependencies

- Phase 3 durable rows, checked keys, and undo state.
- Phase 5 source-ayah editor.
- Phase 6 manual word/group configuration.
- Phase 8 common operation flow.

### Exact Frontend Files / Areas

Add:

- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workspace-view.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-source-row/linking-workspace-source-row.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-source-row/linking-workspace-source-row.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-source-row/linking-workspace-source-row.component.scss

Modify:

- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/linking-workspace.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/linking-workspace.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/linking-workspace.component.scss
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workspace.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking.labels.ts
- Frontend/quran-dashboard-ui/src/app/shared/ui/details-workspace/details-workspace.component.ts
- Frontend/quran-dashboard-ui/src/app/shared/ui/details-workspace/details-workspace.component.html
- Frontend/quran-dashboard-ui/src/styles/_components.scss
- Frontend/quran-dashboard-ui/src/app/shared/README.md
- Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md
- Frontend/quran-dashboard-ui/src/app/features/linking/README.md

Delete after all callers migrate:

- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-item/linking-workspace-item.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-item/linking-workspace-item.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-item/linking-workspace-item.component.scss

Reuse:

- qdResultList and qdResultItem directives.
- qd-details-workspace shell/body/status structure.
- qd-confirm-dialog as a sibling alertdialog.

### State / Model Changes

- Expose prepared rows in stable insertion order and checked state from the native transient set.
- Model count status distinctly as unresolved, stale hint, loading raw progress, error/retry, or
  ready unique count.
- Keep one latest removal snapshot containing the complete row/configuration, original index,
  prior checked membership, active actor, and invoking control type.
- A later durable mutation, actor change, successful bulk clear, or Linking exit may clear Undo.
  Do not expire it on a short timer while keyboard/screen-reader users are locating the action.
- Re-adding an equivalent descriptor updates display metadata and focuses the row only when the
  workspace is already the active origin. External explorers receive status feedback without the
  workspace stealing focus.
- Retire generic addOrFocus, scalar activeSourceKey, and per-row Direct Link after all call sites use
  explicit prepare, open editor, check, remove, and start-operation commands.

### UI / Component Changes

- Render one semantic list item per source through qdResultList/qdResultItem. The native checkbox is
  the membership truth; selected tint/thread is only reinforcement. Label the role=list from the
  real workspace/source-list heading rather than a fabricated column-header relationship.
- Put the selected reinforcement thread on logical inline-start so RTL placement follows the
  document direction without CSS/DOM reordering.
- Keep rows as flat Golden UI siblings with no nested cards, elevation, shadow, gradient, or
  hover-only controls; render static kind/scope metadata as text or a non-interactive badge, not a
  selectable chip.
- Wide row order is source identity, ayah count/editor, word behavior, remove, membership. Medium
  groups identity plus an action band. Compact stacks logically while preserving DOM reading order.
- Treat a column guide as Wide-only visual alignment, not a fake table header or replacement for
  list semantics.
- Use the typed source formatter and canonical Words vocabulary:
  - lemma: الصيغة المعجمية;
  - stem: الأصل الصرفي;
  - retain exact unique mode and Word Type scope/group discriminators.
- Keep descriptor.label as a validated human display snapshot; never use it as identity or omit
  the source-kind discriminator.
- Top actions show selected count, primary ربط المحدد, conditional clear-selection, and a separated
  danger إزالة الجميع. None checked disables linking and never means all rows.
- Automatic rows use an automatic-word checkbox; manual rows use the word editor/count and the
  source editor's grouping state.
- Keep remove and membership controls separated. Use minimum 44px targets on Wide/Medium and 48px
  on Compact.
- Use the qd-details body as the only workspace scroller. The list, row, pager, status, and action
  band do not gain overflow/max-height.
- Add a neutral qdDetailsStatusActions projection beside—not inside—the role=status text so live
  removal feedback and the ordinary تراجع button remain semantically separate.
- Single remove is immediate. Focus same control on next row, otherwise previous row, otherwise the
  empty-workspace heading/action. Undo focuses the restored row editor/count.
- Render remove-all qd-confirm-dialog after the primary Linking shell. While open, make the lower
  shell inert and aria-hidden, disable its focus trap/Close/Escape/backdrop dismissal, and leave the
  alertdialog as the only actionable dialog.
- Remove-all confirmation names the row count, uses danger tone, keeps shared cancel-first focus,
  returns Cancel to إزالة الجميع, and focuses the empty heading after confirmation.

### Data-Flow Changes

- Row check/uncheck mutates only operation membership.
- Ayah editor changes mutate only that source's inclusion/configuration revision.
- Automatic word toggle or manual word editor mutates only that source's word configuration.
- Linking selected snapshots exactly the checked set and hands it to Phase 8.
- Remove persists the remaining ordered rows immediately. Undo restores the complete snapshot at
  its former index for the same active actor and re-persists it.
- Remove-all writes one empty snapshot for the active actor; it never calls localStorage.clear and
  never deletes another actor's bucket.

### Persistence Implications

- Row/configuration additions, edits, removal, Undo, and remove-all go through the serialized
  repository controller.
- Checked membership, count load state, Undo snapshot, confirmation state, and focus target remain
  transient.
- Navbar count remains durable prepared-row count, not current checked count.

### Acceptance Criteria

- Checking a row changes neither ayah inclusion nor word behavior, and either configuration can
  change without checking the row.
- Re-adding an equivalent source does not duplicate, reorder, or reset it.
- Every descriptor discriminator is visible enough to distinguish otherwise equal Arabic labels.
- Unresolved/stale/loading/error/ready count language remains truthful.
- Remove, Undo, and remove-all preserve correct state, actor boundary, live announcement, and focus.
- Exactly one focus trap and actionable dialog remain during remove-all confirmation.
- Wide, Medium, and Compact layouts have coherent RTL/DOM order and no nested vertical or
  horizontal list scrolling.

### Explicit Out-of-Scope Boundary

- No new design system or replacement list/modal primitive.
- No same-actor multi-tab merge, timed Undo requirement, virtualization, or automated tests.
- No automatic opening of the workspace from an external Add action.

## Phase 10 — Add Owner-Only Mushaf Ayah Selection Mode and Workspace Handoff

### Objective

Replace the removed selected-word Linking entry with an Owner-only تحديد mode that selects one or
many ayahs across Mushaf pages and prepares one manual Mushaf source without changing normal study
behavior or protected Quran layout.

This is the safe functional cutover: do not release a build containing Phase 1's removal without
this replacement and the V2 codec/flow phases. Phase sequencing describes implementation work, not
independently deployable releases.

### Dependencies

- Phase 3 prepared-row persistence/upsert semantics.
- Phase 4 focus/surface ownership.
- Phase 6 complete manual source read boundary.
- Phase 8 common operation flow.
- Phase 9 final dense workspace behavior.

### Exact Frontend Files / Areas

Add:

- Frontend/quran-dashboard-ui/src/app/features/linking/state/manual-mushaf-selection.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/mushaf-selection-status/mushaf-selection-status.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/mushaf-selection-status/mushaf-selection-status.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/mushaf-selection-status/mushaf-selection-status.component.scss

Modify:

- Frontend/quran-dashboard-ui/src/app/features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.ts
- Frontend/quran-dashboard-ui/src/app/features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.html
- Frontend/quran-dashboard-ui/src/app/features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.scss
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-header-navigation/mushaf-header-navigation.component.ts
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-header-navigation/mushaf-header-navigation.component.html
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-header-navigation/mushaf-header-navigation.component.scss
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-page-area/mushaf-page-area.component.ts
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-page-area/mushaf-page-area.component.html
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-page-view/mushaf-page-view.component.ts
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-page-view/mushaf-page-view.component.html
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-line/mushaf-line.component.ts
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-line/mushaf-line.component.html
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-word/mushaf-word.component.ts
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-word/mushaf-word.component.html
- Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-word/mushaf-word.component.scss
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking.labels.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/README.md
- Frontend/quran-dashboard-ui/src/app/features/mushaf/README.md

### State / Model Changes

- Scope a Linking-owned manual selection draft to the reader page lifecycle: active flag,
  Quran-ordered selected verse references, current page context, per-verse metadata load state,
  operation generation, and polite status.
- Keep the draft transient across Mushaf page navigation, but reset it on reader route destruction,
  explicit Cancel/Add completion, actor/access loss, or failed live Owner gate.
- Do not add URL/session fields or alter MushafReaderFacade, reader cache identity, current study
  selection, or selected-word analysis state.
- Repeat the Owner gate on activate, toggle, clear, cancel, and add-to-workspace; component
  visibility alone is insufficient.
- Feed neutral ayahSelectionMode and selectedVerseKeys through page-area, page-view, and line. Do not
  inject Linking services or descriptors below the reader-page boundary.

### UI / Component Changes

- Project an Owner-only تحديد button into the existing header action group with aria-pressed.
  Update the header's accessible group label to describe navigation plus Mushaf actions.
- Keep the persistent count/instruction/clear/cancel/add status bar at reader-page level outside
  MushafPageArea so loading, error, or empty page states cannot hide the way out of active mode.
- If page loading unmounts a focused header action, move focus to the persistent mode owner; after a
  successful page mount, restore to a meaningful page/header target only when the user's focus has
  not moved elsewhere.
- Disable Add to workspace until metadata for every selected verse has resolved completely and
  successfully; name the blocking ayah on error and offer retry/removal.
- Give each non-marker word in mode a select/deselect-ayah accessible name containing verseKey and
  aria-pressed. Outside mode, omit those attributes and preserve the existing word-study name.
- Use a neutral no-metric text-color cue for selected mounted ayahs. Add no wrapper, ayah-wide wash,
  thread, padding, margin, glyph node, font, or line-layout change.
- Suppress the ordinary single focused/studied-ayah text cue while selection mode is active without
  mutating MushafReaderFacade; restore it on exit so it cannot masquerade as another draft choice.
- Preserve the already selected study word's ring/background as visually authoritative; suppress
  the draft cue on that word if the states would conflict.
- Markers remain disabled/non-selectable and receive no false selection semantics.
- Keep previous/next/surah/page controls working in mode. Gate only document ArrowLeft/ArrowRight
  word-study navigation while the selection mode is active.

### Data-Flow Changes

- Preserve MushafWordComponent's normal synchronous ayahSelect then wordSelect emission order.
- At the reader-page handlers, when mode is active, toggle the ayah on ayahSelect and ignore the
  immediately following wordSelect; do not exit mode between those events.
- Outside mode, forward both handlers exactly as V1 does so normal study, URL, and session behavior
  remain unchanged.
- Resolve every selected ayah's descriptor metadata through Phase 6's lightweight metadata
  operation and require a matching AyahCoreDto. Record pageFrom/pageTo as refreshable hints; do not
  eagerly assemble every selected ayah's words during the reader draft.
- The current page token slice is never proof of a complete word sequence because an ayah can span
  pageFrom through pageTo. Phase 6 loads and proves the full sequence lazily in the manual word
  editor and during operation resolution.
- Add to workspace upserts one manual descriptor/configuration. A new multi-ayah row defaults to
  grouped and operation-unchecked. An existing equivalent row keeps grouping, inclusion, words,
  order, and checked state while refreshing display/page hints.
- Do not open the workspace or steal Mushaf focus after Add. Announce added/already prepared and
  exit mode only after successful handoff; explicit Cancel also exits.

### Persistence Implications

- The active reader draft and its metadata loads are never persisted.
- Only the successfully prepared manual workspace row goes through the actor-bound repository.
- No Mushaf reader URL/session/cache identity changes and no V1 workspace migration occur.

### Acceptance Criteria

- Non-Owners and unresolved users see no trigger and cannot mutate the draft through direct method
  calls.
- Selecting on page A, navigating to page B, and returning preserves count and mounted emphasis.
- One click in mode toggles one ayah and never opens word study or mutates its URL/session state.
- Header unmounting during page load/error does not hide Cancel/Clear/Add or strand focus.
- Add produces one unchecked manual row without opening the workspace; duplicate Add preserves its
  configuration.
- Cancel/Add completion restores ordinary word clicks and keyboard study navigation.
- Selected-word study remains complete except for the retired Linking action.
- Existing Quran glyph text, DOM text runs, order, ligatures, marker behavior, font, spacing, and
  measured line/page geometry are unchanged.
- The added header action fits Compact/Medium/Wide navigation without clipping, overlapping, or
  invalidating the measured page-area header reservation.

### Explicit Out-of-Scope Boundary

- No selection of individual words directly on the Mushaf page; manual words are configured in the
  workspace editor.
- No renderer redesign, ayah containers, URL-persisted draft, or automatic workspace opening.
- No backend/API/schema/auth changes and no automated tests.

## Phase 11 — Harden Words Integrations, Accessibility, Responsive Behavior, and Current Truth

### Objective

Finish every existing source entrance against the V2 contracts, remove transitional V1 paths, keep
facades/components within current responsibility thresholds, and make the frontend READMEs state
the delivered truth before final manual verification.

### Dependencies

Phases 1–10 complete as one V2 prototype delivery.

### Exact Frontend Files / Areas

Modify as required by the final descriptor/handoff signatures:

- Frontend/quran-dashboard-ui/src/app/features/linking/components/quran-source-linking-actions/quran-source-linking-actions.component.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/components/quran-source-linking-actions/quran-source-linking-actions.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/components/quran-source-linking-actions/quran-source-linking-actions.component.scss
- Frontend/quran-dashboard-ui/src/app/features/words/components/word-drilldown-modal/word-drilldown-modal.component.ts
- Frontend/quran-dashboard-ui/src/app/features/words/components/word-drilldown-modal/word-drilldown-modal.component.html
- Frontend/quran-dashboard-ui/src/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.ts
- Frontend/quran-dashboard-ui/src/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.html
- Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.ts
- Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html
- Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.ts
- Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.html
- Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/unique-detail-overlay-adapter.component.ts
- Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/unique-detail-overlay-adapter.component.html
- Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/root-detail-overlay-adapter.component.ts
- Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/root-detail-overlay-adapter.component.html
- Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/lemma-detail-overlay-adapter.component.ts
- Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/lemma-detail-overlay-adapter.component.html
- Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/stem-detail-overlay-adapter.component.ts
- Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/stem-detail-overlay-adapter.component.html
- Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/word-type-detail-overlay-adapter.component.ts
- Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/word-type-detail-overlay-adapter.component.html
- Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts
- Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.html
- Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-detail-panel.view-model.ts
- Frontend/quran-dashboard-ui/src/app/core/layout/top-navbar/top-navbar.component.ts
- Frontend/quran-dashboard-ui/src/app/core/layout/top-navbar/top-navbar.component.html
- Frontend/quran-dashboard-ui/src/app/features/linking/README.md
- Frontend/quran-dashboard-ui/src/app/features/mushaf/README.md
- Frontend/quran-dashboard-ui/src/app/features/words/README.md
- Frontend/quran-dashboard-ui/src/app/core/README.md
- Frontend/quran-dashboard-ui/src/app/shared/README.md

Read-only terminology authority unless a current-truth defect is separately proven:

- Frontend/quran-dashboard-ui/src/app/features/words/models/words-shared.labels.ts

Retire after all V2 callers migrate:

- V1 scalar source/highlight fields and step branches from linking-workflow models/facade.
- Any compatibility adapter that reconstructs intent from merged selectedVerseKeys.
- addAndOpenDirectLink, addOrFocus, raw-label feedback IDs, and obsolete direct/workspace branches.
- All reads/writes of qd-linking-workspace-v1.

### State / Model Changes

- Ensure every automatic entrance emits the exact final discriminated descriptor: unique mode,
  Root/Lemma/Stem type code or identity, and Word Type scope/group kind where applicable.
- Import/reuse the canonical Words terminology instead of maintaining contradictory Linking labels.
- Keep source-action status IDs stable and generated independently of raw Arabic labels, or omit an
  ID where no aria relationship consumes it.
- Split workflow/source-set/persistence/focus responsibilities into the delegates introduced above
  if the final facade or store would exceed the repository's current file-responsibility guidance.
- Delete transitional fields once no caller references them; do not keep dual V1/V2 truth.

### UI / Component Changes

- Give source actions at least a 44px target on Wide/Medium and 48px on Compact without changing the
  surrounding Words hierarchy.
- Use source-qualified names/descriptions for repeated Add/Direct actions and their status feedback.
- Keep the Golden UI light-scope visual language. Verify dark mode is functional but do not create
  a new dark palette or broaden the phase into a global restyle.
- At 1080px and above, retain the restrained 80vw/88dvh Wide workspace and readable content
  measure. At 768–1079px use the Medium action-band layout. Below 768px use the shared Compact sheet
  and logical stacking.
- Use logical CSS properties for RTL. Do not visually reorder controls in a way that contradicts
  DOM/read order.
- Keep all contributor/group explanations outside Quran text runs.

### Data-Flow Changes

- Route every Words Add action to prepare-only behavior and every Direct action to the common
  ephemeral operation path.
- Capture retained entity-overlay history before Direct Link hides it. Restore overlay state first,
  then focus the regenerated source action or documented fallback.
- Revalidate Owner access at every integration mutation and mock confirmation.
- Verify no remaining automatic resolver imports source-set, workspace, persistence, or workflow
  state; it remains a one-source read boundary.
- Verify no backend/API/generated-client changes are present in the implementation diff.

### Persistence Implications

- Old persisted label snapshots may be replaced by refreshed validated display snapshots, but must
  never change source identity.
- V2 remains the only workspace key/codec. V1 state is ignored, not reinterpreted.
- Integration focus/status/origin state remains transient.

### Acceptance Criteria

- Every supported source family has working Add and Direct actions with complete discriminators.
- Lemma and Stem labels match canonical Words terminology everywhere.
- Source actions meet target size, unique-name, status, Owner-gate, and focus-return contracts.
- No V1 selected-word, scalar workflow, generic active-source, or sessionStorage branch remains.
- Automatic resolvers remain neutral and the operation coordinator remains the only set-aware
  boundary.
- READMEs accurately describe V2 ownership, persistence, scroll/focus, manual-source identity, and
  mock-only status.
- The final implementation diff contains no Backend, generated API, schema, migration, test, or
  unrelated visual-system changes.

### Explicit Out-of-Scope Boundary

- No unrelated Words/Mushaf refactor, dark-theme redesign, or global component rewrite.
- No formal engineering review, deployment, commit, push, or PR unless separately requested.
- No real link implementation or automated tests.

## Cumulative V2 Acceptance Criteria

The prototype is implementation-complete only when all of the following are true:

### Authority and Lifetime

- Only an authenticated active Owner can see or invoke Linking; every mutator and mock command fails
  closed when access is unresolved/lost.
- Prepared rows/configuration survive browser close/reopen for the same actor.
- Logout/access loss closes/cancels Linking and clears memory without deleting the actor's bucket.
- A different actor cannot read, overwrite, Undo, or clear another actor's workspace.
- Restored rows are unchecked and unresolved; transient operation/editor/Door/result state never
  returns.

### Orthogonal Workspace State

- Prepared membership, operation check state, ayah inclusion, and word behavior can each change
  without rewriting the others.
- Workspace count means prepared rows; selected count means checked operation members.
- Re-preparing a stable source refreshes display metadata without duplication or configuration
  loss.
- Automatic word OFF and manual zero-word selection both retain included ayahs.

### Merge and Intent Integrity

- Review contains one numeric Quran-ordered card per verseKey and one presentation occurrence per
  validated word identity/slot, with contributor provenance unioned.
- Automatic sources always preserve singleton per-ayah intent.
- Only the manual Mushaf source can group; a multi-ayah manual source defaults grouped but can be
  explicitly independent, and its stored preference survives a temporary one-ayah inclusion.
- Manual grouped A+B plus automatic A+C reviews A/B/C once while command intent remains manual
  [[A, B]] plus automatic [[A], [C]].
- No failure publishes partial review/intent, and no merged display is reverse-engineered into
  intent.

### Quran Identity and Display Safety

- verseKey is source-backed and numeric-orderable; quranWordId is canonical only when supplied by
  an existing read.
- wordLocation remains a temporary manual occurrence coordinate and is validated against a complete
  ayah before use.
- renderPosition, index, wordNumber, lineWordOrder, and text are never durable/canonical identity.
- Exact Quran text remains unchanged; contributor/status content stays outside protected Quran text
  runs.
- Mushaf selection changes no glyph, font, ligature, marker, word order, spacing, line metrics, or
  normal study behavior.

### Workflow, Door, and Mock Result

- Checked workspace sources and ephemeral Direct Link sources use one source-set/Door/review/mock
  pipeline.
- Direct Link never auto-persists or changes workspace count.
- Door selection is required, select-only, and limited to exact currently live/selectable IDs.
- The mock command carries both merged display and ordered source intents, rechecks access, makes no
  write, and reaches one terminal تم الربط بنجاح result.

### UI, Focus, Scroll, and Responsive Behavior

- The primary dialog is mounted before app inertness; deferred inner surfaces have explicit focus
  handoff.
- Workspace, each editor, and flow each have exactly one vertical scroll owner.
- The primary shell remains stable near 80vw/88dvh on Wide/Medium and uses the shared 94dvh Compact
  sheet behavior.
- Remove/Undo/remove-all and all surface/origin transitions have deterministic focus destinations.
- The nested remove-all alertdialog is the only exception; while active, the lower shell is inert,
  hidden from accessibility, non-dismissible, and not trapping focus.
- Dense rows remain usable in RTL, light/dark functionality, and Compact/Medium/Wide without
  horizontal overflow or color-only meaning.

### Scope Integrity

- Selected-word Linking is gone while selected-word study remains intact.
- No Backend/generated API/schema/migration/permission/deployment change exists.
- No real write, durable group/link/request ID, history, approval, audit, or server workspace is
  implied.
- No automated tests were added, deleted, renamed, or modified.

## Final Manual / Browser Verification Matrix

Run this matrix only after all implementation phases and static gates are complete. Record the
browser, viewport, actor, source descriptors, expected/actual result, focus destination, scrollbar
owner, and any pre-existing visual mismatch for each applicable row.

| Area | Required scenarios and evidence |
| --- | --- |
| Static gates and scope | Run check:no-unit-specs, typecheck:app, check:golden-ui, and build:verify; inspect the final diff for frontend/README allowlist only and zero test/backend/generated-client changes. |
| Owner/access | Owner sees Navbar, Words, and Mushaf entry points; non-Owner and unresolved identity see none; direct method attempts fail closed; access loss during editor/load/review/mock closes and clears transient state without deleting the bucket. |
| Persistence/reopen | Restart same Owner; logout/login same Owner; switch actor A to B to A; envelope-fatal malformed active bucket; mixed valid/invalid rows; duplicate key keeps first valid row; denied/quota storage; V1 session key present; rows restore in order unchecked/unresolved; document same-actor multi-tab last-successful-write-wins. |
| Three orthogonal states | Independently change workspace membership, ayah inclusion, and automatic/manual word behavior; verify no action silently changes either other dimension. |
| Multi-source overlap | Resolve two overlapping automatic sources; deduplicate display ayahs/words, union provenance, preserve singleton intents, exercise new-row ON default and retained OFF preference, exclusion overlap, one zero-contributing member, all-zero block, one member failure, retry, stale generation, and a multi-page merged review whose pager never slices intents/confirmation. |
| Manual grouping | New A+B defaults grouped; switch independent; drop to A and restore B without losing preference; manual grouped A+B plus lemma A+C displays A/B/C once while intent remains [[A, B]] plus [[A], [C]]; edit grouping from review and verify fresh resolution/focus return. |
| Manual words | Zero/one/many selected locations; rapid active-ayah switch; exclude/re-include; cache reuse; real page-spanning ayah where pageFrom differs from pageTo; missing/wrong/duplicate/gapped/count-mismatch data blocks confirmation. |
| Direct versus workspace | Run the same one-source descriptor/config through Direct and checked workspace; compare resolved review and intent; Direct never changes prepared count/storage; Back/Close restores inline or retained-overlay origin. |
| Door | Current live root/child choices; current choice cannot toggle off; archived/stale/by-ID-only value rejected; snapshot refresh invalidates a no-longer-live choice visibly. |
| Dense workspace | Empty, one row, many rows, long Arabic/tashkeel, every source discriminator, unresolved/stale/loading/error/ready counts, check/clear, duplicate refresh, remove first/middle/last, checked removal, Undo, clear-all Cancel/Confirm. |
| Mushaf mode | Owner/non-Owner; select/deselect one/many ayahs; page A to B and back; page loading/error while active; retry/remove unresolved ayah; Add duplicate; Cancel; normal word click and ArrowLeft/Right before/after mode; selected-study-word visual precedence. |
| Modal/focus/inert | First open with deferred body; Navbar Wide/Compact origin; every workspace/editor/flow transition and Back; source removed before return; entity overlay restoration; alertdialog cancel-first focus; lower shell Close/Escape/backdrop suppressed; expected return focus after cancel/confirm. |
| Scroll/overlap | Short and long content in Workspace, source editor, manual editor, and flow; inspect exactly one vertical scrollbar and no horizontal/nested list/chooser/card scroller; verify shell/footer/status/action overlap does not occur. |
| Responsive/RTL | Compact at 767px, Medium at 768px and 1079px, Wide at 1080px plus one larger viewport; confirm 80vw/88dvh Wide/Medium, shared 94dvh Compact, logical row order/thread, danger/checkbox separation, 44/48px targets, no clipping, and Mushaf header navigation/selection action fitting its reserved geometry. |
| Theme | Complete functional paths in light and dark; treat Golden UI as light-scope, record rather than silently redesign any pre-existing dark mismatch, and verify focus/status/selection remains perceivable without color alone. |
| Accessibility | Keyboard-only completion; visible focus; unique source-qualified names; native list/listitem, checkbox/radio/button semantics; aria-current step; aria-pressed Mushaf/manual words; polite status and adjacent Undo; no Quran-text injection. |
| Quran identity safety | Inspect operation/mock payload and restored storage: exact verseKeys, canonical quranWordIds only when supplied, manual wordLocations labelled coordinates, no persisted presentation slot/renderPosition/index/wordNumber/lineWordOrder/text identity, exact Uthmani display retained. |
| Mock-only network safety | Observe network/cache behavior through confirmation: reads only, no Linking write, no cache invalidation, no request/link/group ID, no history/audit append, and one terminal mock result. |

## Implementation Completion Gate

Do not call V2 complete merely because the shell or dense workspace renders. Completion requires:

1. every phase acceptance criterion above;
2. all existing static gates passing without test changes;
3. the full manual/browser matrix recorded against representative real source overlaps and at least
   one source-backed page-spanning ayah;
4. current-truth READMEs updated in the same implementation change;
5. a final scope inspection proving no backend, generated API, schema, migration, permission,
   automated-test, deployment, or real-write work entered the diff.

If any required behavior depends on a backend contract that does not exist—especially canonical
manual-word resolution or durable grouped-link identity—stop at the truthful frontend prototype
boundary and report it. Do not invent the contract client-side.
