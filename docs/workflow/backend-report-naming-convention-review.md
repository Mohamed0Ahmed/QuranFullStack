# Backend Report Naming Convention Review

Date: 2026-06-13  
Scope: Report-only engineering/process review for backend report filename conventions.  
No instruction, README, architecture, source, Spec Kit, migration, build, test, staging, commit, or push action was performed.

## 1. Verdict

**RECOMMENDED WITH CHANGES**

Numeric prefixes for human-authored backend reports are a good convention for this project, especially from Feature 006 onward. The convention should be documented, but not by bloating every root instruction file and not by renaming historical reports.

Recommended adjustment: make `Backend/report/README.md` the durable source of truth for report-folder naming, then add only a short operational reminder to `Backend/AGENTS.md` and `Backend/CLAUDE.md`. Do not update root `AGENTS.md` or root `CLAUDE.md` for this detail.

## 2. Current State

### Existing Backend Report Structure

Current backend report root:

```text
/projects/Dashboard/App/Backend/report/
  README.md
  file-organization-report.md
  database-inventory/
  feature-002-quran-foundation/
  feature-003-imlaei-clean-key/
  feature-003-word-display-tables/
  feature-003-word-identity-links/
  feature-004-word-morphology-foundation/
  feature-005-word-simple-i3rab-foundation/
```

Observed feature report files:

```text
feature-002-quran-foundation/
  ayah-37-130-word-count-investigation.md
  quran-foundation-import-source-readiness-report.md

feature-003-word-display-tables/
  word-import-source-normalization-audit-report.md
  words-unique-tables-audit-report.md

feature-003-imlaei-clean-key/
  imlaei-clean-import-binding-report.md

feature-003-word-identity-links/
  feature-003-word-identity-links-restructure-report.md
  phase-1-unique-simple-identity-switch-report.md
  phase-7-dev-reset-reseed-report.md

feature-004-word-morphology-foundation/
  final-completion-report.md

feature-005-word-simple-i3rab-foundation/
  planning-sync-required-updates-report.md
  segment-pattern-rule-coverage-report.md
  simple-i3rab-label-inventory-report.md
```

### Chronology

Chronology is not consistently easy to infer today.

- Some names imply workflow position, such as `final-completion-report.md` and `phase-1-*` / `phase-7-*`.
- Most names describe report content, not when the report was created or how it fits into the feature workflow.
- Alphabetical ordering does not reliably match implementation order.
- `Backend/report/README.md` lists only older feature folders and does not yet document filename ordering.

### Generated Report Names

No `.json` files currently appear under `/projects/Dashboard/App/Backend/report/`.

Feature 006 planning already uses stable generated importer-output naming outside `Backend/report/`, for example:

```text
resources/report/mutashabihat/mutashabihat-import-report.{md,json}
```

That is consistent with the proposed distinction: generated importer/tool outputs should keep stable canonical names so tools, quickstarts, and repeated runs can target predictable paths.

## 3. Evaluation of Proposed Convention

### Benefits

- Improves workflow readability inside each feature folder by making the intended order visible in plain directory listings.
- Helps future agents locate the latest manual status, review, remediation, and completion reports without guessing from filenames.
- Reduces ambiguity when multiple reports are about similar objects but produced at different stages.
- Makes manual report sequences easier to reference in implementation summaries and handoffs.
- Keeps generated importer/tool outputs stable and automation-friendly.
- Allows Feature 006 onward to improve without disrupting historical links.

### Risks

- Retroactive renames would create git churn and break existing references from specs, docs, reports, or commit discussions.
- Agents might incorrectly prefix generated outputs, making importer/report commands less stable across runs.
- Agents might renumber existing manual reports after inserting a new step, creating unnecessary diff/history noise.
- A purely numeric prefix can imply stronger process rigidity than needed if rules are not explicit.
- If documented only in a README that agents do not read, future report names may continue to drift.
- If documented in every root instruction file, the workspace instructions become unnecessarily detailed and harder to maintain.

### Rules That Must Be Explicit

- Numeric prefixes apply to human-authored/manual backend reports inside `Backend/report/feature-XXX-feature-name/`.
- Use three digits, followed by a hyphen and a descriptive kebab-case name: `001-schema-implementation-report.md`.
- Start the convention from Feature 006 onward.
- Do not rename older reports unless a dedicated cleanup task explicitly requests it.
- Do not rename existing linked reports casually.
- Do not renumber already-published reports just to insert a new report between them; append the next available number instead.
- Generated importer/tool outputs keep stable canonical names, such as `mutashabihat-import-report.md`, `mutashabihat-import-report.json`, or `validation-report.json`.
- Stable generated output names may be overwritten/regenerated by tools when that is the tool contract; manual numbered reports are historical records and should not be overwritten casually.
- Frontend report conventions remain deferred.

## 4. Recommended Documentation Location

### Files To Update Later

1. `/projects/Dashboard/App/Backend/report/README.md`

This should be the durable home for backend report-folder conventions. It already explains the purpose of `Backend/report/`, so filename ordering belongs there.

2. `/projects/Dashboard/App/Backend/AGENTS.md`

Add a short reminder under the existing `Backend Reports and Import Sources` section. Backend coding agents read this file before backend work, so it should point them to the naming convention without carrying the full policy.

3. `/projects/Dashboard/App/Backend/CLAUDE.md`

Mirror the same short reminder as Backend `AGENTS.md` for Claude-based agents.

### Files Not To Update

- Do not update `/projects/Dashboard/App/AGENTS.md`.
- Do not update `/projects/Dashboard/App/CLAUDE.md`.
- Do not update `/projects/Dashboard/App/Frontend/quran-dashboard-ui/AGENTS.md`.
- Do not update `/projects/Dashboard/App/Frontend/quran-dashboard-ui/CLAUDE.md`.
- Do not update `/projects/Dashboard/App/Backend/.architecture/*`.
- Do not update `/projects/Dashboard/App/specs/*`.
- Do not create a shared `docs/workflow` reference doc for this convention unless report conventions expand across backend/frontend/workspace later.

### Why

- Root workspace instruction files should stay high-level: canonical folder routing belongs there, but backend report filename policy is too detailed.
- Backend instruction files should contain a short operational rule because future backend agents will create reports.
- `Backend/report/README.md` should own the full report-folder convention because it is the nearest durable reference for report organization.
- Architecture docs should not own operational report filename policy.
- Frontend conventions are explicitly deferred.

## 5. Proposed Text Blocks

### Backend/report/README.md

Recommended insertion point: after `Reports are grouped into one subfolder per feature/scope.` and before `## Layout`.

````markdown
## Filename Conventions

From Feature 006 onward, human-authored backend reports inside a feature report folder should use a three-digit chronological prefix plus a descriptive kebab-case name:

```text
001-schema-implementation-report.md
002-importer-implementation-report.md
003-real-import-run-summary.md
004-engineering-review.md
005-remediation-report.md
006-final-completion-report.md
```

Use numeric prefixes for manual reports that record workflow steps, reviews, remediation, validation summaries, or completion status. The prefix is local to that feature report folder.

Generated importer/tool outputs should keep stable canonical names so commands, quickstarts, and repeated runs can target predictable files:

```text
mutashabihat-import-report.md
mutashabihat-import-report.json
validation-report.json
```

Do not rename historical reports retroactively unless a dedicated cleanup task explicitly asks for it. Do not renumber already-published reports just to insert a new step; append the next available number instead.
````

### Backend/AGENTS.md

Recommended insertion point: inside `## Backend Reports and Import Sources`, after the paragraph that starts `Use this location for backend implementation reports...` and before `Importer/source-data rules:`.

```markdown
For backend report filename conventions, follow `report/README.md`: from Feature 006 onward, human-authored reports use three-digit chronological prefixes, while generated importer/tool outputs keep stable canonical names. Do not rename old reports unless explicitly requested.
```

### Backend/CLAUDE.md

Recommended insertion point: same as Backend `AGENTS.md`.

```markdown
For backend report filename conventions, follow `report/README.md`: from Feature 006 onward, human-authored reports use three-digit chronological prefixes, while generated importer/tool outputs keep stable canonical names. Do not rename old reports unless explicitly requested.
```

## 6. Migration / Backward Compatibility

### Should Old Files Be Renamed?

No.

Do not rename existing Feature 002-005 reports by default. The current filenames are already referenced by existing docs, plans, reports, and possibly external discussion. Retroactive renaming would create avoidable git churn and link breakage risk without enough value.

### Feature 006 Onward

Start using numeric prefixes for new human-authored backend reports under:

```text
/projects/Dashboard/App/Backend/report/feature-006-quran-mutashabihat-foundation/
```

and for later feature folders.

Recommended examples:

```text
001-schema-implementation-report.md
002-importer-implementation-report.md
003-real-import-run-summary.md
004-engineering-review.md
005-remediation-report.md
006-final-completion-report.md
```

The exact report names can vary by feature, but the numeric prefix should preserve workflow order for manual reports.

### Generated Reports

Generated importer/tool outputs should not use chronological manual-report prefixes unless the tool itself is explicitly designed that way.

Keep stable canonical output names such as:

```text
mutashabihat-import-report.md
mutashabihat-import-report.json
validation-report.json
```

This keeps automation, quickstarts, reruns, and validation commands predictable.

## 7. Final Recommendation

Exact next action:

Update only these files in a follow-up instruction update task:

- `/projects/Dashboard/App/Backend/report/README.md`
- `/projects/Dashboard/App/Backend/AGENTS.md`
- `/projects/Dashboard/App/Backend/CLAUDE.md`

Do not update root workspace instruction files for this convention. Do not rename existing reports. Do not create a shared reference doc now.

Blockers: none.
