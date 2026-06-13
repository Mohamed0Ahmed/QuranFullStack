# Phase 0 Research — Word Simple I‘rab Foundation

All material decisions were already locked by the spec (with its 2026-06-12 Clarifications), the planning
report, and the finalized coverage report. This file records the design decisions, their rationale, and
the alternatives rejected, including the two items the spec deferred to planning (verb name, report path).
There are **no open NEEDS CLARIFICATION** items.

---

## R1 — Generation altitude: DB-to-DB, not source-driven

- **Decision**: Read the **populated Feature 004 morphology** from PostgreSQL, assemble i‘rab in memory,
  and write back to the same DB. No external/source files are read.
- **Rationale**: Every input already exists in the morphology tables (`kind`, `pos`, `case_feature`,
  `verb_tense`, `verb_voice`, `features_raw`/`features_json`, `lemma_buckwalter`). This is the Feature 003
  "DB-to-DB rebuild" altitude, not the Feature 004 "source-driven import" altitude.
- **Alternatives rejected**: Re-reading the QAC source files (unnecessary; morphology is authoritative and
  already validated). Computing labels at read time only (rejected by the spec — labels are stored inline).

## R2 — Console verb name & host

- **Decision**: Add a **fourth verb `generate-i3rab`** to the existing `tools/QuranDashboard.DataImporter`
  host: `generate-i3rab [--report-out <path>] [--force]`. Operator/CI only — never HTTP.
- **Rationale**: Mirrors `import-foundation` / `rebuild-words` / `import-morphology`. No new project; the
  host already wires DI and argument parsing. (Resolves the spec's deferred "generator invocation" item.)
- **Alternatives rejected**: A new console project (unnecessary). An HTTP/API trigger (out of scope, and a
  data-foundation job must not be web-exposed).

## R3 — Report artifact path & format

- **Decision**: Write a **Markdown + JSON** report to `resources/report/words-simple-i3rab/` (default),
  e.g. `simple-i3rab-generation-report.md` (+ `.json`), overridable via `--report-out`.
- **Rationale**: Mirrors Feature 004's `resources/report/words-morphology/morphology-import-report.md`
  convention and its `MarkdownJsonMorphologyReportWriter`. (Resolves the spec's deferred "report path"
  item.) One artifact per run; same Markdown table shape (totals, per-status coverage, per-rule usage,
  hard-check results, warnings, verdict).
- **Alternatives rejected**: Console-only output (not auditable/diffable like Feature 004's reports).

## R4 — Bulk write mechanism for the inline columns

- **Decision**: `COPY` the per-segment tuples `(segment_id, i3rab_arabic, i3rab_rule_id, i3rab_status,
  i3rab_review_reason)` (binary) into a **temporary staging table**, then a single
  `UPDATE quran_word_morphology_segments t SET … FROM staging s WHERE t.id = s.segment_id`, inside **one
  transaction**. Seed `quran_i3rab_rules` (142 rows) in the same transaction.
- **Rationale**: Updating 128,219 rows row-by-row is slow and chatty. `COPY`-to-temp + one set-based
  `UPDATE … FROM` is the standard high-throughput pattern and reuses the existing Npgsql binary-`COPY`
  machinery from `EfBulkMorphologyWriter`. Guarantees the writer touches **only** the four columns
  (`I3RAB-SOURCE-COLUMNS-UNCHANGED`) and performs **no** insert/delete on the segments table
  (`I3RAB-SEGMENT-ROWCOUNT-STABLE`).
- **Alternatives rejected**: EF change-tracking `SaveChanges` over 128k entities (slow, high memory).
  Per-row `UPDATE` statements (chatty, slow). `MERGE`/upsert into the segments table (it would risk
  insert/delete semantics; a plain keyed `UPDATE` is the safest expression of "modify existing rows only").

## R5 — Segment signature key (the lookup key)

- **Decision**: Build a deterministic signature string per segment:
  `kind:pos[:ALLAH][:case][:tense:voice:person]` — exactly the **enriched segment-token signature** from
  the coverage report §3.4 (142 distinct values). Components:
  - `kind` ∈ {PREFIX, STEM, SUFFIX}; `pos` = segment POS code.
  - `:ALLAH` appended for a `PN` stem whose lemma is the divine name (see R7).
  - `:case` ∈ {NOM, ACC, GEN} for noun-class POS (`N`, `PN`, `ADJ`) from `case_feature`.
  - `:tense:voice:person` for verbs (`PERF/IMPF/IMPV` + `ACT/PASS` + person such as `3MS`).
  - `:person` for pronouns (`PRON`) such as `3MP`, `1S`.
  - `:1S` special case for `N:GEN:1S` (the إضافة-to-ياء-المتكلم refinement).
- **Rationale**: The 142-row catalogue is keyed by this signature; the per-segment label is a pure lookup
  (Clarification A, FR-012). Components are read from `features_raw` (a `|`-delimited token list, e.g.
  `STEM|POS:V|IMPF|ACT|LEM:…|3MS`) and the segment's `kind`/`pos`/`case_feature` — no new parsing source.
- **Alternatives rejected**: Composing the Arabic label in code (rejected by Clarification A — pushes
  logic into the generator; the cheaper implementer is more likely to err). A 67-family lookup with
  in-code person/case suffixing (same objection).

## R6 — Rule catalogue: in-code seed, 142 rows, importer-seeded (not `HasData`)

- **Decision**: The catalogue is a curated **in-code seed** `I3rabRuleCatalogSeed` (the same pattern as
  `PosTagSeed.cs`): 142 rows, each `{ signature_key, i3rab_arabic, rule_family, default_status, sort_order
  }`, with the **exact Arabic labels** transcribed from the coverage report §3.4. It is seeded by the
  generator **idempotently** (upsert by `signature_key`) inside the same transaction — **not** via EF
  `HasData`.
- **Rationale**: Matches Feature 004's POS-seed approach and the Backend EF policy (no `HasData` data
  seeding in migrations). Keeping the catalogue in code makes it versioned, reviewable, and the single
  owner of the Arabic labels. The coverage report §3.4 table is the verbatim source for the 142 labels.
- **Alternatives rejected**: `HasData` migration seeding (against Backend policy; couples data to schema
  migrations). Reading the now-deleted JSON/CSV companions (they were removed as stale; the markdown
  coverage report is authoritative).

## R7 — لفظ الجلالة (divine name) identification

- **Decision**: A `PN` stem whose `lemma_buckwalter` matches the divine name (Buckwalter `{ll~ah`, lemma
  id 265 in the live data) gets the `:ALLAH` signature component → `لفظ الجلالة` + case, overriding the
  generic `اسم علم` + case.
- **Rationale**: Confirmed in the inventory (2,698 words, NOM/ACC/GEN). A lemma check is deterministic and
  cheap. The vocative `ٱللَّهُمَّ` is a separate form (its closing ميم = `ميم عوض عن حرف النداء`, a
  distinct `SUFFIX:VOC` signature) — handled by its own catalogue row, not the لفظ الجلالة rule.
- **Alternatives rejected**: Matching on Arabic surface text (fragile with diacritics). Hardcoding the
  lemma id 265 (the lemma **id** is environment-specific; match on `lemma_buckwalter` instead).

## R8 — Status column on an already-populated table (migration backfill)

- **Decision**: `i3rab_status` is `text NOT NULL` with a `CHECK (i3rab_status IN ('approved',
  'needs_review','unsupported'))`. Because the 128,219 segment rows already exist, the migration adds the
  column with a **transient server default of `'unsupported'`** to satisfy the NOT NULL backfill; the
  first `generate-i3rab` run overwrites **all** rows to `'approved'`. The default exists only to make the
  schema change valid on the populated table; after a successful generation **zero** rows remain at the
  default.
- **Rationale**: A NOT NULL column on a populated table needs a backfill value. `'unsupported'` is the
  least-wrong placeholder for "label not yet generated" and is one of the three allowed values. The
  generator then sets every row, and `I3RAB-SEG-STATUS-COMPLETE` + `I3RAB-UNSUPPORTED-CONSISTENT` would
  fail the gate if any pre-generation placeholder survived without a reason — so a committed run guarantees
  100% `approved`.
- **Alternatives rejected**: A native PostgreSQL `enum` type (harder to evolve — adding a value is a
  migration; `text + CHECK` is the Clarification-locked choice and matches how morphology stores its other
  enum-like values as text). Leaving the column nullable (contradicts the locked NOT NULL decision).

## R9 — Idempotency, refusal, and `--force`

- **Decision**: The generator detects whether i‘rab is already populated (any segment with a non-default
  `i3rab_status` / non-null `i3rab_rule_id`). Without `--force` it **refuses** a non-empty target; with
  `--force` it cleanly recomputes and overwrites all rows to an identical result. It also detects
  **missing/stale morphology** (segment count ≠ expected / empty) and refuses, writing nothing. Mirrors
  the Feature 004 `MorphologyRefusalForceTests` behavior.
- **Rationale**: Safe re-runs in CI; deterministic output (FR-026, FR-027). A morphology re-import that
  truncates segments clears the i‘rab columns, so i‘rab is regenerated afterward (FR-028).
- **Alternatives rejected**: Always overwriting silently (unsafe). Incremental/partial updates (the run is
  whole-corpus and cheap; all-or-nothing is simpler and matches the atomic gate).

## R10 — Validation gate (assemble → validate → commit-or-rollback)

- **Decision**: Reuse Feature 004's discipline: assemble in memory, open one transaction, stage + apply,
  run the **9 hard checks** (FR-029) and **5 warnings** (FR-030), and `COMMIT` only if all hard checks
  pass; otherwise `ROLLBACK`, write a failure report, exit non-zero. DB-level constraints (NOT NULL +
  CHECK on status, FK on `i3rab_rule_id`) are the second line of defense (Clarification Q2).
- **Rationale**: Identical posture to `MorphologyValidationRunner`; gives the same auditable
  commit-or-rollback semantics and the same report artifact shape.
- **Alternatives rejected**: Commit-then-validate (could leave bad data). Constraints-only with no
  application gate (a single-column CHECK can't express the conditional/cross-row invariants).

## R11 — Quranic data safety

- **Decision**: Store derived grammatical labels keyed by identifier only (segment id / `quran_word_id` /
  location). Store **no** ayah text. Never modify the original morphology columns, `quran_words`, the
  Uthmani/QPC text, or the `quran_pos_tags` seed. Keep the **208** NULL `form_arabic_normalized` rows NULL
  (label only). Record anything unsupported with a reason; never guess. Labels are explicitly **not**
  authoritative scholarly i‘rab. Test fixtures are source-safe (individual derived labels, never assembled
  ayah text).
- **Rationale**: Matches the workspace product rules and the planning report's safety section.
- **Alternatives rejected**: None — this is a hard product constraint.

---

### Summary of resolved deferrals

| Spec deferral | Resolution |
|---|---|
| Generator invocation / verb name | `generate-i3rab` verb on `DataImporter` (R2) |
| Report artifact path/format | Markdown+JSON in `resources/report/words-simple-i3rab/` (R3) |
| Concurrency | Single exclusive operator/CI run; refuse non-empty without `--force` (R9) |
