# Golden UI Component Catalog — Quran Dashboard

> Companion to `GOLDEN_UI_SYSTEM.md`. Twenty canonical families, each with the eighteen required fields. Family IDs match `UI_DESIGN_HANDOFF.md` §3/§20. `D##` = drift removed (handoff §4). `G##` = genuine difference preserved (handoff §21). Values marked `→ NEW` replace a current inconsistent value.
>
> **Revision 2:** D36 is locked to **disabled + visible reason**; D37 is locked **non-interactive**; typography names roles, not faces (implementation uses the current approved project fonts — `GOLDEN_UI_SYSTEM.md` §3.1); status tokens use the semantic aliases in §2.4 (`lifecycle-active` ≠ `mutation-success`); truncation follows the §8.1 disclosure ladder; one intentional nested **group surface** is permitted (§5.4); Access lifecycle frames are separate and never mixed.
>
> Structural principle for every family: **one focused base + named variants + optional zones**. No family is a single component with dozens of booleans; specialised regions compose from the same primitives.

Field key: **1** Purpose · **2** Consumers · **3** Anatomy · **4** Visual language · **5** Interaction language · **6** Variants · **7** Optional zones · **8** Drift removed · **9** Differences preserved · **10** Responsive (Wide / Medium / Compact) · **11** States · **12** Geometry · **13** Spacing · **14** Typography · **15** Overflow · **16** A11y & RTL · **17** Fixtures · **18** Consumed by.

---

## F01 — App Chrome / Shell

1. The persistent frame: route progress, navigation, account/settings, footer health.
2. Every route (screenshots `03`, `07`, `17`, `22`, `32`).
3. `[2px route-progress line] · [navbar: wordmark · primary links · overflow "المزيد" · theme toggle · settings menu · auth action] · [page slot] · [footer: product line · description · health indicators]`.
4. Navbar `--qd-bg-chrome`, block-end hairline, 3.5rem, sticky. Current route = green tint pill + `--qd-green-text` (never solid green). Hover = `--qd-surface-quiet` (D12). Footer = `--qd-footer-bg` with `--qd-ink-on-dark`, health dots + text label.
5. One link tree rendered once and re-composed per mode (D-duplicate markup). Menus follow F15. Sheet navigation: trap + scroll lock + inert background + visible "إغلاق" + focus return (D13).
6. `authenticated` / `anonymous` (auth action swaps only), `route-loading`.
7. Optional: environment chip (Development), overflow link group, health region.
8. D12 hover token drift · D13 inaccessible mobile overlay · duplicate desktop/mobile link markup · 768 desktop-shell exposure (§4.1).
9. Auth-specific actions; content-height footer (health copy wraps — never a fixed footer height).
10. **Wide** full link row + menus. **Medium** sheet navigation (`→ NEW`, was desktop row) + compact wordmark. **Compact** sheet navigation, 48px rows.
11. auth-initialising (wordmark + skeleton actions) · authenticated · anonymous · current route · menu open · route loading (2px delayed line, 200ms) · health wrap · offline/unknown health = neutral badge.
12. Navbar 3.5rem; links `ctl-md`; sheet rows 48; footer padding-block 24.
13. Navbar padding-inline = page gutter; link gap 4; footer stack gap 6.
14. Wordmark Naskh 20; links `t-body`; footer `t-meta`.
15. Link labels never truncate — the overflow menu absorbs surplus links. Health copy wraps.
16. `<nav aria-label>`, `aria-current="page"`, skip-link to page slot, sheet = `role="dialog" aria-modal`, RTL logical order (wordmark at inline-start).
17. "المنهج القرآني", links لوحة التحكم / قارئ المصحف / الكلمات والجذور / التفاسير / الأبواب / المصادر / المزيد; health "الحالة: سليم · قاعدة البيانات: سليم"; env "Development".
18. All routes.

---

## F02 — Page Shell / Workspace Layout

1. Owns page width, the single inline gutter, and block rhythm.
2. All routed surfaces (the cross-route overlay composes over a base page and belongs to F14).
3. `[gutter owner] → [page header slot] → [toolbar slot] → [content slot(s)] → [pagination slot]`.
4. Invisible by design: no background, no border, no padding beyond the gutter and block rhythm.
5. None (layout only). Scroll: page owns vertical scroll; horizontal page scroll is a defect.
6. `capped-reading` 72rem · `full-data` 100rem · `split-workspace` 100rem + rail scale · `protected-mushaf` feature-owned (G03/G04) · `placeholder` (header and body on one axis — D04).
7. Optional sticky sub-header (Abwab retained-modal strip, Access tabs), optional rail (start/end), optional bottom action bar (Medium).
8. D01 nested gutters · D02 contradictory width classes · D03 Access bypassing page rhythm · D04 placeholder misalignment · D05 Word Types different axis · D06 duplicate explorer gutter · D07/D08 unbounded grids (delegated to F04).
9. G04 four named width intents; Mushaf excluded from the rail scale.
10. **Wide** gutter 32 (40 ≥1440), split available. **Medium** gutter 24, single column, no split. **Compact** gutter 16.
11. Content loading below a stable header · placeholder/empty page (min 40vh, centred single message + one action) · route error/denied (page-level error block).
12. Rails: 16/18/20rem (§3.5); split `1.25fr/1fr`; `scrollbar-gutter: stable` retained.
13. Page header → first block 24; block → block 24 (Compact 16).
14. Inherited.
15. Page never scrolls horizontally; local scrollers are declared by the child family.
16. One `<main>` per page, `aria-labelledby` the page title; landmark order = header → toolbar → content → pagination.
17. Dashboard = capped-reading; Roots/Lemmas/Stems/Unique/Types/Access/Abwab/Templates = split-workspace; Mushaf = protected; /tafsirs etc = placeholder.
18. F03, F08, F09, F11, F19, F20, F18.

---

## F03 — Page & Section Header

1. Identifies the surface and hosts its actions.
2. Dashboard, Words hub, explorers, Abwab, Templates, Access, placeholders, card/panel/dialog sections.
3. `[eyebrow (step numeral + label)] [title] [description] [meta/count row] [action group]`.
4. Title Naskh `t-h1` (`t-h2` for sections); eyebrow `--qd-green-text` `t-caption`; description `--qd-ink-body` `t-body-lg` ≤72ch; hairline under the header only where the current design already uses one (explorer explainer card).
5. Actions right-aligned in logical inline-end order: primary (max one, solid green) → secondary → tertiary/text. Wrap to a second row before shrinking.
6. `page` · `section` · `panel` · `dialog-section` · `explainer` (explorer teaching card: eyebrow + short definition + "عرض الشرح" disclosure).
7. Optional: count/result metadata, status badge, "back" affordance (Templates "العودة للأبواب"), safety action group (Access).
8. D03/D04 axis + rhythm drift · per-feature margins · arbitrary title truncation.
9. Quran/context typography slot; Access safety actions in the header.
10. **Wide** title + actions on one row. **Medium** actions wrap under the title, order preserved. **Compact** actions become a full-width stack (primary first) or a bottom bar for workspaces.
11. no action · one primary · multiple safety actions · long Arabic title (wraps, max 2 lines, then truncate + disclosure) · loading count (reserved-width placeholder).
12. Header block 24 tall min; action group gap 8.
13. eyebrow→title 4; title→description 8; description→meta 12.
14. `t-h1` 30 Naskh / `t-h2` 24 / description `t-body-lg`.
15. Title WRAP→TRUNCATE at 2 lines with full-value disclosure; description WRAP.
16. `<h1>` once per page; section headers `<h2>`; action group `role="group" aria-label`.
17. "الجذور" + "٠٢ أوسع تجميع صرفي" + "النواة التي تخرج منها الأسماء والأفعال جميعًا."; "إدارة الوصول" + "متابعة الحسابات والصلاحيات المباشرة وسجل التغييرات بعناية."
18. Every page and panel.

---

## F04 — Surface / Card

1. Bounded content container.
2. Dashboard destinations, Words hub curriculum, Abwab door cards, Access permission groups, audit events, Mushaf study/source cards, quiet review cards.
3. `[optional header: title · meta · action] [body] [optional footer]`.
4. `--qd-surface`, hairline, `radius-md`, no resting shadow. Hover `--qd-surface-quiet` only. Selected = green tint + 2px inline-start thread. Disabled = `--qd-neutral-tint` + `--qd-neutral-ink-disabled` + explanation text.
5. Whole-card activation when the card is a navigation target (single accessible name); otherwise interactive children only. No hover lift, no scale.
6. `navigation` · `quiet` (metadata) · `selectable` · `event` (audit) · `group-surface` (a nested semantic group such as an Access permission group: quiet fill, hairline, `<fieldset>`/`role="group"` + legend — **not** an independent card) · `study` (Mushaf content slot) · `safety` (warning-tinted, danger hairline).
7. Optional: header action, footer meta, badge row, count strip, warning slot, protected-content slot.
8. D07/D08 unbounded grids · radius/padding vocabulary drift · accent-border generic hover (D15) · shadows/lifts.
9. Safety-warning and Quran-content slots; Dashboard destinations vs Words curriculum semantics (G05).
10. Grid rules in `GOLDEN_UI_SYSTEM.md` §5.2 (max columns + orphan rule per collection). **Compact** always single column. Nesting: one intentional **group surface** inside a card is permitted for a real grouping contract (Access permission groups); gratuitous card-in-card is not (§5.4).
11. rest · hover · focus-visible · selected · disabled (+reason) · loading (skeleton at final geometry) · error (inline block inside body) · empty body · long content.
12. Padding 16 (Dense 12); min-block 0 (never a fixed card height); grid gap 16.
13. header→body 12; body row gap 8.
14. Card title `t-h4` (Naskh for entity/teaching cards, sans for admin cards); meta `t-meta`.
15. Titles TRUNCATE (2 lines) + disclosure; body WRAP; Quran slot EXPAND.
16. One accessible name per card; `aria-pressed`/`aria-selected` only on selectable variants; group cards use `<fieldset>`+`<legend>` semantics for the permission variant.
17. "المصحف والآيات — عرض صفحات المصحف والآيات الكريمة"; permission group "الأبواب" with `indeterminate`; audit event "منح صلاحية · abwab.doors.create".
18. F19, F20, F18, dashboards, hubs.

---

## F05 — Button / Action

1. All user-initiated actions.
2. Everywhere.
3. `[icon slot] [label] [optional count/badge]` — icon slot reserved so busy never resizes.
4. `primary` solid `--qd-green-solid` / white; `secondary` surface + hairline; `tertiary` text-only; `danger` danger-tinted with danger hairline (solid danger only inside a confirm footer); `icon` square. Radius 6.
5. Hover = border/background tone change only. Active = tone change, **no translate** (D14). Busy = label persists + spinner in the icon slot + `aria-busy` + disabled. Destructive actions always require an intermediate confirm or a staged review.
6. `primary` · `secondary` · `tertiary` · `danger` · `icon-only` · `toolbar` (grouped, shared hairline) · `row-action` (icon, appears on hover/focus/selection, always present below Wide).
7. Optional: leading icon, trailing count chip, keyboard-shortcut hint (Wide only).
8. D14 active translate · 20–32px targets (D45/D46/D47) · local visual copies (D22) · green used for generic hover (D12).
9. One primary per view; explicit destructive semantics; Archive-Restore may be **visible-disabled** with an explanation (G22) whereas most missing writes are hidden.
10. **Wide** `ctl-md` inline groups. **Medium** same, wrapping in declared order. **Compact** `ctl-lg`, full-width stack for page/modal primaries; dense modal workflows may use `ctl-sm`.
11. rest · hover · focus-visible · active · disabled (+ reason when the disable is a capability statement) · busy · danger · icon-only (named).
12. Heights 32/40/48; icon 16–20 inside a ≥44px hit area except approved 32px dense modal workflows; min-inline-size reserved from resting label.
13. Group gap 8; icon→label 6.
14. `t-body` 14, weight 600 for primary, 500 otherwise. Never all-caps (meaningless in Arabic).
15. Labels never truncate; long labels wrap to 2 lines at Compact.
16. Icon-only buttons carry `aria-label`; disabled-with-reason uses `aria-describedby`; danger actions announce the target in their accessible name ("أرشفة الباب: الجهاد").
17. "حفظ", "إلغاء", "باب رئيسي جديد", "تطبيق المرشحات", "أرشفة", "استعادة", "تحديد جماعي".
18. All families.

---

## F06 — Form Field / Control

1. Input of text, numbers, choices and inline edits.
2. Access filters and relink, Abwab authoring/pickers/order editors, explorer search and range filters, Mushaf page/surah controls, Templates.
3. `[label] [required marker] [control] [helper] [error]` — helper and error share one reserved line slot.
4. One geometry for input/select/textarea/checkbox row/segmented (D20). Placeholder `--qd-ink-muted`. Select chevron is an **icon asset**, never a gradient (D17). Invalid = danger hairline + danger helper text + `aria-invalid`.
5. `:focus-visible` only (D21). Draft vs applied semantics owned by F08. Aliases: type + Enter → chip with a 44px remove target. Numeric order edit: `ctl-sm` inside a 44px row, Enter commits, Escape reverts, explicit save affordance for write actions.
6. `text` · `textarea` (3→8 rows, resize-block) · `select` · `search` (with clear) · `checkbox` / `checkbox-group` (with indeterminate) · `radio-set` · `range-pair` (min/max counts) · `chips-input` (aliases) · `inline-compact` (order editor) · `masked-evidence` (relink token, read-only mono).
7. Optional: prefix/suffix (LTR isolate), clear button, unit hint, character/row growth, "غير مرتبط" null presentation (`—` + label).
8. D20 input/select geometry disagreement · D21 mixed focus selectors · D22 local Abwab field styling · D17 gradient chevron · D42 focus-driven width change.
9. Direction overrides for email/code/subject; free-text "آية تمثل الباب" is authored non-Quran text and is never normalised or treated as scripture (helper copy states this); compact order editor.
10. **Wide** label above control, 2–3 columns in forms. **Medium** 2 columns max. **Compact** single column, full-width, `ctl-lg`, keyboard-safe modal padding (`env(safe-area-inset-bottom)`).
11. empty · filled · required · invalid (+ server error) · disabled · read-only · busy · indeterminate · with-helper · long-error (wraps, no clipping).
12. Heights per §3.6; textarea min 3 rows; checkbox 20px glyph in a 44px row. **A field that flexes inside a toolbar row is `flex: 1 1 0` with `min-inline-size: 0`**, and its sibling controls are `flex: 0 0 auto` with `white-space: nowrap` — a text input's default `min-width: auto` (≈ 20 characters) otherwise refuses to shrink and overflows the row at Compact. Where the row still cannot fit, it stacks; it never widens the page.
13. label→control 6; control→helper 4; field→field 16; group→group 24.
14. Label `t-meta` 600; value `t-body`; helper `t-caption`; mono for LTR values.
15. Labels WRAP; values TRUNCATE with disclosure; errors WRAP unbounded.
16. Explicit `<label for>`; `aria-describedby` for helper+error; group `<fieldset><legend>`; `aria-invalid`; required marked in text as well as `*`.
17. "اسم الباب *", "وصف الباب", "آية تمثل الباب — نص حر", "أسماء الباب للبحث", "القسم"; "الاسم أو البريد", "كل الحالات", "كل الحسابات"; `owner.research.supervisor@example.test`.
18. F08, F14, F19, F20.

---

## F07 — Tabs / Segmented Control

1. Switch between sibling views of one object or surface.
2. Access tabs, Abwab section tabs + tree/cards toggle, explorer detail tabs + subtabs, Word Type table modes, Mushaf ayah study tabs.
3. `[tablist track] [tab: label + optional count] … [panel]`.
4. Pill tabs on `--qd-surface-sunken`; current = green tint + `--qd-green-text` + 2px thread on the block-end edge. One primitive — no secondary-button tabs (D28).
5. Roving tabindex, logical RTL Arrow mapping, Home/End, `aria-controls`; selection may be URL/query-bound; panel state changes never reflow the tablist.
6. `segmented` (2–3, equal width) · `scrollable` (4+ at Medium/Compact, edge fade) · `subtabs` (nested, `t-meta`, quiet track) · `mode-toggle` (2 items, e.g. شجرة/بطاقات).
7. Optional: count badge per tab, disabled tab **with reason**, panel-level toolbar.
8. D27 18 manual tablists · D28 mixed primitives · D29 missing ayah-tab keyboard · D30 3-tab forced into 2 columns.
9. Domain tab counts and labels (G07: Roots 5, Lemmas/Stems 4, Unique 3, Word Type word 2 / grouped 3); route/query semantics.
10. **Wide** segmented up to 5. **Medium** scrollable single row ≥4. **Compact** scrollable, 48px tabs, current scrolled into view.
11. selected · default · 2/3/4/5 tabs · long labels (truncate + §8.1 disclosure) · with counts · **zero count → disabled tab with a visible reason** (`aria-disabled` + `aria-describedby`; D36 locked — never an empty panel) · panel loading/empty/error · keyboard focus.
12. Tab `ctl-md` (Compact `ctl-lg`); track padding 4; tab gap 4.
13. tablist→panel 16.
14. `t-body`; count `t-caption` in a `radius-xs` chip.
15. Labels TRUNCATE at 12rem + disclosure; the row scrolls rather than wrapping.
16. `role="tablist"`/`tab`/`tabpanel`, `aria-selected`, per-instance IDs (D31), panel `tabindex="-1"` for focus-on-activate.
17. "الكلمات · الآيات · السور · الصيغ · الأصول"; "مساحة العمل · سجل الوصول · الأمان المتقدم"; "التفسير · الترجمة · الإعراب · آيات قريبة (1) · المتشابهات (3)".
18. F09, F11, F14, F18, F19, F20.

---

## F08 — Search / Filter / Toolbar

1. Compose a query and report its result metadata.
2. All five explorers, Word Types taxonomy, Access user list + audit filters, Abwab search + mode toggle.
3. `[search field] [entity filters] [result metadata] [apply/clear] [view controls] · [applied-filter summary line]`.
4. Band on `--qd-surface-sunken`, `radius-md`, min-block 56, stable height across draft/applied because the applied-summary line is a **reserved single line**.
5. Explicit submit (`تطبيق المرشحات`) with Enter support; `مسح` clears all and announces the reset; draft state visibly distinct from applied (draft = hairline-strong, applied chips = green-tinted); pickers follow F15.
6. `explorer` (search + ranges + associations) · `taxonomy` (Word Types: main-type row + child chips + secondary selects) · `list` (Access: search + status + membership) · `audit` (two searchable user pickers + 2 selects + apply) · `workspace` (Abwab: search + section tabs + view toggle).
7. Optional zones: result count, active-filter chip row, sort control, density toggle, "Load more" is **not** here (F13).
8. D06 extra gutter · arbitrary local gaps/breakpoints · excessive blank filter height (Lemmas phone, Word Types) · popup behaviour drift (D33/D34) · toolbar entrance motion (D19).
9. Entity-specific fields (G08); Abwab mode-specific search meaning (G19: live tree marks and retains hierarchy, cards filter the level, archive prunes paths, pickers filter their own hierarchy).
10. **Wide** one to two rows. **Medium** max two rows in declared order (search → primary filter → apply), extras behind "مرشحات إضافية" disclosure. **Compact** search + a "المرشحات (2)" sheet trigger; the sheet applies on submit; no tall empty filter region.
11. draft · applied (+chips) · active count · 0/1/many options · popup open · no-match · options loading (`select` skeleton, width reserved) · option error + retry · cleared · disabled/unready (catalogue failure fails closed with an explanation).
12. Band padding 16; control gap 8; row gap 12; applied-summary line reserved 20px. Flexing search fields are `flex: 1 1 0; min-inline-size: 0`; adjacent filter/sheet triggers are `flex: 0 0 auto; white-space: nowrap` (see F06 field 12) — this is the guard that keeps Compact search-plus-filter rows from overflowing.
13. As above; toolbar→content 16.
14. Labels `t-meta`; result metadata `t-body` with tabular numerals and 7-digit reserved width.
15. Filter labels TRUNCATE + disclosure; the band wraps in a declared order rather than scrolling.
16. `role="search"` on the search region; results announced in the workspace live region ("عدد الجذور: 1,642"); every filter has a persistent visible label.
17. "ابحث في الأبواب…", "اكتب جذرًا…", "ترتيب المصحف", "تصفية حسب الأعداد", "تصفية حسب الارتباط: الجذر / الأصل الصرفي / الصيغة المعجمية — الكل · موجود · غير موجود".
18. F09, F10, F19, F20.

---

## F09 — Golden Table

1. One tabular family for the five Words explorers.
2. Roots, Lemmas, Stems, Unique (simple + vocalized), Word Types word (9 col) and grouped (5 col).
3. `[table container] → [sticky header row: index · identity · count columns] → [body rows] → [pagination slot]`; Compact renders `[semantic card rows]`.
4. Container `--qd-surface` + hairline + `radius-md`; header `--qd-bg-chrome` + `hairline-strong`; rows separated by hairlines; count values in `radius-xs` chips with tabular numerals (existing pattern, screenshot `07`); selected row = green tint + **2px inline-start** thread (D26).
5. Row click selects (never navigates); Enter/Space selects; Up/Down moves focus with `aria-activedescendant`-free roving focus; sortable headers are buttons with `aria-sort` and 3-state cycle (asc → desc → default); selection updates the detail panel and the URL only where the current contract does.
6. `standard` (identity + counts: Roots/Lemmas/Stems/Unique) · `wide-columns` (Word Types word, 9 columns with a column budget) · `grouped-rows` (Word Types grouped, 5 columns, display-only members) — one shell, three row renderers. Access users and audit are **not** in this family (they are F10).
7. Optional zones: bulk checkbox column, sort control, column-budget overflow disclosure, nested detail pagination, row overflow action.
8. D23 five duplicated shells · D24 `aria-rowcount` only on Word Types · D26 physical selected edge · squeezed desktop table at 768 · D40 inconsistent loading vocabulary.
9. G07 column sets · G09 per-entity mobile row heights · G10 Word Types ordering, taxonomy filters and display-only grouped members · G06 Unique simple/vocalized modes.
10. **Wide** full columns, sticky header, container-owned inline scroll if needed, split with the detail panel. **Medium** column budget = index + identity + 3 highest-value counts + "كل الأعداد" disclosure per row; detail as a sheet; **no page overflow** (§4.1). **Compact** semantic cards: identity line + labelled count chips wrapping, entity-specific min-heights (G09), 48px row target, detail as a sheet.
11. initial skeleton (10 rows at final height) · refreshing (2px bar, rows retained) · ready · initial empty · filtered no-match (+ clear) · read error + retry · selected · sort asc/desc · null values (`—` + "غير مرتبط") · long identity (truncate + disclosure) · 0 / 114 / 21,294 / 1,000,000 counts · first/middle/last page · 1000-row page size.
12. Header 44; row 40 (Dense 36); index column 3rem; count column min 4.5rem sized for 7 digits; selected thread 2px.
13. Cell padding-inline 12, padding-block 8; container padding 0.
14. Identity Naskh `t-body-lg`/`t-identity` in details; counts `t-body` tabular; column labels `t-caption` `--qd-ink-muted`.
15. Cells TRUNCATE with the full value carried by the **owning row** per §8.1 (never `title` alone — D35, and never a synthetic tab stop per cell); container SCROLL owns overflow; document never scrolls horizontally.
16. `role="table"` + `aria-rowcount`/`aria-colcount` on **all** variants (D24); `<th scope="col">` with `aria-sort`; row `aria-selected`; Compact cards use `role="list"`/`listitem` with label-value pairs.
17. `س م و` 381/352/81/26/52/6/29 · `ا ل ه` 2,851/1,879/86 · `ر ح م` 339/313/62 · total 1,642 · Word Types اسم 12,364 / فعل 8,544 / حرف وأداة 800 / حروف مقطعة 14 · grouped rows with a zero-count member (D36 case).
18. F11 (selection), F13, F08, F10.

---

## F10 — Result List (non-table)

1. Ordered, semantic lists that are not tabular.
2. Explorer detail lists (words, ayahs, surahs, lemmas, stems, type distribution, grouped members), Access user rail, Access audit events, Mushaf similar-ayah and Mutashabihat result groups.
3. `[list heading + count] [rows] [optional pagination]`; row = `[index] [primary value] [meta] [optional count chip] [optional link/action]`.
4. Rows on `--qd-surface-quiet` separated by hairlines; link rows show an inline-end chevron; selected row = green tint + thread; count chips `radius-xs`.
5. Whole-row activation for link rows (single accessible name); display-only rows are not focusable; **a zero-count row or link is disabled with a visible reason** (D36 locked) rather than opening an empty detail.
6. `linked` (navigates/opens overlay) · `display-only` (type distribution, grouped members) · `master` (Access user rail: name/email/Owner/status, selectable) · `event` (audit cards, newest first, `Load more`) · `quran-result` (F18 renders the content; F10 supplies only the frame).
7. Optional: count chip, secondary meta line, status badges, per-list pagination, group header.
8. D25 inconsistent list roles · locally repeated row chrome · ambiguous zero actions (now one rule: disabled + visible reason — D36) · duplicate list shells.
9. Quran ayah cards and display-only distributions stay distinct renderers (G11); Access users/audit are deliberately **not** Golden Table (handoff §24.9).
10. **Wide** two columns for audit events (max 2), single column elsewhere. **Medium** single column. **Compact** single column, 48px rows, meta wraps under the value.
11. loading (skeleton rows) · ready · empty · zero count · error + retry · paginated (independent nested page) · selected · long values.
12. Row min-block 44 (Compact 48); index column 2.5rem.
13. Row padding 12/12; list gap 0 (hairlines); heading→list 12.
14. Primary value Naskh for entities / sans for admin; meta `t-meta`; count `t-caption`.
15. Values TRUNCATE + disclosure; Quran rows EXPAND; audit reason WRAPs fully.
16. `role="list"`/`listitem` on **every** variant (D25); count in the accessible name; `aria-current` for the selected master row.
17. Detail words: بسم 3 · السما 109 · والسما 9 · سموت 5 · السما 5 · باسما 1 · باسمائهم 2 · السموت 182 · اسمه 5 · مسمى 21 · سميتها 1 · اسم 14. Access rail: 900001 pending (null name → email shown) · 900002 owner active · 900003 disabled. Audit: "منح صلاحية · abwab.doors.create · 2026/08/08 15:33 · منفّذ: النظام".
18. F11, F19, F18.

---

## F11 — Details Workspace

1. Show and act on the selected object.
2. Five explorer details, Access selected user, Abwab selected-door side panel, Mushaf study region, global overlay bodies.
3. `[header: identity · metadata row · status · actions] [tab row] [status slot] [body: one scroller] [optional footer]`.
4. One shell for all consumers: `--qd-surface`, hairline, `radius-md`; identity Naskh `t-identity`; metadata as label/value pairs; header and tab row are fixed while the body changes.
5. Selection arrives from F09/F10/F16; tabs per F07; related links open the global overlay (F14 `overlay`); the shell stays mounted through loading/empty/error (never collapses the split).
6. `entity` (Words: 5/4/4/3/2-3 tabs per G07) · `safety` (Access: permission editor or lifecycle explanation + staged review) · `action-rail` (Abwab: selected door + permitted actions) · `study` (Mushaf: protected content zones) · `overlay-body` (adapter content inside F14).
7. Optional zones: identity chip (كلمة/جذر/صيغة), metadata grid, subtab row, related-entity links, action group, safety warning, review/diff slot, pagination for nested lists.
8. Header/tab/body geometry drift · D31 fixed DOM IDs → per-instance IDs · D32 inconsistent `notFound` semantics (one rule: stay in the tabpanel, render the error state, keep the label) · desktop blank panels (replaced by a designed no-selection state) · drawer variants unified into F14 · D41 blank mutation reserve.
9. G07 tab counts · G12/G13 study source hierarchies and similarity semantics · G17/G18 Access lifecycle + inline staged review · G14 overlay supports word identity only, grouped identities remain local.
10. **Wide** inline panel beside the table (split `1.25fr/1fr`) or a fixed rail. **Medium** sheet/drawer (88dvh) opened by selection, with the selected identity pinned in a context bar so the user never loses it. **Compact** full-height sheet (94dvh), one scroller, header always visible.
11. no selection (designed prompt, not a blank area) · loading (header+tabs+row skeleton) · ready · empty tab · error + retry · notFound (deleted/invalid deep link) · each tab/subtab · action available / permission-hidden / visible-disabled + reason (G22) · dirty (Access) · busy.
12. Panel min-block 24rem at Wide; body is the only scroller; header 64–80; status slot 0 when idle.
13. Header padding 16; header→tabs 12; tabs→body 12; body row gap 0 (hairlines).
14. Identity `t-identity` Naskh; labels `t-caption`; values `t-body`.
15. Identity TRUNCATE + full value in the metadata row; body lists per F10; Quran EXPAND; exactly one scroller (removes the Word Types nested-scroller risk).
16. Panel `aria-labelledby` the identity; tabs per F07; status slot `aria-live="polite"`; `notFound` uses `role="status"`, write errors `role="alert"`.
17. Root `س م و` with 5 tabs + بدون تشكيل/بالتشكيل subtabs; Access user 900002 owner (no editor + bypass explanation) and 900001 pending (editor + 19 permissions in 5 groups); Abwab door 930007 (depth 7 · 24 children · 1,284 descendants · relations 0 dashed).
18. F09, F10, F14, F19, F20, F18.

---

## F12 — Async & Feedback States

1. Communicate loading, refreshing, emptiness, failure and outcome as five distinct concepts (replacing one conflated `qd-state` across 53 call sites).
2. Every data surface.
3. Per concept — see `GOLDEN_UI_SYSTEM.md` §6.4.
4. Skeleton = `--qd-surface-sunken` blocks, flat pulse, **no gradient** (D18). Refresh = flat 2px indicator (solid green segment translating along a hairline track, static under reduced-motion) + `aria-busy`. Empty = one line + optional action, no illustration. Error = danger-tinted block with wrapping message + Retry. Notice = single-line inline card, `aria-live`, `--qd-mutation-success` semantics (distinct from `--qd-lifecycle-active`).
5. Retry re-issues only the failed scope; refresh never discards rendered rows; a write error never clears the user's draft.
6. `skeleton-table` · `skeleton-panel` · `skeleton-list` · `skeleton-quran-page` (measured 15-line canvas; below Wide the 52rem reservation is preserved — G03) · `refresh-inline` · `empty-initial` · `empty-filtered` · `error-read` · `error-write` · `notice`.
7. Optional: retry, secondary action ("مسح المرشحات"), technical detail disclosure (collapsed, mono).
8. D39 conflated state component · D40 text-loader vs skeleton inconsistency · D41 permanent invisible reserve · D18 gradient shimmer.
9. Content-shaped Quran/table skeletons; write errors stay near their origin. **Migration:** the legacy `qd-state` may remain temporarily as a compatibility adapter delegating to these five primitives — never as the semantic owner, never gaining new consumers (`GOLDEN_UI_SYSTEM.md` §6.4).
10. Same vocabulary in all modes; Compact messages are short (≤2 lines) with the Retry action full-width and always reachable without scrolling within the region.
11. initial loading · refreshing · empty · filtered no-result · read error · write error (400/401/403/409 mapped to distinct copy: invalid / unauthorized / forbidden / conflict) · notice · success · conflict (refreshes selected state and states that nothing was retried automatically).
12. Read states: `min-block-size: min(40vh, 20rem)` inside a mounted shell. Write states: content height, no reserve. Skeleton blocks match final line heights exactly.
13. Padding 16; message→action 12.
14. Message `t-body`; title (error only) `t-h4`; technical detail mono `t-caption`.
15. Messages WRAP unbounded (server text may be long); technical detail SCROLLs inside its disclosure.
16. `role="status"` for loading/empty/notice, `role="alert"` for write errors, `aria-busy` on refreshing regions, one live region per workspace, Retry is a real button with a scoped accessible name ("إعادة المحاولة: قائمة الجذور").
17. "جارٍ تحميل المستخدمين…", "لا نتائج مطابقة للمرشحات", "تعارض: تم تحديث السجل من جهة أخرى — أعد المراجعة قبل الحفظ.", "تم منح 3 صلاحيات وسحب 1."
18. All families.

---

## F13 — Pagination & Result Metadata

1. Move through and describe paged results.
2. Explorer main tables, nested detail lists, Access user rail, audit `Load more`.
3. `[السابق] [numerals … ellipsis] [التالي] [optional jump: input + Go] · [result metadata elsewhere in F08]`.
4. Controls follow F05 `secondary`/`tertiary`; current page = green tint + `--qd-green-text`; disabled edges are visibly disabled, never removed.
5. Fixed geometry in all states; jump validates on submit with an inline error in a reserved line; page change announces the new range.
6. `numeric` · `numeric+jump` · `load-more` (audit — separate capability, appended count announced) · `nested` (inside a detail list, `ctl-sm` in 44px hit areas).
7. Optional: page-size hint, total count, jump.
8. D42 widen-on-focus · D43 conditionally mounted Go · D44 duplicate IDs · D45 28–32px phone targets.
9. `Load more` is not numeric pagination; nested detail lists keep independent pages.
10. **Wide** single row, `ctl-md`. **Medium** same. **Compact** `ctl-lg`, numerals reduced to current ± 1 with ellipsis, prev/next always visible.
11. zero pages (bar hidden, metadata still shown) · first · middle · last · huge total · jump idle/focus/invalid/submitting · disabled prev/next · loading (bar reserved).
12. Bar 56; jump input fixed `6rem`; numeral button min 40 (Compact 48) with 44px hit area.
13. Control gap 8; bar padding-block 12.
14. `t-body` tabular numerals.
15. Numerals never wrap; the row collapses numerals before wrapping.
16. `<nav aria-label="التنقل بين الصفحات">`, `aria-current="page"`, per-instance input/error IDs (D44), live region for page changes.
17. 1 · 2 · … · التالي; "عدد الجذور: 1,642"; audit "تحميل المزيد".
18. F09, F10, F19.

---

## F14 — Modal / Drawer / Overlay Shell

1. One shell system for confirmations, authoring, pickers, mobile details and the cross-route entity overlay.
2. Shared confirm (1 Access + 7 Abwab cases), six Abwab authoring dialogs, five Words drawers, global detail overlay, nested dirty alert.
3. `[backdrop] [shell: sticky header (title · optional identity/count · Close) → body (single scroller) → sticky footer (actions · error slot · nested alert strip)]`.
4. `--qd-surface`, `radius-lg`, `--qd-shadow-layer`, backdrop `rgba(35,33,28,.38)`. Footer separated by `hairline-strong`. No shell-level animation beyond a 120ms opacity fade.
5. Focus trap + focus return; Escape closes (dirty → nested `alertdialog` strip in the footer); backdrop click closes only non-dirty, non-destructive shells; one scroller; submitting disables the footer in place.
6. Widths/heights per `GOLDEN_UI_SYSTEM.md` §6.3: `confirm` 30rem · `form` 38rem · `wide` 52rem · `overlay` 46rem · Compact full-bleed 94dvh.
7. Optional zones: identity chip + count (overlay), history controls (Back / Restore), section groups (authoring), picker search + hierarchy (move/door/template-copy), warning slot (destructive), rules explainer (template copy), nested confirm.
8. D48 five geometry families · D49 confirm lacking viewport/scroll rules and double padding · legacy 42rem drawer differences · per-dialog padding ownership.
9. G15 overlay Back / retained Restore / base-route preservation / 8-frame cap · G18 Abwab modal authoring vs Access inline review · G21 move-destination picker vs door-set picker · G24 template-copy rules stated in the confirmation · fixed authoring height for zero-resize stability.
10. **Wide** centred, named widths. **Medium** same widths capped to `100% − 48`. **Compact** full-bleed sheet, 94dvh, safe-area padding, header/footer always visible, keyboard-aware body.
11. loading body · ready · long content (body scrolls) · validation invalid · server error (400/401/403/409 copy) · conflict · dirty close · busy submit · destructive confirm (target named) · nested delete confirm · picker empty/no-match/excluded/disabled · overlay depth 1–8 + cap rejection · retained-closed Restore.
12. Header 64, footer 72 (Compact 80 with safe area); body padding 20 (Compact 16) — **owned by the shell only**.
13. Section gap 20; field gap 16; footer action gap 8.
14. Title `t-h3`; identity chip `t-caption`; body per F06/F10.
15. Body is the single scroller; long validation text wraps; picker hierarchies scroll inside the body; no nested horizontal scroll.
16. `role="dialog" aria-modal="true"` + `aria-labelledby`; nested strip `role="alertdialog"`; Close has a text name; focus starts on the first field or the safe action (never on a destructive one); overlay announces depth changes.
17. "إضافة باب جديد — سيُضاف كباب رئيسي" (name/description/representative-ayah/aliases/section, حفظ · إلغاء); overlay "كلمة بسم — الآيات: 3" with tabs السور / لم يذكر فيها / الآيات; confirm "أرشفة الباب: باب طويل متعدد المستويات… — سيتم أرشفة 24 بابًا فرعيًا".
18. F06, F07, F10, F11, F16, F19, F20.

---

## F15 — Floating Layer / Menu / Popover / Tooltip

1. Transient anchored surfaces.
2. Navbar account/settings menus, Abwab row and context menus, source/association/surah pickers, full-value disclosure popovers, retained-overlay Restore.
3. `[trigger] [layer: optional search → optional group headers → items → optional empty/error]`.
4. `--qd-surface`, `radius-md`, `--qd-shadow-layer`, hairline; item hover `--qd-surface-quiet`; selected item = green tint + check glyph; danger item = danger text + danger tint on hover (shared, no local override — D50).
5. One keyboard contract: Enter/Space/ArrowDown opens; Escape closes + focus returns; Arrow navigation with scroll-into-view; Home/End; type-ahead in searchable pickers; Tab closes; outside click closes; block-axis flip + inline clamp; never affects document flow (D33, D34).
6. `action-menu` · `select-listbox` · `searchable-picker` (surah, source, association, audit user pickers) · `disclosure-popover` (full value for truncated text — also opens on focus and long-press, D35) · `tooltip` (hint only, never the sole carrier of information).
7. Optional: search field, group headers, selected check, counts, footer action ("مسح الاختيار").
8. D33 divergent key sets · D34 divergent max-height/flip/collision · D35 pointer-only `title` · D46 hover-only row actions · D50 local danger hover override.
9. Searchable vs action-only contents; grouped surah list semantics.
10. **Wide** anchored layer. **Medium** anchored, clamped to viewport. **Compact** searchable pickers and action menus become bottom sheets with 48px rows.
11. closed · open · keyboard-entered · selected · loading options · empty · no-match · error + retry · danger item · disabled item + reason.
12. Min-inline 12rem, max-inline 24rem (searchable 28rem); `max-block: min(60vh, 24rem)`; item 40 (Compact 48) with 44px hit area.
13. Layer padding 4; item padding-inline 12; group header padding 8/12.
14. Items `t-body`; group headers `t-caption` `--qd-ink-muted`.
15. Item labels TRUNCATE + full value in the item's accessible name; layer SCROLLs internally.
16. `role="menu"/"menuitem"` for actions, `role="listbox"/"option"` for selection, `aria-expanded` on triggers, `aria-activedescendant` in searchable pickers, focus return guaranteed.
17. Settings menu (الإعدادات · تسجيل الخروج); tree row menu (إضافة باب فرعي · تعديل التفاصيل · نقل إلى… · العلاقات · أرشفة); surah picker grouped list of 114.
18. F01, F06, F08, F16, F20.

---

## F16 — Tree / Hierarchical Picker

1. Represent and navigate real hierarchy.
2. Abwab live tree, archive tree, template hierarchy, move-destination picker, door-set picker, Surah grouped list.
3. Row = `[indent (budgeted)] [optional bulk checkbox] [chevron] [order chip / inline order editor] [name] [direct-child count] [descendant count] [max-depth count] [relation flag] [add-child] [overflow]`.
4. Rows 44 tall on `--qd-surface`, hairlines between siblings, indentation guides as 1px `--qd-hairline` verticals at each level; selected row = green tint + 2px inline-start thread; dashed-zero relation flag preserved.
5. Roving tabindex; **logical** RTL arrows (inline-start arrow collapses/moves to parent, inline-end expands); Enter/Space selects, chevron expands; ContextMenu/Shift+F10 opens the row menu; bulk selection with Shift range; actions always present at Medium/Compact and on hover/focus/selection at Wide (D46).
6. `live-tree` (full keyboard + actions) · `archive-tree` (read-only, Restore may be visible-disabled — G22) · `template-list` (`role="list"`, no arrow navigation — G20) · `destination-picker` (single select, cycle/subtree exclusion — G21) · `set-picker` (multi-select checkboxes — G21) · `grouped-list` (Surah picker).
7. Optional: bulk mode, order editor, search marking, breadcrumb, depth marker chip, per-row menu.
8. D46 16–20px targets · uncapped indentation · D22 local field/button styling · inconsistent picker/listbox behaviour (D33/D34) · hover-only actions.
9. G19 mode-specific search semantics · G20 tree vs list roles · G21 destination vs set pickers · G22 archive Restore visible-disabled · G23 cards have no context menu · no invented protected/locked state.
10. **Wide** tree + 18rem action rail; depth budget 6 levels. **Medium** full-width tree, secondary counts (descendants, max depth) hidden, sticky bottom action bar carries the selection. **Compact** same as Medium with 48px rows and a breadcrumb strip showing the ancestor path.
11. collapsed · expanded · selected · focused · deep (depth 7+ → depth marker) · many children (24) · many descendants (1,284) · search-marked · search zero-match (hierarchy retained, count reported — G19) · loading skeleton · read error · write error/conflict · excluded/disabled in pickers · bulk · permission-hidden actions · retired section.
12. Row 44 (Dense 40, hit area always 44); indent `min(depth,6) × 16`; name `min-inline 12rem`; count chips 4.5rem.
13. Row padding-inline 8; group gap 0; tree→rail 24.
14. Name Naskh `t-body-lg`; counts `t-caption` tabular; order chip `t-caption` mono.
15. Name TRUNCATE with disclosure + full name in the selected panel; **indentation is budgeted, not unbounded**; tree container SCROLLs vertically; never horizontally at page level.
16. `role="tree"`/`treeitem` with `aria-level`/`aria-expanded`/`aria-selected` (live + archive only); template hierarchy `role="list"`; counts in accessible names; search results announced.
17. Section "قسم بحثي طويل لاختبار العنوان في شريط الأقسام" (128 doors); root "الجهاد" (1 child); door 930007 depth 7 · 24 · 1,284 · maxRelativeDepth 5 · relations 0; template "قالب بحثي متعدد الفروع" (47 nodes).
18. F20, F14, F11.

---

## F17 — Chip / Badge / Status / Count

1. Compact labelled values and states.
2. Access lifecycle + Owner, permission counts, explorer count chips, Word Type child chips, Abwab order/relation/alias chips, applied-filter chips.
3. `[optional glyph] [label] [optional count] [optional remove]`.
4. `count` chip: `radius-xs`, `--qd-surface-quiet`, hairline, tabular numerals. `status` badge: `radius-pill`, tinted per §2.4 **with glyph + text**. `filter` chip: green tint when applied. `alias` chip: pill with a 44px remove target. `order` chip: mono numeral.
5. Chips are only interactive when they filter or remove — decorative chips are never focusable. **A zero-value chip that would open a detail is disabled with a visible, accessible reason** (D36 locked); a zero value that is pure information renders `0` (or the dashed `—` where the current design does) and is not interactive at all.
6. `count` · `status` (lifecycle-pending / lifecycle-active / lifecycle-disabled / lifecycle-unknown / archived / read-only — tokens per §2.4) · `membership` (Owner, outline) · `filter-applied` · `alias-removable` · `order` · `relation-flag` (including dashed zero) · `taxonomy` (Word Type child chip with count; disabled at zero).
7. Optional: glyph, count, remove, tooltip-free disclosure for long labels.
8. Local sizes/fills · inconsistent zero interaction (one rule now) · solid-green misuse · colour-only meaning · badge proliferation · one token serving two meanings under one name (`lifecycle-active` vs `mutation-success`).
9. G17 lifecycle + independent Owner membership · morphology/taxonomy semantics · removable alias semantics.
10. Same at all modes; chip rows wrap in reading order; remove targets grow to 44px at Compact.
11. zero (disabled + reason where it would navigate) · one · large (1,000,000 — tabular, reserved width, no reflow) · selected · removable · disabled + reason · warning/success/danger/neutral · long label (truncate at 14rem + §8.1 disclosure).
12. Height 24 (count) / 28 (status) / 32 (interactive filter/alias, 44px hit area); padding-inline 8–10.
13. Chip gap 6; row gap 6.
14. `t-caption` 12 (600 for status), tabular numerals for counts.
15. Labels TRUNCATE + disclosure; counts never truncate.
16. Status badge text is the source of truth (never colour alone); count chips include the dimension in their accessible name; remove buttons named "إزالة: اسم بديل".
17. "قيد الانتظار" / "نشط" / "معطّل" / "حالة غير معروفة" / "مالك" / "أرشيف" / "قراءة فقط"; counts 381 · 2,851 · 12,364 · 0; aliases "اسم بديل", "اسم بديل طويل لاختبار الشريحة".
18. F04, F09, F10, F11, F16, F19, F20.

---

## F18 — Quran / Study / Reader Surfaces (protected)

1. Render scripture and study material exactly, with canonical chrome around it.
2. Mushaf page canvas, selected word, selected ayah, ayah result cards, Tafsir/translation/i'rab, similar ayahs, Mutashabihat groups.
3. `[reader column: nav chrome → page canvas (protected) → page numeral] [study column: identity → morphology segments → metric cards → study tabs → source picker → content]`.
4. Page canvas `--qd-surface` with generous inner margin, no borders inside the canvas, surah/juz markers as-is. Selected word = light green tint behind the word only. Study cards `--qd-surface-quiet`, morphology category colour on the card's inline-start edge with its Arabic label. Highlighted phrase inside an ayah = green underline + tint (existing). The only Compact content-style exception is the exact linking ayah-selection rule in `FRONTEND_UI_RULES.md` §3.
5. Word/ayah selection drives the study column; source pickers per F15; study tabs per F07 (now with full keyboard — D29); **morphology segment rows are non-interactive content (D37 locked)** — no button semantics, no `role="button"`, no hover affordance, no pointer cursor, no focus ring; no Quran animation, ever.
6. `page-canvas` · `word-study` · `ayah-study` · `commentary` (Tafsir/translation/i'rab) · `similar-results` · `mutashabihat-groups` · `ayah-result-card` (used by F10 as a row renderer).
7. Optional zones: source picker, metric cards (occurrence counts), related-navigation links, similarity measures (score/coverage/matched), group phrase header.
8. D29 weak ayah-tab keyboard · D37 fake actionable segments (now non-interactive content) · D47 undersized page/nav triggers (now 44px hit areas around unchanged content) · generic truncation/animation applied to Quran.
9. G02 exact text/fonts/glyphs/markers/no-animation · G03 measured 40/60 shell and page-shaped reservations · G11 ayah cards vs data rows · G12 language-first Tafsir/translation hierarchy vs flat i'rab · G13 similar ayahs vs grouped Mutashabihat.
10. **Wide** 40/60 sticky reader + study. **Medium** reader first, study second, full width, reader keeps its measured geometry and its 52rem loading reservation. **Compact** same order, long document flow, study tabs scrollable.
11. page loading (15-line shaped skeleton) · page ready · first/last page (disabled nav, never hidden) · page error · empty page · no selection (study prompt) · word loading/ready/unavailable · null morphology fields · 0/1/many sources · long commentary · empty/error similar or Mutashabihat results · **D38 area: `panel`/`wordTab`/`segment` URL keys render no control — OWNER DECISION REQUIRED**.
12. Reader measure content-derived (protected); nav/page triggers ≥44px hit area; study card padding 16; commentary measure ≤68ch.
13. Reader→study gap 24; study block gap 16.
14. Quran = protected renderer only. Chrome = Naskh for ayah metadata, sans for labels, mono for verse keys (`2:25`).
15. Quran EXPAND always. Commentary EXPAND inside the study panel's single scroller. Source labels TRUNCATE + disclosure. No nested horizontal scroll anywhere in the reader or study column.
16. Page canvas is a labelled region ("صفحة ٥"); word triggers are buttons with accessible names including verse key and location; study tabs per F07; commentary is a labelled region with its source in the accessible name; `lang`/`dir` correct on mixed-direction sources.
17. Page 5, `2:25`, word location `2:25:1`, verified word `وَبَشِّرِ`; morphology فعل / بَشِّرِ / ب ش ر / التكرار 11 مرة في 6 سورة; tabs التفسير · الترجمة · الإعراب · آيات قريبة (1) · المتشابهات (3); source "التفسير الميسر". **All full ayah and commentary bodies are `[actual Quran text from API]` placeholders.**
18. Mushaf route; F10 (result rows); F14 (overlay bodies containing ayah cards).

---

## F19 — Access Management Workspace

1. Owner-only account, permission, audit and identity-recovery workspace.
2. `/settings/access` (`workspace` default, `audit`, `security`).
3. `[page header + description] [tabs] → workspace: [user rail: filters → list → pagination] [detail: identity header → status/Owner → **one** state-appropriate body → review dock] · audit: [filter card] [event list + Load more] · security: [relink workflow] [reconciliation status]`.

   **The detail body is state-exclusive.** A single frame never shows an Owner bypass explanation *and* a permission editor. The documented target states each own their body (field 6 below), and `Golden UI — Workspaces.dc.html` draws them as **separate frames** so no implementer reads "Active Owner edits direct permissions" out of a documentation composite.
4. Reuses F02 split-workspace + F04 + F06 + F07 + F10 + F17. Safety surfaces are warning/danger tinted with explicit target identity; nothing about a destructive action looks like an ordinary save.
5. Select user → detail; permission changes stage as `+N / −M`; sticky **Review dock** at the block-end of the detail column with "مراجعة التغييرات" / "تجاهل"; review shows added/revoked Arabic labels **and** stable codes, optional reason, destructive warning, no-op disabled, busy; switching users while dirty raises the canonical confirm; lifecycle actions (قبول / تعطيل / إعادة تفعيل) are separate, target-named and confirm-gated.
6. `workspace` · `audit` · `security`; detail sub-variants, each a **separate exclusive frame**: `pending-non-owner` (editor + staged review + activation consequence) · `active-non-owner` (editor + Save review + separate Disable action) · `disabled-non-owner` (no editor; Reactivate starts from zero direct grants, stated in text) · `active-owner` (no editor, no lifecycle action, bypass explanation only) · `pending-owner` / `disabled-owner` (no bypass; copy states Owner membership does not activate access; status and membership stay separate labels) · `unknown-status` (literal unknown label, never mapped to Disabled).
7. Optional zones: review diff, reason field, destructive warning, reconciliation fingerprint disclosure (collapsed, mono), masked evidence token, `Load more`.
8. D03 page rhythm bypass · D02 contradictory width classes · D41 6.5rem blank mutation band → zero-height notice slot + sticky review dock · picker/control drift (D33/D34/D20) · responsive rail failure at 768 (961px) · pointer-only identity disclosure.
9. G16 Owner-only route · G17 three lifecycle states + independent Owner · G18 inline staged review (not modal) · full LTR identity always visible before a safety decision · 409 refreshes selected state and reports conflict with no automatic retry.
10. **Wide** 20rem rail + detail. **Medium** selected-context bar (search + status + selected user) pinned above the detail; the list opens as a sheet — the user never traverses the whole rail to reach the detail. **Compact** same pattern, 48px rows, identity and status pinned while scrolling the editor.
11. list loading/empty/no-match/error · catalogue loading/unready/empty (fails closed with explanation) · 19 permissions across 5 groups with checked/unchecked/indeterminate · assignment disabled · draft `+3 / −1` · review · discard · busy · success notice · invalid / unauthorized / forbidden / conflict / generic error · dirty user switch · audit empty/error/pickers · relink initial/preview (POST — not exercised in audit) · reconciliation loading/error/absent/ready/unready/blocked/candidates · access denied / non-Owner route.
12. Rail 20rem; permission group **surfaces** 15–22rem, max 3 columns (one nested group level — §5.4); review dock 64 sticky (0 when clean); notice slot 0 when idle.
13. Page rhythm per F02/F03 (no local margins); group gap 16; field gap 16.
14. Identity Naskh `t-h3`; email mono LTR `t-meta`; permission label `t-body` + code mono `t-caption`.
15. Name/email TRUNCATE in the rail with the row owning the disclosure and the **full identity always rendered in the detail header** (§8.1 steps 1–2 — no synthetic tab stops); long reason WRAPs; audit event reason WRAPs fully; one scroller per column.
16. Owner guard stated in copy, not only enforced; `<fieldset><legend>` per permission group with `aria-describedby` for indeterminate meaning; review is a labelled region announced on entry; destructive buttons name their target; live region for mutation outcomes.
17. Users 900001 / 900002 / 900003 (§10 of the system doc); groups الأبواب 6 · الأقسام 4 · العلاقات 2 · القوالب 3 · عقد القوالب 4; audit event `PermissionGranted` with System actor and a long Arabic reason; relink masked evidence token.
18. Consumes F02–F08, F10, F12–F15, F17.

---

## F20 — Abwab Workspace / Tree / Authoring

1. Hierarchical classification workspace with permissioned authoring.
2. `/abwab` (`view=tree` · `view=cards` · `archive=1`) and `/abwab/templates`.
3. `[page header + conditional actions (الأرشيف · إدارة الأقسام · القوالب · باب رئيسي جديد)] [retained-modal Restore/Discard strip] [two result counts] [section tabs] [search + view toggle] [main: tree | cards | archive] [selected-action rail]`; Templates = `[16rem list rail] [hierarchy editor]`.
4. Reuses F02 split-workspace + F16 + F08 + F14 + F15 + F17. Cards are F04 `selectable` with a max-4-column grid (D08).
5. Selection drives the rail; permitted actions only (missing writes hidden, archive Restore the deliberate visible-disabled exception); URL owns `section/view/archive/door/card/q/modal` including retained `-closed` forms; search meaning is mode-specific and stated in the UI ("مطابقات مع الحفاظ على التسلسل" vs "تصفية هذا المستوى" vs "مسارات مطابقة فقط").
6. `tree` · `cards` · `archive` · `templates`; dialogs: Door · Move · Sections · Relations · Template node · Template copy (all on F14 `form`/`wide`).
7. Optional zones: bulk mode, order editors, relation groups (similar / opposite / more-comprehensive / less-comprehensive), retained-modal strip, breadcrumb, depth marker.
8. D01 double gutters · D08 unbounded card grid · D46 tiny tree actions/hover-only · D22 local field and button vocabulary · D48/D49 modal geometry and padding drift · D50 local danger hover · 768 overflow (961px).
9. G19 mode-specific search · G20 tree vs template list roles · G21 destination vs set pickers · G22 archive Restore visible-disabled · G23 cards have no context menu · G24 template apply copies detached direct children and never the root (stated in the confirm) · fixed authoring dialog height · **no protected/locked door state is invented**.
10. **Wide** tree + 18rem rail, depth budget 6. **Medium** full-width tree, secondary counts hidden, sticky bottom action bar with the selected door name. **Compact** same + ancestor breadcrumb; cards single column; dialogs full-bleed sheets.
11. tree loading skeleton · collapsed/expanded/selected/deep/many children · search-marked / zero-match (hierarchy retained) · bulk · order edit · permission-hidden vs visible-disabled · read error / write error / 409 conflict · empty tree / empty section / empty archive / archive populated · restore enabled vs disabled + reason · retired section · no-live-section create blocker · relations loading/error/empty/grouped · template list/detail/copy states · dirty close.
12. Rail 18rem; card grid 14–20rem max 4; tree row 44; dialog `form` 38rem × `min(92dvh,44rem)`; Templates rail 16rem.
13. Page rhythm per F02; rail card gap 12; tree indent 16/level (budgeted).
14. Door name Naskh `t-body-lg`; counts `t-caption` tabular; order mono.
15. Name TRUNCATE + disclosure; **indentation budgeted at 6 levels with a depth marker**; description/aliases WRAP; tree SCROLLs vertically only.
16. `role="tree"` live/archive with level/expanded/selected; template hierarchy `role="list"` (G20); every row action has a text name; bulk count announced; conflict announced via `role="alert"`.
17. Section 920001 (128 doors in scope, long name); door 930007 (depth 7 · 24 children · 1,284 descendants · maxRelativeDepth 5 · relations 0 dashed · aliases ×2 · description non-Quran); root "الجهاد"; template 940003 "قالب بحثي متعدد الفروع" (47 nodes); copy rules "تُنسخ الفروع المباشرة فقط · لا يُنسخ جذر القالب · النسخ غير مرتبطة بالقالب".
18. Consumes F02–F08, F10–F17.

---

## Coverage note

All 20 families in the handoff request list are designed. Naming is unchanged from §20 so review can be done row-by-row against the original request. Two families intentionally *contain* other families rather than duplicating them: F19 and F20 are **compositions** (their catalog entries name every primitive they consume), which is how the pack avoids both page-by-page styling and giant universal components.
