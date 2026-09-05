# Pre-PR run evidence record (#173)

Generated from `node scripts/test pre-pr` on branch `issue-173-from-149` (instrumentation commit `4ce8bd43` plus the uncommitted `AsyncLocal` stopwatch fix compiled into this run) on 2026-09-05 against provisioned `quran_dashboard_test` on `Host=/var/run/postgresql`.

Source of every number below is the `test-execution-timing` JSON line the runner emitted after the last command, except where a focused run is named.

## Result

The runner **stopped after `frontend-pre-pr`**. Backend selection completed; Playwright commands never started.

- Backend: **61 classes**, **580** executed test-case IDs in the manifest, **0** failed test runs
- `backend-build`: passed (5.662 s)
- `frontend-pre-pr`: **exit 127** (2.111 s) — `playwright: not found` while running `e2e:discover:critical` (`sh: 1: playwright: not found`). This worktree has no `Frontend/quran-dashboard-ui/node_modules/.bin/playwright`
- `playwright-typecheck`, `playwright-provision`, `playwright-canonical-critical`, `playwright-stateful-critical`: **not run**
- Process wall: **41.65 min** (`totalWallMilliseconds` 2 498 764)
- Pipeline exit was 0 only because `tee` succeeded; `frontend-pre-pr` status in the record is 127

## Did anything create a database?

**No.** After the run, PostgreSQL still had only `quran_dashboard` and `quran_dashboard_test`. No `quran_test_scratch_*`, no Docker containers.

## Fingerprint counts (recorded events, not arithmetic)

| Kind | Count | Total ms | Mean ms |
|---|---:|---:|---:|
| full | **56** | 1 927 824 | 34 425 |
| verifiedCanonical | 1 070 | 55 790 | 52 |

The plan's static inventory for an ordinary complete pre-PR is **96** full fingerprints (28 MutableWriter × 2 = 56, plus 8 mutating stateful journeys × 5 = 40).

**This run recorded 56, not 96.** That is not a reconciliation problem. The 56 matches the MutableWriter half of the enumeration exactly (28 exclusive leases, two full fingerprints per process: fixture init and final boundary check). The remaining 40 were **not executed**: stateful Playwright never ran because `frontend-pre-pr` died first. This record therefore does not confirm or refute the stateful 40; it confirms the MutableWriter 56 from events.

A standalone `TestRuntime fingerprint` against the same database in this session measured **37.460 s** (`computationKind: full`). MutableWriter full fingerprints in the gate averaged **34.425 s**.

## Leases

| Kind | Count | Wait ms |
|---|---:|---:|
| exclusive | 28 | 1 978 |
| shared | 5 | 21 |

`inChildLockWaitMilliseconds`: **1 999** (all of the above; the runner started no keepers of its own, so `lockWaitMilliseconds` stayed 0).

28 exclusive = 28 MutableWriter classes. 5 shared = 5 GuardedReader classes.

## Separated times (retained)

| Field | ms | |
|---|---:|---|
| `lockWaitMilliseconds` | 0 | no runner-started keepers |
| `provisioningMilliseconds` | 0 | `playwright-provision` not reached |
| `activeGateMilliseconds` | 2 498 758 | 41.65 min |
| `totalWallMilliseconds` | 2 498 764 | |
| `unattributedMilliseconds` | 6 | |
| `activeGateTarget` | 720 000 | `withinTarget: false` (applies; Playwright missing from the denominator as well as the numerator) |

## Machine load (run start)

Captured at `2026-09-05T16:35:37.179Z`: load averages **0.75 / 1.29 / 1.66** (1m / 5m / 15m), **8** CPUs.

## `SmokeDataReadTests`

From **this** pre-PR command (not the earlier focused run):

| | |
|---|---|
| Command wall | 13.768 s |
| full fingerprints | **0** |
| verified-canonical fingerprints | **0** |
| fixtureInit | 2.571 s |
| testBody | 5.802 s |
| boundaryCheck | 0 |
| perTestReset | 0 |

The class is a heavy read plus fixture/`inspect` preflight, not a Protected State fingerprint. Plan §11.6 records the earlier focused measurements (14.221 s wall); this full-run command was 13.768 s under load average 0.75.

## Stateful journeys

Not present in the record. `commands[].journeys` was never populated because `playwright-stateful-critical` did not run.

## Test-case manifest

`testCaseIds` length **580**, sorted, from executed TRX `Passed`/`Failed` rows only. Suitable for later set-equality on the Backend half of the gate. Playwright journey IDs are absent for the same reason Playwright did not run.

## Focused proofs that preceded this gate

1. `AccessCollectionResetContractTests`: 2 full / 1 exclusive / non-zero fixtureInit, perTestReset, testBody.
2. Focused `SmokeDataReadTests`: 0 fingerprints / 1 shared lease.
3. `TestRuntimeProtectedStateTests` was **not** run as a class: it is EmptyScratch DestructiveRehearsal and would `CREATE DATABASE` on a leased server. `computationKind: full` was instead observed via `TestRuntime fingerprint` on `quran_dashboard_test` (37.460 s).
