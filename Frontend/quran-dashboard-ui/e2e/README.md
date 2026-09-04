# Playwright state-policy execution

All Playwright tests declare exactly one `canonical-read`, `guarded-read`, or `mutating` policy plus a
fixture profile. The profile records setup writes and the exact API background activities required by
the scenario. `ConnectionStrings__QuranDashboardTest` must point at the existing verified local Test
Database Capability for every supported command.

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

## Guarded and mutating scenarios

Stateful scenarios run sequentially as exact `file:line` Playwright children:

```bash
npm run e2e:stateful
npm run e2e:stateful:critical
npm run e2e:stateful:focused -- e2e/abwab-permissions.e2e.ts:11
```

Each guarded child holds one outer shared TestRuntime keeper lock and starts a fresh `ReadOnly` API.
Each mutating child holds one outer exclusive keeper lock, captures a Protected State fingerprint,
proves the API port is free, and performs the centralized Mutable Application State reset before
starting a fresh `Mutable` API with only its fixture profile's declared background activities. After
the child exits, its API process receipt must prove that exact process stopped before the final reset.
Missing or mismatched process evidence makes cleanup fail closed.

Each child owns a private Playwright output directory and a private structured-evidence directory.
The stateful runner removes raw Playwright output and aggregates only sanitized child results under
`.playwright/evidence/<run-id>/stateful-results.json`.

The complete supported suites run canonical readers first and stateful children second:

```bash
npm run e2e:critical
npm run e2e
```

Focused selection routes automatically by its declared policy:

```bash
npm run e2e:focused -- e2e/linking-success.e2e.ts:82
```

Headed and UI debugging require an explicit read-only or mutating declaration and one exact selector.
A mutating interactive process retains its exclusive keeper lock until the Playwright window closes,
then performs verified final cleanup:

```bash
npm run e2e:headed -- --read-only e2e/abwab-permissions.e2e.ts:11
npm run e2e:ui -- --mutating e2e/linking-success.e2e.ts:82
```

## Legacy sealed provisioning

The artifact-backed provisioning and sealed-execution implementation remains temporarily present for
its separate retirement work, but supported Playwright suite, focused, headed, and UI commands no
longer select it.

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

The retained sealed runner strips artifact/dependency/application credentials, verifies the fixture without
restore or build, restores PostgreSQL once with pulling disabled, and starts the compiled API plus the
prebuilt Angular output. A preloaded system-call guard permits only loopback and the exact private
PostgreSQL address for the UI, API, browser, and their child processes. PostgreSQL remains on a Docker
`--internal` network, so unexpected process and container egress fail closed. Local OIDC/JWKS and
Logto Management API behavior remain stubbed.

The supported local aliases use the same policy-aware persistent-capability orchestration:

```bash
npm run e2e:critical:local
npm run e2e:local
```

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

Legacy sealed resets remain governed by their sealed-execution contract. Supported mutating execution
uses only the centralized TestRuntime reset contract and introduces no test-only HTTP reset endpoint.

Critical execution discovers annotated journeys from the specifications, then runs the selected
file/line locations. It does not maintain a second journey catalogue.
