# 02 — Test Suite Rationalization Audit (Audit A)

Audited branch: `dev` — commit `72792ba9` — audit date 2026-08-08.
Evidence base: `data/test-inventory-backend.json`, `data/test-inventory-frontend.json`,
`data/runtime-measurements.json`, `data/loc-inventory.json`, `data/workflow-gate-inventory.json`,
plus direct spot-verification reads of the repository files named below. This is an audit:
it proposes and classifies; it instructs nothing and deletes nothing.

> **Sanctioned exception note.** `TESTING_STRATEGY.md:17` forbids carrying test counts and
> durations in prose documents. This report is an audit artifact and is the sanctioned
> exception; none of its numbers may be copied back into steady-state docs.

---

## 1. Headline finding: runtime is healthy — LOC and authoring cost are the problem

**CONFIRMED.** The suites are fast and green:

- Backend: all 8 measured lanes complete in **357.7 s wall total** (≈6 min run strictly
  sequentially), 0 failures, 0 unexpected skips (`data/runtime-measurements.json:14-291`).
  The three partition gates together (tier-b + pipeline + smoke) execute **2,211 tests in
  ~202 s** of test wall time.
- Frontend: `test:full` runs **2,964 tests in 232.2 s** under the deliberate 2-fork Vitest
  cap (`data/runtime-measurements.json:293-330`).

**CONFIRMED.** The recurring cost is authoring and maintenance mass, not machine time:

| Body | LOC | Evidence |
|---|---:|---|
| Backend test `.cs` (347 files) | 56,155 physical lines (55,808 by newline count — delta is exactly 1 line/file, a counting-method artifact) | `data/test-inventory-backend.json:10-24`, `data/loc-inventory.json:76` |
| Backend test support (seed `.sql`, `test-gates.tsv`, `test-resources.tsv`, csproj) | 1,969 | `data/loc-inventory.json:76` |
| Frontend `*.spec.ts` (223 files) | 54,145 | `data/test-inventory-frontend.json:9-17` |
| Playwright E2E (`e2e/`, 22 tracked files) | 1,968 | `data/loc-inventory.json:274` |
| **Total test estate** | **≈112.3k–114.2k** (low = test code only: 56,155 + 54,145 + 1,968 = 112,268; high = incl. the 1,969-LOC support-file row = 114,237) | |
| Handwritten product (Backend 56,934 + Frontend 56,221) | **113,155** | `data/loc-inventory.json:6,90` |

The test estate is **≈1:1 with the handwritten product** and weighs ≈1.17M tokens of agent
context (606k backend + 540k frontend + 20k e2e — `data/test-inventory-backend.json:1426`,
`data/test-inventory-frontend.json:9-17`). Every recommendation below is therefore weighted
toward **FEWER/STRONGER** (LOC and duplication), not FASTER (runtime), per the brief's
"fewer, stronger, faster, risk-aligned" target with honesty that "faster" is already largely
achieved.

---

## 2. Inventory — Backend

One test project: `Backend/tests/QuranDashboard.Tests` (`Backend/scripts/test-backend:8`).
347 `.cs` files; 268 test classes; 80 files are fixtures/gates/seeds/TestSupport with no
`[Fact]`/`[Theory]` (`data/test-inventory-backend.json:10-24,1418`). Static lower-bound case
count: **1,860** (1,326 facts incl. 30 custom source-gated facts + 482 InlineData rows + 52
MemberData refs counted once; MemberData expansion is not statically countable —
`data/test-inventory-backend.json:23`). Catalog drift: **NONE** — 268/268 parity between
disk and `test-gates.tsv` in both directions, self-enforced by
`TestGateCatalogTests` (`data/test-inventory-backend.json:1111-1126`; spot-verified: the tsv
has exactly 268 data rows).

### 2.1 By feature

| Feature | Classes | Facts | Theories | InlineData | LOC | Gate |
|---|---:|---:|---:|---:|---:|---|
| Access | 28 | 154 | 19 | 21 | 6,305 | TierB |
| WordsMorphologyExplorers | 16 | 154 | 45 | 116 | 4,501 | TierB |
| WordsWordTypes | 20 | 111 | 29 | 87 | 4,187 | TierB |
| WordsMorphology | 17 | 142 | 2 | 12 | 4,167 | Pipeline |
| Smoke | 16 | 113 | 10 | 4 | 3,065 | Smoke |
| Words | 14 | 69 | 41 | 170 | 2,808 | TierB |
| Translations | 12 | 62 | 0 | 0 | 2,054 | Pipeline |
| Abwab | 8 | 63 | 1 | 2 | 1,865 | TierB |
| WordsMorphologyEnriched | 7 | 52 | 1 | 0 | 1,740 | Pipeline |
| Mutashabihat | 9 | 42 | 1 | 0 | 1,636 | Pipeline |
| Tafsirs | 16 | 45 | 1 | 0 | 1,635 | Pipeline |
| Navigation | 12 | 48 | 1 | 6 | 1,587 | Pipeline |
| FullI3rab | 11 | 40 | 1 | 2 | 1,565 | Pipeline |
| WordsDisplay | 12 | 23 | 1 | 3 | 1,524 | Pipeline |
| MushafReader | 24 | 50 | 5 | 19 | 1,429 | TierB |
| WordsRoots | 8 | 53 | 9 | 16 | 1,291 | TierB |
| WordsSimpleI3rab | 14 | 21 | 8 | 7 | 1,104 | Pipeline |
| ApiBehavior | 7 | 45 | 3 | 10 | 1,085 | TierB |
| FoundationImport | 12 | 14 | 3 | 7 | 865 | Pipeline |
| RateLimiting | 3 | 20 | 0 | 0 | 394 | TierB |
| Middleware | 1 | 2 | 0 | 0 | 148 | TierB |
| Health | 1 | 3 | 0 | 0 | 81 | TierB |
| **Feature-attributed total** | **268** | **1,326** | **181** | **482** | **45,036** | |

(`data/test-inventory-backend.json:25-377`.) The remaining ≈11.1k LOC is TestSupport,
fixtures, gates and seed helpers not attributed to a feature.

By kind: Database 187, Fast 69, Canonical 10, Migration 1, Process 1. By gate (a strict
partition of all 268 classes): TierB 130, Pipeline 122, Smoke 16
(`data/test-inventory-backend.json:378-389`).

Category cuts (definitions and confidence in the data file,
`data/test-inventory-backend.json:871-1109`): db-backed 198 classes / 36,006 LOC;
authorization/security 33 classes / 7,096 LOC (LIKELY, name-heuristic); API read 65 classes /
11,529 LOC (only 16 files HTTP-GET-only — reads mostly resolve query handlers via DI);
API mutation 14 classes / 5,987 LOC; contract 10 classes / 1,651 LOC; importer/pipeline 122
classes / 17,877 LOC; canonical Quran-data 10 classes / 970 LOC; source/hash/manifest 22
classes / 2,253 LOC; process/CLI 1 class / 257 LOC; fast pure-unit 69 classes / 8,773 LOC.

Testcontainers model (**CONFIRMED**, `data/test-inventory-backend.json:885-896`): ONE shared
`postgres:16-alpine` per test process with template-cloned databases + ONE exclusive
`postgres:18-alpine` for `SmokeDataReadTests`; no per-class containers; cross-process OS file
lock; DB parallelism 4. This is already an efficient setup design — it is why the runtime is
healthy despite 198 db-backed classes.

### 2.2 Lanes and measured runtime

Single run, solo machine, sequential lanes; wall times include ~10–15 s fixed
dotnet-test/VSTest startup + container provisioning per lane, so lane walls are not additive
test cost (`data/runtime-measurements.json:430-440`).

| Lane | Classes | Tests passed | Wall (s) | Notes |
|---|---:|---:|---:|---|
| fast | 69 | 559 | 7.5 | pure unit, no containers |
| access | 28 | 249 | 61.3 | all Access classes |
| access-db | 19 | 193 | 43.0 | subset of access |
| migration | 1 | 9 | 21.9 | `AccessMigrationPathTests` |
| process | 1 | 10 | 21.4 | AccessAdmin CLI as real process |
| smoke | 15 | 256 | 72.6 | excludes canonical `SmokeDataReadTests` |
| tier-b | 130 | 1,372 | 65.9 | superset incl. Access, explorers |
| pipeline | 113 | 583 | 64.1 | excludes 9 canonical classes |
| **Measured total** | | **3,231 lane-executions** | **357.7** | lanes overlap — see below |
| pre-pr | 268 | — | est. ≈307 (5–7 min) | NOT run (re-executes measured shards) — `data/runtime-measurements.json:278-284` |
| canonical-data | 10 | — | NEEDS_MEASUREMENT | not in plan; resources staged locally — `data/runtime-measurements.json:286-290` |

**Counting honesty (CONFIRMED):** 3,231 is the sum of lane executions and double-counts —
access ⊂ tier-b, access-db ⊂ access, and the fast lane re-runs Fast-kind classes that live
inside TierB/Pipeline gates. The distinct full-suite runtime count is the partition
tier-b + pipeline + smoke = **2,211 tests / 258 classes**, plus the 10 unmeasured Canonical
classes (~30–35 est. cases). Runtime counts exceed the static 1,860 lower bound because
Theory/MemberData rows expand at run time.

Slowest individual tests (all still single-digit seconds,
`data/runtime-measurements.json:28-273`): `PostgreSqlTestProcessContractTests` 9.7/6.0/4.4 s
(deliberate lock/lease waiting), `TranslationSourceSafetyTests` 5.9/5.2/4.6 s,
`SmokeAuthPipelineTests.OwnerSub_IsBootstrappedActiveOwner` 5.8 s,
`LogtoSubjectRelinkEndpointTests` 4.5 s, `AccessAdminCommandTests` members 2.5–4.8 s (this
class appears among the slowest in three different lanes because access, process and tier-b
all select it).

---

## 3. Inventory — Frontend

223 spec files / 54,145 LOC / 2,459 static case sites (2,964 at runtime — `it.each`/loops
expand); 548 describes; 150 files use TestBed (`data/test-inventory-frontend.json:9-17`).
Runner: Vitest via `@angular/build:unit-test` + jsdom; lanes are `angular.json` test
configurations; the 7 primary lanes partition all 223 specs exactly (0 orphans, 0 double
assignments), self-checked by `testing/verify-test-gates.mjs` and independently re-verified
by the inventory agent (`data/test-inventory-frontend.json:124-140` area, notes at `:4907`).

### 3.1 By area

| Area | Spec files | LOC | Cases (static) | Share of spec LOC |
|---|---:|---:|---:|---:|
| features/words | 92 | 24,207 | 1,120 | 44.7% |
| features/abwab | 32 | 11,675 | 628 | 21.6% |
| features/mushaf | 42 | 8,751 | 297 | 16.2% |
| features/access-admin | 14 | 3,819 | 142 | 7.1% |
| core | 20 | 3,141 | 135 | 5.8% |
| shared | 17 | 1,847 | 114 | 3.4% |
| app-root / auth / dashboard / environments | 6 | 705 | 23 | 1.3% |

### 3.2 Lanes and measured runtime

| Gate | Files | Tests | Wall (s) | Trigger per `TESTING_STRATEGY.md:206-228` |
|---|---:|---:|---:|---|
| typecheck | — | — | 14.6 | compilation-affecting work |
| build:verify | — | — | 18.3 | bundle-affecting work (3 warning-level budget overruns observed) |
| test:fast | 61 | 810 | 72.5 | pure state/mapping/codec work |
| test:feature:words | 92 | 1,379 | 114.3 | words feature work |
| test:full | 223 | 2,964 | 232.2 | one broad gate per frontend PR |
| test:pre-pr | composite | — | not measured (≈ sum of parts + 2 check scripts) | pre-PR when frontend changed |
| authorization / composition / shared | 11 / 105 / 41 | 54 / 1,404 / 261 static | not measured | cross-cuts (`data/test-inventory-frontend.json:111-123`) |

Slowest specs (`data/runtime-measurements.json:331-392`): `abwab-page.component.spec` 5.86 s,
`word-types-explorer-page` 4.96 s, `stems-explorer-page` 2.58 s, `lemmas-explorer-page`
2.48 s, `access-admin-page` 2.22 s, `roots-explorer-page` 2.15 s. The slow list is exactly
the markup-heavy giant-page-spec list — slow and heavy correlate.

By category (`data/test-inventory-frontend.json` `by_category`): component-rendering
dominates — 103 files / 31,797 LOC (59% of spec LOC) / 1,378 cases; facade-store-state 38 /
11,840; route-guard-url 18 / 4,335; pipe-util 30 / 1,823; api-boundary 10 / 2,004;
authorization 9 / 1,111.

### 3.3 E2E

17 Playwright `*.e2e.ts` files / 1,601 LOC (+360 fixtures) / 61 static test calls (runtime
higher — width/theme loops); chromium only; boots both dev servers; **opt-in and never a
required gate** (`Frontend/quran-dashboard-ui/CLAUDE.md` states this explicitly; config
evidence `data/test-inventory-frontend.json:4683-4835`). Runtime and flakiness:
**NEEDS_MEASUREMENT** — requires live DB + dual servers, out of read-only scope
(`data/runtime-measurements.json:402-405`). Journeys covered: abwab archive/restore,
reorder/move/bulk ops, relations lifecycle, URL-state + a11y (roving tabindex, RTL arrows),
permissions posture, mushaf reader/ayah study/word analysis, words explorers, shell nav,
visual width/row budgets.

---

## 4. Highest-value protection map (KEEP — named classes)

These are the suites that buy the protection §29 of the brief forbids weakening casually.
All class names spot-verified to exist on disk.

| Invariant area | Backend classes (evidence) | Frontend specs |
|---|---|---|
| Authentication / authorization pipeline | `AuthorizationPipelineTests`, `AuthorizationPolicyRegistrationTests`, `AuthorizationRequirementHandlerTests`, `AuthorizationRejectionResponseTests`, `AuthorizationStateResolverTests`, `SmokeAuthPipelineTests`, `SmokeRouteAccessContractTests`, `UnsafeEndpointMetadataValidatorTests` (`data/test-inventory-backend.json:897-932`) | `core/auth/current-user.store.spec.ts` (376 LOC/14), `owner.guard.spec.ts`, `auth-bearer-token.spec.ts`, `write-auth-failure.coordinator.spec.ts` |
| Owner rules / bootstrap | `OwnerBootstrapOptionsTests`, `OwnerReconciliationServiceTests` (530 LOC), `SmokeAuthPipelineTests.OwnerSub_IsBootstrappedActiveOwner` (measured 5.8 s) | `owner.guard.spec.ts` |
| Direct permissions / catalogue | `UserPermissionPersistenceTests`, `AbwabPermissionCatalogueTests`, `PermissionCatalogueSynchronizerTests`, `PermissionCatalogueStartupSyncTests` | `abwab-permissions.controller.spec.ts`, `access-admin-permissions.spec.ts`, plus the `check:permission-catalogue` parity script in `test:pre-pr` |
| Account status / inactive tokens | `SmokePublicReadRegressionTests` (inactive/pending personas keep public reads), `AuthorizationStateResolverTests` | `current-user.store.spec.ts` |
| Audit | `AccessAuditEventPersistenceTests`, plus `check:audit-action-types` parity script | — |
| Optimistic concurrency / writes | `SmokeAbwabWriteTests` (1,296 LOC, 72 facts; 38 stale-version/conflict/concurrency assertions — spot-verified by grep), `SmokeAbwabWriteAuthorizationTests`, `AbwabDoorWriteBehaviorTests` (874 LOC) | abwab write-path specs in `abwab-page`/modal suites |
| Migration safety | `AccessMigrationPathTests` (487 LOC, 9 cases, staged-upgrade path incl. collision refusal without mutation) | — |
| Quran data integrity / source provenance | the 10 Canonical classes (source-gated custom Facts — see §7), the 22 source/hash/manifest classes (`data/test-inventory-backend.json:1054-1086`), per-pipeline rollback tests (e.g. `TranslationRollbackTests`), `TranslationSourceSafetyTests` etc. | mushaf render/font invariants inside `features/mushaf` specs; e2e `mushaf-reader.e2e.ts` (Uthmani/Amiri rendering) |
| URL state / guards / restoration | — | the url-sync family: `mushaf-reader.facade.url-sync.spec.ts` (1,041 LOC/29), `word-types-url-sync` (725/61), `stems/lemmas/roots/unique-words-url-sync`, `abwab-url-sync.spec.ts` (213/25) |
| Test-infrastructure integrity | `TestGateCatalogTests` (drift-proof catalog), `PostgreSqlTestProcessContractTests`, `AccessCollectionResetContractTests`, `SmokeCollectionResetContractTests` | `npm run test:gates` (`verify-test-gates.mjs`) |

**Adjudication: KEEP, unchanged, all of the above.** These are the risk-aligned core; their
combined runtime cost is minutes and their protection is exactly the brief-§29 list. The
authorization/security cut is 7,096 LOC — 6.3% of the test estate buying the highest-risk
coverage. Nothing here is a simplification candidate except where a class also appears in a
redundancy cluster below (and there only the *harness*, never the invariant).

---

## 5. Lowest-value / redundancy adjudication — Backend

The inventory lists 13 redundancy clusters (`data/test-inventory-backend.json:1127-1321`).
I re-read the largest ones rather than trusting the scan.

### 5.1 Per-pipeline RefusalForce ×7 — 795 LOC (spot-verified: wc sums to exactly 795)

Read in full: `TafsirRefusalForceTests.cs` (63 LOC, 2 facts) and
`NavigationRefusalForceTests.cs` (108 LOC, 3 facts). **Verdict: the cluster is a shared
invariant shape, NOT copy-paste.** Tafsir asserts refusal exit code + message + snapshot
immutability; Navigation additionally covers a pipeline-specific rerun-guard edge (orphaned
ayah-navigation columns trigger refusal, `NavigationRefusalForceTests.cs:50-78`) and
JSON-report check contents. The invariant they protect is real and documented product
behavior: "Re-imports refuse to overwrite committed data unless `--force`"
(`Backend/tools/QuranDashboard.DataImporter/README.md:41`). **Classification: MERGE
(shared parameterized refusal/force harness; per-pipeline instantiation retained, including
pipeline-specific edge cases as extra facts). Not DELETE — every pipeline must keep an
executing instance of the refusal invariant because each importer wires its own tables and
exit paths.** Tag: LIKELY. Expected saving is modest (~300–450 LOC) because the classes are
already thin; the real win is that pipeline #11 costs one data row, not one new class.

### 5.2 Explorer Logging ×4 — 1,583 LOC (spot-verified: wc sums to exactly 1,583)

Read: `RootsLoggingTests.cs`, `WordTypesLoggingTests.cs`. Each asserts, per query handler
(8–9 handlers per explorer): (a) success log has an **exactly enumerated** field-name list,
(b) warnings for controlled refusals with exact field lists, (c) **no raw search text /
Quran text / sensitive payload ever reaches the log** (`RootsLoggingTests.cs:39-51`,
`WordTypesLoggingTests.cs:59-77`). Verdict: (c) is genuine safety (log-content provenance
for Quran text — brief §29 territory) and per-handler pinning of (a) is the low-value bulk:
exact `FieldNames().Should().BeEquivalentTo([...])` per handler means every telemetry field
addition breaks 1–4 tests without catching a real bug. **Classification: REWRITE — keep one
shared "no sensitive payload in any handler log" property (parameterized over the handler
catalog) plus per-handler smoke of feature/operation identity; drop exhaustive field-name
enumeration.** Tag: LIKELY. Candidate saving ~800–1,200 LOC across the 4 classes. Risk: the
sensitive-payload invariant must survive verbatim; equivalent protection does not exist
anywhere else (grep found no other log-content guard).

### 5.3 The 10 importer fixtures — 4,559 LOC (spot-verified by wc; inventory said ~4,600)

`MorphologyImportTestFixture` 929, `MutashabihatImportTestFixture` 751,
`NavigationImportTestFixture` 681, `TafsirImportTestFixture` 604,
`I3rabGenerationTestFixture` 549, `TranslationImportTestFixture` 369,
`FullI3rabImportTestFixture` 232, `WordsDisplayTestFixture` 176,
`DisplayWordsRealImportFixture` 175, `ImportTestFixture` 93
(`data/test-inventory-backend.json:583-869,1419`). Each wires its own
seed + synthetic-package-writer + run + snapshot + reset harness around the same lease/reset
skeleton; the 5 explorer fixtures separately share an identical lease shape (CONFIRMED at
`RootsExplorerTestFixture.cs:24-25` and the parallel lines in the other four —
`data/test-inventory-backend.json:1420`). **This is the single heaviest duplicated-setup
pattern in the backend.** **Classification: MERGE (a shared importer-fixture skeleton:
lease/reset/report-dir/snapshot plumbing extracted; per-pipeline seed catalogs and table
snapshots remain per-fixture).** Tag: NEEDS_MEASUREMENT for the saving (design-dependent;
plausibly 1,500–2,500 LOC) — and explicitly high-blast-radius: this is DB-safety
infrastructure; a pilot on one small fixture pair would have to precede any broad move.

### 5.4 Other backend clusters (adjudicated from the inventory + protection map)

| Cluster | Count / LOC | Classification | Reasoning |
|---|---|---|---|
| per-pipeline ValidationFailure | 9 classes (Morphology alone 704 LOC) | MERGE (same harness family as §5.1) | invariant (abort+rollback+report violations) must stay per pipeline; shape shareable. LIKELY |
| per-pipeline ReportShape (+Json/Md) | 11 listed (contains duplicates in the data file's list — real distinct count is 7; noted as a data-file blemish) | MERGE | report-artifact shape is one contract with per-pipeline fields. LIKELY |
| per-pipeline ManifestReader ×5, SchemaShape ×4 | ~9 classes | MERGE | same. LIKELY |
| per-pipeline source-safety ×6 | 2,253 LOC family | **KEEP semantics, MERGE harness only** | brief explicitly protects source-safety tests; the slowest pipeline tests are here (Translation 5.9 s) but runtime is a non-issue. LIKELY |
| explorer CountRangeFilter ×4, CacheRead ×4, RedundancyRead ×5, OrderingContract ×2 | ~15 classes | MERGE into shared read-model property harness | same property re-instantiated per explorer; see §8 (GET consolidation). LIKELY |
| CollectionResetContract ×2 | 2 | KEEP | same contract shape against two different fixtures — subjects differ, protection real. CONFIRMED |
| Access schema at three layers (`AccessSchemaModelTests` / `AccessSchemaDriftTests` / `PermissionCatalogueStartupSyncTests`) | 3 | NEEDS_MEASUREMENT | deliberately different subjects (EF model vs live drift vs startup sync); calling it redundant requires case-level reading not yet done (`data/test-inventory-backend.json:1270-1285`). Do not touch under §29 (authorization). |

Largest backend test files (`data/test-inventory-backend.json:1322-1413`) are dominated by
high-protection suites (`SmokeAbwabWriteTests` 1,296, `AccessAdministrationEndpointTests`
1,026, `AbwabDoorWriteBehaviorTests` 874) — large but **KEEP**: size here tracks genuine
mutation-surface coverage, not waste.

---

## 6. Lowest-value / redundancy adjudication — Frontend

72 repeated-variant clusters; **63 of them, totaling 244 case-sites, live in
`features/words`** (`data/test-inventory-frontend.json:567+`; re-counted from the data:
65 feature-scope clusters = 250 case-sites, 63/244 in words). Only 1 cluster is flagged
data-driven today.

### 6.1 The five words explorer page suites — 5,537 LOC combined

`word-types-explorer-page.component.spec.ts` 1,644 / `roots` 1,246 / `stems` 1,169 /
`lemmas` 1,134 / `unique-words-page` 344 (all spot-verified by wc). I measured
title-level identity directly:

- **stems vs lemmas: 38 of ~45 normalized titles identical (≈84%)** — a near-clone pair.
- stems vs roots: only 12 shared — roots carries page-specific behavior (surah sub-tabs,
  five-tab panel, count-click mapping).

**Verdict: "5 near-identical suites" is an overstatement at whole-file granularity; the
truth is a large repeated behavioral core** (sorting contract "Feature 030 N8" — 6
identical cases per page; URL contract; pagination slot; state boxes; count chips; summary
retry; deep-link restore) **plus real page-specific tails.** The repeated core is exactly
what the 63 cluster records capture: the same `it()` re-implemented in 4–5 sibling files
(e.g. "keeps the pagination slot rendered while loading and once loaded" ×5 across the five
`*-words-list` components; "intercepts an unmodified click in-app…" ×7; the 6-case sorting
describe ×4). **Classification: MERGE — a shared explorer-page behavioral harness
(parameterized by page fixture: routes, facade, columns, sort tokens), with per-page files
keeping only their genuinely page-specific describes.** Tag: LIKELY. This is also the
authoring-cost lever: explorer #6 currently costs ~1,100–1,600 LOC of spec; with a harness
it costs a config + its deltas.

### 6.2 `abwab-page.component.spec.ts` — 2,148 LOC, 104 cases, 370 markup assertions

Spot-verified: 2,148 lines; ~242 `querySelector` occurrences (inventory says 235 within its
370 markup-assertion count — same order, different counting rule); 18 internal-surface
casts; also the slowest frontend spec (5.86 s). Read in part. Honest verdict: **this is a
thoughtful suite, not sloppy** — the seed deliberately differentiates `orderValue` vs
`globalOrderValue` so wrong-field reads fail (`abwab-page.component.spec.ts:50-52`), and it
covers archived-door unreachability, focus-never-drops-to-body, bulk mode, overlay
page-scoping, deep-link modal state. Two structural problems: (a) it is a single-file
accretion of ~20 feature-slices whose describe titles pin **dead planning-artifact IDs**
("T502", "T508/M31", "audit item 10/11", "slice J" — the artifacts these reference are
deleted at merge per the workspace lifecycle rule), and (b) 370 markup/selector assertions
couple it to template structure, which is why it breaks on template refactors.
**Classification: REWRITE (split by behavior area; rename describes to behavior language;
prefer role/testid queries over structural selectors; keep every behavioral invariant —
especially focus management and archived-door reachability, which are a11y/RTL protections
under brief §29).** Tag: LIKELY. LOC saving is secondary (~300–600); the value is
maintainability of the highest-churn page.

### 6.3 Markup-selector-heavy and implementation-pinned specs

20 files with ≥57 markup assertions each (`data/test-inventory-frontend.json:285+`), led by
abwab-page (370), word-types-explorer (223), abwab-move-picker (157), abwab-tree (153).
32 files drive protected/private component methods via `component['onX'](...)` or
`as unknown as {onX}` casts (spot-verified in `stems-explorer-page` lines 533–871:
`component['onRowSelected']`, `component['onWordViewChange']`, etc.). **Classification:
REWRITE opportunistically (when a file is next touched), not as a campaign** — the pinning
is LIKELY-confidence pattern-matching, each instance needs judgment, and a mass rewrite of
31k LOC of component specs would burn more than it saves. The a11y-relevant markup
assertions (472 aria/role occurrences across 71 files,
`data/test-inventory-frontend.json:1995`) are protection, not waste — RTL/a11y is a §29
area.

### 6.4 Words api-boundary specs

`roots/stems/lemmas/unique-words/word-types.api.spec.ts` (190–306 LOC each, ~1,110 LOC
total). Spot-verified roots vs stems: 7 of 10/11 normalized titles identical.
**Classification: MERGE (same harness treatment as §6.1, smaller stakes).** Tag: LIKELY.

### 6.5 What is NOT low-value on the frontend

The url-sync family (route-guard-url, 4,335 LOC), authorization specs, facade/store specs
(11,840 LOC) and the api-boundary error-mapping cases are the frontend's risk-aligned core —
**KEEP**. The `test:gates` structural self-check is cheap and prevents silent lane rot —
**KEEP**.

---

## 7. Importer / canonical separation (brief §9 "Importer tests")

**CONFIRMED separation already exists in the runner and catalog:**

- **Operational Quran-safety gates (KEEP, run at canonical triggers):** the 10
  `Kind==Canonical` classes (970 LOC, ~30 cases) are gated by custom source-gated attributes
  — `FoundationImportSourceFact` (7), `CanonicalImportSourceFact` (7), `EnrichedArtifactFact`
  (15), `SmokeDumpFact` (1) + Theory variants — which **skip with a named reason when the
  staged real source package is absent** (spot-verified:
  `Backend/tests/QuranDashboard.Tests/Quran/Import/FoundationImportSourceGate.cs` builds the
  `resources/import-sources/quran-foundation` path and sets `Skip` when missing). The
  canonical lane preflights sources/artifact/dump and prints skip-accounting
  (`Backend/scripts/test-backend:476-524`). A plain `[Fact]` scan misses all 10 classes —
  the inventory corrected for this (`data/test-inventory-backend.json:1125`).
- **Synthetic pipeline suites (Gate==Pipeline, 122 classes / 17,877 LOC):** these are the
  executable spec of the DataImporter's 10 verbs. **None is historical/obsolete:**
  `Backend/tools/QuranDashboard.DataImporter/README.md:3` declares the CLI "the operational
  entry point for every Quran import / generate / rebuild" and lists all 10 verbs as live;
  no pipeline is marked deprecated anywhere I could find. Whether any importer will *ever
  run again* (e.g. FoundationImport after the foundation is final) is a product decision the
  repo does not record — **UNKNOWN**; until decided, the suites are the only executable
  protection for tools that can rewrite Quran data, and the refusal/rollback/source-safety
  invariants map 1:1 to documented safety behavior. **KEEP the invariants; MERGE the
  harnesses (§5.1–5.4).**
- **Deterministic seed/catalog checks:** `TestGateCatalogTests`, `test-resources.tsv`
  cross-checks — KEEP (they are what makes the lane system trustworthy).
- **Repeated assertions of the same invariant:** the per-pipeline clusters of §5.4 — MERGE.

Per the brief: no source-safety test is proposed for deletion, expensive or not. The
slowest pipeline tests are `TranslationSourceSafetyTests` (5.9 s max) — trivial cost.

---

## 8. GET-endpoint test consolidation (brief §9 "GET endpoint tests")

The API-read cut is 65 classes / 11,529 LOC; only 16 files exercise HTTP GET — the rest
resolve query handlers via DI (`data/test-inventory-backend.json:951-963`). I read
`LemmasListReadTests.cs` (579 LOC, 22 facts + 5 theories) as the representative:

- It already matches the brief's minimum-coverage list well: success, paging (incl.
  out-of-range and overflow), search + empty search, not-found, cache/no-redundant-SQL,
  projection semantics (compound-segment counting).
- Its permutations are **property-shaped theories, not copy-paste**: the 15-token sort
  theory asserts one real property ("sorting is ORDER BY only — must never change scope or
  total", `LemmasListReadTests.cs:448-481`), and the alias theory asserts
  legacy-token/alias identity. Cheap in runtime (immutable seed, in-memory).

**Verdict: the waste is not combinatorial permutation within a class — it is the same
property re-implemented per explorer feature** (Roots, Stems, Lemmas, WordTypes,
UniqueWords each own a sort-preserves-scope theory, a paging family, a cache family — the
CountRangeFilter/CacheRead/RedundancyRead/OrderingContract clusters of §5.4).
**Classification: MERGE into a shared read-model property harness parameterized by
(handler, token set, seed counts); per-feature token lists stay as data. Auth-boundary GET
coverage stays where it is (Smoke route-access contract), per the brief's minimum list.**
Tag: LIKELY. Flagged combinatorial excess: none found worth naming beyond this cross-feature
repetition — I will not manufacture one.

---

## 9. E2E ↔ unit overlap (6 candidates)

All six overlap candidates (`data/test-inventory-frontend.json:4836+`) were reviewed;
one pair spot-verified in source (`abwab-url-and-a11y.e2e.ts:57` "invalid query values fail
closed to their defaults" vs `abwab-url-sync.spec.ts`). **Adjudication for all six: KEEP
both layers — no MOVE_TO_E2E, no MOVE_FROM_E2E, no deletion.** Reasoning: the Vitest layer
is the **required** gate; Playwright is **opt-in and explicitly never a required gate**
(`Frontend/quran-dashboard-ui/CLAUDE.md`). Deleting a unit spec in favor of an e2e twin
would move enforcement from a required lane to an optional one — a protection downgrade the
brief forbids for URL-state/a11y/permissions areas. Conversely the e2e copies add browser
truth (real history/Back-Forward, real focus, real RTL arrows) that jsdom cannot give, at
zero required-gate cost. The overlap is 6 behaviors out of 61+ e2e cases — not a material
duplication burden. Tag: CONFIRMED for the policy asymmetry; LIKELY for the per-pair
equivalence claims.

---

## 10. Expensive setup patterns

| Pattern | Cost | Adjudication |
|---|---|---|
| 10 importer fixtures, 4,559 LOC, each hand-wiring seed/run/snapshot/reset around the same lease skeleton | authoring + maintenance, not runtime | MERGE skeleton (§5.3), NEEDS_MEASUREMENT saving |
| 5 explorer fixtures with identical lease shape (~115 LOC each) | small | MERGE opportunistically with §5.3's skeleton; LIKELY |
| Shared postgres:16 container + template-cloned DBs + slot semaphore + cross-process lock | this is the *good* pattern — it is why 198 db-backed classes cost ~6 min | KEEP (CONFIRMED) |
| Angular TestBed in 150 spec files / 1,000 refs; composition lane = 105 files | jsdom bootstrap dominates frontend wall (wall 232 s vs vitest-reported 207 s) | KEEP; the 2-fork cap is a deliberate stability choice — do not raise it as a "fix" without measurement |
| Per-lane fixed ~10–15 s dotnet-test/VSTest startup ×8 lanes | only hurts when lanes are run back-to-back unnecessarily | already governed by the trigger matrix; no change proposed here (gate-cadence findings belong to report 10) |

---

## 11. Classification summary

Brief taxonomy, applied to meaningful groups. Every DELETE_CANDIDATE names replacement
coverage; there are deliberately few, because the honest finding is that this estate's
problem is duplication of *implementation*, not existence of *tests*.

| Group | Classification | Replacement coverage (where required) |
|---|---|---|
| Authorization/security/Owner/audit/concurrency/migration suites (§4) | KEEP | — |
| Canonical classes, source/hash/manifest classes, rollback tests | KEEP | — |
| Contract + catalog-drift tests (backend TSV, frontend test:gates) | KEEP | — |
| url-sync family, facade/store, api-boundary error mapping | KEEP | — |
| E2E suite (17 files) incl. the 6 unit-overlap pairs | KEEP (opt-in status unchanged) | — |
| Per-pipeline RefusalForce / ValidationFailure / ReportShape / ManifestReader / SchemaShape | MERGE (shared harness, per-pipeline instantiation) | invariant keeps executing per pipeline |
| Per-pipeline source-safety ×6 | KEEP invariants, MERGE harness only | — |
| Explorer CountRangeFilter/CacheRead/RedundancyRead/OrderingContract | MERGE (read-model property harness) | property runs per explorer via parameterization |
| 10 importer fixtures + 5 explorer fixtures | MERGE (shared skeleton; pilot first) | fixtures remain, thinner |
| Explorer Logging ×4 (1,583 LOC) | REWRITE | shared no-sensitive-payload property over the handler catalog + per-handler identity smoke replaces exact field-name enumeration |
| Words explorer page quintet (5,537 LOC) + 63 repeated-variant clusters (244 case-sites) | MERGE | parameterized shared suites execute the same titles per page; page-specific describes remain in place |
| Words api specs ×5 | MERGE | same |
| `abwab-page.component.spec.ts`, `word-types-explorer-page.component.spec.ts` | REWRITE | behavior-for-behavior; no case dropped without a named successor |
| Duplicated variant case-*implementations* after harness merges land | DELETE_CANDIDATE (as the merge's final step only) | the parameterized suite that now executes the identical titles — deletion is conditional on the harness landing and `test:gates`/lane counts confirming selection |
| Exact log-field-name enumerations in Logging tests | DELETE_CANDIDATE (within the §5.2 rewrite) | shared safe-fields property + identity smoke |
| Access schema three-layer overlap | NEEDS_MEASUREMENT | case-level reading before any claim |
| implementation-pinned specs (32 files) | REWRITE opportunistically on next touch | — |
| canonical-data lane runtime, e2e runtime/flakiness | NEEDS_MEASUREMENT | — |
| RUN_LESS_OFTEN / MOVE_TO_E2E / MOVE_FROM_E2E | **none proposed** | cadence is already risk-scoped by `TESTING_STRATEGY.md:79-260` (see §13); moving coverage into the opt-in e2e layer would weaken enforcement |

### Seven-question analysis for the five load-bearing proposals

**P1 — Frontend explorer/page harness merge (§6.1, §6.4, the 63 clusters).**
1. *Value today:* per-page regression nets for 5 explorer pages, each independently editable.
2. *Dependents:* feature-words lane (92 files); `test:gates` lane accounting; no production code.
3. *Risk of change:* a parameterized harness can mask per-page template divergence; a page whose template drifts from the shared fixture could pass a shared suite wrongly.
4. *Equivalent protection elsewhere:* partially — e2e `words-explorers.e2e.ts` covers the journeys but is opt-in; not equivalent.
5. *Smallest safe step:* extract ONE cluster (the 6-case sorting contract, identical ×4 by title) into a shared helper consumed by all four pages; keep files/titles otherwise untouched.
6. *Later verification:* lane case counts before/after (runtime case count must not drop for retained behaviors); `test:gates`; feature-words lane green.
7. *Recurring cost removed:* ~3,000–5,000 spec LOC (LIKELY) and, more importantly, the ~1,100–1,600 LOC authoring cost of every future explorer page.

**P2 — Backend per-pipeline invariant harness (§5.1, §5.4).**
1. *Value:* refusal/validation/report/manifest invariants executing against each importer's real wiring.
2. *Dependents:* pipeline lane (113 classes), catalog TSV rows, importer fixtures.
3. *Risk:* pipeline-specific edges (Navigation's orphaned-column rerun guard) getting flattened into a generic harness and silently lost.
4. *Equivalent protection:* none — these are the only executable checks of the `--force` safety contract.
5. *Smallest safe step:* shared assertion helpers (refusal exit-code/message/snapshot-immutability) consumed by existing classes, before any class merging.
6. *Verification:* pipeline lane count unchanged per pipeline; catalog parity test green.
7. *Recurring cost removed:* ~1,000–2,000 LOC across the cluster families (LIKELY at low end) + one-data-row cost for future pipelines.

**P3 — Explorer Logging rewrite (§5.2).**
1. *Value:* per-handler log-shape pinning + the no-sensitive-content guarantee.
2. *Dependents:* tier-b lane; LOGGING_GUIDELINES conformance evidence.
3. *Risk:* losing the only enforcement that raw search text / Quran text never reaches logs.
4. *Equivalent protection:* none elsewhere (grep-confirmed for test code).
5. *Smallest safe step:* add the shared sensitive-payload property first; only then thin the field-name enumerations.
6. *Verification:* the property test enumerates the same handler catalog (count-asserted); tier-b green.
7. *Recurring cost removed:* ~800–1,200 LOC; ends the false-failure tax on every telemetry field change.

**P4 — Importer fixture skeleton (§5.3).**
1. *Value:* isolated, reset-per-test import environments per pipeline.
2. *Dependents:* all 122 pipeline classes + canonical classes; DB-safety machinery (leases, reset contracts).
3. *Risk:* highest of the four — a skeleton bug corrupts isolation for every pipeline suite at once.
4. *Equivalent protection:* `AccessCollectionResetContractTests`/`SmokeCollectionResetContractTests` prove the reset contract pattern is testable — the skeleton must ship with its own reset-contract test.
5. *Smallest safe step:* pilot with the two thinnest fixtures (`ImportTestFixture` 93 LOC, `WordsDisplayTestFixture` 176 LOC).
6. *Verification:* pipeline + canonical lanes green with identical per-class counts; container-cleanup accounting unchanged.
7. *Recurring cost removed:* NEEDS_MEASUREMENT; plausibly 1,500–2,500 LOC and most of the ~500–900 LOC fixture cost of any future pipeline.

**P5 — abwab-page + word-types-explorer-page rewrites (§6.2, §6.3).**
1. *Value today:* the deepest regression nets on the two highest-churn pages — focus management, archived-door unreachability, bulk mode, deep-link modal state (abwab); the heaviest markup coverage in the explorer family (word-types).
2. *Dependents:* feature-abwab and feature-words lanes; `test:gates` lane accounting; no production code.
3. *Risk of change:* a behavior-for-behavior rewrite can silently drop a case, and pruning structural selectors can remove a11y/RTL assertions that are §29 protection.
4. *Equivalent protection elsewhere:* partial — e2e covers the abwab journeys and URL/a11y behaviors, but it is opt-in and never a required gate; not equivalent.
5. *Smallest safe step:* split one feature-slice describe into a behavior-named file using role/testid queries, with every case title-mapped to its predecessor; touch nothing else.
6. *Later verification:* per-lane runtime case counts unchanged for retained behaviors; `test:gates`; feature-abwab and feature-words lanes green.
7. *Recurring cost removed:* ~500–1,200 LOC (secondary); primarily the false-failure tax of 370+223 structural markup assertions on the highest-churn templates.

---

## 12. Candidate reduction ranges

**Runtime — honest position: no reduction is needed, and none is proposed as a goal.**
Backend all-lane wall is ~6 min; the full backend gate is estimated 5–7 min; frontend
`test:full` is 3.9 min. If the frontend merges of P1 land, `test:full` wall plausibly drops
10–20% (abwab-page plus four of the five explorer page specs occupy 5 of the 7 slowest
file slots) — **NEEDS_MEASUREMENT**, and a side effect, not a target.

**Test LOC — the actual target:**

| Source | Candidate range | Confidence |
|---|---|---|
| Frontend explorer/page/api harness merges (P1) | 4,000–7,000 | LIKELY (low end) |
| Backend Logging rewrite (P3) | 800–1,200 | LIKELY |
| Backend per-pipeline cluster harness (P2) | 1,000–2,000 | LIKELY (low end) |
| Importer fixture skeleton (P4) | 1,500–2,500 | NEEDS_MEASUREMENT |
| abwab-page + word-types-page rewrites (P5) | 500–1,200 | LIKELY |
| **Total** | **≈8,000–14,000 LOC (7–12% of the test estate)** | LIKELY at ~8k; NEEDS_MEASUREMENT beyond ~10k |

Equally important and not LOC-denominated: the marginal cost of the *next* explorer page,
importer pipeline, or query handler drops from "copy and adapt a 1,000+ LOC suite/fixture"
to "add a parameter row + page-specific deltas".

---

## 13. Future lane strategy

**CONFIRMED: the existing lane system is already the risk-based matrix the brief asks
audits to design toward.** Backend: 11 lanes over a drift-proof TSV catalog with exact
class/method selection, DB-safety serialization, and per-scope triggers
(`TESTING_STRATEGY.md:70-95`); frontend: 7 partitioning primary lanes + 3 cross-cuts +
composite pre-PR, self-checked by `test:gates` (`TESTING_STRATEGY.md:200-228`). The
execution-trigger matrix (`TESTING_STRATEGY.md:230-260`) already reserves broad gates for
milestone/review/pre-PR boundaries and forbids repeat broad runs. There is no CI
(`data/workflow-gate-inventory.json` `ci.present=false`), so lane discipline is the only
gate discipline — another reason not to weaken it.

Proposed future direction (audit-level, not a plan):

- **Keep the lane topology unchanged.** No lane is mis-scoped; measured costs confirm each
  lane is proportionate to its trigger.
- The only cadence-shaped findings are **cross-boundary duplications outside the test
  suites** (pre-pr always rebuilding, `test:pre-pr` compiling overlapping TS three times,
  deploy-smoke/commit-time re-verification, every migration dragging the canonical dump) —
  these are owned and detailed by report 10 (`data/workflow-gate-inventory.json`
  `redundancy_flags`).
- After P1–P5 land, lane *contents* shrink but lane *names, triggers and selection
  mechanics* should not change — this keeps `TESTING_STRATEGY.md` and both catalogs stable.
- `canonical-data` should get one measured baseline run (it has never been timed) so its
  release-trigger cost is a number instead of folklore.

---

## 14. Risks of the proposed simplifications

1. **Flattening pipeline-specific edges** (Navigation's orphaned-column rerun guard;
   Tafsir's writer-level check) into generic harnesses — mitigated by keeping per-pipeline
   instantiation and treating extra facts as first-class (§5.1). LIKELY the top risk.
2. **Losing the log-content safety net** if the Logging rewrite deletes enumeration before
   the shared sensitive-payload property exists (§5.2 order-of-operations requirement).
3. **Fixture-skeleton blast radius**: a bug in shared lease/reset plumbing degrades
   isolation for all 122 pipeline classes simultaneously (§5.3; pilot-first, reset-contract
   test required).
4. **Parameterized frontend suites masking per-page divergence**: a page whose template
   drifts from the shared fixture may pass shared cases vacuously — mitigated by keeping
   page-specific describes in per-page files and watching runtime case counts per lane.
5. **Markup-assertion pruning removing a11y/RTL protection**: 472 aria/role assertions are
   protection under brief §29; rewrites must distinguish structural-selector pinning from
   semantic/a11y assertions.
6. **Governance**: `CLAUDE.md`/`TESTING_STRATEGY.md` already require documented
   obsolete/redundant proof + named replacement coverage for any test deletion — every
   DELETE_CANDIDATE above is conditional on its named replacement landing first. The audit
   itself deletes nothing.

---

## 15. Mandatory questions answered (brief §25, Q22–31)

| # | Question | Answer |
|---|---|---|
| 22 | Total test LOC? | **≈112.3k–114.2k** (CONFIRMED; low endpoint = test code only, high endpoint adds the 1,969-LOC support row): backend tests 56,155 physical lines (55,808 by newline count; delta = exactly 1/file) + backend test support 1,969 + frontend specs 54,145 + e2e 1,968. Roughly 1:1 with 113,155 handwritten product LOC. Evidence §1. |
| 23 | Total test count? | Distinct runtime: backend **2,211** measured across the tier-b/pipeline/smoke partition (+10 unmeasured Canonical classes, ~30–35 est. cases) + frontend **2,964** (`test:full`) + e2e **61 static** call sites (runtime higher; unmeasured) ≈ **~5,200–5,300 distinct executions**. Static lower bounds: 1,860 backend + 2,459 frontend. The lane-sum figure 3,231 double-counts overlapping backend lanes (CONFIRMED, §2.2). |
| 24 | Backend runtime by lane? | fast 7.5 s / access 61.3 / access-db 43.0 / migration 21.9 / process 21.4 / smoke 72.6 / tier-b 65.9 / pipeline 64.1 — total 357.7 s; pre-pr estimated 5–7 min (not run); canonical-data NEEDS_MEASUREMENT (CONFIRMED for measured lanes; single run, no variance data). §2.2. |
| 25 | Frontend runtime by lane? | typecheck 14.6 s / build:verify 18.3 / test:fast 72.5 (810 tests) / test:feature:words 114.3 (1,379) / test:full 232.2 (2,964); pre-pr = composite, unmeasured; authorization/composition/shared lanes unmeasured (CONFIRMED for measured gates). §3.2. |
| 26 | Slowest test groups? | Backend: `PostgreSqlTestProcessContractTests` (9.7 s max — deliberate lock-wait tests), `TranslationSourceSafetyTests` (5.9), `SmokeAuthPipelineTests` (5.8), `AccessAdminCommandTests` (≤4.8, selected by 3 lanes), `LogtoSubjectRelinkEndpointTests` (4.5). Frontend: `abwab-page` 5.86 s, `word-types-explorer-page` 4.96, then the stems/lemmas/roots/access-admin page specs (2.1–2.6). All are high-protection or markup-heavy suites; none is a runtime problem (CONFIRMED). §2.2, §3.2. |
| 27 | Highest-value safety tests? | The §4 protection map: Access authorization pipeline + Owner + direct-permission + audit classes; `SmokeAbwabWriteTests` (concurrency/conflict); `AccessMigrationPathTests`; the 10 Canonical source-gated classes + 22 source/hash/manifest classes + rollback tests; frontend auth store/guard/coordinator + url-sync family; both catalog-drift self-checks (CONFIRMED class existence; LIKELY category boundaries). |
| 28 | Redundant/low-value clusters? | Backend: 13 inventoried; biggest adjudicated by reading — RefusalForce ×7 (795 LOC, shared shape not copy-paste → MERGE), Logging ×4 (1,583 LOC → REWRITE), importer fixtures (4,559 LOC → MERGE-pilot), per-pipeline validation/report/manifest/schema families → MERGE. Frontend: 72 clusters (63 in words, 244 case-sites); stems↔lemmas page specs 84% title-identical → MERGE; abwab-page spec → REWRITE (§5–6). |
| 29 | Which GET tests can be consolidated? | Not intra-class permutations — those turned out property-shaped and cheap (LemmasListReadTests read in full). The consolidation target is the **same read-model property re-implemented per explorer feature** (sort-preserves-scope, count-range clamp, cache, redundancy, ordering) → one parameterized harness with per-feature token/seed data (LIKELY). §8. |
| 30 | Which importer/canonical tests are essential? | All 10 Canonical classes (source-gated, skip-with-reason), all 22 source/hash/manifest classes, refusal/force + rollback + validation-failure invariants per pipeline — these enforce the documented DataImporter safety contract, and the CLI remains "the operational entry point for every Quran import" with no pipeline marked obsolete (CONFIRMED). Essential = the invariants; the duplicated harnesses around them are not (§7). Whether specific importers will ever rerun is UNKNOWN (product decision, not recorded in repo). |
| 31 | Full-suite vs focused cadence? | Already codified and correct: focused lanes per-edit/per-slice; smoke on any `Backend/api/` contract/auth/middleware change; tier-b at milestone/review/ordinary backend pre-PR; backend `pre-pr` only for shared-infrastructure/release/formal-review triggers; canonical-data at canonical/release triggers; frontend `test:full` once per frontend PR; e2e opt-in always (`TESTING_STRATEGY.md:79-95,206-260` — CONFIRMED). No test group needs more *or* less cadence than documented; the real duplication is cross-boundary re-verification outside the suites, owned by report 10. |

---

## 16. Measurement gaps

- **`canonical-data` lane runtime**: never measured; resources are staged locally so a
  future measured run is possible (NEEDS_MEASUREMENT).
- **`pre-pr` backend lane**: estimated (5–7 min) from measured shards, not executed;
  includes an unmeasured exclusive postgres:18 restore shard.
- **E2E runtime, true case counts, and flakiness**: requires live DB + dual servers; no
  historical flakiness records exist anywhere in the repo (NEEDS_MEASUREMENT / UNKNOWN).
- **Runtime variance**: every number is a single solo run; no repeat-run variance data.
- **MemberData expansion**: backend static case counts are a lower bound (52 MemberData
  refs counted as 1 case each).
- **Frontend cross-cut lane runtimes** (authorization/composition/shared): unmeasured.
- **Access three-layer schema overlap**: needs case-level reading before any redundancy
  claim (NEEDS_MEASUREMENT).
- **Savings ranges for P1–P5**: authoring estimates from verified cluster LOC, not from a
  performed refactor; the fixture-skeleton saving is design-dependent
  (NEEDS_MEASUREMENT).
- **Category boundaries** (authorization/security, api-read, source-safety) rest on
  class-name heuristics (LIKELY, per the data files); the gate/kind/lane numbers rest on
  the TSV catalog and are CONFIRMED.
- Two data-file blemishes found during verification, neither material: the ReportShape
  cluster list contains 4 duplicate entries (real distinct count 7, not 11), and backend
  test LOC differs by exactly one line per file between the two scanners.
