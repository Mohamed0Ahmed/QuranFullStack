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

The shared deletion timing, per-file preservation gate, evidence-to-test rule, long-lived
survivor list, and inbound-reference gate live in
[`docs/README.md` §Lifecycle](../docs/README.md#lifecycle--a-feature-deletes-its-own-planning-artifacts-before-it-merges).
Follow that canonical lifecycle before applying the Spec Kit-specific close steps below.

### Closing a feature — checklist

1. **Engineering review passes.** It compares the work against the plan, so nothing may be
   deleted before it runs.
2. **Acceptance** recorded — quickstart/exit gates run against the final tree.
3. **Complete the shared lifecycle gates** in
   [`docs/README.md` §Lifecycle](../docs/README.md#lifecycle--a-feature-deletes-its-own-planning-artifacts-before-it-merges).
4. **Delete `specs/<feature>/`, `docs/feature-*/`, and `Backend/report/feature-*/`** in one
   commit, and clear the **Active Spec Kit Feature** section of `CLAUDE.md` / `AGENTS.md` in
   the same commit.

## Current / steady-state truth lives elsewhere

After a feature merges, implemented contract truth is the **code**, indexed where useful by the
thin pointer layer [`../docs/contracts/`](../docs/contracts/README.md). Per-feature,
planning-time contracts live in `specs/<feature>/contracts/` during development.
