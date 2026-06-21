# Contract: Selected Ayah Study Similarity Summary

## Endpoint

```text
GET /api/mushaf/ayahs/{verseKey}/study?tafsirSource=...&translationSource=...&fullI3rabSource=...
```

This is the existing selected ayah study endpoint. Feature 012 extends its response data with lightweight similarity counts only.

## Request

- `verseKey` (path, required): natural key `surah:ayah`, e.g. `2:25`.
- `tafsirSource` (query, optional): existing tafsir source key behavior unchanged.
- `translationSource` (query, optional): existing translation source key behavior unchanged.
- `fullI3rabSource` (query, optional): existing full i'rab source key behavior unchanged.
- No body. Read-only.

## Response Delta

`data.similaritySummary` is added:

```json
{
  "isSuccess": true,
  "message": "تم تحميل سياق دراسة الآية",
  "data": {
    "ayah": { "verseKey": "2:25" },
    "selectedSources": {},
    "tafsir": null,
    "translation": null,
    "fullI3rab": null,
    "similaritySummary": {
      "similarAyahCount": 4,
      "mutashabihatGroupCount": 2,
      "mutashabihatOccurrenceCount": 9
    }
  }
}
```

## Count Semantics

| Field | Meaning |
|---|---|
| `similarAyahCount` | Distinct related ayahs after combining incoming and outgoing directed similar links and deduplicating bidirectional rows. |
| `mutashabihatGroupCount` | Distinct mutashabihat groups containing the selected ayah. |
| `mutashabihatOccurrenceCount` | All occurrences across selected ayah's groups, including selected-ayah occurrences. |

## Rules

- Do not include full similar ayah items in this response.
- Do not include mutashabihat group details in this response.
- Zero counts are successful data, not errors.
- Do not add these fields to the Mushaf page response.
- Existing tafsir/translation/full-i'rab behavior remains unchanged.

## Status Codes

| Status | When |
|---|---|
| `200 OK` | Selected ayah resolves; some study source blocks may still be null per existing rules. |
| `400 Bad Request` | Malformed `verseKey`. |
| `404 Not Found` | Well-formed `verseKey` resolves to no ayah. |
| `500` | Unexpected error through global handler. |
