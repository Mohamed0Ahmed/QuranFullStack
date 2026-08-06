# docs/ — workspace planning & pre-Spec Kit documents

This folder is for **forward-looking workspace planning**: pre-Spec Kit reports, capability
studies, and decision addendums authored before or alongside a feature's specs. It is **not**
the current-truth layer. For contracts, `docs/contracts/` is a pointer index (also not the truth; it defers to code + the nearest README).

Where things live now:

- **Current truth of a code area** → the local `README.md` nearest that code
  (e.g. `Backend/README.md`, `Backend/infrastructure/.../MorphologyImporting/README.md`,
  `Frontend/quran-dashboard-ui/src/app/features/words/README.md`). Read the nearest one before
  changing an area. `docs/contracts/` indexes these READMEs and **defers to them — the README/code wins.**
- **Feature plans** → `specs/<feature>/` hosts per-feature Spec-Kit planning (spec/plan/tasks/contracts) for **open features only**. Current contract index → `docs/contracts/`.
- **How to work / how to write code** → `AGENTS.md` / `CLAUDE.md` / `.architecture/*`.
- **Which tests to run and when** → `TESTING_STRATEGY.md` (workspace root) — the `Backend/scripts/test-backend` and `npm run test:*` lanes, the execution-trigger matrix, pipeline triggers, and the PR/release gates. Not a planning doc and not superseded by anything here.
- **Which tests were deliberately not written** → `docs/TESTING_DEBT.md` — one row per skipped area, each naming the concrete change that pays it. Not a place to defer a lane `TESTING_STRATEGY.md` requires, and never a home for `SmokeRouteCatalog` parity entries (those are a build-level gate).
- **Evidence / reference** (import verification, source hashes, provenance) → `Backend/report/`.
- **A browsable HTTP API reference** → not committed. Generate it on demand from
  `Frontend/quran-dashboard-ui/` with `npm run docs:api`, which writes
  `docs/api-reference/index.html`. It used to be committed and nobody regenerated it, which made
  it stale data wearing a contract's clothes.
- **How to rebuild the local database** → `Backend/scripts/README.md`.

Add a new `docs/feature-XXX-<name>/` folder only for genuinely new pre-spec planning; do
not recreate the old feature-report indexes here.

## Lifecycle — a feature deletes its own folder here before it merges

Per the planning-artifact lifecycle rule in `CLAUDE.md` §Workspace Path Conventions, a feature's
`docs/feature-*/` folder is removed in the feature's **last commit before merge**, after the
engineering review passes. There is no buffer and no later sweep, so `ls -d docs/feature-*/`
should list **open features only** — anything else in that listing is a feature that skipped its
deletion commit.

Before deleting, apply the per-file gate from `CLAUDE.md`: a file asserting something no code,
test, or README already says gets that fact written into the nearest README — proved from code
with a `file:LINE` — and every inbound reference repointed. Everything else just goes.

What survives here long-term is only what the survivor list in `CLAUDE.md` names: this README,
`contracts/`, and `TESTING_DEBT.md`. That list lives in one place so it cannot drift against a
second copy.
