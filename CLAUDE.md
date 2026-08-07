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
- **Planning artifacts and reports are deleted by the feature that created them.** A
  feature's `specs/<feature>/`, `docs/feature-*/`, and `Backend/report/feature-*/` are
  working files, and the feature's **last commit before merge is a deletion commit** that
  removes them. There is no buffer, no grace period, and no deferred cleanup pass; the
  project stays clean continuously rather than accumulating and being swept.
  - **The deletion commit comes AFTER the engineering review passes**, never before — the
    review compares the work against the plan, so the plan has to still be there.
  - **It is pure deletion.** README amendments already land in the same commit as the work
    they describe (see *Local README Context* below), so by the time this commit runs the
    READMEs are already true.
  - **The gate, applied per file:** *does this file assert a fact that is not recoverable
    from code, tests, or an existing README?* **No** → delete it; most files answer no.
    **Yes** → write the fact into the nearest README, **prove it from code with a
    `file:LINE`**, repoint every inbound reference, then delete. Never fold a claim you
    could not confirm in code — the artifact says "do this", the code says "I do this", and
    only the second is evidence. Folding wrong is worse than deleting: a paraphrase in a
    README becomes the truth with nothing left to check it against. Folding nothing is just
    as bad — silence about an invariant misleads exactly as much as a stale claim.
  - **Evidence worth keeping becomes a test that fails on drift, not a report.** A canonical
    count, source hash, or measured budget with nothing asserting it is a rumour. If the
    assertion has nowhere to live yet, keep the file and record in `docs/TESTING_DEBT.md`
    what the test must assert and where it must go.
  - **Repoint before you delete.** `grep -rn` the whole repo — code, tests, `.claude/`,
    `.agents/`, `.specify/`, scripts, manifests, every README — for each path being removed.
    A dangling link blocks the delete; it is a defect, not an acceptable cost.
- **The long-lived documentation is exactly this list.** Anything not on it is
  feature-scoped and dies with its feature:
  - every `README.md` anywhere in the repo — the current truth of the area it sits in;
  - root and per-project law: `CLAUDE.md`, `AGENTS.md`, `CODING_PRINCIPLES.md`,
    `TESTING_STRATEGY.md`, `PRODUCT.md`, `DESIGN.md`, `SKILLS_AND_ARCHITECTURE_GUIDE.md`
    and their Backend/Frontend counterparts;
  - all `.architecture/**` documents;
  - `docs/contracts/**` — the pointer index that makes this rule workable;
  - `docs/TESTING_DEBT.md` — a live ledger and the agenda of the next feature;
  - everything under `.claude/`, `.agents/`, `.specify/`, and all code, tests, and
    configuration.

## Local README Context (read before you change a folder)

- Before modifying any folder, look for `README.md` in that folder and in the nearest
  relevant parent folders, and read the nearest relevant README FIRST. It states the
  current truth, boundaries, and invariants of that area.
- Local `README.md` = WHAT an area does now and what must not break.
  `AGENTS.md` / `CLAUDE.md` / `.architecture/*` = HOW to work and how to write code.
  `specs/<feature>/` = per-feature Spec-Kit planning, for **open features only**; a feature
  deletes its own planning artifacts before it merges. Reports = evidence only.
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

### Comments are forbidden by default

**This section is the canonical comment rule for the whole workspace.** Everything else that
mentions comments — `CODING_PRINCIPLES.md`, the Backend and Frontend instruction files, the
`clean-code-guard` reference pack — defers to it and adds only language-specific detail. If any
of them appears to say something different, this wins and that copy is a defect.

Not "used sparingly" — **forbidden**. Code, names and structure carry the meaning. When a piece
of code seems to need a comment, the remedies, in order, are: a better name, a smaller function,
a clearer structure, or a line in the nearest `README.md`. Writing a comment is the last resort,
never the first.

**The single exception, and its bar is deliberately extreme.** A comment may exist only when
**all three** hold:

1. The fact cannot be derived from the code by a competent reader.
2. Omitting it would let a competent developer make a change that is **WRONG** — not merely
   slower to understand. Convenience is not a justification; only prevented harm is.
3. It cannot be solved by renaming, restructuring, or a sentence in the nearest `README.md`.

**The burden of proof lies with the comment, never with its deletion.** If it is arguable
whether a comment meets the bar, it does not — delete it.

**Form, when the exception is genuinely met:** one line, on the exact line it explains, stating
the WHY only. If it needs a paragraph, it is not a comment — it is a README entry.

**Forbidden, with no exception:** narrating what the next line does; restating logic in prose;
section-banner or separator comments; JSDoc or XML doc on components, controllers, DTOs,
services and endpoints; step-by-step comments in templates and stylesheets; commented-out code;
`TODO` with no tracked item; comments that repeat a README.

**NOT comments for the purposes of this rule, and never to be removed:** tool and compiler
directives — `// <auto-generated />`, `#pragma warning disable`, `// eslint-disable-*`,
`// @ts-ignore`, `// prettier-ignore`, `/*! … */` in SCSS (it survives minification) — and
license or copyright headers in vendored or third-party files.

#### Scope — production source code only

The policy governs, and governs nothing else:

- **Backend:** `.cs` under `Backend/api/`, `Backend/application/`, `Backend/domain/`,
  `Backend/infrastructure/`.
- **Frontend:** `.ts`, `.html` and `.scss` under `Frontend/quran-dashboard-ui/src/` —
  templates and stylesheets are production code and are included.

**Out of scope. Leave every comment alone: do not edit them, do not report them as findings.**
Test projects and spec files (`Backend/tests/**`, `*.spec.ts`, `e2e/**`); everything under
`.claude/`, `.agents/` and `.specify/`; `Backend/scripts/**`; the DataImporter tooling; build
and CI configuration; and all generated files (`Migrations/*.Designer.cs`, `*ModelSnapshot.cs`,
`*.d.ts`, build output). These are not where the noise is, and churning them costs more than it
returns. **This boundary is part of the rule — do not widen it.**

#### Where removed knowledge goes

Area-level explanation — what an area does, its boundaries and invariants — belongs in the
nearest `README.md`. If the area has no README, create one; if it has one, extend it. Any fact
written there must be provable from the code it describes: a paraphrase that cannot be checked
against code is worse than the comment it replaced.

#### Enforcement — self-cleaning, like planning artifacts

Every change deletes the offending comments **in the area it touches**, in the same commit as
the work, and updates that area's README when something worth keeping was removed. There is no
separate comment-cleanup pass and no backlog.

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
- **On comments the pack is not the standard.** *Comments are forbidden by default* above is,
  and it is stricter than the pack's "comments that earn their keep" list. The pack is
  annotated to say so; if you find a copy that is not, fix it.

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

### Scope-aware test execution

Before selecting tests, inspect the changed files and read `TESTING_STRATEGY.md` plus the
nearest test README. Run the narrowest meaningful gate first. For Backend compilation
changes, build once, then use `Backend/scripts/test-backend --no-build`. Broad gates run once at
milestone, engineering-review, or pre-PR boundaries—not after individual edits.
Pipeline/canonical gates run only for their documented triggers, and Backend-only work does
not require Frontend tests.

Keep long output visible; never pipe it into `tail`. Use the configured hang timeouts, do
not run concurrent PostgreSQL test processes, and leave no Testcontainers running. Report
the exact gate, command, reason, result, skips, and cleanup state; there is no CI fallback.
The formal reviewer owns
final broad review gates. Deleting a test requires documented obsolete/redundant proof and
named replacement coverage. Commands and the trigger matrix live in
`TESTING_STRATEGY.md`.

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

None.

- When a feature opens, record it here as: feature slug, its `specs/<feature>/plan.md`, and
  its `docs/feature-XXX-*/` decision record. Clear this section back to "None" in the same
  deletion commit that removes those artifacts, per the lifecycle rule above.

<!-- SPECKIT END -->
