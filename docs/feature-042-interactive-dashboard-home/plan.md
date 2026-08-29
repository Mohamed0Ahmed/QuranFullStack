# Interactive Dashboard Home Implementation Plan

## Status and lifecycle

- Status: all implementation phases complete and verified; awaiting final engineering review.
- Surface: authenticated Angular product homepage at `/dashboard`.
- Register: product UI, not a public marketing landing page.
- Planning home: this feature-scoped document under `docs/feature-042-interactive-dashboard-home/`.
- Lifecycle: keep this plan through implementation and engineering review, then delete the entire
  feature planning folder in the final cleanup commit before merge, as required by `docs/README.md`.
- Git boundary: PR #75 already releases the current `dev` branch to `main`. Do not add homepage
  implementation commits to that release. When implementation is explicitly requested, create a
  dedicated feature branch from the then-current `dev` before editing production files.
- Phase boundary: the owner explicitly superseded the earlier per-phase stop on 2026-08-29 and
  requested all remaining phases in one pass, followed by one complete review.

## 1. Feature summary

Replace the current static welcome heading and five-card grid with a large, Arabic-first,
interactive product homepage that explains and launches the real research workflow:

`ayah -> word -> phrase/context -> selection/linking -> Abwab`

The page must help Quran research administrators, supervisors, and teachers resume focused work,
understand the relationship between the implemented tools, and enter the relevant route without
turning the authenticated dashboard into a decorative marketing page.

## 2. Confirmed owner decisions

1. The homepage is a long, multi-section page.
2. The hero contains this exact owner-approved Quran text:

   `﴿ وَنَزَّلْنَا عَلَيْكَ الْكِتَابَ تِبْيَانًا لِكُلِّ شَيْءٍ ﴾`

3. Sections reveal and change state during scrolling, but the Quran text itself never animates,
   fragments, morphs, or participates in parallax.
4. The visual concept is **"From the ayah to the door"** (`من الآية إلى الباب`).
5. The approved light hero background source is currently local at:

   `/home/mohamed/Downloads/ChatGPT Image Aug 29, 2026, 06_47_46 PM.png`

   Source dimensions: `1915 x 821`, RGB PNG, approximately 2 MB.
6. The image is decorative only. The ayah and all product copy remain semantic HTML.
7. The homepage highlights only implemented product surfaces. Top-level placeholder routes must
   not receive primary promotional sections or claims.
8. The owner removed the app name, version, environment, loading, error, and retry strip from the
   homepage entirely. The dashboard page no longer requests app-info data.

## 3. Primary user action

The primary action is to enter or resume a real Quran research workflow. The hero gives two equal
entry choices:

- `افتح قارئ المصحف` -> resume-aware Mushaf destination rooted at `/dashboard/mushaf`.
- `ابدأ البحث في القرآن` -> `/dashboard/words/phrases/repetitions`.

The secondary action is `تعرّف على مسار العمل`, which scrolls to the workflow rail without changing
the route.

## 4. Design direction

### Physical scene

An Arabic-speaking Quran researcher works for hours at a large desktop display in calm daytime
office light. The interface must remain legible, trustworthy, and quiet during long review
sessions. This forces a warm light-first composition rather than a cinematic dark landing page.

### Color strategy

- Strategy: restrained product palette.
- Reuse the existing warm page/surface/ink tokens and primary green.
- The hero image supplies texture, not a second palette.
- Use the existing dark-theme tokens. Do not introduce a permanent homepage-only theme system.
- No gradient text, glass surfaces, decorative gold, or saturated inactive controls.

### Typography

- UI copy and controls: existing `--qd-font-ui` (`IBM Plex Sans Arabic`).
- Section headings: existing `--qd-font-naskh` (`Amiri`).
- Hero ayah: existing protected Quran font path via `--qd-font-quran`.
- The semantic page `h1` is a short product identity such as `مساحة العمل القرآنية`.
- The ayah is a `blockquote`/paragraph, not the page heading and not raster text.
- Body copy remains within the existing prose measure.

### Anti-goals

- No identical icon-card grid.
- No marketing metrics or unimplemented capability claims.
- No scroll hijacking, mandatory snapping, horizontal page navigation, or page-load choreography.
- No generated Arabic text inside imagery.
- No reuse of real Quran verses as decorative preview filler.
- No single giant page component containing every section and interaction.
- No new animation library or design-system dependency.

## 5. Information architecture and final Arabic copy

### Section 0: Hero, "The Book is the starting point"

Content:

- Small semantic `h1`: `مساحة العمل القرآنية`.
- Hero ayah, exact approved text:
  `﴿ وَنَزَّلْنَا عَلَيْكَ الْكِتَابَ تِبْيَانًا لِكُلِّ شَيْءٍ ﴾`
- Supporting sentence:
  `اقرأ الآية في موضعها، تتبّع ألفاظها وسياقاتها، ثم نظّم نتائج البحث داخل أبواب المنهج.`
- Primary actions:
  - `افتح قارئ المصحف`
  - `ابدأ البحث في القرآن`
- In-page action: `تعرّف على مسار العمل`.

Behavior:

- Target visual height: approximately `80svh` after accounting for the existing navbar.
- Render immediately without entrance choreography.
- The approved image fills the hero as an absolutely positioned decorative `<picture>`.
- Center and center-inline-end remain the protected text-safe zone.
- A warm overlay owns text contrast; do not lower the entire hero content opacity.

### Section 1: Resume strip

Title: `تابع من حيث توقفت`

Purpose:

- Make the homepage useful on repeated visits, not only explanatory on the first visit.
- Reuse `NavigationResumeService.targetFor(...)` for known implemented destinations.
- Do not create a second session-storage contract.

Destinations:

- `المصحف`
- `البحث في القرآن`
- `الكلمات والجذور`
- `الأبواب`

If no stored target exists, the existing service naturally returns the route default. The UI does
not need to expose whether a target came from history.

### Section 2: Sticky workflow rail

Visible labels:

`اقرأ في المصحف ← افحص الكلمة والعبارة ← حدّد واربط ← نظّم داخل الأبواب`

Purpose:

- Explain the overall workflow once.
- Show the currently visible major section.
- Allow keyboard/pointer activation to scroll to a section.

The rail is in-page navigation, not application route navigation. Use stable section IDs and
`aria-current="step"` for the active item.

### Section 3: Mushaf study

Heading: `اختر الآية، ثم افتح سياق دراستها`

Copy:

`انتقل بين صفحات المصحف، اختر آية أو كلمة، وافتح التحليل، الأبواب، التفاسير والترجمات، المتشابهات، والآيات القريبة دون فقد موضعك.`

Preview:

- A semantic HTML/CSS composition inspired by the existing Mushaf and selected-ayah panels.
- Preview state labels must come from implemented concepts only.
- Do not show invented Quran text or fake counts.
- The preview may demonstrate these states without calling APIs:
  - `التحليل`
  - `الأبواب`
  - `التفاسير والترجمات`
  - `المتشابهات`
- Pointer and keyboard activation change the preview state in place.
- Fixed preview block size prevents layout movement.

CTA: `افتح المصحف` -> resume-aware Mushaf target.

### Section 4: Word structure

Heading: `من الجذر إلى الكلمة، ومن الكلمة إلى مواضعها`

Copy:

`تتبّع البناء الصرفي من الجذر إلى الصيغة المعجمية، ثم الأصل الصرفي والكلمة، أو افحص الكلمات بحسب نوعها النحوي.`

Interactive sequence:

`الجذر ← الصيغة المعجمية ← الأصل الصرفي ← الكلمة`

Independent entry: `أنواع الكلمات`

Routes:

- `نظرة عامة` -> `/dashboard/words`
- `الكلمات الفريدة` -> `/dashboard/words/unique/tashkeel`
- `الجذور` -> `/dashboard/words/roots`
- `الصيغ المعجمية` -> `/dashboard/words/lemmas`
- `الأصول الصرفية` -> `/dashboard/words/stems`
- `أنواع الكلمات` -> `/dashboard/words/types`

Use the existing route-path helpers instead of duplicating path strings.

### Section 5: Phrase context

Heading: `ابحث في العبارة، لا في الكلمة وحدها`

Copy:

`استعرض العبارات المتكررة، كوّن سياقًا من كلمات سابقة ولاحقة، وقارن المواضع المتشابهة مع الحفاظ على حدود الكلمات الأصلية.`

Tabs and routes:

- `التكرارات` -> `/dashboard/words/phrases/repetitions`
- `البحث اليدوي` -> `/dashboard/words/phrases/context`
- `المتشابهات` -> `/dashboard/words/phrases/similarity`

Preview rules:

- Tabs change one fixed-size preview surface.
- Use neutral phrase tokens/blocks and implemented Arabic labels, not fabricated Quran excerpts.
- Each tab has one focused explanation and one route CTA.
- Major route state remains represented by the existing child routes; homepage preview selection is
  local state because it is only a demonstrative control inside `/dashboard`.

### Section 6: Linking flow

Heading: `اجمع المواضع، ثم حدّد شكل العلاقة`

Copy:

`حدّد الآيات والكلمات المطلوبة، اربط كل آية بصورة مستقلة أو كوحدة واحدة، أو أضف المصادر إلى مساحة الربط لمراجعتها قبل التنفيذ.`

Visual flow:

`مصادر مختارة ← مساحة الربط ← مراجعة الآيات والكلمات ← تنفيذ الربط`

Rules:

- Explain direct linking and workspace linking without performing mutations on the homepage.
- Do not add a route to the linking workspace because it is not a standalone route.
- CTAs:
  - `ابدأ من المصحف`
  - `ابدأ من متشابهات العبارات`

### Section 7: Abwab structure

Heading: `حوّل نتائج البحث إلى بناء واضح`

Copy:

`نظّم أبواب المنهج داخل شجرة مترابطة، راجع المصادر والعلاقات والمواضع، واستخدم القوالب عندما تحتاج إلى هيكل متكرر.`

Preview:

- Read-only conceptual tree with one expandable branch.
- Generic structural labels are allowed; do not copy curated production door content into static
  frontend copy.
- Preview interactions never call write endpoints.

CTAs:

- `افتح الأبواب` -> `/abwab`
- `عرض القوالب` -> `/abwab/templates`

### Section 8: Operational ending

Heading: `اختر نقطة البداية`

Entry rows:

1. `قراءة آية ودراسة سياقها`
2. `البحث عن كلمة أو عبارة`
3. `مراجعة بناء المنهج`

The section ends after the three entry rows. It contains no app-info metadata strip.

## 6. Component and file architecture

The routeable smart page remains:

`Frontend/quran-dashboard-ui/src/app/features/dashboard/pages/dashboard-home/`

It becomes a shell/orchestrator only. Keep its TypeScript, HTML, and SCSS below the frontend soft
review thresholds by composing feature-owned children.

Proposed feature-owned structure, created only as each phase needs it:

```text
Frontend/quran-dashboard-ui/src/app/features/dashboard/
  pages/dashboard-home/
    dashboard-home.component.ts
    dashboard-home.component.html
    dashboard-home.component.scss
  components/dashboard-hero/
    dashboard-hero.component.ts
    dashboard-hero.component.html
    dashboard-hero.component.scss
  components/dashboard-resume-strip/
    dashboard-resume-strip.component.ts
    dashboard-resume-strip.component.html
    dashboard-resume-strip.component.scss
  components/dashboard-workflow-rail/
    dashboard-workflow-rail.component.ts
    dashboard-workflow-rail.component.html
    dashboard-workflow-rail.component.scss
  components/dashboard-mushaf-preview/
  components/dashboard-word-structure/
  components/dashboard-phrase-preview/
  components/dashboard-linking-flow/
  components/dashboard-abwab-preview/
  components/dashboard-entry-section/
  directives/dashboard-section-observer.directive.ts
  models/dashboard-home.models.ts
  models/dashboard-home.content.ts
```

Responsibilities:

- `dashboard-home.component`: active section signal, section registration, resume target mapping,
  and composition only.
- `dashboard-home.content`: exact approved copy, stable section keys, and route-helper results.
- `dashboard-section-observer`: feature-owned IntersectionObserver adapter with a synchronous
  fallback when IntersectionObserver is unavailable.
- Child components: local preview state and rendering only; no backend orchestration.
- `SystemApi`: unchanged unless a real missing operational need is discovered. Do not add a new
  dashboard endpoint for static homepage content.
- `NavigationResumeService`: reuse its public target resolution; do not duplicate persistence.

Do not promote homepage-only components into `shared/` and do not create new routes for preview
sections.

## 7. Hero asset pipeline

The original PNG is an accepted source asset, not the production delivery format.

Implementation steps:

1. Copy the source into a temporary workspace location for conversion; do not commit the 2 MB PNG
   as the shipped browser asset unless a later review explicitly requires source retention.
2. Produce responsive `AVIF` and `WebP` derivatives near `1280px` and `1915px` widths.
3. Store production assets under:

   `Frontend/quran-dashboard-ui/public/assets/dashboard/hero/`

4. Use `<picture>` so the browser selects AVIF, WebP, or the smallest accepted fallback.
5. Set explicit intrinsic width/height, `decoding="async"`, and appropriate hero/LCP loading
   priority to prevent layout shift.
6. Decorative image requirements: `alt=""`, not focusable, and hidden from accessibility APIs.
7. Desktop uses `object-fit: cover` with a centered safe zone. Compact layouts intentionally crop
   the edge network and retain the quiet central paper texture.
8. Target budgets:
   - largest production hero asset: <= 350 KB where visual quality remains acceptable;
   - compact derivative: <= 180 KB where visual quality remains acceptable.
9. Do not apply the previously suggested 8-12% image opacity. The approved image is already quiet.
   Use the full image with a subtle solid/tinted overlay for contrast.

Dark theme decision:

- Phase 1 uses the same source image behind an opaque dark-theme token overlay, with image opacity
  and saturation reduced on the image layer only.
- Do not filter the hero text or controls.
- Browser review is a hard phase gate. If the texture becomes muddy or the edge motifs disappear,
  stop and request a dark variant instead of accumulating CSS filters.

## 8. Motion and scroll contract

Motion must explain state and hierarchy, not decorate the page.

### Allowed motion

- Hero: no entrance sequence. Decorative image may have a maximum 6px scroll-relative shift on
  wide pointer devices only.
- Section reveal: opacity plus `translateY(12px)`, `200-240ms`, exponential/quint ease-out.
- Workflow rail: active state changes when a section crosses the observer band.
- Preview state changes: opacity/transform only, with a fixed content block size.
- Linking flow: advance one semantic stage when its subregion becomes active.
- Abwab preview: expand one read-only branch after the section becomes active.

### Forbidden motion

- Animating the ayah, individual Quran words, layout dimensions, sticky positions, or scroll
  position without direct user intent.
- Bounce, elastic easing, particles, continuous autoplay, or auto-rotating preview tabs.
- Scroll-jacking or mandatory snap points.

### Reduced motion

Under `prefers-reduced-motion: reduce`:

- all sections render visible immediately;
- smooth scrolling becomes immediate scrolling;
- parallax and staged flow transitions are disabled;
- previews remain manually interactive;
- no information is hidden behind motion completion.

## 9. Responsive and accessibility contract

### Wide

- Use asymmetric split compositions, with explanatory copy on the RTL start side and previews on
  the opposite side.
- Workflow rail may remain sticky below the existing navbar using the existing navbar size token.

### Medium

- Reduce preview complexity and column gap while preserving the two-region relationship.
- Do not shrink the ayah below a comfortable reading size.

### Compact

- Stack copy before preview in DOM/reading order.
- Hero remains text-first and may use a shorter visual height.
- Workflow rail becomes a native horizontally scrollable list or a wrapped list; no custom
  scrollbar and no horizontal page overflow.
- Disable decorative parallax.
- Preview controls remain at least the existing minimum hit-target size.

### Accessibility

- Exactly one page `h1`; section titles are ordered `h2` headings.
- Quran content has explicit RTL direction and uses the protected Quran font path.
- In-page workflow controls are native buttons/anchors with clear accessible names.
- Active workflow state uses `aria-current="step"` and never color alone.
- All interactive previews support keyboard focus and activation.
- Focus order follows DOM order; no visual reordering that contradicts reading order.
- Decorative graph lines and hero imagery are excluded from the accessibility tree.
- Maintain WCAG 2.1 AA contrast in light and dark themes.
- Loading and error states retain status/live-region semantics from the current page.

## 10. Implementation phases

### Phase 1: Foundation, content contract, asset, and hero

Ownership:

- Dashboard content/models.
- Dashboard page shell refactor.
- Hero component and production hero assets.
- Existing app-info behavior retained, temporarily rendered below the hero until Phase 5 places it
  in the final section.

Tasks:

1. Create the dedicated implementation branch when explicitly requested.
2. Add typed section keys, exact Arabic content, and route-helper-backed destinations.
3. Convert and add responsive hero assets within the stated budgets.
4. Refactor the page to OnPush/signals where useful without changing `SystemApi` behavior.
5. Implement the semantic hero and both route CTAs.
6. Implement the light and initial dark overlay treatments.
7. Keep the hero immediately visible; add no reveal directive yet.

Acceptance:

- Exact ayah text renders as HTML with no image text.
- Both CTAs reach the intended implemented routes.
- Loading/success/error app-info states still work.
- Light and dark hero text is readable.
- No horizontal overflow at compact, medium, or wide widths.
- Asset budgets and intrinsic sizing are verified.

Stop for review before Phase 2.

### Phase 2: Resume strip and workflow navigation

Ownership:

- Resume strip component.
- Workflow rail component.
- Section observer directive and active-section state in the page shell.

Tasks:

1. Reuse `NavigationResumeService` for Mushaf, Quran search, Words, and Abwab targets.
2. Add stable IDs for every major section.
3. Implement observer-based active section tracking.
4. Implement in-page navigation with reduced-motion-aware scrolling.
5. Make sticky behavior responsive and compatible with the existing navbar.

Acceptance:

- Stored navigation resumes when available and defaults safely when absent.
- Workflow state updates while scrolling and when activated by keyboard.
- `aria-current` and focus styles are correct.
- Reduced motion disables smooth scrolling.
- No separate persistence key or storage format is introduced.

Stop for review before Phase 3.

### Phase 3: Mushaf and word-structure sections

Ownership:

- Mushaf preview component.
- Word-structure component.

Tasks:

1. Build a code-native Mushaf/study preview using existing visual vocabulary.
2. Add manual preview-state controls for the implemented study concepts.
3. Build the connected word-structure sequence and route actions.
4. Use route helpers and existing navigation definitions rather than duplicated strings.
5. Keep all preview copy data-free and avoid invented Quran samples/counts.

Acceptance:

- Every CTA reaches an implemented destination.
- Preview interactions are keyboard operable and fixed-height.
- No Quran source text is invented or copied into decorative preview data.
- The two sections remain understandable without motion.

Stop for review before Phase 4.

### Phase 4: Phrase search, linking, and Abwab sections

Ownership:

- Phrase preview component.
- Linking flow component.
- Abwab preview component.

Tasks:

1. Implement the three phrase-search preview tabs and their route CTAs.
2. Implement the direct/workspace linking explanation as a read-only flow.
3. Implement the read-only conceptual Abwab tree preview.
4. Keep preview state local because it is illustrative and not a primary route state.
5. Do not add API calls, write actions, or a fake linking-workspace route.

Acceptance:

- Search tabs route to repetitions, context, and similarity correctly.
- Linking explanation matches the implemented independent/group selection behavior.
- Abwab preview performs no mutation and uses no production curation data.
- Preview block sizes do not jump when state changes.

Stop for review before Phase 5.

### Phase 5: Motion, operational ending, and visual hardening

Ownership:

- Reveal/scroll state integration.
- Entry section and relocated app metadata.
- Section-level visual hardening only.

Tasks:

1. Add section reveal behavior after static content is accepted.
2. Add state-conveying linking and Abwab transitions.
3. Add the final entry rows without an app-info metadata strip.
4. Remove the dashboard app-info request and its former loading/error/retry UI.
5. Complete reduced-motion, compact, medium, wide, light, and dark treatments.
6. Reassess every component against frontend file-size thresholds; split before any hard threshold.

Acceptance:

- No decorative or continuous animation remains.
- The ayah never animates.
- Reduced-motion mode loses no content or controls.
- Dark hero treatment passes browser review or the phase stops for a dark asset.
- No app-info request or metadata strip remains on the homepage.
- Page shell and component files remain below their review thresholds.

Stop for review before Phase 6.

### Phase 6: Verification and engineering review

Run these frontend commands independently and in this order:

```bash
cd Frontend/quran-dashboard-ui
npm run check:no-unit-specs
npm run typecheck:app
npm run build:verify
```

Then perform browser verification against the running app:

1. Light theme at approximately 1440px wide.
2. Dark theme at approximately 1440px wide.
3. Medium layout near the project medium breakpoint.
4. Compact layout near 390px wide.
5. Reduced-motion emulation.
6. Keyboard-only traversal through hero CTAs, resume targets, workflow rail, preview tabs, and final
   entry rows.
7. Route checks for Mushaf, Words, all three Phrase Search pages, Abwab, and Abwab templates.
8. Slow-network check for the hero image.
9. Confirm no layout shift when the hero image and fonts finish loading.

If the local Playwright prerequisites are already available, run the existing shell journey as a
non-regression check:

```bash
npx playwright test e2e/shell-nav.e2e.ts --project=default
```

Do not create a new E2E file or frontend unit spec.

Request a final engineering review after all checks pass. Only after that review passes:

1. apply any accepted durable contract updates if genuinely required;
2. delete `docs/feature-042-interactive-dashboard-home/` in a pure cleanup commit;
3. proceed to the user-requested commit/PR workflow.

## 11. Testing Decision

**No new automated tests.**

Reason:

- This is a visual/content/navigation composition change and does not introduce a new security,
  authorization, Quran data, write, transaction, or business invariant.
- The required protection is the independent frontend verification commands plus targeted browser
  inspection across themes, motion preferences, breakpoints, and routes.
- The existing `shell-nav.e2e.ts` journey already protects application-shell route reachability and
  should remain unchanged unless implementation reveals a real protected-behavior change.
- Creating a new Playwright file or any `*.spec.ts` is outside the approved Testing Decision.

## 12. Phase review checklist

Every phase report must include:

1. exact files added/modified;
2. implemented scope and explicitly deferred scope;
3. screenshots at the phase-relevant widths/themes;
4. verification commands run and their exit status;
5. browser observations, including reduced motion when motion exists;
6. file line counts for any component TS/HTML/SCSS approaching a soft threshold;
7. confirmation that Quran text, routes, permissions, and data contracts were not changed outside
   the approved scope;
8. known limitations or skipped checks.

The lower-capability implementer must not begin the next phase until the phase review is explicitly
accepted.

## 13. Risks and mitigations

### Risk: the homepage becomes a marketing page

Mitigation: retain resume actions, implemented route CTAs, and calm product copy. Remove any
section that cannot launch or explain a real workflow.

### Risk: the hero image harms LCP or causes layout shift

Mitigation: responsive AVIF/WebP derivatives, explicit dimensions, correct loading priority, asset
budgets, and browser slow-network verification.

### Risk: motion distracts from research work

Mitigation: add motion only after static review, use observer-triggered state changes, cap timings,
and enforce reduced-motion equivalence.

### Risk: the approved light image fails in dark theme

Mitigation: hard browser gate. Request a dark variant rather than stacking uncontrolled filters.

### Risk: Quran text is treated as decoration

Mitigation: semantic HTML, protected font path, exact approved string, no image text, no word-level
animation, and explicit Quran-safety review.

### Risk: one dashboard component exceeds ownership thresholds

Mitigation: keep the route page as a shell and split by major visual responsibility before the
soft/hard thresholds.

### Risk: the homepage advertises placeholder routes

Mitigation: primary sections and CTAs are limited to Mushaf, Words, Phrase Search, Linking context,
and Abwab. Placeholder library routes remain outside the homepage narrative.

## 14. Implementation references to load when their phase starts

- `/home/mohamed/.agents/skills/impeccable/reference/layout.md`
- `/home/mohamed/.agents/skills/impeccable/reference/typeset.md`
- `/home/mohamed/.agents/skills/impeccable/reference/animate.md`
- `/home/mohamed/.agents/skills/impeccable/reference/adapt.md`
- `/home/mohamed/.agents/skills/impeccable/reference/harden.md`
- `/home/mohamed/.agents/skills/impeccable/reference/optimize.md`
- `PRODUCT.md`
- `DESIGN.md`
- `Frontend/quran-dashboard-ui/AGENTS.md`
- `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md`
- `TESTING_CONSTITUTION.md`

## 15. Completion definition

The feature is complete only when:

1. the approved hero asset and exact ayah render correctly in both themes;
2. every accepted section is implemented and routes only to real product surfaces;
3. resume behavior reuses the existing navigation contract;
4. scroll interactions are purposeful, stable, keyboard accessible, and reduced-motion safe;
5. compact, medium, and wide layouts pass browser review;
6. frontend verification commands pass independently;
7. the existing shell journey passes when its prerequisites are available, or the skip is reported;
8. the final engineering review has no unresolved finding;
9. this feature planning folder is deleted before merge.
