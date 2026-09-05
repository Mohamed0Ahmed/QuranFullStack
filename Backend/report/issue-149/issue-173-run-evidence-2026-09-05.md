# Pre-PR run evidence record (#173)

Generated from `scripts/test pre-pr` on branch `149-persistent-test-database` (instrumentation `4ce8bd43`, `24f3e1e0`, evidence propagation `7b36966b`) on 2026-09-06 against provisioned `quran_dashboard_test` on `Host=/var/run/postgresql`.

Every number below comes from the single `test-execution-timing` JSON line the runner emitted after the last command. Nothing is assembled across runs and nothing is extrapolated.

## Result

**Complete and green.** All 67 commands ran; the gate exited 0.

- Backend: **61 classes**, **580** executed test-case IDs, **0** failures
- Frontend: `frontend-pre-pr`, `playwright-typecheck` passed
- Playwright: **6** canonical journeys, **8** stateful journeys, 0 failures
- Wall: **75 min 20 s** (`totalWallMilliseconds` 4 519 991), 00:14:50 → 01:30:10

## Did anything create a database?

**No.** After the run PostgreSQL still had only `quran_dashboard` and `quran_dashboard_test`. No `quran_test_scratch_*`, no containers. `quran_dashboard` was never touched.

## Fingerprint counts (recorded events, not arithmetic)

| Kind | Count | Total ms | Mean ms |
|---|---:|---:|---:|
| full | **96** | 3 160 468 | **32 921** |
| verifiedCanonical | 1 070 | 52 424 | 49 |

**96 confirms the plan's static enumeration from recorded events for the first time.** The two halves are separable from the same record:

| Source | Count | Total ms | Mean ms |
|---|---:|---:|---:|
| MutableWriter (28 classes × 2: `fixtureInit` + `boundaryCheck`) | 56 | 1 932 571 | 34 510 |
| Stateful Playwright (8 mutating journeys × 5) | 40 | 1 227 897 | 30 697 |

The MutableWriter half is not inferred: summing `subPhases.fixtureInitMilliseconds` (991 063 ms, mean 35 395) and `subPhases.boundaryCheckMilliseconds` (941 508 ms, mean 33 625) across exactly the 28 classes reporting a non-zero boundary check yields 1 932 571 ms, and the remainder of the recorded full-fingerprint total is the stateful 40.

### This is the bottleneck, measured

Full fingerprinting is **3 160 468 ms of 4 472 622 ms of active gate — 70.7 %**. Everything else the gate does (61 backend classes, 14 journeys, builds, typechecks, provisioning) accounts for the remaining 22 minutes.

The plan's §2 sample of **42.316 s** per fingerprint overstates the measured population mean of **32.921 s** by 28.5 %. The 96 → 4 reduction remains correct in kind; its absolute saving should be projected from ~32.9 s, not ~42.3 s.

## Leases

| Kind | Count | Wait ms |
|---|---:|---:|
| exclusive | 36 | 2 488 |
| shared | 5 | 21 |

`inChildLockWaitMilliseconds`: **2 509**. `lockWaitMilliseconds` is 0 — the runner started no keepers of its own.

36 exclusive = 28 MutableWriter classes + 8 stateful journeys. 5 shared = 5 GuardedReader classes.

## Separated times (retained)

| Field | ms | |
|---|---:|---|
| `lockWaitMilliseconds` | 0 | no runner-started keepers |
| `provisioningMilliseconds` | 47 354 | `playwright-provision`, excluded from the gate |
| `activeGateMilliseconds` | 4 472 622 | 74.5 min |
| `totalWallMilliseconds` | 4 519 991 | 75.3 min |
| `unattributedMilliseconds` | 15 | |
| `activeGateTarget` | 720 000 | `applies: true`, `withinTarget: false` |

`playwright-provision` is classified `phase=provisioning` and therefore excluded from counted gate time. Reclassifying it to `activeGate` is #174's work, not this ticket's.

## Machine load (run start)

Captured at `2026-09-05T21:14:50.729Z`: load averages **1.30 / 1.70 / 1.94** (1m / 5m / 15m), **8** CPUs.

## `SmokeDataReadTests`

| | |
|---|---|
| Command wall | 13.927 s |
| full fingerprints | **0** |
| verified-canonical fingerprints | **0** |
| `fixtureInit` | 2.462 s |
| `testBody` | 5.793 s |
| `boundaryCheck` | 0 |
| `perTestReset` | 0 |

The class performs **no Protected State fingerprint of any kind**. Its cost is fixture preflight plus heavy reads: 2.462 s of fixture init and 5.793 s of test bodies, the balance being host start and teardown. The plan's 51.1 s figure is not a fingerprint and never was — a standalone `TestRuntime inspect` measures 3.66 s, and this class measured 13.768 s and 13.927 s in two separate complete runs. The gap to 51.1 s is run-to-run variance of the kind the machine-load marker exists to detect, not hidden verification work.

## Stateful journeys

Eight journeys, each reporting `applicationStartup` separated from `testExecution`:

| Journey | applicationStartup ms | testExecution ms |
|---|---:|---:|
| `abwab-inclusion-projection.e2e.ts:200` | 42 228 | 16 736 |
| `abwab-inclusion-projection.e2e.ts:288` | 42 268 | 16 942 |
| `abwab-inclusion-projection.e2e.ts:85` | 42 263 | 18 695 |
| `access-permissions.e2e.ts:44` | 44 400 | 19 291 |
| `device-session.e2e.ts:21` | 44 329 | 13 935 |
| `linking-success.e2e.ts:127` | 42 251 | 18 925 |
| `linking-success.e2e.ts:82` | 42 216 | 25 085 |
| `phrase-search-available.e2e.ts:80` | 44 413 | 21 512 |

The lane cost 1 864 031 ms (31.1 min), of which **1 227 897 ms (65.9 %) is the 40 full fingerprints**. Application startup totals 344 368 ms (42–44 s per journey, strikingly uniform) and test execution 151 121 ms (14–25 s).

## Test-case manifest

`testCaseIds` length **580**, sorted, drawn from executed TRX `Passed`/`Failed` rows only — `NotExecuted` rows are filtered out, so the manifest is suitable for later set-equality comparison.

## Why the stateful 40 were invisible before this run

An earlier complete stateful lane passed green and recorded **zero** fingerprints. Every child process in the controlled Playwright runtime is built by `createCredentialStrippedEnvironment`, an allowlist, and `QURAN_DASHBOARD_RUN_EVIDENCE_PATH` was not on it. The TestRuntime children saw no evidence path, so `RunEvidenceTelemetry.Record` took its early return and the 40 fingerprints were computed but never recorded.

`7b36966b` allowlists the evidence path and command ID alongside the existing `QDB_PR_OBSERVATION_RESULT_DIR`. Neither name matches `SENSITIVE_ENVIRONMENT_NAMES`, so redaction is unchanged and no credential surface widens. `verify-controlled-playwright-runtime.mjs` now asserts both survive stripping, so the propagation cannot silently regress.

A single focused journey confirmed the fix before the full run: **5** full fingerprints and its exclusive lease, where the same journey previously emitted none.

## Known instability

One canonical lane execution aborted with `Internal CLR error. (0x80131506)` in the API host, immediately after `Application started`. Six tests then failed with `ECONNREFUSED ::1:5015` — all downstream of that single runtime abort. It did not reproduce: the same lane passed on retry in 73.6 s and again in this complete run. No OOM, 9.9 GiB free, and `QuranDashboard.Api` does not reference the telemetry assembly, so it is unrelated to the instrumentation. It occurred once in six lane executions and is a gate-stability risk worth tracking independently of the performance effort.

## Superseded record

An earlier invocation on 2026-09-05 (branch `issue-173-from-149`) stopped at `frontend-pre-pr` with **exit 127** — `playwright: not found`, because that worktree had no `Frontend/quran-dashboard-ui/node_modules`. It recorded 56 full fingerprints (the MutableWriter half only) in 41.65 min and could neither confirm nor refute the stateful 40. This record supersedes it.
