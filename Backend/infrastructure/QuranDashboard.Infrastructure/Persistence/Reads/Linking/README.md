# Abwab Linking read models

**Layer:** Infrastructure · read-only queries · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md`

## What this area does

Two read-only EF readers, one per Linking read boundary.

`EfLinkingSourceResolutionReader` turns a typed `LinkingSourceDescriptor` into the **complete** validated
ayah set for all six Abwab linking source families, in deterministic Quran order, with a canonical
`quranWordId` on every word. It backs `application/.../Linking/Queries/ResolveLinkingSource` and the
single `POST /api/linking/sources/resolve` endpoint. There is no paging, ever, and no writes happen
here.

`EfLinkingWorkspaceReader` loads the caller's own prepared workspace for `GET /api/linking/workspace`.
**It is strictly read-only in the strong sense** (spec FR-019, research R21): when the caller has no
`linking_workspaces` row it returns `LinkingWorkspaceProjection.Empty` — `workspaceVersion = null`, no
sources — and performs **zero inserts**. The row is created by the first mutation in
`../../Writes/Linking/`, never by a load. Do not add a lazy create here: a write-on-read breaks the
row-count proof that verifies this rule, and would write a row for every user who merely opens the page.
The reader is scoped to the `userId` its caller resolved from `AuthorizationState.UserId` and takes no
other user selector, so there is no shape in which it can read another user's workspace. The DTO itself
is built by the shared `../../Linking/LinkingWorkspaceProjection`, which the writer also uses to return
post-mutation state — one projection, so a read and a write can never describe the same workspace
differently.

## Key pieces

- `EfLinkingSourceResolutionReader.cs` — dispatch by descriptor kind, the shared `MaxResolvedAyahs`
  guard, matched-word grouping, and the `LinkingMatchedWordRow` shape. **Partial-split by family**;
  keep the split when adding to it.
- `EfLinkingSourceResolutionReader.Automatic.cs` — Root, Lemma, Stem.
- `EfLinkingSourceResolutionReader.UniqueWord.cs` — simple and tashkeel modes.
- `EfLinkingSourceResolutionReader.WordType.cs` — the two selection arms.
- `EfLinkingSourceResolutionReader.ManualMushaf.cs` — the manual family and its completeness proof.
- `LinkingAyahHydration.cs` — shared ayah-level hydration (verse key, Arabic surah name,
  `pageFrom`/`pageTo`) plus the word-level projection, delegating to `AyahWordHydration`.
- `EfLinkingWorkspaceReader.cs` — the workspace load. Two queries at most: the workspace row, then the
  shared projection. Implements `ILinkingWorkspaceReader`.

## Invariants / caveats (read before changing)

- **Ordering is a contract, not a nicety** (spec FR-006). Ayahs are ordered `(surah_number,
  ayah_number)` and words by `word_number`, always, because the Frontend CDK viewport computes item
  offsets from index — an unstable order corrupts the viewport rather than merely reshuffling rows.
  Ayah order comes from `LinkingAyahHydration`'s `ORDER BY`, word order from `AyahWordHydration`'s.
  Neither is inherited from the matched-word query: that query is `DISTINCT` and **EF is free to drop
  an `ORDER BY` that precedes a `Distinct`**, so match order is re-established in memory by
  `(WordNumber, QuranWordId)` in `GroupMatchedWordIds`. Do not "optimise" that back into SQL.
- **The query shape is bounded and independent of ayah count.** Every family resolves in 3–4
  commands: existence probe → matched words → ayah metadata → word hydration (manual swaps the
  existence probe for a completeness read). Ayah id sets travel as one `= ANY(...)` array, never one
  query per ayah. Measured on the canonical database, a 1,879-ayah root resolves in 4 commands.
- **`matchedQuranWordIds` is non-empty for the five automatic families and may be empty for manual**
  (spec FR-008). For automatic families the matched words *are* what selects the ayah, so an ayah with
  no match is never in the set. A manual ayah is the curator's chosen verse: it returns its complete
  canonical word list and contributes the ayah even with zero matched words.
- **Marker behaviour is per family, and every family mirrors its own explorer read.** Root, Lemma,
  Stem, and **Word Type** are marker-free; **Unique Word** and **Manual Mushaf** include ayah markers
  in the `words` list, flagged `isAyahMarker`. That split is not a preference — it is measured
  against the shipped explorers, which are the behaviour the Phase 9 Frontend cutover must not
  change:
  - Word Type is marker-free because **both** Word Type explorer ayah reads are:
    `EfWordTypesReader.GetAyahMatchesAsync` (word arm) and
    `EfWordTypesReader.GetGroupedAyahMatchesAsync` (dimension arm) both call
    `AyahWordHydration.ProjectAyahMatchesAsync` **without** `includeAyahMarkers`, taking its `false`
    default. Adding markers here would be user-visible at cutover — index-derived `renderPosition`
    values shift and per-ayah word counts grow by one — and no requirement asks for that. Both arms
    of this family share the single `HydrateMatchesAsync` call at the end of `ResolveWordTypeAsync`,
    so the rule cannot drift between them.
  - Unique Word keeps markers because its explorer genuinely does: `EfUniqueWordsReader` projects
    `IsAyahMarker` into its own `AyahWordForHighlightDto` word list (it does not use
    `AyahWordHydration` at all).
  - Manual Mushaf keeps markers because the completeness proof counts **non-marker** words against
    `quran_ayahs.words_count_real`, and the Frontend manual reader renders markers deliberately
    (`features/linking/data-access/manual-mushaf-ayah.reader.ts`) with `renderPosition = 0`.

  Markers are never *matched* words — every matched-word query filters `NOT is_ayah_marker` — so this
  flag changes only the `words` list, never which ayahs resolve. Measured on the canonical database:
  the Word Type dimension source `root 4 / noun` returns the same 1,879 ayahs and the same 2,851
  matched word ids either way; only the total word count moves, 38,651 → 36,772.
- **The Word Type family is asymmetric, and this mirrors the shipped Frontend exactly.** For a
  `word` selection the scope is **not** a filter: the row identity (`tashkeelWordId` + `contextCode` +
  its own case/tense/voice) already pins the occurrence set, and the Frontend's resolver deliberately
  omits scope from that request. For a `root`/`stem`/`lemma` selection the scope **is** a filter and
  narrows the set. Scope is nonetheless part of the identity on both arms, so two `word` sources
  browsed under different scopes are distinct sources that resolve to identical ayah sets. Applying
  scope to the word arm "for symmetry" would silently resolve a narrower set than the Frontend does.
- **The occurrence base is reused, never forked.** The dimension arm composes
  `EfWordTypesReader.BaseRowsSql` inside `WITH base AS (...)`, with `ToGroupedReadContext` and
  `BuildGroupedDetailParameters` supplying the context and parameters — the same three members the
  grouped explorer reads use. They were widened from `private` to `internal` for exactly this reason.
  The word arm reuses `EfWordTypesReader.MatchedMorphologyQuery`, the trickiest predicate in the
  explorer. Copying either into this folder would let the two drift apart silently.
- **Lemma and Stem resolve at segment grain, Root at head grain.** Lemma filters
  `quran_word_morphology_segments.lemma_id`; Stem additionally requires `kind = 'STEM'`; Root filters
  `quran_word_morphology.root_id`. `typeCode` narrows the segment `pos` for Lemma and Stem only, and
  is normalized (trim, blank → absent) exactly as the explorers normalize it. This mirrors the
  existing explorer readers so the Phase 9 Frontend cutover cannot change what a source resolves to.
- **`pageFrom`/`pageTo` come from `quran_ayahs`, not from `MIN`/`MAX` over words.** The columns are
  computed once at import. In the current canonical dataset every ayah satisfies
  `page_from = page_to` — no ayah's words straddle a Mushaf page, including the 128-word 2:282 — so
  the span is degenerate today, but reading both columns keeps the DTO honest if the data ever
  carries a real span. Do not substitute `AyahWordHydration`'s single page number: it is `pageFrom`
  only and would silently drop `pageTo`.
- **The manual completeness proof blocks the whole resolution, naming the verse** (plan D8, research
  R9). Per requested verse: the ayah exists and its `verse_key` matches the requested spelling; the
  non-marker `word_number`s are contiguous `1..N`; `N == quran_ayahs.words_count_real`; every
  non-marker `location` is unique with a matching `(surah, ayah)` prefix. The proof itself is pure and
  lives in `Application.Abstractions/Linking/LinkingManualAyahCompleteness.cs`; this folder only feeds
  it rows. A verse is looked up **by its raw `verse_key` string**, so a leading-zero spelling such as
  `002:255` finds nothing and fails the proof by design — that spelling is a distinct identity per
  `contracts/source-identity.md`, and silently canonicalizing it here would break the byte-exact
  identity contract on one side only. There is **no Mushaf page assembly anywhere** in this area.
- **`location` is not carried on `AyahWordRow`.** The proof needs it, so the manual path issues one
  extra bounded projection over `quran_words` rather than widening the row type that four explorer
  readers share.
- **Validation happens before this reader runs, and identity is computed after resolution.**
  Byte-exact Frontend identity parity is guaranteed only for *valid* descriptors, so the API body gate
  and the handler's `TryValidate` both run first. The reader assumes a validated descriptor.
- **This reader is wrapped, and is no longer what the application resolves.**
  `ILinkingSourceResolutionReader` is bound to `CachedLinkingSourceResolutionReader`
  (`../../../Caching/Linking/`), which decorates this class; `EfLinkingSourceResolutionReader` is
  registered as a concrete type and injected into that decorator (F13). **A warm repeat therefore
  never reaches this reader at all** — measured zero EF commands on the second identical
  resolution. Two consequences when changing anything here:
  - Anything that must appear in every response has to come from this reader's output, because the
    cache reconstructs its response from what this reader returned (ordered ayah ids, per-ayah
    ordered `quranWordId`s, `matchedQuranWordIds`, and the ayah/word text captured alongside). A
    new field that the compact form does not carry would be present cold and missing warm.
  - `resolvedAtUtc` is the one value the decorator deliberately does **not** reuse: it is
    re-stamped on every response so a cached response is indistinguishable from a cold one.
    Nothing else here holds state or reads the clock.

## Related

- Reader abstractions, DTOs, limits, identity, and the completeness proof:
  `../../../../../application/QuranDashboard.Application.Abstractions/Linking/README.md`.
- Descriptor value objects: `../../../../../domain/QuranDashboard.Domain/Linking/README.md`.
- Handler: `application/QuranDashboard.Application/Linking/Queries/ResolveLinkingSource/`.
- Shared word hydration and the Word Types occurrence base: `../Quran/Words/README.md`.
- Wire contract: `specs/001-abwab-linking-backend/contracts/linking-sources-api.md`.
- The cache decorator wrapping this reader, and its three deliberate divergences:
  `../../../Caching/Linking/README.md`.
