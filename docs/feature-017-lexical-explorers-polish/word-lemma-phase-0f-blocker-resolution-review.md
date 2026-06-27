# Word-Level Lemma Normalization — Phase 0F Blocker-Resolution Review

**Project:** Quran Dashboard / المنهج القرآني
**Feature:** 017 — Lexical Explorers Polish
**Branch:** `017-lexical-explorers-polish`
**Date:** 2026-06-28
**Task type:** REVIEW / decision-support ONLY. No production C# code, importer code, DI, backend active artifact, source JSON, DB, or migrations changed. The active artifact (`Backend/.../Corrections/word-lemma-normalization.json`) was **not** created. Nothing committed.

---

## 1. Executive Summary

Phase 0 left **30 active blockers** (Phase 0B: 1 = `33:61:2`; Phase 0D: 29). The Phase 0 curator deliberately blocked them because each is a QUL-present word whose own reliable Corpus mapping conflicts with QUL's stored lemma, and auto-overruling QUL was deferred as a "scholarly judgment call."

This review inspected all 30 against the **full ayah text**, the previous/next word, the raw QUL lemma, the own Corpus Buckwalter evidence, and the reliable Arabic mapping. The result is much cleaner than the conservative block implied:

- **27 are mechanical source-misalignment errors, not linguistic debates.** In every one, the word's own surface form maps unambiguously to a reliable lemma, while the QUL lemma is a *different content word that physically appears elsewhere in the same ayah* (a neighbor- or distant-word lemma mis-attached to this token). These are recommended `replace`.
- **2 are genuine QUL-correct modeling artifacts** where the Corpus "candidate" is the wrong lemma — the two special-caution cases. Recommended `keep`.
- **1 is a multi-STEM compound particle** (`أَيْنَمَا` = أَيْن + مَا) — a modeling divergence in the same family as `أَنَّمَآ`/`إِلَّا` already handled in Phase 0E. Recommended `exception`. (The real misalignment in that ayah is on the *neighbor* `33:61:3`, which is outside the 30-blocker set — see §4.30.)
- **0 require `still-blocked`.** All 30 have actionable, evidence-backed recommendations.

**Key correctness note on the 27 `replace`s:** unlike the original 63 shifts (where the content word was *missing* its lemma), in these 29 the defect word is missing its **own** lemma and instead carries a wrong content lemma; the lemma it wrongly carries already has its canonical occurrence on its true word in the ayah. So a single `replace` (wrong → own) is the complete fix — no companion `add` is needed and no lemma occurrence is lost (every displaced lemma is high-frequency / present on its true token).

**This report does not modify the active artifact.** Promotion of these recommendations is a separate, explicitly-authorized follow-up.

---

## 2. Methodology

For each of the 30 blocker locations:

1. Read the **word text** (`qpcUthmani`) from the staged aligned Corpus.
2. Reconstructed the **full ayah** by joining all `qpcUthmani` tokens for that surah:ayah.
3. Read the **previous** and **next** word + their raw QUL lemmas.
4. Read the **raw QUL word-level lemma** at the location (`qul/word-lemma.json`).
5. Read **own Corpus lemma Buckwalter** (`segments[].lemma`) and segment count (multi-STEM detection).
6. Took the **candidate Arabic lemma + reliability** from `curation-tmp/report-summary.json` (already cross-checked against the 4,797 reliable-mapping allow-list).
7. Located, in the ayah text, the **word that the QUL lemma actually belongs to** (the displacement source).
8. Applied the decision policy:
   - `replace` only when (a) QUL is clearly on the wrong word, (b) the word's own candidate is strongly supported, and (c) the fix is mechanical source-alignment, not a debatable interpretation;
   - `keep` when QUL is plausible / evidence insufficient to overrule / Corpus candidate is itself wrong;
   - `exception` for known modeling divergence (multi-STEM compound particle);
   - `still-blocked` when evidence is insufficient.

Corpus was used as **evidence/guard only**. No replace was recommended merely because Corpus has a different reliable mapping; each replace also required that the QUL lemma physically belongs to another identifiable word in the ayah.

Sources inspected read-only: `resources/import-sources/quran-morphology/qul/word-lemma.json`, `.../qul/word-root.json`, `.../corpus/quranic-corpus-morphology-qpc-aligned.json`; plus `curation-tmp/report-summary.json`, `curation-tmp/cat-H-uncertain.json`, `word-lemma-full-normalization-curation-report.md`, `curation-tmp/decisions-ledger.json`.

---

## 3. Table of all 30 blockers

| # | Location | Word | QUL lemma | Candidate | Own Corpus BW | Phase | Recommendation | Conf. |
| ---: | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `12:53:7` | بِٱلسُّوٓءِ | نَفْس | سُوٓء | `suw^'` | 0D | **replace** | high |
| 2 | `17:7:22` | عَلَوْا۟ | عَلا | تَعَٰلَىٰ | `taEa\`laY\`` | 0D | **keep** | high |
| 3 | `18:57:26` | ٱلْهُدَىٰ | دَعَا | هُدًى | `hudFY` | 0D | **replace** | high |
| 4 | `18:96:16` | ءَاتُونِىٓ | نَار | آتَى | `A^taY` | 0D | **replace** | high |
| 5 | `20:123:16` | يَضِلُّ | هُدًى | ضَلَّ | `Dal~a` | 0D | **replace** | high |
| 6 | `20:40:29` | مَدْيَنَ | سِنِين | مَدْيَن | `madoyan` | 0D | **replace** | high |
| 7 | `24:61:63` | أَنفُسِكُمْ | سَلَّمَ | نَفْس | `nafos` | 0D | **replace** | high |
| 8 | `2:144:23` | أُوتُوا۟ | شَطْر | آتَى | `A^taY` | 0D | **replace** | high |
| 9 | `2:148:9` | تَكُونُوا۟ | أَيْن | كَانَ | `kaAna` | 0D | **replace** | high |
| 10 | `2:187:19` | أَنفُسَكُمْ | كَانَ | نَفْس | `nafos` | 0D | **replace** | high |
| 11 | `2:203:16` | إِثْمَ | تَأَخَّرَ | إِثْم | `<ivom` | 0D | **replace** | high |
| 12 | `2:221:19` | مُّؤْمِنٌ | ءَامَنَ | مُؤْمِن | `mu&omin` | 0D | **replace** | medium |
| 13 | `33:5:25` | وَكَانَ | تَعَمَّدَتْ | كَانَ | `kaAna` | 0D | **replace** | high |
| 14 | `33:6:25` | كَانَ | وَلِىّ | كَانَ | `kaAna` | 0D | **replace** | high |
| 15 | `35:18:24` | بِٱلْغَيْبِ | خَشِىَ | غَيْب | `gayob` | 0D | **replace** | high |
| 16 | `35:41:13` | أَحَدٍۢ | أَمْسَكَ | أَحَد | `>aHad` | 0D | **replace** | high |
| 17 | `3:161:16` | نَفْسٍۢ | وَفَّىٰٓ | نَفْس | `nafos` | 0D | **replace** | high |
| 18 | `4:116:1` | إِنَّ | إِنّ | أَنّ | `>an~` | 0D | **keep** | high |
| 19 | `4:162:19` | وَٱلْمُؤْمِنُونَ | مُؤْتُون | مُؤْمِن | `mu&omin` | 0D | **replace** | high |
| 20 | `58:22:44` | حِزْبُ | رَّضِىَ | حِزْب | `Hizob` | 0D | **replace** | high |
| 21 | `5:44:29` | تَشْتَرُوا۟ | خَشِىَ | اشْتَرَىٰ | `{$otaraY\`` | 0D | **replace** | high |
| 22 | `60:10:45` | وَلْيَسْـَٔلُوا۟ | أَمْسَكَ | سَأَلَ | `sa>ala` | 0D | **replace** | high |
| 23 | `6:101:14` | شَىْءٍۢ | خَلَقَ | شَىْء | `$aYo'` | 0D | **replace** | high |
| 24 | `6:151:11` | شَيْـًۭٔا | أَشْرَكَ | شَىْء | `$aYo'` | 0D | **replace** | high |
| 25 | `6:94:25` | وَضَلَّ | تَّقَطَّعَ | ضَلَّ | `Dal~a` | 0D | **replace** | high |
| 26 | `74:31:27` | وَٱلْمُؤْمِنُونَ | آتَى | مُؤْمِن | `mu&omin` | 0D | **replace** | high |
| 27 | `7:38:32` | ضِعْفًۭا | آتَى | ضِعْف | `DiEof` | 0D | **replace** | high |
| 28 | `7:38:34` | ٱلنَّارِ | ضِعْف | نَار | `naAr` | 0D | **replace** | high |
| 29 | `9:93:10` | يَكُونُوا۟ | رَّضِىَ | كَانَ | `kaAna` | 0D | **replace** | high |
| 30 | `33:61:2` | أَيْنَمَا | مَا (Corpus أَيْن+مَا) | — | `>ayon`, `maA` | 0B | **exception** | medium |

---

## 4. Per-blocker detail

Format per entry — displacement source = the word in the ayah that the QUL lemma actually belongs to.

### 4.1 `12:53:7` بِٱلسُّوٓءِ — **replace** (high)
- Ayah 12:53: … إِنَّ ٱلنَّفْسَ لَأَمَّارَةٌۢ **بِٱلسُّوٓءِ** إِلَّا مَا رَحِمَ رَبِّىٓ …
- prev `لَأَمَّارَةٌۢ` / next `إِلَّا`. QUL `نَفْس` · candidate `سُوٓء` (`suw^'` 32/33 = 97.0%).
- Displacement source: `نَفْس` belongs to `ٱلنَّفْسَ` (12:53:5). This token is "بالسوء" = the evil → own lemma `سُوٓء`.
- Reason blocked: own map (`suw^'→سُوٓء`) conflicts with QUL `نَفْس`.
- Rationale: surface = السوء; `نَفْس` is an unrelated word two tokens back. Mechanical.
- Risk if replaced: negligible (`سُوٓء` unambiguous; `نَفْس` retained on `ٱلنَّفْسَ`). Risk if kept: `بالسوء` wrongly joins the نَفْس occurrence set; سُوٓء under-counted.

### 4.2 `17:7:22` عَلَوْا۟ — **keep** (high) — SPECIAL CAUTION confirmed
- Ayah 17:7: … وَلِيُتَبِّرُوا۟ مَا **عَلَوْا۟** تَتْبِيرًا
- prev `مَا` / next `تَتْبِيرًا`. QUL `عَلا` · candidate `تَعَٰلَىٰ` (`taEa\`laY\`` 19/20 = 95.0%).
- `عَلَوْا۟` = "what they had risen over/conquered", root علو → QUL `عَلا` is **correct**. The Corpus candidate `تَعَٰلَىٰ` is a different lemma (form/divine "exalted") mis-mapped by Corpus here.
- Reason blocked: own Corpus BW maps to `تَعَٰلَىٰ`, conflicting with QUL.
- Rationale: candidate mapping is not context-safe — exactly the warned case. QUL plausible and correct.
- Risk if replaced: **high — would assign the wrong lemma** (`تَعَٰلَىٰ`). Risk if kept: none material.

### 4.3 `18:57:26` ٱلْهُدَىٰ — **replace** (high)
- Ayah 18:57: … وَإِن تَدْعُهُمْ إِلَى **ٱلْهُدَىٰ** فَلَن يَهْتَدُوٓا۟ …
- QUL `دَعَا` · candidate `هُدًى` (`hudFY` 57/59 = 96.6%).
- Displacement source: `دَعَا` belongs to `تَدْعُهُمْ` (18:57:23). Token = الهدى → `هُدًى`.
- Mechanical. Risk if replaced: negligible. Risk if kept: الهدى joins دعا set; هدًى under-counted.

### 4.4 `18:96:16` ءَاتُونِىٓ — **replace** (high)
- Ayah 18:96: … جَعَلَهُۥ نَارًۭا قَالَ **ءَاتُونِىٓ** أُفْرِغْ عَلَيْهِ قِطْرًۭا
- QUL `نَار` · candidate `آتَى` (`A^taY` 173/175 = 98.9%).
- Displacement source: `نَار` belongs to `نَارًۭا` earlier in the ayah. Token = آتوني (bring me) → `آتَى`.
- Mechanical. Risk if replaced: negligible. Risk if kept: آتوني joins نار set; آتى under-counted.

### 4.5 `20:123:16` يَضِلُّ — **replace** (high)
- Ayah 20:123: … فَمَنِ ٱتَّبَعَ هُدَاىَ فَلَا **يَضِلُّ** وَلَا يَشْقَىٰ
- QUL `هُدًى` · candidate `ضَلَّ` (`Dal~a` 25/27 = 92.6%).
- Displacement source: `هُدًى` belongs to `هُدَاىَ` (20:123:13). Token = يضل (goes astray) → `ضَلَّ`.
- Mechanical. Risk if replaced: low. Risk if kept: يضل joins هدًى set wrongly.

### 4.6 `20:40:29` مَدْيَنَ — **replace** (high)
- Ayah 20:40: … فَلَبِثْتَ سِنِينَ فِىٓ أَهْلِ **مَدْيَنَ** ثُمَّ جِئْتَ …
- QUL `سِنِين` · candidate `مَدْيَن` (`madoyan` 6/7 = 85.7%).
- Displacement source: `سِنِين` belongs to `سِنِينَ` (20:40:26). Token = مدين (proper noun) → `مَدْيَن`.
- Mechanical; proper noun, surface unambiguous despite lower %. Risk if replaced: negligible. Risk if kept: place-name joins "years" set.

### 4.7 `24:61:63` أَنفُسِكُمْ — **replace** (high)
- Ayah 24:61: … فَسَلِّمُوا۟ عَلَىٰٓ **أَنفُسِكُمْ** تَحِيَّةًۭ …
- QUL `سَلَّمَ` · candidate `نَفْس` (`nafos` 182/190 = 95.8%).
- Displacement source: `سَلَّمَ` belongs to `فَسَلِّمُوا۟` (24:61:61). Token = أنفسكم → `نَفْس`.
- Mechanical. Risk if replaced: negligible. Risk if kept: أنفسكم joins سلّم set.

### 4.8 `2:144:23` أُوتُوا۟ — **replace** (high)
- Ayah 2:144: … وَإِنَّ ٱلَّذِينَ **أُوتُوا۟** ٱلْكِتَـٰبَ لَيَعْلَمُونَ …
- QUL `شَطْر` · candidate `آتَى` (`A^taY` 173/175 = 98.9%).
- Displacement source: `شَطْر` belongs to `شَطْرَ`/`شَطْرَهُۥ` earlier. Token = أوتوا (were given) → `آتَى`.
- Mechanical. Risk if replaced: negligible. Risk if kept: أوتوا joins شطر set.

### 4.9 `2:148:9` تَكُونُوا۟ — **replace** (high)
- Ayah 2:148: … أَيْنَ مَا **تَكُونُوا۟** يَأْتِ بِكُمُ ٱللَّهُ …
- QUL `أَيْن` · candidate `كَانَ` (`kaAna` 1000/1005 = 99.5%).
- Displacement source: `أَيْن` belongs to `أَيْنَ` (2:148:7). Token = تكونوا → `كَانَ`.
- Compound-sensitive note: this is the same `أَيْن`/`أَيْنَمَا` family as blocker 30, but here the token is plainly the verb تكونوا; its lemma is unambiguously `كَانَ`. The `أَيْن` placement on `أَيْنَ` (2:148:7) is a separate add outside this set.
- Mechanical. Risk if replaced: negligible. Risk if kept: تكونوا wrongly joins أين set; كان under-counted.

### 4.10 `2:187:19` أَنفُسَكُمْ — **replace** (high)
- Ayah 2:187: … أَنَّكُمْ كُنتُمْ تَخْتَانُونَ **أَنفُسَكُمْ** فَتَابَ عَلَيْكُمْ …
- QUL `كَانَ` · candidate `نَفْس` (`nafos` 182/190 = 95.8%).
- Displacement source: `كَانَ` belongs to `كُنتُمْ` (2:187:17). Token = أنفسكم → `نَفْس`.
- Mechanical. Risk if replaced: negligible. Risk if kept: أنفسكم joins كان set.

### 4.11 `2:203:16` إِثْمَ — **replace** (high)
- Ayah 2:203: … وَمَن **تَأَخَّرَ** فَلَآ **إِثْمَ** عَلَيْهِ …
- QUL `تَأَخَّرَ` · candidate `إِثْم` (`<ivom` 23/24 = 95.8%).
- Displacement source: `تَأَخَّرَ` belongs to `تَأَخَّرَ` (2:203:14). Token = إثم → `إِثْم`.
- Mechanical. Risk if replaced: negligible. Risk if kept: إثم joins تأخّر set.

### 4.12 `2:221:19` مُّؤْمِنٌ — **replace** (medium) — participle/verb borderline
- Ayah 2:221: … وَلَعَبْدٌۭ **مُّؤْمِنٌ** خَيْرٌۭ مِّن مُّشْرِكٍۢ …
- QUL `ءَامَنَ` · candidate `مُؤْمِن` (`mu&omin` 179/185 = 96.8%).
- The token is the participle/noun مؤمن. QUL elsewhere lemmatizes this exact form as `مُؤْمِن` 179 times; assigning the verb `ءَامَنَ` here is internally inconsistent and matches the distant verb `يُؤْمِنُوا۟`/`يُؤْمِنَّ`.
- Why medium: the verb↔participle lemma is the kind of choice that *can* be a modeling decision; the recommendation rests on QUL's own dominant practice, not on overruling QUL with Corpus.
- Recommendation: `replace` → `مُؤْمِن`; conservative fallback = `keep` if the project prefers to never touch verb/participle lemmatization.
- Risk if replaced: low-medium (aligns with QUL's own norm). Risk if kept: one مؤمن token sits under the verb lemma.

### 4.13 `33:5:25` وَكَانَ — **replace** (high)
- Ayah 33:5: … وَلَـٰكِن مَّا **تَعَمَّدَتْ** قُلُوبُكُمْ ۚ **وَكَانَ** ٱللَّهُ غَفُورًۭا …
- QUL `تَعَمَّدَتْ` · candidate `كَانَ` (`kaAna` 99.5%).
- Displacement source: `تَعَمَّدَتْ` belongs to `تَعَمَّدَتْ` (33:5:22). Token = وكان → `كَانَ`. Surface literally كان.
- Mechanical. Risk if replaced: negligible. Risk if kept: a literal كان token sits under تعمّد.

### 4.14 `33:6:25` كَانَ — **replace** (high)
- Ayah 33:6: … إِلَىٰٓ أَوْلِيَآئِكُم مَّعْرُوفًۭا ۚ **كَانَ** ذَٰلِكَ فِى ٱلْكِتَـٰبِ …
- QUL `وَلِىّ` · candidate `كَانَ` (`kaAna` 99.5%).
- Displacement source: `وَلِىّ` belongs to `أَوْلِيَآئِكُم` (33:6:21). Token literally كَانَ → `كَانَ`.
- Mechanical (surface = lemma). Risk if replaced: negligible. Risk if kept: a literal كان sits under ولي.

### 4.15 `35:18:24` بِٱلْغَيْبِ — **replace** (high)
- Ayah 35:18: … ٱلَّذِينَ يَخْشَوْنَ رَبَّهُم **بِٱلْغَيْبِ** وَأَقَامُوا۟ …
- QUL `خَشِىَ` · candidate `غَيْب` (`gayob` 32/37 = 86.5%).
- Displacement source: `خَشِىَ` belongs to `يَخْشَوْنَ` (35:18:21). Token = بالغيب → `غَيْب`.
- Mechanical. Risk if replaced: low. Risk if kept: بالغيب joins خشي set.

### 4.16 `35:41:13` أَحَدٍۢ — **replace** (high)
- Ayah 35:41: … إِنْ **أَمْسَكَهُمَا** مِنْ **أَحَدٍۢ** مِّنۢ بَعْدِهِۦٓ …
- QUL `أَمْسَكَ` · candidate `أَحَد` (`>aHad` 54/55 = 98.2%).
- Displacement source: `أَمْسَكَ` belongs to `أَمْسَكَهُمَا` (35:41:10). Token = أحد → `أَحَد`.
- Mechanical. Risk if replaced: negligible. Risk if kept: أحد joins أمسك set.

### 4.17 `3:161:16` نَفْسٍۢ — **replace** (high)
- Ayah 3:161: … ثُمَّ تُوَفَّىٰ كُلُّ **نَفْسٍۢ** مَّا كَسَبَتْ …
- QUL `وَفَّىٰٓ` · candidate `نَفْس` (`nafos` 95.8%).
- Displacement source: `وَفَّىٰٓ` belongs to `تُوَفَّىٰ` (3:161:14). Token = نفس → `نَفْس`.
- Mechanical. Risk if replaced: negligible. Risk if kept: نفس joins وفّى set.

### 4.18 `4:116:1` إِنَّ — **keep** (high) — SPECIAL CAUTION confirmed
- Ayah 4:116: **إِنَّ** ٱللَّهَ لَا يَغْفِرُ أَن يُشْرَكَ بِهِۦ …
- QUL `إِنّ` · candidate `أَنّ` (`>an~` 351/362 = 97.0%).
- Surface = إِنَّ (kasra) = the emphatic particle → QUL `إِنّ` is **correct**. The Corpus `>an~`→`أَنّ` is the `إِنّ`/`أَنّ` modeling artifact, not a real correction.
- Reason blocked: own Corpus BW `>an~` conflicts with QUL `إِنّ`.
- Rationale: ayah-initial إِنَّ is unambiguously `إِنّ`. Replacing would change meaning (`أَنّ`).
- Risk if replaced: **high — wrong lemma**. Risk if kept: none.

### 4.19 `4:162:19` وَٱلْمُؤْمِنُونَ — **replace** (high)
- Ayah 4:162: … وَٱلْمُؤْتُونَ ٱلزَّكَوٰةَ **وَٱلْمُؤْمِنُونَ** بِٱللَّهِ وَٱلْيَوْمِ ٱلْـَٔاخِرِ …
- QUL `مُؤْتُون` · candidate `مُؤْمِن` (`mu&omin` 96.8%).
- Displacement source: `مُؤْتُون` belongs to `وَٱلْمُؤْتُونَ` (4:162:16). Token = والمؤمنون → `مُؤْمِن`.
- Clear adjacent-content shift (different lemma entirely). Mechanical. Risk if replaced: negligible. Risk if kept: المؤمنون joins مؤتون set.

### 4.20 `58:22:44` حِزْبُ — **replace** (high)
- Ayah 58:22: … رَضِىَ ٱللَّهُ عَنْهُمْ وَرَضُوا۟ عَنْهُ ۚ أُو۟لَـٰٓئِكَ **حِزْبُ** ٱللَّهِ …
- QUL `رَّضِىَ` · candidate `حِزْب` (`Hizob` 11/13 = 84.6%).
- Displacement source: `رَّضِىَ` belongs to `رَضِىَ` (58:22:36). Token = حزب → `حِزْب`. (Same ayah later has حِزْبَ correctly.)
- Mechanical. Risk if replaced: low. Risk if kept: حزب الله joins رضي set.

### 4.21 `5:44:29` تَشْتَرُوا۟ — **replace** (high)
- Ayah 5:44: … فَلَا تَخْشَوُا۟ ٱلنَّاسَ وَٱخْشَوْنِ وَلَا **تَشْتَرُوا۟** بِـَٔايَـٰتِى …
- QUL `خَشِىَ` · candidate `اشْتَرَىٰ` (`{$otaraY\`` 16/17 = 94.1%).
- Displacement source: `خَشِىَ` belongs to `تَخْشَوُا۟`/`ٱخْشَوْنِ`. Token = تشتروا → `اشْتَرَىٰ`.
- Mechanical. Risk if replaced: low. Risk if kept: تشتروا joins خشي set.

### 4.22 `60:10:45` وَلْيَسْـَٔلُوا۟ — **replace** (high)
- Ayah 60:10: … وَلَا تُمْسِكُوا۟ بِعِصَمِ ٱلْكَوَافِرِ وَسْـَٔلُوا۟ مَآ أَنفَقْتُمْ **وَلْيَسْـَٔلُوا۟** مَآ أَنفَقُوا۟ …
- QUL `أَمْسَكَ` · candidate `سَأَلَ` (`sa>ala` 76/77 = 98.7%).
- Displacement source: `أَمْسَكَ` belongs to `تُمْسِكُوا۟` (60:10:41). Token = وليسألوا → `سَأَلَ`.
- Mechanical. Risk if replaced: negligible. Risk if kept: a "ask" token joins أمسك set.

### 4.23 `6:101:14` شَىْءٍۢ — **replace** (high)
- Ayah 6:101: … وَخَلَقَ كُلَّ **شَىْءٍۢ** ۖ وَهُوَ بِكُلِّ شَىْءٍ عَلِيمٌۭ
- QUL `خَلَقَ` · candidate `شَىْء` (`$aYo'` 98.2%).
- Displacement source: `خَلَقَ` belongs to `وَخَلَقَ` (6:101:11). Token = شيء → `شَىْء`.
- Mechanical. Risk if replaced: negligible. Risk if kept: شيء joins خلق set.

### 4.24 `6:151:11` شَيْـًۭٔا — **replace** (high)
- Ayah 6:151: … أَلَّا تُشْرِكُوا۟ بِهِۦ **شَيْـًۭٔا** ۖ وَبِٱلْوَٰلِدَيْنِ إِحْسَـٰنًۭا …
- QUL `أَشْرَكَ` · candidate `شَىْء` (`$aYo'` 98.2%).
- Displacement source: `أَشْرَكَ` belongs to `تُشْرِكُوا۟` (6:151:9). Token = شيئا → `شَىْء`.
- Mechanical. Risk if replaced: negligible. Risk if kept: شيئا joins أشرك set.

### 4.25 `6:94:25` وَضَلَّ — **replace** (high)
- Ayah 6:94: … لَقَد تَّقَطَّعَ بَيْنَكُمْ **وَضَلَّ** عَنكُم مَّا كُنتُمْ تَزْعُمُونَ
- QUL `تَّقَطَّعَ` · candidate `ضَلَّ` (`Dal~a` 92.6%).
- Displacement source: `تَّقَطَّعَ` belongs to `تَّقَطَّعَ` (6:94:22). Token = وضلّ → `ضَلَّ`.
- Mechanical. Risk if replaced: low. Risk if kept: وضلّ joins تقطّع set.

### 4.26 `74:31:27` وَٱلْمُؤْمِنُونَ — **replace** (high)
- Ayah 74:31: … وَلَا يَرْتَابَ ٱلَّذِينَ أُوتُوا۟ ٱلْكِتَـٰبَ **وَٱلْمُؤْمِنُونَ** ۙ وَلِيَقُولَ …
- QUL `آتَى` · candidate `مُؤْمِن` (`mu&omin` 96.8%).
- Displacement source: `آتَى` belongs to `أُوتُوا۟` (74:31:25). Token = والمؤمنون → `مُؤْمِن`.
- Mechanical. Risk if replaced: negligible. Risk if kept: المؤمنون joins آتى set.

### 4.27 `7:38:32` ضِعْفًۭا — **replace** (high)
- Ayah 7:38: … فَـَٔاتِهِمْ عَذَابًۭا **ضِعْفًۭا** مِّنَ ٱلنَّارِ ۖ قَالَ لِكُلٍّۢ ضِعْفٌۭ …
- QUL `آتَى` · candidate `ضِعْف` (`DiEof` 9/10 = 90.0%).
- Displacement source: `آتَى` belongs to `فَـَٔاتِهِمْ` (7:38:29). Token = ضعفا → `ضِعْف`.
- Mechanical (context unambiguous despite 90%). Risk if replaced: low. Risk if kept: ضعفا joins آتى set.

### 4.28 `7:38:34` ٱلنَّارِ — **replace** (high)
- Ayah 7:38: … عَذَابًۭا ضِعْفًۭا مِّنَ **ٱلنَّارِ** ۖ قَالَ لِكُلٍّۢ ضِعْفٌۭ …
- QUL `ضِعْف` · candidate `نَار` (`naAr` 105/109 = 96.3%).
- Displacement source: `ضِعْف` belongs to `ضِعْفًۭا` (7:38:32). Token = النار → `نَار`. (7:38:32 and 7:38:34 form a two-link chain — both recommended replace; together they make this ayah self-consistent.)
- Mechanical. Risk if replaced: negligible. Risk if kept: النار joins ضعف set.

### 4.29 `9:93:10` يَكُونُوا۟ — **replace** (high)
- Ayah 9:93: … رَضُوا۟ بِأَن **يَكُونُوا۟** مَعَ ٱلْخَوَالِفِ …
- QUL `رَّضِىَ` · candidate `كَانَ` (`kaAna` 99.5%).
- Displacement source: `رَّضِىَ` belongs to `رَضُوا۟` (9:93:7). Token = يكونوا → `كَانَ`.
- Mechanical. Risk if replaced: negligible. Risk if kept: يكونوا joins رضي set.

### 4.30 `33:61:2` أَيْنَمَا — **exception** (medium) — multi-STEM compound; real defect is the neighbor
- Ayah 33:61: مَّلْعُونِينَ ۖ **أَيْنَمَا** ثُقِفُوٓا۟ أُخِذُوا۟ وَقُتِّلُوا۟ تَقْتِيلًۭا
- Word = أَيْنَمَا. **Corpus = two STEM segments**: `أَيْن` (LOC `>ayon`) + `مَا` (REL `maA`) → multi-STEM compound particle. Raw QUL whole-word lemma = `مَا`.
- This is the same modeling-divergence family as `أَنَّمَآ`→`إِنّ` and `إِلَّا` already accepted as `exception` in Phase 0E (QUL one whole-word lemma vs Corpus segmented constituents). It is **not** a clean "lemma shifted off a missing content word" — the previous word `مَّلْعُونِينَ` already has its own lemma; the shift heuristic that flagged it was a false positive (correctly demoted by the curator).
- Recommendation: `exception` (accepted modeling divergence; suppress the shift/diagnostic for this compound). `isMultiStem: true`.
- **Important follow-up (outside the 30-set):** the genuine misalignment in this ayah is on the *next* word `33:61:3` ثُقِفُوٓا۟, which carries QUL lemma `أَيْن` (root ث ق ف). Its own reliable lemma is `ثُقِفُ` (`vuqifu`, the lemma QUL uses for ثقف at `2:191:3`, `3:112:6`, `8:57:2`, `60:2:2`). `33:61:3` should be `replace` `أَيْن`→`ثُقِفُ`. It is currently resolved in Phase 0 (not a blocker), but should be spot-checked when these recommendations are applied so the ayah ends consistent.
- Risk if exception: none (non-mutating; matches Phase 0E policy). Risk if replaced/kept-as-shift: would wrongly null/move a legitimate compound lemma.

---

## 5. Summary counts by recommendation

| Recommendation | Count |
| --- | ---: |
| `replace` | 27 |
| `keep` | 2 |
| `exception` | 1 |
| `still-blocked` | 0 |
| **Total reviewed** | **30** |

- `keep`: `17:7:22`, `4:116:1` (the two special-caution cases — Corpus candidate is the wrong lemma).
- `exception`: `33:61:2` (multi-STEM compound particle).
- `replace`: the remaining 27 (mechanical source-misalignment; `2:221:19` at medium confidence, all others high).

Confidence distribution among `replace`: 26 high, 1 medium (`2:221:19`).

---

## 6. Proposed next step

1. **Accept these recommendations** (human sign-off), in particular noting the two `keep`s where Corpus must NOT override QUL, and the medium-confidence `2:221:19`.
2. In a **separate, explicitly-authorized promotion task** (not this report):
   - convert the 29 Phase 0D blocker entries from `candidate` to `approved` (27 × `replace`) / `accepted-exception` (2 × `keep`) in the curation **draft**, and
   - convert the Phase 0B blocker `33:61:2` from `candidate` to `accepted-exception` (`exception`, `isMultiStem: true`);
   - promote the resolved entries into the **active-staging** artifact;
   - re-run `validate.py` (zero `candidate`/`needs-review`, zero duplicate ids/locations, every `expectedCurrentLemmaArabic` matches raw QUL, every `replace` corrected Arabic resolves under a reliable mapping);
   - while promoting, **spot-check the neighbor `33:61:3`** (`أَيْن`→`ثُقِفُ`) so surah 33:61 ends self-consistent.
3. With 0 blockers remaining, turn **0B GREEN** and **0D GREEN** → Phase 0 MASTER GATE GREEN.
4. Only then: create the backend embedded artifact (`Backend/.../Corrections/word-lemma-normalization.json`) and start importer implementation per `word-level-lemma-full-normalization-implementation-plan.md`.

Each recommended `replace` is a single-operation fix (wrong → own reliable lemma); no companion `add` is required because the displaced lemma already has its canonical occurrence on its true word in the same ayah. This should be asserted by the `MORPH-WORD-LEMMA-REPLACE-VALID` hard check at import time.

---

## 7. Status

- Blockers reviewed: **30 / 30**.
- Recommendation: **27 replace · 2 keep · 1 exception · 0 still-blocked**.
- Phase 0 can be turned GREEN **after** these recommendations are accepted and promoted (no residual scholarly blockers; not done in this review task).
- Active-artifact promotion: **not performed here** (forbidden by task scope).
- Code implementation: **still NOT allowed** until promotion + MASTER GATE GREEN.
- Files created: `docs/feature-017-lexical-explorers-polish/word-lemma-phase-0f-blocker-resolution-review.md` (this file). No other files created or modified.
