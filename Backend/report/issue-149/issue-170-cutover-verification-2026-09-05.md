# Issue #170 — Verify the atomic cutover and risk-based gates

Verification record for ticket #170 of programme #149, produced on the
`149-persistent-test-database` integration branch (parent commit `c39ece48`) on 2026-09-05 against the
provisioned `quran_dashboard_test` database on `/var/run/postgresql`.

The evidence below comes from **one** `node scripts/test pre-pr` invocation, run start to finish with no
manual step in the middle and with **no full Rehearsal Database provisioned on this machine**.

## Result

- **61 backend test classes**, **580 tests**, **0 failures**
- All 67 planned commands exited 0 (0 failing commands)
- Canonical critical Playwright journeys: **6 passed**
- Stateful critical Playwright journeys: **8 passed**
- **Zero databases created.** No `quran_test_scratch_*` database, no container, no dump restore

## The four reported times

| Reported time | This run | Counts toward the 12-minute target? |
|---|---:|---|
| Advisory-lock wait | 0.0 s | No |
| Capability / manual provisioning | 1 m 17.1 s | No |
| **Active gate** | **82 m 26.4 s** | **Yes — this is the only figure the target measures** |
| Total wall time | 83 m 43.5 s | — |
| Unattributed | 0.0 s | No |

`activeGateTarget.withinTarget` = **false** against a
target of 12 m 00.0 s.

Lock wait is 0.0 s because ordinary pre-PR selection contains no
empty-scratch or full-data rehearsal lane, so the runner starts no TestRuntime keeper of its own. The
locks the Backend writer lanes and the controlled Playwright lanes take are acquired *inside* those child
processes and are reported by their own evidence records — the runner aggregates only the waits it can
observe, and the documentation says so rather than implying full coverage.

## The active gate is over target, and that is the honest number

The active gate measured 82 m 26.4 s against a 12-minute target. Two costs
dominate, and neither is a regression introduced here:

1. **The Backend block, 41 m 52.9 s across 28 MutableWriter
   classes.** Pre-PR runs **one test class per process**, so each class pays a fresh `dotnet test` startup,
   xUnit discovery, fixture construction, advisory-lock acquisition, Protected State fingerprinting and a
   verified reset — against in-test durations measured in milliseconds.
2. **The stateful Playwright lane, 34 m 32.4 s.** It runs 8 critical journeys, each with its own
   verified reset and a fresh Mutable API lifecycle.

These figures move with machine load. An earlier complete run of the same commands on the same commit
measured 47 m 17 s of active gate time, with the stateful lane at roughly 9 minutes when run on an idle
machine. Both runs are far outside 12 minutes; the variance changes the size of the gap, not the verdict.

The architecture is explicit that this figure is "measured rather than allowed to weaken correctness or
safety". It is reported here as measured. Closing the gap is a scheduling change (batching classes that
share a fixture into one process, and parallelising journeys that do not contend), not a correctness or
safety change, and it is deliberately **not** attempted under this ticket.

## Groups

| Group | Classes | Elapsed | Database |
|---|---:|---:|---|
| MutableWriter | 28 | 41 m 52.9 s | `quran_dashboard_test` (write + reset) |
| FastNoDb | 27 | 2 m 06.8 s | No database |
| GuardedReader | 5 | 1 m 37.5 s | `quran_dashboard_test` (guarded read) |
| CanonicalReader | 1 | 12.7 s | `quran_dashboard_test` (read) |

## Non-class commands

| Command | Phase | Elapsed | Result |
|---|---|---:|---|
| `backend-build` | activeGate | 15.0 s | passed |
| `frontend-pre-pr` | activeGate | 32.7 s | passed |
| `playwright-typecheck` | activeGate | 2.6 s | passed |
| `playwright-provision` | provisioning | 1 m 17.1 s | passed |
| `playwright-canonical-critical` | activeGate | 1 m 13.7 s | passed |
| `playwright-stateful-critical` | activeGate | 34 m 32.4 s | passed |

`playwright-provision` is the command this ticket added. It is planned deliberately **after**
`backend-build` and `frontend-pre-pr` — both of which rebuild outputs the controlled receipt hashes — and
**before** the two controlled Playwright lanes that validate that receipt. Before this change the
supported command could not complete its Playwright stage in a single invocation: provisioning first was
invalidated by the builds, and provisioning after meant the stage had already failed.

## Did anything create a database?

**No.** Every database-touching class shared the one persistent `quran_dashboard_test`.

For contrast, the policy registry holds **78 classes targeting `EmptyScratch`** — those do
get a freshly created, migrated, empty scratch database per class — and **1 targeting
`FullRehearsal`**. None were selected, because pre-PR selection is driven by affected scope rather than by
the existence of a pipeline.

## Acceptance criteria

| # | Criterion | Evidence |
|---:|---|---|
| 1 | Supported focused selections run through TestRuntime with the expected target, role, profile, lock mode, reset lifecycle and evidence | `scripts/verify-test-policy-runner.mjs` passes; every planned command now declares a phase and the verifier asserts it |
| 2 | FastNoDb independently runnable; reader partitions keep approved safe parallelism | 27 FastNoDb classes ran with no database; partition order unchanged |
| 3 | Required pre-PR risk gates pass against `quran_dashboard_test` and eligible scratch databases | This run: all 67 commands exit 0 |
| 4 | Unrelated full-data pipelines do not run merely because they exist | 0 `FullDataDestructiveRehearsal` commands planned; asserted in `verify-artifact-container-contraction.mjs` |
| 5 | Affected scope selects its rehearsal; full-data stays separately authorized | Verifier asserts affected-feature and affected-concern selection, and `authorizationRequired` withholding |
| 6 | Ordinary cutover passes with no full Rehearsal Database; selecting the absent capability fails only that lane with actionable output | This run passed with none provisioned. A focused `--authorize-full-data` selection exits 3 and prints `capabilityState=capability-missing`, the violation code, TestRuntime's provisioning guidance, and "Only this lane failed" |
| 7 | Protected State matches before and after mutating verification; Mutable state finishes clean; no dump retained | Stateful reset evidence reports `protectedStateMatches: true` with identical before/after/expected fingerprints, `apiProcessAlive: false`, `activeDatabaseConnections: 0` |
| 8 | Lock wait, provisioning, active gate and total wall time reported separately; the 12-minute target applies only to active gate time | The `test-execution-timing` record above |
| 9 | No supported artifact, Testcontainers, clone-local or automatic rebuild fallback remains | `verify-artifact-container-contraction.mjs` passes; repository search finds Testcontainers only in retired-decision documentation and one synthetic advisory-contract fixture |
| 10 | Integration fixes stay within the accepted architecture | The four fixes below add no lifecycle; the retired one is not reintroduced |

## Fixes this ticket made

1. **`scripts/test-policy-runner.mjs` / `scripts/test` — separated timing.** New `EXECUTION_PHASES`,
   `PRE_PR_ACTIVE_GATE_TARGET_MILLISECONDS` and `summarizeExecutionTiming()`. Lock wait is taken from the
   `advisoryLock.waitMilliseconds` the keeper itself reports, not from how long the keeper process took to
   start, so process startup and contract inspection are not miscounted as contention. Capability
   validation (`rehearsal hold` recomputing the Protected State fingerprint) is reported as provisioning
   even when the command belongs to the active gate.

2. **`scripts/test-policy-runner.mjs` — the provisioning-order defect** carried over from #169, described
   above.

3. **`scripts/verify-artifact-container-contraction.mjs` — the directory-existence papercut** carried over
   from #169. The contraction is a repository-content claim, so retired paths are now asserted against
   `git ls-files` rather than `existsSync`. Proven by creating a stale untracked
   `Backend/tools/QuranDashboard.TestArtifacts/obj/stale.cache`: the verifier still passes, where before it
   failed for any developer who had built before the cutover.

4. **`Frontend/quran-dashboard-ui/scripts/verify-independent-playwright-oracles.mjs`** restated the
   PhraseSearch similarity set as three verse keys. Commit `0ff80b9f` (#169) corrected the oracle to the
   reviewed six without updating this independent verifier, so `frontend-pre-pr` failed. The verifier now
   restates the reviewed set. This was found by running the gate, not by inspection.

## Known limitations, recorded rather than hidden

- **Active gate is 82 m 26.4 s, not under 12 minutes.** Cause, run-to-run
  variance, and the reason closing the gap is out of scope are above.
- **In-child lock waits are not aggregated.** A lock a child process holds for its own lifetime is
  reported by that child's evidence record, not by the runner's summary.
- **The contraction verifier now permits untracked residue** at a retired path. That is deliberate: an
  untracked file is the developer's own build artifact, not something this repository ships.
- **`pre-pr` has no resume.** Every invocation restarts all 61 classes.

## How to reproduce

```bash
export ConnectionStrings__QuranDashboardTest='Host=/var/run/postgresql;Database=quran_dashboard_test;Username=mohamed'
node scripts/test pre-pr
```

The final line of output is the `test-execution-timing` JSON record quoted above. It is emitted after a
failing command too, so a failed gate still reports where its time went.

## Every test class in this run

In execution order. **Reads** and **Writes** are the declared data effects from the policy registry;
**Database** is what the class actually touches. `—` in Tests means that class used the vstest output
format that omits the count.

| # | Class | Group | Feature | Tests | Elapsed | Database | Reads | Writes |
|---:|---|---|---|---:|---:|---|---|---|
| 1 | `Api.Access.AbwabPermissionCatalogueTests` | FastNoDb | Access | 4 | 4.1 s | No database | None | None |
| 2 | `Api.Access.AuthorizationBoundaryTests` | FastNoDb | Access | 3 | 4.1 s | No database | None | None |
| 3 | `Api.Access.EmailIdentityNormalizerTests` | FastNoDb | Access | 12 | 4.0 s | No database | None | None |
| 4 | `Api.Access.OwnerBootstrapOptionsTests` | FastNoDb | Access | 4 | 4.0 s | No database | None | None |
| 5 | `Api.Access.UnsafeEndpointMetadataValidatorTests` | FastNoDb | Access | 12 | 3.6 s | No database | None | None |
| 6 | `Api.Middleware.GlobalExceptionHandlerTests` | FastNoDb | Middleware | 2 | 4.3 s | No database | None | None |
| 7 | `Api.PhraseSearch.PhraseSearchComputePipelineTests` | FastNoDb | PhraseSearch | 11 | 15.2 s | No database | None | None |
| 8 | `Api.PhraseSearch.PhraseSearchConditionalRequestTests` | FastNoDb | PhraseSearch | 1 | 7.5 s | No database | None | None |
| 9 | `Api.RateLimiting.RateLimitingIntegrationTests` | FastNoDb | RateLimiting | 10 | 10.7 s | No database | None | None |
| 10 | `Api.Testing.DatabaseActivityPolicyTests` | FastNoDb | ApiBehavior | 13 | 7.0 s | No database | None | None |
| 11 | `Quran.MushafReader.AyahStudyCorruptCoveredAyahKeysTests` | FastNoDb | MushafReader | 1 | 3.9 s | No database | None | None |
| 12 | `Quran.MushafReader.QuranFidelityOracleContractTests` | FastNoDb | MushafReader | 1 | 3.5 s | No database | None | None |
| 13 | `Quran.MushafReader.WordAnalysisCorruptFeaturesJsonTests` | FastNoDb | MushafReader | 1 | 3.5 s | No database | None | None |
| 14 | `Quran.MushafReader.WordAnalysisSegmentFallbackTests` | FastNoDb | MushafReader | 1 | 3.3 s | No database | None | None |
| 15 | `Quran.Navigation.NavigationMetadataWriteIsolationTests` | FastNoDb | Navigation | 8 | 3.0 s | No database | None | None |
| 16 | `Quran.WordsMorphology.MorphologyAssemblerTests` | FastNoDb | WordsMorphology | 30 | 3.1 s | No database | None | None |
| 17 | `Quran.WordsMorphology.WordLemmaNormalizationApplierTests` | FastNoDb | WordsMorphology | 10 | 3.2 s | No database | None | None |
| 18 | `Quran.WordsWordTypes.WordTypesChildCatalogueDriftTests` | FastNoDb | WordsWordTypes | 17 | 3.0 s | No database | None | None |
| 19 | `Smoke.SmokeRouteBaselineTests` | FastNoDb | Smoke | 2 | 3.0 s | No database | None | None |
| 20 | `TestRuntime.TestRuntimeCommandTests` | FastNoDb | ApiBehavior | 39 | 5.5 s | No database | None | None |
| 21 | `TestRuntime.TestRuntimeFullRehearsalTests` | FastNoDb | ApiBehavior | 9 | 5.4 s | No database | None | None |
| 22 | `TestSupport.Access.TestAccessPersonasContractTests` | FastNoDb | Access | 4 | 3.2 s | No database | None | None |
| 23 | `TestSupport.DependencyInjection.OwnedServiceProviderRegistryTests` | FastNoDb | ApiBehavior | 4 | 3.3 s | No database | None | None |
| 24 | `TestSupport.Execution.TestGateCatalogTests` | FastNoDb | ApiBehavior | 12 | 3.6 s | No database | None | None |
| 25 | `TestSupport.Execution.TestPolicyContractTests` | FastNoDb | ApiBehavior | 12 | 3.2 s | No database | None | None |
| 26 | `TestSupport.Process.ProcessExecutionTests` | FastNoDb | ApiBehavior | 4 | 5.6 s | No database | None | None |
| 27 | `TestSupport.Process.ProcessGlobalStateScopeTests` | FastNoDb | ApiBehavior | 5 | 3.0 s | No database | None | None |
| 28 | `Quran.MushafReader.QuranFidelityOracleTests` | CanonicalReader | MushafReader | 2 | 12.7 s | `quran_dashboard_test` (read) | CanonicalQuranData | None |
| 29 | `Smoke.Data.SmokeDataReadTests` | GuardedReader | Smoke | 15 | 51.1 s | `quran_dashboard_test` (guarded read) | CanonicalQuranData,MutableApplicationState | None |
| 30 | `Smoke.SmokeAuthPipelineReadTests` | GuardedReader | Smoke | 3 | 9.1 s | `quran_dashboard_test` (guarded read) | None | None |
| 31 | `Smoke.SmokeCoverageParityTests` | GuardedReader | Smoke | 5 | 8.8 s | `quran_dashboard_test` (guarded read) | None | None |
| 32 | `Smoke.SmokeReadOnlyBootGuardTests` | GuardedReader | Smoke | 3 | 10.1 s | `quran_dashboard_test` (guarded read) | SystemCatalogue,SchemaState | None |
| 33 | `Smoke.SmokeRoutePipelineTests` | GuardedReader | Smoke | 62 | 18.4 s | `quran_dashboard_test` (guarded read) | CanonicalQuranData,SystemCatalogue,MutableApplicationState,SchemaState | None |
| 34 | `Abwab.AbwabCollectionResetContractTests` | MutableWriter | Abwab | 3 | 1 m 27.5 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState,SchemaState | MutableApplicationState |
| 35 | `Abwab.AbwabDoorWriteBehaviorTests` | MutableWriter | Abwab | 39 | 1 m 44.5 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 36 | `Abwab.AbwabRelationWriteBehaviorTests` | MutableWriter | Abwab | 2 | 1 m 24.6 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 37 | `Abwab.AbwabSchemaTests` | MutableWriter | Abwab | 14 | 1 m 31.7 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState,SchemaState | MutableApplicationState |
| 38 | `Abwab.AbwabTemplateApplyBehaviorTests` | MutableWriter | Abwab | 2 | 1 m 23.6 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 39 | `Api.Abwab.AbwabInclusionProjectionTests` | MutableWriter | Abwab | 7 | 1 m 37.5 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState,CanonicalQuranData | MutableApplicationState |
| 40 | `Api.Access.AccessAdministrationEndpointTests` | MutableWriter | Access | 17 | 1 m 25.2 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 41 | `Api.Access.AccessAuditEventPersistenceTests` | MutableWriter | Access | 19 | 1 m 31.9 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 42 | `Api.Access.AccessCollectionResetContractTests` | MutableWriter | Access | 3 | 1 m 27.6 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState,SchemaState | MutableApplicationState |
| 43 | `Api.Access.AccessMeEndpointTests` | MutableWriter | Access | 11 | 1 m 30.7 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 44 | `Api.Access.AccessRolesTests` | MutableWriter | Access | 20 | 1 m 36.2 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 45 | `Api.Access.AuthorizationPipelineTests` | MutableWriter | Access | 7 | 1 m 29.4 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 46 | `Api.Access.AuthorizationRejectionResponseTests` | MutableWriter | Access | 9 | 1 m 26.6 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 47 | `Api.Access.AuthorizationRequirementHandlerTests` | MutableWriter | Access | 18 | 1 m 33.4 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 48 | `Api.Access.AuthorizationStateResolverTests` | MutableWriter | Access | 4 | 1 m 25.3 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 49 | `Api.Access.DeviceSessionLifecycleTests` | MutableWriter | Access | 4 | 1 m 26.9 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 50 | `Api.Access.EmailIdentityPreflightTests` | MutableWriter | Access | 2 | 1 m 23.5 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 51 | `Api.Access.LogtoSubjectRelinkEndpointTests` | MutableWriter | Access | 8 | 1 m 29.5 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 52 | `Api.Access.OwnerReconciliationServiceTests` | MutableWriter | Access | 14 | 1 m 33.4 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 53 | `Api.Access.UserProvisioningServiceTests` | MutableWriter | Access | 12 | 1 m 30.4 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 54 | `Api.Linking.LinkingCollectionResetContractTests` | MutableWriter | Linking | 1 | 1 m 27.7 s | `quran_dashboard_test` (write + reset) | CanonicalQuranData,SystemCatalogue,MutableApplicationState,SchemaState | MutableApplicationState |
| 55 | `Api.Linking.LinkingConfirmationIdempotencyTests` | MutableWriter | Linking | 1 | 1 m 26.4 s | `quran_dashboard_test` (write + reset) | CanonicalQuranData,SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 56 | `Api.Linking.LinkingRecoveryAndAtomicityTests` | MutableWriter | Linking | 12 | 1 m 39.4 s | `quran_dashboard_test` (write + reset) | CanonicalQuranData,SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 57 | `Api.Linking.LinkingSuccessfulJourneyTests` | MutableWriter | Linking | 1 | 1 m 26.1 s | `quran_dashboard_test` (write + reset) | CanonicalQuranData,SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 58 | `Smoke.SmokeAbwabWriteAuthorizationTests` | MutableWriter | Smoke | 23 | 1 m 43.2 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 59 | `Smoke.SmokeAccessAdministrationAuthorizationTests` | MutableWriter | Smoke | 1 | 1 m 24.1 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 60 | `Smoke.SmokeAuthPipelineTests` | MutableWriter | Smoke | 2 | 1 m 24.2 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,MutableApplicationState | MutableApplicationState |
| 61 | `Smoke.SmokeMutableBootGuardTests` | MutableWriter | Smoke | 2 | 1 m 22.3 s | `quran_dashboard_test` (write + reset) | SystemCatalogue,SchemaState | None |

## Sources

- Run log: one `node scripts/test pre-pr` invocation, 2026-09-05.
- Policy registry: `Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv`,
  `test-resources.tsv`.
- Architecture: `docs/testing/persistent-test-database-architecture.md`, "Timing and evidence".
- Runner contract: `docs/testing/test-policy-runner.md`, "Reported timing".
- Preceding ticket's run report: `Backend/report/issue-149/pre-pr-test-run-2026-09-05.md`.
