# Phase 0 Research: Quran Lemmas & Stems Explorer

All decisions are resolved from the Feature 016 capability/linking report, combined implementation
plan, implemented Feature 014/015 patterns, and current repository architecture. No unresolved
clarification marker remains.

Primary sources:

- `docs/feature-016-lemmas-stems-explorer/feature-016-lemmas-stems-explorer-capability-linking-report.md`
- `docs/feature-016-lemmas-stems-explorer/feature-016-lemmas-stems-explorer-combined-implementation-plan.md`
- `specs/015-roots-explorer/` and implemented Feature 015 code
- implemented Feature 014 Unique Words code
- Backend/Frontend architecture guides and `PRODUCT.md` / `DESIGN.md`

## D1 — Existing-data feasibility

- **Decision**: Implement both explorers entirely as read-only projections over existing morphology,
  word, ayah, surah, root, lemma, stem, and POS tables.
- **Rationale**: All 4,793 lemmas and 12,108 stems have Arabic display values and connect through
  morphology to readable Quran words. Foreign-key and segment connectivity checks found no orphaned
  records relevant to the explorers.
- **Alternatives considered**: New importer or denormalized explorer tables — rejected because the
  required data and relationships already exist and are queryable.

## D2 — Separate resource contracts

- **Decision**: Use explicit Lemmas and Stems readers, handlers, controllers, DTOs, frontend models,
  and routes while reusing proven shared primitives.
- **Rationale**: Lemmas and stems have different identities, table columns, nullability, related-item
  semantics, and detail tabs. Explicit contracts are easier to test and safer for downstream
  implementation than a generic morphology discriminator.
- **Alternatives considered**: One `/api/words/morphology/{kind}` endpoint and generic DTO hierarchy —
  rejected as premature abstraction and a source of conditional complexity.

## D3 — Canonical identities and search

- **Decision**: Numeric primary IDs are canonical for lemma, stem, root, and unique-word selection.
  V1 catalogue search is normalized contains matching over Arabic display text.
- **Rationale**: Display text is content, not identity. Lemma Buckwalter has nine duplicate values;
  text-based deep links are therefore unsafe even if Arabic display text is currently unique.
- **Alternatives considered**: Arabic display text, Buckwalter, or normalized text in canonical URLs —
  rejected for stability and uniqueness reasons. Separate Buckwalter search mode is not required by
  the approved v1 spec.

## D4 — Whole-summary caching

- **Decision**: Compute the complete lemma summary list and stem summary list once, cache each bounded
  list, and apply normalized search, sort, and page over the cached result. Use separate `lemmas:` and
  `stems:` namespaces; never create cache entries keyed by raw search text.
- **Rationale**: Verified local aggregate probes were approximately 216.9 ms for 4,793 lemma rows and
  251.7 ms for 12,108 stem rows. Both result sets are bounded and runtime data is stable, making
  compute-once caching predictable and preventing repeated grouped aggregation.
- **Alternatives considered**: Re-aggregate in PostgreSQL for every search/page — viable fallback but
  unnecessary at this scale. Per-search cache keys — rejected as unbounded and privacy-unfriendly.

## D5 — Count semantics

- **Decision**:
  - occurrences = matching morphology row count;
  - ayahs = distinct linked ayah IDs;
  - surahs = distinct surah numbers;
  - simple/tashkeel words = distinct respective unique-word IDs;
  - lemma stems = distinct non-null stem IDs co-occurring with the lemma.
- **Rationale**: These definitions are observable, reconcile table counts with detail lists, and match
  Feature 014/015 study semantics.
- **Alternatives considered**: Precomputed `words_count` as the only authority — rejected as an
  implementation shortcut; the explorer contract is defined from the matching occurrence set and
  must be regression-tested against existing aggregates.

## D6 — Dominant part of speech

- **Decision**: Group matching occurrences by `head_pos`; order by occurrence count descending, then
  the earliest matching Quran word in Mushaf order (`surah_number`, `ayah_number`, `word_number`);
  use the first item as `DominantType`. Expose `OtherTypesCount` in list summaries and the full ordered
  distribution in selection summaries.
- **Rationale**: A lemma or stem can appear with more than one type. The rule is deterministic,
  evidence-based, and avoids inventing a single intrinsic type.
- **Alternatives considered**: First type only, unordered set, or comma-separated table text —
  rejected because they are misleading or too dense. New labels — rejected; existing controlled POS
  labels are authoritative.

## D7 — Stem summary lemma/root relationships

- **Decision**: For a stem table row, choose the dominant co-occurring lemma and root independently by
  occurrence count, then earliest Mushaf occurrence. Preserve null when no relationship exists.
- **Rationale**: 204 stems have no co-occurring lemma and 430 have no co-occurring root. A stem may
  also co-occur with multiple values, so a deterministic summary rule and null-safe UI are required.
- **Alternatives considered**: First database row, text order, or fabricated fallback relationship —
  rejected as nondeterministic or incorrect.

## D8 — Lemma root relationship

- **Decision**: Use `quran_lemmas.root_id` as the lemma table's root relationship. Do not infer a root
  from morphology when the owned-root link is null.
- **Rationale**: The product plan explicitly distinguishes lemma ownership from broader co-occurrence.
  The table needs one stable root meaning, and the owned relationship already supplies it.
- **Alternatives considered**: Dominant co-occurring root — rejected for the lemma summary because it
  would silently change the meaning of the existing ownership relationship.

## D9 — Detail paging and whole lists

- **Decision**: Paginate catalogue, word, and ayah reads. Load mentioned/missing surahs, lemma-related
  stems, stem-related lemmas, and type distributions as whole bounded lists.
- **Rationale**: Verified maxima are large for ayahs and moderate for words, while surahs are capped at
  114, lemma-related stems at 59, and stem-related lemmas at 10.
- **Alternatives considered**: Load every detail list whole — rejected for high-frequency ayahs and
  words. Paginate every tiny relationship list — rejected as unnecessary UX and contract complexity.

## D10 — Exact ayah highlighting and Mushaf navigation

- **Decision**: Reuse Feature 014/015 highlighting payloads: page distinct ayahs, return all readable
  ordered words for each page plus exact `matchedQuranWordIds`, and link ayahs with `verseKey` and
  `pageNumber` to the existing Mushaf focus contract.
- **Rationale**: Word identity avoids false positives and Quran text mutation. Existing components and
  deep-link behavior already implement this safely.
- **Alternatives considered**: String replacement or server-generated highlighted HTML — rejected for
  Quranic data safety, accessibility, and correctness.

## D11 — Cross-page links

- **Decision**: Root, lemma, stem, unique-word, and ayah destinations are real anchors with
  `target="_blank"` and `rel="noopener noreferrer"`. Same-page search, sort, pagination, selection,
  tab, and sub-view changes update query state in the current tab.
- **Rationale**: Study links should remain inspectable/copyable and preserve the current research
  context. This extends the established Mushaf behavior.
- **Alternatives considered**: Router-only same-tab navigation for all destinations — rejected because
  it discards the current study context and conflicts with the locked requirement.

## D12 — Additive Mushaf DTO identities

- **Decision**: Add `Id` to `WordMorphologyLemma` and `WordMorphologyStem` in the backend response and
  corresponding frontend models; map existing loaded entity IDs. Do not change schema or display
  semantics.
- **Rationale**: The reader already loads the referenced lemma/stem. Stable IDs are the only missing
  contract needed for safe links.
- **Alternatives considered**: Look up explorer selections by text in the frontend — rejected because
  text is not canonical identity.

## D13 — Backend architecture and outcomes

- **Decision**: Mirror implemented Roots: focused read interfaces in Application.Abstractions;
  validation/logging handlers and discriminated outcomes in Application; `AsNoTracking` EF readers,
  cache decorators, and DI in Infrastructure; thin controllers returning `ApiResponse<T>`.
- **Rationale**: This is the established project pattern and satisfies Clean Architecture and API
  rules.
- **Alternatives considered**: Direct DbContext in handlers/controllers or one service containing all
  orchestration — rejected by project architecture.

## D14 — Frontend structure and file-size control

- **Decision**: Two thin routeable pages, separate resource API services, separate list/detail facades,
  pure URL helpers, and presentation components. Reuse shared Roots components where contracts fit.
  Split detail loader/update helpers if a facade approaches the 400-line soft threshold.
- **Rationale**: Two resources plus URL restoration and multiple lazy detail views can easily produce
  oversized generated components. Explicit responsibilities and the implemented Roots split prevent
  that failure mode.
- **Alternatives considered**: One combined page/facade with a resource mode — rejected as conditional
  complexity; duplicate all shared components — rejected where existing components are already
  resource-neutral.

## D15 — No migration, index, or new dependency

- **Decision**: Add no migration, schema change, index, package, logging vendor, or design token.
- **Rationale**: Existing indexes cover relationship lookups; whole-table summary aggregation naturally
  uses bounded scans. The capability report found no blocker. Existing backend/frontend dependencies
  cover the implementation.
- **Alternatives considered**: Speculative indexes and denormalized summary columns — rejected until
  measured production evidence demonstrates a need and separate approval is obtained.

## D16 — Testing strategy

- **Decision**: Use Testcontainers PostgreSQL for count, tie-break, null-relationship, pagination,
  highlighting, cache, and query-bound tests. Add handler/controller validation and logging-redaction
  coverage. Frontend tests cover URL parsing/building, list/detail orchestration, count mapping,
  anchors, Mushaf model links, responsive guards, and accessibility semantics.
- **Rationale**: Query correctness and aggregation semantics require real relational behavior; mocks
  are appropriate only at actual boundaries. Source-safe committed slices prevent invented Quran data.
- **Alternatives considered**: In-memory EF provider for aggregate correctness — rejected because its
  relational/query behavior differs from PostgreSQL. Snapshot-heavy UI tests — rejected in favor of
  behavior assertions.

## D17 — Search, selection, and page normalization

- **Decision**: Match the implemented Roots explorer behavior: search and sort changes reset only the
  catalogue page to 1 and preserve the selected identity and active detail state. Frontend malformed
  or non-positive catalogue/detail pages normalize to 1. Backend non-positive page/page-size inputs
  are validation errors. Valid positive pages beyond the available results return successful empty
  pages and remain represented in URL state. Unknown positive identities produce panel not-found
  state without disabling the catalogue.
- **Rationale**: Preserving selection avoids destroying a research context when refining the list,
  while explicit page rules prevent frontend/backend disagreement and make every edge case testable.
- **Alternatives considered**: Clear selection on search/sort — rejected because it diverges from the
  sibling Roots explorer. Clamp positive out-of-range pages to the last page — rejected because it
  requires a second normalization/navigation cycle and obscures the requested state.

## D18 — Catalogue first-render performance acceptance

- **Decision**: Verify SC-002 against production frontend and backend builds with the local API, warm
  application/cache state, no browser throttling, and 20 measured route openings per explorer. Start
  timing at navigation start and stop at the first successful catalogue-table render. At least 19 of
  20 openings per route must complete within 1,000 ms; record the environment and all timings in the
  quickstart completion evidence.
- **Rationale**: This defines “normal operating conditions” and the 95% threshold without adding a
  package or pretending a unit-test runtime measures browser rendering.
- **Alternatives considered**: Vitest duration assertions — rejected because DOM-test timing does not
  represent production navigation, API, and rendering. A new browser automation dependency —
  rejected because Feature 016 explicitly adds no package.

## Carried Risks and Mitigations

| Risk | Mitigation |
|---|---|
| N+1 ayah loading | Batch page ayah IDs, then batch words and match IDs; assert bounded command count. |
| Type or dominant-relationship tie drift | Centralize deterministic ordering and add explicit tie fixtures. |
| Null stem relationships treated as errors | Nullable DTO fields plus table/link tests for absent values. |
| List triggers every detail read | Separate list and detail facades; frontend test asserts no detail API calls on catalogue render. |
| Cache key explosion | Cache whole summaries and bounded identity/page reads only; no raw-search keys. |
| Lexical/Quran text leaks to logs | IDs/counts/booleans only; log-capture tests assert forbidden values absent. |
| Frontend generated files grow too large | Split loader/update/cache helpers at architecture thresholds. |
| Existing Mushaf consumers break | Additive DTO/model fields and focused regression tests. |
