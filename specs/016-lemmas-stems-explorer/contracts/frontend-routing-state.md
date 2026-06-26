# Contract: Frontend Routing, State, and UI Behavior

## Routes

```text
/dashboard/words/lemmas
/dashboard/words/stems
```

- Add stable `lemmas` and `stems` child routes under the existing Words feature.
- Add route-path helpers and route tests.
- Update Words hub cards so `الصيغ المعجمية` and `الأصول الصرفية` are active links.
- Route paths and query keys are technical stable values; Arabic labels may evolve independently.

## Lemmas Query State

| Param | Values | Default |
|---|---|---|
| `search` | Arabic display search | empty |
| `sort` | `mushaf-order`, `occurrences`, `alpha` | `mushaf-order` |
| `page` | positive integer | `1` |
| `lemma` | positive lemma ID | none |
| `view` | `words`, `ayahs`, `surahs`, `stems` | `words` |
| `wordView` | `simple`, `tashkeel` | `simple` |
| `surahView` | `mentioned`, `missing` | `mentioned` |
| `detailPage` | positive integer | `1` |

## Stems Query State

| Param | Values | Default |
|---|---|---|
| `search` | Arabic display search | empty |
| `sort` | `mushaf-order`, `occurrences`, `alpha` | `mushaf-order` |
| `page` | positive integer | `1` |
| `stem` | positive stem ID | none |
| `view` | `words`, `ayahs`, `surahs`, `lemmas` | `words` |
| `wordView` | `simple`, `tashkeel` | `simple` |
| `surahView` | `mentioned`, `missing` | `mentioned` |
| `detailPage` | positive integer | `1` |

Rules:

- Invalid sort/view/sub-view defaults safely.
- Non-positive or malformed pages default to 1.
- A valid positive catalogue or detail page beyond the available results remains in URL state and
  renders the successful empty-page state returned by the API.
- View/sub-view/detail page is meaningful only with a valid positive selection ID.
- `wordView` applies only to words; `surahView` only to surahs; `detailPage` only to words/ayahs.
- Search or sort changes reset only the catalogue page to 1 and preserve the selected identity plus
  its active detail view, sub-view, and detail page.
- Clearing selection clears identity and all detail keys while preserving search/sort/list page.
- Unknown selected identity shows panel not-found and does not break or repeatedly reload the list.
- Page sizes are fixed implementation constants, not URL state.

## Deep-Link Builders

```text
buildLemmasDeepLink({ lemmaId, view, wordView, ... })
buildStemsDeepLink({ stemId, view, wordView, ... })
```

Return the existing deep-link target shape `{ path, queryParams }` and convert with
`deepLinkToHref` when rendering anchors.

Canonical default destination:

```text
/dashboard/words/lemmas?lemma={id}&view=words&wordView=simple
/dashboard/words/stems?stem={id}&view=words&wordView=simple
```

## Table and Selection Behavior

### Lemmas columns

`الصيغة المعجمية`, `الجذر`, `النوع`, `المواضع`, `الآيات`, `السور`,
`كلمات بدون تشكيل`, `كلمات بالتشكيل`, `الأصول الصرفية`.

Count mapping:

| Count | Destination |
|---|---|
| المواضع / الآيات | `view=ayahs&detailPage=1` |
| السور | `view=surahs&surahView=mentioned` |
| كلمات بدون تشكيل | `view=words&wordView=simple&detailPage=1` |
| كلمات بالتشكيل | `view=words&wordView=tashkeel&detailPage=1` |
| الأصول الصرفية | `view=stems` |
| Row selection | `view=words&wordView=simple&detailPage=1` |

### Stems columns

`الأصل الصرفي`, `الصيغة المعجمية`, `الجذر`, `النوع`, `المواضع`, `الآيات`,
`السور`, `كلمات بدون تشكيل`, `كلمات بالتشكيل`.

Count mapping:

| Count | Destination |
|---|---|
| المواضع / الآيات | `view=ayahs&detailPage=1` |
| السور | `view=surahs&surahView=mentioned` |
| كلمات بدون تشكيل | `view=words&wordView=simple&detailPage=1` |
| كلمات بالتشكيل | `view=words&wordView=tashkeel&detailPage=1` |
| Row selection | `view=words&wordView=simple&detailPage=1` |

Zero counts remain keyboard-operable and open a clear empty detail state.

## Details Panels

Lemmas tabs:

- الكلمات: بدون تشكيل / بالتشكيل
- الآيات
- السور: وردت فيها / لم ترد فيها
- الأصول الصرفية

Stems tabs:

- الكلمات: بدون تشكيل / بالتشكيل
- الآيات
- السور: وردت فيها / لم ترد فيها
- الصيغ المعجمية

The selection summary/header exposes dominant type, additional-types indicator, and full type
distribution without adding an overview tab.

Desktop uses the implemented Roots split view with an independently scrolling inline-end panel.
Narrow screens use the existing dismissible overlay/drawer adaptation with Escape, focus trap, and
focus return. Quran text is never animated.

## Data Loading and State Ownership

```text
Routeable page → list/detail facade → resource API service → backend
```

- Page shells parse route state and delegate.
- List facade owns search debounce, sort/page, catalogue load, selection restoration, loading/error/no-results.
- Detail facade owns selected summary, active view/sub-view, detail pagination, per-session cache, not-found.
- Child components emit events and never call APIs.
- Only active detail view loads.
- List render must not call any detail endpoint.
- Reuse loaded data for the same selected identity/view/page when safe.
- Split loader/update helpers when facades approach repository size thresholds.

## Shared Component Reuse

Reuse existing:

- `highlighted-ayah`
- `ayah-matches-list`
- `surah-occurrences-list`
- `missing-surahs-list`
- `word-count-chip`
- shared `qd-pagination`
- Roots split-panel/table styling patterns
- API response cache/deep-link href utilities

Create resource-specific table/panel/word/related-item components where semantics differ. A shared
type-distribution component is valid because the same controlled model and behavior serve both pages.

## Cross-Page Anchors

All of these are real anchors with `target="_blank"` and `rel="noopener noreferrer"`:

| Source | Destination |
|---|---|
| Lemma/stem root | Roots Explorer selected root, words/simple |
| Lemma related stem | Stems Explorer selected stem, words/simple |
| Stem related lemma | Lemmas Explorer selected lemma, words/simple |
| Simple/tashkeel word | Matching Unique Words mode, selected word, ayahs |
| Ayah result | Mushaf page + ayah/focusAyah + ayah panel |
| Mushaf root/lemma/stem | Matching explorer selected identity, words/simple |

Missing identity means non-clickable display. Never construct these URLs from display text.

## Mushaf Integration

Frontend morphology models become:

```ts
lemma: { id: number; text: string | null; buckwalter: string | null } | null;
stem: { id: number; text: string | null } | null;
```

`SelectedWordSectionComponent` computes lemma/stem explorer hrefs from stable IDs. The morphology
summary receives hrefs and renders anchors only when non-empty, preserving existing root and
unique-word behavior.

## Accessibility and RTL

- Count cells are real buttons with descriptive Arabic accessible names.
- Rows expose selected state beyond color.
- Tabs/sub-tabs use tablist semantics and RTL-aware keyboard behavior.
- New-tab anchors communicate meaningful destination; focus remains visible.
- Panel loading uses a polite live status; error/empty/not-found states are explicit.
- Use logical CSS properties and existing `qd-*` tokens/classes.
- No technical IDs appear as visible labels.

## Required Frontend Tests

- Route helper and route registration.
- Lemma/stem URL parse/build/normalize/clear/deep-link.
- Catalogue search/sort/page, selection preservation during search/sort, and no eager detail calls.
- Malformed/non-positive page normalization and controlled positive out-of-range empty pages.
- Row and count destination mapping.
- Invalid/unknown selection behavior.
- Lazy view loading and same-selection cache reuse.
- Exact anchor href plus `target`/`rel`.
- Missing relationship non-link fallback.
- Mushaf morphology ID model and generated links.
- Highlight payload pass-through and pagination.
- Mentioned + missing surah behavior.
- Keyboard/tab/selected-state semantics.
- Narrow-screen environment guards for `matchMedia`/`ResizeObserver`.
