# Quran Dashboard UI Design Handoff

> Status: the evidence base behind the Golden UI system — the input authority `01 GOLDEN_UI_SYSTEM.md` and `02`/`03` cite by section. It records what the UI looked like at the snapshot below; it is not production UI doctrine and not an implementation specification. The living contracts in this folder supersede it wherever they disagree, and `GOLDEN_VISUAL_VERIFICATION.md` owns the verification method.

## Audit basis and counting rules

- Snapshot: branch `audit/project-simplification`, commit `32547a82`, 2026-08-09.
- Runtime evidence: the user's existing authenticated Google Chrome session, inspected without restarting Chrome, replacing the profile, re-authenticating, or submitting writes.
- Source evidence: the Angular source at that snapshot, generated API models, `PRODUCT.md`, `DESIGN.md`, and `.architecture/UI_STYLE_SYSTEM.md`, plus the then-active frontend architecture plan as engineering context only.
- Screenshot evidence: 44 accepted PNGs under `/tmp/quran-ui-design-handoff.L6BYle/current-ui/`. Access screenshots are redacted.
- Surface count: **25 meaningful visual/operational rows**. This is 19 routed patterns expanded where valid query modes create materially different surfaces, plus the cross-route detail overlay. It is not a count of Angular route declarations.
- Canonical-family count: **20 design-family candidates**, comprising 18 cross-cutting families and 2 high-value feature compositions. This is not a proposed Angular component count.
- Drift count: **50 current drift findings** in the D01-D50 ledger: 48 direct removal targets plus 2 decision-gated inconsistencies (D36 and D38). Counts are findings, not CSS selector or call-site totals.
- Genuine-difference count: **24 domain differences** in the G01-G24 ledger.
- Evidence labels: `LIVE` means inspected in Chrome, `SOURCE` means source-only, and `BOTH` means runtime and source agreed.
- Accessibility limit: screenshots and DOM/source inspection identify risks, but do not prove WCAG conformance or full screen-reader behavior.

---

## 1. Executive summary

The application already has a coherent product identity: Arabic-first RTL, a flat parchment surface system, one scholarly green for action/current state, stable Quran typography, and quiet research-tool behavior. The fragmentation is primarily contractual rather than brand-level. The same page shell, table, details, tab, picker, pagination, state, and modal concepts have been implemented repeatedly with different spacing, geometry, semantics, breakpoints, and mobile transformations.

The most consequential findings are:

1. **Five Words explorers are one table/detail family, not five independent designs.** Their data columns, filters, and detail tabs legitimately differ, while their shell, state, selection, pagination, responsive drawer, accessibility semantics, and interaction vocabulary should converge.
2. **Tablet is the most visibly broken band.** At a requested 768px viewport, Roots, Lemmas, Abwab, and Access measured a 961px document width; Word Types measured 866px. The current 767/768 boundary exposes desktop navigation and feature minimum widths too early.
3. **Page gutter ownership is inconsistent.** Access uses one combined frame/container owner; Abwab and Words commonly nest owners and double padding; the placeholder header sits outside its body container; Word Types closes its frame at a different structural point.
4. **Async concepts are not separated.** `qd-state` is used for loading, empty, error, notice, and mutation feedback, while other areas use structural skeletons. Access reserves a large blank mutation band, but other branches resize.
5. **Modal shells are visually related but geometrically fragmented.** Authoring dialogs, confirm dialogs, Words drawers, and the global detail overlay use several width/height/padding/scroll contracts. Workflow content differs genuinely; shell behavior generally should not.
6. **Access and Abwab need dedicated Golden compositions.** They should reuse the same primitives, but their safety, hierarchy, permission, and review semantics are too important to reduce to a generic card or table.
7. **Quran surfaces are protected differences.** Quran text, fonts, full-text growth, ayah cards, Mushaf page measurements, study-source hierarchy, and similarity/Mutashabihat meaning must not be normalized into generic admin patterns.
8. **Current drift conflicts with the approved visual contract.** Gradients remain in select chevrons and skeleton shimmer; generic hover sometimes consumes the green state color; buttons translate on active; and Word Types uses a kinetic entrance treatment.

Claude Design should work family-first. Each Golden family request in section 20 names its consumers, real data, states, responsive obligations, genuine variations, and variations that should be removed. No page-by-page redesign should start before those family decisions are made.

---

## 2. Complete route and surface inventory

### 2.1 Implemented and nested surfaces

| # | Route or surface | Purpose and major regions | Actions and visible data | Current responsive behavior | Reusable and specialized patterns | Evidence |
|---:|---|---|---|---|---|---|
| 1 | `/dashboard` | Authenticated landing page with application information and feature navigation cards. | Navigate to Mushaf, Words, Abwab, and other modules; app name, version, and environment. Footer health is shell data, not Dashboard app-info data. | 1440 shows an uneven 4+1 auto-fill result with unused space; 390 stacks cleanly. | App shell, page header, navigation cards, async app-info state. | BOTH; `dashboard-home.component.*`; screenshots `03`, `04`. |
| 2 | `/dashboard/mushaf` | Protected Mushaf reader: page/navigation region plus selected-word and selected-ayah study region. | Page/surah navigation, word selection, ayah tabs, source selection, read-only related-data navigation. Data includes page lines, markers, Quran words, morphology, Tafsir, translation, i'rab, similar ayahs, and Mutashabihat. | Desktop is a 40/60 split with sticky measured Quran panel; below 1024 it stacks; 390 has no horizontal overflow and a long reader-then-study document. | Protected Quran canvas, study cards, source pickers, tabs, structural skeletons. | BOTH; `mushaf-reader-page.component.*`; screenshots `01`, `02`, `32`-`36`. |
| 3 | `/dashboard/words` | Teaching/navigation hub for the Words explorers. | Navigate through an ordered set of explorer concepts; explanatory Arabic copy. | Deliberate two-column 2+2+1 layout above 640; stacked on phone. | Page header, explainer/card family, concept progression. | BOTH; `words-hub-page.component.*`; screenshots `05`, `06`. |
| 4 | `/dashboard/words/unique/tashkeel` | Explore vocalized unique word identity. Table plus selected detail. | Search, filters, sort, paginate, select; detail order is Surahs, Missing Surahs, Ayahs. | Desktop split table/detail; `<=1023` drawer; 390 card rows; internal desktop scrolling. | Explorer shell, table, details, filters, drawer, overlay links. | BOTH; `unique-words-page.component.*`; screenshot `15`. |
| 5 | `/dashboard/words/unique/simple` | Explore simple-form unique identity. Same family, different identity semantics. | Same actions with simple/vocalized drill-down relationship. | Same structural transformation as vocalized mode. | Same canonical explorer family with mode-specific identity slot. | BOTH; `unique-words.models.ts`; screenshot `38`. |
| 6 | `/dashboard/words/roots` | Root catalogue and related detail workspace. | Search, count ranges, sort, paginate, select; detail order is Words, Ayahs, Surahs, Lemmas, Stems, with domain subtabs. | 1440/1024 split; 768 document width 961; 390 card rows and near-full-height detail drawer. | True shared explorer table; five-view details; association links; global overlay. | BOTH; screenshots `07`-`10`, `27`-`31`. |
| 7 | `/dashboard/words/lemmas` | Lemma catalogue and relationships. | Search, root association, ranges, sort, paginate; detail order is Words, Ayahs, Surahs, Stems. | 1440 split; 768 width 961; 390 cards. Phone filter area has excessive empty height. | Explorer table/details, association picker, type filter. | BOTH; screenshots `11`-`13`. |
| 8 | `/dashboard/words/stems` | Stem catalogue and root/lemma relationships. | Search, root and lemma associations, ranges, sort, paginate; detail order is Words, Ayahs, Surahs, Lemmas. | Desktop split; phone card rows/drawer by shared contract. | Explorer table/details with two association filters. | BOTH; screenshot `14`. |
| 9 | `/dashboard/words/types` | Morphology taxonomy browser and word/grouped tables. | Main type, child, case/tense/voice, word search, presence filters, sort, table-view tabs, counts, selection. | 1440/1024 split; 768 width 866; 390 has no horizontal overflow but excessive vertical whitespace and wrapped controls. | Specialized filter tree composed with shared table/details; independent count state. | BOTH; screenshots `16`, `39`-`41`. |
| 10 | `?qdDetail=...&qdDetailOpen=...` on Words routes | Cross-route entity detail overlay with URL/history stack. | Open linked entity, Back, Close, Restore retained stack; Root/Lemma/Stem/Unique/Word Type-word adapters. | Desktop centered 46rem shell; phone near-full-height shell, including over a feature drawer. | Global focus-trapped dialog, adapter content, stack cap 8. | BOTH; screenshots `30`, `31`; `detail-overlay.*`. |
| 11 | `/abwab?view=tree` | Live hierarchical Abwab workspace. Header, section tabs, search, ARIA tree, selected-action rail. | Select/expand, local search, open add/edit/move/relations/archive dialogs when permitted; hierarchy/counts/relations. | 1440 split with 18rem sticky rail; 768 width 961; 390 stacks. Deep indentation and small hover actions are stressed. | Tree, toolbar, tabs, side panel, menus, authoring dialogs. | BOTH; screenshots `17`-`19`. |
| 12 | `/abwab?view=cards` | Card/drill-down alternative for the current tree level. | Drill into children, select, optional bulk selection; order/name/direct-child count. | Auto-fill `minmax(13rem,1fr)` has no maximum; phone stacks. | Breadcrumb, card grid, empty/no-match, side panel. | BOTH; screenshot `42`. |
| 13 | `/abwab?archive=1` | Read-only archived hierarchy. | Expand; permitted root Restore entry point. Descendants and missing-permission Restore can remain visibly disabled. Archive rows are not a selection workspace. | Same page shell and stacked rail; current live capture was empty. | Read-only ARIA tree, disabled capability disclosure, empty state. | BOTH; screenshot `43`. |
| 14 | `/abwab/templates` | Template list and hierarchy editor workshop. | Create/select/edit/delete/apply templates; add/edit/reorder/delete nodes; apply the template root's children to selected doors when permitted. | 15.5rem list rail and editor at desktop; stacks at `<=1023`. | List/editor split, list skeleton, template hierarchy, authoring dialogs. | BOTH; screenshot `20`. |
| 15 | `/settings/access` | Owner-only default Workspace surface. The canonical default removes the `tab` query key; `?tab=workspace` is normalized away. Header/tabs, 20rem user rail, selected-user details, permission/lifecycle review. | Search/filter/select users; stage permissions; review Accept/Disable/Reactivate flows. No mutation was submitted. | 1440 split; 768 width 961; 390 stacks list before detail. | Master/detail, status badges, permission groups, inline change review. | BOTH; redacted screenshots `22`-`24`. |
| 16 | `/settings/access?tab=security` | Owner-only advanced security and reconciliation status. | Relink fields/preview/confirm and read-only reconciliation disclosure. Relink preview is a POST and was not exercised. | At `>=1024`, advanced security and reconciliation use a 1.5fr/1fr grid; below desktop they stack. The reserved state spans both desktop columns. | High-risk inline workflow, disclosure, async state. | BOTH for resting UI, SOURCE for actions; redacted screenshot `25`. |
| 17 | `/settings/access?tab=audit` | Owner-only audit log and filters. | Target/actor pickers, action and permission filters, Apply, Load more. Shows action/time/target/actor/optional reason. | Desktop two-column filter/event composition; long mobile flow is source-inferred. | Searchable user pickers, filter toolbar, event list, pagination-like load more. | BOTH; redacted screenshot `26`. |
| 18 | `/callback` | OIDC callback/operational route, not a design destination. | Completes authentication state and redirects. | No durable product layout to design. | Auth lifecycle only. | SOURCE; `app.routes.ts`. |

### 2.2 Routed placeholders

These seven routes are real route surfaces but currently share one placeholder contract. They are counted because Claude Design must know they exist, not because seven independent designs are needed.

| # | Route | Current purpose/data/actions | Responsive and pattern | Evidence |
|---:|---|---|---|---|
| 19 | `/tafsirs` | Coming-soon/placeholder only; no feature data or actions. | Heading sits outside the capped body container; clean at 390 and 1024 but leaves a large empty page. | BOTH; screenshots `44`, `45`. |
| 20 | `/resources` | Placeholder only. | Shared placeholder page. | SOURCE. |
| 21 | `/i3rab` | Placeholder only. | Shared placeholder page. | SOURCE. |
| 22 | `/translations` | Placeholder only. | Shared placeholder page. | SOURCE. |
| 23 | `/audio` | Placeholder only. | Shared placeholder page. | SOURCE. |
| 24 | `/mutashabihat` | Placeholder route; current real Mutashabihat experience is embedded in Mushaf study. | Shared placeholder page. | SOURCE. |
| 25 | `/settings` | Placeholder route; Access is the implemented settings sub-area. | Shared placeholder page. | SOURCE. |

Routing evidence: `Frontend/quran-dashboard-ui/src/app/app.routes.ts`, `features/mushaf/mushaf.routes.ts:10-15`, `features/words/words.routes.ts:47-94`, `features/abwab/abwab.routes.ts:12-22`, and `features/access-admin/access-admin.routes.ts:5-14`.

---

## 3. Component-family catalog

The following are design-contract candidates. A family can become a semantic CSS contract, one or several shared Angular components, or a feature-owned composition. The catalog does not prescribe implementation boundaries.

| # | Candidate family | Current implementations and consumers | Common base contract | Genuine variations | Accidental differences to remove |
|---:|---|---|---|---|---|
| F01 | App chrome | Navigation progress, top navbar desktop/mobile, account/settings menus, footer. | RTL navigation, current route, focus, responsive disclosure, health/footer regions. | Authenticated versus anonymous actions; footer health content. | Duplicate desktop/mobile link markup, noncanonical hover fill, tablet overflow, missing mobile focus containment. |
| F02 | Page shell | `.qd-page`, `.qd-container`, `.qd-page-frame`; dashboard, Words, Abwab, Access, placeholders. | One gutter owner, named width intent, predictable block rhythm. | Capped reading, full data, split workspace, protected Mushaf. | Nested padding, cancelled max-width, header/body misalignment, extra explorer gutters. |
| F03 | Page and section headers | Dashboard/Words/Abwab/Access headers, explorer explainer headings, card/dialog section headers. | Title, optional eyebrow/description, actions, wrapping rules. | Quran/context headings may use protected typography; admin headers may carry safety actions. | Spacing, action placement, truncation, and container ownership. |
| F04 | Surface/card | Navigation cards, quiet cards, source cards, permission groups, audit events, Abwab cards. | Flat surface ladder, hairline border, semantic hover/selection, optional header/body/footer. | Quran/study content and safety warnings need specialized content slots. | Radius/padding/hover vocabulary and unbounded grids. |
| F05 | Button and action group | `qd-btn` variants, icon actions, toolbar actions, lifecycle actions, row actions. | 44px mobile target, except approved 32px dense modal workflows; label/icon/busy/disabled/focus, danger semantics. | Destructive versus safe; icon-only needs accessible name. | Heights, local copies, active translation, green used for generic hover, small controls. |
| F06 | Form field/control | Inputs, selects, checkboxes, search, textarea, alias chips, page jump, order editors. | Label, hint, required, error, disabled, busy, LTR override, focus-visible. | Quran-free-text fields must not normalize scripture; inline order editing is compact. | Input/select geometry, focus selector, local Abwab styling, dynamic pagination width. |
| F07 | Tabs and segmented controls | Shared `qd-tabs`, section tabs, detail tabs, Word Type table tabs, selected-ayah tabs. | ARIA tablist, roving tabindex, logical arrows in RTL, stable panel geometry. | Tab counts and labels vary by domain; some are route/query modes. | 18 manual tablist occurrences, mixed button/tab primitives, keyboard and grid mismatches. |
| F08 | Search/filter/toolbar | Explorer search rows, range filters, association pickers, Access filters, Abwab search/toggles. | Submit/clear, applied versus draft state, wrapping, result count, mobile ordering. | Entity-specific filter fields and Abwab mode-specific search semantics. | Arbitrary layouts/breakpoints, excessive blank height, inconsistent popup behavior. |
| F09 | Data table | Roots, Lemmas, Stems, Unique, Word Types word/grouped. | ARIA table, header, sort, selection, skeleton, empty/error, result count, pagination, desktop/internal scroll, mobile row card. | Columns, row information density, default sort, detail views. | Five repeated Angular shells, rowcount/role drift, physical selected edge, different state semantics. |
| F10 | Detail/result list | Words/Ayahs/Surahs/related entities/type distribution/group members. | Heading/count, list semantics, row/link/action, loading/empty/error/pagination. | Quran ayah cards and display-only distributions are distinct renderers. | Role/listitem inconsistency, locally repeated list shells, zero-count action behavior. |
| F11 | Detail workspace/panel | Five Words explorer panels, Access selected user, Abwab side panel, Mushaf study region. | Selected/no-selection, header/metadata, tabs, body state, related links, responsive transformation. | Domain tabs and safety workflows; Mushaf content. | Header/tab/body geometry, per-instance ID behavior, desktop blank panels, drawer variants, notFound semantics. |
| F12 | Async and feedback state | `qd-state` (53 call sites), skeleton rows/panels, notices, inline errors, retries. | Loading, refreshing, empty, error, notice, success as distinct semantics with geometry ownership. | Content-shaped Quran/table skeletons; write errors remain near origin. | One state component conflates concepts; text loaders versus skeletons; Access reserved blank band. |
| F13 | Pagination and result count | Shared pagination across Words lists/details; Access user list; load-more audit. | Total/current, previous/next, page jump, error, focus, mobile target, reserved geometry. | Load-more is not numeric pagination; some nested details have independent pages. | Focus width jump, conditional Go button, duplicate IDs, undersized mobile controls. |
| F14 | Dialog, drawer, and overlay shell | Shared confirm, six Abwab authoring dialogs, five Words drawers, global detail overlay, dirty alerts. | Focus trap, labelled title, Close/Cancel, body scroller, footer, busy/error, viewport-safe mobile geometry. | Confirmation, form authoring, hierarchy picker, and history overlay content remain separate. | Five geometry families, padding ownership, confirm overflow, legacy drawer differences. |
| F15 | Floating/action menu | Navbar menus, Abwab row/context menus, source/association popups, retained overlay restore. | Anchoring, viewport flip, keyboard entry/escape, outside close, focus return, full-label disclosure. | Tree context menus and searchable pickers have different contents. | Popup keyboard/flip behavior, hover-only actions, danger-hover override. |
| F16 | Tree and hierarchical picker | Live Abwab tree, archive tree, template hierarchy, move picker, door picker, Surah picker. | Logical RTL hierarchy, level/expanded/selected state, search, long labels, mobile target size. | Live tree, archive tree, template list, destination picker, set-selection picker have distinct semantics. | Tiny targets, uncapped indentation, inconsistent popup/listbox behavior. |
| F17 | Chip, badge, status, count | Access status/Owner, permission/count chips, Words metrics, Abwab order/relation flags, aliases. | Semantic label, optional icon/count, non-color meaning, wrap/overflow. | Lifecycle status, morphology category, count, and removable alias are separate variants. | Local sizes/fills, zero-value action inconsistency, solid-green misuse risk. |
| F18 | Quran/Ayah/study content | Mushaf page, `qdAyahCard`, highlighted ayahs, Tafsir/translation/i'rab cards, similar/Mutashabihat. | Exact source text, protected font/rendering, generous wrap, source metadata, mixed direction. | Page canvas, result ayah, long commentary, similarity and phrase grouping remain distinct. | Generic UI must not truncate or animate Quran; picker chrome can still be canonical. |
| F19 | Access Management composition | User rail, selected-user explorer, permission groups, lifecycle/review, audit, relink, reconciliation. | Safe master/detail, explicit target identity, staged changes, review before mutation, clear auth state. | Owner/lifecycle/security semantics. | Page spacing, responsive rail, blank state reserve, picker/control drift. |
| F20 | Abwab hierarchy/authoring composition | Live tree/cards/archive, side rail, templates, relations, six authoring dialogs. | Hierarchy context, selection, safe action origin, retained state, conflict/error locality. | Mode search, move/door picker, relations, archive and template rules. | Double gutters, unbounded cards, tiny tree actions, local fields, modal shell drift. |

---

## 4. UI Drift Matrix

This ledger is authoritative for the reported drift count. A row is counted once even if it has many call sites.

| ID | Contract | Current implementation and evidence | Why accidental | Canonicalization opportunity |
|---|---|---|---|---|
| D01 | Gutter ownership | `.qd-page` wraps nested `.qd-container/.qd-page-frame` in Words and Abwab. `_layout.scss:42-85`. | The domain does not require double inline padding. | One owner selected by page-shell variant. |
| D02 | Width semantics | Access combines `qd-container qd-page-frame`; the later frame rule cancels the cap while leaving a misleading class. | Class composition should not reverse its own width meaning. | Explicit `capped`, `full`, `split`, `protected` variants. |
| D03 | Page rhythm | Access bypasses common `.qd-page` and `.qd-page-header` block spacing. | Security semantics do not require unrelated page spacing. | Same header/page rhythm with composition slots. |
| D04 | Placeholder alignment | Placeholder heading is outside the body container. | Heading and message belong to one page axis. | One placeholder/empty page shell. |
| D05 | Explorer structure | Word Types closes its inner frame before the table/detail layout; the other explorers do not. | Taxonomy complexity does not require a different gutter tree. | Same explorer shell, specialized filter slot. |
| D06 | Explorer mobile gutter | `_words-explorer-layout.scss` adds another 16px inline gutter below desktop. | It duplicates page-shell responsibility. | Responsive shell owns the sole gutter. |
| D07 | Dashboard grid | `auto-fill minmax(250px,1fr)` produces 4+1 and excessive blank space. | Navigation count does not require width-driven arbitrary columns. | Deliberate 1/2/3/max-column composition. |
| D08 | Abwab card grid | `auto-fill minmax(13rem,1fr)` has no maximum. | Door-card density should be deliberate. | Max columns and stable card measure. |
| D09 | Words hub breakpoint | Local 640px rule and repeated 1024px two-column rule. | Teaching order is genuine; breakpoint vocabulary is not. | Express 2+2+1 through canonical bands. |
| D10 | Breakpoint mirror | TypeScript breakpoints omit Sass wide desktop 1440. | Parallel breakpoint truth can disagree. | One generated/shared vocabulary. |
| D11 | Raw breakpoints | 360, 420, 640, and repeated raw canonical values remain local. | Screen-specific thresholds lack a domain contract. | Named bands plus rare documented exceptions. |
| D12 | Hover surface | Navbar/dropdown/mobile links use section background instead of the shared hover surface. | Navigation does not need a distinct generic hover tone. | One hover token/semantic class. |
| D13 | Mobile navigation | Full-screen overlay lacks focus trap, inert background, scroll lock, and visible Close. | Accessibility behavior is incomplete, not a product variation. | One accessible responsive-navigation contract. |
| D14 | Button motion | Every active `.qd-btn` translates vertically. | Conflicts with calm, state-only motion and floating-layer-only transforms. | Color/border feedback only. |
| D15 | Mini-card hover | Generic hover uses accent border. | Green is reserved for current/action state, not decoration. | Neutral hover; green only for selected/current. |
| D16 | Select hover | Nonselected select hover uses accent border. | Same allowed-green conflict. | Neutral border hover, green focus/selection. |
| D17 | Select chevron | Two CSS gradients draw the select arrow. | Gradients remain prohibited throughout the control layer; the scoped multi-door Mushaf word and ayah-marker exception does not apply. | Token-compatible icon/background asset. |
| D18 | Skeleton shimmer | Loading shimmer uses a gradient. | Same locked visual conflict. | Flat pulse/tone change or static structural skeleton. |
| D19 | Word Types motion | `uw-toolbar-rise` animates the taxonomy toolbar. | Feature identity does not require decorative entrance motion. | State-only calm transition. |
| D20 | Form geometry | Input and select disagree on height, padding, radius, and line height. | Same control family, no domain reason. | One field geometry scale. |
| D21 | Focus behavior | Inputs use `:focus`; selects/other controls use `:focus-visible`. | Keyboard/pointer focus behavior should be consistent. | One accessible focus-visible contract. |
| D22 | Abwab fields | Door, picker, section, and template styles recreate inputs/buttons locally. | Authored content does not require alternate primitives. | Compose shared field/button classes; retain only layout SCSS. |
| D23 | Table shell | Five components repeat headers, loading, virtual/fallback rows, selection, and states. | Data columns vary, but shell behavior does not. | Canonical table contract with column/row renderers. |
| D24 | Table semantics | Only Word Types exposes `aria-rowcount` across five ARIA tables. | Row-count semantics are not morphology-specific. | Shared ARIA table semantics. |
| D25 | Detail-list semantics | Only part of the Words list family consistently uses `role=list/listitem`. | Related-entity meaning does not justify semantic drift. | Shared list contract and exceptions documented. |
| D26 | Selection edge | Selected explorer row uses a physical right inset. | RTL-first code should express logical inline-start. | Logical green-thread selection edge. |
| D27 | Tab proliferation | 18 feature templates manually implement tablists while shared `qd-tabs` has few consumers. | Labels/counts vary; keyboard and geometry should not. | One tab behavior with composition slots. |
| D28 | Detail-tab primitive | Root/Word Type use secondary-button styling; Lemma/Stem use tab styling. | Same interaction contract. | One details-tab variant. |
| D29 | Ayah-tab keyboard | Selected-ayah tabs lack roving tabindex/Arrow behavior. | Quran content does not require weaker tab behavior. | Same accessible tabs; protected content stays untouched. |
| D30 | Word Type grouped tabs | A three-tab toolbar is forced into two CSS columns. | Wrapping is a local implementation mismatch. | Deliberate 3-item mobile/desktop layout. |
| D31 | Detail DOM IDs | Lemma/Stem use fixed IDs while inline and overlay copies coexist; Root uses per-instance IDs. | Duplicate IDs are an implementation defect. | Shared per-instance ID generation. |
| D32 | `notFound` semantics | Root/Stem leave tabpanel semantics; Lemma/Word Type retain or null labels. | Error identity should not alter roles arbitrarily. | One detail-state semantic rule. |
| D33 | Popup keyboard | Surah, source, and association pickers support different key sets. | Hierarchy differs genuinely; entry, escape, focus, and navigation basics do not. | Shared popup/listbox behavior contract. |
| D34 | Popup geometry | The same pickers differ in max height, above/below flip, and viewport collision. | Content shape does not justify clipping inconsistency. | One anchored-layer geometry utility with variants. |
| D35 | Full-value disclosure | Entity/source/header ellipsis often relies on pointer-only `title`. | Keyboard/touch users cannot reliably discover full text. | Focus/touch disclosure and accessible full label. |
| D36 | Zero-count actions | Word Type metric chips remain actionable at zero while peer explorers usually disable empty detail links. | Needs one explicit product rule, not family drift. | Define empty-detail navigation behavior once. |
| D37 | Segment affordance | Morphology segment rows are native buttons with no handler/output. | The control promises an action that does not exist. | Make them noninteractive or define an approved interaction later. |
| D38 | Inert URL state | Mushaf `panel`, `wordTab`, and `segment` serialize/hydrate but have no visible consumer. | URL state and visible state disagree. | Product decision before design; do not invent behavior. |
| D39 | Async semantics | `qd-state` conflates loading, empty, error, notice, and mutation feedback. | These states need different semantics and geometry. | Separate Golden state contracts. |
| D40 | Loading vocabulary | Access/content areas use loading text while Words/Mushaf use structural skeletons. | Similar surfaces should preserve geometry consistently. | Content-shaped skeleton rules plus inline refresh. |
| D41 | Access reserve | Empty mutation region leaves roughly 6.5rem blank; other branches resize. | Safety does not require invisible permanent whitespace. | Stable but intentional state slot geometry. |
| D42 | Pagination jump width | Jump input doubles width on focus. | Neighboring controls shift during interaction. | Fixed reserved width. |
| D43 | Pagination Go button | Go mounts only when jump mode becomes active. | Conditional mounting moves controls. | Reserve control or use stable inline submission. |
| D44 | Pagination IDs | Every instance emits the same jump input/error IDs. | Multiple visible pagers can create duplicate IDs. | Per-instance IDs. |
| D45 | Pagination targets | Phone controls shrink to 28-32px. | Below the intended 44px mobile action target. | Mobile target contract independent of visual density. |
| D46 | Tree/picker targets | Abwab chevrons/actions are commonly 16-20px and hover-dependent. | Touch use is not a different domain. | 44px hit area with visually quiet icon. |
| D47 | Mushaf navigation targets | Source confirms navigation buttons use a 2rem minimum height and the page trigger has no 44px minimum. | Protected Quran rendering does not protect undersized chrome. | Canonical 44px action hit area around unchanged page content; verify computed geometry after implementation. |
| D48 | Modal geometry | Confirm 28rem, base 36rem, legacy drawer 42rem, overlay 46rem, wide 52rem, with differing heights. | Workflow size can vary, but shell rules are fragmented. | Named modal sizes on one viewport/scroll contract. |
| D49 | Confirm shell | Confirm lacks authoring viewport/overflow rules and likely owns both base and section padding. | Confirmation content should remain viewport-safe. | Canonical shell, one padding owner, body scroller. |
| D50 | Danger menu hover | Templates locally neutralize the shared danger-item hover. | Same destructive action should have one feedback contract. | Shared danger menu item with no local override. |

---

## 5. Real UI data-shape catalog

| Family | Required fields and relationships | Optional/variable fields | Scale and content stress | Source |
|---|---|---|---|---|
| Dashboard app info | App name, version, and environment. | Loading/error and missing metadata. Footer health is a separate shell shape. | Long version/environment values must wrap or deliberately truncate. | `dashboard-home.component.html`, `app-info-data.ts`. |
| Mushaf page | `pageNumber`, `ayahRange`, `lines[]`, `markers[]`, `navigation`, nullable previous/next, `surahs[]`. Lines contain number/type/centering and Quran words; words contain Uthmani text, verse key, location, order. | Surah marker/sajdah metadata; page boundaries. | 15-line measured page geometry; long Quran lines must wrap only according to protected renderer. | `core/api/generated/models/mushaf-page-response.ts:9-17`, `mushaf-line-dto.ts:5-10`, `mushaf-word-dto.ts:4-10`. |
| Word analysis | Occurrence, identity counts, morphology, `renderedWordSegments[]`. | Root/lemma/stem, root/lemma Buckwalter, case, tense, voice, POS/i'rab/rule/features, simple/QPC forms. | Multiple segments, mixed Latin codes/Arabic labels, missing relationships. | `word-analysis-response.ts:8-13`, `word-morphology-dto.ts:8-18`, `rendered-segment-dto.ts:6-20`. |
| Ayah study | Required ayah, selected source keys, similarity summary; Tafsir, translation, full i'rab. | Each study entry nullable; 0/1/many source options; sanitized HTML; direction/language/group metadata. | Unbounded commentary text and mixed RTL/LTR; up to full ayah. | `ayah-study-response.ts:10-17`, source and entry DTOs. |
| Similar ayah | Verse/page/surah identities, Uthmani text, score, coverage, matched count, direction/reverse flag. | Empty result and relationship metadata. | Long full ayahs, several numeric measures. | `similar-ayah-item-dto.ts:4-19`. |
| Mutashabihat | Group key/source, counts, nullable phrase, representative range/key, occurrences/selected occurrences. Occurrence includes page/surah/verse, word range, Uthmani text. | Phrase can be absent; groups and occurrences can be empty. | Deep group list and long Quran text. | `mutashabihat-group-dto.ts:6-18`, `mutashabihat-occurrence-dto.ts:4-16`. |
| Root | ID/text plus seven count dimensions: ayahs, lemmas, occurrences, simple words, stems, surahs, and vocalized words. Detail members link words, ayahs, lemmas, stems, and surahs. | Related arrays can be empty. | List page size can be 1000; counts can be six or seven digits. | `root-list-item-dto.ts:4-14`, root detail DTOs. |
| Lemma | ID/text, root relationship, counts, type distribution. | Root ID/text nullable; distribution/related lists empty. | Long vocalized Arabic, mixed code labels. | `lemma-list-item-dto.ts:4-15`, `lemma-summary-dto.ts:5-17`. |
| Stem | ID/text, lemma/root relationships, counts, type distribution. | Lemma/root identities nullable. | More metadata per row; genuine taller mobile row. | `stem-list-item-dto.ts:4-16`, `stem-summary-dto.ts:5-18`. |
| Unique word | Identity/mode/kind, counts, nullable primary type and root; ayah, surah, missing-surah details. | Type/root enrichment nullable. | Simple and vocalized variants; zero/many matches. | `unique-word-list-item-dto.ts:4-16`. |
| Word Type | Type tree (`code`, Arabic label, count, children, secondary filters); word rows with morphology identities and three counts; grouped root/stem/lemma rows with display and counts. | Case/tense/voice/feature/root/stem/lemma nullable; grouped members. | Four main types, many children, 9-column word table versus 5-column grouped table. | `word-types.models.ts:80-125`, generated Word Type DTOs. |
| Access user | Summary: id, display name, email, status, Owner, permission count, timestamps, version. Detail adds normalized email, subject, title, username, permissions. | Display name/title/username nullable; display falls back to email. | Long LTR email inside RTL, null display name, 19 permissions. | `access-user-summary.ts:4-13`, `access-user-detail.ts:4-17`. |
| Permission catalogue | Stable permission code, Arabic label, server-labelled group. | Catalogue unavailable/unready/empty; group indeterminate. | 19 codes in 5 groups: Doors 6, Sections 4, Relations 2, Templates 3, Template nodes 4. | `permission-codes.generated.ts:4-33`, backend catalogue. |
| Access audit event | Action, UTC time, target and actor identity, optional permission/reason. | System actor, snapshots, metadata, before/after payloads not rendered. | Long identity and reason, incremental load. | `access-audit-event-item.ts:4-27`. |
| Abwab section/tree door | Section: numeric ID, name, order, `doorsInScopeCount`, version; root counts are derived separately. Door: numeric ID, name, description, representative ayah, aliases, section/parent/orders/version, `isArchived`, `sectionRetired`, plus derived depth/live-child/live-descendant/max-relative-depth/relation counts. | Description/representative ayah can be null; aliases empty; retired/archive; permissions affect actions, not shape. | Deep hierarchy, many siblings/children, long Arabic names and aliases. | `abwab-tree-section-dto.ts:4-10`, `abwab-tree-door-dto.ts:4-18`, `abwab.models.ts:123-151`. |
| Abwab relation | Relation type and direction, anchor/targets, grouped display. | Already-linked/excluded reasons, loading/error/empty. | Four visual groups from three relation types. | `abwab.models.ts:23-86`. |
| Abwab template | Template/root/node names, descriptions, representative ayah, aliases, order, parent/depth/children, descendant count. | Empty root, copy candidates unavailable. | Deep hierarchy; root itself is not copied on apply. | `abwab-templates.models.ts:4-80`. |
| Generic UI state | Kind, title/message, optional retry/action, busy/live semantics. | Reserved versus content-shaped geometry. | Backend messages may be long; must wrap and remain near origin. | Shared state and feature facades. |

### Data rules for design fixtures

- Quran text must come from the current API/session. Never author, normalize, abbreviate, or substitute scripture for visual convenience.
- Personal data must remain sanitized. Use `.test` email domains and synthetic IDs. Do not copy live names, emails, subjects, tokens, audit identifiers, or metadata.
- Exercise null, empty, one, many, and very-large count cases. Current explorer lists can request 1000 rows; details use 100; surah lists can reach 114.
- Treat long source/entity/error labels as unbounded. Ellipsis is allowed only with a complete-value discovery path.
- Preserve direction: Arabic/Quran is RTL; email, stable codes, subject-like identifiers, and some source metadata are LTR.

---

## 6. Representative sanitized data examples

These are design fixtures, not production records. Quran content is either a verified live reference or an explicit API placeholder.

```yaml
access_users:
  - id: 900001
    displayName: null
    email: "curator.with.a.long.address@example.test"
    status: "pending"
    isOwner: false
    permissionCount: 0
  - id: 900002
    displayName: "مديرة مراجعة المحتوى ذات الاسم الطويل لاختبار الالتفاف"
    email: "owner.research.supervisor@example.test"
    status: "active"
    isOwner: true
    permissionCount: 0
  - id: 900003
    displayName: "مراجع"
    email: "disabled.curator@example.test"
    status: "disabled"
    isOwner: false
    permissionCount: 0

permission_group:
  label: "الأبواب"
  checked: false
  indeterminate: true
  items:
    - code: "abwab.doors.create"
      label: "إنشاء الأبواب"

audit_event:
  id: 910001
  actionType: "PermissionGranted"
  occurredAtUtc: "2026-08-09T10:15:00Z"
  targetUserId: 900001
  targetDisplayName: "مستخدم تجريبي"
  targetEmail: "target.curator@example.test"
  actorType: "System"
  actorUserId: null
  actorDisplayName: null
  actorEmail: null
  permissionCode: "abwab.doors.create"
  reason: "سبب تجريبي طويل لاختبار التفاف النص داخل بطاقة سجل المراجعة"

abwab_section:
  id: 920001
  name: "قسم بحثي طويل لاختبار العنوان في شريط الأقسام"
  orderValue: 1
  doorsInScopeCount: 128
  version: 3

abwab_door:
  id: 930007
  name: "باب طويل متعدد المستويات لاختبار الالتفاف والاقتطاع داخل الشجرة"
  description: "وصف بحثي غير قرآني طويل يختبر أسطر النمو داخل نموذج التحرير."
  representativeAyahText: null
  aliases: ["اسم بديل", "اسم بديل طويل لاختبار الشريحة"]
  sectionId: 920001
  parentId: 930006
  sectionRetired: false
  depth: 7
  liveChildCount: 24
  liveDescendantCount: 1284
  maxRelativeDepth: 5
  relationCount: 0
  isArchived: false

template:
  id: 940003
  name: "قالب بحثي متعدد الفروع"
  nodeCount: 47

template_apply_rules:
  copyRootDirectChildren: true
  copyTemplateRoot: false
  copiesAreDetached: true

words_layout_cases:
  counts: [0, 114, 21294, 1000000]
  nullableRelationships: { root: null, lemma: null, stem: null }
  longNonQuranLabel: "اسم مصدر طويل جدا لاختبار الالتفاف والإفصاح عن القيمة الكاملة"

verified_quran_reference:
  page: 5
  verseKey: "2:25"
  selectedWordLocation: "2:25:1"
  selectedWordUthmani: "وَبَشِّرِ"
  fullAyahText: "[actual Quran text from API]"
```

---

## 7. State matrices

| Family | Loading/refresh | Ready variants | Empty/no match | Error/conflict | Selection/dirty/busy | Responsive states |
|---|---|---|---|---|---|---|
| App shell | Lazy-route progress with 200ms delay; auth initialization. | Authenticated/anonymous nav; menu open/closed; footer health. | Not applicable. | Auth callback/session reset. | Current route, account menu. | Desktop nav; current mobile overlay; 768 overflow risk. |
| Page shell/header | Optional feature loading below stable header. | Capped, full-data, split, protected. | Placeholder page. | Route-level error/denied. | Header actions available/hidden. | 390/768/1024/1440 and adjacent 767/768, 1023/1024. |
| Table | Structural rows; background refresh without discarding rows. | Sort asc/desc, few/many columns, long text, virtual/fallback path. | Initial empty versus filtered no-result. | Transport retry; selected detail notFound/error. | Hover/focus/selected row; page and nested detail page. | Desktop table/internal scroll; mobile semantic cards; tablet must not expose page overflow. |
| Detail workspace | Header/body skeleton with shell mounted. | No selection, summary, each domain tab/subtab. | Empty tab; select prompt; deleted/notFound. | Retryable read error. | Selected item, overlay link, drawer open. | Inline desktop; drawer `<=1023`; global overlay phone/desktop. |
| Detail/result list | Skeleton rows. | Linked list, display-only list, Quran ayah cards, type distribution. | Empty and zero count. | Scoped retry. | Current subtab/page. | Wrap/stack; no generic Quran truncation. |
| Search/filter toolbar | Catalogue/options loading; background search. | Draft/applied, active count, presets/custom ranges, 0/1/many options. | No matches. | Option transport error/retry. | Open/closed picker, clear, disabled/unready. | Ordered wrap on tablet/phone; anchored popup collision. |
| Tabs | Panel skeleton independent of stable tab geometry. | Selected/default, 2/3/4/5 tabs, count labels. | Disabled/zero-content tab only if product rule says so. | Panel error does not destroy tablist. | Roving focus, RTL arrows, URL/query state. | Scroll/wrap/segmented treatment must be designed per count, not accidental CSS wrap. |
| Modal/dialog | Body skeleton/reserved title/footer. | Base/wide/form/picker/confirm/history content. | Empty picker/form auxiliary data. | Validation, server error, conflict, auth failure. | Valid/invalid, dirty-close, busy submit, destructive confirm. | Viewport-safe 390; one body scroller; focus trap and return. |
| Async feedback | Initial skeleton; subtle refreshing. | Success/notice/announcement. | Empty; filtered no-result. | Error/retry, 400/401/403/409 distinctions where meaningful. | Busy action and disabled controls without label/width jump. | Same semantic vocabulary at all widths. |
| Pagination | Reserved loading slot where list count is pending. | First/middle/last, large total, jump valid. | Zero pages. | Jump validation. | Focused jump, submitting, disabled previous/next. | 44px targets, stable fixed geometry, no duplicate IDs. |
| Access user list | Initial loading text through `qd-state`. | Pending/Active/Disabled, Owner/non-Owner, long/null name. | No users; filter no-match. | List/detail/catalogue error. | Selected user, dirty switch confirmation. | Sticky rail desktop; stacked phone/tablet with clear selected context. |
| Access permission/lifecycle | Catalogue loading/unready. | 19 permissions, group checked/indeterminate, assignment disabled. | Empty catalogue fails closed. | Invalid/conflict/forbidden/unauthorized/generic error. | +N/-M draft, Review, Discard, Accept/Disable/Reactivate busy. | Inline review must remain readable without becoming a modal by default. |
| Access audit/security | Picker/list/reconciliation loading. | Audit filters/events; relink initial/preview; reconciliation ready/blocked/candidates. | No events/no candidates/absent status; Security relink shows “choose a user” when no Workspace selection exists. | Picker/list/relink/reconciliation errors. | Clear picker, load more, reason/confirmation checkbox. | Long stacked mobile flow; LTR technical values. |
| Abwab tree | Initial structural skeleton. | Collapsed/expanded, selected, deep, many children, search-marked, bulk. | Empty tree or section. Live-tree search preserves the hierarchy and can report/mark zero matches rather than replacing it with a no-result state. | Read retry, write error, conflict. | Hover/focus/roving tabindex, order editor, menu, permission-hidden actions. | Desktop rail; stacked; uncapped depth and touch targets must be solved. |
| Abwab cards/archive | Snapshot loading from tree. | Card drill-down; archive root/child; restore visible-enabled/disabled. | Empty level/archive/no match. | Restore/read error/conflict source-only. | Selected/bulk card. | Deliberate max columns; archive remains read-only. |
| Abwab templates | List and detail load independently. | Selected, hierarchy, copy candidate states. | Empty templates, no selection, empty root/copy set. | List/detail/copy error. | New/edit/delete/apply/node busy and dirty-close. | Rail/editor stack at `<=1023`. |
| Mushaf page | Content-shaped page reservation; page navigation loading. | Page success, first/last, page number edit, surah picker. | Empty page. | Page error. | Selected word/ayah and current page. | Fixed measured desktop panel; 52rem below-desktop reservation; protected 390 flow. |
| Mushaf word/ayah | Reserved word/ayah cards and source loading. | Null/complete morphology; 0/1/many sources; each study tab. | No selection, unavailable word, empty similar/Mutashabihat. | Scoped word/ayah/source error. | Selected word, active study source/tab. | Tabs/pickers wrap; Quran content expands. |
| Global detail overlay | Adapter skeleton. | Open depth 1-8, Back, closed retained Restore. | Adapter empty/notFound. | Adapter read error; cap rejection. | Current frame, close/restore. | 46rem desktop, near-full phone; base page remains preserved. |

---

## 8. Access Management deep inventory

### Composition and data flow

- `/settings/access` is one Owner-guarded route with `workspace`, `audit`, and `security` query tabs. Unknown tab values return to workspace. Selected user is intentionally not a deep-link parameter.
- Workspace is a master/detail composition: page header and explanatory text, tabs, a sticky 20rem user rail, then selected-user detail in an explorer-like panel. At `<=1023`, rail and detail stack.
- User filters are submitted name/email search, status (`all`, `pending`, `active`, `disabled`), membership (`all`, `Owner`, `non-Owner`), and pagination.
- A row shows display name with email fallback, LTR email, optional Owner badge, lifecycle badge, and selection. The DTO also carries permission count, timestamps, ID, and version; those technical values are not row chrome.
- There are exactly three lifecycle states plus independent Owner membership. This is not a general role editor.

### Lifecycle and permission matrix

| Target | Current visible contract | Design safety requirement |
|---|---|---|
| Pending non-Owner | Permission editor; staged selection is accepted with or without direct permissions. | Target identity and activation consequence must remain explicit. |
| Active non-Owner | Permission editor; Save review; separate Disable action removes all direct grants. | Destructive removal cannot be visually equivalent to ordinary save. |
| Disabled non-Owner | No editor; Reactivate starts with no direct grants. | Disabled status and zero-grant restart must be stated, not color-only. |
| Active Owner | No permission editor/lifecycle action; Owner bypass explanation. | Do not imply Owner has editable direct grants. |
| Pending/Disabled Owner | No bypass; copy explains Owner membership does not activate access. | Status and membership remain separate labels. |
| Unknown server status | Generic unknown label. | Never silently map an unknown status to Disabled. |

- The permission catalogue contains 19 fixed Abwab codes in five server-labelled groups. Group controls support checked, unchecked, and indeterminate. `assignmentReady=false` and empty catalogue fail closed.
- Dirty state shows `+N / -M`, a sticky Review/Discard bar, an inline change-review surface, and a shared confirm when switching users. Route leave uses native `window.confirm`.
- Normal permission and lifecycle decisions are inline, not modal. The review can show added/revoked Arabic labels and stable codes, optional reason, destructive warning, no-op disabled, and busy state.
- Relink is a separate inline two-step security workflow with new subject, masked evidence token, preview, old/new subject comparison, mandatory reason, explicit checkbox, and Owner reconciliation precondition. **Preview is a POST** and was not exercised.

### Audit and security

- Audit filters: target picker, actor picker, 10-value action dropdown, permission dropdown, Apply, and Load more. The query model has date fields but current UI has no date controls.
- Event cards show action, local formatted time, target, actor or System, optional permission, and reason. IDs, snapshots, metadata, and before/after objects stay payload-only.
- Security contains advanced relink plus read-only reconciliation. Reconciliation has loading, error, absent, ready/unready, blocked/unblocked, and candidate states; its fingerprint is collapsed. `lastReconciliation` exists in the DTO but is not displayed.

### Runtime findings and responsive problems

- The live Owner workspace, Security, and Audit tabs were inspected and redacted before screenshot storage.
- At 768px, document width was 961px. At 390, the list and detail stack without horizontal overflow, but the user must traverse the complete rail before reaching selected-user detail.
- The permanently mounted mutation announcement region creates roughly 6.5rem of blank space in resting states. Other loading/error branches do not reserve equivalent geometry.
- Long names/emails use ellipsis and `title`; touch and keyboard discovery need a designed full-value path.
- Current error outcomes are `success`, `invalid`, `conflict`, `forbidden`, `unauthorized`, and generic `error`. A `409` refreshes selected state and reports conflict; writes are never automatically retried.

### Source-only and unsafe states

Not safely exercised live: access denied, non-Owner route view, catalogue failure/unready/empty, all lifecycle combinations, dirty permission review, user-switch confirmation, mutation busy/success/error, 400/401/403/409, audit empty/error, relink preview/confirm, and reconciliation candidate extremes. Accept, Disable, Reactivate, permission Confirm, and both relink steps are writes and remain forbidden for this audit.

Evidence: `features/access-admin/pages/access-admin-page/*`, `components/access-user-list/*`, `components/access-permission-editor/*`, `components/access-change-review/*`, `components/access-advanced-security/*`, `components/access-audit-log/*`, `state/access-admin.facade.ts`, and the generated Access DTOs.

---

## 9. Abwab Tree deep inventory

### Route and mode contract

- `/abwab` has live tree, cards, and archive modes; `/abwab/templates` is the template workshop.
- The URL contract owns `section`, `view`, `archive`, `door`, `card`, `q`, and `modal`. Modal values include `create`, `child`, `edit`, `move`, `sections`, `relations`, and retained `-closed` forms. The special `relations-<id>-closed` form keeps the relation source pinned after another door is revealed.
- The page header conditionally exposes Archive, Sections, Templates, and Add root. Below it are retained-modal Restore/Discard, two result counts, section tabs, search, tree/cards toggle, main content, and a sticky selected-action rail.
- Search meaning is intentionally mode-specific: live tree marks matches while retaining the hierarchy; cards filter the current level; archive prunes to visible matching paths; pickers filter their hierarchy; whitespace-only input is not filtering.

### Live tree anatomy

Every live row is an ARIA `treeitem` with level, expanded, selected, and roving-tabindex state. Its visual anatomy can contain:

1. depth-derived indentation;
2. optional bulk checkbox;
3. expand/collapse chevron;
4. order chip or inline numeric order editor;
5. door name;
6. direct-child count;
7. total-descendant count;
8. deepest-relative-depth count;
9. relation flag/count, including dashed zero;
10. add-child and overflow actions.

Keyboard behavior includes mirrored RTL arrows, Enter/Space selection or expansion, ContextMenu/Shift+F10, and bulk behavior. At `<=1023`, descendant and depth columns disappear, while direct-child and relation counts remain. Current indentation is unbounded; only the name shrinks. Chevrons/actions are around 1.25rem and commonly become visible through hover/selection, creating a touch and discoverability problem.

Door view data includes identity, name, description, representative ayah free-text, aliases, section and retired-section, parent, section/global order, optimistic version, archived state, derived depth, child/descendant/max-depth counts, relation count, and children. Description, representative ayah, and aliases are authoring-only; there are no created-by, approved-by, or timestamp fields on the current door DTO.

There is **no current protected/locked/pending door property or UI**. Current state is live/archived, live/retired section, selected/bulk/search/revealed, permission-hidden/static order, picker excluded/disabled, and request loading/error/conflict. Later prose that said the tree renders `protected` conflicted with the DTO; Claude Design must not invent a lock state.

### Cards, archive, side panel, and templates

- Cards show derived breadcrumb, order, truncated name, direct-child count, selected/bulk state, and drill-down. They intentionally have no context menu. The `repeat(auto-fill,minmax(13rem,1fr))` grid is unbounded.
- Archive is a separate read-only ARIA tree. Root archived doors may expose Restore. Descendants require the parent first; missing permission can leave Restore visible but disabled with an explanation. Archive omits edit, move, reorder, add, bulk, relations, and live counts.
- The side panel shows the selected door or a prompt, then permission-specific add/edit/move/relations/archive actions. Bulk mode shows count/names and permitted bulk actions.
- Templates use a 15.5rem list rail and a hierarchy editor. Template hierarchy deliberately uses `role=list/listitem`, not tree, because it does not implement live-tree arrow navigation. Template fields mirror authored doors. Applying a template copies root direct children, never the template root, and creates detached doors with no template provenance.

### Authoring and permission behavior

The six authoring dialogs are Door, Move, Sections, Relations, Template node, and Template copy. The common fixed shell is `min(92dvh,44rem)`, becoming `min(94dvh,44rem)` on phones, with body-only scrolling and intentionally stable empty height.

- Door create/edit uses required name, description, optional representative ayah free-text, aliases, and root-section selector when required. No live sections is a blocking state.
- Move uses section tabs, optional cross-section destination selection, search, pinned root destination, hierarchy, and cycle/subtree exclusion.
- Sections manages create, rename, reorder, and delete with nested confirmation and dirty-discard alert.
- Relations has loading/error/empty/grouped states; similar, opposite, more-comprehensive, less-comprehensive views; conditional direction; door picker; and nested delete confirmation.
- Template node reuses the authored-field form.
- Template copy explains size/root-not-copied/detached-copy behavior and uses a multi-select door picker.

There are 19 independent write capabilities. Reads remain public. Most missing writes are hidden, order becomes static, and archive Restore is the deliberate visible-disabled exception. Relations remain readable without write permission.

### Runtime and source-only states

- Live tree, card, empty archive, templates resting UI, and Add-root dialog were inspected. Add-root was closed without saving.
- At 768px the page measured 961px wide. At 390 it stacked without page overflow, but deep indentation and 20px actions remain structurally risky.
- Source-only: populated archive/restore cases, retired section, all permission combinations, extreme depth/many siblings, tree/section/template order edits, no-section root creation, relation groups, move exclusions, dirty alerts, validation, busy, write errors, 401/403/409, bulk conflict, and template apply/copy results.
- Unsafe: pressing Save/Add/Delete/Archive/door-or-archive Restore/Move/Apply or a server-write Confirm. Enter in tree/section/template order editors, new-template form, and template quick-add can write and was not used.

Evidence: `models/abwab.models.ts`, `components/abwab-tree/*`, `abwab-cards/*`, `abwab-archive-view/*`, `abwab-side-panel/*`, `abwab-door-picker/*`, all six modal components, templates page/components, and generated Abwab DTOs.

---

## 10. Modal and dialog catalog

| Type | Purpose/trigger | Content and actions | States and safety | Current geometry/behavior | Classification |
|---|---|---|---|---|---|
| Shared confirm | Access dirty user switch; Abwab single/bulk archive, restore, section delete, relation delete, template delete, template-node delete. | Title, projected warning/details/error, Cancel, destructive or safe Confirm. | Initial, busy, error; seven Abwab cases plus one Access case. No confirmation was submitted. | `min(28rem,100%)`; no owned max viewport height/body scroller; possible double padding. | `CAN_USE_ONE_CANONICAL_MODAL_SHELL` with danger/safe variants. |
| Abwab Door | Add root/child or edit door. | Authored fields, aliases, conditional section, server/missing-section error, Save/Cancel, dirty alert. | Valid/invalid, no sections, busy, server error, dirty close. | Fixed 44rem family, base width; phone 94dvh. | `CANONICAL_SHELL_WITH_OPTIONAL_SECTIONS`. |
| Abwab Move | Move one/bulk doors. | Section tabs, destination search/tree, pinned root, exclusions, Confirm/Cancel. | Single/bulk/cross-section, local search/no-match, selected destination, disabled Confirm. The picker has no inline busy/error/conflict surface. | Wide fixed shell, body scroll. | `GENUINELY_SPECIALIZED_MODAL` content on canonical shell. |
| Abwab Sections | Create/rename/reorder/delete sections. | Inline editor rows, nested delete confirm, dirty alert, Close. | Ready/error, draft, busy writes, delete confirm, dirty close; no loading branch. | Fixed shell with internal worklist. | `GENUINELY_SPECIALIZED_MODAL` content on canonical shell. |
| Abwab Relations | Review/add/delete relations. | Grouped relations, type/direction, door picker, Add, nested delete. | Loading/error/empty/grouped; single anchor/multi target; excluded/already linked. | Wide fixed shell, body scroll. | `GENUINELY_SPECIALIZED_MODAL` content on canonical shell. |
| Template node | Add/edit template node. | Same authored fields as Door, Save/Cancel, dirty alert. | Valid/invalid/busy/error/dirty. | Fixed shell. | `CANONICAL_SHELL_WITH_OPTIONAL_SECTIONS`; share form contract with Door. |
| Template copy | Apply template children to selected doors. | Size/copy rules, target picker, Confirm/Cancel. | Loading/error/empty/selected, busy/disabled. | Wide fixed shell. | `GENUINELY_SPECIALIZED_MODAL` content on canonical shell. |
| Root mobile drawer | Explorer detail on `<=1023`. | Header, five tabs and nested subtabs, lists, Close/backdrop/Escape. | Loading/empty/error/notFound/success. | Legacy drawer around 42rem x 36rem, phone 88dvh; one internal scroller. | `CANONICAL_SHELL_WITH_OPTIONAL_SECTIONS`. |
| Lemma mobile drawer | Same purpose, four domain tabs. | Lemma details and associations. | Same state family. | Same legacy family, differing IDs/tab primitive. | `CANONICAL_SHELL_WITH_OPTIONAL_SECTIONS`. |
| Stem mobile drawer | Same purpose, four domain tabs. | Stem details and associations. | Same state family. | Same legacy family. | `CANONICAL_SHELL_WITH_OPTIONAL_SECTIONS`. |
| Unique drill-down | Selected unique identity details. | Three views and mode identity. | Loading/empty/error/notFound/success. | Drawer/modal family; page banner can remain when closed. | `CANONICAL_SHELL_WITH_OPTIONAL_SECTIONS`. |
| Word Type mobile drawer | Selected word or grouped identity. | Two word tabs or three grouped tabs. | `selectPrompt`, loading/empty/error/notFound/success. | Legacy family; current 3-tab/2-column mismatch. | `CANONICAL_SHELL_WITH_OPTIONAL_SECTIONS`. |
| Global entity detail | Cross-route deep detail/history. | Entity kind/title/count, Back, Close, adapter content, status; retained Restore when closed. | Depth 1-8, loading/empty/error/notFound, cap rejection. | 46rem x `min(92dvh,44rem)`; phone `94dvh`; focus trap. | `GENUINELY_SPECIALIZED_MODAL` because URL/history is material. |
| Embedded dirty alert | Door, Sections, Template node footer. | Discard edits versus continue editing. | Dirty only, nested within authoring dialog. | `alertdialog` strip inside footer, not another full modal. | `CANONICAL_SHELL_WITH_OPTIONAL_SECTIONS`; canonical nested warning. |
| Native route-leave confirm | Leaving Access with a dirty permission draft. | Browser-provided warning/accept/cancel. | Dirty only. | Native `window.confirm`. | Source behavior to preserve or explicitly replace later; not visually designable now. |

Not current modal types: Access permission/lifecycle review and relink are intentionally inline; Approve/Reject are not current generic product modal contracts. Do not design or claim current variants that source does not contain.

---

## 11. Details-family catalog

| Implementation | Header and content | Geometry/responsive | Genuine variation | Drift to remove |
|---|---|---|---|---|
| Root detail | Root title/summary, five rendered views in order: Words, Ayahs, Surahs, Lemmas, Stems; simple/vocalized and mentioned/missing subtabs. | Desktop inline split; `<=1023` drawer; frameless in global overlay. | Five views and root relationships. | Secondary-button tabs, instance-ID behavior differs from peers. |
| Lemma detail | Lemma/root metadata, four rendered views in order: Words, Ayahs, Surahs, Stems; type filter. | Same three render modes. | Nullable root and type distribution. | Fixed DOM IDs and different notFound roles. |
| Stem detail | Stem/lemma/root metadata, four rendered views in order: Words, Ayahs, Surahs, Lemmas; type filter. | Same three render modes. | Two nullable parent relationships and taller mobile information row. | Fixed IDs and semantic drift. |
| Unique detail | Mode/word identity, three rendered views in order: Surahs, Missing Surahs, Ayahs. | Inline/drill-down drawer/overlay adapter; restored closed errors use a one-line page slot. | Simple/vocalized identity and missing-surah meaning. | Banner geometry is fragile; shell should align with family. |
| Word Type detail | Word identity: Ayahs/Surahs. Group identity: Words/Ayahs/Surahs. | Inline/drawer; global overlay only for word identity. | Grouped root/stem/lemma stays local; grouped members are display-only. | Mixed tab primitive, 3-tab wrap, zero-count actions. |
| Global detail overlay | Adapter-owned detail content with entity kind/title/count and history. | Fixed centered/near-full mobile shell. | URL frame grammar, Back, retained Restore, depth cap 8. | Body/detail semantics should reuse the same canonical content contracts. |
| Access user detail | Identity/status/Owner summary, permission editor or lifecycle explanation, inline review. | 20rem rail plus explorer body; stacks at `<=1023`. | Safety/lifecycle composition, not a Words entity explorer. | Header/state spacing and huge blank mutation reserve. |
| Abwab selected side panel | Selected door summary and permitted actions; bulk selection summary. | Sticky 18rem desktop rail; stacks below 1024. | Action-led hierarchy context; authored metadata is not shown. | Generic empty prompt/action spacing and small controls should align. |
| Mushaf selected word | Segment rendering, morphology, root/lemma/stem identity, occurrence/count metadata. | Study pane desktop; stacked below 1024. | Protected Quran text and morphology palette. | Segment rows falsely appear actionable; generic chrome can unify. |
| Mushaf selected ayah | Ayah header, five study tabs, source selection, commentary/results. | Same study pane, long expanding document on phone. | Tafsir/translation/i'rab/similar/Mutashabihat semantics. | Tab keyboard and picker behavior should unify without touching Quran rendering. |

Canonical direction: one details shell with optional identity, metadata, action, tab, status, and related-content zones; feature adapters own data and domain sections. Do not create one giant component with dozens of boolean inputs.

---

## 12. Table-family deep inventory

### Classification

| Surface | Classification | Columns/data | Interaction and geometry | Responsive transformation |
|---|---|---|---|---|
| Roots | `TRUE_SHARED_TABLE_CONTRACT` | Root identity plus seven count dimensions represented by the DTO. | Sort, select, virtual/fallback rows, pagination, detail links; desktop row around 2.5rem. | Mobile card row min 5.5rem; detail drawer. |
| Lemmas | `TRUE_SHARED_TABLE_CONTRACT` | Lemma, nullable root, counts. | Search/root association/ranges/sort/select/paginate. | Mobile card row min 6.5rem. |
| Stems | `TRUE_SHARED_TABLE_CONTRACT` | Stem, nullable lemma/root, counts. | Two association filters plus same shared behavior. | Mobile card row min 6.75rem. |
| Unique Words | `TRUE_SHARED_TABLE_CONTRACT` | Word identity/kind/type/root/counts; simple or vocalized mode. | Mode route, select, sort, paginate, drill-down. | Mobile card row min 4.25rem. |
| Word Types word | `TRUE_SHARED_TABLE_CONTRACT` with specialized columns | Nine columns: row, word, type, root, stem, lemma, three counts. | Taxonomy filters, presence filters, counts, table-view tabs. | Content-driven multi-row card; control flow becomes very tall. |
| Word Types grouped | `TRUE_SHARED_TABLE_CONTRACT` with grouped row renderer | Five columns: row, root/stem/lemma dimension, three counts. | Group selection; member words display-only in detail. | Same responsive family with domain-specific row. |
| Access user rail | `LIST_THAT_ONLY_LOOKS_TABULAR` | Name/email/Owner/status. | Selectable master list with filters and pagination. | Stacks before detail; should not be forced into Golden Table. |
| Access audit events | `LIST_THAT_ONLY_LOOKS_TABULAR` | Action/time/target/actor/permission/reason. | Event cards/list and Load more. | Natural stacked event flow. |
| Abwab live/archive | `GENUINELY_SPECIALIZED_TABLE` equivalent: hierarchy, not table | Level/order/name/counts/actions. | ARIA tree keyboard and expand/collapse. | Hide secondary counts, retain hierarchy. |
| Template hierarchy | `DISPLAY_ONLY_TABULAR_CONTENT`/hierarchical list | Depth/marker/order/name/actions. | `role=list`, not tree. | Stacked editor/list. |
| Quran ayah/results | `GENUINELY_SPECIALIZED_TABLE` alternative: protected content cards | Full ayah, metadata, highlights. | Read/navigate; exact text. | Expands/wraps, never generic mobile row. |

### Shared contract requirements exposed by current evidence

- Header: one height/spacing vocabulary, sortable state, focus order, and long-label disclosure.
- Rows: roving/focusable selection rule, logical inline-start green thread, consistent `aria-rowcount`, and stable hover/selected geometry.
- Loading/empty/error: keep the table/detail shell mounted and replace body content without collapsing the split. Below desktop, current table bodies/replacement states reserve `min(70vh,40rem)`.
- Scroll: desktop tables can own horizontal/internal scroll. Tablet must not widen the whole document. Nested detail lists need a single named scroller rather than competing page/panel overflow.
- Pagination: stable slot and IDs, 44px phone targets, no focus-induced width shift.
- Mobile: semantic card-row renderers, not a squeezed desktop table. Row heights may differ because information density differs, but padding, focus, selection, labels, and action placement should be one system.
- Long values: truncate only non-Quran constrained cells with focus/touch disclosure. Quran results remain full-text cards.

---

## 13. Responsive findings

### Measured viewport evidence

| Surface | 390px | 768px | 1024px | 1440px | Required design response |
|---|---|---|---|---|---|
| Dashboard | No horizontal overflow; cards stack. | Source-inferred breakpoint transition. | Source-inferred multi-column. | No page overflow; uneven 4+1 grid and unused space. | Deliberate card count/columns; preserve readable order. |
| Words hub | No overflow; cards stack and chips wrap. | Local 640px two-column state. | Repeated two-column rule. | 2+2+1 with large remaining space. | Keep progression but use canonical bands and max measure. |
| Roots | No page overflow; semantic card rows. | **scrollWidth 961 at viewport 768**. | No page overflow; split table/detail, clipped/truncated columns. | No page overflow; split. | Tablet must use the mobile/tablet composition before desktop minimums leak. |
| Lemmas | No page overflow; unusually tall empty filter region. | **scrollWidth 961**. | Shared source behavior. | No page overflow; split. | Fix toolbar height and tablet document overflow. |
| Word Types | No overflow but very large vertical gaps/control wrapping. | **scrollWidth 866** even after auth nav simplified. | No overflow; type cards wrap awkwardly. | No overflow; wide split with blank detail. | Design a real tablet taxonomy/filter mode and stable vertical rhythm. |
| Abwab tree | No page overflow; main and rail stack. | **scrollWidth 961**. | Split begins at desktop. | No overflow; wide empty main/rail regions. | Solve 768 nav/workspace overflow, touch hierarchy, and selected context. |
| Access workspace | No page overflow; list then detail. | **scrollWidth 961**. | Split begins at desktop. | No page overflow; 20rem rail/detail. | Tablet master/detail mode cannot be a squeezed desktop shell. |
| Mushaf | No overflow; reader then study; document height about 2382px when selected. | Source says stacked. | No overflow; 40/60 split starts, selected study visible. | No overflow; stable 40/60. | Preserve protected page measure; design tab/source chrome for narrow widths. |
| Placeholder | No overflow; long empty page. | Source-only. | No overflow. | Same shared surface. | Align heading/body and define meaningful empty-page height. |

### Breakpoint and mode conclusions

- Current canonical Sass bands are phone `<=767`, tablet `<=1023`, desktop `>=1024`, and wide desktop `>=1440`. The exact 768px failures show that simply crossing from 767 to 768 cannot activate full desktop navigation/min-width behavior.
- 390, 768, 1024, and 1440 are evidence viewports, not proposed design breakpoints. Claude Design should show what structurally changes, then engineering can map it to tokens.
- Desktop Words is roughly a 5:4 table/detail split and widens at 1440. Access uses 20rem rail, Abwab 18rem, Templates 15.5rem. These values should be reviewed as one split-layout scale rather than copied into Golden comps as unrelated constants.
- Mushaf is genuinely different: 40/60 with a sticky measured Quran page at desktop and a 52rem below-desktop loading reservation derived from page geometry.
- Mobile navigation currently behaves as a full-screen overlay but lacks complete dialog/navigation containment. This is a shell-level blocker independent of feature designs.
- Phone action targets need a 44px hit-area contract even where the visible icon remains small. Pagination, Abwab tree/pickers, and Mushaf page controls are current risks.

---

## 14. Stable-geometry findings

| Area/transition | Current behavior | Stability assessment | Design requirement for Claude Design |
|---|---|---|---|
| Lazy route navigation | A 2px shell progress line waits 200ms, so warm navigations do not flash. | Stable and appropriately low-noise. | Preserve delayed shell feedback without moving page content. |
| Mushaf page loading to loaded | Content-shaped reservation follows the measured 15-line page; below desktop loading reserves 52rem. | Protected strong pattern. | Keep page canvas, line rhythm, and reader/study split stable; do not substitute a generic spinner. |
| Words list loading to loaded | Table bodies and replacement states reserve substantial height; detail panel remains mounted. | Generally stable. | Golden Table and Details should retain their shells through loading/empty/error. |
| Words selected detail changes | Inline panel/drawer shell persists while tab body changes. | Good family intent; body lengths still vary naturally. | Reserve headers/tabs/status, not arbitrary content height. One owned body scroller where needed. |
| Word Type scope counts | Idle/loading/success/error each preserve a metric layout using an invisible mirror. | Strong local solution. | Generalize the geometry principle, not necessarily the exact hidden implementation. |
| Abwab authoring dialog open/content changes | Fixed `min(92dvh,44rem)` shell and body-only scroll; shallow flows leave empty space. | Intentional zero-resize constraint. | Preserve stable shell height as a named authoring variant, while unifying padding/viewport behavior. |
| Global detail overlay adapters | Header/footer/shell stay fixed; adapter body owns status/content. | Stable. | Keep history controls and status slot fixed across adapters. |
| Access mutation status | Always-mounted state region leaves about 6.5rem blank when idle; other branches still resize. | Over-reserved and inconsistent. | Define a compact stable review/feedback slot that is visible when meaningful and does not create unexplained emptiness. |
| `qd-state` across features | Loading/empty/error/notice share one flexible block. | Semantics and dimensions vary unpredictably. | Separate state types and document which owns reservation versus natural growth. |
| Pagination jump focus | Input widens and Go button mounts, moving neighbors. | Direct avoidable shift. | Fixed input/action slots in every state. |
| Busy buttons | Feature-specific labels and local controls may change content width. | Source risk; mutation buttons were not exercised. | Reserve width or keep stable label plus progress indicator; never move destructive decisions during submission. |
| Dynamic counts and tabs | Counts can change from 0 to 6-7 digits; some tab rows wrap. | Potential width/height shift. | Use stable numeric measures or deliberate wrap per breakpoint. Do not disable/disappear actions without a consistent zero rule. |
| Filter/picker results | Popups and result messages mount conditionally; flip logic differs. | Anchor and page geometry can vary. | Floating layers must not alter document flow; reserve applied-filter summary if it changes toolbar height. |
| Footer health content | Status copy can wrap, so footer height is content-dependent. | Natural content growth, not a defect. | Do not invent a fixed footer-height token; keep page stability through shell layout rather than clipping health text. |

Stable geometry does not mean every panel has a fixed height. It means stable controls, anchors, shell regions, and scroll ownership while content that genuinely varies, especially Quran and commentary, can grow.

---

## 15. Overflow and long-content findings

| Content/surface | Current behavior | Classification | Full-value/access requirement |
|---|---|---|---|
| Quran page and ayah text | Protected layout and natural line/ayah growth. | `EXPAND`/protected wrap. | Never generic-truncate. Preserve exact Uthmani text, glyphs, ligatures, markers, and line logic. |
| Tafsir/translation/i'rab | Long body content grows within the study/document flow; mixed direction and sanitized HTML possible. | `EXPAND`, with owned panel/page scroll. | Readable line length for prose; source metadata remains visible; no nested horizontal scroll. |
| Explorer table cells | Ellipsis/truncation in constrained desktop cells. | `TRUNCATE`, partially intentional. | Complete value available to keyboard/touch/assistive tech, not pointer-only `title`. |
| Explorer mobile rows | Labels/values wrap into taller semantic cards. | `WRAP`/`EXPAND`. | Preserve label-value association and consistent rhythm; domain row heights may differ. |
| Root/Lemma/Stem/Unique detail titles | Can truncate in headers and pickers. | `TRUNCATE`. | Focus/touch disclosure and accessible full name. |
| Access name/email | Ellipsis; email forced LTR. | `TRUNCATE`, correct direction. | Full identity must be discoverable before any safety decision. |
| Abwab tree name | Name is the only shrinking item while depth indentation grows. | `ACCIDENTAL_OVERFLOW` risk. | Define depth budget, continuation/indent strategy, and full-name disclosure. |
| Abwab aliases/descriptions | Chips wrap; description grows in authoring form. | `WRAP`/`EXPAND`. | Long chip removal target and multiline form height remain usable. |
| Dashboard/build identifier | Long version/hash may extend the small app-info region. | `WRAP` risk. | Define code-like wrapping or copy/disclosure; do not squeeze navigation cards. |
| 768 document | Roots/Lemmas/Abwab/Access reach 961px; Word Types 866px. | `ACCIDENTAL_OVERFLOW`. | No page-level horizontal scroll at the tablet mode. Local data scrollers only where explicitly designed. |
| Desktop explorer table | Internal table scroll/clipping while split detail remains visible. | `SCROLL`, purposeful when owned. | Sticky/visible headers and selected-row discoverability; no nested competing horizontal scrollers. |
| Word Type detail/page | Local detail scroll rules can combine with global panel scroll. | Nested `SCROLL` risk. | Exactly one body scroller per panel. |
| Authoring dialogs/global overlay | Fixed viewport shell, body-only vertical scroll. | `SCROLL`, purposeful. | Header/footer always reachable; long validation text wraps; phone safe-area and keyboard considered. |
| Shared confirm | No equivalent max-height/body-scroller rule. | `ACCIDENTAL_OVERFLOW` risk. | Apply canonical viewport and one-scroller behavior. |
| Source/association/surah pickers | Labels truncate; popup heights and flips differ. | `TRUNCATE` plus floating `SCROLL`. | Full selected label, stable max height, viewport collision, keyboard scroll-into-view. |

No generic overflow rule should be applied to Quran text. Non-Quran UI labels can wrap, truncate with disclosure, scroll inside a named owner, or expand according to the family contract.

---

## 16. Grid and density findings

| Grid/surface | Current rule | Observed result | Constraint Claude Design must decide |
|---|---|---|---|
| Dashboard navigation | `repeat(auto-fill,minmax(250px,1fr))`. | 1440 produced four cards plus one orphan on the next row and large unused space. | Deliberate 1/2/3/max-column behavior and orphan handling. Underlying destination set differs from the Words curriculum, but arbitrary auto-fill should not remain. |
| Words hub | One column, then a deliberate two-column rule at 640 and again at 1024. | 2+2+1 preserves teaching sequence; wide canvas remains underused. | Preserve sequence and last-card treatment using canonical bands/measure. |
| Abwab cards | `repeat(auto-fill,minmax(13rem,1fr))`, no maximum. | Very wide screens can create excessive columns and scanning cost. | Maximum useful columns, stable card width, and breadcrumb/rail relationship. |
| Access permission groups | `auto-fit minmax(13rem,1fr)`. | Functional responsive grouping, but group density can vary widely. | Group min/max measure and maximum columns based on 5 real groups. |
| Word Type main categories | Items use about a 16rem basis. | At 1024 categories wrap awkwardly and push the table far down. | Dedicated desktop/tablet/phone taxonomy layout with a stable selected summary. |
| Word Type child chips | `auto-fit minmax(10rem,1fr)`. | Count/label length drives irregular rows. | Deliberate chip/list density, long-label wrap, and many-child scrolling/disclosure. |
| Access audit events | Multi-region filtering and event cards. | Desktop has usable breadth; mobile becomes a long sequence. | Define filter collapse/order and event-card measure, not a table conversion. |
| Detail split | Words 5:4, Access fixed rail, Abwab fixed rail, Templates fixed rail. | Related workspace patterns use unrelated constants. | One split-layout scale with named rail/detail variants; do not force Mushaf's 40/60 into it. |

---

## 17. Page-width and gutter findings

Current global layout facts:

- Sticky navbar is 3.5rem.
- `.qd-container` has a 72rem cap and 16px inline padding.
- `.qd-page-frame` is uncapped with 16px inline padding.
- `.qd-page` adds 24px block and 16px inline padding.
- At 420px, `.qd-page` becomes 12px block/8px inline and `.qd-container` 8px inline.
- Global `scrollbar-gutter: stable`, local stable-scroll utility, and body-only modal scrollers are useful foundations.

| Surface | Intended classification | Current width/gutter ownership | Finding |
|---|---|---|---|
| Dashboard | `CAPPED_READING_CONTENT`/navigation | Page plus capped content. | Cap supports scan length, but card grid density is undeliberate. |
| Words hub | `CAPPED_READING_CONTENT` | Page plus nested capped container. | Teaching content can be capped; gutter ownership should still be explicit. |
| Words explorers | `SPLIT_WORKSPACE`/`FULL_DATA_WORKSPACE` | Nested page/frame plus explorer responsive gutter. | Data space is lost twice; table/detail should own the available width. |
| Word Types | `SPLIT_WORKSPACE` | Frame closes before main layout. | Same family begins on a different horizontal axis. |
| Access | `SPLIT_WORKSPACE` | One element combines container/frame; frame cancels cap. | Visual result is one gutter, but class intent is contradictory. |
| Abwab live/cards/archive | `SPLIT_WORKSPACE` | Outer page plus inner container/frame. | Double gutters reduce hierarchy workspace. |
| Templates | `SPLIT_WORKSPACE` | Same nested composition as Abwab. | Double gutter plus fixed rail. |
| Mushaf | `OTHER_SPECIALIZED_LAYOUT`/protected | Feature-owned 40/60 reader/study shell. | Preserve Quran measure and sticky page; unify only outer page axis/chrome. |
| Placeholder routes | `CAPPED_READING_CONTENT` | Heading outside container; body inside. | Misaligned axes and excessive empty page. |

Golden page shells should name purpose, not expose arbitrary class combinations: Full Data Workspace, Capped Reading/Navigation Content, Split Workspace, and Protected Mushaf.

---

## 18. Design-problem catalog

| Category | Evidence/examples | Design question to solve |
|---|---|---|
| `VISUAL_INCONSISTENCY` | Input/select geometry, manual tab styles, danger-menu override, hover surfaces, local Abwab fields. | What is the one visual/interaction vocabulary for controls, tabs, menus, and state? |
| `RESPONSIVE_FAILURE` | 768 widths of 961/866; mobile nav containment; Word Type tablet control stack. | What are the true phone/tablet/desktop structural modes, especially at 768? |
| `LAYOUT_SHIFT` | Pagination input/button changes; dynamic tab/count wrapping; uneven async reservations. | Which controls and shell regions reserve geometry, and which content grows naturally? |
| `OVERFLOW` | Tablet document scroll, deep Abwab indentation, confirm viewport risk, nested Word Type scroller. | Who owns scrolling and how is long content disclosed? |
| `EXCESSIVE_GUTTER` | Words/Abwab/Templates nested page/frame; extra explorer gutter. | Which single element owns each page's inline spacing? |
| `UNBOUNDED_GRID` | Dashboard and Abwab cards. | What maximum columns and item measures serve actual content counts? |
| `DUPLICATED_UI_CONTRACT` | Five table shells, 18 manual tablist template occurrences, many feature-local Dashboard/Mushaf/Words selectors, and local form/control copies. | Which contracts become shared behavior, semantic classes, or specialized compositions? |
| `ACCESSIBILITY_DISCLOSURE` | Pointer-only `title`, small tree/pagination targets, missing mobile nav focus containment, inconsistent ARIA row/list/tab semantics. | How do keyboard, touch, screen-reader, and long-label users discover the same information? |
| `DENSITY_PROBLEM` | Empty desktop detail regions, very tall mobile Word Type/filter flow, variable rails, cramped tablet. | What density modes support long research sessions without wasting or crushing space? |
| `SPECIALIZED_VALID_DIFFERENCE` | Access lifecycle, Abwab hierarchy/search/pickers, overlay history, entity-specific filters/tabs. | How do specialized regions compose from the same primitives without becoming giant universal components? |
| `QURAN_PROTECTED_DIFFERENCE` | Mushaf page, Uthmani text, ayah cards, commentary, similarity/Mutashabihat. | What chrome can unify while fonts, text, geometry, and semantics remain protected? |

---

## 19. Screenshot index and temporary path

Temporary root: `/tmp/quran-ui-design-handoff.L6BYle/current-ui/`

All 44 listed images were opened and visually inspected before acceptance. The missing numeric `37` was a rejected/failed Surah-picker capture and is not evidence. Screenshots remain outside the repository.

### App, Words, and tables

| File | Viewport/state evidenced |
|---|---|
| `shared-patterns/03-dashboard-desktop-1440.png` | Dashboard desktop grid and unused width. |
| `responsive/04-dashboard-mobile-390.png` | Dashboard phone stack and long app-info value. |
| `words/05-words-hub-desktop-1440.png` | Words hub 2+2+1 progression. |
| `responsive/06-words-hub-mobile-390.png` | Words hub stacked flow/chip wrapping. |
| `tables/07-roots-table-desktop-1440.png` | Roots split table/detail. |
| `responsive/08-roots-table-mobile-390.png` | Roots semantic mobile rows. |
| `responsive/09-roots-table-tablet-768.png` | Roots 768 document overflow. |
| `responsive/10-roots-table-desktop-1024.png` | Breakpoint-edge split and clipped columns. |
| `tables/11-lemmas-table-desktop-1440.png` | Lemma table/detail/filter. |
| `responsive/12-lemmas-table-mobile-390.png` | Lemma phone rows and tall filter region. |
| `responsive/13-lemmas-table-tablet-768.png` | Lemma 768 document overflow. |
| `tables/14-stems-table-desktop-1440.png` | Stem table/detail family. |
| `tables/15-unique-words-desktop-1440.png` | Vocalized Unique table/detail. |
| `tables/16-word-types-desktop-1440.png` | Word Type taxonomy/table/detail. |
| `tables/38-unique-words-simple-desktop-1440.png` | Simple Unique mode. |
| `responsive/39-word-types-mobile-390.png` | Word Type phone control/vertical density. |
| `responsive/40-word-types-tablet-768.png` | Word Type 768 overflow. |
| `responsive/41-word-types-desktop-1024.png` | Word Type breakpoint-edge wrapping. |

### Mushaf and details

| File | Viewport/state evidenced |
|---|---|
| `mushaf/01-mushaf-page-5-desktop-1440.png` | Page 5 protected 40/60 desktop shell, no selection. |
| `mushaf/02-mushaf-page-5-mobile-390.png` | Reader-first phone shell. |
| `mushaf/32-mushaf-selected-word-ayah-desktop-1440.png` | Real selected word/ayah study at page 5. |
| `responsive/33-mushaf-selected-word-1024.png` | Mushaf split at desktop edge. |
| `responsive/34-mushaf-selected-word-mobile-390.png` | Selected reader/study stacked phone document. |
| `mushaf/35-mushaf-selected-study-mobile-390.png` | Study tabs/source/content phone behavior. |
| `mushaf/36-mushaf-before-surah-picker-1440.png` | Header/navigation immediately before picker attempt. |
| `details/27-roots-selected-detail-desktop-1440.png` | Real selected Root detail inline. |
| `details/28-roots-mobile-before-detail-390.png` | Mobile base before opening detail. |
| `details/29-roots-mobile-detail-drawer-390.png` | Root near-full mobile drawer. |
| `modals/30-global-detail-overlay-over-drawer-mobile-390.png` | Global overlay layered over feature drawer. |
| `modals/31-global-detail-overlay-desktop-1440.png` | Global entity overlay desktop geometry. |

### Abwab, Access, dialogs, and placeholders

| File | Viewport/state evidenced |
|---|---|
| `abwab/17-abwab-tree-desktop-1440.png` | Live tree, controls, rail, empty workspace breadth. |
| `responsive/18-abwab-tree-mobile-390.png` | Stacked phone hierarchy. |
| `responsive/19-abwab-tree-tablet-768.png` | 768 Abwab overflow. |
| `abwab/20-abwab-templates-desktop-1440.png` | Template list/editor composition. |
| `modals/21-abwab-add-root-dialog-1440.png` | Safely opened Add-root authoring shell; not submitted. |
| `access/22-access-workspace-desktop-1440-redacted.png` | Owner workspace with PII masked; live loading/ready boundary. |
| `responsive/23-access-workspace-mobile-390-redacted.png` | Owner workspace phone stack, PII masked. |
| `responsive/24-access-workspace-tablet-768-redacted.png` | Owner workspace 768 overflow, PII masked. |
| `access/25-access-security-desktop-1440-redacted.png` | Security/relink/reconciliation resting UI, PII masked. |
| `access/26-access-audit-desktop-1440-redacted.png` | Audit filters/events, PII masked. |
| `abwab/42-abwab-cards-desktop-1440.png` | Card mode and unbounded grid evidence. |
| `abwab/43-abwab-archive-desktop-1440.png` | Current empty archive mode. |
| `shared-patterns/44-placeholder-desktop-1024.png` | Placeholder header/body axis and empty page. |
| `shared-patterns/45-placeholder-mobile-390.png` | Placeholder phone behavior. |

---

## 20. Complete Claude Design Golden-family request list

Claude Design should design these as a coordinated system, not as 20 isolated visual exercises. Every family needs at least representative 390, 768, 1024, and 1440 states when its structure changes. The requested output is a visual and interaction contract, not Angular code.

| Golden family | Real consumers and data | Required states | Responsive requirements | Preserve | Do not preserve |
|---|---|---|---|---|---|
| F01 App chrome | Every route; route label/current state, auth/account actions, settings, nav progress, and footer health. Dashboard app-info version/environment is separate feature data. | Auth loading, authenticated, anonymous, current route, menu open, lazy loading, health wrap. | Accessible desktop/tablet/phone navigation; 768 cannot expose a too-wide desktop shell; focus/scroll containment. | Auth-specific actions and content-height footer. | Duplicate link trees, hover token drift, inaccessible full-screen mobile overlay. |
| F02 Page shell | All routed page surfaces; the cross-route detail overlay composes over an existing base page and primarily belongs to F14. | Full Data, Capped Reading/Navigation, Split Workspace, Protected Mushaf, placeholder. | One gutter owner; named width/max rules at all four viewports; stable scrollbar behavior. | Purpose-specific width variants. | Arbitrary nested `.qd-page/.qd-container/.qd-page-frame` combinations. |
| F03 Page/section header | Dashboard, Words, Abwab, Access, placeholders, dialog sections. Data: title, description, optional eyebrow/count/actions. | No action, one primary action, multiple safety actions, long Arabic title, loading count. | Natural action wrap/order; shared axis with body; no title-only disclosure. | Quran/context typography slot. | Per-feature margins, misaligned placeholder header, arbitrary truncation. |
| F04 Surface/card | Navigation, permission groups, audit events, study/source cards, Abwab cards, quiet review cards. | Rest/hover/focus/selected/disabled/loading/error; short/long/empty content. | Deliberate grid measure and stacking; no nested-card habit. | Safety warning and Quran content slots. | Shadows/lifts, accent generic hover, unbounded identical grids. |
| F05 Button/action | All safe/destructive/primary/secondary/icon/toolbar actions. Data: label, icon, optional count, busy/error. | Rest, hover, focus-visible, active, disabled, busy, danger, icon-only. | 44px phone hit area; stable label width; action groups wrap intentionally. | One primary action per view and explicit destructive semantics. | Active translate, 20-32px hit targets, local visual copies. |
| F06 Form/control | Access, Abwab authoring, filters, page/order editors, source pickers. Data: Arabic labels, LTR codes/email, long hints/errors. | Empty/filled/required/invalid/disabled/read-only/busy, textarea, checkbox, select, inline compact edit. | Full-width phone fields, stable label/error geometry, software-keyboard-safe dialogs. | Direction overrides and compact order editor. | Input/select geometry drift, local Abwab styling, focus-width changes. |
| F07 Tabs/segmented | Section tabs, explorer views/subviews, selected ayah, Word Type table modes, Access tabs. | 2/3/4/5 tabs, long labels, counts, zero count, loading/error panel, keyboard focus. | Roving focus and logical RTL arrows; designed wrap/scroll/segmented mode by count. | Domain labels/counts and route/query semantics. | Mixed button/tab styles, 3-tab forced into 2 columns, missing keyboard behavior. |
| F08 Search/filter toolbar | All explorer filters, Access list/audit filters, Abwab search/view controls. | Draft/applied, active count, 0/1/many options, open popup, no-match, loading/error, clear. | Deliberate control order and collapse/wrap at 390/768; stable toolbar height where possible. | Entity-specific fields and Abwab mode-specific search meaning. | Arbitrary local gaps/breakpoints, excessive empty filter height, popup behavior drift. |
| F09 Golden Table | Roots, Lemmas, Stems, Unique, Word Type word/grouped. Real DTO columns and 0-to-7-digit counts. | Initial loading, refresh, ready, empty, no-match, error, selected, sort both directions, long/null values, first/middle/last page. | Desktop/internal scroll, tablet without page overflow, semantic mobile cards with domain row slots. | Columns, row information density, default sort, grouped rows. | Five shell implementations, ARIA drift, physical right edge, squeezed desktop at 768. |
| F10 Detail/result list | Words/related entities/surahs/type distributions/group members and ayah matches. | Loading/ready/empty/error, link/display-only, count zero/many, pagination. | Row/card selection based on semantic content; Quran card grows; non-Quran values disclose fully. | Quran ayah card and display-only distributions. | Inconsistent list roles, locally different row chrome, ambiguous zero actions. |
| F11 Details workspace | Five explorer details, Access user detail, Abwab side panel, Mushaf study. | No selection, selected, loading, empty, error, notFound, each tab/subtab, action/no action. | Inline desktop, intentional tablet composition, phone drawer only where appropriate, single body scroller. | Domain sections, safety workflow, protected study. | Independent headers/tabs/state geometry and unexplained blank panels. |
| F12 Async/feedback | 53 `qd-state` call sites plus table/panel/Quran skeletons and action notices. | Initial loading, refreshing, empty, filtered no-result, error/retry, notice, success, conflict, auth failure. | Preserve final geometry according to family; concise phone messages and reachable Retry. | Content-shaped Quran/table skeletons and local write errors. | One generic state for every meaning, spinner/text inconsistency, invisible permanent blank bands. |
| F13 Pagination/result count | Explorer main/detail lists, Access list, audit Load more. Data: total/page/pageSize/validation. | Zero/first/middle/last, huge total, jump idle/focus/invalid/submitting, disabled previous/next. | 44px targets, stable fixed width, no duplicate IDs, predictable wrapped layout. | Load more as a separate capability. | Widen-on-focus, conditionally mounted Go, undersized controls. |
| F14 Modal/drawer shell | Confirm, six Abwab authoring dialogs, five Words drawers, global overlay. | Base/wide, short/long, loading, validation, server error, dirty, busy, destructive, nested alert. | One viewport/safe-area/focus/scroll contract; named widths; phone header/footer always reachable. | Specialized workflow bodies and history overlay. | Unrelated padding/height systems and unbounded confirm. |
| F15 Floating layer/menu | Nav/account menus, Abwab context/action menus, association/source/surah pickers, retained Restore. | Closed/open, keyboard entry, selected, loading, empty/no-match/error, danger item. | Viewport flip, max height, scroll-into-view, focus return, touch full-value discovery. | Searchable versus action-only contents. | Different key sets/collision logic, hover-only revelation, local danger hover. |
| F16 Tree/hierarchical picker | Live/archive Abwab, templates, move, door picker, Surah grouped list. | Expanded/collapsed/selected/focus/deep/many children/search/no-match/loading/error/excluded/disabled/bulk. | RTL logical depth, controlled indentation, 44px targets, phone/tablet hierarchy context. | Live tree, archive read-only, template list, destination and set-selection semantics. | Tiny targets, uncapped indentation, generic demo-tree data. |
| F17 Chip/badge/status/count | Access Owner/lifecycle, permission counts, Words counts/types, Abwab order/relation/alias. | Zero/one/large, selected/removable/disabled, warning/success/danger, long text. | Wrap with readable order and large remove target; no count-induced layout jump. | Lifecycle/morphology/alias semantic variants. | Decorative badge proliferation and inconsistent zero interaction; do not introduce color-only meaning. |
| F18 Quran/Ayah/study | Mushaf page/word/ayah, ayah matches, Tafsir/translation/i'rab, similar/Mutashabihat. Data only from API. | Page/word/ayah loading, first/last, no selection, null morphology, 0/1/many sources, long commentary, empty/error related results. | Protected measured desktop page and natural phone growth; mixed-direction sources; no Quran truncation. | Exact fonts/glyphs/text, Quran geometry, similarity/Mutashabihat semantics. | Any invented Quran copy, generic table/card compression, text animation. |
| F19 Access workspace | Owner route, user list/detail, permissions/lifecycle, audit, relink, reconciliation. | All lifecycle/Owner combinations, list/detail/catalogue/audit/relink/reconciliation states, dirty review, conflict/auth outcomes. | Master/detail desktop; designed tablet; phone selected-context navigation and long LTR identity. | Owner guard, staged review, inline decisions, safety distinctions. | Generic CRUD admin table, role invention, blank mutation band, direct-edit without review. |
| F20 Abwab workspace/authoring | Tree/cards/archive/templates/side rail, relations and six dialogs. Real hierarchy and permissions. | Live/archive/retired, expanded/deep/search/bulk, permission-hidden/visible-disabled, modal validation/dirty/busy/conflict, template states. | Useful RTL tree at all widths, deliberate cards, split/stack context, safe fixed dialogs. | Mode search semantics, move/door picker distinction, archive rules, template copy semantics. | Generic file tree, invented protected state, double gutters, tiny actions, unbounded cards, local form vocabulary. |

### Expected Golden Design package structure

For each family, Claude Design should return:

1. one canonical anatomy diagram or annotated state frame;
2. realistic desktop, tablet, and phone compositions using the data fixtures in sections 5-6;
3. state coverage, including loading, empty, error, long/null data, and focus/selected behavior;
4. explicit optional zones/capabilities rather than boolean-heavy universal components;
5. a preserve/remove note tied to G and D ledger IDs;
6. long-content and scroll ownership rules;
7. accessibility intent for keyboard, focus, target size, labels, state announcement, and RTL order;
8. no implementation code and no changes to Plan 7 during this design phase.

---

## 21. Genuine domain differences that must remain

This ledger is authoritative for the reported genuine-difference count.

| ID | Difference to preserve | Why the domain requires it |
|---|---|---|
| G01 | Arabic-first RTL layout and direction-aware order. | Core users and product content are Arabic; this is not mirrored localization. |
| G02 | Protected Quran text, fonts, glyphs, ligatures, markers, full-text growth, and no animation. | Accuracy and reverence override generic UI normalization. |
| G03 | Mushaf's measured 40/60 reader/study shell and page-shaped loading reservations. | A Quran page has stable physical reading geometry unlike admin data. |
| G04 | Named Full Data, Capped Reading/Navigation, Split Workspace, and Protected Mushaf page intents. | Different work types need different width behavior; arbitrary per-page gutters do not. |
| G05 | Dashboard destination overview versus Words hub's ordered teaching progression. | The information architecture differs, even though both use card-like navigation. |
| G06 | Unique Words simple and vocalized identity modes. | They represent different linguistic identities and routes. |
| G07 | Roots 5 detail views; Lemmas/Stems 4; Unique 3; Word Type word 2 and grouped 3. | Relationships genuinely differ; the shell should still unify. |
| G08 | Entity-specific filters: Lemma root, Stem root+lemma, Unique type+root, Word Type morphology presence and secondary filters. | Available relationships and queries differ. |
| G09 | Entity-specific mobile row information and resulting minimum heights. | Root, Lemma, Stem, Unique, and grouped rows carry different fields. |
| G10 | Word Types default occurrence ordering, taxonomy filters, and display-only grouped member rows. | Morphology browsing has a distinct task contract. |
| G11 | Quran ayah/result cards versus ordinary data rows. | Full exact scripture and highlights cannot become compact table cells. |
| G12 | Language-first Tafsir/Translation source hierarchy versus flat Full i'rab selection. | Source catalog structures genuinely differ. |
| G13 | Similar Ayahs versus grouped verbal Mutashabihat. | They model different Quranic relationships and data shapes. |
| G14 | Global Word Type overlay supports word identity only; grouped root/stem/lemma details remain local. | Current URL/frame grammar deliberately excludes grouped identities. |
| G15 | Global detail Back, closed retained Restore, base-route preservation, and 8-frame cap. | These are intentional navigation/history states, not modal decoration. |
| G16 | Access is Owner-only while Abwab reads are public and writes permission-gated. | Authorization semantics differ materially. |
| G17 | Access Pending/Active/Disabled and independent Owner membership. | Lifecycle and Owner bypass are not a generic role/status palette. |
| G18 | Access inline staged review versus Abwab modal authoring/confirmation. | Permission decisions need target/diff context; hierarchy authoring needs contained forms/pickers. |
| G19 | Abwab search behavior differs across live tree, cards, archive, and pickers. | Each mode must retain truthful hierarchy/drill-down semantics. |
| G20 | Live/archive ARIA trees versus template `role=list` hierarchy. | Only the live/archive surfaces implement full tree keyboard behavior. |
| G21 | Move destination picker versus checkbox/radio door set picker. | Destination/cycle/root semantics differ from selecting targets. |
| G22 | Archive Restore can remain visibly disabled when unavailable. | It is an intentional capability-disclosure exception; most other missing writes are hidden. |
| G23 | Abwab cards intentionally have no context menu. | Card mode is a simpler drill-down/selection representation, not a second tree. |
| G24 | Template apply copies detached direct children and never copies the template root. | This product rule must be clear in preview/confirmation and cannot be redesigned away. |

Other valid constraints that are not counted as separate G-ledger items: relation direction/grouping, root creation requiring a live section in the relevant mode, fixed-height authoring-dialog stability, natural footer height, and the currently deferred dark-theme navy/gold reconciliation.

---

## 22. Accidental differences that should disappear

Section 4 records 50 current drift findings. Forty-eight are direct removal targets. D36 and D38 are decision-gated inconsistencies: their current mismatch must be resolved, but the correct resolution might remove, disable, implement, or retire behavior after an explicit product decision. The direct removal groups are:

- **Page/shell/responsive drift:** D01-D13.
- **Visual token, motion, form, and local-control drift:** D14-D22 and D50.
- **Table, detail, tab, picker, disclosure, and inert-affordance drift:** D23-D35 and D37.
- **Async-state, pagination, and action-target drift:** D39-D47.
- **Modal geometry and padding/overflow drift:** D48-D49.

Two entries require an explicit product decision before visual design:

- D36: whether a zero-count Word Type detail control should open an empty detail or be disabled.
- D38: whether serialized Mushaf `panel`, `wordTab`, and `segment` state should gain visible behavior or be retired. Claude Design must not invent that behavior.

Resolution does not mean one monolithic component. It means one recognizable contract, shared semantics, and deliberate optional regions across independent feature compositions.

---

## 23. Unknown, unreachable, or source-only states

### Browser/session limit

The Owner-only Access and Abwab live surfaces were captured first. Later in the audit, the existing application session naturally reset after silent-renew validation failed. Console evidence included an incorrect-nonce warning followed by token-validation reset and `silent renew failed`. The audit did not log out, clear storage, restart Chrome, replace the profile, or authenticate again. Later Owner-only variations therefore remained source-only. No tokens or personal identifiers were recorded.

Network inspection remained available and showed normal local application resource/API activity (primarily 200/304 responses). Console inspection otherwise showed expected local Vite/Angular development and HMR messages. This is not a production reliability audit.

### Not safely exercised because server-write controls were not pressed

- Access Accept, Disable, Reactivate, permission Confirm, and relink Preview/Confirm. Relink Preview is a POST.
- Abwab Save, Add, Delete, Archive, door Restore, Move, Apply, relation/template writes, and the Confirm buttons that dispatch those server writes.
- Enter-based Abwab order edits, new-template creation, and template quick-add.
- Mutation busy/success/validation/server-error/conflict responses, including 400/401/403/409 and all-or-nothing bulk conflict.

Opening confirmations, changing a picker selection locally, using retained-overlay Restore, and choosing a dirty-discard control are client-only inspection states and were safe. They are distinct from pressing the server-write action inside a confirmation.

### Conditional states inspected only from source

- Access: denied/non-Owner route, every lifecycle+Owner combination, null-name row, catalogue failure/unready/empty, assignment disabled, dirty draft/review/switch/route leave, audit empty/error/picker extremes, relink preview, reconciliation extremes.
- Abwab: populated archive, root/child Restore permutations, retired section, no-section create blocker, permission-hidden/visible-disabled combinations, extreme depth/siblings/children, move-cycle exclusions, relation groups, template empty/copy/apply results, dirty alerts.
- Words: invalid/deleted deep-link `notFound`, initial and background failures for every explorer, overlay depth 2-8/cap rejection, closed retained Restore, all tab/subtab combinations, zero-count Word Type decision.
- Mushaf: first/last page extremes, page error/empty, null morphology combinations, 0/1/many source catalog extremes, all similar/Mutashabihat state combinations, and visually inert serialized state.
- Global shell: unauthorized callback/error variations and full mobile-navigation keyboard/screen-reader traversal.

### Capture gaps and explicit evidence limits

- The Surah picker source/DOM was inspected, but its screenshot attempt timed out and was rejected; no `37` image exists.
- Dark mode was not visually audited in this run. Source confirms it remains the older navy/gold palette and is intentionally deferred for later reconciliation.
- 320px, short-landscape, 200% zoom, forced-colors, reduced-motion visual comparison, and full keyboard/screen-reader passes were not run. These remain verification work after Golden designs exist.
- Hover, focus, and motion timing cannot be conclusively judged from still screenshots alone. Source evidence is included, but interactive QA remains required.
- The current empty archive does not demonstrate realistic archived hierarchy density.
- No protected/locked Abwab door state was unreachable: it does not exist in the current model. It must not be added to mockups as if current.

---

## 24. Explicit non-design decisions and things Claude Design must not change

1. **Do not implement.** This handoff does not authorize Angular, Tailwind, SCSS, token, route, test, backend, database, or data changes.
2. **Do not revise or approve Plan 7.** `07-frontend-ui-architecture-v2.md` supplied engineering context only. Its visual choices are not pre-approved.
3. **Do not change product data.** Production Quran, Abwab, Access, user, permission, audit, template, relation, and source records must not be mutated or presented as fixtures. Sanitized synthetic values are allowed only for existing non-Quran schemas when they are contract-shaped and explicitly labelled. Fields, semantics, and behaviors must not be invented; Quran text must never be invented.
4. **Do not invent Quran text.** Golden comps must use verified current API/session text or an explicit `[actual Quran text from API]` placeholder.
5. **Do not alter Quran rendering.** Existing Mushaf/Quran fonts, glyphs, ligatures, markers, measured page behavior, and no-animation rule are protected.
6. **Do not redesign authentication or authorization.** Owner guard, lifecycle semantics, direct permission catalogue, visible/hidden capability rules, and conflict/no-retry behavior are product/security contracts.
7. **Do not turn Access into generic role CRUD.** There are three lifecycle states plus independent Owner, not arbitrary roles or group grants.
8. **Do not flatten Abwab semantics.** Preserve mode-specific search, hierarchy roles, archive rules, destination versus set pickers, relation direction, and template-copy behavior.
9. **Do not force every list into Golden Table.** Access users/audit, Abwab trees/templates, and Quran results have different semantics.
10. **Do not create a giant universal component.** Canonical families should use focused bases and optional regions/adapters, not dozens of booleans.
11. **Do not assume fixed-height content everywhere.** Stabilize shells and controls while Quran, commentary, error text, and meaningful lists grow naturally.
12. **Do not use green decoratively.** Preserve the allowed-green list and the 2px green-thread meaning for current/selected state.
13. **Do not add gradients outside the fixed multi-door Mushaf word and ayah-marker exception, glass, resting shadows, hover lifts, decorative religious imagery, gamification, or generic SaaS styling.** The current flat parchment/ink/green identity remains the context.
14. **Do not resolve dark theme in this pack.** Its navy/gold state is a known deferred difference; Golden light-system work must not silently change it.
15. **Do not infer new Approve/Reject modal families.** They are not general current UI contracts. Design only evidence-backed workflows.
16. **Do not treat screenshots as doctrine.** They document current behavior and problems; the Golden family designs will become the visual decision input only after review.
17. **Do not commit or publish screenshot artifacts.** They remain temporary at the reported `/tmp` path unless separately authorized.
18. **Do not infer full accessibility compliance.** Golden designs must state intent; browser/keyboard/screen-reader/zoom verification still follows implementation.

This artifact is complete when Claude Design can identify every current surface, choose the right canonical family, populate it with real-shaped data and states, preserve G01-G24, remove the 48 direct drift targets, resolve D36 and D38 through explicit product decisions, and avoid the non-decisions above without guessing.
