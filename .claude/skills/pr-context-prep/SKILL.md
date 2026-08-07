---
name: pr-context-prep
description: >-
  Prepares a copy-paste-ready PR context package for the Quran Dashboard monorepo.
  Use when the user wants a PR title, description, reviewer focus, risk assessment,
  or merge-readiness check. It reads root status and the branch diff against the
  base, classifies changed Backend, Frontend, specs/docs, and cross-stack paths,
  and reports scope, invariants, evidence, review focus, risk, and readiness. It
  requires merge commits for unsquashed subtree-history imports. Review/prep only:
  never edits, commits, pushes, or opens a PR. Use commit-workflow for Git execution.
---

# PR Context Prep

Produce a clear, copy-paste-ready **PR context package** so that CodeRabbit and
human reviewers immediately understand what changed, why, what must not break, and
where to look. A good package makes the automated review sharper (CodeRabbit reads
the PR body) and saves human reviewers from reconstructing scope from a raw diff.

**This skill is prep/review only.** It never edits code, never commits, never
opens the PR, and never pushes. It reads the current diff/status and writes a
document. Staging, commit planning, and push readiness belong to `commit-workflow`.

## Where CodeRabbit helps (and where it doesn't)

CodeRabbit and human reviewers see one monorepo PR. Tailor review focus to changed
root-relative paths under `Backend/`, `Frontend/quran-dashboard-ui/`, and workspace
docs/specs. Do not create separate PR packages for project directories.

## 1. Establish the target

Before writing anything, pin down three things. Do not guess silently; if the diff
is empty or the branch is ambiguous, say so.

1. **Current branch.** Run from the monorepo root:

   ```bash
   git rev-parse --show-toplevel
   git rev-parse --abbrev-ref HEAD
   git status --short --branch
   git diff --stat
   git diff --cached --stat
   ```

2. **Base branch.** Default to `dev` — feature branches PR into `dev`, never into
   `main`. Use `main` as the base only for an explicit `dev → main` release or
   emergency hotfix the user requests. Otherwise ask only if you genuinely can't
   infer it — e.g. the branch's upstream tracks something other than `dev`, or
   there are stacked branches. Confirm with:

   ```bash
   git merge-base --fork-point dev HEAD   # sanity check the base
   ```

3. **The diff.** Use the three-dot form so you compare against the merge base, not
   a moving `dev`:

   ```bash
   git diff --name-status dev...HEAD
   git diff --stat        dev...HEAD
   git log --oneline      dev..HEAD
   ```

Only claim a file changed when it appears in this output. Related unchanged files
may be cited only after verifying their paths in the repository.

## 2. Classify the change

The classification decides which invariants and CodeRabbit focus apply. A PR can hit
more than one bucket — include every bucket that matches.

| Signal in the diff | Bucket | Emphasize |
| Files under `Backend/**/DataPipelines/`, `*Importer*`, `*Import*`, seed/source packages, `resources/import-sources/` | **Backend importer / DataPipeline** | Quran-source immutability, rollback, idempotence, source hashes, hard checks, report gates |
| New/changed Backend read endpoints, controllers, query handlers, DTOs, projections, pagination/filter code | **Backend read API** | Read-only behavior, projection correctness, pagination/filter, DTO contract, null handling, `ApiResponse` shape, tests |
| Files under `Frontend/quran-dashboard-ui/src/**` (components, services, signals, routes, SCSS) | **Frontend Angular** | State/signals, URL sync, Arabic RTL labels, loading/error/empty states, a11y, API-contract drift |
| `specs/**` (`spec.md`, `plan.md`, `tasks.md`, `contracts/**`, `research.md`, `data-model.md`, `quickstart.md`) | **Specs** | Cross-artifact consistency (see §invariants) |
| Related changes span Backend and Frontend paths | **Cross-stack** | API contract alignment, integration behavior, combined verification |
| Unsquashed `git subtree` import commits | **History-preserving migration** | Imported tips remain ancestors; merge commit required; squash/rebase forbidden |

**Quran-data override:** if any diff touches Quran text, an ayah/word/root/morphology
source file, a seed correction, an identity key, or importer logic that writes Quran
data — flag it at the **top** of the package as `⚠ Quran data touched`, regardless of
bucket. This is the highest-signal thing a reviewer can be told.

## 3. Gather evidence (don't fabricate it)

For the **Test/build evidence** section, use only evidence that actually exists:
results already produced in this conversation, or that the user confirms. If none
exists, say so plainly and state what should be run — do not claim green.

- Backend: `dotnet build` and the `Backend/scripts/test-backend` lanes the change
  triggers — and, for importers or migrations, a `deploy-smoke` pass.
- Frontend: `npm run build:verify` and the `npm run test:*` lanes the change triggers
  (every lane already carries the worker cap; the Angular builder ignores
  `vitest.config.ts`).

Name the executed **lanes** and check them against the execution-trigger matrix in
`TESTING_STRATEGY.md` §5, which is the authoritative changed-scope→lane mapping — read it
there rather than restating it here. Backend lanes are arguments to
`Backend/scripts/test-backend` (its §3); Frontend lanes are `npm run test:*` scripts (its
§4). The former Tier A–E labels are superseded; only `tier-b` survives, as the name of the
Backend no-pipeline lane. Four tree facts the package must get right (section numbers refer
to `TESTING_STRATEGY.md`, not this skill's own steps):

- **There is no CI** (its §8). There is no workflow; every gate is local and unverified.
  Never write "CI will catch it" or present a green pipeline.
- **A hand-written filter is not a lane** (its §1). `--filter "FullyQualifiedName~…"` and
  `npm test -- --include=…` are neither reproducible nor reportable; the package must
  carry lane names.
- **The route-smoke gate is active** (its §6). For route, contract, auth, middleware, or
  binding changes, the evidence section MUST carry a
  `Backend/scripts/test-backend smoke --no-build` line with its pass/fail/skip counts. If a
  route was added or changed, confirm the matching `SmokeRouteCatalog` entry is in the same
  diff. The canonical Smoke **data** tier is a *separate lane* — `SmokeDataReadTests` is
  `Kind=Canonical`, so `smoke` excludes it and `canonical-data` runs it. Evidence must name
  the lane; an unqualified "smoke passed" is incomplete. `canonical-data` and `pre-pr` shard
  (its §3.3), so both shard results must appear, and the runner's
  `canonical data tier: …` line must be quoted verbatim: missing canonical resources
  **fail** that lane at preflight rather than skipping it (its §3.4).
- **The browser E2E layer is opt-in, never a required gate** (its §11). It may appear as
  supplementary evidence and must be labelled supplementary; it never stands in for the
  `smoke` lane or any required lane.

Recommend the relevant skill for missing evidence (`deploy-smoke` for build/migrate/
run, `test-guard` for test quality) rather than running it yourself here.

## 4. Assemble the package

Output the sections below in this exact order. Keep each one tight and
copy-paste-ready — the whole point is that the user pastes it straight into the PR.
Omit a subsection only when it truly doesn't apply, and say why in one line rather
than leaving it blank.

```
# PR: <title>

<one-line ⚠ Quran-data flag here if applicable, else omit>

## Description
<2–5 sentences: what changed and why. Link the feature/spec.>

## Scope
- <what this PR does, bullet per concern>

## Out of scope
- <deliberately excluded, so reviewers don't flag it as missing>

## Changed files
<grouped by area; one line each with a short "why". Note adds/dels from --stat.>

## Related files / patterns to also consider
<unchanged files reviewers should still check: callers of a changed DTO, the
frontend service consuming a changed backend contract, sibling importers, tests
that should have changed but didn't.>

## Related specs / tasks / contracts / docs / reports
<paths under specs/, docs/feature-XXX/, Backend/report/feature-XXX/, contracts/.>

## Critical invariants
<the must-not-break rules that apply to this change — see the tables below.>

## Test / build evidence
<what was run and the result; or "not yet run — needs: …".>

## For CodeRabbit — focus here
<3–6 focused directives; see §CodeRabbit focus.>

## Suggested review checklist
- [ ] <objectively checkable items a reviewer ticks off>

## PR size / split
<one line: fine as-is, or split recommendation with the seam.>

## Risk: <Low | Medium | High>
<one sentence why.>

## Merge readiness: <Ready | Ready with nits | Needs work | Blocked>
<one sentence, tied to evidence and invariants — not vibes.>

## Required merge method
<For unsquashed subtree imports: "Merge commit required; do not squash or rebase."
Otherwise state the repository's normal policy or "No special requirement".>
```

## Critical invariants by bucket

Pull the block(s) matching the classification into the **Critical invariants** and
**Suggested review checklist** sections. These are the things this codebase cares
about most; a reviewer who checks only these has caught the expensive mistakes.

**Quran data safety (always, when data is touched)**
- Quran source files and staged import packages are **immutable inputs** — a PR must
  not silently edit ayah/word/root/morphology text or seed data.
- Identity/stats keys follow the established rule (e.g. clean imlaei-simple, not
  uthmani) while display stays Uthmani — a change here needs an explicit reason.
- Any data mutation is intentional, reviewed, and reversible.

**Backend importer / DataPipeline**
- **Rollback:** a failed run leaves no partial writes (transaction / clear-on-fail).
- **Source immutability:** the importer reads sources; it never rewrites them.
- **Idempotence:** re-running the same source produces the same result, no dupes.
- **Source hashes:** source content is hashed/verified so silent source drift fails.
- **Hard checks:** invariant violations abort the run loudly instead of writing bad data.
- **Report gates:** required import/validation reports are produced under
  `Backend/report/feature-XXX/`.

**Backend read API**
- **Read-only:** the path performs no writes; queries use no-tracking where correctness allows.
- **Projections:** only needed columns are selected; no over-fetch of full entities.
- **Pagination / filter:** page/size bounds and filter predicates are correct and tested.
- **DTO contract:** response shape matches the contract; wrapped in `ApiResponse`.
- **Null handling:** missing/empty results return well-defined empty states, not 500s.
- **Tests:** query/projection/pagination behavior is covered against real infrastructure.

**Frontend Angular**
- **State:** signals/state transitions are correct; no stale or duplicated fetches.
- **URL sync:** shareable/deep-linkable state round-trips through the URL.
- **Arabic RTL labels:** user-facing strings are correct Arabic and render RTL.
- **Loading / error / empty:** all three states are handled, not just the happy path.
- **Accessibility:** keyboard/focus/labels are sane for the changed UI.
- **API-contract drift:** frontend types still match the backend DTO they consume.

**Specs (cross-artifact consistency)**
- `spec.md` ↔ `plan.md` ↔ `tasks.md` agree on scope and are internally consistent.
- `contracts/**`, `data-model.md`, `research.md`, `quickstart.md` reflect the same
  decisions; no contradictions or orphaned tasks/contracts.
- For code+spec PRs: the implementation matches the contract it claims to satisfy.
- Recommend `speckit-analyze` for a deeper cross-artifact pass when specs are large.

**History-preserving migration**
- Verify each imported source tip with `git merge-base --is-ancestor <sha> HEAD`.
- Require GitHub's merge-commit strategy for the PR.
- Squash or rebase merging is blocking because imported source commits would no
  longer be ancestors of `main`.

## CodeRabbit focus instructions

CodeRabbit reads the PR body, so the **For CodeRabbit — focus here** section is a set
of concrete directives that steer it toward what matters and away from noise. Make
them specific to the diff, not generic. Examples of the shape:

- "Verify the new importer aborts and rolls back on a source-hash mismatch; check
  it never writes to the source package."
- "Confirm the paginated endpoint bounds `pageSize` and returns an empty
  `ApiResponse` (not 500) when the filter matches nothing."
- "Check the Angular list handles loading/error/empty and that `WordsService` types
  still match the backend DTO in this PR."
- "Confirm no Quran text/seed values were altered by this refactor."

## Risk & merge-readiness rubric

Be consistent so the labels mean something.

**Risk**
- **High:** Quran text/source/import mutation; importer rollback/idempotence/hash
  logic; schema or EF migration; a DTO/API contract consumed by the frontend;
  repository-history migration.
- **Medium:** new read APIs, non-trivial frontend state/URL logic, meaningful logic changes.
- **Low:** docs, specs-only, tests-only, or formatting with no behavior change.

**Merge readiness**
- **Ready:** scope clean, invariants satisfied, evidence green.
- **Ready with nits:** minor issues that don't block; list them.
- **Needs work:** missing tests/evidence, unaddressed invariant, or scope creep.
- **Blocked:** an invariant is violated (e.g. Quran data mutated without cause) or
  the build/tests fail. A subtree-history migration is also blocked unless merge
  commit merging is available and required.

## Guardrails

- Prep/review only — do not edit code, commit, push, or open the PR.
- Read from the real diff/status; never invent files, paths, specs, or reports.
- Do not claim tests/build passed without evidence; say "not yet run" instead.
- Ask for the base branch only when you truly cannot infer it (default `dev`;
  `main` only for an explicit `dev → main` release or hotfix).
- Keep output copy-paste-ready; prefer tight bullets over prose.
- Flag any Quran text/source/import mutation at the top, every time.
- Staging, commit planning, and push readiness -> use `commit-workflow`.
