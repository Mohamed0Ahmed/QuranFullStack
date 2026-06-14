# Backend Reports

This folder holds **audit, investigation, readiness, verification, and review reports** for the
Quran Dashboard / المنهج القرآني backend — the *what was found / what was done* records produced
during or after implementation. Forward-looking **plans/design documents** live separately under
[`docs/`](../../docs/README.md) (workspace root).

Reports are grouped into one subfolder per feature/scope.

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

## Layout

| Folder | Scope |
| --- | --- |
| `feature-002-quran-foundation/` | Quran foundation import (`002-mushaf-words-foundation`): source-readiness and import data investigations. |
| `feature-003-word-display-tables/` | Feature 003 display/statistics tables: word-source normalization and unique-table audits. |
| `feature-003-imlaei-clean-key/` | Imlaei clean identity key (`word_key_imlaei_simple`) enrichment + import binding. |
| `feature-003-word-identity-links/` | Word identity links restructure analysis (unique-simple by clean imlaei key + `quran_words` link columns). |

## Contents

- `feature-002-quran-foundation/`
  - `quran-foundation-import-source-readiness-report.md`
  - `ayah-37-130-word-count-investigation.md`
- `feature-003-word-display-tables/`
  - `word-import-source-normalization-audit-report.md`
  - `words-unique-tables-audit-report.md`
- `feature-003-imlaei-clean-key/`
  - `imlaei-clean-import-binding-report.md`
- `feature-003-word-identity-links/`
  - `feature-003-word-identity-links-restructure-report.md`
  - `phase-1-unique-simple-identity-switch-report.md`
  - `phase-7-dev-reset-reseed-report.md`

See `file-organization-report.md` (this folder) for the before/after move record.
