---
name: engineering-review
description: >-
  Review-only engineering/code review for the Quran Dashboard FullStack workspace
  (.NET backend + Angular frontend). Use this skill whenever the user asks to review
  code, a diff, a PR, a branch, or a change; asks whether implementation quality is
  engineering-ready, follows the coding principles, or respects the architecture; or
  shares freshly implemented backend or frontend code for a quality or architecture
  check — even if they don't say the word "review". This skill does not judge Git
  staging, commit ordering, push readiness, or untracked-file risk; those belong to
  commit-workflow. Also use it when the change came from Spec Kit (the request
  mentions a Phase, a User Story/US, task IDs like T013–T018, specs/<feature>/,
  spec.md, plan.md, tasks.md, contracts/, or quickstart.md) and should be checked
  against its spec, scope, and contracts. This is a review-only skill: it produces
  findings and a verdict and does not implement fixes unless the user explicitly asks.
---

# Engineering Review Skill

Use this skill to review code changes in the Quran Dashboard FullStack workspace
(.NET backend + Angular frontend).

## Review-Only Guardrails

This skill is for review only.

- It must **not** implement code changes.
- It must **not** refactor code.
- It must **not** create files unless the user explicitly asks.
- It produces findings, risks, and recommendations only.
- It must review implementation content, including untracked files, when those files
  are part of the requested scope.
- It must not treat Git tracking/staging state as an engineering-quality issue.
- Untracked files must never be Findings, Notes, blockers, severity items, Test Guard
  issues, final verdict inputs, or reasons for `CHANGES REQUESTED`.
- If untracked files are noticed, mention them only in a separate optional section
  named `Commit workflow reminder`; that section is outside the engineering verdict.
- `commit-workflow` owns git status, untracked files, explicit staging, commit
  omission risk, commit planning, and push readiness.

If the user wants fixes after the review, that is a separate, explicitly requested
task.

## Required Context / Reading Rules (path-based)

Read only the docs relevant to what actually changed. Do not require reading tool
entrypoint files (Claude/OpenCode/Codex each load their own); rely on the loaded
context for those.

**Always read:**

- `CODING_PRINCIPLES.md`
- `TESTING_STRATEGY.md` — when judging whether the executed tests were sufficient
  for the changed scope (the Verification Check below).

**For deep code-quality review, also consult the clean-code reference pack** (naming
and functions, comments and formatting, SOLID, DRY/KISS/YAGNI, and AI-generated-code
failure modes). Read the relevant file when a finding touches that principle, or walk
the checklist when doing a thorough quality pass:

- `.claude/skills/engineering-review/references/clean-code-guard/naming-and-functions.md`
- `.claude/skills/engineering-review/references/clean-code-guard/comments-and-formatting.md`
- `.claude/skills/engineering-review/references/clean-code-guard/solid.md`
- `.claude/skills/engineering-review/references/clean-code-guard/dry-kiss-yagni.md`
- `.claude/skills/engineering-review/references/clean-code-guard/ai-failure-modes.md`
- `.claude/skills/engineering-review/references/clean-code-guard/review-checklist.md` — structured walk-through for a deep code-quality review.
- `.claude/skills/engineering-review/references/clean-code-guard/sources.md` — bibliography; read only when a finding is contested and you need the source citation.

This pack **deepens** the General Review Goals below; it does **not** replace any
project-specific check, and it does not make this a second review skill — it is
reference material for this one. Where the generic clean-code guidance conflicts with
project conventions, the project wins (see `CODING_PRINCIPLES.md`: C#/.NET `I`-prefixed
interfaces and the `ApiResponse` API envelope are authoritative).

**If the diff contains new or modified test files, apply `test-guard` as a mandatory
dedicated review gate** (test-code quality only; this skill keeps ownership of everything
else). Do **not** merely read `test-guard` as context or only list it under "Docs read" —
you must apply it as a dedicated **Test Guard Review** subsection (see below) and report
its result. Consult:

- `.claude/skills/test-guard/SKILL.md` — the nine test-code rules.
- `.claude/skills/test-guard/references/dotnet.md` — for backend tests (xUnit,
  `WebApplicationFactory`, EF Core + PostgreSQL via Testcontainers, `ApiResponse`).
- `.claude/skills/test-guard/references/jest.md` — for frontend Angular/TypeScript tests.
- `.claude/skills/test-guard/references/frontend-test-harness-constraints.md` — how
  Angular specs run in this project (Vitest fork cap, jsdom missing browser APIs, safe
  patterns); consult when a frontend test finding depends on how the harness behaves.

Recognize test files by pattern: `*Tests.cs`, `*Test.cs`, `*.spec.ts`, `*.test.ts`, and
files under `tests/` or `__tests__/`. Test-code quality findings still use the same
severity levels as every other engineering finding and **must influence the final
verdict** — they fold into the **Findings** section below and drive the dedicated
**Test Guard Review** section. Git tracking/staging status is not test-code quality
and must not fold into Findings or verdict.

**If Backend changed, also read:**

- `Backend/.architecture/BACKEND_STRUCTURE.md`
- `Backend/.architecture/CLEAN_ARCHITECTURE.md`
- `Backend/.architecture/API_GUIDELINES.md` — when API endpoints, controllers,
  middleware, API contracts, response shapes, Swagger/OpenAPI, health checks, or
  API configuration are involved.

**If Frontend changed, also read:**

- `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md`
- `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` — when UI, styles,
  theme, layout, RTL, shared classes, or component visual styling are involved.
- `Frontend/quran-dashboard-ui/.architecture/API_INTEGRATION_GUIDELINES.md` — when
  frontend API services, data-access files, facade/store API orchestration,
  `ApiResponse<T>`, DTO/view model mapping, loading/error/empty states,
  pagination/filter/search API integration, or backend message handling are
  involved.
- `PRODUCT.md` and `DESIGN.md` — when UI/product/design decisions are involved.

**If both backend and frontend changed, read both relevant sets.**

**If the change was implemented from Spec Kit, also read the Spec Kit artifacts.**

This applies when the user asks to review an implementation that came from Spec Kit,
or the request mentions any of:

- a **Phase** (e.g. "implemented Phase 3")
- a **User Story / US** (e.g. "US2")
- **task IDs** like `T001`, `T013–T018`
- `specs/<feature>/`
- `spec.md`, `plan.md`, `tasks.md`
- `contracts/`
- `quickstart.md`
- "implemented Phase" / "implemented tasks"

In that case, additionally read:

- `.claude/skills/engineering-review/SPEC_KIT_IMPLEMENTATION_REVIEW.md` — the extra
  Spec Kit review rules.
- the relevant `specs/<feature>/spec.md`
- the relevant `specs/<feature>/plan.md`
- the relevant `specs/<feature>/tasks.md`
- the relevant files under `specs/<feature>/contracts/` (the feature's planned contracts); the implemented truth is the code + nearest README (indexed by `docs/contracts/`)
- `specs/<feature>/quickstart.md` — when verification or acceptance criteria are
  involved.

If the change is a simple, non–Spec-Kit change, do **not** read the Spec Kit
artifacts; review the code normally.

If a referenced document is missing or unavailable, state that clearly in the
output rather than inventing its rules.

## General Review Goals (both stacks)

1. **Clean Code** — clear descriptive names; small focused units; no vague names
   like `DataItem`, `Obj`, `Temp`, `Info2`; comments forbidden by default per the root
   `CLAUDE.md` (not merely "no comments restating obvious code");
   readable flow. For deeper checks (intention-revealing names, function size and
   single-level-of-abstraction, command/query separation, when a comment earns its
   keep), see `references/clean-code-guard/naming-and-functions.md` and
   `references/clean-code-guard/comments-and-formatting.md`.
2. **SOLID** — single responsibility (one reason to change); focused, useful
   abstractions; high-level logic depends on abstractions not concrete
   infrastructure; interfaces not bloated; implementations honor contracts. For the
   detection smells and modern phrasings, see `references/clean-code-guard/solid.md`.
3. **DRY / KISS / YAGNI** — no duplicated business/validation logic; no unnecessary
   abstractions; no unrequested future features; simplest solution that meets the
   requirement. For knowledge-vs-text duplication, the wrong-abstraction caveat, and
   complexity ceilings, see `references/clean-code-guard/dry-kiss-yagni.md`.
4. **Strong typing** — explicit types in C# and TypeScript; avoid TypeScript `any`
   unless justified; enums/constants for known values; no magic strings/numbers
   where a named constant is clearer.
5. **Focused changes** — change matches requested scope; no unrelated files
   touched; no broad refactor or UI redesign mixed into feature work.
6. **Error handling** — errors are specific and actionable; raw internal exceptions
   are not exposed to users; avoid generic messages when a clearer one is possible.
   Note: the generic "prefer exceptions over return codes" guidance in the pack
   applies **inside layers**; at the API boundary the `ApiResponse` envelope and
   `Backend/.architecture/API_GUIDELINES.md` are authoritative.
7. **AI-generated-code failure modes** — watch for the systematic LLM defects
   catalogued in `references/clean-code-guard/ai-failure-modes.md`: catch-all error
   swallowing, defensive guards for impossible cases, premature abstraction, comment
   pollution, hallucinated APIs/packages, hardcoded "success"/mock returns in
   production code, copy-from-similar (plausible-but-wrong) logic, dead code, and
   speculative configurability. For a thorough quality pass, walk
   `references/clean-code-guard/review-checklist.md`.

## Backend Review Checklist

### 1. Clean Architecture and dependencies

- Domain has no API/EF/Infrastructure dependencies.
- Application does not depend on Infrastructure.
- Api uses Infrastructure only for composition/DI wiring, not controller logic.
- Controllers/endpoints stay thin.
- Business logic belongs in Domain/Application.
- Data access is behind abstractions where appropriate.

### 2. Backend file/folder placement

- Files are organized by domain/feature/bounded context.
- Avoid global dumping folders like `Enums`, `Models`, `DTOs`, `Helpers`, `Utils`,
  `Services` unless truly shared and small.
- Types live near the feature/domain/use case that owns them.

### 3. Backend file size and responsibility thresholds

Check the thresholds defined in `BACKEND_STRUCTURE.md`. You do not need to copy
every number, but explicitly review these file types against their soft/hard
thresholds:

- controllers/endpoints
- handlers
- services
- repositories/read services
- entities/aggregates
- DTOs/contracts/models

Review behavior:

- **Soft threshold exceeded:** ask whether the size is justified and whether
  splitting would improve clarity.
- **Hard threshold exceeded:** mark as a finding and recommend a split.
- **1000+ line backend files:** mark as a serious design smell unless explicit
  human approval exists.
- Thousands-of-lines services are not acceptable.

Terminology: do not use "God service". Use **overloaded service** or **oversized
service**.

### 4. Backend API guideline checks (when API files changed)

- route naming and stability
- HTTP verb correctness
- status codes
- `ApiResponse` shape consistency
- error handling and validation
- localization/messages (Arabic default; English identifiers; no scattered
  hardcoded user-facing strings)
- Swagger/OpenAPI impact
- not leaking internal details, stack traces, SQL, or file system paths

## Frontend Review Checklist

### 1. Feature/component structure

- Feature-first organization under `src/app/features/<feature>/`.
- No global `components/` / `services/` / `models/` dumping folders.
- Routeable smart/page components live in `pages/`.
- Child/presentational components live in `components/` unless truly shared.
- `data-access/` owns API services.
- `state/` owns facade/store/state services.
- `models/` owns feature DTOs/view models/types.

### 2. Component structure

- Components use separate `.html` and `.scss` files by default.
- No inline HTML in `.ts` except a tiny inline component with explicit approval.
- Routeable smart/page components act as shells/orchestrators.
- Large pages are split into meaningful child components.
- Child components do not call backend API services directly.
- Child components receive data through inputs and emit events through outputs
  where practical.

### 3. Frontend file size and responsibility thresholds

Check the thresholds defined in `FRONTEND_STRUCTURE.md` for:

- component TS
- component HTML
- component SCSS
- API services
- facade/store/state services
- utility/helper files

Review behavior:

- **Soft threshold exceeded:** ask whether the size is justified and whether
  splitting would improve clarity.
- **Hard threshold exceeded:** mark as a finding and recommend a split.
- **1000+ line frontend files:** mark as a serious design smell unless explicit
  human approval exists.
- **3000+ line files** are not acceptable and must be split.

Terminology: prefer **overloaded component**, **overloaded service**, **oversized
store**. Do not use "God service".

### 4. Routeable components and URL state

- Every routeable smart/page component has a stable route.
- Child components are not given routes unless they are standalone screens.
- Dynamic navigation links to route definitions, not component classes.
- Important tabs that change the main content are represented in URL state.
- Child routes are used for major tab sections.
- Query params are used for lighter display/filter modes.
- Local state only for minor modal/panel tabs.

### 5. Frontend API integration (when data-access/state/API files changed)

- default flow respected: Page Component → Facade/Store → API Service → Backend.
- API services return `Observable<ApiResponse<T>>` (or the project equivalent).
- the facade/store unwraps `ApiResponse<T>` (components do not repeatedly unwrap
  raw responses).
- components receive page-ready state.
- loading/empty/error state is explicit (loading not confused with empty).
- both HTTP transport errors and backend-controlled failures
  (`isSuccess === false`) are handled.
- DTOs / view models / state models are separated where transformation is needed.
- pagination/filter/search state belongs in facade/store, and in the URL when
  important.
- no Quranic data is fabricated in fallback logic.

### 6. UI style system (when UI/style/template changes exist)

- use of centralized `qd-` classes and CSS variables.
- no repeated one-off card/button/input/table/modal styles.
- no hardcoded color palette in component SCSS.
- light/dark theme compatibility.
- RTL correctness.
- accessibility: visible focus state, sufficient contrast, accessible disabled
  state, no color-only meaning.
- Quranic text readability and safety.

## Quranic Data Safety

Source-sensitive data is the highest-priority safety area. The full rules live in the
shared reference — apply them in full:

- `.claude/skills/engineering-review/references/quran-data-safety.md`

In short, review whether the change invents Quran data, silently corrects it, hides
missing/unknown data, drops traceability/provenance/source checks, harms Quran
readability or RTL semantics, or guesses instead of reporting uncertainty.

Any such issue is high priority (treat as BLOCKING or MAJOR depending on impact).

## Test-Code Review (delegated to test-guard)

When the diff includes test files, review the **test code** using `test-guard`'s nine
rules (see the reading rules above). In scope: test behavior vs implementation,
justified boundary mocks, data-driven variants, no tests for framework guarantees, real
entities/DTOs/value objects, real infrastructure for persistence/query correctness, and
source-safe Quranic test data. `engineering-review` still owns production code,
architecture, Spec Kit compliance, Quranic data safety, API contracts, frontend
structure, UI/product checks, and the final verdict.

### Test Guard Review (required whenever tests changed)

Whenever the diff adds or modifies test files, you **must** produce a dedicated **Test
Guard Review** subsection (see the Review Output Format below). It is not optional, and it
is **not** satisfied by listing `test-guard` under "Docs read". Evaluate, at minimum:

- whether the tests actually prove the requested behavior.
- assertion strength (specific, meaningful assertions vs. trivially-true checks).
- false-positive risk (tests that pass regardless of correctness).
- negative and edge-case coverage.
- regression coverage for the behavior being changed or fixed.
- fixture isolation and cleanup (no leaked shared state between tests).
- synthetic/test data safety (no real or fabricated Quranic source data).
- whether tests compare only counts or samples when full projections or checksums are
  needed to prove correctness.
- whether the tests can pass while the target behavior is broken.

**Severity guidance for test-code findings:**

- A test that can pass while the required behavior is broken is **at least MAJOR**.
- Weak or missing test coverage that affects contract-critical behavior is **BLOCKING**.
- Git tracking/staging state is not a severity item. Untracked files must never be
  Findings, Notes, blockers, severity items, Test Guard issues, final verdict inputs,
  or reasons for `CHANGES REQUESTED`.

Keep this distinct from build/test verification below:

- **test-guard answers:** is the test *code* good?
- **Verification (below) answers:** were builds/tests *run*, and what happened?

## Testing and Verification

- Build was run when relevant.
- Tests were run when available or when logic is sensitive (parsing, validation,
  mapping, importers, business rules).
- Data-related work includes a validation/report path.
- Any skipped verification is clearly stated. If build/test status is unknown, say
  unknown — do not assume success.
- Judge verification *sufficiency* against `TESTING_STRATEGY.md` (workspace root),
  the single source of truth for test selection. Its execution-trigger matrix (§5) is
  the authoritative changed-scope→lane mapping; read it there rather than from any
  restatement, including this one:
  - verify the executed **lane** matches the changed scope and risk. Backend lanes are
    arguments to `Backend/scripts/test-backend` (§3); Frontend lanes are
    `npm run test:*` scripts (§4). The former Tier A–E labels are superseded — only
    `tier-b` survives, as the name of the Backend no-pipeline lane;
  - do not demand a full or exhaustive suite when the matrix accepts a focused lane
    for the change under review;
  - a hand-written `--filter "FullyQualifiedName~…"` or `npm test -- --include=…` is
    not a lane and is not reportable evidence for one (§1) — ask for the lane name;
  - a change hitting a pipeline/canonical trigger (`DataPipelines`, importer/data-generation
    tools, pipeline tables/migrations, canonical resources, shared persistence
    infrastructure) with no `pipeline --feature <key>` / `canonical-data` run is a
    **BLOCKING** finding;
  - **missing canonical resources fail the lane; they do not skip it** (§3.4, §9). The
    runner preflights and exits non-zero, printing `canonical data tier: ran | not
    selected | discovery only | failed preflight`. A "canonical tests skipped, resources
    absent" claim attributed to a runner lane is missing evidence, not passing evidence;
  - there is no CI (`TESTING_STRATEGY.md` §8) — never accept "CI is green" as
    evidence, and never assume a gate ran because a workflow would have run it;
  - the route-smoke gate is active (§6). For route, contract, auth, middleware,
    or binding changes, expect the focused `Access` / `ApiBehavior` / `Middleware` /
    `RateLimiting` / `Health` feature lanes for the touched families plus
    `Backend/scripts/test-backend smoke --no-build`. **A change that touched an API
    route, contract, auth, middleware, or binding without the `smoke` lane running is a
    BLOCKING finding**, as is a route added or changed without the matching
    `SmokeRouteCatalog` entry in the same change (`SmokeCoverageParityTests` fails
    otherwise);
  - the canonical Smoke **data** tier is a separate lane: `SmokeDataReadTests` is
    `Kind=Canonical`, so `smoke` excludes it and `canonical-data` runs it (§6). Evidence
    MUST name the lane it came from; an unqualified "smoke passed" is not enough. A
    *stale or corrupt* dump fails loud rather than skipping — read the message rather
    than treating a failure as a resources problem;
  - a lane that shards (`canonical-data`, `pre-pr` — §3.3) MUST report both shard
    results; either shard failing fails the lane.

  (Section numbers above refer to `TESTING_STRATEGY.md`, not this skill's own
  numbered output sections.)

## Severity Levels

Tag every finding with one of:

- **BLOCKING** — must fix before merge/start/continue.
- **MAJOR** — should fix soon; risky but not necessarily blocking.
- **MINOR** — cleanup or clarity improvement.
- **NOTE** — observation only.

## Review Output Format

Return the review in this structure:

# Engineering Review

## 1. Verdict

Use one of:

- PASS
- PASS WITH NOTES
- CHANGES REQUESTED
- BLOCKED

When tests were added or modified, the final verdict **must account for the Test Guard
Review verdict** below. In particular: contract-critical weak tests must prevent PASS,
and tests that can pass while the required behavior is broken should produce CHANGES
REQUESTED or BLOCKED depending on impact.

## 2. Scope Reviewed

- backend files reviewed
- frontend files reviewed
- docs read

## 3. Spec Kit / Task Compliance Check

Include this section **only** when the change was implemented from Spec Kit (see the
conditional reading rules above and `SPEC_KIT_IMPLEMENTATION_REVIEW.md`). It reports:

- **Spec/feature reviewed** — the `specs/<feature>/` under review.
- **Phase/tasks reviewed** — the phase, user story, and/or task IDs in scope.
- **Tasks completed** — task IDs confirmed implemented, with the file(s) for each.
- **Tasks skipped** — required tasks in the requested phase that are missing or partial.
- **Future tasks implemented early** — later-phase work that leaked in (with severity).
- **Contract compliance summary** — per relevant contract: matches / deviates (with the
  difference) / not applicable.
- **Scope verdict** — whether the implementation stayed within the requested
  phase/tasks.

If Spec Kit does not apply, omit this section or write:
`Not applicable — review was not based on Spec Kit artifacts.`

## 4. Findings

For each finding:

- **Severity:** BLOCKING / MAJOR / MINOR / NOTE
- **File/path:** if applicable
- **Issue:** what is wrong
- **Why it matters:** the risk or principle involved
- **Suggested fix:** practical direction (do not implement it)

If none, write: None.

## 5. Threshold Check

When relevant:

- files near/over their soft thresholds
- files over their hard thresholds
- any 1000+ line files

If not applicable, say so.

## 6. Architecture / Responsibility Check

Summarize whether responsibilities are properly split (backend layering and/or
frontend component/data-access/state separation).

## 7. Quranic Data Safety Check

State explicitly one of: PASS / CONCERN / NOT APPLICABLE, with a one-line reason.

## 8. Test Guard Review

Include this section **only** when the diff adds or modifies test files (see the
conditional reading rules and the Test-Code Review section above). If no test files
changed, omit it. When present, report:

- **Test scope** — which test files/areas changed and what behavior they target.
- **Assertion strength** — are assertions specific and meaningful, or trivially true?
- **False-positive risks** — can these tests pass regardless of correctness, or while
  the target behavior is broken?
- **Missing coverage** — negative/edge cases, regression coverage, and full
  projections/checksums where comparing only counts or samples is insufficient.
- **Fixture/data safety** — synthetic test data only; no real or fabricated Quranic
  source data.
- **Test isolation** — fixture isolation and cleanup; no leaked shared state.
- **Test Guard verdict:** PASS / PASS WITH NOTES / CHANGES REQUESTED / BLOCKED.

## 9. Verification Check

Report build/test evidence if provided, and state the executed lanes versus the lanes
`TESTING_STRATEGY.md` §5 requires for the changed scope (sufficient / insufficient /
stale). If no build/test was run, say so clearly. A canonical lane reported as skipped
for absent resources counts as missing evidence — the runner fails that lane instead.

## Commit workflow reminder

Include this optional section only if Git tracking/staging concerns were noticed. This
section is outside the engineering verdict and must not affect Findings, Notes, Test
Guard verdict, or final Verdict. Direct the user to `commit-workflow` for git status,
untracked files, explicit staging, commit omission risk, commit planning, and push
readiness.

## 10. Final Recommendation

Short, direct next step consistent with the verdict.

## Guardrails

- Be direct and practical.
- Do not invent facts; if the diff or file tree is unavailable, request it.
- If build/test status is unknown, say unknown.
- Git tracking/staging state is not a verdict driver: do not mark CHANGES REQUESTED,
  downgrade the verdict, add Findings/Notes, or ask the implementer to stage/commit
  because a file is untracked or unstaged. Review implementation content as-is; if Git
  concerns are noticed, use only the optional `Commit workflow reminder` section.
- Separate findings by severity; do not inflate severity.
- Do not request broad refactors unless necessary.
- Do not implement fixes unless explicitly asked.
- Avoid skill explosion: this single skill covers backend and frontend review.
- Avoid religiously inappropriate terminology such as "God service"; use
  "overloaded service" or "oversized service".
