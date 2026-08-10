# Test Suite Rationalization — Current State Audit

**Branch** `audit/project-simplification` · **HEAD** `0d5e5a97` · **Audit date** 2026-08-10
**State audited:** immediately after completion of Golden UI Plan 7.

This is a **read-only audit**. No production code, test code, CI, script, config, package, or test
command was modified, deleted, renamed, or created by this work. It classifies and measures; it
implements nothing and instructs nothing. The only file it writes is this report.

**Method.** Every number below is measured against the working tree at `0d5e5a97`, not estimated
and not carried over from a prior document. Static counts come from direct file scans; runtimes
come from real runs on this machine performed for this audit; classification comes from a
two-pass adversarial review in which every backend test class was judged once by a
classification pass and then re-judged by an opposing pass argued from the deletion criteria
(the second pass overturned 141 of the 176 verdicts it examined — see §6.4 for why both passes
are reported and how the disagreement was resolved).

> **Sanctioned exception.** `TESTING_STRATEGY.md:17` forbids carrying test counts and durations in
> prose documents. This is an audit artifact and is the sanctioned exception. None of these numbers
> may be copied into steady-state docs.

**Measurement environment.** Intel Core i7-6820HQ (8 threads), 14 GB RAM, Linux 7.0.0-29-generic,
.NET 10.0.110, Node v20.20.2, Docker with `postgres:16-alpine` / `postgres:18-alpine`
testcontainers, `QURAN_DASHBOARD_TEST_DB_PARALLELISM=4`, Vitest capped at 2 forks.

---

## 1. Headline

Three findings drive everything else.

**1. The frontend spec suite is now as large as the frontend product it tests.** 60,247 lines of
`*.spec.ts` against 61,585 lines of all frontend product code (`.ts` + `.html` + `.scss`) — a
ratio of **0.98 : 1**.

**2. Golden UI Plan 7 changed no user-facing behavior and cost 6,745 lines of spec authoring.**
Across its 12 commits the plan touched **96 unit spec files (+6,745 / −643)** while the Playwright
E2E layer needed **3 files (+12 / −11)** and the backend test suite needed **zero files**. The same
refactor, verified through a browser instead of through jsdom, would have cost ~23 lines instead of
~7,388.

**3. Change amplification is concentrated, measurable, and almost entirely in component specs.**
69% of component specs (79/114) and 67% of directive specs were edited by that behavior-neutral
refactor, versus 6% of state/facade specs, 6% of model/util specs, and 0% of data-access specs.
Over full repository history, component specs are edited 3.6 times each; pure-logic specs 0.7.

The corollary sets the whole policy: **the expensive part of the estate is not runtime, it is
authoring and re-authoring.** The frontend suite's own execution accounts for 115.0 s of a 337.8 s
wall clock; the other 222.8 s is jsdom environment setup, and the real cost is the ~7,400 lines a
design-system migration forced someone to write.

---

## 2. Frontend inventory

### 2.1 Totals

| Metric | Value |
|---|---:|
| `*.spec.ts` files | **248** |
| Test cases executed | **3,371** |
| Static `it(` / `test(` call sites | 2,753 |
| `describe` blocks | 610 |
| Spec LOC | **60,247** |
| Spec bytes / approx. agent-context tokens | 2,405,792 / ~601,000 |
| Full-suite runtime (`npm test`, clean) | **337.8 s wall** (303.7 s runner) |
| — of which test execution | 115.0 s |
| — of which jsdom environment setup | 264.4 s |
| Playwright E2E files / cases / LOC | 17 / 61 / 1,602 |

The executed count (3,371) exceeds the static call-site count (2,753) because parameterized
`.each` blocks expand at runtime; 165 `.each(` blocks exist across the corpus. Both numbers are
given wherever they matter, and the executed count is used for all decision arithmetic.

### 2.2 Distribution by feature area

Runtime is per-file test time from the clean run; it excludes the shared environment setup.

| Area | Files | Cases | LOC | Runtime | % runtime |
|---|---:|---:|---:|---:|---:|
| `features/words` | 93 | 1,391 | 24,515 | 50.2 s | 43.7% |
| `features/abwab` | 32 | 767 | 12,154 | 24.9 s | 21.7% |
| `features/mushaf` | 43 | 329 | 9,209 | 9.2 s | 8.0% |
| `features/access-admin` | 18 | 296 | 5,492 | 10.4 s | 9.0% |
| `shared/ui` | 31 | 309 | 4,423 | 9.4 s | 8.2% |
| `core/navigation` | 7 | 93 | 1,368 | 2.2 s | 1.9% |
| `core/layout` | 6 | 72 | 943 | 4.1 s | 3.6% |
| `core/auth` | 6 | 46 | 878 | 0.9 s | 0.8% |
| `core/data-access` | 3 | 15 | 275 | 0.6 s | 0.6% |
| app root (3) + `environments` (1) | 4 | 11 | 433 | 2.3 s | 2.0% |
| `features/dashboard`, `features/auth` | 2 | 14 | 309 | 0.7 s | 0.6% |
| `shared/layout`, `shared/url`, `core/caching` | 3 | 28 | 248 | <0.1 s | 0.0% |
| **Total** | **248** | **3,371** | **60,247** | **115.0 s** | **100%** |

**Largest and most expensive areas.** `features/words` and `features/abwab` are 125 of 248 files
(50%), 2,158 of 3,371 cases (64%), 36,669 of 60,247 LOC (61%), and 65.4% of test runtime. Any
rationalization that does not touch those two areas cannot move the numbers.

### 2.3 Distribution by spec kind — where the cost actually sits

| Kind | Files | Cases | LOC | Uses TestBed | Golden-UI churn | Lifetime edits/file |
|---|---:|---:|---:|---:|---:|---:|
| component | 114 | 1,551* | 34,792 | 114 | **69%** | **3.6** |
| state / facade | 50 | 713* | 16,139 | 34 | 6% | 3.1 |
| other | 28 | 136* | 3,140 | 8 | 18% | 1.3 |
| data-access | 11 | 107* | 2,153 | 11 | **0%** | 2.4 |
| model / util | 32 | 159* | 2,121 | 0 | 6% | **0.7** |
| directive | 6 | 59* | 1,120 | 6 | **67%** | 1.0 |
| routing / guard | 6 | 26* | 750 | 4 | 17% | 2.5 |
| pipe | 1 | 2* | 32 | 1 | 0% | 0.0 |

\* static call sites, for kind-level comparability.

Split by the single property that predicts amplification — whether the spec boots an Angular
`TestBed`:

| Group | Files | Cases (executed) | Test runtime |
|---|---:|---:|---:|
| **TestBed specs** | 178 | 2,442 | **113.4 s** |
| **Pure-logic specs (no TestBed)** | 70 | 929 | **1.6 s** |

Pure-logic specs are 28% of the files and 27.6% of the cases for **1.4% of the execution time**,
and they are the ones a refactor does not touch. This is the cleanest available line between the
part of the suite that pays for itself and the part that does not.

### 2.4 Growth since the previous audit

Audit A measured commit `72792ba9` on `dev` (2026-08-08), two days earlier.

| | Then (`72792ba9`) | Now (`0d5e5a97`) | Δ |
|---|---:|---:|---:|
| Spec files | 223 | 248 | **+25** |
| Spec LOC | 54,145 | 60,247 | **+6,102** |
| Cases executed | 2,964 | 3,371 | **+407** |
| Suite wall time | 232.2 s | 337.8 s | **+105.6 s (+45%)** |
| Backend test `.cs` files | 347 | 347 | 0 |

The Golden UI plan converged the UI onto one design system — a *reduction* in product concepts —
and the spec suite grew 11% in files, 11% in LOC, 14% in cases, and 45% in wall time. This is the
audit's central quantitative claim about direction of travel.

---

## 3. Backend inventory

### 3.1 Totals

One test project: `Backend/tests/QuranDashboard.Tests`.

| Metric | Value |
|---|---:|
| `.cs` files under the test project | 347 |
| — files carrying test attributes | **257** |
| — fixture / support files with no test attribute | 90 |
| Test classes (locked by `test-gates.tsv`, parity-enforced) | **268** |
| Declared test methods | **1,507** |
| — `[Fact]` / `[Theory]` / custom source-gated | 1,296 / 178 / 33 |
| `[InlineData]` rows / `[MemberData]` refs | 482 / 52 |
| Test cases executed (full suite) | **2,258** |
| Test-file LOC | **43,809** |
| Whole test project LOC (incl. support) | 55,808 |
| Full-suite runtime (`test-backend pre-pr`, clean, 2 shards) | **429.6 s wall** |

`test-gates.tsv` has exactly 268 data rows and `TestGateCatalogTests` enforces bidirectional parity
with the classes discovered on disk, so 268 is authoritative rather than inferred.

### 3.2 Runtime by test family

Each lane measured independently on this machine. The four gate lanes partition the catalog.

| Lane | Wall | Cases | Note |
|---|---:|---:|---|
| `canonical-data` | **371.1 s** | 47 | 86% of suite runtime for 2% of cases — restores the canonical dump and replays real imports |
| `tier-b` | 67.6 s | 1,372 | the daily read/behavior bulk |
| `pipeline` | 67.3 s | 583 | the five import pipelines |
| `smoke` | 59.2 s | 256 | route sweep + authorization matrix |
| `fast` | 7.4 s | — | in-process, no database |
| **Sum of the four gate lanes** | **565.2 s** | **2,258** | vs 429.6 s for one `pre-pr` invocation, which shares fixtures |

The single most important runtime fact: **`canonical-data` is 371.1 s of the estate for 47 test
cases.** It is also, by subject, exactly the kind of source-integrity checking that belongs behind
a trigger rather than in a daily loop.

Backend lane case counts are **identical** to Audit A (`tier-b` 1,372, `pipeline` 583, `smoke` 256),
independently confirming that Golden UI Plan 7 touched no backend test.

### 3.3 Suite health — the backend suite is not reliably green in full-suite mode

This must be recorded plainly because it affects any plan built on this suite.

| Run | Mode | Result |
|---|---|---|
| `pre-pr` (under concurrent load) | full | exit 1 — 4 failures |
| `tier-b` (under concurrent load) | lane | exit 1 — 3 failures, **different set** |
| `AccessAdminCommandTests` alone | isolated | **10/10 pass**, 28.4 s |
| `pre-pr` (clean, unloaded) | full | exit 1 — **10 failures**, all `AccessSchemaDriftTests` |
| `AccessSchemaDriftTests` alone | isolated | **23/23 pass**, 22.5 s |

The failing set moves between runs and every affected class passes in isolation. Root cause is
identified, not guessed:

```
System.IO.IOException : The configured user limit (128) on the number of inotify
instances has been reached...
  at System.IO.FileSystemWatcher.StartRaisingEvents()
  at Microsoft.Extensions.FileProviders.PhysicalFileProvider.Watch(String filter)
  at Microsoft.Extensions.Hosting.HostBuilder.InitializeAppConfiguration()
```

Every host the suite boots creates a `FileSystemWatcher` over its configuration; the suite boots
enough hosts to exhaust `fs.inotify.max_user_instances` (128 on this machine). The earlier
`AccessAdminCommandTests` failure — child process exit 134 (SIGABRT) — is the same class of
resource exhaustion in the tests that spawn a full child `dotnet` process.

**This is not a product defect and not a reason to delete a test.** It is, however, a direct
consequence of how many host-booting tests the suite carries, and it is evidence that the estate
has outgrown its own execution model. Reducing host-booting test count relieves it; so does raising
the limit. Both are implementation decisions outside this audit's scope.

### 3.4 Distribution by area

| Area | Files | Methods | InlineData | LOC |
|---|---:|---:|---:|---:|
| `Api/Access` | 26 | 168 | 21 | 6,185 |
| `Quran/WordsMorphologyExplorers` | 15 | 199 | 116 | 4,486 |
| `Quran/WordsWordTypes` | 20 | 140 | 87 | 4,167 |
| `Quran/WordsMorphology` | 17 | 144 | 12 | 4,150 |
| `Smoke` (incl. `Smoke/Data`) | 15 | 121 | 4 | 2,958 |
| `Quran/Words` | 14 | 110 | 170 | 2,794 |
| `Quran/Translations` | 12 | 62 | 0 | 2,042 |
| `Abwab` | 8 | 64 | 2 | 1,857 |
| `Quran/Mutashabihat` | 9 | 43 | 0 | 1,627 |
| `Quran/Tafsirs` | 16 | 46 | 0 | 1,619 |
| `Quran/Navigation` | 12 | 49 | 6 | 1,575 |
| `Quran/FullI3rab` | 11 | 41 | 2 | 1,554 |
| `Quran/WordsMorphologyEnriched` | 6 | 37 | 0 | 1,527 |
| `Quran/MushafReader` | 24 | 55 | 19 | 1,405 |
| `Quran/WordsDisplay` | 11 | 16 | 0 | 1,332 |
| `Quran/WordsRoots` | 8 | 62 | 16 | 1,283 |
| `Quran/WordsSimpleI3rab` | 14 | 29 | 7 | 1,090 |
| `TestSupport/*` | 8 | 52 | 10 | 1,142 |
| `Api/*` (RateLimiting, Middleware, Health, ApiBehavior) | 6 | 26 | 0 | 646 |
| `Quran/Import` | 5 | 10 | 7 | 370 |

### 3.5 The duplication that makes read tests removable

`Smoke/SmokeRouteCatalog.cs` catalogs **78 route literals** against the API's **~85 HTTP
endpoints**, and `SmokeCoverageParityTests` locks that catalog bidirectionally to the live
`EndpointDataSource` — adding or changing a route fails the suite by name until the catalog is
updated. The route-smoke tier therefore already proves, for essentially the whole API surface:
routing, authorization, model binding, and serialization. `Smoke/Data/SmokeDataReadTests`
additionally proves seeded read routes answer correctly against the restored canonical dump.

Consequently a per-feature test class whose real subject is *"this read route returns 200 with a
well-shaped, correctly ordered payload"* is duplicated at the Smoke layer. That is the single
largest deletion argument in the backend, and it is why the explorer clusters
(`WordsWordTypes`, `WordsMorphologyExplorers`, `WordsRoots`, `Words` — 57 files / 58 classes,
511 declared methods, 12,730 LOC) classify overwhelmingly as DELETE.

---

## 4. Backend classification

Every one of the 268 catalogued classes was classified. Executed-case counts are attributed
per class from the clean full-suite run (2,254 of 2,258 cases attributed, 99.8%).

| Verdict | Classes | Executed cases | LOC |
|---|---:|---:|---:|
| `KEEP — BUSINESS RULE` | **23** | 194 | 5,682 |
| `KEEP — SECURITY` | **25** | 251 | 4,726 |
| `RELEASE_OR_CHANGE_GATE` | **68** | 438 | 11,604 |
| `MERGE` | **23** | 229 | 3,135 |
| `DELETE` | **129** | 1,142 | 19,622 |
| **Total** | **268** | **2,254** | **44,769** |

### 4.1 `KEEP — SECURITY` (25 classes)

The authentication and authorization boundary, kept whole and deliberately un-thinned:

`SmokeAbwabWriteAuthorizationTests`, `SmokeAccessAdministrationAuthorizationTests`,
`SmokeAuthPipelineTests`, `SmokeRoutePipelineTests`, `SmokeBootGuardTests`,
`SmokeRouteBaselineTests`, `SmokeCoverageParityTests`, `AbwabPermissionCatalogueTests`,
`AccessAdminCommandTests`, `AccessMeEndpointTests`, `AccessRolesTests`,
`AuthorizationPipelineTests`, `AuthorizationRejectionResponseTests`,
`AuthorizationRequirementHandlerTests`, `AuthorizationStateResolverTests`,
`EmailIdentityNormalizerTests`, `LogtoSubjectRelinkEndpointTests`, `OwnerBootstrapOptionsTests`,
`OwnerReconciliationServiceTests`, `PermissionCatalogueSynchronizerTests`,
`UnsafeEndpointMetadataValidatorTests`, `UserProvisioningServiceTests`,
`TestAccessPersonasContractTests`, `RateLimitingIntegrationTests`, `GlobalExceptionHandlerTests`.

These cover: 401/403 envelope shapes across five invalid-credential classes, the Owner-promotion
evidence matrix (twelve invalid identity-evidence shapes that must not promote), the exact
19 Abwab permission codes, the per-persona Abwab write authorization matrix, denial
short-circuiting before any handler runs, pending/disabled account states, and production write
protection in the admin CLI.

### 4.2 `KEEP — BUSINESS RULE` (23 classes)

Critical writes, transactions, concurrency, audit, rollback, and corruption prevention:

`AbwabDoorWriteBehaviorTests` (38 methods — the Abwab write core), `AbwabTemplateApplyBehaviorTests`,
`AccessAdministrationEndpointTests` (ordered audit trail, transactional rollback on audit CHECK
violation, two Owners racing one target, actor de-owned while awaiting a row lock),
`AccessAuditEventPersistenceTests` (append-only: EF refuses update and delete),
`MorphologyAssemblerTests`, `MorphologyRefusalForceTests`, `MorphologyValidationFailureTests`,
`WordLemmaNormalizationApplierTests`, `TranslationRefusalForceTests`, `TranslationRollbackTests`,
`TafsirForceRebuildTests`, `TafsirRollbackTests`, `MutashabihatRefusalForceTests`,
`MutashabihatValidationFailureTests`, `NavigationMetadataWriteIsolationTests`,
`FullI3rabForceRebuildTests`, `AyahStudyCorruptCoveredAyahKeysTests`,
`WordAnalysisSegmentFallbackTests`, `DisplayWordsDeterministicIdTests`,
`DisplayWordsRefusalForceTests`, `DisplayWordsSourceUntouchedTests`,
`DisplayWordsValidationFailureTests`, `TestGateCatalogTests`.

### 4.3 `RELEASE_OR_CHANGE_GATE` (68 classes, 438 cases, 11,604 LOC)

Expensive and stable checks whose subject is Quran source data, importer behavior, or schema
shape. They retain real value but only when the thing they guard changes. Moving them out of the
daily loop is where most of the backend runtime saving lives — this bucket contains the whole
`canonical-data` lane (371.1 s).

| Trigger cluster | Classes |
|---|---:|
| `Quran/Import` (foundation import) | 9 |
| `Quran/WordsMorphology` | 7 |
| `Quran/WordsSimpleI3rab` | 7 |
| `Quran/Translations` | 7 |
| `Quran/Tafsirs` | 7 |
| `Quran/Navigation` | 7 |
| `Quran/WordsMorphologyEnriched` | 5 |
| `Quran/WordsDisplay` | 5 |
| `Api/Access` (migration path, schema drift, catalogue startup sync) | 4 |
| `Quran/FullI3rab` | 3 |
| `TestSupport/PostgreSql`, `Quran/Mutashabihat` | 2 each |
| `Smoke` (canonical data tier), `Quran/WordsWordTypes`, `Abwab` (schema shape) | 1 each |

Notable: `AccessMigrationPathTests` and `AccessSchemaDriftTests` are genuinely valuable — they are
the deploy preflight's own proof — but their subject is the migration chain and schema head, so
they should fire on migration/schema/catalogue change and before release, not on every run.
`AbwabSchemaTests` moves here for the same reason: twelve of its fourteen methods interrogate
`information_schema` / `pg_index` for exact column sets and index shapes.

### 4.4 `MERGE` (23 classes)

Content worth keeping that does not deserve its own class. The largest is
`SmokeAbwabWriteTests` (73 methods, 1,295 LOC — the largest backend test file), whose domain rules
are already proven one layer down against the same database by `AbwabDoorWriteBehaviorTests`; the
authorization matrix it also carries stays independently in `SmokeAbwabWriteAuthorizationTests`
(`KEEP — SECURITY`). Others fold reader/manifest/report-shape siblings into their pipeline's
retained class.

### 4.5 `DELETE` (129 classes, 1,142 cases, 19,622 LOC)

Dominated by the named deletion categories. Representative reasoning, quoted from the review:

- `WordTypesTableReadTests` (19 methods, 522 LOC) — "522 lines of pure read permutations …
  roots/stems/lemmas grouping counts, sort ordering, page 1..4 slicing … plus cache-key string
  assertions … the underlying '200 with a well-shaped ordered payload' claim is already covered by
  `SmokeRouteCatalog.cs:203` plus `Smoke/Data`."
- `WordTypesOrderingContractTests` — "2 theories × 2 views × 6 count-sort tokens = 24 `ORDER BY`
  permutations … the transaction here is only a rollback harness for seeding, not a transactional
  invariant under test."
- `WordTypesScopeCountsReadTests` — "one read formula checked against another read formula over the
  same `BaseRowsSql`", plus raw-SQL text audits and cache-key isolation strings.
- `AccessSchemaModelTests` — EF design-time model metadata only; every fact is proven against the
  live database by `AccessSchemaDriftTests`.
- `UserPermissionPersistenceTests` — asserts PostgreSQL SqlState `23505` / `23503`: framework
  behavior as implementation detail.
- `AuthorizationPolicyRegistrationTests` — container-registration assertions; the behavior is proven
  end-to-end by `AuthorizationPipelineTests` (retained as `KEEP — SECURITY`).
- `RateLimitRejectionWriterTests` — lower-layer duplicate of `RateLimitingIntegrationTests`.
- `AbwabCollectionKeyIsolationTests` — guards the test collection's own isolation policy, not a
  product rule.

**No class was marked DELETE for being slow.** The six DELETE classes whose names contain
write/auth/audit/schema keywords were each re-examined individually (§6.4) and each is duplicated
at another layer or asserts framework behavior.

---

## 5. Frontend under the locked policy

### 5.1 The policy applied

All 248 `*.spec.ts` files are deletion candidates; no new `*.spec.ts` by default; verification moves
to typecheck + production build + Playwright/E2E + browser/visual verification; isolated unit tests
require explicit owner approval.

**The replacement gate is measured and cheap:**

| Gate | Wall |
|---|---:|
| `typecheck:app` | 8.4 s |
| `build:verify` (production build) | 29.6 s |
| **Replacement gate total** | **38.0 s** |
| `typecheck:spec` (becomes moot once specs are gone) | 14.1 s |
| Current gate (`typecheck` + build + `test:full`) | **389.9 s** |

Deleting the specs takes the frontend pre-PR gate from **389.9 s to 38.0 s — a 90.3% reduction** —
and removes 60,247 LOC and ~601,000 tokens of agent context.

Two mechanical consequences, recorded but **not acted on**: `npm run typecheck` currently chains
`typecheck:app && typecheck:spec`, and `tsconfig.spec.json` includes only `src/**/*.spec.ts`; with
no specs, `typecheck:spec` compiles nothing. `test:pre-pr` chains `test:full`, which would select
nothing. `testing/verify-test-gates.mjs` and the ten `angular.json` test configurations describe a
partition that would no longer exist.

### 5.2 Behaviors currently protected only by specs

The review of all four frontend areas identified **75 residue behaviors** that survive the filter
"typecheck and the production build cannot catch this, and no existing E2E flow covers it."
By area: `features/mushaf` 27, `features/abwab` + `features/access-admin` 23, `shared/ui` +
`core/*` 15, `features/words` 10.

The critical ones, which must have replacement coverage **before** deletion:

**Authorization-shaped (frontend affordance, not the security boundary):**
- Each of the 19 Abwab write affordances gated on its exact permission code.
- A write `401` starts login exactly once and never retries the mutation; a `403` refreshes access
  through the write-auth-failure coordinator; concurrent 401s start exactly one login flow.
- Permission-draft submission cannot silently revoke — `codesForSubmission()` re-appends codes
  granted during the edit.
- Permission assignment fails closed over a degraded catalogue.
- The `/me` access snapshot fails closed: an unknown permission code nulls the whole current user.
- The Owner route guard admits only an authenticated, active Owner.
- The HTTP origin allowlist fails closed; the Logto access token never leaves the API origin.

**Data-integrity and URL-contract shaped:**
- Explorer and reader URL contracts: fail-closed parsing, canonical serialization, deep-link
  hydration, Back/Forward replay, and self-correcting rewrite of out-of-range values.
- The detail-overlay frame-stack grammar and history semantics (dialog Back converging with
  browser Back, close/restore, retained-closed keys).
- Uthmani segment slicing integrity — contiguous grapheme-based slices over the authoritative
  string, with the highlight refusing to paint when the host text node no longer matches.
- `loadPage` staleness guard and teardown disposal; out-of-order response guards; optimistic
  concurrency tokens on bulk writes; move-picker cycle guard.
- Reference-counted body scroll lock across nested layers, and exactly one active focus trap.

**Important distinction.** The frontend permission specs (17 files, 295 cases) protect *UI
affordances*, not the security boundary. Backend enforcement is independently proven by the Smoke
per-persona authorization matrix and the 25 `KEEP — SECURITY` classes. Deleting them is a UX-
regression risk, not a security-regression risk.

### 5.3 Replacement E2E flows required

The four reviews converge on **18 new Playwright flows**, deliberately few and broad:

| Area | Flows |
|---|---|
| mushaf | `mushaf-url-contract`, `mushaf-request-economy`, `mushaf-reading-controls`, `mushaf-word-render-integrity`, `mushaf-loading-geometry` |
| abwab / access-admin | `abwab-write-permissions`†, `abwab-destructive-writes`†, `abwab-deep-links`, `access-admin-workspace`†, `access-admin-audit-and-recovery`† |
| words | `words-url-contract`, `words-detail-overlay-journey`, `word-types-scope-machine`, `words-keyboard-browsing`, `words-degraded-reads` |
| shared / core | `detail-overlay-journey`, `layered-dismissal-and-keyboard`, `authenticated-owner-journey`† |

† **Blocked.** Five of the eighteen require an authenticated persona that does not exist.

### 5.4 Blocking constraint — E2E cannot cover authenticated behavior today

Verified directly: there is **no `Authorization` header anywhere under `e2e/`**.
`e2e/fixtures/logto.ts` stubs only the OIDC discovery document and an empty JWKS — no flow signs
in. `e2e/fixtures/abwab.ts` seeds its sandbox through **anonymous** API writes
(`request.post('/api/abwab/sections')` with no credentials). `e2e/README.md` records the
consequence: *"Unsafe Abwab routes now require a real authorized persona … those write-oriented
specs receive `401` until an approved E2E authentication/bootstrap mechanism is supplied."*

So a meaningful part of the existing E2E suite is already blocked, and **the 1,063 spec cases in
`features/abwab` (767) and `features/access-admin` (296) cannot be migrated to E2E until an
auth/bootstrap mechanism exists.** This is the single hard prerequisite for the frontend half of
the policy.

Note also that `TESTING_STRATEGY.md` §11 currently designates browser E2E as *"opt-in, never a
required gate,"* and §12 states that measurements *"do not authorize … E2E promotion."* The locked
policy supersedes both, but that is an owner decision that must be written into those sections as
part of implementation — this audit does not make it.

### 5.5 Recommended owner-approval retention set

The policy requires explicit owner approval for any retained isolated unit test. The evidence
supports approving exactly one coherent group: **the 70 pure-logic (non-TestBed) specs — 929 cases,
7,441 LOC, 1.6 s of runtime.**

They earn it on measured grounds: 1.4% of test execution time, 0.7 lifetime edits per file (vs 3.6
for component specs), 6% churn under a refactor that rewrote 69% of component specs, and they
encode genuine domain algorithms — Uthmani segment slicing, Arabic search normalization, ligature
selection, verse-key derivation, explorer sort tokens, pagination windows, floating-layer placement,
the surah/juz catalog invariants, and every explorer's URL codec.

Retaining them also **materially shrinks the E2E replacement burden**, because the URL-contract
behaviors — the largest single residue group and the subject of four of the eighteen proposed flows
— stay covered by fast, refactor-stable tests.

Both scenarios are carried through the final tables:
- **Scenario A — policy-strict:** delete all 248.
- **Scenario B — recommended:** delete 178 TestBed specs, retain the 70 pure-logic specs.

---

## 6. Change amplification

Maintenance cost is treated here as a first-class metric: a healthy test survives internal
refactoring when the behavior it protects has not changed.

### 6.1 The controlled experiment

Golden UI Plan 7 (`87834abe~1..0d5e5a97`, 12 commits) is a near-perfect natural experiment: a
design-system convergence with no intended user-facing behavior change.

| Layer | Files touched | Lines |
|---|---:|---:|
| Frontend production (`.ts`/`.html`/`.scss`) | 336 | +11,730 / −8,177 |
| **Unit specs** | **96** | **+6,745 / −643** |
| — newly authored specs | 57 | +4,694 |
| — pre-existing specs rewritten | 39 | +2,051 / −643 |
| **Playwright E2E** | **3** | **+12 / −11** |
| **Backend tests** | **0** | — |

Per-commit spec-to-production file ratios ranged from 0.24 to 0.35; the peak per-commit *line*
ratio was `23db4036` at 394 spec lines against 114 production `.ts` lines (3.46).

### 6.2 The amplification patterns, each with verified evidence

**1. Coupling to a collaborator instead of an observable contract.**
`332d4b8c`: `access-admin-unsaved-changes.guard.ts` changed **+1 / −2**. Its spec changed
**+369 / −25**. That is **131 spec lines per production line**, because the spec was written
against `window.confirm` rather than against "navigation is blocked until the user decides."

**2. Harness-selector coupling — pure test-side churn.**
`ac110949`: `abwab-page.component.spec.ts` changed **+6 / −2** while
`abwab-page.component.ts`, `.html` and `.scss` were **all untouched**. The edit swaps
`[data-testid="abwab-move-picker"] h3` for `[data-testid="abwab-move-picker-title"]` because the
shared modal shell took ownership of the heading. Zero behavior, non-zero cost.

**3. Deep TestBed provider re-mocking.** `abwab-page.component.spec.ts` — 2,181 lines, 104 `it()`
cases, **31 inline `provide:` blocks and 41 `vi.fn()` stubs**, each re-declaring the `AbwabApi`
surface and an `ActivatedRoute` stub. Adding one method to `AbwabApi` touches every block. Across
the corpus: 177 provider-mock overrides, 178 of 248 specs boot a `TestBed`.

**4. BEM class selectors as test handles.** Specs query component-private classes
(`.word-types-table__row`, `.qd-explorer-table__body`). Renaming a class in a stylesheet — exactly
what a design-system migration does — breaks tests whose DOM role, ARIA state and text are identical.

**5. Asserting composition rather than outcome.** In `3de3df49`, `root-details-panel.component.spec.ts`
turned *"renders a tablist with exactly the five tabs linked to a single tabpanel"* (a real
accessibility outcome) into *"composes the shared details workspace"* plus
`expect(host.querySelector('qd-details-workspace')).toBeTruthy()` — now failing if the panel is
rebuilt with equivalent behavior.

**6. Computed-CSS assertions inside jsdom.** A spec added where only `.scss` changed asserts
`getComputedStyle(trigger).position === 'relative'`. jsdom performs no layout, so this asserts
nothing about rendering — it is a second copy of the stylesheet, in TypeScript.

**7. Design-doctrine regression guards.** In `0d5e5a97` the same ~15-line "D35" assertion block was
pasted into 8 pre-existing specs. Negative assertions with no behavior behind them.

**8. Per-permutation table specs.** 165 `.each(` blocks corpus-wide. Every new table view, URL key,
label or permission code multiplies rows.

**9. Hand-built generated-DTO literals.** Fixtures construct complete generated contract objects
field-by-field, so any backend DTO reshape breaks every fixture naming them.

### 6.3 Backend amplification

The backend's amplification is structurally different — it comes from **parity locks**, not from
DOM coupling.

- `Smoke/SmokeRouteCatalog.cs` — 483 lines, 78 route literals, **17 commits**. Bidirectionally
  locked to the live `EndpointDataSource`, so *every* route addition anywhere in the API requires
  editing this file in the same change. High-value (it is the reason read tests are removable) but
  structurally amplifying.
- `TestSupport/Execution/test-gates.tsv` — 268 rows, **12 commits**. Every added, renamed, moved or
  deleted test class must be registered. Ironically, executing this audit's own recommendations
  will require ~152 edits to this one file.
- `Api/Access/AccessTestFixture.cs` (**14 commits**) and `Smoke/SmokeApiFixture.cs` (**12 commits**)
  — shared mounting points that features grow rather than compose beside, so one feature's fixture
  change ripples across an entire collection.
- `Smoke/SmokeAbwabWriteTests.cs` — largest backend test file (1,295 lines), **11 commits**; grows
  monotonically with the write surface, in addition to the route catalog.
- Verified zero-behavior sweep: `b9acdd45` ("protect all Abwab writes") changed 12 production files
  / 108 lines and 21 test files / **1,695 lines — a 15.7 : 1 test-to-production line ratio.**
- `478d4c01` ("consolidate global usings") edited 51 backend test files with no behavior change.

### 6.4 On the two-pass disagreement

The first classification pass returned 143 KEEP of 268 classes (53%). A second pass, argued from
the deletion criteria and given the Smoke-duplication evidence, overturned **141 of the 176**
KEEP/MERGE verdicts it re-examined (80%), yielding 48 KEEP (18%).

Neither pass is trustworthy alone — each was deliberately one-sided, and whichever ran last would
otherwise dominate. The reported verdicts are the second pass's, adopted only after a targeted
reconciliation: every class whose name contains a write, authorization, audit, rollback, schema,
concurrency, or corruption keyword and that ended as DELETE was re-examined individually. Six such
classes existed; all six were confirmed (§4.5) as duplicated at another layer or asserting
framework behavior. The retained `KEEP` set was separately checked for coverage of each protected
category named in the brief.

**Confidence statement.** The `KEEP — SECURITY` (25) and `KEEP — BUSINESS RULE` (23) sets are
high-confidence — they were argued for from both directions and survived. The boundary between
`DELETE` and `MERGE` within the explorer clusters is lower-confidence and largely a matter of how
much duplicated read content is worth relocating; it does not change the totals materially, since
both buckets leave the daily suite.

### 6.5 The ten highest-cost test areas

Ranked by measured maintenance cost (spec LOC × lifetime edits), with a judgment on whether the
cost is *justified* by what it protects.

| # | Area | Side | Files | LOC | Lifetime edits | Cost justified? |
|---|---|---|---:|---:|---:|---|
| 1 | `features/words` explorer pages + tables | FE | 93 | 24,515 | 234 | **No** — 4 page specs alone are ~5,220 LOC of near-identical explorer permutations |
| 2 | `features/abwab` (esp. `abwab-page.component.spec.ts`) | FE | 32 | 12,154 | 244 | **No** — 2,181-line spec, 33 commits, the most-committed file in the repo |
| 3 | `Api/Access` | BE | 26 | 6,185 | 77 | **Yes** — highest-cost backend area, but it is the authorization boundary |
| 4 | `features/access-admin` | FE | 18 | 5,492 | 56 | **No** — contains the 131:1 guard case; the real boundary is backend-tested |
| 5 | `features/mushaf` | FE | 43 | 9,209 | 31 | **Partly** — highest per-commit line ratio (3.46); segment-slicing logic is worth keeping |
| 6 | Route-catalog parity lock (`SmokeRouteCatalog` + `SmokeCoverageParityTests`) | BE | 2 | 707 | 21 | **Yes, with waste** — 18 of 78 route literals duplicated into a second hardcoded matrix |
| 7 | `shared/ui` primitives | FE | 31 | 4,423 | 50 | **No** — 43 new spec files added by the Golden convergence itself |
| 8 | `Quran/WordsWordTypes` | BE | 20 | 4,167 | 52 | **No** — read/paging/sort permutations duplicated by the Smoke tier |
| 9 | `Quran/WordsMorphologyExplorers` | BE | 15 | 4,486 | 30 | **No** — 116 `InlineData` rows of filter permutations |
| 10 | Access shared fixtures + `test-gates.tsv` registry | BE | 4 | ~1,000 | 31 | **Yes, structurally** — parity locks; cost is inherent to the design |

Rows 3, 6 and 10 are the important nuance: **high maintenance cost is not by itself a reason to
delete.** Those three protect the authorization boundary and the catalog integrity that makes
everything else removable.

---

## 7. Final decision summary

### Frontend

| Metric | Scenario A (policy-strict) | Scenario B (recommended) |
|---|---:|---:|
| Current test files | 248 | 248 |
| Current test cases | 3,371 | 3,371 |
| Files to delete | **248** | **178** |
| Cases removed | **3,371** | **2,442** |
| Replacement E2E flows required | **18** (5 auth-blocked) | **14** (5 auth-blocked) |
| Remaining test files | **0** | **70** |
| Remaining test cases | 0 | 929 |
| Spec LOC removed | 60,247 | 52,806 |
| Gate wall time | 389.9 s → **38.0 s** | 389.9 s → **~50 s** (estimated) |

Scenario B's gate is an estimate, not a measurement: the 70 retained specs execute in 1.6 s but
still pay Vitest startup, and no existing lane selects exactly that set.

Existing Playwright E2E (17 files / 61 cases) is retained unchanged in both scenarios.

### Backend

| Metric | Value |
|---|---:|
| Current test files (`.cs` carrying test attributes) | **257** |
| Current test classes | **268** |
| Current test cases (executed) | **2,258** |
| `KEEP — BUSINESS RULE` | **23 classes / 194 cases** |
| `KEEP — SECURITY` | **25 classes / 251 cases** |
| `MERGE` | **23 classes / 229 cases** (folded into keepers) |
| `DELETE` | **129 classes / 1,142 cases** |
| `RELEASE_OR_CHANGE_GATE` | **68 classes / 438 cases** (retained, removed from daily suite) |
| Estimated remaining — daily suite | **48 classes / 445–674 cases** |
| Estimated remaining — total files retained | **116 classes** (48 daily + 68 gated) |
| Daily-suite runtime (estimated) | 429.6 s → **~60–90 s** — the 371.1 s `canonical-data` lane moves behind a trigger; not directly measurable until the split exists |

### Whole repository

Units: frontend `*.spec.ts` files + backend test classes; cases are executed cases.

| Metric | Scenario A | Scenario B |
|---|---:|---:|
| Current total test files/classes | **516** (248 + 268) | 516 |
| Current total test cases | **5,629** (3,371 + 2,258) | 5,629 |
| Proposed removed — files/classes | **400** (248 FE + 152 BE) | **330** (178 FE + 152 BE) |
| Proposed removed — cases | **4,513** (3,371 + 1,142) | **3,584** (2,442 + 1,142) |
| Remaining — total retained | **116** | **186** |
| Remaining — running daily | **48** | **118** |
| Remaining — daily cases | **~445** | **~1,374** |
| **Reduction in retained units** | **77.5%** | **63.9%** |
| **Reduction in units running daily** | **90.7%** | **77.1%** |
| **Reduction in cases running daily** | **92.1%** | **75.6%** |
| Test LOC removed | **83,004** of 104,056 | **75,563** of 104,056 |

Test-estate context weight falls from ~1.23 M tokens (616 files, 118,017 LOC including support and
E2E) by roughly 700–780 k tokens.

---

## 8. Sequencing risks the implementation plan must respect

Recorded as constraints, not as a plan.

1. **The E2E auth mechanism is the hard prerequisite.** Five of the eighteen proposed flows, and
   all migration of the 1,063 Abwab/access-admin spec cases, are blocked until an approved
   authenticated persona exists for Playwright. Nothing in `features/abwab` or
   `features/access-admin` should be deleted before it lands.
2. **Backend deletion must precede or accompany `test-gates.tsv` edits.** The catalog is parity-
   locked in both directions; removing 152 classes without updating the TSV fails the suite by name,
   and vice versa.
3. **`RELEASE_OR_CHANGE_GATE` needs a trigger mechanism before those 68 classes leave the daily
   lane.** Until one exists, moving them out removes protection rather than deferring it. The
   existing `Backend/scripts/test-backend` lane selectors and the `Concerns` column in the catalog
   are the natural seam.
4. **The suite's own flakiness is independent of this work.** The `inotify` exhaustion (§3.3) will
   persist for whatever remains; reducing host-booting tests relieves but does not by itself fix it.
5. **`TESTING_STRATEGY.md` §11 and §12 contradict the locked policy** and must be rewritten in the
   same change that implements it, along with `testing/README.md`, the ten `angular.json` test
   configurations, `testing/verify-test-gates.mjs`, and the `test:*` / `test:pre-pr` scripts.

---

## 9. Evidence index

All figures reproducible from the tree at `0d5e5a97`.

| Claim | Source |
|---|---|
| FE 248 files / 3,371 cases / 337.8 s | `npm test`, clean run, 2026-08-10 |
| FE per-area runtime, TestBed split | per-file durations parsed from the same run |
| FE replacement gate 38.0 s | `npm run typecheck:app`, `npm run build:verify` |
| BE 268 classes | `TestSupport/Execution/test-gates.tsv` (268 data rows, parity-enforced) |
| BE 1,507 declared methods | attribute scan incl. 33 custom source-gated attributes |
| BE 2,258 cases / 429.6 s | `Backend/scripts/test-backend pre-pr`, clean run |
| BE lane runtimes | five independent `test-backend <lane>` invocations |
| Suite flakiness + root cause | clean `pre-pr` log; `fs.inotify.max_user_instances` = 128 |
| Golden UI churn | `git diff --numstat 87834abe~1 0d5e5a97` |
| 131:1 guard case | `git show --numstat 332d4b8c` |
| Pure test-side churn | `git show --numstat ac110949 -- .../abwab-page/` |
| 15.7:1 backend ratio | `git show --numstat b9acdd45` |
| Lifetime edit rates | `git log --name-only` over `Frontend/.../src` and `Backend/tests` |
| E2E has no authenticated persona | `grep -rn "Authorization\|Bearer" e2e/` → no matches; `e2e/fixtures/logto.ts` |
| 78 of ~85 routes swept | `SmokeRouteCatalog.cs` route literals vs `[Http*]` attributes in `Backend/api` |
| Audit A baseline | `docs/project-simplification-audit/data/*.json` at `72792ba9` |
