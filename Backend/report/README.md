# Backend Reports — durable reference & evidence only

This folder is **evidence/reference only**, not the current-truth layer. Reserve it for
durable, non-derivable records: audits, reviews, acceptance evidence, data-import outputs,
diagnostics, database inventory, source-safety checks, and one-off investigations. Current
truth of a code area lives in the local `README.md` near that code; plans/contracts live in
`specs/`; "how to work" lives in `AGENTS.md` / `.architecture/*`.

Do **not** spawn a per-feature report for routine work, and do not recreate the deleted
feature-report indexes.

## What lives here now

| Folder | Scope |
| --- | --- |
| `architecture/` | Backend structure inventory, target-structure feasibility/execution/review, and the global logging/observability foundation plan. |
| `database/` | `current-database-tables-and-relationships-report.md` — read-only DB baseline. |
| `database-inventory/` | Live PostgreSQL inventory + the DB reset/seeding-order runbook. |
| `feature-008-quran-translations-foundation/` | Generated Quran translation import acceptance reports (Markdown + JSON), including source coverage, validation, exclusions, and provenance warnings. |
| `feature-009-quran-navigation-metadata-foundation/` | Generated Quran navigation metadata import acceptance reports (Markdown + JSON) for juz, hizb, rub, sajda, and ayah coverage validation. |

## Filename conventions (for any future evidence report)

- Human-authored reports: three-digit chronological prefix + kebab-case name
  (`001-real-import-run-summary.md`), local to the report folder; append the next number,
  never renumber published reports.
- Generated importer/tool outputs: stable canonical names so commands can target them
  (`<domain>-import-report.md` / `.json`, `validation-report.json`).
- Do not rename historical reports retroactively unless a dedicated cleanup asks for it.
