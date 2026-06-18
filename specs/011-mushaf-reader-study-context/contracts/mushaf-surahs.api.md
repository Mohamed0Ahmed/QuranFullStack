# Contract: Mushaf Surah Catalog API

## Purpose

Supports **FR-017 jump-by-surah** navigation in the Mushaf reader header. The frontend loads this catalog once, populates the surah selector, and navigates to `startPageNumber` when the user picks a surah. Added during **Phase 4 (US1)** engineering review to satisfy FR-017 without waiting for later URL-state phases.

## Endpoint

```
GET /api/mushaf/surahs
```

- No path parameters, query parameters, or body. Read-only.
- Auth: none in v1 (consistent with existing dashboard endpoints).

## Response

`200 OK` → `ApiResponse<MushafSurahCatalogResponse>` (`IsSuccess=true`, Arabic `message`, `data` = `MushafSurahCatalogResponse`).

`MushafSurahCatalogResponse` shape:

| Field | Type | Notes |
|---|---|---|
| `surahs` | `MushafSurahCatalogItem[]` | Ordered by `surahNumber` ascending |

`MushafSurahCatalogItem`:

| Field | Type | Source |
|---|---|---|
| `surahNumber` | int | `quran_surahs.surah_number` |
| `nameArabic` | string | `quran_surahs.name_arabic` |
| `startPageNumber` | int | `page_from` of ayah 1 when present; otherwise minimum `page_from` for that surah. Surahs with no ayah rows are omitted. |

### Example (abridged)

```json
{
  "isSuccess": true,
  "message": "تم تحميل فهرس السور",
  "data": {
    "surahs": [
      { "surahNumber": 1, "nameArabic": "الفاتحة", "startPageNumber": 1 },
      { "surahNumber": 2, "nameArabic": "البقرة", "startPageNumber": 2 }
    ]
  }
}
```

## Status codes & errors

| Status | When | `message` |
|---|---|---|
| `200 OK` | catalog loaded | `MushafSurahCatalogLoaded` (Arabic via `ApiMessages`) |
| `500` | unexpected (via `GlobalExceptionHandler`) | safe generic |

No `400`/`404` in v1 — the catalog is a full read over `quran_surahs` joined to ayah start pages.

Failure shape: `ApiResponse` with `isSuccess:false`, Arabic `message`, `errors:[]`. No stack traces / SQL / file paths.

## Rules

- Thin controller: bind → call `GetMushafSurahCatalogQuery` → map to `ApiResponse<T>`. No EF/logic in the controller.
- Read-only; never invent surah names or page numbers.
- `startPageNumber` must stay within Mushaf bounds [1,604] on a fully seeded database.
- Frontend: load once per reader session; jump updates the existing `page` URL query param (no new URL keys).
