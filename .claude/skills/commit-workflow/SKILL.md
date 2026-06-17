---
name: commit-workflow
description: >-
  Safe Git commit workflow for the Quran Dashboard FullStack workspace, which is
  three repositories: the Backend repo (App/Backend) and the Frontend repo
  (App/Frontend/quran-dashboard-ui), both tracked as submodules/gitlinks inside
  the FullStack workspace repo (App). Use this skill whenever the user wants to
  commit, stage, or push changes, asks what they should commit, asks about commit
  order, or is working across more than one of these repos, even if they don't
  mention submodules. It inspects status per repo, plans safe explicit staging,
  enforces child-repos-first then workspace-last commit order, suggests concise
  commit messages, and surfaces submodule-pointer and unrelated-file warnings.
  Includes a post-PR sync-to-main workflow, but only after the user explicitly
  says the PR was accepted/merged — never during feature work or when opening a PR.
  This is commit planning and safe execution only: never run destructive Git
  commands (reset/clean/rebase) and never push unless asked.
---

# Commit Workflow Skill

Use this skill to plan and safely perform Git commits in the Quran Dashboard
FullStack workspace.

This skill is for commit planning and safe commit execution only. Do not modify
source code, and do not run destructive Git commands.

## Which workflow to use

This workspace has **three phases**. Use only the phase that matches what the user
asked for.

| Phase | When | What to run |
|-------|------|-------------|
| **A — Feature work** | Default. User is implementing on a feature branch. | Sections 1–6 only. |
| **B — Open PR** | User explicitly says **open PR** (or asks to create a PR). | Help open the PR from the feature branch. **Do not** run section 7. |
| **C — After PR accepted** | User explicitly says the PR was **accepted**, **merged**, or **merged to main**. | Section 7 only (plus section 5 push rules if push is requested). |

**Hard rule:** never run **section 7 (post-PR sync to main)** before the user says
the PR was accepted/merged. Opening a PR is not enough — the merge must be done.

During feature work, do **not** pull `main` into child repos or “sync pointers to
main” unless the user explicitly asks for that outside the post-merge flow.

## Why order matters

This workspace is three separate repositories:

- **Backend repo:** `App/Backend`
- **Frontend repo:** `App/Frontend/quran-dashboard-ui`
- **FullStack workspace repo:** `App`

`App/Backend` and `App/Frontend/quran-dashboard-ui` are tracked from the FullStack
workspace as **submodules / gitlinks**. The workspace repo does not store the child
files; it stores a _pointer_ (a commit SHA) to each child repo. So if you commit
the workspace while it points at a child commit that doesn't exist yet (because the
child wasn't committed first), the pointer is dangling and anyone cloning the
workspace gets a broken submodule.

**Core rule:** if Backend or Frontend have changes, commit those child repos
first, then commit the FullStack workspace repo to update the submodule pointers
and any workspace files.

## 1. Status inspection

Inspect each repo before deciding anything. Run (or ask the user to run):

```bash
git -C App status
git -C App/Backend status
git -C App/Frontend/quran-dashboard-ui status
```

From the output, determine for each repo whether it has changes, and in the
**workspace repo specifically**, distinguish two different kinds of change:

- **Workspace file changes:** real files owned by `App` (e.g. `AGENTS.md`,
  `CODING_PRINCIPLES.md`, `.claude/...`). These appear as normal modified/new
  files.
- **Submodule pointer changes:** a child repo moved to a new commit. Git shows
  these as `modified: Backend (new commits)` or
  `modified: Frontend/quran-dashboard-ui (new commits)`. A pointer change only
  becomes meaningful _after_ the child repo has actually committed that new
  commit.

Do not assume all changes belong to one repo. Decide per repo.

## 2. Commit order

Apply the core rule, then handle the partial cases:

1. **Backend** changes first.
2. **Frontend** changes second.
3. **FullStack / App** changes last (workspace files and updated submodule
   pointers).

Conditional cases:

- **Only App has changes:** commit App only.
- **Only Backend has changes:** commit Backend first, then commit App only if the
  Backend submodule pointer actually changed.
- **Only Frontend has changes:** commit Frontend first, then commit App only if
  the Frontend submodule pointer actually changed.
- **Multiple repos have changes:** Backend, then Frontend, then App.

## 3. Safe staging

- Avoid blind `git add .` / `git add -A` unless the user explicitly approves it.
  It is the easiest way to sweep up unrelated or unwanted files.
- Prefer staging explicit files by path (`git add <path> <path>`).
- Do not stage files unrelated to the current task.
- Never stage generated or sensitive files, including: build outputs, `node_modules`,
  `dist`, `bin`, `obj`, `.angular/cache`, secrets (`.env`, key files, credentials),
  and local/editor config (e.g. `.idea`, local settings) unless the user
  specifically asks.
- If `.gitignore` should already exclude something but it still shows up, flag it
  rather than staging it.

## 4. Commit messages

- Keep messages concise and describe the **intent**, not implementation noise.
- One focused message per repo.

Example messages:

- `Add workspace coding principles`
- `Add backend project instruction files`
- `Add frontend project instruction files`
- `Update fullstack submodule pointers "With Clear Commit Messages guide"`
- `Add engineering review skill`

## 5. Push behavior

- Do not push unless the user asks, or the workflow clearly requires it.
- If pushing, push **child repos before** the FullStack repo, for the same reason
  commits are ordered that way: the workspace pointer must reference commits that
  already exist on the child remotes.

## 6. Verification before commit

Before committing anything:

- Show the changed files per repo so the user can confirm.
- Ask for confirmation if there are unexpected files.
- Warn if a submodule has uncommitted changes.
- Warn if `App` shows a submodule pointer change before the corresponding child
  repo has been committed (committing now would record a dangling pointer).
- Warn if a child repo is on a **detached HEAD** instead of a named branch.
  Commits made detached can orphan work and produce bad App pointers (same SHA
  not on `main`). Always commit on the feature branch during work, or on `main`
  only when intentionally syncing after merge.

## 7. Post-PR sync to main (after merge only)

Run this **only** when the user explicitly confirms the PR was **accepted and
merged** (e.g. “PR accepted”, “merged to main”, “merge done”).

Do **not** run this when:

- the user is still implementing on a feature branch;
- the user only asked to **open** a PR;
- the PR is open but not yet merged.

### What “synced” means

Submodule pointers in `App` store a **commit SHA**, not a branch name. After merge,
healthy state is:

- each touched child repo: local `main` = `origin/main` = latest merge tip;
- `App` committed pointers for Backend/Frontend = those same `main` tips;
- `git submodule status` shows `(heads/main)` for each child, not detached HEAD.

### Steps (in order)

**1. Sync child repos that were in the PR**

For each child repo that had changes in the merged PR (Backend, Frontend, or both):

```bash
git -C App/Backend checkout main
git -C App/Backend pull origin main

git -C App/Frontend/quran-dashboard-ui checkout main
git -C App/Frontend/quran-dashboard-ui pull origin main
```

Skip a child repo if it was not part of the merged PR.

**2. Sync the FullStack workspace**

```bash
git -C App checkout main
git -C App pull origin main
```

**3. Align submodule checkouts with App pointers**

```bash
git -C App submodule update --init
```

**4. Inspect status**

```bash
git -C App status
git -C App submodule status
git -C App/Backend status
git -C App/Frontend/quran-dashboard-ui status
```

**5. Fix pointer drift if needed**

If `App` shows `modified: Backend` or `modified: Frontend/quran-dashboard-ui`
after the pulls above, the workspace pointer is behind (or wrong). Then:

1. Confirm each changed child is on `main` and matches `origin/main`.
2. Stage only the submodule path(s): `git -C App add Backend` (and/or Frontend).
3. Commit `App` with a pointer-update message.
4. If pushing: push child repos first (if any child commits were missing on
   remote — uncommon right after a GitHub merge), then push `App`.

If `git submodule status` shows a commit without `(heads/main)`, checkout `main`
in that child and pull again before committing `App`.

**6. Done when**

- All three repos on `main`, clean working tree (or only intentional uncommitted work).
- `git submodule status` shows `(heads/main)` for Backend and Frontend.
- No `modified: Backend` / `modified: Frontend/...` in `App` unless the user is
  starting new work.

### Post-merge checklist (user-facing summary)

After you accept/merge a PR on GitHub, do this locally:

1. `Backend` → `checkout main` → `pull`
2. `Frontend` → `checkout main` → `pull` (if that PR touched Frontend)
3. `App` → `checkout main` → `pull`
4. `App` → `submodule update --init`
5. If `App` still shows a modified submodule → commit pointer update in `App`
6. Push `App` if you committed a pointer fix (children usually already on remote)

Then start the next feature branch from updated `main` in the relevant repos.

## Output Format

Return the plan in this structure:

# Commit Workflow

## Repo Status

- Backend:
- Frontend:
- FullStack:

## Recommended Commit Order

1.
2.
3.

## Staging Plan

List exact files to stage per repo.

## Commit Messages

Suggest commit messages per repo.

## Warnings

List any risks or unexpected files.
If none, write:
None.

## Commands

Provide commands in the correct order.

## Final Checklist

- Correct phase: feature work (1–6), open PR (B only), or post-merge sync (7)
- Child repos committed first (feature work)
- FullStack repo committed last (feature work)
- No unrelated files staged
- Push order correct if push is requested
- Post-merge: all pointers match child `main` tips; no detached HEAD

## Guardrails

- Be careful with submodules / gitlinks.
- Do not assume all changes belong in one repo.
- Do not invent file paths; use only paths confirmed by `git status`.
- Do not run destructive Git commands.
- Do not use `reset` / `clean` / `rebase` unless explicitly requested.
- Do not commit secrets or local environment files.
- Do not modify source code.
- Do not run section 7 until the user confirms the PR was merged.
- Do not pull/sync to `main` during feature work unless explicitly requested.
- This skill is for commit planning and safe commit execution only.
