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
      "lemmaText": null,                // الصيغة المعجمية (winner or null when unavailable/deferred)
      "stemText": null,                 // الأصل الصرفي (winner or null when unavailable/deferred)
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

## E2b — Unified table endpoint (table-view tabs, Feature 022)

```
GET api/words/word-types/table
    ?tableView={words|roots|stems|lemmas}     (optional; default words)
    &type={noun|verb|particle|inl}
    &childCode={head_pos | tense}        (optional — when a child node is selected)
    &case={nominative|accusative|genitive|null}   (nominal types only)
    &tense={past|present|imperative}     (verb only)
    &voice={active|passive}              (verb only)
    &sort={occurrences|ayahs|surahs|mushaf-order|alpha}
    &page={n}&pageSize={n}
```

Returns `ApiResponse<PagedResult<WordTypeTableRowDto>>`, a **discriminated union** distinguished by a
`kind` property (`"word" | "root" | "stem" | "lemma"`). `tableView=words` (the default) returns the
same word-context rows as E2, plus `kind:"word"` and the row's own `case`/`tense`/`voice`, so the full
composite identity (`tashkeelWordId` + `contextCode` + `case` + `tense` + `voice`) is complete in the
payload — the frontend never re-stamps it. `tableView=roots`, `stems`, or `lemmas` return **grouped**
rows: one row per distinct non-null `rootId`/`stemId`/`lemmaId` within the same filtered occurrence
scope as E2, grouped and counted **before** pagination. Grouped rows carry no row-level detail
(grouped-row drilldown is out of MVP) and render noninteractive in the UI.

```jsonc
// tableView=words (word variant — superset of the E2 WordTypeRowDto shape)
{
  "page": 1, "pageSize": 25, "totalCount": 0,
  "items": [
    {
      "kind": "word",
      "tashkeelWordId": 1234,
      "contextCode": "PN",
      "case": null,                     // null when the case filter is inactive for this scope
      "tense": null,
      "voice": null,
      "displayText": "…",
      "typeCode": "PN",
      "typeLabel": { "ar": "اسم علم" },
      "broadLabel": { "ar": "اسم" },
      "caseOrFeature": "genitive",
      "rootText": "…", "lemmaText": null, "stemText": null,
      "occurrencesCount": 0, "ayahsCount": 0, "surahsCount": 0
    }
  ]
}

// tableView=roots|stems|lemmas (grouped variant — numeric stable ID identity)
{
  "page": 1, "pageSize": 25, "totalCount": 0,   // totalCount = distinct non-null dimension IDs in scope
  "items": [
    { "kind": "root", "rootId": 4210, "displayText": "ك ت ب", "occurrencesCount": 0, "ayahsCount": 0, "surahsCount": 0 }
  ]
}
```

**Rules**:
- Missing/blank `tableView` defaults to `words`; an unrecognized `tableView` value is a controlled 400
  (`InvalidTableView` → `ApiMessages.WordTypesInvalidTableView`), not a silent fallback.
- Grouped views reuse the **identical scoped occurrence base** as E2 (type + child + case + tense +
  voice + `!IsAyahMarker` + non-null tashkeel identity) — the same rows E2 would return, grouped by the
  dimension ID instead of `tashkeelWordId + contextCode`.
- Grouped identity is the **numeric** `rootId`/`stemId`/`lemmaId`. Arabic display text is never
  identity.
- Rows with a null `rootId`/`stemId`/`lemmaId` for the active dimension are **excluded** from grouped
  views (never rendered as an "unknown" bucket).
- Grouped `occurrencesCount`/`ayahsCount`/`surahsCount` are the same occurrence-scoped aggregates as
  E2's table columns (§4.2 of `../data-model.md`), summed per dimension ID instead of per row — a
  **third** view of the occurrence-count family, distinct from both the E1 tree/node row-count family
  and the Roots/Lemmas/Stems explorers' own global segment/`words_count`-backed counts. Grouped counts
  are **never** derived from those explorer aggregates.
- Grouped `totalCount` = count of distinct non-null dimension IDs in the active scope. It is measured
  in a different unit than the E2 `totalCount` (word-context rows) — **the two must never be compared**
  to reason about null-dimension coverage. Null-dimension coverage is instead an **occurrence-sum
  identity**: `Σ occurrencesCount` across all grouped pages (non-null dimension) plus the occurrences
  whose dimension ID is null equals `Σ occurrencesCount` across the `tableView=words` pages for the
  same scope.
- Sorting is deterministic for every `sort` value, tie-broken by the numeric dimension ID (metric sorts
  add a first-Mushaf-occurrence tie-break before the ID; `alpha` reuses the Roots explorer's Arabic
  fold with ordinal collation before the ID).
- `GET .../word-types/words` (E2) is **preserved unchanged** — same params, same `WordTypeRowDto`
  shape, no `kind` discriminator — for existing deep links and external consumers.
- Grouped-row **details** were out of MVP in Feature 022; Feature 023 adds a Word-Types-owned grouped
  detail family (summary/words/ayahs/surahs) under `.../table/{kind}/{dimensionId}` — see **E2c**.
- Cache keys (`wordtypes:table:{filter-hash}:view:{tableView}:sort:{sort}:p{page}:s{pageSize}`) include
  `tableView`, so switching tabs never cross-serves another view's rows.

---

## E2c — Grouped detail summary (root/stem/lemma, Feature 023)

```
GET api/words/word-types/table/{kind}/{dimensionId}
    ?type={noun|verb|particle|inl}
    &childCode={head_pos | tense}        (optional — when a child node is selected)
    &case={nominative|accusative|genitive|null}   (nominal types only)
    &tense={past|present|imperative}     (verb only)
    &voice={active|passive}              (verb only)
```

`kind` is the **plural** route key `roots|stems|lemmas`; `dimensionId` is the numeric
`rootId`/`stemId`/`lemmaId` of a row selected from **E2b**. Returns
`ApiResponse<WordTypeGroupedSummaryDto>` — a **single-shot** scoped summary for that one grouped
dimension. Every grouped detail read carries the identical five-field grammatical scope
(`type`, `childCode`, `case`, `tense`, `voice`) as the selected table row.

```jsonc
{
  "kind": "root",                 // singular discriminator: root | stem | lemma
  "dimensionId": 4210,
  "displayText": "ك ت ب",         // projection-only; never membership identity
  "occurrencesCount": 0,          // المواضع — scoped occurrences of this dimension
  "ayahsCount": 0,                // الآيات — distinct ayahs
  "surahsCount": 0                // السور — distinct surahs
}
```

**Rules**:
- Route `kind` is plural; an unknown value → **400** (`InvalidKind` → `WordTypesInvalidGroupedKind`).
  `dimensionId ≤ 0` → **400** (`InvalidId` → `WordTypesInvalidGroupedId`). A cross-type/invalid grammatical
  filter → **400** (`InvalidFilter` → `WordTypesInvalidFilter`). A **positive** dimension ID that is
  absent from the scoped base → **404** (`NotFound` → `WordTypesGroupedNotFound`). Success → **200**.
- Validation order in the handler is fixed: route kind → positive ID → grammatical filter → reader result.
- `dimensionId`, `displayText`, and the three counts are **exactly equal** to the selected E2b grouped
  row in the same scope (same occurrence base, same aggregates restricted to one dimension ID).
- Membership and all counts derive from **head-level `quran_word_morphology`** via the same scoped
  `base` CTE as E2/E2b. `quran_word_morphology_segments` is **never** joined; a segment-only dimension
  never surfaces and never displaces a word's head IDs.
- Numeric dimension identity only; the text columns are projection-only display fields. Null dimensions
  and ayah-marker words are excluded.
- Cache key `wordtypes:grouped:{kind}:summary:{scope-hash}` folds the dimension ID plus the five scope
  fields into the hash; different kinds/scopes never cross-serve, and only kind/view labels appear in the
  readable prefix.
- The paged member-words, paged ayahs, and single-shot surahs resources under the same
  `{kind}/{dimensionId}` base are added by Feature 023 Tasks 2–4.

## E2d — Grouped detail member words (root/stem/lemma, Feature 023)

```
GET api/words/word-types/table/{kind}/{dimensionId}/words
    ?type={noun|verb|particle|inl}
    &childCode={head_pos | tense}        (optional — when a child node is selected)
    &case={nominative|accusative|genitive|null}   (nominal types only)
    &tense={past|present|imperative}     (verb only)
    &voice={active|passive}              (verb only)
    &page={n}&pageSize={1..100}          (no sort — member order is fixed)
```

Returns `ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>` — the **display-only** member
word-context rows of the scoped group. Same five-field scope as E2c; **no `sort`** parameter.

```jsonc
{
  "tashkeelWordId": 1234,
  "contextCode": "PN",
  "case": null, "tense": null, "voice": null,   // active scope, exactly as the E2b word row
  "displayText": "…",
  "typeCode": "PN",
  "typeLabel": { "ar": "اسم علم" },
  "broadLabel": { "ar": "اسم" },
  "caseOrFeature": "genitive",
  "rootText": "…", "lemmaText": null, "stemText": null,   // projection-only display, never membership
  "occurrencesCount": 0, "ayahsCount": 0, "surahsCount": 0
}
```

**Rules**:
- Membership is the scoped `base` CTE (E2/E2b) restricted to the selected **numeric** head
  `root_id | stem_id | lemma_id = {dimensionId}` **before** grouping, then grouped by the identical
  `(unique_tashkeel_word_id, context_code)` formula the Words view (E2) uses — **row-for-row parity**
  with a numeric-ID-scoped Words baseline. The `rootText`/`lemmaText`/`stemText` labels are
  projection-only and are **never** a membership or parity predicate.
- The same tashkeel word used across multiple contexts (e.g. `N`/`PN`/`ADJ`, or verb past/present/
  imperative) stays **multiple** member rows; there is no distinct-word collapse.
- `TotalCount` is the grouped word-context row count measured **before** paging. Member order is the
  fixed occurrence order (`occurrencesCount DESC`, first mushaf order, tashkeel ID, context).
- Invalid kind/id/filter → **400** (as E2c); invalid paging (`page < 1` or `pageSize` outside `1..100`)
  → **400** (`InvalidPaging` → `WordTypesInvalidPaging`). A **positive** dimension ID absent from the
  scope → **404** (`WordTypesGroupedNotFound`); an existing selection with an **out-of-range** page →
  **200** with an empty `items` array and the correct `TotalCount`.
- Cache key `wordtypes:grouped:{kind}:words:{scope-hash}:p{page}:s{pageSize}` — a distinct `:words:`
  segment (never shares the `:summary:` prefix), page/pageSize appended.

## E2e — Grouped detail ayahs (root/stem/lemma, Feature 023)

```
GET api/words/word-types/table/{kind}/{dimensionId}/ayahs
    ?type={noun|verb|particle|inl}
    &childCode={head_pos | tense}        (optional — when a child node is selected)
    &case={nominative|accusative|genitive|null}   (nominal types only)
    &tense={past|present|imperative}     (verb only)
    &voice={active|passive}              (verb only)
    &page={n}&pageSize={1..100}          (no sort — ayahs page in Mushaf order)
```

Returns `ApiResponse<PagedResult<WordTypeAyahMatchDto>>` — the **distinct scoped ayahs** of the group,
paged in Mushaf order, each hydrated with its full readable word list and the scoped matched-word
provenance. Same five-field scope as E2c; **no `sort`** parameter. Reuses the same `WordTypeAyahMatchDto`
shape as E4.

```jsonc
{
  "verseKey": "2:25",
  "surahNumber": 2, "ayahNumber": 25,
  "pageNumber": 5,
  "matchedWordPositions": [1, 2],           // 1-based word_number of the scoped head matches only
  "matchedWordIds": [1907001, 1907002],     // quran_words.id of the scoped head matches only
  "words": [                                // every readable word of the ayah, marker rows excluded
    { "quranWordId": 1907001, "textUthmani": "…", "isAyahMarker": false }
  ]
}
```

**Rules**:
- The paged ayahs are the **distinct `ayah_id`** of the same scoped `base` CTE restricted to the numeric
  head `root_id | stem_id | lemma_id = {dimensionId}` (E2d membership), ordered by `(surah_number,
  ayah_number)`. `TotalCount` is the distinct-ayah count measured **before** paging.
- Highlight provenance is canonical: the `words` list hydrates `quran_words.text_uthmani` (no ayah-text
  fallback, no string replacement); `matchedWordIds`/`matchedWordPositions` carry **only** the scoped
  head matches, so a non-scoped word in the same ayah appears in `words` but not in the matches. Ayah
  markers are excluded from `words`, and secondary-segment dimensions never surface at head grain.
- Page hydration is **bounded**: one distinct-ayah count query, one grouped-page query, and one
  word-hydration query per page — the command count is fixed regardless of how many ayahs the page
  returns (never one query per ayah).
- Invalid kind/id/filter → **400** (as E2c); invalid paging (`page < 1` or `pageSize` outside `1..100`)
  → **400** (`InvalidPaging` → `WordTypesInvalidPaging`). A **positive** dimension ID absent from the
  scope → **404** (`WordTypesGroupedNotFound`); an existing selection with an **out-of-range** page →
  **200** with an empty `items` array and the correct `TotalCount`.
- Cache key `wordtypes:grouped:{kind}:ayahs:{scope-hash}:p{page}:s{pageSize}` — a distinct `:ayahs:`
  segment (never shares the `:summary:`/`:words:` prefixes), page/pageSize appended.

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
      "matchedWordPositions": [3],
      "matchedWordIds": [12345],
      "words": [
        { "quranWordId": 12340, "textUthmani": "…", "isAyahMarker": false }
      ]
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
  "surahs": [ { "surahNumber": 2, "nameArabic": "البقرة", "occurrencesCount": 0 } ],
  "missingSurahs": [ { "surahNumber": 1, "nameArabic": "الفاتحة" } ]
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

1. **Envelope**: all of E1–E5 (and E2b) return `ApiResponse<T>`; errors use the standard envelope. No envelope change.
2. **Paged shape**: E2, E2b, and E4 use the existing `PagedResult<T>` shape: `page`, `pageSize`, `totalCount`, `items`. E2b's `items` are the `kind`-discriminated `WordTypeTableRowDto` union (§E2b); E2's `items` stay the flat `WordTypeRowDto` shape.
3. **Count integrity**: E2 `totalCount` == E1 node count for the same type/child only when no secondary filter is applied (FR-027); secondary filters narrow E2 and active UI chips only.
4. **Row addressability**: `contextCode` is required to address E3–E5; omitting it for a multi-usage word is a client error, not an implicit union (R14).
5. **Marker exclusion**: every endpoint excludes ayah-marker words.
6. **Labels**: POS/type labels are API-sourced; the four main-type strings + secondary-option strings may be static UI labels.
7. **Read-only**: no write verbs; no migration; no new identity table.
