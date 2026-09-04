# Persistent Full-Data Test Database Architecture

**Status:** Accepted architecture; artifact/container lifecycle contracted

**Decision date:** 2026-09-03

**Scope:** Entire Quran Dashboard monorepo

**Decision:** [ADR 0002](../adr/0002-persistent-full-data-test-database.md)

This document defines the target test database architecture agreed for Quran Dashboard. It is a design
and migration contract, not a description of the current runner. Until the migration is completed and
activated atomically, code, project manifests, and operational READMEs remain the truth for commands
that still exist.

## Objectives

The architecture must:

- Exercise ordinary Backend and Playwright behavior against the complete local Quran dataset.
- Keep Canonical Quran Data, the System Catalogue, and Schema State unchanged during ordinary tests.
- Reset only explicitly classified Mutable Application State.
- Serialize database mutation across Backend, Playwright, shell tooling, and future runners.
- Prevent automated tests from reading or changing the developer's working database.
- Preserve destructive importer, migration, catalogue, recovery, index-build, and schema-drift coverage
  without provisioning PostgreSQL containers or restoring test artifacts.
- Fail closed when database identity, role, schema, catalogue, lock, marker, or capability state differs
  from the committed contract.
- Retain independent correctness oracles and useful non-database execution sealing.

The architecture does not:

- Make the developer database disposable.
- Rebuild, clone, restore, recreate, migrate, or re-import the persistent Test Database during an
  ordinary test run.
- Treat a before/after fingerprint as an independent Quran correctness oracle.
- Introduce an automated repair path for Protected State.
- Imply PostgreSQL 16 support from the retired Testcontainers configuration.
- Add another Frontend testing convention beside Playwright.

## Database topology

| Database | Ownership and use | Ordinary automated behavior |
| --- | --- | --- |
| `quran_dashboard` | Developer-owned local development data and authored work | Never connected to, read, reset, copied, migrated, or mutated |
| `quran_dashboard_test` | Persistent PostgreSQL 18 Test Database Capability containing complete canonical and catalogue data | Read by ordinary database tests; only Mutable Application State may be reset or changed |
| `quran_test_scratch_<run-id>` | Temporary runner-owned database created from `template0` | Used only by eligible empty-scratch Destructive Rehearsals, then receipt-bounded cleanup |
| Explicit full Rehearsal Database | Manually provisioned, non-authoritative full-data database | Used only by an explicitly selected full-data destructive lane; never automatically created, refreshed, or dropped |

The stable name `quran_dashboard_test` describes the logical capability. Its physical database may be
replaced only by the explicit, guarded refresh workflow described below.

## Data classes

The shared vocabulary is defined in [CONTEXT.md](../../CONTEXT.md). Every application table must appear
in exactly one class in the committed database contract.

### Canonical Quran Data

All `quran_*` tables are protected, including Quran text/navigation, words and morphology, tafsir and
translation, i'rab and mutashabihat, derived display tables, and PhraseSearch build/state/index data.
Ordinary tests may read them but may never insert, update, delete, truncate, rebuild, or reset them.

At the decision date the class contains these 39 tables:

`quran_surahs`, `quran_ayahs`, `quran_mushaf_pages`, `quran_mushaf_lines`, `quran_words`,
`quran_words_ordered_tashkeel`, `quran_words_ordered_simple`, `quran_words_unique_tashkeel`,
`quran_words_unique_simple`, `quran_word_morphology`, `quran_word_morphology_segments`, `quran_roots`,
`quran_lemmas`, `quran_lemma_analyses`, `quran_stems`, `quran_pos_tags`, `quran_i3rab_rules`,
`quran_mutashabihat_groups`, `quran_mutashabihat_occurrences`, `quran_similar_ayah_links`,
`quran_tafsir_sources`, `quran_tafsir_entries`, `quran_tafsir_ayah_entries`,
`quran_translation_sources`, `quran_translation_ayah_entries`, `quran_juzs`, `quran_hizbs`,
`quran_rubs`, `quran_sajdas`, `quran_full_i3rab_sources`, `quran_full_i3rab_entries`,
`quran_full_i3rab_ayah_entries`, `quran_phrase_index_builds`, `quran_phrase_index_state`,
`quran_phrase_search_tokens`, `quran_phrase_variants`, `quran_phrase_occurrences`,
`quran_phrase_similarity_edges`, and `quran_phrase_similarity_anchor_stats`.

### System Catalogue

`roles` and `permissions` are protected catalogue state. Ordinary scenario resets preserve them.

A healthy catalogue has:

- Exactly one canonical Owner Role with its stable identity and metadata.
- Every code-defined Permission present, active, and matching its canonical metadata.
- No duplicate, missing, stale, or retired-canonical Permission.
- No active unknown Permission.
- Unknown Permissions only when retired for historical-reference preservation.

Reconciliation is an explicit maintenance operation. It is never an API startup side effect during
tests. Mutating PostgreSQL coverage for reconciliation runs only as a scratch Destructive Rehearsal.

### Mutable Application State

At the decision date this class contains 36 tables.

Access:

- `users`
- `user_permissions`
- `user_device_sessions`
- `access_audit_events`

Abwab:

- `abwab_sections`
- `abwab_doors`
- `abwab_door_aliases`
- `abwab_door_relations`
- `abwab_door_inclusions`
- `abwab_door_inclusion_unit_syncs`
- `abwab_templates`
- `abwab_template_nodes`

Linking:

- `linking_workspaces`
- `linking_workspace_sources`
- `linking_workspace_source_manual_ayahs`
- `linking_workspace_source_ayah_overrides`
- `linking_workspace_source_words`
- `linking_workspace_source_descriptions`
- `linking_operations`
- `linking_confirmation_jobs`
- `linking_door_ayahs`
- `linking_door_ayah_words`
- `linking_source_contributions`
- `linking_units`
- `linking_source_contribution_units`
- `linking_unit_ayahs`
- `linking_unit_ayah_words`
- `linking_unit_ayah_descriptions`
- `linking_data_state`
- `linking_prepared_preflights`
- `linking_prepared_sources`
- `linking_prepared_units`
- `linking_prepared_ayahs`
- `linking_prepared_ayah_words`
- `linking_prepared_ayah_descriptions`
- `linking_prepared_affected_contributions`

The database contract, not this snapshot, becomes executable truth. A new mapped table must be
classified before contract validation passes.

### Schema State

Schema State includes `__EFMigrationsHistory`, migrations, tables, columns, indexes, constraints,
extensions, sequence definitions and counters, and other database objects. Ordinary test execution
does not apply migrations or rewind sequences. Tests consume identifiers returned by the application;
they do not depend on identities restarting at `1`.

## Authoritative Test Database provisioning

Provisioning and refresh are explicit maintenance operations. They never read or copy
`quran_dashboard`.

The refresh workflow is:

1. Acquire the global exclusive test-runtime advisory lock.
2. Validate the selected administrative principal, local PostgreSQL 18 server, contract version, target
   names, and absence of unknown target sessions.
3. Create `quran_dashboard_test_refresh_<run-id>` from `template0`.
4. Apply the committed migrations in order.
5. Run the repository's canonical import, rebuild, and generation pipeline in its supported order using
   repository-authorized source inputs.
6. Construct the migration-seeded Role catalogue and reconcile the code-defined Permission catalogue.
7. Initialize every ordinary Mutable Application State table empty.
8. Set `linking_data_state` to exactly `id=1`, `generation=1`, and the Unix epoch timestamp.
9. Validate the complete capability: migration head, extensions, table classification, grants, System
   Catalogue health, canonical counts/invariants, Protected State fingerprint, and independent oracles.
10. Install the database-scoped capability metadata and restricted grants on the staged database.
11. Require the current `quran_dashboard_test` database to be idle; never terminate unknown sessions.
12. Rename the old capability aside, rename the fully verified staged database to
    `quran_dashboard_test`, and reverify its canonical name, marker, roles, metadata, and fingerprints.
13. Roll the name back if the second rename or post-swap verification fails.
14. Remove the old database only as part of the explicitly confirmed maintenance operation.

The physical replacement is atomic from the supported runner's perspective. No ordinary runner invokes
any part of this workflow. Capability inspection defaults to read-only/dry-run; mutation requires an
explicit apply mode.

### Maintenance authority

Provisioning does not add a fifth test role. An explicitly selected existing local database owner or
administrative login performs the maintenance operation. It uses elevated authority only for the staged
target and guarded swap, stores no new credentials, and grants no test role ownership or mutation rights
on the Development Database.

### Database-scoped capability metadata

The Test Database records its capability metadata in database-scoped PostgreSQL settings rather than an
application table or filesystem receipt. The committed contract defines the exact setting names. The
metadata includes:

- Capability/reset enablement.
- Canonical pipeline identity and input provenance hashes.
- Canonical Quran Data and System Catalogue fingerprints.
- Migration head.
- Refresh timestamp.
- Database-contract schema version.

The earlier design candidate's source-database identity is deliberately absent: provisioning has no
source database. Ordinary roles may read but not change these settings.

## Database contract and control plane

`Backend/testing/test-database-contract.json` will be the committed executable contract. It must declare:

- Every mapped application table exactly once under one data class.
- The deterministic `linking_data_state` baseline.
- Stable test-role names and expected privileges.
- Database names, scratch prefixes, and capability/rehearsal markers.
- The fixed cluster-wide advisory-lock key.
- Allowed database targets and Destructive Rehearsal subtypes.
- Contract and capability metadata versions.

Contract validation compares the manifest with the EF model and, for database lanes, the live PostgreSQL
catalogue. Missing, duplicate, unclassified, or unexpectedly classified tables fail before execution.

`Backend/tools/QuranDashboard.TestRuntime/` will be the sole implementation authority for:

- Capability inspect, dry-run, apply, and verify operations.
- Lock acquisition, ownership verification, and holder diagnostics.
- Protected-state fingerprints.
- Mutable reset and baseline validation.
- Empty-scratch lifecycle.
- Full-rehearsal validation and explicit cleanup.
- Structured safety, timing, and evidence reports.

Backend, Playwright, shell tooling, and future runners call this control plane. They do not duplicate
table allowlists, SQL reset logic, safety checks, lock constants, or fingerprint selection.

## Roles and privilege boundaries

One idempotent local administration command provides inspect, dry-run, apply, and verify modes. It
creates stable NOLOGIN capability roles and grants an explicitly selected local login membership:

| Role | Intended authority |
| --- | --- |
| `quran_dashboard_test_reader` | Read Canonical Quran Data, System Catalogue, Schema State, and permitted Mutable Application State |
| `quran_dashboard_test_application` | Reader authority plus DML only on the reviewed Mutable Application State allowlist |
| `quran_dashboard_test_resetter` | Narrow reset authority for the mutable allowlist and deterministic singleton baseline |
| `quran_dashboard_test_scratch_admin` | Create and own approved scratch databases; never own the Development or persistent Test Database |

Supported runners use `SET ROLE`; they introduce no stored passwords. The privilege verifier proves that
ordinary roles cannot change Canonical Quran Data, the System Catalogue, or Schema State. The reset role
cannot expand its target through `CASCADE`.

## Safety preflight

Before any ordinary database mutation, the control plane requires all of the following:

- Loopback host or local Unix socket.
- Exact database name `quran_dashboard_test`.
- PostgreSQL major 18; the minor version is reported but not pinned.
- Server not operating in recovery/read-replica mode.
- Exact database-scoped test capability/reset marker.
- Expected restricted role and verified privilege matrix.
- Current committed migration head with no pending or unknown migration.
- Healthy System Catalogue.
- Complete table classification and valid capability metadata.
- Verified exclusive advisory-lock ownership for the current run ID.

A mismatch fails closed with a capability report and an actionable maintenance instruction. Ordinary
runners never apply migrations, reconcile the catalogue, provision data, start a container, clone a
database, restore a dump, or fall back to another target.

Ordinary test runs never connect to `quran_dashboard`, even read-only. Only explicit Development Database
operations outside automated testing may target it.

## Advisory-lock protocol

One fixed PostgreSQL advisory-lock key coordinates all supported tools on the PostgreSQL cluster. A
dedicated keeper connection holds a session-level lock and identifies itself with the run ID and command
in `application_name`.

| Effective policy | Lock mode |
| --- | --- |
| `FastNoDb` | None |
| `CanonicalReader` | None, provided every startup/background writer is disabled and the reader role is used |
| `GuardedReader` | Shared |
| `MutableWriter` | Exclusive |
| Scratch or full `DestructiveRehearsal` | Exclusive |
| Capability provision/refresh or System Catalogue reconciliation | Exclusive |

Lock acquisition has a configurable 15-minute default timeout. Wait diagnostics identify the holder and
wait duration. The timeout applies to acquisition, not to an interactive session after acquisition.
PostgreSQL releases the lock if the keeper connection dies.

Fixtures and child processes verify the expected run ID's lock ownership before any reset or mutation.
Direct unsafe execution fails with instructions to use the supported runner. Catalogue reconciliation
always takes the global lock before its narrower transaction-level catalogue lock.

## Mutable reset contract

The exclusive keeper lock spans the entire mutating invocation. A stateful scenario reset occurs only
while its API host and database-writing background services are stopped.

For each stateful scenario the control plane:

1. Verifies the API is stopped and background work is drained.
2. Verifies lock ownership and the Test Database safety preflight.
3. Truncates the explicit 35-table Mutable Application State allowlist excluding
   `linking_data_state`.
4. Uses `CONTINUE IDENTITY` and `RESTRICT`; it never uses `RESTART IDENTITY` or `CASCADE`.
5. Updates the existing `linking_data_state` row to its deterministic baseline.
6. Asserts that the allowlisted scenario tables are empty and that exactly one valid singleton remains.
7. Starts a fresh API host with the scenario's required activity profile.

After the scenario, the host stops before reset. Final cleanup runs even after test failure. Resets are
generated from the committed database contract, never from fixture-local SQL.

## Testing-only API activity profiles

The Testing environment requires one explicit profile; missing or unknown values fail startup.
Production composition is unchanged.

| Profile | Behavior |
| --- | --- |
| `ReadOnly` | Permission synchronization disabled, every Linking processor/cleanup service omitted, reader role and read-only transactions |
| `Mutable` | Permission synchronization disabled; only processors explicitly required by the scenario enabled; cleanup services disabled unless their behavior is under test |
| `DestructiveRehearsal` | Allowed only against a validated scratch or full Rehearsal Database with the explicitly selected maintenance behavior |

Canonical and guarded readers use the genuine `ReadOnly` composition. A service that can issue writes on
startup cannot remain registered merely because it is expected to find no work.

## Protected-state fingerprints and independent oracles

Mutating and full-verification invocations stream one Protected State fingerprint before the first reset
and another after final cleanup. No dump file is retained. The fingerprint covers:

- All Canonical Quran Data.
- Ordered System Catalogue contents.
- Schema definitions, constraints, indexes, extensions, and migration history.
- Protected sequence state where applicable.

It excludes Mutable Application State and mutable-table sequence counters. Commands proven to be
entirely canonical read-only do not require a before/after fingerprint.

A matching fingerprint proves only that the run did not alter Protected State. Correctness remains
grounded in small, independently reviewed Quran and PhraseSearch expectations under repository-root
`test-oracles/`, with provenance and hashes but no dump, artifact ID, container identity, or provisioning
coupling. Expected values are never regenerated from the database under test.

## Executable test policy

### Backend

Every Backend test class declares one policy:

- `FastNoDb`
- `CanonicalReader`
- `GuardedReader`
- `MutableWriter`
- `DestructiveRehearsal`

It also declares the data classes read and written, database target, and destructive subtype when
applicable. Fixture/resource metadata independently declares setup writes, reset profile, database
target, and API startup effects. The effective policy is the strictest combination of class intent and
fixture behavior.

Contract rules include:

- Ordinary readers have no writes.
- `MutableWriter` may write only Mutable Application State.
- Writes to Canonical Quran Data, System Catalogue, or Schema State require
  `DestructiveRehearsal` plus an approved subtype and target.
- Under-classified fixture setup fails validation even when the test body looks read-only.

All MutableWriter classes belong to one `MutableDatabaseCollection`. Its collection fixture validates
capability and exclusive-lock ownership; per-test `IAsyncLifetime` setup/teardown enforces host shutdown,
reset, and cleanup. GuardedReader collections hold shared locks. CanonicalReader collections use the
reader role and ReadOnly profile. Assembly-wide parallelization remains enabled.

### Playwright

Every Playwright test declares exactly one state policy:

- `canonical-read`
- `guarded-read`
- `mutating`

Artifact annotations are removed. Missing, contradictory, or under-classified annotations fail contract
validation. Fixture setup counts toward the effective policy, so a test whose authentication fixture
creates a user is mutating even if its browser action only reads.

The runner groups canonical-read tests under a reusable ReadOnly API host. Each guarded-read or mutating
scenario runs as an exact `file:line` child while the TestRuntime keeper retains the outer shared or
exclusive lock. A mutating child receives its verified reset before API startup, owns a fresh API
lifecycle, and is followed by host shutdown and cleanup. Per-child private evidence directories are
validated and aggregated into one sanitized run report.

## Repository runner and lanes

Repository-root `scripts/test` is the supported orchestration entry point. Existing
`Backend/scripts/test-backend` and Frontend npm commands remain only as thin delegates.

After one build, mixed selections are partitioned and executed deterministically:

1. `FastNoDb`
2. `CanonicalReader`
3. `GuardedReader`
4. `MutableWriter`
5. Empty-scratch `DestructiveRehearsal`
6. Full-data `DestructiveRehearsal` only when explicitly requested

Safe framework-level parallelism remains inside FastNoDb and reader partitions. Cross-partition
concurrency is not initially permitted; a later change requires measured evidence and explicit review.

### Pre-PR selection policy

During feature implementation, run focused tests for the affected scope only. Before opening a pull
request, run the required risk-based gates. The existence of a canonical importer, rebuild, generator,
similarity builder, PhraseSearch build, or other full-data pipeline does not by itself select that
pipeline for every pre-PR run.

A full canonical import, rebuild, or generation Destructive Rehearsal runs before a pull request only
when the changed scope affects that pipeline, its authorized source data, Schema State, consumed or
produced contracts, or safety-critical behavior. It may also run when explicitly selected by a scheduled
or release policy. Otherwise it remains outside the ordinary pre-PR selection.

Cheap, risk-relevant empty-scratch tests may remain eligible for pre-PR, including focused importer,
migration, System Catalogue reconciliation, and schema-drift coverage. This eligibility must not expand
into rebuilding every complete canonical dataset on every pull request. A subtype may move to scheduled
execution only after timing evidence and a risk review identify its replacement protection.

Full-data destructive rehearsals remain explicit and manual. They are never part of ordinary pre-PR
execution unless separately authorized.

Every database-backed focused class, method, or Playwright `file:line` invocation uses `scripts/test` or
a thin delegate. FastNoDb tests may run directly. Explicit headed/UI wrappers provide read-only and
mutating modes; a mutating interactive session holds the exclusive lock until it closes and then performs
verified final cleanup.

## Destructive Rehearsals

Approved subtypes are canonical import/rebuild/generation, migration, System Catalogue reconciliation,
schema drift, PhraseSearch index build, and recovery.

### Empty scratch

Eligible rehearsals create `quran_test_scratch_<run-id>` from `template0` on the existing local
PostgreSQL server. The database has the strict prefix, run ID, expected owner, and recorded receipt. It
may be automatically removed after execution. Crash cleanup may drop it only when all recorded identity
and ownership checks agree.

### Full rehearsal

Full-data index/recovery work requires a manually and explicitly provisioned Rehearsal Database. Its
marker declares canonical pipeline provenance, Protected State fingerprint, migration head,
provisioning timestamp, and intended subtype. The runner recomputes and verifies those values before
mutation, fails with refresh instructions on mismatch, and never builds, refreshes, or drops the database.

A failed full rehearsal remains available for inspection. A separate explicit cleanup command shows the
exact target before removal. Recovery rehearsals may create a temporary full backup only as the object of
that rehearsal; evidence retains its hash and source fingerprint, and the payload is deleted before
successful completion.

## Failure states

- Protected State mismatch stops further mutation and emits diagnostic hashes. There is no automatic
  repair or restore.
- Mutable cleanup failure marks the run failed and the capability dirty. A later supported mutating run
  may recover only through a successful verified initial reset while Protected State still matches.
- A live or unverified API process prevents reset. The runner uses bounded shutdown, verifies process and
  port absence, and only then attempts cleanup.
- Keeper failure relies on PostgreSQL session-lock release; the next run revalidates capability and
  mutable baseline.
- Scratch cleanup remains bounded by receipt, owner, prefix, and run ID.
- Full rehearsal failure preserves its database for explicit inspection and cleanup.

## Timing and evidence

Reports separate:

- Advisory-lock wait.
- Capability/manual provisioning time.
- Active gate time: build, preflight, fingerprints, resets, application startup/shutdown, tests, and
  final cleanup.
- Total wall time.

The 12-minute target applies only to active pre-PR gate time. Lock contention and manual provisioning are
operational evidence, not suite execution cost. The target is measured rather than allowed to weaken
correctness or safety.

Structured reports retain hashes, identities, phases, sanitized failures, and timings. They never retain
database dumps, credentials, tokens, request/response bodies, or private keys.

## Retained non-database execution protections

Playwright retains:

- Locked npm/NuGet inputs and exact Chromium provisioning.
- Build/provisioning receipt validation.
- Ephemeral local TLS.
- Credential stripping and isolated process homes.
- Loopback-only egress enforcement.
- Prebuilt output hashing.
- Sanitized structured diagnostics and private temporary browser output.

PostgreSQL image acquisition, Docker network/container identity, database dump restoration, artifact
verification, and artifact receipt fields are removed. The execution mode should be renamed if “sealed”
would imply a hermetic database fixture.

## Cutover

Implementation proceeds in reviewable phases on one integration branch:

1. Add the contract, TestRuntime control plane, capability administration, and contract tests.
2. Reclassify and migrate Backend tests and fixtures.
3. Migrate Playwright orchestration and independent oracles.
4. Rewrite pre-PR, nightly, release, and evidence contracts.
5. Remove Testcontainers, test artifacts, artifact tooling, dumps, manifests, and obsolete documentation.
6. Switch every supported entry point to the new runner and verify the complete gates.

Activation is atomic. The redesign is not complete while any supported command can select or fall back
to the old container/artifact lifecycle.

## Superseded documentation register

ADR 0002 is the architecture authority now. Ticket #169 removed the container and dump lifecycle. The
documents and contracts below were rewritten or removed with that contraction.

| Document or contract | Superseded scope | Required disposition |
| --- | --- | --- |
| `docs/adr/0001-playwright-only-frontend-testing.md` | Compact-fixture and hermetic-database requirements, plus the hard-gate status of the 12-minute budget | Retain Playwright-only, Chromium, risk-placement, oracle decisions, and the 12-minute measured target; cross-reference ADR 0002 |
| `docs/testing/test-artifacts.md` | Entire test artifact trust, acquisition, restore, and reset contract | Remove; ADR 0002 and this document replace it |
| `docs/testing/test-artifact-manifest.schema.json` | Artifact manifest schema | Remove |
| `docs/testing/test-artifacts-lock.schema.json` | Artifact lock schema | Remove |
| `docs/testing/compact-phrase-search-ready-candidate.md` | Entire compact overlay candidate | Remove after extracting independent expectations to `test-oracles/` |
| `docs/testing/quran-fidelity-oracle-candidate.md` | Artifact packaging, deltas, hashes, container compatibility, and verification commands | Preserve reviewed oracle values/provenance under `test-oracles/`; remove obsolete candidate document |
| `docs/testing/risk-based-strategy.md` | Testcontainers placement, compact/full artifact policy, hard outer timeout, database hermeticity/modes, artifact recovery, and artifact evidence clauses | Rewrite around persistent capability, policy metadata, advisory locking, scratch/rehearsal databases, and measured active-gate timing; retain the risk hierarchy, test-layer ownership, independent-oracle, security, accessibility, and sanitized-evidence principles |
| `docs/testing/pr-observation-matrix.md` | Active artifact provisioning/input contract, parallel database job assumptions, and old timing boundary | Rewrite; retain historical observations as dated evidence where still useful |
| `docs/testing/nightly-risk-lane.md` | Artifact-root input and disposable-container execution contract | Rewrite around explicit capabilities and rehearsal databases |
| `docs/testing/release-candidate-lane.md` | Full-canonical artifact verification and disposable recovery/upgrade targets | Rewrite around explicit rehearsal capabilities |
| `docs/testing/previous-release-migration-upgrade.json` | `quran-canonical` artifact identity, hashes, size, and artifact-seeded rehearsal contract | Replace with a scratch/rehearsal capability contract or remove if the adopted rehearsal no longer needs a standalone declaration |
| `docs/testing/previous-release-migration-upgrade.schema.json` | Required artifact shape in the previous-release declaration | Replace or remove with its declaration |
| `Frontend/quran-dashboard-ui/e2e/README.md` | Artifact/clone-local database modes, container restore, and live-API reset procedure | Rewrite for TestRuntime-controlled persistent capability and per-scenario host lifecycle; retain sanitized evidence guidance |
| `Backend/tools/QuranDashboard.TestArtifacts/README.md` | Entire TestArtifacts tool contract | Remove with the tool |
| `Backend/scripts/README.md` | Test-artifact commands, Testcontainers lanes, full-canonical restore/recovery, and artifact-generation sections | Rewrite those sections only; retain unrelated operational database commands |
| `docs/testing/dependency-advisory-evaluation.md` | References to the TestArtifacts project as an audited development dependency | Update the inventory for TestRuntime; retain the advisory policy |

Machine-readable companions that must change atomically with their human documentation:

- `test-artifacts.lock.json`: remove.
- `pr-observation-matrix.json`: rewrite for policy-partitioned Test Database Capability execution.
- `nightly-risk-lane.json`: rewrite without artifact-root or container provisioning.
- `release-candidate-lane.json`: rewrite around explicit Rehearsal Database inputs.
- Artifact-dependent verification and runner scripts: replace or remove with their owning contracts.

Historical verification reports remain historical evidence; they do not define the target architecture
and must not be used to restore an artifact fallback.
