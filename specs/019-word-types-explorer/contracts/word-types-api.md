# Contract: Word Types HTTP API

**Feature**: 019 — Word Types Explorer
**Layer**: API boundary (ASP.NET Core controller). **Read-only.**
**Envelope**: Every endpoint returns the global **`ApiResponse<T>`** per `Backend/.architecture/API_GUIDELINES.md`. No change to the envelope. Shapes below are the `data` payload (`T`).
**Area**: new controller `WordTypesController` under `Controllers/Words/`. Route base: `api/words/word-types`. Do **not** overload the Unique-Words endpoints.

> JSON is illustrative; field names are the contract. All counts obey the two-count-families rule (see `../data-model.md` §4). Arabic labels in payloads are **API-sourced** from `quran_pos_tags.arabic_label` (except the four static main-type strings).

---

## E1 — Type tree + counts

```
GET api/words/word-types/tree
```

Returns the four main types, their v1 child subtypes, the secondary-filter option metadata per type, and **word-context row counts** per node. Noun children are catalogue-driven from `quran_pos_tags` where `category = 'noun'`, ordered by `SortOrder`. This endpoint has no query parameters in v1; counts are static/unscoped by case, tense, or voice.

```jsonc
{
  "mainTypes": [
    {
      "code": "noun",                 // bucket key (category for parents)
      "label": { "ar": "اسم" },        // static main-type string
      "count": 0,                      // distinct word-context rows under this parent
      "secondaryFilter": {            // which secondary filter this type exposes
        "kind": "case",               // "case" | "tense+voice" | "none"
        "options": [                   // option metadata (codes + labels)
          { "code": "nominative", "label": { "ar": "مرفوع" } },
          { "code": "accusative", "label": { "ar": "منصوب" } },
          { "code": "genitive",   "label": { "ar": "مجرور" } },
          { "code": "null",       "label": { "ar": "غير محدد" } }
        ]
      },
      "children": [
        { "code": "N",   "childCode": "N",   "label": { "ar": "اسم" },    "count": 0 },
        { "code": "PN",  "childCode": "PN",  "label": { "ar": "اسم علم" }, "count": 0 },
        { "code": "ADJ", "childCode": "ADJ", "label": { "ar": "صفة" },    "count": 0 },
        { "code": "PRON","childCode": "PRON","label": { "ar": "ضمير" },   "count": 0 },
        { "code": "REL", "childCode": "REL", "label": { "ar": "اسم موصول" }, "count": 0 },
        { "code": "DEM", "childCode": "DEM", "label": { "ar": "اسم إشارة" }, "count": 0 },
        { "code": "T",   "childCode": "T",   "label": { "ar": "ظرف زمان" }, "count": 0 },
        { "code": "LOC", "childCode": "LOC", "label": { "ar": "ظرف مكان" }, "count": 0 },
        { "code": "TIM", "childCode": "TIM", "label": { "ar": "ظرف زمان" }, "count": 0 },
        { "code": "IMPN","childCode": "IMPN","label": { "ar": "اسم فعل أمر" }, "count": 0 }
      ]
    },
    {
      "code": "verb",
      "label": { "ar": "فعل" },
      "count": 0,
      "secondaryFilter": {
        "kind": "tense+voice",
        "options": [
          { "code": "past",       "label": { "ar": "ماض" } },
          { "code": "present",    "label": { "ar": "مضارع" } },
          { "code": "imperative", "label": { "ar": "أمر" } }
        ],
        "voiceOptions": [
          { "code": "active",  "label": { "ar": "معلوم" } },
          { "code": "passive", "label": { "ar": "مجهول" } }
        ]
      },
      "children": [
        { "childCode": "past",       "label": { "ar": "ماض" },   "count": 0 },
        { "childCode": "present",    "label": { "ar": "مضارع" }, "count": 0 },
        { "childCode": "imperative", "label": { "ar": "أمر" },   "count": 0 }
      ]
    },
    {
      "code": "particle",
      "label": { "ar": "حرف وأداة" },
      "count": 0,                      // MUST exclude head_pos = 'INL'
      "secondaryFilter": { "kind": "none" },
      "children": []                   // particle-code children deferred (v1)
    },
    {
      "code": "inl",
      "label": { "ar": "حروف مقطّعة" },
      "count": 0,                      // head_pos = 'INL'; leaf
      "secondaryFilter": { "kind": "none" },
      "children": []
    }
  ]
}
```

**Rules**: each `count` = distinct word-context rows for that node (data-model §4.1), with no secondary-filter scoping. `secondaryFilter.kind = "none"` for particle and INL (FR-022/023). Noun children include every noun-category POS code present in the catalogue; particle-code children are deferred in v1. POS rows outside the four bucket predicates are excluded from this tree.

---

## E2 — Paged word-context rows

```
GET api/words/word-types/words
    ?type={noun|verb|particle|inl}
    &childCode={head_pos | tense}        (optional — when a child node is selected)
    &case={nominative|accusative|genitive|null}   (nominal types only)
    &tense={past|present|imperative}     (verb only)
    &voice={active|passive}              (verb only)
    &page={n}&pageSize={n}&sort={occurrences|ayahs|surahs|mushaf-order|alpha}
```

Returns `ApiResponse<PagedResult<WordTypeRowDto>>`. The payload is a page of
**word-context rows** (data-model §3). Default sort is `occurrences` descending with a deterministic
Mushaf-order/identity tie-break.

```jsonc
{
  "page": 1,
  "pageSize": 25,
  "totalCount": 0,                      // == E1 node count only when no secondary filter is applied
  "items": [
    {
      "tashkeelWordId": 1234,
      "contextCode": "PN",              // row's resolved context; part of the row key (R6)
      "displayText": "…",               // Uthmani + tashkeel
      "typeCode": "PN",                 // ALWAYS exact for this row — never dominant/mixed
      "typeLabel": { "ar": "اسم علم" }, // from quran_pos_tags
      "broadLabel": { "ar": "اسم" },
      "caseOrFeature": "genitive",      // row's own case/tense/voice context, or null
      "rootText": "…",                  // الجذر (winner; null if unavailable)
      "lemmaText": null,                // الأصل (winner or null when unavailable/deferred)
      "stemText": null,                 // الصيغة (winner or null when unavailable/deferred)
      "occurrencesCount": 0,            // المواضع — scoped to THIS row context
      "ayahsCount": 0,                  // الآيات
      "surahsCount": 0                  // السور
    }
  ]
}
```

**Rules**:
- Row grouping key per active filter context (data-model §3.1). No mixed rows (FR-017/018).
- `totalCount` **MUST equal** the E1 node count for the same active type/child only when no secondary filter is applied; secondary filters narrow E2 `totalCount` and active UI count chips only.
- All counts are occurrence-scoped to each row's exact context, never the union of the word's usages (FR-028).
- Rows with null `rootText`, `lemmaText`, or `stemText` remain in the result; the frontend renders `—` for null values.
- `!IsAyahMarker` always applied.

---

## E3 — Row details-card summary

```
GET api/words/word-types/words/{tashkeelWordId}
    ?contextCode={code}&case=&tense=&voice=
```

Returns the details-card summary for **one word-context row**. The full row context (`tashkeelWordId` + `contextCode` + active feature) must reproduce the exact same row as E2 (no re-collapsing).

```jsonc
{
  "tashkeelWordId": 1234,
  "contextCode": "PN",
  "displayText": "…",
  "typeLabel": { "ar": "اسم علم" },
  "broadLabel": { "ar": "اسم" },
  "caseOrFeature": "genitive",
  "rootText": "…", "lemmaText": null, "stemText": null,
  "occurrencesCount": 0, "ayahsCount": 0, "surahsCount": 0
}
```

---

## E4 — Ayah matches for the row context

```
GET api/words/word-types/words/{tashkeelWordId}/ayahs
    ?contextCode={code}&case=&tense=&voice=&page=&pageSize=
```

Returns `ApiResponse<PagedResult<WordTypeAyahMatchDto>>`.

Ayah list for the exact row context; **highlights only the occurrences belonging to that row context**
(not all usages of the word). Mirror the existing `*AyahMatchDto` / `verse-key` shape used by the
other explorers.

```jsonc
{
  "page": 1,
  "pageSize": 25,
  "totalCount": 0,
  "items": [
    {
      "verseKey": "2:255",
      "surahNumber": 2,
      "ayahNumber": 255,
      "ayahText": "…",
      "matchedWordPositions": [3]      // only positions in THIS row context
    }
  ]
}
```

---

## E5 — Surah distribution for the row context

```
GET api/words/word-types/words/{tashkeelWordId}/surahs
    ?contextCode={code}&case=&tense=&voice=
```

Surah distribution + missing surahs for the exact row context. Mirror the existing `*SurahsResponse` / `*MissingSurahsResponse` shapes.

```jsonc
{
  "surahs": [ { "surahNumber": 2, "occurrencesCount": 0 } ],
  "missingSurahs": [ 1, 9, 112 ]
}
```

---

## Reuse — per-occurrence full analysis (existing, unchanged)

```
GET api/mushaf/words/{location}/analysis
```

Used by the details card **التحليل** tab for a chosen occurrence. **Reuse as-is** (`WordAnalysisResponse`); do not rebuild i'rab.

---

## Cross-cutting contract rules

1. **Envelope**: all of E1–E5 return `ApiResponse<T>`; errors use the standard envelope. No envelope change.
2. **Paged shape**: E2 and E4 use the existing `PagedResult<T>` shape: `page`, `pageSize`, `totalCount`, `items`.
3. **Count integrity**: E2 `totalCount` == E1 node count for the same type/child only when no secondary filter is applied (FR-027); secondary filters narrow E2 and active UI chips only.
4. **Row addressability**: `contextCode` is required to address E3–E5; omitting it for a multi-usage word is a client error, not an implicit union (R14).
5. **Marker exclusion**: every endpoint excludes ayah-marker words.
6. **Labels**: POS/type labels are API-sourced; the four main-type strings + secondary-option strings may be static UI labels.
7. **Read-only**: no write verbs; no migration; no new identity table.
