# Contract — Source File Shapes (exact input formats)

The exact on-disk shapes the readers must parse. Verified against the real files in the data report. **Do not invent or "fix" Quran text** — read bytes as-is.

## Word files (all four identical shape) — JSON object keyed by location

`mushaf/qpc-v4.json`, `words/uthmani.json`, `words/uthmani-simple.json`, `words/imlaei-simple.json` — each is a JSON **object** whose key == `location`:

```json
{
  "1:1:1": { "id": 1, "surah": "1", "ayah": "1", "word": "1", "location": "1:1:1", "text": "..." },
  "1:1:2": { "id": 2, "surah": "1", "ayah": "1", "word": "2", "location": "1:1:2", "text": "..." }
}
```

- `id`, `surah`, `ayah`, `word` come as values; `surah/ayah/word` are **strings** → parse to int.
- `text` is the only field that differs across the four files: glyph code (qpc-v4) vs the three readable forms.
- All four MUST have identical `id`/`surah`/`ayah`/`word` per `location` (0 mismatches) — this is `source-alignment`.
- DTO: `record WordRecordDto(int Id, int Surah, int Ayah, int Word, string Location, string Text);`

## Layout — `mushaf/qpc-v4-pages-layout.json`

```json
{
  "pagesCount": 604, "linesPerPage": 15, "fontName": "v4-tajweed",
  "pages": {
    "1": [
      { "pageNumber": 1, "lineNumber": 1, "lineType": "surah_name", "isCentered": true,  "surahNumber": 1,    "firstWordId": null, "lastWordId": null },
      { "pageNumber": 1, "lineNumber": 2, "lineType": "ayah",       "isCentered": true,  "surahNumber": null, "firstWordId": 1,    "lastWordId": 5 }
    ]
  }
}
```

- `pages` is an object keyed by page number (string) → array of line objects.
- `lineType` ∈ `ayah` | `surah_name` | `basmallah`.
- `firstWordId`/`lastWordId` are set only on `ayah` lines and reference word `id`.
- `surahNumber` is set only on `surah_name` lines.
- DTOs: `record LineDto(int PageNumber, int LineNumber, string LineType, bool IsCentered, int? SurahNumber, int? FirstWordId, int? LastWordId);` and `record LayoutDto(int PagesCount, int LinesPerPage, IReadOnlyDictionary<int, IReadOnlyList<LineDto>> Pages);`

## Surah metadata — `metadata/quran-metadata-surah-name.json` (object, 114)

```json
{ "1": { "id": 1, "name": "Al-Fātiĥah", "name_simple": "Al-Fatihah", "name_arabic": "الفاتحة",
         "revelation_order": 5, "revelation_place": "makkah", "verses_count": 7, "bismillah_pre": false } }
```

- DTO: `record SurahMetaDto(int Id, string Name, string NameSimple, string NameArabic, int RevelationOrder, string RevelationPlace, int VersesCount, bool BismillahPre);`

## Ayah metadata — `metadata/quran-metadata-ayah.json` (object, 6,236)

```json
{ "1": { "id": 1, "surah_number": 1, "ayah_number": 1, "verse_key": "1:1", "words_count": 4, "text": "بِسۡمِ ٱللَّهِ ٱلرَّحۡمَٰنِ ٱلرَّحِيمِ ١" } }
```

- `words_count` here → `quran_ayahs.words_count_source` (NOT the canonical word count).
- `text` uses a **different Uthmani encoding** than `words/uthmani.json`; do **not** equality-compare them.
- DTO: `record AyahMetaDto(int Id, int SurahNumber, int AyahNumber, string VerseKey, int WordsCount, string Text);`

## Fonts — out of scope

Page fonts are **not** part of Feature 002 (they belong to the later public Mushaf Reader). The importer does not read, copy, or validate fonts, and `quran_mushaf_pages` has no font columns. `qpc_glyph` is still imported from `qpc-v4.json` as a lightweight reference. The layout JSON's top-level `fontName` field is ignored.
