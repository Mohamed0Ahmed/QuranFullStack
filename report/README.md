# Backend Reports

This folder holds **audit, investigation, readiness, verification, and review reports** for the
Quran Dashboard / المنهج القرآني backend — the *what was found / what was done* records produced
during or after implementation. Forward-looking **plans/design documents** live separately under
[`docs/`](../../docs/README.md) (workspace root).

Reports are grouped into one subfolder per feature/scope.

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

See `file-organization-report.md` (this folder) for the before/after move record.
