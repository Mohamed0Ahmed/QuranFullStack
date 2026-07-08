# Word Types Explorer — Polish & Semantics Audit

Feature branch: `021-word-types-explorer-polish`
Scope: read-only audit of the current Word Types Explorer page (أنواع الكلمات). No files were modified.
Base feature: `019-word-types-explorer` (spec/plan/contracts under `specs/019-word-types-explorer/`).

---

## Verdict: **BLOCKED**

The page is not ready to match the sibling explorers. There are three correctness/semantics
defects that are visible to the user and one broken scroll/layout that makes the page feel
unfinished:

1. Selecting a parent category (Nouns / Verbs / Particles) immediately loads every word of that
   parent instead of waiting for a real subtype/leaf.
2. Noun subtypes with zero real usage are rendered (including a **duplicate `ظرف زمان`**), and the
   parent count is a row count, not a used-subtype count.
3. **Particles & Tools has no child classifications at all** — the backend hard-codes an empty
   children list for it, so the whole category is a single un-narrowable bucket.
4. The table has no vertical scroll region and the details panel height is inert, so the layout
   grows the page instead of using the fixed-height, independently-scrolling shell the other
   explorers use.

All four are well-scoped and fixable inside this feature. None require migrations, importer
changes, or touching Quran data. Details, responsible files, and a phased plan follow.

---

## Current behavior summary

- On page load the facade (`WordTypesExplorerFacade.bindToRoute`) parses defaults
  (`type=noun`, `childCode=null`) and **immediately fires `loadList()`**, which requests `getTree()`
  **and** `getRows({type:'noun', childCode:null, …})`. The table therefore shows *all* noun
  word-context rows before the user selects anything.
- The top filter row renders exactly the four main nodes returned by the tree API: `اسم` (noun),
  `فعل` (verb), `حرف وأداة` (particle), `حروف مقطّعة` (inl). The number beside each is
  `node.count`, which the backend computes as the number of grouped word-context **rows**, not the
  number of used child subtypes.
- Clicking a main node (`selectType`) navigates with `childCode=null` and reloads rows → loads all
  words of that parent. Clicking the `+` expander opens a panel of child subtypes (noun POS codes /
  verb tenses). Particle and inl expose no `+` expander because their `children` array is empty.
- Noun children come straight from the live POS catalogue (all `quran_pos_tags` rows with
  `category = 'noun'`, ordered by `SortOrder`), each carrying its row count, **including zero-count
  codes**. Two of those codes (`T` and `TIM`) share the identical Arabic label `ظرف زمان`.
- The details panel already reuses the shared `explorer-detail-panel` / `explorer-panel-header`
  chrome, but the page shell and table use bespoke classes that never establish a viewport-bound
  height, so nothing scrolls independently.

## Expected behavior summary

- Exactly 4 main categories side by side: Nouns, Verbs, Particles & Tools, Disjoint Letters.
- The count beside each parent = number of **visible/used child subtypes** (count > 0), not
  words/occurrences.
- Clicking Nouns / Verbs / Particles must **not** load the table. The table shows a clear
  "select a subtype" empty state until a real leaf is chosen. Disjoint Letters (a single leaf)
  loads directly.
- Zero-usage subtypes are hidden and excluded from parent counts; no duplicate labels.
- Particles & Tools exposes real child classifications.
- After selecting a subtype, the table loads only rows matching it; secondary filters
  (case / tense / voice) narrow further.
- The page uses the same explorer shell as Roots/Lemmas/Stems/Unique Words: main table area +
  persistent details panel, each with an independent, viewport-bound scroll area.

---

## File / component / query inventory

### Frontend (`Frontend/quran-dashboard-ui/src/app/features/words/`)

| Concern | File |
| --- | --- |
| Page shell (layout, panel wiring, selection) | `pages/word-types-explorer-page/word-types-explorer-page.component.{ts,html,scss}` |
| Main + subtype filter row | `components/word-type-filter/word-type-filter.component.{ts,html,scss}` |
| Results table | `components/word-types-table/word-types-table.component.{ts,html,scss}` |
| Details panel chrome | `components/word-type-details-panel/word-type-details-panel.component.{ts,html,scss}` |
| List state / load orchestration | `state/word-types-explorer.facade.ts` |
| Detail state | `state/word-types-detail.facade.ts` |
| URL ⇄ state mapping, normalization | `state/word-types-url-sync.ts` |
| API client | `data-access/word-types.api.ts` |
| Types | `models/word-types.models.ts` |
| Labels (Arabic) | `models/word-types.labels.ts` |

Sibling reuse templates (do **not** change unless a shared class is extracted):
`pages/roots-explorer-page/roots-explorer-page.component.{html,scss}`,
`components/roots-table/roots-table.component.scss`,
`components/root-details-panel/root-details-panel.component.scss`.

### Backend (`Backend/…`)

| Concern | File |
| --- | --- |
| Tree assembly (main nodes + children + counts) | `infrastructure/…/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs` → `GetTreeAsync` |
| Tree/rows SQL (counts, child counts, rows) | `infrastructure/…/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.Sql.cs` |
| POS catalogue source | `infrastructure/…/Files/Quran/DataPipelines/Words/MorphologyImporting/PosTagSeed.cs` |
| Child-code validation (mirror of catalogue) | `application/…/Quran/Words/WordTypes/Queries/WordTypesHandlerValidation.cs` |
| Catalogue drift guard test | `tests/…/Quran/WordsWordTypes/WordTypesChildCatalogueDriftTests.cs` |
| API controller | `api/…/Controllers/Words/WordTypesController.cs` |

---

## Findings by severity

### HIGH

**H1 — Clicking a parent category loads all its words (no leaf gate).**
`WordTypesExplorerFacade.bindToRoute` always calls `loadList()` on any query change, and `loadList()`
always requests rows. `selectType(type)` navigates with `childCode:null`; `EfWordTypesReader.GetRowsAsync`
with no child code emits `TypePredicate` with only the category filter → returns every word-context row
for the parent.
Responsible: `state/word-types-explorer.facade.ts` (`bindToRoute`, `loadList`, `selectType`);
`state/word-types-url-sync.ts`; `pages/word-types-explorer-page.component.html` (unconditional
`<qd-word-types-table>` + pagination).
Fix direction: introduce a "leaf selected?" predicate — rows are requested only when
`childCode !== null` **or** `type === 'inl'`. Otherwise the list state moves to a `selectPrompt`
status and the table renders a selection empty state. The verb parent's leaves are the three tenses;
the noun and (future) particle parents' leaves are the POS child codes; `inl` is itself the leaf.

**H2 — Particles & Tools has no child classifications.**
`EfWordTypesReader.GetTreeAsync` hard-codes `MainNode(ParticleType, "حرف وأداة", …, "none", [])` — an
empty children list — and `TreeChildCountsSql()` only computes `noun_children` and `verb_children`.
The frontend reinforces this: `word-types-url-sync.normalizeChildCode` drops any child code when
`type === 'particle'`, and `WordTypesHandlerValidation.IsValidChildCode` rejects every particle child
code. So the whole particle category is one un-narrowable bucket, and combined with H1 it dumps every
particle word into the table.
Note: the POS catalogue *does* have a rich particle taxonomy — ~34 `category = 'particle'` codes
(`P` حرف جر, `CONJ` حرف عطف, `NEG` حرف نفي, `SUB` حرف مصدري, `COND` حرف شرط, …). `INL` is also
`category = 'particle'` but is deliberately split out as its own main type (`head_pos = 'INL'`), so
particle children must exclude `INL`.
Responsible: `EfWordTypesReader.cs` (`GetTreeAsync`), `EfWordTypesReader.Sql.cs`
(`TreeChildCountsSql`), `WordTypesHandlerValidation.cs` (`IsValidChildCode`, plus a new
`ParticleChildCodes` set), `word-types-url-sync.ts` (`normalizeChildCode`).
Fix direction: build particle children catalogue-driven exactly like nouns — `PosTags` where
`Category == 'particle' && Code != 'INL'`, ordered by `SortOrder`, each with its row count; add a
`particle_children` CTE to `TreeChildCountsSql`; add a `ParticleChildCodes` allow-set to validation
(and extend the drift test); let the frontend pass particle child codes through instead of dropping
them. Secondary filter stays `none` for particles.

**H3 — Table has no vertical scroll; details panel height is inert.**
`word-types-table.component.scss` gives `.word-types-table__scroll { overflow-x: auto }` only — no
`overflow-y`, no bounded height — so the table can never scroll vertically and simply extends the
page. The page shell (`word-types-explorer-page.component.scss`) never establishes a viewport-bound
height: `.word-types-page__layout` is a plain grid with `align-items: start` and no `dvh`-based card
height. The details panel's `:host { height: 100% }` + `.explorer-detail-panel__body { overflow: hidden }`
are therefore inert, because the panel's grid cell is auto-height → `100%` resolves to content height →
nothing clips, inner lists (`ayah-matches-list__viewport { overflow-y:auto }`) get no bounded height.
Contrast the working Roots pattern (`roots-explorer-page.component.scss`): it computes
`--roots-table-card-height = header + min(calc(100dvh - chrome), 58rem)` and applies that fixed
`block-size` to **both** `.roots-explorer-layout__table > qd-roots-table` and
`.roots-explorer-layout__panel`; `roots-table.component.scss` then makes the table a flex column with
a `flex:1 1 auto; min-block-size:0; overflow:auto` body under a `flex-shrink:0` header.
Responsible: `word-types-explorer-page.component.scss`, `word-types-table.component.{html,scss}`.
Fix direction: adopt the Roots shell — a `dvh`-based card height on the layout, applied to the table
host and panel column; restructure the table into a non-scrolling header + a scrollable body
(`overflow-y:auto; min-block-size:0`) via `:host-context(.word-types-page__main)` desktop rules.

### MEDIUM

**M1 — Zero-usage noun subtypes are displayed and counted.**
`GetTreeAsync` builds `nounChildren` from the entire noun catalogue and assigns
`nounChildCounts.GetValueOrDefault(pos.Code)` → `0` when a code has no rows. Zero-count children are
kept and rendered. This is the mechanism behind the "one unused subtype" observation.
Responsible: `EfWordTypesReader.GetTreeAsync`.
Fix direction: filter children to `count > 0` before returning (applies to noun and future particle
catalogue children). Verb tenses can stay as the fixed set or also be filtered.

**M2 — Duplicate `ظرف زمان` (two POS codes, identical Arabic label).**
`PosTagSeed` defines both `T` (SortOrder 31, "Time Adverb") and `TIM` (SortOrder 33,
"Temporal Adverb") with the **same** `ArabicLabel = "ظرف زمان"`, both `Category = "noun"`. Both are in
the validation allow-set (`NounChildCodes`) and both render as selectable children. In practice one of
them (`TIM`) has no rows in the corpus, so the user sees two `ظرف زمان` entries, one of them empty.
Root cause classification: **duplicate Arabic labels for two distinct POS codes** *and* **static
catalogue including an unused node**. It is *not* a frontend counting/rendering bug and *not* a
zero-count node returned by the row query — the row query correctly returns 0; the tree assembly keeps
the 0.
Recommended fix: **hide zero-count nodes** (M1) resolves the *visible* duplicate immediately, because
the empty `ظرف زمان` disappears. Do **not** rename or merge the POS codes and do **not** mutate the
seed as part of this polish — `T` vs `TIM` is a source-taxonomy distinction that belongs to the
importer/data layer, which is explicitly out of scope. If both `T` and `TIM` ever carry real rows
simultaneously, revisit with a data-layer decision (rename one label or merge codes) rather than a UI
patch. Track that as a follow-up, not a 021 change.

**M3 — Main-category count is a row count, not a used-subtype count.**
`TreeCountsSql()` groups the base rows per `(tashkeel_word_id, context_code)` and counts the groups,
so `node.count` for each main type is the number of table rows across all its children — not the
number of distinct used subtypes. The filter template binds `{{ node.count }}` directly.
Responsible: `EfWordTypesReader.Sql.cs` (`TreeCountsSql`), `EfWordTypesReader.GetTreeAsync`,
`word-type-filter.component.html`.
Fix direction: derive the parent count from the children after zero-count filtering —
`node.count = children.count(c => c.count > 0)`. Cheapest implementation is in `GetTreeAsync` (count
the filtered `nounChildren` / `verbChildren` / `particleChildren`), which also lets `TreeCountsSql` be
retired or repurposed. Decide the display for the leaf `inl` node (no children): either show nothing or
show its word count — flag as a small product decision (see Open questions).

### LOW

**L1 — Table empty/placeholder state is weak.**
`word-types-table.component.html` shows only `{{ tableLabel }}` ("جدول كلمات النوع") when `rows()` is
null. Once H1 is fixed, the pre-selection state needs a proper "اختر نوعًا فرعيًا لعرض الكلمات"-style
empty state consistent with `WORD_TYPES_EMPTY_SELECTION_LABEL`. Add an explicit label constant in
`word-types.labels.ts`.

**L2 — Panel column is narrow vs siblings.**
`word-types-explorer-page.component.scss` sets the desktop panel column to `minmax(16rem, 22rem)`,
whereas Roots uses `minmax(0, 4fr)` (~44% of the row). The details panel content (summary `dl`, ayah
list) feels cramped. Align the column ratio with the sibling explorers when adopting the shell (H3).

**L3 — Frontend `requestKey` includes secondary filters but E1 counts are unscoped.**
Not a defect (documented design: tree counts stay unscoped), but note it so the count fix (M3) is not
mistakenly wired to the scoped row context. Counts must remain unscoped.

---

## Root-cause answers to the specific questions asked

- **Why does "Particles & Tools" have no child classifications?** Deliberate v1 omission encoded in
  three places: `GetTreeAsync` passes `[]` for particle children, `TreeChildCountsSql` computes no
  particle child counts, and both `normalizeChildCode` (frontend) and `IsValidChildCode` (backend)
  reject particle child codes. The taxonomy exists in `PosTagSeed` (34 particle codes) but is never
  surfaced. See **H2**.
- **What causes the `ظرف زمان` duplication / unused subtype?** Two things together: (a) duplicate
  Arabic labels — `T` and `TIM` both labelled `ظرف زمان` in `PosTagSeed`; (b) the tree keeps
  zero-count catalogue nodes. Classification: **duplicate labels + static catalogue including an
  unused node**. Not a two-codes-one-label *mapping* bug beyond the label collision, not a
  backend row query returning phantom counts, not a frontend counting bug. See **M1 + M2**.
- **Correct fix for the duplicate?** **Hide zero-count nodes** (removes the visible duplicate now).
  Do not rename/merge/remap in 021; a label rename or code merge is a data-layer decision, out of
  scope here.
- **How do URL/state/query params represent the dimensions today?**
  - Parent category → `type` (`noun|verb|particle|inl`).
  - Subtype/leaf → `childCode` (noun POS code or verb tense literal). `inl` has no child code.
  - Secondary filters → `case` (noun only), `tense` + `voice` (verb only); `all` = no filter.
  - Row selection → `word` (+ `contextCode`), plus detail `view` / `detailPage` / `location` / `column`.
  `parseWordTypesQueryParams` normalizes by type (drops case for non-nouns, tense/voice for
  non-verbs, particle/inl child codes). The API mirrors these exactly (`GET /api/words/word-types/words`
  with `type,childCode,case,tense,voice,sort,page,pageSize`).
- **State ⇄ API ⇄ table mismatch?** The main mismatch is *semantic*, not param-plumbing: state allows
  `type` selected with `childCode=null` and the API happily returns all parent rows for that state.
  The table renders whatever rows arrive. The fix is to stop requesting rows until a leaf exists
  (H1), not to change the param shape.

---

## Recommended implementation phases

### Phase 1 — Semantics / state / counts (correctness first)

1. **Gate row loading on a real leaf** (H1). In `word-types-explorer.facade.ts`, only request rows
   when `childCode !== null || type === 'inl'`; otherwise set a `selectPrompt` list status and clear
   rows. Keep the tree request unconditional (the filter always needs it). Update
   `word-types-explorer-page.component.html` to render a selection empty state and hide the table +
   pagination until a leaf is selected.
2. **Surface particle children** (H2). Backend: build `particleChildren` catalogue-driven in
   `GetTreeAsync` (`Category == 'particle' && Code != 'INL'`, `count > 0`), add a `particle_children`
   CTE to `TreeChildCountsSql`, add `ChildCodePredicate` support for particles in the rows SQL, add a
   `ParticleChildCodes` set to `WordTypesHandlerValidation` and extend
   `WordTypesChildCatalogueDriftTests`. Frontend: stop dropping particle child codes in
   `normalizeChildCode`.
3. **Hide zero-count subtypes** (M1) — filter `nounChildren`/`particleChildren` (and optionally verb
   tenses) to `count > 0` in `GetTreeAsync`. This also removes the visible duplicate `ظرف زمان` (M2).
4. **Parent count = used-subtype count** (M3) — set each main node's `count` to the number of its
   post-filter children with `count > 0`; decide `inl` display (Open question O1). Retire/repurpose
   `TreeCountsSql` if no longer used.
5. Add the pre-selection empty-state label (L1) to `word-types.labels.ts`.

### Phase 2 — Layout / details / scroll polish

6. **Adopt the Roots explorer shell** (H3, L2). In `word-types-explorer-page.component.scss`, wrap in
   the shared page frame and compute a `dvh`-based card height; apply it to the table host and the
   panel column; widen the panel column to the sibling ratio.
7. **Give the table a scrollable body** (H3). Restructure `word-types-table.component.html/scss` to a
   non-scrolling header + a `flex:1 1 auto; min-block-size:0; overflow-y:auto` body, mirroring
   `roots-table.component.scss`'s `:host-context(...)` desktop rules. Keep the existing horizontal
   scroll for narrow viewports.
8. Verify the details panel now scrolls independently (its `explorer-detail-panel__body` chrome is
   already correct once the host has a bounded height — no component change expected, only the shell).

### Phase 3 — Focused tests

9. Backend unit/integration: particle children are returned with non-zero counts and exclude `INL`;
   zero-count noun children (incl. `TIM`) are absent; parent counts equal used-subtype counts;
   selecting a particle child code returns rows (no longer 400). Extend the drift test for the new
   `ParticleChildCodes` set.
10. Frontend: facade does **not** request rows when only a parent is selected (noun/verb/particle);
    requests rows for `inl` and for any selected leaf; table shows the selection empty state
    pre-leaf; URL round-trips particle child codes. Reuse existing spec patterns in
    `state/word-types-url-sync.spec.ts` and the page/filter/table `*.spec.ts`.

---

## Explicit "do NOT change" list

- **No EF migrations** unless a change is proven necessary (none is for the above — all fixes are
  read-model/query/UI). The taxonomy already lives in `quran_pos_tags`.
- **No importer / DataPipeline changes** and **no `PosTagSeed` edits**. The `T`/`TIM` duplicate label
  is a source-taxonomy matter; hide the empty node in the read model instead.
- **No Quran text/data mutation** — reads only.
- **No global redesign** — reuse the existing shared `explorer-*` / `qd-*` classes and the Roots
  shell; do not invent a new visual language.
- **No changes to other explorers** (Roots/Lemmas/Stems/Unique Words) unless a shared class must be
  extracted; prefer local `word-types-*` styling that consumes existing shared tokens/classes. The
  details panel already reuses `explorer-detail-panel` chrome — keep it.
- **Keep tree counts unscoped** (do not wire the count fix to the scoped row context).

## Open questions (small product decisions)

- **O1 — Leaf category count display.** For `inl` (single leaf, no children), what does the count
  beside `حروف مقطّعة` mean? Options: show nothing, or show its word/row count. Recommend: show its
  word count so the chip is never blank, and document that leaf counts are word counts while parent
  counts are used-subtype counts.
- **O2 — Verb tense children with zero count.** If a tense has zero rows, hide it too (consistent with
  M1) or keep the fixed three? Recommend hide-if-zero for consistency.

---

## Recommended focused build / test commands

Backend (from `Backend/`):

```bash
dotnet build QuranDashboard.sln
dotnet test tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --filter "FullyQualifiedName~WordsWordTypes"
```

Frontend (from `Frontend/quran-dashboard-ui/`, keep the worker cap — `npm test` OOMs without it):

```bash
# scope to the word-types specs
npm test -- --run src/app/features/words/pages/word-types-explorer-page \
                 src/app/features/words/components/word-type-filter \
                 src/app/features/words/components/word-types-table \
                 src/app/features/words/state/word-types-url-sync.spec.ts
npm run build
```

After Phase 2, a manual desktop check that the table body and details panel scroll independently and
the page itself does not scroll (matches Roots/Lemmas/Stems/Unique Words).
