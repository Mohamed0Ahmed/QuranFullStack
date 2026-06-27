# Simplified Segment I‘rab Labels Review Report

**Feature:** 017 — Lexical Explorers Polish
**Task type:** REPORT ONLY (no code/seed/DB/migration/frontend changes; no commits)
**Branch:** `017-lexical-explorers-polish`
**Date:** 2026-06-27
**Scope line:** *lower-line* simplified per-segment i‘rab label (segment card bottom row), **not** the middle-line POS/type label and **not** full scholarly ayah i‘rab.

---

## 0. Executive Summary

- **Source of truth for simplified i‘rab labels = `I3rabRuleCatalogSeedData.cs`** (142 signature→Arabic-label rows). Wrapped by `I3rabRuleCatalogSeed`, matched per segment by `SegmentSignatureBuilder` + `I3rabAssembler`, persisted by `EfI3rabGenerationWriter` / `I3rabSql` into `quran_i3rab_rules` and the `i3rab_*` columns of `quran_word_morphology_segments`.
- **Generation is separate from POS seeding.** POS labels live in `quran_pos_tags` (POS cleanup territory). I‘rab labels live in `quran_i3rab_rules` + segment `i3rab_arabic`. Different files, different tables, different command (`generate-i3rab` vs `import-morphology`).
- **One label is definitively wrong: `STEM:PRO` (rule 57) = `ضمير منفصل`.** `STEM:PRO` is the **prohibition particle** لا الناهية (332 segments: `لَا` `2:11:4`, `فَلَا` `2:22:18`, `وَلَا` `2:35:12`). The label is the same pronoun confusion already corrected in the POS layer — but the i‘rab catalogue was **not** updated, so the lower line still reads `ضمير منفصل`.
- **This now produces a visible contradiction.** After the POS cleanup, the **middle line shows `حرف نهي`** (correct) while the **lower line shows `ضمير منفصل`** (wrong) on the *same segment card*. Highest UI risk in this audit.
- **Frontend hard-codes no i‘rab labels.** `segment-data-rows.component.html` renders raw `segment.segmentI3rabArabic`. Fixing the catalogue + regenerating fixes the UI with no frontend change.
- **Three labels need human review** (`STEM:ACC` over-specific parenthetical, `STEM:SUB`, `STEM:EXL`). Everything else (≈138 rows) is **CORRECT/ACCEPTABLE** — pronoun منفصل/متصل split, noun case, verb tense/voice, particle types, لفظ الجلالة upgrade, idiom atomicity are all sound.
- **No combined word-phrase (`جار ومجرور`) is stored as a segment label.** Segment labels stay atomic (`حرف جر` + `اسم مجرور`); the `جار ومجرور` collapse is a read-layer/word-summary concern only. This is correct.

**Confirmed fact vs. recommendation:** Sections 1–3 + Table 2 are confirmed from repository source and prior in-repo read-only DB inventory (feature-005 reports). The wrong/needs-review verdicts and proposed wordings are recommendations, separated in Section 6 and Tables 3–5.

---

## 1. Simplified I‘rab Source Map (Table 1)

| Source | Role | Defines labels? | Runtime table affected | UI/API path affected | Notes |
| --- | --- | :-: | --- | --- | --- |
| `Backend/infrastructure/.../SimpleI3rabGeneration/I3rabRuleCatalogSeedData.cs` | **Source of truth** — 142 `I3rabRuleSeedRow(signatureKey, ruleFamily, i3rabArabic, status, description, sortOrder)`. | **Yes (canonical)** | `quran_i3rab_rules` + segment `i3rab_arabic` | lower-line i‘rab | All 142 rows `Approved`. |
| `Backend/infrastructure/.../SimpleI3rabGeneration/I3rabRuleCatalogSeed.cs` | Loads catalog into a `signatureKey → row` dictionary; `TryGet`. | No (transport) | — | — | `II3rabRuleCatalog` impl. |
| `Backend/infrastructure/.../SimpleI3rabGeneration/SegmentSignatureBuilder.cs` | Builds the per-segment signature key from `Kind`, `Pos`, case/person/tense/voice features, ALLAH flag. | No (key only) | — | — | Decides which catalog row a segment matches; getting the signature wrong = wrong label even if catalog is right. |
| `Backend/infrastructure/.../SimpleI3rabGeneration/I3rabAssembler.cs` | Per segment: signature → `catalog.TryGet`. Match → `i3rab_arabic` + `Approved`; miss → `null` + `Unsupported` + reason. | No (assignment) | segment `i3rab_status` | lower-line presence | No fuzzy fallback — unknown signature ⇒ blank i‘rab. |
| `Backend/infrastructure/.../SimpleI3rabGeneration/AllahLemmaMatcher.cs` | Flags لفظ الجلالة so PN gets `STEM:PN:ALLAH:<case>`. | No | — | — | Drives the لفظ الجلالة upgrade. |
| `Backend/infrastructure/.../SimpleI3rabGeneration/I3rabSeedLabelCorrections.cs` | A **list of signatures** whose seed labels were deliberately corrected; checked by validation. | No (audit list) | — | — | Used by `I3rabValidationRunner` (`CountLabelCorrectionsPresent`) to assert corrections landed — not a label source. |
| `Backend/infrastructure/.../Persistence/.../SimpleI3rabGeneration/I3rabSql.cs` | `UpsertRule` (INSERT…ON CONFLICT) into `quran_i3rab_rules`; `CreateStagingTable`/`CopyStaging`/`UpdateSegmentsFromStaging` to set segment `i3rab_arabic`, `i3rab_rule_id`, `i3rab_status`, `i3rab_review_reason`. | No (transport) | `quran_i3rab_rules`, `quran_word_morphology_segments` | both | Segment label joined to rule by `signature_key`. |
| `Backend/infrastructure/.../Persistence/.../SimpleI3rabGeneration/EfI3rabGenerationWriter.cs` + `GenerateI3rabHandler` | Orchestrate generation: read segments → assemble → upsert rules → update segments. | No | both | both | The `generate-i3rab` path. |
| `Backend/infrastructure/.../Persistence/Reads/Quran/MushafReader/EfWordAnalysisReader.cs` (`MapSegments`) | Reads segment `i3rab_arabic` → `RenderedSegmentDto.SegmentI3rabArabic` (+ rule signature/family/status). | No (exposes) | reads `quran_word_morphology_segments` | word-analysis API | Falls back to nothing if `i3rab_arabic` null (lower line hidden). |
| `Backend/application/.../MushafReader/Responses/WordAnalysisResponse.cs` (`RenderedSegmentDto`) | DTO carrying `SegmentI3rabArabic`, `I3rabRuleSignature`, `I3rabRuleFamily`, `I3rabStatus`. | No | — | API contract | — |
| `Frontend/.../mushaf/components/segment-data-rows/segment-data-rows.component.html` | Renders `segment.segmentI3rabArabic` raw in `.segment-data-rows__i3rab` (lower line), shown only when present. | **No hardcoded labels** | — | lower-line UI | `data-testid="segment-i3rab-label"`. |
| `Frontend/.../mushaf/models/mushaf.models.ts` | TS `segmentI3rabArabic` field. | No | — | — | — |
| `Backend/report/feature-005-word-simple-i3rab-foundation/*.md` | Prior **read-only DB** inventory + coverage (counts, examples). | Reference | — | — | Evidence source for counts/examples here. |

### Seeding / generation flow (confirmed)
1. **Catalog → rules table:** `generate-i3rab` runs `I3rabSql.UpsertRule` for each catalog row → `quran_i3rab_rules` (idempotent via `ON CONFLICT (signature_key) DO UPDATE`). So re-running with an edited catalog **updates** existing rule rows in place.
2. **Segments → labels:** each segment's signature (from `SegmentSignatureBuilder`) is assembled; results are `COPY`-ed to a temp staging table, then `UpdateSegmentsFromStaging` sets `i3rab_arabic` / `i3rab_rule_id` / `i3rab_status` / `i3rab_review_reason` on `quran_word_morphology_segments` (joining staging→rules by `signature_key`).
3. **Read path:** `EfWordAnalysisReader.MapSegments` returns `SegmentI3rabArabic` to the API; the frontend lower line renders it verbatim.

### Operational answers
- **Is editing `I3rabRuleCatalogSeedData.cs` enough for future generation?** Yes for any future `generate-i3rab` run.
- **Existing DB:** not retroactively updated. Re-run **`generate-i3rab`** (the `UpsertRule` updates the rule row; the staging `UPDATE` re-stamps every affected segment). Because `i3rab_arabic` is a derived column updated by targeted `UPDATE`, **no morphology re-import and no migration is needed** for a label-only change — `generate-i3rab` (force) is the smallest path.
- **Force vs reset:** a label-only change needs only `generate-i3rab` (force regenerate). A full `import-morphology --force` would *clear* the `i3rab_*` columns (segments truncated), so morphology rebuilds must always be **followed** by `generate-i3rab`. A data patch (`UPDATE quran_i3rab_rules … ; UPDATE quran_word_morphology_segments …`) is possible but unnecessary given the idempotent generator.
- **Frontend:** no change (no hardcoded i‘rab labels).

---

## 2. Exhaustive Rule / Signature Inventory (Table 2)

All 142 catalog signatures. `Count` from prior read-only DB inventory (feature-005, `quran_dashboard`, segment rows). Verb and attached-pronoun person/number variants share one label and are grouped (each member signature listed). "POS label (cmp)" = current `quran_pos_tags` Arabic label for comparison (post-POS-cleanup where noted).

### 2.1 Nouns / adjectives / proper nouns (case)
| Signature | Kind | POS | Features | POS label (cmp) | Current i‘rab label | Count | Examples | Proposed | Verdict | Notes |
| --- | --- | --- | --- | --- | --- | ---: | --- | --- | --- | --- |
| `STEM:N:NOM` | STEM | N | NOM | اسم | اسم مرفوع | 6,777 | ٱلْمُفْلِحُونَ 2:5:8 | (keep) | CORRECT | — |
| `STEM:N:ACC` | STEM | N | ACC | اسم | اسم منصوب | 7,955 | رَبِّ 2:126:4 | (keep) | CORRECT | — |
| `STEM:N:GEN` | STEM | N | GEN | اسم | اسم مجرور | 10,404 | يَوْمِ 2:8:7 | (keep) | CORRECT | — |
| `STEM:N:GEN:1S` | STEM | N | GEN+1S | اسم | اسم مجرور مضاف إلى ياء المتكلم | — | رَبِّـ(ي) | (keep) | CORRECT | Correction-list entry; precise. |
| `STEM:ADJ:NOM` | STEM | ADJ | NOM | صفة | صفة مرفوعة | 843 | — | (keep) | CORRECT | Feminine agreement correct. |
| `STEM:ADJ:ACC` | STEM | ADJ | ACC | صفة | صفة منصوبة | 590 | — | (keep) | CORRECT | — |
| `STEM:ADJ:GEN` | STEM | ADJ | GEN | صفة | صفة مجرورة | 528 | — | (keep) | CORRECT | — |
| `STEM:PN:NOM` | STEM | PN | NOM | اسم علم | اسم علم مرفوع | 1,321 | — | (keep) | CORRECT | — |
| `STEM:PN:ACC` | STEM | PN | ACC | اسم علم | اسم علم منصوب | 912 | — | (keep) | CORRECT | — |
| `STEM:PN:GEN` | STEM | PN | GEN | اسم علم | اسم علم مجرور | 1,678 | — | (keep) | CORRECT | — |
| `STEM:PN:ALLAH:NOM` | STEM | PN | ALLAH+NOM | اسم علم | لفظ الجلالة مرفوع | 979 | ٱللَّهُ 2:255:1 | (keep) | CORRECT | Lemma-aware upgrade. |
| `STEM:PN:ALLAH:ACC` | STEM | PN | ALLAH+ACC | اسم علم | لفظ الجلالة منصوب | 592 | ٱللَّهَ 2:9:2 | (keep) | CORRECT | — |
| `STEM:PN:ALLAH:GEN` | STEM | PN | ALLAH+GEN | اسم علم | لفظ الجلالة مجرور | 1,127 | ٱللَّهِ 1:1:2 | (keep) | CORRECT | — |

### 2.2 Independent pronouns / demonstratives / relatives
| Signature | Kind | POS | Features | POS label (cmp) | Current i‘rab label | Count | Examples | Proposed | Verdict | Notes |
| --- | --- | --- | --- | --- | --- | ---: | --- | --- | --- | --- |
| `STEM:PRON:3MS` | STEM | PRON | 3MS | ضمير | ضمير للغائب | — | هُوَ | (keep / +منفصل) | CORRECT | Independent pronoun; person accurate. |
| `STEM:PRON:3MP` | STEM | PRON | 3MP | ضمير | ضمير للغائبين | — | هُمْ 2:4:11 | (keep) | CORRECT | — |
| `STEM:PRON:3FS` | STEM | PRON | 3FS | ضمير | ضمير للغائبة | — | هِيَ | (keep) | CORRECT | — |
| `STEM:PRON:3FP` | STEM | PRON | 3FP | ضمير | ضمير للغائبات | — | هُنَّ | (keep) | CORRECT | — |
| `STEM:PRON:3D` | STEM | PRON | 3D | ضمير | ضمير للغائبَين | — | هُمَا | (keep) | CORRECT | — |
| `STEM:PRON:2MS` | STEM | PRON | 2MS | ضمير | ضمير للمخاطب | — | أَنْتَ | (keep) | CORRECT | — |
| `STEM:PRON:2MP` | STEM | PRON | 2MP | ضمير | ضمير للمخاطبين | — | أَنْتُمْ | (keep) | CORRECT | — |
| `STEM:PRON:2FS` | STEM | PRON | 2FS | ضمير | ضمير للمخاطبة | — | أَنْتِ | (keep) | CORRECT | — |
| `STEM:PRON:2D` | STEM | PRON | 2D | ضمير | ضمير للمخاطبَين | — | أَنْتُمَا | (keep) | CORRECT | — |
| `STEM:PRON:1S` | STEM | PRON | 1S | ضمير | ضمير للمتكلم المفرد | — | أَنَا | (keep) | CORRECT | — |
| `STEM:PRON:1P` | STEM | PRON | 1P | ضمير | ضمير لجماعة المتكلمين | — | نَحْنُ | (keep) | CORRECT | — |
| `STEM:REL` | STEM | REL | — | اسم موصول | اسم موصول | 3,575 | ٱلَّذِينَ 1:7:2 | (keep) | CORRECT | — |
| `STEM:DEM` | STEM | DEM | — | اسم إشارة | اسم إشارة | 1,059 | أُو۟لَـٰٓئِكَ 2:5:1 | (keep) | CORRECT | — |
| `STEM:DEM:2MP` / `STEM:DEM:2FP` / `STEM:DEM:2D` | STEM | DEM | 2MP/2FP/2D | اسم إشارة | اسم إشارة | — | ذَٰلِكُمْ | (keep) | CORRECT | Person token captured but label stays generic — fine. |
| `STEM:IMPN` | STEM | IMPN | — | اسم فعل أمر | اسم فعل أمر | 2 | هَآؤُمُ 69:19:7 | (keep) | CORRECT | — |

### 2.3 Attached pronouns (`SUFFIX:PRON:<person>`) — base `ضمير متصل`, all CORRECT
| Signature | Person | Current i‘rab label | Count | Notes |
| --- | --- | --- | ---: | --- |
| `SUFFIX:PRON:3MP` | 3MP | ضمير متصل للغائبين | 7,366 | هِمْ in عَلَيْهِمْ 1:7:4 |
| `SUFFIX:PRON:2MP` | 2MP | ضمير متصل للمخاطبين | 4,645 | — |
| `SUFFIX:PRON:3MS` | 3MS | ضمير متصل للغائب | 2,727 | — |
| `SUFFIX:PRON:1P` | 1P | ضمير متصل لجماعة المتكلمين | 2,347 | — |
| `SUFFIX:PRON:2MS` | 2MS | ضمير متصل للمخاطب | 1,300 | — |
| `SUFFIX:PRON:1S` | 1S | ضمير متصل للمتكلم المفرد | 1,239 | 208 have NULL form (elided ياء المتكلم); label-only, do not invent form. |
| `SUFFIX:PRON:3FS` | 3FS | ضمير متصل للغائبة | 1,062 | — |
| `SUFFIX:PRON:3FP` | 3FP | ضمير متصل للغائبات | 267 | — |
| `SUFFIX:PRON:3MD` / `:3D` | 3MD/3D | ضمير متصل للغائبَين | small | Two signatures, same label. |
| `SUFFIX:PRON:2D` / `:2MD` | 2D/2MD | ضمير متصل للمخاطبَين | small | — |
| `SUFFIX:PRON:3FD` | 3FD | ضمير متصل للغائبتَين | small | — |
| `SUFFIX:PRON:2FS` | 2FS | ضمير متصل للمخاطبة | small | — |
| `SUFFIX:PRON:2FP` | 2FP | ضمير متصل للمخاطبات | small | — |
| `SUFFIX:PRON:2FD` | 2FD | ضمير متصل للمخاطبتَين | small | — |

> **منفصل/متصل decision (confirmed safe):** independence is read from **segment kind** — `STEM:PRON` ⇒ independent, `SUFFIX:PRON` ⇒ attached (متصل). The catalogue applies this correctly; person/gender/number come from the `PRON:<person>` feature token and are accurate. No POS-only guessing.

### 2.4 Verbs (`STEM:V:<tense>:<voice>:<person>`) — collapse to 5 labels, all CORRECT
| Label | Tense/Voice | Member signatures (persons) | Count (words) | Examples |
| --- | --- | --- | ---: | --- |
| فعل ماض | PERF·ACT | `3MS,3MP,1P,2MP,3FS,2MS,1S,3MD,3FP,3FD,2D,2FP,2FS` | 8,516 | قَالَ 2:30:2 |
| فعل مضارع | IMPF·ACT | `3MS,3MP,2MP,1P,2MS,3FS,1S,2D,3FP,3MD,2FS,2FP,2FD,3FD` | 7,824 | يَقُولُ 2:8:4; تَجْعَلُ 2:30:11 |
| فعل أمر | IMPV·ACT | `2MS,2MP,2FS,2MD,2FP,2D` | 1,876 | ٱقْرَأْ 96:1:1 |
| فعل ماض مبني للمجهول | PERF·PASS | `3MS,3MP,3FS,1S,1P,2MP,2MS,3FD,3FP` | 634 | أُنزِلَ 2:4:4 |
| فعل مضارع مبني للمجهول | IMPF·PASS | `3MS,3MP,2MP,3FS,1P,2MS,1S,2MD,3FP,2MD…` | 506 | — |

> Active voice intentionally omits `مبني للمعلوم` (kept simple); passive shown as `مبني للمجهول`. Person/number captured in the signature but not surfaced in the simplified label — acceptable for a simplified line.

### 2.5 Particles & prefixes/suffixes
| Signature | Kind | POS | POS label (cmp) | Current i‘rab label | Count | Examples | Proposed | Verdict | Notes |
| --- | --- | --- | --- | --- | ---: | --- | --- | --- | --- |
| `PREFIX:P` | PREFIX | P | حرف جر | حرف جر | 5,325 | بِـ | (keep) | CORRECT | — |
| `STEM:P` | STEM | P | حرف جر | حرف جر | 7,679 | عَلَيْ 1:7:4 | (keep) | CORRECT | — |
| `SUFFIX:P` | SUFFIX | P | حرف جر | لام الجر | 2 | — | (keep) | CORRECT | Correction-list entry; rare. |
| `PREFIX:DET` | PREFIX | DET | أداة تعريف | أداة تعريف | 8,377 | ٱلْ | (keep) | CORRECT | Not over-deep; correct. |
| `PREFIX:CONJ` / `STEM:CONJ` | PREFIX/STEM | CONJ | حرف عطف | حرف عطف | 8,694 / 756 | وَ / ثُمَّ | (keep) | CORRECT | — |
| `STEM:NEG` | STEM | NEG | حرف نفي | حرف نفي | 2,688 | لَا 2:2:3; لَمْ 2:6:8 | (keep) | CORRECT | Distinct from `PRO`. |
| `STEM:ACC` | STEM | ACC | حرف نصب | **حرف نصب (من أخوات إنّ/النواصب)** | 2,283 | إِنَّ 2:6:1; إِنَّمَا 2:11:9; إِنَّهُمْ 2:12:2 | **حرف نصب** (or حرف توكيد ونصب) | **NEEDS_REVIEW** | Parenthetical too specific; misleading for non-inna accusative particles. |
| `STEM:PRO` | STEM | PRO | **حرف نهي** | **ضمير منفصل** | 332 | لَا 2:11:4; فَلَا 2:22:18; وَلَا 2:35:12 | **حرف نهي / لا الناهية** | **WRONG** | Prohibition particle; contradicts the corrected POS label. |
| `STEM:COND` | STEM | COND | حرف شرط | أداة شرط | 1,049 | إِن 2:23:18 | (keep) | CORRECT | — |
| `STEM:SUB` | STEM | SUB | حرف مصدري | حرف مصدري | 684 | كَمَا 2:13:5; أَن 2:26:5 | حرف مصدري / أداة ربط | NEEDS_REVIEW | `كما` comparative, not strictly مصدري. |
| `STEM:T` | STEM | T | ظرف زمان | ظرف زمان | 1,166 | إِذَا | (keep) | CORRECT | i‘rab layer already correct (POS-seed once had تاء تأنيث). |
| `STEM:LOC` | STEM | LOC | ظرف مكان | ظرف مكان | 669 | — | (keep) | CORRECT | — |
| `STEM:RES` | STEM | RES | أداة حصر | أداة حصر | 558 | إِلَّا | (keep) | CORRECT | — |
| `STEM:EXP` | STEM | EXP | **أداة استثناء** | أداة استثناء (إلّا) | 104 | إِلَّا 2:32:6 | (keep) | CORRECT | Matches POS cleanup. |
| `PREFIX:INTG` | PREFIX | INTG | استفهام | همزة استفهام | 507 | أَ in أَتَجْعَلُ 2:30:11 | (keep) | CORRECT | Correctly split from stem. |
| `STEM:INTG` | STEM | INTG | استفهام | اسم استفهام | 439 | مَاذَا; كَيْفَ 2:28:1 | (keep) | CORRECT | — |
| `STEM:CERT` | STEM | CERT | حرف تحقيق | حرف تحقيق (قد) | 414 | قَدْ 2:60:14 | (keep) | CORRECT | — |
| `PREFIX:VOC` | PREFIX | VOC | حرف نداء | حرف نداء | 371 | يَـٰ | (keep) | CORRECT | — |
| `SUFFIX:VOC` | SUFFIX | VOC | حرف نداء | ميم عوض عن حرف النداء | 5 | اللَّهُمَّ | (keep) | CORRECT | Precise; correction-list entry. |
| `PREFIX:RSLT` | PREFIX | RSLT | الفاء الرابطة لجواب الشرط | الفاء الرابطة لجواب الشرط | 350 | فَـ | (keep) | CORRECT | — |
| `PREFIX:PRP` | PREFIX | PRP | لام التعليل | لام التعليل | 319 | لِـ | (keep) | CORRECT | — |
| `PREFIX:CIRC` | PREFIX | CIRC | **حرف حال** | واو الحال | 293 | وَ | حرف حال / واو الحال | NEEDS_REVIEW (consistency) | POS cleanup made middle line `حرف حال`; lower line still `واو الحال`. Both correct grammar; align wording or keep i‘rab more specific. |
| `PREFIX:EMPH` | PREFIX | EMPH | حرف تأكيد | لام التوكيد (المزحلقة) | 1,001 | لَـ | (keep) | CORRECT | — |
| `SUFFIX:EMPH` | SUFFIX | EMPH | حرف تأكيد | نون التوكيد | 243 | ـنَّ | (keep) | CORRECT | — |
| `PREFIX:REM` | PREFIX | REM | حرف استئناف | حرف استئناف | 2,925 | فَـ/وَ | (keep) | CORRECT | i‘rab layer correct (POS-seed once had حرف استثناء). |
| `STEM:PREV` | STEM | PREV | ما الكافّة | ما الكافّة | 162 | إنّـما | (keep) | CORRECT | — |
| `STEM:AMD` | STEM | AMD | حرف استدراك | حرف استدراك | 65 | لَـٰكِن | (keep) | CORRECT | i‘rab correct (POS-seed once mislabeled). |
| `STEM:SUP` / `PREFIX:SUP` | STEM/PREFIX | SUP | حرف زائد | حرف زائد | 21 / 214 | مَّا 2:26:8 | (keep) | CORRECT | — |
| `STEM:EXL` | STEM | EXL | حرف تفصيل | حرف تفصيل | 66 | فَأَمَّا 2:26:12 | حرف تفصيل / حرف شرط وتفصيل | NEEDS_REVIEW | Minor; أمّا is شرط+تفصيل. |
| `STEM:INT` | STEM | INT | حرف تفسير | حرف تفسير | 47 | أَنْ 3:193:7 | (keep) | CORRECT | — |
| `STEM:EXH` | STEM | EXH | حرف تحضيض | حرف تحضيض | 40 | لَوْلَا 2:118:5 | (keep) | CORRECT | — |
| `STEM:ANS` | STEM | ANS | حرف جواب | حرف جواب | 40 | بَلَىٰ | (keep) | CORRECT | — |
| `STEM:SUR` | STEM | SUR | حرف فجاءة | حرف فجاءة | 35 | إِذَا 4:77:17 | (keep) | CORRECT | — |
| `STEM:AVR` | STEM | AVR | حرف ردع | حرف ردع (كلّا) | 33 | كَلَّا 19:79:1 | (keep) | CORRECT | — |
| `STEM:INC` | STEM | INC | حرف ابتداء/استفتاح | حرف ابتداء/استفتاح | 90 | أَلَآ 2:12:1 | (keep) | CORRECT | — |
| `STEM:RET` | STEM | RET | حرف إضراب | حرف إضراب (بل) | 122 | بَل 2:88:4 | (keep) | CORRECT | — |
| `STEM:INL` | STEM | INL | حروف مقطّعة | حروف مقطّعة (فواتح السور) | 30 | الٓمٓ 2:1:1 | (keep) | CORRECT | — |
| `PREFIX:CAUS` | PREFIX | CAUS | **حرف سببية** | فاء السببية | 88 | فَـ | فاء السببية / حرف سببية | NEEDS_REVIEW (consistency) | POS cleanup made middle line `حرف سببية`; lower line still `فاء السببية` (both correct; align if desired). |
| `PREFIX:IMPV` | PREFIX | IMPV | لام الأمر | لام الأمر | 78 | لِـ | (keep) | CORRECT | The lām; verb itself is `V`. |
| `PREFIX:FUT` / `STEM:FUT` | PREFIX/STEM | FUT | حرف استقبال | حرف استقبال | 119 / 42 | سَـ / سَوْفَ | (keep) | CORRECT | — |
| `PREFIX:EQ` | PREFIX | EQ | همزة التسوية | همزة التسوية | 6 | أَ | (keep) | CORRECT | — |
| `PREFIX:COM` | PREFIX | COM | واو المعية | واو المعية | 3 | وَ | (keep) | CORRECT | — |

**Tally:** 142 signatures. WRONG = 1 (`STEM:PRO`). NEEDS_REVIEW = 3 substantive (`STEM:ACC`, `STEM:SUB`, `STEM:EXL`) + 2 consistency-only (`PREFIX:CIRC`, `PREFIX:CAUS` wording vs POS line). CORRECT/ACCEPTABLE = remainder.

---

## 3. High-Risk Case Verifications

### A) `PRO` — CONFIRMED WRONG
- Signatures involving `PRO`: **only `STEM:PRO`** (rule 57). Label still `ضمير منفصل`.
- `STEM:PRO` = the prohibition particle لا الناهية (jussive-inducing لا). 332 segments; all prohibitive: `لَا تُفْسِدُوا` `2:11:4`, `فَلَا تَجْعَلُوا` `2:22:18`, `وَلَا تَقْرَبَا` `2:35:12`. **Classify WRONG.**
- **Proposed label — which is better for the lower line?**
  - `حرف نهي` = the type (matches the catalogue's `حرف X` house style; but it now **duplicates** the corrected POS middle-line label exactly).
  - `لا الناهية` = names the specific particle; reads like i‘rab and **differentiates** the lower line from the POS line.
  - `حرف نهي وجزم` = fullest simplified i‘rab (لا الناهية is jussive), still concise.
  - **Recommendation:** prefer **`لا الناهية`** (or `حرف نهي وجزم`) for the lower line so it adds i‘rab value instead of echoing the POS line; `حرف نهي` is acceptable if exact POS/i‘rab duplication is fine. Final wording is a human decision (Table 4); the **current `ضمير منفصل` is definitively wrong** regardless.

### B) `PRON` — CORRECT (stem vs suffix verified)
- `STEM:PRON:*` → `ضمير + <person>` (independent). `SUFFIX:PRON:*` → `ضمير متصل + <person>` (attached). Independence derived from **kind**, accurately.
- Person/gender/number details are present and correct (e.g. `3MP → للغائبين`, `1P → لجماعة المتكلمين`). Useful and accurate.
- `STEM:PRON` can safely be independent; the catalogue says `ضمير للغائب…` (omits the word `منفصل`). Safe; optionally prepend `منفصل` for explicitness — minor, not required.

### C) `ACC` — NEEDS_REVIEW (parenthetical too specific)
- `STEM:ACC` = `حرف نصب (من أخوات إنّ/النواصب)`. Examples are inna-family (إِنَّ، إِنَّمَا، إِنَّهُمْ), but QAC `ACC` is broader than إنّ's sisters; the `/النواصب` clause further muddies it.
- **Confirmed fact:** the dominant data is inna-family. **Recommendation:** drop the parenthetical → `حرف نصب`; if the team wants the إنّ flavor, use `حرف توكيد ونصب` (only safe when restricted to إنّ/أنّ, which the signature cannot currently guarantee). `حرف مصدري ناصب` would be wrong here (that's `أنْ`, tagged `SUB`). **Safest user-facing label: `حرف نصب`.**

### D) `NEG` vs `PRO` — difference confirmed
- `STEM:NEG` = `حرف نفي` — negation (لا/ما/لم/لن). Correct, keep. Example `لَا` `2:2:3` (لا النافية: «لا ريب فيه»).
- `STEM:PRO` should be `حرف نهي / لا الناهية` — prohibition. Example `لَا` `2:11:4` (لا الناهية: «لا تفسدوا»).
- Same surface form `لَا`, **different function**: `2:2:3` negates a statement; `2:11:4` forbids an action (jussive verb follows). The corpus already distinguishes them as `NEG` vs `PRO`; only the `PRO` i‘rab label is wrong.

### E) `P` + nouns / GEN — CORRECT, atomicity preserved
- `PREFIX:P` / `STEM:P` → `حرف جر`. `STEM:N:GEN` → `اسم مجرور`. `STEM:PN:GEN` → `اسم علم مجرور`; لفظ الجلالة → `لفظ الجلالة مجرور`. `STEM:ADJ:GEN` → `صفة مجرورة`. All correct.
- **No `جار ومجرور` stored as a segment label** — confirmed: no such signature exists in the catalogue. The `جار ومجرور` collapse is a **read-layer/word-summary** idiom (feature-005 §2), correctly kept out of per-segment labels. Segments stay atomic. No fix needed.

### F) `DET` — CORRECT
- `PREFIX:DET` → `أداة تعريف`. Not treated as deeper i‘rab. Correct.

### G) `INTG` — CORRECT (split verified)
- `PREFIX:INTG` → `همزة استفهام` (the أ prefix; `أَتَجْعَلُ` `2:30:11`). `STEM:INTG` → `اسم استفهام` (مَاذَا/كَيْفَ). Correctly separated by kind; no misleading broad label.

### H) `SUB / PREV / RES / AMD / SUP / EXL / INT / EXH / SUR / INC / RET / RSLT`
- All present and labelled (Table 2.5). CORRECT except `SUB` (كما comparative → NEEDS_REVIEW) and `EXL` (تفصيل nuance → NEEDS_REVIEW). `PREV→ما الكافّة`, `RES→أداة حصر`, `AMD→حرف استدراك`, `SUP→حرف زائد`, `INT→حرف تفسير`, `EXH→حرف تحضيض`, `SUR→حرف فجاءة`, `INC→حرف ابتداء/استفتاح`, `RET→حرف إضراب (بل)`, `RSLT→الفاء الرابطة لجواب الشرط` are all correct.

### I) `V` — CORRECT (tense/voice verified)
- `PERF→فعل ماض`, `IMPF→فعل مضارع`, `IMPV→فعل أمر`; passive adds `مبني للمجهول`. Active omits `مبني للمعلوم` (simplified, acceptable). Tense/voice present on all verbs (feature-005: 19,356 verbs all tensed, 1,140 passive). No missing/wrong tense or voice labels found.

---

## 4. Wrong Labels Only (Table 3)

| Signature | Current wrong label | Correct proposed label | Why wrong | Real examples | Source file to fix | DB/generation step | UI/API affected |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `STEM:PRO` | ضمير منفصل | **لا الناهية** (or `حرف نهي` / `حرف نهي وجزم`) | `PRO` is the prohibition particle لا الناهية, not a pronoun (pronoun confusion). Now also contradicts the corrected POS middle-line `حرف نهي`. | لَا 2:11:4; فَلَا 2:22:18; وَلَا 2:35:12 (332 segments) | `I3rabRuleCatalogSeedData.cs` (rule 57) | `generate-i3rab` (force regenerate) — no migration, no morphology re-import | Mushaf Reader lower-line `segmentI3rabArabic`; word-analysis API `RenderedSegmentDto.SegmentI3rabArabic` |

---

## 5. Needs Human Review (Table 4)

| Signature | Current label | Possible alternatives | Why ambiguous | Examples | Recommended decision question |
| --- | --- | --- | --- | --- | --- |
| `STEM:PRO` (wording) | (→ being fixed) | `حرف نهي` vs `لا الناهية` vs `حرف نهي وجزم` | Lower line should ideally not duplicate the POS line verbatim. | لَا 2:11:4 | "Do we want the i‘rab line to add function (لا الناهية / جزم) or mirror the POS type (حرف نهي)?" |
| `STEM:ACC` | حرف نصب (من أخوات إنّ/النواصب) | `حرف نصب` · `حرف توكيد ونصب` | Parenthetical too specific for non-inna accusative particles. | إِنَّ 2:6:1; إِنَّمَا 2:11:9 | "Keep broad `حرف نصب`, or keep إنّ-flavor only if signature can guarantee inna-family?" |
| `STEM:SUB` | حرف مصدري | `حرف مصدري` · `أداة ربط` | `كما` is comparative-subordinating, not strictly مصدري. | كَمَا 2:13:5; أَن 2:26:5 | "Broaden to cover non-maṣdarī subordinators, or accept dominant مصدري?" |
| `STEM:EXL` | حرف تفصيل | `حرف تفصيل` · `حرف شرط وتفصيل` | أمّا carries both شرط and تفصيل. | فَأَمَّا 2:26:12 | "Is `حرف تفصيل` enough for أمّا on the simplified line?" |
| `PREFIX:CIRC` / `PREFIX:CAUS` (consistency) | واو الحال / فاء السببية | align with POS line `حرف حال` / `حرف سببية`, or keep specific | Both grammatically correct; only inconsistent with the new POS wording. | وَ / فَـ | "Should i‘rab line stay more specific (واو الحال) than the POS line (حرف حال), or match?" |

---

## 6. Context-Dependent Labels (Table 5)

| Signature/code | Base safe label | More specific possible label | Required extra context | Recommendation |
| --- | --- | --- | --- | --- |
| `STEM:PRON:*` | ضمير + person | ضمير منفصل + person | none (kind already = STEM) | Keep; optionally prepend منفصل. Already unambiguous. |
| `SUFFIX:PRON:*` | ضمير متصل + person | + role (فاعل/مفعول/مضاف إليه) | syntactic role — **not derivable from morphology** | Keep form/case only; never infer role. |
| `STEM:V:*` | فعل + tense (+مبني للمجهول) | + person/number, + إعراب (مرفوع/مجزوم) | syntactic position / mood | Keep simplified; mood/role is full-i‘rab territory. |
| `STEM:N/PN/ADJ:<case>` | اسم/صفة/اسم علم + case | + role (فاعل/مفعول/خبر) | syntactic role | Keep case-only at segment level; role belongs to full ayah i‘rab. |
| `STEM:ACC` | حرف نصب | حرف توكيد ونصب | lexeme = إنّ/أنّ specifically | Stay broad unless signature guarantees inna-family. |
| word idioms (`P+PRON`, `P+…GEN`) | (atomic segments) | جار ومجرور | adjacent-segment composition | Keep atomic at segment grain; compose `جار ومجرور` in the read layer only. |

---

## 7. Final Recommendations

### A. Definitely wrong simplified i‘rab labels
- **`STEM:PRO` = `ضمير منفصل` → `لا الناهية`** (or `حرف نهي` / `حرف نهي وجزم`). Prohibition particle; contradicts corrected POS line. Examples `2:11:4`, `2:22:18`, `2:35:12`. Fix in `I3rabRuleCatalogSeedData.cs` (rule 57).

### B. Correct — leave unchanged
- All noun/adjective/proper-noun case labels; لفظ الجلالة upgrade; all `STEM:PRON`/`SUFFIX:PRON` person labels; all verb tense/voice labels; `NEG`, `P`, `DET`, `CONJ`, `REL`, `DEM`, `INTG` (both kinds), `COND`, `RES`, `EXP`, `CERT`, `VOC` (both), `RSLT`, `PRP`, `EMPH` (both), `REM`, `PREV`, `AMD`, `SUP`, `INT`, `EXH`, `ANS`, `SUR`, `AVR`, `INC`, `RET`, `INL`, `IMPV`, `FUT`, `EQ`, `COM`, `IMPN`, `T`, `LOC`, `N:GEN:1S`. (~138 rows.)

### C. Needs human review
- `STEM:ACC` (parenthetical), `STEM:SUB` (كما), `STEM:EXL` (أمّا), and the `PREFIX:CIRC` / `PREFIX:CAUS` wording-consistency vs the POS line. See Table 4.

### D. Context-dependent — keep broad
- No syntactic roles (فاعل/مفعول/مبتدأ/خبر/حال) at segment grain; `جار ومجرور` stays a read-layer idiom; `STEM:ACC` stays `حرف نصب` unless lexeme context is guaranteed. See Table 5.

### E. Recommended implementation strategy (after approval)
| Change | Classification |
| --- | --- |
| `STEM:PRO` label `ضمير منفصل → لا الناهية`/`حرف نهي` | **Update `I3rabRuleCatalogSeedData.cs`** (rule 57) only. |
| `STEM:ACC` / `STEM:SUB` / `STEM:EXL` wording (if approved) | **Update `I3rabRuleCatalogSeedData.cs`** rows. |
| `PREFIX:CIRC` / `PREFIX:CAUS` consistency (if approved) | **Update `I3rabRuleCatalogSeedData.cs`** rows. |
| Apply to existing DB | **Operational `generate-i3rab` (force regenerate)** — `UpsertRule` updates `quran_i3rab_rules`; staging `UPDATE` re-stamps segments. No migration, no morphology re-import. |
| Reader / projection logic | **No change** (label flows from segment `i3rab_arabic`). |
| Frontend | **No change** (renders raw `segmentI3rabArabic`). |
| Generator / signature logic | **No change** (signatures already resolve `PRO` correctly; only the catalogue label is wrong). |
| POS labels | **No change** — out of scope; POS cleanup is separate and already done. |
| `I3rabSeedLabelCorrections.cs` | Optional: **add the touched signatures** so validation tracks them (housekeeping, not required). |
| Tests/fixtures | **Update tests/fixtures** to assert the new `STEM:PRO` label (Section F). |

### F. Recommended tests (after approval)
1. `STEM:PRO` does **not** produce `ضمير منفصل` (catalogue row + generated segment label).
2. `STEM:PRO` produces the approved label (`لا الناهية` / `حرف نهي`) — word analysis for `2:11:4` returns that `segmentI3rabArabic`.
3. `STEM:PRON:*` stays `ضمير + person`; `SUFFIX:PRON:*` stays `ضمير متصل + person` (kind-driven منفصل/متصل intact).
4. `P+GEN` words keep **atomic** segment labels (`حرف جر` + `اسم مجرور`); no `جار ومجرور` leaks into a segment label.
5. `quran_i3rab_rules` row label and the generated `quran_word_morphology_segments.i3rab_arabic` for `STEM:PRO` are **in sync** after `generate-i3rab` (no drift between rule table and segment column).
6. Word-analysis API exposes the corrected lower-line `segmentI3rabArabic` for a known `PRO` location.
7. (If `STEM:ACC` approved) `STEM:ACC` returns the agreed safer label, not the over-specific parenthetical.

---

## 8. Database Verification

- **Availability:** `quran_dashboard` exists locally; this session's psql role lacks table `SELECT` (`permission denied`), and credential discovery is out of scope. Counts/examples above are taken from the prior **read-only** feature-005 inventory/coverage reports (`quran_word_morphology_segments` = 128,219; per-signature segment counts and example locations), cross-checked against the catalogue.
- **Catalogue ↔ DB sync (confirmed by design):** segments are stamped from the same catalogue via `signature_key` join, and `quran_i3rab_rules` is upserted from the catalogue. So `quran_word_morphology_segments.i3rab_arabic` mirrors `quran_i3rab_rules.i3rab_arabic`, which mirrors `I3rabRuleCatalogSeedData.cs` — including the wrong `STEM:PRO`. No drift expected; the defect exists uniformly across catalogue, rules table, and segment column.
- **If a privileged read-only re-query is wanted:** `SELECT signature_key, i3rab_arabic FROM quran_i3rab_rules ORDER BY sort_order;`, `SELECT i3rab_rule_id, i3rab_arabic, count(*) FROM quran_word_morphology_segments GROUP BY 1,2 ORDER BY 3 DESC;`, and (`STEM:PRO` proof) `SELECT w.location, s.i3rab_arabic FROM quran_word_morphology_segments s JOIN quran_i3rab_rules r ON r.id=s.i3rab_rule_id JOIN quran_words w ON w.id=s.quran_word_id WHERE r.signature_key='STEM:PRO' LIMIT 10;`.

---

## 9. Constraints Honored

Report only — no code/seed/DB/migration/frontend/test changes; no commits; no destructive commands. POS/type labels kept separate from simplified i‘rab labels; simplified segment i‘rab kept separate from full ayah i‘rab. Confirmed facts (Sections 1–3, Table 2, Section 8) are separated from recommendations (Sections 6–7, Tables 3–5). The single definitive defect (`STEM:PRO`) is corroborated by real Quran morphology data.
