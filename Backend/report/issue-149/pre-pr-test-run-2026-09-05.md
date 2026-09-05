# Pre-PR Test Run Report

Generated from a single `node scripts/test pre-pr` run on the `149-persistent-test-database` integration branch at commit `c30800e6` (ticket #169, *Contract the artifact/container lifecycle*), executed 2026-09-05 against the provisioned `quran_dashboard_test` database on `/var/run/postgresql`.

## Result

- **61 backend test classes**, **580 tests**, **0 failures**
- `backend-build`, `frontend-pre-pr`, `playwright-typecheck`: passed
- Canonical critical Playwright journeys: **6 passed, 0 failed** (verified separately after two oracle corrections)

## Did anything create a database?

**No. This run created zero databases.** Every database-touching class shared the one persistent `quran_dashboard_test`. No scratch databases (`quran_test_scratch_*`) were created, no PostgreSQL containers were started, and no dump was restored — which is the outcome the #149 programme set out to achieve.

| Database behaviour | Classes |
|---|---:|
| No database | 27 |
| Persistent `quran_dashboard_test` — writes + reset | 27 |
| Persistent `quran_dashboard_test` — read only | 7 |

For contrast, the policy registry holds **78 classes targeting `EmptyScratch`** — those *do* get a freshly created, migrated, empty scratch database created and reaped per class — and **1 targeting `FullRehearsal`**. None were selected here, because pre-PR selection is driven by affected scope rather than by the existence of a pipeline.

## Groups

| Group | Classes | What it means |
|---|---:|---|
| MutableWriter | 28 | Writes the persistent test database, then resets Mutable Application State |
| FastNoDb | 27 | No database at all — pure in-memory rules and contracts |
| GuardedReader | 5 | Reads the persistent test database under a lock |
| CanonicalReader | 1 | Reads canonical data from the persistent test database |

## Timing

Per-test execution time was captured for 27 of 61 classes (the two vstest output formats differ; the `Duration:` field is only present in one).

- Measured in-test execution across those 27 classes: **26.0 s**
- Slowest measured class: **Api.PhraseSearch.PhraseSearchComputePipelineTests** at 11.0 s
- Fastest measured class: **Smoke.SmokeRouteBaselineTests** at 59 ms

**Wall time was roughly 45 minutes for the backend stage.** The gap between that and the seconds of actual test execution is process overhead: `pre-pr` runs **one class per process**, so each class pays a fresh `dotnet test` startup, xUnit discovery, fixture construction, database lock acquisition and Protected State fingerprinting. Observed cost was roughly 40-100 s per MutableWriter class against in-test durations measured in milliseconds.

> **Input for ticket #170.** #170 sets a **12-minute active pre-PR gate** target and requires lock wait, capability provisioning, active gate time and total wall time to be reported *separately*. This run shows why that separation is the substantive part of the ticket rather than bookkeeping: active test time is a small fraction of wall time, and the dominant cost is per-class process startup. For comparison, the `gate-contract` lane this replaced ran ~27 minutes by batching hundreds of tests into two shards.

## Every test class in this run

In execution order. **Reads** and **Writes** are the declared data effects from the policy registry; **Database** is what the class actually touches. `—` in Duration means that class used the vstest output format that omits the field.

| # | Class | Group | Feature | Tests | Duration | Database | Reads | Writes |
|---:|---|---|---|---:|---:|---|---|---|
| 1 | `Api.Access.AbwabPermissionCatalogueTests` | FastNoDb | Access | 4 | 94 ms | No database | None | None |
| 2 | `Api.Access.AuthorizationBoundaryTests` | FastNoDb | Access | 3 | 492 ms | No database | None | None |
| 3 | `Api.Access.EmailIdentityNormalizerTests` | FastNoDb | Access | 12 | 114 ms | No database | None | None |
| 4 | `Api.Access.OwnerBootstrapOptionsTests` | FastNoDb | Access | 4 | 66 ms | No database | None | None |
| 5 | `Api.Access.UnsafeEndpointMetadataValidatorTests` | FastNoDb | Access | 12 | 123 ms | No database | None | None |
| 6 | `Api.Middleware.GlobalExceptionHandlerTests` | FastNoDb | Middleware | 2 | 145 ms | No database | None | None |
| 7 | `Api.PhraseSearch.PhraseSearchComputePipelineTests` | FastNoDb | PhraseSearch | 11 | 11.00 s | No database | None | None |
| 8 | `Api.PhraseSearch.PhraseSearchConditionalRequestTests` | FastNoDb | PhraseSearch | 1 | 2.00 s | No database | None | None |
| 9 | `Api.RateLimiting.RateLimitingIntegrationTests` | FastNoDb | RateLimiting | 10 | 4.00 s | No database | None | None |
| 10 | `Api.Testing.DatabaseActivityPolicyTests` | FastNoDb | ApiBehavior | 13 | 1.00 s | No database | None | None |
| 11 | `Quran.MushafReader.AyahStudyCorruptCoveredAyahKeysTests` | FastNoDb | MushafReader | 1 | 80 ms | No database | None | None |
| 12 | `Quran.MushafReader.QuranFidelityOracleContractTests` | FastNoDb | MushafReader | 1 | 99 ms | No database | None | None |
| 13 | `Quran.MushafReader.WordAnalysisCorruptFeaturesJsonTests` | FastNoDb | MushafReader | 1 | 80 ms | No database | None | None |
| 14 | `Quran.MushafReader.WordAnalysisSegmentFallbackTests` | FastNoDb | MushafReader | 1 | 69 ms | No database | None | None |
| 15 | `Quran.Navigation.NavigationMetadataWriteIsolationTests` | FastNoDb | Navigation | 8 | 96 ms | No database | None | None |
| 16 | `Quran.WordsMorphology.MorphologyAssemblerTests` | FastNoDb | WordsMorphology | 30 | 219 ms | No database | None | None |
| 17 | `Quran.WordsMorphology.WordLemmaNormalizationApplierTests` | FastNoDb | WordsMorphology | 10 | 187 ms | No database | None | None |
| 18 | `Quran.WordsWordTypes.WordTypesChildCatalogueDriftTests` | FastNoDb | WordsWordTypes | 17 | 136 ms | No database | None | None |
| 19 | `Smoke.SmokeRouteBaselineTests` | FastNoDb | Smoke | 2 | 59 ms | No database | None | None |
| 20 | `TestRuntime.TestRuntimeCommandTests` | FastNoDb | ApiBehavior | 39 | 2.00 s | No database | None | None |
| 21 | `TestRuntime.TestRuntimeFullRehearsalTests` | FastNoDb | ApiBehavior | 9 | 1.00 s | No database | None | None |
| 22 | `TestSupport.Access.TestAccessPersonasContractTests` | FastNoDb | Access | 4 | 170 ms | No database | None | None |
| 23 | `TestSupport.DependencyInjection.OwnedServiceProviderRegistryTests` | FastNoDb | ApiBehavior | 4 | 73 ms | No database | None | None |
| 24 | `TestSupport.Execution.TestGateCatalogTests` | FastNoDb | ApiBehavior | 12 | 362 ms | No database | None | None |
| 25 | `TestSupport.Execution.TestPolicyContractTests` | FastNoDb | ApiBehavior | 12 | 272 ms | No database | None | None |
| 26 | `TestSupport.Process.ProcessExecutionTests` | FastNoDb | ApiBehavior | 4 | 2.00 s | No database | None | None |
| 27 | `TestSupport.Process.ProcessGlobalStateScopeTests` | FastNoDb | ApiBehavior | 5 | 63 ms | No database | None | None |
| 28 | `Quran.MushafReader.QuranFidelityOracleTests` | CanonicalReader | MushafReader | 2 | — | persistent test DB — read only | CanonicalQuranData | None |
| 29 | `Smoke.Data.SmokeDataReadTests` | GuardedReader | Smoke | 15 | — | persistent test DB — read only | CanonicalQuranData,MutableApplicationState | None |
| 30 | `Smoke.SmokeAuthPipelineReadTests` | GuardedReader | Smoke | 3 | — | persistent test DB — read only | None | None |
| 31 | `Smoke.SmokeCoverageParityTests` | GuardedReader | Smoke | 5 | — | persistent test DB — read only | None | None |
| 32 | `Smoke.SmokeReadOnlyBootGuardTests` | GuardedReader | Smoke | 3 | — | persistent test DB — read only | SystemCatalogue,SchemaState | None |
| 33 | `Smoke.SmokeRoutePipelineTests` | GuardedReader | Smoke | 62 | — | persistent test DB — read only | CanonicalQuranData,SystemCatalogue,MutableApplicationState,SchemaState | None |
| 34 | `Abwab.AbwabCollectionResetContractTests` | MutableWriter | Abwab | 3 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState,SchemaState | MutableApplicationState |
| 35 | `Abwab.AbwabDoorWriteBehaviorTests` | MutableWriter | Abwab | 39 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 36 | `Abwab.AbwabRelationWriteBehaviorTests` | MutableWriter | Abwab | 2 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 37 | `Abwab.AbwabSchemaTests` | MutableWriter | Abwab | 14 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState,SchemaState | MutableApplicationState |
| 38 | `Abwab.AbwabTemplateApplyBehaviorTests` | MutableWriter | Abwab | 2 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 39 | `Api.Abwab.AbwabInclusionProjectionTests` | MutableWriter | Abwab | 7 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState,CanonicalQuranData | MutableApplicationState |
| 40 | `Api.Access.AccessAdministrationEndpointTests` | MutableWriter | Access | 17 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 41 | `Api.Access.AccessAuditEventPersistenceTests` | MutableWriter | Access | 19 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 42 | `Api.Access.AccessCollectionResetContractTests` | MutableWriter | Access | 3 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState,SchemaState | MutableApplicationState |
| 43 | `Api.Access.AccessMeEndpointTests` | MutableWriter | Access | 11 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 44 | `Api.Access.AccessRolesTests` | MutableWriter | Access | 20 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 45 | `Api.Access.AuthorizationPipelineTests` | MutableWriter | Access | 7 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 46 | `Api.Access.AuthorizationRejectionResponseTests` | MutableWriter | Access | 9 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 47 | `Api.Access.AuthorizationRequirementHandlerTests` | MutableWriter | Access | 18 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 48 | `Api.Access.AuthorizationStateResolverTests` | MutableWriter | Access | 4 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 49 | `Api.Access.DeviceSessionLifecycleTests` | MutableWriter | Access | 4 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 50 | `Api.Access.EmailIdentityPreflightTests` | MutableWriter | Access | 2 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 51 | `Api.Access.LogtoSubjectRelinkEndpointTests` | MutableWriter | Access | 8 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 52 | `Api.Access.OwnerReconciliationServiceTests` | MutableWriter | Access | 14 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 53 | `Api.Access.UserProvisioningServiceTests` | MutableWriter | Access | 12 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 54 | `Api.Linking.LinkingCollectionResetContractTests` | MutableWriter | Linking | 1 | — | persistent test DB — writes + reset | CanonicalQuranData,SystemCatalogue,MutableApplicationState,SchemaState | MutableApplicationState |
| 55 | `Api.Linking.LinkingConfirmationIdempotencyTests` | MutableWriter | Linking | 1 | — | persistent test DB — writes + reset | CanonicalQuranData,SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 56 | `Api.Linking.LinkingRecoveryAndAtomicityTests` | MutableWriter | Linking | 12 | — | persistent test DB — writes + reset | CanonicalQuranData,SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 57 | `Api.Linking.LinkingSuccessfulJourneyTests` | MutableWriter | Linking | 1 | — | persistent test DB — writes + reset | CanonicalQuranData,SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 58 | `Smoke.SmokeAbwabWriteAuthorizationTests` | MutableWriter | Smoke | 23 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 59 | `Smoke.SmokeAccessAdministrationAuthorizationTests` | MutableWriter | Smoke | 1 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 60 | `Smoke.SmokeAuthPipelineTests` | MutableWriter | Smoke | 2 | — | persistent test DB — writes + reset | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 61 | `Smoke.SmokeMutableBootGuardTests` | MutableWriter | Smoke | 2 | — | persistent test DB — read only | SystemCatalogue,SchemaState | None |

## Defects this verification found

The worker that implemented #169 reported the ticket green, and its own two-axis code review passed it.
Independent verification found four issues it did not catch. Three were pre-existing; one was a real
regression introduced by the ticket.

### 1. `scripts/test` never set the run id for MutableWriter selections — a #169 regression

`scripts/test` set `QURAN_DASHBOARD_TEST_RUN_ID` on the empty-scratch and full-rehearsal paths only. Plain
MutableWriter selections called `executeCommand(command)` with no environment, so every MutableWriter class
threw at fixture construction:

```
QURAN_DASHBOARD_TEST_RUN_ID is required for MutableWriter tests.
Use the repository scripts/test runner instead of direct dotnet test execution.
```

Before the contraction, `Backend/scripts/test-backend` generated and exported the run id itself. #169 moved
ownership to the root runner but wired only two of the three execution paths, so **the supported runner
could not run 34 of the 61 classes it selects**. Fixed in `7c9fd0c3`.

The worker's verification missed this because it used `--dry-run` and focused FastNoDb classes, which never
construct a MutableWriter fixture. Only a real full `pre-pr` surfaces it.

### 2. `study.tafsir.isGroupLeader` — pre-existing bad oracle

`test-oracles/quran-fidelity.json` asserted `isGroupLeader: true` for `ar-muyassar` 1:1, which the product
has never produced. `IsGroupLeader` is true only when an entry covers more than one ayah — `TafsirAssembler`
derives it as `valueKind == ValueKindLeader`, and that is set only when `coveredKeys.Length > 1`. The
canonical source entry for 1:1 carries a `text` field and nothing else, so it covers itself alone. The
source has exactly 625 entries carrying `ayahKeys`, and the imported data has exactly 625 group leaders.

The old value also contradicted three of its own sibling fields: `sourceValueKind` flat, `coveredAyahCount`
1, and `coveredAyahKeys` `["1:1"]` each entail false. Introduced by `d1abb96f` (#157) under an
`"authority": "source-review"` label — the same commit that introduced the fabricated `21294` unique-tashkeel
count later corrected by `6838982e`. Fixed in `c5c60a5e`.

### 3. PhraseSearch read-path oracle — pre-existing, two wrong expectations

`test-oracles/phrase-search.json` carried two values that had never matched the product. Both were verified
against the live repetitions endpoint and the active index before changing (fixed in `0ff80b9f`):

- `repetitions.displayText` had a spurious alef (`الرحمان`). That spelling appears in **none** of the 106,141
  indexed phrase variants. The repetitions list renders the *simple* text mode — the page default — and the
  endpoint returns `بسم الله الرحمن الرحيم` with `occurrenceCount` 2 across 1:1 and 27:30.
- `similarity.verseKeys` listed only 1:1, 27:30 and 11:41. At `maximumDifferences` 2 the anchor has **four**
  similarity edges, not one. The previous list captured only the edge differing at positions `{3,4}` (11:41)
  and omitted three differing at `{1,2}` — 2:163, 41:2 and 59:22 — each sharing exactly two of the four
  words. The anchor's own two ayahs plus those four are the six the results summary reports.

Introduced by `c732e534` (#165).

### 4. `pre-pr` invalidates its own Playwright provisioning

`pre-pr` runs `frontend-pre-pr`, which rebuilds Angular, **after** the controlled provisioning receipt is
written. That changes the `frontendBuild` hash the receipt pins, so the Playwright stage then refuses to run:

```
Controlled provisioning output frontendBuild has changed; provision again.
```

Provision before `pre-pr` and the build invalidates it; provision after and the stage has already failed. So
the supported command cannot complete its own Playwright stage in a single invocation. **Not yet fixed** —
this is a genuine gap against #169's criterion that the root runner be authoritative for ordinary paths, and
a natural candidate for #170.

### Also worth fixing

`scripts/verify-artifact-container-contraction.mjs` asserts on **directory existence**, so a stale untracked
`Backend/tools/QuranDashboard.TestArtifacts/{bin,obj}` fails it for any developer who built before the
cutover, even with zero tracked files remaining. Checking tracked files instead would be robust. A fresh
clone is unaffected.

## How to reproduce

```bash
ConnectionStrings__QuranDashboardTest='Host=/var/run/postgresql;Database=quran_dashboard_test;Username=mohamed' \
  node scripts/test pre-pr
```

The Playwright stage needs provisioning, and because of finding 4 it must be run *after* the frontend build:

```bash
cd Frontend/quran-dashboard-ui
npm run e2e:provision
npm run e2e:canonical:critical
```

## Sources

- Run log: `pre-pr-run5.log`, 2026-09-05, commit `c30800e6`
- Declared policy: `Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv`
- Fixture policy: `Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-resources.tsv`

Test counts, durations, pass/fail and execution order are measured from the run log. Feature, group, gate,
concerns, reads, writes and database target are the declared values from the policy registry. Nothing in
this report is inferred from class names.
