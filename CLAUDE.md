## Workspace Project Instructions

This repository is the canonical FullStack monorepo. `Backend/` and
`Frontend/quran-dashboard-ui/` are ordinary tracked directories, not separate
repositories.

The root `CLAUDE.md` contains general instructions that apply to the whole workspace.

When working on the Backend project, also read and follow:

- `Backend/CLAUDE.md`

When working on the Frontend project, also read and follow:

- `Frontend/quran-dashboard-ui/CLAUDE.md`

If a task touches both Backend and Frontend, read all relevant instruction files before making changes.

If a project-specific instruction conflicts with a root instruction, follow the more specific project instruction unless it would violate a root safety or product rule.

## Branching workflow

This repository follows a `dev`-based Git-Flow model. Two long-lived branches:

- **`main` — stable / production.** Railway auto-deploys from it, and it is
  protected. It receives merges ONLY from `dev` (releases, roughly every ~5
  features) plus explicit emergency hotfixes. Never commit to `main` directly.
- **`dev` — long-lived integration branch**, branched off `main` and kept in sync
  with `main` after each release or hotfix.

Rules for all work:

- ALL new work branches off `dev`, never off `main`.
- Feature branches open pull requests into `dev`. NEVER open a PR against `main`.
- `dev → main` merges are the release boundary and happen ONLY on explicit
  request from the user. The same applies to emergency hotfixes targeting `main`.

## Workspace Path Conventions

Canonical workspace paths:

- Import source files and local staged data packages live under `resources/`.
- Importers should use staged source packages under `resources/import-sources/<feature-or-source-name>/`.
- `resources/` is local and gitignored; do not assume files under it are committed or available in other clones.
- Source packages must be staged/canonicalized before import features use them. Do not import directly from random upstream folders when a staged package is required.
- Feature planning documents, capability reports, decision addendums, and pre-Spec Kit reports live under `docs/feature-XXX-feature-name/`.
- Spec Kit artifacts live under `specs/<feature>/`, the per-feature planning workspace. For an **active** feature its `spec`/`plan`/`tasks`/`contracts` are live planning inputs (the Spec-Kit implementation-review checks the work against `specs/<feature>/contracts/`). Do not confuse `docs/` planning reports with `specs/` feature specifications, plans, tasks, or quickstarts.
- Current contract index (thin, pointer-only) → `docs/contracts/`; it defers to code + the nearest `README.md`.
- The current/steady-state contract truth is the code + nearest README, indexed by `docs/contracts/`. New features still populate `specs/<feature>/contracts/` during development.
- Backend implementation, import, engineering review, real-run, validation, and completion reports live under `Backend/report/feature-XXX-feature-name/`.
- Frontend report conventions are not established yet; do not invent frontend report folders unless the task explicitly asks for that decision.
- **Planning artifacts are feature-scoped and die when the feature closes.** At feature
  close, delete that feature's `specs/<feature>/`, `docs/feature-XXX-*/`, and
  `Backend/report/feature-XXX-*/` from the working tree. Git history is the archive; the
  deletion is hygiene so agents stop grepping decisions that no longer bind.
  - **N-2 buffer.** Keep the planning artifacts of the **two most recently closed**
    features (by merge date into `dev`) plus **every currently open** feature. Closing a
    third feature evicts the oldest buffered one.
  - **Never deleted by this rule:**
    - Live non-feature folders: `Backend/report/architecture/`, `report/database/`,
      `report/database-inventory/`, `docs/contracts/`, `docs/api-reference/`,
      `docs/deployment-railway/`, `docs/design-preview/`, and the `README.md` of
      `specs/`, `docs/`, and `Backend/report/`.
    - **Umbrella plans still governing unbuilt work** — a master plan spanning several
      unfinished features outlives any one of them.
    - **Evidence** whose facts are not restated by a live document: import/source
      verification, canonical counts and hashes, measured performance budgets that back a
      live assertion, destructive-path and Quran-safety inventories, cross-cutting audits.
      Evidence is judged **per file**, not per folder — a feature folder may lose its
      completion report and keep its import report.
  - **Repoint before you delete.** `grep -rn` the whole repo (code, tests, skills, data
    files, READMEs, `.specify/`) for every path being removed. A referenced artifact may
    not be deleted until the reference is repointed to code + the nearest `README.md`, or
    the fact is folded into that README. Dangling links are a defect, not an acceptable
    cost.

## Local README Context (read before you change a folder)

- Before modifying any folder, look for `README.md` in that folder and in the nearest
  relevant parent folders, and read the nearest relevant README FIRST. It states the
  current truth, boundaries, and invariants of that area.
- Local `README.md` = WHAT an area does now and what must not break.
  `AGENTS.md` / `CLAUDE.md` / `.architecture/*` = HOW to work and how to write code.
  `specs/<feature>/` = per-feature Spec-Kit planning for open features plus the N-2 buffer;
  closed features are deleted from the tree and live in git history. Reports = evidence only.
- If your change alters behavior, commands, boundaries, routes, data invariants,
  import behavior, API contracts, URL state, or tests described in a README, UPDATE
  that README in the SAME change.
- Do NOT create long-lived feature reports by default. Reserve reports for audits,
  reviews, acceptance evidence, data imports, diagnostics, and one-off investigations.
- Specs are per-feature planning artifacts; steady-state truth is code + nearest README, indexed by `docs/contracts/`.

## Coding Principles

Before any implementation work, read and follow:

- `CODING_PRINCIPLES.md`

These principles apply to the whole FullStack workspace. Project-specific instruction files may add more detailed rules for Backend or Frontend work.

### Comment sparingly

Comment only the non-obvious WHY — rationale, gotchas, invariants, decisions, local
security/fail-closed choices — tied to the specific line it explains. Do NOT narrate
WHAT the code does, restate obvious logic, or duplicate a README. This applies to both
Backend and Frontend.

- No `///`/XML-doc comments on controllers, endpoints, or DTOs/models (internal solo
  project; no Swagger/API-doc consumer reads them). Keep XML-doc only for the rare case
  where it carries genuine non-obvious WHY, and prefer a short plain `//` comment there.
- Frontend: no boilerplate JSDoc (`/** */`) narrating a component/service; no
  step-narrating `//`, `<!-- -->`, or SCSS comments that restate the code/markup/style.
- Area-level explanation (WHAT an area does, its boundaries/invariants) belongs in the
  nearest `README.md`, not in per-line comments. If a spot seems to need many comments
  to be understood, add or expand that README instead.

### Clean-code self-check before delivery

Before delivering implementation code, run a quick clean-code guard self-check against
the reference pack at `.claude/skills/engineering-review/references/clean-code-guard/`,
focusing on:

- naming and functions
- comments and formatting
- SOLID
- DRY / KISS / YAGNI
- AI-generated-code failure modes

Notes:

- `engineering-review` remains the formal post-implementation review skill; this
  self-check does not replace it.
- The clean-code-guard pack is reference material only, not a separate skill.
- Project-specific rules override generic clean-code guidance — in particular, C#/.NET
  `I`-prefixed interface names and the `ApiResponse` contract /
  `Backend/.architecture/API_GUIDELINES.md` at the API boundary are authoritative.

### Test-code self-check before delivery

Before delivering tests, run a quick test-code self-check:

- test behavior, not implementation details
- every mock targets a real boundary
- variants use data-driven tests
- no tests for framework guarantees
- real DTOs/entities/value objects are constructed, not mocked
- persistence/query tests use real infrastructure where correctness matters
- Quranic test data remains source-safe

Consult `.claude/skills/test-guard/` only when writing or reviewing test code, or when
deeper guidance is needed.

Notes:

- `engineering-review` remains the formal post-implementation review skill.
- `test-guard` is only for test-code quality.

### Test selection

Before selecting or running tests, read:

- `TESTING_STRATEGY.md`

It is the single source of truth for which tests to run and when (§1). Use the tier
required by the changed scope — Tier A focused per-phase, Tier B no-pipeline milestone
regression, Tier C ordinary pre-PR, Tier D pipeline-triggered, Tier E release/canonical
acceptance (§3), with the change-to-tier matrix in §4 and the validated command catalogs
in §5 (Backend) and §6 (Frontend). Do not run the full Backend suite or the slow Quran
data-pipeline families after every phase unless the strategy's Tier D triggers require it.

Two facts the strategy fixes that agents get wrong here:

- **There is no CI** (§8). Every tier is a local gate that nothing verifies ran; "CI is
  green" is never available as evidence.
- **The route-parity/smoke gate is active** (`QuranDashboard.Tests.Smoke`, §3 Tier A/C, §5).
  Any change touching `Backend/api/` routes, request/response contracts, auth, middleware,
  or model binding MUST run it, and the evidence MUST say whether the data tier ran or
  skipped. Adding or changing a route also requires the matching `SmokeRouteCatalog` entry
  in the same change (§10). The namespace **is** excluded from the fast Tier B/C
  no-pipeline filter — `&FullyQualifiedName!~QuranDashboard.Tests.Smoke.` belongs there.

A third fact: a browser E2E layer exists (`Frontend/quran-dashboard-ui/e2e/`,
`npm run e2e`), but it is **opt-in and not a required tier** — do not present an E2E run as a
Tier C or release gate, and do not confuse it with the backend route-smoke tier, which is
required for route/contract/auth changes.

## Design Context

This is an **Arabic-first (RTL)**, **scholarly and calm** product dashboard for
curating Quran research data: organizing gates (أبواب), reviewing ayah links, and
preparing content for publishing. Users are Arabic-speaking admins and teachers.

Before any frontend / UI work, read **`PRODUCT.md`** and **`DESIGN.md`** when
product/design context is relevant; both files exist. `PRODUCT.md` is the product
strategy/context (register, users, principles, anti-references); `DESIGN.md` is the
visual/design direction. Guiding principles: reverence without ornament, calm for
long focus,
trustworthy structure, genuinely Arabic-first, earned familiarity. Avoid generic
SaaS templates, kitschy religious decor, gamified/consumer UI, and dense
enterprise greige.

<!-- SPECKIT START -->

## Active Spec Kit Feature

- `ux-slice-f` — sections (UX audit items 18-19): `POST api/abwab/sections/{id:int}/order`
  plus the per-section root-door count badge on the tab strip. Plan and evidence:
  `docs/feature-ux-slice-f/plan.md`, `docs/feature-ux-slice-f/evidence.md`. No `specs/`
  workspace — this slice is plan-driven, not Spec Kit.
- When a feature opens, record it here as: feature slug, its `specs/<feature>/plan.md`, and
  its `docs/feature-XXX-*/` decision record. Clear this section back to "None" when the
  feature closes and its planning artifacts are swept per the lifecycle rule above.

<!-- SPECKIT END -->
