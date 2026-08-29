# Quran Phrase Similarity Ayah Linking — Implementation Plan

## Document control

- Status: planned; implementation has not started.
- Working branch: `unified-ayah-linking`.
- Planning style: feature-scoped implementation plan; no Spec Kit artifacts are required.
- Primary surface: Quran phrase similarity search.
- Route: `/dashboard/words/phrases/similarity`.
- Lifecycle: delete this feature folder in the final pure-deletion commit after the owner-requested engineering review passes and before merge, following `docs/README.md`.

## Objective

Extend the existing phrase-similarity results page so a researcher can select any subset of the
result ayahs, or select the complete result set across all pages, then either add that selection to
the linking workspace or start direct linking.

The created source must:

1. contain every selected ayah exactly once;
2. preselect every Quran word that belongs to a qualifying similarity phrase window in that ayah;
3. include both currently green matched words and red differing words;
4. default to independent ayah links;
5. use the existing manual-Mushaf linking workflow;
6. allow the researcher to choose any target door.

This feature is a linking integration for an existing read surface. It is not a new similarity
algorithm, a new linking source kind, or a new Abwab classification system.

## Locked owner decisions

These requirements are authoritative for this feature.

### Door semantics

- “Mutashabihat doors benefit from this page” is a product-use description only.
- The linking workflow must not filter, tag, or restrict the available door tree.
- The researcher may choose any door already exposed by the normal linking workflow.
- Do not add a `mutashabihat` door type, door flag, relation, schema field, or picker filter.

### Selected ayahs

- Every result ayah can be selected individually.
- A select-all action selects the complete similarity result set, not only the current page.
- Selecting all and then deselecting individual ayahs is supported.
- Selection persists while paging or changing result sort within the same exact result population.
- Selection resets when the resolved query, active phrase build, or similarity threshold changes.
- Linking actions are disabled while zero ayahs are selected.
- The default link shape is `independent`.

### Row interaction

- Each standard and compact result row has a checkbox.
- Clicking any non-interactive area of a row toggles that ayah.
- Clicking the checkbox toggles once and must not trigger a second row toggle.
- Clicking the Quran ayah text preserves the current Mushaf navigation behavior and must not
  change selection.
- Keyboard Enter/Space on the selectable row toggles selection when focus is on the row itself.

### Selected words

- Both matched and differing phrase words are preselected for linking.
- The canonical selected-word set for an ayah is the union of all qualifying phrase-window word
  IDs in that ayah.
- `PhraseQuranWordIds` is the authoritative complete set; matched/differing arrays remain display
  roles and must not be treated as separate link types.
- If one ayah contains multiple qualifying similarity occurrences, it remains one ayah source row
  and all qualifying occurrence-word IDs are unioned.
- Word IDs must come from canonical backend data and must be validated to belong to their ayah.

### Linking actions and source

- Support both `إضافة للربط` and `ربط مباشر`.
- Both actions must resolve the same complete selection and construct the same source launch.
- Source kind: `manual-mushaf-ayahs`.
- Visible source label: `متشابهات العبارة «{resolved query display text}»`.
- Source `contextKey`: `null`, preserving the existing exact-selected-ayah-set duplicate identity
  used by manual research sources.
- Initial configuration:
  - `inclusionMode = all-except`;
  - `ayahOverrideIds = []`;
  - `selectedWords = every resolved similarity phrase word`;
  - `automaticWordMatchesEnabled = null`;
  - `manualLinkShape = independent`;
  - `descriptions = []`.

## Current implementation evidence

The executor must inspect these files before editing and treat code as implementation truth.

### Similarity result data is already sufficient for visible pages

- `PhraseSimilarityAyahDto` already contains one unique ayah row with canonical words, page and
  verse identity, occurrence count, and aggregated highlights.
- `PhraseSimilarityHighlightsDto` already contains:
  - `PhraseQuranWordIds`;
  - `MatchedQuranWordIds`;
  - `DifferingQuranWordIds`.
- `EfPhraseSimilarityReader.CreateAyah` already aggregates multiple qualifying occurrences by
  ayah and unions their phrase, matched, and differing Quran word IDs.
- It removes a differing role when the same Quran word is matched by another qualifying
  occurrence. This affects display color only; `PhraseQuranWordIds` remains the complete linking
  selection.
- The result API pages unique ayahs and currently allows at most 100 rows per page.

### Similarity linking is not implemented

- `phrase-similarity-list` renders rows and Mushaf links but has no checkbox column or selection
  state.
- `phrase-similarity-page` exposes no linking actions.
- `PhraseSimilarityApi` exposes only the paged search request.
- The backend has no similarity linking-selection resolver endpoint.

### Existing reusable linking foundation

- `manual-mushaf-ayahs` already supports a list of verse keys and an optional context key.
- `LinkingSourceLaunch` already supports initial selected Quran word IDs and independent/grouped
  shape.
- `PhraseContextAyahSelectionStore` already proves the required compact `only` / `all-except`
  selection semantics across pages.
- Context linking already proves the required asynchronous flow:
  - compact resolver endpoint;
  - request fencing;
  - complete source mapper;
  - workspace and direct-link actions;
  - controlled Arabic failure handling.
- Repetition and context linking launch mappers already demonstrate fail-closed manual-source
  construction from canonical ayah and word IDs.

## Non-goals

Do not expand this feature into any of the following:

- changing similarity scoring, thresholds, Hamming comparison, ranking, or occurrence grouping;
- changing the green/red display-role rules;
- editing Quran text, word boundaries, glyphs, markers, or source resources;
- creating a new linking source kind;
- creating or filtering a special class of Abwab doors;
- adding Abwab similarity relations;
- adding descriptions automatically;
- changing phrase context, phrase repetition, Mushaf mutashabihat, or nearby-ayah linking behavior;
- database schema or migration changes;
- saving similarity selections across different result populations or browser sessions;
- creating new permanent automated tests without explicit owner approval;
- redesigning the similarity page beyond controls required for selection and linking.

## Result and selection identity

Define a **similarity result population** by:

```text
active phrase build
resolved query reference
minimum matched word count
```

The following values do not change population membership and must not reset ayah selection:

```text
sort
page
page size
```

The frontend selection key must include the population identity and exclude sort/page. The backend
resolver must receive the resolution reference and minimum matched word count, then recompute the
same qualifying result population under the active build snapshot.

Selection representation:

```text
mode = only | all-except
ayahIds = selected IDs when mode=only
ayahIds = excluded IDs when mode=all-except
```

Do not store every selected ayah ID after select-all.

## Backend contract design

### Endpoint

Add a focused read-only action endpoint:

```text
POST /api/quran/phrase-search/similarities/linking-selection
```

POST is required because the compact selection body may contain many included or excluded ayah
IDs. This endpoint performs no linking mutation.

Suggested request body:

```json
{
  "resolutionRef": "...",
  "minimumMatchedWords": 3,
  "selectionMode": "only",
  "ayahIds": [10, 14]
}
```

Suggested response:

```text
activeBuildId
query:
  variantId
  displayText
  wordCount
selectedAyahCount
ayahs[]:
  ayahId
  verseKey
  pageNumber
  selectedQuranWordIds[]
```

The response query identity lets the frontend validate and label the launch from the server-owned
resolved phrase rather than reconstructing Quran text.

### Controller and handler requirements

- Protect the endpoint with the same Owner requirement used by context linking selection.
- Use the standard `ApiResponse<T>` envelope.
- Use the phrase-search compute rate limit and timeout policies.
- Map malformed bodies to the existing controlled invalid-selection response.
- Decode and validate `resolutionRef` using the existing reference codec.
- Validate `minimumMatchedWords` using the exact same rules as similarity search.
- Parse only `only` and `all-except` selection modes.
- Reject duplicate, non-positive, or unknown submitted ayah IDs.
- Return build-changed conflict when the resolution belongs to a stale phrase build.
- Return unavailable using the current phrase-search error contract.
- Keep controller logic thin; selection and phrase-read logic belong in Application and
  Infrastructure.

### Reader requirements

Add a focused reader operation rather than repeatedly calling the paged public search endpoint.

The operation must:

1. resolve the active build and anchor phrase from the opaque resolution reference;
2. reproduce the same qualifying occurrence population as the existing similarity search;
3. derive the complete unique-ayah population;
4. validate submitted include/exclude IDs against that population;
5. select the final ayah population using `only` or `all-except` semantics;
6. reject a selection that resolves to zero ayahs;
7. load every qualifying occurrence for each final ayah;
8. hydrate canonical ayah words once per ayah;
9. union all `PhraseQuranWordIds` from qualifying occurrences;
10. validate every selected word ID is positive, unique per ayah, and belongs to that ayah;
11. return ayahs in Quran order.

Do not resolve select-all from the currently visible page and do not trust client-supplied word
IDs.

### Reuse boundaries

- Reuse or extract the existing similarity candidate SQL/read primitives where practical.
- Do not copy the scoring algorithm into the controller or handler.
- Do not alter the existing paged search response merely to support linking.
- Do not cache selection bodies in the general shared phrase-search cache. The resolver response is
  request-scoped.
- Do not introduce a migration.

## Frontend selection design

### Selection store

Create a focused similarity ayah-selection store using the proven context-selection state machine.
It must not be added to the already-busy similarity facade.

Required state:

```text
resultSetKey
mode: only | all-except
overrides: Set<ayahId>
totalAyahCount
selectedCount
revision
```

Required behavior:

- `only` + empty set is the initial/cleared state;
- selecting one in `only` adds the ID;
- deselecting one in `only` removes the ID;
- select all switches to `all-except` with an empty exclusion set;
- deselecting after select all adds the ID to exclusions;
- reselecting an excluded ayah removes it from exclusions;
- changing population identity resets selection;
- changing page or sort keeps selection;
- a revision increases after every effective selection mutation;
- invalid/non-positive IDs are ignored client-side and rejected server-side.

The implementation may extract a small reusable phrase ayah-selection state machine only when that
keeps the context behavior byte-for-byte equivalent. Do not refactor the context feature broadly.

### Result table

Extend `phrase-similarity-list` with:

- checkbox header cell;
- checkbox per standard row;
- checkbox per compact row;
- select-all checked/indeterminate state;
- row-selected presentation from the selection store;
- whole-row click toggling through the existing `QdDataTableComponent` selectable-row API;
- explicit event isolation for the checkbox and Mushaf link;
- selected-count announcement.

Keep the current protected Quran renderer and highlight-role inputs unchanged.

### Results header and actions

The result header must show:

- total unique ayahs;
- total qualifying occurrences;
- selected ayah count;
- select-all control where compact layout requires a visible alternative to the table header;
- add-to-workspace and direct-link actions.

Actions must remain disabled while results are stale/refreshing, the query draft is pending, or the
selection is empty.

## Frontend linking integration

### API method

Add a `resolveLinkingSelection` method to `PhraseSimilarityApi` using the generated request and
response contracts. Do not hand-maintain generated DTOs after OpenAPI generation is available.

### Coordinator

Create a focused similarity-linking coordinator modeled on the context coordinator.

For each action:

1. capture the current population identity, selection snapshot, and selected-count revision;
2. call the compact resolver endpoint;
3. reject/ignore the response if route, query, threshold, build, or selection changed;
4. validate response build, query identity, ayah count, verse uniqueness, and selected word IDs;
5. create one `LinkingSourceLaunch`;
6. call `LinkingWorkspaceStore.addSource` or `LinkingWorkflowFacade.startFromSource`;
7. preserve the selection when resolution fails so the user can retry;
8. prevent double submission while resolution is active;
9. expose controlled Arabic failure feedback;
10. reuse current linking access and focus coordination.

Do not put request orchestration in the list component or the main similarity facade.

### Pure launch mapper

Create a pure mapper such as `phrase-similarity-linking-launch.ts`.

Mapping contract:

```text
source.kind = manual-mushaf-ayahs
source.label = متشابهات العبارة «{response.query.displayText}»
source.contextKey = null
source.manualAyahs = sorted unique response ayahs

initialConfiguration.inclusionMode = all-except
initialConfiguration.ayahOverrideIds = []
initialConfiguration.selectedWords = flattened response selectedQuranWordIds
initialConfiguration.automaticWordMatchesEnabled = null
initialConfiguration.manualLinkShape = independent
initialConfiguration.descriptions = []
```

Fail closed and do not launch when:

- selected count is zero or disagrees with the captured selection;
- response build/query identity is stale;
- an ayah or verse key is duplicated or invalid;
- a page number is outside the Mushaf range;
- a selected word ID is non-positive or duplicated for the same ayah;
- any selected ayah has zero selected words.

## Expected file ownership

### Backend existing files likely to change

- `IPhraseSimilarityReader.cs` or a focused linking-selection reader abstraction;
- phrase similarity response contracts under Application Abstractions;
- similarity request validation helpers only where shared validation is required;
- similarity reader partials under `Persistence/Reads/Quran/PhraseSearch/`;
- phrase-search API message constants only if a localized success message is required;
- DI registration only when the new handler/reader is not conventionally discovered.

### Backend expected new focused files

- one Application query/handler folder for resolving similarity linking selection;
- one thin API controller for `/similarities/linking-selection`;
- one request-body mapper/contract following current phrase-search controller conventions;
- one Infrastructure reader partial for complete selection materialization if adding the operation to
  the existing reader keeps responsibilities coherent.

### Frontend existing files likely to change

- `phrase-similarity.routes.ts` for feature-scoped providers;
- `phrase-similarity.api.ts`;
- `phrase-similarity.models.ts` only for non-generated feature state;
- `phrase-similarity-list` component files;
- `phrase-similarity-page` component files;
- generated API models after the repository's existing generation workflow.

### Frontend expected new focused files

- `state/phrase-similarity-ayah-selection.store.ts`;
- `state/phrase-similarity-linking.coordinator.ts`;
- `utils/phrase-similarity-linking-launch.ts`;
- a small similarity-linking-actions component when keeping actions out of the result list improves
  responsibility boundaries.

Do not create generic `helpers.ts`, put resolver state in the main facade, or add SQL to an API
controller.

## Phased implementation order

The executor must complete phases in order and stop for owner review after each phase. Do not begin
the next phase without the owner's instruction. Do not commit or push unless explicitly asked.

Before every phase:

- confirm the branch is not `main`;
- inspect `git status` and preserve unrelated user changes;
- reread this plan and the exact code in scope;
- read the implicated native routers and only triggered authorities;
- keep Quran text and canonical word IDs read-only;
- do not add production-source comments unless the repository's exceptional comment bar is met.

### Phase 1 — Frontend ayah selection foundation

Scope:

- feature-scoped selection store with `only` / `all-except` semantics;
- population identity excluding sort/page;
- checkbox column in standard and compact layouts;
- select-all and indeterminate states;
- whole-row toggling except checkbox and ayah link;
- selected-count display and accessibility announcement;
- selection reset/persistence rules;
- no linking API call or linking action yet.

Acceptance gate:

- individual selection and deselection work;
- select all then deselect one produces the correct selected count;
- selection survives page and sort changes;
- selection resets when query/build/threshold changes;
- checkbox click toggles exactly once;
- non-ayah row click toggles selection;
- ayah-text click opens the Mushaf without changing selection;
- keyboard row toggling works;
- frontend typecheck/build gates pass;
- no linking action is claimed complete.

Stop and request Phase 1 review.

### Phase 2 — Backend complete linking-selection resolver

Scope:

- request/response contracts;
- request mapper and controlled validation;
- Application query/handler;
- complete similarity-population selection reader;
- all-except resolution across every page;
- unioned canonical `PhraseQuranWordIds` per selected ayah;
- Owner-protected POST endpoint;
- OpenAPI/generated model refresh;
- no frontend linking action yet.

Acceptance gate:

- `only` returns exactly the submitted matching ayahs;
- `all-except` returns every qualifying ayah except submitted exclusions;
- unknown/non-matching/duplicate IDs are rejected;
- zero final selection is rejected;
- each ayah appears once even with several qualifying occurrences;
- selected words equal all qualifying phrase-window word IDs, including matched and differing words;
- every selected word belongs to its ayah;
- stale build/reference returns the controlled conflict/invalid outcome;
- endpoint is read-only and Owner-protected;
- backend build and generated-contract checks pass;
- no direct/workspace launch is claimed complete.

Stop and request Phase 2 review.

### Phase 3 — Direct and workspace linking integration

Scope:

- similarity API resolver method;
- request-fenced linking coordinator;
- pure fail-closed source launch mapper;
- direct-link and add-to-workspace actions;
- busy, zero-selection, stale-result, access, focus, and error states;
- source label and independent default shape;
- duplicate feedback through the existing unified linking workflow.

Acceptance gate:

- both actions resolve the identical complete source;
- select-all is not limited to the current 100-row page;
- source contains each selected ayah once;
- matched and differing phrase words are all preselected;
- one ayah with multiple qualifying occurrences includes their complete word union;
- source label is `متشابهات العبارة «…»`;
- initial shape is independent;
- the ordinary door picker remains unrestricted;
- stale responses cannot launch an older selection;
- resolver failures preserve selection and support retry;
- duplicate sources use existing `موجود بالفعل` behavior;
- backend and frontend verification gates pass.

Stop and request Phase 3 review.

### Phase 4 — Integrated browser smoke and finish gate

This phase is verification and correction only. It must not add new product scope.

Required browser journey:

1. open the phrase similarity page with a query that has more than one results page;
2. record the query, threshold, total ayah count, and active build used for evidence;
3. select one visible ayah by checkbox;
4. deselect it by clicking a non-ayah part of the row;
5. click the ayah text and verify Mushaf navigation without selection change;
6. select all, move to another result page, and verify rows remain selected;
7. deselect at least one ayah on two different pages;
8. change sort and verify selection remains;
9. change threshold and verify selection resets;
10. restore a selection and add it to the workspace;
11. inspect the source: correct label, selected ayahs, all green/red words selected, independent shape;
12. repeat through direct linking;
13. confirm the ordinary full door picker is available;
14. verify duplicate feedback;
15. verify desktop, compact/mobile RTL, keyboard behavior, API failures, and console errors.

After the smoke passes, wait for an explicit owner request before running the formal
engineering-review Skill. Do not self-authorize a commit, push, or planning-artifact deletion.

## Testing Decision

No new automated tests are planned.

Reason:

- the repository Test Freeze prohibits new backend test methods/classes and Playwright files without
  explicit owner approval;
- this feature adds a read resolver and orchestration around the existing linking write workflow,
  not a new critical write rule;
- the cheapest adequate gates are builds, static frontend verification, targeted API reads, and an
  integrated browser smoke.

Required verification commands, run independently:

Backend:

```bash
dotnet build Backend/QuranDashboard.sln --disable-build-servers -m:1 -p:BuildInParallel=false -v minimal
```

Frontend, from `Frontend/quran-dashboard-ui` and in this order:

```bash
npm run check:no-unit-specs
npm run typecheck:app
npm run build:verify
```

If generated-client drift is not covered by `build:verify`, run the repository's existing OpenAPI
generation/check workflow separately. Do not hand-edit generated DTOs instead of regeneration.

## Failure and stale-state behavior

- A linking-selection failure leaves the current ayah selection intact.
- A stale resolver response is ignored and cannot open the linking workflow or mutate workspace.
- A phrase build change invalidates the population and resets selection through the existing route
  refresh behavior.
- Changing threshold resets selection before new results become linkable.
- Sort/page changes preserve selection because they do not change membership.
- A zero-selection state disables both actions.
- A zero-result state exposes no selectable rows or linking actions.
- Invalid include/exclude IDs fail closed; the backend never silently drops them.
- A workspace duplicate uses existing unified duplicate feedback rather than a new message variant.

## Data safety and security invariants

- Never reconstruct, normalize, repair, or invent Quran text.
- Never accept client-supplied Quran word IDs for this resolver.
- Every selected word ID must originate from canonical `quran_words` hydration and belong to its
  selected ayah.
- Treat opaque resolution references as untrusted and validate build/query identity.
- Keep the endpoint Owner-protected, rate-limited, timed out, and read-only.
- Do not log full Quran payloads, opaque references, or selected source bodies unnecessarily.
- Do not introduce a migration or mutate phrase-index/source data.

## Implementation prohibitions

The executor must not:

- link only the current visible page after select-all;
- send selected Quran word IDs from the browser to the resolver;
- compute the full selected source by walking every frontend page;
- use matched IDs while omitting differing IDs, or vice versa;
- create multiple source ayah entries for multiple occurrences in one ayah;
- alter similarity display colors to implement linking;
- include page or sort in population identity;
- omit build/query/threshold from population identity;
- put selection state or linking requests in the main similarity facade;
- create a new source kind or door restriction;
- assign a filter-derived `contextKey`;
- add tests, migrations, commits, pushes, or unrelated refactors without explicit authorization.

## Definition of done

The feature is complete only when all statements are true:

- every locked owner decision is implemented;
- selection works individually and across pages using compact all-except semantics;
- row/checkbox/Mushaf interactions do not conflict;
- the backend resolves the complete selected ayah population independently of frontend paging;
- every selected ayah appears once;
- all qualifying matched and differing phrase words are preselected;
- direct and workspace flows use one validated source mapping;
- source label, duplicate identity, and independent shape match this plan;
- the door picker remains unrestricted;
- no Quran data, similarity algorithm, or unrelated linking behavior regressed;
- backend build and all frontend verification commands pass;
- integrated browser smoke passes;
- every phase has been reviewed when the owner requests it;
- the final owner-requested engineering review reports no open findings;
- this planning folder is deleted in a final pure-deletion commit before merge.
