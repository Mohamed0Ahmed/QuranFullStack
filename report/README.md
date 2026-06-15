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
| `feature-003-quran-words-foundation/` | Feature 003 Quran words foundation: display-table audits, imlaei clean identity key binding, and word identity links restructure/phase reports. |
| `feature-004-word-morphology-foundation/` | Feature 004 word morphology/segment foundation: completion record. |
| `feature-005-word-simple-i3rab-foundation/` | Feature 005 simplified إعراب foundation: rule-coverage inventory, label inventory, and planning-sync review. |
| `feature-006-quran-mutashabihat-foundation/` | Feature 006 متشابهات / similar-ayah foundation: implementation verification, import-run summary, source-safety, scope, and completion reports. |
| `feature-007-quran-tafsir-foundation/` | Feature 007 tafsir import foundation: numbered verification/validation/check reports (US1–US4, build, test, quickstart, architecture, source-safety, scope). |
| `database-inventory/` | Cross-feature database reference: live PostgreSQL inventory and the reset/seeding-order runbook. |

Each feature folder carries its own `README.md` index. From Feature 006 onward, human-authored
reports use numeric prefixes (see *Filename Conventions* above); Feature 002–005 reports keep their
original content-named filenames; Feature 003 was normalized to numeric prefixes as a dedicated task.

## Contents

- `feature-002-quran-foundation/` — `README.md` + 2 reports
- `feature-003-quran-words-foundation/` — `README.md` + `001`…`006` reports
- `feature-004-word-morphology-foundation/` — `README.md` + `final-completion-report.md`
- `feature-005-word-simple-i3rab-foundation/` — `README.md` + 3 reports
- `feature-006-quran-mutashabihat-foundation/` — `README.md` + `001`…`005` reports
- `feature-007-quran-tafsir-foundation/` — `README.md` + `001`…`010` reports
- `database-inventory/` — `current-database-inventory.md`, `database-reset-and-seeding-order.md`
