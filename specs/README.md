# specs/ — per-feature Spec-Kit planning workspace

`specs/<feature>/` holds each feature's Spec-Kit planning artifacts. For an **active**
feature under development, its `spec.md`, `plan.md`, `tasks.md`, `data-model.md`,
`research.md`, `quickstart.md`, `checklists/`, and `contracts/` are **live planning
inputs** — the Spec-Kit implementation-review compares the in-progress work against the
feature's own `specs/<feature>/contracts/`. New features still populate
`specs/<feature>/contracts/` during development.

## Lifecycle — a feature's specs die with the feature

`specs/<feature>/` is a **working** artifact, not an archive. When a feature closes, its
folder is deleted from the working tree; git history keeps it. Only the **two most
recently closed** features stay as a buffer, plus every open feature.

Merged features 001–019 and 026 were removed by the 2026-07-27 lifecycle sweep (001–019
had already lost their `contracts/` during Feature 024). Their planning artifacts are in
git history; nothing in the working tree should link to them.

### Closing a feature — checklist

1. **Merge** the feature branch into `dev` (PR merged, branch state clean).
2. **Acceptance** recorded — quickstart/exit gates run against the final tree; any
   completion or validation report written under `Backend/report/feature-XXX-*/`.
3. **Promote the evidence.** Move anything that must outlive the feature into a live home:
   current truth → the nearest `README.md`; contracts → the code + `docs/contracts/`
   index; durable evidence (import verification, measured budgets backing a live
   assertion, safety inventories) → keep the file and note *why* it is exempt.
4. **Repoint inbound references.** `grep -rn` for every path about to be removed — code,
   tests, skills, data files, READMEs, `.specify/feature.json`. Repoint or inline each
   hit. A dangling link blocks the delete.
5. **Delete the N-2 buffer overflow.** With this feature closed, the feature that was
   third-most-recent loses its `specs/<feature>/`, `docs/feature-XXX-*/`, and
   `Backend/report/feature-XXX-*/` — minus the files exempted at step 3.
6. **Update the folder charters** if a listed folder disappeared: `specs/README.md`,
   `docs/README.md`, `Backend/report/README.md`'s "What lives here now" table, and the
   **Active Spec Kit Feature** section of `CLAUDE.md` / `AGENTS.md`.

## Current / steady-state truth lives elsewhere

After a feature merges, the current contract truth is the **code** + the **nearest
`README.md`**, indexed by the thin pointer layer
[`../docs/contracts/`](../docs/contracts/README.md). That index covers steady-state
truth; per-feature, planning-time contracts live in `specs/<feature>/contracts/` during a
feature's development.
