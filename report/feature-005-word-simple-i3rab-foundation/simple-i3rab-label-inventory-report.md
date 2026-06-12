# Feature 005 — Simplified I‘rab Label Inventory (Read-Only Data Report)

**Type:** Read-only data inspection. No code, no migrations, no Spec Kit artifacts, no DB writes.
**Branch:** `005-word-simple-i3rab-foundation`
**Date:** 2026-06-12
**Source:** **Live Feature 004 database** `quran_dashboard` (DB-based counts, not source-estimated).

> **⚠️ Superseded for final Feature 005 decisions.** This is an early read-only inspection. For the
> **final** labels, data model, and status values, the **authoritative** sources are:
> - [`segment-pattern-rule-coverage-report.md`](segment-pattern-rule-coverage-report.md) — final labels, **100% approved** segment coverage, read-layer decisions.
> - `docs/feature-005-word-simple-i3rab-foundation/feature-005-word-simple-i3rab-foundation-planning-report.md` — the locked v1 plan.
>
> Where this report uses the boolean columns `i3rab_is_supported` / `i3rab_unsupported_reason` (§6–§7) or
> recommends needs-review / sign-off for labels since finalized as **approved** (e.g. `RES → أداة حصر`,
> `T → ظرف زمان`, `SUR → حرف فجاءة`, `EQ`, `COM`, `VOC.SUFFIX`, `P.SUFFIX → لام الجر`, `N.GEN.1S`), the
> finalized reports take precedence: the v1 columns are **`i3rab_status`** / **`i3rab_review_reason`**, and
> **all 142 segment-token signatures are approved-candidate** (0% needs-review, 0% unsupported).

> **Data provenance.** All counts and examples below are **queried from the populated Feature 004
> morphology tables** (verified row totals: `quran_word_morphology` = 77,432, `…_segments` = 128,219,
> `quran_pos_tags` = 49, `quran_roots` = 1,642, `quran_lemmas` = 4,793, `quran_stems` = 12,108). No
> example is invented; every word shows its real `id`, `location`, and Uthmani text from `quran_words`.
> Feature 004 data was **read only** — nothing was modified.

---

## 0. Executive summary

- **Segment-level labels are highly derivable.** Every one of the 128,219 segments already has a POS
  code that resolves to an Arabic label in `quran_pos_tags`, and the high-value grammatical labels
  (noun case, verb tense/voice, attached pronoun person) are fully backed by `features_raw`.
- **Noun case is universal:** all **31,007** noun-class heads (`N`/`PN`/`ADJ`) carry a case
  (`case_feature IS NULL` count = **0**) → reliable `اسم/صفة/اسم علم + مرفوع/منصوب/مجرور`.
- **Verbs are fully tensed:** all **19,356** verbs carry tense + voice, including **1,140 passive**
  verbs → reliable `فعل ماض/مضارع/أمر` + `مبني للمجهول`.
- **لفظ الجلالة is cleanly identifiable** (lemma id **265**, `{ll~ah`, 2,698 words) → `لفظ الجلالة +
  case`.
- **~90.3 % of readable words** (69,884 / 77,432) fall into high-confidence v1 head buckets
  (noun-with-case + verb-with-tense + clear particle/REL/DEM/PRON).
- **208 segments** have `form_arabic_normalized = NULL` — **all** of them `SUFFIX PRON:1S` with an empty
  `form_buckwalter` (the elided 1st-person-singular pronoun). They still get a clean i‘rab label and
  must **not** be given a fabricated Arabic form.
- **Two POS-seed Arabic labels need correction for user-facing i‘rab:** `REM` is labelled `حرف استثناء`
  but QAC `REM` = *resumption* (should read **حرف استئناف**, 2,925 occ.); `RES` is labelled `حرف ردع`
  but QAC `RES` = *restriction* (should read **حرف حصر/قصر**, 558 occ.). See §1.4.

---

## 1. Segment-level label inventory

### 1.1 Full segment POS × kind inventory (every code present in the data)

`seg` = segment rows, `words` = distinct words. Arabic label is the current `quran_pos_tags.arabic_label`.

| POS | Arabic label (current seed) | Kind | seg | words |
|---|---|---|---:|---:|
| N | اسم | STEM | 25,136 | 25,135 |
| PRON | ضمير | SUFFIX | 21,384 | 20,146 |
| V | فعل | STEM | 19,356 | 19,356 |
| CONJ | حرف عطف | PREFIX | 8,694 | 8,694 |
| DET | أداة تعريف | PREFIX | 8,377 | 8,377 |
| P | حرف جر | STEM | 7,679 | 7,679 |
| P | حرف جر | PREFIX | 5,325 | 5,325 |
| PN | اسم علم | STEM | 3,911 | 3,911 |
| REL | اسم موصول | STEM | 3,575 | 3,575 |
| PRON | ضمير | STEM | 3,301 | 3,301 |
| REM | حرف استثناء ⚠️ | PREFIX | 2,925 | 2,925 |
| NEG | حرف نفي | STEM | 2,688 | 2,688 |
| ACC | حرف نصب | STEM | 2,283 | 2,283 |
| ADJ | صفة | STEM | 1,961 | 1,961 |
| T | تاء تأنيث | STEM | 1,166 | 1,166 |
| DEM | اسم إشارة | STEM | 1,059 | 1,059 |
| COND | حرف شرط | STEM | 1,049 | 1,049 |
| EMPH | حرف تأكيد | PREFIX | 1,001 | 1,001 |
| CONJ | حرف عطف | STEM | 756 | 756 |
| SUB | اسم مبهم | STEM | 684 | 684 |
| LOC | ظرف مكان | STEM | 669 | 669 |
| RES | حرف ردع ⚠️ | STEM | 558 | 558 |
| INTG | حرف استفهام | PREFIX | 507 | 507 |
| INTG | حرف استفهام | STEM | 439 | 439 |
| CERT | حرف تحقيق | STEM | 414 | 414 |
| VOC | حرف نداء | PREFIX | 371 | 371 |
| RSLT | فاء الجزاء | PREFIX | 350 | 350 |
| PRO | ضمير منفصل | STEM | 332 | 332 |
| PRP | حرف غاية | PREFIX | 319 | 319 |
| CIRC | واو الحال | PREFIX | 293 | 293 |
| EMPH | حرف تأكيد | SUFFIX | 243 | 243 |
| SUP | حرف زائد | PREFIX | 214 | 214 |
| PREV | حرف تحضيض | STEM | 162 | 162 |
| RET | حرف إضراب | STEM | 122 | 122 |
| FUT | حرف استقبال | PREFIX | 119 | 119 |
| EXP | حرف استثناء | STEM | 104 | 104 |
| INC | حرف ابتداء | STEM | 90 | 90 |
| CAUS | حرف سببية | PREFIX | 88 | 88 |
| IMPV | فعل أمر | PREFIX | 78 | 78 |
| EXL | حرف تعليل | STEM | 66 | 66 |
| AMD | حرف عطف / نفي | STEM | 65 | 65 |
| INT | حرف تفسير | STEM | 47 | 47 |
| FUT | حرف استقبال | STEM | 42 | 42 |
| EXH | حرف تحضيض | STEM | 40 | 40 |
| ANS | حرف جواب | STEM | 40 | 40 |
| SUR | إذا الفجائية | STEM | 35 | 35 |
| AVR | حرف ردع | STEM | 33 | 33 |
| INL | قسم | STEM | 30 | 30 |
| SUP | حرف زائد | STEM | 21 | 21 |
| EQ | حرف تسوية | PREFIX | 6 | 6 |
| VOC | حرف نداء | SUFFIX | 5 | 5 |
| COM | واو المعية | PREFIX | 3 | 3 |
| IMPN | اسم فعل أمر | STEM | 2 | 2 |
| P | حرف جر | SUFFIX | 2 | 2 |

⚠️ = Arabic label should be reviewed before user display (see §1.4).

### 1.2 Noun-class STEM by case (the highest-value labels)

| POS | Case | Proposed Arabic label | seg | words |
|---|---|---|---:|---:|
| N | NOM | اسم مرفوع | 6,777 | 6,777 |
| N | ACC | اسم منصوب | 7,955 | 7,955 |
| N | GEN | اسم مجرور | 10,404 | 10,404 |
| ADJ | NOM | صفة مرفوعة | 843 | 843 |
| ADJ | ACC | صفة منصوبة | 590 | 590 |
| ADJ | GEN | صفة مجرورة | 528 | 528 |
| PN | NOM | اسم علم مرفوع | 1,321 | 1,321 |
| PN | ACC | اسم علم منصوب | 912 | 912 |
| PN | GEN | اسم علم مجرور | 1,678 | 1,678 |

> **100 % case coverage.** Across all 31,007 `N`/`PN`/`ADJ` heads, **zero** lack a case. Adjective
> labels use **feminine agreement** (`صفة مجرورة`, not "صفة مجرور") — a rule responsibility, not naive
> concatenation.

### 1.3 لفظ الجلالة (special lemma) by case

Lemma id **265** = `اللَّه` / Buckwalter `{ll~ah`, `head_pos = PN`, 2,698 words.

| Case | Proposed Arabic label | words |
|---|---|---:|
| NOM | لفظ الجلالة مرفوع | 979 |
| ACC | لفظ الجلالة منصوب | 592 |
| GEN | لفظ الجلالة مجرور | 1,127 |

A simple `lemma_id = 265` check upgrades a generic `اسم علم مجرور` to **`لفظ الجلالة مجرور`**.
(`اللَّهُمَّ`, lemma 656, 5 words, is a separate vocative form — handle separately or leave generic.)

### 1.4 Verbs by tense × voice (word-level head)

| Tense | Voice | Proposed Arabic label | words |
|---|---|---|---:|
| past | active | فعل ماض | 8,516 |
| present | active | فعل مضارع | 7,824 |
| imperative | active | فعل أمر | 1,876 |
| past | passive | فعل ماض مبني للمجهول | 634 |
| present | passive | فعل مضارع مبني للمجهول | 506 |

All 19,356 verbs carry a valid tense + voice; **1,140 passive** verbs exist for examples.

### 1.5 Attached pronoun (SUFFIX PRON) by person/gender/number

Base label `ضمير متصل`, refined from the `PRON:` token. Top persons:

| Token | Refined Arabic label | seg | words |
|---|---|---:|---:|
| 3MP | ضمير متصل للغائبين | 7,366 | 7,337 |
| 2MP | ضمير متصل للمخاطبين | 4,645 | 4,645 |
| 3MS | ضمير متصل للغائب المفرد | 2,727 | 2,727 |
| 1P | ضمير متصل للمتكلمين | 2,347 | 2,347 |
| 2MS | ضمير متصل للمخاطب المفرد | 1,300 | 1,299 |
| **1S** | **ضمير متصل للمتكلم المفرد** | **1,239** | **1,239** |
| 3FS | ضمير متصل للغائبة المفردة | 1,062 | 1,062 |
| 3FP | ضمير متصل للغائبات | 267 | 267 |

(Plus dual/feminine variants 3MD, 3D, 2D, 2FS, 2FP, 2MD, 3FD, 2FD in small counts.) **1S** is the
person whose **208** elided occurrences carry a NULL form — see §4.

### 1.6 POS Arabic-label issues found (must resolve before user display)

| POS | Seed label | QAC meaning | Recommended user-facing label | Occ. |
|---|---|---|---|---:|
| `REM` | حرف استثناء | resumption | **حرف استئناف** | 2,925 |
| `RES` | حرف ردع | restriction | **حرف حصر / قصر** | 558 |
| `T` (head) | تاء تأنيث | feminine marker | ambiguous as a *head* — needs review | 1,166 |
| `AMD` | حرف عطف / نفي | amendment (لكن) | **حرف استدراك** (review) | 65 |

**Recommendation:** the Feature 005 rule layer should **own the user-facing grammatical labels** and not
depend blindly on `quran_pos_tags.arabic_label` for the simplified i‘rab phrasing. This avoids editing
Feature 004 data while still shipping correct Arabic. (POS codes stay for developer audit only.)

---

## 2. Word-level combined summaries

Derived from the ordered segment POS pattern per word. Top patterns (real counts):

| Pattern (POS, dev audit) | words | Proposed Arabic summary | v1 supported? |
|---|---:|---|---|
| `N` | 10,340 | اسم + (مرفوع/منصوب/مجرور حسب الحالة) | ✅ |
| `V+PRON` | 7,439 | فعل + ضمير متصل (فاعل/مفعول — تُعرض كـ «فعل واتصل به ضمير») | ✅ (label), ⚠️ (role) |
| `V` | 5,862 | فعل ماض/مضارع/أمر | ✅ |
| `DET+N` | 5,848 | اسم معرفة + الحالة (أو ببساطة: اسم مرفوع/منصوب/مجرور) | ✅ |
| `P` | 4,947 | حرف جر | ✅ |
| `N+PRON` | 4,022 | اسم + ضمير متصل (مضاف ومضاف إليه) | ✅ (label) |
| `P+PRON` | 3,888 | **جار ومجرور** | ✅ |
| `PN` | 2,834 | اسم علم + الحالة (لفظ الجلالة إن كان كذلك) | ✅ |
| `REL` | 2,204 | اسم موصول | ✅ |
| `CONJ+V+PRON` | 1,470 | حرف عطف، فعل، ضمير متصل | ✅ |
| `ADJ` | 1,384 | صفة + الحالة | ✅ |
| `NEG` | 1,258 | حرف نفي | ✅ |
| `CONJ+V` | 1,243 | حرف عطف، فعل... | ✅ |
| `ACC+PRON` | 863 | حرف نصب + ضمير متصل (اسم «إنّ» وأخواتها) | ⚠️ review |
| `CONJ+NEG` | 862 | حرف عطف، حرف نفي | ✅ |
| `PRON` | 840 | ضمير منفصل/متصل | ✅ |
| `P+N` | 812 | **جار ومجرور** (حرف جر + اسم مجرور) | ✅ |
| `CONJ+DET+N` | 795 | حرف عطف، اسم معرفة + الحالة | ✅ |
| `V+PRON+PRON` | 793 | فعل + ضميران متصلان | ✅ |
| `DEM` | 773 | اسم إشارة | ✅ |
| `CONJ+N` | 773 | حرف عطف، اسم + الحالة | ✅ |
| `P+DET+N` | 765 | **جار ومجرور** (معرفة) | ✅ |
| `CONJ` | 742 | حرف عطف | ✅ |
| `P+REL` | 740 | حرف جر + اسم موصول | ✅ |

**Idiom collapses to support in v1:** `P+PRON`, `P+N(GEN)`, `P+DET+N(GEN)`, `P+REL` → **جار ومجرور**;
`INTG+V` → **همزة استفهام + فعل …**; `DET+N` → **اسم معرفة + الحالة**.

**Patterns to mark “needs review” (no reliable syntactic role from morphology alone):** anything implying
فاعل / مفعول به / مبتدأ / خبر (e.g. the role of the pronoun in `V+PRON`, the noun in `ACC+PRON`). v1 emits
*form/case labels*, not sentence roles.

---

## 3. Recommended UI examples

All real occurrences, pulled from the DB with full segment breakdown. **User-facing display = Arabic
segment + Arabic simplified i‘rab**; POS codes shown here for developer audit only.

> Display convention used below: each segment row = `المقطع العربي → الإعراب المبسط` (`POS` in parentheses
> for audit). Word summary composes segment labels in order with the Arabic comma «،».

### 3.1 Multiple prefixes — وَبِٱلْيَوْمِ (id 109, `2:8:7`)
*Closest real match to “وَبِٱلْآخِرَةِ”: a 4-segment `CONJ+P+DET+N` word; head `N` genitive.*

| seg | عربي | الإعراب المبسط | POS (audit) |
|---|---|---|---|
| 1 | وَ | حرف عطف | PREFIX CONJ |
| 2 | بِ | حرف جر | PREFIX P |
| 3 | ٱلْ | أداة تعريف | PREFIX DET |
| 4 | يَوْمِ | اسم مجرور | STEM N · GEN |

**ملخص الكلمة:** حرف عطف، جار ومجرور (اسم معرفة مجرور). **Useful because** it shows a 3-prefix stack +
definite genitive noun in one word. *(For the literal آخِرَة example, `وَبِٱلْءَاخِرَةِ` exists later in
2:4 with the same pattern.)*

### 3.2 Interrogative hamza + verb — أَتَجْعَلُ (id 498, `2:30:11`)

| seg | عربي | الإعراب المبسط | POS (audit) |
|---|---|---|---|
| 1 | أَ | همزة استفهام | PREFIX INTG |
| 2 | تَجْعَلُ | فعل مضارع | STEM V · IMPF · 2MS |

**ملخص الكلمة:** همزة استفهام، فعل مضارع. **Useful because** it is the canonical `INTG+V` teaching case.

### 3.3 Preposition + pronoun — عَلَيْهِمْ (id 30, `1:7:4`)

| seg | عربي | الإعراب المبسط | POS (audit) |
|---|---|---|---|
| 1 | عَلَيْ | حرف جر | STEM P |
| 2 | هِمْ | ضمير متصل للغائبين | SUFFIX PRON · 3MP |

**ملخص الكلمة:** **جار ومجرور**. **Useful because** it is the textbook `P+PRON` idiom (and from
al-Fātiḥah). *(فِيهَا exists too; عَلَيْهِمْ chosen for the clearer attached pronoun.)*

### 3.4 DET + noun in NOM — ٱلْمُفْلِحُونَ (id 76, `2:5:8`)

| seg | عربي | الإعراب المبسط | POS (audit) |
|---|---|---|---|
| 1 | ٱلْ | أداة تعريف | PREFIX DET |
| 2 | مُفْلِحُونَ | اسم مرفوع | STEM N · NOM |

**ملخص الكلمة:** اسم معرفة مرفوع. **Useful because** it shows `DET+N` definiteness + nominative plural.

### 3.5 لفظ الجلالة in all three cases
| word | id | location | الإعراب المبسط | POS (audit) |
|---|---|---|---|---|
| ٱللَّهِ | 2 | `1:1:2` | **لفظ الجلالة مجرور** | STEM PN · GEN |
| ٱللَّهُ | 5436 | `2:255:1` | **لفظ الجلالة مرفوع** | STEM PN · NOM |
| ٱللَّهَ | 116 | `2:9:2` | **لفظ الجلالة منصوب** | STEM PN · ACC |

**Useful because** it shows the lemma-aware upgrade (`اسم علم` → `لفظ الجلالة`) across NOM/ACC/GEN.

### 3.6 Relative pronoun — ٱلَّذِينَ (id 28, `1:7:2`)
Single `STEM REL`. **الإعراب المبسط:** اسم موصول. **ملخص الكلمة:** اسم موصول.

### 3.7 Demonstrative — أُو۟لَـٰٓئِكَ (id 69, `2:5:1`)
Single `STEM DEM`. **الإعراب المبسط:** اسم إشارة. **ملخص الكلمة:** اسم إشارة.

### 3.8 Verbs — past / present / imperative / passive
| word | id | location | الإعراب المبسط | POS (audit) |
|---|---|---|---|---|
| قَالَ | 489 | `2:30:2` | فعل ماض | STEM V · PERF · active |
| يَقُولُ | 106 | `2:8:4` | فعل مضارع | STEM V · IMPF · active |
| ٱقْرَأْ | 82955 | `96:1:1` | فعل أمر | STEM V · IMPV · active |
| أُنزِلَ | 59 | `2:4:4` | **فعل ماض مبني للمجهول** | STEM V · PERF · **PASS** |
| بُعْثِرَ | 83262 | `100:9:4` | فعل ماض مبني للمجهول | STEM V · PERF · PASS |

### 3.9 NULL-render segment — رَبِّ (id 2362, `2:126:4`)
A word whose attached 1S pronoun is **elided** (no written form):

| seg | عربي | الإعراب المبسط | POS (audit) |
|---|---|---|---|
| 1 | رَبِّ | اسم منصوب | STEM N · ACC |
| 2 | *(لا يُعرض مقطع)* | ضمير متصل للمتكلم المفرد (محذوف) | SUFFIX PRON · 1S · **form = NULL** |

**ملخص الكلمة:** اسم منصوب، ضمير متكلم متصل (محذوف). **Useful because** it shows the correct way to
display i‘rab for a segment with **no** `form_arabic_normalized`: render the **label only**, with a
“محذوف/مُقدَّر” marker, and **never** invent an Arabic form. See §4.

---

## 4. NULL `form_arabic_normalized` inspection

| Check | Result |
|---|---|
| Total segments with `form_arabic_normalized IS NULL` | **208** (0.16 % of 128,219) |
| Grouped by kind / pos / features | **all 208** = `SUFFIX` · `PRON` · `features_raw = "SUFFIX|PRON:1S"` |
| `form_buckwalter` on those rows | **empty** (`''`) on all 208 — i.e. no source surface form at all |
| Distinct words affected | 208 (one such segment per word) |

**Examples:** `رَبِّ` (`2:126:4`, id 2362), and others such as `2:54:5`, `2:132:6`, `2:260:4`, `3:35:5`.
These are the elided/implicit **1st-person-singular** possessive/object pronoun (the “ياء المتكلم”
dropped in pause/spelling).

**Simplified label still available:** yes — `ضمير متصل للمتكلم المفرد` (optionally annotated `محذوف` /
`مُقدَّر`). The label comes entirely from `pos = PRON` + `PRON:1S`, independent of the missing form.

**Safe UI rule:** when `form_arabic_normalized IS NULL`, **do not render a segment chip with text**
(and do not fall back to `form_buckwalter`, which is also empty). Render only the **i‘rab label** with a
“محذوف” marker, or attach the pronoun’s label to the preceding stem’s display.

**Confirmed:** Feature 005 **must not** invent a `form_arabic_normalized` for these rows. It is a
Feature-004 column and is deliberately NULL; Feature 005 only adds an i‘rab **label**, never a form.

---

## 5. Arabic-only user-facing output (confirmed policy)

- **All user-facing labels are Arabic.** POS codes (`P`, `N`, `V`, `INTG`, …) appear in this report
  **only** for developer audit and must **not** be shown in the normal UI.
- The UI should display, per segment: **(1)** `المقطع العربي` if present (`form_arabic_normalized`),
  **(2)** `الإعراب المبسط العربي`, and optionally **(3)** the word’s main type in Arabic
  (`اسم` / `فعل` / `حرف`) derived from `quran_pos_tags.category`.
- **Color may be driven internally** by POS / `category` (noun / verb / particle / other), but the
  **text shown to users stays Arabic**. (Category is already stored: noun / verb / particle / other.)

---

## 6. Data model recommendation (updated for the adjusted proposal)

The report evaluates the adjusted model: **inline i‘rab columns on the existing segments table** instead
of a separate `quran_word_segment_i3rab` table.

### 6.1 Verdict: ✅ adopt inline columns on `quran_word_morphology_segments` for v1

Because segment i‘rab is **strictly 1:1** with a segment, a separate table is pure join overhead
(KISS/YAGNI). Add four derived columns:

| Column | Type | Notes |
|---|---|---|
| `i3rab_arabic` | `text` NULL | simplified Arabic label; NULL only when unsupported |
| `i3rab_rule_id` | `int` NULL | **FK** → `quran_i3rab_rules.id`; NULL only when unsupported |
| `i3rab_is_supported` | `bool` NOT NULL | true when a rule produced a label |
| `i3rab_unsupported_reason` | `text` NULL | required iff `i3rab_is_supported = false` |

Plus an index on `i3rab_rule_id` and a partial index on `(i3rab_is_supported) WHERE i3rab_is_supported
= false` for coverage reporting.

**Keep `quran_i3rab_rules`** as a curated reference / provenance table (rule_key, pattern, Arabic label,
scope, supported flag, sort_order), seeded by the generator like `PosTagSeed` — it gives every label an
FK target and a place to report rule coverage.

### 6.2 Write boundary (hard constraint)

Feature 005 may write **only** the four `i3rab_*` columns, via **targeted `UPDATE`** keyed by segment
`id`. It must **never**:
- INSERT / DELETE / TRUNCATE rows in `quran_word_morphology_segments`, and
- modify `pos`, `kind`, `form_buckwalter`, `form_arabic_normalized`, `features_raw`, `features_json`,
  `root_buckwalter`, `lemma_buckwalter`, or any other Feature 004 column.

### 6.3 Rebuild coupling (must document)

A Feature 004 morphology re-import with `--force` truncates the segments table and therefore **clears
the `i3rab_*` columns**. So: **i‘rab generation runs after morphology import**, and a morphology rebuild
**invalidates** i‘rab until regenerated. (A separate FK table would behave the same via cascade.) The
generator should detect “morphology changed since last i‘rab run” and warn/refuse stale state.

### 6.4 `quran_word_i3rab` summary table: ❌ not needed in v1 — compose at read time

Recommendation: **do not materialize** a word-summary table in v1.

- The word summary is derivable cheaply from ≤ ~5 segment rows: order the supported `i3rab_arabic`
  values by `segment_number` and join with the **Arabic comma «، »**.
- The dominant head label (case / tense / voice) already lives on `quran_word_morphology`.
- Materializing now would add a second derived table + second write path for a presentation concern —
  premature for a “no UI / no API” foundation (YAGNI).

**Read-time recipe (for the later UI/API):**
```text
word_summary = string_agg(i3rab_arabic, '، ' ORDER BY segment_number)
               FROM quran_word_morphology_segments
               WHERE quran_word_id = :id AND i3rab_is_supported;
```
with a small set of **idiom overrides** applied in the read layer (`P+PRON` / `P+…GEN` → `جار ومجرور`).

**Promote later if** word-level filtering or stored idiom summaries become a real requirement — at that
point materialize `quran_word_i3rab` in the same generation pass (cheap, one extra COPY). Document the
trigger; don’t build it speculatively.

---

## 7. Validation implications (adjusted model)

Hard checks (gate the generation commit; rollback on failure):

| Id (proposed) | Invariant |
|---|---|
| `I3RAB-SEG-LABEL-OR-REASON` | every segment has **either** `i3rab_arabic` **or** (`i3rab_is_supported = false` **and** non-empty `i3rab_unsupported_reason`) — no silent NULLs |
| `I3RAB-SUPPORTED-CONSISTENT` | `i3rab_is_supported = true` ⇒ `i3rab_arabic` and `i3rab_rule_id` non-null; `false` ⇒ both null + reason present |
| `I3RAB-WORD-DISPLAYABLE` | every readable word can derive a segment-label display (≥ 1 supported segment, or all segments carry an explicit unsupported reason) |
| `I3RAB-MARKERS-EXCLUDED` | no i‘rab on ayah markers (already guaranteed — markers have no morphology/segments) |
| `I3RAB-RULE-RESOLVES` | every non-null `i3rab_rule_id` resolves to a `quran_i3rab_rules` row |
| `I3RAB-SOURCE-COLUMNS-UNCHANGED` | a hash/snapshot of the **non-i3rab** segment columns is **identical** before & after the run (proves only `i3rab_*` were written) |
| `I3RAB-SEGMENT-ROWCOUNT-STABLE` | segment row count unchanged (128,219); no inserts/deletes |
| `I3RAB-NULL-FORM-NOT-INVENTED` | the 208 NULL-`form_arabic_normalized` rows still have NULL form after the run (i‘rab added a label only) |

Warnings (informational, never gate):

| Id (proposed) | Signal |
|---|---|
| `I3RAB-COVERAGE` | % of segments and words with a supported label (report, don’t fail) |
| `I3RAB-RULE-USAGE` | per-rule hit counts (which rules fired / never fired) |
| `I3RAB-UNKNOWN-PATTERNS` | POS / feature patterns with no rule (so the catalogue can grow) |
| `I3RAB-LABEL-REVIEW` | flagged labels (REM/RES/T/AMD, adjective agreement, idiom collapses) for sign-off |

---

## 8. Final recommendation

### 8.1 Recommended v1 **supported** labels (all source-backed, high confidence)

- **Particles (by POS):** حرف جر `P` · حرف عطف `CONJ` · أداة تعريف `DET` · همزة استفهام `INTG (prefix)` ·
  حرف نفي `NEG` · حرف نصب `ACC` · حرف تأكيد `EMPH` · حرف نداء `VOC` · حرف شرط `COND` · حرف تحقيق `CERT` ·
  حرف استقبال `FUT`.
- **Names/pronouns:** اسم موصول `REL` · اسم إشارة `DEM` · ضمير منفصل `PRO` · ضمير متصل (+person) `SUFFIX
  PRON`.
- **Noun case:** اسم مرفوع/منصوب/مجرور `N` · صفة مرفوعة/منصوبة/مجرورة `ADJ` (feminine agreement) · اسم علم
  مرفوع/منصوب/مجرور `PN`.
- **لفظ الجلالة:** لفظ الجلالة مرفوع/منصوب/مجرور (`PN` + lemma 265).
- **Verbs:** فعل ماض / فعل مضارع / فعل أمر, plus `مبني للمجهول` on PASS.
- **Word idioms:** **جار ومجرور** (`P+PRON`, `P+…GEN`, `P+DET+N`, `P+REL`); **همزة استفهام + فعل…**
  (`INTG+V`); **اسم معرفة + الحالة** (`DET+N`).

### 8.2 Recommended v1 **unsupported / needs-review** patterns

- **Fix POS labels first:** `REM` → حرف استئناف (2,925), `RES` → حرف حصر/قصر (558), review `AMD` →
  حرف استدراك (65), and `T` as a head (1,166).
- **No syntactic roles** (فاعل / مفعول به / مبتدأ / خبر / حال) — not derivable from morphology; mark the
  role of pronouns in `V+PRON` and the noun in `ACC+PRON` as `needs-review`, label form/case only.
- **Rare particles** with low counts and questionable labels (`SUR`, `AVR`, `EQ`, `COM`, `IMPN`, `INT`,
  `SUP`) — label individually but flag for sign-off.
- **Multiword / review-tier** segments — none in the current import, but the rule set must define a
  fallback (`is_supported = false`, reason) for when they appear.

### 8.3 Recommended example words for docs / UI

`109` وَبِٱلْيَوْمِ (multi-prefix) · `498` أَتَجْعَلُ (INTG+V) · `30` عَلَيْهِمْ (P+PRON) · `76`
ٱلْمُفْلِحُونَ (DET+N NOM) · `2` ٱللَّهِ / `5436` ٱللَّهُ / `116` ٱللَّهَ (لفظ الجلالة GEN/NOM/ACC) · `28`
ٱلَّذِينَ (REL) · `69` أُو۟لَـٰٓئِكَ (DEM) · `489` قَالَ (past) · `106` يَقُولُ (present) · `82955`
ٱقْرَأْ (imperative) · `59` أُنزِلَ / `83262` بُعْثِرَ (passive) · `2362` رَبِّ (NULL-form 1S pronoun).

### 8.4 Recommended data model

- **Inline `i3rab_*` columns** on `quran_word_morphology_segments` (no separate segment table).
- **`quran_i3rab_rules`** curated reference/provenance table.
- **No `quran_word_i3rab`** in v1 — compose the word summary at read time (ordered segment labels joined
  with «، », plus idiom overrides); promote to a materialized table only if a real need appears.
- **Per-occurrence grain** (keyed to segment `id` / `quran_word_id`) — case is contextual, so i‘rab must
  not be keyed to the imlaei-simple identity group.

### 8.5 Recommended next step before `/speckit.specify`

1. **Lock the v1 rule catalogue** (§8.1) — the POS→label map, the noun/adjective/PN case-agreement rules,
   the verb tense/voice rules, the لفظ الجلالة upgrade, and the idiom collapses.
2. **Decide label ownership** — adopt the recommendation that the **Feature 005 rule layer owns
   user-facing Arabic labels** (so REM/RES/AMD/T are corrected in rules without editing Feature 004).
3. **Confirm the write boundary + rebuild ordering** (§6.2–§6.3) as spec constraints.
4. Then run **`/speckit.specify`** for `005-word-simple-i3rab-foundation` using this inventory as the
   evidence base.

---

### Quranic data safety

This report reads Feature 004 data **only**, modifies nothing, and shows **individual** word forms +
segment renderings (already-derived, provenance-flagged reading aids) for illustration — never assembled
ayah text and never invented grammar. Unsupported patterns are recorded as unsupported, never guessed.
The simplified labels are **not** authoritative scholarly i‘rab.
