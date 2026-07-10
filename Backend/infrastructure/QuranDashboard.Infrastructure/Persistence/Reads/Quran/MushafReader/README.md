# Mushaf reader read models

**Layer:** Infrastructure · read-only queries · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md`

## What this area does

Read-only EF readers behind Mushaf reader endpoints: page rendering, ayah study context,
similar ayahs, mutashabihat groups, study-source catalogs, and word analysis. No writes happen here.

## Key pieces

- `EfMushafPageReader.cs` — page lines, words, surah/page context, and page markers.
- `EfAyahStudyReader.cs` — ayah core data plus resolved tafsir/translation/full-i3rab payloads
  and similarity-summary counts.
- `EfAyahSimilaritiesReader.cs` — merges outgoing/incoming similarity links into one flat list.
- `EfAyahMutashabihatReader.cs` — loads grouped mutashabihat occurrences for one ayah.
- `EfMushafSurahCatalogReader.cs`, `EfMushafStudySourceCatalogReader.cs` — catalogs for page jumps
  and selectable study sources.
- `EfWordAnalysisReader.cs` — word occurrence identity, unique-word counts, morphology, and
  ordered rendered segments.

## Invariants / caveats (read before changing)

- **Read-only + `AsNoTracking`** semantics throughout; these readers must not mutate state.
- **Ordering is part of the contract.** Pages order lines by `LineNumber` and words by
  `LineNumber` then `LineWordOrder`; markers sort by line, marker type, then marker number.
- **Similarity merging is directional but deduplicated.** Outgoing and incoming links collapse to
  one related ayah row, then sort by score descending and Mushaf order tie-breakers.
- **Mutashabihat grouping is deterministic.** Groups order by `SourceGroupId`; occurrences order by
  surah, ayah, then `WordFrom`; phrase text is derived from ordered non-marker words.
- **Word identity still keys on clean imlaei-simple** via the morphology/display tables; Uthmani
  text stays display-only.
- Response shapes must stay aligned with
  `../../../../../application/QuranDashboard.Application.Abstractions/Quran/MushafReader/Responses/`
  and the API envelope returned by `../../../../../api/QuranDashboard.Api/Controllers/README.md`.

## Related

- Sibling words read models: `../Words/README.md`
- API controllers: `../../../../../api/QuranDashboard.Api/Controllers/README.md`
- Write-side imports: `../../../DataPipelines/Quran/README.md`
