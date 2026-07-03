---
name: commit-workflow
description: >-
  Pointer for non-Claude agents. Canonical commit-workflow instructions live at
  `.claude/skills/commit-workflow/SKILL.md`. Safe Git commit workflow for the Quran
  Dashboard three-repo workspace (Backend + Frontend submodules inside the FullStack
  workspace): per-repo status, safe explicit staging, child-repos-first then
  workspace-last commit order, submodule-pointer safety, and the post-PR sync-to-main
  workflow (only after the PR is merged). Use that Claude skill as the single source of
  truth.
---

# Commit Workflow Skill Pointer

Canonical source:

- `.claude/skills/commit-workflow/SKILL.md`

Non-Claude agents should read and follow the canonical Claude skill file (including
Section 7, the post-PR sync-to-main workflow). Do not keep a second full copy of
commit-workflow rules here; this file exists only to route agents to the single source of
truth.
