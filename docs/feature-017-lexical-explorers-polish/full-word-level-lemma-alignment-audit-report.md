# Full Word-Level Lemma Alignment Audit Report

**Project:** Quran Dashboard / المنهج القرآني  
**Feature:** 017 — Lexical Explorers Polish  
**Branch:** `017-lexical-explorers-polish`  
**Task type:** REPORT ONLY — no production code, importer code, frontend code, source files, database, draft overlay, reset/import commands, or commits changed.  
**Date:** 2026-06-27

## 1. Executive Summary

**Acceptance verdict: `BLOCKED — broader source alignment issue exists beyond the 63`.**

The 63 curated strict previous-word shifts are real, but they are not the complete word-level lemma alignment problem. A broader source-side pass found 59 additional previous-word shift candidates outside the 63, including cases where the shifted lemma lands on another content word rather than a rootless pronoun/particle. No confirmed next-word shift pattern was found.

The current 63-entry draft overlay is not safe as-is. Three remove locations should not become null: `3:33:7` and `21:51:3` should recover `إِبْرَاهِيم`, and `28:50:11` should recover `مِن`. The importer correction plan should be revised before implementation, and the broader 59 candidates need curation or explicit scoping before any production overlay is applied.

## 2. Source Files Inspected

- `resources/import-sources/quran-morphology/manifest.json`
- `resources/import-sources/quran-morphology/qul/word-lemma.json`
- `resources/import-sources/quran-morphology/qul/word-root.json`
- `resources/import-sources/quran-morphology/qul/word-stem-corrected-arabic.json`
- `resources/import-sources/quran-morphology/corpus/quranic-corpus-morphology-qpc-aligned.json`
- `resources/import-sources/quran-morphology/corpus/corpus-qpc-location-alignment-map.json`
- `docs/feature-017-lexical-explorers-polish/word-level-lemma-alignment-corrections.draft.json`
- Existing Feature 017 alignment audit, curation, and correction-plan reports listed in the request

No database query was required for this pass. The audit used staged source JSON only, plus the existing draft curation file as the 63-row review set.

## 3. Methodology

1. Loaded all 77,432 aligned readable Corpus/QPC word records and QUL word-level lemma/root/stem maps.
2. Built same-word Corpus Buckwalter lemma to QUL Arabic lemma mappings from existing QUL lemma assignments, excluding the 63 known remove locations so they could not train their own wrong assignment.
3. Treated a Buckwalter-to-Arabic mapping as reliable when it was unique or dominant (`>= 5` examples and `>= 80%` share). Ambiguous mappings were not used for automatic recovery.
4. Rechecked all 63 draft entries against raw QUL lemma presence, remove-location raw lemma, target Corpus lemma evidence, remove-location Corpus evidence, and own-lemma recovery candidates.
5. Searched all QUL-present words for same-word support, previous-word shifts, next-word shifts, unsupported same-word evidence, and no-Corpus-evidence cases.
6. Searched all QUL-missing words for valid-null, shifted-target, reliable recovery, and uncertain cases.
7. Reviewed multi-STEM words separately to avoid false positives from compound particles and split Corpus behavior.

## 4. Global Counts

| Metric | Count |
| --- | ---: |
| Readable aligned Corpus/QPC words | 77432 |
| QUL word-level lemma entries | 72507 |
| QUL word-level root entries | 50298 |
| QUL corrected stem entries | 77432 |
| Draft 63 correction entries | 63 |
| Reliable Buckwalter -> Arabic mappings | 4797 |
| Ambiguous Buckwalter mappings | 9 |
| Same-word supported QUL lemma assignments | 72294 |
| Confirmed/strong previous-word shift locations, including 63 + new candidates | 122 |
| Confirmed next-word shift locations | 0 |
| QUL-missing words with reliable Arabic recovery candidate outside the 63 targets | 1595 |
| Multi-STEM words checked | 483 |
| Multi-STEM words with QUL lemma | 483 |
| Multi-STEM suspicious/modeling-review words | 8 |

## 5. Audit Category Counts A-H

| Category | Count | Interpretation |
| --- | ---: | --- |
| A. Supported same-word lemma | 72294 | QUL lemma has same-word Corpus evidence under reliable mapping. |
| B. Valid null word-level lemma | 3221 | No QUL lemma and no Corpus lemma evidence requiring recovery. |
| C. Confirmed previous-word shift | 119 | Includes 60 remove-to-null rows from the 63 plus 59 broader candidates. |
| D. Confirmed next-word shift | 0 | No confirmed next-word pattern found. |
| E. Missing own lemma | 1658 | 63 known targets plus reliable missing-lemma recovery candidates outside the 63. |
| F. Wrong replacement needed | 3 | Known remove locations that need own lemma recovery rather than null. |
| G. Multi-STEM / compound review | 7 | Mostly compound particles/split behavior; exclude from simple automatic correction. |
| H. Uncertain/manual review | 130 | Unsupported, ambiguous, or unresolved source cases. |

## 6. Detailed 63-Shift Re-Check Table

| id | target location | remove location | shifted lemma Arabic | remove word text | remove Corpus lemma Buckwalter | decision | own lemma Arabic candidate | evidence / reason |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| WLA-0718 | `2:44:5` | `2:44:6` | نَفْس | وَأَنتُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='نَفْس'; targetBW=nafos; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-1281 | `2:75:3` | `2:75:4` | ءَامَنَ | لَكُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='ءَامَنَ'; targetBW='aAmana; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-2146 | `2:114:16` | `2:114:17` | كَانَ | لَهُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-4310 | `2:216:10` | `2:216:11` | شَىْء | وَهُوَ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='شَىْء'; targetBW=$aYo'; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-4599 | `2:228:7` | `2:228:8` | حَلَلْ | لَهُنَّ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='حَلَلْ'; targetBW=Halalo; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-5184 | `2:247:13` | `2:247:14` | كَانَ | لَهُ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-7041 | `3:33:6` | `3:33:7` | ءَال | إِبْرَٰهِيمَ | `<iboraAhiym` | `replace-with-own-lemma` | إِبْرَاهِيم | targetRaw=None; removeRaw='ءَال'; targetBW='aAl; remove location has reliable own Corpus Buckwalter -> Arabic mapping |
| WLA-9666 | `3:178:14` | `3:178:15` | إِثْم | وَلَهُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='إِثْم'; targetBW=<ivom; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-10410 | `4:12:8` | `4:12:9` | كَانَ | لَّهُنَّ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-11053 | `4:36:4` | `4:36:5` | أَشْرَكَ | بِهِۦ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='أَشْرَكَ'; targetBW=>a$oraka; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-11362 | `4:50:7` | `4:50:8` | كَفَىٰ | بِهِۦٓ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَفَىٰ'; targetBW=kafaY`; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-12507 | `4:101:20` | `4:101:21` | كَانَ | لَكُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-14224 | `5:5:9` | `5:5:10` | حِلّ | لَّكُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='حِلّ'; targetBW=Hil~; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-14227 | `5:5:12` | `5:5:13` | حِلّ | لَّهُمْ ۖ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='حِلّ'; targetBW=Hil~; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-16606 | `5:106:37` | `5:106:38` | اشْتَرَىٰ | بِهِۦ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='اشْتَرَىٰ'; targetBW={$otaraY`; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-16863 | `5:116:19` | `5:116:20` | كَانَ | لِىٓ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-17033 | `6:5:10` | `6:5:11` | كَانَ | بِهِۦ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-17289 | `6:20:10` | `6:20:11` | نَفْس | فَهُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='نَفْس'; targetBW=nafos; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-18615 | `6:92:17` | `6:92:18` | ءَامَنَ | بِهِۦ ۖ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='ءَامَنَ'; targetBW='aAmana; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-18833 | `6:101:9` | `6:101:10` | كَانَ | لَّهُۥ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-21731 | `7:89:16` | `7:89:17` | كَانَ | لَنَآ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-21789 | `7:92:11` | `7:92:12` | كَانَ | هُمُ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-22840 | `7:157:32` | `7:157:33` | ءَامَنَ | بِهِۦ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='ءَامَنَ'; targetBW='aAmana; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-25330 | `9:17:16` | `9:17:17` | نَار | هُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='نَار'; targetBW=naAr; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-25758 | `9:37:6` | `9:37:7` | ضَلَّ | بِهِ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='ضَلَّ'; targetBW=Dal~a; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-26131 | `9:55:15` | `9:55:16` | نَفْس | وَهُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='نَفْس'; targetBW=nafos; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-26723 | `9:85:14` | `9:85:15` | نَفْس | وَهُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='نَفْس'; targetBW=nafos; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-26885 | `9:94:10` | `9:94:11` | ءَامَنَ | لَكُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='ءَامَنَ'; targetBW='aAmana; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-29732 | `11:8:20` | `11:8:21` | كَانَ | بِهِۦ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-29885 | `11:17:17` | `11:17:18` | ءَامَنَ | بِهِۦ ۚ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='ءَامَنَ'; targetBW='aAmana; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-31853 | `12:17:14` | `12:17:15` | مُؤْمِن | لَّنَا | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='مُؤْمِن'; targetBW=mu&omin; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-31975 | `12:24:4` | `12:24:5` | هَمَّ | بِهَا | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='هَمَّ'; targetBW=ham~a; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-32247 | `12:38:8` | `12:38:9` | كَانَ | لَنَآ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-32684 | `12:64:14` | `12:64:15` | حَٰفِظ | وَهُوَ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='حَٰفِظ'; targetBW=Ha`fiZ; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-34646 | `14:11:18` | `14:11:19` | كَانَ | لَنَآ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-36865 | `16:60:9` | `16:60:10` | أَعْلَىٰ | وَهُوَ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='أَعْلَىٰ'; targetBW=>aEolaY`; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-39004 | `17:66:12` | `17:66:13` | كَانَ | بِكُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-39651 | `17:110:9` | `17:110:10` | دَعَا | فَلَهُ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='دَعَا'; targetBW=daEaA; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-42277 | `19:79:5` | `19:79:6` | مَدَّ | لَهُۥ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='مَدَّ'; targetBW=mad~a; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-43482 | `20:101:3` | `20:101:4` | سَآءَ | لَهُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='سَآءَ'; targetBW=saA^'a; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-44506 | `21:51:2` | `21:51:3` | آتَى | إِبْرَٰهِيمَ | `<iboraAhiym` | `replace-with-own-lemma` | إِبْرَاهِيم | targetRaw=None; removeRaw='آتَى'; targetBW=A^taY; remove location has reliable own Corpus Buckwalter -> Arabic mapping |
| WLA-44511 | `21:51:7` | `21:51:8` | كَانَ | بِهِۦ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-44716 | `21:73:13` | `21:73:14` | كَانَ | لَنَا | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-48793 | `24:55:29` | `24:55:30` | أَشْرَكَ | بِى | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='أَشْرَكَ'; targetBW=>a$oraka; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-49290 | `25:15:10` | `25:15:11` | كَانَ | لَهُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-49822 | `25:58:9` | `25:58:10` | كَفَىٰ | بِهِۦ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَفَىٰ'; targetBW=kafaY`; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-49842 | `25:59:15` | `25:59:16` | سَأَلَ | بِهِۦ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='سَأَلَ'; targetBW=sa>ala; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-50830 | `26:111:2` | `26:111:3` | ءَامَنَ | لَكَ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='ءَامَنَ'; targetBW='aAmana; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-53708 | `28:50:10` | `28:50:11` | أَضَلّ | مِمَّنِ | `man, min` | `replace-with-own-lemma` | مِن | targetRaw=None; removeRaw='أَضَلّ'; targetBW=>aDal~; remove location has reliable own Corpus Buckwalter -> Arabic mapping |
| WLA-53807 | `28:57:10` | `28:57:11` | مَكَّ | لَّهُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='مَكَّ'; targetBW=mak~a; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-54443 | `29:8:7` | `29:8:8` | أَشْرَكَ | بِى | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='أَشْرَكَ'; targetBW=>a$oraka; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-54741 | `29:26:1` | `29:26:2` | ءَامَنَ | لَهُۥ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='ءَامَنَ'; targetBW='aAmana; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-55090 | `29:47:8` | `29:47:9` | ءَامَنَ | بِهِۦ ۖ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='ءَامَنَ'; targetBW='aAmana; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-55193 | `29:53:9` | `29:53:10` | بَغْتَة | وَهُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='بَغْتَة'; targetBW=bagotap; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-59327 | `34:39:15` | `34:39:16` | شَىْء | فَهُوَ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='شَىْء'; targetBW=$aYo'; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-60655 | `36:30:9` | `36:30:10` | كَانَ | بِهِۦ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='كَانَ'; targetBW=kaAna; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-64470 | `40:12:9` | `40:12:10` | أَشْرَكَ | بِهِۦ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='أَشْرَكَ'; targetBW=>a$oraka; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-65886 | `41:21:12` | `41:21:13` | شَىْء | وَهُوَ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='شَىْء'; targetBW=$aYo'; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-66049 | `41:31:12` | `41:31:13` | نَفْس | وَلَكُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='نَفْس'; targetBW=nafos; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-66564 | `42:9:8` | `42:9:9` | وَلِىّ | وَهُوَ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='وَلِىّ'; targetBW=waliY~; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-68799 | `45:10:16` | `45:10:17` | وَلِىّ | وَلَهُمْ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='وَلِىّ'; targetBW=waliY~; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-69327 | `46:8:12` | `46:8:13` | شَىْء | هُوَ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='شَىْء'; targetBW=$aYo'; remove location has no reliable own word-level lemma requirement after removing shifted lemma |
| WLA-74622 | `57:15:11` | `57:15:12` | نَار | هِىَ | `-` | `remove-to-null` | - | targetRaw=None; removeRaw='نَار'; targetBW=naAr; remove location has no reliable own word-level lemma requirement after removing shifted lemma |

## 7. Remove-Location Own Lemma Recovery Table

| location | current wrong lemma | corrected lemma | operationKind | evidence |
| --- | --- | --- | --- | --- |
| `3:33:7` | ءَال | إِبْرَاهِيم | `replace` | remove word `إِبْرَٰهِيمَ` has Corpus BW `<iboraAhiym`; reliable mapping: <iboraAhiym->إِبْرَاهِيم (55/56, 98.2%) |
| `21:51:3` | آتَى | إِبْرَاهِيم | `replace` | remove word `إِبْرَٰهِيمَ` has Corpus BW `<iboraAhiym`; reliable mapping: <iboraAhiym->إِبْرَاهِيم (55/56, 98.2%) |
| `28:50:11` | أَضَلّ | مِن | `replace` | remove word `مِمَّنِ` has Corpus BW `man, min`; reliable mapping: man->مِن (870/870, 100.0%); min->مِن (3103/3225, 96.2%) |

Proposed replacement operations:

```json
[
  {
    "location": "3:33:7",
    "expectedCurrentLemmaArabic": "ءَال",
    "correctedLemmaArabic": "إِبْرَاهِيم",
    "operationKind": "replace"
  },
  {
    "location": "21:51:3",
    "expectedCurrentLemmaArabic": "آتَى",
    "correctedLemmaArabic": "إِبْرَاهِيم",
    "operationKind": "replace"
  },
  {
    "location": "28:50:11",
    "expectedCurrentLemmaArabic": "أَضَلّ",
    "correctedLemmaArabic": "مِن",
    "operationKind": "replace"
  }
]
```

## 8. QUL Present Lemma Audit

| Class | Count |
| --- | ---: |
| Total QUL lemma entries | 72507 |
| Supported by same-word Corpus evidence | 72294 |
| Looks shifted to previous word under threshold mapping | 120 |
| Looks shifted to next word | 0 |
| Unsupported by same-word/neighbor evidence or ambiguous/no Corpus support | 93 |

Representative unsupported or shifted-to-previous examples outside the original 63 are listed in section 11. The high unsupported count is not itself an automatic correction set: it includes legitimate QUL-vs-Corpus modeling differences, phrase-head choices, null Corpus lemma evidence, and cases requiring manual source normalization.

## 9. QUL Missing Lemma Audit

| Class | Count |
| --- | ---: |
| Total words without QUL word-level lemma | 4925 |
| Missing because target of the known 63 shifts | 63 |
| Missing with reliable Arabic recovery candidate outside the known 63 | 1595 |
| Missing but probably valid null, with no Corpus lemma evidence | 3221 |
| Missing uncertain, with Corpus lemma evidence but no reliable mapping | 46 |

Top missing-recovery examples outside the 63:

| location | word | Corpus lemma BW | recovered Arabic candidate |
| --- | --- | --- | --- |
| `2:10:11` | كَانُوا۟ | `kaAna` | كَانَ |
| `2:26:14` | ءَامَنُوا۟ | `'aAmana` | ءَامَنَ |
| `2:29:18` | شَىْءٍ | `$aYo'` | شَىْء |
| `2:33:6` | أَنۢبَأَهُم | `>an[ba>a` | أَنۢبَأَ |
| `2:33:14` | غَيْبَ | `gayob` | غَيْب |
| `2:33:21` | كُنتُمْ | `kaAna` | كَانَ |
| `2:38:8` | هُدًۭى | `hudFY` | هُدًى |
| `2:38:11` | هُدَاىَ | `hudFY` | هُدًى |
| `2:41:13` | تَشْتَرُوا۟ | `{$otaraY\`` | اشْتَرَىٰ |
| `2:41:16` | قَلِيلًۭا | `qaliyl` | قَلِيل |
| `2:58:19` | وَسَنَزِيدُ | `zaAda` | زَادَ |
| `2:60:17` | أُنَاسٍۢ | `<insa\`n` | إِنسَٰن |
| `2:61:36` | سَأَلْتُمْ ۗ | `sa>ala` | سَأَلَ |
| `2:61:40` | وَٱلْمَسْكَنَةُ | `masokanap` | مَسْكَنَة |
| `2:64:11` | لَكُنتُم | `kaAna` | كَانَ |
| `2:74:22` | يَشَّقَّقُ | `ya$~aq~aqu` | يَشَّقَّقُ |
| `2:75:6` | كَانَ | `kaAna` | كَانَ |
| `2:85:23` | أَفَتُؤْمِنُونَ | `'aAmana` | ءَامَنَ |
| `2:87:9` | وَءَاتَيْنَا | `A^taY` | آتَى |
| `2:87:23` | أَنفُسُكُمُ | `nafos` | نَفْس |
| `2:88:8` | فَقَلِيلًۭا | `qaliyl` | قَلِيل |
| `2:96:10` | أَحَدُهُمْ | `>aHad` | أَحَد |
| `2:102:27` | أَحَدٍ | `>aHad` | أَحَد |
| `2:102:38` | يُفَرِّقُونَ | `far~aqu` | فَرَّقُ |
| `2:102:48` | أَحَدٍ | `>aHad` | أَحَد |

## 10. Multi-STEM / Compound Review

| Metric | Count |
| --- | ---: |
| Multi-STEM words checked | 483 |
| Multi-STEM words with QUL word-level lemma | 483 |
| Multi-STEM words with suspicious/simple mismatch | 8 |

The multi-STEM mismatches are dominated by compound particles such as `أَنَّمَآ`, `إِنَّمَا`, `مِمَّا`, `عَمَّا`, and `مِمَّن`. They create false positives for simple same-word matching and should be excluded from automatic correction unless manually curated. One known multi-STEM remove location, `28:50:11` (`مِمَّنِ`), is not a null case: the dominant same-source mapping supports replacement with `مِن`.

Representative multi-STEM review examples:

| location | word | QUL lemma | Corpus lemma BW values | POS |
| --- | --- | --- | --- | --- |
| `8:28:2` | أَنَّمَآ | إِنّ | `>an~, maA` | `ACC, PREV` |
| `8:73:6` | إِلَّا | إِلَّا | `<in, laA` | `COND, NEG` |
| `11:14:5` | أَنَّمَآ | إِنّ | `>an~, maA` | `ACC, PREV` |
| `18:110:8` | أَنَّمَآ | إِنّ | `>an~, maA` | `ACC, PREV` |
| `21:108:5` | أَنَّمَآ | إِنّ | `>an~, maA` | `ACC, PREV` |
| `28:50:11` | مِمَّنِ | أَضَلّ | `man, min` | `P, REL` |
| `38:70:5` | أَنَّمَآ | إِنّ | `>an~, maA` | `ACC, NEG` |
| `41:6:8` | أَنَّمَآ | إِنّ | `>an~, maA` | `ACC, PREV` |

## 11. New Suspected Cases Outside the 63

The broader pass found **59** previous-word shift candidates outside the 63 and **0** next-word shift candidates. These are not production-ready corrections yet; they must be curated because many require replacement with the remove/current word own lemma, and several are phrase-head/modeling cases rather than the original rootless-pronoun pattern.

| current location | current word | raw QUL lemma on current word | own Corpus BW | own Arabic candidate | previous location | previous word | previous Corpus BW |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `2:126:23` | قَلِيلًۭا | مَّتَّعْ | `qaliyl` | قَلِيل | `2:126:22` | فَأُمَتِّعُهُۥ | `m~at~aEo` |
| `3:49:13` | لَكُم | خَلَقَ | `-` | - | `3:49:12` | أَخْلُقُ | `xalaqa` |
| `3:93:4` | حِلًّۭا | كَانَ | `Hil~` | - | `3:93:3` | كَانَ | `kaAna` |
| `3:116:15` | ٱلنَّارِ ۚ | أَصْحَٰب | `naAr` | نَار | `3:116:14` | أَصْحَـٰبُ | `>aSoHa\`b` |
| `3:154:16` | يَظُنُّونَ | نَفْس | `Zan~a` | ظَنَّ | `3:154:15` | أَنفُسُهُمْ | `nafos` |
| `4:23:53` | غَفُورًۭا | كَانَ | `gafuwr` | غَفُور | `4:23:52` | كَانَ | `kaAna` |
| `4:28:7` | ٱلْإِنسَـٰنُ | خَلَقَ | `<insa\`n` | إِنسَٰن | `4:28:6` | وَخُلِقَ | `xalaqa` |
| `4:45:7` | وَكَفَىٰ | وَلِىّ | `kafaY\`` | كَفَىٰ | `4:45:6` | وَلِيًّۭا | `waliY~` |
| `4:54:12` | ءَالَ | آتَى | `'aAl` | ءَال | `4:54:11` | ءَاتَيْنَآ | `A^taY` |
| `4:59:21` | تُؤْمِنُونَ | كَانَ | `'aAmana` | ءَامَنَ | `4:59:20` | كُنتُمْ | `kaAna` |
| `4:106:6` | غَفُورًۭا | كَانَ | `gafuwr` | غَفُور | `4:106:5` | كَانَ | `kaAna` |
| `4:129:13` | فَتَذَرُوهَا | مَيْل | `ya*ara` | يَذَرَ | `4:129:12` | ٱلْمَيْلِ | `mayol` |
| `5:41:13` | بِأَفْوَٰهِهِمْ | ءَامَنَ | `>afowa\`h` | أَفْوَٰه | `5:41:12` | ءَامَنَّا | `'aAmana` |
| `6:101:12` | وَخَلَقَ | صَٰحِبَة | `xalaqa` | خَلَقَ | `6:101:11` | صَـٰحِبَةٌۭ ۖ | `Sa\`Hibap` |
| `6:111:15` | لِيُؤْمِنُوٓا۟ | كَانَ | `'aAmana` | ءَامَنَ | `6:111:14` | كَانُوا۟ | `kaAna` |
| `6:136:17` | لِشُرَكَآئِهِمْ | كَانَ | `$ariyk` | شَرِيك | `6:136:16` | كَانَ | `kaAna` |
| `6:158:26` | ءَامَنَتْ | كَانَ | `'aAmana` | ءَامَنَ | `6:158:25` | تَكُنْ | `kaAna` |
| `7:12:15` | وَخَلَقْتَهُۥ | نَار | `xalaqa` | خَلَقَ | `7:12:14` | نَّارٍۢ | `naAr` |
| `7:47:6` | ٱلنَّارِ | أَصْحَٰب | `naAr` | نَار | `7:47:5` | أَصْحَـٰبِ | `>aSoHa\`b` |
| `7:86:18` | قَلِيلًۭا | كَانَ | `qaliyl` | قَلِيل | `7:86:17` | كُنتُمْ | `kaAna` |
| `7:134:17` | لَنُؤْمِنَنَّ | رِجْز | `'aAmana` | ءَامَنَ | `7:134:16` | ٱلرِّجْزَ | `rijoz` |
| `8:41:18` | ءَامَنتُم | كَانَ | `'aAmana` | ءَامَنَ | `8:41:17` | كُنتُمْ | `kaAna` |
| `9:61:15` | لِلْمُؤْمِنِينَ | ءَامَنَ | `mu&omin` | مُؤْمِن | `9:61:14` | وَيُؤْمِنُ | `'aAmana` |
| `9:105:11` | ٱلْغَيْبِ | عَٰلِم | `gayob` | غَيْب | `9:105:10` | عَـٰلِمِ | `Ea\`lim` |
| `9:111:6` | أَنفُسَهُمْ | مُؤْمِن | `nafos` | نَفْس | `9:111:5` | ٱلْمُؤْمِنِينَ | `mu&omin` |
| `9:122:3` | ٱلْمُؤْمِنُونَ | كَانَ | `mu&omin` | مُؤْمِن | `9:122:2` | كَانَ | `kaAna` |
| `9:124:15` | فَزَادَتْهُمْ | ءَامَنَ | `zaAda` | زَادَ | `9:124:14` | ءَامَنُوا۟ | `'aAmana` |
| `10:13:13` | لِيُؤْمِنُوا۟ ۚ | كَانَ | `'aAmana` | ءَامَنَ | `10:13:12` | كَانُوا۟ | `kaAna` |
| `10:71:11` | كَبُرَ | كَانَ | `kabura` | - | `10:71:10` | كَانَ | `kaAna` |
| `12:111:20` | وَهُدًۭى | شَىْء | `hudFY` | هُدًى | `12:111:19` | شَىْءٍۢ | `$aYo'` |
| `16:111:8` | وَتُوَفَّىٰ | نَفْس | `waf~aY\`^` | وَفَّىٰٓ | `16:111:7` | نَّفْسِهَا | `nafos` |
| `16:118:14` | أَنفُسَهُمْ | كَانَ | `nafos` | نَفْس | `16:118:13` | كَانُوٓا۟ | `kaAna` |
| `17:11:7` | ٱلْإِنسَـٰنُ | كَانَ | `<insa\`n` | إِنسَٰن | `17:11:6` | وَكَانَ | `kaAna` |
| `17:12:18` | ٱلسِّنِينَ | عَدَد | `siniyn` | سِنِين | `17:12:17` | عَدَدَ | `Eadad` |
| `17:33:15` | سُلْطَـٰنًۭا | وَلِىّ | `suloTa\`n` | سُلْطَٰن | `17:33:14` | لِوَلِيِّهِۦ | `waliY~` |
| `17:100:13` | ٱلْإِنسَـٰنُ | كَانَ | `<insa\`n` | إِنسَٰن | `17:100:12` | وَكَانَ | `kaAna` |
| `21:47:12` | مِثْقَالَ | كَانَ | `mivoqaAl` | - | `21:47:11` | كَانَ | `kaAna` |
| `24:2:18` | تُؤْمِنُونَ | كَانَ | `'aAmana` | ءَامَنَ | `24:2:17` | كُنتُمْ | `kaAna` |
| `25:6:11` | غَفُورًۭا | كَانَ | `gafuwr` | غَفُور | `25:6:10` | كَانَ | `kaAna` |
| `28:58:15` | وَكُنَّا | قَلِيل | `kaAna` | كَانَ | `28:58:14` | قَلِيلًۭا ۖ | `qaliyl` |
| `28:79:12` | لَنَا | لَيْت | `-` | - | `28:79:11` | يَـٰلَيْتَ | `layot` |
| `30:55:12` | يُؤْفَكُونَ | كَانَ | `>ufika` | أُفِكَ | `30:55:11` | كَانُوا۟ | `kaAna` |
| `33:24:15` | غَفُورًۭا | كَانَ | `gafuwr` | غَفُور | `33:24:14` | كَانَ | `kaAna` |
| `33:43:12` | بِٱلْمُؤْمِنِينَ | كَانَ | `mu&omin` | مُؤْمِن | `33:43:11` | وَكَانَ | `kaAna` |
| `34:3:12` | ٱلْغَيْبِ ۖ | عَٰلِم | `gayob` | غَيْب | `34:3:11` | عَـٰلِمِ | `Ea\`lim` |
| `35:6:9` | حِزْبَهُۥ | دَعَا | `Hizob` | حِزْب | `35:6:8` | يَدْعُوا۟ | `daEaA` |
| `38:76:8` | وَخَلَقْتَهُۥ | نَار | `xalaqa` | خَلَقَ | `38:76:7` | نَّارٍۢ | `naAr` |
| `39:8:17` | يَدْعُوٓا۟ | كَانَ | `daEaA` | دَعَا | `39:8:16` | كَانَ | `kaAna` |
| `41:15:19` | هُوَ | خَلَقَ | `-` | - | `41:15:18` | خَلَقَهُمْ | `xalaqa` |
| `49:11:25` | أَنفُسَكُمْ | يَلْمِزُ | `nafos` | نَفْس | `49:11:24` | تَلْمِزُوٓا۟ | `yalomizu` |
| `53:26:9` | شَيْـًٔا | شَفَٰعَة | `$aYo'` | شَىْء | `53:26:8` | شَفَـٰعَتُهُمْ | `$afa\`Eap` |
| `57:8:8` | لِتُؤْمِنُوا۟ | دَعَا | `'aAmana` | ءَامَنَ | `57:8:7` | يَدْعُوكُمْ | `daEaA` |
| `58:17:12` | ٱلنَّارِ ۖ | أَصْحَٰب | `naAr` | نَار | `58:17:11` | أَصْحَـٰبُ | `>aSoHa\`b` |
| `59:9:28` | نَفْسِهِۦ | شُحّ | `nafos` | نَفْس | `59:9:27` | شُحَّ | `$uH~` |
| `59:22:9` | ٱلْغَيْبِ | عَٰلِم | `gayob` | غَيْب | `59:22:8` | عَـٰلِمُ | `Ea\`lim` |
| `62:8:13` | ٱلْغَيْبِ | عَٰلِم | `gayob` | غَيْب | `62:8:12` | عَـٰلِمِ | `Ea\`lim` |
| `64:16:13` | نَفْسِهِۦ | شُحّ | `nafos` | نَفْس | `64:16:12` | شُحَّ | `$uH~` |
| `70:19:3` | خُلِقَ | إِنسَٰن | `xalaqa` | خَلَقَ | `70:19:2` | ٱلْإِنسَـٰنَ | `<insa\`n` |
| `89:23:6` | ٱلْإِنسَـٰنُ | تَذَكَّرَ | `<insa\`n` | إِنسَٰن | `89:23:5` | يَتَذَكَّرُ | `ta*ak~ara` |

## 12. Uncertain / Blocking Cases

- The original 63 are confirmed as a real previous-word shift family, but 3 remove locations need `replace` operations instead of `remove` operations.
- The 59 new previous-word candidates outside the 63 show a broader alignment issue; they block implementation unless explicitly scoped out and documented.
- 1595 QUL-missing words have reliable Arabic recovery candidates outside the 63. They should not all become automatic overlay entries, but they show that missing lemmas are broader than the strict shift target set.
- 46 missing-lemma words have Corpus evidence but no reliable Arabic mapping under the thresholded mapping.
- 8 multi-STEM words need compound/manual review and should be excluded from simple same-word validation.

## 13. Recommended Next Action

1. Do not implement the current 63-entry overlay as-is.
2. Revise the overlay schema/application plan to support `replace` operations, not only `add` + `remove-to-null`.
3. Update the draft overlay for at least these three remove-location replacements: `3:33:7 -> إِبْرَاهِيم`, `21:51:3 -> إِبْرَاهِيم`, and `28:50:11 -> مِن`.
4. Create a second curation pass for the 59 broader previous-word candidates before importer implementation.
5. Keep next-word shift handling as a diagnostic check; this audit found zero confirmed next-word shifts.
6. Treat multi-STEM/compound words as manual-review or explicit allow-list cases, not automatic correction targets.
7. Revise `word-level-lemma-alignment-correction-plan.md` to reflect the broader source-normalization issue and the required replacement operation kind.

Final status: **`BLOCKED — broader source alignment issue exists beyond the 63`**.
