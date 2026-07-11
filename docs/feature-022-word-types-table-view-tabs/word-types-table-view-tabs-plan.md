# Word Types Explorer — Table View Tabs Plan

Branch: current working branch
Scope: **planning only.** No implementation code is included in this document.

Status: plan only — this document describes how to add table-level view tabs to the existing Word Types Explorer.

---

## 1. Executive summary

The Word Types Explorer currently shows a paginated table of individual word rows for the selected word-type filters. The requested feature is to add a tab row above the table so the same filtered scope can be viewed as one of four table views:

- **Words** — current behavior; show every matching word/context row.
- **Roots** — group the matching scope by root.
- **Stems** — group the matching scope by stem/form.
- **Lemmas** — group the matching scope by lemma/origin.

The feature should not be implemented as frontend-only grouping. Because the table is paginated and sorted, grouping only the currently loaded frontend page would produce incorrect counts, ordering, and pagination. The grouped views must be backed by the API/read model so counts and pagination represent the full filtered result set.

Recommended URL parameter:

```txt
tableView=words|roots|stems|lemmas
```

Do not use `view` for this feature because the Word Types Explorer already uses `view` for the details panel (`ayahs|surahs`).

---

## 2. Existing architecture context

The words feature is organized as separate explorers for roots, lemmas, stems, word types, and unique words. Each explorer follows the same pattern: routed page component, facade/detail facade, cache, URL sync, API service, models/labels, and mappers.

The Word Types Explorer currently has:

- Route/page shell: `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/`
- Filter + table components under: `Frontend/quran-dashboard-ui/src/app/features/words/components/`
- API client: `Frontend/quran-dashboard-ui/src/app/features/words/data-access/word-types.api.ts`
- List state/facade/cache/url-sync under: `Frontend/quran-dashboard-ui/src/app/features/words/state/`
- Models/labels: `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.*`

Important invariants:

- URL state is a user-facing shareable contract.
- Existing deep links without `tableView` must continue to work and default to `words`.
- Word identity is clean imlaei-simple for matching/search identity while display stays Uthmani where applicable.
- Quranic data must not be invented or silently altered.

---

## 3. Product behavior

### 3.1 User story

As a curator using the Word Types Explorer, after selecting a word type/subtype such as a verb tense or particle subtype, I want to switch the table between individual words, roots, stems, and lemmas so I can understand the selected grammatical scope at different levels of aggregation.

### 3.2 Tabs

Add tabs above the table:

```txt
Words | Roots | Stems | Lemmas
```

Arabic UI labels should be:

```txt
كلمات | جذور | صيغ | أصول
```

Code names should stay aligned with the existing explorer vocabulary:

```ts
'words' | 'roots' | 'stems' | 'lemmas'
```

### 3.3 Tab semantics

| Tab | Meaning | Table grain |
| --- | --- | --- |
| Words | Current behavior | One row per matching word/context |
| Roots | Group current filtered scope by root | One row per root |
| Stems | Group current filtered scope by stem/form | One row per stem |
| Lemmas | Group current filtered scope by lemma/origin | One row per lemma |

The active tab applies **inside** the current Word Types filter scope:

- Main type (`noun`, `verb`, `particle`, `inl`)
- Child subtype (`childCode`)
- Case filter for nouns
- Tense/voice filters for verbs
- Sort
- Page/page size

### 3.4 Selection behavior

MVP behavior:

- Changing `tableView` resets page to `1`.
- Changing `tableView` clears any selected row/detail panel.
- The details panel remains available for word rows only in the first implementation phase.
- Grouped row details for roots/stems/lemmas are a follow-up phase.

Reason: the existing details panel identity is word-row based (`tashkeelWordId`, `contextCode`, case/tense/voice). Grouped rows need a different identity contract.

---

## 4. Recommended implementation phases

## Phase 0 — Confirm backend/read-model capability

Before frontend implementation, inspect the backend Word Types read model and decide whether to extend the existing rows endpoint or create a new endpoint.

Search commands:

```bash
rg "word-types" Backend
rg "WordTypes" Backend
rg "WordType" Backend/application Backend/infrastructure
```

Decision point:

### Option A — Extend existing endpoint

Extend:

```txt
GET /api/words/word-types/words
```

with:

```txt
tableView=words|roots|stems|lemmas
```

Pros:

- Smaller frontend API change.
- Existing facade can keep calling `getRows()`.

Cons:

- Endpoint name `/words` becomes semantically odd when returning roots/stems/lemmas.
- The row DTO may become too broad if word rows and grouped rows differ significantly.

### Option B — Add a table endpoint (recommended)

Add:

```txt
GET /api/words/word-types/table
```

with:

```txt
type=noun|verb|particle|inl
childCode=...
case=...
tense=...
voice=...
tableView=words|roots|stems|lemmas
sort=occurrences|ayahs|surahs|mushaf-order|alpha
page=1
pageSize=25
```

Pros:

- Semantically clear.
- Allows a row DTO designed for both word and grouped rows.
- Easier to evolve later for grouped-row details.

Cons:

- Slightly larger API/controller/read-model change.

Recommended choice: **Option B** if this feature is expected to grow; **Option A** only for a very small MVP.

---

## Phase 1 — Backend contract and query semantics

### 1. Add table view enum/request value

Create or extend backend request parsing to accept:

```txt
words
roots
stems
lemmas
```

Validation rules:

- Unknown values should return a controlled validation failure.
- Missing value defaults to `words`.
- Existing filters remain validated exactly as today.

### 2. Return paged grouped rows

Use a paged response shape compatible with the current frontend `PagedResultDto<T>`:

```ts
interface WordTypeTableRowDto {
  id: string;
  tableView: 'words' | 'roots' | 'stems' | 'lemmas';
  displayText: string;

  typeCode: string | null;
  typeLabel: { ar: string } | null;
  broadLabel: { ar: string } | null;
  caseOrFeature: string | null;

  rootText: string | null;
  lemmaText: string | null;
  stemText: string | null;

  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;

  // Only populated for tableView=words in MVP.
  tashkeelWordId: number | null;
  contextCode: string | null;
}
```

Prefer stable IDs over display text:

- Root rows: root ID/code if available; otherwise normalized root text as a fallback.
- Stem rows: stem ID/code if available; otherwise normalized stem text as a fallback.
- Lemma rows: lemma ID/code if available; otherwise normalized lemma text as a fallback.
- Word rows: existing word identity.

### 3. Grouping rules

For every `tableView`, first apply the current Word Types scope filters, then aggregate.

#### Words

Current behavior.

#### Roots

Group matching rows by root.

- Exclude or explicitly bucket rows with missing root according to product decision.
- Recommended MVP: include missing root as a controlled “No root” row only if the source data genuinely has such rows and the backend can label it safely without inventing Quranic content.

#### Stems

Group matching rows by stem/form.

#### Lemmas

Group matching rows by lemma/origin.

### 4. Count definitions

For each row:

- `occurrencesCount`: total occurrences for that group inside the selected Word Types scope.
- `ayahsCount`: distinct ayahs containing occurrences for that group inside the selected scope.
- `surahsCount`: distinct surahs containing occurrences for that group inside the selected scope.

### 5. Sort definitions

Keep existing sort options if possible:

| Sort | Grouped-view meaning |
| --- | --- |
| `occurrences` | Descending occurrence count |
| `ayahs` | Descending distinct ayah count |
| `surahs` | Descending distinct surah count |
| `mushaf-order` | First occurrence of the group in mushaf order |
| `alpha` | Arabic lexical ordering on the grouped display text |

If any sort cannot be implemented correctly for grouped rows, block the implementation and document the limitation instead of silently approximating.

---

## Phase 2 — Frontend model and URL state

File:

```txt
Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.models.ts
```

Add:

```ts
export type WordTypeTableView = 'words' | 'roots' | 'stems' | 'lemmas';

export const WORD_TYPE_TABLE_VIEWS = [
  'words',
  'roots',
  'stems',
  'lemmas',
] as const satisfies readonly WordTypeTableView[];

export const DEFAULT_WORD_TYPE_TABLE_VIEW: WordTypeTableView = 'words';

export function isWordTypeTableView(value: unknown): value is WordTypeTableView {
  return (WORD_TYPE_TABLE_VIEWS as readonly string[]).includes(value as string);
}
```

Extend `ParsedWordTypesQuery`:

```ts
tableView: WordTypeTableView;
```

Extend `WORD_TYPES_QUERY_KEYS`:

```ts
tableView: 'tableView'
```

If a new row DTO is needed, add it separately rather than overloading `WordTypeRowDto` too heavily:

```ts
export interface WordTypeTableRowDto {
  id: string;
  tableView: WordTypeTableView;
  displayText: string;
  typeCode: string | null;
  typeLabel: WordTypeLabelDto | null;
  broadLabel: WordTypeLabelDto | null;
  caseOrFeature: string | null;
  rootText: string | null;
  lemmaText: string | null;
  stemText: string | null;
  occurrencesCount: number;
  ayahsCount: number;
  surahsCount: number;
  tashkeelWordId: number | null;
  contextCode: string | null;
}
```

---

## Phase 3 — Frontend URL sync

File:

```txt
Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.ts
```

Update parsing:

```ts
tableView: normalizeTableView(queryParams.get(WORD_TYPES_QUERY_KEYS.tableView)),
```

Update `WordTypesQueryChange`:

```ts
tableView: WordTypeTableView | null;
```

Update `WORD_TYPES_QUERY_ORDER` to include `tableView`, preferably near the primary list scope params:

```ts
const WORD_TYPES_QUERY_ORDER = [
  'type',
  'childCode',
  'tableView',
  'case',
  'tense',
  'voice',
  'sort',
  'page',
  'word',
  'contextCode',
  'view',
  'detailPage',
  'location',
  'column',
] as const;
```

Add normalizer:

```ts
function normalizeTableView(value: string | null): WordTypeTableView {
  return value !== null && isWordTypeTableView(value)
    ? value
    : DEFAULT_WORD_TYPE_TABLE_VIEW;
}
```

Test cases to add/update:

- Missing `tableView` defaults to `words`.
- Invalid `tableView` defaults to `words`.
- Valid values round-trip through `buildWordTypesQueryParams()`.
- Changing table view can clear selection keys through facade behavior.
- Existing URLs without the new param keep working.

---

## Phase 4 — Frontend API, cache, and facade

### 4.1 API service

File:

```txt
Frontend/quran-dashboard-ui/src/app/features/words/data-access/word-types.api.ts
```

Add `tableView` to row request options:

```ts
tableView: WordTypeTableView;
```

Send it as a query param:

```ts
params = params.set('tableView', options.tableView);
```

If a new backend endpoint is chosen, add a dedicated method:

```ts
getTableRows(options: WordTypeTableRowsRequest): Observable<ApiResponse<PagedResultDto<WordTypeTableRowDto>>>;
```

### 4.2 Cache

File:

```txt
Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-cache.ts
```

Ensure `tableView` is included in the row cache key.

Reason: without this, switching from Words to Roots could reuse stale rows from the previous view.

### 4.3 Facade

File:

```txt
Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.ts
```

Extend default query:

```ts
tableView: DEFAULT_WORD_TYPE_TABLE_VIEW,
```

Add method:

```ts
selectTableView(tableView: WordTypeTableView): void {
  this.navigate({
    ...buildWordTypesQueryParams({
      tableView,
      page: DEFAULT_WORD_TYPES_PAGE,
    }),
    ...clearWordTypesSelection(),
  });
}
```

Update request key and API calls so `tableView` triggers a reload.

Recommended behavior:

- Changing table view resets page to 1.
- Changing table view clears current selection.
- Loading/error/empty state behavior remains consistent with the current list behavior.

---

## Phase 5 — Tabs UI component

Create:

```txt
Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-table-view-tabs/
  word-type-table-view-tabs.component.ts
  word-type-table-view-tabs.component.html
  word-type-table-view-tabs.component.scss
  word-type-table-view-tabs.component.spec.ts
```

Component contract:

```ts
@Component({
  selector: 'qd-word-type-table-view-tabs',
  standalone: true,
  templateUrl: './word-type-table-view-tabs.component.html',
  styleUrl: './word-type-table-view-tabs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeTableViewTabsComponent {
  readonly selectedView = input.required<WordTypeTableView>();
  readonly disabled = input(false);
  readonly viewSelected = output<WordTypeTableView>();
}
```

Suggested labels should live in `word-types.labels.ts` using the existing labels/getter pattern:

```ts
export const WORD_TYPE_TABLE_VIEW_OPTIONS = [
  { value: 'words', label: 'كلمات' },
  { value: 'roots', label: 'جذور' },
  { value: 'stems', label: 'صيغ' },
  { value: 'lemmas', label: 'أصول' },
] as const;
```

Accessibility:

- Use `role="tablist"` for the container.
- Use buttons with `role="tab"`.
- Set `aria-selected` for the active tab.
- Disable interactions while loading only if necessary; otherwise allow switching and let the facade reload.
- Keep RTL visual order natural: Words, Roots, Stems, Lemmas as Arabic reads right-to-left in the UI.

Styling:

- Use existing `--qd-*` tokens.
- Keep the tabs calm and parchment/navy/gold aligned.
- Active tab may use muted gold accent sparingly.
- Do not introduce a new global style system for this component.

---

## Phase 6 — Page integration

Files:

```txt
Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts
Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.html
Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.scss
```

Update imports in the page component to include the new tabs component.

Add handler:

```ts
protected selectTableView(tableView: WordTypeTableView): void {
  this.explorerFacade.selectTableView(tableView);
}
```

Place the tabs above the table, preferably between the filters and the table/sort controls:

```html
<qd-word-type-table-view-tabs
  [selectedView]="listState().query.tableView"
  [disabled]="listState().status === 'loading'"
  (viewSelected)="selectTableView($event)"
/>
```

Design note:

- The tabs should feel like a table view switcher, not another taxonomy filter.
- Keep them visually lighter than the main word-type filter cards.
- They should sit close to the table because they control the table grain.

---

## Phase 7 — Table rendering

Files:

```txt
Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.ts
Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.html
Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.scss
```

Add input:

```ts
readonly tableView = input<WordTypeTableView>('words');
```

Recommended columns:

### Words

Keep current columns:

- Word
- Type
- Root
- Stem
- Lemma
- Occurrences
- Ayahs
- Surahs

### Roots

- Root
- Type/scope label, if useful
- Occurrences
- Ayahs
- Surahs

Optional later:

- Distinct words
- Distinct stems
- Distinct lemmas

### Stems

- Stem
- Root
- Type/scope label, if useful
- Occurrences
- Ayahs
- Surahs

### Lemmas

- Lemma
- Root
- Stem, if useful and not misleading
- Occurrences
- Ayahs
- Surahs

Important rendering rules:

- Do not fabricate missing root/stem/lemma text.
- Use a controlled empty dash/label for missing values.
- Counts should remain clickable only where the detail action is actually supported.
- In MVP, row selection/detail actions should either be limited to `words` or handled as disabled/placeholder for grouped views.

---

## Phase 8 — Details panel follow-up plan

This is intentionally out of MVP unless explicitly requested.

To support details for grouped rows later, add a new selected identity model:

```ts
type WordTypeSelectedTableRow =
  | { tableView: 'words'; tashkeelWordId: number; contextCode: string; case: WordTypeCase; tense: WordTypeTense; voice: WordTypeVoice }
  | { tableView: 'roots'; rootId: string }
  | { tableView: 'stems'; stemId: string }
  | { tableView: 'lemmas'; lemmaId: string };
```

URL params might become:

```txt
selectedKind=word|root|stem|lemma
selectedId=...
```

or:

```txt
group=...
groupKind=roots|stems|lemmas
```

This should be designed carefully because it changes the shareable URL contract.

---

## Phase 9 — Documentation updates

Update:

```txt
Frontend/quran-dashboard-ui/src/app/features/words/README.md
```

Document:

- Word Types Explorer now has table view tabs.
- `tableView` is part of the URL-state contract.
- Default is `words` for old deep links.
- Grouped views are backend-backed and paginated.
- MVP details panel is word-row based unless grouped details are implemented.

If backend API contracts/specs exist for Word Types Explorer, update those too.

---

## Phase 10 — Tests and checks

### Frontend unit tests

Run/update targeted tests:

```bash
cd Frontend/quran-dashboard-ui
npm test -- --run src/app/features/words/state/word-types-url-sync.spec.ts
npm test -- --run src/app/features/words/state/word-types-explorer.facade*.spec.ts
npm test -- --run src/app/features/words/components/word-types-table/word-types-table.component.spec.ts
npm test -- --run src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts
```

Add tests for the new tabs component:

```bash
npm test -- --run src/app/features/words/components/word-type-table-view-tabs/word-type-table-view-tabs.component.spec.ts
```

### Frontend build

```bash
cd Frontend/quran-dashboard-ui
ng build
```

### Backend tests/build if backend changes are made

```bash
dotnet build
```

Run any existing backend Word Types tests after finding them:

```bash
rg "WordTypes" Backend/tests
```

Then run the relevant test project/filter.

---

## 11. Acceptance criteria

MVP is complete when:

1. The Word Types Explorer displays table tabs above the results table:
   - Words
   - Roots
   - Stems
   - Lemmas
2. The active tab is reflected in URL state as `tableView`.
3. Existing URLs without `tableView` still default to `words`.
4. Changing the tab:
   - resets page to 1;
   - clears the current selection/detail panel;
   - reloads rows for the active table view;
   - does not lose the selected word-type filters.
5. Grouped views are backed by backend/read-model aggregation, not frontend-only grouping.
6. Pagination totals represent the full grouped result set.
7. Counts are correct for the selected scope.
8. Sorting is correct for each table view or explicitly blocked if a sort cannot be correctly supported.
9. Missing root/stem/lemma values are displayed as controlled missing states, not invented text.
10. Tests cover URL parsing/building, facade tab selection behavior, tabs UI, and table rendering differences.
11. The words feature README documents the new URL contract and behavior.

---

## 12. Risks and mitigations

### Risk: incorrect grouped pagination

Mitigation: perform grouping and total counting in backend SQL/read model before pagination.

### Risk: stale cache rows across tabs

Mitigation: include `tableView` in row cache keys.

### Risk: confusing detail panel behavior

Mitigation: MVP clears detail selection on tab switch and only supports word-row details until grouped details are explicitly designed.

### Risk: URL contract conflict with existing `view`

Mitigation: use `tableView`, not `view`.

### Risk: using Arabic display text as identity

Mitigation: use stable IDs/codes when available; use normalized text only as a documented fallback.

---

## 13. Recommended implementation order

1. Backend contract/read-model aggregation.
2. Frontend models and URL sync.
3. Frontend API + cache key + facade reload behavior.
4. Tabs component.
5. Page integration.
6. Table rendering per `tableView`.
7. README/spec updates.
8. Tests/build.
9. Optional follow-up: grouped row detail panels.
