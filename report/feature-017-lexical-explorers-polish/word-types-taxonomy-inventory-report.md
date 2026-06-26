# Word Types Taxonomy Inventory Report

## 1. Scope And Verdict

- Task type: report only; no source/test changes.
- Branch inspected: `017-lexical-explorers-polish`.
- Runtime database was available and queried read-only: `quran_dashboard` on local PostgreSQL.
- Current source of truth for POS/type catalogue is `quran_pos_tags`, populated from `PosTagSeed.GetAll()`.
- Current source of truth for per-word type assignment is `quran_word_morphology.head_pos`.
- Current source of truth for segment-level type assignment is `quran_word_morphology_segments.pos`.

## 2. Sources Inspected

### Repository Files

| Source | Finding |
| --- | --- |
| `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/PosTagSeed.cs` | Defines 49 POS codes, Arabic labels, English labels, `category`, sort order, and some descriptions. |
| `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/PosTag.cs` | POS entity fields: `Code`, `ArabicLabel`, `EnglishLabel`, `Category`, `SortOrder`, `Description`. |
| `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/WordMorphology.cs` | Word-level morphology stores `HeadPos`. |
| `Backend/infrastructure/.../MorphologyAssembler.cs` | `headPos` is the first `STEM` segment POS, or first segment POS fallback; unknown POS codes are collected against `PosTagSeed`. |
| `Backend/infrastructure/.../MorphologyBulkCopier.cs` | Copies `PosTagSeed.GetAll()` into `quran_pos_tags`; copies `word.HeadPos` into `quran_word_morphology.head_pos`; copies segment POS into `quran_word_morphology_segments.pos`. |
| `Backend/infrastructure/.../MorphologySql.cs` | Validation requires `head_pos` to match first `STEM` segment POS and checks morphology coverage/segment counts. |
| `Backend/infrastructure/.../PosTagConfiguration.cs` | Maps `quran_pos_tags` columns and indexes `category`, `sort_order`. |
| `Backend/infrastructure/.../WordMorphologyConfiguration.cs` | Maps `quran_word_morphology.head_pos` and FK to `quran_pos_tags.code`. |
| `Backend/infrastructure/.../WordMorphologySegmentConfiguration.cs` | Maps `quran_word_morphology_segments.pos` and FK to `quran_pos_tags.code`; also stores `i3rab_arabic`. |
| `Backend/application/.../MushafReader/Responses/WordAnalysisResponse.cs` | Backend response exposes `headPos`, `headPosLabel`, `segmentPos`, `segmentPosLabel`. |
| `Backend/infrastructure/.../MushafReader/EfWordAnalysisReader.cs` | Reads labels from `quran_pos_tags`; falls back to raw code if missing. |
| `Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts` | Frontend model includes `headPosLabel` and `segmentPosLabel`. |
| `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/word-morphology-summary/word-morphology-summary.component.html` | Current UI displays word type as `morphology().headPosLabel.ar`. |
| `Backend/infrastructure/.../SimpleI3rabGeneration/I3rabRuleCatalogSeedData.cs` | Simple i3rab has separate contextual Arabic labels, derived by rule signature/family, not canonical POS catalogue labels. |
| `Backend/report/database/current-database-tables-and-relationships-report.md` | Confirms `quran_pos_tags` has 49 rows and `quran_word_morphology` has 77,432 rows in baseline. |

### Tables Queried

| Table | Use |
| --- | --- |
| `quran_pos_tags` | Canonical POS code catalogue and labels. |
| `quran_word_morphology` | Runtime word-level `head_pos` counts. |
| `quran_word_morphology_segments` | Segment-level POS counts and i3rab label context. |
| `quran_words` | Example word text/location for each `head_pos`. |
| `quran_i3rab_rules` | Distinct contextual i3rab Arabic labels. |

## 3. Database Verification

- DB query mode: `PGOPTIONS='-c default_transaction_read_only=on'`.
- `quran_pos_tags`: 49 rows.
- `quran_word_morphology`: 77,432 rows.
- `quran_word_morphology_segments`: 128,219 rows.
- `quran_i3rab_rules`: 142 rows, 87 distinct `i3rab_arabic` labels.
- No DB writes were performed.

## 4. Category Summary

| Category in `quran_pos_tags` | Codes | Head occurrences | Segment occurrences | Codes used as head | Codes used as segment |
| --- | ---: | ---: | ---: | ---: | ---: |
| `noun` | 11 | 40,854 | 62,496 | 10 | 10 |
| `verb` | 3 | 19,356 | 19,356 | 1 | 1 |
| `particle` | 34 | 17,222 | 46,367 | 22 | 34 |
| `other` | 1 | 0 | 0 | 0 | 0 |

Notes:

- 16 codes have zero `head_pos` occurrences.
- 4 codes have zero segment occurrences: `PERF`, `IMPF`, `TIM`, `ABR`.
- Many zero-head codes are still meaningful segment POS codes, especially prefixes like `DET`, `REM`, `EMPH`, `VOC`, and `RSLT`.

## 5. Complete POS Catalogue Inventory

`Head count` is from `quran_word_morphology.head_pos`. `Segment count` is included because it explains codes that never appear as word-level head types.

| # | Code | Arabic label | English/internal meaning | Category | Head count | Segment count | 3 head examples |
| ---: | --- | --- | --- | --- | ---: | ---: | --- |
| 1 | `N` | اسم | Noun | `noun` | 25,135 | 25,136 | بِسْمِ (1:1:1); ٱلْحَمْدُ (1:2:1); رَبِّ (1:2:3) |
| 2 | `V` | فعل | Verb | `verb` | 19,356 | 19,356 | نَعْبُدُ (1:5:2); نَسْتَعِينُ (1:5:4); ٱهْدِنَا (1:6:1) |
| 3 | `PN` | اسم علم | Proper Noun | `noun` | 3,911 | 3,911 | ٱللَّهِ (1:1:2); لِلَّهِ (1:2:2); ٱللَّهُ (2:7:2) |
| 4 | `ADJ` | صفة | Adjective | `noun` | 1,961 | 1,961 | ٱلرَّحْمَـٰنِ (1:1:3); ٱلرَّحِيمِ (1:1:4); ٱلرَّحْمَـٰنِ (1:3:1) |
| 5 | `PRON` | ضمير | Pronoun | `noun` | 3,301 | 24,685 | إِيَّاكَ (1:5:1); وَإِيَّاكَ (1:5:3); هُمْ (2:4:11) |
| 6 | `P` | حرف جر | Preposition | `particle` | 7,679 | 13,006 | عَلَيْهِمْ (1:7:4); عَلَيْهِمْ (1:7:7); فِيهِ ۛ (2:2:5) |
| 7 | `CONJ` | حرف عطف | Conjunction | `particle` | 756 | 9,450 | أَمْ (2:6:7); أَوْ (2:19:1); ثُمَّ (2:28:7) |
| 8 | `NEG` | حرف نفي | Negation | `particle` | 2,643 | 2,688 | وَلَا (1:7:8); لَا (2:2:3); لَمْ (2:6:8) |
| 9 | `REL` | اسم موصول | Relative Pronoun | `noun` | 3,323 | 3,575 | ٱلَّذِينَ (1:7:2); ٱلَّذِينَ (2:3:1); وَٱلَّذِينَ (2:4:1) |
| 10 | `DEM` | اسم إشارة | Demonstrative | `noun` | 1,059 | 1,059 | ذَٰلِكَ (2:2:1); أُو۟لَـٰٓئِكَ (2:5:1); وَأُو۟لَـٰٓئِكَ (2:5:6) |
| 11 | `VOC` | حرف نداء | Vocative | `particle` | 0 | 376 | — |
| 12 | `INL` | حروف مقطّعة | Quranic Initials | `particle` | 30 | 30 | الٓمٓ (2:1:1); الٓمٓ (3:1:1); الٓمٓصٓ (7:1:1) |
| 13 | `IMPV` | لام الأمر | Imperative Lām | `particle` | 0 | 78 | — |
| 14 | `PERF` | فعل ماض | Perfect | `verb` | 0 | 0 | — |
| 15 | `IMPF` | فعل مضارع | Imperfect | `verb` | 0 | 0 | — |
| 16 | `ACC` | حرف نصب | Accusative Particle | `particle` | 2,283 | 2,283 | إِنَّ (2:6:1); إِنَّمَا (2:11:9); إِنَّهُمْ (2:12:2) |
| 17 | `EMPH` | حرف تأكيد | Emphatic | `particle` | 0 | 1,244 | — |
| 18 | `REM` | حرف استئناف | Resumption | `particle` | 0 | 2,925 | — |
| 19 | `ANS` | حرف جواب | Answer Particle | `particle` | 40 | 40 | بَلَىٰ (2:81:1); بَلَىٰ (2:112:1); إِذًۭا (2:145:30) |
| 20 | `PRO` | ضمير منفصل | Independent Pronoun | `noun` | 327 | 332 | لَا (2:11:4); فَلَا (2:22:18); وَلَا (2:35:12) |
| 21 | `FUT` | حرف استقبال | Future Particle | `particle` | 42 | 161 | فَسَوْفَ (4:30:6); سَوْفَ (4:56:5); فَسَوْفَ (4:74:18) |
| 22 | `INTG` | استفهام | Interrogative | `particle` | 433 | 946 | مَاذَآ (2:26:24); كَيْفَ (2:28:1); مَا (2:68:7) |
| 23 | `COND` | حرف شرط | Conditional | `particle` | 1,048 | 1,049 | وَلَوْ (2:20:14); وَإِن (2:23:1); إِن (2:23:18) |
| 24 | `PREV` | ما الكافّة | Preventive | `particle` | 0 | 162 | — |
| 25 | `CAUS` | فاء السببية | Causative | `particle` | 0 | 88 | — |
| 26 | `AMD` | حرف استدراك | Amendment Particle | `particle` | 65 | 65 | وَلَـٰكِن (2:12:5); وَلَـٰكِن (2:13:17); وَلَـٰكِن (2:57:15) |
| 27 | `EXL` | حرف تفصيل | Explanation | `particle` | 66 | 66 | فَأَمَّا (2:26:12); وَأَمَّا (2:26:20); فَأَمَّا (3:7:14) |
| 28 | `RES` | أداة حصر | Restriction | `particle` | 558 | 558 | إِلَّآ (2:9:7); إِلَّا (2:26:38); إِلَّا (2:45:6) |
| 29 | `PRP` | لام التعليل | Purpose | `particle` | 0 | 319 | — |
| 30 | `COM` | واو المعية | Comitative | `particle` | 0 | 3 | — |
| 31 | `T` | ظرف زمان | Time Adverb | `noun` | 1,166 | 1,166 | وَإِذَا (2:11:1); وَإِذَا (2:13:1); وَإِذَا (2:14:1) |
| 32 | `LOC` | ظرف مكان | Locative Adverb | `noun` | 669 | 669 | مَعَكُمْ (2:14:13); حَوْلَهُۥ (2:17:9); فَوْقَهَا ۚ (2:26:11) |
| 33 | `TIM` | ظرف زمان | Temporal Adverb | `noun` | 0 | 0 | — |
| 34 | `ABR` | مختصر | Abbreviation | `other` | 0 | 0 | — |
| 35 | `DET` | أداة تعريف | Determiner | `particle` | 0 | 8,377 | — |
| 36 | `SUB` | حرف مصدري | Subordinating Conjunction | `particle` | 681 | 684 | كَمَآ (2:13:5); كَمَآ (2:13:10); أَن (2:26:5) |
| 37 | `IMPN` | اسم فعل أمر | Imperative Verbal Noun | `noun` | 2 | 2 | مِسَاسَ ۖ (20:97:10); هَآؤُمُ (69:19:7) |
| 38 | `AVR` | حرف ردع | Aversion | `particle` | 33 | 33 | كَلَّا ۚ (19:79:1); كَلَّا ۚ (19:82:1); كَلَّآ ۚ (23:100:6) |
| 39 | `CERT` | حرف تحقيق | Certainty | `particle` | 414 | 414 | قَدْ (2:60:14); وَلَقَدْ (2:65:1); وَقَدْ (2:75:5) |
| 40 | `CIRC` | واو الحال | Circumstantial | `particle` | 0 | 293 | — |
| 41 | `EQ` | همزة التسوية | Equalization | `particle` | 0 | 6 | — |
| 42 | `EXH` | حرف تحضيض | Exhortation | `particle` | 40 | 40 | لَوْلَا (2:118:5); لَوْلَآ (4:77:33); لَوْلَا (5:63:1) |
| 43 | `EXP` | حرف استثناء | Exceptive | `particle` | 104 | 104 | إِلَّا (2:32:6); إِلَّآ (2:34:7); إِلَّآ (2:78:6) |
| 44 | `INC` | حرف ابتداء/استفتاح | Inceptive | `particle` | 90 | 90 | أَلَآ (2:12:1); أَلَآ (2:13:13); أَلَآ (2:214:26) |
| 45 | `INT` | حرف تفسير | Interpretation | `particle` | 47 | 47 | أَن (2:125:16); أَنْ (3:193:7); أَنِ (4:66:5) |
| 46 | `RET` | حرف إضراب | Retraction | `particle` | 122 | 122 | بَل (2:88:4); بَلْ (2:100:7); بَل (2:116:6) |
| 47 | `RSLT` | الفاء الرابطة لجواب الشرط | Result | `particle` | 0 | 350 | — |
| 48 | `SUP` | حرف زائد | Supplemental | `particle` | 13 | 235 | مَّا (2:26:8); مَا (2:144:16); مَا (2:148:8) |
| 49 | `SUR` | حرف فجاءة | Surprise | `particle` | 35 | 35 | إِذَا (4:77:17); فَإِذَا (6:44:18); فَإِذَا (7:107:3) |

## 6. Zero-Head Codes

These are in the catalogue but do not appear as `quran_word_morphology.head_pos` in the current database.

| Code | Arabic label | Category | Segment count | Notes |
| --- | --- | --- | ---: | --- |
| `VOC` | حرف نداء | `particle` | 376 | Segment-only prefix/code in current data. |
| `IMPV` | لام الأمر | `particle` | 78 | Segment-only in current data; description says imperative verb itself is coded `V`. |
| `PERF` | فعل ماض | `verb` | 0 | Not used as POS; verb tense appears as feature/i3rab label. |
| `IMPF` | فعل مضارع | `verb` | 0 | Not used as POS; verb tense appears as feature/i3rab label. |
| `EMPH` | حرف تأكيد | `particle` | 1,244 | Segment-only in current data. |
| `REM` | حرف استئناف | `particle` | 2,925 | Segment-only in current data. |
| `PREV` | ما الكافّة | `particle` | 162 | Segment-only in current data. |
| `CAUS` | فاء السببية | `particle` | 88 | Segment-only in current data. |
| `PRP` | لام التعليل | `particle` | 319 | Segment-only in current data. |
| `COM` | واو المعية | `particle` | 3 | Segment-only in current data. |
| `TIM` | ظرف زمان | `noun` | 0 | Duplicate-like label with `T`; no current DB use. |
| `ABR` | مختصر | `other` | 0 | Only catalogue code with `other` category; no current DB use. |
| `DET` | أداة تعريف | `particle` | 8,377 | Segment-only determiner prefix. |
| `CIRC` | واو الحال | `particle` | 293 | Segment-only in current data. |
| `EQ` | همزة التسوية | `particle` | 6 | Segment-only in current data. |
| `RSLT` | الفاء الرابطة لجواب الشرط | `particle` | 350 | Segment-only in current data. |

## 7. Current Arabic POS Labels Used Elsewhere

### Backend/Frontend Word Analysis

- `WordAnalysisResponse.morphology.headPosLabel.ar` uses `quran_pos_tags.arabic_label`.
- `RenderedSegmentDto.segmentPosLabel.ar` uses `quran_pos_tags.arabic_label`.
- Frontend Mushaf selected-word summary displays `headPosLabel.ar` under `نوع الكلمة`.
- Segment views receive `segmentPosLabel`, also from `quran_pos_tags`.

### Simple I3rab

Simple i3rab labels are contextual grammar labels, not POS catalogue labels. They include POS-like labels plus case/voice/person details.

Examples from current DB:

- `فعل مضارع` (14 rules), `فعل ماض` (13), `فعل ماض مبني للمجهول` (9), `فعل مضارع مبني للمجهول` (9), `فعل أمر` (6).
- `اسم إشارة` (4), `اسم علم مجرور/مرفوع/منصوب`, `اسم مجرور/مرفوع/منصوب`, `اسم استفهام`, `اسم موصول`, `اسم فعل أمر`.
- `حرف جر`, `حرف عطف`, `حرف نفي`, `حرف نداء`, `حرف تحقيق (قد)`, `حرف نصب (من أخوات إنّ/النواصب)`, `حروف مقطّعة (فواتح السور)`.

Do not use i3rab labels as canonical Unique Words type labels; they are rule-level annotations and can be more specific than `head_pos`.

## 8. Proposed Taxonomy Tree

This tree is evidence-based from current `category`, labels, descriptions, and runtime usage. It is proposed for product/API discussion, not an implementation decision.

### Level 1: `اسم`

- Level 2: عام
  - `N` اسم
- Level 2: علم وصفة
  - `PN` اسم علم
  - `ADJ` صفة
- Level 2: ضمائر وأسماء مبهمة
  - `PRON` ضمير
  - `PRO` ضمير منفصل
  - `REL` اسم موصول
  - `DEM` اسم إشارة
  - `INTG` استفهام, uncertain because seed category is `particle` but description says prefix/stem usage diverges and i3rab can label stem as `اسم استفهام`.
- Level 2: ظروف
  - `T` ظرف زمان
  - `TIM` ظرف زمان, unused in current DB
  - `LOC` ظرف مكان
- Level 2: اسم فعل
  - `IMPN` اسم فعل أمر

### Level 1: `فعل`

- Level 2: عام
  - `V` فعل
- Level 2: زمن/صيغة, catalogue-only or feature/i3rab-level in current DB
  - `PERF` فعل ماض, zero head/segment POS usage
  - `IMPF` فعل مضارع, zero head/segment POS usage

### Level 1: `حرف`

- Level 2: جر/عطف/نفي/نصب/شرط/استقبال
  - `P` حرف جر
  - `CONJ` حرف عطف
  - `NEG` حرف نفي
  - `ACC` حرف نصب
  - `COND` حرف شرط
  - `FUT` حرف استقبال
  - `SUB` حرف مصدري
- Level 2: نداء/تعريف/تأكيد/استئناف
  - `VOC` حرف نداء, segment-only
  - `DET` أداة تعريف, segment-only
  - `EMPH` حرف تأكيد, segment-only
  - `CERT` حرف تحقيق
  - `REM` حرف استئناف, segment-only
  - `INC` حرف ابتداء/استفتاح
- Level 2: جواب/استدراك/تفصيل/حصر/استثناء/إضراب
  - `ANS` حرف جواب
  - `AMD` حرف استدراك
  - `EXL` حرف تفصيل
  - `RES` أداة حصر
  - `EXP` حرف استثناء
  - `RET` حرف إضراب
- Level 2: لامات وفاءات وواوات خاصة
  - `IMPV` لام الأمر, segment-only
  - `PRP` لام التعليل, segment-only
  - `CAUS` فاء السببية, segment-only
  - `RSLT` الفاء الرابطة لجواب الشرط, segment-only
  - `COM` واو المعية, segment-only
  - `CIRC` واو الحال, segment-only
- Level 2: أدوات خاصة
  - `PREV` ما الكافّة, segment-only
  - `AVR` حرف ردع
  - `EQ` همزة التسوية, segment-only
  - `EXH` حرف تحضيض
  - `INT` حرف تفسير
  - `SUP` حرف زائد
  - `SUR` حرف فجاءة

### Level 1: `حروف مقطّعة`

- Level 2: فواتح السور
  - `INL` حروف مقطّعة

Evidence for separate treatment:

- Existing Arabic label is already top-level-like: `حروف مقطّعة`.
- Existing description says: `Disconnected letters opening certain surahs (الحروف المقطّعة); not an oath particle`.
- Runtime usage is small but real: 30 head occurrences.
- Although current seed category is `particle`, displaying it as just `حرف` hides a Quran-specific class users likely recognize.

### Level 1: غير مصنف / آخر

- Level 2: Catalogue-only
  - `ABR` مختصر, category `other`, zero head and zero segment occurrences.

## 9. Uncertain Mappings

| Code | Current label | Evidence | Uncertainty |
| --- | --- | --- | --- |
| `INL` | حروف مقطّعة | Category is `particle`, but label/description identify disconnected Quranic initials; 30 head uses. | Product should decide whether broad class is `حروف مقطّعة` or `حرف`. Recommendation: separate broad class. |
| `INTG` | استفهام | Category is `particle`; description says `PREFIX:INTG` is `همزة الاستفهام` and `STEM:INTG` is `اسم استفهام`; 433 head uses. | Cannot cleanly force all `INTG` under `حرف` or `اسم` without segment/kind/rule context. |
| `PERF` | فعل ماض | Category is `verb`, but zero head and zero segment uses; verb tense is represented through `V` + features/i3rab labels. | Keep as catalogue entry, but do not expect it as Unique Words broad/detailed type from `head_pos`. |
| `IMPF` | فعل مضارع | Category is `verb`, but zero head and zero segment uses; verb tense is represented through `V` + features/i3rab labels. | Same as `PERF`. |
| `TIM` | ظرف زمان | Category is `noun`, but zero DB use and overlaps `T` label. | Do not surface unless runtime data appears. |
| `ABR` | مختصر | Category is `other`; zero DB use. | Only current code outside noun/verb/particle; no runtime evidence for UI grouping. |
| `PRO` | ضمير منفصل | Category is `noun`, 327 head uses; first examples are visually particles like `لَا`/`وَلَا`, likely because head POS follows first STEM segment after prefixes and rendered whole word includes prefixes. | UI should trust label from DB but avoid over-explaining examples without segment breakdown. |

## 10. Direct Product Answers

### Is `اسم / فعل / حرف` enough?

- Enough for a broad scan/filter only.
- Not enough as the only exposed API/type model because current catalogue has 49 codes and users can distinguish `حرف نفي`, `حرف جر`, `اسم علم`, `اسم إشارة`, `ضمير`, etc.
- If Unique Words table shows only broad class, keep detailed fields in API for tooltip/future filter/detail panel.

### Should `حروف مقطعة` be separate?

- Yes, safest product treatment is a separate broad class `حروف مقطّعة` for display/grouping.
- Evidence: existing label and description are explicit; current DB has 30 head occurrences; collapsing to `حرف` loses a recognizable Quran-specific category.
- If backend still keeps canonical `category = particle`, API can derive `broadClass = quranic_initials` for `INL` without changing seed data.

### Any current POS codes that do not fit cleanly under `اسم / فعل / حرف / حروف مقطعة`?

- `ABR` (`مختصر`, category `other`) does not fit, but has zero current head and segment occurrences.
- `INTG` does not fit cleanly as one broad class without extra context because code-level description says prefix/stem usage diverges; current seed category says `particle`, while i3rab can label stem usage as `اسم استفهام`.
- `PERF`/`IMPF` are verb-labelled catalogue entries but not current POS usages; they are better understood as tense labels/features, not current Unique Words word-type values.
- `TIM` is unused and duplicates the Arabic label of `T`.

## 11. Safest Unique Words API Shape

Recommendation: return both broad and detailed fields.

Suggested API fields for each Unique Words list item:

| Field | Purpose | Display now? |
| --- | --- | --- |
| `primaryWordTypeCode` | Exact source `quran_word_morphology.head_pos` winner code. | No, identity/testing/future filters. |
| `primaryWordTypeArabicLabel` | Current detailed label from `quran_pos_tags.arabic_label`. | Optional; useful tooltip/detail. |
| `primaryWordTypeEnglishLabel` | Current detailed English label from `quran_pos_tags.english_label`. | No. |
| `primaryWordTypeCategory` | Existing seed `category`: `noun`, `verb`, `particle`, `other`. | No, internal mapping/debug. |
| `primaryWordTypeBroadCode` | Derived display group, e.g. `noun`, `verb`, `particle`, `quranic_initials`, `other`, `unknown`. | No/optional. |
| `primaryWordTypeBroadArabicLabel` | Display label: `اسم`, `فعل`, `حرف`, `حروف مقطّعة`, etc. | Yes, recommended main column. |

Why both:

- Broad-only loses useful evidence and makes future type filters expensive or breaking.
- Detailed-only can clutter Unique Words table and creates product ambiguity for codes like `حرف نفي` vs broad `حرف`.
- Both lets table start calm with broad class while preserving exact POS provenance.

## 12. Short Recommendation

- Unique Words visible column should initially show broad class: `اسم`, `فعل`, `حرف`, `حروف مقطّعة`, fallback `—`.
- API should also return detailed POS code/labels from `quran_pos_tags` but table does not need to show them by default.
- Treat `INL` as broad `حروف مقطّعة` despite seed `category = particle`.
- Keep uncertain mappings explicit; do not rewrite seed taxonomy in this slice.
