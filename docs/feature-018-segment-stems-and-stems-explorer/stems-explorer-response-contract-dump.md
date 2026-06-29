# Stems Explorer Response Contract Dump

Read-only dump. Shapes only. No cleanup, no refactor, no code change.

## Common Shapes

| Shape | Current shape |
|---|---|
| `ApiResponse<T>` | `{ isSuccess: boolean, message: string \| null, data?: T \| null, errors?: string[] \| null }` |
| `PagedResult<T>` | `{ page: number, pageSize: number, totalCount: number, items: T[] }` |
| `TypeSummaryDto` | `{ code: string, arabicLabel: string, englishLabel: string, occurrencesCount: number, firstSurahNumber: number, firstAyahNumber: number, firstWordNumber: number }` |
| `AyahWordForHighlightDto` | `{ quranWordId: number, textUthmani: string, isAyahMarker: boolean }` |

## Special Focus

- `StemWordsListComponent` renders `StemWordItemDto.displayTextUthmani`.
- Current simple branch in `EfStemsReader.LoadStemWordRowsAsync` projects `w.TextUthmani`.
- Current tashkeel branch also projects `w.TextUthmani`.
- So `بدون تشكيل` shows tashkeel because backend simple projection is wrong.
- Fix is backend-only if contract stays same: simple branch should project `w.TextUthmaniSimple`.

## GET `/api/words/stems`

Query params: `search?`, `sort?=mushaf-order|occurrences|alpha`, `page?=1`, `pageSize?=1000`

Backend type: `ApiResponse<PagedResult<StemListItemDto>>`

### Current shape

```json
{
  "isSuccess": true,
  "message": "<localized success message>",
  "data": {
    "page": 1,
    "pageSize": 1000,
    "totalCount": 1,
    "items": [
      {
        "id": 500,
        "stemText": "أصل-500",
        "lemmaId": 700,
        "lemmaText": "صيغة-700",
        "lemmaBuckwalter": null,
        "rootId": 800,
        "rootText": "جذر-800",
        "rootBuckwalter": null,
        "dominantType": {
          "code": "N",
          "arabicLabel": "اسم",
          "englishLabel": "Noun",
          "occurrencesCount": 5,
          "firstSurahNumber": 1,
          "firstAyahNumber": 1,
          "firstWordNumber": 1
        },
        "otherTypesCount": 0,
        "occurrencesCount": 5,
        "ayahsCount": 3,
        "surahsCount": 2,
        "simpleWordsCount": 2,
        "tashkeelWordsCount": 2,
        "firstVerseKey": "1:1"
      }
    ]
  },
  "errors": null
}
```

### Field map

| Field | Backend source / projection | Frontend consumer | Visible in UI? | Used for routing/cache/highlighting/tests only? | Safe to remove? | Recommendation |
|---|---|---|---|---|---|---|
| `id` | `StemSummaryRow.Id` from `quran_stems.id` | `stems-explorer.facade.ts`, `stems-table.component.ts/html` | no | yes | no | KEEP |
| `stemText` | `StemSummaryRow.StemText` | `stems-explorer.facade.ts` maps to `displayText`; `stems-table.component.html` | yes | no | no | KEEP |
| `lemmaId` | dominant lemma row from `BuildDominantLemma(...)` | `stems-table.component.ts/html` | no | yes | no | KEEP |
| `lemmaText` | dominant lemma row from `BuildDominantLemma(...)` | `stems-table.component.ts/html` | yes | no | no | KEEP |
| `lemmaBuckwalter` | dominant lemma row from `BuildDominantLemma(...)` | runtime none; tests/spec fixtures only | no | yes | needs coordinated change | KEEP FOR NOW |
| `rootId` | dominant root row from `BuildDominantRoot(...)` | `stems-table.component.ts/html` | no | yes | no | KEEP |
| `rootText` | dominant root row from `BuildDominantRoot(...)` | `stems-table.component.ts/html` | yes | no | no | KEEP |
| `rootBuckwalter` | dominant root row from `BuildDominantRoot(...)` | runtime none; tests/spec fixtures only | no | yes | needs coordinated change | KEEP FOR NOW |
| `dominantType` | first `StemTypeDistributionRow` from `MaterializeTypeDistribution(...)` | `stems-table.component.ts/html` | partial | no | no | KEEP |
| `otherTypesCount` | computed from type distribution | `stems-table.component.ts/html` | yes | no | no | KEEP |
| `occurrencesCount` | SQL aggregate | table / pagination logic | yes | no | no | KEEP |
| `ayahsCount` | SQL aggregate | table | yes | no | no | KEEP |
| `surahsCount` | SQL aggregate | table | yes | no | no | KEEP |
| `simpleWordsCount` | SQL aggregate | table | yes | no | no | KEEP |
| `tashkeelWordsCount` | SQL aggregate | table | yes | no | no | KEEP |
| `firstVerseKey` | `BuildFirstVerseKey(...)` | backend tests/spec fixtures only | no | yes | needs coordinated change | KEEP FOR NOW |

## GET `/api/words/stems/{id}`

Query params: none

Backend type: `ApiResponse<StemSummaryDto>`

### Current shape

Same list shape above, plus `typeDistribution`.

```json
{
  "isSuccess": true,
  "message": "<localized success message>",
  "data": {
    "id": 500,
    "stemText": "أصل-500",
    "lemmaId": 700,
    "lemmaText": "صيغة-700",
    "lemmaBuckwalter": null,
    "rootId": 800,
    "rootText": "جذر-800",
    "rootBuckwalter": null,
    "dominantType": {
      "code": "N",
      "arabicLabel": "اسم",
      "englishLabel": "Noun",
      "occurrencesCount": 5,
      "firstSurahNumber": 1,
      "firstAyahNumber": 1,
      "firstWordNumber": 1
    },
    "otherTypesCount": 0,
    "occurrencesCount": 5,
    "ayahsCount": 3,
    "surahsCount": 2,
    "simpleWordsCount": 2,
    "tashkeelWordsCount": 2,
    "firstVerseKey": "1:1",
    "typeDistribution": [
      {
        "code": "N",
        "arabicLabel": "اسم",
        "englishLabel": "Noun",
        "occurrencesCount": 5,
        "firstSurahNumber": 1,
        "firstAyahNumber": 1,
        "firstWordNumber": 1
      }
    ]
  },
  "errors": null
}
```

### Field map

| Field | Backend source / projection | Frontend consumer | Visible in UI? | Used for routing/cache/highlighting/tests only? | Safe to remove? | Recommendation |
|---|---|---|---|---|---|---|
| `typeDistribution` | `StemSummaryRow.TypeDistribution` from `LoadWholeSummaryAsync()` | `stems-detail.facade.ts`, `stems-explorer-page.component.ts`, `stem-ayah-type-filters.component.ts/html` | yes | yes | no | KEEP |
| All list fields above | same as `/api/words/stems` | same as list endpoint | mixed | mixed | mixed | same as list endpoint |

## GET `/api/words/stems/{id}/words/{wordKind}`

Path param: `wordKind = simple | tashkeel`

Query params: `page?=1`, `pageSize?=100`

Backend type: `ApiResponse<PagedResult<StemWordItemDto>>`

### Current shape: simple

```json
{
  "isSuccess": true,
  "message": "<localized success message>",
  "data": {
    "page": 1,
    "pageSize": 100,
    "totalCount": 2,
    "items": [
      {
        "uniqueWordId": 32001,
        "kind": "simple",
        "displayTextUthmani": "كَلِمَة",
        "occurrencesCount": 10,
        "firstVerseKey": "1:1"
      },
      {
        "uniqueWordId": 32002,
        "kind": "simple",
        "displayTextUthmani": "كَلَّمَ",
        "occurrencesCount": 1,
        "firstVerseKey": "1:1"
      }
    ]
  },
  "errors": null
}
```

### Current shape: tashkeel

```json
{
  "isSuccess": true,
  "message": "<localized success message>",
  "data": {
    "page": 1,
    "pageSize": 100,
    "totalCount": 2,
    "items": [
      {
        "uniqueWordId": 31001,
        "kind": "tashkeel",
        "displayTextUthmani": "كَلِمَة",
        "occurrencesCount": 10,
        "firstVerseKey": "1:1"
      },
      {
        "uniqueWordId": 31002,
        "kind": "tashkeel",
        "displayTextUthmani": "كَلَّمَ",
        "occurrencesCount": 1,
        "firstVerseKey": "1:1"
      }
    ]
  },
  "errors": null
}
```

### Field map

| Field | Backend source / projection | Frontend consumer | Visible in UI? | Used for routing/cache/highlighting/tests only? | Safe to remove? | Recommendation |
|---|---|---|---|---|---|---|
| `uniqueWordId` | `w.UniqueSimpleWordId` or `w.UniqueTashkeelWordId` | `stems-detail-view.loader.ts`, `stem-words-list.component.ts/html` | no | yes | no | KEEP |
| `kind` | selected `StemWordKindKeys.Simple/Tashkeel` | `stem-words-list.component.ts` deep-link build | no | yes | no | KEEP |
| `displayTextUthmani` | simple branch should be `w.TextUthmaniSimple`; current bug branch uses `w.TextUthmani`; tashkeel branch uses `w.TextUthmani` | `stem-words-list.component.html` | yes | no | no | FIX SOURCE |
| `occurrencesCount` | grouped count in `GetStemWordsPageAsync` | `stem-words-list.component.html` | yes | no | no | KEEP |
| `firstVerseKey` | `BuildFirstVerseKey(...)` | backend tests/spec fixtures only | no | yes | needs coordinated change | REMOVE |

## GET `/api/words/stems/{id}/ayahs`

Query params: `page?=1`, `pageSize?=100`, `type?` optional

Backend type: `ApiResponse<PagedResult<StemAyahMatchDto>>`

### Current shape

```json
{
  "isSuccess": true,
  "message": "<localized success message>",
  "data": {
    "page": 1,
    "pageSize": 100,
    "totalCount": 1,
    "items": [
      {
        "ayahId": 7001,
        "verseKey": "4:57",
        "surahNumber": 4,
        "surahNameArabic": "النساء",
        "ayahNumber": 57,
        "pageNumber": 92,
        "matchedQuranWordIds": [9001],
        "words": [
          {
            "quranWordId": 9001,
            "textUthmani": "كلمة-تجريبية-١",
            "isAyahMarker": false
          }
        ]
      }
    ]
  },
  "errors": null
}
```

### Field map

| Field | Backend source / projection | Frontend consumer | Visible in UI? | Used for routing/cache/highlighting/tests only? | Safe to remove? | Recommendation |
|---|---|---|---|---|---|---|
| `ayahId` | `quran_ayahs.id` | `ayah-matches-list.component.ts/html` trackBy | no | no | needs coordinated change | KEEP FOR NOW |
| `verseKey` | `quran_ayahs.verse_key` | `ayah-matches-list.component.ts/html` Mushaf deep link | yes | yes | no | KEEP |
| `surahNumber` | `quran_ayahs.surah_number` | runtime none | no | no | yes | REMOVE |
| `surahNameArabic` | join `quran_surahs.name_arabic` | `ayah-matches-list.component.html` | yes | no | no | KEEP |
| `ayahNumber` | `quran_ayahs.ayah_number` | `ayah-matches-list.component.html` | yes | no | no | KEEP |
| `pageNumber` | `ResolveAyahPageNumber(...)` from first readable word | `ayah-matches-list.component.ts/html` | yes | yes | no | KEEP |
| `matchedQuranWordIds` | distinct matched Quran word ids from morphology join | `highlighted-ayah.component.ts/html` | no | yes | no | KEEP |
| `words` | `QuranWords` rows for ayah, ordered by mushaf order | `highlighted-ayah.component.ts/html` | yes | yes | no | KEEP |

### Ayah highlight words shape

`words[]` is `AyahWordForHighlightDto[]`:

```json
{
  "quranWordId": 9001,
  "textUthmani": "كلمة-تجريبية-١",
  "isAyahMarker": false
}
```

## GET `/api/words/stems/{id}/surahs`

Query params: none

Backend type: `ApiResponse<StemSurahsResponse>`

### Current shape

```json
{
  "isSuccess": true,
  "message": "<localized success message>",
  "data": {
    "id": 500,
    "stemText": "أصل-500",
    "surahsCount": 1,
    "surahs": [
      {
        "surahNumber": 1,
        "nameArabic": "سورة-اختبار",
        "occurrencesInSurah": 2
      }
    ]
  },
  "errors": null
}
```

### Field map

| Field | Backend source / projection | Frontend consumer | Visible in UI? | Used for routing/cache/highlighting/tests only? | Safe to remove? | Recommendation |
|---|---|---|---|---|---|---|
| `id` | `quran_stems.id` | runtime none; page-spec fixtures only | no | yes | needs coordinated change | REMOVE |
| `stemText` | `quran_stems.stem_text` | runtime none; page-spec fixtures only | no | yes | needs coordinated change | REMOVE |
| `surahsCount` | `surahs.Count` | runtime none; page-spec fixtures only | no | yes | needs coordinated change | REMOVE |
| `surahs` | `StemSurahItemDto[]` | `surah-occurrences-list.component.ts/html` | yes | no | no | KEEP |

### Nested item shape

`surahs[]` is `StemSurahItemDto[]`:

```json
{
  "surahNumber": 1,
  "nameArabic": "سورة-اختبار",
  "occurrencesInSurah": 2
}
```

| Field | Backend source / projection | Frontend consumer | Visible in UI? | Used for routing/cache/highlighting/tests only? | Safe to remove? | Recommendation |
|---|---|---|---|---|---|---|
| `surahNumber` | `SurahOccurrenceRow.SurahNumber` | `surah-occurrences-list.component.html` trackBy | no | no | needs coordinated change | KEEP FOR NOW |
| `nameArabic` | `QuranSurahs.NameArabic` | `surah-occurrences-list.component.html` | yes | no | no | KEEP |
| `occurrencesInSurah` | `SurahOccurrenceRow.OccurrencesInSurah` | `surah-occurrences-list.component.html` | yes | no | no | KEEP |

## GET `/api/words/stems/{id}/missing-surahs`

Query params: none

Backend type: `ApiResponse<StemMissingSurahsResponse>`

### Current shape

```json
{
  "isSuccess": true,
  "message": "<localized success message>",
  "data": {
    "id": 500,
    "stemText": "أصل-500",
    "missingSurahsCount": 0,
    "surahs": []
  },
  "errors": null
}
```

### Field map

| Field | Backend source / projection | Frontend consumer | Visible in UI? | Used for routing/cache/highlighting/tests only? | Safe to remove? | Recommendation |
|---|---|---|---|---|---|---|
| `id` | `quran_stems.id` | runtime none; page-spec fixtures only | no | yes | needs coordinated change | REMOVE |
| `stemText` | `quran_stems.stem_text` | runtime none; page-spec fixtures only | no | yes | needs coordinated change | REMOVE |
| `missingSurahsCount` | `missingSurahs.Count` | runtime none; page-spec fixtures only | no | yes | needs coordinated change | REMOVE |
| `surahs` | `MissingSurahItemDto[]` | `missing-surahs-list.component.ts/html` | yes | no | no | KEEP |

### Nested item shape

`surahs[]` is `MissingSurahItemDto[]`:

```json
{
  "surahNumber": 2,
  "nameArabic": "سورة-اختبار"
}
```

| Field | Backend source / projection | Frontend consumer | Visible in UI? | Used for routing/cache/highlighting/tests only? | Safe to remove? | Recommendation |
|---|---|---|---|---|---|---|
| `surahNumber` | `QuranSurahs.SurahNumber` | `missing-surahs-list.component.html` trackBy | no | no | needs coordinated change | KEEP FOR NOW |
| `nameArabic` | `QuranSurahs.NameArabic` | `missing-surahs-list.component.html` | yes | no | no | KEEP |

## GET `/api/words/stems/{id}/lemmas`

Query params: none

Backend type: `ApiResponse<StemLemmasResponse>`

### Current shape

```json
{
  "isSuccess": true,
  "message": "<localized success message>",
  "data": {
    "id": 602,
    "stemText": "عَلِمَ",
    "lemmasCount": 2,
    "lemmas": [
      {
        "lemmaId": 502,
        "lemmaText": "عِلْم",
        "lemmaBuckwalter": "Ailm",
        "occurrencesCount": 3
      },
      {
        "lemmaId": 504,
        "lemmaText": "مَعْرِفَة",
        "lemmaBuckwalter": "maArifap",
        "occurrencesCount": 1
      }
    ]
  },
  "errors": null
}
```

### Field map

| Field | Backend source / projection | Frontend consumer | Visible in UI? | Used for routing/cache/highlighting/tests only? | Safe to remove? | Recommendation |
|---|---|---|---|---|---|---|
| `id` | `quran_stems.id` | runtime none; page-spec fixtures only | no | yes | needs coordinated change | REMOVE |
| `stemText` | `quran_stems.stem_text` | runtime none; page-spec fixtures only | no | yes | needs coordinated change | REMOVE |
| `lemmasCount` | `lemmas.Count` | runtime none; page-spec fixtures only | no | yes | needs coordinated change | REMOVE |
| `lemmas` | `StemLemmaItemDto[]` | `stem-lemmas-list.component.ts/html` | yes | no | no | KEEP |

### Nested item shape

`lemmas[]` is `StemLemmaItemDto[]`:

```json
{
  "lemmaId": 502,
  "lemmaText": "عِلْم",
  "lemmaBuckwalter": "Ailm",
  "occurrencesCount": 3
}
```

| Field | Backend source / projection | Frontend consumer | Visible in UI? | Used for routing/cache/highlighting/tests only? | Safe to remove? | Recommendation |
|---|---|---|---|---|---|---|
| `lemmaId` | related lemma id from `GetStemLemmasAsync` | `stem-lemmas-list.component.ts/html` deep link | no | yes | no | KEEP |
| `lemmaText` | `QuranLemmas.LemmaText` | `stem-lemmas-list.component.html` | yes | no | no | KEEP |
| `lemmaBuckwalter` | `QuranLemmas.LemmaBuckwalter` | `stem-lemmas-list.component.html` | yes | no | no | KEEP |
| `occurrencesCount` | grouped count in `GetStemLemmasAsync` | `stem-lemmas-list.component.html` | yes | no | no | KEEP |

## Confirmed Answer

- The only field causing `بدون تشكيل` to show tashkeel is `StemWordItemDto.displayTextUthmani`.
- The only required fix for that bug is backend projection from `TextUthmani` to `TextUthmaniSimple` in the simple stem-word branch.
- No frontend rendering change is required for that specific bug.
