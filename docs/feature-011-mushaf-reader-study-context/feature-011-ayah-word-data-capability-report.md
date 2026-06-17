# Feature 011 — Mushaf Reader Study Context: Ayah/Word Data Capability Report

## 1. Executive summary

The existing local `quran_dashboard` database is sufficient to begin planning a dashboard-only Mushaf Reader page with page navigation, page/line/word rendering, selected ayah study details, and selected word morphology/segment analysis.

- **Mushaf page data already available:** 604 pages, 9,046 mushaf lines, 83,668 `quran_words` rows in page/line order, canonical surah/ayah metadata, line types (`ayah`, `surah_name`, `basmallah`), ayah-end marker rows, and navigation division metadata for juz/hizb/rub/sajda.
- **Selected ayah details already available:** canonical ayah identity/text, surah name, word counts, page presence, first line derivation on the current page, juz/hizb/rub tags, sajda marker, 84 tafsir sources, 167 translation sources, 4 full-i3rab sources, mutashabihat occurrences, and similar-ayah links.
- **Selected word analysis already available:** quran word occurrence identity, display text (`text_uthmani`), simple/imlaei forms, page/line order, ordered/unique word identity rows, root/lemma/stem/head POS morphology, segment-level morphology, POS labels, and simple i3rab rule assignment for all 128,219 segment rows.
- **Immediate page response:** keep it lean: page number, previous/next page, surahs on page, page ayah ranges, line metadata, words per line using `quran_words.text_uthmani`, ayah end markers, and page-level navigation markers derived from division starts/sajda rows.
- **Lazy-loaded data:** selected ayah tafsir/translation/full-i3rab and selected word morphology/segments should be loaded only after selection. Do not include all sources or heavy HTML/text payloads in the page response.
- **Not available yet:** product-level default source settings, persisted user preferences, precomputed frontend layout widths/justification metadata, segment-to-`text_uthmani` character alignment offsets, semantic POS color policy, and any public-reader specific contracts. These are not blockers for v1 if treated as configuration/UI behavior.

## 2. Page-level data available

| Page item | Source table(s) | Join path | Key columns | Main response or lazy-loaded |
| --- | --- | --- | --- | --- |
| Page number | `quran_mushaf_pages` | Direct lookup | `page_number` | Main page response |
| Previous/next page | `quran_mushaf_pages` | Derive from `page_number` within 1..604 | `page_number` | Main page response |
| Surah/surahs on page | `quran_mushaf_pages`, `quran_surahs`, optionally `quran_ayahs` | page boundary columns to `quran_surahs`; exact page set via `quran_words.ayah_id -> quran_ayahs.surah_number -> quran_surahs` | `first_surah_number`, `last_surah_number`, `surah_number`, `name_arabic` | Main page response |
| Ayah ranges on page | `quran_mushaf_pages`, `quran_ayahs`, `quran_words` | page boundary columns; exact ranges via distinct ayahs in `quran_words` for page | `first_ayah_number`, `last_ayah_number`, `verse_key`, `ayah_number` | Main page response |
| Lines | `quran_mushaf_lines` | `quran_mushaf_pages.page_number -> quran_mushaf_lines.page_number` | `line_number`, `line_type`, `is_centered`, `words_count`, `first_word_id`, `last_word_id` | Main page response |
| Line types | `quran_mushaf_lines` | Same as lines | `line_type` values verified: `ayah`, `surah_name`, `basmallah` | Main page response |
| Words per line | `quran_words` | `quran_words.page_number = page` and `line_number = line`; join `quran_ayahs` for `verse_key` | `id`, `location`, `ayah_id`, `surah_number`, `ayah_number`, `word_number`, `line_word_order`, `text_uthmani`, `is_ayah_marker` | Main page response |
| Mushaf display text | `quran_words` | Same as words per line | `text_uthmani` | Main page response; **must not** be reconstructed from segments |
| Ayah end markers | `quran_words` | Same line word query, filter `is_ayah_marker = true` | `text_uthmani`, `qpc_glyph`, `word_number`, `line_word_order` | Main page response |
| Juz marker | `quran_juzs`, `quran_ayahs`, `quran_words` | `quran_juzs.first_ayah_id -> quran_ayahs.id -> quran_words.ayah_id`; include when first ayah appears on page | `juz_number`, `first_ayah_id`, `first_verse_key` | Main page response as marker metadata |
| Hizb marker | `quran_hizbs`, `quran_ayahs`, `quran_words` | `quran_hizbs.first_ayah_id -> quran_ayahs.id -> quran_words.ayah_id` | `hizb_number`, `juz_number`, `first_ayah_id`, `first_verse_key` | Main page response as marker metadata |
| Rub marker | `quran_rubs`, `quran_ayahs`, `quran_words` | `quran_rubs.first_ayah_id -> quran_ayahs.id -> quran_words.ayah_id` | `rub_number`, `hizb_number`, `first_ayah_id`, `first_verse_key` | Main page response as marker metadata |
| Sajda marker | `quran_sajdas`, `quran_ayahs`, `quran_words` | `quran_sajdas.ayah_id -> quran_ayahs.id -> quran_words.ayah_id` | `sajdah_number`, `verse_key`, `sajdah_type` | Main page response as marker metadata |
| Surah start marker/header | `quran_mushaf_lines`, `quran_surahs` | `quran_mushaf_lines.line_type = 'surah_name'` and `surah_number -> quran_surahs` | `line_number`, `surah_number`, `name_arabic` | Main page response |

Marker placement is derivable: for sajda/rub/hizb/juz, resolve the related ayah then place the marker beside the first line where that ayah appears on the current page (`MIN(quran_words.line_number)` for that `ayah_id` and `page_number`). If an ayah spans multiple lines, this satisfies the agreed first-line marker rule.

## 3. Ayah-level data available

### Core ayah data

Available from `quran_ayahs`, `quran_surahs`, `quran_words`, `quran_juzs`, `quran_hizbs`, `quran_rubs`, and `quran_sajdas`:

- `ayahId`: `quran_ayahs.id` for internal joins only.
- `verseKey`: `quran_ayahs.verse_key` such as `2:25`.
- `surahNumber`, `surahNameArabic`: `quran_ayahs.surah_number -> quran_surahs.name_arabic`.
- `ayahNumber`: `quran_ayahs.ayah_number`.
- `ayahText`: `quran_ayahs.text_uthmani`.
- `wordsCount`: `words_count_real` or `words_count_source`.
- `pagePresence`: `page_from`, `page_to`, plus exact line presence from `quran_words`.
- `firstLineOnCurrentPage`: derived from `MIN(quran_words.line_number)` for the selected `verse_key` and page.
- `juzNumber`, `hizbNumber`, `rubNumber`: denormalized on `quran_ayahs` and backed by division tables.
- `sajda`: `quran_sajdas` row when present.

**Feature 011 v1 classification:** in scope.

### Tafsir

- Available sources: **84** `quran_tafsir_sources` across **33** languages; all inspected sources have `content_coverage_count = 6236`.
- Arabic tafsir sources: 35, including brief and detailed sources. Present source keys include `ar-muyassar`, `ar-mukhtasar`, `ar-jalalayn`, `ar-saadi`, `ar-tabari`, `ar-qurtubi`, and others.
- Entry path: `quran_ayahs.id -> quran_tafsir_ayah_entries.ayah_id -> quran_tafsir_entries.id`, with source metadata through `quran_tafsir_sources.id`.
- Grouped/ranged behavior exists: `quran_tafsir_entries.source_shape` includes `flat` and `grouped_leader`; mappings include `flat`, `leader`, and `member_pointer`.
- Recommended selected-entry fields: `sourceKey`, `sourceDisplayNameAr`, `sourceShortNameAr`, `languageCode`, `direction`, `tafsirKind`, `sourceValueKind`, `sourceLeaderVerseKey`, `isGroupLeader`, `coveredAyahCount`, `coveredAyahKeys`, `tafsirText`.

**Feature 011 v1 classification:** selected/default tafsir is in scope for selected ayah lazy-load; all-source browsing is available but better deferred.

### Translations

- Available sources: **167** `quran_translation_sources` across **83** languages; all inspected sources have `content_coverage_count = 6236`.
- No Arabic translation source was found in the inspected `quran_translation_sources` (`language_code = 'ar'` returned no rows). English has 19 sources, including `en-sahih-international`, `en-haleem`, `en-pickthall`, and others.
- Entry path: `quran_ayahs.id -> quran_translation_ayah_entries.ayah_id -> quran_translation_sources.id`.
- Recommended selected-entry fields: `sourceKey`, `displayNameAr`, `displayNameEn`, `languageCode`, `languageNameAr`, `languageNameEn`, `nativeName`, `direction`, `translationType`, `containsInlineFootnotes`, `containsHtmlMarkup`, `text`.

**Feature 011 v1 classification:** selected/default translation is available but should be lazy-loaded; whether it is in v1 depends on dashboard study-panel scope. Source browsing and multi-translation comparison should be deferred.

### Full i3rab

- Available sources: **4** complete HTML sources in `quran_full_i3rab_sources`: `daas`, `darwish`, `jadwal`, `muyassar`; each has `content_coverage_count = 6236` and `markup_format = html`.
- Entry path: `quran_ayahs.id -> quran_full_i3rab_ayah_entries.ayah_id -> quran_full_i3rab_entries.id`, with source metadata through `quran_full_i3rab_sources.id`.
- HTML text is available in `quran_full_i3rab_entries.i3rab_html`.
- Grouped/ranged behavior exists: `source_shape` includes `flat` and `grouped_leader`; mappings include `flat`, `leader`, and `member_pointer`.
- Covered verse keys are available in `covered_ayah_keys`.
- Recommended selected-entry fields: `sourceKey`, `displayNameAr`, `shortNameAr`, `displayNameEn`, `markupFormat`, `sourceValueKind`, `sourceLeaderVerseKey`, `isGroupLeader`, `coveredAyahCount`, `coveredAyahKeys`, `i3rabHtml`.

**Feature 011 v1 classification:** selected/default full-i3rab entry is available but heavy; lazy-load only when the ayah i3rab tab/panel is active.

### Other ayah-related data

- Mutashabihat occurrences: `quran_mutashabihat_occurrences` has 3,557 rows; join via `ayah_id` to groups and optional word spans (`word_from`, `word_to`). **Available but better deferred** unless the v1 study context explicitly includes mutashabihat.
- Similar ayah links: `quran_similar_ayah_links` has 3,552 directed links; join `source_ayah_id` and `target_ayah_id` to `quran_ayahs`. **Available but better deferred**.
- Navigation metadata: juz/hizb/rub/sajda and page/line presence are available now. **In scope for v1**.

## 4. Word-level data available

For a selected readable word, use natural location keys such as `2:25:3`. Ayah marker rows (`is_ayah_marker = true`) are display markers, not selectable analysis words; morphology/display identity tables cover 77,432 readable words.

| Item | Source table(s) | Join path | Key columns | Recommended response field |
| --- | --- | --- | --- | --- |
| Quran word id | `quran_words` | Direct by `location` | `id` | `quranWordId` |
| Location / word location | `quran_words` | Direct | `location` | `wordLocation` |
| Verse key | `quran_words`, `quran_ayahs` | `quran_words.ayah_id -> quran_ayahs.id` | `verse_key` | `verseKey` |
| Surah number | `quran_words` | Direct | `surah_number` | `surahNumber` |
| Ayah number | `quran_words` | Direct | `ayah_number` | `ayahNumber` |
| Word number | `quran_words` | Direct | `word_number` | `wordNumber` |
| Page number | `quran_words` | Direct | `page_number` | `pageNumber` |
| Line number | `quran_words` | Direct | `line_number` | `lineNumber` |
| Line word order | `quran_words` | Direct | `line_word_order` | `lineWordOrder` |
| Uthmani display text | `quran_words` | Direct | `text_uthmani` | `textUthmani` |
| Uthmani simple | `quran_words` | Direct | `text_uthmani_simple` | `textUthmaniSimple` |
| Imlaei simple | `quran_words` | Direct | `text_imlaei_simple` | `textImlaeiSimple` |
| QPC glyph | `quran_words` | Direct | `qpc_glyph` | `qpcGlyph` |
| Marker exclusion | `quran_words` | Filter out `is_ayah_marker = true` for analysis | `is_ayah_marker` | `isAyahMarker` / validation guard |
| Ordered tashkeel row | `quran_words_ordered_tashkeel` | `quran_word_id = quran_words.id` | `word_order_in_mushaf`, `word_order_in_ayah`, counts | `orderedTashkeel` |
| Ordered simple row | `quran_words_ordered_simple` | `quran_word_id = quran_words.id` | `word_order_in_mushaf`, `word_key_imlaei_simple`, counts | `orderedSimple` |
| Unique tashkeel identity | `quran_words_unique_tashkeel` | `quran_words.unique_tashkeel_word_id -> id` | `id`, text forms, occurrence counts | `uniqueTashkeel` |
| Unique simple identity | `quran_words_unique_simple` | `quran_words.unique_simple_word_id -> id` | `id`, `word_key_imlaei_simple`, counts | `uniqueSimple` |
| Occurrence/ayah/surah counts | Ordered and unique display tables | Same display joins | `occurrences_count`, `ayahs_count`, `surahs_count` | `occurrencesCount`, `ayahsCount`, `surahsCount` |
| Root | `quran_word_morphology`, `quran_roots` | `quran_word_morphology.quran_word_id -> quran_words.id`; `root_id -> quran_roots.id` | `root_text`, `root_buckwalter` | `root` |
| Lemma | `quran_word_morphology`, `quran_lemmas` | `lemma_id -> quran_lemmas.id` | `lemma_text`, `lemma_buckwalter` | `lemma` |
| Stem | `quran_word_morphology`, `quran_stems` | `stem_id -> quran_stems.id` | `stem_text` | `stem` |
| Head POS / word type | `quran_word_morphology`, `quran_pos_tags` | `head_pos -> quran_pos_tags.code` | `head_pos`, labels | `headPos` |
| POS labels | `quran_pos_tags` | Same head or segment POS join | `arabic_label`, `english_label`, `category` | `posLabelAr`, `posLabelEn` |
| Case feature | `quran_word_morphology` | Direct | `case_feature` | `caseFeature` |
| Verb tense | `quran_word_morphology` | Direct | `verb_tense` | `verbTense` |
| Voice | `quran_word_morphology` | Direct | `verb_voice` | `verbVoice` |
| Morphology source fields | `quran_word_morphology` | Direct | `segment_count`, `head_features_json`, `is_verb` | `segmentCount`, `headFeatures`, `isVerb` |
| Segment number | `quran_word_morphology_segments` | `quran_word_id = quran_words.id` | `segment_number` | `segmentNumber` |
| Segment kind | `quran_word_morphology_segments` | Same | `kind` | `segmentKind` |
| Segment form | `quran_word_morphology_segments` | Same | `form_arabic_normalized` | `segmentDisplayText` |
| POS | `quran_word_morphology_segments`, `quran_pos_tags` | `pos -> quran_pos_tags.code` | `pos` | `segmentPos` |
| POS label | `quran_pos_tags` | Same | `arabic_label`, `english_label` | `segmentPosLabel` |
| Features | `quran_word_morphology_segments` | Direct | `features_raw`, `features_json` | `segmentFeatures` |
| Segment root/lemma/stem | `quran_word_morphology_segments` | Direct for root/lemma; stem represented by segment kind/form and morphology stem | `root_buckwalter`, `lemma_buckwalter`, `kind`, `form_arabic_normalized` | `segmentRootBuckwalter`, `segmentLemmaBuckwalter` |
| Simple i3rab Arabic | `quran_word_morphology_segments` | Direct | `i3rab_arabic` | `segmentI3rabArabic` |
| I3rab rule id | `quran_word_morphology_segments` | Direct | `i3rab_rule_id` | `i3rabRuleId` |
| Rule signature/family/status | `quran_i3rab_rules` | `i3rab_rule_id -> quran_i3rab_rules.id` | `signature_key`, `rule_family`, `default_status`; plus `i3rab_status` on segment | `i3rabRuleSignature`, `i3rabRuleFamily`, `i3rabStatus` |
| Arabic render tier/source | `quran_word_morphology_segments` | Direct | `arabic_render_tier`, `arabic_render_source` | `arabicRenderTier`, `arabicRenderSource` |

## 5. Segment-colored rendering feasibility

Selected-word segment rendering is feasible for the analysis side panel:

- Query `quran_word_morphology_segments` by `quran_word_id`, ordered by `segment_number`.
- Render each segment as an inline span with no inserted spaces between spans.
- Use `form_arabic_normalized` when it is present and visually safe.
- Fallback behavior:
  - If `form_arabic_normalized` is empty/null, do not invent segment text.
  - Return the segment row with an empty/fallback flag, and let the panel either show a small placeholder glyph/marker or fall back to the full `quran_words.text_uthmani` for the whole word.
  - Keep the raw segment metadata visible in the row so the data issue is not hidden.
- Inspection found 208 segment rows where `form_arabic_normalized` is empty or null out of 128,219 total segment rows, so fallback handling is required.
- This glued colored segment rendering is for the **word analysis panel only**.
- The Mushaf page text itself must remain `quran_words.text_uthmani`; do not render Mushaf lines from segment forms.
- Segment colors in v1 should be stable visual-linking slots only, not semantic POS colors.

Recommended segment response shape:

```json
{
  "renderedWordSegments": [
    {
      "segmentLocation": "2:25:3:1",
      "segmentNumber": 1,
      "segmentDisplayText": "...",
      "segmentColorSlot": 1,
      "segmentI3rabArabic": "فعل ماض",
      "segmentPos": "V",
      "segmentPosLabel": { "ar": "فعل", "en": "Verb" },
      "segmentFeatures": {
        "raw": "STEM|POS:V|PERF|...",
        "json": []
      },
      "displayTextStatus": "available"
    }
  ]
}
```

## 6. Recommended API response shapes

These are draft DTO shapes only. In the existing API boundary, place these shapes under the standard `ApiResponse.data` envelope.

### A. Mushaf page response

Example endpoint: `GET /api/mushaf/pages/{pageNumber}`

```json
{
  "pageNumber": 5,
  "previousPageNumber": 4,
  "nextPageNumber": 6,
  "surahs": [
    { "surahNumber": 2, "nameArabic": "البقرة", "firstAyahOnPage": 25, "lastAyahOnPage": 29 }
  ],
  "ayahRange": { "firstVerseKey": "2:25", "lastVerseKey": "2:29" },
  "navigation": {
    "juzNumbers": [1],
    "hizbNumbers": [1],
    "rubNumbers": [1, 2]
  },
  "lines": [
    {
      "lineNumber": 1,
      "lineType": "ayah",
      "isCentered": false,
      "words": [
        {
          "wordLocation": "2:25:1",
          "verseKey": "2:25",
          "wordNumber": 1,
          "lineWordOrder": 1,
          "textUthmani": "...",
          "isAyahMarker": false
        }
      ]
    }
  ],
  "markers": [
    {
      "markerType": "rub",
      "markerNumber": 2,
      "verseKey": "2:26",
      "lineNumber": 4,
      "wordLocation": "2:26:1"
    }
  ]
}
```

Keep this response reasonably sized: do not include tafsir, translations, full i3rab HTML, or word morphology.

### B. Selected ayah study response

Example endpoint: `GET /api/mushaf/ayahs/{verseKey}/study?tafsirSource=...&translationSource=...&fullI3rabSource=...`

```json
{
  "ayah": {
    "verseKey": "2:25",
    "surahNumber": 2,
    "surahNameArabic": "البقرة",
    "ayahNumber": 25,
    "textUthmani": "...",
    "wordsCount": 34,
    "pageFrom": 5,
    "pageTo": 5,
    "juzNumber": 1,
    "hizbNumber": 1,
    "rubNumber": 1,
    "sajda": null
  },
  "selectedSources": {
    "tafsirSource": "ar-muyassar",
    "translationSource": "en-sahih-international",
    "fullI3rabSource": "muyassar"
  },
  "tafsir": {
    "sourceKey": "ar-muyassar",
    "displayNameAr": "التفسير الميسر",
    "tafsirKind": "brief",
    "sourceValueKind": "flat",
    "coveredAyahKeys": ["2:25"],
    "text": "..."
  },
  "translation": {
    "sourceKey": "en-sahih-international",
    "displayNameAr": "صحيح إنترناشونال",
    "displayNameEn": "Saheeh International",
    "languageCode": "en",
    "direction": "ltr",
    "text": "..."
  },
  "fullI3rab": {
    "sourceKey": "muyassar",
    "displayNameAr": "الإعراب الميسّر",
    "markupFormat": "html",
    "sourceValueKind": "flat",
    "coveredAyahKeys": ["2:25"],
    "html": "..."
  }
}
```

### C. Selected word analysis response

Example endpoint: `GET /api/mushaf/words/{wordLocation}/analysis`

```json
{
  "word": {
    "quranWordId": 360,
    "wordLocation": "2:25:3",
    "verseKey": "2:25",
    "surahNumber": 2,
    "ayahNumber": 25,
    "wordNumber": 3,
    "pageNumber": 5,
    "lineNumber": 1,
    "lineWordOrder": 3,
    "textUthmani": "...",
    "textUthmaniSimple": "...",
    "textImlaeiSimple": "...",
    "qpcGlyph": "..."
  },
  "identity": {
    "orderedTashkeel": { "wordOrderInMushaf": 329, "occurrencesCount": 206, "ayahsCount": 201, "surahsCount": 54 },
    "orderedSimple": { "wordOrderInMushaf": 329, "occurrencesCount": 263, "ayahsCount": 254, "surahsCount": 59 },
    "uniqueTashkeel": { "id": 90, "occurrencesCount": 206 },
    "uniqueSimple": { "id": 82, "wordKeyImlaeiSimple": "...", "occurrencesCount": 263 }
  },
  "morphology": {
    "headPos": "V",
    "headPosLabel": { "ar": "فعل", "en": "Verb" },
    "root": { "text": "...", "buckwalter": "Amn" },
    "lemma": { "text": "...", "buckwalter": "'aAmana" },
    "stem": { "text": "..." },
    "isVerb": true,
    "verbTense": "past",
    "verbVoice": "active",
    "caseFeature": null,
    "headFeatures": []
  },
  "renderedWordSegments": [
    {
      "segmentLocation": "2:25:3:1",
      "segmentNumber": 1,
      "segmentKind": "STEM",
      "segmentDisplayText": "...",
      "segmentColorSlot": 1,
      "segmentPos": "V",
      "segmentPosLabel": { "ar": "فعل", "en": "Verb" },
      "segmentFeatures": { "raw": "...", "json": [] },
      "segmentI3rabArabic": "فعل ماض",
      "i3rabRuleId": 18,
      "i3rabRuleSignature": "STEM:V:PERF:ACT:3MP",
      "i3rabRuleFamily": "V.PERF.ACT",
      "i3rabStatus": "approved"
    }
  ]
}
```

## 7. URL state recommendations

Use natural Quran keys in the shareable URL where possible; avoid database numeric ids.

Recommended query parameters:

- `page=5` — current Mushaf page.
- `ayah=2:25` — selected ayah.
- `word=2:25:3` — selected word occurrence.
- `segment=2:25:3:1` — selected segment when applicable.
- `panel=ayah|word|sources|none` — active side panel.
- `ayahTab=tafsir|translation|full-i3rab|links` — active selected-ayah tab.
- `wordTab=morphology|segments|i3rab|identity` — active selected-word tab.
- `tafsirSource=ar-muyassar` — selected tafsir source key.
- `translationSource=en-sahih-international` — selected translation source key.
- `fullI3rabSource=muyassar` — selected full-i3rab source key.

Example: `/dashboard/mushaf?page=5&ayah=2:25&word=2:25:3&segment=2:25:3:1&panel=word&wordTab=segments&tafsirSource=ar-muyassar&translationSource=en-sahih-international&fullI3rabSource=muyassar`

## 8. Default source policy

No explicit `is_default` or preference column was found in the inspected source catalogues. Defaults should therefore be configuration-driven, not hardcoded in query logic.

Recommended policy:

1. Use configured dashboard defaults when present:
   - `MushafReader:DefaultTafsirSourceKey`
   - `MushafReader:DefaultTranslationSourceKey`
   - `MushafReader:DefaultFullI3rabSourceKey`
2. Validate configured source keys against source tables at startup or first use.
3. If no configured key exists:
   - Tafsir: prefer a full-coverage Arabic source matching a configured tafsir kind preference (`brief` for compact side panel, `detailed` for study-heavy mode). Present examples include `ar-muyassar` and `ar-mukhtasar`, but the product should choose the actual default.
   - Translation: prefer a full-coverage source for the configured UI/study language. No Arabic translation source was found; English candidates include `en-sahih-international`, `en-haleem`, and others. The product should choose the default.
   - Full i3rab: choose one of the four complete configured sources (`daas`, `darwish`, `jadwal`, `muyassar`). The product should choose whether compact/easy (`muyassar`) or comprehensive sources are preferred.
4. Return the selected source metadata with each lazy-loaded ayah study response so the UI can show exactly what was used.

## 9. Sample SQL / query strategy

All examples are read-only representative SQL and omit credentials/connection strings.

### One Mushaf page with lines and words

```sql
SELECT
  p.page_number,
  p.first_surah_number,
  p.first_ayah_number,
  p.last_surah_number,
  p.last_ayah_number,
  l.line_number,
  l.line_type,
  l.is_centered,
  l.words_count,
  w.location AS word_location,
  a.verse_key,
  w.word_number,
  w.line_word_order,
  w.text_uthmani,
  w.qpc_glyph,
  w.is_ayah_marker
FROM quran_mushaf_pages p
JOIN quran_mushaf_lines l ON l.page_number = p.page_number
LEFT JOIN quran_words w
  ON w.page_number = l.page_number
 AND w.line_number = l.line_number
LEFT JOIN quran_ayahs a ON a.id = w.ayah_id
WHERE p.page_number = @pageNumber
ORDER BY l.line_number, w.line_word_order;
```

Navigation markers for the page can be loaded with a union over `quran_juzs`, `quran_hizbs`, `quran_rubs`, and `quran_sajdas`, joined to `quran_words` to derive first page line.

### One selected ayah with selected tafsir/translation/full-i3rab

```sql
WITH selected_ayah AS (
  SELECT a.*
  FROM quran_ayahs a
  WHERE a.verse_key = @verseKey
)
SELECT a.id, a.verse_key, a.surah_number, s.name_arabic,
       a.ayah_number, a.text_uthmani, a.words_count_real,
       a.page_from, a.page_to, a.juz_number, a.hizb_number, a.rub_number,
       sj.sajdah_number, sj.sajdah_type
FROM selected_ayah a
JOIN quran_surahs s ON s.surah_number = a.surah_number
LEFT JOIN quran_sajdas sj ON sj.ayah_id = a.id;

SELECT ts.source_key, ts.display_name_ar, ts.short_name_ar, ts.language_code,
       ts.direction, ts.tafsir_kind, tae.source_value_kind,
       tae.source_leader_verse_key, tae.is_group_leader,
       te.covered_ayah_count, te.covered_ayah_keys, te.tafsir_text
FROM selected_ayah a
JOIN quran_tafsir_ayah_entries tae ON tae.ayah_id = a.id
JOIN quran_tafsir_sources ts ON ts.id = tae.source_id
JOIN quran_tafsir_entries te ON te.id = tae.tafsir_entry_id
WHERE ts.source_key = @tafsirSourceKey;

SELECT trs.source_key, trs.display_name_ar, trs.display_name_en,
       trs.language_code, trs.direction, trs.translation_type, tr.text
FROM selected_ayah a
JOIN quran_translation_ayah_entries tr ON tr.ayah_id = a.id
JOIN quran_translation_sources trs ON trs.id = tr.source_id
WHERE trs.source_key = @translationSourceKey;

SELECT fs.source_key, fs.display_name_ar, fs.markup_format,
       fie.source_value_kind, fie.source_leader_verse_key,
       fie.is_group_leader, fe.covered_ayah_count,
       fe.covered_ayah_keys, fe.i3rab_html
FROM selected_ayah a
JOIN quran_full_i3rab_ayah_entries fie ON fie.ayah_id = a.id
JOIN quran_full_i3rab_sources fs ON fs.id = fie.source_id
JOIN quran_full_i3rab_entries fe ON fe.id = fie.entry_id
WHERE fs.source_key = @fullI3rabSourceKey;
```

### One selected word with morphology, POS, root, lemma, stem, segments, and simple i3rab

```sql
SELECT w.id, w.location, a.verse_key, w.surah_number, w.ayah_number,
       w.word_number, w.page_number, w.line_number, w.line_word_order,
       w.text_uthmani, w.text_uthmani_simple, w.text_imlaei_simple,
       w.qpc_glyph, w.is_ayah_marker,
       m.head_pos, hpt.arabic_label AS head_pos_ar,
       hpt.english_label AS head_pos_en,
       m.segment_count, r.root_text, r.root_buckwalter,
       le.lemma_text, le.lemma_buckwalter, st.stem_text,
       m.is_verb, m.verb_tense, m.verb_voice,
       m.case_feature, m.head_features_json
FROM quran_words w
JOIN quran_ayahs a ON a.id = w.ayah_id
LEFT JOIN quran_word_morphology m ON m.quran_word_id = w.id
LEFT JOIN quran_pos_tags hpt ON hpt.code = m.head_pos
LEFT JOIN quran_roots r ON r.id = m.root_id
LEFT JOIN quran_lemmas le ON le.id = m.lemma_id
LEFT JOIN quran_stems st ON st.id = m.stem_id
WHERE w.location = @wordLocation
  AND w.is_ayah_marker = false;

SELECT seg.segment_location, seg.segment_number, seg.kind,
       seg.form_arabic_normalized, seg.pos,
       spt.arabic_label AS pos_ar, spt.english_label AS pos_en,
       seg.features_raw, seg.features_json,
       seg.root_buckwalter, seg.lemma_buckwalter,
       seg.arabic_render_tier, seg.arabic_render_source,
       seg.i3rab_arabic, seg.i3rab_rule_id,
       rule.signature_key, rule.rule_family,
       rule.default_status, seg.i3rab_status
FROM quran_words w
JOIN quran_word_morphology_segments seg ON seg.quran_word_id = w.id
LEFT JOIN quran_pos_tags spt ON spt.code = seg.pos
LEFT JOIN quran_i3rab_rules rule ON rule.id = seg.i3rab_rule_id
WHERE w.location = @wordLocation
ORDER BY seg.segment_number;
```

## 10. Risks and planning notes

- **Performance risks:** page queries are small, but ayah study joins against tafsir/translation/full-i3rab tables must use source-key filters and indexed ayah/source joins. Avoid loading all sources for an ayah unless explicitly requested.
- **Payload size risks:** tafsir and full-i3rab HTML can be large, especially grouped entries covering multiple ayahs. Keep the page response lean and lazy-load study panels.
- **Arabic text rendering risks:** Mushaf line alignment depends on font, text shaping, CSS, and available width. Database provides line/order data, not pixel-perfect browser justification. The frontend must test Arabic shaping carefully.
- **Segment rendering risks:** segment forms are analysis data, not authoritative Mushaf text. Some segment display forms are empty/null; fallback behavior is required.
- **Grouped tafsir/full-i3rab behavior:** a selected ayah may point to a grouped leader entry. Responses must expose `sourceValueKind`, `sourceLeaderVerseKey`, `isGroupLeader`, and `coveredAyahKeys` so the UI can explain why a displayed text covers multiple ayahs.
- **Mobile layout implications:** although this is a dashboard page using available width, side panels with stable dimensions and internal scroll will need responsive collapse/drawer behavior on narrow screens.
- **Backend responsibilities:** validate page/verse/word/source keys, query indexed tables, return stable DTOs, enforce marker placement metadata, prevent marker rows from word analysis, and sanitize/control HTML handling policy for full-i3rab/tafsir content.
- **Frontend responsibilities:** Arabic-first RTL layout, line visual alignment, stable scrollable cards, URL state synchronization, segment color slot rendering, and preserving Mushaf text from `text_uthmani`.

## 11. Final recommendation

The existing database is sufficient to start Feature 011 planning.

Recommended Feature 011 v1 scope:

- Dashboard Mushaf page route using full available dashboard width.
- Page navigation and header context: page, surah(s), ayah range, juz/hizb/rub summary.
- Main center Mushaf page display from `quran_words.text_uthmani`, grouped by `quran_mushaf_lines`.
- Line types for `surah_name`, `basmallah`, and `ayah` lines.
- Ayah end markers from `quran_words.is_ayah_marker` rows.
- Sajda/rub/hizb/juz markers beside the first line of the related ayah on the page.
- URL state for page, selected ayah, selected word, selected segment, active panel/tabs, and selected source keys.
- Lazy-loaded selected ayah study data for one selected tafsir, translation, and/or full-i3rab source.
- Lazy-loaded selected word analysis with morphology, ordered/unique identities, segments, POS labels, and simple i3rab.

Out of scope for v1:

- Public visitor-reader behavior.
- Loading all tafsirs/translations/full-i3rab entries into the page response.
- Semantic POS color system.
- Editing/importing Quranic data.
- Database cleanup or nullable/all-null analysis.

Deferred follow-up features:

- User/admin source preference management.
- Advanced source browser and multi-source comparison.
- Mutashabihat/similar-ayah study panel integration.
- Precomputed browser layout aids if visual line alignment is insufficient with CSS/font tuning alone.
- Segment-to-Uthmani character alignment research if exact segment overlays are later required.

## Verification

- SQL/catalog inspection approach used:
  - Read the existing database tables/relationships report as baseline.
  - Verified current local database connectivity with `PGOPTIONS='-c default_transaction_read_only=on'`.
  - Queried `information_schema.columns` for relevant table columns.
  - Queried direct counts/source metadata for tafsir, translation, full-i3rab, navigation metadata, line types, marker rows, and sample page/ayah/word join paths.
  - Local connection configuration was resolved from safe local configuration/user-secrets handling for execution only; no passwords or full connection strings are included here.
- Confirmation: only read-only SQL/catalog/data queries were run.
- Confirmation: no source code changes, migrations, importers, or database writes were made.
- Created file: `docs/feature-011-mushaf-reader-study-context/feature-011-ayah-word-data-capability-report.md`.
