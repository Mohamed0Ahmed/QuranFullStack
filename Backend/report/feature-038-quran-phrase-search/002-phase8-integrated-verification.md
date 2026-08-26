# Phase 8 integrated verification and actual sizing

Measured on 2026-08-26 from branch `wordsSearchFeature` at base commit `ca619c77`. All mutable
database work targeted the named disposable local database `quran_dashboard_phrase_phase8`.
The approved local source `quran_dashboard` was read only, no remote or production database was
contacted, and connection secrets are omitted. This is temporary feature evidence and is deleted
by the Phase 9 feature-artifact lifecycle.

## Outcome

Phase 8 passed its authorized integrated verification scope with two known unrelated smoke failures.
Both full phrase generations passed all 42 hard checks, the API and browser rejected unavailable and
changed-build state without serving partial data, and canonical pointer rollback was rehearsed and
reversed successfully. `canonical-data` was deliberately not run because Phase 9 owns the canonical
dump replacement; it is deferred, not passed.

The only production-source change found necessary by the walkthrough is scoped to the routed phrase
tabs. Active matching now ignores query parameters while requiring an exact path, and focused tabs
activate with Enter or Space. No storage configuration was changed: measured resident WAL and the
successful preflight do not justify replacing the existing configurable safety model with a local
machine guess. Capacity guidance from the actual measurements is recorded below.

## Approved source and disposable clone

| Evidence | Result |
| --- | --- |
| Branch guard | `wordsSearchFeature`; not `main`; clean at the Phase 8 base |
| Source migration history | 6 migrations; head `20260826012918_AddQuranPhraseSearchIndex` |
| Source state | revision 1; non-stale; fingerprint `8d9eedffe7b8a490f0e9b597a63229fbe91d3cd4a1bfd32d86d92801167fb075` |
| Source pointers before and after | active `9c8559aa-ea1b-4a54-89ef-0dfa8cd24b82`; previous `593bacb1-8037-49d6-8bb2-4c9e749e4c00` |
| Source database size | 11,381,814,975 bytes |
| Source phrase relations | 10,687,791,104 bytes across its retained generations |
| Disposable target | local `quran_dashboard_phrase_phase8` only |
| Rebuildability proof | schema and all non-derived data restored from a 102,968,693-byte custom dump; `quran_phrase_*` data excluded and the migration's empty state seed restored |
| Clone foundation | same migration head; 83,668 positive-number Quran rows; zero derived builds before the run |
| Clone starting size | database 579,991,231 bytes; phrase relation overhead 294,912 bytes |
| Cleanup proof | API/UI stopped; clone dropped through `Backend/scripts/drop-db --yes`; database existence count returned 0; temporary dump/reports/responses removed |

A physical template copy was intentionally not used: it would have duplicated 10.69 GB of already
derived phrase data before measuring a fresh build. The logical clone retained the approved schema,
foundation, and migration state while making derived rebuildability explicit.

## OpenAPI and verification gates

The following were run sequentially. Contract generation was repeated from the same tree.

| Gate | Result |
| --- | --- |
| `Backend/scripts/export-swagger` plus `npm run generate:api`, twice | PASS; no intended diff |
| Swagger SHA-256 | `2e4cce23ba3f9ea9cc05dd4e24ba7a659bb88df4a427a0d8d2e6c8fac4dbf3dc` both times |
| Generated client tree SHA-256 | `1b873962d515fe3b244e34807ae1fa269f22cb083187ee9a9980a2c51a3d7ade` both times |
| `Backend/scripts/check-api-contract` | PASS; API contract up to date |
| `Backend/scripts/qd-build` | PASS; 0 errors; 2 occurrences of the existing SSH.NET NU1903 advisory warning |
| `Backend/scripts/check-pending-model --no-build` | PASS |
| `Backend/scripts/test-backend migration --no-build` | PASS, 1/1 |
| `Backend/scripts/test-backend pipeline --no-build` | PASS, 222/222 |
| `Backend/scripts/test-backend smoke --no-build` | 98/100; two unrelated known failures listed below |
| `Backend/scripts/test-backend tier-b --no-build` | PASS, 349/349 |
| `Backend/scripts/test-backend canonical-data --no-build` | NOT RUN; deliberately deferred to Phase 9 dump replacement |
| `npm run check:no-unit-specs` | PASS |
| `npm run typecheck:app` | PASS |
| `npm run build:verify` | PASS; existing initial-bundle warning, 703.43 kB versus 700.00 kB budget |
| `git diff --check` | PASS |

The retained smoke failures were unchanged from the documented baseline:

1. `SmokeCoverageParityTests.EveryRegisteredRoute_HasACatalogEntry` reports the unrelated Mushaf
   door-highlights route missing from the catalog.
2. `SmokeBootGuardTests.AuthenticationSchemes_SeparateApiAccessFromInteractiveIdentityEvidence`
   still expects only Bearer/Logto while the application also registers
   `ApplicationAuthentication` and `DeviceSession`.

The canonical build initially returned a sandbox-only zero-error failure because its restore graph
could not open the required process/socket path. The identical canonical script outside that
restriction passed. The migration lane and rollback verb had the same initial socket restriction and
passed on their authorized reruns. No product failure is hidden by those retries.

## Full generation builds

Both commands used the canonical importer verb in `DOTNET_ENVIRONMENT=Production`, the environment
documented for importer operation. An initial Development-environment attempt stopped before opening
the database because host validation could not construct Access handlers without `ICurrentUser`; it
wrote no phrase rows or pointers. The operational Production runs were:

```text
dotnet run --project Backend/tools/QuranDashboard.DataImporter/QuranDashboard.DataImporter.csproj --no-build -- build-phrase-index --report-out <temporary-report-root>
dotnet run --project Backend/tools/QuranDashboard.DataImporter/QuranDashboard.DataImporter.csproj --no-build -- build-phrase-index --force --report-out <temporary-report-root>
```

| Measurement | First generation | Forced replacement |
| --- | ---: | ---: |
| Build ID | `122634eb-3537-4472-a858-fc9578d2ad1f` | `35288551-d748-4c7f-a85a-f5482ad71101` |
| Outcome | PASS / Active | PASS / Active |
| Hard checks | 42/42 | 42/42 |
| Exact / similarity ready | true / true | true / true |
| Report duration | 398,252 ms | 532,546 ms |
| Wall time | 6:39.83 | 8:54.26 |
| Peak managed memory | 253,122,760 bytes | 253,113,248 bytes |
| Peak RSS | 339,092 KiB | 424,016 KiB |
| Search tokens | 33,756 | 33,756 |
| Variants | 1,368,351 | 1,368,351 |
| Occurrences | 1,591,910 | 1,591,910 |
| Similarity edges | 1,115,977 | 1,115,977 |
| Similarity anchor stats | 560,722 | 560,722 |
| Source fingerprint | approved fingerprint | approved fingerprint |
| Warnings / errors | none / none | none / none |

After the second activation exactly two compatible, ready generations remained. The second build was
active and the first was previous. The incremental retained-generation footprint from the first
post-build database to the second was 2,207,866,880 bytes.

## Storage, WAL, and capacity

### Preflight and observed growth

| Measurement | First generation | Forced replacement |
| --- | ---: | ---: |
| Database before | 579,991,231 | 2,809,370,303 |
| Phrase relations before | 294,912 | 2,229,485,568 |
| Preflight additional-generation allowance | 579,991,231 | 2,809,370,303 |
| Preflight WAL allowance | 579,991,231 | 2,809,370,303 |
| Configured safety margin | 4,294,967,296 | 4,294,967,296 |
| Required free bytes | 5,454,949,758 | 9,913,707,902 |
| Verified available free bytes | 30,686,531,584 | 28,423,491,584 |
| Preflight result | PASS | PASS |
| Database after | 2,809,353,919 | 5,017,220,799 |
| Phrase relations after | 2,229,469,184 | 4,437,098,496 |
| WAL LSN delta | 4,669,954,256 | 6,647,711,800 |
| Free bytes after | 28,423,512,064 | 26,215,485,440 |

The WAL deltas are cumulative generated bytes, not resident filesystem bytes. At final capture the
local PostgreSQL instance retained 1,073,741,824 WAL bytes, with `max_wal_size=1GB`,
`wal_keep_size=0`, and archiving disabled. A deployed database can retain WAL differently, so its
operator-provided free-space proof must reflect that environment rather than copying this local
resident-WAL number.

Final measured double-generation database size was 5,016,975,039 bytes, of which phrase relations
were 4,437,114,880 bytes. Relation sizes were:

| Relation | Heap bytes | Index bytes | Total bytes |
| --- | ---: | ---: | ---: |
| `quran_phrase_variants` | 1,171,955,712 | 1,216,225,280 | 2,388,180,992 |
| `quran_phrase_occurrences` | 268,992,512 | 732,012,544 | 1,001,005,056 |
| `quran_phrase_similarity_edges` | 224,460,800 | 529,096,704 | 753,557,504 |
| `quran_phrase_similarity_anchor_stats` | 76,611,584 | 202,129,408 | 278,740,992 |
| `quran_phrase_search_tokens` | 7,413,760 | 8,110,080 | 15,523,840 |
| State and build metadata | 24,576 | 81,920 | 106,496 |
| **Total** | **1,749,458,944** | **2,687,655,936** | **4,437,114,880** |

### Railway capacity decision

A 5 GB volume is not sufficient. The measured steady database with only active and previous
generations is already 5,016,975,039 bytes, before staging another replacement, WAL, or free-space
margin. Even interpreting the quota as 5 GiB leaves only about 352 MB, far below the preflight.

The historical 9,913,707,902-byte preflight above was calculated before the second generation, when
the current database was 2,809,370,303 bytes; it is not the requirement for another build from the
final steady state. Applying the builder's current formula to the measured final database gives
14,328,917,374 required free bytes: twice 5,016,975,039 bytes plus the 4,294,967,296-byte safety
margin. The resulting total-volume hard floor is 19,345,892,413 bytes, or about 19.35 decimal GB /
18.02 GiB. This is the measured-current-corpus hard boundary for this build formula, not a universal
future minimum.

A decimal 20 GB volume would leave only 654,107,587 bytes above that hard floor. It therefore barely
passes the local formula and is not meaningful operational headroom. Provision 25 GB or more as the
safer starting class, then supply a fresh remote verified-free-byte and WAL-retention proof immediately
before every build. A production decision must use the deployed provider's actual free capacity and
WAL behavior.

No config value was changed. The second replacement passed the current proof, actual generation size
was below its additional-generation allowance, and local resident WAL stayed within the combined WAL
and safety headroom. Increasing a default from cumulative LSN alone would mislabel generated WAL as
simultaneously resident storage.

## Sanitized request plans

`EXPLAIN (ANALYZE, BUFFERS)` was captured without retaining SQL text, input text, or opaque
references. The first observation followed the completed builds, so PostgreSQL buffers were already
mostly warm. The table therefore distinguishes first observed and immediate repeat; it does not claim
a forced physical-cache cold run. API first measurements below did start from a fresh API process.

| Read shape | Intended access and scan boundary | First observed | Repeat |
| --- | --- | ---: | ---: |
| Default repetitions | build/mode/length/order index; 25 rows; 32 shared hits | 0.224 ms | 0.202 ms |
| Worst paired-context occurrence set | build+variant occurrence index; 2,763 occurrences; 1,034 shared hits | 9.976 ms | 9.330 ms |
| Worst paired-context ayah hydration | occurrence subquery index-only; 37,183 words hydrated | 79.543 ms | 83.438 ms |
| Worst length-2/3 manual similarity partition | build/mode/length variant index; 53,992 non-anchor variants scored; 4,376 shared hits | 336.249 ms | 332.972 ms |
| Default global anchors | anchor-stat ordering index plus variant primary key; 129 shared hits | 1.366 ms | 0.908 ms |
| Direct neighbors | both directional edge indexes index-only, zero heap fetches, then variant key; 111 shared hits | 0.590 ms | 0.399 ms |

The worst paired-context hydration covered almost half the Quran-word table. PostgreSQL deliberately
selected one bounded scan of the 83,668-row base table instead of thousands of random index probes;
the occurrence selection itself remained index-only. No phrase query scanned another build, and the
manual-similarity scan was bounded to one active build, mode, and length partition. Neighbor lookup
used only direct edges and performed no transitive traversal.

## API latency and failure-state evidence

All API/UI services were started only through `Backend/scripts/qd-api` and
`Backend/scripts/qd-ui` against the disposable clone.

| Read shape | Fresh-process first | Immediate repeat | Result |
| --- | ---: | ---: | --- |
| Default repetitions | 165.139 ms | 21.131 ms | HTTP 200; total 8,950 |
| Worst paired-context branch read | 385.274 ms | 266.664 ms | HTTP 200; paired source population preserved |
| Worst length-2/3 manual similarity | 711.377 ms | 496.125 ms | HTTP 200; one qualifying result |
| Default global anchors | 253.439 ms | 31.684 ms | HTTP 200; total 42,300 |
| Direct neighbors | 80.108 ms | 23.198 ms | HTTP 200; total 341 |

The high-cardinality context resolution completed in 367.4 ms and its following branch read in the
same process completed in 266.664 ms. A controlled stale-state rehearsal returned HTTP 503 with
`phrase_index_unavailable` in 9.271 ms and null data. A request using the superseded build returned
HTTP 409 with `phrase_index_changed` in 9.225 ms and null data. The clone state was restored after the
503 rehearsal.

## Desktop, mobile, and keyboard walkthrough

| Phase 8 matrix case | Evidence |
| --- | --- |
| Simple length 2 descending | Highest occurrence result first; 8,950 qualifying phrases |
| Switch to tashkil | Total changed to 8,726; identity changed while full Uthmani ayah rendering remained intact |
| Prefixed versus unprefixed phrase | Distinct candidate populations, 40 versus 12 complete contexts; no dropped prefix |
| Hamza written/omitted | Both input spellings resolved to the same exact candidate identity while corpus spelling remained exact |
| Ambiguous folded spelling | Two explicit candidates shown; no silent merge |
| Phrase resolving to one source token | One exact candidate shown; 142 occurrences and 141 complete contexts |
| Start and end boundaries | Direct previous and following boundary states shown; labels remained side counts |
| Previous then following selection | Both branches filtered the same paired occurrence set; fixed-query center remained stable |
| Load more on one side | Selected side expanded from 25 to 50 options; other side, context page, URL, and focus remained stable |
| Both boundaries fixed | Exact full-context count available and matched the one fully fixed context in the bounded full-ayah case |
| Nine-token 50 percent | Minimum exact-match control enforced five words; bounded request completed |
| Length 2/3 similarity | On-demand partition scoring worked; the generation hard check confirmed no stored short edges |
| Long bounded input | 128 source tokens, 678 characters, and 1,229 UTF-8 bytes accepted; UI removed shareable state and displayed the session-only notice |
| Build activation while open | Old state reset, changed-index message shown, raw input retained, opaque state removed, and no old result remained |
| Missing/stale index | Explicit unavailable panel and retry action; never an empty-result presentation |
| Desktop 1440 | RTL previous/query/following placement, no horizontal overflow, full cards and deep links rendered |
| Mobile 390 | Previous, fixed query, then following stacked; document width stayed within viewport with no horizontal overflow |
| Keyboard | RTL arrows, Home/End, Enter, and Space changed routed tabs; branch choices and semantic pagination controls were reachable |

Browser back/forward restored route state and filters. The tab fix was verified after a hot rebuild and
again through the production frontend gates. Existing Words Hub and Mushaf page 4 were spot-checked:
the phrase entry point, Quran words, page navigation, Uthmani rendering, and deep links showed no
regression.

## Log redaction

Representative resolution, context, repetition, similarity, stale, unavailable, and rollback reads
were followed in the API process output. EF logged parameter names and `?` placeholders, including
encoded-input and reference parameters, but not their values. A source search found no feature logger
that emits raw query or opaque-reference values. The retained report likewise contains no input text,
encoded query value, resolution reference, or connection secret.

## Rollback and final cleanup

The canonical `rollback-phrase-index` importer verb ran under the source fence. It activated the first
compatible generation, marked the second superseded, and swapped active/previous atomically in 4.60
seconds with 155,004 KiB peak RSS. Direct database verification confirmed equal counts, readiness,
format compatibility, revision, and fingerprint. Capabilities and repetitions returned HTTP 200 from
the rolled-back active generation.

Reloading an open browser page tied to the newer build produced the explicit changed-index state,
preserved only the human input, removed the stale opaque reference, and displayed no partial result.
Running the same canonical verb again restored the newest generation in 4.82 seconds. Final pointers
were active `35288551-d748-4c7f-a85a-f5482ad71101` and previous
`122634eb-3537-4472-a858-fc9578d2ad1f`; both were ready and identical in derived counts and source
fingerprint.

The browser viewport was reset and the agent-created tab closed. API and UI listeners were stopped.
The disposable database was dropped through the exact Backend script, its absence was verified, all
temporary Phase 8 artifacts were removed, and a final read-only check showed the approved source's
original pointers, fingerprint, and non-stale state unchanged.

No new test file or test method was added. No production operation, canonical dump overwrite,
`canonical-data`, `pre-pr`, commit, stage, push, or formal review was performed in Phase 8.
