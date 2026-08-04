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
- **Which tests to run and when** → `TESTING_STRATEGY.md` (workspace root) — execution tiers A–E, pipeline triggers, and the PR/release gates. Not a planning doc and not superseded by anything here.
- **Which tests were deliberately not written** → `docs/TESTING_DEBT.md` — one row per skipped area, each naming the concrete change that pays it. Not a place to defer a tier `TESTING_STRATEGY.md` requires, and never a home for `SmokeRouteCatalog` parity entries (those are a build-level gate).
- **Evidence / reference** (import verification, source hashes, provenance) → `Backend/report/`.
- **A browsable HTTP API reference** → not committed. Generate it on demand from
  `Frontend/quran-dashboard-ui/` with `npm run docs:api`, which writes
  `docs/api-reference/index.html`. It used to be committed and nobody regenerated it, which made
  it stale data wearing a contract's clothes.
- **How to rebuild the local database** → `Backend/scripts/README.md`.

Add a new `docs/feature-XXX-<name>/` folder only for genuinely new pre-spec planning; do
not recreate the old feature-report indexes here.

## Lifecycle — `docs/feature-XXX-*/` dies with its feature

Per the planning-artifact lifecycle rule in `CLAUDE.md` §Workspace Path Conventions, a
feature's `docs/feature-XXX-*/` folder is deleted from the working tree when the feature
closes; only the **two most recently closed** features plus every open one are kept. Git
history is the archive. Before deleting, repoint every inbound reference into the nearest
`README.md`, and promote any fact that no live document restates.

Non-feature folders here are **not** subject to the sweep and stay indefinitely. `CLAUDE.md`
§Workspace Path Conventions holds the authoritative never-deleted list; this file defers to it
rather than repeating it, so that one list cannot drift against a second. Cross-cutting audits
kept at the top level of `docs/` are covered by the same exemption.

Which feature folders are currently buffered is not recorded here — a written list goes stale the
next time a feature closes. `ls -d docs/feature-*/` is the answer; the N-2 rule in `CLAUDE.md`
says how many of them should be there.
