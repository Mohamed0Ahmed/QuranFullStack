# Contract: Selected Word Analysis API

## Endpoint

```
GET /api/mushaf/words/{wordLocation}/analysis
```

- `wordLocation` (path, string, required): natural key `surah:ayah:word` (e.g., `2:25:3`).
- No query parameters. No body. Read-only. Lazy: called only when a readable word is selected.

## Behavior

- Resolves the word by `location`. **Ayah-end marker rows (`is_ayah_marker = true`) are rejected** as not analyzable.
- Returns occurrence identity + display forms, ordered/unique identity counts, head morphology, and ordered segments with a stable `segmentColorSlot` per segment.
- **Incomplete analysis**: if the location resolves to a readable word but required morphology, ordered/unique identity, or segment rows are missing, return a controlled failure (`404` / `MushafWords.AnalysisIncomplete`). Do **not** synthesize zero counts or empty morphology in a `200` response.
- **Segment fallback**: if a segment has no display form (`form_arabic_normalized` empty/null), return the segment with `displayTextStatus:"missing"` and no invented text; the full word is preserved by the frontend from `textUthmani`.

## Response

`200 OK` → `ApiResponse<WordAnalysisResponse>` (see data-model.md §B3).

### Example (abridged)

```json
{
  "isSuccess": true,
  "message": "تم تحميل تحليل الكلمة",
  "data": {
    "word": {
      "quranWordId": 360, "wordLocation": "2:25:3", "verseKey": "2:25",
      "surahNumber": 2, "ayahNumber": 25, "wordNumber": 3,
      "pageNumber": 5, "lineNumber": 1, "lineWordOrder": 3,
      "textUthmani": "...", "textUthmaniSimple": "...", "textImlaeiSimple": "...", "qpcGlyph": "..."
    },
    "identity": {
      "orderedTashkeel": { "occurrencesCount": 206, "ayahsCount": 201, "surahsCount": 54 },
      "orderedSimple": { "occurrencesCount": 263, "ayahsCount": 254, "surahsCount": 59 },
      "uniqueTashkeel": { "id": 90, "occurrencesCount": 206, "ayahsCount": 201, "surahsCount": 54 },
      "uniqueSimple": { "id": 82, "wordKeyImlaeiSimple": "...", "occurrencesCount": 263, "ayahsCount": 254, "surahsCount": 59 }
    },
    "morphology": {
      "headPos": "V", "headPosLabel": { "ar": "فعل", "en": "Verb" },
      "root": { "id": 42, "text": "...", "buckwalter": "Amn" }, "lemma": { "text": "...", "buckwalter": "'aAmana" }, "stem": { "text": "..." },
      "isVerb": true, "verbTense": "past", "verbVoice": "active", "caseFeature": null
    },
    "renderedWordSegments": [
      {
        "segmentLocation": "2:25:3:1", "segmentNumber": 1, "segmentColorSlot": 1, "segmentKind": "STEM",
        "segmentDisplayText": "...", "displayTextStatus": "available",
        "segmentPos": "V", "segmentPosLabel": { "ar": "فعل", "en": "Verb" },
        "segmentI3rabArabic": "فعل ماض", "i3rabRuleId": 18, "i3rabRuleSignature": "STEM:V:PERF:ACT:3MP",
        "i3rabRuleFamily": "V.PERF.ACT", "i3rabStatus": "approved",
        "segmentFeatures": { "raw": "...", "json": [] }
      }
    ]
  }
}
```

## Status codes & errors

| Status | When | `message` key |
|---|---|---|
| `200 OK` | readable word resolves | `MushafWords.AnalysisLoaded` |
| `400 Bad Request` | malformed `wordLocation`, or location is an ayah-end marker | `MushafWords.InvalidWordLocation` / `MushafWords.NotAnalyzable` |
| `404 Not Found` | location resolves to no word | `Common.NotFound` |
| `404 Not Found` | readable word exists but required morphology/identity/segment rows are missing | `MushafWords.AnalysisIncomplete` |
| `500` | unexpected | safe generic |

## Rules

- Segments ordered by `segmentNumber`; `segmentColorSlot` stable per segment for frontend color-linking (slot, not color).
- Segment colors are **visual-linking only**; the backend never emits semantic POS colors.
- Never reconstruct the whole word from segment forms; whole-word text is `textUthmani`.
- Thin controller → `GetWordAnalysisQuery` → `ApiResponse<T>`.
