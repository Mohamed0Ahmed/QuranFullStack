# Quran Context Alternative Filters and Ayah Linking — Implementation Plan

## Document control

- Status: planned; implementation has not started.
- Working branch: `unified-ayah-linking`.
- Planning style: feature-scoped implementation plan; no Spec Kit artifacts are required.
- Primary surface: Quran phrase context explorer.
- Route: `/dashboard/words/phrases/context`.
- Lifecycle: delete this feature folder in the final pure-deletion commit after the owner-requested engineering review passes and before merge, following `docs/README.md`.

## Objective

Extend the existing phrase context explorer so a researcher can:

1. continue using the current single-word path navigation;
2. alternatively select several words at the same immediate previous or following position as an OR group;
3. cross-filter previous and following options against one another;
4. see one result row per unique ayah while preserving every matching occurrence and every highlighted Quran word;
5. select individual ayahs or all matching ayahs;
6. link the selected ayahs directly or add them to the linking workspace;
7. reject a source when its final selected ayah set is exactly equal to an existing source, regardless of the context-filter path that produced it.

This is an extension of the current context explorer and the existing unified manual-Mushaf linking workflow. It is not a new search engine and it must not introduce another linking source kind.

## Locked owner decisions

These requirements are authoritative for this feature.

### Alternative selection

- Every word row in both the previous and following tables gets an additive action.
- The unselected action is `+`.
- The selected action is `×`.
- Clicking the word text preserves the existing single-path behavior.
- Clicking `+` does not navigate to the next path level. It adds that word to an alternative group at the current immediate position.
- More than one word may be added to the same group.
- Alternatives inside one group are OR conditions.
- Previous-side and following-side constraints are combined with AND.
- Additive selection is available on both sides.
- Selected alternatives remain visible and pinned at the top of their own table.
- A selected alternative remains pinned even if cross-filtering gives it a current count of zero.
- Each side has its own `مسح الكل` action.
- While a side has a non-empty additive group, word-text navigation and path navigation on that same side are disabled.
- The other side remains fully operable.
- Clearing the side group restores the existing word/path navigation behavior.
- Boundary rows (`بداية الآية` and `نهاية الآية`) retain the existing single-path behavior and do not receive additive `+` actions in this feature.

### Cross-filtering

- Selected previous alternatives filter the following table.
- Selected following alternatives filter the previous table.
- Both active groups filter the result ayahs.
- Each table is a self-excluding facet: its candidate counts apply every committed path and every constraint from the opposite side, but do not restrict the candidate universe to its own active alternatives.
- This self-exclusion is required so unselected choices remain discoverable and can be added to the group.

### Result rows and statistics

- The table displays one row per unique ayah.
- If an ayah contains two or more matching occurrences, it appears once.
- Every matching occurrence contributes its highlighted Quran words to that one ayah row.
- The result summary immediately above the table shows:
  - unique ayah count;
  - matching occurrence count.
- “Occurrence count” means matching anchors/positions, not rows.
- The result row retains the current Mushaf navigation behavior when the user clicks the ayah text.

### Ayah selection

- Every unique ayah row has a checkbox.
- The table header has a select-all checkbox.
- Selecting all and then deselecting one or more individual ayahs is supported.
- Selection persists while paging within the same exact result set.
- Selection resets when the query, committed path, or either alternative group changes. This prevents hidden stale ayahs from being linked after the filter changed.
- Linking actions are disabled when zero ayahs are selected.

### Linking

- The selected ayahs support both:
  - direct linking;
  - add to linking workspace.
- The default manual link shape is `independent` so every ayah is a separate link unit.
- All highlighted canonical Quran word IDs from all matching occurrences in a selected ayah are preselected in the linking configuration.
- The visible source label is `البحث عن «{original query}»`.
- The original submitted query comes from the route/search state, not from a reconstructed Quran spelling.
- The source kind is `manual-mushaf-ayahs`.
- The source `contextKey` is `null`.
- Exact duplicate identity is based on the sorted selected verse-key set:
  - the exact same selected ayah set is rejected as `موجود بالفعل`;
  - a set differing by at least one ayah is allowed, even when its visible label is the same;
  - a different filter path that resolves to the same ayah set is still a duplicate.

## Current implementation evidence

The executor must understand these facts before editing.

### Current frontend behavior

- `phrase-context-url-sync.ts` stores one opaque `before` path reference and one opaque `after` path reference.
- `phrase-context-web` emits only single option/path selection events.
- `phrase-context-selection.store.ts` stores raw occurrence rows and has no ayah-selection state.
- `phrase-context-occurrence-list` tracks rows by `occurrenceId`, so a repeated ayah currently appears more than once.
- The result renderer currently derives previous/following highlights by slicing the full-side highlight arrays using the selected path length.
- The result page size is 200.
- The existing facade is already large. Do not add ayah selection, linking-resolution state, and alternative mutation logic directly into that one facade.

### Current backend behavior

- `PhraseContextSelection` contains a resolution plus one linear previous path and one linear following path.
- `PhrasePathReference.SelectedExactTokenIds` represents one token at every selected depth.
- `FilteredOccurrencesSql` compares one exact token ID at each selected path position.
- Branch candidate counts already derive from the filtered occurrence population, so the singular previous/following cross-filtering foundation is present.
- Results are paged and ranked by occurrence, not by unique ayah.
- `PhraseContextOccurrenceDto` already carries canonical Quran word IDs for the query and the full previous/following sides.
- The reference codec validates build, text mode, side, query identity, payload checksum, length, and token limits. Alternative references must retain the same fail-closed behavior.

### Live baseline captured before implementation

For the simple-mode query `عذاب` on build `e828c1b0-4fbc-4063-a685-a654bee53d28`:

- 150 matching occurrences are returned;
- those occurrences belong to 145 unique ayahs;
- five ayahs contain two occurrences;
- the first previous candidates include:
  - `وَلَهُمْ`: 23 occurrences;
  - `لَهُمْ`: 20 occurrences;
  - `فَلَهُمْ`: 2 occurrences;
- ayah `85:10` contains two query occurrences. Under the previous alternative group `وَلَهُمْ + لَهُمْ + فَلَهُمْ`, it must appear once with both matching previous/query pairs highlighted.

The active build ID is runtime evidence only. Never hardcode it in production code or retained tests.

## Non-goals

Do not expand this feature into any of the following:

- Quran search from the navigation menu or other Quran search pages;
- Mushaf mutashabihat or nearby-ayah linking, which is already implemented;
- phrase repetition behavior, except reusing its linking-launch patterns where appropriate;
- a new automatic linking source kind;
- database schema or migration changes;
- Quran source/importer changes;
- changes to Quran text, word boundaries, glyphs, markers, or canonical IDs;
- additive boundary alternatives;
- saved named searches;
- permanent new automated tests without explicit owner approval;
- visual redesign of the context explorer beyond the controls required here.

## Core terminology and state model

Use the following terms consistently in code and reviews.

- **Query span**: the original resolved phrase, for example `عذاب`.
- **Committed path**: the existing linear previous or following word path selected by clicking word text.
- **Alternative group**: a set of exact token IDs selected with `+` at the immediate next position after a side's committed path.
- **Result occurrence**: one query anchor that matches both committed paths and active alternative groups.
- **Result ayah**: one unique ayah containing one or more result occurrences.
- **Facet self-exclusion**: candidate counts for a side ignore that side's active alternative group while applying the opposite side's active group.
- **Final filter**: query span + both committed paths + both alternative groups.

Represent the state conceptually as:

```text
resolution
previous committed path
following committed path
previous alternative group at the next previous position
following alternative group at the next following position
```

Do not convert the existing committed path into an array of OR groups. The owner requires OR alternatives only at the currently visible immediate position, and word navigation is blocked on that side while the group exists. Keeping committed paths and active groups separate preserves the current navigation contract and makes clearing a group deterministic.

## Formal filtering semantics

For an occurrence `o`, define:

- `P` as the ordered previous committed path, closest word first;
- `F` as the ordered following committed path, closest word first;
- `AP` as the previous alternative exact-token set;
- `AF` as the following alternative exact-token set;
- `QStart` and `QEnd` as the query span word positions.

An occurrence belongs to the final result only when all conditions hold:

```text
previous committed path matches P
AND following committed path matches F
AND (AP is empty OR token at QStart - |P| - 1 is in AP)
AND (AF is empty OR token at QEnd + |F| + 1 is in AF)
AND existing requested boundary conditions match
```

Within `AP` and within `AF`, membership is OR. Between all four lines, composition is AND.

### Candidate population for the previous table

Apply:

- previous committed path `P`;
- following committed path `F`;
- following alternative group `AF`;
- existing boundary constraints.

Do not apply `AP` when computing previous candidate rows or their counts. Compute the candidate token at `QStart - |P| - 1`.

### Candidate population for the following table

Apply:

- previous committed path `P`;
- following committed path `F`;
- previous alternative group `AP`;
- existing boundary constraints.

Do not apply `AF` when computing following candidate rows or their counts. Compute the candidate token at `QEnd + |F| + 1`.

### Final summary and result population

Apply `P`, `F`, `AP`, and `AF`. Both side headers must report the same final occurrence count. The result response must report the final occurrence count and final unique ayah count separately.

## Backend contract design

### 1. Alternative reference

Add a focused immutable reference type near `PhrasePathReference`, for example:

```csharp
public sealed record PhraseContextAlternativeReference(
    Guid BuildId,
    PhraseTextMode Mode,
    PhraseContextSide Side,
    IReadOnlyList<int> QueryExactTokenIds,
    IReadOnlyList<int> CommittedPathExactTokenIds,
    IReadOnlyList<int> AlternativeExactTokenIds);
```

Required invariants:

- build ID is non-empty;
- mode and side are defined;
- query identity is non-empty and valid;
- committed path matches the side's supplied path exactly;
- alternative IDs are positive, distinct, and canonically sorted before encoding;
- the group is non-empty;
- the combined query/path/alternative payload respects the existing bounded query/reference protections;
- decoded references are rejected if build, mode, side, query, or committed path differs from the active request;
- do not accept client-supplied raw token arrays in the public endpoint contract.

Add codec methods with the same checksum and fail-closed pattern as existing resolution/path references:

```csharp
string EncodeAlternatives(PhraseContextAlternativeReference reference);
bool TryDecodeAlternatives(string? value, out PhraseContextAlternativeReference? reference);
```

Use a new reference kind byte; do not reinterpret an existing kind. Include alternative state in `ComputeScope(PhraseContextSelection)` so cursors and cache keys cannot be reused across different groups.

### 2. Selection model and request parser

Extend `PhraseContextSelection` with nullable previous/following alternative references.

Extend the existing branches/results requests with optional query parameters:

- `previousAlternativesRef`
- `followingAlternativesRef`

The parser must validate every reference against:

- resolution build/mode/query;
- expected side;
- the exact committed path for that side;
- no alternatives when the committed path ends at an ayah boundary.

Malformed, stale, cross-side, cross-query, or cross-path references return the existing controlled invalid-reference response. Do not silently clear invalid alternative state in the backend.

The existing `contexts/groups` and `contexts/occurrences` endpoints are out of scope unless compilation requires a mechanical signature update. They must retain their current behavior.

### 3. Branch option response

Keep the existing single-navigation `selectionRef` meaning intact. Add the minimum explicit fields necessary for additive UI behavior:

```text
isAlternativeSelected
alternativeToggleRef
```

Rules:

- For an unselected word, `alternativeToggleRef` represents the group after adding that token.
- For a selected word, it represents the group after removing that token.
- If removing the last token clears the group, the toggle reference is `null` and the frontend removes that side's URL parameter.
- Boundary rows expose no additive toggle reference.
- The frontend must never create or mutate exact token arrays itself.
- Selected rows are returned at the top of `options`.
- Selected rows missing from the current candidate population are still returned with a current count of zero.
- Hydrate a zero-count selected row's display text from the canonical mode-specific unique-word table (`QuranWordsUniqueSimple` or `QuranWordsUniqueTashkeel`), never from client text or from a stale frontend label.
- Do not duplicate a selected row again in the unselected candidate list.
- Preserve deterministic ordering: selected rows first, then current candidate ordering. Within the pinned section use a stable count-descending/exact-token-ID tie break.
- Preserve the existing candidate paging cursor for the non-pinned candidate population. Pinned rows do not consume the 25-row candidate page size.

`totalOptions` remains the candidate-universe count, not candidate count plus pinned zero-count rows. This prevents header totals from changing merely because a selected row is retained for removal.

### 4. Result response grouped by ayah

Replace occurrence-row pagination in `PhraseContextResultsResponse` with unique-ayah pagination. The response must expose:

```text
activeBuildId
page
pageSize
totalAyahCount
totalOccurrenceCount
items: unique ayah result rows
```

Each result ayah DTO must contain at least:

```text
ayahId
verseKey
surahNumber
surahNameArabic
ayahNumber
pageFrom
pageTo
words
occurrenceCount
highlights.queryQuranWordIds
highlights.previousQuranWordIds
highlights.followingQuranWordIds
```

Highlight rules:

- query IDs are the union of query-span word IDs from every matching occurrence in that ayah;
- previous IDs are the union of the words matched by the committed previous path plus the active previous alternative position for every occurrence;
- following IDs use the equivalent following logic;
- arrays are distinct and ordered by canonical Quran word order;
- do not return every word before or after the query as “selected context”;
- do not rely on frontend slicing to infer selected IDs;
- words continue to come from canonical `quran_words` rows without text reconstruction.

SQL/read flow:

1. produce the final filtered occurrence population;
2. calculate `COUNT(*)` for occurrences and `COUNT(DISTINCT ayah_id)` for ayahs;
3. rank unique ayahs in Quran order;
4. page unique ayahs, not occurrences;
5. load every matching occurrence for the ayahs on that page;
6. hydrate each ayah's canonical words once;
7. aggregate highlights in Infrastructure/C# using canonical word positions and IDs;
8. return one DTO per ayah.

This ordering prevents occurrences of one ayah from being split across pages and prevents incomplete highlighting.

### 5. Compact linking-selection resolver

Do not construct a “select all” source from only the currently loaded 200 ayahs. Add a read-only action endpoint using POST because the selection body can contain many ayah IDs:

```text
POST /api/quran/phrase-search/contexts/linking-selection
```

Suggested request shape:

```json
{
  "resolutionRef": "...",
  "previousRef": null,
  "followingRef": null,
  "previousAlternativesRef": null,
  "followingAlternativesRef": null,
  "selectionMode": "only",
  "ayahIds": [14, 17]
}
```

Supported selection modes:

- `only`: `ayahIds` are the selected ayahs;
- `all-except`: every final-filter ayah is selected except `ayahIds`.

Suggested response shape:

```text
activeBuildId
selectedAyahCount
ayahs[]:
  ayahId
  verseKey
  pageNumber
  selectedQuranWordIds[]
```

Resolver invariants:

- parse and validate the exact same final filter as branches/results;
- reject duplicate, non-positive, unknown, or non-matching submitted ayah IDs;
- reject a selection resolving to zero ayahs;
- return selected ayahs in Quran order;
- return every selected ayah exactly once;
- return the union of query/previous/following highlighted IDs across every matching occurrence;
- validate that every returned selected word belongs to its ayah and is a canonical positive Quran word ID;
- use the active build snapshot and return the existing build-changed conflict when stale;
- use the existing phrase-search compute rate limit and timeout policies;
- use the standard `ApiResponse<T>` envelope and Arabic messages;
- perform no linking mutation. This endpoint only resolves compact source input.

The endpoint does not decide duplicate identity and does not create a new source kind. The frontend maps its response into the existing manual-Mushaf launch contract.

### 6. Cache and cursor identity

Update context branches/results cache keys to include canonical previous/following alternative IDs. Equivalent alternative sets selected in different orders must share the same identity.

Update branch cursor scope to include both groups. A cursor created before any group change must be rejected after the group changes.

The linking-selection resolver should not cache user-specific include/exclude bodies in the general phrase read cache. It may reuse the shared filtered read primitives, but keep the response request-scoped unless a bounded, justified cache key is explicitly reviewed.

## Backend file ownership map

Expected existing files to modify:

- `Backend/application/QuranDashboard.Application.Abstractions/Quran/PhraseSearch/IPhraseSearchReferenceCodec.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/PhraseSearch/IPhraseContextReader.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/PhraseSearch/Responses/PhraseContextResponses.cs`
- `Backend/application/QuranDashboard.Application/Quran/PhraseSearch/PhraseContextRequestParser.cs`
- branch/results query and handler files under `Backend/application/QuranDashboard.Application/Quran/PhraseSearch/Queries/`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/PhraseSearch/PhraseSearchReferenceCodec*.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/PhraseSearch/EfPhraseContextReader*.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/PhraseSearch/PhraseSearchCacheKeys.cs`
- branches/results controllers under `Backend/api/QuranDashboard.Api/Controllers/Quran/PhraseSearch/`
- API message constants only if a new localized resolver message is required.

Expected new focused files:

- one Application query/handler folder for resolving the linking selection;
- one thin API controller for the linking-selection endpoint;
- focused response/request contracts only where the existing feature layout requires them;
- an additional `EfPhraseContextReader` partial if necessary to keep database, result aggregation, and linking resolution responsibilities separated.

Do not put SQL or selection logic in controllers. Do not turn `IPhraseContextReader` into a general linking writer.

## Frontend state and URL contract

### URL state

Extend `PhraseContextUrlState` with:

```text
previousAlternatives: string | null
followingAlternatives: string | null
```

Recommended query parameter names:

```text
beforeAny
afterAny
```

Keep existing `before` and `after` meanings unchanged.

Update all URL-state operations:

- parsing and validation;
- serialization;
- default state;
- branch state key;
- final result state key;
- back/forward behavior;
- route cancellation/request fencing;
- session-only long-URL fallback.

State key rules:

- branch workspace identity includes both committed paths and both alternative groups;
- result identity includes the same filter state plus page where appropriate;
- ayah selection identity excludes result page but includes every filter reference;
- changing either group resets result page to 1;
- clearing one group changes only that side's group parameter;
- single-path navigation is refused client-side while the same side's group is non-empty.

### Alternative actions

Add facade/coordinator methods with side-explicit names. Do not overload the existing word-navigation method with additive behavior.

Conceptual actions:

```text
togglePreviousAlternative(toggleRef)
toggleFollowingAlternative(toggleRef)
clearPreviousAlternatives()
clearFollowingAlternatives()
```

Every toggle or clear:

- navigates through the route-authoritative state;
- resets result page to 1;
- lets the existing route request fences cancel stale requests;
- moves focus back to a stable control/row on the same side;
- does not clear the opposite side;
- resets ayah selection through the changed result-set key.

Do not optimistically fabricate counts or result rows. The server response owns cross-filter truth.

## Frontend alternative-table UI

Extend `phrase-context-web` without changing the existing word-button semantics.

Required table structure:

- row number;
- word/boundary text;
- additive action column for word rows;
- occurrence count.

Required behavior:

- `+` has an accessible label such as `إضافة {word} إلى مجموعة الكلمات السابقة`;
- `×` has an accessible label such as `إزالة {word} من مجموعة الكلمات السابقة`;
- selected state is exposed with `aria-pressed` or an equivalent explicit state;
- action controls remain keyboard operable;
- word/path buttons on the active-group side are disabled;
- additive action buttons remain enabled except while loading;
- `مسح الكل` appears only when that side's group is non-empty;
- the selected group is shown as a compact card/chip collection near that side's path;
- the current committed breadcrumb remains visually and behaviorally distinct from the additive group;
- selected rows are pinned by response order; do not resort server rows independently in the component;
- a zero-count selected row remains removable;
- boundary rows have no empty or misleading action cell on compact layouts;
- RTL, responsive table behavior, and Quran font rendering remain unchanged.

Do not add explanatory metadata that the owner did not request. The visible additions are the actions, selected-group presentation, clear action, statistics, checkboxes, and linking actions.

## Frontend unique-ayah result table

Replace occurrence-oriented row state with unique-ayah row state.

Required changes:

- row identity becomes `ayahId`;
- table `totalRowCount` uses `totalAyahCount`;
- pagination uses unique ayah count;
- the status announcement says ayahs and occurrences accurately;
- the renderer consumes server-aggregated highlight arrays directly;
- remove previous/following highlight slicing from the component;
- add a checkbox column in standard and compact layouts;
- add a header select-all checkbox with checked and indeterminate states;
- clicking the checkbox must not open the Mushaf;
- clicking the ayah link continues to open the Mushaf;
- keep the Quran words and their protected rendering inside the existing highlighted-ayah component.

### Dedicated ayah-selection store

Create a focused feature-scoped store rather than growing the main context facade.

Recommended state:

```text
resultSetKey
mode: only | all-except
overrides: Set<ayahId>
```

Semantics:

- initial/cleared state: `only` with an empty set;
- select one in `only`: add its ID;
- deselect one in `only`: remove its ID;
- select all: switch to `all-except` with an empty set;
- deselect one after select all: add its ID to exclusions;
- reselect an excluded row: remove its ID from exclusions;
- clear all: switch to `only` with an empty set;
- selected count in `only`: set size;
- selected count in `all-except`: `totalAyahCount - exclusion count`;
- changing `resultSetKey`: clear selection;
- changing page only: retain selection.

Do not store thousands of selected IDs after select-all. The `all-except` representation is required.

## Frontend linking integration

Create a focused phrase-context linking coordinator/helper. Do not duplicate the complete linking workflow and do not alter the generic linking source identity rules.

On direct-link or add-to-workspace action:

1. capture the exact current filter state and ayah-selection snapshot;
2. call the linking-selection resolver;
3. ignore a stale response if route/filter/selection changed while it was loading;
4. map the compact response into `LinkingSourceLaunch`;
5. call the existing `LinkingWorkflowFacade.startFromSource` or `LinkingWorkspaceStore.addSource` path;
6. reuse existing access checks, focus coordination, and duplicate feedback;
7. expose a controlled Arabic error when resolution fails;
8. prevent double submission while resolution is active.

Launch mapping:

```text
source.kind = manual-mushaf-ayahs
source.label = البحث عن «{trimmed original query}»
source.contextKey = null
source.manualAyahs = resolver ayahs mapped to verseKey/page/displayHint
initialConfiguration.inclusionMode = all-except
initialConfiguration.ayahOverrideIds = []
initialConfiguration.selectedWords = flattened resolver selectedQuranWordIds
initialConfiguration.automaticWordMatchesEnabled = null
initialConfiguration.manualLinkShape = independent
initialConfiguration.descriptions = []
```

Sort and deduplicate manual ayahs and selected words defensively in the pure mapper. Fail closed and do not launch linking if response completeness or canonical ID validation fails.

Do not generate a filter-derived `contextKey`. `contextKey: null` is load-bearing for the owner's exact-ayah-set duplicate requirement.

## Frontend file ownership map

Expected existing files to modify:

- `Frontend/quran-dashboard-ui/src/app/features/words/quran-phrase-search/models/phrase-context.models.ts`
- `.../state/phrase-context-url-sync.ts`
- `.../state/phrase-context-workspace.loader.ts`
- `.../state/phrase-context-selection.store.ts`
- `.../state/phrase-context-action.coordinator.ts`
- `.../state/phrase-context.facade.ts`
- `.../data-access/phrase-context.api.ts`
- `.../components/phrase-context-explorer/*`
- `.../components/phrase-context-web/*`
- `.../components/phrase-context-occurrence-list/*`
- `.../pages/phrase-context-page/*`
- generated API client models/services produced by the repository's existing generation workflow.

Expected new focused files:

- `state/phrase-context-ayah-selection.store.ts`;
- a focused context-linking coordinator/controller;
- a pure context-linking launch mapper;
- a compact linking-actions component only if the existing generic source-actions component cannot safely support asynchronous source resolution.

File-size guard:

- do not put ayah selection into the already-large main facade;
- do not put resolver orchestration into the result-table component;
- if a facade/store crosses the hard structure threshold, stop and split by workflow before continuing;
- do not create generic `helpers.ts` or unrelated dumping files.

## Phased implementation order

The executor must complete phases in order and stop for owner review after each phase. Do not silently begin the next phase. Do not commit or push unless explicitly asked.

Before Phase 1, the executor must:

- confirm the current branch is not `main`;
- inspect `git status` and preserve unrelated user changes;
- read the root and implicated Backend/Frontend routers and only their triggered authorities;
- treat this plan as active feature intent and code as current implementation truth;
- use focused files/partials instead of growing an already oversized responsibility;
- avoid production-source comments unless the repository's three-part comment exception is explicitly satisfied;
- record the exact baseline commands/results used for that phase's handoff.

### Phase 1 — Reference and application contract foundation

Scope:

- alternative reference record and codec methods;
- canonical sorting/distinct validation;
- `PhraseContextSelection` extension;
- request parser validation;
- cache/scope identity inputs;
- branches/results query/controller parameter plumbing;
- response contract fields needed for alternative actions and grouped result totals;
- linking-selection request/response abstraction and Application query/handler skeleton without final SQL if separation is cleaner.

Acceptance gate:

- existing requests without alternative refs still parse and behave as before;
- malformed/stale/cross-side/cross-path alternative refs fail as invalid reference;
- equivalent token sets encode to one canonical state;
- cursors/scopes differ when an alternative group differs;
- backend builds;
- no UI behavior is claimed complete.

Stop after this gate and request Phase 1 review.

### Phase 2 — Backend faceting, unique-ayah results, and linking resolver

Scope:

- three occurrence populations where needed: previous facet, following facet, final results;
- OR-in-group/AND-between-sides predicates;
- self-excluding candidate counts;
- pinned selected options including zero-count selections;
- unique-ayah paging and separate totals;
- unioned server-owned highlight IDs;
- compact linking-selection endpoint;
- cache keys, page weights, and invalid-reference/build-changed outcomes;
- generated OpenAPI contract availability for the frontend.

Acceptance gate:

- base `عذاب` response reports 150 occurrences and 145 unique ayahs on the captured data build;
- selecting `وَلَهُمْ + لَهُمْ + فَلَهُمْ` behaves as one previous OR group;
- adding a following selection reduces/recomputes previous counts and results;
- selected previous rows remain pinned when the following filter gives one a zero count;
- `85:10` is returned once with both matching pairs in its highlight union;
- result pages never split occurrences belonging to the same ayah;
- linking resolver returns all selected ayahs, including select-all beyond the visible page;
- backend builds;
- no frontend behavior is claimed complete.

Stop after this gate and request Phase 2 review.

### Phase 3 — Frontend alternative-group interaction

Scope:

- URL params/state keys/request fences;
- API query parameters and generated model adoption;
- `+`/`×` controls;
- pinned selected rows;
- per-side group card and `مسح الكل`;
- same-side navigation lock;
- opposite-side continued operation;
- cross-filter refresh and page reset;
- loading, focus, error, keyboard, RTL, and responsive states.

Acceptance gate:

- word text still performs existing single-path navigation when no same-side group exists;
- `+` adds without advancing the path;
- multiple alternatives can be added and individually removed;
- `×` on the last selected word clears the group;
- `مسح الكل` clears only its own side;
- previous group does not disable following controls, and vice versa;
- browser back/forward and refresh preserve the group state;
- stale requests cannot overwrite a newer group state;
- frontend verification commands pass.

Stop after this gate and request Phase 3 review.

### Phase 4 — Unique-ayah selection and linking

Scope:

- unique-ayah result rendering and statistics;
- checkbox per ayah and select-all header;
- `only`/`all-except` selection store;
- selection persistence across result pages;
- selection reset on filter changes;
- asynchronous compact linking resolution;
- manual source launch mapping;
- direct-link and workspace actions;
- exact ayah-set duplicate identity and existing Arabic feedback.

Acceptance gate:

- one row per unique ayah;
- visible stats distinguish ayahs from occurrences;
- select all followed by one deselection produces all matching ayahs except that ayah;
- a checkbox click never opens the Mushaf;
- an ayah-text click still opens the Mushaf;
- linked selected words equal all colored words across all matching occurrences;
- source label uses the original query text;
- initial link shape is independent;
- the exact same ayah set is reported as already present;
- a set differing by one ayah can be added as another source;
- direct and workspace flows both work;
- no first-200-only behavior exists;
- backend and frontend verification gates pass.

Stop after this gate and request Phase 4 review.

### Phase 5 — Integrated browser smoke and finish gate

This phase is verification and correction only. It must not introduce new product scope.

Required browser journey on the running backend/frontend:

1. open the context explorer and search simple-mode `عذاب`;
2. verify baseline 145 ayahs / 150 occurrences on the current captured build, or explicitly record if the active phrase build changed;
3. add previous alternatives `وَلَهُمْ`, `لَهُمْ`, and `فَلَهُمْ`;
4. verify the three rows are pinned and their actions show `×`;
5. verify previous word/path navigation is disabled while following remains usable;
6. select a following candidate and verify previous counts/results cross-filter;
7. clear only the following group and verify the previous group remains;
8. inspect `85:10`: one row, both matching pairs colored;
9. select all, deselect one ayah, and verify selected count;
10. add to workspace and verify the source label and independent shape;
11. attempt the same ayah set again and verify `موجود بالفعل`;
12. change one ayah in the selection and verify the source is allowed;
13. repeat using direct linking;
14. verify no console errors, failed API calls, focus traps, or mobile/RTL breakage.

After the smoke passes, wait for an explicit owner request before running the formal engineering-review Skill. Do not self-authorize a formal review, commit, push, or planning-artifact deletion.

## Testing Decision

No new automated tests are planned.

Reason:

- the repository Test Freeze prohibits new backend test methods/classes and Playwright files without explicit owner approval;
- the feature changes read/query behavior and UI orchestration but does not introduce a new security or critical write invariant that justifies a permanent test exception;
- the cheapest adequate gates are compilation, frontend static/build verification, targeted API reads, and an integrated browser smoke.

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

If the repository's generated client drift check is not included in `build:verify`, run the existing documented OpenAPI generation/check command separately. Do not hand-edit generated models as a substitute for regeneration.

## Failure and stale-state behavior

- A build change returns the existing conflict behavior and resets/re-resolves the context workspace through the current facade flow.
- An invalid alternative reference shows the existing controlled invalid-reference state; do not silently drop one side's group.
- A branch/results request failure keeps the last safe visible state according to the current request-status contract and exposes a safe Arabic message.
- A linking-selection failure leaves the ayah selection intact so the user can retry.
- A stale linking-selection response is ignored and must never launch a source for an older filter/selection.
- A zero-result cross-filter keeps both side controls available for removal/clear; it does not strand the user.
- A zero-selected-ayah state hides or disables linking actions.
- A zero-count pinned alternative remains removable.

## Data safety and security invariants

- Never infer, normalize, or repair Quran text in the frontend.
- Exact token matching continues to use the selected simple/tashkeel identity from canonical Quran words.
- Every selected word ID used for linking must be proven to belong to the corresponding selected ayah.
- Never trust client-supplied ayah IDs or reference payloads without backend validation.
- Never use displayed Arabic word text as filter identity.
- Do not log full Quran result payloads or opaque references unnecessarily.
- Keep the current phrase-search rate-limit, timeout, active-build snapshot, and conditional response behavior where applicable.
- No database schema change is authorized.

## Implementation prohibitions for the executing model

The following shortcuts are explicitly forbidden:

- grouping only the current frontend page by `ayahId`;
- creating a linking source from only loaded rows after select-all;
- summing candidate counts in the frontend to invent result totals;
- sending raw alternative token-ID arrays as public URL parameters;
- using display text as alternative identity;
- placing OR alternatives into the existing linear `SelectedExactTokenIds` without preserving committed-path semantics;
- allowing same-side word navigation while a group exists;
- disabling the opposite side when only one side has a group;
- dropping selected zero-count alternatives from the table;
- assigning a filter-derived `contextKey` to the manual linking source;
- using occurrence IDs as the final result-row identity;
- highlighting all words before/after the query instead of the matched path/group words;
- adding new tests, commits, pushes, migrations, or unrelated refactors without owner authorization.

## Definition of done

The feature is complete only when all statements are true:

- all locked owner decisions are implemented;
- alternative references and filters fail closed;
- OR/AND and self-excluding facet semantics match this plan;
- unique ayah and occurrence counts are both exact;
- repeated occurrences in one ayah aggregate into one row and one selected-word union;
- select-all works across the full result set, not merely one page;
- direct and workspace linking use the same complete source launch;
- duplicate identity is determined by the exact selected ayah set;
- no Quran rendering/data invariant regressed;
- backend build and all three frontend verification commands pass;
- the integrated browser smoke passes;
- every implementation phase has been reviewed when the owner requests it;
- the final owner-requested engineering review reports no open findings;
- this planning folder is deleted in a final pure-deletion commit before merge.
