# Testing Strategy V2 — Risk-Based, Focused Verification Implementation Plan

Either Claude or Sol/Codex can execute this plan directly and sequentially. It requires no
delegation, external Skill, or new execution framework.

**Goal:** Run the right verification at the right risk boundary, once when possible, while keeping
every current security, migration, Quran-data, route, transaction, and test-runtime protection.

**Architecture:** `TESTING_STRATEGY.md` remains the sole owner of test selection and evidence
policy. It will classify the existing commands into four operational boundaries—focused local,
protected trigger, final cumulative-diff, and release/canonical—without deleting, merging, or moving
any current protection-bearing trigger. The implementation/change workflow produces evidence;
engineering review classifies it, PR context packages it, and Git/Test Guard/deploy workflows neither
create nor demand it.

**Mechanisms:** A smaller boundary model, an exhaustive cumulative-final-diff trigger algorithm,
same-state evidence reuse, one scheduled home for `check-api-contract`, exact command/documentation
repairs, and a temporary dated observation log for the measurements Sol requires before any later
cadence reduction. No production code, test code, test selection catalog, schema, dataset, CI, Skill,
router, or Spec Kit behavior changes.

## Global constraints

- Freeze Routing V2 and Skills V2: no router, canonical/adapter Skill, metadata, ownership, review
  cadence, or exact testing-heading pointer change. Preserve the current numbered strategy headings
  and their semantics; `TESTING_STRATEGY.md` remains the only selection policy.
- Keep every lane, command, catalog/configuration, timeout/fork cap, build rule, and fail/skip behavior.
  Classification is not deletion/consolidation evidence.
- Preserve conservative freshness after code/test/config/generated/migration/source/canonical changes.
  Final selection uses the entire base-to-worktree diff, including generated and in-scope untracked
  files—never only the latest fix.
- Keep counts, pass totals, durations, and saving estimates out of standing policy/README prose; only
  dated observations may contain them.
- Preserve local/no-CI honesty and visibility of missing, failed, skipped, preflight, shard, cleanup,
  environment, and unknown evidence.
- Use only §14's static/table-top checks for this docs-only implementation. No broad lane, commit,
  push, PR, review, deploy, migration apply, or dump regeneration without separate authority.

---

## 1. Current testing-workflow problems

1. The strategy still assigns execution to review/pre-PR workflows, while Skills V2 makes engineering
   review a classifier, PR context a packager, and Git/Test Guard/deploy workflows evidence-agnostic.
2. Its stage-shaped matrix makes one broad result look newly required at several successive labels,
   even though the lanes themselves are already risk-scoped and unchanged-state reuse already exists.
3. Focused fixes should not trigger a broad run apiece, but earlier broad evidence cannot be called
   fresh after code changes; one new cumulative-diff final run is needed after the fix set settles.
4. Report 10's last-fix proposal omitted shared Pipeline/Quran persistence and shared test/runtime
   triggers and incorrectly relied on release as the backstop; Sol rejected that escape path.
5. `check-api-contract` has no authoritative schedule despite a stale-contract incident, and its real
   trigger is the full exporter/generator input closure, not a `Backend/api/` path test.
6. Frontend policy omits `test:feature:access-admin` and names exact-spec feedback without a safe
   command or a local-feedback-versus-final-gate distinction.
7. Backend `pre-pr` has protection-bearing shared-runtime/full-regression uses; its stage-shaped name
   must not auto-trigger it for every PR.
8. Invocation/fix frequency, canonical/dump/contract/pre-PR/focused-Frontend cost, E2E stability, and
   variance remain incomplete, so assumed savings cannot become policy.

## 2. Evidence disposition: what can change now

### A. Safe policy simplifications supported now

- Add the four-boundary ownership model without reducing the current trigger matrix; focused work stays
  focused, and cumulative-final-diff mapping is a non-subtractive safety backstop.
- Run each selected broad/composite gate once on the final state and reuse it across non-executing
  stages while tree/environment are unchanged.
- After review findings, run focused/protected fix evidence; after the fix set settles, recompute the
  whole-diff union and run the new final boundary once.
- Schedule `check-api-contract` for every exporter/generator-visible change and document safe exact
  Frontend spec feedback through `npm test`, retaining named lanes as final evidence.
- Repair only direct obsolete cadence wording; preserve every command and protected trigger.

The boundary model is an ownership/selection overlay, not a cadence reduction. The only newly
scheduled gate supported now is `check-api-contract`; every reduction discussed in §2B awaits a later
measured plan.

### B. Changes that require measurement before implementation

- No scope-aware freshness across later code/test/config/generated/source changes before the §11
  observation window and cumulative-diff proof.
- No migration-to-dump relaxation or reduction of Backend `pre-pr`'s shared-runtime,
  always-build/two-shard protection before cost and replacement evidence.
- No generated-Frontend-only omission of `test:full`/`test:pre-pr`; focused consumer cost and missed
  runtime risk are unmeasured, and no generic `test:contract` lane exists.
- No lane merge/rename/delete based on overlap, and no required E2E promotion before current stability,
  auth/bootstrap, runtime, and flakiness evidence.
- No CI/hosted-canonical/automatic-telemetry or percentage/runtime target; those require later owner
  decisions.

### C. Existing protections that remain unchanged

- single-process PostgreSQL ownership, versions 16/18 separation, sequential shards, run-ID cleanup,
  timeouts, visible output, and external-runtime refusal;
- canonical fail-not-skip preflight and separate canonical/Smoke-data-tier reporting;
- migration upgrade/collision/refusal/pending-model/dump-head/same-change regeneration safety;
- auth/access/Owner/permission/identity and route/middleware/binding/serialization Smoke plus
  bidirectional route-catalog parity;
- Quran source/hash/manifest/provenance/refusal/rollback/Pipeline/persistence and audit/transaction/
  `xmin`/conflict protection; and
- Frontend auth/parity/typecheck/build/gate-partition/jsdom/browser boundaries, explicit adverse
  evidence, and the local no-CI evidence model.

## 3. Target Testing Strategy V2 model

Use four operational boundaries. `MEASUREMENT_REQUIRED` and `REDUNDANT_OR_MISPLACED` are adjudication
labels, not two extra execution boundaries.

| Boundary | Purpose | Owner | Timing |
|---|---|---|---|
| `FOCUSED` | Fastest meaningful feedback for the actual unit, feature, or fix. It may be an exact local selector or a named narrow lane. | Implementer of the change. | During each implementation task and focused fix; no broad promotion merely because a phase ended. |
| `PROTECTED_TRIGGER` | A named guard whose risk requires stronger evidence: access/security, route composition, schema, process boundary, importer/Quran persistence, canonical data, shared test runtime, parity, or exporter visibility. | Implementation/change workflow, selected by this strategy. | Once the protected slice is coherent; repeat on the final state if a later relevant change invalidated it. Never once per edit by default. |
| `FINAL_BOUNDARY` | The broad/composite safety net selected from the union of every trigger in the cumulative final diff. | Implementation/change orchestrator, never a review/Git/PR/deploy Skill. | After the feature/change and any review fixes are complete, against the final state. Run each selected broad composite once. |
| `RELEASE_ONLY` | The existing release acceptance composition. | Explicit release workflow. | Only for an authorized release/hotfix boundary; it is not a deferred substitute for missing feature/PR evidence. |

`FINAL_BOUNDARY` does not cancel a current milestone/review/pre-PR/protected requirement. It prevents
last-fix-only selection and duplicate reruns when the already-required result and environment are
unchanged.

Target flow:

```text
implementation task
  -> focused relevant verification
  -> continue implementation/tasks
  -> protected-trigger verification when a real trigger fires
  -> feature/change complete
  -> map the cumulative final diff to every trigger
  -> run each selected final broad/composite gate once
  -> engineering review classifies and PR context packages that evidence
  -> commit/Test Guard/deploy stages neither run nor demand it
  -> focused/protected verification for any findings fixes
  -> recompute from the cumulative final diff and run the new final gate union once
```

The second final run exists only when review fixes changed the code state. No policy can honestly call
the first run fresh for the new state. The saving comes from not rerunning broad gates after every
individual fix or at every later workflow label.

## 4. Exact boundary rules

- **Focused:** Backend uses `feature --test`, `feature --class`, `feature FEATURE_KEY`, or `fast`; build
  once when compilation changes, then use `--no-build` against that output. Frontend exact-spec feedback
  uses `npm test -- --watch=false --include=src/app/.../name.spec.ts`, followed by the affected named
  lane when required. It is not final gate evidence, and bare `ng test`/`npx ng test` remains forbidden.
- **Protected:** run the applicable named gate after the protected slice is coherent, not after every
  edit. Security/access, routes, schema, canonical/Quran persistence, and shared test/runtime work never
  become focused-only. A later relevant change invalidates the old protected result for final closure.
- **Final:** establish the full change set and take the §7 union. Do not run a standalone Frontend
  `typecheck`, `build:verify`, or `test:full` immediately before unchanged `test:pre-pr`. Do not compose
  Backend `pre-pr` from overlapping lanes or treat it as a replacement for specifically required Smoke,
  migration/pending-model, dump-regeneration, or API-contract evidence.
- **Release:** preserve full Backend `pre-pr` + `canonical-data` + Frontend `test:pre-pr`, staged
  resources, and complete skip/shard accounting. Release never retroactively repairs missing feature
  evidence, and browser E2E remains supplementary.

A broad later run never excuses missing focused coverage. Current milestone/review/pre-PR requirements
remain, but an unchanged result is reused rather than rerun merely because the workflow label changed.

## 5. Backend gate classification

Each named gate receives one primary V2 class. Release may compose a gate whose primary class is
protected/final without changing that class. No row authorizes a lane deletion.

| Existing gate/check | Class | V2 boundary and reason |
|---|---|---|
| `fast` | `FOCUSED` | Small logic feedback; keep `Kind=Fast` container-free and catalog-enforced. |
| `feature FEATURE_KEY`, `--class`, `--test` | `FOCUSED` | Exact implementation/feature feedback; unknown or undiscovered selectors continue to fail. |
| `access` | `PROTECTED_TRIGGER` | Full Access/auth/Owner/direct-permission protection after a coherent access slice; exact Access cases remain the local focused loop. |
| `access-db` | `PROTECTED_TRIGGER` | Access persistence, constraints, EF model, catalogue, grants, transactions, and audit storage; it does not replace migration/process gates. |
| `migration` | `PROTECTED_TRIGGER` | Migration/backfill/collision/refusal/schema safety; retain the real staged upgrade boundary. |
| `check-pending-model` | `PROTECTED_TRIGGER` | Separate non-mutating EF-model check whenever schema/model is in scope. |
| `process` | `PROTECTED_TRIGGER` | Real AccessAdmin wrapper/operator boundary; the current wrapper union remains `process` + `access` + `smoke` + final `tier-b`. |
| `smoke` | `PROTECTED_TRIGGER` | Mandatory real API composition for route, request/response contract, auth, middleware, binding, serialization, startup, DI, or shared `DbContext` composition; preserve route-catalog parity and data-tier separation. |
| `pipeline [--feature]` | `PROTECTED_TRIGGER` | Named feature for isolated importer work; full affected selection for shared Pipeline/Quran persistence. It never silently absorbs canonical classes. |
| `canonical-data` | `PROTECTED_TRIGGER` | Canonical source/manifest/hash/dump and shared Quran persistence protection. Canonical-source acceptance still also requires Pipeline, Smoke, and full Backend `pre-pr`; release participation never defers that feature boundary. |
| `tier-b` | `FINAL_BOUNDARY` | Ordinary broad no-Pipeline Backend safety net. One unchanged-state run may satisfy the current milestone/review/pre-PR requirements; this plan does not remove those requirements or widen it with unrelated Pipeline/canonical work. |
| `pre-pr` | `PROTECTED_TRIGGER` | Preserve the full always-build/two-shard gate for shared test/runtime infrastructure, canonical-source/full-regression acceptance, and release. Its name alone does not create an extra unchanged-state run. |
| `create-smoke-dump --yes` | `PROTECTED_TRIGGER` | Same-change migration-head protection remains mandatory. Any cadence relaxation is `MEASUREMENT_REQUIRED`. |
| `check-api-contract` | `PROTECTED_TRIGGER` | Run after the exporter/generator-visible surface is complete; it complements rather than replaces route Smoke, focused consumer coverage, type-check, or final Frontend verification. |

The Backend solution build is a prerequisite, not an extra test class: compilation-affecting changes
still build once before `--no-build` lanes, and the final evidence must be against current output.
`cleanup-test-runtime` remains runner mechanics, not a selectable gate; its ownership and zero-leftover
reporting remain mandatory.

No Backend lane is classified `REDUNDANT_OR_MISPLACED`. What is misplaced is invoking a gate solely
because a new workflow label began. Access/Tier B and cross-axis overlap require measurement before
any consolidation claim.

## 6. Frontend gate classification

| Existing gate/check | Class | V2 boundary and reason |
|---|---|---|
| Exact spec through `npm test -- --watch=false --include=...` | `FOCUSED` | Safe local feedback through the owned fork cap/timeout; not named final evidence. |
| `test:fast` | `FOCUSED` | Pure model/utility/data/state/cache/URL/helper feedback. Do not put TestBed/component specs in it. |
| `test:feature:access-admin`, `:abwab`, `:auth`, `:dashboard`, `:mushaf`, `:words` | `FOCUSED` | Coherent feature-slice verification. V2 must list all six existing feature commands. |
| `test:shared` | `FOCUSED` | App shell/core/shared/environment/routing slice; never substitutes for an affected feature/cross-cut. |
| `test:authorization` | `PROTECTED_TRIGGER` | Frontend auth configuration, token/session handling, secure-origin/interceptor, route posture, or auth-generated contract changes. |
| `test:composition` | `PROTECTED_TRIGGER` | Shared component harness, Angular rendering, directive, overlay, or composition changes. It remains jsdom, never browser evidence. |
| `test:gates` | `PROTECTED_TRIGGER` | Spec add/move/rename/delete or `angular.json` include/configuration change; preserves partition and non-vacuity checks. |
| `check:permission-catalogue` | `PROTECTED_TRIGGER` | Cross-stack permission catalogue/generated-code parity. |
| `check:audit-action-types` | `PROTECTED_TRIGGER` | Backend/Frontend audit-action type parity. |
| `typecheck:app`, `typecheck:spec` | `FOCUSED` | Individual compilation feedback when only one TypeScript project is implicated. |
| `typecheck` | `PROTECTED_TRIGGER` | App+spec strictness for generated DTO, config, routing, template, or cross-project type changes; also a leg of the final composite. |
| `build:verify` | `FINAL_BOUNDARY` | Production template/bundle/configuration build; run standalone only when the final composite is not selected. |
| `test:full` / `npm test` | `FINAL_BOUNDARY` | Full jsdom suite when a standalone broad Frontend gate is selected. Do not run it immediately before unchanged `test:pre-pr`. |
| `test:pre-pr` | `FINAL_BOUNDARY` | Keep the permission parity → audit parity → typecheck → production build → full-test composite and current review/pre-PR/release requirements; one final unchanged-state run satisfies those labels. |
| `e2e:typecheck` | `FOCUSED` | Type-check E2E code when E2E files/config/fixtures change; it does not promote browser E2E to required status. |
| Browser `e2e` | `MEASUREMENT_REQUIRED` | Keep opt-in supplementary browser truth. Do not promote until current auth readiness, runtime, repeat stability, residue, and flakiness are measured. |

No current required Frontend gate is `RELEASE_ONLY`; release reuses the final `test:pre-pr`
composition. No named lane is `REDUNDANT_OR_MISPLACED`; only duplicate standalone invocations of a
composite's unchanged legs are.

There is no generic `test:contract` lane. For a generated model change, select exact consumer specs
and the affected feature lane; add `test:authorization` only when auth/session/security models are in
scope; retain `typecheck` and the current final `test:pre-pr` until measurement supports a narrower
policy. Do not invent a lane name in prose.

## 7. Cumulative-final-diff trigger algorithm

Run this algorithm whenever the implementation/change workflow believes the feature/change or a set
of review fixes is complete.

1. **Fix the comparison base.** Use the feature/change base (normally its merge base with `dev`) and
   the complete current worktree. Include committed, staged, unstaged, generated, and in-scope
   untracked files. A last commit, last finding, or last fix is not the comparison base.
2. **Classify semantics, not extensions alone.** For every changed path and behavior, test it against
   all of these trigger families:
   - Backend logic/feature and compilation output;
   - authentication, authorization, identity, Owner, direct permissions, access status, and audit;
   - API routes, endpoint metadata, middleware, binding, request/response contracts, serialization,
     startup, DI, configuration, and shared API/`DbContext` composition;
   - OpenAPI exporter/generator-visible inputs from §10;
   - migrations, schema, EF model/snapshot, backfill, collision/refusal, and process/operator wrapper;
   - importer/pipeline code, manifests, source handling, reachable persistence, shared Pipeline, Quran
     persistence, canonical sources/artifacts/hash/dump, and transaction boundaries;
   - Backend test catalog/resource rows, fixtures, collections, PostgreSQL runtime/locks/shards,
     runner/build/output/cleanup logic, and any other shared test infrastructure;
   - Frontend utility/state, feature, shared/core/shell, auth/security, component/composition, routing,
     templates, environment/configuration, generated DTOs, production bundle, test setup/configuration,
     and spec layout; and
   - browser-only geometry, focus, history/navigation, RTL input, or real-network behavior, while
     retaining E2E's current supplementary status.
3. **Union; never subtract.** Add every focused, protected, final, and release-only requirement that
   any cumulative change triggers. A later fix can add a gate; it cannot erase a gate triggered by an
   earlier part of the still-present diff.
4. **Apply the authoritative protected mappings.** At minimum:
   - Access/security selects the affected Access/Frontend authorization gates and Smoke whenever the
     API route/contract/auth pipeline is implicated;
   - route/middleware/binding/serialization selects focused API-family evidence plus `smoke` and
     same-change route-catalog parity;
   - schema/model selects the affected DB/Access feature, `migration`, `check-pending-model`,
     same-change dump regeneration, `smoke`, and final `tier-b`; add `process` when the operator wrapper
     also changed;
   - an AccessAdmin wrapper/operator change selects `process` + `access` + `smoke` + final `tier-b`;
   - importer/shared Pipeline/Quran persistence selects the affected Pipeline scope, full Pipeline
     where shared, and `canonical-data` for canonical source/manifest/hash/dump, Quran schema or
     persistence, and shared-Pipeline triggers;
   - a canonical source/manifest/hash/dump change selects full `pipeline` + `canonical-data` +
     `smoke` + full Backend `pre-pr` for canonical acceptance; this happens at the feature/change
     boundary and is not deferred to release;
   - shared Backend test/runtime infrastructure selects its exact contract/pilot checks and the
     current full Backend `pre-pr` protection;
   - Frontend test/config/spec-layout changes select `test:gates`; auth and composition select their
     cross-cuts; and
   - exporter/generator-visible changes select `check-api-contract`, generated-output review, affected
     Frontend consumer verification, and `typecheck`.
5. **Add the final broad boundary.** Add `tier-b` for an ordinary Backend final diff; use full Backend
   `pre-pr` for shared test/runtime infrastructure, canonical-source acceptance, or another current
   explicit full-regression/release trigger. Add `test:pre-pr` for a Frontend source/spec/config/
   generated diff under current policy. Documentation-only changes do not gain product gates merely
   because a file changed.
6. **Collapse only exact composite duplication.** When `test:pre-pr` is selected for the same final
   state, do not separately rerun its unchanged parity/typecheck/build/full-test legs immediately
   before it. Do not infer equivalent collapse across overlapping Backend selectors; their current
   named protection remains.
7. **Execute against the final state and record reality.** Report exact command, selection reason,
   pass/fail/skip/unknown outcome, Backend shards and canonical-tier status where applicable, and
   cleanup/environment state. A missing resource, failed preflight, failed shard, unexpected skip, or
   unknown result cannot close the gate.
8. **Recompute after any fix set.** Focused verification may happen per fix. After the last fix,
   restart at step 1 and derive final verification from the whole remaining diff, never from the fix
   list.

When a change cannot be mapped confidently, preserve the stronger current trigger and stop for an
owner decision; do not interpret ambiguity as permission to run less.

## 8. Evidence freshness and reuse

Evidence records must name the command/lane, reason, code state/change scope, result, skips, and any
required shard/canonical/cleanup status. They are consumed as follows:

- **Focused evidence** proves the local task/fix only. It does not claim the final cumulative gate.
- **Protected/final evidence** may be classified by engineering review, packaged by PR context, and
  reused at a later required boundary only while tree/environment remain unchanged. Commit/Test Guard/
  deploy workflows stay evidence-agnostic. A new workflow label does not stale the result.
- A later change to code, tests, configuration, generated API artifacts, migration/model state,
  canonical sources/artifacts/dump, the runner/catalog/harness, or another dependency covered by the
  gate makes final evidence stale. Run focused verification while fixes are in motion, then recompute
  and run the final union once after they settle.
- A packaging-only action or unrelated prose edit does not create a test requirement. If prose changes
  a contract or test policy, verify that document with the static policy probes instead of inventing a
  product test lane.
- A relevant environment change—toolchain/dependency state, staged canonical resources, migration
  head, database runtime/major, or test infrastructure—also invalidates evidence that depended on it.
- Failed, unexpectedly skipped, preflight-failed, incomplete-shard, cleanup-failed, and unknown
  evidence remains exactly that. Never narrow a selection or relabel a runtime curl to manufacture a
  pass.
- There is no CI evidence. Only observed local output can be cited.

This plan deliberately does not implement lane-scoped freshness across later code changes. That
optimization stays `MEASUREMENT_REQUIRED`.

## 9. Workflow ownership and stages that must not rerun tests

| Workflow owner | Owns | Must not do |
|---|---|---|
| Implementation/change workflow | Focused verification for its tasks/fixes; protected gates when triggered; cumulative-diff mapping and final-gate execution. | Substitute a broad unrelated lane for focused coverage, or run broad gates at every phase. |
| `TESTING_STRATEGY.md` | Command vocabulary, classification, risk triggers, final-diff algorithm, freshness/reuse, and failure/skip evidence semantics. | Delegate policy to a Skill/README or silently weaken a protected trigger. |
| `engineering-review` | Consume supplied same-diff evidence, compare it with this strategy, and report sufficient/stale/missing/failed evidence in its verdict. | Run builds/tests, invoke Test Guard, or recreate evidence. |
| `commit-workflow` | Git-integrity checks and the requested Git action. | Run or demand builds/tests/review evidence. |
| `pr-context-prep` | Package existing scope and evidence; label gaps honestly. | Rerun tests, generate evidence, or adjudicate readiness independently. |
| `test-guard` | Test-code quality guidance/review. | Select lanes, run tests, or decide executed-evidence sufficiency. |
| `deploy-smoke` | Explicit deployment preflight/runtime observations and owned process lifecycle. | Run test lanes or present curls as route-Smoke/canonical evidence. |
| Any other Skill | Its one implemented V2 responsibility. | Silently add build/test evidence or create an implicit gate. |

If a non-executing stage discovers missing/stale evidence, it reports the gap and returns execution to
the implementation/change workflow. It does not close the gap itself.

## 10. `check-api-contract` trigger and ownership

`Backend/scripts/check-api-contract` is Backend-owned mechanics with cross-area outputs. The
implementation/change workflow runs it after the exporter/generator-visible surface is complete and
before final evidence is packaged. The trigger is semantic and includes at least:

- controller/action routes, verbs, binding, parameters, result types, status/response metadata, and
  endpoint documentation read by the exporter;
- request/response DTO graphs wherever they live, including API-local contracts, Application
  Abstractions responses, shared paging/envelope types, nullability, enums, and inherited/polymorphic
  schemas;
- serialization names/ignore/converter/polymorphism metadata and any schema-shaping attribute;
- Swagger/OpenAPI registration, document/version/security/schema filters, JSON-schema mappings,
  exporter startup/configuration, and API/Swashbuckle tooling/package changes;
- Frontend OpenAPI generator configuration, pruning/generation scripts, and committed generated-model
  rules; and
- any other change that alters what `export-swagger`, Swashbuckle, `ng-openapi-gen`, or the pruning
  step reads, even if the path is outside `Backend/api/`.

The command continues to Release-build/export offline with permission-catalogue startup sync disabled,
regenerate committed Swagger/models, and fail on diff. It does not replace:

- `smoke` for real route/auth/binding/serialization composition;
- focused Backend controller/contract tests;
- focused Frontend consumer/auth tests;
- Frontend `typecheck`/build/final verification; or
- human review of deliberate compatibility changes.

Its end-to-end cost is `MEASUREMENT_REQUIRED`, but its scheduling is safe now because the status quo
has already allowed stale committed contract output.

## 11. Measurement protocol and blocked cadence decisions

Create a temporary header-only observation file at
`docs/project-simplification-audit/data/testing-cadence-observations.tsv`. It is audit evidence, not
policy and not a required gate. Its exact columns are:

```text
observed_at_utc	change_id	boundary	diff_ref	command	classification	trigger_reason	result	wall_seconds	fix_iteration	canonical_tier	shard_cleanup_status	evidence_ref	notes
```

Do not seed example or estimated rows. During the next two or three real features, append a row only
from observed output. Record repeated invocations as separate rows and state why they repeated. Do
not make a feature fail merely because temporary audit logging was missed.

The later cadence decision remains blocked until the evidence contains, or explicitly cannot obtain:

1. actual gate-invocation and review-fix frequency across two or three representative features;
2. dated `canonical-data` and Backend `pre-pr` runs with both shards/canonical accounting;
3. authorized `create-smoke-dump` timing—never regenerate a dump merely to time it without explicit
   authority and a safe local source database;
4. end-to-end `check-api-contract` timing and generated-diff behavior;
5. focused Frontend consumer/authorization plus typecheck timing for a generated-contract change;
6. current Frontend `test:pre-pr` component timings rather than a prose estimate;
7. whether the suspected stage/fix duplicate invocations actually occur and why; and
8. E2E runtime/repeat stability/auth readiness only if the owner later considers changing its opt-in
   status.

Single runs have no variance claim. Historical flakiness remains unknown unless evidence exists.
Measurements may inform a later bounded plan; they do not retroactively authorize scoped freshness,
lane deletion, migration/dump relaxation, generated-only full-suite removal, E2E promotion, or CI.

## 12. Exact implementation file set

| File | Exact responsibility |
|---|---|
| `TESTING_STRATEGY.md` | Overlay §§3–10 on the current trigger semantics without reducing cadence: four boundaries, complete lane classification, non-subtractive cumulative-diff algorithm, reuse/failure semantics, corrected ownership, `check-api-contract`, and measurement-blocked decisions. Preserve numbered headings §1, §3.3, §3.4, §5, §6, §8, §9, and §11 so every existing Skill/README pointer retains its semantic target, plus all runner/PostgreSQL/canonical/route/build/no-CI/E2E protections. |
| `Frontend/quran-dashboard-ui/testing/README.md` | Add the safe exact-spec local command through `npm test`, label it non-gate feedback followed by named-lane evidence, list the already-existing `test:feature:access-admin`, and keep all configuration/fork-cap/jsdom/E2E mechanics unchanged. Do not add a package script or configuration. |
| `Backend/api/QuranDashboard.Api/Controllers/README.md` | Keep exporter/generated-artifact mechanics and the stale-contract incident; replace its local cadence sentence with an exact pointer to the V2 `check-api-contract` trigger and clarify that exporter-visible DTO/serialization/Swagger inputs are not confined to controllers. |
| `SKILLS_AND_ARCHITECTURE_GUIDE.md` | Change only the two testing bullets: implementation produces every currently required focused/protected/final result; before PR, missing/stale evidence returns to implementation, while PR prep only packages it. Preserve current gate requirements, review cadence, Skills, routers, and unrelated workflows. |
| `docs/project-simplification-audit/data/testing-cadence-observations.tsv` | Create only the exact header in §11. It is temporary dated audit telemetry, carries no estimates, and is not test policy or required evidence. |

No other file is expected to change. In particular, do not modify:

- `Backend/scripts/test-backend`, `check-api-contract`, `export-swagger`, `check-pending-model`,
  `create-smoke-dump`, or `Backend/scripts/README.md`;
- Backend test code, `test-gates.tsv`, `test-resources.tsv`, PostgreSQL runtime code, or canonical
  gates/resources/fixtures;
- Frontend `package.json`, `angular.json`, TypeScript configs, Vitest/unit specs, Playwright config,
  E2E specs/fixtures, generated API artifacts, or production source;
- any `.claude/skills/`, `.agents/skills/`, Skill sidecar, root/area router, architecture contract,
  Spec Kit file, persistent memory, CI/deployment configuration, database/schema, or historical audit
  report/data file.

## 13. Small sequential implementation steps

### Step 1 — Freeze commands, triggers, and the allowlist

- [ ] Confirm the branch is not `main`, capture one root `git status --short`, and use §12 as the
  cumulative file allowlist.
- [ ] Capture `Backend/scripts/test-backend --help`, the current Frontend `package.json` test scripts,
  the ten `angular.json` configurations, `check-api-contract`/`export-swagger` mechanics, and the
  PostgreSQL/canonical protection text without executing a test gate.
- [ ] Map every current §5 trigger to a row in §§5–7. Stop if a current protection-bearing trigger has
  no unchanged destination.
- [ ] Create `testing-cadence-observations.tsv` with the exact header from §11 and no data rows.

### Step 2 — Rewrite the canonical policy around risk boundaries

- [ ] Keep purpose/authority, anti-count/duration, lane selection/mechanics/commands, current trigger
  semantics, failure rules, and no-CI truth; add the four boundaries and align execution ownership
  without moving a protection-bearing gate.
- [ ] Make every Backend and Frontend command/classification in §§5–6 explicit, including
  `test:feature:access-admin`, exact Frontend local spec execution, `e2e:typecheck`, and the absence of
  a generic Frontend contract lane.
- [ ] Re-express §5 with the non-subtractive cumulative-final-diff algorithm while retaining every
  current gate union, including canonical acceptance and AccessAdmin wrapper unions. Any actual
  cadence/freshness reduction stays blocked on §11.
- [ ] Add §§8–9 ownership: engineering review classifies, PR prep packages, and Git/Test Guard/
  deploy/other Skills neither produce nor demand test evidence.
- [ ] Add the full `check-api-contract` trigger closure from §10 and the `MEASUREMENT_REQUIRED`
  boundaries from §11. Copy no measured runtime/count into standing policy.
- [ ] Preserve the current numbered heading targets used by Skills V2. If the model cannot fit without
  changing a pointer's semantics, stop rather than editing a Skill.

### Step 3 — Repair only the three direct documentation consumers

- [ ] Update the Frontend testing README with the exact local command/non-gate distinction and the
  complete existing feature-lane list; change no runner/configuration mechanics.
- [ ] Update the Controllers README's one contract-check cadence statement to defer to the canonical
  V2 trigger while retaining local exporter/generated-output facts and the stale-contract incident.
- [ ] Update only the implementation and before-PR testing bullets in
  `SKILLS_AND_ARCHITECTURE_GUIDE.md`; do not alter formal-review timing, Skill responsibilities, or
  any router/Spec Kit workflow.
- [ ] Run an inbound wording scan for stage-owned test execution. Repair only a direct contradiction
  in the §12 allowlist; an additional consumer is a stop condition, not authority to widen scope.

### Step 4 — Verify the policy graph, not the product suite

- [ ] Run the static checks and table-top trigger probes in §14 against the same cumulative docs-only
  diff.
- [ ] Confirm every old lane/command still exists exactly once in the canonical inventory, no script
  or catalog changed, and no protected trigger disappeared.
- [ ] Confirm the final diff is limited to §12 plus this approved plan artifact. Do not run Tier B,
  Pipeline, canonical-data, Backend `pre-pr`, Frontend `test:full`/`test:pre-pr`, or E2E for a
  documentation-only strategy change.

## 14. Focused verification of the strategy change

### Static checks

1. `git status --short` plus the tracked and untracked path lists are limited to §12 plus this plan;
   `git diff --check` passes for tracked edits, and
   `git diff --no-index --check -- /dev/null <new-file>` emits no whitespace diagnostics for each
   new file (its exit status `1` only reports that the file differs from `/dev/null`).
2. `Backend/scripts/test-backend --help` still reports `fast`, `feature`, `access`, `access-db`,
   `migration`, `process`, `smoke`, `tier-b`, `pipeline`, `canonical-data`, and `pre-pr`; the policy
   contains each without inventing a command.
3. The Frontend policy/README match the real scripts for all six feature lanes, `fast`,
   `authorization`, `composition`, `shared`, `full`, `gates`, type checks, build, pre-PR, and E2E.
4. The exact-spec example begins with `npm test --`, contains `--watch=false`, and is labeled focused
   local feedback rather than a final gate; no sanctioned example uses bare `ng test` or `npx ng
   test`.
5. A search across current non-historical workflow owners finds no instruction for
   `engineering-review`, `commit-workflow`, `pr-context-prep`, `test-guard`, or `deploy-smoke` to run
   tests, and the strategy does not assign them execution.
6. Every existing exact `TESTING_STRATEGY.md` pointer in Skills/READMEs still lands on the same
   numbered semantic owner—especially §§1, 3.3, 3.4, 5, 6, 8, 9, and 11. A broken pointer is a stop,
   not permission to edit Skills.
7. The strategy contains the cumulative-diff rule and explicitly names auth/access,
   route/middleware/binding/serialization, migration/schema/model, canonical Quran data,
   importer/Pipeline/Quran persistence, shared persistence/transactions, shared test/runtime
   infrastructure, exporter-visible API changes, and Frontend test/config/generated-contract scope.
8. The `check-api-contract` trigger includes controllers, DTO graphs/envelopes, serialization/schema
   metadata, Swagger configuration/tooling, and Frontend generator/pruning inputs; it is not reduced
   to `Backend/api/` path matching and is not called a replacement for Smoke/typecheck/consumer tests.
9. PostgreSQL serialization, shard reporting, canonical preflight/fail-not-skip, migration dump-head,
   route-catalog parity, no-CI, E2E opt-in, failure/skip/unknown, and cleanup rules remain reachable and
   materially unchanged.
10. `testing-cadence-observations.tsv` has exactly the §11 header and zero fabricated data rows.
11. No standing count, duration, percentage-saving target, fabricated measurement, new CI promise, or
    test-deletion/consolidation instruction appears in the changed policy/README prose.

### Table-top cumulative-diff probes

Evaluate each row without running its commands. The expected union is a policy acceptance contract,
not a new executable matrix.

| Final cumulative diff | Required selection result |
|---|---|
| Pure Backend feature logic | Exact/feature or fast feedback during work; affected feature evidence and one final `tier-b`; no Smoke/Pipeline/canonical/Frontend. |
| Access service plus API auth route | Exact Access feedback; `access`, route `smoke`, route-catalog parity, affected API contract tests, `check-api-contract` if exporter-visible, and final `tier-b`; no Pipeline/canonical unless independently triggered. |
| Migration/EF model and Access persistence | Exact/`access-db`, `migration`, `check-pending-model`, current same-change dump regeneration, affected Access, route Smoke, and final `tier-b`; preserve any stronger current trigger instead of inferring a reduction. |
| AccessAdmin wrapper/operator boundary | Exact process feedback, then `process` + `access` + `smoke` + final `tier-b`; do not treat lower-level Access tests as process-boundary evidence. |
| Shared Pipeline or Quran persistence | Representative/named feedback, full affected `pipeline`, canonical-data whenever current shared/Quran trigger applies, required schema/transaction checks, and final `tier-b`; no Frontend without a real generated/frontend diff. |
| Canonical source/manifest/hash/dump | Exact canonical feedback, full `pipeline` + `canonical-data` + `smoke` + full Backend `pre-pr`, with canonical tier/shards/skips explicit; release is not the backstop. |
| Shared Backend test runtime/catalog/shards | Exact contract plus pilot feature, required representative protected lanes, and the full Backend `pre-pr`; PostgreSQL processes remain sequential and canonical tier remains explicit. |
| Frontend feature component | Exact spec local feedback, affected feature and composition when implicated, then one final `test:pre-pr`; no Backend. |
| Frontend auth/config | Exact spec, `test:authorization`, affected feature/shared lane, `typecheck` when implicated, then one final `test:pre-pr`; Backend only if Backend also changed. |
| Exporter-visible DTO/serialization/Swagger change | `check-api-contract`, generated diff review, exact affected consumer tests, affected feature or authorization lane, `typecheck`, route Smoke when API composition changed, and current final Backend/Frontend gates; no invented `test:contract`. |
| Spec layout or Angular test configuration | Affected focused lane plus `test:gates`, then the current final Frontend boundary; no E2E promotion. |
| Review fixes touching only one local behavior | Focused verification per fix; after all fixes, rerun this algorithm against the whole diff and execute the resulting final union once. The latest fix never removes earlier triggers. |
| Browser-only geometry/focus/history behavior | Preserve relevant unit/feature coverage and label a targeted E2E run supplementary if explicitly chosen; do not claim jsdom proves geometry or make E2E mandatory from this plan. |

Any probe that selects from only the latest fix, drops a protection-bearing trigger, assigns execution
to a non-owning Skill, treats release as the feature backstop, or adds an unrelated broad lane fails
Testing Strategy V2.

## 15. Explicit non-goals

- No test/spec/E2E/fixture/helper/data deletion, merge, parameterization, rewrite, relocation, or
  consolidation.
- No lane/catalog/config/package-script/runner/builder/timeout/fork-cap/watch/report/shard/cleanup
  change, and no production/API/architecture/style/persistence/schema/canonical/import/deploy change.
- No Engineering Review cadence/verdict, Skill/adapter/router, Spec Kit, persistent-memory, README
  compression, or agent-orchestration redesign.
- No CI or E2E promotion, required browser matrix, or movement of browser-only assertions into jsdom.
- No count/runtime/percentage/fewer-runs target, and no use of observation logging as gate evidence.

## 16. Stop conditions

Stop and report instead of broadening or weakening the plan when any of the following is true:

1. Branch is `main`, user changes overlap, or the diff cannot stay within §12.
2. A current trigger cannot fit the model without weakening its lane/cadence/failure/evidence, or
   cumulative mapping would use only the latest fix or release as a missing-gate backstop.
3. The change needs any §2B reduction before §11 evidence, or preserving PostgreSQL, canonical,
   migration/dump, Quran, audit/transaction, auth/route, failure visibility, or no-CI truth would be
   compromised.
4. Complete API-contract coverage requires script/production/generator/generated-output changes, or a
   direct contradiction is outside §12. Report it and request a separate scope decision.
5. Measurement would mutate a database/dump, lack staged resources, run Backend processes
   concurrently, or exceed authority. Record unavailable; never fabricate or force it.

Implementation is complete only when the canonical policy selects from the cumulative final diff,
the current high-risk protections remain unchanged, broad evidence has one final execution owner,
engineering review classifies and PR context packages it while other Skills stay evidence-agnostic,
`check-api-contract` has its full trigger closure, and every further cadence reduction is visibly
blocked on real measurement.
