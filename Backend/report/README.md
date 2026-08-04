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
| `feature-XXX-*/` for a Quran import | Generated import acceptance reports (Markdown + JSON): source coverage, validation, exclusions, provenance warnings. Kept — see below. |
| `feature-*/` for any other feature | Phase and completion evidence. Deleted by the feature itself, in its last commit before merge. |

**Reviews, audits, plans, and structure inventories do not belong here.** They used to. They
were deleted on 2026-08-04 because a review records what was true on its date and cannot say
what is true now — and a fresh review can be run on demand at any time, which makes an old one
worth less than nothing once it starts being read as current. If you want a review, run one.

## Lifecycle — a feature deletes its own folder here

Per the planning-artifact lifecycle rule in `CLAUDE.md` §Workspace Path Conventions, a feature's
`Backend/report/feature-*/` folder is removed in the feature's **last commit before merge**,
after the engineering review passes. No buffer, no later sweep. Apply the per-file gate from
`CLAUDE.md` first: a fact not recoverable from code, tests, or a README goes into the nearest
README with a `file:LINE` from code proving it, and every inbound reference gets repointed.

**Evidence worth keeping becomes a test that fails on drift, not a report.** A canonical count,
source hash, or measured budget with nothing asserting it is a rumour. That rule has exactly one
standing exception, and it is why the two import folders survive:

- The `feature-008` (translations) and `feature-009` (navigation-metadata) reports are the only
  surviving record of source verification, per-source hashes, exclusions, and provenance
  warnings for those imports, and **the assertion has nowhere to live yet**: the canonical smoke
  dump (`Backend/scripts/create-smoke-dump`) pins only the five morphology baseline tables, so no
  tier can currently see a translations or navigation row count. `docs/TESTING_DEBT.md` row C5
  records the owed test; when it lands, these files go.
- Separately, `tools/QuranDashboard.DataImporter/Import/DefaultPaths/DataImporterDefaults.cs`
  hardcodes both directories as the importers' default output targets. That does not by itself
  protect the reports — the writers call `Directory.CreateDirectory` and would recreate an empty
  folder — but it does mean the next run writes into a directory whose history had been discarded.

## Filename conventions (for any future evidence report)

- Human-authored reports: three-digit chronological prefix + kebab-case name
  (`001-real-import-run-summary.md`), local to the report folder; append the next number,
  never renumber published reports.
- Generated importer/tool outputs: stable canonical names so commands can target them
  (`<domain>-import-report.md` / `.json`, `validation-report.json`).
- Do not rename historical reports retroactively unless a dedicated cleanup asks for it.
