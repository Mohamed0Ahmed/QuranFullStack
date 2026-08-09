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
- **How to work / how to write code** → the agent's native root router (`CLAUDE.md` for
  Claude, `AGENTS.md` for Sol/Codex), its native area router when applicable, the nearest
  `README.md`, and only the triggered neutral or specialist source. The root files are routers,
  not duplicated law books.
- **Which tests to run and when** → `TESTING_STRATEGY.md` (workspace root) — the `Backend/scripts/test-backend` and `npm run test:*` lanes, the execution-trigger matrix, pipeline triggers, and the PR/release gates. Not a planning doc and not superseded by anything here.
- **Which tests were deliberately not written** → `docs/TESTING_DEBT.md` — one row per skipped area, each naming the concrete change that pays it. Not a place to defer a lane `TESTING_STRATEGY.md` requires, and never a home for `SmokeRouteCatalog` parity entries (those are a build-level gate).
- **Evidence / reference** (import verification, source hashes, provenance) → `Backend/report/`.
- **Frontend reports** → no convention is established. Do not invent a Frontend report folder
  without an explicit decision.
- **A browsable HTTP API reference** → not committed. Generate it on demand from
  `Frontend/quran-dashboard-ui/` with `npm run docs:api`, which writes
  `docs/api-reference/index.html`. It used to be committed and nobody regenerated it, which made
  it stale data wearing a contract's clothes.
- **How to rebuild the local database** → `Backend/scripts/README.md`.

Add a new `docs/feature-XXX-<name>/` folder only for genuinely new pre-spec planning; do
not recreate the old feature-report indexes here.

## Lifecycle — a feature deletes its own planning artifacts before it merges

A feature's `specs/<feature>/`, `docs/feature-*/`, and
`Backend/report/feature-*/` are working files. The feature removes them in its **last commit
before merge**. There is no buffer, grace period, or later cleanup sweep: only open features
have planning artifacts in the tree.

The deletion commit comes **after the engineering review passes**, never before, because the
review compares the work against the plan. It is pure deletion: README amendments already land
with the work they describe, so the steady-state READMEs are true before this commit runs.

Before deleting, apply this gate to every file: **does it assert a fact that is not recoverable
from code, tests, or an existing README?**

- **No** → delete it.
- **Yes** → write the fact into the nearest README, prove it from code with a `file:LINE`,
  repoint every inbound reference, then delete it. Never fold a claim that code cannot confirm;
  an unprovable planning claim is dropped rather than promoted to current truth.

Evidence worth keeping becomes a test that fails on drift, not a report. A canonical count,
source hash, or measured budget with nothing asserting it is a rumour. If the assertion has no
home yet, keep that file and record in `docs/TESTING_DEBT.md` what the test must assert and where
it must go.

Repoint before deleting. Search the whole repository — code, tests, `.claude/`, `.agents/`,
`.specify/`, scripts, manifests, and every README — for each path being removed. A dangling
reference blocks deletion.

## Long-lived documentation

Anything not in this exact list is feature-scoped and dies with its feature:

- every `README.md` anywhere in the repository;
- root and per-project law: `CLAUDE.md`, `AGENTS.md`, `CODING_PRINCIPLES.md`,
  `TESTING_STRATEGY.md`, `PRODUCT.md`, `DESIGN.md`, `SKILLS_AND_ARCHITECTURE_GUIDE.md`, and
  their Backend/Frontend counterparts;
- all `.architecture/**` documents;
- `docs/contracts/**`, the pointer index;
- `docs/TESTING_DEBT.md`, the live testing-debt ledger;
- everything under `.claude/`, `.agents/`, and `.specify/`, plus all code, tests, and
  configuration.
