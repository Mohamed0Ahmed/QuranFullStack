# Pre-PR gate performance plan

Authoritative plan for reducing pre-PR gate execution time. Written 2026-09-05 against
`149-persistent-test-database` at commit `8d42c28c`.

**Nothing here is implemented.** No product code, test code, or TestRuntime behaviour has been changed.
This document is the sole input for specification and ticketing; it assumes no prior context.

---

## 1. Purpose and scope

The ordinary pre-PR gate takes far longer than its 12-minute target. This plan establishes why, what may
be changed, what may not, and in what order — with a measured checkpoint at every step.

**In scope:** the test runner (`scripts/test`, `scripts/test-policy-runner.mjs`), the Backend delegate,
xUnit fixture dispatch, the Playwright orchestration scripts, the TestRuntime CLI, the job matrix, and the
two governing documents.

**Out of scope:** product code, and any change that weakens a safety guarantee in §9.

---

## 2. Verified sequential baseline

One complete `node scripts/test pre-pr` invocation, no full Rehearsal Database provisioned:

| | |
|---|---|
| Backend test classes / tests / failures | 61 / 580 / 0 |
| Planned commands, all exit 0 | 67 |
| Canonical Playwright | 6 / 6 |
| Stateful Playwright | 8 / 8 |
| Databases created | **0** |
| **Measured sequential local baseline** | **83 m 43.5 s** |

This is one sequential `scripts/test pre-pr` invocation — a diagnostic, not the §4.1 target metric. No MAX
figure exists yet; the aggregate coordinator (§7, P8) is the first thing that can produce one.

The 83 m 43.5 s counts the 77.1 s of automated Playwright provisioning, per the timing definition in §4.1.

**Run-to-run variance is large.** An earlier complete run of the identical commands measured 47 m 17 s.
Per-operation cost varies with cache and machine load by roughly 3×. Every projection in this document
must be read against that.

### 2.1 Measured block costs

| Block | Classes | Elapsed |
|---|---:|---:|
| MutableWriter | 28 | 41 m 52.9 s |
| FastNoDb | 27 | 2 m 06.8 s |
| GuardedReader | 5 | 1 m 37.5 s |
| CanonicalReader | 1 | 12.7 s |

| Command | Elapsed |
|---|---:|
| `backend-build` | 15.0 s |
| `frontend-pre-pr` | 32.7 s |
| `playwright-typecheck` | 2.6 s |
| `playwright-provision` | 1 m 17.1 s |
| `playwright-canonical-critical` | 1 m 13.7 s |
| `playwright-stateful-critical` | 34 m 32.4 s |

### 2.2 Direct measurements

| Measurement | Value |
|---|---|
| One full Protected State fingerprint (`TestRuntime fingerprint`) | **42.316 s** total, 25.49 s user CPU |
| `TestRuntime inspect` | 3.66 s |
| Ordered JSON text, `quran_phrase_variants` (largest canonical table), server-side | 1 368 351 rows, 610 924 031 bytes, 13.3 s |
| Canonical corpus | 39 tables, ~4.7 M rows, ~2.4 GB |

MutableWriter class duration is essentially independent of test count: a 1-test class took 1 m 26 s and a
39-test class 1 m 44 s — an intercept near **85 s** and a slope near **0.47 s/test**. The cost is per
*process*, not per test.

---

## 3. The bottleneck

### 3.1 What the fingerprint does

`ProtectedStateFingerprint` serializes every row of all 39 Canonical Quran Data tables as
`row_to_json(row)::text`, orders them under `COLLATE "C"`, and SHA-256s the stream with a 4-byte
big-endian length prefix before each component name, table name, and row
(`ProtectedStateFingerprint.cs:537-543`). It also hashes System Catalogue (2 tables) and schema state.

A second entry point, `ComputeWithVerifiedCanonicalAsync`, **substitutes** a caller-supplied canonical
digest and reads no canonical row (`ProtectedStateFingerprint.cs:126-131`). It hashes only catalogue and
schema, and is correspondingly cheap.

### 3.2 How often the gate pays it

Every `ProtectedStateFingerprint.Compute*` call site was enumerated. The gate performs **96 full
fingerprints**:

- **56 from MutableWriter.** `AccessTestFixture.InitializeAsync` computes one as the verified boundary
  (`AccessMutableWriterTest.cs:115`) and `VerifyFinalProtectedStateAsync` computes another
  (`AccessMutableWriterTest.cs:494`). The runner emits **one `dotnet test` process per class**
  (`selectionCommand` in `scripts/test-policy-runner.mjs`), so 28 classes × 2 = 56.
- **40 from stateful Playwright.** Per mutating scenario: `TestRuntime fingerprint` (1), `reset --phase
  initial` (before + after = 2), `reset --phase final` (2). The CLI reset path passes
  `verifiedCanonicalQuranDataFingerprint: null` (`MutableStateResetter.cs:53`), so each recomputes the
  whole corpus. All 8 critical stateful journeys are `mutating`, so 8 × 5 = 40.

Per-test resets inside the fixture already use the cheap verified-canonical path, and the three
`*CollectionResetContractTests` call the cheap variant too. Neither inflates the count.

**The count of 96 is verified static fact. The duration attributed to it is extrapolation** from one
42.3 s sample and is contradicted in magnitude by the 47 m 17 s run (§2). Direction is certain;
arithmetic is not. Establishing the real per-call cost is the first work item (§7, P0).

### 3.3 The secondary costs

1. **`dotnet test` process startup, 61×.** FastNoDb classes measure 3.0–4.3 s each with no database, no
   lock and no fixture — close to pure CLR/assembly-load/discovery cost.
2. **Collection-fixture construction, 28×.** All 28 MutableWriter classes share one
   `ICollectionFixture<AccessTestFixture>`; xUnit builds it once per *process*, and the runner supplies 28.
3. **Per-scenario application stack, 8×.** Roughly 47 s per stateful scenario for API startup, static
   frontend server, browser launch and the journey. Playwright's `webServer` starts once per child, and
   the stateful runner spawns one child per scenario.
4. **Playwright discovery, 3×** across the two lanes and focused selection.

---

## 4. Settled decisions

These are owner decisions. They are not re-litigated by this plan.

### 4.1 Timing definition

**The pre-PR timing target is the elapsed time of the required parallel gate, measured as MAX across the
required branches** — the gate *makespan*: a common start, eligible branches run concurrently, stop when
the last required branch completes. It is **not** the sum of one sequential local invocation.

Each branch's measured time **includes all automatic work in its normal gate execution**:

- build and setup performed by the gate (locked restore, `npm ci`, `dotnet build`)
- automated Playwright provisioning (`npm run e2e:provision`)
- application startup
- database preparation and reset performed as part of the gate
- test execution

**Excluded, and only this:**

- advisory-lock wait caused by **external** contention — contention between two required branches of the
  same gate is self-contention and is **not** excludable
- explicit or manual one-time Test Database capability provisioning or refresh that is not part of an
  ordinary pre-PR run (for example `rehearsal hold` validating a manually provisioned full Rehearsal
  Database)

**Governing principle: the reported number may not be improved by reclassifying normal automated gate work
as "provisioning".**

Shared resources do not make MAX undefined; serialization simply makes MAX larger.

**Consequence for shipped code.** `scripts/test-policy-runner.mjs` plans `playwright-provision` with
`phase: 'provisioning'`, and `summarizeExecutionTiming` excludes provisioning from the reported gate time.
`npm run e2e:provision` is automated work in an ordinary run, so **it must be reclassified to
`activeGate`**. The `phase` field remains useful with a corrected meaning: it distinguishes *automated gate
work* (counted) from *manual capability provisioning* (excluded).

`pr-observation-matrix.json` already encodes this definition — `durationScope` states that provisioning,
database preparation, application startup and test execution are included — as does
`docs/testing/risk-based-strategy.md:417-418`. `docs/testing/persistent-test-database-architecture.md:482-483`
contradicts it and must be corrected to match.

### 4.2 Execution topology

**One PostgreSQL instance. One `quran_dashboard_test`. One lock-bearing branch.**

All work that takes the advisory lock runs in a single required branch, so the gate cannot contend with
itself. Genuinely lock-free work runs in parallel branches.

| Branch | Contents | Lock |
|---|---|---|
| **Lock-bearing** | MutableWriter (28), GuardedReader (5), stateful critical Playwright (8) | exclusive + shared |
| **Backend lock-free** | FastNoDb (27), CanonicalReader (1), canonical critical Playwright (6) | none |
| **Frontend** | `frontend-pre-pr`, `playwright-typecheck` | none |
| **Contract/model** | `check-api-contract`, `check-pending-model` | none |

Verified lock behaviour:

- `PersistentTestDatabaseReader(guarded: true)` acquires a **shared** lock; `guarded: false` returns before
  acquiring (`PersistentTestDatabaseReader.cs:50`). GuardedReader is guarded; CanonicalReader is not.
- All five GuardedReader classes share the single `SmokeDataCollection` fixture.
- The canonical Playwright lane runs `TestRuntime inspect` and spawns its child with **no keeper**
  (`run-canonical-playwright.mjs:85-106`).
- The control lock is a **cluster-wide singleton by protocol**: every keeper is retargeted to the
  `postgres` database and ownership verification demands that database's OID
  (`AdvisoryLockProtocol.cs:64-65, 104-110, 182-195`). Separate databases on one instance would still
  contend.

**A common preparation phase is required before fan-out.** Locked restore, `npm ci`, `dotnet build`, the
frontend production build, and `npm run e2e:provision` all write shared outputs; in particular
`e2e:provision` writes the single fixed receipt
`Frontend/quran-dashboard-ui/.playwright/provisioning/controlled-receipt.json`
(`provision-controlled-playwright.mjs:17-18`). Running branches concurrently in one checkout without a
common preparation phase races on those outputs, independently of the database lock. Preparation time
counts toward the makespan (§4.1) and must run **after** the builds whose outputs the receipt hashes.

### 4.3 Safety decisions

**Canonical attribution for MutableWriter batching.**

- **Block-level canonical corruption attribution is accepted.** After batching, a canonical violation is
  attributed to the MutableWriter block, not to an individual class.
- **Full canonical corruption detection remains mandatory.** The full fingerprint at both ends of the
  batched block is not negotiable, and no change may reduce coverage of the canonical table set.
- **Immediate per-class canonical attribution is not required.**

A per-class *catalogue and schema* boundary check remains worthwhile and should be added — described
precisely as what it is. It cannot provide canonical attribution, because the verified-canonical path
substitutes the canonical digest without reading canonical rows (§3.1).

---

## 5. The optimizations

Each entry states what it changes, what it preserves, and its projected effect. **All timings are
projections until measured** (§8).

### B — Batch MutableWriter into one process

All 28 classes carry `[Collection(nameof(MutableDatabaseCollection))]` over one
`ICollectionFixture<AccessTestFixture>`, and the collection is declared with `DisableParallelization = true`
(`AccessMutableWriterTest.cs:526-527`). Running them in one `dotnet test` process collapses 56 full
fingerprints to 2.

Preserved by xUnit's own semantics: exclusive advisory locking (one lease for the block rather than 28
sequential leases), per-test reset cadence (`BeginScenarioAsync`/`EndScenarioAsync` are per test and are
untouched), serialization within the collection, and test semantics.

Changed: canonical attribution becomes block-level (§4.3); process isolation between classes is lost, so
static state now persists across all ~288 tests; one run ID covers the block; a hang stops the block rather
than one class.

Required with it: a set-equality discovery assertion (the classes a batched filter discovers must equal
exactly the selected set) and the per-class catalogue/schema boundary check described below.

**The class-boundary mechanism.** xUnit 2.9.3 provides no per-class hook for a *collection* fixture:
`IAsyncLifetime` on a test class is per **test** (a new instance per test), and the collection fixture
lives for the whole collection. `IClassFixture<T>` is genuinely per class but cannot reach the collection
fixture's lease or verified canonical digest, because xUnit does not inject collection fixtures into
class-fixture constructors. Neither is usable.

The seam that is usable already exists. All 28 classes derive from one of exactly four base classes, and
all four funnel through the same choke point:

| Base class | Classes |
|---|---:|
| `Api/Access/AccessMutableWriterTest.cs:18` | 14 |
| `Abwab/AbwabMutableWriterTest.cs:6` | 6 |
| `Api/Linking/LinkingMutableWriterTest.cs:8` | 4 |
| `Smoke/SmokeMutableWriterTest.cs:10` | 4 |

Every one calls `Fixture.BeginScenarioAsync(…)` and `Fixture.EndScenarioAsync()`. Pass the test class
identity (`GetType()`) through `BeginScenarioAsync`, have the fixture track the last class it saw, and run
the catalogue/schema boundary check whenever that identity changes, plus once at block end. That is **four
one-line changes, not 28**, and yields **28 checks** — 27 class transitions plus the final block boundary.

**The dependency to hold:** this is a class-*transition* boundary, not a lifecycle hook. It relies on xUnit
grouping test cases by class within a collection, which `XunitTestCollectionRunner` does and which
`DisableParallelization = true` makes deterministic. If a future change introduces interleaving within the
collection, this mechanism must be revisited.

### C — Verified-canonical resets via a resident TestRuntime session

The stateful lane recomputes the full canonical hash five times per scenario because the CLI `reset` path
passes `verifiedCanonicalQuranDataFingerprint: null`. The in-process fixture already avoids this by passing
a real `ProtectedStateFingerprintReport` (`MutableStateResetter.cs:56-81`).

**Design: a resident TestRuntime session.** One TestRuntime process acquires the lease, computes the full
fingerprint once, retains the verified report **in memory**, serves bounded reset requests for the scenario,
and performs the final full check before releasing.

**A caller-supplied `--verified-canonical-fingerprint` CLI flag is explicitly not the design.** It would
accept two caller-controlled strings, so a caller holding the lock could supply mutually consistent
fabricated values and skip reading canonical state entirely. The resident session keeps lock ownership and
verified state in the same process and is not forgeable.

Scope: per scenario. Each scenario already acquires its own keeper and *then* computes its own fingerprint
(`run-stateful-playwright.mjs:143-170`), so the retained value never crosses a lock boundary. **C requires
no change to lease granularity, run IDs, or failure handling.** It takes the lane from 40 full fingerprints
to 8 full plus 32 cheap.

### A — Batch FastNoDb into one process, with parallelization disabled

27 classes with no database, no lock and no fixture, each currently paying full process startup.

**A hazard that must be handled in the same change.** There is no `xunit.runner.json` and no assembly-level
`[CollectionBehavior]`, so xUnit's default applies: distinct collections run **in parallel**. These 27
classes carry no `[Collection]` attribute, so each is its own implicit collection. `ProcessGlobalStateScopeTests`
mutates process-wide state — `Directory.SetCurrentDirectory`, `Console.SetOut`/`SetError`,
`Environment.SetEnvironmentVariable` (`TestSupport/Process/ProcessGlobalStateScopeTests.cs:8-53`).
Batching without disabling parallelization would run it concurrently with the other 26 while it swaps the
process's working directory and console streams. Today this is invisible only because one class per process
gives each its own CLR.

Disabling parallelization for the batched command reproduces today's fully-serial semantics exactly.

### D — Batch GuardedReader into one process

All five classes share the single `SmokeDataCollection` fixture, which owns one shared lock. One process
instead of five; one fixture construction and one `TestRuntime inspect` preflight instead of five.

### F — One lease and one boundary fingerprint for the stateful lane

After C the lane still performs 8 full fingerprints, one per scenario. Acquiring a single exclusive lease
for the whole lane, computing the full fingerprint once at lane start and once at lane end, reduces that
to 2.

Preserved: scenarios still run strictly one at a time; resets still run with the API stopped; each scenario
still spawns its own Playwright child, so `webServer` still starts a fresh API.

Changed, and required as part of the change:

1. **Run-ID protocol.** Each scenario currently mints its own run ID used for the keeper, the Playwright
   environment and the reset, and reset verifies ownership by run ID and command
   (`MutableStateResetter.cs:131-156`). A lane-wide keeper needs either one run ID across all scenarios or
   an explicit split between lane lock identity and per-scenario evidence identity.
2. **Aggregate failure protocol.** Every scenario currently releases its keeper in `finally`, including
   after malformed evidence or an unidentified API (`run-stateful-playwright.mjs:247-287`). A lane-wide
   keeper must guarantee final cleanup, final full verification and release across signals and partial
   child failure, without starting the next scenario on dirty state.

Longer exclusive hold is a real trade: stronger isolation, larger blast radius.

### N — Byte-identical framed-`bytea` streaming

`AppendQueryAsync` reads each row with `reader.GetString(0)`, and `Append` immediately calls
`Utf8.GetBytes(value)` — every row is decoded from UTF-8 to a UTF-16 string and re-encoded back, millions
of times over ~2 GB. Having PostgreSQL return `convert_to(row_to_json(…)::text, 'UTF8')` as `bytea`,
reading the bytes, and framing them client-side exactly as now yields a **byte-identical digest by
construction** — no algorithm change, no cross-check problem, no server memory risk — while removing the
round-trip and its allocations.

This targets the 25.49 s of user CPU directly. Its saving is unmeasured.

### G — Explicit concurrent scheduling

Schedule the branches of §4.2 with their dependencies made explicit, rather than firing commands and
letting the lock arbitrate. Must not manufacture lock wait that then disguises real cost.

### E — Discover Playwright policies once per run

Discovery currently runs up to three times. Run once and pass the result to both lanes.

### H — Derive affected scope from the diff

`planPrePrSelection` selects all 61 classes unconditionally as required candidates; `--feature`/`--concern`
only *add* Pipeline and EmptyScratch classes and are hand-typed. Deriving them from the diff as an
overridable default makes the existing rule usable.

**This must never narrow a required gate.** After B its speed value is near zero; it is a correctness-of-
intent change.

---

## 6. Rejected options

| Option | Why rejected |
|---|---|
| Server-side `string_agg` hashing | Not byte-equivalent — the algorithm length-frames every value (`ProtectedStateFingerprint.cs:537-543`) — and chunk digests are not composable into the same SHA-256, so a "both algorithms agree" cross-check is incoherent. Ordered JSON for one table alone is 611 MB, too large for naive aggregation. |
| Moving the control lock into the target database | Unsafe. Scratch creation takes the lock *before the scratch database exists* and issues `CREATE DATABASE … TEMPLATE …` from a `postgres` maintenance connection (`ScratchDatabaseLifecycle.cs:245-258, 284-292, 690-701`). Refresh takes it before creating the staged database and renames both databases while holding it (`CapabilityRefresher.cs:134-171, 488-555`); a keeper connected to the target would itself block the rename. Scratch, persistent, staged-refresh and rehearsal databases would occupy separate lock namespaces. Ownership verification is bound to the `postgres` OID (`AdvisoryLockProtocol.cs:182-195`; `ContractValidation.cs:153-157`). |
| Separate PostgreSQL instances | Not rejected on merit, but it is infrastructure the architecture does not describe and it is not required by the settled topology (§4.2). If ever adopted, each instance keeps its own `postgres` control lock — never per-target locks. |
| Redefining the target to the sequential local duration | Contradicts §4.1. The sequential duration stays a useful diagnostic and must never be reported as the official target. |
| Reusing one API across stateful scenarios | Violates fresh-API-per-scenario. |
| Running stateful scenarios in parallel | Violates one-scenario-at-a-time. |
| Narrowing the canonical fingerprint's table set | Weakens the protected-state guarantee. |
| Caching a fingerprint to disk between runs | Weakens the before/after guarantee. |
| Using `pg_stat_*` or stored markers as invalidation proof | Statistics lag and reset; stored markers cannot detect out-of-band owner or superuser corruption. Useful as a hint, never as verification. |

**The database contract does not change.** `targets.testDatabase`, the fixed advisory-lock key, and
`advisoryLock.database = postgres` all stay as they are.

---

## 7. Sequence and checkpoints

Every phase ends with a **measured** checkpoint from the timing evidence record. No phase is complete on
inspection.

**P0 — Instrument.** No behaviour change. Emit per-fingerprint timing from TestRuntime and per-phase timing
from the backend fixture and the stateful lane. Resolve why `SmokeDataReadTests` took 51.1 s where its four
peers took 8.8–18.4 s (it is not a fingerprint — `TestRuntime inspect` measures 3.66 s — so it is a genuinely
heavy read).
*Checkpoint:* the run report states from evidence, not arithmetic, how many full fingerprints occurred and
what each cost. Expect 96.

**P1 — Partition and phase correction.** Add planner-only `lock-bearing` and `lock-free` partitions to
`scripts/test`. Assert their Backend test-ID union equals the required 580-test manifest exactly, their
journey union equals the full critical catalogue, and both intersections are empty. Point
`pr-observation-matrix.json` at the explicit partitions. Reclassify `playwright-provision` to `activeGate`
and correct `persistent-test-database-architecture.md` to match `risk-based-strategy.md` and the matrix.
*Checkpoint:* union/intersection assertions pass; no journey planned twice; the timing record reports on
the corrected basis. Runs no gate and touches no database behaviour.

**P2 — B: MutableWriter batching.** With the set-equality discovery assertion and the per-class
catalogue/schema boundary check.
*Checkpoint:* block ≤ 330 s; exactly 2 full fingerprints in the block; **28** boundary checks present and
passing (27 class transitions plus the final block boundary); same 288 tests, 0 failures; one exclusive
lease for the block.

**P3 — C: resident TestRuntime session.**
*Checkpoint:* stateful lane performs 8 full fingerprints, not 40; 8 scenarios still serial; 8 distinct API
process receipts with 8 distinct PIDs; `protectedStateMatches: true`; `dumpFilesRetained: 0`.

**P4 — A: FastNoDb batching with parallelization disabled.**
*Checkpoint:* block ≤ 20 s; 27 classes and the same test count reported; `ProcessGlobalStateScopeTests`
passes under repeat runs.

**P5 — D: GuardedReader batching.**
*Checkpoint:* GuardedReader ≤ 80 s; one shared lease; one `inspect` preflight.

**P6 — F: lane-wide stateful lease.** With the run-ID and aggregate-failure protocols.
*Checkpoint:* exactly 2 full fingerprints in the lane; full verification at both lane boundaries; clean
release proven across an induced mid-lane failure.

**P7 — N: framed-`bytea` streaming.**
*Checkpoint:* digest byte-identical to the current algorithm on `quran_dashboard_test`; fingerprint cost
measured before and after.

**P8 — Aggregate coordinator.** Common preparation phase, then concurrent branch execution, measuring
every branch from the common start. First point at which MAX can be measured at all.
*Checkpoint:* a MAX figure exists, derived from a real aggregate run.

**P9 — G, E, H.** Remaining levers, measured individually.

---

## 8. Timing projections

**These are projections, not forecasts.** They derive from one 42.3 s fingerprint sample against measured
block times, and §2 records ~3× run-to-run variance. They are superseded by the first real measurement.

Lock-bearing branch = MutableWriter + GuardedReader + stateful Playwright. MAX ≈ common preparation +
lock-bearing branch, since it dominates every other branch.

| Configuration | Lock-bearing branch | MAX (incl. measured preparation) |
|---|---:|---:|
| B + C + A + D | ~18 m 02 s | ~19 m 34 s |
| + F | ~13 m 48 s | ~15 m 20 s |
| + F + N (fingerprint cost halved) | ~12 m 24 s | ~13 m 56 s |
| + F + N (fingerprint cost → 0; an upper bound N cannot reach) | ~10 m 59 s | ~12 m 31 s |

Other branches: Backend lock-free ≈ 97 s; frontend ≈ 35 s; contract/model unmeasured. Measured common
preparation is 92.1 s (`backend-build` + `e2e:provision`); `dotnet restore`, `npm ci` and the frontend
production build are **unmeasured** and add to every row.

### 8.1 Twelve minutes is not reachable on current evidence

Even driving remaining fingerprint cost to zero — an upper bound N cannot achieve — leaves ~12 m 31 s
before unmeasured restore and install costs. The floor is set by work the guarantees protect: ~380 s of
per-scenario API and browser startup across 8 stateful journeys, plus the MutableWriter block's
non-fingerprint residual.

Once B, C and F land, **fingerprinting stops being the binding cost.** The plan still takes the gate from
83 m 43 s to roughly 14 minutes — an ~83 % reduction — with no guarantee weakened.

**The honest checkpoint is "target unmet until an aggregate run proves otherwise", not a projected
figure.** Whether the 12-minute target is right for this topology is a question to revisit with
measurements in hand.

---

## 9. Non-negotiable guarantees

Regardless of the target:

1. **Protected State fingerprints** — same algorithm, same tables, same ordering and normalization. A
   **full** Protected State fingerprint is computed at both ends of **every exclusive lease**: once after
   acquisition, establishing the verified boundary, and once before release, which must match. Within a
   single continuously held lease, resets may reuse the canonical component captured at that lease's
   start, provided the value is held in-process and never crosses a lease boundary. Failure to complete
   the closing full verification fails the lease. Any transport change must produce a byte-identical
   digest. Never narrow the table set.
2. **Canonical corruption detection preserved in full.** Canonical *attribution* is block-level by
   decision (§4.3); catalogue and schema attribution is preserved per class.
3. **Reset contracts** — reset runs with the API stopped, proves it, verifies before and after, retains no
   dump.
4. **Role restrictions** — reader role for hashing, application role for writes, unchanged.
5. **Advisory locking** — exclusive for writers, shared for readers, no lane running without its lease.
   Granularity may change only as a reviewed decision, and only toward holding longer.
6. **Fresh API per stateful scenario, one scenario at a time.**
7. **Test classification rules** — `test-gates.tsv` and `test-resources.tsv` remain authoritative, enforced
   by `TestPolicyContractTests` and `verify-test-policy-runner.mjs`. Batching changes how classes are
   *dispatched*, never how they are *classified*.
8. **No test removed, skipped, or de-scoped for time.** Assert an exact test-ID manifest — set equality
   before and after, rejecting skipped or not-run cases. A count or a floor is insufficient: it permits
   substitutions, duplicates and silent skips.
9. **No artifacts, Testcontainers, database cloning, or fallback databases.** Zero databases created remains
   a pass criterion.
10. **No product-code change.**
11. **Discovery assertions stay as strong** — set equality, never a relaxed filter.
12. **`--blame-hang` stays a hang detector, not a command budget.** Keep the 20-minute timeout and add a
    separate outer block timeout; inflating it to batch duration weakens fail-closed behaviour.
13. **An over-target number is reported honestly.**

---

## 10. Telemetry

**Retain:** the separated times (lock wait / excluded provisioning / counted gate time / total wall), the
per-command phase, the target comparison, emission after a *failing* command, and the rule that a telemetry
defect never flips a verdict.

**Add:**

1. **Fingerprint counters** — full and verified-canonical counts and total ms in each. This is the single
   number tracking the whole effort; it should fall from 96 full to 4.
2. **An exact test-case manifest** — captured before and after batching, asserted equal (§9.8).
3. **Sub-phase timings inside batched commands** — fixture init, boundary checks, per-test reset, test body.
   Batching removes the per-command boundary that provided per-class timing for free.
4. **Per-scenario phases in the stateful lane**, with `applicationStartup` separated from `testExecution`.
5. **A machine-load marker** at run start. Before/after comparison is meaningless without it, given ~3×
   variance.
6. **In-child lock waits**, currently unaggregated and more important once the block holds one long lease.
7. **A branch dimension and a MAX calculation.** The record currently describes one sequential invocation
   and cannot evidence the §4.1 target at all.

---

## 11. Remaining uncertainties

Recorded, not resolved. None blocks P0–P7.

1. **All durations are projections** from a single 42.3 s sample against ~3× observed variance (§8). P0
   converts this plan from hypothesis to measurement.
2. **The 12-minute target is not reachable** by the changes in this plan (§8.1). Whether the target or the
   topology should change is a decision to take with measurements in hand.
3. **`api-contract-model` is unmeasured** and could independently be the binding branch.
4. **`dotnet restore`, `npm ci` and the frontend production build are unmeasured**, and all three count
   toward the makespan.
5. **N's saving is unmeasured.** Its mechanism is sound and its digest is identical by construction, but
   the magnitude is unknown until P7.
6. **`SmokeDataReadTests` is not a fingerprint.** A focused `scripts/test` run of that class on
   2026-09-05 (`issue-173-from-149`, `quran_dashboard_test`) measured **14.221 s** command wall, **0**
   full fingerprints, **0** verified-canonical fingerprints, and **1 shared lease** (5 ms wait).
   Sub-phases: fixtureInit **2.370 s**, testBody **6.032 s**, boundaryCheck **0**, perTestReset **0**.
   The remaining ~5.8 s sits outside those four buckets (process startup, discovery, and the
   GuardedReader `inspect` preflight). The earlier **51.1 s** figure was a different run's wall time,
   not Protected State verification (`TestRuntime inspect` was already known to be ~3.66 s; a full
   fingerprint in the same session cost ~39.7 s).
7. **No CI exists.** There is no Actions/GitLab/Jenkins/CircleCI configuration in the repository;
   `pr-observation-matrix.json` is a provider-neutral specification and
   `scripts/run-pr-observation-job.mjs:34-44` executes exactly one job per invocation. The aggregate
   coordinator in P8 is the first thing that could measure MAX.
8. **Static-state independence across batched classes is unproven.** No MutableWriter class currently
   mutates process-global state, but random-order, repeat-run and failure-recovery behaviour under batching
   has not been demonstrated. P2's checkpoint should include a repeat run.
