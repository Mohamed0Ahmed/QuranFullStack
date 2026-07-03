---
name: test-guard
description: >-
  Pointer for non-Claude agents. Canonical test-guard instructions (the nine test-code
  rules and the dotnet / jest references) live at `.claude/skills/test-guard/SKILL.md`.
  Review-only test-code quality gate for the Quran Dashboard workspace (.NET/C# +
  Angular/TypeScript): behavior-not-implementation, justified boundary mocks, data-driven
  variants, real DTOs/entities, real infrastructure for persistence, and source-safe
  Quranic test data. Use that Claude skill as the single source of truth.
---

# Test Guard Skill Pointer

Canonical source:

- `.claude/skills/test-guard/SKILL.md`
- references: `.claude/skills/test-guard/references/` (`dotnet.md`, `jest.md`, `llm-app-testing.md`)

Non-Claude agents should read and follow the canonical Claude skill file and its
references. Do not keep a second full copy of test-guard rules here; this file exists only
to route agents to the single source of truth.
