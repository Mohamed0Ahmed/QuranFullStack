---
name: performance-backend-review
description: >-
  Deep, review-only PERFORMANCE audit for the Quran Dashboard .NET / ASP.NET Core /
  EF Core / PostgreSQL backend. Use only when the user explicitly asks for a
  backend or database performance review: "performance-backend-review", slow
  endpoints, N+1 queries, EF Core query cost, tracking vs AsNoTracking, indexes
  or query plans, transaction/lock cost, pagination/result-size cost,
  over-fetching, large response payloads, streaming vs in-memory loading,
  caching/reuse of expensive reads, Quran importer/DataPipeline runtime, or slow
  backend tests. Review only: inspect changed backend scope unless asked wider,
  produce evidence-based findings and recommendations, and never edit code. Do
  not use for general review, clean-code, architecture, Spec Kit, or merge
  readiness.
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
