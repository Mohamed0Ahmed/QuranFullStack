# Word-Level Lemma Alignment Audit Report

**Project:** Quran Dashboard / المنهج القرآني  
**Feature:** 017 — Lexical Explorers Polish  
**Branch:** `017-lexical-explorers-polish`  
**Task type:** REPORT ONLY — no production code, tests, migrations, importers, reseed, destructive commands, or commits changed.  
**Date:** 2026-06-27

Only read-only `SELECT` queries were run against the local `quran_dashboard` database. The existing PostgreSQL cluster was started because it was down; no import, reset, migration, reseed, or DML/DDL was run. DB and sudo credentials are intentionally not recorded in this report.

---

## 1. Verdict

### **BLOCKED**

`quran_word_morphology.lemma_id` is **not reliable enough today as the final occurrence-set authority for Lemmas Explorer** without a curated alignment correction pass.

The narrow segment-id audit finds only **8** head-lemma rows with no matching `quran_word_morphology_segments.lemma_id`. Seven are legitimate QUL-vs-Corpus compound/modeling divergences; one is the known `28:50:11` anomaly.

However, the broader source-alignment audit finds **63 likely QUL word-level lemma shifts** where:

- the current word has a content-word head lemma,
- the current word/root and current segments are rootless/pronominal/particle-like,
- the current segments do **not** carry the head lemma Buckwalter from Corpus,
- the previous word has the matching root and Corpus segment lemma Buckwalter,
- and the previous word's word-level lemma is null.

The important discovery: **62 of those 63 likely shifts already have a matching `segment.lemma_id`** because the importer copies the word-level head lemma into single-STEM segments. Therefore, a hard check that only asks "does any segment have the same `lemma_id` as the word head?" misses nearly all shifted cases.

Final recommendation: **BLOCKED for occurrence-set correctness** until the 63 likely shifted word-level lemmas are reviewed and corrected or explicitly accepted. The Lemma Details segment-matched type-distribution fix can still be developed, but it should not be treated as a final data-quality fix while the head occurrence set itself contains shifted lemma rows.

---

## 2. Executive Summary

The original suspicious case is real:

| Location | Word | Current word-level lemma | Actual current segments | Finding |
| --- | --- | --- | --- | --- |
| `28:50:11` | `مِمَّنِ` | `أَضَلّ` (`5942`) | `مِن` / `مِن` | `أَضَلّ` belongs to previous word `28:50:10` (`أَضَلُّ`) |

The direct no-matching-segment check finds:

| Bucket | Count |
| --- | ---: |
| Total readable morphology words | 77,432 |
| Words with non-null word-level lemma | 72,507 |
| Words with at least one matching segment lemma id | 72,499 |
| Words with no matching segment lemma id | 8 |
| Legitimate compound/modeling divergence among the 8 | 7 |
| Likely anomaly among the 8 | 1 |
| Ambiguous among the 8 | 0 |

The broader shift heuristic finds:

| Bucket | Count |
| --- | ---: |
| Strict likely shifted word-level lemma rows | 63 |
| Strict rows hidden by matching `segment.lemma_id` | 62 |
| Strict rows visible in no-matching-segment check | 1 |
| Affected lemmas | 26 |
| Affected surahs | 28 |
| Affected ayahs | 61 |
| Distinct current-word/head-lemma patterns | 51 |

The 63-row pattern is visible in the staged source itself. Example:

| Location | QUL lemma | QUL root | Corpus segment lemma |
| --- | --- | --- | --- |
| `2:44:5` (`أَنفُسَكُمْ`) | null | `ن ف س` | `nafos` |
| `2:44:6` (`وَأَنتُمْ`) | `نَفْس` | null | null |

This is not caused by the reader. It is already present in the staged QUL word-level source and then imported into `quran_word_morphology.lemma_id`.

---

## 3. Data Source and Model Explanation

### Word-Level Morphology

`quran_word_morphology` has one head row per readable Quran word. Its `lemma_id` is populated from the staged QUL whole-word file:

- Source file: `resources/import-sources/quran-morphology/qul/word-lemma.json`
- Reader: `JsonQulLemmaReader`
- Import path: `MorphologyImportSource.LoadAsync`
- Assembly logic: `MorphologyAssembler.Assemble`

The assembler creates the lemma lexicon from QUL Arabic lemma text. For each readable location:

- `qulLemma = lemmas[location]`
- if non-blank, the lemma text is added/resolved in `lemmaIndex`
- `AlignedWordDto.LemmaId` receives that QUL-derived assigned id
- the bulk writer persists it as `quran_word_morphology.lemma_id`

So `quran_word_morphology.lemma_id` is intended to represent the **QUL word-level lemma for that word location**. It is not derived from Corpus segment lemmas at read time, and it is not guaranteed to equal the first STEM segment's Corpus lemma.

`head_pos` is derived separately from the first STEM segment:

```csharp
var stemSegment = segments.FirstOrDefault(s =>
    string.Equals(s.Kind, "STEM", StringComparison.Ordinal));
var headPos = stemSegment?.Pos ?? segments.FirstOrDefault()?.Pos ?? string.Empty;
```

### Segment-Level Morphology

`quran_word_morphology_segments.lemma_id` is populated by `MorphologyAssembler.ResolveLemmaId`.

Current behavior:

- Non-STEM segments get `null`.
- Single-STEM words return `wordHeadLemmaId` directly.
- Multi-STEM words use Corpus segment `lemma_buckwalter`, with head-id shortcut, Arabic-form matching, and curated disambiguation.

That means segment `lemma_id` is **not always independent evidence**. In single-STEM words, if the QUL word-level lemma is shifted to the wrong word, the segment can inherit the shifted id even when the Corpus segment has no lemma Buckwalter. This is exactly why 62 of the 63 strict likely anomalies are hidden by the simple segment-id equality check.

### Why Segment and Word Lemmas Can Legitimately Differ

Some multi-STEM compound particles have a QUL whole-word lexical unit while Corpus splits the word into constituent segment lemmas. These should not be treated as automatic errors.

Known legitimate examples in this audit:

- `أَنَّمَآ`: word-level QUL lemma `إِنّ`; segments `أَنّ` + `مَا`
- `إِلَّا`: word-level QUL lemma `إِلَّا`; segments `إِن` + `لَا`

---

## 4. Audit Methodology

The audit used three layers.

1. **Direct segment-id match check**
   - Candidate definition:
     - `quran_word_morphology.lemma_id IS NOT NULL`
     - no segment row for the same `quran_word_id` has `segment.lemma_id = m.lemma_id`
   - This is the exact check requested for "head lemma vs segment lemmas".

2. **Neighbor shift check**
   - For candidates and broader heuristics, compare the current head lemma against previous/next word segment lemma Buckwalter, roots, POS, and morphology.
   - Specifically verify `28:50:10` and `28:50:11`.

3. **Broader strict source-shift heuristic**
   - Current word has a content lemma (`quran_lemmas.root_id IS NOT NULL`).
   - Current word has no word-level root (`m.root_id IS NULL`).
   - All current segments have null `root_id`.
   - Current segments do not carry the head lemma Buckwalter.
   - Previous word has no word-level lemma.
   - Previous word has the same root as the current head lemma.
   - Previous word has a Corpus segment with the same lemma Buckwalter as the current head lemma.

This strict heuristic is conservative. It deliberately ignores broad noisy signals such as "head lemma Buckwalter does not match a current segment Buckwalter" by itself, because that flags thousands of known modeling/normalization cases.

---

## 5. Corpus-Wide Counts

### Direct Segment-Id Match Counts

| Metric | Count |
| --- | ---: |
| Total readable morphology words | 77,432 |
| Words with non-null word-level lemma | 72,507 |
| Words with at least one matching segment lemma id | 72,499 |
| Words with no matching segment lemma id | 8 |

### Classification of the 8 No-Matching-Segment Cases

| Classification | Count |
| --- | ---: |
| A. Legitimate compound/modeling divergence | 7 |
| B. Likely source alignment anomaly | 1 |
| C. Ambiguous human review | 0 |

### Strict Likely Shift Counts

| Metric | Count |
| --- | ---: |
| Strict likely shifted rows | 63 |
| Strict rows where `segment.lemma_id` already matches head lemma | 62 |
| Strict rows with no matching segment lemma id | 1 |
| Affected lemmas | 26 |
| Affected surahs | 28 |
| Affected ayahs | 61 |
| Distinct current-word/head-lemma patterns | 51 |

### Affected Lemmas in the Strict Shift Set

| Head lemma | Count |
| --- | ---: |
| `كَانَ` `[107]` | 17 |
| `ءَامَنَ` `[210]` | 8 |
| `نَفْس` `[460]` | 5 |
| `أَشْرَكَ` `[835]` | 4 |
| `شَىْء` `[1320]` | 4 |
| `حِلّ` `[15855]` | 2 |
| `كَفَىٰ` `[1106]` | 2 |
| `نَار` `[96]` | 2 |
| `وَلِىّ` `[1523]` | 2 |
| 17 other lemmas | 1 each |

Top current-word/head-lemma patterns:

| Pattern | Count |
| --- | ---: |
| `بِهِۦ -> كَانَ [107]` | 4 |
| `لَنَآ -> كَانَ [107]` | 3 |
| `بِهِۦ ۖ -> ءَامَنَ [210]` | 2 |
| `بِهِۦ -> أَشْرَكَ [835]` | 2 |
| `بِى -> أَشْرَكَ [835]` | 2 |
| `لَكُمْ -> ءَامَنَ [210]` | 2 |
| `لَهُمْ -> كَانَ [107]` | 2 |
| `وَهُمْ -> نَفْس [460]` | 2 |
| `وَهُوَ -> شَىْء [1320]` | 2 |
| `مِمَّنِ -> أَضَلّ [5942]` | 1 |

---

## 6. No-Matching-Segment Cases

These are the 8 rows where `m.lemma_id` has no matching `s.lemma_id` for the same word.

| quran_word_id | Location | Text | Simple | Head lemma | BW | head_pos | Label | Segment lemmas | Classification | Reason |
| ---: | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 24120 | `8:28:2` | `أَنَّمَآ` | `انما` | `إِنّ [11]` | `<in~` | ACC | حرف نصب | `أَنّ[250]/>an~`; `مَا[4]/maA` | A | QUL whole-particle lemma differs from Corpus constituents |
| 24967 | `8:73:6` | `إِلَّا` | `الا` | `إِلَّا [205]` | `<il~aA` | COND | حرف شرط | `إِن[541]/<in`; `لَا[77]/laA` | A | QUL compound lexical unit differs from Corpus split |
| 29823 | `11:14:5` | `أَنَّمَآ` | `انما` | `إِنّ [11]` | `<in~` | ACC | حرف نصب | `أَنّ[250]/>an~`; `مَا[4]/maA` | A | QUL whole-particle lemma differs from Corpus constituents |
| 41358 | `18:110:8` | `أَنَّمَآ` | `انما` | `إِنّ [11]` | `<in~` | ACC | حرف نصب | `أَنّ[250]/>an~`; `مَا[4]/maA` | A | QUL whole-particle lemma differs from Corpus constituents |
| 45135 | `21:108:5` | `أَنَّمَآ` | `انما` | `إِنّ [11]` | `<in~` | ACC | حرف نصب | `أَنّ[250]/>an~`; `مَا[4]/maA` | A | QUL whole-particle lemma differs from Corpus constituents |
| 53708 | `28:50:11` | `مِمَّنِ` | `ممن` | `أَضَلّ [5942]` | `>aDal~` | P | حرف جر | `مِن[130]/min`; `مِن[130]/man` | B | Head lemma belongs to previous word `أَضَلُّ` |
| 62917 | `38:70:5` | `أَنَّمَآ` | `انما` | `إِنّ [11]` | `<in~` | ACC | حرف نصب | `أَنّ[250]/>an~`; `مَا[4]/maA` | A | QUL whole-particle lemma differs from Corpus constituents |
| 65654 | `41:6:8` | `أَنَّمَآ` | `انما` | `إِنّ [11]` | `<in~` | ACC | حرف نصب | `أَنّ[250]/>an~`; `مَا[4]/maA` | A | QUL whole-particle lemma differs from Corpus constituents |

Affected no-match lemmas:

| Lemma | Count |
| --- | ---: |
| `إِنّ [11]` | 6 |
| `إِلَّا [205]` | 1 |
| `أَضَلّ [5942]` | 1 |

Affected no-match ayahs: `8:28`, `8:73`, `11:14`, `18:110`, `21:108`, `28:50`, `38:70`, `41:6`.

---

## 7. Suspicious Anomaly Analysis

### Narrow Anomaly Visible in the No-Match Set

`28:50:11` is a source alignment anomaly:

| Field | Value |
| --- | --- |
| Current location | `28:50:11` |
| Current word | `مِمَّنِ` |
| Current segments | `P/min -> مِن [130]`; `REL/man -> مِن [130]` |
| Current word-level lemma | `أَضَلّ [5942]` |
| Previous location | `28:50:10` |
| Previous word | `أَضَلُّ` |
| Previous root | `ض ل ل` |
| Previous Corpus segment lemma Buckwalter | `>aDal~` |

The staged source confirms the shift:

| Location | QUL lemma | QUL root | QUL stem | Corpus segment |
| --- | --- | --- | --- | --- |
| `28:50:10` | null | `ض ل ل` | `أَضَلُّ` | `N/>aDal~` |
| `28:50:11` | `أَضَلّ` | null | `مِ` | `P/min`; `REL/man` |

### Broader Strict Likely Shift Set

The same pattern occurs beyond `28:50:11`. These 63 rows are likely word-level source alignment shifts. The current word is usually a pronoun or particle/prepositional phrase, while the head lemma belongs to the previous content word.

| quran_word_id | Location | Current word | Head lemma | head_pos | Current segments | Previous word | Previous root | Previous segments |
| ---: | --- | --- | --- | --- | --- | --- | --- | --- |
| 718 | `2:44:6` | `وَأَنتُمْ` | `نَفْس [460]` | PRON | `CIRC:-`; `PRON:نَفْس[460]/-` | `أَنفُسَكُمْ` | `ن ف س` | `N:nafos`; `PRON:-` |
| 1281 | `2:75:4` | `لَكُمْ` | `ءَامَنَ [210]` | PRON | `P:-`; `PRON:ءَامَنَ[210]/-` | `يُؤْمِنُوا۟` | `ا م ن` | `V:'aAmana`; `PRON:-` |
| 2146 | `2:114:17` | `لَهُمْ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `كَانَ` | `ك و ن` | `V:kaAna` |
| 4310 | `2:216:11` | `وَهُوَ` | `شَىْء [1320]` | PRON | `CIRC:-`; `PRON:شَىْء[1320]/-` | `شَيْـًۭٔا` | `ش ي ا` | `N:$aYo'` |
| 4599 | `2:228:8` | `لَهُنَّ` | `حَلَلْ [2360]` | PRON | `P:-`; `PRON:حَلَلْ[2360]/-` | `يَحِلُّ` | `ح ل ل` | `V:Halalo` |
| 5184 | `2:247:14` | `لَهُ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `يَكُونُ` | `ك و ن` | `V:kaAna` |
| 7041 | `3:33:7` | `إِبْرَٰهِيمَ` | `ءَال [3643]` | PN | `PN:ءَال[3643]/<iboraAhiym` | `وَءَالَ` | `ا و ل` | `CONJ:-`; `N:'aAl` |
| 9666 | `3:178:15` | `وَلَهُمْ` | `إِثْم [10381]` | PRON | `REM:-`; `P:-`; `PRON:إِثْم[10381]/-` | `إِثْمًۭا` | `ا ث م` | `N:<ivom` |
| 10410 | `4:12:9` | `لَّهُنَّ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `يَكُن` | `ك و ن` | `V:kaAna` |
| 11053 | `4:36:5` | `بِهِۦ` | `أَشْرَكَ [835]` | PRON | `P:-`; `PRON:أَشْرَكَ[835]/-` | `تُشْرِكُوا۟` | `ش ر ك` | `V:>a$oraka`; `PRON:-` |
| 11362 | `4:50:8` | `بِهِۦٓ` | `كَفَىٰ [1106]` | PRON | `P:-`; `PRON:كَفَىٰ[1106]/-` | `وَكَفَىٰ` | `ك ف ي` | `REM:-`; V:kafaY\` |
| 12507 | `4:101:21` | `لَكُمْ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `كَانُوا۟` | `ك و ن` | `V:kaAna`; `PRON:-` |
| 14224 | `5:5:10` | `لَّكُمْ` | `حِلّ [15855]` | PRON | `P:-`; `PRON:حِلّ[15855]/-` | `حِلٌّۭ` | `ح ل ل` | `N:Hil~` |
| 14227 | `5:5:13` | `لَّهُمْ ۖ` | `حِلّ [15855]` | PRON | `P:-`; `PRON:حِلّ[15855]/-` | `حِلٌّۭ` | `ح ل ل` | `N:Hil~` |
| 16606 | `5:106:38` | `بِهِۦ` | `اشْتَرَىٰ [3170]` | PRON | `P:-`; `PRON:اشْتَرَىٰ[3170]/-` | `نَشْتَرِى` | `ش ر ي` | V:{$otaraY\` |
| 16863 | `5:116:20` | `لِىٓ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `يَكُونُ` | `ك و ن` | `V:kaAna` |
| 17033 | `6:5:11` | `بِهِۦ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `كَانُوا۟` | `ك و ن` | `V:kaAna`; `PRON:-` |
| 17289 | `6:20:11` | `فَهُمْ` | `نَفْس [460]` | PRON | `RSLT:-`; `PRON:نَفْس[460]/-` | `أَنفُسَهُمْ` | `ن ف س` | `N:nafos`; `PRON:-` |
| 18615 | `6:92:18` | `بِهِۦ ۖ` | `ءَامَنَ [210]` | PRON | `P:-`; `PRON:ءَامَنَ[210]/-` | `يُؤْمِنُونَ` | `ا م ن` | `V:'aAmana`; `PRON:-` |
| 18833 | `6:101:10` | `لَّهُۥ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `تَكُن` | `ك و ن` | `V:kaAna` |
| 21731 | `7:89:17` | `لَنَآ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `يَكُونُ` | `ك و ن` | `V:kaAna` |
| 21789 | `7:92:12` | `هُمُ` | `كَانَ [107]` | PRON | `PRON:كَانَ[107]/-` | `كَانُوا۟` | `ك و ن` | `V:kaAna`; `PRON:-` |
| 22840 | `7:157:33` | `بِهِۦ` | `ءَامَنَ [210]` | PRON | `P:-`; `PRON:ءَامَنَ[210]/-` | `ءَامَنُوا۟` | `ا م ن` | `V:'aAmana`; `PRON:-` |
| 25330 | `9:17:17` | `هُمْ` | `نَار [96]` | PRON | `PRON:نَار[96]/-` | `ٱلنَّارِ` | `ن و ر` | `DET:-`; `N:naAr` |
| 25758 | `9:37:7` | `بِهِ` | `ضَلَّ [614]` | PRON | `P:-`; `PRON:ضَلَّ[614]/-` | `يُضَلُّ` | `ض ل ل` | `V:Dal~a` |
| 26131 | `9:55:16` | `وَهُمْ` | `نَفْس [460]` | PRON | `CIRC:-`; `PRON:نَفْس[460]/-` | `أَنفُسُهُمْ` | `ن ف س` | `N:nafos`; `PRON:-` |
| 26723 | `9:85:15` | `وَهُمْ` | `نَفْس [460]` | PRON | `CIRC:-`; `PRON:نَفْس[460]/-` | `أَنفُسُهُمْ` | `ن ف س` | `N:nafos`; `PRON:-` |
| 26885 | `9:94:11` | `لَكُمْ` | `ءَامَنَ [210]` | PRON | `P:-`; `PRON:ءَامَنَ[210]/-` | `نُّؤْمِنَ` | `ا م ن` | `V:'aAmana` |
| 29732 | `11:8:21` | `بِهِۦ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `كَانُوا۟` | `ك و ن` | `V:kaAna`; `PRON:-` |
| 29885 | `11:17:18` | `بِهِۦ ۚ` | `ءَامَنَ [210]` | PRON | `P:-`; `PRON:ءَامَنَ[210]/-` | `يُؤْمِنُونَ` | `ا م ن` | `V:'aAmana`; `PRON:-` |
| 31853 | `12:17:15` | `لَّنَا` | `مُؤْمِن [516]` | PRON | `P:-`; `PRON:مُؤْمِن[516]/-` | `بِمُؤْمِنٍۢ` | `ا م ن` | `P:-`; `N:mu&omin` |
| 31975 | `12:24:5` | `بِهَا` | `هَمَّ [3214]` | PRON | `P:-`; `PRON:هَمَّ[3214]/-` | `وَهَمَّ` | `ه م م` | `CONJ:-`; `V:ham~a` |
| 32247 | `12:38:9` | `لَنَآ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `كَانَ` | `ك و ن` | `V:kaAna` |
| 32684 | `12:64:15` | `وَهُوَ` | `حَٰفِظ [3081]` | PRON | `CONJ:-`; `PRON:حَٰفِظ[3081]/-` | `حَـٰفِظًۭا ۖ` | `ح ف ظ` | `N:Ha`fiZ` |
| 34646 | `14:11:19` | `لَنَآ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `كَانَ` | `ك و ن` | `V:kaAna` |
| 36865 | `16:60:10` | `وَهُوَ` | `أَعْلَىٰ [5193]` | PRON | `CONJ:-`; `PRON:أَعْلَىٰ[5193]/-` | `ٱلْأَعْلَىٰ ۚ` | `ع ل و` | `DET:-`; N:>aEolaY\` |
| 39004 | `17:66:13` | `بِكُمْ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `كَانَ` | `ك و ن` | `V:kaAna` |
| 39651 | `17:110:10` | `فَلَهُ` | `دَعَا [568]` | PRON | `REM:-`; `P:-`; `PRON:دَعَا[568]/-` | `تَدْعُوا۟` | `د ع و` | `V:daEaA`; `PRON:-` |
| 42277 | `19:79:6` | `لَهُۥ` | `مَدَّ [4139]` | PRON | `P:-`; `PRON:مَدَّ[4139]/-` | `وَنَمُدُّ` | `م د د` | `CONJ:-`; `V:mad~a` |
| 43482 | `20:101:4` | `لَهُمْ` | `سَآءَ [2737]` | PRON | `P:-`; `PRON:سَآءَ[2737]/-` | `وَسَآءَ` | `س و ا` | `CONJ:-`; `V:saA^'a` |
| 44506 | `21:51:3` | `إِبْرَٰهِيمَ` | `آتَى [1737]` | PN | `PN:آتَى[1737]/<iboraAhiym` | `ءَاتَيْنَآ` | `ا ت ي` | `V:A^taY`; `PRON:-` |
| 44511 | `21:51:8` | `بِهِۦ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `وَكُنَّا` | `ك و ن` | `CONJ:-`; `V:kaAna`; `PRON:-` |
| 44716 | `21:73:14` | `لَنَا` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `وَكَانُوا۟` | `ك و ن` | `CONJ:-`; `V:kaAna`; `PRON:-` |
| 48793 | `24:55:30` | `بِى` | `أَشْرَكَ [835]` | PRON | `P:-`; `PRON:أَشْرَكَ[835]/-` | `يُشْرِكُونَ` | `ش ر ك` | `V:>a$oraka`; `PRON:-` |
| 49290 | `25:15:11` | `لَهُمْ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `كَانَتْ` | `ك و ن` | `V:kaAna` |
| 49822 | `25:58:10` | `بِهِۦ` | `كَفَىٰ [1106]` | PRON | `P:-`; `PRON:كَفَىٰ[1106]/-` | `وَكَفَىٰ` | `ك ف ي` | `CONJ:-`; V:kafaY\` |
| 49842 | `25:59:16` | `بِهِۦ` | `سَأَلَ [192]` | PRON | `P:-`; `PRON:سَأَلَ[192]/-` | `فَسْـَٔلْ` | `س ا ل` | `REM:-`; `V:sa>ala` |
| 50830 | `26:111:3` | `لَكَ` | `ءَامَنَ [210]` | PRON | `P:-`; `PRON:ءَامَنَ[210]/-` | `أَنُؤْمِنُ` | `ا م ن` | `INTG:-`; `V:'aAmana` |
| 53708 | `28:50:11` | `مِمَّنِ` | `أَضَلّ [5942]` | P | `P:مِن[130]/min`; `REL:مِن[130]/man` | `أَضَلُّ` | `ض ل ل` | `N:>aDal~` |
| 53807 | `28:57:11` | `لَّهُمْ` | `مَكَّ [3533]` | PRON | `P:-`; `PRON:مَكَّ[3533]/-` | `نُمَكِّن` | `م ك ن` | `V:mak~a` |
| 54443 | `29:8:8` | `بِى` | `أَشْرَكَ [835]` | PRON | `P:-`; `PRON:أَشْرَكَ[835]/-` | `لِتُشْرِكَ` | `ش ر ك` | `PRP:-`; `V:>a$oraka` |
| 54741 | `29:26:2` | `لَهُۥ` | `ءَامَنَ [210]` | PRON | `P:-`; `PRON:ءَامَنَ[210]/-` | `۞ فَـَٔامَنَ` | `ا م ن` | `CONJ:-`; `V:'aAmana` |
| 55090 | `29:47:9` | `بِهِۦ ۖ` | `ءَامَنَ [210]` | PRON | `P:-`; `PRON:ءَامَنَ[210]/-` | `يُؤْمِنُونَ` | `ا م ن` | `V:'aAmana`; `PRON:-` |
| 55193 | `29:53:10` | `وَهُمْ` | `بَغْتَة [3010]` | PRON | `CIRC:-`; `PRON:بَغْتَة[3010]/-` | `بَغْتَةًۭ` | `ب غ ت` | `N:bagotap` |
| 59327 | `34:39:16` | `فَهُوَ` | `شَىْء [1320]` | PRON | `REM:-`; `PRON:شَىْء[1320]/-` | `شَىْءٍۢ` | `ش ي ا` | `N:$aYo'` |
| 60655 | `36:30:10` | `بِهِۦ` | `كَانَ [107]` | PRON | `P:-`; `PRON:كَانَ[107]/-` | `كَانُوا۟` | `ك و ن` | `V:kaAna`; `PRON:-` |
| 64470 | `40:12:10` | `بِهِۦ` | `أَشْرَكَ [835]` | PRON | `P:-`; `PRON:أَشْرَكَ[835]/-` | `يُشْرَكْ` | `ش ر ك` | `V:>a$oraka` |
| 65886 | `41:21:13` | `وَهُوَ` | `شَىْء [1320]` | PRON | `CONJ:-`; `PRON:شَىْء[1320]/-` | `شَىْءٍۢ` | `ش ي ا` | `N:$aYo'` |
| 66049 | `41:31:13` | `وَلَكُمْ` | `نَفْس [460]` | PRON | `CONJ:-`; `P:-`; `PRON:نَفْس[460]/-` | `أَنفُسُكُمْ` | `ن ف س` | `N:nafos`; `PRON:-` |
| 66564 | `42:9:9` | `وَهُوَ` | `وَلِىّ [1523]` | PRON | `CONJ:-`; `PRON:وَلِىّ[1523]/-` | `ٱلْوَلِىُّ` | `و ل ي` | `DET:-`; `N:waliY~` |
| 68799 | `45:10:17` | `وَلَهُمْ` | `وَلِىّ [1523]` | PRON | `CONJ:-`; `P:-`; `PRON:وَلِىّ[1523]/-` | `أَوْلِيَآءَ ۖ` | `و ل ي` | `N:waliY~` |
| 69327 | `46:8:13` | `هُوَ` | `شَىْء [1320]` | PRON | `PRON:شَىْء[1320]/-` | `شَيْـًٔا ۖ` | `ش ي ا` | `N:$aYo'` |
| 74622 | `57:15:12` | `هِىَ` | `نَار [96]` | PRON | `PRON:نَار[96]/-` | `ٱلنَّارُ ۖ` | `ن و ر` | `DET:-`; `N:naAr` |

---

## 8. Neighbor Shift Analysis

### Specific Verification: `28:50:10` / `28:50:11`

DB rows:

| Location | Word | word root | word lemma | stem | Corpus segments |
| --- | --- | --- | --- | --- | --- |
| `28:50:10` | `أَضَلُّ` | `ض ل ل` | null | `أَضَلُّ` | `N/>aDal~` |
| `28:50:11` | `مِمَّنِ` | null | `أَضَلّ [5942]` | `مِ` | `P/min`; `REL/man` |

Source rows:

| Location | QUL lemma | QUL root | QUL stem |
| --- | --- | --- | --- |
| `28:50:10` | null | `ض ل ل` | `أَضَلُّ` |
| `28:50:11` | `أَضَلّ` | null | `مِ` |

Conclusion: **yes, `أَضَلّ` belongs to `28:50:10` and is shifted onto `28:50:11` in the QUL word-level lemma source.**

### Broader Shift Signature

All 63 strict likely anomalies match the previous-word direction. None match the next-word direction under the strict heuristic.

| Direction | Count |
| --- | ---: |
| Previous word carries matching root and Corpus lemma Buckwalter | 63 |
| Next word carries matching root and Corpus lemma Buckwalter | 0 |

This suggests a systematic QUL word-lemma alignment offset pattern in some rows where the content word has root/stem but no QUL lemma, and the following rootless pronoun/particle receives that lemma.

---

## 9. Legitimate Compound / Modeling Divergences

| Pattern | Examples | Why legitimate | Diagnostic allow-list? |
| --- | --- | --- | --- |
| `أَنَّمَآ -> إِنّ` | 6 occurrences: `8:28:2`, `11:14:5`, `18:110:8`, `21:108:5`, `38:70:5`, `41:6:8` | QUL assigns a whole-particle/family lemma; Corpus segments as `أَنّ + مَا` | Yes, allow-list for diagnostics only |
| `إِلَّا -> إِلَّا` | 1 occurrence: `8:73:6` | QUL treats `إِلَّا` as one lexical unit; Corpus splits as `إِن + لَا` | Yes, allow-list for diagnostics only |

These should not fail import. They should be explicitly allowed in a diagnostic report so real anomalies remain visible.

---

## 10. Impact on Current/Future UI

### Lemmas Explorer Occurrence Sets

High impact. Lemmas Explorer counts, list rows, words, ayahs, surahs, missing-surah views, stems relationships, and type distribution all start from `quran_word_morphology.lemma_id`.

For the strict 63 shifted rows:

- the previous content word is missing from its expected lemma occurrence set,
- the following pronoun/particle/current word is incorrectly included,
- 62 rows are not caught by `segment.lemma_id` equality because importer inheritance copied the shifted lemma onto the current segment.

### Lemma Details Type Distribution

The proposed segment-matched type fix remains technically valid for multi-STEM type labeling, but it does **not** correct the occurrence set. For the 62 single-STEM inherited-shift rows, segment-matched POS can still classify the wrong word because the segment id was copied from the shifted word-level lemma.

### Lemma Ayah Filtering

Affected. `GetLemmaAyahMatchesAsync` filters by `m.LemmaId == id`; shifted rows can cause:

- false-positive ayahs/words for the shifted lemma,
- false-negative omission of the previous true content word when its `m.lemma_id` is null.

### Roots Explorer

Roots Explorer uses `m.RootId`, not `m.LemmaId`, for root occurrence sets. The strict shift pattern shows the previous content word usually has the correct root, so root occurrence sets are less affected. However, related lemma summaries or any future root detail that uses word-level lemma as a related dimension can inherit shifted lemma metadata.

### Stems Explorer

Stems Explorer uses word-level `m.StemId` for occurrence sets. The strict shift pattern usually leaves the content word's stem on the previous word and a pronoun/particle stem on the current shifted word. Stem occurrence sets are less directly affected than lemmas, but dominant lemma/root relationships in `EfStemsReader.Summary` read `m.LemmaId` and can be polluted by shifted lemma rows.

### Word Analysis Panel

Affected. `EfWordAnalysisReader` exposes `WordMorphologyDto.Lemma` from `m.LemmaId`. For strict shifted rows, the word analysis panel can show a content lemma on the following pronoun/particle/current word.

### API/DTO Responses Exposing Word-Level Lemma

Affected response surfaces include:

- Lemmas Explorer list/summary/detail DTOs derived from `EfLemmasReader`.
- `GET /api/words/lemmas/{id}/ayahs` and matching/highlight behavior.
- Lemma words/surahs/missing-surahs/stems relationships.
- Stems Explorer dominant/related lemma metadata.
- Mushaf word analysis `WordMorphologyDto.Lemma`.
- Any Unique Words list enrichment that reads `m.LemmaId`.

---

## 11. Validation and Correction Recommendations

### Should We Add a New Hard Validation Check?

Not immediately as a hard import blocker.

Reasons:

- A naive "head lemma must appear on a segment" check would falsely fail legitimate compound divergences (`أَنَّمَآ`, `إِلَّا`).
- The simple segment-id check misses 62 of 63 strict likely shifts.
- A broad Buckwalter mismatch check is too noisy; it flags thousands of normalization/modeling cases.

Recommended now:

1. Add a **diagnostic warning/report** for:
   - no matching segment lemma id, excluding an allow-list,
   - strict previous-word shift signature,
   - current rootless/pronominal segment inheriting a content lemma whose previous word has the matching root and Corpus lemma Buckwalter.
2. Keep a small allow-list for legitimate compound divergences.
3. After curated corrections are applied and the baseline is clean, promote the strict shift diagnostic to a hard validation check.

### Should Suspicious Anomalies Fail Import?

Current state: report only / warning only.

After correction: yes, fail import on newly introduced strict source-shift rows unless they are explicitly allow-listed with a reason.

### Should wid 53708 Be Fixed?

Yes. But wid `53708` is one member of a broader 63-row strict shift set. Fixing only `53708` would leave the same class of problem in Lemmas Explorer occurrence sets.

### Where Should Corrections Live?

Recommended order:

1. **Source staging correction overlay** — best option.
   - Keep staged upstream source immutable.
   - Add an auditable curated correction map for QUL word-level lemma alignment.
   - Apply it during morphology import before assigning `quran_word_morphology.lemma_id`.
2. **Importer curated map** — acceptable if represented as data/config and documented in import reports.
3. **Post-import correction** — not recommended; lower traceability and easier to drift.
4. **Reader-layer exclusion** — not recommended; hides source problems, duplicates logic across APIs, and leaves stored data wrong.

---

## 12. Open Questions

1. Should the 63 strict likely shifted rows all be corrected as a batch, or should a human linguist review each before any correction map is created?
2. Should legitimate compound divergences be represented as an explicit allow-list table/config so diagnostics remain stable?
3. Should `quran_word_morphology_segments.lemma_id` stop inheriting word head lemma for single-STEM segments when the Corpus segment has no lemma Buckwalter, or should that remain for existing reader behavior and be handled by a separate diagnostic?
4. Should the correction map adjust only word-level `lemma_id`, or also segment-level inherited `lemma_id` on the shifted target word?
5. Should QUL source files be re-staged from an upstream corrected source if available, instead of maintaining local corrections?

---

## 13. Final Recommendation

### **BLOCKED**

Do not treat current `quran_word_morphology.lemma_id` as fully reliable for Lemmas Explorer occurrence sets yet.

Proceed with the Lemma Details segment-matched type fix only as a reader implementation improvement, not as a final data-quality resolution. Before shipping/closing the larger Lemmas Explorer correctness story, create a curated word-level lemma alignment correction plan for the 63 strict likely shifted rows, rerun the diagnostics, and then decide whether to promote the strict shift check to a hard import validation.

---

## 14. Appendix: SQL Used

All commands below are SELECT-only. The local socket invocation and credentials are omitted.

### A. Corpus Counts

```sql
SELECT 'total_readable_morphology_words' AS metric, COUNT(*)
FROM quran_word_morphology m
JOIN quran_words w ON w.id=m.quran_word_id
WHERE NOT w.is_ayah_marker
UNION ALL
SELECT 'words_with_non_null_word_lemma', COUNT(*)
FROM quran_word_morphology m
JOIN quran_words w ON w.id=m.quran_word_id
WHERE NOT w.is_ayah_marker AND m.lemma_id IS NOT NULL
UNION ALL
SELECT 'words_with_at_least_one_matching_segment_lemma', COUNT(*)
FROM quran_word_morphology m
JOIN quran_words w ON w.id=m.quran_word_id
WHERE NOT w.is_ayah_marker
  AND m.lemma_id IS NOT NULL
  AND EXISTS (
    SELECT 1
    FROM quran_word_morphology_segments s
    WHERE s.quran_word_id=m.quran_word_id
      AND s.lemma_id=m.lemma_id)
UNION ALL
SELECT 'words_with_no_matching_segment_lemma', COUNT(*)
FROM quran_word_morphology m
JOIN quran_words w ON w.id=m.quran_word_id
WHERE NOT w.is_ayah_marker
  AND m.lemma_id IS NOT NULL
  AND NOT EXISTS (
    SELECT 1
    FROM quran_word_morphology_segments s
    WHERE s.quran_word_id=m.quran_word_id
      AND s.lemma_id=m.lemma_id);
```

### B. No-Matching-Segment Rows

```sql
WITH bad AS (
  SELECT m.quran_word_id, m.lemma_id
  FROM quran_word_morphology m
  JOIN quran_words w ON w.id=m.quran_word_id
  WHERE NOT w.is_ayah_marker
    AND m.lemma_id IS NOT NULL
    AND NOT EXISTS (
      SELECT 1
      FROM quran_word_morphology_segments s
      WHERE s.quran_word_id=m.quran_word_id
        AND s.lemma_id=m.lemma_id))
SELECT m.quran_word_id,
       w.surah_number||':'||w.ayah_number||':'||w.word_number AS loc,
       w.text_uthmani,
       w.text_imlaei_simple,
       m.lemma_id AS head_lemma_id,
       l.lemma_text AS head_lemma,
       l.lemma_buckwalter AS head_lemma_bw,
       m.head_pos,
       pt.arabic_label AS head_pos_arabic_label,
       m.segment_count,
       (
         SELECT string_agg(
           s.segment_number||':'||s.kind||':'||s.pos||':'||
           coalesce(sl.lemma_text,'-')||'['||coalesce(s.lemma_id::text,'null')||']/'||
           coalesce(s.lemma_buckwalter,'-'),
           ' | ' ORDER BY s.segment_number)
         FROM quran_word_morphology_segments s
         LEFT JOIN quran_lemmas sl ON sl.id=s.lemma_id
         WHERE s.quran_word_id=m.quran_word_id
       ) AS segment_lemmas
FROM bad b
JOIN quran_word_morphology m ON m.quran_word_id=b.quran_word_id
JOIN quran_words w ON w.id=m.quran_word_id
JOIN quran_lemmas l ON l.id=m.lemma_id
LEFT JOIN quran_pos_tags pt ON pt.code=m.head_pos
ORDER BY w.surah_number,w.ayah_number,w.word_number;
```

### C. Neighbor Verification for `28:50`

```sql
SELECT w.id,
       w.surah_number||':'||w.ayah_number||':'||w.word_number loc,
       w.text_uthmani,
       w.text_imlaei_simple,
       m.head_pos,
       m.root_id,
       wr.root_text,
       m.lemma_id,
       wl.lemma_text,
       m.stem_id
FROM quran_words w
LEFT JOIN quran_word_morphology m ON m.quran_word_id=w.id
LEFT JOIN quran_roots wr ON wr.id=m.root_id
LEFT JOIN quran_lemmas wl ON wl.id=m.lemma_id
WHERE w.surah_number=28
  AND w.ayah_number=50
  AND w.word_number BETWEEN 9 AND 12
ORDER BY w.word_number;

SELECT s.quran_word_id,
       w.surah_number||':'||w.ayah_number||':'||w.word_number loc,
       s.segment_number,
       s.kind,
       s.pos,
       s.form_buckwalter,
       s.form_arabic_normalized,
       s.root_buckwalter,
       s.lemma_buckwalter,
       s.root_id,
       s.lemma_id,
       sl.lemma_text
FROM quran_word_morphology_segments s
JOIN quran_words w ON w.id=s.quran_word_id
LEFT JOIN quran_lemmas sl ON sl.id=s.lemma_id
WHERE w.surah_number=28
  AND w.ayah_number=50
  AND w.word_number BETWEEN 9 AND 12
ORDER BY w.word_number,s.segment_number;
```

### D. Strict Shift Signature

```sql
WITH rows AS (
  SELECT m.quran_word_id,
         w.surah_number,
         w.ayah_number,
         w.word_number,
         w.text_uthmani,
         w.text_imlaei_simple,
         m.head_pos,
         m.root_id AS word_root_id,
         m.lemma_id,
         l.lemma_text,
         l.lemma_buckwalter,
         l.root_id AS lemma_root_id,
         lr.root_text AS lemma_root_text,
         (
           SELECT bool_or(s.lemma_id=m.lemma_id)
           FROM quran_word_morphology_segments s
           WHERE s.quran_word_id=m.quran_word_id
         ) AS current_seg_id_matches_head,
         (
           SELECT bool_or(s.lemma_buckwalter=l.lemma_buckwalter)
           FROM quran_word_morphology_segments s
           WHERE s.quran_word_id=m.quran_word_id
         ) AS current_seg_bw_matches_head,
         (
           SELECT bool_and(s.root_id IS NULL)
           FROM quran_word_morphology_segments s
           WHERE s.quran_word_id=m.quran_word_id
         ) AS all_current_segment_roots_null,
         pw.id AS prev_id,
         pm.root_id AS prev_root_id,
         pm.lemma_id AS prev_lemma_id,
         EXISTS (
           SELECT 1
           FROM quran_word_morphology_segments ps
           WHERE ps.quran_word_id=pw.id
             AND ps.lemma_buckwalter=l.lemma_buckwalter
         ) AS prev_content_word_matches_head
  FROM quran_word_morphology m
  JOIN quran_words w ON w.id=m.quran_word_id
  JOIN quran_lemmas l ON l.id=m.lemma_id
  LEFT JOIN quran_roots lr ON lr.id=l.root_id
  LEFT JOIN quran_words pw
    ON pw.surah_number=w.surah_number
   AND pw.ayah_number=w.ayah_number
   AND pw.word_number=w.word_number-1
  LEFT JOIN quran_word_morphology pm ON pm.quran_word_id=pw.id
  WHERE NOT w.is_ayah_marker
    AND m.lemma_id IS NOT NULL
),
strict AS (
  SELECT *
  FROM rows
  WHERE lemma_root_id IS NOT NULL
    AND word_root_id IS NULL
    AND all_current_segment_roots_null
    AND coalesce(current_seg_bw_matches_head,false)=false
    AND prev_lemma_id IS NULL
    AND prev_root_id=lemma_root_id
    AND prev_content_word_matches_head
)
SELECT 'strict_likely_anomaly_count', count(*) FROM strict
UNION ALL
SELECT 'strict_matching_segment_id_count', count(*) FROM strict WHERE current_seg_id_matches_head
UNION ALL
SELECT 'strict_no_matching_segment_id_count', count(*) FROM strict WHERE NOT current_seg_id_matches_head
UNION ALL
SELECT 'strict_affected_lemmas', count(DISTINCT lemma_id) FROM strict
UNION ALL
SELECT 'strict_affected_surahs', count(DISTINCT surah_number) FROM strict
UNION ALL
SELECT 'strict_affected_ayahs', count(DISTINCT surah_number||':'||ayah_number) FROM strict;
```

### E. Source Spot-Check Script

```python
import json
from pathlib import Path

base = Path("resources/import-sources/quran-morphology")
lemma = json.loads((base / "qul/word-lemma.json").read_text())
root = json.loads((base / "qul/word-root.json").read_text())
stem = json.loads((base / "qul/word-stem-corrected-arabic.json").read_text())
corpus = json.loads((base / "corpus/quranic-corpus-morphology-qpc-aligned.json").read_text())

for loc in ["28:50:9", "28:50:10", "28:50:11", "28:50:12"]:
    print(loc, lemma.get(loc), root.get(loc), stem.get(loc), corpus[loc]["segments"])
```
