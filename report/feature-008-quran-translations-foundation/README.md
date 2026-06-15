# Feature 008 — Quran Translations Foundation: Backend Reports

This folder holds the **backend implementation, verification, validation, and completion reports**
for Feature 008 (Quran Translations Foundation). It is the *what was done / what was verified* record for the
translation import work on branch `008-quran-translations-foundation`.

Spec Kit artifacts (spec, plan, tasks, contracts, quickstart) live under
`specs/008-quran-translations-foundation/`. Forward-looking planning lives under
`docs/feature-008-quran-translations-foundation/`. **This folder is only for backend reports.**

## Where reports go

- **Human-authored reports** (verification, review, remediation, completion) belong **here**:
  `Backend/report/feature-008-quran-translations-foundation/`
  (absolute: `/projects/Dashboard/App/Backend/report/feature-008-quran-translations-foundation/`).
- **Importer/tool-generated reports** (`translation-import-report.md`, `translation-import-report.json`) default to
  `/projects/Dashboard/App/resources/report/quran-translations/` when the importer is run **without** an explicit
  `--report-out` directory. `resources/` is local and gitignored, so generated import reports are **not**
  committed; copy any generated report you want to keep into this folder.

## Filename conventions

Follow `Backend/report/README.md`:

- Human-authored reports use a three-digit chronological prefix plus a kebab-case name, local to this folder.
- Generated importer outputs keep stable canonical names so commands and quickstarts can target predictable
  files: `translation-import-report.md` and `translation-import-report.json`.
- Do not rename or renumber already-published reports; append the next available number instead.

## Planned report index

These reports are produced by later phases of `tasks.md` (filled in as each phase completes):

| File | Produced by | Scope |
| --- | --- | --- |
| `001-implementation-scope.md` | T005, T019 | Implementation guardrails and planned DI mapping |
| `002-schema-and-importer-implementation-report.md` | T076 | Schema, import flow, and changed paths |
| `003-validation-and-reporting-verification.md` | T077 | All `TR-*` checks and report files |
| `004-source-safety-and-scope-check.md` | T078 | Source-package safety and scope boundaries |
| `005-test-verification.md` | T079 | Feature 008 test subset results |
| `006-build-verification.md` | T080 | Full backend build result |
| `007-quickstart-validation.md` | T081 | Quickstart CLI validation |
| `008-clean-code-self-check.md` | T082 | Clean-code guard self-check |
| `009-test-code-self-check.md` | T083 | Test-code quality self-check |
| `010-final-scope-check.md` | T084 | Final scope and git-diff verification |

## Report index (complete)

| Report | Status | Summary |
| --- | --- | --- |
| [001-implementation-scope.md](./001-implementation-scope.md) | DRAFT | Phase 2 foundational — guardrails and planned DI mapping |

> Status: Phase 2 foundational complete (2026-06-15). Migration `AddQuranTranslations` generated; `database update` not run.
