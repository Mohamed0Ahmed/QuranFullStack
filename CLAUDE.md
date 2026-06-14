<!-- SPECKIT START -->

Active feature: **Quran Tafsir Foundation** — branch `007-quran-tafsir-foundation`.
For technologies, project structure, shell commands, and design artifacts, read the current plan
and its siblings:

- `specs/007-quran-tafsir-foundation/plan.md` — technical context, project structure, gates
- `specs/007-quran-tafsir-foundation/spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/`
- `docs/feature-007-quran-tafsir-foundation/feature-007-quran-tafsir-foundation-planning-report.md` — long-form companion plan
- `resources/import-sources/quran-tafsirs/README.md`, `manifest.json`, `package-report.md` — final local source package metadata (every import count traces here)

<!-- SPECKIT END -->

## Workspace Project Instructions

This repository is a FullStack workspace.

The root `CLAUDE.md` contains general instructions that apply to the whole workspace.

When working on the Backend project, also read and follow:

- `Backend/CLAUDE.md`

When working on the Frontend project, also read and follow:

- `Frontend/quran-dashboard-ui/CLAUDE.md`

If a task touches both Backend and Frontend, read all relevant instruction files before making changes.

If a project-specific instruction conflicts with a root instruction, follow the more specific project instruction unless it would violate a root safety or product rule.

## Workspace Path Conventions

Canonical workspace paths:

- Import source files and local staged data packages live under `resources/`.
- Importers should use staged source packages under `resources/import-sources/<feature-or-source-name>/`.
- `resources/` is local and gitignored; do not assume files under it are committed or available in other clones.
- Source packages must be staged/canonicalized before import features use them. Do not import directly from random upstream folders when a staged package is required.
- Feature planning documents, capability reports, decision addendums, and pre-Spec Kit reports live under `docs/feature-XXX-feature-name/`.
- Spec Kit artifacts live under `specs/`; do not confuse `docs/` planning reports with `specs/` feature specifications, plans, tasks, contracts, or quickstarts.
- Backend implementation, import, engineering review, real-run, validation, and completion reports live under `Backend/report/feature-XXX-feature-name/`.
- Frontend report conventions are not established yet; do not invent frontend report folders unless the task explicitly asks for that decision.

## Coding Principles

Before any implementation work, read and follow:

- `CODING_PRINCIPLES.md`

These principles apply to the whole FullStack workspace. Project-specific instruction files may add more detailed rules for Backend or Frontend work.

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
