---
name: pr-context-prep
description: Use when asked to prepare a PR title, description, scope, evidence summary, or reviewer context package for the Quran Dashboard.
---

# PR Context Prep

## Responsibility

Assemble a copy-paste-ready PR context package from the real branch state and evidence
that already exists: title, description, scope and out-of-scope, changed files grouped
by area, related files and specs/contracts/docs, pointers to the applicable invariant
owners, the supplied test/build evidence (or its absence, stated plainly), reviewer and
CodeRabbit focus, and a suggested review checklist.

**Not this skill's job:** writing files, any Git/PR mutation, rerunning or generating
evidence, an independent merge-readiness verdict, fixes, or invoking another Skill.
Readiness is adjudicated by the reviewers this package serves.

## Establish the target

From the monorepo root: current branch and status (`git status --short --branch`), base
branch (default `dev`; `main` only for an explicit `dev → main` release or hotfix), and
the three-dot diff against the merge base (`git diff --name-status dev...HEAD`,
`git diff --stat dev...HEAD`, `git log --oneline dev..HEAD`). Claim a file changed only
when this output shows it; cite related unchanged files only after verifying their
paths. If the diff is empty or the branch is ambiguous, say so instead of guessing.

## Classify and flag

Group the diff by area — Backend importer/DataPipeline, Backend API, Frontend Angular,
specs/docs, cross-stack, history-preserving subtree import — and point each group's
invariants at their owner instead of restating them: the active Spec Kit artifact when the feature
is open, the matching `docs/contracts/*.md` pointer, the implicated code, and the applicable
`Backend/.architecture/` / `Frontend/quran-dashboard-ui/.architecture/` heading.

- **Quran-data flag:** if the diff touches Quran text, an ayah/word/root/morphology
  source file, a seed correction, an identity key, or importer logic that writes Quran
  data, put `⚠ Quran data touched` at the **top** of the package.
- **Subtree imports:** a PR carrying unsquashed subtree history must state "Merge commit
  required; do not squash or rebase" and list the imported tips (verified with
  `git merge-base --is-ancestor <sha> HEAD`).

## Evidence (never fabricate)

Use only evidence that already exists in this conversation or that the user supplies.
Label supplied evidence and anything missing plainly, and never claim green. Do not run
verification from this Skill; state gaps for the caller to close.

## Output

The package in copy-paste order: title (Quran flag on top when applicable), description
(2–5 sentences), scope, out-of-scope, changed files grouped by area, related
files/specs/contracts/reports, invariant owner pointers, test/build evidence, CodeRabbit
focus (3–6 directives specific to this diff), suggested review checklist, size/split
advice, a one-line risk note (Low / Medium / High with the reason), and the required
merge method.
