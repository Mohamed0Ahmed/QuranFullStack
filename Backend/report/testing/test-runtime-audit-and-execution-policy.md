# Test Runtime Audit and Execution Policy — Before/After Evidence

Dated run evidence for the test-runtime optimization work (Phases 1–8) measured on its final
state. Date **2026-08-06**. Branch `feature/security-authorization-permissions`, HEAD
**9ed3a5d8** (`docs: codify scope-aware test execution`), working tree clean
(`git status --porcelain` empty) before, during, and after every run recorded here.

This file is evidence. Per plan §1 the counts and durations below belong to a dated run and
are deliberately **not** promoted into `TESTING_STRATEGY.md` or any living README.

---

## 1. Verdict

**Phase 9 acceptance is NOT met.** The single permitted broad Backend measurement exited **1**:
one test failed.

Plan §9 Phase 9 Acceptance requires that "all success criteria in §10 … are evidenced." §10
includes the lifecycle guarantees that `PostgreSqlTestProcessContractTests` exists to prove, and
one of that suite's assertions failed. The gate is therefore not satisfied, and this report is
the evidence a fresh reviewer needs in order to decide the remedy — it does not itself issue a
rollback decision.

Separately and factually, the scope of the failure is narrow:

- The failing case is in `QuranDashboard.Tests.TestSupport.PostgreSql` — the test harness's own
  contract suite. **No product guarantee failed.** All security, audit, migration-path,
  schema-drift, CLI/startup, rollback, source-safety and Quran-data assertions passed, as did all
  152 route-Smoke cases and all 13 canonical `SmokeDataReadTests` cases.
- 2,019 of 2,020 executed Backend cases passed; 0 skipped.
- The complete Frontend gate sequence passed: type-check, build, and the full suite (205 files,
  2,693 cases, 0 failed, 0 skipped).

The 152 / 13 Smoke figures are derived from the broad run's own TRX pair, not carried over from
the focused sanity runs: shard 1 contains 152 `QuranDashboard.Tests.Smoke.*` results across 10
route classes and zero `Smoke.Data` results; shard 2 contains exactly the 13 `Smoke.Data`
canonical results.

The one-run budget for each side was spent exactly once. There was no rerun, no narrowed filter,
and no partial run relabelled as the measurement. The first of these two facts — a failed broad
Backend run — is what blocks acceptance; the second is what bounds it.

---

## 2. Environment

Identical host for every run in this report.

| Item | Value |
| --- | --- |
| Host | `mohamed-HP-ZBook-15-G3`, Linux 7.0.0-29-generic |
| CPU / RAM | 8 logical CPUs / 15,098 MB (≈5,120 MB free at start) |
| .NET SDK | 10.0.110 |
| VSTest | 18.0.2 (x64), xUnit VSTest adapter v3.1.1 |
| `@angular/build` | 20.3.27 |
| Vitest | 3.2.6 |
| Docker | warm images; **zero containers present before the run** (`docker-before.txt` empty) |
| Frontend fork cap | `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` (load-bearing per plan §1; retained, set by the `test` script itself) |
| Backend database parallelism | `QURAN_DASHBOARD_TEST_DB_PARALLELISM=4`, exported by `Backend/scripts/test-backend` |

---

## 3. Exact commands

### 3.1 BEFORE — the fixed audit baseline (NOT rerun)

Plan §13.7 forbids rerunning a before suite and directs reuse of the audit's "exact recorded
commands/environment."

**The baseline's exact command lines are not recorded in any surviving artifact.** Plan §1 records
its *numbers* only, and `Backend/report/testing/phase-2-shared-postgres-runtime-evidence.md`
records only Phase 2 commands. What is provable about the baseline's shape:

- It could not have used `Backend/scripts/test-backend`, because plan §1 states the Backend suite
  at baseline "has no traits, categories, runsettings, stable gate script, or complete gate
  validator" — the script is a Phase 1 deliverable.
- It was **one** `dotnet test` process (plan §13.7 contrasts the after-run's "safe two-process
  orchestration" with "the single-process 479.46-second baseline").
- The Frontend baseline ran under the same fork cap, which plan §1 fixes as load-bearing.

No baseline command line is reconstructed here. Inventing a plausible invocation would be worse
than recording the gap.

### 3.2 AFTER — Backend, run once

```bash
# from /projects/Dashboard/App
VSTestLogger=trx Backend/scripts/test-backend pre-pr \
  --results-dir /tmp/quran-dashboard-test-runtime-after/backend
```

Exit **1**.

Wrapped by `/tmp/quran-dashboard-test-runtime-after/run-backend-measurement.sh`, which starts a
bounded `docker events --filter type=container` collector and a 2-second read-only resource
sampler under printed PIDs (654692 and 654693), timestamps every output line, and kills and
`wait`s both collectors in the same step through an `EXIT`/`INT`/`TERM` trap — never left as
hidden background work, as §13.7 requires.

**The `VSTestLogger=trx` prefix is not part of the script.** `Backend/scripts/test-backend` passes
`--results-directory` but never registers a TRX logger, contrary to §13.7's claim that "the script
… emits TRX". MSBuild reads `VSTestLogger` as a property; this was proven on a 0.9 s
container-free probe (`feature --class …TestGateCatalogTests --no-build`, `probe-trx.log`) **before**
the budget was spent. Two TRX files were produced. No script, test, or production file was modified.

### 3.3 AFTER — Frontend, full suite run once

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm run typecheck        # exit 0
npm run build:verify     # exit 0
npm test -- --reporters=default --reporters=junit    # exit 0
```

`npm test` is exactly `npm run test:full`, so this is §13.7's "same type-check/build/full sequence
as `npm run test:pre-pr`, with the full suite executed once and default progress retained while
adding JUnit."

**§13.7's prescribed command is invalid against the shipped builder.** The literal form —

```bash
npm test -- --reporters=default --reporters=junit \
  --outputFile=/tmp/quran-dashboard-test-runtime-after/frontend.junit.xml
```

— exits **1 after 0.62 s** with `Error: Unknown argument: outputFile`, executing zero tests
(`frontend-literal-attempt.log`). The `@angular/build` 20.3.27 `unit-test` builder declares
`additionalProperties: false` and has no `outputFile` property. Because nothing executed, the
one-run Frontend budget was untouched. With `--outputFile` rejected, Vitest's JUnit reporter falls
back to stdout; the `<?xml … </testsuites>` span was extracted from the captured stdout to
`frontend.junit.xml`. It parsed on the first attempt with no interleaving repair, and its counts
(205 testsuites, 2,693 cases, `skipped="0"` on every suite) match the default reporter exactly.

---

## 4. The fixed BEFORE baseline (audit baseline — not rerun)

Reproduced verbatim from plan §1. These are the audit's numbers, carried forward unchanged.

| Measure | Baseline |
| --- | ---: |
| Backend full-suite wall time | 479.46 s |
| Backend runtime cases | 1,958 |
| Backend PostgreSQL starts | 22 |
| Collection-owned PostgreSQL fixtures | 21 |
| Extra nested Abwab PostgreSQL fixture | 1 |
| Peak simultaneous PostgreSQL containers | 8 |
| Backend peak test-run RSS | 1,464,508 KB |
| Frontend full-suite wall time | 306.50 s |
| Frontend test files | 207 |
| Frontend runtime cases | 2,696 |
| Frontend bundle generation | 34.947 s |
| Frontend test-body time | 92.10 s |

---

## 5. Backend AFTER — build and test wall time, recorded separately

Externally timestamped per output line, cross-checked against each tool's own self-report.

| Segment | External wall | Tool self-report |
| --- | ---: | --- |
| Solution build (incremental, warm) | 4.55 s¹ | MSBuild `Time Elapsed 00:00:04.24`; 0 warnings, 0 errors |
| **Shard 1/2** — shared `postgres:16-alpine` | 308.99 s | MSBuild `00:05:08.74`; VSTest `Total time: 5.1231 Minutes` (307.39 s) |
| Inter-shard handover barrier | 0.08 s | script: `postgres runtime before next shard: free after 0s` |
| **Shard 2/2** — exclusive `postgres:18-alpine` | 56.19 s | MSBuild `00:00:55.94`; VSTest `Total time: 55.1364 Seconds` |
| Labelled cleanup + exit | 0.12 s | `cleanup: removed 0 container(s) and 0 network(s)` |
| **Test wall only (both shards, sequential)** | **365.18 s** | |
| **TOTAL measured wall** | **369.99 s** | |

¹ The 4.24 s MSBuild figure is the build time and is the one to cite. The 4.55 s external span
additionally includes wrapper/script startup before the first build line; it is *as recorded by
the measurement wrapper and is not re-derivable from the preserved logs*, because the wrapper's
start epoch was printed to the terminal rather than into a captured file. Every other number in
this report was re-derived from preserved evidence.

The build ran **once**, ahead of both shards. Both shard invocations pass `--no-build`
(`Backend/scripts/test-backend:539`, inside `build_test_args`, used for every shard), so shard 2
compiled nothing and its `Build succeeded` line is the VSTest target only. Build and test wall
time are therefore genuinely separated, not apportioned.

Shard boundaries verified from log timestamps: shard 1 spans `1785997538.315 → 1785997847.307`;
shard 2 spans `1785997847.386 → 1785997903.573`. The shards are strictly sequential and never
coexist (proved by Docker events in §8), so their wall times are **summed as sequential segments**.
No overlapping lanes were summed.

### 5.1 Case counts and shard partition

| | Executed | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| Shard 1 (PG16) | 2,007 | 2,006 | **1** | 0 |
| Shard 2 (PG18 canonical) | 13 | 13 | 0 | 0 |
| **Aggregate** | **2,020** | **2,019** | **1** | **0** |

Both TRX `Counters` elements report `notExecuted="0"` and `executed == total`. 253 distinct
classes produced results — exactly the 253 rows in `test-gates.tsv`.

**Partition verified from the TRX pair, not assumed:** the shard-1 TRX contains **zero**
`SmokeDataReadTests` results; the shard-2 TRX contains exactly those 13. Decision B's
complementary, non-overlapping split held.

---

## 6. Frontend AFTER

| Gate | Exit | Wall | Detail |
| --- | ---: | ---: | --- |
| `npm run typecheck` | 0 | 14.32 s | `typecheck:app` + `typecheck:spec` |
| `npm run build:verify` | 0 | 17.20 s | `Application bundle generation complete. [16.180 seconds]` |
| `npm test` (full suite) | 0 | 218.99 s | see below |

Full-suite breakdown, from Vitest's own summary line:

```
Test Files  205 passed (205)
     Tests  2693 passed (2693)
  Duration  193.63s (transform 6.33s, setup 76.82s, collect 14.65s,
                     tests 67.34s, environment 171.11s, prepare 17.39s)
```

The unit-test builder reported its own bundling inside this run: **`Application bundle generation
complete. [23.401 seconds]`**. That — not `build:verify`'s 16.180 s — is the figure comparable to
the baseline's 34.947 s bundle generation.

Test-body time is **67.34 s** (Vitest's `tests` component; the JUnit root carries
`time="67.344977147"`; summing per-suite `time` attributes independently gives 66.85 s, the 0.5 s
difference being per-suite rounding). It is summed parallel work and is reported only as a
component — the comparison figure is wall time.

Three **pre-existing** bundle-budget WARNINGS in `build:verify` (exit 0, unrelated to this work):
initial bundle 578.99 kB vs a 500 kB budget; `selected-ayah-section.component.scss` 5.85 kB vs
4 kB; `selected-word-section.component.scss` 4.65 kB vs 4 kB.

---

## 7. Before/after deltas, with comparability stated

### 7.1 Backend — the delta is NOT directly comparable

| Measure | Before (audit) | After (2026-08-06) | Delta |
| --- | ---: | ---: | ---: |
| Full-suite wall | 479.46 s | 369.99 s | −109.47 s (−22.8%) |
| Runtime cases | 1,958 | 2,020 | +62 |
| PostgreSQL starts | 22 | 2 | −20 |
| Peak simultaneous PostgreSQL containers | 8 | 1 | −7 |
| Peak test-run RSS | 1,464,508 KB | 1,548,248 KB | +83,740 KB — **not method-comparable** |

Three qualifications, all load-bearing:

1. **Shape.** The baseline is one `dotnet test` process. The after-run is one build plus **two
   sequential, non-overlapping** `dotnet test` invocations with a handover barrier between them
   (Decision B, §9). It is not the same orchestration, and the aggregate must not be read as if it
   were the single-process baseline.
2. **The after-run failed.** The 369.99 s aggregate is a *failed* run compared against a *passing*
   baseline, and it includes **66.02 s spent inside the one test that failed**. No "would have
   been" number is computed here — that would be a second measurement, which was not run and is
   not authorized.
3. **RSS is not method-comparable.** The sampler matched `dotnet|testhost|vstest`, which also
   catches MSBuild nodes and the build itself; the baseline's measurement method is not recorded.
   Do not read +5.7% as a regression.

The −22.8% is arithmetically true and structurally qualified by all three points above.

### 7.2 Frontend — the delta IS comparable

Same command shape, same fork cap, passing run on both sides.

| Measure | Before (audit) | After (2026-08-06) | Delta |
| --- | ---: | ---: | ---: |
| Full-suite wall | 306.50 s | 218.99 s | −87.51 s (−28.6%) |
| Test files | 207 | 205 | −2 |
| Runtime cases | 2,696 | 2,693 | −3 |
| Bundle generation | 34.947 s | 23.401 s | −11.55 s |
| Test-body time | 92.10 s | 67.34 s | −24.76 s |

The file and case deltas are **fully attributed**: Phase 7 (`a86120e1`) deletes exactly
`src/app/app.sanity.spec.ts` and `src/app/core/auth/auth.testing.spec.ts` (−2 files), and their
1 + 2 cases account for all 3 missing cases.

### 7.3 Case-count reconciliation — Backend residual is OPEN

Backend went 1,958 → 2,020, **+62**. Attributable: Phases 1–4 added 54 TestSupport cases
(`TestGateCatalogTests` 14, `PostgreSqlTestProcessContractTests` 27, `OwnedServiceProviderRegistryTests`
4, `ProcessExecutionTests` 4, `ProcessGlobalStateScopeTests` 5); Phase 7 removed 3 backend methods
and added one data-driven theory. That leaves a residual of roughly **11 rows unattributed**. The
baseline's counting method is not recorded and cannot be re-derived without a second broad run.
**Reported as a raw delta, not reconciled.**

A separate metric must not be confused with either: VSTest *discovery* reports **1,985** entries
(`pre-pr --list-tests`), because discovery enumerates compile-time-known theory rows. The authority
for the after count is **2,020 executed cases**. What discovery *does* prove is coverage: catalog
rows and discovered classes reconcile at **253 = 253 with zero drift in both directions**
(`diff <(sort catalog-classes.txt) <(sort discovered-classes.txt)` is empty; the script performs
its own bidirectional reconciliation at `Backend/scripts/test-backend:601-631` and exits 1 on any
mismatch — it exited 0). This independently evidences §10's "every Backend test belongs to at
least one validated execution gate."

---

## 8. Resource lifecycle inventory

Method: bounded `docker events --filter type=container` capture (collector PID 654692, started
before the run, killed and waited in the same step), 501 events; plus a 2-second read-only
`docker exec … psql` sampler on `pg_database` (PID 654693; 165 `docker ps` ticks, 156 database
ticks). Docker held zero containers before the run.

### 8.1 PostgreSQL containers — classified by the event `image` attribute

| Image | create | start | kill | die | destroy | Alive |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `postgres:16-alpine` | 1 | 1 | 1 | 1 | 1 | 303.7 s |
| `postgres:18-alpine` | 1 | 1 | 1 | 1 | 1 | 49.3 s |

**Total PostgreSQL starts = 2** (baseline 22).

**Peak simultaneous PostgreSQL = 1.** Computed from `start → die` running intervals rather than
`create → destroy`, because `destroy` lags asynchronously — precisely what the script's
`await_free_postgres_runtime` barrier absorbs. Overlapping PostgreSQL interval pairs: **0**. The
PG16 container died at epoch `1785997846.801`; the PG18 container started at `1785997853.798` — a
**7.0 s gap**, so **no PG16/PG18 overlap occurred** (§10 requirement, §6 Decision B requirement).
The independent 2-second `docker ps` sampler never saw more than one postgres container across
165 ticks.

### 8.2 Ryuk — inventoried separately, never counted as a PostgreSQL duplicate

`testcontainers/ryuk:0.9.0` — **create 2, start 2, die 1, destroy 1** within the capture window:
one reaper per test host, i.e. one per shard. The second reaper was still `Up 53 seconds` in
`docker-after.txt` at T+3 s and self-reaped shortly afterwards; it is not project-owned.

### 8.3 Final zero-container state

At T+3 s after the script exited, `docker ps -a` filtered on
`owner=backend-tests` + `repository=quran-dashboard` + `kind=postgresql` returned an **empty file**
(`docker-after-owned.txt`, 0 lines). At the later check `docker ps -a` returned **0 containers of
any kind**. §10's "zero project-owned test containers remain after each test process exits" is met
exactly.

The script's own accounting reported `cleanup: removed 0 container(s) and 0 network(s); 0
container(s) were already removed by their test host` — the test hosts' `ProcessExit` handlers
reached zero unaided and the script trap acted as a pure backstop.

### 8.4 Databases, templates and schemas

Measured by the 2-second sampler (`select datname||':'||datistemplate from pg_database`). Sampling
gives a **lower bound**: a database created and dropped inside one 2-second window is invisible.

- **Template databases: exactly 1** — `qdb_test_template_654913_1_a8456d1c`, observed transitioning
  `datistemplate false → true` (2 ticks false, 130 ticks true). PID `654913` is the shard-1 test
  host and `_1_` is the first generated name in that process, directly evidencing "one migrated
  template per process, built once, on the first migrated lease."
- **Leased databases: 28 distinct `qdb_*` observed** = 1 template + **27 clones**, across **21
  distinct lease owners**: `abwabschemafixture`, `accessadmincommandtests`,
  `accessmigrationtestfixture`, `accessschemadrifttests`, `accesstestfixture`,
  `displaywordsrealimportfixture`, `fulli3rabimporttestfixture`, `i3rabgenerationtestfixture`,
  `importtestfixture`, `morphologyexplorerstestfixture`, `morphologyimporttestfixture`,
  `mushafreadertestfixture`, `mutashabihatimporttestfixture`, `navigationimporttestfixture`,
  `postgresqltestprocesscontracttests`, `smokeapifixture`, `tafsirimporttestfixture`,
  `translationimporttestfixture`, `uniquewordstestfixture`, `wordsdisplaytestfixture`,
  `wordtypestestfixture`.
- **Concurrent database cap held: peak simultaneous `qdb_*` = 5** = the 4-slot lease cap + the
  template. `QURAN_DASHBOARD_TEST_DB_PARALLELISM=4` was never exceeded (§10 "at most four ordinary
  database collection leases are active").
- The PG18 exclusive server hosted **no** `qdb_*` database; the canonical dump is restored into
  `postgres` itself.
- **Per-schema counts are not observable** from Docker or `pg_database`. The staged migration lane's
  single empty database was observed (`qdb_test_accessmigrationtestfixture_654913_100_c8ea9d49`),
  but its per-case `PostgreSqlSchemaLease` schemas were not counted.

### 8.5 Full migration-chain applications — CONTRACT-ONLY, NOT MEASURED

**This number was not measured and is not measurable from the evidence captured.**

§13.7 states "the PostgreSQL runtime also writes one machine-readable lifecycle summary into the
results directory." **That artifact was never implemented.** The results directory holds only the
two TRX files and Blame folders, and no source under `Backend/tests/QuranDashboard.Tests/` writes
such a summary (the only `File.WriteAllText` is `CrossProcessPostgreSqlLock.cs:111`, the lock-holder
sidecar).

What the code contract says — applied once per process to the template, with clones never
reapplying it — is at `TestSupport/PostgreSql/PostgreSqlTestServer.cs:196-237` and documented in
`TestSupport/PostgreSql/README.md`. The strongest **observable proxy** is the single template
transition in §8.4: one template per process, built once. Source reading is not laundered here as
measurement; the count remains open.

### 8.6 External process launches

Measured in-container, from the event stream:

- **1** `pg_restore --username postgres --dbname postgres --data-only --disable-triggers --jobs 4
  /dump/quran-canonical.dump` — exactly one canonical restore, on shard 2.
- **6** `pg_isready` execs (3 per server) from the testcontainers wait strategy.
- **155** sampler `psql` execs (134 on PG16, 21 on PG18) — the observer's own, see §11.6.

Test-host OS process launches were not counted at runtime. The source has exactly three launch
sites — `TestSupport/Process/ProcessExecution.cs:30` (used by `ProcessExecutionTests` via bash),
`PostgreSqlTestProcessContractTests.cs:347` (a real second process holding the cross-process lock
via `flock`), and `Api/Access/AccessAdminCommandTests.cs:75` (the access-admin wrapper via bash) —
and every test exercising them passed.

### 8.7 Peak RSS

1,548,248 KB for the largest single process; 1,865,924 KB summed across `dotnet`/`testhost`/`vstest`.
Baseline 1,464,508 KB. **Not method-comparable** — see §7.1 point 3.

---

## 9. Decision B and why the two-shard aggregate is not the baseline's shape

Plan §6 required PG16/PG18 compatibility to be **proven, not assumed**. Phase 5 (`9eb34521`,
`test: serialize canonical postgres restore`) selected **Decision B — retain exclusive
PostgreSQL 18**, because the canonical archive/restore semantics could not be proven on
PostgreSQL 16. §6 is explicit that a compatibility failure "selects Decision B; it does not justify
skipping or reducing `SmokeDataReadTests`" — and it did not: all 13 canonical cases ran and passed.

Decision B's contract, and the evidence for each clause:

| Decision B requirement | Evidence |
| --- | --- |
| `SmokeDataCollection` globally nonparallel | `Smoke/Data/SmokeDataCollection.cs:9` (`DisableParallelization = true`); sole membership asserted by `TestSupport/Execution/TestGateCatalogTests.cs:184` |
| PG18 owner takes the same cross-process OS lock as PG16 | shared runtime; handover barrier printed `free after 0s` |
| Full pre-PR = two complementary invocations | shard-1 TRX has 0 `SmokeDataReadTests`; shard-2 TRX has exactly the 13 |
| Empty intersection, union = intended complete class set | 253 classes with results = 253 catalog rows; catalog↔discovery diff empty both ways |
| Fully dispose shard 1 and PG16 before PG18 starts | PG16 `die` at `…846.801`, PG18 `start` at `…853.798` |
| Prove by Docker events that PG16 and PG18 never overlap | 0 overlapping intervals, 7.0 s gap |

**Why the aggregate is not directly comparable to 479.46 s.** The baseline was a single
`dotnet test` process holding whatever containers it started, at a measured peak of 8 simultaneous
PostgreSQL containers. The after-run is deliberately *serialized*: one build, then shard 1, then a
barrier that waits for the PG16 runtime to be fully free, then shard 2. That serialization is the
safety property (§10: "two concurrent `dotnet test` processes cannot hold project PostgreSQL
runtimes at the same time"; "no PostgreSQL 16/18 overlap occurs"), and it costs wall time that the
baseline never paid. Comparing 369.99 s to 479.46 s therefore compares two different
orchestrations, not two versions of one. The segments are summed because they are sequential and
non-overlapping; no overlapping lane was ever summed.

---

## 10. Skip accounting — zero skips, none expected, none observed

§11's "False speedup by skipped tests" risk is closed by direct measurement, not by inference.

| Suite | Mechanism checked | Result |
| --- | --- | --- |
| Backend shard 1 | TRX `<Counters … notExecuted="0" executed="2007" total="2007">` | 0 skipped |
| Backend shard 2 | TRX `<Counters … notExecuted="0" executed="13" total="13">` | 0 skipped |
| Backend log | `grep -ciE "skipped|\[SKIP\]"` over `backend-pre-pr.log` | 0 matches |
| Frontend | `skipped="0"` on **all 205** JUnit `<testsuite>` elements; no "N skipped" line in the default reporter | 0 skipped |

**No test was expected to skip, and none did.** In particular the canonical data tier did not skip:
shard 2 executed all 13 `SmokeDataReadTests` cases against the staged canonical dump and exited 0.
Focused evidence agrees — the route-Smoke lane's log prints `canonical data tier: not selected`
(the data tier correctly did **not** run there, satisfying `TESTING_STRATEGY.md` §3/§5's explicit
evidence requirement), and `pre-pr --list-tests` prints `canonical data tier: discovery only` with
zero containers started.

---

## 11. The failure

### 11.1 Full output, verbatim

```
QuranDashboard.Tests.TestSupport.PostgreSql.PostgreSqlTestProcessContractTests
  .LeaseDisposal_DropsTheDatabase_AndIsIdempotent    [1 m 6 s / TRX 66.02 s]

Error Message:
   Expected (PostgreSqlTestProcess.AvailableDatabaseSlotsAsync()) to be 0, but found 1
   (difference of 1).

Stack Trace:
     at FluentAssertions.Execution.LateBoundTestFramework.Throw(String message)
     at FluentAssertions.Execution.DefaultAssertionStrategy.HandleFailure(String message)
     at FluentAssertions.Execution.AssertionChain.FailWith(Func`1 getFailureReason)
     at FluentAssertions.Numeric.NumericAssertionsBase`3.Be(T expected, String because, Object[] becauseArgs)
     at QuranDashboard.Tests.TestSupport.PostgreSql.PostgreSqlTestProcessContractTests
        .LeaseDisposal_DropsTheDatabase_AndIsIdempotent()
        in /projects/Dashboard/App/Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/PostgreSqlTestProcessContractTests.cs:line 156
```

`backend-pre-pr.log` line 12504; shard-1 TRX `_mohamed-HP-ZBook-15-G3_2026-08-06_09_25_41.trx`.

Shard 1 then reported `Test Run Failed. Total tests: 2007, Passed: 2006, Failed: 1`, followed by
`MSB4181: The "VSTestTask" task returned false but did not log an error` and `Build FAILED.
0 Warning(s) 0 Error(s)` — the ordinary way `dotnet test` surfaces a failed run, **not** a
compilation problem.

### 11.2 Mechanism — a racy harness assertion, and the direction of the error proves it

`Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/PostgreSqlTestProcessContractTests.cs:144-157`:
the test takes a `maintenance` lease, reads `slotsBefore`, then leases and disposes a second lease
twice, then asserts the count returned to `slotsBefore`.

- Line 155 — `(await DatabaseExistsAsync(maintenance, databaseName)).Should().BeFalse()` — **passed**.
  The drop-and-idempotency contract the test exists to prove held.
- Line 156 failed on the slot-count baseline only.
- `AvailableDatabaseSlots` is the **process-wide** `SemaphoreSlim.CurrentCount`
  (`PostgreSqlTestProcess.cs:20-23`), shared by every collection in the same test host. Under
  full-suite parallelism other collections acquire and release between the two observations.
- With a 4-slot cap and one lease held by this test, `slotsBefore` can only be 0 if three further
  slots were held by *other* collections at that instant. By the second read at least one foreign
  holder had released.

**The direction is decisive.** "Expected 0, but found 1" reports **more** free slots than expected.
A genuine slot leak — the failure mode this test exists to catch — would show **fewer**. This is a
racy assertion, not a runtime leak.

The class passes in isolation: the pre-measurement sanity run executed all 27 cases in 21.45 s,
0 failed.

### 11.3 It is a family of three, not one case

`.Should().Be(slotsBefore)` on the same process-global counter appears at:

- `:105` — `CanceledCaller_IsRefused_AndLeavesTheRuntimeUsable`
- `:140` — `QueuedCaller_ThatIsCanceledWhileWaiting_LeavesTheSlotCountIntact`
- `:156` — the failure

Two of the three passed by luck in this run. Separately, `:69`
(`DatabaseSlots_MakeAnExtraLeaseWait_UntilOneIsReleased`) and `:118` drain every free slot in a
`while (await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync() > 0)` loop — a starvation hazard
for concurrent collections that **plausibly** contributed to the 66.02 s duration, which is
otherwise unexplained. The causal link is not proven by this evidence. A patch to line 156 alone
would leave the flake in place.

### 11.4 Scope for the rollback decision

The reviewer, not this report, owns the remedy. What the evidence supports:

- The racy assertion **ships in Phase 2** (`001ab59c`, `test: add shared postgres test runtime`),
  which introduced both `AvailableDatabaseSlotsAsync` and these assertions.
- It is **exposed** only by the four-slot cross-collection contention that Phases 3–4 legitimately
  created; the assertion was latent before there were enough concurrent lease holders to saturate
  the cap.
- Reverting Phase 2 would remove the entire shared runtime — the mechanism, stated so it can argue
  for itself. Whether the correct action is a rollback, an assertion fix, or something else is the
  reviewer's call.

### 11.5 Script reporting defect — the log misattributes the failure

`backend-pre-pr.log` ends with `canonical data tier: failed`, although shard 2 **is** the canonical
tier and it passed 13/13 and exited 0. The message keys off the aggregate `test_status`
(`Backend/scripts/test-backend:661-666`), so a shard-1 failure is reported as a canonical-tier
failure. **A reader of the log alone would misdiagnose which resource tier broke.** Recorded, not
fixed — Phase 9 measures and reports; it does not implement.

### 11.6 Observer effect, disclosed

The resource sampler issued **155** read-only `docker exec … psql` calls (134 on PG16, 21 on PG18),
visible in the event stream. They query only `pg_database` on the `postgres` maintenance database
and cannot touch an in-process `SemaphoreSlim`, so they **cannot** have caused §11.2 — which is in
any case provable from source independently of the run. They do add a small, unquantified overhead
to the 369.99 s aggregate.

---

## 12. Runtime target bands (§10) — deviations stated with the number

| Scope | §10 target band | Measured | Verdict |
| --- | --- | ---: | --- |
| Full Backend comparison | 400–500 s (investigate above 503 s) | **369.99 s** | **Below band.** Favourable; the investigate-above threshold was never approached. Stated as a deviation, not silently absorbed. |
| Full Frontend comparison | 285–330 s | **218.99 s** | **Below band.** Favourable deviation. |
| Complete Access lane | "Establish the Phase 1 baseline first"; provisional 60–180 s | **28.75 s** | **Below band, and the delta is UNAVAILABLE** — see below. |
| Backend focused DB class after build | 10–60 s | 21.45 s (`PostgreSqlTestProcessContractTests`, `--no-build`) | Within band |
| Backend pure focused feedback after build | 1–30 s | 0.896 s (`TestGateCatalogTests`), 0.087 s, 0.082 s, 2 s | Within band |
| Frontend one-file focused feedback | 30–90 s | not measured this run | Not measured |

**The Access before/after delta cannot be computed.** §10 (plan line 1503) required "establish the
Phase 1 baseline first", and no pre-change Access timing was ever recorded:
`Backend/report/testing/` contains only `phase-2-shared-postgres-runtime-evidence.md` and the plan
itself, and the Phase 2 evidence explicitly fences Access as not run. The measured 28.75 s sits
well below the provisional band. Its composition — **115 cases across 19 classes** in
`Api.Access` + `TestSupport.Access` — is corroborated by the broad run's own shard-1 TRX, which
contains exactly 115 results across 19 classes in those two namespaces; the lane log adds
`resource needs: Database,Migration,Process` and selects no Pipeline/canonical/Frontend class.
The timing itself is a focused-run figure. Recorded as
**band deviation, favourable, before-number never recorded, delta unavailable.** No baseline is
reconstructed.

---

## 13. Slowest tests, classes and files

All parsed from machine-readable results (TRX ×2 for the Backend, extracted JUnit for the Frontend).

### 13.1 Backend — slowest 20 tests (seconds)

| s | Outcome | Test |
| ---: | --- | --- |
| 66.02 | **FAILED** | `PostgreSqlTestProcessContractTests.LeaseDisposal_DropsTheDatabase_AndIsIdempotent` |
| 20.41 | Passed | `Quran.Import.ForceReloadTests.ForceReRun_ProducesTableStateIdenticalToFirstImport` |
| 19.08 | Passed | `Quran.Import.ValidationReportTests.ValidSource_WritesPassWithWarningsReportForAyah37130` |
| 16.48 | Passed | `WordsMorphologyEnriched.EnrichedMorphologyArtifactTests.Boundary_ayah_13_37_is_represented` |
| 15.48 | Passed | `EnrichedMorphologyArtifactTests.Corrected_lemma_41_44_16_is_not_ءَامَنَ` |
| 14.73 | Passed | `Quran.Import.ReRunGuardTests.ReRunWithoutForce_OnPopulatedTables_RefusesAndChangesNothing` |
| 12.93 | Passed | `EnrichedMorphologyArtifactTests.Artifact_has_no_duplicate_word_segment_keys` |
| 12.80 | Passed | `Quran.Import.ImportCountsTests.ImportIntoEmptyDatabase_PersistsExpectedCounts` |
| 12.43 | Passed | `Quran.Import.ImportReconstructionTests.ImportIntoEmptyDatabase_ResolvesSampleWordAndPageOneLayout` |
| 12.11 | Passed | `EnrichedMorphologyArtifactTests.Artifact_has_no_duplicate_word_locations` |
| 11.11 | Passed | `Quran.Import.ImlaeiCleanKeyImportTests.Import_BindsCleanImlaeiKeyAndPreservesRawImlaeiText` |
| 8.43 | Passed | `Translations.TranslationReportShapeTests.Success_reports_include_provenance_warning_inline_markup_language_cove…` |
| 8.31 | Passed | `Quran.Import.ValidationFailureTests.CorruptedSourceWithDuplicateWordId_AbortsImportAndPersistsNothing` |
| 7.39 | Passed | `PostgreSqlTestProcessContractTests.ExternalReadOnlyLease_LeavesItsDatabaseIntact_WhenDisposed` |
| 7.18 | Passed | `Translations.TranslationSourceSafetyTests.Success_reports_do_not_include_translation_body_text_or_arabic_quran_a…` |
| 6.92 | Passed | `PostgreSqlTestProcessContractTests.ConcurrentMigratedLeases_ShareOneServer_AndIsolateTheirData` |
| 6.83 | Passed | `EnrichedMorphologyArtifactTests.Artifact_segment_count_is_exactly_128219` |
| 6.72 | Passed | `PostgreSqlTestProcessContractTests.CanceledCaller_IsRefused_AndLeavesTheRuntimeUsable` |
| 6.51 | Passed | `EnrichedMorphologyArtifactTests.Corrected_lemma_11_29_17_is_not_ءَامَنَ` |
| 6.46 | Passed | `Translations.TranslationRollbackTests.Report_write_failure_after_copy_rolls_back_all_writes` |

### 13.2 Backend — slowest 20 classes (summed case time / cases)

| s | Cases | Class |
| ---: | ---: | --- |
| 129.61 | 17 | `Quran.WordsMorphologyEnriched.EnrichedMorphologyArtifactTests` |
| 100.60 | 27 | `TestSupport.PostgreSql.PostgreSqlTestProcessContractTests` ¹ |
| 23.96 | 7 | `Quran.Translations.TranslationReportShapeTests` |
| 20.41 | 1 | `Quran.Import.ForceReloadTests` |
| 19.08 | 1 | `Quran.Import.ValidationReportTests` |
| 18.57 | 3 | `Quran.Translations.TranslationSourceSafetyTests` |
| 17.91 | 3 | `Quran.Translations.TranslationRollbackTests` |
| 14.73 | 1 | `Quran.Import.ReRunGuardTests` |
| 12.80 | 1 | `Quran.Import.ImportCountsTests` |
| 12.43 | 1 | `Quran.Import.ImportReconstructionTests` |
| 11.11 | 1 | `Quran.Import.ImlaeiCleanKeyImportTests` |
| 9.76 | 4 | `Quran.Translations.TranslationExcludedSourceTests` |
| 8.31 | 1 | `Quran.Import.ValidationFailureTests` |
| 7.39 | 70 | `Smoke.SmokeAbwabWriteTests` |
| 6.75 | 2 | `Quran.Translations.TranslationRefusalForceTests` |
| 5.79 | 10 | `Quran.WordsDisplay.DisplayWordsRealImportIdentityLinksTests` |
| 5.43 | 13 | `Smoke.Data.SmokeDataReadTests` (the PG18 shard in full) |
| 5.42 | 4 | `Api.Access.AccessAdminCommandTests` |
| 5.00 | 49 | `Smoke.SmokeRoutePipelineTests` |
| 4.51 | 13 | `Quran.WordsMorphology.MorphologyValidationFailureTests` |

¹ Inflated by the failing case's 66.02 s slot wait; without it the class totals 34.58 s.

### 13.3 Frontend — slowest 20 tests (body seconds)

| s | Test |
| ---: | --- |
| 1.708 | `entity-detail-overlay-ayah-continuity` — carries a three-frame stack onto… |
| 0.669 | `app.nested-layers` — keeps the drawer inside the inert shell subtree… |
| 0.608 | `entity-detail-overlay-invariant` — starts a one-frame overlay stack from… |
| 0.497 | `auth-bearer-token` — attaches `Authorization: Bearer <t…` |
| 0.486 | `dev-latency.interceptor` — forwards successful responses after the configured dev l… |
| 0.417 | `abwab.routes` — renders the abwab page at its root path |
| 0.415 | `stems-explorer-page` US2 — headline result count equal to the li… |
| 0.398 | `app.nested-layers` — holds the body scroll… |
| 0.391 | `word-types-explorer-page` — defaults route state to `type=noun`, loads tree o… |
| 0.389 | `entity-detail-overlay-host` — mounts the matching real a… |
| 0.382 | `app.nested-layers` — leaves exactly one f… |
| 0.376 | `lemmas-explorer-page` US1 — headline result count equal to the l… |
| 0.374 | `app.nested-layers` — holds the body scroll (2) |
| 0.361 | `roots-explorer-page` US2 — mounts the explainer hero inside the intro band |
| 0.342 | `unique-words-page` — renders the page title and active mode label |
| 0.329 | `abwab-page` — composes the toolbar, tree, side panel and announcer |
| 0.314 | `unique-words-table` — scrolls the fallback body back to the top |
| 0.276 | `root-detail-overlay-adapter` — renders the panel content f… |
| 0.270 | `entity-detail-overlay-host` — mounts the persistent host |
| 0.266 | `abwab-relations-modal` — renders one group per n… |

### 13.4 Frontend — slowest 20 files (summed body time / cases)

| s | Cases | Spec |
| ---: | ---: | --- |
| 5.656 | 109 | `abwab-page.component.spec` |
| 5.033 | 72 | `word-types-explorer-page.component.spec` |
| 2.765 | 41 | `stems-explorer-page.component.spec` |
| 2.629 | 40 | `lemmas-explorer-page.component.spec` |
| 2.114 | 34 | `roots-explorer-page.component.spec` |
| 1.823 | 4 | `app.nested-layers.spec` |
| 1.755 | 12 | `entity-detail-overlay-host.component.spec` |
| 1.718 | 26 | `unique-words-page.component.spec` |
| 1.708 | 1 | `entity-detail-overlay-ayah-continuity.spec` |
| 1.605 | 40 | `abwab-relations-modal.component.spec` |
| 1.320 | 25 | `detail-overlay-history.service.spec` |
| 1.063 | 48 | `word-types-table.component.spec` |
| 1.052 | 38 | `abwab-move-picker.component.spec` |
| 0.940 | 23 | `unique-words-table.component.spec` |
| 0.921 | 46 | `abwab-tree.component.spec` |
| 0.919 | 27 | `abwab-sections-modal.component.spec` |
| 0.806 | 21 | `selected-word-section.component.spec` |
| 0.788 | 21 | `abwab-door-modal.component.spec` |
| 0.731 | 20 | `top-navbar.component.spec` |
| 0.727 | 16 | `abwab-template-copy-modal.component.spec` |

---

## 14. §10 success criteria — evidenced or not

| Criterion | Status | Evidence |
| --- | --- | --- |
| ≤1 PostgreSQL 16 container per test process | Met | events: PG16 create/start = 1 |
| Two concurrent `dotnet test` processes cannot hold runtimes simultaneously | Met | cross-process lock contract suite passed (`flock` child process, `:347`); handover barrier printed |
| No PG16/PG18 overlap | Met | 0 overlapping intervals; 7.0 s gap |
| Ryuk inventoried separately | Met | §8.2 |
| Zero project-owned containers after exit | Met | `docker-after-owned.txt` empty; `docker ps -a` 0 of any kind |
| Unique database or proven-safe schema per collection | Met | hard assertion, not sampling: `TestGateCatalogTests.CollectionResources_MatchCompiledCollectionDefinitions` passed in the broad run, reconciling all 21 `test-resources.tsv` rows against the compiled `[CollectionDefinition]`s. Corroborated by 21 distinct sampled lease owners with distinct `qdb_*` names |
| ≤4 ordinary database leases active; Fast tests parallel and container-free | Met | the cap **held while saturated** — peak concurrent `qdb_*` = 5 (4 leases + template) and never 6, in a run whose `DatabaseSlots_…` and `QueuedCaller_…` cases deliberately drain the cap to zero free slots. `--list-tests` and `TestGateCatalogTests` started 0 containers |
| Migration chain applied once to the process template; clones do not reapply | Met (by construction) | §8.5 and §21.2 — the runtime cannot reapply it: `PostgreSqlTestServer.cs:31` builds the template behind one `Lazy<Task<string>>` and `:217` is the only `MigrateAsync()`, while `:239-243` clones every lease with `CREATE DATABASE … TEMPLATE`. Runtime count still not emitted |
| Lease isolation/cleanup pass regardless of collection order | **NOT met** | §11 and §21.5 — `LeaseDisposal_DropsTheDatabase_AndIsIdempotent` failed under cross-collection contention. This single bullet is what blocks acceptance |
| One PG16 builder + one locked PG18 canonical builder (Decision B) | Met | §9 |
| Migration-upgrade tests retain staged execution and refusals | Met | `AccessMigrationPathTests`, `AccessSchemaDriftTests` passed |
| Every Backend **and Frontend** test belongs to a validated gate | Met | Backend: 253 catalog rows ↔ 253 discovered classes, diff empty both ways. Frontend: §21.1 — `node testing/verify-test-gates.mjs` exits 0 over 205 spec files and 9 configurations |
| Focused Access selects `Api.Access` + `TestSupport.Access` only | Met | 19 classes / 115 cases, corroborated by shard-1 TRX; `resource needs: Database,Migration,Process`; no Pipeline/canonical/Frontend |
| Pipeline/canonical not run for isolated authorization work | Met | access and smoke lanes both print `canonical data tier: not selected` |
| Broad suites executed once at the final milestone | Met | one Backend, one Frontend broad run; budget spent exactly once |
| All security/audit/migration/CLI/startup/rollback/Quran-data guarantees pass | Met | 2,019/2,020 passed; the sole failure is in `TestSupport.PostgreSql` |
| No false speedup by skipped tests | Met | 0 skips both suites, §10 above |

---

## 15. Process-global risk re-inventory (§9 Phase 9 requirement)

Static re-inventory over `Backend/tests/QuranDashboard.Tests/` and
`Frontend/quran-dashboard-ui/src/`.

### 15.1 Backend

- **21 `CollectionDefinition` declarations** — matching the baseline's 21 collection-owned fixtures.
  Exactly **two** are nonparallel:
  - `SmokeDataCollection` — `Smoke/Data/SmokeDataCollection.cs:9`, `DisableParallelization = true`.
    Sole member `Smoke/Data/SmokeDataReadTests.cs:10`. Guards the exclusive PG18 canonical server.
    `TestSupport/Execution/TestGateCatalogTests.cs:184` asserts sole membership, so a second class
    joining is a test failure.
  - `AccessProcessGlobalCollection` — `Api/Access/AccessMigrationTestFixture.cs:102`,
    `DisableParallelization = true`. Members `AccessMigrationPathTests.cs:6`,
    `AccessSchemaDriftTests.cs:6`, `AccessAdminCommandTests.cs:8`. Guards env/cwd/console mutation.
- **Environment / current-directory / console mutation** is confined to two files:
  `TestSupport/Process/ProcessGlobalStateScope.cs` and its own tests (which restore in `finally`).
  **Zero test bodies mutate these directly.** Three consumer sites, all disposed:
  `AccessAdminCommandTests.cs:14` (`using var`); `AccessAdminCommandTests.cs:27` (deliberately not
  `using` — disposed in `try`/`finally` at :39 so :43 can assert `RestoreFailures` is empty after
  disposal; correct, not a leak); `AccessMigrationTestFixture.cs:79` (`using var`).
- `Console.Error.WriteLine` at `PostgreSqlTestServer.cs:307`, `PostgreSqlTestProcess.cs:145`,
  `OwnedServiceProviderRegistry.cs:65` are writes, not stream replacement.
- `AppContext.SetSwitch` / `DefaultThreadCurrentCulture` / `CurrentCulture` assignment: **zero hits**.
- **Static mutable state: exactly two**, both infrastructure and both safely managed —
  `PostgreSqlDatabaseName.cs:15` `counter` (the bounded-name monotonic counter, §11 safeguard) and
  `PostgreSqlTestProcess.cs:11` `exclusiveLeases` (`Interlocked.Increment` :77,
  `Interlocked.Decrement` :83/:89, `Volatile.Read` :96). Every other static is `static readonly`,
  `const`, or a get-only/`Lazy` immutable test-data member.

### 15.2 Frontend (205 spec files)

`src/test-setup.ts` installs one unconditional global `afterEach` restoring, in order:
`vi.unstubAllGlobals()`, `vi.useRealTimers()`, `vi.restoreAllMocks()`, `localStorage.clear()`,
`sessionStorage.clear()`, `documentElement` `data-theme` removal, `document.body.style.overflow`
reset.

- 8 `vi.stubGlobal` sites (`matchMedia` ×4, `ResizeObserver` ×2, `requestIdleCallback`, 1 in
  `app.nested-layers.spec.ts`) — all covered by `unstubAllGlobals`.
- `localStorage`/`sessionStorage` mutation across ~15 specs — covered by the global clears.
- 11 spec files use fake timers — covered by `useRealTimers`.
- 6 spec files call `document.body.appendChild`; **all 6** remove their nodes.
- Zero direct `window.<prop> =` assignments in specs.
- **One deliberate unrestored process-global:** `src/test-setup.ts` replaces `console.warn` by plain
  assignment (a CDK/jsdom `[cdkFocusInitial]` noise filter delegating via `passThroughWarn`).
  Because it is a plain assignment and not a `vi` spy, `vi.restoreAllMocks()` does not undo it, and
  `setupFiles` re-runs per test file against a worker-shared `globalThis.console`. It is installed
  once at setup scope rather than per test, so there is no cross-test leakage.

This re-inventory was **static grep only**; no Frontend test execution beyond the single broad run
occurred.

---

## 16. Plan-vs-implementation gaps in §13.7

Four gaps between what §13.7 promises and what the tree ships. All were worked around from outside
the tree; **no script, test, or production file was modified.**

1. **The script does not emit TRX.** §13.7 says "the script … emits TRX"; `Backend/scripts/test-backend`
   passes `--results-directory` but never registers a logger. Worked around with `VSTestLogger=trx`
   in the environment, proven on a container-free probe before spending the budget.
2. **The script does not record build vs test wall time separately.** Worked around by timestamping
   every output line externally and cross-checking against MSBuild/VSTest self-reports (§5).
3. **The machine-readable lifecycle summary was never implemented.** §13.7 promises one in the
   results directory; it does not exist, and no source writes one. Consequence: template/database
   counts are a sampled lower bound (§8.4) and migration-chain application counts remain
   unmeasurable (§8.5).
4. **The prescribed Frontend command is invalid.** `--outputFile` does not exist in
   `@angular/build` 20.3.27's `unit-test` schema (`additionalProperties: false`), so §13.7's command
   exits 1 before executing anything (§3.3). This is a plan defect, not an implementation defect.

---

## 17. Intentionally unrun

### 17.1 Playwright browser E2E — deliberately not run

`Frontend/quran-dashboard-ui/e2e/` + `playwright.config.ts` (`npm run e2e`) was **not** executed.

`TESTING_STRATEGY.md` §11 makes it an **opt-in local gate, never a required lane**: not required
pre-PR, not required for release, reportable only as supplementary evidence and only when labelled
as such, and explicitly **not** a substitute for the backend route-smoke gate (§6). Plan §9
Phase 9's "Explicitly not run" clause names it directly: "Playwright E2E unless separately
triggered by a real browser-flow change." This work changed test infrastructure, scripts and
documentation — no browser-flow behavior — so no trigger fired.

A further reason not to run it opportunistically: the suite boots the real local
`quran_dashboard` database, and the Abwab specs write to it through per-test sandbox sections. It
is not a read-only measurement instrument and would have perturbed local state during a
measurement run.

### 17.2 Authorization Phase 3 tests

Not run: Phase 3 of the Authorization feature has not started (plan §9 Phase 9, "Explicitly not
run").

---

## 18. Risks and residual concerns

| # | Concern | Status |
| --- | --- | --- |
| 1 | **The failing lifecycle-contract test** (§11) and its two sibling assertions at `:105`/`:140`, plus the slot-draining loops at `:69`/`:118`. | **OPEN — blocks Phase 9 acceptance.** Remedy is the reviewer's decision. |
| 2 | **No Access-lane BEFORE baseline exists**, so §10's required Access delta is uncomputable. Required by §10 line 1503 and never established during Phase 1. | **OPEN, deferred from Phase 1.** Measured 28.75 s is below band; the delta cannot be recovered without a second broad run, which the one-run budget forbids. |
| 3 | **Migration-chain application counts unmeasurable** because §13.7's lifecycle summary was never implemented. | **OPEN, deferred.** §8.5. |
| 4 | **Per-schema creation counts unmeasurable** from Docker or `pg_database`. | **OPEN, deferred.** §8.4. |
| 5 | **Backend case-count residual of ~11 rows unattributed** (1,958 → 2,020). Baseline counting method not recorded. | **OPEN.** §7.3. Reported as a raw delta. |
| 6 | **Peak RSS is not method-comparable** to the baseline. | Disclosed, not a finding. §7.1. |
| 7 | **Script misattributes a shard-1 failure as a canonical-tier failure** (`test-backend:661-666`). | **OPEN reporting defect**, recorded not fixed. §11.5. |
| 8 | **Sampler observer effect**: 155 read-only `docker exec` calls add unquantified overhead to 369.99 s. | Disclosed. Cannot have caused §11. |
| 9 | **Benign cleanup display race** seen once during the pre-measurement sanity runs: one run reported `removed 1` because the trap's label snapshot still listed a container testcontainers had begun deleting; `docker rm --force` then succeeded, exit 0, no leftover. In the broad run and 7 of 8 sanity runs the count was `removed 0`, i.e. the test host reaches zero unaided and the trap is a genuine backstop. | Noted, not a defect. |
| 10 | **Pre-existing noise, not new**: the FluentAssertions/Xceed community-license banner in every run that loads the assertion library; the three Frontend bundle-budget warnings (§6). Both pre-date this work. | Flagged so they are not scored as regressions. |

---

## 19. Evidence index

All under `/tmp/quran-dashboard-test-runtime-after/`. Nothing was discarded or truncated.

**Broad measurement** — `run-backend-measurement.sh` (the wrapper), `backend-pre-pr.log`
(13,155 timestamped lines), `backend/_mohamed-HP-ZBook-15-G3_2026-08-06_09_25_41.trx` (shard 1),
`backend/_mohamed-HP-ZBook-15-G3_2026-08-06_09_31_36.trx` (shard 2), `docker-events.jsonl`
(501 events), `sampler.tsv`, `docker-before.txt` / `docker-after.txt` / `docker-after-owned.txt`,
`probe-trx.log`, `MEASUREMENT-SUMMARY.txt`.

**Frontend** — `frontend-typecheck.log`, `frontend-buildverify.log`, `frontend-literal-attempt.log`
(the invalid §13.7 command), `frontend-full.log`, `frontend.junit.xml`.

**Pre-measurement sanity runs** (8 focused Backend runs, 321 tests, 321 passed, 0 failed, 0 skipped;
zero containers left behind) — `01-gate-catalog.log`, `02-pg-process-contract.log`,
`03-owned-sp-registry.log`, `04-process-execution.log`, `05-process-global-state.log`,
`06-access.log`, `07-smoke.log`, `08-prepr-listtests.log`, plus derived `catalog-classes.txt`,
`discovered-classes.txt`, `discovered-tests.txt`. Results directories used `focused-*`
subdirectories so the broad run's `backend/` directory stayed clean.

---

## 20. Scope statement

- No test was run in the course of writing this report; every number above was re-derived from the
  preserved logs, TRX files, Docker event stream and JUnit XML.
- The one-run budget was spent exactly once per side. No rerun, no narrowed filter, no partial run
  relabelled as the measurement.
- No production code, test, migration, Quran source data, importer behavior, script or
  configuration was changed. No planning artifact or report was deleted.
- The implementation plan was **not** modified: plan §9 permits updating it "only if implementation
  evidence required an approved design deviation," and no deviation was approved.
- No `git add`, `git commit`, or other state-changing Git command was run. Working tree clean,
  HEAD `9ed3a5d8`.
- This file is the single file written by this step.

---

## 21. Formal-review addendum (Phase 9 reviewer, 2026-08-06)

Added by the fresh formal reviewer of §13.9 checkpoint 9. It records only evidence the reviewer
gathered that §14 previously left unevidenced, plus the adjudication of items earlier phases
deferred. It runs no broad measurement: the one-run budget was already spent and stays spent.

### 21.1 Frontend execution-gate completeness — the missing half of a §10 criterion

§10 requires that "every Backend **and Frontend** test belongs to at least one validated execution
gate." The Backend half was evidenced by the 253 ↔ 253 catalog reconciliation. The Frontend half
had no evidence in this report or in any phase report: `Frontend/quran-dashboard-ui/testing/verify-test-gates.mjs`
landed in Phase 6 (`81142242`) and nobody recorded running it.

```text
cd Frontend/quran-dashboard-ui && node testing/verify-test-gates.mjs
→ exit 0
  spec files under src/: 205
  e2e files under e2e/: 16
  full gate include: ["**/*.spec.ts"]
  feature-abwab 31, feature-auth 1, feature-dashboard 1, feature-mushaf 42, feature-words 92,
  authorization 8, composition 96, shared 38, fast 60
  PASS verify-test-gates: 205 spec file(s), 9 configurations
```

It is a hard gate, not a report: `verify-test-gates.mjs:168-172` fails when the full gate misses any
spec file, and `:174-181` fails unless every spec file lands in **exactly one** primary area
configuration. `:213-223` additionally fails a dead include pattern and any pattern that leaks an
`e2e/*.e2e.ts` file into a Vitest lane. The criterion is met on both halves. Node script, no
container, no test execution — it costs nothing against the measurement budget.

### 21.2 Coverage preservation, derived from Git across the whole range

Independent of every phase report, the test-case inventory was rebuilt from `7aba2f98~1` and from
`HEAD` and diffed. Backend: 1,319 → 1,371 **source-declared test methods** — a third metric, counted
by static attribute parsing, and deliberately not comparable to the baseline's 1,958 runtime cases,
to discovery's 1,985 entries, or to the run's 2,020 executed cases; it exists only to make removals
exact, and the +62 executed-case residual in §7.3 stays open. Exactly **nine** methods were removed,
all of them authorised by plan §8 and none of them a lost assertion.

| Removed at base | Where it went | Assertions |
| --- | --- | --- |
| `AbwabTreeReadTests.GetTreeAsync_OnFreshSchema_…` | renamed `…_OnEmptyDatabase_…` in `001ab59c` | identical three assertions; the nested per-test fixture (the baseline's 22nd PostgreSQL start) replaced by `ResetAbwabAsync()` |
| `AccessMigrationPathTests.AuthorizationPreflight_RejectsLiveSchemaDrift` | `AccessSchemaDriftTests` | `[Theory]`/`MemberData` **15 rows at base, 15 rows at HEAD**, and the 15 expected violation strings are byte-identical |
| `…AuthorizationPreflight_AcceptsAFreshlyMigratedAndSynchronizedSchema` | `AccessSchemaDriftTests` | identical |
| `…AuthorizationPreflight_RejectsARetiredCanonicalPermission` | `AccessSchemaDriftTests` | identical |
| `…CatalogueSync_ReportsARetiredCanonicalPermissionWithoutReactivatingIt` | `AccessSchemaDriftTests` | identical, including the `ReadRetiredCodesAsync` non-reactivation check |
| `…AuthorizationPreflight_UnreachableDatabase_…` | `AccessAdminCommandTests.Wrapper_UnreachableDatabase_PropagatesAControlledOperationalFailure` | exit code 4, `access_admin_failure=`, and `NotContain("   at ")` all retained, and moved from the in-process boundary to the stronger `bash scripts/access-admin` wrapper |
| `EmailIdentityContractTests.Vectors_CoverValidInvalidAndNormalizedDuplicateCases` | `EmailIdentityNormalizerTests.DuplicateVectorGroup_NormalizesToOneSharedIdentity` | duplicate-group arm strengthened to real normalizer behavior over **every** group; valid arm already covered; **invalid arm has no survivor** → `docs/TESTING_DEBT.md` row TR1 |
| `MorphologyExplorersFixtureSmokeTests.Fixture_StartsAndSeedsDatabase_Successfully` | deleted per §8 | replaced by `LemmasListReadTests`, which passed |
| `WordTypesFixtureSmokeTests.Fixture_StartsAndSeedsDatabase_Successfully` | deleted per §8 | replaced by `WordTypesMainReadTests`, which passed |

The three staged migration cases — clean-identity migration, colliding-identity refusal before the
additive migration, and the final migration's rejection of unbackfilled rows — are unchanged apart
from the runner helper name, and two new cases (`StagedMigrationSchemas_KeepOneCasesHistoryAndTablesOutOfAnother`,
`StagedMigrationCases_ReceiveAnEmptyDatabase_NotTheMigratedHeadTemplate`) now assert that the staged
lane never receives the migrated head template.

Frontend: 2,157 → 2,154 `it()` sites, the three deletions being exactly `app.sanity.spec.ts` (1 case)
and `auth.testing.spec.ts` (2 cases), both authorised by §8.

A per-commit sweep confirms deletions occurred only in the three phases entitled to them —
`001ab59c` (the rename), `3b3a8b5e` (the Access reclassification) and `a86120e1` (the consolidation).
No other commit in the range removes a test method.

### 21.3 Adjudication of items earlier phases deferred

- **`SqlDisplayWordsRebuilder.RebuildAsync` leaks its connection — still unfixed, correctly untouched.**
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/DisplayRebuilding/SqlDisplayWordsRebuilder.cs:55`
  calls `connection.OpenAsync(ct)` directly on `dbContext.Database.GetDbConnection()` instead of
  `dbContext.Database.OpenConnectionAsync()`, so EF's open-count bookkeeping never learns about it and
  no path closes it. `git log 7aba2f98~1..HEAD` over that file is empty — this prerequisite did not
  touch it, which was the right call. It is **production** work, outside this prerequisite, and is
  recorded as a follow-up in the review verdict rather than here.
- **The skip gate added in `e96496f5` is not a silent-skip hole.** Seven previously unconditional
  `[Fact]`s in `Quran.Import` became `[FoundationImportSourceFact]`, which sets `Skip` when
  `resources/import-sources/quran-foundation` is absent. All seven classes carry `Kind=Canonical` in
  `test-gates.tsv`, and `Backend/scripts/test-backend:492-495` **fails the lane with exit 1** when that
  package is missing rather than skipping it. Through any sanctioned lane the skip path is
  unreachable, which is what `TESTING_STRATEGY.md` requires; it follows the pre-existing
  `CanonicalImportSourceTestGate` / `SmokeDumpGate` pattern, and every consumer of every skip gate in
  the project is preflighted the same way. The residual reachability is direct `dotnet test`
  invocation outside the runner.
- **D5 arm 2, `ACCESS_ME_CONTRACT_FIXTURES`, and the frozen `48`** are recorded as rows TR1, TR2 and
  TR3 in `docs/TESTING_DEBT.md` — the ledger `CLAUDE.md` designates for exactly this. TR3 also records
  that the number has already drifted: the sweep is 49 cases today.

### 21.4 Independent reconciliation of the broad evidence

Both TRX files were re-parsed from scratch. Every headline figure holds: shard 1 `total=2007
executed=2007 passed=2006 failed=1 notExecuted=0`; shard 2 `total=13 executed=13 passed=13
notExecuted=0`; 252 + 1 = 253 distinct result classes; 152 `Smoke.*` route cases in shard 1 with zero
`Smoke.Data`; 13 `Smoke.Data` cases in shard 2; 115 Access cases across 19 classes; exactly one
non-passing result, `LeaseDisposal_DropsTheDatabase_AndIsIdempotent` at `00:01:06.02`. No contradiction
was found between this report, the measurement log, the Docker event stream and the diff.

### 21.5 Why the failure is structural, not merely racy — and what must not be rolled back

§11.2 is right that the assertion is the defect and the runtime is sound. The reviewer adds the
mechanism that makes it *deterministic in kind* rather than unlucky:

- `PostgreSqlTestProcessContractTests` carries **no `[Collection]` attribute**, and the project has
  no `xunit.runner.json` and no `CollectionBehavior` override. Under xUnit defaults it therefore runs
  in its own implicit collection, **in parallel with every other collection**, on 8 threads.
- `AvailableDatabaseSlots` is `databaseSlots.CurrentCount` — one process-wide `SemaphoreSlim`
  (`PostgreSqlTestServer.cs:16`, `:38`) shared by all of them. Comparing it across a window in which
  other collections lease and release cannot be made reliable by retrying.
- Worse than the assertion: `:69` and `:118` drain **every** free slot in a
  `while (AvailableDatabaseSlotsAsync() > 0)` loop and hold them. Against the 4-slot cap that starves
  every concurrently running database collection for the duration — the most plausible reading of the
  failing case's 66 s, and a standing tax on every future `pre-pr`.

The defective file is
`Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/PostgreSqlTestProcessContractTests.cs`.
It arrived in `001ab59c` (Phase 2) and was only *exposed* by the four-slot cross-collection
contention that Phases 3–4 legitimately created. **Reverting Phase 2 would delete the entire shared
runtime and is not the remedy.** The smallest correct change appears to be giving this class a
non-parallel collection of its own, which removes both the cross-collection observation window and
the starvation in one edit; rewriting the three `.Should().Be(slotsBefore)` assertions is the
alternative and does not address `:69`/`:118`. This review is review-only and prescribes neither.
