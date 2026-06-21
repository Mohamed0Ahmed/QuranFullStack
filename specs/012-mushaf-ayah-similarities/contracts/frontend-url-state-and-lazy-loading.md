# Contract: Frontend URL State And Lazy Loading

## URL State

Extend the existing selected ayah action/tab state.

Accepted `ayahTab` values:

- `tafsir`
- `translation`
- `full-i3rab`
- `similar-ayahs`
- `mutashabihat`

Rules:

- Unknown values normalize to the existing default selected ayah tab.
- URL restoration must preserve selected page, selected ayah, and active ayah tab.
- Do not introduce a separate `ayahAction` URL key unless implementation discovers a concrete conflict with the existing tab model.

## Lazy Loading

| Active tab | Data behavior |
|---|---|
| `tafsir` | Uses selected ayah study payload. |
| `translation` | Uses selected ayah study payload. |
| `full-i3rab` | Uses selected ayah study payload. |
| `similar-ayahs` | Loads flat similar ayahs detail if not cached/in-flight. |
| `mutashabihat` | Loads grouped mutashabihat detail if not cached/in-flight. |

Rules:

- Initial Mushaf page load does not request similarity counts or details.
- Selecting an ayah requests selected ayah study and receives `similaritySummary` counts.
- Selecting an ayah does not automatically request similar ayah details or mutashabihat details.
- Opening a URL with `ayahTab=similar-ayahs` or `ayahTab=mutashabihat` may trigger the corresponding lazy detail request after the selected ayah is known.
- Loading, empty, and error states are scoped to the active detail action.

## Arabic UI Labels

| State | Label |
|---|---|
| Similar action | `آيات قريبة في المعنى` |
| Similar short tab | `آيات قريبة` |
| Mutashabihat action | `المتشابهات اللفظية للحفظ` |
| Mutashabihat short tab | `المتشابهات` |
| Similar empty | `لا توجد آيات قريبة في المعنى لهذه الآية في البيانات الحالية.` |
| Mutashabihat empty | `لا توجد متشابهات لفظية مسجلة لهذه الآية في البيانات الحالية.` |
| Similar loading | `جارٍ تحميل الآيات القريبة...` |
| Mutashabihat loading | `جارٍ تحميل المتشابهات اللفظية...` |

## Rendering Rules

- Similar ayahs render as one flat list.
- Mutashabihat render as group sections/cards.
- Selected ayah occurrence in mutashabihat must have a text/icon/label marker, not color alone.
- Long content scrolls inside the selected ayah study area without breaking the Mushaf page layout.
