# Feature 007 — Quran Tafsir Foundation · Curation Review Questions

**Type:** Decisions Mohamed needs to make **before any source file is copied**. Curation only —
nothing has been copied; no source under `resources/tafsirs/` was modified; no import package created.

**Companion artifacts:**
`tafsir-curation-candidate-report.md`, `tafsir-curation-candidate-manifest.draft.json` (draft only).

**Current candidate counts:** 93 total · **84 approved_candidate** · **0 needs-review** ·
9 excluded (7 incomplete + 1 non-tafsir + 1 suspect quality).

> **Update:** Human curation has **resolved all 5 previously needs-review sources** — they are now
> `approved_candidate`. See §0. The questions that were specific to those 5 have been removed.

---

## 0. Resolved during human curation (no longer open)

| # | old source_key | new source_key | decision |
|---|---|---|---|
| 1 | `ru-suddi-saadi` | `ru-saadi-alt` | It is **al-Sa'di** (not al-Suddi); approved as a **second Russian al-Sa'di edition**. |
| 2 | `ku-rebar` | `ckb-rebar` | Approved as **Rebar Kurdish Tafsir** (Sorani/`ckb`, rtl); source identity clear, personal author remains **unknown** (allowed). |
| 3 | `fa-saadi` | `fa-saadi` | Approved as **Persian al-Sa'di**; the `fr-` filename prefix is misleading only. |
| 4 | `ku-mukhtasar` | `ckb-mukhtasar` | Approved as **al-Mukhtasar** in **Central Kurdish / Sorani** (`ckb`, rtl). |
| 5 | `ur-bayan-ul-quran` | `ur-bayan-ul-quran-thanwi` | Approved as **Bayan-ul-Quran by Ashraf Ali Thanwi**. |

Related metadata applied: language **`ckb`** (Central Kurdish / Sorani, rtl) added, generic **`ku`**
dropped; contributor **`ashraf-ali-thanwi`** added, **`bayan-ul-quran-author`** removed, **`rebar`**
kept as type `unknown`. Institutional attributions confirmed (Tafsir Center, King Fahd Complex,
al-Sa'di, al-Jalalayn editorial). **No follow-up needed on these items.**

---

## A. Blocking before copy

These must be answered before we copy anything into an import-sources package.

1. **Approve the candidate list?** Do you approve the **84 `approved_candidate`** sources as the set we
   are allowed to copy for v1? (Yes / a named subset / different cut.)
2. **Scope of v1 copy.** The source inspection report recommended **Arabic-only for v1**. The 84
   approved candidates are **35 Arabic + 49 non-Arabic**. Which do you want to copy first?
   - (a) Arabic approved only (35), non-Arabic in v2; **or**
   - (b) all 84 approved; **or**
   - (c) a small vetted Arabic subset first (e.g. al-Muyassar, al-Mukhtasar, al-Sa'di, Ibn Kathir,
     al-Tabari, al-Qurtubi, al-Baghawi, al-Jalalayn).
3. **License / provenance (the main cross-cutting warning).** License/provenance is **unknown for all
   93 sources** (recorded as warning metadata, never invented). Confirm: it is acceptable to **import
   internally** with `license = unknown` / `provenance = unknown`, deferring external publishing until
   licensing is cleared? Are any sources already cleared to publish? Modern/likely-copyrighted works to
   flag specifically: al-Wasit/Tantawi, Fi Zilal (Sayyid Qutb), Maarif-ul-Quran, Tazkirul Quran,
   Bayan-ul-Quran (Thanwi), and the entire al-Mukhtasar family.
4. **Confirm exclusion of incomplete-coverage sources for v1.** Per decision #1 we **do not copy** the
   7 sources with content coverage < 6,236 (al-Wajiz 1,645; Ibn Uthaymeen 3,456; al-Baydawi 3,811;
   al-Durr al-Manthur 6,171; al-Suddi 6,177; Ibn al-Qayyim 6,234; Indonesian al-Sa'di 6,235).
   Confirm: **ignore now, revisit later** if a more complete edition is sourced?
5. **Confirm exclusion of the non-tafsir resource.** `al-muyassar-fi-al-gharib` stays
   `excluded_non_tafsir` (gharib glossary). Confirm it stays out of the tafsir package (and is it wanted
   later as a separate "gharib" resource, or dropped entirely?).

---

## B. Naming review

- **No source is blocked on display-name uncertainty** (`needs_name_review` count = 0). All 84 approved
  display names are distinct in both Arabic and English (translations are disambiguated by a language
  qualifier, e.g. *Tafsir al-Sa'di (Urdu)*; the two Russian al-Sa'di editions are *Tafsir al-Sa'di
  (Russian)* and *Tafsir al-Sa'di — Russian Alternate Edition*).
- Confirm only: the **language-qualified naming convention** (`<work> (<language>)` /
  `source_key = <lang>-<work>`) is acceptable as the standard.

---

## C. Quality review

1. **`tr-ibn-kathir`** (`turkish/original/tr-tafsir-ibne-kathir.json`) — remains
   `excluded_suspect_quality`. It is structurally complete (6,236) but the text is **far too short for
   Ibn Kathir** (avg ~132 chars/entry; 1:1 = 41 chars) — it reads like brief ayah glosses, not Ibn
   Kathir's tafsir. Confirm the current decision (**drop from v1**), or choose: re-source a complete
   Turkish Ibn Kathir, or keep but relabel as a short gloss (not attributed to Ibn Kathir).

---

## D. Later / not blocking

These do not block the v1 copy decision.

1. **Filename normalization at copy time** (cosmetic): `english/en-tafisr-…` ("tafisr" typo);
   romanization variants `ibne-kahtir` / `ibn-e-kaseer` / `ibne-kathir` (all Ibn Kathir); the
   `fr-` Persian prefix on `fa-saadi`. Confirm we normalize filenames to the `source_key` when copying.
2. **Multiple language editions of the same work** (e.g. al-Sa'di in 7 languages incl. two Russian
   editions, Ibn Kathir in ~5, al-Mukhtasar in ~31) is **expected and intended** — confirm we keep them
   as separate sources.
3. **Stale reports** under `resources/tafsirs/report/` (they describe a previous 103/52 state) are
   **left untouched** for now — confirm whether/when to regenerate or remove them (separate task).
4. **import-sources placeholder** — `resources/import-sources/quran-tafsirs/` was **not** created
   (per instruction). Create it (empty + README "not approved yet") only when you say so.
5. **Revisit excluded incomplete-coverage sources** if/when more complete editions are sourced
   (al-Baydawi, Ibn Uthaymeen, al-Wajiz, al-Suddi, al-Durr al-Manthur, Ibn al-Qayyim, Indonesian al-Sa'di).

---

*End of review questions. No source files copied; no source modified; documentation/curation only.*
