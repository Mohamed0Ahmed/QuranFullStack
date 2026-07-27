# Skills & Architecture Guide — Quran FullStack Workspace (المنهج القرآني)

A single practical map of the custom **skills**, **project-level documents**, and
**architecture reference files** used in this workspace, plus the recommended
workflows for implementation, review, Spec Kit, tests, and commits.

> This guide is an **index and decision aid**, not a replacement for the canonical
> docs. Where it summarizes a rule, the linked file remains the source of truth.
> Do not copy architecture-doc or skill content into this guide; point to it.

_Reflects the workspace as of 2026-07-11._

## Workspace shape

This is one canonical Git monorepo rooted at `App/`:

- **Workspace:** project docs, `.claude/skills/`, and Spec Kit specs.
- **Backend:** `Backend/` - .NET 10, ASP.NET Core Web API, EF Core, PostgreSQL, Code First, Clean Architecture.
- **Frontend:** `Frontend/quran-dashboard-ui/` - Angular / TypeScript.

Backend and Frontend are ordinary tracked directories. Git status, commits, branches,
PRs, and pushes operate once from the monorepo root.

---

## Table of contents

1. [Primary project-level documents](#1-primary-project-level-documents)
2. [Current skills](#2-current-skills)
   - [Invocation & reading behavior](#invocation--reading-behavior)
3. [Backend architecture docs](#3-backend-architecture-docs)
4. [Frontend architecture docs](#4-frontend-architecture-docs)
5. [Recommended workflows](#5-recommended-workflows)
6. [Decision matrix](#6-decision-matrix)
7. [Anti-patterns](#7-anti-patterns)
8. [Gaps & next actions](#8-gaps--next-actions)

---

## 1. Primary project-level documents

These live at the workspace root and apply across Backend + Frontend.

| File | What it is for | Who/what reads it | When it matters |
|------|----------------|-------------------|-----------------|
| `CODING_PRINCIPLES.md` | General coding principles for the whole workspace: Clean Code, SOLID, DRY/KISS/YAGNI, separation of concerns, strong typing, focused changes, error handling, testing/verification, **Quranic Data Safety**, UI/product consistency, Definition of Done. Also points to the deeper `clean-code-guard` references and the `test-guard` skill. | Every agent/human before implementation; `engineering-review` (always reads it); `backend-structure-review` (always reads it). | All implementation and review work. |
| `TESTING_STRATEGY.md` | Single source of truth for **test selection, verification depth, execution tiers (A–E), slow data-pipeline triggers, and the phase/milestone/PR/release gates**. Tier A focused per-phase, Tier B no-pipeline milestone regression, Tier C ordinary pre-PR, Tier D pipeline-triggered, Tier E release/canonical acceptance, plus the change-to-tier matrix (its §4) and validated command catalogs (its §5 Backend, §6 Frontend). Records three absences that change what counts as evidence: **no CI** (its §8), **no route-parity/smoke gate** (planned only, its §13), **no browser E2E** (its §3 Tier E). | Every agent/human before selecting or running tests; `engineering-review` (verification sufficiency); `test-guard` (evidence tiers); `pr-context-prep` (evidence section). | Whenever tests are selected, run, or verification evidence is judged. |
| `PRODUCT.md` | Product strategy & context: register, users (Arabic-speaking admins/supervisors/teachers), product purpose (manage Quran research data, review ayah links, organize gates أبواب, publish), principles, anti-references. | Anyone doing user-facing/product or UI work. | Frontend/UX/product decisions and any backend change that affects user-facing behavior. |
| `DESIGN.md` | Visual/design direction — the "Quiet Scriptorium" north star: Arabic-first RTL, restrained parchment/ink palette, calm typography; explicitly rejects generic SaaS, kitschy religious decor, gamified UI, enterprise greige. Currently a **seed/direction** doc (see §8). | Anyone doing UI/visual work. | Frontend visual/design tasks. For concrete tokens/classes use `UI_STYLE_SYSTEM.md`. |
| `AGENTS.md` | Workspace entrypoint for non-Claude agents (Codex/OpenCode/etc.). Points to project instruction files, coding principles, the clean-code & test-code self-checks, and design context. | Non-Claude coding agents. | Loaded at session start for those agents. |
| `CLAUDE.md` | Same role as `AGENTS.md`, for Claude. Points to `CODING_PRINCIPLES.md`, the self-checks, and design context. | Claude Code. | Loaded at session start. |

**What should NOT be duplicated into these files:**

- Full clean-code / test-guard rule bodies (they live in the skills/references; the docs only *point* to them).
- Architecture rules (those live in the Backend/Frontend `.architecture/` docs).
- Spec Kit per-feature details (those live in the active feature's `specs/<feature>/`; merged features 001–019 had their `contracts/` removed, steady-state truth is code + README indexed by `docs/contracts/`).
- Anything that would create a second, drifting copy of a rule that already has a home.

---

## 2. Current skills

All custom skills live under `.claude/skills/`. Ten are workspace skills — the four
review/commit skills detailed below (`engineering-review`, `test-guard`,
`backend-structure-review`, `commit-workflow`) plus six operational skills
(`deploy-smoke`, `pr-context-prep`, `dependency-audit`, `performance-backend-review`,
`performance-angular-review`, `backend-global-usings-cleanup`). There are also **14
`speckit-*` skills** (the Spec Kit command set: `specify`, `clarify`, `plan`, `tasks`,
`analyze`, `implement`, `checklist`, `constitution`, the `git-*` helpers,
`taskstoissues`) — the Spec Kit workflow commands referenced in §5.

### Quick orientation

| Skill | Review-only? | Can implement? | Scope | Relationship to `engineering-review` |
|-------|:---:|:---:|-------|--------------------------------------|
| `engineering-review` | ✅ Yes | ❌ No (unless explicitly asked) | **Primary holistic post-implementation review** | — (it is the hub) |
| `test-guard` | Review **and** write-time guard | ✅ Authors/guards test code | **Test-code quality only** | Called *by* engineering-review for the test-file portion of a diff |
| `backend-structure-review` | ✅ Yes | ❌ No (unless explicitly asked) | **Backend structure / layering / placement** | A focused subset, not a replacement |
| `commit-workflow` | Planning + safe execution | Runs git (no destructive cmds) | **Git tracking, commit ordering & safe staging** | Independent (runs after review) |
| `deploy-smoke` | ✅ Report-only | ❌ No (build/migrate/smoke only) | **Local build/migrate/runtime smoke** | Independent (runtime gate, not code review) |
| `pr-context-prep` | ✅ Prep-only | ❌ No (never edits/commits/opens PR) | **PR context package before opening a PR** | Independent (PR-time; use after review) |
| `dependency-audit` | ✅ Report-only (audit) | ❌ No upgrades unless asked | **NuGet + npm vuln / staleness audit** | Independent (security hygiene) |
| `performance-backend-review` | ✅ Yes (explicit-invoke) | ❌ No | **Backend / EF Core / Postgres perf audit** | Perf counterpart; not the general gate |
| `performance-angular-review` | ✅ Yes (explicit-invoke) | ❌ No | **Angular frontend perf audit** | Perf counterpart; not the general gate |
| `backend-global-usings-cleanup` | ❌ No — action skill | ✅ Edits C# global usings | **Backend global-usings consolidation** | Independent (mechanical cleanup) |
| `clean-code-guard` | _Not a skill_ — reference pack | n/a | Deep clean-code references | Lives **inside** engineering-review |

### 2.1 `engineering-review` — the primary holistic review skill

- **Purpose:** the single, holistic **post-implementation** code review for the workspace (.NET backend + Angular frontend). Covers Clean Code, SOLID, DRY/KISS/YAGNI, separation of concerns, backend/frontend architecture, file-size/responsibility thresholds, routeable components & URL state, API integration & `ApiResponse<T>` handling, the UI style system, strong typing, focused scope, error handling, **Quranic Data Safety**, and build/test verification. When the change came from Spec Kit, it **also** applies phase/task/contract compliance.
- **Best used when:** reviewing a change, diff, PR, branch, or a completed Spec Kit phase; deciding whether implementation quality is engineering-ready.
- **Boundary:** it does **not** judge Git staging, commit ordering, push readiness, or untracked-file risk. It reviews implementation content, including untracked files when they are part of the requested scope, but Git tracking/staging state never affects findings, notes, Test Guard verdict, or final verdict.
- **Do not use when:** you only need a narrow structure question answered (use `backend-structure-review`), only test files changed (use `test-guard`), or you want fixes implemented (review is findings-only).
- **Reads / references (path-based, only what changed):**
  - Always: `CODING_PRINCIPLES.md`, and `TESTING_STRATEGY.md` when judging whether the executed tests were sufficient for the changed scope.
  - Deep clean-code: `.claude/skills/engineering-review/references/clean-code-guard/*`.
  - Backend changed: `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`.
  - Frontend changed: `Frontend/quran-dashboard-ui/.architecture/{FRONTEND_STRUCTURE,UI_STYLE_SYSTEM,API_INTEGRATION_GUIDELINES}.md`, plus `PRODUCT.md`/`DESIGN.md` for UI/product decisions.
  - Test files in the diff: applies `test-guard` (`references/dotnet.md` backend, `references/jest.md` frontend).
  - Spec Kit change: `.claude/skills/engineering-review/SPEC_KIT_IMPLEMENTATION_REVIEW.md` + the active feature's `specs/<feature>/{spec,plan,tasks,contracts}` (planning inputs); the implemented/steady-state truth is code + nearest README (indexed by `docs/contracts/`).
- **Output:** structured verdict (PASS / PASS WITH NOTES / CHANGES REQUESTED / BLOCKED), scope reviewed, optional Spec Kit compliance section, findings by severity (BLOCKING / MAJOR / MINOR / NOTE), threshold check, architecture/responsibility check, Quranic data safety check, verification check, final recommendation.
- **Review-only:** ✅ yes. **Implements changes:** ❌ no, unless the user explicitly asks for fixes as a separate task.

#### 2.1a `SPEC_KIT_IMPLEMENTATION_REVIEW.md` (add-on, inside engineering-review)

- **Purpose:** extra rules that **extend** engineering-review **only when** the change was implemented from Spec Kit. Verifies phase/task scope (no future-phase leakage, nothing skipped), task→file traceability, Locked-Decisions and Out-of-Scope compliance, contract compliance (compare against the feature's planned `specs/<feature>/contracts` **and** the implemented code + nearest README indexed by `docs/contracts/`; response envelope via `Contracts/ApiResponse.cs` + `API_GUIDELINES.md` §5), acceptance/quickstart verification, scope-creep, and single-source-of-truth derivation.
- **Best used when:** the request mentions a Phase, a User Story/US, task IDs (e.g. `T013–T018`), `specs/<feature>/`, `spec.md`/`plan.md`/`tasks.md`, `specs/<feature>/contracts/`, or "implemented Phase/tasks".
- **Do not use when:** the change is a simple, non–Spec-Kit change.
- **Relationship to engineering-review:** it is a conditional module of it, not a separate skill. Findings fold into the same output.

#### 2.1b `references/clean-code-guard/` (reference pack, inside engineering-review)

- **What it is:** vendored deep clean-code reference material — `naming-and-functions.md`, `comments-and-formatting.md`, `solid.md`, `dry-kiss-yagni.md`, `ai-failure-modes.md`, `review-checklist.md`, `sources.md`.
- **Status:** **NOT a separate skill** in this workspace — deliberately reference-only, to avoid trigger collision with `engineering-review`. (See §7.)
- **Used by:** engineering-review (for deep code-quality review) and the **clean-code self-check before delivery** in `CLAUDE.md`/`AGENTS.md`.
- **Project overrides:** C#/.NET `I`-prefixed interface names and the `ApiResponse` / `API_GUIDELINES.md` API boundary win over the generic guidance (recorded in `CODING_PRINCIPLES.md`).

### 2.2 `test-guard` — test-code quality (separate, narrow skill)

- **Purpose:** quality gate for **test code only** — nine universal rules (behavior not implementation, justified boundary mocks, data-driven variants, justified existence, scenario naming, sacred regression tests, no framework-guarantee tests, real entities/DTOs, real infrastructure for persistence). Prevents AI-generated test bloat.
- **Best used when:** writing, adding, editing, or reviewing tests; or a diff whose changed files are tests. Recognizes `*.spec.ts`, `*.test.ts`, `*Tests.cs`, `*Test.cs`, and files under `tests/` or `__tests__/`.
- **Do not use when:** reviewing production code (that's engineering-review), running tests (use the test runner), or enforcing style (linter).
- **Reads / references:** `references/dotnet.md` (.NET/xUnit, `WebApplicationFactory`, EF Core + PostgreSQL via Testcontainers, `ApiResponse` assertions, no `DbContext`/entity/DTO mocking, SQLite limits, source-safe Quranic test data); `references/jest.md` (Angular/TS); `references/llm-app-testing.md` (only if LLM/agent workflows are introduced). Project rules win over its generic rules.
- **Output:** per-file rule violations (`Rule N violation … What / Fix`), grouped by file, with a must-fix / should-fix / sacred / worth-noting severity guide.
- **Review-only:** no — it both **guards while writing tests** and reviews them. **Implements changes:** it authors/guards test code; it does not touch production code.
- **Relationship to engineering-review:** engineering-review **delegates the test-code portion** of a mixed diff to test-guard, and **only** when the diff contains test files. engineering-review keeps the final verdict and everything else.

### 2.3 `backend-structure-review` — focused backend structure review

- **Purpose:** review-only check of backend **file/folder organization, domain/feature foldering, Clean Architecture layering, dependency direction, file-size/responsibility thresholds, and Quranic data safety/traceability**. Keeps the backend organized by domain/feature first and prevents technical-type dumping folders.
- **Best used when:** "Where should this enum/value object/DTO/handler live?", "Review this proposed backend folder structure", "Did these new backend folders violate Clean Architecture?", or whenever new backend folders/files are added.
- **Do not use when:** you need the **full** post-implementation review gate (use engineering-review), or for frontend work.
- **Reads / references:** always `CODING_PRINCIPLES.md` + `Backend/.architecture/BACKEND_STRUCTURE.md`; `CLEAN_ARCHITECTURE.md` for layering/flow; `API_GUIDELINES.md` for API boundary. It **cites** these canonical docs rather than restating them.
- **Output:** verdict (PASS / PASS WITH NOTES / NEEDS CHANGES / BLOCKED), summary, blocking issues, structure notes, layering check, file-size check, anti-pattern check, Quranic data safety check, recommendations.
- **Review-only:** ✅ yes. **Implements changes:** ❌ no, unless explicitly asked.
- **Relationship to engineering-review:** a **focused subset** — structure/layering/placement only. It is **not** the general review and deliberately does not duplicate engineering-review.

### 2.4 `commit-workflow` - safe monorepo Git commits

- **Purpose:** plan and safely execute commits in the monorepo. Owns Git tracking/staging concerns, including status, untracked files, explicit staging, commit omission risk, focused commit boundaries, and push readiness. Inspects one root status, groups changes by concern, suggests concise messages, and warns about unrelated-file risks.
- **Best used when:** committing/staging/pushing or deciding focused commit boundaries.
- **Do not use when:** you want destructive Git operations (it never runs `reset`/`clean`/`rebase`) — those are out of scope.
- **Reads / references:** live root `git status`, diffs, branch, upstream, and recent log.
- **Output:** repository status, commit plan, staging plan, suggested messages, warnings, exact commands, final checklist.
- **Review-only:** no — it is **planning + safe execution** (runs non-destructive git only). **Implements source changes:** ❌ never modifies source code.
- **Relationship to engineering-review:** independent; typically used **after** review passes.

### 2.5 `deploy-smoke` — local build / migrate / runtime smoke

- **Purpose:** report-only check that a change still restores, builds, migrates, and runs locally — catches build breakage, pending/broken migrations, and dead endpoints before review/PR/commit. Verifies backend build + frontend build, inspects the local DB target, optionally applies **local** migrations only with explicit approval, and smokes `/api/health` + changed endpoints.
- **Best used when:** after an EF Core migration, before a review or PR, after a dependency/perf change, or before committing a cross-stack change.
- **Do not use when:** you want fixes applied (report-only), a full quality review (engineering-review), or a vulnerability audit (dependency-audit).
- **Hard rules:** local only; verify & display the DB target first; never drop/reset/reseed a DB; never target remote/production.
- **Output:** verdict (`PASS` / `PASS WITH DEPLOYMENT NOTE` / `CHANGES REQUESTED` / `BLOCKED`), scope, commands, evidence, deployment notes, risks/skipped, next action.
- **Review-only:** ✅ report-only. **Implements changes:** ❌ no.

### 2.6 `pr-context-prep` — PR context package (before opening a PR)

- **Purpose:** produce a copy-paste-ready PR context package so reviewers and CodeRabbit understand scope, risk, and invariants. Reads the branch diff/status vs base, classifies the change by **path group** (Backend, Frontend, specs/docs, cross-stack), and emits scope/out-of-scope, changed-file summary, related files & specs, critical invariants (Quran data safety first), test/build evidence, CodeRabbit focus, review checklist, size/split advice, risk level, and a merge-readiness call. Requires GitHub **merge commits** for PRs that import unsquashed subtree history.
- **Best used when:** about to open a PR, or asked for a PR title/description/reviewer focus/merge-readiness.
- **Do not use when:** you need Git staging/commit ordering/push execution (that is `commit-workflow`).
- **Review/prep only:** ✅ never edits, commits, or opens the PR.

### 2.7 `dependency-audit` — NuGet + npm security/staleness audit

- **Purpose:** audit backend NuGet and frontend npm dependencies for known vulnerabilities and staleness. Separates **direct** from **transitive** advisories, identifies the likely parent for a transitive one, and proposes the **smallest safe remediation** with verification commands.
- **Best used when:** checking for vulnerable/outdated packages, reacting to a CVE/advisory, or before bumping a dependency.
- **Do not use when:** implementing features, doing a full engineering/perf review, or a build/runtime smoke (recommend `deploy-smoke` after a bump).
- **Guardrails:** audit-first; no upgrades unless explicitly asked; never a major bump by default; never suppress an advisory without explicit approval; never mix dependency cleanup with feature/perf changes.
- **Review-only:** ✅ report-first. **Implements changes:** ❌ not unless explicitly asked.

### 2.8 `performance-backend-review` — backend/DB performance audit (explicit-invoke)

- **Purpose:** deep, **review-only** performance audit for the .NET / ASP.NET Core / EF Core / PostgreSQL backend — N+1 queries, tracking vs `AsNoTracking`, missing indexes/query plans, transaction/lock cost, pagination/result-size and payload over-fetching, streaming vs in-memory, caching of expensive reads, importer/DataPipeline runtime cost, and slow backend tests. Inspects only the changed backend scope unless a wider audit is requested.
- **Best used when:** the user **explicitly** asks for a backend or database performance review.
- **Do not use when:** you want general/engineering/PR/clean-code/structure review (those are `engineering-review` / `backend-structure-review`), or frontend perf (that is `performance-angular-review`). Triggers only on explicit backend-perf intent, not the word "review".
- **Output:** evidence-based findings, severities, recommendations — **never code fixes.**

### 2.9 `performance-angular-review` — frontend performance audit (explicit-invoke)

- **Purpose:** deep, **review-only** performance audit for the Angular 20 frontend — change-detection cost, Signals/computed/effect recomputation, RxJS leaks and subscription/timer/router cleanup, missing `@for` `track`, large DOM/heavy lists, bundle/chunk cost, duplicate API calls, route lazy-loading, and slow Vitest runs. Inspects only the changed frontend scope unless a wider audit is requested.
- **Best used when:** the user **explicitly** asks for an Angular/frontend performance review.
- **Do not use when:** you want general/engineering/UI-design review, or backend perf (`performance-backend-review`). Triggers only on explicit frontend-perf intent.
- **Output:** evidence-based findings, severities, recommendations — **never code fixes.**

### 2.10 `backend-global-usings-cleanup` — C# global-usings consolidation (action skill)

- **Purpose:** the one **action** skill here (it edits code) — cleans up and consolidates C# global usings across the backend projects. Promotes only common, layer-safe, non-feature-specific namespaces that repeat in **more than five files** in the same project into that project's `GlobalUsings.cs`, removes now-redundant per-file usings, respects Clean Architecture layer boundaries from `BACKEND_STRUCTURE.md`, and verifies with `dotnet build`.
- **Best used when:** the same imports repeat across many files in a Backend project, or a `GlobalUsings.cs` is sprawling/missing.
- **Do not use when:** adding a single using, cleaning frontend/TypeScript imports, or touching C# `using` resource/disposal statements. Does **not** edit `BACKEND_STRUCTURE.md`.
- **Review-only:** ❌ no — it modifies backend code (usings only), then builds to verify.

---

## Invocation & Reading Behavior

What is **auto-loaded**, what is **manually invoked**, what is **conditionally read**,
and what is **reference-only**. Use this to know when each item actually comes into play.

| Item / Path | Category | Auto or Manual? | Trigger / When used | Who reads it | Notes |
|-------------|----------|-----------------|---------------------|--------------|-------|
| `CLAUDE.md` | Entry-point / auto-loaded context | **Auto** | Session start (Claude) | Claude Code | Points to principles, the clean-code & test-code self-checks, design context. |
| `AGENTS.md` | Entry-point / auto-loaded context | **Auto** | Session start (non-Claude agents) | Codex / OpenCode / etc. | Mirror of `CLAUDE.md` for other agents. |
| `CODING_PRINCIPLES.md` | Required project principle | **Mandated read** (not auto-injected) | Before any implementation or review | Every agent; **always** read by `engineering-review` & `backend-structure-review` | Core principles incl. **Quranic Data Safety**. Required by the entry-point files. |
| `TESTING_STRATEGY.md` | Required project policy | **Mandated read** (not auto-injected) | Before selecting/running tests or judging verification evidence | Every agent; `engineering-review`, `test-guard`, `pr-context-prep` | Tiered test execution (A–E), pipeline-trigger rules, release canonical gate. Controls test *selection*; test *quality* stays with `test-guard`. |
| `PRODUCT.md` | Conditional architecture doc | **Conditional** | Only when product / user-facing behavior is involved | Anyone doing product/UX/UI work | Product context (not under `.architecture`). **Not needed for backend-only work unless user-facing behavior is affected.** |
| `DESIGN.md` | Conditional architecture doc | **Conditional** | Only for UI / visual work | Anyone doing UI work | Design direction (seed). For concrete tokens/classes use `UI_STYLE_SYSTEM.md`. **Not for backend-only work** unless user-facing. |
| Backend `.architecture/*` (`BACKEND_STRUCTURE.md`, `CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md`) | Conditional architecture doc | **Conditional** | When backend files in the relevant area change | `engineering-review`, `backend-structure-review` (path-based) | **Not read on every command** — read only when the touched area matches. |
| Frontend `.architecture/*` (`FRONTEND_STRUCTURE.md`, `UI_STYLE_SYSTEM.md`, `API_INTEGRATION_GUIDELINES.md`) | Conditional architecture doc | **Conditional** | When frontend files in the relevant area change | `engineering-review` (path-based) | **Not read on every command** — read only when the touched area matches. |
| `engineering-review` | Manually invoked skill | **Manual** | Post-implementation review; "is implementation quality engineering-ready?" | Invoked on request | **Normally manually requested.** Primary holistic review; review-only; does not judge Git staging or untracked-file risk. |
| `SPEC_KIT_IMPLEMENTATION_REVIEW.md` | Conditional skill add-on | **Conditional** (inside engineering-review) | When the change came from Spec Kit (Phase / US / task IDs / `specs/<feature>/`) | Read by `engineering-review` | Extends engineering-review; **not standalone.** |
| `references/clean-code-guard/*` | Reference-only pack | **Never invoked** (read on demand) | During deep clean-code review or the clean-code self-check | `engineering-review`; the clean-code self-check | **Never a skill** — no triggers. Reference material only. |
| `test-guard` | Manually invoked skill (also applied by engineering-review) | **Manual** for test-only; **Conditional** within engineering-review | Write/add/edit tests, or test-only review; engineering-review applies it **only when a mixed diff contains test files** | Invoked on request; referenced by `engineering-review` | **Explicitly requested** for test-only reviews. Test-code quality only. |
| `backend-structure-review` | Manually invoked skill | **Manual (explicit)** | Focused backend placement / layering / foldering questions | Invoked on request | **Normally explicitly requested.** Not the full review gate. |
| `commit-workflow` | Git workflow skill | **Manual** | Monorepo commit / stage / push planning | Invoked on request | Path-aware focused commits; no destructive git. |
| `deploy-smoke` | Runtime smoke skill | **Manual** | After a migration / before review or PR / after dep or perf change | Invoked on request | Report-only; local DB only; never drops/resets a DB. |
| `pr-context-prep` | PR-prep skill | **Manual** | About to open a PR | Invoked on request | Prep-only; never edits/commits/opens the PR; path-group classification. |
| `dependency-audit` | Security-audit skill | **Manual** | Vuln/staleness check; CVE reaction; before a bump | Invoked on request | Audit-first; no upgrades unless asked; no default major bump. |
| `performance-backend-review` | Perf-audit skill | **Manual (explicit)** | Explicit backend/DB performance review only | Invoked on request | Review-only; changed scope; never code fixes. Not the general gate. |
| `performance-angular-review` | Perf-audit skill | **Manual (explicit)** | Explicit Angular/frontend performance review only | Invoked on request | Review-only; changed scope; never code fixes. Not the general gate. |
| `backend-global-usings-cleanup` | Action skill | **Manual** | Repeated imports / sprawling `GlobalUsings.cs` in a Backend project | Invoked on request | **Edits code** (usings only); verifies with `dotnet build`. |
| Spec Kit skills (`speckit-*`: specify, clarify, plan, tasks, analyze, implement, …) | Spec Kit command | **Manual** (user-invoked slash commands) | Feature spec → clarify → plan → tasks → analyze → implement lifecycle | User invokes | 14 commands; see workflow §5A. |

### Practical rule of thumb

- **Normal implementation:** follow `AGENTS.md`/`CLAUDE.md` + `CODING_PRINCIPLES.md` + the relevant architecture docs for the area you touch.
- **Completed-implementation review:** ask for `engineering-review`.
- **Backend folder/layer uncertainty:** ask for `backend-structure-review`.
- **Test-only review:** ask for `test-guard`.
- **Commit planning:** ask for `commit-workflow`.
- **Does it still build/run?** ask for `deploy-smoke` (after a migration, before a PR).
- **Opening a PR:** ask for `pr-context-prep` (scope, risk, reviewer/CodeRabbit focus).
- **Are our packages safe/current?** ask for `dependency-audit`.
- **Backend/frontend feels slow:** ask (explicitly) for `performance-backend-review` / `performance-angular-review`.
- **Imports repeated across a Backend project:** ask for `backend-global-usings-cleanup`.

---

## 3. Backend architecture docs

Location: `Backend/.architecture/`. These are the **canonical** backend rules; skills cite them rather than restating.

| File | Governs | Read it when | Example tasks |
|------|---------|--------------|---------------|
| `BACKEND_STRUCTURE.md` | File/folder placement, **feature/domain (bounded-context) organization**, GlobalUsings placement, file-size/responsibility thresholds. | Adding/moving backend files or folders; deciding where a type belongs; any size/responsibility question. | "Where should `WordSortBy` live?"; "Is this service oversized?"; "Add a new feature folder." |
| `CLEAN_ARCHITECTURE.md` | Layer responsibilities, **dependency direction**, request/use-case flow, where business logic vs data access belongs. | Adding handlers/services/repositories; wiring DI; anything crossing Domain/Application/Infrastructure/Api. | "Does Application depend on Infrastructure here?"; "Where does this business rule go?" |
| `API_GUIDELINES.md` | API boundary & endpoint behavior, **`ApiResponse` response shape**, localization/messages (Arabic default), validation, health checks, Swagger/OpenAPI, middleware/configuration, error shape. | Adding/changing endpoints, controllers, middleware, response envelopes, or API messages. | "Review this endpoint's response shape"; "Is this error leaking internals?" |

**Backend projects:** `api/QuranDashboard.Api`, `domain/QuranDashboard.Domain`, `application/QuranDashboard.Application.Abstractions`, `application/QuranDashboard.Application`, `infrastructure/QuranDashboard.Infrastructure`, `shared/QuranDashboard.Shared`.

**Relationship to skills:** `backend-structure-review` and `engineering-review` both read these path-based. The docs are the source of truth; the skills apply them. `test-guard/references/dotnet.md` aligns with `API_GUIDELINES.md` (assert the `ApiResponse` envelope) and `CLEAN_ARCHITECTURE.md` (test behavior, not call chains).

---

## 4. Frontend architecture docs

Location: `Frontend/quran-dashboard-ui/.architecture/`. Canonical frontend rules.

| File | Governs | Read it when | Example tasks |
|------|---------|--------------|---------------|
| `FRONTEND_STRUCTURE.md` | Feature folder structure, **routeable smart/page components**, child/presentational components, file-size thresholds, URL state for important tabs, avoiding oversized page components. | Adding/moving Angular features, pages, or components; routing/URL-state decisions. | "Review this feature folder layout"; "Should this tab be in the URL?" |
| `UI_STYLE_SYSTEM.md` | Centralized design tokens, **`qd-*` classes & CSS variables**, RTL rules, themes (light/dark), typography, accessibility; avoiding one-off component styling. | Any styling/theme/layout/RTL work; adding shared visual classes. | "Review this component's SCSS"; "Is this RTL/contrast-correct?" |
| `API_INTEGRATION_GUIDELINES.md` | **Page → Facade/Store → API Service → Backend** flow, DTO/ViewModel/State separation, `Observable<ApiResponse<T>>`, loading/empty/error states, backend messages, pagination/search/filter URL state, **Quranic data safety in API integration**. | Frontend data-access/state/API work; wiring services/facades; handling `ApiResponse<T>`. | "Review this facade's API handling"; "Are loading/empty/error states explicit?" |

**Note on `DESIGN.md` vs `UI_STYLE_SYSTEM.md`:** `DESIGN.md` (root) is the **direction/north star**; `UI_STYLE_SYSTEM.md` is the **canonical implementation system** (tokens, `qd-*` classes). For concrete styling, follow `UI_STYLE_SYSTEM.md`.

**Relationship to skills:** `engineering-review` reads these path-based for frontend changes (UI style system, structure, API integration, plus `PRODUCT.md`/`DESIGN.md` for product/design calls). `test-guard/references/jest.md` covers the *test* side of frontend code.

---

## 5. Recommended workflows

### A. Before starting a new Spec Kit feature

1. Draft the implementation intent / plan at a high level.
2. `/speckit-specify` — create the spec.
3. `/speckit-clarify` — resolve underspecified areas.
4. `/speckit-plan` — technical context, structure, gates.
5. `/speckit-tasks` — dependency-ordered `tasks.md`.
6. `/speckit-analyze` — cross-artifact consistency check.
7. **Fix critical/high issues before any implementation.**

### B. During implementation

- Implement **by phase/chunk**, not all tasks at once (see §7).
- Follow `AGENTS.md`/`CLAUDE.md` + `CODING_PRINCIPLES.md`.
- Select test commands per `TESTING_STRATEGY.md` — Tier A (focused, changed scope) for ordinary phases; do not run full or pipeline suites unless its triggers require them.
- Read the Backend/Frontend `.architecture/` docs **for the area you're touching** (§3, §4).
- Run the **clean-code self-check before delivery** (in `CLAUDE.md`/`AGENTS.md`; backed by `clean-code-guard` references).
- If writing tests, run the **test-code self-check** (in `CLAUDE.md`/`AGENTS.md`; backed by `test-guard`).

### C. After implementing a phase

- Run `engineering-review`.
- **Explicitly state** if it was implemented from Spec Kit, and include the **phase/tasks** and the `specs/<feature>/` path so the Spec Kit compliance module activates.
- Fix **only the review findings** before moving on (keep scope focused).

### D. For backend structure uncertainty

- Use `backend-structure-review`. Examples:
  - "Where should this enum live?"
  - "Review this proposed backend folder structure."
  - "Did these new backend folders violate Clean Architecture?"

### E. For tests

- **Test-only change?** Use `test-guard` directly.
- **Mixed feature change?** Use `engineering-review`; it applies `test-guard` **only to the test files** in the diff and keeps the overall verdict.

### F. For commits

- Use `commit-workflow`.
- Inspect and commit once from the monorepo root; split only by coherent concern.
- Stage **explicit paths**; avoid broad `git add .`/`-A` unless explicitly safe.
- Treat untracked files and commit omission risk as commit-workflow concerns, not engineering-review findings.
- Never commit build outputs, `node_modules`, `bin`/`obj`, `.angular/cache`, or secrets.

### G. Before opening a PR

- Run `deploy-smoke` to confirm the change still builds, migrates, and runs locally.
- Run the pre-PR tier `TESTING_STRATEGY.md` requires — Tier C for an ordinary PR, plus Tier D when the change touches `DataPipelines`, importer tools, canonical resources, pipeline tables/migrations, or shared persistence (`TESTING_STRATEGY.md` §3, §4). If the change touched `Backend/api/` routes, contracts, auth, middleware, or model binding, run the `Tests.Api.*` families and state that **no route-parity gate exists in this tree** — the smoke tier is planned, not active (`TESTING_STRATEGY.md` §13). There is no CI (`TESTING_STRATEGY.md` §8), so nothing runs these for you.
- Then run `pr-context-prep` to package scope, invariants, evidence, and reviewer/CodeRabbit focus.
- Open the PR with `commit-workflow` for staging/commits; for unsquashed subtree-import PRs, use GitHub's **merge commit** strategy.

### H. Performance & dependency audits (explicit, review-only)

- **Backend/DB feels slow?** Explicitly invoke `performance-backend-review`.
- **Angular UI feels slow/janky?** Explicitly invoke `performance-angular-review`.
- **Checking packages for vulnerabilities/staleness?** Use `dependency-audit`; run `deploy-smoke` after any approved bump.
- These are findings-only; they do not apply fixes.

---

## 6. Decision matrix

| Task / Question | Use this skill/doc | Why |
|-----------------|--------------------|-----|
| "Review Phase 3 implementation from Spec Kit" | `engineering-review` + `SPEC_KIT_IMPLEMENTATION_REVIEW.md` | Holistic review + phase/task/contract compliance. |
| "Review only new test files" | `test-guard` | Narrow test-code quality gate. |
| "Which tests must I run for this change?" | `TESTING_STRATEGY.md` | Tiered test selection (A–E) by changed scope and pipeline triggers. |
| "Where should `WordSortBy` enum live?" | `backend-structure-review` + `BACKEND_STRUCTURE.md` | Placement/foldering question. |
| "Review API endpoint response shape" | `engineering-review` + `API_GUIDELINES.md` | API boundary & `ApiResponse` envelope. |
| "Review Angular feature folder layout" | `engineering-review` + `FRONTEND_STRUCTURE.md` | Frontend structure/routeable pages. |
| "Review component styling / RTL / theme" | `engineering-review` + `UI_STYLE_SYSTEM.md` | Tokens, `qd-*` classes, RTL, a11y. |
| "Review facade/API data flow & states" | `engineering-review` + `API_INTEGRATION_GUIDELINES.md` | Page→Facade→Service flow, `ApiResponse<T>`, states. |
| "Commit Backend + Frontend changes safely" | `commit-workflow` | Monorepo-aware grouping and safe explicit staging. |
| "Does this change still build / migrate / run?" | `deploy-smoke` | Local build + migrate-check + runtime smoke; report-only. |
| "Prepare / write up a PR before opening it" | `pr-context-prep` | Scope, invariants, evidence, reviewer/CodeRabbit focus, merge-readiness. |
| "Are our NuGet/npm packages vulnerable or stale?" | `dependency-audit` | Direct vs transitive advisories + smallest safe remediation. |
| "This backend endpoint / query is slow" | `performance-backend-review` | Explicit EF/DB/N+1/index/payload perf audit; findings-only. |
| "This Angular page is slow / re-renders too much" | `performance-angular-review` | Explicit change-detection/Signals/RxJS/bundle perf audit; findings-only. |
| "Same imports repeated across a Backend project" | `backend-global-usings-cleanup` | Consolidates layer-safe global usings; verifies with `dotnet build`. |
| "Deep clean-code issue in implementation" | `engineering-review` using `references/clean-code-guard/*` | Deep naming/SOLID/DRY/AI-failure-mode checks. |
| "Is this layer dependency allowed?" | `backend-structure-review` + `CLEAN_ARCHITECTURE.md` | Dependency direction/layering. |
| "Start a new feature" | `speckit-*` chain (§5A) | Spec → clarify → plan → tasks → analyze. |

---

## 7. Anti-patterns

- ❌ **Don't** use `backend-structure-review` as the full code-review gate — it only covers structure/layering/placement. Use `engineering-review` for the holistic gate.
- ❌ **Don't** use `test-guard` for production-code review — it is test-code only.
- ❌ **Don't** promote `clean-code-guard` to a separate skill — it is reference material under `engineering-review` (it would otherwise collide with engineering-review's triggers). Only revisit deliberately.
- ❌ **Don't** read `PRODUCT.md`/`DESIGN.md` for backend-only work unless user-facing behavior is affected.
- ❌ **Don't** run all Spec Kit phases at once unless explicitly approved — implement by phase so the gates do their job.
- ❌ **Don't** let architecture-doc content get duplicated inside skills (or this guide) — cite the canonical doc instead, to avoid drift.
- ❌ **Don't** replace Quranic Data Safety rules with generic testing or clean-code rules — source-safety always applies (production *and* tests) and outranks convenience.

---

## 8. Gaps & next actions

**Inventory check (all present unless noted):**

- Root docs: `CODING_PRINCIPLES.md`, `TESTING_STRATEGY.md`, `PRODUCT.md`, `DESIGN.md`, `AGENTS.md`, `CLAUDE.md` ✅
- Skills: `engineering-review/` (+ `SPEC_KIT_IMPLEMENTATION_REVIEW.md`, `references/clean-code-guard/`), `test-guard/` (+ `dotnet.md`, `jest.md`, `llm-app-testing.md`, `frontend-test-harness-constraints.md`), `backend-structure-review/`, `commit-workflow/`, `deploy-smoke/`, `pr-context-prep/`, `dependency-audit/`, `performance-backend-review/`, `performance-angular-review/`, `backend-global-usings-cleanup/` ✅; plus 14 `speckit-*` skills ✅
- Backend `.architecture/`: `BACKEND_STRUCTURE.md`, `CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md` ✅
- Frontend `.architecture/`: `FRONTEND_STRUCTURE.md`, `UI_STYLE_SYSTEM.md`, `API_INTEGRATION_GUIDELINES.md` ✅

**Gaps found:**

- `DESIGN.md` is still a **seed** doc — its header notes it should be regenerated (`/impeccable document`) once there is real UI code to capture actual tokens/components. Until then, `UI_STYLE_SYSTEM.md` is the operative styling source.
- No workspace-root `README.md` — this guide partly fills the "what is here / how do I work" gap, but a short README pointing newcomers to this guide would help.
- `test-guard` has **no Angular-specific reference** yet (`jest.md` is the closest match). Consider an `angular.md` later only if real Angular test conventions diverge.

**Recommended next action:** keep this guide as the onboarding map; review/update it whenever a skill or `.architecture/` doc is added or materially changed.
