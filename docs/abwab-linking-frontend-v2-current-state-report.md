# Abwab Linking Frontend V2 — Current State Report

**Scope:** read-only factual audit of the implemented Abwab Linking Frontend Prototype V2 on branch
`feature/abwab-linking-frontend-prototype` (head `b25f8e98`).

**Purpose:** produce the factual handoff that a later Backend/database architecture report will be
written against. This document records what exists now, what works, what is frontend-prototype-only,
and what a future Backend must eventually support.

**Method:** source-code inspection only. No browser, no runtime measurement, no test execution, no
production build, no backend inspection beyond the frontend's existing generated read contracts. No
production code, Backend code, migration, API, database design, test, or commit was created or
modified while producing this report.

**Authority note:** `docs/abwab-linking-frontend-prototype-v2-report.md` and
`docs/abwab-linking-frontend-prototype-v2-plan.md` were read as intent. Where the implemented code
differs from them, **this report records the code**, and the divergence is called out explicitly.

---

## 0. Inventory of the implemented feature

All Linking code lives under
`Frontend/quran-dashboard-ui/src/app/features/linking/`.

| Layer | Files |
| --- | --- |
| Models | `linking-source.models.ts`, `linking-manual-mushaf.models.ts`, `linking-workspace.models.ts`, `linking-ayah.models.ts`, `linking-operation.models.ts`, `linking-merge.models.ts`, `linking-workflow.models.ts`, `linking-workspace-view.models.ts`, `linking-focus-origin.models.ts`, `linking.labels.ts` |
| State | `linking-access.service.ts`, `linking-workspace.store.ts`, `linking-source-set.coordinator.ts`, `linking-workflow.facade.ts`, `linking-source-editor.facade.ts`, `linking-manual-word-editor.facade.ts`, `manual-mushaf-selection.store.ts`, `linking-focus.coordinator.ts` |
| Data access | `linking-source-resolver.ts`, `linking-source-resolver.registry.ts`, `complete-paged-source.loader.ts`, `resolvers/{unique-word,root,lemma,stem,word-type,manual-mushaf-ayahs}-linking-source.resolver.ts`, `manual-mushaf-ayah.reader.ts`, `linking-workspace.repository.ts`, `local-storage-linking-workspace.repository.ts`, `linking-workspace.codec.ts`, `linking-command.port.ts`, `mock-linking-command.port.ts` |
| Utils | `linking-source-key.ts`, `linking-selection.ts`, `linking-merge.ts`, `linking-source-intents.ts`, `linking-operation-members.ts`, `linking-verse-order.ts`, `manual-link-shape.ts`, `manual-mushaf-ayah-completeness.ts`, `linking-source-presentation.ts` |
| Components | `linking-workspace-host`, `linking-workspace`, `linking-workspace-source-row`, `linking-source-ayah-editor`, `linking-manual-word-editor`, `linking-ayah-selection`, `linking-ayah-card`, `direct-link-workflow`, `linking-door-step`, `quran-source-linking-actions`, `mushaf-selection-status` |
| Current-truth README | [features/linking/README.md](Frontend/quran-dashboard-ui/src/app/features/linking/README.md) |

Integration points outside the feature:

- [app.ts:21](Frontend/quran-dashboard-ui/src/app/app.ts:21) mounts `<qd-linking-workspace-host />` as a sibling of the app shell and the Words entity-detail overlay host.
- [top-navbar.component.ts:63-76,197-205](Frontend/quran-dashboard-ui/src/app/core/layout/top-navbar/top-navbar.component.ts:197) — Owner-only مساحة الربط trigger + prepared-row count badge.
- Words: 4 detail panels, 5 entity-detail-overlay adapters, 4 explorer pages, the unique-word drilldown modal, and `word-types-detail-panel.view-model.ts` build `LinkingSourceDescriptor` values and render `qd-quran-source-linking-actions`.
- Mushaf: [mushaf-reader-page.component.ts](Frontend/quran-dashboard-ui/src/app/features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.ts) owns the ayah-selection dispatch branch; `mushaf-page-area → mushaf-header-navigation / mushaf-page-view → mushaf-line → mushaf-word` carry neutral `ayahSelectionMode` / `selectedVerseKeys` inputs.

---

## 1. Linking workspace — implemented model

### 1.1 Access gate

[`LinkingAccessService.canUseLinking`](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-access.service.ts:9) is
`authStateKnown() && isAuthenticated() && isActive() && isOwner()`. It is re-checked inside every
public mutator of `LinkingWorkspaceStore` (via `canMutate()`), inside `ManualMushafSelectionStore`
(`requireOwner()`), inside `LinkingWorkflowFacade`, inside `LinkingSourceSetCoordinator`, and again
inside `MockLinkingCommandPort.execute`. Fail-closed is implemented, not merely visual.

### 1.2 Prepared sources (the durable unit)

```ts
// models/linking-workspace.models.ts:36
interface LinkingWorkspaceItem {
  sourceKey: string;
  source: LinkingSourceDescriptor;
  configuration: LinkingSourceConfiguration;   // 'automatic' | 'manual'
  configurationRevision: number;
  lastResolvedCount: number | null;
  lastResolvedCountIsStale: boolean;
}
```

`LinkingSourceConfiguration` is a discriminated union
([linking-workspace.models.ts:16](Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workspace.models.ts:16)):

- `automatic` → `{ ayahInclusion, automaticWordMatchesEnabled }`
- `manual` → `{ ayahInclusion, wordLocationsByVerseKey, linkShape }`

Impossible combinations (an automatic source holding manual word locations, or vice versa) are
unrepresentable. `initialConfiguration()`
([linking-workspace.store.ts:507](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts:507))
defaults automatic sources to `automaticWordMatchesEnabled: true`, and manual sources to
`linkShape: 'grouped'` when the set has more than one verse, `'independent'` otherwise.

`addSource()` is idempotent by `sourceKey`: re-adding an equivalent descriptor **replaces only the
descriptor** (refreshing the display label) and preserves `configuration`, `lastResolvedCount`,
staleness, `configurationRevision`, and row position
([linking-workspace.store.ts:70-98](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts:70)).

### 1.3 Selected/checked sources for the current operation

`checkedSourceKeysSignal` is a separate ordered `string[]`
([linking-workspace.store.ts:38](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts:38)).

- `checkSource` / `uncheckSource` / `clearCheckedSources` touch **only** that signal — they never
  call `persist()`.
- `remove()` drops the key from the checked set; `undoRemove()` restores it if it was checked.
- Hydration explicitly sets it to `[]` ([store:362](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts:362)).
- `acknowledgeSuccess()` clears it after a workspace-origin mock success
  ([linking-workflow.facade.ts:221-223](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts:221)).

**Checked membership is transient, by design and in code.**

### 1.4 Per-source ayah inclusion

`LinkingSelection = { mode: 'all-except' | 'only'; verseKeys }`. The default is
`{ mode: 'all-except', verseKeys: [] }` — everything included.
[`utils/linking-selection.ts`](Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-selection.ts)
supplies toggle, select-all, clear-all, `reconcileLinkingSelection` (drops overrides no longer in the
universe), `selectedLinkingVerseKeys`, and `selectedLinkingAyahCount`. Overrides are always stored in
universe order and de-duplicated.

Inclusion is edited only through the source-ayah editor and is **persisted**.

### 1.5 Per-source word behaviour

- **Automatic sources:** one boolean `automaticWordMatchesEnabled`, rendered as a native checkbox in
  the row ([linking-workspace-source-row.component.html:19-23](Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-source-row/linking-workspace-source-row.component.html:19)).
  There is no per-word picker for automatic sources. OFF suppresses every word contribution but keeps
  every included ayah ([linking-source-set.coordinator.ts:187-202](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-set.coordinator.ts:187)).
- **Manual source:** `wordLocationsByVerseKey: Readonly<Record<string, readonly string[]>>`, edited in
  the dedicated manual word editor. Empty set = ayah included, zero word highlights. Never "all
  words".

Both are **persisted**.

### 1.6 Automatic sources

Unique Word, Root, Lemma, Stem, Word Type. They are prepared from Words explorer pages, Words detail
panels, the unique-word drilldown modal, and the five entity-detail-overlay adapters, all through the
shared `qd-quran-source-linking-actions` seam (إضافة للربط / ربط مباشر).

### 1.7 Manual Mushaf source

Created only from the Mushaf reader. `ManualMushafSelectionStore`
([manual-mushaf-selection.store.ts](Frontend/quran-dashboard-ui/src/app/features/linking/state/manual-mushaf-selection.store.ts))
owns a transient, Owner-gated draft:

- `activate()` / `cancel()` / `clear()` / `toggle(verseKey)` / `retry(verseKey)` / `remove(verseKey)` / `addToWorkspace()`;
- each toggled verse triggers `ManualMushafAyahReader.readMetadata(verseKey)` (an ayah-study read) and
  is held as `loading | ready | error` with a generation guard;
- `canAddToWorkspace` requires **every** entry `ready` with a non-null reference;
- entries are kept in numeric Quran order;
- the draft survives Mushaf page navigation, is discarded on route destroy
  ([mushaf-reader-page.component.ts:79](Frontend/quran-dashboard-ui/src/app/features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.ts:79)),
  cancel, successful handoff, or access loss;
- handoff calls `workspace.addSource(...)` and does **not** open the workspace or move focus.

The reader header exposes the Owner-only `تحديد` toggle with `aria-pressed`
([mushaf-header-navigation.component.html:29-37](Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-header-navigation/mushaf-header-navigation.component.html:29)).
In mode, `onAyahSelect` consumes the verse toggle and `ignoreNextWordSelection` suppresses the
immediately following `wordSelect`; `ArrowLeft`/`ArrowRight` study navigation is paused
([mushaf-reader-page.component.ts:51,93-114](Frontend/quran-dashboard-ui/src/app/features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.ts:93)).
`qd-mushaf-selection-status` is mounted at reader-page level, **outside** the page-area load-state
conditional, so count/clear/cancel/add survive page loading and errors.

### 1.8 Grouped vs independent manual intent

Stored as `linkShape: 'grouped' | 'independent'` on the manual configuration. The **effective** shape
is derived, never stored:

```ts
// utils/manual-link-shape.ts:8
effectiveManualLinkShape(preference, includedVerseKeys) =
  includedVerseKeys.length > 1 ? preference : 'independent';
```

The stored preference is not erased when inclusion temporarily drops to one ayah. The radio pair is
rendered only when `selectedCount() > 1`; otherwise the editor states the effective single
interpretation
([linking-source-ayah-editor.component.html:22-36](Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-source-ayah-editor/linking-source-ayah-editor.component.html:22)).

### 1.9 Persistence behaviour — persisted vs transient

**Persisted** (`LinkingWorkspacePersistedItem`, [linking-workspace.models.ts:52](Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workspace.models.ts:52)):

| Field | Note |
| --- | --- |
| `sourceKey` | recomputed on decode; persisted value is not trusted |
| `source` (descriptor incl. `label`, manual `pageNumber`/`displayHint`) | display snapshot, never identity |
| `configuration` | ayah inclusion + automatic flag **or** manual word locations + linkShape |
| `lastResolvedCount` | always re-marked stale on decode |
| ordered item position | array order |

**Transient (never serialised):** checked source keys, `configurationRevision`,
`lastResolvedCountIsStale=false`, active surface, editor source key, undo snapshot, clear-all request,
persistence warning, resolved `LinkingAyah[]`, Quran text/DTOs, `MergedLinkingSelection`,
`LinkingSourceIntent[]`, per-member load/progress/error, selected Door, mock result, focus origin,
editor search query, editor/review client page, Mushaf selection draft.

On decode, every row is reconstructed with `configurationRevision = 0` and
`lastResolvedCountIsStale = true` ([linking-workspace.codec.ts:105](Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-workspace.codec.ts:105)),
so a restored row renders as *unchecked, unresolved, stale count*.

### 1.10 Actor isolation

Key: `qd-linking-workspace:v2:${encodeURIComponent(actorSub)}`
([local-storage-linking-workspace.repository.ts:27](Frontend/quran-dashboard-ui/src/app/features/linking/data-access/local-storage-linking-workspace.repository.ts:27)).
The envelope repeats `version: 2`, `actorSub`, `revision`, and `items`; `parseEnvelope` rejects a
payload whose `actorSub` is not exactly the requesting subject.

- No storage call happens before `authStateKnown()` and while `loadState() === 'loading'`
  ([store:327](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts:327)).
- Actor change bumps `actorGeneration`, resets in-memory state, and re-hydrates; late completions from
  a previous generation are dropped (`isCurrentActor`).
- Losing access / logging out calls `resetInMemoryWorkspace()` — it clears memory and **preserves**
  that actor's bucket. Only a malformed/cross-actor payload triggers `invalidateActiveActor`.
- `items` and `checkedSourceKeys` are computed to `[]` until `isReadyForCurrentActor()`, so another
  actor's rows cannot flash.
- Saves are serialised through a promise chain (`saveQueue`) with a monotonic
  `durableWorkspaceRevision`; a storage failure surfaces a non-blocking Arabic warning and leaves the
  in-memory workspace usable.
- Same-actor multi-tab is last-writer-wins; there is no `storage` event listener anywhere.

### 1.11 Direct Link behaviour

`LinkingWorkflowFacade.startFromSource(source)`
([linking-workflow.facade.ts:104](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts:104)):

- if a Words entity overlay is open, the source is parked in `pendingSource`, the overlay is closed
  into its retained state, and the flow starts only after `overlay.isOpen()` becomes false;
- builds **one ephemeral member** via `ephemeralLinkingOperationMember(...)` with
  `sourceKey = 'ephemeral:<kind>'`, `origin: 'direct-link'`, `configurationRevision: 0`;
- **never** adds a row to the workspace;
- `origin: 'direct-link'` is what prevents the coordinator from writing reconciliation back into the
  workspace ([coordinator:103](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-set.coordinator.ts:103));
- the flow opens at a `configure-source` step that exposes **only** the automatic word-match toggle
  ([direct-link-workflow.component.html:18-28](Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.html:18)).

> **Divergence from plan:** the V2 report §12.2 wanted Direct Link to also expose an ayah-inclusion
> editor. It does not. A Direct Link operation always uses `DEFAULT_LINKING_SELECTION` — all resolved
> ayahs of that one source.

Direct Link dismissal restores the retained entity overlay first, then `LinkingFocusCoordinator`
targets the regenerated source action via the `data-linking-source-action` selector fallback
([linking-focus.coordinator.ts:64-74](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-focus.coordinator.ts:64)).

### 1.12 Surfaces and shell

One `qd-modal-shell` at `80vw × 88dvh`
(`--qd-linking-workspace-modal-inline-size` / `-block-size`, `styles/_tokens.scss:247-248`),
`flushBody=true`, `returnFocus=false`, hosting five surface states
(`closed | workspace | source-ayah-editor | manual-word-editor | linking-flow`). Inner surfaces are
`@defer (on idle)` with an sr-only status placeholder; the shell itself mounts synchronously with the
inert boundary. The remove-all `qd-confirm-dialog` is a sibling top layer, the only nested-modal
exception.

Scroll ownership (one vertical owner per surface):

| Surface | Owner |
| --- | --- |
| Modal shell body | `overflow: hidden` via `flushBody` |
| Workspace | `.qd-details__body` (`styles/_components.scss:1036-1042`) |
| Source-ayah editor | `linking-source-ayah-editor.component.scss:13` |
| Manual word editor | `…__body` at `linking-manual-word-editor.component.scss:45` (host `overflow: hidden` at `:13`) |
| Door/review flow | `direct-link-workflow.component.scss:64` |

---

## 2. Source families — implemented descriptors and read paths

`LinkingSourceKind = 'manual-mushaf-ayahs' | 'unique-word' | 'root' | 'lemma' | 'stem' | 'word-type'`
([linking-source.models.ts:6](Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-source.models.ts:6)).
`'mushaf-word'` is fully gone.

Stable keys ([utils/linking-source-key.ts](Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-key.ts)),
`|`-joined and `encodeURIComponent`-escaped, deliberately excluding `label`:

| Kind | Descriptor shape | `sourceKey` composition |
| --- | --- | --- |
| `unique-word` | `{ mode: 'simple'\|'tashkeel', wordId, label }` | `unique-word\|mode\|wordId` |
| `root` | `{ rootId, label }` | `root\|rootId` |
| `lemma` | `{ lemmaId, typeCode: string\|null, label }` | `lemma\|lemmaId\|typeCode` |
| `stem` | `{ stemId, typeCode: string\|null, label }` | `stem\|stemId\|typeCode` |
| `word-type` | `{ selection: word\|root\|stem\|lemma + full scope {type, childCode, case, tense, voice}, label }` | `word-type\|selectionKind\|id(+contextCode/case/tense/voice for 'word')\|…scope` |
| `manual-mushaf-ayahs` | `{ manualAyahs: [{ verseKey, pageNumber\|null, displayHint\|null }], label }` | `manual-mushaf-ayahs\|<numerically ordered, de-duplicated verseKeys>` |

Manual identity is the normalised verse set only — page hints, display hints, label, inclusion,
selected words, and `linkShape` never participate.

### 2.1 Per-family read behaviour

| Family | Endpoint (frontend call) | Page size | Multi-request? | Word-match truth | Canonical word ID |
| --- | --- | --- | --- | --- | --- |
| Unique Word | `GET api/words/unique/{simple\|tashkeel}/{wordId}/ayahs` | `DEFAULT_AYAH_PAGE_SIZE = 100` | **Yes** — all pages sequentially | `matchedQuranWordIds.includes(word.quranWordId)` | **Yes**, `word.quranWordId` |
| Root | `GET api/words/roots/{rootId}/ayahs` | `ROOT_DETAIL_PAGE_SIZE = 100` | **Yes** | `word.isMatched` flag | **No** — set to `null` |
| Lemma | `GET api/words/lemmas/{lemmaId}/ayahs` (+`typeCode`) | `LEMMA_DETAIL_PAGE_SIZE = 100` | **Yes** | `word.isMatched` flag | **No** |
| Stem | `GET api/words/stems/{stemId}/ayahs` (+`typeCode`) | `STEM_DETAIL_PAGE_SIZE = 100` | **Yes** | `word.isMatched` flag | **No** |
| Word Type (word) | `GET api/words/word-types/words/{tashkeelWordId}/ayahs` | `WORD_TYPES_DETAIL_PAGE_SIZE = 100` | **Yes** | `matchedWordIds.has(word.quranWordId)` | **Yes** |
| Word Type (grouped) | `GET api/words/word-types/table/{roots\|stems\|lemmas}/{dimensionId}/ayahs` | `100` | **Yes** | `matchedWordIds` | **Yes** |
| Manual Mushaf | `GET api/mushaf/ayahs/{verseKey}/study` **+** `GET api/mushaf/pages/{n}` for every page in `pageFrom..pageTo` | n/a | **Yes** — 1 study read + 1..N page reads **per verse** | `wordLocation ∈ configuration.wordLocationsByVerseKey[verseKey]` | **No** — `null` |

Retained per-family configuration beyond identity: Lemma/Stem keep `typeCode`; Word Type keeps its
full selection discriminant + grammatical scope; Unique Word keeps `mode`; the manual source keeps
per-verse `pageNumber`/`displayHint` hints (refreshable, never authoritative).

### 2.2 Duplicate collapsing inside one source

Each automatic resolver collapses repeated `verseKey` rows only when the fully mapped ayah objects are
JSON-identical; contradictory duplicates throw
(`تعارضت بيانات الآية المكررة في نتائج المصدر.`).

### 2.3 Manual completeness proof

[`ManualMushafAyahReader`](Frontend/quran-dashboard-ui/src/app/features/linking/data-access/manual-mushaf-ayah.reader.ts)
+ [`manual-mushaf-ayah-completeness.ts`](Frontend/quran-dashboard-ui/src/app/features/linking/utils/manual-mushaf-ayah-completeness.ts)
publish a manual ayah only after proving all of:

1. `AyahCoreDto.verseKey` equals the requested key;
2. every page `pageFrom..pageTo` returns a matching `pageNumber` envelope (via `MushafReaderCache`, concurrency 3);
3. tokens ordered by `pageNumber → lineNumber → lineWordOrder`, filtered to this `verseKey`;
4. every non-marker word has a `wordLocation`;
5. non-marker `wordNumber` values are contiguous `1..wordsCount`;
6. non-marker count `=== AyahCoreDto.wordsCount`;
7. `wordLocation` unique within the ayah and `renderPosition === index + 1`.

Any failure throws one controlled blocking error; a partial ayah is never published.
`validateManualWordLocations` additionally rejects saved locations that are no longer in the
freshly-read ayah (`إحداثيات كلمات المصحف المحفوظة لم تعد صالحة.`).

**No word-analysis request is made on this path.** `MushafWordAnalysisApi` is not referenced anywhere
in Linking.

---

## 3. Current ayah loading behaviour  *(critical section)*

### 3.1 Normal source browsing (unchanged, paginated)

Words explorer pages, detail panels, and entity-detail overlays continue to browse
`.../ayahs?page=N&pageSize=100` one page at a time and render `qd-pagination`. That behaviour is
untouched by V2 and remains correct.

### 3.2 Linking Workspace — what the code does today

**Loading is already complete-set.** All five automatic families call
[`loadCompletePagedSource`](Frontend/quran-dashboard-ui/src/app/features/linking/data-access/complete-paged-source.loader.ts:11),
which:

- starts at page 1 and `expand()`s sequentially until `page * pageSize >= totalCount`;
- validates every envelope: success flag, integer `page`/`pageSize`/`totalCount`, page continuity
  (`page === previous.page + 1`), stable `pageSize` and `totalCount`, and exact expected item count
  per page;
- reports raw progress `{ loaded: offset + items.length, total: totalCount }`;
- `reduce`s into one array and emits **once**, after full success. No partial result is ever published.

So a 2,000-ayah Root source today issues **20 sequential HTTP requests** and holds all 2,000 mapped
`LinkingAyah` objects (with full Uthmani word arrays) in memory.

The manual source is complete by construction (all included verses, all their pages).

**Rendering is client-paged, not virtualized.**

| Surface | Constant | Behaviour |
| --- | --- | --- |
| Source-ayah editor | `EDITOR_PAGE_SIZE = 12` ([linking-source-editor.facade.ts:15](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-editor.facade.ts:15)) | `filteredAyahs → visibleAyahs = slice(page)`; a `qd-pagination` control appears when `filteredCount > 12` |
| Review step | `REVIEW_PAGE_SIZE = 12` ([direct-link-workflow.component.ts:13](Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.ts:13)) | same slice-and-paginate |
| Manual word editor | none | renders **every** included ayah and **every** word button at once ([linking-manual-word-editor.component.html:29-65](Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-manual-word-editor/linking-manual-word-editor.component.html:29)) |

Search (`arabicSearchIncludes` over verseKey + surah name + Uthmani words) and Select All / Clear All
/ per-ayah checkbox all operate on the **complete resolved universe**, not the visible page
([linking-source-editor.facade.ts:50-59,102-121](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-editor.facade.ts:50)).
Hidden/filtered/paged-out ayahs retain their selection. So the *semantics* are already
complete-universe; only the *rendering* is paged.

### 3.3 Angular CDK Virtual Scroll — does it exist?

**Not in Linking.** `@angular/cdk ^20.2.14` is a dependency and `ScrollingModule` /
`CdkVirtualScrollViewport` are already used elsewhere in this app —
[`shared/ui/data-table/data-table.component.ts:2,79`](Frontend/quran-dashboard-ui/src/app/shared/ui/data-table/data-table.component.ts:2)
and `word-drilldown-modal.component.ts:3`. No Linking template contains a
`cdk-virtual-scroll-viewport`.

### 3.4 Locked product direction (recorded, not implemented)

> **LOCKED:** Normal source browsing may remain paginated 100-by-100. The **Linking Workspace source
> configuration surface must receive the complete resolved ayah set of a source and render it as one
> continuous Angular CDK virtualized list** — no pagination — whether the source has 10, 200, or 2,000
> ayahs. The user must be able to scroll the complete source and exclude any ayah freely.

**What would need to change later (documentation only — not done here):**

1. `linking-source-ayah-editor.component.html` — replace the `qd-linking-ayah-selection` +
   `qd-pagination` pairing with a `<cdk-virtual-scroll-viewport>` that becomes **that surface's single
   vertical scroll owner** (the current `.linking-source-ayah-editor` `overflow: auto` at
   `…scss:13` must yield to it, not nest it).
2. `linking-source-editor.facade.ts` — delete `EDITOR_PAGE_SIZE`, `page`, `setPage`, `pageCount`,
   `visibleAyahs`; expose `filteredAyahs` directly. `LinkingSourceEditorState.page` leaves the model.
3. `linking-ayah-selection.component` — accept the full list, drop the `page`/`pageSize`/
   `filteredCount`/`paginationLabel` inputs and the `pageChanged` output, and move its `<ul>` inside
   the viewport (`*cdkVirtualFor` requires a predictable item template; the current `qd-linking-ayah-card`
   has variable height because Quran text wraps, so `itemSize` needs `autosize` or a measured fixed
   row).
4. `LinkingWorkspaceHostComponent` scroll-owner audit must be re-run for the editor surface.
5. The review step (`direct-link-workflow`) is a separate decision — it is currently paged at 12 and is
   **not** part of this locked direction.

**Backend relevance:** the frontend currently reconstructs a complete source by walking the ordinary
paginated explorer API N times. A future Backend Linking/source-resolution boundary should be able to
return the **complete resolved ayah set for a source in one call**, so the Linking client stops
issuing `ceil(total/100)` requests and stops depending on cross-page envelope stability
(`totalCount`/`pageSize` must not shift mid-walk today, or the loader throws).

---

## 4. Multi-source merge and intent

### 4.1 Independent resolution

[`LinkingSourceSetCoordinator.resolve(members)`](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-set.coordinator.ts:52):

- orders members by `operationOrder` (workspace row order);
- bumps a `generation`; every progress/success/failure publication is generation-guarded;
- `forkJoin` over members → all resolve concurrently, **published atomically**. Any member error
  produces `MemberResolutionError`, `result` stays `null`, and the failing member (or all, when the
  error is not member-scoped) is marked `error`;
- cancels on access loss and on `activeSurface() === 'closed'`;
- per member: reconcile its own inclusion against its own universe → conditionally write back to the
  workspace **only** when `origin === 'workspace'` **and** the row's `configurationRevision` still
  equals the captured one → validate manual word locations → filter to included verses → apply that
  member's word configuration.

### 4.2 `verseKey` deduplication and merged display

[`mergeLinkingSources`](Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-merge.ts:11)
keys a `Map` by `verseKey`. First contributor wins for the rendered `ayah`; subsequent contributors
must pass `assertCompatibleAyahs`, which throws on:

- conflicting non-null `ayahId`, `surahNumber`, `surahNameArabic`;
- **any** difference in `pageNumber` or `ayahNumber`;
- differing non-marker word counts;
- differing non-marker word text or conflicting non-null `canonicalQuranWordId` at the same index.

`enrichAyah` then fills only `null` metadata (`ayahId`, `surahNumber`, `surahNameArabic`) from the
later contributor. Final order is numeric Quran order via `compareLinkingVerseKeys`.

> **Sharp edge:** `pageNumber` is compared with strict `!==`, not the null-tolerant `conflicts()`.
> Automatic resolvers emit the API's `match.pageNumber`; the manual reader emits `core.pageFrom`. For a
> page-spanning ayah where the automatic source reports a different page than `pageFrom`, a mixed
> manual + automatic operation over that verse throws `تعارضت بيانات الآية بين مصادر الربط.` and
> blocks the whole operation. This is a real, reachable failure mode.

### 4.3 Word-contribution union

`mergeWordSelections` unions `sourceKeys` per non-marker word position, and fills
`canonicalQuranWordId` / `wordLocation` from whichever contributor supplied a non-null value.
Alignment is **positional over the marker-filtered array**, guarded by identical `textUthmani` and
non-conflicting canonical IDs.

> **Implemented-but-unused:** `MergedAyahSelection.words` and `MergedAyahSelection.sourceKeys` are
> computed and are **consumed by nothing** — not by the review template, not by the mock port. Grep
> confirms the only consumers of `mergedSelection` are `.ayahs.length` and `.ayahs[].ayah`.
> The review renders `<qd-linking-ayah-card [ayah]="ayah.ayah">`, i.e. **the first contributing
> member's `LinkingAyah` with that member's own `isSourceMatch` flags**. A second source's matched
> words on a shared ayah are therefore **not** visually highlighted, and merged provenance is not
> displayed anywhere. The union logic exists and is correct; the presentation layer does not read it.

### 4.4 Source provenance

Provenance lives in two places: `MergedAyahSelection.sourceKeys` / `MergedLinkingWordSelection.sourceKeys`
(computed, unrendered — see above) and `LinkingSourceIntent.sourceKey` + `.source` (computed **and**
rendered as a per-source line in review:
`{{ intent.source.label }} — {{ labels.intentModes[intent.contributionMode] }}`).

### 4.5 Intent derivation

[`createLinkingSourceIntent(member, ayahs)`](Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-intents.ts:7)
runs **per member, on that member's own reconciled ayahs**, never on the merged display:

| Configuration | `contributionMode` | Units |
| --- | --- | --- |
| automatic | `automatic` | one singleton unit per included ayah |
| manual, 1 included ayah | `manual-single` | one singleton unit |
| manual, ≥2 included, preference `independent` | `manual-independent` | one singleton unit per ayah |
| manual, ≥2 included, preference `grouped` | `manual-grouped` | **exactly one** unit containing all ayahs in Quran order |

### 4.6 Invariant check — "merged display does not define link intent"

**Confirmed preserved.** Trace of the stated example:

- Manual Mushaf `{A, B}`, `linkShape: 'grouped'`, both included → `effectiveManualLinkShape = 'grouped'`
  → `units = [{ ayahs: [A, B] }]`, mode `manual-grouped`.
- Lemma source resolving `{A, C}`, automatic → `units = [{ayahs:[A]}, {ayahs:[C]}]`, mode `automatic`.
- `mergeLinkingSources` produces `ayahs = [A, B, C]` — A once, keyed by `verseKey`, with
  `sourceKeys = [manualKey, lemmaKey]`.
- `sourceIntents = [manualIntent, lemmaIntent]` — two independent records; neither is derived from,
  nor reconstructible from, the merged list.
- Both travel to the command boundary as required siblings:
  `LinkingSourceSetOperationResult = { mergedSelection, sourceIntents }`
  ([linking-workflow.models.ts:24](Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workflow.models.ts:24)),
  and `MockLinkingCommandPort` rejects the command when **either** is empty.

The invariant holds at the model, derivation, and command-boundary levels.

### 4.7 Edge behaviour actually implemented vs. specified

| Edge case | Implemented? |
| --- | --- |
| Same ayah in two sources → one merged entry, both source keys recorded | ✅ |
| Same word matched twice → one entry, unioned `sourceKeys` | ✅ computed / ❌ not rendered |
| One source ON, one OFF → OFF contributes ayah, no words | ✅ |
| One source excludes an ayah another includes → ayah retained | ✅ |
| Equivalent source added twice → one prepared row | ✅ |
| One resolver fails → no partial confirm, retry available | ✅ (retry restarts the whole set) |
| Checked source contributing **zero** ayahs → visible warning | ❌ `contributedAyahCount` is tracked but **never rendered**; `canContinue` is computed but **never consumed** |
| All sources contribute zero → block Door/review | ⚠️ partially — `canSubmit` blocks confirmation, but the flow still advances to the Door step |
| No rows checked → start action disabled | ✅ (`[disabled]="selectedCount() === 0"`) |

---

## 5. Quran word identity

| Identity | Source of truth today | Classification |
| --- | --- | --- |
| **Canonical `quranWordId`** | Unique Word (`words[].quranWordId`) and Word Type (`words[].quranWordId`) reads only | **Backend-supplied. Trustworthy.** |
| Root / Lemma / Stem matched word | boolean `word.isMatched` on an ordered complete ayah; `canonicalQuranWordId` explicitly `null` | **No canonical identity exists on this path.** |
| **Manual `wordLocation`** | `MushafWordDto.wordLocation`, format `^\d{1,3}:\d{1,3}:\d{1,2}$`, validated against the proven complete ayah | **Prototype coordinate. Not a database ID.** |
| **`renderPosition`** | automatic: 0-based array index **including** ayah markers. manual: 1-based `wordNumber` for non-markers, `0` for markers | **Presentation-only. Semantics differ per family.** |
| Presentation-occurrence intent | `{ identity: 'presentation-occurrence', verseKey, renderPosition }` | **Frontend-only alignment. Must be resolved/validated by Backend.** |

`LinkingWordContribution` ([linking-merge.models.ts:40](Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-merge.models.ts:40))
is an explicit three-way discriminated union — the code never lets a `wordLocation` or a
`renderPosition` masquerade as a canonical ID. Selection order in
[`wordContributionsFor`](Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-intents.ts:33):
manual + non-null `wordLocation` → `manual-word-location`; else non-null canonical → `canonical-quran-word-id`;
else → `presentation-occurrence`.

**Frontend-prototype identity that a Backend must later resolve or reject:**

1. `presentation-occurrence` — every Root/Lemma/Stem matched word arrives this way. `renderPosition`
   for those families is an index into a marker-free array, which is not a durable coordinate.
2. `manual-word-location` — needs canonical resolution to a `QuranWord` row, with validation that the
   location is non-marker, belongs to its declared `verseKey`, and sits inside an included ayah.
3. `ayahId` — nullable across families (Word Type resolver hardcodes `ayahId: null`; the manual reader
   also emits `null`). Only Unique Word / Root / Lemma / Stem supply it.
4. Every persisted `verseKey` — `isVerseKey` is structural only (surah 1–114, ayah 1–286); it accepts
   impossible coordinates such as `114:200`. Membership is proven today only by a successful source
   read.

No future API design is proposed here.

---

## 6. Current Door flow

Steps ([linking-workflow.facade.ts:20](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts:20)):
`configure-source` (Direct Link only) → `resolve` → `door` → `review` → `submitting` → `success | error`.

- **Door picker:** `qd-abwab-door-picker` fed `snapshot()?.liveRoots ?? []` with `single=true`
  ([linking-door-step.component.html](Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-door-step/linking-door-step.component.html)).
  The Abwab snapshot is loaded once per operation when resolution succeeds.
- **Selection validation:** `selectDoor` only accepts an ID found under `liveRoots`
  ([facade:172,248](Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts:248)); it is select-only and cannot toggle back to `null`. Archived doors live in
  `snapshot.archivedRoots` / `byId` and are not reachable.
- **Review:** shows the merged ayah count, one line per source intent (label + Arabic contribution
  mode), and 12 merged ayah cards per client page.

**Confirmations requested:**

| Claim | Verified |
| --- | --- |
| The final operation is still mock-only | ✅ `MockLinkingCommandPort.execute` returns `of({kind:'linked', message:'تم الربط بنجاح'})` |
| No real link is written | ✅ no `HttpClient` injection, no request, no cache invalidation, no store mutation |
| No Backend linking endpoint exists | ✅ nothing in the frontend calls any linking write path; `LINKING_COMMAND_PORT` resolves to the mock by default factory |
| No real audit/history/request is produced | ✅ no history, no durable ID, no group ID, no approval, no notification |

The mock re-validates: Owner access, Door membership in `liveRoots`, and non-empty
`mergedSelection.ayahs` **and** `sourceIntents`. Success is terminal — the footer renders no action at
`success`; only the notice's dismiss (`acknowledgeSuccess`) is available, which clears transient
checked membership for a workspace-origin operation and returns to the workspace.

---

## 7. New product requirement — per-source / per-ayah descriptions  *(NOT implemented)*

### 7.1 The requirement

For every **included ayah contribution inside a source**, the user must eventually be able to attach
**one or more** linking descriptions / reasons that are specific to *that ayah within that source*.

```
Source: Root X
  Ayah A → selected words [...]  + description 1, description 2
  Ayah B → selected words [...]  + its own descriptions

Source: Lemma Y
  Ayah A → its own, different descriptions
```

Rules:

- descriptions belong to the **source's ayah contribution**, not to the ayah;
- they must **not** be merged merely because the review display deduplicates the ayah visually;
- multiple descriptions per (source, ayah) must be possible.

### 7.2 Current state

**Nothing exists.** Grep across `features/linking/` finds no `description`, `reason`, `note`, `وصف`,
`سبب`, or `تعليل` concept. The only `description`-named symbols are the accessible-name helper
`sourceDescription` in `quran-source-linking-actions.component.ts` and the label
`highlightDescription` — neither is related.

### 7.3 Where the frontend would need to extend (identification only, no design)

| Location | Extension needed |
| --- | --- |
| `models/linking-workspace.models.ts` → `LinkingSourceConfiguration` | Add a per-verse collection to **both** union arms, structurally parallel to the manual `wordLocationsByVerseKey`, e.g. `descriptionsByVerseKey: Readonly<Record<string, readonly …[]>>`. It must sit on the **configuration**, so it is inherently source-owned. |
| `models/linking-workspace.models.ts` → `LinkingWorkspacePersistedItem` | Nothing new — it already persists the whole `configuration`. |
| `data-access/linking-workspace.codec.ts` | New validator + normaliser beside `isManualWordLocationsByVerseKey` / `normalizeWordLocations`; new bounds beside `MAX_WORD_LOCATIONS_PER_AYAH` / `MAX_STRING_LENGTH`; verse keys must be constrained to the source's own universe the way manual keys already are at `:123-128`. |
| `state/linking-workspace.store.ts` | New revision-bumping mutators beside `setManualWordLocations` / `setAutomaticWordMatchesEnabled`; reuse `updateConfiguration` so `configurationRevision` still guards write-back. |
| `state/linking-source-editor.facade.ts` + `components/linking-source-ayah-editor` + `components/linking-ayah-selection` | The per-ayah row is the natural editing site. **This collides directly with §3.4** — the same row template becomes a CDK virtual item, so descriptions and virtualization must be designed together (variable-height rows). |
| `models/linking-merge.models.ts` → `LinkingIntentAyah` | Add the descriptions to the **intent** ayah, alongside `wordContributions`. This is the transport that already carries source-owned per-ayah data. |
| `utils/linking-source-intents.ts` → `wordContributionsFor` sibling | Read the member's own configuration per verse — per-member, so two sources contributing the same verse keep separate descriptions by construction. |
| `models/linking-merge.models.ts` → `MergedAyahSelection` | **Must NOT gain a merged description field.** Merged display deduplicates; descriptions must never be unioned there. |
| `components/direct-link-workflow` review | Needs a per-source, per-ayah presentation; the current review renders one flat merged card list with a separate intent summary, so this is a genuine UI addition. |

The existing architecture is well-shaped for this: source-owned per-ayah data already has a working
precedent (`wordLocationsByVerseKey` → `LinkingIntentAyah.wordContributions`), and the
merged-display-vs-intent separation already guarantees non-merging.

No database design is proposed here.

---

## 8. Current persistence boundary

### 8.1 Implementation

Three separated responsibilities, exactly as planned:

1. **Pure codec** — [`linking-workspace.codec.ts`](Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-workspace.codec.ts): `encodeLinkingWorkspace` / `decodeLinkingWorkspace`, no storage API.
2. **Port** — [`linking-workspace.repository.ts`](Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-workspace.repository.ts): `load(actorSub)`, `save(actorSub, revision, items)`, `invalidateActiveActor(actorSub)` — all `Promise`-returning, i.e. async-ready.
3. **Adapter** — [`local-storage-linking-workspace.repository.ts`](Frontend/quran-dashboard-ui/src/app/features/linking/data-access/local-storage-linking-workspace.repository.ts): three `localStorage` calls, nothing else.

Components talk only to `LinkingWorkspaceStore`; the store is the only holder of the repository
reference.

Decoding is strict:

- envelope-fatal → `{ items: [], invalidPayload: true }` on unparseable JSON, `version !== 2`,
  `actorSub` mismatch, over-long `actorSub`, non-integer `revision`, or non-array `items`;
- per-item: descriptor must pass `isLinkingSourceDescriptor`; manual sources are normalised
  (deduplicated, numerically Quran-ordered, first occurrence per verse wins); descriptor strings are
  length-bounded; `configuration` kind must match the source kind; manual `ayahInclusion` keys and
  `wordLocationsByVerseKey` keys must be inside the descriptor's own verse set; selections are
  normalised; `lastResolvedCount` must be `null` or a bounded non-negative integer;
- `sourceKey` is **recomputed** from the descriptor, never trusted;
- duplicate keys keep the first valid occurrence;
- bounds: 100 items, 10,000 selection keys, 500 word locations per ayah, 1,000-char strings,
  1,000,000 max count.

### 8.2 Why this remains temporary prototype persistence

1. **No security boundary.** Any same-origin script reads and writes `localStorage`. The actor binding
   prevents accidental cross-actor display, not tampering. The store's own comment-free contract
   assumes a hostile-free client.
2. **The client authors the truth.** Descriptors, inclusion sets, word coordinates, and link shape are
   written by the browser and read back by the browser. Nothing server-side ever validated that this
   Owner may link this source, that the verse keys exist, or that the word locations are real.
3. **Device-local only.** No cross-device synchronisation, no server revision, no conflict resolution.
4. **Last-writer-wins across tabs.** No `storage` event listener, no locking; `durableWorkspaceRevision`
   only serialises writes *within one tab*.
5. **`lastResolvedCount` is explicitly untrusted** — always re-marked stale on decode and never used
   as source truth.
6. **Quota / denial is survivable but silent-ish** — a failure sets one Arabic warning signal and keeps
   the in-memory workspace usable, meaning a user can work for a whole session with nothing persisted.
7. **No history.** There is no record of when a source was prepared, by whom, or what changed.

### 8.3 What would move to server-side per-user persistence

- ordered prepared sources for an actor (the workspace itself);
- each source's typed descriptor and complete scope, with server-validated identity;
- per-source ayah inclusion;
- per-source automatic word-match preference;
- per-source manual word selections (as canonical word references once resolved);
- per-source manual grouped/independent preference;
- **(future)** per-source per-ayah descriptions (§7);
- authoritative resolved counts.

What should **stay** client-side: checked operation membership, surface/editor/focus state, search
queries, client page positions, load/progress/error state, the merged display, and the selected Door
before submission.

No schema is proposed here.

---

## 9. V1 retirement / remaining transitional code

| V1 assumption | Status | Evidence |
| --- | --- | --- |
| Scalar one-source workflow state and command | **Removed** | `LinkingWorkflowState.members: readonly LinkingOperationMember[]`; `LinkingCommand = { doorId, operation: {mergedSelection, sourceIntents} }` |
| Selected-Mushaf-word Linking source (`kind: 'mushaf-word'`) | **Removed** | no `'mushaf-word'` string anywhere; `selected-word-section` contains no Linking import |
| `MushafWordLinkingSourceResolver` | **Removed** | deleted in `9d637bfb`; not in `resolvers/` |
| `state/linking-workspace-session.ts` (sessionStorage) | **Removed** | deleted in `9d637bfb`; no `sessionStorage` reference in `features/linking/` |
| V1 storage key `qd-linking-workspace-v1` | **Removed, never migrated** | only `qd-linking-workspace:v2:` exists; the README states V1 is neither read nor migrated |
| `LinkingWorkspaceItemComponent` (large cards) | **Removed** | deleted in `ee1dc680`, replaced by `linking-workspace-source-row` on `qdResultItem` |
| Scalar `highlightSourceWords` on the workspace item | **Removed** | replaced by per-source `automaticWordMatchesEnabled` / manual word locations |
| `resultCount` field | **Renamed** | now `lastResolvedCount` + `lastResolvedCountIsStale` |
| Scalar `activeSourceKey` | **Removed** | replaced by `activeSurface` + `editorSourceKey` |
| Inert `تعديل اختيار الآيات` action (addOrFocus no-op) | **Removed** | the count button now calls `openAyahEditor` and a real editor surface exists |
| Door validation against `snapshot.byId` (archived reachable) | **Removed** | both `selectDoor` and the mock validate against `liveRoots` |
| **Client paging in the workspace source editor** | **Still present — transitional** | `EDITOR_PAGE_SIZE = 12`. Explicitly a temporary workspace solution; §3.4 locks CDK virtualization as the replacement |
| Client paging in the review step | **Still present — intentional for now** | `REVIEW_PAGE_SIZE = 12`; not covered by the locked direction |
| `LinkingSelection` `all-except` / `only` model | **Required, retained** | reused per prepared source; correct and load-bearing |
| `CompletePagedSourceLoader` | **Required, retained** | but see §3.4 / §10 — Backend complete-resolution would make it removable |
| `MergedAyahSelection.words` / `.sourceKeys` | **Present, unconsumed** | computed union with provenance that no UI reads — cleanup candidate *or* the missing render work |
| `LinkingSourceSetCoordinator.canContinue` | **Present, unconsumed** | dead public API |
| `LinkingOperationMemberLoadState.contributedAyahCount` | **Present, unconsumed** | the "non-contributing source" warning was never rendered |
| Dead V1 labels: `reviewIndependent`, `reviewHighlight`, `reviewTarget`, `editSelection`, `sourceReady` | **Present, unconsumed — cleanup candidates** | `reviewIndependent` is literally the V1 copy *"كل آية ستُربط بالباب بصورة مستقلة."* that contradicts grouped manual intent. It is not rendered, but it is still in `linking.labels.ts:64` |
| Old highlight/global-selection semantics | **Removed** | there is no workflow-global highlight step; `LinkingAyahCardComponent.highlightSourceWords` is a per-render input, hardcoded `true` at the review site |
| `mushaf-reader-session.ts` sessionStorage | **Unrelated, required** | Mushaf reader URL/session state, never Linking |
| README claim of "temporary compatibility selectors" (`features/linking/README.md:21-22`) | **Stale documentation** | no compatibility adapter remains in the code; the README sentence outlived the migration |

**Verdict on V1 retirement: complete.** No V1 model, key, resolver, session file, or scalar command
path survives. The residue is dead labels, three unconsumed public members, and one stale README
sentence.

---

## 10. Frontend readiness for Backend integration

### 10.1 Stable product behaviour — safe to design Backend against

1. **Owner-only, fail-closed, re-checked at every mutation.** Authorization is a server concern, but
   the frontend's actor model (`sub`-bound) is settled.
2. **The prepared-source model.** `sourceKey + descriptor + configuration` per row, ordered, idempotent
   by key. Stable and correct.
3. **Six source families with stable identities.** Automatic keys unchanged since V1; manual identity =
   normalised verse set. These are the identities a Backend must be able to store and re-resolve.
4. **Three orthogonal selection levels.** Prepared vs checked vs configured never infer one another.
   Verified in code.
5. **Complete-universe selection semantics.** Search, Select All, Clear All, and reconciliation all
   operate on the full resolved set, not the visible page.
6. **Atomic multi-source operations.** All-or-nothing publication with generation guards; no partial
   confirmation is possible.
7. **`verseKey` as the ayah merge identity.** Universal across every resolver and the Mushaf DTO.
8. **The merged-display / source-intent sibling contract.** This is the single most important stable
   output for the Backend: `{ mergedSelection, sourceIntents }`, where intents are per-source ordered
   nested ayah units that the display can never reconstruct.
9. **Manual grouped/independent intent with a derived effective shape.** Preference persists through a
   temporarily-single inclusion.
10. **Manual completeness proof.** A manual ayah is publishable only after a 7-point structural proof.
11. **The command boundary.** `LinkingCommandPort` is an injected token with an Observable-returning
    `execute(command)`; swapping the mock for an HTTP adapter is a one-provider change.
12. **The persistence port.** `LinkingWorkspaceRepository` is async-shaped; a server adapter replaces
    the localStorage adapter without touching components, editors, selection helpers, or merge logic.

### 10.2 Temporary frontend implementation workarounds — do NOT treat as product requirements

| Workaround | Nature |
| --- | --- |
| `localStorage` workspace persistence | UX emulator; §8 |
| Client paging at 12 in the source-ayah editor | To be replaced by CDK virtualization; §3.4 |
| Walking `ceil(total/100)` explorer pages to build one source | Backend complete-resolution should replace it |
| `presentation-occurrence` word identity for Root/Lemma/Stem | Frontend alignment only |
| `wordLocation` as the manual word coordinate | Prototype coordinate, not a canonical ID |
| `sourceKey = 'ephemeral:<kind>'` for Direct Link | In-memory placeholder |
| `MockLinkingCommandPort` | Presentation-only |
| Direct Link having no ayah-inclusion editor | Implementation shortfall vs. plan §12.2 |
| Manual word editor rendering every ayah's every word at once | Contradicts report §10.5's chooser design; will not scale to a large manual set |
| Merged word provenance computed but unrendered | Missing render work, not a product decision |

### 10.3 Where Backend support would simplify the frontend

1. **Complete source resolution in one call.** Removes `CompletePagedSourceLoader`'s 20-request walks,
   its cross-page envelope-stability requirement, and its raw-progress-vs-unique-count ambiguity.
   Directly enables the locked CDK-virtualized workspace list.
2. **Canonical `quranWordId` on every matched word** (or a batch `wordLocation → quranWordId`
   resolution). Eliminates `presentation-occurrence` entirely and removes the positional word-alignment
   guard in `linking-merge.ts`.
3. **Consistent, non-null ayah metadata** (`ayahId`, `surahNumber`, `surahNameArabic`, and a stable page
   convention). Removes `enrichAyah`, `assertCompatibleAyahs`, and the `pageNumber` strict-equality
   failure mode in §4.2.
4. **Server-side workspace persistence.** Removes the codec's entire defensive-validation surface and
   the actor-generation/save-queue machinery.
5. **Server-issued durable link/group identity.** Removes the frontend's need to carry grouped intent
   as an explicit nested structure "until a backend supplies its own representation".
6. **Server-validated verse membership.** Makes the structural-only `isVerseKey` guard sufficient at
   the edge instead of requiring a successful read to prove membership.

---

## 11. Deferred read-side behaviour  *(explicit)*

> **Do not design or implement the GET API for retrieving existing links of a Door yet.**

Sequence agreed:

1. First add and validate the **frontend presentation of a Door's existing links**.
2. Then decide what read model / API that UI actually needs.
3. The upcoming Backend report focuses on the **write/storage model and source-resolution support** —
   **not** the final Door-links read API.

Current state consistent with this: nothing in the frontend reads existing links. There is no
Door-links query, no link list component, no link cache key, and no read contract for links. The Abwab
snapshot read (`AbwabApi.getTree`, ETag-conditional) is the tree only; it carries no link data.

---

# Final report structure

## 1. Current implementation verdict

**The V2 frontend prototype is implemented and coherent. It is ready to drive Backend/database
architecture design for the write/storage and source-resolution boundary.**

The V1→V2 reshape is complete: scalar workflow, the Mushaf selected-word source, sessionStorage, the
V1 storage key, and the large card UI are all gone with no compatibility shim. The four hardest
product contracts — three orthogonal selection levels, complete-universe per-source configuration,
atomic multi-source resolution, and the merged-display / source-intent sibling separation — are
implemented correctly in code, not just described in the plan.

The implementation is a faithful prototype, with three honest caveats: workspace source rendering is
still client-paged rather than virtualized; merged word provenance is computed but not displayed; and
the entire persistence and command layer is deliberately local and mock-only.

## 2. What is already production-shape UX

- Owner-only fail-closed access, re-checked at every mutation and at mock confirmation.
- Global Navbar entry with a prepared-source count; one primary Linking shell at 80vw × 88dvh with
  five surface states and exactly one vertical scroll owner per surface.
- Prepared-source workspace: dense `qdResultList` rows, idempotent add, per-row ayah-count editor
  entry, per-row word behaviour control, isolated danger remove with a working single-item Undo, and a
  confirmed remove-all.
- Three orthogonal selection levels with the invariants intact.
- Source-ayah editor: complete one-source load, raw progress, local Arabic search, Select All / Clear
  All over the full universe, retained selection for hidden rows, revision-guarded reconciliation
  write-back, retry, and controlled error states.
- Manual Mushaf entry: Owner-only `تحديد` header toggle with `aria-pressed`, per-word accessible
  select/deselect names, draft surviving page navigation, a persistent status/actions owner outside
  the page-area load conditional, and a handoff that neither opens the workspace nor steals focus.
- Manual word editor with a revision-guarded atomic draft save and an explicit "changes are not saved
  until confirmed" hint.
- Grouped/independent manual intent with a derived effective shape that never erases the preference.
- Atomic multi-source resolution with per-member progress/error and no partial publication.
- Door step restricted to `liveRoots`, select-only, validated at selection and at confirmation.
- Focus coordination across entry, surface swaps, editor return, and retained-entity-overlay
  restoration.

## 3. What is still prototype-only

- **Persistence:** actor-bound `localStorage`. No server, no security, no cross-device, no history,
  last-writer-wins across tabs.
- **Completion:** `MockLinkingCommandPort` — no HTTP, no link, no group, no durable ID, no audit, no
  request, no approval, no cache invalidation.
- **Word identity:** `presentation-occurrence` (Root/Lemma/Stem) and `manual-word-location` are
  frontend coordinates. Only Unique Word and Word Type carry canonical `quranWordId`.
- **Source loading:** N sequential explorer page requests reconstruct one source; the client holds the
  whole set in memory.
- **Source rendering in the workspace:** client-paged at 12 rather than CDK-virtualized.
- **Merged provenance:** `MergedAyahSelection.words` / `.sourceKeys` computed, never rendered.
- **Direct Link:** no ayah-inclusion editor; ephemeral `sourceKey`.
- **Manual word editor:** renders all included ayahs and all their words at once.
- **Verse-key validation:** structural only (`114:200` passes the guard).

## 4. Confirmed gaps before Backend integration

| # | Gap | Where |
| --- | --- | --- |
| G1 | **Per-source / per-ayah descriptions do not exist at all** — the new product requirement | §7 |
| G2 | Workspace source configuration is client-paged, not CDK-virtualized over the complete set | §3.3–3.4 |
| G3 | No complete-source-resolution read path; the client walks paginated explorer APIs | §3.2 |
| G4 | Merged word union and source provenance are computed but never rendered; the review shows only the first contributor's highlights | §4.3 |
| G5 | Zero-contribution warning and `canContinue` are modelled but never surfaced; the flow can reach the Door step with an empty merge | §4.7 |
| G6 | `pageNumber` strict equality in `assertCompatibleAyahs` can hard-block a mixed manual + automatic operation on a page-spanning ayah | §4.2 |
| G7 | Review omits the target Door and the source contribution status (`reviewTarget` label is dead) | §6, §9 |
| G8 | Direct Link cannot edit ayah inclusion | §1.11 |
| G9 | Manual word editor does not scale — no chooser, no lazy per-ayah loading | §3.2 |
| G10 | No canonical identity for Root/Lemma/Stem matched words, and none for manual words | §5 |
| G11 | `isVerseKey` accepts impossible per-surah coordinates; membership is proven only by a successful read | §5 |
| G12 | Dead V1 labels and three unconsumed public members remain; `features/linking/README.md:21-22` still claims compatibility selectors that no longer exist | §9 |

## 5. Backend-relevant requirements extracted from the frontend

*(Requirements only. No schema, endpoint, migration, or entity design.)*

**Storage / write model**

1. An authenticated actor's **ordered, persistent workspace** of prepared sources.
2. **Stable typed source identity** for six families, preserving each family's complete scope:
   Unique Word (`mode`, `wordId`), Root (`rootId`), Lemma (`lemmaId`, `typeCode`), Stem (`stemId`,
   `typeCode`), Word Type (selection discriminant + `type`/`childCode`/`case`/`tense`/`voice`), and
   Manual Mushaf (a normalised, numerically ordered, de-duplicated `verseKey` set).
3. **Per-source ayah inclusion**, expressible as the current `all-except` / `only` override model or an
   equivalent, and reconcilable against a re-resolved universe.
4. **Per-source automatic word-match preference** (boolean).
5. **Per-source manual word selections**, verse-scoped, resolved to canonical words.
6. **Per-source manual grouped/independent preference**, stored independently of the current inclusion
   count.
7. **Per-source per-ayah descriptions** — one or more per (source, ayah), never merged across sources
   even when the display deduplicates the ayah. *(§7 — new requirement.)*
8. **One operation containing one or several selected sources**, with a captured immutable snapshot per
   member.
9. **Non-flattenable link intent:** automatic sources produce one unit per ayah; a manual source
   produces either one unit per ayah or exactly one unit containing all its included ayahs. The merged
   display must never be able to reconstruct, widen, or flatten this.
10. **One validated currently-live Door** as the operation target.
11. **Server-issued durable link / group identity** — the frontend fabricates none.
12. **Server-side authorization** independent of frontend visibility, re-checked at write.
13. **Concurrency / revision behaviour** for a workspace reachable from multiple tabs and devices.

**Source-resolution support**

14. **Complete resolved ayah set for a source in one boundary call**, so the Linking client stops
    reconstructing it from ordinary paginated explorer APIs. This is the single highest-value Backend
    capability for the frontend.
15. **Canonical `quranWordId` for every matched word**, or a batch resolution of submitted
    `wordLocation` coordinates. Manual-location resolution must validate that each location is
    non-marker, belongs to its declared `verseKey`, sits inside an included ayah, and retains
    source/ayah provenance — it must not return an unscoped flat ID set.
16. **Consistent non-null ayah metadata** (`ayahId`, surah number/name) and a stable page convention for
    page-spanning ayahs.
17. **Server-validated verse membership**, so structural verse-key syntax is sufficient at the client
    edge.
18. **Clear validation / error reporting** when a source, verse, word coordinate, or Door becomes
    invalid, distinguishable per member so the client can attribute and retry.

## 6. Questions that genuinely require a database/backend architecture decision

*(Listed, deliberately unanswered.)*

1. **Grouped-vs-automatic in one mixed operation.** When a grouped manual source contributes `{A,B}`
   and an automatic source independently contributes `{A,C}`, how does a grouped unit partition durable
   links, and what happens to the shared ayah A?
2. **Canonical manual-word resolution contract.** Return `quranWordId` inside complete ayah/page reads,
   or batch-resolve submitted `wordLocation` values at write time?
3. **Root / Lemma / Stem matched-word identity.** Should these reads start returning canonical word IDs,
   or should the write model accept a source-scoped match assertion without per-word IDs?
4. **Description ownership and cardinality.** Is a description an attribute of the (source, ayah)
   contribution, of the resulting link, or a first-class annotation entity? How many, how long, and are
   they editable/versioned after the link is written?
5. **Source-descriptor storage form.** Store the typed descriptor and re-resolve on read, or materialise
   the resolved ayah set at write time — and what happens when Quran source data or a morphology scope
   later changes?
6. **Workspace persistence granularity and concurrency.** Whole-workspace revision vs per-source
   revision; last-writer-wins vs optimistic-concurrency rejection; cross-device merge semantics.
7. **Idempotency and re-linking.** What happens when the same source + Door operation is confirmed
   twice? Is a link unique per (Door, ayah), per (Door, source, ayah), or per intent unit?
8. **Complete source resolution shape.** One unbounded response, a streamed/chunked response, or a
   cached resolved-set identity the client can revalidate — and what its cost ceiling is for a 2,000-ayah
   source.
9. **Audit / history model.** What is recorded at write time, and does it belong to the operation, the
   link, or the workspace?
10. **Authorization scope.** Is Linking Owner-only server-side, and does source-level or Door-level
    permission exist?

---

*End of report. No Backend implementation plan, database design, API design, migration, or test is
included by design.*
