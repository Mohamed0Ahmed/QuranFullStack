---
name: commit-workflow
description: Use when asked to inspect, branch, stage, commit, push, open a PR, or synchronize Git state in the Quran Dashboard monorepo.
---

# Commit Workflow

## Responsibility

Perform the explicitly requested Git operation on the single monorepo (rooted at `App/`;
Backend and Frontend are ordinary tracked directories — run Git from the repository
root), with the Git-integrity checks this skill owns: one root status inspection,
explicit path-based staging, staged-diff inspection, `git diff --cached --check`,
branch/upstream confirmation, and the post-merge `dev` sync.

**Not this skill's job:** builds, tests, code review, deployment, source-code fixes, or
automatic PR preparation. Existing build/test/review evidence is not a Git gate — do not
run or demand it here.

## Branch model

`dev` is the long-lived integration branch; `main` is protected Railway production.
Create every branch off `dev` and PR into `dev`. Never commit to `main`; `main` moves
only on an explicit `dev → main` release or emergency hotfix the user requests.

## The requested operation

Do only what was asked:

- **Inspect/plan:** `git status --short --branch`, `git diff --stat`,
  `git diff --cached --stat`, `git log --oneline -10` once from the root. Group changed
  paths by concern and plan the fewest focused commits that stay reviewable and
  buildable; split unrelated concerns, keep one coherent cross-stack feature together,
  preserve dependency order.
- **Stage/commit:** stage explicit root-relative paths — no blind `git add .`/`-A`
  without explicit approval of the complete working tree. Inspect the staged diff and
  run `git diff --cached --check` before committing. Never stage generated or sensitive
  files (`node_modules`, `dist`, `bin`, `obj`, `.angular/cache`, secrets, credentials,
  local editor settings); flag unexpected or ignored-but-present files instead of
  staging them. One concise, intent-describing message per focused commit, following
  repository conventions.
- **Push:** only when the user asks. Confirm the destination remote/branch and that the
  local commits are the intended range over the base (`dev` for feature work) first.
  Never force-push.
- **Open a PR:** only when explicitly requested; target `dev`. If the branch carries
  unsquashed subtree imports, require GitHub's **merge commit** strategy — squash or
  rebase merging would make the imported commits cease to be ancestors of `main`.
- **Post-merge sync to `dev`:** only after the user explicitly confirms the PR was
  merged, and only with a clean worktree: `git switch dev`,
  `git pull --ff-only origin dev`, then re-check status. For any PR that imported
  unsquashed subtree history, re-verify each imported tip with
  `git merge-base --is-ancestor <tip> dev`. Do not switch to or sync `main` here.

## Guardrails

- Never run destructive Git commands: `reset`, `clean`, `rebase`, force-push.
- Never modify source code; never invent paths — use only paths Git status confirms.
- Commit, push, and PR authority is explicit: perform each only when requested.

## Output

Report the operation performed (or planned): repository status, the commit/staging plan
with exact root-relative paths, suggested messages, the commands in order, and warnings
(unexpected files, branch/upstream concerns) or `None`.
