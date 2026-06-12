# Contract — Console Verb (`generate-i3rab`)

The existing `tools/QuranDashboard.DataImporter` console host gains a **fourth verb**. The i‘rab
generation is operator/CI-run only — **never** exposed over HTTP.

## Usage

```text
QuranDashboard.DataImporter import-foundation  [...]              # Feature 002 (unchanged)
QuranDashboard.DataImporter rebuild-words       [...]             # Feature 003 (unchanged)
QuranDashboard.DataImporter import-morphology   [...]             # Feature 004 (unchanged)
QuranDashboard.DataImporter generate-i3rab [--report-out <path>] [--force]   # this feature
```

`generate-i3rab` is **DB-to-DB**: it reads the populated morphology and writes the inline `i3rab_*`
columns + seeds `quran_i3rab_rules`. It reads **no** source files.

### `generate-i3rab` arguments

| Argument | Required | Meaning |
|---|---|---|
| `--report-out <path>` | no | Directory for the Markdown+JSON report. Defaults to `resources/report/words-simple-i3rab/`. |
| `--force` | no | Recompute and overwrite an already-populated i‘rab set. Without it, a non-empty target causes refusal. |

Unknown arguments are rejected with usage text, consistent with the existing parser.

## Behavior (ordered)

1. **Preflight — morphology readiness.** Verify the morphology is present and complete (segment count =
   the expected 128,219; `quran_word_morphology` non-empty). If missing/stale → **refuse**, write nothing,
   exit non-zero with a clear message. *(FR-025)*
2. **Preflight — target emptiness.** If any segment already carries a generated i‘rab (non-default status
   / non-null `i3rab_rule_id`) and `--force` is **absent** → **refuse**, write nothing, exit non-zero.
   With `--force`, continue and recompute all rows. *(FR-027)*
3. **Seed catalogue.** Idempotently upsert the 142 `quran_i3rab_rules` rows by `signature_key`.
4. **Assemble.** For each segment: build its signature (research R5) → look it up in the catalogue → set
   `(i3rab_arabic, i3rab_rule_id, i3rab_status='approved', i3rab_review_reason=null)`. A segment whose
   signature has no catalogue match → `i3rab_status='unsupported'` + a reason (expected count: 0 in v1).
   *(FR-001, FR-012)*
5. **Write (one transaction).** `COPY` the per-segment tuples to a temp table, `UPDATE … FROM` the four
   columns keyed by segment id, alongside the catalogue seed. *(research R4)*
6. **Validate (gate).** Run the 9 hard checks; if all pass → `COMMIT`; else `ROLLBACK` (write nothing).
   *(FR-026, FR-029)*
7. **Report.** Write the Markdown+JSON report (per-status coverage, per-rule usage, unmatched signatures,
   needs-review summary, seed-divergence list, every hard-check result, verdict). *(FR-031)*

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Generation committed; all hard checks passed; report written. |
| non-zero | Refusal (stale morphology / non-empty without `--force`) **or** a hard-check failure (rolled back). A report (or refusal message) is still written. |

## Hard guarantees (asserted by the gate)

- Writes **only** `i3rab_arabic`, `i3rab_rule_id`, `i3rab_status`, `i3rab_review_reason` on segments, and
  inserts into `quran_i3rab_rules`. No other column/table is touched. *(FR-020, FR-023)*
- Segment row count stays **128,219**; no insert/delete/truncate of segments. *(FR-021)*
- The **208** NULL `form_arabic_normalized` rows stay NULL; no form is invented. *(FR-022)*
- `quran_words`, original morphology columns, and the `quran_pos_tags` seed are unchanged. *(FR-020, FR-023)*
