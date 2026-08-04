# Backend Reports — durable reference & evidence only

This folder is **evidence/reference only**, not the current-truth layer. Reserve it for
durable, non-derivable records: audits, reviews, acceptance evidence, data-import outputs,
diagnostics, database inventory, source-safety checks, and one-off investigations. Current
truth of a code area lives in the local `README.md` near that code; plans/contracts live in
`specs/`; "how to work" lives in `AGENTS.md` / `.architecture/*`.

Do **not** spawn a per-feature report for routine work, and do not recreate the deleted
feature-report indexes.

## What lives here

`ls Backend/report/` is the inventory — this file deliberately keeps no folder list, because a
written one goes stale on the next feature close and then contradicts the tree. What each **kind**
of folder is for:

| Kind | Scope |
| --- | --- |
| Non-feature folders (`architecture/`, `database/`, `database-inventory/`) | Durable cross-cutting records: backend structure and target-structure reviews, the logging/observability foundation plan, the read-only DB baseline, and the live PostgreSQL inventory + reset/seeding-order runbook. Never swept. |
| `feature-XXX-*/` for a Quran import | Generated import acceptance reports (Markdown + JSON): source coverage, validation, exclusions, provenance warnings. Exempt from the sweep on the evidence ground below. |
| `feature-*/` for any other feature | Phase and completion evidence. Swept with the feature unless an individual file qualifies as evidence — the judgement is **per file**, not per folder. |

## Lifecycle — `feature-XXX-*/` folders die with their feature

Per the planning-artifact lifecycle rule in `CLAUDE.md` §Workspace Path Conventions, a
feature's `Backend/report/feature-XXX-*/` folder is deleted when the feature closes; only
the two most recently closed features plus every open one are kept, and **evidence is
judged per file** — a folder can lose its completion report and keep its import report.

`architecture/`, `database/`, and `database-inventory/` are non-feature folders and are
never swept.

The two `feature-008` / `feature-009` folders above are **permanently exempt** on two
independent grounds:

- `tools/QuranDashboard.DataImporter/Import/DefaultPaths/DataImporterDefaults.cs`
  hardcodes both directories as the importers' default output targets — deleting them
  breaks `import-translations` and `import-navigation-metadata`.
- Their reports are the only surviving record of source verification, per-source hashes,
  exclusions, and provenance warnings for those imports. `database-inventory/` supersedes
  **counts** only; it never supersedes source-verification, exclusion, or provenance facts.

## Filename conventions (for any future evidence report)

- Human-authored reports: three-digit chronological prefix + kebab-case name
  (`001-real-import-run-summary.md`), local to the report folder; append the next number,
  never renumber published reports.
- Generated importer/tool outputs: stable canonical names so commands can target them
  (`<domain>-import-report.md` / `.json`, `validation-report.json`).
- Do not rename historical reports retroactively unless a dedicated cleanup asks for it.
