# Feature 010 — Quran Full I'rab Foundation: Backend Reports

This folder holds the **backend implementation, verification, and completion reports** for Feature 010
(Quran Full I'rab Foundation) — the ayah-level full إعراب data foundation cloned from the Tafsir
pipeline (Feature 007).

Planning lives under `docs/feature-010-quran-full-i3rab-foundation/`. **This folder is only for backend reports.**

## Where reports go

- **Human-authored reports** belong here, numeric-prefixed per `Backend/report/README.md`.
- **Generated importer output** (`full-i3rab-import-report.md` / `.json`) defaults to
  `/projects/Dashboard/App/resources/report/quran-full-i3rab/` (local, gitignored). A snapshot of the
  verified `forced=false` PASS run is kept here as `full-i3rab-import-report.{md,json}`; the
  rerun-refusal (idempotency) output is kept separately as `full-i3rab-rerun-refusal-report.{md,json}`.

## Report index

| Report | Status | Summary |
| --- | --- | --- |
| [001-build-verification.md](./001-build-verification.md) | PASS | `dotnet build QuranDashboard.sln` — 0 errors, 0 warnings |
| [002-test-verification.md](./002-test-verification.md) | PASS | 42 FullI3rab tests green |
| [003-clean-code-self-check.md](./003-clean-code-self-check.md) | PASS | Clean Architecture boundaries, file sizes within thresholds |
| [004-test-code-self-check.md](./004-test-code-self-check.md) | PASS | Real-infra tests, synthetic source-safe fixtures |
| [005-real-import-run-summary.md](./005-real-import-run-summary.md) | PASS | Full staged package import: 4 sources, 6,236 mappings/source, verdict `pass` |
| [006-final-completion-report.md](./006-final-completion-report.md) | IMPLEMENTATION COMPLETE | Phases 1–5 done; provenance remains `unknown` / internal-only |
| [full-i3rab-import-report.md](./full-i3rab-import-report.md) | PASS | Snapshot of verified real-run importer report (Markdown) — `verdict=pass`, `persisted=true`, `forced=false`, 21/21 hard checks |
| [full-i3rab-import-report.json](./full-i3rab-import-report.json) | PASS | Snapshot of verified real-run importer report (JSON) — same run |
| [full-i3rab-rerun-refusal-report.md](./full-i3rab-rerun-refusal-report.md) | REFUSED | Idempotency evidence: second run without `--force` refused (exit 2), no mutation (Markdown) |
| [full-i3rab-rerun-refusal-report.json](./full-i3rab-rerun-refusal-report.json) | REFUSED | Idempotency evidence (JSON) |

> Status: reports added 2026-06-17 (Phase 5 polish + real import).
