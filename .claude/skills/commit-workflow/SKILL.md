---
name: commit-workflow
description: >-
  Safe Git commit workflow for the Quran Dashboard monorepo. Use this skill
  whenever the user wants to commit, stage, or push changes, asks what should be
  committed, or asks about commit order. It inspects the single repository from
  its root, plans explicit path-based staging, keeps commits focused by concern,
  suggests concise messages, and surfaces unrelated-file and push-readiness
  warnings. Includes a post-PR sync-to-dev workflow, but only after the user
  explicitly says the PR was merged. Never run destructive Git
  commands and never push unless asked.
---

# Commit Workflow Skill

Use this skill to plan and safely perform Git commits in the Quran Dashboard
monorepo.

This skill handles Git only. Do not modify source code or run destructive Git
commands.

## Repository model

`App/` is the single Git repository. Backend and Frontend are ordinary tracked
directories:

- `Backend/`
- `Frontend/quran-dashboard-ui/`

Run Git commands from the repository root. `git -C Backend` and
`git -C Frontend/quran-dashboard-ui` still resolve to the same root repository;
they do not inspect independent repositories.

### Branch model

`dev` is the long-lived integration branch; `main` is stable/production and
protected. Create every new branch off `dev`, and treat `dev` as the base for
commit ranges and pushes. Never commit directly to `main`.

## Which workflow to use

| Phase | When | What to run |
|-------|------|-------------|
| **A - Feature work** | Default. User is implementing on a feature branch. | Sections 1-6 only. |
| **B - Open PR** | User explicitly asks to open a PR. | Prepare and open the PR against `dev`; do not run section 7. |
| **C - After PR merged** | User explicitly confirms the PR was merged. | Section 7 only, plus push rules if requested. |

Never run post-merge synchronization before the user confirms the PR was merged.
For PR title, description, scope, invariants, and reviewer focus, use
`pr-context-prep` first.

## 1. Status inspection

Inspect the repository once from its root:

```bash
git status --short --branch
git diff --stat
git diff --cached --stat
git log --oneline -10
```

Group changed paths by concern: workspace tooling/docs, Backend, Frontend, specs,
or a coherent cross-stack feature. Do not mistake path groups for separate Git
repositories.

## 2. Commit planning

Create the fewest focused commits that preserve reviewability and buildability.

- Keep one coherent full-stack feature together when its Backend, Frontend, and
  contract changes form one atomic behavior.
- Split unrelated concerns even when they live under the same project directory.
- Do not split changes merely because they cross `Backend/` and `Frontend/`.
- Preserve dependency order when one commit must exist before another.

There is no required Backend-first, Frontend-second, workspace-last order.

## 3. Safe staging

- Avoid blind `git add .` or `git add -A` unless the user explicitly approves the
  complete working tree.
- Prefer explicit root-relative paths.
- Do not stage files unrelated to the current task.
- Never stage generated or sensitive files, including `node_modules`, `dist`,
  `bin`, `obj`, `.angular/cache`, secrets, credentials, or local editor settings
  unless explicitly requested.
- If an ignored/generated file unexpectedly appears, flag it instead of staging it.

## 4. Commit messages

- Describe intent, not implementation noise.
- Use one message per focused commit.
- Follow repository message conventions.

Examples:

- `feat(words): add word types explorer`
- `fix(api): bound word type page size`
- `docs: update workspace coding guidance`

## 5. Push behavior

- Do not push unless the user asks or the requested workflow requires it.
- Push the current monorepo branch once, after all intended commits and checks.
- Never force-push.
- Before pushing, confirm the destination remote and branch.

## 6. Verification before commit or push

Before committing:

- Inspect `git status`, staged diff, unstaged diff, and recent log.
- Show exact staged paths and flag unexpected files.
- Confirm the current branch is named, not detached.
- Run relevant Backend/Frontend checks for changed areas when feasible.

Before pushing:

- Confirm the working tree contains no unintended changes.
- Confirm local commits are the intended range over the upstream/base branch
  (`dev` for feature work).
- If a PR branch contains unsquashed subtree imports, require GitHub's **merge
  commit** strategy. Squash or rebase merging would make imported source commits
  cease to be ancestors of `main`.

## 7. Post-PR sync to dev

Run only after the user explicitly confirms the PR was merged:

```bash
git status --short --branch
```

Stop if this preflight shows uncommitted paths. Only with a clean worktree, run:

```bash
git switch dev
git pull --ff-only origin dev
git status --short --branch
```

For any PR that imported unsquashed subtree history, rerun every ancestor check
recorded in the PR package against `dev`:

```bash
git merge-base --is-ancestor <imported-tip> dev
```

Healthy state:

- local `dev` equals `origin/dev`;
- working tree is clean, except intentional new work;
- every imported subtree tip remains an ancestor of `dev`, when applicable;
- no submodule initialization or pointer synchronization is required.

Start the next feature branch from updated `dev`.

Do not switch to or sync `main` here. `main` only moves on an explicit
`dev → main` release or emergency hotfix the user requests.

## Output format

# Commit Workflow

## Repository Status

Current branch, upstream, clean/dirty state, and changed path groups.

## Commit Plan

List focused commits in order, or state that one commit is sufficient.

## Staging Plan

List exact root-relative paths for each commit.

## Commit Messages

Suggest one message per planned commit.

## Warnings

List unexpected files, missing verification, branch/upstream concerns, or `None`.

## Commands

Provide commands in execution order.

## Final Checklist

- Correct phase selected.
- One root repository inspected.
- Only intended paths staged.
- Relevant checks passed or gaps reported.
- Push destination confirmed when push requested.
- Subtree-import PRs use merge commits, never squash or rebase.

## Guardrails

- Do not invent paths; use only paths confirmed by Git status.
- Do not run `reset`, `clean`, `rebase`, force-push, or other destructive commands.
- Do not commit secrets or generated outputs.
- Do not modify source code.
- Do not push unless requested.
- Do not run section 7 until the user confirms the PR was merged.
