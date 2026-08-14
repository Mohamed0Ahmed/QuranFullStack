# Linking scalability and responsiveness — performance audit

- Status: **pre-Spec Kit current-state report; not an implementation plan**
- Date: 2026-08-14
- Audited repository snapshot: `651c4c61`
- Verdict: **CHANGES REQUESTED**

The linking workflow is functionally capable of handling a source containing 1,821 ayahs, but its
current runtime shape is not safe or responsive at that scale. The delay is not explained by one
slow query, one missing spinner, or a conventional subscription leak. It is the combined result of:

1. a backend writer that persists independent ayahs one unit at a time, producing thousands of
   sequential database phases inside one transaction;
2. a frontend state model that materializes several full copies of the same ayahs and words and
   rebuilds them after small selection changes;
3. unbounded rendering paths that create tens of thousands of Quran word and ayah elements when a
   source or grouped result is opened;
4. count-based caches and eager request fan-out that do not provide a real memory bound; and
5. a synchronous request contract that expands and transports the full operation twice, then makes
   the user wait for all persistence work before it can report success.

This report records the confirmed causes, the risks at larger scale, and the recommended target
architecture so that a separate implementation plan can be derived from it.

---

## 1. Review question and scope

The review answers four questions:

1. Why does opening source ayahs or merged ayahs feel delayed?
2. Why does confirming the link to a door take a very long time for 1,821 ayahs?
3. Can the current frontend memory/cache design crash a user's tab or degrade other users?
4. What architecture keeps the workflow responsive if the operation grows beyond 1,821 ayahs,
   even if that requires a substantial cross-stack change or a few more requests?

### Scope reviewed

Backend:

- linking preflight preparation and classification;
- confirmation request mapping, validation, idempotency, transaction, and locking;
- unit, contribution, orphan, and derived door-state persistence;
- EF Core tracking and `SaveChanges` behavior;
- query shapes and relevant entity indexes;
- source-resolution cache shape and current operation limits.

Frontend:

- direct-link and workspace source resolution;
- source configuration, selection, merging, and operation-intent construction;
- source and merged preflight viewers;
- independent, grouped, and manual-word editors;
- virtual-scroll coverage and the measured-row strategy;
- frontend response caches, request fan-out, and workspace mutation queue;
- loading, preparing, submitting, and lifecycle behavior.

### Evidence boundary

This is a static code-path audit. No application server, browser profiler, or live database
benchmark was started for this report.

- The backend command counts are static path counts for the most relevant scenario: a fresh source,
  an empty door, 1,821 independent units, and matched word contributions.
- The DOM counts are structural estimates using 1,821 ayahs and an approximate 10–15 rendered words
  per ayah. They are not a browser heap measurement.
- Exact latency, retained bytes, connection-pool behavior, and production percentiles remain
  unmeasured. The first implementation phase must establish those baselines.
- The findings identify real loops, allocations, requests, and render paths. Proposed numerical
  performance gates later in this document are initial targets to calibrate under production-like
  conditions, not claims about current measured performance.

---

## 2. Executive diagnosis

### 2.1 Symptom-to-cause map

| User-visible symptom | Immediate cause | Confirmed underlying cause |
|---|---|---|
| Opening “source ayahs” freezes or feels unresponsive | Angular must synchronously create a very large view | Several preflight and grouped paths do not virtualize ayahs or words |
| Opening “all ayahs after merge” is slow in grouped mode | The grouped branch bypasses the existing virtual list | Group semantics are coupled to one giant DOM article |
| A selection or word click can lag | The operation is rebuilt before the next user action completes | Full source configuration, merge, sort, and intent construction run after small changes |
| The tab's memory rises substantially | Multiple object graphs represent the same Quran data at once | DTO cache, raw model, configured model, merged words, preview, intents, preflight, and request bodies coexist |
| “Confirm link” waits a very long time | The server performs thousands of sequential database phases | One lookup and up to three `SaveChanges` calls per independent unit |
| A second write to the same door waits | The door row remains locked during the whole heavy operation | `FOR UPDATE` is acquired before loading, classifying, writing, rebuilding, and storing the result |
| Production is likely to amplify the delay | Network, concurrent users, pool pressure, and lock contention are added | The current algorithm already does excessive local work and is not bounded by operation complexity |
| A spinner may not appear when a list opens | The browser does not get a paint opportunity | The expensive DOM construction happens in the same main-thread task as the click |

### 2.2 Primary conclusion

The confirmation delay for 1,821 independently linked ayahs is primarily a **backend persistence
algorithm problem**. The opening and interaction lag is primarily a **frontend data-shape and
rendering problem**. The two amplify each other through an oversized synchronous preflight/confirm
contract.

A spinner is required for honest feedback during real asynchronous work, but it cannot make the
current operation safe:

- it does not reduce thousands of database calls;
- it does not prevent an HTTP timeout;
- it does not reduce the browser heap;
- it cannot paint while the main thread is building tens of thousands of nodes; and
- it cannot protect the server from several users confirming large operations concurrently.

The correct direction is to reduce the work first, bound the remaining work, and expose real
preparing/progress states around it.

---

## 3. Scale model for the reported 1,821-ayah case

### 3.1 Why the link shape matters

Automatic and manual-independent linking create one linking unit per ayah:

- `Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-intents.ts:17-33`

Therefore 1,821 selected ayahs become 1,821 independent units. A manual-grouped selection instead
represents the ayahs as one unit. The difference in runtime between these shapes is expected under
the current writer, but it should not be this large: correctness semantics must not determine
whether the system performs thousands of database round trips.

### 3.2 Static backend work count

The unit synchronization loop is at:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.Persistence.cs:130-163`

For every unit, it performs an indexed lookup at lines 134–137. A new unit then causes:

- one `SaveChanges` after inserting the unit at line 184;
- one `SaveChanges` after inserting its ayahs at line 202; and
- one `SaveChanges` after inserting words/descriptions at line 226.

For a fresh 1,821-unit source with word contributions:

| Static count | Approximate value |
|---|---:|
| Independent units | 1,821 |
| `SaveChanges` calls inside new-unit creation | 5,463 |
| Total `SaveChanges` calls including surrounding operation work | about 5,470 |
| Per-unit lookup queries | 1,821 |
| Query phases including surrounding state queries | about 1,831 |
| Sequential database I/O phases before the response | about 7,300 |

These are not 7,300 logical rows; they are approximately 7,300 separate opportunities to pay
command preparation, database execution, app/database transport, EF bookkeeping, and async
scheduling costs. A production deployment does not need high internet latency between the browser
and API for this to become slow: even small app-to-database latency multiplied thousands of times
dominates the response.

The unique index used by the unit lookup is already appropriate:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Linking/LinkingUnitConfiguration.cs:52`

Adding another ordinary index does not remove the per-unit loop and is not the root solution.

### 3.3 Static frontend render scale

The source preflight viewer renders every item without virtual scrolling:

- `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-ayah-viewer/linking-preflight-ayah-viewer.component.html:50-70`

The group component also builds every ayah and word:

- `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-ayah-group/linking-preflight-ayah-group.component.html:14-32`

The grouped merged path bypasses the virtual path:

- `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-merged-ayah-viewer/linking-preflight-merged-ayah-viewer.component.html:33-39`

At 1,821 ayahs, the current structure is estimated to create:

- roughly 35,000–44,000 elements for the independent preflight presentation; or
- roughly 22,000–31,000 elements for the grouped presentation;

plus Angular component/view objects, text nodes, event bindings, accessibility nodes, Quran font
shaping, RTL layout, and wrapping. The exact browser heap is unknown, but this is already enough to
produce long tasks and can trigger a tab out-of-memory kill on a low-memory device.

---

## 4. Backend findings

### B1 — MAJOR: persistence is N units × several database saves

Evidence:

- the sequential unit loop is at
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.Persistence.cs:130-163`;
- every unit performs a lookup at lines 134–137;
- every new unit invokes saves at lines 184, 202, and 226.

Runtime impact:

- database latency is multiplied by the number of independent ayahs;
- EF must prepare and execute thousands of change batches;
- the request remains open until all calls finish;
- the same algorithm becomes worse with more than 1,821 ayahs.

Direction:

- compute all required unit identities first;
- query all existing matching units in one bounded query/set;
- insert missing units and child rows in set-based or batched form;
- attach contribution links in batches;
- keep the number of database phases approximately constant or in a small bounded number of
  batches, while allowing row count to grow linearly.

### B2 — MAJOR: repeated saves scan a growing tracked graph

The confirmation writer loads the locked door state into one tracked `DbContext`:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.State.cs:31`

It then adds the operation graph, units, ayahs, words, links, and derived door state to the same
context. In the 1,821-ayah case, a plausible tracked graph exceeds 10,000 entities before accounting
for every existing door row.

Each `SaveChanges` performs change detection over a graph that is getting larger. The database
context also performs audit inspection:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs:109`

Runtime and memory impact:

- CPU and allocation cost become cumulative rather than merely linear;
- garbage collection becomes more frequent;
- one large operation consumes substantial server memory;
- multiple large operations on different doors multiply memory and connection pressure for all
  users, even though their door locks do not conflict.

Direction:

- separate read/projection state from the write graph where possible;
- avoid tracking the complete door hierarchy merely to calculate a delta;
- write bounded batches or set-based statements;
- clear or isolate staging/tracking between phases when correctness permits;
- measure allocated bytes and peak tracked-entity count as explicit performance signals.

### B3 — MAJOR: the door lock covers the full expensive workflow

The transaction starts at:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.cs:26`

The door is locked with `FOR UPDATE` at:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.State.cs:13-23`

Commit does not occur until:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.cs:148`

The lock is held while the code loads state, reclassifies, validates the token, performs all
per-unit writes, removes orphans, rebuilds door state, and stores the result.

Concurrency impact:

- a second confirmation/write for the same door waits for the entire operation;
- requests for other doors can proceed, but thousands of commands per request compete for the
  database pool, CPU, I/O, and server heap;
- a slow or timed-out client request can leave expensive server work in progress until cancellation
  is observed or the transaction ends.

Direction:

- prepare deterministic source/unit data outside the final lock;
- acquire the door lock only for version revalidation and the atomic final merge;
- retain one atomic visible commit;
- serialize same-door large jobs deliberately instead of relying on a long accidental row-lock wait;
- cap global worker concurrency to protect the database.

### B4 — MAJOR: small source edits replace and rebuild large state

Updating a contribution removes its current unit links and synchronizes the full replacement:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.Persistence.cs:42-53`

The writer later reloads all units, ayahs, and words to rebuild derived door state:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.DoorState.cs:14-36`

Door-word synchronization performs a repeated linear lookup:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.DoorState.cs:58-62`

Impact:

- changing one ayah in a large source can repeat work for all 1,821 ayahs;
- rebuilding the full door grows with historical door content, not just the current change;
- repeated `First` lookups can make the word projection trend toward quadratic work.

Direction:

- calculate a true contribution diff;
- add/remove only changed unit links;
- delete only newly orphaned units using set-based operations;
- update derived door ayahs/words only for affected ayah IDs;
- index in-memory lookup structures once instead of repeatedly scanning collections.

### B5 — MAJOR: classification contains scale-dependent quadratic paths

Grouped-unit comparison searches the comparison ayahs repeatedly:

- `Backend/application/QuranDashboard.Application/Linking/LinkingOperationClassifier.cs:137`

Overlap classification scans existing contributions for each requested ayah:

- `Backend/application/QuranDashboard.Application/Linking/LinkingOperationClassifier.cs:177-191`

Impact:

- this is not the leading cause on an empty door, where B1 dominates;
- it becomes material as the door accumulates sources, units, and overlapping ayahs;
- optimizing persistence alone may expose classification as the next bottleneck.

Direction:

- build dictionaries/sets keyed by ayah ID and unit identity once;
- classify requested ayahs in linear or near-linear passes;
- add scenarios with a populated, overlapping door to performance gates.

### B6 — MAJOR: no total synchronous-operation complexity budget

Current limits include 3,000 resolved ayahs per source and 100 workspace sources:

- `Backend/application/QuranDashboard.Application.Abstractions/Linking/LinkingLimits.cs:9-11`

The operation validator does not enforce or route on total:

- sources;
- units;
- ayahs across sources;
- words/descriptions;
- estimated row mutations; or
- predicted synchronous cost.

Relevant validation entry point:

- `Backend/application/QuranDashboard.Application/Linking/LinkingOperationValidation.cs`

Impact:

- a functionally valid operation can be inappropriate for a synchronous HTTP request;
- a per-source ayah limit does not bound a multi-source operation;
- raising the current limit without changing the algorithm increases timeout and resource-exhaustion
  risk.

Direction:

- introduce a measured complexity score;
- retain an absolute safety limit for invalid/abusive requests;
- route work below a calibrated budget through a fast synchronous path;
- route legitimate larger work through a durable asynchronous job path rather than rejecting it.

### B7 — MINOR: preflight and confirmation both expand the full operation

The confirmation body contains units, ayahs, words, and descriptions:

- `Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingOperationBodies.cs:37`

The API mapper copies the expanded lists:

- `Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingOperationBodyMapper.cs:258`

Confirmation resolves/prepares the operation again:

- `Backend/application/QuranDashboard.Application/Linking/Commands/ConfirmLinkingOperation/ConfirmLinkingOperationHandler.cs:36`

The confirmation response itself is small:

- `Backend/application/QuranDashboard.Application.Abstractions/Linking/Responses/LinkingConfirmationResultDto.cs:5`

Impact:

- the long wait for “linked successfully” is not caused by a large response;
- the client pays serialization and upload cost twice;
- the API repeats resolution/allocation work already performed for preflight;
- browser-to-API latency and payload parsing amplify the issue in production.

Direction:

- create a server-owned prepared preflight resource;
- confirm by compact identifier, token, and idempotency key;
- page detail views independently of the confirmation command.

### B8 — NOTE: backend source-cache weight is not a memory bound

The source cache allows 60,000 source-ayah slots and 6,500 text ayahs:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/LinkingSourceCacheEntryOptions.cs:9-15`

The resolved-source entry weight is set from ayah count:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/LinkingSourceResolutionCache.cs:91`;
- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/LinkingSourceResolutionCache.cs:110`.

The text cache assigns every ayah a fixed weight of one:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/LinkingAyahTextCache.cs:7`;
- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/LinkingAyahTextCache.cs:51-54`.

Those weights do not account for actual word count, strings, source-membership arrays, or object
overhead. This is not the cause of thousands of confirmation writes, and it should not be changed
speculatively. It needs measurement and a weight based on estimated retained bytes or at least
ayah/word cardinality.

### Backend strengths to preserve

- Source hydration is batched rather than N+1:
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/LinkingAyahHydration.cs:22`
  and
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/AyahWordHydration.cs:15-35`.
- The unit identity index matches the current lookup:
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Linking/LinkingUnitConfiguration.cs:52`.
- Preflight reads use projections/`AsNoTracking` where appropriate.
- Idempotency is protected by a unique index:
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Linking/LinkingOperationConfiguration.cs:60`.
- The token, door/contribution versions, hash-collision validation, and atomic transaction are
  correctness controls, not performance defects. They must remain in the target design.

---

## 5. Frontend findings

### F1 — MAJOR: several large viewers bypass virtualization

Confirmed full-DOM paths:

- source preflight:
  `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-ayah-viewer/linking-preflight-ayah-viewer.component.html:50-70`;
- source/group words:
  `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-ayah-group/linking-preflight-ayah-group.component.html:14-32`;
- grouped merged preflight:
  `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-merged-ayah-viewer/linking-preflight-merged-ayah-viewer.component.html:33-39`;
- grouped selection editor:
  `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-selection/linking-ayah-selection.component.html:25-51`;
- grouped word buttons:
  `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-group/linking-ayah-group.component.html:8-34`;
- manual word editor:
  `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-manual-word-editor/linking-manual-word-editor.component.html:30-65`.

The ordinary independent and merged branches already have virtual-scroll implementations; the
problem is inconsistent coverage.

Impact:

- DOM size grows with the complete source, not the visible viewport;
- Quran font shaping, RTL layout, wrapping, bindings, focusable buttons, and accessibility tree
  construction all happen at once;
- low-memory users can experience long freezes or a browser tab crash;
- grouped mode remains slow even when its semantic result could be represented by the same
  virtualized rows.

Direction:

- create one shared virtual ayah-row host used by every source, grouped, merged, and manual path;
- treat the ayah as the virtualization unit: every resident ayah must render all of its verified
  words together and in authoritative order; do not virtualize individual Quran words;
- keep group identity and boundaries in the data/header, not by requiring all group rows in one DOM
  subtree;
- keep approximately a viewport plus overscan mounted regardless of total ayah count;
- preserve stable ayah/word IDs, Quran font, RTL, highlighting, keyboard behavior, and accessible
  position metadata when rows are recycled.

### F2 — MAJOR: small selection changes rebuild the full operation

Every direct configuration change reaches `reconfigure`:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts:425-434`

The coordinator reapplies configuration, merges sources, and rebuilds intents:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-set.coordinator.ts:101-116`;
- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-set.coordinator.ts:132-152`;
- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-set.coordinator.ts:186-224`.

Configuration copies each ayah and its words:

- `Frontend/quran-dashboard-ui/src/app/features/linking/utils/apply-linking-source-configuration.ts:13-23`

Merge creates additional word objects:

- `Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-merge.ts:11-37`;
- `Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-merge.ts:81-106`.

Intent creation creates and sorts units for the operation:

- `Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-intents.ts:11-33`

The direct preview adds another configured copy:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts:105-110`

Impact:

- toggling one ayah or word can allocate tens of thousands of temporary objects;
- the browser pauses for garbage collection even if the visible rows are virtualized;
- in the direct editor, the lag grows with the currently edited source rather than only the row;
  when a multi-source workflow resolves/advances, expansion grows with the selected sources.

The workspace editor separately rebuilds the active source:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-editor.facade.ts:49-55`

and then queues the full-state persistence described in F6. It does not rebuild every cached source
on each workspace click.

Direction:

- store Quran ayahs/words once in an immutable entity-normalized store keyed by stable IDs;
- represent inclusion, grouping, word choices, and highlights as compact `Set`/`Map` overlays;
- derive only visible row state during interaction;
- build the expanded operation snapshot once when advancing to preflight, not on each click;
- use memoized keyed selectors rather than one global recomputation fan-out.

### F3 — MAJOR: the source cache is count-bounded, not memory-bounded

The cache allows six complete source responses:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source.cache.ts:5-16`

The generic cache stores complete `ApiResponse` values and evicts by entry count:

- `Frontend/quran-dashboard-ui/src/app/core/caching/api-response-cache.ts:7-79`

It has no:

- ayah/word/estimated-byte weight;
- TTL;
- Quran data-version key;
- explicit clear operation; or
- separation between durable canonical display pages and transient operation previews.

A cache hit also maps the full DTO tree back into a new linking tree:

- `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts:24-35`;
- `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts:64-81`.

Impact:

- six small entries and six 3,000-ayah entries have radically different memory cost;
- the root-scoped cache can retain large responses after the linking modal closes;
- “cache hit” avoids the network but still pays a full object-tree allocation.

Direction:

- use one entity-normalized canonical Quran page store;
- weight pages by word count or estimated retained bytes;
- include data version in the key;
- add bounded LRU/TTL and explicit transient cleanup on dismiss/success;
- give fallback preview pages a separate small cache so they cannot evict useful canonical pages.

### F4 — MAJOR: multiple sources resolve eagerly and remain resident together

All accepted member sources are resolved with one eager `forkJoin`, without a client-side
concurrency cap inside the current workspace/backend source limits:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-set.coordinator.ts:85-95`

Results are retained in `rawAyahsBySourceKey`:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-set.coordinator.ts:173-183`

Impact:

- all requests start together;
- `forkJoin` retains every completed result until the slowest finishes;
- all source graphs then remain resident and are copied into configured/merged graphs;
- accepted worst-case multi-source shapes can approach hundreds of thousands of ayah objects and
  millions of word-related objects before details are opened; this is a theoretical structural
  bound, not a measured retained-heap count.

Direction:

- resolve descriptors/compact metadata first;
- limit text-page concurrency to a small calibrated number, initially 2–4;
- cancel stale work when the operation changes or closes;
- load detail pages only for the opened source/merged view, with adjacent-page prefetch.

### F5 — MAJOR: the same expanded graph is built and uploaded twice

Automatic sources expand to one intent unit per ayah:

- `Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-intents.ts:17-33`

The request mapper builds bodies for each unit/ayah:

- `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-operation-request.ts:19-29`

The full graph is sent for preflight:

- `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-preflight.api.ts:40`

It is rebuilt and sent again for confirmation:

- `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/http-linking-command.port.ts:23-46`

The generated preflight DTO also includes detailed word/description change arrays:

- `Frontend/quran-dashboard-ui/src/app/core/api/generated/models/linking-ayah-preflight-dto.ts:8`

Some of that detail is parsed and allocated but not retained by the frontend mapper:

- `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-preflight.api.ts:84`

Direction:

- post a compact source descriptor plus selection overlay to create preflight;
- return counts/summary and a prepared resource identifier first;
- fetch detailed ayah impact in pages only when the user opens it;
- confirm the prepared resource rather than sending the full graph again.

### F6 — MAJOR: workspace saving can accumulate full-state requests

Each workspace change builds a configuration request and queues it:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts:399-422`

Selected words are flattened into a new object array:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace-merge.ts:61-95`

The runner serializes queued closures without coalescing:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace-sync.runner.ts:120-141`

Every mutation sends full configuration and maps a full returned workspace snapshot:

- `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/http-linking-workspace.repository.ts:69-87`;
- `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/http-linking-workspace.repository.ts:125-135`.

Impact:

- rapid edits can retain several progressively larger request bodies;
- serial requests preserve stale intermediate states that the user no longer cares about;
- total memory and network work can trend toward `O(number of edits × full source size)`;
- with increasing selection size, the practical behavior can appear quasi-quadratic.

Direction:

- keep a local draft;
- coalesce unsent changes latest-wins per source;
- use a short debounce or explicit save boundary;
- flush before advancing/closing when required by product semantics;
- prefer a delta contract and return only the updated source/version rather than the full workspace.

### F7 — MINOR: no paintable preparing state before heavy view creation

The click immediately toggles the view:

- `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-step/linking-preflight-step.component.ts:50-55`

The heavy viewer is instantiated in the same render transition:

- `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-step/linking-preflight-step.component.html:57-61`;
- `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-step/linking-preflight-step.component.html:86-88`.

The existing skeleton represents API loading, not synchronous view preparation. If the data is
already ready, the browser may not paint a spinner before constructing the large DOM.

The direct workflow already has a text-shaped submitting skeleton:

- `Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/direct-link-workflow.component.html:111`

The target should evolve that state into real stages rather than introduce a second bespoke
spinner.

Direction:

1. enter an explicit `preparing` state;
2. render the shared row skeleton with `aria-busy`;
3. cross a verified committed-paint boundary, such as Angular `afterNextRender` followed by another
   animation frame, double `requestAnimationFrame`, or an equivalent proven by a browser trace;
4. split any remaining preparation into short tasks;
5. mount the virtualized viewer and transition to `ready`;
6. use the shared row skeleton for lists and a text/status skeleton for a single operation stage;
7. show real network/job progress during preflight and confirmation.

This improves feedback only after F1/F2 reduce the underlying work.

### F8 — MINOR: fallback preview pages pollute the primary cache

Fallback preview uses chunks of 1,000 and starts all chunks with `forkJoin`:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-preflight-preview.facade.ts:32`;
- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-preflight-preview.facade.ts:172-200`.

The chunks use the same six-entry source cache. A large fallback can evict useful source responses
while retaining its own complete preview map.

Direction:

- replace 1,000-item eager chunks with view-driven pages;
- isolate preview-page cache from source/descriptor cache;
- cap page concurrency and prefetch only adjacent pages.

### F9 — MINOR: one Map signal can invalidate every open source preview

All preview states live in one Map signal:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-preflight-preview.facade.ts:38-40`.

Every source update clones and replaces the Map:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-preflight-preview.facade.ts:203-215`.

Each open viewer derives its rows by reading that Map and scanning its source ayahs:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-preflight-preview.facade.ts:128-140`.

Impact: loading or filtering one source can invalidate computations for other open sources. This is
smaller than the full-DOM and object-copy findings, but it adds avoidable fan-out with several open
viewers.

Direction: use keyed per-source state/signals and selectors so one source update does not invalidate
unrelated source views.

### F10 — NOTE: the measured-row strategy recomputes all offsets during updates

Scroll and render events invoke the strategy update:

- `Frontend/quran-dashboard-ui/src/app/features/linking/utils/measured-row-virtual-scroll.strategy.ts:39-50`.

Each update rebuilds an offsets array and scans every measured row to recalculate the estimate:

- `Frontend/quran-dashboard-ui/src/app/features/linking/utils/measured-row-virtual-scroll.strategy.ts:135-155`.

Impact: this is approximately O(N) work per update. It is not the leading 1,821-ayah freeze while
full-DOM branches still exist, but it should be profiled at 6,236 ayahs and with several paged
viewers after F1 is fixed.

Direction: keep the virtualizer's data window bounded, or adopt an incremental measured/prefix-sum
strategy if profiling proves the full offset rebuild material.

### F11 — NOTE: a fresh source response is validated twice

The resolver validates a fresh response in `tap` and again in `map`:

- `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts:24-35`.

Validation creates a Set of word IDs per ayah on each pass:

- `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts:54-61`.

This is a minor transient allocation rather than a root cause. Preserve validation but validate a
fresh response once and pass the validated value into mapping.

### F12 — NOTE: no conventional unbounded subscription leak was found

Core request lifecycles cancel/reset correctly, and the measured-row strategy disconnects its
`ResizeObserver`. The leading memory issue is retained/copy-heavy state and full DOM, not a growing
listener count.

A minor exception exists in manual Mushaf metadata loading:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/manual-mushaf-selection.store.ts:101`

The generation guard prevents stale state updates, but removing/resetting an ayah does not cancel
the in-flight metadata request/cache population. This is worth cleaning up after the larger memory
problems.

### Frontend strengths to preserve

- Linking components use `OnPush`.
- Stable ayah/verse identifiers are used for list tracking.
- Direct preview, the ordinary independent editor, and the ordinary merged viewer already
  demonstrate virtualized paths.
- Core request cancellation and observer teardown prevent an obvious lifecycle leak.
- Existing Quran font, RTL, ordering, word highlight, and group semantics are correctness
  requirements and must remain unchanged.

---

## 6. User and production risk

### 6.1 Browser risk

The current design can affect a user in three increasingly severe ways:

1. **Interaction delay:** allocation, change detection, font shaping, and layout create long tasks.
2. **GC churn:** repeated full-source copies cause pauses and rising heap during a workflow.
3. **Tab termination:** unbounded DOM plus multiple retained source graphs can exceed the browser's
   memory budget, particularly on mobile or low-memory desktops.

This is not evidence that every 1,821-ayah operation will crash. It is evidence that the system has
no structural memory bound, so the outcome depends on device, browser, other tabs, source word
counts, number of open sources, and interaction history.

### 6.2 Server and multi-user risk

A single large confirmation can hold:

- a database connection and transaction;
- one door lock;
- a large tracked graph;
- thousands of sequential commands; and
- an open HTTP request.

Several operations on different doors do not block on the same row lock, but they compete for
server heap, CPU, database connections, and PostgreSQL I/O. Operations on the same door serialize
behind the long lock. Production concurrency therefore risks degrading unrelated users even if
local single-user testing eventually succeeds.

### 6.3 Why production can be worse

The exact production delta is unmeasured, but the current design is sensitive to:

- app-to-database latency multiplied by thousands of phases;
- browser-to-API upload latency for the expanded body twice;
- JSON parsing/allocation for large request and preflight detail graphs;
- database pool contention;
- same-door lock waits;
- lower-memory client devices; and
- reverse-proxy or platform request timeouts.

Response compression, if absent, could reduce transport bytes, but it cannot solve persistence
round trips, full object copies, or unbounded DOM. It is a supporting optimization to verify, not
the primary fix.

---

## 7. Recommended target architecture

### 7.1 End-to-end target

```text
Compact source descriptor + selection overlays
                    |
                    v
Create durable prepared preflight
                    |
                    +--> small work: lightweight summary when ready
                    |
                    +--> expensive preparation: 202 + preflightId/status/progress
                    |
                    +--> paged source/merged details on demand
                    |
                    v
Confirm { preflightId, token, idempotencyKey }
                    |
          +---------+---------+
          |                   |
          v                   v
  bounded sync path     durable async job
  for small work        for large work
          |                   |
          +---------+---------+
                    |
                    v
      short atomic final door transaction
```

This architecture intentionally allows a small increase in request count. Today, source resolution
downloads a complete Quran text/word graph for the source. Separately, preflight and confirmation
upload the expanded unit/ayah operation twice; manual operations include selected word IDs, while
automatic operations do not upload Quran text. A few cancelable, page-sized detail requests plus a
compact command are preferable to those large materializations because they bound parsing,
allocation, rendering, and timeout risk.

### 7.2 Canonical frontend data model

Store each Quran ayah/word once:

- entity-normalized immutable records keyed by `ayahId` and `wordId`;
- source membership represented by compact ID lists/ranges;
- ayah inclusion represented compactly as `mode: all-except | only` plus
  `overrideAyahIds: Set<AyahId>`, so “select all” does not expand to thousands of IDs;
- manually selected words represented by `Map<AyahId, Set<WordId>>`;
- automatic source-match highlights represented separately as
  `sourceIdentity -> ayahId -> matchedQuranWordIds`;
- grouping and link shape represented as metadata, not copied ayah objects;
- preflight classifications/highlights represented as ID-based overlays.

“Entity-normalized” refers only to storage and references. Quran strings and code points remain
byte-for-byte as supplied by the authoritative versioned source; this design does not normalize or
rewrite Quran text.

Benefits:

- selection changes update a small structure;
- visible rows look up their overlays without rebuilding all words;
- cached text pages can be evicted without losing the user's selection;
- multiple sources can share canonical Quran entities;
- operation expansion is deferred to a deliberate transition.

### 7.3 Paged and virtualized details

- Initial preflight response returns source totals, new/existing/conflict counts, and operation
  classification when preparation is small; expensive preparation first returns a durable
  `preflightId` and status/progress.
- Source and merged details are fetched only when opened.
- Use cursor/page sizes around 50–100 ayahs initially, then calibrate.
- Prefetch at most the adjacent page.
- Every UI path uses the same virtual ayah-row host. The ayah is the virtualization unit, and every
  resident ayah renders its complete verified word sequence in authoritative order.
- The mounted row count stays O(viewport + bounded overscan); approximately 20–40 rows is an initial
  benchmark target to calibrate for defined viewport sizes and variable ayah heights.
- Grouped mode exposes group context through a header/sticky boundary while rows remain virtual.
- Cursors use stable authoritative Quran order and are bound to `preflightId` plus the Quran data
  revision. Page traversal must neither skip nor duplicate ayahs.
- Selection, automatic-match highlights, and group membership remain ID-based and independent of
  page residence.

### 7.4 Prepared preflight resource

Create a durable PostgreSQL-backed preflight resource containing:

- `preflightId`;
- normalized operation intent or a deterministic reference to it;
- door and contribution/source versions;
- the trusted Quran dataset/source revision or provenance hash used to resolve it;
- token and classification summary;
- creator/context ownership;
- creation/expiry timestamps;
- processing state if preparation itself is asynchronous.

The prepared resource must be immutable after creation. Its token must bind the canonical request,
normalized intent, and trusted Quran source revision; it must not be possible to mutate the intent
behind an already-issued token. The resource must not rely only on an in-memory cache because the
API may restart or run multiple instances.

Confirmation becomes:

```json
{
  "preflightId": "...",
  "token": "...",
  "idempotencyKey": "..."
}
```

Under the final lock, the backend still revalidates the door/contribution versions and token. If the
trusted Quran dataset/source revision changed after preparation, confirmation must either resolve
again server-side and issue a new preflight or reject the prepared operation as stale. The compact
contract removes duplicate upload/resolution without weakening the current server-side source
validation or stale-operation protection.

### 7.5 Set-based confirmation writer

Recommended write sequence:

1. Prepare and hash the requested units outside the final door lock.
2. Acquire the lock and revalidate the prepared operation against current versions.
3. Fetch all existing required unit identities in one set query.
4. Insert missing units in one or a small bounded number of batches.
5. Insert unit ayahs, words/descriptions, and contribution links in bounded batches.
6. Apply a true source contribution diff.
7. Delete newly orphaned units set-wise.
8. Update derived door ayahs/words only for affected ayah IDs.
9. Store the idempotent outcome and commit once atomically.

The implementation may use EF batching, set-based SQL, or staging/COPY where measurements justify
it. The architectural requirement is bounded database phases and one atomic visible result, not a
specific persistence library trick.

### 7.6 Adaptive synchronous and asynchronous execution

Use a measured complexity score based on units, ayahs, words, descriptions, expected mutations, and
door state size.

- Below the calibrated threshold: execute the optimized writer synchronously.
- Above the threshold: accept a durable job quickly and return `202 Accepted` with `jobId`.

Large-job flow:

1. create an idempotent job;
2. prepare normalized rows in bounded-memory stages outside the final door lock;
3. expose real stage/item progress;
4. serialize jobs for the same door;
5. bound global worker concurrency;
6. acquire the door lock for a short final revalidation/merge transaction;
7. publish one atomic result;
8. retain/query the result by idempotency key if the client disconnects.

Staging rows are non-authoritative preparation data. They must not participate in any live linking
read or become visible as door state before the final transaction commits successfully.

Polling is acceptable and simpler than SSE for the first version because the user explicitly
accepts a small increase in requests. The plan should choose polling versus SSE using expected job
duration and infrastructure support.

Cancellation is safe only before the atomic final merge starts. Once commit outcome is uncertain
to the client, idempotent status lookup must replace a blind retry.

### 7.7 Bounded frontend cache and request policy

- Cache canonical ayah pages, not complete operation-specific source graphs.
- Weight entries by ayah/word count or estimated bytes.
- Include Quran data version in cache keys.
- Use a small page budget and LRU/TTL.
- Clear transient selection/preflight overlays on dismiss/success.
- Keep selection IDs even if display pages are evicted.
- Limit page/source concurrency and cancel stale work.
- Coalesce workspace writes latest-wins per source.
- Return updated source/version rather than the full workspace snapshot where possible.

### 7.8 Honest loading and progress states

The frontend state model should distinguish:

- `preparing` — yielding and constructing compact local state;
- `loading-details` — fetching a page;
- `preflighting` — server is preparing/classifying;
- `ready` — summary is usable;
- `submitting` — synchronous confirmation;
- `queued/running/finalizing` — asynchronous job stages;
- `succeeded`, `failed`, and safely `cancelled`.

Use the shared row skeleton and `aria-busy`. A spinner should never imply cancellability once the
atomic final transaction has started, and progress must be tied to real stages or processed counts,
not a fabricated timer. The existing submitting skeleton should be extended into these states;
`qd-panel-skeleton` should use rows for list preparation/loading and text/status for a single
operation stage rather than adding a custom spinner.

---

## 8. Recommended decisions for the future plan

| Decision | Recommendation | Reason |
|---|---|---|
| Fix only the spinner? | Reject | It cannot reduce work, memory, lock duration, or timeout risk |
| Increase the six-entry frontend cache? | Reject | Entry count is not a memory budget |
| Add another unit index? | Reject as primary fix | The existing identity index fits; round-trip count is the problem |
| Force grouped links for speed? | Reject | Link semantics must follow user intent, not persistence limitations |
| Split one link into user-visible partial commits? | Reject | It weakens atomicity and can expose incomplete Quran linkage |
| Virtualize all ayah-row display paths? | Adopt | DOM must stay bounded while each resident ayah keeps all of its words |
| Normalize Quran data and store selection as IDs? | Adopt | Eliminates repeated full-object copies and supports paging |
| Use several small paged requests? | Adopt | Bounded, cancelable work is safer than one oversized graph |
| Prepare preflight server-side and confirm by ID/token? | Adopt | Removes duplicate expansion and enables paged details/jobs |
| Replace per-unit persistence with set/batch operations? | Adopt urgently | It addresses the primary confirmation bottleneck |
| Use sync for all valid operations? | Reject | Validity does not imply suitability for one HTTP request |
| Add durable async jobs above a measured budget? | Adopt | Supports larger legitimate links without timeouts or resource spikes |

---

## 9. Recommended delivery sequence

This is sequencing guidance for the later plan, not a task breakdown.

### Phase 0 — baseline and observability

Instrument before trusting any optimization:

- client payload bytes, response bytes, request count, and cancellation;
- long tasks, INP, render time, DOM node count, and heap snapshots;
- source resolution and object-allocation hotspots;
- backend stage durations;
- database command count and affected rows;
- lock wait and lock-held duration;
- peak tracked entities/allocated bytes;
- cache hit/miss and weighted occupancy;
- operation shape: sources, units, ayahs, words, descriptions, existing door size.

Baseline scenarios:

1. 1,821 automatic/independent ayahs on an empty door;
2. 3,000 automatic/independent ayahs;
3. 1,821 manual-grouped ayahs;
4. one-ayah/one-word update inside a 1,821-ayah contribution;
5. several overlapping sources on a populated door;
6. a multi-source operation whose aggregate approaches the complete Quran;
7. two confirmations for the same door;
8. confirmations for different doors;
9. browser runs on a representative low-memory device.

### Phase 1 — immediate containment

- Replace per-unit persistence with a batched/set-based writer.
- Implement true source diff and affected-only door-state update.
- Remove quadratic classifier/door-word lookups.
- Virtualize every source, grouped, merged, and manual-word path.
- Add paintable `preparing` and real `submitting` states.
- Coalesce workspace writes.
- Bound source/page request concurrency.
- Add weighted cache cleanup.

This phase should make 1,821 ayahs usable without waiting for the full contract redesign.

### Phase 2 — entity-normalized frontend state

- Introduce canonical immutable Quran entities.
- Replace configured/merged/full-preview copies with ID overlays.
- Defer operation snapshot creation until the user advances.
- Split keyed per-source view state to reduce recomputation fan-out.
- Make detail paging and virtual rows the only rendering path.

### Phase 3 — compact prepared-preflight contract

- Add the durable preflight resource and expiry cleanup.
- Send source descriptors and compact selection overrides.
- Return summary first.
- Add paged source/merged detail endpoints.
- Confirm by `preflightId`, token, and idempotency key.
- Update generated frontend contracts and integration boundaries.

This is an XL cross-stack change and likely includes a database migration.

### Phase 4 — durable large-operation jobs

- Define the complexity score and calibrated threshold.
- Add job persistence, worker, same-door serialization, global concurrency limit, and status API.
- Stage large work with bounded memory.
- Keep final lock/transaction short and atomic.
- Add polling or SSE progress and disconnect-safe result recovery.

### Phase 5 — enforce performance gates

- Convert the baseline scenarios into repeatable backend PostgreSQL and browser performance lanes.
- Fail on command-count, DOM-bound, payload, or regression thresholds where deterministic.
- Record environment-sensitive latency/heap trends without pretending they are deterministic unit
  tests.
- Run a formal backend and Angular performance review before feature completion.

---

## 10. Proposed acceptance gates

These are initial targets for planning and must be calibrated with Phase 0 evidence.

### Frontend

- Mounted ayah rows stay O(viewport + bounded overscan), with an initial 20–40-row safety target
  tested against defined viewport sizes and variable ayah heights, regardless of total aggregate
  size.
- No source/grouped/manual viewer renders all ayahs or words from non-resident ayahs; each resident
  ayah renders its complete authoritative word sequence.
- A selection toggle does not rebuild every ayah/word or operation intent.
- Interaction tasks should stay below 50 ms where possible; representative INP should remain below
  200 ms.
- Closing/dismissing the workflow releases operation-specific graphs; retained heap is bounded by
  the documented canonical page-cache budget rather than total source size.
- Source/details loading is cancelable and concurrency-bounded.
- Workspace rapid edits coalesce; queued request count does not grow with every click.
- Quran ordering, text, font, tashkeel, word IDs, highlights, grouped semantics, RTL, keyboard use,
  and accessibility remain correct under row recycling and page eviction.

### Backend

- A fresh 1,821-independent-ayah confirmation uses database phases measured in tens, not thousands.
- `SaveChanges`/database command count is bounded by batches, not units.
- A one-ayah update inside a large contribution mutates only the true delta and affected derived
  rows.
- The final door-lock duration covers revalidation and atomic merge, not source hydration or
  thousands of per-unit saves.
- Same-door work is deliberately serialized; different-door work is globally bounded.
- The optimized synchronous path meets a calibrated production-like percentile target; an initial
  candidate is p95 at or below 2 seconds for the 1,821 scenario after warm-up.
- Work above the synchronous budget returns a durable `jobId` quickly; an initial candidate is
  within 500 ms, followed by real progress/status.
- A client timeout/disconnect cannot create duplicate work; any ambiguous commit outcome is
  recoverable through idempotent status lookup.
- Idempotency, token/version validation, unit identity collision checks, and atomicity remain intact.

### Contract and payload

- Confirmation does not upload the complete expanded unit/ayah operation after successful
  preflight.
- Initial preflight does not return all detailed ayah/word impacts unless explicitly requested in
  pages.
- Page size, concurrency, and cache weight have explicit server/client bounds.
- Detail cursors are stable in authoritative Quran order, bound to the prepared operation and data
  revision, and cannot skip or duplicate ayahs.
- Operation complexity is observable and determines sync versus async execution.
- No valid large operation depends on an indefinitely open HTTP request to finish.

---

## 11. Quran data and rendering safety

Performance work must not:

- invent, normalize, truncate, reorder, or silently correct Quran text;
- trust client-supplied Quran text or IDs in place of server-side source validation;
- remove source/version/hash validation;
- weaken preflight token or stale-door/contribution detection;
- expose partial link state through intermediate commits;
- alter word/ayah identity, selection, highlight, or group semantics;
- substitute a lighter but incorrect Quran font;
- break tashkeel, RTL order, keyboard navigation, or accessibility;
- show plausible fallback Quran text while a page is unknown/loading.

Paging and virtualization control only which verified rows are resident/rendered. They must not
change the complete logical selection or the authoritative operation content.

Atomicity, idempotency, provenance, and Quran rendering correctness take precedence over a latency
target. The target architecture improves performance without trading away those guarantees.

---

## 12. Plan input: proposed scope and boundaries

### Proposed feature objective

Make linking responsive and resource-bounded from small selections through large multi-source
operations, while preserving exact Quran data, link semantics, preflight safety, idempotency, and
one atomic visible result.

### Required capability outcomes

1. Bounded frontend DOM and retained memory.
2. O(visible rows) interaction rendering.
3. Compact selection state with deferred operation expansion.
4. Bounded/cache-aware detail loading.
5. Batched or set-based backend persistence.
6. Short final door lock.
7. Compact prepared-preflight confirmation.
8. Adaptive synchronous/asynchronous execution.
9. Real loading/progress/status feedback.
10. Measured regression gates for 1,821 and larger scenarios.

### Explicit non-goals

- changing the meaning of automatic, independent, or grouped linking;
- changing Quran source data;
- solving performance by reducing displayed text fidelity;
- removing correctness validation;
- allowing partially committed linking;
- redesigning unrelated Quran, Abwab, or workspace features;
- treating an arbitrary smaller ayah limit as the final scalability solution.

### Decisions the plan must lock

1. Prepared-preflight storage schema and expiry/cleanup policy.
2. Compact descriptor and selection-overlay API contracts.
3. Detail pagination/cursor contract and initial page size.
4. Sync/async complexity-score inputs and initial threshold.
5. Job status transport: polling first or SSE.
6. Same-door serialization and global worker concurrency policy.
7. Set-based writer technique: EF batching, explicit SQL, or staging/COPY.
8. Canonical frontend entity-store ownership and cache budget.
9. Workspace draft/coalescing semantics and flush boundary.
10. Production-like benchmark environment and enforceable performance budgets.

### Likely affected areas

Backend:

- `Backend/application/QuranDashboard.Application/Linking/`;
- `Backend/application/QuranDashboard.Application.Abstractions/Linking/`;
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/`;
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/`;
- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/`;
- `Backend/api/QuranDashboard.Api/Contracts/Linking/`;
- linking controllers/endpoints and generated OpenAPI;
- linking persistence entities/configurations/migrations for prepared resources/jobs;
- PostgreSQL integration and performance tests.

Frontend:

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/`;
- `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/`;
- `Frontend/quran-dashboard-ui/src/app/features/linking/utils/`;
- all source, grouped, merged, preflight, and manual-word linking components;
- `Frontend/quran-dashboard-ui/src/app/core/caching/`;
- generated API models;
- feature and retained browser performance journeys.

### Dependencies and sequencing constraints

- Backend batching can start before the new API contract and should not wait for it.
- Full frontend normalization can start behind the existing contract but must anticipate paged
  details.
- Prepared preflight must preserve current token/version/idempotency behavior before confirmation
  switches to the compact body.
- Async jobs depend on the batched writer and durable prepared resource; they should not automate
  the current per-unit algorithm.
- Performance gates need baseline instrumentation before final thresholds are locked.

---

## 13. Final verdict

**CHANGES REQUESTED.**

The current workflow has real scalability defects on both sides of the API:

- the backend confirmation path can execute approximately 7,300 sequential database I/O phases for
  the reported 1,821-independent-ayah case;
- the frontend can retain several complete representations of the same Quran content and rebuild
  them after a small edit;
- several viewers can construct tens of thousands of DOM elements at once;
- caches and request fan-out are not bounded by memory or operation complexity; and
- the synchronous expanded contract repeats work and provides no safe path for larger legitimate
  operations.

The preferred solution is a deliberate cross-stack redesign: entity-normalize storage references
without changing Quran strings, page Quran display data, virtualize every large view by ayah row,
model selection as compact ID overlays, batch/set-write the backend, prepare preflight durably,
confirm by identifier/token, and route work above a measured budget to a durable job with real
progress and a short atomic final transaction.

Adding a spinner is part of the user experience, but it is not the performance fix.
