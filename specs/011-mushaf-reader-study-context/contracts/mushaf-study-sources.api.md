# Contract: Mushaf Study Source Catalog API

## Purpose

Supports **FR-027** source switching in the selected-ayah study panel. The frontend loads this catalog once per reader session, populates the tafsir / translation / full-i3rab selectors, and passes the chosen `sourceKey` to `GET /api/mushaf/ayahs/{verseKey}/study` when the user switches a source.

Added as a Phase 5+ addendum (engineering gap closure): v1 ayah-study returns **one resolved source per kind** only; this endpoint returns **all** catalog rows for selection UI.

## Endpoint

```
GET /api/mushaf/study-sources
```

- No path parameters, query parameters, or body. Read-only.
- Auth: none in v1 (consistent with existing dashboard Mushaf endpoints).

## Response

`200 OK` → `ApiResponse<MushafStudySourceCatalogResponse>` (`IsSuccess=true`, Arabic `message`, `data` = `MushafStudySourceCatalogResponse`).

`MushafStudySourceCatalogResponse` shape:

| Field | Type | Source table |
|---|---|---|
| `tafsirSources` | `StudySourceCatalogItem[]` | `quran_tafsir_sources` |
| `translationSources` | `StudySourceCatalogItem[]` | `quran_translation_sources` |
| `fullI3rabSources` | `StudySourceCatalogItem[]` | `quran_full_i3rab_sources` |

`StudySourceCatalogItem` — flat list; one row per selectable `source_key`. **No ayah text.**

| Field | Type | DB column | Notes |
|---|---|---|---|
| `sourceKey` | string | `source_key` | Passed to ayah-study query params |
| `displayNameAr` | string | `display_name_ar` | Primary selector label (Arabic-first UI) |
| `displayNameEn` | string? | `display_name_en` | Optional English label |
| `languageCode` | string | `language_code` | Grouping / filtering |
| `languageNameAr` | string? | `language_name_ar` | UI `<optgroup>` label; null when not stored (e.g. some full-i3rab rows) |
| `direction` | string | `direction` | `rtl` or `ltr` |
| `tafsirKind` | string? | `tafsir_kind` | Tafsir rows only; otherwise null |
| `translationType` | string? | `translation_type` | Translation rows only: `simple` or `with_footnotes`; otherwise null |

### Ordering (locked)

| Kind | Order by |
|---|---|
| Tafsir | `language_name_ar`, `tafsir_kind`, `display_name_ar` |
| Translation | `language_name_ar`, `translation_type` (`simple` before `with_footnotes`), `display_name_ar` |
| Full i3rab | `display_name_ar` |

### Example (abridged)

```json
{
  "isSuccess": true,
  "message": "تم تحميل كتالوج مصادر الدراسة",
  "data": {
    "tafsirSources": [
      {
        "sourceKey": "ar-muyassar",
        "displayNameAr": "التفسير الميسر",
        "displayNameEn": "Muyassar Tafsir",
        "languageCode": "ar",
        "languageNameAr": "العربية",
        "direction": "rtl",
        "tafsirKind": "brief",
        "translationType": null
      }
    ],
    "translationSources": [
      {
        "sourceKey": "en-sahih-international",
        "displayNameAr": "صحيح الدولية",
        "displayNameEn": "Sahih International",
        "languageCode": "en",
        "languageNameAr": "الإنجليزية",
        "direction": "ltr",
        "tafsirKind": null,
        "translationType": "simple"
      }
    ],
    "fullI3rabSources": [
      {
        "sourceKey": "muyassar",
        "displayNameAr": "الإعراب الميسر",
        "displayNameEn": "Muyassar I3rab",
        "languageCode": "ar",
        "languageNameAr": null,
        "direction": "rtl",
        "tafsirKind": null,
        "translationType": null
      }
    ]
  }
}
```

## Status codes & errors

| Status | When | `message` |
|---|---|---|
| `200 OK` | catalog loaded | `MushafStudySourceCatalogLoaded` (Arabic via `ApiMessages`) |
| `500` | unexpected (via `GlobalExceptionHandler`) | safe generic |

No `400`/`404` in v1 — the catalog is a full read over the three source dimension tables.

Failure shape: `ApiResponse` with `isSuccess:false`, Arabic `message`, `errors:[]`. No stack traces / SQL / file paths.

## Rules

- Thin controller: bind → call `GetMushafStudySourceCatalogQuery` → map to `ApiResponse<T>`. No EF/logic in the controller.
- Read-only; never invent source names, keys, or language labels.
- Return metadata only — never include tafsir/translation/i3rab ayah text in this payload.
- Frontend: load once per reader session (alongside surah catalog); source switch updates existing URL query params (`tafsirSource`, `translationSource`, `fullI3rabSource`) and triggers ayah-study reload.
- `GET /api/mushaf/ayahs/{verseKey}/study` remains unchanged: one resolved source per kind, never the full catalog.
