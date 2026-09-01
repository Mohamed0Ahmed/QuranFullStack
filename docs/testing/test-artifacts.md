# Test Artifact Contract

**Status:** Accepted design; compact artifacts and the approved full-canonical Quran-only artifact are
locked. Full-canonical execution and the previous-release upgrade rehearsal are Local-first and fail-closed.

**Decision date:** 2026-08-30

This document defines how Quran Dashboard test data is identified, acquired, verified, provisioned,
shared, reset, refreshed, and retired. It supports the
[Risk-Based Testing Strategy](./risk-based-strategy.md) without making test execution depend on an
arbitrary developer database or repeated copies of large restored datasets.

## Core rules

- Quran data is always source-traceable. Fabricated Quranic content is never a fallback.
- Required PR journeys use compact fixtures containing only their reviewed Quran/PhraseSearch
  sentinels and small mutable scenarios.
- Large full-canonical and phrase-ready artifacts are scheduled/release inputs, not per-test fixtures.
- A large artifact is resolved, verified, and provisioned once per applicable run.
- The multi-gigabyte full-canonical/PhraseSearch state is never physically copied or restored per
  scenario, test, or journey group. Exact artifact sizes belong in the tracked lock.
- Isolation means independently clean mutable state, not independent copies of immutable corpora.
- On the current trusted solo-developer runner, the explicit local content-addressed artifact root plus
  the tracked lock is the trust source. There is no default path and no database fallback.
- Named required lanes fail closed when a required artifact is absent or mismatched.
- Operator or production snapshots are not test fixtures unless they are deliberately sanitized,
  manifested, reviewed, and adopted under this contract.

## Current state

The approved non-production candidate is `quran-canonical`, a 372,143,834-byte custom dump with
SHA-256 `3d4038d561a2b4b048e72c05f0cc472b2b1bcf0f2af0d09d0c054cff38e9b29d`. Its companion manifest
has SHA-256 `3b2d15dbc30d8dbe5010f1d373e6a33b8e089902b6347ebb6f561f18874bec3e`, was created at
`2026-08-26T11:52:38Z`, records migration `20260826012918_AddQuranPhraseSearchIndex` at count `6`,
PostgreSQL `18.6`, and all 32 Quran table counts. It contains Quran data but deliberately excludes
PhraseSearch, Abwab, Access, and Linking state.

The archive was created on `2026-08-26T14:50:46 EEST` from
`quran_dashboard_phrase_phase9_canonical_repair` by PostgreSQL/pg_dump `18.6`, custom archive format
`1.16` with gzip compression. `Backend/scripts/create-smoke-dump` produced it locally and excludes
`quran_phrase_*` data. A disposable PostgreSQL 18 restore verified every manifest count, zero phrase
rows, no populated non-Quran public table, database size `1351440063` bytes, and summed Quran relation
size `1339113472` bytes.

Producer and consumer checks provide migration parity, canonical baseline counts, temporary output files,
hash verification, producer-major compatibility, and restored row-count validation. Limits remain:

- All resources are gitignored and unavailable in a clean clone. The lock contains the approved artifact's
  exact companion-manifest table scope and counts, while provisioning resolves only the explicit local
  content-addressed root. It never substitutes a repository path or an arbitrary developer, shared,
  staging, or production database.
- Historical provenance is limited: the current manifest has no source-package hashes and no remote
  immutable-storage ID. The local content-addressed dump identity is the trusted-runner boundary for now;
  unavailable source-package hashes remain explicitly unavailable rather than being reconstructed.
- Previous-release upgrade rehearsal is active in the explicit scheduled/release Backend lane. Its adopted
  declaration verifies local Git evidence before target selection: authoritative previous release
  `df07306b5a5ebe08ff205c0d2f6cd5a10af87f2d` is the successful Production deployment `6158870536` / status
  `17506058851`, with the same six-migration head as this branch and therefore zero forward migrations.
  Supplemental commit `08b161f4f41c390c8332cd1842e3bdec6c03e322` is successful Production deployment
  `6074244346` / status `17279084675` with five migrations; it is historical five-to-six rehearsal evidence,
  not the authoritative previous release. The lane creates a private disposable PostgreSQL 18 target,
  restores the current-head Quran-only `quran-canonical` payload into the historical five-migration schema,
  proves all 32 locked counts, advances through migration six, boots the API, and checks canonical and
  unavailable-PhraseSearch sentinels. This restore compatibility proof does not claim the artifact was
  produced at schema five.
- A direct focused test can skip when an artifact is absent; the supported Backend `pre-pr` wrapper
  instead fails its canonical preflight.
- The Frontend harness now consumes the locked compact cross-stack artifact by default; its former
  local-database clone behavior remains available only as explicit non-canonical `clone-local` mode.
- The compact PhraseSearch available-path snapshot is now adopted through a source-reviewed overlay,
  hashed oracle and manifest, and tracked lock. Other PhraseSearch-ready and Abwab operator snapshots
  remain insufficient trust contracts until separately reviewed and adopted.

This document retains the existing safeguards and closes the local acquisition, trust-root, fixture-size,
and reset gaps.

Current implementation truth includes the
[canonical dump producer](../../Backend/scripts/create-smoke-dump),
[Backend test runner](../../Backend/scripts/test-backend), and the canonical fixture/gate code under
[`Backend/tests/QuranDashboard.Tests/Smoke/Data`](../../Backend/tests/QuranDashboard.Tests/Smoke/Data/).

## Artifact classes

The exact identifiers are implementation work, but the lock must distinguish these logical sets:

| Logical set | Intended content | Required by | Provisioning rule |
| --- | --- | --- | --- |
| Compact cross-stack base | Independently reviewed Quran sentinels plus small deterministic Access, Abwab, and Linking prerequisites | Required PR fidelity, security, Linking, and Abwab journeys | Provision once for a compatible PR stack; reset only mutable scenario tables |
| Compact PhraseSearch-ready fixture | Source-traceable minimal phrase variants, active-build state, capabilities, and reviewed expected results | Required PR PhraseSearch available-path journey | Provision once; never rebuild during ordinary journeys |
| Backend canonical smoke | Full approved Quran-only dump and manifest | Scheduled/release canonical Backend reads | Fetch, verify, and restore once per applicable run |
| Foundation canonical sources | Approved foundation package plus required MASAQ input | Scheduled/release importer protection | Fetch and verify once per applicable run |
| Enriched morphology | Enriched artifact and manifest | Scheduled/release enriched import protection | Fetch and verify once per applicable run |
| Full PhraseSearch-ready state | Full canonical Quran and activated phrase index with build manifest | Scheduled/release full PhraseSearch journeys | Provision once and share immutably; do not copy per test |
| PhraseSearch build input | Eligible full source state with no active build | Dedicated scheduled/release build/activation test | Exclusive state; only this test builds the index |

Compact fixtures must remain genuinely compact. They contain real, reviewed Quran/source excerpts and
only the derived PhraseSearch records required to prove the selected behavior. They do not build the
full Quran corpus during a PR.

Non-Quran Access, Abwab, and Linking scenario data may be deterministic and synthetic. It remains
logically separate from the trusted Quran oracle and must not be mistaken for canonical authored data.

## Tracked trust root

The tracked repository-root `test-artifacts.lock.json` is the trust root. Its strict schema is
[`test-artifacts-lock.schema.json`](./test-artifacts-lock.schema.json); hashed artifact manifests use
[`test-artifact-manifest.schema.json`](./test-artifact-manifest.schema.json). Only reviewed entries with
real hashes and Quran sentinels are adopted; an unfinished fixture remains absent. A required lane or
artifact requested before its reviewed entry exists fails closed.

Each locked artifact records at least:

- Stable artifact ID and contract version.
- Lanes or journey groups that require it.
- Exact staged relative paths.
- Exact byte size of every delivered archive, dump, fixture, and manifest.
- SHA-256 for every delivered file and the artifact manifest itself.
- Migration head and migration count.
- PostgreSQL producer major/version and, where applicable, the required container digest.
- Producer command and producer/tool version.
- Source identity and provenance, including an explicit limitation where historical source-package hashes
  are unavailable.
- Table scope, including explicit presence/absence of Quran, PhraseSearch, Abwab, Access, and Linking
  data.
- Reviewed Quran sentinels, expected counts, and oracle hash where applicable.
- PhraseSearch manifest hash, source fingerprint, and readiness expectations where applicable. The
  volatile active build ID remains inside the immutable artifact manifest and is compared with runtime
  capabilities; it is not duplicated into the tracked lock.
- A credential-free immutable logical storage identifier. The current full-canonical artifact uses a
  `local://…@sha256:<payload-hash>` identity; remote provider identities are deferred.
- Refresh reason, date, and owning role.

A hash stored only inside an untrusted artifact manifest is insufficient. The tracked lock pins the
manifest hash as well as its payload files.

## Local storage and acquisition

The current full-canonical contract is Local-first for the trusted solo-developer runner. Provisioning
requires `QURAN_TEST_ARTIFACT_ROOT`; it never reads the historical repository checkout path and never
falls back to an ambient connection string or another database. For a payload pinned as `<payload-hash>`,
the root must contain this content-addressed layout:

```text
$QURAN_TEST_ARTIFACT_ROOT/sha256/<payload-hash>/<payload-file-name>
$QURAN_TEST_ARTIFACT_ROOT/sha256/<payload-hash>/<manifest-file-name>
```

The lock's `local://…@sha256:<payload-hash>` identity must match the locked payload SHA-256. The
provisioner derives every source path from that hash, stages only the locked file names, then verifies
locked sizes, hashes, manifest identity, migration state, table scope, counts, provenance, and restore
sentinels before use. Missing root, directory, file, size, hash, manifest, migration state, scope,
counts, or provenance fails closed before a database restore.

Provisioning follows this order:

1. Resolve the local immutable storage identity from the tracked lock beneath `QURAN_TEST_ARTIFACT_ROOT`.
2. Copy only the identified files to the isolated staging root.
3. Verify byte size and SHA-256 before database use.
4. Validate the artifact manifest against a strict schema.
5. Validate migration and PostgreSQL compatibility.
6. Validate table names against an allowlist or strict identifier rule before interpolating them into
   SQL.
7. Stage the artifact at its declared relative path.
8. Start sealed execution with no retrieval credentials or arbitrary database fallback.

Cache by the lockfile or artifact content hash. A cache hit still verifies the payload. A cache miss is
part of the 12-minute PR activation measurement for compact fixtures. Large artifacts are outside the
PR journey path and are provisioned once per applicable scheduled or release run.

The implemented read-only command surface is:

- `status`: report required, present, missing, stale, or mismatched sets.
- `verify`: perform all lock, manifest, schema, compatibility, and sentinel checks.

Run these through `Backend/scripts/test-artifacts`; both accept `--lane` or `--artifact`. `status`
checks lock shape, selection, staged presence/size, and migration freshness. `verify` additionally
checks hashes, strict artifact-manifest shape, safe table identifiers, and lock/manifest agreement.
Both are read-only and return non-zero for any required set that is missing, stale, or mismatched.

The implemented full-canonical controlled-provisioning surface is:

- `provision-full-canonical`: scheduled/release-only provision-once command. It resolves the locked
  local content-addressed files beneath `QURAN_TEST_ARTIFACT_ROOT` into an isolated staging root exactly
  once, invokes the same trust verifier, checks the private disposable PostgreSQL target's migration
  state, restores once, validates every manifest row count and reviewed sentinel-table count, and writes
  a credential-free receipt. An incomplete/failed receipt blocks automatic retry.
- `verify-full-canonical`: sealed execution-side receipt and shared-state verifier. It has no retrieval
  adapter, rejects artifact credentials in its environment, and performs no restore.
- `rehearse-full-canonical-recovery`: an explicit-intent recovery contract. It captures integrity
  metadata for a representative backup, verifies it before restoring only to an isolated disposable
  target, and records sanitized data-recovery evidence distinct from application rollback. Full-canonical
  sentinel declarations pin each critical read's SHA-256 in both the lock and artifact manifest, while
  recovery evidence retains the locked staged hashes and source provenance. It remains fail-closed when a
  critical-read fingerprint is unavailable from the approved artifact.

External storage is explicitly deferred. Revisit it only when a remote CI provider, second machine, or
additional developer needs the artifact. That decision must add a reviewed provider-neutral acquisition
adapter and immutable remote identity; this repository does not configure Cloudflare or any other
provider, credentials, uploads, or provider-specific settings.

## PR fixture model

Required PR journeys do not restore or clone the full canonical/PhraseSearch database.

- Provision the locked compact cross-stack base and compatible PhraseSearch-ready overlay once for an
  isolated stack. Verify both before composition and require the same pinned PostgreSQL image digest.
- Share immutable Quran and PhraseSearch tables across scenarios.
- Keep mutable scenario tables explicitly identified and small.
- Reset only those mutable tables between mutating scenarios.
- Verify the expected clean-state sentinel after reset.
- Drain or account for background jobs before considering reset complete.
- Do not rely on execution order.

The mutable allowlist is implementation-owned and must cover the scenario's Access/session/audit,
Abwab, Linking workspace/job/outcome, and derived projection state. A table outside the allowlist is not
silently truncated. Expanding the allowlist is a reviewed test-infrastructure change.

Reset may use deterministic delete/truncate-and-reseed operations, a small schema/database clone, or a
copy-on-write mechanism. The mechanism is acceptable only when it:

- Proves the required clean state.
- Does not modify immutable Quran/PhraseSearch data.
- Preserves scenario independence.
- Fits the 12-minute end-to-end PR budget and low execution-cost constraint under measurement.

Repeated full-database copy/restore is prohibited. Quran words and PhraseSearch indexes are not rebuilt
except by their dedicated build/activation tests.

## Scheduled and release model

Large canonical and phrase-ready inputs are provisioned once per applicable run. Tests share the
restored immutable state and isolate only small mutable scenarios. Separate databases or application
processes are created only for incompatible configuration or exclusive operations.

The dedicated PhraseSearch build/activation test receives eligible state with no active build. It
proves:

- Fail-closed prerequisites and storage proof.
- No active pointer before successful completion.
- Successful report and activation.
- Matching active build ID in capabilities.
- Correct post-build reads.
- Refusal to force or replace an already active build.

All other PhraseSearch tests consume a ready artifact and must not rebuild it. The implemented PR
available-path journey composes `compact-phrase-search-ready` over `compact-cross-stack-base`, asserts
the runtime active build ID and readiness against the oracle, and proves unchanged capabilities after
the browser flow.

Destructive Abwab snapshot/topics imports receive their own small mutable target state and prove
checksum validation, empty-table or other documented preconditions, transactionality, rollback,
identity/projection repair, exact results, and a post-operation API read.

## Local modes

The Frontend Playwright harness exposes two explicit modes:

- `artifact`: deterministic and canonical for the selected lane. This is the default for critical/full
  commands and the only mode accepted in target CI.
- `clone-local`: opt-in developer convenience using a loopback-only database. It is non-canonical and
  cannot be cited as release evidence.

The harness never infers `clone-local` from user secrets or an ambient connection string. CI rejects
it before reading either. Clone-local enforces loopback access; artifact mode uses only its private
internal Docker network. Artifact execution verifies the lock, requires the digest-pinned image to be
preloaded, restores once for the Playwright command, and runs with Docker pulling disabled.

## Hermetic execution

Acquisition is a controlled-egress provisioning concern. Target test execution must be sealed:

- Dependencies, browser binaries, container images, certificates, and artifacts are already present.
- PostgreSQL images are pinned by digest and pulling is disabled during execution.
- Backend tests use `--no-restore` and `--no-build` where the runner contract permits.
- OIDC/JWKS and Logto Management API behavior are local stubs.
- Artifact credentials are absent.
- Process/container network egress is denied.

The implemented browser harness separates these phases as `npm run e2e:provision` followed by
`npm run e2e:critical` or `npm run e2e`. Provisioning produces a credential-free receipt binding the
npm and NuGet locks, artifact trust lock, exact Chromium revision, PostgreSQL digest, certificates,
egress guard, and build outputs. Execution rejects stale/missing inputs, strips retrieval and package
credentials, restores the already-verified fixture with pulling disabled, and starts prebuilt outputs.
Only loopback and the exact private database address are permitted to execution processes; PostgreSQL
uses an internal Docker network.

Failed runs preserve sanitized application/container logs, step-event traces, text/media-masked
screenshots, console errors, and request method/origin/path/status metadata under `.playwright/evidence/`. The evidence
contract excludes request/response headers and bodies, credentials, private keys, database dumps, and
query strings. Structured results carry the artifact, database, startup, and test durations plus the
14-day failed-diagnostic and 30-day aggregate-timing retention requirements for later provider-neutral
upload wiring. Playwright writes any unfiltered working files only to a private temporary directory
outside the evidence tree; sealed teardown deletes that directory before the retained evidence is
schema- and signature-validated.

The staging Logto sentinel is a separate serialized provider-contract lane with a narrow allowlist and
dedicated non-human identities. It does not reuse artifact retrieval credentials or mutate Abwab,
Linking, PhraseSearch, or canonical Quran data.

## Refresh triggers

Refresh an artifact when any of these changes:

- Migration head or migration count.
- Canonical source identity, source hash, or approved Quran oracle.
- Seeded journey expectation.
- PostgreSQL producer/restore major or pinned image digest.
- Importer or fixture contract version.
- PhraseSearch format, source fingerprint, active-build contract, or availability policy.
- Included table scope.

Do not refresh merely because a test failed.

## Refresh review

Artifact and Quran-oracle changes require an independent reviewer. The candidate report contains:

- Old and new hashes and byte sizes.
- Old and new migration head/count.
- Added or removed tables and every row-count delta.
- Old and new source/provenance identifiers.
- Golden Quran sentinel comparison.
- PhraseSearch fingerprint/build/readiness changes when applicable.
- Explicit reason for every expected delta.
- Producer version and command.

Changed counts or Quran values are never automatically accepted. The reviewer validates the source,
not just internal consistency between a dump and the manifest produced beside it.

## Retention, recovery, and security

- Retain every artifact referenced by an active release, supported previous-release migration test, or
  current lockfile.
- Retain the immediately previous approved artifact generation until the new generation has completed
  scheduled and release verification.
- Recovery uses the local immutable content identity and tracked hashes, not mutable aliases.
- Do not record connection strings, credentials, signed URLs, storage proofs, volatile paths, real
  identity tokens, or production-derived personal data in tracked files.
- Diagnostic database dumps are opt-in, sanitized, checksummed, access-controlled, and time-limited.

## Governance

The pull-request author explains why an artifact or oracle change is required. An independent reviewer
checks provenance, deltas, and source fidelity. Emergency acceptance follows the same maintainer,
owner, rationale, and seven-day expiry rules as a test downgrade; it cannot bypass hash or provenance
verification.

Review this contract after an artifact incident, unexplained count change, migration failure, stale
lock, new destructive importer verb, PhraseSearch format change, or the quarterly strategy review.
