---
name: pr-context-prep
description: >-
  Pointer for non-Claude agents. Canonical pr-context-prep instructions live at
  `.claude/skills/pr-context-prep/SKILL.md`. Prepares a copy-paste-ready PR context
  package before a GitHub PR is opened on the Quran Dashboard workspace, so CodeRabbit
  and human reviewers understand scope, risk, and invariants: it reads the current
  diff/status against the base branch, classifies the change (Backend importer,
  Backend read API, Frontend Angular, specs/docs, or App submodule-pointer bump), and
  emits title, description, scope/out-of-scope, changed files, related files, related
  specs/contracts/reports, critical invariants (Quran data safety first), test/build
  evidence, CodeRabbit focus, a review checklist, size/split advice, risk level, and a
  merge-readiness call. CodeRabbit runs on Backend and Frontend PRs only. Review/prep
  only: never edits code, commits, or opens the PR. Use that Claude skill as the single
  source of truth.
---

# PR Context Prep Skill Pointer

Canonical source:

- `.claude/skills/pr-context-prep/SKILL.md`

Non-Claude agents should read and follow the canonical Claude skill file. Do not keep
a second full copy of pr-context-prep rules here; this file exists only to route
agents to the single source of truth.

For staging, commit order, and submodule-pointer commits, use `commit-workflow`.
