# Feature 005 — Segment-Pattern Rule-Coverage Inventory (Read-Only)

**Type:** Read-only DB inspection. No code, no migrations, no Spec Kit artifacts, no DB writes, no edits to existing reports.
**Branch:** `005-word-simple-i3rab-foundation`  ·  **Date:** 2026-06-12
**Source:** live `quran_dashboard` DB (Feature 004 morphology), read-only.
**Companions:** _removed._ The pre-final `segment-pattern-rule-coverage.json` / `.csv` companions were **deleted** to prevent accidental use as canonical rule seeds. This finalized markdown report and the planning report are **authoritative**. If a machine-readable rule seed is needed during implementation, regenerate it from the finalized **142-signature / 67-family** approved catalogue (§3.4 / §4).
**Sibling:** [`simple-i3rab-label-inventory-report.md`](simple-i3rab-label-inventory-report.md) (label inventory) — this report adds the **full segment-ordering** analysis.

> All counts are queried from the populated tables (`…_morphology`=77,432, `…_segments`=128,219). Every pattern carries a real example word (`id`/`location`/Uthmani). Nothing was invented; Feature 004 data was read-only. User-facing labels are **Arabic only**; POS codes appear for developer audit only.

## 1. Executive summary

| Metric | Value |
| --- | ---: |
| Total readable words | 77,432 |
| Total morphology rows | 77,432 |
| Total segment rows | 128,219 |
| Distinct **POS-only** patterns (level A) | 358 |
| Distinct **kind+POS** patterns (level B) | 371 |
| Distinct **enriched i‘rab-signature** patterns (level C) | 1337 |
| Distinct **segment-token** signatures (rule basis) | 142 |
| Proposed **rule families** | 67 |
| Word coverage — top 10 / 25 / 50 / 100 POS-only patterns | 63.09% / 80.34% / 91.68% / 97.16% |
| Long tail — singleton POS-only / enriched patterns | 73 / 405 |
| Segment label coverage (any label) | 100.0% |
| Segment **approved-candidate** / needs-review / unsupported | 100.0% / 0.0% / 0.0% |
| Words fully-approved (all segments approved) | 100.0% |
| Words **displayable** (no unsupported segment) | 100.0% |

**Read:** segment-level i‘rab is highly tractable — **every** segment resolves to an approved-candidate label (**100 %** segment-row coverage), and **100 %** of words are displayable from segment labels. The long tail is real (405 enriched patterns occur once) but it is **morphologically routine** — singletons are rare person/number/voice combinations of already-covered rules, not new grammar.

## 2. Pattern levels

Patterns were computed per word at three altitudes (representative slices shown below; the full per-pattern enumerations previously lived in the now-removed JSON/CSV companions — regenerate from the finalized catalogue if a complete machine-readable list is needed):

- **A) POS-only** — ordered POS codes, e.g. `N`, `V+PRON`, `DET+N`, `P+PRON`, `CONJ+P+DET+N`. (358 distinct)
- **B) Kind+POS** — adds segment kind, e.g. `STEM:N`, `PREFIX:DET + STEM:N`, `PREFIX:P + STEM:N + SUFFIX:PRON`. (371 distinct)
- **C) Enriched i‘rab-signature** — adds the grammar-driving features: noun/adj/PN case (NOM/ACC/GEN), verb tense (PERF/IMPF/IMPV) + voice (ACT/PASS) + person, pronoun person (PRON:1S…), and the **Allah lemma** flag. e.g. `PREFIX:P + STEM:N:GEN + SUFFIX:PRON:2MS`, `PREFIX:INTG + STEM:V:IMPF:ACT:2MS`, `STEM:PN:ALLAH:GEN`, `STEM:V:PERF:PASS:3MS`. (1,337 distinct)

The **segment-token** signature (the per-segment piece of level C, 142 distinct) is the direct basis for segment-level i‘rab rules and is listed in full in §3.4.

## 3. Pattern tables

### 3.1 POS-only — top 30 (top 30 of 358; full list was in the now-removed companions)

| # | pattern | words | % | segs | forms | example |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 1 | `N` | 10,340 | 13.3537 | 10,340 | 4279 | رَبِّ `1:2:3` |
| 2 | `V+PRON` | 7,439 | 9.6071 | 14,878 | 2882 | ٱهْدِنَا `1:6:1` |
| 3 | `V` | 5,862 | 7.5705 | 5,862 | 1957 | نَعْبُدُ `1:5:2` |
| 4 | `DET+N` | 5,848 | 7.5524 | 11,696 | 1721 | ٱلْحَمْدُ `1:2:1` |
| 5 | `P` | 4,947 | 6.3888 | 4,947 | 24 | مِن `2:4:8` |
| 6 | `N+PRON` | 4,022 | 5.1942 | 8,044 | 1941 | قَبْلِكَ `2:4:9` |
| 7 | `P+PRON` | 3,888 | 5.0212 | 7,776 | 300 | عَلَيْهِمْ `1:7:4` |
| 8 | `PN` | 2,834 | 3.66 | 2,834 | 186 | ٱللَّهِ `1:1:2` |
| 9 | `REL` | 2,204 | 2.8464 | 2,204 | 23 | ٱلَّذِينَ `1:7:2` |
| 10 | `CONJ+V+PRON` | 1,470 | 1.8984 | 4,410 | 824 | وَيُقِيمُونَ `2:3:4` |
| 11 | `ADJ` | 1,384 | 1.7874 | 1,384 | 649 | عَظِيمٌۭ `2:7:12` |
| 12 | `NEG` | 1,258 | 1.6247 | 1,258 | 20 | لَا `2:2:3` |
| 13 | `CONJ+V` | 1,243 | 1.6053 | 2,486 | 639 | وَأَنزَلَ `2:22:8` |
| 14 | `ACC+PRON` | 863 | 1.1145 | 1,726 | 61 | إِنَّهُمْ `2:12:2` |
| 15 | `CONJ+NEG` | 862 | 1.1132 | 1,724 | 16 | وَلَا `1:7:8` |
| 16 | `PRON` | 840 | 1.0848 | 840 | 29 | إِيَّاكَ `1:5:1` |
| 17 | `P+N` | 812 | 1.0487 | 1,624 | 355 | بِسْمِ `1:1:1` |
| 18 | `CONJ+DET+N` | 795 | 1.0267 | 2,385 | 399 | وَٱلسَّمَآءَ `2:22:6` |
| 19 | `V+PRON+PRON` | 793 | 1.0241 | 2,379 | 580 | رَزَقْنَـٰهُمْ `2:3:7` |
| 20 | `DEM` | 773 | 0.9983 | 773 | 35 | ذَٰلِكَ `2:2:1` |
| 21 | `CONJ+N` | 773 | 0.9983 | 1,546 | 504 | وَرَعْدٌۭ `2:19:7` |
| 22 | `P+DET+N` | 765 | 0.988 | 2,295 | 313 | لِّلْمُتَّقِينَ `2:2:7` |
| 23 | `CONJ` | 742 | 0.9583 | 742 | 10 | أَمْ `2:6:7` |
| 24 | `P+REL` | 740 | 0.9557 | 1,480 | 46 | بِمَآ `2:4:3` |
| 25 | `ACC` | 714 | 0.9221 | 714 | 7 | إِنَّ `2:6:1` |
| 26 | `T` | 660 | 0.8524 | 660 | 54 | كُلَّمَآ `2:20:5` |
| 27 | `DET+ADJ` | 575 | 0.7426 | 1,150 | 196 | ٱلرَّحْمَـٰنِ `1:1:3` |
| 28 | `RES` | 556 | 0.718 | 556 | 2 | إِلَّآ `2:9:7` |
| 29 | `SUB` | 528 | 0.6819 | 528 | 8 | أَن `2:26:5` |
| 30 | `P+N+PRON` | 505 | 0.6522 | 1,515 | 313 | بِنُورِهِمْ `2:17:12` |

### 3.2 Kind+POS — top 25 (top 25 of 371; full list was in the now-removed companions)

| # | pattern | words | % | example |
| --- | --- | ---: | ---: | --- |
| 1 | `STEM:N` | 10,340 | 13.3537 | رَبِّ `1:2:3` |
| 2 | `STEM:V + SUFFIX:PRON` | 7,439 | 9.6071 | ٱهْدِنَا `1:6:1` |
| 3 | `STEM:V` | 5,862 | 7.5705 | نَعْبُدُ `1:5:2` |
| 4 | `PREFIX:DET + STEM:N` | 5,848 | 7.5524 | ٱلْحَمْدُ `1:2:1` |
| 5 | `STEM:P` | 4,947 | 6.3888 | مِن `2:4:8` |
| 6 | `STEM:N + SUFFIX:PRON` | 4,022 | 5.1942 | قَبْلِكَ `2:4:9` |
| 7 | `STEM:PN` | 2,834 | 3.66 | ٱللَّهِ `1:1:2` |
| 8 | `STEM:REL` | 2,204 | 2.8464 | ٱلَّذِينَ `1:7:2` |
| 9 | `STEM:P + SUFFIX:PRON` | 2,101 | 2.7133 | عَلَيْهِمْ `1:7:4` |
| 10 | `PREFIX:P + STEM:PRON` | 1,787 | 2.3078 | لَهُمْ `2:11:3` |
| 11 | `PREFIX:CONJ + STEM:V + SUFFIX:PRON` | 1,470 | 1.8984 | وَيُقِيمُونَ `2:3:4` |
| 12 | `STEM:ADJ` | 1,384 | 1.7874 | عَظِيمٌۭ `2:7:12` |
| 13 | `STEM:NEG` | 1,258 | 1.6247 | لَا `2:2:3` |
| 14 | `PREFIX:CONJ + STEM:V` | 1,243 | 1.6053 | وَأَنزَلَ `2:22:8` |
| 15 | `STEM:ACC + SUFFIX:PRON` | 863 | 1.1145 | إِنَّهُمْ `2:12:2` |
| 16 | `PREFIX:CONJ + STEM:NEG` | 862 | 1.1132 | وَلَا `1:7:8` |
| 17 | `STEM:PRON` | 840 | 1.0848 | إِيَّاكَ `1:5:1` |
| 18 | `PREFIX:P + STEM:N` | 812 | 1.0487 | بِسْمِ `1:1:1` |
| 19 | `PREFIX:CONJ + PREFIX:DET + STEM:N` | 795 | 1.0267 | وَٱلسَّمَآءَ `2:22:6` |
| 20 | `STEM:V + SUFFIX:PRON + SUFFIX:PRON` | 793 | 1.0241 | رَزَقْنَـٰهُمْ `2:3:7` |
| 21 | `STEM:DEM` | 773 | 0.9983 | ذَٰلِكَ `2:2:1` |
| 22 | `PREFIX:CONJ + STEM:N` | 773 | 0.9983 | وَرَعْدٌۭ `2:19:7` |
| 23 | `PREFIX:P + PREFIX:DET + STEM:N` | 765 | 0.988 | لِّلْمُتَّقِينَ `2:2:7` |
| 24 | `STEM:CONJ` | 742 | 0.9583 | أَمْ `2:6:7` |
| 25 | `STEM:ACC` | 714 | 0.9221 | إِنَّ `2:6:1` |

### 3.3 Enriched i‘rab-signature — top 30 (top 30 of 1,337; full list was in the now-removed companions)

| # | signature | words | % | status | example |
| --- | --- | ---: | ---: | --- | --- |
| 1 | `STEM:P` | 4,947 | 6.3888 | approved-candidate | مِن `2:4:8` |
| 2 | `STEM:N:ACC` | 4,438 | 5.7315 | approved-candidate | صِرَٰطَ `1:7:1` |
| 3 | `STEM:N:NOM` | 3,055 | 3.9454 | approved-candidate | هُدًۭى `2:2:6` |
| 4 | `PREFIX:DET + STEM:N:GEN` | 2,925 | 3.7775 | approved-candidate | ٱلْعَـٰلَمِينَ `1:2:4` |
| 5 | `STEM:N:GEN` | 2,847 | 3.6768 | approved-candidate | رَبِّ `1:2:3` |
| 6 | `STEM:REL` | 2,204 | 2.8464 | approved-candidate | ٱلَّذِينَ `1:7:2` |
| 7 | `STEM:V:PERF:ACT:3MS` | 2,054 | 2.6527 | approved-candidate | خَتَمَ `2:7:1` |
| 8 | `STEM:V:PERF:ACT:3MP + SUFFIX:PRON:3MP` | 1,525 | 1.9695 | approved-candidate | كَفَرُوا۟ `2:6:3` |
| 9 | `STEM:V:IMPF:ACT:3MP + SUFFIX:PRON:3MP` | 1,505 | 1.9436 | approved-candidate | يُؤْمِنُونَ `2:3:2` |
| 10 | `STEM:V:IMPF:ACT:3MS` | 1,469 | 1.8971 | approved-candidate | يَقُولُ `2:8:4` |
| 11 | `PREFIX:DET + STEM:N:NOM` | 1,464 | 1.8907 | approved-candidate | ٱلْحَمْدُ `1:2:1` |
| 12 | `PREFIX:DET + STEM:N:ACC` | 1,459 | 1.8842 | approved-candidate | ٱلصِّرَٰطَ `1:6:2` |
| 13 | `STEM:NEG` | 1,258 | 1.6247 | approved-candidate | لَا `2:2:3` |
| 14 | `STEM:V:IMPF:ACT:2MP + SUFFIX:PRON:2MP` | 920 | 1.1881 | approved-candidate | تُفْسِدُوا۟ `2:11:5` |
| 15 | `PREFIX:CONJ + STEM:NEG` | 862 | 1.1132 | approved-candidate | وَلَا `1:7:8` |
| 16 | `STEM:PN:ALLAH:GEN` | 828 | 1.0693 | approved-candidate | ٱللَّهِ `1:1:2` |
| 17 | `PREFIX:P + STEM:N:GEN` | 812 | 1.0487 | approved-candidate | بِسْمِ `1:1:1` |
| 18 | `PREFIX:P + PREFIX:DET + STEM:N:GEN` | 765 | 0.988 | approved-candidate | لِّلْمُتَّقِينَ `2:2:7` |
| 19 | `STEM:CONJ` | 742 | 0.9583 | approved-candidate | أَمْ `2:6:7` |
| 20 | `STEM:PN:ALLAH:NOM` | 733 | 0.9466 | approved-candidate | ٱللَّهُ `2:7:2` |
| 21 | `STEM:DEM` | 728 | 0.9402 | approved-candidate | ذَٰلِكَ `2:2:1` |
| 22 | `STEM:ACC` | 714 | 0.9221 | approved-candidate | إِنَّ `2:6:1` |
| 23 | `STEM:T` | 660 | 0.8524 | approved-candidate | كُلَّمَآ `2:20:5` |
| 24 | `STEM:ADJ:NOM` | 605 | 0.7813 | approved-candidate | عَظِيمٌۭ `2:7:12` |
| 25 | `PREFIX:P + STEM:PRON:3MS` | 602 | 0.7775 | approved-candidate | بِهِۦ `2:22:13` |
| 26 | `STEM:PN:ALLAH:ACC` | 592 | 0.7645 | approved-candidate | ٱللَّهَ `2:9:2` |
| 27 | `PREFIX:CONJ + STEM:V:PERF:ACT:3MS` | 558 | 0.7206 | approved-candidate | وَأَنزَلَ `2:22:8` |
| 28 | `STEM:RES` | 556 | 0.718 | approved-candidate | إِلَّآ `2:9:7` |
| 29 | `STEM:V:PERF:ACT:1P + SUFFIX:PRON:1P` | 555 | 0.7168 | approved-candidate | ءَامَنَّا `2:8:5` |
| 30 | `STEM:SUB` | 528 | 0.6819 | approved-candidate | أَن `2:26:5` |

### 3.4 Segment-token inventory — ALL 142 (the rule basis)

Each row = one distinct segment signature, its proposed Arabic i‘rab label, v1 status, rule key, and a real example.

| seg signature | kind | segs | words | i‘rab (Arabic) | v1 | rule key | example |
| --- | --- | ---: | ---: | --- | --- | --- | --- |
| `STEM:N:GEN` | STEM | 10,403 | 10,403 | اسم مجرور | ✅ | `N.GEN` | بِسْمِ `1:1:1` |
| `PREFIX:CONJ` | PREFIX | 8,694 | 8,694 | حرف عطف | ✅ | `CONJ` | وَإِيَّاكَ `1:5:3` |
| `PREFIX:DET` | PREFIX | 8,377 | 8,377 | أداة تعريف | ✅ | `DET` | ٱلرَّحْمَـٰنِ `1:1:3` |
| `STEM:N:ACC` | STEM | 7,955 | 7,955 | اسم منصوب | ✅ | `N.ACC` | ٱلصِّرَٰطَ `1:6:2` |
| `STEM:P` | STEM | 7,679 | 7,679 | حرف جر | ✅ | `P` | عَلَيْهِمْ `1:7:4` |
| `SUFFIX:PRON:3MP` | SUFFIX | 7,366 | 7,337 | ضمير متصل للغائبين | ✅ | `PRON.SUF.3MP` | عَلَيْهِمْ `1:7:4` |
| `STEM:N:NOM` | STEM | 6,777 | 6,777 | اسم مرفوع | ✅ | `N.NOM` | ٱلْحَمْدُ `1:2:1` |
| `PREFIX:P` | PREFIX | 5,325 | 5,325 | حرف جر | ✅ | `P` | بِسْمِ `1:1:1` |
| `SUFFIX:PRON:2MP` | SUFFIX | 4,645 | 4,645 | ضمير متصل للمخاطبين | ✅ | `PRON.SUF.2MP` | تُفْسِدُوا۟ `2:11:5` |
| `STEM:V:PERF:ACT:3MS` | STEM | 3,600 | 3,600 | فعل ماض | ✅ | `V.PERF.ACT` | خَتَمَ `2:7:1` |
| `STEM:REL` | STEM | 3,575 | 3,575 | اسم موصول | ✅ | `REL` | ٱلَّذِينَ `1:7:2` |
| `PREFIX:REM` | PREFIX | 2,925 | 2,925 | حرف استئناف | ✅ | `REM` | وَمِمَّا `2:3:6` |
| `SUFFIX:PRON:3MS` | SUFFIX | 2,727 | 2,727 | ضمير متصل للغائب | ✅ | `PRON.SUF.3MS` | فِيهِ ۛ `2:2:5` |
| `STEM:NEG` | STEM | 2,688 | 2,688 | حرف نفي | ✅ | `NEG` | وَلَا `1:7:8` |
| `STEM:V:IMPF:ACT:3MS` | STEM | 2,556 | 2,556 | فعل مضارع | ✅ | `V.IMPF.ACT` | يَقُولُ `2:8:4` |
| `SUFFIX:PRON:1P` | SUFFIX | 2,347 | 2,347 | ضمير متصل لجماعة المتكلمين | ✅ | `PRON.SUF.1P` | ٱهْدِنَا `1:6:1` |
| `STEM:ACC` | STEM | 2,283 | 2,283 | حرف نصب (من أخوات إنّ/النواصب) | ✅ | `ACC` | إِنَّ `2:6:1` |
| `STEM:V:PERF:ACT:3MP` | STEM | 2,129 | 2,129 | فعل ماض | ✅ | `V.PERF.ACT` | كَفَرُوا۟ `2:6:3` |
| `STEM:V:IMPF:ACT:3MP` | STEM | 1,996 | 1,996 | فعل مضارع | ✅ | `V.IMPF.ACT` | يُؤْمِنُونَ `2:3:2` |
| `SUFFIX:PRON:2MS` | SUFFIX | 1,300 | 1,299 | ضمير متصل للمخاطب | ✅ | `PRON.SUF.2MS` | أَنْعَمْتَ `1:7:3` |
| `STEM:V:PERF:ACT:1P` | STEM | 1,240 | 1,240 | فعل ماض | ✅ | `V.PERF.ACT` | رَزَقْنَـٰهُمْ `2:3:7` |
| `SUFFIX:PRON:1S` | SUFFIX | 1,239 | 1,239 | ضمير متصل للمتكلم المفرد | ✅ | `PRON.SUF.1S` | إِنِّى `2:30:5` |
| `STEM:V:IMPF:ACT:2MP` | STEM | 1,225 | 1,225 | فعل مضارع | ✅ | `V.IMPF.ACT` | تُفْسِدُوا۟ `2:11:5` |
| `STEM:T` | STEM | 1,166 | 1,166 | ظرف زمان | ✅ | `T.TIME` | وَإِذَا `2:11:1` |
| `STEM:PN:ALLAH:GEN` | STEM | 1,127 | 1,127 | لفظ الجلالة مجرور | ✅ | `PN.ALLAH.GEN` | ٱللَّهِ `1:1:2` |
| `STEM:PRON:3MS` | STEM | 1,126 | 1,126 | ضمير للغائب | ✅ | `PRON.STEM.3MS` | بِهِۦ `2:22:13` |
| `SUFFIX:PRON:3FS` | SUFFIX | 1,062 | 1,062 | ضمير متصل للغائبة | ✅ | `PRON.SUF.3FS` | وَقُودُهَا `2:24:9` |
| `STEM:COND` | STEM | 1,049 | 1,049 | أداة شرط | ✅ | `COND` | وَلَوْ `2:20:14` |
| `STEM:DEM` | STEM | 1,009 | 1,009 | اسم إشارة | ✅ | `DEM` | ذَٰلِكَ `2:2:1` |
| `PREFIX:EMPH` | PREFIX | 1,001 | 1,001 | لام التوكيد (المزحلقة) | ✅ | `EMPH.PREFIX` | لَذَهَبَ `2:20:17` |
| `STEM:PN:ALLAH:NOM` | STEM | 980 | 980 | لفظ الجلالة مرفوع | ✅ | `PN.ALLAH.NOM` | ٱللَّهُ `2:7:2` |
| `STEM:V:IMPV:ACT:2MS` | STEM | 951 | 951 | فعل أمر | ✅ | `V.IMPV.ACT` | ٱهْدِنَا `1:6:1` |
| `STEM:PRON:3MP` | STEM | 907 | 907 | ضمير للغائبين | ✅ | `PRON.STEM.3MP` | هُمْ `2:4:11` |
| `STEM:V:IMPV:ACT:2MP` | STEM | 870 | 870 | فعل أمر | ✅ | `V.IMPV.ACT` | ءَامِنُوا۟ `2:13:4` |
| `STEM:ADJ:NOM` | STEM | 843 | 843 | صفة مرفوعة | ✅ | `ADJ.NOM` | عَظِيمٌۭ `2:7:12` |
| `STEM:CONJ` | STEM | 756 | 756 | حرف عطف | ✅ | `CONJ` | أَمْ `2:6:7` |
| `STEM:SUB` | STEM | 684 | 684 | حرف مصدري | ✅ | `SUB` | كَمَآ `2:13:5` |
| `STEM:LOC` | STEM | 669 | 669 | ظرف مكان | ✅ | `LOC` | مَعَكُمْ `2:14:13` |
| `STEM:V:IMPF:ACT:1P` | STEM | 592 | 592 | فعل مضارع | ✅ | `V.IMPF.ACT` | نَعْبُدُ `1:5:2` |
| `STEM:PN:ALLAH:ACC` | STEM | 592 | 592 | لفظ الجلالة منصوب | ✅ | `PN.ALLAH.ACC` | ٱللَّهَ `2:9:2` |
| `STEM:ADJ:ACC` | STEM | 590 | 590 | صفة منصوبة | ✅ | `ADJ.ACC` | ٱلْمُسْتَقِيمَ `1:6:3` |
| `STEM:V:PERF:ACT:2MP` | STEM | 562 | 562 | فعل ماض | ✅ | `V.PERF.ACT` | كُنتُمْ `2:23:2` |
| `STEM:RES` | STEM | 558 | 558 | أداة حصر | ✅ | `RES` | إِلَّآ `2:9:7` |
| `STEM:PN:GEN` | STEM | 551 | 551 | اسم علم مجرور | ✅ | `PN.GEN` | لِـَٔادَمَ `2:34:5` |
| `STEM:PRON:2MP` | STEM | 536 | 536 | ضمير للمخاطبين | ✅ | `PRON.STEM.2MP` | لَكُمُ `2:22:3` |
| `STEM:ADJ:GEN` | STEM | 528 | 528 | صفة مجرورة | ✅ | `ADJ.GEN` | ٱلرَّحْمَـٰنِ `1:1:3` |
| `PREFIX:INTG` | PREFIX | 507 | 507 | همزة استفهام | ✅ | `INTG.PREFIX` | أَنُؤْمِنُ `2:13:9` |
| `STEM:V:IMPF:ACT:2MS` | STEM | 487 | 487 | فعل مضارع | ✅ | `V.IMPF.ACT` | تُنذِرْهُمْ `2:6:9` |
| `STEM:V:PERF:ACT:3FS` | STEM | 479 | 479 | فعل ماض | ✅ | `V.PERF.ACT` | رَبِحَت `2:16:7` |
| `STEM:V:IMPF:ACT:3FS` | STEM | 455 | 455 | فعل مضارع | ✅ | `V.IMPF.ACT` | تَجْرِى `2:25:9` |
| `STEM:INTG` | STEM | 439 | 439 | اسم استفهام | ✅ | `INTG.STEM` | مَاذَآ `2:26:24` |
| `STEM:CERT` | STEM | 414 | 414 | حرف تحقيق (قد) | ✅ | `CERT` | قَدْ `2:60:14` |
| `PREFIX:VOC` | PREFIX | 371 | 371 | حرف نداء | ✅ | `VOC` | يَـٰٓأَيُّهَا `2:21:1` |
| `STEM:V:IMPF:ACT:1S` | STEM | 368 | 368 | فعل مضارع | ✅ | `V.IMPF.ACT` | أَعْلَمُ `2:30:25` |
| `PREFIX:RSLT` | PREFIX | 350 | 350 | الفاء الرابطة لجواب الشرط | ✅ | `RSLT` | فَأْتُوا۟ `2:23:9` |
| `STEM:PN:NOM` | STEM | 341 | 341 | اسم علم مرفوع | ✅ | `PN.NOM` | يَـٰٓـَٔادَمُ `2:33:2` |
| `STEM:PRO` | STEM | 332 | 332 | ضمير منفصل | ✅ | `PRO` | لَا `2:11:4` |
| `STEM:V:PERF:PASS:3MS` | STEM | 327 | 327 | فعل ماض مبني للمجهول | ✅ | `V.PERF.PASS` | أُنزِلَ `2:4:4` |
| `STEM:PN:ACC` | STEM | 320 | 320 | اسم علم منصوب | ✅ | `PN.ACC` | ءَادَمَ `2:31:2` |
| `PREFIX:PRP` | PREFIX | 319 | 319 | لام التعليل | ✅ | `PRP` | لِيُحَآجُّوكُم `2:76:18` |
| `PREFIX:CIRC` | PREFIX | 293 | 293 | واو الحال | ✅ | `CIRC` | وَمَا `2:8:9` |
| `SUFFIX:PRON:3FP` | SUFFIX | 267 | 267 | ضمير متصل للغائبات | ✅ | `PRON.SUF.3FP` | فَسَوَّىٰهُنَّ `2:29:13` |
| `SUFFIX:EMPH` | SUFFIX | 243 | 243 | نون التوكيد | ✅ | `EMPH.SUFFIX` | يَأْتِيَنَّكُم `2:38:6` |
| `STEM:V:PERF:ACT:2MS` | STEM | 232 | 232 | فعل ماض | ✅ | `V.PERF.ACT` | أَنْعَمْتَ `1:7:3` |
| `PREFIX:SUP` | PREFIX | 214 | 214 | حرف زائد | ✅ | `SUP` | وَلَـٰكِن `2:12:5` |
| `STEM:PRON:3FS` | STEM | 189 | 189 | ضمير للغائبة | ✅ | `PRON.STEM.3FS` | هِىَ ۚ `2:68:8` |
| `STEM:V:PERF:ACT:1S` | STEM | 179 | 179 | فعل ماض | ✅ | `V.PERF.ACT` | أَنْعَمْتُ `2:40:6` |
| `STEM:V:IMPF:PASS:3MS` | STEM | 177 | 177 | فعل مضارع مبني للمجهول | ✅ | `V.IMPF.PASS` | يُوصَلَ `2:27:14` |
| `STEM:PRON:1P` | STEM | 176 | 176 | ضمير لجماعة المتكلمين | ✅ | `PRON.STEM.1P` | نَحْنُ `2:11:10` |
| `STEM:PRON:2MS` | STEM | 163 | 163 | ضمير للمخاطب | ✅ | `PRON.STEM.2MS` | إِيَّاكَ `1:5:1` |
| `STEM:PREV` | STEM | 162 | 162 | ما الكافّة | ✅ | `PREV` | إِنَّمَا `2:11:9` |
| `STEM:PRON:1S` | STEM | 151 | 151 | ضمير للمتكلم المفرد | ✅ | `PRON.STEM.1S` | وَإِيَّـٰىَ `2:40:12` |
| `STEM:V:IMPF:PASS:3MP` | STEM | 141 | 141 | فعل مضارع مبني للمجهول | ✅ | `V.IMPF.PASS` | يُنصَرُونَ `2:48:19` |
| `STEM:V:PERF:PASS:3MP` | STEM | 132 | 132 | فعل ماض مبني للمجهول | ✅ | `V.PERF.PASS` | رُزِقُوا۟ `2:25:14` |
| `STEM:RET` | STEM | 122 | 122 | حرف إضراب (بل) | ✅ | `RET` | بَل `2:88:4` |
| `PREFIX:FUT` | PREFIX | 119 | 119 | حرف استقبال | ✅ | `FUT` | وَسَنَزِيدُ `2:58:19` |
| `SUFFIX:PRON:3MD` | SUFFIX | 115 | 115 | ضمير متصل للغائبَين | ✅ | `PRON.SUF.3MD` | كَانَا `2:36:6` |
| `STEM:V:IMPF:PASS:2MP` | STEM | 113 | 113 | فعل مضارع مبني للمجهول | ✅ | `V.IMPF.PASS` | تُرْجَعُونَ `2:28:13` |
| `STEM:EXP` | STEM | 104 | 104 | أداة استثناء (إلّا) | ✅ | `EXP` | إِلَّا `2:32:6` |
| `SUFFIX:PRON:3D` | SUFFIX | 101 | 101 | ضمير متصل للغائبَين | ✅ | `PRON.SUF.3D` | فَأَزَلَّهُمَا `2:36:1` |
| `SUFFIX:PRON:2D` | SUFFIX | 98 | 98 | ضمير متصل للمخاطبَين | ✅ | `PRON.SUF.2D` | وَكُلَا `2:35:7` |
| `STEM:V:PERF:PASS:3FS` | STEM | 93 | 93 | فعل ماض مبني للمجهول | ✅ | `V.PERF.PASS` | أُعِدَّتْ `2:24:12` |
| `STEM:INC` | STEM | 90 | 90 | حرف ابتداء/استفتاح | ✅ | `INC` | أَلَآ `2:12:1` |
| `PREFIX:CAUS` | PREFIX | 88 | 88 | فاء السببية | ✅ | `CAUS` | فَتَكُونَا `2:35:16` |
| `PREFIX:IMPV` | PREFIX | 78 | 78 | لام الأمر | ✅ | `IMPV.PREFIX.LAM` | فَلْيَصُمْهُ ۖ `2:185:17` |
| `STEM:EXL` | STEM | 66 | 66 | حرف تفصيل | ✅ | `EXL` | فَأَمَّا `2:26:12` |
| `STEM:AMD` | STEM | 65 | 65 | حرف استدراك | ✅ | `AMD` | وَلَـٰكِن `2:12:5` |
| `SUFFIX:PRON:2FS` | SUFFIX | 53 | 53 | ضمير متصل للمخاطبة | ✅ | `PRON.SUF.2FS` | ٱصْطَفَىٰكِ `3:42:7` |
| `STEM:V:IMPF:PASS:3FS` | STEM | 53 | 53 | فعل مضارع مبني للمجهول | ✅ | `V.IMPF.PASS` | تُرْجَعُ `2:210:16` |
| `STEM:V:IMPF:ACT:2D` | STEM | 48 | 48 | فعل مضارع | ✅ | `V.IMPF.ACT` | تَقْرَبَا `2:35:13` |
| `STEM:V:IMPF:ACT:3FP` | STEM | 48 | 48 | فعل مضارع | ✅ | `V.IMPF.ACT` | يُؤْمِنَّ ۚ `2:221:5` |
| `STEM:DEM:2MP` | STEM | 48 | 48 | اسم إشارة | ✅ | `DEM` | ذَٰلِكُم `2:49:14` |
| `STEM:INT` | STEM | 47 | 47 | حرف تفسير | ✅ | `INT` | أَن `2:125:16` |
| `STEM:FUT` | STEM | 42 | 42 | حرف استقبال | ✅ | `FUT` | فَسَوْفَ `4:30:6` |
| `STEM:ANS` | STEM | 40 | 40 | حرف جواب | ✅ | `ANS` | بَلَىٰ `2:81:1` |
| `STEM:EXH` | STEM | 40 | 40 | حرف تحضيض | ✅ | `EXH` | لَوْلَا `2:118:5` |
| `STEM:V:PERF:ACT:3MD` | STEM | 38 | 38 | فعل ماض | ✅ | `V.PERF.ACT` | كَانَا `2:36:6` |
| `STEM:V:PERF:ACT:3FP` | STEM | 36 | 36 | فعل ماض | ✅ | `V.PERF.ACT` | تَطَهَّرْنَ `2:222:16` |
| `STEM:V:IMPF:ACT:3MD` | STEM | 35 | 35 | فعل مضارع | ✅ | `V.IMPF.ACT` | يُعَلِّمَانِ `2:102:25` |
| `STEM:SUR` | STEM | 35 | 35 | حرف فجاءة | ✅ | `SUR` | إِذَا `4:77:17` |
| `STEM:AVR` | STEM | 33 | 33 | حرف ردع (كلّا) | ✅ | `AVR` | كَلَّا ۚ `19:79:1` |
| `SUFFIX:PRON:2FP` | SUFFIX | 30 | 30 | ضمير متصل للمخاطبات | ✅ | `PRON.SUF.2FP` | كَيْدِكُنَّ ۖ `12:28:10` |
| `STEM:INL` | STEM | 30 | 30 | حروف مقطّعة (فواتح السور) | ✅ | `INL` | الٓمٓ `2:1:1` |
| `STEM:V:PERF:PASS:2MP` | STEM | 30 | 30 | فعل ماض مبني للمجهول | ✅ | `V.PERF.PASS` | أُحْصِرْتُمْ `2:196:6` |
| `STEM:PRON:3FP` | STEM | 27 | 27 | ضمير للغائبات | ✅ | `PRON.STEM.3FP` | هُنَّ `2:187:8` |
| `STEM:V:IMPV:ACT:2FS` | STEM | 27 | 27 | فعل أمر | ✅ | `V.IMPV.ACT` | ٱقْنُتِى `3:43:2` |
| `STEM:V:PERF:PASS:1S` | STEM | 22 | 22 | فعل ماض مبني للمجهول | ✅ | `V.PERF.PASS` | أُمِرْتُ `6:14:15` |
| `STEM:V:PERF:PASS:1P` | STEM | 22 | 22 | فعل ماض مبني للمجهول | ✅ | `V.PERF.PASS` | رُزِقْنَا `2:25:22` |
| `SUFFIX:PRON:2MD` | SUFFIX | 21 | 21 | ضمير متصل للمخاطبَين | ✅ | `PRON.SUF.2MD` | طَهِّرَا `2:125:17` |
| `STEM:SUP` | STEM | 21 | 21 | حرف زائد | ✅ | `SUP` | مَّا `2:26:8` |
| `STEM:PRON:3D` | STEM | 16 | 16 | ضمير للغائبَين | ✅ | `PRON.STEM.3D` | بِهِمَا ۚ `2:158:17` |
| `STEM:V:IMPV:ACT:2MD` | STEM | 16 | 16 | فعل أمر | ✅ | `V.IMPV.ACT` | طَهِّرَا `2:125:17` |
| `SUFFIX:PRON:3FD` | SUFFIX | 11 | 11 | ضمير متصل للغائبتَين | ✅ | `PRON.SUF.3FD` | ٱلْتَقَتَا ۖ `3:13:7` |
| `STEM:V:IMPF:PASS:2MS` | STEM | 9 | 9 | فعل مضارع مبني للمجهول | ✅ | `V.IMPF.PASS` | تُسْـَٔلُ `2:119:7` |
| `STEM:V:PERF:ACT:3FD` | STEM | 9 | 9 | فعل ماض | ✅ | `V.PERF.ACT` | ٱلْتَقَتَا ۖ `3:13:7` |
| `STEM:V:IMPV:ACT:2FP` | STEM | 8 | 8 | فعل أمر | ✅ | `V.IMPV.ACT` | فَتَعَالَيْنَ `33:28:11` |
| `STEM:PRON:2D` | STEM | 8 | 8 | ضمير للمخاطبَين | ✅ | `PRON.STEM.2D` | لَكُمَا `7:21:3` |
| `STEM:V:IMPF:ACT:2FS` | STEM | 7 | 7 | فعل مضارع | ✅ | `V.IMPF.ACT` | أَتَعْجَبِينَ `11:73:2` |
| `STEM:V:PERF:PASS:2MS` | STEM | 6 | 6 | فعل ماض مبني للمجهول | ✅ | `V.PERF.PASS` | لِنتَ `3:159:5` |
| `STEM:V:IMPF:PASS:1P` | STEM | 6 | 6 | فعل مضارع مبني للمجهول | ✅ | `V.IMPF.PASS` | نُرَدُّ `6:27:9` |
| `PREFIX:EQ` | PREFIX | 6 | 6 | همزة التسوية | ✅ | `EQ` | ءَأَنذَرْتَهُمْ `2:6:6` |
| `STEM:V:PERF:ACT:2FP` | STEM | 6 | 6 | فعل ماض | ✅ | `V.PERF.ACT` | لُمْتُنَّنِى `12:32:4` |
| `SUFFIX:VOC` | SUFFIX | 5 | 5 | ميم عوض عن حرف النداء | ✅ | `VOC.SUFFIX` | ٱللَّهُمَّ `3:26:2` |
| `STEM:V:PERF:ACT:2FS` | STEM | 4 | 4 | فعل ماض | ✅ | `V.PERF.ACT` | كُنتِ `12:29:8` |
| `STEM:V:IMPV:ACT:2D` | STEM | 4 | 4 | فعل أمر | ✅ | `V.IMPV.ACT` | وَكُلَا `2:35:7` |
| `STEM:V:IMPF:ACT:2FP` | STEM | 4 | 4 | فعل مضارع | ✅ | `V.IMPF.ACT` | تُرِدْنَ `33:28:7` |
| `STEM:V:IMPF:PASS:1S` | STEM | 4 | 4 | فعل مضارع مبني للمجهول | ✅ | `V.IMPF.PASS` | أُبْعَثُ `19:33:8` |
| `PREFIX:COM` | PREFIX | 3 | 3 | واو المعية | ✅ | `COM` | وَيَعْلَمَ `3:142:12` |
| `SUFFIX:PRON:2FD` | SUFFIX | 2 | 2 | ضمير متصل للمخاطبتَين | ✅ | `PRON.SUF.2FD` | تَجْرِيَانِ `55:50:3` |
| `STEM:V:IMPF:PASS:3FP` | STEM | 2 | 2 | فعل مضارع مبني للمجهول | ✅ | `V.IMPF.PASS` | يُعْرَفْنَ `33:59:15` |
| `STEM:V:IMPF:ACT:2FD` | STEM | 2 | 2 | فعل مضارع | ✅ | `V.IMPF.ACT` | تَجْرِيَانِ `55:50:3` |
| `STEM:IMPN` | STEM | 2 | 2 | اسم فعل أمر | ✅ | `IMPN` | مِسَاسَ ۖ `20:97:10` |
| `STEM:PRON:2FS` | STEM | 2 | 2 | ضمير للمخاطبة | ✅ | `PRON.STEM.2FS` | لَكِ `3:37:21` |
| `SUFFIX:P` | SUFFIX | 2 | 2 | لام الجر | ✅ | `P.SUFFIX` | فَمَالِ `4:78:30` |
| `STEM:V:PERF:ACT:2D` | STEM | 2 | 2 | فعل ماض | ✅ | `V.PERF.ACT` | شِئْتُمَا `2:35:11` |
| `STEM:V:PERF:PASS:3FD` | STEM | 1 | 1 | فعل ماض مبني للمجهول | ✅ | `V.PERF.PASS` | فَدُكَّتَا `69:14:4` |
| `STEM:V:IMPF:PASS:2MD` | STEM | 1 | 1 | فعل مضارع مبني للمجهول | ✅ | `V.IMPF.PASS` | تُرْزَقَانِهِۦٓ `12:37:5` |
| `STEM:V:PERF:PASS:3FP` | STEM | 1 | 1 | فعل ماض مبني للمجهول | ✅ | `V.PERF.PASS` | أُحْصِنَّ `4:25:36` |
| `STEM:N:GEN:1S` | STEM | 1 | 1 | اسم مجرور مضاف إلى ياء المتكلم | ✅ | `N.GEN.1S` | تَحْتِىٓ ۖ `43:51:15` |
| `STEM:DEM:2FP` | STEM | 1 | 1 | اسم إشارة | ✅ | `DEM` | فَذَٰلِكُنَّ `12:32:2` |
| `STEM:DEM:2D` | STEM | 1 | 1 | اسم إشارة | ✅ | `DEM` | ذَٰلِكُمَا `12:37:12` |
| `STEM:V:IMPF:ACT:3FD` | STEM | 1 | 1 | فعل مضارع | ✅ | `V.IMPF.ACT` | أَتَعِدَانِنِىٓ `46:17:6` |

Legend: ✅ approved-candidate · 🟡 needs-review · ⛔ unsupported-v1.

## 4. Rule catalogue proposal (segment-level)

Aggregated from the 142 tokens into **67 rule families** (person/number variants collapsed). Each rule assigns `i3rab_arabic` to a **single segment**. The display joins per-segment labels with the Arabic comma «، » (so each colored segment shows its own same-colored label).

**Worked example — بِحَمْدِكَ (`2:30:20`):**

| segment | i‘rab (per-segment, colored) |
| --- | --- |
| بِ | حرف جر |
| حَمْدِ | اسم مجرور |
| كَ | ضمير متصل في محل جر مضاف إليه |

Joined display: **حرف جر، اسم مجرور، ضمير متصل في محل جر مضاف إليه** (idiom collapse: «جار ومجرور، والكاف مضاف إليه»).

| rule key | segment target | i‘rab (Arabic) | count (segs) | confidence | example |
| --- | --- | --- | ---: | --- | --- |
| `PRON.SUF.3MP` | SUFFIX:PRON | ضمير متصل (+ تحديد الشخص) | 21,384 | approved-candidate | عَلَيْهِمْ (1:7:4); تُفْسِدُوا۟ (2:11:5) |
| `N.GEN` | STEM:N:GEN | اسم مجرور | 10,404 | approved-candidate | بِسْمِ (1:1:1); تَحْتِىٓ ۖ (43:51:15) |
| `CONJ` | PREFIX:CONJ | حرف عطف | 8,694 | approved-candidate | وَإِيَّاكَ (1:5:3) |
| `V.PERF.ACT` | STEM:V:PERF:ACT | فعل ماض | 8,516 | approved-candidate | خَتَمَ (2:7:1); كَفَرُوا۟ (2:6:3) |
| `DET` | PREFIX:DET | أداة تعريف | 8,377 | approved-candidate | ٱلرَّحْمَـٰنِ (1:1:3) |
| `N.ACC` | STEM:N:ACC | اسم منصوب | 7,955 | approved-candidate | ٱلصِّرَٰطَ (1:6:2) |
| `V.IMPF.ACT` | STEM:V:IMPF:ACT | فعل مضارع | 7,824 | approved-candidate | يَقُولُ (2:8:4); يُؤْمِنُونَ (2:3:2) |
| `P` | STEM:P | حرف جر | 7,679 | approved-candidate | عَلَيْهِمْ (1:7:4) |
| `N.NOM` | STEM:N:NOM | اسم مرفوع | 6,777 | approved-candidate | ٱلْحَمْدُ (1:2:1) |
| `P` | PREFIX:P | حرف جر | 5,325 | approved-candidate | بِسْمِ (1:1:1) |
| `REL` | STEM:REL | اسم موصول | 3,575 | approved-candidate | ٱلَّذِينَ (1:7:2) |
| `PRON.STEM.3MS` | STEM:PRON | ضمير (+ تحديد الشخص) | 3,301 | approved-candidate | بِهِۦ (2:22:13); هُمْ (2:4:11) |
| `REM` | PREFIX:REM | حرف استئناف | 2,925 | approved-candidate | وَمِمَّا (2:3:6) |
| `NEG` | STEM:NEG | حرف نفي | 2,688 | approved-candidate | وَلَا (1:7:8) |
| `ACC` | STEM:ACC | حرف نصب (من أخوات إنّ/النواصب) | 2,283 | approved-candidate | إِنَّ (2:6:1) |
| `V.IMPV.ACT` | STEM:V:IMPV:ACT | فعل أمر | 1,876 | approved-candidate | ٱهْدِنَا (1:6:1); ءَامِنُوا۟ (2:13:4) |
| `T.TIME` | STEM:T | ظرف زمان | 1,166 | approved-candidate | وَإِذَا (2:11:1) |
| `PN.ALLAH.GEN` | STEM:PN:ALLAH:GEN | لفظ الجلالة مجرور | 1,127 | approved-candidate | ٱللَّهِ (1:1:2) |
| `DEM` | STEM:DEM | اسم إشارة | 1,059 | approved-candidate | ذَٰلِكَ (2:2:1); ذَٰلِكُم (2:49:14) |
| `COND` | STEM:COND | أداة شرط | 1,049 | approved-candidate | وَلَوْ (2:20:14) |
| `EMPH.PREFIX` | PREFIX:EMPH | لام التوكيد (المزحلقة) | 1,001 | approved-candidate | لَذَهَبَ (2:20:17) |
| `PN.ALLAH.NOM` | STEM:PN:ALLAH:NOM | لفظ الجلالة مرفوع | 980 | approved-candidate | ٱللَّهُ (2:7:2) |
| `ADJ.NOM` | STEM:ADJ:NOM | صفة مرفوعة | 843 | approved-candidate | عَظِيمٌۭ (2:7:12) |
| `CONJ` | STEM:CONJ | حرف عطف | 756 | approved-candidate | أَمْ (2:6:7) |
| `SUB` | STEM:SUB | حرف مصدري | 684 | approved-candidate | كَمَآ (2:13:5) |
| `LOC` | STEM:LOC | ظرف مكان | 669 | approved-candidate | مَعَكُمْ (2:14:13) |
| `V.PERF.PASS` | STEM:V:PERF:PASS | فعل ماض مبني للمجهول | 634 | approved-candidate | أُنزِلَ (2:4:4); رُزِقُوا۟ (2:25:14) |
| `PN.ALLAH.ACC` | STEM:PN:ALLAH:ACC | لفظ الجلالة منصوب | 592 | approved-candidate | ٱللَّهَ (2:9:2) |
| `ADJ.ACC` | STEM:ADJ:ACC | صفة منصوبة | 590 | approved-candidate | ٱلْمُسْتَقِيمَ (1:6:3) |
| `RES` | STEM:RES | أداة حصر | 558 | approved-candidate | إِلَّآ (2:9:7) |
| `PN.GEN` | STEM:PN:GEN | اسم علم مجرور | 551 | approved-candidate | لِـَٔادَمَ (2:34:5) |
| `ADJ.GEN` | STEM:ADJ:GEN | صفة مجرورة | 528 | approved-candidate | ٱلرَّحْمَـٰنِ (1:1:3) |
| `INTG.PREFIX` | PREFIX:INTG | همزة استفهام | 507 | approved-candidate | أَنُؤْمِنُ (2:13:9) |
| `V.IMPF.PASS` | STEM:V:IMPF:PASS | فعل مضارع مبني للمجهول | 506 | approved-candidate | يُوصَلَ (2:27:14); يُنصَرُونَ (2:48:19) |
| `INTG.STEM` | STEM:INTG | اسم استفهام | 439 | approved-candidate | مَاذَآ (2:26:24) |
| `CERT` | STEM:CERT | حرف تحقيق (قد) | 414 | approved-candidate | قَدْ (2:60:14) |
| `VOC` | PREFIX:VOC | حرف نداء | 371 | approved-candidate | يَـٰٓأَيُّهَا (2:21:1) |
| `RSLT` | PREFIX:RSLT | الفاء الرابطة لجواب الشرط | 350 | approved-candidate | فَأْتُوا۟ (2:23:9) |
| `PN.NOM` | STEM:PN:NOM | اسم علم مرفوع | 341 | approved-candidate | يَـٰٓـَٔادَمُ (2:33:2) |
| `PRO` | STEM:PRO | ضمير منفصل | 332 | approved-candidate | لَا (2:11:4) |
| `PN.ACC` | STEM:PN:ACC | اسم علم منصوب | 320 | approved-candidate | ءَادَمَ (2:31:2) |
| `PRP` | PREFIX:PRP | لام التعليل | 319 | approved-candidate | لِيُحَآجُّوكُم (2:76:18) |
| `CIRC` | PREFIX:CIRC | واو الحال | 293 | approved-candidate | وَمَا (2:8:9) |
| `EMPH.SUFFIX` | SUFFIX:EMPH | نون التوكيد | 243 | approved-candidate | يَأْتِيَنَّكُم (2:38:6) |
| `SUP` | PREFIX:SUP | حرف زائد | 214 | approved-candidate | وَلَـٰكِن (2:12:5) |
| `PREV` | STEM:PREV | ما الكافّة | 162 | approved-candidate | إِنَّمَا (2:11:9) |
| `RET` | STEM:RET | حرف إضراب (بل) | 122 | approved-candidate | بَل (2:88:4) |
| `FUT` | PREFIX:FUT | حرف استقبال | 119 | approved-candidate | وَسَنَزِيدُ (2:58:19) |
| `EXP` | STEM:EXP | أداة استثناء (إلّا) | 104 | approved-candidate | إِلَّا (2:32:6) |
| `INC` | STEM:INC | حرف ابتداء/استفتاح | 90 | approved-candidate | أَلَآ (2:12:1) |
| `CAUS` | PREFIX:CAUS | فاء السببية | 88 | approved-candidate | فَتَكُونَا (2:35:16) |
| `IMPV.PREFIX.LAM` | PREFIX:IMPV | لام الأمر | 78 | approved-candidate | فَلْيَصُمْهُ ۖ (2:185:17) |
| `EXL` | STEM:EXL | حرف تفصيل | 66 | approved-candidate | فَأَمَّا (2:26:12) |
| `AMD` | STEM:AMD | حرف استدراك | 65 | approved-candidate | وَلَـٰكِن (2:12:5) |
| `INT` | STEM:INT | حرف تفسير | 47 | approved-candidate | أَن (2:125:16) |
| `FUT` | STEM:FUT | حرف استقبال | 42 | approved-candidate | فَسَوْفَ (4:30:6) |
| `ANS` | STEM:ANS | حرف جواب | 40 | approved-candidate | بَلَىٰ (2:81:1) |
| `EXH` | STEM:EXH | حرف تحضيض | 40 | approved-candidate | لَوْلَا (2:118:5) |
| `SUR` | STEM:SUR | حرف فجاءة | 35 | approved-candidate | إِذَا (4:77:17) |
| `AVR` | STEM:AVR | حرف ردع (كلّا) | 33 | approved-candidate | كَلَّا ۚ (19:79:1) |
| `INL` | STEM:INL | حروف مقطّعة (فواتح السور) | 30 | approved-candidate | الٓمٓ (2:1:1) |
| `SUP` | STEM:SUP | حرف زائد | 21 | approved-candidate | مَّا (2:26:8) |
| `EQ` | PREFIX:EQ | همزة التسوية | 6 | approved-candidate | ءَأَنذَرْتَهُمْ (2:6:6) |
| `VOC.SUFFIX` | SUFFIX:VOC | ميم عوض عن حرف النداء | 5 | approved-candidate | ٱللَّهُمَّ (3:26:2) |
| `COM` | PREFIX:COM | واو المعية | 3 | approved-candidate | وَيَعْلَمَ (3:142:12) |
| `IMPN` | STEM:IMPN | اسم فعل أمر | 2 | approved-candidate | مِسَاسَ ۖ (20:97:10) |
| `P.SUFFIX` | SUFFIX:P | لام الجر | 2 | approved-candidate | فَمَالِ (4:78:30) |
| `N.GEN.1S` | STEM:N:GEN:1S | اسم مجرور مضاف إلى ياء المتكلم | 1 | approved-candidate | تَحْتِىٓ ۖ (43:51:15) |

## 5. Idiom / phrase-level composition

Patterns where per-segment labels are individually correct, but the combined display reads better as a known idiom. **Recommendation: store per-segment labels in v1 (DB); apply idiom collapses in the read/UI layer**, not in the importer.

| word pattern | per-segment labels | combined display | when |
| --- | --- | --- | --- |
| `P + PRON` (3,888 words) | حرف جر، ضمير متصل | **جار ومجرور** | read-layer |
| `P + N:GEN` (812 `P+N`) | حرف جر، اسم مجرور | **جار ومجرور** | read-layer |
| `P + DET + N:GEN` (765) | حرف جر، أداة تعريف، اسم مجرور | **جار ومجرور** (معرفة) | read-layer |
| `P + N:GEN + PRON` | حرف جر، اسم مجرور، ضمير متصل في محل جر مضاف إليه | **جار ومجرور، والضمير مضاف إليه** | read-layer |
| `N + PRON` (4,022) | اسم + الحالة، ضمير متصل | الضمير **في محل جر مضاف إليه** | read-layer |
| `V + PRON` (7,439) | فعل…، ضمير متصل | الضمير **في محل نصب مفعول به** | read-layer |
| `ACC + PRON` (863) | حرف نصب، ضمير متصل | الضمير **في محل نصب اسم إنّ** | read-layer |
| `INTG + V` (أتجعل `2:30:11`) | همزة استفهام، فعل مضارع | **همزة استفهام + فعل مضارع** | v1 (segment labels) |
| `DET + N` (5,848) | أداة تعريف، اسم + الحالة | **اسم معرفة** + الحالة | v1 (segment labels) |
| `P + SUB` (e.g. كَمَا، كَأَن، عَمَّا) | جار، مجرور | **جار ومجرور** | read-layer (pattern-aware) |
| `SUP + AMD` (e.g. وَلَـٰكِن) | حرف زائد، حرف استدراك | **حرف استدراك** | read-layer (pattern-aware) |
| `ACC + PREV` (e.g. إِنَّمَا) | حرف نصب، ما الكافّة | **كافّة ومكفوفة** | read-layer (pattern-aware) |
| `REM + EXL` (e.g. فَأَمَّا) | حرف استئناف، حرف تفصيل | **حرف استئناف، حرف تفصيل** | read-layer |

The «محل …» role refinements depend on the **preceding** segment's POS (P→جر، N→مضاف إليه، V→مفعول به، ACC/إنّ→اسمها). They are derivable at word level but are **interpretive**, so v1 stores the plain segment label (`ضمير متصل`) and the read-layer adds the role.

**Pattern-aware display for `P + SUB`:** the base `STEM:SUB` label is `حرف مصدري`, but when the word pattern is `P+SUB`, the preposition governs the SUB as a noun-like complement. In this pattern, the pattern-aware segment display replaces the base labels so that each colored segment gets a matching colored iʻab label:

- `P` segment displays: **جار**
- `SUB` segment displays: **مجرور**

This applies to words like `كَمَا` (كَ=P، ما=SUB), `كَأَن` (كَ=P، أن=SUB), `عَمَّا` (عَ=P، ما=SUB). For all other patterns (standalone `SUB`, `CONJ+SUB`, `SUB+NEG`, `INTG+SUP+SUB`, `T+SUB`, `CIRC+SUB`, `REM+SUB`, `ACC+SUB`, `PRP+SUB+NEG`), the base label `حرف مصدري` is used directly. In `CONJ+P+SUB` the CONJ keeps its label while the P+SUB pair uses the pattern-aware overrides: **حرف عطف، جار، مجرور**.

**Pattern-aware display for `SUP + AMD`:** `SUP` is globally approved as `حرف زائد`. In the specific `SUP+AMD` pattern (e.g. `وَلَـٰكِن` where the SUP is the waw preceding لكن), the segment labels are `SUP` ⇒ **حرف زائد** and `AMD` ⇒ **حرف استدراك**. The combined word-level display for `وَلَـٰكِن` is **حرف استدراك** (the `حرف زائد` label is omitted from the combined display since it is grammatically absorbed by the استدراك).

## 6. NULL `form_arabic_normalized` cases (reconfirmed)

- **All NULL-form segments** belong to one signature: **`SUFFIX:PRON:1S`** — the elided 1st-person-singular pronoun with an empty `form_buckwalter`.
- Confirmed total of NULL renders = **208** (per the sibling report and Feature 004 gate `MORPH-SEG-RENDER-TOTAL`); they sit inside the `SUFFIX:PRON:1S` token (whole token = 1,239 segments, of which 208 have an elided/NULL form).
- Example: `2:30:5` (إِنِّى); see also رَبِّ `2:126:4`.
- **Segment label still available:** `ضمير متصل للمتكلم المفرد` (optionally annotated «محذوف/مُقدَّر»). It derives from `pos=PRON` + `PRON:1S`, independent of the missing form.
- **Safe display rule:** when `form_arabic_normalized IS NULL`, render **no segment chip text** (do not fall back to the empty `form_buckwalter`); show **only the i‘rab label** with a «محذوف» marker, or attach it to the preceding stem.
- **Confirmed:** Feature 005 must **not** invent a `form_arabic_normalized` for these rows — it adds an i‘rab label only.

## 7. Problematic / needs-review labels

From the complete inventory, these POS/rules need an Arabic-label or interpretation decision before user display. Several are **seed mislabels** in `quran_pos_tags` discovered against real usage.

| POS | seed `arabic_label` | observed usage (real forms) | count | proposed Arabic display | decision |
| --- | --- | --- | ---: | --- | --- |
| `T` | تاء تأنيث | إذا، إذ، لمّا، يوم، بعد | 1,166 | ظرف زمان / أداة شرط (+ الحالة) | approved — **seed wrong**, relabel |
| `INL` | قسم | الٓمٓ، حمٓ، الٓر، طسٓمٓ | 30 | حروف مقطّعة (فواتح السور) | approved — **seed wrong**, relabel |
| `REM` | حرف استثناء | وَ، فَ (resumption) | 2,925 | حرف استئناف | approved-candidate — **seed wrong**, relabel |
| `RES` | حرف ردع | إلّا | 558 | أداة حصر | approved — seed imprecise, relabel |
| `AMD` | حرف عطف / نفي | لٰكن | 65 | حرف استدراك | approved — seed imprecise, relabel |
| `PREV` | حرف تحضيض | ما (الكافّة) | 162 | ما الكافّة | approved — **seed wrong**, relabel |
| `SUB` | اسم مبهم | أن، ما، لو | 684 | حرف مصدري | approved — base label `حرف مصدري`; `P+SUB` patterns handled by pattern-aware display as `جار، مجرور` |
| `INT` | حرف تفسير | أن (المفسِّرة) | 47 | حرف تفسير | approved — simplified display label (internal note: أنْ المفسِّرة) |
| `INC` | حرف ابتداء | حتّى، ألا، بل | 90 | حرف ابتداء/استفتاح | approved |
| `EXL` | حرف تعليل | أمّا، إمّا | 66 | حرف تفصيل | approved — simplified display label |
| `EXH` | حرف تحضيض | لولا، هلّا | 40 | حرف تحضيض | approved — simplified display label (internal note: لولا/هلّا) |
| `SUR` | إذا الفجائية | إذًا، إذا | 35 | حرف فجاءة | approved — simplified display label (internal note: إذا الفجائية) |
| `SUP` | حرف زائد | فَ، وَ، ما | 235 | حرف زائد | approved — observed forms include `ف`، `و`، `ما`; global label is `حرف زائد`, not form-specific like `واو زائدة` |
| `EQ` | حرف تسوية | أ، ء | 6 | همزة التسوية | approved |
| `COM` | واو المعية | وَ | 3 | واو المعية | approved |
| `VOC` (suffix) | حرف نداء | ميم (اللَّهُمَّ) | 5 | ميم عوض عن حرف النداء | approved (internal note: vocative suffix in اللَّهُمَّ) |
| `P` (suffix) | حرف جر | لِ (في فَمَالِ) | 2 | لام الجر | approved — refined label (internal note: suffix preposition after interrogative ما) |
| `N` (GEN+1S) | اسم مجرور | تحتى | 1 | اسم مجرور مضاف إلى ياء المتكلم | approved — refined label (internal note: الياء ضمير متصل في محل جر بالإضافة) |
| `IMPN` | اسم فعل أمر | مساس، هاؤم | 2 | اسم فعل أمر | approved (rare) |
| `INTG` (stem) | حرف استفهام | ما، ماذا، من | 439 | اسم استفهام | approved — relabel |
| `IMPV` (prefix) | فعل أمر | لْ (في فليفعل) | 78 | لام الأمر | approved-candidate — relabel for prefix |

> **Recommendation (same as sibling report):** the Feature 005 **rule layer owns user-facing Arabic labels** and does not blindly reuse `quran_pos_tags.arabic_label`. This ships correct Arabic without editing Feature 004 data.

## 8. Coverage strategy (v1)

**Support immediately (approved-candidate, 100.0% of segment rows — 128,219/128,219):** all **142 segment-token signatures** including noun/adjective/PN case (اسم/صفة/اسم علم + مرفوع/منصوب/مجرور), لفظ الجلالة + case, verb tense + voice (ماض/مضارع/أمر + مبني للمجهول), attached/independent pronouns (ضمير متصل/منفصل + person), حرف مصدري (SUB), أداة حصر (RES), اسم استفهام (STEM:INTG), حرف استدراك (AMD), حرف زائد (SUP), ما الكافّة (PREV), حرف ابتداء/استفتاح (INC), حرف تفصيل (EXL), حرف تفسير (INT), حرف تحضيض (EXH), حرف فجاءة (SUR), حروف مقطّعة (INL), همزة التسوية (EQ), ميم عوض عن حرف النداء (VOC.SUFFIX), واو المعية (COM), لام الجر (P.SUFFIX), اسم مجرور مضاف إلى ياء المتكلم (N.GEN.1S), and the clear particles (حرف جر، حرف عطف، أداة تعريف، حرف نفي، اسم موصول، اسم إشارة، همزة استفهام، حرف نصب، أداة شرط، حرف تحقيق، لام التوكيد، حرف نداء، حرف استقبال، الفاء الرابطة، واو الحال، لام التعليل، فاء السببية، حرف إضراب، حرف ردع، حرف استئناف، أداة استثناء).

**Mark needs-review (0.0% of segment rows):** none — all 128,219 segment rows map to an approved-candidate rule.

**Read-layer interpretive refinements (not counted in segment-row coverage):** ضمير محلّ role in `V+PRON` (مفعول به) and `ACC+PRON` (اسم إنّ) — see §5.

**Set unsafe-v1 (0% — none structural):** every segment resolves to an approved-candidate label; the only items to **not display as i‘rab** are the syntactic **roles** (فاعل/مفعول به/مبتدأ/خبر/حال) — not derivable from morphology.

| Coverage metric | approved-only | approved+needs-review |
| --- | ---: | ---: |
| Segment coverage | 100.0% | 100.0% |
| Word display coverage | 100.0% | 100.0% |

> *Reconciled to 100%: since all 142 segment-token signatures are approved-candidate (0% needs-review),
> every word is fully-approved from approved segment labels, so the approved-only and
> approved+needs-review columns are equal. The earlier 95.33% reflected the pre-finalization state when a
> few segment tokens were still needs-review. Read-layer role refinements in `V+PRON`/`ACC+PRON` (§5) are
> interpretive, not approval gaps.*

**No remaining segment-token gaps.** Read-layer attached-pronoun role refinements (`V+PRON`, `ACC+PRON`) remain separate from segment-row coverage.

## 9. Data-model implication (validated against the full inventory)

The full pattern set **confirms** the intended model:

- **Inline `i3rab_*` columns on `quran_word_morphology_segments`** (`i3rab_arabic`, `i3rab_rule_id`, `i3rab_status`, `i3rab_review_reason`). `i3rab_status` is a 3-value enum (`approved` / `needs_review` / `unsupported`); in v1 every row is `approved` (`needs_review`/`unsupported` are schema-reserved). Segment i‘rab is strictly 1:1 with a segment and there are only **142 distinct segment signatures** → a per-segment column + a small rule table is the right grain. **No `quran_word_segment_i3rab` table.**
- **Keep `quran_i3rab_rules`** — the 67-family catalogue (§4) is small, curated, and gives every label an FK + provenance + coverage reporting.
- **No `quran_word_i3rab` in v1.** The inventory shows word summaries are a deterministic ordered join of segment labels (+ a handful of read-layer idiom collapses, §5). With 1337 enriched word patterns but only 142 segment tokens, materializing word summaries would duplicate derivable data. **Compose at read time** with «، »; promote to a table only if word-level filtering/idioms become a hard requirement.
- **Per-occurrence grain** (segment `id`): case/tense are contextual, so i‘rab must not key to the imlaei-simple identity group.

## 10. Final recommendation

- **Total patterns found:** 358 POS-only · 371 kind+POS · 1337 enriched signatures · 142 segment tokens · 67 proposed rule families.
- **Recommended v1 supported set:** the approved-candidate rule families (§4/§8) — all **142 segment-token signatures** / **67 rule families**, including noun/adj/PN case, لفظ الجلالة, verb tense+voice, pronouns, ظرف زمان (T), حرف مصدري (SUB), أداة حصر (RES), اسم استفهام (STEM:INTG), حرف استدراك (AMD), حرف زائد (SUP), ما الكافّة (PREV), حرف ابتداء/استفتاح (INC), حرف تفصيل (EXL), حرف تفسير (INT), حرف تحضيض (EXH), حرف فجاءة (SUR), حروف مقطّعة (INL), همزة التسوية (EQ), ميم عوض عن حرف النداء (VOC.SUFFIX), واو المعية (COM), لام الجر (P.SUFFIX), اسم مجرور مضاف إلى ياء المتكلم (N.GEN.1S), حرف استئناف (REM), and the clear particles. Covers **100.0%** of segment rows (128,219/128,219) and **100.0%** of words fully (every word's segments are approved).
- **Recommended needs-review set (segment rows only):** **empty** — no segment-token needs-review items remain.
- **Read-layer notes (not segment-row coverage):** attached-pronoun role refinements in `V+PRON` and `ACC+PRON` patterns (§5).
- **Recommended unsupported-v1 set:** syntactic **roles** only (فاعل/مفعول به/مبتدأ/خبر/حال). No segment label is unsupported.
- **Recommended UI examples (colored segment + i‘rab):** بِحَمْدِكَ `2:30:20` (P+N+PRON), وَبِٱلْيَوْمِ `2:8:7` (4-segment prefix stack), أَتَجْعَلُ `2:30:11` (INTG+V), عَلَيْهِمْ `1:7:4` (جار ومجرور), ٱلْمُفْلِحُونَ `2:5:8` (DET+N NOM), ٱللَّهِ/ٱللَّهُ/ٱللَّهَ `1:1:2`/`2:255:1`/`2:9:2` (لفظ الجلالة GEN/NOM/ACC), أُنزِلَ `2:4:4` (passive), رَبِّ `2:126:4` (NULL 1S pronoun).
- **Recommended updates to the Feature 005 planning report before `/speckit.specify`:** (1) adopt the 67-family **segment-level** rule catalogue as the spec's rule basis; (2) record the **seed-label corrections** (T→ظرف زمان, SUB→حرف مصدري, RES→أداة حصر, INTG-stem→اسم استفهام, AMD→حرف استدراك, SUP→حرف زائد, PREV→ما الكافّة, EXL→حرف تفصيل, INT→حرف تفسير, EXH→حرف تحضيض, SUR→حرف فجاءة, INL, REM, VOC.SUFFIX, COM, P.SUFFIX→لام الجر, N.GEN.1S) and the rule-layer-owns-labels decision; (3) lock the **idiom collapses as read-layer behavior** (not importer), including the `P+SUB` pattern-aware override (`جار، مجرور`), the `SUP+AMD` pattern-aware override (segment labels `حرف زائد، حرف استدراك`; combined display `حرف استدراك`), and the `ACC+PREV` pattern-aware override (segment labels `حرف نصب، ما الكافّة`; combined display `كافّة ومكفوفة`); (4) confirm inline `i3rab_*` columns + `quran_i3rab_rules`, no word summary table in v1; (5) cite this report's coverage numbers as the spec's success-criteria baseline.

### Quranic data safety
Read-only inspection; individual word forms and derived segment renderings shown for illustration, never assembled ayah text, never invented grammar. Unsupported items are recorded, not guessed. Simplified labels are **not** authoritative scholarly i‘rab.

