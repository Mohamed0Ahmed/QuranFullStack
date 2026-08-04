# specs/ — per-feature Spec-Kit planning workspace

`specs/<feature>/` holds each feature's Spec-Kit planning artifacts. For an **active**
feature under development, its `spec.md`, `plan.md`, `tasks.md`, `data-model.md`,
`research.md`, `quickstart.md`, `checklists/`, and `contracts/` are **live planning
inputs** — the Spec-Kit implementation-review compares the in-progress work against the
feature's own `specs/<feature>/contracts/`. New features still populate
`specs/<feature>/contracts/` during development.

## Lifecycle — a feature's specs die with the feature, in the feature's own last commit

`specs/<feature>/` is a **working** artifact, not an archive. The feature deletes its own
folder in its **last commit before merge**; git history is the archive. There is no buffer
and no later sweep, so a `specs/<feature>/` in the tree means that feature is **open**.

### Closing a feature — checklist

1. **Engineering review passes.** It compares the work against the plan, so nothing may be
   deleted before it runs.
2. **Acceptance** recorded — quickstart/exit gates run against the final tree.
3. **Apply the per-file gate** (`CLAUDE.md` §Workspace Path Conventions) to every planning
   file: does it assert a fact not recoverable from code, tests, or an existing README?
   No → it goes. Yes → write the fact into the nearest `README.md` and **prove it from code
   with a `file:LINE`** before deleting. A claim you cannot confirm in code is not folded —
   it is dropped, and said out loud.
4. **Turn evidence into a test, not a report.** A canonical count, hash, or measured budget
   that nothing asserts is a rumour. If it has nowhere to be asserted yet, keep that one
   file and add the owed assertion to `docs/TESTING_DEBT.md`.
5. **Repoint inbound references.** `grep -rn` for every path about to be removed — code,
   tests, `.claude/`, `.agents/`, `.specify/`, scripts, manifests, READMEs. A dangling link
   blocks the delete.
6. **Delete `specs/<feature>/`, `docs/feature-*/`, and `Backend/report/feature-*/`** in one
   commit, and clear the **Active Spec Kit Feature** section of `CLAUDE.md` / `AGENTS.md` in
   the same commit.

## Current / steady-state truth lives elsewhere

After a feature merges, the current contract truth is the **code** + the **nearest
`README.md`**, indexed by the thin pointer layer
[`../docs/contracts/`](../docs/contracts/README.md). That index covers steady-state
truth; per-feature, planning-time contracts live in `specs/<feature>/contracts/` during a
feature's development.
