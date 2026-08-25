# Skills & Architecture Guide — Quran FullStack Workspace (المنهج القرآني)

A single practical map of the custom **skills**, **project-level documents**, and
**architecture reference files** used in this workspace, plus the recommended
workflows for implementation, review, Spec Kit, tests, and commits.

> This guide is an **index and decision aid**, not a replacement for the canonical
> docs. Where it summarizes a rule, the linked file remains the source of truth.
> Do not copy architecture-doc or skill content into this guide; point to it.

_Reflects the workspace as of 2026-08-09._

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
   - [Ownership summary](#ownership-summary)
   - [Review terminology](#review-terminology)
   - [Reference inventory](#reference-inventory)
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
| `CODING_PRINCIPLES.md` | General coding principles for the whole workspace: Clean Code, SOLID, DRY/KISS/YAGNI, separation of concerns, strong typing, focused changes, error handling, testing/verification, **Quranic Data Safety**, UI/product consistency, Definition of Done. Also points to the retained `clean-code-guard` references and the `test-guard` skill. | Every agent/human before implementation; the review skills load only its implicated headings. | All implementation and review work. |
| `TESTING_CONSTITUTION.md` | The sole testing-policy authority, including the Test Freeze; Backend lane mechanics live in the Backend test README and browser-journey mechanics live in the E2E README. | Every agent/human before selecting, writing, running, or judging verification. | Whenever tests or verification evidence are in scope. |
| `PRODUCT.md` | Product context: register, users, purpose, product voice, functional invariants, and Quran safety during the UI rebuild. | Anyone doing user-facing/product or UI work. | Frontend/UX/product decisions and any backend change that affects user-facing behavior. |
| `DESIGN.md` | Transitional UI-rebuild status. It records that no permanent design rules are active and that final rules will be extracted after owner approval of the complete interface. | Anyone doing UI/visual work. | Frontend visual work during the rebuild. The owner's explicit phase direction controls. |
| `AGENTS.md` | Sol/Codex-native workspace router with the universal safety kernel, native area routes, and trigger-scoped pointers. It does not route through Claude entrypoints. | Sol/Codex coding agents. | Loaded at session start for those agents. |
| `CLAUDE.md` | Claude-native workspace router with the equivalent universal safety kernel, Claude area routes, and trigger-scoped pointers. It does not route through Sol/Codex entrypoints. | Claude Code. | Loaded at session start. |

**What should NOT be duplicated into these files:**

- Full clean-code / test-guard rule bodies (they live in the skills/references; the docs only *point* to them).
- Architecture rules (those live in the Backend/Frontend `.architecture/` docs).
- Spec Kit per-feature details (those live in the active feature's `specs/<feature>/`; implemented steady-state truth is code, indexed by `docs/contracts/` where a pointer is useful).
- Anything that would create a second, drifting copy of a rule that already has a home.

---

## 2. Current skills

All custom skills live under `.claude/skills/` — **`ls .claude/skills/` is the roster**. They
fall into two families: the 11 workspace skills summarized below, and the **`speckit-*`** family
(the Spec Kit command set referenced in §5: `specify`, `clarify`, `plan`, `tasks`, `analyze`,
`implement`, `converge`, `checklist`, `constitution`, `taskstoissues`, and the `git-*` helpers).

Thin adapters for non-Claude runtimes live under `.agents/skills/`: each adapter carries matching
discovery metadata and points to the canonical `.claude/skills/<name>/SKILL.md`, which every
runtime reads in full and follows. The `speckit-*` sets differ between the two trees — diff the
directories before assuming a Spec Kit skill is available to whichever runtime you are in.

### Ownership summary

One Skill owns one result. A Skill may inspect the evidence needed to produce that result, but it
never adds another Skill's build, test, review, fix, Git, PR, performance, dependency, or
deployment stage — and **no project Skill automatically invokes another project Skill**. All 11
are explicitly invoked, and neither review Skill invokes the other or any project Skill. Each
canonical `SKILL.md` states its own responsibility,
non-responsibilities, conditional context, and output contract; this table is the index, not a
second rule source.

| Skill | Owns | Never owns |
|-------|------|------------|
| `engineering-review` | The explicitly requested formal review at any boundary — normally once at the completed feature/change boundary: findings with stable IDs (`ER-1`, `ER-2`, ...) + verdict, plus the re-review path that marks prior findings `CLOSED`/`OPEN`/`REGRESSED`. Consumes supplied final evidence, including an existing same-diff Test Guard result; reports Test Guard evidence missing only when the active plan/spec/contract explicitly requires that separate evidence. | Fixes, builds/tests, Git/PR/deploy, dependency/performance audits, unrequested review, invoking other Skills (including `test-guard` and `focused-review`). |
| `focused-review` | The explicitly requested scoped review of one phase, task, fix, selected file set, or explicit architecture/security/data-safety checkpoint: frozen scope, scoped findings (`CLEAR`/`FINDINGS` — not a formal verdict), explicit exclusions; may observe supplied checkpoint evidence. | Scope expansion (files → branch, phase → feature, checkpoint → final readiness), formal verdicts, final evidence sufficiency, verification runs, fixes, Git/PR/deploy, invoking other Skills, the Spec Kit formal add-on. |
| `test-guard` | Test-code quality guidance/review against its nine rules; its result is evidence the formal review consumes. | Production review, test selection/execution, evidence-sufficiency verdicts, test fixes, Git. |
| `backend-structure-review` | Explicitly requested backend placement/layer/file-responsibility advice or focused findings. | Auto-firing on ordinary new files, holistic review, fixes, builds, tests, Git. |
| `commit-workflow` | The explicitly requested Git operation (branch/stage/commit/push/PR-open/sync) plus its Git-integrity checks. | Builds, tests, review, deploy, fixes, automatic PR prep. |
| `deploy-smoke` | The explicitly requested deployment preflight / local runtime smoke; may build only a missing targeted deployable artifact the smoke needs. | Proactive pre-review/pre-PR gates, test lanes, installs, Git, remote deploy, destructive data or unapproved migration action. |
| `pr-context-prep` | The copy-paste PR context package built from the branch diff and existing evidence. | File writes, Git/PR mutation, evidence reruns, formal review, an independent merge-readiness verdict, fixes. |
| `dependency-audit` | The NuGet/npm vulnerability/staleness scan and report, with remediation options. | Package/lock edits, restore/build/test/smoke, advisory suppression, Git. |
| `performance-backend-review` | Evidence-based backend/EF/PostgreSQL performance review (explicit invocation; read-only measurement allowed). | General/architecture/test-quality review, speculation, mutation, fixes. |
| `performance-angular-review` | Evidence-based frontend render/state/DOM/network/bundle/test-runtime performance review (explicit invocation). | General/accessibility/test-quality review, speculation, mutation, fixes. |
| `backend-global-usings-cleanup` | Import-only C# global-usings consolidation (the `>5`-files rule) plus focused compilation of affected projects. | Tests, reviews, broad refactors, docs, Git. |

#### Review terminology

Use neutral technical descriptions in review output: **monolithic**, **overloaded**,
**multi-responsibility service**, **oversized service/component/store**. Do not use religious
terminology such as "God service". This heading is the canonical home of that rule;
`engineering-review` and `backend-structure-review` point here.

### Reference inventory

| Location | Contents |
|----------|----------|
| `.claude/skills/engineering-review/SPEC_KIT_IMPLEMENTATION_REVIEW.md` | Conditional add-on rules applied only when the reviewed change came from Spec Kit. |
| `.claude/skills/engineering-review/references/clean-code-guard/` | `ai-failure-modes.md` (AI-specific review failure modes) and `review-checklist.md` (optional deep-review traversal aid). Canonical clean-code principles stay in `CODING_PRINCIPLES.md` §§2–4 and §7. |
| `.claude/skills/engineering-review/references/quran-data-safety.md` | Small conditional cross-area Quran safety reference; the canonical authority is `CODING_PRINCIPLES.md` §10 plus the implicated source and renderer code. |
| `.claude/skills/test-guard/references/` | `dotnet.md` (.NET/xUnit applications of the nine rules). Playwright mechanics live in `Frontend/quran-dashboard-ui/e2e/README.md`. |

---

## 3. Backend architecture docs

Location: `Backend/.architecture/`. These are the **canonical** backend rules; skills cite them rather than restating.

| File | Governs | Read it when | Example tasks |
|------|---------|--------------|---------------|
| `BACKEND_STRUCTURE.md` | File/folder placement, **feature/domain (bounded-context) organization**, GlobalUsings placement, file-size/responsibility thresholds. | Adding/moving backend files or folders; deciding where a type belongs; any size/responsibility question. | "Where should `WordSortBy` live?"; "Is this service oversized?"; "Add a new feature folder." |
| `CLEAN_ARCHITECTURE.md` | Layer responsibilities, **dependency direction**, request/use-case flow, where business logic vs data access belongs. | Adding handlers/services/repositories; wiring DI; anything crossing Domain/Application/Infrastructure/Api. | "Does Application depend on Infrastructure here?"; "Where does this business rule go?" |
| `API_GUIDELINES.md` | API boundary & endpoint behavior, **`ApiResponse` response shape** (§5), localization/messages (Arabic default), validation, health checks, Swagger/OpenAPI, middleware/configuration, error shape, **security and safety** (§11). | Adding/changing endpoints, controllers, middleware, response envelopes, or API messages. | "Review this endpoint's response shape"; "Is this error leaking internals?" |

**Backend projects:** `api/QuranDashboard.Api`, `domain/QuranDashboard.Domain`, `application/QuranDashboard.Application.Abstractions`, `application/QuranDashboard.Application`, `infrastructure/QuranDashboard.Infrastructure`, `shared/QuranDashboard.Shared`.

**Relationship to skills:** the review skills read these path-based, for the exact implicated
headings. The docs are the source of truth; the skills apply them.

---

## 4. Frontend architecture docs

Structural and API authorities live under `Frontend/quran-dashboard-ui/.architecture/`.
Current UI behavior and styling are owned by the Angular implementation while the visual rebuild is
in progress.

| File | Governs | Read it when | Example tasks |
|------|---------|--------------|---------------|
| `FRONTEND_STRUCTURE.md` | Feature folder structure, **routeable smart/page components**, child/presentational components, file-size thresholds, URL state for important tabs, avoiding oversized page components. | Adding/moving Angular features, pages, or components; routing/URL-state decisions. | "Review this feature folder layout"; "Should this tab be in the URL?" |
| `API_INTEGRATION_GUIDELINES.md` | **Page → Facade/Store → API Service → Backend** flow, DTO/ViewModel/State separation, `Observable<ApiResponse<T>>`, loading/empty/error states, backend messages, pagination/search/filter URL state, **Quranic data safety in API integration**. | Frontend data-access/state/API work; wiring services/facades; handling `ApiResponse<T>`. | "Review this facade's API handling"; "Are loading/empty/error states explicit?" |
| `src/app/core/layout/`, `src/app/core/navigation/`, `src/styles/` | Current shell, navigation, tokens, and shared styling implementation. These paths are implementation truth, not a permanent visual authority during the rebuild. | UI implementation or review in the directly implicated area. | "How does the current shell behave?"; "Where is this shared token implemented?" |

`DESIGN.md` is a transition note during the rebuild. Visual decisions follow the owner's active
phase direction, and current behavior is verified against the implicated Angular implementation.

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
- Follow your native root and area routers, the active feature artifacts when applicable, and only the triggered headings of `CODING_PRINCIPLES.md` and architecture authorities.
- Use `TESTING_CONSTITUTION.md` and the active plan's `Testing Decision` to select verification;
  the Backend test README and E2E README supply only operational commands and fixtures.
- Read the Backend/Frontend `.architecture/` docs **for the area you're touching** (§3, §4).
- Before delivery, read `CODING_PRINCIPLES.md` §12 and the production-code headings already implicated; do not load the full `clean-code-guard` pack or run a formal review unless requested.
- If writing tests, use the native `test-guard` Skill's rules and its stack-relevant reference.

### C. Review checkpoints and the formal final review

- A normal phase continues after its `Testing Decision` verification — no review Skill
  runs by default, and no arrow in this workflow is an automatic Skill invocation.
- **Optional focused checkpoint (explicit request):** ask for `focused-review` when one slice
  deserves early scrutiny — a migration/schema foundation, auth boundary, Quran
  source/import/persistence integrity, transaction/rollback/audit boundary, major public API
  foundation, or another risky checkpoint. It returns scoped findings only; fixes are separate,
  explicitly requested implementation.
- **Formal final review (explicit request):** after the completed feature/change and any
  pre-review fixes settle, implementation runs the fresh cumulative final union, then ask for
  `engineering-review`. **Explicitly state** if it was implemented from Spec Kit, and include
  the **phase/tasks** and the `specs/<feature>/` path so the Spec Kit compliance module
  activates. An explicitly requested earlier formal review uses the same contract as a
  deliberate override.
- **After formal findings:** fix only the review findings as separate implementation (with
  focused/protected verification while fixes are in motion), rerun the whole final union once
  after fixes settle, then ask for the `engineering-review` re-review — same reviewer session
  when practical. Original finding IDs return `CLOSED`, `OPEN`, or `REGRESSED`; the Skill owns
  the fresh-full-review fallback conditions.
- If the change touched test files and you want a review to consume a Test Guard result, invoke
  `test-guard` separately first; each review consumes an existing same-diff result and never
  invokes `test-guard` itself.

### D. For backend structure uncertainty

- Use `backend-structure-review`. Examples:
  - "Where should this enum live?"
  - "Review this proposed backend folder structure."
  - "Did these new backend folders violate Clean Architecture?"

### E. For tests

- **Test-only change?** Ask for `test-guard` directly.
- **Mixed feature change?** At the formal boundary, ask for `engineering-review`; it consumes an existing same-diff `test-guard` result when supplied, and reports that evidence missing only when the active plan/spec/contract explicitly requires it. It does not invoke `test-guard` itself — request the skills separately when you want both.

### F. For commits

- Use `commit-workflow` for the explicitly requested Git operation.
- Inspect and commit once from the monorepo root; split only by coherent concern.
- Stage **explicit paths**; avoid broad `git add .`/`-A` unless explicitly safe.
- Treat untracked files and commit omission risk as commit-workflow concerns, not engineering-review findings.
- Never commit build outputs, `node_modules`, `bin`/`obj`, `.angular/cache`, or secrets.

### G. Before opening a PR

- Confirm implementation already produced the fresh evidence selected by `TESTING_CONSTITUTION.md`
  and the active plan's `Testing Decision`. Use the retained Backend test and scripts READMEs for
  route-smoke and contract-command mechanics when those checks were selected. Missing or stale evidence
  returns to implementation; there is no CI, and PR preparation only packages what actually ran.
- Optionally ask for `deploy-smoke` (explicit request) when you want a local runtime smoke, and `engineering-review` for the formal gate.
- Then ask for `pr-context-prep` to package scope, invariants, evidence, and reviewer/CodeRabbit focus from what already ran.
- Use `commit-workflow` for the Git/PR execution; for unsquashed subtree-import PRs, use GitHub's **merge commit** strategy.

### H. Performance & dependency audits (explicit, review-only)

- **Backend/DB feels slow?** Explicitly invoke `performance-backend-review`.
- **Angular UI feels slow/janky?** Explicitly invoke `performance-angular-review`.
- **Checking packages for vulnerabilities/staleness?** Use `dependency-audit`; ask for `deploy-smoke` separately after any approved bump.
- These are findings-only; they do not apply fixes.

---

## 6. Decision matrix

| Task / Question | Use this skill/doc | Why |
|-----------------|--------------------|-----|
| "Review Phase 3 only" / "Review these three changed files only" | `focused-review` | Scoped findings on the named slice; no formal verdict or feature expansion. |
| "Review this auth/schema/Quran foundation checkpoint" | `focused-review` | Explicit high-risk checkpoint; loads only the implicated security/Quran owners. |
| "Run a formal engineering review of Phase 2 now" | `engineering-review` | Explicitly requested earlier formal review — deliberate override, same contract. |
| "Run the formal engineering review for the completed Spec Kit feature" | `engineering-review` + `SPEC_KIT_IMPLEMENTATION_REVIEW.md` | Formal final review + phase/task/contract compliance. |
| "We fixed all formal review findings; re-review them" | `engineering-review` (re-review path) | Finding closure: prior IDs return `CLOSED`/`OPEN`/`REGRESSED` against fresh final evidence. |
| "Review only new test files" | `test-guard` | Narrow test-code quality gate. |
| "Which tests must I run for this change?" | `TESTING_CONSTITUTION.md` + the active plan's `Testing Decision` | The constitution is the policy authority and each plan records the risk-specific selection. |
| "Where should `WordSortBy` enum live?" | `backend-structure-review` + `BACKEND_STRUCTURE.md` | Placement/foldering question. |
| "Review API endpoint response shape" | `focused-review` + `API_GUIDELINES.md` | Scoped API-boundary checkpoint & `ApiResponse` envelope. |
| "Review Angular feature folder layout" | `focused-review` + `FRONTEND_STRUCTURE.md` | Scoped frontend structure/routeable-pages checkpoint. |
| "Review component styling / RTL / theme" | `focused-review` + the implicated Angular templates/styles | Scoped checkpoint against the owner's active direction, current behavior, RTL, and accessibility. |
| "Review facade/API data flow & states" | `focused-review` + `API_INTEGRATION_GUIDELINES.md` | Scoped checkpoint: Page→Facade→Service flow, `ApiResponse<T>`, states. |
| "Commit Backend + Frontend changes safely" | `commit-workflow` | Monorepo-aware grouping and safe explicit staging. |
| "Does this change still build / migrate / run?" | `deploy-smoke` (explicit request) | Local preflight + runtime smoke; report-only. |
| "Prepare / write up a PR before opening it" | `pr-context-prep` | Scope, invariants, evidence, reviewer/CodeRabbit focus. |
| "Are our NuGet/npm packages vulnerable or stale?" | `dependency-audit` | Direct vs transitive advisories + smallest safe remediation options. |
| "This backend endpoint / query is slow" | `performance-backend-review` | Explicit EF/DB/N+1/index/payload perf audit; findings-only. |
| "This Angular page is slow / re-renders too much" | `performance-angular-review` | Explicit change-detection/Signals/RxJS/bundle perf audit; findings-only. |
| "Same imports repeated across a Backend project" | `backend-global-usings-cleanup` | Consolidates layer-safe global usings; focused compile verification. |
| "Deep clean-code issue in implementation" | `engineering-review` using `references/clean-code-guard/*` | AI failure modes + optional traversal aid over `CODING_PRINCIPLES.md` §§2–4, §7. |
| "Is this layer dependency allowed?" | `backend-structure-review` + `CLEAN_ARCHITECTURE.md` | Dependency direction/layering. |
| "Start a new feature" | `speckit-*` chain (§5A) | Spec → clarify → plan → tasks → analyze. |

---

## 7. Anti-patterns

- ❌ **Don't** use `backend-structure-review` as the full code-review gate — it only covers structure/layering/placement. Use `engineering-review` for the formal holistic gate.
- ❌ **Don't** escalate a generic narrow review request ("review this phase / task / fix / these files") to `engineering-review` — that is `focused-review`'s result. `engineering-review` requires an explicit formal request ("formal"/"final" or the Skill named).
- ❌ **Don't** run a formal review by default after every phase — a normal phase continues after its `Testing Decision` verification; high-risk checkpoints use an explicitly requested `focused-review`.
- ❌ **Don't** let `focused-review` expand its scope, judge final evidence sufficiency, or issue a formal verdict — `CLEAR`/`FINDINGS` closes no final boundary.
- ❌ **Don't** use `test-guard` for production-code review — it is test-code only.
- ❌ **Don't** chain project skills automatically — no project Skill invokes another, and neither review Skill invokes the other. The caller sequences them.
- ❌ **Don't** promote `clean-code-guard` to a separate skill — it is reference material under `engineering-review` (it would otherwise collide with engineering-review's triggers). Only revisit deliberately.
- ❌ **Don't** read `PRODUCT.md`/`DESIGN.md` for backend-only work unless user-facing behavior is affected.
- ❌ **Don't** run all Spec Kit phases at once unless explicitly approved — implement by phase so the gates do their job.
- ❌ **Don't** let architecture-doc content get duplicated inside skills (or this guide) — cite the canonical doc instead, to avoid drift.
- ❌ **Don't** replace Quranic Data Safety rules with generic testing or clean-code rules — source-safety always applies (production *and* tests) and outranks convenience.

---

## 8. Gaps & next actions

**Inventory check (all present unless noted):**

- Root docs: `CODING_PRINCIPLES.md`, `TESTING_CONSTITUTION.md`, `PRODUCT.md`, `DESIGN.md`, `AGENTS.md`, `CLAUDE.md` ✅
- Skills: `engineering-review/` (+ `SPEC_KIT_IMPLEMENTATION_REVIEW.md`, `references/clean-code-guard/` with `ai-failure-modes.md` + `review-checklist.md`, `references/quran-data-safety.md`), `focused-review/` (self-contained, no reference pack), `test-guard/` (+ `dotnet.md`), `backend-structure-review/`, `commit-workflow/`, `deploy-smoke/`, `pr-context-prep/`, `dependency-audit/`, `performance-backend-review/`, `performance-angular-review/`, `backend-global-usings-cleanup/` ✅; plus the `speckit-*` family ✅
- Backend `.architecture/`: `BACKEND_STRUCTURE.md`, `CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md` ✅
- Frontend authorities: `.architecture/FRONTEND_STRUCTURE.md`, `.architecture/API_INTEGRATION_GUIDELINES.md`; current UI implementation under `src/app/core/layout/`, `src/app/core/navigation/`, and `src/styles/` ✅

**Recommended next action:** keep this guide as the onboarding map; review/update it whenever a skill or `.architecture/` doc is added or materially changed.
