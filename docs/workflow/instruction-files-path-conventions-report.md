# Instruction Files Path Conventions Report

Date: 2026-06-13  
Scope: Report-only inspection of `/projects/Dashboard/App` instruction files and folder conventions.  
No instruction, architecture, source, migration, build, test, staging, or commit action was performed.

## 1. Verdict

**READY TO UPDATE**

The workspace already has the intended folder split in practice:

- Workspace-local import source packages live under `resources/import-sources/`.
- Planning/design documents live under `docs/feature-XXX-feature-name/`.
- Spec Kit artifacts live under `specs/`.
- Backend reports live under `Backend/report/feature-XXX-feature-name/`.

The main gap is discoverability. The durable AGENTS/CLAUDE instruction files do not yet state these conventions as stable rules, so agents can infer paths only from active feature notes or older feature documents.

## 2. Current Instruction File Inventory

| File | Exists | Current role | Mentions resources/docs/specs/report paths? | Notes |
| --- | --- | --- | --- | --- |
| `/projects/Dashboard/App/AGENTS.md` | Yes | Root OpenCode/workspace instruction file. Points agents to Backend and Frontend project instruction files, `CODING_PRINCIPLES.md`, clean-code/test self-checks, and product/design context. | Mentions `specs/005...`, `docs/feature-005...`, and `Backend/report/feature-005...` only inside the active Spec Kit block. Does not mention `resources/`. | Needs a durable workspace path-conventions section outside the active Spec Kit block. |
| `/projects/Dashboard/App/CLAUDE.md` | Yes | Root Claude/workspace instruction file. Same structure and content as root `AGENTS.md`, with Claude-specific wording for the file name. | Same as root `AGENTS.md`. | Should mirror the root `AGENTS.md` convention text so both agent families get the same guidance. |
| `/projects/Dashboard/App/Backend/AGENTS.md` | Yes | Backend OpenCode instruction file. Points to `.architecture/BACKEND_STRUCTURE.md`, `.architecture/CLEAN_ARCHITECTURE.md`, `.architecture/API_GUIDELINES.md`; defines migration restrictions and API localization reminders. | Does not mention `resources/`, `import-sources/`, `docs/`, `specs/`, or `Backend/report/` as canonical paths. | Needs a backend-specific source-package/report section. |
| `/projects/Dashboard/App/Backend/CLAUDE.md` | Yes | Backend Claude instruction file. Same structure and content as Backend `AGENTS.md`. | Same as Backend `AGENTS.md`. | Should mirror Backend `AGENTS.md`. |
| `/projects/Dashboard/App/Frontend/quran-dashboard-ui/AGENTS.md` | Yes | Frontend OpenCode instruction file. Points to frontend architecture docs and root product/design docs. | Does not mention report conventions. | Leave untouched for now. Frontend report conventions are intentionally deferred. |
| `/projects/Dashboard/App/Frontend/quran-dashboard-ui/CLAUDE.md` | Yes | Frontend Claude instruction file. Same content as frontend `AGENTS.md` with a different title. | Does not mention report conventions. | Leave untouched for now. |

Additional inspected file:

- `/projects/Dashboard/App/CODING_PRINCIPLES.md` exists and includes Quranic data safety rules, including that data processors/importers/generators must produce clear reports with totals, missing records, duplicates, warnings, and validation result. It does not define the canonical report path.

Observed duplication/conflict status:

- Root `AGENTS.md` and root `CLAUDE.md` are intentionally duplicated with only tool-name wording differences. No conflict found.
- Backend `AGENTS.md` and Backend `CLAUDE.md` are intentionally duplicated. No conflict found.
- Frontend `AGENTS.md` and Frontend `CLAUDE.md` are intentionally duplicated. No conflict found.
- No instruction file currently conflicts with the requested conventions.
- The active Spec Kit block in root instruction files already references `docs/feature-005...`, `specs/005...`, and `Backend/report/feature-005...`, but that block is feature-specific and should not be the only source of durable path guidance.

## 3. Current Folder Inventory

| Path | Exists | Observed contents/convention |
| --- | --- | --- |
| `/projects/Dashboard/App/resources` | Yes | Contains `import-sources/` and `report/`. Root `.gitignore` ignores `resources/`, so this tree is local-only by default. |
| `/projects/Dashboard/App/resources/import-sources` | Yes | Contains staged source package folders: `mutashabihat/`, `quran-foundation/`, `quran-morphology/`. |
| `/projects/Dashboard/App/docs` | Yes | Contains feature planning folders named `feature-001...` through `feature-006...` plus `README.md`. |
| `/projects/Dashboard/App/specs` | Yes | Contains Spec Kit feature folders named `001...` through `006...`. |
| `/projects/Dashboard/App/Backend/report` | Yes | Contains backend report folders by feature/scope plus `README.md` and `file-organization-report.md`. |
| `/projects/Dashboard/App/Backend/.architecture` | Yes | Contains `API_GUIDELINES.md`, `BACKEND_STRUCTURE.md`, and `CLEAN_ARCHITECTURE.md`. |

Notable existing conventions observed:

- `docs/README.md` says `docs/` holds planning and design documents: foundation plans, design documents, and implementation plans, while post-work audits/verification reports live separately under `Backend/report/`.
- `Backend/report/README.md` says `Backend/report/` holds audit, investigation, readiness, verification, and review reports: records of what was found or done during/after backend implementation.
- Feature documents already use `docs/feature-XXX-feature-name/` for planning and pre-Spec Kit reports.
- Backend reports already use `Backend/report/feature-XXX-feature-name/` for backend investigations, implementation reports, validation reports, and review outputs.
- Existing feature documents already show the desired source-package pattern: staged packages under `App/resources/import-sources/<source-or-feature>/`, with upstream/random local folders treated as provenance rather than importer defaults.
- Some older/newer planning reports mention upstream paths outside `/projects/Dashboard/App/resources`, including `/projects/Dashboard/resources/...` and `~/Desktop/projects/Dashboard/resources/...`. These should remain provenance notes only, not canonical importer defaults.

## 4. Recommended Documentation Strategy

Update later:

- `/projects/Dashboard/App/AGENTS.md`
- `/projects/Dashboard/App/CLAUDE.md`
- `/projects/Dashboard/App/Backend/AGENTS.md`
- `/projects/Dashboard/App/Backend/CLAUDE.md`

Do not update later unless separately requested:

- `/projects/Dashboard/App/Frontend/quran-dashboard-ui/AGENTS.md`
- `/projects/Dashboard/App/Frontend/quran-dashboard-ui/CLAUDE.md`
- Backend `.architecture/` docs
- Spec Kit templates

Recommended shape:

- Add a concise `Workspace Path Conventions` section to root `AGENTS.md` and root `CLAUDE.md` after `Workspace Project Instructions` and before `Coding Principles`.
- Add a concise `Backend Reports and Import Sources` section to Backend `AGENTS.md` and Backend `CLAUDE.md` after `Backend Architecture Guides` and before `EF Core Migrations`.
- Keep root files responsible for workspace-level routing: `resources/`, `docs/`, `specs/`, and `Backend/report/`.
- Keep backend files responsible for backend-specific usage: importers use staged local packages from root `resources/import-sources/`, backend reports go to `Backend/report/feature-XXX-feature-name/`, and source data is not silently edited.
- Leave frontend instruction files unchanged until frontend-heavy reporting conventions are intentionally created.

What should not be duplicated:

- Do not copy full backend folder architecture rules into root instruction files; root should only route agents to the correct canonical folders.
- Do not copy this full report into AGENTS/CLAUDE; use short operational rules.
- Do not duplicate Spec Kit artifact rules inside backend architecture docs; `specs/` is a workspace-level convention.
- Do not put report-path policy into `.architecture/BACKEND_STRUCTURE.md` unless the team wants architecture docs to govern operational reporting too. Today, `docs/README.md` and `Backend/report/README.md` already cover the planning/report split.

Optional shared reference doc:

- If the team wants to avoid maintaining identical long path text in four instruction files, create `/projects/Dashboard/App/docs/workflow/path-conventions.md` later and keep AGENTS/CLAUDE snippets short.
- This is optional. The immediate update can be done safely by adding short sections directly to the four instruction files.

## 5. Proposed Text Blocks

### App/AGENTS.md

Recommended insertion point: after `Workspace Project Instructions`, before `Coding Principles`.

```markdown
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
```

### App/CLAUDE.md

Recommended insertion point: after `Workspace Project Instructions`, before `Coding Principles`.

```markdown
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
```

### Backend/AGENTS.md

Recommended insertion point: after `Backend Architecture Guides`, before `EF Core Migrations`.

```markdown
## Backend Reports and Import Sources

Backend report outputs belong under:

- `report/feature-XXX-feature-name/` from the Backend repo perspective
- `/projects/Dashboard/App/Backend/report/feature-XXX-feature-name/` as an absolute path

Use this location for backend implementation reports, import reports, engineering review outputs, real-run reports, validation reports, and backend feature completion reports.

Importer/source-data rules:

- Canonical local source packages live at `/projects/Dashboard/App/resources/import-sources/<feature-or-source-name>/`.
- `resources/` is local and gitignored; do not assume these files are committed or available in CI/production.
- Import features should read from staged/canonicalized source packages, not random upstream folders, when a staged package is required.
- Treat upstream source folders as provenance/read-only inputs unless the task explicitly asks to stage or canonicalize a package.
- Do not silently modify source data. Preserve traceability from imported/generated data back to the staged source package.

Planning and Spec Kit separation:

- Workspace planning reports and pre-Spec Kit documents belong under `/projects/Dashboard/App/docs/feature-XXX-feature-name/`.
- Spec Kit artifacts belong under `/projects/Dashboard/App/specs/`.
- Backend post-work and validation reports belong under `/projects/Dashboard/App/Backend/report/`, not under workspace `docs/`.
```

### Backend/CLAUDE.md

Recommended insertion point: after `Backend Architecture Guides`, before `EF Core Migrations`.

```markdown
## Backend Reports and Import Sources

Backend report outputs belong under:

- `report/feature-XXX-feature-name/` from the Backend repo perspective
- `/projects/Dashboard/App/Backend/report/feature-XXX-feature-name/` as an absolute path

Use this location for backend implementation reports, import reports, engineering review outputs, real-run reports, validation reports, and backend feature completion reports.

Importer/source-data rules:

- Canonical local source packages live at `/projects/Dashboard/App/resources/import-sources/<feature-or-source-name>/`.
- `resources/` is local and gitignored; do not assume these files are committed or available in CI/production.
- Import features should read from staged/canonicalized source packages, not random upstream folders, when a staged package is required.
- Treat upstream source folders as provenance/read-only inputs unless the task explicitly asks to stage or canonicalize a package.
- Do not silently modify source data. Preserve traceability from imported/generated data back to the staged source package.

Planning and Spec Kit separation:

- Workspace planning reports and pre-Spec Kit documents belong under `/projects/Dashboard/App/docs/feature-XXX-feature-name/`.
- Spec Kit artifacts belong under `/projects/Dashboard/App/specs/`.
- Backend post-work and validation reports belong under `/projects/Dashboard/App/Backend/report/`, not under workspace `docs/`.
```

### Optional Shared Reference Doc

Recommended only if the team wants one canonical longer reference instead of repeating detailed rules in instruction files.

Potential path: `/projects/Dashboard/App/docs/workflow/path-conventions.md`

```markdown
# Workspace Path Conventions

## Import Source Packages

- Canonical local import source root: `/projects/Dashboard/App/resources`.
- Canonical staged package root: `/projects/Dashboard/App/resources/import-sources/<feature-or-source-name>/`.
- `resources/` is local and gitignored; agents must not assume these files are committed, pushed, available in CI, or available in production.
- Importers should use staged/canonicalized source packages. Do not import directly from random upstream folders when a staged package is required.
- Upstream folders may be documented as provenance, but should not become runtime defaults unless explicitly approved.

## Planning Docs

- Planning reports, data capability reports, decision addendums, and pre-Spec Kit reports belong under `/projects/Dashboard/App/docs/feature-XXX-feature-name/`.
- `docs/` is for intended plans, design reasoning, and pre/post decision records at the workspace level.

## Spec Kit Artifacts

- Spec Kit artifacts belong under `/projects/Dashboard/App/specs/`.
- Do not mix Spec Kit specifications, plans, tasks, contracts, or quickstarts into `docs/`.

## Backend Reports

- Backend reports belong under `/projects/Dashboard/App/Backend/report/feature-XXX-feature-name/`.
- Use this path for backend implementation reports, import reports, engineering review outputs, real-run reports, validation reports, and backend completion reports.

## Frontend Reports

- Frontend report conventions are deferred. Do not invent a frontend report folder without an explicit task or decision.
```

## 6. Path Convention Rules

Final recommended rules:

### App/resources

- Canonical absolute path: `/projects/Dashboard/App/resources`.
- Purpose: local import source files, staged data packages, and local resource-related outputs.
- Git status: ignored by root `.gitignore` via `resources/`.
- Rule: agents must not assume files under `resources/` are committed, pushed, available in CI, or production dependencies.
- Rule: `resources/` is not a runtime production dependency by default.

### App/resources/import-sources

- Canonical absolute path: `/projects/Dashboard/App/resources/import-sources`.
- Package pattern: `/projects/Dashboard/App/resources/import-sources/<feature-or-source-name>/`.
- Purpose: staged/canonicalized local source packages used by importers.
- Rule: import features should read from staged packages when a staged package is required.
- Rule: do not import directly from random upstream folders such as `/projects/Dashboard/resources/...` or `~/Desktop/...` unless the task explicitly says to inspect or stage from those paths.
- Rule: upstream folders are provenance/read-only inputs until a staged package is created.

### App/docs

- Canonical absolute path: `/projects/Dashboard/App/docs`.
- Feature pattern: `/projects/Dashboard/App/docs/feature-XXX-feature-name/`.
- Purpose: feature planning documents, data capability reports, decision addendums, pre-Spec Kit reports, and companion planning material.
- Rule: do not put backend implementation/validation reports here when they belong to `Backend/report/`.

### App/specs

- Canonical absolute path: `/projects/Dashboard/App/specs`.
- Purpose: Spec Kit artifacts only, including feature specifications, plans, tasks, contracts, data models, research, and quickstarts produced by Spec Kit workflows.
- Rule: do not confuse `docs/` planning reports with `specs/` Spec Kit artifacts.

### Backend/report

- Canonical absolute path: `/projects/Dashboard/App/Backend/report`.
- Feature pattern: `/projects/Dashboard/App/Backend/report/feature-XXX-feature-name/`.
- Purpose: backend implementation reports, import reports, engineering review outputs, real-run reports, validation reports, backend completion reports, audit/investigation/readiness reports, and records of what was found or done.
- Rule: backend post-work and validation outputs should go here, not to workspace `docs/`.

### Frontend Reports Deferred

- Current status: no frontend report convention is established.
- Rule: do not invent frontend report folders now.
- Rule: leave frontend `AGENTS.md` and `CLAUDE.md` unchanged unless a future frontend-heavy task explicitly establishes a frontend report convention.

## 7. Risks / Anti-patterns

Future agents must avoid:

- Writing reports into random folders such as ad hoc root folders, feature source folders, or upstream resource directories.
- Reading from `/projects/Dashboard/resources/...` when the canonical workspace-local path should be `/projects/Dashboard/App/resources/...`.
- Treating `resources/` files as committed source files or assuming they exist in another clone, CI, or production.
- Making runtime production behavior depend on local gitignored `resources/` files by default.
- Mixing planning reports in `docs/` with Spec Kit artifacts in `specs/`.
- Putting backend implementation, validation, import, or real-run reports under `App/docs` instead of `Backend/report`.
- Importing directly from random upstream folders when a staged package is required.
- Silently modifying source data instead of creating/staging canonical copies and preserving provenance.
- Editing backend architecture docs to solve a workspace path-discoverability issue unless the team explicitly decides architecture docs should own operational report-path policy.
- Adding frontend report conventions prematurely.

## 8. Final Recommendation

Update instruction files now in a follow-up step.

Exact files to edit next:

- `/projects/Dashboard/App/AGENTS.md`
- `/projects/Dashboard/App/CLAUDE.md`
- `/projects/Dashboard/App/Backend/AGENTS.md`
- `/projects/Dashboard/App/Backend/CLAUDE.md`

Recommended non-edits for the next step:

- Do not edit `/projects/Dashboard/App/Frontend/quran-dashboard-ui/AGENTS.md`.
- Do not edit `/projects/Dashboard/App/Frontend/quran-dashboard-ui/CLAUDE.md`.
- Do not edit `/projects/Dashboard/App/Backend/.architecture/*`.
- Do not edit Spec Kit artifacts unless a separate Spec Kit task requires it.

Follow-up decisions:

- Decide whether a shared durable reference document such as `/projects/Dashboard/App/docs/workflow/path-conventions.md` should be created later. This is optional, not a blocker.
- Decide whether `docs/README.md` and `Backend/report/README.md` should be refreshed later to include feature 004-006 folders and the `resources/import-sources/` rule. This is useful housekeeping, not required before updating instruction files.

Blockers: none.
