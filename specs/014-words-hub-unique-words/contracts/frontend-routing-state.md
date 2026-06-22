# Contract: Frontend Routing, State, And UI Behavior

## Routes

```text
/dashboard/words
/dashboard/words/unique
/dashboard/words/unique/tashkeel
/dashboard/words/unique/simple
```

Rules:

- `/dashboard/words` renders the Words hub.
- `/dashboard/words/unique` redirects to `/dashboard/words/unique/tashkeel`.
- `tashkeel` and `simple` are stable route keys, not translated labels.
- The `words` navigation item points to `/dashboard/words`.
- The generic fallback route must not handle `words` after this feature is implemented.

## Query Params

List state:

| Param | Values | Default |
|---|---|---|
| `search` | Arabic text | empty |
| `sort` | `mushaf-order`, `occurrences`, `alpha` | `mushaf-order` |
| `page` | positive integer | `1` |

Modal drill-down state:

| Param | Values | Meaning |
|---|---|---|
| `word` | stable unique-word ID | Opens modal for selected word when valid. |
| `view` | `surahs`, `missing`, `ayahs` | Active modal view. |
| `ap` | positive integer | Ayah-match page when `view=ayahs`. |

Rules:

- Closing the modal clears `word`, `view`, and `ap` only.
- Closing the modal preserves route mode, `search`, `sort`, and list `page`.
- Invalid modal state produces a controlled Arabic state and keeps the list usable.
- Unknown route mode normalizes to `tashkeel` where safe.

## Data Loading

| UI state | Data behavior |
|---|---|
| Hub | No unique-word list or drill-down reads required. |
| Unique list open | Load one paged list for the active kind/search/sort/page. |
| Modal opened with `surahs` | Load summary if needed and mentioned-surahs data. |
| Modal opened with `missing` | Load summary if needed and missing-surahs data. |
| Modal opened with `ayahs` | Load summary if needed and paged ayah-match data. |

Rules:

- Child components do not call API services directly.
- API services return `ApiResponse<T>`.
- Facade/store maps `ApiResponse<T>` into page-ready loading, data, empty, and error state.
- Transport failures and backend `isSuccess === false` failures are both handled.

## Arabic Labels

| UI element | Arabic label |
|---|---|
| Hub active card | `الكلمات الفريدة` |
| Coming soon badge | `قريبًا` |
| Future card | `الجذور` |
| Future card | `الصيغة المعجمية` |
| Future card | `الأصل الصرفي` |
| Future card | `أنواع الكلمة` |
| Tashkeel mode | `بالتشكيل` |
| Simple mode | `إملائي (بدون تشكيل)` |
| Search label | `بحث` |
| Sort label | `ترتيب` |
| Count chip | `المواضع` |
| Count chip | `الآيات` |
| Count chip | `السور` |
| Count chip | `لم يذكر في` |
| Empty list | `لا توجد نتائج` |

## Highlighting Rules

- `highlighted-ayah` receives words and a set/list of matched word IDs.
- A word is highlighted only when its `quranWordId` is in `matchedQuranWordIds`.
- Do not perform string replacement or text-fragment matching.
- Highlighting must not rely on color alone; include a class/marker/accessible label.
- Quran text rendering and fonts remain stable and are not animated.
