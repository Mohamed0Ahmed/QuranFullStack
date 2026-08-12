# docs/ — workspace planning & pre-Spec Kit documents

This folder is for **forward-looking workspace planning**: pre-Spec Kit reports, capability
studies, and decision addendums authored before or alongside a feature's specs. It is **not**
the current-truth layer. For contracts, `docs/contracts/` is a pointer index that defers to code
and the applicable architecture authorities.

Where things live now:

- **Implemented truth of a code area** → the code itself. `docs/contracts/` may index the owning
  code and architecture source but never overrides them.
- **Feature plans** → `specs/<feature>/` hosts per-feature Spec-Kit planning
  (spec/plan/tasks/contracts) for **open features only**. These artifacts own active feature intent.
  Current contract index → `docs/contracts/`.
- **How to work / how to write code** → the agent's native root router (`CLAUDE.md` for
  Claude, `AGENTS.md` for Sol/Codex), its native area router when applicable, and only the
  triggered neutral or specialist source. The root files are routers, not duplicated law books.
- **Which tests to run and when** → `TESTING_CONSTITUTION.md` is the sole policy authority;
  Backend lane and fixture mechanics live in `Backend/tests/QuranDashboard.Tests/README.md`, and
  browser-journey mechanics live in `Frontend/quran-dashboard-ui/e2e/README.md`.
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
review compares the work against the plan. It is pure deletion: implementation and operational
documentation changes already land with the work they describe.

Before deleting, apply this gate to every file: **does it assert a fact that is not recoverable
from code, tests, or an existing governing authority?**

- **No** → delete it.
- **Yes** → use the smallest appropriate **existing** authority only when the fact is truly
  permanent and belongs there, prove it from code with a `file:LINE`, repoint every inbound
  reference, then delete it. Never create a code-area README or fold an unprovable planning claim
  into current truth; drop such a claim instead.

Evidence worth keeping becomes an existing retained gate that fails on drift, not a report. A
canonical count, source hash, or measured budget with nothing asserting it is a rumour. If the
assertion has no approved executable home, keep the evidence file or drop an unprovable planning
claim; the Test Freeze does not maintain a ledger of coverage that was deliberately not written.

Repoint before deleting. Search the whole repository — code, tests, `.claude/`, `.agents/`,
`.specify/`, scripts, manifests, and retained operational READMEs — for each path being removed. A dangling
reference blocks deletion.

## Long-lived documentation

Anything not in this exact list is feature-scoped and dies with its feature:

- operational, tooling, test, docs, scripts, setup/deployment, and provenance `README.md` files;
- no application/code-area README is part of the long-lived documentation model;
- root and per-project law: `CLAUDE.md`, `AGENTS.md`, `CODING_PRINCIPLES.md`,
  `TESTING_CONSTITUTION.md`, `PRODUCT.md`, `DESIGN.md`, `SKILLS_AND_ARCHITECTURE_GUIDE.md`, and
  their Backend/Frontend counterparts;
- all `.architecture/**` documents;
- `docs/contracts/**`, the pointer index;
- everything under `.claude/`, `.agents/`, and `.specify/`, plus all code, tests, and
  configuration.
