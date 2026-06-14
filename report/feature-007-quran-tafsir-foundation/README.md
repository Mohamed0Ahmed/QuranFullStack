# Feature 007 — Quran Tafsir Foundation: Backend Reports

This folder holds the **backend implementation, verification, validation, and completion reports**
for Feature 007 (Quran Tafsir Foundation). It is the *what was done / what was verified* record for the
tafsir import work on branch `007-quran-tafsir-foundation`.

Spec Kit artifacts (spec, plan, tasks, contracts, quickstart) live under
`specs/007-quran-tafsir-foundation/`. Forward-looking planning lives under
`docs/feature-007-quran-tafsir-foundation/`. **This folder is only for backend reports.**

## Where reports go

- **Human-authored reports** (verification, review, remediation, completion) belong **here**:
  `Backend/report/feature-007-quran-tafsir-foundation/`
  (absolute: `/projects/Dashboard/App/Backend/report/feature-007-quran-tafsir-foundation/`).
- **Importer/tool-generated reports** (`tafsir-import-report.md`, `tafsir-import-report.json`) default to
  `/projects/Dashboard/App/resources/report/quran-tafsirs/` when the importer is run **without** an explicit
  `--report-out` directory. `resources/` is local and gitignored, so generated import reports are **not**
  committed; copy any generated report you want to keep into this folder.

## Filename conventions

Follow `Backend/report/README.md`:

- Human-authored reports use a three-digit chronological prefix plus a kebab-case name, local to this folder.
- Generated importer outputs keep stable canonical names so commands and quickstarts can target predictable
  files: `tafsir-import-report.md` and `tafsir-import-report.json`.
- Do not rename or renumber already-published reports; append the next available number instead.

## Planned report index

These reports are produced by later phases of `tasks.md` (filled in as each phase completes):

| File | Produced by | Scope |
| --- | --- | --- |
| `001-us1-import-foundation-verification.md` | T032 | User Story 1 import verification |
| `002-us2-integrity-verification.md` | T042 | User Story 2 integrity/refusal verification |
| `003-us3-rerun-verification.md` | T050 | User Story 3 safe re-run / force verification |
| `004-us4-reporting-verification.md` | T061 | User Story 4 audit-report verification |
| `005-build-verification.md` | T062 | Full backend build result |
| `006-test-verification.md` | T063 | Full backend test suite result |
| `007-quickstart-validation.md` | T064 | Quickstart CLI validation |
| `008-architecture-self-check.md` | T065 | Clean Architecture / structure self-check |
| `009-source-safety-check.md` | T066 | Source-package and Quran-foundation safety check |
| `010-scope-check.md` | T067 | No API/frontend/public-reader/search/seeding scope creep |

> Status: Phase 1 (Setup) scaffolding. Implementation reports are added as their phases complete.
