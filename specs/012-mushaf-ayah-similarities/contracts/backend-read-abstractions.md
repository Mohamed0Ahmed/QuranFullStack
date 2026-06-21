# Contract: Backend Read Abstractions

## Purpose

Define the application-facing read contracts for Feature 012. Infrastructure implements these reads; Application handlers orchestrate validation/outcomes; API controllers only bind and wrap responses.

## Existing Reader Extension

`IAyahStudyReader.GetAyahStudyAsync(...)` keeps its existing inputs and response, but `AyahStudyResponse` gains `SimilaritySummary`.

Required behavior:

- Counts are computed for the selected ayah only.
- Counts are returned even when tafsir/translation/full-i'rab source blocks are null.
- Counts do not trigger loading detail lists.

## New Reader: Similar Ayahs

Conceptual signature:

```text
GetSimilarAyahs(verseKey, cancellationToken) -> SimilarAyahsResponse? or not-found outcome
```

Responsibilities:

- Validate selected ayah exists.
- Read outgoing and incoming similar links.
- Deduplicate related ayahs.
- Join related ayahs to canonical ayah/surah metadata.
- Return flat items.

Must not:

- Persist reverse edges.
- Return EF/domain entities directly.
- Copy Quran text from similarity tables.

## New Reader: Ayah Mutashabihat

Conceptual signature:

```text
GetAyahMutashabihat(verseKey, cancellationToken) -> AyahMutashabihatResponse? or not-found outcome
```

Responsibilities:

- Validate selected ayah exists.
- Read selected ayah occurrences.
- Load distinct groups and all sibling occurrences for each group.
- Join occurrence ayahs to canonical ayah/surah metadata.
- Derive phrase text from canonical words if included.
- Preserve grouped output.

Must not:

- Flatten groups.
- Invent phrase text when word ranges cannot resolve.
- Copy Quran text from mutashabihat tables.
- Create or modify import data.

## Handler Outcomes

Each new query should expose controlled outcomes:

- Success with response.
- Invalid verse key.
- Not found.

Unexpected errors remain under global exception handling.
