# Feature 020 — `quran_lemmas.lemma_text` Collision Inventory

**Date (UTC):** 2026-07-04
**Branch:** `020-lexical-polish-and-project-hygiene`
**Type:** READ-ONLY inventory. No file/artifact edit, no import, no migration, no commit.
**Source (read only):** `resources/import-sources/quran-enriched-morphology/corpus-based-enriched-morphology.dashboard-ready.json` (77,432 records).

**Verdict:** **MANUAL_REVIEW_REQUIRED**

---

## Why this exists

`quran_lemmas.lemma_text` carries a **UNIQUE** index (`QuranDashboardDbContextModelSnapshot.cs:1666-67`). The enriched pathway keys lemma identity on `lemma_buckwalter` (plan §4), but the corpus distinguishes homographs with a numeric-suffixed Buckwalter (`X` / `X2`) that renders to the **same** vocalized `lemma_text`. Two lemma rows then share `lemma_text` and the second `COPY quran_lemmas` row violates the unique index.

## Key finding

The numeric suffix is **deliberate corpus lemma disambiguation, not spurious noise.** Of the 15 collisions, **4** pair variants that carry **different roots** and **11** pair variants with **different POS** (proper-vs-common noun, or verb-vs-nominal). Those are objectively **distinct lemmas** that merely share a display form; collapsing them on `lemma_text` would merge distinct Quran lemmas. Only cases with identical root **and** identical POS look like genuine duplicates.

## Method

Head lemma per word = the lowest-`segmentNumber` STEM segment's `lemmaBuckwalter`/`lemmaArabic` (what `EnrichedDimensionBuilder` mints). `lemma_text` = the `lemmaArabic` at the variant's first (min word-order) head occurrence. `COPY row` = 1-based rank in ascending `first_word_order` (COPY insert order). `head_occ` = words whose head lemma is that Buckwalter; `stem-seg occ` = STEM segments (any position) carrying it. Recommendation rubric (objective): different root **or** different POS → `MANUAL_REVIEW_REQUIRED`; same root + proper-noun POS → `COLLAPSE_SAFE_WITH_NOTES`; same root + same non-PN POS → `COLLAPSE_SAFE`. No religious/semantic content is asserted — only corpus POS/root/location attributes.

## Summary

- Distinct head lemma Buckwalters: **4,832** · distinct head `lemma_text`: **4,817**
- `lemma_text` collisions: **15** (all 2-variant; all 15 are numeric-suffix pairs)
- different root: **4** · different POS: **11**
- recommendation: **COLLAPSE_SAFE**=1, **COLLAPSE_SAFE_WITH_NOTES**=1, **MANUAL_REVIEW_REQUIRED**=13

| # | lemma_text | buckwalters | diff root | diff POS | recommendation |
| ---: | --- | --- | :---: | :---: | --- |
| 1 | `مَٰلِك` | `ma`lik`, `ma`lik2` | no | yes | MANUAL_REVIEW_REQUIRED |
| 2 | `إِذَا` | `<i*aA`, `<i*aA2` | no | yes | MANUAL_REVIEW_REQUIRED |
| 3 | `مَع` | `maE`, `maE2` | no | yes | MANUAL_REVIEW_REQUIRED |
| 4 | `حَيْث` | `Hayov`, `Hayov2` | no | yes | MANUAL_REVIEW_REQUIRED |
| 5 | `أَوْفَىٰ` | `>awofaY``, `>awofaY`2` | no | yes | MANUAL_REVIEW_REQUIRED |
| 6 | `عَصَا` | `EaSaA2`, `EaSaA` | yes | yes | MANUAL_REVIEW_REQUIRED |
| 7 | `صَٰلِح` | `Sa`liH`, `Sa`liH2` | no | yes | MANUAL_REVIEW_REQUIRED |
| 8 | `هُود` | `huwd2`, `huwd` | no | no | COLLAPSE_SAFE_WITH_NOTES |
| 9 | `عَاد` | `EaAd`, `EaAd2` | yes | yes | MANUAL_REVIEW_REQUIRED |
| 10 | `بَعْل` | `baEol`, `baEol2` | no | yes | MANUAL_REVIEW_REQUIRED |
| 11 | `جَٰهِلِيَّة` | `ja`hiliy~ap`, `ja`hiliy~ap2` | no | yes | MANUAL_REVIEW_REQUIRED |
| 12 | `جَوَاب` | `jawaAb`, `jawaAb2` | yes | no | MANUAL_REVIEW_REQUIRED |
| 13 | `يُغَاثُ` | `yugaAvu2`, `yugaAvu` | yes | no | MANUAL_REVIEW_REQUIRED |
| 14 | `أَحْصَىٰ` | `>aHoSaY``, `>aHoSaY`2` | no | yes | MANUAL_REVIEW_REQUIRED |
| 15 | `عَصْف` | `EaSof`, `EaSof2` | no | no | COLLAPSE_SAFE |

## Per-collision detail

### 1. `مَٰلِك` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT POS (e.g. proper-vs-common or verb-vs-nominal) — likely distinct lemmas._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `ma`lik` | 8 | 14 | `1:4:1` | 3 | 3 | N | mlk|م ل ك |
| `ma`lik2` | 4063 | 68161 | `43:77:2` | 1 | 1 | PN | mlk|م ل ك |

### 2. `إِذَا` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT POS (e.g. proper-vs-common or verb-vs-nominal) — likely distinct lemmas._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `<i*aA` | 72 | 139 | `2:11:1` | 409 | 409 | COND, SUR, T | — |
| `<i*aA2` | 1583 | 11868 | `4:77:17` | 14 | 14 | SUR | — |

### 3. `مَع` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT POS (e.g. proper-vs-common or verb-vs-nominal) — likely distinct lemmas._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `maE` | 84 | 191 | `2:14:13` | 159 | 159 | LOC | — |
| `maE2` | 258 | 682 | `2:41:6` | 5 | 5 | P | — |

### 4. `حَيْث` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT POS (e.g. proper-vs-common or verb-vs-nominal) — likely distinct lemmas._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `Hayov` | 228 | 592 | `2:35:10` | 29 | 29 | LOC, N | Hyv|ح   ي   ث |
| `Hayov2` | 645 | 2753 | `2:144:15` | 2 | 2 | COND | Hyv|ح   ي   ث |

### 5. `أَوْفَىٰ` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT POS (e.g. proper-vs-common or verb-vs-nominal) — likely distinct lemmas._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `>awofaY`` | 255 | 670 | `2:40:8` | 18 | 18 | V | wfy|و   ف   ي |
| `>awofaY`2` | 2422 | 27250 | `9:111:25` | 2 | 2 | ADJ, N | wfy|و   ف   ي |

### 6. `عَصَا` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT roots — distinct lemmas that share only display text._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `EaSaA2` | 339 | 949 | `2:60:7` | 10 | 10 | N | ESw|ع   ص   و |
| `EaSaA` | 368 | 1028 | `2:61:57` | 27 | 27 | V | ESy|ع   ص   ي |

### 7. `صَٰلِح` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT POS (e.g. proper-vs-common or verb-vs-nominal) — likely distinct lemmas._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `Sa`liH` | 373 | 1045 | `2:62:14` | 65 | 65 | ADJ, N | SlH|ص ل ح |
| `Sa`liH2` | 2112 | 21424 | `7:73:4` | 9 | 9 | PN | SlH|ص ل ح |

### 8. `هُود` — COLLAPSE_SAFE_WITH_NOTES

_same root + proper-noun POS; the numeric tag may still separate distinct named referents — confirm referent identity before collapse._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `huwd2` | 549 | 2068 | `2:111:8` | 3 | 3 | PN | hwd|ه   و   د |
| `huwd` | 2106 | 21288 | `7:65:4` | 7 | 7 | PN | hwd|ه   و   د |

### 9. `عَاد` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT roots — distinct lemmas that share only display text._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `EaAd` | 711 | 3292 | `2:173:18` | 6 | 6 | N | Edw|ع   د   و |
| `EaAd2` | 2105 | 21286 | `7:65:2` | 24 | 24 | PN | Ewd|ع   و   د |

### 10. `بَعْل` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT POS (e.g. proper-vs-common or verb-vs-nominal) — likely distinct lemmas._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `baEol` | 909 | 4613 | `2:228:22` | 6 | 6 | N | bEl|ب ع ل |
| `baEol2` | 3915 | 61931 | `37:125:2` | 1 | 1 | PN | bEl|ب ع ل |

### 11. `جَٰهِلِيَّة` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT POS (e.g. proper-vs-common or verb-vs-nominal) — likely distinct lemmas._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `ja`hiliy~ap` | 1372 | 9167 | `3:154:21` | 1 | 1 | N | jhl|ج ه ل |
| `ja`hiliy~ap2` | 1776 | 15385 | `5:50:2` | 3 | 3 | PN | jhl|ج ه ل |

### 12. `جَوَاب` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT roots — distinct lemmas that share only display text._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `jawaAb` | 2125 | 21581 | `7:82:3` | 4 | 4 | N | jwb|ج   و   ب |
| `jawaAb2` | 3817 | 58863 | `34:13:9` | 1 | 1 | N | jby|ج   ب   ي |

### 13. `يُغَاثُ` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT roots — distinct lemmas that share only display text._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `yugaAvu2` | 2682 | 32455 | `12:49:8` | 1 | 1 | V | gyv|غ ي ث |
| `yugaAvu` | 3071 | 40194 | `18:29:20` | 1 | 1 | V | gwv|غ   و   ث |

### 14. `أَحْصَىٰ` — MANUAL_REVIEW_REQUIRED

_variants carry DIFFERENT POS (e.g. proper-vs-common or verb-vs-nominal) — likely distinct lemmas._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `>aHoSaY`` | 2796 | 35038 | `14:34:11` | 10 | 10 | V | HSy|ح   ص   ي |
| `>aHoSaY`2` | 3041 | 39815 | `18:12:6` | 1 | 1 | N | HSy|ح   ص   ي |

### 15. `عَصْف` — COLLAPSE_SAFE

_same root + same POS + numeric-only tag — appears to be a spurious source duplicate of one lemma._

| buckwalter | COPY row | first word order | first location | head occ | stem-seg occ | POS | root (bw\|ar) |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| `EaSof` | 4264 | 73491 | `55:12:3` | 2 | 2 | N | ESf|ع ص ف |
| `EaSof2` | 4584 | 80570 | `77:2:2` | 1 | 1 | N | ESf|ع ص ف |

## Recommendation

Do **not** blindly collapse on `lemma_text`. Collapse only the identical-root/identical-POS duplicates; route every different-root or different-POS pair to scholarly review, because the corpus deliberately separated them. Alternatives that avoid merging distinct lemmas: (a) reconcile the artifact upstream so each retained lemma has a unique `lemma_text` (e.g. keep the source's disambiguation in a way the schema can store); (b) revisit the `UNIQUE(lemma_text)` constraint as a product decision. Both are follow-ups outside the FirstWordOrder remediation.

