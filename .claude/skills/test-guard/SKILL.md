---
name: test-guard
description: Use when explicitly asked for Quran Dashboard Backend or Playwright test-code quality guidance or review.
---

# Test Guard

## Responsibility

Judge or guide **how retained Backend and Playwright tests are written** against the nine rules
below, for changed or proposed `*Tests.cs`, `*Test.cs`, and `*.e2e.ts` code. When invoked before test writing, apply the rules as
guidance so violations are never written; when reviewing, report violations. This
skill's result is evidence — the formal review verdict stays with the separately
requested `engineering-review`, which consumes an existing same-diff Test Guard result.

**Not this skill's job:** production-code review, selecting or running tests, judging
whether executed test evidence was sufficient for a changed scope
(`TESTING_CONSTITUTION.md` owns policy; the Backend and E2E READMEs own commands;
`engineering-review` owns the verification verdict), implementing test fixes, Git, or loading stack references the changed tests do
not use. Be a sharp reviewer, not a pedantic one: flag what wastes maintenance effort or
hides real bugs, ignore cosmetic preferences.

The Test Freeze applies: updating a retained Permanent test or a triggered change/release gate is
narrowly allowed, while any new Backend or Playwright test requires owner approval recorded in the
active Testing Decision. This Skill never supplies that approval. Frontend unit tests are prohibited.

## The Nine Rules

### Rule 1: Test behavior, not implementation
Test what code does from the caller's perspective. Assert return values and observable
side effects. Never assert that an internal helper was called with specific arguments —
that test breaks on every refactor while catching nothing.

### Rule 2: Every mock must be justified
Mock only at system boundaries: network/HTTP, databases, filesystem I/O on external
files, clock and randomness, third-party SDKs. Never mock internal classes or helpers to
isolate a "unit" — the seams hide the integration bugs worth catching. When you mock a
boundary, assert what the caller *does with the response*, not that the mock received
specific arguments.

### Rule 3: One scenario per test, data-driven for variants
Tests sharing identical setup that differ only in input/output values merge into one
data-driven test (xUnit `[Theory]`, Playwright parameterized cases). Separate tests are correct when
setup, assertions, or boundary mocks genuinely differ.

### Rule 4: Every test must justify its existence
Ask: "What bug does this catch that no other test catches?" Delete tests that only catch
typos, verify default values of data classes, or test trivial pass-through logic.

### Rule 5: Name tests for the scenario
`Method_Scenario_ExpectedOutcome` or a requirement-style sentence — the name reads like
a requirement, not an echo of the function signature.

### Rule 6: Production regression tests are sacred
Tests reproducing a real production bug are always justified. Reference the incident in
the name or a comment and never delete them. Exempt from Rule 4.

### Rule 7: No tests for framework guarantees
Don't test that the validation library validates, the ORM commits, or the router
returns 404. Test *your* logic on top. Smell: a test that would still pass with all the
project's custom code deleted.

### Rule 8: State and value objects are real, never mocked
Never mock a data model, DTO, entity, or state object — construct a real instance.
Mocking state hides field-name typos and validation errors. If construction is painful,
that is design feedback: add a small builder or factory, don't mock.

### Rule 9: Infrastructure under test gets real infrastructure
When database queries, schema behavior, or persistence logic *is the subject*, run
against a real test database with real migrations via fixtures. Mocking the session
there tests nothing. Mocking the database is fine when persistence is only a side effect
of the behavior under test.

## Severity guide

- **Must fix:** Rules 1, 2, 8 — these hide real bugs or make tests brittle.
- **Should fix:** Rules 3, 4, 5, 7 — these cause bloat and maintenance drag.
- **Sacred:** Rule 6 — never delete, always allow.
- **Worth noting:** Rule 9 — test architecture; flag it, don't block small changes on it.

## Reporting format

```
**Rule N violation** in `tests/path/file.ext::<test_name>`
- What: <one sentence describing the violation>
- Fix: <one sentence describing what to do instead>
```

Group violations by file; don't mention clean files. When writing new tests, ask for
each: "What specific bug does this catch that no other test catches?" — no clear answer,
no test. Do not flag pre-existing violations in untouched files unless asked to audit.

## Conditional references

- [references/dotnet.md](references/dotnet.md) — when .NET/xUnit tests are in scope.
- `Frontend/quran-dashboard-ui/e2e/README.md` — when Playwright journeys or fixtures are in scope.
  Load both only when the change genuinely spans Backend and Playwright.
- `Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/README.md` and
  `Backend/tests/QuranDashboard.Tests/README.md` — only for real PostgreSQL
  fixture/serialization scope.
- `CODING_PRINCIPLES.md` §10 — Quranic data safety applies to test data in full
  (synthetic-only, clearly labeled placeholders, never hand-typed "real" scripture);
  load it whenever test data is source-sensitive.
