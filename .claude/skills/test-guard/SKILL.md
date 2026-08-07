---
name: test-guard
description: "Review generated or changed test code against universal testing rules before it ships, for the Quran Dashboard workspace (.NET/C# backend, Angular/TypeScript frontend). Use reactively after an agent writes, edits, generates, or refactors tests, and when the request is test-specific: 'write tests for X', 'add tests', 'edit tests', 'review these tests', or when test files appear in a diff you are checking. Recognizes test files by pattern: *.spec.ts, *.test.ts, *Tests.cs, *Test.cs, and files under tests/ or __tests__/. Can also guide test writing when explicitly invoked before the work. Scope is test-code quality only; engineering-review remains the primary holistic post-implementation review skill. This skill is the quality gate that prevents AI-generated test bloat."
---

# Test Guard

You are reviewing generated or changed test code before it ships. Enforce the rules below after the first test-writing pass and before the tests are presented, committed, or merged. Be a sharp reviewer, not a pedantic one: flag what wastes maintenance effort or hides real bugs, ignore cosmetic preferences.

These rules exist because coding agents over-generate tests. The common failure modes: mock-heavy unit tests that assert implementation details, near-duplicate test bodies that differ by one value, and tests that re-verify the framework instead of the project's logic. Each looks productive in a diff and costs maintenance forever.

## Scope and precedence (this workspace)

- **Project rules win.** When this skill conflicts with the project's own rules (`CLAUDE.md`, `AGENTS.md`, `CODING_PRINCIPLES.md`, the Backend/Frontend architecture docs, or a spec/contract), follow the project. In particular, Quranic data safety always applies to tests.
- **`engineering-review` remains the primary holistic post-implementation review skill** — it owns production code, architecture, Spec Kit compliance, Quranic data safety, API contracts, frontend structure, UI/product checks, and the final verdict. It delegates the *test-code* portion of a review to this skill.
- **`test-guard` owns only test-code quality** — *how* tests are written. It does not review production code and does not run tests. Whether the *right* tests were run — the required verification tier for the changed scope — is judged against the workspace `TESTING_STRATEGY.md` (see "Verification tiers and evidence sufficiency" below).

## When this skill activates

- A coding agent has just written new test functions or test files
- You are editing existing tests
- You are reviewing the test-code portion of a diff that contains test changes
- The user asks you to write, add, edit, or review tests

## Adapt to the project first

These rules are universal, but their application is not. Before reviewing:

1. Check the project's own agent instructions (`CLAUDE.md`, `AGENTS.md`) and testing docs. Project-specific testing rules win over this skill when they conflict.
2. Identify the test stack, then read the matching reference for concrete patterns:
   - .NET / C# / xUnit (or NUnit/MSTest) → [references/dotnet.md](references/dotnet.md)
   - Angular / TypeScript tests (Jest / Vitest / Jasmine) → [references/jest.md](references/jest.md)
3. If the project calls LLM APIs, uses agent/workflow frameworks, or wires up observability/telemetry, also read [references/llm-app-testing.md](references/llm-app-testing.md) — it adds three rules specific to LLM applications.
4. Map the project's system boundaries: network calls, databases, filesystem, clock and randomness, third-party SDKs, LLM APIs. Existing fixtures and test helpers usually reveal where the project already draws these lines.

## What to do

1. Read the test code: the diff, the new file, or the section being modified.
2. Check each test against the rules below.
3. Report violations concisely: rule number, location, why it violates, suggested fix.
4. If the user explicitly invokes this skill before test writing, apply the rules as you write — don't write violations and then flag them.

When writing new tests, ask for each test: "What specific bug does this catch that no other test in this suite catches?" If you can't answer clearly, don't write it.

## The Nine Rules

### Rule 1: Test behavior, not implementation
Test what code does from the caller's perspective. Assert return values and observable side effects. Never assert that an internal helper was called with specific arguments — that test breaks on every refactor while catching nothing.

**Violation pattern:** asserting a mock of an internal function was called, where that function is not a system boundary.
**Fix:** assert the return value or the state change the caller observes.

### Rule 2: Every mock must be justified
Mock only at system boundaries: network and HTTP calls, LLM APIs, databases, filesystem I/O on external files, clock and randomness, third-party SDKs. Never mock internal classes or helper functions to isolate a "unit" — the seams you create hide the integration bugs worth catching.

When you mock a boundary, assert what the caller *does with the response*, not that the mock received specific arguments.

### Rule 3: One scenario per test, data-driven for variants
If two or more tests share identical setup and differ only in input/output values, merge them into one data-driven test (xUnit `[Theory]` + `[InlineData]`/`[MemberData]`, Jest `test.each`).

**When separate tests ARE correct:** different setup, different assertions, different mock configurations, or genuinely different scenarios that happen to exercise the same function.

### Rule 4: Every test must justify its existence
Ask: "What bug does this catch that no other test catches?" Delete tests that only catch typos, verify default values of data classes, or test trivial pass-through logic.

**Common unjustified tests:** constructors setting attributes, a function rejecting input the type system already forbids, string formatting of log messages, a constant equaling its literal value.

### Rule 5: Name tests for the scenario
Pattern: `Method_Scenario_ExpectedOutcome` (or a requirement-style sentence). The name should read like a requirement, not echo the function signature.

| Bad | Good |
|-----|------|
| `TestParseResponse2` | `Malformed_response_falls_back_to_default` |
| `GetLanguage_NoClass` | `Element_without_class_returns_empty_language` |
| `AddTags_SingleString` | `Single_tag_normalizes_to_list` |

### Rule 6: Production regression tests are sacred
Tests that reproduce a real production bug are always justified. Reference the incident (date, issue ID, or short description) in the name or a comment, and never delete them. They are exempt from Rule 4 — their justification is the incident.

### Rule 7: No tests for framework guarantees
Don't test that the validation library validates, the ORM commits, the router returns 404, or the test framework's fixtures work. Test *your* logic that sits on top of the framework.

**Violation pattern:** a test that would still pass if you deleted all the project's custom code and kept only framework defaults.

### Rule 8: State and value objects are real, never mocked
Never mock a data model, DTO, entity, or state object. Construct a real instance. Mocking state hides field-name typos and validation errors — exactly the bugs worth catching. If constructing the real object is painful, that is design feedback, not a reason to mock; add a small builder or factory helper.

### Rule 9: Infrastructure under test gets real infrastructure
When database queries, schema behavior, or persistence logic *is the subject* of the test, run against a real test database with real migrations applied via fixtures. Mocking the session there tests nothing. Mocking the database is fine when persistence is only a side effect of the behavior under test.

## Reporting format

When flagging violations, use this format:

```
**Rule N violation** in `tests/path/file.ext::<test_name>`
- What: <one sentence describing the violation>
- Fix: <one sentence describing what to do instead>
```

Group violations by file. If a file has no violations, don't mention it.

## Severity guide

Not all violations are equal. Use judgment:

- **Must fix:** Rules 1, 2, 8 — these hide real bugs or make tests brittle
- **Should fix:** Rules 3, 4, 5, 7 — these cause bloat and maintenance drag
- **Sacred:** Rule 6 — never delete, always allow
- **Worth noting:** Rule 9 — test architecture; flag it, but don't block small changes on it

## Execution lanes and evidence sufficiency

This skill judges *how* tests are written. When the request also puts test **evidence**
in front of you (a phase hand-off, a pre-PR package, a review report), judge sufficiency
against the workspace policy — do not invent your own bar:

1. Read `TESTING_STRATEGY.md` (workspace root) — the single source of truth for test
   selection and execution lanes (§1).
2. Derive the required lane from the changed paths and risk using the execution-trigger
   matrix (§5). Backend lanes are arguments to `Backend/scripts/test-backend` (§3);
   Frontend lanes are `npm run test:*` scripts (§4). The former Tier A–E labels are
   superseded; only `tier-b` survives, as the name of the Backend no-pipeline lane.
3. Compare executed vs required and say sufficient / insufficient / stale. Evidence
   produced before the last code change is stale (§2).
4. A hand-written `--filter "FullyQualifiedName~…"` or `npm test -- --include=…` is not a
   lane and is not reportable evidence for one (§1). Ask for the lane name.

Three tree-specific facts that change the verdict (section numbers refer to
`TESTING_STRATEGY.md`):

- **No CI exists** (§8). "CI is green" is not available as evidence here.
- **The route-smoke gate is active** (§6). When the test changes under review touch API
  routes, request/response contracts, auth, middleware, or binding, ask for a
  `Backend/scripts/test-backend smoke --no-build` run. A new or changed route also needs
  its `SmokeRouteCatalog` entry in the same change. The canonical Smoke **data** tier is a
  *separate lane* — `SmokeDataReadTests` is `Kind=Canonical`, so `smoke` excludes it and
  `canonical-data` runs it. Evidence must say which lane ran; an unqualified "smoke passed"
  is not enough.
- **Missing canonical resources fail the lane, they do not skip it** (§3.4, §9). The runner
  preflights and exits non-zero, printing `canonical data tier: ran | not selected |
  discovery only | failed preflight`. A canonical claim of "skipped, resources absent" from
  a runner lane is not a resources problem — it means the run did not come from the runner,
  and it is not evidence.
- **The browser E2E layer is opt-in, never a required gate** (§11). An E2E run may be
  offered as supplementary evidence and must say so; it does not substitute for the `smoke`
  lane or for any required lane.

Selecting the lane is not this skill's call to override — `engineering-review` owns the
final verification verdict. Report the mismatch; don't block on it alone.

## References

- [references/dotnet.md](references/dotnet.md) — .NET/C# patterns: xUnit `[Theory]`, `WebApplicationFactory`, EF Core + PostgreSQL via Testcontainers, `ApiResponse` envelope, real entities/DTOs, Quranic data safety
- [references/jest.md](references/jest.md) — Jest/Vitest/Angular TS patterns: `test.each`, module mocks, msw, snapshot discipline, Testing Library queries
- [references/frontend-test-harness-constraints.md](references/frontend-test-harness-constraints.md) — this project's Angular/Vitest/jsdom harness: focused-run command + fork cap, the `--run` caveat, jsdom's missing browser APIs (matchMedia/ResizeObserver/layout/CDK virtual scroll), and safe patterns to preserve (words-labels TDZ getters)
- [references/llm-app-testing.md](references/llm-app-testing.md) — three extra rules for LLM applications: prompt contracts, observability wiring, agent-flow transitions

## What this skill does NOT do

- It does not run tests. Use the project's test runner for that.
- It does not enforce code style — that's the linter's job.
- It does not decide *what* to test — only *how* to test it, plus (per
  `TESTING_STRATEGY.md`) whether the executed tier of tests was sufficient
  evidence for the changed scope.
- It does not review production code — that is `engineering-review`'s job.
- It does not flag pre-existing violations in files you're not touching, unless asked to audit.
