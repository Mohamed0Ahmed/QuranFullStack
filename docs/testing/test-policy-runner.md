# Executable test policy and repository runner

Repository-root `scripts/test` is the policy-aware coordinator for Backend and Playwright execution.
Every Backend catalog row is `Migrated`. Playwright state-policy execution is classified; absence of
policy metadata is never treated as an execution default.

Backend classes are catalogued in
`Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv`. A migrated row declares one
of `FastNoDb`, `CanonicalReader`, `GuardedReader`, `MutableWriter`, or `DestructiveRehearsal`, its data
reads and writes, database target, and destructive subtype. Fixture/resource effects are independently
catalogued in `test-resources.tsv`; every resource declares setup writes, reset behavior, target,
and API startup effects. The effective Backend policy is the strictest valid combination.

Playwright uses `canonical-read`, `guarded-read`, or `mutating` plus one `fixture-policy` annotation.
The fixture profiles live in `e2e/playwright-policy.json` and record setup writes, reset behavior, API
startup effects, and enabled background activities. The legacy inventory is empty: every E2E source is
classified, and authentication/persona fixtures that create or change users require a mutating profile.

Focused implementation examples preserve exact selectors:

```bash
scripts/test focused --backend-class QuranDashboard.Tests.Api.Access.AccessRolesTests --build
scripts/test focused --backend-test QuranDashboard.Tests.Api.Access.AccessRolesTests.Some_case --no-build
scripts/test focused --playwright e2e/mushaf-reader.e2e.ts:412 --no-build
```

Migrated Backend readers require `ConnectionStrings__QuranDashboardTest` to name the verified local
`quran_dashboard_test` capability. The Backend delegate runs `QuranDashboard.TestRuntime inspect`
before test execution and supplies a short-lived runner context to the fixture. Reader services use the
restricted reader role with read-only transactions; API-backed fixtures use the Testing `ReadOnly`
activity profile. `GuardedReader` fixtures additionally retain the shared TestRuntime advisory lock for
their lifetime. Missing capability state fails before VSTest starts, and no reader command provisions,
migrates, restores, seeds, or starts a database writer.

Focused `CanonicalReader` Playwright selection now runs through the persistent capability path. The
delegate performs `QuranDashboard.TestRuntime inspect`, starts one reusable Testing `ReadOnly` API host
against `quran_dashboard_test`, and keeps Playwright's two-worker parallelism. The API applies the
restricted reader role and read-only transactions, with Permission synchronization and every Linking
background writer omitted. Canonical execution acquires no advisory lock and never provisions, restores,
resets, migrates, or rebuilds database state. Guarded and mutating selections run as separate exact
`file:line` children. A guarded child retains one shared TestRuntime keeper around a fresh `ReadOnly`
API lifecycle. A mutating child retains one exclusive keeper, receives a centralized verified reset
before API startup, starts a fresh `Mutable` API with only its declared activities, and proves that API
stopped before verified final cleanup. Per-child private evidence is aggregated without retaining raw
Playwright output.

Pre-PR mode always plans the required Backend tier, contract, Frontend policy/build, Playwright
typecheck, controlled Playwright provisioning, the persistent canonical-read critical Chromium gate, and
the stateful critical gate. Provisioning is planned deliberately after `backend-build` and
`frontend-pre-pr`: the controlled receipt hashes the built Backend and Frontend outputs, so any command
that rebuilds them after the receipt is written would invalidate it and the controlled Playwright lanes
would refuse to start.
The direct `Backend/scripts/test-backend pre-pr` delegate execs this coordinator. Empty-scratch
rehearsals stay out of ordinary pre-PR unless an affected feature or concern selects them.
Add only the affected pipeline/contract scope:

```bash
scripts/test pre-pr --feature FoundationImport --dry-run
scripts/test pre-pr --concern Schema --dry-run
scripts/test pre-pr --policy scheduled --dry-run
```

The plan partitions selections in this order: FastNoDb, CanonicalReader, GuardedReader, MutableWriter,
empty-scratch DestructiveRehearsal, and full-data DestructiveRehearsal. In both focused and pre-PR modes, full-data work is omitted and reported
in `authorizationRequired` unless the operator reviews the dry run and repeats the command with
`--authorize-full-data`. Merely having a canonical pipeline in the repository never selects it.

The empty-scratch partition is executable rather than plan-only. For each exact Backend selection,
`scripts/test` holds the TestRuntime global exclusive lock, removes only receipt-verified crash leftovers,
creates a runner-owned PostgreSQL 18 database from `template0`, supplies its receipt-validated context to
the selected test process, and performs verified cleanup before releasing the lock. Migration-path,
Permission catalogue reconciliation, schema-drift, foundation import/rebuild, and navigation import
classes use this path. Foundation and navigation source, manifest, path, write-isolation, and validation
rules that need no PostgreSQL remain independently selectable `FastNoDb` classes. Their empty-scratch
rehearsals stay outside ordinary pre-PR execution unless the affected feature, authorized source, Schema
State, produced/consumed contract, or safety-critical scope selects them; the partition does not authorize
or select an unrelated full-data pipeline.

After each empty-scratch command, the runner emits one `empty-scratch-test-execution` JSON record that
binds the selected feature, concerns, exact class or method, run ID, subtype, lifecycle step statuses, test
outcome, per-phase and total timings, sanitized failure/violation codes, and verified cleanup result.
Missing, malformed, mismatched, or unsuccessful lifecycle evidence fails the command even if its child
process exits zero. The aggregate copies only allowlisted scratch identity and status fields from
lower-level reports; connection strings, credentials, diagnostic messages, database rows, dumps, and
other payloads are never retained.

Morphology and enriched-morphology imports, display-word rebuilds, simple i'rab generation, and full i'rab
imports also use the empty-scratch partition. Their database-writing classes apply committed migrations
inside the receipt-bound scratch database and never provision a PostgreSQL server or target
`quran_dashboard_test`. Parsing, normalization, assembly, artifact, and manifest checks that do not need a
database remain `FastNoDb`. Ordinary pre-PR selection excludes the destructive pipeline classes; affected
feature, source, schema, contract, or safety scope selects them, as does an explicit scheduled/release
policy.

Migrated full-data PhraseSearch index and recovery selections use the manual full Rehearsal Database
capability only after `--authorize-full-data`. The root runner asks TestRuntime to validate the dedicated
rehearsal connection, recompute its Protected State fingerprint, verify its subtype, provenance, migration,
and freshness markers, and retain the cluster-wide exclusive lock around the exact selected test. Missing
or stale capability state fails only that selected command with manual provisioning or refresh guidance.
The runner never provisions or removes the target, and a failed target stays available for explicit
inspection; database removal requires the separately confirmed TestRuntime cleanup command.

## Reported timing

Every planned command declares the phase its elapsed time belongs to. `playwright-provision` is
`provisioning`; every other command is `activeGate`. After the last command -- and also after a command
that fails -- the runner emits one `test-execution-timing` JSON record. It reports these four times
separately:

| Reported time | What it covers |
| --- | --- |
| `lockWaitMilliseconds` | Advisory-lock contention, as measured by the TestRuntime keepers the runner starts |
| `provisioningMilliseconds` | Capability/manual provisioning and capability validation, excluding lock wait |
| `activeGateMilliseconds` | Build, preflight, resets, application startup/shutdown, tests, and final cleanup, excluding lock wait and capability validation |
| `totalWallMilliseconds` | Wall time for the whole invocation |

Lock wait is taken from the `advisoryLock.waitMilliseconds` the keeper itself reports, not from how long
the keeper process took to start: process startup, connection, and contract inspection are the command's
own cost, not contention. A command may also spend part of its elapsed time validating a manually
provisioned capability -- `rehearsal hold` recomputes the Protected State fingerprint before it takes the
lock -- and that portion is reported as provisioning even though the command belongs to the active gate.

The runner reports the lock wait it starts itself in `lockWaitMilliseconds`. Lock waits a child
process acquires for its own lifetime -- a Backend writer lane, or a controlled Playwright lane --
are aggregated separately as `inChildLockWaitMilliseconds`.

The same record also carries, from recorded events rather than arithmetic:

| Field | What it covers |
| --- | --- |
| `fingerprints.full` | Count and total milliseconds of full Protected State fingerprints |
| `fingerprints.verifiedCanonical` | Count and total milliseconds of verified-canonical fingerprints |
| `leases.exclusive` / `leases.shared` | Advisory-lease counts and waits by kind |
| `testCaseIds` | Exact executed test-case IDs, sorted for later set-equality comparison |
| `commands[].subPhases` | Fixture init, boundary checks, per-test reset, and test body |
| `commands[].journeys` | Stateful-lane per-journey `applicationStartup` and `testExecution` |
| `machineLoad` | Load averages and CPU count captured at run start |

`unattributedMilliseconds` reports whatever wall time the per-command records do not account for, so the
figures above are never made to add up by construction. The record also carries `activeGateTarget` and
the per-command breakdown in `commands`.

`activeGateTarget` records the 12-minute target: it `applies` only in pre-PR mode, and `withinTarget`
compares it against `activeGateMilliseconds` alone. Lock contention and manual provisioning are
operational evidence and never count toward it, and the target is measured rather than allowed to weaken
correctness or safety.
