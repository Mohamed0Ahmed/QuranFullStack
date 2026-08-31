# Test artifact trust tool

`QuranDashboard.TestArtifacts` consumes the repository-root `test-artifacts.lock.json` trust
catalogue. Its default `status` and `verify` commands are read-only and never open a database
connection. The explicit full-canonical provisioning commands are controlled-egress, scheduled/release
operations; they are the only commands that fetch or restore an artifact.

Use the repository wrapper from any directory:

```bash
Backend/scripts/test-artifacts status
Backend/scripts/test-artifacts status --lane critical
Backend/scripts/test-artifacts verify --artifact compact-cross-stack-base
Backend/scripts/test-artifacts provision-full-canonical --run scheduled --fetch-adapter /private/fetch-artifact --database-connection-file /private/postgres.connection --database-container qdb-full-canonical-scheduled --staging-root /private/qdb-artifacts --receipt /private/qdb-receipt.json
env -i PATH="$PATH" HOME=/tmp QURAN_DASHBOARD_FULL_CANONICAL_NETWORK=blocked Backend/scripts/test-artifacts verify-full-canonical --run scheduled --database-connection-file /private/postgres.connection --database-container qdb-full-canonical-scheduled --staging-root /private/qdb-artifacts --receipt /private/qdb-receipt.json
```

`status` validates the lock, resolves the requested lane or artifact, checks staged existence and
exact size, and compares the locked migration head/count with the repository. `verify` additionally
checks every SHA-256, strictly parses the external manifest, validates table identifiers, and compares
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
 external manifest.

`provision-full-canonical` applies only to `scheduled` or `release`. It selects only locked artifacts
with a `full-canonical` restore contract, invokes the provider-owned fetch adapter once per artifact,
then runs the same lock/manifest/hash verifier used by `verify`. It accepts only a loopback disposable
PostgreSQL target supplied through a private connection file. The target must be an empty container
published only at a literal loopback IP (`127.0.0.1` or `::1`), whose Docker image is exactly
`postgres@<locked digest>` and whose
`com.qurandashboard.full-canonical.run` label matches the requested lane. It also checks target and
`pg_restore` PostgreSQL major compatibility, confirms the connection file port is published by that
same container as exactly one literal-loopback binding, confirms the repository migration state,
restores only the locked `public` Quran tables once, and compares every manifest table count plus every
reviewed sentinel-table count before writing a credential-free receipt. An existing incomplete or failed
receipt blocks automatic retry so a partial large restore is never silently repeated.

`verify-full-canonical` is the sealed execution-side command. It has no fetch adapter and runs only in
an allowlisted credential-free environment with `QURAN_DASHBOARD_FULL_CANONICAL_NETWORK=blocked`; the
provider must enforce external-egress denial and set that marker only after doing so. The marker is an
attestation, not a network control. It rechecks the receipt, trust contract, pinned container identity,
migration, table counts, and reviewed sentinels against the already restored shared state. The fetch adapter is
provider-owned: it receives an artifact ID, its credential-free immutable storage ID, and an isolated
staging root, and must stage the exact lock-relative files. Neither the adapter nor its credentials are
named in the receipt.

`previous-release-upgrade` is currently a read-only fail-closed adoption gate. It verifies the tracked
declaration at `docs/testing/previous-release-migration-upgrade.json` and reports the exact missing
evidence before a PostgreSQL target can be selected or mutated. The repository has no authoritative
previous release ref/tag and no approved representative prior-schema artifact: the current compact
artifacts both declare the repository's current migration head. Do not replace that blocker with a
guessed migration ID, a developer database, or a production-derived dump. Adoption must bind an
authoritative released head and a reviewed, credential-free representative artifact before the
scheduled/release disposable upgrade rehearsal can be enabled.

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
failed sealed verification is fatal and can never fall back to the local ignored dump. Ordinary local
canonical runs retain the existing ignored-dump behavior.
