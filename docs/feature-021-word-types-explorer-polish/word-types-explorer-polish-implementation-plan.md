# Word Types Explorer — Polish & Semantics Implementation Plan

Branch: `021-word-types-explorer-polish`
Source of truth (audit): `docs/feature-021-word-types-explorer-polish/word-types-explorer-polish-and-semantics-audit-report.md`
Status: plan only — no code was written and no source files were modified.

This plan is written to be handed to an implementation agent phase by phase. Each step names exact
files, functions, and the precise change. Do not deviate from the locked product decisions or the
hard constraints below.

---

## 0. Locked product decisions (do not renegotiate)

1. Top parent categories stay exactly four: Nouns (`noun`), Verbs (`verb`), Particles & Tools
   (`particle`), Disjoint Letters (`inl`).
2. Parent counts:
   - `noun` / `verb` / `particle`: count = number of **visible/used child subtypes** (children with
     `count > 0`).
   - `inl`: count = **1** (single leaf).
   - Never show word/occurrence counts beside a parent.
3. Parent selection:
   - Clicking `noun` / `verb` / `particle` must **not** load table rows. It selects/opens the parent
     and prompts the user to choose a subtype. The table shows a clean "select a subtype" empty state.
   - Rows load only after a real child subtype/leaf is selected.
   - `inl` is the only parent that loads directly (it is already a leaf).
4. Subtype visibility: hide every zero-count subtype (noun, verb, particle); exclude zero-count
   subtypes from parent counts. Do **not** edit `PosTagSeed`. Do **not** rename/merge `T`/`TIM`; the
   visible duplicate `ظرف زمان` disappears only because the zero-count one is hidden.
5. Particles & Tools: expose real child classifications from the POS catalogue; exclude `INL`
   (Disjoint Letters is its own main category). Particle child codes must be accepted by backend
   validation and preserved by frontend URL state.
6. UI/layout: match the established explorer shell (Roots/Lemmas/Stems/Unique Words); reuse the Roots
   layout pattern; fix the broken table scroll; table and details panel get independent bounded
   scroll regions; no new visual language; no unrelated global redesign.

## Hard constraints

- No EF migrations. No importer/DataPipeline changes. No `PosTagSeed` edits.
- No Quran text/data mutation. Feature stays read-only.
- No changes to Roots/Lemmas/Stems/Unique Words source (reference only; they may be read but not
  edited). Reuse their shared `explorer-*` / `qd-*` classes and tokens.
- No package changes (none are required by this plan).
- Test fixture files under `Backend/tests/…` (including `word-types-explorer-seed.sql`) are **not**
  production data and may be edited — that is not a `PosTagSeed` or importer change.

---

## 1. Executive summary

**Current problem.** The Word Types Explorer (1) loads all words of a parent category the moment the
parent is clicked; (2) renders zero-count noun subtypes, including a duplicate `ظرف زمان`; (3) shows a
row count (not a used-subtype count) beside each parent; (4) exposes no child classifications for
Particles & Tools; and (5) has no working vertical scroll — the table has only `overflow-x`, the page
never establishes a viewport-bound height, so the details panel's `height:100%` is inert and the whole
page grows instead of scrolling in independent panes.

**Target behavior.** Four parents with used-subtype counts (`inl`=1); parent clicks open the subtype
picker and show a "select a subtype" empty state without loading rows; `inl` loads directly; zero-count
subtypes are hidden and excluded from counts; particles expose their catalogue subtypes (excluding
`INL`); and the page adopts the Roots explorer shell so the table body and details panel each scroll
independently within a viewport-bound layout.

**Why this needs no migration/importer/data change.** The full POS taxonomy already exists in the
`quran_pos_tags` table (seeded from `PosTagSeed`, ~34 particle codes + noun codes incl. `T`/`TIM`). All
fixes are read-model/query shaping (`EfWordTypesReader` + its SQL), validation allow-lists, frontend
state gating, and CSS/template restructuring. Nothing changes the schema, the importer, or any Quran
row. The duplicate `ظرف زمان` is resolved by *hiding* the zero-count node in the read model, not by
altering the source taxonomy.

---

## 2. File inventory

### Backend — to update
| File | Change |
| --- | --- |
| `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs` | `GetTreeAsync`: add catalogue-driven particle children; hide zero-count children; parent count = used-subtype count; `inl`=1; drop dependency on `TreeCountsSql`. |
| `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.Sql.cs` | Add `particle_children` CTE to `TreeChildCountsSql`; add particle branch to `ChildCodePredicate`; remove now-unused `TreeCountsSql`/`TreeCountRow` (or leave dead-code-free). |
| `Backend/application/QuranDashboard.Application/Quran/Words/WordTypes/Queries/WordTypesHandlerValidation.cs` | Add `ParticleChildCodes` set; extend `IsValidChildCode` to accept particle child codes (excluding `INL`). |

### Frontend — to update
| File | Change |
| --- | --- |
| `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.models.ts` | Add `'selectPrompt'` to `WordTypesLoadStatus`. |
| `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.labels.ts` | Add "select a subtype" empty-state label. |
| `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.ts` | Gate row loading on leaf selection in `loadList()`; tree-only path for parent selection. |
| `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.ts` | `normalizeChildCode`: pass particle child codes through (drop only for `inl`). |
| `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.ts` | On parent select, open the subtype panel for parents with children. |
| `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.html` | Render `selectPrompt` empty state; gate table+pagination; wrap projected panel content for independent scroll. |
| `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.scss` | Adopt Roots-style shell + `dvh`-based card height. |
| `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.html` | Split header (non-scrolling) from a scrollable body. |
| `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.scss` | Table becomes flex column with bounded scrollable body (mirror roots-table). |

### Tests — to update / add
| File | Change |
| --- | --- |
| `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesMainReadTests.cs` | Update main-count expectations (noun 4→3, verb 4→3); rewrite/remove `Rows_TotalCount_EqualsTreeCount_ForMainTypes` (parent count ≠ row count now). |
| `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesSubtypeReadTests.cs` | Particle now has children (`PRO`), zero-count `P` hidden, `inl` still empty; remove `("particle","P",InvalidFilter)` reject case; add particle-child success cases. |
| `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesSecondaryFilterReadTests.cs` | Re-check the `noun.Count` assertion (line ~64) against new used-subtype semantics; keep particle secondary-filter rejections. |
| `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesChildCatalogueDriftTests.cs` | Add particle-code coverage: every non-`INL` particle catalogue code is accepted; `INL` is rejected as a particle child. |
| `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/word-types-explorer-seed.sql` | (Optional, for the duplicate-label test) add a zero-count noun code pair `T`/`TIM` sharing `ظرف زمان`, with a row only for `T`. |
| `Frontend/…/state/word-types-url-sync.spec.ts` | Particle child code round-trips (no longer dropped). |
| `Frontend/…/components/word-type-filter/word-type-filter.component.spec.ts` | Particle exposes an expander + children. |
| `Frontend/…/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts` | Parent click → no rows request + `selectPrompt` empty state; subtype click → rows request; `inl` loads directly. |

### Reference only (read, never edit)
- Shell: `Frontend/…/pages/roots-explorer-page/roots-explorer-page.component.{html,scss}`
- Table scroll pattern: `Frontend/…/components/roots-table/roots-table.component.scss`
- Panel host pattern: `Frontend/…/components/root-details-panel/root-details-panel.component.scss`
- Catalogue source of truth: `Backend/…/MorphologyImporting/PosTagSeed.cs`

---

## 3. Phase 1 — Semantics (backend read model + frontend state)

### 3.1 Backend: particle children from the catalogue

File: `EfWordTypesReader.cs` → `GetTreeAsync`.

1. After the existing `nounChildCounts` / `verbChildCounts` derivation, add:
   ```csharp
   var particleChildCounts = childCounts
       .Where(row => row.Type == ParticleType)
       .ToDictionary(row => row.ChildCode, row => row.Count);
   ```
2. Build the particle catalogue exactly like nouns, excluding `INL`:
   ```csharp
   var particleCatalogue = await _dbContext.PosTags.AsNoTracking()
       .Where(pos => pos.Category == ParticleType && pos.Code != InlPos)
       .OrderBy(pos => pos.SortOrder)
       .Select(pos => new PosCatalogueRow(pos.Code, pos.ArabicLabel))
       .ToListAsync(cancellationToken);
   var particleChildren = particleCatalogue
       .Select(pos => ChildNode(pos.Code, pos.ArabicLabel, particleChildCounts.GetValueOrDefault(pos.Code)))
       .ToList();
   ```
   `InlPos` (`"INL"`) already exists as a constant in this class.

File: `EfWordTypesReader.Sql.cs` → `TreeChildCountsSql`.

3. Add a `particle_children` CTE mirroring `noun_children` but for the particle category minus `INL`,
   and union it into `all_children`:
   ```sql
   ), particle_children AS (
       SELECT '{ParticleType}' AS type, head_pos AS child_code, tashkeel_word_id
       FROM base
       WHERE pos_category = '{ParticleType}' AND head_pos <> '{InlPos}'
       GROUP BY head_pos, tashkeel_word_id
   ), all_children AS (
       SELECT * FROM noun_children
       UNION ALL SELECT * FROM verb_children
       UNION ALL SELECT * FROM particle_children
   )
   ```

File: `EfWordTypesReader.Sql.cs` → `ChildCodePredicate`.

4. Add the particle branch so a selected particle child pins the head POS (same shape as noun):
   ```csharp
   ParticleType => "m.head_pos = @childCode",
   ```
   (Leave the `_ => "FALSE"` default for `inl`.) `TypePredicate` for particle already emits
   `AND pos.category = 'particle' AND m.head_pos <> 'INL'`, so a particle child is a strict subset.

### 3.2 Backend: child-code validation for particles

File: `WordTypesHandlerValidation.cs`.

5. Add a `ParticleChildCodes` allow-set = the hand-mirrored list of all `category = "particle"` codes
   from `PosTagSeed` **except** `INL`. Current set (keep in sync with `PosTagSeed`, guarded by the
   drift test):
   ```
   P, CONJ, NEG, VOC, IMPV, ACC, EMPH, REM, ANS, PRO, FUT, INTG, COND, PREV, CAUS, AMD, EXL,
   RES, PRP, COM, DET, SUB, AVR, CERT, CIRC, EQ, EXH, EXP, INC, INT, RET, RSLT, SUP, SUR
   ```
6. Extend `IsValidChildCode`:
   ```csharp
   public static bool IsValidChildCode(string? type, string? childCode) =>
       string.IsNullOrWhiteSpace(childCode)
       || (type == NounType && NounChildCodes.Contains(childCode))
       || (type == VerbType && VerbChildCodes.Contains(childCode))
       || (type == ParticleType && ParticleChildCodes.Contains(childCode));
   ```
   `INL` is intentionally absent from `ParticleChildCodes`, so `IsValidChildCode("particle","INL")`
   stays `false`. Validation accepts every catalogue particle code even when its tree count is 0 (the
   UI hides zero-count children, but a deep-link to a zero-count child must return 200-empty, not 400).

### 3.3 Backend: hide zero-count children + parent counts

File: `EfWordTypesReader.cs` → `GetTreeAsync`.

7. Filter zero-count children for all three parents that have children:
   ```csharp
   nounChildren = nounChildren.Where(c => c.Count > 0).ToList();
   verbChildren = verbChildren.Where(c => c.Count > 0).ToList();
   particleChildren = particleChildren.Where(c => c.Count > 0).ToList();
   ```
   This alone removes the visible duplicate `ظرف زمان` in production, because the zero-count code
   (`TIM` in the current corpus) disappears.
8. Replace the main-node counts with used-subtype counts and drop the `counts` lookup:
   ```csharp
   return new WordTypeTreeDto([
       MainNode(NounType,     "اسم",         nounChildren.Count,     "case",       nounChildren),
       MainNode(VerbType,     "فعل",         verbChildren.Count,     "tense+voice", verbChildren),
       MainNode(ParticleType, "حرف وأداة",   particleChildren.Count, "none",       particleChildren),
       MainNode(InlType,      "حروف مقطّعة", 1,                      "none",       []),
   ]);
   ```
   Particle keeps `secondaryFilter.kind = "none"` (no case/tense/voice for particles) — it now simply
   also carries children.
9. Remove the now-unused `counts` query at the top of `GetTreeAsync`, and delete `TreeCountsSql()` and
   the `TreeCountRow` record from `EfWordTypesReader.Sql.cs`. Confirm no other caller references them
   (grep `TreeCountsSql` / `TreeCountRow`). Keep `TreeChildCountRow`.

### 3.4 Frontend: URL normalization for particle child codes

File: `word-types-url-sync.ts` → `normalizeChildCode`.

10. Stop dropping particle child codes; only `inl` has no child dimension:
    ```typescript
    if (type === 'inl') {
      return null;
    }
    if (type === 'verb') {
      return isWordTypeTense(raw) ? raw : null;
    }
    return raw; // noun OR particle: POS codes the parser can't enumerate; backend validates.
    ```
    Update the accompanying comment to reflect that particle now passes through like noun.

### 3.5 Frontend: gate row loading on leaf selection + `inl` direct-load

File: `word-types.models.ts`.

11. Add `'selectPrompt'` to the `WordTypesLoadStatus` union:
    ```typescript
    export type WordTypesLoadStatus =
      'idle' | 'loading' | 'selectPrompt' | 'success' | 'empty' | 'error' | 'notFound';
    ```

File: `word-types-explorer.facade.ts` → `loadList()`.

12. Compute leaf selection and branch. A leaf is selected when `childCode !== null` **or**
    `type === 'inl'`:
    ```typescript
    private loadList() {
      const query = this.state().query;
      const leafSelected = query.childCode !== null || query.type === 'inl';
      this.state.update((c) => ({ ...c, status: 'loading', errorMessage: '' }));

      const tree$ = this.cache.getOrLoad(WordTypesCacheKeys.tree, () => this.api.getTree());

      if (!leafSelected) {
        return tree$.pipe(
          tap((tree) => this.handleTreeOnly(tree)),
          catchError(() => { /* set status:'error', tree:null, rows:null */ return of(undefined); }),
          map(() => undefined),
        );
      }

      return forkJoin({ tree: tree$, rows: this.cache.getOrLoad(
        WordTypesCacheKeys.rows(query, query.sort, query.page),
        () => this.api.getRows({ ...query, pageSize: WORD_TYPES_PAGE_SIZE }),
      ) }).pipe(/* existing handleListResponse path */);
    }
    ```
13. Add `handleTreeOnly(tree)`: on tree success set
    `{ status: 'selectPrompt', tree: tree.data, rows: null, errorMessage: '' }`; on tree failure set
    the existing error state. Do **not** request rows.
14. `requestKey` already includes `type` and `childCode`, so parent→parent and leaf→parent transitions
    re-trigger `loadList()`. No change needed there. `selectType` / `selectChild` already
    `clearWordTypesSelection()` and reset the page; keep as-is.

File: `word-types.labels.ts`.

15. Add a label:
    ```typescript
    export const WORD_TYPES_SELECT_SUBTYPE_LABEL = 'اختر نوعًا فرعيًا لعرض الكلمات.';
    ```

### 3.6 Frontend: parent selection opens the subtype picker + selection empty state

File: `word-type-filter.component.ts` → `selectType`.

16. When a parent with children is selected, also open its panel so the user is immediately prompted
    to pick a subtype (leaf categories like `inl` have no panel):
    ```typescript
    protected selectType(node: WordTypeTreeNodeDto): void {
      if (this.loading()) { return; }
      this.typeSelected.emit(node.code);
      this.openPanelType.set(node.children.length > 0 ? node.code : null);
    }
    ```

File: `word-types-explorer-page.component.ts` + `.html`.

17. Expose the new label (getter `selectSubtypeLabel` → `WORD_TYPES_SELECT_SUBTYPE_LABEL`).
18. In the template, render the select-subtype empty state and gate the table + pagination so parents
    show the prompt instead of a table:
    ```html
    @switch (listState().status) {
      @case ('selectPrompt') {
        <p class="qd-empty-state" data-testid="word-types-select-subtype">{{ selectSubtypeLabel }}</p>
      }
      @case ('loading') { <p class="qd-loading-state" role="status" aria-live="polite">{{ loadingLabel }}</p> }
      @case ('error')   { <p class="qd-error-state">{{ listState().errorMessage || errorLabel }}</p> }
      @case ('empty')   { <p class="qd-empty-state">{{ emptyLabel }}</p> }
    }

    @if (listState().status !== 'selectPrompt') {
      <qd-word-types-table … />
      @if (listState().rows; as page) { <qd-pagination … /> }
    }
    ```
    Result: clicking Nouns/Verbs/Particles shows the prompt and never renders rows; selecting a
    subtype (or `inl`) renders the table.

---

## 4. Phase 2 — UI layout / details / scroll polish

Goal: mirror the Roots explorer shell so both the table body and the details panel scroll
independently inside a viewport-bound layout. Reuse existing shared classes/tokens; introduce only
`word-types-*` local classes. Do not touch Roots/Lemmas/Stems/Unique Words source.

### 4.1 Page shell + viewport-bound card height

File: `word-types-explorer-page.component.scss` (reference: `roots-explorer-page.component.scss`).

1. Keep the existing two-column layout intent but adopt the Roots height model. On the layout
   container define `dvh`-based custom properties and apply a fixed `block-size` to the table host and
   the panel column at desktop:
   ```scss
   @use '../../../../../styles/breakpoints' as bp;

   @media (min-width: bp.$qd-bp-desktop-min) {
     .word-types-page__layout {
       grid-template-columns: minmax(0, 5fr) minmax(0, 4fr); // match Roots ratio (was 16–22rem)
       align-items: start;
       --wt-chrome-block-size: 16rem; // header + filter + toolbar + pagination reserve
       --wt-table-header-height: 2.75rem;
       --wt-table-body-height: min(calc(100dvh - var(--wt-chrome-block-size)), 58rem);
       --wt-table-card-height: calc(var(--wt-table-header-height) + var(--wt-table-body-height));
     }
     .word-types-page__main > qd-word-types-table { block-size: var(--wt-table-card-height); }
     qd-word-type-details-panel { block-size: var(--wt-table-card-height); display: block; }
   }
   @media (min-width: bp.$qd-bp-wide-desktop-min) {
     .word-types-page__layout {
       --wt-chrome-block-size: 14rem;
       --wt-table-body-height: min(calc(100dvh - var(--wt-chrome-block-size)), 64rem);
       --wt-table-card-height: calc(var(--wt-table-header-height) + var(--wt-table-body-height));
     }
   }
   ```
   Tune `--wt-chrome-block-size` against the actual rendered chrome (page header + filter row +
   sort toolbar + pagination). The panel column already carries the details panel; the panel's
   `:host { height: 100% }` + `explorer-detail-panel__body { overflow: hidden }` become effective once
   the host has a bounded height.

### 4.2 Table card structure + scrollable body

File: `word-types-table.component.html` (reference: `roots-table.component`).

2. Restructure from a single `word-types-table__scroll` wrapper into a non-scrolling header and a
   scrollable body:
   ```html
   <section class="word-types-table" [attr.aria-label]="tableLabel">
     @if (rows(); as page) {
       <div class="word-types-table__header" role="rowgroup"> … header row (unchanged cells) … </div>
       <div class="word-types-table__body" role="rowgroup" [attr.aria-rowcount]="page.totalCount">
         @for (row of page.items; …) { … existing row button … }
       </div>
     } @else {
       <p class="word-types-table__placeholder">{{ tableLabel }}</p>
     }
   </section>
   ```
   Keep the `role="table"`/`role="row"` semantics equivalent to today (put `role="table"` on the
   section, `role="row"` on header/rows). Preserve the `data-word-types-row` attribute exactly so
   `focusRow()` keeps working.

File: `word-types-table.component.scss` (reference: `roots-table.component.scss`).

3. Make the header and rows share the same grid template (keep the existing
   `grid-template-columns … min-inline-size: 58rem`). Give the body a bounded height + vertical scroll,
   and switch to the flex-column card on desktop via `:host-context`:
   ```scss
   @use '../../../../../styles/breakpoints' as bp;

   .word-types-table__body {
     block-size: var(--wt-table-body-height, min(70vh, 40rem));
     overflow: auto;              // vertical + horizontal (min-inline-size drives horizontal)
     scrollbar-gutter: stable;
   }
   .word-types-table__header { flex-shrink: 0; }

   @media (min-width: bp.$qd-bp-desktop-min) {
     :host-context(.word-types-page__main) { display: block; block-size: 100%; }
     :host-context(.word-types-page__main) .word-types-table {
       display: flex; flex-direction: column; block-size: 100%; box-sizing: border-box;
     }
     :host-context(.word-types-page__main) .word-types-table__header {
       flex-shrink: 0; block-size: var(--wt-table-header-height, 2.75rem);
     }
     :host-context(.word-types-page__main) .word-types-table__body {
       flex: 1 1 auto; min-block-size: 0; block-size: auto;
     }
   }
   @media (max-width: bp.$qd-bp-tablet-max) {
     .word-types-table__body { block-size: min(70vh, 40rem); }
   }
   ```
   Remove the old `.word-types-table__scroll { overflow-x: auto }` rule (replaced by the body).

### 4.3 Bounded details panel + independent scroll

The panel component (`word-type-details-panel`) already reuses the shared `explorer-detail-panel`
chrome and correct host styles — no component change is needed once the shell (4.1) gives its host a
bounded height. The only work is making the **projected content** scroll as one region under the
`explorer-detail-panel__body` (which is `display:flex; flex-direction:column; overflow:hidden`).

File: `word-types-explorer-page.component.html` (projected panel content).

4. Keep the summary block as a non-shrinking header and wrap the `@switch` view content in a
   scrollable region:
   ```html
   <section class="word-types-summary" data-testid="word-type-summary"> … dl … </section>
   <div class="word-types-details__scroll">
     @switch (panelState().status) { … existing ayahs / surahs / analysis blocks … }
   </div>
   ```

File: `word-types-explorer-page.component.scss` (styles the projected content — it is declared in the
page view).

5. Add:
   ```scss
   .word-types-summary { flex-shrink: 0; }
   .word-types-details__scroll { flex: 1 1 auto; min-block-size: 0; overflow-y: auto; }
   ```
   The `ayah-matches-list` already has its own internal `__viewport` scroll; nesting under a bounded
   region is fine (matches the Roots panel pattern where sub-tabs are `flex-shrink:0` and the list
   scrolls).

### 4.4 Empty-state styling + desktop/mobile expectations

6. The `selectPrompt` and existing empty states already use the shared `qd-empty-state` class — no new
   styling needed beyond ensuring they sit inside the bounded main column.
7. Desktop (≥ `qd-bp-desktop-min`): two columns; table body and panel each scroll independently; the
   page itself does not scroll. Mobile/tablet: single column; the details panel renders as the shared
   modal (`inline=false`, unchanged); the table body uses `min(70vh, 40rem)`.

---

## 5. Phase 3 — Tests and verification

### 5.1 Backend tests

Seed facts (from `word-types-explorer-seed.sql`): noun rows resolve to `N`(2), `PN`(1), `ADJ`(1);
verb rows resolve to `unspecified`(1), `past`(1), `present`(1), `imperative`(1); particle catalogue =
`P`(0 rows), `PRO`(1 row); `INL`(1 row) is its own type. Under the new semantics the used-subtype
parent counts become **noun=3, verb=3, particle=1, inl=1**.

Update `WordTypesMainReadTests.cs`:
- `Tree_ReturnsFourMainTypes_WithCorrectMainCounts`: change `Count(tree,"noun")` 4→**3**,
  `Count(tree,"verb")` 4→**3**; `particle`=1 and `inl`=1 stay.
- `Rows_TotalCount_EqualsTreeCount_ForMainTypes`: this asserts the old "parent count == row count"
  invariant, which is deliberately no longer true. Rewrite it into a rows-shape test (still assert
  every row has display text / id / context code, and assert row totals per the seed: noun=4, verb=4,
  particle=1, inl=1) **without** tying totals to `node.Count`. Keep `ParticleRows_ExcludeInl…` and the
  other rows tests unchanged.

Update `WordTypesSubtypeReadTests.cs`:
- `Tree_ReturnsFixedVerbTenseChildren_AndNoParticleOrInlChildren` → rename to reflect new truth and
  assert: verb children `past, present, imperative`; **particle children = `["PRO"]`** (zero-count `P`
  hidden, `INL` excluded); `inl` children empty.
- Add a test: `Tree_ParticleChildren_AreCatalogueDriven_ExcludeInl_AndHideZeroCount` — particle
  children codes `Should().Equal("PRO")`, and `Should().NotContain(c => c.Code == "P" || c.Code == "INL")`.
- `Handler_RejectsInvalidChildCode_ForType`: **remove** `[InlineData("particle","P", …InvalidFilter)]`
  (particle `P` is now valid). Keep the noun/verb/inl reject rows. Add a new fact/theory:
  `particle`+`PRO` → `GetWordTypeRowsOutcome.Success` with rows; `particle`+`P` → `Success` with
  `TotalCount == 0` (valid but empty — proves deep-link tolerance for hidden zero-count children).
- The noun/verb child-count theories are unaffected by hiding (all seeded children have rows) — leave
  them.

Update `WordTypesSecondaryFilterReadTests.cs`:
- Re-check the `noun.Count` read (~line 64). If it compares to a row-count-derived number, update the
  expected value to the used-subtype count (3) or refocus the assertion on "unchanged by secondary
  filter" (still true: tree counts are unscoped). Keep the particle secondary-filter rejection cases.

Update `WordTypesChildCatalogueDriftTests.cs`:
- Add a fact mirroring the noun one for particles: every `PosTagSeed` code with `Category == "particle"`
  and `Code != "INL"` satisfies `WordTypesHandlerValidation.IsValidChildCode("particle", code)`; and
  assert `IsValidChildCode("particle","INL")` is `false`. This guards `ParticleChildCodes` against
  drift from the catalogue.

Optional (duplicate-label proof) `word-types-explorer-seed.sql` + a new subtype test:
- Add noun catalogue rows `('T','ظرف زمان',…,'noun', <order>)` and `('TIM','ظرف زمان',…,'noun', <order>)`
  and a single morphology row with `head_pos = 'T'` (so `T` count = 1, `TIM` count = 0). Then assert
  the noun children contain exactly one child with `Label.Ar == "ظرف زمان"` and its `Code == "T"`.
  This is a test-fixture change only — not a `PosTagSeed` edit.

### 5.2 Frontend tests

`state/word-types-url-sync.spec.ts`:
- A particle child code round-trips: `parseWordTypesQueryParams` with `type=particle&childCode=PRO`
  yields `childCode === 'PRO'` (previously dropped to `null`); `inl` still drops any child code;
  verb still validates against the tense set.

`components/word-type-filter/word-type-filter.component.spec.ts`:
- Given a tree where `particle` has children, the particle node renders the `+` expander and, when
  selected/expanded, lists its child buttons. Assert `inl` (no children) renders no expander.

`pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts`:
- Selecting a parent (`noun`/`verb`/`particle`) does **not** issue a rows request (spy the API /
  facade) and renders the `word-types-select-subtype` empty state; the table is not rendered.
- Selecting a subtype (e.g. `noun` + `N`, or a particle child) issues a rows request and renders the
  table.
- `inl` loads directly (rows requested with `type=inl`, `childCode=null`).
- Optional: assert the details panel and table have independent scroll containers if practical in the
  component test (DOM presence of `word-types-table__body` and `word-types-details__scroll`); full
  scroll behavior is better verified manually (see 6.4).

Keep the frontend worker cap when running tests (see memory: `npm test` OOMs without
`VITEST_MAX_FORKS`).

---

## 6. Review gates

### 6.1 Focused backend test command (from `Backend/`)
```bash
dotnet build QuranDashboard.sln
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --filter "FullyQualifiedName~WordsWordTypes"
```

### 6.2 Focused frontend test command (from `Frontend/quran-dashboard-ui/`)
```bash
npm test -- --run \
  src/app/features/words/pages/word-types-explorer-page \
  src/app/features/words/components/word-type-filter \
  src/app/features/words/components/word-types-table \
  src/app/features/words/state/word-types-url-sync.spec.ts
```

### 6.3 Build commands
```bash
# backend
dotnet build QuranDashboard.sln
# frontend
npm run build
```

### 6.4 Manual UI checks (desktop + mobile)
- Nouns/Verbs/Particles: click parent → table shows "اختر نوعًا فرعيًا لعرض الكلمات", no rows load,
  subtype panel opens.
- Particles & Tools shows real subtypes (e.g. حرف جر, حرف عطف, …); no empty subtypes; no `INL`.
- Nouns: no duplicate `ظرف زمان`; no zero-count subtypes.
- Parent counts equal the number of visible subtypes; Disjoint Letters shows `1` and loads directly.
- Select a subtype → table loads only matching rows; case/tense/voice narrow further.
- Table body scrolls independently; details panel scrolls independently; the page itself does not
  scroll. Layout matches Roots/Lemmas/Stems/Unique Words.

Recommended: run the workspace `deploy-smoke` (build + migrate-check + health) before opening a PR;
no migration is expected, so migrate-check should report none.

---

## 7. Risks / notes

- **Count semantics differ by category.** Parent categories (`noun`/`verb`/`particle`) show a
  used-subtype count; the leaf `inl` shows `1`. This is intentional and locked. Document it inline
  where `MainNode(InlType, …, 1, …)` is set so a future reader does not "fix" it back to a row count.
- **Tree counts stay unscoped by secondary filters.** Do not wire the new counts to the scoped row
  context; case/tense/voice never change tree counts (existing `WordTypeReadContext.Unscoped`
  contract; `TreeChildNode_Counts_HonorNoSecondaryFilterScoping` guards this).
- **Validation is a superset of the visible tree.** Zero-count children are hidden in the UI but still
  accepted by `IsValidChildCode` so deep-links return 200-empty, not 400. Keep the drift test green.
- **Do not solve POS taxonomy naming here.** The `T`/`TIM` shared label `ظرف زمان` is a source-taxonomy
  matter owned by the importer/data layer. This feature only hides the zero-count node; renaming or
  merging codes is out of scope and must not touch `PosTagSeed`.
- **Chrome-height tuning is empirical.** `--wt-chrome-block-size` must be checked against the rendered
  header/filter/toolbar/pagination stack; if the table overflows the viewport, raise it. The `min(…, Nrem)`
  cap prevents an over-tall table on very large screens (same guard Roots uses).
- **Keep DOM contracts stable.** Preserve `data-word-types-row`, `data-testid` hooks, and ARIA roles
  during the table restructure so existing focus management (`focusRow`) and tests keep working.
```
