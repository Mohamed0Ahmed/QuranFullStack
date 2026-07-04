# Stem Count Drift Diagnostic Report

- Feature: 020 — Lexical Polish and Project Hygiene
- Run date: 2026-07-04
- Scope: diagnostic only, read-only DB/source inspection

> Superseded decision note (2026-07-04): this diagnostic's evidence remains historical, but its
> recommendation to accept `quran_stems=11,848` is superseded by the later product/data decision to
> preserve U+06E6 in segment render while normalizing it out of `quran_stems` identity. The current target
> count is `quran_stems=11,843`; see `stem-identity-normalization-implementation-report.md`.

## Verdict

**COUNT_11848_ACCEPTED**

`quran_stems = 11,848` is the correct count for the currently staged dot-render-fixed enriched artifact under the current Dashboard enriched importer identity rule.

The old `11,843` count is traceable to the pre-dot-fix attempted import where U+06E6 (`ۦ`) was effectively absent from stem display text. If U+06E6 is stripped from the current staged artifact's head STEM display values, the derived distinct stem count becomes exactly `11,843`. With U+06E6 preserved as required by the dot-render fix, 5 valid stem display values no longer collapse into their stripped counterparts.

No invalid, duplicate, orphan, or unintended stem rows were found.

## Current DB Counts

Read-only query against local `quran_dashboard` after the successful enriched import:

| Table | Count |
|---|---:|
| quran_word_morphology | 77,432 |
| quran_word_morphology_segments | 128,219 |
| quran_roots | 1,642 |
| quran_lemmas | 4,817 |
| quran_lemma_analyses | 4,832 |
| quran_stems | 11,848 |
| quran_pos_tags | 49 |

The import did persist; this diagnostic did not encounter a stale/rolled-back state.

## Source of Old `11,843` Expectation

Repository search found `11,843` in:

`Backend/report/feature-020-lexical-polish-and-project-hygiene/enriched-morphology-clean-reset-acceptance-report.md`

That report recorded the previous failed import attempt's attempted totals before rollback:

| Table | Attempted rows |
|---|---:|
| quran_stems | 11,843 |

No active code constant, test assertion, spec invariant, or importer hard gate for `11843` was found in the searched Dashboard repo content. The previous value was an observed attempted total from the old staged artifact, not an independent invariant.

## Importer Stem Identity Rule

The current enriched importer mints stem dimensions only from each word's head STEM segment:

- `EnrichedDimensionBuilder` selects the lowest-numbered `STEM` segment as the head STEM.
- `ResolveOrCreateStem` uses `segment.FormArabic` as `stem_text`.
- Blank head stem text is skipped.
- Segment-level STEM rows later resolve to this value-based stem index; secondary STEM segments do not mint new `quran_stems` rows.

Relevant implementation facts:

- `stem_text` identity is Arabic display text, not Buckwalter.
- `quran_stems` has no Buckwalter column.
- Therefore preserving U+06E6 in `formArabic` can legitimately split stem identities that previously collapsed when U+06E6 was stripped.

## Independent Artifact-Derived Count

Read-only probe over:

`resources/import-sources/quran-enriched-morphology/corpus-based-enriched-morphology.dashboard-ready.json`

Probe logic matched the importer identity rule: choose each record's first `kind="STEM"` segment and count distinct non-empty `formArabic` values.

| Metric | Count |
|---|---:|
| records | 77,432 |
| distinct head STEM `formArabic` values | 11,848 |
| distinct head STEM values if U+06E6 (`ۦ`) is stripped | 11,843 |
| delta caused by preserving U+06E6 | 5 |
| empty head STEM values | 0 |
| secondary STEM segments | 483 |
| head stem values containing U+06E6 | 17 |

DB and artifact agree: both produce `11,848` stems.

## Five-Row Drift Table

These 5 current rows contain U+06E6 and would collapse into an existing stripped stem text if U+06E6 were removed. They explain the full `11,848 - 11,843 = 5` delta.

| stem id | stem_text | stripped existing stem_text | stripped stem id | first location | first segment | form_buckwalter | pos | words_count | word refs | segment refs | reference kind | sample locations |
|---:|---|---|---:|---|---|---|---|---:|---:|---:|---|---|
| 425 | `هِۦ` | `هِ` | 2207 | `2:22:13` | `2:22:13:2` | `hi.` | `PRON` | 300 | 300 | 300 | head word STEM | `100:4:2`, `100:5:2`, `10:107:17`, `10:16:10`, `10:24:10`, `10:40:4`, `10:40:9`, `10:51:10` |
| 3372 | `يُحْىِۦ` | `يُحْىِ` | 1298 | `2:258:18` | `2:258:18:1` | `yuHoYi.` | `V` | 11 | 11 | 11 | head word STEM | `10:56:2`, `2:258:18`, `2:259:12`, `23:80:3`, `30:24:11`, `3:156:31`, `40:68:3`, `44:8:5` |
| 3373 | `أُحْىِۦ` | `أُحْىِ` | 4099 | `2:258:22` | `2:258:22:1` | `>uHoYi.` | `V` | 1 | 1 | 1 | head word STEM | `2:258:22` |
| 5451 | `هَٰذِهِۦ` | `هَٰذِهِ` | 716 | `4:78:14` | `4:78:14:1` | `ha\`*ihi.` | `DEM` | 23 | 23 | 23 | head word STEM | `10:22:38`, `11:64:2`, `11:99:3`, `12:108:2`, `12:65:12`, `17:72:4`, `18:19:24`, `18:35:11` |
| 10494 | `نُحْىِۦ` | `نُحْىِ` | 14645 | `15:23:3` | `15:23:3:1` | `nuHoYi.` | `V` | 2 | 2 | 2 | head word STEM | `15:23:3`, `50:43:3` |

Notes:

- `stem_buckwalter` is not stored in `quran_stems`; the table uses the persisted first segment's `form_buckwalter` as source evidence.
- All 5 rows are referenced by both `quran_word_morphology.stem_id` and `quran_word_morphology_segments.stem_id`.
- All 5 rows are word-level/head-stem references, not secondary-only segment-level stem artifacts.
- All 5 come from real Corpus Buckwalter forms containing `.`, now correctly rendered as U+06E6 (`ۦ`).

## Reference / Integrity Checks

Read-only DB checks:

| Check | Observed | Result |
|---|---:|---|
| duplicate stem ids | 0 | PASS |
| duplicate `stem_text` groups | 0 | PASS |
| empty/null `stem_text` | 0 | PASS |
| stems with zero total refs | 0 | PASS |
| stems with zero word refs | 0 | PASS |
| invalid `first_word_order_in_mushaf` | 0 | PASS |
| duplicate `first_word_order_in_mushaf` | 0 | PASS |
| `words_count` mismatches word refs | 0 | PASS |

Artifact checks:

| Check | Observed | Result |
|---|---:|---|
| distinct head stems | 11,848 | PASS |
| empty head stems | 0 | PASS |
| U+06E6-preserving delta | 5 | PASS |

No mismatch was found between importer report and DB count: both report `quran_stems=11848`.

## Data Problem Assessment

No real data problem found.

- The 5 rows are not duplicates under the current display identity rule; `stem_text` remains unique.
- They are not orphan rows; each has word and segment references.
- They are not first-order artifacts; each has a valid unique `first_word_order_in_mushaf`.
- They do not come from the dot anchor `12:101:14:2`; that anchor is a suffix PRON and does not mint a stem dimension.
- They are not a Feature 018 secondary-stem side effect; `quran_stems` mints from head STEM values only, and all 5 rows are head word STEM references.
- They are real Corpus-derived forms with `.` Buckwalter preserved and now rendered to U+06E6.

## Recommendation

Update acceptance expected `quran_stems` count from `11,843` to `11,848` for the dot-render-fixed enriched artifact.

Do not fix the artifact or importer for this count. The count increase is the expected consequence of preserving a real Quranic mark (`ۦ`) in stem display identity after fixing the SourceAudit Buckwalter mapping.

Recommended next action:

1. Update Feature 020 acceptance/report expectations to `quran_stems=11,848`.
2. Run the final clean reset acceptance with the fixed staged artifact and updated expected count.
3. Keep `MORPH-DIM-COUNTS` informational unless product decides stem identity should normalize away Quranic marks; that would be a separate schema/import semantics decision and should not be bundled with this fix.

## Safety Confirmation

- no `reset-db`
- no import run
- no DB writes
- no production/remote DB touched
- no code changes
- no schema/migration changes
- no SourceAudit changes
- no `PosTagSeed` changes
- no lemma_text / quran_lemma_analyses decision changes
- no commit
