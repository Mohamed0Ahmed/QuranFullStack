---
name: pr-context-prep
description: >-
  Prepares a high-quality, copy-paste-ready PR context package before a GitHub
  pull request is opened on the Quran Dashboard workspace, so CodeRabbit and human
  reviewers understand scope, risk, and invariants at a glance. Use this skill
  whenever the user is about to open a PR, asks for a PR title or description, asks
  what reviewers or CodeRabbit should focus on, wants to "prep" / "package" / "write
  up" a PR, or asks whether a change is ready to review or merge — even if they
  don't say "PR context". It reads the current git diff/status against the base
  branch, classifies the change (Backend importer/DataPipeline, Backend read API,
  Frontend Angular, specs/docs, or App submodule-pointer bump), and emits scope,
  out-of-scope, changed-file summary, related files/patterns reviewers should also
  look at, related specs/contracts/reports, critical project invariants (Quran data
  safety first), test/build evidence, CodeRabbit focus instructions, a review
  checklist, size/split advice, a risk level, and a merge-readiness call. CodeRabbit
  runs on Backend and Frontend PRs only; for App/FullStack pointer-only PRs it emits
  a lighter package and says so. This is review/prep only: it never edits code, never
  commits, and never opens the PR. For staging, commit order, and submodule-pointer
  commits, use commit-workflow instead.
---

# PR Context Prep

Produce a clear, copy-paste-ready **PR context package** so that CodeRabbit and
human reviewers immediately understand what changed, why, what must not break, and
where to look. A good package makes the automated review sharper (CodeRabbit reads
the PR body) and saves human reviewers from reconstructing scope from a raw diff.

**This skill is prep/review only.** It never edits code, never commits, never
opens the PR, and never pushes. It reads the current diff/status and writes a
document. Staging, commit order, and submodule-pointer commits belong to
`commit-workflow`.

## Where CodeRabbit helps (and where it doesn't)

CodeRabbit runs on **Backend** (`App/Backend`) and **Frontend**
(`App/Frontend/quran-dashboard-ui`) PRs only. Those are real code repos.

The **FullStack/App** repo mostly carries **submodule-pointer bumps** and workspace
docs; CodeRabbit adds little there. For an App-only pointer PR, still produce the
package but keep it light, drop the CodeRabbit focus section to a one-liner
("pointer-only PR; CodeRabbit not applicable"), and lean on the human-review
checklist instead.

## 1. Establish the target

Before writing anything, pin down three things. Do not guess silently — if the diff
is empty or the repo is ambiguous, say so.

1. **Which repo is the PR for?** Detect the repo holding the feature branch with
   commits ahead of the base. Usually the user is working in one repo:

   ```bash
   git -C App/Backend rev-parse --abbrev-ref HEAD
   git -C App/Frontend/quran-dashboard-ui rev-parse --abbrev-ref HEAD
   git -C App rev-parse --abbrev-ref HEAD
   ```

   The repo whose branch is not `main` and is ahead of the base is the PR repo.

2. **Base branch.** Default to `main`. Only ask the user if you genuinely can't
   infer it — e.g. the branch's upstream tracks something other than `main`, or
   there are stacked branches. Confirm with:

   ```bash
   git -C <repo> merge-base --fork-point main HEAD   # sanity check the base
   ```

3. **The diff.** Use the three-dot form so you compare against the merge base, not
   a moving `main`:

   ```bash
   git -C <repo> diff --name-status main...HEAD
   git -C <repo> diff --stat        main...HEAD
   git -C <repo> log --oneline      main..HEAD
   ```

Only cite files and paths that appear in this output. Never invent paths.

## 2. Classify the change

The classification decides which invariants and CodeRabbit focus apply. A PR can hit
more than one bucket — include every bucket that matches.

| Signal in the diff | Bucket | Emphasize |
| Files under `.../DataPipelines/`, `*Importer*`, `*Import*`, seed/source packages, `resources/import-sources/` | **Backend importer / DataPipeline** | Quran-source immutability, rollback, idempotence, source hashes, hard checks, report gates |
| New/changed read endpoints, controllers, query handlers, DTOs, projections, pagination/filter code | **Backend read API** | Read-only behavior, projection correctness, pagination/filter, DTO contract, null handling, `ApiResponse` shape, tests |
| Files under `App/Frontend/quran-dashboard-ui/src/**` (components, services, signals, routes, SCSS) | **Frontend Angular** | State/signals, URL sync, Arabic RTL labels, loading/error/empty states, a11y, API-contract drift |
| `specs/**` (`spec.md`, `plan.md`, `tasks.md`, `contracts/**`, `research.md`, `data-model.md`, `quickstart.md`) | **Specs** | Cross-artifact consistency (see §invariants) |
| `App` diff is only `modified: Backend/Frontend (new commits)` + workspace docs | **App pointer bump** | Pointer targets exist on child remotes; light package |

**Quran-data override:** if any diff touches Quran text, an ayah/word/root/morphology
source file, a seed correction, an identity key, or importer logic that writes Quran
data — flag it at the **top** of the package as `⚠ Quran data touched`, regardless of
bucket. This is the highest-signal thing a reviewer can be told.

## 3. Gather evidence (don't fabricate it)

For the **Test/build evidence** section, use only evidence that actually exists:
results already produced in this conversation, or that the user confirms. If none
exists, say so plainly and state what should be run — do not claim green.

- Backend: `dotnet build`, the touched test project(s), and — for importers or
  migrations — a `deploy-smoke` pass.
- Frontend: the Angular build and the touched `*.spec.ts` (respect the test worker
  cap; the Angular builder ignores `vitest.config.ts`).

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
<3–6 focused directives; see §CodeRabbit focus. One-liner for App pointer PRs.>

## Suggested review checklist
- [ ] <objectively checkable items a reviewer ticks off>

## PR size / split
<one line: fine as-is, or split recommendation with the seam.>

## Risk: <Low | Medium | High>
<one sentence why.>

## Merge readiness: <Ready | Ready with nits | Needs work | Blocked>
<one sentence, tied to evidence and invariants — not vibes.>
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

For an **App pointer-only PR**, replace the whole section with: "Pointer-only PR;
CodeRabbit not applicable — see human checklist."

## Risk & merge-readiness rubric

Be consistent so the labels mean something.

**Risk**
- **High:** Quran text/source/import mutation; importer rollback/idempotence/hash
  logic; schema or EF migration; a DTO/API contract consumed by the frontend.
- **Medium:** new read APIs, non-trivial frontend state/URL logic, meaningful logic changes.
- **Low:** docs, specs-only, tests-only, formatting, submodule-pointer bumps.

**Merge readiness**
- **Ready:** scope clean, invariants satisfied, evidence green.
- **Ready with nits:** minor issues that don't block; list them.
- **Needs work:** missing tests/evidence, unaddressed invariant, or scope creep.
- **Blocked:** an invariant is violated (e.g. Quran data mutated without cause) or
  the build/tests fail.

## Guardrails

- Prep/review only — do not edit code, commit, push, or open the PR.
- Read from the real diff/status; never invent files, paths, specs, or reports.
- Do not claim tests/build passed without evidence; say "not yet run" instead.
- Ask for the base branch only when you truly cannot infer it (default `main`).
- Keep output copy-paste-ready; prefer tight bullets over prose.
- Flag any Quran text/source/import mutation at the top, every time.
- Staging, commit order, and submodule-pointer commits → use `commit-workflow`.
