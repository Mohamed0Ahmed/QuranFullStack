# تحسينات الواجهة / UI Improvements

This folder holds **report-only** documentation for UI polish work on the Quran
Dashboard frontend. These reports capture observed UI issues, root-cause analysis,
desired UX behavior, and phased implementation plans — **without** implementing
code as part of the report itself.

UI improvement reports are intentionally separate from feature work. They are **not**
Spec Kit artifacts and must **not** live under `specs/` or any `docs/feature-XXX-*/`
folder. A report here describes what should change; actual implementation happens
later under its own approved task (and, if it grows into a feature, its own Spec Kit
flow).

## Purpose

- Give UI polish work a stable home and a predictable numbering scheme.
- Record the *why* and *what* of each improvement before any code is written.
- Keep a single index so the state of every UI improvement is visible at a glance.

## Numbering and file naming

Each UI improvement gets its own numbered report file.

- **Report ID format:** `UI-001`, `UI-002`, `UI-003`, … (zero-padded to three digits).
- **File naming format:** `001-short-kebab-case-title-report.md`
  - The leading number matches the report ID (`UI-001` → `001-...`).
  - The slug is a short, kebab-case description of the improvement.
  - The file always ends with `-report.md`.

One report = one file = one UI improvement.

## Status values

A report's status is one of:

| Status        | Meaning                                                            |
| ------------- | ----------------------------------------------------------------- |
| `Reported`    | Issue documented and analyzed; no implementation work started.    |
| `Planned`     | Implementation approach agreed; scheduled but not yet started.    |
| `In Progress` | Implementation underway.                                          |
| `Implemented` | Code changes complete.                                            |
| `Reviewed`    | Implementation reviewed and accepted.                             |

## Scope rules

- **UI-only by default.** Reports here cover frontend UI and frontend state/UX only,
  unless a report explicitly states otherwise.
- **No backend changes** unless a separate, approved task says so.
- **No API contract changes**, **no database / migration changes**, and
  **no Quranic data changes** as part of UI improvement work.
- **No Spec Kit artifacts** are created from these reports.

If an improvement turns out to require backend, API, database, or Quranic-data
changes, that is out of scope for this folder and must be raised as its own task.

## Index

| ID     | Title                                                          | File                                            | Status   | Area                          |
| ------ | -------------------------------------------------------------- | ----------------------------------------------- | -------- | ----------------------------- |
| UI-001 | Stable Loading & No Layout Shift for Mushaf Selection Panels   | `001-stable-loading-layout-shift-report.md`     | Reported | Mushaf Reader / Study Panels  |
