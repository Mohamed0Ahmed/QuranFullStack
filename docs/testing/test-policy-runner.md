# Executable test policy and repository runner

Repository-root `scripts/test` is the additive policy-aware coordinator for the Test Database
Capability migration. It does not activate the TestRuntime cutover: the existing Backend and sealed
Playwright commands remain the operational implementation for entries explicitly marked `Unmigrated`
until the atomic cutover ticket lands. Absence of policy metadata is never treated as an execution
default.

Backend classes are catalogued in
`Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv`. A migrated row declares one
of `FastNoDb`, `CanonicalReader`, `GuardedReader`, `MutableWriter`, or `DestructiveRehearsal`, its data
reads and writes, database target, and destructive subtype. An unmigrated row must leave all policy
fields blank and explicitly say `Unmigrated`. Fixture/resource effects are independently catalogued in
`test-resources.tsv`; migrated resources declare setup writes, reset behavior, target, and API startup
effects. The effective Backend policy is the strictest valid combination.

Playwright uses `canonical-read`, `guarded-read`, or `mutating` plus one `fixture-policy` annotation.
The fixture profiles and temporary legacy inventory live in `e2e/playwright-policy.json`. The inventory
pins each unmigrated E2E source by SHA-256, so changing or adding a test requires either policy metadata
or an explicit inventory update. Artifact and `read-only` annotations remain accepted only on these
hash-pinned legacy sources until their migration tickets land.

Focused implementation examples preserve exact selectors:

```bash
scripts/test focused --backend-class QuranDashboard.Tests.Api.Access.AccessRolesTests --build
scripts/test focused --backend-test QuranDashboard.Tests.Api.Access.AccessRolesTests.Some_case --no-build
scripts/test focused --playwright e2e/mushaf-reader.e2e.ts:277 --no-build
```

Migrated Backend readers require `ConnectionStrings__QuranDashboardTest` to name the verified local
`quran_dashboard_test` capability. The Backend delegate runs `QuranDashboard.TestRuntime inspect`
before test execution and supplies a short-lived runner context to the fixture. Reader services use the
restricted reader role with read-only transactions; API-backed fixtures use the Testing `ReadOnly`
activity profile. `GuardedReader` fixtures additionally retain the shared TestRuntime advisory lock for
their lifetime. Missing capability state fails before VSTest starts, and no reader command provisions,
migrates, restores, seeds, or starts a database writer.

Focused Playwright selection remains inside the existing sealed provisioning, credential-stripping,
loopback-egress, and sanitized-evidence runner while the database lifecycle is still legacy.

Pre-PR mode always plans the required Backend tier, contract, Frontend policy/build, Playwright
typecheck, and critical Chromium gates. Add only the affected pipeline/contract scope:

```bash
scripts/test pre-pr --feature FoundationImport --dry-run
scripts/test pre-pr --concern Schema --dry-run
scripts/test pre-pr --policy scheduled --dry-run
```

The plan partitions selections in this order: FastNoDb, CanonicalReader, GuardedReader, MutableWriter,
empty-scratch DestructiveRehearsal, and full-data DestructiveRehearsal. Legacy entries are shown in a
separate temporary partition. In both focused and pre-PR modes, full-data work is omitted and reported
in `authorizationRequired` unless the operator reviews the dry run and repeats the command with
`--authorize-full-data`. Merely having a canonical pipeline in the repository never selects it.

The empty-scratch partition is executable rather than plan-only. For each exact Backend selection,
`scripts/test` holds the TestRuntime global exclusive lock, removes only receipt-verified crash leftovers,
creates a runner-owned PostgreSQL 18 database from `template0`, supplies its receipt-validated context to
the selected test process, and performs verified cleanup before releasing the lock. Migration-path,
Permission catalogue reconciliation, and schema-drift classes are the initial migrated empty-scratch
coverage. They remain cheap pre-PR candidates when Access or Schema scope selects them; the partition does
not authorize or select any unrelated full canonical pipeline.

Migrated full-data PhraseSearch index and recovery selections use the manual full Rehearsal Database
capability only after `--authorize-full-data`. The root runner asks TestRuntime to validate the dedicated
rehearsal connection, recompute its Protected State fingerprint, verify its subtype, provenance, migration,
and freshness markers, and retain the cluster-wide exclusive lock around the exact selected test. Missing
or stale capability state fails only that selected command with manual provisioning or refresh guidance.
The runner never provisions or removes the target, and a failed target stays available for explicit
inspection; database removal requires the separately confirmed TestRuntime cleanup command.
