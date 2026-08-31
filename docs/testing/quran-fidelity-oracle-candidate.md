# Quran fidelity oracle candidate report

**Artifact:** `compact-cross-stack-base`

**Candidate:** `2026.08.31.3`

**Issue:** #83

**Review date:** 2026-08-31

**Owner role:** artifact maintainer

## Reason

The required Quran-fidelity journey needs one small source-reviewed trust root that is independent of
the runtime database. This candidate expands the page-1 oracle from word-only evidence to exact ayah
Unicode, line/word relationships, and one real translation/tafsir association for verse `1:1`. The
compact payload adds only the five study rows needed for that association. It does not change the
canonical Quran, page, line, or word rows.

## Artifact deltas

| File | Previous SHA-256 | Previous bytes | Candidate SHA-256 | Candidate bytes | Reason |
| --- | --- | ---: | --- | ---: | --- |
| `manifest.json` | `967cba3e0f185ae1dc89e61f8d5fef149f7a94f1bda1de9f72a290bc2b618a88` | 1,871 | `f5b595378fb7e0c4aefb3ba832fedc7a1168b2d24f1bfe3a128bff4c352203ee` | 4,414 | Record the reviewed sources and the five added study-table counts. |
| `oracle.json` | `8d24afda3bcff11f0dc7b76ff6122e36289ee745c35e429f788d20ded76fed2f` | 6,338 | `bde6d3bf7ea0a9d594847bd8d661ffa05ddf8a2b46ae3b2e5c8fa63320ae4a21` | 17,428 | Add exact ayah, layout, tafsir, translation, and visible source-identity evidence. |
| `compact-cross-stack-base.dump` | `2ab22eccfd7b318ce80a802081dfc3644033c0b9459ed489cee7cc8256c2dc95` | 725,329 | `cce855a3c76aa55af3a0d6203d4026d73655868a06354e051db71bd82e666a1f` | 740,705 | Add one source and one `1:1` mapping for each of tafsir and translation. |

The migration contract is unchanged: head
`20260826012918_AddQuranPhraseSearchIndex`, count `6`. PostgreSQL remains `18.6`, using the pinned
container digest `sha256:7341002d2b8c7c5bdd7542a671a95b36196c0b5b888daf454ae4fc33ba5346d7`.

The producer command is:

```text
dotnet ef migrations script; pg_restore --disable-triggers; retain page-1 Mushaf rows; add reviewed 1:1 study rows; pg_dump --format=custom
```

## Table deltas

All previously declared table counts remain unchanged. The following tables are added to the compact
scope; each changes from zero rows to one row:

| Table | Previous | Candidate | Reason |
| --- | ---: | ---: | --- |
| `quran_tafsir_sources` | 0 | 1 | Identify `ar-muyassar` with reviewed display/provenance metadata. |
| `quran_tafsir_entries` | 0 | 1 | Preserve the exact upstream `1:1` HTML. |
| `quran_tafsir_ayah_entries` | 0 | 1 | Associate that entry with canonical ayah `1:1`. |
| `quran_translation_sources` | 0 | 1 | Identify `en-sahih-international` with reviewed display metadata. |
| `quran_translation_ayah_entries` | 0 | 1 | Associate the exact upstream translation with canonical ayah `1:1`. |

## Source and provenance review

The previous source set contained `quran-canonical`, `quran-foundation-uthmani`, and
`quran-foundation-layout`. The candidate keeps those identities, narrows the Uthmani source's role to
word text/locations, and adds the separately hashed ayah and study inputs below.

| Identity | SHA-256 | Reviewed input and purpose |
| --- | --- | --- |
| `quran-foundation-uthmani` | `380c2080cb5c4639257ac4bbedb395c24fb85f04b8190532705714c830382239` | `resources/import-sources/quran-foundation/words/uthmani.json`; page-1 word text and locations. |
| `quran-foundation-ayah-metadata` | `bf1e0d24abf378acc7ebd35c4ecbcbf941057594fefa50322cc5c801565de0f4` | `resources/import-sources/quran-foundation/metadata/quran-metadata-ayah.json`; exact ayah Unicode. |
| `quran-foundation-layout` | `9d6cf089a40c8c8e17939631470aa93fa33aea07aac3fd5dc7f6519b94d40d53` | `resources/import-sources/quran-foundation/mushaf/qpc-v4-pages-layout.json`; exact page-1 line membership. |
| `translation-en-sahih-international` | `db544f7634cba2d69fdb9b0ccc43139a8784a8d8e33cb7802aefb0530614cb58` | `resources/import-sources/quran-translations/sources/en-sahih-international.fn.json`; exact `1:1` content. |
| `translation-source-manifest` | `f61cbcd36991cf88efdaaa835ff473aa85f4ac05edf3b8480a77ce3e2303ace9` | `resources/import-sources/quran-translations/manifest.json`; source key, package hash, coverage, and unknown license/provenance status. |
| `translation-display-metadata` | `7f3c511d96412554b7383c9c02560fe5feeb7137f43dcc4884b8b7642d908d0a` | `resources/import-sources/quran-translations/source-display-metadata.json`; reviewed Arabic/English display identity. |
| `tafsir-ar-muyassar` | `091e857e8f88142b20e7a38ffc0075cb9c3b92a07dcf66376dbe1cdd1fa2848e` | `resources/import-sources/quran-tafsirs/sources/ar-muyassar.json`; exact `1:1` content. |
| `tafsir-source-manifest` | `ae22b29e76eeac1789091d367382d3baa6291771c1f763c95ebd5f92a884daf8` | `resources/import-sources/quran-tafsirs/manifest.json`; source key, visible identity, contributor, package hash, coverage, and unknown license/provenance status. |

The translation and tafsir manifests both report unknown license/provenance. The candidate therefore
records them as internal-only test evidence and makes no redistribution or public-release claim.

## Golden sentinel comparison

`quran-fidelity.page-1-words` remains exactly 36 ordered words. Its expected word locations, verse
keys, exact Uthmani values, and ayah-marker flags are unchanged. The sentinel oracle hash changes from
`8d24afda3bcff11f0dc7b76ff6122e36289ee745c35e429f788d20ded76fed2f` to
`bde6d3bf7ea0a9d594847bd8d661ffa05ddf8a2b46ae3b2e5c8fa63320ae4a21` because the oracle now also
records:

- all seven exact page-1 ayah strings;
- all eight page-1 line relationships;
- exact `1:1` tafsir and translation source identities/content.

No PhraseSearch fingerprint, build, pointer, or readiness field changes in this candidate.

## Independent-review procedure

The reviewer must hash the listed inputs, compare the oracle's seven ayahs, 36 words, eight lines, and
both `1:1` study strings directly with those inputs, then run:

```bash
Backend/scripts/test-artifacts verify --artifact compact-cross-stack-base
NUGET_PACKAGES=/tmp/qdb-issue-83-nuget Backend/scripts/test-backend feature MushafReader --build
npm_config_cache=/tmp/qdb-issue-83-npm-cache npm run e2e:critical
```

Acceptance is based on that source comparison plus the independent Standards and issue-spec reviews;
matching the dump to its own manifest is not sufficient.
