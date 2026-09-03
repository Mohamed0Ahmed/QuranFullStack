# Playwright provisioning and database modes

## Persistent canonical reads

Canonical Mushaf, word-explorer, and PhraseSearch reads use the persistent Test Database Capability.
Provide the existing local capability connection through `ConnectionStrings__QuranDashboardTest`; the
runner validates it through `QuranDashboard.TestRuntime inspect` and never creates or refreshes it:

```bash
npm run e2e:canonical
npm run e2e:canonical:critical
npm run e2e:canonical:focused -- e2e/mushaf-reader.e2e.ts:412
```

These commands run only tests whose effective policy is `CanonicalReader`. They start one reusable API
host with the Testing `ReadOnly` profile, the restricted reader role, read-only transactions, and no
startup/background writers. Canonical readers retain two-worker Playwright parallelism, require no
advisory lock or reset, and consume the reviewed expectations under repository-root `test-oracles/`.
Missing or unhealthy capability state fails before the browser starts.

The artifact-backed commands below remain temporary only for the guarded, mutating, and explicitly
unmigrated scenarios. Their later migration and the final runner cutover are separate tickets.

Required critical and full evidence uses two explicit phases. Controlled provisioning may use the
network and any short-lived artifact/dependency credentials available to the job:

```bash
npm run e2e:provision
```

This runs `npm ci`, locked NuGet restore, exact Playwright Chromium installation, digest-pinned
PostgreSQL image acquisition, verification of the compact base and PhraseSearch-ready overlay,
ephemeral localhost certificate
generation, and Backend/Frontend builds. It writes a credential-free receipt under
`.playwright/provisioning/`; the receipt is rejected after any npm, NuGet, artifact-lock, browser, or
image drift.

Execution consumes only that preloaded receipt and its outputs:

```bash
npm run e2e:critical
npm run e2e
```

Both commands strip artifact/dependency/application credentials, verify the fixture again without
restore or build, restore PostgreSQL once with pulling disabled, and start the compiled API plus the
prebuilt Angular output. A preloaded system-call guard permits only loopback and the exact private
PostgreSQL address for the UI, API, browser, and their child processes. PostgreSQL remains on a Docker
`--internal` network, so unexpected process and container egress fail closed. Local OIDC/JWKS and
Logto Management API behavior remain stubbed.

For developer iteration without the sealed receipt, use the explicit local commands. Artifact remains
the default; cloning a developer database remains opt-in, loopback-only, and non-canonical:

```bash
npm run e2e:critical:local
npm run e2e:local
E2E_DATABASE_MODE=clone-local npm run e2e:local
```

`clone-local` is non-canonical evidence. It accepts loopback PostgreSQL only, is rejected whenever a
CI environment marker is present, and is never selected because a connection string or user secret
happens to exist.

Each sealed run writes structured durations for artifact provisioning, database preparation,
application startup, and test execution under `.playwright/evidence/<run-id>/`. Failed runs also keep
sanitized application/container logs, step-event traces, text/media-masked screenshots, browser console errors, and
request method/origin/path/status metadata. Request/response headers and bodies, database dumps,
credentials, cookies, tokens, signed query strings, and private keys are never captured. The manifest
declares the required 14-day failed-diagnostic and 30-day aggregate-timing retention contracts for the
provider-neutral observation jobs.

Sealed evidence never enables Playwright's raw trace or HTML reporter because those formats can embed
headers and bodies. The structured reporter retains only sanitized step events; the failure fixture
masks all text, background images, and rendered media before taking its diagnostic screenshot. Each
run places Playwright's unfiltered working output in a separate private temporary directory that is
deleted before evidence inspection. It copies and hashes its immutable provisioning receipt, then
validates retained diagnostic filenames, MIME contracts, JSON schemas, PNG signatures, and text
redaction before declaring the evidence safe.

Artifact execution restores `compact-cross-stack-base` and composes the verified
`compact-phrase-search-ready` data-only overlay with foreign-key constraints active. It then validates
the runtime active build, source fingerprint, non-stale state, succeeded status, and exact/similarity
readiness against the verified manifest. Ordinary execution never runs the PhraseSearch builder.

Tests annotated `mutating` reset state before and after their scenario. The reset truncates only the
literal allowlist in `e2e/harness/database-contract.mjs`; `permissions`, every `quran_*` table, and
therefore all PhraseSearch tables stay outside it. The reset verifies every allowlisted table is
empty, requires Linking background processors to be idle, and compares deterministic before/after
SHA-256 fingerprints of all Quran and PhraseSearch table data with the baseline captured immediately
after restore. A scenario-side mutation or reset-side mismatch fails the test run.

Critical execution discovers annotated journeys from the specifications, then runs the selected
file/line locations. It does not maintain a second journey catalogue.
