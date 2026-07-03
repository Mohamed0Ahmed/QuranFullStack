---
name: deploy-smoke
description: >-
  Pointer for non-Claude agents. Canonical deploy-smoke instructions live at
  `.claude/skills/deploy-smoke/SKILL.md`. Report-only local deployment / runtime
  smoke-check for the Quran Dashboard fullstack workspace (.NET backend + Angular
  frontend): restore/build, verify and display the local DB target, list migrations and
  check for pending model changes, apply a local migration only with explicit local-DB
  approval, and smoke `/api/health` plus changed endpoints and the frontend build. Never
  drops/resets a database, never runs destructive/import/reseed scripts unless asked, and
  never targets a remote/production DB silently. Use that Claude skill as the single source
  of truth.
---

# Deploy Smoke Skill Pointer

Canonical source:

- `.claude/skills/deploy-smoke/SKILL.md`

Non-Claude agents should read and follow the canonical Claude skill file, including its
database-safety hard rules (verify and display the local DB target before any migration;
apply only with explicit local-DB approval; never touch a remote/production DB) and its
report-only output format. Do not keep a second full copy of deploy-smoke rules here; this
file exists only to route agents to the single source of truth.
