---
name: backend-global-usings-cleanup
description: >-
  Pointer for non-Claude agents. Canonical backend-global-usings-cleanup instructions
  live at `.claude/skills/backend-global-usings-cleanup/SKILL.md`. Action skill that
  consolidates C# global usings across the Quran Dashboard .NET backend: promotes common
  layer-safe namespaces (repeated in more than five files) into GlobalUsings.cs, removes
  redundant local usings, respects Clean Architecture boundaries, verifies with
  `dotnet build`, and never edits BACKEND_STRUCTURE.md. Use that Claude skill as the
  single source of truth.
---

# Backend Global Usings Cleanup Skill Pointer

Canonical source:

- `.claude/skills/backend-global-usings-cleanup/SKILL.md`

Non-Claude agents should read and follow the canonical Claude skill file. Do not keep a
second full copy of backend-global-usings-cleanup rules here; this file exists only to
route agents to the single source of truth.
