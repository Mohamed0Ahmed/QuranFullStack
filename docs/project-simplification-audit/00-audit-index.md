# 00 — Audit Index

**Project Simplification, Cost Reduction & Agent Context Audit** — Quran Dashboard / المنهج القرآني monorepo.
Contract: [PROJECT_SIMPLIFICATION_AUDIT_BRIEF.md](PROJECT_SIMPLIFICATION_AUDIT_BRIEF.md) (canonical; read-only audit, no fixes implemented).

## Baseline

| Item | Value |
|---|---|
| Audited branch | `dev` |
| Audited commit | `72792ba9ff589c66aa25632a464b56b8bf7787af` ("Merge access catalogue readiness feature") |
| Audit dates | 2026-08-08 → 2026-08-09 |
| Baseline gate | PASSED — branch `dev`, HEAD matches the brief's observed remote baseline; working tree clean **except** the untracked `docs/project-simplification-audit/` folder itself (the audit's own workspace, containing only the brief at start) |
| Environment | i7-6820HQ (8 threads), 14 GB RAM, Docker 29.7.1, .NET SDK 10.0.110, Node v20.20.2 |

## Reports

| # | Report | Scope (brief §) |
|---|---|---|
| 01 | [Executive summary](01-executive-summary.md) | §27 — the 18 headline answers |
| 02 | [Test suite audit](02-test-suite-audit.md) | §9 (Audit A), Q22–31 |
| 03 | [Agent context & instruction audit](03-agent-context-instruction-audit.md) | §7 + §10 (Audit B), Q1–8 |
| 04 | [Custom skills audit](04-custom-skills-audit.md) | §11 + §12 (Audit C), Q16–21 |
| 05 | [Memory & retrieved-context audit](05-memory-context-audit.md) | §13 (Audit D) |
| 06 | [README / markdown decision audit](06-readme-markdown-decision-audit.md) | §14 + §23 (Audit E), Q9–15 |
| 07 | [Frontend styling audit](07-frontend-styling-audit.md) | §15 + §22 (Audit F), Q32–42 |
| 08 | [Architecture & code size audit](08-architecture-code-size-audit.md) | §16 + §24 (Audit G), Q53–64 |
| 09 | [API surface & payload audit](09-api-surface-payload-audit.md) | §17 + §24 (Audit H), Q43–52 |
| 10 | [Build / test / review workflow cost audit](10-build-workflow-cost-audit.md) | §19 + §20 (Audit I), Q65–68 |
| 11 | [Cross-cutting priorities](11-cross-cutting-priorities.md) | §21 + §26 — nine independent workstreams (not an implementation plan) |
| 12 | [Post-audit review handoff](12-post-audit-review-handoff.md) | §28 — ready-to-use Sol + Claude review prompts |

## Machine-readable evidence (`data/`)

| File | Produced by |
|---|---|
| `loc-inventory.json` | Phase 1 static inventory (LOC categories, largest files/dirs) |
| `test-inventory-backend.json`, `test-inventory-frontend.json` | Phase 1 (tests, lanes, fixtures, clusters) |
| `instruction-inventory.json` | Phase 1 (74 files, routing graph, 8 task traces, duplicated rules) |
| `skill-inventory.json` | Phase 1 (10 skills, closures, adapters) |
| `markdown-decision-inventory.json` | Phase 1 (138 md files, 111 decisions) |
| `style-inventory.json` | Phase 1 (121 SCSS files, Tailwind/qd-*/token usage, repeated blocks) |
| `endpoint-inventory-backend.json`, `endpoint-consumers-frontend.json` | Phase 1 (85 operations, field usage, screens, caching) |
| `workflow-gate-inventory.json` | Phase 1 (18 scripts, 22 gates, cadence matrix) |
| `history-evidence.json` | Phase 1 (git-history "why" evidence) |
| `runtime-measurements.json` | Phase 1b — solo, strictly sequential lane timings |
| `endpoint-classification.json` | Report 09 (authoritative endpoint/field classifications) |

**API explorer:** [`api-explorer/index.html`](api-explorer/index.html) — static, self-contained (opens as `file://`, no server, no external requests), seeded from Swagger and enriched from code; synthetic examples only. Point-in-time artifact pinned to this commit; the Swagger + `check-api-contract` pipeline remains the contract truth.

## Method

1. **Phase 1 — static inventory:** 11 parallel read-only agents produced the `data/` files.
2. **Phase 1b — runtime measurement:** one solo agent, strictly sequential (backend build + 8 lanes, 3,231 tests; frontend 5 gates, 2,964 tests; zero failures; all Testcontainers cleaned, `docker ps -a` empty afterward).
3. **Phase 2 — report authoring:** one agent per report; each spot-verified inventory claims before asserting.
4. **Phase 3 — adversarial verification:** one independent skeptic per report + a cross-report completeness/consistency critic recomputed the load-bearing claims (10 verifiers, 66 findings: 1 critical, 12 major, 53 minor); all accepted corrections were applied by a surgical fix round with per-finding re-verification (two rejections, each documented with evidence).
5. **Phase 4 — synthesis:** reports 00/01/11/12 written from the corrected, verified numbers.

Roughly 40 sub-agent passes, ~2,100 tool invocations and ~6.2M sub-agent tokens were spent across the phases. Read-only discipline held throughout: at completion `git status` shows only the untracked `docs/project-simplification-audit/` folder; no repository file outside it was modified, no commits or pushes were made, no destructive script was executed.

## Measurement limitations (declared, not oversights)

- **Runtime:** e2e suite (needs live DB + both servers), `canonical-data` lane, `create-smoke-dump`, backend `pre-pr` (estimated 5–7 min from shard supersets), `check-api-contract` end-to-end cost, frontend cross-cut lanes — unmeasured; single solo run per measured lane (no variance); build timed with warm caches.
- **Context:** all token figures are bytes/4 static full-read upper bounds on *prescribed* reading; actual runtime read behavior is uninstrumented. Spec-artifact sizes unmeasured (no Spec-Kit feature open at baseline). Out-of-repo context (user-global config, session injections) observed but not byte-counted.
- **Consumers:** out-of-repo consumers of anonymous GET endpoints cannot be excluded statically; 157+ generically-named DTO fields carry UNKNOWN_CONSUMER honestly; payload bytes are static estimates pending wire capture (`\uXXXX`-escaping assumption LIKELY, unverified at runtime).
- **Usage:** Cursor editor usage UNKNOWN (rule file maintained, no attributable commits); Codex CLI activity CONFIRMED (refs/codex checkpoints 2026-08-05→07 UTC) but usage *level* NEEDS_MEASUREMENT; per-feature gate-firing frequency UNKNOWN (no CI, no run log); historical test flakiness has no records.
- **Analysis:** duplicate-style/spec detection is textual/title-based (misses reordered or renamed duplication); words/access README adjudication is structure-level (LIKELY); morphology-table row counts asserted nowhere in code, so several payload formulas lack a row-count anchor.

## Status

The audit answers all 68 mandatory questions of brief §25 (mapped per report above), proposes no implementation, and leaves a machine-readable evidence base sufficient for remediation planning without another repository-wide discovery pass. Independent Sol and Claude reviews (report 12) are the required next step.

```text
AUDIT_COMPLETE_WITH_MEASUREMENT_GAPS
```
