# Backend Reports — durable reference & evidence only

This folder is **evidence/reference only**, not the current-truth layer. Reserve it for
durable, non-derivable records: audits, reviews, acceptance evidence, data-import outputs,
diagnostics, database inventory, source-safety checks, and one-off investigations. Active feature
intent and contracts live in `specs/`; implemented truth lives in code. Route through
`Backend/CLAUDE.md` for Claude or `Backend/AGENTS.md` for Sol/Codex, then load only the triggered
`.architecture/*` source.

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

**There is no database-baseline report and there should not be one.** `database/` held a
2026-06-17 snapshot of table shapes and row counts; by the time it was deleted the tree had
twice as many migrations and two schema areas the report had never heard of, and a skill was
still citing it as an input. Schema truth is the EF configurations under
`Persistence/Configurations/` plus `Migrations/`; live cardinality is measured in a read-only
session and dated when reported.

**Reviews, audits, plans, and structure inventories do not belong here.** They used to. They
were deleted on 2026-08-04 because a review records what was true on its date and cannot say
what is true now — and a fresh review can be run on demand at any time, which makes an old one
worth less than nothing once it starts being read as current. If you want a review, run one.

## Lifecycle — a feature deletes its own folder here

The shared deletion timing, per-file preservation gate, evidence-to-test rule, survivor list,
and inbound-reference gate live in
[`docs/README.md` §Lifecycle](../../docs/README.md#lifecycle--a-feature-deletes-its-own-planning-artifacts-before-it-merges).
They apply to `Backend/report/feature-*/`; this README adds only the report-specific exceptions below.

**Evidence worth keeping becomes a test that fails on drift, not a report.** A canonical count,
source hash, or measured budget with nothing asserting it is a rumour. That rule has exactly one
standing exception, and it is why the two import folders survive:

- The `feature-008` (translations) and `feature-009` (navigation-metadata) reports are the only
  surviving record of source verification, per-source hashes, exclusions, and provenance
  warnings for those imports, and **the assertion has nowhere to live yet**: the canonical smoke
  dump (`Backend/scripts/create-smoke-dump`) pins only the five morphology baseline tables, so no
  tier can currently see a translations or navigation row count. Under the Test Freeze, these
  reports remain the evidence unless an owner-approved retained gate supersedes them.
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
