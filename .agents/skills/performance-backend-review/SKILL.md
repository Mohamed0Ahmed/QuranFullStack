---
name: performance-backend-review
description: >-
  Deep, review-only PERFORMANCE audit for the Quran Dashboard .NET / ASP.NET Core /
  EF Core / PostgreSQL backend. Explicit-invocation only — use this skill when the user
  explicitly asks for a backend or database performance review, e.g. running
  "performance-backend-review", slow backend endpoints, N+1 queries, EF Core query
  performance, AsNoTracking vs tracking, missing indexes or query plans, transaction or
  lock cost, pagination / result-size cost, over-fetching or large response payloads,
  streaming vs loading into memory, caching or reuse of expensive reads, Quran importer /
  DataPipeline runtime cost, or slow backend tests (Testcontainers, repeated real-source
  imports). It inspects only the changed backend scope unless a wider audit is requested,
  and produces evidence-based findings, severities, and recommendations — never code fixes.
  Do NOT use this skill for general code review, engineering review, PR review, clean-code
  review, architecture or structure review, Spec Kit / phase review, or "is this safe to
  merge" — those belong to engineering-review (and backend-structure-review). It triggers
  only on explicit backend performance intent, not on the word "review" by itself.
---

# Performance Backend Review

OpenCode / Codex pointer for the project skill. The canonical definition — the full
checklist, severity model, anti-noise rules, Quran-data-safety constraint, and the exact
11-section output format — lives in the Claude Code skill:

`.claude/skills/performance-backend-review/SKILL.md`

Use that file as the authoritative checklist. In short: this is an **explicit-invocation,
review-only** backend/database performance audit (.NET / ASP.NET Core / EF Core /
PostgreSQL, including the `DataPipelines` importers and `tools/QuranDashboard.DataImporter`).
Inspect only the changed backend scope unless a wider audit is requested. Produce
evidence-based findings, severities, and recommendations only — do not modify code, and
never recommend trading Quran data integrity, atomicity, or provenance for speed. Return the
review using the output format defined in the canonical skill.
