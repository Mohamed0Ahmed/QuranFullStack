# Contract — Validation Checks & Report Schema

The gate runs **9 hard checks** (any failure ⇒ rollback) and **5 warnings** (informational). One
Markdown+JSON report artifact is written per run (mirrors Feature 004's
`MarkdownJsonMorphologyReportWriter`).

## Hard checks (gate the commit)

| Id | Expected | Fails when |
|---|---|---|
| `I3RAB-SEG-STATUS-COMPLETE` | 128,219 non-null statuses ∈ {approved, needs_review, unsupported} | any segment has null/invalid status |
| `I3RAB-APPROVED-CONSISTENT` | 0 violations | an `approved` row missing `i3rab_arabic` or `i3rab_rule_id` |
| `I3RAB-NEEDS-REVIEW-CONSISTENT` | 0 violations | a `needs_review` row missing `i3rab_rule_id` or `i3rab_review_reason` |
| `I3RAB-UNSUPPORTED-CONSISTENT` | 0 violations | an `unsupported` row with empty `i3rab_review_reason` |
| `I3RAB-WORD-DISPLAYABLE` | 77,432 | any readable word cannot derive an ordered segment-label display |
| `I3RAB-RULE-RESOLVES` | 0 dangling | any non-null `i3rab_rule_id` not in `quran_i3rab_rules` |
| `I3RAB-SOURCE-COLUMNS-UNCHANGED` | unchanged | any original morphology column, **`quran_words`**, or the **`quran_pos_tags` seed** differs before/after (snapshot/hash of non-i3rab segment columns + row-count/hash of `quran_words` and `quran_pos_tags`) — FR-020, FR-023 |
| `I3RAB-SEGMENT-ROWCOUNT-STABLE` | 128,219 = 128,219 | segment row count changed; any insert/delete |
| `I3RAB-NULL-FORM-NOT-INVENTED` | 208 NULL → 208 NULL | any of the 208 `form_arabic_normalized` rows became non-NULL |

## Warnings (never gate)

| Id | Signal |
|---|---|
| `I3RAB-COVERAGE` | per-status counts/percentages (v1: 100% approved) |
| `I3RAB-RULE-USAGE` | per-rule (and per-family) hit counts; rules that never fired |
| `I3RAB-UNKNOWN-PATTERNS` | any segment signature with no catalogue match (v1: empty) |
| `I3RAB-NEEDS-REVIEW-SUMMARY` | enumerated needs-review items (v1: empty) |
| `I3RAB-LABEL-REVIEW` | labels diverging from `quran_pos_tags.arabic_label` (the 21 corrections) |

## Report JSON schema (sibling `.json`; Markdown mirrors it)

```json
{
  "runUtc": "2026-06-12T00:00:00Z",
  "verdict": "PASS",
  "persisted": true,
  "forced": false,
  "totals": {
    "segments": 128219,
    "words": 77432,
    "rules": 142,
    "families": 67,
    "approved": 128219,
    "needsReview": 0,
    "unsupported": 0,
    "wordsDisplayable": 77432,
    "nullFormsPreserved": 208
  },
  "checks": [
    { "id": "I3RAB-SEG-STATUS-COMPLETE", "severity": "hard", "expected": "128219", "observed": "128219", "passed": true }
  ],
  "ruleUsage": [
    { "signatureKey": "STEM:N:GEN", "ruleFamily": "N.GEN", "i3rabArabic": "اسم مجرور", "segments": 10403 }
  ],
  "warnings": [
    { "id": "I3RAB-LABEL-REVIEW", "note": "21 rule-layer label corrections present (catalogue owns labels; overrides quran_pos_tags seed)" }
  ]
}
```

## Markdown report sections (mirror Feature 004's report)

1. Header — run UTC, verdict, persisted, forced.
2. **Totals** — segments, words, rules, families, per-status coverage, words displayable, NULL forms.
3. **Checks** — table of every hard check (id, severity, expected, observed, passed).
4. **Rule usage** — per-rule / per-family hit counts (top + full).
5. **Warnings** — coverage, label-review (the 21 corrections), unknown patterns (none), needs-review (none).
6. **Notes** — commit/rollback outcome; "labels are simplified, not authoritative scholarly i‘rab."

> **Quranic data safety:** the report shows counts and signature keys / individual labels only — never
> assembled ayah text. Failure reports list violating segment ids, not Quran content.
