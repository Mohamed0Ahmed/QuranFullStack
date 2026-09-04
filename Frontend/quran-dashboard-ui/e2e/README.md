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

## Controlled browser execution

Required critical and full evidence uses two explicit phases. Controlled provisioning may use the
network and short-lived package credentials available to the job:

```bash
npm run e2e:provision
```

This runs `npm ci`, locked NuGet restore, exact Playwright Chromium installation, ephemeral localhost
certificate generation, the loopback egress-guard build, and Backend/Frontend builds. It writes a
credential-free schema-v2 receipt under `.playwright/provisioning/`; execution rejects the receipt
after npm, NuGet, harness-source, Chromium, certificate, guard, TestRuntime, API, or Frontend output
drift. Provisioning does not acquire a PostgreSQL image, verify a database artifact, create a Docker
network or container, or restore a dump.

The controlled canonical and stateful runners strip package, artifact, development-database, and
ambient application credentials before starting their children. They pass only the explicitly
required Test Database connection, use the receipt's exact Chromium and prebuilt outputs, and give
each child an isolated process home, Playwright output directory, and structured-evidence directory.
A preloaded system-call guard permits only native or IPv4-mapped loopback traffic, including local
PostgreSQL, and rejects external request leaks. Local OIDC/JWKS and Logto Management API behavior
remain stubbed.

The supported local aliases use the same policy-aware persistent-capability orchestration:

```bash
npm run e2e:critical:local
npm run e2e:local
```

Each complete run writes one `playwright-run.json` containing the validated canonical and stateful
partition reports. Stateful reports include every exact scenario child, its sanitized outcome,
startup/test/shutdown timing, lock and reset phases, cleanup status, and approved diagnostics.
Playwright's unfiltered working output and isolated process home are always private temporary
directories and are deleted on success or failure.

Controlled evidence never enables Playwright's raw trace or HTML reporter because those formats can
embed headers and bodies. Failed runs may retain only sanitized application output, step-event traces,
text/media-masked screenshots, browser-console errors, accessibility observations, and request
method/origin/path/status metadata. Evidence inspection removes raw archives, traces, dumps, keys, and
certificates; validates retained filenames, MIME contracts, JSON schemas, and PNG signatures; rejects
symlinks; and redacts known credentials. Request/response headers and bodies, database dumps,
credentials, cookies, tokens, signed query strings, private keys, and unmasked browser output are not
retained.

Critical execution discovers annotated journeys from the specifications, then runs the selected
file/line locations. It does not maintain a second journey catalogue.
