# 09 — API Surface & Payload Audit (Audit H)

Audited branch: `dev` · commit `72792ba9` · date 2026-08-08 · author: report 09
Brief sections covered: §17 (Audit H), §24 (generated-code trace), mandatory questions 43–52.

Input evidence: `data/endpoint-inventory-backend.json` (85 operations, auth, data paths, DTO
field sources, payload estimates), `data/endpoint-consumers-frontend.json` (call sites, 671
field classifications, chatty-screen and caching analysis). Machine-readable output of this
report: `data/endpoint-classification.json` (all 85 operations, consumed by the api-explorer).

Every load-bearing claim taken from the data files was independently re-verified in the repo
by this author; corrections found during re-verification are called out explicitly
(§3.1, §5.3). This is an audit: it proposes and classifies. It contains no implementation
instructions.

---

## 1. Inventory summary (Q43)

**CONFIRMED — 85 operations over 78 paths, 229 schemas.** Independently recounted by this
author from `Frontend/quran-dashboard-ui/openapi/swagger.json` (parse of `paths`, methods
GET/POST/PUT/DELETE): 78 paths, 85 operations, 229 component schemas — identical to
`data/endpoint-inventory-backend.json` `swagger_stats`.

| Method | Count |
|---|---|
| GET | 58 |
| POST | 18 |
| DELETE | 5 |
| PUT | 4 |

By feature area (backend inventory `swagger_stats.by_feature_area`):

| Area | Operations |
|---|---|
| Words (Lemmas/Roots/Stems/UniqueWords/WordTypes/WordTypeGroupedDetails) | 38 |
| Abwab (doors/sections/templates/relations/tree) | 25 |
| Access (users/permissions/audit/relink/reconciliation/me) | 13 |
| MushafReader (pages/study/similarities/mutashabihat/word analysis/catalogs) | 7 |
| System (dashboard, health) | 2 |

Auth distribution (CONFIRMED, `endpoint-inventory-backend.json` notes[0]): 51 anonymous
(every GET outside Access), 21 `permission:<code>`, 12 owner, 1 authenticated
(`GET /api/access/me`). There is no fallback authorization policy
(`Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:51`), and
`UnsafeEndpointMetadataValidator`
(`Backend/api/QuranDashboard.Api/Authorization/Validation/UnsafeEndpointMetadataValidator.cs:34-99`)
forces every POST/PUT/DELETE to carry exactly one `[RequirePermission]`/`[RequireOwner]` at
startup. All 27 write operations live in Abwab (21) and Access (6).

Endpoint classification totals (this report's output, `data/endpoint-classification.json`):

| Classification | Count |
|---|---|
| KEEP | 70 |
| INTERNAL_ADMIN | 10 |
| SHRINK_RESPONSE | 3 |
| DELETE_OR_DEPRECATE_CANDIDATE | 2 |
| MERGE_CANDIDATE / SPLIT_OR_LAZY_LOAD / NEEDS_MEASUREMENT | 0 |

Zero MERGE_CANDIDATE is a deliberate verdict, not an omission: the only near-duplicate pair
(`GET /api/words/word-types/words` vs `/table`) resolves as a deprecation, and the
brief (§17) forbids merging endpoints merely to reduce endpoint count (see §6.1 below).

---

## 2. Contract parity and its fragility (Q44)

**CONFIRMED — zero drift in both directions.** All 85 Swagger operations map to a live
controller action and all 85 controller actions appear in Swagger
(`endpoint-inventory-backend.json` `drift.verdict`; route+method matched after normalizing
`{param:constraint}` templates). Spot-check by this author: the operation/method/schema
counts match an independent parse, and every endpoint sampled during this audit
(audit-events, words lists, abwab tree, word analysis, surah catalog) had a live controller.

The mechanism that produces this parity:

- `Backend/scripts/export-swagger:22-31` builds the API in Release and runs the Swashbuckle
  CLI against the built DLL, writing directly to
  `Frontend/quran-dashboard-ui/openapi/swagger.json`. Startup permission-catalogue sync is
  disabled during generation so spec export never mutates a database (verified by reading
  the script).
- `Backend/scripts/check-api-contract:7-24` re-runs the export, regenerates the frontend
  client (`npm run generate:api`), then fails (`git diff --exit-code`) if the committed spec
  or generated output differs from the code. Verified by reading the script.

**The fragility (CONFIRMED): nothing runs this check mechanically.** There is no `.github/`
directory in the repo (verified: `ls .github` fails), the frontend `test:pre-pr` script runs
permission-catalogue, audit-action-type, typecheck, build and test gates but **not**
`check-api-contract` (`Frontend/quran-dashboard-ui/package.json:28`), and no reference to
`check-api-contract` exists in `Backend/scripts/test-backend`, any `.claude/skills/` file,
or `TESTING_STRATEGY.md` (verified by grep — the only references are descriptive READMEs and
`docs/contracts/http-api.md:18-22`). Parity today is the product of human discipline: someone
ran the check after the last API change. The measured zero drift proves the discipline has
held **so far**; it does not prove it will hold. Classification of the parity guarantee
itself: **LIKELY durable, with a single-point-of-failure cadence** — a candidate for the
workflow-cost report (10) rather than an API-surface change.

---

## 3. Response-field analysis (Q45)

**CONFIRMED baseline** (`endpoint-consumers-frontend.json` `models.totals`): 143 payload
models analyzed (232 generated model files minus 79 `ApiResponse`/paged wrappers and 10 type
aliases), 671 payload fields total:

| Field status | Count | Share |
|---|---|---|
| USED | 460 | 68.6% |
| UNKNOWN_CONSUMER | 157 | 23.4% |
| UNUSED_CANDIDATE | 54 | 8.0% |

The `UNKNOWN_CONSUMER` block is an honest static-analysis limit, not suspicion of deadness:
generic names (`id`, `name`, `status`, `version`…) cannot be attributed to a specific model
by text grep. A type-aware pass (LSP find-references) would resolve most of them —
NEEDS_MEASUREMENT. Per brief §4, **no UNKNOWN_CONSUMER field is a removal candidate in this
report.**

### 3.1 Author re-verification of the field inventory — one correction, one confirmation

Spot-verifying the top UNUSED_CANDIDATE and adjacent USED claims produced one correction and
one confirmed classification with a new nuance, both reflected in
`data/endpoint-classification.json`:

1. **`AccessAuditEventItem.metadata` was misclassified USED.** The inventory's evidence line
   points at `mushaf-study-source-catalog.api.mock.ts:39` — a generic-name cross-match in an
   unrelated mushaf mock. Author re-grep of `src/app/features/access-admin` production code
   (ts+html, non-spec) found **zero** occurrences of `metadata`. It is in fact a 6th
   unrendered field of the audit item, and a jsonb one (see §4.1). CONFIRMED.
2. **`OwnerReconciliationStatus.candidates` is USED — and the inventory already evidenced
   it correctly.** Two of its three evidence lines (`word-types-url-sync.ts:342,348`) are
   generic-name cross-matches, but the third is the real consumer:
   `access-admin-page.component.html:97` iterates `status.candidates` and renders
   `candidate.normalizedEmail` and `candidate.state`. The author pass confirmed the USED
   call rather than corrected it. What is new is the nuance this adds to the
   "7 never-referenced models" list: `OwnerReconciliationStatusCandidate` is
   *nominally* unreferenced (the interface name is never imported) but its **fields flow to
   the template** via the parent object. Nominal never-referenced ≠ dead. CONFIRMED.

Consequence: the effective unused-candidate count is **55 fields** (54 from the inventory
plus `metadata`), and one "never-referenced model" is actually load-bearing. All other
spot-checks (audit snapshots/states ×4, `AccessUserSummary.permissionCount`,
`AbwabTreeDoorDto.directChildCount`, `SimilarAyahItemDto.score/matchedWordsCount/hasReverseLink`,
`WordMorphologyRoot/Lemma.buckwalter` vs the rendered `root?.text/lemma?.text/stem?.text` in
`word-morphology-summary.component.html:26-63`) confirmed the inventory exactly.

---

## 4. Query-cost classification of the unused candidates (Q46, Q47)

Brief §17 forbids claiming "remove JSON field = fewer DB queries" without proving the query
path. This author read every reader named below. The 55 unused-candidate fields bucket into
the brief's five classes as follows.

### 4.1 Flagship: the audit-log projection (`GET /api/access/audit-events`)

**CONFIRMED, traced end-to-end by this author.**

- `EfAccessAuditReader.ListAsync`
  (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Access/EfAccessAuditReader.cs:16-63`)
  materializes **full `AccessAuditEvent` entities** — there is no `Select` projection — with
  two `Include`s (`ActorUser`, `TargetUser`). Every column of `access_audit_events` is
  fetched for every page row, including five jsonb columns: `actor_snapshot`,
  `target_snapshot` (required), `before_state`, `after_state` (nullable), `metadata`
  (required) — column types CONFIRMED at
  `Persistence/Configurations/Access/AccessAuditEventConfiguration.cs:66-100`.
- Per row, `ToItem` (`EfAccessAuditReader.cs:99-116`) then runs `JsonDocument.Parse` on four
  of them and `JsonSerializer.SerializeToElement` on the fifth — CPU work per event, per
  page load.
- The DTO (`AccessAuditEventItem`,
  `Backend/application/QuranDashboard.Application.Abstractions/Access/AccessAuditContracts.cs:16-33`)
  carries all five into the response envelope.
- The only consumer (`access-audit-log.component.html`) renders `actionType`,
  `occurredAtUtc`, `permissionCode`, actor/target display labels — and none of the five
  document fields (author grep: zero non-spec occurrences of
  `actorSnapshot|targetSnapshot|beforeState|afterState|metadata` in access-admin production
  code; spec files construct them as `{}` fixtures only).

Honest cost classes for removing the five fields from the **list response**:

| Change | Class per brief §17 |
|---|---|
| Drop from DTO only (reader still materializes entities) | (5) serialization/network + per-row parse CPU removed |
| Also convert reader to a `Select` projection | adds (2) smaller EF projection — jsonb columns no longer fetched |
| Join removal | **No** — the two user `Include`s feed display names that ARE rendered |
| Fewer queries | **No** — same single query either way |

The audit **storage** is untouched in every variant — before/after states and snapshots
remain written and queryable in the database, which is where §29's auditability protection
actually lives. What shrinks is a page payload whose per-item size is dominated by documents
nobody renders (17-field items where 5 fields are unbounded JSON documents;
`payload_estimates`: 20 KB typical / 150 KB high-end per page of 25 — NEEDS_MEASUREMENT for
the exact snapshot-size distribution).

### 4.2 Full bucket table for the 55 unused-candidate fields

| Bucket | Fields | Evidence | Class |
|---|---|---|---|
| **Removed-query wins (4)** | `OwnerReconciliationStatus.lastReconciliation` | Sole consumer of `GetLatestOwnerReconciliationAsync` — a `FromSqlRaw` jsonb-path scan over `access_audit_events` with no supporting index (`EfAccessAuditReader.cs:78-89`; handler wiring `GetOwnerReconciliationStatusHandler.cs:12`). Dropping the field deletes one query per admin page load. CONFIRMED | **(4) fewer queries** |
| **Removed-join wins (3)** | `RenderedSegmentDto.i3rabRuleId`, `.i3rabRuleFamily`, `.i3rabRuleSignature` | The `QuranI3rabRules` LEFT JOIN in `EfWordAnalysisReader.LoadSegmentsAsync` (`EfWordAnalysisReader.cs:148-163`) produces only `SignatureKey`/`RuleFamily`, and `segment.I3rabRuleId` feeds only the id field; all three are unrendered. Removing all three removes the join. CONFIRMED | **(3) removed join** |
| **Removed-subquery win (1)** | `AccessUserSummary.permissionCount` | Correlated `user.UserPermissions.Count` inside the list `Select` (`EfAccessUserReader.cs:52-54`) → COUNT subquery per row in SQL. Unrendered. Small absolute cost (small user table) but a genuine query-shape change. CONFIRMED | **(3) removed subquery** |
| **Smaller-projection + serialization (6)** | `AccessAuditEventItem.actorSnapshot`, `.targetSnapshot`, `.beforeState`, `.afterState`, `.metadata`; `RenderedSegmentDto.segmentFeatures` | §4.1 above; `segmentFeatures` projects `FeaturesRaw`/`FeaturesJson` columns (`EfWordAnalysisReader.cs:165-166`) into a DTO nothing consumes (`SegmentFeaturesDto` is one of the never-referenced models). CONFIRMED | **(2)+(5)** |
| **Projected-column scalars, DTO+serialization only (~30)** | `AccessUserSummary/Detail` timestamps + `userName`; `PermissionCatalogueItem.englishDescription`; `TafsirEntryDto`/`FullI3RabEntryDto` `shortNameAr`/`isGroupLeader`/`sourceLeaderVerseKey`; `TranslationEntryDto.containsHtmlMarkup`; `WordMorphologyDto.caseFeature`/`.verbVoice`; `WordMorphology{Root,Lemma}.buckwalter`; `WordOccurrenceDto` ×5 (`textUthmaniSimple`, `textImlaeiSimple`, `qpcGlyph`, `wordNumber`, `lineWordOrder`); `MushafWordDto.wordNumber`/`.lineWordOrder`; `PageMarkerDto.sajdahType`; `SajdaDto` ×2; `SurahOnPage` ×2; `AyahRange` ×2; `PageNavigationSummary.hizbNumbers`/`.rubNumbers`; `UniqueSimpleWordCountSummary.wordKeyImlaeiSimple`; `MutashabihatGroupDto.sourceGroupId` | Columns are projected (or trivially derived) inside joins/queries the *used* fields need anyway; several are used server-side for ordering (`LineWordOrder` — `EfMushafPageReader.cs:37,80`; `SourceGroupId` — `EfAyahMutashabihatReader.cs:61`), so the column fetch stays even if the DTO field goes. LIKELY (per-field reader checks sampled, not exhaustive) | **(2) and/or (5)** |
| **In-memory-derived, DTO-only (~15)** | `AbwabTreeDoorDto.directChildCount` (computed from already-fetched doors, `EfAbwabTreeReader.cs:25-28`); `SimilarAyahItemDto.score`/`.matchedWordsCount`/`.hasReverseLink` (score is used server-side for ordering, `EfAyahSimilaritiesReader.cs:62`); `MutashabihatOccurrenceDto`/`MutashabihatSelectedOccurrenceDto` `isRepresentative`/`wordTo` (`WordTo` feeds `DerivePhraseText` server-side, `EfAyahMutashabihatReader.cs:118`); `MutashabihatGroupDto.selectedOccurrences` | Deriving data is already in memory; removal changes bytes on the wire and DTO width only. CONFIRMED for the named readers | **(1)+(5)** |

**Q46 direct answer:** exactly **10 fields** have real per-request DB/query cost beyond
serialization — the 5 audit-item documents (column fetch + parse), `lastReconciliation`
(whole extra raw-SQL query), the 3 word-analysis rule fields (LEFT JOIN), and
`permissionCount` (correlated subquery). All 10 are flagged
`EXPENSIVE_UNUSED_CANDIDATE` in `data/endpoint-classification.json`.

**Q47 direct answer:** the remaining ~45 unused candidates are network/serialization and/or
DTO-width overhead only — no query-shape change is provable for them, and several would keep
their column fetch regardless because the value is used server-side for ordering or phrase
derivation.

**Later verification (brief §4, question 6) for the three SHRINK_RESPONSE endpoints**
(`audit-events`, `owner-reconciliation/status`, `words/{wordLocation}/analysis`): the
dropped properties disappear from the spec and generated client via `check-api-contract`
(regenerate + `git diff`); a template-parity grep proves no handwritten consumer references
the removed fields; and for audit-events specifically, an assertion that list items no
longer carry the document fields while the stored `access_audit_events` row still does —
storage untouched, per the §29 risk analysis above.

---

## 5. Over-fetching (Q48)

### 5.1 The single biggest payload lever is not a field at all: JSON escaping

**CONFIRMED (configuration absence):** no `AddJsonOptions`, `JavaScriptEncoder`, or
`UnsafeRelaxedJsonEscaping` exists anywhere under `Backend/api`, `Backend/application`,
`Backend/infrastructure`, `Backend/shared` (author grep, zero hits). System.Text.Json's
default encoder therefore escapes every non-ASCII character as `\uXXXX` — **6 bytes per
Arabic character versus 2 bytes as raw UTF-8** — in every response of this Arabic-first
product. **CONFIRMED additionally:** no response-compression middleware exists in the API
(grep for `ResponseCompression|brotli|gzip`: zero hits), so nothing on the app server
mitigates the inflation; whether Railway's edge compresses is UNKNOWN (out of repo).

Effect magnitude is LIKELY ~3× on Arabic string content (exact wire bytes
NEEDS_MEASUREMENT — no runtime payload capture was taken in Phase 1b). All payload estimates
in the backend inventory assume escaped output.

Per the audit contract this is recorded as a **finding, not a fix**: the remediation class
is a single-line encoder option plus a §29 check (OpenAPI contract parity — the spec itself
does not change, but golden-file tests or snapshot assertions on serialized output would).
It is called out because it dwarfs every field-removal saving in this report combined.

### 5.2 1000-row default pages on the words explorers — deliberate, but heavy

**CONFIRMED on both sides of the wire.** Backend: `DefaultListPageSize = 1000` in
`UniqueWordsController.cs:21`, `RootsController.cs:27`, `StemsController.cs:25`,
`WordTypesController.cs:24`, `LemmasController.cs:25`. Frontend: this is not an accidental
fallback — the client *explicitly requests* 1000
(`UNIQUE_WORDS_PAGE_SIZE = 1000`, `unique-words.models.ts:119`; `ROOTS_LIST_PAGE_SIZE = 1000`,
`roots.models.ts:160`; `LEMMAS_LIST_PAGE_SIZE = 1000`, `lemmas.models.ts:153`) and renders
through `cdk-virtual-scroll-viewport` (`unique-words-table.component.html:86`) with a
per-query session cache (48-entry LRU, `api-response-cache.ts:5`).

Estimated payloads (backend inventory, LIKELY, escaped-output basis):

| Endpoint | Typical | High-end |
|---|---|---|
| `GET /api/words/word-types/words` (dead — §6.1) | 400 KB | 550 KB |
| `GET /api/words/word-types/table` | 350 KB | 500 KB |
| `GET /api/words/unique/{kind}` | 280 KB | 400 KB |
| `GET /api/words/lemmas` / `roots` / `stems` | 250 KB | 350 KB |

Adjudication: **not naive over-fetch** — every DTO field on these lists is consumed
(§3), the page size is a considered UX design (fetch once, virtual-scroll, cache), and
in-flight dedupe prevents duplicate fetches. The recurring cost is the byte volume per
distinct query, which the encoder finding (§5.1) would cut by an estimated 30–60% without
touching the design (LIKELY; NEEDS_MEASUREMENT). Field removal offers nothing here.
Whether 1000 is the right number versus 250/500 is a product/latency tradeoff:
NEEDS_MEASUREMENT (no telemetry on real filter usage or page-load timing exists in the repo).

### 5.3 Abwab tree and soft-deleted doors — a corrected claim

The tree reader fetches **all** doors with no `DeletedAtUtc` filter while sections and
aliases are filtered live-only (`EfAbwabTreeReader.cs:15-17` vs `:10-12,:19-21`). The task
hypothesis was that soft-deleted doors are over-fetch. **Author verification refutes the
unused-claim:** the DTO marks them (`isArchived`) and the frontend actively consumes the
flag — `abwab-tree.builder.ts:37,40,72,77` filters and *renders archived doors* (archived
subtree display with an `includeArchivedChildren` toggle). This is product behavior, not
waste. CONFIRMED.

What remains true: the payload grows monotonically with archive size forever, and the
response includes an `UNUSED_CANDIDATE` field (`directChildCount`) plus an expensive used
field (`relationCount`, which costs a full live-relations pair scan per uncached tree load —
`EfAbwabTreeReader.cs:61-77`, mitigated by the ETag/304 conditional-GET layer,
`abwab-snapshot.facade.ts:49`). Door/archive row counts are asserted nowhere in code or
tests — payload magnitude NEEDS_MEASUREMENT.

### 5.4 Audit events (§4.1) — the one true field-level over-fetch

5 of 17 item fields are unrendered JSON documents; per-page savings 40–70% of item bytes is
plausible but NEEDS_MEASUREMENT (snapshot sizes depend on stored permission-code lists).

### 5.5 What is *not* over-fetching (positive findings)

- Mushaf reader: pages ~35 KB typical (139 words/page derived from canonical counts), study
  fields split per-tab and lazy-loaded, adjacency prefetch is cache-guarded
  (`endpoint-consumers-frontend.json` screens/mushaf-reader-page). Correct by design.
- Words detail drill-downs default to 100 rows, one call per tab activation, cached.
- 14 backend `IMemoryCache` reader decorators + ETag conditional GET on abwab
  (backend inventory notes[3]) reduce repeat query cost on the hot read paths.

---

## 6. Unused endpoints and never-referenced models (Q51)

### 6.1 `GET /api/words/word-types/words` — DELETE_OR_DEPRECATE_CANDIDATE

Author-verified: the wrapper `WordTypesApi.getRows` (`word-types.api.ts:88`) has **zero
production callers** — the only 3 callers are its own spec file
(`word-types.api.spec.ts:102,128,148`); the explorer uses `getTableRows` →
`GET /api/words/word-types/table` (`word-types-explorer.facade.ts:278`). No
`Backend/tools`/`Backend/scripts` consumer (author grep: zero hits). The `/table` endpoint's
Words view internally reuses the same EF reader method (`EfWordTypesReader.cs:117-124`), so
the endpoint is a fully superseded external surface over a still-needed internal path.

Brief §4's seven answers:

1. **Value today:** none found in-repo; duplicate of `/table?tableView=words` at the API
   boundary.
2. **Dependents:** 3 frontend spec tests; `WordTypesCacheKeys.rows()` factory
   (`word-types-cache.ts:15`); smoke route-baseline tests pin the route inventory
   (`SmokeRouteCatalog.cs`) and would need a deliberate baseline update; 3 swagger schemas +
   generated models exist only for it.
3. **Risk:** an out-of-repo consumer (external tool, script on another machine) breaks.
   The API is publicly readable (anonymous GET), so absence of an in-repo consumer is not
   proof of absence of consumers — per §4 this stays a *candidate*.
4. **Equivalent protection elsewhere:** yes — `/table` returns a superset via the same
   reader; consumers have a direct replacement.
5. **Smallest safe step:** deprecation marking + access-log observation window, not
   deletion (observation infrastructure NEEDS_MEASUREMENT — no request logging analysis
   exists in the repo).
6. **Later verification:** route disappears from swagger + `check-api-contract` diff;
   smoke route baseline updated deliberately; grep proves no dangling wrapper/cache-key.
7. **Recurring cost removed:** ~134 backend LOC (controller action ~33,
   `GetWordTypeRowsQuery` 15, `GetWordTypeRowsHandler` 86, wc-verified), frontend wrapper +
   key factory + 3 specs (~150–200 LOC), 3 swagger schemas (1,700 bytes), generated models
   (19+ lines), and one endpoint's worth of smoke/parity surface in every future contract
   check.

### 6.2 `GET /api/mushaf/surahs` — DELETE_OR_DEPRECATE_CANDIDATE

Author-verified: zero occurrences of `mushaf/surahs` in frontend `src`, `e2e`, `scripts`,
`testing` outside generated models. The surah-jump picker reads a **build-time static
catalog** instead (`mushaf-surah-juz-catalog.json`, 838 lines;
`mushaf-reader.facade.ts:384`). No tools/scripts consumer (author grep).

1. **Value today:** duplicates data the frontend ships statically; the endpoint is cached
   server-side (`CachedMushafSurahCatalogReader`) but nothing calls it.
2. **Dependents:** generated models (`MushafSurahCatalogResponse` — one of the 7
   never-referenced models); smoke route baseline; the static JSON is the *actual*
   consumer-facing catalog, with its own staleness risk (114 surahs are canonically fixed,
   so drift risk is essentially nil — CONFIRMED stable domain).
3. **Risk:** same out-of-repo-consumer caveat as 6.1; additionally this endpoint is the only
   API source of the surah catalog — deleting it makes the static JSON the *sole* source.
   Given the catalog is immutable canon (114 surahs, `ImportCountsTests.cs:38`), that is a
   low-risk single-sourcing, but it is a real architectural decision, not housekeeping.
4. **Equivalent protection:** the static catalog plus canonical import tests assert the same
   data domain.
5. **Smallest safe step:** deprecation marking + observation; alternatively *keep* the
   endpoint and delete the static JSON (reverse consolidation) — both directions remove the
   duplication; choosing one is a product/offline-behavior decision this audit does not make.
6. **Later verification:** swagger diff + route-baseline update + grep for dangling refs.
7. **Recurring cost removed:** 125 backend LOC across 7 files (wc-verified: controller 16,
   query 3, handler 15, EF reader 51, cached reader 23, response DTO 9, interface 8), 3
   swagger schemas (839 bytes), 2 generated model files (15 lines), cache registration.

### 6.3 The 7 never-referenced generated models — nuanced, not uniformly dead

| Model | Verdict |
|---|---|
| `MushafSurahCatalogResponse` | Dead with its endpoint (§6.2). CONFIRMED |
| `SegmentFeaturesDto` | Dead payload: projected and serialized, never consumed (§4.2). CONFIRMED |
| `WordMorphologyRoot` / `WordMorphologyLemma` / `WordMorphologyStem` | Interfaces never *named*, but their `.text` fields ARE rendered (`word-morphology-summary.component.html:26-63`) via structural typing; only `.buckwalter` members are unused. **Not dead.** CONFIRMED |
| `AccessOwnerReconciliationSummary` | Dead with `lastReconciliation` (§4.2). CONFIRMED |
| `OwnerReconciliationStatusCandidate` | **Not dead** — fields rendered via parent iteration (§3.1). CONFIRMED |

The generated-model layer cannot be used as a deadness signal at all:
`ng-openapi-gen.json` sets `ignoreUnusedModels: false`, so every spec schema is regenerated
regardless of use, and `scripts/prune-generated-api.mjs` deletes all generated *services*,
leaving models-only (CONFIRMED, `endpoint-consumers-frontend.json`
`generated_client_reality`). Model presence is a build artifact, not evidence.

---

## 7. Chatty screens (Q49)

**Highest fan-out screen: access-admin** (`/settings/access`, ownerGuard) — 4 parallel calls
on load (`users`, `permissions`, `audit-events`, `owner-reconciliation/status` via
`Promise.all`, `access-admin.facade.ts:118-121`), plus 2 parallel calls per user selection
(`users/{id}` + `users/{id}/permissions`, `:199`), plus per-query refetches. It is the only
screen with **zero client-side caching** (`endpoint-consumers-frontend.json` caching.uncached).

**Adjudication: correct-by-design, with one optional exception.**

- The 4 slices are genuinely distinct aggregates with different refresh triggers; they run
  in parallel so wall latency is bounded by the slowest; every mutation re-reads the
  affected slice (write-then-reload is correctness-required for admin state).
- Freshness-first with no cache is the right default for a security-administration surface:
  a stale permission grid or user status shown to an Owner is worse than a refetch. The
  population is small (payloads 1.5–30 KB) and the page is owner-only and rarely visited.
- A merged "admin summary" endpoint is **not** recommended — it would couple four refresh
  cadences and contradict §17's warning against merging to reduce endpoint count.
- The one defensible micro-improvement: `GET /api/access/permissions` returns the
  permission *catalogue*, which changes only at deployment (startup catalogue sync).
  A session-lifetime client cache for that single slice would be safe. LIKELY low value;
  optional.

Other screens: mushaf-reader (2 foreground + 2 background prefetch, all cache-guarded) and
word-types explorer (3 calls, tree cached per session) were verified as deliberate and
non-duplicative. No duplicate-call-per-load pattern was found statically anywhere
(`chatty_findings`; in-flight dedupe in `api-response-cache.ts:22`). A runtime network trace
would be needed to certify zero duplicates across effect re-runs — NEEDS_MEASUREMENT.

---

## 8. Lazy-load candidates (Q50)

The app is already substantially lazy — this is a positive finding, verified against the
screens inventory: mushaf study/similar-ayahs/mutashabihat load per-tab on activation only;
words detail views load one call per tab activation; the association picker loads on
typing; access user detail loads on selection.

Exactly **one** new lazy-load candidate emerged: the audit-event documents (§4.1). If the
admin UI ever needs before/after diffs, the shape that preserves auditability *display*
without paying for it on every list page is a per-event detail fetch
(`SPLIT_OR_LAZY_LOAD` as the alternative recorded in the classification file). Today, with
no UI consuming the documents at all, plain response-shrink is the smaller step.

---

## 9. Plausible total reduction (Q52)

| Dimension | Candidate reduction | Confidence |
|---|---|---|
| **Payload — encoder finding (§5.1)** | ~3× on Arabic string content; est. 30–60% total bytes on words lists and mushaf study/pages (Arabic-prose-dominated) | LIKELY (mechanism CONFIRMED, wire bytes NEEDS_MEASUREMENT) |
| **Payload — audit list shrink (5 fields)** | 40–70% of audit item bytes | NEEDS_MEASUREMENT (snapshot size distribution) |
| **Payload — other 45 field removals** | Single-digit % on their endpoints; trivial in absolute terms | LIKELY |
| **Query work** | Removes per-request: 1 raw-SQL jsonb scan (reconciliation status), 1 LEFT JOIN (word analysis), 1 correlated COUNT subquery per user row (access users), 5 jsonb column fetches + 5 parse/serialize ops per audit row (with projection) | CONFIRMED shapes; absolute latency NEEDS_MEASUREMENT (no query timing captured) |
| **Handwritten code** | ~260 backend LOC + ~150–200 frontend/spec LOC from the two dead endpoints; ~40–60 LOC from field removals (DTO records, reader mappings) | CONFIRMED counts (wc-verified) for the chains; LIKELY for field LOC |
| **Generated/support** | ~2.5 KB swagger (0.8% of 309 KB), ~34 generated-model lines, 6 schemas | CONFIRMED |
| **Endpoints** | 85 → 83 | CONFIRMED candidates, pending out-of-repo-consumer observation |

Honest framing per §24: the **code-size** wins here are small (the API layer is not where
this repository's bulk lives), and nothing in this report justifies an "API is bloated"
narrative — 83 of 85 operations are consumed, 68.6% of fields are provably rendered, and
caching/laziness are already well-engineered. The recurring value is concentrated in (a) the
encoder finding, (b) the audit-list shrink, and (c) retiring two dead endpoint chains before
they accrete tests, docs, and contract-check surface.

---

## 10. §24 trace chains — generated code excluded from architecture blame

### Chain A: `GET /api/access/audit-events` (SHRINK_RESPONSE)

| Layer | Artifact | Unused-field share |
|---|---|---|
| EF/SQL | `EfAccessAuditReader.ListAsync` — entity materialization, no projection; 2 `Include`s; 5 jsonb columns fetched (`EfAccessAuditReader.cs:16-63`) | 5 of ~17 columns fetched for nothing |
| Backend DTO | `AccessAuditEventItem`, 17 params (`AccessAuditContracts.cs:16-33`) | 5 params |
| Swagger | `AccessAuditEventItem` + `AccessAuditEventPage` + `...ApiResponse` wrapper schemas | 5 properties |
| Generated model | `access-audit-event-item.ts` (27 lines) + page + wrapper | 5 members (untyped `{}`) |
| Handwritten frontend | `access-admin.api.ts:69` (`listAuditEvents`), `access-audit.store.ts:16` — passes items through untouched | 0 lines touch the 5 fields |
| Template | `access-audit-log.component.html:68-72` renders action/time/actor/target/permission/reason | 5 fields die here, unrendered |

Code-size saving if the 5 fields are dropped: ~30–40 lines across DTO, swagger, generated
model — **trivial**. The honest benefit is runtime (bytes, jsonb fetch, per-row parse), not
LOC. Any change here touches §29 territory (audit) and requires the explicit risk analysis
recorded in the classification file: storage untouched, display-parity preserved (the UI
never showed the documents), DB remains the audit source of truth.

### Chain B: `GET /api/words/word-types/words` (DELETE_OR_DEPRECATE_CANDIDATE)

| Layer | Artifact | Fate on deprecation |
|---|---|---|
| EF/SQL | `EfWordTypesReader.GetRowsAsync` (`EfWordTypesReader.cs:72`) | **Stays** — internally reused by `/table`'s Words view (`:117-124`) |
| Handler | `GetWordTypeRowsQuery` (15 LOC) + `GetWordTypeRowsHandler` (86 LOC) + outcome type + DI line | Removable |
| Controller | `WordTypesController.GetRows` action (~33 LOC, `WordTypesController.cs:40-72`) | Removable |
| Swagger | `WordTypeRowDto`, `WordTypeRowDtoPagedResult`, `...ApiResponse` — exclusively owned, 1,700 bytes (author-computed schema-closure over all 85 ops) | Removable |
| Generated model | `word-type-row-dto.ts` (19 lines) + wrappers | Auto-pruned on regeneration |
| Handwritten frontend | `WordTypesApi.getRows` (~40 LOC incl. params), `WordTypesCacheKeys.rows()` (`word-types-cache.ts:15`), 3 spec-file callers | Removable |
| Template | none — nothing ever rendered it | — |

This chain demonstrates the §24 discipline both ways: the visible "endpoint code" is ~450
LOC across the stack, but the EF reader — the part that looks most substantial — is shared
and must not be counted as a saving.

---

## 11. Mandatory questions answered (43–52)

| Q | Answer |
|---|---|
| **43** | **85 operations** (58 GET / 18 POST / 5 DELETE / 4 PUT) over 78 paths, 229 schemas — CONFIRMED by independent recount (§1). Words 38, Abwab 25, Access 13, Mushaf 7, System 2. |
| **44** | **Zero drift in both directions** — CONFIRMED (§2). Parity is produced by `export-swagger` + `check-api-contract` (regenerate-and-git-diff), but the check is invoked by convention only: no CI exists (no `.github/`), and neither frontend `test:pre-pr` nor any backend lane nor any Skill runs it. Single-point-of-failure cadence — a workflow finding, not an API finding. |
| **45** | Of 671 payload fields: **54 UNUSED_CANDIDATE (8.0%)** + 1 author-found misclassified field (`AccessAuditEventItem.metadata`) = **55 with no known consumer**; 157 UNKNOWN_CONSUMER are a static-analysis attribution limit, not removal candidates (§3). |
| **46** | **10 fields have real DB/query cost**: the 5 audit jsonb documents (column fetch + per-row parse; projection change required to realize the DB half), `lastReconciliation` (deletes an un-indexed raw-SQL jsonb scan — class 4), the 3 word-analysis i3rab-rule fields (deletes a LEFT JOIN — class 3), `permissionCount` (deletes a correlated COUNT subquery — class 3). All CONFIRMED by reading the readers (§4). |
| **47** | The remaining **~45 are serialization/network and DTO-width overhead only** (classes 1/2/5); several keep their column fetch regardless because the value is used server-side for ordering/derivation (§4.2). |
| **48** | True field-level over-fetch: **audit-events** (5 unrendered jsonb documents per item). Systemic over-weight: **`\uXXXX` escaping of all Arabic** (no `AddJsonOptions` anywhere — CONFIRMED; ~3× on Arabic content — LIKELY; no response compression middleware — CONFIRMED) across all hot paths. The words-list 1000-row pages (0.25–0.55 MB est.) are **deliberate design** (frontend requests 1000 explicitly, virtual-scrolls, caches) — the cost is bytes, best addressed by the encoder finding, not by field or design changes. The abwab-tree soft-deleted-doors hypothesis was **refuted**: archived doors are rendered product behavior (§5.3). |
| **49** | **access-admin** is the chattiest screen (4 parallel on load + 2 per selection, zero client cache) and is adjudicated **correct-by-design** for admin freshness; no merge recommended; the only optional improvement is session-caching the deployment-static permission catalogue (§7). No duplicate-per-load calls exist anywhere (static evidence; runtime certification NEEDS_MEASUREMENT). |
| **50** | The app already lazy-loads study tabs, detail views, and pickers correctly. The **single new lazy-load candidate** is the audit-event document payload via a per-event detail fetch — recorded as the alternative shape to the audit-list shrink (§8). |
| **51** | **2 endpoints with no in-repo consumer**: `GET /api/mushaf/surahs` (frontend uses a static catalog) and `GET /api/words/word-types/words` (superseded by `/table`; wrapper has only spec callers). Both classified DELETE_OR_DEPRECATE_CANDIDATE with full §4 seven-answer analyses (§6.1–6.2); both carry the caveat that anonymous public GETs may have out-of-repo consumers — smallest safe step is deprecation + observation, never immediate deletion. Of the 7 never-referenced generated models, only 3 are genuinely dead payloads; 4 are consumed structurally (§6.3). |
| **52** | Payload: 30–60% on Arabic-heavy hot paths from the encoder finding (LIKELY), 40–70% of audit item bytes (NEEDS_MEASUREMENT), single-digit % elsewhere. Query work: 1 raw-SQL scan + 1 LEFT JOIN + 1 correlated subquery + 5 jsonb fetches/parses per audit row removable (shapes CONFIRMED, latency NEEDS_MEASUREMENT). Code: ~260 backend + ~150–200 frontend/spec LOC (dead chains, wc-verified), ~2.5 KB swagger, 85→83 operations. The API surface is overall **healthy**: 83/85 operations consumed, 68.6% of fields provably rendered (§9). |

---

## 12. Measurement gaps

1. **Wire payload bytes** — all payload numbers are static estimates on escaped-output
   assumptions; no runtime capture (Playwright network trace / curl against a local run) was
   taken in this read-only pass. Affects §5.1, §5.2, §5.4, Q52 bands.
2. **Whether Railway's edge applies compression** — out of repo; determines how much of the
   escaping inflation reaches real users.
3. **Query latency deltas** — the removable query shapes are proven from code; their
   absolute cost (small admin tables vs 83k-word corpus tables) was not measured.
4. **Out-of-repo API consumers** — all read endpoints are anonymous; access logs or
   deployment telemetry would be needed before any deprecation graduates to deletion.
5. **Audit snapshot/state size distribution** — stored jsonb document sizes drive the
   audit-shrink payoff; not asserted anywhere in code or tests.
6. **UNKNOWN_CONSUMER resolution** — 157 generic-named fields need a type-aware
   find-references pass to be attributed; until then they are untouchable per §4.
7. **Morphology-table row counts** (roots/lemmas/stems/unique words) — asserted nowhere in
   code or tests (backend inventory note); list-payload high-ends depend on them.
8. **Effect-re-run duplicate calls** — static analysis found none; only a runtime trace can
   certify zero.
