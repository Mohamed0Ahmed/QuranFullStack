# Phase 0 Research — Quran Word Morphology Foundation

All decisions below are settled; there are **no open NEEDS CLARIFICATION** items (the spec, its
2026-06-10 Clarifications, and the three planning docs are exhaustive). Each entry: **Decision →
Rationale → Alternatives rejected.** Facts about the existing system were read from source (the importer
`Program.cs` verb dispatch, the Feature 002 import abstractions in `Quran/Import/`, the `COPY`-based
`EfBulkQuranImportWriter`, and the JSON source readers / `ManifestReader`).

---

## R1. Computation location: in-memory assembly + Npgsql binary `COPY` (source-driven, not DB-to-DB)

**Decision.** Read the local JSON source files, **assemble** the morphology graph (per-word records,
segments, deduplicated dimensions, transliterated segment Arabic) in application memory, then **bulk-load
with Npgsql binary `COPY`** and validate inside one transaction. This mirrors the Feature 002 importer
(`EfBulkQuranImportWriter`), **not** the Feature 003 `INSERT … SELECT` pattern.

**Rationale.** Unlike Feature 003 (whose inputs already lived in PostgreSQL), Feature 004's inputs are
**files** (the aligned corpus + three QUL files). The work — parsing, Buckwalter→Arabic transliteration,
POS/feature mapping, dimension de-duplication — is inherently application-side and cannot be expressed as
set-based SQL over existing tables. `COPY` is the established, fastest bulk path in this codebase and is
already proven by Feature 002. Only `quran_words.{id, location, is_ayah_marker}` is read from the DB (to
key and gate the load).

**Alternatives rejected.** *`INSERT … SELECT` (Feature 003 style)* — impossible; the source is not in the
DB. *EF `AddRange` change-tracking* — slow and memory-heavy for ~205k rows; wrong tool for a bulk import.

---

## R2. Source readers + manifest verification (read-only, local-only)

**Decision.** One reader per file (`JsonAlignedCorpusReader`, `JsonQulRootReader`,
`JsonQulLemmaReader`, `JsonQulStemReader`) plus `MorphologyManifestReader`, orchestrated by
`MorphologyImportSource` (the `IMorphologyImportSource`). The default `--source` is the local in-repo
path `App/resources/import-sources/quran-morphology/`; the importer reads **only** that tree and never
the external Desktop workspace. The manifest is verified (file presence, `expectedRecordCount`,
`fileSizeBytes`, `sha256`) **before** the run; `MORPH-SOURCE-UNCHANGED` re-verifies size/sha256 **after**
the run to prove the files were never written.

**Rationale.** Mirrors the Feature 002 `ManifestReader`/`QuranImportSource` split (FR-001–FR-004,
FR-036). Per-file readers keep each parser cohesive and unit-testable. Local-only, manifest-gated reads
make the load reproducible and tamper-evident, and keep runtime independent of any external path.

**Alternatives rejected.** *One monolithic reader* (less cohesive, harder to test). *Reading the external
Desktop research workspace at runtime* (explicitly forbidden — read-only provenance only). *Trusting files
without a manifest* (no tamper/version evidence).

---

## R3. Buckwalter→Arabic transliteration map as a single, fail-closed source of truth

**Decision.** Encode the full QAC *extended* Buckwalter→Unicode table once in `BuckwalterArabicMap`
(61 characters, all mapped per the capability report). `SegmentArabicRenderer` transliterates each
non-empty segment `form` deterministically. The `MORPH-SEG-CHARSET` hard check asserts **0 unmapped**
characters; a previously-unseen character **refuses the import** rather than emitting `�`.

**Rationale.** The capability report proved 100 % character coverage and deterministic mapping. Centralng
the table makes it the single re-validatable asset; fail-closed behavior protects Quranic data safety on
any future source refresh.

**Alternatives rejected.** *Scattered inline maps* (drift risk). *Best-effort with replacement glyphs*
(would silently corrupt). *A third-party transliterator* (opaque; not aligned to the QAC scheme).

---

## R4. Segment Arabic rendering — Option B (flagged derived reading aid)

**Decision.** Store `form_arabic_normalized` best-effort for **every non-empty** segment, with
`arabic_render_tier` ∈ {`clean`, `quranic_marks`, `review`, `multiword`} and a constant
`arabic_render_source = 'buckwalter-transliteration'`. Empty forms (the 208 `(SUFFIX, PRON)` cases) →
`NULL`. The raw `form_buckwalter` is always retained. The value is **never** written from
`qpc_glyph`/`text_uthmani`, **never** named `qpc_segment_text`, and **never** used as Mushaf text.

**Rationale.** The capability report's Option B recommendation (spec FR-019–FR-021, clarification context):
deterministic and ~94 % high-fidelity, but a morphological reading (≈79.83 % whole-word agreement with
Uthmani), so it must be flagged and never mistaken for the Mushaf. Tiers route the ~0.4 % fragile and
1 multiword rows to curators.

**Alternatives rejected.** *Option A* (single unflagged column — erases fidelity tiers, invites misuse).
*Option C* (defer rendering — discards a deterministic, reviewed, useful aid for no safety gain).

---

## R5. Dimensions deduplicated on Arabic text; Buckwalter-only ⇒ null link (clarification Q1)

**Decision.** `quran_roots`/`quran_lemmas`/`quran_stems` are keyed by their **Arabic display text**
(`UNIQUE(root_text/lemma_text/stem_text)`), with the Buckwalter value kept as a nullable cross-reference.
A dimension row exists **only** when an Arabic value is present. When the corpus supplies a Buckwalter
root/lemma but QUL has **no** Arabic value (e.g. the ~1,704 corpus-only lemmas), the word's
`root_id`/`lemma_id` is **NULL** and the Buckwalter value is retained only at segment level
(`root_buckwalter`/`lemma_buckwalter`). No transliterated/placeholder dimension row is created. Each
dimension carries `words_count` and a `UNIQUE first_word_order_in_mushaf` for stable display ordering.

**Rationale.** Clarification Q1 (2026-06-10) + FR-018/FR-022: Arabic display values come from QUL only;
"no invented values"; a UNIQUE-on-Arabic key cannot admit a null/placeholder Arabic. The Buckwalter
cross-reference preserves losslessness for a future resolution feature.

**Alternatives rejected.** *Transliterated Arabic fallback* (introduces derived Arabic into a display
dimension — rejected by Q1). *Null/placeholder Arabic dimension rows keyed by Buckwalter* (breaks the
UNIQUE-on-Arabic key; mixes identity bases).

---

## R6. POS controlled vocabulary from a curated in-code dictionary; fail-closed resolution

**Decision.** `quran_pos_tags` (≈ 30 rows: `code` PK, `arabic_label`, `english_label`, `category` ∈
{noun, verb, particle, other}, `sort_order`, `description`) is **seeded by the importer** from a curated
in-code dictionary (idempotent upsert), **not** by the migration. `head_pos` and every segment `pos`
reference `quran_pos_tags.code`; `MORPH-POS-RESOLVES` asserts 0 unknown codes (fail-closed: a new POS code
refuses the import until the dictionary is extended).

**Rationale.** Planning report §3.7 + FR-008/FR-023–FR-025. The corpus supplies POS *codes*; the
human-readable Arabic/English labels, category, and order are **curated** (not in the corpus, not
inventable per run), so they live in code and are seeded as data — queryable/joinable without schema
churn, and kept out of migrations per `Backend/CLAUDE.md`.

**Alternatives rejected.** *`HasData` seed in the migration* (forbidden — migrations are schema-only).
*A large hard-coded C# enum for POS* (not joinable/extensible; the spec requires a table). *Auto-adding
unknown codes at runtime* (would admit unlabelled tags — fail-closed is safer).

---

## R7. Verb features: tense/voice/case mapping; active-by-default, no inferred flag (clarification Q2)

**Decision.** `verb_tense` ← {PERF→past, IMPF→present, IMPV→imperative}; `verb_voice` = `passive` iff the
corpus marks PASS, otherwise `active` **by documented convention** (no separate inferred flag);
`case_feature` ← {NOM→nominative, ACC→accusative, GEN→genitive}, else `NULL`. `is_verb = (head_pos = 'V')`.
Non-verbs carry null verb fields. `MORPH-VERB-FEATURE-CONSISTENCY` is scoped to the head STEM: if
`head_pos = 'V'`, the first STEM must have exactly one tense and the stored word-level tense/voice must
match that head STEM; if `head_pos <> 'V'`, word-level verb fields stay null even when additional
non-head STEM segments exist. The verbatim `features_raw` string is retained per segment so PASS
presence/absence can be recomputed later.

**Rationale.** Clarification Q2 (2026-06-10) + planning report §3.2 + FR-016/FR-017: KISS — store
`active`/`passive` only; the raw FEATURES string preserves the explicit/inferred distinction for anyone
who needs it. Case is null where unmarked (no invention).

**Alternatives rejected.** *A `voice_inferred` flag/`voice_source` column* (extra column the plan
deliberately omitted; recomputable from `features_raw`). *Null voice when unmarked* (breaks "every verb
has a voice"; complicates filters).

---

## R8. Grain: one row per readable word, keyed to `quran_word_id`; head POS from the first STEM segment

**Decision.** `quran_word_morphology` PK = `quran_word_id` (FK→`quran_words.id`, 1:1 with readable
words). The aligned corpus is joined to `quran_words` by `location` (= `qpcLocation`), filtered to
`is_ayah_marker = false`. `head_pos` is the POS of the first `kind = 'STEM'` segment by
`segment_number`; this is an operational morphology summary, not a full syntactic/iʿrab head claim.
`segment_count` matches the segment rows, and all additional STEM segments are preserved. Morphology is
**per occurrence**, never keyed to Feature 003 identity links.

**Rationale.** Planning report §3.1–§3.2 + FR-009–FR-015 plus the real-source multi-STEM investigation:
case and features vary by context, so the grain must be per occurrence. Markers receive no morphology
(FR-010). The real corpus contains 483 fused words with two STEM segments and no words with zero or more
than two STEM segments; preserving all segments while choosing the first STEM for `head_pos` keeps the
word-level summary deterministic without discarding source analysis.

**Alternatives rejected.** *Per-identity grain* (loses context-specific case/features). *Including markers*
(forbidden). *Deriving head POS from a non-STEM segment* (incorrect).

---

## R9. Trigger: a new `import-morphology` verb on the existing console host

**Decision.** Add a third verb to `tools/QuranDashboard.DataImporter/Program.cs`
(`import-foundation | rebuild-words | import-morphology`). `import-morphology` accepts
`--source <path>` (default = the local `quran-morphology/` path), `[--report-out <path>]`, and
`[--force]`. It depends on `import-foundation` having run and is independent of `rebuild-words`.

**Rationale.** FR-001/FR-006/FR-037: an operator/CI batch op must stay **off any HTTP surface**; the host
already wires DI + a report writer; a third verb avoids an 8th project. A distinct verb keeps each verb
single-purpose (`import-foundation` builds the core from files; `rebuild-words` derives display from the
DB; `import-morphology` is source-driven **and** joins `quran_words`).

**Alternatives rejected.** *Overloading `import-foundation`* (mixes two unrelated source schemas).
*A new console project* (host/DI sprawl). *A guarded API endpoint* (violates "no API / no request-path").

---

## R10. Transactional load with a hard-gated validation suite before commit

**Decision.** Within one transaction: (if `--force`) `TRUNCATE` the six morphology tables
`RESTART IDENTITY CASCADE`; seed `quran_pos_tags`; `COPY` dimensions, then morphology, then segments; run
the validation queries; **commit only if every hard check passes, else roll back**. `quran_words` is
never in the truncate/write set. Orchestration mirrors `ImportQuranFoundationHandler` (refuse-unless-empty
→ act → write report → map exit code).

**Rationale.** FR-027–FR-036: validating inside the same transaction as the writes is the only way to
guarantee "nothing is written on failure" atomically. FK order (dimensions/pos before morphology before
segments) satisfies referential integrity during `COPY`.

**Alternatives rejected.** *Validate-after-commit-then-delete* (non-atomic; leaves a bad-data window).
*`DELETE` instead of `TRUNCATE`* (slower; keeps identity high-water mark).

---

## R11. Validation home: DB-bound queries in Infrastructure; constants/records in Abstractions

**Decision.** Structural/relational checks (counts, marker exclusion, location match, segment presence,
POS resolution, verb-feature consistency, dimension resolution, render totals/tiers, not-Uthmani guard,
source-unchanged) run as queries in `EfBulkMorphologyWriter` (SQL text in `MorphologySql`). Charset
coverage is computed during assembly (it is file-derived). Known constants
(`ExpectedReadableWords = 77_432`, `ExpectedEmptyForms = 208`, tier baselines) and the
check-result/result records live in `Application.Abstractions/Quran/Words/Morphology/`. Correctness is
proven with **real-Postgres integration tests** (Testcontainers) plus pure unit tests for the
transliteration map.

**Rationale.** Invariants are query-bound, so they execute where DB access lives (Infra references
Abstractions + Domain, not Application). Keeping records/constants in Abstractions keeps the contract
strongly typed and avoids leaking EF entities. Matches the Feature 003 R10 posture and test-guard
guidance (real infrastructure for query correctness).

**Alternatives rejected.** *Pure in-memory validator* (cannot express the relational invariants).
*Holding an open transaction across the Application boundary* (leaky).

---

## R12. Report: feature-local records + a Markdown+JSON writer

**Decision.** Feature-local records (`MorphologyImportResult`, `MorphologyImportTotals`,
`MorphologyCheckResult`) and `IMorphologyReportWriter`, implemented by
`MarkdownJsonMorphologyReportWriter`, mirroring the Feature 002/003 report pattern. The report records
per-table totals, the tier distribution, the review/`multiword`/empty lists, the hard-check results, the
warnings (incl. ≈79.83 % whole-word agreement), and the outcome. Default output:
`resources/report/words-morphology/`.

**Rationale.** FR-029/FR-030. The Feature 002 `QuranImportValidationResult` carries import-specific
fields; a small cohesive feature-local set keeps domain grouping clean and parallels the familiar
`ImportCheckResult` shape (`Id, Severity, Expected, Observed, Passed`).

**Alternatives rejected.** *Reusing `QuranImportValidationResult`* (cross-feature coupling, unused
fields). *A shared generic result type now* (premature).

---

## R13. Column types and keys

**Decision.**
- `quran_word_id` (PK/FK), segment `id` (identity PK), dimension `id`s (identity PK),
  `first_word_order_in_mushaf`, `words_count`: **`int`** (ids ≤ 83,668 and order ≤ 77,432 exceed
  `smallint`).
- `segment_number`, `segment_count`, `distinct_lemmas_count`, `sort_order`: **`smallint`** (≤ 32,767).
- `quran_pos_tags.code`: **`text` PK** (open vocabulary, ~30 rows).
- Text columns (`location`, `head_pos`, `pos`, `kind`, `verb_*`, `case_feature`, `form_buckwalter`,
  `form_arabic_normalized`, `arabic_render_*`, `*_text`, `*_buckwalter`, `features_raw`): **`text`**;
  `head_features_json`/`features_json`: **`jsonb`**.
- FKs: morphology→`quran_words.id`, →`quran_roots/lemmas/stems.id` (nullable), `head_pos`→`pos_tags.code`;
  segment→`quran_words.id`; lemma→root (nullable).

**Rationale.** Matches the Feature 002/003 typing convention (`smallint` where it fits, `int` otherwise).
Real FKs to the stable, never-truncated `quran_words` add integrity at no cost. `text` PK for POS codes
matches the open controlled vocabulary.

**Alternatives rejected.** *`smallint` ids* (overflow). *Enum-typed POS column* (not joinable; spec wants
a table). *Promoting every feature token to a column* (schema churn — keep the bag in `jsonb`).

---

## R14. Relational vs JSON feature storage

**Decision.** Head POS, verb tense/voice, case, dimension FKs, segment `kind`/`pos`/`segment_number`,
`arabic_render_tier` are **relational** (they drive filters/joins). The full feature token bag is kept
**verbatim** in `features_raw` (`text`, lossless) plus a parsed `features_json` (`jsonb`) for query
convenience; the head segment's bag is `head_features_json` (`jsonb`).

**Rationale.** Planning report §3.2/§3.3 "relational vs JSON" note + FR-012/FR-014: filterable
dimensions are columns; the open-ended feature set stays JSON to avoid schema churn while `features_raw`
guarantees losslessness and re-parseability (and supports R7's voice recomputation).

**Alternatives rejected.** *All-relational features* (unbounded schema churn). *JSON-only (no
`features_raw`)* (lossy if parsing changes).

---

## Resolved unknowns summary

| Topic | Resolution |
|---|---|
| How to compute | Read JSON → assemble in memory → Npgsql binary `COPY`, one transaction (R1) |
| Source reads | Per-file readers + manifest verify; local-only `quran-morphology/`; never external path (R2) |
| Transliteration | Central `BuckwalterArabicMap`; deterministic; `MORPH-SEG-CHARSET` fail-closed (R3) |
| Segment Arabic | Option B: `form_arabic_normalized` + tier + source; empties→NULL; render provenance guard (R4) |
| Dimensions | Dedup on Arabic text; Buckwalter-only ⇒ null link; Buckwalter kept as cross-ref (R5, Q1) |
| POS vocabulary | Curated in-code dictionary, importer-seeded; fail-closed resolution (R6) |
| Verb features | tense/voice/case mapping; active-by-default, no inferred flag (R7, Q2) |
| Grain | One row per readable word, keyed `quran_word_id`; head POS = first STEM by `segment_number` (R8) |
| Trigger | New `import-morphology` verb on the existing console host (R9) |
| Atomicity & gate | One transaction; validate before commit; rollback on failure (R10) |
| Validation home | Queries in Infra; constants/records in Abstractions; charset at assembly (R11) |
| Report | Feature-local records + Markdown+JSON writer; `resources/report/words-morphology/` (R12) |
| Types & keys | `int`/`smallint` per range; `text` POS-code PK; FKs to `quran_words` + dims (R13) |
| Feature storage | Relational filterables + `features_raw` (text) + `features_json`/`head_features_json` (jsonb) (R14) |
