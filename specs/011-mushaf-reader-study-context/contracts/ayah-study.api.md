# Contract: Selected Ayah Study API

## Endpoint

```
GET /api/mushaf/ayahs/{verseKey}/study?tafsirSource=...&translationSource=...&fullI3rabSource=...
```

- `verseKey` (path, string, required): natural key `surah:ayah` (e.g., `2:25`).
- `tafsirSource` (query, string, optional): source key (e.g., `ar-muyassar`).
- `translationSource` (query, string, optional): source key (e.g., `en-sahih-international`).
- `fullI3rabSource` (query, string, optional): source key (e.g., `muyassar`).
- No body. Read-only. Lazy: called only when an ayah is selected.

## Behavior (locked)

- Returns the **three sources together** in one response: tafsir + translation + full i3rab.
- Source resolution per kind: **explicit query param → configured default → empty**.
  Configured defaults: `MushafReader:DefaultTafsirSourceKey=ar-muyassar`,
  `MushafReader:DefaultTranslationSourceKey=en-sahih-international`,
  `MushafReader:DefaultFullI3rabSourceKey=muyassar`.
- A missing/unknown source for a kind → that kind's block is `null` with a per-kind empty state; the other kinds still load. **No silent substitution.**
- `selectedSources` echoes the **resolved** key actually used per kind (or null).
- For grouped/ranged tafsir/full-i3rab entries, include `isGroupLeader`, `sourceValueKind`, `sourceLeaderVerseKey`, `coveredAyahCount`, `coveredAyahKeys`.

## Response

`200 OK` → `ApiResponse<AyahStudyResponse>` (see data-model.md §B2).

### Example (abridged)

```json
{
  "isSuccess": true,
  "message": "تم تحميل دراسة الآية",
  "data": {
    "ayah": {
      "verseKey": "2:25", "surahNumber": 2, "surahNameArabic": "البقرة", "ayahNumber": 25,
      "textUthmani": "...", "wordsCount": 34, "pageFrom": 5, "pageTo": 5,
      "juzNumber": 1, "hizbNumber": 1, "rubNumber": 1, "sajda": null
    },
    "selectedSources": { "tafsirSource": "ar-muyassar", "translationSource": "en-sahih-international", "fullI3rabSource": "muyassar" },
    "tafsir": { "sourceKey": "ar-muyassar", "displayNameAr": "التفسير الميسر", "tafsirKind": "brief", "sourceValueKind": "flat", "isGroupLeader": false, "coveredAyahCount": 1, "coveredAyahKeys": ["2:25"], "text": "..." },
    "translation": { "sourceKey": "en-sahih-international", "displayNameEn": "Saheeh International", "languageCode": "en", "direction": "ltr", "translationType": "...", "containsHtmlMarkup": false, "text": "..." },
    "fullI3rab": { "sourceKey": "muyassar", "displayNameAr": "الإعراب الميسّر", "markupFormat": "html", "sourceValueKind": "flat", "isGroupLeader": false, "coveredAyahCount": 1, "coveredAyahKeys": ["2:25"], "html": "..." }
  }
}
```

### Missing-source example

If `fullI3rabSource=does-not-exist` (and no usable default), `data.fullI3rab` is `null`, `data.selectedSources.fullI3rabSource` is `null`, and `tafsir`/`translation` still load. The UI shows a calm empty state for the full-i3rab card only.

## Status codes & errors

| Status | When | `message` key |
|---|---|---|
| `200 OK` | ayah resolves (even if some source blocks are null) | `MushafAyahs.StudyLoaded` |
| `400 Bad Request` | malformed `verseKey` | `MushafAyahs.InvalidVerseKey` |
| `404 Not Found` | `verseKey` resolves to no ayah | `Common.NotFound` |
| `500` | unexpected | safe generic |

## Rules

- Load only the one selected/default source per kind — never all sources.
- HTML (`fullI3rab.html`, any markup in tafsir/translation) is returned **unmodified**; sanitization happens at render time on the frontend.
- Thin controller → `GetAyahStudyQuery` → `ApiResponse<T>`.
