# 028 Abwab Safety Foundations — Completion & Validation Report

**Feature:** `028-abwab-safety-foundations` (fail-closed substrate) · **Branch:**
`028-abwab-safety-foundations` · **Date:** 2026-07-22 · **Phase:** 9 (Polish &
Cross-Cutting Concerns) · **Exit criterion:** SC-011 (every §18.2 exit/acceptance gate
passes; first Abwab→Quran FK remains prohibited until acceptance).

This is the acceptance-evidence record for the six delivered stages (US1–US6). It records the
per-gate exit results, the phase commit hashes, the known deviations / carry-forwards, and the
handoff to `029`. It does **not** replace `destructive-path-inventory.md` (the US2 enumeration),
which stays as the destructive-path evidence.

## Phase commit hashes

| Stage | Commit | Subject |
|---|---|---|
| Setup | `3ebcddb` | Abwab substrate + test + e2e scaffold folders |
| Foundational | `d691e1d` | shared real-PG fixture + FK prohibition guard |
| US1 | `f679964` | CI & migration-safety pipeline (real-PG + gates) |
| US2 | `30c9c80` | Quran import safety & destructive-path lockdown |
| US3 | `c2ecb8a` | audit/timeline/write/concurrency/time kernel |
| US4 | `4140651` | shared frontend foundation (§14.1 generic primitives) |
| US5 | `36b69e7` | System Owner & permission foundation (security slice) |
| US6 | `bdeed50` | durable notification storage (no public surface) |

Migrations delivered (EF-tooling generated): `20260722142911_AddAbwabSafetyKernel` (US3/T038),
`20260722161332_AddSecurityOwnershipAndPermissions` (US5), `20260722180938_AddAbwabNotificationStorage`
(US6/T067).

## Exit-gate results (quickstart.md §18.2, run 2026-07-22 on this checkout)

Environment: .NET 10.0.110, Node 20.20.2 / npm 10.8.2, Docker available (Testcontainers
`postgres:16-alpine`), Playwright chromium 1.61.1.

| # | Gate | Command | Result |
|---|---|---|---|
| 1 | **Authoritative whole-project backend test** (real PG, no filter) | `dotnet test …/QuranDashboard.Tests.csproj -c Release` | **PASS** — `Failed: 0, Passed: 1775, Skipped: 0, Total: 1775`, 4m11s |
| 2 | Backend Release build | `dotnet build Backend/QuranDashboard.sln -c Release` | **PASS** — 0 Warning(s), 0 Error(s) |
| 3 | Frontend unit tests (Vitest fork cap) | `npm test` (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2 ng test`) | **PASS** — 177 files, 1974 tests passed; exactly 2 vitest workers spawned (cap enforced) |
| 4 | Frontend production build | `npm run build` | **PASS** (exit 0). Pre-existing bundle-/SCSS-budget **warnings** on mushaf components (unrelated to 028; non-blocking) |
| 5 | Vitest fork-cap gate | `npm run check:fork-cap` | **PASS** |
| 6 | Frontend no-drag source gate (FR-041) | `npm run check:no-drag` | **PASS** — no drag/drop tokens in `src/` |
| 7 | Foundation-boundary gate (FR-023/SC-010) | `npm run check:foundation-boundary` | **PASS** — primitives Forms/HTTP/domain-free |
| 8 | License gate | `npm run check:licenses` | **PASS** — no forbidden prod-dep licenses |
| 9 | Security-audit + secret/license (FR-042) | `Backend/scripts/security-audit.sh` | **PASS** (exit 0) — blocking gates clean: secret scan (fail-closed), license, backend `dotnet list --vulnerable`. Frontend `npm audit` **report-only** by design (26 pre-existing transitive advisories owned by the `dependency-audit` workflow) |
| 10 | Staged source-strategy gate | `Backend/scripts/check-import-source-strategy.sh` | **PASS** — importer resolves sources only under `resources/import-sources/` |
| 11 | API contract-drift gate | `Backend/scripts/check-api-contract` | **PASS** (exit 0) — "API contract up to date"; regenerated swagger/models/docs produced no `git diff` |
| 12 | Playwright harness/spike/e2e | `npx playwright test` | **PASS** — 6/6 (harness: RTL+ARIA, keyboard-focus restoration, critical-dialog Escape+focus return, virtualized window; permissions non-authoritative; synthetic-tree spike) |
| 13 | CI workflow valid + wires all gates | `.github/workflows/ci.yml` | **PASS** — valid YAML; 4 jobs (`backend-tests`, `api-contract-drift`, `frontend`, `security-audit`) wire every gate above |

**Exit verdict:** all §18.2 gates green. SC-011 satisfied on this checkout; acceptance is the
repo owner's to grant.

## Clean-code & test-guard self-check (T073)

The delivered 028 diff (`341ffbf..HEAD`, excluding Migrations/`*ModelSnapshot*`/`*.Designer.cs`)
was scanned against the clean-code-guard reference pack and the test-guard rules.

- **Findings:** the code is clean. No unused usings/imports, no dead code, no debug/TODO/FIXME
  leftovers, no WHAT-narration or doc-comment boilerplate, no naming/typo issues. Test-guard: real
  PostgreSQL for persistence/concurrency, real DTOs/entities constructed, test doubles
  (`FixedServerClock`, `RecordingCachePublisher`) target real ports — no violations.
- **Fix applied (comment-only, no behavior/assertion/migration change):** removed/tightened three
  banner comments in the test-support helper
  `Backend/tests/QuranDashboard.Tests/Abwab/_Support/SecurityTestHarness.cs` (deleted the
  `// --- Read helpers ---` banner; reworded the two `// --- … (each on a fresh context) ---`
  banners to plain sentences keeping the invariant note). No test assertion, mock, or logic was
  touched; the authoritative suite (gate 1) re-ran green (1775 passed).
- **Deferred (report-only, not a safe mechanical fix):** `PermissionAdministrationHandler`
  has two `DescribeTarget` overloads with identical bodies for `GrantPermissionCommand` vs
  `RevokePermissionCommand`; the types share no interface, so collapsing them is a design choice,
  not a safe rename — left as-is. (Non-issue: `CanonicalQuranSourceVerifier.SourceManifest`'s
  `Revision`/`StableIdScheme` are JSON-deserialization DTO fields, not dead code.)

## READMEs updated / verified (T072)

Verified accurate (already updated by their phases; no change needed): importer
(`Backend/tools/QuranDashboard.DataImporter/README.md`), Quran DataPipelines
(`…/Persistence/DataPipelines/Quran/README.md`) + Safety (`…/Quran/Safety/README.md`),
API Security (`Backend/api/QuranDashboard.Api/Security/README.md`), Notifications
(`…/Infrastructure/Abwab/Notifications/README.md`), frontend `core` and `shared`
(`src/app/core/README.md`, `src/app/shared/README.md`), permissions feature, frontend root,
and `Backend/scripts/README.md`.

Gap filled: added `Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/README.md` — the
Abwab **write-kernel** infrastructure area had no README (only its `Notifications/` sub-area did).
It documents the two-layer kernel (SavingChanges guard + barrier-gated audited-commit executor),
the `FOR UPDATE` row-lock serialization, post-commit cache publication, the sealed personal-delete
policy, the server clock, and the invariants (row-lock-not-xmin, read-API locks, DB append-only
defense, no Abwab→Quran FK).

## Known deviations / carry-forwards

1. **Redacted Neon PRODUCTION credential still in git history.** During US1 a real Neon production
   DB credential was redacted from a committed 2026-07-18 review doc, but it **remains in git
   HISTORY**. The repo owner **MUST rotate** the credential and move it to user-secrets/env. An
   optional git-history scrub is a separate step. (The secret-scan gate is fail-closed on the
   working tree; it does not rewrite history.)
2. **OpenAPI/swagger baseline lost Arabic operation descriptions.** US1 regenerated the
   OpenAPI/swagger baseline to match current code; this dropped Arabic operation descriptions that
   were already **dead since commit `78d70f0`**. Restoring them (via XML-doc generation) is a
   separate owner decision, **deliberately not taken** here.
3. **US3 concurrency = `FOR UPDATE` row-lock, not xmin.** Npgsql 10 removed
   `UseXminAsConcurrencyToken`; `AbwabRevisionState` concurrency is enforced by the mandated
   `FOR UPDATE` row-lock (the data-model lists `Version (xmin)`; the substitution is documented in
   `AbwabAuditedCommitExecutor`/`AbwabRevisionState`). **Forward 029-hardening:** (a) the
   stabilization/generation writer-discovery scan is **assembly-scoped (Application/Api)** — assert
   that writers live in the scanned assemblies before 029 adds real writers; (b) wire a runtime
   append-only DB role — the `abwab_app` role is currently NOLOGIN defense-in-depth, and the
   **append-only trigger is the live guarantee**.
4. **US2 destructive-import behavior change.** `--force` imports now require the
   `QURANDASHBOARD_ALLOW_DESTRUCTIVE_IMPORT` opt-in **and refuse in Production** (behavior change).
   The restricted-role GRANT/REVOKE was delivered in **US3/T038**.
5. **US4 test-only dependency.** `fake-indexeddb` was added as a **devDependency** (test-only; used
   to inject a fake `IDBFactory` into the IndexedDB store spec).
6. **US6 forward 032-hardening notes.** Read-state first-write race is **fail-closed**; add a live
   concurrent-dedup test in 032; the Playwright harness omits the write-guard interceptor — **no
   gap** (harness scenarios drive inline content, not the audited kernel).

## Acceptance handoff to 029 (T075)

- **FK prohibition still enforced at exit.** `NoPrematureQuranFkTests` is green in the authoritative
  run: `NoForeignKeyConnectsAbwabAndQuran` (no Abwab↔Quran FK exists) plus both non-vacuity
  assertions — `QuranEntitiesAreClassifiedSoTheGuardIsNotVacuous` and
  `AbwabEntitiesAreClassifiedSoTheGuardIsNotVacuous` (the guard sees real Quran **and** real Abwab
  substrate entities, so the boundary check cannot pass vacuously from either side).
- **Handoff:** per FR-009 / §18.2, `029` may add the **first** Abwab→Quran foreign key **only after
  this feature's exit is accepted**. Until then the FK guard is the enforced gate. The write kernel
  (ChangeSet UoW, `AbwabWriteBarrier`, timeline generation, server clock), the security slice, and
  notification storage are ready for `029`–`034` to build on.
