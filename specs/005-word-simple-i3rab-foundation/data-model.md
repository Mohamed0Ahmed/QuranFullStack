# Phase 1 Data Model — Word Simple I‘rab Foundation

One new PostgreSQL table (`quran_i3rab_rules`, 142 importer-seeded rows) plus **four new columns** added
inline to the existing `quran_word_morphology_segments`. DB columns `snake_case`; EF entities
`PascalCase` under `Domain/Quran/Words/Morphology/Irab/`. Types follow the Feature 002/003/004 convention:
`smallint` where values ≤ 32,767, `int` otherwise; Arabic text is `text` with default collation.

> **Authoritative morphology is never touched.** Feature 005 adds an i‘rab **label** keyed by identifier.
> It never modifies the original morphology columns, `quran_words`, the Uthmani/QPC text, or the
> `quran_pos_tags` seed, and never sets a form for the 208 NULL-`form_arabic_normalized` segments.

---

## 1. Relationships

```text
quran_word_morphology_segments (128,219 — EXISTING; +4 i3rab_* columns)
    │  i3rab_rule_id (nullable)  ──────────▶  quran_i3rab_rules.id   (FK, ON DELETE RESTRICT)
    │  i3rab_status (NOT NULL, CHECK ∈ {approved, needs_review, unsupported})
    │  i3rab_arabic (nullable text)          quran_i3rab_rules (142 rows / 67 families — NEW, importer-seeded)
    │  i3rab_review_reason (nullable text)        signature_key UNIQUE, rule_family, i3rab_arabic,
    ▼                                              default_status, description, sort_order
(read-only inputs, never mutated):
  kind, pos, case_feature(via word), verb_tense/voice(via word), features_raw/json, lemma_buckwalter
```

- **Grain**: i‘rab is **1:1 with a segment** (per-occurrence). No new per-segment table — the labels are
  inline columns. No per-word summary table — word summaries are composed at read time.
- **FK**: `quran_word_morphology_segments.i3rab_rule_id` → `quran_i3rab_rules.id` (nullable; `RESTRICT`).
- **Writes allowed**: only the four `i3rab_*` columns (via keyed `UPDATE`) and inserts into
  `quran_i3rab_rules`. No insert/delete/truncate of segment rows; no change to any other column or table.

---

## 2. New table — `quran_i3rab_rules` (the curated catalogue)

**142 rows**, one per distinct segment-token signature (coverage report §3.4), grouped into **67
families** via `rule_family`. Importer-seeded idempotently (upsert by `signature_key`); **not** `HasData`.
This table is the **single owner of user-facing Arabic labels**.

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `int` (identity) | NO | PK; FK target for `i3rab_rule_id` |
| `signature_key` | `text` | NO | **UNIQUE**. The segment signature, e.g. `STEM:N:GEN`, `STEM:V:PERF:ACT:3MS`, `STEM:PN:ALLAH:GEN`, `SUFFIX:PRON:3MP`, `STEM:N:GEN:1S` |
| `rule_family` | `text` | NO | One of the 67 families, e.g. `N.GEN`, `V.PERF.ACT`, `PRON.SUF`, `PN.ALLAH.GEN` (reporting/grouping only) |
| `i3rab_arabic` | `text` | NO | The **exact** canonical Arabic label for this signature (verbatim from coverage report §3.4) |
| `default_status` | `text` | NO | `CHECK ∈ {approved, needs_review, unsupported}`. **`approved` for all 142 rows in v1.** |
| `description` | `text` | YES | Dev/provenance note (e.g. POS meaning, seed-correction note) |
| `sort_order` | `smallint` | NO | Display/iteration order |

Indexes: PK on `id`; UNIQUE on `signature_key`; index on `rule_family` (reporting).

**Sample rows** (illustrative — full 142 come verbatim from coverage report §3.4):

| signature_key | rule_family | i3rab_arabic | default_status |
|---|---|---|---|
| `STEM:N:GEN` | `N.GEN` | اسم مجرور | approved |
| `STEM:N:NOM` | `N.NOM` | اسم مرفوع | approved |
| `STEM:N:ACC` | `N.ACC` | اسم منصوب | approved |
| `STEM:ADJ:GEN` | `ADJ.GEN` | صفة مجرورة | approved |
| `STEM:PN:ALLAH:GEN` | `PN.ALLAH.GEN` | لفظ الجلالة مجرور | approved |
| `STEM:V:PERF:ACT:3MS` | `V.PERF.ACT` | فعل ماض | approved |
| `STEM:V:IMPF:PASS:3MS` | `V.IMPF.PASS` | فعل مضارع مبني للمجهول | approved |
| `SUFFIX:PRON:3MP` | `PRON.SUF` | ضمير متصل للغائبين | approved |
| `STEM:T` | `T.TIME` | ظرف زمان | approved |
| `STEM:SUR` | `SUR` | حرف فجاءة | approved |
| `SUFFIX:P` | `P.SUFFIX` | لام الجر | approved |
| `STEM:N:GEN:1S` | `N.GEN.1S` | اسم مجرور مضاف إلى ياء المتكلم | approved |

> The **21 seed-label corrections** (spec FR-011) MUST appear with their corrected Arabic in this table
> (e.g. `T → ظرف زمان`, `RES → أداة حصر`, `STEM:INTG → اسم استفهام`, `INL → حروف مقطّعة (فواتح السور)`).
>
> **Exact column mapping (transcribe all 142 rows — do not invent or translate):**
> - `signature_key` ← coverage report **§3.4 column 1** ("seg signature", e.g. `STEM:N:GEN`,
>   `SUFFIX:PRON:3MP`). 142 distinct values; this is the lookup key.
> - `i3rab_arabic` ← coverage report **§3.4 "i‘rab (Arabic)" column** (verbatim Arabic).
> - `rule_family` ← the **§4 family (67 distinct)** that the signature rolls up to, **not** §3.4's finer
>   "rule key" column. Derive it by dropping the person/number/gender suffix from the signature
>   (e.g. `SUFFIX:PRON:3MP` and `SUFFIX:PRON:2MP` → family `PRON.SUF`; `STEM:V:PERF:ACT:3MS` and
>   `…:3MP` → `V.PERF.ACT`; `STEM:N:GEN` → `N.GEN`). `rule_family` is **reporting-only** — getting it
>   wrong does not affect labels or coverage, but the seed test (T025) asserts **exactly 67** distinct
>   families, so the collapse must be correct.

---

## 3. New columns on `quran_word_morphology_segments`

| Column | Type | Null | Notes |
|---|---|---|---|
| `i3rab_arabic` | `text` | YES | The simplified Arabic label shown to users; equals the matched rule's `i3rab_arabic` for `approved` rows |
| `i3rab_rule_id` | `int` | YES | **FK** → `quran_i3rab_rules.id` (`ON DELETE RESTRICT`); the rule that produced the label |
| `i3rab_status` | `text` | **NO** | `CHECK ∈ {approved, needs_review, unsupported}`; **all rows = `approved` after a successful run** |
| `i3rab_review_reason` | `text` | YES | Required by the gate for `needs_review` / `unsupported` (zero such rows in v1) |

Index: btree on `i3rab_rule_id`. (Optional partial index `WHERE i3rab_status <> 'approved'` for reporting
— zero rows in v1, cheap.)

**Consistency rules** (FR-004–FR-006; enforced by the application gate, not a single-column CHECK):

| `i3rab_status` | `i3rab_arabic` | `i3rab_rule_id` | `i3rab_review_reason` |
|---|---|---|---|
| `approved` | **required** | **required** | null |
| `needs_review` | optional (internal review-only) | **required** | **required** |
| `unsupported` | null (not shown) | null | **required** (non-empty) |

---

## 4. `I3rabStatus` enum (Domain)

```text
I3rabStatus = Approved | NeedsReview | Unsupported
```

- Stored as the lowercase snake string in `i3rab_status` (`approved` / `needs_review` / `unsupported`).
- Used in code by the assembler/validator; the entity property `WordMorphologySegment.I3rabStatus` is a
  `string?` (matching the existing `Kind`/`Pos` string-on-entity convention), mapped 1:1 to the column.

---

## 5. Entity changes (Domain)

**`WordMorphologySegment`** (existing — add four properties + one nav):

```text
+ string?            I3rabArabic
+ int?               I3rabRuleId
+ string?            I3rabStatus        // 'approved' | 'needs_review' | 'unsupported'
+ string?            I3rabReviewReason
+ QuranI3rabRule?    I3rabRule          // nav for the FK
```

**`QuranI3rabRule`** (new):

```text
int      Id
string   SignatureKey      // unique
string   RuleFamily
string   I3rabArabic
string   DefaultStatus     // 'approved' in v1
string?  Description
short    SortOrder
```

---

## 6. Migration shape (`AddWordSimpleI3rab`) — generated via EF tooling during `/implement`

Schema-only (no `HasData`). The migration MUST, in order:

1. `CREATE TABLE quran_i3rab_rules` (columns/constraints/indexes from §2).
2. `ALTER TABLE quran_word_morphology_segments`:
   - add `i3rab_arabic text NULL`,
   - add `i3rab_rule_id int NULL` + FK → `quran_i3rab_rules(id)` `ON DELETE RESTRICT` + index,
   - add `i3rab_status text NOT NULL DEFAULT 'unsupported'` (transient backfill default; see research R8)
     + `CHECK (i3rab_status IN ('approved','needs_review','unsupported'))`,
   - add `i3rab_review_reason text NULL`.

> The `'unsupported'` default only backfills the existing 128,219 rows so the NOT NULL add is valid; the
> first `generate-i3rab` run overwrites every row to `'approved'`. The catalogue rows are **not** seeded
> by the migration — the generator seeds them.

---

## 7. Validation rules (mapped to FR-029 hard checks)

| Check id | Rule |
|---|---|
| `I3RAB-SEG-STATUS-COMPLETE` | every segment has a non-null `i3rab_status` ∈ the allowed set (128,219 rows) |
| `I3RAB-APPROVED-CONSISTENT` | `approved` ⇒ `i3rab_arabic` **and** `i3rab_rule_id` non-null |
| `I3RAB-NEEDS-REVIEW-CONSISTENT` | `needs_review` ⇒ `i3rab_rule_id` **and** `i3rab_review_reason` non-null |
| `I3RAB-UNSUPPORTED-CONSISTENT` | `unsupported` ⇒ non-empty `i3rab_review_reason` |
| `I3RAB-WORD-DISPLAYABLE` | every readable word (77,432) yields an ordered segment-label display |
| `I3RAB-RULE-RESOLVES` | every non-null `i3rab_rule_id` resolves to a `quran_i3rab_rules` row |
| `I3RAB-SOURCE-COLUMNS-UNCHANGED` | all original morphology columns **and** `quran_words` **and** the `quran_pos_tags` seed unchanged before & after (hash/snapshot of non-i3rab segment columns + row-count/hash of `quran_words` and `quran_pos_tags`) — FR-020, FR-023 |
| `I3RAB-SEGMENT-ROWCOUNT-STABLE` | `quran_word_morphology_segments` row count = 128,219 before & after; no insert/delete |
| `I3RAB-NULL-FORM-NOT-INVENTED` | the 208 `form_arabic_normalized IS NULL` rows remain NULL after the run |

Warnings (never gate): `I3RAB-COVERAGE`, `I3RAB-RULE-USAGE`, `I3RAB-UNKNOWN-PATTERNS`,
`I3RAB-NEEDS-REVIEW-SUMMARY`, `I3RAB-LABEL-REVIEW`.

**v1 expected result after commit**: 128,219 / 128,219 segments `approved`; 0 `needs_review`; 0
`unsupported`; 77,432 / 77,432 words displayable; 142 catalogue rows; 67 families; 208 NULL forms still
NULL; 0 changes to original morphology columns.

---

## 8. Read-time word summary (derived — NOT stored)

For display (a later feature), compose per word:

```sql
SELECT string_agg(i3rab_arabic, '، ' ORDER BY segment_number)
FROM   quran_word_morphology_segments
WHERE  quran_word_id = :id;
```

Idiom collapses (`P+PRON → جار ومجرور`), «محل …» role refinements (`V+PRON → في محل نصب مفعول به`), and
pattern-aware overrides (`P+SUB → جار، مجرور`; `SUP+AMD → حرف استدراك`; `ACC+PREV → كافّة ومكفوفة`) are
**read-layer behavior** and are **not** stored in v1 (spec FR-019). No `quran_word_i3rab` table exists.
