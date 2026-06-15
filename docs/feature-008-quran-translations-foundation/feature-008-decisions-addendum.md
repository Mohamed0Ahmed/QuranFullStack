# Feature 008 — Quran Translations Foundation — Decisions Addendum

> **Type:** Locked-decisions record (planning only — no code, no migrations, no Spec Kit, no source copy).
> **Date:** 2026-06-15
> **Source of truth for data facts:** `translation-source-curation-report.md` (same folder).
> **Authority:** This addendum is authoritative for Feature 008 scope and curation decisions. Where it
> differs from the original curation report, **this addendum wins** (notably the empty-text policy and
> the recomputed counts).

---

## 1. Purpose

The curation report inventoried and verified the raw translation sources. This addendum records the
**decisions Mohamed has locked** on top of that report, recomputes the **final approved/excluded
counts** under those decisions, and lists what still needs a human decision before `/speckit.specify`.

---

## 2. What changed vs the curation report

| Topic | Curation report | Locked decision (this addendum) |
|---|---|---|
| Empty-text files | Warning-level; "allow with warning" for ≤66 empties | **Hard exclude.** Any empty/null/missing/non-string `t` → file excluded, even for one ayah. `TR-NO-EMPTY-TEXT` is a **hard** gate. |
| Approved count | Estimate "≈168–172" | **Final: 167** — package built & validated. See §4. |
| Word-by-word | "defer" recommendation | **Locked out** of Feature 008. |
| Middle table | "2 tables recommended" | **Locked: 2 tables**, no `quran_translation_entries`. |
| Misclassified simple-with-footnotes | "decision needed" | **Locked: reclassify** to `translation_type = with_footnotes`. |
| Near-dup unattributed copies | "review" | **Locked: drop** `ko-unknown`, `sq-unknown`. |

---

## 3. Locked decisions (D1–D14)

| # | Decision | Implication for Spec Kit |
|---|---|---|
| **D1** | Backend **data-foundation only**. No UI, no API endpoint, no search index, no startup seeding, no permissions/access work, no WBW. | Spec scope is import + tables + validation + reports only. |
| **D2** | Include **ayah-level only**: `simple` + `with_footnotes` (keyed `surah:ayah`, value `{ "t": ... }`). | Two source types feed one importer. |
| **D3** | **Exclude all word-by-word** (keyed `surah:ayah:word`, variable word counts, needs `quran_words` alignment). | 11 files explicitly excluded; reserved for a future feature. |
| **D4** | **Completeness is hard.** Exclude a file if it lacks the exact 6,236 key set, OR has any empty/null/missing/non-string `t`. | `TR-COVERAGE-COUNT` + `TR-NO-EMPTY-TEXT` are **hard** gates. |
| **D5** | **Preserve text exactly.** Keep `[[…]]` and embedded HTML verbatim; do **not** parse, sanitize, strip, normalize, or restructure. | `TR-TEXT-UNCHANGED` hard; no footnote table. |
| **D6** | `translation_type` ∈ {`simple`, `with_footnotes`} lives on **`quran_translation_sources`** (not per ayah row). | Source-level column; enables future UI filter. |
| **D7** | **Content over folder:** a `simple/` file containing `[[…]]` is stored as `with_footnotes`. | 3 files reclassified (see §5). |
| **D8** | **Two tables**: `quran_translation_sources` + `quran_translation_ayah_entries`. No middle table. | One text row per (source, ayah). |
| **D9** | **Denormalized source metadata** (no languages table in v1). **Direction per source**, not per language. | Source row carries app-facing language + selection metadata only (see §6). |
| **D10** | Source keys / file names = `<languageCode>-<translatorSlug>`; footnote variant gets a marker (e.g. `.fn`). | Deterministic naming; language prefix required for uniqueness. |
| **D11** | **No silent duplicate unattributed copies.** Drop clearly unattributed near-dup copies; keep distinct translations; keep useful simple/with-footnotes type variants; mark anything uncertain `NEEDS_HUMAN_DECISION`. | `ko-unknown`, `sq-unknown` dropped; malayalam + hausa pairs flagged (§7). |
| **D12** | **Unknown provenance allowed for internal dev import only**, captured in manifest/report audit metadata; never presented as publish-ready. | No license/provenance DB columns in v1; provenance warning gate remains report-only. |
| **D13** | Importer must include the **TR-\* validation gates** listed in the planning report §Validation. | `TR-NO-EMPTY-TEXT` hard. |
| **D14** | Propose (do not create) the package at `App/resources/import-sources/quran-translations/` with `README.md`, `manifest.json`, `package-report.md`, `sources/<sourceKey>.json`. | Built in a later "final package curation" step (§8). |

---

## 4. Final counts under the locked policy (package built & independently validated)

> **Status:** The package has been built at `App/resources/import-sources/quran-translations/` and
> passed 852 independent validation checks. The counts below are **final**, not provisional. O1 and O2
> were both resolved as "keep both", so the approved count is firmly **167** (the earlier 165 floor no
> longer applies).

Starting from the 175 ayah-level files (139 simple + 36 with-footnotes):

| Step | Removed | Running total |
|---|---|---|
| Ayah-level files inspected | — | 175 |
| − Fail completeness (≠ 6,236 keyset) | 0 | 175 |
| − Fail **NO-EMPTY-TEXT** (hard, D4) | **6** | 169 |
| − Drop unattributed near-dup copies (D11) | **2** | **167** |

**Approved (final): `167`** — `simple = 129`, `with_footnotes = 38`.
- The `38` with-footnotes count includes **3 reclassified** files (D7).
- **Languages covered: 83** (one language, **Ganda**, is lost as collateral of D4 — its only resource has 2 empty ayahs; see §7/O3).
- **Approved ayah rows: 167 × 6,236 = `1,041,412`**. Approved payload ≈ **279 MiB**.

**Excluded (firm): `19`** = 11 word-by-word + 6 empty-text + 2 unattributed near-dup.
Check: `167 approved + 19 excluded = 186` total. ✓

### Files excluded by NO-EMPTY-TEXT (D4)

| File | Type | Empty ayahs |
|---|---|---|
| `albanian/simple/translation-pioneers-center-simple.json` | simple | 1,955 (truncated) |
| `kannada/with-footnotes/kannada-quran-inline-footnotes.json` | footnotes | 66 |
| `english/simple/en-maarif-ul-quran-simple.json` | simple | 5 |
| `ganda/simple/african-development-foundation-simple.json` | simple | 2 |
| `dutch/simple/nl-abdalsalaam-simple.json` | simple | 1 |
| `urdu/simple/urdu-sayyid-qatab-simple.json` | simple | 1 |

---

## 5. Reclassified files (D7) — kept, type corrected to `with_footnotes`

| File (physically under `simple/`) | Stored type |
|---|---|
| `divehi/simple/ml-shaikh-aboobakr-ibrahim-ali-simple.json` | `with_footnotes` |
| `russian/simple/ru-abu-adel-simple.json` | `with_footnotes` |
| `russian/simple/ru-ministry-of-awqaf-simple.json` | `with_footnotes` |

---

## 6. Locked `quran_translation_sources` field set (D9)

`source_key`, `language_code`, `language_name_en`, `language_name_ar`, `native_name`, `direction`
(per source), `translation_type`, `display_name_en`, `display_name_ar`, `translator_key`,
`translator_name_en`, `translator_name_ar`, `contains_inline_footnotes`, `contains_html_markup`,
`content_coverage_count`.

`display_name_en` and `display_name_ar` are required / NOT NULL. `translator_name_en` and
`translator_name_ar` remain optional / nullable.

Source file paths, package file paths, hashes, file sizes, license/provenance values, and other
manifest metadata remain part of `manifest.json`, `source-display-metadata.json`, and import reports
for validation/audit only; they are not persisted as v1 DB columns.

---

## 7. Open decisions — **RESOLVED** (locked into the built package)

| # | Item | Resolution |
|---|---|---|
| **O1** | Hausa pair (simple ≈ footnotes, 0.89). | **RESOLVED — keep both** as type variants (`ha-abubakar` + `ha-abubakar-mahmood-jummi.fn`). |
| **O2** | Malayalam pair (0.60). | **RESOLVED — keep both** (`ml-abdul-hamid-haidar-kanhi-muhammad` + `ml-abdul-hameed`). |
| **O3** | Ganda loss (only source has 2 empty ayahs). | **RESOLVED — accepted** for v1; no Ganda shipped. Revisit with a complete source later. |
| **O4** | Language codes. | **RESOLVED — `filipino`=`fil`, `tagalog`=`tl` (separate); `kurdish`=`ku`;** shared codes aligned with the Feature 007 manifest. |
| **O5** | Footnote-variant filename marker. | **RESOLVED — `.fn.json`** (e.g. `en-sahih-international.fn.json`). |

With O1 and O2 both "keep both", the **approved count is firmly 167**.

---

## 8. Final Package Curation step — **DONE**

The deterministic curation pass has been run and the package built at
`App/resources/import-sources/quran-translations/` (`resources/` is gitignored). It:

1. Applied D2–D4, D7, D11 programmatically → approved `167` / `simple 129` / `with_footnotes 38`.
2. Resolved O1/O2 as "keep both".
3. Derived `source_key` / `package_file` via `<lang>-<translatorSlug>` (+ `.fn`), set `direction`
   per source, set `contains_inline_footnotes` / `contains_html_markup`; human display/translator
   names derived from filenames (Arabic names left null pending enrichment).
4. Computed `sha256` + `file_size_bytes` per source; wrote `manifest.json`, `README.md`,
   `package-report.md`, and 167 byte-identical files under `sources/`.

`manifest.json` is now the frozen contract the importer validates against (`TR-SOURCE-COUNT` 167,
`TR-TYPE-COUNTS` 129/38, `TR-EXCLUDED-COUNT` 19, `TR-SOURCE-HASH`, …). The package passed 852
independent post-build validation checks.

---

## 9. Readiness

**`READY_FOR_SPEC_KIT`.** All structural decisions are locked, O1–O5 are resolved, and the final
import-source package is built and validated (167 sources, 129/38 split, 83 languages, 19 excluded,
all copies byte-identical to source). Nothing remains before `/speckit.specify`.

---

## 10. Display metadata contract (final)

The package now has **two required contracts**, both consumed by the importer:

- **`manifest.json`** — the frozen **file/hash/source** contract (source set, sha256, sizes,
  coverage, exclusions). Unchanged by display-metadata work.
- **`source-display-metadata.json`** — the required **display metadata** contract (167 records,
  top-level `status: "final"`, each record `metadataStatus: "final_display_ready"`).
  `source-display-metadata.review.json` is retained as the pre-final review overlay.

**Importer requirements (for Spec Kit):** the importer **must read both files** and **fail** if
`source-display-metadata.json` is missing, invalid JSON, incomplete (≠167 records or any empty
`displayNameEn`/`displayNameAr`), or not aligned with the manifest `sourceKey` set.

**Required vs optional fields:**
- **Required (import-blocking):** `displayNameEn`, `displayNameAr`, `languageCode`, `languageNameEn`,
  `languageNameAr`, `nativeName`, `direction`, `translationType`, `sourceKey`, `packageFile`.
- **Optional (non-blocking, not user-facing):** `translatorNameEn`, `translatorNameAr`. These are NOT
  required for import acceptance and must not be the primary selector. `needsReview` is informational
  only and must not block import.

**User-facing v1 selection model:** `languageNameAr` / `languageNameEn` + `translationType` +
`displayNameAr` / `displayNameEn` + translation text. Translator/source/provenance metadata is **not**
part of the v1 user-facing selection model.

**Display-name rules (enforced):** no `displayNameAr`/`displayNameEn` contains
`unattributed` / `unknown` / `غير منسوبة` / `غير معروف`; unknown/unattributed sources get a clean
neutral title (e.g. `az-unknown` → "Azerbaijani Translation" / "ترجمة الأذرية").

**v1 DB import scope unchanged:** do **not** add license / provenance / source-attribution /
package-integrity columns to the v1 translation import (`source_file_original`, `package_file`,
`sha256`, `file_size_bytes`, `license`, `provenance`, `manifest_metadata`). These remain
manifest/report audit data only.
