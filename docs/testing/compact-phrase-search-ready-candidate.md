# Compact PhraseSearch-ready artifact candidate

**Candidate date:** 2026-08-31

**Artifact:** `compact-phrase-search-ready` version `2026.08.31.1`

**Reason:** Protect the reviewed available PhraseSearch path through durable Add to Workspace without
building the phrase index during an ordinary Playwright run.

## Trust inputs

This is a new compact overlay, so there is no previous artifact generation to compare. The candidate
was selected from these existing source-traceable inputs; none of the Quran text, source identities,
hashes, or PhraseSearch records were synthesized.

| Input | Bytes | SHA-256 | Review use |
| --- | ---: | --- | --- |
| `resources/import-sources/quran-foundation/words/uthmani.json` | 8,872,057 | `380c2080cb5c4639257ac4bbedb395c24fb85f04b8190532705714c830382239` | Exact word ID, location, and Uthmani text comparison for 1:1, 11:41, and 27:30 |
| Completed `phrase-index-build-report.json` for build `4b5e247e-decb-4ea4-9098-2b5b88759c88` | 92,943 | `2010f1cd2c0fe7a23a55eea196902d6b0fa165abe2acf9db5a5ed6f7ff7f17e0` | Successful activation, format, source fingerprint, and exact/similarity readiness |
| `railway-phrase-ready-data-20260829.dump` | 113,913,638 | `b4be17fc52b8e2d89c273585ec762bab82e34fdf755e99dccf50d44d267e9d98` | Selected build, token, variant, occurrence, anchor-stat, edge, and word-identity rows |
| Locked `compact-cross-stack-base` payload | 740,705 | `cce855a3c76aa55af3a0d6203d4026d73655868a06354e051db71bd82e666a1f` | Migration-aligned schema and already reviewed page-1 Quran fixture composed before this overlay |

The Uthmani comparison covered Quran word IDs 1-4, 30296-30306, and 51975-51982 at their exact
locations. It also covered IDs 63, 78, 386, 629, 1831, 2201, 2453, 2737, 5552, 7379, and 20180,
which are the canonical first occurrences required by the retained unique-word foreign keys. The
expected phrase is the real four-word variant at 1:1 and 27:30. The non-identical
comparison is the real variant at 11:41, with two matched words and differing positions 3 and 4.

## PhraseSearch identity and reviewed oracle

- Active build ID: `4b5e247e-decb-4ea4-9098-2b5b88759c88`.
- Source fingerprint: `6320611aa63d3ee757e4bc5a75fc19b5c5fd0e78a257be59e78aed7d5a145957`.
- Build status: succeeded and active; exact and similarity readiness are both true.
- Exact variant ID: `111821`, with occurrences at 1:1 and 27:30.
- Repetitions display assembled from the canonical Imlaei-simple values of Quran word IDs 1-4:
  `بسم الله الرحمان الرحيم`.
- Similar variant ID: `111822`, occurring at 11:41.
- Context expectation: 1:1 and 27:30.
- Similarity expectation with a two-difference limit: 1:1, 27:30, and 11:41.
- Add to Workspace expectation: selected ayah 1:1 persists Quran word IDs 1-4 and is independently
  readable from the Linking workspace API.

## Table selection

The full snapshot was reduced to the smallest source-backed selection needed by the reviewed oracle.
Compact build totals were normalized to the compact generation so runtime capabilities describe the
delivered rows rather than the discarded full corpus.

| Table | Full rows | Compact rows | Reason retained |
| --- | ---: | ---: | --- |
| `quran_mushaf_pages` | 604 | 13 | Page references for results and canonical unique-word first occurrences; page 1 is already in the base |
| `quran_words` | 83,668 | 30 | Readable result words plus every canonical unique-word first occurrence; IDs 1-4 agree with the base/source |
| `quran_words_unique_simple` | 14,910 | 17 | Exact search-token and selected word identities |
| `quran_words_unique_tashkeel` | 19,016 | 18 | Required Quran word identities, including the exact first-occurrence closure |
| `quran_phrase_index_builds` | 1 | 1 | Reviewed active build metadata with compact counts |
| `quran_phrase_index_state` | 1 | 1 | Active build pointer, source fingerprint, and non-stale state |
| `quran_phrase_search_tokens` | 33,756 | 6 | Tokens referenced by the two reviewed variants |
| `quran_phrase_variants` | 1,368,351 | 2 | Exact and non-identical reviewed variants |
| `quran_phrase_occurrences` | 1,591,910 | 3 | Two exact occurrences and one similar occurrence |
| `quran_phrase_similarity_anchor_stats` | 560,722 | 2 | Reviewed query/neighbor anchor statistics |
| `quran_phrase_similarity_edges` | 1,115,977 | 1 | The real reviewed similarity edge |

No table was added outside the manifest. The overlay contains Quran/PhraseSearch data only; Access,
Abwab, and Linking state remain mutable scenario data.

## Candidate outputs

| File | Previous | Candidate bytes | Candidate SHA-256 |
| --- | --- | ---: | --- |
| `test-artifacts/compact-phrase-search-ready/manifest.json` | absent | 3,173 | `a216e809dd168852a35df25e38dab5e6acf5318cc5d2f85ec5b1439a76ff7de9` |
| `test-artifacts/compact-phrase-search-ready/oracle.json` | absent | 4,639 | `5562ec216ec3dd87bda16034be97c8fac34577315c82f040ddc5d2c571677cb9` |
| `test-artifacts/compact-phrase-search-ready/compact-phrase-search-ready.dump` | absent | 8,699 | `de231a3f6aab423de3c898fa8550194606c5bb38b56fc80c5bebda0aeee1db11` |

The payload TOC contains only the 11 tables declared by the manifest; it carries no sequence `setval`
records or mutable Access, Abwab, Linking, role, or user state.

The migration stays at `20260826012918_AddQuranPhraseSearchIndex` with six migrations, and the
producer remains PostgreSQL 18.6. The tracked lock pins every output hash and size, the manifest hash,
the source fingerprint, the available readiness expectation, the PostgreSQL image digest, and the
content-addressed payload identity.

## Composition and ordinary execution

The Playwright database harness verifies both locked artifacts, restores the compact base once, and
restores this overlay with all foreign-key constraints active. It then compares the runtime active
build, source fingerprint, non-stale state, succeeded status, and exact/similarity readiness with the
verified manifest. It never runs the PhraseSearch builder. Mutating journeys reset only the literal small-table allowlist, seed the
existing canonical Linking revision precondition when required, and compare deterministic before and
after fingerprints of every `quran_*` table. A different active build, stale state, changed capability,
or immutable row mutation fails the journey.
