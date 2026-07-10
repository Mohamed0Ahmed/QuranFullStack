# Skills & Architecture Guide — Quran FullStack Workspace (المنهج القرآني)

A single practical map of the custom **skills**, **project-level documents**, and
**architecture reference files** used in this workspace, plus the recommended
workflows for implementation, review, Spec Kit, tests, and commits.

> This guide is an **index and decision aid**, not a replacement for the canonical
> docs. Where it summarizes a rule, the linked file remains the source of truth.
> Do not copy architecture-doc or skill content into this guide; point to it.

_Reflects the workspace as of 2026-07-10._

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
| `PRODUCT.md` | Product strategy & context: register, users (Arabic-speaking admins/supervisors/teachers), product purpose (manage Quran research data, review ayah links, organize gates أبواب, publish), principles, anti-references. | Anyone doing user-facing/product or UI work. | Frontend/UX/product decisions and any backend change that affects user-facing behavior. |
| `DESIGN.md` | Visual/design direction — the "Quiet Scriptorium" north star: Arabic-first RTL, restrained parchment/ink palette, calm typography; explicitly rejects generic SaaS, kitschy religious decor, gamified UI, enterprise greige. Currently a **seed/direction** doc (see §8). | Anyone doing UI/visual work. | Frontend visual/design tasks. For concrete tokens/classes use `UI_STYLE_SYSTEM.md`. |
| `AGENTS.md` | Workspace entrypoint for non-Claude agents (Codex/OpenCode/etc.). Points to project instruction files, coding principles, the clean-code & test-code self-checks, and design context. | Non-Claude coding agents. | Loaded at session start for those agents. |
| `CLAUDE.md` | Same role as `AGENTS.md`, for Claude. Points to `CODING_PRINCIPLES.md`, the self-checks, and design context. | Claude Code. | Loaded at session start. |

**What should NOT be duplicated into these files:**

- Full clean-code / test-guard rule bodies (they live in the skills/references; the docs only *point* to them).
- Architecture rules (those live in the Backend/Frontend `.architecture/` docs).
- Spec Kit per-feature details (those live in `specs/<feature>/`).
- Anything that would create a second, drifting copy of a rule that already has a home.

---

## 2. Current skills

All custom skills live under `.claude/skills/`. There are also **14 `speckit-*` skills** (the Spec Kit command set: `specify`, `clarify`, `plan`, `tasks`, `analyze`, `implement`, `checklist`, `constitution`, the `git-*` helpers, `taskstoissues`) — those are the Spec Kit workflow commands referenced in §5.

### Quick orientation

| Skill | Review-only? | Can implement? | Scope | Relationship to `engineering-review` |
|-------|:---:|:---:|-------|--------------------------------------|
| `engineering-review` | ✅ Yes | ❌ No (unless explicitly asked) | **Primary holistic post-implementation review** | — (it is the hub) |
| `test-guard` | Review **and** write-time guard | ✅ Authors/guards test code | **Test-code quality only** | Called *by* engineering-review for the test-file portion of a diff |
| `backend-structure-review` | ✅ Yes | ❌ No (unless explicitly asked) | **Backend structure / layering / placement** | A focused subset, not a replacement |
| `commit-workflow` | Planning + safe execution | Runs git (no destructive cmds) | **Git tracking, commit ordering & safe staging** | Independent (runs after review) |
| `clean-code-guard` | _Not a skill_ — reference pack | n/a | Deep clean-code references | Lives **inside** engineering-review |

### 2.1 `engineering-review` — the primary holistic review skill

- **Purpose:** the single, holistic **post-implementation** code review for the workspace (.NET backend + Angular frontend). Covers Clean Code, SOLID, DRY/KISS/YAGNI, separation of concerns, backend/frontend architecture, file-size/responsibility thresholds, routeable components & URL state, API integration & `ApiResponse<T>` handling, the UI style system, strong typing, focused scope, error handling, **Quranic Data Safety**, and build/test verification. When the change came from Spec Kit, it **also** applies phase/task/contract compliance.
- **Best used when:** reviewing a change, diff, PR, branch, or a completed Spec Kit phase; deciding whether implementation quality is engineering-ready.
- **Boundary:** it does **not** judge Git staging, commit ordering, push readiness, or untracked-file risk. It reviews implementation content, including untracked files when they are part of the requested scope, but Git tracking/staging state never affects findings, notes, Test Guard verdict, or final verdict.
- **Do not use when:** you only need a narrow structure question answered (use `backend-structure-review`), only test files changed (use `test-guard`), or you want fixes implemented (review is findings-only).
- **Reads / references (path-based, only what changed):**
  - Always: `CODING_PRINCIPLES.md`.
  - Deep clean-code: `.claude/skills/engineering-review/references/clean-code-guard/*`.
  - Backend changed: `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`.
  - Frontend changed: `Frontend/quran-dashboard-ui/.architecture/{FRONTEND_STRUCTURE,UI_STYLE_SYSTEM,API_INTEGRATION_GUIDELINES}.md`, plus `PRODUCT.md`/`DESIGN.md` for UI/product decisions.
  - Test files in the diff: applies `test-guard` (`references/dotnet.md` backend, `references/jest.md` frontend).
  - Spec Kit change: `.claude/skills/engineering-review/SPEC_KIT_IMPLEMENTATION_REVIEW.md` + the relevant `specs/<feature>/{spec,plan,tasks}.md`, `contracts/`, `quickstart.md`.
- **Output:** structured verdict (PASS / PASS WITH NOTES / CHANGES REQUESTED / BLOCKED), scope reviewed, optional Spec Kit compliance section, findings by severity (BLOCKING / MAJOR / MINOR / NOTE), threshold check, architecture/responsibility check, Quranic data safety check, verification check, final recommendation.
- **Review-only:** ✅ yes. **Implements changes:** ❌ no, unless the user explicitly asks for fixes as a separate task.

#### 2.1a `SPEC_KIT_IMPLEMENTATION_REVIEW.md` (add-on, inside engineering-review)

- **Purpose:** extra rules that **extend** engineering-review **only when** the change was implemented from Spec Kit. Verifies phase/task scope (no future-phase leakage, nothing skipped), task→file traceability, Locked-Decisions and Out-of-Scope compliance, contract compliance (`contracts/api-*.md`, `ui-*.md`), acceptance/quickstart verification, scope-creep, and single-source-of-truth derivation.
- **Best used when:** the request mentions a Phase, a User Story/US, task IDs (e.g. `T013–T018`), `specs/<feature>/`, `spec.md`/`plan.md`/`tasks.md`, `contracts/`, or "implemented Phase/tasks".
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

---

## Invocation & Reading Behavior

What is **auto-loaded**, what is **manually invoked**, what is **conditionally read**,
and what is **reference-only**. Use this to know when each item actually comes into play.

| Item / Path | Category | Auto or Manual? | Trigger / When used | Who reads it | Notes |
|-------------|----------|-----------------|---------------------|--------------|-------|
| `CLAUDE.md` | Entry-point / auto-loaded context | **Auto** | Session start (Claude) | Claude Code | Points to principles, the clean-code & test-code self-checks, design context. |
| `AGENTS.md` | Entry-point / auto-loaded context | **Auto** | Session start (non-Claude agents) | Codex / OpenCode / etc. | Mirror of `CLAUDE.md` for other agents. |
| `CODING_PRINCIPLES.md` | Required project principle | **Mandated read** (not auto-injected) | Before any implementation or review | Every agent; **always** read by `engineering-review` & `backend-structure-review` | Core principles incl. **Quranic Data Safety**. Required by the entry-point files. |
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
| Spec Kit skills (`speckit-*`: specify, clarify, plan, tasks, analyze, implement, …) | Spec Kit command | **Manual** (user-invoked slash commands) | Feature spec → clarify → plan → tasks → analyze → implement lifecycle | User invokes | 14 commands; see workflow §5A. |

### Practical rule of thumb

- **Normal implementation:** follow `AGENTS.md`/`CLAUDE.md` + `CODING_PRINCIPLES.md` + the relevant architecture docs for the area you touch.
- **Completed-implementation review:** ask for `engineering-review`.
- **Backend folder/layer uncertainty:** ask for `backend-structure-review`.
- **Test-only review:** ask for `test-guard`.
- **Commit planning:** ask for `commit-workflow`.

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

---

## 6. Decision matrix

| Task / Question | Use this skill/doc | Why |
|-----------------|--------------------|-----|
| "Review Phase 3 implementation from Spec Kit" | `engineering-review` + `SPEC_KIT_IMPLEMENTATION_REVIEW.md` | Holistic review + phase/task/contract compliance. |
| "Review only new test files" | `test-guard` | Narrow test-code quality gate. |
| "Where should `WordSortBy` enum live?" | `backend-structure-review` + `BACKEND_STRUCTURE.md` | Placement/foldering question. |
| "Review API endpoint response shape" | `engineering-review` + `API_GUIDELINES.md` | API boundary & `ApiResponse` envelope. |
| "Review Angular feature folder layout" | `engineering-review` + `FRONTEND_STRUCTURE.md` | Frontend structure/routeable pages. |
| "Review component styling / RTL / theme" | `engineering-review` + `UI_STYLE_SYSTEM.md` | Tokens, `qd-*` classes, RTL, a11y. |
| "Review facade/API data flow & states" | `engineering-review` + `API_INTEGRATION_GUIDELINES.md` | Page→Facade→Service flow, `ApiResponse<T>`, states. |
| "Commit Backend + Frontend changes safely" | `commit-workflow` | Monorepo-aware grouping and safe explicit staging. |
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

- Root docs: `CODING_PRINCIPLES.md`, `PRODUCT.md`, `DESIGN.md`, `AGENTS.md`, `CLAUDE.md` ✅
- Skills: `engineering-review/` (+ `SPEC_KIT_IMPLEMENTATION_REVIEW.md`, `references/clean-code-guard/`), `test-guard/` (+ `dotnet.md`, `jest.md`, `llm-app-testing.md`), `backend-structure-review/`, `commit-workflow/` ✅; plus 14 `speckit-*` skills ✅
- Backend `.architecture/`: `BACKEND_STRUCTURE.md`, `CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md` ✅
- Frontend `.architecture/`: `FRONTEND_STRUCTURE.md`, `UI_STYLE_SYSTEM.md`, `API_INTEGRATION_GUIDELINES.md` ✅

**Gaps found:**

- `DESIGN.md` is still a **seed** doc — its header notes it should be regenerated (`/impeccable document`) once there is real UI code to capture actual tokens/components. Until then, `UI_STYLE_SYSTEM.md` is the operative styling source.
- No workspace-root `README.md` — this guide partly fills the "what is here / how do I work" gap, but a short README pointing newcomers to this guide would help.
- `test-guard` has **no Angular-specific reference** yet (`jest.md` is the closest match). Consider an `angular.md` later only if real Angular test conventions diverge.

**Recommended next action:** keep this guide as the onboarding map; review/update it whenever a skill or `.architecture/` doc is added or materially changed.
