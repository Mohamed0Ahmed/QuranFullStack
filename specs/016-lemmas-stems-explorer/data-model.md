# Data Model: Quran Lemmas & Stems Explorer

Feature 016 introduces no persisted entity and no state transition that writes to storage. This
document defines read models derived from existing Quran data and frontend URL/UI state.

## Existing Source Entities

### QuranLemma

| Field | Type | Rules |
|---|---|---|
| `id` | positive integer | Stable canonical selection/deep-link identity; never shown as user content. |
| `lemma_text` | non-empty Arabic string | Primary display/search value. |
| `lemma_buckwalter` | nullable string | Supporting display/metadata only; not canonical identity. |
| `root_id` | nullable positive integer | Owned root relationship used in lemma table summaries. |
| `first_word_order_in_mushaf` | integer | Stable Mushaf-order list sort input. |

### QuranStem

| Field | Type | Rules |
|---|---|---|
| `id` | positive integer | Stable canonical selection/deep-link identity. |
| `stem_text` | non-empty Arabic string | Primary display/search value. |
| `first_word_order_in_mushaf` | integer | Stable Mushaf-order list sort input. |

### WordMorphology

| Field | Type | Rules |
|---|---|---|
| `quran_word_id` | positive integer | Links to one readable Quran word occurrence. |
| `lemma_id` | nullable positive integer | Inclusion key for lemma aggregates/details. |
| `stem_id` | positive integer in current verified data | Inclusion key for stem aggregates/details. |
| `root_id` | nullable positive integer | Co-occurring root; used for dominant stem-root summary only. |
| `head_pos` | controlled code | Source for dominant type and complete type distribution. |

### QuranWord

Provides occurrence identity and ordering:
`id`, `ayah_id`, `surah_number`, `ayah_number`, `word_number`, `page_number`,
`unique_simple_word_id`, `unique_tashkeel_word_id`, and stored display text fields.

### Supporting Entities

- **QuranRoot**: stable root identity and display text.
- **QuranPosTag**: controlled POS code plus Arabic/English labels.
- **Unique Simple/Tashkeel Word**: stable destinations and display forms.
- **QuranAyah**: verse identity/key and metadata.
- **QuranSurah**: surah number and Arabic name; universe size is 114.

## Derived Read Models

### TypeSummary

| Field | Type | Validation |
|---|---|---|
| `code` | non-empty string | Existing controlled `head_pos`; never invented. |
| `arabicLabel` | non-empty string | Existing controlled Arabic label. |
| `englishLabel` | non-empty/nullable per existing contract | Existing controlled label only. |
| `occurrencesCount` | non-negative integer | Number of matching morphology rows with this type. |
| `firstSurahNumber` | 1..114 | Earliest matching occurrence. |
| `firstAyahNumber` | positive integer | Earliest matching occurrence. |
| `firstWordNumber` | positive integer | Earliest matching occurrence. |

Ordering: `occurrencesCount DESC`, then
`firstSurahNumber`, `firstAyahNumber`, `firstWordNumber` ascending. First item is dominant.

### LemmaListItem / LemmaSummary

| Field | Type | Rule |
|---|---|---|
| `id` | positive integer | Lemma identity. |
| `lemmaText` | non-empty string | Stored display text. |
| `lemmaBuckwalter` | nullable string | Supporting metadata. |
| `rootId`, `rootText`, `rootBuckwalter` | nullable | From owned `QuranLemma.root_id`; all null when absent. |
| `dominantType` | TypeSummary | First ordered type distribution item. |
| `otherTypesCount` | non-negative integer | `max(typeDistribution.Count - 1, 0)`. |
| `occurrencesCount` | positive integer | Matching morphology rows for lemma. |
| `ayahsCount` | positive integer | Distinct matching ayahs. |
| `surahsCount` | 1..114 | Distinct matching surahs. |
| `simpleWordsCount` | positive integer | Distinct simple word IDs. |
| `tashkeelWordsCount` | positive integer | Distinct tashkeel word IDs. |
| `stemsCount` | non-negative integer | Distinct non-null stem IDs. |
| `firstVerseKey` | non-empty verse key | First matching occurrence context. |
| `typeDistribution` | ordered list | Summary-only detail field; counts total occurrences. |

Invariant: `stemsCount == relatedStems.items.Count`.

### DominantRelatedItem

Used for stem list/summary columns:

| Field | Type | Rule |
|---|---|---|
| `id` | positive integer | Related lemma/root identity. |
| `text` | non-empty string | Existing display text. |
| `buckwalter` | nullable string | Root/lemma supporting metadata where available. |
| `occurrencesCount` | positive integer | Co-occurrence count within selected stem. |
| first occurrence coordinates | positive integers | Tie-break metadata. |

Ordering: count descending, then earliest Mushaf occurrence ascending.

### StemListItem / StemSummary

| Field | Type | Rule |
|---|---|---|
| `id` | positive integer | Stem identity. |
| `stemText` | non-empty string | Stored display text. |
| dominant lemma fields | nullable | Highest co-occurring lemma; null when no lemma. |
| dominant root fields | nullable | Highest co-occurring root; null when no root. |
| `dominantType` | TypeSummary | First ordered type distribution item. |
| `otherTypesCount` | non-negative integer | Additional types count. |
| `occurrencesCount` | positive integer | Matching morphology rows for stem. |
| `ayahsCount` | positive integer | Distinct matching ayahs. |
| `surahsCount` | 1..114 | Distinct matching surahs. |
| `simpleWordsCount` | positive integer | Distinct simple word IDs. |
| `tashkeelWordsCount` | positive integer | Distinct tashkeel word IDs. |
| `firstVerseKey` | non-empty verse key | First matching occurrence context. |
| `typeDistribution` | ordered list | Summary-only detail field. |

Invariant: when a dominant relation is null, its related ID/text fields are all null and the UI renders
a non-interactive empty value.

### MorphologyWordItem

| Field | Type | Rule |
|---|---|---|
| `uniqueWordId` | positive integer | Simple or tashkeel destination identity. |
| `kind` | `simple` or `tashkeel` | Must match requested sub-view. |
| `displayTextUthmani` | non-empty stored string | Never synthesized. |
| `occurrencesCount` | positive integer | Count scoped to selected lemma/stem. |
| `firstVerseKey` | non-empty verse key | First matching context. |

### MorphologyAyahMatch

| Field | Type | Rule |
|---|---|---|
| ayah identity/metadata | positive IDs/numbers | Existing ayah/surah/page data. |
| `verseKey` | valid verse key | Existing Mushaf destination identity. |
| `matchedQuranWordIds` | non-empty unique integer list | Exact matched readable words for selection in this ayah. |
| `words` | ordered readable word list | Existing highlight DTO shape; no fabricated text. |

### MentionedSurah / MissingSurah

- Mentioned: `surahNumber`, `nameArabic`, `occurrencesInSurah`.
- Missing: `surahNumber`, `nameArabic`.
- Mentioned and missing sets are disjoint.
- Their unique union equals all 114 surahs.

### RelatedStem / RelatedLemma

- Related stem: `stemId`, `stemText`, `occurrencesCount` within selected lemma.
- Related lemma: `lemmaId`, `lemmaText`, optional `lemmaBuckwalter`,
  `occurrencesCount` within selected stem.
- Items are unique by related identity and ordered deterministically by count then Mushaf occurrence
  (or existing agreed display order if count tie metadata is not exposed).

## Backend Validation

- Resource IDs must be positive; unknown positive IDs produce controlled not-found.
- `sort` is one of `mushaf-order`, `occurrences`, `alpha`; empty uses `mushaf-order`.
- `wordKind` is `simple` or `tashkeel`.
- `page` and `pageSize` are positive and bounded by the existing Words conventions.
- Search is optional; whitespace means unfiltered. Raw search text is never logged.
- Readers return DTOs only and perform no tracked mutation.

## Frontend Explorer State

### LemmasExplorerState

`search`, `sort`, `page`, `lemmaId`, `view`, `wordView`, `surahView`, `detailPage`.

Allowed `view`: `words`, `ayahs`, `surahs`, `stems`.

### StemsExplorerState

`search`, `sort`, `page`, `stemId`, `view`, `wordView`, `surahView`, `detailPage`.

Allowed `view`: `words`, `ayahs`, `surahs`, `lemmas`.

### Defaults and normalization

- `sort=mushaf-order`, `page=1`, `view=words`, `wordView=simple`,
  `surahView=mentioned`, `detailPage=1`.
- Selection must be a positive integer.
- `wordView` applies only to `view=words`.
- `surahView` applies only to `view=surahs`.
- `detailPage` applies only to `words` and `ayahs`.
- Malformed or non-positive frontend `page` and `detailPage` values normalize to 1.
- Valid positive pages beyond the available results remain in URL state and render a successful
  controlled empty page.
- Clearing selection preserves `search`, `sort`, and list `page`; selection-specific fields are removed.
- Unknown selected identity enters a not-found panel state without disabling the list.

## UI State Transitions

| Action | Result |
|---|---|
| Select lemma/stem row | Set identity; `view=words`, `wordView=simple`, `detailPage=1`. |
| Activate occurrences/ayahs count | Set identity; `view=ayahs`, `detailPage=1`. |
| Activate surahs count | Set identity; `view=surahs`, `surahView=mentioned`. |
| Activate simple/tashkeel word count | Set identity; `view=words`, selected `wordView`, `detailPage=1`. |
| Activate lemma stems count | Set lemma; `view=stems`. |
| Change search/sort | Reset list page to 1; preserve the selected identity and its active detail view/sub-view/page state. |
| Change selected identity | Clear previous detail cache/session and load only the new active view. |
| Close panel | Clear selection-specific query params; preserve list state. |
| Cross-page link | Open stable destination in a new tab; current state remains unchanged. |

## Persistence and Mutation

None. All models above are projections or ephemeral UI state. No migration, importer, write endpoint,
or runtime data correction is part of Feature 016.
