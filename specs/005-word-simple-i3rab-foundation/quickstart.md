# Quickstart — Word Simple I‘rab Foundation

How to build, run, and verify the i‘rab generation. Operator/CI only; no UI, no API.

## Prerequisites

- Feature 004 morphology is **imported and validated** (segments = 128,219). i‘rab generation reads this
  data; it refuses on missing/stale morphology.
- The `AddWordSimpleI3rab` migration is applied (adds `quran_i3rab_rules` + the four `i3rab_*` columns).
  Generate it with EF tooling during implementation (Backend policy: only when explicitly requested), then
  apply it.
- PostgreSQL connection configured via user-secrets (as for the other importers).

## Generate

```bash
# from the Backend solution root
dotnet run --project tools/QuranDashboard.DataImporter -- generate-i3rab
# re-run / overwrite an already-populated set:
dotnet run --project tools/QuranDashboard.DataImporter -- generate-i3rab --force
# custom report directory:
dotnet run --project tools/QuranDashboard.DataImporter -- generate-i3rab --report-out ./out
```

Exit `0` = committed and all hard checks passed. Non-zero = refusal or a gated rollback (see the report).

## Expected result (v1)

- `quran_i3rab_rules`: **142** rows / **67** families; every `default_status = approved`.
- `quran_word_morphology_segments`: **128,219** rows with `i3rab_status = approved`, a non-null
  `i3rab_arabic`, and a resolvable `i3rab_rule_id`; **0** needs_review / unsupported.
- The **208** NULL `form_arabic_normalized` rows: still NULL, each with an i‘rab label.
- Original morphology columns and row count: **unchanged**.
- Report at `resources/report/words-simple-i3rab/simple-i3rab-generation-report.md` (+ `.json`).

## Verify (SQL spot-checks)

```sql
-- 1) full coverage, all approved
SELECT i3rab_status, count(*) FROM quran_word_morphology_segments GROUP BY 1;
--   approved | 128219

-- 2) catalogue size
SELECT count(*) AS rules, count(DISTINCT rule_family) AS families FROM quran_i3rab_rules;
--   142 | 67

-- 3) known labels (FR-011 corrections + لفظ الجلالة)
SELECT s.i3rab_arabic
FROM quran_word_morphology_segments s JOIN quran_words w ON w.id = s.quran_word_id
WHERE w.location = '1:1:2' AND s.segment_number = 1;          -- لفظ الجلالة مجرور

-- 4) read-time word summary (e.g. بِحَمْدِكَ 2:30:20)
SELECT string_agg(i3rab_arabic, '، ' ORDER BY segment_number)
FROM quran_word_morphology_segments s JOIN quran_words w ON w.id = s.quran_word_id
WHERE w.location LIKE '2:30:20';   -- حرف جر، اسم مجرور، ضمير متصل في محل جر مضاف إليه

-- 5) NULL forms preserved
SELECT count(*) FROM quran_word_morphology_segments WHERE form_arabic_normalized IS NULL;  -- 208
```

## Tests

```bash
dotnet test Backend/tests/QuranDashboard.Tests   # unit (signature/catalogue/assembler) + Testcontainers integration
```

Integration tests assert: 100% approved coverage, idempotency, `--force`, refusal on stale morphology and
on non-empty target without `--force`, source columns unchanged, segment row count stable, 208 NULL forms
preserved, FK + CHECK enforced, and each hard-check failure rolls back.
