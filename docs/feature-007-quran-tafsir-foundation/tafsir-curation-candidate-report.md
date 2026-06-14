# Feature 007 — Quran Tafsir Foundation · Curation Candidate Report

**Date (UTC):** 2026-06-14T09:56:07Z
**Type:** Curation / review artifact only. **No source files were copied.** No code, no migrations,
no Spec Kit. Source files under `resources/tafsirs/` were **not modified**.
**Companion artifacts:**
`tafsir-curation-candidate-manifest.draft.json` (draft, **not** a final import manifest),
`tafsir-curation-review-questions.md`.
**Upstream inspection:** `feature-007-quran-tafsir-foundation-source-inspection-report.md`.

> **Decisions applied:** (1) any source with content coverage < 6,236 is **excluded** from the v1
> candidate package; (2) uncertain name/author/language sources may appear but are marked
> **needs review**; (3) **no file is copied** until Mohamed approves; (5) `al-muyassar-fi-al-gharib`
> is **excluded_non_tafsir**; (6) license/provenance is **unknown** for all sources (warning metadata,
> not invented). Coverage was **re-audited from the actual files** for this report.

---

## 1. Executive summary

| Metric | Count |
|---|---:|
| Total tafsir files inspected | 93 |
| Total languages | 33 |
| Arabic files | 42 |
| Non-Arabic files | 51 |
| Complete sources (6,236 content-covered ayahs) | 86 |
| Excluded — incomplete coverage | 7 |
| Excluded — non-tafsir | 1 |
| Excluded — suspect quality | 1 |
| Sources needing name review | 0 |
| Sources needing author review | 0 |
| Sources needing language review | 0 |
| **Approved candidates** | **84** |

**Final verdict: READY FOR HUMAN CURATION.**

The candidate set is clean and internally consistent: every excluded source has a concrete,
file-verified reason, and every approved candidate is a complete (6,236) general tafsir with clear
language/author/name. **Human curation is complete — 0 sources remain in needs-review** (the
5 previously open sources were resolved; see "Human curation decisions applied" below). Nothing is
copied. The single remaining cross-cutting warning is **unknown license/provenance for all 93
sources** (does not block curation; must be resolved before publishing).

---

## Human curation decisions applied

The 5 sources previously marked needs-review were resolved by human curation (Mohamed) and are now
`approved_candidate` (each complete at 6,236; `include_in_future_import = true`):

| # | old source_key | new source_key | decision |
|---|---|---|---|
| 1 | `ru-suddi-saadi` | `ru-saadi-alt` | Confirmed **al-Sa'di** (not al-Suddi); approved as a second Russian al-Sa'di edition. |
| 2 | `ku-rebar` | `ckb-rebar` | Approved as **Rebar Kurdish Tafsir** (Sorani/`ckb`, rtl); source identity clear, personal author remains **unknown**. |
| 3 | `fa-saadi` | `fa-saadi` | Approved as **Persian al-Sa'di**; the `fr-` filename prefix is misleading only — language is Persian. |
| 4 | `ku-mukhtasar` | `ckb-mukhtasar` | Approved as **al-Mukhtasar** in **Central Kurdish / Sorani** (`ckb`, rtl). |
| 5 | `ur-bayan-ul-quran` | `ur-bayan-ul-quran-thanwi` | Approved as **Bayan-ul-Quran by Ashraf Ali Thanwi**. |

Related metadata changes: language record **`ckb`** (Central Kurdish / Sorani, rtl) added and the
generic **`ku`** record dropped as unused; contributor **`ashraf-ali-thanwi`** (person) added;
**`bayan-ul-quran-author`** removed (no longer referenced); **`rebar`** retained as type `unknown`.
License/provenance remains **unknown for all 93 sources** (acceptable for internal import curation,
not for external publishing).

---



## 2. Curation statuses

Each source carries **exactly one** status. Rules applied (in priority order):

1. content coverage < 6,236 → **`excluded_incomplete_coverage`** (overrides everything else);
2. not a general tafsir → **`excluded_non_tafsir`**;
3. complete but quality-suspect (beyond mere naming) → **`excluded_suspect_quality`**;
4. complete general tafsir but uncertain metadata → **`needs_name_review`** /
   **`needs_author_review`** / **`needs_language_review`**;
5. complete, general tafsir, clear name/author/language → **`approved_candidate`**.

| Status | Count | `include_in_future_import` |
|---|---:|---|
| `approved_candidate` | 84 | true |
| `needs_name_review` | 0 | false (until confirmed) |
| `needs_author_review` | 0 | false (until confirmed) |
| `needs_language_review` | 0 | false (until confirmed) |
| `excluded_suspect_quality` | 1 | false |
| `excluded_non_tafsir` | 1 | false |
| `excluded_incomplete_coverage` | 7 | false |
| **Total** | **93** | — |

---

## 3. Candidate table (all 93 sources)

Ordered by status group, then language code, then `source_key`. Full review detail is in §4–§6;
the `review_reason` column here is a short tag. `source_file` paths are relative to
`resources/tafsirs/` (the manifest's `sourceRoot`).

| status | source_key | language_code | language_name_ar | language_name_en | display_name_ar | short_name_ar | display_name_en | short_name_en | contributor_key | contributor_name_ar | contributor_name_en | contributor_type | resource_kind | tafsir_kind | content_coverage_count | source_file | review_reason | include_in_future_import |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| approved_candidate | ar-abu-suud | ar | العربية | Arabic | إرشاد العقل السليم — أبو السعود | أبو السعود | Irshad al-Aql (Abu al-Su'ud) | Abu al-Su'ud | abu-suud | أبو السعود العمادي | Abu al-Su'ud | person | tafsir | detailed | 6236 | languages/arabic/original/tafsir-abi-al-su-ood.json | — | true |
| approved_candidate | ar-adwa-al-bayan | ar | العربية | Arabic | أضواء البيان — الشنقيطي | أضواء البيان | Adwa al-Bayan (al-Shinqiti) | Adwa al-Bayan | al-shinqiti | محمد الأمين الشنقيطي | al-Shinqiti | person | tafsir | detailed | 6236 | languages/arabic/original/adwa-al-bayan.json | — | true |
| approved_candidate | ar-alusi | ar | العربية | Arabic | روح المعاني — الألوسي | الألوسي | Ruh al-Ma'ani (al-Alusi) | al-Alusi | al-alusi | محمود الألوسي | al-Alusi | person | tafsir | detailed | 6236 | languages/arabic/original/tafsir-al-alusi.json | — | true |
| approved_candidate | ar-aysar-al-tafasir | ar | العربية | Arabic | أيسر التفاسير — الجزائري | أيسر التفاسير | Aysar al-Tafasir (al-Jaza'iri) | Aysar al-Tafasir | al-jazairi | أبو بكر جابر الجزائري | Abu Bakr al-Jaza'iri | person | tafsir | brief | 6236 | languages/arabic/original/abu-bakr-jabir-al-jazairi.json | — | true |
| approved_candidate | ar-baghawi | ar | العربية | Arabic | معالم التنزيل — البغوي | البغوي | Ma'alim al-Tanzil (al-Baghawi) | al-Baghawi | al-baghawi | الحسين بن مسعود البغوي | al-Baghawi | person | tafsir | detailed | 6236 | languages/arabic/original/ar-tafsir-al-baghawi.json | — | true |
| approved_candidate | ar-bahr-al-muhit | ar | العربية | Arabic | البحر المحيط — أبو حيّان | البحر المحيط | al-Bahr al-Muhit (Abu Hayyan) | al-Bahr al-Muhit | abu-hayyan | أبو حيّان الأندلسي | Abu Hayyan al-Andalusi | person | tafsir | detailed | 6236 | languages/arabic/original/al-bahr-al-muhit.json | — | true |
| approved_candidate | ar-basit | ar | العربية | Arabic | البسيط — الواحدي | البسيط | al-Basit (al-Wahidi) | al-Basit | al-wahidi | علي بن أحمد الواحدي | al-Wahidi | person | tafsir | detailed | 6236 | languages/arabic/original/al-basit.json | — | true |
| approved_candidate | ar-fath-al-bayan | ar | العربية | Arabic | فتح البيان — القنوجي | فتح البيان | Fath al-Bayan (al-Qannuji) | Fath al-Bayan | al-qannuji | صديق حسن خان القنوجي | al-Qannuji | person | tafsir | detailed | 6236 | languages/arabic/original/fath-al-bayan-li-al-qanuji.json | — | true |
| approved_candidate | ar-fath-al-qadir | ar | العربية | Arabic | فتح القدير — الشوكاني | فتح القدير | Fath al-Qadir (al-Shawkani) | Fath al-Qadir | al-shawkani | محمد بن علي الشوكاني | al-Shawkani | person | tafsir | detailed | 6236 | languages/arabic/original/fath-al-qadir-al-shawkani.json | — | true |
| approved_candidate | ar-ibn-abi-hatim | ar | العربية | Arabic | تفسير ابن أبي حاتم | ابن أبي حاتم | Tafsir Ibn Abi Hatim | Ibn Abi Hatim | ibn-abi-hatim | ابن أبي حاتم الرازي | Ibn Abi Hatim | person | tafsir | detailed | 6236 | languages/arabic/original/tafsir-ibn-abi-hatim.json | — | true |
| approved_candidate | ar-ibn-abi-zamanin | ar | العربية | Arabic | تفسير ابن أبي زمنين | ابن أبي زمنين | Tafsir Ibn Abi Zamanin | Ibn Abi Zamanin | ibn-abi-zamanin | ابن أبي زمنين | Ibn Abi Zamanin | person | tafsir | brief | 6236 | languages/arabic/original/tafsir-ibn-abi-zamanin.json | — | true |
| approved_candidate | ar-ibn-kathir | ar | العربية | Arabic | تفسير القرآن العظيم — ابن كثير | ابن كثير | Tafsir Ibn Kathir | Ibn Kathir | ibn-kathir | إسماعيل بن عمر بن كثير | Ibn Kathir | person | tafsir | detailed | 6236 | languages/arabic/original/ar-tafsir-ibn-kathir.json | — | true |
| approved_candidate | ar-jalalayn | ar | العربية | Arabic | تفسير الجلالين | الجلالين | Tafsir al-Jalalayn | al-Jalalayn | al-jalalayn | المحلّي والسيوطي | al-Mahalli & al-Suyuti | editorial_team | tafsir | brief | 6236 | languages/arabic/original/tafsir-jalalayn.json | — | true |
| approved_candidate | ar-jami-al-bayan-iji | ar | العربية | Arabic | جامع البيان — الإيجي | الإيجي | Jami al-Bayan (al-Iji) | al-Iji | al-iji | محمد بن عبد الرحمن الإيجي | al-Iji | person | tafsir | brief | 6236 | languages/arabic/original/jamia-al-bayan-aliji.json | — | true |
| approved_candidate | ar-kashshaf | ar | العربية | Arabic | الكشاف — الزمخشري | الكشاف | al-Kashshaf (al-Zamakhshari) | al-Kashshaf | al-zamakhshari | محمود الزمخشري | al-Zamakhshari | person | tafsir | detailed | 6236 | languages/arabic/original/al-kashshaf-al-zamakhshari.json | — | true |
| approved_candidate | ar-lubab | ar | العربية | Arabic | اللباب في علوم الكتاب — ابن عادل | اللباب | al-Lubab (Ibn Adil) | al-Lubab | ibn-adil | ابن عادل الحنبلي | Ibn Adil | person | tafsir | detailed | 6236 | languages/arabic/original/al-lubab-fi-ulum-al-kitab.json | — | true |
| approved_candidate | ar-mahasin-al-tawil | ar | العربية | Arabic | محاسن التأويل — القاسمي | القاسمي | Mahasin al-Ta'wil (al-Qasimi) | al-Qasimi | al-qasimi | جمال الدين القاسمي | al-Qasimi | person | tafsir | detailed | 6236 | languages/arabic/original/mahasin-al-ta-wil-al-qasimi.json | — | true |
| approved_candidate | ar-mawardi | ar | العربية | Arabic | النكت والعيون — الماوردي | الماوردي | al-Nukat wa al-Uyun (al-Mawardi) | al-Mawardi | al-mawardi | علي بن محمد الماوردي | al-Mawardi | person | tafsir | detailed | 6236 | languages/arabic/original/tafsir-al-mawardi.json | — | true |
| approved_candidate | ar-mawsuat-al-mathur | ar | العربية | Arabic | موسوعة التفسير المأثور | التفسير المأثور | Encyclopedia of Tafsir bil-Ma'thur | al-Ma'thur | editorial-mathur | هيئة تحرير (موسوعة التفسير المأثور) | Editorial compilation | editorial_team | tafsir | detailed | 6236 | languages/arabic/original/mawsoo-at-al-tafsir-al-ma-thoor.json | — | true |
| approved_candidate | ar-muharrar-al-wajiz | ar | العربية | Arabic | المحرر الوجيز — ابن عطية | ابن عطية | al-Muharrar al-Wajiz (Ibn Atiyyah) | Ibn Atiyyah | ibn-atiyyah | عبد الحق بن عطية | Ibn Atiyyah | person | tafsir | detailed | 6236 | languages/arabic/original/al-muharrar-al-wajiz-ibn-atiyyah.json | — | true |
| approved_candidate | ar-mukhtasar | ar | العربية | Arabic | المختصر في التفسير | المختصر | al-Mukhtasar fi al-Tafsir | al-Mukhtasar | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/arabic/original/arabic-al-mukhtasar-in-interpreting-the-noble-quran.json | — | true |
| approved_candidate | ar-muyassar | ar | العربية | Arabic | التفسير الميسر | الميسر | al-Tafsir al-Muyassar | al-Muyassar | king-fahd-complex | مجمع الملك فهد لطباعة المصحف الشريف | King Fahd Complex | institution | tafsir | brief | 6236 | languages/arabic/original/ar-tafsir-muyassar.json | — | true |
| approved_candidate | ar-nasafi | ar | العربية | Arabic | مدارك التنزيل — النسفي | النسفي | Madarik al-Tanzil (al-Nasafi) | al-Nasafi | al-nasafi | عبد الله بن أحمد النسفي | al-Nasafi | person | tafsir | detailed | 6236 | languages/arabic/original/tafsir-al-nasafi.json | — | true |
| approved_candidate | ar-nazm-al-durar | ar | العربية | Arabic | نظم الدرر — البقاعي | نظم الدرر | Nazm al-Durar (al-Biqa'i) | Nazm al-Durar | al-biqai | إبراهيم البقاعي | al-Biqa'i | person | tafsir | detailed | 6236 | languages/arabic/original/nazam-al-durar-al-biqa-i.json | — | true |
| approved_candidate | ar-qurtubi | ar | العربية | Arabic | الجامع لأحكام القرآن — القرطبي | القرطبي | al-Jami li-Ahkam (al-Qurtubi) | al-Qurtubi | al-qurtubi | محمد بن أحمد القرطبي | al-Qurtubi | person | tafsir | detailed | 6236 | languages/arabic/original/ar-tafseer-al-qurtubi.json | — | true |
| approved_candidate | ar-razi | ar | العربية | Arabic | مفاتيح الغيب — الرازي | الرازي | Mafatih al-Ghayb (al-Razi) | al-Razi | al-razi | فخر الدين الرازي | al-Razi | person | tafsir | detailed | 6236 | languages/arabic/original/tafsir-al-razi.json | — | true |
| approved_candidate | ar-saadi | ar | العربية | Arabic | تيسير الكريم الرحمن — السعدي | السعدي | Taysir al-Karim (al-Sa'di) | al-Sa'di | as-saadi | عبد الرحمن بن ناصر السعدي | Abd al-Rahman al-Sa'di | person | tafsir | detailed | 6236 | languages/arabic/original/tafsir-as-saadi.json | — | true |
| approved_candidate | ar-samani | ar | العربية | Arabic | تفسير السمعاني | السمعاني | Tafsir al-Sam'ani | al-Sam'ani | al-samani | منصور بن محمد السمعاني | al-Sam'ani | person | tafsir | detailed | 6236 | languages/arabic/original/tafsir-al-sam-ani.json | — | true |
| approved_candidate | ar-samarqandi | ar | العربية | Arabic | بحر العلوم — السمرقندي | السمرقندي | Bahr al-Ulum (al-Samarqandi) | al-Samarqandi | al-samarqandi | نصر بن محمد السمرقندي | al-Samarqandi | person | tafsir | detailed | 6236 | languages/arabic/original/tafsir-al-samarqandi.json | — | true |
| approved_candidate | ar-tabari | ar | العربية | Arabic | جامع البيان — الطبري | الطبري | Jami al-Bayan (al-Tabari) | al-Tabari | al-tabari | محمد بن جرير الطبري | al-Tabari | person | tafsir | detailed | 6236 | languages/arabic/original/ar-tafsir-al-tabari.json | — | true |
| approved_candidate | ar-tahrir-tanwir | ar | العربية | Arabic | التحرير والتنوير — ابن عاشور | ابن عاشور | al-Tahrir wa al-Tanwir (Ibn Ashur) | Ibn Ashur | ibn-ashur | محمد الطاهر بن عاشور | Ibn Ashur | person | tafsir | detailed | 6236 | languages/arabic/original/ar-tafseer-tahrir-al-tanwir.json | — | true |
| approved_candidate | ar-tashil-ibn-juzayy | ar | العربية | Arabic | التسهيل لعلوم التنزيل — ابن جزي | ابن جزي | al-Tashil (Ibn Juzayy) | Ibn Juzayy | ibn-juzayy | محمد بن جزي الكلبي | Ibn Juzayy | person | tafsir | brief | 6236 | languages/arabic/original/tafsir-ibn-juzay.json | — | true |
| approved_candidate | ar-thaalibi | ar | العربية | Arabic | الجواهر الحسان — الثعالبي | الثعالبي | al-Jawahir al-Hisan (al-Tha'alibi) | al-Tha'alibi | al-thaalibi | عبد الرحمن الثعالبي | al-Tha'alibi | person | tafsir | detailed | 6236 | languages/arabic/original/tafsir-al-tha-alibi.json | — | true |
| approved_candidate | ar-wasit-tantawi | ar | العربية | Arabic | التفسير الوسيط — طنطاوي | الوسيط (طنطاوي) | al-Tafsir al-Wasit (Tantawi) | al-Wasit (Tantawi) | tantawi | محمد سيد طنطاوي | Muhammad Sayyid Tantawi | person | tafsir | detailed | 6236 | languages/arabic/original/ar-tafsir-al-wasit.json | — | true |
| approved_candidate | ar-zad-al-masir | ar | العربية | Arabic | زاد المسير — ابن الجوزي | زاد المسير | Zad al-Masir (Ibn al-Jawzi) | Zad al-Masir | ibn-al-jawzi | عبد الرحمن بن الجوزي | Ibn al-Jawzi | person | tafsir | detailed | 6236 | languages/arabic/original/tafsir-ibn-al-jawzi.json | — | true |
| approved_candidate | as-mukhtasar | as | الأسامية | Assamese | المختصر في التفسير (الأسامية) | المختصر | The Abridged Explanation (Assamese) | Mukhtasar (AS) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/assamese/original/assamese-mokhtasar.json | — | true |
| approved_candidate | az-mukhtasar | az | الأذرية | Azerbaijani | المختصر في التفسير (الأذرية) | المختصر | The Abridged Explanation (Azerbaijani) | Mukhtasar (AZ) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/azeri/original/azeri-mokhtasar.json | — | true |
| approved_candidate | bn-abu-bakr-zakaria | bn | البنغالية | Bengali | تفسير أبو بكر زكريا (البنغالية) | أبو بكر زكريا | Tafsir Abu Bakr Zakaria (Bengali) | Abu Bakr Zakaria (BN) | abu-bakr-zakaria | أبو بكر محمد زكريا | Abu Bakr Muhammad Zakaria | person | tafsir | detailed | 6236 | languages/bengali/original/bn-tafsir-abu-bakr-zakaria.json | — | true |
| approved_candidate | bn-ahsanul-bayaan | bn | البنغالية | Bengali | أحسن البيان (البنغالية) | أحسن البيان | Ahsan-ul-Bayan (Bengali) | Ahsanul Bayan (BN) | ahsanul-bayaan | حافظ صلاح الدين يوسف | Hafiz Salahuddin Yusuf | person | tafsir | detailed | 6236 | languages/bengali/original/bn-tafsir-ahsanul-bayaan.json | — | true |
| approved_candidate | bn-ibn-kathir | bn | البنغالية | Bengali | تفسير ابن كثير (البنغالية) | ابن كثير | Tafsir Ibn Kathir (Bengali) | Ibn Kathir (BN) | ibn-kathir | إسماعيل بن عمر بن كثير | Ibn Kathir | person | tafsir | detailed | 6236 | languages/bengali/original/bn-tafseer-ibn-e-kaseer.json | — | true |
| approved_candidate | bn-mukhtasar | bn | البنغالية | Bengali | المختصر في التفسير (البنغالية) | المختصر | The Abridged Explanation (Bengali) | Mukhtasar (BN) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/bengali/original/bengali-mokhtasar.json | — | true |
| approved_candidate | bs-mukhtasar | bs | البوسنية | Bosnian | المختصر في التفسير (البوسنية) | المختصر | The Abridged Explanation (Bosnian) | Mukhtasar (BS) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/bosnian/original/bosnian-mokhtasar.json | — | true |
| approved_candidate | ckb-mukhtasar | ckb | الكردية السورانية | Central Kurdish / Sorani | المختصر في التفسير (الكردية السورانية) | المختصر | The Abridged Explanation (Central Kurdish / Sorani) | Mukhtasar (CKB) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/kurdish/original/kurdish-mokhtasar.json | — | true |
| approved_candidate | ckb-rebar | ckb | الكردية السورانية | Central Kurdish / Sorani | تفسير رێبەر (الكردية السورانية) | رێبەر | Rebar Kurdish Tafsir | Rebar (CKB) | rebar | غير محدد | Rebar / Unknown | unknown | tafsir | detailed | 6236 | languages/kurdish/original/kurd-tafsir-rebar.json | — | true |
| approved_candidate | en-ibn-kathir | en | الإنجليزية | English | تفسير ابن كثير (الإنجليزية) | ابن كثير | Tafsir Ibn Kathir (English) | Ibn Kathir (EN) | ibn-kathir | إسماعيل بن عمر بن كثير | Ibn Kathir | person | tafsir | detailed | 6236 | languages/english/original/en-tafisr-ibn-kathir.json | — | true |
| approved_candidate | en-jalalayn | en | الإنجليزية | English | تفسير الجلالين (الإنجليزية) | الجلالين | Tafsir al-Jalalayn (English) | Jalalayn (EN) | al-jalalayn | المحلّي والسيوطي | al-Mahalli & al-Suyuti | editorial_team | tafsir | brief | 6236 | languages/english/original/tafsir-al-jalalayn.json | — | true |
| approved_candidate | en-maarif-ul-quran | en | الإنجليزية | English | معارف القرآن (الإنجليزية) | معارف القرآن | Maarif-ul-Quran (English) | Maarif-ul-Quran (EN) | maarif-author | محمد شفيع العثماني | Mufti Muhammad Shafi | person | tafsir | detailed | 6236 | languages/english/original/en-tafsir-maarif-ul-quran.json | — | true |
| approved_candidate | en-mukhtasar | en | الإنجليزية | English | المختصر في التفسير (الإنجليزية) | المختصر | The Abridged Explanation (English) | Mukhtasar (EN) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/english/original/abridged-explanation-of-the-quran.json | — | true |
| approved_candidate | en-tazkirul-quran | en | الإنجليزية | English | تذكير القرآن (الإنجليزية) | تذكير القرآن | Tazkirul Quran (English) | Tazkirul Quran (EN) | wahiduddin-khan | وحيد الدين خان | Wahiduddin Khan | person | tafsir | detailed | 6236 | languages/english/original/tazkirul-quran-en.json | — | true |
| approved_candidate | es-mukhtasar | es | الإسبانية | Spanish | المختصر في التفسير (الإسبانية) | المختصر | The Abridged Explanation (Spanish) | Mukhtasar (ES) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/spanish/original/spanish-mokhtasar.json | — | true |
| approved_candidate | fa-mukhtasar | fa | الفارسية | Persian | المختصر في التفسير (الفارسية) | المختصر | The Abridged Explanation (Persian) | Mukhtasar (FA) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/persian/original/persian-mokhtasar.json | — | true |
| approved_candidate | fa-saadi | fa | الفارسية | Persian | تفسير السعدي (الفارسية) | السعدي | Tafsir al-Sa'di (Persian) | al-Sa'di (FA) | as-saadi | عبد الرحمن بن ناصر السعدي | Abd al-Rahman al-Sa'di | person | tafsir | detailed | 6236 | languages/persian/original/fr-tafsir-as-saadi.json | — | true |
| approved_candidate | ff-mukhtasar | ff | الفولانية | Fulah | المختصر في التفسير (الفولانية) | المختصر | The Abridged Explanation (Fulah) | Mukhtasar (FF) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/fulah/original/fulani-mokhtasar.json | — | true |
| approved_candidate | fr-mukhtasar | fr | الفرنسية | French | المختصر في التفسير (الفرنسية) | المختصر | The Abridged Explanation (French) | Mukhtasar (FR) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/french/original/french-mokhtasar.json | — | true |
| approved_candidate | hi-mukhtasar | hi | الهندية | Hindi | المختصر في التفسير (الهندية) | المختصر | The Abridged Explanation (Hindi) | Mukhtasar (HI) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/hindi/original/hindi-mokhtasar.json | — | true |
| approved_candidate | id-mukhtasar | id | الإندونيسية | Indonesian | المختصر في التفسير (الإندونيسية) | المختصر | The Abridged Explanation (Indonesian) | Mukhtasar (ID) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/indonesian/original/indonesian-mokhtasar.json | — | true |
| approved_candidate | it-mukhtasar | it | الإيطالية | Italian | المختصر في التفسير (الإيطالية) | المختصر | The Abridged Explanation (Italian) | Mukhtasar (IT) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/italian/original/italian-mokhtasar.json | — | true |
| approved_candidate | ja-mukhtasar | ja | اليابانية | Japanese | المختصر في التفسير (اليابانية) | المختصر | The Abridged Explanation (Japanese) | Mukhtasar (JA) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/japanese/original/japanese-mokhtasar.json | — | true |
| approved_candidate | km-mukhtasar | km | الخميرية | Khmer | المختصر في التفسير (الخميرية) | المختصر | The Abridged Explanation (Khmer) | Mukhtasar (KM) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/central-khmer/original/khmer-mokhtasar.json | — | true |
| approved_candidate | ky-mukhtasar | ky | القيرغيزية | Kyrgyz | المختصر في التفسير (القيرغيزية) | المختصر | The Abridged Explanation (Kyrgyz) | Mukhtasar (KY) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/kyrgyz/original/kyrgyz-mokhtasar.json | — | true |
| approved_candidate | ml-mukhtasar | ml | المالايالامية | Malayalam | المختصر في التفسير (المالايالامية) | المختصر | The Abridged Explanation (Malayalam) | Mukhtasar (ML) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/malayalam/original/malayalam-mokhtasar.json | — | true |
| approved_candidate | ps-mukhtasar | ps | البشتوية | Pashto | المختصر في التفسير (البشتوية) | المختصر | The Abridged Explanation (Pashto) | Mukhtasar (PS) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/pashto/original/pashto-mokhtasar.json | — | true |
| approved_candidate | ru-ibn-kathir | ru | الروسية | Russian | تفسير ابن كثير (الروسية) | ابن كثير | Tafsir Ibn Kathir (Russian) | Ibn Kathir (RU) | ibn-kathir | إسماعيل بن عمر بن كثير | Ibn Kathir | person | tafsir | detailed | 6236 | languages/russian/original/ru-tafsir-ibne-kahtir.json | — | true |
| approved_candidate | ru-mukhtasar | ru | الروسية | Russian | المختصر في التفسير (الروسية) | المختصر | The Abridged Explanation (Russian) | Mukhtasar (RU) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/russian/original/russian-mokhtasar.json | — | true |
| approved_candidate | ru-saadi | ru | الروسية | Russian | تفسير السعدي (الروسية) | السعدي | Tafsir al-Sa'di (Russian) | al-Sa'di (RU-2) | as-saadi | عبد الرحمن بن ناصر السعدي | Abd al-Rahman al-Sa'di | person | tafsir | detailed | 6236 | languages/russian/original/tafsir-as-saadi-russian.json | — | true |
| approved_candidate | ru-saadi-alt | ru | الروسية | Russian | تفسير السعدي — النسخة الروسية الثانية | السعدي | Tafsir al-Sa'di — Russian Alternate Edition | al-Sa'di (RU Alt) | as-saadi | عبد الرحمن بن ناصر السعدي | Abd al-Rahman al-Sa'di | person | tafsir | detailed | 6236 | languages/russian/original/ru-tafseer-al-saddi.json | — | true |
| approved_candidate | si-mukhtasar | si | السنهالية | Sinhala | المختصر في التفسير (السنهالية) | المختصر | The Abridged Explanation (Sinhala) | Mukhtasar (SI) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/sinhala/original/sinhalese-mokhtasar.json | — | true |
| approved_candidate | sq-saadi | sq | الألبانية | Albanian | تفسير السعدي (الألبانية) | السعدي | Tafsir al-Sa'di (Albanian) | al-Sa'di (SQ) | as-saadi | عبد الرحمن بن ناصر السعدي | Abd al-Rahman al-Sa'di | person | tafsir | detailed | 6236 | languages/albanian/original/sq-saadi.json | — | true |
| approved_candidate | sr-mukhtasar | sr | الصربية | Serbian | المختصر في التفسير (الصربية) | المختصر | The Abridged Explanation (Serbian) | Mukhtasar (SR) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/serbian/original/serbian-mokhtasar.json | — | true |
| approved_candidate | ta-mukhtasar | ta | التاميلية | Tamil | المختصر في التفسير (التاميلية) | المختصر | The Abridged Explanation (Tamil) | Mukhtasar (TA) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/tamil/original/tamil-mokhtasar.json | — | true |
| approved_candidate | te-mukhtasar | te | التيلوغوية | Telugu | المختصر في التفسير (التيلوغوية) | المختصر | The Abridged Explanation (Telugu) | Mukhtasar (TE) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/telugu/original/telugu-mokhtasar.json | — | true |
| approved_candidate | th-mukhtasar | th | التايلاندية | Thai | المختصر في التفسير (التايلاندية) | المختصر | The Abridged Explanation (Thai) | Mukhtasar (TH) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/thai/original/thai-mokhtasar.json | — | true |
| approved_candidate | tl-mukhtasar | tl | التاغالوغية | Tagalog | المختصر في التفسير (التاغالوغية) | المختصر | The Abridged Explanation (Tagalog) | Mukhtasar (TL) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/tagalog/original/tagalog-mokhtasar.json | — | true |
| approved_candidate | tr-mukhtasar | tr | التركية | Turkish | المختصر في التفسير (التركية) | المختصر | The Abridged Explanation (Turkish) | Mukhtasar (TR) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/turkish/original/turkish-mokhtasar.json | — | true |
| approved_candidate | tr-saadi | tr | التركية | Turkish | تفسير السعدي (التركية) | السعدي | Tafsir al-Sa'di (Turkish) | al-Sa'di (TR) | as-saadi | عبد الرحمن بن ناصر السعدي | Abd al-Rahman al-Sa'di | person | tafsir | detailed | 6236 | languages/turkish/original/tafsir-as-saadi.json | — | true |
| approved_candidate | ug-mukhtasar | ug | الأويغورية | Uyghur | المختصر في التفسير (الأويغورية) | المختصر | The Abridged Explanation (Uyghur) | Mukhtasar (UG) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/uyghur/original/uyghur-mokhtasar.json | — | true |
| approved_candidate | ur-bayan-ul-quran-thanwi | ur | الأردية | Urdu | بيان القرآن — التهانوي (الأردية) | بيان القرآن | Bayan-ul-Quran by Ashraf Ali Thanwi (Urdu) | Bayan-ul-Quran (Thanwi) | ashraf-ali-thanwi | أشرف علي التهانوي | Ashraf Ali Thanwi | person | tafsir | detailed | 6236 | languages/urdu/original/tafsir-bayan-ul-quran.json | — | true |
| approved_candidate | ur-fi-zilal | ur | الأردية | Urdu | في ظلال القرآن (الأردية) | في ظلال القرآن | Fi Zilal al-Quran (Urdu) | Fi Zilal (UR) | sayyid-qutb | سيد قطب | Sayyid Qutb | person | tafsir | detailed | 6236 | languages/urdu/original/tafsir-fe-zalul-quran-syed-qatab.json | — | true |
| approved_candidate | ur-ibn-kathir | ur | الأردية | Urdu | تفسير ابن كثير (الأردية) | ابن كثير | Tafsir Ibn Kathir (Urdu) | Ibn Kathir (UR) | ibn-kathir | إسماعيل بن عمر بن كثير | Ibn Kathir | person | tafsir | detailed | 6236 | languages/urdu/original/tafseer-ibn-e-kaseer-urdu.json | — | true |
| approved_candidate | ur-saadi | ur | الأردية | Urdu | تفسير السعدي (الأردية) | السعدي | Tafsir al-Sa'di (Urdu) | al-Sa'di (UR) | as-saadi | عبد الرحمن بن ناصر السعدي | Abd al-Rahman al-Sa'di | person | tafsir | detailed | 6236 | languages/urdu/original/tafsir-as-saadi.json | — | true |
| approved_candidate | ur-tazkirul-quran | ur | الأردية | Urdu | تذكير القرآن (الأردية) | تذكير القرآن | Tazkirul Quran (Urdu) | Tazkirul Quran (UR) | wahiduddin-khan | وحيد الدين خان | Wahiduddin Khan | person | tafsir | detailed | 6236 | languages/urdu/original/tazkiru-quran-ur.json | — | true |
| approved_candidate | uz-mukhtasar | uz | الأوزبكية | Uzbek | المختصر في التفسير (الأوزبكية) | المختصر | The Abridged Explanation (Uzbek) | Mukhtasar (UZ) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/uzbek/original/uzbek-mokhtasar.json | — | true |
| approved_candidate | vi-mukhtasar | vi | الفيتنامية | Vietnamese | المختصر في التفسير (الفيتنامية) | المختصر | The Abridged Explanation (Vietnamese) | Mukhtasar (VI) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/vietnamese/original/vietnamese-mokhtasar.json | — | true |
| approved_candidate | zh-mukhtasar | zh | الصينية | Chinese | المختصر في التفسير (الصينية) | المختصر | The Abridged Explanation (Chinese) | Mukhtasar (ZH) | tafsir-center | مركز تفسير للدراسات القرآنية | Tafsir Center for Quranic Studies | institution | tafsir | brief | 6236 | languages/chinese/original/chinese-mokhtasar.json | — | true |
| excluded_suspect_quality | tr-ibn-kathir | tr | التركية | Turkish | تفسير ابن كثير (التركية) | ابن كثير | Tafsir Ibn Kathir (Turkish) | Ibn Kathir (TR) | ibn-kathir | إسماعيل بن عمر بن كثير | Ibn Kathir | person | tafsir | detailed | 6236 | languages/turkish/original/tr-tafsir-ibne-kathir.json | text too short for Ibn Kathir | false |
| excluded_non_tafsir | ar-muyassar-fi-al-gharib | ar | العربية | Arabic | الميسر في غريب القرآن | غريب القرآن | al-Muyassar fi Gharib al-Quran | Gharib (glossary) | — | — | — | unknown | gharib | unknown | 6236 | languages/arabic/original/al-muyassar-fi-al-gharib.json | gharib glossary (not tafsir) | false |
| excluded_incomplete_coverage | ar-baydawi | ar | العربية | Arabic | أنوار التنزيل — البيضاوي | البيضاوي | Anwar al-Tanzil (al-Baydawi) | al-Baydawi | al-baydawi | عبد الله بن عمر البيضاوي | al-Baydawi | person | tafsir | detailed | 3811 | languages/arabic/original/tafsir-al-baydawi.json | coverage 3811/6236 | false |
| excluded_incomplete_coverage | ar-durr-al-manthur | ar | العربية | Arabic | الدر المنثور — السيوطي | الدر المنثور | al-Durr al-Manthur (al-Suyuti) | al-Durr al-Manthur | al-suyuti | جلال الدين السيوطي | al-Suyuti | person | tafsir | detailed | 6171 | languages/arabic/original/al-durr-al-manthur.json | coverage 6171/6236 | false |
| excluded_incomplete_coverage | ar-ibn-al-qayyim | ar | العربية | Arabic | تفسير ابن القيم | ابن القيم | Tafsir Ibn al-Qayyim | Ibn al-Qayyim | ibn-al-qayyim | ابن قيّم الجوزية | Ibn al-Qayyim | person | tafsir | detailed | 6234 | languages/arabic/original/tafsir-ibn-al-qayyim.json | coverage 6234/6236 | false |
| excluded_incomplete_coverage | ar-ibn-uthaymeen | ar | العربية | Arabic | تفسير ابن عثيمين | ابن عثيمين | Tafsir Ibn Uthaymeen | Ibn Uthaymeen | ibn-uthaymeen | محمد بن صالح العثيمين | Ibn Uthaymeen | person | tafsir | detailed | 3456 | languages/arabic/original/tafsir-ibn-uthaymeen.json | coverage 3456/6236 | false |
| excluded_incomplete_coverage | ar-suddi | ar | العربية | Arabic | تفسير السدي | السدي | Tafsir al-Suddi | al-Suddi | al-suddi | إسماعيل بن عبد الرحمن السدي | al-Suddi | person | tafsir | detailed | 6177 | languages/arabic/original/ar-tafseer-al-saddi.json | coverage 6177/6236 | false |
| excluded_incomplete_coverage | ar-wajiz | ar | العربية | Arabic | الوجيز — الواحدي | الوجيز | al-Wajiz (al-Wahidi) | al-Wajiz | al-wahidi | علي بن أحمد الواحدي | al-Wahidi | person | tafsir | brief | 1645 | languages/arabic/original/al-wajiz-wahidi.json | coverage 1645/6236 | false |
| excluded_incomplete_coverage | id-saadi | id | الإندونيسية | Indonesian | تفسير السعدي (الإندونيسية) | السعدي | Tafsir al-Sa'di (Indonesian) | al-Sa'di (ID) | as-saadi | عبد الرحمن بن ناصر السعدي | Abd al-Rahman al-Sa'di | person | tafsir | detailed | 6235 | languages/indonesian/original/id-tafsir-as-saadi.json | coverage 6235/6236 | false |

---

## 4. Excluded — incomplete coverage (content coverage < 6,236)

These **must not** be copied into the future import package (decision #1). Coverage was re-audited
from the files (following group pointers; counting only ayahs that actually receive tafsir text).

| source_key | display_name_ar | display_name_en | coverage | missing | source_file |
|---|---|---|---:|---:|---|
| id-saadi | تفسير السعدي (الإندونيسية) | Tafsir al-Sa'di (Indonesian) | 6235 | 1 | languages/indonesian/original/id-tafsir-as-saadi.json |
| ar-ibn-al-qayyim | تفسير ابن القيم | Tafsir Ibn al-Qayyim | 6234 | 2 | languages/arabic/original/tafsir-ibn-al-qayyim.json |
| ar-suddi | تفسير السدي | Tafsir al-Suddi | 6177 | 59 | languages/arabic/original/ar-tafseer-al-saddi.json |
| ar-durr-al-manthur | الدر المنثور — السيوطي | al-Durr al-Manthur (al-Suyuti) | 6171 | 65 | languages/arabic/original/al-durr-al-manthur.json |
| ar-baydawi | أنوار التنزيل — البيضاوي | Anwar al-Tanzil (al-Baydawi) | 3811 | 2425 | languages/arabic/original/tafsir-al-baydawi.json |
| ar-ibn-uthaymeen | تفسير ابن عثيمين | Tafsir Ibn Uthaymeen | 3456 | 2780 | languages/arabic/original/tafsir-ibn-uthaymeen.json |
| ar-wajiz | الوجيز — الواحدي | al-Wajiz (al-Wahidi) | 1645 | 4591 | languages/arabic/original/al-wajiz-wahidi.json |

**Reason (all):** the source structurally lists all 6,236 verse keys, but a portion of ayahs have
**empty text** and no group leader to resolve to — i.e. genuine content gaps. Re-evaluate for a later
version if a more complete edition is sourced; **out of scope for v1.**

---

## 5. Non-tafsir resources

- **`ar-muyassar-fi-al-gharib`** — الميسر في غريب القرآن / al-Muyassar fi Gharib al-Quran (`languages/arabic/original/al-muyassar-fi-al-gharib.json`)
  - `resource_kind = gharib`; **`excluded_non_tafsir`** (decision #5). It is a rare-word (gharib) glossary (avg ~183 chars/entry), not a general tafsir. `include_in_future_import = false`.


---

## 6. Needs review (do not guess — Mohamed to decide)

### needs_name_review

_None._

### needs_author_review

_None._

### needs_language_review

_None._

### excluded_suspect_quality

- **`tr-ibn-kathir`** — تفسير ابن كثير (التركية) / Tafsir Ibn Kathir (Turkish) (`languages/turkish/original/tr-tafsir-ibne-kathir.json`)
  - **Issue:** Complete (6236) but text far too short for Ibn Kathir (avg ~132 chars; 1:1=41). Likely stub/mislabeled.
  - **Options:** (a) drop from v1; (b) re-source a complete Ibn Kathir Turkish edition; (c) keep but relabel as a short gloss, not Ibn Kathir. **`include_in_future_import = false`.**


> **Note on filename typos (non-blocking):** `english/en-tafisr-…` ("tafisr"),
> `…ibne-kahtir/ibn-e-kaseer/ibne-kathir` romanization variants — these do **not** make the work
> identity uncertain (all are Ibn Kathir) and are left for normalization at copy time, not flagged here.

---

## 7. Files in this curation set

- `tafsir-curation-candidate-report.md` — this report.
- `tafsir-curation-candidate-manifest.draft.json` — draft review manifest (all 93 sources;
  `isFinalImportManifest: false`; **no files copied**).
- `tafsir-curation-review-questions.md` — grouped decisions required before any copy.

*End of report. Curation/documentation only — no tafsir files copied, no source modified.*
