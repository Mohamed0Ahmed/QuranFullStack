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
- Merged features 001–019 are historical and their `contracts/` were removed; the current/steady-state contract truth is the code + nearest README, indexed by `docs/contracts/`. New features still populate `specs/<feature>/contracts/` during development.
- Backend implementation, import, engineering review, real-run, validation, and completion reports live under `Backend/report/feature-XXX-feature-name/`.
- Frontend report conventions are not established yet; do not invent frontend report folders unless the task explicitly asks for that decision.

## Local README Context (read before you change a folder)

- Before modifying any folder, look for `README.md` in that folder and in the nearest
  relevant parent folders, and read the nearest relevant README FIRST. It states the
  current truth, boundaries, and invariants of that area.
- Local `README.md` = WHAT an area does now and what must not break.
  `AGENTS.md` / `CLAUDE.md` / `.architecture/*` = HOW to work and how to write code.
  `specs/<feature>/` = per-feature Spec-Kit planning (active features live; 001–019 historical, contracts removed). Reports = evidence only.
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

### Comment sparingly (default: NO comment)
Default to writing NO comment. Add an inline comment ONLY when it captures critical,
non-obvious WHY that the code cannot show and whose loss would cause a real bug or a
security / Quran-data-integrity mistake — e.g. a fail-closed decision, a data-safety
invariant, a concurrency/commit-ordering rationale, or a genuine gotcha — tied to the exact
line. If you want to explain WHAT the code does, an area’s behaviour/boundaries/invariants,
or how to use it: do NOT comment — put it in the nearest README.md. Never narrate WHAT the
code does, restate logic, add banner/separator comments, or write boilerplate ///-XML-doc or
JSDoc on controllers, endpoints, DTOs, components, or services (no Swagger/doc consumer reads
them). Frontend: no step-narrating //, <!-- -->, or SCSS comments restating markup/style.
Keep only functional/directive comments (license headers, compiler/analyzer pragmas,
lint-disable, tool markers). When in doubt, delete it; if the knowledge matters, add it to
the nearest README.

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

## Testing Strategy

Before selecting or running tests, read:

- `TESTING_STRATEGY.md`

Use the tier required by the changed scope (Tier A focused per-phase, Tier B
no-pipeline milestone regression, Tier C ordinary pre-PR, Tier D
pipeline-triggered, Tier E release/canonical acceptance). Do not run full or
slow Quran data-pipeline suites after every phase unless the strategy's
triggers require them. Required verification must be fresh against the final
working tree.

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

