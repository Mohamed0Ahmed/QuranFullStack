# specs/ — per-feature Spec-Kit planning workspace

`specs/<feature>/` holds each feature's Spec-Kit planning artifacts. For an **active**
feature under development, its `spec.md`, `plan.md`, `tasks.md`, `data-model.md`,
`research.md`, `quickstart.md`, `checklists/`, and `contracts/` are **live planning
inputs** — the Spec-Kit implementation-review compares the in-progress work against the
feature's own `specs/<feature>/contracts/`. New features still populate
`specs/<feature>/contracts/` during development.

## Merged features 001–019 are historical (frozen)

The 001–019 feature folders are merged and **frozen** — do not scan them routinely.
**Only their `contracts/` subfolders were removed** during Feature 024; every other
artifact (`spec`/`plan`/`tasks`/`data-model`/`research`/`quickstart`/`checklists`, and
`002/source-provenance.md`) stays as the historical record. Their archived documents may
still link to the removed `contracts/` paths — those links are historical and
intentionally not maintained.

## Current / steady-state truth lives elsewhere

After a feature merges, the current contract truth is the **code** + the **nearest
`README.md`**, indexed by the thin pointer layer
[`../docs/contracts/`](../docs/contracts/README.md). That index covers steady-state
truth; per-feature, planning-time contracts live in `specs/<feature>/contracts/` during a
feature's development.
