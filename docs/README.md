# docs/ — workspace planning & pre-Spec Kit documents

This folder is for **forward-looking workspace planning**: pre-Spec Kit reports, capability
studies, and decision addendums authored before or alongside a feature's specs. It is **not**
the current-truth layer. For contracts, `docs/contracts/` is a pointer index (also not the truth; it defers to code + the nearest README).

Where things live now:

- **Current truth of a code area** → the local `README.md` nearest that code
  (e.g. `Backend/README.md`, `Backend/infrastructure/.../MorphologyImporting/README.md`,
  `Frontend/quran-dashboard-ui/src/app/features/words/README.md`). Read the nearest one before
  changing an area. `docs/contracts/` indexes these READMEs and **defers to them — the README/code wins.**
- **Feature plans** → `specs/<feature>/` hosts per-feature Spec-Kit planning (spec/plan/tasks/contracts) for open features plus the N-2 buffer. Current contract index → `docs/contracts/`.
- **How to work / how to write code** → `AGENTS.md` / `CLAUDE.md` / `.architecture/*`.
- **Evidence / reference** (audits, imports, diagnostics, DB inventory) → `Backend/report/`.

Add a new `docs/feature-XXX-<name>/` folder only for genuinely new pre-spec planning; do
not recreate the old feature-report indexes here.

## Lifecycle — `docs/feature-XXX-*/` dies with its feature

Per the planning-artifact lifecycle rule in `CLAUDE.md` §Workspace Path Conventions, a
feature's `docs/feature-XXX-*/` folder is deleted from the working tree when the feature
closes; only the **two most recently closed** features plus every open one are kept. Git
history is the archive. Before deleting, repoint every inbound reference into the nearest
`README.md`, and promote any fact that no live document restates.

Non-feature folders here are **not** subject to the sweep and stay indefinitely:
`contracts/`, `api-reference/`, `deployment-railway/`, `design-preview/`,
`performance-review/`, and cross-cutting audits kept at the top level of `docs/`.

Currently buffered: `feature-033-auth-roles-permissions/` (closed 2026-07-19) and
`feature-032-rate-limiting/` (closed 2026-07-18).
