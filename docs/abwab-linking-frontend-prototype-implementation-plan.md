# Abwab Linking Frontend Prototype Implementation Plan

## Objective

Deliver a fully interactive, frontend-only Quran Linking prototype inside the existing Angular
application. An active System Owner can prepare Quran-derived sources in a global workspace, open
one Direct Link workflow, select a live Abwab Door, load and refine the complete source ayah set,
review source-word highlighting, and receive the mock result `تم الربط بنجاح`. The prototype locks
the UX and frontend contracts without creating or implying any durable link, request, approval, or
backend write.

## Locked Scope

- The feature is visible and actionable only when `CurrentUserStore` has resolved an authenticated,
  active user whose `isOwner` value is `true`. Unknown, loading, error, signed-out, inactive, and
  non-Owner states fail closed.
- Do not add a linking permission, reuse an Abwab permission, add an Admin switch, or implement an
  Admin submission/review branch.
- Reuse existing read APIs only. Do not add an HTTP mutation, backend endpoint, generated write DTO,
  database change, migration, cache invalidation, or cache mutation.
- The only terminal mock result is `تم الربط بنجاح`. It has no Link ID, Request ID, Draft ID, audit
  ID, approval status, or durable history.
- Source-derived ayahs are independent selected verse keys. Do not infer a grouped link from a
  multi-ayah source result.
- Grouped linking is reserved for a future explicit Mushaf selection workflow. This prototype adds
  no `mushaf-group` descriptor, grouped-selection UI, or grouped execution path.
- Search is client-side filtering over the current fully resolved source result only. Do not add a
  global Quran/Mushaf search.
- Source highlighting starts on. Unique Word and Word Type use canonical match IDs; Root, Lemma,
  and Stem use their accurate `isMatched` flags for presentation. Array positions are never treated
  or persisted as Quran-word identity.
- Temporary workspace persistence is limited to actor-bound, versioned `sessionStorage`. Loaded
  ayah DTOs/text, modal state, workflow step, selected Door, load/error state, and mock result stay
  in memory.
- Do not add, edit, delete, or rename automated tests. In particular, do not touch `*.spec.ts` or
  add a Playwright journey.
- Keep changes inside the Angular frontend. Do not modify the backend or unrelated features.

## Architectural Direction

All new domain behavior belongs under
`Frontend/quran-dashboard-ui/src/app/features/linking/`. The feature uses a root-scoped Signals
workspace store for cross-route state and a single root-mounted host whose workspace and Direct
Link contents are deferred until opened. `TopNavbarComponent` reads only Owner visibility, item
count, and open commands. Source pages/detail adapters construct serializable descriptors and
render one shared `QuranSourceLinkingActionsComponent`; they do not own workflow state or make
linking reads.

The Direct Link flow is one `qd-modal-shell` with `variant="wide"`. Its facade resolves a descriptor
through a source resolver registry, uses the existing source read APIs, aggregates every page before
selection is enabled, and maps wire data into a Linking-owned neutral ayah model. It reuses
`AbwabSnapshotFacade` and `AbwabDoorPickerComponent` for a real live Door, uses an adaptive
`all-except`/`only` selection model keyed by `verseKey`, and executes through a replaceable
frontend-only mock command port.

```text
Source detail -> shared Linking actions -> workspace store or Direct Link facade
Navbar        -> lightweight workspace signals -> root Linking host
Root host     -> one deferred Wide modal surface
Direct Link   -> resolver registry -> existing read APIs -> neutral Linking ayahs
              -> AbwabSnapshotFacade + AbwabDoorPickerComponent
              -> mock command port -> تم الربط بنجاح
```

The root host and resolver-heavy workflow must remain out of the eager Navbar path. The host should
follow the existing entity-detail overlay's `@defer` composition pattern. The host must render only
one Linking dialog at a time: the workspace surface or Direct Link, never nested shells.

## Testing Decision

**Automated tests: none.** The repository Test Freeze is controlling, the prototype adds no approved
security or critical business invariant test exception, and frontend unit specs are prohibited.
No implementation phase may change the frozen test estate.

Every implementation phase runs the normal frontend gates as independent commands, in this order:

```bash
cd Frontend/quran-dashboard-ui
npm run check:no-unit-specs
npm run typecheck:app
npm run build:verify
```

Any phase that changes templates or styles also runs `npm run check:golden-ui` before
`npm run build:verify`. Browser checks named below are targeted manual verification to perform
during later implementation; Chrome/browser tooling is not part of writing this plan.

## Phase 1 — Define the Linking contracts

**Goal**

Create the feature-owned serializable source, neutral ayah, workspace-selection, workflow, and copy
contracts on which every later slice depends.

**Why now**

Descriptors and neutral result models are the stable boundary between independently owned source
features and the global workflow. Defining only those contracts first prevents source DTOs or
callbacks from leaking into workspace persistence.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-source.models.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-ayah.models.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workspace.models.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workflow.models.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/models/linking.labels.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-key.ts`
- Read-only contract references: `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types-detail.models.ts`,
  `Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts`

**Implementation**

- Define a discriminated `LinkingSourceDescriptor` union for `mushaf-word`, `unique-word`, `root`,
  `lemma`, `stem`, and `word-type`.
- Preserve every result-defining field: Unique mode; Lemma/Stem `typeCode`; Mushaf
  `quranWordId`, `wordLocation`, `verseKey`, and `pageNumber`; and the complete Word Type word or
  grouped selection plus its scope. Do not include the current list page, list sort, or list search
  in result identity.
- Keep Word Type's persisted shape Linking-owned and structurally serializable; source adapters map
  from the existing `WordTypeDetailSelection` rather than persisting feature services or view state.
- Define `LinkingAyah` keyed by `verseKey`, with nullable `ayahId`, `surahNumber`, and
  `surahNameArabic` where current reads omit them. Define words with `renderPosition`, nullable
  `canonicalQuranWordId`, exact `textUthmani`, `isAyahMarker`, and `isSourceMatch`.
- Define the adaptive selection union: `{ mode: 'all-except'; verseKeys }` and
  `{ mode: 'only'; verseKeys }`.
- Define the Direct Link step/result discriminants and centralize Arabic feature labels, including
  the sole success text.
- Derive a deterministic workspace key from the complete result-defining descriptor. Use explicit
  field ordering; do not depend on incidental object-property order.

**Explicit non-goals**

- No store, component, API read, storage access, route, grouped descriptor, or backend-shaped write
  request.
- No speculative Draft/Request/approval models and no Admin outcome.

**Verification**

- Run the normal three-command frontend gate set.
- Review the union exhaustiveness and confirm that no descriptor contains an Observable, callback,
  facade, DTO graph, modal state, or fake identity.

## Phase 2 — Establish fail-closed Owner access

**Goal**

Create one Linking-owned access signal that expresses the exact active-System-Owner rule.

**Why now**

The store, Navbar, source actions, and confirmation all need one authoritative visibility/action
decision before they expose behavior.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-access.service.ts`
- Existing authority, read only: `Frontend/quran-dashboard-ui/src/app/core/auth/current-user.store.ts`

**Implementation**

- Inject `CurrentUserStore` and expose a read-only computed signal that is true only when
  `authStateKnown`, `isAuthenticated`, `isActive`, and `isOwner` are all true.
- Keep the rule feature-owned and reusable by the Navbar, action component, stores, and final
  confirmation recheck.
- Expose no role-name parsing and no generic authorization framework.

**Explicit non-goals**

- No new permission code, generated catalogue edit, route guard, Admin behavior, token parsing, or
  reuse of an Abwab write permission.

**Verification**

- Run the normal three-command frontend gate set.
- Code-review the computed rule against `CurrentUserStore`; every unresolved or non-Owner branch
  must evaluate false.

## Phase 3 — Build the root Signals workspace state

**Goal**

Implement the in-memory prepared-source collection and its compact selection/highlight state.

**Why now**

The root host, Navbar count, workspace cards, persistence, and source actions all need one state
owner before UI wiring begins.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-selection.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workspace.models.ts`

**Implementation**

- Make `LinkingWorkspaceStore` `providedIn: 'root'` and expose readonly Signals for ordered items,
  item count, active surface, and whether Linking is open.
- Support idempotent add/focus by derived source key, explicit remove, workspace open/close, edit
  selection, Direct Link intent for exactly one source, result-count refresh, and highlight toggle.
- Initialize every Quran-derived source with `highlightSourceWords: true` and `all-except` with an
  empty exclusion set.
- Keep `resultCount` unknown until the descriptor has been resolved once; never display an invented
  zero as a resolved source count. Persist the last successfully resolved count later.
- Implement pure selection operations keyed only by `verseKey`: toggle one, Select All, Clear All,
  intersect stale overrides with a new complete universe, expand to explicit selected keys, and
  calculate the global selected count.
- Preserve prepared sources after mock success. Only explicit remove deletes one.
- Gate mutating/open commands through the Owner access signal so hidden UI is not the only boundary.

**Explicit non-goals**

- No API orchestration, loaded ayah storage, Door assignment, batch execution, mock history, or
  persistence yet.

**Verification**

- Run the normal three-command frontend gate set.
- Review pure selection transitions for empty, Select All, Clear All, individual toggles, and a
  changed verse-key universe; do not add automated tests.

## Phase 4 — Persist an actor-bound workspace session

**Goal**

Restore only the safe, lightweight workspace subset across a same-tab refresh.

**Why now**

Persistence should wrap the settled workspace model before UI components begin depending on
hydration timing or restoration behavior.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace-session.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts`
- Convention reference: `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader-session.ts`

**Implementation**

- Use one namespaced key such as `qd-linking-workspace-v1` and an envelope with `version: 1`, the
  current user's `sub`, and ordered prepared items.
- Store only descriptor, derived key, adaptive selection override, last resolved result count, and
  highlight preference. Re-derive selected count instead of persisting it independently.
- Guard browser access, JSON parse, schema validation, ID ranges, descriptor discriminants,
  Word Type scopes, source-key recomputation, and `sessionStorage` quota/failure paths.
- Wait for resolved current-user state before hydration. Restore only on an exact actor `sub`
  match; clear storage and in-memory items on logout or actor change so no cross-user flash or
  restoration is possible.
- Reject unknown versions and structurally corrupt envelopes fail closed. Deduplicate source keys
  and verse-key overrides while preserving the first valid workspace order.
- Re-resolve a restored source only when the workspace item is opened/edited or Direct Link starts.
  Reconcile stored selection against the newly completed verse-key universe and surface a calm
  notice when the source result changed.
- Keep in-memory behavior functional if storage is unavailable.

**Explicit non-goals**

- No `localStorage`, URL serialization, generic persistence framework, loaded Quran DTO/text,
  selected Door, open modal, workflow step, loading/error state, or mock result in storage.

**Verification**

- Run the normal three-command frontend gate set.
- Manually inspect the serialized schema in code and confirm every forbidden transient or Quran
  payload field is absent.

## Phase 5 — Mount one deferred global Linking host

**Goal**

Add the single application-level composition point that owns Linking dialogs and global inertness.

**Why now**

Navbar and source actions need a stable global target, and dialog layering must be correct before
feature surfaces are populated.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.html`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.scss`
- New minimal shell: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/linking-workspace.component.ts`
- New minimal shell: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/linking-workspace.component.html`
- New minimal shell: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/linking-workspace.component.scss`
- Update: `Frontend/quran-dashboard-ui/src/app/app.ts`
- Existing convention: `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/entity-detail-overlay-host.component.html`

**Implementation**

- Mount `LinkingWorkspaceHostComponent` once as a sibling of `qd-app-shell` and
  `qd-entity-detail-overlay-host`.
- Keep the host light. Load the workspace/direct workflow subtree with `@defer` only when a Linking
  surface opens, following the entity-overlay adapter pattern.
- Use `qd-modal-shell variant="wide"`; do not create new modal geometry, focus trapping, backdrop,
  Escape, or scroll-lock behavior.
- Extend app-root inert/`aria-hidden` composition so the app shell is inert while either the entity
  overlay or Linking is open, and an entity overlay is inert during any short transition in which
  Linking becomes the foreground surface.
- Ensure the host selects one domain surface at a time. Opening Direct Link from workspace replaces
  workspace content/shell state; it never opens a dialog inside the workspace dialog.
- Keep Compact behavior delegated to the existing modal shell sheet contract.

**Explicit non-goals**

- No route, drawer, fifth modal variant, source resolver, Door picker, or finished workspace UI.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify` in the prescribed
  order.
- During later browser verification, open/close the empty host at Wide and Compact and verify one
  focus trap, correct focus return, body scroll lock, and inert background content.

## Phase 6 — Add the Owner-only Navbar workspace trigger

**Goal**

Expose the global workspace through the existing Wide/Compact Navbar action seam.

**Why now**

The root host and workspace signals now exist, so the Navbar can remain a thin trigger instead of
becoming a state owner.

**Files / areas**

- Update: `Frontend/quran-dashboard-ui/src/app/core/layout/top-navbar/top-navbar.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/core/layout/top-navbar/top-navbar.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/core/layout/top-navbar/top-navbar.component.scss`
- Read-only state owners: `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-access.service.ts`,
  `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts`

**Implementation**

- Add one workspace button inside the existing `chromeActions` template so the same implementation
  renders in Wide chrome and the Compact navigation sheet.
- Hide it unless the Linking access signal is true. Keep it enabled when visible even with zero
  items.
- Empty state: neutral/dim icon treatment and no count. Nonempty state: existing active-state visual
  language plus a plain functional numeric count; do not make the count a reward badge.
- Bind `aria-label="مساحة الربط"` when empty and include the item count in the Arabic label when
  nonempty. Use the current inline SVG/current-color icon convention rather than a new icon package.
- Dispatch only workspace open. The Navbar may read item count and active state but must not own the
  item array, persistence, workflow, or resolver imports.
- On Compact, close `sheetOpen` first and open Linking on the next microtask/render turn so the
  navigation modal releases focus/scroll lock before the Linking modal opens.

**Explicit non-goals**

- No `NAV_MENU` item, route, disabled-empty button, Admin state, workspace data ownership, or theme
  redesign.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Later browser check: non-Owner hidden; active Owner visible; empty button remains operable; count
  appears only when nonempty; Compact sheet closes before the Linking dialog opens.

## Phase 7 — Complete the workspace surface and cards

**Goal**

Render multiple prepared sources and the approved per-item operations in the global workspace.

**Why now**

The host and Navbar provide access to the store; the workspace can now establish the UI contract
that later source actions and resolvers populate.

**Files / areas**

- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/linking-workspace.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/linking-workspace.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/linking-workspace.component.scss`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-item/linking-workspace-item.component.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-item/linking-workspace-item.component.html`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-item/linking-workspace-item.component.scss`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/models/linking.labels.ts`
- Reuse: `Frontend/quran-dashboard-ui/src/app/shared/ui/details-workspace/`, `action/`,
  `empty-state/`, `notice/`, `skeleton/`

**Implementation**

- Compose the Wide modal with `qd-details-workspace` where its header/body/footer anatomy fits and
  use the existing action, async-state, notice, spacing, typography, token, and RTL contracts.
- Render ordered cards with source-kind label, display label, last resolved result count, global
  selected count, highlight state, Remove, Edit selection, and Direct Link.
- Show an explicit unresolved/loading state instead of inventing a count before first resolution.
- Make Edit/Direct target one source. Keep removal explicit and ensure a mock success does not
  remove or mark a source linked.
- Provide a calm empty state. Preserve the active/count styling discipline from the Navbar and do
  not introduce statuses such as Draft, Submitted, Approved, or Linked.
- Keep card components presentational; commands go to the workspace/workflow owners.

**Explicit non-goals**

- No batch selection/linking, Door stored on cards, status columns, history, HTTP read/write, or
  source-specific branching in templates.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Later browser check: empty state, multiple-card order, count/highlight display, remove, and a card
  remaining unchanged after returning from a mock result.

## Phase 8 — Create the Direct Link state machine and single-shell flow

**Goal**

Own one active source and the sequential Direct Link workflow without nesting dialogs.

**Why now**

Workspace actions need a workflow owner before Door selection, resolver loading, or confirmation
can be added independently.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.html`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.scss`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/linking-workspace-host.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workflow.models.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/models/linking.labels.ts`
- Existing overlay coordinator: `Frontend/quran-dashboard-ui/src/app/core/navigation/detail-overlay/detail-overlay-history.service.ts`

**Implementation**

- Implement one state machine with `door`, `ayahs`, `highlight`, `review`, and `result` steps. Source
  identity is header/summary context, not a separate step.
- Support an ephemeral source invoked directly and a workspace-backed source whose saved
  selection/highlight state is reconciled after resolution.
- Keep Door, search query, loaded ayahs, progress/error, current step, and result transient. Reset
  them on final dismissal without deleting workspace items.
- Define guarded Back/Next/Confirm transitions; later phases fill their prerequisites. Steps are a
  presentational progress indicator, not freely activatable `qd-tabs`.
- Recheck Owner access before start and on every transition that could expose or execute the flow.
- Centralize global-layer handoff: if Direct Link starts from an open entity-detail overlay, retain
  its URL stack, call the existing overlay close behavior, and open Linking only after that shell is
  closed. Do not alter overlay URL codecs/history.
- Return from a workspace-started flow to the workspace surface; dismiss a source-started flow back
  to the source page. Never have both Linking shells open.

**Explicit non-goals**

- No resolver implementation, Door data, selection list, confirmation execution, free step jumping,
  nested confirmation dialog, or route state.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Later browser check: guarded step order, Back/close behavior, workspace return, and entity overlay
  closing before Direct Link opens with its retained restore state intact.

## Phase 9 — Reuse the live Abwab Door picker

**Goal**

Make the first workflow step select and validate one real live Door from existing read state.

**Why now**

The state machine now provides the exact lifecycle for transient Door selection without pulling in
source-resolution concerns.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-door-step/linking-door-step.component.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-door-step/linking-door-step.component.html`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-door-step/linking-door-step.component.scss`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.scss`
- Reuse unchanged where possible: `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-snapshot.facade.ts`
- Reuse unchanged where possible: `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-picker/abwab-door-picker.component.ts`

**Implementation**

- Ask `AbwabSnapshotFacade` to load on entering the Door step and bind its loading, error, empty,
  retry, and `snapshot.liveRoots` state to `AbwabDoorPickerComponent`.
- Configure the picker with `[single]="true"` and one `pickedId`; derive the selected Door/section
  labels from the current `snapshot.byId` rather than persisting display copies.
- Keep archived Doors out through `liveRoots`. If the selected ID is missing or no longer live after
  refresh, clear it, show a controlled notice, and remain/return on the Door step.
- Require one live Door before Next and revalidate against the current snapshot immediately before
  mock confirmation.

**Explicit non-goals**

- No hardcoded/fake Doors, `AbwabMovePickerComponent`, Abwab authoring state, tree mutation, archive
  behavior, permission reuse, or write call.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Later browser check: live-tree loading/retry/empty states, single selection, search/expansion, and
  stale selected Door returning the workflow to a valid state.

## Phase 10 — Add shared source actions through Unique Word

**Goal**

Introduce the two shared source actions at the first real source consumer, covering both Unique Word
modes through their genuinely shared path.

**Why now**

The workspace and Direct Link entry points exist. Unique Word already provides stable source IDs,
labels, both modes in one stack, and canonical match data, making it the smallest truthful first
source adapter.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/quran-source-linking-actions/quran-source-linking-actions.component.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/quran-source-linking-actions/quran-source-linking-actions.component.html`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/quran-source-linking-actions/quran-source-linking-actions.component.scss`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-drilldown-modal/word-drilldown-modal.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-drilldown-modal/word-drilldown-modal.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/unique-detail-overlay-adapter.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/unique-detail-overlay-adapter.component.html`

**Implementation**

- Make the common component accept one descriptor and render `إضافة للربط` and `ربط مباشر` only
  when the Owner access signal is true and the descriptor is complete.
- Keep the component thin: Add dispatches the descriptor to the workspace; Direct dispatches it to
  the workflow. It makes no source API call and owns no workflow step.
- Make duplicate Add focus the existing item and surface calm, accessible feedback rather than
  duplicating it.
- In `WordDrilldownModalComponent`, derive `{ kind: 'unique-word', mode, wordId, label }` only from a
  resolved `state.summary`/`selectedWordId` and place the shared actions in the existing
  `qdDetailsActions` zone.
- For the frameless global overlay, render the same shared action contribution at the top of
  `UniqueDetailOverlayAdapterComponent` after its summary resolves. Direct Link uses the facade's
  close-then-open handoff; do not alter overlay navigation history.
- Use the same implementation for `simple` and `tashkeel` while retaining mode in the descriptor
  key. Do not collapse their ID namespaces.

**Explicit non-goals**

- No resolver/read yet, row-level table actions, copied workflow logic, overlay URL changes, or
  other source families.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Later browser check both Unique modes in the page detail and global overlay: Owner-only actions,
  idempotent Add, workspace count change, and overlay-to-Direct handoff.

## Phase 11 — Resolve every Unique Word ayah page

**Goal**

Create the resolver registry and prove deterministic complete traversal with the first source.

**Why now**

Unique actions are the first consumers that can start Direct Link, so the generic resolver and
paging foundation is introduced at the moment it is needed, not earlier.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/complete-paged-source.loader.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/resolvers/unique-word-linking-source.resolver.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts`
- Existing read only: `Frontend/quran-dashboard-ui/src/app/features/words/data-access/unique-words.api.ts`
- Existing DTOs: `Frontend/quran-dashboard-ui/src/app/features/words/models/unique-words.models.ts`

**Implementation**

- Define the resolver interface keyed by descriptor `kind` and a registry with an explicit
  not-yet-supported result for descriptor kinds whose later resolver phase has not landed. Register
  Unique Word here; each later source phase replaces its bounded unsupported entry, and Phase 19
  leaves the final switch exhaustive. Keep the registry/workflow inside the deferred Direct Link
  dependency path so importing lightweight Navbar signals does not eagerly pull every source API
  into app chrome.
- Implement a focused paged loader that requests page 1, reads the server `totalCount`/page size,
  traverses all remaining pages in deterministic page order, exposes loaded/total progress, and
  supports cancellation when the active source changes or the dialog closes.
- Treat a failed HTTP call, failed `ApiResponse`, missing data, inconsistent page metadata, or
  incomplete traversal as a retryable whole-source error. Never publish a partial `allAyahs` array
  or enable confirmation from it.
- Deduplicate by `verseKey` while preserving first source order. Identical repeats may collapse;
  conflicting data for the same verse key must fail resolution rather than be silently chosen.
- Map Unique words from raw `UniqueWordAyahMatchDto`: retain exact Uthmani text and canonical
  `quranWordId`, and set `isSourceMatch` from `matchedQuranWordIds`.
- On successful complete resolution, reconcile a workspace item's saved selection with the new
  universe and update its result count. Keep loaded ayahs in workflow memory only.
- Reuse `UniqueWordsApi.getAyahMatches`; do not modify its cache, current drilldown pagination, or
  API contract.

**Explicit non-goals**

- No new backend read endpoint, parallel page flood, partial-success confirmation, source-facade
  mutation, or other source resolver.

**Verification**

- Run the normal three-command frontend gate set.
- Code-review that every response page is required before success and that no resolved Quran DTO is
  written to session storage.
- Later browser check a multi-page Unique source: visible progress, complete final count, retry on a
  failed page, and no selection screen on partial failure.

## Phase 12 — Implement complete-set selection and local Arabic search

**Goal**

Make the resolved source ayahs selectable and searchable without corrupting hidden selections.

**Why now**

The first resolver supplies a real complete universe, so the neutral selection contract can now be
implemented and verified against live source results.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-selection/linking-ayah-selection.component.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-selection/linking-ayah-selection.component.html`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-selection/linking-ayah-selection.component.scss`
- Move/promote: `Frontend/quran-dashboard-ui/src/app/features/mushaf/utils/arabic-search-normalize.ts`
  to `Frontend/quran-dashboard-ui/src/app/shared/quran/arabic-search-normalize.ts`
- Update imports: `Frontend/quran-dashboard-ui/src/app/features/mushaf/utils/study-source-catalog.groups.ts`,
  `Frontend/quran-dashboard-ui/src/app/features/mushaf/utils/surah-jump-catalog.helpers.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-selection.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.scss`

**Implementation**

- Promote the existing pure Arabic normalization helper because Linking becomes its second feature
  consumer; update current Mushaf consumers without changing normalization behavior.
- Derive `allAyahs`, transient `searchQuery`, filtered `visibleAyahs`, explicit selected verse keys,
  and global selected count. Search over concatenated exact Uthmani word text plus `verseKey` and
  available Arabic surah name.
- Compare normalized copies only; render the exact returned Uthmani text and never write normalized
  text into a result or workspace item.
- Render a checkbox for each visible ayah, keyed by `verseKey`. Filtering changes visibility only;
  it never changes selection.
- Apply Select All and Clear All to the complete `allAyahs` universe, not the filtered subset.
  Display the global selected count even while a filter hides selected rows.
- Start with all results selected, require at least one selected ayah before Next/Confirm, and keep
  the compact adaptive representation in workspace state.
- Use `qd-form-field`/`qdControl`, the native `.qd-checkbox` pattern, `qdAyahCard`, and existing
  loading/error/empty/notice owners.

**Explicit non-goals**

- No server search, global Quran search, URL search state, filtered-only bulk action, automatic
  selection changes, or persistence of result text.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Later browser check: all selected initially; deselect; filter diacritic-insensitively; hidden
  selection survives; selected count remains global; Select/Clear affect the full source; zero
  selected blocks progression.

## Phase 13 — Render truthful source highlighting

**Goal**

Present exact ayah text with source matches highlighted by the neutral contract and default the
review preference on.

**Why now**

Selection already renders neutral ayahs; adding a Linking-owned presentation adapter now avoids
forcing incompatible identity assumptions into existing Words/Mushaf renderers.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-card/linking-ayah-card.component.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-card/linking-ayah-card.component.html`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-card/linking-ayah-card.component.scss`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-selection/linking-ayah-selection.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-selection/linking-ayah-selection.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-selection/linking-ayah-selection.component.scss`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.scss`
- Presentation references, do not repurpose identity: `Frontend/quran-dashboard-ui/src/app/features/words/components/highlighted-ayah/`,
  `Frontend/quran-dashboard-ui/src/app/shared/ui/ayah-card/`

**Implementation**

- Build a Linking-owned ayah renderer around `qdAyahCard` that consumes `isSourceMatch` directly
  and tracks display tokens by render position without naming that value `quranWordId`.
- Use canonical Quran-word IDs only where a resolver actually provides them. Never synthesize an
  ID for Root/Lemma/Stem or persist a render position.
- Preserve exact word order, exact Uthmani text, ayah markers, existing Quran font/token rules,
  Arabic line height, and RTL semantics. Keep styles scoped outside protected Mushaf renderers.
- Initialize highlight on for every source. The toggle changes presentation and the later mock
  command option only; it does not change selected ayahs.
- Show the same highlighted neutral rendering in ayah selection and review, with a clear off state
  when the Owner disables highlighting.

**Explicit non-goals**

- No modification to `qd-mushaf-word`, Quran glyph mapping, Quran text, existing lossy Root/Lemma/
  Stem mappers, or array-index identity. No animation of Quran text.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Later browser check Unique canonical matches, default-on toggle behavior, exact Uthmani rendering,
  and selection stability while highlighting is toggled.

## Phase 14 — Execute through the mock command port

**Goal**

Complete review, confirmation, and the one approved presentation-only success result.

**Why now**

Door, complete source results, selection, and highlighting are now available, so the mock boundary
can validate the exact approved command without inventing backend behavior.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-command.port.ts`
- New: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/mock-linking-command.port.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workflow.models.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.scss`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/models/linking.labels.ts`
- Reuse: `Frontend/quran-dashboard-ui/src/app/shared/ui/notice/notice.component.ts`

**Implementation**

- Define a replaceable injection boundary whose command contains only source descriptor, live target
  Door ID, explicit selected verse keys, and `highlightSourceWords`.
- Implement the prototype adapter with no `HttpClient`. Before resolving, recheck active Owner
  access, current live Door membership, complete-source load success, and at least one selected
  verse key.
- Return only `{ kind: 'linked', message: 'تم الربط بنجاح' }` and keep it in the active workflow's
  presentation state.
- Render a confirmation summary of source, Door, global selected/total counts, independent ayah-link
  meaning, and highlight preference. Confirm is the primary action only on this step.
- Show the visible result and an accessible notice/live-region announcement. Closing it clears the
  transient result but leaves a workspace-backed prepared source unchanged.
- Ensure no API cache invalidation or mutation occurs on mock success.

**Explicit non-goals**

- No HTTP request, fake latency requirement, retry queue, IDs, durable success status, Admin review
  outcome, approval/request copy, or optimistic cache change.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Code-review the mock adapter imports to confirm there is no `HttpClient` or source-cache write.
- Later browser check review copy, last-moment Owner/Door/selection validation, exact success text,
  accessible announcement, and prepared source retention.

## Phase 15 — Integrate the selected Mushaf word occurrence

**Goal**

Support one selected, analyzed Mushaf word occurrence as a one-ayah source without changing protected
reader rendering.

**Why now**

The complete generic workflow is proven by Unique Word; Mushaf can now add its distinct one-result
adapter without influencing paged-source or grouped-link semantics.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/resolvers/mushaf-word-linking-source.resolver.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.html`
- Update only if layout needs it: `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.scss`
- Existing reads: `Frontend/quran-dashboard-ui/src/app/features/mushaf/data-access/mushaf-pages.api.ts`,
  `Frontend/quran-dashboard-ui/src/app/features/mushaf/data-access/mushaf-word-analysis.api.ts`

**Implementation**

- After `WordAnalysisViewModel` succeeds, construct a `mushaf-word` descriptor from
  `analysis.word.quranWordId`, `wordLocation`, `verseKey`, `pageNumber`, and exact selected word
  label, then render the shared actions in the selected-word section header/action area.
- Resolve by reading the descriptor's existing Mushaf page and reconstructing only the matching
  `verseKey` from its ordered page words. Mark the selected `wordLocation` as the source match.
- Preserve the selected occurrence's canonical `quranWordId`; leave sibling canonical IDs null if
  the page read does not expose them. Never infer them from position.
- Treat a missing page, verse, word location, or analysis identity as a retryable resolution error,
  not fabricated content.
- Keep this descriptor strictly one selected occurrence/one ayah. All-occurrence linking remains the
  Unique Word source and grouped Mushaf selection remains deferred.

**Explicit non-goals**

- No action on `qd-mushaf-word`, line/page renderer edit, multiple selection, grouped link, new
  Mushaf endpoint, or Quran rendering/style change.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Later browser check: actions appear only after valid analysis, exactly one ayah resolves and is
  selected, only the chosen occurrence highlights, and page navigation/reader selection remain
  unchanged.

## Phase 16 — Integrate Root sources

**Goal**

Support a selected Root through the generic details action seam and complete paged resolution.

**Why now**

Root is the first `isMatched` source and establishes the shared Words details-shell projection that
Lemma, Stem, and Word Type will reuse.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/resolvers/root-linking-source.resolver.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/components/details-panel-shell/details-panel-shell.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/components/root-details-panel/root-details-panel.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/components/root-details-panel/root-details-panel.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/root-detail-overlay-adapter.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/root-detail-overlay-adapter.component.html`
- Existing read: `Frontend/quran-dashboard-ui/src/app/features/words/data-access/roots.api.ts`

**Implementation**

- Add a domain-neutral projected action slot to `QdDetailsPanelShellComponent` beside its existing
  Close action; the shell must not import Linking or know source types.
- Construct `{ kind: 'root', rootId, label }` from the selected root ID and resolved summary in both
  explorer and overlay adapters, and pass it to the Root panel/shared action contribution.
- Keep actions off table rows and hidden while selection/summary is absent or invalid.
- Aggregate every `RootsApi.getRootAyahMatches` page with the shared loader.
- Map each returned word's exact text and `isMatched` flag to the neutral shape. Set
  `canonicalQuranWordId: null`; do not call or reuse `root-ayah-match.mapper.ts` for identity because
  it substitutes indexes.
- Preserve complete-set error/progress/deduplication semantics from Unique.

**Explicit non-goals**

- No changes to root list/table behavior, detail pagination state, existing mapper identity,
  backend DTOs, or canonical-ID invention.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Later browser check page and overlay actions, complete Root result count, accurate `isMatched`
  highlighting, and absence of persisted/commanded array indexes.

## Phase 17 — Integrate Lemma sources

**Goal**

Support a selected Lemma while preserving the current ayah `typeCode` scope.

**Why now**

Lemma reuses the Root projection and `isMatched` mapper pattern but adds one result-defining filter,
so it merits a separate bounded slice.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/resolvers/lemma-linking-source.resolver.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-details-panel/lemma-details-panel.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/lemma-detail-overlay-adapter.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/lemma-detail-overlay-adapter.component.html`
- Existing read: `Frontend/quran-dashboard-ui/src/app/features/words/data-access/lemmas.api.ts`

**Implementation**

- Construct `{ kind: 'lemma', lemmaId, typeCode, label }` from the current selected Lemma and its
  current ayah-type scope. Include `typeCode` in the workspace key so filtered and unfiltered
  descriptors remain distinct.
- Ensure overlay descriptors use `frame.typeCode`; page descriptors use the same current
  `panelState().ayahTypeCode` semantics. Do not silently drop the filter.
- Aggregate all `LemmasApi.getLemmaAyahMatches(id, page, pageSize, typeCode)` pages.
- Map returned words from exact text and `isMatched`; canonical IDs remain null and no segment match
  identity is claimed.
- Reuse the shared action component, complete-page loader, neutral rendering, and stored selection
  reconciliation without adding workflow code to the Lemma page/facade.

**Explicit non-goals**

- No Lemma facade ownership of Linking, segment-ID invention, type filter redesign, table-row
  actions, or backend read change.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Later browser check filtered versus unfiltered descriptors remain distinct, refresh restores the
  exact scope, and visible matches follow `isMatched` without fake IDs.

## Phase 18 — Integrate Stem sources

**Goal**

Support a selected Stem through the established details, paging, filter, and `isMatched` contracts.

**Why now**

Stem mirrors the proven Lemma shape but remains an independent resolver/source integration, keeping
the phase small and reviewable.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/resolvers/stem-linking-source.resolver.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/components/stem-details-panel/stem-details-panel.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/stem-detail-overlay-adapter.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/stem-detail-overlay-adapter.component.html`
- Existing read: `Frontend/quran-dashboard-ui/src/app/features/words/data-access/stems.api.ts`

**Implementation**

- Construct `{ kind: 'stem', stemId, typeCode, label }` from the selected Stem and exact current
  ayah-type scope in explorer and overlay paths.
- Include `typeCode` in the stable key and pass it to every page of
  `StemsApi.getStemAyahMatches`.
- Aggregate all pages and map exact word text plus `isMatched` to the neutral model, leaving
  canonical IDs null.
- Reuse all common actions, state, selection, search, highlighting, Door, and mock execution
  behavior. Keep Stem source code limited to descriptor construction and read mapping.

**Explicit non-goals**

- No combined Lemma/Stem abstraction beyond already identical shared infrastructure, no source
  facade mutation, fake identity, or backend change.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Later browser check Stem page and overlay entry, exact `typeCode` restoration, complete results,
  and truthful `isMatched` highlighting.

## Phase 19 — Integrate Word Type word and grouped-dimension sources

**Goal**

Support the existing selected Word Type word and Root/Stem/Lemma grouped-detail scopes using raw
canonical match data.

**Why now**

Word Type has the most complex descriptor and a metadata gap; implementing it last lets it reuse
all stable generic behavior while keeping its raw-response mapping isolated.

**Files / areas**

- New: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/resolvers/word-type-linking-source.resolver.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.html`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/word-type-detail-overlay-adapter.component.ts`
- Update: `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/adapters/word-type-detail-overlay-adapter.component.html`
- Existing reads/models: `Frontend/quran-dashboard-ui/src/app/features/words/data-access/word-types.api.ts`,
  `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types-detail.models.ts`
- Raw generated result: `Frontend/quran-dashboard-ui/src/app/core/api/generated/models/word-type-ayah-match-dto.ts`

**Implementation**

- Map the active `panelState().selection` into a descriptor that preserves its discriminant and
  complete `WordTypeDetailScope`. Expose actions only for an actual selected word or grouped
  Root/Stem/Lemma detail; a bare whole-filter/list scope has no current ayah endpoint and must not
  become a descriptor.
- For `kind: 'word'`, aggregate `WordTypesApi.getAyahMatches`; for grouped Root/Stem/Lemma, aggregate
  `getGroupedAyahMatches` with the exact existing grouped request parameters.
- In the global overlay, support its current word-kind selection only. Do not pretend grouped
  overlay frames exist.
- Map the raw `WordTypeAyahMatchDto` directly: preserve `words[].quranWordId`, calculate
  `isSourceMatch` from `matchedWordIds`, retain match positions only for display fallback if needed,
  and do not call the existing lossy shared mapper that substitutes indexes.
- Use `verseKey` as selection identity. Set unavailable `ayahId` and `surahNameArabic` to null and
  render a controlled absence rather than inventing metadata.
- Preserve deterministic complete traversal and all common workspace/workflow behavior.

**Explicit non-goals**

- No whole-filter source, new Word Type endpoint, generated DTO edit, fabricated ayah metadata,
  grouped ayah link semantics, or change to existing Words list/detail behavior.

**Verification**

- Run `check:no-unit-specs`, `typecheck:app`, `check:golden-ui`, and `build:verify`.
- Later browser check selected word plus each grouped dimension, complete result counts, canonical
  word highlighting, null metadata presentation, and absence of a Linking action for a bare list
  scope.

## Phase 20 — Finish integration consistency and focused browser verification

**Goal**

Close only proven integration gaps, align all new surfaces with current Golden UI, and verify the
approved prototype flows end to end.

**Why now**

All source slices and common behavior now exist. Cleanup is justified only where their real usage
reveals duplication, ownership drift, accessibility gaps, or deferred-boundary leakage.

**Files / areas**

- Review only files already changed under `Frontend/quran-dashboard-ui/src/app/features/linking/`
- Review the touched Navbar, app root, Words detail/adapters, Mushaf selected-word section, and
  shared Arabic normalizer
- Verification authorities: `Frontend/quran-dashboard-ui/FRONTEND_UI_RULES.md`,
  `Frontend/quran-dashboard-ui/.architecture/golden-ui/GOLDEN_VISUAL_VERIFICATION.md`,
  `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md`

**Implementation**

- Remove only duplication demonstrated across implemented resolvers/adapters; do not create a
  generic source framework beyond the registry, complete-page loader, and neutral contracts already
  in use.
- Confirm deferred boundaries: app/Navbar eagerly retain only lightweight host/state code; resolver,
  source API, Door picker, and heavy workflow UI load when Direct Link is opened.
- Review component/facade sizes against frontend thresholds and split only a file that has crossed a
  responsibility boundary. Keep components on UI coordination and facades on orchestration.
- Align Arabic copy, logical properties, focus order, visible labels, live regions, Compact/Medium/
  Wide behavior, light/dark token use, and one-primary-action discipline with existing primitives.
- Perform the targeted browser matrix below and fix only defects inside the locked prototype scope.

**Explicit non-goals**

- No redesign, opportunistic refactor, new generalized UI primitive, automated test change,
  performance initiative, backend work, or deferred feature.

**Verification**

- Run, independently and in order: `npm run check:no-unit-specs`, `npm run typecheck:app`,
  `npm run check:golden-ui`, and `npm run build:verify`.
- Targeted Owner browser flow: Navbar empty state -> add Unique source -> navigate routes -> reopen
  workspace -> refresh -> actor-safe restore -> edit selection -> Direct Link -> live Door -> full
  source load -> all selected -> deselect -> local search with hidden selection preserved ->
  highlight on/off -> review -> mock `تم الربط بنجاح` -> source remains prepared.
- Targeted access flow: signed-out, unresolved, inactive, and non-Owner users see no Navbar/source
  Linking UI; loss of Owner state during an open flow disables confirmation and fails closed.
- Targeted source flow: one representative run for Mushaf occurrence, Unique simple/tashkeel, Root,
  Lemma with `typeCode`, Stem with `typeCode`, Word Type word, and one grouped dimension. Confirm
  complete counts and the correct canonical-ID versus `isMatched` highlight path.
- Targeted responsive/global-layer flow: Wide and Compact Navbar entry, Compact sheet closes first,
  no nested dialogs, correct focus return/scroll lock/inertness, and entity overlay closes before
  Direct Link while remaining restorable.
- Inspect Network during mock confirmation: no linking write request and no API cache-invalidating
  request is emitted.

## Cumulative Acceptance

- An authenticated active System Owner sees Linking actions and the Navbar workspace icon; every
  other access state fails closed and sees no Linking UI.
- The Navbar button works when empty, shows neutral empty state, shows an active state/count when
  nonempty, behaves the same in Wide and Compact, and does not own workspace data.
- Unique Word simple/tashkeel, Root, Lemma, Stem, Word Type selected word/grouped dimension, and one
  selected Mushaf word occurrence can each produce a serializable descriptor and expose the same
  Add/Direct actions at their approved detail seams.
- Multiple sources can be added idempotently, removed, reopened for selection editing, and linked
  one at a time. No batch linking or durable status is present.
- Workspace state survives route component destruction and same-tab refresh through a versioned,
  actor-bound `sessionStorage` envelope. Logout/actor change clears it; another user never sees it.
- Stored data contains no loaded ayah text/DTO, open-modal state, workflow step, loading/error state,
  selected Door, or mock result.
- Every paged resolver deterministically loads the complete source set before selection or
  confirmation, exposes progress/error/retry state, deduplicates safely by `verseKey`, and never
  proceeds with partial data.
- Every resolved source starts with all ayahs selected. Selection is keyed by stable `verseKey`;
  Select All/Clear All operate on the complete source, and at least one selected ayah is required.
- Client-side Arabic-friendly search filters only the current source result, preserves exact
  Uthmani display text, does not change hidden selections, and leaves the displayed selected count
  global.
- Source highlighting is on by default. Unique Word and Word Type use current canonical match IDs;
  Root/Lemma/Stem use accurate `isMatched`; Mushaf marks only the selected occurrence. No array
  index is persisted or represented as a canonical QuranWord ID.
- A real currently live Door is selected through `AbwabSnapshotFacade` and
  `AbwabDoorPickerComponent`; missing/archived/stale targets block progress or return to Door
  selection.
- Confirmation expands the adaptive selection to explicit independent verse keys and calls only the
  frontend mock port. The visible and announced terminal result is exactly `تم الربط بنجاح`.
- Mock success does not remove the workspace item, create fake history/IDs/status, call a linking
  write, or mutate/invalidate an API cache.
- Source-derived results never create a grouped ayah link. No Mushaf grouped workflow or global
  Quran/Mushaf search exists.
- The app uses one root-mounted deferred Linking host, one Wide Direct Link modal, existing Compact
  shell behavior, correct inert/focus/scroll-lock interaction, and no nested incompatible dialogs.
- The new UI reuses existing Golden UI primitives, tokens, Arabic-first RTL behavior, async-state
  owners, and protected Quran presentation rules; it does not look like a separate mini-app.
- No automated test file was added or changed. `check:no-unit-specs`, Angular app typecheck,
  production build, and relevant Golden UI/static guards pass, and the targeted browser matrix
  verifies the approved interactions during implementation.

## Deferred / Future Work

- Real linking backend commands, persistence, link identities, database schema/migrations, and
  server-authoritative validation.
- A canonical linking permission for Admins propagated through the backend permission catalogue,
  `/api/access/me`, generated frontend permission codes, and the normal `CurrentUserStore.can(...)`
  path.
- Admin submission/review behavior and a server-selected Owner-direct versus Admin-review result.
- Canonical matched QuranWord/segment IDs for Root, Lemma, and Stem source reads.
- Word Type `ayahId`/Arabic surah metadata and a read endpoint for a whole grammatical filter scope
  if that scope later becomes linkable.
- Backend source-wide read-contract improvements if deterministic traversal of existing pagination
  later proves insufficient.
- Real Draft, Request, review, approval, publish, audit, conflict/idempotency, concurrency, restore,
  retry, and reconciliation workflows.
- Explicit Mushaf multi-selection and grouped linking, owned by the Mushaf reader rather than
  inferred from source-derived results or workspace batching.
- Global Quran/Mushaf search.
