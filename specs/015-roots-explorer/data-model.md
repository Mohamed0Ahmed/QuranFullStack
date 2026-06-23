# Phase 1 Data Model: Quran Roots Explorer

Read-only model over existing tables. No schema change, no migration, no writes. Backend identifiers
exist for selection/URL/navigation only and are **never displayed** in the UI.

## Existing source tables (read-only)

| Table | Used for |
|---|---|
| `quran_roots` | root id, `root_text`, `words_count` (= occurrences), `distinct_lemmas_count` (= lemmas, co-occurrence), `first_word_order_in_mushaf` (mushaf-order sort) |
| `quran_word_morphology` | join key `quran_word_id`, `root_id` (indexed), `lemma_id`, `stem_id` — the driving relation for all per-root reads |
| `quran_words` | `id` (highlight token), `ayah_id`, `surah_number`, `word_number`, `page_number`, `text_uthmani`, `is_ayah_marker`, `unique_simple_word_id`, `unique_tashkeel_word_id` |
| `quran_words_unique_simple` / `quran_words_unique_tashkeel` | display text + identity of the words sub-views; deep-link targets into Feature 014 |
| `quran_lemmas` | `id`, `lemma_text` (text for the lemmas tab; **count comes from morphology co-occurrence**, not this table's `root_id`) |
| `quran_stems` | `id`, `stem_text` (no `root_id`; stems derived via morphology) |
| `quran_ayahs` | `id`, `verse_key`, `surah_number`, `ayah_number` |
| `quran_surahs` | `surah_number`, `name_arabic` (Arabic surah name; mentioned/missing) |

**Driving relation** for every per-root read:
`quran_word_morphology m (m.root_id = X)` → `JOIN quran_words w ON w.id = m.quran_word_id`.
Morphology rows are one-per-readable-word, so ayah markers never enter the set.

## Conceptual entities

- **Root** — root text + 8 aggregate counts; the table row and panel header.
- **RootWord** — a distinct simple or tashkeel word identity among the root's words; carries display text, in-root occurrence count, and the unique-word id used to deep-link into Feature 014.
- **RootAyahMatch** — a verse containing the root's words; carries verse metadata, the ordered words for rendering, and the matched `quran_words.id` set for highlighting.
- **RootSurah** — a surah where the root appears (with in-surah occurrence count) or is missing.
- **RootLemma** — a lemma appearing among the root's words (co-occurrence); text + in-root occurrence count + lemma id (retained for future linking; not interactive now).
- **RootStem** — a stem appearing among the root's words; text + in-root occurrence count + stem id (retained; not interactive now).

## The eight counts (authoritative rules)

| Column (Arabic) | Rule | Source |
|---|---|---|
| المواضع | total occurrences of the root | `quran_roots.words_count` (verified == morphology `COUNT(*)`) |
| الآيات | distinct verses containing the root | `COUNT(DISTINCT w.ayah_id)` |
| السور | distinct surahs containing the root | `COUNT(DISTINCT w.surah_number)` |
| كلمات بدون تشكيل | distinct simple word identities | `COUNT(DISTINCT w.unique_simple_word_id)` |
| كلمات بالتشكيل | distinct tashkeel word identities | `COUNT(DISTINCT w.unique_tashkeel_word_id)` |
| الصيغ المعجمية | distinct lemmas among the root's words (**co-occurrence**) | `COUNT(DISTINCT m.lemma_id)` == `quran_roots.distinct_lemmas_count` |
| الأصول الصرفية | distinct stems among the root's words | `COUNT(DISTINCT m.stem_id)` |

**Invariant**: the الصيغ المعجمية column value equals the number of items in the root's lemmas tab.
Both use co-occurrence. The `quran_lemmas.root_id` ownership count is **not** used (differs for 49 roots).

## Read DTO shapes (see contracts for full signatures)

- `RootListItemDto { Id, RootText, OccurrencesCount, AyahsCount, SurahsCount, SimpleWordsCount, TashkeelWordsCount, LemmasCount, StemsCount, FirstVerseKey }`
- `RootSummaryDto { Id, RootText, …same counts…, FirstVerseKey }`
- `RootWordItemDto { UniqueWordId, Kind("simple"|"tashkeel"), DisplayTextUthmani, OccurrencesCount, FirstVerseKey }`
- `RootAyahMatchDto { AyahId, VerseKey, SurahNumber, SurahNameArabic, AyahNumber, PageNumber, MatchedQuranWordIds[], Words: AyahWordForHighlightDto[] }` (reuse F014 `AyahWordForHighlightDto { QuranWordId, WordNumber, TextUthmani, IsAyahMarker }`)
- `RootSurahsResponse { Id, RootText, SurahsCount, Surahs: RootSurahItemDto[] }`; `RootSurahItemDto { SurahNumber, NameArabic, OccurrencesInSurah }`
- `RootMissingSurahsResponse { Id, RootText, MissingSurahsCount, Surahs: MissingSurahItemDto[] }`; `MissingSurahItemDto { SurahNumber, NameArabic }`
- `RootLemmasResponse { Id, RootText, LemmasCount, Lemmas: RootLemmaItemDto[] }`; `RootLemmaItemDto { LemmaId, LemmaText, OccurrencesCount }`
- `RootStemsResponse { Id, RootText, StemsCount, Stems: RootStemItemDto[] }`; `RootStemItemDto { StemId, StemText, OccurrencesCount }`
- `PagedResult<T> { Page, PageSize, TotalCount, Items[] }` (existing shared shape)

## Pagination & wholeness

- Paginated: roots list; ayah matches; words sub-views.
- Whole (bounded): mentioned surahs and missing surahs (≤114); lemmas (≈≤22 worst); stems (≈≤84 worst).

## Validation rules (owned by Application handlers)

- `sort` ∈ {`mushaf-order`,`occurrences`,`alpha`}; empty → default `mushaf-order`; unknown → 400.
- `wordKind` ∈ {`simple`,`tashkeel`}; unknown → 400.
- `id` (root) must be a positive integer; otherwise 400; unknown id → 404 (controlled not-found).
- `page`/`pageSize` positive and bounded (max page size mirrors F014, 1000); out of range → 400.
- Readers return read DTOs only, `AsNoTracking`, no EF entities, no mutation, exclude ayah markers from occurrence/highlight data.

## Data-safety rules

- No writes, no migrations, no source-data mutation, no invented Quran text.
- Highlight by `quran_words.id` only (no string matching).
- No internal table/column names in API routes or user-facing messages.
- Backend identifiers never rendered in the UI.
