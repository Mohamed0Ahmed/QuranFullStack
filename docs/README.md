# docs/ — workspace planning & pre-Spec Kit documents

This folder is for **forward-looking workspace planning**: pre-Spec Kit reports, capability
studies, and decision addendums authored before or alongside a feature's specs. It is **not**
the current-truth layer.

Where things live now:

- **Current truth of a code area** → the local `README.md` nearest that code
  (e.g. `Backend/README.md`, `Backend/infrastructure/.../MorphologyImporting/README.md`,
  `Frontend/quran-dashboard-ui/src/app/features/words/README.md`). Read the nearest one before
  changing an area.
- **Feature plans / contracts** → `specs/<feature>/` (unchanged; specs are authoritative
  planning artifacts).
- **How to work / how to write code** → `AGENTS.md` / `CLAUDE.md` / `.architecture/*`.
- **Evidence / reference** (audits, imports, diagnostics, DB inventory) → `Backend/report/`.

Prior per-feature planning docs were consolidated into the above and removed. Add a new
`docs/feature-XXX-<name>/` folder only for genuinely new pre-spec planning; do not recreate
the old feature-report indexes here.
