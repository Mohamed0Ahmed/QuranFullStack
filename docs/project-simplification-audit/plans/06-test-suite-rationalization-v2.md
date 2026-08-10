# Test Suite Rationalization V2 — Minimum Durable Test Estate Implementation Plan

**Supersedes** the previous contents of this file (the Stems/Lemmas sorting-pilot plan), which is
obsolete: it optimized for sharing assertions between two frontend page specs that this plan
deletes outright.

**Primary evidence:** `docs/project-simplification-audit/reports/test-suite-rationalization-current-state.md`
(measured at `0d5e5a97`, 2026-08-10). Section references below in the form *(audit §N)* point there.

**Goal.** Reduce feature implementation time, test authoring time, maintenance and refactor churn,
change amplification, agent context weight, and daily verification runtime — by shrinking the test
estate to the minimum set that still protects genuinely important risks. Coverage percentage is not
a goal and is not measured.

---

## 0. Plan contract and authority

This artifact is a plan. Creating it authorizes nothing. It does not authorize implementation, test
deletion, a product test run, a formal review, a commit, push, PR, deploy, database operation, or
audit cleanup. Each phase below becomes executable only when the owner separately authorizes it.

**Global constraints**

- `main` is protected Railway production. No phase here is executed on `main`.
- No phase weakens backend authorization or bypasses authentication. Phase 1 in particular adds an
  environment-gated *test issuer*, never an auth bypass, and adds a guard proving it is inert
  outside the test environment.
- No Quran source data, manifest, dump, or religious label is invented, corrected, normalized, or
  mutated by any phase.
- Deletion never precedes its replacement. Every deletion phase has an explicit predecessor phase
  that must be complete and verified first (§6).
- A README is amended in the same change that makes its described truth false.
- No test is deleted solely because it is slow. Slow-but-critical becomes a gate (§4.4), not a
  deletion.
- No test-count, LOC, or runtime number in §10 is an acceptance criterion on its own. The
  acceptance criteria are §9; §10 is the expected consequence.

---

## 1. Measured baseline

From the audit, at `0d5e5a97`. All figures measured, none estimated.

| | Frontend | Backend |
|---|---:|---:|
| Test files | 248 `*.spec.ts` | 257 `.cs` (268 classes) |
| Test cases (executed) | 3,371 | 2,258 |
| Test LOC | 60,247 | 43,809 |
| Full-suite runtime | 337.8 s | 429.6 s |
| Playwright E2E | 17 files / 61 cases / 1,602 LOC | — |

**Cost concentration (audit §2.3, §6):**

- Frontend spec LOC is **0.98 : 1** against all frontend product code.
- Golden UI Plan 7 changed no user-facing behavior and cost **96 spec files (+6,745 / −643)**;
  E2E cost 3 files (+12 / −11); backend cost 0 files.
- Component specs churned **69%** under that refactor; pure-logic specs **6%**; data-access **0%**.
- Lifetime edit rate: component **3.6**/file, pure-logic **0.7**/file.
- 178 of 248 specs boot a `TestBed`; they cost 113.4 s of 115.0 s test time.
- `canonical-data` is **371.1 s for 47 cases** — 86% of backend runtime for 2% of cases.
- Worst single amplification case: `access-admin-unsaved-changes.guard.ts` **+1 / −2**, its spec
  **+369 / −25** — **131 spec lines per production line**.

**Classification (audit §4):** 23 `KEEP — BUSINESS RULE`, 25 `KEEP — SECURITY`,
68 `RELEASE_OR_CHANGE_GATE`, 23 `MERGE`, 129 `DELETE`.

**Blocking prerequisite (audit §5.4):** there is no `Authorization` header anywhere under `e2e/`;
`fixtures/logto.ts` stubs only OIDC discovery and an empty JWKS. Authenticated Abwab/Access E2E does
not work today.

---

## 2. Target end state

**Frontend**
- Zero `*.spec.ts` files. No Vitest, no jsdom, no Angular unit-test target.
- Verification = application typecheck + production build + a small set of durable Playwright
  journeys + browser/visual verification.
- New `*.spec.ts` prohibited by default; an isolated unit test requires explicit owner approval
  recorded in the change's `Testing Decision` section.

**Backend**
- A **daily** suite of ~17 cohesive behavior-focused classes protecting only business
  rules/critical invariants and security/authorization.
- **68 gate classes** retained in full but executed only when their owned concern changes, or
  before release.
- Everything duplicated at another layer — read/paging/filter/sort permutations, controller
  delegation, DTO/mapping, framework behavior, report-shape permutations — deleted.

**Policy**
- One short canonical `TESTING_CONSTITUTION.md` is the single source of truth.
- Agent instruction files route to it and do not restate it.
- `TESTING_STRATEGY.md` is deleted; its surviving operational truth moves to the constitution and
  the two test READMEs.

---

## 3. Locked decisions

| # | Decision | Consequence |
|---|---|---|
| L1 | Delete **all 248** `*.spec.ts`. | The pure-logic retention option the audit recommended (§5.5) is **not** taken. Residual risk is accepted and named in §13. |
| L2 | No new `*.spec.ts` by default. | Enforced by a build-level check (Phase 7), not by convention alone. |
| L3 | Authenticated E2E capability lands **before** any Abwab/Access spec is deleted. | Phase 1 gates Phase 6 for those two areas. |
| L4 | Backend daily suite is limited to Business Rule + Security. | The 48 KEEP classes are the *starting point*, consolidated to ~17 (§4.3). |
| L5 | The 68 gate classes are retained, never silently dropped. | Requires the trigger model (§4.4) to exist before they leave the daily lane. |
| L6 | `test-gates.tsv` is replaced by class-local attributes. | Removes a 268-row manual registry with 12 commits of churn (§4.5). |
| L7 | `SmokeRouteCatalog` is retained; its duplicate write-matrix registry is removed. | One registry edit per new route instead of two (§4.5). |

---

## 4. Target architecture

### 4.1 The testing constitution

New file `TESTING_CONSTITUTION.md` at the repository root — short and operational, ≤ 120 lines. It
is the only policy document; everything else points at it.

Required content:

1. **Default is no test.** Do not add a test unless a specific important risk requires one. The
   burden of proof is on adding, not on omitting.
2. **No test-per-component, no test-per-service, no test-per-endpoint, no test-per-DTO.**
3. **No coverage-percentage target exists.** Coverage is not measured and may not be cited.
4. **Frontend `*.spec.ts` is prohibited by default.** Frontend verification is typecheck,
   production build, Playwright journeys, and browser/visual verification. An isolated frontend
   unit test requires explicit owner approval, recorded in the change's `Testing Decision`.
5. **Permanent backend tests protect only** (a) business rules and critical invariants — important
   domain rules, critical writes, transactions, concurrency, audit, restore, corruption prevention;
   and (b) security/authorization — authentication boundaries, exact permissions, Owner behavior,
   pending/disabled states, 401/403 behavior, write protection.
6. **Quran source, importer, generator, schema, and catalogue integrity checks belong to
   change-triggered or release gates**, not the daily suite.
7. **A test must normally survive internal refactoring** when the behavior it protects has not
   changed. A test coupled to DOM structure, CSS class names, container registration, collaborator
   identity, or private cache keys is a defect in the test.
8. **Choose the cheapest verification layer that can catch the risk**: typecheck → build → a single
   assertion in an existing class → a new class → a browser journey. Never the layer above when the
   layer below suffices.
9. **Do not duplicate protection across layers.** If the route-smoke tier already proves routing,
   authorization, binding and serialization, a per-feature test may not re-prove them.
10. **Every plan states a `Testing Decision`** naming only the tests/gates that specific change
    requires, or "none" with a reason.

### 4.2 Frontend target

| Layer | Runs | Purpose |
|---|---|---|
| `typecheck:app` | every change | contracts, generated DTO drift, dead references |
| `build:verify` | every change | template binding errors, budgets, real compilation |
| Playwright journeys | UI-behavior changes, pre-PR | important user behavior |
| Browser/visual verification | UI-visible changes | the Golden visual protocol already in `07-frontend-ui-architecture-v2.md` |

### 4.3 Backend daily target — 48 KEEP classes consolidated to ~17

Consolidation preserves every protected behavior. Class count falls; assertions do not. Where a
source class is absorbed, its cases move as named cases into the target class.

**Security — 25 → 9 classes (139 declared methods preserved)**

| Target class | Absorbs | Methods |
|---|---|---:|
| `AuthorizationBoundaryTests` | `AuthorizationPipelineTests`, `AuthorizationRequirementHandlerTests`, `AuthorizationStateResolverTests`, `AuthorizationRejectionResponseTests`, `UnsafeEndpointMetadataValidatorTests` | 32 |
| `IdentityProvisioningTests` | `UserProvisioningServiceTests`, `AccessMeEndpointTests`, `EmailIdentityNormalizerTests`, `LogtoSubjectRelinkEndpointTests` | 29 |
| `OwnerAuthorityTests` | `OwnerReconciliationServiceTests`, `AccessRolesTests`, `OwnerBootstrapOptionsTests` | 26 |
| `AccessAdminCommandTests` | *(unchanged — stays separate)* | 10 |
| `PermissionCatalogueTests` | `AbwabPermissionCatalogueTests`, `PermissionCatalogueSynchronizerTests` | 6 |
| `SmokeAuthorizationMatrixTests` | `SmokeAuthPipelineTests`, `SmokeAbwabWriteAuthorizationTests`, `SmokeAccessAdministrationAuthorizationTests` | 7 |
| `SmokeRouteContractTests` | `SmokeCoverageParityTests`, `SmokeRouteBaselineTests`, `SmokeRoutePipelineTests`, `SmokeBootGuardTests` | 13 |
| `ApiEdgeProtectionTests` | `RateLimitingIntegrationTests`, `GlobalExceptionHandlerTests` | 12 |
| `TestAccessPersonasContractTests` | *(unchanged — persona roster lock)* | 4 |

`AccessAdminCommandTests` stays separate **on purpose**: it is `Kind=Process`, lives in the
non-parallel `AccessProcessGlobalCollection`, and folding it into `OwnerAuthorityTests` would drag a
parallel-safe class into a serialized collection.

**Business rule — 23 → 8 classes (167 declared methods preserved)**

| Target class | Absorbs | Methods |
|---|---|---:|
| `AbwabWriteInvariantsTests` | `AbwabDoorWriteBehaviorTests`, `AbwabTemplateApplyBehaviorTests` | 39 |
| `AccessAdministrationInvariantsTests` | `AccessAdministrationEndpointTests`, `AccessAuditEventPersistenceTests` | 28 |
| `MorphologyDomainInvariantsTests` | `MorphologyAssemblerTests`, `MorphologyValidationFailureTests`, `WordLemmaNormalizationApplierTests` | 43 |
| `ImportPipelineRefusalAndRollbackTests` | `TafsirForceRebuildTests`, `TafsirRollbackTests`, `TranslationRollbackTests`, `TranslationRefusalForceTests`, `MutashabihatRefusalForceTests`, `NavigationMetadataWriteIsolationTests`, `FullI3rabForceRebuildTests`, `DisplayWordsRefusalForceTests`, `DisplayWordsSourceUntouchedTests`, `MorphologyRefusalForceTests` | 32 |
| `ImportPipelineValidationFailureTests` | `MutashabihatValidationFailureTests`, `DisplayWordsValidationFailureTests` | 6 |
| `DisplayWordsIdentityTests` | `DisplayWordsDeterministicIdTests` | 4 |
| `ReaderCorruptionGuardTests` | `AyahStudyCorruptCoveredAyahKeysTests`, `WordAnalysisSegmentFallbackTests` | 2 |
| `TestGateAttributeContractTests` | `TestGateCatalogTests`, rewritten for §4.5 | 13 |

`ImportPipelineRefusalAndRollbackTests` is the one place a `[Theory]` over a pipeline-descriptor
table is correct rather than a shortcut: *"a rebuild without `--force` is refused"*, *"a failed
import rolls back completely"*, and *"the source artifact is untouched"* are literally the same rule
across seven pipelines. Each pipeline remains a separately named, separately executed row —
collapsing them into one execution would be a coverage loss and is prohibited.

`MorphologyAssemblerTests` and `MorphologyValidationFailureTests` stay in a morphology-specific
class because their rules are domain-specific, not the shared pipeline shape.

**Daily backend total as planned here: ~17 classes, 306 declared methods, 445 executed cases.**
The method and case figures are the protected behaviors and must be preserved; the class count is a
grouping decision that review may adjust (§9 criterion 6).

### 4.4 Release/change gate trigger model

The 68 gate classes are retained and made *triggered*. Five named gates plus a release gate:

| Gate | Fires when these change | Classes |
|---|---|---:|
| `gate-source` | `resources/import-sources/**`, `resources/reference-sources/**`, any manifest or checksum | shares classes with `gate-importer` |
| `gate-importer` | importer / assembler / generator / rebuilder production code for a pipeline | 59 (Import 9, WordsMorphology 7, WordsSimpleI3rab 7, Translations 7, Tafsirs 7, Navigation 7, MorphologyEnriched 5, WordsDisplay 5, FullI3rab 3, Mutashabihat 2) |
| `gate-schema` | EF migrations, `DbContext` configuration, entity/index/constraint shape | `AccessMigrationPathTests`, `AccessSchemaDriftTests`, `PermissionCatalogueStartupSyncTests`, `AbwabSchemaTests`, the pipeline `*SchemaShapeTests` |
| `gate-catalogue` | permission catalogue, POS-tag seed, route catalogue, persona roster | `WordTypesChildCatalogueDriftTests`, `EmailIdentityPreflightTests` |
| `gate-testinfra` | `TestSupport/PostgreSql/**` | `PostgreSqlTestProcessContractTests`, `PostgreSqlDatabaseSlotContractTests` |
| `release` | before any deploy | **all** of the above, plus `SmokeDataReadTests` and the whole `canonical-data` tier |

A gate is fired by **changed-path mapping**, computed from the cumulative branch diff, in the same
place the existing lane selection already lives (`Backend/scripts/test-backend`). Two safety rules
make this non-silent:

- **Fail-closed default.** A changed backend path that matches no mapping fires `release` (all
  gates). Under-triggering is impossible by construction; only over-triggering is.
- **Gates are mandatory before release.** `release` is not optional and is not satisfied by a daily
  run. `Backend/README.md` §Deployment states this.

**Existing infrastructure supports this cleanly.** `test-backend` already selects lanes from a
per-class catalog and already runs sharded lanes; `test-gates.tsv` already carries a `Concerns`
column used for exactly this kind of secondary selection. The change is a new selector plus a
changed-path mapping table — not new machinery.

### 4.5 Registry churn reduction

Two registries drive most backend test churn (audit §6.3). Both are fixed, neither is weakened.

**`test-gates.tsv` → class-local attributes.** 268 rows, 12 commits, and every added/renamed/moved/
deleted test class must edit it. Replace with:

```csharp
[TestGate(Feature.Access, TestKind.Database, Gate.Daily)]
[TestGate(Feature.Tafsirs, TestKind.Database, Gate.Change, Trigger.Importer)]
```

`TestGateAttributeContractTests` then asserts every discovered test class carries exactly one
`[TestGate]` and that its enum values are internally consistent. The protection is identical — a
class cannot escape classification — but **registration is local to the class, so adding a test
class edits one file instead of two and can never drift.** Lane selection reads attributes by
reflection from the built assembly; `--no-build` still works because it reads the existing DLL.
Delete `test-gates.tsv` and its parser — in Phase 3b, once the estate is final.

**`SmokeRouteCatalog` → keep, de-duplicate.** The catalog and its bidirectional parity lock are
*retained*: they are the reason 129 read-test classes are deletable at all. But
`SmokeCoverageParityTests.PhaseFiveAbwabWriteMatrix` hardcodes 21 (method, template, permission)
triples of which 18 route strings are byte-duplicates of catalog literals. Derive that matrix from
`SmokeRouteCatalog` entries whose access kind is `Permission` or `OwnerOnly`. Result: **adding a
route edits exactly one file.**

### 4.6 Playwright journeys — 18 proposed flows consolidated to 8

The audit proposed 18 flows (§5.3). Consolidated to 8 durable journeys, because a journey that
traverses a real user path incidentally covers most of the narrow flows around it.

| # | Journey | Covers | Auth |
|---|---|---|---|
| 1 | `reader-journey.e2e.ts` *(extends `mushaf-reader.e2e.ts`)* | URL contract + self-correction, deep link, bare-entry session restore, selection interplay, peek lifecycle, surah/source pickers, RTL keyboard word stepping | no |
| 2 | `reader-integrity.e2e.ts` | Uthmani segment slicing, highlight refusal on text mismatch, waqf-mark transform, ayah-end marker guard, loading geometry reservations | no |
| 3 | `explorer-journey.e2e.ts` *(extends `words-explorers.e2e.ts`)* | explorer URL contract, Word Types dual scope machine, keyboard table browsing, count-range commit semantics | no |
| 4 | `overlay-journey.e2e.ts` | detail-overlay frame grammar, stack/history, dialog Back vs browser Back, close/restore, base continuity, reference-counted scroll lock, single focus trap | no |
| 5 | `request-economy.e2e.ts` | switch debounce, cache identity and no-refetch, lazy similarity tabs, load-once catalogue, stale-page guard, out-of-order response guards | no |
| 6 | `abwab-authoring.e2e.ts` | per-permission write affordance gating, destructive-write confirmation arithmetic, optimistic-concurrency tokens, bulk 404/400 recovery, move-picker cycle guard, 401/403 write handling | **yes** |
| 7 | `abwab-deep-links.e2e.ts` *(extends `abwab-url-and-a11y.e2e.ts`)* | dead section id fallback, archived-door restore refusal, retained `relations-<id>-closed` key | no |
| 8 | `access-admin-journey.e2e.ts` | permission-draft no-silent-revoke, fail-closed assignment over degraded catalogue, unsaved-changes guard, account variant matrix, audit append + Load-more, identity relink, Owner-gated navbar affordance | **yes** |

Five new files, three extensions of existing flows. Journeys 6 and 8 are gated on Phase 1.

---

## 5. Exact deletion and retention targets

| Body | Current | Delete | Retain | Notes |
|---|---:|---:|---:|---|
| Frontend `*.spec.ts` | 248 files / 3,371 cases | **248 / 3,371** | 0 | L1 |
| Frontend E2E flow files | 17 | 0 | 17 + **5 new** = 22 | 3 extended in place |
| Backend `DELETE` | 129 classes / 1,142 cases | **129 / 1,142** | 0 | audit §4.5 |
| Backend `MERGE` | 23 classes / 229 cases | 23 class files | content folded into keepers | only where it adds unique protection |
| Backend `KEEP` | 48 classes / 445 cases | 0 behaviors | **~17 classes / 445 cases** | consolidated §4.3; count is a target, behaviors are binding |
| Backend `GATE` | 68 classes / 438 cases | 0 | 68, triggered | §4.4 |
| `test-gates.tsv` | 268 rows | **whole file** (Phase 3b) | — | replaced by class-local attributes |
| Frontend unit-test infra | see Phase 7 | whole set | — | — |
| `TESTING_STRATEGY.md` | 652 lines | **whole file** | — | replaced by constitution |

---

## 6. Phase dependency graph

```
  P0  Constitution + agent routing            (independent)

  FRONTEND TRACK
  P1  Authenticated E2E capability ──┐
                                     ├──> P2b Auth journeys (6, 8) ──┐
  P2a Non-auth journeys (1–5, 7) ────┘                               ├──> P6 Frontend
                                                                     │    spec deletion
  BACKEND TRACK                                                      │
  P3a Gate infrastructure                                            │
      (TSV retained as transition source)                            │
       └──> P4 KEEP consolidation                                    │
            (+ [TestGate] on the final classes only)                 │
             └──> P5 Backend deletion                                │
                  (DELETE + MERGE + absorbed KEEP sources)           │
                   └──> P3b Registry cutover                         │
                        (attribute the 68 gate classes, delete TSV)  │
                         │                                           │
                         └───────────────┬───────────────────────────┘
                                         │
                                    P7 Infra removal ──> P8 Verify
```

- **P2a is independent of P1** and should start immediately in parallel.
- **P6 requires P2a + P2b complete and green.**
- **The registry migration is deliberately split.** P3a builds the attribute and trigger
  infrastructure but attributes **no existing class**; the TSV remains the classification source
  during the transition. Attributes are applied only to classes that survive: the consolidated daily
  classes in P4, and the 68 gate classes in P3b. **No class that P5 deletes is ever attributed.**
- **P5 requires P3a and P4** — consolidation must complete before the source classes disappear.
- **P3b requires P5**, because final-estate parity can only be verified once the estate is final.
- **P7 requires P3b, P5 and P6.**

---

## 7. Phases

Each phase states scope, dependencies, safety gate, verification, and rollback.

### Phase 0 — Constitution and agent routing

**Scope (create/modify):**
- **Create** `TESTING_CONSTITUTION.md` (§4.1), ≤ 120 lines.
- **Modify** `CLAUDE.md`, `AGENTS.md` — replace the two testing rows in *Trigger routing*:
  - *Selecting, running, or reporting tests* → `TESTING_CONSTITUTION.md` plus the nearest test README.
  - *Writing or reviewing test code* → `TESTING_CONSTITUTION.md`, then
    `.claude/skills/test-guard/SKILL.md` (Claude) / `.agents/skills/test-guard/SKILL.md` (Sol/Codex)
    and only the stack-relevant reference.
- **Modify** `Backend/CLAUDE.md`, `Backend/AGENTS.md`, `Frontend/quran-dashboard-ui/CLAUDE.md`,
  `Frontend/quran-dashboard-ui/AGENTS.md` — same two rows, area-scoped.
The requirement that every future implementation plan carries a `Testing Decision` section is
constitution rule 10 (§4.1). It needs no template file of its own.

**The routing rule is one sentence in each file.** The constitution is never restated or summarized
in an instruction file — that is what created the current duplication.

**Dependencies:** none. **Safety gate:** none (documentation only).
**Verification:** each of the six instruction files names `TESTING_CONSTITUTION.md` exactly once in
its testing rows; no instruction file restates a constitution rule.
**Rollback:** revert; nothing else depends on P0 mechanically.

### Phase 1 — Authenticated E2E capability

The hard prerequisite (audit §5.4). **This phase must not weaken backend authorization.**

**Design.** The API already validates RS256 tokens through the real `JwtBearer` handler, and the E2E
suite already stubs the Logto origin. Add a **test issuer** the API trusts *only* under an explicit,
environment-gated flag:

- Backend accepts a second issuer/JWKS **only when** `ASPNETCORE_ENVIRONMENT` is `Testing`
  **and** `E2E:TestIssuer:Enabled=true`. Both conditions required; default is off.
- Playwright mints an RS256 token from a local test key and serves the matching JWKS on the
  already-stubbed Logto origin (`e2e/fixtures/logto.ts` currently returns an empty key set —
  it becomes the real fixture seam).
- The persona's **grants are provisioned through the real access path**, not injected. Tokens go
  through the real handler; permission checks are the real checks. Nothing is bypassed.

**Scope:**
- `Backend/api/.../Program.cs` + auth configuration — the gated second issuer.
- `Frontend/quran-dashboard-ui/e2e/fixtures/logto.ts` — serve a real JWKS.
- **New** `e2e/fixtures/auth.ts` — persona minting, session seeding, per-test teardown.
- **New** security guard case in `AuthorizationBoundaryTests`: *the test issuer is rejected in
  Development and Production even when the flag is set.* This is a `KEEP — SECURITY` behavior and
  is permanent.

**Dependencies:** none. **Safety gate:** the new guard case must be red before the gating logic
exists and green after. A reviewer confirms the flag defaults off and production config cannot
enable it.
**Verification:** one authenticated Playwright smoke navigation reaching a permission-gated
affordance; `Backend/scripts/test-backend feature --class ...AuthorizationBoundaryTests` green.
**Rollback:** revert; no test has been deleted at this point.

### Phase 2a — Non-authenticated journeys (1–5, 7)

**Scope:** `e2e/reader-journey.e2e.ts` *(extend)*, `e2e/reader-integrity.e2e.ts` *(new)*,
`e2e/explorer-journey.e2e.ts` *(extend)*, `e2e/overlay-journey.e2e.ts` *(new)*,
`e2e/request-economy.e2e.ts` *(new)*, `e2e/abwab-deep-links.e2e.ts` *(extend)*,
`e2e/README.md`.

**Dependencies:** none. **Safety gate:** each journey must fail when its protected behavior is
deliberately broken — a non-vacuity check per journey, recorded in the phase evidence. A journey
that passes against a broken build is not coverage.
**Verification:** `npm run e2e` green twice consecutively (stability), `npm run e2e:typecheck` green.
**Rollback:** revert; specs are still present and still protecting.

### Phase 2b — Authenticated journeys (6, 8)

**Scope:** `e2e/abwab-authoring.e2e.ts` *(new)*, `e2e/access-admin-journey.e2e.ts` *(new)*,
`e2e/fixtures/abwab.ts` *(use the authenticated persona instead of anonymous writes)*,
`playwright.config.ts` *(the auth journeys join the serial `abwab` project — they write)*.

**Dependencies:** P1. **Safety gate:** the existing Abwab sandbox teardown invariant still holds —
no live `e2e-sandbox-*` door or section may remain after a run (`GET /api/abwab/tree` is the check).
**Verification:** `npm run e2e` green twice consecutively; sandbox residue check clean.
**Rollback:** revert.

### Phase 3a — Gate trigger infrastructure (no class edits)

Builds the mechanism only. **It attributes no existing test class**, because ~152 of the 268 are
about to be deleted and attributing them would be pure waste — mechanical edits to classes with a
scheduled removal date.

**Scope:**
- **New** `Backend/tests/QuranDashboard.Tests/TestSupport/Execution/TestGateAttribute.cs` and its
  `Feature` / `TestKind` / `Gate` / `Trigger` enums.
- **Modify** `Backend/scripts/test-backend` — lane selection resolves each class's classification
  from its `[TestGate]` when present, **falling back to the TSV row when absent**; new `daily`,
  `gate-source`, `gate-importer`, `gate-schema`, `gate-catalogue`, `gate-testinfra` and `release`
  lanes; changed-path → gate mapping with the fail-closed default (§4.4).
- **Modify** `TestGateCatalogTests` — during the transition it asserts every discovered class is
  classified by **exactly one** of the two sources, and that no class carries both an attribute and
  a TSV row that disagree.
- **Modify** `Backend/tests/QuranDashboard.Tests/README.md`, `Backend/scripts/README.md`,
  `Backend/README.md` §Deployment (release gate is mandatory).

**The dual-source resolver is explicitly temporary transition scaffolding with a scheduled
deletion in P3b.** It is not a compatibility layer to be kept, and no phase after P3b may read a TSV.

**Dependencies:** none. **Safety gate:** a lane-equivalence check asserts every lane resolves to the
same class set as the pre-change TSV-only selection. **Verification:** `pre-pr` unchanged in
composition; the new gate lanes resolve to the class sets named in §4.4.
**Rollback:** revert; nothing has been deleted or reclassified.

### Phase 4 — Backend KEEP consolidation (48 → ~17)

**Scope:** the 48 classes named in §4.3, consolidated into the target daily classes. Fold `MERGE`
content **only where it adds unique protection** — `SmokeAbwabWriteTests` (73 methods) contributes
only what `AbwabWriteInvariantsTests` does not already prove one layer down, and its authorization
matrix stays in `SmokeAuthorizationMatrixTests`.

**Each consolidated class is created carrying its `[TestGate(..., Gate.Daily)]` attribute.** These
are the first attributed classes in the repository; the source classes they absorb are never
attributed, because P5 deletes them.

**The ~17 figure is a plan target, not a contract.** §4.3's grouping is the reviewed starting point;
implementation and review may land on a smaller or slightly larger cohesive set where that is
demonstrably safer or clearer. The binding constraints are the four in acceptance criterion 6 (§9),
not the number.

**Dependencies:** P3a (the attribute type exists).
**Safety gate:** a behavior-preservation matrix mapping every source case to its destination case,
reviewed before the source classes are removed. Case count may fall where setup was duplicated;
**behaviors may not.**
**Verification:** `test-backend tier-b` and `smoke` green; the consolidated classes execute ≥ the
445 baseline executed cases minus only cases the matrix explicitly records as duplicate.
**Rollback:** revert; the audit report is the durable record of what each class protected.

### Phase 5 — Backend deletion (129 DELETE + 23 MERGE + absorbed KEEP sources)

**Scope:** delete the 129 `DELETE` class files, the 23 `MERGE` class files whose content P4 folded,
and the **KEEP source classes P4 absorbed** into consolidated targets. Delete now-unreferenced
fixtures, seeds, interceptors, and collection definitions — including `SqlCommandCountInterceptor` /
`RowMaterializationInterceptor` if the query-budget assertions that used them are gone.

Every class deleted here is one that was never attributed, and every class that remains after this
phase is either already attributed (P4's daily classes) or is a gate class awaiting P3b.

**The temporary TSV stays valid throughout this phase.** `test-gates.tsv` is still the
classification source for unattributed classes until P3b, so a deleted class that keeps its row
would leave the registry naming a class that no longer exists — and
`TestGateCatalogTests`' transition assertion (P3a) fails on exactly that. Therefore:

- **Delete each class and its TSV row in the same change.** Never one without the other.
- **Do not add `[TestGate]` to any class this phase deletes** — attributing a class on its way out
  is the waste this sequencing exists to avoid.
- After this phase the TSV classifies **only surviving, still-unattributed gate classes**. Nothing
  else may remain in it.
- P4's consolidated daily classes stay attribute-classified and have no TSV row at any point.

P3b then attributes the remaining gate classes, verifies final-estate completeness and parity, and
deletes the TSV and the fallback resolver entirely.

**Dependencies:** P3a, P4. **Safety gate:** before deleting each class, confirm its protection is
either (a) recorded as duplicated in the audit's per-class rationale, or (b) present in a P4 target
class. The six risk-named DELETE classes (audit §4.5) are re-confirmed individually by a reviewer.
**Verification:** full `pre-pr` green in composition; `SmokeRouteContractTests` still proves 78/78
route parity — the deletion must not reduce route coverage. **No stale TSV registration exists**:
every remaining row names a class that still exists and carries no `[TestGate]`, and every surviving
class is classified by exactly one source — the transition assertion from P3a proves both.
**Rollback:** revert. This is the largest single deletion; it should be one reviewable commit per
area cluster, not one commit for all 152.

### Phase 3b — Final registry cutover

Runs **after** P5, when the estate is final and only surviving classes remain to classify.

**Scope:**
- **Modify** the 68 surviving gate classes to carry `[TestGate(..., Gate.Change, Trigger.X)]`.
  (The consolidated daily classes were already attributed when P4 created them.)
- **Rewrite** `TestGateCatalogTests` → `TestGateAttributeContractTests` — every discovered class
  carries exactly one `[TestGate]`; the TSV is no longer consulted.
- **Modify** `Backend/scripts/test-backend` — delete the TSV fallback branch from the resolver.
- **Delete** `TestSupport/Execution/test-gates.tsv` and its parser.

**Dependencies:** P5. **Safety gate:** the TSV is deleted only after
`TestGateAttributeContractTests` proves 100% attribute coverage of the **final** estate and the
lane-equivalence check passes against the final estate. Deleting the TSV while any class is
unattributed would silently drop that class from every lane.
**Verification:** `daily`, each `gate-*`, and `release` resolve to their intended class sets; the
union of `daily` + all gates equals the full discovered set — no class is orphaned.
**Rollback:** revert restores the TSV and the fallback branch together.

### Phase 6 — Frontend spec deletion (248 files)

**Scope:** delete all 248 `src/**/*.spec.ts`.

**Dependencies:** P2a **and** P2b, both green twice. Abwab and access-admin specs may not be
deleted until Phase 2b is verified — that is the audit's hard prerequisite (L3).
**Safety gate:** a coverage-handoff matrix mapping each of the 75 residue behaviors (audit §5.2) to
the journey that now covers it, or to an explicit accepted-risk row (§13).
**Verification:** `npm run typecheck:app` and `npm run build:verify` green; `npm run e2e` green.
**Rollback:** revert. Deleted specs remain recoverable from git history indefinitely; the audit
report records what each area protected.

### Phase 7 — Obsolete infrastructure removal

**Scope — frontend:**
- Delete `src/test-setup.ts`, `tsconfig.spec.json`, `testing/verify-test-gates.mjs`,
  `testing/README.md` (the whole `testing/` folder).
- `angular.json` — delete the entire `test` target and all ten configurations.
- `package.json` — delete `test`, `test:fast`, `test:feature:*` (6), `test:authorization`,
  `test:composition`, `test:shared`, `test:full`, `test:gates`, `typecheck:spec`. Rewrite
  `typecheck` to `typecheck:app` only. Rewrite `test:pre-pr` to
  `check:permission-catalogue && check:audit-action-types && check:golden-ui && typecheck && build:verify && e2e`.
- `package.json` dependencies and devDependencies — once every `*.spec.ts` and all Angular/Vitest
  test infrastructure is gone, remove **every package used only by the retired unit/component-test
  system**. `vitest` and `jsdom` are the two known certain removals; the phase must *audit* the
  manifest rather than stop at those two, since transitive test-only helpers, matchers, environment
  shims, or type packages may also become dead.

  **Removal rule.** A package is removed only when nothing outside the retired unit-test system
  imports or configures it. A package is **kept** when it is still used by Playwright/E2E,
  production code, the Angular build, `typecheck:app`, linting/formatting, API generation, docs
  generation, or any other active workflow. `@playwright/test` and `typescript` are the obvious
  keeps; `@types/node` is retained if any surviving script or config needs it.

  **Verification for this bullet:** for each candidate, a repository-wide search for its import
  specifier and its config keys returns matches only in files this plan already deleted. After
  removal, a clean install plus `typecheck:app`, `build:verify`, `e2e:typecheck` and `e2e` all pass,
  and no config file references a removed package.

**Scope — repository:**
- **Delete** `TESTING_STRATEGY.md` (652 lines). Its surviving operational truth moves to
  `TESTING_CONSTITUTION.md` (policy), `Backend/tests/QuranDashboard.Tests/README.md` (lanes,
  fixtures, gates), and `e2e/README.md` (journeys, prerequisites, invariants).
- **Modify** `.claude/skills/test-guard/SKILL.md` and `.agents/skills/test-guard/SKILL.md` — scope to
  backend + Playwright; **delete** `references/jest.md` and
  `references/frontend-test-harness-constraints.md`.
- **Modify every remaining live repository file that references `TESTING_STRATEGY.md`** — including
  native instruction files, root guides, READMEs, scripts, configs, and Skills — so each surviving
  policy route points to `TESTING_CONSTITUTION.md` and each surviving operational route points to the
  owning backend-test or E2E README. Planning/history artifacts are removed by their normal lifecycle,
  not treated as live operational references.
- **Modify** `docs/TESTING_DEBT.md` — rows whose tests this plan deletes are removed, not marked
  done; the file's premise is re-stated against the constitution.
- **Add** the L2 enforcement check: a build-level guard failing if any `src/**/*.spec.ts` exists.
  Place it beside the existing `check:golden-ui` / `check:permission-catalogue` scripts, which are
  the established pattern for this.

**Dependencies:** P3b, P5, P6. **Safety gate:** no remaining live instruction, guide, script, config,
README, or Skill references a deleted path, and no remaining package is unit-test-only.
**Verification:** `npm run test:pre-pr` green end-to-end after a clean install;
`rg 'test:full|vitest|jsdom|spec\.ts'` returns only intentional matches; the dependency audit above
shows zero dead unit-test-only packages and zero dangling config references to them; a repository-wide
`rg 'TESTING_STRATEGY\.md'` returns only planning/history artifacts scheduled for lifecycle removal.
**Rollback:** revert. No compatibility shim is left behind — that is deliberate.

### Phase 8 — Final verification and amplification proof

**Scope:** execute §8's verification model once end to end, then run the §11 amplification probes.

**Dependencies:** all. **Safety gate:** §9 acceptance criteria all met.
**Verification:** §8. **Rollback:** n/a.

---

## 8. Final verification model

Verification is **scoped to what the change actually touches**. Running backend database lanes for a
CSS edit, or booting a browser for a backend importer change, is exactly the waste this plan exists
to remove. Scope is computed from the cumulative branch diff.

| Change scope | Frontend verification | Backend verification |
|---|---|---|
| **Frontend only** (`Frontend/**`, no generated client change) | `typecheck:app` + `build:verify`; the relevant Playwright journeys when user-facing behavior changed; browser/visual verification when the change is UI-visible | **none** |
| **Backend only** (`Backend/**`, no contract change) | **none** | `test-backend daily`; plus any `gate-*` lane whose owned concern the diff touches |
| **Shared API / contract** (OpenAPI, generated client, `ApiResponse<T>`, permission codes, route surface) | `typecheck:app` + `build:verify` + the journeys touching the affected surface | `test-backend daily` (which includes `SmokeRouteContractTests`) + any triggered gate |
| **Both stacks** | the frontend column | the backend column |
| **Documentation only** | **none** | **none** |
| **Documentation that is executable testing configuration** (`TESTING_CONSTITUTION.md` is policy and is exempt; a README that documents a lane, gate mapping, or script contract is not) | re-run whatever the changed configuration selects | same |

Concretely:

```
# frontend-only change
npm run typecheck:app        #  8.4 s
npm run build:verify         # 29.6 s
npm run e2e                  # only when user-facing behavior changed

# backend-only change
Backend/scripts/test-backend daily --build     # business rule + security
```

**Gates keep their own ownership regardless of the table above.** A `gate-*` lane fires when its
owned concern changes, computed from the same diff and **fail-closed to `release`** for any
unmapped backend path (§4.4). A gate is never skipped because the change "looked frontend-only" —
scope routing selects which side runs, it never suppresses a triggered gate.

**Before release:** `Backend/scripts/test-backend release` — every gate plus the canonical tier.
This is mandatory and is not satisfied by any number of daily runs.

---

## 9. Acceptance criteria

The plan is complete when **all** of these hold:

1. `TESTING_CONSTITUTION.md` exists, is ≤ 120 lines, and contains all ten rules in §4.1.
2. All six agent instruction files route to it in one sentence and restate none of it.
3. `find src -name '*.spec.ts'` returns **0**; a build-level check fails if one is added.
4. `vitest` and `jsdom` are absent from `package.json`; `angular.json` has no `test` target.
5. `TESTING_STRATEGY.md`, `test-gates.tsv`, `testing/`, `tsconfig.spec.json`, and `src/test-setup.ts`
   do not exist.
6. The backend daily lane is green twice consecutively and is **behaviorally** correct — the
   binding test is these four conditions, **not** a class count:
   - every `KEEP — BUSINESS RULE` behavior in audit §4.2 is still covered by a named case;
   - every `KEEP — SECURITY` behavior in audit §4.1 is still covered by a named case;
   - no deleted category of low-value coverage has been reintroduced (no read/paging/filter/sort
     permutations, controller delegation, DTO/mapping, framework behavior, or report-shape
     permutations appear in the daily lane);
   - the daily lane is intentionally minimal — every class in it is justified by rule 5 of the
     constitution, and a reviewer can state which risk each one protects.

   §4.3's ~17 classes are the planned grouping and the expected outcome. A final count that is
   smaller, or slightly larger, is acceptable when review shows it is a safer or more cohesive
   split; a count that matches 17 while failing any of the four conditions is not.
7. All **68** gate classes still exist, each carries a `[TestGate]` with a `Trigger`, and
   `release` executes every one of them.
8. `TestGateAttributeContractTests` proves every discovered test class is classified, `test-gates.tsv`
   and the resolver's TSV fallback branch are both gone, and no lane reads a TSV.
9. `SmokeRouteContractTests` still proves bidirectional parity for all 78 catalogued routes, and the
   Abwab write matrix is **derived** from the catalog, not a second literal list.
10. All **8** Playwright journeys exist, pass twice consecutively, and each has a recorded
    non-vacuity check.
11. The coverage-handoff matrix accounts for all **75** residue behaviors — covered or explicitly
    accepted.
12. Every §11 amplification probe meets its target.

---

## 10. Expected final estate and reductions

| | Before | After | Δ |
|---|---:|---:|---:|
| Frontend `*.spec.ts` | 248 files / 3,371 cases / 60,247 LOC | **0** | −100% |
| Playwright flow files | 17 | **22** | +5 |
| Backend daily classes | 268 (all ran daily) | **~17** | ~−93.7% |
| Backend daily cases | 2,258 | **~445** | ~−80.3% |
| Backend gate classes | 0 (none triggered) | **68** (438 cases, triggered) | — |
| **Repo units running daily** | **516** | **39** (17 backend + 22 journeys) | **−92.4%** |
| **Test LOC** | 105,658 (incl. E2E) | **~24,600** | **−76.7%** |
| Agent context weight | ~1.23 M tokens | **~0.33 M tokens** | ~−73% |
| Frontend gate wall time | 389.9 s | **38.0 s** | −90.3% |
| Backend daily wall time | 429.6 s | **~60–90 s** (est.) | ~−80% |
| Canonical tier in daily loop | 371.1 s, always | **0 s** (triggered/release) | −100% |

**Every row in this table is an expected outcome, not a correctness requirement.** Correctness is
§9. Backend daily class and case counts move with whatever grouping review settles on (§9 criterion
6); backend daily wall time is not directly measurable until the daily/gate split exists. Phase 8
records the real figures.

---

## 11. Change-amplification proof criteria

This is the plan's primary design goal, so it gets explicit, falsifiable probes. Each replays the
*shape* of a real historical change measured in the audit.

| # | Probe | Historical baseline | Target after |
|---|---|---|---|
| CA1 | Rename a component-private BEM class and move a heading into a shared shell, changing no behavior. | `ac110949`: `abwab-page.component.spec.ts` +6/−2 with **zero** production files changed in that directory. | **0** test-file edits. |
| CA2 | Swap a guard's collaborator (`window.confirm` → a page method) without changing observable behavior. | `332d4b8c`: production +1/−2, spec +369/−25 — **131:1**. | **0** test-file edits. |
| CA3 | Add a read-only endpoint with ordinary paging/filter/sort. | Historically: a new per-feature read class plus catalog plus matrix. | **1** file edited (`SmokeRouteCatalog`), **0** new test classes. |
| CA4 | Add one business rule to an existing domain. | Historically: a new class plus a TSV row. | **1** test method in the owning KEEP class, **0** new files, **0** registry edits. |
| CA5 | Add an authorization requirement to an existing route. | `b9acdd45`: 12 production files/108 lines vs 21 test files/1,695 lines — **15.7:1**. | **≤ 2:1** test-to-production line ratio. |
| CA6 | Add a new backend test class. | 1 class file + 1 `test-gates.tsv` row (12 commits of churn on that file). | **1** file — the attribute is class-local. |
| CA7 | Run a design-system-wide visual convergence like Golden UI Plan 7. | 96 spec files, +6,745/−643 lines. | **0** unit-spec edits; journeys change only if user-visible behavior changed. |

CA1, CA2 and CA7 are the ones that matter most: they are the failure mode that produced this audit.

---

## 12. Explicit non-goals

- No coverage percentage is measured, targeted, or reported — before or after.
- No speculative abstraction is created to preserve retired testing infrastructure. Dead
  compatibility layers are deleted, not shimmed.
- The `inotify` exhaustion that makes the current full suite unreliable (audit §3.3) is **not**
  fixed by this plan. Reducing host-booting classes relieves it; if it persists after Phase 5,
  raising `fs.inotify.max_user_instances` is a separate environment decision.
- No CI system is introduced. The repository remains CI-free (`TESTING_STRATEGY.md` §8's one
  surviving fact, which moves to the constitution).
- Frontend visual/browser verification protocol is not redesigned; it already exists in
  `07-frontend-ui-architecture-v2.md`.

---

## 13. Accepted residual risk

Locked decision L1 deletes all 248 specs, including the 70 pure-logic specs the audit recommended
retaining (audit §5.5). Recording what that costs, so it is a decision and not an accident:

- **Well covered by journeys:** every URL contract (reader, explorers, overlay frame grammar),
  cache/no-refetch semantics, keyboard models, focus and scroll-lock behavior, Uthmani slicing and
  highlight integrity. These are journey-visible and land in journeys 1–5.
- **Weakly reachable by journeys:** pure table-driven mappings with no distinct UI surface —
  Buckwalter↔Arabic mapping, ligature selection, morphology display labels, verse-key formatting,
  surah/juz catalog invariants. A journey exercises the common path but not every row.
- **Disposition:** accepted. These are stable, rarely-changed pure functions whose failure is
  visually obvious in the reader. If the owner later judges the risk too high, the remedy is the
  approval path the constitution already defines — a small number of named pure-logic unit tests,
  approved explicitly — not a return to component specs.

---

## 14. Execution recommendation

**Most of this plan is straightforward enough for direct phased execution.** Phases 0, 2a, 6, 7 and
8 are mechanical or well-bounded: they have clear file sets, obvious verification, and cheap
rollback. A single implementer can execute them sequentially.

**Two phases carry materially different risk and would benefit from more structure:**

- **Phase 1 (authenticated E2E capability)** is the only genuinely novel design in the plan, it
  touches production authentication configuration, and getting it wrong means either a broken E2E
  suite or — much worse — a weakened auth boundary. It should be executed as a **standalone spike
  with its own review**, not as step 2 of a long sequence. Its guard test is the gate.
- **Phases 4 and 5 (consolidate 48 → ~17, then delete the DELETE/MERGE/absorbed classes)** are high-volume and repetitive
  across 20 backend areas, and the failure mode is silent — a behavior dropped during consolidation
  produces a green suite. These benefit from **parallel per-area execution with an independent
  verification pass per area**, because the work decomposes cleanly by area and the checking is the
  expensive part. Executing them as one long serial edit is where a behavior would quietly go
  missing.

So: **direct phased execution, with Phase 1 run as a reviewed spike and Phases 4–5 fanned out per
area with independent per-area verification.** The remainder does not justify additional
orchestration machinery.

---

## 15. Stop conditions

Stop and report before proceeding if any of these occur:

- A gate class cannot be mapped to a trigger without ambiguity — do not guess; a mis-mapped gate is
  silently lost protection.
- Phase 4's behavior-preservation matrix cannot account for a source case.
- Phase 1's guard test cannot be made to fail before the gating logic exists (it would be vacuous).
- Any journey cannot be made to fail against a deliberately broken build.
- The `release` lane's runtime exceeds what the owner will actually run before a deploy — a gate
  nobody runs is deleted protection wearing a different name.
