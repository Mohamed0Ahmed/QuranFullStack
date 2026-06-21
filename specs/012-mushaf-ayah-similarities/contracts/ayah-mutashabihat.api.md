# Contract: Ayah Mutashabihat API

## Endpoint

```text
GET /api/mushaf/ayahs/{verseKey}/mutashabihat
```

Returns grouped phrase/word-based mutashabihat for a selected ayah.

## Request

- `verseKey` (path, required): selected ayah natural key, e.g. `2:25`.
- No body. Read-only. Lazy: called only when `المتشابهات اللفظية للحفظ` is active.

## Behavior

- Query groups containing the selected ayah.
- Return one group object per distinct group.
- Each group contains its own occurrences across ayahs.
- Never flatten all occurrences into one top-level list.
- If phrase text is returned, derive it from canonical word text for the occurrence ayah and word range.
- If phrase text cannot be resolved cleanly, return the word range and omit phrase text rather than inventing text.

## Response

`200 OK` -> `ApiResponse<AyahMutashabihatResponse>`.

```json
{
  "isSuccess": true,
  "message": "تم تحميل المتشابهات اللفظية",
  "data": {
    "verseKey": "2:25",
    "groupCount": 1,
    "groups": [
      {
        "groupKey": "mutashabihat:1234",
        "sourceGroupId": 1234,
        "representativeVerseKey": "2:25",
        "representativeWordFrom": 3,
        "representativeWordTo": 6,
        "phraseTextUthmani": "...",
        "occurrenceCount": 5,
        "distinctAyahCount": 5,
        "distinctSurahCount": 3,
        "selectedOccurrences": [
          {
            "verseKey": "2:25",
            "wordFrom": 3,
            "wordTo": 6,
            "isRepresentative": true,
            "phraseTextUthmani": "..."
          }
        ],
        "occurrences": [
          {
            "verseKey": "2:25",
            "surahNumber": 2,
            "surahNameArabic": "البقرة",
            "ayahNumber": 25,
            "pageNumber": 5,
            "wordFrom": 3,
            "wordTo": 6,
            "isSelectedAyah": true,
            "isRepresentative": true,
            "textUthmani": "...",
            "phraseTextUthmani": "..."
          }
        ]
      }
    ]
  }
}
```

## Empty Response

```json
{
  "isSuccess": true,
  "message": "تم تحميل المتشابهات اللفظية",
  "data": {
    "verseKey": "2:25",
    "groupCount": 0,
    "groups": []
  }
}
```

Frontend empty label: `لا توجد متشابهات لفظية مسجلة لهذه الآية في البيانات الحالية.`

## Status Codes

| Status | When |
|---|---|
| `200 OK` | Selected ayah exists, including zero groups. |
| `400 Bad Request` | Malformed `verseKey`. |
| `404 Not Found` | Well-formed `verseKey` resolves to no ayah. |
| `500` | Unexpected error through global handler. |
