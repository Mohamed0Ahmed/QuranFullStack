# Performance Backend Review

Use the project backend performance review skill as the checklist:

.claude/skills/performance-backend-review/SKILL.md

Explicit-invocation, review-only backend / database performance audit (.NET / ASP.NET Core /
EF Core / PostgreSQL, including the DataPipelines importers). Inspect only the changed backend
scope unless a wider audit is asked for. Cover API/query cost (N+1, projections, tracking vs
AsNoTracking, round-trips, pagination, payload size), PostgreSQL indexes, transaction/lock
cost, importer/DataPipeline runtime, cache/reuse, and backend test runtime.

Evidence-based findings only — no speculative micro-optimizations. Never recommend weakening
Quran data integrity, atomicity, source hashes/manifest checks, validation gates, or
provenance for speed. Review only: do not implement fixes unless explicitly asked.
Return the review using the output format defined in the skill.
