# Risk-Based Testing Strategy

**Strategy designed; not yet adopted**

**Decision date:** 2026-08-30

**Scope:** Entire Quran Dashboard monorepo

This strategy protects the failures that would harm users or Quran Dashboard data most. It favors a
small number of trustworthy, cross-stack journeys over blanket line or branch coverage. It records
both the repository's current protection and the target state; planned protection must not be reported
as if it already exists.

The Frontend testing decision is recorded separately in
[ADR 0001](../adr/0001-playwright-only-frontend-testing.md). Artifact acquisition, trust, and reset
rules live in [Test Artifacts](./test-artifacts.md).

## Outcomes and non-goals

The strategy must:

- Prevent fabricated, corrupted, or incorrectly associated Quran data from reaching the UI.
- Protect authentication, device sessions, Permissions, and Owner-only boundaries.
- Prove the highest-risk user journeys through Angular, the real API, and PostgreSQL.
- Protect irreversible or destructive imports, snapshots, index builds, and migrations.
- Keep mandatory pull-request feedback within 12 minutes, including provisioning and startup.
- Produce deterministic, independently reviewable evidence.

The strategy does not:

- Set line, branch, file, or component coverage targets.
- Require one test file per bug fix or one browser test per endpoint.
- Add a Frontend unit-test runner or a second Frontend testing convention.
- Add Firefox or WebKit lanes.
- Treat route registration, non-empty text, or a page rendering as proof of business behavior.
- Select or implement a CI provider.

## Release-risk hierarchy

Release decisions use the approved three-tier hierarchy:

1. Quran fidelity, provenance, and destructive data corruption.
2. Authentication, authorization, and privilege boundaries.
3. Core user journeys and Backend–Frontend contract compatibility.

This release-risk hierarchy is separate from the critical-journey rollout order in the adoption
roadmap. Destructive operational workflows form a parallel P0 track under the first tier; they do not
wait for the ordered browser-journey work and do not create another release-risk tier.

## Current protection inventory

This inventory is a repository snapshot from 2026-08-30. The executable catalogues and manifests
remain implementation truth as the suite changes.

### Backend

The Backend has one .NET 10 xUnit project, `QuranDashboard.Tests`, using FluentAssertions,
`WebApplicationFactory`, and Testcontainers PostgreSQL. Its enforced catalogue currently contains:

- 123 test classes and 612 declared test methods; parameterized cases make the runtime count higher.
- 31 Fast, 80 Database, 1 Migration, 1 Process, and 10 Canonical classes.
- 56 TierB, 59 Pipeline, and 8 Smoke classes.
- 22 catalogued collection fixtures with explicit state and resource policies.

Protection is strongest around:

- Access provisioning, Permission enforcement, Owner reconciliation, audit persistence, and
  authorization metadata.
- Foundation, navigation, tafsir, translation, morphology, display-word, i'rab, and mutashabihat
  imports, including rollback, source validation, idempotency, and canonical counts.
- API route and access-metadata parity for all 126 registered routes.
- Canonical Quran dump restore and selected read payloads.
- Rate limiting and shared HTTP-pipeline behavior.

Important limits remain:

- 64 of the 126 route entries are parity-only and are not dispatched by the generic HTTP sweep.
- The canonical read set contains only 13 positive data-route expectations; 11 PhraseSearch
  expectations prove only the unavailable `503` state.
- There is no dedicated Linking behavior suite.
- Device-session cookie, expiry, CSRF, replacement, and revoke behavior lacks end-to-end protection.
- PhraseSearch build/activation and the available read path lack dedicated protection.
- Abwab snapshot import/export, topics import, and several template/relation semantics are weak or
  absent.

The supported runner already offers risk-shaped selectors such as `fast`, `tier-b`, `gate-contract`,
`smoke`, `pipeline`, `canonical-data`, `feature`, and `pre-pr`. The existing Backend catalogue remains
the executable source of truth; new Linking and PhraseSearch classes must be added there rather than to
a second catalogue.

### Frontend

The Angular application deliberately uses Playwright only:

- Angular schematics skip unit-test generation, there is no Angular `test` target, and
  `check:no-unit-specs` rejects `src/**/*.spec.ts`.
- Six Playwright specifications currently declare 24 Chromium tests; one tafsir scenario can skip when
  only one tafsir source is seeded.
- Current coverage includes Mushaf navigation and selection, ayah/word study tabs, word explorers,
  basic shell navigation, anonymous Abwab denial, and one directly granted Abwab create affordance.
- Playwright launches the real Angular app and API and connects them to PostgreSQL. Logto discovery,
  keys, and Management API behavior are replaced locally.

Important limits remain:

- There is no successful Linking, PhraseSearch, access-administration, or Abwab mutation journey.
- Authentication setup injects local OIDC state and does not prove the real redirect/callback boundary.
- Most unannotated Quran assertions remain visibility or non-empty checks; the first critical
  artifact-backed sentinel now compares page 1 with its locked independent oracle.
- The current harness defaults to the verified compact artifact; arbitrary local data is available
  only through explicit, loopback-only, non-canonical `clone-local` mode.
- `test:pre-pr` does not include Playwright or `e2e:typecheck`.
- Controlled `e2e:provision` now locks and preloads dependencies, Chromium, the PostgreSQL image,
  compact artifacts, ephemeral certificates, and build outputs. Canonical `e2e`/`e2e:critical`
  consume the receipt under credential-free network sealing; explicit `*:local` commands retain the
  developer path.

### Cross-stack and automation

- `Backend/scripts/check-api-contract` detects drift in the committed OpenAPI document and generated
  Frontend DTOs. Frontend operations are intentionally handwritten, so generated-model parity alone
  cannot prove URL, method, query, or envelope wiring.
- The committed deployment health check proves liveness and selected dependencies, not a usable Quran
  journey, PhraseSearch expectation, CORS, or authenticated behavior.
- No tracked CI workflow currently enforces the supported repository gates.
- No tracked repository-wide testing strategy exists in the current tree outside these new documents.

Primary evidence lives in the [Backend test project](../../Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj),
[Backend gate catalogue](../../Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv),
[Backend test runner](../../Backend/scripts/test-backend),
[Frontend package scripts](../../Frontend/quran-dashboard-ui/package.json),
[Playwright configuration](../../Frontend/quran-dashboard-ui/playwright.config.ts),
[unit-spec policy check](../../Frontend/quran-dashboard-ui/scripts/check-no-unit-specs.mjs), and
[API contract checker](../../Backend/scripts/check-api-contract).

## What counts as protected

A P0 journey is protected only when its evidence identifies:

- The unacceptable failure.
- One successful path.
- One relevant denial, stale-state, rollback, idempotency, or recovery path.
- The authoritative data or source oracle.
- The owning test layer.
- A cross-stack sentinel when a user interacts with the behavior.
- The required execution lane and state/resource policy.

For P1, the same structure applies when behavior is changed or promoted into a required lane. A route
existing, an HTTP status alone, non-empty text, or a rendered component is not sufficient evidence.

## Test placement

Use one owning layer and a thin cross-stack sentinel:

| Behavior | Primary protection |
| --- | --- |
| Pure rule or calculation | Fast xUnit |
| Persistence, transaction, concurrency, import, or projection invariant | xUnit with Testcontainers |
| HTTP contract, cookie, CSRF, authorization, rate limit, or error envelope | `WebApplicationFactory` |
| User-visible wiring, routing, URL state, browser behavior, and Backend-Frontend integration | Playwright |

Exhaustive permutations belong below the browser. Playwright proves representative risk branches. A
bug fix may extend an existing test; it does not require a new file. Regression protection belongs at
the narrowest faithful seam.

## Critical journey catalogue

This is a human risk map, not a duplicate executable test catalogue. Backend class selection remains in
`test-gates.tsv`; Playwright annotations live in the specifications themselves.

### 1. Quran fidelity

- **Current status:** Open.
- **Unacceptable failure:** Fabricated, corrupted, or incorrectly associated Quran text, word location,
  translation/tafsir source, or rendered glyph reaches the UI.
- **Successful path:** Navigate through the real UI to a reviewed verse and word, then observe the exact
  expected associations, source identities, font readiness, and absence of replacement glyphs.
- **Failure or recovery path:** Missing or mismatched source data fails closed to the controlled missing
  state, and any trusted-oracle mismatch fails the gate rather than accepting a new value.
- **Authoritative oracle:** The independently reviewed, committed subset of verse keys, exact Unicode
  text, word locations, source identities, and its recorded provenance/hash.
- **Owning test layer:** Backend canonical/import and API-mapping xUnit tests.
- **Cross-stack sentinel:** Chromium Playwright navigates the Mushaf and study UI and compares its
  visible associations with the trusted subset and independent API mapping.
- **Lane and state/resource policy:** Required PR `critical`, `read-only` journey using the compact
  cross-stack base fixture; run desktop and the approved Mushaf mobile variant against immutable Quran
  state. Full-canonical validation remains scheduled/release evidence.

### 2. Device sessions and Permissions

- **Current status:** Open.
- **Unacceptable failure:** An unauthenticated, revoked, disabled, expired, or under-privileged actor
  retains authority; cookie-authenticated unsafe requests bypass the CSRF contract.
- **Successful path:** Complete automatic device-session bootstrap over HTTPS, prove cookie-backed
  access, then have an Owner grant one exact Permission through the UI so an Active non-Owner can perform
  the protected action.
- **Failure or recovery path:** Missing or mismatched CSRF fails on protected unsafe endpoints; logout,
  revoke, disable, and Permission removal make both the UI affordance and protected API action fail even
  when browser state is stale.
- **Authoritative oracle:** `/api/access/me` without `Authorization`, direct-Permission reads, filtered
  audit events, persisted device-session state, and the actual protected API allow/deny result.
- **Owning test layer:** Backend `WebApplicationFactory` authentication/authorization tests plus Access
  database integration tests.
- **Cross-stack sentinel:** Chromium Playwright exercises HTTPS session bootstrap and the Owner access
  administration UI, then independently verifies the non-Owner's effective authority.
- **Lane and state/resource policy:** Required PR `critical`, `mutating` journey using the compact
  cross-stack base fixture; reset only the small Access/session/audit scenario tables and use distinct
  least-privilege Owner and non-Owner identities.

### 3. Linking

- **Current status:** Open.
- **Unacceptable failure:** Linking accepts stale or unauthorized state, duplicates work, loses a
  confirmed result, or leaves partial door links/projections after cancellation or failure.
- **Successful path:** An Owner configures sources, obtains an unblocked prepared preflight, confirms it,
  observes the durable outcome, and sees the expected Abwab/Mushaf projection.
- **Failure or recovery path:** Prove stale-revision refusal, idempotent resubmission, cancellation,
  restart recovery, lease/inclusion-sync failure handling, and zero partial writes.
- **Authoritative oracle:** Canonical source verse keys, the prepared-preflight token/summary, durable
  confirmation outcome, door-link snapshot, and Mushaf ayah-to-door projection.
- **Owning test layer:** Backend application/persistence xUnit tests with Testcontainers, including the
  real API-hosted background processors and HTTP response variants.
- **Cross-stack sentinel:** Chromium Playwright performs the Owner configuration, preflight, and
  confirmation through the real UI, then independently rereads the durable outcome and projection.
- **Lane and state/resource policy:** Required PR `critical`, `mutating` journey using the compact
  cross-stack base fixture; run desktop and the approved Linking mobile variant, and reset only small
  Linking workspace/job/outcome, Abwab, and related projection state.

### 4. PhraseSearch

- **Current status:** Open.
- **Unacceptable failure:** An unavailable, stale, mismatched, or incorrectly built phrase index serves
  wrong results or adds the wrong Quran selection to Linking.
- **Successful path:** Verify ready capabilities against the approved build manifest, perform the
  reviewed search/context/similarity path in the UI, resolve the selection, and persist it with Add to
  Workspace.
- **Failure or recovery path:** Prove unavailable `503`, stale-reference `409`, controlled unavailable
  UI, fail-closed build prerequisites, inactive state after build failure, and refusal to replace an
  already active one-shot build.
- **Authoritative oracle:** The hashed fixture/builder manifest, source fingerprint, active build ID at
  runtime, reviewed query/result subset, and the independently reread Linking workspace source.
- **Owning test layer:** Backend process/database/API tests for build, readiness, query semantics,
  conditional requests, and bounded compute behavior.
- **Cross-stack sentinel:** Chromium Playwright performs an available query and persistent Add to
  Workspace action, then independently rereads the workspace; resolution alone is not persistence
  evidence.
- **Lane and state/resource policy:** Required PR `critical`, `mutating` journey using the compact
  PhraseSearch-ready fixture with immutable phrase data and resettable workspace state. Full one-shot
  build/activation uses exclusive eligible state in the scheduled/release lane only.

### 5. Abwab projection

- **Current status:** Open.
- **Unacceptable failure:** An authorized mutation is lost, partially committed, incorrectly ordered or
  versioned, projected to the wrong Mushaf content, or remains possible after Permission removal.
- **Successful path:** A least-privilege user performs the representative mutation through the UI,
  refreshes, and observes the durable Abwab tree and consuming projection.
- **Failure or recovery path:** Prove unauthorized denial, stale-version/concurrency conflict,
  transaction rollback with no partial state, and revoke/disable behavior.
- **Authoritative oracle:** The public Abwab tree, door/tree versions, relevant ETag/detail response,
  relation/inclusion/link counts, and Mushaf projection where applicable.
- **Owning test layer:** Backend Abwab application/persistence xUnit tests with Testcontainers plus HTTP
  Permission and concurrency protection.
- **Cross-stack sentinel:** Chromium Playwright performs the real mutation and verifies it after a fresh
  read/browser context through the consuming UI and independent API seam.
- **Lane and state/resource policy:** Required PR `critical`, `mutating` journey using the compact
  cross-stack base fixture; reset only small Abwab, Access, and related projection scenario tables.

### Playwright selection metadata

Each target Playwright suite records these annotations in the specification that owns the test:

- `critical`.
- `mobile` when the approved mobile variant applies.
- Exactly one of `mutating` or `read-only`.
- The required artifact or compact-fixture identifier.
- The risk/journey identifier.

Lane selection consumes these annotations and validates them through Playwright discovery output. The
specifications remain the executable source of truth; this human risk map does not become a second
machine-readable catalogue.

### Quran fidelity oracle

Expected Quran values must not be generated from the database under test. Commit a deliberately small,
independently reviewed oracle containing approved verse keys, exact Unicode text, word locations, and
translation/tafsir source identities. Record its provenance and hash in the artifact contract. Never
auto-accept changed text or counts to make a gate pass.

The UI sentinel proves navigation, visible association, local font readiness, and absence of
replacement glyphs. Data/API tests own exact Unicode and relational assertions. Whole-page screenshots
and enormous golden payloads are not fidelity oracles.

### Device sessions and Permissions

Use distinct actors:

- A locally stubbed signed-in browser lets the application perform its actual automatic device-session
  bootstrap over HTTPS.
- `/api/access/me` must then succeed through the secure cookie without an `Authorization` header.
- Missing or mismatched CSRF is asserted only on unsafe endpoints protected by the CSRF contract.
- Logout or revoke must make cookie-backed `/api/access/me` unauthorized.
- A dedicated Owner uses the real access-administration UI.
- An Active non-Owner receives exactly the Permission under test and performs the protected action.
  Revoke or disable must remove both UI affordance and API authority, even if the browser is stale.

An Owner cannot prove direct-Permission enforcement because Owner authorization bypasses direct grants.
Linking is a separate Owner-only journey and cannot be authorized with an Abwab Permission.

### Linking

Exercise real API-hosted background processors. Poll observable state with bounded semantic conditions,
never fixed sleeps. Test code must accept the documented idempotent response variants: preflight and
confirmation can return an existing resource, and confirmation can resolve directly to a durable
outcome rather than always returning a new job.

The browser performs the risk-bearing configuration, preflight, and confirmation. Backend tests own
exhaustive stale revision, lease loss, idempotency, cancellation, restart recovery, inclusion-sync
failure, and zero-partial-write assertions. A timeout report includes the last business state, job ID,
and sanitized logs.

### PhraseSearch

PhraseSearch readiness is currently binary. A valid `200` capabilities response implies both exact and
similarity readiness; invalid or stale active state returns `503`. Each environment declares whether
PhraseSearch is expected to be `available` or intentionally `unavailable`, and smoke compares reality
with that declaration.

The ready journey verifies that the active build ID matches the approved fixture or builder manifest.
Backend tests own unavailable `503`, stale-reference `409`, bounded compute behavior, and conditional
GET behavior. ETags are compared only within one API process and only on endpoints that support them;
capabilities itself is not an ETag endpoint. The browser proves an available query, resolves the
selection, invokes the persistent Add to Workspace action, and independently rereads the workspace.
The selection-resolution endpoint alone is not persistence evidence. The full suite also contains one
controlled unavailable-state UI check.

Quran words and PhraseSearch data are immutable during ordinary journeys. They are rebuilt only by the
dedicated build/activation tests.

### Abwab projection

Use the real UI for the representative mutation. Verify durable state through a fresh read of the
public Abwab tree, relevant detail endpoint, version/ETag behavior, and the consuming Mushaf projection
when the mutation affects it. Backend tests own concurrency, all-or-nothing template behavior,
relations, inclusion topology, archive/restore, and other semantic permutations.

## Setup, actions, and independent verification

Deterministic prerequisites may be provisioned through APIs or test-support tooling. The risk-bearing
user action must pass through the real UI when that UI exists. A write cannot be considered proven by
the same optimistic UI state that issued it: reload, use a new browser context, query an independent
read API, or inspect the owning database invariant in a Backend integration test.

Direct SQL is acceptable for isolated preconditions or Backend-only invariant checks. It cannot be the
sole cross-stack proof of a user-visible result.

Use accessible roles, names, labels, and stable URLs as Playwright selectors. Add test IDs only when no
durable semantic selector exists, such as repeated Quran glyph runs or virtualized controls. Do not use
DOM structure, implementation classes, fixed sleeps, or test ordering as contracts.

## State isolation and execution cost

Isolation means clean, independent mutable state; it does not mean repeatedly copying the full
canonical or PhraseSearch database.

- Required PR journeys use compact, source-traceable fixtures.
- Immutable Quran and PhraseSearch tables may be shared within a compatible isolated test stack.
- Between mutating scenarios, reset only an explicit allowlist of small Access/session/audit, Abwab,
  Linking, job/outcome, and related projection tables. Verify that the expected clean state was restored.
- Journey groups may share a PostgreSQL server, database, and application bootstrap when this reset
  contract preserves independence.
- Use separate stacks only for incompatible configuration or exclusive operations, not as the default
  isolation mechanism.
- A measured copy-on-write mechanism is allowed when it satisfies the timing and cost constraints.
- Repeated physical copies or restores of the multi-gigabyte full-canonical/PhraseSearch state per
  scenario, test, or journey group are prohibited. Exact artifact sizes belong in the tracked lock.
- Large canonical and phrase-ready artifacts are provisioned once per applicable scheduled or release
  run.
- Full-canonical acquisition is Local-first on the current trusted solo-developer runner: a scheduled or
  release provisioner resolves only the lock-pinned payload beneath `QURAN_TEST_ARTIFACT_ROOT`, then
  shares its immutable restored state. It never falls back to an ambient developer, shared, staging, or
  production database. External storage is deferred until remote CI, a second machine, or another
  developer requires it.

Tests must pass independently and in random order. PhraseSearch read scenarios use a provisioned
immutable ready fixture; the one-shot build/activation test receives separate eligible state. See
[Test Artifacts](./test-artifacts.md) for the complete contract.

## Target gate matrix

### Pull requests

After fresh measurement, every PR has four parallel, independently bounded jobs:

1. Backend PR lane.
2. API contract and pending-model verification.
3. Frontend policy checks, type-checking, and production build.
4. Critical Chromium Playwright journeys, with mobile emulation only where tagged.

The Backend candidate remains the full supported `Backend/scripts/test-backend pre-pr` lane unless
fresh evidence proves it violates the 12-minute or artifact-cost contract. The current `pre-pr` command
requires large canonical resources, while large resources are intentionally scheduled/release inputs.
That incompatibility must be resolved explicitly during the activation pilot: retain all affordable
Backend classes in the PR lane, supply compact faithful equivalents where possible, and keep the
artifact-bound full-canonical classes required in scheduled/release lanes. Do not silently label a
reduced lane as the current full `pre-pr` command.

Contract and model verification use the repository-supported Backend scripts. Frontend verification
includes `test:pre-pr` and `e2e:typecheck`. The provider-neutral
[PR observation matrix](./pr-observation-matrix.md) exposes all four jobs with one attempt, an outer
12-minute timeout, and structured end-to-end duration. The matrix is explicitly non-blocking; no
document may claim the critical browser gate is enforced until it meets the activation criteria below.

The 12-minute budget:

- Includes dependency/artifact provisioning, database preparation, application startup, and test
  execution.
- Ends when the last required job completes; runner queue time is excluded.
- Uses an outer job timeout rather than relying only on per-test hang diagnostics.
- Must not be met by silently removing critical evidence.

Before enforcement, run a minimum activation pilot of 20 representative executions, including five
cold artifact-cache runs. Every run must finish within 12 minutes, overall p95 must remain below 10.5
minutes, and there must be no first-attempt flaky passes. Continue monitoring after enforcement.

Each critical journey group enters observation mode, completes its own 20-run pilot, and becomes
blocking independently. The four-job matrix is fully adopted only after all five groups are enforced.

### Scheduled, release, and post-deploy lanes

| Lane | Distinct evidence |
| --- | --- |
| Nightly | Full Chromium Playwright suite, designated mobile variants, PhraseSearch build/activation, destructive importer/restore tests, artifact verification, accessibility scans, and non-blocking browser timing |
| Weekly and lockfile changes | Risk-based NuGet/npm advisory evaluation |
| Release candidate | Previous-release migration upgrade, isolated staging critical journeys, real Logto sentinel, complete artifact verification, and manual release charter |
| Post-deploy | Read-only production smoke against declared environment expectations |

Do not repeat work merely to fill a lane. A scheduled or release run provisions each required large
artifact once and shares its immutable state; it never recopies that state per test.

The production smoke verifies an exact Mushaf read, declared PhraseSearch state, and the deployed
same-origin UI-to-API rewrite. Anonymous denial uses only a harmless protected read, such as
`GET /api/access/me` returning `401`; production smoke never probes denial with
`POST`/`PUT`/`PATCH`/`DELETE`. When a dedicated least-privilege canary exists, the same safe read also
proves cookie-backed identity. If direct Railway-origin CORS is an intentional supported contract, test
it separately with a safe preflight/request carrying the deployed UI `Origin`. The smoke performs no
production mutations.

## Hermeticity and reproducible inputs

Required PR jobs have two phases:

1. **Controlled provisioning:** locked dependency restore, the exact Chromium revision, digest-pinned
   PostgreSQL images, verified compact artifact retrieval, ephemeral HTTPS certificates, and build.
2. **Sealed execution:** no external egress, preloaded dependencies/images, local OIDC/JWKS and Logto
   Management API stubs, verified fixtures, and `--no-build`/`--no-restore` execution where supported.

Retain the browser request-leak detector and also enforce process/container egress denial. Artifact
credentials exist only during provisioning. Only the serialized staging Logto sentinel receives an
explicit external allowlist.

The implemented provider-neutral harness uses a preloaded system-call guard for the browser,
Frontend server, API, and child processes. It admits only loopback plus the exact private PostgreSQL
address; the database container remains on a Docker internal network. A stale provisioning receipt,
missing output, missing browser/image, or credential-bearing child environment fails closed.

Required infrastructure inputs are immutable and reviewed: PostgreSQL images are pinned by digest,
NuGet uses locked restore, npm uses the committed lockfile, and the Playwright browser revision is part
of cache identity. CI generates ephemeral HTTPS certificates; secure-cookie journeys never downgrade
to HTTP.

The target harness must expose explicit local database modes:

- `artifact`: deterministic default for critical/full commands and the only acceptable target CI mode.
- `clone-local`: opt-in loopback-only developer convenience; it is non-canonical and cannot serve as
  release evidence.

The implemented harness must never infer clone mode from user secrets, and no test lane may point at
production or a shared staging database.

## Migrations, destructive operations, and dependencies

The operational P0 track runs in parallel with the journey roadmap:

- Artifact trust, verification, and reproducible restore.
- PhraseSearch one-shot build safety and post-build capability proof.
- Abwab snapshot export/import and topics import checksum, rollback, and exact-result protection.
- Previous-release database upgrade testing.
- Verified backup/restore rehearsal.

A schema release restores the previous released database shape and representative data, applies
migrations forward, boots the application, and reruns canonical and critical sentinels. Forward-only
recovery is proven through verified backup/restore rather than requiring a potentially unsafe `Down`
implementation.

Dependency advisories run when lockfiles change, on schedule, and before release. Confirmed high or
critical production exposure blocks. An unreachable or development-only advisory requires analysis
rather than automatic failure. Waivers identify package/path, rationale, owner, mitigation, and expiry.
The provider-neutral [dependency advisory evaluation](./dependency-advisory-evaluation.md) implements
those triggers, scope/path evidence, fail-closed waiver rules, and structured results. It is a distinct
weekly/change/release contract and is deliberately unavailable as a nightly trigger.

## Accessibility, performance, RTL, and responsive behavior

- Critical Playwright journeys include keyboard/focus assertions and automated accessibility scans.
  Serious and critical violations block; lower severities remain visible work.
- Chromium desktop is the primary project. Mobile emulation is limited to responsive-critical Mushaf
  and Linking behavior. Firefox and WebKit are out of scope.
- RTL and responsive protection uses functional assertions, not a blanket screenshot suite.
- Query-count gates apply only to genuinely expensive or N+1-prone Backend paths.
- Browser timing remains non-blocking until stable baselines exist. Promote a specific budget only after
  repeated stable measurements.

## Change obligations

Every pull request states which risks and journeys it affects. Then:

- New or changed P0/P1 behavior updates protection at its owning layer.
- User-observable cross-stack behavior updates its Playwright sentinel when the contract changes.
- A bug fix adds or improves regression protection at the narrowest faithful seam unless the pull
  request explains why that is disproportionate.
- HTTP changes run contract verification and inspect affected handwritten Frontend clients.
- Migration/import changes run the applicable pending-model, rollback, canonical, and post-operation
  read protection.
- "No test change" requires an evidence-based explanation, not a checkbox.

Preserve the behavioral value of the existing 123 Backend classes and 24 Playwright tests. Improve,
consolidate, or remove them only with evidence that protection is redundant, misleading, or replaced by
stronger proof. Do not perform a wholesale rewrite.

## Failure, flakes, and evidence

Required PR tests have no retry. Scheduled runs may retry once for classification, but the run remains
failed and records both attempts. There is no silent quarantine.

An emergency downgrade requires:

- An issue and named owner.
- Affected risk and rationale.
- Maintainer approval.
- An expiry of at most seven calendar days.

Persist structured Backend and Playwright results with timings. On failure, retain sanitized
application/container logs, Playwright trace, screenshot, console errors, and relevant request metadata.
Never retain credentials, tokens, raw production-derived databases, or sensitive response bodies.
Retain failed diagnostics for at least 14 days and aggregate timing history for at least 30 days.
Diagnostic database dumps are opt-in, sanitized, and checksummed.

The sealed Playwright harness emits provider-neutral JSON for artifact provisioning, database
preparation, application startup, and test execution. Failed-run evidence contains sanitized step
events, text/media-masked screenshots, logs, console errors, and request method/origin/path/status only; it explicitly
records that headers, bodies, and database dumps were not captured. Upload/retention wiring belongs to
the observation jobs rather than to a specific CI provider here.

A failed post-deploy smoke halts promotion. In production it opens an incident and triggers application
rollback when evidence points to the new release. Database rollback is never automatic; data recovery
uses the verified restore procedure and explicit operator approval.

## Manual release charter

Keep manual evidence short and limited to behavior automation cannot faithfully establish:

- Representative Arabic/Quran typography inspection.
- Assistive-technology sampling beyond automated checks.
- Restore rehearsal for destructive operational changes.
- Staging real-Logto redirect/callback/logout and provider configuration review.

Repeated defects found manually become candidates for automation at the narrowest faithful seam.

## Strategy health

Track risk and evidence, not blanket coverage:

- 100% of P0 journeys mapped to success, failure/recovery, oracle, owning layer, cross-stack sentinel,
  and required lane.
- Every P1 risk protected or recorded with an owner and review date.
- Required-lane p95 and maximum wall-clock.
- First-attempt flake rate.
- Escaped P0/P1 defects and the missing or ineffective protection.
- Expired downgrades, dependency waivers, and stale artifact locks.
- Artifact refresh frequency and unexplained oracle/count changes.

Line or branch coverage may be used locally as diagnostic evidence. It is never a target or release
gate.

## Governance and review triggers

The pull-request author declares risk impact; the reviewer validates it. Golden Quran oracle or
artifact changes require an independent reviewer and explicit old/new hashes, counts, sentinels, and
reasons. Emergency downgrades require maintainer approval.

Review the journey map after:

- An escaped P0/P1 defect.
- A new Permission, Owner-only route, or destructive importer verb.
- A migration, canonical source, or artifact refresh.
- A PhraseSearch availability-policy change.
- A repeated flake or material timing regression.
- The lightweight quarterly review.

CI integration remains provider-neutral. Scripts and job contracts define inputs, outputs, timeouts,
and required results; CI-provider selection is a separate decision.

## Adoption roadmap

Two tracks proceed together.

The cross-stack journey track follows the approved order:

1. Quran fidelity.
2. Device sessions and Permissions.
3. Linking.
4. PhraseSearch.
5. Abwab projection.

The operational track begins immediately with artifact trust, destructive imports/restores,
PhraseSearch build safety, migration upgrades, and backup/restore proof.

The strategy is **designed** when this document, the artifact document, and ADR 0001 accurately record
the current state, gaps, decisions, and target rollout. It is **adopted** only when deterministic
fixtures exist, operational P0 protection is enforced, all five journey groups have passed activation
pilots and block appropriately, and release/post-deploy lanes are active. Until then, the open statuses
in this document remain explicit work.
