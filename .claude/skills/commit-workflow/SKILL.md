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
  This is commit planning and safe execution only: never run destructive Git
  commands (reset/clean/rebase) and never push unless asked.
---

# Commit Workflow Skill

Use this skill to plan and safely perform Git commits in the Quran Dashboard
FullStack workspace.

This skill is for commit planning and safe commit execution only. Do not modify
source code, and do not run destructive Git commands.

## Why order matters

This workspace is three separate repositories:

- **Backend repo:** `App/Backend`
- **Frontend repo:** `App/Frontend/quran-dashboard-ui`
- **FullStack workspace repo:** `App`

`App/Backend` and `App/Frontend/quran-dashboard-ui` are tracked from the FullStack
workspace as **submodules / gitlinks**. The workspace repo does not store the child
files; it stores a *pointer* (a commit SHA) to each child repo. So if you commit
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
  becomes meaningful *after* the child repo has actually committed that new
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
- `Update fullstack submodule pointers`
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
- Child repos committed first
- FullStack repo committed last
- No unrelated files staged
- Push order correct if push is requested

## Guardrails

- Be careful with submodules / gitlinks.
- Do not assume all changes belong in one repo.
- Do not invent file paths; use only paths confirmed by `git status`.
- Do not run destructive Git commands.
- Do not use `reset` / `clean` / `rebase` unless explicitly requested.
- Do not commit secrets or local environment files.
- Do not modify source code.
- This skill is for commit planning and safe commit execution only.
