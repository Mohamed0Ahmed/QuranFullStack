# Contract: Source Resolution API

> Wire truth is the regenerated `openapi/swagger.json` + generated models (research.md R16). This
> document fixes the semantics the implementation must satisfy; shapes below use the JSON casing
> the generator emits (camelCase).

## `POST /api/linking/sources/resolve` — `[RequireOwner]`

One boundary for **all six** source families (plan D1 — a POST used as a read is deliberate and
metadata-valid because the route is Owner-only). Returns the complete validated ayah set in one
response — no paging, ever (spec FR-005).

### Request — `LinkingSourceDescriptorBody`

A discriminated object mirroring the Frontend's `LinkingSourceDescriptor` union exactly
(`models/linking-source.models.ts`):

```jsonc
// kind: "root"
{ "kind": "root", "label": "جذر: قول", "rootId": 42 }

// kind: "lemma" | "stem"  (typeCode nullable)
{ "kind": "lemma", "label": "…", "lemmaId": 7, "typeCode": null }
{ "kind": "stem",  "label": "…", "stemId": 105, "typeCode": "N" }

// kind: "unique-word"  (mode: "simple" | "tashkeel")
{ "kind": "unique-word", "label": "…", "mode": "tashkeel", "wordId": 3204 }

// kind: "word-type"  (selection union: word | root | stem | lemma)
{ "kind": "word-type", "label": "…",
  "selection": {
    "kind": "word", "tashkeelWordId": 501, "contextCode": "W:501",
    "case": "all", "tense": "past", "voice": "active",
    "scope": { "type": "verb", "childCode": null, "case": "all", "tense": "past", "voice": "all" } } }

// kind: "manual-mushaf-ayahs"
{ "kind": "manual-mushaf-ayahs", "label": "…",
  "manualAyahs": [ { "verseKey": "2:255" }, { "verseKey": "1:1" } ] }
```

Validation (`LinkingSourceDescriptorValidation`, Phase 1): well-formedness per family — ids are
positive integers; enum tokens from the exact vocabularies in `contracts/source-identity.md`;
verse keys match `^\d{1,3}:\d{1,3}$` with surah 1–114, ayah 1–286; manual set non-empty (at least
one ayah — there is **no** manual-specific size or identity-length cap; resolution is bounded by
the ordinary `MaxResolvedAyahs = 3000` rule like every family, and uniqueness rides the fixed-size
identity hash per `source-identity.md`); label non-blank. Impossible descriptors are **unrepresentable** in the
Domain value object — the body maps into it or fails with 400.

### Response — `200` `ApiResponse<LinkingResolvedSourceDto>` (standard envelope, Arabic messages)

```jsonc
{
  "sourceIdentity": "root|42",          // byte-exact per contracts/source-identity.md
  "resolvedAtUtc": "2026-08-12T10:00:00Z",
  "totalAyahCount": 1994,
  "ayahs": [                             // ordered by (surahNumber, ayahNumber) — always
    {
      "ayahId": 262, "verseKey": "2:255", "surahNumber": 2, "ayahNumber": 255,
      "surahNameArabic": "البقرة", "pageFrom": 42, "pageTo": 42,
      "matchedQuranWordIds": [ 12345, 12351 ],   // automatic: non-empty; manual Mushaf: may be empty
      "words": [                         // ordered by wordNumber; complete word list
        { "quranWordId": 12345, "wordNumber": 1, "textUthmani": "ٱللَّهُ", "isAyahMarker": false }
      ]
    }
  ]
}
```

### Status mapping

| Status | When |
| --- | --- |
| 200 | Resolved (all families) |
| 400 | Invalid/incoherent descriptor; `MaxResolvedAyahs` (default 3,000) exceeded; manual completeness failure — message names the exact verse key |
| 404 | Referenced dimension id (root/lemma/stem/word) does not exist |

### Semantics the implementation must preserve

- **Matched-word rule per family** (spec FR-008): for the five **automatic** families, every
  returned ayah has at least one matched word — an ayah with no match is never in the set. For
  **manual Mushaf** sources, the ayahs are the curator's chosen verses: each returns its complete
  canonical word list, and `matchedQuranWordIds` may be empty (typically is — user-authored word
  selections live in the workspace, not in resolution). A manual ayah with zero selected words is
  valid and still contributes the ayah.
- **Determinism is a contract** (spec FR-006): the CDK viewport computes offsets from index; an
  unstable order corrupts the viewport. Order ayahs `(surah_number, ayah_number)`, words
  `word_number`, always.
- **Marker behavior per family unchanged from today's explorer reads** (spec FR-009): Unique Word
  includes markers (flagged `isAyahMarker`); Root/Lemma/Stem **and Word Type** exclude them — all
  five pre-existing `AyahWordHydration` callers, both Word Types ayah reads included, are marker-free.
  Manual Mushaf, which has no explorer counterpart, includes markers flagged.
  `AyahWordHydration`'s marker filter becomes a parameter; existing consumers keep their shapes.
- **Bounded query shape**: the existing 4–5-command hydration pattern with `Skip/Take` removed;
  command count independent of ayah count. Never one query per ayah.
- **Manual Mushaf completeness proof** (server-side, plan D8/F5): verse exists and `verse_key`
  matches; non-marker `word_number`s contiguous `1..N`; `N == quran_ayahs.words_count_real`; every
  non-marker `location` unique with matching `(surah, ayah)` prefix. Any failure blocks the whole
  resolution naming the verse. No Mushaf page assembly anywhere.
- **Every word carries `quranWordId`** including Root/Lemma/Stem (already loaded by hydration —
  projection only, F4). Existing explorer DTOs are **not** changed (plan D3).
- **Caching decorator (Phase 4) is invisible on the wire** — same DTO, zero SQL on warm repeat;
  key derived only from the typed descriptor (research.md R11).
