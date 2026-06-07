---
name: engineering-review
description: >-
  Review-only engineering/code review for the Quran Dashboard FullStack workspace
  (.NET backend + Angular frontend). Use this skill whenever the user asks to
  review code, a diff, a PR, a branch, or a change, or asks whether a change is
  ready to merge, follows the coding principles, or respects the architecture,
  even if they don't say the word "review". It reads the relevant architecture
  docs path-based and checks Clean Code, SOLID, DRY/KISS/YAGNI, separation of
  concerns, backend/frontend architecture, file size and responsibility
  thresholds, routeable components and URL state, API integration and
  ApiResponse<T> handling, the UI style system, strong typing, focused scope,
  error handling, Quranic data safety, and build/test/report verification, then
  returns a structured verdict with severity levels. When the change was implemented
  from Spec Kit artifacts (the request mentions a Phase, a User Story/US, task IDs
  like T013–T018, specs/<feature>/, spec.md, plan.md, tasks.md, contracts/,
  quickstart.md, or "implemented Phase/tasks"), it additionally verifies the code
  against the active spec, phase/task scope, locked decisions, and contracts. This is
  a review skill only: produce findings and recommendations; do not implement fixes
  unless the user explicitly asks.
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

If the user wants fixes after the review, that is a separate, explicitly requested
task.

## Required Context / Reading Rules (path-based)

Read only the docs relevant to what actually changed. Do not require reading tool
entrypoint files (Claude/OpenCode/Codex each load their own); rely on the loaded
context for those.

**Always read:**

- `CODING_PRINCIPLES.md`

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
- the relevant files under `specs/<feature>/contracts/`
- `specs/<feature>/quickstart.md` — when verification or acceptance criteria are
  involved.

If the change is a simple, non–Spec-Kit change, do **not** read the Spec Kit
artifacts; review the code normally.

If a referenced document is missing or unavailable, state that clearly in the
output rather than inventing its rules.

## General Review Goals (both stacks)

1. **Clean Code** — clear descriptive names; small focused units; no vague names
   like `DataItem`, `Obj`, `Temp`, `Info2`; no comments restating obvious code;
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

Source-sensitive data is the highest-priority safety area. Review whether the
change:

- invents Quranic text, ayah text, word text, roots, tafsir, translations, i3rab,
  or gates.
- silently corrects Quranic data in frontend or backend without traceability.
- hides missing data instead of showing controlled states.
- removes source/traceability metadata for imported/generated data.
- changes Quranic display in a way that may affect meaning or readability.

Any such issue is high priority (treat as BLOCKING or MAJOR depending on impact).

## Testing and Verification

- Build was run when relevant.
- Tests were run when available or when logic is sensitive (parsing, validation,
  mapping, importers, business rules).
- Data-related work includes a validation/report path.
- Any skipped verification is clearly stated. If build/test status is unknown, say
  unknown — do not assume success.

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

## 8. Verification Check

Report build/test evidence if provided. If no build/test was run, say so clearly.

## 9. Final Recommendation

Short, direct next step consistent with the verdict.

## Guardrails

- Be direct and practical.
- Do not invent facts; if the diff or file tree is unavailable, request it.
- If build/test status is unknown, say unknown.
- Separate findings by severity; do not inflate severity.
- Do not request broad refactors unless necessary.
- Do not implement fixes unless explicitly asked.
- Avoid skill explosion: this single skill covers backend and frontend review.
- Avoid religiously inappropriate terminology such as "God service"; use
  "overloaded service" or "oversized service".
