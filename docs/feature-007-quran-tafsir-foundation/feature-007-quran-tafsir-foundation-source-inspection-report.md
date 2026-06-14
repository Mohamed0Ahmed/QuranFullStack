# Feature 007 — Quran Tafsir Foundation · Source Inspection Report

**Date:** 2026-06-14
**Scope:** Read-only inspection of `resources/tafsirs/`. No code, no migrations, no source-file
changes, no Spec Kit. This report precedes schema design.
**Author:** Capability inspection (pre-Spec-Kit)
**Canonical ayah target:** `quran_ayahs` (6,236 ayahs, Hafs). This feature must **not** modify
`quran_ayahs` and must **not** copy Quran ayah text into tafsir tables.

---

## 0. Executive summary

- **93 tafsir source JSON files** under `resources/tafsirs/languages/<lang>/original/`, across
  **33 language folders**. **42 are Arabic**; **51 are non-Arabic** (50 distinct languages + Arabic).
- **Structure is highly uniform.** Every file is a single JSON object keyed by `verse_key`
  (`"surah:ayah"`). Every file has **exactly 6,236 top-level keys**, all valid verse keys —
  **0 missing, 0 extra, 0 non-verse-key (metadata) keys** in any file.
- Each record value is **one of two shapes**: an **object** `{"text": ...}` /
  `{"text": ..., "ayah_keys": [...]}` (a tafsir block), or a **string** that is a `verse_key`
  pointer to a group-leader ayah. **100%** of string values are valid verse-key pointers — there are
  **no orphan strings and no empty strings** anywhere. This is the Quran.com / QDC grouped-tafsir
  convention.
- **Structural coverage is complete (6,236) for all files**, but **content coverage** (ayahs that
  actually receive tafsir text) is **partial for a handful**: al-Baydawi 3,811; Ibn Uthaymeen 3,456;
  al-Wajiz (al-Wahidi) 1,645; al-Durr al-Manthur 6,171; al-Suddi (`ar-tafseer-al-saddi`) 6,177;
  Ibn al-Qayyim 6,234; Indonesian as-Saadi 6,235. All others = 6,236.
- **No file contains any embedded license, provenance, author, or language metadata.** Author and
  language must be assigned by us during import. **License/provenance is UNKNOWN for every source.**
- **No byte-identical duplicates** in the current set.
- **One non-tafsir resource is still present**: `al-muyassar-fi-al-gharib.json` is a *gharib* (rare-word)
  glossary, not a tafsir. It should be excluded from tafsir import or modelled with an explicit resource kind.
- The **existing reports under `resources/tafsirs/report/` are STALE** — they describe a previous,
  larger state (105 zips → 103 JSON, 52 Arabic). Do not trust their counts; this report supersedes them.

**Verdict: READY WITH NOTES.** No blocking structural issues. Open decisions: licensing/provenance,
v1 scope, handling of partial-coverage and the gharib resource, and the grouped-entry data model.

---

## 1. Folder inventory

### 1.1 Top-level structure

```
resources/tafsirs/
├── README.md                     # conventions (language folders, keep originals, etc.)
├── client-showcase/
│   └── tafsirs-resources.html    # JS-rendered review gallery (filters only; no per-source metadata)
├── languages/                    # 33 language folders
│   └── <language>/
│       ├── original/             # the source tafsir JSON files (the real data)
│       ├── report/               # EMPTY in every language
│       └── samples/              # EMPTY in every language
├── report/                       # 11 inspection/inventory reports (STALE — see §1.4)
├── samples/                      # EMPTY
└── scripts/
    └── audit-tafsir-json-structures.mjs   # the script that produced the (stale) audits
```

### 1.2 Source files (the data)

- **93 tafsir JSON files**, all at `languages/<lang>/original/*.json`.
- File formats present: **JSON only** for data. No SQLite, no CSV, no XML. Supporting material is
  Markdown (reports), one `.mjs` (audit script), one `.html` (showcase).
- All `original/` files are **original/source** files (extracted from per-resource zips, per the
  extraction report). There are **no derived/normalized data files** — normalization has not happened yet.

**Per-language file counts (current):**

| Language | Files | Language | Files | Language | Files |
|---|--:|---|--:|---|--:|
| arabic | 42 | urdu | 5 | english | 5 |
| bengali | 4 | russian | 4 | turkish | 3 |
| indonesian | 2 | kurdish | 2 | persian | 2 |
| albanian | 1 | assamese | 1 | azeri | 1 |
| bosnian | 1 | central-khmer | 1 | chinese | 1 |
| french | 1 | fulah | 1 | hindi | 1 |
| italian | 1 | japanese | 1 | kyrgyz | 1 |
| malayalam | 1 | pashto | 1 | serbian | 1 |
| sinhala | 1 | spanish | 1 | tagalog | 1 |
| tamil | 1 | telugu | 1 | thai | 1 |
| uyghur | 1 | uzbek | 1 | vietnamese | 1 |

Total = **93**.

### 1.3 File-size range

From ~1.0 MB (`tr-tafsir-ibne-kathir.json`) to ~66.6 MB (`mawsoo-at-al-tafsir-al-ma-thoor.json`).
The Arabic classical tafsirs dominate the large end (al-Alusi 56 MB, Tahrir wa Tanwir 48 MB,
al-Razi 48 MB, Ibn Uthaymeen 44 MB, al-Tabari 39 MB).

### 1.4 Reports / samples / notes (non-data)

- `report/` contains 11 files (5 `.md` + their `.json` companions, plus the classification report).
  **All are stale**: `tafsir-json-structure-audit-report.md`, `tafsir-zip-extraction-report.md`,
  `tafsir-download-inventory-report.md`, `tafsirs-html-inventory-report.md`,
  `tafsirs-languages-client-summary.md`, `arabic-tafsir-files-classification-report.md`. They report
  **103 JSON / 52 Arabic**, which no longer matches the folder (93 / 42). The classification report is
  still useful for **Arabic work identification** (names/authors) and for understanding **what was removed**.
- `client-showcase/tafsirs-resources.html` is a client-side gallery; it carries **no static
  per-source metadata** (only filter tags: `arabic`, `non-arabic`, `mukhtasar`, `plain`, `html`,
  `notes`, `saadi`, `kathir`). It explicitly documents a "files deleted before the report" section.
- `samples/` and every `languages/*/samples` + `languages/*/report` are **empty**.

### 1.5 Files removed since the prior audit (provenance note)

Comparing the stale audit's 52-Arabic list with the current 42 Arabic files, **10 Arabic files were
removed** — i.e. the folder was curated down to general tafsir:

| Removed file | Why (per stale classification report) |
|---|---|
| `al-dur-al-masun-lil-samin-al-halabi.json` | i'rab / grammatical analysis |
| `al-i-rab-al-muyassar.json` | i'rab |
| `al-jadwal-fi-i-rab-al-quran.json` | i'rab |
| `alrab-al-quran-li-da-as.json` | i'rab |
| `i-rab-al-quran-li-al-darwish.json` | i'rab |
| `al-nashr-li-ibn-al-jazari.json` | qira'at (readings), not tafsir |
| `tahlil-kalimat-al-qur-an.json` | word-by-word morphology |
| `tadabbur-wa-amal.json` | tadabbur / reflection resource |
| `asseraj-fi-bayan-gharib-alquran.json` | byte-identical duplicate of `tafsir-as-saadi.json` |
| `tafsir-makhi.json` | **a genuine tafsir** (Makki ibn Abi Talib) — removal may be unintended |

> **Note A (open question):** `al-muyassar-fi-al-gharib.json` (a *gharib* glossary) was **kept**,
> while other non-tafsir resources were removed. And `tafsir-makhi.json` (a real tafsir) was removed.
> Both look inconsistent with "general tafsir only" and should be confirmed.

---

## 2. Structure consistency

### 2.1 Are all files the same shape? — Yes, one schema family, two record-value variants.

**Top-level shape (all 93 files):** a single JSON **object** whose keys are `verse_key` strings
(`"1:1"`, `"2:255"`, …). Exactly **6,236 keys** per file. No arrays at the root, no wrapper object,
no metadata header.

**Record-value shape — two variants:**

- **Variant A — grouped (object + pointer)** — most files. A record value is either:
  - an **object** `{"text": "<tafsir>", "ayah_keys": ["s:a", …]}` for a **group leader** (the
    `ayah_keys` array lists every ayah this block covers), or `{"text": "<tafsir>"}` for a single-ayah block; or
  - a **string** equal to the leader's `verse_key` (a **member pointer**), e.g. `"1:2": "1:1"`.
- **Variant B — flat (object only, no grouping)** — 4 files: every record is `{"text": ...}`,
  no `ayah_keys`, no pointers. Members that have no own text appear as `{"text": ""}` (empty).
  Files: `arabic/al-wajiz-wahidi.json`, `arabic/ar-tafseer-al-saddi.json`,
  `english/tafsir-al-jalalayn.json`, `kurdish/kurd-tafsir-rebar.json`.

This matches the **Quran.com / QDC** tafsir export format (confirmed further by `class="qpc-hafs"`,
`verse_key` keys, and HTML `<span>`/`<p>` markup in Arabic files).

### 2.2 Field reference

| Element | Where | Notes |
|---|---|---|
| `verse_key` | top-level object key | `"surah:ayah"`. The only addressing key. Always present, always 6,236. |
| `text` | record object field | The tafsir text (HTML or plain). The single text field. |
| `ayah_keys` | record object field (leaders only, Variant A) | Array of `verse_key`s the block covers. Absent for single-ayah blocks and for Variant B. |
| member pointer | record value is a **string** | A `verse_key` pointing to the leader. Variant A only. |
| metadata / footnotes / license / author / language | **none** | No such fields exist in any file. |

- **Ayah key fields:** only `verse_key` (top-level key) and the `ayah_keys` array. No surah/ayah
  numeric fields, no internal ayah id, no numeric index.
- **Tafsir text field:** `text` (single field; same name in both variants).
- **Metadata fields:** none.
- **Footnotes / HTML fields:** no dedicated field. HTML lives **inline inside `text`**. Footnotes are
  effectively absent (see §7) — only `russian/ru-tafsir-ibne-kahtir.json` uses inline `<sup>`.
- **Missing/nullable fields:** `ayah_keys` is present only on Variant-A leaders. `text` can be an
  empty string in some sources (see §3.3 / §7).

### 2.3 Verdict

One importer with **two record-value branches** (object-or-string; `ayah_keys`-or-not) handles **all 93
files**. No per-file special cases are needed for parsing.

---

## 3. Ayah mapping

### 3.1 How entries map to ayahs

- Mapping is via **`verse_key`** (`"surah:ayah"`) as the top-level key — there is **no** ayah id,
  numeric index, or surah/ayah number field. Resolution to `quran_ayahs` must be done by parsing
  `verse_key` → (surah, ayah) → `ayah_id`.
- All entries are **ayah-level addressed**, but tafsir blocks are frequently **ayah-range / grouped**:
  one text block (`ayah_keys`) covers several consecutive ayahs, and the other members point to it.
- Example (`tafsir-as-saadi.json`): `"1:1"` is a leader with `ayah_keys = ["1:1"…"1:7"]` and the full
  text; `"1:2"`…`"1:7"` are strings `"1:1"`. So al-Fatiha is one block of 7 ayahs.

### 3.2 Coverage vs 6,236

- **Structural key coverage = 6,236 for every file** (0 missing, 0 extra, 0 duplicate top-level keys —
  JSON object keys are unique by construction; verified that all keys match `^\d+:\d+$` and equal the
  canonical Hafs set).
- **No unexpected keys** in any file (no ranges like `"2:1-5"`, no non-numeric keys).

### 3.3 Content coverage (ayahs that actually receive text) — the real coverage signal

Most files cover all 6,236. The exceptions (ayahs structurally present but with **empty text** and no
leader to resolve to):

| Source | Covered ayahs | Ayahs without text | Note |
|---|--:|--:|---|
| `arabic/al-wajiz-wahidi.json` | 1,645 | 4,591 | Flat (Variant B); large content gap |
| `arabic/tafsir-ibn-uthaymeen.json` | 3,456 | 2,780 | Grouped; large content gap (known: incomplete in QDC) |
| `arabic/tafsir-al-baydawi.json` | 3,811 | 2,425 | Grouped; large content gap |
| `arabic/al-durr-al-manthur.json` | 6,171 | 65 | Minor gap |
| `arabic/ar-tafseer-al-saddi.json` | 6,177 | 59 | Flat (Variant B); minor gap (al-Suddi) |
| `arabic/tafsir-ibn-al-qayyim.json` | 6,234 | 2 | Compilation; near-complete |
| `indonesian/id-tafsir-as-saadi.json` | 6,235 | 1 | Near-complete |
| **all other 86 files** | **6,236** | 0 | Complete |

### 3.4 Duplicates / orphans / unexpected keys

- **No duplicate ayah entries** within a source (unique JSON keys).
- **No orphan pointers** and **no empty pointers**: every string value is a valid `verse_key`
  (0 non-pointer strings, 0 empty strings across all 93 files). Pointer targets were sampled and
  resolve to leaders in the same file.
- **No unexpected key formats** anywhere.

> **Validation implication:** "coverage" must be measured as **content coverage** (text actually
> present, following pointers), not just key count — every file is 6,236 by key count and that number
> is misleading on its own.

---

## 4. Languages

### 4.1 Languages present

**33 language folders** = **31 distinct languages** that translate plus Arabic, i.e. **Arabic +
32 other languages**. (Folder = language; folder is authoritative for a file's language.)

### 4.2 Recommended canonical language records (`quran_tafsir_languages`)

| folder | code | name_ar | name_en | native_name | direction |
|---|---|---|---|---|---|
| arabic | `ar` | العربية | Arabic | العربية | rtl |
| albanian | `sq` | الألبانية | Albanian | Shqip | ltr |
| assamese | `as` | الأسامية | Assamese | অসমীয়া | ltr |
| azeri | `az` | الأذرية | Azerbaijani | Azərbaycan | ltr |
| bengali | `bn` | البنغالية | Bengali | বাংলা | ltr |
| bosnian | `bs` | البوسنية | Bosnian | Bosanski | ltr |
| central-khmer | `km` | الخميرية | Khmer | ខ្មែរ | ltr |
| chinese | `zh` | الصينية | Chinese | 中文 | ltr |
| english | `en` | الإنجليزية | English | English | ltr |
| french | `fr` | الفرنسية | French | Français | ltr |
| fulah | `ff` | الفولانية | Fulah | Fulfulde | ltr |
| hindi | `hi` | الهندية | Hindi | हिन्दी | ltr |
| indonesian | `id` | الإندونيسية | Indonesian | Bahasa Indonesia | ltr |
| italian | `it` | الإيطالية | Italian | Italiano | ltr |
| japanese | `ja` | اليابانية | Japanese | 日本語 | ltr |
| kurdish | `ku` (or `ckb`) | الكردية | Kurdish | کوردی / Kurdî | **rtl?** (verify — Sorani) |
| kyrgyz | `ky` | القيرغيزية | Kyrgyz | Кыргызча | ltr |
| malayalam | `ml` | المالايالامية | Malayalam | മലയാളം | ltr |
| pashto | `ps` | البشتوية | Pashto | پښتو | rtl |
| persian | `fa` | الفارسية | Persian | فارسی | rtl |
| russian | `ru` | الروسية | Russian | Русский | ltr |
| serbian | `sr` | الصربية | Serbian | Српски | ltr |
| sinhala | `si` | السنهالية | Sinhala | සිංහල | ltr |
| spanish | `es` | الإسبانية | Spanish | Español | ltr |
| tagalog | `tl` | التاغالوغية | Tagalog | Tagalog | ltr |
| tamil | `ta` | التاميلية | Tamil | தமிழ் | ltr |
| telugu | `te` | التيلوغوية | Telugu | తెలుగు | ltr |
| thai | `th` | التايلاندية | Thai | ไทย | ltr |
| turkish | `tr` | التركية | Turkish | Türkçe | ltr |
| urdu | `ur` | الأردية | Urdu | اردو | rtl |
| uyghur | `ug` | الأويغورية | Uyghur | ئۇيغۇرچە | rtl |
| uzbek | `uz` | الأوزبكية | Uzbek | Oʻzbek | ltr |
| vietnamese | `vi` | الفيتنامية | Vietnamese | Tiếng Việt | ltr |

**RTL languages:** Arabic, Persian, Urdu, Pashto, Uyghur, and (almost certainly) **Kurdish-Sorani**.

### 4.3 Aliases / inconsistencies detected

- README documents canonical folder names for ambiguous English labels: *Central khmer* → `central-khmer`,
  *Kirghiz* → `kyrgyz`, *Sinhalese* → `sinhala`, *Uighur/uyghur* → `uyghur`.
- **Filename language mislabel:** `persian/fr-tafsir-as-saadi.json` carries a **`fr-`** prefix
  (looks French) but is in the **persian** folder and is the **Persian** as-Saadi. Folder is
  authoritative; the filename prefix is misleading.
- **Filename typos:** `english/en-tafisr-ibn-kathir.json` ("tafisr"); `turkish/tr-tafsir-ibne-kathir.json`,
  `russian/ru-tafsir-ibne-kahtir.json`, `bengali/bn-tafseer-ibn-e-kaseer.json`,
  `urdu/tafseer-ibn-e-kaseer-urdu.json` (inconsistent "kathir/kaseer/kahtir" romanizations).
- **Kurdish** code/direction needs confirmation (Sorani `ckb`/rtl vs Kurmanji `kmr`/ltr). `kurd-tafsir-rebar`
  and `kurdish-mokhtasar` both appear to be Sorani (Arabic script).

### 4.4 Which resources are Arabic

The **42 files under `languages/arabic/original/`**. All other 51 files are non-Arabic. (Note: every
file, regardless of language, may *quote* Arabic ayah text inside `text`; that does not change the
resource language.)

---

## 5. Mufassirs / authors / contributors

### 5.1 Does each source have a clear author?

- **Most do** (classical named works → a known mufassir).
- **A significant set is institutional/editorial, not a single mufassir:**
  - **al-Mukhtasar fi Tafsir al-Quran al-Karim** (المختصر في التفسير) — by the **Tafsir Center for
    Quranic Studies (مركز تفسير)**. This is the source behind the Arabic `arabic-al-mukhtasar-…`,
    the English `abridged-explanation-of-the-quran`, **and all ~30 `*-mokhtasar` translations**.
    → **institution**, not a person.
  - **al-Tafsir al-Muyassar** (التفسير الميسر, `ar-tafsir-muyassar`) — **King Fahd Complex** editorial
    committee. → **institution**.
  - **Mawsu'at al-Tafsir al-Ma'thur** (موسوعة التفسير المأثور, `mawsoo-at-…`) — a compiled
    encyclopedia. → **editorial_team / institution**.
- **A few need review** (uncertain author): `urdu/tafsir-bayan-ul-quran.json` (which "Bayan-ul-Quran"?),
  `kurdish/kurd-tafsir-rebar.json` ("Rebar"), `russian/ru-tafseer-al-saddi.json` vs
  `russian/tafsir-as-saadi-russian.json` (two "Saadi" Russian files — confirm one is not al-Suddi).

### 5.2 Recommended `quran_tafsir_authors` / `quran_tafsir_contributors` records

Columns: `slug`, `name_ar`, `name_en`, `type` (`person` | `institution` | `editorial_team` |
`unknown`), `notes`.

| slug | name_ar | name_en | type | notes |
|---|---|---|---|---|
| `tafsir-center` | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | al-Mukhtasar + all mokhtasar translations |
| `king-fahd-complex` | مجمع الملك فهد لطباعة المصحف الشريف | King Fahd Complex | institution | al-Tafsir al-Muyassar |
| `editorial-mathur` | (هيئة تحرير) | Editorial compilation | editorial_team | Mawsu'at al-Tafsir al-Ma'thur |
| `as-saadi` | عبد الرحمن بن ناصر السعدي | Abd al-Rahman al-Sa'di | person | Taysir al-Karim al-Rahman (+ translations) |
| `ibn-kathir` | إسماعيل بن عمر بن كثير | Ibn Kathir | person | Tafsir al-Quran al-Azim (+ translations) |
| `al-suddi` | إسماعيل بن عبد الرحمن السدي | al-Suddi | person | `ar-tafseer-al-saddi` (distinct from al-Sa'di) |
| `al-tabari` | محمد بن جرير الطبري | al-Tabari | person | Jami al-Bayan |
| `al-qurtubi` | محمد بن أحمد القرطبي | al-Qurtubi | person | al-Jami li-Ahkam al-Quran |
| `al-baghawi` | الحسين بن مسعود البغوي | al-Baghawi | person | Ma'alim al-Tanzil |
| `al-razi` | فخر الدين الرازي | al-Razi | person | Mafatih al-Ghayb |
| `al-alusi` | محمود الألوسي | al-Alusi | person | Ruh al-Ma'ani |
| `al-zamakhshari` | محمود الزمخشري | al-Zamakhshari | person | al-Kashshaf |
| `abu-hayyan` | أبو حيّان الأندلسي | Abu Hayyan al-Andalusi | person | al-Bahr al-Muhit |
| `al-wahidi` | علي بن أحمد الواحدي | al-Wahidi | person | al-Basit + al-Wajiz |
| `al-suyuti` | جلال الدين السيوطي | al-Suyuti | person | al-Durr al-Manthur (also half of al-Jalalayn) |
| `ibn-atiyyah` | عبد الحق بن عطية | Ibn Atiyyah | person | al-Muharrar al-Wajiz |
| `ibn-adil` | ابن عادل الحنبلي | Ibn Adil | person | al-Lubab fi Ulum al-Kitab |
| `ibn-ashur` | محمد الطاهر بن عاشور | Ibn Ashur | person | al-Tahrir wa al-Tanwir |
| `tantawi` | محمد سيد طنطاوي | M. S. Tantawi | person | al-Tafsir al-Wasit |
| `al-shawkani` | محمد بن علي الشوكاني | al-Shawkani | person | Fath al-Qadir |
| `al-qannuji` | صديق حسن خان القنوجي | al-Qannuji | person | Fath al-Bayan |
| `al-qasimi` | جمال الدين القاسمي | al-Qasimi | person | Mahasin al-Ta'wil |
| `al-biqai` | إبراهيم البقاعي | al-Biqa'i | person | Nazm al-Durar |
| `abu-suud` | أبو السعود العمادي | Abu al-Su'ud | person | Irshad al-Aql al-Salim |
| `al-mawardi` | علي بن محمد الماوردي | al-Mawardi | person | al-Nukat wa al-Uyun |
| `al-nasafi` | عبد الله بن أحمد النسفي | al-Nasafi | person | Madarik al-Tanzil |
| `al-samani` | منصور بن محمد السمعاني | al-Sam'ani | person | Tafsir al-Sam'ani |
| `al-samarqandi` | نصر بن محمد السمرقندي | al-Samarqandi | person | Bahr al-Ulum |
| `al-thaalibi` | عبد الرحمن الثعالبي | al-Tha'alibi | person | al-Jawahir al-Hisan |
| `ibn-abi-hatim` | ابن أبي حاتم الرازي | Ibn Abi Hatim | person | Tafsir Ibn Abi Hatim |
| `ibn-abi-zamanin` | ابن أبي زمنين | Ibn Abi Zamanin | person | Tafsir Ibn Abi Zamanin |
| `ibn-al-jawzi` | عبد الرحمن بن الجوزي | Ibn al-Jawzi | person | Zad al-Masir |
| `ibn-al-qayyim` | ابن قيّم الجوزية | Ibn al-Qayyim | person | compiled tafsir |
| `ibn-juzayy` | محمد بن جزي الكلبي | Ibn Juzayy | person | al-Tashil li-Ulum al-Tanzil |
| `ibn-uthaymeen` | محمد بن صالح العثيمين | Ibn Uthaymeen | person | Tafsir Ibn Uthaymeen (partial) |
| `al-baydawi` | عبد الله بن عمر البيضاوي | al-Baydawi | person | Anwar al-Tanzil (partial) |
| `al-jalalayn` | المحلّي و السيوطي | al-Mahalli & al-Suyuti | editorial_team | Tafsir al-Jalalayn (two authors) |
| `al-shinqiti` | محمد الأمين الشنقيطي | al-Shinqiti | person | Adwa al-Bayan |
| `al-jazairi` | أبو بكر جابر الجزائري | Abu Bakr al-Jaza'iri | person | Aysar al-Tafasir |
| `al-iji` | محمد بن عبد الرحمن الإيجي | al-Iji | person | Jami al-Bayan (al-Iji) |
| `maarif-author` | محمد شفيع العثماني | Mufti Muhammad Shafi | person | Maarif-ul-Quran (en) |
| `wahiduddin-khan` | وحيد الدين خان | Wahiduddin Khan | person | Tazkirul Quran (en + ur) |
| `abu-bakr-zakaria` | أبو بكر محمد زكريا | Abu Bakr Muhammad Zakaria | person | Bengali tafsir |
| `ahsanul-bayaan` | (صلاح الدين يوسف) | Ahsan-ul-Bayan author | person | confirm attribution |
| `sayyid-qutb` | سيد قطب | Sayyid Qutb | person | Fi Zilal al-Quran (ur) |
| `bayan-ul-quran-author` | (غير مؤكد) | (uncertain) | unknown | Urdu Bayan-ul-Quran — needs review |
| `rebar` | (غير مؤكد) | Rebar (uncertain) | unknown | Kurdish — needs review |

> Non-Arabic translation files of a named work should reference the **original author** (e.g. all
> as-Saadi translations → `as-saadi`) and may additionally carry a `translator` later (no translator
> data exists in-file today).

---

## 6. Tafsir source / book names (standardized)

Conventions used below:
- `source_key` = **`<lang>-<work-slug>`** (guarantees global uniqueness across languages).
- For non-Arabic translations, the **display names embed the language qualifier** so every Arabic name
  and every English name is **distinct** (per requirement).
- `type` ∈ {brief, detailed} is a best-effort editorial estimate (borderline = medium, folded toward
  the closer value with a note). `coverage` = content coverage from §3.3.
- Path = `languages/<lang>/original/<file>`. `author` = slug from §5.2.

### 6.1 Arabic sources (42)

| source_key | display_name_ar | short_name_ar | display_name_en | short_name_en | author | type | coverage | file |
|---|---|---|---|---|---|---|---|---|
| `ar-tabari` | جامع البيان — الطبري | الطبري | Jami al-Bayan (al-Tabari) | al-Tabari | al-tabari | detailed | 6236 | ar-tafsir-al-tabari.json |
| `ar-qurtubi` | الجامع لأحكام القرآن — القرطبي | القرطبي | al-Jami li-Ahkam (al-Qurtubi) | al-Qurtubi | al-qurtubi | detailed | 6236 | ar-tafseer-al-qurtubi.json |
| `ar-ibn-kathir` | تفسير القرآن العظيم — ابن كثير | ابن كثير | Tafsir Ibn Kathir | Ibn Kathir | ibn-kathir | detailed | 6236 | ar-tafsir-ibn-kathir.json |
| `ar-baghawi` | معالم التنزيل — البغوي | البغوي | Ma'alim al-Tanzil (al-Baghawi) | al-Baghawi | al-baghawi | detailed | 6236 | ar-tafsir-al-baghawi.json |
| `ar-razi` | مفاتيح الغيب — الرازي | الرازي | Mafatih al-Ghayb (al-Razi) | al-Razi | al-razi | detailed | 6236 | tafsir-al-razi.json |
| `ar-alusi` | روح المعاني — الألوسي | الألوسي | Ruh al-Ma'ani (al-Alusi) | al-Alusi | al-alusi | detailed | 6236 | tafsir-al-alusi.json |
| `ar-kashshaf` | الكشاف — الزمخشري | الكشاف | al-Kashshaf (al-Zamakhshari) | al-Kashshaf | al-zamakhshari | detailed | 6236 | al-kashshaf-al-zamakhshari.json |
| `ar-bahr-al-muhit` | البحر المحيط — أبو حيّان | البحر المحيط | al-Bahr al-Muhit (Abu Hayyan) | al-Bahr al-Muhit | abu-hayyan | detailed | 6236 | al-bahr-al-muhit.json |
| `ar-basit` | البسيط — الواحدي | البسيط | al-Basit (al-Wahidi) | al-Basit | al-wahidi | detailed | 6236 | al-basit.json |
| `ar-wajiz` | الوجيز — الواحدي | الوجيز | al-Wajiz (al-Wahidi) | al-Wajiz | al-wahidi | brief | **1645** | al-wajiz-wahidi.json |
| `ar-durr-al-manthur` | الدر المنثور — السيوطي | الدر المنثور | al-Durr al-Manthur (al-Suyuti) | al-Durr al-Manthur | al-suyuti | detailed | 6171 | al-durr-al-manthur.json |
| `ar-muharrar-al-wajiz` | المحرر الوجيز — ابن عطية | ابن عطية | al-Muharrar al-Wajiz (Ibn Atiyyah) | Ibn Atiyyah | ibn-atiyyah | detailed | 6236 | al-muharrar-al-wajiz-ibn-atiyyah.json |
| `ar-lubab` | اللباب في علوم الكتاب — ابن عادل | اللباب | al-Lubab (Ibn Adil) | al-Lubab | ibn-adil | detailed | 6236 | al-lubab-fi-ulum-al-kitab.json |
| `ar-tahrir-tanwir` | التحرير والتنوير — ابن عاشور | ابن عاشور | al-Tahrir wa al-Tanwir (Ibn Ashur) | Ibn Ashur | ibn-ashur | detailed | 6236 | ar-tafseer-tahrir-al-tanwir.json |
| `ar-wasit-tantawi` | التفسير الوسيط — طنطاوي | الوسيط (طنطاوي) | al-Tafsir al-Wasit (Tantawi) | al-Wasit (Tantawi) | tantawi | detailed | 6236 | ar-tafsir-al-wasit.json |
| `ar-fath-al-qadir` | فتح القدير — الشوكاني | فتح القدير | Fath al-Qadir (al-Shawkani) | Fath al-Qadir | al-shawkani | detailed | 6236 | fath-al-qadir-al-shawkani.json |
| `ar-fath-al-bayan` | فتح البيان — القنوجي | فتح البيان | Fath al-Bayan (al-Qannuji) | Fath al-Bayan | al-qannuji | detailed | 6236 | fath-al-bayan-li-al-qanuji.json |
| `ar-mahasin-al-tawil` | محاسن التأويل — القاسمي | القاسمي | Mahasin al-Ta'wil (al-Qasimi) | al-Qasimi | al-qasimi | detailed | 6236 | mahasin-al-ta-wil-al-qasimi.json |
| `ar-nazm-al-durar` | نظم الدرر — البقاعي | نظم الدرر | Nazm al-Durar (al-Biqa'i) | Nazm al-Durar | al-biqai | detailed | 6236 | nazam-al-durar-al-biqa-i.json |
| `ar-abu-suud` | إرشاد العقل السليم — أبو السعود | أبو السعود | Irshad al-Aql (Abu al-Su'ud) | Abu al-Su'ud | abu-suud | detailed | 6236 | tafsir-abi-al-su-ood.json |
| `ar-mawardi` | النكت والعيون — الماوردي | الماوردي | al-Nukat wa al-Uyun (al-Mawardi) | al-Mawardi | al-mawardi | detailed | 6236 | tafsir-al-mawardi.json |
| `ar-nasafi` | مدارك التنزيل — النسفي | النسفي | Madarik al-Tanzil (al-Nasafi) | al-Nasafi | al-nasafi | detailed | 6236 | tafsir-al-nasafi.json |
| `ar-samani` | تفسير السمعاني | السمعاني | Tafsir al-Sam'ani | al-Sam'ani | al-samani | detailed | 6236 | tafsir-al-sam-ani.json |
| `ar-samarqandi` | بحر العلوم — السمرقندي | السمرقندي | Bahr al-Ulum (al-Samarqandi) | al-Samarqandi | al-samarqandi | detailed | 6236 | tafsir-al-samarqandi.json |
| `ar-thaalibi` | الجواهر الحسان — الثعالبي | الثعالبي | al-Jawahir al-Hisan (al-Tha'alibi) | al-Tha'alibi | al-thaalibi | detailed | 6236 | tafsir-al-tha-alibi.json |
| `ar-ibn-abi-hatim` | تفسير ابن أبي حاتم | ابن أبي حاتم | Tafsir Ibn Abi Hatim | Ibn Abi Hatim | ibn-abi-hatim | detailed | 6236 | tafsir-ibn-abi-hatim.json |
| `ar-ibn-abi-zamanin` | تفسير ابن أبي زمنين | ابن أبي زمنين | Tafsir Ibn Abi Zamanin | Ibn Abi Zamanin | ibn-abi-zamanin | brief | 6236 | tafsir-ibn-abi-zamanin.json |
| `ar-zad-al-masir` | زاد المسير — ابن الجوزي | زاد المسير | Zad al-Masir (Ibn al-Jawzi) | Zad al-Masir | ibn-al-jawzi | detailed | 6236 | tafsir-ibn-al-jawzi.json |
| `ar-ibn-al-qayyim` | تفسير ابن القيم | ابن القيم | Tafsir Ibn al-Qayyim | Ibn al-Qayyim | ibn-al-qayyim | detailed | 6234 | tafsir-ibn-al-qayyim.json |
| `ar-tashil-ibn-juzayy` | التسهيل لعلوم التنزيل — ابن جزي | ابن جزي | al-Tashil (Ibn Juzayy) | Ibn Juzayy | ibn-juzayy | brief | 6236 | tafsir-ibn-juzay.json |
| `ar-ibn-uthaymeen` | تفسير ابن عثيمين | ابن عثيمين | Tafsir Ibn Uthaymeen | Ibn Uthaymeen | ibn-uthaymeen | detailed | **3456** | tafsir-ibn-uthaymeen.json |
| `ar-baydawi` | أنوار التنزيل — البيضاوي | البيضاوي | Anwar al-Tanzil (al-Baydawi) | al-Baydawi | al-baydawi | detailed | **3811** | tafsir-al-baydawi.json |
| `ar-jalalayn` | تفسير الجلالين | الجلالين | Tafsir al-Jalalayn | al-Jalalayn | al-jalalayn | brief | 6236 | tafsir-jalalayn.json |
| `ar-adwa-al-bayan` | أضواء البيان — الشنقيطي | أضواء البيان | Adwa al-Bayan (al-Shinqiti) | Adwa al-Bayan | al-shinqiti | detailed | 6236 | adwa-al-bayan.json |
| `ar-aysar-al-tafasir` | أيسر التفاسير — الجزائري | أيسر التفاسير | Aysar al-Tafasir (al-Jaza'iri) | Aysar al-Tafasir | al-jazairi | brief | 6236 | abu-bakr-jabir-al-jazairi.json |
| `ar-jami-al-bayan-iji` | جامع البيان — الإيجي | الإيجي | Jami al-Bayan (al-Iji) | al-Iji | al-iji | brief | 6236 | jamia-al-bayan-aliji.json |
| `ar-saadi` | تيسير الكريم الرحمن — السعدي | السعدي | Taysir al-Karim (al-Sa'di) | al-Sa'di | as-saadi | detailed | 6236 | tafsir-as-saadi.json |
| `ar-suddi` | تفسير السدي | السدي | Tafsir al-Suddi | al-Suddi | al-suddi | detailed | 6177 | ar-tafseer-al-saddi.json |
| `ar-muyassar` | التفسير الميسر | الميسر | al-Tafsir al-Muyassar | al-Muyassar | king-fahd-complex | brief | 6236 | ar-tafsir-muyassar.json |
| `ar-mukhtasar` | المختصر في التفسير | المختصر | al-Mukhtasar fi al-Tafsir | al-Mukhtasar | tafsir-center | brief | 6236 | arabic-al-mukhtasar-in-interpreting-the-noble-quran.json |
| `ar-mawsuat-al-mathur` | موسوعة التفسير المأثور | التفسير المأثور | Encyclopedia of Tafsir bil-Ma'thur | al-Ma'thur | editorial-mathur | detailed | 6236 | mawsoo-at-al-tafsir-al-ma-thoor.json |
| `ar-muyassar-fi-al-gharib` | الميسر في غريب القرآن | غريب القرآن | al-Muyassar fi Gharib al-Quran | Gharib (glossary) | — | brief | 6236 | al-muyassar-fi-al-gharib.json |

> **⚠ `ar-muyassar-fi-al-gharib` is a *gharib* (rare-word) glossary, not a tafsir** (avg ~183 chars/entry).
> Recommend **excluding** it from tafsir import, or modelling it with an explicit `resource_kind = gharib`.

### 6.2 Non-Arabic sources (51)

| source_key | display_name_ar | display_name_en | short_name_en | author | type | coverage | file (lang/original/…) |
|---|---|---|---|---|---|---|---|
| `en-mukhtasar` | المختصر في التفسير (الإنجليزية) | The Abridged Explanation (English) | Mukhtasar (EN) | tafsir-center | brief | 6236 | english/abridged-explanation-of-the-quran.json |
| `en-ibn-kathir` | تفسير ابن كثير (الإنجليزية) | Tafsir Ibn Kathir (English) | Ibn Kathir (EN) | ibn-kathir | detailed | 6236 | english/en-tafisr-ibn-kathir.json |
| `en-maarif-ul-quran` | معارف القرآن (الإنجليزية) | Maarif-ul-Quran (English) | Maarif-ul-Quran | maarif-author | detailed | 6236 | english/en-tafsir-maarif-ul-quran.json |
| `en-jalalayn` | تفسير الجلالين (الإنجليزية) | Tafsir al-Jalalayn (English) | Jalalayn (EN) | al-jalalayn | brief | 6236 | english/tafsir-al-jalalayn.json |
| `en-tazkirul-quran` | تذكير القرآن (الإنجليزية) | Tazkirul Quran (English) | Tazkirul Quran (EN) | wahiduddin-khan | detailed | 6236 | english/tazkirul-quran-en.json |
| `sq-saadi` | تفسير السعدي (الألبانية) | Tafsir al-Sa'di (Albanian) | al-Sa'di (SQ) | as-saadi | detailed | 6236 | albanian/sq-saadi.json |
| `as-mukhtasar` | المختصر في التفسير (الأسامية) | The Abridged Explanation (Assamese) | Mukhtasar (AS) | tafsir-center | brief | 6236 | assamese/assamese-mokhtasar.json |
| `az-mukhtasar` | المختصر في التفسير (الأذرية) | The Abridged Explanation (Azerbaijani) | Mukhtasar (AZ) | tafsir-center | brief | 6236 | azeri/azeri-mokhtasar.json |
| `bn-mukhtasar` | المختصر في التفسير (البنغالية) | The Abridged Explanation (Bengali) | Mukhtasar (BN) | tafsir-center | brief | 6236 | bengali/bengali-mokhtasar.json |
| `bn-ibn-kathir` | تفسير ابن كثير (البنغالية) | Tafsir Ibn Kathir (Bengali) | Ibn Kathir (BN) | ibn-kathir | detailed | 6236 | bengali/bn-tafseer-ibn-e-kaseer.json |
| `bn-abu-bakr-zakaria` | تفسير أبو بكر زكريا (البنغالية) | Tafsir Abu Bakr Zakaria (Bengali) | Abu Bakr Zakaria | abu-bakr-zakaria | detailed | 6236 | bengali/bn-tafsir-abu-bakr-zakaria.json |
| `bn-ahsanul-bayaan` | أحسن البيان (البنغالية) | Ahsan-ul-Bayan (Bengali) | Ahsanul Bayan (BN) | ahsanul-bayaan | detailed | 6236 | bengali/bn-tafsir-ahsanul-bayaan.json |
| `bs-mukhtasar` | المختصر في التفسير (البوسنية) | The Abridged Explanation (Bosnian) | Mukhtasar (BS) | tafsir-center | brief | 6236 | bosnian/bosnian-mokhtasar.json |
| `km-mukhtasar` | المختصر في التفسير (الخميرية) | The Abridged Explanation (Khmer) | Mukhtasar (KM) | tafsir-center | brief | 6236 | central-khmer/khmer-mokhtasar.json |
| `zh-mukhtasar` | المختصر في التفسير (الصينية) | The Abridged Explanation (Chinese) | Mukhtasar (ZH) | tafsir-center | brief | 6236 | chinese/chinese-mokhtasar.json |
| `fr-mukhtasar` | المختصر في التفسير (الفرنسية) | The Abridged Explanation (French) | Mukhtasar (FR) | tafsir-center | brief | 6236 | french/french-mokhtasar.json |
| `ff-mukhtasar` | المختصر في التفسير (الفولانية) | The Abridged Explanation (Fulah) | Mukhtasar (FF) | tafsir-center | brief | 6236 | fulah/fulani-mokhtasar.json |
| `hi-mukhtasar` | المختصر في التفسير (الهندية) | The Abridged Explanation (Hindi) | Mukhtasar (HI) | tafsir-center | brief | 6236 | hindi/hindi-mokhtasar.json |
| `id-saadi` | تفسير السعدي (الإندونيسية) | Tafsir al-Sa'di (Indonesian) | al-Sa'di (ID) | as-saadi | detailed | 6235 | indonesian/id-tafsir-as-saadi.json |
| `id-mukhtasar` | المختصر في التفسير (الإندونيسية) | The Abridged Explanation (Indonesian) | Mukhtasar (ID) | tafsir-center | brief | 6236 | indonesian/indonesian-mokhtasar.json |
| `it-mukhtasar` | المختصر في التفسير (الإيطالية) | The Abridged Explanation (Italian) | Mukhtasar (IT) | tafsir-center | brief | 6236 | italian/italian-mokhtasar.json |
| `ja-mukhtasar` | المختصر في التفسير (اليابانية) | The Abridged Explanation (Japanese) | Mukhtasar (JA) | tafsir-center | brief | 6236 | japanese/japanese-mokhtasar.json |
| `ku-rebar` | تفسير ره‌بەر (الكردية) | Tafsir Rebar (Kurdish) | Rebar (KU) | rebar | detailed | 6236 | kurdish/kurd-tafsir-rebar.json |
| `ku-mukhtasar` | المختصر في التفسير (الكردية) | The Abridged Explanation (Kurdish) | Mukhtasar (KU) | tafsir-center | brief | 6236 | kurdish/kurdish-mokhtasar.json |
| `ky-mukhtasar` | المختصر في التفسير (القيرغيزية) | The Abridged Explanation (Kyrgyz) | Mukhtasar (KY) | tafsir-center | brief | 6236 | kyrgyz/kyrgyz-mokhtasar.json |
| `ml-mukhtasar` | المختصر في التفسير (المالايالامية) | The Abridged Explanation (Malayalam) | Mukhtasar (ML) | tafsir-center | brief | 6236 | malayalam/malayalam-mokhtasar.json |
| `ps-mukhtasar` | المختصر في التفسير (البشتوية) | The Abridged Explanation (Pashto) | Mukhtasar (PS) | tafsir-center | brief | 6236 | pashto/pashto-mokhtasar.json |
| `fa-saadi` | تفسير السعدي (الفارسية) | Tafsir al-Sa'di (Persian) | al-Sa'di (FA) | as-saadi | detailed | 6236 | persian/fr-tafsir-as-saadi.json |
| `fa-mukhtasar` | المختصر في التفسير (الفارسية) | The Abridged Explanation (Persian) | Mukhtasar (FA) | tafsir-center | brief | 6236 | persian/persian-mokhtasar.json |
| `ru-suddi-saadi` | تفسير السعدي (الروسية) | Tafsir al-Sa'di (Russian) | al-Sa'di (RU-1) | as-saadi | detailed | 6236 | russian/ru-tafseer-al-saddi.json |
| `ru-ibn-kathir` | تفسير ابن كثير (الروسية) | Tafsir Ibn Kathir (Russian) | Ibn Kathir (RU) | ibn-kathir | detailed | 6236 | russian/ru-tafsir-ibne-kahtir.json |
| `ru-mukhtasar` | المختصر في التفسير (الروسية) | The Abridged Explanation (Russian) | Mukhtasar (RU) | tafsir-center | brief | 6236 | russian/russian-mokhtasar.json |
| `ru-saadi` | تفسير السعدي — نسخة ثانية (الروسية) | Tafsir al-Sa'di — alt (Russian) | al-Sa'di (RU-2) | as-saadi | detailed | 6236 | russian/tafsir-as-saadi-russian.json |
| `sr-mukhtasar` | المختصر في التفسير (الصربية) | The Abridged Explanation (Serbian) | Mukhtasar (SR) | tafsir-center | brief | 6236 | serbian/serbian-mokhtasar.json |
| `si-mukhtasar` | المختصر في التفسير (السنهالية) | The Abridged Explanation (Sinhala) | Mukhtasar (SI) | tafsir-center | brief | 6236 | sinhala/sinhalese-mokhtasar.json |
| `es-mukhtasar` | المختصر في التفسير (الإسبانية) | The Abridged Explanation (Spanish) | Mukhtasar (ES) | tafsir-center | brief | 6236 | spanish/spanish-mokhtasar.json |
| `tl-mukhtasar` | المختصر في التفسير (التاغالوغية) | The Abridged Explanation (Tagalog) | Mukhtasar (TL) | tafsir-center | brief | 6236 | tagalog/tagalog-mokhtasar.json |
| `ta-mukhtasar` | المختصر في التفسير (التاميلية) | The Abridged Explanation (Tamil) | Mukhtasar (TA) | tafsir-center | brief | 6236 | tamil/tamil-mokhtasar.json |
| `te-mukhtasar` | المختصر في التفسير (التيلوغوية) | The Abridged Explanation (Telugu) | Mukhtasar (TE) | tafsir-center | brief | 6236 | telugu/telugu-mokhtasar.json |
| `th-mukhtasar` | المختصر في التفسير (التايلاندية) | The Abridged Explanation (Thai) | Mukhtasar (TH) | tafsir-center | brief | 6236 | thai/thai-mokhtasar.json |
| `tr-saadi` | تفسير السعدي (التركية) | Tafsir al-Sa'di (Turkish) | al-Sa'di (TR) | as-saadi | detailed | 6236 | turkish/tafsir-as-saadi.json |
| `tr-ibn-kathir` | تفسير ابن كثير (التركية) | Tafsir Ibn Kathir (Turkish) | Ibn Kathir (TR) | ibn-kathir | detailed | 6236 | turkish/tr-tafsir-ibne-kathir.json |
| `tr-mukhtasar` | المختصر في التفسير (التركية) | The Abridged Explanation (Turkish) | Mukhtasar (TR) | tafsir-center | brief | 6236 | turkish/turkish-mokhtasar.json |
| `ur-ibn-kathir` | تفسير ابن كثير (الأردية) | Tafsir Ibn Kathir (Urdu) | Ibn Kathir (UR) | ibn-kathir | detailed | 6236 | urdu/tafseer-ibn-e-kaseer-urdu.json |
| `ur-saadi` | تفسير السعدي (الأردية) | Tafsir al-Sa'di (Urdu) | al-Sa'di (UR) | as-saadi | detailed | 6236 | urdu/tafsir-as-saadi.json |
| `ur-bayan-ul-quran` | بيان القرآن (الأردية) | Bayan-ul-Quran (Urdu) | Bayan-ul-Quran | bayan-ul-quran-author | detailed | 6236 | urdu/tafsir-bayan-ul-quran.json |
| `ur-fi-zilal` | في ظلال القرآن (الأردية) | Fi Zilal al-Quran (Urdu) | Fi Zilal (UR) | sayyid-qutb | detailed | 6236 | urdu/tafsir-fe-zalul-quran-syed-qatab.json |
| `ur-tazkirul-quran` | تذكير القرآن (الأردية) | Tazkirul Quran (Urdu) | Tazkirul Quran (UR) | wahiduddin-khan | detailed | 6236 | urdu/tazkiru-quran-ur.json |
| `ug-mukhtasar` | المختصر في التفسير (الأويغورية) | The Abridged Explanation (Uyghur) | Mukhtasar (UG) | tafsir-center | brief | 6236 | uyghur/uyghur-mokhtasar.json |
| `uz-mukhtasar` | المختصر في التفسير (الأوزبكية) | The Abridged Explanation (Uzbek) | Mukhtasar (UZ) | tafsir-center | brief | 6236 | uzbek/uzbek-mokhtasar.json |
| `vi-mukhtasar` | المختصر في التفسير (الفيتنامية) | The Abridged Explanation (Vietnamese) | Mukhtasar (VI) | tafsir-center | brief | 6236 | vietnamese/vietnamese-mokhtasar.json |

**Disambiguation handled:**
- as-Saadi appears in 7 sources → each carries a distinct language qualifier; the two Russian Saadi files
  are `ru-suddi-saadi` (RU-1) vs `ru-saadi` (RU-2) — **confirm whether RU-1 is actually al-Suddi**.
- Ibn Kathir appears in 6 sources → each language-qualified.
- al-Mukhtasar appears in ~31 sources (Arabic + translations) → each language-qualified.
- al-Jalalayn appears Arabic + English; al-Suddi (`ar-suddi`) is distinct from al-Sa'di (`ar-saadi`).

> **⚠ `tr-ibn-kathir` (`turkish/tr-tafsir-ibne-kathir.json`) is suspect**: avg ~132 chars/entry,
> max 1,092 — far too short for Ibn Kathir. Likely a stub/low-quality export. **Review before import.**

---

## 7. Text content quality

- **Format:** mixed. **HTML** for most classical Arabic and the major translations (`<p>`, `<div>`,
  `<span class="qpc-hafs|arabic|brown|blue|gray">`, `<h2>`); **plain text** for the al-Mukhtasar
  translation family (the `*-mokhtasar` files are 0% HTML). HTML density per source ranges from 0%
  (mukhtasar, plain Saddi-Russian, mawardi-low) to 100% (e.g. `fath-al-bayan`, several short ones).
- **Embedded ayah quotations:** Arabic tafsir text routinely **quotes ayah text inline**, wrapped in
  `qpc-hafs` spans or `{…}`/`﴿…﴾` brackets. This is part of the tafsir prose — it is **not** the
  canonical ayah and must be stored **as-is inside tafsir text**, never written into `quran_ayahs`.
- **Footnotes / references:** **no dedicated footnote field** anywhere. Only one file
  (`russian/ru-tafsir-ibne-kahtir.json`) uses inline `<sup>` markers; no file uses a `foot_note`
  attribute. → A `footnotes_json` column is **not required from source**; footnotes (where present)
  are inline within `text`.
- **Empty text:** present in a few sources (see §3.3). Importer must **skip empty-text records**
  (do not create empty rows). Largest: al-Wajiz (4,591 empty), Ibn Uthaymeen (2,780), al-Baydawi (2,425).
- **Very short text:** a few sources have `min length` of 1–3 chars and very low averages
  (`tr-tafsir-ibne-kathir` avg 132; `chinese-mokhtasar` avg 68; `japanese-mokhtasar` avg 105). Some
  are legitimately compact languages; `tr-ibn-kathir` is suspicious. → warrant a **very-short-text warning**.
- **Very long text:** single entries up to ~174 KB (`mawsoo-at-al-tafsir-al-ma-thoor` 174,174;
  `tafsir-ibn-al-qayyim` 170,343; `ibn-uthaymeen` 152,802; `ru-ibn-kathir` 138,044). → text columns
  must be unbounded (`TEXT` / `nvarchar(max)`), not length-limited.
- **Repeated text:** within a source, grouped members **share one text via pointer** (by design — not a
  defect). Across the curated set there are **no byte-identical files**.
- **Malformed content:** none found — all 93 are valid JSON; all keys well-formed; all pointers resolve.

**Recommended text columns:** keep the original `text` verbatim in **`text_html`** (when HTML) /
**`text_plain`** (when plain), or a single `text` + `is_html` flag plus a derived
`text_plain` (HTML-stripped) for future search. **`footnotes_json` not needed in v1** (optional/future).
Keep a `metadata_json` for raw audit (see §10).

---

## 8. License and provenance

- **No license, copyright, source-URL, edition, or provenance field exists in any source file** —
  the JSON is pure `verse_key → text/pointer`.
- **No license metadata** in the folder either (README/showcase/reports carry none).
- **Format strongly indicates a Quran.com / QDC (Quranic Universal Library) tafsir export**
  (`verse_key` keys, `ayah_keys` grouping, `class="qpc-hafs"` markup), but this is **inference, not a
  documented provenance**, and **does not establish a license**.
- **→ License & provenance = UNKNOWN for all 93 sources.** This is the single most important
  non-structural gap.

**Recommended:** emit a **`TAFSIR-LICENSE-KNOWN` warning** for every source until a license/provenance
decision is recorded per source (in the import manifest, not invented). Do not publish externally until
licensing is cleared, especially for modern/copyrighted works (e.g. Ibn Uthaymeen, Tantawi al-Wasit,
Fi Zilal / Sayyid Qutb, Maarif-ul-Quran, Tazkirul Quran, the al-Mukhtasar family).

---

## 9. Recommended v1 scope

**Primary recommendation: import Arabic general-tafsir sources only in v1** — i.e. the 42 Arabic files
**minus** `al-muyassar-fi-al-gharib` (gharib, not tafsir) = **41 Arabic tafsirs**, with partial-coverage
sources allowed but flagged. Design the schema and importer to be **multilingual-ready** so the 51
non-Arabic sources can be added in v2 without migration.

**Reasoning:**
- **Structure is uniform and safe** — one importer handles all 93; technically "all resources" is feasible.
- **Product is Arabic-first/scholarly** — Arabic tafsirs are the core value and are the cleanest to curate.
- **Author/name curation is well-understood for Arabic** (classical named works, already classified);
  non-Arabic needs ~50 display-name + author decisions (incl. uncertain `rebar`, `bayan-ul-quran`, the
  two Russian Saadi files) that should not block v1.
- **Licensing is unknown for all** — keeping v1 to Arabic narrows the licensing review surface.
- It avoids importing the **gharib** resource and the **suspect** `tr-ibn-kathir`.

**Lowest-risk alternative (if preferred):** start with a **small vetted Arabic subset** first —
e.g. `ar-muyassar`, `ar-mukhtasar`, `ar-saadi`, `ar-ibn-kathir`, `ar-tabari`, `ar-qurtubi`,
`ar-baghawi`, `ar-jalalayn` — all complete (6,236), well-known, with clear authors; then expand to the
rest of the Arabic set, then non-Arabic.

**Do not** import "all resources" in v1 *only because* the structure is uniform — the blocker is
licensing + non-Arabic naming/author curation, not parsing.

---

## 10. Recommended database model (v1)

Four tables (matching the expected direction), plus a mapping table to handle grouped (range) tafsir
cleanly. **Verified against source:** entries reference `quran_ayahs` by `ayah_id`; `verse_key` stored
redundantly for audit only; canonical ayah remains `quran_ayahs`; ayah text never copied.

### 10.1 `quran_tafsir_languages`
`id` PK · `code` (ISO 639) **UNIQUE** · `name_ar` · `name_en` · `native_name` (nullable) ·
`direction` (`rtl`|`ltr`).

### 10.2 `quran_tafsir_authors` (contributors)
`id` PK · `slug` **UNIQUE** · `name_ar` · `name_en` · `type` (`person`|`institution`|`editorial_team`|`unknown`) ·
`notes` (nullable). Nullable from sources whose author is unresolved.

### 10.3 `quran_tafsir_sources`
`id` PK · `source_key` (slug) **UNIQUE** · `language_id` FK→`quran_tafsir_languages` ·
`author_id` FK→`quran_tafsir_authors` (nullable) ·
`display_name_ar` **UNIQUE** · `short_name_ar` · `display_name_en` **UNIQUE** · `short_name_en` ·
`kind` (`brief`|`detailed`|`unknown`) · `resource_kind` (`tafsir`|`gharib`, default `tafsir`) ·
`source_file` (relative path) · `source_checksum` (sha256) ·
`ayah_coverage_count` (distinct ayahs with text) · `has_html` (bool) ·
`license` (nullable) · `provenance` (nullable) · `metadata_json` (nullable raw import metadata).

### 10.4 `quran_tafsir_entries`  (one row per distinct text block / group leader)
`id` PK · `source_id` FK→`quran_tafsir_sources` ·
`first_ayah_id` FK→**`quran_ayahs`** · `first_verse_key` (redundant audit) ·
`text_html` (nullable) · `text_plain` (nullable) · `is_html` (bool) ·
`ayah_count` (group size) · `metadata_json` (nullable — e.g. original `ayah_keys`).
**UNIQUE(`source_id`, `first_ayah_id`).**

### 10.5 `quran_tafsir_entry_ayahs`  (ayah-level mapping; gives FK to every covered ayah)
`id` PK · `entry_id` FK→`quran_tafsir_entries` · `source_id` (denormalized, for the constraint below) ·
`ayah_id` FK→**`quran_ayahs`** · `verse_key` (redundant audit).
**UNIQUE(`source_id`, `ayah_id`)** (an ayah maps to at most one entry per source).

> This two-table shape matches the source exactly (a leader's `text` + its `ayah_keys` members /
> pointers) and **stores each text once** (DRY). Querying "all tafsirs for ayah N" = filter
> `quran_tafsir_entry_ayahs` by `ayah_id` → join entries/sources.
>
> **Alternative (one-row-per-ayah, denormalized):** a single `quran_tafsir_entries(source_id, ayah_id,
> text…)` with text **duplicated** across grouped members. Simpler to query, but duplicates large text
> (e.g. al-Tabari ~6,236 rows of multi-KB text instead of ~1,334) — **not recommended** for v1 given
> file sizes.

### 10.6 Constraints / indexes / decisions
- **Unique constraints:** `languages.code`; `authors.slug`; `sources.source_key`,
  `sources.display_name_ar`, `sources.display_name_en`; `entries(source_id, first_ayah_id)`;
  `entry_ayahs(source_id, ayah_id)`.
- **Foreign keys to `quran_ayahs`:** `entries.first_ayah_id` and `entry_ayahs.ayah_id`.
- **Store `verse_key` redundantly?** **Yes** — on `entries.first_verse_key` and `entry_ayahs.verse_key`
  for audit/readability, but `ayah_id` is the canonical join key; `quran_ayahs` stays authoritative.
- **Keep raw metadata JSON?** **Yes, optional** — `sources.metadata_json` (checksum, counts, inferred
  provenance) and optionally `entries.metadata_json` (original `ayah_keys`). Not the source of truth.
- **Indexes for future API/search:** `entry_ayahs(ayah_id)` (ayah → tafsirs), `entries(source_id)`,
  `sources(language_id)`, `sources(author_id)`. (No full-text search in v1 — out of scope.)

---

## 11. Recommended validation checks

**Hard (fail import):**
- **TAFSIR-SOURCE-MANIFEST** — every imported source is declared in an explicit manifest (source_key,
  language, author, file path, expected checksum, license/provenance fields present even if "unknown").
- **TAFSIR-SOURCE-UNCHANGED** — file sha256 matches the manifest before import (re-import safety; the
  source set was already curated/changed once — §1.5).
- **TAFSIR-STRUCTURE-SHAPE** — root is an object of `verse_key → (string | {text[, ayah_keys]})`;
  reject any other shape or any top-level key not matching `^\d+:\d+$`.
- **TAFSIR-LANGUAGE-KNOWN** — each source's language resolves to a `quran_tafsir_languages` row.
- **TAFSIR-SOURCE-NAME-UNIQUE** — `source_key`, `display_name_ar`, and `display_name_en` are each
  unique across sources.
- **TAFSIR-AYAH-RESOLVES** — every top-level `verse_key`, every `ayah_keys` member, and every pointer
  target resolves to an existing `quran_ayahs` row (parse → surah/ayah → ayah_id).
- **TAFSIR-POINTER-RESOLVES** — every string-pointer record points to a leader entry **in the same
  source** (no orphan/empty pointers). *(Current data: 100% pass.)*
- **TAFSIR-DUPLICATE-ENTRY** — no ayah mapped to two entries within one source
  (`UNIQUE(source_id, ayah_id)` in mapping).
- **TAFSIR-TEXT-NOT-EMPTY** — every persisted entry has non-empty `text` after trim; empty-text records
  are skipped, not stored.

**Warnings (report, do not fail):**
- **TAFSIR-COVERAGE** — report **content** coverage vs 6,236 per source; warn when partial
  (al-Wajiz 1,645; Ibn Uthaymeen 3,456; al-Baydawi 3,811; al-Durr 6,171; al-Suddi 6,177; …).
- **TAFSIR-TEXT-TOO-SHORT** — warn on suspiciously short average/min text (flags `tr-ibn-kathir`).
- **TAFSIR-LICENSE-KNOWN** — warn while license/provenance is `unknown` (currently **all** sources).
- **TAFSIR-RESOURCE-KIND** — warn/segregate non-tafsir resources kept in the set
  (`al-muyassar-fi-al-gharib` → `gharib`).
- **TAFSIR-AUTHOR-RESOLVED** — warn when a source has no resolved author
  (`rebar`, `bayan-ul-quran`, RU Saadi/Suddi ambiguity).

---

## 12. Final verdict

### **READY WITH NOTES**

The tafsir corpus is **structurally clean, uniform, complete by key, and safe to normalize**: one
importer with a two-branch record reader covers all 93 files; addressing is consistent `verse_key`;
pointers and grouping are fully resolvable; no duplicates; no malformed content. The four-table model
(+ ayah mapping) in §10 fits the source exactly and keeps `quran_ayahs` canonical.

### Recommended v1 import scope
**Arabic general tafsirs only (41 files): the 42 Arabic files minus `al-muyassar-fi-al-gharib`**, with
partial-coverage sources imported but flagged. Schema multilingual-ready for non-Arabic in v2.
(Lowest-risk alternative: a vetted ~8-source Arabic subset first — §9.)

### Blocking issues
- **None structural.** The only true blocker for *publishing* is **license/provenance = UNKNOWN for all
  sources** — acceptable for internal import/foundation, must be resolved before any external exposure.

### Open questions before Spec Kit
1. **Licensing/provenance** — what is the documented source and license for these files? Which sources
   are clear to use/publish (esp. modern works: Ibn Uthaymeen, al-Wasit/Tantawi, Fi Zilal/Sayyid Qutb,
   Maarif-ul-Quran, Tazkirul Quran, the al-Mukhtasar family)?
2. **v1 scope** — Arabic-only (recommended), vetted Arabic subset, or all resources?
3. **Partial-coverage sources** — import al-Wajiz (1,645), Ibn Uthaymeen (3,456), al-Baydawi (3,811)
   in v1 (flagged), or defer?
4. **Gharib resource** — exclude `al-muyassar-fi-al-gharib`, or keep with `resource_kind = gharib`?
   Also: was removing `tafsir-makhi.json` (a real tafsir) intended (§1.5)?
5. **Data model** — accept the two-table grouped model (entries + entry_ayahs, text stored once), or
   require one-row-per-ayah (denormalized, text duplicated)?
6. **Text storage** — store HTML verbatim + derive `text_plain`, or store both columns explicitly?
7. **Language edge cases** — confirm Kurdish code/direction (`ckb`/rtl vs `kmr`/ltr); confirm the two
   Russian "Saadi" files (is `ru-tafseer-al-saddi` al-Suddi or al-Sa'di?); confirm suspect
   `tr-tafsir-ibne-kathir` (very short text).
8. **Source set freshness** — the `resources/tafsirs/report/` files are stale (103/52 vs current 93/42);
   should they be regenerated/removed so they don't mislead Spec Kit?

---

*End of report. Read-only inspection; no source files were modified, moved, renamed, or deleted, and no
code/migrations/Spec-Kit were run.*
