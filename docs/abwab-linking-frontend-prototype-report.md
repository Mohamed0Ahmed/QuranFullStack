# Abwab Quran Linking frontend prototype architecture report

**Status:** frontend-only, code-only architectural audit
**Date:** 2026-08-11
**Delivery boundary:** report only; no implementation plan, production-code change, test change,
backend mutation, database change, runtime inspection, browser tooling, screenshot, or profiling.

## Executive conclusion

The cleanest prototype is a feature-owned, root-scoped `linking` workspace mounted once at the
application root, with a light `sessionStorage`-backed Signals store and one deferred global host.
Source pages contribute only a serializable source descriptor and invoke two common actions:
`إضافة للربط` and `ربط مباشر`. A central resolver registry loads the source's ayahs through the
existing read APIs. Neither source pages nor their existing facades should own linking workflow
state.

Both the global workspace and Direct Linking should use the existing `qd-modal-shell` with its
`wide` variant. Direct Linking should be a multi-step workflow inside one dialog, not a stack of
door, ayah, highlight, and confirmation dialogs. Compact already becomes the shell's existing
sheet. There is no general drawer primitive in the current code, and adding a full-screen route or
a fifth modal geometry would work against the existing shell contracts.

The prototype is feasible with existing reads, with four important qualifications:

1. **Admin access cannot yet be represented safely.** `CurrentUserStore.can(...)` is exactly the
   mechanism to reuse, but the generated permission union contains no linking permission. The
   Owner path can be prototyped now; an Admin path requires a canonical permission-catalogue code
   to reach `/api/access/me` and the generated frontend codes. Reusing an unrelated Abwab write
   permission or inventing an untyped string would be incorrect.
2. **Root, Lemma, and Stem can highlight accurately at word level today, but cannot provide
   canonical matched Quran-word IDs.** Their responses carry `isMatched` and Uthmani text only.
   Existing mappers assign array indexes to a field named `quranWordId`; those indexes are render
   surrogates and must never enter prototype persistence or a future linking command.
3. **Existing source ayah reads are paged.** The workflow requires one source-wide selection, so a
   resolver must aggregate all pages before the selection step, preserve one global selection set,
   and expose loading/error progress. Existing detail-page pagination state is not the workflow's
   selection state.
4. **Word Type data is strongest for matching but incomplete for ayah metadata.** Raw ayah results
   contain canonical Quran-word IDs and matched IDs, but omit `ayahId` and Arabic surah name. A
   prototype can key selection by `verseKey`; the later read contract should fill those metadata
   gaps. A whole grammatical-filter scope without a selected word/grouped row has no current ayah
   result endpoint.

The smallest useful v1 workspace prepares multiple independent sources but links only one source
at a time. It stores source identity, selection overrides, count, and the highlight preference. It
does not store a Door, a Draft, a Request, approval status, or mock history. A mock success leaves
the prepared source in the workspace until the user explicitly removes it; this avoids pretending
that a durable backend state now exists.

## Code evidence and current architectural constraints

All `src/...` paths in this report are relative to `Frontend/quran-dashboard-ui/`.

### App-wide state and global layers

- `src/app/app.ts:14-15` already demonstrates the correct one-time composition: the app shell and
  the global entity-detail overlay are siblings. The app shell becomes inert while that overlay is
  open (`app.ts:14,20`). A Linking host belongs at this same composition boundary.
- `src/app/core/layout/top-navbar/top-navbar.component.html:26-27,71,77` has one reusable
  `chromeActions` template rendered in the Wide action cluster and in the Compact navigation
  sheet. This is the best insertion point for the workspace trigger; it avoids a duplicate desktop
  and Compact implementation.
- The Navbar already reads `ScrollLockService` and becomes inert while a modal holds the shared
  lock (`top-navbar.component.ts:23,56,58-61`). The linking host should use `qd-modal-shell`, never
  manipulate body scrolling itself.
- `qd-modal-shell` owns the only dialog widths (`confirm | form | wide | overlay`), topmost focus
  trap, focus return, backdrop/Escape behavior, and a reference-counted scroll lock
  (`shared/ui/modal-shell/modal-shell.component.ts:20,26,37,68-70,137-138`). `wide` is already a
  named geometry (`modal-shell.component.scss:42`).

### Access state

- `CurrentUserStore` exposes fail-closed `isActive`, `isOwner`, `can`, and `canAny`
  (`core/auth/current-user.store.ts:37-38,80-85`). `can` already treats an active Owner as allowed
  and otherwise requires an exact direct permission.
- The permission type is generated from `ABWAB_PERMISSION_CODES`; the current generated catalogue
  has Door, Section, Relation, Template, and Template Node groups only
  (`core/auth/permission-codes.generated.ts:4-32`). There is no linking code.
- Therefore visibility should be computed from the existing access store, not from role names,
  route names, token claims, or a local boolean. Unknown/loading/error/inactive states must remain
  hidden and non-actionable.

### Existing detail and action seams

- `qd-details-workspace` already owns a projected `[qdDetailsActions]` header zone
  (`shared/ui/details-workspace/details-workspace.component.html:11-27`).
- The shared Words `qd-details-panel-shell` currently fills that zone with only Close
  (`features/words/components/details-panel-shell/details-panel-shell.component.html:18-31`). Its
  clean extension seam is generic action projection; linking logic should not be embedded in the
  shell.
- Unique Words uses the same `qd-details-workspace` action zone directly in
  `features/words/components/word-drilldown-modal/word-drilldown-modal.component.html`.
- Mushaf should integrate in the selected-word study section, after word analysis is available.
  It must not add actions to every `qd-mushaf-word` glyph/button or alter protected Quran
  rendering.

### Existing storage conventions

- The Mushaf session codec already establishes the relevant precedent: one namespaced
  `sessionStorage` key, guarded read/write, JSON parsing, and fail-closed normalization
  (`features/mushaf/state/mushaf-reader-session.ts:17,79-96,102-124`).
- Theme preference uses `localStorage` because it is durable user preference; that is the wrong
  lifecycle for a temporary linking workspace.
- URL state is authoritative for individual explorers and detail overlays, but the requested
  workspace intentionally spans routes. Putting workspace items into route query parameters would
  duplicate state and create oversized, shareable URLs for tab-local prototype data.

## Recommended architecture

```text
Source page/detail/overlay
  -> QuranSourceLinkingActions (common component)
       -> LinkingWorkspaceStore.add(descriptor)
       -> LinkingWorkflowFacade.startDirect(descriptor)

TopNavbar workspace trigger
  -> LinkingWorkspaceStore.openWorkspace()

App root
  -> AppShell
  -> EntityDetailOverlayHost
  -> LinkingWorkspaceHost (mounted once; heavy content deferred until open)
       -> LinkingWorkspace view
       -> DirectLinkWorkflow (one qd-modal-shell, variant="wide")
            -> source resolver registry -> existing read APIs
            -> AbwabSnapshotFacade -> AbwabDoorPicker
            -> source-wide ayah selection
            -> highlight review
            -> confirmation
            -> MockLinkingCommandPort
```

### Ownership

The new domain should be feature-first, for example `src/app/features/linking/`. It is not generic
UI and should not become a `shared/` dumping ground. Its workspace store may still be
`providedIn: 'root'`; root lifetime does not require moving domain behavior into `core/`.

Recommended responsibility boundaries:

| Owner | Responsibility |
|---|---|
| `LinkingWorkspaceStore` | Prepared-source collection, open surface/mode, source selection overrides, highlight preference, lightweight session persistence. |
| `LinkingWorkflowFacade` | One active Direct Link state machine, async source loading, selected Door, review/confirm/result states, permission recheck. |
| `QuranSourceLinkingActions` | Permission-aware rendering of the two actions and dispatch of a descriptor. No API calls and no workflow state. |
| Source resolver registry | Selects a resolver by descriptor kind and converts existing source reads into one neutral ayah model. |
| Per-source resolver | Existing read-API orchestration, page aggregation, source-specific mapping, and gap reporting. No UI. |
| `LinkingWorkspaceHost` | One global dialog host and the workspace/direct-flow composition. |
| `MockLinkingCommandPort` | Prototype-only command validation and discriminated mock outcome. No `HttpClient`, cache mutation, or durable history. |
| `sessionStorage` codec | Versioned serialization, structural validation, actor binding, and fail-closed restoration. |

The Navbar should read only lightweight computed state (`canUseLinking`, `itemCount`, `isOpen`) and
dispatch open/close. Importing the full workflow, Words resolvers, or Abwab picker into the Navbar
would eagerly couple all lazy features to application chrome. The global host should defer the
heavy workflow subtree until opened, following the existing global detail overlay's deferred
adapter pattern.

### Global layer interaction

`app.ts` should treat an open Linking dialog like the current entity overlay and make the app shell
inert. The modal shell already makes Navbar chrome inert through the shared scroll lock, but the
root-level `inert`/`aria-hidden` boundary remains necessary for the main route content.

Only one domain dialog should remain open:

- Opening Direct Link from a normal page opens Linking directly.
- Opening Direct Link from the entity-detail overlay should capture the descriptor, close the
  detail overlay into its existing retained state, then open Linking. The user may restore the
  detail overlay after the Linking dialog closes.
- The Navbar cannot be activated while the entity overlay has made the app shell inert, so the
  Navbar path does not create that conflict.
- On Compact, clicking the workspace action inside the navigation sheet should close that sheet
  first, then open Linking. Do not stack the Linking modal on the navigation modal.

## Answers to the report questions

### 1. Cleanest architecture for a global frontend-only Linking Workspace

A feature-owned root store plus a single root-mounted, deferred host is the cleanest architecture.
It preserves state across route component destruction, centralizes modal/focus ownership, keeps
source pages as descriptor producers, and prevents five separate explorers from growing five
workflow implementations.

The workspace is a modal surface, not a route. Its state is tab-local prototype state, not a
shareable navigation contract. The source's originating route may be stored only as optional
navigation context for “return to source”; it is not the workspace's source of truth.

### 2. Where its state should live

Use Signals in `LinkingWorkspaceStore`, `providedIn: 'root'`, under the `linking` feature. Persist a
versioned, serializable subset to `sessionStorage`. Keep loaded ayah text and API DTOs in memory
only; restore them through source resolvers after refresh.

Do not put this state in:

- `TopNavbarComponent` (destroy/presentation ownership and eager coupling);
- individual Words or Mushaf facades (route-scoped duplication);
- `AbwabSnapshotFacade` (read snapshot only);
- URL query state (cross-route workspace, potentially large selections);
- `localStorage` (too durable for a mock prototype);
- an NgRx-style global store introduced only for this feature (the app already uses focused
  Signals facades/stores).

### 3. Navbar integration

Use an icon button in the existing `chromeActions` template, not a `NAV_MENU` item. The workspace
is an action and has no route. The same template already renders in both Wide and Compact chrome.

Behavior:

- Hidden unless access is resolved and the user is active and allowed.
- Always operable when visible, including when empty; “dim” must be visual state, not `disabled`.
- `aria-label` should say “مساحة الربط” and include the count when nonzero.
- Empty: neutral icon treatment, count absent.
- Nonempty: active-state treatment plus a functional numeric count. The count is status, not a
  decorative/reward badge.
- Clicking from Compact closes the navigation sheet before opening the workspace.

The Navbar should call store commands and should not own dialog state beyond its existing
navigation sheet.

### 4. Shared Direct Linking owner

`DirectLinkWorkflowComponent`, orchestrated by `LinkingWorkflowFacade`, should own Direct Linking.
Source pages provide descriptors and never own workflow steps, selected Door state, confirmation,
or result branching.

### 5. Direct Linking surface choice

Use one `qd-modal-shell variant="wide"` with sequential internal steps:

1. Door selection.
2. Source ayah selection and client-side search.
3. Highlight review and option.
4. Confirmation summary.
5. Mock result.

The Source is already known before the dialog opens, so it is context in the header/summary rather
than a separate step.

This is a **multi-step modal**. It should not be:

- a drawer: the codebase has responsive detail-drawer compositions but no general drawer owner;
- a set of nested modals: it would create unnecessary focus/dirty/dismissal complexity;
- a full-screen route: it would turn temporary cross-route state into navigation state and remove
  the global-workspace benefit;
- a new modal width: `wide` is the existing 52rem contract and Compact already becomes a 94dvh
  sheet.

Use the shell's one body scroller and fixed footer for Back/Next/Confirm. A step indicator is
presentational workflow progress, not `qd-tabs`: sequential steps have validation and cannot be
freely activated like tab panels.

### 6. Door selection reuse

Reuse `AbwabSnapshotFacade` and `AbwabDoorPickerComponent`:

- `AbwabSnapshotFacade` is already root-scoped, loads the public tree read, caches it under an ETag,
  and exposes loading/error/empty state (`features/abwab/state/abwab-snapshot.facade.ts:11,22-32,49`).
- The built snapshot exposes `liveRoots` and `byId`; the builder explicitly partitions archived
  doors (`abwab-tree.builder.ts:71-91`).
- `AbwabDoorPickerComponent` already supports search, expansion, loading/error/empty states, and
  single-selection radio semantics (`abwab-door-picker.component.ts:29,43-56,61,80-111`). Use
  `[single]="true"`, `snapshot.liveRoots`, and one `pickedId`.

Do not reuse `AbwabMovePickerComponent`: “move under main door,” cycle exclusions, and section move
semantics are not target-Door selection. Do not reuse the management page or tree wholesale; those
carry authoring, selection, archive, URL, and permission behavior unrelated to linking.

The selected target stores the real `doorId`; its label and section label are derived from the
current snapshot. Archived doors are never offered. If a selected Door disappears or becomes
archived after refresh, the workflow clears it and returns to Door selection.

### 7. Source-page actions without duplicated linking logic

Render one common `QuranSourceLinkingActions` component at source-level action seams. Every caller
supplies one serializable `LinkingSourceDescriptor`; the component supplies the same permission
gate, labels, disabled/loading behavior, duplicate-add behavior, notice, and calls to the global
store/facade.

Recommended seams:

| Surface | Integration seam |
|---|---|
| Root/Lemma/Stem/Word Type detail | Generic projected action slot in `qd-details-panel-shell` / `qd-details-workspace`; no table-row actions. |
| Unique Word drilldown | Existing `qdDetailsActions` zone in `word-drilldown-modal`. |
| Global entity-detail overlay | The same source action contribution from the active adapter; Direct Link closes the retained detail overlay before opening Linking. |
| Mushaf selected word | `selected-word-section` header after `WordAnalysisViewModel` exists. Do not touch `qd-mushaf-word`. |

Source list/table components should remain unchanged. A row click still selects and loads the
existing detail. Linking is an action on a resolved source, not a second row-navigation system.

### 8. Common source adapter and neutral ayah shape

The serializable descriptor and runtime resolver must be separate. Never persist callbacks,
Observables, facades, or raw DTOs.

```ts
type LinkingSourceDescriptor =
  | {
      kind: 'mushaf-word';
      quranWordId: number;
      wordLocation: string;
      verseKey: string;
      pageNumber: number;
      label: string;
    }
  | { kind: 'unique-word'; mode: 'simple' | 'tashkeel'; wordId: number; label: string }
  | { kind: 'root'; rootId: number; label: string }
  | { kind: 'lemma'; lemmaId: number; typeCode: string | null; label: string }
  | { kind: 'stem'; stemId: number; typeCode: string | null; label: string }
  | {
      kind: 'word-type';
      selection: WordTypeDetailSelection;
      label: string;
      originListContext?: WordTypeOriginListContext;
    };

interface QuranLinkingSourceResolver<T extends LinkingSourceDescriptor> {
  readonly kind: T['kind'];
  loadAllAyahs(source: T): Observable<LinkingSourceResult>;
}

interface LinkingSourceResult {
  readonly source: LinkingSourceDescriptor;
  readonly ayahs: readonly LinkingAyah[];
}

interface LinkingAyah {
  readonly verseKey: string;       // selection key available everywhere
  readonly ayahId: number | null;  // null where the current read omits it
  readonly surahNumber: number | null;
  readonly surahNameArabic: string | null;
  readonly ayahNumber: number;
  readonly pageNumber: number;
  readonly words: readonly LinkingAyahWord[];
}

interface LinkingAyahWord {
  readonly renderPosition: number;
  readonly canonicalQuranWordId: number | null;
  readonly textUthmani: string;
  readonly isAyahMarker: boolean;
  readonly isSourceMatch: boolean;
}
```

`renderPosition` is deliberately not named or serialized as a Quran-word ID. Root/Lemma/Stem can
populate `isSourceMatch` without fabricating identity. Unique Word and Word Type can additionally
populate `canonicalQuranWordId`.

The descriptor's stable workspace key is derived from every field that changes the result scope:
mode for Unique Word; `typeCode` for Lemma/Stem; the complete Word Type selection and scope. List
search/sort/page are origin navigation context, not result identity, unless an existing ayah
endpoint actually consumes them.

### 9. Data already available for each source

See the source capability matrix below. In summary:

- Unique Word simple/tashkeel: complete canonical match IDs and word tokens.
- Root/Lemma/Stem: complete word-level match booleans and text, but no canonical word IDs.
- Word Type selected word/group: raw DTO has canonical word/matched IDs; the existing UI mapper
  currently throws them away and remaps to indexes. The linking resolver should map the raw DTO
  directly into the neutral shape.
- Mushaf selected occurrence: `WordAnalysisViewModel.word` has canonical `quranWordId`,
  `wordLocation`, `verseKey`, and text. The current page has the rest of that ayah's display words,
  keyed by `wordLocation`, but those page words do not expose canonical IDs.

Every descriptor must preserve result-defining context: Unique mode; Lemma/Stem `typeCode`; and
the complete Word Type selection/scope. List search, sort, page, and presence filters are available
as origin context but do not redefine an existing detail read unless that endpoint consumes them.

### 10. Source types that can highlight accurately today

Accurate highlighting means accurate visible matched words. It does **not** mean every source can
already serialize canonical occurrence IDs. Root/Lemma/Stem are visually accurate at the returned
word granularity, but their future write contract needs more read/domain identity.

The existing `HighlightedAyahComponent` expects `AyahWordForHighlightDto` plus IDs. Its current
Root/Lemma/Stem mappers use array indexes as IDs
(`utils/root-ayah-match.mapper.ts:7-23`, `lemma-ayah-match.mapper.ts:7-23`,
`stem-ayah-match.mapper.ts:7-23`). For the linking workflow, either harden that renderer to accept
an explicit per-word `isSourceMatch` flag and optional canonical ID, or add a linking-owned
renderer with the same protected Quran presentation. Do not persist or command with those indexes.

Highlighting should initialize `true` for every Quran-derived source. The toggle changes only the
review presentation/prototype command option; it does not change which ayahs are selected.

### 11. Source types that need more read data later

- Root/Lemma/Stem need canonical matched Quran-word IDs; Lemma/Stem may also need segment identity
  if the future contract distinguishes segment matches within one word.
- Word Type needs `ayahId` and Arabic surah name for a complete neutral ayah contract, and a new
  source read only if a whole unselected grammatical-filter scope becomes linkable.
- Mushaf selected occurrence and both Unique Word modes have enough identity for this prototype.
- Every paged source needs deterministic complete traversal for the source-wide selection. That is
  an aggregation requirement now and may motivate a later read contract, but it does not justify a
  prototype write API.

### 12. Temporary Mock command/result architecture

Put a replaceable interface between workflow and execution:

```ts
interface LinkingCommandPort {
  execute(command: PrototypeLinkCommand): Promise<PrototypeLinkResult>;
}

interface PrototypeLinkCommand {
  readonly source: LinkingSourceDescriptor;
  readonly targetDoorId: number;
  readonly selectedVerseKeys: readonly string[];
  readonly highlightSourceWords: boolean;
}

type PrototypeLinkResult =
  | { kind: 'linked'; message: 'تم الربط بنجاح' }
  | { kind: 'submitted-for-review'; message: 'تم إرسال طلب الربط للمراجعة' };
```

The prototype implementation performs no HTTP call and writes no backend/cache data. It validates
that access is still allowed, the Door is still live, and at least one ayah remains selected, then
returns the result kind from current Owner state. A future API adapter can implement the port, but
the real backend—not the browser—must become authoritative for permission and outcome.

Do not create a fake Request ID, approval queue entry, Draft ID, audit event, or persisted “linked”
status. The terminal result exists only in the active dialog. `qd-notice` can announce the outcome;
the workflow should also show the same visible result as its final step.

### 13. Owner/Admin final UX

At confirmation time—not only when the dialog opens—re-read `CurrentUserStore`:

- Active Owner: mock result `linked`, visible copy `تم الربط بنجاح`.
- Active non-Owner with the future exact linking permission: mock result
  `submitted-for-review`, visible copy `تم إرسال طلب الربط للمراجعة`.
- Unknown, loading, inactive, signed-out, or no longer permitted: fail closed; disable Confirm and
  show an access-changed message. Never silently choose either success branch.

The branch must use `isOwner` and exact permission, not a role name. Owner/direct versus
Admin/review is prototype presentation only; a later server response must choose the real result.

**Current blocker:** the exact linking permission does not exist in the generated frontend
catalogue. The report therefore does not recommend a fake code. Until that contract exists, only
the active Owner path can be truthful. A prototype-only developer switch would not satisfy the
locked product permission behavior and should not ship in the real frontend.

### 14. `sessionStorage` restoration

Use a namespaced, versioned envelope, for example:

```ts
interface StoredLinkingWorkspaceV1 {
  readonly version: 1;
  readonly actorSub: string;
  readonly items: readonly StoredPreparedSourceV1[];
}

interface StoredPreparedSourceV1 {
  readonly key: string;
  readonly source: LinkingSourceDescriptor;
  readonly selection:
    | { mode: 'all-except'; verseKeys: readonly string[] }
    | { mode: 'only'; verseKeys: readonly string[] };
  readonly resultCount: number;
  readonly highlightSourceWords: boolean;
}
```

The adaptive selection encoding keeps persistence small:

- Initial/Select All: `all-except` with an empty set.
- Individual deselection: add hidden or visible verse keys to the exclusion set.
- Clear All: `only` with an empty set.
- Individual selection after Clear All: add keys to the inclusion set.

Restoration rules:

1. Parse in a guarded browser-only path; unknown version, invalid union member, nonpositive ID,
   malformed key, or non-string verse key rejects the affected item or whole envelope fail closed.
2. Wait for current-user resolution before exposing restored items. Restore only when `actorSub`
   matches the current user; clear on logout or identity change.
3. Do not persist open dialogs, current step, loading/error state, loaded ayah text, Door selection,
   or mock result.
4. Re-resolve each source lazily when the user opens/edits/links it; apply the stored selection mode
   against the newly loaded `verseKey` universe.
5. Drop exclusions/inclusions that no longer exist, refresh the result count, and show a calm info
   notice if the read data changed.
6. Deduplicate source keys and verse keys. Preserve workspace order.
7. Guard quota/storage failures and keep the in-memory workspace functional, matching existing
   frontend storage behavior.

### 15. Explicitly outside the prototype

The following must remain absent:

- linking write API, command, endpoint, generated request DTO, or `HttpClient` mutation;
- migrations, tables, entities, Drafts, Requests, approvals, review queues, publishing, or audit;
- backend authorization behavior or a client-created permission string;
- durable linked/submitted status, request IDs, retry queues, offline sync, or reconciliation;
- cross-user workspace restoration;
- linking multiple prepared sources in one command;
- grouped links created from Root/Lemma/Stem/Unique/Word Type results;
- full Mushaf grouped-selection design;
- changes to Quran source data, Quran text, glyph rendering, or fabricated word identity;
- route/URL serialization of the global workspace;
- a new generic application store, drawer system, modal geometry, notification system, or design
  system.

### 16. Reuse versus new frontend components

| Reuse | Use |
|---|---|
| `qd-modal-shell` `wide` | Global workspace and one Direct Link workflow shell. |
| `qdAction` | Navbar trigger, source actions, Back/Next/Confirm, remove/edit actions. |
| `qd-form-field` + `qdControl` | Ayah search input. |
| Native `.qd-checkbox` pattern | Ayah selection, Select All/Clear All controls. No new checkbox system is required. |
| `qdAyahCard` | Neutral card frame for each selectable ayah. |
| `qd-empty-state`, `qd-error-state`, skeleton, `qd-notice` | Existing async/result semantics. |
| `qd-details-workspace` | Workspace header/action/body/footer anatomy where useful. |
| `AbwabSnapshotFacade` + `AbwabDoorPickerComponent` | Real live Door read and single target selection. |
| Existing Words/Mushaf APIs and caches | Read-only source resolution; no source facade mutation. |
| Existing Arabic search normalizer | `features/mushaf/utils/arabic-search-normalize.ts` already provides diacritic-insensitive comparison. On a second consumer, move/promote the pure helper to an appropriate shared Quran/search location rather than importing Mushaf domain UI. Display text remains untouched. |

New linking-owned pieces are justified for domain behavior: workspace store and codec, global host,
common source actions, workflow facade/component, resolver registry and resolvers, selectable ayah
list, neutral source/ayah models, and mock command port.

### 17. Current components that should not absorb linking behavior

- `TopNavbarComponent`: only trigger/count/visibility wiring; no workspace array, persistence, or
  workflow steps.
- `NAV_MENU`, `NavItem`, and route definitions: workspace is not navigation.
- `MushafWordComponent`, Mushaf line/page renderers, and Quran styles: no per-glyph linking actions
  or renderer changes.
- Roots/Lemmas/Stems/Unique/Word Type table components: no linking menus per row. Resolve a source
  through the existing detail selection, then show the shared actions.
- Existing source explorer facades/controllers and URL sync: no global workspace ownership and no
  mock command behavior.
- Entity-detail overlay URL codec/history: linking state is not part of `qdDetail` frames.
- `AbwabPageComponent`, `AbwabTreeComponent`, `AbwabMovePickerComponent`, Abwab selection store, and
  Abwab write controllers: Door selection reuses the read facade and door picker only.
- `qd-confirm-dialog`: do not open a nested confirmation dialog; confirmation is a step inside the
  Direct Link shell.
- API caches: a mock confirmation must not invalidate or mutate them.

The only shared Words modification justified by this architecture is a domain-neutral projected
action seam in the details shell if required. The linking action component remains linking-owned.

### 18. Frontend contracts that inform the later backend/API design

The frontend audit establishes these future contract requirements without designing or
implementing them now:

1. A canonical linking permission code propagated through the existing permission catalogue and
   `/api/access/me` contract.
2. A discriminated source descriptor matching the frontend union, including Unique mode,
   Lemma/Stem `typeCode`, and complete Word Type word/group scope.
3. One source-resolution read shape with stable ayah identity, verse key, page/surah metadata,
   Uthmani display words, canonical Quran-word IDs where highlighting/persistence requires them,
   and explicit matched occurrences.
4. Canonical Quran-word IDs for Root/Lemma/Stem results; current array indexes are not identities.
5. Word Type `ayahId` and Arabic surah name, plus an explicit endpoint if “entire current
   grammatical scope” becomes a source without selecting a row/group.
6. A source-wide retrieval strategy suitable for selection of every result. The current UI reads
   100-row detail pages; the later contract may remain paged, but it must support deterministic
   complete traversal and stable ordering.
7. Independent ayah links for source-derived multi-ayah selections. No implicit grouped-link
   aggregate.
8. A server-authoritative result discriminant for Owner direct link versus Admin submission for
   review. The frontend should render the returned outcome, not infer the final durable state.
9. Validation that the target Door is live and the source/ayah relation is still valid at command
   time.
10. A decision about whether `highlightSourceWords` is only presentation preference or persisted
    link metadata. The prototype may carry it, but it must not force a storage design.
11. Idempotency/conflict semantics and audit/request identifiers only when the real write workflow
    is designed; they are intentionally absent from this prototype.

## Source capability matrix

“Canonical source ID” means the source itself has a stable identity. It is distinct from the
“matched-word IDs” column. “Accurate highlight now” means the currently returned matched visible
words can be highlighted without guessing; it does not claim that every match has a canonical
Quran-word ID.

| Source | Current route/page | Canonical source ID available? | Display label available? | Ayah result list available? | Ayah text available? | QuranWord / matched-word IDs available? | Can highlight accurately now? | Needs additional read data? | Existing component/facade to reuse | Add-to-Linking integration point | Direct-Link integration point |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Mushaf selected word occurrence | `/dashboard/mushaf`; `MushafReaderPageComponent` + selected-word study section | **Yes after word analysis:** `quranWordId`; also `wordLocation`, `verseKey`, `pageNumber`. Page words alone expose only `wordLocation`. | **Yes:** selected `textUthmani`. | **One selected ayah**, reconstructable from current page words. This is not an all-occurrences read. Use Unique Word actions for all simple/tashkeel occurrences. | **Yes:** current page token list for the selected `verseKey`. | **Selected occurrence only:** canonical ID from `WordOccurrenceDto`; sibling page words have no canonical IDs. | **Yes for the one selected occurrence**, using `wordLocation`/selected canonical ID. | **No for the one-occurrence prototype.** A dedicated ayah read would simplify rehydration; grouped Mushaf selection remains separate. | `MushafReaderFacade`, `MushafPagesApi`, `MushafWordAnalysisApi`, `SelectedWordSectionComponent`. | Selected-word section header after analysis succeeds. | Same common action component; Direct Link descriptor is the selected occurrence. |
| Unique Word — simple | `/dashboard/words/unique/simple`; `UniqueWordsPageComponent` | **Yes:** `(mode='simple', id)`. | **Yes:** `displayText`. | **Yes, paged:** `/api/words/unique/simple/{id}/ayahs`. | **Yes:** tokenized `words[].textUthmani`. | **Yes:** `words[].quranWordId` and `matchedQuranWordIds`. | **Yes, canonical.** | **No identity gap for prototype.** Complete traversal still aggregates pages. | `UniqueWordsApi`, `UniqueWordsDrilldownController/Facade`, `WordDrilldownModalComponent`, `AyahMatchesListComponent`. | Drilldown details action zone; same in global overlay adapter. | Same zone/action; descriptor preserves `mode`. |
| Unique Word — tashkeel | `/dashboard/words/unique/tashkeel`; `UniqueWordsPageComponent` | **Yes:** `(mode='tashkeel', id)`. | **Yes:** `displayText`. | **Yes, paged:** `/api/words/unique/tashkeel/{id}/ayahs`. | **Yes:** tokenized words. | **Yes:** canonical word and matched IDs. | **Yes, canonical.** | **No identity gap for prototype.** Complete traversal still aggregates pages. | Same Unique Words stack as simple mode. | Drilldown details action zone; same in global overlay adapter. | Same zone/action; never collapse simple and tashkeel IDs into one namespace. |
| Root | `/dashboard/words/roots`; `RootsExplorerPageComponent` | **Yes:** `rootId`. | **Yes:** `rootText`. | **Yes, paged:** `/api/words/roots/{id}/ayahs`. | **Yes:** ordered Uthmani word tokens. | **No canonical Quran-word IDs.** Response carries `isMatched` only; current mapper uses array indexes as render surrogates. | **Yes at returned word level**, using `isMatched`. | **Yes later:** canonical Quran-word/matched occurrence IDs if the real contract needs them. | `RootsApi`, `RootsDetailController/Facade`, `RootDetailsPanelComponent`; current ayah mapper only as evidence, not identity. | Generic details-shell action slot; adapter builds `{rootId,label}`. | Same common action; resolver aggregates root ayah pages. |
| Lemma | `/dashboard/words/lemmas`; `LemmasExplorerPageComponent` | **Yes:** `lemmaId`; ayah result identity also includes optional `typeCode`. | **Yes:** `lemmaText`. | **Yes, paged:** `/api/words/lemmas/{id}/ayahs?typeCode=...`. | **Yes:** ordered Uthmani word tokens. | **No canonical IDs.** `isMatched` is word-level; no segment ID is exposed. | **Yes at returned word level.** It cannot identify which internal segment matched. | **Yes later:** canonical matched word/segment occurrence identity where required. | `LemmasApi`, `LemmasDetailController/Facade`, `LemmaAyahTypeFiltersComponent`, `LemmaDetailsPanelComponent`. | Generic details action; descriptor must capture current `typeCode`. | Same action; resolver must not lose `typeCode`. |
| Stem | `/dashboard/words/stems`; `StemsExplorerPageComponent` | **Yes:** `stemId`; ayah result identity also includes optional `typeCode`. | **Yes:** `stemText`. | **Yes, paged:** `/api/words/stems/{id}/ayahs?typeCode=...`. | **Yes:** ordered Uthmani word tokens. | **No canonical IDs.** `isMatched` only; current mapper uses indexes. | **Yes at returned word level.** | **Yes later:** canonical matched word/segment occurrence identity where required. | `StemsApi`, `StemsDetailController/Facade`, `StemAyahTypeFiltersComponent`, `StemDetailsPanelComponent`. | Generic details action; descriptor captures `typeCode`. | Same action; resolver aggregates the exact filtered result. |
| Word Type selected word / grouped dimension | `/dashboard/words/types`; `WordTypesExplorerPageComponent` | **Yes, composite.** Word: `tashkeelWordId + contextCode + case + tense + voice` plus stored scope. Group: `kind + dimensionId + type + childCode + case + tense + voice`. A bare whole-filter scope is **not** currently a source read. | **Yes:** selected word/group summary `displayText`; type labels/scopes are available. | **Yes, paged after a selected word or grouped root/stem/lemma row.** No current ayah endpoint for the unselected whole table/filter scope. | **Yes:** `words[].textUthmani`. | **Yes in raw DTO:** canonical `words[].quranWordId`, `matchedWordIds`, and matched positions. Current shared mapper discards IDs and substitutes indexes. | **Yes**, if the linking resolver maps the raw DTO rather than the lossy shared mapper. | **Yes for metadata:** `ayahId` and Arabic surah name. **Yes if whole-filter scope is required:** a new read contract. | `WordTypesApi`, `WordTypesDetailFacade/Controller`, `WordTypeDetailSelection`, `WordTypeDetailsPanelComponent`. | Generic detail action; page can support word and grouped selections. Global overlay currently supports word-kind only. | Same action; descriptor must preserve the complete result scope and distinguish list-only origin filters. |

## Ayah selection and search contract

All source results are selected by default. Selection is keyed by `verseKey`, not array index,
pagination position, or filtered-list position. `verseKey` is present in every current source ayah
response and avoids the `ayahId` gaps in Word Type.

The workflow state should derive:

- `allAyahs`: the complete resolved source set;
- `searchQuery`: transient and not persisted;
- `visibleAyahs`: client-side filtered `allAyahs`;
- `selectedVerseKeys`: derived from the adaptive selection model over **allAyahs**;
- `selectedCount`: global selected count, never visible-row count.

Filtering must not mutate the selection model. Select All and Clear All apply to the complete source
set, not only visible results. If a visible-only bulk action is ever desired, it needs different
explicit copy and is outside this prototype direction.

Current responses contain enough Uthmani tokens to build a client-side search string for all seven
matrix rows. Search should compare a derived normalized copy and continue displaying the exact
returned Uthmani text. The existing `arabic-search-normalize.ts` supplies a safe pure comparison
seam; no Quran text is rewritten or invented. Verse key and available Arabic surah name may also be
search fields, but search remains strictly within `allAyahs`.

Because source reads are paged, the selection screen should not claim “all selected” until complete
aggregation succeeds. A partial read failure keeps the workflow in a retryable read-error state; it
must not confirm against an incomplete invisible result set.

## Workspace v1 contract

Each prepared item contains:

- derived stable key;
- serializable source descriptor;
- source type label and display label;
- result count and selected count;
- adaptive selection override;
- `highlightSourceWords`, default `true`;
- optional origin route context for returning to the source.

Supported operations:

- idempotent add/focus existing item;
- remove;
- open/edit source-wide ayah selection;
- toggle highlight preference;
- start Direct Link for one item;
- retain items across navigation and same-tab refresh.

Not included in v1:

- batch-link multiple prepared sources;
- Door assignment on the workspace card;
- status columns such as Draft, Sent, Approved, or Linked;
- mock activity history;
- automatic removal after mock success.

Direct Link invoked from a source may use an ephemeral descriptor without adding it to the
workspace. Invoked from a workspace item, it uses that item's saved selection/highlight settings.
In both cases the final command expands the selection model to explicit selected verse keys.

## Mushaf grouped-linking seam

Grouped linking should remain owned by the Mushaf reading interaction, not inferred from a Words
source result or from multiple items in the global workspace. The only shared decision needed now
is extensibility: the resolver registry and neutral `LinkingAyah` model must permit a future,
distinct Mushaf-group descriptor without changing the existing source-derived contracts.

The later UI seam is the Mushaf page/reader facade above `MushafPageAreaComponent`, where multiple
ayah/word selections can be coordinated with the reader's URL and page state. It is not
`MushafWordComponent`, the Words source adapter, or `LinkingWorkspaceStore`. No `mushaf-group`
descriptor, grouping controls, group identity, or persistence should be added during this
prototype merely to reserve that future path.

## Final recommendation

Proceed later with one frontend `linking` feature centered on a root-scoped Signals store, one
root-mounted deferred host, descriptor-driven source adapters, the existing Wide modal shell, the
existing Abwab live tree/picker, and versioned `sessionStorage`. Keep source actions thin and place
them at detail-level action seams. Treat current Root/Lemma/Stem matches as presentation booleans,
not Quran-word identities. Restrict truthful prototype access to active Owner until a canonical
linking permission exists; then the same `CurrentUserStore.can(...)` mechanism can expose the
Admin/review branch without new auth architecture.

This architecture locks the UX and the frontend contract while leaving the future backend Draft,
Request, approval, authorization, and mutation design genuinely unbuilt.
