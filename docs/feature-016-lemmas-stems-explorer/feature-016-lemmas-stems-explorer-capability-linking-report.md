# Feature 016 — Lemmas & Stems Explorer — Capability + Linking Report

> Planning/capability verification only. No feature implementation, backend/frontend source changes,
> migrations, endpoints, routes, Spec Kit artifacts, or commits were produced. Database verification was
> read-only (`default_transaction_read_only=on`). Credentials used for local verification are not printed
> here.

| Item | Value |
| --- | --- |
| Feature | 016 — Lemmas & Stems Explorer |
| Proposed routes | `/dashboard/words/lemmas`, `/dashboard/words/stems` |
| Reference UX | Feature 014 Unique Words Explorer + Feature 015 Roots Explorer |
| Reference linking source | Feature 011 Mushaf Reader selected word analysis |
| Data posture | Read-only over existing Feature 004 morphology data |
| Final verdict | **READY WITH NOTES** |

---

## 1. Evidence Inspected

Source/report evidence:

- `Backend/report/database/current-database-tables-and-relationships-report.md`
- `Backend/report/feature-015-roots-explorer/roots-explorer-readonly-verification-report.md`
- `docs/feature-015-roots-explorer/feature-015-roots-explorer-combined-implementation-plan.md`
- `Frontend/report/feature-015-roots-explorer/roots-explorer-frontend-ux-contract-report.md`
- Backend morphology entities/configuration:
  `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/QuranRoot.cs`,
  `QuranLemma.cs`, `QuranStem.cs`, `WordMorphology.cs`, and matching EF configurations under
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Words/Morphology/`
- Existing roots backend:
  `Backend/api/QuranDashboard.Api/Controllers/Words/RootsController.cs`,
  `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/IRootsReader.cs`,
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Roots/EfRootsReader.cs`
- Existing unique words backend:
  `Backend/api/QuranDashboard.Api/Controllers/Words/UniqueWordsController.cs`
- Existing frontend Words/Roots state and routes:
  `Frontend/quran-dashboard-ui/src/app/features/words/words.routes.ts`,
  `state/roots-url-sync.ts`, `state/unique-words-url-sync.ts`, `models/roots.models.ts`,
  `models/unique-words.models.ts`, `data-access/roots.api.ts`, `data-access/unique-words.api.ts`
- Existing Mushaf route/linking state:
  `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-url-sync.ts`,
  `models/mushaf.models.ts`, `components/selected-word-section/*`,
  `components/word-morphology-summary/*`

Read-only database verification:

- Local PostgreSQL database: `quran_dashboard`
- `current_setting('transaction_read_only') = on`
- Queries printed only counts/plans/IDs, not Quran text or lexical display strings.

---

## 2. Data Readiness

### 2.1 Population and display values

| Check | Result |
| --- | ---: |
| Lemmas in `quran_lemmas` | 4,793 |
| Stems in `quran_stems` | 12,108 |
| Lemmas with non-empty Arabic display (`lemma_text`) | 4,793 |
| Lemmas with null/empty Arabic display | 0 |
| Stems with non-empty Arabic display (`stem_text`) | 12,108 |
| Stems with null/empty Arabic display | 0 |
| Lemmas with `root_id` ownership link | 4,640 |
| Lemmas with co-occurring root via `quran_word_morphology.root_id` | 4,640 |
| Stems with co-occurring lemma via morphology | 11,904 |
| Stems with co-occurring root via morphology | 11,678 |
| Morphology rows with null `lemma_id` | 4,925 |
| Morphology rows with null `stem_id` | 0 |
| Morphology rows with null `root_id` | 27,134 |

Assessment:

- `quran_lemmas` and `quran_stems` are populated and sufficient for read-only explorer pages.
- Arabic display values are complete for both lemmas and stems.
- All stems are attached to readable word morphology, but not every stem co-occurs with a lemma/root.
  That is a data fact, not a blocker; the UI must handle empty lemma/root fields for affected stems.
- Root-less morphology rows already exist and were accepted in Feature 015; Feature 016 must not invent
  root links for lemma/stem rows where no root exists.

### 2.2 Duplicate identity checks

| Check | Result |
| --- | ---: |
| Duplicate lemma Arabic display values (`lemma_text`) | 0 values / 0 extra rows |
| Duplicate stem Arabic display values (`stem_text`) | 0 values / 0 extra rows |
| Duplicate root Arabic display values (`root_text`) | 0 |
| Duplicate root Buckwalter values (`root_buckwalter`) | 0 |
| Duplicate lemma Buckwalter values (`lemma_buckwalter`) | 9 values / 9 extra rows |
| Duplicate stem Buckwalter/form key | Not applicable: `QuranStem` has no Buckwalter column; `stem_text` is unique. |

Assessment:

- Arabic display text is unique today for lemmas and stems, but it should still not be used as the URL
  identity. It is display content, not an explicit URL contract.
- Lemma Buckwalter is not unique. **Do not use `lemma_buckwalter` alone for deep links.**
- EF configurations enforce unique indexes on `quran_lemmas.lemma_text`, `quran_stems.stem_text`, and
  `quran_roots.root_text`, but numeric primary keys remain the safest URL identities.

### 2.3 Connectivity to readable Quran words and morphology segments

| Check | Result |
| --- | ---: |
| Lemmas not connected to readable `quran_words` through morphology | 0 |
| Stems not connected to readable `quran_words` through morphology | 0 |
| Lemmas not connected to readable words that have morphology segments | 0 |
| Stems not connected to readable words that have morphology segments | 0 |
| `quran_word_morphology` rows without matching `quran_words` | 0 |
| `quran_word_morphology` rows pointing to ayah markers | 0 |
| `quran_word_morphology` rows without segments | 0 |
| Bad morphology lemma/stem/root FK references | 0 / 0 / 0 |

Assessment:

- Every lemma and stem can be explored through existing readable word morphology.
- The existing ID-based highlight pattern used by Unique Words and Roots remains applicable: detail ayah
  matches can return `matchedQuranWordIds` and full ayah `words` without string replacement.

### 2.4 Scale and pagination implications

Whole-summary aggregation timing probes:

- Lemma summary shape over `quran_word_morphology JOIN quran_words`, grouped by `lemma_id` with counts
  for occurrences, roots, stems, ayahs, surahs, simple words, and tashkeel words:
  **216.9 ms** for 4,793 rows.
- Stem summary shape over the same join, grouped by `stem_id` with counts for occurrences, lemmas,
  roots, ayahs, surahs, simple words, and tashkeel words: **251.7 ms** for 12,108 rows.
- These were read-only local `EXPLAIN (ANALYZE, BUFFERS, TIMING)` probes. They are acceptable for a
  compute-once/cache-whole-list strategy, but should be rechecked in the implementation plan if the
  production database is materially different.

Worst observed aggregate sizes:

| Entity | Max occurrences | Max ayahs | Max surahs | Max related roots/lemmas | Max related stems | Max simple words | Max tashkeel words |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Lemma | 3,938 | 2,497 | 101 | 13 roots | 59 stems | 102 | 170 |
| Stem | 1,646 | 1,362 | 99 | 10 lemmas / 2 roots | n/a | 32 | 72 |

Pagination requirement:

- Lemma/stem list pages require pagination because 4,793 lemmas and 12,108 stems exceed comfortable
  table rendering and URL restore budgets.
- Ayah detail tabs require pagination for both pages.
- Word detail tabs require pagination for both pages.
- Surah, roots, lemmas-for-stem, and stems-for-lemma detail lists may be whole-list loads because their
  per-selected-entity maxima are bounded by 114 surahs, 13 roots/10 lemmas, and 59 stems/72 forms.

---

## 3. URL Identity and Deep-Linking

### 3.1 Existing route/query state

Roots Explorer:

- Route path helper: `rootsRoutePath()` in
  `Frontend/quran-dashboard-ui/src/app/core/navigation/route-paths.ts`
- Route: `/dashboard/words/roots`
- URL parser/builder: `Frontend/quran-dashboard-ui/src/app/features/words/state/roots-url-sync.ts`
- Existing query params from `ROOTS_QUERY_KEYS`:
  `search`, `sort`, `page`, `root`, `view`, `wordView`, `surahView`, `detailPage`
- Existing deep-link builder: `buildRootsDeepLink(options)` returns `{ path: rootsRoutePath(), queryParams }`
- Selection identity: numeric `root` query param (`rootId` in TypeScript changes).

Unique Words Explorer:

- Route helper: `uniqueWordsRoutePath(kind)`
- Routes: `/dashboard/words/unique/tashkeel`, `/dashboard/words/unique/simple`
- URL parser/builder: `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words-url-sync.ts`
- Existing query params from `UNIQUE_WORDS_QUERY_KEYS`:
  `search`, `sort`, `page`, `word`, `view`, `ap`
- Existing deep-link builder: `buildUniqueWordsDeepLink(kind, options)`.
- Selection identity: numeric `word` query param plus route mode (`simple` or `tashkeel`).

Mushaf Reader:

- Route path: `MUSHAF_ROUTE_PATH = '/dashboard/mushaf'`
- URL parser/builder: `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-url-sync.ts`
- Existing query params from `MUSHAF_URL_KEYS`:
  `page`, `ayah`, `focusAyah`, `word`, `segment`, `panel`, `ayahTab`, `wordTab`, `tafsirSource`,
  `translationSource`, `fullI3rabSource`
- Existing ayah deep-link builder: `buildMushafDeepLink({ pageNumber, ayah, focusAyah, panel })`.
- Ayah identity: `verseKey` string such as `2:255`, plus `page` for page load/focus.
- Word identity: location string in `word`, with `verseKeyFromWordLocation(word)` fallback.

### 3.2 Safest identifier per link target

| Target | Safest URL identity | Rationale |
| --- | --- | --- |
| Root | Numeric `quran_roots.id` in `root` query param | Existing Roots Explorer already uses `root` as positive int. Root text is display only. |
| Lemma | Numeric `quran_lemmas.id` in `lemma` query param | `lemma_text` is unique today but display content; `lemma_buckwalter` has duplicates. Use ID, optionally include display only for local UI state. |
| Stem | Numeric `quran_stems.id` in `stem` query param | `stem_text` is unique today but display content. Use ID for stability and consistency. |
| Ayah | `verseKey` plus `pageNumber` | Existing Mushaf deep links require `page`, `ayah`, `focusAyah`, `panel`. Use `verseKey` for ayah identity and page for routing/focus. |
| Unique simple word | Route mode `simple` + numeric `quran_words_unique_simple.id` in `word` | Existing Feature 014 contract. Do not use text/key alone. |
| Unique tashkeel word | Route mode `tashkeel` + numeric `quran_words_unique_tashkeel.id` in `word` | Existing Feature 014 contract. Do not use text alone. |

Recommendation:

- Use **numeric IDs as canonical URL identity** for root, lemma, stem, and unique words.
- Include display text only in frontend in-memory state or route navigation extras if useful for optimistic
  panel title rendering. Do not require display text to restore a shared URL.
- Do not use Arabic display text, Buckwalter, or normalized text as the canonical URL identity.

### 3.3 Recommended query params for Feature 016

Recommended Lemmas Explorer route:

```text
/dashboard/words/lemmas
```

Recommended Lemmas Explorer query params:

| Param | Values | Default | Meaning |
| --- | --- | --- | --- |
| `search` | Arabic lemma text or Buckwalter search text | empty | List search. Backend should normalize Arabic display text and optionally search Buckwalter if product wants Latin input. |
| `sort` | `mushaf-order`, `occurrences`, `alpha` | `mushaf-order` | Same convention as Unique Words and Roots. `occurrences` means descending. |
| `page` | positive int | `1` | List page. |
| `lemma` | positive int | none | Selected lemma ID. |
| `view` | `words`, `ayahs`, `surahs`, `stems` | `words` | Active detail tab. |
| `wordView` | `simple`, `tashkeel` | `simple` | Only when `view=words`. |
| `surahView` | `mentioned`, `missing` | `mentioned` | Only when `view=surahs`. |
| `detailPage` | positive int | `1` | Only for paged detail views (`words`, `ayahs`). |

Recommended Stems Explorer route:

```text
/dashboard/words/stems
```

Recommended Stems Explorer query params:

| Param | Values | Default | Meaning |
| --- | --- | --- | --- |
| `search` | Arabic stem text | empty | List search. |
| `sort` | `mushaf-order`, `occurrences`, `alpha` | `mushaf-order` | Same convention as Unique Words and Roots. |
| `page` | positive int | `1` | List page. |
| `stem` | positive int | none | Selected stem ID. |
| `view` | `words`, `ayahs`, `surahs`, `lemmas` | `words` | Active detail tab. |
| `wordView` | `simple`, `tashkeel` | `simple` | Only when `view=words`. |
| `surahView` | `mentioned`, `missing` | `mentioned` | Only when `view=surahs`. |
| `detailPage` | positive int | `1` | Only for paged detail views (`words`, `ayahs`). |

Do not add `lemmaText`, `stemText`, `rootText`, or Buckwalter to canonical URL state. If a combined
state is desired for initial perceived speed, use it as optional non-authoritative display state only,
not as the lookup key.

### 3.4 Deep-link examples

Root details from Lemmas/Stems:

```text
/dashboard/words/roots?root=55&view=words&wordView=simple
```

Lemma details from Stems or Mushaf Reader:

```text
/dashboard/words/lemmas?lemma=130&view=words&wordView=simple
```

Stem details from Lemmas or Mushaf Reader:

```text
/dashboard/words/stems?stem=5&view=words&wordView=simple
```

Ayah selection/focus in Mushaf Reader:

```text
/dashboard/mushaf?page=2&ayah=2:255&focusAyah=2:255&panel=ayah
```

Unique simple word detail through existing Feature 014:

```text
/dashboard/words/unique/simple?word=123&view=ayahs
```

Unique tashkeel word detail through existing Feature 014:

```text
/dashboard/words/unique/tashkeel?word=456&view=ayahs
```

Notes:

- Replace numeric IDs with actual IDs from DTOs.
- For ayah links from Lemmas/Stems details, the backend should return `verseKey` and `pageNumber`, as
  Roots already does in `RootAyahMatchDto` (`verseKey`, `pageNumber`). The frontend can call the
  existing `buildMushafDeepLink`.

---

## 4. Backend Readiness

### 4.1 Existing tables/entities/relationships

Required existing tables:

- `quran_lemmas`: `id`, `lemma_text`, `lemma_buckwalter`, `root_id`, `words_count`,
  `first_word_order_in_mushaf`
- `quran_stems`: `id`, `stem_text`, `words_count`, `first_word_order_in_mushaf`
- `quran_roots`: `id`, `root_text`, `root_buckwalter`, `words_count`, `distinct_lemmas_count`,
  `first_word_order_in_mushaf`
- `quran_word_morphology`: `quran_word_id`, `root_id`, `lemma_id`, `stem_id`, `head_pos`, features
- `quran_words`: word occurrence identity, ayah/page/surah/word position, `unique_simple_word_id`,
  `unique_tashkeel_word_id`
- `quran_ayahs` and `quran_surahs`: ayah metadata and Arabic surah names
- `quran_words_unique_simple` and `quran_words_unique_tashkeel`: existing Unique Words targets

Relevant EF entities:

- `QuranLemma` includes `Id`, `LemmaText`, `LemmaBuckwalter`, optional `RootId`, `WordsCount`,
  `FirstWordOrderInMushaf`.
- `QuranStem` includes `Id`, `StemText`, `WordsCount`, `FirstWordOrderInMushaf`; it has no `RootId`.
- `WordMorphology` includes optional `RootId`, `LemmaId`, `StemId` and navigation to all three.

Migration requirement:

- **No migration is needed** for the requested read-only feature. All required tables, IDs, display
  values, and relationships already exist.

### 4.2 Recommended endpoints

Prefer explicit endpoints over a generic morphology explorer endpoint. The pages have similar patterns
but different identity names, columns, relationship semantics, and DTOs. Clarity is safer than an
over-generic controller.

Recommended Lemmas API base:

```text
GET /api/words/lemmas
GET /api/words/lemmas/{id}
GET /api/words/lemmas/{id}/words/{wordKind}
GET /api/words/lemmas/{id}/ayahs
GET /api/words/lemmas/{id}/surahs
GET /api/words/lemmas/{id}/missing-surahs
GET /api/words/lemmas/{id}/stems
```

Recommended Stems API base:

```text
GET /api/words/stems
GET /api/words/stems/{id}
GET /api/words/stems/{id}/words/{wordKind}
GET /api/words/stems/{id}/ayahs
GET /api/words/stems/{id}/surahs
GET /api/words/stems/{id}/missing-surahs
GET /api/words/stems/{id}/lemmas
```

Root links do not need a new roots endpoint; reuse existing:

```text
GET /api/words/roots/{id}
```

For list rows, each summary item should include:

- Canonical ID (`id`)
- Display text (`lemmaText` or `stemText`)
- Related root/lemma display and ID where applicable
- `headPos`/type if the product means morphology `head_pos` by `النوع`; this needs product definition
  because one lemma/stem can occur with multiple POS/head types.
- Summary counts matching the locked table columns.

### 4.3 Query/read-model strategy

Mirror Feature 014 and 015:

- Application.Abstractions: `ILemmasReader`, `IStemsReader`, DTOs, sort/kind types.
- Application: query handlers with validation, outcome unions, structured logging.
- Infrastructure: EF readers using `AsNoTracking()` projections and cache decorators.
- Api: thin controllers returning `ApiResponse<T>`.

Expected query shapes can be implemented with `AsNoTracking()` projections:

- List summaries: grouped aggregation over `quran_word_morphology JOIN quran_words`, then join to
  `quran_lemmas`/`quran_stems` and optionally root/lemma display rows.
- Details words: group by `unique_simple_word_id` or `unique_tashkeel_word_id` for selected lemma/stem.
- Details ayahs: same batched shape as `EfRootsReader.GetRootAyahMatchesAsync`, filtering by
  `lemma_id` or `stem_id` and returning `matchedQuranWordIds`.
- Surahs/missing: distinct `surah_number` with counts, then 114-surah complement.
- Related roots/lemmas/stems: distinct IDs from morphology, joined to display tables.

Pagination:

- List endpoints: yes.
- `/words/{wordKind}` detail endpoints: yes.
- `/ayahs`: yes.
- `/surahs`, `/missing-surahs`, `/stems` for lemma, and `/lemmas` for stem: no pagination needed unless
  product later asks for user-configurable page sizes; current maxima are bounded.

Caching:

- Reuse the Feature 015 compute-once/cache-whole-list pattern for summary lists if the implementation
  plan accepts the measured ~217 ms lemma and ~252 ms stem full aggregations.
- Use bounded cache namespaces such as `lemmas:` and `stems:`.
- Do not cache raw free-text search keys if search is handled by DB per request. If using cached whole
  list, apply search/sort/page in memory and avoid per-search cache entries.

Indexes:

- Existing indexes cover identity and relationship access:
  `quran_word_morphology.lemma_id`, `stem_id`, `root_id`; unique indexes on `quran_lemmas.lemma_text`,
  `quran_stems.stem_text`, and first occurrence ordering.
- Whole-list aggregations chose sequential scans/hash joins/group aggregates, which is expected for
  bounded whole-table reads. No speculative index is recommended.
- If implementation later rejects cache-whole-list and repeatedly performs searched/page-scoped DB
  aggregations, measure first. Only propose new indexes with `EXPLAIN ANALYZE` evidence.

---

## 5. Frontend Readiness

### 5.1 Reusable Feature 014/015 patterns

Reuse directly or by close adaptation:

- Routing under existing Words area:
  `Frontend/quran-dashboard-ui/src/app/features/words/words.routes.ts`
- Route-path helpers:
  `Frontend/quran-dashboard-ui/src/app/core/navigation/route-paths.ts`
- URL parse/build discipline:
  `state/roots-url-sync.ts` and `state/unique-words-url-sync.ts`
- Deep-link helpers:
  `buildRootsDeepLink`, `buildUniqueWordsDeepLink`, `deepLinkToHref`
- Split table + persistent detail panel pattern:
  `pages/roots-explorer-page/*`, `components/roots-table/*`, `components/root-details-panel/*`
- Shared detail components:
  `highlighted-ayah`, `ayah-matches-list`, `surah-occurrences-list`, `missing-surahs-list`,
  `word-count-chip`, `root-words-list` pattern
- Shared pagination:
  `Frontend/quran-dashboard-ui/src/app/shared/ui/pagination/pagination.component.*`
- Frontend cache pattern:
  `roots-cache.ts`, `unique-words-cache.ts`, `core/caching/api-response-cache.ts`

### 5.2 Recommended folder structure

Prefer keeping both pages inside the existing `features/words` feature:

```text
src/app/features/words/
  pages/lemmas-explorer-page/
  pages/stems-explorer-page/
  components/lemmas-table/
  components/stems-table/
  components/lemma-details-panel/
  components/stem-details-panel/
  components/lemma-words-list/
  components/stem-words-list/
  components/lemma-stems-list/
  components/stem-lemmas-list/
  data-access/lemmas.api.ts
  data-access/stems.api.ts
  state/lemmas-*.ts
  state/stems-*.ts
  models/lemmas.models.ts
  models/stems.models.ts
```

This is clearer than a new top-level `features/lemmas` or `features/stems` because the routes live in
the Words area and the UI must reuse the Words/Roots visual system.

### 5.3 Visual and interaction constraints

Confirmed direction:

- Reuse the Roots Explorer split-screen page structure, SCSS tokens, `qd-card`, `qd-btn`, table grid,
  count chip, panel header, tab strip, loading/error/empty/not-found states, and responsive behavior.
- Keep Arabic-first RTL behavior and logical CSS properties.
- No new color palette, no new visual language, no redesign.
- No modal as the main desktop detail experience. Roots currently uses an inline panel on desktop and
  modal-like overlay only when `inline()` is false for smaller screens; Feature 016 should follow the
  same responsive adaptation but keep desktop as a persistent detail panel.
- Keep Quran text rendering stable and unanimated.

Locked page-specific tables/tabs from the brief:

- Lemmas table columns:
  `الصيغة المعجمية`, `الجذر`, `النوع`, `المواضع`, `الآيات`, `السور`, `كلمات بدون تشكيل`,
  `كلمات بالتشكيل`, `الأصول الصرفية`.
- Lemma panel tabs:
  `الكلمات` (`بدون تشكيل`, `بالتشكيل`), `الآيات`, `السور` (`وردت فيها`, `لم ترد فيها`),
  `الأصول الصرفية`.
- Stems table columns:
  `الأصل الصرفي`, `الصيغة المعجمية`, `الجذر`, `النوع`, `المواضع`, `الآيات`, `السور`,
  `كلمات بدون تشكيل`, `كلمات بالتشكيل`.
- Stem panel tabs:
  `الكلمات` (`بدون تشكيل`, `بالتشكيل`), `الآيات`, `السور` (`وردت فيها`, `لم ترد فيها`),
  `الصيغ المعجمية`.

### 5.4 Tests needed

Frontend tests:

- URL parse/build/restore for `/dashboard/words/lemmas` and `/dashboard/words/stems`.
- Invalid query values default safely; selection IDs must be positive ints.
- Row selection opens the details panel and restores on refresh/back-forward.
- Count click mapping opens the correct tab/sub-view and clears irrelevant sub-view params.
- Cross-page navigation:
  root link → `/dashboard/words/roots?root=...`, lemma link → `/dashboard/words/lemmas?lemma=...`,
  stem link → `/dashboard/words/stems?stem=...`, ayah link → `buildMushafDeepLink`, unique word link →
  `buildUniqueWordsDeepLink`.
- Lazy loading: table render must not fetch details.
- Loading/error/empty/not-found states match Roots Explorer behavior.
- Accessibility: count cells are buttons, links have labels, selected row has a non-color-only state,
  panel tabs have `role="tablist"`/`role="tab"`, and keyboard navigation works.

Backend tests:

- List summary counts for representative lemmas/stems.
- Duplicate/identity guard: URLs use IDs; display/Buckwalter are not used for lookup.
- Missing optional root/lemma relationships render controlled null values.
- Ayah match highlighting returns exact `matchedQuranWordIds`.
- Mentioned/missing surahs sum to 114.
- Unique simple/tashkeel word detail IDs are present in word detail rows.
- Cache behavior and query count bounds, modeled on Feature 014/015 tests.

---

## 6. Mushaf Reader Linking Readiness

### 6.1 Current selected word DTO/view model

Backend DTO:

- `Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/Responses/WordAnalysisResponse.cs`
- `WordMorphologyDto` currently contains:
  - `Root`: `WordMorphologyRoot(int Id, string? Text, string? Buckwalter)`
  - `Lemma`: `WordMorphologyLemma(string? Text, string? Buckwalter)`
  - `Stem`: `WordMorphologyStem(string? Text)`

Frontend model:

- `Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts`
- `WordMorphologyDto` currently contains:
  - `root: { id: number; text: string | null; buckwalter: string | null } | null`
  - `lemma: { text: string | null; buckwalter: string | null } | null`
  - `stem: { text: string | null } | null`

Current selected word UI:

- `SelectedWordSectionComponent` already builds root links using `buildRootsDeepLink({ rootId })`.
- `WordMorphologySummaryComponent` renders root as an anchor when `rootExplorerHref` exists.
- Lemma and stem are currently display-only spans because the DTO lacks `lemma.id` and `stem.id`.
- Unique simple/tashkeel identity links already exist using `buildUniqueWordsDeepLink` and the unique
  word IDs in `WordIdentityDto`.

### 6.2 Minimal DTO additions required

To support Mushaf Reader → Lemmas/Stems Explorer links, add IDs to the selected word morphology DTOs:

```csharp
public sealed record WordMorphologyLemma(
    int Id,
    string? Text,
    string? Buckwalter);

public sealed record WordMorphologyStem(
    int Id,
    string? Text);
```

And matching TypeScript model fields:

```ts
lemma: { id: number; text: string | null; buckwalter: string | null } | null;
stem: { id: number; text: string | null } | null;
```

Backend mapping source is already available in `EfWordAnalysisReader.MapMorphology(...)`: the method
loads `QuranLemma` and `QuranStem` by `morphology.LemmaId` and `morphology.StemId`. It currently maps
only display text; adding IDs is a minimal projection/contract change, not a schema change.

### 6.3 Click behavior from Mushaf Reader

Recommended behavior after DTO IDs exist:

- Root click:
  `/dashboard/words/roots?root={rootId}&view=words&wordView=simple`
- Lemma click:
  `/dashboard/words/lemmas?lemma={lemmaId}&view=words&wordView=simple`
- Stem click:
  `/dashboard/words/stems?stem={stemId}&view=words&wordView=simple`
- Unique word clicks remain unchanged:
  `/dashboard/words/unique/tashkeel?word={uniqueTashkeelId}&view=ayahs` and
  `/dashboard/words/unique/simple?word={uniqueSimpleId}&view=ayahs`.

Open question for implementation plan:

- Current Mushaf links use regular anchors with `target="_blank"`. Feature 016 should decide whether
  lemma/stem/root links should preserve that behavior for consistency or navigate in the same tab. The
  safest planning default is to preserve existing Mushaf behavior unless product requests otherwise.

---

## 7. Risks / Blockers

### BLOCKING

- None found. Existing data is sufficient and no migration is required.

### MAJOR

- **Mushaf Reader lemma/stem links need IDs.** Current selected word morphology DTOs include root ID
  but lemma/stem display only. Implementation must add `lemma.id` and `stem.id` to the word-analysis
  API contract before Mushaf Reader can deep-link safely to the new explorers.
- **Define `النوع` precisely.** The requested Lemma/Stem table includes `النوع`, but the existing data
  exposes `head_pos` per word occurrence. A lemma or stem can plausibly appear with multiple POS/head
  types. The plan/spec must define whether `النوع` means dominant/first POS, a set of POS labels, or a
  filterable summary. Do not guess in implementation.

### MINOR

- **Lemma Buckwalter is duplicated.** There are 9 duplicate Buckwalter values, so Buckwalter cannot be
  a unique URL key. Numeric IDs solve this.
- **Some stems lack co-occurring root/lemma relationships.** 11,678 of 12,108 stems co-occur with roots;
  11,904 co-occur with lemmas. The UI/API must support null/empty related root/lemma values without
  treating them as data errors.
- **Current Roots route already exists.** Feature 016 should extend linking into existing Roots rather
  than rework Roots URL state.
- **List summary full aggregation is larger than Roots.** Lemma and stem whole-summary probes are still
  acceptable (~217 ms and ~252 ms locally), but the implementation plan should re-run or cite these
  measurements before locking cache-whole-list.

### NOTE

- No duplicate Arabic display values exist for lemmas or stems today, but display text remains display
  content, not canonical identity.
- Existing Roots Explorer default row selection is `view=words&wordView=simple` in current code
  (`DEFAULT_ROOT_VIEW = 'words'`), matching the desired persistent detail panel pattern.
- Existing Mushaf ayah deep link contract already supports selecting/focusing an ayah via
  `page`, `ayah`, `focusAyah`, and `panel=ayah`.

---

## 8. Final Verdict

### READY WITH NOTES

Feature 016 can proceed to planning/specification. The morphology data is present, populated, uniquely
displayable, connected to readable segmented words, and queryable through existing read-only patterns.
No migration or speculative index is justified.

The combined plan should lock these before Spec Kit artifacts are created:

- Numeric-ID URL contracts: `lemma`, `stem`, existing `root`, existing unique-word `word`.
- Exact `النوع` semantics for lemmas/stems.
- Minimal Mushaf `WordMorphologyLemma.Id` and `WordMorphologyStem.Id` DTO additions.
- Explicit endpoint contracts for `/api/words/lemmas` and `/api/words/stems`.
- Reuse of Roots Explorer split-screen UI with no new visual language.

Recommended next step: **Combined Implementation Plan**.

**READY WITH NOTES**
