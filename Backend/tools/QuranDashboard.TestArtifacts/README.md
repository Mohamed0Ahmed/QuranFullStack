# Test artifact trust tool

`QuranDashboard.TestArtifacts` consumes the repository-root `test-artifacts.lock.json` trust
catalogue. Its default `status` and `verify` commands are read-only and never open a database
connection. The explicit full-canonical provisioning commands are local-first, scheduled/release
operations; provisioning is the only command that copies or restores an artifact.

Use the repository wrapper from any directory:

```bash
Backend/scripts/test-artifacts status
Backend/scripts/test-artifacts status --lane critical
Backend/scripts/test-artifacts verify --artifact compact-cross-stack-base
QURAN_TEST_ARTIFACT_ROOT=/private/qdb-artifacts Backend/scripts/test-artifacts provision-full-canonical --run scheduled --database-connection-file /private/postgres.connection --database-container qdb-full-canonical-scheduled --staging-root /private/qdb-staging --receipt /private/qdb-receipt.json
env -i PATH="$PATH" HOME=/tmp QURAN_DASHBOARD_FULL_CANONICAL_NETWORK=blocked Backend/scripts/test-artifacts verify-full-canonical --run scheduled --database-connection-file /private/postgres.connection --database-container qdb-full-canonical-scheduled --staging-root /private/qdb-staging --receipt /private/qdb-receipt.json
```

`status` validates the lock, resolves the requested lane or artifact, checks staged existence and
exact size, and compares the locked migration head/count with the repository. `verify` additionally
checks every SHA-256, strictly parses the artifact manifest, validates table identifiers, and compares
manifest identity, migration, producer, table scope, provenance, sentinels, and optional PhraseSearch
state with the lock.

Every selected artifact is reported as `present`, `missing`, `stale`, or `mismatched`, followed by a
summary. Exit code `0` means every selected artifact is present and trusted, `1` means the request
failed closed, and `2` means the command line is invalid. An explicit lane or artifact not present in
the lock fails closed.

The lock and manifest schemas are tracked under `docs/testing/`. Lock entries must use repository-
relative staged paths, lowercase SHA-256 values, strict PostgreSQL identifiers, and credential-free
 immutable storage identities ending in `@sha256:<hash>` or `@version:<provider-version>`. A
PhraseSearch lock entry records only the manifest hash, source
 fingerprint, and readiness expectation; its volatile `activeBuildId` belongs only in the hashed
 artifact manifest.

`provision-full-canonical` applies only to `scheduled` or `release`. It selects only locked artifacts
with a `full-canonical` restore contract, resolves each `local://…@sha256:<payload-hash>` identity only
beneath `QURAN_TEST_ARTIFACT_ROOT/sha256/<payload-hash>/`, and then runs the same lock/manifest/hash
verifier used by `verify`. It accepts only a loopback disposable
PostgreSQL target supplied through a private connection file. The target must be an empty container
published only at a literal loopback IP (`127.0.0.1` or `::1`), whose Docker image is exactly
`postgres@<locked digest>` and whose
`com.qurandashboard.full-canonical.run` label matches the requested lane. It also checks target and
`pg_restore` PostgreSQL major compatibility, confirms the connection file port is published by that
same container as exactly one literal-loopback binding, confirms the repository migration state,
restores only the locked `public` Quran tables once, and compares every manifest table count plus every
reviewed sentinel-table count before writing a credential-free receipt. An existing incomplete or failed
receipt blocks automatic retry so a partial large restore is never silently repeated.

`verify-full-canonical` is the sealed execution-side command. It has no retrieval adapter and runs only in
an allowlisted credential-free environment with `QURAN_DASHBOARD_FULL_CANONICAL_NETWORK=blocked`; the
provider must enforce external-egress denial and set that marker only after doing so. The marker is an
attestation, not a network control. It rechecks the receipt, trust contract, pinned container identity,
migration, table counts, and reviewed sentinels against the already restored shared state. It never uses
an ambient developer, shared, staging, or production database as a fallback.

`previous-release-upgrade` is the read-only adoption verifier. Before any rehearsal target is created or
selected, it validates the strict declaration at `docs/testing/previous-release-migration-upgrade.json`,
both local Git commit objects and migration inventories, the current inventory, and the lock-pinned
`quran-canonical` identity, scope, counts, and hashes. It performs no network lookup and never opens a
database. The authoritative previous release is `df07306b5a5ebe08ff205c0d2f6cd5a10af87f2d` (Production
deployment `6158870536`, successful status `17506058851`): its six-migration head is the current head,
so its forward delta is honestly empty. `08b161f4f41c390c8332cd1842e3bdec6c03e322` is only supplemental
historical five-to-six rehearsal evidence (deployment `6074244346`, status `17279084675`); it is never
relabeled as the previous release.

Run the actual supplemental rehearsal only in a scheduled or release job:

```bash
QURAN_DASHBOARD_ARTIFACT_EXECUTION=release \
QURAN_TEST_ARTIFACT_ROOT=/private/qdb-artifacts \
Backend/scripts/test-backend previous-release-upgrade --build
```

The test verifies adoption before leasing an exclusive private `postgres:18-alpine` target, applies the
current chain through migration five, resolves `quran-canonical` only beneath
`$QURAN_TEST_ARTIFACT_ROOT/sha256/<payload-sha256>/`, restores and counts all locked Quran tables, applies
the forward migration to six, boots the real API, and verifies a canonical read plus unavailable
PhraseSearch state. It writes a sanitized JSON phase receipt (heads, artifact hashes, phase status, and
timings only) outside the worktree; no connection string, password, dump, or raw URL is retained.

## Recovery rehearsal

`rehearse-full-canonical-recovery --confirm-backup` reserves the controlled mutation seam for a
representative backup/recovery rehearsal. Backup creation is refused unless the operator supplies the
exact confirmation. The reusable contract records only the backup filename, byte size, SHA-256,
migration state, locked manifest/payload/oracle hashes, source provenance, table counts, sentinel counts,
and the lock-pinned SHA-256 critical-read fingerprints. It verifies the backup before touching an empty
disposable target, then verifies target migration compatibility, counts, sentinels, and critical reads.
The backup output must be a new private file outside the worktree. The adapter accepts an explicit Quran-
table allowlist for both backup and restore and attests that both source and target are disposable. Its
receipt classifies the operation as
`data-recovery` and explicitly records that application rollback was not requested.

The approved `quran-canonical` lock entry records the exact 32 companion-manifest table counts, payload
and manifest hashes, migration state, and local immutable identity. Historical source-package hashes and
critical-read fingerprints remain unavailable, so recovery rehearsal fails closed rather than inventing
them. Do not substitute a developer, shared, staging, or production database, a mutable storage alias,
or a made-up artifact identity. External storage is deferred until remote CI, a second machine, or
another developer requires it; no provider integration, credentials, or uploads are configured here.

For scheduled/release Backend canonical reads, set the seven non-secret path/run variables below before
calling `Backend/scripts/test-backend canonical-data --no-build` (or a lane that includes the canonical
class): `QURAN_DASHBOARD_FULL_CANONICAL_RECEIPT`,
`QURAN_DASHBOARD_FULL_CANONICAL_CONNECTION_FILE`,
`QURAN_DASHBOARD_FULL_CANONICAL_STAGING_ROOT`, and
`QURAN_DASHBOARD_FULL_CANONICAL_DATABASE_CONTAINER`,
`QURAN_DASHBOARD_FULL_CANONICAL_NETWORK=blocked`, and
`QURAN_DASHBOARD_FULL_CANONICAL_RUN=scheduled|release`, and
`QURAN_DASHBOARD_ARTIFACT_EXECUTION=scheduled|release`. The runner first invokes
`verify-full-canonical`; `SmokeDataFixture` then connects to that already-verified loopback-only state
and never restores or copies the dump. The connection file stays private and is not included in the
receipt. Scheduled/release execution must declare the matching artifact execution lane; a missing or
failed sealed verification is fatal and can never fall back to another database or a checkout-relative
dump. Ordinary local canonical runs require the same explicit content-addressed artifact root.
