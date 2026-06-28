# POS / Word-Type Arabic Labels Review Report

**Feature:** 017 — Lexical Explorers Polish
**Task type:** REPORT ONLY (no code, seed, DB, migration, or frontend changes)
**Branch inspected:** `017-lexical-explorers-polish`
**Date:** 2026-06-27

---

## 0. Executive Summary

- The canonical catalogue of POS / word-type labels is **`PosTagSeed.cs`** (49 codes). It is the single source of truth.
- The catalogue is pushed into the database table **`quran_pos_tags`** by a **binary `COPY`** in `MorphologyBulkCopier.CopyPosTagsAsync`, invoked during the **morphology import** (`EfBulkMorphologyWriter`). It is **not** seeded by EF `HasData` / migration `InsertData`.
- **One label is definitively wrong: `PRO`.** It is seeded as `ArabicLabel = "ضمير منفصل"` / `EnglishLabel = "Independent Pronoun"` / `Category = "noun"`. In the Quranic Arabic Corpus (QAC), `PRO` is the **prohibition particle** (لا الناهية). Every real occurrence in the database (e.g. `لَا` in `لَا تُفْسِدُوا` at `2:11:4`, `فَلَا` `2:22:18`, `وَلَا` `2:35:12`) is the prohibitive lā. Recommended label: **`حرف نهي`**, category **`particle`**.
- The `PRO` error has **two display blast radii**: (1) the Mushaf Reader word-type label `headPosLabel.ar` / `segmentPosLabel.ar` shows `ضمير منفصل`; (2) the Unique Words list collapses `PRO` (category `noun`) to the broad label `اسم`, which is also wrong (should be `حرف`).
- The same mislabel is **propagated a second time** into the i‘rab rule catalogue: `I3rabRuleCatalogSeedData.cs` line 66, `STEM:PRO → "ضمير منفصل"`. This is a separate seed source and is flagged here, but per the task's i‘rab rule, it is recorded as evidence and left for human approval — not recommended as an in-scope POS change.
- The **frontend hard-codes no Arabic POS labels.** It renders whatever the API returns. Therefore fixing the seed + reseeding fixes the UI with no frontend change.
- All other inspected codes are **CORRECT** or **ACCEPTABLE**, with a small set flagged **NEEDS_REVIEW** (`SUB`, `EXL`, `TIM` duplicate) or **CONTEXT_DEPENDENT** (`PRON`, `INTG`).

**Confirmed fact vs. recommendation:** Sections 1–6 are confirmed facts from repository source and prior in-repo DB inventory. Section 7+ separates the confirmed-wrong list from human-review recommendations.

---

## 1. Inventory Of Label Sources

| # | Source | Role | Defines Arabic POS labels? |
| ---: | --- | --- | --- |
| 1 | `Backend/infrastructure/.../Files/Quran/DataPipelines/Words/MorphologyImporting/PosTagSeed.cs` | **Source of truth** — 49 `PosTag` rows (Code, ArabicLabel, EnglishLabel, Category, SortOrder, optional Description). | **Yes (canonical)** |
| 2 | `Backend/domain/.../Quran/Words/Morphology/PosTag.cs` | Entity shape for a POS tag. | No (schema only) |
| 3 | `Backend/infrastructure/.../Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyBulkCopier.cs` (`CopyPosTagsAsync`) | Binary `COPY` of `PosTagSeed.GetAll()` into `quran_pos_tags`. | No (transport) |
| 4 | `Backend/infrastructure/.../MorphologyImporting/EfBulkMorphologyWriter.cs` | Orchestrates the morphology import; calls `CopyPosTagsAsync`; truncates morphology tables when `force`. | No (transport) |
| 5 | `Backend/infrastructure/.../MorphologyImporting/MorphologySql.cs` (`TruncateMorphologyTables`) | `TRUNCATE … quran_pos_tags … RESTART IDENTITY CASCADE`. | No |
| 6 | `Backend/infrastructure/.../Files/.../MorphologyImporting/MorphologyAssembler.cs` | Computes per-word `headPos` (first `STEM` segment POS, else first segment POS) and validates segment POS against `PosTagSeed` codes. | No (assignment, not labels) |
| 7 | `quran_pos_tags` (DB table) | Runtime copy of the seed; what readers join to. | Mirror of seed |
| 8 | `Backend/infrastructure/.../Persistence/Reads/Quran/MushafReader/EfWordAnalysisReader.cs` | Reads `arabic_label` / `english_label` from `quran_pos_tags` for head + each segment; falls back to the raw code when the tag is missing. | No (exposes seed labels) |
| 9 | `Backend/infrastructure/.../Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs` (`ResolvePrimaryWordTypeBroadLabel`) | Collapses winner `head_pos` to a **broad** label `اسم/فعل/حرف` by `category`, special-casing `INL → حروف مقطّعة`. | **Derives broad label** (not the detailed POS label) |
| 10 | `Backend/application/.../Quran/MushafReader/Responses/WordAnalysisResponse.cs` | DTO: `WordMorphologyDto.HeadPosLabel` (`LocalizedLabel Ar/En`) and `RenderedSegmentDto.SegmentPosLabel`. | No (carries labels) |
| 11 | `Backend/application/.../Quran/Words/Responses/UniqueWordListItemDto.cs` | DTO: `PrimaryWordTypeCode`, `PrimaryWordTypeBroadArabicLabel`. | No (carries broad label) |
| 12 | `Backend/infrastructure/.../Files/.../SimpleI3rabGeneration/I3rabRuleCatalogSeedData.cs` | **Separate** seed of simplified i‘rab display labels keyed by segment signature (e.g. `STEM:PRO → "ضمير منفصل"`). | Yes, but **i‘rab labels** (not POS catalogue) |
| 13 | `Frontend/.../features/mushaf/models/mushaf.models.ts` | TS interface: `headPosLabel`, `segmentPosLabel: LocalizedLabel`. | No |
| 14 | `Frontend/.../features/mushaf/components/word-morphology-summary/word-morphology-summary.component.{ts,html}` | Renders `morphology().headPosLabel.ar` under the Arabic label `نوع الكلمة`. | **No hardcoded labels** — renders API value |
| 15 | `Frontend/.../features/mushaf/components/segment-data-rows/segment-data-rows.component.html` | Renders `segment.segmentPosLabel.ar` per segment. | **No hardcoded labels** |
| 16 | `Frontend/.../features/mushaf/utils/morphology-display.labels.ts` | Only a `morphologyTextOrDash()` helper (empty → `—`). Contains **no** Arabic POS strings. | No |
| 17 | `Backend/tests/.../Quran/Words/UniqueWordsListReadTests.cs` | Asserts broad labels (`PN→اسم`, `V→فعل`, `P→حرف`, `INL→حروف مقطّعة`). | Test fixture (broad only) |
| 18 | `report/feature-017-lexical-explorers-polish/word-types-taxonomy-inventory-report.md` | Prior in-repo inventory with **read-only DB counts** and examples. Reused as DB evidence here. | Reference |

**Key structural finding:** the frontend has **no** hardcoded Arabic POS labels. Every word-type label the user sees originates in `quran_pos_tags`, which originates in `PosTagSeed.cs`. The Unique Words page is the only place a *derived broad* label is computed, and that derivation reads the seed `category`.

---

## 2. Seeding Flow (Exact Path Into The Database)

### 2.1 Source of truth
`PosTagSeed.GetAll()` in `PosTagSeed.cs` returns 49 `PosTag` records. This is the **only** authoritative definition of POS code → Arabic label / English label / category / sort order / description.

### 2.2 Seeding mechanism
- **Not** EF `HasData`. **Not** migration `InsertData`. The migration `20260610155434_AddQuranWordMorphology` only **creates** `quran_pos_tags` (PK on `code`, indexes on `category` and `sort_order`, FKs from `quran_word_morphology.head_pos` and `quran_word_morphology_segments.pos`). It inserts no rows.
- **Actual path:** `EfBulkMorphologyWriter` → `MorphologyBulkCopier.CopyPosTagsAsync(connection)` → Npgsql `BeginBinaryImportAsync` → `COPY quran_pos_tags (code, arabic_label, english_label, category, sort_order, description) FROM STDIN (FORMAT BINARY)` → one row per `PosTagSeed.GetAll()` entry.
- **Reseed semantics:** when the morphology import runs with `force = true`, `EfBulkMorphologyWriter` first executes `MorphologySql.TruncateMorphologyTables` = `TRUNCATE quran_word_morphology_segments, quran_word_morphology, quran_lemmas, quran_roots, quran_stems, quran_pos_tags RESTART IDENTITY CASCADE`, then re-copies **all six** tables (pos tags first). Without `force`, no truncate occurs and a second `COPY` would collide on the `code` primary key — so a label change requires a `force` reseed.

### 2.3 Is `quran_i3rab_rules` a separate label source?
**Yes.** `I3rabRuleCatalogSeedData.cs` seeds `quran_i3rab_rules` with *simplified i‘rab display labels* keyed by **segment signature** (e.g. `STEM:PRO`, `SUFFIX:PRON:3FP`, `PREFIX:INTG`). These are richer/contextual grammar labels (they encode case, voice, person), are surfaced as `segmentI3rabArabic` in `WordAnalysisResponse`, and are **distinct** from the `quran_pos_tags` POS catalogue labels. They are related but not the same data set (see Section 5).

### 2.4 Does the frontend hardcode POS Arabic labels?
**No.** The Mushaf Reader components render the API-supplied `headPosLabel.ar` and `segmentPosLabel.ar`. `morphology-display.labels.ts` holds only a dash fallback helper. The Unique Words table renders `primaryWordTypeBroadArabicLabel` straight from the API.

### 2.5 Is changing `PosTagSeed.cs` enough for future imports?
**Yes for future imports.** Because `CopyPosTagsAsync` reads `PosTagSeed.GetAll()` at import time, any fresh/`force` morphology import after editing the seed writes the corrected labels. **But the current database is not retroactively updated** — existing rows keep the old labels until a reseed.

### 2.6 Operational step required after changing seed labels
| Scenario | Required action |
| --- | --- |
| Fix label text and/or category in `PosTagSeed.cs` | Edit seed, then **rerun morphology import with `force = true`** (truncate + reseed of all six morphology tables). A pure label patch is otherwise possible via a one-off SQL `UPDATE`/data-patch migration, but the project's established path is the `force` reseed. |
| Want corrected label visible without full morphology rebuild | Add a **data patch** (SQL `UPDATE quran_pos_tags SET arabic_label = …, category = … WHERE code = 'PRO'`). This is the lighter operational option; see Section 9E. |
| Frontend | **No change** — labels flow from API/DB. |
| Migration | Not needed for the *label text*; the table schema is unchanged. A data-patch migration is one delivery option, not a schema requirement. |
| Caching | Reader results may be cached at runtime; a cache flush/restart may be needed for users to see corrected labels immediately (operational, not code). |

### 2.7 Do existing local DB labels match the current seed source?
**Yes.** The prior in-repo inventory (`word-types-taxonomy-inventory-report.md`, read-only query of `quran_dashboard`) lists 49 rows whose labels match `PosTagSeed.cs` exactly — including the wrong `PRO = "ضمير منفصل"`. So the database faithfully mirrors the seed, error and all.

> **DB verification note for this session:** the `quran_dashboard` database exists locally, but the OS-level psql role available in this session lacks `SELECT` on `quran_pos_tags` (`ERROR: permission denied for table quran_pos_tags`), and probing connection credentials was out of scope. DB counts/examples below are therefore taken from the **prior in-repo read-only inventory**, which is itself repository evidence, and cross-checked against the seed. They were **not** re-queried live in this session.

---

## 3. Complete POS Label Table

Columns:
- **Head ct** = `quran_word_morphology.head_pos` count (from prior DB inventory).
- **Seg ct** = `quran_word_morphology_segments.pos` count.
- **Head?** = used as a word-level head POS.
- **Examples** = real Quran locations from prior DB inventory.
- **Verdict** = CORRECT / WRONG / NEEDS_REVIEW / CONTEXT_DEPENDENT (for the **POS-catalogue Arabic label**).

| Code | English label | Arabic label (current) | In `quran_pos_tags`? | In segments? | Seg ct | Head? (ct) | Example locations | QAC technical meaning | Project usage | Proposed Arabic label | Verdict | Notes / UI risk |
| --- | --- | --- | :-: | :-: | ---: | :-: | --- | --- | --- | --- | --- | --- |
| `N` | Noun | اسم | yes | yes | 25,136 | yes (25,135) | بِسْمِ 1:1:1; ٱلْحَمْدُ 1:2:1 | Noun | head + segment + broad `اسم` | اسم | CORRECT | — |
| `V` | Verb | فعل | yes | yes | 19,356 | yes (19,356) | نَعْبُدُ 1:5:2; ٱهْدِنَا 1:6:1 | Verb | head + segment + broad `فعل` | فعل | CORRECT | — |
| `PN` | Proper Noun | اسم علم | yes | yes | 3,911 | yes (3,911) | ٱللَّهِ 1:1:2; ٱللَّهُ 2:7:2 | Proper noun | head + segment + broad `اسم` | اسم علم | CORRECT | — |
| `ADJ` | Adjective | صفة | yes | yes | 1,961 | yes (1,961) | ٱلرَّحْمَـٰنِ 1:1:3; ٱلرَّحِيمِ 1:1:4 | Adjective | head + segment | صفة | CORRECT | — |
| `PRON` | Pronoun | ضمير | yes | yes | 24,685 | yes (3,301) | إِيَّاكَ 1:5:1; هُمْ 2:4:11 | Personal pronoun | head + segment | ضمير | CORRECT | Keep POS-level as `ضمير` only; منفصل/متصل needs segment kind (Section 4). |
| `P` | Preposition | حرف جر | yes | yes | 13,006 | yes (7,679) | عَلَيْهِمْ 1:7:4; فِيهِ 2:2:5 | Preposition | head + segment + broad `حرف` | حرف جر | CORRECT | — |
| `CONJ` | Conjunction | حرف عطف | yes | yes | 9,450 | yes (756) | أَمْ 2:6:7; ثُمَّ 2:28:7 | Coordinating conjunction | head + segment | حرف عطف | CORRECT | — |
| `NEG` | Negation | حرف نفي | yes | yes | 2,688 | yes (2,643) | لَا 2:2:3; لَمْ 2:6:8 | Negative particle | head + segment | حرف نفي | CORRECT | Distinct from `PRO` (prohibition). |
| `REL` | Relative Pronoun | اسم موصول | yes | yes | 3,575 | yes (3,323) | ٱلَّذِينَ 1:7:2; 2:3:1 | Relative pronoun | head + segment | اسم موصول | CORRECT | — |
| `DEM` | Demonstrative | اسم إشارة | yes | yes | 1,059 | yes (1,059) | ذَٰلِكَ 2:2:1; أُو۟لَـٰٓئِكَ 2:5:1 | Demonstrative pronoun | head + segment | اسم إشارة | CORRECT | — |
| `VOC` | Vocative | حرف نداء | yes | yes | 376 | no (0) | يَـٰٓ… (prefix) | Vocative particle | segment prefix | حرف نداء | CORRECT | Segment-only. |
| `INL` | Quranic Initials | حروف مقطّعة | yes | yes | 30 | yes (30) | الٓمٓ 2:1:1; الٓمٓصٓ 7:1:1 | Quranic initials / disjoint letters | head + segment; broad-cased to `حروف مقطّعة` | حروف مقطّعة | CORRECT | Label + special broad-case both correct (Section 4). |
| `IMPV` | Imperative Lām | لام الأمر | yes | yes | 78 | no (0) | لِـ… (prefix on jussive) | Imperative (lām of command) prefix | segment prefix | لام الأمر | CORRECT | The imperative verb itself is `V`; `IMPV` is the prefix lām (Section 4). |
| `PERF` | Perfect | فعل ماض | yes | no | 0 | no (0) | — | Perfect-tense feature | catalogue only (tense via `V`+features) | فعل ماض | CORRECT | Never surfaces as head/segment POS in current data. |
| `IMPF` | Imperfect | فعل مضارع | yes | no | 0 | no (0) | — | Imperfect-tense feature | catalogue only | فعل مضارع | CORRECT | Same as `PERF`. |
| `ACC` | Accusative Particle | حرف نصب | yes | yes | 2,283 | yes (2,283) | إِنَّ 2:6:1; إِنَّمَا 2:11:9 | Accusative particle | head + segment | حرف نصب | ACCEPTABLE | `حرف نصب` is safely broad; the narrower i‘rab phrasing "من أخوات إنّ" is misleading for non-inna cases (Section 4). |
| `EMPH` | Emphatic | حرف تأكيد | yes | yes | 1,244 | no (0) | لَـ…/نّ (prefix/suffix) | Emphatic lām / nūn | segment | حرف تأكيد | CORRECT | Segment-only. |
| `REM` | Resumption | حرف استئناف | yes | yes | 2,925 | no (0) | وَ/فَ الاستئنافية | Resumption particle | segment prefix | حرف استئناف | CORRECT | Not exception; seed description correct. |
| `ANS` | Answer Particle | حرف جواب | yes | yes | 40 | yes (40) | بَلَىٰ 2:81:1; إِذًۭا 2:145:30 | Answer particle | head + segment | حرف جواب | CORRECT | — |
| `PRO` | **Independent Pronoun** | **ضمير منفصل** | yes | yes | 332 | yes (327) | لَا 2:11:4; فَلَا 2:22:18; وَلَا 2:35:12 | **Prohibition particle (لا الناهية)** | head + segment; **broad-cased to `اسم`** | **حرف نهي** | **WRONG** | **High UI risk** — both the detailed label (`ضمير منفصل`) and the broad label (`اسم`, via category `noun`) are wrong; category must become `particle` (Section 4). |
| `FUT` | Future Particle | حرف استقبال | yes | yes | 161 | yes (42) | سَوْفَ 4:56:5; فَسَوْفَ 4:30:6 | Future particle (سـ/سوف) | head + segment | حرف استقبال | CORRECT | — |
| `INTG` | Interrogative | استفهام | yes | yes | 946 | yes (433) | مَاذَآ 2:26:24; كَيْفَ 2:28:1 | Interrogative (particle **and** interrogative noun) | head + segment | استفهام | CONTEXT_DEPENDENT | Spans particle (`همزة/هل`) and noun (`ما/كيف/متى`); neutral `استفهام` is acceptable but category `particle` understates noun uses (Section 4). |
| `COND` | Conditional | حرف شرط | yes | yes | 1,049 | yes (1,048) | وَإِن 2:23:1; إِن 2:23:18 | Conditional particle | head + segment | حرف شرط | CORRECT | — |
| `PREV` | Preventive | ما الكافّة | yes | yes | 162 | no (0) | إنّـ**ما** | Preventive مَا | segment | ما الكافّة | CORRECT | Segment-only. |
| `CAUS` | Causative | فاء السببية | yes | yes | 88 | no (0) | فَـ (prefix) | Causal fā | segment prefix | فاء السببية | CORRECT | — |
| `AMD` | Amendment Particle | حرف استدراك | yes | yes | 65 | yes (65) | وَلَـٰكِن 2:12:5 | Amendment particle (لكنّ) | head + segment | حرف استدراك | CORRECT | — |
| `EXL` | Explanation | حرف تفصيل | yes | yes | 66 | yes (66) | فَأَمَّا 2:26:12; وَأَمَّا 2:26:20 | Explanation/detail particle (أمّا) | head + segment | حرف تفصيل | NEEDS_REVIEW | Arabic `تفصيل` vs English `Explanation` mismatch; both defensible for أمّا. Low priority. |
| `RES` | Restriction | أداة حصر | yes | yes | 558 | yes (558) | إِلَّآ 2:9:7; إِلَّا 2:45:6 | Restriction particle | head + segment | أداة حصر | ACCEPTABLE | Seed note "not aversion/ردع" correct. |
| `PRP` | Purpose | لام التعليل | yes | yes | 319 | no (0) | لِـ (prefix) | Purpose lām (لام كي) | segment prefix | لام التعليل | CORRECT | — |
| `COM` | Comitative | واو المعية | yes | yes | 3 | no (0) | وَ (prefix) | Comitative wāw | segment | واو المعية | CORRECT | — |
| `T` | Time Adverb | ظرف زمان | yes | yes | 1,166 | yes (1,166) | وَإِذَا 2:11:1; 2:13:1 | **Time adverb** | head + segment | ظرف زمان | CORRECT | Confirmed time adverb, **not** a feminine marker (Section 4). |
| `LOC` | Locative Adverb | ظرف مكان | yes | yes | 669 | yes (669) | مَعَكُمْ 2:14:13; فَوْقَهَا 2:26:11 | Location adverb | head + segment | ظرف مكان | CORRECT | — |
| `TIM` | Temporal Adverb | ظرف زمان | yes | no | 0 | no (0) | — | Temporal adverb | catalogue only | ظرف زمان | NEEDS_REVIEW | Duplicate Arabic label of `T`; zero DB use. Harmless but redundant. |
| `ABR` | Abbreviation | مختصر | yes | no | 0 | no (0) | — | Abbreviation | catalogue only (`other`) | مختصر | CORRECT | Zero DB use. |
| `DET` | Determiner | أداة تعريف | yes | yes | 8,377 | no (0) | ٱلـ (prefix) | Determiner (ال) | segment prefix | أداة تعريف | CORRECT | Segment-only. |
| `SUB` | Subordinating Conjunction | حرف مصدري | yes | yes | 684 | yes (681) | كَمَآ 2:13:5; أَن 2:26:5 | Subordinating conjunction | head + segment | حرف مصدري | NEEDS_REVIEW | `حرف مصدري` fits أن/كي but `كما` is comparative-subordinating, not strictly مصدري. Consider `حرف مصدري/أداة ربط`. |
| `IMPN` | Imperative Verbal Noun | اسم فعل أمر | yes | yes | 2 | yes (2) | مِسَاسَ 20:97:10; هَآؤُمُ 69:19:7 | Imperative verbal noun | head + segment | اسم فعل أمر | CORRECT | Distinct from `IMPV` (the lām). |
| `AVR` | Aversion | حرف ردع | yes | yes | 33 | yes (33) | كَلَّا 19:79:1 | Aversion particle (كلا) | head + segment | حرف ردع | CORRECT | — |
| `CERT` | Certainty | حرف تحقيق | yes | yes | 414 | yes (414) | قَدْ 2:60:14; وَلَقَدْ 2:65:1 | Particle of certainty (قد) | head + segment | حرف تحقيق | CORRECT | — |
| `CIRC` | Circumstantial | واو الحال | yes | yes | 293 | no (0) | وَ (prefix) | Circumstantial wāw | segment | واو الحال | CORRECT | — |
| `EQ` | Equalization | همزة التسوية | yes | yes | 6 | no (0) | أَ (prefix) | Equalization hamza | segment | همزة التسوية | CORRECT | — |
| `EXH` | Exhortation | حرف تحضيض | yes | yes | 40 | yes (40) | لَوْلَا 2:118:5 | Exhortation particle | head + segment | حرف تحضيض | CORRECT | — |
| `EXP` | Exceptive | حرف استثناء | yes | yes | 104 | yes (104) | إِلَّا 2:32:6; 2:34:7 | Exceptive particle (إلا) | head + segment | حرف استثناء | CORRECT | — |
| `INC` | Inceptive | حرف ابتداء/استفتاح | yes | yes | 90 | yes (90) | أَلَآ 2:12:1; 2:13:13 | Inceptive particle (ألا الاستفتاحية) | head + segment | حرف ابتداء/استفتاح | CORRECT | — |
| `INT` | Interpretation | حرف تفسير | yes | yes | 47 | yes (47) | أَن 2:125:16; أَنْ 3:193:7 | Particle of interpretation (أي/أن المفسرة) | head + segment | حرف تفسير | CORRECT | — |
| `RET` | Retraction | حرف إضراب | yes | yes | 122 | yes (122) | بَل 2:88:4; بَلْ 2:100:7 | Retraction particle (بل) | head + segment | حرف إضراب | CORRECT | — |
| `RSLT` | Result | الفاء الرابطة لجواب الشرط | yes | yes | 350 | no (0) | فَـ (prefix) | Result fā (apodosis) | segment prefix | الفاء الرابطة لجواب الشرط | CORRECT | — |
| `SUP` | Supplemental | حرف زائد | yes | yes | 235 | yes (13) | مَّا 2:26:8; مَا 2:144:16 | Supplemental/extra particle | head + segment | حرف زائد | CORRECT | — |
| `SUR` | Surprise | حرف فجاءة | yes | yes | 35 | yes (35) | إِذَا 4:77:17; فَإِذَا 6:44:18 | Surprise particle (إذا الفجائية) | head + segment | حرف فجاءة | CORRECT | — |

**Tally:** 49 codes. WRONG = 1 (`PRO`). NEEDS_REVIEW = 3 (`EXL`, `TIM`, `SUB`). CONTEXT_DEPENDENT = 1 (`INTG`). ACCEPTABLE = 2 (`ACC`, `RES`). CORRECT = 42.

---

## 4. Special Suspicious Codes (Deep Dive)

### `PRO` — **CONFIRMED WRONG**
- **Current:** `ArabicLabel = "ضمير منفصل"`, `EnglishLabel = "Independent Pronoun"`, `Category = "noun"`, `SortOrder = 20`.
- **QAC meaning:** `PRO` = **prohibition particle** (لا الناهية), the lā that makes a following imperfect verb jussive to express prohibition.
- **Evidence:** all DB examples are prohibitive lā: `لَا تُفْسِدُوا` (`2:11:4`), `فَلَا تَجْعَلُوا` (`2:22:18`), `وَلَا تَقْرَبَا` (`2:35:12`). 327 head occurrences / 332 segment occurrences, all this pattern. The label appears to have been confused with `PRON` (the actual pronoun, code 5).
- **Correct label:** **`حرف نهي`** (English: `Prohibition Particle`), **category `particle`**.
- **Affected display paths:**
  1. Mushaf Reader — `WordMorphologyDto.HeadPosLabel.ar` (selected-word "نوع الكلمة") and `RenderedSegmentDto.SegmentPosLabel.ar` (segment rows) both show `ضمير منفصل` (`EfWordAnalysisReader.MapMorphology` / `MapSegments`).
  2. Unique Words list — `EfUniqueWordsReader.ResolvePrimaryWordTypeBroadLabel("PRO", "noun")` returns `اسم` (because category is `noun`). After the category fix to `particle`, it would correctly return `حرف`.
  3. i‘rab catalogue — `STEM:PRO → "ضمير منفصل"` (`I3rabRuleCatalogSeedData.cs:66`) shows the same wrong text as `segmentI3rabArabic` (see Section 5).

### `PRON` — CORRECT (keep POS-level generic)
- **Current:** `ضمير` / `Pronoun` / `noun`. This **is** the real pronoun code.
- **Decision:** POS-level label should remain **`ضمير` only**. `ضمير منفصل` vs `ضمير متصل` **cannot** be decided from POS alone — it depends on **segment kind**: a `STEM:PRON` is independent (منفصل), a `SUFFIX:PRON` is attached (متصل). This distinction already lives correctly in the i‘rab catalogue (e.g. `STEM:PRON:3FS → "ضمير للغائبة"`, `SUFFIX:PRON:3FP → "ضمير متصل للغائبات"`). Do **not** push منفصل/متصل into the POS catalogue.

### `ACC` — ACCEPTABLE (POS), but watch the i‘rab phrasing
- **Current:** `حرف نصب` / `Accusative Particle`. QAC `ACC` = accusative particle. The broad `حرف نصب` is safe and correct.
- **When `حرف نصب من أخوات إنّ` is misleading:** `ACC` is broader than إنّ-and-sisters; it also tags other accusative particles. Labeling every `ACC` as "من أخوات إنّ" mislabels non-inna accusative cases. The narrow phrasing exists only in i‘rab rules, not the POS seed — so the POS label needs no change; just avoid forcing the inna-family wording at the POS layer.

### `NEG` — CORRECT
- `حرف نفي` / `Negation`. Negative particle (لا/ما/لم/لن). Correct, and correctly distinct from `PRO` (prohibition).

### `INL` — CORRECT
- `حروف مقطّعة` / `Quranic Initials`. Disjoint surah-opening letters (الحروف المقطّعة). Seed description explicitly says "not an oath particle". The Unique Words reader additionally special-cases `INL → حروف مقطّعة` as a broad class. Both correct.

### `T` — CORRECT (time adverb, not feminine marker)
- `ظرف زمان` / `Time Adverb`. In this corpus `T` is the **time adverb** code (e.g. إذا, يوم), **not** a feminine (تاء التأنيث) marker. DB examples (وَإِذَا …) confirm. Correct.

### `IMPV` — CORRECT (lām of command)
- `لام الأمر` / `Imperative Lām`. `IMPV` is the **prefixed lām of command** on a jussive verb; the imperative verb itself is coded `V`. Seed description states this. Segment-only (78 segments, 0 head). Correct. Distinct from `IMPN` (imperative verbal **noun**).

### `IMPN` — CORRECT
- `اسم فعل أمر` / `Imperative Verbal Noun`. DB examples مِسَاسَ (20:97:10), هَآؤُمُ (69:19:7). Correct.

### `SUB` — NEEDS_REVIEW (minor)
- `حرف مصدري`. Fits أن/كي but `كما` (a DB head example) is comparative subordinating, not strictly مصدري. Optionally broaden to `حرف مصدري/أداة ربط`. Low priority.

### `INTG` — CONTEXT_DEPENDENT
- `استفهام`. `INTG` spans an interrogative **particle** (همزة الاستفهام, هل) and interrogative **nouns** (ما, كيف, متى, أين). The seed keeps a neutral `استفهام` and the i‘rab catalogue splits `PREFIX:INTG → همزة استفهام` vs `STEM:INTG → اسم استفهام`. The POS-level neutral label is acceptable; just note its `category = particle` understates the noun usages, so any future "broad class from category" logic will mis-bucket interrogative nouns under `حرف`.

### Other requested codes (REM, RES, AMD, PREV, DET, P, CONJ, REL, VOC, COM, EQ, EXH, INT, SUR, SUP, EXL, N, PN, ADJ, V)
All verified CORRECT or ACCEPTABLE in Section 3. The only non-clean ones among these are `EXL` (Arabic/English nuance, NEEDS_REVIEW) and `RES`/`ACC` (broad but acceptable).

---

## 5. POS Labels vs. i‘rab Labels (Keep Separate)

| Aspect | `quran_pos_tags` (POS catalogue) | `quran_i3rab_rules` (i‘rab catalogue) |
| --- | --- | --- |
| Seed file | `PosTagSeed.cs` | `I3rabRuleCatalogSeedData.cs` |
| Key | POS `code` (e.g. `PRO`) | Segment **signature** (e.g. `STEM:PRO`, `SUFFIX:PRON:3FP`) |
| Granularity | One technical POS/type label per code | Contextual grammar label encoding case/voice/person |
| Surfaced as | `headPosLabel`, `segmentPosLabel` | `segmentI3rabArabic` (+ rule signature/family/status) |
| Purpose | "What type of word is this" | "How is this segment parsed grammatically" |

- POS labels are **technical type labels**; i‘rab labels are **simplified i‘rab display labels**. They are related but not interchangeable.
- **Do not** recommend rewriting i‘rab rule labels in this slice unless there is direct evidence one is wrong.
- **One direct-evidence exception worth flagging:** `STEM:PRO → "ضمير منفصل"` (`I3rabRuleCatalogSeedData.cs:66`). Because `STEM:PRO` is the prohibition particle لا (same `2:11:4` data), this i‘rab label is wrong by the **same root cause** as the POS `PRO` error and should read `لا الناهية` / `حرف نهي`. It is recorded here as evidence; the POS fix is primary, and the i‘rab correction is left for explicit human approval (it changes a different seed and its rule signatures/tests).
- This report's primary focus remains **POS/type labels**.

---

## 6. Database Verification

- **Availability:** `quran_dashboard` exists on local PostgreSQL. This session's psql role lacked table-level `SELECT` (`permission denied for table quran_pos_tags`); credential discovery was out of scope.
- **Evidence source used:** the prior in-repo read-only inventory (`report/feature-017-lexical-explorers-polish/word-types-taxonomy-inventory-report.md`), which queried `quran_dashboard` read-only (`default_transaction_read_only=on`) and recorded: `quran_pos_tags` = 49 rows; `quran_word_morphology` = 77,432 rows; `quran_word_morphology_segments` = 128,219 rows; `quran_i3rab_rules` = 142 rows / 87 distinct `i3rab_arabic`.
- **Label/seed consistency:** every label in that inventory matches `PosTagSeed.cs`, including the wrong `PRO = "ضمير منفصل"`. → **DB labels currently match the seed source** (no drift), so a seed fix + reseed will fully propagate.
- **Head-POS usage** counts per code are reproduced in Section 3 (`Head ct`).
- **Mismatch found:** none between DB and seed. The mismatch is between **seed/DB and linguistic correctness** for `PRO`.

> If a privileged read-only re-query is desired, the confirming queries are: `SELECT code, arabic_label, english_label, category FROM quran_pos_tags ORDER BY sort_order;`, `SELECT pos, count(*) FROM quran_word_morphology_segments GROUP BY pos ORDER BY 2 DESC;`, and `SELECT head_pos, count(*) FROM quran_word_morphology GROUP BY head_pos ORDER BY 2 DESC;`.

---

## 7. Final Recommendations

### A. Definitely Wrong Labels
| Code | Current Arabic | Correct Arabic | Why definitely wrong | Risk |
| --- | --- | --- | --- | --- |
| `PRO` | ضمير منفصل (category `noun`, English "Independent Pronoun") | **حرف نهي** (category `particle`, English "Prohibition Particle") | QAC `PRO` is the prohibition particle (لا الناهية); 100% of DB occurrences are prohibitive lā (`2:11:4` etc.). Confused with `PRON`. | **High** — visibly mislabels لا in the Mushaf Reader and mis-buckets it as `اسم` in Unique Words. Scholar-facing, undermines trust. |

### B. Acceptable Labels
| Code | Current Arabic | Why acceptable |
| --- | --- | --- |
| `ACC` | حرف نصب | Correct broad QAC meaning; avoids the misleading inna-family narrowing. |
| `RES` | أداة حصر | Matches QAC restriction; seed note disambiguates from ردع. |
| `SUP` | حرف زائد | Correct for supplemental particle. |
| (all 42 CORRECT rows in Section 3) | — | Match QAC meaning and real DB usage. |

### C. Needs Human Review
| Code | Current Arabic | Possible alternatives | Why review |
| --- | --- | --- | --- |
| `SUB` | حرف مصدري | حرف مصدري/أداة ربط | `كما` (DB head example) is comparative-subordinating, not strictly مصدري. |
| `EXL` | حرف تفصيل | حرف تفصيل / حرف تفسير | Arabic `تفصيل` vs English `Explanation`; both defensible for أمّا. |
| `TIM` | ظرف زمان | (merge/remove) | Duplicate Arabic label of `T`; zero DB use — decide whether to keep the code. |

### D. Context-Dependent Labels
| Code | Base Arabic label | Extra context needed for a more specific display |
| --- | --- | --- |
| `PRON` | ضمير | Segment **kind**: `STEM:PRON` → منفصل, `SUFFIX:PRON` → متصل (already in i‘rab catalogue). Do not encode at POS level. |
| `INTG` | استفهام | Segment kind: `PREFIX:INTG` → همزة استفهام (particle), `STEM:INTG` → اسم استفهام (noun). |
| `ACC` | حرف نصب | Lexeme: only some `ACC` are "من أخوات إنّ"; keep that detail in i‘rab, not POS. |

### E. Recommended Fix Strategy (per change)
| Change | Classification |
| --- | --- |
| `PRO` Arabic label `ضمير منفصل → حرف نهي` and English `Independent Pronoun → Prohibition Particle` | **Update `PosTagSeed.cs` only** (text). |
| `PRO` `Category "noun" → "particle"` (fixes the Unique Words broad label `اسم → حرف`) | **Update `PosTagSeed.cs`** (category) — required for the broad-label fix; no reader logic change needed (`ResolvePrimaryWordTypeBroadLabel` already maps `particle → حرف`). |
| Apply the corrected `PRO` row to the existing DB | **Update importer/reseed flow** (`force` morphology import) **or** add a one-off **data-patch migration** (`UPDATE quran_pos_tags … WHERE code='PRO'`). Pick one; reseed is the established path, data-patch is lighter. |
| Reader / projection logic | **No change needed** (label flows from DB; broad mapping already correct once category is `particle`). |
| Frontend display | **No change needed** (no hardcoded POS labels). |
| `STEM:PRO` i‘rab label `ضمير منفصل → لا الناهية/حرف نهي` | **Update importer/reseed flow** for `quran_i3rab_rules` — **separate, human-approval-gated** (out of primary POS scope). |
| `SUB`, `EXL`, `TIM` | **No change** until human review concludes. |
| Tests/fixtures | **Update tests/fixtures** to assert the corrected `PRO` labels (Section 8) after approval. |

---

## 8. Recommended Tests (After Human Approval Of The Corrected Table)

1. **POS seed correctness (`PRO`):** `PosTagSeed.GetAll()` entry for `PRO` has `ArabicLabel == "حرف نهي"`, `EnglishLabel == "Prohibition Particle"`, `Category == "particle"`.
2. **Mushaf Reader detailed label:** word analysis for `2:11:4` (لَا) returns `morphology.headPosLabel.ar == "حرف نهي"` (and the prohibition segment's `segmentPosLabel.ar == "حرف نهي"`).
3. **Unique Words broad label:** a unique word whose winner `head_pos == "PRO"` returns `primaryWordTypeBroadArabicLabel == "حرف"` (not `اسم`).
4. **`PRON` stays pronoun-related:** POS label for `PRON` is exactly `ضمير`; segment-kind tests keep `STEM:PRON` → منفصل and `SUFFIX:PRON` → متصل at the i‘rab layer.
5. **`T` is not a feminine marker:** POS label for `T` is `ظرف زمان`; a `T` head word (e.g. `2:11:1` وَإِذَا) renders `ظرف زمان`.
6. **`INL` is disjoint letters:** `INL` head (e.g. `2:1:1` الٓمٓ) renders detailed `حروف مقطّعة` and broad `حروف مقطّعة`.
7. **DB ↔ seed parity:** a check (test or import validation) asserting every `quran_pos_tags` row equals the corresponding `PosTagSeed` row (catches future drift / un-reseeded patches).
8. **API exposure:** `WordAnalysisResponse` and the Unique Words list endpoint return the corrected Arabic labels for the codes above.
9. *(If the i‘rab correction is approved)* `STEM:PRO` i‘rab label asserts `لا الناهية`/`حرف نهي`, not `ضمير منفصل`.

---

## 9. Constraints Honored

Report only. No code, seed, DB, frontend, migration, or test changes were made. Confirmed facts (Sections 1–6) are separated from recommendations (Sections 7–8). Evidence is drawn from repository source files and the prior in-repo read-only DB inventory; the single definitive defect (`PRO`) is corroborated by real Quran morphology data (`2:11:4`, `2:22:18`, `2:35:12`).
