# Feature Specification: Word Types Explorer (أنواع الكلمات)

**Feature Branch**: `019-word-types-explorer`
**Created**: 2026-06-30
**Status**: Draft
**Input**: Pre-spec plan `docs/feature-019-word-types-explorer/word-types-explorer-pre-spec-plan.md` (locked product decisions) + capability report `docs/feature-019-word-types-explorer/word-types-explorer-capability-and-ui-report.md`.

---

## Overview *(context for implementers)*

The Word Types Explorer is a new page in the existing Words hub of the Quran research dashboard. It lets an Arabic-speaking admin/teacher **browse Quran words grouped by their main grammatical type** (noun / verb / particle-and-tool / disconnected letters), refine by grammatical subtype and grammatical features, and inspect a chosen word's occurrences, surahs, and grammatical analysis.

The page is **table-first**: a compact word-type **filter picker** at the top, a central **words table**, and a right-side **selected-word details card**. The type hierarchy is used **only** inside the filter picker; it is never the main layout.

This spec describes **WHAT** the page must do and the **exact behavioral rules** the implementation must satisfy. It deliberately avoids prescribing technology, endpoints, table names, or columns — those belong to the planning phase. Where a rule is subtle (especially the row model and the two count families), it is stated precisely so a straightforward implementation cannot get it wrong.

---

## Terminology *(read this first — used throughout)*

- **Main word type**: the single primary grammatical type of a word, derived from the word's *main (head) part of speech*. There are exactly four for this feature: **اسم** (noun), **فعل** (verb), **حرف وأداة** (particle/tool), **حروف مقطّعة** (disconnected Qur'anic letters).
- **Subtype**: a more specific grammatical type inside a main type (e.g. under اسم: اسم علم / صفة / ضمير …). Verbs are subdivided by **tense** (ماض / مضارع / أمر), not by a separate part-of-speech code.
- **Secondary feature filter**: an additional grammatical refinement applied *within* a selected type — **case** for nominal types (مرفوع / منصوب / مجرور / غير محدد) and **tense + voice** for verbs (ماض/مضارع/أمر and معلوم/مجهول). These are NOT word types.
- **Occurrence**: one appearance of a word at one location in the Mushaf (one surah:ayah:word position). End-of-ayah markers are NOT occurrences and are always excluded.
- **Displayed word**: the visible word text, shown in **Uthmani script with full tashkeel (vowel marks)**.
- **Grammatical context**: the resolved type/subtype (+ any active secondary feature) that a set of occurrences shares.
- **Word-context row (THE ROW MODEL)**: one table row = **one displayed word + one resolved grammatical context under the active filter**. A row is **not** the word text alone. The same displayed word may appear in **more than one row** when it is used with different grammatical contexts (see FR rules below). This is the single most important rule in this spec.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse words by main type (Priority: P1)

An admin opens the Word Types Explorer. They see the four main type filters with a word count beside each. Clicking a parent with children browses its subtypes without disturbing the committed table/details; choosing a child commits that list scope. This is the minimum usable product: a browsable, type-filtered word list.

**Why this priority**: This is the core value — seeing the Qur'an's vocabulary organized by grammatical type. Without it nothing else matters; with it alone the page is already useful.

**Independent Test**: Load the page, browse each parent, commit representative children, and confirm the table shows the matching mutually exclusive grammatical rows; confirm browsing alone leaves the prior table/details intact.

**Acceptance Scenarios**:

1. **Given** the page is open, **When** no type is selected yet, **Then** a clear default state is shown (either a prompt to pick a type, or a sensible default type selected) and the four main type filters each display a word count.
2. **Given** a committed subtype and open details, **When** the admin clicks another parent with children, **Then** only that parent's children are displayed; the committed table and details do not change until a child is selected.
3. **Given** the admin clicks **حروف مقطّعة**, **Then** the table shows only disconnected-letters words (e.g. الٓمٓ) and no other type.
4. **Given** any main type is selected, **When** the admin reads a row, **Then** the row shows the displayed word in Uthmani-with-tashkeel plus its occurrence/ayah/surah counts for that row's context.

---

### User Story 2 - Inspect a selected word's details (Priority: P2)

After filtering, the admin activates one of a row's occurrence/ayah/surah statistics. The right-side details panel opens the mapped view for that exact word-context. The row container itself is inert. The ayah list highlights only the occurrences that belong to the selected row's context.

**Why this priority**: Browsing is more valuable when the admin can verify and study a specific word. This is the primary "study" action.

**Independent Test**: Select a type, activate a row statistic, and confirm the mapped detail tab populates for exactly that row's word + context; clicking or keyboard-activating the row container does nothing.

**Acceptance Scenarios**:

1. **Given** a type-filtered table, **When** the admin activates a row statistic, **Then** the details header and mapped content belong to that exact word-context and the row receives the shared active color.
2. **Given** a selected row, **When** the admin opens the **الآيات** tab, **Then** the listed ayahs are exactly those containing the row-context occurrences, with the relevant word highlighted, and the ayah count matches the row's الآيات value.
3. **Given** a selected row, **When** the admin opens the **السور** tab, **Then** the surah distribution (and any "not mentioned in" information) reflects only the row-context occurrences.
4. **Given** a selected row, **When** the admin opens the **التحليل** tab for a specific occurrence, **Then** the full grammatical analysis of that occurrence is shown.

---

### User Story 3 - Refine a main type by subtype (Priority: P3)

The admin clicks a main type to reveal its subtypes. Under **اسم** they choose **اسم علم**; the table narrows to proper-noun words. Under **فعل** they choose **فعل أمر**; the table narrows to imperative verbs. Each subtype filter shows its own word count.

**Why this priority**: Subtype browsing sharpens the grammatical exploration but is meaningful only after the main-type browse (P1) exists.

**Independent Test**: Browse each main type, select a child subtype, and confirm the table and subtype count narrow correctly while browsing alone remains inert.

**Acceptance Scenarios**:

1. **Given** a main type with subtypes, **When** the admin clicks that parent, **Then** its child subtypes appear with counts and no list/detail/URL state changes.
2. **Given** the admin selects subtype **اسم علم**, **Then** the table shows only proper-noun word-context rows and the table total equals the **اسم علم** count.
3. **Given** the admin selects subtype **فعل أمر**, **Then** the table shows only imperative-verb word-context rows.
4. **Given** any subtype is selected, **Then** the set of rows is a subset of the rows shown for that subtype's parent.

---

### User Story 4 - Apply secondary grammatical filters (Priority: P4)

When a **nominal** type is selected, the admin sees a **case** filter (الكل / مرفوع / منصوب / مجرور / غير محدد) and can narrow the table to, say, genitive nominals. When a **verb** type is selected, the admin sees **tense** (الكل / ماض / مضارع / أمر) and **voice** (الكل / معلوم / مجهول) filters. When a **particle/tool** or **حروف مقطّعة** type is selected, **no** case/tense/voice filter is shown.

**Why this priority**: Grammatical-feature refinement is a power-user enhancement layered on top of type/subtype browsing.

**Independent Test**: Select a nominal type and confirm only the case filter appears and narrows results; select a verb type and confirm tense+voice appear; select a particle type and confirm none appear.

**Acceptance Scenarios**:

1. **Given** a nominal type is selected, **When** the admin picks case **مجرور**, **Then** the table shows only the genitive nominal word-contexts and the counts update to that scope.
2. **Given** a verb type is selected, **When** the admin picks tense **مضارع** and voice **مجهول**, **Then** the table shows only present-tense passive verbs.
3. **Given** a particle/tool type or حروف مقطّعة is selected, **Then** no case/tense/voice filter is offered.
4. **Given** a nominal type and case **غير محدد**, **Then** the table shows only nominal word-contexts that have no determinable case.

---

### User Story 5 - Share / restore an exact view (Priority: P5)

The admin configures a list scope and opens a detail under that scope, then may change the list to another child while keeping the detail open. Reopening the link restores the list scope and the detail's original grammatical scope independently, together with the exact identity/view/page.

**Why this priority**: Shareable, restorable deep links help collaboration and review but are not required for core use.

**Independent Test**: Open a detail, change the child list scope, reload via the saved link, and confirm the new table plus the detail's original scope/identity/view are restored independently.

**Acceptance Scenarios**:

1. **Given** different current list and stored detail scopes, **When** the link is reopened, **Then** both scopes plus the same identity/view/page are restored independently.
2. **Given** a word that produces multiple rows (different contexts), **When** a deep link to one specific row is reopened, **Then** the correct one of those rows is selected — not a different context of the same spelling.

---

### User Story 6 - Drill into a grouped root/stem/lemma row (Priority: P3, Feature 023)

After switching the table to the **جذور / أصول / صيغ** view (Feature 022), the admin activates a grouped
root, stem, or lemma statistic and opens its mapped scoped details without leaving the active grammatical
scope. (Feature 022 shipped grouped rows as noninteractive
with no detail; Feature 023 supersedes that MVP restriction.)

**Why this priority**: Grouped drilldown turns the aggregation views from read-only tallies into a usable
research path, but the word-row workflow (US1–US5) remains the core.

**Independent Test**: In each grouped view, confirm its three statistic buttons map to related words,
ayahs, and surahs for the exact numeric identity/scope, while the row container remains inert.

**Acceptance Scenarios**:

1. **Given** a grouped root row under an active scope, **When** one of its statistics is activated,
   **Then** the requested detail uses that exact numeric identity and current scope (not a broader scope).
2. **Given** a dimension that only appears via a sub-word segment, **When** the grouped summary for the
   active head scope is read, **Then** that segment-only dimension never appears and never replaces the
   word's head root/stem/lemma.

---

### Edge Cases

- **Same spelling, multiple grammatical usages**: a displayed word used both as اسم and as صفة MUST appear as **two separate rows** (one per context), each with its own counts, its own details card, and its own ayah list. No row may mix two contexts. There is NO "dominant type" single row.
- **Disconnected letters (حروف مقطّعة / INL)**: these are their own main type and MUST NOT also be counted under حرف وأداة (no double counting).
- **No determinable grammatical feature**: when case/tense/voice is unknown for a word-context, it is treated as "غير محدد" for the case filter; tense/voice filters are offered only for verbs; an unknown feature is never shown with a misleading label.
- **Missing root / lemma / stem**: when a word-context has no root, lemma, or stem, the corresponding column/field shows a neutral placeholder (—) and the row still appears.
- **Multi-part words (multiple stems in one word)**: the word's main type is its head (primary) type; secondary stems do not create extra rows and do not change the main type. (See Assumptions.)
- **Empty result set**: when a filter combination matches no words, the table shows a clear empty state and all counts read zero; the page does not error.
- **Word with no main-type bucket** (an uncommon catalogue code outside the four main types): it is excluded from all four v1 buckets and never silently counted under اسم / فعل / حرف وأداة / حروف مقطّعة.
- **Markers**: end-of-ayah markers never appear as words, rows, or in any count.

---

## Requirements *(mandatory)*

### Functional Requirements

#### A. Page layout & navigation

- **FR-001**: The page MUST be table-first: a word-type filter picker (top), a words table (main area), and a selected-word details card (right side).
- **FR-002**: The type hierarchy MUST appear ONLY inside the filter picker; it MUST NOT become the main page layout.
- **FR-003**: The details card MUST belong to the **selected word-context row**, not to the selected type.
- **FR-004**: The page MUST follow the existing Words-hub explorer look-and-feel and be Arabic-first / right-to-left, calm and scholarly (consistent with the other lexical explorers).
- **FR-005**: On narrow viewports the layout MUST adapt (e.g. details card collapses/stacks) consistent with the existing explorers.

#### B. Main types & filter actions

- **FR-006**: The filter picker MUST offer exactly four main types: **اسم**, **فعل**, **حرف وأداة**, **حروف مقطّعة**.
- **FR-007**: Clicking a main type that has children MUST only browse/show that parent's child list. It MUST NOT change committed table/detail state or URL state. Selecting a child commits the new list scope; the childless **حروف مقطّعة** leaf commits directly.
- **FR-008**: Committed children under **اسم**, **فعل**, and **حرف وأداة** MUST remain within their mutually exclusive parent bucket; directly committed **حروف مقطّعة** contains only disconnected-letter words.
- **FR-009**: The **حرف وأداة** selection MUST exclude disconnected letters (حروف مقطّعة), and disconnected letters MUST be counted only under their own main type (no double counting).
- **FR-010**: The four main types MUST be mutually exclusive: every word's main type places it under exactly one of the four.

#### C. Subtypes

- **FR-011**: Expanding **اسم** MUST reveal nominal subtypes including at least **اسم علم**, **صفة**, **ضمير**, plus any additional nominal subtypes the system's word-type catalogue defines (e.g. اسم موصول, اسم إشارة, ظرف). Each subtype MUST be selectable and show its own word count.
- **FR-012**: Expanding **فعل** MUST reveal verb **tense** subtypes: **ماض**, **مضارع**, **أمر** (verb voice is offered as a secondary filter, not as a subtype — see FR-019).
- **FR-013**: Browsing **حرف وأداة** MUST reveal the selectable particle subtypes defined by the catalogue; the parent button itself remains browse-only.
- **FR-014**: **حروف مقطّعة** MUST be a leaf (no children, no secondary filters).
- **FR-015**: Selecting any subtype MUST yield a strict subset of its parent's words.
- **FR-016**: Subtype and main-type Arabic display labels MUST come from the system's word-type catalogue (not be re-invented in the page), except the four fixed main-type headings and the secondary-filter option labels.

#### D. The row model (word + context) — CRITICAL

- **FR-017**: A table row MUST represent **one displayed word together with one resolved grammatical context under the active filter** — never the word text alone.
- **FR-018**: When a displayed word has more than one grammatical usage that matches the active filter, it MUST appear as **multiple rows**, one per distinct context (e.g. an اسم row and a صفة row). The system MUST NOT merge them into a single "dominant" row, and MUST NOT show a misleading mixed row.
- **FR-018a**: Each such row MUST have its own occurrence count, ayah count, surah count, details card, and ayah list, scoped strictly to that row's context.
- **FR-018b**: Within a single row, the type/subtype shown MUST be exact for that row (the row's own context), never an aggregate or "dominant" value.

#### E. Secondary grammatical feature filters

- **FR-019**: When a **nominal** type/subtype is selected, the page MUST show a **case** filter with options: **الكل**, **مرفوع**, **منصوب**, **مجرور**, and **غير محدد** (for word-contexts with no determinable case).
- **FR-020**: When a **verb** type is selected, the page MUST show a **tense** filter (**الكل**, **ماض**, **مضارع**, **أمر**) and a **voice** filter (**الكل**, **معلوم**, **مجهول**).
- **FR-021**: When a **particle/tool** type or **حروف مقطّعة** is selected, the page MUST NOT show case, tense, or voice filters.
- **FR-022**: Selecting a secondary feature MUST narrow the rows to word-contexts matching that feature and update the table `totalCount` plus any active UI count chips derived from the filtered rows. The type-tree endpoint/counts remain unscoped by secondary filters in v1.
- **FR-023**: Secondary feature filters MUST NOT be treated as word types and MUST NOT cross type boundaries (e.g. no case filter applied to verbs).

#### F. Count semantics — TWO DISTINCT FAMILIES (must never be conflated)

- **FR-024**: **Filter / tree node counts are WORD-CONTEXT (row) counts**: a node's number = the count of distinct word-context rows that match that node (i.e. the number of rows the table would show for that node), NOT raw occurrences and NOT distinct word spellings.
- **FR-025**: Examples that MUST hold: the **فعل** count = number of verb word-context rows under all verbs; the **فعل أمر** count = number of word-context rows under imperative verbs only; the **اسم علم** count = number of word-context rows under proper nouns only.
- **FR-026**: **Table count columns are OCCURRENCE-level statistics for that exact row context**: **المواضع** = number of occurrences in that row's context; **الآيات** = number of distinct ayahs containing those occurrences; **السور** = number of distinct surahs containing those occurrences. These MUST be scoped to the row's context, never to all usages of the spelling.
- **FR-027**: For any selected main type or child node **with no secondary feature filter applied**, the node's filter count (FR-024) MUST equal the table's total row count for the same active type/child. When a secondary feature filter is applied, the table `totalCount` is the filtered row count and is not expected to equal the unscoped tree node count.
- **FR-028**: No count anywhere on the page may include end-of-ayah markers, and no count may be derived from sub-word parts (prefixes/suffixes/segment-level types) — counts derive only from the word's main type and word-level grammatical features.

#### G. Display & identity

- **FR-029**: Words MUST be displayed in **Uthmani script with full tashkeel**. The page MUST NOT offer a Simple / without-tashkeel display toggle in v1.
- **FR-030**: Word identity for grouping rows MUST be based on the fully-vowelled (tashkeel) word form, combined with the row's grammatical context per FR-017/FR-018. (Internal unvowelled search may be added later; display stays tashkeel.)

#### H. Table columns

- **FR-031**: The table MUST include these columns: **الكلمة** (word), **النوع** (type/subtype for the row context), **الجذر** (root), **الصيغة** (stem/form), **الأصل** (lemma), **المواضع** (occurrences), **الآيات** (ayahs), **السور** (surahs). Root display is required where source data provides a root; lemma/stem winner enrichment may return null in v1 if deferred.
- **FR-032**: The **النوع** column MUST show the row's exact (selected/derived) subtype where useful, never a "dominant" value (consistent with FR-018b).
- **FR-033**: When **الجذر**, **الصيغة**, or **الأصل** cannot be resolved for a row, or when lemma/stem winner enrichment is deferred in v1, that cell MUST show a neutral placeholder (—); the row MUST still appear and MUST NOT be dropped because root/lemma/stem is null.
- **FR-034**: The table MUST support paging and a sensible default sort (e.g. by occurrence count descending), consistent with the existing explorers.

#### I. Details card

- **FR-035**: The details header MUST identify the selected word-context, and detail content MUST begin directly with its tabs/content without a repeated summary card. Summary data MAY remain loaded for title and state orchestration.
- **FR-036**: The details card MUST provide three tabbed sections: **الآيات الخاصة بالكلمة**, **السور**, **التحليل**.
- **FR-037**: The **الآيات** tab MUST list the ayahs containing the row-context occurrences and MUST highlight the matching word occurrences for that context; it MUST reflect the active filter context (e.g. under فعل → أمر, highlight only imperative-verb occurrences).
- **FR-038**: The **السور** tab MUST show the surah distribution for the row context (and may show which surahs do not contain it), consistent with the existing explorers.
- **FR-039**: The **التحليل** tab MUST present the full grammatical analysis for a chosen occurrence, reusing the existing per-word analysis capability.

#### J. I'rab (grammatical parsing) integration

- **FR-040**: I'rab MUST be treated as secondary information, never as a type-tree dimension.
- **FR-041**: Nominal **case** and verb **tense/voice** MUST be sourced from the word's head (main) grammatical features, exposed as secondary filters/displays only.
- **FR-042**: Full i'rab MUST appear only as a per-occurrence detail/action inside the التحليل tab (reusing existing analysis); it MUST NOT influence any tree/table count.
- **FR-043**: Sub-word / segment-level simple i'rab MUST NOT affect tree or table counts in v1.

#### K. Data correctness gate

- **FR-044**: Before this feature is considered correct, the live data MUST reflect the corrected label/category for the prohibition particle so that it is classified under **حرف وأداة** (particle), not under **اسم**. (Pre-implementation gate — see Assumptions / Dependencies.) If this correction is not yet applied in the data, the affected words will mis-bucket; the implementation MUST be verified against corrected data.

#### L. Reuse & non-disruption

- **FR-045**: The feature MUST reuse the existing Words-hub explorer patterns (split-view table + details, ayah list, surah distribution, per-word analysis, URL-restorable state) rather than introduce parallel mechanisms.
- **FR-046**: The feature MUST NOT change or disturb the behavior, results, or contracts of the existing Roots / Lemmas / Stems / Unique-Words explorers.
- **FR-047**: The feature MUST be read-only with respect to Quran data: it MUST NOT modify, import, or re-derive any Quran word, morphology, or label data.

#### M. Grouped detail drilldown (Feature 023)

- **FR-048**: Every word/root/stem/lemma row container MUST be inert and non-focusable. Only its three
  native statistic buttons open details. Word mappings are occurrences/ayahs → ayahs and surahs →
  surahs; grouped mappings are occurrences → related words, ayahs → ayahs, and surahs → surahs. A
  grouped action MUST preserve exact numeric identity and full grammatical scope. The exact scoped row
  receives the shared active color until details close; no cross-scope coincidental row may be active.
- **FR-049**: Grouped detail membership and counts MUST derive from the word's **head-level** grammatical
  dimensions (`quran_word_morphology`) only, using the same scoped occurrence base as the grouped table.
  Sub-word / segment dimensions (`quran_word_morphology_segments`) MUST NOT contribute membership or
  counts, MUST NOT surface a segment-only dimension, and MUST NOT displace a word's head root/stem/lemma.
- **FR-050**: Grouped detail identity MUST be the **numeric** `root_id`/`stem_id`/`lemma_id`; the Arabic
  display text is presentation only and MUST NOT be used as membership identity. Null dimensions and
  ayah markers remain excluded.
- **FR-051**: Shareable detail identity MUST use exactly one explicit positive key: `root`, `stem`, or
  `lemma` for grouped details, or `word + contextCode` for a word detail. Detail identity is independent
  of the active `tableView`; combinations such as a roots table with a preserved stem detail MUST
  restore on refresh and history navigation. Multiple simultaneous identities fail closed, and the
  generic `dim` key is forbidden.
- **FR-052**: Detail view defaults are kind-aware: word selection defaults to `ayahs`; grouped selection
  defaults to `words`. `detailPage` remains internal page `1` when omitted or invalid, is omitted from
  canonical URLs at page `1`, is serialized only above page `1`, and is always removed for `surahs`.
- **FR-053**: URL state MUST store list scope separately from the five-field detail-scope snapshot
  (`detailType`, `detailChildCode`, `detailCase`, `detailTense`, `detailVoice`). Refresh, direct loading,
  and browser Back/Forward MUST restore both independently with identity/view/page. Selecting a child
  changes only list scope and preserves the open detail snapshot; a new statistic replaces it from the
  current list. Missing/incomplete/incompatible snapshots fail closed, and closing details clears them.
- **FR-054**: The table-view strip, the table shell, and the details host MUST remain mounted (the same
  DOM hosts) across parent, child, filter, sort, view, loading, empty, and error transitions once the
  tree has loaded. The table MUST own its prompt/loading/empty/error (with retry) inside its own body,
  and the split table/details layout MUST be retained for grouped views. `tableView` MUST survive
  type/subtype/case/tense/voice/sort/page changes — only choosing the **Words** tab returns a grouped
  view to `words`. This supersedes the Feature 022 MVP behavior that hid the strip without a leaf, hid
  the details panel and expanded the table full-width for grouped views, and reset `tableView` on main
  type / parent changes.
- **FR-054a**: Changing `tableView` MUST change only the displayed table and reset list page. It MUST
  preserve the complete open detail identity/scope/view/page/content without a detail reload. If the
  current table kind differs, no row is active; returning to the matching kind and scope restores the
  exact selected-row color.
- **FR-055**: A grouped selection MUST render, inside the always-mounted details host, kind-aware tabs —
  word → آيات/سور; grouped → الكلمات المرتبطة/آيات/سور — with RTL roving focus. Content begins directly
  with the tabs and active list; no repeated summary card is shown. Grouped detail content is paged
  member words, paged ayahs, and single-shot surahs for the selected numeric dimension and scope.
- **FR-056**: Grouped **member-word rows MUST be strictly display-only**: each row shows its word context
  and three scoped counts (occurrences/ayahs/surahs) and MUST NOT be a button/link, carry a `tabindex`,
  interactive-surface, or selected state, mutate selection, or write the URL. Only member-list pagination
  emits, and it obeys the same `detailPage` canonicalization as every other paged detail view.

### Key Entities *(include if feature involves data)*

- **Main Word Type**: one of four — اسم / فعل / حرف وأداة / حروف مقطّعة. Derived from a word's main (head) grammatical type. Has an Arabic label and a word-context count.
- **Subtype**: a grammatical type nested under a main type (nominal subtypes like اسم علم / صفة / ضمير; verb tense subtypes ماض / مضارع / أمر). Has an Arabic label (from the catalogue for nominal/particle subtypes) and a word-context count.
- **Secondary Feature Filter**: a within-type grammatical refinement — Case (مرفوع/منصوب/مجرور/غير محدد) for nominals; Tense + Voice for verbs. Not a word type.
- **Word-Context Row**: the core list unit = displayed word (Uthmani+tashkeel) + resolved grammatical context. Carries: type/subtype, applicable feature (case or tense/voice), root, lemma, stem, and occurrence/ayah/surah counts scoped to the context.
- **Occurrence**: one Mushaf appearance of a word (a surah:ayah:word position), excluding markers; the unit aggregated into the table's occurrence/ayah/surah counts and highlighted in the الآيات tab.
- **Selected-Word Details**: the right-card view of one Word-Context Row, including its ayah list, surah distribution, and per-occurrence full analysis.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: From the open page, an admin can see all words of any chosen main type within 2 seconds of selecting it, with no manual configuration.
- **SC-002**: 100% of the time, the count shown on a selected main/child filter node equals the number of rows in the table for that type/child when no secondary filter is applied (tree count == table total for unscoped type/child selections).
- **SC-003**: For any word that is used with two or more grammatical types under the active filter, the table shows it as the correct number of separate rows (one per context), with zero mixed/aggregate rows — verifiable on a known sample (e.g. a word used as both اسم and صفة yields 2 rows).
- **SC-004**: Every table row's المواضع / الآيات / السور reflect only that row's context: re-counting the highlighted occurrences in the details card's الآيات tab matches the row's الآيات value 100% of the time.
- **SC-005**: Disconnected letters never appear in the particle (حرف وأداة) results, and the sum of disconnected-letter rows is identical whether counted from the main type or the table (no double counting).
- **SC-006**: For nominal selections the case filter is present and for verb selections the tense+voice filters are present; for particle/disconnected-letter selections none are present — 100% of selections obey these visibility rules.
- **SC-007**: All displayed words appear with full tashkeel; the page offers no without-tashkeel toggle.
- **SC-008**: A shared deep link restores the exact filters and compatible selected word-context or
  grouped identity in 100% of cases, including words that produce multiple rows.
- **SC-009**: Markers and sub-word parts contribute to zero counts anywhere on the page (verifiable against known totals).
- **SC-010**: The existing Roots / Lemmas / Stems / Unique-Words explorers produce identical results before and after this feature ships (no regressions).
- **SC-011**: An admin can go from opening the page to viewing a specific word's ayahs and analysis in at most 4 interactions (select type → optional subtype/feature → select row → open tab).

---

## Assumptions

- **Data already exists**: The system already stores, per Qur'an word, its main (head) grammatical type and word-level grammatical features (verb tense, verb voice, nominal case), plus links to root / lemma / stem. This feature reads that data; it does **not** create or re-derive it.
- **Main type source**: The main word type is the word's head part of speech (defined as the first stem part of the word). Multi-part words use this existing head policy; secondary stems are not surfaced in v1.
- **Sub-word types out of scope**: Prefix/suffix/segment-level parts of speech are excluded from all type buckets and counts in v1; they may become a separate future feature.
- **Display**: Uthmani-with-tashkeel only; no Simple/without-tashkeel toggle in v1. Identity for row grouping is the fully-vowelled word form + grammatical context.
- **Labels**: Arabic type/subtype labels come from the existing word-type catalogue; only the four main-type headings and the secondary-filter option words are fixed UI strings.
- **Reuse**: The page reuses the existing explorers' split-view, ayah list, surah distribution, per-word analysis, caching, and URL-state patterns; it adds a new type-filter picker as the only genuinely new interaction element.
- **Read-only & isolated**: No migrations, no importers, no data writes; existing explorers and their contracts are untouched.
- **Optional/deferrable scope** (default decisions for v1, can be tightened in planning):
  - Nominal subtypes beyond اسم/اسم علم/صفة/ضمير (e.g. اسم موصول, اسم إشارة, ظرف) are **included** because the catalogue already labels them.
  - Specific particle subtypes under حرف وأداة are **optional**; the parent "all particles" selection is required.
  - **الأصل** (lemma) and **الصيغة** (stem) columns are **included** as columns, but their winner-enrichment values may be deferred in v1; if deferred, the API returns null and the UI displays `—` without dropping the row.

## Dependencies

- **Corrected prohibition-particle data (pre-implementation gate)**: The live data must classify the prohibition particle (لا الناهية) under particle/حرف وأداة, not under nouns. This correction exists at the source catalogue; the running data set must reflect it before the feature is validated, otherwise ~hundreds of those words mis-bucket. Verifying this is the first step before implementation.
- **Existing per-word analysis capability**: The التحليل tab depends on the existing capability that returns a single word's full grammatical analysis by its location.
- **Existing word-type catalogue**: Provides Arabic labels and the noun/verb/particle category grouping for types and subtypes.

## Out of Scope (v1)

- Simple / without-tashkeel display mode and any Simple↔Tashkeel toggle.
- Internal unvowelled search input (may come later; display stays tashkeel).
- Sub-word (prefix/suffix/segment) parts of speech in any count or as a tree dimension.
- Full per-segment i'rab as a filter or tree dimension (only available as a per-occurrence detail in التحليل).
- Surfacing secondary stems of multi-part words.
- Any change to the existing Roots / Lemmas / Stems / Unique-Words explorers.
- Any data import, migration, or write operation.
