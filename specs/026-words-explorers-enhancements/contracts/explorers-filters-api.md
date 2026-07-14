# Contract: Unique Words / Roots / Lemmas / Stems filter params (Feature 026)

Existing routes; all params below are **new, optional, additive** query params on the
LIST reads only. Detail/summary/surah/ayah endpoints are unchanged. Responses stay
`ApiResponse<PagedResult<...ListItemDto>>` with unchanged item shapes. Requests
without the new params behave byte-identically to today.

## 1. Count-range params (shared grammar)

Per metric `k`: `kMin` / `kMax` (`int?`).

- Validation (all four controllers/handlers): `kMin >= 0`; `kMax >= kMin` when both
  present; violation → 400 with the page's new `InvalidFilter` outcome + centralized
  Arabic message. Either bound omissible (open-ended).
- Composition: all active ranges AND together, and AND with existing
  `search`/`sort`/paging.
- Counting semantics: ranges compare against the SAME count values the list rows
  display (no recomputation, no family change).

### Metrics per endpoint

| Endpoint | Range params |
|---|---|
| `GET api/words/unique/{kind}` | `occMin/Max`, `ayahsMin/Max`, `surahsMin/Max` — SQL predicates on `occurrences_count` / `ayahs_count` / `surahs_count` |
| `GET api/words/roots` | those three + `simpleWordsMin/Max`, `tashkeelWordsMin/Max`, `lemmasMin/Max`, `stemsMin/Max` — in-memory predicates over the cached whole-summary rows |
| `GET api/words/lemmas` | Unique trio + `simpleWordsMin/Max`, `tashkeelWordsMin/Max`, `stemsMin/Max` |
| `GET api/words/stems` | Unique trio + `simpleWordsMin/Max`, `tashkeelWordsMin/Max` |

## 2. Association params

| Endpoint | Param | Validation | Semantics |
|---|---|---|---|
| `GET api/words/unique/{kind}` | `primaryType` (string) | POS code must exist in the POS catalogue (`quran_pos_tags`, resolved via the existing readers), else 400 `InvalidFilter` | keeps rows whose **primary** word type — computed by the same rule as the displayed chip enrichment — equals the code. Predicate lives in the base SQL; the enrichment and the predicate must share one selection rule (agreement is a tested invariant). Frontend feeds its type select from the existing word-types tree read (no new endpoint). |
| `GET api/words/unique/{kind}` | `rootId` (int) | positive, else 400 | keeps rows whose **primary** root (same rule as displayed chip) is this root |
| `GET api/words/lemmas` | `rootId` (int) | positive, else 400 | real FK belonging: lemma's `root_id = @rootId` |
| `GET api/words/stems` | `rootId` / `lemmaId` (int) | positive, else 400 | derived **primary** association match (the association already displayed on the row). Label contract: "الجذر الأساسي" / "الصيغة المعجمية الأساسية"; README documents primary-not-sole. |

Unknown/absent id values that pass validation but match nothing → 200 with empty page
and `TotalCount = 0` (not 404 — filters are not identities).

## 3. Caching

- **Backend**: Unique Words list cache keys gain every new param (normalized; absent ⇒
  pre-feature key unchanged). Roots/Lemmas/Stems: **no backend cache-key change** —
  their readers cache the whole summary and derive per request.
- **Frontend**: all four pages' list cache keys gain every new param.

## 4. Statistics note (C1 — no API change)

The headline result-count on these four pages is the existing
`PagedResult.TotalCount` of the (now filterable) list read. **No new endpoint, no new
field, no aggregation.** Any drift between the stat and pagination is a bug by
definition.

## 5. Logging

Range/association params may be logged as booleans/ids; `search` text remains
never-logged (existing rule).
