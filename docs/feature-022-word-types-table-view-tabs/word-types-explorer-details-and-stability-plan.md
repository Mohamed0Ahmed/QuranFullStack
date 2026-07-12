# Word Types Explorer Details & Stability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: use `superpowers:executing-plans` to execute this plan task-by-task with the review checkpoints below. Do not create a worktree or change branches; this plan is intentionally for the current branch.

## 1. Goal

Extend the current Word Types Explorer so its four aggregation views remain stable and selectable, and so root, stem, and lemma rows open fully scoped details without changing the Quranic-data grain established by Features 019 and 022.

The deliverable is additive, read-only, migration-free, importer-free, and package-free. It preserves the existing `GET /api/words/word-types/words` and `GET /api/words/word-types/table` contracts while adding grouped-detail reads, explicit grouped URL selection, a persistent table/details shell, row numbering, and quiet explorer-row interaction.

## 2. Architecture summary

The Backend adds four Word-Types-owned detail resources below the existing unified table resource:

```text
GET /api/words/word-types/table/{kind}/{dimensionId}
GET /api/words/word-types/table/{kind}/{dimensionId}/words
GET /api/words/word-types/table/{kind}/{dimensionId}/ayahs
GET /api/words/word-types/table/{kind}/{dimensionId}/surahs
```

`kind` is the plural route key `roots|stems|lemmas`. Every action accepts the same grammatical scope as the selected table row: `type`, `childCode`, `case`, `tense`, and `voice`. Member words and ayahs are server-paged; summary and surahs are single-shot. A new `WordTypeGroupedSelection` crosses the Application/Infrastructure boundary, and new partial reader files keep `EfWordTypesReader.cs` below its hard size threshold. A separate `WordTypeGroupedDetailsController` shares the existing route base without growing `WordTypesController.cs` past its controller threshold.

The Frontend represents selection as a discriminated union (`word|root|stem|lemma`), keeps explicit URL keys (`word/contextCode`, `root`, `stem`, `lemma`), and routes all detail loading through the existing detail facade/loader boundary. Internal detail state always normalizes a paged detail view to page 1 when the URL omits the page, while the canonical URL omits `detailPage` at page 1, writes it only above page 1, and removes it for surahs. The list shell, four-view strip, and detail-panel host stay mounted. The table owns prompt/loading/empty/error rendering inside its stable body. Grouped member-word rows use a new display-only component and cannot navigate or mutate selection.

## 3. Locked decisions

These rules override the historical Feature 022 MVP decisions that hid grouped details, made grouped rows noninteractive, hid the strip without a leaf, expanded grouped tables full width, and reset `tableView` to `words`:

1. The strip remains visible after the tree loads in RTL order: `كلمات | جذور | أصول | صيغ`.
2. `tableView` survives main type, child, case, tense, voice, sort, and list-page changes. Only choosing the Words tab changes it to `words`.
3. A grammatical scope change resets list page to 1 and clears the old scoped selection; sort and list-page changes preserve a still-valid selection.
4. The table component and its structural shell are never conditionally removed. Prompt, loading, empty, error, and retry UI render inside its body.
5. The details component host is never conditionally removed. It renders a kind-aware empty selection when no active row is valid.
6. Main grouped rows are selectable; newly selected root/stem/lemma details default internally to `view=words` and `detailPage = 1`. The canonical page-1 URL is `view=words` without `detailPage`.
7. Grouped details include summary/counts, member words, ayahs, and surahs.
8. Related words and ayahs are server-paged. Surahs and missing surahs are single-shot. For words/ayahs, internal `detailPage` defaults to 1, the URL omits page 1 and writes only values greater than 1; surahs always remove `detailPage`.
9. Member-word rows are display-only: no button/link, no click handler, no navigation, no selected state, no URL write, no `qd-interactive-surface`.
10. All grouped table/detail membership comes from head-level `quran_word_morphology` via the same scoped `base` CTE. No `quran_word_morphology_segments` join is allowed.
11. Member words group exactly as the Words table: `(unique_tashkeel_word_id, context_code)` after all scope and dimension filters.
12. Root/stem/lemma IDs are numeric identity; labels are display only. Null dimensions and ayah markers remain excluded.
13. The four measures remain distinct: morphology occurrences, word-context rows, distinct ayahs, distinct surahs.
14. URL keys are explicit: `root`, `stem`, `lemma`. No generic `dim` key is permitted.
15. All four main table views display a page-relative row number: `(page - 1) * pageSize + index + 1`. Database IDs are never displayed.
16. Word Types table rows use `qd-explorer-table__row` and never `qd-interactive-surface`. Selected rows remain distinct; loading rows receive no hover.
17. No existing global Roots/Stems/Lemmas endpoint or explorer facade supplies these scoped details.

## 4. Current-to-target contract map

| Concern | Current contract | Target contract |
|---|---|---|
| Grouped list | `GET .../table?tableView=roots|stems|lemmas` | Preserved unchanged |
| Grouped summary | Missing | `GET .../table/{kind}/{dimensionId}`, single-shot |
| Grouped words | Missing | `GET .../table/{kind}/{dimensionId}/words?page&pageSize` |
| Grouped ayahs | Missing | `GET .../table/{kind}/{dimensionId}/ayahs?page&pageSize` |
| Grouped surahs | Missing | `GET .../table/{kind}/{dimensionId}/surahs`, no paging |
| Route kind | No grouped-detail route | Plural route values `roots|stems|lemmas`; unknown value is 400 |
| Response kind | Table rows use singular `root|stem|lemma` discriminator | Grouped summary returns singular `kind`; member DTO is a word-context row |
| Scope | Table list carries type/child/case/tense/voice | Every grouped detail action carries the identical five-field scope |
| Selection | Word-only `word+contextCode` | Discriminated word/root/stem/lemma selection |
| URL | Grouped selection dropped | `root`, `stem`, or `lemma` parsed/restored/shared |
| Detail tabs | Word only: ayahs/surahs | Word: ayahs/surahs; grouped: words/ayahs/surahs |
| Default detail tab | Word defaults ayahs | Grouped defaults words; word remains ayahs |
| Detail paging | Ayahs only | Grouped words + all ayahs; never surahs |
| Detail-page URL | Current callers inconsistently write or clear default page values | Internal page defaults to 1; canonical URL omits page 1, writes `detailPage` only when > 1, and removes it for surahs |
| Shell | Conditional table/strip/panel | Stable table, strip after tree load, always-present panel host |
| Grouped rows | Plain noninteractive divs | Native keyboard-selectable row buttons |
| Member rows | Missing | Plain display-only rows with scoped counts |
| Main row number | Missing | Shared page-relative number on all four views |
| Hover | Word row uses card-lift utility | Quiet shared explorer-row hover, no transform/shadow lift |

### Exact Backend payloads

```csharp
public enum WordTypeGroupedDimensionKind { Root, Stem, Lemma }

public sealed record WordTypeGroupedSelection(
    WordTypeGroupedDimensionKind Kind,
    int DimensionId,
    WordTypeFilter Filter)
{
    public bool IsValid => DimensionId > 0;
}

public sealed record WordTypeGroupedSummaryDto(
    string Kind,                 // root | stem | lemma
    int DimensionId,
    string DisplayText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount);

public sealed record WordTypeGroupedMemberWordDto(
    int TashkeelWordId,
    string ContextCode,
    string? Case,
    string? Tense,
    string? Voice,
    string DisplayText,
    string TypeCode,
    WordTypeLabelDto TypeLabel,
    WordTypeLabelDto BroadLabel,
    string? CaseOrFeature,
    string? RootText,
    string? LemmaText,
    string? StemText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount);
```

`IWordTypesReader` adds exactly:

```csharp
Task<WordTypeGroupedSummaryDto?> GetGroupedSummaryAsync(
    WordTypeGroupedSelection selection, CancellationToken cancellationToken);
Task<PagedResult<WordTypeGroupedMemberWordDto>?> GetGroupedMemberWordsAsync(
    WordTypeGroupedSelection selection, int page, int pageSize, CancellationToken cancellationToken);
Task<PagedResult<WordTypeAyahMatchDto>?> GetGroupedAyahMatchesAsync(
    WordTypeGroupedSelection selection, int page, int pageSize, CancellationToken cancellationToken);
Task<WordTypeSurahsResponse?> GetGroupedSurahsAsync(
    WordTypeGroupedSelection selection, CancellationToken cancellationToken);
```

`null` means that the positive dimension ID does not exist in the supplied scope. An existing selection with an out-of-range page returns a non-null empty page with the correct `TotalCount`.

### Exact Frontend selection

```ts
export type WordTypeDetailSelection =
  | { kind: 'word'; identity: WordTypeRowIdentity }
  | { kind: 'root'; rootId: number; scope: WordTypeDetailScope }
  | { kind: 'stem'; stemId: number; scope: WordTypeDetailScope }
  | { kind: 'lemma'; lemmaId: number; scope: WordTypeDetailScope };
```

`WordTypeDetailScope` contains `type`, `childCode`, `case`, `tense`, and `voice`. `WordTypeDetailView` becomes `'words'|'ayahs'|'surahs'`, but `words` is valid only for grouped selection.

## 5. Exact file inventory

### Create: Backend

- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/WordTypeGroupedDimensionKind.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/WordTypeGroupedSelection.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/Responses/WordTypeGroupedSummaryDto.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/Responses/WordTypeGroupedMemberWordDto.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedSummary/GetWordTypeGroupedSummaryQuery.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedSummary/GetWordTypeGroupedSummaryHandler.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedSummary/GetWordTypeGroupedSummaryOutcome.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedWords/GetWordTypeGroupedWordsQuery.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedWords/GetWordTypeGroupedWordsHandler.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedWords/GetWordTypeGroupedWordsOutcome.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedAyahs/GetWordTypeGroupedAyahsQuery.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedAyahs/GetWordTypeGroupedAyahsHandler.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedAyahs/GetWordTypeGroupedAyahsOutcome.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedSurahs/GetWordTypeGroupedSurahsQuery.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedSurahs/GetWordTypeGroupedSurahsHandler.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedSurahs/GetWordTypeGroupedSurahsOutcome.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.GroupedDetails.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.GroupedDetails.Sql.cs`
- `Backend/api/QuranDashboard.Api/Controllers/Words/WordTypeGroupedDetailsController.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesGroupedSummaryReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesGroupedMemberWordsReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesGroupedAyahsReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesGroupedSurahsReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesGroupedDetailsControllerTests.cs`

### Modify: Backend

- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/IWordTypesReader.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/WordTypesHandlerValidation.cs`
- `Backend/application/QuranDashboard.Application/DependencyInjection.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.Sql.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/CachedWordTypesReader.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/WordTypesCacheKeys.cs`
- `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/word-types-explorer-seed.sql`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesCacheReadTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesLoggingTests.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md`
- `Backend/api/QuranDashboard.Api/Controllers/README.md`

### Create: Frontend

- `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types-detail.models.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail.facade.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail-view.loader.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-grouped-words-list/word-type-grouped-words-list.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-grouped-words-list/word-type-grouped-words-list.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-grouped-words-list/word-type-grouped-words-list.component.scss`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-grouped-words-list/word-type-grouped-words-list.component.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.scss`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.spec.ts`

### Modify: Frontend

- `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.models.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.labels.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-cache.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-cache.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail.facade.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail-view.loader.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail-panel.updates.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/data-access/word-types.api.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/data-access/word-types.api.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.scss`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.scss`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.scss`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/README.md`

### Modify: specifications and planning documentation

- `specs/019-word-types-explorer/spec.md`
- `specs/019-word-types-explorer/data-model.md`
- `specs/019-word-types-explorer/contracts/word-types-api.md`
- `specs/019-word-types-explorer/contracts/backend-read-abstractions.md`
- `specs/019-word-types-explorer/contracts/frontend-routing-state.md`
- `specs/019-word-types-explorer/quickstart.md`
- `docs/feature-022-word-types-table-view-tabs/word-types-table-view-tabs-plan.md` (add a supersession note only; preserve its historical MVP decisions)

No other production, test, spec, config, package, migration, or importer file is in scope.

## 6. Dependency/order rationale

1. Summary locks dimension parsing, scoped identity, not-found semantics, the controller route family, and count parity.
2. Member words then reuse the same scoped selection and refactor the existing `RowsSql`/`RowsCountSql` only enough to apply an allowlisted dimension predicate, guaranteeing row-for-row grouping parity.
3. Ayahs reuse that base and establish canonical highlight provenance plus bounded page hydration.
4. Surahs complete the Backend surface and verify cache/log/controller behavior across all four reads.
5. Frontend models/URL state must land before consumers so refresh/share/back-forward behavior is authoritative.
6. API/cache methods land before the facade/loader that consumes them.
7. The detail facade establishes kind-aware orchestration and stale protection before visual wiring.
8. Stable shell behavior is separated from grouped detail rendering so reviewers can reject transition behavior without reopening data contracts.
9. Grouped selection and panel content integrate the completed state/API surface.
10. Row numbering/hover/a11y is a small independent visual consistency slice.
11. Full verification runs only after every contract-owning task has updated its documentation.

## 7. Global acceptance criteria

- All four tabs remain visible after a successful tree read and the active `tableView` never changes implicitly.
- The same table and detail component hosts remain in the DOM through parent, child, filter, sort, view, loading, empty, and error transitions.
- Root/stem/lemma selections restore on refresh and browser history using only their explicit key.
- A selected grouped summary exactly equals the selected list row for occurrences, ayahs, and surahs.
- All grouped reads are filtered by kind, ID, type, childCode, case, tense, and voice at head-word grain.
- Secondary segment dimensions never appear in grouped details.
- Member words equal Words-table word-context grouping, including multiple contexts for the same tashkeel word; membership and parity are filtered by selected numeric `root_id`, `stem_id`, or `lemma_id`, never by `rootText`, `stemText`, `lemmaText`, or any other display label.
- Ayah DTOs hydrate canonical `quran_words.text_uthmani` and matched word IDs/positions in bounded page queries.
- Detail state is page 1 when a paged-view URL omits `detailPage`; canonical URLs omit page 1, serialize only pages greater than 1, and always remove `detailPage` for surahs.
- Surahs/missing surahs are single-shot and never receive `detailPage` or paging query parameters.
- Backend and frontend caches isolate kind, ID, scope, view, and page where applicable.
- Invalid kind/filter/id/paging is 400; a valid but absent scoped group is 404; out-of-range pages are 200-empty.
- Grouped member rows contain no interactive element or interaction class and emit no selection/navigation event.
- All main table views show page-relative row numbers and no DB ID.
- Table rows have quiet shared hover, selected state beyond color, visible focus, and no hover for skeleton rows.
- Desktop keeps the split table/detail workspace; mobile preserves the detail host and opens the existing modal behavior only for valid selection.
- Focused tests, full Word Types tests, Backend build, Frontend build, clean-code self-check, and test-code self-check pass.

---

## Task 1: Backend grouped selection, summary, and route foundation

**Task goal:** Add the validated root/stem/lemma selection contract and a scoped summary whose three counts are identical to the selected grouped table row.

**Exact files**

- Create: `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/WordTypeGroupedDimensionKind.cs`
- Create: `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/WordTypeGroupedSelection.cs`
- Create: `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/Responses/WordTypeGroupedSummaryDto.cs`
- Create: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedSummary/GetWordTypeGroupedSummaryQuery.cs`
- Create: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedSummary/GetWordTypeGroupedSummaryHandler.cs`
- Create: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedSummary/GetWordTypeGroupedSummaryOutcome.cs`
- Create: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.GroupedDetails.cs`
- Create: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.GroupedDetails.Sql.cs`
- Create: `Backend/api/QuranDashboard.Api/Controllers/Words/WordTypeGroupedDetailsController.cs`
- Create: `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesGroupedSummaryReadTests.cs`
- Modify: `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/IWordTypesReader.cs`
- Modify: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/WordTypesHandlerValidation.cs`
- Modify: `Backend/application/QuranDashboard.Application/DependencyInjection.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.Sql.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/CachedWordTypesReader.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/WordTypesCacheKeys.cs`
- Modify: `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`
- Modify: `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/word-types-explorer-seed.sql`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md`
- Modify: `Backend/api/QuranDashboard.Api/Controllers/README.md`
- Modify: `specs/019-word-types-explorer/spec.md`
- Modify: `specs/019-word-types-explorer/data-model.md`
- Modify: `specs/019-word-types-explorer/contracts/word-types-api.md`
- Modify: `specs/019-word-types-explorer/contracts/backend-read-abstractions.md`

**Existing symbols involved**

- `WordTypeTableView`/`WordTypeTableViewParser`, `WordTypeFilter`, `WordTypesHandlerValidation.IsValidFilter`.
- `EfWordTypesReader.BaseRowsSql`, `WordTypeReadContext`, `DimensionColumns`, `BuildCountParameters`.
- `WordTypesCacheEntryOptions.Detail` and current summary outcome/status mapping.

**Interfaces**

- Consumes: the existing grouped table row IDs and full `WordTypeFilter` scope.
- Produces: `WordTypeGroupedDimensionKind`, `WordTypeGroupedSelection`, `WordTypeGroupedSummaryDto`, `GetGroupedSummaryAsync`, `GET .../table/{kind}/{dimensionId}`, and the common validation/outcome vocabulary used by Tasks 2–4.

**Failing tests first**

Add these exact tests to `WordTypesGroupedSummaryReadTests.cs`:

- `GroupedSummary_RootStemLemma_InSameScope_MatchesSelectedTableRow` as a `[Theory]` over roots/190700, stems/190600, lemmas/190500. Load the grouped row through `GetTableRowsAsync`, load the summary through the new method, and assert ID, display text, occurrences, ayahs, and surahs are equal.
- `GroupedSummary_ActiveSecondaryFilter_RecomputesTheSameScopedCounts`: root 190701 under verb + tense=past returns 1/1/1, not the global root totals.
- `GroupedSummary_DimensionOutsideScope_ReturnsNull`: root 190701 under noun scope is absent.
- `GroupedSummary_HeadDimensionIgnoresSecondarySegmentDimension`: add a clearly fixture-only secondary segment on word 1903001 pointing to the already-seeded alternate IDs 190701/190502/190602; those IDs remain absent from the noun-scoped details and never alter the word's head IDs 190700/190500/190600.
- `GroupedSummary_MarkerAndNullDimensionsRemainExcluded`: marker 1903004 and null head dimensions cannot create a summary.
- `GroupedSummaryHandler_InvalidKindIdFilterAndMissingGroup_MapToControlledOutcomes`: unknown kind → `InvalidKind`, ID 0 → `InvalidId`, cross-type filter → `InvalidFilter`, positive absent ID → `NotFound`.

Focused command, working directory `Backend/`:

```bash
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter "FullyQualifiedName~WordTypesGroupedSummaryReadTests" --logger "console;verbosity=minimal"
```

Expected pre-implementation failure: compile errors for `WordTypeGroupedDimensionKind`, `WordTypeGroupedSelection`, `GetGroupedSummaryAsync`, and `GetWordTypeGroupedSummaryHandler`.

**Minimum implementation**

1. Define enum values `Root|Stem|Lemma`. The parser accepts plural route keys `roots|stems|lemmas`; `ToRouteKey` returns plural and `ToDtoKind` returns singular.
2. Add `WordTypesHandlerValidation.NormalizeFilter(type, childCode, case, tense, voice)` so all four grouped handlers create the same filter before calling `IsValidFilter`.
3. Add `GetGroupedSummaryAsync` to the reader. Implement it in the new partial, not the 572-line primary file.
4. In grouped SQL, select from `BaseRowsSql(context)`, apply the allowlisted `root_id|stem_id|lemma_id = @dimensionId` predicate, group by that ID, and compute `COUNT(*)`, `COUNT(DISTINCT ayah_id)`, and `COUNT(DISTINCT surah_number)`. Do not reference the segments table.
5. Add `WordTypesCacheKeys.GroupedSummary(selection)`. Hash dimension ID plus the five scope fields; expose only view/kind labels in the readable prefix.
6. The handler validates in order: route kind, positive ID, filter, reader result. It logs only kind, numeric ID, type, childCode, and boolean secondary-filter flags.
7. Create `WordTypeGroupedDetailsController` with route `api/words/word-types/table` and summary action `[HttpGet("{kind}/{dimensionId:int}")]`. Map invalid inputs to 400, missing scoped group to 404, success to 200 `ApiResponse<WordTypeGroupedSummaryDto>`.
8. Add one fixture-only segment row using already-seeded alternate dimension IDs; do not invent new Quranic text or catalogue values. The test must prove the head row wins by absence of segment expansion.

**Exact verification**

Run the focused command above, then:

```bash
dotnet build QuranDashboard.sln --disable-build-servers -m:1 -p:BuildInParallel=false -p:RestoreDisableParallel=true -v minimal
```

Expected: all summary tests pass; build has zero errors; the selected grouped row and summary have exact count parity.

**Documentation belonging to this task**

- Add the summary route/status/payload to `word-types-api.md`.
- Add grouped selection and summary reader semantics to `backend-read-abstractions.md` and `data-model.md`.
- Add the grouped-details user story and head-grain requirement to `spec.md`.
- Document scoped grouped detail reads in Backend Words README and the route family in Controllers README.

**Review checkpoint:** Verify the SQL text contains `quran_word_morphology` and no `quran_word_morphology_segments`; compare a table row and summary in the same fixture scope.

**Suggested commit boundary:** `feat(word-types): add scoped grouped summaries`

---

## Task 2: Backend grouped member-word paging with Words-table parity

**Task goal:** Return display-only member data as paged word-context rows grouped exactly like the existing Words table after filtering the scoped base by the selected numeric `root_id`, `stem_id`, or `lemma_id`; display text is never membership identity.

**Exact files**

- Create: `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/Responses/WordTypeGroupedMemberWordDto.cs`
- Create: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedWords/GetWordTypeGroupedWordsQuery.cs`
- Create: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedWords/GetWordTypeGroupedWordsHandler.cs`
- Create: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedWords/GetWordTypeGroupedWordsOutcome.cs`
- Create: `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesGroupedMemberWordsReadTests.cs`
- Modify: `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/IWordTypesReader.cs`
- Modify: `Backend/application/QuranDashboard.Application/DependencyInjection.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.GroupedDetails.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.GroupedDetails.Sql.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.Sql.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/CachedWordTypesReader.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/WordTypesCacheKeys.cs`
- Modify: `Backend/api/QuranDashboard.Api/Controllers/Words/WordTypeGroupedDetailsController.cs`
- Modify: `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md`
- Modify: `specs/019-word-types-explorer/data-model.md`
- Modify: `specs/019-word-types-explorer/contracts/word-types-api.md`
- Modify: `specs/019-word-types-explorer/contracts/backend-read-abstractions.md`

**Existing symbols involved**

- `RowsSql`, `RowsCountSql`, `ContextExpression`, `TypeCodeExpression`, `WordTypeRowSqlResult`, `BuildRowsParameters`, `ReadPaging.CalculateSafeSkip`.
- `WordTypeSort.Occurrences` provides the fixed deterministic member ordering.

**Interfaces**

- Consumes: `WordTypeGroupedSelection` from Task 1.
- Produces: `GetGroupedMemberWordsAsync` and `GET .../{kind}/{dimensionId}/words?page&pageSize` for Tasks 6–9.

**Failing tests first**

- `GroupedMemberWords_RootStemLemma_MatchNumericIdScopedWordsBaselineRowForRow` theory: for root 190700, stem 190600, and lemma 190500 under noun scope, use a test-local `LoadNumericDimensionWordsBaselineAsync(kind, dimensionId, filter)` helper over the real PostgreSQL fixture. Its allowlisted branch must apply `m.root_id = @dimensionId`, `m.stem_id = @dimensionId`, or `m.lemma_id = @dimensionId` before grouping by the same `(unique_tashkeel_word_id, context_code)` formula as the Words table. Compare `(TashkeelWordId, ContextCode)`, occurrences, ayahs, and surahs row-for-row with `GetGroupedMemberWordsAsync`. The helper and assertions must not read or filter `root_text`, `stem_text`, `lemma_text`, `RootText`, `StemText`, `LemmaText`, winner text, or DTO display labels.
- `GroupedMemberWords_NounParent_PreservesHeadPosUsageSplit`: tashkeel ID 191001 returns separate `N`, `PN`, and `ADJ` rows, never one distinct-word row.
- `GroupedMemberWords_VerbParent_PreservesTenseUsageSplit`: root 190701 returns past, present, and imperative contexts; `tense=past` filters base first and returns only past.
- `GroupedMemberWords_ExactChildPinsContextWithoutChangingGroupingContract`: noun/PN returns one 191001+PN row.
- `GroupedMemberWords_ReportsOccurrenceRowAyahAndSurahMeasuresSeparately`: assert summary occurrences, member `TotalCount`, member row counts, distinct ayah count, and distinct surah count as separate named assertions.
- `GroupedMemberWords_PaginatesAfterWordContextGrouping`: pageSize 1 gives deterministic pages, same total count, and an out-of-range empty page.
- `GroupedMemberWords_MarkersNullAndSegmentOnlyDimensionsNeverAppear`.
- `GroupedWordsHandler_InvalidPaging_ReturnsInvalidPaging`.

Focused command:

```bash
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter "FullyQualifiedName~WordTypesGroupedMemberWordsReadTests" --logger "console;verbosity=minimal"
```

Expected pre-implementation failure: missing member DTO, reader method, handler, and route.

**Minimum implementation**

1. Extend `RowsSql` and `RowsCountSql` with an optional `WordTypeGroupedDimensionKind?` that changes only `BaseRowsSql` by adding the allowlisted numeric predicate `root_id|stem_id|lemma_id = @dimensionId`. Existing callers pass null and remain byte-for-byte semantically unchanged; no text column is permitted in this membership predicate.
2. Extend row/count parameter builders to add the selected numeric `@dimensionId` only for grouped detail calls. `rootText`, `stemText`, and `lemmaText` remain projection-only display fields and are never accepted as query parameters or used to filter parity expectations.
3. Reuse the existing grouping/winner/order SQL; do not copy it into a second implementation and never use `DISTINCT tashkeel_word_id` alone.
4. Map `WordTypeRowSqlResult` to `WordTypeGroupedMemberWordDto`, carrying active case/tense/voice values exactly as `WordTableRowDto` does.
5. Count grouped word-context rows before paging, use page/pageSize validation 1..100, and return 200-empty for an out-of-range page.
6. Add `GroupedWords` cache key with page and pageSize. Summary and other views must not share its prefix.
7. Add `[HttpGet("{kind}/{dimensionId:int}/words")]`. It accepts the five scope params plus page/pageSize and no sort.

**Exact verification**

Run the focused test and Backend build from Task 1. Expected: each numeric-ID-scoped baseline matches row-for-row, context splits remain separate, and page metadata is correct.

**Documentation belonging to this task**

- Document the paged words route and DTO in `word-types-api.md`.
- Replace any distinct-unique-word language with exact `(unique_tashkeel_word_id, context_code)` semantics in `data-model.md` and `backend-read-abstractions.md`; state that the selected numeric head `root_id|stem_id|lemma_id` filters the base before grouping and labels are projection-only.
- Document fixed occurrence-order paging and display-only consumption in Backend Words README.

**Review checkpoint:** Inspect the SQL diff beside `RowsSql` and the parity test baseline. Reject any copied grouping logic, segment join, or membership/parity filter using `root_text`, `stem_text`, `lemma_text`, `RootText`, `StemText`, `LemmaText`, or winner display text.

**Suggested commit boundary:** `feat(word-types): add scoped grouped member words`

---

## Task 3: Backend grouped ayahs with canonical highlights and bounded queries

**Task goal:** Add paged scoped ayahs using canonical Quran-word Uthmani text, exact matched IDs/positions, and no N+1 hydration.

**Exact files**

- Create: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedAyahs/GetWordTypeGroupedAyahsQuery.cs`
- Create: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedAyahs/GetWordTypeGroupedAyahsHandler.cs`
- Create: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedAyahs/GetWordTypeGroupedAyahsOutcome.cs`
- Create: `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesGroupedAyahsReadTests.cs`
- Modify: `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/IWordTypesReader.cs`
- Modify: `Backend/application/QuranDashboard.Application/DependencyInjection.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.GroupedDetails.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.GroupedDetails.Sql.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/CachedWordTypesReader.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/WordTypesCacheKeys.cs`
- Modify: `Backend/api/QuranDashboard.Api/Controllers/Words/WordTypeGroupedDetailsController.cs`
- Modify: `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md`
- Modify: `specs/019-word-types-explorer/contracts/word-types-api.md`
- Modify: `specs/019-word-types-explorer/contracts/backend-read-abstractions.md`

**Existing symbols involved**

- `WordTypeAyahMatchDto`, `AyahWordForHighlightDto`, `ReadPaging.CalculateSafeSkip`.
- Existing `GetAyahMatchesAsync` page-then-hydrate pattern and `ResolveAyahPageNumber`.

**Interfaces**

- Consumes: grouped selection and scoped base.
- Produces: `GetGroupedAyahMatchesAsync` and `GET .../{kind}/{dimensionId}/ayahs?page&pageSize`.

**Failing tests first**

- `GroupedAyahs_RootStemLemma_ReturnOnlySameScopeMatches` theory.
- `GroupedAyahs_ActiveCaseTenseAndVoiceFiltersPropagateToBase`.
- `GroupedAyahs_PaginatesDistinctAyahsBeforeHydratingWords`: pageSize 1 gives ordered verse pages and stable `TotalCount`.
- `GroupedAyahs_UsesQuranWordsUthmaniTextAndMatchedWordIdsForHighlighting`: query expected non-marker `quran_words` for the returned ayah and compare ID/text/order to DTO `Words`; compare scoped IDs and positions to `MatchedWordIds`/`MatchedWordPositions`.
- `GroupedAyahs_ExcludesMarkersAndSecondarySegmentDimensions`.
- `GroupedAyahs_OutOfRangePageIsEmptyButExistingSelectionIsNotNotFound`.
- `GroupedAyahs_PageHydrationUsesBoundedCommandCount`: with the existing PostgreSQL interceptor, assert the command count is fixed (count + grouped page + one word hydration query), not proportional to returned ayahs.
- `GroupedAyahsHandler_InvalidPagingAndMissingGroup_ReturnControlledOutcomes`.

Focused command:

```bash
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter "FullyQualifiedName~WordTypesGroupedAyahsReadTests" --logger "console;verbosity=minimal"
```

Expected pre-implementation failure: missing reader/handler/route symbols.

**Minimum implementation**

1. Count distinct ayah IDs from the dimension-filtered `base`. Zero means not found.
2. Page grouped ayah metadata in Mushaf order and aggregate matched word IDs/positions for only that page.
3. Hydrate all readable words for all page ayah IDs in one `AsNoTracking` query from `QuranWords.TextUthmani`, excluding markers.
4. Reuse `WordTypeAyahMatchDto` and `ResolveAyahPageNumber`; do not introduce an ayah-text fallback or string replacement.
5. Add a paged `GroupedAyahs` key and the `[HttpGet("{kind}/{dimensionId:int}/ayahs")]` action.

**Exact verification**

Run the focused test and Backend build. Expected: canonical word text/IDs match database rows; no marker appears; command count stays bounded; page metadata passes.

**Documentation belonging to this task**

- Add the paged ayah route, highlight provenance, and bounded hydration rule to `word-types-api.md` and `backend-read-abstractions.md`.
- Add canonical `quran_words.text_uthmani` provenance to Backend Words README.

**Review checkpoint:** Verify the page query aggregates matches and the hydration query runs once per page, not once per ayah.

**Suggested commit boundary:** `feat(word-types): add scoped grouped ayahs`

---

## Task 4: Backend grouped surahs, cache isolation, logging, and HTTP completion

**Task goal:** Complete single-shot surah/missing-surah reads and prove the full Backend surface is isolated, safely logged, and correctly mapped to HTTP.

**Exact files**

- Create: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedSurahs/GetWordTypeGroupedSurahsQuery.cs`
- Create: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedSurahs/GetWordTypeGroupedSurahsHandler.cs`
- Create: `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/GetWordTypeGroupedSurahs/GetWordTypeGroupedSurahsOutcome.cs`
- Create: `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesGroupedSurahsReadTests.cs`
- Create: `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesGroupedDetailsControllerTests.cs`
- Modify: `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/IWordTypesReader.cs`
- Modify: `Backend/application/QuranDashboard.Application/DependencyInjection.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.GroupedDetails.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.GroupedDetails.Sql.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/CachedWordTypesReader.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/WordTypesCacheKeys.cs`
- Modify: `Backend/api/QuranDashboard.Api/Controllers/Words/WordTypeGroupedDetailsController.cs`
- Modify: `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`
- Modify: `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesCacheReadTests.cs`
- Modify: `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesLoggingTests.cs`
- Modify: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md`
- Modify: `Backend/api/QuranDashboard.Api/Controllers/README.md`
- Modify: `specs/019-word-types-explorer/contracts/word-types-api.md`
- Modify: `specs/019-word-types-explorer/contracts/backend-read-abstractions.md`

**Existing symbols involved**

- `WordTypeSurahsResponse`, `WordTypeSurahOccurrenceDto`, `WordTypeMissingSurahDto`.
- `WordTypesCacheEntryOptions.Detail`, `RecordingLoggerProvider`, `ApiResponse<T>`.

**Interfaces**

- Consumes all prior grouped contracts.
- Produces the complete Backend API consumed by Frontend Tasks 6–9.

**Failing tests first**

- `GroupedSurahs_RootStemLemma_ReturnScopedMentionedAndMissingSurahs` theory.
- `GroupedSurahs_ActiveFiltersNarrowOccurrenceCounts`.
- `GroupedSurahs_IsSingleShotAndHasNoPagingContract`: reflection asserts the controller action has no page/pageSize arguments; result includes mentioned and missing arrays.
- `GroupedSurahs_UsesServerAggregateInsteadOfLoadingEveryOccurrence`: command interceptor observes one aggregate read plus one bounded surah-catalogue read.
- `GroupedDetailsCacheKeys_IsolateKindIdScopeViewAndApplicablePage`: change one field at a time; words/ayahs change by page, summary/surahs have no page component.
- `GroupedDetailsCachedReader_RepeatedReadsDoNotIssueExtraCommands`.
- `GroupedDetailsHandlers_LogSafeStructuredFieldsWithoutTextPayloadOrSql`.
- `GroupedDetailsController_UsesVerifiedRouteTemplates`: reflect exact four `HttpGet` templates.
- `GroupedDetailsController_MapsInvalidKindIdFilterPagingAndNotFound`: assert 400/404 `ApiResponse` and success 200.

Focused commands:

```bash
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter "FullyQualifiedName~WordTypesGroupedSurahsReadTests|FullyQualifiedName~WordTypesGroupedDetailsControllerTests|FullyQualifiedName~WordTypesCacheReadTests|FullyQualifiedName~WordTypesLoggingTests" --logger "console;verbosity=minimal"
```

Expected pre-implementation failure: missing surah handler/action/cache key, then route/status assertions fail until the controller is complete.

**Minimum implementation**

1. Aggregate occurrence counts by `surah_number` inside PostgreSQL from the scoped base; fetch the 114-row surah catalogue once; derive mentioned and missing lists in numeric order.
2. Add `GroupedSurahs` cache key without page/pageSize and the no-paging controller action.
3. Complete all four controller actions using the same query-param names and outcome mappings.
4. Register all four handlers in Application DI. Infrastructure DI remains unchanged because the existing decorated `IWordTypesReader` registration still owns the implementation.
5. Add Arabic feature-owned messages for four successful reads, invalid grouped kind, invalid grouped ID, and scoped group not found.
6. Log handler boundaries with safe kind/ID/scope/page metadata only; never log display text, Quran text, payload, SQL, or large ID arrays.

**Exact verification**

Run the focused command, then:

```bash
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter "FullyQualifiedName~WordsWordTypes" --logger "console;verbosity=minimal"
```

Expected: all Word Types Backend tests pass; no cache cross-serve; surah actions expose no paging.

**Documentation belonging to this task**

- Complete all four routes, status codes, caching, logging, and single-shot surah policy in Backend contracts/READMEs.
- State explicitly that missing surahs share the single-shot response and `detailPage` is not an API parameter.

**Review checkpoint:** API/architecture review of route names, thin controller behavior, `ApiResponse` mapping, localized messages, and cache key dimensions.

**Suggested commit boundary:** `feat(word-types): complete grouped detail api`

---

## Task 5: Frontend discriminated selection and explicit URL identity

**Task goal:** Make word/root/stem/lemma selection shareable, restorable, and browser-history-safe without a generic identity key.

**Exact files**

- Create: `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types-detail.models.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.models.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.spec.ts`
- Modify: `specs/019-word-types-explorer/spec.md`
- Modify: `specs/019-word-types-explorer/contracts/frontend-routing-state.md`
- Modify: `docs/feature-022-word-types-table-view-tabs/word-types-table-view-tabs-plan.md`

**Existing symbols involved**

- `ParsedWordTypesQuery`, `WORD_TYPES_QUERY_KEYS`, `WORD_TYPES_SELECTION_QUERY_KEYS`, `WordTypesQueryChange`, `WORD_TYPES_QUERY_ORDER`.
- `parseWordTypesQueryParams`, `buildWordTypesQueryParams`, `clearWordTypesSelection`.
- Existing convention evidence: `buildWordTypesQueryParams` and sibling builders serialize values explicitly supplied by callers, while `roots-explorer-page.component.ts` passes `detailPage: null` when resetting to the default. Therefore the target canonicalization belongs in Word Types caller/helper logic: omit default page 1 without changing generic builder semantics.

**Interfaces**

- Consumes explicit table row discriminators and the five scope fields.
- Produces `WordTypeDetailSelection`, `root/stem/lemma` parsed fields, kind-aware default view, and selection-clearing helpers for Tasks 7–9.

**Failing tests first in `word-types-url-sync.spec.ts`**

- `restores root selection only from tableView=roots&root=positiveId`.
- Equivalent data-driven cases for stem and lemma.
- `ignores root/stem/lemma keys that are incompatible with the active tableView`.
- `keeps word/contextCode only in words view`.
- `defaults a newly selected grouped identity to view=words and internal detailPage=1 while omitting detailPage from the canonical URL`.
- `normalizes word selection view=words back to ayahs`.
- `parses missing detailPage as internal page 1 for words and ayahs`.
- `canonicalizes words and ayahs page 1 by emitting detailPage=null, and emits the numeric value only for detailPage greater than 1`.
- `normalizes surahs detailPage to internal page 1 and canonical clear params remove detailPage even when the incoming URL supplied a positive value`.
- `clears only incompatible keys when changing table view and never emits dim`.
- `roundTripsGroupedSelectionWithFullScopeForRefreshAndSharing`.
- `replaysRootThenStemThenRootParamMapsAsBrowserBackForwardState` using real `ParamMap` instances.
- Update canonical-order test to include `root,stem,lemma` and assert no `dim`.

Focused command, working directory `Frontend/quran-dashboard-ui/`:

```bash
npm test -- --include=src/app/features/words/state/word-types-url-sync.spec.ts
```

Expected pre-implementation failure: grouped keys/types are absent and the parser drops grouped selection.

**Minimum implementation**

1. Add query keys `root`, `stem`, `lemma`; add nullable fields to `ParsedWordTypesQuery`.
2. Expand `WordTypeDetailView` to words/ayahs/surahs. Keep separate defaults: word → ayahs, grouped → words.
3. Add the exact discriminated selection and scope types to the new detail-model file; move `WordTypesDetailState` there to keep `word-types.models.ts` focused.
4. Parse only the selection key compatible with `tableView`. Positive IDs only; labels never participate.
5. Add `clearSelectionForTableView(target)`: clear every incompatible selection key, leave the target key untouched, clear view/detailPage/location/column, and never generate `dim`.
6. Grammatical scope changes use `clearWordTypesSelection`; sort/page do not.
7. Keep `detailPage = 1` in parsed/facade state for a missing or invalid page on words/ayahs. Add one canonical detail-page serialization helper used by row selection, tab changes, pagination, and deep links: return `null` at page 1, return `String(page)` only when `page > 1`, and return `null` unconditionally for `view=surahs`. Do not change the generic merge builder's established “serialize supplied values” responsibility.
8. Add a short supersession note at the top of the historical 022 plan pointing to this plan; do not rewrite historical MVP sections.

Canonical examples the implementation and routing-contract tests must preserve:

```text
?tableView=roots&root=123&view=words
?tableView=stems&stem=456&view=ayahs&detailPage=2
?tableView=lemmas&lemma=789&view=surahs
```

The first URL restores internal page 1 without serializing it; the second restores page 2; the third restores internal page 1 and cannot retain a stale `detailPage`.

**Exact verification**

Run the focused test. Expected: all explicit-key, default-view, page-1 omission, page>1 serialization, surah removal, refresh/share, and history tests pass.

**Documentation belonging to this task**

- Update `frontend-routing-state.md` with exact keys, compatibility matrix, defaults, canonical page-1 omission/page>1 serialization/surah removal, and browser restoration.
- Update `spec.md` URL identity requirements.
- Add only the supersession note to the old 022 plan.

**Review checkpoint:** Search the Frontend and updated specs for `dim`; the only allowed occurrence is an explicit statement that it is forbidden. Inspect every `detailPage` URL write and confirm page 1 becomes `null`, pages above 1 are serialized, and surahs always produce `null` while internal state remains 1.

**Suggested commit boundary:** `feat(word-types): add grouped selection url state`

---

## Task 6: Frontend grouped API methods and cache keys

**Task goal:** Expose typed grouped reads with exact scope propagation and cache isolation matching the Backend.

**Exact files**

- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/data-access/word-types.api.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/data-access/word-types.api.spec.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-cache.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-cache.spec.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/README.md`
- Modify: `specs/019-word-types-explorer/contracts/frontend-routing-state.md`

**Existing symbols involved**

- `WordTypesApi.identityParams`, `WordTypesCacheKeys.table`, `ApiResponseCache.getOrLoad`.

**Interfaces**

- Consumes Task 4 routes and Task 5 types.
- Produces `getGroupedSummary`, `getGroupedMemberWords`, `getGroupedAyahMatches`, `getGroupedSurahs` plus matching cache keys for Task 7.

**Failing tests first**

In `word-types.api.spec.ts`:

- `getGroupedSummary_UsesPluralKindRouteAndPropagatesFullScope`.
- `getGroupedMemberWords_SendsPageAndPageSize`.
- `getGroupedAyahMatches_SendsPageAndPageSize`.
- `getGroupedSurahs_SendsNoPagingParams`.
- Use `test.each` for root/stem/lemma route conversion.

In `word-types-cache.spec.ts`:

- `groupedDetailKeysDifferByKindIdScopeAndView`.
- `groupedWordsAndAyahsDifferByPage`.
- `groupedSummaryAndSurahsHaveNoPageComponent`.
- `sameGroupedRequestProducesStableKey`.

Focused command:

```bash
npm test -- --include=src/app/features/words/data-access/word-types.api.spec.ts --include=src/app/features/words/state/word-types-cache.spec.ts
```

Expected pre-implementation failure: methods and grouped cache functions are undefined.

**Minimum implementation**

1. Add a typed `WordTypeGroupedRequestParams` carrying kind-specific ID plus full scope.
2. Convert singular internal kind to plural route segment with an exhaustive helper.
3. Always send `type`; send `childCode` when present; use `identityParams` for concrete case/tense/voice.
4. Only member words/ayahs accept page/pageSize.
5. Cache keys include kind, ID, type, child, case, tense, voice, view, and page for paged views.

**Exact verification**

Run the focused command. Expected: HTTP request URLs/params exactly match the Backend contract and all cache separation assertions pass.

**Documentation belonging to this task:** Update the frontend API/cache examples in `frontend-routing-state.md` and Frontend Words README.

**Review checkpoint:** Inspect the surahs request and key to confirm neither contains page/detailPage.

**Suggested commit boundary:** `feat(word-types): add grouped detail client contracts`

---

## Task 7: Kind-aware detail facade, loader, cancellation, retry, and not-found states

**Task goal:** Generalize the existing word detail pipeline to all four selection kinds while protecting state from stale responses.

**Exact files**

- Create: `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail.facade.spec.ts`
- Create: `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail-view.loader.spec.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types-detail.models.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail.facade.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail-view.loader.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail-panel.updates.ts`
- Modify: `specs/019-word-types-explorer/contracts/frontend-routing-state.md`

**Existing symbols involved**

- `WordTypesDetailFacade.bindToRoute`, `toPanelUrlState`, `syncFromUrlState`, `loadSummaryAndRestore`, `loadActiveView`.
- `WordTypesDetailViewLoader.loadActiveView`, existing `switchMap`/subscription cancellation, panel update helpers.

**Interfaces**

- Consumes Task 5 selection and Task 6 API/cache.
- Produces a page-ready kind-aware `WordTypesDetailState` and `retry()` for Tasks 8–9.

**Failing tests first**

Facade spec:

- `RootUrlWithoutDetailPage_RestoresRootSelectionWordsViewAndInternalPageOne`; data-driven stem/lemma variants.
- `PagedGroupedUrl_RestoresPageAboveOneAndBackToOmittedPageOne` using route emissions for `detailPage=2` followed by the same selection without `detailPage`.
- `WordUrl_RestoresWordSelectionAndKeepsAyahsDefault`.
- `BrowserBackForward_ReplacesKindSummaryAndActiveView`.
- `ScopeChange_ForSameDimensionId_LoadsNewScopedSummary`.
- `LaterSelectionWinsWhenEarlierSummaryRespondsLate`.
- `LaterViewOrPageWinsWhenEarlierDetailRespondsLate`.
- `MissingScopedDimensionProducesNotFoundWithoutClearingListState`.
- `TransportFailureProducesErrorAndRetryReloadsCurrentSelection`.

Loader spec:

- `GroupedWords_LoadsOnlyGroupedMemberWordsWithRequestedPage`.
- `Ayahs_DispatchesWordOrGroupedEndpointBySelectionKind`.
- `Surahs_DispatchesSingleShotEndpointAndIgnoresDetailPage`.
- `WordsView_IsRejectedForWordSelection`.
- `ChangingPageUsesSeparateCacheEntryForWordsAndAyahsOnly`.

Focused command:

```bash
npm test -- --include=src/app/features/words/state/word-types-detail.facade.spec.ts --include=src/app/features/words/state/word-types-detail-view.loader.spec.ts
```

Expected pre-implementation failure: detail state accepts only `WordTypeRowIdentity` and loader has no words/grouped dispatch.

**Minimum implementation**

1. Replace `selectedRow` with `selection: WordTypeDetailSelection|null` and add `kind`, grouped/word summary union, `words` page, ayahs, surahs.
2. Derive panel URL state from the active explicit selection key plus current scope.
3. Summary dispatch: word uses current endpoint/cache; grouped uses grouped endpoint/cache.
4. Loader dispatches by both selection kind and view. `words` exists only for grouped selection.
5. `isPaginatedWordTypeView` returns true for words/ayahs and false for surahs.
6. Preserve `detailPage = 1` in facade/panel state when a paged view is selected or restored without a URL page; URL omission is not a null internal page.
7. Keep `switchMap` and unsubscribe prior summary/detail loads. Add an active request key/generation check before every state update so a late non-cancellable response cannot overwrite a newer kind/scope/view/page.
8. `retry()` reloads summary when summary is absent, otherwise reloads the active view. Failures are not cached by `ApiResponseCache`.
9. A 404 becomes kind-aware not-found; controlled failure/transport error becomes retryable error.

**Exact verification**

Run the focused test. Expected: correct endpoint dispatch, defaults, history restoration, stale protection, errors, retry, and paging policy all pass.

**Documentation belonging to this task:** Update state/loading/error/not-found/retry behavior in `frontend-routing-state.md`.

**Review checkpoint:** Use delayed observables in tests; prove that response order cannot overwrite route order.

**Suggested commit boundary:** `feat(word-types): generalize detail orchestration`

---

## Task 8: Stable table strip, list transitions, and always-mounted details region

**Task goal:** Preserve `tableView` and keep the strip, table shell, and detail host mounted through every list transition.

**Exact files**

- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.labels.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.spec.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.html`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.spec.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.html`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.scss`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/README.md`
- Modify: `specs/019-word-types-explorer/spec.md`
- Modify: `specs/019-word-types-explorer/contracts/frontend-routing-state.md`

**Existing symbols involved**

- `selectType`, `selectChild`, `selectCase`, `selectTense`, `selectVoice`, `changeSort`, `loadList`, `handleTreeOnlyResponse`.
- `hasTableScope`, current page `@if` gates, `WordTypesTableComponent` loading skeleton.

**Interfaces**

- Consumes Task 7 page-ready detail state.
- Produces stable layout and retry events consumed by Task 9 content.

**Failing tests first**

Explorer facade spec:

- Replace old reset tests with `selectType_PreservesActiveTableView` and `selectChildNull_PreservesActiveTableView`.
- `test.each` for case/tense/voice/sort asserting tableView unchanged and page=1.
- `selectTableViewWords_IsTheOnlyActionThatReturnsAGroupedViewToWords`.
- `ScopeChangesClearOldScopedSelectionButSortAndListPagePreserveIt`.
- `TableViewChangeClearsOnlyIncompatibleSelectionKeys`.
- `TreeOnlyParentStatePreservesTableViewAndDoesNotRequestRows`.

Page spec:

- `stripRemainsVisibleAfterTreeLoadsForParentAndLeafScopes`.
- `activeGroupedViewRemainsHighlightedAcrossEveryScopeFilterChange`.
- `tableComponentHostIsTheSameNodeAcrossViewFilterLoadingPromptEmptyAndErrorTransitions`.
- `detailsComponentHostIsTheSameNodeAcrossWordsRootsStemsLemmasAndEmptySelection`.
- `selectPromptLoadingEmptyAndErrorRenderInsideWordTypesTable`.
- `tableViewSwitchNeverProducesAFrameWithoutShellOrSkeleton`.
- `listErrorRetryDelegatesToFacadeAndKeepsShellMounted`.

Focused commands:

```bash
npm test -- --include=src/app/features/words/state/word-types-explorer.facade.spec.ts
npm test -- --include=src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts
```

Expected pre-implementation failure: current main-type/parent selection resets to words; strip, table, and panel are conditionally absent.

**Minimum implementation**

1. Remove every implicit `tableView: DEFAULT_WORD_TYPE_TABLE_VIEW` write from main type/parent changes.
2. Clear all selection on grammatical scope changes; preserve selection on sort and list-page changes.
3. Keep tree data on a rows failure after tree success.
4. Render the table-view tabs whenever `tree !== null`, not only when `hasTableScope`.
5. Render `qd-word-types-table` unconditionally in the layout. Pass status/message and a retry output so it owns prompt/loading/empty/error states inside its body.
6. Remove outer prompt/error/empty branches that replace the table.
7. Render `qd-word-type-details-panel` unconditionally for every table view. Remove grouped full-width modifier and retain desktop split layout.
8. Add `retryList()` in the facade with a cancellable retry subscription; errors are not cached.
9. Keep skeleton rows noninteractive and ensure no blank body exists between route query update and loading state.

**Exact verification**

Run both focused commands. Expected: all host-identity, strip, view-preservation, in-shell state, and retry tests pass.

**Documentation belonging to this task**

- Update `spec.md` stable behavior requirements.
- Reverse grouped-panel/noninteractive/hidden-strip/reset statements in Frontend Words README.
- Update `frontend-routing-state.md` transition matrix.

**Review checkpoint:** Manually emit every route-state transition in the page test and verify DOM host identity, not only eventual content.

**Suggested commit boundary:** `fix(word-types): stabilize explorer layout transitions`

---

## Task 9: Selectable grouped rows and complete grouped details UI

**Task goal:** Connect grouped rows to scoped summary/words/ayahs/surahs while keeping member rows strictly display-only.

**Exact files**

- Create: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-grouped-words-list/word-type-grouped-words-list.component.ts`
- Create: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-grouped-words-list/word-type-grouped-words-list.component.html`
- Create: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-grouped-words-list/word-type-grouped-words-list.component.scss`
- Create: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-grouped-words-list/word-type-grouped-words-list.component.spec.ts`
- Create: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.ts`
- Create: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.html`
- Create: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.scss`
- Create: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.spec.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.labels.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.html`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.scss`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.spec.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.html`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.scss`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.spec.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.html`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.scss`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/README.md`
- Modify: `specs/019-word-types-explorer/spec.md`
- Modify: `specs/019-word-types-explorer/contracts/word-types-api.md`
- Modify: `specs/019-word-types-explorer/contracts/frontend-routing-state.md`

**Existing symbols involved**

- `WordTypesTableComponent.rowSelected`/`rowDomId`/`isSelected`.
- `WordTypeDetailsPanelComponent.tabs`/`onTabKeydown`.
- `AyahMatchesListComponent`, `SurahOccurrencesListComponent`, `MissingSurahsListComponent`, `PaginationComponent`.

**Interfaces**

- Consumes Task 7 detail state and Task 8 stable hosts.
- Produces the complete user-facing grouped details experience.

**Failing tests first**

Main table/page:

- `selectingRootStemLemmaWritesOnlyItsExplicitUrlKeyAndViewWordsWithoutDefaultDetailPage`.
- `selectedGroupedRowReceivesAriaSelectedAndDistinctState`.
- `pagePassesCorrectSelectionKindAndFullScopeToDetailFacade`.
- `newGroupedSelectionDefaultsToWordsTab`.
- `groupedSummaryDisplaysLabelOccurrencesAyahsAndSurahsMatchingSelectedRow`.
- `groupedWordsAndAyahsKeepInternalPageOneButOmitItFromUrlAndWriteOnlyPagesAboveOne`.
- `surahsAlwaysRemoveDetailPageFromUrlAndRemainInternalPageOne`.
- `groupedErrorRetryAndNotFoundRenderInsideMountedDetailsRegion`.

Details panel:

- `wordKindShowsAyahsAndSurahsTabs`.
- `rootStemLemmaKindsShowWordsAyahsAndSurahsTabsWithRtlRovingFocus`.
- `tabsStayDisabledForEmptySelectionButPanelSurfaceRemains`.

Grouped member component:

- `memberRowsRenderWordContextAndThreeScopedCounts`.
- `memberRowsContainNoButtonAnchorTabindexInteractiveSurfaceOrSelectedClass`.
- `clickingMemberRowEmitsNoEventAndDoesNotCallRouter`.
- `paginationIsTheOnlyEmittedInteraction`.
- `mobileMarkupKeepsLabelsReadableInRtl`.

Focused commands:

```bash
npm test -- --include=src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.spec.ts --include=src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.spec.ts --include=src/app/features/words/components/word-type-grouped-words-list/word-type-grouped-words-list.component.spec.ts
npm test -- --include=src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts
```

Expected pre-implementation failure: grouped rows emit nothing, the panel is word-only, and member components do not exist.

**Minimum implementation**

1. Change main-table `selectedRow` and `rowSelected` to the discriminated table-row union. Implement exact per-kind identity comparison.
2. Render grouped main rows as native `button type="button"` rows with `qd-explorer-table__row`, `aria-current`, `aria-selected`, and native Enter/Space behavior. Counts remain part of the row action, not separate drilldowns.
3. Page maps row kind to its explicit URL key, sets grouped internal state to `view=words`/page 1, omits `detailPage` from that canonical selection URL, and sends full scope to the detail facade.
4. Details panel receives current detail kind and derives available tabs. Use the existing RTL arrow/Home/End behavior over the kind-specific tab list.
5. Render `WordTypeDetailSummaryComponent` above active detail content for word and grouped summaries.
6. Render `WordTypeGroupedWordsListComponent` for grouped words. It has no row output and no Router dependency; only pagination emits.
7. Reuse ayah/surah/missing components unchanged. For words/ayahs, remove `detailPage` at page 1 and write it only above page 1; remove it for surahs regardless of prior URL state. Every URL omission still maps to internal page 1.
8. Keep the page host desktop-default and `matchMedia` guard. On mobile, the component host remains mounted; valid selection uses existing modal/trap/scroll-lock behavior.

**Exact verification**

Run the focused commands. Expected: correct kind/default/scope, complete content states, display-only member rows, and desktop/mobile component behavior pass.

**Documentation belonging to this task**

- Add grouped details, display-only member rule, and paging policy to `spec.md`, Frontend Words README, and `frontend-routing-state.md`.
- Ensure `word-types-api.md` says member identity fields are display data in this iteration, not a drilldown contract.

**Review checkpoint:** Search the new member component for `button`, `a`, `router`, `navigate`, `rowSelected`, `qd-interactive-surface`, and `qd-is-selected`; all must be absent.

**Suggested commit boundary:** `feat(word-types): add grouped details ui`

---

## Task 10: Row numbering, quiet hover, and row-state accessibility

**Task goal:** Bring all four main table views onto the established numbering and quiet explorer-row pattern.

**Exact files**

- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.labels.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.html`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.scss`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.spec.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/README.md`
- No global stylesheet change is required.

**Existing symbols involved**

- `ROW_NUMBER_HEADER` from `words-shared.labels.ts`.
- `pageRelativeRowNumber` from `unique-words-pagination-display.ts`.
- Shared `qd-explorer-table__header-cell--row-number`, `qd-explorer-table__cell--row-number`, `qd-explorer-table__row`, selected/loading exclusions.

**Interfaces**

- Consumes page/pageSize from `PagedResultDto`.
- Produces consistent numbering and final row visual/a11y behavior.

**Failing tests first in `word-types-table.component.spec.ts`**

- `allFourViewsRenderPageRelativeRowNumbersWithoutDatabaseIds`: page 2, pageSize 25 renders 26/27 for words, roots, stems, lemmas; ID values do not appear as visible text.
- `wordAndGroupedRowsUseQuietExplorerRowClassWithoutInteractiveSurface`.
- `selectedWordAndGroupedRowsExposeAriaSelectedCurrentAndSelectedClass`.
- `loadingRowsHaveLoadingClassNoInteractiveSurfaceAndNoSelectableAttributes`.
- `rowButtonsRemainKeyboardOperableAndFocusRowFindsEveryKind`.
- Update grouped-rendering tests from noninteractive to selectable main rows; do not weaken member-row tests from Task 9.

Focused command:

```bash
npm test -- --include=src/app/features/words/components/word-types-table/word-types-table.component.spec.ts
```

Expected pre-implementation failure: no row-number column; word row still has `qd-interactive-surface`; grouped main rows are divs.

**Minimum implementation**

1. Add `rowNumber: ROW_NUMBER_HEADER` to `WORD_TYPES_TABLE_HEADERS` through the TDZ-safe label import pattern.
2. Add `rowNumber(index)` calling `pageRelativeRowNumber(rows.page, rows.pageSize, index)`.
3. Add number header/cell/skeleton to word and grouped grids with shared number classes.
4. Add `qd-explorer-table__row` to real main rows; remove `qd-interactive-surface` from the whole row.
5. Update grid templates and mobile layout for the number column using logical properties.
6. Keep `word-types-table__row--loading` and shared hover exclusion. Do not add transform, shadow transition, or lift locally.

**Exact verification**

Run the focused test, then:

```bash
npm test -- --include=src/app/features/words/components/word-types-table/word-types-table.component.spec.ts --include=src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts
```

Expected: numbering/class/selection/loading/focus tests pass in jsdom without geometry assertions.

**Documentation belonging to this task:** Add row numbering, non-visible IDs, quiet hover, and selected/loading state rules to Frontend Words README.

**Review checkpoint:** Inspect rendered classes in all four views and confirm no table row owns the card-lift utility.

**Suggested commit boundary:** `style(word-types): align table row behavior`

---

## Task 11: Cross-stack verification, documentation closure, and clean-code/test guard

**Task goal:** Prove the complete plan meets every locked requirement without adding unrelated work.

**Exact files**

- Modify `specs/019-word-types-explorer/quickstart.md` with the final focused/full commands and manual acceptance flow.
- Only correct omissions discovered in files already owned by Tasks 1–10; do not start new refactors.

**Existing symbols involved**

- `QuranDashboard.sln`, `QuranDashboard.Tests.csproj`, the Angular `test`/`build` package scripts, and the focused Word Types test classes/specs named in Tasks 1–10.
- The clean-code guard references and Test Guard rules required by the workspace instructions.

**Interfaces**

- Consumes: every Backend route/reader/handler/cache contract and every Frontend URL/API/cache/facade/component contract produced by Tasks 1–10.
- Produces: no new runtime interface; it produces passing evidence, an updated quickstart, and a requirement-to-test trace suitable for engineering review.

**Failing tests/checks first**

Before final fixes, run the coverage matrix below and record any requirement with no passing test:

- Backend: summary/list parity; root/stem/lemma same-scope reads; head grain; null/marker exclusion; canonical text; member parity/context split; four measures; paging policy; cache isolation; invalid/not-found.
- Frontend: persistent strip/view/shell/panel; explicit URL restore/history; page-1 omission/page>1 serialization/surah removal with internal page 1; correct kind/default; member noninteractivity; scope/paging/cache/stale protection; numbering/hover/selection/loading; desktop/mobile/a11y.

Expected pre-verification failure reason: any uncovered matrix row, failing focused suite, build error, or documentation mismatch means the implementation is not complete even if individual tasks passed.

**Minimum implementation**

1. Run the focused matrix before broad builds so a failure maps to its owning task.
2. Fix only defects within the already-approved file inventory and rerun the smallest failing command.
3. Run full Word Types Backend tests and the complete focused Frontend set.
4. Run both production builds.
5. Perform the manual desktop/mobile flow and the clean-code/test-code checks.
6. Update `quickstart.md` with the commands that actually passed and the final manual flow; do not add a completion report.

**Exact verification commands**

Backend, working directory `Backend/`:

```bash
dotnet build QuranDashboard.sln --disable-build-servers -m:1 -p:BuildInParallel=false -p:RestoreDisableParallel=true -v minimal
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter "FullyQualifiedName~WordsWordTypes" --logger "console;verbosity=minimal"
```

Frontend, working directory `Frontend/quran-dashboard-ui/`:

```bash
npm test -- --include=src/app/features/words/**/*word-types*.spec.ts --include=src/app/features/words/components/word-type-grouped-words-list/*.spec.ts --include=src/app/features/words/components/word-type-detail-summary/*.spec.ts
npm run build
```

If the builder rejects the multi-pattern invocation, run these supported capped subsets instead:

```bash
npm test -- --include=src/app/features/words/state/word-types-*.spec.ts
npm test -- --include=src/app/features/words/data-access/word-types.api.spec.ts
npm test -- --include=src/app/features/words/components/word-type-*/**/*.spec.ts
npm test -- --include=src/app/features/words/components/word-types-table/*.spec.ts
npm test -- --include=src/app/features/words/pages/word-types-explorer-page/*.spec.ts
```

Expected passing result: zero failed focused tests, Backend build success, Frontend production build success.

**Manual desktop/mobile acceptance**

1. Open a parent scope with `tableView=roots`. Confirm the strip, table, and details region remain present while the subtype prompt is inside the table.
2. Change main type, child, case/tense/voice, and sort. Confirm roots remains active and no blank frame appears.
3. Select root/stem/lemma rows; confirm the URL contains the explicit identity and `view=words` but no page-1 `detailPage`. Refresh, share the URL, and use Back/Forward; confirm the correct kind, default words tab, and internal page 1 restore.
4. Page grouped words and ayahs. Confirm page 1 omits `detailPage`, page 2 writes `detailPage=2`, returning to page 1 removes it, and switching to surahs removes it regardless of the prior page.
5. Click/tap member rows and confirm nothing happens. Use pagination and confirm only pagination acts.
6. Verify row 26 on page 2, no visible DB IDs, quiet hover, visible keyboard focus, selected distinction, and no skeleton hover.
7. Check desktop split scrolling and mobile modal/RTL layout in both themes.

**Clean-code/test-code guard**

- Naming/functions: grouped kind/selection helpers remain exhaustive and focused.
- SOLID/DRY/KISS/YAGNI: no global explorer abstraction, no generic endpoint, no member drilldown.
- AI-code failure modes: no duplicated SQL grouping, speculative fallback, stale response write, or broad catch.
- Tests: behavior-based, data-driven variants, real DTOs, real PostgreSQL fixture for SQL, boundary-only HTTP mocks, source-safe fixture values.

**Documentation belonging to this task**

- Update quickstart commands and manual flow.
- Cross-check all earlier contract edits; this task must not postpone any route, URL, paging, grain, or member-interaction documentation from its owning task.

**Review checkpoint:** Formal requirement-to-test trace, then Quranic-data semantics review, then frontend interaction/accessibility review.

**Suggested commit boundary:** `test(word-types): verify grouped detail stability`

---

## Recommended execution order

Execute Tasks 1 → 11 in order. Tasks 2 and 3 may be reviewed independently after Task 1, but they both edit shared SQL/cache/controller files and should not be implemented concurrently in the same worktree. Frontend Tasks 5 and 6 can be prepared separately only after Backend names are frozen; Task 7 must precede Tasks 8–9.

## Highest-risk tasks

1. **Task 2:** numeric-ID-scoped member-word grouping parity. A text-label filter could merge same-label catalogue identities, while a distinct-word shortcut would hide legitimate contexts.
2. **Task 3:** ayah hydration provenance and bounded query shape.
3. **Task 7:** stale-response protection across four selection kinds and three detail views.
4. **Task 8:** stable DOM hosts and `tableView` preservation through route transitions.
5. **Task 9:** preventing member-word drilldown while enabling grouped main-row selection.

## Suggested review checkpoints

- After Task 1: Backend API/architecture and head-grain review.
- After Task 2: Quranic-data grouping/count parity review.
- After Task 4: complete Backend contract/cache/logging review.
- After Task 5: URL identity/refresh/history review.
- After Task 7: state-race and paging-policy review.
- After Task 9: product behavior, member noninteraction, desktop/mobile, and accessibility review.
- After Task 11: full engineering review and merge-readiness review.

## Plan self-review result

| Requirement family | Owning task(s) |
|---|---|
| Same-grain grouped summary and list-row parity | 1 |
| Numeric-ID member filtering plus word-context grouping/context split/measures/paging | 2 |
| Canonical scoped ayahs, highlighting, bounded queries | 3 |
| Single-shot surahs/missing surahs, HTTP/cache/logging | 4 |
| Explicit grouped URL identity, canonical detail-page omission/serialization, refresh/share/history | 5, 7, 9 |
| Frontend API scope propagation and cache separation | 6 |
| Kind-aware loading, stale protection, retry/not-found | 7 |
| Persistent strip/table/details and `tableView` preservation | 8 |
| Selectable grouped rows and display-only member UI | 9 |
| Row numbering, quiet hover, selection/loading a11y | 10 |
| Full commands, manual desktop/mobile, guard checks | 11 |

- Every inspection-report and locked user requirement maps to at least one numbered task.
- Task 2 filters and compares member parity only by numeric `root_id`, `stem_id`, or `lemma_id`; display text remains projection-only everywhere.
- Tasks 5, 7, and 9 keep internal `detailPage = 1`, omit page 1 from canonical URLs, serialize only pages greater than 1, and remove it for surahs.
- Every path and existing symbol was verified against the current repository; all new files are labelled Create.
- Backend/Frontend route, DTO, kind, scope, view, and paging names match; page-1 URL omission still restores internal `detailPage = 1`.
- No placeholder, generic `dim` key, segment expansion, surah paging, member drilldown, or implicit `tableView` reset remains.
- No migration, importer, package, config, production-data, branch, or unrelated-refactor work is included.
- Historical Feature 022 MVP decisions are preserved as history but explicitly superseded; current-truth READMEs and Feature 019 contracts are updated with their owning tasks.
- The plan is executable task-by-task without another architecture investigation.
