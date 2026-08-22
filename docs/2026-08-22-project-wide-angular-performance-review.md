# Angular Performance Review

- Review date: 2026-08-22
- Review mode: initial project-wide static Angular performance review with existing-artifact evidence
- Current-state identity: `dev@1bcf56b1586153a65ee0cf74f0e944d3c04d3ed8`

## 1. Verdict

**CHANGES REQUESTED** — the Frontend has two evidence-backed MAJOR performance problems: its global
idle strategy recursively preloads every lazy route, and the Linking Quran viewport rebuilds a
6,236-object backing array during range changes. The 100-card Words detail surface remains a high-
priority measurement target rather than a confirmed defect without a browser profile.

## 2. Scope reviewed

The review covered the Angular production paths across:

- App bootstrap, configuration, route graph, and deferred hosts.
- `core/**` and `shared/**`, excluding generated API clients.
- Authentication, Dashboard, and Access Admin.
- Mushaf and every Words explorer/detail family.
- Abwab and Linking, including their virtual lists, workflows, recovery, and preflight paths.
- Component templates/styles, signals/effects, request ownership, caches, lazy loading, observers,
  listeners, and Quran rendering primitives.

Three parallel passes plus the final coverage pass reviewed **765 tracked production
TS/HTML/SCSS/JSON files and 78,880 LOC**. The reproducible inventory excludes `*.spec.ts`, generated
API clients, and generated permission output:

- App/core/shared/auth/access/dashboard/global styles: 183 files / 14,900 LOC.
- Mushaf/Words: 351 files / 36,475 LOC.
- Abwab/Linking: 231 files / 27,505 LOC.

The final pass added all 12 global style partials and the three statically imported Mushaf catalog/
ligature JSON files omitted from the initial slices. The JSON assets total about 23.8 KB raw and add
no separate material finding; their lazy-route cost is already represented in APR-1. Overlap from
following immediate callers and shared primitives was de-duplicated in this report.

Evidence and limitations:

- No application service or browser was started; no build, test, Lighthouse run, Angular DevTools
  profile, heap capture, or bundle regeneration was performed.
- A clean-tree production artifact generated minutes after the reviewed HEAD was inspected as
  contextual evidence, not formal same-build proof. Its initial JavaScript was 306,930 bytes raw
  (about 81,451 gzip-sum), and CSS was 90,313 bytes raw (about 15,427 gzip). This does not support an
  initial-bundle-size finding against the configured 700 kB warning / 800 kB error threshold.
- The same artifact and route graph support an estimated 1,484,821 raw / 360,015 gzip-sum bytes
  across 78 additional files fetched by automatic post-idle route preloading. Those values remain
  estimates until a current instrumented build/network trace reproduces them.
- Tests/specs/contracts, generated output, dependency auditing, and visual-design review were
  excluded.

## 3. Findings

### MAJOR

#### APR-1 — The global idle strategy recursively preloads every lazy route

- `Frontend/quran-dashboard-ui/src/app/app.config.ts:37` installs
  `withPreloading(IdlePreloadStrategy)`.
- `Frontend/quran-dashboard-ui/src/app/core/navigation/idle-preload.strategy.ts:8-21` schedules an
  independent idle callback for every route, forces it after three seconds (or a 1.5-second timer
  fallback), and unconditionally calls `load()`.
- Angular recursively applies the strategy to both `loadChildren` and `loadComponent` boundaries.
  `Frontend/quran-dashboard-ui/src/app/app.routes.ts:35-70` therefore exposes Dashboard, Mushaf,
  Words, Abwab, callback, and owner-only Access Admin trees to post-idle loading.
- `Frontend/quran-dashboard-ui/src/app/app.ts:17-24` separately idle-prefetches the entity overlay and
  Linking workspace hosts.
- The likely-current artifact maps 817,375 raw / 182,923 gzip-sum bytes directly to route entry/page
  chunks; static dependency closure estimates 1,484,821 raw / 360,015 gzip-sum bytes across 78
  additional files beyond the dashboard baseline.
- Impact: a short dashboard visit can pay transfer, parse/compile, allocation, and request-concurrency
  costs for unrelated or inaccessible features. The healthy initial bundle does not prevent this
  post-idle work.
- Suggested direction: make preloading explicit and selective; exclude callback, placeholders,
  owner-only administration, and expensive feature trees unless usage data justifies them. Use one
  budgeted scheduler with bounded concurrency and save-data/network awareness. Preserve
  `@defer when`; change prefetch timing only after measuring first-open latency.

#### APR-2 — Linking rebuilds and re-diffs a 6,236-row array during virtual-scroll range changes

- `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-virtual-ayah-list/linking-virtual-ayah-list.component.ts:246-248`
  calls `loadRange` on every
  `renderedRangeStream` emission without first de-duplicating the corresponding backend page range.
- Cached pages can return synchronously from
  `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source-pages.facade.ts:148-151`.
- Every accepted range calls `ensureRows([], totalItems)` and replaces the rows signal at
  `linking-virtual-ayah-list.component.ts:294-317`; `:359-375` allocates an object for every placeholder
  via `Array.from({ length })`. The legal Quran total is 6,236 positions.
- The template passes every new array reference through `*cdkVirtualFor` at
  `linking-virtual-ayah-list.component.html:12`. Stable `trackRow` protects node identity, but does not
  remove the O(6,236) allocation and iterable-diff work.
- `linking-virtual-ayah-list.component.ts:319-324` also re-emits unchanged page metadata. Consumers
  write it into parent state at `linking-inline-source-workflow.controller.ts:92-115` and
  `linking-source-editor.facade.ts:137-154`.
- Impact: scrolling can allocate thousands of objects, diff the full backing collection, and fan out
  parent signal writes even when the required backend page is already cached.
- Suggested direction: de-duplicate by backend page-range key; create the placeholder backing array
  once per request identity/total; update only loaded positions; do not set unchanged rows; emit
  revision/totals/types only when they change. Keep measured variable-height virtualization and do
  not render the complete Quran in the DOM.

### MINOR

#### APR-3 — Floating layers synchronously read and write layout on every captured scroll event

- `Frontend/quran-dashboard-ui/src/app/shared/ui/floating-layer/floating-layer.directive.ts:119-124`
  registers an unthrottled capture-phase window scroll handler and directly calls `reposition()`.
- `floating-layer.directive.ts:157-164` invokes placement; `floating-layer-placement.ts:103-124`
  reads bounding boxes, dimensions, direction, and root font size, then immediately writes styles and
  dataset properties.
- The primitive is used by searchable pickers and context menus across Mushaf, Words, and Abwab.
- Impact: an open layer can force repeated layout/style work at scroll-event frequency inside the
  zone-based app.
- Suggested direction: coalesce viewport changes into one `requestAnimationFrame`, register
  high-frequency listeners outside Angular, cache invariant values while open where safe, and cancel
  the pending frame during teardown.

#### APR-4 — Access Admin duplicates detail reads and performs four GETs after every mutation

- `Frontend/quran-dashboard-ui/src/app/features/access-admin/state/access-admin.facade.ts:163-180`
  requests both user detail and user permissions on every selection, although the detail response
  already contains status, owner flag, version, and permission codes. Both backend endpoints execute
  the same user-load query.
- Mutation payloads are accepted then discarded at `access-admin.facade.ts:278-286`.
- `access-admin.facade.ts:328-333` subsequently reloads detail, permissions, the user list, and the
  global permission catalogue; success/busy release waits for this refresh at `:286-295`.
- Impact: selection duplicates backend work, and each write adds a four-request refresh wave and
  user-visible wait.
- Suggested direction: derive the draft from the authoritative detail response, reconcile returned
  mutation data, and refresh only representations absent or version-invalidated. Do not weaken
  version/concurrency semantics.

#### APR-5 — Visible Linking rows repeatedly allocate presentation models from template methods

- `Frontend/quran-dashboard-ui/src/app/features/linking/components/linking-virtual-ayah-list/linking-virtual-ayah-list.component.html:14-16,24,28,34,50-52`
  invokes several methods per visible
  row during change detection.
- `linking-virtual-ayah-list.component.ts:124-160` creates three `Set` instances plus new word and ayah
  objects; `:163-165` repeatedly scans overlays; `:172-177` uses a linear `includes` lookup, including
  through grouped-start/end checks.
- The viewport bounds the number of visible rows, so this is not the full-array problem in APR-2,
  but the per-row allocations are on the scroll and selection path.
- Suggested direction: build a memoized row presentation model when a page or selection revision
  changes and expose selection as a computed `Set`. Preserve exact canonical word IDs, order, text,
  grouping, and highlighting.

#### APR-6 — Opening the Linking door step cancels and restarts its own preload

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts:221-225,377-396`
  calls `doors.ensureLoaded()` before showing the door step.
- The step instantiates `AbwabManagementPicker`; its constructor unconditionally calls `load()` at
  `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-management-picker/abwab-management-picker.component.ts:131-136`.
- `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-snapshot.facade.ts:49-56` cancels the
  current request before starting another GET.
- Impact: the intended preload is canceled/restarted, or followed by an extra conditional GET if it
  completed quickly, adding a round trip to an interactive flow.
- Suggested direction: use `ensureLoaded()` on picker creation; reserve `load()` for explicit refresh
  and retry.

#### APR-7 — Linking retains per-preflight signals for the application lifetime

- Root-scoped
  `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-preflight-details.facade.ts:29-50`
  stores a state entry for each encountered `preflightId`.
- Cancel/evict at `:68-78` release requests/cache but do not remove state; `:165-171` permanently
  creates the corresponding signals.
- Impact: every unique preflight visited in a long session leaves unreachable state until the whole
  application is destroyed.
- Suggested direction: remove state on eviction and user change after canceling its active range.

#### APR-8 — Selecting a Mushaf word eagerly loads full Ayah Study content

- Word selection writes both word and verse into URL state at
  `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-url-sync.ts:90-110`.
- Hydration independently marks Word Analysis and Ayah Study for reload at
  `mushaf-url-hydration.ts:49-76`; the facade starts both at
  `mushaf-reader.facade.ts:518-554`.
- The active Analysis surface renders word analysis and door content, not the study source blocks, at
  `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/study-context-section/study-context-section.component.html:45-55`.
  Until Sources/Similarity is opened, the study payload
  is used only for the related-count badge at `study-context-section.component.ts:116-127`.
- `Frontend/quran-dashboard-ui/src/app/features/mushaf/data-access/mushaf-ayah-study.api.ts:19-37`
  issues the request;
  `Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts:119-123,181-187`
  shows that the response carries tafsir, translation, full I'rab, ayah core, source metadata, and
  similarity summary.
- Impact: a common word-click path pays an additional text-heavy transfer, parsing, mapping, caching,
  and backend read even when the user never opens study content.
- Suggested direction: load Ayah Study when its group is opened, or fetch a lightweight authoritative
  summary for the badge. Keep the existing unknown badge state until real data arrives; do not
  synthesize counts or detach content from provenance.

#### APR-9 — Superseded Mushaf requests are ignored but not consistently canceled

- `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.ts:379-415`
  overwrites the page subscription without first unsubscribing it; a token prevents stale writes but
  only the latest stored subscription remains available for teardown.
- Similar-Ayah and Mutashabihat runners do not retain the subscription returned by their load helper
  at `mushaf-similar-ayahs-load.runner.ts:61-88` and
  `mushaf-mutashabihat-load.runner.ts:61-88`; their clear methods only cancel timers and advance a
  token at `:26-33`.
- Ayah Study and Word Analysis already demonstrate explicit active-request ownership and cancellation.
- Impact: rapid page, ayah, or tab changes can leave superseded HTTP, parsing, cache population, and
  backend work running even though correctness guards reject stale UI writes.
- Suggested direction: give these loads the same subscription ownership as Ayah Study/Word Analysis,
  cancel before replacement/clear, and retain the request token as defense in depth.

### NOTE — measure before changing

#### APR-10 — Words ayah-detail surfaces render 100 full Quran cards without virtualization

- Every Words family requests a detail page size of 100:
  `Frontend/quran-dashboard-ui/src/app/features/words/models/roots.models.ts:148-151`,
  `lemmas.models.ts:152-155`, `stems.models.ts:147-150`,
  `unique-words.models.ts:119-123`, and `word-types.models.ts:265-271`.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/ayah-matches-list/ayah-matches-list.component.ts:48-61`
  maps every item, while `ayah-matches-list.component.html:25-68` creates a normal full card for every
  result.
- Each card renders each non-marker Quran word as its own span at
  `Frontend/quran-dashboard-ui/src/app/features/words/components/highlighted-ayah/highlighted-ayah.component.ts:16-20`
  and `highlighted-ayah.component.html:1-10`.
- `ayah-matches-list.component.scss:16-40` provides neither virtualization nor `content-visibility`;
  overlay mode expands all cards into normal document flow. The component is reused 17 times across
  nine templates covering Roots, Lemmas, Stems, Unique Words, and Word Types.
- A full response deterministically creates 100 cards and roughly one span per Quran word, but no
  browser trace in this review establishes material frame/layout cost. Profile the 100-row inline and
  overlay cases first. If material, reduce detail page size or use the existing measured variable-
  height viewport approach; never substitute a guessed fixed height.

#### APR-11 — Root caches are bounded by entry count, not retained payload weight

- `Frontend/quran-dashboard-ui/src/app/core/caching/api-response-cache.ts:5-10,59-73` retains up to 48
  responses per cache and evicts by entry count, not bytes/rows.
- Five root-scoped Words caches can each retain responses containing 1,000 rows; virtualization limits
  DOM nodes but not transfer, JSON decoding, view-model allocation, or cached heap.
- In-flight de-duplication and LRU bounds are already present, so this is not an unbounded-cache
  finding. Measure response weights and heap after representative multi-filter churn; use weighted
  budgets or route-scoped caches only if retained memory is material.

#### APR-12 — Every Mushaf word installs eight event bindings, including `pointermove`

- `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-line/mushaf-line.component.html:19-32`
  instantiates a component per word.
- `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-word/mushaf-word.component.html:16-23`
  binds pointer down/move/up/cancel/enter/leave, context menu, and
  click. The move handler is useful only while a touch/pen long press is active.
- Measure scripting/change-detection activity while moving across a page. If material, delegate at
  line/page level or attach move/end handlers only for an active gesture, outside Angular until an
  output is required.

#### APR-13 — Large Linking draft equality uses repeated full serialization

- `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace-configuration-sync.runner.ts:118-121,159-195`
  compares drafts before and after
  sends; `:236-248,304-306` uses `JSON.stringify` for drafts, selections, and descriptions.
- A legal draft can include thousands of ayah/word selections, but `all-except` representation and a
  250 ms debounce usually bound the path.
- Measure long tasks and allocations first. If material, use dirty slices/revision counters and typed
  comparisons rather than full serialization.

### Rejected false positives and healthy evidence

- All reviewed `@for` blocks provide tracking; hot lists use stable entity/verse/word identities.
- The 1,000-row explorer tables use `QdDataTableComponent` with measured variable-height CDK
  virtualization and stable row IDs; they do not render 1,000 normal DOM rows.
- `ApiResponseCache` is a 48-entry LRU and coalesces in-flight requests; it is bounded.
- Linking page scheduling/cache prevents duplicate HTTP for the same cached page during range churn.
  APR-2 is an allocation/diff/state-fan-out problem, not a duplicate-network claim.
- The Abwab tree is not virtualized, but the live read-only database estimate was only 16 doors; no
  scale defect is claimed.
- `SessionScrollStateDirective` coalesces with `requestAnimationFrame` and disconnects observers and
  listeners; no hot leak was established.
- Reviewed component subscriptions, timers, Resize/Mutation observers, polling, recovery leases, and
  BroadcastChannel ownership have bounded lifetimes or cleanup paths apart from APR-7/APR-9.
- Bidirectional Abwab/Linking ownership is already reported as Engineering ER-6; without a measured
  chunk-cost delta it is not repeated here as a performance finding.
- Mushaf/Words route groups are lazy, entity overlay adapters are guarded by `@defer`, and the
  likely-current initial artifact is under its configured budget.
- CSS searches found no broad perpetual animation/filter hot path; Quran glyphs are not animated and
  relevant motion honors reduced-motion preferences.
- Missing `OnPush` alone was not treated as a finding. The heavy Words and Mushaf page/line/word
  render chains already use it, and no proportional hot defect was proven for the exceptions.

## 4. Quran rendering safety

**PASS for the recommendations.** None trades away Uthmani text accuracy, canonical word IDs, source
provenance, Arabic shaping, font choice, RTL, reading order, selection/highlighting, readability, or
accessibility. APR-2/APR-10 must not be fixed with guessed fixed heights or virtualization that breaks
focus, text selection, screen-reader semantics, browser find, grouping, or complete word rendering.
If those guarantees cannot be demonstrated, smaller authoritative pages are preferable.

## 5. Next step

Fix APR-2 first because it puts deterministic O(6,236) allocation and iterable-diff work directly on
scroll. In parallel, replace global route preloading with an explicit measured policy and profile the
100-card ayah-detail case before choosing smaller pages versus measured virtualization. Then address
the bounded request/layout/state findings. Validate closure with a current production build plus cold
0–10 second Network/Performance trace, Angular scroll/DOM profile, rapid-navigation cancellation
trace, Access Admin request count, and heap snapshots; preserve the Quran safety constraints above.
