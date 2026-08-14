# Linking scalability — full direct implementation plan

- Status: **ready for phased implementation**
- Type: **ordinary implementation plan; no Spec Kit workflow**
- Input: linking-performance-audit-report.md
- Delivery shape: **seven implementation phases**
- Manual verification owner: **the user after Phase 7; it is not an implementation phase**

## 1. Objective

Make linking resource-bounded from a direct source through large multi-source operations while
preserving exact Quran data, current link semantics, owner authorization, stale-operation safety,
idempotency, and one atomic visible confirmation result.

The completed implementation must remove the current full-graph workflow:

    compact source/configuration state
            |
            v
    paged source details
            |
            v
    durable prepared preflight
            |
            v
    paged preflight details
            |
            v
    durable confirmation job
            |
            v
    short atomic door merge

The target covers every Backend finding B1–B8 and Frontend finding F1–F12 in the audit. It includes
the API, persistence schema, generated migrations, background processors, Frontend state, cache,
request lifecycle, and virtual rendering changes required for the complete solution.

## 2. Hard boundaries

### In scope

- batched and set-based Backend classification and persistence;
- a persisted linking-data revision used by pages, caches, prepared resources, and tokens;
- durable prepared-preflight tables and a database-leased preparation processor;
- compact source/configuration contracts and revision-bound numbered source/preflight pages;
- a delta workspace-configuration contract;
- durable confirmation jobs with database leasing and same-door serialization;
- additive API rollout followed by a Frontend cutover and legacy-contract removal;
- canonical Frontend Quran entities and ID-only selection/classification overlays;
- separate weight-bounded source-page and prepared-detail caches;
- bounded, coalesced, cancelable request scheduling;
- one page-backed virtual ayah host for every large linking viewer; and
- real preparation/job states using existing skeleton and notice patterns.

### Out of scope

- Spec Kit artifacts or commands;
- benchmarks, profiling, heap/DOM capture, latency budgets, performance counters, or calibration;
- Playwright, a browser run, or an agent-owned manual-verification phase;
- new automated test classes or test methods;
- SSE or WebSockets;
- a second sync path selected by an unmeasured complexity threshold;
- changing Quran text, IDs, order, provenance, word fidelity, or rendering;
- changing automatic, independent, grouped, or manual-word link semantics;
- forcing grouped links as a performance shortcut;
- partially visible commits or splitting one logical confirmation into user-visible partial writes;
- reducing the allowed operation to an arbitrary smaller ayah limit;
- editing InitialBaseline or hand-editing any generated migration, Designer, or model snapshot;
- applying a migration to a database;
- deployment, commit, push, PR, or formal review; and
- unrelated UI redesign.

## 3. Locked architecture decisions

### 3.1 Compatibility and cutover

1. New source-page, prepared-preflight, workspace-delta, and confirmation-job routes are additive.
2. The current complete-source and expanded operation routes remain functional while the new
   Backend and Frontend paths are built.
3. Every Frontend caller moves to the new contracts before a legacy route or model is removed.
4. Phase 7 removes the old routes and expanded models only after targeted repository searches show
   no caller.
5. Existing workspaces, contributions, units, door state, and stored operation outcomes are not
   rewritten.
6. M2 and M3 are additive migrations. No authoritative linking table is dropped or rebuilt.

### 3.2 Execution mode

- Source detail pages are synchronous and bounded by page size.
- Every prepared preflight is durable and asynchronous.
- Every mutating confirmation is a durable asynchronous job.
- An exact retry may return an already-created or terminal resource immediately.
- A ready preflight that is a no-op is shown as a no-op. If its confirmation endpoint is called
  anyway, the Backend records a durable no-op operation/job outcome.
- Polling is the only status transport in this plan.
- There is no sync/async complexity threshold because calibration and measurement are excluded.

### 3.3 Initial structural policy

These are implementation limits, not performance acceptance targets.

| Policy | Initial value |
|---|---:|
| Source and prepared-detail page size | 100 ayahs |
| Server page-size maximum | 100 ayahs |
| Frontend page requests in flight globally | 2 |
| Adjacent prefetch | next page only |
| Frontend source-page cache budget | 20,000 weight units |
| Frontend source-page cache TTL | 10 minutes |
| Frontend prepared-detail cache budget | 8,000 weight units |
| Frontend prepared-detail cache TTL | 5 minutes |
| Backend compact-source cache budget | 60,000 reference units |
| Backend ayah-text cache budget | 60,000 reference units |
| Backend cache expiry | 30-minute sliding, 4-hour absolute |
| Preflight processors active globally | 2 |
| Confirmation workers active globally | 2 |
| Confirmation workers active for one door | 1 |
| Active linking workflows per actor | 4 |
| Persistence/cleanup batch size | 500 rows |
| Worker lease | 2 minutes |
| Worker heartbeat | 30 seconds |
| Maximum automatic attempts | 3 |
| Initial status poll delay | 1,500 ms |
| Accepted server poll-delay range | 1,000–5,000 ms |
| Ready preflight lifetime | 30 minutes |
| Abandoned queued/preparing lifetime | 2 hours |
| Terminal resource retention | 24 hours |
| Cleanup interval | 5 minutes |
| Frontend recovery records per actor | 16 |
| Frontend recovery serialized-weight budget per actor | 8 MiB |
| Recovery reconciliation requests in flight | 1 |

All Backend values live in validated LinkingScalability options in appsettings.json. Frontend
page/cache/scheduler values live in one linking-specific policy file. They must not be duplicated as
component magic numbers.

### 3.4 Authorization and failure contract

- Every new route remains RequireOwner.
- Actor identity is always taken from the authorization state, never from a request body.
- Reading another actor's preflight/job returns 404 so resource existence is not disclosed.
- Malformed bodies or invalid page numbers/page sizes return 400.
- A missing source, door, preflight, or job returns 404.
- A stale revision, stale source/view version, stale door/contribution version, token mismatch,
  idempotency mismatch, or invalid detail/mutation lifecycle transition returns 409.
- Detail, confirm, or cancel against an expired prepared resource returns 410.
- GET status returns 200 for every retained lifecycle row, including stale, failed, cancelled,
  expired, confirmed, and terminal jobs. Missing/unauthorized rows return 404; a row whose retention
  cleanup has started is treated as already removed and also returns 404.
- Lifecycle conflict responses carry a linking-specific data object with a stable code; the
  Frontend must not branch on localized message text.
- Internal exceptions and lease-owner data are never returned to the Frontend.

Stable lifecycle codes are:

    LINKING_DATA_STALE
    SOURCE_VIEW_STALE
    WORKSPACE_SOURCE_STALE
    PREFLIGHT_NOT_READY
    PREFLIGHT_BLOCKED
    PREFLIGHT_STALE
    PREFLIGHT_EXPIRED
    PREFLIGHT_CANCELLED
    PREPARATION_FAILED
    PREFLIGHT_ALREADY_CONFIRMED
    PREPARATION_ABANDONED
    CONFIRMATION_CANCELLED
    CONFIRMATION_FAILED
    ACTIVE_LINKING_WORKFLOW_LIMIT
    IDEMPOTENCY_CONFLICT
    CANCELLATION_TOO_LATE

failureCode is null for queued/preparing/ready/confirmed preflights and
queued/running/finalizing/succeeded jobs. Terminal mapping is exhaustive:

| Resource transition | status | failureCode |
|---|---|---|
| Preflight user cancellation before job acceptance | cancelled | PREFLIGHT_CANCELLED |
| Preflight revision invalidated | stale | LINKING_DATA_STALE |
| Preflight door/contribution/token input invalidated | stale | PREFLIGHT_STALE |
| Preparation controlled/permanent error or retry exhaustion | failed | PREPARATION_FAILED |
| Queued/preparing resource exceeds abandoned lifetime | failed | PREPARATION_ABANDONED |
| Ready preflight lifetime elapses | expired | PREFLIGHT_EXPIRED |
| Accepted job cancellation; written to job and pinned preflight | cancelled | CONFIRMATION_CANCELLED |
| Job revision invalidated; written to job and pinned preflight | stale | LINKING_DATA_STALE |
| Job door/contribution/token invalidated; written to job and pinned preflight | stale | PREFLIGHT_STALE |
| Job controlled/permanent error or retry exhaustion; written to job and pinned preflight | failed | CONFIRMATION_FAILED |
| Defensive blocked-preflight detection before mutation | failed | PREFLIGHT_BLOCKED |

Internal exception text is logged only; PREPARATION_FAILED and CONFIRMATION_FAILED never expose it.

## 4. Trusted linking-data revision

There is no authoritative persisted linking/Quran revision in the current repository.
ResolvedAtUtc is a load timestamp, not a revision. Foundation and morphology inputs validate
checksums, but linking resolution also depends on derived display-word and morphology tables, and
the validated digests are not carried through as one complete linking provenance value.

The implementation therefore adds a singleton generation named LinkingDataRevision. It is an
invalidation/version generation, not a claimed provenance hash.

### Required rules

1. M2 seeds singleton row 1 with generation 1 for an existing database.
2. Every governed writer begins its transaction by locking singleton row 1 with `FOR UPDATE`, before
   its first read/write/TRUNCATE of Quran, display-word, morphology, or linking-resolution data, and
   holds that lock through commit. After a real change it executes
   `UPDATE linking_data_state SET generation = generation + 1, updated_at_utc = ... WHERE id = 1
   RETURNING generation` near the end of the same transaction. Reading a value and later writing
   `value + 1` is forbidden because two writers could lose an increment.
3. The generation is incremented only after the writer has successfully made a real change and
   rolls back with that writer on failure.
4. These three supported writers must increment it:
   - EfBulkQuranImportWriter;
   - SqlDisplayWordsRebuilder; and
   - EfBulkMorphologyWriter.
5. Every source response, prepared preflight, prepared-detail page, Backend cache key, Frontend
   cache key, and both prepared and compatibility preflight tokens carry the generation.
   During the compatibility window, the legacy full-state workspace PUT and both legacy expanded
   preflight/confirmation bodies carry expectedLinkingDataRevision captured from their displayed
   source/preflight result. The legacy Frontend rejects a mixed-generation source set, and the
   Backend compares that expected generation under the shared revision lock before validating or
   writing ID-bearing configuration, before preparing expanded IDs, and again before confirmation
   finalization.
6. Old-generation source pages, detail pages, preflights, and confirmations fail with
   LINKING_DATA_STALE; they are never silently refreshed and committed.
7. Cache invalidation does not scan every key. The new generation makes old keys unreachable, and
   their bounded cache entries leave through LRU/TTL.
8. Every revision-bound Backend read begins by taking `FOR SHARE` on singleton row 1 before reading
   the generation or any governed data, and holds it until its transaction ends. This covers source
   pages, source/cache fills and cache hits, delta validation, preflight enqueue/preparation/detail
   hydration, legacy expanded preflight/confirmation preparation, and both new and compatibility
   confirmation finalization.
9. The universal database lock order is linking_data_state first, then Quran/linking/workspace/door
   rows. A non-mutating read transaction must not be declared database `READ ONLY` when that would
   reject `FOR SHARE`. This ordering prevents a reader/writer deadlock and prevents PostgreSQL's
   non-MVCC-safe TRUNCATE behavior from exposing old generation with new/empty Quran data.
10. A revision-bound repeatable-read executor treats PostgreSQL SQLSTATE 40001 while acquiring the
    first shared revision lock as a whole-operation retry, never a partial continuation. HTTP reads
    use at most the configured MaximumAutomaticAttempts; prepared work returns to its existing
    durable attempt lifecycle. Exhaustion returns a controlled transient failure.

## 5. Exact HTTP contracts

The examples below define fields and lifecycle behavior. Existing ApiResponse envelopes remain in
place.

During Phases 3–6 the retained legacy bodies gain one required compatibility field without changing
their expanded source arrays:

    POST /api/linking/operations/preflight
    { "expectedLinkingDataRevision": 17, "...": "existing legacy preflight body" }

    POST /api/linking/operations
    { "expectedLinkingDataRevision": 17, "...": "existing legacy confirmation body" }

    PUT /api/linking/workspace/sources/{id}/configuration
    { "expectedLinkingDataRevision": 17, "...": "existing full configuration body" }

The legacy preflight response also returns linkingDataRevision. Confirmation sends that exact value
with its opaque token; neither endpoint accepts expanded ayah/word IDs without the generation that
produced them.

### 5.1 Paged source resolution

    POST /api/linking/sources/resolve-page

Request:

    {
      "descriptor": { "...": "existing LinkingSourceDescriptorBody shape" },
      "expectedLinkingDataRevision": 17,
      "expectedSourceViewIdentity": "...",
      "view": {
        "segment": "included",
        "inclusionMode": "all_except",
        "ayahOverrideIds": [12, 15]
      },
      "page": 1,
      "pageSize": 100
    }

Rules:

- The first request for a logical view uses null for both expected fields and page 1.
- Later requests repeat the same descriptor/view, send the returned linking revision and
  sourceViewIdentity as expectations, and may request any positive page number. A recomputed view
  identity mismatch returns 409 rather than mixing logical views.
- `view.segment` is `all`, `included`, or `excluded`. `all` omits inclusionMode/ayahOverrideIds;
  included/excluded send the compact current inclusion overlay. Label, descriptions, selected words,
  automatic-match display, and link shape are not view-membership inputs and do not invalidate it.
- The server derives resolutionIdentity only from canonical descriptor fields that affect Quran
  resolution; presentation label and contribution/link mode are excluded. It canonicalizes the
  inclusion overlay and derives sourceViewIdentity from resolution identity + segment + normalized
  inclusion. It never trusts a client-supplied identity. Page size is deliberately not view
  identity; it is a separate required cache/request key component.
- Each request uses one non-mutating repeatable-read transaction. It first locks
  linking_data_state `FOR SHARE`, then captures revision, compact membership/matches, and page
  hydration from that same database snapshot, retaining the shared lock until completion.
- Ordering is authoritative Quran order with a stable ayah-ID tie-break.
- Revision stability makes numbered offset pages deterministic; one operation must never mix pages
  from different generations.
- Resolution may produce/cache one compact ordered ID/match index for total count and page
  traversal. For included/excluded, the server filters that compact index before applying the
  numbered offset. Quran ayah/word DTO hydration and response construction occur only for the
  requested logical-view page. Building the complete expanded response and then slicing it, or
  returning complete membership to let the client find a distant logical row, is forbidden.
- MaxResolvedAyahs stops being a response-materialization limit after the legacy route is removed.
  Page size is the transport bound; operation validity remains bounded by canonical Quran
  membership and the existing MaxPreparedSources value.

Response data:

    {
      "resolutionIdentity": "...",
      "sourceViewIdentity": "...",
      "linkingDataRevision": 17,
      "totalAyahCount": 1819,
      "page": 1,
      "pageSize": 100,
      "totalPages": 19,
      "items": [
        {
          "ayahId": 1,
          "verseKey": "1:1",
          "surahNumber": 1,
          "ayahNumber": 1,
          "surahNameArabic": "...",
          "pageFrom": 1,
          "pageTo": 1,
          "matchedQuranWordIds": [1],
          "words": [
            {
              "quranWordId": 1,
              "wordNumber": 1,
              "textUthmani": "...",
              "isAyahMarker": false
            }
          ]
        }
      ]
    }

### 5.2 Delta workspace configuration

    PATCH /api/linking/workspace/sources/{sourceId}/configuration

Request:

    {
      "sourceVersion": 24,
      "expectedLinkingDataRevision": 17,
      "changes": [
        { "kind": "set-label", "label": "..." },
        { "kind": "set-ayah-included", "ayahId": 1, "included": true },
        {
          "kind": "replace-inclusion",
          "mode": "all_except",
          "ayahOverrideIds": []
        },
        {
          "kind": "set-word-selected",
          "ayahId": 1,
          "quranWordId": 1,
          "selected": true
        },
        {
          "kind": "set-automatic-word-matches",
          "enabled": true
        },
        {
          "kind": "set-manual-link-shape",
          "shape": "grouped"
        },
        {
          "kind": "replace-ayah-descriptions",
          "ayahId": 1,
          "descriptions": ["..."]
        }
      ]
    }

Rules:

- Changes in one request are applied atomically to one source.
- expectedLinkingDataRevision is required for every PATCH. Under the revision-first shared lock the
  server compares it before validating or writing any ayah/word ID-bearing state; sourceVersion alone
  cannot detect a Quran foundation/morphology rebuild.
- The server validates the effective final configuration, not each intermediate change in
  isolation.
- Repeated changes for one target are normalized to the last value before the request is sent.
- replace-inclusion handles select-all/clear-all without emitting one change per ayah.
- The old PUT route remains until Phase 7. From Phase 3 it also requires
  expectedLinkingDataRevision from the displayed compatibility source, takes the revision shared
  lock before validating/reading/writing ayah or Quran-word IDs, and returns LINKING_DATA_STALE
  without saving when it differs.

Response data contains only the acknowledgement needed to advance the local draft; it never
returns the complete updated source:

    {
      "workspaceVersion": 8,
      "sourceId": 42,
      "sourceVersion": 25,
      "linkingDataRevision": 17,
      "normalizedAppliedChanges": [
        { "kind": "set-label", "label": "..." }
      ]
    }

### 5.3 Create a prepared preflight

    POST /api/linking/preflights

Request:

    {
      "preparationKey": "uuid",
      "doorId": 12,
      "expectedLinkingDataRevision": 17,
      "sources": [
        {
          "orderValue": 1,
          "workspaceSource": {
            "sourceId": 42,
            "sourceVersion": 24
          }
        },
        {
          "orderValue": 2,
          "inlineSource": {
            "descriptor": { "...": "existing descriptor shape" },
            "configuration": {
              "inclusionMode": "only",
              "ayahOverrideIds": [1, 2],
              "selectedWords": [
                { "ayahId": 1, "quranWordId": 1 }
              ],
              "automaticWordMatchesEnabled": null,
              "manualLinkShape": "independent",
              "descriptions": [
                { "ayahId": 1, "orderValue": 1, "body": "..." }
              ]
            }
          }
        }
      ]
    }

Rules:

- Exactly one of workspaceSource or inlineSource is present for each source.
- sources.Count is required to be from 1 through the existing MaxPreparedSources value.
- At most four nonterminal linking workflows may exist for one actor. A workflow is one prepared
  preflight before job creation or its queued/running/finalizing confirmation job, never both; a
  fifth create returns ACTIVE_LINKING_WORKFLOW_LIMIT. Preflight create first takes an actor-scoped
  advisory transaction lock, then its preparationKey lock; exact retry lookup precedes the count.
  In Phase 3, before the job table exists, the count is only unaccepted queued/preparing/ready
  preflights. Phase 4 extends that same quota store/query to count unaccepted
  queued/preparing/ready preflights plus queued/running/finalizing jobs, and confirmation enqueue
  takes the same actor lock while atomically converting one preflight slot into its job slot. This
  bounds concurrent durable work across tabs/instances, not ayahs or sources inside a valid
  operation.
- Source order is unique and contiguous from 1.
- expectedLinkingDataRevision is optional only when no source page was displayed; enqueue then
  captures the current generation. When supplied, it must equal the current generation. The stored
  prepared request always has one required captured generation.
- A workspace source is loaded through actor ownership and must match sourceVersion.
- Inline label has one source of truth: descriptor.label. Inline configuration has no label field;
  canonical hashing, prepared summaries, and the eventual contribution all use descriptor.label.
- The enqueue transaction copies the complete compact descriptor/configuration snapshot into
  prepared-source rows. Later workspace edits cannot mutate that prepared request.
- Enqueue uses one READ COMMITTED transaction. Its first statements take the actor-scoped and then
  preparationKey advisory transaction locks; only after any wait completes does it query exact
  retry and active count, so it sees the winner's committed row instead of a stale repeatable-read
  snapshot. For a new request it takes linking_data_state `FOR SHARE`, locks referenced workspace
  source headers in ascending source ID, verifies their versions, then reads every configuration
  child and writes the prepared snapshot before releasing those locks. Every workspace mutation
  must lock its source header before changing children, so those reads form one coherent source
  snapshot under READ COMMITTED. Request order is stored independently from lock order.
- The inline form supports the existing direct-source workflow without forcing it into the
  workspace.
- preparationKey is unique per actor. An exact retry returns the same resource; reuse with a
  different canonical request hash returns IDEMPOTENCY_CONFLICT.
- Canonical hashing uses validated domain values, source order, sorted/distinct ID sets, preserved
  description order, and the existing length-prefixed token style; it never hashes raw JSON member
  order. A hash match is followed by canonical request-document equality.
- The Backend derives authoritative source membership, units, contribution mode, matches,
  existing contributions, Quran data, and classification. The client never sends trusted Quran
  text or expanded units.
- A newly queued resource returns 202 with Location and pollAfterMs. An exact retry of an already
  ready/terminal resource may return 200 with the same resource.

### 5.4 Prepared-preflight resource and details

    GET /api/linking/preflights/{preflightId}
    DELETE /api/linking/preflights/{preflightId}
    GET /api/linking/preflights/{preflightId}/sources/{preparedSourceId}/ayahs
    GET /api/linking/preflights/{preflightId}/merged-ayahs

Status values:

    queued
    preparing
    ready
    stale
    failed
    cancelled
    expired
    confirmed

Preparation stages:

    resolving
    classifying
    persisting

Status response data contains:

    {
      "preflightId": "uuid",
      "status": "preparing",
      "stage": "resolving",
      "processedSources": 1,
      "totalSources": 2,
      "processedAyahs": 100,
      "totalAyahs": null,
      "pollAfterMs": 1500,
      "linkingDataRevision": 17,
      "createdAtUtc": "...",
      "expiresAtUtc": null,
      "isNoOp": null,
      "isBlocked": null,
      "preflightToken": null,
      "totals": null,
      "sources": [],
      "failureCode": null
    }

Only real completed counts are returned. No timer-generated percentage is allowed. When ready,
the response includes the current LinkingPreflightCounts shape, token, the 30-minute expiresAtUtc,
and one compact summary per source, but not every ayah detail. Each source summary contains:

    {
      "preparedSourceId": 101,
      "orderValue": 1,
      "resolutionIdentity": "...",
      "label": "...",
      "sourceKind": "...",
      "contributionMode": "...",
      "automaticWordMatchesEnabled": true,
      "classification": "...",
      "counts": { "...": "existing per-source count shape" },
      "existingContributionId": 55,
      "expectedContributionVersion": "...",
      "totalAyahCount": 1821
    }

Detail query parameters are page, pageSize, and filter. Supported filter values preserve the
current classification tokens:

    ALL
    NEW_AYAH
    OVERLAP_OTHER_SOURCE
    UNCHANGED
    UPDATE
    REMOVE
    INVALID

Each detail page returns canonical Quran ayah/word entities plus ID-based preflight overlays.
Per-source paging orders by prepared source order then Quran order. A merged query first pages the
distinct `(quranOrder, ayahId)` set, then loads every source overlay for the selected ayahs; an ayah
and its overlays can never be split across pages. `filter=ALL` counts distinct ayahs across all
sources. Any other filter includes an ayah when at least one source overlay matches it, still
returns all overlays for that ayah, and reports the distinct filtered ayah count. Because prepared
rows are immutable, a virtual range maps directly to a numbered page; the page response includes
page, pageSize, totalItems, and totalPages.

The generated-client detail response shape is explicit:

    {
      "preflightId": "uuid",
      "linkingDataRevision": 17,
      "detailKind": "merged",
      "preparedSourceId": null,
      "filter": "ALL",
      "page": 1,
      "pageSize": 100,
      "totalItems": 1821,
      "totalPages": 19,
      "items": [
        {
          "ayah": {
            "ayahId": 1,
            "verseKey": "1:1",
            "surahNumber": 1,
            "ayahNumber": 1,
            "surahNameArabic": "...",
            "pageFrom": 1,
            "pageTo": 1,
            "words": [
              {
                "quranWordId": 1,
                "wordNumber": 1,
                "textUthmani": "...",
                "isAyahMarker": false
              }
            ]
          },
          "sourceOverlays": [
            {
              "preparedSourceId": 101,
              "sourceOrder": 1,
              "preparedUnitId": 501,
              "isRequested": true,
              "unitOrder": 1,
              "ayahOrder": 1,
              "isGrouped": false,
              "classification": "NEW_AYAH",
              "invalidReason": null,
              "matchedQuranWordIds": [1],
              "requestedQuranWordIds": [1],
              "descriptions": ["..."],
              "overlappingSources": [],
              "wordChanges": { "added": [1], "removed": [], "unchanged": [] },
              "doorWordImpact": { "added": [1], "existing": [], "removed": [] },
              "descriptionChanges": {
                "added": ["..."],
                "removed": [],
                "changed": [],
                "unchanged": []
              }
            }
          ]
        }
      ]
    }

For a per-source route, detailKind is `source`, preparedSourceId is required, and every item has
exactly that source overlay. For merged detail it is null and sourceOverlays contains all overlays
for the paged ayah. The impact objects reuse the existing typed preflight shapes; the API never
exposes raw classification_impact JSONB.

An old-only ayah removed by an update is persisted as a REMOVE overlay with
`isRequested: false`, nullable preparedUnitId, its previous deterministic unit/ayah order, and empty
requestedQuranWordIds. Ayahs still present in the desired intent use one requested overlay whose
typed impact describes any old words/descriptions removed; they are not duplicated as a second
REMOVE row.

Status GET returns the status DTO with 200 for every retained status. Detail GET returns 200 only
for ready/confirmed resources at the current revision, 409 PREFLIGHT_NOT_READY for queued/preparing,
409 with the retained failure code for stale/failed/cancelled, and 410 PREFLIGHT_EXPIRED for expired.

Every detail read uses one non-mutating repeatable-read transaction. It first locks
linking_data_state `FOR SHARE`, then reads the current LinkingDataRevision and prepared page in the
same snapshot, rejects a revision mismatch before Quran hydration, and never combines prepared
snapshot IDs with current Quran rows from another generation.

DELETE behavior:

- queued becomes cancelled immediately;
- preparing records cancellationRequested and the processor stops between bounded stages/batches;
- in Phase 3, before M3 exists, ready becomes cancelled directly. From Phase 4 onward, ready becomes
  cancelled only when no confirmation job references it;
- from Phase 4 onward, a preflight referenced by any retained confirmation job returns 409;
- confirmed cannot be cancelled and returns PREFLIGHT_ALREADY_CONFIRMED;
- DELETE on already cancelled is idempotent and returns its status; stale/failed return 409 and
  expired returns 410 PREFLIGHT_EXPIRED.

### 5.5 Confirmation jobs

    POST /api/linking/preflights/{preflightId}/confirmation-jobs
    GET /api/linking/confirmation-jobs/{jobId}
    DELETE /api/linking/confirmation-jobs/{jobId}
    GET /api/linking/confirmation-outcomes/{idempotencyKey}

Create request:

    {
      "preflightToken": "...",
      "idempotencyKey": "uuid"
    }

The client never resends sources, units, ayahs, words, descriptions, door version, or contribution
versions.

Job statuses:

    queued
    running
    finalizing
    succeeded
    stale
    failed
    cancelled

Job stages:

    loading-prepared
    applying-unit-diff
    synchronizing-door
    committing

Job response data contains:

    {
      "jobId": "uuid",
      "preflightId": "uuid",
      "status": "running",
      "stage": "applying-unit-diff",
      "processedItems": 500,
      "totalItems": 1821,
      "pollAfterMs": 1500,
      "cancellationRequested": false,
      "createdAtUtc": "...",
      "startedAtUtc": "...",
      "completedAtUtc": null,
      "result": null,
      "failureCode": null
    }

Rules:

- A preflight may own only one confirmation job.
- idempotencyKey is exclusive across retained jobs and durable LinkingOperation outcomes. A
  succeeded or no-op key remains reserved permanently by LinkingOperation; a non-successful key is
  reserved by its terminal job for the 24-hour retention window.
- The prepared-job request hash is lowercase SHA-256 over a length-prefixed canonical tuple of
  contract kind, schema version, route preflight ID, supplied token, and the preflight's linking-data
  revision; idempotencyKey is the lookup key and is not hashed into its own payload. The legacy hash
  uses its own contract kind/schema plus door, expectedLinkingDataRevision, token, and the complete
  normalized ordered expanded body. Actor and door are also compared as columns on every replay.
- Enqueue takes linking_data_state `FOR SHARE` before locking the preflight row, verifies
  owner/token/revision/ready/nonexpired state and atomically requires `isBlocked = false`, then
  inserts the job and marks the preflight as confirmation-accepted in one transaction. A blocked
  ready preflight returns 409 PREFLIGHT_BLOCKED and can never own a job. Acceptance suspends the
  original preflight expiry; a valid queued job cannot later fail merely because its queue wait
  passed the former ready expiry.
- Exact POST retry returns the same retained job. After successful/no-op job cleanup it returns the
  discriminated durable-outcome envelope defined below. The same key with another
  request/preflight/token returns IDEMPOTENCY_CONFLICT.
- Exact retry checks durable LinkingOperation first. If succeeded/no-op job/preflight rows were
  already cleaned, a matching stored request hash still returns the durable operation outcome.
- The owner-only confirmation-outcome lookup returns that same durable succeeded/no-op result by
  idempotency key only when the owned operation has `prepared_job` kind and immutable job/preflight
  reference IDs. It returns 404 for a missing/other-actor key and 409 IDEMPOTENCY_CONFLICT for a
  historical or legacy-kind key that cannot form this envelope.
- New/nonterminal work returns 202. An exact terminal retry may return 200.
- queued cancellation is immediate.
- running cancellation sets cancellationRequested; the worker stops before finalizing.
- Transition to finalizing checks cancellation and owns the door slot atomically.
- finalizing and succeeded cannot be cancelled; DELETE returns CANCELLATION_TOO_LATE.
- DELETE on cancelled is idempotent and returns its status; stale/failed return a lifecycle 409.
- Success marks the preflight confirmed and writes both preflight/job completedAtUtc timestamps in
  the same transaction as the link mutation, operation outcome, and succeeded job. Every non-success
  terminal transition—queued cancellation, worker
  cancellation, stale, or exhausted failure—locks job then pinned preflight and writes both matching
  terminal statuses, failure code where applicable, and completedAtUtc values in one transaction.
  The preflight is never left accepted/ready after its job is terminal.
- Closing the Frontend workflow only stops polling. It never cancels an accepted durable job.

The confirmation POST response is discriminated by resourceKind. A retained job uses
`resourceKind: "job"` and the job response above. Recovery after its cleanup uses:

    {
      "resourceKind": "durable_outcome",
      "jobId": "uuid",
      "preflightId": "uuid",
      "idempotencyKey": "uuid",
      "status": "succeeded",
      "completedAtUtc": "...",
      "result": { "...": "existing LinkingConfirmationResult shape" }
    }

This is a durable outcome DTO, not a fabricated live Job DTO; polling ends immediately.

## 6. Persistence design

### 6.1 M2 — M2DurablePreparedLinkingPreflight

Generate this migration with Backend/scripts/add-mig M2DurablePreparedLinkingPreflight after the
model is complete.

#### linking_data_state

- id smallint primary key with a check requiring id = 1;
- generation bigint, required and greater than zero;
- updated_at_utc;
- one seeded row for an existing database.

#### linking_prepared_preflights

- id UUID primary key;
- actor_user_id and door_id foreign keys, both explicitly Restrict/NoAction;
- preparation_key UUID;
- status and stage constrained to the locked lifecycle tokens;
- request_schema_version and canonical request_document JSONB;
- request_hash and nullable intent_hash;
- linking_data_revision;
- expected_door_version, nullable until classification is ready;
- preflight_token, nullable until ready;
- is_no_op and is_blocked, nullable until ready;
- requested/new/overlapping/unchanged/updated/removed/invalid count columns;
- processed_sources, total_sources, processed_ayahs, nullable total_ayahs;
- cancellation_requested_at_utc;
- confirmation_accepted_at_utc, nullable and used to pin an accepted preflight;
- lease_owner, lease_expires_at_utc, attempt_count;
- cleanup_owner, cleanup_lease_expires_at_utc, cleanup_attempt_count, and
  cleanup_started_at_utc for durable multi-transaction cleanup fencing;
- created, started, ready, expires, completed, confirmed, and updated UTC timestamps;
- failure_code;
- xmin concurrency token.

Constraints and indexes:

- unique actor_user_id + preparation_key;
- alternate key id + actor_user_id + door_id for the confirmation-job ownership FK;
- request and intent hashes are fixed lowercase SHA-256 hex when present;
- status/lease/created index for queue claims;
- actor/id index;
- partial ready-expiry index `(expires_at_utc, id)` for unaccepted ready rows not already under
  cleanup;
- partial terminal-cleanup index `(completed_at_utc, id)` for the locked terminal-status set where
  cleanup has not started; and
- partial cleanup-reclaim index `(cleanup_lease_expires_at_utc, id)` where cleanup has started.

#### linking_prepared_sources

- bigint primary key and preflight foreign key with Restrict delete behavior;
- order_value;
- resolution_identity and fixed-size resolution_identity_hash, derived only from the descriptor's
  resolution-affecting fields and used for source resolution/page/cache identity; label and
  contribution/link mode are excluded;
- contribution_identity and fixed-size contribution_identity_hash, derived with the existing
  LinkingContributionIdentity semantics including contribution/link mode;
- label, source_kind, and contribution_mode; label is copied from the effective descriptor and is
  not duplicated inside configuration_document;
- descriptor_schema_version and descriptor_document JSONB;
- configuration_schema_version and configuration_document JSONB;
- nullable workspace_source_id and captured source_version, both snapshot values with no workspace
  source FK;
- automatic_word_matches_enabled;
- nullable existing_contribution_id and expected_contribution_version, both snapshot values with no
  authoritative contribution FK;
- source classification and count columns;
- total_ayah_count.

Constraints and indexes:

- unique preflight_id + order_value;
- alternate key id + preflight_id for composite child FKs;
- unique preflight_id + contribution_identity_hash; neither unbounded identity text column is part
  of a B-tree key;
- nonunique preflight_id + resolution_identity_hash lookup index;
- every contribution-hash candidate is compared with the full contribution identity: equal means a
  duplicate prepared source and unequal means a controlled hash-collision failure. A hash alone is
  never accepted as identity equality.

#### linking_prepared_units

- bigint primary key plus preflight_id and source_id;
- composite `(source_id, preflight_id)` foreign key to the source alternate key, with Restrict
  delete behavior;
- order_value;
- unit_identity and unit_identity_hash;
- is_grouped;
- unique source_id + order_value;
- alternate key id + source_id + preflight_id for the ayah composite FK;
- index source_id + unit_identity_hash;
- every hash candidate is verified with the full identity before reuse.

Prepared-unit rows represent only desired/requested units used by confirmation; old-only removal
detail does not create a desired unit.

#### linking_prepared_ayahs

- bigint primary key;
- preflight_id and source_id with a composite `(source_id, preflight_id)` foreign key to the source
  alternate key and Restrict delete behavior;
- nullable unit_id with a composite `(unit_id, source_id, preflight_id)` foreign key to the unit
  alternate key and Restrict delete behavior when present;
- is_requested plus a check requiring unit_id for requested rows and requiring null unit_id for
  old-only REMOVE rows;
- source_order, unit_order, ayah_order, and authoritative quran_order;
- is_grouped snapshot so a removed overlay retains its previous grouping semantics without a
  synthetic desired unit;
- ayah_id stored as an immutable snapshot value, with no FK to quran_ayahs;
- classification and nullable invalid_reason;
- schema-versioned classification_impact JSONB containing overlaps, word changes, door-word impact,
  and description changes;
- unique source_id + ayah_id;
- source-detail ALL index `(source_id, quran_order, ayah_id)`;
- source-detail filter index `(source_id, classification, quran_order, ayah_id)`;
- merged-detail ALL index `(preflight_id, quran_order, ayah_id)`; and
- merged-detail filter index `(preflight_id, classification, quran_order, ayah_id)`;
- overlay-hydration index `(preflight_id, ayah_id, source_order)` for the second merged query.

The classifier writes one row per source/ayah union. Desired ayahs are is_requested; old-only ayahs
are non-requested REMOVE rows ordered deterministically from their previous contribution. Final
confirmation reads only requested units/ayahs/words as the desired state, while detail queries read
both kinds.

#### linking_prepared_ayah_words

- prepared_ayah_id + quran_word_id composite primary key;
- prepared_ayah_id foreign key with Restrict delete behavior;
- quran_word_id stored as an immutable snapshot value, with no FK to quran_words;
- is_source_match and is_requested flags, with a check requiring at least one to be true, so detail
  pages can reproduce automatic source-match highlights even when those matches are disabled as a
  contribution;
- order_value;
- unique prepared_ayah_id + order_value;
- detail DTOs project matchedQuranWordIds from is_source_match and requestedQuranWordIds from
  is_requested; confirmation reads only is_requested rows as the final word contribution.

#### linking_prepared_ayah_descriptions

- bigint primary key;
- prepared_ayah_id foreign key with Restrict delete behavior;
- order_value and body;
- unique prepared_ayah_id + order_value.

#### linking_prepared_affected_contributions

- preflight_id + contribution_id composite primary key, with only preflight_id constrained by a
  Restrict FK; contribution_id is a versioned snapshot value and must not block a concurrent
  authoritative contribution deletion;
- expected_contribution_version;
- contains every existing contribution whose version participates in the token/final check.

Prepared descriptor/configuration snapshots are immutable after enqueue. Unit/ayah/word/description
rows may be replaced only by a fenced retry while status is queued/preparing. Once ready, all
prepared intent rows and hashes are immutable; only lifecycle/lease/expiry fields may change.

Prepared ayah/word IDs deliberately do not reference the canonical Quran tables. The supported
foundation writer rebuilds those tables with `TRUNCATE ... CASCADE`; FKs from ephemeral prepared
rows would silently erase part of a prepared snapshot while leaving its header. Revision checks
make the old snapshot stale, and current Quran hydration occurs only after that check succeeds.
Expected door/source/contribution versions copied from PostgreSQL xmin are ordinary numeric snapshot
columns. They are not configured as `IsRowVersion`; only the prepared-preflight and job headers use
their own xmin as EF concurrency tokens.

### 6.2 M3 — M3DurableLinkingConfirmationJobs

Generate this migration with Backend/scripts/add-mig M3DurableLinkingConfirmationJobs after the
model is complete.

#### linking_confirmation_jobs

- id UUID primary key;
- preflight_id, actor_user_id, and door_id, with a composite
  `(preflight_id, actor_user_id, door_id)` FK to the prepared-preflight alternate key and Restrict
  delete behavior; actor and door direct FKs are also explicitly Restrict/NoAction;
- idempotency_key UUID;
- request_hash, fixed lowercase SHA-256 hex;
- status and stage constrained to the locked tokens;
- processed_items and total_items;
- cancellation_requested_at_utc;
- attempt_count, lease_owner, and lease_expires_at_utc;
- cleanup_owner, cleanup_lease_expires_at_utc, cleanup_attempt_count, and
  cleanup_started_at_utc;
- queued, started, completed, and updated UTC timestamps;
- operation_id nullable foreign key with Restrict/NoAction delete behavior;
- schema-versioned outcome_document JSONB;
- failure_code;
- xmin concurrency token.

Constraints and indexes:

- unique idempotency_key within retained jobs; cross-table exclusivity with durable operations is
  enforced by the shared advisory lock and both-table lookup;
- unique preflight_id so one prepared intent cannot create two jobs;
- unique operation_id when present;
- status/lease/queued index for claims;
- door/status index;
- partial unique door_id index for running/finalizing jobs, enforcing one active job per door;
- partial terminal-cleanup index `(completed_at_utc, id)` for the locked terminal-status set where
  cleanup has not started; and
- partial cleanup-reclaim index `(cleanup_lease_expires_at_utc, id)` where cleanup has started.

#### additions to linking_operations

- prepared_preflight_id, nullable for historical rows and SET NULL on prepared cleanup;
- prepared_preflight_reference_id and confirmation_job_reference_id, immutable UUID snapshot values
  with no FKs, required for a prepared-job outcome and null for a legacy outcome; these survive
  cleanup and reconstruct the durable-outcome envelope;
- request_contract_kind and request_schema_version, nullable for historical rows and required for
  every operation created after M3 (`prepared_job` or `legacy_expanded`);
- request_hash, fixed lowercase SHA-256 hex when present, nullable for historical rows and required
  for every operation created after M3, including legacy and no-op outcomes;
- linking_data_revision, nullable for historical rows.

M3 adds checks requiring the contract metadata columns to be either all null for a historical row
or all present for a post-M3 row. `prepared_job` requires both immutable reference IDs;
`legacy_expanded` requires both immutable reference IDs and the relational prepared_preflight_id to
be null. Application insertion of `prepared_job` also requires the relational prepared_preflight_id;
that value may later become null only through the declared cleanup SET NULL behavior.

Existing rows are not given a fabricated kind, hash, or revision. An old row whose idempotency key
collides with a new request produces IDEMPOTENCY_CONFLICT because payload equality cannot be proven.
Every replay created after M3 compares actor, door, contract kind, schema version, and canonical
request hash; cross-kind reuse is always IDEMPOTENCY_CONFLICT rather than pretending compact and
expanded bodies are equal. Both paths store LinkingOperation for mutating and no-op outcomes.

### 6.3 Expiry and cleanup

- An unaccepted ready preflight expires 30 minutes after ready and transitions to expired; its
  header remains as a 410 tombstone for the terminal retention period.
- Job enqueue locks the preflight and sets confirmation_accepted_at_utc in the same transaction as
  the job insert. That pins the prepared snapshot until its job is terminal, regardless of the
  original expires_at_utc.
- Queued/preparing rows with no live lease after 2 hours transition to failed with the stable
  PREPARATION_ABANDONED failure code; `abandoned` is not a separate lifecycle status.
- Failed, stale, cancelled, expired, confirmed, and job terminal metadata remain queryable for 24
  hours.
- A preflight is never expired or deleted while any retained confirmation job references it. The
  job-to-preflight FK is Restrict, so cleanup must remove an eligible terminal job first.
- Cleanup claims one eligible resource under `FOR UPDATE SKIP LOCKED` by setting cleanupOwner,
  cleanupLeaseExpiresAtUtc, cleanupAttemptCount, and cleanupStartedAtUtc. Each later cleanup
  heartbeat/delete transaction must match that fence and an unexpired cleanup lease; another
  instance may reclaim only an expired cleanup lease. Once cleanupStartedAtUtc is set, public reads
  treat the resource as already removed.
- The claimed cleaner deletes actual child rows in transactions of at most 500 rows, not 500 large
  parents followed by unbounded cascades.
  The prepared deletion order is words/descriptions, ayahs, units, affected-contribution rows,
  sources, then the header. Every prepared-child FK therefore uses Restrict rather than cascade.
- Once both terminal-retention clocks have elapsed, cleanup deletes the job, drains prepared
  children in those batches, and finally deletes the preflight tombstone; deleting that header sets
  only the relational operation FK to null while immutable reference IDs remain. A partially
  drained terminal resource is never readable as ready and is safe for the next cleanup pass to
  resume.
- The durable LinkingOperation outcome is never deleted by this cleanup.

## 7. Worker and transaction rules

### 7.1 Database-leased claims

Both processors use PostgreSQL-backed leases; an in-memory semaphore is insufficient.

Claim transaction:

1. acquire a short processor-specific PostgreSQL advisory transaction lock;
2. count unexpired active leases and stop when the configured global limit is reached;
3. for confirmation, exclude doors with a running/finalizing job;
4. reclaim an expired lease on the same running/finalizing resource before another job for its
   door, using a nonblocking per-resource advisory-lock attempt and skipping it if the old
   transaction still owns that lock;
5. select the oldest eligible row with FOR UPDATE SKIP LOCKED;
6. set lease owner, lease expiry, attempt, status, and start timestamp;
7. commit and release the advisory lock.

The advisory lock is held only for claim coordination. It is never held for resolution,
classification, or confirmation work. Claim returns `(leaseOwner, attemptCount)` and attemptCount
is the fencing token for that execution. Every leased-worker heartbeat, progress update, lifecycle
transition, and terminal write uses a conditional statement matching resource ID, leaseOwner,
attemptCount, expected status, and an unexpired lease according to database time; it must affect
exactly one row or the worker stops without publishing any result. API cancellation of an unclaimed
queued resource instead locks the row and matches its expected queued status; it never fabricates a
lease. Expiry/abandonment maintenance uses a short row lock plus expected status/lease conditions
without setting cleanupStartedAtUtc; later retention cleanup uses its independent cleanup fence from
Section 6.3. An expired processing lease can never be revived by its old owner. `xmin` is not a
lease-fencing token.

The sole expiry-check exception is the succeeded update inside a final authoritative transaction
that already acquired the per-job advisory lock and job row while the lease was unexpired. Those
locks exclude reclaim through commit, so the succeeded update still matches leaseOwner/attemptCount
but deliberately does not recheck wall-clock expiry later in that same transaction.

Heartbeats extend the lease every 30 seconds through a separate short conditional update. A worker
that cannot renew its fenced lease cancels its remaining work. Reclaim first takes a transaction
advisory lock derived from the resource ID before incrementing attemptCount. Confirmation write
transactions take the matching transaction advisory lock. Prepared child processing instead
acquires the matching session advisory lock on a dedicated connection before opening its
repeatable-read child transaction and releases it immediately after that transaction commits or
rolls back; connection loss also releases it automatically. Its short header-finalization
transaction acquires the matching transaction advisory lock before checking the current fence.
Transaction and session forms conflict on the same key. This prevents an expired attempt and its
replacement from writing concurrently without establishing a stale repeatable-read snapshot while
waiting for the lock, while still allowing reclaim after the child transaction finishes. Lease
expiry makes work retryable; attempt 3 converts an unresolved transient failure into failed with
PREPARATION_FAILED or CONFIRMATION_FAILED for the owning processor.

### 7.2 Prepared-preflight processor

1. Claim one queued preflight and retain its leaseOwner/attemptCount fence.
2. On one dedicated connection, acquire the per-preflight session advisory lock. Before opening the
   repeatable-read processing transaction, run a current READ COMMITTED header fence check requiring
   status preparing, matching leaseOwner/attemptCount, and an unexpired lease. Then open the
   repeatable-read transaction; its first data statement takes linking_data_state `FOR SHARE` and
   retains it through the child-snapshot commit. Only then read/drain governed tables. A retry drains
   child rows left by the previous attempt in actual batches of at most 500 before rebuilding them.
3. Require the generation captured at enqueue; queued work never resolves itself against a newer
   unseen generation.
4. Resolve prepared sources in source order through a streaming/batched Backend reader.
5. Apply inclusion, manual-word, description, and link-shape configuration server-side.
6. Load the required confirmed door/contribution state once and build indexed lookups.
7. Classify with dictionaries/sets, never repeated linear scans.
8. Write units, ayahs, words, descriptions, affected contribution versions, counts, request/intent
   inputs in batches of 500 with bounded EF tracking. The processing transaction does not update
   the leased header row, so heartbeat/progress updates cannot create a repeatable-read
   serialization conflict.
9. Check cancellation between sources/batches through a separate short read.
10. After each completed source/batch boundary, publish real processed counts through a separate
    short update guarded by the same leaseOwner/attemptCount fence; a zero-row update aborts this
    attempt.
11. Immediately before child commit, run a separate short database-time fence probe and require the
    same owner/attempt with an unexpired lease. If it fails, roll back. If the lease crosses expiry
    only after that probe, the session lock still prevents replacement; the staging commit may land
    but cannot become ready and the next attempt rebuilds it.
12. Commit the child snapshot while the header remains preparing; detail endpoints still reject it.
    Release the session advisory lock immediately after that child transaction commits or rolls
    back.
13. Stop the heartbeat and open a short finalization transaction. First take the matching
    per-preflight transaction advisory lock; then take linking_data_state `FOR SHARE` and the
    prepared header `FOR UPDATE`, and require status preparing, the captured generation, matching
    leaseOwner/attemptCount, and an unexpired lease. If reclaim won the gap after child commit, this
    attempt observes the new fence and exits without publishing.
14. Verify the persisted affected-contribution version rows. Transition the header to ready and
    write expected door version, counts, intent hash, and token with one conditional update matching
    status, leaseOwner, attemptCount, an unexpired lease, and
    `cancellation_requested_at_utc IS NULL`. It must affect exactly one row. If cancellation is now
    requested, transition to cancelled with the same fence; if generation changed, transition to
    stale. The worker never publishes ready after a racing DELETE.

The session advisory lock is released in `finally` around the child transaction only. If
finalization finds an expired or replaced fence, it publishes nothing, releases its transaction
lock, and lets the current/reclaimed attempt rebuild.

A crash between child commit and header finalization leaves preparing rows that the expired-lease
retry drains and rebuilds. An older attempt that loses the fence can never finalize those rows as
ready.

The token binds:

- token schema version;
- preflight ID;
- actor and door IDs;
- canonical request hash;
- normalized intent hash;
- linking-data revision;
- expected door version; and
- the ordered affected contribution ID/version pairs.

### 7.3 Confirmation worker

Work before the final door lock:

1. Claim the job and retain its leaseOwner/attemptCount fence.
2. Verify owner, the job-pinned prepared resource, `isBlocked = false`, job request hash, token, and
   linking-data revision. Once the job was accepted, do not reapply the preflight's original ready
   expiry.
3. Read immutable requested prepared units/ayahs/words in ordered batches of at most 500 with no
   tracking; REMOVE-only detail rows never become desired link state. Do not build the complete
   confirmation graph in process memory. Derive old/new affected ayah IDs in a relational
   temporary/CTE workset from all prepared classification rows and matching old contributions.
4. Publish only completed batch counts through fenced short updates and honor cancellation before
   transitioning to finalizing.

Enter-finalizing transaction:

1. Take the per-job transaction advisory lock.
2. Transition the claimed job from running to finalizing with one conditional update matching
   leaseOwner, attemptCount, expected status, an unexpired lease, and no cancellation request. Zero
   affected rows means stop and roll back.
3. Commit this short transition before authoritative writes. The partial unique door index now owns
   the door's finalizing slot, polling can observe finalizing, and later DELETE returns
   CANCELLATION_TOO_LATE.

Authoritative final transaction:

1. Take the per-job transaction advisory lock, then read linking_data_state with `FOR SHARE`, require
   the prepared generation, and retain that shared lock until commit so a Quran/display/morphology
   revision writer cannot interleave.
2. Lock the job row only when status, leaseOwner, attemptCount, and an unexpired lease still match
   finalizing. This entry-time row/advisory lock is the fence for the whole transaction, even if
   wall-clock lease time passes while that transaction holds the row. No matching row means stop
   without a write.
3. Lock/recheck the pinned preflight and require confirmation accepted, immutable token, and
   `isBlocked = false`; any violation rolls back with PREFLIGHT_BLOCKED/stale handling.
4. Acquire the door row lock.
5. Recheck door xmin, every affected contribution xmin, token, and idempotency request hash.
6. Use the prepared relational rows as staging for set-based INSERT/UPDATE/DELETE statements; each
   command/bind set is bounded and no complete graph is materialized in application memory.
7. Match unit hashes in bulk and compare full unit identity before reuse.
8. Insert missing units and their ayah/word/description rows in bounded set operations.
9. Diff contribution-unit links: preserve unchanged, insert missing, update order, and remove stale.
10. Delete only units made orphaned by this operation.
11. Rebuild derived door ayahs/words only for the relational union of old and new affected ayah IDs.
12. Persist the LinkingOperation outcome, including no-op.
13. Mark the prepared preflight confirmed with confirmed_at_utc and completed_at_utc.
14. Mark the confirmation job succeeded with the same operation/result using the same lease fence;
    set completed_at_utc and require exactly one row to change. Match leaseOwner/attemptCount on the
    already locked row, without a second expiry predicate as defined by the entry-lock exception in
    Section 7.1.
15. Commit once.

The link state, idempotent operation outcome, and succeeded job state share one transaction. A
crash after commit therefore recovers a succeeded job; it never repeats the link mutation. A stale
revision/version rolls back all link changes, then a separate short transaction takes the per-job
advisory lock, locks the job before its pinned preflight, and atomically marks both stale with their
completed timestamps. Queued cancellation does the same with an expected queued status; worker
cancellation and exhausted failure additionally match the current leaseOwner/attemptCount fence.
Each transition must update both rows or roll back, and cleanup cannot begin between them. If
ownership was already lost, the old worker makes no state change. A crash after the short finalizing
commit is recovered by reclaiming the expired finalizing lease and re-entering the authoritative
transaction; the durable operation check resolves commit ambiguity. It never silently refreshes an
old intent.

During the compatibility window, the legacy confirmation route takes the same advisory lock derived
from idempotencyKey and checks both confirmation jobs and LinkingOperation before it mutates. Every
legacy outcome, including no-op, is persisted as LinkingOperation before that lock is released. A
key already owned by a job is an exact recovery only when its canonical request matches; otherwise
it is IDEMPOTENCY_CONFLICT. This leaves no post-lock window in which a legacy no-op key can be reused.

## 8. Execution rules

1. Implement one named phase at a time and stop at its boundary.
2. Each phase manifest is its file allowlist. Report a newly discovered dependency before editing
   outside it.
3. Do not start a later phase to repair an earlier phase opportunistically.
4. Generated migration/OpenAPI files may change only through their repository generators.
5. Do not apply M2 or M3 to any database without separate explicit authority.
6. Do not create performance instrumentation or test code to compensate for the excluded
   measurement work.
7. Do not commit, push, open a PR, deploy, or start formal review without separate authorization.
8. After Phase 7 and the non-browser automated gates, stop and hand the result to the user.

## 9. Phase overview

| Phase | Outcome | Schema/API impact |
|---|---|---|
| 1. Bounded Backend core | Current classifier/writer loses per-unit I/O and quadratic scans | None |
| 2. Data revision and prepared schema | Trusted invalidation generation and durable preflight storage exist | Generated M2 |
| 3. Paged/prepared API and revision bridge | Source pages, workspace deltas, durable preflight processor/details, and legacy revision fencing exist | Additive routes |
| 4. Durable confirmation Backend | Leased jobs and short atomic final merge exist | Generated M3 and additive routes |
| 5. Frontend canonical engine | Pages, overlays, caches, polling, and coalescing are bounded | Uses new routes |
| 6. Virtual UI and cutover | Every live linking flow uses paged prepared resources/jobs | Frontend cutover |
| 7. Legacy removal | Expanded routes/models/state are removed after all callers move | Breaking cleanup |

---

## Phase 1 — Bounded Backend classifier and confirmation core

### Goal

Remove the current per-unit query/save pattern, growing tracked graph, full contribution
replacement, and repeated linear scans while the legacy API remains active.

### Implementation

1. Create one immutable operation workset before persistence:
   - canonical unit identity and hash once per requested unit;
   - exact identity retained for collision validation;
   - units deduplicated across sources;
   - source/unit order retained separately;
   - old/new affected ayah ID sets retained.
2. Replace classifier and preflight projection scans with dictionaries/sets keyed by ayah,
   contribution, unit identity, and word identity.
3. Load existing unit candidates in batches by door + identity hash, then compare full identities.
4. Insert missing units and children with AddRange/bounded SaveChanges stages; no query or save may
   sit inside a requested-unit loop.
5. Keep tracked entity count bounded by stage/batch and detach completed batches when their
   generated keys are captured.
6. Diff contribution-unit links instead of remove-and-recreate.
7. Delete only newly orphaned units.
8. Synchronize door ayahs/words only for affected ayah IDs using indexed lookups.
9. Preserve current transaction, token/version validation, idempotency, ordering, collision
   handling, and atomic visibility.

### Phase manifest

- Backend/application/QuranDashboard.Application/Linking/LinkingOperationClassifier.cs
- Backend/application/QuranDashboard.Application/Linking/LinkingOperationPreparation.cs
- Backend/application/QuranDashboard.Application/Linking/LinkingPreflightProjection.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.Persistence.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.State.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.DoorState.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.Outcome.cs
- new workset/batch partial files in that same Writes/Linking folder

### Completion boundary

- no query or SaveChanges remains inside a requested-unit loop;
- classifier/projection lookups are indexed rather than nested scans;
- contribution updates are true diffs;
- derived state work is affected-only;
- collision verification still compares exact identity after hash match; and
- no controller, HTTP contract, entity, configuration, or migration changes.

---

## Phase 2 — Linking-data revision and durable prepared-preflight schema

### Goal

Create the authoritative revision and durable, immutable prepared-preflight storage required by
paging, caching, and later jobs.

### Implementation

1. Add the M2 entities/configurations and every constraint/index from Section 6.1.
2. Add LinkingDataRevision reader, revision-locked read-scope, and writer-store abstractions plus
   their Infrastructure implementations.
3. Update the three Quran/linking resolution writers from Section 4 so revision changes in their
   own successful transaction: lock linking_data_state `FOR UPDATE` before touching governed data,
   use the atomic increment-and-returning statement after a real change, and retain the lock through
   commit. No writer may read then increment in application memory.
4. Register revision services.
5. Add all DbSets/configurations to QuranDashboardDbContext.
6. Generate M2DurablePreparedLinkingPreflight only through Backend/scripts/add-mig.
7. Verify the generated model has Restrict/composite prepared relationships and no prepared
   ayah/word FK to the rebuildable Quran tables.
8. Keep prepared tables unused by public routes until Phase 3.

### Phase manifest

- new Backend/domain/QuranDashboard.Domain/Linking/LinkingDataState.cs
- new Backend/domain/QuranDashboard.Domain/Linking/LinkingPreparedPreflight.cs
- new Backend/domain/QuranDashboard.Domain/Linking/LinkingPreparedSource.cs
- new Backend/domain/QuranDashboard.Domain/Linking/LinkingPreparedUnit.cs
- new Backend/domain/QuranDashboard.Domain/Linking/LinkingPreparedAyah.cs
- new Backend/domain/QuranDashboard.Domain/Linking/LinkingPreparedAyahWord.cs
- new Backend/domain/QuranDashboard.Domain/Linking/LinkingPreparedAyahDescription.cs
- new Backend/domain/QuranDashboard.Domain/Linking/LinkingPreparedAffectedContribution.cs
- new lifecycle token/enum files beside those entities
- new Backend/application/QuranDashboard.Application.Abstractions/Linking/ILinkingDataRevisionReader.cs
- new Backend/application/QuranDashboard.Application.Abstractions/Linking/ILinkingDataRevisionReadScope.cs
- new revision store files under Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Linking/
- new entity configurations under Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Linking/
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/LinkingDependencyInjection.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Foundation/EfBulkQuranImportWriter.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/DisplayRebuilding/SqlDisplayWordsRebuilder.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/EfBulkMorphologyWriter.cs
- generated M2DurablePreparedLinkingPreflight migration pair
- generated Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/QuranDashboardDbContextModelSnapshot.cs

### Completion boundary

- one seeded revision exists in the model;
- all three supported writers update it atomically with no lost-increment path;
- all governed writers acquire the revision exclusive lock before any governed table, and the
  revision reader/read-scope primitives needed by Phase 3 exist with the required lock order;
- no public route/token claims revision-bound behavior in Phase 2; the complete compatibility and
  new-reader wiring is an explicit Phase 3 boundary;
- every M2 table, FK, delete behavior, constraint, index, and concurrency token matches Section 6.1;
- a foundation truncate cannot cascade-delete part of a prepared snapshot;
- existing linking rows are untouched;
- InitialBaseline is untouched; and
- M2 and its Designer/snapshot are generator output, not handwritten files.

---

## Phase 3 — Paged source, workspace delta, prepared preflight, and revision bridge

### Goal

Add the compact/paged Backend path and durable asynchronous preparation while keeping legacy
routes compatible through the minimum revision-aware Frontend bridge.

### Implementation

1. Split the current full source reader into:
   - a page reader for HTTP details;
   - a streaming/batched preparation reader for server work; and
   - shared descriptor validation/identity logic.
2. Implement resolve-page with the revision-first shared lock in a non-mutating repeatable-read
   transaction and revision-bound numbered pages over authoritative Quran order. Implement
   independent all/included/excluded logical views and their view-specific total/page calculation
   so any virtual range can request its page directly without scanning preceding pages or returning
   complete membership.
3. Change Backend source-cache keys to include LinkingDataRevision.
4. Weight compact source entries by ayah IDs + Quran word IDs + matched word IDs.
5. Weight hydrated ayah entries by one ayah + its hydrated word count.
6. Rename cache option units from Ayahs to References, reject entries above the whole budget, and
   keep load coalescing.
7. Add the delta workspace command/handler/writer and response from Section 5.2; preserve the old
   PUT response through a compatibility handler, but add its required expectedLinkingDataRevision
   guard and the matching compatibility Frontend propagation in this phase.
8. Validate workspace deltas against the revisioned compact membership/match index and targeted
   word ownership data under the revision-first shared lock, reject an expected-generation mismatch
   before any write, then lock the workspace source header; the delta writer must not
   resolve/hydrate a complete source DTO.
9. Revision-scope the compatibility source/preflight/confirmation path as one deployable change:
   add linkingDataRevision to its source and preflight DTOs and expectedLinkingDataRevision to both
   legacy expanded preflight and confirmation bodies. The legacy Frontend retains the revision
   beside each resolved source, rejects/reloads a mixed-generation source set, sends the single
   generation with preflight, then forwards the returned generation with confirmation. The Backend
   compares it under the revision shared lock before either expanded preparation, binds it into a
   versioned legacy preflight token, and returns LINKING_DATA_STALE on mismatch. The confirmation
   writer rechecks it after taking the revision shared lock and before the door lock, then recomputes
   the token with that same generation. On LINKING_DATA_STALE the compatibility Frontend evicts the
   old resolved-source set, stops any legacy workspace save, and requires a fresh
   resolution/review; it never replays old IDs. The legacy preflight-preview hydration also requires
   the preflight generation before displaying Quran details. Resident compatibility source-cache
   entries are keyed by returned revision + source identity; any descriptor-only key exists only
   while the first request is in flight. The token stays opaque to the Frontend; M3 job/idempotency
   behavior remains Phase 4 work.
10. Add compact prepared-preflight body mapping and canonical request hashing.
11. In the enqueue transaction, enforce `1..MaxPreparedSources`, validate ownership/versions and
    full contribution identities, take the actor workflow-cap lock before the preparationKey lock,
    check exact retry before atomically counting the Phase 3 quota of unaccepted nonterminal
    preflights, lock workspace source headers in ascending ID under the READ COMMITTED locking
    protocol from Section 5.3, and persist the compact source snapshots before returning 202. Do not
    query the not-yet-created confirmation-job table in this phase.
12. Add the database-leased prepared processor, cancellation, status, source-detail, merged-detail,
    and cleanup handlers.
13. After resolving requested sources, load confirmed state in this order:
    - existing contributions matching the requested contribution identities and all of their old unit
      ayahs;
    - affected ayah IDs as old contribution ayahs union newly requested ayahs; and
    - other door contributions/door ayahs/words/descriptions intersecting that affected set.
      The new prepared path must not load the complete confirmed door graph.
14. Use the Phase 1 indexed classifier and persist page-ready results from prepared rows.
15. Implement source and merged detail queries exactly as Section 5.4: page distinct ayahs first,
    fetch all overlays second, count distinct ayahs, and use separate ALL/filter indexes.
16. Apply leaseOwner/attemptCount fencing to every preparation heartbeat, progress transition, and
    finalization. Apply the independent durable cleanupOwner/cleanupAttemptCount lease to every
    multi-transaction cleanup pass; retry cleanup and expiry cleanup drain actual child rows in
    batches of 500.
17. Add stable lifecycle error DTOs/messages.
18. Add the routes to the retained smoke route catalog as owner-only parity entries.
19. Export Swagger and regenerate the Angular API client through check-api-contract.

### Phase manifest

API:

- Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingSourcesController.cs
- Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingOperationsController.cs
- Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingWorkspaceController.cs
- new Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingPreflightsController.cs
- new source-page, workspace-delta, prepared-preflight, page, and lifecycle contract files under
  Backend/api/QuranDashboard.Api/Contracts/Linking/
- Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingOperationBodies.cs
- Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingOperationBodyMapper.cs
- Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingWorkspaceBodies.cs
- Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingWorkspaceConfigurationBodyMapper.cs
- Backend/api/QuranDashboard.Api/Common/ApiMessages.cs

Application and abstractions:

- Backend/application/QuranDashboard.Application.Abstractions/Linking/ILinkingSourceResolutionReader.cs
- Backend/application/QuranDashboard.Application.Abstractions/Linking/ILinkingConfirmedStateReader.cs
- Backend/application/QuranDashboard.Application.Abstractions/Linking/ILinkingWorkspaceWriter.cs
- new source-page/preparation-reader, prepared-store, claim, status, page, and lifecycle ports under
  Backend/application/QuranDashboard.Application.Abstractions/Linking/
- new workspace-delta input/acknowledgement contracts and command/handler files under
  Backend/application/QuranDashboard.Application.Abstractions/Linking/ and
  Backend/application/QuranDashboard.Application/Linking/Commands/
- Backend/application/QuranDashboard.Application/Linking/Commands/ReplaceLinkingWorkspaceSourceConfiguration/ReplaceLinkingWorkspaceSourceConfigurationCommand.cs
- Backend/application/QuranDashboard.Application/Linking/Commands/ReplaceLinkingWorkspaceSourceConfiguration/ReplaceLinkingWorkspaceSourceConfigurationHandler.cs
- Backend/application/QuranDashboard.Application/Linking/LinkingWorkspaceExecution.cs
- Backend/application/QuranDashboard.Application/Linking/LinkingWorkspaceOutcome.cs
- new source-page and prepared-resource DTOs under the same abstractions project
- new handlers under Backend/application/QuranDashboard.Application/Linking/Queries/ResolveLinkingSourcePage/
- Backend/application/QuranDashboard.Application/Linking/Queries/ResolveLinkingSource/ResolveLinkingSourceHandler.cs
- Backend/application/QuranDashboard.Application/Linking/Queries/PreflightLinkingOperation/PreflightLinkingOperationHandler.cs
- Backend/application/QuranDashboard.Application/Linking/Commands/ConfirmLinkingOperation/ConfirmLinkingOperationHandler.cs
- Backend/application/QuranDashboard.Application/Linking/Commands/ConfirmLinkingOperation/ConfirmLinkingOperationOutcome.cs
- Backend/application/QuranDashboard.Application.Abstractions/Linking/ILinkingConfirmationWriter.cs
- new linking-data-stale exception/outcome files under
  Backend/application/QuranDashboard.Application.Abstractions/Linking/
- new create/get/cancel/detail/process use cases under
  Backend/application/QuranDashboard.Application/Linking/PreparedPreflights/
- Backend/application/QuranDashboard.Application.Abstractions/Linking/Preflight/LinkingPreflightToken.cs
- Backend/application/QuranDashboard.Application.Abstractions/Linking/Preflight/LinkingOperationRequest.cs
- Backend/application/QuranDashboard.Application.Abstractions/Linking/Preflight/LinkingPreflightResultDto.cs
- Backend/application/QuranDashboard.Application.Abstractions/Linking/Preflight/LinkingOperationIntent.cs
- Backend/application/QuranDashboard.Application.Abstractions/Linking/Responses/LinkingResolvedSourceDto.cs
- Backend/application/QuranDashboard.Application/Linking/LinkingOperationPreparation.cs
- Backend/application/QuranDashboard.Application/Linking/LinkingOperationClassifier.cs
- Backend/application/QuranDashboard.Application/Linking/LinkingPreflightProjection.cs
- Backend/application/QuranDashboard.Application/Linking/LinkingOperationValidation.cs
- Backend/application/QuranDashboard.Application/DependencyInjection.cs

Infrastructure:

- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/EfLinkingSourceResolutionReader.cs
- all current EfLinkingSourceResolutionReader partial files
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/LinkingAyahHydration.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/EfLinkingConfirmedStateReader.cs
- new page/preparation readers and prepared persistence/claim files under Persistence/Reads/Linking
  and Persistence/Writes/Linking
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingWorkspaceWriter.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingWorkspaceWriter.Configuration.cs
- all EfLinkingConfirmationWriter partial files
- Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/CachedLinkingSourceResolutionReader.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/LinkingResolvedSourceCompact.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/LinkingSourceResolutionCache.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/LinkingAyahTextCache.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/LinkingSourceCacheKeys.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/LinkingSourceCacheEntryOptions.cs
- new prepared processor, cleanup service, and LinkingScalability options files
- Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/LinkingDependencyInjection.cs
- Backend/api/QuranDashboard.Api/appsettings.json

Generated/retained contract artifacts:

- Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs
- Frontend/quran-dashboard-ui/openapi/swagger.json
- generator-owned Frontend/quran-dashboard-ui/src/app/core/api/generated/

Compatibility Frontend revision bridge:

- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-preflight.api.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-command.port.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/http-linking-command.port.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-operation-request.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-workspace.repository.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/http-linking-workspace.repository.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-operation.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-preflight.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workspace.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-workflow.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source.cache.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-editor.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-set.coordinator.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-preflight-preview.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace-sync.runner.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace-merge.ts
- one new compatibility revision/error model beside those files

### Completion boundary

- source resolution retains at most a compact bounded ID/match index, and Quran DTO hydration
  occurs for only one revision-bound numbered page;
- no source page requires a durable source snapshot;
- prepared POST persists compact immutable snapshots and returns a durable ID;
- preparation survives API restart through DB leasing;
- an expired preparation worker cannot publish after a replacement attempt owns its fence;
- two cleanup instances cannot drain the same prepared resource concurrently;
- status and details never return the complete preflight graph;
- merged pages contain complete overlays for each distinct paged ayah;
- workspace configuration traffic is delta-in/delta-out;
- cache weights count retained references rather than ayahs only;
- all new routes are additive and owner-only;
- the compatibility preflight token and its confirmation consumer are revision-bound without
  changing its opaque Frontend use;
- the compatibility expanded preflight cannot combine sources from different revisions or submit
  ayah/word IDs without the revision that produced them;
- the compatibility full-state workspace PUT cannot persist ayah/word IDs without the revision of
  the displayed source and never auto-replays them after LINKING_DATA_STALE;
- every public source/preflight/confirmation read that now carries a revision follows the
  revision-first shared-lock order; and
- no confirmation caller has moved yet.

---

## Phase 4 — Durable confirmation jobs and short atomic finalization

### Goal

Move every new-path confirmation to a recoverable database job and make the final door lock cover
only revalidation plus set-based merge.

### Implementation

1. Add the M3 job entity/configuration and LinkingOperation fields from Section 6.2.
2. Generate M3DurableLinkingConfirmationJobs only through Backend/scripts/add-mig.
3. Add enqueue/get/cancel handlers and exact retry/request-hash behavior. Extend the Phase 3 shared
   workflow-quota store so subsequent preflight creates count both unaccepted preflights and
   nonterminal jobs. Enqueue takes a short actor workflow-cap advisory lock followed by the
   idempotencyKey lock, checks both linking_confirmation_jobs and linking_operations, takes the
   revision shared lock before the preflight row lock, and atomically converts/pins one ready
   nonexpired preflight into its job slot before inserting the job in the same transaction, with no
   transient double count or released slot.
4. Extend the already revision-fenced legacy confirmation path to use that same idempotency lock,
   check both tables, and persist a LinkingOperation even for no-op before releasing the lock.
   Every post-M3 legacy operation stores its contract kind/schema and canonical expanded-request
   hash; replay compares
   actor + door + kind + schema + hash, and legacy finalization takes the revision shared lock before
   the door lock, requires the revision captured by compatibility preparation, and recomputes the
   revision-bound opaque token. Compute the request hash before legacy source preparation so a fast
   both-table lookup can return an exact durable replay; the final writer still acquires the lock and
   repeats the lookup after preparation to close the race. This preserves cross-table idempotency and
   revision ordering during compatibility.
5. Add database claim, heartbeat, lease recovery, attempt fencing, global concurrency, and
   same-door serialization.
6. Add the confirmation hosted worker and the state machine from Section 5.5.
7. Stream prepared rows in batches and implement relational/set-based merge plus affected-only door
   synchronization without a complete in-memory workset.
8. Revalidate token, door version, and affected contribution versions under the final door lock;
   hold a shared linking-data-state lock until commit so the revision cannot change mid-merge.
9. Persist no-op and mutating LinkingOperation outcomes.
10. Commit authoritative link state, operation outcome, confirmed preflight, succeeded job, and both
    resource completed timestamps together under the lease fence. For every non-success terminal
    outcome, update the fenced job and its pinned preflight to matching terminal states with both
    completed timestamps in one separate short transaction.
11. Add terminal retention and ordered bounded cleanup with Restrict job/preflight relationships and
    the durable cleanup lease/fence from Section 6.3. Extend the Phase 3 prepared status,
    cancellation, expiry, and cleanup paths so an accepted/job-referenced preflight is pinned, job
    terminal state is propagated, jobs are removed before prepared rows, and no Phase 3-only cleanup
    assumption survives M3.
12. Add owner-only job routes and route catalog entries.
13. Export Swagger and regenerate the Angular API client.
14. Keep the legacy synchronous POST route until Phase 7.

### Phase manifest

- new Backend/domain/QuranDashboard.Domain/Linking/LinkingConfirmationJob.cs
- new Backend/domain/QuranDashboard.Domain/Linking/LinkingConfirmationJobStatus.cs
- Backend/domain/QuranDashboard.Domain/Linking/LinkingOperation.cs
- new job configuration plus
  Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Linking/LinkingOperationConfiguration.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs
- generated M3DurableLinkingConfirmationJobs migration pair
- generated Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/QuranDashboardDbContextModelSnapshot.cs
- new job command/query/status DTO/port files under
  Backend/application/QuranDashboard.Application.Abstractions/Linking/ and
  Backend/application/QuranDashboard.Application/Linking/ConfirmationJobs/
- the Phase 3 prepared-preflight create/quota/status/cancel/expiry/cleanup use cases and their
  abstractions under Backend/application/QuranDashboard.Application/Linking/PreparedPreflights/ and
  Backend/application/QuranDashboard.Application.Abstractions/Linking/
- Backend/application/QuranDashboard.Application/Linking/Commands/ConfirmLinkingOperation/ConfirmLinkingOperationHandler.cs
- new shared canonical request-hash/idempotency lookup files beside the existing linking operation
  application abstractions and Infrastructure writer files
- new Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingConfirmationJobsController.cs
- new Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingConfirmationOutcomesController.cs
- Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingPreflightsController.cs
- new confirmation-job bodies/responses/mappers under Backend/api/QuranDashboard.Api/Contracts/Linking/
- Backend/api/QuranDashboard.Api/Common/ApiMessages.cs
- all EfLinkingConfirmationWriter partial files
- the Phase 3 prepared-preflight quota/store/status/cancel/expiry/cleanup persistence files under
  Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/ and
  Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/
- new job repository, claimer, worker, heartbeat, prepared-finalization, and job-cleanup
  coordinator/repository files under
  Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/
- Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/LinkingDependencyInjection.cs
- Backend/application/QuranDashboard.Application/DependencyInjection.cs
- Backend/api/QuranDashboard.Api/appsettings.json
- Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs
- Frontend/quran-dashboard-ui/openapi/swagger.json
- generator-owned Frontend/quran-dashboard-ui/src/app/core/api/generated/

### Completion boundary

- all new-path confirmations return/recover a durable job;
- exact retries cannot create duplicate work;
- two workers/instances cannot run the same job or the same door concurrently;
- a superseded lease attempt cannot heartbeat, finalize, or publish a terminal state;
- an accepted job remains valid after the preflight's former ready-expiry instant;
- cancellation is impossible after finalizing starts;
- the door lock excludes source resolution and preflight classification;
- final writes use prepared relational rows and bounded set operations;
- no successful link commit can coexist with a nonterminal/failed job record; and
- no terminal job can coexist with an accepted/ready pinned preflight or a missing terminal clock;
  and
- legacy routes still operate for the old Frontend.

---

## Phase 5 — Frontend canonical engine, caches, and request lifecycle

### Goal

Build the bounded Frontend data engine and new API adapters without yet removing the existing
presentation components.

### Implementation

1. Regenerate and consume the new API models; never hand-edit generated files.
2. Add immutable canonical entity stores keyed by linkingDataRevision + ayahId/wordId.
3. Store source membership, matches, selection, manual words, grouping, and classification as
   compact ID overlays.
4. Replace verse-key selection state with canonical ayah IDs; verseKey remains display data.
5. Maintain one active linking-data generation per operation. A page from a newer generation
   cancels/releases older page state; pages from two generations can never be combined. When any
   source page was displayed, the compact preflight sends this generation.
6. Add explicit cache and active-range leases from resident pages to canonical entities. Eviction
   releases the cache lease, range disposal releases the consumer lease, and entities disappear
   when neither remains; selection IDs stay independent.
7. Reject a conflicting payload for the same revision/entity key as a controlled data error instead
   of overwriting canonical Quran data.
8. Validate each fresh page exactly once before insertion. Cache hits do not remap/revalidate the
   complete page.
9. Add two independent weighted LRU/TTL caches:
   - source pages, budget 20,000 and TTL 10 minutes;
   - prepared detail pages, budget 8,000 and TTL 5 minutes.
10. Page weight equals ayah references + word references + match/classification/impact references +
   one unit per 64 text code units. An entry larger than its cache budget is served through a
   transient active-range lease but is not cached; releasing that range also releases its canonical
   entities.
11. Source-page keys contain revision, resolutionIdentity, sourceViewIdentity, pageSize, and page.
    Prepared-detail keys contain revision, preflightId, detailKind, nullable preparedSourceId,
    filter, pageSize, and page. Identical-request coalescing uses the same complete keys. A local
    inclusion-draft generation scopes provisional/in-flight source-view work until the server
    returns its canonical sourceViewIdentity; unrelated label/description/word-selection changes do
    not evict the view pages.
12. Coalesce identical in-flight page requests and cap all page HTTP work at two globally.
13. Translate each virtual rendered range directly to its numbered pages; a jump to a distant range
    must not fetch preceding pages.
14. Prefetch only the next adjacent page after the visible range enters the final quarter of the
    loaded page. Cancel queued/stale generations.
15. Keep prepared/transient details separate so they cannot evict normal source pages.
16. Expose keyed per-source/per-preflight signals rather than one Map signal invalidating every
    preview.
17. Implement workspace sync from acknowledged configuration to latest local draft:
    - 250 ms per-source debounce;
    - one in-flight patch per source and two globally;
    - create the next normalized delta only after the acknowledged source version arrives;
    - structural add/remove/reorder operations stay in a separate serialized queue;
    - on WORKSPACE_SOURCE_STALE, reload/rebase once only when linkingDataRevision is unchanged, then
      surface a controlled conflict;
    - on LINKING_DATA_STALE, stop synchronization, cancel/release old-generation pages and drafts,
      and require a fresh source load and user review; never replay ayah/word-ID deltas onto the new
      generation;
    - flush selected workspace sources before preflight and the current source before leaving its
      editor.
18. Make LinkingManualWordEditorFacade consume the page facade and ID overlays instead of the
    complete LinkingSourceResolver result.
19. In ManualMushafSelectionStore, own one metadata subscription per verseKey and cancel it on
    remove, clear, reset, replacement retry, and access loss.
20. Add prepared-preflight polling and confirmation-job polling with no overlapping requests.
21. Honor server pollAfterMs within 1–5 seconds, falling back to 1.5 seconds.
22. Stop polling on terminal status, workflow dismissal, actor change, or resource generation
    replacement.
23. Before `POST /preflights`, transactionally append/update a versioned preparation receipt in an
    actor-partitioned IndexedDB recovery journal: preparationKey, exact canonical compact request,
    and nullable preflightId. If that durable write fails, do not send the POST. The record contains
    only IDs/configuration, never Quran pages/entities. As soon as preflightId is durable, replace
    the request document with the ID/recovery metadata rather than retaining both.
24. Before `POST /confirmation-jobs`, transactionally write a separate confirmation receipt keyed by
    actor + idempotencyKey: preflight ID, token, canonical job request hash, and nullable job ID. If
    that write fails, do not send the POST. As soon as jobId is durable, replace token/request data
    with jobId + idempotencyKey; after this confirmation receipt commits, remove the superseded
    preparation receipt.
25. Actor hydration and every linking-workspace open use an IndexedDB actor+state index and one
    actor-scoped, lease-fenced recovery leader across tabs. Its recovery queue allows one HTTP
    request in flight: repeat the exact preflight POST when its ID is unknown, load a known
    preflight, then repeat/load the exact confirmation POST/job. If a known succeeded job has aged
    out, recover its durable outcome by idempotency key. It performs one status
    reconciliation for non-open workflows; only the workflow actively opened by the user enters
    continuous status polling. IndexedDB survives tab/window closure, so a lost response cannot
    orphan work or cause a differently keyed retry.
26. The journal permits at most 16 records and 8 MiB of serialized receipt weight per actor. If a
    new receipt would exceed either bound, block starting that operation and require recovery,
    acknowledgement, or safe cancellation; never evict nonterminal work. A preparation receipt is
    removed only after acknowledged terminal cancellation/failure/expiry or
    after a confirmation receipt has been durably written for it. A confirmation receipt is removed
    only after its terminal outcome is loaded and acknowledged. Dismissal, navigation, logout, actor
    change, or explicit workflow reset never deletes a nonterminal receipt; records stay partitioned
    by actor and are resumed only when that actor is authorized again. Best-effort preflight DELETE
    retains its receipt until the server confirms a terminal state. A terminal-unacknowledged ID-only
    record expires locally after the server's 24-hour terminal-retention window; succeeded/no-op
    outcomes remain recoverable through durable LinkingOperation.
27. Split the oversized workflow/workspace classes into the focused stores/facades below.
28. Keep core/caching/api-response-cache.ts unchanged; linking owns these policies.

### Phase manifest

New models:

- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-entities.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-page.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-operation-draft.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-prepared-preflight.models.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-execution.models.ts

New data access:

- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-pages.api.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-prepared-preflight.api.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-execution.api.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-job-status.api.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-workspace-configuration.repository.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/http-linking-workspace-configuration.repository.ts

New state:

- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-quran-entity.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-page-cache.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-page-request.scheduler.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-pages.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-operation-draft.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-prepared-preflight.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-preflight-details.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-execution.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-status-poll.runner.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace-configuration-sync.runner.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-recovery.store.ts
- one Frontend linking policy/constants file

Existing integration files:

- linking models under Frontend/quran-dashboard-ui/src/app/features/linking/models/
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-manual-ayah-metadata.reader.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-workspace.repository.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/http-linking-workspace.repository.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source.cache.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-set.coordinator.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-editor.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-manual-word-editor.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/manual-mushaf-selection.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-preflight-preview.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace-sync.runner.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace-merge.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts
- generated Frontend/quran-dashboard-ui/src/app/core/api/generated/

### Completion boundary

- selection/edit state contains IDs and metadata, not cloned Quran graphs;
- page caches have independent weight/TTL budgets and release canonical entities;
- cache keys cannot cross linking-data revisions;
- page/source work is coalesced, globally bounded, and cancelable;
- workspace edits cannot accumulate one full-state request per click;
- preflight/job polling and recovery are implemented with no overlap;
- lost preparation and confirmation POST responses recover from their exact compact receipts;
- fresh responses are validated once;
- manual metadata work is canceled with its generation; and
- the legacy presentation path keeps only the Phase 3 revision bridge and stays separate from the
  new page engine until Phase 6; no adapter may join numbered pages back into one complete-source
  array.

---

## Phase 6 — Shared virtual UI, real states, and Frontend cutover

### Goal

Move every direct/workspace linking path to page-backed canonical data, durable preflights, and
confirmation jobs while mounting only a bounded ayah window.

### Implementation

1. Create one LinkingVirtualAyahList host backed by the source/preflight page facades.
2. Ayah is the virtualization unit. Every resident ayah renders every authoritative word in order;
   words are never virtualized independently.
3. Represent group headers/boundaries as row metadata so grouped paths do not create giant nested
   DOM subtrees.
4. Keep only canonical IDs/overlays in row inputs and use stable ayah/word tracking IDs.
5. Upgrade the measured-row strategy to an estimated row height plus a Fenwick/prefix-delta tree:
   - total scroll extent is available before every page is resident;
   - a measured row updates offsets in O(log N);
   - no update rebuilds a full offsets array;
   - missing pages render the existing bounded row skeleton until loaded.
6. Route all large paths through the host:
   - direct source preview/configuration;
   - workspace source preview/editor;
   - grouped selection;
   - manual-word editing;
   - per-source preflight details; and
   - merged preflight details.
7. Preserve Quran font, RTL order, tashkeel, labels, highlights, keyboard/focus behavior, and
   accessible row position metadata as virtual rows recycle.
8. Change the workflow to:

       configure-source
       door
       preflighting
       ready
       submitting / queued / running / finalizing
       succeeded / failed / cancelled

   preparing and loading-details are paintable substates. The mount sequence is fixed:
   set preparing, wait for afterNextRender, wait one requestAnimationFrame, verify the operation
   generation is still current, then mount the virtual host and transition to ready.
9. Remove the client-side full resolve step. Opening a source loads only its first page.
10. Before preflight, flush selected workspace source deltas, create one compact prepared request,
    and poll until ready/terminal.
11. Fetch source/merged detail pages only when the panel/range requests them.
12. Confirm by preflight ID/token/idempotency key and poll the job resource.
13. Show real server stage/count text only; do not fabricate percentage progress.
14. Do not expose cancellation after finalizing starts.
15. Dismissal stops subscriptions and releases transient pages but does not cancel an accepted job;
    its durable confirmation receipt remains.
16. If no confirmation job exists, dismissal sends a best-effort preflight DELETE for queued,
    preparing, or ready work; expiry remains the fallback if that request cannot be delivered.
17. On stale, discard token/details and require a fresh prepared preflight before another explicit
    confirmation. Never auto-retry the mutation.
18. Clear transient overlays/pages on dismiss, terminal acknowledgement, and operation-generation
    replacement.
19. Keep styling changes limited to the shared host, skeleton/status layout, and existing UI
    language; this is not a visual redesign.

### Phase manifest

- Frontend/quran-dashboard-ui/src/app/features/linking/utils/measured-row-virtual-scroll.strategy.ts
- new Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-virtual-ayah-list/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-card/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-direct-source-preview/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-source-ayah-editor/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-selection/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-ayah-group/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-manual-word-editor/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-ayah-viewer/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-ayah-group/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-merged-ayah-viewer/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-preflight-step/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-door-step/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/direct-link-workflow/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-host/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace/
- Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-workspace-source-row/
- Frontend/quran-dashboard-ui/src/app/features/linking/models/linking.labels.ts
- the Phase 5 linking models, data-access adapters, stores, facades, scheduler, poller, and recovery
  files
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts

### Completion boundary

- every listed large viewer receives a logical range/page, not a full source array;
- all, included, excluded, grouped-selection, and manual-word logical views can jump directly to a
  distant page without complete membership, a complete graph, or a preceding-page scan;
- no grouped/manual/preflight branch bypasses the shared virtual host;
- a resident ayah still renders its complete authoritative word sequence;
- row measurement updates do not rebuild offsets for the full result;
- direct and workspace flows use compact preflight plus job confirmation only;
- no expanded operation graph is retained or uploaded by the live workflow;
- preparing/loading/job states paint before heavy/page work; and
- the old APIs remain only as unused compatibility routes.

---

## Phase 7 — Legacy contract and compatibility removal

### Goal

Remove the complete-source and expanded-operation paths after the new Frontend has no callers.
This is implementation cleanup, not a manual-verification phase.

### Implementation

1. Use targeted searches for old route strings, expanded DTO names, intent builders, and complete
   resolved-source retention.
2. Remove:
   - POST /api/linking/sources/resolve;
   - POST /api/linking/operations/preflight;
   - POST /api/linking/operations;
   - PUT /api/linking/workspace/sources/{id}/configuration.
3. Remove old expanded API bodies/mappers and synchronous handlers.
4. Remove full-resolution response models/read interfaces no longer needed by server preparation.
5. Remove the old Frontend source-resolution API, expanded preflight API, command port, operation
   request builder, expanded intent models/utilities, and compatibility adapters.
6. Remove old full-graph cache/state paths and obsolete component wrappers after all templates use
   LinkingVirtualAyahList.
7. Remove MaxResolvedAyahs only after no code uses a full materialized response; keep
   MaxPreparedSources and existing description/domain validation.
8. Update the route catalog for removed routes.
9. Export final Swagger and regenerate/prune the Angular generated client.
10. Confirm through static searches that none of these remain:

        /api/linking/sources/resolve
        /api/linking/operations/preflight
        POST /api/linking/operations
        LinkingPreflightBody
        LinkingConfirmationBody
        LinkingOperationUnitBody
        linking-operation-request
        LinkingSourceResolver
        LinkingSourceResolverRegistry
        ReplaceLinkingWorkspaceSourceConfiguration
        LinkingResolvedSourceDto as a complete-source HTTP response

### Phase manifest

Backend removal/update:

- Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingSourcesController.cs
- Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingOperationsController.cs
- Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingWorkspaceController.cs
- Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingOperationBodies.cs
- Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingOperationBodyMapper.cs
- Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingWorkspaceBodies.cs
- Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingWorkspaceConfigurationBodyMapper.cs
- Backend/api/QuranDashboard.Api/Common/ApiMessages.cs
- Backend/application/QuranDashboard.Application.Abstractions/Linking/LinkingLimits.cs
- Backend/application/QuranDashboard.Application.Abstractions/Linking/ILinkingWorkspaceWriter.cs
- legacy source/operation response and request files under
  Backend/application/QuranDashboard.Application.Abstractions/Linking/
- Backend/application/QuranDashboard.Application/Linking/Queries/ResolveLinkingSource/
- Backend/application/QuranDashboard.Application/Linking/Queries/PreflightLinkingOperation/
- Backend/application/QuranDashboard.Application/Linking/Commands/ConfirmLinkingOperation/
- Backend/application/QuranDashboard.Application/Linking/Commands/ReplaceLinkingWorkspaceSourceConfiguration/
- legacy-only parts of Backend/application/QuranDashboard.Application/Linking/LinkingOperationPreparation.cs
- legacy-only parts of
  Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingWorkspaceWriter.Configuration.cs
- legacy-only source resolution/cache compatibility files under Backend/infrastructure/QuranDashboard.Infrastructure/
- Backend/application/QuranDashboard.Application/DependencyInjection.cs
- Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/LinkingDependencyInjection.cs
- Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs

Frontend removal/update:

- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolution.api.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolver.registry.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-preflight.api.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-command.port.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/http-linking-command.port.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-operation-request.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-workspace.repository.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/data-access/http-linking-workspace.repository.ts
- legacy expanded models under Frontend/quran-dashboard-ui/src/app/features/linking/models/
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source.cache.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-set.coordinator.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-preflight-preview.facade.ts
- Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace-sync.runner.ts
- legacy-only merge/selection/intent utilities under
  Frontend/quran-dashboard-ui/src/app/features/linking/utils/
- obsolete component wrappers under Frontend/quran-dashboard-ui/src/app/features/linking/components/
- Frontend/quran-dashboard-ui/openapi/swagger.json
- generator-owned Frontend/quran-dashboard-ui/src/app/core/api/generated/

### Completion boundary

- no Frontend or Backend caller references the removed routes/models;
- no live workflow materializes a complete source or expanded confirmation request;
- only the page/preflight/job/durable-outcome contracts remain public;
- generated Swagger/client output contains no legacy operation body;
- authoritative linking tables and historical operations remain intact; and
- no new cleanup migration is introduced.

## 10. Finding-to-phase traceability

| Audit finding | Implemented by |
|---|---|
| B1 per-unit persistence | Phase 1, Phase 4 |
| B2 growing tracked graph | Phase 1, Phase 3, Phase 4 |
| B3 long door lock | Phase 3, Phase 4 |
| B4 full contribution rebuild | Phase 1, Phase 4 |
| B5 quadratic classifier | Phase 1, Phase 3 |
| B6 unsafe synchronous complexity | Phase 3, Phase 4 |
| B7 expanded operation sent/prepared twice | Phase 3, Phase 6, Phase 7 |
| B8 Backend cache is not memory-weighted | Phase 2, Phase 3 |
| F1 non-virtualized viewers | Phase 6 |
| F2 full operation rebuild after edits | Phase 5, Phase 6 |
| F3 count-only Frontend cache | Phase 5 |
| F4 eager all-source resolution | Phase 3, Phase 5, Phase 6 |
| F5 expanded graph uploaded twice | Phase 3, Phase 4, Phase 6, Phase 7 |
| F6 accumulating workspace full-state writes | Phase 3, Phase 5 |
| F7 no paintable preparing state | Phase 6 |
| F8 fallback data pollutes primary cache | Phase 5 |
| F9 one Map invalidates all previews | Phase 5 |
| F10 full offset recomputation | Phase 6 |
| F11 fresh response validated twice | Phase 5 |
| F12 uncancelled manual metadata request | Phase 5 |

## 11. Testing decision and non-browser gates

Testing Decision: **no new automated tests**. Under the repository Test Freeze and without separate
owner approval, this plan creates no test class, method, performance test, benchmark, or Playwright
journey. It does run the existing change-triggered gates listed below. SmokeRouteCatalog changes are
minimal updates to retained route metadata, not new test methods.

Checks run inside their owning phase rather than as a separate verification phase:

| Change boundary | Existing non-browser checks |
|---|---|
| Backend production changes | Backend build |
| Phase 2 Quran foundation/display/morphology writer changes | Backend/scripts/test-backend pipeline --no-build and Backend/scripts/test-backend canonical-data --no-build, after that phase's Backend build |
| Each generated migration | Backend/scripts/test-backend migration --no-build and Backend/scripts/check-pending-model --build, after that phase's Backend build |
| API additions/removals and SmokeRouteCatalog changes | Backend/scripts/check-api-contract and Backend/scripts/test-backend smoke --no-build, after that phase's Backend build |
| Frontend state/data changes | npm run check:no-unit-specs, then npm run typecheck:app, then npm run build:verify |
| Frontend component changes | npm run check:no-unit-specs, then npm run typecheck:app, then npm run build:verify, plus npm run check:golden-ui |
| Final Phase 7 state | Backend build, API contract check, Backend smoke lane, and Frontend static/type/build checks |

No gate in this plan records timing, memory, DOM count, request duration, or throughput. No browser
or manual verification is run by the implementing agent. Manual verification remains with the user
after Phase 7.

## 12. Explicit risks and required safeguards

1. The IDE tabs named 20260814122710_M2DurablePreparedLinkingPreflight do not exist in the current
   filesystem; only InitialBaseline exists. Phase 2 generates a new M2 and never assumes those tabs
   are real files.
2. A foundation manifest version/hash alone is not a complete linking revision because unique-word,
   morphology, and display-word truth have different writers.
3. A source-page endpoint that materializes all ayahs before slicing does not solve Backend memory
   or query cost and fails Phase 3.
4. A prepared resource is immutable intent, not an authoritative live link. It becomes visible only
   through the final confirmation transaction.
5. A stale prepared operation must fail and be reviewed again; it cannot refresh itself and commit
   unseen changes.
6. A cleanup worker must not expire or delete a preflight while any retained confirmation job
   references it; an accepted job pins the prepared snapshot until terminal handling completes.
7. A process-local semaphore cannot provide job uniqueness across restart or multiple API
   instances; DB leases and constraints are mandatory.
8. A hash is only an index/candidate key. Full source/unit identity comparison remains mandatory.
9. A successful linking commit and a failed/nonterminal job record cannot diverge; they must share
   the final transaction.
10. Closing the UI cannot imply cancellation after the server accepts a job.
11. Existing cache entries from an old revision may remain allocated until bounded eviction/TTL,
    but they must never be addressable by a new revision key.
12. Without separately approved invariant tests, lease/cancellation/commit-ambiguity behavior is
    protected by the explicit transaction design, implementation review when requested, and the
    existing non-browser gates only.
13. A lease timeout is not proof that the previous worker stopped. The attempt fence and per-resource
    advisory lock are both required; omitting either makes late commits possible.
14. Prepared Quran IDs are revision-bound snapshot values, not canonical-table FKs, because the
    supported foundation writer rebuilds canonical Quran tables through TRUNCATE CASCADE.
15. Selecting 500 parent resources and cascading their children is not bounded cleanup. The limit
    applies to actual deleted child rows in the explicit Restrict-FK deletion order.
16. The IndexedDB recovery journal contains only active or terminal-unacknowledged compact receipts,
    partitioned by actor and deleted by the lifecycle rules in Phase 5; it never stores
    source/detail pages or canonical Quran entities and is not used as a page cache.

## 13. Final handoff and document lifecycle

After Phase 7:

- stop implementation;
- report changed files, generated M2/M3 names, and the existing automated checks that were run;
- do not run a benchmark, profiler, Playwright, browser, manual smoke, deployment, or formal review;
- hand the result to the user for manual verification; and
- treat any issue the user finds as a separately authorized, narrowly scoped follow-up.

This file and its audit input are feature-scoped working documents under docs/feature-*. They are
removed only during the feature's final pre-merge documentation cleanup after implementation
review has used them, following docs/README.md.
