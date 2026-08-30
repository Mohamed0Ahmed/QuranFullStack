# Test artifact trust tool

`QuranDashboard.TestArtifacts` is the read-only consumer of the repository-root
`test-artifacts.lock.json` trust catalogue. It never fetches, extracts, restores, publishes, or
refreshes artifacts and never opens a database connection.

Use the repository wrapper from any directory:

```bash
Backend/scripts/test-artifacts status
Backend/scripts/test-artifacts status --lane critical
Backend/scripts/test-artifacts verify --artifact compact-cross-stack-base
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
