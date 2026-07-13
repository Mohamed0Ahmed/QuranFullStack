# docs/ — workspace planning & pre-Spec Kit documents

This folder is for **forward-looking workspace planning**: pre-Spec Kit reports, capability
studies, and decision addendums authored before or alongside a feature's specs. It is **not**
the current-truth layer. For contracts, `docs/contracts/` is a pointer index (also not the truth; it defers to code + the nearest README).

Where things live now:

- **Current truth of a code area** → the local `README.md` nearest that code
  (e.g. `Backend/README.md`, `Backend/infrastructure/.../MorphologyImporting/README.md`,
  `Frontend/quran-dashboard-ui/src/app/features/words/README.md`). Read the nearest one before
  changing an area. `docs/contracts/` indexes these READMEs and **defers to them — the README/code wins.**
- **Feature plans** → `specs/<feature>/` hosts active per-feature Spec-Kit planning (spec/plan/tasks/contracts); merged 001–019 are historical (their contracts removed). Current contract index → `docs/contracts/`.
- **How to work / how to write code** → `AGENTS.md` / `CLAUDE.md` / `.architecture/*`.
- **Evidence / reference** (audits, imports, diagnostics, DB inventory) → `Backend/report/`.

Prior per-feature planning docs were consolidated into the above and removed. Add a new
`docs/feature-XXX-<name>/` folder only for genuinely new pre-spec planning; do not recreate
the old feature-report indexes here.
