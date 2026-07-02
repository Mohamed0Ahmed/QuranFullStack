---
name: dependency-audit
description: >-
  Pointer for non-Claude agents. Canonical dependency-audit instructions live at
  `.claude/skills/dependency-audit/SKILL.md`. Audit-first dependency / security scan for the
  Quran Dashboard fullstack workspace — backend NuGet (`dotnet list package --vulnerable
  --include-transitive` / `--outdated`) and frontend npm (`npm audit` / `npm outdated`). It
  separates direct from transitive issues, finds the likely parent for a transitive advisory,
  and proposes the smallest safe remediation with verification commands. It does not upgrade
  packages unless explicitly asked, never does major upgrades by default, never suppresses
  advisories without approval, and never mixes dependency cleanup with feature/performance
  changes. Use that Claude skill as the single source of truth.
---

# Dependency Audit Skill Pointer

Canonical source:

- `.claude/skills/dependency-audit/SKILL.md`

Non-Claude agents should read and follow the canonical Claude skill file, including its
audit-first guardrails (no upgrades unless asked, no majors by default, no advisory
suppression without approval, no mixing with feature/perf changes), the parent-over-direct-pin
rule for transitive issues, and its nine-section output format. Do not keep a second full copy
of dependency-audit rules here; this file exists only to route agents to the single source of
truth.
